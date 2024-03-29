﻿using FileBroker.Model.Interfaces.Broker;
using FOAEA3.Model.Interfaces;
using FOAEA3.Model.Interfaces.Broker;

namespace FileBroker.Common.Brokers
{
    public class FEDLicenceDenialAPIBroker : IFEDLicenceDenialAPIBroker, IVersionSupport
    {
        public IAPIBrokerHelper ApiHelper { get; }
        public string Token { get; set; }

        public FEDLicenceDenialAPIBroker(IAPIBrokerHelper apiHelper, string token)
        {
            ApiHelper = apiHelper;
            Token = token;
        }

        public async Task<string> GetVersion()
        {
            string apiCall = $"api/v1/FederalLicenceDenialFiles/Version";
            return await ApiHelper.GetString(apiCall, maxAttempts: 1, token: Token);
        }

        public async Task<string> GetConnection()
        {
            string apiCall = $"api/v1/FederalLicenceDenialFiles/DB";
            return await ApiHelper.GetString(apiCall, maxAttempts: 1, token: Token);
        }
    }
}