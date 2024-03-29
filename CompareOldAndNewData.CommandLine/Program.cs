﻿using CompareOldAndNewData.CommandLine;
using DBHelper;
using FileBroker.Data.DB;
using FOAEA3.Data.Base;
using FOAEA3.Resources.Helpers;
using Microsoft.Extensions.Configuration;
using System.Text;

string aspnetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

IConfiguration configuration = new ConfigurationBuilder()
        .AddJsonFile($"appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{aspnetCoreEnvironment}.json", optional: true)
        .Build();

var foaea2DB = new DBToolsAsync(configuration.GetConnectionString("Foaea2DB").ReplaceVariablesWithEnvironmentValues());
var foaea3DB = new DBToolsAsync(configuration.GetConnectionString("Foaea3DB").ReplaceVariablesWithEnvironmentValues());
var fileBrokerDB = new DBToolsAsync(configuration.GetConnectionString("FileBroker").ReplaceVariablesWithEnvironmentValues());

var repositories2 = new DbRepositories(foaea2DB);
var repositories3 = new DbRepositories(foaea3DB);
var repositories2Finance = new DbRepositories_Finance(foaea2DB);
var repositories3Finance = new DbRepositories_Finance(foaea3DB);

var requestLogDB = new DBRequestLog(fileBrokerDB);
var requests = await requestLogDB.GetAll();
int n = 1;

var output = new StringBuilder();

DateTime start = DateTime.Now;
ColourConsole.WriteEmbeddedColorLine($"Starting time [yellow]{start}[/yellow]");

foreach (var request in requests)
{
    var action = request.MaintenanceAction + request.MaintenanceLifeState;
    var enfSrv = request.Appl_EnfSrv_Cd.Trim();
    var ctrlCd = request.Appl_CtrlCd.Trim();

    var foaea2RunDate = new DateTime(2022, 11, 7).Date;
    var foaea3RunDate = new DateTime(2022, 11, 10).Date;

    ColourConsole.WriteEmbeddedColor($"Comparing [cyan]{enfSrv}-{ctrlCd}[/cyan]... ([green]{n}[/green] of [green]{requests.Count}[/green])\r");
    await CompareAll.RunAsync(repositories2, repositories2Finance, repositories3, repositories3Finance,
                              action, enfSrv, ctrlCd, foaea2RunDate, foaea3RunDate, output);
    n++;
}

File.WriteAllText(@"C:\work\Compare1.txt", output.ToString());

Console.WriteLine("\nFinished");

DateTime end = DateTime.Now;
var difference = end - start;
ColourConsole.WriteEmbeddedColorLine($"Completed time [yellow]{end}[/yellow] (Duration [yellow]{difference.Minutes}[/yellow] minutes)");

