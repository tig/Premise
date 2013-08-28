# The Premise WebClient Protocol

*By Charlie Kindel*

*Version 0.1 - 2013-08-28*

This is a reverse engineered documentation of the Premise protocol spoken between the SYSConnector ActiveX control and the Premise Server. This is different than the protocol that Premise Builder and Minibroker use to talk to Premise Server.

## Overview
The Premise WebClient protocol is a REST-like HTTP based protocol. It does not follow actual SOAP or REST rules, but is actually fairly clean. It is spoken over port 80 and 443 (in the case of HTTPS) by default, or whatever port you set the WebServer to in Premise.

The protocol supports subscriptions. Clients can subscribe to either a property or class. To do this, Premise bastardizes HTTP by requiring the client keep the underlying socket open and supporting multiple responses to a single POST. These subscriptions are designed to be used by the SYSConnector and within the context of HTML.

## Commands
The general model for commands is to do an HTTP GET or POST to a URL formatted as

    http://localhost/sys/<object>?<command>?...

Where `<host>` is the hostname or IP address of the Premise server and `<command>` is one of `a`, `b`, `c`, `d`, `e`, `f`, or `g` as described below.

The `<object>` must be a object reference, which is either by name or GUID ie:

	Schema/Modules/AutomationBrowser/TaskList/Classes/TaskMenu/OnRenderPlugin
	{D84E7290-3664-4899-9222-A2DC237E6D96}

Premise supports the following commands:

### a - SubscribeToProperty

    <object>?a?<targetelementid>?[64]<method>?<property>?<subid>?<subname>

* `<object>` is the Premise object in question.
* `<targetelementid>` is a client specific ID. It identifies the HTML element on the client that is the target of the subscription.  
* `<method>` is a method name. If the 64 prefix is present it is **base64** encoded.  Can be blank.
* `<property>` is the property to subscribe to.
* `<subid>` is the client specific subscription ID. It appears this is used on the server only for diagnostics. It will be used as the `SubscriptionElem` property value for the subscription under WebServer\Sites\Default Web Site\Subscriptions in Builder.
* `<subname>` is the client specific subscription name. It appears this is used on the server only for diagnostics. It will be used as the `PluginPath` property value for the subscription under WebServer\Sites\Default Web Site\Subscriptions in Builder.

As described below in **Normal Responses** each time a property's value changes, Premise will send a response on the socket. The HTTP header named `Target-Element` will contain the `targetelementid` of the property that changed and the content of the response will be the new value.

### b - SubscribeToClass

    <object>?b?<targetelementid>?[64]<action>?<class>?<subid>?<subname>

* `<object>` is the Premise object in question
* `<targetelementid>` is a client specific ID 
* `<method>` is a method name. If the 64 prefix is present it is **base64** encoded.  Can be blank.
* `<class>` is the class to subscribe to.
* `<subid>` is the client specific subscription ID. It appears this is used on the server only for diagnostics. It will be used as the `SubscriptionElem` property value for the subscription under WebServer\Sites\Default Web Site\Subscriptions in Builder.
* `<subname>` is the client specific subscription name. It appears this is used on the server only for diagnostics. It will be used as the `PluginPath` property value for the subscription under WebServer\Sites\Default Web Site\Subscriptions in Builder.

Class subscriptions don't get property change events, just add/delete.

### c - Unsubscribe

    <object>?c?<subid>

### d - InvokeMethod 

	<object>?d?<targetelementid>?64<method>

* `<object>` is the Premise object to invoke the method on
* `<targetelementid>` is a client specific ID 
* `<method>` is a method name. If the 64 prefix is present it is **base64** encoded.  Can be blank.

Example:

    http://localhost/sys/Home?d??TestFn(True, "Gold")

* Works with script functions.  Use method.TestFn, method.ParamName1, etc...
* Does not work with script methods.
* Does not appear to work with built-in object methods (e.g. IsOfExplicitType(type) fails).

### e - SetValue 

	<object>?e?<property>

* `<object>` is the Premise object to invoke the method on
* `<property>` is the name of the property to set
* HTTP message-body contains is the new value
* Use `POST`

Example: 

    http://localhost/sys/Home/Kitchen/Overheadlight?e?PowerState

With a message-body containing either `0` or `1`

This is a fire & forget request.

### f - GetValue 

	<object>?f?<targetelement>?<property>

or 

    <object>!<propertyname>

* `<object>` is the Premise object to invoke the method on
* `<targetelementid>` is a client specific ID (optional)
* `<property>` is the name of the property to get
* HTTP response message-body contains the value

Notes:

* If `<property>` is `_xml` the returned mime-type will be `text/xml` and the content will be the entire object formatted as XML.

Examples: 

    http://localhost/sys/Home/Kitchen/Overheadlight?f??PowerState
    http://localhost/sys/Home/Kitchen/Overheadlight!PowerState

### g - RunCommand

	<object>?g?<commandname>

* `<object>` is the Premise object to invoke the method on
* `<commandname>` is the name of the command to run

Example:

    http://localhost/sys/Home?d??TestFn(True, "Gold")

This is a fire & forget request.

## Request Format

SYSConnector uses HTTP `GET` for any request that does not include content in the request and `POST` for requests that include content (e.g. SetValue). This does not adhear to REST guidelines, but that's the way it is.

SYSConnector requests look like this (SetValue example)

    POST HTTP/1.1
    User-Agent: SYSConnector/1.0 (WinNT)
    User-Agent: SYSConnector/1.0 (WinCE)
    Cookie: <cookie>
    Content-Type: text/plain
    Content-Length: <contentlength>
    SYSConnector: true
    
    <content>
    
## Normal Responses
Responses are standard HTTP (except in "fast mode", described elsewhere). Generally of the form:

    HTTP/1.1 200 OK
    <response strings>
    Date: <expiry date>
    <cookies>
    Cache-Control: no-cache
    Content-Type: <mime type>
    Target-Element: <target element>
    Content-Length: <content length>

    <content>

The mime-type is typically `text/hmtl` but `image/bmp`, `application/x-msmetafile`, `image/x-icon`, and `application/x-msmetafile` may be returned for images.

## Error Responses

Doing something that results in an error (e.g. accessing an object that does not exist) will result in a 404. Premise appears to close the socket on the server side when this happens.

    HTTP/1.1 404 Object Not Found
    Date: <expiry date>
    Connection: close
    Cache-Control: no-cache
    Content-Type: text/plain
	<cookies>
    Content-Length: <content length>

    <error content>

## Premise Specific Responses

Premise will send a response out of band of a specific POST or GET to indicate to the client status of the connection. The following responses may be sent.

### pauseConnection

If the client gets this response it should assume all subscriptions have been cancelled and should stop stop sending new requests.

### resumeConnection

If the client gets this response it can resume sending requests.

### fastMode

If the client gets the `fastMode` response fast mode is on. 

Turn on 'fast mode' by setting the `FastMode` value of the object `{8D692EC9-EB74-4155-9D83-315872AC9800}` to True:

    /sys/{8D692EC9-EB74-4155-9D83-315872AC9800}??e?FastMode?[True/False]

#### Fast Mode Requests

In 'fast mode' (see below) no HTTP headers are required and the command is not URL encoded

    <content-length><space><command><\r\n\r\n>[content, size_is(content-length)]

#### Fast Mode Responses

In 'fast mode' only the lines in the standard response after `Content-Type` are returned and the content is not URL encoded.
