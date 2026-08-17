---
title: Feliz Web Apps for JavaScript devs
subtitle: ~
categories: fsharp,dotnet,javascript,react
abstract: Hello, It's been a while since I wrote something here 😁  Last time I was writing stuff about Avalonia...
date: 2020-04-07
language: en
---

Hello, It's been a while since I wrote something here 😁

Last time I was writing stuff about Avalonia and Avalonia.FuncUI A way to write desktop applications with F# in a simple and easy way.

Today I'll give you something a little bit different, I'll go back to my web roots and show you how easily you can create web applications with F# and React under the hood. This post is directed to Javascript developers which may be a little bored of javascript and are looking for something new to add to their stack, or for other devs that may be looking for javascript alternatives with strong typing but for some reason don't want to go for Typescript/Flow.

If you have been away from any .net for quite some time you may not know that .net is Free and open source. No, You don't need Visual Studio, No you will not be locked into licensing fees and Yes you may even try to go FullStack F# after trying this.

What will we be using today?
- [Nodejs](https://nodejs.org/en/download/)
    - [nvm](https://github.com/nvm-sh/nvm) (optional)
    - [nvm-windows](https://github.com/coreybutler/nvm-windows) (optional)
- [dotnet core](https://dotnet.microsoft.com/)
- [visual studio code](https://code.visualstudio.com/)
    - [Ionide Extension](https://marketplace.visualstudio.com/items?itemName=Ionide.Ionide-fsharp)
- [Feliz](https://zaid-ajaj.github.io/Feliz/#/Feliz/ProjectTemplate) (yeah it wasn't clickbait)

> Nvm is not required but recommended if you chose to use nvm skip the Nodejs link and follow the respective nvm repository instructions.

It is very likely that you are using VSCode already due to its popularity.

After you install the required tools from above we can proceed, let's start with the command line.

## Create the project
> You need to have the Feliz Templates Installed on your machine at this point
>
> `dotnet new -i Feliz.Template::*`

In my case will be like this:
```
dotnet new feliz -o FelizSample
```
> (-o means output, where to put the project)

then open your VSCode instance wherever you had chosen.
after that don't forget to enable the Ionide extension either globally or by project basis 

![Enable Ionide](https://dev-to-uploads.s3.amazonaws.com/i/ww1uc0b8jnuehthkrdra.png)

you will see the following project structure once you are ready to continue
![Project Structure](https://dev-to-uploads.s3.amazonaws.com/i/4fi90r2wc1h9rvc9xdxp.png)


## Install dependencies
Now, for us to start hacking through we need to install the node dependencies

```
npm install # or yarn install or pnpm install your choice
```
> when you run the start command the dotnet dependencies will install themselves

## Run
```
npm start
```
and then go to localhost:8080 to see the app running
![App running](https://dev-to-uploads.s3.amazonaws.com/i/vakbtrj6atkkfadk0412.png)

If you see on my browser I have the React Dev Tools extension as well as the Redux DevTools extension installed (if you are a react developer yourself you may already have these installed)... So yes, you already have a nice dev experience out of the box when you start clicking those buttons

![Feliz Counter](https://dev-to-uploads.s3.amazonaws.com/i/iaygso6rx5gbe7duysfg.gif)


## Code
Finally some code!

Now that we have the application running let's check a little bit of the code. Feliz is a DSL for the React Syntax and the way Feliz is written it resembles the react API, it even supports [Hooks!](https://zaid-ajaj.github.io/Feliz/#/Feliz/ReactApiSupport)
the out of the box sample uses an [Elmish Sample](https://github.com/elmish/elmish) which uses the [Elm Architecture](https://guide.elm-lang.org/architecture/) 

![Sample Code](https://dev-to-uploads.s3.amazonaws.com/i/w8eseioqp7a0j38n4p3x.png)

You can either continue to use that architecture which I believe it's pretty great I've talked about that in my Avalonia.FuncUI Series, It uses an Elmish implementation for Avalonia, so If you read that it also applies to this post.

If you want to use react functions components you can do that as well
```fsharp
let counter = React.functionComponent(fun () ->
    let (count, setCount) = React.useState(0)
    Html.div [
        Html.h1 count
        Html.button [
            prop.text "Increment"
            prop.onClick (fun _ -> setCount(count + 1))
        ]
    ])
```

![Both Counters](https://dev-to-uploads.s3.amazonaws.com/i/saz55y3idoxttxf9rgjm.png)

```fsharp
let private reactCounter = React.functionComponent("ReactCounter", fun () ->
    let (count, setCount) = React.useState(0) // Yup, Hooks!
    let text = sprintf "ReactCounter: %i" count
    Html.div [
            Html.h1 text
            Html.button [
                prop.text "Increment"
                prop.onClick (fun _ -> setCount(count + 1))
            ]
            Html.button [
                prop.text "Decrement"
                prop.onClick (fun _ -> setCount(count - 1))
            ]
        ]
 )

let render (state: State) (dispatch: Msg -> unit) =
    let text = sprintf "ElmishCounter: %i" state.Count
    Html.div [
        Html.h1 text
        Html.button [
            prop.onClick (fun _ -> dispatch Increment)
            prop.text "Increment"
        ]

        Html.button [
            prop.onClick (fun _ -> dispatch Decrement)
            prop.text "Decrement"
        ]
        Html.hr []
        reactCounter() // it's used here
    ]
```

**Hey but what about Props?**
To use props within your react components you just need to supply a type annotation. Without diving too much in the Main.fs file we will just take the Elmish stuff out and use the `reactCounter` directly

```fsharp
module Main

open Fable.Core.JsInterop
open Feliz
open Browser.Dom

importAll "../styles/main.scss"

ReactDOM.render (App.reactCounter { count = 10 }, document.getElementById "feliz-app")

```
We just go straight for the React API and render our component
```fsharp
module App

open Feliz
type CounterProps = { count: int }

let reactCounter = React.functionComponent("ReactCounter", fun (props: CounterProps) ->
    let (count, setCount) = React.useState(props.count)
    let text = sprintf "ReactCounter: %i" count
    Html.div [
            Html.h1 text
            Html.button [
                prop.text "Increment"
                prop.onClick (fun _ -> setCount(count + 1))
            ]
            Html.button [
                prop.text "Decrement"
                prop.onClick (fun _ -> setCount(count - 1))
            ]
        ]
 )

```
And that's the output
![ReactCounter Props](https://dev-to-uploads.s3.amazonaws.com/i/36s2o1p2lyooj9f50ukr.png)

And that's it! if you had ever wanted to try F# but you felt the Elmish Architecture was kinda scary or that it was too much trying to learn F# as well as Elmish at the same time Feliz is here yo help you!

Feliz has some libraries out to help you ease web development
like 
- Feliz Router
- Feliz Recharts 
- Feliz PigeonMaps
- Feliz MaterialUI
- Feliz Bulma

These can be found in the same docs as Feliz

## Closing Thoughts
Feliz can help you explore the goodness that F# is taking your existing React knowledge and also I didn't mention this but the Feliz template also has a testing project included that uses Fable.Mocha so... yeah you also have nice tools to start working on your next project!


you can check the source code here
{% github AngelMunoz/FelizSample %}

If you are interested in more Feliz posts let me know in the comments or twitter :)

> Offtopic: By the way, I've been streaming F# coding in https://twitch.tv/tunaxor for a few days now if you are interested mind to pass by :) the schedule is in the channel
