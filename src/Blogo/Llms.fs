/// llms.txt / llms-full.txt generation, following the llmstxt.org
/// convention (Jeremy Howard, 2024):
///
///   # Site Name
///   > One-paragraph description.
///   ## Section
///   - [Page title](url): one-line summary
///
/// llms.txt is a curated index; llms-full.txt inlines every post's
/// markdown source for single-file ingestion.
module Blogo.Llms

open System
open System.Text
open Blogo.Config

let private sorted (articles: Content.Articles) =
  articles
  |> Map.toList
  |> List.sortByDescending (fun (_, article: Content.Article) -> article.Date.Ticks)

/// /llms.txt — the index file.
let index (config: Config) (articles: Content.Articles) : string =
  let sb = StringBuilder()

  sb.AppendLine("# " + config.Title) |> ignore
  sb.AppendLine() |> ignore
  sb.AppendLine("> " + config.Description) |> ignore
  sb.AppendLine() |> ignore

  sb.AppendLine(
    sprintf
      "Blog by %s. Articles are mostly about F#, .NET and frontend development."
      config.MasterUserDisplayName
  )
  |> ignore

  sb.AppendLine() |> ignore
  sb.AppendLine("## Posts") |> ignore
  sb.AppendLine() |> ignore

  for ((user, slug), article) in sorted articles do
    let url = config.ServerUrl + Urls.POST_URL (user, slug)

    let summary =
      if String.IsNullOrEmpty article.Abstract then
        sprintf "%s" (article.Date.ToString("yyyy-MM-dd"))
      else
        sprintf "%s (%s)" article.Abstract (article.Date.ToString("yyyy-MM-dd"))

    sb.AppendLine(sprintf "- [%s](%s): %s" article.Title url summary)
    |> ignore

  sb.AppendLine() |> ignore
  sb.AppendLine("## Optional") |> ignore
  sb.AppendLine() |> ignore
  sb.AppendLine(sprintf "- [RSS 2.0 feed](%s/feed.rss)" config.ServerUrl)
  |> ignore
  sb.AppendLine(sprintf "- [Atom 1.0 feed](%s/feed.atom)" config.ServerUrl)
  |> ignore
  sb.AppendLine(sprintf "- [Sitemap](%s/sitemap.xml)" config.ServerUrl)
  |> ignore

  sb.ToString()

/// /llms-full.txt — every post's markdown source, newest first.
let full (config: Config) (articles: Content.Articles) : string =
  let sb = StringBuilder()

  sb.AppendLine("# " + config.Title + " — full content") |> ignore
  sb.AppendLine() |> ignore

  for ((user, slug), article) in sorted articles do
    sb.AppendLine(sprintf "---") |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine("# " + article.Title) |> ignore
    sb.AppendLine() |> ignore

    sb.AppendLine(
      sprintf
        "URL: %s%s\nDate: %s"
        config.ServerUrl
        (Urls.POST_URL (user, slug))
        (article.Date.ToString("yyyy-MM-dd"))
    )
    |> ignore

    sb.AppendLine() |> ignore
    sb.AppendLine(article.RawContent) |> ignore
    sb.AppendLine() |> ignore

  sb.ToString()
