
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Core;
using EskaCMS.Core.Enums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models; 
using EskaCMS.Media.Models;
using EskaCMS.Media.Services;
using EskaCMS.Media.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.Media.Controllers
{
    /// <summary>
    /// Api controller for alias management.
    /// </summary>

    [Route("api/media")]
    // [Authorize]
    [ApiController]
    public class MediaApiController : Controller
    {
        private readonly IMediaService _service;
        private readonly Core.Services.IMediaService _IMediaService;
        private readonly IWorkContext _workContext;

        public MediaApiController(IMediaService service, Core.Services.IMediaService IMediaService, IWorkContext workContext)
        {
            _service = service;
            _IMediaService = IMediaService;
            _workContext = workContext;
        }

        /// <summary>
        /// Gets single media
        /// </summary>
        /// <returns>The list model</returns>
        [Route("get-media/{id}")]
        [HttpGet]
        public async Task<IActionResult> Get(long id)
        {
            var media = await _service.GetById(id);
            if (media == null)
            {
                return NotFound();
            }

            return Ok(media);
        }

        /// <summary>
        /// Gets single media
        /// </summary>
        /// <returns>The list model</returns>
        [Route("get-folder/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetFolderById(long id)
        {
            long siteId = Convert.ToInt64(Request.Headers["SiteId"]);

            var folder = await _IMediaService.GetFolderByIdAsync(siteId, id);
            if (folder == null)
            {
                return NotFound();
            }

            return Ok(folder);
        }

        /// <summary>
        /// Gets the image url for the specified dimensions.
        /// </summary>
        /// <param name="id">The unqie id</param>
        /// <param name="width">The optional width</param>
        /// <param name="height">The optional height</param>
        /// <returns>The public url</returns>
        [Route("url/{id}/{width?}/{height?}")]
        [HttpGet]
        public async Task<IActionResult> GetUrl(long id, int? width = null, int? height = null)
        {
            if (!width.HasValue)
            {
                var media = await _IMediaService.GetByIdAsync(id);

                if (media != null)
                {
                    return Redirect(media.PublicUrl);
                }
                return NotFound();
            }
            else
            {
                return Redirect(await _IMediaService.EnsureVersionAsync(id, width.Value, height));
            }
        }

        /// <summary>
        /// Gets the list model.
        /// </summary>
        /// <returns>The list model</returns>
        [Route("list")]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] long? folderId = null, MediaType? filter = null, [FromQuery] int? width = null, [FromQuery] int? height = null)
        {
            try
            {
                long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _service.GetList(SiteId, folderId, filter, width, height));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Saves the meta information for the given media asset.
        /// </summary>
        /// <param name="model">The media model</param>
        [Route("meta/save")]
        [HttpPost]
        public async Task<IActionResult> SaveMeta(MediaListModel.MediaItem model)
        {
            model.SiteId = Convert.ToInt64(Request.Headers["SiteId"]);

            if (await _service.SaveMeta(model))
            {
                return Ok("The meta information was succesfully updated");
            }
            else
            {
                return Ok("The meta information was succesfully updated");
            }
        }

        [Route("meta/{cultureId}/{mediaId}")]
        [HttpGet]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GetMediaDetailsbyCultureId(long cultureId, long mediaId)
        {
            long siteId = Convert.ToInt64(Request.Headers["SiteId"]);

            if (mediaId!=0)
            {
                var result = await _service.GetMeta(mediaId, siteId, cultureId);
                if (result != null)
                    return Ok(result);
                else
                    return Ok(new Core.Entities.Media());
            }
            else
            {
                return BadRequest("File Name Should be sent");
            }
        }

        [Route("GetFolderDetailsbyCultureId/{cultureId}/{FolderId}")]
        [HttpGet]
        public async Task<IActionResult> GetFolderDetailsbyCultureId(long CultureId, long FolderId)
        {
            long siteId = Convert.ToInt64(Request.Headers["SiteId"]);

            if (FolderId !=0 )
            {
                var result = await _service.GetFolderDetailsByCultureId(CultureId, FolderId, siteId);
                if (result != null)
                    return Ok(result);
                else
                    return Ok(new Core.Entities.MediaFoldersDetails());
            }
            else
            {
                return BadRequest("Folder name should be sent");
            }
        }

        [Route("folder/save")]
        [HttpPost]
        //[Authorize]
        public async Task<IActionResult> SaveFolder(MediaFolderVM model, MediaType? filter = null)
        {
            try
            {
                model.CreatedById = await _workContext.GetCurrentUserId();
                model.SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                await _service.SaveFolder(model);

                var result = await _service.GetList(model.SiteId, model.ParentId, filter);

                result.Status = new StatusMessage
                {
                    Type = StatusMessage.Success,
                    Body = String.Format("The folder {0} was saved", model.Title)
                };

                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Route("folder/delete/{id}")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DeleteFolder(long id)
        {
            try
            {
                long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var folderId = await _service.DeleteFolder(SiteId, id);

                var result = await _service.GetList(SiteId, folderId);

                result.Status = new StatusMessage
                {
                    Type = StatusMessage.Success,
                    Body = "The folder was successfully deleted"
                };

                return Ok(result);
            }
            catch (ValidationException e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Adds a new media upload.
        /// </summary>
        /// <param name="model">The upload model</param>
        [Route("upload")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] MediaUploadModel model)
        {

            var siteId =  _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId(); ;

            // Allow for dropzone uploads
            if (!model.Uploads.Any())
            {
                model.Uploads = HttpContext.Request.Form.Files;
            }

            try
            {
                if (model.SiteId == 0)
                    model.SiteId = siteId;

                if (model.CreatedBy == 0)
                    model.CreatedBy = userId;


                var uploaded = await _service.SaveMedia(model);

               // if (uploaded != null)
                    return Ok(new { uploaded });
                #region Comments
                //  throw new Exception("Duplicate entry");


                //if (uploaded.Count == model.Uploads.Count())
                //{
                //    return Ok("Uploaded all media assets");
                //}
                //else if (uploaded.Count == 0)
                //{
                //    return Ok("Could not upload the media assets");
                //}
                //else
                //{
                //    return Ok(String.Format("Uploaded {0} of {1} media assets", uploaded, model.Uploads.Count()));
                //}
                #endregion

            }
            catch (Exception e)
            {
                if (e.Message != null && e.Message.Contains("Duplicate File Name"))
                {
                    return Conflict("This Record Cannot Be Duplicated Please Try To Change The Name Before Upload");
                }
                return BadRequest(e.Message);
            }
        }

        [Route("move/{folderId?}")]
        [HttpPost]
        [Consumes("application/json")]
        [Authorize]
        public async Task<IActionResult> Move([FromBody] IEnumerable<long> items, long? folderId,[FromQuery] bool Force=false)
        {
            try
            {
                var moved = 0;
                long SiteId = Convert.ToInt64(Request.Headers["SiteId"]);

                foreach (long id in items)
                {
                    
                    
                    var media = await _IMediaService.GetByIdAsync(id);

                    var OldBreadCrumb = await  _service.GetFolderBreadCrumb(media.FolderId,null);

                   
                    var NewBreadCrumb = await  _service.GetFolderBreadCrumb(folderId,null);

                    // var BreadCrumb = OldBreadCrumb.Select(x => x.Name).ToArray().Concat(NewBreadCrumb.Select(x => x.Name).ToArray()).ToArray();


                    if (media != null)
                    {

                        var NewUrl = string.Join("-", NewBreadCrumb.Select(b=>b.Name).ToArray());
                        var OldUrl = string.Join("-", OldBreadCrumb.Select(b=>b.Name).ToArray());

                        await _IMediaService.MoveAsync(media, folderId,OldUrl , NewUrl,Force);
                        moved++;

                        continue;
                    }

                    var folder = await _IMediaService.GetFolderByIdAsync(SiteId, id);
                    if (folder != null)
                    {
                        if (folderId.HasValue && folderId == folder.Id)
                            continue;

                        folder.ParentId = folderId;
                        await _IMediaService.SaveFolderAsync(folder);
                        moved++;
                    }
                }


                if (moved > 0)
                {
                    return Ok($"Media file{(moved > 1 ? "s" : "")} was successfully moved.");
                }
                else
                {
                    return BadRequest("The media file was not found.");
                }
            }
            catch (Exception e)
            {
                if (e.Message != null && e.Message.Contains("Duplicate File Name"))
                {
                    return Conflict("This record cannot be duplicated Please Change The Name Before Move");
                }
                return BadRequest(e.Message);
            }
        }

        [Route("delete")]
        [HttpPost]
        [Consumes("application/json")]
        [Authorize]
        public async Task<IActionResult> Delete([FromBody] IEnumerable<long> items)
        {
            try
            {
                foreach (var id in items)
                {
                    await _service.DeleteMedia(id);
                }

                return Ok(new StatusMessage
                {
                    Type = StatusMessage.Success,
                    Body = $"The media file{(items.Count() > 1 ? "s" : "")} was successfully deleted"
                });
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException.Message.Contains("foreign key constraint fails"))
                {
                    return BadRequest("File is already being used and cannot be deleted");
                }
                return BadRequest(e.Message);
            }
        }
      
        [Route("setAsFolderThumbnail")]
        [HttpPut]
        public async Task<IActionResult> setAsFolderThumbnail(MediaFolderVM media)
        {
            try
            {
                StatusMessage statusMessage = new StatusMessage();
                var defultThumbnail = await _IMediaService.setThumbnailFolder(media);
                if (defultThumbnail == true)
                {
                    statusMessage.Body = "updated successfuy";
                    statusMessage.Type = StatusMessage.Success;
                }

                return Ok(statusMessage);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);

            }

        }
    }
}