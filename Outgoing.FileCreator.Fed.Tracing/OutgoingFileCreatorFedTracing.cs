﻿using DBHelper;
using FileBroker.Business;
using FileBroker.Common;
using FileBroker.Model.Interfaces;
using FOAEA3.Model;
using FOAEA3.Resources.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Outgoing.FileCreator.Fed.Tracing;

public static class OutgoingFileCreatorFedTracing
{
    public static async Task Run(string[] args = null)
    {
        var consoleOut = Console.Out;
        using (var textOut = new StreamWriter(new FileStream("log.txt", FileMode.Append)))
        {
            args ??= Array.Empty<string>();

            var config = new FileBrokerConfigurationHelper(args);

            if (config.LogConsoleOutputToFile)
                Console.SetOut(textOut);
            Console.WriteLine($"*** Started {AppDomain.CurrentDomain.FriendlyName}.exe: {DateTime.Now}");
            ColourConsole.WriteEmbeddedColorLine("Starting Federal Outgoing Tracing Files Creator");

            string aspnetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            ColourConsole.WriteEmbeddedColorLine($"Using Environment: [yellow]{aspnetCoreEnvironment}[/yellow]");
            ColourConsole.WriteEmbeddedColorLine($"FTProot: [yellow]{config.FTProot}[/yellow]");
            ColourConsole.WriteEmbeddedColorLine($"FTPbackupRoot: [yellow]{config.FTPbackupRoot}[/yellow]");
            ColourConsole.WriteEmbeddedColorLine($"Audit Root Path: [yellow]{config.AuditConfig.AuditRootPath}[/yellow]");

            var fileBrokerDB = new DBToolsAsync(config.FileBrokerConnection);

            await CreateOutgoingFederalTracingFiles(fileBrokerDB, config.ApiRootData, config);

            ColourConsole.WriteLine("Completed.");
            Console.WriteLine($"*** Ended: {DateTime.Now}\n");
        }
        Console.SetOut(consoleOut);
    }

    private static async Task CreateOutgoingFederalTracingFiles(DBToolsAsync fileBrokerDB, ApiConfig apiRootData,
                                                                IFileBrokerConfigurationHelper config)
    {

        var foaeaApis = FoaeaApiHelper.SetupFoaeaAPIs(apiRootData);
        var db = DataHelper.SetupFileBrokerRepositories(fileBrokerDB);

        var federalFileManager = new OutgoingFederalTracingManager(foaeaApis, db, config);

        var federalTraceOutgoingSources = (await db.FileTable.GetFileTableDataForCategory("TRCOUT"))
                                            .Where(s => s.Active == true);

        foreach (var federalTraceOutgoingSource in federalTraceOutgoingSources)
        {
            var errors = new List<string>();
            (string filePath, errors) = await federalFileManager.CreateOutputFile(federalTraceOutgoingSource.Name);
            if (errors.Count == 0)
            {
                if (!string.IsNullOrEmpty(filePath))
                    ColourConsole.WriteEmbeddedColorLine($"Successfully created [cyan]{filePath}[/cyan]");
                else
                    ColourConsole.WriteEmbeddedColorLine($"Skipped [cyan]{federalTraceOutgoingSource.Name}[/cyan]. No data/events were found.");
            }
            else
                foreach (var error in errors)
                {
                    ColourConsole.WriteEmbeddedColorLine($"Error creating [cyan]{federalTraceOutgoingSource.Name}[/cyan]: [red]{error}[/red]");
                    await db.ErrorTrackingTable.MessageBrokerError("TRCOUT", federalTraceOutgoingSource.Name,
                                                                   new Exception(error), displayExceptionError: true);
                }
        }

    }
}
