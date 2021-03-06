using Newtonsoft.Json;

namespace FileBroker.Business;

public class IncomingProvincialTracingManager
{
    private string FileName { get; }
    private APIBrokerList APIs { get; }
    private RepositoryList Repositories { get; }
    private ProvincialAuditFileConfig AuditConfiguration { get; }

    public IncomingProvincialTracingManager(string fileName,
                                            APIBrokerList apis,
                                            RepositoryList repositories,
                                            ProvincialAuditFileConfig auditConfig)
    {
        FileName = fileName;
        APIs = apis;
        Repositories = repositories;
        AuditConfiguration = auditConfig;
    }

    public MessageDataList ExtractAndProcessRequestsInFile(string sourceTracingData, List<UnknownTag> unknownTags, bool includeInfoInMessages = false)
    {
        var result = new MessageDataList();

        var fileAuditManager = new FileAuditManager(Repositories.FileAudit, AuditConfiguration, Repositories.MailServiceDB);

        var fileNameNoCycle = Path.GetFileNameWithoutExtension(FileName);
        var fileTableData = Repositories.FileTable.GetFileTableDataForFileName(fileNameNoCycle);

        Repositories.FileTable.SetIsFileLoadingValue(fileTableData.PrcId, true);

        bool isValid = true;

        // convert data from json into object
        var tracingFileData = ExtractTracingDataFromJson(sourceTracingData, out string error);
        var tracingFile = tracingFileData.NewDataSet;

        if (!string.IsNullOrEmpty(error))
        {
            isValid = false;
            result.AddSystemError(error);
        }
        else
        {
            ValidateHeader(tracingFile, ref result, ref isValid);
            ValidateFooter(tracingFile, ref result, ref isValid);

            if (isValid)
            {
                int errorCount = 0;
                int warningCount = 0;
                int successCount = 0;

                foreach (var data in tracingFile.TRCAPPIN20)
                {
                    bool isValidRequest = true;

                    var fileAuditData = new FileAuditData
                    {
                        Appl_EnfSrv_Cd = data.dat_Appl_EnfSrvCd,
                        Appl_CtrlCd = data.dat_Appl_CtrlCd,
                        Appl_Source_RfrNr = data.dat_Appl_Source_RfrNr,
                        InboundFilename = FileName + ".XML"
                    };

                    var requestError = new MessageDataList();

                    ValidateActionCode(data, ref requestError, ref isValidRequest);

                    if (isValidRequest)
                    {
                        var traceData = tracingFile.TRCAPPIN21.Find(t => t.dat_Appl_CtrlCd == data.dat_Appl_CtrlCd);

                        var tracingMessage = new MessageData<TracingApplicationData>
                        {
                            Application = GetTracingApplicationDataFromRequest(data, traceData),
                            MaintenanceAction = data.Maintenance_ActionCd,
                            MaintenanceLifeState = data.dat_Appl_LiSt_Cd,
                            NewRecipientSubmitter = data.dat_New_Owner_RcptSubmCd,
                            NewIssuingSubmitter = data.dat_New_Owner_SubmCd,
                            NewUpdateSubmitter = data.dat_Update_SubmCd
                        };

                        var messages = ProcessApplicationRequest(tracingMessage);

                        if (messages.ContainsMessagesOfType(MessageType.Error))
                        {
                            var errors = messages.FindAll(m => m.Severity == MessageType.Error);

                            fileAuditData.ApplicationMessage = errors[0].Description;
                            errorCount++;
                        }
                        else if (messages.ContainsMessagesOfType(MessageType.Warning))
                        {
                            var warnings = messages.FindAll(m => m.Severity == MessageType.Warning);

                            fileAuditData.ApplicationMessage = warnings[0].Description;
                            warningCount++;
                        }
                        else
                        {
                            if (includeInfoInMessages)
                            {
                                var infos = messages.FindAll(m => m.Severity == MessageType.Information);

                                result.AddRange(infos);
                            }

                            fileAuditData.ApplicationMessage = "Success";
                            successCount++;
                        }

                    }
                    else
                    {
                        fileAuditData.ApplicationMessage = requestError[0].Description;
                        errorCount++;
                    }

                    Repositories.FileAudit.InsertFileAuditData(fileAuditData);

                }

                fileAuditManager.GenerateAuditFile(FileName, unknownTags, errorCount, warningCount, successCount);
                fileAuditManager.SendStandardAuditEmail(FileName, AuditConfiguration.AuditRecipients, 
                                                        errorCount, warningCount, successCount, unknownTags.Count);
            }

        }

        if (!isValid)
        {
            result.AddSystemError($"One of more error(s) occured in file ({FileName}.XML)");

            fileAuditManager.SendSystemErrorAuditEmail(FileName, AuditConfiguration.AuditRecipients, result);
        }

        Repositories.FileAudit.MarkFileAuditCompletedForFile(FileName);
        Repositories.FileTable.SetIsFileLoadingValue(fileTableData.PrcId, false);
        Repositories.FileTable.SetNextCycleForFileType(fileTableData);

        return result;
    }

    public MessageDataList ProcessApplicationRequest(MessageData<TracingApplicationData> tracingMessageData)
    {
        TracingApplicationData tracing;

        if (tracingMessageData.MaintenanceAction == "A")
        {
            tracing = APIs.TracingApplications.CreateTracingApplication(tracingMessageData.Application);
        }
        else // if (tracingMessageData.MaintenanceAction == "C")
        {
            switch (tracingMessageData.MaintenanceLifeState)
            {
                case "00": // change
                case "0": 
                    tracing = APIs.TracingApplications.UpdateTracingApplication(tracingMessageData.Application);
                    break;

                case "14": // cancellation
                    tracing = APIs.TracingApplications.CloseTracingApplication(tracingMessageData.Application);
                    break;

                case "29": // transfer
                    tracing = APIs.TracingApplications.TransferTracingApplication(tracingMessageData.Application,
                                                                                  tracingMessageData.NewRecipientSubmitter,
                                                                                  tracingMessageData.NewIssuingSubmitter);
                    break;

                default:
                    tracing = tracingMessageData.Application;
                    tracing.Messages.AddError($"Unknown dat_Appl_LiSt_Cd ({tracingMessageData.MaintenanceLifeState})" +
                                              $" for Maintenance_ActionCd ({tracingMessageData.MaintenanceAction})");
                    break;
            }
        }

        return tracing.Messages;
    }

    private void ValidateHeader(MEPTracing_TracingDataSet tracingFile, ref MessageDataList result, ref bool isValid)
    {
        int cycle = FileHelper.GetCycleFromFilename(FileName);
        if (int.Parse(tracingFile.TRCAPPIN01.Cycle) != cycle)
        {
            isValid = false;
            result.AddSystemError($"Cycle in file [{tracingFile.TRCAPPIN01.Cycle}] does not match cycle of file [{cycle}]");
        }

    }

    private static void ValidateFooter(MEPTracing_TracingDataSet tracingFile, ref MessageDataList result, ref bool isValid)
    {
        if (int.Parse(tracingFile.TRCAPPIN99.ResponseCnt) != tracingFile.TRCAPPIN20.Count)
        {
            isValid = false;
            result.AddSystemError("Invalid ResponseCnt in section 99");
        }
    }

    private static void ValidateActionCode(MEPTracing_RecType20 data, ref MessageDataList result, ref bool isValid)
    {
        bool validActionLifeState = true;
        string actionCode = data.Maintenance_ActionCd.Trim();
        string actionState = data.dat_Appl_LiSt_Cd.Trim();

        if ((actionCode == "A") && actionState.NotIn("00", "0"))
            validActionLifeState = false;
        else if ((actionCode == "C") && (actionState.NotIn("00", "0", "14", "29")))
            validActionLifeState = false;
        else if (actionCode.NotIn("A", "C"))
            validActionLifeState = false;

        if (!validActionLifeState)
        {
            isValid = false;
            result.AddSystemError($"Invalid MaintenanceAction [{actionCode}] and MaintenanceLifeState [{actionState}] combination.");
        }

    }

    private static MEPTracingFileData ExtractTracingDataFromJson(string sourceTracingData, out string error)
    {
        error = string.Empty;

        MEPTracingFileData result;
        try
        {
            result = JsonConvert.DeserializeObject<MEPTracingFileData>(sourceTracingData);
        }
        catch
        {
            try
            {
                var single = JsonConvert.DeserializeObject<MEPTracingFileDataSingle>(sourceTracingData);
                if (single is null) 
                    throw new NullReferenceException("json conversion failed for MEPTracingFileDataSingle");

                result = new MEPTracingFileData();
                result.NewDataSet.TRCAPPIN01 = single.NewDataSet.TRCAPPIN01;
                result.NewDataSet.TRCAPPIN20.Add(single.NewDataSet.TRCAPPIN20);
                result.NewDataSet.TRCAPPIN21.Add(single.NewDataSet.TRCAPPIN21);
                result.NewDataSet.TRCAPPIN99 = single.NewDataSet.TRCAPPIN99;
            }
            catch (Exception ee)
            {
                error = ee.Message;
                result = new MEPTracingFileData();
            }
        }

        return result;
    }

    private static TracingApplicationData GetTracingApplicationDataFromRequest(MEPTracing_RecType20 baseData, MEPTracing_RecType21 tracingData)
    {
        var tracingApplication = new TracingApplicationData
        {
            Appl_EnfSrv_Cd = baseData.dat_Appl_EnfSrvCd,
            Appl_CtrlCd = baseData.dat_Appl_CtrlCd,
            Appl_Source_RfrNr = baseData.dat_Appl_Source_RfrNr,
            Subm_Recpt_SubmCd = baseData.dat_Subm_Rcpt_SubmCd,
            Subm_SubmCd = baseData.dat_Subm_SubmCd,
            Appl_Lgl_Dte = baseData.dat_Appl_Lgl_Dte.ConvertToDateTimeIgnoringTimeZone()?.Date ?? default,
            Appl_Dbtr_SurNme = baseData.dat_Appl_Dbtr_SurNme,
            Appl_Dbtr_FrstNme = baseData.dat_Appl_Dbtr_FrstNme,
            Appl_Dbtr_MddleNme = baseData.dat_Appl_Dbtr_MddleNme,
            Appl_Dbtr_Brth_Dte = baseData.dat_Appl_Dbtr_Brth_Dte.ConvertToDateTimeIgnoringTimeZone()?.Date,
            Appl_Dbtr_Gendr_Cd = baseData.dat_Appl_Dbtr_Gendr_Cd.Trim() == "" ? "M" : baseData.dat_Appl_Dbtr_Gendr_Cd.Trim(),
            Appl_Dbtr_Entrd_SIN = baseData.dat_Appl_Dbtr_Entrd_SIN,
            Appl_Dbtr_Parent_SurNme = baseData.dat_Appl_Dbtr_Parent_SurNme_Birth,
            Appl_CommSubm_Text = baseData.dat_Appl_CommSubm_Text,
            Appl_Rcptfrm_Dte = baseData.dat_Appl_Rcptfrm_dte.ConvertToDateTimeIgnoringTimeZone()?.Date ?? default,
            AppCtgy_Cd = baseData.dat_Appl_AppCtgy_Cd,
            Appl_Group_Batch_Cd = baseData.dat_Appl_Group_Batch_Cd,
            Medium_Cd = baseData.dat_Appl_Medium_Cd,
            Appl_Affdvt_DocTypCd = baseData.dat_Appl_Affdvt_Doc_TypCd,
            AppReas_Cd = baseData.dat_Appl_Reas_Cd,
            Appl_Reactv_Dte = baseData.dat_Appl_Reactv_Dte.ConvertToDateTimeIgnoringTimeZone(),
            AppLiSt_Cd = (ApplicationState)int.Parse(baseData.dat_Appl_LiSt_Cd),
            Appl_SIN_Cnfrmd_Ind = 0,
            ActvSt_Cd = "A",
            Appl_Crdtr_SurNme = tracingData.dat_Appl_Crdtr_SurNme,
            Appl_Crdtr_FrstNme = tracingData.dat_Appl_Crdtr_FrstNme,
            Appl_Crdtr_MddleNme = tracingData.dat_Appl_Crdtr_MddleNme,
            Trace_Child_Text = tracingData.dat_Trace_Child_Text,
            Trace_Breach_Text = tracingData.dat_Trace_Breach_Text,
            Trace_ReasGround_Text = tracingData.dat_Trace_ReasGround_Text,
            FamPro_Cd = tracingData.dat_FamPro_Cd,
            Statute_Cd = tracingData.dat_Statute_Cd,
            Trace_LstCyclCmp_Dte = DateTime.Now,
            Trace_LiSt_Cd = 0,
            InfoBank_Cd = tracingData.dat_InfoBank_Cd,
        };

        return tracingApplication;
    }
}
