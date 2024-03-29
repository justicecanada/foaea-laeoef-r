﻿using FOAEA3.Business.Areas.Application;
using FOAEA3.Common;
using FOAEA3.Common.Helpers;
using FOAEA3.Model;
using FOAEA3.Model.Constants;
using FOAEA3.Model.Enums;
using FOAEA3.Model.Interfaces.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FOAEA3.API.Tracing.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class TracingEventsController : FoaeaControllerBase
{
    [HttpGet("Version")]
    public ActionResult<string> GetVersion() => Ok("TracingEvents API Version 1.0");

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
    public async Task<ActionResult<ApplicationEventsList>> GetEvents([FromRoute] ApplKey id,
                                                              [FromQuery] int? queue,
                                                              [FromServices] IRepositories repositories)
    {
        EventQueue eventQueue;
        if (queue.HasValue)
            eventQueue = (EventQueue)queue.Value;
        else
            eventQueue = EventQueue.EventSubm;

        var manager = new ApplicationManager(new ApplicationData(), repositories, config, User);

        if (await manager.LoadApplication(id.EnfSrv, id.CtrlCd))
            return Ok(await manager.EventManager.GetApplicationEventsForQueue(eventQueue));
        else
            return NotFound();

    }

    [HttpGet("RequestedTRCIN")]
    public async Task<ActionResult<ApplicationEventsList>> GetRequestedTRCINTracingEvents([FromQuery] string enforcementServiceCode,
                                                                             [FromQuery] string fileCycle,
                                                                             [FromServices] IRepositories repositories)
    {
        var manager = new TracingManager(repositories, config, User);

        if (string.IsNullOrEmpty(enforcementServiceCode))
            return BadRequest("Missing enforcementServiceCode parameter");

        if (string.IsNullOrEmpty(fileCycle))
            return BadRequest("Missing fileCycle parameter");

        var result = await manager.GetRequestedTRCINTracingEvents(enforcementServiceCode, fileCycle);
        return Ok(result);
    }

    [HttpGet("Details/Active")]
    public async Task<ActionResult<ApplicationEventDetailsList>> GetActiveTracingEventDetails([FromQuery] string enforcementServiceCode,
                                                                                 [FromQuery] string fileCycle,
                                                                                 [FromServices] IRepositories repositories)
    {
        var manager = new TracingManager(repositories, config, User);

        var result = await manager.GetActiveTracingEventDetails(enforcementServiceCode, fileCycle);

        return Ok(result);
    }

}
