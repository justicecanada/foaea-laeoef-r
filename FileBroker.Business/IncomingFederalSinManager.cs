namespace FileBroker.Business;

public class IncomingFederalSinManager
{
    private APIBrokerList APIs { get; }
    private RepositoryList Repositories { get; }

    public IncomingFederalSinManager(APIBrokerList apiBrokers, RepositoryList repositories)
    {
        APIs = apiBrokers;
        Repositories = repositories;
    }

    public List<string> ProcessFlatFile(string flatFileContent, string flatFileName)
    {
        var fileTableData = GetFileTableData(flatFileName);

        var sinFileData = new FedSinFileBase();

        var errors = new List<string>();

        if (!fileTableData.Active.HasValue || !fileTableData.Active.Value)
            errors.Add($"[{fileTableData.Name}] is not active.");

        if (errors.Any())
            return errors;

        string fileCycle = Path.GetExtension(flatFileName)[1..];

        int expectedCyle = fileTableData.Cycle;
        int actualCyle = int.Parse(fileCycle);
        if (actualCyle != expectedCyle)
        {
            errors.Add($"Error: expecting cycle {expectedCyle} but received {actualCyle}");
            return errors;
        }

        Repositories.FileTable.SetIsFileLoadingValue(fileTableData.PrcId, true);

        try
        {
            var fileLoader = new IncomingFederalSinFileLoader(Repositories.FlatFileSpecs, fileTableData.PrcId);
            fileLoader.FillSinFileDataFromFlatFile(sinFileData, flatFileContent, ref errors);

            if (errors.Any())
                return errors;

            ValidateHeader(sinFileData.SININ01, flatFileName, ref errors);
            ValidateFooter(sinFileData.SININ99, sinFileData.SININ02, ref errors);

            if (errors.Any())
                return errors;

            var sinResults = ExtractSinResultsFromFileData(sinFileData);

            if ((sinResults != null) && (sinFileData.SININ02.Count > 0))
            {
                var processCodes = Repositories.ProcessParameterTable.GetProcessCodes(fileTableData.PrcId);

                SendSinResultsToFOAEA(sinResults, flatFileName, (short)processCodes.AppLiSt_Cd);
            }

        }
        catch (Exception e)
        {
            errors.Add("An error occurred: " + e.Message);
        }
        finally
        {
            if (errors.Count == 0)
                Repositories.FileTable.SetNextCycleForFileType(fileTableData, fileCycle.Length);
            Repositories.FileTable.SetIsFileLoadingValue(fileTableData.PrcId, false);
        }

        return errors;
    }

    private void SendSinResultsToFOAEA(List<SINResultData> sinResults, string flatFileName, short appLiSt_Cd)
    {
        var requestedEvents = APIs.ApplicationEvents.GetRequestedSINEventDataForFile(flatFileName);
        var requestedEventDetails = APIs.ApplicationEvents.GetRequestedSINEventDetailDataForFile(flatFileName);

        foreach (var sinResult in sinResults)
            UpdateSinEventTables(sinResult, requestedEvents, requestedEventDetails, flatFileName, appLiSt_Cd);

        UpdateSinResultTable(sinResults);

        var latestUpdatedSinData = APIs.ApplicationEvents.GetLatestSinEventDataSummary();
        foreach (var updatedSinDataSummary in latestUpdatedSinData)
            UpdateApplicationBasedOnReceivedSinResult(updatedSinDataSummary, requestedEvents, requestedEventDetails);

    }

    private void UpdateStateForSinEvent(List<ApplicationEventData> requestedEvents, int eventId, string newState)
    {
        var eventsForThisAppl = requestedEvents.Where(m => m.Event_Id == eventId).ToList();

        if (eventsForThisAppl.Any())
        {
            var thisEvent = eventsForThisAppl.First();
            thisEvent.ActvSt_Cd = newState;
            APIs.ApplicationEvents.SaveEvent(thisEvent);
        }
    }

    private void UpdateApplicationBasedOnReceivedSinResult(SinInboundToApplData updatedSinDataSummary,
                                        List<ApplicationEventData> requestedEvents,
                                        List<ApplicationEventDetailData> requestedEventDetails)
    {
        string appl_EnfSrv_Cd = updatedSinDataSummary.Appl_EnfSrv_Cd;
        string appl_CtrlCd = updatedSinDataSummary.Appl_CtrlCd;

        bool sourceModifiedSin = false;

        var appl = APIs.Applications.GetApplication(appl_EnfSrv_Cd, appl_CtrlCd);

        if (appl.ActvSt_Cd != "A")
        {
            UpdateStateForSinEvent(requestedEvents, updatedSinDataSummary.Event_Id, "P"); // shouldn't this close the event???
            return;
        }

        if (appl.AppLiSt_Cd != ApplicationState.SIN_CONFIRMATION_PENDING_3)
        {
            if (string.IsNullOrEmpty(updatedSinDataSummary.Appl_Dbtr_Cnfrmd_SIN) &&
                !string.IsNullOrEmpty(updatedSinDataSummary.Appl_Dbtr_RtrndBySrc_SIN))
                sourceModifiedSin = true; // why only check when not at state 3 and why allowed to continue???
            else
            {
                UpdateStateForSinEvent(requestedEvents, updatedSinDataSummary.Event_Id, "I");
                AddSysEvent(appl_EnfSrv_Cd, appl_CtrlCd, EventCode.C50482_APPLICATION_STATE_WAS_NOT_AT_3_EVENT_WAS_MARKED_AS_INNACTIVE);
                return;
            }
        }

        if (!updatedSinDataSummary.ValStat_Cd.HasValue)
        {
            // this hasn't occurred since 1997 -- probably not needed anymore???
            UpdateStateForSinEvent(requestedEvents, updatedSinDataSummary.Event_Id, "I");
            AddSysEvent(appl_EnfSrv_Cd, appl_CtrlCd, EventCode.C50483_NO_MATCHING_ACTIVE_DETAIL_EVENTS_WERE_FOUND_FOR_INBOUND_RECORD);
            return;
        }

        if (updatedSinDataSummary.Tot_Invalid > 0)
        {
            UpdateStateForSinEvent(requestedEvents, updatedSinDataSummary.Event_Id, "I");
            return;
        }

        if (updatedSinDataSummary.Tot_Childs != updatedSinDataSummary.Tot_Closed)
        {
            UpdateStateForSinEvent(requestedEvents, updatedSinDataSummary.Event_Id, "A");
            return;
        }

        appl.Appl_LastUpdate_Dte = DateTime.Now;
        appl.Appl_LastUpdate_Usr = "MSGBRO";

        if (IsSinConfirmed(updatedSinDataSummary))
        {
            var confirmationSinData = new SINConfirmationData
            {
                IsSinConfirmed = true,
                ConfirmedSIN = updatedSinDataSummary.SVR_SIN
            };

            APIs.Applications.SinConfirmation(appl_EnfSrv_Cd, appl_CtrlCd, confirmationSinData);
        }
        else // SIN is NOT confirmed
        {
            var confirmationSinData = new SINConfirmationData
            {
                IsSinConfirmed = false,
                ConfirmedSIN = String.Empty
            };

            APIs.Applications.SinConfirmation(appl_EnfSrv_Cd, appl_CtrlCd, confirmationSinData);

            if (sourceModifiedSin)
                AddAMEvent(appl_EnfSrv_Cd, appl_CtrlCd, EventCode.C50532_SOURCE_MODIFIED_SIN_NOT_CONFIRMED,
                           $"SIN {updatedSinDataSummary.SVR_SIN} was not confirmed");
        }

        UpdateStateForSinEvent(requestedEvents, updatedSinDataSummary.Event_Id, "C");
    }

    private void AddSysEvent(string appl_EnfSrv_Cd, string appl_CtrlCd, EventCode eventCode)
    {
        var newSysEvent = new ApplicationEventData
        {
            Appl_EnfSrv_Cd = appl_EnfSrv_Cd,
            Appl_CtrlCd = appl_CtrlCd,
            Queue = EventQueue.EventSYS,
            Event_Reas_Cd = eventCode,
            Subm_SubmCd = "MSGBRO",
            Subm_Recpt_SubmCd = "MSGBRO",
            Event_TimeStamp = DateTime.Now,
            Event_RecptSubm_ActvStCd = "A",
            Event_Effctv_Dte = DateTime.Now,
            ActvSt_Cd = "A",
            AppLiSt_Cd = ApplicationState.INITIAL_STATE_0
        };
        APIs.ApplicationEvents.SaveEvent(newSysEvent);
    }

    private void AddAMEvent(string appl_EnfSrv_Cd, string appl_CtrlCd, EventCode eventCode, string reasonText)
    {
        var newSysEvent = new ApplicationEventData
        {
            Appl_EnfSrv_Cd = appl_EnfSrv_Cd,
            Appl_CtrlCd = appl_CtrlCd,
            Queue = EventQueue.EventAM,
            Event_Reas_Cd = eventCode,
            Event_Reas_Text = reasonText,
            Subm_SubmCd = "MSGBRO",
            Subm_Recpt_SubmCd = "MSGBRO",
            Event_TimeStamp = DateTime.Now,
            Event_RecptSubm_ActvStCd = "A",
            Event_Effctv_Dte = DateTime.Now,
            ActvSt_Cd = "A",
            AppLiSt_Cd = ApplicationState.INITIAL_STATE_0
        };
        APIs.ApplicationEvents.SaveEvent(newSysEvent);
    }

    private static bool IsSinConfirmed(SinInboundToApplData updatedSinDataSummary)
    {
        return (updatedSinDataSummary.ValStat_Cd == 0) && (!string.IsNullOrEmpty(updatedSinDataSummary.SVR_SIN));
    }

    private void UpdateSinResultTable(List<SINResultData> sinResults)
    {
        APIs.Sins.InsertBulkData(sinResults);
    }

    private void UpdateSinEventTables(SINResultData sinResult,
                                      List<ApplicationEventData> requestedEvents,
                                      List<ApplicationEventDetailData> requestedEventDetails,
                                      string flatFileName, short appLiSt_Cd)
    {
        var eventForThisAppl = requestedEvents.Where(m => m.Appl_EnfSrv_Cd == sinResult.Appl_EnfSrv_Cd &&
                                                           m.Appl_CtrlCd == sinResult.Appl_CtrlCd).FirstOrDefault();

        if (eventForThisAppl is not null)
        {
            int eventId = eventForThisAppl.Event_Id;
            eventForThisAppl.ActvSt_Cd = "P";
            eventForThisAppl.Event_Compl_Dte = DateTime.Now;
            APIs.ApplicationEvents.SaveEvent(eventForThisAppl);

            var eventDetailForThisAppl = requestedEventDetails.Where(m => m.Event_Id == eventId).FirstOrDefault();

            if (eventDetailForThisAppl is not null)
            {
                string eventReason = $"[FileNm:{flatFileName}][ErrDes:000000MSGBRO]" +
                                     $"[(EnfSrv:{sinResult.Appl_EnfSrv_Cd.Trim()})(CtrlCd:{sinResult.Appl_CtrlCd.Trim()})]";

                eventDetailForThisAppl.Event_Reas_Cd = null;
                eventDetailForThisAppl.Event_Reas_Text = eventReason;
                eventDetailForThisAppl.Event_Effctv_Dte = DateTime.Now;
                eventDetailForThisAppl.AppLiSt_Cd = appLiSt_Cd;
                eventDetailForThisAppl.ActvSt_Cd = "C";
                eventDetailForThisAppl.Event_Compl_Dte = DateTime.Now;

                APIs.ApplicationEvents.SaveEventDetail(eventDetailForThisAppl);
            }
        }
    }

    private static List<SINResultData> ExtractSinResultsFromFileData(FedSinFileBase sinFileData)
    {
        var sinResults = new List<SINResultData>();

        foreach (var sinResult in sinFileData.SININ02)
        {
            sinResults.Add(
                new SINResultData
                {
                    Appl_EnfSrv_Cd = sinResult.dat_Appl_EnfSrvCd,
                    Appl_CtrlCd = sinResult.dat_Appl_CtrlCd,
                    SVR_TimeStamp = sinFileData.SININ01.FileDate,
                    SVR_TolCd = sinResult.dat_SVR_TolCd,
                    SVR_SIN = sinResult.dat_SVR_SIN,
                    SVR_DOB_TolCd = (short?)sinResult.dat_SVR_DOB_TolCd,
                    SVR_GvnNme_TolCd = (short?)sinResult.dat_SVR_GvnNme_TolCd,
                    SVR_MddlNme_TolCd = (short?)sinResult.dat_SVR_MddlNme_TolCd,
                    SVR_SurNme_TolCd = (short?)sinResult.dat_SVR_SurNme_TolCd,
                    SVR_MotherNme_TolCd = (short?)sinResult.dat_SVR_MotherNme_TolCd,
                    SVR_Gendr_TolCd = (short?)sinResult.dat_SVR_Gendr_TolCd,
                    ValStat_Cd = (short)sinResult.dat_ValStat_Cd,
                    ActvSt_Cd = "C"
                }
            );
        }

        return sinResults;
    }

    private FileTableData GetFileTableData(string flatFileName)
    {
        string fileNameNoCycle = Path.GetFileNameWithoutExtension(flatFileName);

        return Repositories.FileTable.GetFileTableDataForFileName(fileNameNoCycle);
    }

    private static void ValidateHeader(FedSin_RecType01 dataFromFile, string flatFileName, ref List<string> errors)
    {
        int cycle = FileHelper.GetCycleFromFilename(flatFileName);
        if (dataFromFile.Cycle != cycle)
            errors.Add($"Cycle in file [{dataFromFile.Cycle}] does not match cycle of file [{cycle}]");
    }

    private static void ValidateFooter(FedSin_RecType99 dataFromFile, List<FedSin_RecType02> dataSummary, ref List<string> errors)
    {
        if (dataFromFile.ResponseCnt != dataSummary.Count)
            errors.Add("Invalid ResponseCnt in section 99");
    }

}
