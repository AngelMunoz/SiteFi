---
title: Mixing Google Cloud and F#
subtitle: ~
categories: fsharp, gcloud, fsadvent, dotnet
abstract: Today's topic is Google Cloud Platform and F#...
date: 2022-12-16
language: en
---

[google cloud platform]: https://cloud.google.com/
[google cloud functions]: https://github.com/GoogleCloudPlatform/functions-framework-dotnet
[amazon web services]: https://aws.amazon.com/blogs/developer/f-tooling-support-for-aws-lambda/
[dotnet]: https://get.dot.net
[google apis]: https://github.com/googleapis/google-cloud-dotnet
[aspnet core]: https://dotnet.microsoft.com/en-us/apps/aspnet
[pub/sub]: https://cloud.google.com/pubsub/
[#fsadvent]: https://sergeytihon.com/fsadvent/
[sergey tihon]: https://sergeytihon.com

> # This post is part of the [#FsAdvent] Calendar Thanks to [Sergey Tihon] for once again, organizing such an amazing event that brings many F# folks talking about the whole experience of using F# in a wide variety of use cases

Hello there folks, it has been a while!

I'm sorry for the lack of content and the lack of consistency (with both perla and the short F# video series I've been working with) but that's what happens when you only write for fun!

Today's topic is [Google Cloud Platform] and F#...

> Wait what? Is that even possible? I thought that F# is just for some niche scenarios and hobby apps... Well even if that was the case (it is not) it is a Microsoft language, so it should be an Azure only thing right?

Nope, it can run on [Amazon Web Services] as well!

I digress, while <abbr title="Google Cloud Platform">GCP</abbr> might not be the first option to pop up in your head when it comes to cloud and serverless (specially when we talk about [dotnet]), today that is a reality.

There are several libraries in dotnet for [google apis] which cover most of the GCP usage, but we will focus in [Google Cloud Functions] for dotnet.

> #### Disclaimer
>
> This blog entry is directed to those folks who are slightly familiar with gcloud, the more experience you have with it the better, but it should be fairly straight forward to follow (keeping in mind that anything that relates to the cloud is always a not so simple topic).
> Also, this is not a tutorial or a "how to" kind of article but it should give you some pointers to allow you get at least a function online and running.

## Before we start

While this is not a tutorial let's try to make a couple of functions just to get our feet wet

We a few things

- A Google Cloud Project

![Project Selection](https://imgur.com/PT1OLaq.png)

- You can create a new one if you don't have it

![Create a new project](https://imgur.com/1wfja0Q.png)

> **Note**: The project id will be very important as it will be used in the google cloud APIs and dotnet libraries.

- The GCloud CLI - https://cloud.google.com/sdk/gcloud/
- The dotnet templates for the functions framework - https://github.com/GoogleCloudPlatform/functions-framework-dotnet
  - `dotnet new -i Google.Cloud.Functions.Templates`

## Fun is for Functions

Let's go for the simplest one un-authenticated, http functions let's get started

```
PS C:\Users\scyth\repos> dotnet new gcf-http -o MyFunction
The template "Google Cloud Functions HttpFunction" was created successfully.
```

This will create a new function template:

```
Function.fs
MyFunction.fsproj
```

The `Function.fs` file looks like this

```fsharp
namespace MyFunction

open Google.Cloud.Functions.Framework
open Microsoft.AspNetCore.Http

type Function() =
    interface IHttpFunction with
        /// <summary>
        /// Logic for your function goes here.
        /// </summary>
        /// <param name="context">The HTTP context, containing the request and the response.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        member this.HandleAsync context =
            async {
                do! context.Response.WriteAsync "Hello, Functions Framework." |> Async.AwaitTask
            } |> Async.StartAsTask :> _
```

cloud functions run [aspnet core] under the hood, but since it is a controlled environment it was designed in a way that the only concern you should worry about is running your function so all of the usual boilerplate related to adding services, middleware or enabling features is hidden away from you which for most simple scenarios this is what you will need and in the case of F# where dependency injection is more related to simply use functions it might be enough.

let's talk about the handler, surely it looks quite... weird right? well yes, it is kind of weird because with an `async {}` <abbr title="Computation Expression">CE</abbr> requires that you convert tasks to async, and that's a whole complex topic which may even have encyclopedic material to discuss with, in the meantime we won't care too much about it and we can change our handler to the following form:

```fsharp
member this.HandleAsync context =
  task {
      do! context.Response.WriteAsync "Hello, Functions Framework."
  }
```

> **Note**: This is not an endorsement to switch all of the existing code in any other codebase from `async {}` to `task {}` both CEs have their usages and for this purpose since we're going to interface with many task based libraries (like GCloud dotnet APIs or aspnet's task based functions) and it makes sense to stay on the task side of things, for the most part `async {}` will be enough for most cases and it is still the [recommended default](https://learn.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts).

That being said, let's continue with the function, the context parameter is an `HttpContext` object which means you have access to the `Request` and `Response` to it, you can easily extract information like the request's path, query string, it's body and other useful stuff. let's change our function to accept some json content and return some json as well.

To avoid cluttering our function body too much we will add a `Json` module that will handle deserialization via `System.Text.Json` although, feel free to use whatever json deserialization strategy you may want to go with, like `Thoth.Json.Net`.

```fsharp
namespace MyFunction

open Google.Cloud.Functions.Framework
open Microsoft.AspNetCore.Http

module Json =
  open System.IO
  open System.Text.Json

  let tryDeserialize<'Type> (body: Stream) =
    task {
      try
        let! result = JsonSerializer.DeserializeAsync<'Type>(body)

        return Ok result
      with
      | ex -> return Error ex.Message
    }

type Function() =
  interface IHttpFunction with

    member this.HandleAsync context =
      task {
        let! content = Json.tryDeserialize<{| numbers: int array |}> (context.Request.Body)

        match content with
        | Ok value ->
          let total = Array.sum value.numbers
          printfn $"Sum of the numbers is {total}"
          return! context.Response.WriteAsJsonAsync({| total = total |})
        | Error err ->
          printfn $"There were no numbers :( {err}"
          context.Response.StatusCode <- 400
          return! context.Response.WriteAsJsonAsync({| total = 0 |})
      }
```

Great, we now have a deserialized body and depending on what happened we can decide whether we can continue or not or if we want to tell the consumer that they didn't do what it was expected, the sky's the limit here, let's try it!

```
PS C:\Users\scyth\repos\MyFunction> dotnet run
2022-11-26T06:11:45.606Z [Google.Cloud.Functions.Hosting.EntryPoint] [info] Serving function MyFunction.Function
2022-11-26T06:11:45.720Z [Microsoft.Hosting.Lifetime] [info] Now listening on: http://127.0.0.1:8080
2022-11-26T06:11:45.721Z [Microsoft.Hosting.Lifetime] [info] Application started. Press Ctrl+C to shut down.
2022-11-26T06:11:45.722Z [Microsoft.Hosting.Lifetime] [info] Hosting environment: Production
2022-11-26T06:11:45.722Z [Microsoft.Hosting.Lifetime] [info] Content root path: C:\Users\scyth\repos\MyFunction
```

The first few lines are what we would normally expect when we run an asp net app locally via `dotnet run` just logs and the URL where the server is listening, I will do a bad request and a good request via postman, but feel free to use your favorite tool for this

```
[info] Request starting HTTP/1.1 GET http://127.0.0.1:8080/ - -
Scopes: [{ RequestId: 0HMMFEKIDG1IQ:00000002, RequestPath: / }]
There were no numbers :( The input does not contain any JSON tokens. Expected the input to start with a valid JSON token, when isFinalBlock is true. Path: $ | LineNumber: 0 | BytePositionInLine: 0.
[info] Request finished HTTP/1.1 GET http://127.0.0.1:8080/ - - - 400 - application/json;+charset=utf-8 239.0244ms
```

In the case above I didn't send any json body in the request and it shows our logs as well as asp.net ones.

```
Scopes: [{ RequestId: 0HMMFEKIDG1IQ:00000002, RequestPath: / }]
[info] Request starting HTTP/1.1 GET http://127.0.0.1:8080/ application/json;+charset=utf-8 32
Scopes: [{ RequestId: 0HMMFEKIDG1IQ:00000003, RequestPath: / }]
Sum of the numbers is 45
[info] Request finished HTTP/1.1 GET http://127.0.0.1:8080/ application/json;+charset=utf-8 32 - 200 - application/json;+charset=utf-8 30.8089ms
Scopes: [{ RequestId: 0HMMFEKIDG1IQ:00000003, RequestPath: / }]
```

with a little bit of date cleaning you can see in the logs that logs are there which is nice but, hold on a minute... they don't look as nice as the rest of the logs, perhaps that could be a problem with google cloud monitoring tools

Well one of the good things of asp.net is that it has a ton of stuff out fo the box and it can be included via dependency injection, which in this case we can use our `Function` constructor to bring things like an `ILogger<Function>`, `IConfiguration`, and other services that come out of the box from aspnet
let's change our code a little bit

```fsharp
namespace MyFunction

open Google.Cloud.Functions.Framework
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging // This line got added

module Json = // ... omit for brevity ...

// Notice
type Function(logger: ILogger<Function>) =
  interface IHttpFunction with

    member this.HandleAsync context =
      task {
        let! content = Json.tryDeserialize<{| numbers: int array |}> (context.Request.Body)

        match content with
        | Ok value ->
          let total = Array.sum value.numbers
          // switch from printfn to logger
          logger.LogInformation $"Sum of the numbers is {total}"
          return! context.Response.WriteAsJsonAsync({| total = total |})
        | Error err ->
          // switch from printfn to logger
          logger.LogInformation $"There were no numbers :( {err}"
          context.Response.StatusCode <- 400
          return! context.Response.WriteAsJsonAsync({| total = 0 |})
      }
```

Now our log entries will look more like

```
2022-11-26T06:22:08.838Z [MyFunction.Function] [info] Sum of the numbers is 45
```

```
2022-11-26T06:24:18.960Z [MyFunction.Function] [info] There were no numbers :( The input does not contain any JSON tokens. Expected the input to start with a valid JSON token, when isFinalBlock is true. Path: $ | LineNumber: 0 | BytePositionInLine: 0.
```

This will be very helpful to diagnose things once you deploy your function to the cloud, as it is simple to run and debug locally but it is not so simple once things are living in the cloud, logs are very important thing to get right and help yourself with future issues where your code may not be behaving as you would expect

> **Note**: There are further customizations you can do to the startup sequence if you want for example add services via the asp.net's dependency injection mechanisms, you can view how [in this document](https://github.com/GoogleCloudPlatform/functions-framework-dotnet/blob/9c21f17e944a0516d15eea83d1c4e62b266d7e17/docs/customization.md).

Great! Now we have our local function that we run, and we know it works time to deploy!

### Deployment

Deploying via the CLI is I believe relatively simple, it takes a single command albeit, with a bunch of arguments but nothing more than that.

```
gcloud functions deploy my-function-name-in-gcp-console \
  --region us-west1 \
  --runtime dotnet6 \
  --trigger-http \
  --allow-unauthenticated \
  --entry-point MyFunction.Function
```

> **Note**: Please ensure that you have initialized the gcloud cli (`gcloud init`) and that you have selected the project you created earlier.

- `gcloud functions deploy my-function-name-in-gcp-console`

  The first argument is the name of your function, this name will be the one that will show in the google cloud functions console and used as an identifier for your function (each new deploy will increment a version of that function rather than deploy a new function to a different endpoint)

- `--region us-west1`

  Where is this function getting deployed in the cloud? In my case, us-west1 is the assigned region.

- `--runtime dotnet6`

  Select the runtime to run this function with, since dotnet6 is the most recent LTS release at the time of writing, it makes sense to select it.

- `--trigger-http`

  Let GCP know that you want this function to be invoked via HTTP (rather than an Event like those from storage, firestore, or pub/sub)

- `--allow-unauthenticated`

  In this case you are not using anything related to IAM access within the gcloud network so we'll keep it simple and allow anyone to hit our endpoint

- `--entry-point MyFunction.Function`

  This is the "full namespace path" to the class that implements the `IHttpFunction` interface it doesn't have to me named `Function` but for our purposes it can stay like that.

- `--gen2`

  This is not listed here but I will mention it anyways because the following screenshot is using gcloud functions gen2 so your screen might be different than mine, but gen2 functions are still in preview so take it with a grain of salt.

If all went well you should see a screen like this once you selected your function in the gcloud console

![Test View for GCloud Function](https://imgur.com/LCJJK29.png)

You can monitor logs and also see what the function answers from there.

## Next Steps

Great! hopefully the process of creating a new google cloud function, developing one and finally deploying it, I think in a best case scenario you are likely doing the full process in around 30 minutes just because the gcloud cli takes a lot of time to install 🤣.

But things in real life are not always that simple when it comes to projects, so are there other things worth having in mind? Yes let's see

### NuGet dependencies

Since GCloud functions run in a controlled environment, they use conventions for deployments and other stuff if you are using a package manager like `paket` you may need to fall back to NuGet because the deployment process can't be customized as far as I know, so keep that in mind.

Other than that you can use `dotnet add package My.Dependency --version 1.10.0` normally and dependencies install just right.

### Project References

Referencing other projects like a shared library that has core types, validations and other business rules sometimes is a must because this code could be used by different projects in an organization this can prove problematic when you need to deploy something because your project structure now contains more than one project. Example:

```
README.md
src/
  Lib/
    ...
    Types.fs
    Lib.fsproj
  ProjectA/
    ...
    ProjectA.fsproj
  FunctionProject/
    Function.fs
    FunctionProject.fsproj
```

The idea now would be to deploy from src rather than from `src/FunctionProject` but if you do that just like that your project won't work as it won't be able to find `Lib/Lib.fsproj` which is not great, you have to change your deployment command and add the following

```
gcloud functions deploy my-function-name \
... other arguments ...
--set-build-env-vars=GOOGLE_BUILDABLE=FunctionProject
```

- `--set-build-env-vars=GOOGLE_BUILDABLE=FunctionProject`

  If you set this env var `GOOGLE_BUILDABLE` and point it to the directory of the project you want to build (that has project references) if will ensure to grab the sources if the current directory and build that project in the cloud to ensure it has what it needs to build correctly (You can find more about it in [this document](https://github.com/GoogleCloudPlatform/functions-framework-dotnet/blob/9c21f17e944a0516d15eea83d1c4e62b266d7e17/docs/examples.md#multiprojectfunction-and-multiprojectdependency)).

### .gcloudignore and .gitignore

Don't forget to include both of these files otherwise you will end up uploading the whole sources + build artifacts to gcloud which is dead space you don't want to pay for!
An example of `.gcloudignore`

```
# let gcloiud ignore this file 😅
.gcloudignore

# If you would like to upload your .git directory, .gitignore file or files
# from your .gitignore file, remove the corresponding line
# below:
.git
.gitignore

node_modules
# include gitignore contents at this point
#!include:.gitignore

# Ensure the production appsettings is there
!appsettings.Production.json
# ignore other appsetting files

appsettings.*.json
# ... the rest of your ignores for gcloud ...
```

the `.gitignore` file can be created using `dotnet new gitignore` and both files complement each other (they must be side by side).

### Working with GCLoud APIs with Auth

Some APIs like firestore, require you to have a set of credentials available in the environment, for the SDK to make authenticated requests to the GCP API, we can simplify by generating the default project credentials

```
gcloud auth application-default login
```

This will generate a set of credentials based on the default project your gcloud CLI is currently running

- Linux, macOS: `$HOME/.config/gcloud/application_default_credentials.json`
- Windows: `%APPDATA%\gcloud\application_default_credentials.json`

> **_NOTE_**: Please ensure that your default project is not a **_PRODUCTION_** one! otherwise you may end up developing with the wrong data.
> [More info about this in the documentation](https://cloud.google.com/docs/authentication/application-default-credentials).

## The good

I think the dotnet team in google cloud has nailed the experience for the most part, it is simple it follows what one would expect from dotnet-land libraries adding a whole set of functions that talk to each other via gcloud pub/sub + http functions can be a powerful combo for serverless, and the free tier is very generous.

F# is not the only supported language of course you can also write functions in C# and VB, and guess what? they all have the same examples in the dotnet cloud functions framework yes including F# and VB isn't that crazy!?
...Once again third-party companies show that you can support template for languages other than `C#` and that is a very welcome thing from me I love to see that you get to decide what do tool do you want to use.

## The bad

The documentation is sometimes a little too vague, you have to jump from section to section in hopes to find what you're looking for and just to be able to find it but most likely with C# samples, if you're a VB or an F# dev, then you have to have that mental overhead of translating that code from C# to VB or F# which is not desirable by any means, however I found out that the most important bits of information were in the GitHub markdown files rather than in the gcloud documentation. Arguably, this is because dotnet is basically a recent addition and probably most of the work and efforts were directed into provide a smooth developer experience and filling the gaps here and there when it comes to the docs.

## The ugly

This is not a problem of functions _per-se_ more like a gcloud libraries problem, most of the libraries do have certain preference for C# based designs, that means that in cases like `firestore` you will have to create classes rather than records, that or if you're using just primitives, records with `CLIMutable` attribute... You can't win them all I guess regardless, I think that's something we can live with as F# class and other OOP features support is great as well, it simply isn't that common but is not the end of the world!

## Demo project

I made a sample project that you can use as a reference and explore that goes a little further than the hello world project style (not too far though)

- [https://github.com/AngelMunoz/FsFediverseArchive](https://github.com/AngelMunoz/FsFediverseArchive)

In this project you will be able to see a few things

- A Shared library

  that contains types, and Thoth.Json encoders/decoders to have a unified API for json serialization/deserialization

- An Http Function

  that works as a webhook for one of the social media websites I'm part of

- An Event function

  that gets triggered when a [Pub/Sub] topic is fired (from the webhook function)

- An Http Function

  that shows the public notes and replies I've made or received, these notes were saved in firestore in the event function

While it's not the most complex project you'll ever see

You can see the last function in action in this website: [https://fediverse.tunaxor.me](https://fediverse.tunaxor.me)

## Closing thoughts

Once again F# proves that it can be a powerful tool that is not necessarily tied to run specifically with other Microsoft products like Windows or Azure. If you are in a situation that for some reason you have to run serverless in google cloud, don't fret! F# is there to help you if you need it!

Also making the FsFediverseArchive project made me realize that cloud, serverless and projects of a similar theme are very complex in nature, when your things are local everything works just fine and dandy, but when things get to the cloud... That is not always the case there is confusion, missing logs, code behaving weird and no obvious root cause... But all in all, things can be done and there's certain level of testing and integration with emulators and such tooling that can help so the road may be dark, but there's almost always a light at the end that guides us.

So, what do you think? Please give it a shot and let me know if the process is as simple and as smooth as I felt it was when I did it for the first time, it would be nice to hear where are the gotchas and issues so we can help each other out there :)

You know where to find me!

- [@angelmunoz@misskey.cloud](https://misskey.cloud/@angelmunoz) - Fediverse presence (like mastodon).
- [@angel_d_munoz](https://twitter.com/angel_d_munoz) - We'll take the beatings until the ship sinks or if we get bored.
