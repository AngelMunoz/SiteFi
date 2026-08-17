/// RSS 2.0 and Atom 1.0 feeds (based on SiteFi's Main.fs, hardened for
/// spec conformance: RFC 822/3339 dates, rel="self" links, permalink guids).
module Blogo.Feeds

open System
open System.Xml.Linq
open Blogo

// Utilities to make XML construction somewhat sane
[<AutoOpen>]
module private Xml =
  let TEXT(s: string) = XText(s)
  let (=>) (a1: string) (a2: string) = XAttribute(XName.Get a1, a2)
  let N = XName.Get

  let X (tag: XName) (attrs: XAttribute list) (content: obj list) =
    XElement(tag, List.map box attrs @ List.map box content)

let private sorted(articles: Content.Articles) =
  articles
  |> Map.toList
  |> List.sortByDescending(fun (_, article: Content.Article) ->
    article.Date.Ticks)

// For a simple but useful reference on Atom vs RSS content, refer to:
// https://www.intertwingly.net/wiki/pie/Rss20AndAtom10Compared
let atom (config: Config.Config) (articles: Content.Articles) : XDocument =
  let ns = XNamespace.Get "http://www.w3.org/2005/Atom"
  let articles = sorted articles

  let doc =
    X (ns + "feed") [] [
      X (ns + "title") [] [ TEXT config.Title ]
      X (ns + "subtitle") [] [ TEXT config.Description ]
      X (ns + "link") [
        "rel" => "alternate"
        "href" => config.ServerUrl
      ] []
      X (ns + "link") [
        "rel" => "self"
        "type" => "application/atom+xml"
        "href" => (config.ServerUrl + "/feed.atom")
      ] []
      X (ns + "updated") [] [ Helpers.ATOM_DATE DateTime.UtcNow ]
      X (ns + "id") [] [ TEXT config.ServerUrl ]
      X (ns + "author") [] [
        X (ns + "name") [] [ TEXT config.MasterUserDisplayName ]
      ]
      for ((user, slug), article) in articles do
        let permalink = config.ServerUrl + Urls.POST_URL(user, slug)

        X (ns + "entry") [] [
          X (ns + "title") [] [ TEXT article.Title ]
          X (ns + "link") [ "rel" => "alternate"; "href" => permalink ] []
          // Use permalink as id for entry
          X (ns + "id") [] [ TEXT permalink ]
          for category in article.Categories do
            X (ns + "category") [ "term" => category ] []
          if not (String.IsNullOrEmpty article.Abstract) then
            X (ns + "summary") [] [ TEXT article.Abstract ]
          X (ns + "updated") [] [ TEXT <| Helpers.ATOM_DATE article.Date ]
          X (ns + "published") [] [ TEXT <| Helpers.ATOM_DATE article.Date ]
          X (ns + "content") [ XAttribute(XName.Get "type", "html") ] [
            TEXT article.Content
          ]
        ]
    ]

  XDocument(doc)

let rss (config: Config.Config) (articles: Content.Articles) : XDocument =
  let contentNs = XNamespace.Get "http://purl.org/rss/1.0/modules/content/"
  let atomNs = XNamespace.Get "http://www.w3.org/2005/Atom"
  let articles = sorted articles

  let doc =
    X (N "rss") [
      "version" => "2.0"
      XAttribute(XNamespace.Xmlns + "content", contentNs.NamespaceName)
      XAttribute(XNamespace.Xmlns + "atom", atomNs.NamespaceName)
    ] [
      X (N "channel") [] [
        X (N "title") [] [ TEXT config.Title ]
        X (N "description") [] [ TEXT config.Description ]
        X (N "link") [] [ TEXT config.ServerUrl ]
        X (atomNs + "link") [
          "rel" => "self"
          "type" => "application/rss+xml"
          "href" => (config.ServerUrl + "/feed.rss")
        ] []
        X (N "language") [] [ TEXT config.MasterLanguage ]
        X (N "generator") [] [ TEXT "Blogo (F# + Hox)" ]
        X (N "lastBuildDate") [] [ Helpers.RSS_DATE DateTime.UtcNow ]
        for ((user, slug), article) in articles do
          let permalink = config.ServerUrl + Urls.POST_URL(user, slug)

          X (N "item") [] [
            X (N "title") [] [ TEXT article.Title ]
            X (N "link") [] [ TEXT permalink ]
            X (N "guid") [ "isPermaLink" => "true" ] [ TEXT permalink ]
            for category in article.Categories do
              X (N "category") [] [ TEXT category ]
            X (N "description") [] [ TEXT article.Abstract ]
            X (N "pubDate") [] [ TEXT <| Helpers.RSS_DATE article.Date ]
            X (contentNs + "encoded") [] [ TEXT article.Content ]
          ]
      ]
    ]

  XDocument(doc)
