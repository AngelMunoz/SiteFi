---
title: Stencil The web components compiler... Part 2!
subtitle: ~
categories: stencil,javascript,tsx,webcomponents
abstract: I'll share with you what I have found in Stencil part 2!
date: 2018-08-31
language: en
---

[Stencil]: https://stenciljs.com/
[Aurelia]: https://aurelia.io/
[Vue]: https://vuejs.org/
[src/components]: https://github.com/AngelMunoz/tun-stencil-sample/tree/master/src/components
[forms]: https://github.com/AngelMunoz/tun-stencil-sample/blob/master/src/pages/forms/forms.tsx
[tun-data-form]: https://github.com/AngelMunoz/tun-stencil-sample/blob/master/src/components/tun-data-form/tun-data-form.tsx

you can find source code for this post here:

{% github AngelMunoz/tun-stencil-sample %}

and 

[Stackblitz Antularjs Template With Stencil Components](https://stackblitz.com/edit/tun-stencil-sample-usage)

also the website is published [in this place](https://asisma-7f89d.firebaseapp.com/forms)

In the last Post I shared with you that stencil is a `Web Components Compiler` focused on `Custom Elements` which uses `TSX` and other `reactjs` inspired technology

Yesterday I decided to make some public stuff so you could see what I was talking about and I kind of took it a little further deploying a website to `firebase` and also publishing the same website on `npm` and then use the components on the website to share and use in other website/project.

Let me tell you that I was Amazed with the results, but let's get started with forms first, because that's what I promised on the last post



## Forms and Events
In [src/components] you will find three components
1. tun-data-form
2. tun-profile-form
3. tun-navbar

From those 3, `tun-navbar` is badly designed for sharing, because it has implicit and explicit data from the web application itself (like routes exclusively for the website itself) it's like that on semi purpose (I didn't think it was going to be easy to share at all) but it's a gotcha you can already see when working with shareable website components in stencil, you could replace those routes with slots or even properties in a way that the component isn't depending on your website at all, but allow it to be extensible.

The other two components are mere forms without a specific purpose, they exist just to show how to do stuff in stencil rather than make a website work.

In `Frameworks` like `Vue` or `Aurelia` I like to work with `top -> down` communication, and then producing events in children elements with listeners In their parents that way I can use the same component in different context as long as that context has the same properties and similar meaning.


In the case of `tun-data-form` we use it like this in the [forms] page
```tsx
<section>
  <h1>Data Form</h1>
  <tun-data-form edit={this.editData}></tun-data-form>
</section>
```

we're passing down a Boolean value to know if we can edit data, some websites, display information almost ready to edit but we need a click on a switch/button else where to allow us edit information, we're just following that in here.

In [tun-data-form] we can see quite a lot of code but let's go step by step

```tsx
import { Component, Prop, Event, EventEmitter, State } from '@stencil/core';

@Component({
  tag: 'tun-data-form',
  styleUrl: 'tun-data-form.scss'
})
export class TunDataForm {
  @Prop() edit: boolean = false;

  @Event() submitDataForm: EventEmitter;
  @Event() resetDataForm: EventEmitter;

  @State() email: string;
  @State() phoneNumber: string;
  @State() password: string;
```

on the first line, we import what we will be using on our component, the following code specifies where to find our custom styles and which tag will be using for this component.

On the next line we have our class declaration and start looking at some code 
we have the following decorators
1. Prop
2. Event
3. State

`Prop` is a decorator that lets us specify that the marked `class` property will be coming from the outside of the component
```tsx
  <tun-data-form edit={this.editData}></tun-data-form>
```
in this case, it is that `edit` property that we used before on `forms.tsx`, the difference from `Prop` and `State` is that props are by default `one way` binded and can't be modified by the component itself.

`Event` is a decorator that will allow us to send events to the exterior of the component in a way that can eventually be captured as in a usual form `element.addEventListener('submitDataForm',() => {}, false)`

`State` is a decorator that tells our component that `class` properties marked with this, will be used internally in the component and that they don't need to be exposed.


Then we have our render function
```tsx
render() {
    return (
      <form onSubmit={this.onSubmit.bind(this)} onReset={this.onReset.bind(this)}>
        <article class='columns is-multiline'>
          <section class='column is-half'>
            <section class='field'>
              <label class='label'>Email</label>
              <p class='control'>
                <input type='email' class='input' name='email'
                  onInput={this.onInput.bind(this)} readOnly={!this.edit} required />
              </p>
            </section>
          </section>
          <section class='column is-half'>
            <section class='field'>
              <label class='label'>Password</label>
              <p class='control'>
                <input type='password' class='input' name='password'
                  onInput={this.onInput.bind(this)} readOnly={!this.edit} required />
              </p>
            </section>
          </section>
          <section class='column is-two-thirds'>
            <section class='field'>
              <label class='label'>Phone Number</label>
              <p class='control'>
                <input type='tel' class='input' name='phoneNumber'
                  onInput={this.onInput.bind(this)}
                  readOnly={!this.edit} pattern='[+0-9]{3}[- ][0-9]{3}[- ][0-9]{3}[- ][0-9]{2}[- ][0-9]{2}' required />
              </p>
            </section>
          </section>
        </article>
        {this.edit ? <button class='button is-info is-outlined' type='submit'>Change</button> : <span></span>}
        {this.edit ? <button class='button is-primary is-outlined' type='reset'>Cancel</button> : <span></span>}
      </form>
    );
  }
```

which as you guess is your typical markup code, the only code that might be relevant for the purpose of this post is these lines
```tsx
onSubmit={this.onSubmit.bind(this)} onReset={this.onReset.bind(this)}
onInput={this.onInput.bind(this)} readOnly={!this.edit}
```

We're dealing with events here and setting properties on events, we bind some functions that are part of the class ahead in the code

this relates similarly to `onclick="myfn()"`
and the last relevant code:
```tsx
onSubmit(event: Event) {
  event.preventDefault();
  this.submitDataForm.emit({
    email: this.email,
    phoneNumber: this.phoneNumber,
    password: this.password
  });
}

onReset() {
  this.resetDataForm.emit();
}
```
(for the usage of the `onInput` function please check the last post)

In this part we lastly use `this.submitDataForm` and `this.resetDataForm` which are the `class` properties we marked as `@Event` earlier, these are just sintactic sugar for the following 
```ts
const event = new CustomEvent('submitDataForm', { 
  detail: {
    email: this.email,
    phoneNumber: this.phoneNumber,
    password: this.password
  }
})
document.querySelector('tun-data-form').dispatchEvent(event);
```

in the end We're still #UsingThePlatform just take in mind that everything on the methods, functions etc is tied to your logic and such, but the least a component depends on something, the more portable it is


now I should be able to use this form component wherever I want, if I find fit I can also pass a property which may contain everything I need to fill those fields before using it that's just up to usage

now If we go to the [forms] page, there will be a method with another decorator we haven't seen yet `@Listen()`

```tsx
@Listen('submitDataForm')
onSubmitDataForm({ detail: { email, password, phoneNumber }, }: CustomEvent) {
  console.log(email, password, phoneNumber);
}

@Listen('resetDataForm')
onResetDataForm() {
  this.editData = false;
}
```

`Listen` is a decorator that is sugar over 
```ts
document.querySelector('tun-data-form')
  .addEventListener('submitDataForm', function onSubmitDataForm({}) {});

```
it may look like Stencil is declaring stuff somewhere and adding itself to window in some way but no, this is totally just javascript under the hood, just browser API's and nothing more, we're not using any kind of `framework` or `framework` specific methods, functions; It's just the browser environment with it's API's

The code here is fairly simple, it's just Listening to the `submitDataForm` custom event that we fired (`.emit()`) in the [tun-data-form] component as you can see, the properties we sent in our emit, now are available on our `detail` property of our Custom Event having these emited, we can now start doing ajax stuff, either sending it to our API, processing it somewhere, storing it on Local Storage, whatever you want/need to do with that information

### Bonus

So far we have a form that doesn't depend on custom business logic, it's job is just about collecting data, and emitting that data for a parent component to manage the business logic for it. What if we decide that we have other application that should use the same component? but meh, it's on angularjs I bet it won't work.


**Wrong!** head to [this place](https://asisma-7f89d.firebaseapp.com/forms/) to see how the form is performing and how it seems to work, pleas open the console and see that we're logging what we get from our custom events we fired.

I have published the same repository in [NPM](https://www.npmjs.com/package/tun-stencil-sample) with the help of these [Docs](https://stenciljs.com/docs/distribution)
and also with the help of unpkg, and created this [stackblitz](https://stackblitz.com/edit/tun-stencil-sample-usage) where I wanted to use the forms I created for my website
(you can try that too `https://unpkg.com/tun-stencil-sample@0.0.1/dist/tun-stencil-sample.js`)

#### Now Pay attention because, this blew my mind once I realized what was going on here

in the index.html we have the following code
```html
<div id="app">
  <div ui-view></div>
  <hr>
  <h1>Don't forget to check the console</h1>
  <tun-profile-form edit></tun-profile-form>
  <hr>
  <tun-data-form edit></tun-data-form>
</div>
```
those are the same forms we created in our previous website! **NO MODIFICATIONS** :super_ultra_crazy_mega_parrot_ever:
you will need to add/remove manually the `edit` property for the moment, but on the right side you can see how it is working equally to the website you visited before!

yeah but event handling must be hard right?
**Wrong!** head to `app.js` and you will see at the end the following lines

```js
document.querySelector('tun-data-form')
  .addEventListener('submitDataForm', event => console.log(event.detail), false);

document.querySelector('tun-profile-form')
  .addEventListener('submitTunProfile', event => console.log(event.detail), false);
```
*whaaat?* I mean just that? that means that if I'm using [Aurelia] I would be doing `<tun-data-form submit-tun-profile.bind="myFn($event)"><tun-data-form>`
If I'm using [Vue] it would be `<tun-data-form @submit-tun-profile="myFn"><tun-data-form>` and that is Just Awesome! I haven't personally tried it but hey, did you check that the template is actually using `Angular Js`? and let's be fair angularjs isn't the most *outsider* friendly framework out there and I have tested some compiled `polymer web components` previously in Vue and [they worked just fine](https://stackblitz.com/edit/polymer-vue-playground) so I'm completely sure [Stencil] will work too.


My head was blown off yesterday when I was finishing doing this, it only took a couple of hours!  not days, not weeks, not months, just a couple of hours for the `Maximum Portability` I've ever seen.

My heart has been taken by Stencil and I can not express how much interested and amazed I'm at the work of the Ionic Team that made all this work possible in a way that is not only intuitive but without that extra bunch, frameworks often put in.


Lastly I wanted to share a video from last year when they first presented Stencil at the last year's *Polymer Summit 2017*

{% youtube UfD-k7aHkQE %}


Thank you for reading this mess of a post, and please share your thoughts on the comments below! also any feedback on the code I have share with you is pretty much appreciated, I'm not a heavy user of tsx/jsx so there might be some patterns that are not great at all.
