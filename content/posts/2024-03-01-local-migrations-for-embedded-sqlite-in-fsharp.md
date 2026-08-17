---
title: Local migrations for embedded SQLite in F#
subtitle: ~
categories: fsharp, dotnet, codebase, exploration
abstract: Running migrations in a server is simple, but how about local embedded databases?...
date: 2024-03-01
language: en
---

Hello there, once again we're back with more F# goodies.

There's a very minimal chance that you might remember one of my F# OSS projects called **Migrondi**.

https://github.com/AngelMunoz/Migrondi

It is fair as I wrote it over four years ago, So I'll give you a quick reminder:

> Migrondi is a SQL Migrations tool designed to be simple and execute simple migrations. Write SQL and execute SQL against your database.

I initially wrote it as a CLI tool, but in my last attempt I tried to expose the core parts as a library, mostly to support programmatic cases as well as to be able to change the backing storage from file system to something else.

For the most part supports some of the major databases like SQL Server, MySQL, PostgreSQL, and SQLite.

Having that said... Have you ever written a CLI tool, or any kind of user facing application which stores local data? If you have, you might have faced the problem of managing a local database instance, perhaps a NoSQL embedded alternative or something painful related to text file formats.

Personally, I used to use [LiteDB] which is a NoSQL version which in v4 paired very nicely with F# thanks to Zaid's lovely [LiteDB.FSharp] library. Sadly, when v5 showed up, a lot of the F# niceties were lost given how the API was changed and v5 was not very F# friendly. You can still use it of course but you fall back to more unsafe F# code which is not ideal.

So the next natural candidate is SQLite, which is a very popular choice for local databases and also a safe bet for F# developers as there's a bunch of F# libraries that support SQL very well. My issue in general with SQL databases is having to manage the schema and migrations prior to get hacking around with your app.

For applications that have a remote database that's fine, you can use a tool like Migrondi, Flyway, Liquibase, etc. to manage your migrations and you're good to go but, for local databases how do you manage your schema and migrations?.

I'm sure there are solutions out there that I'm not aware of, but it was a big driver for me to add library support to Migrondi, so I could use it to manage local databases as well.

So let's see how we can use Migrondi to manage a local SQLite database. in your project!

For this we'll be looking at an example I already wrote for you!

https://github.com/AngelMunoz/Siquelin

Which for the intended purposes, I'd recommend you to set github at the tag `01-exploring-a-foreign-codebase` or clone it and checkout to that tag as that will be the code this post will be talking about.

> If you want to learn a little bit more on "How to explore a foreign codebase" I'd recommend you to check out this post: [Exploring an F# foreign codebase] which contains a few tips and tricks to get you started with unknown to you F# codebases.

In our particular case we're only interested in the `Types.fs`, `Migrations.fs`, `Program.fs` and the `Commands.Hidden` in `Commands.fs` module.

> **_NOTE_**: `dotnet add package Migrondi.Core --prerelease` to add the new bits to your project in case you want to try this yourself.

Let's start with the `Migrations.fs` file, which contains a few helper functions with `Migrondi.Core` to manage our migrations.

```fsharp
namespace Siquelin.Migrations

module Runner =
  open Migrondi.Core
  open Microsoft.Extensions.Logging

  let internal getMigrondi rootDir config =

    let migrondi = Migrondi.MigrondiFactory(config, rootDir)
    migrondi.Initialize()
    migrondi

  let runMigrations (logger: ILogger, migrondi: IMigrondi) = ...

  let addNewMigration (logger: ILogger, migrondi: IMigrondi) (name: string) = ...
```

If you don't want to customize Migrondi in any way (meaning that you want to use Migrondi just like you would use the CLI tool), then you can use `MigrondiFactory` to create a new instance of Migrondi and then call `Initialize` to load the migrations from the root directory.

The `MigrondiFactory` function takes a `MigrondiConfig` item and a `string` which is the root directory where the migrations are located.

The run migrations function is not particularly complex, it simply calls `MigrationsList()` from an `IMigrondi` instance and then filters the pending migrations. If there are any pending migrations, it calls `RunUp` to apply them and logs the applied migrations.

```fsharp
let runMigrations (logger: ILogger, migrondi: IMigrondi) =
    let hasPending =
      migrondi.MigrationsList()
      |> Seq.choose(fun m ->
        match m with
        | Pending p -> Some p
        | _ -> None
      )
      |> Seq.length > 0

    let applied: seq<MigrationRecord> =
      if hasPending then migrondi.RunUp() else Seq.empty

    for migration in applied do
      logger.LogInformation("Applied migration {}", migration.name)
```

The `addNewMigration` function is also quite short, it mainly calls Migrondi to generate a new migration file.

```fsharp
let addNewMigration (logger: ILogger, migrondi: IMigrondi) (name: string) =
    let migration = migrondi.RunNew name
    logger.LogInformation("Generated migration {}", migration.name)
```

For our simple application, we don't need anything else, once we're able to access an `IMigrondi` instance we can run migrations and add new ones.

Now, let's take a look at the `Types.fs` file, which contains the types that we'll be using to interact with the database.

```fsharp
type WorkDay = {
  id: int
  date: DateOnly
}

type ShiftItem = {
  id: int
  workDayId: int
  start: TimeOnly
  finish: TimeOnly
}
```

Our CLI app focuses on being able to save "shift items" for a given "work day". and basically for any work day we can have multiple shift items.

Which should be listable when required.

> **_NOTE_**: Keep in mind that this example is for a use case where the user is generating the data, but if you're generating the data yourself for configuration, or for pre/post processing, you can follow this approach as well!

Without diving into much detail, our app has a `new-migration` hidden command, it is callable from the CLI and is enabled only in `DEBUG` builds.

When we're developing our app we are able to call the following command to generate a new migration file to manage our SQLite database.

```
dotnet run -- new-migration initial-types
```

This will generate a new migration file in the `migrations` directory which may look like this `./migrations/initial-types_1708665113196.sql`

Our file content is about creating the tables for our types.

```sql
-- MIGRONDI:NAME=initial-types_1708665113196.sql
-- MIGRONDI:TIMESTAMP=1708665113196
-- ---------- MIGRONDI:UP ----------

create table work_days(
    id integer primary key autoincrement,
    date text not null,
    unique(date)
);

create table shift_items(
    id integer primary key autoincrement,
    work_day_id integer not null,
    start_time text not null,
    end_time text not null,

    foreign key(work_day_id) references work_days(id)
      ON DELETE CASCADE
);

-- ---------- MIGRONDI:DOWN ----------
```

In this file we have two sections

- `MIGRONDI:UP` which contains the SQL to create the tables.
- `MIGRONDI:DOWN` which is empty, but it would contain the SQL to drop the tables.

The golden rule in Migrondi is to not remove anything from `MIGRONDI:UP` and above and not to remove the particular line where `MIGRONDI:DOWN` is located.

The first part is basically metadata for migrondi to know what to do with the file, the content between `MIGRONDI:UP` and `MIGRONDI:DOWN` is the SQL that Migrondi will execute when running the migration, and below `MIGRONDI:DOWN` is the SQL that Migrondi will execute when rolling back the migration.

Many folks don't like rolling back, so feel free to leave it empty, it is your choice.

In `Program.fs` theres a single line that is important for us to be able to use Migrondi.

```fsharp
[<EntryPoint>]
let main argv =

  let env = Env.getEnv()

  // run this at the start of the app regardless of the commands
  // this will ensure that the database is up to date
  Runner.runMigrations(env.logger, env.migrondi)

  ... more content ...
```

When the app starts, it will run the migrations, and then it will continue with the rest of the app.

And that's it! As long as we tell Migrondi where our migrations are and where is the database, we can use Migrondi to manage our local SQLite database.

Let's check the `Env.fs` file to see what are the particular values we're picking up from the environment.

```fsharp
let private loggerFactory = ...

let private getEnvLocations () =
  // The appData let binding, refers to where are we going to store our database
  // in the user's machine. We're using the LocalApplicationData folder to avoid
  // polluting the user's home directory.
  // ~/.local/share/siquelin in *nix systems
  // C:\Users\<USER>\AppData\Local\siquelin in Windows
  let appData =
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "siquelin"
    )

  // The full path to our database file including it's name.
  let dbPath = Path.Combine(appData, "siquelin.db")

  // appDir is the directory where the app is located, we use this to locate the migrations
  // in dev time we will be invoking these migrations from the project directory
  // so we just pick the current directory
  // in release time we will have our migrations next to our assembly
  // so we want to use that instead of the current directory
  let appDir =
#if DEBUG
    // At dev time we want to be in the project directory
    // as they will be copied to the output directory when building for release
    // ~/projects/Siquelin
    Environment.CurrentDirectory
#else
    // where is our assembly located
    // migrations will be in the same directory (e.g. {AppContext.BaseDirectory}/migrations/*.sql)
    AppContext.BaseDirectory
#endif

  {
    appDirectory = appDir
    appDataDirectory = appData
    databasePath = dbPath
  }

let getEnv () : Types.Env.AppEnv =
  let logger = loggerFactory.Value.CreateLogger("Siquelin")

  // get the locations for the app as we saw in the previous function
  let locations = getEnvLocations()

  // create the database directory if it doesn't exist
  // to avoid exceptions when trying to create the database file
  Path.GetDirectoryName locations.databasePath
  |> Directory.CreateDirectory
  |> ignore

  let config = {
    // use the default configuration but override the connection string
    MigrondiConfig.Default with
        connection = $"Data Source={locations.databasePath};"
  }

  // Get an instance of migrondi, the migrondi instance will use the app directory
  // as the root directory for the migrations, and the connection string to connect to the database
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

From here on you can start running the other commands

```
dotnet run -- item 10:00 11:00
dotnet run -- item 11:30 12:30
```

And then list the items

```
dotnet run -- list-items
info: Siquelin[0]
      Listing shift items for: Saturday, February 24, 2024
info: Siquelin[0]
      Shift item: 10:00 - 11:00
info: Siquelin[0]
      Shift item: 11:30 - 12:30
```

Before deploying this to production, we need to be sure that our migrations will be available for the app to run, so we will a couple of lines in our `Siquelin.fsproj` file to include the migrations in the output directory.

```xml
<ItemGroup>
    <None Include="./migrations/*">
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

This will ensure that our migrations are copied to the output directory when we build the app, and live next to the assembly.

Yay! We have a local database, we're managing it with Migrondi!

However, the eagle eyed reader might have noticed that we can't identify the shift items, they are just a bunch of times, and our users might want to know which shift item is which.

The bad news is that we've already shipped this and our users are already logging their shift items... so we can't simply blow away the database and start from scratch the next time we run the app, once again Migrondi comes to the rescue!

We can add a new migration to add a new column in the existing tables, and then we can update the data to fill in the new column.

```
dotnet run -- new-migration add-name-column
```

This will create something like `add-name-column_1708811304117.sql`, then we can add the following migration.

```sql
-- MIGRONDI:NAME=add-name-column_1708811304117.sql
-- MIGRONDI:TIMESTAMP=1708811304117
-- ---------- MIGRONDI:UP ----------

-- We're adding new columns to the database to store the name of the item.
-- and attempt to backfill

alter table work_days add column item_name text;

UPDATE work_days SET item_name = CAST(id AS TEXT) where item_name is null;

alter table shift_items add column item_name text;

UPDATE shift_items SET item_name = CAST(id AS TEXT) where item_name is null;

-- ---------- MIGRONDI:DOWN ----------
```

After adding that migration, we can update our types

```fsharp
type WorkDay = {
  id: int
  date: DateOnly
  name: string option
}

type ShiftItem = {
  id: int
  workDayId: int
  name: string
  start: TimeOnly
  finish: TimeOnly
}
```

and the rest of the code updates.
The next time we run the app, the migrations will be applied and the new columns will be added to the database, and the data will be updated to fill in the new columns.

Let's move forward to the [02-siquelin-upsert-existing] tag in the repository to see the updated implementation.

In regards to our migration management, nothing changed other than the migration we added, and the types we updated. the rest were application and business logic changes.

Running the new commands

```
dotnet run -- item 17:55 20:15 "watch netflix"
dotnet run -- item 17:00 17:30 "Bathroom"
```

And then listing the items

```
dotnet run -- list-items
info: Siquelin[0]
      Listing shift items for: Saturday, February 24, 2024
info: Siquelin[0]
      Shift item: 'watch netflix': 17:55 - 20:15
info: Siquelin[0]
      Shift item: 'Bathroom': 17:00 - 17:30
```

Even if our users typed the wrong command (as it is a breaking change for them because a new required argument as added to the command line interface), the migrations will be applied and the data will be updated to fill in the new columns.

This is done transparently to the users, and we can be sure that the database is up to date.

That's how we can use Migrondi to manage a local SQLite database in our F# application. There are of course alternative approaches to this and hopefully Migrondi is up to the task for your use case. If not, I welcome you to open an issue in the repository for your potential contribution or discussions related to what you're trying to do.

Feedback and user testing is always welcome, so if you have any thoughts or questions, feel free to reach out to me on twitter or open an issue in the repository.

[LiteDB]: https://www.litedb.org/
[LiteDB.FSharp]: https://github.com/Zaid-Ajaj/LiteDB.FSharp
[Exploring an F# foreign codebase]: https://dev.to/tunaxor/exploring-a-foreign-f-codebase-3og
[02-siquelin-upsert-existing]: https://github.com/AngelMunoz/Siquelin/tree/02-siquelin-upsert-existing
