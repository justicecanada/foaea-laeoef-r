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
public class ApplicationEventsController : FoaeaControllerBase
{
    [HttpGet("Version")]
    public ActionResult<string> GetVersion() => Ok("ApplicationEvents API Version 1.0");

    [HttpGet("DB")]
    [Authorize(Roles = Roles.Admin)]
    public ActionResult<string> GetDatabase([FromServices] IRepositories repositories) => Ok(repositories.MainDB.ConnectionString);

    [HttpGet("queues")]
    public ActionResult<Dictionary<int, string>> GetQueueNames()
    {
        var values = new Dictionary<int, string>();
        foreach (var g in Enum.GetValues(typeof(EventQueue)))
            values.Add((int)g, g?.ToString()?.Replace("Event", "Evnt"));

        return Ok(values);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationEventsList>> GetEvents([FromRoute] string id,
                                                                          [FromQuery] int? queue,
                                                                          [FromServices] IRepositories repositories)
    {
        EventQueue eventQueue;
        if (queue.HasValue)
            eventQueue = (EventQueue)queue.Value;
        else
            eventQueue = EventQueue.EventSubm;

        return await GetEventsForQueue(id, repositories, eventQueue);
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationEventData>> SaveEvent([FromServices] IRepositories repositories)
    {
        var applicationEvent = await APIBrokerHelper.GetDataFromRequestBody<ApplicationEventData>(Request);

        var eventManager = new ApplicationEventManager(new ApplicationData(), repositories);

        await eventManager.SaveEvent(applicationEvent);

        return Ok();

    }

    private async Task<ActionResult<ApplicationEventsList>> GetEventsForQueue(string id, IRepositories repositories, EventQueue queue)
    {
        var applKey = new ApplKey(id);

        var manager = new ApplicationManager(new ApplicationData(), repositories, config, User);

        if (await manager.LoadApplication(applKey.EnfSrv, applKey.CtrlCd))
            return Ok(await manager.EventManager.GetApplicationEventsForQueue(queue));
        else
            return NotFound();
    }

    [HttpGet("GetLatestSinEventDataSummary")]
    public async Task<ActionResult<List<SinInboundToApplData>>> GetLatestSinEventDataSummary([FromServices] IRepositories repositories)
    {
        var applManager = new ApplicationEventManager(new ApplicationData(), repositories);

        return await applManager.GetLatestSinEventDataSummary();
    }

}
