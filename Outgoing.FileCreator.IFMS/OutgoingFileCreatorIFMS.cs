﻿using DBHelper;
using FileBroker.Business;
using FileBroker.Common;
using FileBroker.Model.Interfaces;
using FOAEA3.Model;
using FOAEA3.Resources.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outgoing.FileCreator.IFMS
{
    public static class OutgoingFileCreatorIFMS
    {
        public static async Task Run(string[] args = null)
        {
            args ??= Array.Empty<string>();

            ColourConsole.WriteEmbeddedColorLine("Starting Federal Outgoing SIN File Creator");

            var config = new FileBrokerConfigurationHelper(args);

            var fileBrokerDB = new DBToolsAsync(config.FileBrokerConnection);

            await CreateOutgoingIFMSAsync(fileBrokerDB, config.ApiRootData, config);

            ColourConsole.Write("Completed.\n");
        }

        private static async Task CreateOutgoingIFMSAsync(DBToolsAsync fileBrokerDB, ApiConfig apiRootData,
                                                          IFileBrokerConfigurationHelper config)
        {
            var foaeaApis = FoaeaApiHelper.SetupFoaeaAPIs(apiRootData);

            var db = DataHelper.SetupFileBrokerRepositories(fileBrokerDB);

            var financialManager = new OutgoingFinancialManager(foaeaApis, db, config);

            var outgoingIFMSdata = (await db.FileTable.GetFileTableDataForCategoryAsync("IFMSFDOUT"))
                                    .Where(s => s.Active == true).ToList();

            if ((outgoingIFMSdata is null) || (outgoingIFMSdata.Count > 1)) 
            {
                ColourConsole.WriteEmbeddedColorLine($"Error creating [cyan]IFMS[/cyan] file: [red]Too many active IFMSFDOUT entries![/red]");
                await db.ErrorTrackingTable.MessageBrokerErrorAsync("IFMSFDOUT", "IFMSFDOUT",
                                                                     new Exception("Too many active IFMSFDOUT entries!"), displayExceptionError: true);
                return;
            }

            var errors = new List<string>();
            var outgoingIFMSfile = outgoingIFMSdata.First();
            
            string filePath = await financialManager.CreateIFMSfile(outgoingIFMSfile.Name, errors);

            if (errors.Count == 0)
                ColourConsole.WriteEmbeddedColorLine($"Successfully created [cyan]{filePath}[/cyan]");
            else
                foreach (var error in errors)
                {
                    ColourConsole.WriteEmbeddedColorLine($"Error creating [cyan]{outgoingIFMSfile.Name}[/cyan]: [red]{error}[/red]");
                    await db.ErrorTrackingTable.MessageBrokerErrorAsync("SINOUT", outgoingIFMSfile.Name,
                                                                                new Exception(error), displayExceptionError: true);
                }
        }
    }
}