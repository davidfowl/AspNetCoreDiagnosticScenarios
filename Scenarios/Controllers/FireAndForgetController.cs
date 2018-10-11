using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scenarios.Model;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Scenarios.Controllers
{
    public class FireAndForgetController : Controller
    {
        [HttpGet("/fire-and-forget-1")]
        public IActionResult FireAndForget1([FromServices]PokemonDbContext context)
        {
            // This is async void!
            ThreadPool.QueueUserWorkItem(async state =>
            {
                await Task.Delay(1000);

                context.Pokemon.Add(new Pokemon());
                await context.SaveChangesAsync();
            });

            return Accepted();
        }


        [HttpGet("/fire-and-forget-2")]
        public IActionResult FireAndForget2([FromServices]PokemonDbContext context)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);

                context.Pokemon.Add(new Pokemon());
                await context.SaveChangesAsync();
            });

            return Accepted();
        }

        [HttpGet("/fire-and-forget-3")]

        public IActionResult FireAndForget3([FromServices]IServiceScopeFactory serviceScopeFactory)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);

                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("Background Task");

                    // This captures the HTTP context!
                    using (logger.BeginScope("Background operation kicked off from {RequestId}", HttpContext.TraceIdentifier))
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

        [HttpGet("/fire-and-forget-4")]
        public IActionResult FireAndForget4([FromServices]IServiceScopeFactory serviceScopeFactory)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);

                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

                    var logger = loggerFactory.CreateLogger("Background Task");

                    // This uses the HTTP context after the request AFTER the request as ended!
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

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);

                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

                    var logger = loggerFactory.CreateLogger("Background Task");

                    // This uses the HTTP context after the request AFTER the request as ended!
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
