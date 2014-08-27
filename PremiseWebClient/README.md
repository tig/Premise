Premise WebClient .NET Client Library Project
=======
*Copyright © 2014 Kindel Systems*

This project includes the following:

* **Premise WebClient .NET Client Library** - A .NET library that should make it easy to build .NET apps that talk to Premise. Fully supports subscriptions so polling is not needed. 
* **pr** - A simple command line tool that makes it easy to exercise the client library. 

## Premise WebClient .NET Client Library
*A .NET library for accessing Premise from .NET.*

The **Premise WebClient .NET Client Library** makes it easy to build .NET apps that talk to Premise. It supports the entire [Premise WebClient Protocol](https://github.com/tig/Premise/blob/master/Premise%20WebClient%20Protocol.md) including subscriptions so polling is not needed. See README.md in that project for more details. 

The client is written in C# 4.5 (Visual Studio 2012) and the library is built as .NET Portable Class Library (PCL) so it should work on ASP.NET, Windows Phone 7/8, Windows 8, Android (Xamarin), and iOS (Xamarin). 

### Installation
To add the PremiseWebClient library to your .NET, Windows Phone, or Windows 8 project use NuGet.

From the command line:

    PM> Install-Package PremiseWebClient -Pre

Or use "Manage NuGet Packages" in Visual Studio.

### Example Usage
This example illustrates connecting to a device (a light in my home office). It shows subscribing to both the `Brightness` properties and shows setting properties.

```C#
// Setup server connection
_server.Host = host;
_server.Port = port;
_server.Username = username;
_server.Password = password;

// Turn on subscriptions (optional if you don't want them)
await _server.StartSubscriptionsAsync();

// FastMode is off by default
_server.FastMode = true;

// Create a local object representing a server object
PremiseObject deskButton = new PremiseObject("sys://Home/Downstairs/Office/Keypad/Button_Desk");

// Create event handler to receive subscription notifications when the value of a 
// property on this object changes on the server
deskButton.PropertyChanged += (sender, args) => {
    // Note 
    var val = ((PremiseObject)sender)[PremiseProperty.NameFromItem(args.PropertyName)];
    Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location, PremiseProperty.NameFromItem(args.PropertyName), val);
};

// Identify what properties you care about. In this case we care about 'Description'
// 'Status', and 'Trigger' because this is a keypad button. 
// You can provide hints as to their types so that the library can coerce 
// types between .NET and Premise.
await deskButton.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypePercent);

// Notice for 'Status' we set the last parameter to true. This subscribes to
// changes. If you don't set this to true (or separately call "SubscribeToProperty"
// the property will not be subscribed by default. But the property will
// get updated from the server initially. 
await deskButton.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);

await deskButton.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);

// Use array syntax to access the properties.
Console.WriteLine("deskButton Status is {0}", deskbutton["Status"]);

// From code you can trigger a Premise momentary property just by setting 
// it to true
deskButton["Trigger"] = true;

// However, PremiseLib supports XAML Commands which make wiring up buttons in 
// UI easy. Use the syntax "<name>Command" in your XAML or code and PremiseLib
// will automatically set the property to true under the covers.
deskButton["TriggerCommand"].Execute(null);

// PremiseLib also supports optional CommandParameters for XAML Commands.
deskButton["DescriptionCommand"].Execute("Hello");

```

Example of setting properties in code.
```C#
PremiseObject entryLight = new PremiseObject("sys://Home/Upstairs/EntryLight");
await entryLight.AddPropertyAsync("Brightness", PremiseProperty.PremiseType.TypePercent, true);
entryLight["Brightness"] = entryLight["Brightness"] - .15;

// The preceding line will cause the Brightness property to change on the client. This
// will cause the value to get sent to the server.
// This will then result in a subscription update back to the client. 
// Therefore the property change notification be called at least twice (once for when we
// changed the value locally and once when the server notified us).
// In some cases, the client will get multiple updates because the underlying driver
// in Premise rounds the value (Lutron does this). 

// Note we can use Premise type syntax because PremiseProperty knows how to 
// coerce for PremiseTypePercent types.
entryLight["Brightness"] = "33%";
```

This example shows how you would create an ObservableCollection that could be easily consumed by a XAML based application on Windows 8 or Windows Phone.

```C#
        ...
        await PremiseServer.Instance.StartSubscriptionsAsync();

        PremiseObject o1 = new PremiseObject("sys://Home/Downstairs/Office/Keypad/Button_Desk");
        await o1.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
        await o1.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);
        await o1.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);

        PremiseObject o2 = new PremiseObject("sys://Home/Downstairs/Office/Keypad/Button_Workshop");
        await o2.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
        await o2.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);
        await o2.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);

        KeypadButtons = new ObservableCollection<PremiseObject> {
            (PremiseObject) o1,
            (PremiseObject) o2
        };
        foreach (PremiseObject l in KeypadButtons) {
            l.PropertyChanged += (s, a) => Debug.WriteLine("MVM: {0}: {1} = {2}",
                                                           ((PremiseObject) s).Location,
                                                           a.PropertyName,
                                                           ((PremiseObject) s)[a.PropertyName]);

        }
    }

    private ObservableCollection<PremiseObject> _KeypadButtons;
    public ObservableCollection<PremiseObject> KeypadButtons {
        get { return _KeypadButtons; }
        set { _KeypadButtons = value; RaisePropertyChanged("KeypadButtons"); }
    }
}
```

The corresponding XAML:

```XAML
<ListBox x:Name="Lighting" ItemsSource="{Binding Path=KeypadButtons}" >
    <ListBox.ItemTemplate>
        <DataTemplate >
            <Grid Margin="6">
                <StackPanel Orientation="Horizontal" >
                    <TextBlock Text="{Binding [Description]}" VerticalAlignment="Center" Width="120" />
                    <TextBlock Text="{Binding [Status]}"  VerticalAlignment="Center" Width="120" />
                    <Button Content="Trigger" Command="{Binding [TriggerCommand]}" Width="186" />
                </StackPanel>
            </Grid>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

The beauty of the above is that the button is automatically disabled if there is no connection to the server.

## Other examples

* pr - A command line example included in the PremiseLib project
* PremiseMetro - a Win8/WinRT example.

## What's Next?
* Put the library on NuGet
* Incorporate it into an Android app using Xamarin (Mono). This will suss out all cross-platform issues.
* Build an iOS test app.
* Re-write my Windows Phone app to use this instead of my hack-job I'm currently using.

