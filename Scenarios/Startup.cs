using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Scenarios.Services;
using System.Threading;

namespace Scenarios
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddHttpClient<PokemonService>();

            services.AddHttpClient("timeout", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            services.AddHttpContextAccessor();

            services.AddDbContext<PokemonDbContext>(o =>
            {
                o.UseInMemoryDatabase("MyApplication");
            });

            services.AddSingleton(sp =>
            {
                // This is *BAD*, do not try to do asynchronous work in a synchronous callback!
                // This can lead to thread pool starvation.
                return sp.GetRequiredService<RemoteConnectionFactory>().ConnectAsync().Result;
            });

            services.AddSingleton(sp =>
            {
                // This is *BAD*, do not try to do asynchronous work in a synchronous callback!
                // This specific implementation can lead to a dead lock
                return GetLoggingRemoteConnection(sp).Result;
            });

            services.AddSingleton<LazyRemoteConnection>();
            services.AddSingleton<RemoteConnectionFactory>();

            services.AddControllers();
        }

        private async Task<LoggingRemoteConnection> GetLoggingRemoteConnection(IServiceProvider sp)
        {
            // As part of service resolution, we hold a lock on the container
            var connectionFactory = sp.GetRequiredService<RemoteConnectionFactory>();
            var connection = await connectionFactory.ConnectAsync();

            // We've resumed on a different thread and we're about to trigger another call to GetRequiredService.
            // This call requires the same lock so it will wait for the existing service resolution to release it
            // before continuing. 

            // This will result in a dead lock because we're running as part of the original service resolution!
            var logger = sp.GetRequiredService<ILogger<LoggingRemoteConnection>>();

            return new LoggingRemoteConnection(connection, logger);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, PokemonDbContext context)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseFileServer();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Force database seeding to execute
            context.Database.EnsureCreated();
        }
    }
}
