﻿using FOAEA3.Business.Areas.Application;
using FOAEA3.Common;
using FOAEA3.Common.Helpers;
using FOAEA3.Model;
using FOAEA3.Model.Constants;
using FOAEA3.Model.Enums;
using FOAEA3.Model.Interfaces.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FOAEA3.API.Areas.Application.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ApplicationEventDetailsController : FoaeaControllerBase
{
    [HttpGet("Version")]
    public ActionResult<string> GetVersion() => Ok("ApplicationEventDetails API Version 1.0");

    [HttpGet("DB")]
    [Authorize(Roles = Roles.Admin)]
    public ActionResult<string> GetDatabase([FromServices] IRepositories repositories) => Ok(repositories.MainDB.ConnectionString);

    [HttpGet("{id}/SIN")]
    public async Task<ActionResult<ApplicationEventDetailsList>> GetSINEvents([FromRoute] string id, [FromServices] IRepositories repositories)
    {
        return await GetEventsForQueue(id, repositories, EventQueue.EventSIN_dtl);
    }

    [HttpGet("{id}/Trace")]
    public async Task<ActionResult<ApplicationEventDetailsList>> GetTraceEvents([FromRoute] string id, [FromServices] IRepositories repositories)
    {
        return await GetEventsForQueue(id, repositories, EventQueue.EventTrace_dtl);
    }

    [HttpPost]
    public async Task<ActionResult> SaveEventDetail([FromServices] IRepositories repositories)
    {
        var applicationEventDetail = await APIBrokerHelper.GetDataFromRequestBody<ApplicationEventDetailData>(Request);

        var eventDetailManager = new ApplicationEventDetailManager(new ApplicationData(), repositories);

        await eventDetailManager.SaveEventDetail(applicationEventDetail);

        return Ok();

    }

    [HttpPost("Batch")]
    public async Task<ActionResult> SaveEventDetails([FromServices] IRepositories repositories)
    {
        var applicationEventDetails = await APIBrokerHelper.GetDataFromRequestBody<ApplicationEventDetailsList>(Request);

        var eventDetailManager = new ApplicationEventDetailManager(new ApplicationData(), repositories);
        eventDetailManager.EventDetails.AddRange(applicationEventDetails);
        await eventDetailManager.SaveEventDetails();

        return Ok();
    }

    [HttpPut]
    public async Task<ActionResult<ApplicationEventDetailData>> UpdateEventDetail([FromServices] IRepositories repositories,
                                                                                  [FromQuery] string command,
                                                                                  [FromQuery] string activeState,
                                                                                  [FromQuery] string applicationState,
                                                                                  [FromQuery] string enfSrvCode,
                                                                                  [FromQuery] string writtenFile)
    {
        var eventIds = await APIBrokerHelper.GetDataFromRequestBody<List<int>>(Request);

        var eventDetailManager = new ApplicationEventDetailManager(new ApplicationData(), repositories);

        if (command?.ToLower() == "markoutboundprocessed")
        {
            await eventDetailManager.UpdateOutboundEventDetail(activeState, applicationState, enfSrvCode, writtenFile, eventIds);
        }

        return Ok();

    }

    private async Task<ActionResult<ApplicationEventDetailsList>> GetEventsForQueue(string id, IRepositories repositories, EventQueue queue)
    {
        var applKey = new ApplKey(id);

        var manager = new ApplicationManager(new ApplicationData(), repositories, config, User);

        if (await manager.LoadApplication(applKey.EnfSrv, applKey.CtrlCd))
            return Ok(await manager.EventDetailManager.GetApplicationEventDetailsForQueue(queue));
        else
            return NotFound();
    }


}
