/// Site-wide configuration, read from content/config.yml.
/// Format preserved from SiteFi (ported from Main.fs Site.ReadConfig).
module Blogo.Config

open System
open System.IO
open Blogo

[<CLIMutable>]
type RawConfig = {
  serverUrl: string
  shortTitle: string
  title: string
  description: string
  masterUserDisplayName: string
  masterLanguage: string
  languages: string
  users: string
  // New, optional:
  fediverseCreator: string
  urlMode: string
  issuesUrl: string
}

type Config = {
  ServerUrl: string
  ShortTitle: string
  Title: string
  Description: string
  MasterUserDisplayName: string
  MasterLanguage: string
  Languages: Map<string, string>
  Users: Map<string, string>
  FediverseCreator: string
  UrlMode: string
  IssuesUrl: string
  // Raw-HTML snippets loaded from content/snippets/ (see withSnippets)
  ExtraHead: string
  ExtraFooter: string
  ExtraBodyEnd: string
}

/// Zero out if article has the master language
let URL_LANG (config: Config) lang =
  if config.MasterLanguage = lang then "" else lang

let private KEY_VALUE_LIST whatFor ss =
  (Helpers.NULL_TO_EMPTY ss).Split([| "," |], StringSplitOptions.None)
  |> Array.choose(fun s ->
    if String.IsNullOrEmpty s then
      None
    else
      let parts = s.Split([| "->" |], StringSplitOptions.None)

      if Array.length parts <> 2 then
        eprintfn
          "warning: Incorrect key-value format for substring [%s] in [%s] for [%s], ignoring."
          s
          ss
          whatFor

        None
      else
        Some(parts.[0].Trim(), parts.[1].Trim()))
  |> Set.ofArray
  |> Set.toList
  |> Map.ofList

let read(path: string) =
  if File.Exists path then
    let config = Yaml.OfYaml<RawConfig>(File.ReadAllText path)

    let languages = KEY_VALUE_LIST "languages" config.languages

    let users = KEY_VALUE_LIST "users" config.users

    {
      ServerUrl = Helpers.NULL_TO_EMPTY config.serverUrl
      ShortTitle = Helpers.NULL_TO_EMPTY config.shortTitle
      Title = Helpers.NULL_TO_EMPTY config.title
      Description = Helpers.NULL_TO_EMPTY config.description
      MasterUserDisplayName = Helpers.NULL_TO_EMPTY config.masterUserDisplayName
      MasterLanguage = Helpers.NULL_TO_EMPTY config.masterLanguage
      Languages = languages
      Users = users
      FediverseCreator = Helpers.NULL_TO_EMPTY config.fediverseCreator
      UrlMode =
        match Helpers.NULL_TO_EMPTY config.urlMode with
        | "" -> "legacy"
        | other -> other
      IssuesUrl = Helpers.NULL_TO_EMPTY config.issuesUrl
      ExtraHead = ""
      ExtraFooter = ""
      ExtraBodyEnd = ""
    }
  else
    {
      ServerUrl = "http://localhost:5000"
      ShortTitle = "My Blog"
      Title = "My F# Blog"
      Description = "TODO: write the description of this blog"
      MasterUserDisplayName = "My Name"
      MasterLanguage = "en"
      Languages = Map.ofList [ "en", "English" ]
      Users = Map.empty
      FediverseCreator = ""
      UrlMode = "legacy"
      IssuesUrl = ""
      ExtraHead = ""
      ExtraFooter = ""
      ExtraBodyEnd = ""
    }

/// Loads optional raw-HTML snippets from content/snippets/{head,footer,body-end}.html.
/// These are injected verbatim into every page (trusted content).
let withSnippets (contentDir: string) (config: Config) =
  let readSnippet name =
    let path = Path.Combine(contentDir, "snippets", name + ".html")

    if File.Exists path then File.ReadAllText path else ""

  {
    config with
      ExtraHead = readSnippet "head"
      ExtraFooter = readSnippet "footer"
      ExtraBodyEnd = readSnippet "body-end"
  }
