---
title: Targeting Node, Deno and Bun with F#
subtitle: ~
categories: deno, node, bun, fsharp
abstract: We'll be talking about how can we use the Fable Compiler to target different javascript runtimes besides the browser!
date: 2022-07-13
language: en
---

[bun.sh]: https://bun.sh
[deno.land]: https://deno.land
[nodejs]: https://nodejs.org
[fable compiler]: https://fable.io
[deno.land/std]: https://deno.land/std
[next.js]: https://github.com/oven-sh/bun#bun-create
[fable.node]: https://github.com/fable-compiler/fable-node
[fable.deno]: https://github.com/AngelMunoz/fable-bun
[fable.bun]: https://github.com/AngelMunoz/fable-bun
[glutinum]: https://github.com/glutinum-org/Glutinum
[perla]: https://github.com/AngelMunoz/Perla
[giraffe]: https://giraffe.wiki
[saturn]: https://saturnframework.org

Hello folks, here we are once again with more F#, this time we'll be talking about how can we use the [fable compiler] to target [nodejs], [bun.sh] and [deno.land].

As you may (or not) know by now if you have read some of my older posts fable lets you compile your F# code into modern web standards JavaScript this has a lot of advantages for modern runtimes like bun/deno which accept ES modules by default that means you don't need to further process your compiled JS code if not required it should just work!

## What is node, deno, and bun?

Over the past decade the JavaScript ecosystem grew exponentially and innovated in many areas that were missing for JavaScript, it allowed the language to modernize and to enable tooling for web applications as well as servers, people found out that sometimes it makes sense to be able to share the code that runs in the browser with the one that runs on the server, node, deno and bun precisely allow you to do that, they are JavaScript runtimes built on top of web browser engines like V8 (chromium) and WebKit (safari) although the server code is different from the client, there is always logic that can be shared between both parties be it validation, workflow execution and other cool stuff.

### [nodejs]

Until today... it is still the most used runtime to deploy server or desktop JavaScript it builds itself on top of chromium's V8 engine to power JavaScript code in a runtime similar yet different to the browser.

When node was getting started the JavaScript landscape was vastly different but node provided some niceties over browser JavaScript at the time, most notably for me the notion of modules, the format called commonjs caught the attention of many people who wanted to prove how applications were built there were other module systems at the time, amd, umd, system, etc but no one had a definitive solution, browserify was then built, webpack came to the scene, and a lot of tooling after (including Typescript, Babel, ES2015, and other niceties) here we are today, the node ecosystem is a beast on its own and with the support to ESModules the ecosystem is finally in the transition to a more web standards code which can allow better source code sharing among the browser and node itself.

### [deno.land]

As per the words taken from deno's landing page:

> Deno is a simple, modern and secure runtime for JavaScript, TypeScript, and WebAssembly that uses V8 and is built in Rust.

> - Provides web platform functionality and adopts web platform standards.
> - Secure by default. No file, network, or environment access, unless explicitly enabled.
> - Supports TypeScript out of the box.
> - Ships only a single executable file.
> - Has built-in development tooling like a dependency inspector (deno info) and a code formatter (deno fmt).
> - Has a set of reviewed (audited) standard modules that are guaranteed to work with Deno: [deno.land/std].

Deno (which is built by the same person who initially built node) is basically another take to node but with different philosophies in some areas, some of the most notable and already mentioned are typescript support out of the box, it also uses V8 and is built with rust. Unlike node, deno doesn't have package manager, rather than that deno leverages web standards where it can and in this case it uses URL imports in ESModules to import files and import maps to keep bare modules intact, this pairs nicely with CDNs like jspm, jsdelivr, skypack and deno's cdn as well.

### [Bun.sh]

Bun is the new player in the game and oh boi... what a player it is!

> Bun is a fast all-in-one JavaScript runtime
> Bundle, transpile, install and run JavaScript & TypeScript projects — all in Bun.
>
> - Bun is a new JavaScript runtime with a native bundler, transpiler, task runner and npm client built-in.

Bun aims to be compatible with node where it can, as well as being web standards driven (like deno) but it also takes lessons from the JavaScript ecosystem and tries to provide performant and efficient tooling it's like if you combined rollup/esbuild/npm/pnpm/yarn all in one.

One important bit is that Bun implements the node resolution algorithm which helps a lot bringing the existing node ecosystem into bun basically almost out of the box, in fact one of its advertising features is that you can run [Next.js] projects within bun without a hassle.

Also unlike deno and node, Bun preferred to use WebKit instead of V8 which seems to be faster in bun's benchmarks and well it is a very interesting prospect when you can tell folks "**_Hey! do tou want to make your node faster? Just run it in bun!_**"

#### Will node usage decline?

Now the creation of bun, and deno doesn't mean that node is going to die anytime soon, the idea alone is laughable. While these projects aim to solve similar problems, It depends how each project's developer audience uses them, that will make these projects favor more, less or different use cases.

Think about it for the moment, just think how many frameworks are out there yet most of then co-exist naturally and help each other out to improve, thankfully creating a JS runtime isn't as easy as writing yet another framework 🤣.

For us Developers though it adds more choices on the table, and that's good competition drives innovation. Given how each runtime relies more on web standards these innovations may end up in the standards and benefit everyone at the same time.

It also opens the possibility that code you write may be as agnostic as possible and run without modifications in different runtimes.

## Getting back to fsharp

Now what does this mean for the F# folks?

Depending on how you use F# it might not mean anything at all or it might mean leveraging the type safety and the power of F# to write safe code that will perform well in a multitude of runtimes be it lambda functions, web workers like cloudflare's, or simply leverage the excellent F# tooling to improve your codebase and take advantage of the well supported compilation JavaScript target.

We will use a simple console application for this case.

> **Note**: keep in mind that you should install node, deno, or bun depending which one you want to target I'll show the three runtimes but all of them are optional!

```sh
dotnet new console -lang F# -o fs-sample && cd fs-sample
dotnet new tool-manifest
dotnet tool install fable

# Let's built the app right away just to test it

dotnet fable -o dist
```

These commands should create and build, and compile JavaScript from the F# console application
inside the `dist/Program.js` file you will find a similar output to this:

```js
import {
  printf,
  toConsole,
} from "./fable_modules/fable-library.3.7.16/String.js";

toConsole(printf("Hello from F#"));
```

> You can run this file in the standard means of your runtime
>
> - **Node**: `node dist/Program.js`
>
> - **Bun**: `bun dist/Program.js`
>
> - **Deno**: `deno run dist/Program.js`
>
> **Note**: node requires a package.json file with the property `"type": "module"` to run without issues
>
> To add that just run npm init -y and add said property

At this point I can tell you:

"**_That's it, that's all you need to target JavaScript runtimes with F#_**"

Hopefully this is a reminder that Fable just outputs JavaScript , you can use the plain JavaScript as is in the runtimes that support ES2015 (and a few newer features) without the need for extra tooling like bundlers, and transpilers or similar tooling and as I've said before on other posts "_Wherever Web Standards JavaScript runs, F# code will run as well_"

There's a cool feature from fable when you use an `[<EntryPoint>]` attribute, let's change the `Program.fs` code to the following

```fsharp
[<EntryPoint>]
let main argv =
    printf "%A" argv
    0
```

after running once again `dotnet fable -o dist` the compiled output looks like this

```js
import {
  printf,
  toConsole,
} from "./fable_modules/fable-library.3.7.16/String.js";

(function (argv) {
  toConsole(printf("%A"))(argv);
  return 0;
})(typeof process === "object" ? process.argv.slice(2) : []);
```

> You can run this file in the standard means of your runtime
>
> - **Node**: `node dist/Program.js -- -a --b=c` - `--,-a,-b,asdasd`
>
> - **Bun**: `bun dist/Program.js -- -a --b=c` - `-a,-b,asdasd`
>
> - **Deno**: `deno run dist/Program.js -- -a --b=c` -

Deno doesn't output anything at all, and that's because Deno doesn't use `process.argv` like node and bun but rather `Deno.args` so that's one of the few differences you will find, also bun requires to escape the arguments via `--` otherwise it tries to parse them as if they were bun's cli arguments.

This entry point function might be useful for you depending what are you targeting and if you are looking forward to use the program's cli arguments.

### Packages

For Node and Bun the package story is the same, just run npm/pnpm/yarn/bun install and once packages are downloaded just run things with bun, although keep in mind that if you're calling a CLI tool that internally calls Node, it won't run in bun but node.

for Deno the story is slightly different, you can use an import map like this:

```json
{
  "imports": {
    "urlpattern-polyfill": "https://cdn.skypack.dev/pin/urlpattern-polyfill@v5.0.3-5dMKTgPBkStj8a3hiMD2/mode=imports,min/optimized/urlpattern-polyfill.js",
    "http": "https://deno.land/std@0.147.0/http/server.ts"
  }
}
```

which in turn allows you to do this in deno

```ts
import "urlpattern-polyfill";
// or
import { serve } from "http";
```

while these are not "packages" like the node/bun ones, they behave in the same way, deno applies cache techniques to allow offline usage as well so you don't depend on internet to import your dependencies at runtime.

Does that import map thing feel familiar? well maybe I spoke about that a few months ago when I wrote about a project of mine ([Perla]) which uses import maps to allow you to write Single Page Applications without node installed!

## Fable.Node Fable.Bun, Fable.Deno

What about specific APIs for node, deno and bun?

Well you're in luck if you want to target node because [Fable.Node] has been out for a while and since node is the most popular runtime in this list you'll even find bindings to projects like express via the [Glutinum] project which are high quality bindings with test suites to ensure things don't just break!

If you want the newer runtimes though... you'll have to wait for me to release the bindings for [fable.bun] and [fable.deno] that will allow you to target Bun and Deno's APIs

Now let's move to something more exciting than just a console

## Enter the Bix Experiment

With Both Bun and Deno out I really wanted to see if I could make something to test them out both runtimes offer HTTP servers that work with `Request` and `Response` which were introduced with the Fetch API in the browsers a few years ago

I have always wanted to make a JavaScript framework just to be part of the meme and as well to contribute back what the internet has given me for free over the years, this is where **Bix** comes in

**Bix** is a micro-framework designed with F# in mind and that runs on both Deno and Bun! and in theory it also should even run in a service worker! (intercepting fetch requests) although I haven't tested that yet, it offers a general purpose handler that coupled with a set of route definitions it can bring a [Giraffe]/[Saturn] like framework to life in JavaScript runtimes which is incredibly awesome! useful? maybe not 😅, but awesome indeed. Let's see some code for it

```fsharp
open Bix
open Bix.Types
open Bix.Handlers
open Bix.Router

open Bix.Bun

let checkCredentials: HttpHandler =
    fun next ctx ->
        let req: Request = ctx.Request
        let bearer = req.headers.get "Authorization" |> Option.ofObj
        // dummy handler
        match bearer with
        | None -> (setStatusCode (401) >=> sendText "Not Authorized") next ctx
        | Some token -> next ctx

let routes =
    Router.Empty
    // helper functions to define routes
    |> Router.get ("/", fun next ctx -> sendText "Hello, World!" next ctx)
    |> Router.get ("/posts/:slug", fun next ctx ->
        promise { // promise based handlers are supported
            let slug = ctx.PathParams "slug"
            let! post = Database.find slug // database from somewhere
            let! html = Views.renderPost post // views from somewhere
            return! sendHtml html next ctx
        }
    )
    |> Router.get ("/json", fun next ctx ->
        let content = {| name = "Bix Server!"; Date = System.DateTime.Now |}
        sendJson content next ctx
    )
    // route composition a'la suave/giraffe is supported
    |> Router.get ("/protected", (checkCredentials >=> (fun next ctx -> sendText "I'm protected!" next ctx)))

let server =
    Server.Empty
    |> Server.withRouter routes
    |> Server.withDevelopment true
    |> Server.withPort 5000
    |> Server.run

let mode =
    if server.development then
        "Development"
    else
        "Production"

printfn $"{mode} Server started at {server.hostname}"
```

For Deno it isn't much different

```fsharp
// open the Bix.Deno module
open Bix.Deno

Server.Empty
// you can use the same routes without changes!
|> Server.withRouter routes
|> Server.withDevelopment true
|> Server.withPort 5000
// the run function returns a promise in deno due how the std HTTP server works
|> Server.run
|> Promise.start
```

Bix provides some basic http handlers like returning json responses, set status codes, send html, and even send html files.

The most amazing (at least for me) about this is that... 90% - 95% of micro-framework code is shared code between both runtimes, the only thing that really changes is the `run` and the internal `Request` handler function which need to be different because of how the servers are started in both runtimes and that they are different in some areas, so we need to abstract some of these details away in order to make the rest of the framework re-usable between platforms.

If there's a `Request`/`Response` http server for node, be sure that it can be supported as well

If this peeks your interest then visit the project https://github.com/AngelMunoz/fable-bun there are slightly more complete samples there (including server side rendered endpoint using Feliz.ViewEngine) and give it a go, I'll try to start releasing the first previews over the next days/week but Feedback is super important here.

## Final Thoughts

Fable is a very powerful tool to make F# code, style and conciseness available almost everywhere via JavaScript (and soon other languages), I'm truly excited to see how bun, deno and node will grow together and improve to become really good assets in the software developer toolbelt.

Creating a framework was also fun, I can finally call myself a JavaScript developer now that I've built my own framework 😅 `/s` if you want to know more about how Bix internals work and how is everything abstracted to _just work_ in both deno and bun, feel free to let me know in the comments below or on twitter!

I'd be glad to write another piece specifically for that purpose
