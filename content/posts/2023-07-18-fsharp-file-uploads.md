---
title: F# File uploads with Saturn and Falco
subtitle: ~
categories: fsharp, dotnet, backend, file-upload
abstract: Today's topic is File Uploads to F# backends!
date: 2023-07-18
language: en
---

[giraffe]: https://giraffe.wiki
[falco]: https://www.falcoframework.com/
[saturn]: https://saturnframework.org
[htmx]: https://htmx.org/

Hello there folks!

It has been quite a while (once again hah!) while I've been busy working in a few of my own F# projects the truth is that I didn't have much to write about in F# after my last "Simple things in F#" series.

Today I don't have a new series to start with but rather a simple example which may or may not grow in another blog series. For the moment we'll talk about how to do File uploads to an F# backend powered by [Falco] and [Saturn] so let's get started!

The samples for this post can be found at this repository:

https://github.com/AngelMunoz/fsharp-file-uploads

---

So the situation is the following

> You just found F# or saw a tweet/toot/thread related to F# and decided to give it a go, you've been told there are F# web frameworks like [falco] and [saturn] but you haven't seen a lot of samples out there, it would be nice if there's a focused sample you can take a look at.

Well let me tell you that I've been there as well! Well... in my case it was with php, then with node, then with C# and at some point F# as well hah! But I digress, Let's start with our client side where we'll be sending files to our back-end.

### HTML Forms

At this point in time, HTML is present everywhere so this is where we will start, given the following HTML:

```html
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Send a File to the Server</title>
  </head>
  <body>
    <!-- Standard HTML Post -->
    <form id="standard">
      <fieldset>
        <legend>Standard Html Upload</legend>
        <!-- don't forget the name, it is quite important! -->
        <input type="file" name="my-uploaded-file" />
        <!-- trigger the submit event with this button/input -->
        <input type="submit" value="Upload" />
        <progress id="standard-progress" style="opacity: 0"></progress>
      </fieldset>
    </form>
    <!-- Once the server answers our request we'll set the responses below -->
    <section id="response-target"></section>
    <section id="error-target"></section>

    <!-- Let's also sprinkle vanilla JS here to make it a little bit dynamic -->
    <script>
      // Let us wait for the DOM to load, so we can find our HTML nodes
      document.addEventListener("DOMContentLoaded", () => {
        // let's gather the elements we'll be working with
        const form = document.querySelector("#standard");
        const target = document.querySelector("#response-target");
        const errorTarget = document.querySelector("#error-target");
        const progress = document.querySelector("#standard-progress");

        form.addEventListener("submit", (e) => {
          // avoid page reloads we'll send our request manually
          e.preventDefault();
          // these days building a form data is so simple
          // just pass the form reference to the constructor and
          // all inputs related to the form will be added automatically
          const formData = new FormData(form);
          progress.style.opacity = 1;
          // We'll use a standard fetch here for simplicity
          fetch("/uploads", {
            method: "POST",
            body: formData,
          })
            .then((response) =>
              // if the status code is not ok, then we'll send the text to the catch handler
              response.ok
                ? response.text()
                : // remember text is a promise, so we'll need to extract that there
                  response.text().then((content) => Promise.reject(content))
            )
            .then((response) => {
              // if all goes well then we'll swap our target's html
              // with the server's response
              console.log("Success");
              target.innerHTML = response;
            })
            .catch((error) => {
              // you can reject promises with anything not just Errors so we'll check
              if (error instanceof Error) {
                errorTarget.textContent = error.message;
                return;
              }
              // if it was not an error then it is likely we're here due to our
              // handler above
              errorTarget.innerHTML = error;
            })
            .finally(() => {
              // hide the progress in any case
              progress.style.opacity = 0;
            });
        });
      });
    </script>
  </body>
</html>
```

Our client side HTML is as simple as we can get it, just a form and a few lines of JavaScript to avoid a page reload. The form submission will hit the `/uploads` endpoint in our server, this index file will be server by our server as well so no need to point to the full address (also to leave CORS out of the situation for the moment).

### Saturn Back-end

Fortunately our sample is so focused that it can be implemented in a file with around... 50-60 LoC

```fsharp
open System
open System.IO

open Microsoft.AspNetCore.Http

open Saturn
open Giraffe
open Giraffe.ViewEngine

// This is a templating function to produce HTML content that we'll use later on
let responseTemplate color content =
    // Giraffe.ViewEngine uses a tagElement  attribute list  tagElement list
    article
        [ _style $"color: %s{color}" ]
        [ header [] [
            // encodedText is a function that produces a string with encoded html,
            // this avoids sending raw html to the client from the server
            h3 [] [ encodedText "File Contents below:" ]
          ]
          pre [] [ encodedText content ]
        ]
// the index handler is quite simple, just send the html file which is located by
// the relative path to the current working directory of the server
let index next context = htmlFile "./index.html" next context

// This is our File Uploads Handler
let handler next (context: HttpContext) =
    task {
        // Saturn/Giraffe can also use aspnet's features directly
        let form = context.Request.Form
        // From the ASPNET's form abstraction we try to get a single file with the name
        // we provided, we also convert it to an option to handle potential null values
        let extractedFile = form.Files.GetFile "my-uploaded-file" |> Option.ofObj
        // if we wanted to extract multiple files we'd use something like form.Files.GetFiles "input's name attribute"

        match extractedFile with
        | Some file ->
            // if the file is present in the request, then we can do anything we want here
            // from validating size, extension, content type, etc., etc.

            // For our use case we'll create a disposable stream reader to get the text content of the file
            use reader = new StreamReader(file.OpenReadStream())
            // in our simple use case we'll just read the content into a single string
            let! content = reader.ReadToEndAsync()

            // we'll write the file to disk just as a sample
            // we could upload it to S3, Google Buckets, Azure Storage as well
            do! File.WriteAllTextAsync($"./{Guid.NewGuid()}.txt", content)

            // We received a file and we've "processed it" successfully
            let content = responseTemplate "green" content
            // send our HTML content to the client and that's it
            return! htmlView content next context
        | None ->
            // The file was not found in the request well return an error message
            let content = responseTemplate "tomato" "The file was not provided"
            // and also set our status code to 400 to signal it was a client error
            return! (setStatusCode 400 >> htmlView content) next context
    }

// lastly register our routes in the router
let appRouter =
    router {
        get "/" index
        get "/index.html" index
        post "/uploads" handler
    }

// create a server
let server = application { use_router appRouter }

// run it baby!
run server
```

That's it! Nothing too crazy I hope!

Let's put that into an F# project

```bash
# create the project
dotnet new console -lang F# -o saturn-sample
cd saturn-sample
# add the Saturn package
dotnet add package Saturn
# create the empty index file
touch index.html
```

Now copy and paste the contents of the previously provided client side HTML into the `index.html` file, then copy F# source code and replace the contents of the `Program.fs` file.
and start your app.

```bash
dotnet run
```

You should be able to visit `http://localhost:5000/` and start testing the file upload.

### Falco Back-End

Fortunately, the falco sample is not much different and also fits in a single file! we'll be using the same `index.html` that we shown above so we'll go straight to the F# source code for this.

```fsharp
open System
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Http

open Falco
open Falco.Routing
open Falco.HostBuilder
open Falco.Markup

// Similarly to the saturn sample
// we'll provide a templating function
// this one uses Falco.Markup though rather than Giraffe.ViewEngine
let responseTemplate color content =

    Elem.article
        [ Attr.style $"color: %s{color}" ]
        [ Elem.header [] [ Elem.h3 [] [ Text.enc "File Contents below:" ] ]
          Elem.pre [] [ Text.enc content ] ]

// equally to the previous sample we'll read the index file and return an html response
let index (context: HttpContext) : Task =
    task {
        // the only difference here is that we are able to pass the request aborted cancellation token
        let! content = File.ReadAllTextAsync("./index.html", context.RequestAborted)
        return! Response.ofHtmlString content context
    }

let handler context : Task =
    task {
        // Falco can also use aspnet's features directly
        // but offers an F# API for ease of use
        let! form = Request.streamForm context

        let extractedFile =
            // extract the file safely from the
            // IFormFileCollection in the http context
            form.Files
            |> Option.bind (fun form ->
                // try to extract the uploaded file named after the "name" attribute in html
                // GetFile returns null if no file is present, so we safely convert it into an optional value
                form.GetFile "my-uploaded-file" |> Option.ofObj)

        match extractedFile with
        | Some file ->
            // if the file is present in the request, then we can do anything we want here
            // from validating size, extension, content type, etc., etc.

            // For our use case we'll create a disposable stream reader to get the text content of the file
            use reader = new StreamReader(file.OpenReadStream())
            // in our simple use case we'll just read the content into a single string
            let! content = reader.ReadToEndAsync()

            // we'll write the file to disk just as a sample
            // we could upload it to S3, Google Buckets, Azure Storage as well
            do! File.WriteAllTextAsync($"./{Guid.NewGuid()}.txt", content)

            // We received a file and we've "processed it" successfully
            let content = responseTemplate "green" content
            // send our HTML content to the client and that's it
            return! Response.ofHtml content context
        | None ->
            // The file was not found in the request return something
            let content = responseTemplate "tomato" "The file was not provided"

            return! context |> Response.withStatusCode 400 |> Response.ofHtml content
    }


// declare the host and the endpoints
webHost [||] {
    endpoints
        [
          // handle the index routes as well
          get "/" index
          get "/index.html" index
          // our file endpoint handler as well
          post "/uploads" handler ]
}

```

Again, let's put that into an F# project

```bash
# create the project
dotnet new console -lang F# -o falco-sample
cd falco-sample
# add the Falco package
dotnet add package Falco
# create the empty index file
touch index.html
```

Now copy and paste the contents of the previously provided client side HTML into the `index.html` file, then copy F# source code and replace the contents of the `Program.fs` file.
and start your app.

```bash
dotnet run
```

You should be able to visit `http://localhost:5000/` and start testing the file upload.

---

Both backends in F# are virtually the same a few differences here and there might make it more appealing for some than others but the premise is the same, using F# should make it easy and safe regardless of your preferred style and we can see this in using the F# Optional type to avoid null reference exceptions which can bite hard in languages where null is the day to day bread.

Please note the usage of the `use` keyword in the samples, for disposable references (the ones that implement the `IDisposable` interface) F# can make the disposition automatically at the end of the scope when this keyword is used, otherwise you would need to manually call _reference_`.Dispose()` which can be easily forgotten so... avoid that and just let F# handle it for you!

I must mention that when you're dealing with file uploads you should avoid reading the contents into a string, this is for performance reasons mainly because rarely you actually need to inspect the contents of the file you usually pass it on to another file upload service like I mentioned in the sample, aws, gcloud, azure, you name it letting the code process a stream is better as you don't allocate the string in memory for un-needed reasons.

### Bonus HTMX

You may have heard of this [HTMX] thing... that supposedly makes it easier to deal with client side HTML + any existing back-end.

Well, let me tell you that it works wonders with F# as well!

to use HTMX we'll change our HTML a little bit:

```html
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Send a File to the Server</title>
    <!-- Include HTMX in your website -->
    <script src="https://unpkg.com/htmx.org@1.9.3"></script>
  </head>
  <body>
    <!-- HTMX uses hx-* attributes to indicate the behavior of the element that will send 
        an http request, in this case
        we're telling htmx to post a form-data request to the /uploads endpoint
        and when the server sends a response replace the inner html of the response target
        with the server's response
     -->
    <form
      id="htmx-form"
      hx-encoding="multipart/form-data"
      hx-post="/uploads"
      hx-target="#response-target"
      hx-swap="innerHtml"
    >
      <fieldset>
        <legend>HTMX Upload</legend>
        <input type="file" name="my-uploaded-file" />
        <button>Upload</button>
        <progress id="htmx-progress" value="0" max="100"></progress>
      </fieldset>
    </form>

    <section id="response-target"></section>
    <section id="error-target"></section>

    <!-- Closely to vanilla JS, but with a smoother API to work with -->
    <script>
      // given the element (specified by the selector) listen to the progress event in the
      // backing hxr request
      // crafting an xhr request manually is quite lengthy so that's why we used fetch in the plain
      // html example, htmx thankfully hides that for us and lets us focus on the omportant bits
      htmx.on("#htmx-form", "htmx:xhr:progress", function (evt) {
        htmx
          .find("#htmx-progress")
          .setAttribute("value", (evt.detail.loaded / evt.detail.total) * 100);
      });
      // if the backend sends an error we'll replace the contents of the error target instead
      htmx.on("htmx:responseError", (error) => {
        evt.detail.target = htmx.find("#error-target");
      });
    </script>
  </body>
</html>
```

You can replace the contents of the `index.html` file with that and it should work just as great, with the difference that we need to write a little bit of JS ourselves and we also get file upload progress as well!

### Closing thoughts

Hopefully this shows that F# is not an alien language and it is more similar than you think, no weird maths, no weird point free code. This is very close to what you would write in other languages like typescript given that they provide a framework similar to aspnet!

Does this help you at all?
Do you want more content like this?

Until the next time, you know where to find me!

- [@angelmunoz@misskey.cloud](https://misskey.cloud/@angelmunoz) - Fediverse presence (like mastodon).
- [@angel_d_munoz@threads.net](https://threads.net/@angel_d_munoz) - We're getting started here :P .
- [@angel_d_munoz](https://twitter.com/angel_d_munoz) - We'll take the beatings until the ship sinks or if we get bored.
