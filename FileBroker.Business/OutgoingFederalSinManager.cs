﻿using FileBroker.Common.Helpers;
using System.Text;

namespace FileBroker.Business;

public class OutgoingFederalSinManager : IOutgoingFileManager
{
    private APIBrokerList APIs { get; }
    private RepositoryList DB { get; }

    private FoaeaSystemAccess FoaeaAccess { get; }

    public OutgoingFederalSinManager(APIBrokerList apis, RepositoryList repositories, IFileBrokerConfigurationHelper config)
    {
        APIs = apis;
        DB = repositories;

        FoaeaAccess = new FoaeaSystemAccess(apis, config.FoaeaLogin);
    }

    public async Task<(string, List<string>)> CreateOutputFile(string fileBaseName)
    {
        var errors = new List<string>();

        bool fileCreated = false;

        var fileTableData = await DB.FileTable.GetFileTableDataForFileName(fileBaseName);

        int cycleLength = 3;
        int thisNewCycle = fileTableData.Cycle + 1;
        if (thisNewCycle == 1000)
            thisNewCycle = 1;
        string newCycle = thisNewCycle.ToString(new string('0', cycleLength));

        try
        {
            var processCodes = await DB.ProcessParameterTable.GetProcessCodes(fileTableData.PrcId);

            string newFilePath = fileTableData.Path + fileBaseName + "." + newCycle;
            if (File.Exists(newFilePath))
            {
                errors.Add("** Error: File Already Exists");
                return ("", errors);
            }

            await FoaeaAccess.SystemLogin();

            try
            {
                var data = await GetOutgoingData(fileTableData, processCodes.ActvSt_Cd, processCodes.AppLiSt_Cd,
                                                 processCodes.EnfSrv_Cd);

                var eventIds = new List<int>();
                string fileContent = GenerateOutputFileContentFromData(data, newCycle, ref eventIds);

                await File.WriteAllTextAsync(newFilePath, fileContent);
                fileCreated = true;

                await DB.OutboundAuditTable.InsertIntoOutboundAudit(fileBaseName + "." + newCycle, DateTime.Now, fileCreated,
                                                                     "Outbound File created successfully.");

                await DB.FileTable.SetNextCycleForFileType(fileTableData, newCycle.Length);

                await APIs.ApplicationEvents.UpdateOutboundEventDetail(processCodes.ActvSt_Cd, processCodes.AppLiSt_Cd,
                                                                       processCodes.EnfSrv_Cd,
                                                                       "OK: Written to " + newFilePath, eventIds);
            }
            finally
            {
                await FoaeaAccess.SystemLogout();
            }

            return (newFilePath, errors);

        }
        catch (Exception e)
        {
            string error = "Error Creating Outbound Data File: " + e.Message;
            errors.Add(error);

            await DB.OutboundAuditTable.InsertIntoOutboundAudit(fileBaseName + "." + newCycle, DateTime.Now, fileCreated, error);

            await DB.ErrorTrackingTable.MessageBrokerError($"File Error: {fileTableData.PrcId} {fileBaseName}",
                                                                       "Error creating outbound file", e, displayExceptionError: true);

            return (string.Empty, errors);
        }
    }

    private async Task<List<SINOutgoingFederalData>> GetOutgoingData(FileTableData fileTableData, string actvSt_Cd,
                                                         int appLiSt_Cd, string enfSrvCode)
    {
        var recMax = await DB.ProcessParameterTable.GetValueForParameter(fileTableData.PrcId, "rec_max");
        int maxRecords = string.IsNullOrEmpty(recMax) ? 0 : int.Parse(recMax);

        var data = await APIs.Sins.GetOutgoingFederalSins(maxRecords, actvSt_Cd, appLiSt_Cd, enfSrvCode);
        return data;
    }

    private static string GenerateOutputFileContentFromData(List<SINOutgoingFederalData> data,
                                                            string newCycle, ref List<int> eventIds)
    {
        var result = new StringBuilder();

        result.AppendLine(GenerateHeaderLine(newCycle));
        foreach (var item in data)
        {
            result.AppendLine(GenerateDetailLine(item));
            eventIds.Add(item.Event_dtl_Id);
        }
        result.AppendLine(GenerateFooterLine(data.Count));

        return result.ToString();
    }

    private static string GenerateHeaderLine(string newCycle)
    {
        string julianDate = DateTime.Now.AsJulianString();

        return $"01{newCycle}{julianDate}";
    }

    private static string GenerateDetailLine(SINOutgoingFederalData item)
    {
        string result = $"02{item.Appl_EnfSrv_Cd,6}{item.Appl_CtrlCd,6}{item.Appl_Dbtr_Entrd_SIN,9}" +
                        $"{item.Appl_Dbtr_FrstNme,15}{item.Appl_Dbtr_MddleNme,15}{item.Appl_Dbtr_SurNme,25}" +
                        $"{item.Appl_Dbtr_Parent_SurNme,25}{item.Appl_Dbtr_Gendr_Cd,1}{item.Appl_Dbtr_Brth_Dte,8}";

        return result;
    }

    private static string GenerateFooterLine(int rowCount)
    {
        return $"99{rowCount:000000}";
    }
}
