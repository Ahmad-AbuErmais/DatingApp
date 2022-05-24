
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Entities;
using EskaCMS.Core.Enums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public class CategoriesService : ICategories
    {
        private readonly IRepository<Category> _categoriesRepository;
        private readonly IWorkContext _workContext;

        public CategoriesService(IRepository<Category> categoriesRepository,
                        IWorkContext workContext)
        {
            _categoriesRepository = categoriesRepository;
            _workContext = workContext;

        }
        public async Task<long> AddCategory(CategoryViewModel category)
        {
            try
            {
                if (category.Id == 0)
                {
                    Category newObj = new Category();
                    newObj.CreationDate = DateTimeOffset.Now;
                    newObj.CreatedById = category.UserId;
                    newObj.Name = category.Name;
                    newObj.CategoryTypeId = (long)category.CategoryTypeId;
                    newObj.Description = category.Description;
                    newObj.Slug = category.Name;
                    newObj.Status = EStatus.Active;
                    newObj.StatusDate = DateTimeOffset.Now;
                    newObj.IncludeInMenu = category.IncludeInMenu;
                    newObj.IsPublished = category.IsPublished;
                    newObj.MetaDescription = category.MetaDescription;
                    newObj.MetaKeywords = category.MetaKeywords;
                    newObj.MetaTitle = category.MetaTitle;
                    newObj.ParentId = category.ParentId;
                    newObj.ImageUrl = category.ImageUrl;
                    newObj.ThumbnailImageId = category.ThumbnailImageId;
                    newObj.SiteId = category.SiteId;
                    newObj.DisplayOrder = category.DisplayOrder;
                    newObj.ParentId = category.ParentId != 0 ? category.ParentId : null;
                    newObj.CultureId = category.CultureId != 0 ? category.CultureId : null;
                    _categoriesRepository.Add(newObj);
                    await _categoriesRepository.SaveChangesAsync();
                    return newObj.Id;
                }

                else

                {
                    var updateObj = await _categoriesRepository.Query().Where(x => x.Id == category.Id).FirstOrDefaultAsync();
                    updateObj.CategoryTypeId = (long)category.CategoryTypeId;
                    updateObj.ModificationDate = DateTimeOffset.Now;
                    updateObj.ModifiedById = category.UserId;
                    updateObj.Name = category.Name;
                    updateObj.Description = category.Description;
                    updateObj.Slug = category.Slug;
                    updateObj.Status = category.Status;
                    updateObj.StatusDate = DateTimeOffset.Now;
                    updateObj.IncludeInMenu = category.IncludeInMenu;
                    updateObj.IsPublished = category.IsPublished;
                    updateObj.MetaDescription = category.MetaDescription;
                    updateObj.MetaKeywords = category.MetaKeywords;
                    updateObj.MetaTitle = category.MetaTitle;
                    updateObj.ParentId = category.ParentId;
                    updateObj.ImageUrl = category.ImageUrl;
                    updateObj.SiteId = category.SiteId;
                    updateObj.DisplayOrder = category.DisplayOrder;
                    updateObj.ParentId = category.ParentId != 0 ? category.ParentId : null;
                    updateObj.CultureId = category.CultureId;
                    _categoriesRepository.Update(updateObj);
                    await _categoriesRepository.SaveChangesAsync();
                }


                return 0;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }


        public async Task<CategoryViewModel> GetCategoriesById(long CategoryId)
        {
            try
            {
                return await _categoriesRepository.Query().Where(x => x.Id == CategoryId).Select(p => new CategoryViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CategoryTypeId = (GeneralEnums.ECategoryTypes)p.CategoryTypeId,
                    DisplayOrder = p.DisplayOrder,
                    IncludeInMenu = p.IncludeInMenu,
                    IsPublished = p.IsPublished,
                    MetaDescription = p.MetaDescription,
                    MetaKeywords = p.MetaKeywords,
                    MetaTitle = p.MetaTitle,
                    ParentId = p.ParentId,
                    Slug = p.Slug,
                    Status = p.Status,
                    ThumbnailImageId = p.ThumbnailImageId,
                    ImageUrl = p.ImageUrl,
                    CultureId = p.CultureId.HasValue ? p.CultureId.Value : 0
                }).FirstOrDefaultAsync();

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<List<CategoryViewModel>> GetCategoriesByTypeId(GeneralEnums.ECategoryTypes CategoryTypeId, long? SiteId, long? ParentId, bool WithShared = false)
        {
            try
            {
                return await _categoriesRepository.Query().Where(x => x.CategoryTypeId == (long)CategoryTypeId
               && ((x.ParentId == ParentId) || ParentId == 0)
               && (x.SiteId == SiteId || SiteId == 0 || WithShared)
               && x.Status != GeneralEnums.EStatus.Deleted).Select(p => new CategoryViewModel
               {
                   Id = p.Id,
                   Name = p.Name,
                   Description = p.Description,
                   CategoryTypeId = (GeneralEnums.ECategoryTypes)p.CategoryTypeId,
                   DisplayOrder = p.DisplayOrder,
                   IncludeInMenu = p.IncludeInMenu,
                   IsPublished = p.IsPublished,
                   MetaDescription = p.Description,
                   MetaKeywords = p.MetaKeywords,
                   MetaTitle = p.MetaTitle,
                   ParentId = p.ParentId,
                   Slug = p.Slug,
                   SiteId = p.SiteId,
                   Status = p.Status,
                   CreatedById = p.CreatedById,
                   ThumbnailImageId = p.ThumbnailImageId,
                   CultureId = p.CultureId.HasValue ? p.CultureId.Value : 0
               }).ToListAsync();

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }



        public async Task<bool> ChangeCategoryStatus(long CatId, long ModifiedById, GeneralEnums.EStatus Status)
        {
            try
            {
                var cat = await _categoriesRepository.Query().Where(x => x.Id == CatId).FirstOrDefaultAsync();
                cat.Status = Status;
                cat.ModifiedById = ModifiedById;
                cat.ModificationDate = DateTimeOffset.Now;
                await _categoriesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<CategoryViewModel>> GetCategoriesByIds(List<long> ids, long? SiteId)
        {
            var gridData = await _categoriesRepository.Query().Where(x => x.Status == GeneralEnums.EStatus.Published && x.SiteId == SiteId && x.CategoryTypeId == (long)ECategory.Ecommerce).Select(p => new CategoryViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryTypeId = (GeneralEnums.ECategoryTypes)p.CategoryTypeId,
                DisplayOrder = p.DisplayOrder,
                IncludeInMenu = p.IncludeInMenu,
                IsPublished = p.IsPublished,
                MetaDescription = p.Description,
                MetaKeywords = p.MetaKeywords,
                MetaTitle = p.MetaTitle,
                ParentId = p.ParentId,
                Slug = p.Slug,
                Status = p.Status,
                ThumbnailImageId = p.ThumbnailImageId,
                ImageUrl = p.ImageUrl,
                CultureId = p.CultureId.HasValue ? p.CultureId.Value : 0
            }).ToListAsync();

            if (ids.Count > 0)
            {
                var FilteredCategories = new List<CategoryViewModel>();
                foreach (var item in gridData)
                {
                    foreach (var id in ids)
                    {
                        if (item.Id == id)
                        {
                            FilteredCategories.Add(item);
                        }
                    }

                }
                return FilteredCategories;
            }
            else
                return gridData;
        }



        public async Task<List<CategoryViewModel>> GetAllCategories(long SiteId)
        {
            var gridData = await _categoriesRepository.Query().Where(x => x.Status != GeneralEnums.EStatus.Deleted && x.SiteId == SiteId).Select(p => new CategoryViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryTypeId = (GeneralEnums.ECategoryTypes)p.CategoryTypeId,
                DisplayOrder = p.DisplayOrder,
                IncludeInMenu = p.IncludeInMenu,
                IsPublished = p.IsPublished,
                MetaDescription = p.Description,
                MetaKeywords = p.MetaKeywords,
                MetaTitle = p.MetaTitle,
                ParentId = p.ParentId,
                Slug = p.Slug,
                Status = p.Status,
                ThumbnailImageId = p.ThumbnailImageId,
                ImageUrl = p.ImageUrl,
                CultureId = p.CultureId.HasValue ? p.CultureId.Value : 0
            }).ToListAsync();

            return gridData;
        }
        public SmartTableResult<CategoryViewModel> GetCategoriesbySiteId(SmartTableParam param, long SiteId)
        {
            try
            {

                var query = _categoriesRepository.Query().Include(x => x.Culture).Where(x => x.SiteId == SiteId && x.Status != GeneralEnums.EStatus.Deleted);

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.Name != null)
                    {
                        string name = search.Name;
                        query = query.Where(x => x.Name.ToLower().Contains(name.ToLower()));
                    }

                    if (search.CultureId != null)
                    {
                        long cultureId = search.CultureId != "" ? search.CultureId : 0;
                        if (cultureId != 0)
                            query = query.Where(x => x.CultureId == cultureId);
                    }
                    if (search.CategoryTypeId != null)
                    {
                        long categorytypeId = search.CategoryTypeId != "" ? search.CategoryTypeId : 0;
                        if (categorytypeId != 0)
                            query = query.Where(x => x.CategoryTypeId == categorytypeId);
                    }

                }

                var gridData = query.ToSmartTableResult(
                    param,
                    x => new CategoryViewModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        CategoryTypeId = (GeneralEnums.ECategoryTypes)x.CategoryTypeId,
                        CategoryType = x.CategoryType.Name,
                        CultureId = x.CultureId.HasValue ? x.CultureId.Value : 0,
                        CultureName =x.Culture.Culture.Name,
                        Slug=x.Slug,
                        Status=x.Status

                    });



                return gridData;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
