/// Static generation: enumerates every page the site needs (ported from
/// SiteFi's IWebsite.Actions logic), renders it with Hox and writes it out,
/// then copies assets and writes feeds/sitemap/robots.
module Blogo.Generator

open System
open System.IO
open Hox
open Hox.Rendering
open Blogo
open Blogo.Config

let private writePage (buildDir: string) (path: string) (node: Hox.Core.Node) =
  let rel = if path = "/" then "index.html" else path.TrimStart('/')
  let full = Path.Combine(buildDir, rel)
  Directory.CreateDirectory(Path.GetDirectoryName full) |> ignore

  use fs = File.Create full
  Render.toStream(node, fs) |> _.GetAwaiter().GetResult()

let private copyAssets (assetsDir: string) (buildDir: string) =
  if Directory.Exists assetsDir then
    for file in
      Directory.EnumerateFiles(assetsDir, "*", SearchOption.AllDirectories) do
      let rel = Path.GetRelativePath(assetsDir, file)
      let dest = Path.Combine(buildDir, rel)
      Directory.CreateDirectory(Path.GetDirectoryName dest) |> ignore
      File.Copy(file, dest, true)

let private displayName (config: Config) (user: string) =
  if String.IsNullOrEmpty user then
    config.MasterUserDisplayName
  elif Map.containsKey user config.Users then
    config.Users.[user]
  else
    user

/// Builds the whole site into buildDir. Returns the number of pages written.
let buildAll (contentDir: string) (assetsDir: string) (buildDir: string) =
  let sw = Diagnostics.Stopwatch.StartNew()
  let config =
    Config.read(Path.Combine(contentDir, "config.yml"))
    |> Config.withSnippets contentDir

  if config.UrlMode <> "legacy" then
    eprintfn
      "warning: urlMode '%s' is not supported yet, using 'legacy'."
      config.UrlMode

  let articles = Content.read(Path.Combine(contentDir, "posts"))

  if Directory.Exists buildDir then
    Directory.Delete(buildDir, true)

  Directory.CreateDirectory buildDir |> ignore

  let languages =
    articles
    |> Map.toList
    |> List.map(fun (_, art) -> URL_LANG config art.Language)
    |> Set.ofList
    |> Set.toList

  let categories =
    articles
    |> Map.toList
    |> List.collect(fun (_, article) -> article.Categories)
    |> Set.ofList
    |> Set.toList

  let users =
    articles |> Map.toList |> List.map(fst >> fst) |> Set.ofList |> Set.toList

  let mutable sitemapPages = []

  let write path node =
    writePage buildDir path node
    printfn "  %s" path

  // Home pages, one per language in use
  for language in languages do
    let path = Urls.LANG language

    write
      (if path = "/" then "/" else path)
      (Templates.listingPage
        config
        articles
        language
        config.Title
        config.Description
        (fun _ article -> language = URL_LANG config article.Language))

    sitemapPages <- ((if path = "/" then "/" else path), None) :: sitemapPages

  // Article pages
  for ((user, slug), article) in Map.toList articles do
    write article.Url (Templates.articlePage config articles article)
    sitemapPages <- (article.Url, Some article.Date) :: sitemapPages

  // User home pages
  for user in users do
    let path = Urls.USER_URL user

    write
      path
      (Templates.listingPage
        config
        articles
        ""
        (displayName config user)
        ""
        (fun (u, _) _ -> user = u))

    sitemapPages <- (path, None) :: sitemapPages

  // Category pages (per category x language combination that has articles)
  for category in categories do
    for language in languages do
      let hasArticles =
        articles
        |> Map.toList
        |> List.exists(fun (_, art: Content.Article) ->
          language = URL_LANG config art.Language
          && List.contains category art.Categories)

      if hasArticles then
        let path = Urls.CATEGORY category language

        write
          path
          (Templates.listingPage
            config
            articles
            language
            (sprintf "\"%s\"" category)
            "Filtered articles"
            (fun _ article ->
              language = URL_LANG config article.Language
              && List.contains category article.Categories))

        sitemapPages <- (path, None) :: sitemapPages

  // Feeds
  Feeds.rss config articles
  |> fun d -> d.Save(Path.Combine(buildDir, "feed.rss"))

  Feeds.atom config articles
  |> fun d -> d.Save(Path.Combine(buildDir, "feed.atom"))

  printfn "  /feed.rss"
  printfn "  /feed.atom"

  // SEO
  Seo.sitemap config (List.rev sitemapPages)
  |> fun d -> d.Save(Path.Combine(buildDir, "sitemap.xml"))

  File.WriteAllText(Path.Combine(buildDir, "robots.txt"), Seo.robots config)
  printfn "  /sitemap.xml"
  printfn "  /robots.txt"

  // llms.txt convention
  File.WriteAllText(Path.Combine(buildDir, "llms.txt"), Llms.index config articles)
  File.WriteAllText(Path.Combine(buildDir, "llms-full.txt"), Llms.full config articles)
  printfn "  /llms.txt"
  printfn "  /llms-full.txt"

  // Static assets
  copyAssets assetsDir buildDir

  sw.Stop()

  printfn
    "Done: %i articles, %i pages written in %ims"
    (Map.count articles)
    sitemapPages.Length
    sw.ElapsedMilliseconds
