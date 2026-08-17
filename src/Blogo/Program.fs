/// Blogo CLI: build | watch
/// Run from the repository root.
module Blogo.Program

open System
open System.IO
open System.Threading

let private contentDir =
  Path.Combine(Directory.GetCurrentDirectory(), "content")

let private assetsDir = Path.Combine(Directory.GetCurrentDirectory(), "assets")
let private buildDir = Path.Combine(Directory.GetCurrentDirectory(), "build")

let private build() =
  try
    Blogo.Generator.buildAll contentDir assetsDir buildDir
  with ex ->
    eprintfn "build failed: %s" ex.Message

/// Regenerate (debounced) whenever content or assets change.
let private watch() =
  build()

  use signal = new AutoResetEvent(false)

  let watchDir dir =
    let watcher = new FileSystemWatcher(dir, "*")

    watcher.IncludeSubdirectories <- true

    watcher.NotifyFilter <-
      NotifyFilters.FileName
      ||| NotifyFilters.LastWrite
      ||| NotifyFilters.DirectoryName

    let onChange _ = signal.Set() |> ignore

    watcher.Changed.Add onChange
    watcher.Created.Add onChange
    watcher.Deleted.Add onChange
    watcher.Renamed.Add(fun _ -> signal.Set() |> ignore)
    watcher.EnableRaisingEvents <- true
    watcher

  use w1 = watchDir contentDir
  use w2 = watchDir assetsDir

  printfn "Watching content/ and assets/ — press Enter to stop."

  let stop = new AutoResetEvent(false)

  // Not disposed: the task is still running when watch() returns.
  async {
    Console.In.ReadLine() |> ignore
    stop.Set() |> ignore
  }
  |> Async.StartAsTask
  |> ignore

  let mutable running = true

  while running do
    match
      WaitHandle.WaitAny([| signal :> WaitHandle; stop :> WaitHandle |], 300)
    with
    | 0 ->
      // debounce: drain any burst of events, then rebuild once
      Thread.Sleep 200

      while signal.WaitOne(0) do
        ()

      printfn "change detected, rebuilding..."
      build()
    | 1 -> running <- false
    | _ -> ()

  0

[<EntryPoint>]
let main args =
  match args with
  | [| "watch" |] -> watch()
  | [| "build" |]
  | [||] ->
    build()
    0
  | _ ->
    eprintfn "usage: blogo [build|watch]"
    1
