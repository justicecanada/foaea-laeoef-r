﻿using FOAEA3.Business.Areas.Application;
using FOAEA3.Common;
using FOAEA3.Model;
using FOAEA3.Model.Constants;
using FOAEA3.Model.Interfaces.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FOAEA3.API.Interception.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EISOrequestsController : FoaeaControllerBase
    {
        [HttpGet("CRA")]
        [Authorize(Policy = Policies.ApplicationReadAccess)]
        public async Task<ActionResult<List<ProcessEISOOUTHistoryData>>> GetEISOvalidApplications([FromServices] IRepositories db,
                                                                                                  [FromServices] IRepositories_Finance dbFinance)
        {
            var manager = new InterceptionManager(db, dbFinance, config, User);
            return await manager.GetEISOvalidApplications();
        }

        [HttpGet("EI")]
        [Authorize(Policy = Policies.ApplicationReadAccess)]
        public async Task<ActionResult<List<EIoutgoingFederalData>>> GetEIvalidApplications(string enfSrv,
                                                                                            [FromServices] IRepositories db,
                                                                                            [FromServices] IRepositories_Finance dbFinance)
        {
            var manager = new InterceptionManager(db, dbFinance, config, User);
            return await manager.GetEIoutgoingData(enfSrv);
        }

    }
}
