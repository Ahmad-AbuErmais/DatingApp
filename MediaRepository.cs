

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Core.Entities;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using AutoMapper;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Data;
using EskaCMS.Core.ViewModels;
using System.IO;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.Core.Repositories
{
    public class MediaRepository : IMediaRepository
    {

        private readonly IRepository<Media> _mediaRepository;
        private readonly IRepository<MediaDetails> _MediaDetailsRepository;
        private readonly IRepository<MediaFoldersDetails> _mediafoldersDetailsRepository;
        private readonly IRepository<MediaFolders> _mediafoldersRepository;
        private readonly IRepository<MediaVersion> _mediaversionsRepository;
        private readonly IWorkContext _WorkContext;
        private readonly IMapper _mapper;
        private readonly EskaDCMSDbContext _context;
        class FolderCount
        {
            public long SiteId { get; set; }
            public long? FolderId { get; set; }
            public long? Order { get; set; }
            public int Count { get; set; }
        }



        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="db">The current db context</param>
        public MediaRepository(
            IRepository<Media> mediaRepository,
            IRepository<MediaFolders> mediafoldersRepository,
            IMapper mapper,
            IRepository<MediaVersion> mediaversionsRepository,
            IRepository<MediaDetails> MediaDetailsRepository,
            IRepository<MediaFoldersDetails> mediafoldersDetailsRepository,
            IWorkContext WorkContext,
            EskaDCMSDbContext context)
        {
            _mediaRepository = mediaRepository;
            _mediafoldersRepository = mediafoldersRepository;
            _mapper = mapper;
            _mediaversionsRepository = mediaversionsRepository;
            _WorkContext = WorkContext;
            _MediaDetailsRepository = MediaDetailsRepository;
            _context = context;
            _mediafoldersDetailsRepository = mediafoldersDetailsRepository;

        }

        /// <summary>
        /// Gets all media available in the specified folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media</returns>
        public async Task<IEnumerable<long>> GetAll(long siteId, long? folderId = null) =>
            await _mediaRepository.Query()
                .AsNoTracking()
                .Where(m => m.FolderId == folderId && m.SiteId == siteId)
                .Include(m => m.MediaDetails)
                .OrderBy(m => m.Filename)
                .Select(m => m.Id)
                .ToListAsync()
                .ConfigureAwait(false);

        /// <summary>
        /// <inheritdoc cref="IMediaRepository.CountAll"/>
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public Task<int> CountAll(long? folderId) =>
            _mediaRepository.Query()
                .AsNoTracking()
                .Where(m => m.FolderId == folderId).CountAsync();

        /// <summary>
        /// Gets all media folders available in the specified
        /// folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media folders</returns>
        public async Task<IEnumerable<long>> GetAllFolders(long siteId, long? folderId = null) =>
            await _mediafoldersRepository.Query()
                .AsNoTracking()
                .Where(f => f.ParentId == folderId && f.SiteId == siteId)
                //.OrderBy(f => f.Name)
                .Select(f => f.Id)
                .ToListAsync()
                .ConfigureAwait(false);

        /// <summary>
        /// Get media for all Ids in this enumerable.
        /// </summary>
        /// <param name="ids">One or several media id</param>
        /// <returns>The matching media</returns>
        public Task<IEnumerable<MediaVM>> GetById(params long[] ids)
        {

            var Medias = _mediaRepository.Query().AsNoTracking()
              .Include(c => c.Versions)
              .Where(m => ids.Contains(m.Id))
              .Include(m => m.MediaDetails)
              .OrderBy(m => m.Filename)
              .ToArrayAsync()
              .ContinueWith(t => t.Result.Select(m => (Models.MediaVM)m));

            return Medias;
        }


        /// <summary>
        /// Gets the media with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media</returns>
        public Task<MediaVM> GetById(long id) =>
          _mediaRepository.Query()
                .AsNoTracking()
                .Include(m => m.Versions)
                .Include(m => m.MediaDetails)
                .FirstOrDefaultAsync(m => m.Id == id).ContinueWith(t => (Models.MediaVM)t.Result);

        public Task<List<Media>> GetByFileName(string filename, long siteId) =>
        _mediaRepository.Query()
        .AsNoTracking()
        .Where(m => m.SiteId == siteId && m.Filename.Equals(filename))
            .Include(m => m.MediaDetails).ToListAsync();

        public Task<List<MediaDetailsWithOrderVM>> GetMediaDetails(long MediaId, long CultureId = 0) =>
        _MediaDetailsRepository.Query()
        .AsNoTracking()
        .Where(m =>
                    m.MediaId == MediaId
                    && (CultureId == 0 || m.CultureId == CultureId)
            )
            .Include(m => m.Media)
            .Select(m => new MediaDetailsWithOrderVM()
            {
                CreatedById = m.CreatedById,
                CultureId = m.CultureId,
                Culture = m.Culture,
                Description = m.Description,
                AltText = m.AltText,
                Order = m.Media.Order,
                Id = m.Id,
                Media = m.Media,
                MediaId = m.MediaId,
                ModifiedById = m.ModifiedById,
                Title = m.Title
            }).ToListAsync();



        public Task<List<MediaFolderVM>> GetByFolderName(string folderCode, long siteId) =>
        _mediafoldersRepository.Query()
        .AsNoTracking()
        .Where(m => m.SiteId == siteId).Select(x => new MediaFolderVM
        {
            Id = x.Id,
            ParentId = x.ParentId,
            Order = x.Order
            //Code = x.Code,
            //Description = x.Description,
            //Title = x.Title,
            //CultureId = x.CultureId,
            //Name = x.Name,
            //SiteId = x.SiteId
        }).ToListAsync();


        /// <summary>
        /// Gets the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media folder</returns>
        public Task<MediaFolderVM> GetFolderById(long siteId, long id, long CultureId) =>
           _mediafoldersRepository.Query()
                .AsNoTracking()
                .Include(m => m.MediaFoldersDetails)
                .Where(f => f.Id == id && f.SiteId == siteId)
                .Select(f => new MediaFolderVM
                {
                    Id = f.Id,
                    ParentId = f.ParentId,
                    Title = f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0) == null
                                ? ""
                                : f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0).Title,

                    Description = f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0) == null
                                ? ""
                                : f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0).Description,

                    ThumbnailId = f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0) == null
                                ? 0
                                : f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0).ThumbnailId,
                    MediaFoldersDetails = f.MediaFoldersDetails.ToList(),
                    Order = f.Order
                    //Name = f.Name,
                    //Title = f.Title,
                    //Description = f.Description,
                    //CreationDate = f.CreationDate,
                    //SiteId = f.SiteId,
                    //CultureId = f.CultureId,
                    //Code = f.Code
                })
                .FirstOrDefaultAsync();

        /// <summary>
        /// Gets the hierachical media structure.
        /// </summary>
        /// <returns>The media structure</returns>
        public async Task<MediaStructure> GetStructure(long siteId, long CultureId = 0)
        {
            var folders = await _mediafoldersRepository.Query()
                .AsNoTracking()
                .Include(f => f.MediaFoldersDetails)
                .Where(x =>
                        x.SiteId == siteId &&
                    (
                        x.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId) != null
                        || CultureId == 0)
                    )
                .OrderBy(f => f.ParentId)
                .ThenBy(f => f.Order)
                //.ThenBy(f => f.Name)
                .ToListAsync()
                .ConfigureAwait(false);

            var count = await _mediaRepository.Query()
                .AsNoTracking()
                .Where(x => x.SiteId == siteId)
                .GroupBy(m => m.FolderId)
                .Select(m => new FolderCount
                {
                    FolderId = m.Key,
                    Count = m.Count()

                })
                .ToListAsync()
                .ConfigureAwait(false);

            return Sort(folders, count);
        }

        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="model">The model to save</param>
        public async Task<MediaVM> Save(MediaVM model)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Create  Query For Media

                    var media = await _mediaRepository.Query()
                   .Include(m => m.Versions)
                   .FirstOrDefaultAsync(m => m.Id == model.Id)
                   .ConfigureAwait(false);

                    //var fileNameWithoutExt = model.Filename.Substring(0, model.Filename.LastIndexOf("."));

                    //model.Filename = String.Concat(fileNameWithoutExt, "(", SimilarNameCount.ToString(), ")", Path.GetExtension(model.Filename));
                    if (media == null)
                    {
                        media = new Media
                        {
                            CreationDate = DateTimeOffset.Now,
                            Filename = model.Filename
                        };


                        //Invoke Function If Have Any Duplicate FileNames And Return Boolean Value

                        var CheckFileNameDuplicate = await SimilarCountNamesInSameFolders(media.Filename, model.FolderId, model.SiteId);

                        if (model.Force == false && CheckFileNameDuplicate)
                        {
                             throw new Exception("Duplicate File Name");
                           
                        }
                        else
                        {

                            //media.Filename

                            if (CheckFileNameDuplicate)
                            {
                                // delete the file by fileName 
                                var MediaToDelete = await _mediaRepository.Query().Where(x => x.Filename == model.Filename &&x.FolderId==model.FolderId&&x.SiteId==model.SiteId).FirstOrDefaultAsync().ConfigureAwait(false);

                                if (MediaToDelete != null)
                                {
                                
                                    _mediaRepository.Remove(MediaToDelete);
                               
                                }

                            }
                        }
                        _mediaRepository.Add(media);
                    }
                    //media.Filename = model.Filename;
                    media.FolderId = model.FolderId;
                    media.Type = model.Type;
                    media.Size = model.Size;
                    media.Width = model.Width;
                    media.Order = model.Order;
                    media.Height = model.Height;
                    media.ContentType = model.ContentType;
                    media.Properties = Media.SerializeProperties(model.Properties);
                    media.ModificationDate = DateTimeOffset.Now;
                    media.SiteId = model.SiteId;
                    media.PublicUrl = model.PublicUrl;
                    // Delete removed versions

                    var current = model.Versions.Select(v => v.Id).ToArray();
                    var removed = media.Versions.Where(v => !current.Contains(v.Id)).ToArray();

                    if (removed.Length > 0)
                    {
                        for (int i = 0; i < removed.Length; i++)
                        {
                            _mediaversionsRepository.Remove(removed[i]);
                        }

                    }

                    // Add new versions
                    foreach (var version in model.Versions)
                    {
                        if (media.Versions.All(v => v.Id != version.Id))
                        {
                            var mediaVersion = new MediaVersion
                            {
                                //Id = version.Id,
                                MediaId = media.Id,
                                Size = version.Size,
                                Width = version.Width,
                                Height = version.Height,
                                FileExtension = version.FileExtension
                            };
                            _mediaversionsRepository.Add(mediaVersion);
                            media.Versions.Add(mediaVersion);
                        }
                    }

                    // Save all changes
                    await _mediaRepository.SaveChangesAsync().ConfigureAwait(false);
                    await transaction.CommitAsync();
                    return media;

                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw ex;

                }
            }
        }

        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="model">The model</param>
        public async Task SaveFolder(MediaFolderVM model)
        {
            try
            {


                var folderList = await _mediafoldersRepository.Query()
                    .Where(x => x.SiteId == model.SiteId).Include(x => x.MediaFoldersDetails)
                    .ToListAsync()
                    .ConfigureAwait(false);
                var folder = new MediaFolders();

                folder = folderList.Where(f => f.MediaFoldersDetails.FirstOrDefault().CultureId == model.CultureId).FirstOrDefault();

                if (folder == null)
                {

                    folder = new MediaFolders()
                    {
                        // Id = model.Id != Guid.Empty ? model.Id : Guid.NewGuid(),
                        CreationDate = DateTimeOffset.Now,
                        ParentId = model.ParentId,
                        Order = model.Order
                    };
                    model.Id = folder.Id;
                    //if (folderList.Count() > 0)
                    //{
                    //    folder.Code = folderList.FirstOrDefault().Code;
                    //}
                    _mediafoldersRepository.Add(folder);

                    //  model.Id = folder.Id;

                }

                //folder.Description = model.Description;
                folder.ParentId = model.ParentId;
                //folder.Name = model.Name;
                //folder.Title = model.Title;
                //folder.SiteId = model.SiteId;
                //folder.CultureId = model.CultureId;
                //folder.CreatedById = model.CreatedById;

                await _mediafoldersRepository.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Moves the media to the folder with the specified id.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="folderId">The folder id</param>
        public async Task Move(Models.MediaVM model, long? folderId, string? OldBreadCramb = null, string? NewBreadCramb = null,bool Force=false)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var media = await _mediaRepository.Query()
                        .FirstOrDefaultAsync(m => m.Id == model.Id)
                        .ConfigureAwait(false);

                    // If Media Null                  
                    if (media == null)
                    {
                        throw new Exception("File not found");
                    }
                    //After Check Now Will Change The Directory

                    var OldPublicUrl = media.PublicUrl;

                    var OldFileName = media.Filename;                   

                    if(!string.IsNullOrEmpty(OldBreadCramb))
                    {
                        OldFileName = OldFileName.Replace(OldBreadCramb, "");

                    }
                    media.Filename = String.Concat(NewBreadCramb, "-", OldFileName); 
                    
                    if (OldBreadCramb.Length == 0)
                    {
                        media.PublicUrl = OldPublicUrl.Substring(0, OldPublicUrl.LastIndexOf("/") + 1) + media.Filename;
                    }

                    else
                    {
                        media.PublicUrl = OldPublicUrl.Replace(OldBreadCramb, NewBreadCramb);
                    }

                   
                    media.FolderId = folderId;

                    var CheckFileNamesDuplicate = await SimilarCountNamesInSameFolders(media.Filename, folderId, model.SiteId);

                    if (Force == false && CheckFileNamesDuplicate)
                    {
                        throw new Exception("Duplicate File Name");
                    }

                    else if(CheckFileNamesDuplicate)
                    {
                        
                            // delete the file by fileName 
                            var MediaToDelete = await _mediaRepository.Query().Where(x => x.Filename == media.Filename && x.FolderId == folderId && x.SiteId == model.SiteId).FirstOrDefaultAsync().ConfigureAwait(false);

                            if (MediaToDelete != null)
                            {

                                _mediaRepository.Remove(MediaToDelete);

                                File.Delete("wwwroot/" + media.PublicUrl);


                            }
                        
                    }
                        await _mediaRepository.SaveChangesAsync().ConfigureAwait(false);
                        MoveFileOrRename(OldPublicUrl, media.PublicUrl);
                        await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw ex;
                }
            }
        }
        /// <summary>
        /// Count The FileNames Have The Same Names In Same Folder And SiteId
        /// </summary>
        /// <param name="NewFileName"></param>
        /// <param name="folderId"></param>
        /// <param name="SiteId"></param>
        /// <returns></returns>

        private async Task<bool> SimilarCountNamesInSameFolders(string NewFileName,long?folderId,long?SiteId)
        {
          var CountNamesSimilar=  await _mediaRepository.Query().
                  Where(x => x.FolderId == folderId &&
                              x.SiteId == SiteId &&
                              x.Filename.ToLower() == NewFileName.ToLower())
                  .CountAsync()
                  .ConfigureAwait(false);
            if (CountNamesSimilar != 0)
                return true;
            return false;

        }
        /// <summary>
        /// Deletes the media with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        public async Task Delete(long id)
        {
            try
            {
                var media = await _mediaRepository.Query()
                .Include(m => m.Versions)
                .FirstOrDefaultAsync(m => m.Id == id)
                .ConfigureAwait(false);

                if (media != null)
                {
                    _mediaRepository.Remove(media);
                    await _mediaRepository.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Deletes the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        public async Task DeleteFolder(long id)
        {
            var folder = await _mediafoldersRepository.Query()
                .Include(f => f.MediaFoldersDetails)
                .FirstOrDefaultAsync(f => f.Id == id)
                .ConfigureAwait(false);

            if (folder != null)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (folder == null)
                        {
                            throw new Exception("Could Not Find Folder");
                        }
                        if (folder != null && folder.MediaFoldersDetails != null)
                        {
                            _mediafoldersDetailsRepository.RemoveRange(folder.MediaFoldersDetails);
                            await _mediafoldersDetailsRepository.SaveChangesAsync().ConfigureAwait(false);
                        }
                        if (folder != null)
                        {
                            _mediafoldersRepository.Remove(folder);
                            await _mediafoldersRepository.SaveChangesAsync().ConfigureAwait(false);
                        }
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();

                        throw new Exception("Could Not Delete Folder");
                    }

                }

            }
        }



        /// <summary>
        /// Sorts the items.
        /// </summary>
        /// <param name="folders">The full folder list</param>
        /// <param name="count">The list of item count</param>
        /// <param name="parentId">The current parent id</param>
        /// <param name="level">The current level in the structure</param>
        /// <returns>The structure</returns>
        private MediaStructure Sort(IEnumerable<MediaFolders> folders, IList<FolderCount> count, long? parentId = null, int level = 0)
        {
            var rootCount = count.FirstOrDefault(c => c.FolderId == null)?.Count;
            var totalCount = count.Sum(c => c.Count);
            var result = new MediaStructure
            {
                MediaCount = rootCount.HasValue ? rootCount.Value : 0,
                TotalCount = totalCount
            };

            var mediaFolders = folders as MediaFolders[] ?? folders.ToArray();
            foreach (var folder in mediaFolders.Where(f => f.ParentId == parentId)/*.OrderBy(f => f.Name)*/)
            {
                //var item = _mapper.Map<MediaFolders, MediaStructureItem>(folder);
                MediaStructureItem item = new MediaStructureItem()
                {
                    Id = folder.Id,
                    Order = folder.Order,
                    MediaFoldersDetails = folder.MediaFoldersDetails.ToList(),
                    Name = folder.MediaFoldersDetails.FirstOrDefault()?.Title,
                    ThumbnailId = folder.MediaFoldersDetails.FirstOrDefault()?.ThumbnailId,
                    ThumbnailMedia = folder.MediaFoldersDetails.FirstOrDefault()?.ThumbnailMedia,
                    Description = folder.MediaFoldersDetails.FirstOrDefault()?.Description,
                };

                var folderCount = count.FirstOrDefault(c => c.FolderId == folder.Id)?.Count;

                item.Level = level;
                item.Items = Sort(mediaFolders, count, folder.Id, level + 1);
                item.FolderCount = folders.Count(f => f.ParentId == item.Id);
                item.MediaCount = folderCount.HasValue ? folderCount.Value : 0;
                result.Add(item);
            }
            return result;
        }


        public MediaFolders GetFolderWithDetails(long folderId)
        {
            return _mediafoldersRepository.Query().Where(x => x.Id == folderId).Include(x => x.MediaFoldersDetails).FirstOrDefault();
        }

        public async Task SaveMediaDetails(MediaDetailsWithOrderVM model)
        {
            try
            {
                bool IsInEditMode = model.Id != 0;

                MediaDetails MediaDetails = new MediaDetails();

                if (IsInEditMode)
                {
                    MediaDetails = await _MediaDetailsRepository.Query()
                    .FirstOrDefaultAsync(m => m.Id == model.Id)
                    .ConfigureAwait(false);
                }


                MediaDetails.Title = model.Title;
                MediaDetails.AltText = model.AltText;
                MediaDetails.Description = model.Description;

                if (IsInEditMode)
                {
                    MediaDetails.ModifiedById = await _WorkContext.GetCurrentUserId();
                }
                else
                {
                    MediaDetails.CreatedById = await _WorkContext.GetCurrentUserId();
                    MediaDetails.CultureId = model.CultureId;
                    MediaDetails.MediaId = model.MediaId;
                    _MediaDetailsRepository.Add(MediaDetails);

                }

                var media = _mediaRepository.Query()
                                    .Where(m => m.Id == MediaDetails.MediaId)
                                    .FirstOrDefault();

                media.Order = model.Order;
                await _mediaRepository.SaveChangesAsync();

                // Save all changes
                await _MediaDetailsRepository.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<bool> DeleteMediaWithDetails(long MediaId)
        {
            using (var Transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var Media = await _mediaRepository.Query()
                  .Include(m => m.Versions)
                  .Include(m => m.MediaDetails)
                  .FirstOrDefaultAsync(m => m.Id == MediaId)
                  .ConfigureAwait(false);

                    if (Media != null && Media.MediaDetails != null)
                    {
                        _MediaDetailsRepository.RemoveRange(Media.MediaDetails);
                        await _MediaDetailsRepository.SaveChangesAsync();

                        _mediaRepository.Remove(Media);
                        await _mediaRepository.SaveChangesAsync();
                    }
                    Transaction.Commit();
                    return true;

                }
                catch (Exception e)
                {
                    Transaction.Rollback();
                    throw e;

                }

            }

        }
        public async Task<bool> updateThmbnail(MediaFolderVM mediaFolder)
        {
            try
            {
                var thumbnailFolder = await _mediafoldersDetailsRepository.Query().Where(x => x.FolderId == mediaFolder.Id).ToListAsync();
                foreach (var obj in thumbnailFolder)
                {
                    obj.ThumbnailId = mediaFolder.ThumbnailId;
                }
                _mediafoldersDetailsRepository.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private void MoveFileOrRename(string Source, string Target)
        {
         

            File.Move("wwwroot/" + Source, "wwwroot/" + Target);
        }
    }
}