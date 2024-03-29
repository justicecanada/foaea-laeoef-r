﻿using FOAEA3.Model.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FOAEA3.Model.Interfaces.Repository
{
    public interface ISummFAFRRepository
    {
        string CurrentSubmitter { get; set; }
        string UserId { get; set; }

        Task<DataList<SummFAFR_Data>> GetSummFaFr(int summFAFR_Id);
        Task<DataList<SummFAFR_Data>> GetSummFaFrList(List<SummFAFR_DE_Data> summFAFRs);
    }
}
