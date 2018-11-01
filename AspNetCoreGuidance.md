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

:white_check_mark: **GOOD** This example uses `StreamReader.ReadToEndAsync` and as a result, does not block the thread while reading.

```C#
public class MyController : Controller
{
    [HttpGet("/pokemon")]
    public async Task<ActionResult<PokemonData>> Get()
    {
        // This synchronously reads the entire http request body into memory.
        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        return JsonConvert.DeserializeObject<PokemonData>(json);
    }
}
```

:bulb:**NOTE: If the request is large it could lead to out of memory problems which can result in a Denial Of Service. See [this](#avoid-reading-the-entire-request-body-or-response-body-into-memory) for more information.**

## Prefer using HttpRequest.ReadAsFormAsync() over HttpRequest.Form

TBD

## Use buffering and synchronous reads and writes as an alternative to asynchronous reading and writing

TBD

## Avoid reading the entire request body or response body into memory

TBD

## Do not access the HttpContext from multiple threads in parallel. It is not thread safe.

TBD

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

:white_check_mark: **GOOD** This example returns a `Task` to the framework so the http request doesn't complete until the entire action completes.

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

## Do not capture the HttpContext in background threads

TBD

## Do not capture services injected into the controllers on background threads

❌ **BAD** This example shows a closure is capturing the context from the Controller action parameter. This is bad because this work item could run
outside of the request scope and the PokemonDbContext is scoped to the request. As a result, this will crash the process.

```C#
public class FireAndForgetController : Controller
{
    [HttpGet("/fire-and-forget-1")]
    public IActionResult FireAndForget1([FromServices]PokemonDbContext context)
    {
        // This is an implicit async void method. ThreadPool.QueueUserWorkItem takes an Action, but the compiler allows
        // async void delegates to be used in its place. This is dangerous because unhandled exceptions will bring down the entire server process.
        ThreadPool.QueueUserWorkItem(async state =>
        {
            await Task.Delay(1000);

            // This closure is capturing the context from the Controller action parameter. This is bad because this work item could run
            // outside of the request scope and the PokemonDbContext is scoped to the request. As a result, this will crash the process with
            // and ObjectDisposedException
            context.Pokemon.Add(new Pokemon());
            await context.SaveChangesAsync();
        });

        return Accepted();
    }
}
```

:white_check_mark: **GOOD** This example injects an `IServiceScopeFactory` and creates a new dependency injection scope in the background thread and does not reference
anything from the controller itself. It also uses `Task.Run` which will not crash the process if an exception occurs on the background thread.

```C#
[HttpGet("/fire-and-forget-3")]
public IActionResult FireAndForget3([FromServices]IServiceScopeFactory serviceScopeFactory)
{
    // This version of fire and forget adds some exception handling. We're also no longer capturing the PokemonDbContext from the incoming request.
    // Instead, we're injecting an IServiceScopeFactory (which is a singleton) in order to create a scope in the background work item.
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000);

        // Create a scope for the lifetime of the background operation and resolve services from it
        using (var scope = serviceScopeFactory.CreateScope())
        {
            // This will a PokemonDbContext from the correct scope and the operation will succeed
            var context = scope.ServiceProvider.GetRequiredService<PokemonDbContext>();

            context.Pokemon.Add(new Pokemon());
            await context.SaveChangesAsync();
        }
    });

    return Accepted();
}
```

## Avoid adding headers after the HttpResponse has started

ASP.NET Core does not buffer the http response body. This means that the very first time the response is written, the headers are sent along with that chunk of the body to the client. When this happens, it's no longer possible to change response headers.

❌ **BAD** This logic tries to add response headers after the response has already started.

```C#
app.Use(async (next, context) =>
{
    await context.Response.WriteAsync("Hello ");
    
    await next();
    
    // This may fail if next() already wrote to the response
    context.Response.Headers["test"] = "value";    
});
```

:white_check_mark: **GOOD** This example checks if the http response has started before writing to the body.

```C#
app.Use(async (next, context) =>
{
    await context.Response.WriteAsync("Hello ");
    
    await next();
    
    // Check if the response has already started before adding header and writing
    if (!context.Response.HasStarted)
    {
        context.Response.Headers["test"] = "value";
    }
});
```

:white_check_mark: **GOOD** This examples uses `HttpResponse.OnStarting` to set the headers before the response headers are flushed to the client.

It allows you to register a callback that will be invoked just before response headers are written to the client. It gives you the ability to append or override headers just in time, without requiring knowledge of the next middleware in the pipeline.

```C#
app.Use(async (next, context) =>
{
    // Wire up the callback that will fire just before the response headers are sent to the client.
    context.Response.OnStarting(() => 
    {       
        context.Response.Headers["someheader"] = "somevalue"; 
        return Task.CompletedTask;
    });
    
    await next();
});
```
