/// Page templates as Hox components (replaces SiteFi's WebSharper UI
/// MainTemplate/index.html templating). Full redesign: semantic HTML,
/// brown/teal "cozy terminal" theme defined in assets/css/site.css.
///
/// Everything here is built from small single-purpose components; compose
/// them instead of growing deep node trees.
module Blogo.Templates

open System
open Hox
open Hox.Core
open Blogo
open Blogo.Config

/// Everything the layout needs to know about the page being rendered.
type PageInfo = {
  Title: string option
  Description: string
  CanonicalPath: string // site-relative, e.g. /blog/x.html
  OgType: string // "website" | "article"
  Published: DateTime option
  /// "" for the master language, otherwise the language key (es, ...)
  LangKey: string
}

// ---------------------------------------------------------------------------
// DSL helpers: selector strings only carry classes/ids, anything with
// URL-ish values goes through .attr().
// ---------------------------------------------------------------------------

let private el (selector: string) (children: Node seq) = h (selector, children)

let private link (href: string) (cls: string) (children: Node seq) =
  let cls =
    match cls with
    | "" -> ""
    | cls -> "." + cls.Replace(' ', '.')

  (h ($"a{cls}", children)).attr ("href", href)

let private meta (name: string) (content: string) =
  (h "meta").attr("name", name).attr ("content", content)

let private metaProp (property: string) (content: string) =
  (h "meta").attr("property", property).attr ("content", content)

/// Fill-based inline SVG icon (icons are decorative: aria-hidden).
let private svgFill (path: string) =
  (h ("svg.icon[aria-hidden=true]", (h "path").attr ("d", path)))
    .attr ("viewBox", "0 0 24 24")

/// Stroke-based inline SVG icon.
let private svgStroke (path: string) =
  (h ("svg.icon-stroke[aria-hidden=true]", (h "path").attr ("d", path)))
    .attr ("viewBox", "0 0 24 24")

let private ICON_MENU = svgStroke "M3 6h18M3 12h18M3 18h18"

let private ICON_RSS =
  svgFill
    "M4 4a16 16 0 0 1 16 16h-3A13 13 0 0 0 4 7V4zm0 6a10 10 0 0 1 10 10h-3a7 7 0 0 0-7-7v-3zm2 6a2 2 0 1 1 0 4 2 2 0 0 1 0-4z"

let private formatLang (langopt: string) =
  if String.IsNullOrWhiteSpace langopt then "en-GB"
  elif langopt = "en" then "en-GB"
  else "es-MX"

// ---------------------------------------------------------------------------
// Shared fragments
// ---------------------------------------------------------------------------

/// A <time> element with a machine-readable datetime attribute.
let private postDate (date: DateTime) =
  (h ("time", text (Helpers.FORMATTED_DATE date)))
    .attr ("datetime", date.ToString("yyyy-MM-dd"))

/// Inline list of category tags linking to the category pages.
let private categoryTags (config: Config) (langopt: string) (categories: string list) =
  if categories.IsEmpty then
    fragment []
  else
    el "p.card-tags" [
      for category in categories do
        link (Urls.CATEGORY category langopt) "tag" [ text ("#" + category) ]
        text " "
    ]

let private displayName (config: Config) (user: string) =
  if String.IsNullOrEmpty user then
    config.MasterUserDisplayName
  elif Map.containsKey user config.Users then
    config.Users.[user]
  else
    user

// ---------------------------------------------------------------------------
// Header
// ---------------------------------------------------------------------------

/// The top menu: Home + a "Latest" dropdown with the 5 most recent articles.
let private menuItems (articles: Content.Articles) =
  let latest =
    articles
    |> Map.toList
    |> List.sortByDescending (fun (_, article: Content.Article) -> article.Date.Ticks)
    |> List.truncate 5

  el "nav.site-nav" [
    link "/" "nav-link" [ text "Home" ]
    if not latest.IsEmpty then
      el "details.nav-dropdown" [
        el "summary" [ text "Latest" ]
        el "div.nav-dropdown-menu" [
          for (_, article) in latest do
            link article.Url "" [ text article.Title ]
        ]
      ]
    else
      fragment []
  ]

/// Language selector (only rendered when more than one language is in use).
let private languageSelector (config: Config) (articles: Content.Articles) (langopt: string) =
  let languages =
    articles
    |> Map.toList
    |> List.map (fun (_, art) -> art.Language)
    |> List.distinct
    // Filter out the master language
    |> List.filter (fun lang -> URL_LANG config lang |> (String.IsNullOrEmpty >> not))

  // Add back the default language IFF there is at least one other language
  let languages =
    let LANG lang =
      let langkey =
        if String.IsNullOrEmpty lang then config.MasterLanguage else lang

      if config.Languages.ContainsKey langkey then
        lang, config.Languages.[langkey]
      else
        lang, langkey

    if languages.Length > 0 then
      (LANG "") :: List.map LANG languages
    else
      []

  if languages.IsEmpty then
    fragment []
  else
    el "nav.lang-nav" [
      for (url_lang, lang) in languages do
        let cls = if langopt = url_lang then "lang-link is-active" else "lang-link"
        link (Urls.LANG url_lang) cls [ el "strong" [ text lang ] ]
    ]

let private navToggle =
  (h ("button.nav-toggle", ICON_MENU))
    .attr("aria-label", "Toggle navigation")
    .attr ("data-drawer-toggle", "")

let private rssLink =
  (link "/feed.rss" "icon-link" [ ICON_RSS ]).attr ("aria-label", "RSS feed")

let private themeToggle =
  (h "button#theme-toggle.icon-btn")
    .attr("aria-label", "Toggle dark mode")
    .attr ("type", "button")

let private siteHeader (config: Config) (articles: Content.Articles) (langopt: string) =
  h (
    "header.site-header",
    [
      el "div.site-header-inner" [
        navToggle
        link "/" "brand" [ text config.ShortTitle ]
        menuItems articles
        el "div.header-tools" [
          languageSelector config articles langopt
          rssLink
          themeToggle
        ]
      ]
    ]
  )

/// Mobile drawer: Home plus the latest articles.
let private drawer (articles: Content.Articles) =
  let latest =
    articles
    |> Map.toList
    |> List.sortByDescending (fun (_, article: Content.Article) -> article.Date.Ticks)
    |> List.truncate 5

  fragment [
    h "div.drawer-backdrop[data-drawer-close]"
    el "aside#drawer.drawer" [
      el "nav" [
        el "ul.drawer-list" [
          el "li" [ link "/" "" [ text "Home" ] ]
          for (_, article) in latest do
            el "li" [ link article.Url "" [ text article.Title ] ]
        ]
      ]
    ]
  ]

// ---------------------------------------------------------------------------
// Footer & scripts
// ---------------------------------------------------------------------------

let private siteFooter (config: Config) =
  h (
    "footer.site-footer",
    [
      el "p" [
        text (
          sprintf
            "© %i %s — "
            DateTime.UtcNow.Year
            config.MasterUserDisplayName
        )
        link "/feed.rss" "" [ text "RSS" ]
        text " · "
        link "/feed.atom" "" [ text "Atom" ]
      ]
      el "p.muted" [ text "Generated with Blogo (F# + Hox)" ]
      if not (String.IsNullOrEmpty config.ExtraFooter) then
        raw config.ExtraFooter
      else
        fragment []
    ]
  )

let private cdnScript (src: string) =
  (h "script").attr("src", src).attr ("defer", "")

let private pageScripts =
  fragment [
    cdnScript "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js"
    cdnScript "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/fsharp.min.js"
    cdnScript "/js/site.js"
  ]

// ---------------------------------------------------------------------------
// <head>
// ---------------------------------------------------------------------------

/// Open Graph / Twitter card / canonical meta for a page.
let private seoMeta (config: Config) (info: PageInfo) (title: string) (canonical: string) =
  fragment [
    metaProp "og:site_name" config.Title
    metaProp "og:title" title
    metaProp "og:description" info.Description
    metaProp "og:url" canonical
    metaProp "og:type" info.OgType
    match info.Published with
    | Some dt -> metaProp "article:published_time" (dt.ToString("yyyy-MM-dd"))
    | None -> fragment []
    meta "twitter:card" "summary"
    (h "link").attr("rel", "canonical").attr ("href", canonical)
  ]

/// Feed discovery tags.
let private feedLinkTags =
  fragment [
    (h "link")
      .attr("rel", "alternate")
      .attr("type", "application/rss+xml")
      .attr ("href", "/feed.rss")
    (h "link")
      .attr("rel", "alternate")
      .attr("type", "application/atom+xml")
      .attr ("href", "/feed.atom")
  ]

/// Set the theme before first paint to avoid a flash of the wrong mode.
let private themeInitScript =
  el "script" [
    raw
      "!function(){try{var t=localStorage.getItem('theme');if(t)document.documentElement.dataset.theme=t}catch(e){}}()"
  ]

let private headNodes (config: Config) (info: PageInfo) =
  let title =
    match info.Title with
    | None -> config.Title
    | Some t -> t + " | " + config.Title

  let canonical = config.ServerUrl + info.CanonicalPath

  [
    h "meta[charset=utf-8]"
    meta "viewport" "width=device-width, initial-scale=1.0"
    el "title" [ text title ]
    meta "description" info.Description
    if not (String.IsNullOrEmpty config.FediverseCreator) then
      meta "fediverse:creator" config.FediverseCreator
    else
      fragment []
    seoMeta config info title canonical
    feedLinkTags
    (h "link").attr("rel", "manifest").attr ("href", "/manifest.webmanifest")
    (h "link").attr("rel", "stylesheet").attr ("href", "/css/site.css")
    themeInitScript
    if not (String.IsNullOrEmpty config.ExtraHead) then
      raw config.ExtraHead
    else
      fragment []
  ]

// ---------------------------------------------------------------------------
// Page shell
// ---------------------------------------------------------------------------

/// Full page shell: <head> + header/drawer/footer around the page body.
let layout (config: Config) (articles: Content.Articles) (info: PageInfo) (body: Node) : Node =
  fragment [
    raw "<!DOCTYPE html>"
    (h (
      "html",
      [
        el "head" (headNodes config info)
        h (
          "body",
          [
            siteHeader config articles info.LangKey
            drawer articles
            el "main#main" [ body ]
            siteFooter config
            pageScripts
            if not (String.IsNullOrEmpty config.ExtraBodyEnd) then
              raw config.ExtraBodyEnd
            else
              fragment []
          ]
        )
      ]
    ))
      .attr ("lang", formatLang info.LangKey)
  ]

// ---------------------------------------------------------------------------
// Listing pages (home / category / user)
// ---------------------------------------------------------------------------

/// A single article card for listing pages.
let private articleCard (config: Config) (user: string) (article: Content.Article) =
  h (
    "article.card",
    [
      el "h2.card-title" [ link article.Url "" [ text article.Title ] ]
      el "p.card-meta" [
        text "by "
        link (Urls.USER_URL user) "" [ text (displayName config user) ]
        text " · "
        postDate article.Date
      ]
      if not (String.IsNullOrEmpty article.Abstract) then
        el "p.card-abstract" [ text article.Abstract ]
      else
        fragment []
      categoryTags config (URL_LANG config article.Language) article.Categories
    ]
  )

/// Cards for a filtered set of articles, newest first.
let private articleList
  (config: Config)
  (articles: Content.Articles)
  (filter: (string * string) -> Content.Article -> bool)
  =
  articles
  |> Map.toList
  |> List.filter (fun (key, article) -> filter key article)
  |> List.sortByDescending (fun (_, article: Content.Article) -> article.Date.Ticks)
  |> List.map (fun ((user, _), article) -> articleCard config user article)

let private heroSection (bannerTitle: string) (bannerSubtitle: string) =
  h (
    "section.hero",
    [
      el "div.hero-inner" [
        el "h1" [ text bannerTitle ]
        if not (String.IsNullOrEmpty bannerSubtitle) then
          el "p.hero-subtitle" [ text bannerSubtitle ]
        else
          fragment []
      ]
    ]
  )

/// Home / category / user listing page.
let listingPage
  (config: Config)
  (articles: Content.Articles)
  (langopt: string)
  (bannerTitle: string)
  (bannerSubtitle: string)
  (filter: (string * string) -> Content.Article -> bool)
  : Node =
  let body =
    fragment [
      heroSection bannerTitle bannerSubtitle
      h ("section.section", el "div.card-list" (articleList config articles filter))
    ]

  let info = {
    Title = None
    Description = config.Description
    CanonicalPath = Urls.LANG langopt
    OgType = "website"
    Published = None
    LangKey = langopt
  }

  layout config articles info body

// ---------------------------------------------------------------------------
// Article page
// ---------------------------------------------------------------------------

/// "On this page" table of contents for an article.
let private tocNav (article: Content.Article) =
  if article.Toc.IsEmpty then
    fragment []
  else
    el "nav.toc" [
      el "p.toc-title" [ text "On this page" ]
      el "ol.toc-list" [
        for entry in article.Toc do
          el $"li.toc-level-{entry.Level}" [
            link ("#" + entry.Id) "" [ text entry.Text ]
          ]
      ]
    ]

/// Sidebar list of every post, marking the current one.
let private allPostsNav (articles: Content.Articles) (current: Content.Article) =
  el "nav.all-posts" [
    el "p.toc-title" [ text "All posts" ]
    el "div.all-posts-list" [
      for (_, item) in
        articles
        |> Map.toList
        |> List.sortByDescending (fun (_, item: Content.Article) -> item.Date) do
        let cls = if item.Url = current.Url then "is-active" else ""
        link item.Url cls [ text item.Title ]
    ]
  ]

/// Title, subtitle, date + reading time, category tags.
let private postHeader (config: Config) (langopt: string) (article: Content.Article) =
  h (
    "header.post-header",
    [
      el "h1" [ text article.Title ]
      if not (String.IsNullOrEmpty article.Subtitle) then
        el "p.post-subtitle" [ raw article.Subtitle ]
      else
        fragment []
      el "p.post-meta" [
        postDate article.Date
        text (sprintf " · %i min read" article.ReadingTimeMinutes)
      ]
      categoryTags config langopt article.Categories
    ]
  )

/// The "raise an issue" block, only when issuesUrl is configured.
let private postFeedback (config: Config) (article: Content.Article) =
  if String.IsNullOrEmpty config.IssuesUrl then
    fragment []
  else
    h (
      "blockquote.post-feedback",
      [
        text "Is there something wrong? "
        (h ("a", text "Raise an issue!"))
          .attr(
            "href",
            config.IssuesUrl + "?title=" + Uri.EscapeDataString article.Title
          )
          .attr ("target", "_blank")
      ]
    )

/// A full article page with TOC + all-posts sidebar.
let articlePage (config: Config) (articles: Content.Articles) (article: Content.Article) : Node =
  let langopt = URL_LANG config article.Language

  let body =
    h (
      "div.post-layout",
      [
        h (
          "article.post",
          [
            postHeader config langopt article
            h ("section.post-content", raw article.Content)
            postFeedback config article
          ]
        )
        el "aside.post-sidebar" [
          tocNav article
          allPostsNav articles article
        ]
      ]
    )

  let info = {
    Title = Some article.Title
    Description =
      if String.IsNullOrEmpty article.Abstract then
        config.Description
      else
        article.Abstract
    CanonicalPath = article.Url
    OgType = "article"
    Published = Some article.Date
    LangKey = langopt
  }

  layout config articles info body
