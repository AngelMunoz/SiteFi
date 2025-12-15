module Client

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Notation

module Highlight =
  open WebSharper.HighlightJS

  let private RegisterLanguages() =
    Hljs.RegisterLanguage("fsharp", Language.Fsharp)
    Hljs.RegisterLanguage("javascript", Language.Javascript)
    Hljs.RegisterLanguage("typescript", Language.Typescript)
    Hljs.RegisterLanguage("css", Language.Css)
    Hljs.RegisterLanguage("html", Language.Xml)
    Hljs.RegisterLanguage("xml", Language.Xml)
    Hljs.RegisterLanguage("json", Language.Json)
    Hljs.RegisterLanguage("sql", Language.Sql)

  let Run() =
    JS.ImportFile "highlight.js/styles/atom-one-light.min.css"
    RegisterLanguages()
    Hljs.HighlightAll()

module Bulma =

  let DrawerShown = Var.Create false

  [<JavaScriptExport>]
  let ToggleDrawer() = DrawerShown.Update not

  let HookDrawer() =
    DrawerShown.View
    |> View.Sink(fun shown ->
      JS.Document
        .QuerySelectorAll(".drawer-backdrop, .lhs-drawer")
        .ForEach(
          (fun (node, _, _, _) ->
            let node = node :?> Dom.Element

            "shown"
            |> if shown then node.ClassList.Add else node.ClassList.Remove),
          JS.Undefined
        ))

[<SPAEntryPoint>]
let Main() =
  Bulma.HookDrawer()
  Highlight.Run()

[<assembly: JavaScript>]
do ()
