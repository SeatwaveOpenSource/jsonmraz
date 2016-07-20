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

            app.Map("/set", _app => {
                _app.Run(async context => {
                    await context.Response.WriteAsync("set stuff");
                });
            });

            app.Run(async context => {
                var path = context.Request.Path.Value.Split('/');
                var root = path[1];

                if (string.IsNullOrWhiteSpace(root)) return;

                var json = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText($"json/{root}.json"));

                foreach (var pathSection in path) {

                    if (string.IsNullOrWhiteSpace(pathSection) || pathSection == root) continue;

                    foreach (var jsonObj in json) {
                        json = jsonObj.Name == pathSection ? jsonObj.Value : json;
                    }
                }

                await context.Response.WriteAsync($"{JsonConvert.SerializeObject(json)}");
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
