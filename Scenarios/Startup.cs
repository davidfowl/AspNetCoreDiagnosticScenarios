using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scenarios.Services;

namespace Scenarios
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
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

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, PokemonDbContext context)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseFileServer();

            app.UseMvc();

            context.Database.EnsureCreated();
        }
    }
}
