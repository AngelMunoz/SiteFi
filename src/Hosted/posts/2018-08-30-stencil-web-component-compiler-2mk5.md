---
title: Stencil... Web Component Compiler?
subtitle: ~
categories: stencil,webcomponents,jsx,javascript
abstract: I'll tell you about my experience playing around with stencil.js
date: 2018-08-30
language: en
---

[Polymer]: https://www.polymer-project.org
[Stencil]: https://stenciljs.com
[Ionic]: https://ionicframework.com
[React]: https://reactjs.org
[Aurelia]: https://aurelia.io

# Context

So this is quite a surprise for me, I was looking around web component solutions, because it has been promised for years that they are the future of the web. Right, in paper (Ba Dum... Polymer anyone? tsss...) are quite awesome, finally the *damned* `date-picker` I can use everywhere, no more frameworks, finally html, css, and javascript!

Well, I was kind of wrong... I played first with polymer, I even tried to make my own [bootstrap] (as a practice, experiment) based on polymer and web-components technology but that ended up Failing so bad... because one of the maximum pain points I've seen with web components is `style sharing` and rightfully so! because they were designed to be encapsulated so no one is messing around with the `date-picker` styles! (I will keep using an imaginary `date-picker`.

so designing web components is also designing the public API for them, which is not bad inherently, but we've been doing web development with shared styles for years that now that we have encapsulation it feels weird and unnatural sometimes!

yeah sure when I design a `class` in an OOP language, I expect to have `public`, `protected`, and `private` fields, properties or methods and everyone expects me to do so properly.

Now [Polymer] it is a great alternative for Web Components, but in the end you still end up doing [Polymer] rather than javascript, I understand that it is sugar that prevents you from working on the low level API for the Custom Elements, Shadow Dom Stuff, and such things. But somehow I also expect it to be less polymer'ish. One of the polymer points is going full web component, so you don't seem to have access to Custom Elements [which are not the same as a web component](https://softwareengineering.stackexchange.com/questions/289038/what-is-the-difference-between-web-components-and-custom-elements/289039) as is which is a downside given the support for Web Components at the moment, don't get me wrong, they have better support now days than even the last year but let me get to explain it.


Now that you have some context on my experimentation and experience with this let's get to it.

# Stencil
[Stencil] has a very intresting wording in their website:
>The magical, reusable web component compiler

`Web Component Compiler`... Hmm... my JavaScript senses are tingling, I don't see `Framework` anywhere in that sentence.
Stencil is built by the [Ionic] team, which uses a bunch of [React] inspired technology
- Virtual DOM
- Async rendering (inspired by React Fiber)
- Reactive data-binding
- TypeScript (I know this is not react)
- JSX

I myself haven't used react beyond a hello world, it just doesn't have an appeal to me, I'm sorry it's just not for me or is it?

Stencil focuses on producing `Custom Elements`, taken from their docs
 
>Stencil is a compiler that generates Web Components (more specifically, Custom Elements).

This is that shadow dom is not enabled by default, a custom element lacks shadow dom meaning its style is not encapsulted!, so you can have a custom bootstrap themed component, or bulma themed component if you have various projects with a shared css frameworks, Stencil might be quite appealing to you in that area.

So by now, we know that Stencil doesn't do Web Components by default, more like Custom Elements (remember what I said about polymer going full web component?)
how doest it look a Stencil Component?

```tsx
import { Component, Prop } from "@stencil/core";

@Component({
  tag: 'tun-login-form',
  styleUrl: 'tun-login-form.css'
})
export class TunLoginForm {
  @State() email: string;
  @State() password: string;

  render() {
    (
      <form onSubmit={this.onSubmit.bind(this)}>
        <label htmlFor="email" >Email</label>
        <input type="email" value={this.email} id="email" name="email" 
         onInput={this.onInputChange.bind(this)} required />
        <label htmlFor="password">Password</label>
        <input type="password" value={this.password} id="password" name="password"
         onInput={this.onInputChange.bind(this)} required />
        <button type="submit">Login</button>
        <button type="reset">Clear</button>
      </form>
    );
  }

  onInputChange({target}) { /* ...stuff...  */ }
  async onSubmit(event: Event) { /* ...stuff...  */ }
}
```

so when you look at this, you might say: `well you are not doing polymer'ish, but surely you are doing react'ish/stencil'ish` yeah kind of. Up to certain point yes, because when you start managing values is when you start feeling that native usage, for example just look at how we are performing the `binding`

```tsx
<input value={this.password} onInput={this.onInputChange.bind(this)} />
```
our usual two way data binding is managed in two separated steps and marked as `@State` at the beggining

I might be very naive at this, but this is one way I would handle it
```tsx
onInputChange({ target }) {
  switch (target.getAttribute('name')) {
    case 'email':
      this.email = target.value;
      break;
    case 'password':
      this.password = target.value;
      break;
    default:
      console.warn('Name not Found')
      break;
  }

  if (target.validity.typeMismatch) {
    target.classList.add('is-danger');
    target.classList.remove('is-success');
  } else {
    target.classList.remove('is-danger');
    target.classList.add('is-success');
  }
}
```
that's when you start feeling that you are actually using JavaScript rather than using the non existent `Framework` when was the last time you used classList?
or used the HTML5 validation API? I know that API is not the best around, but is just as native as it gets! and all of this without external third party libraries, if you are skilled enough in css, you can just go full HTML, TSX, CSS using the fetch API, using HTML5 Validation, this is just something that you don't do everyday in your fancy `[insert framework]` which is fine, because those frameworks offer different solutions to different problems, the point here is that this should be able to use wherever you need/want regardless of the frameworks you use, because these are compiled into native `Custom Elements`!

also take a look at the `submit` function

```tsx
async onSubmit(event: Event) {
  event.preventDefault();
  let res;
  try {
    res = await fetch('https://myserver.com/auth/login', {
      method: "POST",
      // don't forget cors stuff when using fetch
      mode: "cors",
      headers: {
        "Content-Type": "application/json; charset=utf-8",
      },
      body: JSON.stringify({
        email: this.email,
        password: this.password
      })
    })
       .then(response => response.json());
  } catch (error) {
    return console.warn({ error });
  }
  console.log('Woohoo Login!', { res })
}
```

so `event` is an submit event, that you have to prevent or the browser will reload!
where's your `jQuery` now huh? so you don't see `element.addEventListener(/* ... */)` anywhere but you can see the resemblance to native javascript code, `frameworkless` in the end what you get is a compiled Custom element hat you can just plug in wherever you want as long as your browser supports it!

and just as simple as that you can start building a website as a `PWA`, you can start building Custom Elements for public consumers, or even Web Components because it is as easy as adding `shadow: true`
```ts
@Component({
  tag: 'tun-login-form',
  styleUrl: 'tun-login-form.css',
  shadow: true // <= this one
})
```

This gets to the pain points I talked about styling, these are not Polymer or Stencil's fault, it's just how Shadow DOM Works, but stencil does an amazing job focusing at `Custom Elements` and not just Full `Web Components` which allow the usual shared styling we're used to.

At this point, I feel like Stencil, does keep me closer to the native browser methods (up to a certain point) and they clain some good stuff:

>Simple
With intentionally small tooling, a tiny API, zero configuration, and TypeScript support, you're set.

>Performant
6kb min+gzip runtime, server side rendering, and the raw power of native Web Components.

a `tiny API` and a `small runtime`, I have a private project that I'm working on with and I can't share details but I will make something public soon to share how it feels.

You can choose to also create the bundle in a `dist` directory and that will be created for consumption from `npm` services, you can find more [information here](https://stenciljs.com/docs/distribution)


### Stuff that I don't like

1. TSX

Don't get me wrong, TSX is cool, but I hope they went more like the [Aurelia] way, just a plain js/ts file with a plain class with it's corresponding html file and that's it, no decorator stuff until you need advanced stuff, But I do understand the reasons on why using TSX, it just fits the project


2. Recommended File Structure
```
├── card
│   ├── card.scss
│   ├── card.tsx
├── card-content
│   ├── card-content.scss
│   └── card-content.tsx
├── card-title
│   ├── card-title.scss
```

while I know everything it's a component in the end, when you chose the web project they also use this structure which to me doesn't fit too much because I get lost on which are strictly components and which are page like components (but that's just me) in the style guide they are pretty clear about it too

>This is a component style guide created and enforced internally by the core team of Stencil, for the purpose of standardizing Ionic Core components. This should only be used as a reference for other teams in creating their own style guides. Feel free to modify to your team's own preference.


### Wrap Up

So Far I like pretty much of it, because there's not much to like/dislike they have a small API on purpose and that API adheres to the `Web Component` standards, nothing more, nothing less and it seems to work pretty well.

I will post some other findings in subsequent entries sharing how to do `parent <- child` communication (spoilers, more decorators and js native CustomEvents stuff)


Share your thoughts on the comments below! and thank you for having read this mess of a post.

Don't forget to check the second part!

{% link https://dev.to/tunaxor/stencil-the-web-components-compiler-part-2-313b %}
