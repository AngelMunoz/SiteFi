---
title: Aurelia Script
subtitle: ~
categories: javascript,aurelia,electron
abstract: Let's spin off an electron app with the fairly new aurelia-script
date: 2019-01-07
language: en
---

Last year [Aurelia](https://aurelia.io/) introduced a version that can be put in a script tag and you are ready to go! it was named `aurelia-script`

{% github Aurelia/script %}

{% codesandbox wnr6zxv6vl %} 

Aurelia is conventions based, you need an `<name>.html` file with a `template tag` and a `<name>.js` file with a class and that's it you now have a component to be used anywhere else. the fact that Aurelia is conventions based, means that
you can go from *concept* to *prototype* to *feature* with the same files you started with.

### Setting up with Electron

When playing with this I found out that the router finds for these convention based files in the root of the server, using the dynamic imports, (`import()`) so this brings two things to the table, if you want to use `aurelia-script` you need to take into consideration that it is meant for browsers with dynamic imports support, and the other... is that loading from `file://` won't work at all!

this can be fixed quite easily, just run a small static server to serve your content, I chose `koa.js` but you can easily use a static server package out there

{% github AngelMunoz/AuExplorer %}

first of all our code in the `index.js` file at the root is pretty simple

```js
// imports
const Koa = require('koa');
const serve = require('koa-static');
const { resolve } = require('path');
const { spawn } = require('child_process');

const app = new Koa();
let elcProcess;

// I'm not sure how secure is this at all
app.use(serve(`${__dirname}/node_modules`));
app.use(serve(`${__dirname}/renderer`));

// get the correct electron bin for your platform
const electron = process.platform === 'win32' ? resolve('node_modules/.bin', 'electron.cmd') : resolve('node_modules/.bin', 'electron');
const indexFile = resolve(__dirname, 'main/index.js');

// after we successfully start our server, then spawn the electron process
app.listen(45789, '127.0.0.1', () => {
  // you could also add argv arguments if you need
  // like [indexFile, ...process.argv.slice(2)]
  elcProcess = spawn(electron, [indexFile], {});
  // bind process monitoring if you need
  elcProcess.on('error', onElcError);
  elcProcess.stdout.on('data', onElcData);
  elcProcess.on('exit', onElcExit)
});

function onElcData(chunk) {/*...*/}

function onElcError(err) {/*...*/}

function onElcExit(code, signal) {/*...*/}
```

nothing fancy just your every other day node server.

Inside the renderer we have our aurelia app which starts pretty much like the one I showed you in the codesandbox above

```html
<script src="/aurelia-script/dist/aurelia_router.umd.js"></script>
<script src="/localforage/dist/localforage.js"></script>
<script src="/dayjs/dayjs.min.js"></script>
<script>
  const aurelia = new au.Aurelia();
  aurelia
    .use
    .standardConfiguration()
    .developmentLogging();
  aurelia
    .start()
    .then(() => aurelia.setRoot(`app.js`, document.body))
    .catch(ex => {
      document.body.textContent = `Bootstrap error: ${ex}`;
    });
</script>
```
you might be thinking `why do I need to manually call these libraries! iugh! it's 2019!` well I just tried this as a proof of concept, so there might be better options on how to do this, perhaps parcel?, or you can just build your app and spit the bundle in there, but the principal idea of this sample is to go for simplicity and just put in some stuff together and just work it out!

other thing to have into consideration is that I turned off `node integration` for the sample and added a preload script to add the `ipcRenderer` to the window object so I could just send back and forth messages to the `main` process (more on that later on).

Let's take a look to our app.js file

```js
// simple class
export class App {
  constructor() {
    this.message = "Hello world";
    this.menuOpen = false;
    // bind process events to your class functions
    ipcRenderer.on('get:home:ans', this.setHomePath.bind(this));
  }
  
  // normal life cycle methods available!
  async activate() {
    const home = await localforage.getItem('home');
    if (!home) {
      // send a message to the main process
      ipcRenderer.send('get:home');
    }
  }
   
  // just like in any other aurelia app!
  configureRouter(config, router) {
    
    config.options.pushState = true;

    config.map([
      {
        route: ["", "home"],
        name: "home",
        // relative to the root of the server
        moduleId: "pages/home.js",
        title: "Home",
        nav: true
      },
      {
        route: "contact",
        name: "contact",
        moduleId: "pages/contact.js",
        title: "Contact",
        nav: true
      }
    ]);
    this.router = router;
  }

  toggleMenu() {/*...*/}

  async setHomePath(event, path) {/*...*/}
}
```

now you may wondering, how can the ipcRenderer be just there? no require no import no anything, well that's because we have a small preload script that does that for us, I'll show the `createWindow` function at the `index.js` in the main directory and omit the rest.

```js

function createWindow() {
  // Create the browser window.
  mainWindow = new BrowserWindow({
    /*...*/
    webPreferences: {
      // call the preload script
      preload: `${__dirname}/preload.js`,
      // disable node integration for this window
      nodeIntegration: false
    }
    /*...*/
  })

  // and load the index.html of the app.
  mainWindow.loadURL('http://localhost:45789')
  /*...*/
}

```
and then in our preload script
```js
const { ipcRenderer } = require('electron');
window.ipcRenderer = ipcRenderer;
window.PATH_DIVIDER = process.platform === 'win32' ? '\\' : '/';
```
you can use this script to expose node internals if you need them, like the [inAppPurchase API](https://electronjs.org/docs/api/in-app-purchase#inapppurchase)
but in my short thinking you should be able to accomplish most of the things by just using the `ipc`-inter-process communication.


### Thoughts
Well this was a cool thing to experiment and try on, it feels really great to just pull a script tag and have all the power of aurelia and it's conventions at your hands!
for example if you wanted all of the power of vue, the most ideal is to have `vue` files but when you use a script tag, that's not really a possibility you would need to change your vue components into other syntax which doesn't match the same experience of the `vue` files, I feel the same applies to other frameworks out there at the moment.

#### Extra

if you wonder how Dependecy injection and bindable decorators you would normally use fit in `aurelia-script` you can check this sample out

>https://codesandbox.io/s/92vmpwnjjo



Please share your thoughts and comments below and have an awesome week!
