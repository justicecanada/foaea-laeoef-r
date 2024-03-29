﻿using DBHelper;
using FileBroker.Business;
using FileBroker.Common;
using FileBroker.Model.Interfaces;
using FOAEA3.Model;
using FOAEA3.Resources.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Outgoing.FileCreator.Fed.SIN;

public static class OutgoingFileCreatorFedSIN
{
    public static async Task Run(string[] args = null)
    {
        args ??= Array.Empty<string>();

        ColourConsole.WriteEmbeddedColorLine("Starting Federal Outgoing SIN File Creator");

        var config = new FileBrokerConfigurationHelper(args);

        var fileBrokerDB = new DBToolsAsync(config.FileBrokerConnection);

        await CreateOutgoingFederalSinFile(fileBrokerDB, config.ApiRootData, config);

        ColourConsole.Write("Completed.\n");
    }

    private static async Task CreateOutgoingFederalSinFile(DBToolsAsync fileBrokerDB, ApiConfig apiRootData,
                                                        IFileBrokerConfigurationHelper config)
    {
        var foaeaApis = FoaeaApiHelper.SetupFoaeaAPIs(apiRootData);

        var db = DataHelper.SetupFileBrokerRepositories(fileBrokerDB);

        var federalFileManager = new OutgoingFederalSinManager(foaeaApis, db, config);

        var federalSinOutgoingSources = (await db.FileTable.GetFileTableDataForCategory("SINOUT"))
                                          .Where(s => s.Active == true);

        foreach (var federalSinOutgoingSource in federalSinOutgoingSources)
        {
            var errors = new List<string>();
            (string filePath, errors) = await federalFileManager.CreateOutputFile(federalSinOutgoingSource.Name);

            if (errors.Count == 0)
                ColourConsole.WriteEmbeddedColorLine($"Successfully created [cyan]{filePath}[/cyan]");
            else
                foreach (var error in errors)
                {
                    ColourConsole.WriteEmbeddedColorLine($"Error creating [cyan]{federalSinOutgoingSource.Name}[/cyan]: [red]{error}[/red]");
                    await db.ErrorTrackingTable.MessageBrokerError("SINOUT", federalSinOutgoingSource.Name,
                                                                               new Exception(error), displayExceptionError: true);
                }
        }

    }
}
