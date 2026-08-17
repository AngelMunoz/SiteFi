/// Markdown conversion pipeline + article navigation extraction (TOC, reading time).
/// Pipeline preserved from SiteFi (Main.fs Markdown module).
module Blogo.Markdown

open System
open System.Text
open Markdig
open Markdig.Syntax
open Markdig.Syntax.Inlines
open Markdig.Renderers.Html

let pipeline =
  MarkdownPipelineBuilder()
    .UsePipeTables()
    .UseGridTables()
    .UseListExtras()
    .UseEmphasisExtras()
    .UseGenericAttributes()
    .UseAutoLinks()
    .UseTaskLists()
    .UseMediaLinks()
    .UseCustomContainers()
    .UseMathematics()
    .UseEmojiAndSmiley()
    .UseYamlFrontMatter()
    .UseAdvancedExtensions()
    .UseAutoIdentifiers()
    .UsePreciseSourceLocation()
    .UseSmartyPants()
    .Build()

let convert(content: string) = Markdown.ToHtml(content, pipeline)

type TocEntry = { Level: int; Id: string; Text: string }

let rec private inlineText(``inline``: Inline) : string =
  match ``inline`` with
  | null -> ""
  | :? LiteralInline as lit -> lit.Content.ToString()
  | :? CodeInline as code -> code.Content
  | :? ContainerInline as container ->
    container |> Seq.map inlineText |> String.concat ""
  | _ -> ""

/// GitHub-style slug, used as a fallback if the AutoIdentifiers
/// extension didn't assign an id to a heading.
let private slugify(text: string) =
  let sb = StringBuilder()

  for c in text.ToLowerInvariant() do
    if Char.IsLetterOrDigit c then
      sb.Append c |> ignore
    elif c = ' ' || c = '-' then
      sb.Append '-' |> ignore

  sb.ToString()

/// Extract a table of contents (h1..h3) from the markdown source.
let toc(content: string) : TocEntry list =
  let doc = Markdown.Parse(content, pipeline)

  [
    for heading in doc.Descendants<HeadingBlock>() do
      if heading.Level <= 3 then
        let text = inlineText heading.Inline

        let id =
          match heading.GetAttributes() with
          | null -> slugify text
          | attrs ->
            match attrs.Id with
            | null -> slugify text
            | id -> id

        {
          Level = heading.Level
          Id = id
          Text = text
        }
  ]

/// Rough reading time in minutes (200 wpm, minimum 1).
let readingTime(content: string) : int =
  let plain = Markdown.ToPlainText(content, pipeline)

  let words =
    plain.Split(
      [| ' '; '\t'; '\n'; '\r' |],
      StringSplitOptions.RemoveEmptyEntries
    )

  max 1 (int(Math.Ceiling(float words.Length / 200.0)))
