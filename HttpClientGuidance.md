# Table of contents
 - [Using HttpClient](#using-httpclient)
 - [Different platform implementations](#different-platform-implementations)
 - [A note about WebClient](#webclient)
   
## Using HttpClient

[HttpClient](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=net-5.0) is the primary API for making outbound HTTP requests in .NET. 

## Different Platform Implementations

`HttpClient` is a wrapper API around an `HttpMessageHandler`. The most inner `HttpMessageHandler` is the one that's responsible for making the HTTP request. There are several implementations on various .NET platforms. This document is focused on server applications and will focus on 2-3 main implementations:
- HttpClientHandler/WebRequestHandler on .NET Framework
- SocketHttpHandler on .NET Core/5
- WinHttpHandler on .NET Framework or .NET Core/5 (runs on both but is Windows specific)

## A note about WebClient

WebClient is considered a legacy .NET API at this point and has been completely superseded by HttpClient. New code should be written with HttpClient.

❌ **BAD** This example uses the legacy WebClient to make a synchronous HTTP request.

```C#
public string DoSomethingAsync()
{
    var client = new WebClient();
    return client.DownloadString("http://www.google.com");
}
```

:white_check_mark: **GOOD** This example uses an HttpClient to asynchronously make an HTTP request.

```C#
static readonly HttpClient client = new HttpClient();

public Task<string> DoSomethingAsync()
{
    return client.GetStringAsync("http://www.google.com");
}
```
