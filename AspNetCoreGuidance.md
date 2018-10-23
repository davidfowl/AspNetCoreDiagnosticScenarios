# WIP: ASP.NET Core Guidance

ASP.NET Core is a cross-platform, high-performance, open-source framework for building modern, cloud-based, Internet-connected applications. This guide captures some of the common pitfalls and practices when writing scalable server applications.

## Avoid using synchronous Read/Write overloads on HttpRequest.Body and HttpResponse.Body

All IO in ASP.NET Core is asynchronous. Servers implement the `Stream` interface which has both synchronous and asynchronous overloads. The asynchronous ones should be preferred to avoid blocking thread pool threads (this could lead to thread pool starvation).

❌ **BAD** This example uses the `StreamReader.ReadToEnd` and as a result blocks the current thread to wait for the result. This is an example of [sync over async](AsyncGuidance.md#avoid-using-taskresult-and-taskwait).

```C#
public class MyController : Controller
{
    [HttpGet("/pokemon")]
    public ActionResult<PokemonData> Get()
    {
        // This synchronously reads the entire http request body into memory.
        // If the client is slowly uploading, we're doing sync over async because Kestrel does *NOT* support synchronous reads.
        var json = new StreamReader(Request.Body).ReadToEnd();

        return JsonConvert.DeserializeObject<PokemonData>(json);
    }
}
```

✔️**GOOD** This example uses `StreamReader.ReadToEndAsync` and as a result, does not block the thread while reading.

```C#
public class MyController : Controller
{
    [HttpGet("/pokemon")]
    public ActionResult<PokemonData> Get()
    {
        // This synchronously reads the entire http request body into memory.
        var json = new StreamReader(Request.Body).ReadToEndAsync();

        return JsonConvert.DeserializeObject<PokemonData>(json);
    }
}
```

:warning: **NOTE: If the request is large it could lead to out of memory problems which can result in a Denial Of Service. See [this](#avoid-reading-the-entire-request-body-or-response-body-into-memory) for more information.**


## Use buffering and synchronous reads and writes as an alternative to asynchronous reading and writing

## Avoid reading the entire request body or response body into memory

## Do not access the HttpContext from multiple threads in parallel. It is not thread safe.

## Do not use the HttpContext after the request is complete

The `HttpContext` is only valid as long as there is an active http request in flight. The entire ASP.NET Core pipeline is an asynchronous chain of delegates that executes every request. When the `Task` returned from this chain completes, the `HttpContext` is recycled. 

❌ **BAD** This example uses async void (which is a **ALWAYS** bad in ASP.NET Core applications) and as a result, accesses the `HttpResponse` after the http request is complete. It will crash the process as a result.

```C#
public class AsyncVoidController : Controller
{
    [HttpGet("/async")]
    public async void Get()
    {
        await Task.Delay(1000);

        // THIS will crash the process since we're writing after the response has completed on a background thread
        await Response.WriteAsync("Hello World");
    }
}
```

✔️**GOOD** This example returns a `Task` to the framework so the http request doesn't complete until the entire action completes.

```C#
public class AsyncController : Controller
{
    [HttpGet("/async")]
    public async Task Get()
    {
        await Task.Delay(1000);
        
        await Response.WriteAsync("Hello World");
    }
}
```

## Prefer logging scopes with data over passing the HttpContext values into loggers directly

## Do not capture the HttpContext in background threads

## Do not capture services injected into the controllers on background threads

## Avoid writing to the response body after middleware pipeline executes
