---
title: Migrondi, Simple SQL migrations tool
subtitle: ~
categories: sql,dotnet,fsharp,tool
abstract: Today I'll write about a tool I live coded on my twitch stream the first weeks of April As part of my...
date: 2020-04-14
language: en
---

Today I'll write about a tool I live coded on my twitch stream the first weeks of April As part of my F# journey 

> Please note that this tool does not require you to use .net at all it's a standalone tool that you can integrate to any workflow you may have but... if you are a .netcore user you can install this as a global tool as well
`dotnet tool install --global Migrondi`

{% github AngelMunoz/Migrondi %}

Migrondi is a simple SQL migrations tool with a very few commands

- init
- new
- up
- down
- list

To use it just download a binary from the releases page or... build the sources
after that just put it on your path and you can start working with it

>For the rest of the post I will assume that the Migrondi binary is in the system's path and it's named "migrondi.exe" or "migrondi" in linux

Migrondi isn't tied to any project at all you can have your migrations entirely outside of your project or if you want to keep your SQL migrations as part of your git history you can do them on your repository as well.

## Init
The init command is perhaps the first that you will run if you don't have anything in place yet, it will create a "migrations" directory and a "migrondi.json" wherever you had invoked the migrondi command

I created a sample directory under my user
```
PS C:\Users\scyth\sample> migrondi
Migrondi
Copyright (C) 2020 Angel D. Munoz

ERROR(S):
  No verb selected.

  init       Creates basic files and directories to start using migrondi.

  new        Creates a new Migration file.

  up         Runs the migrations against the database.

  down       Rolls back migrations from the database.

  list       List the amount of migrations in the database.

  help       Display more information on a specific command.

  version    Display version information.
```

Running the init command creates a file and a directory
```
PS C:\Users\scyth\sample> migrondi init
Created C:\Users\scyth\sample\migrondi.json and C:\Users\scyth\sample\migrations\
PS C:\Users\scyth\sample>
```

![after init](https://dev-to-uploads.s3.amazonaws.com/i/cwaqrhe69159vk1tmkti.png)

the contents of the "migrondi.json" file are very simple
```json
{
  "connection": "Data Source=migrondi.db",
  "migrationsDir": "C:\\Users\\scyth\\sample\\migrations\\",
  "driver": "sqlite"
}
```
if you are just trying or curious you can leave the JSON file as is, it will work with SQLite database inside the same directory as the config file, if you want to try with other databases you can switch the connection and the driver, you can check [here](https://github.com/AngelMunoz/Migrondi#config-file) for more information about the config file

> About the "migrationsDir" it can be a relative path as well, just remember where you are invoking migrondi from, in the README you will see it's listed as `"migrationsDir": "./migrations/"` it's fine as well it just assumes that you are invoking migrondi above the migrations directory


## New
Migrondi has just one objective and that is running migrations, I didn't want to build an abstraction over SQL because I don't think that's a simple approach at all and requires you to have .NET specific tooling which is not what I'm looking for.

If you choose `MySQL` as your SQL dialect, then your migrations have to be written MySQL. If you are using PostgreSQL the same thing applies.

the `new` command takes a parameter `-n` or `--name` which is the name of the migration that you will be adding to your database

```
PS C:\Users\scyth\sample> migrondi new -n InitialMigration
```

that will create a new SQL file with the name you provided and a Unix timestamp

![Initial Migration](https://dev-to-uploads.s3.amazonaws.com/i/drl1h6yfh9q8lkpulj4r.png)

the contents are up to you, either create a new table, insert values, alter columns, you name it.
In this example let's create a simple todo's table
```sql
-- ---------- MIGRONDI:UP:1586888312019 --------------
-- Write your Up migrations here
CREATE TABLE Todos(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT
);
-- ---------- MIGRONDI:DOWN:1586888312019 --------------
-- Write how to revert the migration here
DROP TABLE Todos;
```

> there are some important things here, please **Do not remove the "MIGRONDI" annotations** these are used to identify which part of the script run when the up/down command is invoked, you can delete the "-- Write your....." comments though

## Up
The up command reads every file inside the migrations directory and runs it against the database, the history of these migrations is preserved in a table called "migration" and it's used to keep track of what to run when the up/down command is invoked

#### v0.6.0
We added a `--dry-run true` option and you can now check on your terminal what's going to be run against your database before doing it

![migrondi up](https://dev-to-uploads.s3.amazonaws.com/i/ygrteto6lhcccq65rl4i.png)
After running the "up" command, our SQLite database is created. Let's check the contents

![Check database exists](https://dev-to-uploads.s3.amazonaws.com/i/6we9l9hqlftcd6xft0vx.png)

> By the way, I'm using this VSCode plugin for the database access https://marketplace.visualstudio.com/items?itemName=mtxr.sqltools

The database was created and the migrations table was updated

# Down
Let's say for some reason you need to revert those changes then you just need to run the `down` command 

#### v0.6.0
We added a `--dry-run true` option and you can now check on your terminal what's going to be run against your database before doing it.

![migrondi down](https://dev-to-uploads.s3.amazonaws.com/i/dxudxt3qh3jjd6fkrs3g.png)
after running down I tried to execute the same select all query, but since the migration has been run down my down statement says `DROP TABLE Todos;` so it took down the table If I check my "migrations" table it shows empty meaning we went back to the beginning.

> The Up and Down commands have the parameter "-t" "--total" which allows you to specify the number of migrations to go up or to go down if you don't specify a number it will run all migrations up and all migrations down


## List
The list command gives you the migrations that are present in the database or that are pending. Let's say you had an error in your migrations and the up command worked with some migrations, you can easily detect which migration is in the database
![migrondi list](https://dev-to-uploads.s3.amazonaws.com/i/mr9y3hdwva0l1j3tk2sg.png)
that way you don't need to go down on all migrations and see if the next time it works or not, just fix the last one that failed and keep going.


### Insights and closing thoughts
Migrondi was written in F# and the binary is a self-contained dotnet core app, you don't need to have anything installed besides that binary that's why it's a big binary.

Every migration is run inside a transaction so if anything inside the migration script fails the change is not committed to the database.

If you can give it a try and let me know if it's useful I'd be glad. If you like it or have any doubts ping me below in the comments or on Twitter :)


I hope you are having excellent week cheers :)
