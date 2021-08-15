---
title: Yup, A MongoDB POC for UWP
subtitle: ~
categories: uwp,dotnet,csharp,poc
abstract: Yup, it's time to do some experience sharing
date: 2019-09-23
language: en
---

Yup, it has been a long time since I did some writing but today I wanted to share my experience on building a proof of concept of MongoDB Database Manager for the Windows Store

## Where to start?

Well once you have your visual studio 2019 with the Universal Windows Platform (UWP) development package it can be really straight forward but the default templates are... well quite empty and if you (like me) are not well versed into .net land you will do some nice things in atrocious ways like having rest calls on the code behind view file, as well as tons of business logic in them... and uhhh tons of stuff
however one good way to start these days is using the Windows Template Studio project

{% github Microsoft/WindowsTemplateStudio %}

which gives you some defaults and options that are in tune with some coding standards in the community.

You can get set up pretty fast with it, so you can forget implementing your own boilerplate for a navigation pane or a menu bar. You can also choose different patterns to manage your view's logic either code behind (without a library and just plain you), MVVM Light, MVVM Basic, Caliburn Micro and Prism.

I chose Caliburn Micro, since I've been an [Aurelia](https://aurelia.io/) user so I figured out It could be familiar in some aspects for me.

![New App](https://thepracticaldev.s3.amazonaws.com/i/g4z8i6ulpf90ho0s5ats.png)

You can choose a set of pre-built pages for your project (quite nice if you have already designed/sketched some of your views)

![Options for Pages](https://thepracticaldev.s3.amazonaws.com/i/d4gyvzhndk54tgsy3j4w.png)

you can add as many as you want if you need multiple webviews, charts, datagrids etc.
these bring you most of the code you need to make the page work, most of the time you will tie some things here and there but they'll cut some code work for you.

Then after you can add some UWP specific features, like sharing from, sharing to your app, deep links if you provide a protocol for your app. Also you can add Http services, a Web API project (if you intend to use your backend in .net) and lastly of course, Tests in the form of MSTest, XUnit or NUnit and also WinApp Driver (which is kind of like Selenium but for UWP Apps).


## What's next?
The following is just plain coding, and when you are using the template studio  you don't feel that much lost, basically is just following the pattern you chose and adding your specific data/logic code and that should do.

In our case, we'll do a fairly straight forward way to do things, we'll only have two views, our Main Page which will be where you can add/modify/delete database connections

### Main Page
![Main Page](https://thepracticaldev.s3.amazonaws.com/i/c75635d7490hecnkqzva.png)

I decided to store these connections using the WinRT API's Local Settings

```csharp
namespace Yup.Services 
{
  public class PreviousConnectionService
  {
    public Task SaveConnection(string keyName, PreviousConnection value)
    {
      return ApplicationData
               .Current
               .LocalSettings
               .SaveAsync($"previous:mongodb:{keyName}", value);
    }

    public void RemoveConnection(PreviousConnection toRemove)
    {
      ApplicationData
        .Current
        .LocalSettings
        .RemoveKeyValue($"previous:mongodb:{toRemove.KeyName}");
      return;
    }

    public Task<PreviousConnection[]> GetPreviousConnectionsAsync()
    {
      var connections = ApplicationData
          .Current
          .LocalSettings
          .Values
          .Where(kv => kv.Key.Contains("previous:mongodb:"))
          .Select(kv => 
            Json.ToObjectAsync<PreviousConnection>(kv.Value as string));
      return Task.WhenAll(connections);
    }

    // ... and other methods which are not relevant for this post
  }
}
```

For our copy from clipboard button the code is also pretty simple

```xml
<Page 
  xmlns:cal="using:Caliburn.Micro"> <!-- and other namespaces -->
<StackPanel
  Padding="24"
  MaxWidth="450"
  Background="{ThemeResource SystemControlAcrylicElementBrush}"
  Visibility="{x:Bind ViewModel.ShowAddForm, Converter={StaticResource BoolToVisibilityConverter }, Mode=OneWay}">
 <!-- text blocks and labels -->
  <AppBarButton
    Margin="8"
    x:Uid="MainPage_FromPaperclip"
    Icon="Attach"
    cal:Message.Attach="[Event Click] = [Action OnPaperClip()]" />
</StackPanel>
<!-- and more stuff... -->
</Page>
```

```csharp
namespace Yup.ViewModels
{
  public class MainViewModel
  {
    // ... bunch of stuff

    public async void OnPaperClip()
    {
      var package = Clipboard.GetContent();

      try
      {
        var content = await package.GetTextAsync();
        SelectedItem = new PreviousConnection() 
                        { IsActive = false, 
                          KeyName = SelectedItem.KeyName, 
                          MongoUrl = content 
                        };
      }
      catch (Exception e)
      {
        Debug.WriteLine(e.Message);
      }
    }
    // more stuff
  }
}
```
due the way data binding works on UWP we need to assign a new SelectedItem instance instead of just modifying the actual one, that way we are 100% that our UI will update itself.

Once we select a connectionand hit connect we'll use our mongo service to connect with the provided url

```csharp
public async void OnConnect()
{
  _mongoservice.SetUrl(SelectedItem.MongoUrl);
  await _prevConnService.SetActiveConnectionAsync(SelectedItem);
  _navigation.NavigateToViewModel<DatabasesViewModel>();
  IsLoading = true;
}
```
`_mongoservice` is registered as a singleton service, so there is only one instance of it for the whole application.

After that we navigate to our second view, the DatabasesView.

### DatabasesView
Here we have a simple layout. A grid of 4 areas where we will put a Treeview on the left spaning two rows listing the databases and it's collections. then we have on the right one row for the text editor we will use later and a second row to display a listview of the results of the queries we will do.


![Databases](https://thepracticaldev.s3.amazonaws.com/i/41v08vjperltpys71v1z.png)

Now bear with me because things will get weird from now on.
If you are thinking something like this

>Is that the monaco editor?

or

>Looks like vscode

Yes, you don't need glasses (or.. perhaps you do?) and you are right, for this I used the Monaco Editor to be able to have a nice editing experience out of the box (it's a proof of concept of that too) We're using a WebView and a local HTML file with a simple Javascript file and some event communications out there

let's see inside HTML/MonacoEditor.html...

![Monaco](https://thepracticaldev.s3.amazonaws.com/i/zaw7dfc5zz3vokkkukg9.png)

as you can see is a simple file that does only that, the javascript file is also very short

![Monaco JsFile](https://thepracticaldev.s3.amazonaws.com/i/leqbwvl5zs46pze61v9i.png)

Key points on this `C# <-> Webview` are the following

- Create an editor instance
- Register commands (like Ctrl + Enter)
- Use `window.external.notify` to send information to the C# code
- To notify the monaco editor to change content (switching databases) Use `InvokeAsyncScript`

In this point I had some Issues to manage the WebView's events from the ViewModel, using Caliburn Micro's defaults event wiring so In this specific case I decided to use the code behind to handle some of the event wiring/actions

```xml
<muxc:TreeView
  x:Name="DatabaseTree"
  Grid.RowSpan="2"
  Grid.Column="0"
  SelectionMode="Single"
  ItemsSource="{x:Bind ViewModel.Databases}"
  cm:Message.Attach="[Event ItemInvoked] = [Action OnItemInvoked($eventArgs)]">
  <!-- item template for the treeview and it's children -->
</muxc:TreeView>
<!-- a little bit more ahead... -->
<WebView
  x:Name="EditorWebView"
  Grid.Row="0"
  Margin="0, 0, 0, 5"
  MinHeight="220"
  MinWidth="120"
  cm:Message.Attach="
    [Event NavigationStarting] = [Action OnNavigationStarting()];
    [Event NavigationCompleted] = [Action OnWebViewLoaded($eventArgs)];
    [Event ScriptNotify] = [Action OnScriptNotify($eventArgs)]" />
```

```csharp
namespace Yup.Views
{
  public sealed partial class DatabasesPage : Page
  {
    public DatabasesPage()
    {
      InitializeComponent();
      Loaded += DatabasesPage_Loaded;
      // ... more things
    }
    // ... more things
    private void DatabasesPage_Loaded(object sender, RoutedEventArgs e)
    {
      ViewModel.OnEntrySelected += ViewModel_OnEntrySelected;
      EditorWebView.Source = new Uri("ms-appx-web:///Html/MonacoEditor.html");
    }

    private async void ViewModel_OnEntrySelected(object sender, DatabaseEntry e)
    {
      var contents = JsonConvert.SerializeObject(e);
      var response = await EditorWebView.InvokeScriptAsync(
        "setEditorContent", 
        new string[] { contents }
      );
      if (response.Length > 0)
      {
        Debug.WriteLine($"{response[0]} - {response[1]}");
      }
      ViewModel.IsLoadingEditor = false;
    }
  }
}

```

Tipically you would not want to do this, as the Main Page xaml code showed, you can simply wire your events (like clicks) to your ViewModel directly, in this case with caliburn micro, but I'm pretty sure things are similar with Prism and MVVM Light/Basic.

From now on, the code is also fairly simple (although repetitive) and straight forward

```csharp
namespace Yup.ViewModels
{
  public class DatabasesViewModel : Screen
  {
    // ... a lot of declarations
    protected override async void OnViewReady(object view)
    {
      base.OnViewReady(view);
      IsLoadingDatabases = true;
      var databases = await _mongoservice.GetDatabases();
      Databases.Clear();
      foreach (var database in databases)
      {
        Databases.Add(new DatabaseEntry() {/* entry values */}); ;
      }
      IsLoadingDatabases = false;
    }
    // ... methods and other stuff ...

    public async void OnScriptNotify(NotifyEventArgs args)
    { /* We'll see more details ahead */ }

    public async Task OnExecuteStatement()
    { /* We'll see more details ahead */ }

  }
}

```

once we've landed on this location we use the viewmodel life cycle events like OnViewReady to load the databases, at this point without collections, we'll fetch the collections when we select a database.

So... to continue our WebView's code above to do the `HTML -> C#` and `C# -> HTML` communication  we'll see two methods 

- `OnScriptNotify`
when we do edits in the editor and press Ctrl + Enter we notify the webview that something is happening we pick that and since we know we we used a string in the format `comand;value` in this case we only use the "ExecuteInEditor" command, but if we were to do other commands like ... New Tab (CTRL + T), or quick save shortcuts (CTRL + S), etc. we could use this place to manage/handle commands

```csharp
var execution = args.Value.Split(';');
var command = execution[0];
var commandValue = execution[1];
QueryStatement = commandValue;
switch (command)
{
  case "ExecuteInEditor":
  await OnExecuteStatement();
  break;
}
```

- `OnExecuteStatement`
```csharp
IsLoadingResults = true;
QueryError = "";
HeaderResults.Clear();
QueryResults.Clear();
try
{
  var (cursor, ok) = await _mongoservice
                       .ExecuteRawAsync(SelectedDb, QueryStatement);
  cursor
    .Value
    .AsBsonDocument
    .TryGetValue("firstBatch", out BsonValue firstBatch);
  var rows = firstBatch.AsBsonArray;
  foreach (var row in rows)
  {
    var result = row
                  .AsBsonDocument
                  .ToJson(
                    new JsonWriterSettings
                     { Indent = true, IndentChars = "  " });
    QueryResults.Add(result);
  }

}
catch (Exception e)
{
  QueryError = e.Message;
  Debug.WriteLine($"{e.Message}");
}
IsLoadingResults = false;
```
One of the pain points for me was on how to present the data for example we don't know anything of the data we're about to show, no headers, no values, no data types. I wanted to show the results in a datagrid with headers columns and rows and any nested json, to be shown as a string but I was not able to come up with a solution even using the Windows Community Toolkit's DataGrid, so I ended up just converting each result to a string and showing a ListView of said results.

Other thing you may notice is that we're not doing our common query language/commands for mongodb, ex: `db.collection.find()` but using a [database command](https://docs.mongodb.com/manual/reference/command/)

that's why you see that `await _mongoservice.ExecuteRawAsync(SelectedDb, QueryStatement);` line up above.

Regarding our Views... that's it that's all we need to query a database and execute CRUD commands on it or... is it?

### Onto Services
Yes, we need something else, and that's the MongoDB service.

Using the mongodb driver for C#  when you are using your own data is quite nice, because you can create models and then use these types to do CRUD operations just nice, perhaps is just my lack of experience but working with BsonValues/BsonElements is weird and feels like diving in a pit.

but it isn't very complicated to do simple things

```csharp
namespace Yup.Core.Services
{
  public class MongoService
  {
    private MongoClient _mongoclient;
    public string CurrentUrl { get; private set; }

    public void SetUrl(string url)
    {
      _mongoclient = new MongoClient(url);
      CurrentUrl = url;
    }

    public async Task<IEnumerable<string>> GetCollectionsFrom(string dbName)
    {
      var database = _mongoclient.GetDatabase(dbName);
      var cursor = await database.ListCollectionNamesAsync();
      return cursor.ToEnumerable();
    }

    public async Task<IEnumerable<string>> GetDatabases()
    {
      var cursor = await _mongoclient.ListDatabaseNamesAsync();
      return cursor.ToEnumerable();
    }

    public async Task<(BsonElement, BsonElement)> ExecuteRawAsync(string dbName, string command)
    {
      var db = _mongoclient.GetDatabase(dbName);
      var result = await db.RunCommandAsync<BsonDocument>(command);
      result.TryGetElement("cursor", out BsonElement cursor);
      result.TryGetElement("ok", out BsonElement ok);
      return (cursor, ok);
    }
  }
}
```
in this case the `ExecuteRawAsync` Method pulls out the `cursor` and the `ok` from the command's response. The driver itself is very flexible so you should be able to do very complex things if needed.


## Extras

So yeah that wasn't so bad was it?

I guess for the average js dev (like me) perhaps it feels too bloated and a lot of code for a fairly simple thing but I think it is worth it if you take into account other things in mind, the Store is a way to distribute your app, it gets to work on sandboxed environments like Windows S, your users will thank you with their machine's resource usage

![36mb idle](https://thepracticaldev.s3.amazonaws.com/i/5nhsgq5h2cvu0g64ekk6.png)
that's when it's idle at it's lowest memory usage

![134mb](https://thepracticaldev.s3.amazonaws.com/i/j430dify5hd622877aa4.png)
that's when we load the Monaco editor and being idle but remember we added the monaco editor to be nice and don't implement a text editor, perhaps there's a good text editing library out there or if you want to go really cheap you could have used a simple textbox and resource usage would be like... 40-50mb max ram usage?

Plus other unexplored goodies in this post like, Toast notifications, User Activity integration (like clicking on your task view and get back to that query you made yesterday but forgot to save), share from/to your app, deep linking


You can find the Source code on this repo

{% github AngelMunoz/Yup %}

## Closing thoughts
It was a rewarding experience and I kind of answered myself the following question

>Can there be good dev tooling in the windows store?

YES! there can be it's just that developers are looking into other places at the moment.

In fact this App Inspired me

{% twitter 1125284314738855936 %}

which is a rest client like Postman but purely UWP and one that of course doesn't take as much as Postman's resource usage on your machine.

Perhaps people feel UWP it's too hard/complex, while you may need to write more code than your usual javascript I think in the end it is not that matters much, the trade offs seem fine to me.


Please leave your comments and feedback if you like it, if you don't well tell me also! :)
