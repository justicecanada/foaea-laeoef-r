﻿using DBHelper;
using FOAEA3.Model.Interfaces.Repository;
using TestData.TestDB;

namespace TestData.TestDataBase
{
    public class InMemory_Repositories : IRepositories
    {
        public IDBToolsAsync MainDB { get; }

        public string CurrentUser { get; set; }

        public string CurrentSubmitter { get; set; }

        public string UpdateSubmitter { get; set; }

        public IApplicationRepository ApplicationTable { get; }

        public IApplicationEventRepository ApplicationEventTable { get; }

        public IApplicationSearchRepository ApplicationSearchTable { get; }

        public ISubmitterRepository SubmitterTable { get; }

        public IEnfOffRepository EnfOffTable { get; }

        public IEnfSrvRepository EnfSrvTable { get; }

        public IProvinceRepository ProvinceTable { get; }

        public ISubjectRepository SubjectTable { get; }

        public ITracingRepository TracingTable { get; }

        public IAffidavitRepository AffidavitTable { get; }

        public ILoginRepository LoginTable { get; }

        public ISubmitterProfileRepository SubmitterProfileTable { get; }

        public ISubjectRoleRepository SubjectRoleTable { get; }

        public ILicenceDenialRepository LicenceDenialTable => throw new System.NotImplementedException();

        public ISINResultRepository SINResultTable => throw new System.NotImplementedException();

        public ITraceResponseRepository TraceResponseTable => throw new System.NotImplementedException();

        public IProductionAuditRepository ProductionAuditTable { get; }

        public ISINChangeHistoryRepository SINChangeHistoryTable => throw new System.NotImplementedException();

        public IFamilyProvisionRepository FamilyProvisionTable => throw new System.NotImplementedException();

        public IInfoBankRepository InfoBankTable => throw new System.NotImplementedException();

        public IInterceptionRepository InterceptionTable { get; }

        public INotificationRepository NotificationService => throw new System.NotImplementedException();

        public IApplicationEventDetailRepository ApplicationEventDetailTable { get; }

        public ICaseManagementRepository CaseManagementTable => throw new System.NotImplementedException();

        public IAccessAuditRepository AccessAuditTable => throw new System.NotImplementedException();

        public IFailedSubmitAuditRepository FailedSubmitAuditTable => throw new System.NotImplementedException();

        public IPostalCodeRepository PostalCodeTable => throw new System.NotImplementedException();

        public ILicenceDenialResponseRepository LicenceDenialResponseTable => throw new System.NotImplementedException();

        public ISecurityTokenRepository SecurityTokenTable { get; }

        public IEnfSrcRepository EnfSrcTable => throw new System.NotImplementedException();

        public ICraSinPendingRepository CraSinPendingTable => throw new System.NotImplementedException();

        public InMemory_Repositories()
        {
            MainDB = new InMemory_MainDB();

            ApplicationTable = new InMemoryApplication();
            InterceptionTable = new InMemoryInterception();
            ApplicationEventTable = new InMemoryApplicationEvent();
            ApplicationEventDetailTable = new InMemoryApplicationEventDetail();
            // EventSINDetailRepository = new InMemoryEventSINDetail();
            ApplicationSearchTable = new InMemoryApplicationSearch();
            SubmitterTable = new InMemorySubmitter();
            EnfOffTable = new InMemoryEnfOff();
            EnfSrvTable = new InMemoryEnfSrv();
            ProvinceTable = new InMemoryProvince();
            SubjectTable = new InMemorySubject();
            TracingTable = new InMemoryTracing();
            AffidavitTable = new InMemoryAffidavit();
            LoginTable = new InMemoryLogin();
            SubmitterProfileTable = new InMemorySubmitterProfile();
            SubjectRoleTable = new InMemorySubjectRole();
            ProductionAuditTable = new InMemoryProductionAudit();
            SecurityTokenTable = new InMemorySecurityToken();
        }
    }
}
