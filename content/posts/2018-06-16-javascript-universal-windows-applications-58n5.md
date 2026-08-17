---
title: JavaScript Universal Windows Applications
subtitle: ~
categories: javascript,vue,aurelia,angular
abstract: Let me show you that you can target windows store apps as well!
date: 2018-06-16
language: en
---

I will mark this post on the history/melancholic reason I did the projects I will show you, if you want to skip it go ahead

> Melancholic stuff starts


Sometimes We love technology, sometimes we just want that technology to take us places where we never imagined! one of those technologies I used to love was the concept Behind Windows ~~Metro~~ ~~Modern~~ Universal Apps now commonly known as **UWP** and the promise was 
>One Common Language Runtime All platforms


yes, targeting web, phones, desktop, tablets, xbox, anything that ran windows cool right!? this will take us t the future where all cars fly and no one is offended by anything....(not even this last sentence)

and one of the most amazing things is that you could start doing so with javascript! yes Universal Windows Applications with javascript! now anyone could make these apps, I mean they were trying that for years with cordova and the atom shell (electron) was about to come (or it already was there) so why not windows?


Well it was easier said than done at the time I remember Windows 8.1 update was almost arriving, On April 8, 2014, Microsoft released the Windows 8.1 Update
I was still in university I was amazed by the work that was being done to fulfill that dream but there were some conditions that did not allow this to happen specifically on javascript (C# this post is not about you!) wether you like it or not, stack overflow was not roasting people (yet... well that much)  people for using jQuery, Angularjs Was Still a Boss, so Microsoft released their own library to compete with jQuery and interop with other frameworks like Knockout, AngularJs or even React! that library was called **WinJS**, and it had a look and feel that matched perfectly Windows looks at the time.



It grew good to the point it got to version 4.4.x and you could do basically anything that the UWP Apps let you do at that time, tailored for web experiences, as well because you could use it outside windows apps! it was a cool experience for me, I was still looking at the dream.

Sadly It didn't grow too much, Microsoft failed on the phone market, they had ton of shit in their store at the time (it is better now, it's recovering, but the damage was done), they seriously needed  to change their strategy, so then Windows 10 came to change things for good! but again, due to the lack of developers javascript apps were in a bad spot, and they lost navigation/sample templates once the windows 10 sdks came out (even today that's still true, we only have the empty/winjs templates, well with the addition of PWA support but that's ultra recent), so new developers, and other developers (if there were any) as well stoped doing javascript apps for the UWP because it felt like we were left behind! 


WinJS entered in maintenance mode, to the point that today is stale, call it dead, because it feels wrong to call it other way

In the end I felt sad on myself because I did want to help the ecosystem but I didn't have any experience that helped me to do so and so I went on my journey in JavaScript land after this traumatic process of dream denial

> Melancholic Stuff Ends


Few years later! here we are with JavaScript under our pockets and over our food!
the JavaScript ecosystem is one of the most rich there is, the language itself has evolved so much and so well, that I decided to take a look at what were the UWP (in JavaScript) doing at the moment, after playing around on the samples I thought that you had to do all of the vanilla JavaScript to create a good app, and since WinJS lib was basically dead why would I even try to do DOM manipulations with a dead lib? (sorry I'm too young to know what was to do DOM manipulations all the time with al the JavaScript quirks that today are a mere joke)


I think that's an important reason of why people don't pick up this project for UWP apps, you go there and you are offered nothing but "Good luck boy, there's nothing much to do here", so Since Edge supports ES2015, I thought on myself *why don't I mix and Match some ES2015 with older not so mainstream tech from today?*

thus these following projects were born

{% twitter 1007631165782716417 %}
{% twitter 1007772728290619395 %}
{% twitter 1007911648739414016 %}


yes! at the end I even included some Vue and Aurelia (no React Angular 6 guys sorry) samples including some more conservative even may be called legacy options with anguarjs (1.7.x) and winjs and let me tell you a thing!


the truth is that you have Full access to the WinRT API! that means you are able to do most if not all of the things you can do in C# or Visual Basic or C++ that have access to the WinRT API, no need of WinJS, no need of frameworks... nothing it's there!, and you are targeting a browser environment so if Edge can render it, so can a UWP App


I think the lack of boilerplates, the lack of people showing what can be done with your everyday tech piece inside a JavaScript UWP is what makes us miss this target some times, I know there is Xamarin, but to be fair every time I install it on a new or formatted PC, I create a project from the templates and it always fails to compile, so I need to spend hours looking for solutions.
I know it  cross compiles to other OS'ses but not everyone needs that, not everyone wants to change to C# and do a windows app, sometimes you already have some web app there and want wish to have a way to better distribute your content, etcetera



I know it's not the most common use case, but if you ever felt that a UWP in JavaScript could have saved you and you didn't do it because you think it had no support, well let me remember you that the UWP team, the Chakra team and even the Visual Studio team, support this kind of target so don't be afraid to target it! because in the end if they discontinue the JavaScript support (sure as hell no) IT is Still a Web Application! a couple of changes and ready to redeploy on the web!




So I learned quite a lot doing these projects, I finally feel that I contributed on my part at least to keep a faded dream alive, and I am confident that I will target UWP apps without fear anymore, I've got Vue, Aurelia and even AngularJS (with a transpiler/bundler if necessary) on my back


Take aways if you are going to develop a UWP in JavaScript

## ES2015 Modules
the ES2015 modules syntax is available BUT you need to do a fully qualified import
`import util from ./util'` Won't work and fail silently, you need to do something like `import util from '/src/utils/util.js'`, Yes with extension! that's really important! another one, if any of the imports inside `util` fails, it will make the `util` script to silently fail to be imported as well!



## Classes
Use them! you have the OOP'nes of classes at your disposal if you don't feel comfortable enough with prototypes (even though classes are just sugar on them), classes and modules fit all well!, if you want to go full functional using functions as well feel free to do so! as long as it works on edge it will work there!


## CSP
Security is something that matters and the Electron team realized that very well to the point of including warnings on the console on dev mode, so you will have to work with CSP enabled, no eval, no inline stuff, no new Function, so if your dependency works with this, make sure they have a  CSP compliant version else you won't be able to use it, Vue by itself can't be used in a UWP because it uses these things to compile the templates, but once it's compiled it makes no use of such functions! that's why you can use it safely with a bundler! same case with Aurelia


## Fonts

Be ready to load your fonts locally, because since the CSS scripts generally try to pick them from the web, the CSP policies will block these resources


## Bundlers
if you are going to use a bundler like webpack, that like to hash the compiled versions, and code split and stuff like that, it will interfere with the visual studio build, the visual studio build likes to statically know what does it has to load and hat is available to it, so your dist names should be consistent to the point that the name doesn't change so you can keep testing, also remember o run their build/watcher scripts on the background so you can keep refreshing your app


## anchor tags
these may break your app's navigation if you are not careful, for example in the Aurelia router they use something like this in the html `route-href="route:home"` that ends up just adding an href attribute, but this breaks on the UWP app and ends up reloading your app, if the router of your application (wether it's Aurelia or not) does something like this, try to call that router programmatically.



so... quite lengthy! I please hope that you liked this reading (with my typos and grammar stuff along the way and my parenthesis too!) the links to the projects are below, please if you can provide feedback I'd love to hear it, if you can share these also I'd be glad, thanks and have a good weekend!


https://github.com/AngelMunoz/WinJS-ES2015-UWP
https://github.com/AngelMunoz/Angularjs-ES2015-UWP
https://github.com/AngelMunoz/Vue-UWP
https://github.com/AngelMunoz/AureliaUWP
