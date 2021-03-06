using FOAEA3.Model.Enums;
using System.Collections.Generic;

namespace FOAEA3.Model.Interfaces
{
    public interface ILicenceDenialRepository
    {
        public string CurrentSubmitter { get; set; }
        public string UserId { get; set; }

        LicenceDenialApplicationData GetLicenceDenialData(string appl_EnfSrv_Cd, string appl_L01_CtrlCd = null, string appl_L03_CtrlCd = null);
        List<LicenceSuspensionHistoryData> GetLicenceSuspensionHistory(string appl_EnfSrv_Cd, string appl_CtrlCd);

        void CreateLicenceDenialData(LicenceDenialApplicationData data);
        void UpdateLicenceDenialData(LicenceDenialApplicationData data);

        bool CloseSameDayLicenceEvent(string appl_EnfSrv_Cd, string appl_L01_CtrlCd, string appl_L03_CtrlCd);

        List<LicenceDenialOutgoingFederalData> GetFederalOutgoingData(int maxRecords,
                                                                string activeState,
                                                                ApplicationState lifeState,
                                                                string enfServiceCode);

        List<LicenceDenialToApplData> GetLicenceDenialToApplData(string fedSource);

        List<LicenceDenialOutgoingProvincialData> GetProvincialOutgoingData(int maxRecords,
                                                                            string activeState,
                                                                            string recipientCode,
                                                                            bool isXML = true);
    }
}
