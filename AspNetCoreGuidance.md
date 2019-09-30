# Table of contents
 - [ASP.NET Core Guidance](#aspnet-core-guidance)
   - [Avoid using synchronous Read/Write overloads on HttpRequest.Body and HttpResponse.Body](#avoid-using-synchronous-readwrite-overloads-on-httprequestbody-and-httpresponsebody)
   - [Prefer using HttpRequest.ReadAsFormAsync() over HttpRequest.Form](#prefer-using-httprequestreadasformasync-over-httprequestform)
   - [Use buffered and synchronous reads and writes as an alternative to asynchronous reading and writing](#use-buffered-and-synchronous-reads-and-writes-as-an-alternative-to-asynchronous-reading-and-writing)
   - [Avoid reading large request bodies or response bodies into memory](#avoid-reading-large-request-bodies-or-response-bodies-into-memory)
   - [Do not store IHttpContextAccessor.HttpContext in a field](#do-not-store-ihttpcontextaccessorhttpcontext-in-a-field)
   - [Do not access the HttpContext from multiple threads in parallel. It is not thread safe.](#do-not-access-the-httpcontext-from-multiple-threads-in-parallel-it-is-not-thread-safe)
   - [Do not use the HttpContext after the request is complete](#do-not-use-the-httpcontext-after-the-request-is-complete)
   - [Do not capture the HttpContext in background threads](#do-not-capture-the-httpcontext-in-background-threads)
   - [Do not capture services injected into the controllers on background threads](#do-not-capture-services-injected-into-the-controllers-on-background-threads)
   - [Avoid adding headers after the HttpResponse has started](#avoid-adding-headers-after-the-httpresponse-has-started)

# ASP.NET Core Guidance

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
        // This asynchronously reads the entire http request body into memory.
        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        return JsonConvert.DeserializeObject<PokemonData>(json);
    }
}
```

:bulb:**NOTE: If the request is large it could lead to out of memory problems which can result in a Denial Of Service. See [this](#avoid-reading-large-request-bodies-or-response-bodies-into-memory) for more information.**

## Prefer using HttpRequest.ReadAsFormAsync() over HttpRequest.Form

You should always prefer `HttpRequest.ReadAsFormAsync()` over `HttpRequest.Form`. The only time it is safe to use `HttpRequest.Form` is the form has already been read by a call to `HttpRequest.ReadAsFormAsync()` and the cached form value is being read using `HttpRequest.Form`. 

❌ **BAD** This example uses HttpRequest.Form uses [sync over async](AsyncGuidance.md#avoid-using-taskresult-and-taskwait) under the covers and can lead to thread pool starvation (in some cases).

```C#
public class MyController : Controller
{
    [HttpPost("/form-body")]
    public IActionResult Post()
    {
        var form = HttpRequest.Form;
        
        Process(form["id"], form["name"]);

        return Accepted();
    }
}
```

:white_check_mark: **GOOD** This example uses `HttpRequest.ReadAsFormAsync()` to read the form body asynchronously.

```C#
public class MyController : Controller
{
    [HttpPost("/form-body")]
    public async Task<IActionResult> Post()
    {
        var form = await HttpRequest.ReadAsFormAsync();
        
        Process(form["id"], form["name"]);

        return Accepted();
    }
}
```

## Avoid reading large request bodies or response bodies into memory

In .NET any single object allocation greater than 85KB ends up in the large object heap ([LOH](https://blogs.msdn.microsoft.com/maoni/2006/04/19/large-object-heap/)). Large objects are expensive in 2 ways:

- The allocation cost is high because the memory for a newly allocated large object has to be cleared (the CLR guarantees that memory for all newly allocated objects is cleared)
- LOH is collected with the rest of the heap (it requires a "full garbage collection" or Gen2 collection)

This [blog post](https://adamsitnik.com/Array-Pool/#the-problem) describes the problem succinctly:

> When a large object is allocated, it’s marked as Gen 2 object. Not Gen 0 as for small objects. The consequences are that if you run out of memory in LOH, GC cleans up whole managed heap, not only LOH. So it cleans up Gen 0, Gen 1 and Gen 2 including LOH. This is called full garbage collection and is the most time-consuming garbage collection. For many applications, it can be acceptable. But definitely not for high-performance web servers, where few big memory buffers are needed to handle an average web request (read from a socket, decompress, decode JSON & more).

Naively storing a large request or response body into a single `byte[]` or `string` may result in quickly running out of space in the LOH and may cause performance issues for your application because of full GCs running. 

## Use buffered and synchronous reads and writes as an alternative to asynchronous reading and writing

When using a serializer/de-serializer that only supports synchronous reads and writes (like JSON.NET) then prefer buffering the data into memory before passing data into the serializer/de-serializer.

:bulb:**NOTE: If the request is large it could lead to out of memory problems which can result in a Denial Of Service. See [this](#avoid-reading-large-request-bodies-or-response-bodies-into-memory) for more information.**

## Do not store IHttpContextAccessor.HttpContext in a field

The `IHttpContextAccessor.HttpContext` will return the `HttpContext` of the active request when accessed from the request thread. It should not be stored in a field or variable.

❌ **BAD** This example stores the HttpContext in a field then attempts to use it later.

```C#
public class MyType
{
    private readonly HttpContext _context;
    public MyType(IHttpContextAccessor accessor)
    {
        _context = accessor.HttpContext;
    }
    
    public void CheckAdmin()
    {
        if (!_context.User.IsInRole("admin"))
        {
            throw new UnauthorizedAccessException("The current user isn't an admin");
        }
    }
}
```

The above logic will likely capture a null or bogus HttpContext in the constructor for later use.

:white_check_mark: **GOOD** This example stores the IHttpContextAccesor itself in a field and uses the HttpContext field at the correct time (checking for null).

```C#
public class MyType
{
    private readonly IHttpContextAccessor _accessor;
    public MyType(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }
    
    public void CheckAdmin()
    {
        var context = _accessor.HttpContext;
        if (context != null && !context.User.IsInRole("admin"))
        {
            throw new UnauthorizedAccessException("The current user isn't an admin");
        }
    }
}
```

## Do not access the HttpContext from multiple threads in parallel. It is not thread safe.

The `HttpContext` is *NOT* threadsafe. Accessing it from multiple threads in parallel can cause corruption resulting in undefined behavior (hangs, crashes, data corruption).

❌ **BAD** This example makes 3 parallel requests and logs the incoming request path before and after the outgoing http request. This accesses the request path from multiple threads potentially in parallel.

```C#
public class AsyncController : Controller
{
    [HttpGet("/search")]
    public async Task<SearchResults> Get(string query)
    {
        var query1 = SearchAsync(SearchEngine.Google, query);
        var query2 = SearchAsync(SearchEngine.Bing, query);
        var query3 = SearchAsync(SearchEngine.DuckDuckGo, query);

        await Task.WhenAll(query1, query2, query3);
        
        var results1 = await query1;
        var results2 = await query2;
        var results3 = await query3;

        return SearchResults.Combine(results1, results2, results3);
    }

    private async Task<SearchResults> SearchAsync(SearchEngine engine, string query)
    {
        var searchResults = SearchResults.Empty;
        try
        {
            _logger.LogInformation("Starting search query from {path}.", HttpContext.Request.Path);
            searchResults = await _searchService.SearchAsync(engine, query);
            _logger.LogInformation("Finishing search query from {path}.", HttpContext.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed query from {path}", HttpContext.Request.Path);
        }

        return searchResults;
    }
}
```

:white_check_mark: **GOOD** This example copies all data from the incoming request before making the 3 parallel requests.

```C#
public class AsyncController : Controller
{
    [HttpGet("/search")]
    public async Task<SearchResults> Get(string query)
    {
        string path = HttpContext.Request.Path;
        var query1 = SearchAsync(SearchEngine.Google, query, path);
        var query2 = SearchAsync(SearchEngine.Bing, query, path);
        var query3 = SearchAsync(SearchEngine.DuckDuckGo, query, path);

        await Task.WhenAll(query1, query2, query3);
        
        var results1 = await query1;
        var results2 = await query2;
        var results3 = await query3;

        return SearchResults.Combine(results1, results2, results3);
    }

    private async Task<SearchResults> SearchAsync(SearchEngine engine, string query, string path)
    {
        var searchResults = SearchResults.Empty;
        try
        {
            _logger.LogInformation("Starting search query from {path}.", path);
            searchResults = await _searchService.SearchAsync(engine, query);
            _logger.LogInformation("Finishing search query from {path}.", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed query from {path}", path);
        }

        return searchResults;
    }
}
```

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

❌ **BAD** This example shows a closure is capturing the HttpContext from the Controller property. This is bad because this work item could run
outside of the request scope and as a result, could lead to reading a bogus HttpContext.

```C#
[HttpGet("/fire-and-forget-1")]
public IActionResult FireAndForget1()
{
    _ = Task.Run(() =>
    {
        await Task.Delay(1000);

        // This closure is capturing the context from the Controller property. This is bad because this work item could run
        // outside of the http request leading to reading of bogus data.
        var path = HttpContext.Request.Path;
        Log(path);
    });

    return Accepted();
}
```


:white_check_mark: **GOOD** This example copies the data required in the background task during the request explictly and does not reference
anything from the controller itself.

```C#
[HttpGet("/fire-and-forget-3")]
public IActionResult FireAndForget3()
{
    string path = HttpContext.Request.Path;
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000);

        // This captures just the path
        Log(path);
    });

    return Accepted();
}
```

## Do not capture services injected into the controllers on background threads

❌ **BAD** This example shows a closure is capturing the DbContext from the Controller action parameter. This is bad because this work item could run
outside of the request scope and the PokemonDbContext is scoped to the request. As a result, this will end up with an ObjectDisposedException.

```C#
[HttpGet("/fire-and-forget-1")]
public IActionResult FireAndForget1([FromServices]PokemonDbContext context)
{
    _ = Task.Run(() =>
    {
        await Task.Delay(1000);

        // This closure is capturing the context from the Controller action parameter. This is bad because this work item could run
        // outside of the request scope and the PokemonDbContext is scoped to the request. As a result, this throw an ObjectDisposedException
        context.Pokemon.Add(new Pokemon());
        await context.SaveChangesAsync();
    });

    return Accepted();
}
```

:white_check_mark: **GOOD** This example injects an `IServiceScopeFactory` and creates a new dependency injection scope in the background thread and does not reference
anything from the controller itself.

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
