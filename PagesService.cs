using EskaCMS.Core.Data;
using EskaCMS.Core.Entities;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Pages.Entities;
using EskaCMS.Pages.Models;
using EskaCMS.Pages.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using static EskaCMS.Core.Entities.BusinessModels.BusinessModel;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.Pages.Services
{
    public class PagesService : IPages
    {
        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<PageVersions> _pageVersions;
        private readonly IRepository<PageTemplates> _pageTemplatesRepository;
        private readonly IRepository<TemplateVersions> _templateVersionsRepository;
        private readonly IRepository<PagesRoles> _PagesRolesRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository;
        private readonly ICategories _CategoriesService;
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<TemplateImportedFiles> _TemplateImportedFilesRepository;
        private readonly IRepository<Site> _SitesRepository;
        private readonly IWorkContext _WorkContext;
        private readonly IRepository<SitesHostNames> _SitesHostNamesRepo;
        public PagesService(
            IRepository<TemplateVersions> templateVersionsRepository,
            IRepository<Page> pageRepository,
            IRepository<PageVersions> pageVersions,
            IRepository<PageTemplates> pageTemplates,
            IRepository<PagesRoles> PagesRolesRepository,
          IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository,
          IRepository<TemplateImportedFiles> TemplateImportedFilesRepository,
          IRepository<Site> SitesRepository,
        EskaDCMSDbContext context,
        IRepository<SitesHostNames> SitesHostNamesRepo,
          ICategories CategoriesService,
          IWorkContext WorkContext
          )
        {
            _pageRepository = pageRepository;
            _pageVersions = pageVersions;
            _pageTemplatesRepository = pageTemplates;
            _templateVersionsRepository = templateVersionsRepository;
            _CategoriesService = CategoriesService;
            _siteSettingsRepository = siteSettingsRepository;
            _context = context;
            _PagesRolesRepository = PagesRolesRepository;
            _TemplateImportedFilesRepository = TemplateImportedFilesRepository;
            _SitesRepository = SitesRepository;
            _WorkContext = WorkContext;
            _SitesHostNamesRepo = SitesHostNamesRepo;   
        }

        public async Task<List<Page>> GetRelatedPages(long ParentId, long SiteId)
        {

            return await _pageRepository.Query()
                .Where(p => p.ParentId == ParentId && p.SiteId == SiteId)
                  .Include(p => p.CurrentVersion)
                  .Include(p => p.Culture)
                  .Include(p => p.PageCategory)
                  .Include(p => p.PageRoles)
                  .Include(p=>p.Thumbnail)
                  .ToListAsync();
        }

        public async Task<List<PageCultureDetailsVM>> GetPageCultures(long ParentPageId, long SiteId)
        {
            var RealatedPages = await GetRelatedPages(ParentPageId, SiteId);
            var Result = RealatedPages.Select(c => new PageCultureDetailsVM()
            {
                PageCultureStatus = c.Status,
                PageId = c.Id,
                ParentPageId = c.ParentId,
                CultureCode = c.Culture.CultureId,
                CultureId = c.CultureId


            }).ToList();

            return Result;
        }

        public async Task<List<PageIdAndCultureIdVM>> AddEditPage(long UserId, long SiteId, List<PagesViewModel> Pages)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    List<PageIdAndCultureIdVM> result = new List<PageIdAndCultureIdVM>();
                    Pages = Pages.OrderByDescending(p => p.Id).ToList();
                    bool IsEditMode = Pages[0].Id != 0;

                    if (!IsEditMode)
                    {
                        Pages = Pages.OrderByDescending(p => p.Name).ToList();
                    }
                    else
                    {
                        Pages = Pages.OrderByDescending(p => (int)p.Status).ToList();
                    }

                    long ParentId = 0;


                    foreach (var Page in Pages)
                    {
                        //testing data need to be deleted
                        //Page.RolesIds = new List<long>();
                        //Page.RolesIds.Add(1);
                        //Page.RolesIds.Add(4);


                        if (Page.Status == EStatus.Unpublished && Page.Id ==0)
                        {
                            result.Add(new PageIdAndCultureIdVM()
                            {
                                CultureId = Page.CultureId,
                                PageId = Page.Id
                            });
                            continue;
                        }


                        var ObjPage = Page.Id == 0
                                ? new Entities.Page()
                                : await _pageRepository.Query().Where(x => x.Id == Page.Id).Include(x => x.PageRoles).FirstOrDefaultAsync();


                        if (Page.PageCategoryId == 0)
                        {
                            var newCat = new CategoryViewModel();
                            newCat.Name = Page.PageCategoryName;
                            newCat.UserId = UserId;
                            newCat.Slug = Page.PageCategoryName + "-" + Page.PageCategoryId;
                            newCat.SiteId = Page.SiteId;
                            newCat.CategoryTypeId = ECategoryTypes.Page;
                            Page.PageCategoryId = await _CategoriesService.AddCategory(newCat);
                        }



                        ObjPage.Name = string.IsNullOrEmpty(Page.Name) ? Pages[0].Name : Page.Name;
                        ObjPage.Slug = string.IsNullOrEmpty(Page.Slug) ? Pages[0].Slug : Page.Slug;
                 
                        ObjPage.Status = Page.Status;
                        ObjPage.StatusDate = DateTimeOffset.Now;
                        ObjPage.NavigationTitle = Page.Slug;
                        ObjPage.PageCategoryId = Page.PageCategoryId;
                        ObjPage.SiteId = SiteId;
                        ObjPage.Route = Page.Route;
                        ObjPage.Description = Page.Description;

                        ObjPage.CultureId = Page.CultureId;

                        ObjPage.Keywords = Page.Keywords;
                        ObjPage.AddtionalMetaTags = Page.AddtionalMetaTags;
                        ObjPage.AdditionalContent = Page.AdditionalContent;
                        ObjPage.IsLoginRequired = Page.IsLoginRequired;
                        ObjPage.ThumbnailId = Page.ThumbnailId;
                        ObjPage.PageTemplateId = Page.PageTemplateId > 0 ? Page.PageTemplateId : null;
                      
                        if (ObjPage.Id == 0 )
                        {
                            ObjPage.CreatedById = UserId;
                            ObjPage.CreationDate = DateTimeOffset.Now;
                            _pageRepository.Add(ObjPage);
                            ObjPage.CurrentVersion = new PageVersions
                            {
                                CreatedById = ObjPage.CreatedById,
                                CreationDate = DateTimeOffset.Now,
                                SettingsBody = "{}",
                                VersionId = 1,
                                
                            };
                            await _pageRepository.SaveChangesAsync();
                            ObjPage.CurrentVersion.PageId = ObjPage.Id;
                        }
                        else
                        {
                            ObjPage.ModificationDate = DateTimeOffset.Now;
                            ObjPage.ModifiedById = UserId;
                            await _pageRepository.SaveChangesAsync();

                        }

                        if (Page.DuplicatedOfId !=0) // incase of duplicate page
                        {
                            var OrigenalPageVersion = await _pageRepository
                                                       .Query()
                                                       .Where(p => p.ParentId == Page.DuplicatedOfId &&
                                                              p.CultureId == Page.CultureId)
                                                       .Include(p=>p.CurrentVersion )
                                                       .Select(p=>p.CurrentVersion)
                                                       .FirstOrDefaultAsync();

                            ObjPage.CurrentVersion.PageComponents = OrigenalPageVersion.PageComponents;
                            ObjPage.CurrentVersion.SettingsBody = OrigenalPageVersion.SettingsBody;

                            await _pageRepository.SaveChangesAsync();

                        }

                        ObjPage.ParentId = ParentId == 0 ? ObjPage.Id : ParentId;
                        ParentId = ParentId == 0 ? ObjPage.Id : ParentId;
                        result.Add(new PageIdAndCultureIdVM()
                        {
                            CultureId = ObjPage.CultureId,
                            PageId = ObjPage.Id
                        });
                        // await UpsertPageRolesAsync(ObjPage, Page.RolesIds, UserId);
                        await _pageRepository.SaveChangesAsync();

                    }
                    await _pageRepository.SaveChangesAsync();


                    //if there is no page added before set the first page as defult just for the first time
                    SetDefaultParentPageIdForSite(SiteId, ParentId);

                    transaction.Commit();
                    return result;
                }
                catch (Exception exc)
                {
                    transaction.Rollback();
                    if (exc.InnerException?.Message.Contains("Duplicate entry") == true)
                    {
                        throw new Exception("Slug is already used");
                    }
                    throw exc;

                }

            }
        }

        public bool SetDefaultParentPageIdForSite(long siteId,long parentId)
        {
            try
            {
              var site=_SitesRepository.Query().Where(x => x.Id == siteId).FirstOrDefault();
                if (site.DefaultParentPageId == 0)
                {
                    site.DefaultParentPageId = parentId;
                    _SitesRepository.SaveChanges();
                }

                return true;

            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> UpdatePagesStatus(string PagesIds, int Status, long UserId, long SiteId)
        {
            try
            {
                int[] ids = PagesIds.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
                var pageList = await _pageRepository.Query().Where(x => x.Status != EStatus.Deleted
                        && x.SiteId == SiteId).Select(x => x).ToListAsync();

                for (int i = 0; i < pageList.Count(); i++)
                {
                    for (int j = 0; j < ids.Count(); j++)
                    {
                        if (ids[j] == pageList[i].Id)
                        {
                            pageList[i].Status = (EStatus)Status;
                            pageList[i].ModifiedById = UserId;
                            if (Status == (long)EStatus.Deleted)
                            {
                                pageList[i].Slug = pageList[i].Id + "Deleted" + pageList[i].Slug;
                            }
                            else 
                            {
                                pageList[i].Slug = pageList[i].Slug.Split("Deleted").LastOrDefault();
                            }
                            await _pageRepository.SaveChangesAsync();
                        }
                    }
                }
                    return true;
                
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public async Task<bool> DeletePages(string PagesIds, long UserId, long SiteId)
        {
            try
            {
                int[] ids = PagesIds.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
                var pageList = await _pageRepository.Query().Where(x => x.Status != EStatus.Deleted
                        && x.SiteId == SiteId).Select(x => x).ToListAsync();

                for (int i = 0; i < pageList.Count(); i++)
                {
                    for (int j = 0; j < ids.Count(); j++)
                    {
                        if (ids[j] == pageList[i].Id)
                        {
                            pageList[i].Status = EStatus.Deleted;
                            pageList[i].ModifiedById = UserId;
                            pageList[i].Slug = pageList[i].Id + "Deleted" + pageList[i].Slug;

                            await _pageRepository.SaveChangesAsync();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        //End of bulk update and delete
        public async Task<List<PageResultVM>> DublicatePage(long ParentId, long SiteId, long UserId)
        {

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var originalPages = await GetRelatedPages(ParentId, SiteId);
                    var DublicatePages = new List<Page>();
                    long DublicateParentId = 0;

                    foreach (var Page in originalPages)
                    {
                        var ObjPage = new Page();
                        ObjPage.Name = Page.Name + " (Duplicated)";
                        ObjPage.Slug = Page.Slug + "-duplicated " + Guid.NewGuid().ToString().Substring(1, 2);
                        ObjPage.CreatedById = UserId;
                        ObjPage.CreationDate = DateTimeOffset.Now;
                        ObjPage.Status = Page.Status;
                        ObjPage.StatusDate = DateTimeOffset.Now;
                        ObjPage.NavigationTitle = Page.Slug + "-duplicated";
                        ObjPage.PageCategoryId = Page.PageCategoryId;
                        ObjPage.SiteId = SiteId;
                        ObjPage.Route = Page.Route;
                        ObjPage.Description = Page.Description;
                        ObjPage.Keywords = Page.Keywords;
                        ObjPage.AddtionalMetaTags = Page.AddtionalMetaTags;
                        ObjPage.ThumbnailId = Page.ThumbnailId;
                        ObjPage.AdditionalContent = Page.AdditionalContent;
                        ObjPage.CultureId = Page.CultureId;
                        ObjPage.PageTemplateId = Page.PageTemplateId > 0 ? Page.PageTemplateId : null;
                        ObjPage.PageCategory = Page.PageCategory;

                        _pageRepository.Add(ObjPage);


                        var pageContent = Page.CurrentVersion;
                        var Settings = pageContent != null ? pageContent.SettingsBody : "{}";
                        var PageComponents = pageContent != null ? pageContent.PageComponents : "";

                        ObjPage.CurrentVersion = new PageVersions
                        {
                            CreatedById = UserId,
                            CreationDate = DateTime.Now,
                            SettingsBody = Settings,
                            VersionId = 1,
                            PageComponents = PageComponents
                        };

                        _pageRepository.SaveChanges();


                        List<long> PageRolesIdList = Page.PageRoles.Select(r => r.RoleId).ToList();
                        await UpsertPageRolesAsync(ObjPage, PageRolesIdList, UserId);
                        ObjPage.CurrentVersion.PageId = ObjPage.Id;
                        ObjPage.ParentId = DublicateParentId == 0 ? ObjPage.Id : DublicateParentId;
                        DublicateParentId = DublicateParentId == 0 ? ObjPage.Id : DublicateParentId;
                        ObjPage.Slug = Page.Slug + "-duplicated " + ObjPage.Id;
                        DublicatePages.Add(ObjPage);
                    }

                    await _pageRepository.SaveChangesAsync();
                    transaction.Commit();
                    return BuildPagesTree(DublicatePages);
                }
                catch (Exception exc)
                {
                    transaction.Rollback();
                    throw exc;
                }
            }
        }
        public void UpdatePageVersion(long? pageId, long versionId)
        {
            Page page = _pageRepository.Query().FirstOrDefault(v => v.Id == pageId);
            page.CurrentVersionId = versionId;
            _pageRepository.SaveChanges();
        }

        public async Task<IList<PageVersionVM>> SavePageSettings(SavePageSettingsViewModel SavePageSettingsModel, long siteId)
        {
            try
            {

                using (var transaction = _pageRepository.BeginTransaction())
                {
                    var ObjPageVersions = new PageVersions();
                    ObjPageVersions.PageId = SavePageSettingsModel.PageId;
                    ObjPageVersions.VersionId = GetLatestVersionId(SavePageSettingsModel.PageId) + 1;
                    ObjPageVersions.SettingsBody = SavePageSettingsModel.Settings;
                    ObjPageVersions.CreatedById = SavePageSettingsModel.UserId;
                    ObjPageVersions.CreationDate = DateTimeOffset.Now;

                    if (SavePageSettingsModel.PageComponents == null)
                    {
                        SavePageSettingsModel.PageComponents = new List<string>();
                    }

                    ObjPageVersions.PageComponents = JsonConvert.SerializeObject(SavePageSettingsModel.PageComponents);

                    _pageVersions.Add(ObjPageVersions);
                    await _pageVersions.SaveChangesAsync();
                    UpdatePageVersion(ObjPageVersions.PageId, ObjPageVersions.Id);

                    var ObjPage = await _pageRepository.Query().Where(x => x.Id == SavePageSettingsModel.PageId).Include(x => x.PageVersions).FirstOrDefaultAsync();
                    long latestVersionId = ObjPage.PageVersions.Max(x => x.VersionId);

                    ObjPage.ModifiedById = SavePageSettingsModel.UserId;
                    ObjPage.ModificationDate = DateTimeOffset.Now;

                    //PageVersions newVersion = new PageVersions
                    //{
                    //    CreatedById = ObjPage.CreatedById,
                    //    CreationDate = DateTime.Now,
                    //    PageId = ObjPage.Id,
                    //    SettingsBody = SavePageSettingsModel.Settings,
                    //    VersionId = latestVersionId + 1
                    //};
                    //await _pageVersions.SaveChanges();



                    string tempMaxPageVersionCount = GetSiteSettingByKey("MaxPageVersionCount", siteId);
                    long MaxPageVersionCount = 50;

                    if (tempMaxPageVersionCount != "")
                    {
                        MaxPageVersionCount = long.Parse(tempMaxPageVersionCount);
                    }

                    //var allOldVersions = _pageVersions.Query().Where(x => x.PageId == SavePageSettingsModel.PageId && x.VersionId < ObjPageVersions.VersionId - MaxPageVersionCount).ToList();

                    //foreach (var item in allOldVersions)
                    //{
                    //    _pageVersions.Remove(item);
                    //}
                    var allOldVersions = _pageVersions.Query().Where(x => x.PageId == SavePageSettingsModel.PageId).OrderBy(v => v.CreationDate).ToList();

                    if (allOldVersions.Count >= MaxPageVersionCount)
                    {
                        var diff = allOldVersions.Count - MaxPageVersionCount;
                        for (int i = 0; i < diff; i++)
                        {
                            _pageVersions.Remove(allOldVersions[i]);

                        }
                    }
                    await _pageVersions.SaveChangesAsync();
                    await _pageRepository.SaveChangesAsync();

                    var Versions = await GetAllPageVersions(ObjPage.Id);

                    transaction.Commit();
                    return Versions;
                }
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }



        public async Task<IList<PageResultVM>> GetAllPages(string PageName = null, EStatus? Status = null , long? PageCategory = null, long siteId = 0, long CultureId = 0, string CultureCode = null)
        {
            try
            {


             
                var pageList = await _pageRepository.Query()
                    .Include(y => y.PageTemplate)
                    .Include(x => x.PageCategory)
                    .Include(x => x.Culture)
                    .Include(x => x.Site)
                    .Include(x => x.Thumbnail)
                    .Where(x => (
                    ((x.Status != EStatus.Deleted && x.Status != EStatus.Inactive && x.Status != EStatus.Unpublished && (!Status.HasValue || Status.Value == 0)) || ((x.Status == Status) && (Status.HasValue && Status.Value !=0) ))
                    && x.SiteId == siteId
                    && ((x.PageCategoryId == PageCategory) || PageCategory == null || PageCategory == 0)
                    && (string.IsNullOrEmpty(CultureCode) || x.Culture.CultureId == CultureCode)
                    && ((!string.IsNullOrEmpty(PageName) && x.Name.ToLower().Equals(PageName.ToLower())) || string.IsNullOrEmpty(PageName) || PageName.Equals("0"))
                    && (CultureId == 0 || x.CultureId == CultureId)
                    )).ToListAsync();


                 return BuildPagesTree(pageList);


            }
            catch (Exception exc)
            {
                throw exc;
            }

        }
        public async Task<IList<PageResultVM>> GetAllPagesFlat(string PageName = null, long? PageCategory = null, EStatus? Status=null, long siteId = 0, long cultureId = 0)
        {

            var pageList = await _pageRepository.Query()
                .Include(y => y.PageTemplate)
                .Include(x => x.PageCategory)
                .Include(x => x.Culture)
                .Include(x => x.Site)
                .Include(x => x.Thumbnail)
                 .Where(x => (
                 x.SiteId == siteId&&
                ((x.Status != EStatus.Deleted && x.Status != EStatus.Inactive && x.Status != EStatus.Unpublished && (!Status.HasValue || Status.Value == 0)) ||
                ((x.Status == Status) &&
                (Status.HasValue && Status.Value != 0)))
                && (cultureId == 0 || x.CultureId == cultureId)
                && ((x.PageCategoryId == PageCategory) || PageCategory == null || PageCategory == 0)
                && ((!string.IsNullOrEmpty(PageName) && x.Name.ToLower().Equals(PageName.ToLower())) || string.IsNullOrEmpty(PageName) || PageName.Equals("0"))))
                 .Select(page => new PageResultVM
                {

                    Name = page.Name,
                    PageStatus = page.Status,
                    PageCategoryId = page.PageCategoryId,
                    PageCategoryName = page.PageCategory.Name,
                    CreationDate = page.CreationDate,
                    ParentPageId = page.Id,
                    TemplateName = page.PageTemplate == null ? "No Template" : page.PageTemplate.Name,
                    TemplateId = page.PageTemplate == null ? 0 : page.PageTemplate.Id,
                    ParentPageSlug = page.Slug,
                    IsDefaultPage = page.Site == null ? false : page.Site.DefaultParentPageId == page.ParentId,

                }).ToListAsync();

            return pageList;


        }

        public async Task<IList<PageSiteMapResultVM>> GetAllPagesBysiteId(long siteId = 0, EStatus Status = EStatus.Active, long? PageCategory = null)
        {
            try
            {


                var pageList = await _pageRepository.Query()
                   .Include(x => x.Culture)
                   .Include(x => x.Site)
                   .Where(x => ((x.Status != EStatus.Deleted && Status == EStatus.Active) || (Status != EStatus.Active && x.Status == Status)) /*when status is active  will return not deleted pages*/
                   && x.SiteId == siteId).Select(x =>new PageSiteMapResultVM
                   {
                       ParentPageId=x.ParentId.Value,
                       CultureId=x.CultureId,
                       PageSlug=x.Slug,
                       Name=x.Name,
                       Culture=x.Culture.Culture.Name
                   }).ToListAsync();




                return pageList;


            }
            catch (Exception exc)
            {
                throw exc;
            }

        }
        public async  Task<List<PageSlugResultVM>> GetRelatedSlugsInfoBySlug(string slug)
        {
            try
            { var CurrentSiteId = await _WorkContext.GetCurrentSiteIdAsync();

                var TargetPage = await _pageRepository.Query()
                    .Where(x => x.Slug == slug && x.SiteId==CurrentSiteId && x.Status == EStatus.Published )
                    .Select(x=>x.ParentId)
                    .FirstOrDefaultAsync();

                   var PagesSlugs = await _pageRepository.Query()
                    .Where(x => 
                            x.ParentId == TargetPage &&
                            EStatus.Deleted != x.Status &&
                            x.Status == EStatus.Published 
                    )
                    .Select( x=>new PageSlugResultVM
                    {
                        CultureId = x.CultureId,
                        CultureName=x.Culture.Culture.Name,
                        Name =x.Name,
                        Slug=x.Slug
                      }
                    )
                  .ToListAsync();

                //Find HostName  By ClutureId

                var SiteHostNames   = await _SitesHostNamesRepo.Query()
                    .Where(h=>h.SiteId == CurrentSiteId  )
                    .Select(x => new SitesHostNamesResultVM
                    {
                     CultureId = x.CultureId,
                     Prefix = x.Prefix,
                     Hostname=x.HostName
                     
                    }).ToListAsync();
               
                
                foreach(var SlugItem in PagesSlugs)
                {
                    var HostObj = SiteHostNames.FirstOrDefault(h => h.CultureId == SlugItem.CultureId);
                    SlugItem.HostName = HostObj == null ? "" : HostObj.Hostname;
                    SlugItem.Prefix = HostObj == null ? "" : HostObj.Prefix;
                }
                
                return PagesSlugs;
 
            }
           
            catch (Exception E)
            {
                throw E;
            }
        

        }

        public async Task<Page> ClonePages(long TargetPageId, long SourcePageId, long SiteId)
        {
            try
            {
                var RealatedPages = await _pageRepository.Query()
                                .Where(p => p.SiteId == SiteId && (p.Id == TargetPageId || p.Id == SourcePageId))
                                .Include(p => p.CurrentVersion)
                                .ToListAsync();

                var SourcePage = RealatedPages.Where(p => p.Id == SourcePageId).FirstOrDefault();
                var TargetPage = RealatedPages.Where(p => p.Id == TargetPageId).FirstOrDefault();

                var ClonedVersion = new PageVersions()
                {
                    PageId = TargetPage.Id,
                    CreatedById = SourcePage.CurrentVersion.CreatedById,
                    CreationDate = DateTime.Now,
                    PageComponents = SourcePage.CurrentVersion.PageComponents,
                    SettingsBody = SourcePage.CurrentVersion.SettingsBody,


                };

                _pageVersions.Add(ClonedVersion);

                _pageVersions.SaveChanges();

                ClonedVersion.VersionId = ClonedVersion.Id;
                TargetPage.CurrentVersionId = ClonedVersion.Id;

                _pageVersions.SaveChanges();

                TargetPage.CurrentVersion = ClonedVersion;
                return TargetPage;

            }
            catch (Exception E)
            {
                throw E;
            }
        }

        public async Task<bool> updatePageRoles(long ParentId, List<long> RolesIdList, long SiteId, long UserId)
        {
            using (var Transaction = _context.Database.BeginTransaction())
                try
                {
                    var RelatedPages = await GetRelatedPages(ParentId, SiteId);
                    foreach (var Page in RelatedPages)
                    {
                        await UpsertPageRolesAsync(Page, RolesIdList, UserId);
                    }
                    await _pageRepository.SaveChangesAsync();
                    await _PagesRolesRepository.SaveChangesAsync();

                    Transaction.Commit();
                    return true;

                }
                catch (Exception E)
                {
                    await Transaction.RollbackAsync();

                    throw E;
                }
        }

        public async Task<PagesViewModel> GetPageSettings(long pageId)
        {
            try
            {
                PagesViewModel page = await _pageRepository.Query()
                    .Where(x => x.Id == pageId && x.Status != EStatus.Deleted)
                    .Include(x => x.CurrentVersion)
                    .ThenInclude(x => x.CreatedBy)
                    .Include(x => x.PageTemplate)
                    .ThenInclude(x => x.CurrentVersion)
                    .ThenInclude(x => x.ImportedFiles)
                    .Include("PageTemplate.CurrentVersion.CreatedBy")
                    .Include(x => x.Thumbnail)
                    .Include(x => x.Culture)
                    .Select(v => new PagesViewModel
                    {
                       
                        CultureId = v.Culture.Id,
                        ParentId = v.ParentId,
                        AdditionalContent = v.AdditionalContent,
                        AddtionalMetaTags = v.AddtionalMetaTags,
                        CurrentVersion = new PageVersionVM
                        {
                            CreatedById = v.CurrentVersion.CreatedById,
                            CreatedByName = v.CurrentVersion.CreatedBy.FullName,
                            CreationDate = v.CurrentVersion.CreationDate,
                            Id = v.CurrentVersion.Id,
                            SettingsBody = v.CurrentVersion.SettingsBody,
                            VersionId = v.CurrentVersion.VersionId,
                            PageComponents = v.CurrentVersion.PageComponents
                        },
                        CurrentVersionId = v.CurrentVersionId,
                        Description = v.Description,
                        Id = v.Id,
                        ThumbnailId = v.ThumbnailId,
                        ThumbnailURL = v.Thumbnail.PublicUrl,
                        IsLoginRequired = v.IsLoginRequired,
                        Keywords = v.Keywords,
                        Name = v.Name,
                        PageCategoryId = v.PageCategoryId,
                        PageCategoryName = v.PageCategory.Name,
                        PageTemplateId = v.PageTemplateId != null ? v.PageTemplateId.Value : 0,
                        PageTemplate = v.PageTemplate != null ? new PageTemplateVM
                        {
                            Description = v.PageTemplate.Description,
                            Name = v.PageTemplate.Name,
                            AddtionalMetaTags = v.PageTemplate.AddtionalMetaTags,
                            CurrentVersion = new TemplateVersionVM
                            {
                                CreatedById = v.PageTemplate.CurrentVersion.CreatedById,
                                CreatedByName = v.PageTemplate.CurrentVersion.CreatedBy.FullName,
                                CreationDate = v.PageTemplate.CurrentVersion.CreationDate,
                                Css = v.PageTemplate.CurrentVersion.Css,
                                ImportFilesList = v.PageTemplate.CurrentVersion.ImportedFiles
                                        .Select(i => new Core.ViewModels.ImportedFilesVM()
                                        {
                                            Id = i.Id,
                                            FileId = i.Media == null ? 0 : i.Media.Id,
                                            FileName = i.Media == null ? string.Empty : i.Media.Filename,
                                            FileUrl = i.Media == null ? string.Empty : i.Media.PublicUrl,
                                            SourceId = v.PageTemplate.CurrentVersionId
                                        })
                                        .ToList(),
                                Scripts = v.PageTemplate.CurrentVersion.Scripts,
                                SettingsBody = v.PageTemplate.CurrentVersion.SettingsBody,
                                VersionId = v.PageTemplate.CurrentVersion.VersionId,
                                TemplateComponents = v.PageTemplate.CurrentVersion.TemplateComponents
                            }
                        } : null,
                        Route = v.Route,
                        Slug = v.Slug,
                        SiteId = v.SiteId,
                        Status = v.Status,
                        StatusDate = v.StatusDate

                    })
                    .FirstOrDefaultAsync();

                return page;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<IList<PageVersionVM>> GetAllPageVersions(long pageId)
        {
            IList<PageVersionVM> pageVersions = await _pageVersions.Query()
                .Where(x => x.PageId == pageId)
                .Include(x => x.CreatedBy)
                .Select(v => new PageVersionVM
                {
                    // CreatedById = v.CreatedById,
                    CreatedByName = v.CreatedBy.FullName,
                    CreationDate = v.CreationDate,
                    Id = v.Id,
                    //PageId = v.PageId,
                    //  SettingsBody = v.SettingsBody,
                    VersionId = v.VersionId
                }).OrderByDescending(x => x.VersionId).ToListAsync();
            return pageVersions;
        }

        public async Task<bool> DeletePageByID(long PageId, long UserId)

        {
            try
            {
                var Page = await _pageRepository.Query().Where(x => x.Id == PageId).Select(x => x).FirstOrDefaultAsync();
                Page.Status = EStatus.Deleted;
                Page.ModifiedById = UserId;
                Page.ModificationDate = DateTimeOffset.Now;
                await _pageRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }


        /**************************************** Page Template *********************************/
        #region Page Templates

        public async Task<bool> UpdatePagesTemplate(string PagesIds, int TemplateId, long UserId, long SiteId)
        {
            try
            {
                int[] ids = PagesIds.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
                var pageList = await _pageRepository.Query().Where(x => x.Status != EStatus.Deleted
                        && x.SiteId == SiteId).Select(x => x).ToListAsync();

                for (int i = 0; i < pageList.Count(); i++)
                {
                    for (int j = 0; j < ids.Count(); j++)
                    {
                        if (ids[j] == pageList[i].Id)
                        {
                            pageList[i].PageTemplateId = TemplateId;
                            pageList[i].ModifiedById = UserId;
                            await _pageRepository.SaveChangesAsync();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public void UpdateTemplateVersion(long? templateId, long versionId)
        {
            PageTemplates page = _pageTemplatesRepository.Query().FirstOrDefault(v => v.Id == templateId);
            page.CurrentVersionId = versionId;
            _pageRepository.SaveChanges();
        }


        public int ChangePageAndTemplatesVersions()
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                var pages = _pageRepository.Query().Include(x => x.PageVersions);
                foreach (var page in pages)
                {
                    if (page.CurrentVersionId == null && page.PageVersions.Count > 1)
                    {
                        var pageVersion = page.PageVersions.OrderBy(e => e.VersionId).Last();
                        page.CurrentVersionId = pageVersion.Id;
                    }
                    else if (page.CurrentVersionId == null)
                    {
                        page.CurrentVersion = new PageVersions
                        {
                            CreatedById = page.CreatedById,
                            CreationDate = DateTime.Now,
                            PageId = page.Id,
                            SettingsBody = "{}",
                            VersionId = 1
                        };
                    }
                }
                _pageRepository.SaveChanges();
                transaction.Commit();
            }
            using (var transaction = _context.Database.BeginTransaction())
            {
                var pagesTemplates = _pageTemplatesRepository.Query().Include(x => x.PageTemplateVersions);
                foreach (var pageTemplate in pagesTemplates)
                {
                    if (pageTemplate.CurrentVersionId == 0 && pageTemplate.PageTemplateVersions.Count > 1)
                    {
                        var pageVersion = pageTemplate.PageTemplateVersions.OrderBy(e => e.VersionId).Last();
                        pageTemplate.CurrentVersionId = pageVersion.Id;
                    }
                    else if (pageTemplate.CurrentVersionId == 0)
                    {
                        pageTemplate.CurrentVersion = new TemplateVersions
                        {
                            CreatedById = pageTemplate.CreatedById,
                            CreationDate = DateTime.Now,
                            Css = "",
                            Scripts = "",
                            SettingsBody = "{}",
                            VersionId = 1,
                            TemplateId = pageTemplate.Id
                        };
                    }
                }
                _pageTemplatesRepository.SaveChanges();
                transaction.Commit();
                return 0;
            }

        }


        public async Task<IList<PageTemplates>> GetAllPageTemplates(long SiteId, EStatus? Status)
        {
            try
            {
                var PageTemplates = await _pageTemplatesRepository.Query().Where(x => x.SiteId == SiteId
                && (
                (Status != null && x.Status == Status)  // if Status not null check template status
                || (Status == null && x.Status == EStatus.Published)  // if status is null get published
                || (Status == EStatus.All) // if status = ALL get All Template
                )).Select(x => x).ToListAsync();
                return PageTemplates;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }



        public async Task<PageTemplates> AddPageTemplates(PageTemplates pageTemplate, long SiteId, long UserId)
        {
            try
            {

                pageTemplate.Status = EStatus.Published;
                pageTemplate.SiteId = SiteId;
                pageTemplate.CreatedById = UserId;
                pageTemplate.CurrentVersion = new TemplateVersions
                {
                    CreatedById = UserId,
                    CreationDate = DateTime.Now,
                    Css = "",
                    Scripts = "",
                    SettingsBody = "{}",
                    VersionId = 1
                };

                _pageTemplatesRepository.Add(pageTemplate);
                await _pageTemplatesRepository.SaveChangesAsync();
                pageTemplate.CurrentVersion.TemplateId = pageTemplate.Id;
                await _pageTemplatesRepository.SaveChangesAsync();
                return pageTemplate;

            }
            catch (Exception exc)
            {
                throw exc;
            }

        }

        public async Task<PageTemplates> EditPageTemplates(PageTemplates pageTemplate, long UserId, long PageId)
        {
            try
            {
                var PT = await _pageTemplatesRepository.Query().Where(x => x.Id == PageId).FirstOrDefaultAsync();

                PT.ModifiedById = UserId;
                PT.ModificationDate = DateTimeOffset.Now;
                PT.Name = pageTemplate.Name;
                PT.Description = pageTemplate.Description;
                PT.Type = pageTemplate.Type;
                PT.Status = pageTemplate.Status;
                PT.AddtionalMetaTags = pageTemplate.AddtionalMetaTags;


                await _pageTemplatesRepository.SaveChangesAsync();

                return PT;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<bool> DeleteTemplateByID(long ID, long UserId)
        {

            try
            {
                var PT = await _pageTemplatesRepository.Query().Where(x => x.Id == ID).Select(x => x).FirstOrDefaultAsync();
                PT.Status = EStatus.Deleted;
                PT.ModifiedById = UserId;
                PT.ModificationDate = DateTimeOffset.Now;
                await _pageRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }



        public async Task<IList<VersionsViewModel>> SaveTemplatesSettings(TemplateSettingsModel templateSettingsModel, long siteId)
        {
            try
            {

                TemplateVersions tv = new TemplateVersions();

                tv.TemplateId = templateSettingsModel.TemplateId;
                tv.SettingsBody = templateSettingsModel.Settings;
                tv.Css = templateSettingsModel.Css;
                if (templateSettingsModel.TemplateComponents == null)
                {
                    templateSettingsModel.TemplateComponents = new List<string>();
                }

                tv.TemplateComponents = JsonConvert.SerializeObject(templateSettingsModel.TemplateComponents);
                tv.CreatedById = templateSettingsModel.UserId;
                tv.CreationDate = DateTimeOffset.Now;
                tv.VersionId = GetLatestTemplateVersionId(templateSettingsModel.TemplateId) + 1;
                _templateVersionsRepository.Add(tv);

                await _templateVersionsRepository.SaveChangesAsync();
                UpdateTemplateVersion(tv.TemplateId, tv.Id);


                string tempMaxPageVersionCount = GetSiteSettingByKey("MaxPageTemplateVersionCount", siteId);
                long MaxPageVersionCount = 15;

                if (tempMaxPageVersionCount != "")
                {
                    MaxPageVersionCount = long.Parse(tempMaxPageVersionCount);
                }

                var allOldVersions = _templateVersionsRepository.Query().Where(x => x.TemplateId == templateSettingsModel.TemplateId && x.VersionId < tv.VersionId - MaxPageVersionCount)
                    .Include(t=>t.ImportedFiles).ToList();

                foreach (var item in allOldVersions)
                {
                    _TemplateImportedFilesRepository.RemoveRange(item.ImportedFiles);
                    _TemplateImportedFilesRepository.SaveChanges();
                    _templateVersionsRepository.Remove(item);
                }

                await _templateVersionsRepository.SaveChangesAsync();

                var Versions = _templateVersionsRepository.Query().Where(x => x.TemplateId == templateSettingsModel.TemplateId).Select(x => new VersionsViewModel
                {
                    Id = x.Id,
                    VersionId = x.VersionId,
                    CreatedByName = x.CreatedBy.FullName,
                    CreationDate = x.CreationDate,
                    CreatedById = x.CreatedById
                }
                 ).OrderByDescending(x => x.VersionId).Take(100).ToList();

                return Versions;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }


        public async Task<PageTemplateVM> GetPageTemplateSettings(long TemplateId)
        {
            try
            {
                PageTemplateVM PageTemplate = await _pageTemplatesRepository.Query()
                    .Where(x => x.Id == TemplateId)
                    .Include(x => x.CurrentVersion.ImportedFiles)
                    .ThenInclude(i => i.Media)
                    .Include(x=>x.CurrentVersion.CreatedBy)
                    .Select(x => new PageTemplateVM
                    {
                        Description = x.Description,
                        Name = x.Name,
                        Type = (long)x.Type,
                        CurrentVersion = new TemplateVersionVM
                        {
                            CreatedById = x.CurrentVersion.CreatedById,
                            CreatedByName = x.CurrentVersion.CreatedBy.FullName,
                            CreationDate = x.CurrentVersion.CreationDate,
                            Css = x.CurrentVersion.Css,
                            ImportFilesList = x.CurrentVersion.ImportedFiles
                            .Select(i => new Core.ViewModels.ImportedFilesVM()
                            {
                                Id = i.Id,
                                FileId = i.Media == null ? 0 : i.Media.Id,
                                FileName = i.Media == null ? string.Empty : i.Media.Filename,
                                FileUrl = i.Media == null ? string.Empty : i.Media.PublicUrl,
                                SourceId = x.CurrentVersionId
                            })
                            .ToList(),
                            Scripts = x.CurrentVersion.Scripts,
                            SettingsBody = x.CurrentVersion.SettingsBody,
                            VersionId = x.CurrentVersion.VersionId,
                        }
                    }).FirstOrDefaultAsync();

                //var TemplateSetting = new TemplateSettingsViewModel();

                //TemplateSetting.Template = PageTemplate;

                //var Versions = _templateVersionsRepository.Query().Where(x => x.TemplateId == TemplateId).Select(x => new VersionsViewModel
                //{
                //    VersionId = x.VersionId,
                //    CreatedBy = x.CreatedBy.FullName,
                //    CreationDate = x.CreationDate,
                //    CreatedById = x.CreatedById
                //}).OrderByDescending(x => x.VersionId).Take(100).ToList();

                //TemplateSetting.Versionslist = Versions;

                //if (VersionId != -1)
                //{
                //    var Version = _templateVersionsRepository.Query().Where(x => x.TemplateId == TemplateId && x.VersionId == VersionId).FirstOrDefault();
                //    TemplateSetting.SettingsBody = Version != null ? Version.SettingsBody : null;
                //    TemplateSetting.Css = Version != null ? Version.Css : null;
                //    TemplateSetting.Scripts = Version != null ? Version.Scripts : null;
                //    TemplateSetting.ImportFiles = Version != null ? Version.ImportFiles : null;


                //}
                //else
                //{
                //    var LatestVersion = GetLatestTemplateVersionId(TemplateId);
                //    var Version = _templateVersionsRepository.Query().Where(x => x.TemplateId == TemplateId && x.VersionId == LatestVersion).FirstOrDefault();
                //    TemplateSetting.SettingsBody = Version != null ? Version.SettingsBody : null;
                //    TemplateSetting.Css = Version != null ? Version.Css : null;
                //    TemplateSetting.Scripts = Version != null ? Version.Scripts : null;
                //    TemplateSetting.ImportFiles = Version != null ? Version.ImportFiles : null;
                //}


                return PageTemplate;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }



        public async Task<PageTemplates> DuplicateTemplate(PageTemplates pageTemplate, long TemplateId, long UserId)
        {
            try
            {
                var orgTemp = await _pageTemplatesRepository.Query().Where(x => x.Id == TemplateId).Include(x => x.CurrentVersion).FirstOrDefaultAsync();

                var result = await AddPageTemplates(pageTemplate, orgTemp.SiteId, UserId);

                TemplateSettingsModel ts = new TemplateSettingsModel();

                ts.Css = orgTemp.CurrentVersion.Css;
                ts.Settings = orgTemp.CurrentVersion.SettingsBody;
                ts.UserId = UserId;
                ts.TemplateId = result.Id;

                await SaveTemplatesSettings(ts, orgTemp.SiteId);

                return result;
            }

            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<Page> GetPageByIdAsync(int id, long siteId)
        {
            try
            {
                var page = await _pageRepository.Query().Include(x => x.PageCategory).Where(x => x.Id == id).Select(x => x).FirstOrDefaultAsync();
                return page;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> SetTemplate(TemplateSettingsModel template)
        {
            try
            {
                var Template = _pageTemplatesRepository.Query()
                                .Where(x => x.Id == template.TemplateId)
                                .Include(x => x.CurrentVersion)
                                .ThenInclude(i => i.ImportedFiles)
                                .ThenInclude(i => i.Media)
                                .FirstOrDefault();

                Template.CurrentVersion.Css = template.Css;
                Template.CurrentVersion.Scripts = template.Scripts;
                await _pageTemplatesRepository.SaveChangesAsync();

                foreach (var Item in template.ImportFilesList)
                {

                }

                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<TemplateVersionVM> GetTemplateData(long TemplateId)
        {
            try
            {
                // ChangesToBeTestedByYazan
                var TemplateData = _pageTemplatesRepository.Query()
                    .Where(x => x.Id == TemplateId)
                    .Include(x => x.CurrentVersion)
                    .ThenInclude(x => x.ImportedFiles)
                    .ThenInclude(x => x.Media)
                    .Select(x => new TemplateVersionVM()
                    {
                        TemplateId = x.Id,
                        Css = x.CurrentVersion.Css,
                        ImportFilesList = x.CurrentVersion.ImportedFiles
                            .Select(i => new Core.ViewModels.ImportedFilesVM()
                            {
                                Id = i.Id,
                                FileId = i.Media == null ? 0 : i.Media.Id,
                                FileName = i.Media == null ? string.Empty : i.Media.Filename,
                                FileUrl = i.Media == null ? string.Empty : i.Media.PublicUrl,
                                SourceId = x.CurrentVersionId
                            })
                            .ToList(),
                        Scripts = x.CurrentVersion.Scripts,

                    })
                    .FirstOrDefault();


                return TemplateData;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        // this function for updating a part of template
        public async Task<bool> SetTemplate(TemplateSettingsModel template, UpdateTemplateEnum Target)
        {
            try
            {
                var Template = _pageTemplatesRepository.Query().Where(x => x.Id == template.TemplateId).Include(x => x.CurrentVersion).FirstOrDefault();
                switch (Target)
                {
                    case UpdateTemplateEnum.CSS:

                        Template.CurrentVersion.Css = template.Css;
                        break;

                    case UpdateTemplateEnum.ImportFiles: 

                        var Files = template.ImportFilesList.Where(f=>f.Id == 0).Select(i => new TemplateImportedFiles()
                        {
                            MediaId = i.FileId,
                            TemplateVersionId = Template.CurrentVersionId

                        }).ToList();

                        _TemplateImportedFilesRepository.AddRange(Files);
                        await _TemplateImportedFilesRepository.SaveChangesAsync();
                        break;

                    case UpdateTemplateEnum.Scripts:

                        Template.CurrentVersion.Scripts = template.Scripts;
                        break;
                }



                await _pageTemplatesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<VersionsViewModel>> GetTemplatesVersions(long TemplateId)
        {
            try
            {
                return await _templateVersionsRepository.Query().Where(x => x.TemplateId == TemplateId).Select(x => new VersionsViewModel
                {
                    Id = x.Id,
                    VersionId = x.VersionId,
                    CreatedByName = x.CreatedBy.FullName,
                    CreationDate = x.CreationDate,
                    CreatedById = x.CreatedById
                }).OrderByDescending(x => x.VersionId).Take(100).ToListAsync();

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> DeleteImportedFile(long Id)
        {
            try
            {
                var ImportedFile = await _TemplateImportedFilesRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                _TemplateImportedFilesRepository.Remove(ImportedFile);
                await _TemplateImportedFilesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }
        #endregion


        #region Helper_Functions

        private static PageVersionVM GetCurrentPageVersion(ICollection<PageVersions> pageVersions, long pageId)
        {
            var pageVersion = pageVersions.Where(version => version.PageId == pageId).OrderBy(e => e.VersionId).Last();
            PageVersionVM pageLatestVersion = new PageVersionVM
            {
                CreatedById = pageVersion.CreatedById,
                CreatedByName = pageVersion.CreatedBy.FullName,
                CreationDate = pageVersion.CreationDate,
                Id = pageVersion.Id,
                SettingsBody = pageVersion.SettingsBody,
                VersionId = pageVersion.VersionId
            };
            return pageLatestVersion;
        }
     

        private static TemplateVersionVM GetCurrentTemplateVersion(ICollection<TemplateVersions> templateVersions, long templateId)
        {
            var templateVersion = templateVersions
                                    .Where(version => version.TemplateId == templateId)
                                    .OrderBy(e => e.VersionId)
                                    .Last();

            TemplateVersionVM templateLatestVersion = new TemplateVersionVM
            {
                CreatedById = templateVersion.CreatedById,
                CreatedByName = templateVersion.CreatedBy.FullName,
                CreationDate = templateVersion.CreationDate,
                Css = templateVersion.Css,
               // ImportFilesList = templateVersion.ImportFiles,
                Scripts = templateVersion.Scripts,
                SettingsBody = templateVersion.SettingsBody,
                VersionId = templateVersion.VersionId
            };
            return templateLatestVersion;
        }


        private long GetLatestVersionId(long PageId)

        {
            var page = _pageRepository.Query().Where(x => x.Id == PageId).Select(x => x).FirstOrDefault();
            if (page != null)
            {
                return page.CurrentVersionId.GetValueOrDefault() ;
            }

            return 0;

        }

        private long GetLatestTemplateVersionId(long TemplateId)
        {
            var temp = _pageTemplatesRepository.Query().Where(x => x.Id == TemplateId).Select(x => x).FirstOrDefault();
            if (temp != null)
            {
                return temp.CurrentVersionId;
            }

            return 0;
        }


        private string GetSiteSettingByKey(string key, long siteId)
        {
            var setting = _siteSettingsRepository.Query().Where(x => x.SiteId == siteId && x.Id == key).Select(x => x.Value).FirstOrDefault();
            if (setting == null)
                return "";
            else
                return setting;
        }


        private async Task<bool> UpsertPageRolesAsync(Page SelectedPage, List<long> RolesId, long UserId)
        {

            foreach (var id in RolesId)
            {
                var Role = SelectedPage.PageRoles?.FirstOrDefault(x => x.RoleId == id && x.PageId == SelectedPage.Id);
                if (Role == null)
                {
                    Role = new PagesRoles()
                    {
                        RoleId = id,
                        PageId = SelectedPage.Id,
                        CreatedById = UserId
                    };
                    _PagesRolesRepository.Add(Role);

                }
                else
                {
                    Role.RoleId = id;
                    Role.CreatedById = UserId;
                    _PagesRolesRepository.Update(Role);


                }

            }

            var DeletedRoles = SelectedPage.PageRoles.Where(p => !RolesId.Contains(p.RoleId)).ToList();
            if (DeletedRoles.Count != 0)
            {
                _PagesRolesRepository.RemoveRange(DeletedRoles);
            }
            await _PagesRolesRepository.SaveChangesAsync();
            return true;
        }




        private List<PageResultVM> BuildPagesTree(List<Page> pageList)
        {
            List<Page> parentPages = new List<Page>();
            List<PageResultVM> pageResults = new List<PageResultVM>();

            parentPages = pageList.Where(x => x.ParentId == x.Id || x.ParentId == null).ToList();

            foreach (var page in parentPages)
            {
                PageResultVM pageResultVM = new PageResultVM();

                pageResultVM.Name = page.Name;
                pageResultVM.PageStatus = page.Status;
                pageResultVM.PageCategoryId = page.PageCategoryId;
                pageResultVM.PageCategoryName =page.PageCategory.Name;
                pageResultVM.CreationDate = page.CreationDate;
                pageResultVM.ParentPageId = page.Id;
                pageResultVM.TemplateName = page.PageTemplate ==null ?"No Template": page.PageTemplate.Name;
                pageResultVM.TemplateId = page.PageTemplate ==null ?0: page.PageTemplate.Id;
                pageResultVM.ParentPageSlug = page.Slug;
                pageResultVM.IsDefaultPage = page.Site == null ? false : page.Site.DefaultParentPageId == page.ParentId;
                pageResultVM.PageCultureDetails = pageList.Where(x => x.ParentId == page.Id || x.Id == page.Id)
                    .Select(c => new PageCultureDetailsVM()
                    {
                        Name = c.Name,
                        PageCultureStatus = c.Status,
                        PageId = c.Id,
                        ParentPageId = c.ParentId,
                        CultureCode = c.Culture.CultureId,
                        CultureId = c.CultureId,
                        Thumbnail = c.Thumbnail == null ? "" : c.Thumbnail.PublicUrl,
                        Slug = c.Slug
                    })
                    .ToList();


                pageResults.Add(pageResultVM);
            }

            return pageResults.OrderByDescending(p=>p.CreationDate).ToList();
        }



        //private List<PageSiteMapResultVM> BuildSiteMapTree(List<Page> pageList)
        //{
        //    List<Page> parentPages = new List<Page>();
        //    List<PageSiteMapResultVM> pageResults = new List<PageSiteMapResultVM>();

        //    parentPages = pageList.Where(x => x.ParentId == x.Id || x.ParentId == null).ToList();

        //    foreach (var page in parentPages)
        //    {
        //        PageSiteMapResultVM pageResultVM = new PageSiteMapResultVM();

        //        pageResultVM.Name = page.Name;
        //        pageResultVM.CreationDate = page.CreationDate;
        //        pageResultVM.ParentPageId = page.Id;
        //        pageResultVM.PageSlug = page.Slug;
        //        pageResultVM.CultureId = page.CultureId;
        //        //pageResultVM.PageCultureDetails = pageList.Where(x => x.ParentId == page.Id || x.Id == page.Id)
        //        //.Select(c => new PageCultureDetailsVM()
        //        //{
        //        //    PageCultureStatus = c.Status,
        //        //    PageId = c.Id,
        //        //    ParentPageId = c.ParentId,
        //        //    CultureCode = c.Culture.CultureId,
        //        //    CultureId = c.CultureId,
        //        //    Thumbnail = c.Thumbnail == null ? "" : c.Thumbnail.PublicUrl,
        //        //    Slug = c.Slug
        //        //}).ToList();


        //        pageResults.Add(pageResultVM);
        //    }

        //    return pageResults;
        //}

        #endregion

        #region Old_And_Not_Used_Apis


        //public async Task<Page> AddNewPage(PagesViewModel Page)
        //{
        //    try
        //    {
        //        if (Page.PageCategoryId == 0)
        //        {
        //            var newCat = new CategoryViewModel();
        //            newCat.Name = Page.PageCategoryName;
        //            newCat.UserId = Page.UserId;
        //            newCat.Slug = Page.PageCategoryName + "-" + Page.PageCategoryId;
        //            newCat.SiteId = Page.SiteId;
        //            newCat.CategoryTypeId = ECategoryTypes.Page;
        //            Page.PageCategoryId = await _CategoriesService.AddCategory(newCat);
        //        }
        //        Page ObjPage = new Page();
        //        ObjPage.Name = Page.Name;
        //        ObjPage.Slug = Page.Slug;
        //        ObjPage.CreatedById = Page.UserId;
        //        ObjPage.CreationDate = DateTimeOffset.Now;
        //        ObjPage.Status = Page.Status;
        //        ObjPage.StatusDate = DateTimeOffset.Now;
        //        ObjPage.NavigationTitle = Page.Slug;
        //        ObjPage.PageCategoryId = Page.PageCategoryId;
        //        ObjPage.SiteId = Page.SiteId;
        //        ObjPage.Route = Page.Route;
        //        ObjPage.Description = Page.Description;
        //        ObjPage.Keywords = Page.Keywords;
        //        ObjPage.AddtionalMetaTags = Page.AddtionalMetaTags;
        //        ObjPage.AdditionalContent = Page.AdditionalContent;
        //        ObjPage.IsLoginRequired = Page.IsLoginRequired;
        //        ObjPage.ThumbnailId = Page.ThumbnailId;

        //        ObjPage.CultureId = Page.CultureId;

        //        long? noTemplate = null;
        //        ObjPage.PageTemplateId = Page.PageTemplateId != 0 ? Page.PageTemplateId : noTemplate;
        //        _pageRepository.Add(ObjPage);
        //        ObjPage.CurrentVersion = new PageVersions
        //        {
        //            CreatedById = ObjPage.CreatedById,
        //            CreationDate = DateTime.Now,
        //            SettingsBody = "{}",
        //            VersionId = 1
        //        };
        //        await _pageRepository.SaveChangesAsync();
        //        ObjPage.CurrentVersion.PageId = ObjPage.Id;
        //        await _pageRepository.SaveChangesAsync();
        //        return ObjPage;
        //    }
        //    catch (Exception exc)
        //    {
        //        throw exc;
        //    }
        //}


        //public async Task<Page> EditPage(PagesViewModel Page)
        //{
        //    try
        //    {

        //        var ObjPage = await _pageRepository.Query().Where(x => x.Id == Page.Id).FirstOrDefaultAsync();
        //        if (Page.PageCategoryId == 0)
        //        {
        //            var newCat = new CategoryViewModel();
        //            newCat.Name = Page.PageCategoryName;
        //            newCat.UserId = Page.CreatedById;
        //            newCat.Slug = Page.PageCategoryName + "-" + Page.PageCategoryId;
        //            newCat.SiteId = Page.SiteId;
        //            newCat.CategoryTypeId = ECategoryTypes.Page;
        //            Page.PageCategoryId = await _CategoriesService.AddCategory(newCat);
        //        }
        //        ObjPage.Name = Page.Name;
        //        ObjPage.Slug = Page.Slug;
        //        ObjPage.CreatedById = ObjPage.CreatedById;
        //        ObjPage.CreationDate = ObjPage.CreationDate;
        //        ObjPage.ModifiedById = Page.UserId;
        //        ObjPage.ModificationDate = DateTimeOffset.Now;
        //        ObjPage.Status = Page.Status;
        //        ObjPage.StatusDate = DateTimeOffset.Now;
        //        ObjPage.NavigationTitle = Page.Slug;
        //        ObjPage.PageCategoryId = Page.PageCategoryId;
        //        ObjPage.SiteId = Page.SiteId;
        //        ObjPage.Route = Page.Route;
        //        ObjPage.Description = Page.Description;
        //        ObjPage.Keywords = Page.Keywords;
        //        ObjPage.AddtionalMetaTags = Page.AddtionalMetaTags;
        //        ObjPage.IsLoginRequired = Page.IsLoginRequired;
        //        ObjPage.ThumbnailId = Page.ThumbnailId;
        //        ObjPage.AdditionalContent = Page.AdditionalContent;
        //        ObjPage.PageTemplateId = Page.PageTemplateId > 0 ? Page.PageTemplateId : null;
        //        //ObjPage.PageTemplateId = Page.CultureId;
        //        ObjPage.Slug = Page.Slug;

        //        await _pageRepository.SaveChangesAsync();
        //        return ObjPage;
        //    }
        //    catch (Exception exc)
        //    {
        //        throw exc;
        //    }
        //}
        //Bulk update and delete
        #endregion

        public async Task<Dictionary<string, object>> GetComponentsSettings(long PageId, string ComponentId, string Culture)
        {
            try
            {
                var pageContent = await _pageVersions.Query().Where(x => x.PageId == PageId).OrderByDescending(x => x.VersionId).FirstOrDefaultAsync();

                JObject jo = JObject.Parse(pageContent.SettingsBody);

                foreach (JToken token in jo.FindTokens("cols"))
                {
                    foreach (JToken comp in token.FindTokens("component"))
                    {
                        var sett = comp.FindTokens("setting");
                        //{

                        var returnss = sett.Values<JProperty>().SingleOrDefault(i => i.Name == Culture).Value.ToString();

                        JObject jsonObj = JObject.Parse(returnss);
                        var validationobject = jsonObj["componentSettingsId"].ToString();
                        if (validationobject == ComponentId)
                        {
                            Dictionary<string, object> dictObj = jsonObj.ToObject<Dictionary<string, object>>();
                            return dictObj;
                        }

                        //}


                    }

                }
                return new Dictionary<string, object>();
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

      
    }
}
