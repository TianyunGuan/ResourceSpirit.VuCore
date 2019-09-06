using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using ResourceSpirit.WebApi.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace ResourceSpirit.WebApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            ////No constructor for type 'Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenerator' can be instantiated using services from the service container and default values.
            services.AddMvcCore().AddApiExplorer();//防止上面注释的错误

            #region Configure API Version

            services.AddApiVersioning(o =>
            {
                o.ReportApiVersions = true;//return versions in a response header
                o.DefaultApiVersion = new ApiVersion(1, 0);//default version select 
                o.AssumeDefaultVersionWhenUnspecified = true;//if not specifying an api version,show the default version
            }).AddVersionedApiExplorer(option =>
            {
                option.GroupNameFormat = "'v'VVVV";//api group name
                option.AssumeDefaultVersionWhenUnspecified = true;//whether provide a service API version
            });

            #endregion

            #region Configure Swagger

            services.AddSwaggerGen(s =>
            {
                //Generate api description doc
                //
                var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

                foreach (var description in provider.ApiVersionDescriptions)
                {
                    s.SwaggerDoc(description.GroupName, new Info
                    {
                        Contact = new Contact
                        {
                            Name = "Danvic Wang",
                            Email = "danvic96@hotmail.com",
                            Url = "https://yuiter.com"
                        },
                        Description = "A front-background project build by ASP.NET Core 2.2 and Vue",
                        Title = "Grapefruit.VuCore",
                        Version = description.ApiVersion.ToString(),
                        License = new License
                        {
                            Name = "MIT",
                            Url = "https://mit-license.org/"
                        }
                    });
                }

                //Show the api version in url address
                s.DocInclusionPredicate((version, apiDescription) =>
                {
                    if (!version.Equals(apiDescription.GroupName))
                        return false;

                    var values = apiDescription.RelativePath
                        .Split('/')
                        .Select(v => v.Replace("v{version}", apiDescription.GroupName));

                    apiDescription.RelativePath = string.Join("/", values);
                    return true;
                });

                //Remove version parameter
                s.OperationFilter<RemoveVersionFromParameter>();

                //Add comments description
                //
                var basePath = Path.GetDirectoryName(AppContext.BaseDirectory);//get application located directory
                var apiPath = Path.Combine(basePath, "ResourceSpirit.WebApi.xml");
                //var dtoPath = Path.Combine(basePath, "ResourceSpirit.Application.xml");
                s.IncludeXmlComments(apiPath, true);
                //s.IncludeXmlComments(dtoPath, true);

                //Add Jwt Authorize to http header
                s.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",//Jwt default param name
                    In = "header",//Jwt store address
                    Type = "apiKey"//Security scheme type
                });
                //Add authentication type
                s.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "Bearer", new string[] { } }
                });
            });

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            #region Enable Swagger

            app.UseSwagger(o =>
            {
                o.PreSerializeFilters.Add((document, request) =>
                {
                    document.Paths = document.Paths.ToDictionary(p => p.Key.ToLowerInvariant(), p => p.Value);
                });
            });

            app.UseSwaggerUI(s =>
            {
                //Default to show the latest api docs
                foreach (var description in provider.ApiVersionDescriptions.Reverse())
                {
                    s.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                        $"Grapefruit.VuCore API {description.GroupName.ToUpperInvariant()}");
                }
            });

            #endregion
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
