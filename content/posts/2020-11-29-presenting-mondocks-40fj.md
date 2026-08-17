---
title: Presenting Mondocks
subtitle: ~
categories: mongodb,fsharp,showdev,dotnet
abstract: I'm a person that tries to ease other people's lives, if not at least my own. One of the things I fou...
date: 2020-11-29
language: en
---

[Driver]: https://docs.mongodb.com/drivers/csharp
[Zaid's Npgsql.FSharp]: https://github.com/Zaid-Ajaj/Npgsql.FSharp
[Npgsql.FSharp.Analyzers]: https://github.com/Zaid-Ajaj/Npgsql.FSharp.Analyzer
[Mongo Command]: https://docs.mongodb.com/manual/reference/command/
[MongoDB's Extended Json Spec]: https://docs.mongodb.com/manual/reference/mongodb-extended-json/
[Computation Expressions]: https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions
[samples]: https://github.com/AngelMunoz/Mondocks/tree/master/samples
[Object Spread]: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/Spread_syntax#Spread_in_object_literals

I'm a person that tries to ease other people's lives, if not at least my own. One of the things I found hard to do when I started moving to F# was the "lack of MongoDB support" which is up to some point is false, MongoDB provides it's own [Driver] which for all intends and purposes it's focused in C#'s OOP style it is quite similar to an ODM (Object Document Mapper) and while it's use is quite idiomatic for C#, using it from F# sometimes can be quite clunky, what I wanted when I was learning was to focus on learning F# and not focus on database schemas, or how to make the driver work in a foreign language... sadly there was not a lot I could do for it, so I moved on to SQL solutions like [Zaid's Npgsql.FSharp] library which is an amazing piece of tech if you include the [Npgsql.FSharp.Analyzers] 100% recommended. Today I finally feel able to contribute back something that can be useful for those node developers who are looking to learn F# next

{% github AngelMunoz/Mondocks %}

***Mondocks*** is a [Mongo Command] builder library (like a SQL builder library but for MongoDB) which focuses on producing JSON that is compatible with [MongoDB's Extended Json Spec], it provides a set of helpers called [Computation Expressions] that create a Domain Specific Language that you can use to keep using the queries and objects that you may know how to handle already.
But enough text, let's see some code (don't forget to check the [samples] as well).

> Installing this library is quite simple
> `dotnet install Mondocks`


{% gist https://gist.github.com/AngelMunoz/35cf2bc439da9969664f9987f7109ee3 file=read.fsx %}

> **NOTE**: you can run that with F# Interactive, download the file with the name `find.fsx` and run `dotnet fsi ./find.fsx`
> ***NOTE***: you also need to use the MongoDB.Driver library to execute these commands since Mondocks only produces JSON
> ```fsharp
>   new MongoClient(URL)
>         .GetDatabase(dbname)
>         .RunCommand(JsonCommand(mycommand))
> ```
> Which also means you can use it side by side with the usual MongoDB.Driver's API so it's a win-win you're not sacrificing anything 😁


In the sample above we're leveraging anonymous records from F# to create MongoDB queries since they behave pretty much like Javascript Objects we can even create new definitions from existing anonymous records similar to [Object Spread] in javascript (check `filterbyNameAndId`).

Let's move up to the update, updates are quite simple as well

{% gist https://gist.github.com/AngelMunoz/35cf2bc439da9969664f9987f7109ee3 file=update.fsx %}

> ***NOTE***: there are certain BSON types that have to be represented in some specific ways to conform to the MongoDB JSON extended spec in this example you can see ```{|``$date``: .... |}``` for more info check [here](https://docs.mongodb.com/manual/reference/mongodb-extended-json/#example)

I think that shows a little bit of how this library works and what you can expect from it.

You can check the [samples] to see how you can do things like count, distinct, delete, find, index creation, findAndModify, etc, etc. and also here's a small F# Restful API which uses this library

{% github AngelMunoz/Frest %}

If you know a MEAN Stack developer who could use a new language like F# I encourage you to show this library to them, perhaps that sparks interest. If you tried mongo in F# before but you didn't like it perhaps this is the time to give it a check again.


If you find a bug or have suggestions, feel free to raise a couple of issues or ping me on Twitter.

As always I hope you're having a great day and feel free to drop some comments below
