# Common Pitfalls writing scalable services in ASP.NET Core

This document serves as a guide for writing scalable services in ASP.NET Core. Some of the guidance is general purpose but will be explained through the lens of writing 
web services. The examples shown here are based on experiences with customer applications and issues found on Github and Stack Overflow. Besides general guidance,
this repository will have guides on how to diagnose common issue in the various tools available (WinDbg, lldb, sos, Visual Studio, PerfView etc).


## Asynchronous Programming

Asynchronous programming has been around for several years on the .NET platform but has historically been very difficult to do well. Since the introduction of async/await
in C# 5 asynchronous programming has become mainstream. Modern frameworks (like ASP.NET Core) are fully asynchronous and it's very hard to avoid the async keyword when writing
web services. As a result, there's been lots of confusion on the best practices for async and how to use it properly. Lets start with some of the basic rules:

### Asynchrony is viral 

Once you go async, all of your callers **MUST** be async, there's no good way gradually migrate callers to be async. It's all or nothing (very much like generics).

❌ **BAD** This example uses the `Task.Result` and as a result blocks the current thread to wait for the result. This is an example of [sync over async](#avoid-using-taskresult-and-taskwait).

```C#
public async int DoSomethingAsync()
{
    var result = CallDependencyAsync().Result
    return result + 1;
}
```

✔️**GOOD** This example uses the await keyword to get the result from `CallDependencyAsync`.

```C#
public async Task<int> DoSomethingAsync()
{
    var result = await CallDependencyAsync();
    return result + 1;
}
```

### Async void

Use of async void in ASP.NET Core applications is *ALWAYS* bad. Avoid it, never do it. Typically, it's used when developers are trying to implement fire and forget patterns triggered by a controller action. Async void methods will crash the process if an exception is thrown. We'll look at more of the patterns that cause developers to do this in ASP.NET Core applications but here's a simple example:

❌ **BAD** Async void methods can't be tracked and therefore unhandled exceptions can result in application crashes.

```C#
public class MyController : Controller
{
    [HttpPost("/start")]
    public IActionResult Post()
    {
        BackgroundOperationAsync();
        return Accepted();
    }
    
    public async void BackgroundOperationAsync()
    {
        var result = await CallDependencyAsync();
        return result + 1;
    }
}
```

✔️**GOOD** Task returning methods are better since unhandled exceptions trigger the TaskScheduler.UnobservedTaskException.

```C#
public class MyController : Controller
{
    [HttpPost("/start")]
    public IActionResult Post()
    {
        Task.Run(BackgroundOperationAsync);
        return Accepted();
    }
    
    public async Task BackgroundOperationAsync()
    {
        var result = await CallDependencyAsync();
        return result + 1;
    }
}
```

### Avoid using Task.Result and Task.Wait

There are very few ways to use Task.Result and Task.Wait correctly so the general advice is to completely avoid using them in your code. 

#### :warning: Sync over async

Using Task.Result or Task.Wait to block wait on an asynchronous operation to complete is *MUCH* worse than calling a truly synchronous API to block. This phenomenon is dubbed "Sync over async". Here is what happens at a very high level:

- An asynchronous operation is kicked off.
- The calling thread is blocked waiting for that operation to complete.
- When the asynchronous operation completes, it schedules a continuation to the thread pool to resume the code waiting on that operation.

The result is that we need to use 2 threads instead of 1 to complete synchronous operations. This usually leads to [thread pool starvation](https://blogs.msdn.microsoft.com/vancem/2018/10/16/diagnosing-net-core-threadpool-starvation-with-perfview-why-my-service-is-not-saturating-all-cores-or-seems-to-stall/) and results in service outages.

#### :warning: Deadlocks

The `SynchronizationContext` is an abstraction that gives application models a chance to control where asynchronous continuations run. ASP.NET (non-core), WPF and Windows Forms each have an implementation that will result in a deadlock if Task.Wait or Task.Result is used on the main thread. This behavior has lead to a bunch of "clever" code snippets that show the "right" way to block waiting for a Task. The truth is, there's is no good way to block waiting for a Task to complete.

❌ **BAD** The below are all examples that are trying to avoid the dead lock situation but still succumb to "sync over async" problems.

```C#
public string DoOperationBlocking()
{
    return Task.Run(() => DoAsyncOperation()).Result;
}

public string DoOperationBlocking2()
{
    return Task.Run(() => DoAsyncOperation()).GetAwaiter().GetResult();
}

public string DoOperationBlocking3()
{
    return Task.Run(() => DoAsyncOperation().Result).Result;
}

public string DoOperationBlocking4()
{
    return Task.Run(() => DoAsyncOperation().GetAwaiter().GetResult()).GetAwaiter().GetResult();
}

public string DoOperationBlocking5()
{
    return DoAsyncOperation().Result;
}

public string DoOperationBlocking6()
{
    return DoAsyncOperation().GetAwaiter().GetResult();
}

public string DoOperationBlocking7()
{
    var task = DoAsyncOperation();
    task.Wait();
    return task.GetAwaiter().GetResult();
}
```

### Prefer await over ContinueWith

`Task` existed before the async/await keywords were introduced and as such provided ways to execute continuations without a reliance the language. Although these
methods are still valid to use, we generally recommend that you prefer async/await to using ContinueWith. ContinueWith also does not capture the `SynchronizationContext` and as a result is actually semantically different to async/await.

❌ **BAD** The example uses ContinueWith instead of async

```C#
public async Task<int> DoSomethingAsync()
{
    return CallDependencyAsync().ContinueWith(task =>
    {
        return task.Result + 1;
    });
}
```

✔️**GOOD** This example uses the await keyword to get the result from `CallDependencyAsync`.

```C#
public async Task<int> DoSomethingAsync()
{
    var result = await CallDependencyAsync();
    return result + 1;
}
```

### Always create TaskCompletionSource with TaskCreationOptions.RunContinuationsAsynchronously

