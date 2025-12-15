# SiteFi - your F# static site generator

![](docs/vscode01.gif)

SiteFi is a simple and highly configurable static site generator for F#. It uses [WebSharper](https://websharper.com) to build the pages of your website and to generate HTML files for them.

## Key features

 * Add your Markdown articles and build, no coding is necessary
 * Get a full, standalone HTML blog, ready to deploy
 * Write in multiple languages, and get a language selector and main pages for each
 * Tag your articles with categories, and get listing pages for each
 * Have multiple authors? No problem! Each get a separate folder for articles.
 * RSS 2.0 and Atom 1.0 feeds
 * Syntax highlighting for F# code blocks (using Highlight.js)
 * Develop dynamic articles in F#, with charts, visualizations, etc.
 * Streamlined workflow for template changes (style, layout, etc.) - see effects immediately, and only rebuild when you are done

# 1. Configuring your blog

The main configuration file is located in `src/Hosted/config.yml`. Use this file to configure various site-wide aspects such as the URL you are deploying your blog to, your name, the default language for your articles, and RSS/Atom feed info.

```text
serverUrl: http://mywebsite.com
shortTitle: My Blog
title: My F# Blog
description: My ramblings and experiences with building stuff with F#
masterUsername: My Name
masterLanguage: en
languages: "en->English,es->Spanish,hu->Hungarian"
users: "extra1->My Friend's Name"
```

| Property      | What it is |
|:------------- |:--------------|
| `serverUrl`   | The URL the site will be deployed under. *Default*: `"http://localhost:5000"` (the port used by the `Hosted` project.) |
| `shortTitle`  | The title of the site, used in the main navigation bar. *Default*: `"My Blog"`|
| `title`       | The title of the home page and the RSS/Atom feeds. *Default*: `"My F# Blog"`|
| `description` | The description of the site, used in the RSS/Atom feeds. *Default*: `"TODO: write the description of this blog"`|
| `masterUserDisplayName` | The default user's display name. Set this to your name, your company name, or whatever. *Default*: `"My Name"`|
| `masterLanguage` | The default language. Use a key value, such as `"en"`. Languages won't show up until you start using at least two languages. *Default*: `"en"` |
| `languages`      |  A comma-separated list of mappings from language keys to display names. *Default*: `"en->English"` |
| `users`      |  A comma-separated list of mappings from usernames to display names. Usernames correspond to subfolders under the main posts directory. *Default*: `"jsmith->John Smith"` |

# 2. Building and running your blog

## Dependencies

- [Node.js](https://nodejs.org/en/) and [pnpm](https://pnpm.io/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer)

## Building the repository

1.  **Install Dependencies**: Run `pnpm install` in the `src/Hosted` directory to install JavaScript dependencies (including Highlight.js and esbuild).
    ```bash
    cd src/Hosted
    pnpm install
    cd ../..
    ```

2.  **Build**: In the root folder, run `dotnet build`. This command:
    *   Builds the `Client` project (F# -> JS).
    *   Bundles the client-side code and dependencies using `esbuild`.
    *   Builds the `Hosted` project.
    *   Builds the `Website` project, which generates the static HTML files in the `build` directory.

    ```bash
    dotnet build SiteFi.sln
    ```

    Alternatively, you can use the `build.sh` script (on Linux/macOS) or `build.ps1` (on Windows) which handles dependency installation and building for you.

    ```bash
    ./build.sh
    ```

## Previewing

To preview the generated static site:

1.  Run the `serve.sh` script (Linux/macOS) or `serve.cmd` (Windows). This uses `dotnet-serve` to host the contents of the `build` directory.
    ```bash
    ./serve.sh
    ```
    This will start a local server, usually at `http://localhost:4300`.

## Cleaning

You can clean your solution with `dotnet clean` from the root folder. This removes generated artifacts, including the `build` folder.

# 3. Writing your articles

The markdown files for your articles are in `src/Hosted/posts`. Add your `.md` files to this folder with the naming convention `YYYY-MM-DD-YourArticleTitle.md` or `YYYYMMDD-YourArticleTitle.md`. Give at least the title in the YAML header, as follows:

```yaml
---
title: A wonderful F# journey
subtitle: The best path to getting my F# blog up and running
---
```

You can use the following properties in the YAML header:

| Property      | What it is |
|:------------- |:--------------|
| `title`       | The title of the article. |
| `subtitle`    | The subtitle of the article. |
| `abstract`    | A brief description of the article. |
| `date`        | The date of the article, overriding the date given through the filename. |
| `categories`  | The comma-separated list of categories/tags of the article. |
| `language`    | The language code of the article. You can map these to language labels in `config.yml`.|

After adding or modifying an article, run `dotnet build SiteFi.sln` to regenerate the static site in the `build` folder.

## Multilingual articles

Use the `language` property in your article header to mark the language (e.g., `"en"`, `"es"`). Map these keys to display names in `config.yml` under the `languages` setting. The `masterLanguage` setting specifies the default language key.

## Multiple authors

By default, the `src/Hosted/posts` folder contains the master user's articles. Additional authors can have their own subfolders within `posts`. Configure their display names using the `users` setting in `config.yml`.

Example structure:
```text
posts
   |-- john
       2020-01-01-HappyNewYear.md
   2020-01-15-WebSharperSPAs.md
```

Config:
```yaml
users: "john->John Smith"
```

# Solution Structure

This repository contains three main projects:

*   **`src/Client`**: A WebSharper F# project that compiles to JavaScript. It handles client-side interactivity, such as the drawer menu and syntax highlighting.
    *   **Syntax Highlighting**: Uses [Highlight.js](https://highlightjs.org/) via `WebSharper.HighlightJS`.
    *   **Styles**: Styles are managed via SCSS in `src/Hosted/scss` and compiled during the build. Highlight.js themes are imported dynamically.

*   **`src/Hosted`**: An ASP.NET Core application that acts as the "host" for development and the source of truth for the static generation.
    *   **Bundling**: Uses `esbuild` (via `pnpm` scripts) to bundle the WebSharper-generated JavaScript and third-party libraries (like Highlight.js) into a single optimized file (`Client.bundled.js`).
    *   **Live Development**: You can run this project with `dotnet run --project src/Hosted/Hosted.fsproj` to see changes without full static regeneration (note: some changes might still require a rebuild).

*   **`src/Website`**: A WebSharper Sitelet project responsible for **static site generation**.
    *   **Output**: Generates the full HTML site in the `build` directory.
    *   **Feeds**: Automatically generates `feed.rss` and `feed.atom`.

# Appendix - Image credit

* `src/Hosted/img/Banner.jpg` by Plush Design Studio on Unsplash - https://unsplash.com/photos/UHqfUTDmdC4

