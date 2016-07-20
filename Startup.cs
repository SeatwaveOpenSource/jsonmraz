using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
// ReSharper disable All

namespace jsonmraz
{
    public class Startup
    {

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

            string[] path = new string[] {};
            string root = null;
            dynamic json = null;

            app.Use(async (context, next) => {
                path = context.Request.Path.Value.Split('/');
                root = path[1];

                if (string.IsNullOrWhiteSpace(root)) return;
                json = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText($"json/{root}.json"));

                await next();
            });

            app.MapWhen(context => context.Request.QueryString.Value.Contains("="), _app => {
                _app.Run(async context => {
                    await context.Response.WriteAsync("set stuff");
                });
            });

            app.Run(async context => {

                if (string.IsNullOrWhiteSpace(root)) return;

                foreach (var pathSection in path) {
                    if (string.IsNullOrWhiteSpace(pathSection) || pathSection == root) continue;
                    foreach (var jsonObj in json) {
                        json = jsonObj.Name == pathSection ? jsonObj.Value : json;
                    }
                }

                await context.Response.WriteAsync($"{JsonConvert.SerializeObject(json)}");
            });
        }

        public static void Main(string[] args) => new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Startup>()
            .Build().Run();
    }
}
