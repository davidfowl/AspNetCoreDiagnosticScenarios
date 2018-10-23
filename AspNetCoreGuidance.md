# ASP.NET Core Guidance

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

✔️**GOOD** This example uses `StreamReader.ReadToEndAsync` 

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

## Prefer logging scopes with data over passing the HttpContext values into loggers directly

## Do not capture the HttpContext in background threads

## Do not capture services injected into the controllers on background threads

## Avoding writing to the response body after middleware pipeline executes
