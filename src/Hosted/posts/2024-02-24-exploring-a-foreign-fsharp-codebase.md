---
title: Exploring a foreign F# codebase
subtitle: ~
categories: fsharp, dotnet, codebase, exploration
abstract: Let me share with you how I've learned to explore new F# codebases...
date: 2024-02-24
language: en
---

Hello folks, here we are again with more F# content for you!

I have a few things in mind that I want to write about relative to some of my older projects, but I'm not settled in stone yet.

This blog post is going to be part of a series called "Dissecting an F# codebase" where I'll try to tell you how to approach new codebases, what to look for and how to get a handle on them. Today's topic is the first one and relatively simple: "This place is inmense, how do I even begin to explore it!?".

For that we'll be working with [Siquelin] a small project I wrote a few days ago in order to produce a series of blog posts, some about [Migrondi] and some about this series.

In case the contents of the project have changed at the time you're reading this, please check the git tag: [`01-exploring-a-foreign-codebase`]

## Finding a cool F# project to explore

While you can follow these tips with the stated project above, please feel free to check it out with any project you already have in mind or new ones to see if this will work for you or not.

The first step when you have a project already in mind, is to simply visit their repository and check out what the `README` has in place

> ## WIP
>
> This repository is meant to be source material for a future set of blog entries for more F# goodies.

Oh great... that's quite useful, isn't it? 😅

Hmm maybe file structure?

```
.config
.vscode
migrations
.editorconfig
.gitignore
Commands.fs
Database.fs
Env.fs
Extensions.fs
Migrations.fs
Program.fs
README.md
Siquelin.fsproj
Types.fs
```

Yes... but no. GitHub doesn't help very much in this department as it doesn't have any notion of how the files are used. If you come from other languages you also know that Files can be deceiving as they don't tell much about the codebase.

Well, here's one of the cool things F# offers for you: The often controversial "Top-Down File Ordering" requirement. Many folks when they come to F# from other languages are quite annyoed by this and is seen as a limitation, which perhaps it is! But it also has a very cool side effect: It gives you a very clear idea of how the codebase is structured.

So rather than doing what we've tried so far, let us check the `Siquelin.fsproj` file.

> **_NOTE_**: Keep in mind that larger projects may have multiple `.fsproj` files. In those cases, you need to determine which one is the main project and go from there.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Extensions.fs" />
    <Compile Include="Types.fs" />
    <Compile Include="Migrations.fs" />
    <Compile Include="Database.fs" />
    <Compile Include="Env.fs" />
    <Compile Include="Commands.fs" />
    <Compile Include="Program.fs" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Donald" Version="10.0.2" />
    <PackageReference Include="FSharp.SystemCommandLine" Version="0.17.0-beta4" />
    <PackageReference Include="Migrondi.Core" Version="1.0.0-beta-010" />
  </ItemGroup>
  <ItemGroup>
    <None Include="./migrations/*">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

Oh! ok, ok cool, this is a bit more useful. From checking this file we can see that the project fills the following bullets:

- It is a console project (otherwise it wouldn't have the output type node)
- It targets dotnet 8
- The entry point is `Program.fs` (top-down file ordering!)
- It has 3 dependencies: `Donald`, `FSharp.SystemCommandLine` and `Migrondi.Core`
- It is copying the migrations folder to the output directory

> **_NOTE_**: You can try this with the [Feliz] codebase, which is larger and has multiple `.fsproj` files. But as you will find out, the project structure is laid out just like we've seen here.

We can now go back to the Top-Down file ordering requirement and quickly determine that `Extensions.fs` is the most accessible file in the project as it is at the top and any other file below it will have knowledge from this file. The last file is `Program.fs` which must have the entry point for the application given that we've already determined that we're working with a console application.

Any other file in-between them may tell the structure of the application. But keep in mind that just because a file is ordered above another it doesn't mean it is using it's sibling contents. Sometimes these files don't use each other and may be using the contents of the files above them instead.

Let's check our entry point first.

```fsharp
open FSharp.SystemCommandLine
open Siquelin
open Siquelin.Types
open Siquelin.Migrations

[<EntryPoint>]
let main argv =

  let env = Env.getEnv()

  // run this at the start of the app regardless of the commands
  // this will ensure that the database is up to date
  Runner.runMigrations(env.logger, env.migrondi)

  rootCommand argv {
    description "A simple time tracking cli example"

    // set the main handler to do nothing
    setHandler id

    // add a subcommand to list shifts

    addCommand(Commands.logDay env)
    addCommand(Commands.logItem env)
    addCommand(Commands.listItemsForDay env)

#if DEBUG
    // only allow this command in debug mode as it is meant for dev purposes
    addCommand(Commands.Hidden.newMigration env)
#endif
  }
```

Oh, looks like the author at least gave us some comments to work with and figure out what the thing is doing. It seems like it's a CLI application for time tracking. It also seems like it is running some sort of migrations even before the commands are parsed and executed which is a bit questionable but we don't know the reasons behind that

So from this file we've at least determined that it is a CLI application which has four commands and one is hidden.
We can pick the next file in the list or we can check the `Commands` module to feed our curiosity and coincidentally the `Commands` module lives in the `Commands.fs` file!

> **_NOTE_**: For larger codebases with more history it is likely that the `Program.fs` file will have a lot of orchestration and logic as well. given that it is often where everything clashes and starts, for example the [Fable] Entrypoint is in `Entry.fs` and it contains a lot of code. The best you can do always is to start at the bottom of the file and work your way up. Remember: Everything at the bottom uses what has already been defined at the top so there are no circular dependencies or random functions/types at the bottom that can trip you off, everything comes from the top!

If the file you're going to check next is big, or contains a lot of modules, I'd recommend you to fold/collapse via your editor the modules and check them one by one.

If we apply the tip from above then the `Commands.fs` file would look like this:

```fsharp
namespace Siquelin

open System
open FSharp.SystemCommandLine

open Siquelin.Types

module Parsing = ...

module Handlers = ...

module Commands = ...
```

Which is very helpful, otherwise this file would blast us 200 lines of code to the face directly filling us with a lot of context we don't understand yet which may become quickly confusing!

Following from our previous experience, we knew there was a `Commands.logDay` function, so we can check the `Commands` module and see what it does.
Once again following the same tip, we can fold/collapse the module and check the `logDay` function.

```fsharp
module Commands =
  let logDay (env: Env.AppEnv) = ...

  let logItem (env: Env.AppEnv) = ...

  let listItemsForDay (env: Env.AppEnv) = ...

  module Hidden = ...
```

Oh, so it seems like the `Commands` module is just a collection of functions that are being used in the `Program.fs` file.

As a word of caution, these functions seem to be asking for an `Env.AppEnv` type, it can be easy to start looking into what this `AppEnv` is and quickly get derailed looking into more, and more, and more code! My personal recommendation is to first keep checking the function is doing and _how_ it is using this so called `AppEnv`

```fsharp
let logDay (env: Env.AppEnv) =
    let argument =
        Input.Argument<string>(
        "day",
        "The day to log, in the format of 'yyyy-MM-dd'"
        )

    let cmd = command "log" {
        description "Start a new work day"

        inputs(argument)

        setHandler(
        (fun day ->
            match Parsing.dayParser day with
            | Ok day -> day
            | Error e -> failwith e
        )
        >> Handlers.logDay(env.logger, env.workdays)
        )
    }

    cmd
```

Looks like we're creating a command with a description and an argument, and then we're setting a handler that is using the `Parsing.dayParser` and `Handlers.logDay` functions.

From here, it looks like the first parsing function is going to be composed with the `Handlers.logDay` function thanks to the `>>` operator.
We don't know the shape of the `Handlers.logDay` function yet but now at least we know that the so called `AppEnv` is some sort of dependency container as it has a logger and a workdays property.

The following step would be to check the `Handlers` module and see what the `logDay` function is doing.

```fsharp
module Handlers =
    let logDay (logger: ILogger, workdays: WorkdayService) (day: DateOnly) = ...

    let logItem
        (logger: ILogger, workDays: WorkdayService, shiftItems: ShiftItemService)
        (start: TimeOnly, finish: TimeOnly, day: DateOnly option) = ...

    let listItemsForDay
        (logger: ILogger, shiftItems: ShiftItemService)
        (day: DateOnly option) = ...
```

Oh, similar to the `Commands` module, it seems like the `Handlers` module is just a collection of things and by the looks of it these are functions.

```fsharp
let logDay (logger: ILogger, workdays: WorkdayService) (day: DateOnly) =
    logger.LogInformation(
        "Logging a new work day: {day}",
        day.ToLongDateString()
    )

    workdays.create day
```

Cool! So it looks like the `logDay` function is just logging a message and then calling the `create` method on the `workdays` service.

Notice how we've been able to follow the code without doing any `Ctrl/Cmd+F` or Go to definition or a simiar action. the code so far has been very linear and hasn't done any kind of weird indirections or anything like that.

Let's skip one of the tips above and check the `WorkdayService` and see what the `create` method is doing via go to definition.

It took us straight into the `Types.fs` file and we found this:

```fsharp
namespace Siquelin.Types

open System

type WorkDay = { id: int; date: DateOnly }

type ShiftItem = {
  id: int
  workDayId: int
  start: TimeOnly
  finish: TimeOnly
}

module Env =
  open Microsoft.Extensions.Logging
  open Migrondi.Core

  type SiquelinDataLocations = {
    appDirectory: string
    appDataDirectory: string
    databasePath: string
  }

  [<Struct>]
  type ShiftItemQueryError = | WorkDayNotFound

  type WorkdayService =
    abstract member create: DateOnly -> unit
    abstract member list: unit -> WorkDay list
    abstract member get: DateOnly -> WorkDay option
    abstract member exists: DateOnly -> bool

  type ShiftItemService =
    abstract member create:
      DateOnly * TimeOnly * TimeOnly -> Result<unit, ShiftItemQueryError>

    abstract member list:
      DateOnly -> Result<ShiftItem list, ShiftItemQueryError>

  type AppEnv = {
    locations: SiquelinDataLocations
    logger: ILogger
    migrondi: IMigrondi
    workdays: WorkdayService
    shiftItems: ShiftItemService
  }
```

Looks like a file defining some types and a module with even more types... Aha there it is!

```fsharp
type WorkdayService =
    abstract member create: DateOnly -> unit
    abstract member list: unit -> WorkDay list
    abstract member get: DateOnly -> WorkDay option
    abstract member exists: DateOnly -> bool
```

Oh great just an interface definition 🫠... where is this being created? Maybe if I click in `See usages` I can find it!
Well... yes, you may find it that way, but do you want to do that?

You just followed up to check up a type in a function parameter and you're now exposed to this other information which may be a distracting factor if you're trying to understand how it works, it might have been useful and give you other contexts which is fine the more seasoned F# developer you are. However, if you're still not comfortable enough with F# this might be introducing you to more noise and overload you making you think: "Hmm I better check out this later there's a ton of stuff there...".

We've felt for this trap so... how do we get out of here?
Let's go back to the `Handlers` module and check the `logDay` function again.

```fsharp
let logDay (logger: ILogger, workdays: WorkdayService) (day: DateOnly) = ...
```

We now know that our `logDay` function is asking for a `WorkdayService` and a `ILogger` and thanks to our "go to definition" click, we also know that the `WorkdayService` is being part of the `AppEnv` type.

But wait, we already knew that, didn't we? We knew that the `AppEnv` type was being used in the `Program.fs` to create the commands and within the `Commands` module we also found out that the `AppEnv` type had a `workdays` property.

Let's check the `Program.fs` file once again but let's check the `Env` module this time.

```fsharp

[<EntryPoint>]
let main argv =

  let env = Env.getEnv()

```

Oh, great looks like there's a function in the `Env` module that is creating the `AppEnv` type for us. Let's check it out.

```fsharp
module Siquelin.Env

open System
open System.IO

open Microsoft.Extensions.Logging

open Migrondi.Core

open Siquelin.Types.Env
open Siquelin.Migrations
open System.Data

let private loggerFactory = ...

let private getEnvLocations () = ...

let getEnv () : Types.Env.AppEnv = ...
```

Oh, this file is laid out differently than the others, while sure it has an `Env` module, it looks like the whole file is a single module instead. It also looks like it is creating the `AppEnv` type for us and it is using a `loggerFactory` and a `getEnvLocations` function to do so.

Let's expand the `getEnv` function and see what it is doing.

```fsharp
let getEnv () : Types.Env.AppEnv =
  let logger = loggerFactory.Value.CreateLogger("Siquelin")
  let locations = getEnvLocations()

  // create the database directory if it doesn't exist
  // to avoid exceptions when trying to create the database file
  Path.GetDirectoryName locations.databasePath
  |> Directory.CreateDirectory
  |> ignore

  let config = {
    MigrondiConfig.Default with
        connection = $"Data Source={locations.databasePath};"
  }

  let migrondi = Runner.getMigrondi locations.appDirectory config

  let getConnection () : IDbConnection =
    new Microsoft.Data.Sqlite.SqliteConnection(config.connection)

  let workdays = Database.Workday.factory getConnection
  let shiftItems = Database.ShiftItem.factory getConnection

  {
    locations = locations
    logger = logger
    migrondi = migrondi
    workdays = workdays
    shiftItems = shiftItems
  }
```

Welp, at least we now know where this `workdays` implementation is coming from. Looks like it is being created from a `Database.Workday.factory` function.

Sheesh... we're out of the trap! we're back to the normal flow where we can check things from the bottom to the top and not get derailed by other things.

But how would the non-trap version would have been?

For that we would have taken just the same steps but in reverse.

- I'm in the `logDay` function who is supplying these paramters?
- I'm in the `Commands.logDay` function this is supplying me with the `env` parameter, where is this being supplied?
- I'm in the `Program.fs` file and I see that the `env` is being created in the `Env` module, let's check that out.

And then we would have been in the `Env` module and continue our bottom to top exploration with folded members/modules. This would have prevented us from getting cognitive overload from information we don't necessarily need at this moment. Keep in mind that while those are two approaches to the same problem (figuring out how does `WorkdayService` looks and it is implemented), I personally believe that one method is better for seasoned F# developers and the other is better for newcomers. Feel free to mix and match these techniques so far to your liking and see what works best for you.

What do we know so far?

- We have a CLI application
- It has 4 commands, one of them is hidden
- It is running migrations before the commands are parsed and executed
- The `Commands` module is using an `AppEnv` type
- The cli commands are being created in the `Commands` module
- The `Commands` module is using a `Parsing` and `Handlers` module
- The `Handlers` module is using a `WorkdayService` and a `ShiftItemService`
- The `WorkdayService` and `ShiftItemService` are being created in the `Env` module
- The `Env` module is creating the `AppEnv` type
- The `AppEnv` type is being used in as a dependency container which is created in the `Program.fs` file

I hope that at this point you're spotting the discovering pattern and can start using it to explore foreign F# code bases. Perhaps that will ease a bit of stress when you're trying to understand a new codebase and potentially contribute to it.

- Find the desired `.fsproj` file
- Check the dependencies
- Check the top-down file ordering
- Check the last file in the list
- If the code is big, fold/collapse the contents and check them from the bottom to the top
- Visit the next module/file above what you're currently checking and repeat the process.

This is my personal way of exploring a new codebase, and I hope it helps you as well.

> **_Note_**: For applications, this is a good way to start, but for libraries it might slightly different. Libraries don't have "entry points" in the same way applications do, so while you might still want to check the `.fsproj` file and the top-down file ordering The files in there might be sibling files and not sharing code between them, or there might be a set of indirections that you need to follow to understand how the library works. But I still find the bottom to top approach to be useful in those cases.

We'll stop here for the moment as this is just one entry in the series. Next time we'll talk about partial application and how it is used for dependency injection in the `Siquelin` project which probably at this point you've already spotted it.

In case you have any questions or comments, feel free to reach out to me on Twitter, Threads, The Fediverse or GitHub.
I hope you've enjoyed this entry and I hope to see you in the next one!

[Siquelin]: https://github.com/AngelMunoz/Siquelin
[Migrondi]: https://github.com/AngelMunoz/Migrondi
[Feliz]: https://github.com/Zaid-Ajaj/Feliz/blob/master/Feliz/Feliz.fsproj
[`01-exploring-a-foreign-codebase`]: https://github.com/AngelMunoz/Siquelin/tree/01-exploring-a-foreign-codebase
[Fable]: https://github.com/fable-compiler/Fable/blob/98bf8288b154cbae4ebfc29db79ad9ac163906e1/src/Fable.Cli/Entry.fs
