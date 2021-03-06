using DBHelper;
using FOAEA3.Common.Filters;
using FOAEA3.Data.Base;
using FOAEA3.Data.DB;
using FOAEA3.Model;
using FOAEA3.Model.Interfaces;
using FOAEA3.Resources.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FOAEA3.Common
{
    public static class Startup
    {
        public static void ConfigureAPIServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers(options =>
            {
                options.ReturnHttpNotAcceptable = true;
                options.Filters.Add(new ActionAutoLoggerFilter());
            })
               .AddXmlDataContractSerializerFormatters();

            AddDBServices(services, configuration.GetConnectionString("FOAEAMain").ReplaceVariablesWithEnvironmentValues());
            services.Configure<CustomConfig>(configuration.GetSection("CustomConfig"));
        }

        public static void ConfigureAPI(WebApplication app, IWebHostEnvironment env, IConfiguration configuration, string apiName)
        {
            ColourConsole.WriteEmbeddedColorLine($"Starting [cyan]{apiName}[/cyan]...");
            ColourConsole.WriteEmbeddedColorLine($"Using .Net Code Environment = [yellow]{env.EnvironmentName}[/yellow]");

            Log.Information("Starting API {apiName}", apiName);
            Log.Information("Using .Net Code Environment = {ASPNETCORE_ENVIRONMENT}", env.EnvironmentName);
            Log.Information("Machine Name = {MachineName}", Environment.MachineName);

            string currentServer = Environment.MachineName;
            var prodServersSection = configuration.GetSection("ProductionServers");
            var prodServers = prodServersSection.Get<List<string>>();
            for (int i = 0; i < prodServers.Count; i++)
                prodServers[i] = prodServers[i].ReplaceVariablesWithEnvironmentValues();

            if (!env.IsEnvironment("Production"))
            {
                app.UseDeveloperExceptionPage();
            }
            else if (prodServers.Any(prodServer => prodServer.ToLower() == currentServer.ToLower()))
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected fault happened. Try again later");
                    });
                });
            }
            else
            {

                Log.Fatal($"Trying to use Production environment on non-production server {currentServer}. Application stopping!", currentServer);
                Console.WriteLine($"Trying to use Production environment on non-production server {currentServer}");
                Console.WriteLine("Application stopping...");

                Task.Delay(2000).Wait();

                app.Lifetime.StopApplication();
            }

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", apiName + " v1");
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            Console.WriteLine("Loading Reference Data");

            using IServiceScope serviceScope = app.Services.CreateScope();
            var provider = serviceScope.ServiceProvider;
            var repositories = provider.GetRequiredService<IRepositories>();

            ReferenceData.Instance().LoadFoaEvents(new DBFoaMessage(repositories.MainDB));
            ReferenceData.Instance().LoadActiveStatuses(new DBActiveStatus(repositories.MainDB));
            ReferenceData.Instance().LoadGenders(new DBGender(repositories.MainDB));
            ReferenceData.Instance().LoadProvinces(new DBProvince(repositories.MainDB));
            ReferenceData.Instance().LoadMediums(new DBMedium(repositories.MainDB));
            ReferenceData.Instance().LoadLanguages(new DBLanguage(repositories.MainDB));
            ReferenceData.Instance().LoadDocumentTypes(new DBDocumentType(repositories.MainDB));
            ReferenceData.Instance().LoadCountries(new DBCountry(repositories.MainDB));
            ReferenceData.Instance().LoadApplicationReasons(new DBApplicationReason(repositories.MainDB));
            ReferenceData.Instance().LoadApplicationCategories(new DBApplicationCategory(repositories.MainDB));
            ReferenceData.Instance().LoadApplicationLifeStates(new DBApplicationLifeState(repositories.MainDB));
            ReferenceData.Instance().LoadApplicationComments(new DBApplicationComments(repositories.MainDB));

            if (ReferenceData.Instance().Messages.Count == 0)
                ColourConsole.WriteLine("Reference Data Loaded Successfully.");
            else
            {
                ColourConsole.WriteLine("Reference Data Failed to Load !!!", ConsoleColor.Red);
                foreach(var message in ReferenceData.Instance().Messages)
                    ColourConsole.WriteLine($"  {message.Description}", ConsoleColor.Red);
            }

            // var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS").Split(";");
            var api_url = configuration["Urls"];

            ColourConsole.WriteEmbeddedColorLine($"[green]Waiting for API calls...[/green] [yellow]{api_url}[/yellow]\n");

        }

        private static void AddDBServices(IServiceCollection services, string connectionString)
        {
            var mainDB = new DBTools(connectionString);

            services.AddScoped<IDBTools>(m => ActivatorUtilities.CreateInstance<DBTools>(m, mainDB)); // used to display the database name at top of page
            services.AddScoped<IRepositories>(m => ActivatorUtilities.CreateInstance<DbRepositories>(m, mainDB));
            services.AddScoped<IRepositories_Finance>(m => ActivatorUtilities.CreateInstance<DbRepositories_Finance>(m, mainDB)); // to access database procs for finance tables
            services.AddScoped<IFoaEventsRepository>(m => ActivatorUtilities.CreateInstance<DBFoaMessage>(m, mainDB));
            services.AddScoped<IActiveStatusRepository>(m => ActivatorUtilities.CreateInstance<DBActiveStatus>(m, mainDB));
            services.AddScoped<IGenderRepository>(m => ActivatorUtilities.CreateInstance<DBGender>(m, mainDB));
            services.AddScoped<IApplicationCommentsRepository>(m => ActivatorUtilities.CreateInstance<DBApplicationComments>(m, mainDB));
            services.AddScoped<IApplicationLifeStateRepository>(m => ActivatorUtilities.CreateInstance<DBApplicationLifeState>(m, mainDB));

            Log.Information("Using MainDB = {MainDB}", mainDB.ConnectionString);
            ColourConsole.WriteEmbeddedColorLine($"Using Connection: [yellow]{mainDB.ConnectionString}[/yellow]");
        }
    }
}
