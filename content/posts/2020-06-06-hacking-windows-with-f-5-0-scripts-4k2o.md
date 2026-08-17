---
title: Hacking Windows with F# 5.0 Scripts
subtitle: ~
categories: winrt,fsharp,dotnet,windows
abstract: Hello everyone, today I bring you some F# 5.0 preview niceties FSharp Conf was last Friday (June 5th,...
date: 2020-06-06
language: en
---

Hello everyone, today I bring you some F# 5.0 preview niceties [FSharp Conf](http://fsharpconf.com/) was last Friday (June 5th, 2020) and there were many many awesome #fsharp talks 100% recommended, if you are into web development there are a couple of talks for you:

- SAFE Stack – The Road Ahead - Isaac Abraham
- From Zero to F# Hero - James Randal

That being said... today's content post is little different I'll talk about the nuget package references for F# scripts (fsx files), you can read more about the new F# 5.0 features [here](https://devblogs.microsoft.com/dotnet/announcing-f-5-preview-1/).

In the past if you wanted to script with F# you needed to have the library code downloaded and reference the dll file directly
```fsharp
#r "../libs/MyLib.dll"
open MyLib
/// do code with MyLib
```

F# 5.0 is introducing the `nuget` references now if you want to reference a library it's pretty simple for example 
```fsharp
#r "nuget: Newtonsoft.Json"
open Newtonsoft.Json

let o = {| X = 2; Y = "Hello" |}

printfn "%s" (JsonConvert.SerializeObject o)
```

that will download the Nuwtonsoft.Json package (in a transparent way for the user) and add the referenced library, after that you will be able to open any namespaces that the library provides.

I feel that gives you a better experience when you want to create some scripts either for quick usage, prototyping or even API exploration as we will see.

Recently Microsoft released a new nuget package: `Microsoft.Windows.Sdk.NET` which exposes in a pretty nice way the native WinRT API's from Windows. The same API's you would use from UWP Apps the most modern stuff is in there 
{% github microsoft/CsWinRT %}

I've done some WinRT stuff in the past with UWP applications and I have always felt that the WinRT API is just so nice to work with and remembering that everything you do with it is 100% native.

### What can you do with the WinRT projection? 
Anything that does not require some sort of UI, if you want to use an API that requires a CoreWindow or that it executes on the UI thread then you would need to implement some interfaces for your application as noted [here](https://github.com/microsoft/CsWinRT/issues/287#issuecomment-632391358) everything else that is UI-less is good to go, and as an example: In this repository I made a small Avalonia App that leverages the WinRT API. It includes examples for the following API's
- Windows.Media
- Windows.Networking
- Windows.System.Power
{% github AngelMunoz/WinRTFs %}

And... after half day reading... Finally some code.

The first Example is a small gist that uses the 
`Windows.Storage` API to take 5 files from the the user's music library and check the its music properties.

{% gist https://gist.github.com/AngelMunoz/149487755f2c348cb6b38306c2917269 %}

the code is very short and is async heavy the reason for that is there are a lot of Async APIs in WinRT and here's Larry Osterman explaining why
{% twitter 1088118104369049601 %}

Fortunately the System namespace includes a nice extension method that converts an IAsyncAction to a usual Task, then we use some F# Async functions to make it work seamlessly in F#.

```fsharp
KnownFolders
    .MusicLibrary
    .GetFilesAsync()
    .AsTask() |> Async.AwaitTask
```
Here we use the MusicLibrary IStorageFolder to get its files in a very simple way the `IStorageFolder` contains some nice properties and methods that we can leverage to either create/delete/update new files or directories


```fsharp
for file in files do
    file
        .Properties
        .GetMusicPropertiesAsync()
        .AsTask() |> Async.AwaitTask
```
In this one, we just iterate over the files to read properties on each of them you can read more about the `StorageFile` type [here](https://docs.microsoft.com/en-us/uwp/api/windows.storage.storagefile?view=winrt-19041#methods)

And that's it. The WinRT API is at your fingertips reach now available with F#!

For the Full reference on these API's you can check this link

https://docs.microsoft.com/en-us/uwp/api/

# Bonus
What about a terminal music player prototype?
As noted here...
{% twitter 1269052899910389762 %}
you can actually write a PoC of a media player in less than 50 LoC with F#!
If you add some lines for the `System Media Transport Controls` (the ones that give you info when you turn up/down the volume, change or play/pause the song) and input management, you can actually write a better PoC in less than 100 LoC

> To run this sample type 
>`dotnet fsi --langversion:preview media-player.fsx`
>(or whatever the name of the file is), also don't forget to download [.net5 preview](https://dotnet.microsoft.com/download/dotnet/5.0)

{% gist https://gist.github.com/AngelMunoz/fb5d9d09e252be2e3f750cf3827fcfae %}

Let's go bit by bit

```fsharp
#r "nuget: Microsoft.Windows.Sdk.NET, 10.0.18362.3-preview"

open System

open Windows.Media
open Windows.Media.Core
open Windows.Media.Playback

open Windows.Storage
open Windows.Storage.FileProperties
open Windows.Storage.Search
open Windows.Storage.Streams
```
Nothing fancy so far, we are just opening the namespaces that contain the types and classes we will use along the way

```fsharp
let asyncGetFiles () =
    async {
        let queryOpts = QueryOptions()
        queryOpts.FileTypeFilter.Add(".mp3")

        let query =
            KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOpts)

        let! files = query.GetFilesAsync().AsTask() |> Async.AwaitTask
        return files |> Seq.take 5
    }

```
Here we define the function `asyncGetFiles` inside we do a query to get only `.mp3` files also the [Windows.Storage.Search](https://docs.microsoft.com/en-us/uwp/api/windows.storage.search?view=winrt-19041) API contains some nice classes to create complex queries on files as well as give you a number of ways to present that information to you.

Like in our first example we get the files from the music library in an asynchronous way. Since this is a prototype we just take 5 but we could bring the entire in a single trip if we wanted to.

The next section is quite large so I'll add the explanation as comments as we go by
```fsharp
let asyncGetPlaylist =
    async {
        let! files = asyncGetFiles ()
        /// create a new media playlist
        /// this will be the source for our player later on
        let playlist = MediaPlaybackList()
        for item in files do
            let! thumbnail =
               item
                   .GetThumbnailAsync(ThumbnailMode.MusicView)
                   .AsTask() |> Async.AwaitTask
            let! musicProps =
                item
                    .Properties
                    .GetMusicPropertiesAsync()
                    .AsTask() |> Async.AwaitTask
            /// let's create a media source for each file we found
            let source = MediaSource.CreateFromStorageFile item
            /// create the items from the sources
            let mediaPlaybackItem = MediaPlaybackItem source
            /// this part is important only if you want a nice
            /// integration with the OS, but is not really necessary
            let props = mediaPlaybackItem.GetDisplayProperties()
            props.Type <- MediaPlaybackType.Music
            props.MusicProperties.Title <- musicProps.Title
            props.MusicProperties.AlbumArtist <- musicProps.AlbumArtist
            props.MusicProperties.AlbumTitle <- musicProps.Album
            props.MusicProperties.Artist <- musicProps.Artist
            props.MusicProperties.TrackNumber <- musicProps.TrackNumber
            props.Thumbnail <- RandomAccessStreamReference.CreateFromStream thumbnail
            /// once you have set the properties don't forget to apply them
            /// otherwise these will not show in the SMTC
            /// (System Media Transport Controls)
            mediaPlaybackItem.ApplyDisplayProperties(props)
            /// finally add the mediaplayback item to the playlist
            playlist.Items.Add mediaPlaybackItem
        return playlist
    }

```
Now, it may seem convoluted having to create a source for the item then a source for the playback item then adding it to the playlist but here's the cool thing they are abstractions that will allow you to control the playlist and the player in an easy way without worrying what is the actual source of the media you will play, because you can play physical Video, Music, and even Streams the source can be in your hard drive or can come from the internet the API is pretty flexible.


```fsharp
let player = new MediaPlayer()

async {
    let! playlist = asyncGetPlaylist
    player.Source <- playlist
    player.Play()
}
|> Async.RunSynchronously
```
This part is pretty simple I believe, just get the playlist, assign it to the player, then start playing.

The rest it's just to be able to change songs and pause/play by reading keystrokes
```fsharp
let hmsg = "Press q to quit, up arrow to play or pause and arrows to move previous or next"
printfn "%s" hmsg
let mutable key: ConsoleKeyInfo = Console.ReadKey()
let playlist = player.Source :?> MediaPlaybackList

while key.Key <> ConsoleKey.Q do
    match key.Key with
    | ConsoleKey.RightArrow ->
        playlist.MoveNext() |> ignore
    | ConsoleKey.LeftArrow ->
        playlist.MovePrevious() |> ignore
    | ConsoleKey.UpArrow ->
        match player.CurrentState with
        | MediaPlayerState.Paused -> player.Play()
        | MediaPlayerState.Playing -> player.Pause()
        | _ -> ()
    | ConsoleKey.H -> printfn "\n%s" hmsg
    | _ -> ()
    key <- Console.ReadKey()

player.Dispose()
exit(0)
```

when you have a player in memory you will be only able to Play/Pause if you want to stop completely you need to dispose the player 
`player.Dispose()` that will also release the SMTC integration you may have had added.

This is a prototype that if you feel confident enough you may even be able to just copy paste the functions into a console application with a few more tweaks and you have a terminal player MVP if you want to go crossp-platform you may want to use [LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp) instead 😁


# Closing thoughts
So... Yeah! using F# 5.0 scripts to prototype and explore API's is a blessing. Once F#5.0 is GA (Generaly Available) you could also set a few snippets of code to showcase how simple is to use F# to do complex things (like some of the stuff you can do with the WinRT API) if you want to get some of your Colleagues to adopt F#

If you have any doubts/comments please feel free to write them below or to ping me on twitter 😁 thanks for the time you spent here.
