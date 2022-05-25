
using System;
using System.Linq;
using System.Threading.Tasks;
//using Piranha.Models;
//using Piranha.Manager.Models;
using System.IO;
using System.Collections.Generic;
using System.Data;
using EskaCMS.Core.Services;
using EskaCMS.Core.Enums;
using EskaCMS.Core.Models;
using EskaCMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Media.Models;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.ViewModels;

namespace EskaCMS.Media.Services
{
    public class MediaService : Interfaces.IMediaService
    {
        //private readonly IApi _api;

        private readonly IMediaService _MediaService;
        private readonly IRepository<MediaFolders> _mediafoldersRepository;
        private readonly IRepository<MediaFoldersDetails> _mediafoldersdetailsRepository;
        private readonly IWorkContext _WorkContext;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="api">The default api</param>
        public MediaService(
            IMediaService MediaService,
            IRepository<MediaFolders> mediafoldersRepository,
            IRepository<MediaFoldersDetails> mediafoldersdetailsRepository,
            IWorkContext WorkContext

        )
        {
            _MediaService = MediaService;
            _mediafoldersRepository = mediafoldersRepository;
            _mediafoldersdetailsRepository = mediafoldersdetailsRepository;
            _WorkContext = WorkContext;
        }

        /// <summary>
        /// Get media model by media id
        /// </summary>
        /// <param name="id">Media id</param>
        /// <returns>Model</returns>
        public async Task<MediaListModel.MediaItem> GetById(long id)
        {
            var media = await _MediaService.GetByIdAsync(id);
            if (media == null)
                return null;

            return new Models.MediaListModel.MediaItem
            {
                Id = media.Id,
                FolderId = media.FolderId,
                Type = media.Type.ToString(),
                Filename = media.Filename,
                PublicUrl = media.PublicUrl.Replace("~", ""),
                ContentType = media.ContentType,
                Title = media.Title,
                AltText = media.AltText,
                Description = media.Description,
                Properties = media.Properties.ToArray().OrderBy(p => p.Key).ToList(),
                Size = FormatByteSize(media.Size),
                Order = media.Order,
                Width = media.Width,
                Height = media.Height,
                ModificationDate = media.ModificationDate
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string FormatByteSize(double bytes)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB" };

            int index = 0;
            if (bytes > 1023)
            {
                do
                {
                    bytes /= 1024;
                    index++;
                } while (bytes >= 1024 && index < 3);
            }

            return $"{bytes:0.00} {SizeSuffixes[index]}";
        }
        /// <summary>
        /// Gets the breadcrumb list of the folders from the selected folder id
        /// </summary>
        /// <param name="structure">The complete media folder structure</param>
        /// <param name="folderId">The folder id</param>
        /// <returns></returns>
        public async Task<List<MediaFolderSimple>> GetFolderBreadCrumb(MediaStructure structure, long? folderId)
        {
            var folders = await GetFolderBreadCrumbReversed(structure, folderId);
            folders.Reverse();
            return folders;
        }


        public async Task<List<MediaFolderSimple>> GetFolderBreadCrumb(long? FolderId, long? ParentFolderId) 
        { 
        var SiteId = await  _WorkContext.GetCurrentSiteIdAsync();
            var MediaStructure = new Models.MediaListModel
            {
                CurrentFolderId = FolderId,
                ParentFolderId = ParentFolderId,
                Structure = await _MediaService.GetStructureAsync(SiteId)

            };
           
                return await GetFolderBreadCrumb(MediaStructure.Structure, FolderId);



        
        }

/// <summary>
/// Gets the breadcrumb list of the folders from the selected folder id in reverse order with child first in list
/// </summary>
/// <param name="structure">The complete media folder structure</param>
/// <param name="folderId">The folder id</param>
/// <returns></returns>
private async Task<List<MediaFolderSimple>> GetFolderBreadCrumbReversed(MediaStructure structure, long? folderId)
        {
            var folders = new List<MediaFolderSimple>();
   
            if (!folderId.HasValue)
                return folders;

            foreach (var item in structure)
            {
                if (item.Id == folderId)
                {
                    folders.Add(new MediaFolderSimple() { Id = item.Id, Name = item.Name });
                    return folders;
                }

                if (item.Items.Count > 0)
                {
                    folders = await GetFolderBreadCrumbReversed(item.Items, folderId);
                    if (folders.Count > 0)
                    {
                        folders.Add(new MediaFolderSimple() { Id = item.Id, Name = item.Name });
                        return folders;
                    }
                }
            }
            return folders;
        }

        /// <summary>
        /// Gets the list model for the specified folder, or the root
        /// folder if no folder id is given.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <param name="filter">The optional content type filter</param>
        /// <param name="width">The optional width for images</param>
        /// <param name="height">The optional height for images</param>
        /// <returns>The list model</returns>
        public async Task<Models.MediaListModel> GetList(long siteId, long? folderId = null, MediaType? filter = null, int? width = null, int? height = null)
        {
            var CultureId = _WorkContext.GetCurrentCultureId();
            var model = new Models.MediaListModel
            {
                CurrentFolderId = folderId,
                ParentFolderId = null,
                Structure = await _MediaService.GetStructureAsync(siteId)

            };

            model.CurrentFolderBreadcrumb = await GetFolderBreadCrumb(model.Structure, folderId);

            model.RootCount = model.Structure.MediaCount;
            model.TotalCount = model.Structure.TotalCount;

            if (folderId.HasValue)
            {
                var partial = model.Structure.GetPartial(siteId, folderId, true);

                if (partial.FirstOrDefault()?.MediaCount == 0 && partial.FirstOrDefault()?.FolderCount == 0)
                {
                    model.CanDelete = true;
                }
            }

            if (folderId.HasValue)
            {
                var folder = await _MediaService.GetFolderByIdAsync(siteId, folderId.Value);
                if (folder != null)
                {
                    model.CurrentFolderName = folder.Title;
                    model.ParentFolderId = folder.ParentId;
                }
            }

            var holdMedia = (await _MediaService.GetAllByFolderIdAsync(siteId, folderId));
            if (filter.HasValue)
            {
                holdMedia = holdMedia
                    .Where(m => m.Type == filter.Value);
            }
            var pairMedia = holdMedia.Select(m => new
            {
                media = m,
                mediaItem = new MediaListModel.MediaItem
                {
                    Id = m.Id,
                    FolderId = m.FolderId,
                    Type = m.Type.ToString(),
                    Filename = m.Filename,
                    PublicUrl = m.PublicUrl.TrimStart('~'), //Will only enumerate the start of the string, probably a faster operation.
                    ContentType = m.ContentType,
                    Title = m.MediaDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Title,
                    AltText = m.MediaDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.AltText,
                    Description = m.MediaDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Description,
                    Properties = m.Properties.ToArray().OrderBy(p => p.Key).ToList(),
                    Size = FormatByteSize(m.Size),
                    Order = m.Order,
                    Width = m.Width,
                    Height = m.Height,
                    ModificationDate = m.ModificationDate,
                    CultureId = m.CultureId
                }
            }).ToArray();

            model.Folders = model.Structure.GetPartial(siteId, folderId)
                .Select(f => new MediaListModel.FolderItem
                {
                    Id = f.Id,
                    Name = f.MediaFoldersDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Title,
                    ItemCount = f.MediaCount,
                    Order=f.Order,
                    ThumbnailUrl = f.MediaFoldersDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Thumbnail?.PublicUrl
                }).ToList();

            //model.Folders = model.Folders.GroupBy(x => x.Code).Select(x => x.FirstOrDefault()).ToList();

            if (width.HasValue)
            {
                foreach (var mp in pairMedia.Where(m => m.media.Type == MediaType.Image))
                {
                    if (mp.media.Versions.Any(v => v.Width == width && v.Height == height))
                    {
                        mp.mediaItem.AltVersionUrl =
                            (await _MediaService.EnsureVersionAsync(mp.media, width.Value, height).ConfigureAwait(false))
                            .TrimStart('~');
                    }
                }
            }

            model.Media = pairMedia.Select(m => m.mediaItem).OrderByDescending(x=>x.Id).ToList();
            model.ViewMode = model.Media.Count(m => m.Type == "Image") > model.Media.Count / 2 ? MediaListModel.GalleryView : MediaListModel.ListView;

            return model;
        }

        //public async Task SaveFolder(MediaFolderVM model)
        //{
        //    var folderList = await _MediaService.GetByFolderNameAsync(model.Code, model.SiteId);
        //    if (folderList.Count() > 0)
        //    {
        //        var folder = folderList.Where(x => x.CultureId == model.CultureId).Select(x => new MediaFolderVM
        //        {
        //            Id = x.Id,
        //            ParentId = x.ParentId,
        //            Title = x.Title,
        //            Description = x.Description,
        //            Name = x.Name,
        //            CultureId = x.CultureId,
        //            Code = x.Code,
        //            CreatedById = x.CreatedById
        //        }).FirstOrDefault();

        //        if (folder == null)
        //        {
        //            folder = folderList.Select(x => new MediaFolderVM
        //            {
        //                Id = x.Id,
        //                ParentId = x.ParentId,
        //                Title = x.Title,
        //                Description = x.Description,
        //                Name = x.Name,
        //                CultureId = x.CultureId,
        //                Code = x.Code
        //            }).FirstOrDefault();
        //            folder.Code = model.Name;
        //            folder.CreatedById = model.CreatedById;
        //        }
        //        folder.Name = model.Name;
        //        folder.Title = model.Title;
        //        folder.CultureId = model.CultureId;
        //        folder.Description = model.Description;
        //        folder.SiteId = model.SiteId;
        //        folder.CreatedById = model.CreatedById;
        //        folder.ParentId = model.ParentId;
        //        await _MediaService.SaveFolderAsync(folder);
        //    }
        //    else
        //    {
        //        await _MediaService.SaveFolderAsync(model);
        //    }
        //}

        public async Task SaveFolder(MediaFolderVM model)

        {
            try
            {
                MediaFolders mediaFolder = new MediaFolders();
                MediaFoldersDetails mediaFoldersDetails = new MediaFoldersDetails();
                var folder = _MediaService.GetFolderWithDetails(model.Id);
                var IsCultureExist = false;
                if (folder != null)

                {
                    IsCultureExist = model.CultureId == (folder.MediaFoldersDetails.
                Where(x => x.FolderId == model.Id && x.CultureId == model.CultureId).Select(x => x.CultureId).FirstOrDefault());

                }

                if (folder == null)
                {
                    mediaFolder.ParentId = model.ParentId;
                    mediaFolder.SiteId = model.SiteId;
                    mediaFolder.Order = model.Order;

                    _mediafoldersRepository.Add(mediaFolder);
                    _mediafoldersRepository.SaveChanges();


                    mediaFoldersDetails.FolderId = mediaFolder.Id;
                    mediaFoldersDetails.ThumbnailId = model.ThumbnailId;
                    mediaFoldersDetails.Title = model.Title;
                    mediaFoldersDetails.Description = model.Description;
                    mediaFoldersDetails.CultureId = model.CultureId;
                    mediaFoldersDetails.CreatedById = model.CreatedById;
                    mediaFoldersDetails.CreationDate = DateTime.Now;
                    mediaFoldersDetails.ModifiedById = model.CreatedById;
                    _mediafoldersdetailsRepository.Add(mediaFoldersDetails);
                    _mediafoldersdetailsRepository.SaveChanges();

                }

                else if (!IsCultureExist)
                {
                    var Folder = _mediafoldersRepository.Query().Where(f=>f.Id == model.Id).FirstOrDefault();
                    Folder.Order = model.Order;
                    _mediafoldersRepository.SaveChanges();

                    mediaFoldersDetails.FolderId = model.Id;
                    mediaFoldersDetails.ThumbnailId = model.ThumbnailId.HasValue ? model.ThumbnailId : null;
                    mediaFoldersDetails.Title = model.Title;
                    mediaFoldersDetails.CultureId = model.CultureId;
                    mediaFoldersDetails.Description = model.Description;
                    mediaFoldersDetails.CreatedById = model.CreatedById;
                    mediaFoldersDetails.CreationDate = DateTime.Now;
                    mediaFoldersDetails.ModifiedById = model.CreatedById;
                    _mediafoldersdetailsRepository.Add(mediaFoldersDetails);
                    _mediafoldersdetailsRepository.SaveChanges();

                }
                else if (model.Id != null && IsCultureExist)
                {
                    var Folder = _mediafoldersRepository.Query().Where(f => f.Id == model.Id).FirstOrDefault();
                    Folder.Order = model.Order;
                    _mediafoldersRepository.SaveChanges();

                    var obj = (folder.MediaFoldersDetails.
                    Where(x => x.FolderId == model.Id && x.CultureId == model.CultureId).FirstOrDefault());



                    obj.FolderId = model.Id;
                    obj.ThumbnailId = model.ThumbnailId;
                    obj.Title = model.Title;
                    obj.CultureId = model.CultureId;
                    obj.Description = model.Description;
                    obj.CreatedById = model.CreatedById;
                    obj.CreationDate = DateTime.Now;
                    obj.ModifiedById = model.CreatedById;
                    _mediafoldersdetailsRepository.Update(obj);
                    _mediafoldersdetailsRepository.SaveChanges();

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }




        }
        public async Task<long?> DeleteFolder(long siteId, long id)
        {
            var folder = await _MediaService.GetFolderByIdAsync(siteId, id);

            if (folder != null)
            {
                folder.SiteId = siteId;
                await _MediaService.DeleteFolderAsync(folder);
                return folder.ParentId;
            }
            return null;
        }

        /// <summary>
        /// Save or update media assets to storage
        /// </summary>
        /// <param name="model">Upload model</param>
        /// <returns>The number of upload managed to be saved or updated</returns>
        public async Task<List<MediaVM>> SaveMedia(MediaUploadModel model)
        {
            try
            {
                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();
                var MediaStructure = new Models.MediaListModel
                {
                    CurrentFolderId = model.ParentId,
                    ParentFolderId = null,
                    Structure = await _MediaService.GetStructureAsync(SiteId)

                };

                var CurrentFolderBreadcrumb = await GetFolderBreadCrumb(MediaStructure.Structure, model.ParentId);


                var uploadedMediaIds = new List<MediaVM>();

                // Go through all of the uploaded files
                foreach (var upload in model.Uploads)
                {

                    string Breadcrumb = "";

                    foreach (var item in CurrentFolderBreadcrumb)
                    {
                        Breadcrumb += string.IsNullOrEmpty(Breadcrumb)? item.Name: ( "-" + item.Name);
                    }


                    if (upload.Length > 0 && !string.IsNullOrWhiteSpace(upload.ContentType))
                    {
                        using (var stream = upload.OpenReadStream())
                        {
                            var mediaIds = await _MediaService.SaveAsync(new StreamMediaContent
                            {
                                Id = model.Uploads.Count() == 1 ? model.Id : null,
                                FolderId = model.ParentId,
                                Filename = string.IsNullOrEmpty(Breadcrumb)? Path.GetFileName(upload.FileName): Breadcrumb +"-" + Path.GetFileName(upload.FileName) 
                                ,Data = stream,
                                CreatedById = model.CreatedBy,
                                SiteId = model.SiteId,
                                CultureId = model.CultureId,
                                Force=model.Force.HasValue?model.Force.Value:false
                                
                            });
                            
                            uploadedMediaIds.AddRange(mediaIds);
                        }
                    }
                }
                return uploadedMediaIds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Saves the updated meta information for the given media asset.
        /// </summary>
        /// <param name="media">The media asset</param>
        /// <returns>If the meta information was updated successful</returns>
        public async Task<bool> SaveMeta(MediaListModel.MediaItem media)
        {
            try
            {

                var mediaDetailsList = await _MediaService.GetMediaDetailsAsync(media.Id, media.CultureId);

                var model = mediaDetailsList.FirstOrDefault();

                if (model == null)
                {
                    model = new Core.ViewModels.MediaDetailsWithOrderVM();
                    model.CultureId = media.CultureId;
                    model.Id = 0;
                    model.MediaId = media.Id;
                }

                model.Title = media.Title;
                model.AltText = media.AltText;
                model.Description = media.Description;
                model.Order = media.Order;

                await _MediaService.SaveMediaDetailsAsync(model);

                return true;

            }
            catch (Exception E)
            {

                return false;
            }

        }

        public async Task<MediaDetailsWithOrderVM> GetMeta(long mediaId, long siteId, long cultureId = 0)
        {
            var mediaList = await _MediaService.GetMediaDetailsAsync(mediaId, cultureId);
            return mediaList.Where(y => y.CultureId == cultureId).FirstOrDefault();
        }
        public async Task<MediaFolderVM> GetFolderByCulture(string folderCode, long siteId, long cultureId)
        {
            var mediaFolderList = await _MediaService.GetByFolderNameAsync(folderCode, siteId);
            return mediaFolderList.Where(x => x.CultureId == cultureId).FirstOrDefault();
        }


        public async Task<MediaFolderDetailsWithOrderVM> GetFolderDetailsByCultureId(long CultureId, long FolderId, long siteId)
        {
            var result = await _MediaService.GetFoldersDetailsByCultureId(FolderId, CultureId, siteId);
            return result;
        }


        public async Task<long?> DeleteMedia(long id)
        {

            await _MediaService.DeleteMediaWithDetails(id);
            return id;

            return null;
        }

        public MediaFolders GetFolderByCulture(long folderId, long cultureId)
        {
            var folder = _mediafoldersRepository.Query().Include(x => x.MediaFoldersDetails)
                .Where(x => x.Id == folderId).FirstOrDefault();
            return folder;
        }


   
    }
}