using EskaCMS.Pages.Models;
using EskaCMS.Pages.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using EskaCMS.Core.Extensions;
using EskaCMS.Filters.ActionFilters;

namespace EskaCMS.APIs.Controllers
{

    [ApiController]
    [Route("api/Pages")]
   // [Authorize]

    public class PagesController : ControllerBase
    {

        private readonly IPages _pageServices;
        private readonly IWorkContext _workContext;

        public PagesController(IPages PagesService , IWorkContext workContext)
        {
            _pageServices = PagesService;
             _workContext = workContext;
        }

        //[HttpPost]
        //[Route("AddNewPage")]
        //public async Task<IActionResult> AddNewPage(PagesViewModel Page)
        //{
        //    try
        //    {
        //        Page.UserId = long.Parse(Request.Headers["UserId"]);
        //        Page.SiteId = long.Parse(Request.Headers["SiteId"].ToString());
        //        // Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());

        //        return Ok(await _pageServices.AddNewPage(Page));

        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}
        [HttpGet]
        [Route("GetRelatedSlugsInfo/{slug}")]
        public async Task<IActionResult> GetRelatedSlugsInfo(string slug)
        {
            try
            { 
                return Ok(await _pageServices.GetRelatedSlugsInfoBySlug(slug));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
         
        }

        [HttpGet]
        [Route("GetRelatedPages/{ParentId}")]
        public async Task<IActionResult> GetRelatedPages(long ParentId)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());

                return Ok(await _pageServices.GetRelatedPages(ParentId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }



        //[HttpPost]
        //[Route("EditPage/{PageId}")]
        //public async Task<IActionResult> EditPage(long PageId, PagesViewModel Page)
        //{
        //    try
        //    {
        //        Page.UserId = long.Parse(Request.Headers["UserId"]);
        //        Page.SiteId = long.Parse(Request.Headers["SiteId"].ToString());
        //        //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
        //        return Ok(await _pageServices.EditPage(Page));

        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }

        //}


        [HttpPost]
        [Route("AddEditPage")] 
        public async Task<IActionResult> AddEditPage(List<PagesViewModel> Pages)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();
                long SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
                return Ok(await _pageServices.AddEditPage(UserId, SiteId, Pages));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }


        [HttpPost]
        [Route("UpsertPageRoles")]
        public async Task<IActionResult> UpsertPageRoles(UpdatePageRolesVM VM)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                return Ok(await _pageServices.updatePageRoles(VM.ParentPageId, VM.PageRolesList, UserId, SiteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpGet]
        [Route("GetIPAddress")]
        public void GetIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                string ipAddress = string.Empty;
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress=ip.ToString();
                    }
                }
            }
            catch(Exception ex)
            {
             string messsage=ex.Message;
            }
        }

    

        //Bulk update and delete
        [HttpPost]
        [Route("UpdatePagesStatus/{PagesIds}/{Status}")]
        public async Task<IActionResult> UpdatePagesStatus(string PagesIds, int Status)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
                return Ok(await _pageServices.UpdatePagesStatus(PagesIds, Status, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("DeletePages/{PagesIds}")]
        public async Task<IActionResult> DeletePages(string PagesIds)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
                return Ok(await _pageServices.DeletePages(PagesIds, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        //End bulk update and delete



        
        //[HttpPost]
        //[Route("DuplicatePage/{ParentId}")]
        //public async Task<IActionResult> DuplicatePage(long ParentId)
        //{
        //    try
        //    {
        //        var UserId = long.Parse(Request.Headers["UserId"]);
        //        var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
        //        return Ok(await _pageServices.DublicatePage(ParentId, SiteId, UserId));

        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}


        [HttpPost]
        [Route("SavePageSettings")]
        public async Task<IActionResult> SavePageSettings(SavePageSettingsViewModel SavePageSettingsModel)
        {
            try
            {
                SavePageSettingsModel.UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());

                return Ok(await _pageServices.SavePageSettings(SavePageSettingsModel, SiteId));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("GetPageById/{id?}")]
        public async Task<IActionResult> GetPageById(int id)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetPageByIdAsync(id, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("ChangePageAndTemplatesVersions")]
        public IActionResult ChangePageAndTemplatesVersions()
        {
            try
            {
                _pageServices.ChangePageAndTemplatesVersions();
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Authorize]
        [Route("GetAllPages/{PageName?}/{Status?}/{PageCategory?}/{CultureId?}/{CultureCode?}")]
        public async Task<IActionResult> GetAllPages(string PageName = null, EStatus? Status = null, long? PageCategory = null,long CultureId=0,string CultureCode =null)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetAllPages(PageName, Status, PageCategory, SiteId, CultureId , CultureCode));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetAllPagesFlat/{PageName?}/{PageCategory?}/{Status?}/{cultureId?}")]
     public async Task<IActionResult> GetAllPagesFlat(string PageName = null, long? PageCategory = null, EStatus? Status=null, long cultureId = 0)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetAllPagesFlat(PageName, PageCategory, Status, SiteId, cultureId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }

        }

        [HttpGet]
        [Authorize]
        [Route("GetPageCultures/{pageId}/{ParentPageId}")]
        public async Task<IActionResult> GetPageCultures(long ParentPageId )
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetPageCultures(ParentPageId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Authorize]
        [Route("ClonePages/SourceParentId/TargetPageId")]
        public async Task<IActionResult> ClonePages(long SourceParentId,long TargetPageId)
        {
            try
            {

                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                return Ok(await _pageServices.ClonePages(TargetPageId, SourceParentId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetPageSettings/{pageId}")]
        public async Task<IActionResult> GetPageSettingsAsync(long pageId)
        {
            try
            {
                return Ok(await _pageServices.GetPageSettings(pageId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("UpdatePageVersion/{pageId}/{versionId}")]
        public IActionResult UpdatePageVersion(long pageId, long versionId)
        {
            try
            {
                _pageServices.UpdatePageVersion(pageId, versionId);
                return Ok(true);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetAllPageVersions/{pageId}")]
        public async Task<IActionResult> GetAllPageVersions(long pageId)
        {
            try
            {
                return Ok(await _pageServices.GetAllPageVersions(pageId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        //[HttpGet]
        //[Route("GetPageVersion/{versionId}")]
        //public async Task<IActionResult> GetPageVersion(long versionId)
        //{
        //    try
        //    {
        //         return Ok(await _pageServices.GetPageVersion(versionId));
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}

        [HttpGet]
        [Route("DeletePageByID/{PageId}")]
        public async Task<IActionResult> DeletePageByID(long PageId)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                return Ok(await _pageServices.DeletePageByID(PageId, UserId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }



        }


      

    }
}