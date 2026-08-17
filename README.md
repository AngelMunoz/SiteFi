# Blogo — your F# static blog generator

Blogo is a small, dependency-light static blog generator written in F#.
It renders markdown posts to a fully static HTML site using
[Hox](https://github.com/AngelMunoz/Hox) for HTML composition,
[Markdig](https://github.com/xoofx/markdig) for markdown, and
[YamlDotNet](https://github.com/aaubry/YamlDotNet) for front matter/config.

It is the successor of the WebSharper-based SiteFi fork: same posts, same
URL scheme, same feeds — no WebSharper, no ASP.NET, no Node toolchain.

## Features

- Write markdown, get a standalone static blog — no coding required
- Full redesign with a brown/teal theme, dark mode (`prefers-color-scheme`
  + manual toggle, persisted)
- Multilingual articles with a language selector and per-language home pages
- Categories/tags with per-category (and per-language) listing pages
- Multiple authors: each gets a subfolder under `content/posts`
- Article pages with auto table of contents, heading anchors and reading time
- RSS 2.0 and Atom 1.0 feeds
- SEO basics: `sitemap.xml`, `robots.txt`, Open Graph / Twitter card meta,
  canonical URLs, feed `<link rel="alternate">` discovery
- `llms.txt` + `llms-full.txt` per the [llmstxt.org](https://llmstxt.org)
  convention (index of all posts; full markdown source inlined)
- Syntax highlighting via highlight.js (CDN) with a theme-matched palette
- Watch mode for authoring
- Raw-HTML snippets: drop `head.html`, `footer.html` or `body-end.html` into
  `content/snippets/` and they're injected verbatim into every page — this is
  how analytics/widgets (Firebase, ko-fi, bsky-widget) are wired in

## Dependencies

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- `dotnet-serve` (optional, for previewing): `dotnet tool install -g dotnet-serve`

## Building

```bash
./build.sh
# or
dotnet run --project src/Blogo -- build
```

Output goes to `build/`.

## Previewing

```bash
./serve.sh   # http://localhost:4300
```

## Authoring

Drop markdown files into `content/posts` named `YYYY-MM-DD-YourTitle.md`
(or `YYYYMMDD-YourTitle.md`). Front matter:

```yaml
---
title: A wonderful F# journey
subtitle: Optional subtitle
abstract: A brief description for listings and social cards
date: Optional override for the filename date
categories: fsharp, dotnet
language: es
---
```

While writing, run watch mode (regenerates on any content/asset change):

```bash
dotnet run --project src/Blogo -- watch
```

Keep `serve.sh` running in another terminal to preview.

## Configuration

`content/config.yml`:

| Property | What it is |
| --- | --- |
| `serverUrl` | Deployment URL, used for feeds/sitemap/canonical links |
| `shortTitle` | Title shown in the navigation bar |
| `title` | Home page title, feed title |
| `description` | Site description (feeds, meta, hero) |
| `masterUserDisplayName` | Default author's display name |
| `masterLanguage` | Default language key (e.g. `en`) |
| `languages` | `key->Label` pairs, e.g. `"en->English,es->Spanish"` |
| `users` | `username->Display Name` pairs (subfolders of `content/posts`) |
| `fediverseCreator` | Optional, e.g. `@you@hachyderm.io` |
| `issuesUrl` | Optional; enables the "Raise an issue!" block on articles |
| `urlMode` | Reserved, only `legacy` is supported |

## Deploying

`build/` is a plain static directory. `CNAME` and `.nojekyll` are already in
`assets/` for GitHub Pages; any static host works.

Firebase Hosting config is included (`firebase.json`, `.firebaserc`), with
CORS headers for the feeds. Deploy with:

```bash
firebase deploy
```

## Deferred

- Static search (Pagefind standalone binary, or a build-time JSON index +
  vanilla JS) — deliberately left out to keep zero npm anywhere.
- `urlMode: pretty` (`/blog/<slug>/index.html` style URLs).
