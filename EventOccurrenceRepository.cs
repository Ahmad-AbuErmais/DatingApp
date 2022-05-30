using EskaCMS.Core.Enums;
using EskaCMS.Core.Extensions;
using EskaCMS.Events.Entities;
using EskaCMS.Events.Models;
using EskaCMS.Events.Repositories.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;
using static EskaCMS.Events.Enums.EventEnums;

namespace EskaCMS.Events.Repositories
{
    public class EventOccurrenceRepository : IEventOccurrenceRepository
    {
        private readonly IRepository<EventOccurrence> _eventOccurrenceRepository;
        private readonly IWorkContext _workContext;

        public EventOccurrenceRepository(IRepository<EventOccurrence> eventOccurrenceRepository,
            IWorkContext workContext)
        {
            _eventOccurrenceRepository = eventOccurrenceRepository;
            _workContext = workContext;
        }

        public async Task<EventOccurrenceVM> GetById(long id)
        {
            return await _eventOccurrenceRepository.Query().Where(x => x.Id == id).Select(x => new EventOccurrenceVM
            {
                Description = x.Description,
                ModifiedById = x.ModifiedById,
                ModificationDate = DateTimeOffset.Now,
                EventId = x.EventId,
                FromTime = x.FromTime,
                LocationId = x.LocationId,
                Status = x.Status,
                ThumbnailId = x.ThumbnailId,
                MaxEnrollment = x.MaxEnrollment,
                Name = x.Name,
                Settings = x.Settings,
                ReservationDate = x.ReservationDate,
                ToTime = x.ToTime,
                CreatedById = x.CreatedById

            }).FirstOrDefaultAsync();
        }

        public async Task<long> Create(EventOccurrenceVM OccurrenceVM)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync();
            var userId = await _workContext.GetCurrentUserId();

            var OccurrenceEntity = new EventOccurrence();
            OccurrenceEntity.CreatedById = userId;
            OccurrenceEntity.CreationDate = DateTimeOffset.Now;
            OccurrenceEntity.Description = OccurrenceVM.Description;
            OccurrenceEntity.EventId = OccurrenceVM.EventId;
            OccurrenceEntity.ReservationDate = OccurrenceVM.ReservationDate;
            OccurrenceEntity.ToTime = OccurrenceVM.ToTime;
            OccurrenceEntity.ThumbnailId = OccurrenceVM.ThumbnailId;
            OccurrenceEntity.Status = EStatus.Active;
            OccurrenceEntity.Settings = OccurrenceVM.Settings;
            OccurrenceEntity.LocationId = OccurrenceVM.LocationId;
            OccurrenceEntity.MaxEnrollment = OccurrenceVM.MaxEnrollment;
            OccurrenceEntity.FromTime = OccurrenceVM.FromTime;
            OccurrenceEntity.SiteId = siteId;

            _eventOccurrenceRepository.Add(OccurrenceEntity);
            await _eventOccurrenceRepository.SaveChangesAsync();

            return OccurrenceEntity.Id;
        }

        public async Task  Delete(long Id)
        {
            var reservation = await _eventOccurrenceRepository.Query().FirstOrDefaultAsync(x => x.Id == Id);
            reservation.Status = GeneralEnums.EStatus.Deleted /*Delete*/;
            _eventOccurrenceRepository.Update(reservation);
            await _eventOccurrenceRepository.SaveChangesAsync();
        }

        public async Task<bool> Update(EventOccurrenceVM OccurrenceVM, long OccurrenceId)
        {
            var EntityOccurance = await _eventOccurrenceRepository.Query().Where(x => x.Id == OccurrenceId).FirstOrDefaultAsync();
            EntityOccurance.Description = OccurrenceVM.Description;
            EntityOccurance.ModifiedById = OccurrenceVM.ModifiedById;
            EntityOccurance.ModificationDate = DateTimeOffset.Now;
            EntityOccurance.EventId = OccurrenceVM.EventId;
            EntityOccurance.ReservationDate = OccurrenceVM.ReservationDate;
            EntityOccurance.FromTime = OccurrenceVM.FromTime;
            EntityOccurance.LocationId = OccurrenceVM.LocationId;
            EntityOccurance.Status = OccurrenceVM.Status;
            EntityOccurance.ThumbnailId = OccurrenceVM.ThumbnailId;
            EntityOccurance.MaxEnrollment = OccurrenceVM.MaxEnrollment;
            EntityOccurance.Name = OccurrenceVM.Name;
            EntityOccurance.Settings = OccurrenceVM.Settings;
            EntityOccurance.ToTime = OccurrenceVM.ToTime;

            _eventOccurrenceRepository.Update(EntityOccurance);
            await _eventOccurrenceRepository.SaveChangesAsync();
            return true;
        }

        public async Task<EventOccurrenceVM> GetEventOccuranceByEventId(long EventId, EStatus? Status)
        {
            return await _eventOccurrenceRepository.Query().Where(x => x.EventId == EventId && ((x.Status == Status) || Status == null)).Select(x => new EventOccurrenceVM
            {
                Description = x.Description,
                ModifiedById = x.ModifiedById,
                ModificationDate = DateTimeOffset.Now,
                EventId = x.EventId,
                FromTime = x.FromTime,
                LocationId = x.LocationId,
                Status = x.Status,
                ThumbnailId = x.ThumbnailId,
                MaxEnrollment = x.MaxEnrollment,
                Name = x.Name,
                Settings = x.Settings,
                ReservationDate = x.ReservationDate,
                ToTime = x.ToTime

            }).FirstOrDefaultAsync();
        }

        public async Task<List<EventOccurrenceVM>> GetEventOccuranceByDate(DateTimeOffset ReservationDate, long? LocationId, EStatus? Status = null)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync();
            return await _eventOccurrenceRepository.Query().Include(x => x.CreatedBy).Where(x => x.SiteId == siteId &&
            x.ReservationDate.Date.Equals(ReservationDate.Date) && (x.LocationId == LocationId || LocationId == null)
             && (x.Status != EStatus.Deleted && ((x.Status == Status) || (Status == null)))).Select(x => new EventOccurrenceVM
            {
                Id=x.Id,
                Description = x.Description,
                ModifiedById = x.ModifiedById,
                ModificationDate = DateTimeOffset.Now,
                EventId = x.EventId,
                FromTime = x.FromTime,
                LocationId = x.LocationId,
                Status = x.Status,
                ThumbnailId = x.ThumbnailId,
                MaxEnrollment = x.MaxEnrollment,
                Name = x.Name,
                Settings = x.Settings,
                ReservationDate = x.ReservationDate,
                ToTime = x.ToTime,
                CreatedById = x.CreatedById,
                CoreUsername = x.CreatedBy.UserName
            }).ToListAsync();
        }

       public async Task<List<EventOccurrenceVM>> GetEventOccuranceByLocationId(long LocationId, EStatus? Status=null)
        {
            return await _eventOccurrenceRepository.Query().Include(x => x.CreatedBy)
                .Where(x => x.LocationId == LocationId && ((x.Status == Status) || Status == null)).Select(x => new EventOccurrenceVM
            {
                Description = x.Description,
                ModifiedById = x.ModifiedById,
                ModificationDate = DateTimeOffset.Now,
                EventId = x.EventId,
                ReservationDate = x.ReservationDate,
                FromTime = x.FromTime,
                LocationId = x.LocationId,
                Status = x.Status,
                ThumbnailId = x.ThumbnailId,
                MaxEnrollment = x.MaxEnrollment,
                Name = x.Name,
                Settings = x.Settings,
                ToTime = x.ToTime,
                CreatedById = x.CreatedById,
                CoreUsername = x.CreatedBy.UserName
            }).ToListAsync();
        }
    }
}
