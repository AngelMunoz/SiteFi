namespace Website

open System
open System.Xml.Linq
open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server

type EndPoint =
    | [<EndPoint "GET /">] Home of lang:string
    | [<EndPoint "GET /blog">] Article of slug:string
    | [<EndPoint "GET /category">] Category of string * lang:string
    | [<EndPoint "GET /feed.atom">] AtomFeed
    | [<EndPoint "GET /feed.rss">] RSSFeed
    | [<EndPoint "GET /refresh">] Refresh

// Utilities to make XML construction somewhat sane
[<AutoOpen>]
module Xml =
    let TEXT (s: string) = XText(s)
    let (=>) (a1: string) (a2: string) = XAttribute(XName.Get a1, a2)
    let N = XName.Get
    let X (tag: XName) (attrs: XAttribute list) (content: obj list) =
        XElement(tag, List.map box attrs @ List.map box content)

module Markdown =
    open Markdig

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
            .Build()

    let Convert content = Markdown.ToHtml(content, pipeline)

module Yaml =
    open System.Text.RegularExpressions
    open YamlDotNet.Serialization

    let SplitIntoHeaderAndContent (source: string) =
        let delimRE = Regex("^---\\w*\r?$", RegexOptions.Compiled ||| RegexOptions.Multiline)
        let searchFrom = if source.StartsWith("---") then 3 else 0
        let m = delimRE.Match(source, searchFrom)
        if m.Success then
            source.[searchFrom..m.Index-1], source.[m.Index + m.Length..]
        else
            "", source

    let OfYaml<'T> (yaml: string) =
        let deserializer = (new DeserializerBuilder()).Build()
        if String.IsNullOrWhiteSpace yaml then
            deserializer.Deserialize<'T>("{}")
        else
            let yaml = deserializer.Deserialize<'T>(yaml)
            eprintfn "DEBUG/YAML=%A" yaml
            yaml

// Helpers around blog URLs.
// These need to match the endpoint type of the main sitelet.
module Urls =
    let CATEGORY (cat: string) lang =
        if String.IsNullOrEmpty lang then
            sprintf "/category/%s" cat
        else
            sprintf "/category/%s/%s" cat lang
    let POST_URL (slug: string) = "/blog/" + slug + ".html"
    let LANG (lang: string) = sprintf "/%s" lang

module Helpers =
    open System.IO
    open System.Text.RegularExpressions

    let NULL_TO_EMPTY (s: string) = match s with null -> "" | t -> t

    let FORMATTED_DATE (dt: DateTime) = dt.ToString("MMM dd, yyyy")
    let ATOM_DATE (dt: DateTime) = dt.ToString("yyyy-MM-dd'T'HH:mm:ssZ")
    let RSS_DATE (dt: DateTime) = dt.ToString("ddd, dd MMM yyyy HH:mm:ss UTC")

    // Return (fullpath, filename-without-extension, (year, month, day), slug, extension)
    let (|ArticleFile|_|) (fullpath: string) =
        let filename = Path.GetFileName(fullpath)
        let filenameWithoutExt = Path.GetFileNameWithoutExtension(fullpath)
        let r = new Regex("([0-9]+)-([0-9]+)-([0-9]+)-(.+)\.(md)")
        if r.IsMatch(filename) then
            let a = r.Match(filename)
            let V (i: int) = a.Groups.[i].Value
            let I = Int32.Parse
            Some (fullpath, filenameWithoutExt, (I (V 1), I (V 2), I (V 3)), V 4, V 5)
        else
            None

module Site =
    open System.IO
    open WebSharper.UI.Html

    type MainTemplate = Templating.Template<"..\\hosted\\index.html", serverLoad=Templating.ServerLoad.WhenChanged>

    type [<CLIMutable>] RawConfig =
        {
            serverUrl: string
            title: string
            description: string
            masterUsername: string
            masterLanguage: string
            languages: string
        }

    type Config =
        {
            ServerUrl: string
            Title: string
            Description: string
            MasterUsername: string
            MasterLanguage: string
            Languages: (string * string) list
        }

    type [<CLIMutable>] RawArticle =
        {
            title: string
            subtitle: string
            ``abstract``: string
            url: string
            content: string
            date: string
            categories: string
            language: string
        }

    type Article =
        {
            Title: string
            Subtitle: string
            Abstract: string
            Url: string
            Content: string
            Date: DateTime
            Categories: string list
            Language: string
        }

    /// Zero out if article has the master language
    let URL_LANG (config: Config) lang =
        if config.MasterLanguage = lang then "" else lang

    let ReadConfig() =
        let config = Path.Combine (__SOURCE_DIRECTORY__, @"..\hosted\config.yml")
        if File.Exists config then
            let config = Yaml.OfYaml<RawConfig> (File.ReadAllText config)
            let languages =
                (Helpers.NULL_TO_EMPTY config.languages).Split([| "," |], StringSplitOptions.None)
                |> Array.map (fun s ->
                    let parts = s.Split([| "->" |], StringSplitOptions.None)
                    if Array.length parts <> 2 then
                        eprintfn "warning: Incorrect language format for substring [%s], ignoring." s
                    parts.[0], parts.[1]
                )
                |> Set.ofArray
                |> Set.toList
            {
                ServerUrl = Helpers.NULL_TO_EMPTY config.serverUrl
                Title = Helpers.NULL_TO_EMPTY config.title
                Description = Helpers.NULL_TO_EMPTY config.description
                MasterUsername = Helpers.NULL_TO_EMPTY config.masterUsername
                MasterLanguage = Helpers.NULL_TO_EMPTY config.masterLanguage
                Languages = languages
            }
        else
            {
                ServerUrl = "http://localhost:5000"
                Title = "My F# Blog"
                Description = "TODO: write the description of this blog"
                MasterUsername = "My Name"
                MasterLanguage = "en"
                Languages = ["en", "English"]
            }

    let ReadArticles() : Map<string, Article> =
        let folder = Path.Combine (__SOURCE_DIRECTORY__, @"..\hosted\posts")
        if Directory.Exists folder then
            Directory.EnumerateFiles(folder, "*.md", SearchOption.AllDirectories)
            |> Seq.toList
            |> List.choose (Helpers.(|ArticleFile|_|))
            |> List.fold (fun map (fullpath, fname, (year, month, day), slug, extension) ->
                eprintfn "Found file: %s" fname
                let header, content =
                    File.ReadAllText fullpath
                    |> Yaml.SplitIntoHeaderAndContent
                let article = Yaml.OfYaml<RawArticle> header
                let title = Helpers.NULL_TO_EMPTY article.title
                let subtitle = Helpers.NULL_TO_EMPTY article.subtitle
                let ``abstract`` = Helpers.NULL_TO_EMPTY article.``abstract``
                let url = Urls.POST_URL fname
                // If the content is given in the header, use that instead.
                let content =
                    if article.content <> null then
                        Markdown.Convert article.content
                    else
                        Markdown.Convert content
                let date = DateTime(year, month, day)
                let categories =
                    Helpers.NULL_TO_EMPTY article.categories
                let categories =
                    if not <| String.IsNullOrEmpty categories then
                        categories.Split [| ',' |]
                        // Note: categories are case-sensitive.
                        |> Array.map (fun cat -> cat.Trim())
                        |> Array.filter (not << String.IsNullOrEmpty)
                        |> Set.ofArray
                        |> Set.toList
                    else
                        []
                let language = Helpers.NULL_TO_EMPTY article.language
                Map.add fname
                    {
                        Title = title
                        Subtitle = subtitle
                        Abstract = ``abstract``
                        Url = url
                        Content = content
                        Date = date
                        Categories = categories
                        Language = language
                    } map
            ) Map.empty
        else
            eprintfn "warning: the posts folder (%s) does not exist." folder
            Map.empty

    let Menu (articles: Map<string, Article>) =
        let latest =
            articles
            |> Map.toSeq
            |> Seq.truncate 5
            |> Map.ofSeq
        [
            "Home", "/", Map.empty
            "Latest", "#", latest
        ]

    let private head() =
        __SOURCE_DIRECTORY__ + "/../Hosted/js/Client.head.html"
        |> File.ReadAllText
        |> Doc.Verbatim

    let Page langopt (config: Config) (title: option<string>) hasBanner articles (body: Doc) =
        // Compute the language keys used in all articles
        let languages =
            articles
            |> Map.toList
            |> List.map (fun (_, art) -> art.Language)
            |> List.distinct
            // Filter out the master language
            |> List.filter (fun lang ->
                URL_LANG config lang |> (String.IsNullOrEmpty >> not)
            )
        // Add back the default language IFF there is at least one other language
        let languages =
            let langmap = Map.ofList config.Languages
            // Turn a language key to a (key, displayname) pair.
            // Empty input corresponds to the master language.
            let LANG lang =
                let langkey =
                    if String.IsNullOrEmpty lang then config.MasterLanguage else lang
                if langmap.ContainsKey langkey then
                    lang, langmap.[langkey]
                else
                    lang, langkey
            if languages.Length > 0 then
                (LANG "") :: List.map LANG languages
            else
                []
        let head = head()
        MainTemplate()
#if !DEBUG
            .ReleaseMin(".min")
#endif
            .NavbarOverlay(if hasBanner then "overlay-bar" else "")
            .Head(head)
            .Title(
                match title with
                | None -> ""
                | Some t -> t + " | "
            )
            .LanguageSelectorPlaceholder(
                if languages.IsEmpty then
                    Doc.Empty
                else
                    MainTemplate.LanguageSelector()
                        .Languages(
                            languages
                            |> List.map (fun (url_lang, lang) ->
                                if langopt = url_lang then
                                    MainTemplate.LanguageItemActive()
                                        .Title(lang)
                                        .Url(Urls.LANG url_lang)
                                        .Doc()
                                else
                                    MainTemplate.LanguageItem()
                                        .Title(lang)
                                        .Url(Urls.LANG url_lang)
                                        .Doc()
                            )
                        )
                        .Doc()
            )
            .TopMenu(Menu articles |> List.map (function
                | text, url, map when Map.isEmpty map ->
                    MainTemplate.TopMenuItem()
                        .Text(text)
                        .Url(url)
                        .Doc()
                | text, _, children ->
                    let items =
                        children
                        |> Map.toList
                        |> List.sortByDescending (fun (key, item) -> item.Date)
                        |> List.map (fun (key, item) ->
                            MainTemplate.TopMenuDropdownItem()
                                .Text(item.Title)
                                .Url(item.Url)
                                .Doc())
                    MainTemplate.TopMenuItemWithDropdown()
                        .Text(text)
                        .DropdownItems(items)
                        .Doc()
            ))
            .DrawerMenu(Menu articles |> List.map (fun (text, url, children) ->
                MainTemplate.DrawerMenuItem()
                    .Text(text)
                    .Url(url)
                    .Children(
                        match url with
                        | "/blog" ->
                            ul []
                                (children
                                |> Map.toList
                                |> List.sortByDescending (fun (_, item) -> item.Date)
                                |> List.map (fun (_, item) ->
                                    MainTemplate.DrawerMenuItem()
                                        .Text(item.Title)
                                        .Url(item.Url)
                                        .Doc()
                                ))
                        | _ -> Doc.Empty
                    )
                    .Doc()
            ))
            .Body(body)
            .Doc()
        |> Content.Page

    let BlogSidebar config (articles: Map<string, Article>) (article: Article) =
        MainTemplate.Sidebar()
            .Categories(
                // Render the categories widget iff there are categories
                if article.Categories.IsEmpty then
                    Doc.Empty
                else
                    MainTemplate.Categories()
                        .Categories(
                            article.Categories
                            |> List.map (fun category ->
                                MainTemplate.Category()
                                    .Name(category)
                                    .Url(Urls.CATEGORY category (URL_LANG config article.Language))
                                    .Doc()
                            )
                        )
                        .Doc()
            )
            // There is always at least one blog post, so we render this
            // section no matter what.
            .ArticleItems(
                articles
                |> Map.toList
                |> List.sortByDescending (fun (_, item) -> item.Date)
                |> List.map (fun (_, item) ->
                    MainTemplate.ArticleItem()
                        .Title(item.Title)
                        .Url(item.Url)
                        .ExtraCSS(if article.Url = item.Url then "is-active" else "")
                        .Doc()
                )
            )
            .Doc()

    let PLAIN html =
        div [Attr.Create "ws-preserve" ""] [Doc.Verbatim html]

    let ArticlePage (config: Config) articles (article: Article) =
        // Zero out if article has the master language
        let langopt = URL_LANG config article.Language
        MainTemplate.ArticlePage()
            // Main content panel
            .Article(
                MainTemplate.Article()
                    .Title(article.Title)
                    .Subtitle(Doc.Verbatim article.Subtitle)
                    .Content(PLAIN article.Content)
                    .Doc()
            )
            // Sidebar
            .Sidebar(BlogSidebar config articles article)
            .Doc()
        |> Page langopt config (Some article.Title) false articles

    // The silly ref's are needed because offline sitelets are
    // initialized in their own special way, without having access
    // to top-level values.
    let articles : Map<string, Article> ref = ref Map.empty
    let config : Config ref = ref <| ReadConfig()

    let Main (config: Config ref) (articles: Map<_, Article> ref) =
        let ARTICLES_BY f articles =
            Map.filter f articles
        let ARTICLES (articles: Map<_, Article>) =
            [ for (_, article) in Map.toList articles ->
                MainTemplate.ArticleCard()
                    .Author(config.Value.MasterUsername)
                    .Title(article.Title)
                    .Abstract(article.Abstract)
                    .Url(article.Url)
                    .Date(Helpers.FORMATTED_DATE article.Date)
                    .ArticleCategories(
                        if article.Categories.IsEmpty then
                            Doc.Empty
                        else
                            article.Categories
                            |> List.map (fun category ->
                                MainTemplate.ArticleCategory()
                                    .Title(category)
                                    .Url(Urls.CATEGORY category (URL_LANG config.Value article.Language))
                                    .Doc()
                            )
                            |> Doc.Concat
                    )
                    .Doc()
            ]                        
        Application.MultiPage (fun (ctx: Context<_>) -> function
            | Home langopt ->
                MainTemplate.HomeBody()
                    .Banner(
                        MainTemplate.HomeBanner().Doc()
                    )
                    .ArticleList(
                        ARTICLES_BY (fun _ article ->
                            langopt = URL_LANG config.Value article.Language
                        ) articles.Value
                        |> ARTICLES
                    )
                    .Doc()
                |> Page langopt config.Value None false articles.Value
            | Article p ->
                let page =
                    if p.ToLower().EndsWith(".html") then
                        p.Substring(0, p.Length-5)
                    else
                        p
                if articles.Value.ContainsKey page then
                    ArticlePage config.Value articles.Value articles.Value.[page]
                else
                    Map.toList articles.Value
                    |> List.map fst
                    |> sprintf "Trying to find page \"%s\" (with key=\"%s\"), but it's not in %A" p page
                    |> Content.Text
            | Category (cat, langopt) ->
                MainTemplate.HomeBody()
                    .Banner(
                        MainTemplate.CategoryBanner()
                            .Category(cat)
                            .Doc()
                    )
                    .ArticleList(
                        ARTICLES_BY (fun _ article ->
                            langopt = URL_LANG config.Value article.Language
                            &&
                            List.contains cat article.Categories
                        ) articles.Value
                        |> ARTICLES
                    )
                    .Doc()
                |> Page langopt config.Value None false articles.Value
            // For a simple but useful reference on Atom vs RSS content, refer to:
            // https://www.intertwingly.net/wiki/pie/Rss20AndAtom10Compared
            | AtomFeed ->
                Content.Custom (
                    Status = Http.Status.Ok,
                    Headers = [Http.Header.Custom "content-type" "application/atom+xml"],
                    WriteBody = fun stream ->
                        let ns = XNamespace.Get "http://www.w3.org/2005/Atom"
                        let doc =
                            X (ns + "feed") [] [
                                X (ns + "title") [] [TEXT config.Value.Title]
                                X (ns + "subtitle") [] [TEXT config.Value.Description]
                                X (ns + "link") ["href" => config.Value.ServerUrl] []
                                X (ns + "updated") [] [Helpers.ATOM_DATE DateTime.UtcNow]
                                for (slug, article) in Map.toList articles.Value do
                                    X (ns + "entry") [] [
                                        X (ns + "title") [] [TEXT article.Title]
                                        X (ns + "link") ["href" => config.Value.ServerUrl + Urls.POST_URL slug] []
                                        X (ns + "id") [] [TEXT slug]
                                        for category in article.Categories do
                                            X (ns + "category") [] [TEXT category]
                                        X (ns + "summary") [] [TEXT article.Abstract]
                                        X (ns + "updated") [] [TEXT <| Helpers.ATOM_DATE article.Date]
                                    ]
                            ]
                        doc.Save(stream)
                )
            | RSSFeed ->
                Content.Custom (
                    Status = Http.Status.Ok,
                    Headers = [Http.Header.Custom "content-type" "application/rss+xml"],
                    WriteBody = fun stream ->
                        let doc =
                            X (N "rss") ["version" => "2.0"] [
                                X (N "channel") [] [
                                    X (N "title") [] [TEXT config.Value.Title]
                                    X (N "description") [] [TEXT config.Value.Description]
                                    X (N "link") [] [TEXT config.Value.ServerUrl]
                                    X (N "lastBuildDate") [] [Helpers.RSS_DATE DateTime.UtcNow]
                                    for (slug, article) in Map.toList articles.Value do
                                        X (N "item") [] [
                                            X (N "title") [] [TEXT article.Title]
                                            X (N "link") [] [TEXT <| config.Value.ServerUrl + Urls.POST_URL slug]
                                            X (N "guid") ["isPermaLink" => "false"] [TEXT slug]
                                            for category in article.Categories do
                                                X (N "category") [] [TEXT category]
                                            X (N "description") [] [TEXT article.Abstract]
                                            X (N "pubDate") [] [TEXT <| Helpers.RSS_DATE article.Date]
                                        ]
                                ]
                            ]
                        doc.Save(stream)
                )
            | Refresh ->
                // Reload the article cache and the master configs
                articles := ReadArticles()
                config := ReadConfig()
                Content.Text "Articles/configs reloaded."
        )

open System.IO

[<Sealed>]
type Website() =
    let articles = ref <| Site.ReadArticles()
    let config = ref <| Site.ReadConfig()

    interface IWebsite<EndPoint> with
        member this.Sitelet = Site.Main config articles
        member this.Actions =
            let articles = Map.toList articles.Value
            let categories =
                articles
                |> List.map snd
                |> List.collect (fun article -> article.Categories)
                |> Set.ofList
                |> Set.toList
            let languages =
                articles
                |> List.map snd
                |> List.map (fun article -> Site.URL_LANG config.Value article.Language)
                |> Set.ofList
                |> Set.toList
            [
                for language in languages do
                    Home language
                for (slug, _) in articles do
                    Article slug
                for category in categories do
                    for language in languages do
                        if
                            List.exists (fun (_, (art: Site.Article)) ->
                                language = Site.URL_LANG config.Value art.Language
                                &&
                                List.contains category art.Categories 
                            ) articles
                        then
                            Category (category, language)
                RSSFeed
                AtomFeed
            ]

[<assembly: Website(typeof<Website>)>]
do ()
