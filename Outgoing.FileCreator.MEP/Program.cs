using DBHelper;
using FileBroker.Business;
using FileBroker.Data;
using FileBroker.Data.DB;
using FileBroker.Model.Interfaces;
using FOAEA3.Common.Brokers;
using FOAEA3.Common.Helpers;
using FOAEA3.Model;
using FOAEA3.Resources.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Outgoing.FileCreator.MEP
{
    class Program
    {
        static void Main(string[] args)
        {
            ColourConsole.WriteEmbeddedColorLine("Starting MEP Outgoing File Creator");

            string aspnetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{aspnetCoreEnvironment}.json", optional: true, reloadOnChange: true)
                .AddCommandLine(args);

            IConfiguration configuration = builder.Build();

            var fileBrokerDB = new DBTools(configuration.GetConnectionString("FileBroker").ReplaceVariablesWithEnvironmentValues());
            var apiRootForFiles = configuration.GetSection("APIroot").Get<ApiConfig>();

            bool generateTracingFiles = true;
            bool generateLicencingFiles = true;
            bool generateStatsFiles = true;

            if (args.Length > 0)
            {
                string option = args[0].ToUpper();
                switch (option)
                {
                    case "TRACE_ONLY":
                        generateLicencingFiles = false;
                        generateStatsFiles = false;
                        break;
                    case "LICENCE_ONLY":
                        generateTracingFiles = false;
                        generateStatsFiles = false;
                        break;
                    case "STATS_ONLY":
                        generateLicencingFiles = false;
                        generateTracingFiles = false;
                        break;
                    default:
                        break;
                }
            }

            var applicationApiHelper = new APIBrokerHelper(apiRootForFiles.FoaeaApplicationRootAPI, currentSubmitter: "MSGBRO", currentUser: "MSGBRO");
            var tracingApiHelper = new APIBrokerHelper(apiRootForFiles.FoaeaTracingRootAPI, currentSubmitter: "MSGBRO", currentUser: "MSGBRO");
            var licenceDenialApiHelper = new APIBrokerHelper(apiRootForFiles.FoaeaLicenceDenialRootAPI, currentSubmitter: "MSGBRO", currentUser: "MSGBRO");

            var apiBrokers = new APIBrokerList
            {
                Applications = new ApplicationAPIBroker(applicationApiHelper),
                ApplicationEvents = new ApplicationEventAPIBroker(applicationApiHelper),
                TracingApplications = new TracingApplicationAPIBroker(tracingApiHelper),
                TracingResponses = new TraceResponseAPIBroker(tracingApiHelper),
                TracingEvents = new TracingEventAPIBroker(tracingApiHelper),
                LicenceDenialApplications = new LicenceDenialApplicationAPIBroker(licenceDenialApiHelper),
                LicenceDenialTerminationApplications = new LicenceDenialTerminationApplicationAPIBroker(licenceDenialApiHelper),
                LicenceDenialResponses = new LicenceDenialResponseAPIBroker(licenceDenialApiHelper),
                LicenceDenialEvents = new LicenceDenialEventAPIBroker(licenceDenialApiHelper),
            };

            var repositories = new RepositoryList
            {
                FileTable = new DBFileTable(fileBrokerDB),
                FlatFileSpecs = new DBFlatFileSpecification(fileBrokerDB),
                OutboundAuditDB = new DBOutboundAudit(fileBrokerDB),
                ErrorTrackingDB = new DBErrorTracking(fileBrokerDB),
                ProcessParameterTable = new DBProcessParameter(fileBrokerDB),
                MailServiceDB = new DBMailService(fileBrokerDB)
            };

            if (generateTracingFiles)
                CreateOutgoingProvincialFiles(repositories, "TRCAPPOUT", new OutgoingProvincialTracingManager(apiBrokers, repositories));

            if (generateLicencingFiles)
                CreateOutgoingProvincialFiles(repositories, "LICAPPOUT", new OutgoingProvincialLicenceDenialManager(apiBrokers, repositories));

            if (generateStatsFiles)
                CreateOutgoingProvincialFiles(repositories, "STATAPPOUT", new OutgoingProvincialStatusManager(apiBrokers, repositories));
        }

        private static void CreateOutgoingProvincialFiles(RepositoryList repositories, string category,
                                                          IOutgoingFileManager outgoingProvincialFileManager)
        {
            var provincialOutgoingSources = repositories.FileTable.GetFileTableDataForCategory(category)
                                                                  .Where(s => s.Active == true);

            foreach (var provincialOutgoingSource in provincialOutgoingSources)
            {
                string filePath = outgoingProvincialFileManager.CreateOutputFile(provincialOutgoingSource.Name, out List<string> errors);
                if (errors.Count == 0)
                    ColourConsole.WriteEmbeddedColorLine($"Successfully created [cyan]{filePath}[/cyan]");
                else
                    foreach (var error in errors)
                    {
                        ColourConsole.WriteEmbeddedColorLine($"Error creating [cyan]{provincialOutgoingSource.Name}[/cyan]: [red]{error}[/red]");
                        repositories.ErrorTrackingDB.MessageBrokerError(category, provincialOutgoingSource.Name, 
                                                                        new Exception(error), displayExceptionError: true);
                    }
            }
        }
    }
}
