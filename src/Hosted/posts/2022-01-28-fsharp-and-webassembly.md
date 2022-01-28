---
title: F# and WebAssembly
subtitle: ~
categories: fsharp,dotnet,webassembly,webdev
abstract: Do you want to use F# for web development rather than javascript? this one is for you!
date: 2022-01-28
language: en
---

[bolero is working on a variation like this]: https://github.com/fsbolero/Bolero/issues/249
[highlight html/sql templates in f#]: https://marketplace.visualstudio.com/items?itemName=alfonsogarciacaro.vscode-template-fsharp-highlight
[html for f#]: https://marketplace.visualstudio.com/items?itemName=daniel-hardt.html-for-fsharp-lit-template
[example]: https://github.com/AngelMunoz/Mandadin/blob/fun-blazor-migration/Mandadin/Navbar.fs#L69-L107
[fable]: https://fable.io
[bolero]: https://fsbolero.io
[blazor]: https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor
[angular]: https://angular.io
[vue]: https://vuejs.org/
[react]: https://reactjs.org/
[svelte]: https://svelte.dev/
[aurelia]: https://aurelia.io/
[fun.blazor]: https://slaveoftime.github.io/Fun.Blazor/
[type provider]: https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/type-providers/
[elmish]: https://elmish.github.io/elmish/
[feliz]: https://github.com/Zaid-Ajaj/Feliz
[avalonia.funcui]: https://github.com/fsprojects/Avalonia.FuncUI
[fsharp.control.reactive]: http://fsprojects.github.io/FSharp.Control.Reactive/index.html
[fsharp.data.adaptive]: https://github.com/fsprojects/FSharp.Data.Adaptive

## F# and WebAssembly

When I talk about F# and Web development I tend to speak about [Fable] which is an `F# -> JS` compiler (although, in Fable 4+ it will officially target more than just JS), in a sense you're basically replacing Typescript or Flow or any other JS compiler for F#.

Today we will talk about [Bolero] and [Fun.Blazor] which are some F# abstractions over [Blazor], Microsoft's Frontend Framework, objectively speaking blazor is a direct competitor to [Angular], [Vue], [React], [Svelte], [Aurelia] and similar alternatives.

I know there is people out there waiting both patiently and desperately for the death of javascript while I don't think that day will ever come, I understand how being able a single language to fill many if not all parts of the stack is useful (we already do that with JS and thousands of people love it), so let's take a look!

## Bolero

Getting started with Bolero is pretty simple

```
dotnet new -i Bolero.Templates
dotnet new bolero-app -o MyApp
cd MyApp && dotnet run -p src/MyApp.Server
```

> Most of the features I will talk about here are well described in bolero's website so don't forget to check them out there as well

### Hosting models

Bolero (just as blazor) can be used for both server side and client side apps, the templates by default provide both backend and frontend projects, this is because there's a feature that allows you to have Hot Reload for HTML templates but it only works when you run the frontend app with the server project.

### Markup and DSLs

Bolero two main ways to write HTML, one is using HTML templates and the other is to use an HTML DSL, both things ultimately will produce

#### HTML Templates

Bolero uses a custom HTML [Type Provider] that allows you to write HTML templates in a type safe manner and provides some Hot Reload capabilities

```fsharp
type Hello = Template<"""<div id="hello">Hello, world!</div>""">
// or using a filepath
type Hello = Template<"templates/hello.html">
```

this gives you a type which has information about your HTML template, templates can have _holes_ in them, which can be used to insert F# values.

Let's make a simple counter

```html
<section class="${Classes}">
  <p>Count: <span>${Count}</span></p>
  <button onclick="${Increment}">Increment</button>
  <button onclick="${Decrement}">Decrement</button>
  <button onclick="${Reset}">Reset</button>
</section>
```

with that HTML as our template we can handle these holes inside our F# code

```fsharp
type Counter = Template<"templates/counter.html">

let getCounter initial =
    let mutable count = initial
    let getCounterCls count = if count > 10 "warning" else "normal"
    // let's fill each of the wholes we made on the template
    Counter()
        .Classes($"counter {getCounterCls count}")
        .Count(count)
        .Increment(fun _ -> count <- count + 1)
        .Decrement(fun _ -> count <- count - 1)
        .Reset(fun _ -> count <- initial)
        // once we're done call Elt() to get the instance of the template
        .Elt()

// use this somewhere else
let startsAt100 = getCounter 100
let startsAt0 = getCounter 0
```

the `.Elt()` at the end is to get the instance of the template.

You can also get nested templates, bind inputs, and radios for example by the way don't be scared by the `mutable` keyword right there is just to show a brief example in a normal situation you would likely be using [Elmish]

If plain HTML is not your cup of tea, let's move on to an F# DSL then

#### HTML DSL

The HTML DSL is a fairly standard one it is composed from a function that takes two lists as arguments, the first is for attributes and the second one is for children elements our counter example would look like this

```fsharp
let getCounter initial =
    let mutable count = initial
    let getCounterCls count = if count > 10 "warning" else "normal"

    section [ attr.``class`` $"counter {getCounterCls count}" ] [
        p [] [ text "Count: "; span [] [ text $"%i{count}" ] ]
        button [ on.click(fun _ -> count <- count + 1) ] [ text "Increment" ]
        button [ on.click(fun _ -> count <- count - 1) ] [ text "Decrement" ]
        button [ on.click(fun _ -> count <- initial) ] [ text "Reset" ]
    ]

// use this somewhere else
let startsAt100 = getCounter 100
let startsAt0 = getCounter 0
```

The main advantages of this approach is that you get the full power of the F# language. You can call functions, use ifs, pattern matches, get out of the box type safety and whatever you can do with F#.

The main disadvantages are that since this is F# code it must get compiled any time you make changes to it so... You will have to re-start your server any time you make changes to it this can be slow and discouraging for some people which is completely understandable.

### Elmish

One of the popular architectures for F# enthusiasts is the ability to use the elmish architecture (also known as MVU) bolero offers elmish in the form of Components this is super convenient given how unscalable can elmish be on more complex aplications, rather than have a main elmish loop, you can have each of your website parts as a different elmish loop allowing you to de-couple some logic and even pages when not needed.

let's revisit our counter example

```fsharp
type State = { count: int }
type Msg =
    | Increment
    | Decrement
    | Reset

let init initial = { count = initial }

let update initial msg state =
    match msg with
    | Increment -> { state with count = state.count + 1}
    | decrement -> { state with count = state.count - 1}
    | Increment -> { state with count = initial }
```

This is the core part of our elmish model we will see how it works with both HTML and the DSL approaches

```fsharp
// the HTML template stays the same
let view state dispatch =
    let getCounterCls count = if count > 10 "warning" else "normal"
    // let's fill each of the wholes we made on the template
    Counter()
        .Classes($"counter {getCounterCls state.count}")
        .Count(count)
        .Increment(fun _ -> dispatch Increment)
        .Decrement(fun _ -> dispatch Decrement)
        .Reset(fun _ -> dispatch Reset)
        // once we're done call Elt() to get the instance of the template
        .Elt()
```

```fsharp
let view state dispatch =
    let getCounterCls count = if count > 10 "warning" else "normal"

    section [ attr.``class`` $"counter {getCounterCls count}" ] [
        p [] [ text "Count: "; span [] [ text $"%i{count}" ] ]
        button [ on.click(fun _ -> dispatch Increment) ] [ text "Increment" ]
        button [ on.click(fun _ -> dispatch Decrement) ] [ text "Decrement" ]
        button [ on.click(fun _ -> dispatch Reset) ] [ text "Reset" ]
    ]
```

Now, let's tie in all of that in our component

```fsharp
type MyCounter() =
    inherit ProgramComponent<State, Msg>()

    [<Parameter>]
    // Grab the value from outside of the component
    member val InitialValue: int option = None with get, set

    override this.Program =
        // ensure there's a default value for our elmish loop
        let initial = this.InitialValue |> Option.defaultValue 0

        // with partial application give it the initial value
        let update = update initial

        // Run the elmish program
        Program.mkSimple (fun _ -> init initial) update view
```

As you can see regardless if we chose HTML or the DSL the elmish component works the same. there are also other types of elmish components that you could use to fine-tune your component's performance these are called `View Components` but I'll leave those to you as homework 😜

To use this component we can call it in the following way

```fsharp
let startsAt0 = comp<MyCounter> [ "InitialValue" => 100 ] []
let startsAt100 = comp<MyCounter> [] []
```

We could use these in other `view` function somehwere or if I'm not mistaken even in a C#'s blazor file but don't quote me on that because I haven't tried it.

All in all this is more-less the gist of Bolero there are other features like routing, remoting, dependency injection interop, JS services that can be used but I think many of those require their own articles, if you're interested in those let me know!

## Fun.Blazor

Fun Blazor is a library built on top of bolero it can interop with bolero nicely but it offers a few more options in the DSL and state management department.

> Note that **Fun.Blazor** is still a young library but I find it promising and more ergonomic to use to the point I actually could favor it over other solutions.

To get started with Fun.Blazor you can either use this extremely minimal (PWA ready) template I made for this post

https://github.com/AngelMunoz/Fun.Blazor.Sample

or use the official templates

```
dotnet new --install Fun.Blazor.Templates::*
dotnet new fun-blazor-min-wasm -o MyApp
cd MyApp && dotnet run
```

> I will continue here with the simplified template I made which differs a little bit from the official but the core concepts remain the same.

Fun.Blazor offers three alternative ways to write your HTML code, the first is by using a set of Computation Expressions (CE), the second is by using a [Feliz] style of DSL and the third one is to write HTML Templates as well

Let's revise our counter example again

```fsharp
let counter initial =
  adaptiview () {
    let! counter, setCounter = cval(initial).WithSetter()
    p () { childContent $"Count: {counter}" }

    button () {
      onclick (fun _ -> setCounter (counter + 1))
      childContent "Increment"
    }

    button () {
      onclick (fun _ -> setCounter (counter - 1))
      childContent "Decrement"
    }

    button () {
      onclick (fun _ -> setCounter (initial))
      childContent "Reset"
    }
  }
```

If you're familiar with JSX/Swift/Kotlin this might be the best way for you to work with your views it's also the most performant variation of Fun.Blazor's way to write HTML content, Fun.Blazor offers custom operations that help you model views in a slightly less verbose manner than the plain DSL offered by bolero, it's worth noting that [bolero is working on a variation like this] but it might take a while to land.

```fsharp
let counter initial =
  adaptiview () {
    let! counter, setCounter = cval(initial).WithSetter()
    html.p [
        attr.childContent$"Count: {counter}"
    ]

    html.button [
        evts.click (fun _ -> setCounter (counter + 1))
        attr.childContent $"Increment"
    ]

    html.button [
        evts.click (fun _ -> setCounter (counter - 1))
        attr.childContent $"Decrement"
    ]

    html.button [
        evts.click (fun _ -> setCounter (initial))
        attr.childContent $"Reset"
    ]
  }
```

if you've ever used [Feliz] or [Avalonia.FuncUI] then this DSL will make you feel at home, it's less verbose than the original DSL and gives you basically the same benefits, in the case of Fun.Blazor is slightly less performant but it is a viable alternative

```fsharp
let counter initial =
  adaptiview () {
    let! counter, setCounter = cval(initial).WithSetter()
    Template.html
      $"""
      <section>
        <p>Count: <span>{counter}</span></p>
        <button onclick="{fun _ -> setCounter (counter + 1)}">Increment</button>
        <button onclick="{fun _ -> setCounter (counter - 1)}">Decrement</button>
        <button onclick="{fun _ -> setCounter (initial)}">Reset</button>
      </section>
      """
  }
```

You might look at this and think:

> Why on earth would I use this version? no syntax coloring, no nothing ewww!

- If you're using VSCode you can use the [Highlight HTML/SQL templates in F#] extension and you will automatically get HTML intellisense, syntax coloring as well as F# support!
- If you're using Visual Studio you can use [Html for F#], it won't give you HTML intellisense but it at least offers the HTML syntax coloring which is greatly appreciated

Besides that, one strong point for that HTML templates is the ability to use Web Components/Custom Elements which are on the rise on usage, sadly these are able to do Hot Reload like bolero's ones but they have their own benefits

### State Management

While Fun.Blazor also allows you to use Elmish, it also offers a different way to handle view's states (which hopefully you spotted already) since Web Assembly uses F# code, it means it has access to a vast amount of libraries in the wild and take advantage of those as well, Fable is somewhat limited by javascript itself so any improvements you can get from the F# language is also limited to what Javascript can understand.

#### Adaptiviews

This is not the case though! Fun.Blazor takes advantage of the [FSharp.Data.Adaptive] library which works more-less like an excel spreadsheet's cells, each value is fixed unless it's being computed by another this allows performant updates on-demand, and with the help of `adaptiview() {}` it can leverage those same efficient updates to allow performant views in F#, in essence `adaptiviews` are hook-like abstractions.

```fsharp
adaptiview() {
    let! value, setValue = cval("my initial value").WithSetter()
    // do whatever else you want with the value and set value
}
```

#### Stores

If you like Sutil/Svelte like state management using stores then using Fun.Blazor components will be a great option for you

```fsharp
let counterComponent (initial: int) =
  html.inject (fun (hook: IComponentHook) ->
    let counter = hook.UseStore initial
    // this value will automatically update any time counter changes
    let adaptiveValue = hook.UseAVal counter

    adaptiview () {
      // we can use the value directly if we bind it on the
      // adaptiview CE
      let! computed = adaptiveValue
      // use the computed value
      p () { childContent $"Count: %i{computed}" }

      button () {
        // publish updates to the store
        onclick (fun _ -> counter.Publish(computed + 1))
        childContent "Increment"
      }

      button () {
        onclick (fun _ -> counter.Publish(computed - 1))
        childContent "Decrement"
      }

      button () {
        onclick (fun _ -> counter.Publish(initial))
        childContent "Reset"
      }
    })
```

This can be a powerful approach because you can have functions that take a store and push updates to it, the adaptive value will always be up to date and you can even produce multiple adaptive values from the same store! meaning that sub components or other views can also enjoy the benefits of sharing stores and generate their own adaptive values.

### Components

We just saw a component with stores and adaptive values, but the benefits of Fun.Blazor components don't stop there, they let you access blazor's components lifecycle hooks seamlessly!

for [example] you can tap on the after first render and do async operations that can be integrated with observables thanks to [FSharp.Control.Reactive]

```fsharp
html.inject(fun (hook: IHookComponent, themeService: IThemeService) ->
    let theme = hook.UseStore Theme.Dark

    // Event
    hook.OnFirstAfterRender
    // async task
    |> Observable.map themeService.GetTheme
    |> Observable.switchTask
    // update the store
    |> Observable.subscribe theme.Publish
    // ensure disposal from the component's subscription
    |> hook.AddDispose

    nav() {
        // the rest of the view code
    }
)
```

The same can be done with other blazor lifecycle hooks.

Fun.Blazor offers ways to handle state outside elmish which I believe they are very powerful and can help you avoid some annoyances you can get when you use existing frameworks/approaches

## Closing thoughts

So there it is! I gave some love to WebAssembly in F# as well I'm still on the fence of going full web assembly but it is certainly an alternative you can consider thanks to the fact that web assembly is part of the browser standards and not some propietary plugin thing (sorry Silverlight)

Bolero wasn't really appealing to me given the options for state handling (elmish only basically) and the DSL options but with Fun.Blazor and the state management options it provides I really feel like it's worth trying out, it is still a young library so make sure you try it, report bugs, give feedback and help it grow!
