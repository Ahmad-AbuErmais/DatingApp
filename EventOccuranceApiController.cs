using EskaCMS.Core.Entities;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Events.Models;
using EskaCMS.Events.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;
using static EskaCMS.Events.Enums.EventEnums;


namespace EskaCMS.Events.Controllers
{
    [Route("api/EventsOccurance")]
    [ApiController]
    [Authorize]
    public class EventOccuranceApiController : ControllerBase
    {
        private readonly IEventsService _eventsService;
        private readonly IWorkContext _workContext;
        public EventOccuranceApiController(IEventsService eventsService, IWorkContext workContext)
        {
            _eventsService = eventsService;
            _workContext = workContext;
        }

        #region EventOccurrence APIs
        [HttpPost]
        [Route("AddEventOccurrence")]
        public async Task<IActionResult> AddEventOccurrence(EventOccurrenceVM OccurrenceVM)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _eventsService.AddEventOccurrence(OccurrenceVM));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("EditEventOccurrence")]
        public async Task<IActionResult> EditEventOccurrence(EventOccurrenceVM OccurrenceVM, long OccurrenceId)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _eventsService.EditEventOccurrence(OccurrenceVM, OccurrenceId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetEventOccuranceById/{EventOccuranceId}")]
        public async Task<IActionResult> GetEventOccuranceById(long EventOccuranceId)
        {
            try
            {

                return Ok(await _eventsService.GetEventOccuranceById(EventOccuranceId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetEventOccuranceEventId/{EventId}/{Status}")]
        public async Task<IActionResult> GetEventOccuranceEventId(long EventId, EStatus? Status)
        {
            try
            {

                return Ok(await _eventsService.GetEventOccuranceEventId(EventId, Status.Value));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetEventOccuranceByDate/{ReservationDate}/{LocationId?}/{Status?}")]
        public async Task<IActionResult> GetEventOccuranceByDate(DateTimeOffset ReservationDate,long? LocationId, EStatus? Status)
        {
            try
            {

                return Ok(await _eventsService.GetEventOccuranceByDate(ReservationDate, LocationId, Status));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetEventOccuranceLocationId/{LocationId}/{Status}")]
        public async Task<IActionResult> GetEventOccuranceLocationId(long LocationId, EStatus? Status)
        {
            try
            {
                return Ok(await _eventsService.GetEventOccuranceLocationId(LocationId, Status.Value));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteReservation/{ReservationId}")]
        public async Task<IActionResult> DeleteReservation(long ReservationId)
        {
            try
            {
                await _eventsService.DeleteEventOccurrence(ReservationId);
                return Ok(true);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }
        #endregion
    }
}
