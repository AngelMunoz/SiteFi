/// SEO outputs: sitemap.xml and robots.txt.
module Blogo.Seo

open System
open System.Xml.Linq
open Blogo.Config

/// lastmod per URL: article pages use the article date, the rest get none.
let sitemap
  (config: Config)
  (pages: (string * DateTime option) list)
  : XDocument =
  let ns = XNamespace.Get "http://www.sitemaps.org/schemas/sitemap/0.9"

  let X name (content: obj list) =
    XElement(ns + name, List.map box content)

  let urls = [
    for (path, lastmod) in pages ->
      let loc = X "loc" [ config.ServerUrl + path ]

      match lastmod with
      | Some dt -> X "url" [ loc; X "lastmod" [ dt.ToString("yyyy-MM-dd") ] ]
      | None -> X "url" [ loc ]
  ]

  XDocument(XElement(ns + "urlset", List.map box urls))

let robots(config: Config) : string =
  sprintf "User-agent: *\nAllow: /\nSitemap: %s/sitemap.xml\n" config.ServerUrl
