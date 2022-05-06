﻿using FOAEA3.Model;
using FOAEA3.Model.Interfaces;
using FOAEA3.Model.Interfaces.Broker;
using FOAEA3.Resources.Helpers;

namespace FOAEA3.Common.Brokers
{
    public class InterceptionApplicationAPIBroker : IInterceptionApplicationAPIBroker
    {
        private IAPIBrokerHelper ApiHelper { get; }

        public InterceptionApplicationAPIBroker(IAPIBrokerHelper apiHelper)
        {
            ApiHelper = apiHelper;
        }

        public InterceptionApplicationData GetApplication(string dat_Appl_EnfSrvCd, string dat_Appl_CtrlCd)
        {
            string key = ApplKey.MakeKey(dat_Appl_EnfSrvCd, dat_Appl_CtrlCd);
            string apiCall = $"api/v1/interceptions/{key}";
            return ApiHelper.GetDataAsync<InterceptionApplicationData>(apiCall).Result;
        }

        public InterceptionApplicationData CreateInterceptionApplication(InterceptionApplicationData interceptionApplication)
        {
            var data = ApiHelper.PostDataAsync<InterceptionApplicationData, InterceptionApplicationData>("api/v1/Interceptions",
                                                                                               interceptionApplication).Result;
            return data;
        }

        public InterceptionApplicationData TransferInterceptionApplication(InterceptionApplicationData interceptionApplication, string newRecipientSubmitter, string newIssuingSubmitter)
        {
            string key = ApplKey.MakeKey(interceptionApplication.Appl_EnfSrv_Cd, interceptionApplication.Appl_CtrlCd);
            string apiCall = $"api/v1/interceptions/{key}/transfer?newRecipientSubmitter={newRecipientSubmitter}" +
                                                                 $"&newIssuingSubmitter={newIssuingSubmitter}";
            var data = ApiHelper.PutDataAsync<InterceptionApplicationData, InterceptionApplicationData>(apiCall,
                                                                                              interceptionApplication).Result;
            return data;
        }

        public InterceptionApplicationData UpdateInterceptionApplication(InterceptionApplicationData interceptionApplication)
        {
            string key = ApplKey.MakeKey(interceptionApplication.Appl_EnfSrv_Cd, interceptionApplication.Appl_CtrlCd);
            var data = ApiHelper.PutDataAsync<InterceptionApplicationData, InterceptionApplicationData>($"api/v1/interceptions/{key}",
                                                                                              interceptionApplication).Result;
            return data;
        }

        public InterceptionApplicationData VaryInterceptionApplication(InterceptionApplicationData interceptionApplication)
        {
            string key = ApplKey.MakeKey(interceptionApplication.Appl_EnfSrv_Cd, interceptionApplication.Appl_CtrlCd);
            var data = ApiHelper.PutDataAsync<InterceptionApplicationData, InterceptionApplicationData>($"api/v1/interceptions/{key}/Vary",
                                                                                              interceptionApplication).Result;
            return data;
        }

    }
}
