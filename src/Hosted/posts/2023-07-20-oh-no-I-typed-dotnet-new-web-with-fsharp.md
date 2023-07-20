---
title: Oh no, I typed 'dotnet new web' with F#!
subtitle: ~
categories: fsharp, dotnet, webdev, aspnet
abstract: What!? That a friend of yours was insisting that you use F# for your next project, and you just wanted to get it over with?...
date: 2023-07-20
language: en
---

[giraffe]: https://giraffe.wiki
[falco]: https://www.falcoframework.com/
[saturn]: https://saturnframework.org

Oh Jesus... not this again... please don't tell me that you just typed:

- `dotnet new web -lang F# -o MyProject`

What!? that _a friend of yours was insisting that you use F# for your next project, and you just wanted to get it over with?_
Let me guess, _he didn't even tell you that there are better*™* web frameworks for F#..._ like [Giraffe], [Saturn], and [Falco] among others?
_You just followed the instructions at the microsoft documentation?_ No worries that happens more often than you think.

---

If that sounds like too dramatic for you, let me tell you I've seen it before online, I've heard it in person and even in my own head.

When we insist to our friends to try out F# sometimes we actually don't expect them to try F# (because of reasons) and we don't give a path or indication to how to get started with the _"True F# experience™"_ other than "You should try F# it's an awesome language".

In the F# online communities there are people who will tell you to avoid Microsoft at all costs, others will tell you that there are F# tailored solutions by the community and others won't care at all, it's your code not theirs.

But if for some particular reason you stumbled into the microsoft docs and you followed the instructions to create a new web project with F# you may have typed that command above and you may be wondering what to do next.

### dotnet new web...

When you run that command you'll get a directory structure like this:

```
MyProject
├── appsettings.Development.json
├── appsettings.json
├── Program.fs
├── MyProject.fsproj
```

This is what is known as a _Minimal API_ in dotnet it is an aspnet feature that introduces a simpler way to get started without too much ceremony for C# projects that also happens to work with F#.

The `Program.fs` file is the entry point of your application and should look somewhat like the following

```fsharp
open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore

    app.Run()

    0 // Exit code
```

It has the standard main function with a web application builder, an example to define a single route and start the application, it is basically a _hello world_ web application.

Buf you will notice straight away there are some weird things around, like the `Func<string>(fun () -> "Hello World!")` part why not just pass a function directly there? also what's the `|> ignore` thing doing? why are we ignoring that though?

It turns out that in C# the compiler and tooling is build in a way that `Func<T>`, `Action<T>`, and friends can be written without specifying it there, F# functions (`FSharpFunc<T>`) on the other hand are not a 1-1 equivalent for those types thus there has to be a way to translate the F# idiom to the C# idiom.

F# also has this feature that doesn't let you simply discard return values from an expression, you have to explicitly ignore them. Take into account the following JavaScript code

```js
function sayHello(msg) {
  console.log("Hello World!", msg);
  return "Hello World!" + msg;
}

// here we use it to print out to the console
sayHello("F#");
// here we use it to assing the value to a const
const allTogether = sayHello("F#");
```

You've seen code like this in your language of choice before and no one cares too much because the code works as they intend it to work when they consume it.

F# will tell you with a warning that there's a function that returns a value that you're not using this in the F# world is very helpful as it helps you to define what do you actually want to do with the function and the data that comes in.

In our sample above MapGet is part of a route builder meaning that it always returns an instance of `RouteHandlerBuilder` in C# there's no warning for discarded values so it doesn't show anything, in F# to avoid that warning we explicitly ignore the value.

**Ok, but is that actually bad?**

Nope, it is not bad it is just annoying, as you have to type more characters and it introduces more noise to the code, but it is not inherently bad.

Before we surrender to the F# idiomatic gods, let's try to come up with code structure that hopefully will keep things bearable.

```fsharp
// Let's add the required namespaces
open System
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http

// Rather than using functions inlined in the MapGet call we can define them
// as private functions in a dedicated module
// we use RequireQualifiedAccess to force us to use the module name
// when calling the functions
[<RequireQualifiedAccess>]
module MinimalHandlers =
    // dummy type to be able to resolve the logger
    type UploadAvatar =
          interface
          end

    // F# functions are public by default so if we want some sort of encapsulation here
    // (which is not necessary) we have to add the "private" keyword after "let"
    let private indexHandler () = "Hello World!"

    // here's a sample of a handler for file uploads
    // which in reallity is what we care about when we write our endpoints
    // the boilerplate and other things can be left out in a more localized place
    // here we can focus specifically on the request and response
    let private uploadAvatar (context: HttpContext) (logger: ILogger<UploadAvatar>) =
        task {
            logger.LogInformation "uploadAvatar Got Called"

            let! form = context.Request.ReadFormAsync(context.RequestAborted)

            let userAvatar = form.Files.GetFile "user-avatar" |> Option.ofObj

            match userAvatar with
            | Some file ->
                if file.ContentType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase) then
                    // do what you want with the file
                    return Results.NoContent()
                else
                    // if is not an image then we don't want it, tell the client it is a bad request
                    return Results.BadRequest("The file must be an image")

                    // no file means no avatar, and we will tell the client it is a bad request
            | None -> return Results.BadRequest("The request must contain a file")
        }
    // Now this is our public function, it takes a web application from the aspnet's
    // builder and registers the routes we want
    let register (app: WebApplication) =
        app
            // remember that (fun () -> "Hello World!") thing?
            // well we can now just pass the function directly
            // which adds a little bit of indirection but it makes it easier to read
            .MapGet("/", Func<string>(indexHandler))
             // Before we "ignore" the endpoint builder, we can also enjoy aspnet's features
             // which are very useful if you want swagger/open api documentation built for you
            .Produces(StatusCodes.Status200OK, "text/plain")
        |> ignore

        app
            .MapPost("/uploads", Func<HttpContext, ILogger<UploadAvatar>, Task<IResult>>(uploadAvatar))
            .Accepts("multipart/form-data")
            .Produces(StatusCodes.Status204NoContent, "text/plain")
            .ProducesProblem(StatusCodes.Status400BadRequest, "text/plain")
        |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    let app = builder.Build()

    // Here's where we register our handlers
    MinimalHandlers.register app
    // if we have more modules we can repeat this pattern
    // and our app is suddenly less annoying than what it got started with
    // examples could be:
    //
    // AuthHandlers.register app
    //
    // ProductHandlers.register app
    app.Run()

    0 // Exit code
```

Well now that we have something in place it isn't that bad is it? It could be better or a little bit more streamlined but this is just enough to keep that annoying friend of ours that keeps telling us to use F# away from our backs for a while.

### But... How do we scale?

One thing I've heard before if you come from languages like Javascript/Typescript where express apps or Python's Flask apps blow up to hell due to the "micro-framework" focus (i.e. using just functions and routes) is that just function handlers are not good to scale the app size, while I disagree with that statement I can see where it comes from, in the case of F# as you saw above, you could keep adding modules with handlers, registering them at the main function and you should be good to go, minimal API functions can work nicely with dependency injection and other cool features built into aspnet which help with application's growth.

But if you're still not convinced, we can also add _controller_ endpoints which might be more reminiscent to controllers in frameworks like ruby on rails, laravel, django, etc which are often associated to "bigger" applications.

To do that we'll have to make some changes to our single file

```fsharp
// First we need to add the namespace
// otherwise controllers are not found
namespace PlainWeb

open System
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http

// We have to use the ApiController + Route attributes
// to enable aspnets conventions for controllers
[<ApiController>]
[<Route("[controller]")>]
// we can use dependency injection directly
type UploadsController(logger: ILogger<UploadsController>) =
    inherit ControllerBase()

    // This attribute is used to specify the route otherwise it is taken from the method name
    [<HttpPost("user-avatar");
      // these are similar to the minimal api builder methods, they are used to specify
      // the content type of the request and response
      Consumes("multipart/form-data");
      ProducesResponseType(StatusCodes.Status204NoContent);
      ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member ctrl.UserAvatar(avatar: IFormFile) : Task<IActionResult> =
        task {
            logger.LogInformation("UserAvatar Got Called")

            // File instance is obtained by the name of the file in the form data
            // in this case our file was named "avatar" in the client request
            // we also need to use Option.ofObj to convert the potential null value to an option
            let avatar = avatar |> Option.ofObj

            match avatar with
            | None -> return ctrl.BadRequest("The request must contain a file")
            | Some avatar ->

                if avatar.ContentType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase) then
                    // do what you want with the file
                    return ctrl.NoContent()
                else
                    return ctrl.BadRequest("The file must be an image")
        }

[<RequireQualifiedAccess>]
module MinimalHandlers = (* ... *)

// namespaces cannot contain values (e.g. let mything = ) so we have to use a module wrapper
// to contain our main function
module Program =

    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)
        // add the required services for the controllers to work
        builder.Services.AddControllers() |> ignore

        let app = builder.Build()

        MinimalHandlers.register app
        // ProductHandlers.register app
        // ProductHandlers.register app

        // ensure that the controller routing middleware is added
        app.MapControllers() |> ignore

        app.Run()

        0 // Exit code
```

With this, we now have Controllers that are convention based, and minimal apis that are a little bit more explicit both are valid ways to build web applications in F# and aspnet and this also includes "bigger" applications, you can mix and match them as you see fit.

In my personal and biased opinion, controllers are a little bit more verbose than minimal apis, but they are also more familiar to people coming from other languages, so it is a matter of preference and what you want to do with your application.

### So... what's next?

Getting started with your project of course! you can start adding more routes, or even adding another library like the ones mentioned at the beginning of this post or... hear me out:

- Keep using standard aspnet features!

While you're learning this new F# thing with web servers you'll find that there's a ton of C# tailored documentation for things like Web API Controllers so I'd suggest that you can keep learning it with that until you feel comfortable enough to try out the other frameworks and F# tailored libraries. In the end they all build on top of aspnet (except maybe for a few select choices) so what you learn here will also be useful there.

This is also not a definitive guide and I hope that you don't get discouraged from trying F# if for some reason you stumble upon the **_Official_** documentation and you end up doing something that may or may not be what your friends/twitter/reddit/discord folks were telling you about.

It is likely that when you try these minimal api things out you start feeling the friction between plain aspnet and F# so you will likely reach for a more F# tailored solution.

You know where to find me!

- [@angelmunoz@misskey.cloud](https://misskey.cloud/@angelmunoz) - Fediverse presence (like mastodon).
- [@angel_d_munoz@threads.net](https://threads.net/@angel_d_munoz) - We're getting started here :P .
- [@angel_d_munoz](https://twitter.com/angel_d_munoz) - We'll take the beatings until the ship sinks or if we get bored.
