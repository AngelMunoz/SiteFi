---
title: Dealing with Workflows in F#
subtitle: ~
categories: fsharp,fsadvent,dotnet,simplethingsfsharp
abstract: Let's see how F# can help us to think about workflows and error handling...
date: 2021-12-01
language: en
---

[fstoolkit.errorhandling]: https://demystifyfp.gitbook.io/fstoolkit-errorhandling/
[result<success,error>]: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-resultmodule.html
[option<type>]: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-optionmodule.html
[file.exists is unreliable]: https://blog.paranoidcoding.com/2009/12/10/the-file-system-is-unpredictable.html
[computation expressions]: https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions

Although I was not planning on writing another blog post, here we are back again with single things in F#.

This time we will speak about _dealing with workflows in F#_, F# is a pretty concise language and one of it's strengths in my opinion, is that you can write programs that are often more correct.

> Note that when I say correct I don't mean bug free, I simply mean that the compiler is so helpful that it verifies that most of the code simply behaves as you write it. You are still able to encode logic bugs or in some cases stumble upon a real bug.

When you deal with workflows in F# there are often two main ways that I've come to find:

- [Option<Type>]
- [Result<Success,Error>]

### Option

If you are relatively new to F# you may have had some headaches trying to represent `null` given how common `null` is in other languages like C#, JavaScript, Java, or C. I do not have to try to convince you about how common is that an unexpected null is a reliable source of bugs. In F# null is not allowed by default (unless you have to inter-operate with C#/VB) so... how do you represent that? Well, the answer is simple: With Options!

```fsharp
let withNumber = Some 10

let withoutNumber = None
```

Options by themselves are pretty cool but in F# thanks to pattern matching they become really powerful

```fsharp
// The compiler will ensure you cover every match possible
match withNumber, withoutNumber with
| (Some num1, None) ->
  printfn $"Numbers: {num1} - "
| (None, Some num2 -> ) ->
  printfn $"Numbers: - {num2}"
| (Some num1, Some num2) ->
  printfn $"Numbers: {num1} - {num2}"
| None, None ->
  printfn $"Numbers: - "
```

Hmm, but that might not make a lot of sense, so let's try to read a file

```fsharp
open System.IO

let content = File.ReadAllText("./file.txt")
```

If you run that you will see a whooping `System.IO.FileNotFoundException: Could not find file './file.txt'.` or something similar which is not great to be honest, and [File.Exists is unreliable], how about making it work in a type safe manner? Sure!

```fsharp
open System
open System.IO

let getContentFromFile filePath =
  try
    let content = File.ReadAllText(filePath)
    if String.IsNullOrWhiteSpace content then
        None
    else
        Some content
  with _ -> None
let content = getContentFromFile "./file.txt"
printfn $"%A{content}"
```

This should print "_None_" to the console, if you pass a file path of an existing file it should print the contents.

Options are a good approach when you need to discard or do not care what happened with the data, the main point is just to know the answer to: "Is there any data?"

### Result

Well... Options are great, but _what if I want to know about what actually happened with my data?_

This is where results come in, they help you model workflows where errors are expected and even in some cases, "recover" from those errors.

```fsharp
open System

let randomValue = Random.Shared.Next(0,101)

let result =
  if randomValue % 2 = 0 then
    // return the successful result
    Ok randomValue
  else
    // return the error result
    Error $"Number is Odd: {randomValue}"

printfn $"%A{result}"

```

If you run this in the FSI (FSharp Interactive) you might get something like this

```
Ok 80
```

```
Error Number is Odd: 89
```

Hmm, how about also checking if the number is more than 10 but lower than 61?

```fsharp
open System

let randomValue = Random.Shared.Next(0,101)

let result =
  // validate if is odd first
  if randomValue % 2 = 0 then
    // validate if is in bounds
    if randomValue > 10 && randomValue < 61 then
      Ok randomValue
    else
      Error $"Number is Even but out of bounds: {randomValue}"
  else
    Error $"Number is Odd: {randomValue}"

printfn $"%A{result}"
```

When I ran these two times I got these results:

```
Error "Number is Even but out of bounds: 90"
```

```
Ok 50
```

That's great but those nested ifs... are not so funny are they? we could separate those validations in different functions

```fsharp
open System

let isOdd value =
  if value % 2 = 0 then
    Ok value
  else
    Error $"Number is Odd: {value}"

let isInBounds (value: int) =
  if value > 10 && value < 61 then
    Ok value
  else
    Error $"Number is out of bounds: {value}"

let randomValue = Random.Shared.Next(0,101)

let result = isOdd randomValue
let result2 = isInBounds randomValue
printfn $"%A{result} - %A{result2}"
```

Cool we now have two possible results like `Error "Number is Odd: 27" - Ok 27` or `Ok 68 - Error "Number is out of bounds: 68"` or even `Ok 20 - Ok 20`. If we change our function signature a bit, we will be able to pipe one result into another.

```fsharp
open System

let isOdd value =
  if value % 2 = 0 then
    Ok value
  else
    Error $"Number is Odd: {value}"

// rather than taking an int as the value, take the result itself
let isInBounds (value: Result<int, string>) =
  // use pattern matching to access the result value
  match value with
  | Ok value ->
    if value > 10 && value < 61 then
      Ok value
    else
      // return a new error
      Error $"Number is Even but out of bounds: {value}"
  // here you can choose to return the previous error
  // or to decide if you want to recover from it
  // Error message -> ... code ...
  | previousError -> previousError

let randomValue = Random.Shared.Next(0,101)

let result = isOdd randomValue |> isInBounds
printfn $"%A{result}"
```

So far we've been using strings for errors but can we make it better?

For sure, we can define a discriminated union that describes these situations

```fsharp
open System

let isOdd value =
  if value % 2 = 0 then
    Ok value
  else
    Error (OddNumber value)

// rather than taking an int as the value, take the result itself
let isInBounds (value: Result<int, ValidationError>) =
    // use pattern matching to access the result value
    match value with
    | Ok value ->
      if value > 10 && value < 61 then
        Ok value
      else
        // return a new error
        Error (OutOfBounds value)
    // here you can choose to return the previous error
    // or to decide if you want to recover from it
    // Error message -> ... code ...
    | previousError -> previousError


let getValue() =
  let randomValue = Random.Shared.Next(0,101)
  // evaluate our random value
  let result = isOdd randomValue |> isInBounds
  match result with
  | Ok value -> $"Value is: {value}"
  | Error (OddNumber value) -> $"Number is Odd: {value}"
  | Error (OutOfBounds value) -> $"Number is Even but out of bounds: {value}"

printfn $"{getValue()}"
```

After a couple runs you will get the three possible results. Hopefully this sheds some light on how and why

#### Results vs Exceptions

Let's talk a bit about something that I've seen happen before which is replacing exceptions with results (I have been guilty as well in some cases). In my head, exceptions are for abnormal events on your program, something that is not part of the domain you're working on.

As an example on a student management system:

- Not being able to reach a third party server.
- Grade a student which is not enrolled on the course or it is enrolled on a different one.

The first one is related to the environment your program runs on, if the network goes down it's not an error of your application it's precisely an exceptional event that might need to be handled outside your application.

The second one is related to what your system should do which is: _manage students_.

This doesn't mean that you can't use Results with exceptions, in some cases you actually know something might throw an exception and you want to handle that. Let's think about our safe file reader function, currently it just tells us if there was something there or not it doesn't tell us if there was actually an exception, neither we know if the file was empty. With a few changes we can have that.

```fsharp
open System
open System.IO

type ReaderError =
  | EmptyFile
  | FileNotFound of providedPath: string

let getContentFromFile filePath =
  try
    let content = File.ReadAllText(filePath)
    if String.IsNullOrWhiteSpace content then
      Error EmptyFile
    else
      Ok content
  with
  // as part of our requirements we know the file may exist or not
  | :? FileNotFoundException as ex -> Error (FileNotFound filePath)
  // since other exceptions are not part of our requirements
  // we leave them untouched
  | ex -> reraise()
let content = getContentFromFile "/file.txt"

match content with
| Ok content -> printfn "%s" content
| Error EmptyFile -> printfn "The file was empty"
| Error (FileNotFound path) -> printfn $"The file was not found at {path}"
```

## [FsToolkit.ErrorHandling]

For the most part we now have an idea what are the uses of _Options_ and _Results_, once thing that may happen often (hopefully you noticed a hint about it) is when you have nested operations involving Option and Results, you start going in what I call (because I must have seen it somewhere else) the stair of despair

```fsharp

match result with
| Ok username ->
  match queryDB username with
  | Ok user ->
    match doOperation parameter user with
    | Ok () ->  printfn "Success"
    | Error OperationFailed -> $"Error %A{err}"
    | Error PreconditionFailed -> eprintfn $"Error %A{err}"
  | Error NotFound -> eprintfn $"Error %A{err}"
  | Error UnreachableDB ->
    match queryDB username with
    | Ok user ->
      match doOperation parameter user with
      | Ok () ->  printfn "Success"
      | Error OperationFailed -> $"Error %A{err}"
      | Error PreconditionFailed -> eprintfn $"Error %A{err}"
    | Error NotFound -> eprintfn $"Error %A{err}"
    | Error UnreachableDB -> eprintfn $"Error %A{err}"
| Error UsernameNotValid -> eprintfn $"Error %A{err}"
```

As we saw before could fix them a little bit by creating functions

```fsharp
// declare the errors we might expect
type OperationError =
  | OperationFailed
  | PreconditionFailed
  | NotFound
  | UnreachableDB
  | UsernameNotValid

// get a Result<string, OperationError>
// from somewhere
let getUsername (): Result<string, OperationError> = // ... operation ...

let queryDB (username: Result<string, OperationError>): Result<User, OperationError> =
  match username with
  // simulate a database query
  | Ok username -> database.query username
  | error -> error

let doOperation parameter (user: Result<User, QueryError>): Result<_string_, OperationError> =
  match user with
  // simulate a database query
  | Ok user -> users.updateParameter parameter user
  | error -> error

let operationResult =
  // get the username result
  let result =  getUsername()
  let operationResult =
    result
    |> queryDB
    |> doOperation "Some parameter"
  match operationResult with
  // check if we can re-try in case the DB
  // was not available
  | UnreachableDB ->
    result
    |> queryDB
    |> doOperation "Some parameter"
  // otherwise return the result either success or error
  | result -> result

printfn $"%A{operationResult}"
```

While that looks quite better, it still falls short some times, in this case the code is very simplistic, but we tend to face code/issues with more complexity. That's where _**FsToolkit.ErrorHandling**_ shines, FsToolkit.ErrorHandling provides a set of [Computation Expressions] that in turn give us as well a great DSL to work with options and Results, the example above would be reworked to something like this

```fsharp

// declare the errors we might expect
type OperationError =
  | OperationFailed
  | PreconditionFailed
  | NotFound
  | UnreachableDB
  | UsernameNotValid

// get a Result<string, OperationError> from somewhere
let getUsername () : Result<string, OperationError> = // ... operation ...

// no need to take results as inputs the outputs remain the same
let queryDB (username: string) : Result<User, OperationError> =
  // simulate a database query
  database.query username

// no need to take results as inputs the outputs remain the same
let doOperation parameter (user: User) : Result<_, OperationError> =
  // simulate a database query
  users.updateParameter parameter user

let operationResult =
  result {
    // bind, or ensure that username is indeed the success case
    // which is a string
    let! username = getUsername ()

    // bind, or ensure that the user is indeed a User
    let! user =
      match queryDB username with
      // retry or return the existing result
      | Error UnreachableDB -> queryDB username
      | opResult -> opResult
    // ensure we return the result of the operation
    // we use `return!` because doOperation returns a Result
    // so we need to return and bind it, in another words
    // ensure the value is a success case and return the result
    return! doOperation "someUsername" user
  }
printfn $"%A{operationResult}"
```

That looks simpler right? Hopefully it looks, what _FsToolkit.ErrorHandling_ is doing here is letting us focus on the "**happy path**" of our operations without worrying about nesting and do the _stair of despair_ with all of the pattern matching we did on the first example, also look at the "re-try" operation, we used pattern matching and but rather than nest our way to the end we just operated on the success case via `let! user =`.

If at any point `let! username`, `let! user` or `return! doOperation` fail or have an error, the `result` computation expression (CE) will shortcut and just return the error case.

If we actually want to recover from errors we need to handle them with the helper functions inside the Result module. Let's go back to the odd/in-range validations example

```fsharp
#r "nuget: FsToolkit.ErrorHandling"

open System
open FsToolkit.ErrorHandling

type ValidationError =
  // let's add two new errors
  | ParseIntFailure of string
  | ParseFloatFailure of string
  // we'll keep the past ones
  | OddNumber of int
  | OutOfBounds of int

let randomValue () =
  Random.Shared.NextDouble() * 100. |> string

let isOdd value =
  if value % 2 = 0 then
    Ok value
  else
    Error(OddNumber value)

let isInBounds value =
  if value > 10 && value < 61 then
    Ok value
  else
    Error(OutOfBounds value)

let tryParseInt (value: string) =
  try
    Ok(value |> int)
  with
  | _ -> Error(ParseIntFailure value)

let tryParseFloat (value: string) =
  try
    Ok(value |> float |> int)
  with
  | _ -> Error(ParseFloatFailure value)

let recoverIntParsingFailure error =
  match error with
  // as long as our "recovery" matches the same
  // return type, we can safely use it
  | ParseIntFailure value -> tryParseFloat value
  | error -> Error error

let finalResult =
  result {
    let! value =
      tryParseInt (randomValue ())
      // BONUS: you can access an error in the middle of a validation chain
      // this can be useful for logging, telemetry, or even fire up events
      |> Result.teeError (fun error -> printfn "Failed to parse int %A" error)
      // Try to recover from parsing an int failure
      |> Result.orElseWith recoverIntParsingFailure
      // teeError (and tee) don not modify the value at all
      |> Result.teeError (fun error -> printfn "Failed to parse float %A" error)

    let! value = isOdd value
    return isInBounds value
  }
printfn $"%A{finalResult}"
```

In our sample above, we used as a bonus the `teeError` function which lets us access the error value (if any) to log our error to the console and we re-tried the operation with a float parsing rather than an int parsing, from the beginning we knew int was going to fail but I wanted to show you that, you should be able to model some processes nicely via result workflows. That includes modeling _unhappy_ paths that can be recoverable as well.

Designing them is not one of my strong areas though, so I will defer that whole topic to someone else with more experience.

As a more compelling real-life example from [FsToolkit.ErrorHandling] check their example

```fsharp
// Given the following functions:
//   tryGetUser: string -> Async<User option>
//   isPwdValid: string -> User -> bool
//   authorize: User -> Async<Result<unit, AuthError>>
//   createAuthToken: User -> Result<AuthToken, TokenError>

type LoginError = InvalidUser | InvalidPwd | Unauthorized of AuthError | TokenErr of TokenError

let login (username: string) (password: string) : Async<Result<AuthToken, LoginError>> =
  asyncResult {
    // requireSome unwraps a Some value or gives the specified error if None
    let! user = username |> tryGetUser |> AsyncResult.requireSome InvalidUser

    // requireTrue gives the specified error if false
    do! user |> isPwdValid password |> Result.requireTrue InvalidPwd

    // Error value is wrapped/transformed (Unauthorized has signature AuthError -> LoginError)
    do! user |> authorize |> AsyncResult.mapError Unauthorized

    // Same as above, but synchronous, so we use the built-in mapError
    return! user |> createAuthToken |> Result.mapError TokenErr
  }
```

Server applications tend to follow specific workflows and in this example we can see a login flow, this function can be used on the HTTP handler and simplify the code quite a lot, specially when you have to deal with async/task based functions that are also returning or using results.

## Final Thoughts

Options and results are actually really useful on the language they can help you ensure your data is consistent and correct. F#'s type inference will also ensure that your data is correct and that you won't have unexpected values where they are not supposed to go.

Hopefully this post sheds some light on options and results as individual concepts and spark ideas on how you can apply them to your F# code.

Also shout out to the wonderful [FsToolkit.ErrorHandling] library, it simplifies working with these so much.

We'll catch ourselves on the next one!
