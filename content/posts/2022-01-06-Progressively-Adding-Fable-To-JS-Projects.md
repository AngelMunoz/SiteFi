---
title: Progressively adding Fable to JS projects
subtitle: ~
categories: fsharp,node,javascript,fable
abstract: Sometimes using F# in existing projects isn't that hard let's take a look...
date: 2022-01-06
language: en
---

[fable]: https://fable.io
[php, rust, dart]: https://github.com/fable-compiler/Fable/pull/2653
[perla]: https://github.com/AngelMunoz/Perla
[fable-browser]: https://github.com/fable-compiler/fable-browser
[.net tool]: https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools
[.net sdk]: https://get.dot.net

> You can find the sources for the code we'll use in this post in [this repository's directory](https://github.com/AngelMunoz/blogpostdrafts/tree/main/progressively)

Hello everyone!

Hopefully you have had an amazing end of the year and the holidays are finally finishing for many (mine are done for sure), what a better time to start something new or even better yet progressively enhancing something that already exists!

## First of all What is Fable and what are the alternatives?

The [Fable] project is a `F#` -> `<Lang>` compiler where `<Lang>` is any of `Javascript`, `Typescript` and `Python` at the time of writing the last two are more experimental and the main support is for Javascript future iterations of Fable will cover these languages and even more like [PHP, Rust, Dart].

One could say Fable is a direct competitor to projects like Typescript, Flow, Purescript, ReasonML and similar projects which aim to write in a typed language to produce safer code. While every of the mentioned projects has their own pros/cons I won't discuss that here since it's very likely you already chose Fable for the new code effort.

### What does Fable do?

> As mentioned above Fable is an `F#` -> `<Lang>` compiler but from here on we will talk about fable in the context of an `F#` -> `Javascript` compiler.

Fable is distributed via a [.NET tool] which can be installed both globally and locally via `dotnet tool install -g fable` (or remove the `-g` to do it locally) meaning that it requires that you have the [.NET SDK] installed on your machine.

Before continuing into the complete topic there are a few myths that I want to get out of the way for sure

- Fable is a framework
- Fable is react
- Fable is for SPAs
- Fable is for new projects
- Fable requires Node.js

The truth and only truth is that Fable is an F# -> JS Compiler hence you can treat it like any other, just like you would treat typescript or purescript or reasonml or even babel. The reality would actually be

- Fable is a tool to produce Javascript code.
- Fable allows you to use React JS code as well as Svelte, Lit, and others.
- Fable can be used for single JS scripts as well as full SPA projects there are no hard requirements.
- Fable produces JS code, so wherever you can consume JS code Fable will work<sup>\*</sup> even slightly older projects.
- Fable can be used in any context outside nodejs like any python, ruby, or php servers.

> <sup>\*</sup> Fable emits modern javascript so your target needs to at least support the ES2015 ecmascript specification, in some cases (for older environments) further processing will be needed to re-transpile the JS code to ES3/ES5.

Having that said, let's dive into the topic at last.

## New Javascript projects

If you are not very familiar to nodejs because you are either a backend dev from other ecosystem or a frontend developer who happens to use node because that's how the ecosystem is right now I'll give you a run down the very basics of a node project.

type on the terminal on a new directory the following command

```
npm init -y
```

it should print something like this

```json
// Wrote to /path/to/directory/package.json:
{
  "name": "project1",
  "version": "1.0.0",
  "description": "",
  "main": "index.js",
  "scripts": {
    "test": "echo \"Error: no test specified\" && exit 1"
  },
  "keywords": [],
  "author": "",
  "license": "ISC"
}
```

That... in essence is a node project even if you haven't created a `index.js` as is indicated in the main field, of course you can add the file and adjust the newly created package.json like this

```js
// src/index.js
console.log("Hello, World!");
```

```json
{
  "name": "project1",
  "version": "1.0.0",
  "description": "",
  "main": "./src/index.js",
  "scripts": {
    "start": "node ./src/index.js"
  },
  "keywords": [],
  "author": "",
  "license": "ISC"
}
```

Now you can run `npm start` or `npm run start` you should see the lovely _Hello, World!_ message.

Yeah, yeah I know you didn't come here for the node part; New Fable projects are also very very simple, with the .NET SDK installed you just need to run

```sh
# you can delete the previous src directory just to make this work smoothly
dotnet new console -lang F# -o src
# The following commands are to install the fable .NET tool locally
dotnet new tool-manifest
dotnet tool install fable
```

While we can run fable from the terminal whenever we want we can leverage the fact that we're inside a node project and leverage the npm commands

```json
{
  "name": "project1",
  "version": "1.0.0",
  "description": "",
  "main": "./src/Program.fs.js",
  "scripts": {
    "start-app": "node ./src/Program.fs.js",
    "start": "dotnet fable src --run npm run start-app"
  },
  "keywords": [],
  "author": "",
  "license": "ISC",
  "type": "module" // NOTE: this is required to run the fable output
}
```

now you can enter `npm start` and you'll see Fable compiling then getting a _Hello from F#_ even if it was not run in .NET but node.js

If you want to target node.js this is a basic setup you can try. There are other tools like pm2 or nodemon that can help you minimize the developer feedback loop that can re-run servers or node processes and allow the debugger to connect.

## Existing Javascript projects

Let's create a new node project again and this time instead of creating a console app, we will create a class library

```sh
npm init -y
dotnet new classlib -o src -lang F#
# The following commands are to install the fable .NET tool locally
dotnet new tool-manifest
dotnet tool install fable
```

replace the contents of the package.json file with the following contents

```json
{
  "name": "project2",
  "version": "1.0.0",
  "description": "",
  "main": "./src/index.js",
  "scripts": {
    "start-app": "node ./src/index.js",
    "start": "dotnet fable src --run npm run start-app"
  },
  "keywords": [],
  "author": "",
  "license": "ISC",
  "type": "module"
}
```

The file structure looks like this

```
package.json
  | src
    index.js
    Library.fs
    src.fsproj
```

then add the following index.js

```js
import { hello } from "./Library.fs.js";

hello("Javascript");
```

and run `npm start` you should see the lovely _Hello Javascript_

At this point we can assume that any existing project and file on those projects in this case represented by our `index.js` can introduce F# in the code base and the reasoning for this is that this is the exact mechanism you can use to introduce typescript in a code base. Although, typescript benefits Javascript code from the editor and other tooling around so it's arguably easier but I digress, the main point is that you can either incrementally add F# code to your javascript project and let them co-exist side by side or you can slowly migrate JS code to F# code, file by file, module by module, however you feel the pace is better for your team.

Now let's take this exercise a little bit further just to show that we can do it, we will create a new vitejs project

```
npm init vite@latest project3 --template lit
cd project3 && npm install && npm run dev
```

This should run a lit plain JS project let's add two simple F# files to `src`

```xml
<!-- App.fsproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Library.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Fable.Lit" Version="1.4.1" />
  </ItemGroup>

</Project>
```

```fsharp
// Library.fs
open Lit
[<LitElement("my-counter")>]
let Counter() =
    // This call is obligatory to initialize the web component
    let _, props =
        LitElement.init(fun init ->
            init.props <- {| initial = Prop.Of(defaultValue = 0) |})
    let counter, setCounter = Hook.useState props.initial.Value
    html
        $"""
        <article>
            <p>{counter}</p>
            <button @click={fun _ -> setCounter(counter + 1)}>+</button>
            <button @click={fun _ -> setCounter(counter - 1)}>-</button>
        </article>
        """
```

inside `src/my-element.js` we will import the compiled fable file

```js
// src/my-element.js
import { html, css, LitElement } from "lit"; // this should be already there
import "./Library.fs.js"; // this line
// ... the rest of the file
```

next we will modify the "dev" script in package.json for the following `"dev": "dotnet fable src --watch --run vite serve"`.

Lastly we will add inside `index.html` the following content right inside the body element

```html
<my-element>
  <p>This is child content</p>
  <!-- This content is from our Fable Code  -->
  <my-counter></my-counter>
</my-element>
```

now let's run `npm run dev` and visit `localhost:3000` and we should see our counter inside the default

This particular technique is very powerful given that Fable.Lit produces web components meaning that you can render those in any existing framework so you can slowly migrate away from angular/react/vue using Fable.Lit components!

### Typescript Projects

In the case of typescript projects you only need to add `"allowJS": true` to the `tsconfig.json`'s compiler options

```json
{
  "compilerOptions": {
    //... the rest of the config
    "allowJs": true
  }
  //... the rest of the config
}
```

### Webpack and other bundlers/dev servers

In the last example we used vite which loads ES modules by default, other modern tools like webpack/snowpack/parcel should be exactly the same, just import those fable output files where you need them and the bundler should manage that since (and I emphasize) Fable output is modern standards javascript.

that will make typescript to also process your Fable output files

> **_NOTE_**: If you have a strict config enabled you might face issues with _implicit any_ errors, you can also add `"checkJs": false` so your Fable output doesn't get re-checked by typescript (after all it has already been checked by F#)

## Good ol' monoliths

I hear you, you have a [Django | ASP.NET | Express | Flask | RoR | Laravel | Slim] app that doesn't use a SPA like tool chain that serves it's own javascript files statically (wwwroot in the case of .NET)

I have good news for you, you can use any of the approaches above to produce your javascript and include it in your `JS modules` or directly in the `index.html` there are are some caveats about Fable projects with JS dependencies. There are two approaches here you are managing your JS dependencies in any of the following ways

- via NPM
- via CDN/Local Dist file

If it's via NPM and you already have sorted out how to serve those then it's about just using Fable as usual and let it emit your JS files directly to the static files directory via fable's outDir flag: ` -o --outDir Redirect compilation output to a directory`, something along the lines of `dotnet fable fable-sources -o wwwroot` and it should just work.

If you need to handle dependencies via CDN/Local Dist file then some dependencies won't work because they use node like imports `import {} from 'lit/some/sub/directory.js` browser imports need to start with `/` or `./` or even `../` so they can be valid ES module imports thankfully for this you can check out in a shameless plug one of the projects I'm working on: [Perla] which handles this precise case but I digress, the ideal situation would be you with npm and already figured out how to serve node dependencies to your compiled code.

Please remember that each F# file is equal to a single JS File when it's ran through fable so you can create scripts for specific pages, you don't need to import/export everything from a single entry point and you can use [fable-browser] to do DOM manipulation, so it is not necessary to add a whole SPA framework to enhance parts of your monolith.

### Final Thoughts

A brief recap, we just saw how to add Fable

- New node projects
- Existing node projects
- New/Existing Vite/Webpack/Typescript projects

the short summary would be this

1. Get the .NET SDK
2. Create a new F# project (either console or class library)
3. Install Fable as a local/global tool
4. Integrate the fable command as part of your workflow (in our case the npm scripts we modified above)
5. Run Fable and start enhancing with or migrating to F# your code base.

Also we got remembered that Fable outputs Javascript, not react, not a SPA, not anything else (in the context of this blog post) so your existing knowledge of how to use Javascript inside a SPA, Monolith, Node.js applies exactly the same.

I put a lot emphasis on that because I have seen people who believe Fable _must_ be used in a certain way or that there's a religious way to use it. No it's a tool and has several uses feel free to pick your own way to use it.
