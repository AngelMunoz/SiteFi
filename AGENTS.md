# AGENTS.md

## What this is

Blogo: a static blog generator in F# (net10.0 console app). Markdown posts
in `content/posts` + `content/config.yml` → static HTML in `build/`.

Hard constraints:

- **No npm/npx/Node anywhere.** The only JS is the hand-written
  `assets/js/site.js`; the only CSS is the hand-written `assets/css/site.css`.
- NuGet dependencies are limited to **Hox**, **Markdig**, **YamlDotNet**.
- URL scheme is frozen ("legacy mode"): `/blog/<slug>.html`,
  `/user/<user>[/<slug>].html`, `/category/<cat>[/<lang>].html`,
  `/<lang>.html`, `/feed.rss`, `/feed.atom`. All URL logic lives in
  `src/Blogo/Urls.fs` — change URLs only there.

## Build / run

```bash
./build.sh                                # generate build/
dotnet run --project src/Blogo -- build # same
dotnet run --project src/Blogo -- watch # regenerate on content/asset changes
./serve.sh                                # preview at :4300 (needs dotnet-serve)
```

Run commands from the repository root — paths (`content/`, `assets/`,
`build/`) are resolved from the current working directory.

## Layout

- `src/Blogo/` — the generator, compile order matters (see fsproj):
  - `Urls.fs` — URL scheme (legacy).
  - `Helpers.fs` — filename/date parsing, `|ArticleFile|_|` active pattern.
  - `Yaml.fs` — front matter splitting + YamlDotNet deserialization.
  - `Config.fs` — `config.yml` model (`RawConfig` is CLIMutable; keep new
    optional keys nullable-tolerant via `Helpers.NULL_TO_EMPTY`).
  - `Markdown.fs` — Markdig pipeline, TOC extraction, reading time.
  - `Content.fs` — `Article` record + article store loading.
  - `Templates.fs` — all Hox components (layout, listing pages, article page).
  - `Feeds.fs` — RSS 2.0 / Atom 1.0 (XElement).
  - `Seo.fs` — sitemap.xml, robots.txt.
  - `Llms.fs` — llms.txt index + llms-full.txt (full post markdown).
  - `Generator.fs` — page enumeration + writing + asset copy.
  - `Program.fs` — CLI (`build` | `watch`).
- `content/posts/` — markdown articles (`YYYY-MM-DD-slug.md`).
- `content/snippets/` — optional `head.html` / `footer.html` / `body-end.html`
  raw-HTML snippets, injected into every page (loaded by
  `Config.withSnippets`).
- `assets/` — copied verbatim to `build/` (css, js, fonts, img, CNAME,
  manifest, .nojekyll).
- `firebase.json` / `.firebaserc` — Firebase Hosting deploy (public dir is
  `build/`, CORS headers for the feeds).

## Conventions

- Templates: Hox DSL — `h "tag.cls"` selectors for structure, `.attr()` for
  anything URL-ish, `text` (escaped) vs `raw` (trusted HTML only, e.g.
  Markdig output). List elements in Hox children need both branches of
  `if` — use `fragment []` for the empty case.
- The theme is defined once in `assets/css/site.css` with CSS custom
  properties; dark mode = `prefers-color-scheme` + `[data-theme]` override.
  Brown/teal palette — keep it.
- Posts with CRLF line endings exist; don't assume LF when parsing text.

## Verifying changes

After changes, run `./build.sh` and check:

- page count line ("Done: N articles, M pages") stays sane
- `xmllint --noout build/feed.rss build/feed.atom build/sitemap.xml`
- every `content/posts/*.md` has a matching `build/blog/<name>.html`
