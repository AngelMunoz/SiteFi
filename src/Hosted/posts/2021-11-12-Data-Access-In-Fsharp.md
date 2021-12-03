---
title: Data Access in Fsharp
subtitle: ~
categories: fsharp,sql,mongodb,dotnet,simplethingsfsharp
abstract: Need to access a datasource like MySQL, MSSQL or PostgreSQL F# got you covered!...
date: 2021-11-12
language: en
---

[dapper.fsharp]: https://github.com/Dzoukr/Dapper.FSharp
[donald]: https://github.com/pimbrouwers/Donald
[mondocks]: https://github.com/AngelMunoz/Mondocks
[dapper]: https://github.com/DapperLib/Dapper
[dbeaver]: https://dbeaver.io/

# Simple things in F#

Hello there, this is the 6th entry in Simple Things F#.

Today we'll talk about Database access. Databases are something we have to use very often after all it is where we store our data most common databases we use are

- SQL Databases
- NoSQL Databases

I won't dive deep into the differences between them, rather than that I will focus on how you can access these databases from F# code.

> As in previous entries I'll be using F# scripts which can be executed with the .NET CLI that comes in the .NET SDK which you can get from here: https://get.dot.net

Let's review our options for today

- [Dapper.FSharp] - Dapper Wrapper (i.e. anything that MSSQL, PostgreSQL, MySQL supports)
- [Donald] - ADO.NET wapper (i.e MSSQL, PostgreSQL, SQLite, MySQL, and others)
- [Mondocks] - MongoDB DSL for the MongoDB .NET Driver

We will not complicate things and work with simple DB Schemas, and we will be using PostgreSQL since it's a pretty common database used around the world, but please keep in mind these solutions (and others that I will share at the end) work with MSSQL and MySQL as well.

> If you have docker installed, spin up a postgresql instance
>
> ```
> docker run -d \
>  --name my-instance-name \
>  -e POSTGRES_PASSWORD=Admin123 \
>  -e POSTGRES_USER=admin
>  -p 5432:5432 \
>  postgres:alpine
> ```

This will be our little schema, nothing fancy something just to get started with some F# code.

```sql
create table authors(
    id uuid primary key,
    name varchar(100),
    email varchar(100),
    twitter_handle varchar(100) null
);
create table posts(
    id uuid primary key,
    title varchar(140) not null,
    content text not null,
    authorId uuid references authors(id)
);
```

> You can create the PostgreSQL database using any DB manager you already know. In case you don't have anything available you can use [dbeaver].

Once you have your database cretated and have the schema in place let's begin with the cool stuff

## Dapper.FSharp

If you like ORMs this is going to be a library for you, given that you can map records to tables so using them is seamless, also Dapper.FSharp adds a couple of F# types to make your life easier.

Let's check what are our F# records going to be:

```fsharp
type Author =
    { id: Guid
      name: string
      email: string
      twitter_handle: string option }

type Post =
    { id: Guid
      title: string
      content: string
      authorId: Guid }
```

Here we just did a 1-1 record translation, more complex schemas may differ from what your application is using you can use DTO's or anonymous records to work with these differences.

```fsharp
// From F# 5.0 + you can "require" NuGet packages in F# scripts
#r "nuget: Npgsql"
#r "nuget: Dapper.FSharp"

open System
open Npgsql
open Dapper.FSharp
open Dapper.FSharp.PostgreSQL
// register our optional F# types
OptionTypes.register ()

type Author =
    { id: Guid
      name: string
      email: string
      twitter_handle: string option }

// register our tables
let authorTable =
    // we can use this function to match tables
    // with different names to our record definitions
    table'<Author> "authors" |> inSchema "public"

let connstring =
    "Host=192.168.100.5;Username=admin;Password=Admin123;Database=simple_fsharp"
/// In normal circunstances you would write
/// `use! conn = new NpgsqlConnection(connString)`
/// but inside F# scripts we're not allowed for top declarations like this,
/// so we use let instead
let conn = new NpgsqlConnection(connstring)

// Generate two different authors
// one with an optional handle to see how we can deal with null values
let authors =
    [ { id = Guid.NewGuid()
        name = "Angel D. Munoz"
        email = "some@email.com"
        twitter_handle = Some "angel_d_munoz" }
      { id = Guid.NewGuid()
        name = "Misterious Person"
        email = "mistery@email.com"
        twitter_handle = None } ]

// If you were to use ASP.NET core
// you would be running on a task or async method
task {
    /// the `!` here indicates that we will wait
    /// for the `InsertAsync` operation to finish
    let! result =
        // here's the Dapper.FSharp magical DSL
        insert {
            into authorTable
            values authors
        }
        |> conn.InsertAsync

    /// If all goes well you shoul'd see
    /// `Rows Affected: 2` in tour console
    printfn $"Rows Affected: %i{result}"

}
// we're inside a script hence why we need run it synchronously
// most of the time you don't need this
|> Async.AwaitTask
|> Async.RunSynchronously
```

> To Run this, copy this content into a file named `script.fsx` (or whatever name you prefer) and type:
>
> - `dotnet fsi script.fsx`

> If you get a message like "warning FS3511: This state machine is not statically compilable." don't worry it is being tracked in https://github.com/dotnet/fsharp/issues/12038

Cool! so far we have inserted two authors to our database from our mapping, now let's bring those folks back

```fsharp
#r "nuget: Dapper.FSharp"
#r "nuget: Npgsql"

open System
open Npgsql
open Dapper.FSharp
open Dapper.FSharp.PostgreSQL
// register our optional F# types
OptionTypes.register ()

type Author =
    { id: Guid
      name: string
      email: string
      twitter_handle: string option }

let authorTable =
    table'<Author> "authors" |> inSchema "public"

let connstring =
    "Host=192.168.100.5;Username=admin;Password=Admin123;Database=simple_fsharp"

let conn = new NpgsqlConnection(connstring)

task {
    let! allUsers =
        select {
            for author in authorTable do
                selectAll
        }
        |> conn.SelectAsync<Author>

    printfn "Names: "

    for user in allUsers do
        printfn $"\t%s{user.name}"

    let! usersWithTwitterHandle =
        select {
            for author in authorTable do
                where (author.twitter_handle <> None)
        }
        |> conn.SelectAsync<Author>

    printfn "Twitter Handles:"

    for user in usersWithTwitterHandle do
        // we use .Value because filter users whose handle is None
        printfn $"\t%s{user.twitter_handle.Value}"

}
// we're inside a script hence why we need run it synchronously
// most of the time you don't need this
|> Async.AwaitTask
|> Async.RunSynchronously
```

> To Run this, copy this content into a file named `script.fsx` (or whatever name you prefer) and type:
>
> - `dotnet fsi script.fsx`

you should see something like this:

```
Names:
  Angel D. Munoz
  Misterious Person
Twitter Handles:
  angel_d_munoz
```

Let's check the update code, which to be honest is pretty similar, what do we update though? Our Mysterious user doesn't have a twitter handle, so let's add one

```fsharp
#r "nuget: Dapper.FSharp"
#r "nuget: Npgsql"

open System
open Npgsql
open Dapper.FSharp
open Dapper.FSharp.PostgreSQL
// register our optional F# types
OptionTypes.register ()

type Author =
    { id: Guid
      name: string
      email: string
      twitter_handle: string option }

// register our tables
let authorTable =
    table'<Author> "authors" |> inSchema "public"

let connstring =
    "Host=192.168.100.5;Username=admin;Password=Admin123;Database=simple_fsharp"

let conn = new NpgsqlConnection(connstring)

task {
    let! noHandleUsers =
        select {
            for author in authorTable do
                where (author.twitter_handle = None)
        }
        |> conn.SelectAsync<Author>
    // let's try to get the first result from the result set
    match noHandleUsers |> Seq.tryHead with
    // if there is one, let's update it
    | Some user ->
        let user =
            // partially update the record of the user with
            // the F# record update syntax
            { user with twitter_handle = Some "mysterious_fsharper" }

        let! result =
            update {
                for author in authorTable do
                    set user
                    where (author.id = user.id)
            }
            |> conn.UpdateAsync

        printfn $"Users updated: %i{result}"
    // if we have run this script, our result set will be empty
    | None -> printfn "No Users Without handle were Found"

}
// we're inside a script hence why we need run it synchronously
// most of the time you don't need this
|> Async.AwaitTask
|> Async.RunSynchronously
```

> To Run this, copy this content into a file named `script.fsx` (or whatever name you prefer) and type:
>
> - `dotnet fsi script.fsx`

After the script is run, we should see

> Users updated: 1

And if we run it for a second time we'll see

> No Users Without handle were Found

And if we run the "select" script we'll should see the `mysterious_fsharper` handle

```
Twitter Handles:
  angel_d_munoz
  mysterious_fsharper
```

So far, things have been quite straight forward, but what if you don't like the ORM style? If you like to write SQL like a real programmer (_**Which of course, it's sarcasm**_.) or you simply like to write your SQL queries, let's then take a look at [Donald].

## Donald

Donald can help us to have a 1-1 mapping with our models just like [Dapper.FSharp] but it needs help from our side, it is quite flexible in some aspects and tedious in others let's see how can we add these helpers.

For the Donald scripts we will modify our `Author` and `Post` records a little bit, we will add a static function called `DataReader` which will take an `IDataReader` and return the corresponding record

```fsharp
#r "nuget: Donald"

open System
open System.Data
open Donald

// Same Author model from before
type Author =
    { id: Guid
      name: string
      email: string
      twitter_handle: string option }
    // Add the DataReader
    static member DataReader(rd: IDataReader) : Author =
        // the reader has some functions that help us map
        // existing columns from the database and their
        // data type to our record, this can be really great
        // when you need to work on a schema you don't own
        { id = rd.ReadGuid "id"
          name = rd.ReadString "name"
          email = rd.ReadString "email"
          twitter_handle = rd.ReadStringOption "twitter_handle" }

// We do the same with the Post record
type Post =
    { id: Guid
      title: string
      content: string
      authorId: Guid }

    static member DataReader(rd: IDataReader) : Post =
        { id = rd.ReadGuid "id"
          title = rd.ReadString "title"
          content = rd.ReadString "content"
          authorId = rd.ReadGuid "authorId" }
```

> To Run this, copy this content into a file named `script.fsx` (or whatever name you prefer) and type:
>
> - `dotnet fsi script.fsx`

> There are more patterns you can follow rather than attaching the static function directly to the Record, you could have a `module Author = ...` which contains helper functions (like the data reader) but for simplicity we will attach it right there in the record.

[Donald] offers two syntax styles when it comes to creating and manipulating queries:

- Fluent Style

  The fluent style is an approach based on piping functions (i.e. using `|>`), this is similar

  ```fsharp
  let authorsFluent =
      conn
      |> Db.newCommand "SELECT * FROM authors WHERE twitter_handle <> @handle"
      |> Db.setParams [ "handle", SqlType.Null ]
      |> Db.query Author.DataReader

  ```

- Expression Style

  The Expression style, uses what in F# we call `Computation Expressions` which you already used with [Dapper.FSharp]! Here's the same previous query with the expression style

  ```fsharp
  let authorsExpression =
      dbCommand conn {
          cmdText "SELECT * FROM authors WHERE twitter_handle <> @handle"
          cmdParam [ "handle", SqlType.Null ]
      }
      |> Db.query Author.DataReader
  ```

They are slightly different and depending on your background one might feel more confortable than the other Feel free to choose the one you like the best, in my case I will continue the rest of the post with the Expression based one given that we already have some expression based code from [Dapper.FSharp]. Previously we added some authors, let'ts try to add Posts to those authors with [Donald].

```fsharp
#r "nuget: Npgsql"
#r "nuget: Donald"

open System
open Npgsql
open Donald
open System.Data

type Author =
    { id: Guid
      name: string
      email: string
      twitter_handle: string option }

    static member DataReader(rd: IDataReader) : Author =
        { id = rd.ReadGuid "id"
          name = rd.ReadString "name"
          email = rd.ReadString "email"
          twitter_handle = rd.ReadStringOption "twitter_handle" }

type Post =
    { id: Guid
      title: string
      content: string
      authorId: Guid }

    static member DataReader(rd: IDataReader) : Post =
        { id = rd.ReadGuid "id"
          title = rd.ReadString "title"
          content = rd.ReadString "content"
          authorId = rd.ReadGuid "authorId" }

let connstring =
    "Host=192.168.100.5;Username=admin;Password=Admin123;Database=simple_fsharp"
let conn = new NpgsqlConnection(connstring)

let authorsResult =
    // let's query all of the authors
    dbCommand conn { cmdText "SELECT * FROM authors" }
    |> Db.query Author.DataReader

let authors =
    // authorsResult is a DbResult<Author list>
    // that is a helper type
    // which help us successful and failed database operations
    match authorsResult with
    // if the operation was successful return the authors
    | Ok authors -> authors
    // otherwise print to the console what failed
    // and return an empty list
    | Error err ->
        printfn "%O" err
        List.empty

let insertCommand =
  """INSERT INTO posts(id, title, content, authorId)
     VALUES(@id, @title, @content, @authorId)"""
for author in authors do
    let postId = Guid.NewGuid()
    let result =
        dbCommand conn {
            cmdText insertCommand

            cmdParam [ "id", SqlType.Guid postId
                       "title", SqlType.String $"RandomPost: {postId}"
                       "content", SqlType.String "This is an extremely Long Post!..."
                       "authorId", SqlType.Guid author.id ]
        }
        |> Db.exec

    match result with
    | Ok () -> printfn $"Inserted post with id: {postId}"
    | Error err -> printfn $"Failed to insert post with id: {postId}... {err}"
```

> To Run this, copy this content into a file named `script.fsx` (or whatever name you prefer) and type:
>
> - `dotnet fsi script.fsx`

At this point we should have one post for each user in our database you can run it a couple times more to inser other posts if you wish, but I think these scripts show how you can do Database operations with these libraries

## Other Libraries

The F# ecosystem has several options that can appeal to developers of all kinds here are a few more that are worth looking at if you're looking for more alternatives

- RepoDB - https://github.com/mikependon/RepoDB

  RepoDB is a .NET micro ORM Database library that focuses on performance and has compatibility with many adapters

- Dusty Tables - https://github.com/Zaid-Ajaj/DustyTables

  Zaid is an F# OSS Beast, Dusty tables is a simple functional wrapper on top of the SqlClient ADO.NET adapter

- Npgsql.FSharp - https://github.com/Zaid-Ajaj/Npgsql.FSharp , https://github.com/Zaid-Ajaj/Npgsql.FSharp.Analyzer

  Zaid once again showing us the F# OSS spirit, this time with Npgsql.FSharp which is a PostgreSQL focused wrapper which has a SQL analyzer that can type verify your queries against your database at compile time!

- SQLHydra - https://github.com/JordanMarr/SqlHydra

  SQLHydra provides a CLI experience for record generation from an existing database Schema plus a SQL query builder similar to Dapper.FSharp, this is a more complete solution that works for Postgres, MSSQL and SQLite.

### The elephant in the Room...

EntityFramework has always been unfriendly to F# given how it relies on inheritance and mutability which isn't bad, it is the most used in C# after all but it provides some heavy friction with F#, with C#9's Records it might be on a better place but I haven't been able to try it nor the excitement to test it to be honest.

## Bonus! Mondocks

> Time for some shameless plug (with some shame)

I know, I Know... .NET is not the most friendly towards mongodb databases given how schemaless it is, and with F# it is even worse! there's a lot of cases where you can have missing properties (which is the same as having a null or even worse some times) but if you control the Database or for some reason you need to interact with MongoDatabases (perhaps because you're migrating from Node.js or similar) I took some time to work out on a DSL that is quite similar to Node's MongoDB query language.

The approach with Mondocks is somewhat different, with Mondocks ideally you want to work with anonymous records to shape your data and then once you have the information do the mapping to the corresponding Record or DTO

```fsharp
#r "nuget: Mondocks.Net"
#r "nuget: MongoDB.Driver"

open System
open MongoDB
open MongoDB.Driver

open Mondocks.Queries
open Mondocks.Types

type Author =
    { id: Guid
      name: string
      email: string
      twitter_handle: string option }

type Post =
    { id: Guid
      title: string
      content: string
      authorId: Guid }

let insertCmd =
    insert "authors" {
        documents [ { id = Guid.NewGuid()
                      name = "Angel D. Munoz"
                      email = "some@email.com"
                      twitter_handle = Some "angel_d_munoz" }
                    { id = Guid.NewGuid()
                      name = "Misterious Person"
                      email = "mistery@email.com"
                      twitter_handle = None } ]
    }

let client = MongoClient("mongodb://192.168.100.5/")

let database = client.GetDatabase("simple_fsharp")

let result =
    database.RunCommand<InsertResult>(JsonCommand insertCmd)

printfn $"Inserted: %i{result.n}"
```

> To Run this, copy this content into a file named `script.fsx` (or whatever name you prefer) and type:
>
> - `dotnet fsi script.fsx`

To do an update it's a similar case, we will fetch the author first then we will update it

```fsharp
#r "nuget: Mondocks.Net"
#r "nuget: MongoDB.Driver"

open System
open MongoDB.Bson
open MongoDB.Driver

open Mondocks.Queries
open Mondocks.Types

type Author =
    { _id: Guid
      name: string
      email: string
      twitter_handle: string }

let client = MongoClient("mongodb://192.168.100.5/")
let database = client.GetDatabase("simple_fsharp")


let findCmd =
    find "authors" {
        filter {| twitter_handle = "" |}
        limit 1
    }

let result =
    database.RunCommand<FindResult<Author>>(JsonCommand findCmd)
// check on the database result set if we have an author
match result.cursor.firstBatch |> Seq.tryHead with
| Some author ->
    let updateCmd =
        update "authors" {
            // query by author _id
            updates [ { q = {| _id = author._id |}
                        u =
                          { author with
                              // set the updated handle
                              twitter_handle = "mysterious_fsharper" }
                        multi = Some false
                        upsert = Some false
                        collation = None
                        arrayFilters = None
                        hint = None } ]
        }

    let result =
        database.RunCommand<UpdateResult>(JsonCommand updateCmd)

    printfn $"Updated: %i{result.n}"
| None -> printfn "No Author was found"
```

> To Run this, copy this content into a file named `script.fsx` (or whatever name you prefer) and type:
>
> - `dotnet fsi script.fsx`

You will also see that you lost a lot of safety doing these kinds of queries, given the nature of MongoDB it's hard to keep safety around it overall, however if you come from a dynamic runtime this DSL might feel a little bit more to what you're used to, there are some rough corners but I invite you to try it and log issues, if you're looking for an F# OSS project to dip your toes, it might be a great one :)

Also, you can use the usual MongoDB Driver as well you can use both side by side to be honest I made it in a way that doesn't require you to jump out from a standard .NET Driver experience.

# Closing Thoughts...

When it comes to SQL F# is a safe bet be it on the server, scripts and other environments F# can help you keep type safety accross your database and your application, there are plenty of alternatives for you to try and I'm pretty sure you'll find what fits best for you.

We'll catch ourselves on the next time!
