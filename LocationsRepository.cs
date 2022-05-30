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
    public class LocationsRepository : ILocationsRepository
    {
        private readonly IRepository<EventLocation> _eventLocationRepository;
        private readonly IWorkContext _workContext;
        public LocationsRepository(
            IRepository<EventLocation> eventLocationRepository,
            IWorkContext workContext)
        {
            _eventLocationRepository = eventLocationRepository;
            _workContext = workContext;
        }

        public async Task<EventLocationVM> GetById(long id)
        {
            return await _eventLocationRepository.Query().Where(x => x.Id == id).Select(x => new EventLocationVM
            {
                Name = x.Name,
                LocationMapURL = x.LocationMapURL,
                ThumbnailId = x.ThumbnailId,
                Status = x.Status,
                CreationDate = x.CreationDate,
                Settings = x.Settings,
                SiteId = x.SiteId,
                CultureId = x.CultureId,
                CategoryId = x.CategoryId
            }).FirstOrDefaultAsync();
        }

        public async Task<long> Create(EventLocationVM Location)
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            var LocationEntity = new EventLocation();
            LocationEntity.CreatedById = await _workContext.GetCurrentUserId();
            LocationEntity.CreationDate = DateTimeOffset.Now;
            LocationEntity.CultureId = Location.CultureId;
            LocationEntity.LocationMapURL = Location.LocationMapURL;
            LocationEntity.Name = Location.Name;
            LocationEntity.Settings = Location.Settings;
            LocationEntity.SiteId = SiteId;
            LocationEntity.Status = Location.Status;
            LocationEntity.CategoryId = Location.CategoryId.HasValue && Location.CategoryId != 0 ? Location.CategoryId : null;
            LocationEntity.ThumbnailId = Location.ThumbnailId;
            _eventLocationRepository.Add(LocationEntity);
            await _eventLocationRepository.SaveChangesAsync();

            return LocationEntity.Id;
        }

        public async Task Delete(long id)
        {
            var location = await _eventLocationRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            location.Status = GeneralEnums.EStatus.Deleted /*Delete*/;
            _eventLocationRepository.Update(location);
            await _eventLocationRepository.SaveChangesAsync();
        }

        public async Task<bool> Update(EventLocationVM LocationVM, long LocationId)
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            var UserId = await _workContext.GetCurrentUserId();
            EventLocation EntityLocation = new EventLocation();
            EntityLocation = await _eventLocationRepository.Query().Where(x => x.Id == LocationId).FirstOrDefaultAsync();
            EntityLocation.CultureId = LocationVM.CultureId != 0 ? LocationVM.CultureId : EntityLocation.CultureId;
            EntityLocation.ModifiedById = UserId;
            EntityLocation.ModificationDate = DateTimeOffset.Now;
            EntityLocation.LocationMapURL = LocationVM.LocationMapURL;
            EntityLocation.Name = LocationVM.Name;
            EntityLocation.Settings = LocationVM.Settings;
            EntityLocation.SiteId = SiteId;
            EntityLocation.Status = LocationVM.Status != 0 ? LocationVM.Status : EntityLocation.Status;
            EntityLocation.ThumbnailId = LocationVM.ThumbnailId != 0 ? LocationVM.ThumbnailId : null;
            EntityLocation.Longitude = LocationVM.Longitude;
            EntityLocation.Latitude = LocationVM.Latitude;
            EntityLocation.CategoryId = LocationVM.CategoryId;
            _eventLocationRepository.Update(EntityLocation);
            await _eventLocationRepository.SaveChangesAsync();
            return true;
        }


        public async Task<List<EventLocationVM>> GetLocationsBySiteId(long CultureId, Core.Enums.GeneralEnums.EStatus? Status)
        {

            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            return await _eventLocationRepository.Query().Include(x => x.Category)
                .Where(x => x.SiteId == SiteId &&
                x.Status != EStatus.Deleted &&
            x.CultureId == CultureId &&
            (Status == null  ||  x.Status == Status ))
                .Select(x => new EventLocationVM
            {
                Id = x.Id,
                Name = x.Name,
                LocationMapURL = x.LocationMapURL,
                ThumbnailId = x.ThumbnailId,
                Status = x.Status,
                CreationDate = x.CreationDate,
                Settings = x.Settings,
                SiteId = x.SiteId,
                CultureId = x.CultureId,
                CategoryId = x.CategoryId,
                Latitude = x.Latitude,
                Longitude = x.Longitude
            }).ToListAsync();
        }

        public async Task<bool> ToggleStatus(long LocationId , Core.Enums.GeneralEnums.EStatus LocationStatus)
        {
            var userId = await _workContext.GetCurrentUserId();
            var EntityLocationEvent = await _eventLocationRepository.Query().Where(x => x.Id == LocationId).FirstOrDefaultAsync();
            EntityLocationEvent.ModifiedById = userId;
            EntityLocationEvent.ModificationDate = DateTimeOffset.Now;
            EntityLocationEvent.Status = LocationStatus == EStatus.Active ? EStatus.Inactive : EStatus.Active;
            _eventLocationRepository.Update(EntityLocationEvent);
            await _eventLocationRepository.SaveChangesAsync();
            return true;
        }




    }
}
