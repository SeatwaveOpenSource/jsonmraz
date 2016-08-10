using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace jsonmraz
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

            string[] path = { };
            string root = null;
            dynamic json = null;

            app.Use(async (context, next) => {
                path = context.Request.Path.Value.Split('/');
                root = path[1];
                if (string.IsNullOrWhiteSpace(root) || root.Contains("favicon.ico")) return;
                json = JsonConvert.DeserializeObject(File.ReadAllText($"json/{root}.json"));
                await next();
            });

            app.MapWhen(context => context.Request.QueryString.Value.Contains("set"), _app => {
                _app.Run(async context => {
                    dynamic _json = findJsonObject(json, path, root);

                    var propertyName = context.Request.Query.Single(x => x.Key == "set").Value[0];
                    var propertyValue = context.Request.Query.Single(x => x.Key == "value").Value[0];

                    var type = json.SelectToken($"{(_json as JObject)?.Path}")[propertyName].Value.GetType();

                    json.SelectToken($"{(_json as JObject)?.Path}")[propertyName] = Convert.ChangeType(propertyValue, type);

                    var jsonOutput = JsonConvert.SerializeObject(json);

                    File.WriteAllText($"json/{root}.json", jsonOutput);

                    await context.Response.WriteAsync($"{jsonOutput}");
                });
            });

            app.Run(async context => {
                var jsonObj = findJsonObject(json, path, root);
                await context.Response.WriteAsync($"{JsonConvert.SerializeObject(jsonObj)}");
            });
        }

        static dynamic findJsonObject(dynamic json, string[] path, string root)
        {
            var _json = json;
            foreach (var pathSection in path) {

                if (string.IsNullOrWhiteSpace(pathSection) || pathSection == root) continue;
                foreach (var jsonObj in _json) {
                    if (jsonObj.Name != pathSection) continue;
                    _json = jsonObj.Value;
                }
            }
            return _json;
        }

        public static void Main(string[] args) => new WebHostBuilder()
            .UseConfiguration
            (
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("hosting.json", optional: true)
                    .Build()
            )
            .UseKestrel()
            .UseIISIntegration()
            .UseStartup<Startup>()
            .Build()
            .Run();
    }
}
