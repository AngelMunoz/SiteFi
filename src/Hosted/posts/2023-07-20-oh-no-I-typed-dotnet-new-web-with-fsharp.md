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
_You just followed the instructions at the microsoft documentation?_

No worries that happens more often than you think.

> All of the code for this post can be found [here](https://github.com/AngelMunoz/dotnet-new-web-fsharp)

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

It turns out that in C# the compiler and tooling is built in a way that `Func<T>`, `Action<T>`, and friends can be written transparently (e.g. `(string name) => Results.Ok(name)`) there, F# functions (`FSharpFunc<T>`, `fun (name: string) -> Results.Ok(name)`) on the other hand while syntactically similar they are not a 1-1 replacement for those types in the dotnet runtime and there has to be a translation the F# idiom to the C# idiom.

In our sample above MapGet is part of a route builder meaning that it always returns an instance of `RouteHandlerBuilder` in C# there's no warning for discarded values so it doesn't show anything, in F# to avoid that warning we explicitly ignore the value.

> **_Note_**: In the case of the `|> ignore<'T>` (you don't see the `'T` because the compiler provides type inference) this is a feature that doesn't let you simply discard return > values from an expression, you have to explicitly ignore them or live with a warning. Take into account the following JavaScript code
>
> ```js
> function addProp(obj, propName, value) {
>   // The previous dev who worked here
>   // Mutated the object, it didn't creae a new one
>   // Not even god remembers why, he left the company 2 years ago
>   obj[propName] = value;
>   return obj;
> }
> // create a person with a name
> const person = { name: "Frank" };
>
> // add the age property to the person use the same reference
> addProp(person, "age", 30);
>
> // someone else decided to apply object destructuring
> const { name, age, job } = addProp(person, "job", "Developer");
> ```
>
> Now that the original dev is not around, we're not sure what was the true intent of the function was it to mutate the object only? was it intended to create copies and use the resulting value? no idea but If you're a relatively seasoned JS dev, you know mutating an object within a function may lead to unexpected code paths yet > the editor/tooling won't complain about it, it is usually you or a co-worker that finds out because they got a jira ticket to fix something out.
>
> For F# this means that it will produce a warning in the build logs as well as the editor that there's a function call that returns a value that is not being used (the first function call in the previous example) it won't complain about the second function call because you're actually using the function's return value and since most objects are immutable in F# that kind of code that mutates an instance and returns it probably wouldn't compile unless it is a classic dotnet object.

**Ok, but is that actually bad?**

You might be wondering if those annoyances might limit how your F# code behaves or what can be done with it and the reality is that no that's not really a big issue in my opinion it is not bad it is just annoying, as you have to type more characters and it introduces more noise to the code, you certainly notice that C# gets more love but that's not a big deal, you can still write your code and it will work just fine.

At this point you might start wondering if there's an easier with less friction path to work with F# and asp.net, the answer is: **_Sure!_** there are actually other options you can check out like:

- [Falco] - A functional web framework for F# that sits on top of aspnet.
- [Giraffe] - A functional web framework for F# that sits on top of aspnet.
- [Saturn] - A functional web take on MVC built on top of giraffe

both Falco and Giraffe are very similar but they offer different tools to work with they are not a 1-1 equivalent at the userland code level they are both worth looking at for you to evaluate.

Saturn is built on top of the giraffe abstractions for routes and function composition with a few helpers that provide a MVC-like smooth yet functional abstraction, it is more opinionated but also worth looking at.

---

Before we surrender to the F# idiomatic gods, I find myself in the position to tell you that you can still use the minimal api features and build your web application and that you can actually make it simpler to integrate things like swagger and open api documentation which is currently something the F# tailored solutions may not be as simple to add.

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

If we follow the pattern of keeping our endpoints in a module and registering them at the main function we can keep our code organized with minimal effort.

**_But... How do we scale?_**

One thing I've heard before if you come from languages like Javascript/Typescript where express apps or Python's Flask apps blow up to hell due to the "micro-framework" focus of using just functions and routes

Some believe that by using only function handlers you set up yourself for spaghetti code with an untelligible mess.

This in my opinion is more of an architectural level problem rather than one at the application/framework level, you can still use the same patterns you use in other languages to keep your code organized such as dependency injection. In asp.net DI is built into the framework so you can use it out of the box. In the example above we actually injected a logger into our handler and the only thing we had to do was to add the correct signature plus the type annotation in the route registration.

If we add more services to our application, we can rely in the function parameters rather than closures to access services that may live in a module.

That being said, if you're still not convinced, we can also add _controller_ endpoints which might be more reminiscent to controllers in frameworks like ruby on rails, laravel, django, etc which are often associated to "bigger" applications.

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
// DI in this case is applied at the controller's constructor level
// but... at the same time we're using a closure-like access to the handler's dependencies c:
// which was not present in the minimal example.
type UploadsController(logger: ILogger<UploadsController>) =
    inherit ControllerBase()

    // This attribute is used to specify the route otherwise it is taken from the method name
    [<HttpPost("user-avatar")>]
      // these are similar to the minimal api builder methods, they are used to specify
      // the content type of the request and response
    [<Consumes("multipart/form-data")>]
    [<ProducesResponseType(StatusCodes.Status204NoContent)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
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

// Guess what!?
// We don't need to get rid of our minimal handlers!
[<RequireQualifiedAccess>]
module MinimalHandlers = (* ... *)

// namespaces cannot contain values (let bindings)
// we have to use a module wrapper to contain our main function
module Program =

    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)
        // add the required services for the controllers to work
        builder.Services.AddControllers() |> ignore

        let app = builder.Build()

        // as an example, we can keep our minimal api handlers
        // plus the controller route handlers!
        MinimalHandlers.register app
        // ProductHandlers.register app
        // ProductHandlers.register app

        // Ensure that the controller routing middleware is added
        app.MapControllers() |> ignore

        app.Run()

        0 // Exit code
```

With this, we also have convention based controllers that build in years of battle tested asp.net framework patterns, and minimal apis which are somewhat a _newer_ and more lightweight concept that internally builds on top of the same conventions for regular asp.net apps.

In my personal and biased opinion, controllers are a little bit more verbose and feel heavier than minimal apis, so I tend to favor the later but they are also more familiar to people coming from other languages

In the end it is a matter of preference and how comfortable are you with the code you're writing.

### So... what's next?

Getting started with your project of course! you can start adding more routes, or switching to the F# frameworks we've already mentioned or... hear me out...

- Just Keep using standard asp.net!

If you got to this point and felt comfortable using plain asp.net then there's nothing wrong with that, perhaps even better as you may be able to provide feedback to the asp.net team on how to improve the experience for F# developers.

### Closing Thoughts

A last word of advice, while you're learning this new F# thing with web servers you'll find that there's a ton of C# tailored documentation for things like Controllers and minimal APIs while the F# content in this regard is scarce. The main reason this happens is that most of the F# comunity doesn't want to deal with the friction of using C# idioms in F# code so they build their own solutions. Thus you're likely going to find more information by switching to an F# tailored solution.

I'd suggest that before you fully commit to something, make a couple of toy projects to test the waters and follow your gut, which one felt better and go from there! eventually you'll end up with something that you like and that works for you with the added knowledge of asp.net under the hood.

This is also not a definitive guide and I hope that you don't get discouraged from trying F# if for some reason you stumble upon the **_Official_** documentation and you end up doing something that may or may not be what your friends/twitter/reddit/discord folks were telling you about.

It is likely that when you try these minimal api things out you start feeling the friction between plain aspnet and F# so you will likely reach for a more F# tailored solution.

You know where to find me!

- [@angelmunoz@misskey.cloud](https://misskey.cloud/@angelmunoz) - Fediverse presence (like mastodon).
- [@angel_d_munoz@threads.net](https://threads.net/@angel_d_munoz) - We're getting started here :P .
