/// Article model + loading from content/posts (ported from SiteFi's Main.fs
/// Site.ReadArticles, with the fix that bodies go through the full Markdig
/// pipeline, and TOC/reading time are computed up front).
module Blogo.Content

open System
open System.IO
open Blogo

[<CLIMutable>]
type RawArticle = {
  title: string
  subtitle: string
  ``abstract``: string
  url: string
  content: string
  date: string
  categories: string
  language: string
}

type Article = {
  Title: string
  Subtitle: string
  Abstract: string
  Url: string
  Content: string
  /// The original markdown source (for llms-full.txt and similar outputs)
  RawContent: string
  Date: DateTime
  Categories: string list
  Language: string
  Toc: Markdown.TocEntry list
  ReadingTimeMinutes: int
}

/// The article store, mapping (user*slug) pairs to articles.
type Articles = Map<string * string, Article>

let read(root: string) : Articles =
  let readFolder user store =
    let folder =
      if String.IsNullOrEmpty user then
        root
      else
        Path.Combine(root, user)

    if Directory.Exists folder then
      Directory.EnumerateFiles(folder, "*.md", SearchOption.TopDirectoryOnly)
      |> Seq.toList
      |> List.choose Helpers.``|ArticleFile|_|``
      |> List.fold
        (fun map (fullpath, fname, (year, month, day), _slug, _extension) ->
          let header, content =
            File.ReadAllText fullpath |> Yaml.SplitIntoHeaderAndContent

          let article = Yaml.OfYaml<RawArticle> header
          let title = Helpers.NULL_TO_EMPTY article.title
          let subtitle = Helpers.NULL_TO_EMPTY article.subtitle

          let ``abstract`` = Helpers.NULL_TO_EMPTY article.``abstract``

          let url = Urls.POST_URL(user, fname)

          // If the content is given in the header, use that instead.
          let rawContent =
            if article.content <> null then article.content else content

          let date =
            match Helpers.NULL_TO_EMPTY article.date with
            | "" -> DateTime(year, month, day)
            | dateStr ->
              match DateTime.TryParse dateStr with
              | true, dt -> dt
              | _ -> DateTime(year, month, day)

          let categories = Helpers.NULL_TO_EMPTY article.categories

          let categories =
            if not <| String.IsNullOrEmpty categories then
              categories.Split [| ',' |]
              // Note: categories are case-sensitive.
              |> Array.map(fun cat -> cat.Trim())
              |> Array.filter(not << String.IsNullOrEmpty)
              |> Set.ofArray
              |> Set.toList
            else
              []

          let language = Helpers.NULL_TO_EMPTY article.language

          Map.add
            (user, fname)
            {
              Title = title
              Subtitle = subtitle
              Abstract = ``abstract``
              Url = url
              Content = Markdown.convert rawContent
              RawContent = rawContent
              Date = date
              Categories = categories
              Language = language
              Toc = Markdown.toc rawContent
              ReadingTimeMinutes = Markdown.readingTime rawContent
            }
            map)
        store
    else
      if not(String.IsNullOrEmpty user) then
        eprintfn "warning: the posts folder (%s) does not exist." folder

      store

  if Directory.Exists root then
    Directory.EnumerateDirectories(root)
    // Read user articles
    |> Seq.fold
      (fun store folder -> readFolder (Path.GetFileName folder) store)
      Map.empty
    // Read main articles
    |> readFolder ""
  else
    eprintfn "warning: the posts folder (%s) does not exist." root
    Map.empty
