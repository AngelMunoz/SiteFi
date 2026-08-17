---
title: Building a Webpack alternative in F#
subtitle: ~
categories: fsharp,fsadvent,dotnet,showdev
abstract: Building tooling for the JS ecosystem is a real challenge, butLet's see how can we do something about it...
date: 2021-12-16
language: en
---

[fable]: https://fable.io/
[parcel]: https://parceljs.org/
[fuse-box]: https://fuse-box.org/
[webpack]: https://webpack.js.org/
[cliwrap]: https://github.com/Tyrrrz/CliWrap
[fable real world]: https://github.com/AngelMunoz/real-world-fable
[snowpack]: https://www.snowpack.dev/
[vitejs]: https://vitejs.dev/
[unbundled]: https://www.snowpack.dev/concepts/how-snowpack-works#unbundled-development
[import maps]: https://github.com/WICG/import-maps
[suave]: https://suave.io/
[saturn]: https://saturnframework.org/
[esbuild]: https://esbuild.github.io/
[published about sse]: https://dev.to/tunaxor/server-sent-events-with-saturn-and-fsharp-m6b
[import map generator]: https://generator.jspm.io/
[yarp]: https://microsoft.github.io/reverse-proxy/articles/direct-forwarding.html
[there's an hmr spec]: https://github.com/snowpackjs/esm-hmr
[elmish.hmr]: https://elmish.github.io/hmr/
[fable.lit]: https://fable.io/Fable.Lit/
[webworkers]: https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API/Using_web_workers
[perla]: https://perla-docs.web.app/
[esm modules]: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Modules
[fsharp.control.reactive]: http://fsprojects.github.io/FSharp.Control.Reactive/
[@yaurthek]: https://twitter.com/Yaurthek

<script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>

Fable 3 made something I was not aware for some time, it moved to emitting ESM Modules and leaving babel and other stuff behind for users to set up.
It was around June that I was really mad at compilation times with [Fable] projects, After being in the JS/Node ecosystems for years I wondered what could be done to improve that situation.

<blockquote class="twitter-tweet" data-theme="dark"><p lang="en" dir="ltr">If you&#39;re only using html/css/<a href="https://twitter.com/hashtag/fsharp?src=hash&amp;ref_src=twsrc%5Etfw">#fsharp</a> I think you can have a fairly modern dev setup with just suave, fable cli and F# scripts, I&#39;ll see if I can get a POC out later on, what would be missing HMR, bundling, some types of imports and preprocessing</p>&mdash; Angel Munoz (@angel_d_munoz) <a href="https://twitter.com/angel_d_munoz/status/1404250352338325510?ref_src=twsrc%5Etfw">June 14, 2021</a></blockquote>

At the time I had been exploring alternatives to [Webpack] like [fuse-box], [parcel], and [esbuild]. Around the same time I was made aware aware that browsers had already implemented [ESM modules], so technically as long as you produced HTML, CSS, and JS you didn't need any kind of pre-processing at all.

This shouldn't be that hard, I just needed a server that well... served the HTML/CSS/JS files right?
I went to my desktop, created an F# script added a couple of libraries like [Suave] and [CliWrap] so I could call the `dotnet fable` command from my F# code and make it compile my Fable files.

Taking out some code I came up with this PoC:

```fsharp
// I omited more code above for brevity
let stdinAsyncSeq () =
    let readFromStdin () =
        Console.In.ReadLineAsync() |> Async.AwaitTask

    asyncSeq {
        // I wanted to think this is a "clever"
        // way to keep it running
        while true do
            let! value = readFromStdin ()
            value
    }
    |> AsyncSeq.distinctUntilChanged
    |> AsyncSeq.iterAsync onStdinAsync

let app =
    choose [ path "/"
             >=> GET
             // send the index file
             >=> Files.browseFileHome "index.html"
             // serve static files
             GET >=> Files.browseHome
             RequestErrors.NOT_FOUND "Not Found"
             // SPA like fallback
             >=> redirect "/" ]

let config (publicPath: string option) =
    let path =
        Path.GetFullPath(
            match publicPath with
            | Some "built" -> "./dist"
            | _ -> "./public"
        )

    printfn $"Serving content from {path}"
    // configure the suave server instance
    { defaultConfig with
          bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 3000 ]
          homeFolder = Some path
          compressedFilesFolder = Some(Path.GetFullPath "./.compressed") }
// let's make it run!
stdinAsyncSeq () |> Async.Start
// dotnet fsi suave.fsx built to show how bundled files work
startWebServer (config (fsi.CommandLineArgs |> Array.tryLast)) app
```

Now, I could have my suave server and my Fable compiler running on the background. I could see my files being served in my browser I could make changes, press F5 and see them working.

<blockquote class="twitter-tweet" data-theme="dark"><p lang="en" dir="ltr">It is possible to have a &quot;node-less&quot; <a href="https://twitter.com/hashtag/fsharp?src=hash&amp;ref_src=twsrc%5Etfw">#fsharp</a> frontend development experience thanks to <a href="https://twitter.com/FableCompiler?ref_src=twsrc%5Etfw">@FableCompiler</a> and <a href="https://twitter.com/SuaveIO?ref_src=twsrc%5Etfw">@SuaveIO</a> <br>with a small F# script you can spin up a suave server and have fable compile your files in the background <a href="https://t.co/LfscIPlw9s">https://t.co/LfscIPlw9s</a></p>&mdash; Angel Munoz (@angel_d_munoz) <a href="https://twitter.com/angel_d_munoz/status/1404300942648954882?ref_src=twsrc%5Etfw">June 14, 2021</a></blockquote>

Cool it worked... Yay!... sure, with my attention span for some things I simply didn't think too much about it, or so I thought.

What came up next was experimenting with snowpack and fuse-box to see which setup could work best with Fable 3 and Although, Both projects work extremely well with Fable, the snowpack project felt more compelling to me thanks to the promoted [Unbundled] development concept. I decided to go for it and tried the [Fable Real World] implementation and switched webpack for snowpack and the results were kind of what I was expecting, faster builds, a simpler setup and a much faster developer loop feedback with the browser.

Unconsciously on the back of my head was still that voice about writing something like snowpack in F#... In my mind, the people who build those kinds of tools are like people in the movies; You know they exist but, you don't think you are capable of doing something like it. Specially when most of my experience at that point was building UI's in things like Angular.

I went ahead and started studying the snowpack source code and I found out that they were using [esbuild] a JS/TS compiler written in Go, no wonder why it was faster than anything done in JavaScript at the time.

Also, on the background [vitejs] was also starting to get in shape, I was looking at Evan's tweets from afar and getting inspired from that as well so I realized I needed to go back and see if I could go even further.

- What if I used esbuild as well?
- What if I could use esbuild to produce my prod bundle after I built my fable code?

<blockquote class="twitter-tweet" data-theme="dark"><p lang="en" dir="ltr">Going back to that suave-dev server...<br>if we do add esbuild, we could actually go nodeless even when preparing for prod stuff<a href="https://twitter.com/hashtag/fsharp?src=hash&amp;ref_src=twsrc%5Etfw">#fsharp</a><br>updated the scripts there<a href="https://t.co/LfscIPlw9s">https://t.co/LfscIPlw9s</a> <a href="https://t.co/61bvfyTBkd">pic.twitter.com/61bvfyTBkd</a></p>&mdash; Angel Munoz (@angel_d_munoz) <a href="https://twitter.com/angel_d_munoz/status/1407072676355751938?ref_src=twsrc%5Etfw">June 21, 2021</a></blockquote>

Turns out... I wasn't that crazy, after all both vite and snowpack were doing it as well!

Around September vite got traction with the vue user base and other users as well. I also studied a bit the vite source code, and even used it for some Fable material for posts. I was trying to make some awareness of [Fable.Lit] support for Web Components and I wanted to experiment in reality how good vite was, and boi it's awesome If you're starting new projects that depend on node tooling in my opinion, it's your best bet.

Anyways, I tend to be looking at what's new on the web space, and by this time these... [Import Maps] thing came to my attention, it is a really nice browser feature that can be used to control the browser's behavior to import JavaScript files.

Import maps can tell the browser to use "bare specifiers" (i.e. `import dependency from "my-dependency"` rather than `"./my-dependency.js`)

Almost like "pull this import from this URL".

Hopefully you are starting to put the pieces together as I was doing.

Maybe... It might be possible to actually enjoy the NPM ecosystem without having to rely on the local tooling... Just maybe...

<blockquote class="twitter-tweet" data-theme="dark"><p lang="en" dir="ltr">It&#39;s starting to make sense if this works we would just need a couple of msbuild tasks to switch from lookup to pinned urls in prod mode and no node needed for frontend development, <a href="https://twitter.com/skypackjs?ref_src=twsrc%5Etfw">@skypackjs</a> and import maps really enable cool stuff<a href="https://twitter.com/hashtag/fsharp?src=hash&amp;ref_src=twsrc%5Etfw">#fsharp</a> <a href="https://t.co/u8dvHaqeeK">pic.twitter.com/u8dvHaqeeK</a></p>&mdash; Angel Munoz (@angel_d_munoz) <a href="https://twitter.com/angel_d_munoz/status/1438579622904582144?ref_src=twsrc%5Etfw">September 16, 2021</a></blockquote>

From then on, It was just about making small F# scripts to experiment PoC's and implementing small features.

At this point is when I said to myself.

> It's time to build a Webpack alternative...

For this... _FSharp.DevServer_... I needed to have an idea of what I wanted to implement to make it usable at least, I settled on the following set of features.

- Serve HTML/CSS/JS
- Support for Fable Projects
- Reload on change
- Install dependencies
- Production Bundles
- Transpilation on the fly
- Dev Proxy
- Plugins
- HMR

Those are the least features necessary IMO to consider for a project like this.

I will take you on a quick tour at how those got implemented at some point in the project.

> Keep in mind I'm not an F# expert! I'm just a guy with a lot of anxiety and free time so I use my code to distract myself while having a ton of fun with F# code.

While for a proof of concept Suave did great, I switched it in favor of [Saturn] given my familiarity with it and some ASP.NET code.

## Serve HTML/CSS/JS

From most of the features, this must have been perhaps the most simple after all if you're using a server, it should be really simple to do right?

Well... Yes and No... it turns out that if you serve static files they get out of the middleware chain very quick due to the order of the static middleware is in. While it was good for serving it if I wanted to reload on change or compile these files I was not going to be able to do it.

```fsharp
// the current devServer function has way more stuff
// due the extra features that have been implemented
let private devServer (config: FdsConfig) =
    let withAppConfig (appConfig: IApplicationBuilder) =
        // let's serve everything statically
        // but let's ignore some extensions
        let ignoreStatic =
          [ ".js"
            ".css"
            ".module.css"
            ".ts"
            ".tsx"
            ".jsx"
            ".json" ]
        // mountedDirs is a property in perla.jsonc
        // that enables you to watch a partcular set of
        // directories for source code
        for map in mountedDirs do
          let staticFileOptions =
            let provider = FileExtensionContentTypeProvider()

            for ext in ignoreStatic do
              provider.Mappings.Remove(ext) |> ignore

            let options = StaticFileOptions()

            options.ContentTypeProvider <- provider
            // in the next lines we enable local mapings
            // to URL's e.g.
            // ./src on disk -> /src on the URL
            options.RequestPath <- PathString(map.Value)

            options.FileProvider <-
              new PhysicalFileProvider(Path.GetFullPath(map.Key))

            options

          appConfig.UseStaticFiles staticFileOptions
          |> ignore

        let appConfig =
          // at the same time we enable transpilation
          // middleware when we're ignoring some extensions
          appConfig.UseWhen(
            Middleware.transformPredicate ignoreStatic,
            Middleware.configureTransformMiddleware config
          )
    // set the configured options
    application {
        app_config withAppConfig
        webhost_config withWebhostConfig
        use_endpoint_router urls
    }
    // build it
    app
        .UseEnvironment(Environments.Development)
        .Build()
```

This part is just about serving files, nothing more, nothing less, that's the core of a dev server.

## Support for Fable Projects

Fable is actually not hard to support, fable is distributed as a dotnet tool, we can invoke the command with [CliWrap] which has proven us in the PoC stage, how simple is to call a process from .NET.

```fsharp
// This is the actual Fable implementation

module Fable =
  let mutable private activeFable: int option = None

  // this is to start/stop the fable command
  // if requested by the user
  let private killActiveProcess pid =
    try
      let activeProcess = System.Diagnostics.Process.GetProcessById pid

      activeProcess.Kill()
    with
    | ex -> printfn $"Failed to Kill Procees with PID: [{pid}]\n{ex.Message}"

  // Helper functions to add arguments to the fable command

  let private addOutDir
    (outdir: string option)
    (args: Builders.ArgumentsBuilder)
    =
    match outdir with
    | Some outdir -> args.Add $"-o {outdir}"
    | None -> args

  let private addExtension
    (extension: string option)
    (args: Builders.ArgumentsBuilder)
    =
    match extension with
    | Some extension -> args.Add $"-e {extension}"
    | None -> args

  let private addWatch (watch: bool option) (args: Builders.ArgumentsBuilder) =
    match watch with
    | Some true -> args.Add $"--watch"
    | Some false
    | None -> args

  // we can fire up fable either as a background process
  // or before calling esbuild for production
  let fableCmd (isWatch: bool option) =

    fun (config: FableConfig) ->
      let execBinName =
        if Env.isWindows then
          "dotnet.exe"
        else
          "dotnet"

      Cli
        .Wrap(execBinName)
        .WithArguments(fun args ->
          args
            .Add("fable")
            .Add(defaultArg config.project "./src/App.fsproj")
          |> addWatch isWatch
          |> addOutDir config.outDir
          |> addExtension config.extension
          |> ignore)
        // we don't do a lot, we simply re-direct the stdio to the console
        .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
        .WithStandardOutputPipe(
          PipeTarget.ToStream(Console.OpenStandardOutput())
        )

  let stopFable () =
    match activeFable with
    | Some pid -> killActiveProcess pid
    | None -> printfn "No active Fable found"

  let startFable
    (getCommand: FableConfig option -> Command)
    (config: FableConfig option)
    =
    task {
      // Execute and wait for it to finish
      let cmdResult = getCommand(config).ExecuteAsync()
      activeFable <- Some cmdResult.ProcessId

      return! cmdResult.Task
    }
```

Keeping the process ID on memory might not be the best idea and there can be better ways to handle that but at least for now it works just fine.

Calling the `startFable` function with fable options, will make fable run on the background, this allows us to have fable output JS files that we will be able to serve.

## Reload on change

Reloading on change was an interesting feature to do, first of all I needed a file watcher and I have had heard before that the .NET one wasn't really that great, I also needed to communicate with the frontend when something changed in the backend.

For the file watcher, I tried to search for good alternatives, but to be honest in the end I decided to go with the one in the BCL.

I was kind of scared though how would I manage multiple notifications and events without making it a mess? I had No idea... Thankfully [FSharp.Control.Reactive] was found and is just what I needed. This library allows you to make observables from events and has a bunch of nice utility functions to work with stream like collections if you've used RxJS or RX.NET you will feel at home with it.

```fsharp

  let getFileWatcher (config: WatchConfig) =
    let watchers =
      // monitor a particular list of  addresses
      (defaultArg config.directories ([ "./src" ] |> Seq.ofList))
      |> Seq.map (fun dir ->
        // for each address create a file watcher
        let fsw = new FileSystemWatcher(dir)
        fsw.IncludeSubdirectories <- true
        fsw.NotifyFilter <- NotifyFilters.FileName ||| NotifyFilters.Size

        let filters =
          defaultArg
            config.extensions
            (Seq.ofList [ "*.js"
                          "*.css"
                          "*.ts"
                          "*.tsx"
                          "*.jsx"
                          "*.json" ])
        // ensure you're monitoring all of the
        // extensions you want to reload on change
        for filter in filters do
          fsw.Filters.Add(filter)
        // and ensure you will rise events for them :)
        fsw.EnableRaisingEvents <- true
        fsw)


    let subs =
      watchers
      |> Seq.map (fun watcher ->
        // for each watche react to the following events
        // Renamed
        // Changed
        // Deleted
        // Created
        [ watcher.Renamed
          // To prevent overflows and weird behaviors
          // ensure to throttle the events
          |> Observable.throttle (TimeSpan.FromMilliseconds(400.))
          |> Observable.map (fun e ->
            { oldName = Some e.OldName
              ChangeType = Renamed
              name = e.Name
              path = e.FullPath })
          watcher.Changed
          |> Observable.throttle (TimeSpan.FromMilliseconds(400.))
          |> Observable.map (fun e ->
            { oldName = None
              ChangeType = Changed
              name = e.Name
              path = e.FullPath })
          watcher.Deleted
          |> Observable.throttle (TimeSpan.FromMilliseconds(400.))
          |> Observable.map (fun e ->
            { oldName = None
              ChangeType = Deleted
              name = e.Name
              path = e.FullPath })
          watcher.Created
          |> Observable.throttle (TimeSpan.FromMilliseconds(400.))
          |> Observable.map (fun e ->
            { oldName = None
              ChangeType = Created
              name = e.Name
              path = e.FullPath }) ]
        // Merge these observables in a single one
        |> Observable.mergeSeq)

    { new IFileWatcher with
        override _.Dispose() : unit =
          watchers
          // when disposing, dispose every watcher you may have around
          |> Seq.iter (fun watcher -> watcher.Dispose())

        override _.FileChanged: IObservable<FileChangedEvent> =
          // merge the the merged observables into a single one!!!
          Observable.mergeSeq subs }
```

With this setup you can easily observe changes to multiple directories and multiple extensions it might not be the most efficient way to do it, but It at least got me started with it, now that I had a way to know when something changed I needed to tell the browser what had happened.

For that I chose SSE (Server Sent Events) which is a really cool way to do real time notifications from the server exclusively without having to implement web sockets it's just an HTTP call which can be terminated (or not).

```fsharp

  let private Sse (watchConfig: WatchConfig) next (ctx: HttpContext) =
    task {
      let logger = ctx.GetLogger("Perla:SSE")
      logger.LogInformation $"LiveReload Client Connected"
      // set up the correct headers
      ctx.SetHttpHeader("Content-Type", "text/event-stream")
      ctx.SetHttpHeader("Cache-Control", "no-cache")
      ctx.SetStatusCode 200

      // send the first event
      let res = ctx.Response
      do! res.WriteAsync($"id:{ctx.Connection.Id}\ndata:{DateTime.Now}\n\n")
      do! res.Body.FlushAsync()

      // get the observable of file changes
      let watcher = Fs.getFileWatcher watchConfig

      logger.LogInformation $"Watching %A{watchConfig.directories} for changes"

      let onChangeSub =
        watcher.FileChanged
        |> Observable.map (fun event ->
          task {
            match Path.GetExtension event.name with
            | Css ->
              // if the change was on a CSS file send the new content
              let! content = File.ReadAllTextAsync event.path

              let data =
                Json.ToTextMinified(
                  {| oldName =
                      event.oldName
                      |> Option.map (fun value ->
                        match value with
                        | Css -> value
                        | _ -> "")
                     name = event.path
                     content = content |}
                )
              // CSS HMR was basically free!
              do! res.WriteAsync $"event:replace-css\ndata:{data}\n\n"
              return! res.Body.FlushAsync()
            // if it's any other file well... just reload
            | Typescript
            | Javascript
            | Jsx
            | Json
            | Other _ ->
              let data =
                Json.ToTextMinified(
                  {| oldName = event.oldName
                     name = event.name |}
                )

              logger.LogInformation $"LiveReload File Changed: {event.name}"
              do! res.WriteAsync $"event:reload\ndata:{data}\n\n"
              return! res.Body.FlushAsync()
          })
        // ensure the task gets done
        |> Observable.switchTask
        |> Observable.subscribe ignore
      // if the client closes the browser
      // then dispose these resources
      ctx.RequestAborted.Register (fun _ ->
        watcher.Dispose()
        onChangeSub.Dispose())
      |> ignore

      // keep the connection alive
      while true do
        // TBH there must be a better way to do it
        // but since this is not critical, it works just fine
        do! Async.Sleep(TimeSpan.FromSeconds 1.)

      return! text "" next ctx
    }
```

At this time, I also [published about SSE] on my blog, I really felt it was a really cool thing and decided to share it with the rest of the world :)

## Install dependencies

I was really undecided if I wanted to pursue a webpack alernative because

- How can you install dependencies without npm?
- Do you really want to do
  - `import { useState } from 'https://cdn.skypack.dev/pin/react@v17.0.1-yH0aYV1FOvoIPeKBbHxg/mode=imports,min/optimized/react.js'`

On every damned file? oh no no no, I don't think so... Enter the Import Maps, this feature (along esbuild) was the thing that made me realize it was actually possible to ditch out node/webpack/npm entirely (at least in a local and direct way) instead of doing that ugly import from above, if you can provide a import map with your dependencies the rest should be relatively easy

```html
<script type="importmap">
  {
    "imports": {
      "moment": "https://ga.jspm.io/npm:moment@2.29.1/moment.js",
      "lodash": "https://cdn.skypack.dev/lodash"
    }
  }
</script>
<!-- Allows you to do the next  -->
<script type="module">
  import moment from "moment";
  import lodash from "lodash";
</script>
```

<blockquote class="twitter-tweet" data-theme="dark"><p lang="en" dir="ltr">Here&#39;s something towards a node&#39;less frontend development for <a href="https://twitter.com/hashtag/fsharp?src=hash&amp;ref_src=twsrc%5Etfw">#fsharp</a> with help of <a href="https://twitter.com/skypackjs?ref_src=twsrc%5Etfw">@skypackjs</a> <br>The cli tool &quot;installs&quot; a package (i.e. grabs a skypack lookup url) and saves: import map, lock, dependency this import map is added to the index file <a href="https://t.co/gt9qooiYVl">pic.twitter.com/gt9qooiYVl</a></p>&mdash; Angel Munoz (@angel_d_munoz) <a href="https://twitter.com/angel_d_munoz/status/1439457103433936897?ref_src=twsrc%5Etfw">September 19, 2021</a></blockquote>

So here I was trying to replicate a version of `package.json` this ended up implementing the `perla.jsonc.lock` file which is not precisely a _lock_ file, while the URL's there are certainly the pined and production versions of those packages, it's in reality the _**import map**_ in disguise, to get that information though I had to investigate how to do it. Once again I decided to study snowpack since it's the only frontend dev tool I know it has this kind of mechanism (remote sources), after some investigation and some PoC's I also stumbled upon JSPM's recently released [Import Map Generator] which is basically what I wanted to do! Skypack, JSPM and Unpkg offer reliable CDN services for production with all of these investigations and gathered knowledge I went to implement fetching dependencies and "_installing_" them with the dev server tool.

```fsharp

[<RequireQualifiedAccessAttribute>]
module internal Http =
  open Flurl
  open Flurl.Http

  [<Literal>]
  let SKYPACK_CDN = "https://cdn.skypack.dev"

  [<Literal>]
  let SKYPACK_API = "https://api.skypack.dev/v1"

  [<Literal>]
  let JSPM_API = "https://api.jspm.io/generate"

  let private getSkypackInfo (name: string) (alias: string) =
    // FsToolkit.ErrorHandling FTW
    taskResult {
      try
        let info = {| lookUp = $"%s{name}" |}
        let! res = $"{SKYPACK_CDN}/{info.lookUp}".GetAsync()

        if res.StatusCode >= 400 then
          return! PackageNotFoundException |> Error

        let mutable pinnedUrl = ""
        let mutable importUrl = ""
        // try to get the pinned URL from the headers
        let info =
          if
            res.Headers.TryGetFirst("x-pinned-url", &pinnedUrl)
            |> not
          then
            {| info with pin = None |}
          else
            {| info with pin = Some pinnedUrl |}

        // and the imports as well
        let info =
          if
            res.Headers.TryGetFirst("x-import-url", &importUrl)
            |> not
          then
            {| info with import = None |}
          else
            {| info with import = Some importUrl |}

        return
          // generate the corresponding import map entry
          [ alias, $"{SKYPACK_CDN}{info.pin |> Option.defaultValue info.lookUp}" ],
          // skypack doesn't handle any import maps so the scopes will always be empty
          []
      with
      | :? Flurl.Http.FlurlHttpException as ex ->
        match ex.StatusCode |> Option.ofNullable with
        | Some code when code >= 400 ->
          return! PackageNotFoundException |> Error
        | _ -> ()

        return! ex :> Exception |> Error
      | ex -> return! ex |> Error
    }

  let getJspmInfo name alias source =
    taskResult {
      let queryParams =
        {| install = [| $"{name}" |]
           env = "browser"
           provider =
           // JSPM offer various reliable sources
           // to get your dependencies
            match source with
            | Source.Skypack -> "skypack"
            | Source.Jspm -> "jspm"
            | Source.Jsdelivr -> "jsdelivr"
            | Source.Unpkg -> "unpkg"
            | _ ->
              printfn
                $"Warn: An unknown provider has been specied: [{source}] defaulting to jspm"

              "jspm" |}

      try
        let! res =
          JSPM_API
            .SetQueryParams(queryParams)
            .GetJsonAsync<JspmResponse>()

        let scopes =
          // F# type serialization hits again!
          // the JSPM response may include a scope object or not
          // so try to safely check if it exists or not
          match res.map.scopes :> obj |> Option.ofObj with
          | None -> Map.empty
          | Some value -> value :?> Map<string, Scope>

        return
          // generate the corresponding import map
          // entries as well as the scopes
          res.map.imports
          |> Map.toList
          |> List.map (fun (k, v) -> alias, v),
          scopes |> Map.toList
      with
      | :? Flurl.Http.FlurlHttpException as ex ->
        match ex.StatusCode |> Option.ofNullable with
        | Some code when code >= 400 ->
          return! PackageNotFoundException |> Error
        | _ -> ()

        return! ex :> Exception |> Error
    }

  let getPackageUrlInfo (name: string) (alias: string) (source: Source) =
    match source with
    | Source.Skypack -> getSkypackInfo name alias
    | _ -> getJspmInfo name alias source
```

This was a relatively low effort to implement but it did require finding a way to gather these resources so they can be mapped to json objects. This approach also allows you yo import different version fo the same package in the same application! that can be useful when you want to migrate dependencies slowly rolling them out.

## Production Bundles

Just as Installing dependencies, having a production ready build is critical This is where [esbuild] finally comes into the picture it is a crucial piece of the puzzle. Esbuild while it's written in go and offers a npm package, it provides a single executable binary which can be used in a lot of platforms and and architectures, it distributes itself through the npm registry so it's about downloading the package in the correct way and just executing it like we did for the fable command.

```fsharp
let esbuildJsCmd (entryPoint: string) (config: BuildConfig) =

  let dirName =
    (Path.GetDirectoryName entryPoint)
      .Split(Path.DirectorySeparatorChar)
    |> Seq.last

  let outDir =
    match config.outDir with
    | Some outdir -> Path.Combine(outdir, dirName) |> Some
    | None -> Path.Combine("./dist", dirName) |> Some

  let execBin = defaultArg config.esBuildPath esbuildExec

  let fileLoaders = getDefaultLoders config

  Cli
    .Wrap(execBin)
    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    // CliWrap simply allows us to add arguments to commands very easy
    .WithArguments(fun args ->
      args.Add(entryPoint)
      |> addEsExternals config.externals
      |> addIsBundle config.bundle
      |> addTarget config.target
      |> addDefaultFileLoaders fileLoaders
      |> addMinify config.minify
      |> addFormat config.format
      |> addInjects config.injects
      |> addOutDir outDir
      |> ignore)

```

the CLI API from esbuild is pretty simple to be honest and is really effective when it comes to transpilation the benefits are that it not just transpiles Javascript, it also transpiles typescript, jsx and tsx files. Adding to those features esbuild is blazing fast.

# Transpilation on the fly

The dev server not only needs to serve JS content to the browser, often it needs to serve Typescript/JSX/TSX as well, and as we found earlier in the post if you serve static content your options for transforming or manipulating these request are severely limited, so I had to make particular middlewares to enable compiling single files on the fly.
let's check a little bit how these are somewhat laid out on Perla

```fsharp
[<RequireQualifiedAccess>]
module Middleware =
    // this function helps us determine a particular extension is in the request path
    // if it is we  will use one of the middlewares below on the calling site.
    let transformPredicate (extensions: string list) (ctx: HttpContext) = ...

    let cssImport
        (mountedDirs: Map<string, string>)
        (ctx: HttpContext)
        (next: Func<Task>) = ...

    let jsonImport
        (mountedDirs: Map<string, string>)
        (ctx: HttpContext)
        (next: Func<Task>) = ...

    let jsImport
        (buildConfig: BuildConfig option)
        (mountedDirs: Map<string, string>)
        (ctx: HttpContext)
        (next: Func<Task>) =
        task {
        let logger = ctx.GetLogger("Perla Middleware")

        if
            // for the moment, we just serve the JS as is and don't process it
            ctx.Request.Path.Value.Contains("~perla~")
            || ctx.Request.Path.Value.Contains(".js") |> not
        then
            return! next.Invoke()
        else

            let path = ctx.Request.Path.Value
            logger.LogInformation($"Serving {path}")

            let baseDir, baseName =
                // check if we're actually monitoring this directory and this file extension
                mountedDirs
                |> Map.filter (fun _ v -> String.IsNullOrWhiteSpace v |> not)
                |> Map.toSeq
                |> Seq.find (fun (_, v) -> path.StartsWith(v))

            // find the file on disk
            let filePath =
                let fileName =
                    path.Replace($"{baseName}/", "", StringComparison.InvariantCulture)

                Path.Combine(baseDir, fileName)
            // we will serve javascript regardless of what we find on disk
            ctx.SetContentType "text/javascript"

            try
                if Path.GetExtension(filePath) <> ".js" then
                    return failwith "Not a JS file, Try looking with another extension."
                // if the file exists on disk
                // and has a js extension then just send it as is
                // the browser should be able to interpret it
                let! content = File.ReadAllBytesAsync(filePath)
                do! ctx.WriteBytesAsync content :> Task
            with
            | ex ->
                let! fileData = Esbuild.tryCompileFile filePath buildConfig

                match fileData with
                | Ok (stdout, stderr) ->
                    if String.IsNullOrWhiteSpace stderr |> not then
                        // In the SSE code, we added (later on)
                        // an observer for compilation errors and send a message to the client,
                        // this should trigger an "overlay" on the client side
                        Fs.PublishCompileErr stderr
                        do! ctx.WriteBytesAsync [||] :> Task
                    else
                        // if the file got compiled then just write the file to the body
                        // of the request
                        let content = Encoding.UTF8.GetBytes stdout
                        do! ctx.WriteBytesAsync content :> Task
                | Error err ->
                    // anything else, just send a 500
                    ctx.SetStatusCode 500
                    do! ctx.WriteTextAsync err.Message :> Task
        }
        :> Task

    let configureTransformMiddleware
        (config: FdsConfig)
        (appConfig: IApplicationBuilder) =
        let serverConfig =
            defaultArg config.devServer (DevServerConfig.DefaultConfig())

        let mountedDirs = defaultArg serverConfig.mountDirectories Map.empty

        appConfig
            .Use(Func<HttpContext, Func<Task>, Task>(jsonImport mountedDirs))
            .Use(Func<HttpContext, Func<Task>, Task>(cssImport mountedDirs))
            .Use(
                Func<HttpContext, Func<Task>, Task>(jsImport config.build mountedDirs)
            )
            |> ignore
```

It is a pretty simple module (I want to think) that only has some functions that deal with the content of the files and return any compiled result if neededm otherwise just send the file.

Now... Let's take a look at the magic behind `let! fileData = Esbuild.tryCompileFile filePath buildConfig` to be honest I didn't really know what I was doing, the main line of thought was just to try and find the content on disk and try the next extension if it didn't work. Hah! well ![as long as it works](https://th.bing.com/th/id/OIP.XBALsTMB2KdavcvdRAlpuwHaFD?pid=ImgDet&rs=1)

```fsharp
let tryCompileFile filepath config =
  taskResult {
    let config = (defaultArg config (BuildConfig.DefaultConfig()))
    // since we're using
    // FsToolkit.ErrorHandling if the operation fails it will
    // "early return" meaning it won't continue the success path
    let! res = Fs.tryReadFile filepath
    let strout = StringBuilder()
    let strerr = StringBuilder()
    let (_, loader) = res

    let cmd = buildSingleFileCmd config (strout, strerr) res
    // execute esbuild on the file
    do! (cmd.ExecuteAsync()).Task :> Task

    let strout = strout.ToString()
    let strerr = strerr.ToString()

    let strout =
      match loader with
      | Jsx
      | Tsx ->
        try
          // if the file needs injects (e.g automatic "import React from 'react'" in JSX files)
          let injects =
            defaultArg config.injects (Seq.empty)
            |> Seq.map File.ReadAllText

          // add those right here
          let injects = String.Join('\n', injects)
          $"{injects}\n{strout}"
        with
        | ex ->
          printfn $"Perla Serve: failed to inject, {ex.Message}"
          strout
      | _ -> strout
    // return the compilation results
    // the transpiled output and the error if any
    return (strout, strerr)
  }
```

Surely thats a lot of things to do for a single file, I'm sure it must be quite slow right? Well... It turns out that **.NET** and **Go** are quite quite feaking fast

<blockquote class="twitter-tweet" data-theme="dark"><p lang="en" dir="ltr">.NET6 is really fast even for my not optimized sloppy <a href="https://twitter.com/hashtag/fsharp?src=hash&amp;ref_src=twsrc%5Etfw">#fsharp</a> code<br>I&#39;m doing a bunch of IO/async operations without peformance in mind plus my weird mental logic and yet both esbuild transform + middleware stuff + IO + tasks<br>and yet each request takes between 10-20ms <a href="https://t.co/GHCEoUPM36">pic.twitter.com/GHCEoUPM36</a></p>&mdash; Angel Munoz (@angel_d_munoz) <a href="https://twitter.com/angel_d_munoz/status/1444508310393212934?ref_src=twsrc%5Etfw">October 3, 2021</a></blockquote>

each request takes around 10-20ms and I'm pretty sure it can be improved once the phase of heavy development settles down and the code base stabilizes a little bit more.

## Dev Proxy

This one is pretty new, a dev proxy is somewhat necessary specially when you will host your applications on your own server so you are very likely to have URLs like `/api/my-endpoint` rather than `http://my-api.com/api/my-endpoint` it also helps you target different environments with a single configuration change, in this case it was not really complex thanks to [@Yaurthek] who hinted at me one [Yarp] implementation of a dev proxy, so I ended up basing my work on that.

The whole idea here is to read a json file with some `origin -> target` mappings and then just adding a proxy to the server application.

```fsharp

  let private getHttpClientAndForwarder () =
    // this socket handler is actually disposable
    // but since technically I will only use one in the whole application
    // I won't need to dispose it
    let socketsHandler = new SocketsHttpHandler()
    socketsHandler.UseProxy <- false
    socketsHandler.AllowAutoRedirect <- false
    socketsHandler.AutomaticDecompression <- DecompressionMethods.None
    socketsHandler.UseCookies <- false
    let client = new HttpMessageInvoker(socketsHandler)
    let reqConfig = ForwarderRequestConfig()
    reqConfig.ActivityTimeout <- TimeSpan.FromSeconds(100.)
    client, reqConfig

  let private getProxyHandler
    (target: string)
    (httpClient: HttpMessageInvoker)
    (forwardConfig: ForwarderRequestConfig)
    : Func<HttpContext, IHttpForwarder, Task> =
    // this is actually using .NET6 Minimal API's from asp.net!
    let toFunc (ctx: HttpContext) (forwarder: IHttpForwarder) =
      task {
        let logger = ctx.GetLogger("Perla Proxy")
        let! error = forwarder.SendAsync(ctx, target, httpClient, forwardConfig)

        // report the errors to the log as a warning
        // since we don't need to die if a request fails
        if error <> ForwarderError.None then
          let errorFeat = ctx.GetForwarderErrorFeature()
          let ex = errorFeat.Exception
          logger.LogWarning($"{ex.Message}")
      }
      :> Task
    Func<HttpContext, IHttpForwarder, Task>(toFunc)
```

And then somewher inside the aspnet application configuration

```fsharp
match getProxyConfig with
| Some proxyConfig ->
    appConfig
    .UseRouting()
    .UseEndpoints(fun endpoints ->
        let (client, reqConfig) = getHttpClientAndForwarder ()
        // for each mapping add the url add an endpoint
        for (from, target) in proxyConfig |> Map.toSeq do
            let handler = getProxyHandler target client reqConfig
            endpoints.Map(from, handler) |> ignore)
| None -> appConfig
```

That's it! At least on my initial testing it seems to work fine, I would need to have some feedback on the feature to know if this is actually working for more complex use cases.

## Future and Experimental things

What you have seen so far (and some other minor features) are already inside Perla, they are working and they try to provide you a seamless experience for building Single Page Applications however there are still missing pieces for a complete experience. For example Perla doesn't support Sass or Less at the moment and Sass is a pretty common way to write styles on big frontend projects, we are not able to parse out `.Vue` files or anything else that is not HTML/CSS/JS/TS/JSX/TSX, We do support HMR for CSS files since that is not a complex mechanism but, HMR for JS/TS/JSX/TSX files is not there yet sady. Fear not that We're looking for a way to provide these at some point in time.

## Plugins

I'm a fan of `.fsx` files, F# scripts are pretty flexible and since F# 5.0 they are even more powerful than ever allowing you to pull dependencies directly from NuGet without any extra command.

The main goal for the author and user experiences is somewhat like this

As an Author:

- Write an `.fsx` script
- Upload it to gist/github
- Profit

As a User:

- Add an entry yo tour "plugins" section
- Profit

Implementation details are more complex though...

My vision is somewhere along the following lines

- get a request for a different file e.g. sass files
- if the file is not part of the default supported extensions
  - Call a function that will parse the content of that file
  - get the transpiled content or the compilation error (just like we saw above with the js middleware)
  - return the valid HTML/CSS/JS content to the browser

To get there I want to leverage the [FSharp.Compiler.Services] NuGet Package to start an F# interactive session that runs over the life of the server,

- Start the server, also if there are plugins in the plugin section, start the fsi session.
- load the plugins, download them to a known location in disk, or even just get the strings without downloading the file to disk
- execute the contents on the fsi session and grab a particular set of functions
  - These functions can be part of a life cycle which may possible be something like
    - on load // when HMR is enabled
    - on change // when HMR is enabled
    - on transform // when the file is requested
    - on build // when the production build is executed
- call the functions in the plugins section whenever needed

Starting an FSI session is not a complex task let's take a look.

let's say we have the following script:

```fsharp
#r "nuget: LibSassHost, 1.3.3"
#r "nuget: LibSassHost.Native.linux-x64, 1.3.3"

open System.IO
open LibSassHost

let compileSassFile =
  let _compileSassFile (filePath: string) =
    let filename = Path.GetFileName(filePath)
    let result = SassCompiler.Compile(File.ReadAllText(filePath))
    [|filename; result.CompiledContent |]
```

In this file we're able to provide a function that when given a file path, it will try to compile a `.scss` file into it's `.css` equivalent, to be able to execute that in Perla, we need a module that does somewhat like this:

```fsharp
#r "nuget: FSharp.Compiler.Service, 41.0.1"

open System
open System.IO
open FSharp.Compiler.Interactive.Shell

module ScriptedContent =

    let tryGetSassPluginFunction (content: string): (string -> string) option =
        let defConfig =
            FsiEvaluationSession.GetDefaultConfiguration()

        let argv =
            [| "fsi.exe"
               "--noninteractive"
               "--nologo"
               "--gui-" |]

        use stdIn = new StringReader("")
        use stdOut = new StringWriter()
        use stdErr = new StringWriter()

        use session =
            FsiEvaluationSession.Create(defConfig, argv, stdIn, stdOut, stdErr, true)
        session.EvalInteractionNonThrowing(content) |> ignore

        match session.TryFindBoundValue "compileSassFile" with
        | Some bound ->
            // If there's a value with that name on the script try to grab it
            match bound.Value.ReflectionValue with
            // ensure it fits the signature we are expecting
            | :? FSharpFunc<string, string> as compileSassFile ->
              Some compileSassFile
            | _ -> None
        | None -> None

let content = File.ReadAllText("./path/to/sass-plugin.fsx")
// this is where it get's nice, we can also fetch the scritps from the cloud
// let! content = Http.getFromGithub("AngelMunoz/Perla.Sass")
match ScriptedContent.tryGetSassPluginFunction(content) with
| Some plugin ->
  let css = plugin "./path/to/file.scss"
  printfn $"Resulting CSS:\n{css}"
| None -> printfn "No plugin was found on the script"
```

This is more-less what I have in mind, it has a few downsides though

- Convention based naming
- Badly written plugins might leak memory or make Perla's performance to slow down
- Script distribution is a real concern, there's no clear way to do it as of now
- Security concerns when executing code with Perla's permissions on the user's behalf

And many others that I might not be looking after.

Being able to author plugins and process any kind of file into something Perla can use to enhance the consumer experience is just worth it though, for example just look at the vast amount of webpack and vite plugins. The use cases are there for anyone to fulfill them .

### HMR

This is the golden apple I'm not entirely sure how to tackle...
[There's an HMR spec] that I will follow for that since that's what snowpack/vite's HMR is based on, libraries like [Fable.Lit], or [Elmish.HMR] are working towards being compatible with vite's HMR, so if Perla can make it work like them, then we won't even need to write any specific code for Perla.

I can talk however of CSS HMR, This is a pretty simple change to support given that CSS changes are automatically propagated in the browser, it basically does half of the HMR for us.

Perla does the following:

- Sees `import "./app.css`
- Runs the `cssImport` middleware function I hinted at earlier and returns a ~CSS~ Javascript file that injects a script tag on the head of the page.

```fsharp
  let cssImport
    (mountedDirs: Map<string, string>)
    (ctx: HttpContext)
    (next: Func<Task>)
    =
    task {
      // skip non-css files
      if ctx.Request.Path.Value.Contains(".css") |> not then
        return! next.Invoke()
      else

        let logger = ctx.GetLogger("Perla Middleware")
        let path = ctx.Request.Path.Value

        let baseDir, baseName =
          mountedDirs
          |> Map.filter (fun _ v -> String.IsNullOrWhiteSpace v |> not)
          |> Map.toSeq
          |> Seq.find (fun (_, v) -> path.StartsWith(v))

        let filePath =
          let fileName =
            path.Replace($"{baseName}/", "", StringComparison.InvariantCulture)

          Path.Combine(baseDir, fileName)

        logger.LogInformation("Transforming CSS")

        let! content = File.ReadAllTextAsync(filePath)
        // return the JS code to insert the CSS content in a style tag
        let newContent =
          $"""
const css = `{content}`
const style = document.createElement('style')
style.innerHTML = css
style.setAttribute("filename", "{filePath}");
document.head.appendChild(style)"""

        ctx.SetContentType "text/javascript"
        do! ctx.WriteStringAsync newContent :> Task
    }
    :> Task
```

In the SSE handler function we observe for file changes in disk and depending on the content we do the corresponding update

```fsharp
watcher.FileChanged
|> Observable.map (fun event ->
  task {
    match Path.GetExtension event.name with
    | Css ->
      // a CSS file was changed, read all of the content
      let! content = File.ReadAllTextAsync event.path

      let data =
        Json.ToTextMinified(
          {| oldName =
              event.oldName
              |> Option.map (fun value ->
                match value with
                | Css -> value
                | _ -> "")
              name = event.path
              content = content |}
        )
      // Send the SSE Message to the client with the new CSS content
      do! res.WriteAsync $"event:replace-css\ndata:{data}\n\n"
      return! res.Body.FlushAsync()
    | Typescript
    | Javascript
    | Jsx
    | Json
    | Other _ -> //... other content ...
  })
|> Observable.switchTask
|> Observable.subscribe ignore
```

To handle these updates we use two cool things, [WebWorkers] and a simple scripts, the live reload script has this content

```javascript
// initiate worker
const worker = new Worker("/~perla~/worker.js");
// connect to the SSE endpoint
worker.postMessage({ event: "connect" });

function replaceCssContent({ oldName, name, content }) {
  const css = content?.replace(/(?:\\r\\n|\\r|\\n)/g, "\n") || "";
  const findBy = oldName || name;

  // find the style tag with the particular name
  const style = document.querySelector(`[filename="${findBy}"]`);
  if (!style) {
    console.warn("Unable to find", oldName, name);
    return;
  }
  // replace the content
  style.innerHTML = css;
  style.setAttribute("filename", name);
}

function showOverlay({ error }) {
  console.log("show overlay");
}

// react to the worker messages
worker.addEventListener("message", function ({ data }) {
  switch (data?.event) {
    case "reload":
      return window.location.reload();
    case "replace-css":
      return replaceCssContent(data);
    case "compile-err":
      return showOverlay(data);
    default:
      return console.log("Unknown message:", data);
  }
});
```

Inside our Worker the code is very very similar

```javascript
let source;

const tryParse = (string) => {
  try {
    return JSON.parse(string) || {};
  } catch (err) {
    return {};
  }
};

function connectToSource() {
  if (source) return;
  //connect to the SSE endpoint
  source = new EventSource("/~perla~/sse");
  source.addEventListener("open", function (event) {
    console.log("Connected");
  });
  // react to file reloads
  source.addEventListener("reload", function (event) {
    console.log("Reloading, file changed: ", event.data);
    self.postMessage({
      event: "reload",
    });
  });
  // if the server sends a `replace-css` event
  // notify the main thread about it
  // Yes! web workers run on background threads!
  source.addEventListener("replace-css", function (event) {
    const { oldName, name, content } = tryParse(event.data);
    console.log(`Css Changed: ${oldName ? oldName : name}`);
    self.postMessage({
      event: "replace-css",
      oldName,
      name,
      content,
    });
  });

  source.addEventListener("compile-err", function (event) {
    const { error } = tryParse(event.data);
    console.error(error);
    self.postMessage({
      event: "compile-err",
      error,
    });
  });
}

self.addEventListener("message", function ({ data }) {
  if (data?.event === "connect") {
    connectToSource();
  }
});
```

And that's how the CSS HMR works in Perla and it is instant, in less than a blink of an eye! Well... maybe not but pretty close to it.

For the JS side I'm still not sure how this will work given that I might need to have a mapping in both sides of the files I have and what is their current version.

## What's next?

Whew! That was a lot! but shows how to build each part of the Webpack alternative I've been working on Called [Perla] there are still some gaps though

- Project Scaffolding

  This will be an important step for adoption I believe, generating certain files, or even starter projects to reduce the onboarding complexity is vital, so this is likely the next step for me (even before the HMR)

- Unit/E2E Testing

  Node based test runners won't work naturally since we're not using node! So this is an area to investigate, for E2E I already have the thought of using playwright, for unit tests I'm not sure yet but I guess I'd be able to pick something similar or simply have a test framework that runs entirely on the browser.

- Import map ergonomics

  Some times, you must edit by hand the import map (`perla.jsonc.lock`) to get dependencies like `import fp from 'lodash/fp'` with the import maps the browser knows what to do with `lodash` but not `lodash/fp` so an edit must be made, this requires you to understand how these dependencies work and how you need to write the import map, it's an area I'd love to make as simple as possible

- Typescript Types for dependencies

  Typescript (and related Editors/IDEs like vscode and webstorm) rely on the presence of node_modules to pick typings from disk, It would be nice if typescript worked with the URI style imports for typings that would fix a lot of issues.

- Library/Build only mode

  There might be certain cases where you would like to author a library either for you and your team, perhaps you only need the JS files and a package.json to share the sources, while not a priority it's something it's worth looking at.

- Improve the _Install_ story

  The goal is run a single line command, get your stuff installed in place regardless of if you have .NET or not

## Closing thoughts...

So those are some of the things I might have on my plate for next, of course if I receive feedback on this project I may prioritize some things over the others but rather than doing it all for myself, I wish to share this and make it a community effort to continue to improve the Frontend tooling story that doesn't rely on complex patterns and extremely weird and cryptic errors, hundreds of hours spent in configuration, something as simple that you feel confident enough to trust and use :)

> by the way, this is the repository in question :)
>
> [AngelMunoz/Perla](https://github.com/AngelMunoz/Perla)

And with all due respect, I really thank the maintainers of snowpack,esbuild, and vite who make an incredible job at reducing the complexity of frontend tooling as well, they inspired me AND if you're a loving node user please look at their projects ditch webpack enough so they also reflect back and simplify their setups!

For the .NET community I wish to spark a little of interest to look outside and build tooling for other communities, I think it is a really nice way to introduce .NET with the rest of the world and new devs be it with F#, C# or VB, I think .NET is an amazing platform for that. This has been a really good learning exercise and at the same time a project I believe in so, I will try to spend more time and push it to as much as I can.

I'll see you on the next one :)
