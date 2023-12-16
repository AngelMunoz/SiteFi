---
title: Revisiting WASM for F#
subtitle: ~
categories: fsharp, dotnet, webdev, fsadvent
abstract: It is time for the FsAdvent post for this year! Let's see how's WASM faring in the F# world!...
date: 2023-12-16
language: en
---

Hey there folks! It has been a while!

As you may be aware with my content at this point I do a bunch of F# on my free time, on my not so _free time_ I do web development.

I am a big fan of going with web components + plain (build-less) javascript whenever possible, so it is not surprising that I often favor things like the [Fable Compiler], where I can target my F# code directly to javascript and be as close to the native JS experience as possible, both for interop concerns and for ecosystem integration.

And while JS is still the best course of action today to do front-end development, I think with the release of [dotnet] (`#dropthedot`) 8 we're bound to re-visit how is Blazor doing.

> If you don't know what [Blazor] is the Tl;Dr would be that it is a framework for building dotnet apps for WASM and running dotnet code in the browser like F#, be sure to visit Microsoft docs for more information though!

### Blazor itself!

Before we go down into the F# code, I'd like to offer some of the new and shiny things that may be relevant for you when you evaluate if Blazor is good for your or your organization.

It is no secret that the talented folks at Microsoft can produce and improve very well crafted software when executives don't get in their way and I think Blazor is one of those pieces. On each release since dotnet 6 things have improved from trimming, AoT, runtime size, and, performance among others, they have also introduced different ways to render wasm in your dotnet apps.

- Pre-rendering - Server Only no interactivity
- Server Interactivity - Server Only, interactivity via websockets
- Client WASM - Client only loads the whole app + runtime in the browser
- `Auto Interactive` - best of the 3 worlds, pre-renders, provides interaction while wasm loads, once wasm loads it stays there

I was going to write a slightly longer part on this topic but to be honest [Dustin wrote a much better and comprehensive post about it], so you should check him out.

I agree with most of the conten on his post (specially when he says "I've never been the person who blindly champions every Microsoft technology without critical thought") except on two sides

- `Auto Interactive` mode is more akin to meta-frameworks like `Nextjs`, `Remix`, `Qwik`, and not so with `SPA` frameworks like `Angular`, `React` or `Vue`

  This means that only client wasm should be compared to the later, and the former should be compared to blazor as a whole not just one segment of it, from that lens blazor isn't really that far behind and can actually provide a really performant and close to today JS frameworks, though Dev experience is still behind as there is no reliable hot reload (specially for F# where there's no hot reliad at all).

- "The parallels with Silverlight are hard to ignore"

  I would also say that **_IF_** blazor worked on a browser plugin like silverlight did, today that's not the case it is built on the [webassembly] standard which and it is being adopted in the browsers which means once it gets on the web, it is unlikely to ever go out again. Even if Microsoft themselves leave Blazor today, it can still work, the burden of creating a fork and keeping blazor alive will certainly be big but someone will be able to do that, just like the [open silver] folks revived silverlight via wasm tech without any particular Microsoft involvement.

That being said, let's talk about the stuff we're here for.

### The F# bits

Over the past month I went down to try two of the most prominent libraries that build on top of Blazor

- [Bolero]
- [Fun.Blazor]

Last time we saw them was at the frontend review I wrote a couple of years ago

> ## [Exploring the F# Frontend Landscape](https://dev.to/tunaxor/exploring-the-f-frontend-landscape-13aa)

But it is also nothing too extreme.

Essentially both options remain virtually the same in the sense that those simple example I showed there still work.

If it is not too cynical I think that might already be a point for wasm as you you may know how easy is to JS codebases not work after a couple of years pass by 😆. Well yeah maybe to cynic but, it is indeed a strong point for wasm codebases and specially dotnet ones, if the code worked before it is almost guaranteed that it will be working in a few years without any modifications at all.

> **_Note_**: The examples and use cases I'll show below require interactivity, either with WASM only mode or any other of the previously mentioned rendering modes in Blazor, so keep that in mind.

#### Bolero

Let's dive into Bolero. This framework doesn't shy away from it's functional flavored style it goes straight into the MVU paradigm and it is the most common way you'll see code around, our previous example was the following:

```fsharp

type Model = { value: int }

type Message = Increment | Decrement

let initModel = { value = 0 }

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

type MyApp() =
    // TheProgramComponent class implements the bits
    // that interop with blazor to make elmish work
    inherit ProgramComponent<Model, Message>()

    override _.Program =
        // here we start our elmish loop and let it do it's thing
        Program.mkSimple (fun _ -> initModel) update view
```

The elmish loop as usual provides predictability and a traceable code flow, however one of the main criticisms of MVU is the fact that once the code starts growing larger the update function becomes massive and starts losing the appeal.

While there's arguments from both sides if that's a bad thing at all, Bolero offers a way to abstract certain parts of your code in `Elmish components` which can be used to further de-couple the main update events with inner events in certain parts of your screen.

Let's say for example that we want a modal-like component that can be

- Entirely Dismissed
- Cencelled
- Accepted with value

```fsharp
type ModalError =
  | Cancelled
  | Dismissed

type ModalInfo = {
  header: string;
  message: string;
}

type MyModal() =
    inherit ElmishComponent<ModalInfo, Result<unit, ModalError>> =

    override this.View model dispatch =
      let { header = modalHeader; message = message; okValue = okValue } = model
      dialog {
        // For simplicity we'll let the parent control the visibility of this modal
        // so we'll set it to true rather than a dynamic value.
        attr.``open`` true
        // You could certainly handle this locally within the component.
        header {
            h3 {
                modalHeader
            }
            button {
                on.click(fun _ -> dispatch (Error Dismissed))
                "❌"
            }
        }

        p { message }

        button {
          attr.autofocus true
          on.click (fun _ -> dispatch (Error Cancelled))
          "Cancel"
        }

        button {
          on.click (fun _ -> dispatch (Ok ()))
          "Ok"
        }
      }
```

With styling aside, the general structure of our modal would look like that.

> **_Note_**: As a general rule (regardless Blazor or Bolero) when you design components you want to pass the information into the components via props/parameters and any modifications hoisted to the parent via events/callbacks this enables unidirectional flow that makes it less prone to have bugs in your code and also de-couples the component of knowing what to do with the information once it "has changed".

In your parent you would use it like this:

```fsharp
// assuming we've defined already our message and our model before.
let view model dispatch =
  let translations = getTranslations model.language
  article {

    // content off your view

    // somewhere dispatch a message to open the modal
    button {
      on.click(fun _ -> dispatch OpenModal)
      "Open Modal"
    }

    cond model.showModal <| function
    | true ->
      ecomp<MyModal, _, _>
        { header: translations["modalTitle"]; message: translations["modalMessage"] }
        (fun value ->
          match value with
          | Ok () -> dispatch CanContinue
          | Error _ -> dispatch ShowAlternativeFlow
        )
    | false -> empty()
  }
```

As you can see this is a fairly simple way to keep constant with the MVU pattern but also allowing for internal messages to not leak into the main elmish loop.

This is not new though and it is well documented in their website so you should check them out when you have a chance.

> **_Note_**: If you're interested in more Bolero specific examples for frontend use cases, let me know I'd be happy to write about those of that can help folks out there.

And if you include a few updates for Elmish V4 like [reusable subscriptions] then the MVU pattern becomes easier to manage.

Bolero clearly goes for the more functional side of frontend development and it works nice. There are still a few extra features I haven't talked about like remoting, routing but bolero is a solid choice if you want to bring your existing skills to the frontend development without getting lost into quest to learn how to do modern web development in the JS world.

#### Fun.Blazor

This framework also supports MVU via its [Fun.Blazor.Elmish] package but let's take a look at what's in the box.

If you have keep up with the frontend development landscape in JS-land then you may know that today's sauce is using "hooks" or "signals".
Hooks are a react invention to react problems, and while they aren't really needed in many frameworks today (because they don't have react problems) they provide good developer experience to handle unidirectional flow and local state management and that made them quite popular even outside react.

On the signals front, folks have gone full circle in their quest for wheel re-invention and currently we landed back into observables as the primitive for reactive state.
Thankfully this time we're not entirely back at square 0 with signals, as one of the major proponents [Ryan Carniato] is a very skilled person and more importantly he `remembers history`, something the frontend folks tend to not do when they're iterating and stomping accedentally in concepts already tried in the past.

This iteration of signals is very much appreciated and for the developers it looks almost like if they were using hooks.

With that context, I'd like to show you [FSharp.Data.Adaptive] which is an abstraction for reactive data that works similarly to excel cells, and pretty much fits the shape of signals in the frontend

```fsharp
type MyCounter() =
  inherit FunBlazorComponent() =

    let state = cval 0

    override this.Render() =
      article {
        button { onclick(fun _ -> state.Publish(fun state -> state + 1)) }

        "Counter: "

        adaptiview() {
          let! counter = state
          $"{counter}"
        }

        button { onclick(fun _ -> state.Publish(fun state -> state - 1)) }
      }
```

In the example above our local state is handled by _changeable_ values, which will drive any other computations and the Adaptive model will take care of caching and checking for value changes that affect how often our views render, in the case above only the `adaptiview()` node will be re-rendered any time the state changes, the rest of the contents remain static which contrasts with the MVU way to always re-render regardless of state changes.

If we go back to the modal example from above it would look somewhat like this:

```fsharp
type ModalError =
  | Cancelled
  | Dismissed

type ModalInfo = {
  header: string;
  message: string;
}


module Modals =
  let MyModal (modalInfo: ModalInfo, onAction: unit -> Result<unit, ModalError>) =
    let { header = modalHeader; message = message } = modalInfo
    dialog {
      // For simplicity we'll let the parent control the visibility of this modal
      // so we'll set it to true rather than a dynamic value.
      attr.``open`` true
      // You could certainly handle this locally within the component.
      header {
        h3 { modalHeader }
        button {
            onclick(fun _ -> onAction (Error Dismissed))
            "❌"
        }
      }

      p { message }

      button {
        attr.autofocus true
        onclick (fun _ -> onAction (Error Cancelled))
        "Cancel"
      }

      button {
        onclick (fun _ -> onAction (Ok ()))
        "Ok"
      }
    }

type MyApp() =
  inherit FunBlazorComponent()

  override this.Render() =
    article {
      // your view's content

      // trigger the dialog
      button {
        onclick (fun _ -> state.Publish (fun state -> { state with modalOpen = true }))
        "Open Modal"
      }

      adaptiview() {
        let! (state, setState)  = state.WithSetter()

        let { modalOpen = isOpen; modalInfo = modalInfo } = state

        if modalOpen then
          Modals.MyModal(
            modalInfo,
            (fun result ->
              match result with
              | Ok() ->
                setState({ state with modalOpen = false })
                continue()
              | Error _ ->
                setState({ state with modalOpen = false })
                showAlternativeFlows()
            )
          )
      }
    }
```

Now, it certainly makes it simpler to write and to reason about what is updating what, however it can introduce complexity when coordinating other parts/features of the view you're currently in, in our MVU example we just dispatched another message, and here we're calling other functions, which they may be callbacks or part of the view's current function.

> **_NOTE_**: Similarly to the MVU message, if you are able to pas information as parameters/props and hoist state to the parent via events/callbacks then this will be simpler to reason about.

### Interop with JS

While both Bolero and Fun.Blazor provide means for your F# code to shine without the pain that may come with JS tooling (specially if you don't work with that in a day to day basis) until WASM gets DOM or Browser API access you still have to fallback to JS when that's the case. This is when we step back from the F# framework side and lean on the Blazor layer.

For both Bolero an fun Blazor you should be able to use dependency injection by standard means.

First let's create a common interface and a simple factory for our service.

```fsharp

type ILocalStorage =
  abstract getItem: string -> Task<string option>
  abstract setItem: string * obj -> Task<unit>

module LocalStorage =
  let factory (services: IServiceProvider) =
    let js = services.GetService<IJSRuntime>()

    { new ILocalStorage  with
      override _.getItem key = task {
        let! content = js.InvokeAsync<string>("localStorage.getItem", key)
        return content |> Option.ofObj
      }

      override _.setItem (key, value) = task {
        do! js.InvokeVoidAsync("localStorage.setItem", [| box key; value |])
      }
    }
```

With that factory, we can then register that in our `Startup.fs` file or wherever we are currently registering our DI services

To get a reference in a bolero component, it is fairly straight forward:

```fsharp
type Services = {
  jsRuntime: IJSRuntime
  localStorage: ILocalStorage
}
module MyApp =

  let update (services: Services) model message =

    match message with
    // in cases you already have registered service you can pass that to the update function
    // similar to constructor injection, but in this case it is partial application
    | FromParameter ->
      state,
      Cmd.ofTask.perform services.localStorage.getItem model.key SetContentInModel

    // For one of shots, you can simply invoke JS interop effect directly in the elmish loop
    | UsingBoleroHelpers =
      state,
      Cmd.OfJS.perform services.jsRuntime "localStorage.getItem" [| box model.key |] SetContentInModel

type MyApp() =
    // TheProgramComponent class implements the bits
    // that interop with blazor to make elmish work
    inherit ProgramComponent<Model, Message>()

    [<Inject>] // with get, set is important as the DI takes place on public properties
    member val LocalStorage: ILocalStorage = Unchecked.defaultOf<_> with get, set

    override this.Program =

        let update state message =
          let dependencies = { localStorage = this.LocalStorage; jsRuntime = this.JsRuntime }
          // partially apply the function dependencies
          MyApp.update dependencies

        // here we start our elmish loop and let it do it's thing
        Program.mkSimple (fun _ -> initModel) update view
```

For Fun.Blazor the situation is very similar

```fsharp
type MyComponent() =
  inherit FunBlazorComponent() =

  let key = "some-key"
  let state = cval {| value = None |}

  [<Inject>] // with get, set is important as the DI takes place on public properties
  member val LocalStorage: ILocalStorage = Unchecked.defaultOf<_> with get, set

  override this.Render() =
    article {
      button {
        onclick(fun _ task {
          let! value = this.LocalStorage.getItem(key)
          state.Publish(fun state -> {| state with value = value|})
        })
        "Invoke Function"
      }
    }
```

Both Bolero and Fun.Blazor allow you to interop with javascript seamlessly, it however depends on you how you'd like to structure your programs and follow patterns, one way is simpler but can lead to more complex code eventually while the other is very straight forward but can be cumbersome once it gets up to certain height.

Continuing with the topic at hand interop happens at the blazor layer, the parameters you pass in the `InvokeAsync` function in `IJSRuntime` must be JSON serializable, this serialization is not customizable as far as I know (there's better info at the MS docs in case I get that wrong). so you have to be careful what you're sending, for F# most types will work but discriminated unions will not, as that support has not been added yet to `System.Text.Json` (which powers all of interop layer)

In the previous interop example, we used global functions, and while nothing stops you from also adding your own namespace in the global window e.g.

```js
window.MyNamespace = {
  MySubSection: {
    doWork(a, b, c) {},
  },
};
// call it like window.MyNamespace.MySubSection.doWork(a, b, c);
```

It promotes global polution and also you leave lazy loading out of the window, a better approach is to work straight with Javascript modules so, how about importing your own authored JS files? let's think about a module like this.

```js
// /js/my-script.js
import { dependency } from "./lib/dependencies/dependency.js";

export async function doWork({ a, b, c }) {
  const value = await dependency(a, b);
  return c + value;
}
```

We can use our factory function again to create a service for that =

```fsharp
type IMyService =
  inherit IAsyncDisposable
  abstract doWork: string * string * int -> Task<int>

module MyService =
  let factory (services: IServiceProvider) =
    let js = services.GetService<IJSRuntime>()
    // use a lazy value here to call the import only untill we really need it
    let jsModule = lazy(js.InvokeAsync<IJSObjectReference>("import", "/js/my-script.js"))

    { new IMyService  with
      override _.doWork(a, b, c) = task {
          // will fetch the script on the first call
          // if this gets called again it will use the result from the already settled value task
          let! jsModule = jsModule.Value

          let! result = jsModule.InvokeAsync<int>(a, b, c)
          return result
      }
      override member _.DisposeAsync() =
        task {
          let! jsModule = jsModule.value
          return jsModule.Dispose()
        }
        |> ValueTask
    }
```

And just like that, you have lazy loading for services, and simple JS interop for both Fun.Blazor and Bolero you can then inject this service as shown above.

## Closing thoughts!

There's a bunch more to talk about (from each of the frameworks and blazor itself!) but I'd rather leave that for other post entries.
Having in mind that this section is purely my personal thoughts...

I'd say blazor (with either bolero/fun.blazor) has moved the needle favorable a little bit more.

When I wrote the original piece about the Frontend landscape I felt WASM was simply not worth it at the time except on very specific cases, today with the new Auto Interactive rendering mode blazor offers plus the advances in the F# counterparts I think we're getting into "Let's GOOOO" territory.

Assuming you can deploy aspnet servers freely, then if you were considering nuxt/next/remix or those kinds of metaframerowks, then Blazor might have become an option for you and depending on the talent pool you have around it might be worth it to have lesser context switches and enjoy the benefits of F# and full dotnet in the browser.

Assuming WASM only mode then... Things haven't changed that much but they have changed a bit enough. While I don't have numbers loading times and trimmed app size have improved quite a lot so WASM apps are closer to your standard "Enterprise Angular" application (if you've seen those you know what I mean), so loading times and bundle sizes might not be that relevant for you anymore. Except in cases where time to interaction means $$$, then stick to pre-render and server first approaches.

For places like intranet applications or enterprise'y large apps then I'd consider it even more today for sure you could write those in angular/react today but if you still end up working with 10 thousands of lines of code, I think Blazor can benefit better from F#, its language features. and dotnet ecosystem specially in that "Full Stack" scenario where your core library gets shared entirely, not just a subset that may or may not work in the browser but the real thing.

[Fable Compiler]: https://fable.io/
[Blazor]: https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor
[Bolero]: https://fsbolero.io/
[Fun.Blazor]: https://slaveoftime.github.io/Fun.Blazor.Docs/
[dotnet]: https://get.dot.net
[htmx]: https://htmx.org
[Dustin wrote a much better and comprehensive post about it]: https://dusted.codes/dotnet-blazor
[webassembly]: https://webassembly.org/
[open silver]: https://opensilver.net/
[reusable subscriptions]: https://elmish.github.io/elmish/docs/subscription.html#subscription-reusability
[Ryan Carniato]: https://twitter.com/RyanCarniato
