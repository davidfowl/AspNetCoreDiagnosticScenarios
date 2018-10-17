using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scenarios.Model;

namespace Scenarios.Controllers
{
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


        [HttpGet("/fire-and-forget-2")]
        public IActionResult FireAndForget2([FromServices]PokemonDbContext context)
        {
            // This uses Task.Run instead of ThreadPool.QueueUserWorkItem. It's mostly equivalent to the FireAndForget1 but since we're using 
            // async Task instead of async void, unhandled exceptions won't crash the process. They will however trigger the TaskScheduler.UnobservedTaskException
            // event when exceptions go unhandled.
            Task.Run(async () =>
            {
                await Task.Delay(1000);

                // This closure is capturing the context from the Controller action parameter. This is bad because this work item could run
                // outside of the request scope and the PokemonDbContext is scoped to the request. As a result, this will throw an unhandled ObjectDisposedException.
                context.Pokemon.Add(new Pokemon());
                await context.SaveChangesAsync();
            });

            return Accepted();
        }

        [HttpGet("/fire-and-forget-3")]

        public IActionResult FireAndForget3([FromServices]IServiceScopeFactory serviceScopeFactory)
        {
            // This version of fire and forget adds some exception handling. We're also no longer capturing the PokemonDbContext from the incoming request.
            // Instead, we're injecting an IServiceScopeFactory (which is a singleton) in order to create a scope in the background work item.
            Task.Run(async () =>
            {
                await Task.Delay(1000);

                // Create a scope for the lifetime of the background operation and resolve services from it
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("Background Task");

                    // THIS IS DANGEROUS! We're capturing the HttpContext from the incoming request in the closure that
                    // runs the background work item. This will not work because the incoming http request will be over before
                    // the work item executes.
                    using (logger.BeginScope("Background operation kicked off from {RequestId}", HttpContext.TraceIdentifier))
                    {
                        try
                        {
                            // This will a PokemonDbContext from the correct scope and the operation will succeed
                            var context = scope.ServiceProvider.GetRequiredService<PokemonDbContext>();

                            context.Pokemon.Add(new Pokemon());
                            await context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Background task failed.");
                        }
                    }
                }
            });

            return Accepted();
        }

        [HttpGet("/fire-and-forget-4")]
        public IActionResult FireAndForget4([FromServices]IServiceScopeFactory serviceScopeFactory)
        {
            Task.Run(async () =>
            {
                await Task.Delay(1000);

                using (var scope = serviceScopeFactory.CreateScope())
                {
                    // Instead of capturing the HttpContext from the controller property, we use the IHttpContextAccessor
                    var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

                    var logger = loggerFactory.CreateLogger("Background Task");

                    // THIS IS DANGEROUS! We're trying to use the HttpContext from the incoming request in the closure that
                    // runs the background work item. This will not work because the incoming http request will be over before
                    // the work item executes.
                    using (logger.BeginScope("Background operation kicked off from {RequestId}", accessor.HttpContext.TraceIdentifier))
                    {
                        try
                        {

                            var context = scope.ServiceProvider.GetRequiredService<PokemonDbContext>();

                            context.Pokemon.Add(new Pokemon());
                            await context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Background task failed.");
                        }
                    }
                }
            });

            return Accepted();
        }

        [HttpGet("/fire-and-forget-5")]
        public IActionResult FireAndForget5([FromServices]IServiceScopeFactory serviceScopeFactory)
        {
            // Capture the trace identifier first
            string traceIdenifier = HttpContext.TraceIdentifier;

            Task.Run(async () =>
            {
                await Task.Delay(1000);

                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

                    var logger = loggerFactory.CreateLogger("Background Task");

                    // This uses the traceIdenifier captured at the time the request started.
                    using (logger.BeginScope("Background operation kicked off from {RequestId}", traceIdenifier))
                    {
                        try
                        {

                            var context = scope.ServiceProvider.GetRequiredService<PokemonDbContext>();

                            context.Add(new Pokemon());
                            await context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Background task failed.");
                        }
                    }
                }
            });

            return Accepted();
        }
    }
}
