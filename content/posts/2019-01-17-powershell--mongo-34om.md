---
title: Powershell + Mongo?
subtitle: ~
categories: powershell,mongo,terminal,cmd
abstract: I found out this Powershell module that allows me to do mongo stuff from the terminal!
date: 2019-01-17
language: en
---


Often one of the things I hear when people recommend you stuff for windows development is to switch to `git-bash` as a default terminal. While I do install `git` on my machines, I have always wonder why do you need `git-bash` as default at all? you have `Powershell` after all!

`Powershell` comes with a bunch of common bash aliases like `ls`, `cd`, `cat`, etc, you can find most common aliases with `Get-Alias` (or `get-alias`), I can see right of the bat `wget`, `tee` and some others. Plus `Powershell Core` is [Cross Platform](https://github.com/PowerShell/PowerShell#get-powershell)!

---
Sometimes I need to do some manual backups on mongo databases while I know there are thousands of solutions to this already available but one of the ways to learn is to reinvent the wheel for n-th time so I decided to see if there was some way to talk to mongo from powershell and found this project

{% github nightroman/Mdbc %}

and Decided to give it a go, so I installed the vscode extension for Powershell plus the module in my local modules


```powershell
Install-Module Mdbc -Scope CurrentUser
```

after that just created a `test.ps1` file

```powershell
Import-Module Mdbc;

function New-MongoExport {
  param (
    # Your Server's URL
    [string]
    $Url = "mongodb://localhost:27017",
    # Database to connect to
    [string]
    $Db = "test",
    # Collection Name
    [string] 
    $CollectionName = "users",
    # Where do we want to put that information
    [string] 
    $Path = ".\$collectionName.json",
    # Default Limit
    [int16] 
    $Limit = 10
  )
  # Remove the file if it exists
  Remove-Item $Path -ErrorAction Continue;
  # Connect and count
  Connect-Mdbc $Url $Db $CollectionName;
  $count = Get-MdbcData -Count;
  # Do some fancy output
  Write-Host "Exporting $($count) records" -ForegroundColor Yellow -BackgroundColor DarkCyan;
  for ($i = 0; $i -lt $count; $i += $Limit) {
    # You don't actually need to paginate
    # The module uses the mongo driver so it's quite fast
    # But anyways it reads like `Skip $i items and take the first $Limit items then, export those`
    Get-MdbcData -Skip $i -First $Limit | Export-MdbcData $Path -Append;
  }
  # We're Done, fancy output
  Write-Host "File Written: $Path" -ForegroundColor Black -BackgroundColor Green;
}

New-MongoExport
# Also you can use it like this
# New-MongoExport -Url mongodb://myotherserver:1234 -Db NotTestDB -CollectionName posts
```

I think the module actually has a way to export collections directly, but like I said above I just wanted to toy out with this.

I don't know, but this appealed to me makes me wonder what I could do with it, besides automation, you know this language is supposed to be used to automate stuff also anyways

Have you done something in `Powershell`? share it in the comments!
Also if you don't like `Powershell` could you share why?
I mean I guess there's a reason why [WSL](https://docs.microsoft.com/en-us/windows/wsl/faq) is a thing nowdays.
