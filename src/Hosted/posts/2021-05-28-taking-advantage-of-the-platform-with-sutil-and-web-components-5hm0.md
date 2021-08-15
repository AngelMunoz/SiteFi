---
title: Taking Advantage of the platform with Sutil and Web Components
subtitle: ~
categories: fsharp,svelte,webcomponents,litelement
abstract: Hello everyone, here we are once again with another F# blog post.   This time I want to bring attenti...
date: 2021-05-28
language: en
---

[Svelte]: https://svelte.dev/
[Feliz]: https://zaid-ajaj.github.io/Feliz/
[React]: https://reactjs.org/
[Fable Compiler]: https://fable.io/
[Web Components]: https://developer.mozilla.org/en-US/docs/Web/Web_Components
[Elmish Book]: https://zaid-ajaj.github.io/the-elmish-book/#/

Hello everyone, here we are once again with another F# blog post. 

This time I want to bring attention to a project that has caught my eye and fits my way of doing web development from F#

{% github davedawkins/Sutil %}

Sutil is an abstraction over [Svelte] in contrast to [Feliz] which is an abstraction over [React] both projects allow you to do web development the only (and radical) difference is that when you do your F# there's a different engine under the hood when you website runs.

If you have control over the SPA you're building you can use whatever you want that is for sure but, let's say you work for a company who has multiple products and then something like this happens:

> We're going to go under a transformation process, we will create a design system for our branding and all of our applications will use the same core components, we have formed a team that has chosen the *Lit | Stencil | FAST* library, don't worry you will be slowly replacing parts of existing applications with these core components in the future.

Since web components work on all modern browsers and are framework agnostic since they work as native tags e.g. you can use them inside Vue, Aurelia, Angular, Svelte, you name it (even react with some caveats).

So now you have to make company's components work with your existing (or new) Fable SPA's.

> The source code for this post can be found in this repository
> {% github AngelMunoz/sutil-and-web-components %}

### A word on Web Component Distribution
Web components are usually distributed as ES Modules (sometimes with polyfills to port back to older browsers) and are often easy to install

> ```html
> <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.0.0-beta.41/dist/themes/base.css">
> <script type="module" src="https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.0.0-beta.41/dist/shoelace.js"></script>
> ```

Just drop those tags and you can start using shoelace components in your application, no bundling, no Webpack, no preprocessors no whatever you fear from the JS ecosystem. which make them compelling in some places where no JS frameworks are actually that useful (e.g. server side rendered applications or multi page apps)

In our case we're building a Sutil SPA which it means we're likely using a bundling system like Snowpack/Webpack so we will have to do the classic  `npm install @shoelace-style/shoelace` and then import every component we want with side effects because we don't want our app bundle to grow massively in size.

Now back to the integration, web components are often registered like this 
```js
class MyComponent extends HTMLElement { 
    /*... */
}

customElements.register('my-component', MyComponent);
```

> different libraries define them different but the `customElements.register` is for EVERY component out there.

therefore most of the time what you'll see when dealing with web components will be a single import

```js
import 'my-component.js'
```

> In our case I'll be using [Shoelace](https://shoelace.style/) as the web component library in this repo.

Having that said, if we take a look at `Main.fs`. We're importing  each component as needed

```fsharp
module Main

open Fable.Core
open Sutil.DOM
open Fable.Core.JsInterop

importSideEffects "./styles.css"
importSideEffects "@shoelace-style/shoelace/dist/themes/base.css"

importDefault "@shoelace-style/shoelace/dist/components/button/button.js"
|> ignore

importDefault "@shoelace-style/shoelace/dist/components/skeleton/skeleton.js"
|> ignore

[<ImportMember("@shoelace-style/shoelace/dist/utilities/base-path.js")>]
let setBasePath (path: string) : unit = jsNative

// this requires a specific configuration for shoelace
// check snowpack.config.js
setBasePath "shoelace"

// Start the app
App.view () |> mountElement "sutil-app"
```
> Usually we would use importSideEffects "the-library/component.js" (like above) but the documentation of shoelace says that we should do default imports with their particular implementation to prevent bloated bundles and enable tree shaking hence why we import and ignore at the same time.
>
> Please note the `.js` at the end (it's very important for snowpack to work properly, you can ignore it in the case of other bundlers as far as i know)

What we just did is to import the library's (either third party or your company's initiative one) components into the browser, now every time we write a `sl-button` or `sl-skeleton` the browser will understand that a custom element will be rendered.

Generally speaking Web Components work in the following way
- Pass attributes for values

      which can set internally a property, please note also that properties are not the same as attributes
- Emit Events/CustomEvents so you can update attributes/properties as you need

> There are some cases where web components need you to call a method of that instance so you will have to query for a reference of that element to get the instance and then invoke the method. 

That means that 80%-95% (based on my not comprobable experience) of the time you would just define attributes and listen for events.

### Using Web Components In Sutil

The Sutil DSL is very complete and permissive where needed i.e. you can use 
- `Css.custom("align-self", "stretch")`
- `Attr.custom("some-attribute", "my-value")`
- `Html.custom("my-tag", [])`
- `on "event-name" handler modifiers`
- `onCustomEvent<'T> "event-name" handler modifiers`

If you can't find a property in the Sutil DSL you can report it to the repository but with these helpers you can easily continue working without having to wait for a fix.

There are several ways we can use these web components but we'll start with the most raw one.

```fsharp
Html.custom ("sl-button", [
    type' "sucess"
    text "This is a Web Component Button"
    onClick (fun _ -> printfn "Hey success!") []
])
```

> Check https://shoelace.style/components/button for the component documentation.

Cool that should give us a green button on our screen if we imported the button in the main file

let's try a circle button with an icon
```fsharp
Html.custom (
    "sl-button",
   [ Attr.custom ("circle", "")
     Attr.custom ("size", "large")
     onClick (fun _ -> printfn "Hey circle!") []
     Html.custom ("sl-icon", [ Attr.custom ("name", "gear") ]) ]
)
```
this should give us something like this

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/wehd9c25xezfapxgsgza.png)

no effort at all well perhaps a few keystrokes but that's it no need for wrappers, no need for bindings just the standard sutil bindings. let's try something fancier, something that emits an event let's try a menu

```fsharp
let printValue (e: Event) =
    // current work around until a new release with `onCustomEvent<'T>` is out
    let event =
        (e :?> CustomEvent<{| item: {| value: string |} |}>)

    match event.detail with
    | Some event -> printfn $"Got: {event.item.value}"
    | None -> printfn "Got nothing"

Html.custom (
"sl-menu",
[ Html.custom ("sl-menu-item", [ Attr.value "First"; text "First" ])
    Html.custom ("sl-menu-item", [ Attr.value "Second"; text "Second" ])
    Html.custom ("sl-menu-divider", [])
    on "sl-select" printValue [] ]
)
```


> Although, we know here that we want to know the `value` property sometimes to have proper support for the element type we will need to create a proper binding

handling custom events isn't that hard either even if we had to put a workaround which shouldn't be the case once the next release of Sutil (at the current time of writing) is out.

There are some components that are a little bit more complex like a drawer or a dialog which have actual methods for those elements you will need to write a small binding or if the component allows it act on it with its attributes/properties

let's check a dialog as an example
```fsharp
type SlDialog =
  inherit HTMLElement

  abstract member show : unit -> JS.Promise<unit>
  abstract member hide : unit -> JS.Promise<unit>

let openDialog (e: Event) =
    let dialog = document.querySelector ("sl-dialog")
    (dialog :?> SlDialog).show () |> ignore

let closeDialog (e: Event) =
    let e = (e.target :?> HTMLElement)
    (e.parentElement :?> SlDialog).hide () |> ignore

Html.custom (
    "sl-button",
    [ type' "warning"
      text "Open Dialog"
      onClick openDialog [] ]
)

Html.custom (
    "sl-dialog",
    [ Attr.custom ("label", "My Dialog")
      Html.custom (
        "sl-button",
        [ type' "primary"
          text "Close"
          Attr.custom ("slot", "footer")
          onClick closeDialog [] ]) ])
```

I think this might be the "worst case" scenario given that you would have to manually query for the element in the DOM, then add a binding (which is just extending HTMLElement).

On the easier side we can do most of the hide/show via attributes/properties, let's create a function that renders an alert on the screen when its open property changes.

When you use Sutil it's very likely that you are using `Stores` to manage state. We will work as if this was a more ready to use component rather than a simple example.

```fsharp
(* Components/Alert.fs *)
type SlAlertProps =
  { closable: bool
    duration: float option
    open': bool
    type': string option }

let Alert (props: IStore<SlAlertProps>, content: NodeFactory seq) =
  let closable: IObservable<bool> = props .> (fun props -> props.closable)

  let duration: IObservable<float> =
    props
    .> (fun props -> props.duration |> Option.defaultValue JS.Infinity)

  let open': IObservable<bool> = props .> (fun props -> props.open')

  let type': IObservable<string> =
    props
    .> (fun props -> props.type' |> Option.defaultValue "info")

  Html.custom (
    "sl-alert",
    [ Bind.attr ("closable", closable)
      Bind.attr ("duration", duration)
      Bind.attr ("type", type')
      Bind.attr ("open", open')
      yield! content ]
  )
```

> `.>` is an operator. It takes a store and maps a function to create an observable of the result of said function, this is the same as doing:
>
> `Store.map (fun store -> store.prop) existingStore`

That's our "reusable" component/function let's see how it is being used at Home.fs

```fsharp
(* Pages/Home.fs *)
let alertStore : IStore<SlAlertProps> =
    Store.make
        { closable = true
          duration = Some 3500.
          open' = false
          type' = None }

Html.section [
    // remember stores are observables under the hood, so don't forget to dispose them
    // when you're done with them or you will have memory leaks
    disposeOnUnmount [ alertStore ]
    Html.custom (
        "sl-button",
        [ text "Open Alert"
          type' "info"
          onClick
            (fun _ ->
              // set the open property to true
              alertStore
              |> Store.modify (fun store -> { store with open' = true }))
            [] ]
      )

    Alert(
      alertStore,
      // remember this is the content of our sl-alert
      [ Html.p [
          text
            "This is a sample on how you can make components with from existing libraries that may fit better in your applications"
        ]
        Html.custom (
          "sl-button",
          [ text "Close"
            onClick
              (fun _ ->
                // set the open property as false
                alertStore
                |> Store.modify (fun store -> { store with open' = false }))
              [] ]
        ) ]
    )
]
```

## Recap

When dealing with web components (either from our design system or third party individual components) we want to do a few things

- Import the Element via Script Tag or ESModule Import
- Use any of the following to define your element, its attributes and react to its changes
    - `Html.custom("", [])`
    - `Attr.custom("", "")`
    - `on "event-name" handler []`
- style the css parts it has

while I didn't touch styling, you'll find that I overrode some variables in the styles.css file at the bottom
```css
@media (prefers-color-scheme: dark) {
  :root {
    --su-background-color: #2f2f2f;
    /* The following are defined by the shoelace library and changed by us to
       let the component adapt to our color scheme */
    --sl-color-gray-700: var(--su-color);
    --sl-color-primary-500: var(--su-color);
  }
}
```
> If there's need for a styling write up as well let me know.


## Closing thoughts

If you like the React way of doing things (which fits completely in the functional programming realm) feel free to visit the (extremely good, even if you're not choosing Feliz give it a read) [Elmish Book] which can give you a really good guidance on how to develop SPA's with F# and the [Fable Compiler].

If you like svelte or you don't want to do everything react style (hooks, context, etc) then Sutil offers you an alternative that is quite compelling based on observables and works as any other framework. That means you can go back to certain browser API's that are automatically ruled out when using React like the Events and CustomEvents. [Web Components] are not ruled out but react [has some friction](https://reactjs.org/docs/web-components.html) with them.

Let me know what you think! ping me on twitter or in the comments below 😁 have an awesome weekend!
