
---
title: Exploring the F# Frontend Landscape
subtitle: ~
categories: fsharp,dotnet,webassembly,webdev
abstract: Today we will talk about what is the current frontend landscape of Frontend development for the F# ecosystem.
date: 2022-05-23
language: en
---

[feliz]: https://github.com/Zaid-Ajaj/Feliz
[react.js]: https://reactjs.org
[feliz.engine]: https://github.com/alfonsogarciacaro/Feliz.Engine
[feliz.solid]: https://github.com/alfonsogarciacaro/Feliz.Solid
[feliz.snabdom]: https://github.com/alfonsogarciacaro/Feliz.Snabbdom
[fable.svelte]: https://github.com/fable-compiler/Fable.Store
[fable.lit]: https://github.com/fable-compiler/Fable.Lit
[lit.dev]: https://lit.dev
[sutil]: https://github.com/davedawkins/Sutil
[bolero]: https://fsbolero.io/
[fun.blazor]: https://slaveoftime.github.io/Fun.Blazor.Docs/
[blazor]: https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor
[websharper]: https://websharper.com/
[avalonia.funcui]: https://github.com/fsprojects/Avalonia.FuncUI
[avalonia.funcui wasm template]: https://github.com/AngelMunoz/FuncUI.Wasm.Template
[shoelace.style]: https://shoelace.style
[fast.design]: https://fast.design
[adobe spectrum]: https://opensource.adobe.com/spectrum-web-components/
[highlight html/sql templates in f#]: https://marketplace.visualstudio.com/items?itemName=alfonsogarciacaro.vscode-template-fsharp-highlight
[html for f# (lit template)]: https://marketplace.visualstudio.com/items?itemName=daniel-hardt.html-for-fsharp-lit-template
[alfonso said]: https://twitter.com/alfonsogcnunez/status/1528379845394440193
[use photoshop natively in the browser]: https://web.dev/ps-on-the-web/
[vue's composition api]: https://vuejs.org/guide/introduction.html#composition-api
[svelte]: https://svelte.dev/

Hello everyone it's been a while!
Today we will talk about what is the current frontend landscape of Frontend development for the F# ecosystem. Over the last few years Fable has become quite capable and more bindings have been released.

> **_Disclaimer_**: _**The F# Ecosystem Is Stable**_ you don't need to jump ship to the next thing or anything like that! Just because there are options doesn't mean you need to ditch out what you know and learn something new. Think of this as as restaurant menu: There are options but ultimately it is your choice which one you pick and you can even decide "I don't want to eat here" it's completely fine. No kittens will die and the world won't stop so if you see a lot of options I'd suggest you to not feel pressured to choose!

When it comes to the Frontend landscape for F# we have three main roads:

- Fable
- WebSharper
- Web Assembly

These have different approaches to the frontend landscape but ultimately do the same thing **_Websites_** some of those options allow you to go for server side rendering and others allow you to go for single page applications.

## Fable

In this Section:

> High Profile:
>
> - Feliz
> - Fable.Lit
> - Sutil
>
> Low Profile
>
> - Fable.Svelte
> - Feliz.Snabdom
>
> Fable Next:
>
> - Feliz.Solid

Fable is an F# to JavaScript compiler (like Typescript compiles to JavaScript) it is near its fourth major release and has a very very strong ecosystem based around [React.js] although, in recent times other options have become available.

### Why Fable and who benefits most from it?

Fable is meant for those developers that need to work with the JavaScript ecosystem or that want to benefit from it, it would be unwise to negate the number of good libraries and existing solutions that have been born in it.

Fable rather than negate that JavaScript exists, builds on top of it and gives you the most flexible option when it comes to frontend development in F#.
You keep using the safety of F# (most of the time) and when necessary you can fall back to JavaScript interoperation (via emit, imports or the dynamic operator) or even JavaScript itself to fill the missing gaps that could be there.

The bad thing is that the JavaScript ecosystem is so vast and has grown so much over the years it might be possible that if you want X library there won't be bindings for it. After all the F# developer numbers are way too low compared so we don't have the programmer power to be on equal grounds.

Writing bindings isn't complex but it can take quite some time from your development efforts if the libraries you're targeting are too big, this cost is only paid once though, when the bindings are complete is just a matter of maintaining the bindings up to date (which isn't too big of a chore).

That being said! Let's dive into Fable's options:

### [Feliz]

This is by far the most popular library in the F# frontend ecosystem, This library took existing lessons from Fable React and improved its DSL (Domain Specific Language) to be more concise and less verbose than existing alternatives. It also took a different approach when it came to React applications, it steered slightly away from what was very popular at the time the Elmish architecture (also known as MVU) and provided an API that is as close as possible to React itself.

Feliz introduced hooks which helped to simplify state management in some cases, as well as reduce the verbosity MVU can get when applications grow, since it was compatible with previous fable-react bindings it wasn't a big of an effort to migrate to Feliz.

Your typical Feliz UI component looks like this

```fsharp
[<ReactComponent>]
let Counter() =
    let (count, setCount) = React.useState(0)
    Html.div [
        Html.button [
            prop.style [ style.marginRight 5 ]
            prop.onClick (fun _ -> setCount(count + 1))
            prop.text "Increment"
        ]

        Html.button [
            prop.style [ style.marginLeft 5 ]
            prop.onClick (fun _ -> setCount(count - 1))
            prop.text "Decrement"
        ]

        Html.h1 count
    ]
```

Its DSL is based on a list of properties for each kind of HTML tag, you can build reusable pieces of UI by just writing functions and other components given React's nature it is clear why Feliz is the most used, it simply fits with the F# mind, data and functions!

Feliz supports MVU via the `Feliz.UseElmish` package

```fsharp
type Msg =
    | Increment
    | Decrement

type State = { Count : int }

let init() = { Count = 0 }, Cmd.none

let update msg state =
    match msg with
    | Increment -> { state with Count = state.Count + 1 }, Cmd.none
    | Decrement -> { state with Count = state.Count - 1 }, Cmd.none

[<ReactComponent>]
let Counter() =
    let state, dispatch = React.useElmish(init, update, [| |])
    Html.div [
        Html.h1 state.Count
        Html.button [
            prop.text "Increment"
            prop.onClick (fun _ -> dispatch Increment)
        ]

        Html.button [
            prop.text "Decrement"
            prop.onClick (fun _ -> dispatch Decrement)
        ]
    ]

// somewhere else
ReactDOM.render(Counter(), document.getElementById "feliz-app")
```

If you're looking to dive into Frontend F# then Feliz is a solid choice you will learn what most of the F# FE devs use and it is arguably the best choice today within the Fable realm.

The downsides of Feliz are the downsides of using React, since Feliz is a 1-1 binding over React you get the same problems React devs have, weird rules for hooks, easy to mistakenly provoke re-renders, and effects are still not entirely figured out in the react ecosystem and you have to keep manually what things need to re-render your UI. React uses Virtual DOM which was a performant way to render UI's in the early 2010's it is not the case anymore where browsers have caught up in performance and it turns out that in performance critical situations the VDOM diffing from React is just overhead rather than an advantage. For your average website, this shouldn't be a concern though but it is worth mentioning it.

### [Fable.Lit]

This is my personal favorite one when it comes to Fable options, [Fable.Lit] builds on top of [lit.dev] which is a web component library built on web standards. It brings performant straightforward and inter-framework compatible components to the F# FE landscape since Lit works with DOM elements themselves rather than abstractions you can manipulate component instances like if you were doing vanilla JavaScript except that you can use the F# safety for that.

In Fable.Lit rather than building an F# DSL (we tried) we use a string-based alternative which is closed to the HTML you know and love, this also helps a lot when you have to consume web components like those from [shoelace.style], [fast.design], [adobe spectrum] components, and more, this will be a very important and big point over the next few years now that web components have taken off finally with major companies like Microsoft, Salesforce, Github, Adobe and more are using them.

Here's two ways you can use Fable.Lit Components

```fsharp
[<HookComponent>]
let functionCounter(initial: int) =
    let value, setValue = Hook.useState initial
    html
        $"""
        <!-- @<event name> means attach a handler to this event -->
        <sl-button outline variant="neutral" @click={fun _ -> setValue value + 1}>Increment</sl-button>
        <sl-button outline variant="neutral" @click={fun _ -> setValue value - 1}>Decrement</sl-button>
        <sl-badge>Count: {value}</sl-badge>
        """

[<LitElement("my-custom-element")>]
let MyCustomElement() =
    let host, props =
        LitElement.init(fun config ->
            config.props = {| initial = Prop.Of(0, attribute = "initial") |}
            // defaults to true if not set
            config.useShadowDom <- false
        )
    let value, setValue = Hook.useState props.initial.Value

    html
        $"""
        <sl-button outline variant="neutral" @click={fun _ -> setValue value + 1}>Increment</sl-button>
        <sl-button outline variant="neutral" @click={fun _ -> setValue value - 1}>Decrement</sl-button>
        <sl-badge>Count: {value}</sl-badge>
        """

// using both somewhere
html
    $"""
    Function Component:
    {functionCounter 20}
    <br>
    <!-- .initial means bind to "initial" property -->
    <my-custom-element .initial={10}></my-custom-element>
    """
```

First of all if you are wondering "ugh strings", "that doesn't give any highlight", "the holes are not typed" I have a few words for that:

1. Interpolated strings aren't as flexible as JS tagged templates so in .NET we fallback to using just objects and we lose type safety
2. We actually have two extensions to give you the ability to highlight these F# strings
   - [Highlight HTML/SQL templates in F#]
   - [Html for F# (Lit Template)]
   - For Rider and other editors no-one has tried to build a plugin as far as I know

Here's the thing (and the main reason I like it):

- Did we have to write bindings for `sl-button`?
- Would we need to write bindings for any other custom element/web component?

The answer for both is **_No_**, we still have write bindings for the JS parts of the libraries we might use but when it comes to custom elements and other standard HTML elements we don't need to do anything, that includes it's attributes/properties.

The tradeoff is precisely that we gain access to a vast array of libraries out there but we lose some type safety when you describe your UIs.

And before I forget it, Fable.Lit also supports the Elmish architecture

```fsharp
type Msg =
    | Increment
    | Decrement

type State = { Count : int }

let init() = { Count = 0 }, Cmd.none

let update msg state =
    match msg with
    | Increment -> { state with Count = state.Count + 1 }, Cmd.none
    | Decrement -> { state with Count = state.Count - 1 }, Cmd.none

[<HookComponent>]
let Counter() =
    let model, dispatch = Hook.useElmish(init, update)
    html $"""
        <h1>{model.Count}</h1>
        <button @click={fun _ -> dispatch Increment}>Increment</button>
        <button @click={fun _ -> dispatch Decrement}>Decrement</button>
    """
```

Lit itself is a pretty safe bet is a solid choice and built on web standards so it's very likely to have a really long life (it first came out around 2013-2014 as polymer if you ever head of that) and has adjusted and improved together with web browsers

Fable.Lit on the other hand is fairly new and the bindings may still have some areas where we can improve but the technology underneath is already production ready.

### [Sutil]

When I first learned about Sutil I fell in love with it, it brought a lot of concepts from [Svelte] to the F# frontend landscape and while it's development has been slower than most it has some really interesting choices which can fit some minds better than the other alternatives.

Sutil is the first pure F# framework for the frontend that we have, it doesn't have bindings to any framework because it's just F#.

Sutil uses a Feliz variation of a DSL called [Feliz.Engine] (we'll talk about it later on) so you get the F# type safety you know and love with reactive UI elements.
Sutil functions run only once and then everything is static unless you choose to be reactive via stores. This helps in regards performance and updates are only applied where things change.

In a similar fashion to Fable.Li, Sutil works with plain DOM elements which make it compatible with Web Components as well, it also provides features to write web components with it!

If you really like the programming model of svelte or rxjs (observables) then Sutil is something to look after, it also has built-in animations, chrome dev tools, and other nice features.

Here's how Sutil looks like

```fsharp
// functions can be separated from UI logic
// with some thought we can make these very reusable
// and even UI agnostic
let increment (counter: IStore<int>) =
    counter
    |> Store.modify (fun count -> count + 1)

let decrement (counter: IStore<int>) =
    counter
    |> Store.modify (fun count -> count - 1)

let view() =
    let counter = Store.make 0

    Html.div [
        // make this element reactive
        Bind.el(counter, fun count -> Html.p $"Counter: {count}")
        Html.div [
            // using stablished HTML elements
            Html.button [
                onClick (fun _ -> increment counter ) []
                text "Increment"
            ]
            // interoperation with custom elements
            Html.custom("sl-button", [
                Attr.custom("variation", "neutral")
                onClick (fun _ -> decrement counter) []
                text "Decrement"
            ])
        ]
    ]
```

As mentioned before whenever we mount/call `view` it will render once and when stores/observables emit a new value only the reactive parts will update this allows for fine grained updates

Sutil Also offers MVU support:

```fsharp
type Msg =
    | Increment
    | Decrement

type State = { Count : int }

let init() = { Count = 0 }, Cmd.none

let update msg state =
    match msg with
    | Increment -> { state with Count = state.Count + 1 }, Cmd.none
    | Decrement -> { state with Count = state.Count - 1 }, Cmd.none

let Counter() =
    let model, dispatch = () |> Store.makeElmishSimple init update ignore
    Html.div [
        disposeOnUnmount [ model ]
        Bind.fragment (model |> Store.map getCounter) <| fun n ->
            Html.h1 [ text $"Counter = {n}" ]

        Html.div [
            Html.button [
                onClick (fun _ -> dispatch Decrement) []
                text "-"
            ]

            Html.button [
                onClick (fun _ -> dispatch Increment) []
                text "+"
            ]
        ]]
```

Part of the disadvantages in Sutil is the slow updates although David recently mentioned that he will be working on it, more maintainers would be welcome.
Also it has been in beta for a while so it might not be so ready for prime time.
More testing from real users would be nice because at least on my relatively limited testing it feels just as solid as any of the previous choices.

### [Feliz.Engine]

Once the high profile projects have been mentioned one project that is worth mentioning and one you can use if you plan to ever bring another framework to the F# space is Feliz.Engine

Feliz.Engine is a library that defines in a _standard_ way F# DSLs for Elements, Attributes and Styles. It was born out of the Original Feliz DSL but modified slightly to fit a more general use case.

Sutil, Feliz.Solid and Feliz.Snabdom use Feliz.Engine under the hood you could also use it to bring others to the fold!

This project deserves a mention just for it's potential to bring more to the ecosystem (and to not confuse it with Feliz itself)

### [Fable.Svelte]

These bindings were a way to make F# work with `.svelte` files. I don't have much to say about this other than it exists and you could take a look if you want but it's usage is fairly low.

For this one I don't think is a good choice for your next serious project, maybe for experiments here and there but given it's low usage there might be some bugs not found just yet in the way, Sutil would be a better choice if you like Svelte-like way of doing UIs

Svelte of course is rock solid as a choice and is one of the most popular JS frameworks out there, the problem lies on how mature are the bindings and how battle tested they are

### [Feliz.Snabdom]

Using Feliz.Engine comes Feliz.Snabdom as well snabdom has a virtual dom implementation but deals with DOM elements rather than abstract over them (like react) this gives you more wiggle room to interoperate with third party components. it provides life cycle hooks, lazy loaded elements and other features.

I'm not fan of virtual dom myself so I haven't really tried this much more than just a few examples at the same time I'm not sure of the maturity of the bindings, although snabdom has been out for years and has been used by thousands of devs the concerns lie in how portable the code is + how mature the bindings could be.

## Fable Next!

These new options are coming hot from the oven and paint a bright future for Fable integrations on the Frontend ecosystem!

Fable 4 (snake island) is going to bring `JSX` compilation, meaning that frameworks that use JSX as their DSL and building blocks will have an even easier time integrating, this support will come to Feliz.Engine as well this means a couple of things

- Stable API for the F# side (Feliz.Engine)
- Broad Target of UI frameworks by just configuring the packages you want to use (be it solidjs, vue jsx, inferno, preact etc)
- Easier migration paths between F# <-> JS

Given how JSX is still a compilation step you can always fallback to manual JSX and continue from/to JSX if needed.

### [Feliz.Solid]

This is an exciting one, [solid.js] has been going up in popularity these days because it is what react could have been

- True and Predictable reactivity
- No manual dependency tracking
- Observable support
- No Virtual DOM
- Fast and Efficient
- Small footprint Library

So if the react model is _your thing_ and you want to avoid many of the react footguns then this is something to keep an eye for

Solid code looks like this:

```fsharp
[<JSX.Component>]
let Counter() =
    let count, setCount = Solid.createSignal(0)
    let doubled() = count() * 2
    let quadrupled() = doubled() * 2

    Html.fragment [
        Html.p $"{count()} * 2 = {doubled()}"
        Html.p $"{doubled()} * 2 = {quadrupled()}"
        Html.br []
        Html.button [
            Attr.className "button"
            Ev.onClick(fun _ -> count() + 1 |> setCount)
            Html.children [
                Html.text $"Click me!"
            ]
        ]
    ]
```

As you can see it is very similar to Sutil or Feliz.Snabdom and that's because it is using Feliz.Engine as well! while these don't interoperate between themselves easy because each library defines what the DSL actually emits: DOM Elements, Virtual DOM elements they do use the same DSL so learning one it basically teaches you the others as well!

The main disadvantage of this is that is of course new, it only works on Fable 4 (Alpha at the time of writing) and thus should not be considered for your next serious project. Once Fable 4 is released for real then it could be something to really consider and contribute to.

### Fable + JSX

If you're thinking "my favorite framework is not in the list" do not worry, writing bindings for Fable has become easier over the years, specially when you take Feliz.Engine into account Fable 4 will bring JSX as well meaning that it could be even simpler to integrate to the JavaScript ecosystem.

Perhaps you like vue, but supporting vue files is too much, perhaps the framework you are using at work supports JSX this has the potential to bring a lot with minimal changes, like [Alfonso said]

> It's like programming against an interface (JSX) instead of an implementation (the compiled JS)

Although he also said there are nuances and likely differences in how each framework and its tooling treats JSX so Feliz.Engine is close to universal but not _that_ universal.

## [WebSharper]

Web sharper has been out for quite some time and has an interesting F# first approach to the UI, WebSharper aims to fulfill the full-stack F# promise hiding away some of the JS details but has some pretty good capabilities when it needs to interoperate with javascript.

Rather than a set of multiple libraries and frameworks WebSharper is a one stop shop all style of framework

A simple Web Sharper Application looks like this:

```fsharp
[<Website>]
let Main =
    Application.SinglePage (fun ctx ->
        Content.Page(
            h1 [] [ text "Hello World!"]
        )
    )
```

This will tell WebSharper to generate some JavaScript and run it directly on the body of your application.

While WebSharper has a ViewModel strategy it also offers MVU support, for example a counter can look like this

```fsharp
[<JavaScript>]
module Counter =

    type Model = { Counter : int }

    type Message = Increment | Decrement

    let Update (msg: Message) (model: Model) =
        match msg with
        | Increment -> { model with Counter = model.Counter + 1 }
        | Decrement -> { model with Counter = model.Counter - 1 }

    let Render (dispatch: Dispatch<Message>) (model: View<Model>) =
        div [] [
            button [on.click (fun _ _ -> dispatch Decrement)] [text "-"]
            span [] [text (sprintf " %i " model.V.Counter)]
            button [on.click (fun _ _ -> dispatch Increment)] [text "+"]
        ]

    let Main =
        App.CreateSimple { Counter = 0 } Update Render
        |> App.Run
        |> Doc.RunById "main"
```

or if you prefer HTML

```html
<!-- this is inside the HTML page you're serving -->
<body>
  <button ws-onclick="OnDecrement">-</button>
  <div>${Counter}</div>
  <button ws-onclick="OnIncrement">+</button>
  <script type="text/javascript" src="Content/Counter.min.js"></script>
  <!--[BODY]-->
</body>
```

```fsharp

[<JavaScript>]
module Client =
    // The templates are loaded from the DOM, so you just can edit index.html
    // and refresh your browser, no need to recompile unless you add or remove holes.
    type MySPA = Template<Snippet.IndexHtml, ClientLoad.FromDocument>

    type Model = int

    type Message =
        | Increment
        | Decrement

    let update msg model =
        match msg with
        | Message.Increment -> model + 1
        | Message.Decrement -> model - 1

    let view =
        let vmodel = Var.Create 0

        let handle msg =
            let model = update msg vmodel.Value

            vmodel.Value <- model

        MySPA()
            .OnIncrement(fun _ -> handle Message.Increment)
            .OnDecrement(fun _ -> handle Message.Decrement)
            .Counter(V(string vmodel.V))
            .Bind()

        fun model ->
            vmodel.Value <- model

    let Main =
        view init

```

[There's a whole website with demos you can try!](https://try.websharper.com/)

WebSharper also offers a reactive model that can be used in exchange of the MVU architecture, the last example could also be simplified to the next example:

```fsharp
[<JavaScript>]
module Client =
    // The templates are loaded from the DOM, so you just can edit index.html
    // and refresh your browser, no need to recompile unless you add or remove holes.
    type MySPA = Template<Snippet.IndexHtml, ClientLoad.FromDocument>
    let counter = Var.Create 0
    let Main =
        MySPA()
            .OnIncrement(fun _ -> counter.Value <- counter.Value + 1)
            .OnDecrement(fun _ -> counter.Value <- counter.Value - 1)
            .Counter(V(string counter.V))
            .Bind()
```

This reactive style is similar to the new [Vue's Composition API] so regardless of your choices WebSharper has you covered.

That being said, on my twitter bubble WebSharper is not one of the most popular ones and I'm not entirely aware why my guess is that it tries to hide JavaScript as much as possible to try to stay in F# and that could create some sort of vendor lock in and friction in some cases, this doesn't mean it is a bad choice though if you don't want to build on top of the JavaScript ecosystem that much it looks like solid technology to pick up specially because it offers paid support so this can be a good fit for teams rather than individuals.

**If you want to learn more about WebSharper let me know, so I can explore more and dedicate a couple of blog posts to it.**

## Web Assembly

In this Section:

> - Bolero
> - Fun.Blazor
> - Avalonia.FuncUI

Web Assembly is the _newest_ player in the game and one that is being/will be a true game changer with it comes to web development, just as a taste of it's power you can now [use photoshop natively in the browser] now, what does that mean for you as a .NET developer?

It means that you can run F# code (or C# if that's your jam) natively on the browser, no intermediary JavaScript that you have to touch and you keep the safety of F#

### Why Web Assembly and who benefits most from it?

Web assembly (WASM) is meant for users who want to run native code in the browser, this has a few implications, WebAssembly does not have so far access to DOM and neither a garbage collector, so for WASM apps to work with .NET you need to load a .NET runtime + your application's code. That means that any time your website is visited you have to wait a few seconds while your web app loads the runtime + your code.
Any time that any of these technologies have to they need to share information with the JS world and this can be costly, while you as an App developer don't have to do it manually you still need to be wary of the costs of serialization/deserialization that are made any time you share information with the JS world, be it in form of big UI trees, large amounts/rows of data and similar situations.

That being said, if you can afford these drawbacks then you will be able to enjoy F# safety in all of its glory, no more weird JavaScript emits, or trying to bind an interface to a JavaScript object and hopefully that holds true at runtime. It's just the real deal.

You can leverage the .NET ecosystem when it comes to libraries with all of their patterns and knowledge you already may have. This also means that since you are using .NET you can share logic and data 100% with the server, after all .NET6 libraries (unless they are using OS specific APIs) work on ASP.NET core for the server and WASM, this means no shared folders with tweaked paths and `#if FABLE` or similar directives it's just the assembly being shared as is.

While you don't need to interact with JavaScript at all, you can do so if you must there are ways to interoperate with functions declared in the global scope or even in JavaScript modules.

So you're not entirely isolated you can interact with the outside world if needed.

> **DISCLAIMER**: Most of these alternatives rely on [Blazor] which is the product solution offered by Microsoft to tap into WebAssembly with C# as usual F# is not in the roadmap but the community always jumps in to save the day and offer F# devs the experiences they deserve.

## [Bolero]

Bolero is the most stable and mature solution for F# web assembly, it is brought to you by the same devs behind web sharper, in a way it could be the next step in their path to make web applications with F#. Bolero offers most of the functionalities of web sharper but native this time, HTML Templates via a Type Provider, Client Routing with Discriminated unions, F# DSL, MVU.

Your typical Bolero view looks like this

```fsharp

type Model = { value: int }
let initModel = { value = 0 }

type Message = Increment | Decrement
let update message model =
    match message with
    | Increment -> { model with value = model.value + 1 }
    | Decrement -> { model with value = model.value - 1 }

let view model dispatch =
    div {
        button { on.click (fun _ -> dispatch Decrement); "-" }
        string model.value
        button { on.click (fun _ -> dispatch Increment); "+" }
    }

let program =
    Program.mkSimple (fun _ -> initModel) update view

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program = program

```

It is pretty similar to the other MVU samples we've seen so far bolero also offers interoperation with Blazor concepts like external components from third party libraries, remoting (RPC client-server style of communication) and of course Pure F#.

If you liked the approach that WebSharper gave you and you're looking for the next step then bolero will be just what you want you can translate concepts and knowledge from there. The sad part for me is that it doesn't have a reactive style when it comes to handling state MVU is great but on larger applications it just doesn't cut it for me, the nice thing of the bad is that you can create multiple elmish components in your application and use parameters so you don't have a single master Elmish update function and rather each component have it's own state.

## [Fun.Blazor]

This is another abstraction built on top of Blazor, it is the new kid in town and has some very enticing models when it comes to handle state. Fun.Blazor recently put out its version 2.0.0 which adds a bunch of nice things all around, it allows you to create UI's with F# Computation Expressions (CE) in a similar fashion to bolero, it also has the ability to use string templates (much like Fable.Lit) and has some options for routing like a giraffe style router it offers seamless integration with blazor's dependency injection, very useful to interoperate with JavaScript and other Blazor services.

Your typical Fun.Blazor component looks like this:

```fsharp
adaptiview() {
    let count, setCount = cval 0 // changeable value

    h1 { $"Counter: {count}"}

    button {
        onclick (fun _ -> setCount count + 1)
        "Increment"
    }

    button {
        onclick (fun _ -> setCount count - 1)
        "Decrement"
    }
}
```

Fun.Blazor brings the power of `FSharp.Data.Adaptive` to allow incremental updates to the view, this package works like excel cells, where one cell may be the source of truth for others, the other cells can recompute values depending on the first cell this allows for performant updates in the UI because only the _reactive_ parts change. This model is very close to Sutil's model as well In fact you can use stores in Fun.Blazor as well

```fsharp
let myComponent() =
    html.comp(fun (hook: IComponentHook) ->
        let counter = hook.UseStore 0
        let double =
            store.Observable
            |> Observable.map(fun n -> n * n)
            |> AVal.ofObservable counter.Current hook.AddDispose

        adaptiview() {
            let! countValue, setCount = counter.WithSetter()
            let! doubleValue = double

            h1 { $"Counter: {countValue}, Double: {doubleValue}"}

            button {
                onclick (fun _ -> setCount countValue + 1)
                "Increment"
            }

            button {
                onclick (fun _ -> setCount countValue - 1)
                "Decrement"
            }
        }
    )
```

Adaptive views are a concept that I'd love if other frameworks implemented because the reactive model really resonates with me that being said, I know you want to see elmish in action, so I'm pleased to tell you that yes, it supports MVU as well

```fsharp
type Model = { value: int }
let initModel = { value = 0 }

type Message = Increment | Decrement
let update message model =
    match message with
    | Increment -> { model with value = model.value + 1 }
    | Decrement -> { model with value = model.value - 1 }
// using elmish directly
html.elmish (init, update, fun state dispatch ->
    div {
        h1 { $"Count: {state.value}" }
        button {
            onclick (fun _ -> dispatch Increment)
            "Increment"
        }
        button {
            onclick (fun _ -> dispatch Decrement)
            "Decrement"
        }
    }
)

// using elmish with adaptive views
html.comp (fun (hook: IComponentHook) ->
    let state, dispatch = hook.UseElmish(init, update)
    div {
        adaptiview() {
            let! count = state
            h1 { $"Count: {count.value}" }
        }
        button {
            onclick (fun _ -> dispatch Increment)
            "Increment"
        }
        button {
            onclick (fun _ -> dispatch Decrement)
            "Decrement"
        }
    }
)
```

Fun.Blazor has a lot of potential it is still young it needs more real world usage to validate many of the efforts taken in v2.0.0, although young it feels like a solid option, but keep in mind that just like Sutil it has only one maintainer, so if you like this you should look into contributing to the framework because it feels like really good option.

## [Avalonia.FuncUI]

This one might come as a surprise for many because Avalonia is a **_Desktop_** application framework! but as seen in this [Avalonia.FuncUI WASM Template] it is possible to bring the power of avalonia into the desktop via WASM, The main advantage of Avalonia.FuncUI is that you will be able to share code between browsers, android, ios, mac, linux, and windows.

Avalonia.FuncUI was also recently updated and added this reactive-like model in v0.5.0

```fsharp
Component(fun ctx ->
    let state = ctx.useState 0
    DockPanel.create [
        DockPanel.verticalAlignment VerticalAlignment.Center
        DockPanel.horizontalAlignment HorizontalAlignment.Center
        DockPanel.children [
            TextBlock.create [
                TextBlock.dock Dock.Top
                TextBlock.text (string state.Current)
            ]
            Button.create [
                Button.dock Dock.Bottom
                Button.onClick (fun _ -> state.Current - 1 |> state.Set)
                Button.content "-"
            ]
            Button.create [
                Button.dock Dock.Bottom
                Button.content "+"
                Button.onClick (fun _ -> state.Current + 1 |> state.Set)
            ]
        ]
    ]
)
```

The concepts of `IWritable<'T>` and `IReadable<'T>` work just like adaptive/changeable/stores/observables that we've seen before in sutil/fun.blazor so Avalonia.FuncUI could start becoming a competitor in the Web landscape specially if you already have some desktop app development experience, this is part of the power of WASM in practice in the case of Avalonia.FuncUI WASM you don't really need to know anything of web development to get started, just jump in!

That being said, Avalonia uses Skia to render in a canvas (likely using webgl) so you won't have any kind of DOM nodes to inspect and also as far as I'm aware (and I'd love to be corrected if necessary) you throw accesibility out of the window because of that so assistive technology won't work with this kind of web sites, it also worth noting that as far as I'm aware (at the time of writing) Web support in Avalonia is in beta state so there might be a couple few bugs lurking out there.

## MAUI

The elephant in the room here would likely be MAUI because it also has blazor support but I'm going to be very dismissive here and I plead guilty about it because it is a Microsoft product, I'd prefer to Microsoft to step out of the way of its .NET ecosystem and I'd love to better cultivate alternatives like Uno/Avalonia, but whatever right?

- Good Luck about F# support
- Good Luck to have linux support

Both things can be done with Avalonia without any major issues, also all of the other alternatives discussed here can be developed on any of the three major operating systems heck even from your Raspberry Pi 4 that's not something I'd expect in MAUI soon.

## Takeaways

That being said the F# Frontend Landscape is not that big it might feel confusing with all the tweets and news going all over the place but thankfully as most things F#: We are pretty much settled how we use things and even if there is variety most alternatives can live together in one way or another.

Here's the **_tl;dr_**:

Use Fable if:

- You want to take advantage of the JavaScript Ecosystem
- You want to have the possibility to migrate away from F# if necessary
- You want the lowest amount of kb of resources to the browser.
- You must interact with JavaScript on a heavy basis
- You Like and want to use React or Lit or Vanilla (Sutil)

Don't use Fable if:

- You really dislike JS
- You want true type safety
- You can afford go vanilla F# (i.e. not use most of the JS ecosystem)
- You don't want to learn or deal with the JavaScript Tooling

Use WebSharper if:

- You don't mind about going F# first JS second
- You want type safe HTML via type providers
- You want to blur the lines between client side F# and server side F#
- You don't care too much of the toolchain and only care about final deployable assets

Don't use WebSharper if:

- You need to literally make manual js files and adjustments to the compiling toolchain
- You need a more JavaScript oriented application
- You need to build on top of the existing node tooling
- You care about an extra runtime for your application (around 8kb)

Use Web Assembly if:

- You want to run native F# in the browser not a fake one
- You want to leverage the .NET ecosystem
- You want to enjoy .NET tooling to publish, distribute and build your F#
- You want to share code between Desktop, Server, and Mobile (like Avalonia will allow you)

Don't use Web Assembly if:

- You don't want to ship heavy websites (even if there's trimming support for some libs)
- You need low TTI (time to interaction) and TFP (Time to Fist Painting)
- You need a more mature ecosystem
- You need heavy JS interop

## Personal Opinion

No one asked for it, and no one should because my opinion shouldn't have any weight on your decision making.
That being said...

My personal top is:

0. Fable.Lit
1. Sutil - Fun.Blazor
2. Feliz.Solid?

The main reason is that Fable.Lit checks the boxes when it comes to web standards and I'm primarily a frontend developer and React is just not my thing mainly due to it's focus on hooks which in the case of React they might make sense but are too magical and very prone to errors and performance issues if badly used (I'm looking at you useFootGun, I mean useEffect)

Sutil comes in second because of it is extremely awesome to have a pure F# framework that also offers a reactive state management model it simply fits my mental model of doing websites.

Fun.Blazor comes swiftly on second place as well for the Tie because it takes the same concepts sutil uses for state management and takes it a step further

Feliz.Solid comes in third because it also offers a reactive model that is likely going to replace React in a lot of places and codebases in the future. It doesn't suffer of the same problems React has and its future looks Bright, its author was recently (at the time of writing) at Netlify one company that has been hiring a lot of extremely good talent like Rich Harris (author of Svelte) so it can only go up from here while the Feliz integration matures.

## Conclusion

So there you have it I hope this post sheds some light on what the current state of F# frontend is and what you should take into account if you want to choose one or the other alternatives we have.

Ultimately you should not be pressured to _chose right_. F# solutions even if they are not _"mature"_ are pretty solid, after all that's the main reason we chose F# to either work with or have fun (sometimes both) you should avoid one of these alternatives if you truly have reasons not to pick it otherwise it's likely going to work your needs.

These frameworks are excellent work of members of the F# community even if they look _young_ or are in _beta_ these tools are extremely well done and are far more capable than what you would think when you hear those words give them a try give feedback to its authors and remember not everything is React or derivatives you have choices today :)

Until the next one, don't forget to leave your comments and questions if needed!
