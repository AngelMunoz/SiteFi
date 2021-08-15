---
title: UWP, Electron, and Desktop development with javascript; some thoughts.
subtitle: ~
categories: javascript,electron,uwp
abstract: I share you some of my origins as a developer as well as puting options out there
date: 2018-06-20
language: en
---

# Hi everyone
so, I have gone back to review what made me want to learn programming stuff, I'va have learned more in these last 3 years that whatever I could have ever learn in a while, Some day I just woke up saw a windows 8 app development announcement somewhere and it got my attention, at the time I was studying to be a nutritionist, so I was not even aware of what would be ahead of me.


I was lazy and wanted computers do most of my stuff for me, so I had some spreadsheet calculations that I felt they were too boring to fill, so I tried an even more automated solution for that I realized that the only way I would get that was going to be if i started learning on how to develop such things. 
I left it there, and continued with my life, few months later I dropped school and wander around thinking what I wanted for my life, I went back to the university but this time on the last minute I changed my decision for an IT career.


At the time, windows 8 came and people were looking at it as complete garbage, I didn't feel it that way, on one of the final projects I delivered that "automated solution" I wanted for you can see the repository here

`https://github.com/AngelMunoz/NutricalcLegacy`

**just a warning you will see a lot of jQuery and a bunch of repeated code that may make you throw up. On my defense hahaha is that I was just learning.**

so for work reasons and school reasons also I distanced myself from Windows Apps 
I wasn't in for C# I liked how it was cool on the javascript side, but at the time I needed to focus more on Java But you can see why javascript windows apps are something that I feel too close


Few years later here we are writing this piece.

I dig back into creating javascript windows apps, you can check related posts on my profile, or on my twitter profile.


# Now
So it seems that the javascript world doesn't want to be behind and loks for motives to keep improving and having a broader and richer engagement, 
with phone gap for example then came something that changed the game for many

## Electron
Electron came to the scene formerly named "Atom Shell" used to precisely create the Atom Editor at github's when it came out it sparkled something inside me, writing desktop apps in javascript!? where did I heard that before? it was a cool concept and if you were really into javascript this was a cool way to go for it

Electron now days is a pretty much flexible place, you can do amazing suff with it, from USB Boot makers like [Etcher](https://etcher.io) or the famous (or infamous) Slack app, Spotify while it is not made with electron I understand that it has been made with [Nw.js](https://nwjs.io) but in the end it's the same concept, now Electron while praised by many it's HATED (yes with uppercase) for some other people, there are even some clones of Steve Balmer saying that Electron is a Cancer.

Jokes aside, The complaints go for many places, from security to performance, to It's not native. now, the security stuff got so intense recently that for Electron 2.0 Github decided to include Warnings for most of the common security flaws there and whill you be surprised that many of the stuff you usually do doesn't adhere to CSP rules? of course the most common case would be that you use a bundler and the runtime you use is completely fine with CSP


But while many say it's not native you can still call some API's inside electron to do native stuff like notifications

Electron might not be the holy grail, because it isn't but one of the most important things about it (if not the most important) is that you can Crosscompile to Windows, MacOS, and Linux, This right here is the most important thing for me and the sole reason I believe it became so popular, share code between different operating systems


## [Universal Windows Platform](https://docs.microsoft.com/en-us/windows/uwp/get-started/universal-application-platform-guide) - UWP
The universal windows platform is a cool concept in theory, having apps that behave well in different platforms and sharing most of the code at basically close to no cost (there might be some cases where it's not that good I guess), Phones, Tablets, Desktops, Hololens, Surface Hub, Xbox,IOT, You name it wherever microsoft is, you should be able to deploy an application from the store once you have built it.

But it must be C# right? well not exacly, You can write UWP Apps in C#, C++, VB, Javascript, Xaml, DirectX12, Html, and all of those languages have access to the same API called the [WinRT API](https://docs.microsoft.com/en-us/uwp/api), so whatever you can do in C# as long as your target device can handle it, you can do the same in C++ or Javascript, and here's my catch, many of the electron detractors say that there is no javascript native solutions, that there aren't any opitions out there. Well I say they are wrong on that statement, I just Build up two different solutions in Javascript on Different Web Frameworks, without any `Audio` html tags, so I don't think it's right to say there are not Native Javascript Desktop development, you can find the [Aurelia](https://github.com/AngelMunoz/AureliaUWP) sample and the [Vue](https://github.com/AngelMunoz/Vue-UWP) sample pretty much similar in terms of approach, but one thing you will find equaly the same is the WinRT API Access, that you will find in a C# app, in a C++ app.

### Why isn't UWP/javascript that popular then?
It's basically one thing `It's Windows` why? you may ask, well microsoft has some history with developers, some love them some hate them, while it might be appleaing to write C# to have a broad audience, most of the folks out there writing web apps don't want to learn another language, because javascript is such a great language now days that much other languages may feel clunky or restricting for some people, I personally know C# and Java, (the one I can defend my self on quite well), some of Python too, but My main these days is Javascript, while python is a good alternative it doesn't fit what I try to build on my spare time, and today you can do/target almost anything in javascript.

So Being Javascript so popular, and UWP such a tempting platform not s popular option?

1. Windows Phones
    Javascript was targeted in it's early days more to the Windows Phone area, so it felt appealing to web developers that have web apps to bring them on, Sadly the Windows Phone market never took off and just died.
    so why do you want to take you code on a dead platform?
2. Lack of Non Microsoft Showcases
    Microsoft had put templates for Windows 8 and Windows 8.1 Apps that showcased WinJS to give the apps that "Native" look of their C# counterparts had, even WinJS team created shims and libraries to interoperate with AngularJS and React, but most of the marketed stuff was Microsoft only.
    So why would you know that you can use Vue, React (alone), Knockout, Ember, Aurelia, and other web frameworks if the only stuff you see in adds is Windows only?
3. WinJS
    WinJS is a javascript library for web applications, not only windows stuff, you can se a [Sample](winjs-l7ad.firebaseapp.com) there, but one thing is that, it's just a library on the level of jQuery, so you live your life modifying the DOM, instead of trying to build an app, I don't say low level (in web apps of course) is bad but today's libraries and frameworks let you forget what's working with the DOM like. another point here is that for Web Apps WinJS was the defacto way to go, and the WinRT API was nowhere to be found for javascript developers in the same way it is today, Microsoft gave the impression that it  was WinJS or nothing native when it came to UWP Apps in javascript.
    So Why would you use a low level library to do what other frameworks let you do easier?
    Why would you target a platform that you think (and the company gives you the impression) it needs a specific library to have native access?
4. Electron (at some detail, not most)
    Without the phone market mentioned in the first point, the most appealing cross compile feature was cross Operating System, not Cross Platform at the time so that's why electron took a bit of the UWP/JavaScript landscape, and adding to the third point, it didn't force/enforce you to use a specific library to do stuff

so take aways
- No one wants to target a dead platform
- While Microsoft stuff is cool, everyone else wants to do their way, not Microsoft's
- Not many people, that's why jQuery became less and less popular over the days
- you shouldn't need a library specifically to do stuff, It should come on the environment (It was on the environment but there were not much public awareness)




## Finish Thoughts
UWP/Javascript is Like Electron, they both use HTML/CSS/JS to create web applications, But I think UWP is a little bit stronger than Electron regarding Security, Platform reach (Platform, not OS), and Native API surface that why I would say UWP is Native even if it is JavaScript, but I would not say that to a 100% because I don't know how the internals work.
Electron is Cross OS so, that a major take away for most of people and even it is one for me also.

I do love both solutions I am not here to bash anyone this is just my opinion and my recapitulation of how I felt it happened these last years.


So with all of this information I just gave you out of nowhere but my experience over the last years what do you think? why people choose electron over UWP?
if you made Electron apps Why Didn't you chose UWP?


Share your thoughts with me!
