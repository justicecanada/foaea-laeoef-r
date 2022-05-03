﻿using FOAEA3.Business.Areas.Application;
using FOAEA3.Common.Helpers;
using FOAEA3.Model;
using FOAEA3.Model.Enums;
using FOAEA3.Model.Interfaces;
using FOAEA3.Resources.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace FOAEA3.API.Tracing.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TracingEventsController : ControllerBase
    {
        private readonly CustomConfig config;

        public TracingEventsController(IOptions<CustomConfig> config)
        {
            this.config = config.Value;
        }

        [HttpGet("Version")]
        public ActionResult<string> GetVersion() => Ok("TracingEvents API Version 1.0");

        [HttpGet("queues")]
        public ActionResult<Dictionary<int, string>> GetQueueNames()
        {
            var values = new Dictionary<int, string>();
            foreach (var g in Enum.GetValues(typeof(EventQueue)))
                values.Add((int)g, g?.ToString()?.Replace("Event", "Evnt"));

            return Ok(values);
        }

        [HttpGet("{id}")]
        public ActionResult<List<ApplicationEventData>> GetEvents([FromRoute] string id,
                                                                  [FromQuery] int? queue,
                                                                  [FromServices] IRepositories repositories)
        {
            EventQueue eventQueue;
            if (queue.HasValue)
                eventQueue = (EventQueue)queue.Value;
            else
                eventQueue = EventQueue.EventSubm;

            return GetEventsForQueue(id, repositories, eventQueue);
        }
               
        [HttpGet("RequestedTRCIN")]
        public ActionResult<ApplicationEventData> GetRequestedTRCINTracingEvents([FromQuery] string enforcementServiceCode,
                                                                                 [FromQuery] string fileCycle,
                                                                                 [FromServices] IRepositories repositories)
        {
            APIHelper.ApplyRequestHeaders(repositories, Request.Headers);
            APIHelper.PrepareResponseHeaders(Response.Headers);

            var manager = new TracingManager(repositories, config);

            if (string.IsNullOrEmpty(enforcementServiceCode))
                return BadRequest("Missing enforcementServiceCode parameter");

            if (string.IsNullOrEmpty(fileCycle))
                return BadRequest("Missing fileCycle parameter");

            var result = manager.GetRequestedTRCINTracingEvents(enforcementServiceCode, fileCycle);
            return Ok(result);

        }

        [HttpGet("Details/Active")]
        public ActionResult<ApplicationEventDetailData> GetActiveTracingEventDetails([FromQuery] string enforcementServiceCode,
                                                                                     [FromQuery] string fileCycle,
                                                                                     [FromServices] IRepositories repositories)
        {
            APIHelper.ApplyRequestHeaders(repositories, Request.Headers);
            APIHelper.PrepareResponseHeaders(Response.Headers);

            var manager = new TracingManager(repositories, config);

            var result = manager.GetActiveTracingEventDetails(enforcementServiceCode, fileCycle);

            return Ok(result);
        }

        private ActionResult<List<ApplicationEventData>> GetEventsForQueue(string id, IRepositories repositories, EventQueue queue)
        {
            APIHelper.ApplyRequestHeaders(repositories, Request.Headers);
            APIHelper.PrepareResponseHeaders(Response.Headers);

            var applKey = new ApplKey(id);

            var manager = new ApplicationManager(new ApplicationData(), repositories, config);

            if (manager.LoadApplication(applKey.EnfSrv, applKey.CtrlCd))
                return Ok(manager.EventManager.GetApplicationEventsForQueue(queue));
            else
                return NotFound();
        }
    }
}
