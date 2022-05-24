using EskaCMS.Core.Enums;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
  public  interface ICategories
    {
        Task<long> AddCategory(CategoryViewModel category);
     
        
        Task<List<CategoryViewModel>> GetCategoriesByTypeId(GeneralEnums.ECategoryTypes CategoryTypeId, long? SiteId,long? ParentId , bool WithShared = false);
        Task<CategoryViewModel> GetCategoriesById(long CategoryId);
        Task<List<CategoryViewModel>> GetCategoriesByIds(List<long> ids, long? SiteId);
        
        Task<bool> ChangeCategoryStatus(long CatId, long ModifiedById,GeneralEnums.EStatus Status);

        Task<List<CategoryViewModel>> GetAllCategories(long SiteId);
        SmartTableResult<CategoryViewModel> GetCategoriesbySiteId(SmartTableParam param, long SiteId);


    }
}
