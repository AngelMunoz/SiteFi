---
title: Is that a Giraffe?
subtitle: ~
categories: dotnet,fsharp,giraffe
abstract: Well I've been far away these last months, quite busy at work also looking for ...
date: 2018-12-02
language: en
---

Well I've been far away these last months, quite busy at work also looking for something to share with the _Dev.to_ community.

I have been playing around too much with _.net_ lately for my personal projects as well as trying to finally figure it out so I can eventually add it to my `I can do that sh*t` belt. So far some frontend js  and node are in that belt. I'd like to extend it a little bit more.

I hope to share more of the experiences with writing UWP apps in C# vs Js in later posts but for now I'll take a look at [Giraffe](https://github.com/giraffe-fsharp/Giraffe).

(you can skip the first two sections if you only want to see the code)

### First of all F#?
Yeah I have tried dozens of times before to figure out how to actually do something meaningful in F#, but so far my attempts were kind of frustrated by me not understanding FP at all.

The fact that often when you look at FP resources you get to tend these `mystical-hocus-pocus-magical` applications/buzz words where you only see these one liners very often and a ton of functions that seem to have depths within other functions and such didn't help either. Some discussions in internetland are quite authoritative on how you should do FP in the first place.

I learned from OOP so while I kind of find the use cases for small chunks of functions, I have never grasped totally the idea of getting these chunks together to do a big thing and it's not bad actually I'm just bad at grasping these FP resources because I didn't really had something to apply these things.


## What's Giraffe?
[Giraffe](https://github.com/giraffe-fsharp/Giraffe) is somewhat a library/framework built on top of asp.net core for functional programmers, so yeah you have your kestrel server, access to common asp.net dependency injection, middleware and such. Like the author said in one of his [blogs](https://dusted.codes/functional-aspnet-core)

>Ideally as an F# developer I would like to replace the object oriented MVC framework with a functional equivalent, but keep the rest of ASP.NET Core's offering at the same time.

It was the idea when he began to work on it and he did it really well with the help of the community around Giraffe.

that being said...

### Something to share

{% github AngelMunoz/Giraffarig %}

Now please take in mind that I'm no F# ninja not even close I kind of grasped the concepts that allowed me to do that skeleton by understanding other technologies like express(Js), asp.net core (C# version), Flask and others
so my Functional Fu is not top notch however I really like to highlight some of the concepts that allowed me to do something in F#.

The basic block for routes in Giraffe are these `HttpHandler` functions
```fsharp
let routeHandler:HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        return! Successful.OK "Hello World!" next ctx
    }
```
now I have done stuff in express for a living for some time before and this function seemed familiar to me (except for lang + syntax  y'know)

```js
function (req, res, next) {
  res.send('Hello World!')
}
```
Hah... intresting... after reading some of the Giraffe docs I kind of understood the deal, these functions were like express's middleware!

You can just use multiple small functions to pass along the request's pipeline (or however you may want to call it)

So I decided to pull in the MongoDB's .net driver and a login/signup as well as a user get route.


```fsharp
// Program.fs
let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> text "Hello world"
                route "/api/users" >=> authorize >=> getUsers
            ]
        POST >=>
            choose [
                route "/api/auth/login" >=> login
                route "/api/auth/signup" >=> signup
            ]
        setStatusCode 404 >=> text "Not Found" ]
```
so this was my router, two set of **GET** and **POST** routes
you can see that better in the repository but the deal here is kind of pattern you might spot
```fsharp
route "/" >=> text "Hello world"
```
that `>=>` symbol is an intresting function, you might learn more about it [here](https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md#compose-), but basically 

>The compose combinator combines two HttpHandler functions into one.

In that example, we use the function route, that takes one argument, then apply the result to the text function which takes a parameter too, in that way we can create a simple route which will deliver us a `Hello World`
you can see it better on the getUsers endpoint

```fsharp
route "/api/users" >=> authorize >=> getUsers
```
here we take a route, then an authorize function which will challenge the request and if successful, it will continue with the `getUsers` function.

Now, before the getUsers function, let me share that using the mongo driver from F# is not hard at all

```fsharp
module Db
open MongoDB.Driver
open System

[<CLIMutable>]
type User = 
    { Id: BsonObjectId;  
      Name: string; 
      LastName: string; 
      Role: string; 
      Email: string; 
      Password: string; }
[<CLIMutable>]
type EndpointResult<'T> = { Count: int64; List: List<'T>; }

let connectionString = Environment.GetEnvironmentVariable  "MONGO_URL"

[<Literal>]
let DatabaseName = "fs_database_name"
let client = new MongoClient(connectionString)
let db = client.GetDatabase(DatabaseName)
let UserCollection = db.GetCollection<User> "fs_users"
```

quite straight forward right? we will use that `UserCollection` later on to do our user queries

```fsharp
let getUsers:HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        // a small helper function that gets limit/offset from query strings
        let (limit, offset) = getPagination ctx
        // no special filters, just give me that count
        let count = UserCollection.CountDocuments(Builders.Filter.Empty)
        let users = 
            UserCollection.Find(Builders.Filter.Empty)
                .Skip(Nullable(offset))
                .Limit(Nullable(limit))
                .ToEnumerable()
            // we have now our users let's get rid of sensitive information
            |> Seq.map(fun user -> userToDTO(user))
            |> Seq.toList
        // return the users plus the next and context so it can be composed
        return! Successful.OK ({ Count = count; List = users }) next ctx
    }
```

Aaaand... just as that, boom you have a paginable endpoint! kind of similar on what you would do in express.

there is also `Jwt authentication` in that repo so you might want to take a look to that too, jwt authentication is done with `asp.net core` so that is a beneffit from Giraffe in the end it's just asp.net core!


### Conclusions
With that sample up and running I can see myself using F# more often (Finally!) and I'm glad that I've used express before otherwise this could have been another failed attempt on F# for me :)
also I wanted to mention that I've tried to use asp.net core alone too with F#, but it was too messy and things didn't really worked well for me I kind of felt I was out of place when comparing it when using it with C#

Thanks for reading my none sense! I'm sorry if I wandered too much in some parts.

As always feel free to share feedback/comments down below!
