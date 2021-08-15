---
title: JavaScript Windows Universal Applications Part 2
subtitle: ~
categories: javascript,uwp,vue,native
abstract: What if I show you a native audio player inside a vue UWP without an audio tag?
date: 2018-06-19
language: en
---

You may seen this before: 
{% link https://dev.to/tunaxor/javascript-universal-windows-applications-58n5 %}


while the title is a little misleading, it's more a complain about why I feel sad about the current state of the UWP JavaScript ecosystem rather than show *that* ***you*** *can target UWP*


this time I will try to be more centered on the topic and show you, All the code samples pictures are in this repo 

{% github AngelMunoz/Vue-UWP %} 


So this post is more targeted towards those people who have a little dislike for electron, because of Security reasons, Performance reasons, etc.

Please note that I'm not against electron I'm a fan as well, but there's people that say there are not JavaScript -> native desktop alternatives for them (this is one), I know there is no cross platform (between apple stuff and Linux), but no solution is perfect, you can go back to electron if you want/need to :P



# Why Vue
Why Vue and not react you may think? this setup applies to ANYTHING that is JavaScript HTML and CSS. For security reasons there are some limitations, CSP is enabled by default, and the environment is as restrictive as you may think so anything that does not adhere to CSP will fail, that's why I recommend you to bundle your stuff regardless of what framework you choose, many runtimes adhere to CSP (like the Vue Runtime). I've got other samples in my repositories too, including angularjs, Aurelia (if Aurelia works, be sure that Angular +2 will work also), and even a WinJS sample if you want to go all crazy manipulating the DOM yourself in these days!


# Code Samples!

Let's begin directly with the Component that Natively loads an mp3 from the file system and plays it as your good ol' windows media player, your new Groove (that will die one of these days), or your usual Spotify (which I know is not native)

{% gist https://gist.github.com/AngelMunoz/794c2e362e9d3b4033011d420a6b3f49 %} 


Just to mention I'm using [Vuetify.js](https://vuetifyjs.com) for the view so if you wonder what a `v-toolbar` is, well you can watch them in their site.



## Data
so let's go in parts, data as you may have guessed is the normal data function you will find in any vue component around the world, nothing fancy, we initialize some properties, so we can access them later, like `file`, `player`, `picker`, etc

## Before Mount
So this is where we pick the native part of this App
```js
// create a new media player
this.player = new Windows.Media.Playback.MediaPlayer();
// bind on media ended listener
this.player.onmediaended = this.resetControls.bind(this);

// create a new picker
this.picker = new Windows.Storage.Pickers.FileOpenPicker();
this.picker.viewMode = Windows.Storage.Pickers.PickerViewMode.thumbnail;
// set initial suggested location
this.picker.suggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.musicLibrary;
// specify extensions you want to use for the file picker
this.picker.fileTypeFilter.replaceAll([".mp3"]);
```

if you are wondering "Where does `new Windows.Media.Playback.MediaPlayer();` comes from!?

it comes from this [API](https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/media-playback-with-mediasource)

now if you check that link you will find that the docs are in C# but, the API does exist in JavaScript too so who needs C# in this App right?

also we assign an event listener to the player, once it finishes playing the media we'll let it know what to do within our component


now we Initialize our `Picker` is your standard File picker from windows, we let it know that it should look for songs at the Music directory from the User's library and tell it to only pick files that end in `.mp3`


now that our component is ready and rendered let's theck the Methods

## Methods
We have 3 important methods right now
```js
async pickAudio() {
  try {
    // lets try to pick a file
    const file = await this.picker.pickSingleFileAsync();
    // oh wait! is that async/await?
    if (file) { this.file = file } // if there's a file we use it
     console.log(file);
   } catch (e) {
    // if there's an error we handle it a console.log will be fine for now
     console.error(e);
   }
},
play() {
  // check that we can play stuff (check the player and the media source)
  if (!this.player || !this.player.source || !this.mediaSource) {
    this.mediaSource = Windows.Media.Core.MediaSource.createFromStorageFile(this.file);
    this.player.source = this.mediaSource;
  }
    // Kaboom! the native stuff playing out there
    this.player.play();
    this.isPlaying = true
   },
pause() {
  // let's just pause for a second!
  this.player.pause();
  this.isPlaying = false;
},
```
So When we pick the audio, we don't need to use complex logic nor reinvent the wheel In JavaScript, these methods are just there in the environment.


Once you click on the small music'y button you will be prompted to pick a file
![File Picker](https://i.imgur.com/7XONfcp.png)


Notice that there is no `<audio>` HTML tag. If you were wondering that was going to be the trick on the sleeve, I'm sorry we don't need HTML5 for Audio Here!

Once you hit play and change the volume in your laptop/desktop
you will see something like this 

![Audio Controls](https://i.imgur.com/0FCzruu.png)

or if you are still into windows phones

![Audio Controls](https://i.imgur.com/fPp0WAI.png)

so... you are also abe to access the metadata in that audio control so you can show who the artist is and song name, and more information if you need.

# Home
You just need to include this component in your home page and it will be working already


```js
import UwpMediaToolbar from "../components/UwpMediaToolbar.vue";
```
```html
<uwp-media-toolbar></uwp-media-toolbar>
```

was that complicated? I hope not so much
if you want a System Dialog it isn't that much complicated too
```js
new Windows.UI.Popups.MessageDialog(`Woah You Clicked Me!\nAs Native as It Gets`, "I'm Clicked!").showAsync();
```
if you don't like the namespacing from the WinRT runtime you could always desctructure what you need at the top of your script part
```js
const { MessageDialog } = Windows.UI.Popups;
```
and also this applies to ***Any*** framework that compiles a CSP compatible javascript bundle, not just Vue


how could you improve this? You may wan to check out [this sample with Aurelia](https://github.com/AngelMunoz/AureliaUWP) which you can select multiple files and make a playlist with shuffle and that sorf of stuff


you can always abstract to that to a javascript file/function/class somewhere and call it as a library or helper function


# Last thoughts
This is not just Media Player stuff. this can apply to Bluetooth, to Point of Sale Apps, for Wi-Fi access
For anything your average windows can do this will be able as well the decision is yours.


In the end I just want to tell you that it might be a little unexplored territory for the UWP/JavaScript thingy, but for example if you are part of a company that is developing or going to develop stuff for windows and are short of C# pals, if you want to leverage the Windows Store as a distribution mechanism, this is a viable alternative this is not new stuff, this has been around since Windows 8, also remember you can sign your package and distribute as an appx file that can be side-loaded inside the system (more like a company case, but you could do that in exchange of .exe packages in some cases)


Besides electron and electron-like frameworks, what are your options for JavaScript native desktop development? don't say None, here you have one.


## Not everything is glory
We're well aware of the phone market of windows (it doesn't exist), there's a rumored phone like device in the works that could change things, but nothing is certain.

Also this is Windows 10 Only stuff, unless you manage to abstract the file loading mechanisms, audio playing so you can use HTML5 when you are in the browser and native media when you are on windows, (which isn't impossible by the way, the simplest, least elegant way is to check with an `IF` that there's the `Windows` namespace :P)
about performance, well I know there's not the whole chromium in there, but the chakra engine is there, so perhaps with more experimenting someone can shed light in this place

`It is windows`  Well buddy, I can't help you there if you don't like windows this stuff is not for you :)



Sorry for my grammar, sorry for my bad writing, but I hope you can use this for any good reason!
