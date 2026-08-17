---
title: PWA's and Windows Apps
subtitle: ~
categories: uwp,offline,pwa,aurelia
abstract: If you don't want to target UWP with WinJS, you still have an option
date: 2018-07-20
language: en
---

# Hello Everyone!
So, I've published before on why targeting UWP instead of *electron* is a good choice if the only thing you want/need is windows, and you need native access API's that come with the *winrt* runtime, like media players, or POS API access.

That included creating your app entirely on Visual Studio with JavaScript, perhaps a bundler like *fuse-box* or webpack, parcel you name it, the point was create something that spit out an html/css/js bundle so your UWP app could run it.

Well today I bring a different approach, you may have heard about [PWA](https://developers.google.com/web/progressive-web-apps/)s Google has invested a ton of resources into PWA development and spread knowledge and also Microsoft [published](https://blogs.windows.com/msedgedev/2018/02/06/welcoming-progressive-web-apps-edge-windows-10/) in February that Windows 10 and MS Edge would support PWAs, so if you need PWA information you can click on those links, because that information is a little out of the scope of this post.

**Dev.to** is itself a PWA, if you have ever visited the website in your android phone (not sure about iOS) chrome asks you if you want to add the website in your phone, if you do accept, then Dev.to becomes like another app in your phone.

I have a question for you

>Now, what if you already have a website, but you see an opportunity to include features that seem to be windows only?

you need to follow 2 small steps
1. Update your site to become a PWA (add a manifest, a service worker, and other small requirements remember to click that pwa link for more info)
2. Create an Appx for a Windows Store Submission.

and when your app gets accepted, Boom! you already have a PWA and a Windows 10 App, accessible to every platform that run Windows 10, and also that includes having access to any **WinRT** API!

```js
if (window.Windows) {
  // yay we're on windows!
}
```

so you could have a media player website that when is launched from windows as an App, it can run on native API's instead of your HTML5 media API's.

I don't have a demo for native API access right now, but I can hare you what i have built for my home usage (and practice as well)

## Mandadin
[Mandadin](https://mandadin.tunaxor.me) is a PWA that uses Aurelia, Pouchdb, Workbox (for service workers) and your standard Web stuff, is hosted in firebase and is isolated to the local device, so anything you do on your phone stays on your phone, because it uses IndexedDB to store whatever you want to store in there
you can find the code here

{% github AngelMunoz/shoppinator %}

and you will find that the code might be a little messy but it serves the purpose of taking our shopping list notes. Why did I reinvent the wheel instead of using X? because it serves me as practice.

you can find the [Windows store App here](https://www.microsoft.com/store/apps/9NKDDF1M5MTG) and if you install it you will see a couple of things

1. It's in Spanish.- yeah, It's not meant to provide a general audience solution, it was my wife's request and some sort of a resume like app.
2. it's exacly the same app that is loaded when you visit [Mandadin](https://mandadin.tunaxor.me)

the thing on the point 2 is that you are running inside a Windows App shell! so **You Do Have Access to WinRT API's!**

what can you do with that? there's more info about that here 
{% link https://dev.to/tunaxor/javascript-windows-universal-applications-part-2-15nl %}


so yeah! you can target UWP with your existing code base today! and with a little of organization and effort you can have native capabilities in a Windows store application that it's just basically a website!

EDIT:
Did I forget to mention that you don't need to update the Appx Package? it will always pick what you have in your website and register the new service worker! so Booom! continuous Deployment already in progress!


So, does this make you think in some sort of side project of your own that can be published in the windows store App?


So thank you for reading this piece :) I hope you have a wonderful weekend and please share your thoughts about this on the comments section.
