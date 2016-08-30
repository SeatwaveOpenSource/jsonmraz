using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace jsonmraz
{
    public class Startup
    {
        static IConfigurationRoot Configuration { get; set; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

            string[] path = { };
            string root = null;
            dynamic _JSON_ = null;

            app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.Use(async (context, next) =>
            {
                path = context.Request.Path.Value.Split('/');
                root = path[1];
                if (string.IsNullOrWhiteSpace(root) || root.Contains("favicon.ico")) return;

                var jsonLocation = Configuration.GetSection("json.location").Value;
                var actualJsonLocation = !string.IsNullOrWhiteSpace(jsonLocation) ? jsonLocation : "json/"; //ERRGH!

                _JSON_ = JsonConvert.DeserializeObject(File.ReadAllText($"{actualJsonLocation}{root}.json"));

                await next();
            });

            app.MapWhen(context => context.Request.Method == HttpMethod.Post.Method, _app =>
            {
                _app.Run(async context =>
                {
                    Object updatedObject;
                    using (var sr = new StreamReader(context.Request.Body))
                        updatedObject = JsonConvert.DeserializeObject<Object>(sr.ReadToEnd());

                    dynamic objectToUpdate = FindJsonObject(_JSON_, path, root);

                    var type = _JSON_.SelectToken($"{(objectToUpdate as JObject)?.Path}")[updatedObject.key].Value.GetType();

                    _JSON_.SelectToken($"{(objectToUpdate as JObject)?.Path}")[updatedObject.key] = Convert.ChangeType(updatedObject.value, type);

                    var jsonToWrite = JsonConvert.SerializeObject(_JSON_);

                    await File.WriteAllText($"json/{root}.json", jsonToWrite);
                });
            });

            app.Run(async context =>
            {
                var jsonObj = FindJsonObject(_JSON_, path, root);
                await context.Response.WriteAsync($"{JsonConvert.SerializeObject(jsonObj)}");
            });
        }

        public class Object
        {
            public string key { get; set; }
            public object value { get; set; }
        }

        static dynamic FindJsonObject(dynamic json, string[] path, string root)
        {
            var _json = json;
            foreach (var pathSection in path)
            {
                if (string.IsNullOrWhiteSpace(pathSection) || pathSection == root) continue;
                foreach (var jsonObj in _json)
                {
                    if (jsonObj.Name != pathSection && (jsonObj as JObject)?.Path != $"[{pathSection}]") continue;
                    _json = jsonObj.Value ?? jsonObj;
                }
            }
            return _json;
        }

        public static void Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("setup.json", optional: true)
                .Build();

            new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls(Configuration.GetSection("server.urls").Value)
                .UseIISIntegration()
                .Build()
                .Run();
        }
    }
}
