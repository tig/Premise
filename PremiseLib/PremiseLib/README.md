Premise WebClient .NET Client Library
=======
*Copyright © 2013 Kindel Systems*

*A .NET library for accessing Premise from .NET.*

The **Premise WebClient .NET Client Library** makes it easy to build .NET apps that talk to Premise. It supports the entire [Premise WebClient Protocol](https://github.com/tig/Premise/blob/master/Premise%20Protocol%20Docs.md) including subscriptions so polling is not needed. See README.md in that project for more details. 

The client is written in C# 4.5 (Visual Studio 2012) and makes heavy use of two features that may or may not be portable to other platforms: `dynamic` and `async / await`. I intend to make this library work on ASP.NET, Windows Phone, Windows 8, Android (Xamarin), and iOS (Xamarin). So far it has only been used in a console app.

I've provided a little command line sample app called `pr` that I used for testing this library. You can find it [here.](..)

## Example Usage
This example illustrates connecting to a device (a light in my home office). It shows subscribing to both the `Brightness` properties and shows setting properties.

```
PremiseObject ob = new PremiseObject("sys://Home/Downstairs/Office/Undercounter");

// Create event handler to receive notifications when the value of a 
// property on this object changes
ob.PropertyChanged += (sender, args) => {
    var val = ((PremiseObject)sender).GetMember(args.PropertyName);
    Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location,
         args.PropertyName, val);
};

// Subscribe to the properties. Provide hints as to their types so that the
// library an coerce types between .NET and Premise.
await ob.AddPropertyAsync("Brightness", PremiseProperty.PremiseType.TypePercent, true);

// You don't have to call the AddPropertyAsync synchronously if you don't want. 
// They are fire-and-forget
ob.AddPropertyAsync("PowerState", PremiseProperty.PremiseType.TypeBoolean, true);

// Use dynamic to access the properties.
((dynamic) ob).Brightness = ((dynamic) ob).Brightness - .15;

// The preceding line will have caused the value to change on the server
// resulting in a subscription update to the client. Therefore the property
// change notification above will have been called twice (once for when we
// changed the value locally and once when the server notified us). 

// Note we can use Premise type syntax
((dynamic)ob).Brightness = "33%";
```

## What's Next?
* Put the library on NuGet
* Incorporate it into an Android app using Xamarin (Mono). This will suss out all cross-platform issues.
* Build an iOS test app.
* Build a Windows 8 test app.
* Re-write my Windows Phone app to use this instead of my hack-job I'm currently using.

