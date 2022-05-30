
using EskaCMS.BlogArchive.Entities;
using EskaCMS.BlogArchive.Services.Interfaces;
using EskaCMS.Core.Data;
using EskaCMS.Core.Entities;
using EskaCMS.Core.Enums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Menus.Entities;
using EskaCMS.Menus.Models;
using EskaCMS.Menus.Services.Interfaces;
using EskaCMS.Pages.Entities;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.Menus.Services
{
    public class MenuService : IMenu
    {

        private readonly IRepository<Menu> _MenuRepository;
        private readonly IRepository<MenuGroup> _MenuGroupRepository;
        //    private readonly IRepository<MenuSettings> _MenuSettingsRepository;
        private readonly IRepository<Page> _pageRepository;
        private readonly IRepository<Posts> _PostRepository;
        //private readonly IRepository<Category> _PostCategoriesRepository;

        private readonly IBlogArchive _BlogArchiveService;
        private readonly ICategories _CategoriesService;
        private readonly IUserRoleService _UserRoleService;
        private readonly IMenuRole _MenuRoleService;
        private readonly IRepository<MenusRoles> _MenuRolesRepository;
        private readonly IWorkContext _workContext;
        private readonly EskaDCMSDbContext _Context;
        public MenuService(IRepository<Posts> PostRepository,
                            IBlogArchive BlogArchiveService,
                            IRepository<Page> pageRepository,
                            IRepository<Menu> menuRepository,
                            IRepository<MenuGroup> MenuGroupRepository,
                            // IRepository<MenuSettings> menuSettingsRepository,
                            ICategories CategoriesService,
                            IUserRoleService UserRoleService,
                            IMenuRole MenuRoleService,
                             IRepository<MenusRoles> MenuRolesRepository,
                            IWorkContext workContext,
                            EskaDCMSDbContext Context
                           )
        {
            _MenuRepository = menuRepository;
            // _MenuSettingsRepository = menuSettingsRepository;
            _pageRepository = pageRepository;
            _PostRepository = PostRepository;
            _BlogArchiveService = BlogArchiveService;
            _CategoriesService = CategoriesService;
            _UserRoleService = UserRoleService;
            _MenuRoleService = MenuRoleService;
            _MenuRolesRepository = MenuRolesRepository;
            _workContext = workContext;
            _MenuGroupRepository = MenuGroupRepository;
            _Context = Context;
        }

        public MenuService()
        {
        }


        public async Task<MenuViewModel> GetMenuById(long id)
        {
            try
            {
                // get published static menu
                var Menu = await _MenuRepository.Query().Where(y => y.Id == id)
                    .Include(x => x.Thumbnail)
                    .Include(x => x.MenuGroup)
                    //.Include(x => x.MenuSettings)
                    .Include(x => x.Post)
                    .Include(x => x.Page)
                    .Include(x => x.Product)
                    .Select(x =>
                       new MenuViewModel
                       {
                           Id = x.Id,
                           Name = x.Name,
                           Icon = x.Icon,
                           PageId = x.PageId,
                           PostId = x.PostId,
                           ProductId = x.ProductId,
                           MenuType = x.MenuType,
                           Title = x.Title,
                           ThumbnailURL = x.Thumbnail.PublicUrl,
                           ThumbnailId = x.ThumbnailId.HasValue ? x.ThumbnailId.Value : null,
                           Slug = GetSlug(x),// x.Slug,
                           TemplateId = x.TemplateId,
                           SiteId = x.SiteId,
                           Status = x.Status,
                           CategoriesId = x.MenuGroup.CategoryId ?? 0,
                           //  Content = x.MenuSettings.Settings,
                           Order = x.Order,
                           ExternalSlug = x.ExternalSlug
                           // AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,

                       }
                      ).FirstOrDefaultAsync();

                return Menu;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<MenuBreadCrumbVM>> GetMenuBreadCrumb(long id)
        {
            try
            {
                var TargetMenu = await _MenuRepository.Query()
                                            .Where(y => y.Id == id)
                                            .Include(x => x.Thumbnail)
                                            .Include(x => x.MenuGroup)
                                            .Include(x => x.Post)
                                            .Include(x => x.Page)
                                            .Include(x => x.Product)
                                            .Select(x =>
                                                       new MenuBreadCrumbVM
                                                       {
                                                           Id = x.Id,
                                                           Name = x.Name,
                                                           Icon = x.Icon,
                                                           Title = x.Title,
                                                           ThumbnailURL = x.Thumbnail.PublicUrl,
                                                           ThumbnailId = x.ThumbnailId.HasValue ? x.ThumbnailId.Value : null,
                                                           Slug = GetSlug(x),// x.Slug,
                                                           ParentId = x.ParentId,
                                                           MenuGroupId=x.MenuGroupId,
                                                         })
                                            .FirstOrDefaultAsync();

                var Menus = await _MenuRepository
                    .Query()
                    .Where(y => y.MenuGroupId == TargetMenu.MenuGroupId &&
                                y.Status != EStatus.Deleted)
                    .Include(x => x.Thumbnail)
                    .Include(x => x.MenuGroup)
                    .Include(x => x.Post)
                    .Include(x => x.Page)
                    .Include(x => x.Product)
                    .Select(x =>
                       new MenuBreadCrumbVM
                       {
                           Id = x.Id,
                           Name = x.Name,
                           Icon = x.Icon,
                           Title = x.Title,
                           ThumbnailURL = x.Thumbnail.PublicUrl,
                           ThumbnailId = x.ThumbnailId.HasValue ? x.ThumbnailId.Value : null,
                           Slug = GetSlug(x),// x.Slug,
                           ParentId = x.ParentId,
                           MenuGroupId = x.MenuGroupId,

                       }
                      ).ToListAsync();


               var Result=  await GetMenuBreadCrumb(TargetMenu , Menus , new List<MenuBreadCrumbVM>());
                Result.Reverse();
                return Result;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<MenuBreadCrumbVM>> GetMenuBreadCrumb(MenuBreadCrumbVM TargetMenu ,List<MenuBreadCrumbVM> MenuList , List<MenuBreadCrumbVM> MenuBreadCrumb)
        {

            if(TargetMenu == null)
            {
                return MenuBreadCrumb;
            }
            else
            {
                var TargetMenuParent = MenuList.FirstOrDefault(m=>m.Id == TargetMenu.ParentId);
                MenuBreadCrumb.Add(TargetMenu);
                return await GetMenuBreadCrumb(TargetMenuParent, MenuList, MenuBreadCrumb);
            }
        }

        public async Task<MenuViewModel> GetMenuByIdWithChilds(long id)
        {
            try
            {
                // get published static menu
                var Menu = await _MenuRepository.Query().Where(y => y.Id == id)
                    .Include(x => x.Thumbnail)
                    //  .Include(x => x.MenuSettings)
                    .Include(x => x.MenuGroup)
                    .Include(x => x.Post)
                    .Include(x => x.Page)
                    .Include(x => x.Product)
                    .Select(x =>
                       new MenuViewModel
                       {
                           Id = x.Id,
                           Name = x.Name,
                           Icon = x.Icon,
                           PageId = x.PageId,
                           PostId = x.PostId,
                           ProductId = x.ProductId,
                           MenuType = x.MenuType,
                           Title = x.Title,
                           ThumbnailURL = x.Thumbnail.PublicUrl,
                           ThumbnailId = x.ThumbnailId,
                           Slug = GetSlug(x),// x.Slug,
                           TemplateId = x.TemplateId,
                           SiteId = x.SiteId,
                           Status = x.Status,
                           CategoriesId = x.MenuGroup.CategoryId ?? 0,
                           // Content = x.MenuSettings.Settings,
                           Order = x.Order,
                           ExternalSlug = x.ExternalSlug
                           //  AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,

                       }
                      ).FirstOrDefaultAsync();

                Menu.Childs = GetChilds(Menu.Id, GeneralEnums.EStatus.Published);

                return Menu;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<IList<MenuViewModel>> GetMenusBySiteIdAndMenuType(long siteId, GeneralEnums.EMenuTypes Type)
        {
            try
            {
                // get not deleted static menu
                var Menus = await _MenuRepository.Query()
                    .Where(y => y.MenuType == Type && y.Status != GeneralEnums.EStatus.Deleted && y.SiteId == siteId)
                    .Include(x => x.Thumbnail)
                     // .Include(x => x.MenuSettings)
                     .Include(x => x.Post)
                     .Include(x => x.MenuGroup)
                    .Include(x => x.Page)
                    .Include(x => x.Product)
                    .Select(x =>
                       new MenuViewModel
                       {
                           Id = x.Id,
                           Name = x.Name,
                           Icon = x.Icon,
                           PageId = x.PageId,
                           PostId = x.PostId,
                           ProductId = x.ProductId,
                           MenuType = x.MenuType,
                           Title = x.Title,
                           ThumbnailURL = x.Thumbnail.PublicUrl,
                           ThumbnailId = x.ThumbnailId,
                           Slug = GetSlug(x),// x.Slug,
                           TemplateId = x.TemplateId,
                           SiteId = x.SiteId,
                           Status = x.Status,
                           CategoriesId = x.MenuGroup.CategoryId ?? 0,
                           //   Content = x.MenuSettings.Settings,
                           Order = x.Order,
                           ExternalSlug = x.ExternalSlug
                           //  AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                       }
                      ).ToListAsync();

                return Menus;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<List<MenuViewModel>> GetMenusByCategoryId(long CatId, GeneralEnums.EStatus status, long userId, GeneralEnums.SourceMenuType sourceType)
        {
            try
            {
                bool isGuest = _workContext.IsGuestUser().Result;

                List<MenuViewModel> MenuItems = new List<MenuViewModel>();
                var query = _MenuRepository.Query();
                switch (sourceType)
                {
                    case GeneralEnums.SourceMenuType.Admin:
                        query = query.Include(q => q.MenuGroup).Where(x => x.MenuGroup.CategoryId == CatId);
                        break;
                    case GeneralEnums.SourceMenuType.Web:
                        var securityMenuList = _MenuRolesRepository.Query().Select(x => x.MenuId)
            .Distinct().ToList();
                        if (!isGuest)
                        {
                            // If normal user --> get authorized and all public menu for this category
                            var RoleObj = _UserRoleService.GetRoleByUserId(userId).Result;
                            var userRoleIds = RoleObj.Select(x => x.RoleId).ToList();
                            if (userRoleIds != null && userRoleIds.Count > 0)
                            {
                                query = query
                                    .Include(x => x.MenuGroup)
                                    .Include(x => x.MenuRoles)
                                    .Where(x =>
                                    (
                                        (x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)) && x.MenuGroup.CategoryId == CatId) ||
                                        (!securityMenuList.Contains(x.Id) && x.MenuGroup.CategoryId == CatId)
                                    ));
                            }
                        }
                        else
                        {
                            // If guest user --> get public menu for this category
                            query = query.Include(x => x.MenuGroup).Where(x => x.MenuGroup.CategoryId == CatId && !securityMenuList.Contains(x.Id));
                        }
                        break;
                }

                var items = query.Where(x => ((x.Status == status) || (status == GeneralEnums.EStatus.All && x.Status != GeneralEnums.EStatus.Deleted))
                          && x.ParentId == null)
                        .Include(x => x.Thumbnail)
                         .Include(x => x.Post)
                         .Include(x => x.Page)
                         .Include(x => x.Parent)
                         .Include(x => x.MenuGroup)
                         .Include(x => x.Product).ToList();

                MenuItems = items.Select(x =>
                                  new MenuViewModel
                                  {
                                      Id = x.Id,
                                      Name = x.Name,
                                      Icon = x.Icon,
                                      PageId = x.PageId,
                                      MenuType = x.MenuType,
                                      ThumbnailId = x.ThumbnailId,
                                      Title = x.Title,
                                      ThumbnailURL = x.Thumbnail == null ? "" : x.Thumbnail.PublicUrl,
                                      Slug = x.MenuType == EMenuTypes.None ? "" : GetSlug(x), //x.MenuType == GeneralEnums.EMenuTypes.Page ? x.Page.Slug : x.ExternalSlug,
                                      TemplateId = x.TemplateId,
                                      SiteId = x.SiteId,
                                      Status = x.Status,
                                      CategoriesId = x.MenuGroup.CategoryId ?? 0,
                                      Order = x.Order,
                                      ParentId = x.ParentId,
                                      ExternalSlug = x.ExternalSlug
                                      //Content = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.Settings).FirstOrDefault(),
                                      //AddtionalMetaTags = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.AddtionalMetaTags).FirstOrDefault(),
                                  }
                                 ).OrderBy(x => x.Order).Distinct().ToList();

                foreach (var item in MenuItems)
                {
                    item.Childs = GetChilds(item.Id, status);
                }
                return MenuItems;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public static string GetSlug(Menu menu)
        {
            switch (menu.MenuType)
            {
                case EMenuTypes.Page:
                    return menu.Page.Slug;
                case EMenuTypes.Post:
                    return menu.Post.Slug;
                case EMenuTypes.Product:
                    return menu.Product.Slug;
                case EMenuTypes.URL:
                    return menu.ExternalSlug;
                case EMenuTypes.None:
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }
        public async Task<List<MenuViewModel>> GetMenusByCategoryIdForWebAsync(long CatId)
        {
            try
            {
                long SiteId = await _workContext.GetCurrentSiteIdAsync();
                var CurrentUserId = _workContext.GetCurrentUserId().Result;

                List<Menu> Query = new List<Menu>(); // for top level only
                List<Menu> AllSubMenus = new List<Menu>();

                List<MenuViewModel> MenuItems = new List<MenuViewModel>();
                var RoleObj = _UserRoleService.GetRoleByUserId(CurrentUserId).Result;
                var userRoleIds = RoleObj.Select(x => x.RoleId).ToList();

                // If normal user --> get authorized and all public menu for this category
                Query = _MenuRepository.Query()
                       .Include(x => x.MenuGroup)
                      .Where(x => x.SiteId == SiteId
                        && x.MenuGroup.CategoryId == CatId
                        && x.Status == GeneralEnums.EStatus.Published
                        && (x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)) || x.MenuRoles.Count == 0)
                        && x.ParentId == null
                        )
                       .Include(x => x.Parent)
                       .Include(x => x.Page)
                       .Include(x => x.MenuRoles).ToList();



                AllSubMenus = _MenuRepository.Query()
                      .Include(x => x.MenuGroup)
                      .Where(x => x.SiteId == SiteId
                        && x.MenuGroup.CategoryId == CatId
                        && x.Status == GeneralEnums.EStatus.Published
                        && (x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)) || x.MenuRoles.Count == 0)
                        && x.ParentId != null
                        )
                       .Include(x => x.Parent)
                       .Include(x => x.Page)
                       .Include(x => x.MenuRoles)
                       .Include(x => x.MenuGroup)
                       .ToList();


                MenuItems = Query.Select(x =>
                                  new MenuViewModel
                                  {
                                      Id = x.Id,
                                      Name = x.Name,
                                      Icon = x.Icon,
                                      PageId = x.PageId,
                                      MenuType = x.MenuType,
                                      ThumbnailURL = x.Thumbnail.PublicUrl,
                                      Title = x.Title,
                                      //  Image = x.Image,
                                      Slug = GetSlug(x), //x.MenuType == GeneralEnums.EMenuTypes.Page ? x.Page.Slug : x.ExternalSlug,
                                      TemplateId = x.TemplateId,
                                      SiteId = x.SiteId,
                                      Status = x.Status,
                                      CategoriesId = x.MenuGroup.CategoryId ?? 0,
                                      Order = x.Order,
                                      Childs = GetChildsForWeb(x.Id, AllSubMenus),
                                      ParentId = x.ParentId,
                                      ExternalSlug = x.ExternalSlug
                                      //Content = query.
                                      // AddtionalMetaTags = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.AddtionalMetaTags).FirstOrDefault(),
                                  }
                                 ).OrderBy(x => x.Order).Distinct().ToList();

                //foreach (var item in MenuItems)
                //{
                //    item.Childs = GetChildsForWeb(item.Id);
                //}
                return MenuItems;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public List<TabViewModel> GetTabsByCategoryId(long CatId)
        {
            try
            {
                List<TabViewModel> tabs = _MenuRepository.Query()
                    .Include(x => x.MenuGroup)
                    .Where(x => x.MenuGroup.CategoryId == CatId && x.Status == GeneralEnums.EStatus.Published && x.ParentId == null)
                    .Include(x => x.Thumbnail)
                    .Select(t => new TabViewModel
                    {
                        Id = t.Id,
                        ThumbnailURL = t.Thumbnail.PublicUrl,
                        ThumbnailId = t.ThumbnailId.HasValue ? t.ThumbnailId.Value : null,
                        Name = t.Name,
                        Slug = GetSlug(t)
                    }).ToList();
                return tabs;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        //Checked with Rami, this action is not used at all //Firas
        //public async Task<SubMenuViewModel> GetSubMenu(long catId, long id, string idType)
        //{
        //    try
        //    {
        //        var subMenuVM = new SubMenuViewModel();

        //        if (idType == "1")
        //        {
        //            subMenuVM.currentMenu = await _MenuRepository.Query()
        //                .Where(y => y.CategoryId == catId && y.Status == GeneralEnums.EStatus.Published
        //                && y.MenuType == GeneralEnums.EMenuTypes.Page
        //                && y.PageId == id)
        //                .Include(x => x.Thumbnail)
        //                .Include(x=>x.Parent)
        //                .Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 MenuType = x.MenuType,
        //                 ThumbnailId=x.ThumbnailId,
        //                 Title = x.Title,
        //                 ThumbnailURL = x.Thumbnail.PublicUrl,
        //                 Slug = x.GetSlug(x),
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 CategoriesId = x.CategoryId ?? 0,

        //             }).FirstOrDefaultAsync();
        //            subMenuVM.parentMenu = subMenuVM.currentMenu.Parent.Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 MenuType = x.MenuType,
        //                 ThumbnailId = x.ThumbnailId,
        //                 Title = x.Title,
        //                 ThumbnailURL = x.Thumbnail.PublicUrl,
        //                 Slug = x.GetSlug(x),
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 CategoriesId = x.CategoryId ?? 0
        //             }).FirstOrDefault();

        //            subMenuVM.menuSiblings = await _MenuRepository.Query().Where(y => y.CategoryId == catId && y.ParentId == subMenuVM.currentMenu.ParentId && y.Status == GeneralEnums.EStatus.Published).Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 MenuType = x.MenuType,
        //                 ThumbnailId = x.ThumbnailId,
        //                 Title = x.Title,
        //                 ThumbnailURL = x.Thumbnail.PublicUrl,
        //                 Slug = x.GetSlug(x),
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 CategoriesId = x.CategoryId ?? 0
        //             }
        //              ).ToArrayAsync();

        //            if (subMenuVM.menuSiblings.Count > 0)
        //            {
        //                foreach (var item in subMenuVM.menuSiblings)
        //                {
        //                    if (item.Type == GeneralEnums.EMenuTypes.Page)
        //                    {
        //                        item.Slug = _pageRepository.Query().Where(y => y.Id == item.PageId).FirstOrDefault().Slug;
        //                    }

        //                    item.Childs = GetChilds(item.Id, GeneralEnums.EStatus.Published);
        //                }
        //            }

        //            return subMenuVM;

        //        }
        //        else if (idType == "2")
        //        {
        //            subMenuVM.currentMenu = await _MenuRepository.Query().Where(y => y.Id == id).Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 Type = x.Type,
        //                 Url = x.Url,
        //                 Title = x.Title,
        //                 Image = x.Image,
        //                 Slug = x.Slug,
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 ParentId = x.ParentId,
        //                 CategoriesId = x.CategoryId ?? 0
        //             }).FirstOrDefaultAsync();
        //            subMenuVM.parentMenu = await _MenuRepository.Query().Where(y => y.Id == subMenuVM.currentMenu.ParentId).Select(x =>
        //             new MenuViewModel
        //             {
        //                 Id = x.Id,
        //                 Name = x.Name,
        //                 Icon = x.Icon,
        //                 PageId = x.PageId,
        //                 Type = x.Type,
        //                 Url = x.Url,
        //                 Title = x.Title,
        //                 Image = x.Image,
        //                 Slug = x.Slug,
        //                 TemplateId = x.TemplateId,
        //                 SiteId = x.SiteId,
        //                 Status = x.Status,
        //                 CategoriesId = x.CategoryId ?? 0
        //             }).FirstOrDefaultAsync();
        //            subMenuVM.menuSiblings = await _MenuRepository.Query().Where(y => y.CategoryId == catId && y.ParentId == subMenuVM.currentMenu.ParentId && y.Status == GeneralEnums.EStatus.Published).Select(x =>
        //              new MenuViewModel
        //              {
        //                  Id = x.Id,
        //                  Name = x.Name,
        //                  Icon = x.Icon,
        //                  PageId = x.PageId,
        //                  Type = x.Type,
        //                  Url = x.Url,
        //                  Title = x.Title,
        //                  Image = x.Image,
        //                  Slug = x.Slug,
        //                  TemplateId = x.TemplateId,
        //                  SiteId = x.SiteId,
        //                  Status = x.Status,
        //                  CategoriesId = x.CategoryId ?? 0
        //              }
        //              ).ToArrayAsync();

        //            if (subMenuVM.menuSiblings.Count > 0)
        //            {
        //                foreach (var item in subMenuVM.menuSiblings)
        //                {
        //                    if (item.Type == GeneralEnums.EMenuTypes.Page)
        //                    {
        //                        item.Slug = _pageRepository.Query().Where(y => y.Id == item.PageId).FirstOrDefault().Slug;
        //                    }
        //                    item.Childs = GetChilds(item.Id, GeneralEnums.EStatus.Published);
        //                }
        //            }
        //            return subMenuVM;

        //        }
        //        else if (idType == "3")
        //        {
        //            var blog = _PostRepository.Query().Where(y => y.Id == id).FirstOrDefault();

        //            subMenuVM.currentMenu = new MenuViewModel
        //            {
        //                Id = blog.Id,
        //                Name = blog.Title,
        //                Slug = blog.Slug
        //            };

        //            //var blogCat = await _CategoriesService.GetCategoriesById(blog.PostCategoryId);// _PostCategoriesRepository.Query().Where(y => y.Id == blog.PostCategoryId).FirstOrDefault();
        //            //subMenuVM.parentMenu = new MenuViewModel
        //            //{
        //            //    Id = blogCat.Id,
        //            //    Name = blogCat.Name,
        //            //};


        //            dynamic blogs = await _BlogArchiveService.GetPosts(blog.SiteId, -1, 1, 100);

        //            subMenuVM.menuSiblings = new List<MenuViewModel>();

        //            foreach (var item in blogs.PostsList)
        //            {
        //                var s = new MenuViewModel();
        //                s.Id = item.Id;
        //                s.Name = item.Title;
        //                s.Slug = item.Slug;
        //                s.Type = GeneralEnums.EMenuTypes.Page;

        //                subMenuVM.menuSiblings.Add(s);
        //            }

        //        }



        //        return subMenuVM;

        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}
        public List<MenuViewModel> GetChildsForWeb(long ParentId, List<Menu> AllMenuItems)
        {
            try
            {


                var ChildMenuItems = AllMenuItems //.Include(x => x.MenuRoles)
                    .Where(x => x.ParentId == ParentId)
                    .Select(x => new MenuViewModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Icon = x.Icon,
                        PageId = x.PageId,
                        MenuType = x.MenuType,
                        // Url = x.Url,
                        Title = x.Title,
                        //  Image = x.Image,
                        Slug = x.MenuType == GeneralEnums.EMenuTypes.Page ? x.Page.Slug : x.ExternalSlug,
                        TemplateId = x.TemplateId,
                        SiteId = x.SiteId,
                        Status = x.Status,
                        // CategoriesId = x.CategoryId ?? 0,
                        MenuGroupId = x.MenuGroupId,
                        Order = x.Order,
                        Childs = GetChildsForWeb(x.Id, AllMenuItems),
                        ParentId = x.ParentId,
                        ThumbnailId = x.ThumbnailId,
                        ThumbnailURL = x.Thumbnail?.PublicUrl,
                        ExternalSlug = x.ExternalSlug
                        //Content = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.Settings).FirstOrDefault(),
                        //  AddtionalMetaTags = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.AddtionalMetaTags).FirstOrDefault(),
                    }
                  ).OrderBy(x => x.Order).Distinct().ToList();
                //foreach (var item in AuthrizedMenuItems)
                //{
                //    item.Childs = GetChildsForWeb(item.Id, AllMenuItems);
                //    AuthorizedMenuList.Add(item);
                //}

                return ChildMenuItems;
                //}
                //else
                //{
                //    var Menus = _MenuRepository.Query()
                //        .Where(y => y.ParentId == parentId
                //&& ((y.Status == status) || (status == GeneralEnums.EStatus.All && y.Status != GeneralEnums.EStatus.Deleted)))
                //        .Select(x =>
                //          new MenuViewModel
                //          {
                //              Id = x.Id,
                //              Name = x.Name,
                //              Icon = x.Icon,
                //              PageId = x.PageId,
                //              Type = x.Type,
                //              Url = x.Url,
                //              Title = x.Title,
                //              Image = x.Image,
                //              Order = x.Order,
                //              TemplateId = x.TemplateId,
                //              SiteId = x.SiteId,
                //              Status = x.Status,
                //              ParentId = x.ParentId,
                //              CategoriesId = x.CategoryId ?? 0,
                //              Content = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.Settings).FirstOrDefault(),
                //              //   AddtionalMetaTags = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.AddtionalMetaTags).FirstOrDefault(),
                //              //  Slug = x.Type == GeneralEnums.EMenuTypes.Page ? _pageRepository.Query().Where(y => y.Id == x.PageId).Select(x => x.Slug).FirstOrDefault() : x.Slug
                //          }
                //         ).OrderBy(x => x.Order).ToList();
                //    if (Menus.Count > 0)
                //    {
                //        foreach (var item in Menus)
                //        {
                //            item.Childs = GetChilds(item.Id, status);
                //        }
                //    }
                //    else
                //        Menus = new List<MenuViewModel>();
                //    return Menus;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<MenuViewModel> GetChilds(long parentId, GeneralEnums.EStatus status, List<MenusRoles> MenuRoles = null)
        {
            try
            {

                List<MenuViewModel> AuthorizedMenuList = new List<MenuViewModel>();
                if (MenuRoles?.Count > 0)
                {
                    var AuthrizedMenuItems = _MenuRepository.Query()
                        .Where(x => MenuRoles.Select(y => y.MenuId).ToList().Contains(x.Id)
                        && x.ParentId == parentId
                        && ((x.Status == status) || (status == GeneralEnums.EStatus.All && x.Status != GeneralEnums.EStatus.Deleted)))
                        .Include(x => x.Thumbnail)
                        .Include(x => x.Parent)
                        //  .Include(x=>x.MenuSettings)
                        .Select(x => new MenuViewModel
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Icon = x.Icon,
                            PageId = x.PageId,
                            MenuType = x.MenuType,
                            ThumbnailId = x.ThumbnailId,
                            Title = x.Title,
                            ThumbnailURL = x.Thumbnail.PublicUrl,
                            Slug = GetSlug(x),// x.Type == GeneralEnums.EMenuTypes.Page ? x.Page.Slug : x.Slug,
                            TemplateId = x.TemplateId,
                            SiteId = x.SiteId,
                            Status = x.Status,
                            // CategoriesId = x.CategoryId ?? 0,
                            MenuGroupId = x.MenuGroupId,
                            Order = x.Order,
                            ParentId = parentId,
                            ExternalSlug = x.ExternalSlug
                            // Content = x.MenuSettings.Settings,
                            // AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                        }
                      ).OrderBy(x => x.Order).Distinct().ToList();
                    foreach (var item in AuthrizedMenuItems)
                    {
                        item.Childs = GetChilds(item.Id, status);
                        AuthorizedMenuList.Add(item);
                    }

                    return AuthorizedMenuList.Distinct().ToList();
                }
                else
                {
                    var Menus = _MenuRepository.Query()
                        .Where(y => y.ParentId == parentId
                        && ((y.Status == status) || (status == GeneralEnums.EStatus.All && y.Status != GeneralEnums.EStatus.Deleted)))
                        .Include(x => x.Thumbnail)
                        .Include(x => x.Page)
                        // .Include(x=>x.MenuSettings)
                        .Select(x =>
                          new MenuViewModel
                          {
                              MenuGroupId = x.MenuGroupId,
                              Id = x.Id,
                              Name = x.Name,
                              Icon = x.Icon,
                              PageId = x.PageId,
                              MenuType = x.MenuType,
                              ThumbnailId = x.ThumbnailId,
                              Title = x.Title,
                              ThumbnailURL = x.Thumbnail.PublicUrl,
                              Slug = GetSlug(x),// x.Type == GeneralEnums.EMenuTypes.Page ? x.Page.Slug : x.Slug,
                              TemplateId = x.TemplateId,
                              SiteId = x.SiteId,
                              Status = x.Status,
                              // CategoriesId = x.CategoryId ?? 0,
                              Order = x.Order,
                              ParentId = parentId
                              //   Content = x.MenuSettings.Settings,
                              // AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                          }
                         ).OrderBy(x => x.Order).ToList();
                    if (Menus.Count > 0)
                    {
                        foreach (var item in Menus)
                        {
                            item.Childs = GetChilds(item.Id, status);
                        }
                    }
                    else
                        Menus = new List<MenuViewModel>();
                    return Menus;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<MenuViewModel> GetChilds(long parentId, GeneralEnums.EStatus status, GeneralEnums.SourceMenuType source, List<MenusRoles> MenuRoles = null)
        {
            try
            {
                var securedMenu = _MenuRolesRepository.Query().Select(x => x.MenuId)
                     .Distinct().ToList();
                List<MenuViewModel> AuthorizedMenuList = new List<MenuViewModel>();
                if (MenuRoles?.Count > 0)
                {

                    var AuthrizedMenuItems = _MenuRepository.Query()
                        .Where(x => MenuRoles.Select(y => y.MenuId).ToList().Contains(x.Id)
                        && x.ParentId == parentId
                        && ((x.Status == status) || (status == GeneralEnums.EStatus.All && x.Status != GeneralEnums.EStatus.Deleted)))
                        .Include(x => x.Thumbnail)
                        // .Include(x => x.MenuSettings)
                        .Select(x => new MenuViewModel
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Icon = x.Icon,
                            PageId = x.PageId,
                            MenuType = x.MenuType,
                            ThumbnailId = x.ThumbnailId,
                            Title = x.Title,
                            ThumbnailURL = x.Thumbnail.PublicUrl,
                            Slug = GetSlug(x),// x.Type == GeneralEnums.EMenuTypes.Page ? x.Page.Slug : x.Slug,
                            ExternalSlug = GetSlug(x),
                            TemplateId = x.TemplateId,
                            SiteId = x.SiteId,
                            Status = x.Status,
                            //CategoriesId = x.CategoryId ?? 0,
                            MenuGroupId = x.MenuGroupId,
                            Order = x.Order,
                            //   Content = x.MenuSettings.Settings,
                            //  AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                        }
                      ).Distinct().ToList();
                    foreach (var item in AuthrizedMenuItems)
                    {
                        item.Childs = GetChilds(item.Id, status);
                        AuthorizedMenuList.Add(item);
                    }

                    return AuthorizedMenuList.Distinct().ToList();
                }
                else
                {
                    var Menus = _MenuRepository.Query()
                        .Where(y => y.ParentId == parentId
                    && !securedMenu.Contains(y.Id)
                    && ((y.Status == status) || (status == GeneralEnums.EStatus.All && y.Status != GeneralEnums.EStatus.Deleted)))
                        .Include(x => x.Thumbnail)
                        .Include(x => x.MenuGroup)
                        // .Include(x => x.MenuSettings)
                        .Select(x => new MenuViewModel
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Icon = x.Icon,
                            PageId = x.PageId,
                            MenuType = x.MenuType,
                            ThumbnailId = x.ThumbnailId,
                            Title = x.Title,
                            ThumbnailURL = x.Thumbnail.PublicUrl,
                            Slug = GetSlug(x),// x.Type == GeneralEnums.EMenuTypes.Page ? x.Page.Slug : x.Slug,
                            TemplateId = x.TemplateId,
                            SiteId = x.SiteId,
                            Status = x.Status,
                            ExternalSlug = x.ExternalSlug,
                            CategoriesId = x.MenuGroup.CategoryId ?? 0,
                            Order = x.Order,
                            //   Content = x.MenuSettings.Settings,
                            //   AddtionalMetaTags = x.MenuSettings.AddtionalMetaTags,
                        }
                         ).ToList();
                    if (Menus.Count > 0)
                    {
                        foreach (var item in Menus)
                        {
                            item.Childs = GetChilds(item.Id, status);
                        }
                    }
                    else
                        Menus = new List<MenuViewModel>();
                    return Menus;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        public async Task<long> AddMenu(MenuViewModel menu)
        {
            try
            {
                if (menu.Order == 0)
                {
                    var maximumOrder = await _MenuRepository.Query()
                        .Include(x => x.MenuGroup)
                        .Where(x => x.ParentId == menu.ParentId && x.SiteId == menu.SiteId
                        && x.MenuGroup.CategoryId == menu.CategoriesId
                        && x.Status != GeneralEnums.EStatus.Deleted)
                        .MaxAsync(y => y.Order);

                    if (maximumOrder != null)
                    {
                        menu.Order = maximumOrder + 1;
                    }
                    else
                    {
                        menu.Order = 1;
                    }
                }

                var m = new Menu();

                m.Name = menu.Name;
                m.Icon = menu.Icon;
                m.PageId = menu.PageId;
                m.MenuType = menu.MenuType;
                m.Title = menu.Title;
                m.ThumbnailId = menu.ThumbnailId;
                m.ExternalSlug = menu.ExternalSlug;
                m.CreatedById = menu.UserId;
                m.SiteId = menu.SiteId;
                m.Status = EStatus.Unpublished;
                m.MenuGroupId = menu.MenuGroupId;
                m.ParentId = menu.ParentId;
                m.TemplateId = menu.TemplateId;
                m.Order = menu.Order;
                switch (menu.MenuType)
                {

                    case EMenuTypes.Page:
                        m.PageId = menu.SlugTargetId;
                        break;
                    case EMenuTypes.Post:
                        m.PostId = menu.SlugTargetId;
                        break;
                    case EMenuTypes.Product:
                        m.ProductId = menu.SlugTargetId;
                        break;
                }

                _MenuRepository.Add(m);
                await _MenuRepository.SaveChangesAsync();

                return m.Id;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }



        public async Task<bool> AddEditMenu(List<MenuViewModel> menus, long SiteId, long UserId)
        {
            using (var scope = _Context.Database.BeginTransaction())
            {
                try
                {
                    menus = menus.OrderByDescending(m => m.Id).ToList();




                    foreach (var menu in menus)
                    {

                        Menu m =  _MenuRepository.Query().Include(x => x.MenuGroup).Where(y => y.Id == menu.Id).FirstOrDefault();
                       if(m == null)
                        {
                            continue;
                        }

                        m.MenuType = EMenuTypes.Page;
                        m.PageId = menu.SlugTargetId;

                    }


                    await _MenuRepository.SaveChangesAsync();

                    await scope.CommitAsync();


                    return true;

                }
                catch (Exception exc)
                {
                    await scope.RollbackAsync();
                    throw exc;
                }
            }
        }

        public async Task<bool> EditMenu(int userId, long menuId, MenuViewModel EditedMenu)
        {
            try
            {
                var Menu = _MenuRepository.Query().Where(y => y.Id == menuId).FirstOrDefault();

                Menu.Name = EditedMenu.Name;
                Menu.Icon = EditedMenu.Icon;
                Menu.PageId = EditedMenu.PageId;

                Menu.Title = EditedMenu.Title;
                Menu.ThumbnailId = EditedMenu.ThumbnailId;
                Menu.ExternalSlug = EditedMenu.ExternalSlug;
                Menu.ModifiedById = userId;
                Menu.MenuGroupId = EditedMenu.MenuGroupId;
                Menu.ModificationDate = DateTimeOffset.Now;
                Menu.TemplateId = EditedMenu.TemplateId;
                Menu.ParentId = EditedMenu.ParentId == 0? null : EditedMenu.ParentId;
                Menu.MenuType = EditedMenu.MenuType;
                Menu.Order = EditedMenu.Order;
                switch (EditedMenu.MenuType)
                {

                    case EMenuTypes.Page:
                        Menu.PageId = EditedMenu.SlugTargetId;
                        break;
                    case EMenuTypes.Post:
                        Menu.PostId = EditedMenu.SlugTargetId;
                        break;
                    case EMenuTypes.Product:
                        Menu.ProductId = EditedMenu.SlugTargetId;
                        break;
                }
                await _MenuRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> EditMenuStatus(int userId, long id, int statusId)
        {
            try
            {
                if((EStatus)statusId == EStatus.Deleted)
                {
                    await DeleteMenu(userId, id);
                }
                else
                {
                    var menu = _MenuRepository.Query().Where(y => y.Id == id).FirstOrDefault();
                    menu.Status = (EStatus)statusId;
                    menu.ModifiedById = userId;
                    _MenuRepository.SaveChanges();

                }

                
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<bool> publishAllMenus(publishMenuViewModel menuObj, long UserId, long SiteId)
        {
            try
            {
                foreach (long MenuId in menuObj.MenuIds)
                {
                    var menuList = _MenuRepository.Query().Where(x => x.Id == MenuId && x.SiteId == SiteId).Select(x => x).FirstOrDefault();
                    menuList.Status = menuObj.status;
                    menuList.ModifiedById = UserId;
                }
                await _MenuRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        //end bulk update,delete,publish

        public async Task<bool> DeleteMenu(long userId, long menuId)
        {
            try
            {
                var MenuList = await _MenuRepository.Query().Where(y => y.Id == menuId || y.ParentId == menuId).ToListAsync();
                var SelectedMenu = MenuList.FirstOrDefault(y => y.Id == menuId);
                SelectedMenu.Status = GeneralEnums.EStatus.Deleted;
                SelectedMenu.ModifiedById = userId;
                SelectedMenu.ModificationDate = DateTimeOffset.Now;
                _MenuRepository.Update(SelectedMenu);


                foreach (var MenuItem in MenuList)
                {
                    MenuItem.ParentId = SelectedMenu.ParentId;
                    _MenuRepository.Update(MenuItem);
                }
                await _MenuRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<long> DuplicateMenuCategory(MenuViewModel menu)
        {
            var oldId = menu.Id;
            var NewCat = await _CategoriesService.GetCategoriesById(menu.Id);
            NewCat.Id = 0;
            NewCat.Name = menu.Name;
            NewCat.UserId = menu.UserId;
            var newCatId = await _CategoriesService.AddCategory(NewCat);// await AddMenuCategory(siteId, userId, menu.Name);
            var copiedMenu = await GetMenusByCategoryId(oldId, GeneralEnums.EStatus.All, menu.UserId, GeneralEnums.SourceMenuType.Admin);

            if (copiedMenu != null && copiedMenu.Count > 0)
            {
                foreach (var item in copiedMenu)
                {
                    var m = item;
                    m.CategoriesId = newCatId;
                    m.UserId = menu.UserId;
                    var newMenu = await AddMenu(m);
                    if (item.Childs != null && item.Childs.Count > 0)
                    {
                        await addChildesMenuAsync(menu.SiteId, menu.UserId, item, newMenu);

                    }
                }
            }

            return newCatId;
        }

        private async Task addChildesMenuAsync(long siteId, long userId, MenuViewModel m, long parentId)
        {

            if (m.Childs != null && m.Childs.Count > 0)
            {
                foreach (var item in m.Childs)
                {
                    item.UserId = userId;
                    item.ParentId = parentId;
                    var newMenu = await AddMenu(item);
                    var Menu = await GetMenuById(newMenu);
                    Menu.ParentId = parentId;
                    if (item.Childs.Count > 0)
                    {
                        await addChildesMenuAsync(siteId, userId, item, Menu.Id);
                    }
                }
            }


        }



        //added by Mahmoud to save order of the menu

        public void menuSwapHandler(List<Menu> menuList)
        {
            long? tempparentid = menuList[0].ParentId;
            long? temporder = (menuList[0].Order != null) ? menuList[0].Order : menuList[0].Id;
            menuList[0].ParentId = menuList[1].ParentId;
            menuList[0].Order = (menuList[1].Order != null) ? menuList[1].Order : menuList[1].Id;
            menuList[1].ParentId = tempparentid;
            menuList[1].Order = temporder;
        }

        public void moveMenu(List<Menu> menuList, MenuOrderViewModel menuOrder)
        {
            var sourceItem = menuList.Find(x => x.Id == menuOrder.sourceID);

            sourceItem.ParentId = menuOrder.destinationParentId;
            sourceItem.Order = menuOrder.newOrder;
        }

        public async Task<bool> SwapAndAddMenu(MenuOrderViewModel menuOrder, long siteId)
        {
            try
            {

                var result = false;
                var menuList = _MenuRepository.Query().Where(y => y.Id == menuOrder.sourceID || y.Id == menuOrder.destinationID).ToList();
                if (menuOrder.sourceParentId == 0) menuOrder.sourceParentId = null;
                if (menuOrder.destinationParentId == 0) menuOrder.destinationParentId = null;
                if (menuList.Count > 0)
                {
                    if (menuOrder.type == "swap")
                    {
                        List<Menu> childList = new List<Menu>();
                        childList = _MenuRepository.Query()
                                                    .Include(x => x.MenuGroup)
                                                    .Where(y => y.ParentId == menuOrder.destinationParentId
                                                                && y.Status != GeneralEnums.EStatus.Deleted
                                                                && menuOrder.categoryId == y.MenuGroup.CategoryId
                                                                && siteId == y.SiteId)
                                                    .OrderBy(x => x.Order)
                                                    .ToList<Menu>();

                        if (childList != null)
                        {
                            var destIndex = childList.FindIndex(x => x.Id == menuOrder.destinationID);
                            var sourceIndex = childList.FindIndex(x => x.Id == menuOrder.sourceID);

                            if (destIndex != -1 && sourceIndex != -1 && menuOrder.destinationParentId == menuOrder.sourceParentId && destIndex - sourceIndex == 1)
                            {
                                menuSwapHandler(menuList);
                            }
                            else
                            {
                                for (int i = destIndex; i < childList.Count; i++)
                                {
                                    if (i == destIndex)
                                    {
                                        var menuitem = menuList.Find(item => item.Id == menuOrder.sourceID);
                                        var destItem = childList[destIndex];
                                        var firstItem = childList.FirstOrDefault();
                                        var previousItem = childList.ElementAtOrDefault(i - 1);

                                        menuitem.ParentId = destItem.ParentId;

                                        if ((firstItem.Id == destItem.Id && childList[destIndex].Order != 1) || (firstItem.Id != destItem.Id && childList[destIndex].Order - 1 > previousItem.Order))
                                            menuitem.Order = destItem.Order - 1;
                                        else
                                        {
                                            menuitem.Order = destItem.Order;
                                            destItem.Order = destItem.Order + 1;
                                        }

                                    }
                                    else
                                    {
                                        var previousItem = childList.ElementAtOrDefault(i - 1);
                                        if (previousItem != null && childList[i].Order == previousItem.Order)
                                        {
                                            childList[i].Order = childList[i].Order + 1;
                                        }

                                    }
                                }
                            }
                        }
                    }
                    else if (menuOrder.type == "add")
                    {
                        var sourceItem = menuList.Find(item => item.Id == menuOrder.sourceID);
                        sourceItem.ParentId = menuOrder.destinationID;
                        sourceItem.Order = menuOrder.newOrder;
                    }
                    await _MenuRepository.SaveChangesAsync();
                    result = true;
                }
                else
                {
                    result = false;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<Menu>> GetMenuByParentPageId(long ParentPageId)
        {
            var RelatedPagesIds = await _pageRepository.Query()
                .Where(p => p.ParentId == ParentPageId && p.ParentId != null).Select(s => s.Id).ToListAsync();

            return await _MenuRepository.Query()
                .Where(m => m.PageId.HasValue && RelatedPagesIds.Contains(m.PageId.Value))
                .Include(m => m.MenuGroup)
                .ToListAsync();


        }




        public async Task<List<Menu>> ChangeMenuSort(MenuGroupWithSortVM Model)
        {
            using (var transaction = _MenuRepository.BeginTransaction())
            {
                try
                {
                    var MenusList = await _MenuRepository
                                .Query()
                                .Where(m => m.MenuGroupId == Model.MenuGroupId && m.Status != EStatus.Deleted)
                                .ToListAsync();

                    foreach (var menu in MenusList)
                    {
                        var selectedMenu = Model.SortedList.Where(i => i.Id == menu.Id).FirstOrDefault();
                        if(selectedMenu == null)
                        {
                            continue;
                        }
                        menu.Order = selectedMenu.Order;
                        menu.ParentId = selectedMenu.ParentId == 0 ? null : selectedMenu.ParentId;
                    }


                    await _MenuRepository.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return MenusList.OrderBy(m => m.Order).ToList();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw e;
                }
            }

        }

        public async Task<List<MenuViewModel>> GetMenusByGroupId(long CatId)
        {
            try
            {
                long SiteId = await _workContext.GetCurrentSiteIdAsync();
                var CurrentUserId = _workContext.GetCurrentUserId().Result;

                List<Menu> Query = new List<Menu>(); // for top level only
                List<Menu> AllSubMenus = new List<Menu>();

                List<MenuViewModel> MenuItems = new List<MenuViewModel>();
                var RoleObj = _UserRoleService.GetRoleByUserId(CurrentUserId).Result;
                List<long> userRoleIds = new List<long>();
                if (RoleObj != null)
                {
                    userRoleIds = RoleObj.Select(x => x.RoleId).ToList();
                }
                // If normal user --> get authorized and all public menu for this category
                Query = _MenuRepository.Query()
                       .Include(x => x.MenuGroup)
                      .Where(x => x.SiteId == SiteId
                        && x.MenuGroup.Id == CatId
                        && x.Status == GeneralEnums.EStatus.Published
                        && (x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)) || x.MenuRoles.Count == 0)
                        && x.ParentId == null
                        )
                       .Include(x => x.Parent)
                       .Include(x => x.Page)
                       .Include(x => x.MenuRoles).ToList();



                AllSubMenus = _MenuRepository.Query()
                      .Include(x => x.MenuGroup)
                      .Where(x => x.SiteId == SiteId
                        && x.MenuGroup.Id == CatId
                        && x.Status == GeneralEnums.EStatus.Published
                        && (x.MenuRoles.Any(y => userRoleIds.Contains(y.RoleId)) || x.MenuRoles.Count == 0)
                        && x.ParentId != null
                        )
                       .Include(x => x.Parent)
                       .Include(x => x.Page)
                       .Include(x => x.MenuRoles)
                       .Include(x => x.MenuGroup)
                       .ToList();


                MenuItems = Query.Select(x =>
                                  new MenuViewModel
                                  {
                                      Id = x.Id,
                                      Name = x.Name,
                                      Icon = x.Icon,
                                      PageId = x.PageId,
                                      MenuType = x.MenuType,
                                      ThumbnailURL = x.Thumbnail?.PublicUrl,
                                      Title = x.Title,
                                      //  Image = x.Image,
                                      Slug = x.MenuType == GeneralEnums.EMenuTypes.Page ? x.Page.Slug : x.ExternalSlug,
                                      TemplateId = x.TemplateId,
                                      SiteId = x.SiteId,
                                      Status = x.Status,
                                      CategoriesId = x.MenuGroup.CategoryId ?? 0,
                                      Order = x.Order,
                                      Childs = GetChildsForWeb(x.Id, AllSubMenus),
                                      ParentId = x.ParentId,
                                      ExternalSlug=x.ExternalSlug,
                                      
                                      //Content = query.
                                      // AddtionalMetaTags = _MenuSettingsRepository.Query().Where(y => y.MenuId == x.Id).Select(y => y.AddtionalMetaTags).FirstOrDefault(),
                                  }
                                 ).OrderBy(x => x.Order).Distinct().ToList();

                //foreach (var item in MenuItems)
                //{
                //    item.Childs = GetChildsForWeb(item.Id);
                //}
                return MenuItems;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }



        #region MenuGroups

        public async Task<List<MenuGroupVM>> GetMenuGroupsList(long SiteId, long CategoryId = 0)
        {
            var MenuGroupsList = await _MenuGroupRepository
                .Query()
                .Where(m => m.SiteId == SiteId
                            && m.Status != EStatus.Deleted
                            && (CategoryId == 0 || m.CategoryId == CategoryId))
                .Include(x => x.Menus)
                .Include("Menus.MenuRoles")
                .Include("Menus.Thumbnail")
                .Include("Menus.Page")
                .Include(m => m.Category)
                .ThenInclude(c => c.Culture)
                .Select(m => new MenuGroupVM
                {
                    Category = m.Category,
                    CultureCode = m.Category.Culture.CultureId,
                    Id = m.Id,
                    Name = m.Name,
                    SiteId = m.SiteId,
                    MenuItemsCount = m.Menus.Where(m=>m.Status!=EStatus.Deleted).ToList().Count,
                    CategoryId = m.CategoryId.HasValue ? m.CategoryId.Value : 0,
                    Status = m.Status,
                    Menus = m.Menus.Where(m => m.Status != EStatus.Deleted).ToList()

                }).ToListAsync();

            return MenuGroupsList;
        }

        public async Task<MenuGroupVM> GetMenuGroupById(long GroupId)
        {
            var MenuGroupsItem = await _MenuGroupRepository
                .Query()
                .Where(m => m.Id == GroupId)
                .Include(m => m.Menus)
                .Include("Menus.MenuRoles")
                .Include("Menus.Thumbnail")
                .Include(m => m.Category)
                .ThenInclude(s => s.Culture)
                .Select(m => new MenuGroupVM
                {
                    Category = m.Category,
                    CultureCode = m.Category.Culture.CultureId,
                    Id = m.Id,
                    Name = m.Name,
                    SiteId = m.SiteId,
                    MenuItemsCount = m.Menus.Count,
                    CategoryId = m.CategoryId.HasValue ? m.CategoryId.Value : 0,
                    Status = m.Status,
                    Menus = m.Menus.ToList()
                }).FirstOrDefaultAsync();

            return MenuGroupsItem;
        }


        public async Task<MenuGroupVM> UpsertMenuGroup(UpsertMenuGroupVM Model, long SiteId, long UserId)
        {
            bool IsEditMood = Model.Id != 0;
            MenuGroup GroupItem = new MenuGroup();
            if (IsEditMood)
            {
                GroupItem = await _MenuGroupRepository
                               .Query()
                               .Where(m => m.Id == Model.Id)
                               .FirstOrDefaultAsync();
            }


            GroupItem.Name = Model.Name;
            GroupItem.CategoryId = Model.CategoryId;

            if (!IsEditMood)
            {
                GroupItem.Status = EStatus.Published;
                GroupItem.SiteId = SiteId;
                GroupItem.CreatedById = UserId;
                GroupItem.CreationDate = DateTime.Now;

                _MenuGroupRepository.Add(GroupItem);
            }
            else
            {
                GroupItem.ModificationDate = DateTime.Now;
                GroupItem.ModifiedById = UserId;

            }

            _MenuGroupRepository.SaveChanges();


            return await GetMenuGroupById(GroupItem.Id);
        }


        public async Task<MenuGroupVM> CloneMenuGroup(CloneMenuGroupVM model, long UserId, long SiteId)
        {
            using (var transaction = _Context.Database.BeginTransaction())
            {
                try
                {
                    var SourceGroup = await _MenuGroupRepository
                      .Query()
                      .Where(g => g.Id == model.SourceGroupId)
                      .Include(g => g.Menus)
                      .ThenInclude(m => m.MenuRoles)

                    .Include(m => m.Category)
                    .ThenInclude(s => s.Culture)
                      .FirstOrDefaultAsync();

                    MenuGroup newGroup = new MenuGroup()
                    {
                        CategoryId = SourceGroup.CategoryId,
                        CreatedById = UserId,
                        CreationDate = DateTime.Now,
                        SiteId = SiteId,
                        Name = model.Name,
                        Status = EStatus.Published

                    };

                    _MenuGroupRepository.Add(newGroup);
                    await _MenuGroupRepository.SaveChangesAsync();

                    await CloneMenuItemsAndRoles(newGroup, SourceGroup, UserId, SiteId);



                    await transaction.CommitAsync();

                    return await GetMenuGroupById(newGroup.Id);
                }
                catch (Exception E)
                {
                    await transaction.RollbackAsync();

                    throw E;
                }
            }

        }

        public async Task<Menu> AddMenuItemToGroup(AddMenuItemToGroupVM Model, long UserId)
        {

            var MenuItem = await _MenuRepository
                .Query()
                .Where(m => m.Id == Model.MenuItemId)
                .FirstOrDefaultAsync();

            MenuItem.ModificationDate = DateTime.Now;
            MenuItem.ModifiedById = UserId;
            MenuItem.MenuGroupId = Model.MenuGroupId;

            await _MenuGroupRepository.SaveChangesAsync();



            return MenuItem;
        }


        public async Task<MenuGroup> UpdateMenuGroupStatus(long GroupId, EStatus NewStatus, long UserId)
        {

            MenuGroup GroupItem = await _MenuGroupRepository
                                .Query()
                                .Where(m => m.Id == GroupId)
                                .Include(m => m.Menus)
                                .FirstOrDefaultAsync();

            if (GroupItem.Menus.Count() > 0 && NewStatus == EStatus.Deleted)
            {
                throw new Exception("Could Not Delete , this Group Has Menu Items Attached To It");
            }

            GroupItem.ModificationDate = DateTime.Now;
            GroupItem.ModifiedById = UserId;
            GroupItem.Status = NewStatus;
            await _MenuGroupRepository.SaveChangesAsync();



            return GroupItem;
        }

        #endregion


        #region HelperFunctions

        private async Task<MenuGroup> CloneMenuItemsAndRoles(MenuGroup newGroup, MenuGroup SourceGroup, long UserId, long SiteId)
        {
            List<KeyValuePair<Menu, Menu>> MenuMapper = new List<KeyValuePair<Menu, Menu>>(); // < Source , NewMenu >


            foreach (var MenuItem in SourceGroup.Menus)
            {
                Menu NewMenuItem = new Menu()
                {
                    CreatedById = UserId,
                    CreationDate = DateTime.Now,
                    Description = MenuItem.Description,
                    ExternalSlug = MenuItem.ExternalSlug,
                    Icon = MenuItem.Icon,
                    IsLoginRequired = MenuItem.IsLoginRequired,
                    MenuGroupId = newGroup.Id,
                    MenuType = MenuItem.MenuType,
                    Name = MenuItem.Name,
                    Title = MenuItem.Title,
                    ThumbnailId = MenuItem.ThumbnailId,
                    TemplateId = MenuItem.TemplateId,
                    Status = MenuItem.Status,
                    ProductId = MenuItem.ProductId,
                    PageId = MenuItem.PageId,
                    Order = MenuItem.Order,
                    PostId = MenuItem.PostId,
                    SiteId = SiteId,
                    ParentId = null, // ParentId will Be Null now but Menu Mapper Will Take care of it
                };

                _MenuRepository.Add(NewMenuItem);
                await _MenuRepository.SaveChangesAsync();


                NewMenuItem.MenuRoles = await CloneMenuRoles(MenuItem.MenuRoles.ToList(), NewMenuItem.Id, UserId);

                MenuMapper.Add(new KeyValuePair<Menu, Menu>(MenuItem, NewMenuItem));
            }

            foreach (var MapperItem in MenuMapper)
            {
                if (MapperItem.Key.ParentId == null)
                {
                    MapperItem.Value.ParentId = null;
                    continue;
                }

                var Parent = MenuMapper.Where(x => x.Key.Id == MapperItem.Key.ParentId).FirstOrDefault();
                MapperItem.Value.ParentId = Parent.Value.Id;

            }

            await _MenuRepository.SaveChangesAsync();

            return newGroup;
        }


        private async Task<List<MenusRoles>> CloneMenuRoles(List<MenusRoles> SourceRoles, long NewMenuItemId, long UserId)
        {
            List<MenusRoles> NewMenuRoles = new List<MenusRoles>();

            foreach (var RoleItem in SourceRoles)
            {
                MenusRoles NewRoleItem = new MenusRoles()
                {
                    RoleId = RoleItem.RoleId,
                    MenuId = NewMenuItemId,
                    CreationDate = DateTime.Now,
                    CreatedById = UserId

                };

                NewMenuRoles.Add(NewRoleItem);

            }

            _MenuRolesRepository.AddRange(NewMenuRoles);
            await _MenuRolesRepository.SaveChangesAsync();

            return NewMenuRoles;
        }
        #endregion
    }
}
