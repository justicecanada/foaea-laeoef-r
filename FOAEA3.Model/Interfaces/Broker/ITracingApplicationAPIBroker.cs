﻿using FOAEA3.Model.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FOAEA3.Model.Interfaces.Broker
{
    public interface ITracingApplicationAPIBroker
    {
        IAPIBrokerHelper ApiHelper { get; }
        string Token { get; set; }

        Task<TracingApplicationData> CancelTracingApplication(TracingApplicationData tracingApplication);
        Task<TracingApplicationData> CreateTracingApplication(TracingApplicationData tracingData);
        Task<TracingApplicationData> FullyServiceApplication(TracingApplicationData tracingApplication, FederalSource fedSource);
        Task<TracingApplicationData> PartiallyServiceApplication(TracingApplicationData tracingApplication, FederalSource fedSource);
        Task<TracingApplicationData> GetApplication(string dat_Appl_EnfSrvCd, string dat_Appl_CtrlCd);
        Task<List<TraceCycleQuantityData>> GetTraceCycleQuantityData(string enfSrvCd, string fileCycle);
        Task<List<TraceToApplData>> GetTraceToApplData();
        Task<TracingApplicationData> UpdateTracingApplication(TracingApplicationData tracingApplication);
        Task<TracingApplicationData> CertifyTracingApplication(TracingApplicationData tracingApplication);
        Task<TracingApplicationData> InsertAffidavit(AffidavitData data);
        Task<TracingApplicationData> TransferTracingApplication(TracingApplicationData tracingApplication,
                                                                 string newRecipientSubmitter,
                                                                 string newIssuingSubmitter);
        Task<List<TracingOutgoingFederalData>> GetOutgoingFederalTracingRequests(int maxRecords, string activeState,
                                                                           int lifeState, string enfServiceCode);
        Task<TracingOutgoingProvincialData> GetOutgoingProvincialTracingData(int maxRecords, string activeState,
                                                                             string recipientCode);
    }
}
