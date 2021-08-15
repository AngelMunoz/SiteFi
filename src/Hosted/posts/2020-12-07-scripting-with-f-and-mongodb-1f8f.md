---
title: Backing up files with F# Scripts and MongoDB GridFS
subtitle: ~
categories: mongodb,fsharp,dotnet,scripting
abstract: F# 5.0 has strong scripting capabilities thanks to the updated #r "nuget: " directive, this allows yo...
date: 2020-12-07
language: en
---

F# 5.0 has strong scripting capabilities thanks to the updated `#r "nuget: "` directive, this allows you to pull any NuGet dependency and start using it from your F# scripts right away. To show off this feature I'll propose the following use case

> I want to backup files from a directory into a database and be able to restore them back to the directory I want

There are some ways we can do this from saving the data to a database and uploading to AWS/GCP/Azure/Dropbox in this case I wanted to showcase one of the Libraries I worked on previously called [Mondocks](https://github.com/AngelMunoz/Mondocks) which allows you to write MongoDB commands and execute them via the normal .NET MongoDB Driver.
I have a Raspberry PI on my LAN with a 1TB HDD and it has docker running so I can save my files there.

Let's start with the Backups, also I added some comments so you can read the source code and continue reading hereafter

{% gist https://gist.github.com/AngelMunoz/48e8e81e81571a73340f67a718d6333f file=backup.fsx %}

that's all we need to start backing up either the current directory or a directory we specify with our arguments, that should look like the following Gif

![Backup Files](https://dev-to-uploads.s3.amazonaws.com/i/6ol1ynkt7lb0zem4wra9.gif)

Now Let's continue with the restores

{% gist https://gist.github.com/AngelMunoz/48e8e81e81571a73340f67a718d6333f file=restore.fsx %}
The restoration process was hopefully as simple as the backup and should look like this
![Restore Backup](https://dev-to-uploads.s3.amazonaws.com/i/0l2k9as2pm2wdr3bg1so.gif)

And that's it! Hopefully, I showed you a bit of the F#'s scripting capabilities as well as how simple is to use Mondocks without sacrificing the MongoDB driver, since it's a side by side usage thing here's the project if you'd like to take a look

{% github AngelMunoz/Mondocks %}

Also, Shout out to [Spectre.Console](https://spectresystems.github.io/spectre.console/) for the amazing console output.


# Closing thoughts
Remember that F# is cross-platform, so if you want to run these scripts on Linux/MacOS as well you should be able to do so. F# is powerful yet it won't get in your way, it will most likely help you figure out how to do things nicely.



If you have further comments or doubts, please let me know down below or ping me on Twitter 😁
