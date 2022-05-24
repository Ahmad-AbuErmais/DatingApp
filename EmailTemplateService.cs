using EskaCMS.Core.Entities;
using EskaCMS.Core.Enums;
using EskaCMS.Core.Models;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.EmailSender.SMTP.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {

        private readonly IRepositoryWithTypedId<EmailTemplates, string> _emailtemplatesRepository;

        private IRepository<EmailTemplateType> _emailTemplateType;

        public EmailTemplateService(IRepositoryWithTypedId<EmailTemplates, string> emailtemplatesRepository, IRepository<EmailTemplateType> emailTemplateType)
        {
            _emailtemplatesRepository = emailtemplatesRepository;
            _emailTemplateType = emailTemplateType;
        }
        public async Task<bool> AddTemplate(EmailTemplates model)
        {
            try
            {
                model.Id = model.Subject;
                model.CreationDate = DateTimeOffset.Now;
                model.Status = GeneralEnums.EStatus.Active;

                _emailtemplatesRepository.Add(model);
                await _emailtemplatesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<bool> EditTemplate(EmailTemplates model)
        {
            try
            {
                model.ModificationDate = DateTimeOffset.Now;
                model.Status = EStatus.Active;
                _emailtemplatesRepository.Update(model);
                await _emailtemplatesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<bool> DeleteTemplate(string TemplateId, long UserId, long SiteId)
        {
            try
            {
                var template = await _emailtemplatesRepository.Query().Where(x => x.Id == TemplateId && x.SiteId == SiteId).FirstOrDefaultAsync();

                template.Status = EStatus.Deleted;
                template.ModifiedById = UserId;

                _emailtemplatesRepository.Update(template);
                await _emailtemplatesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<EmailTemplateVM> GetEmailTemplateById(string Id)
        {
            try
            {
                var template = await _emailtemplatesRepository.Query().Where(x => x.Id == Id).Select(x => new EmailTemplateVM
                {
                    Id = x.Id,
                    CultureId = x.CultureId,
                    EmailTemplateTypeId = x.EmailTemplateTypeId,
                    IsHTML = x.IsHTML,
                    SiteId = x.SiteId,
                    Subject = x.Subject,
                    EmailBody = x.EmailBody


                }).FirstOrDefaultAsync();
                return template;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<EmailTemplateListOutputVM<EmailTemplateOutputVM>> GetTemplates(EmailTemplateSearchVM search)
        {
            try
            {
                List<EmailTemplates> query = await _emailtemplatesRepository.Query().Where(x => x.Status == EStatus.Active && x.SiteId == search.SiteId)
                    .Include(x => x.EmailTemplateType)
                    .ToListAsync();
                if (search.EmailTemplateSort != null)
                {
                    query = SortEmailTemplates(search.EmailTemplateSort, query);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id).ToList();
                }
                List<EmailTemplates> EmailTemplateQuery = query
                                      .Where(email =>
                                          string.IsNullOrEmpty(search.SearchValue)
                                          || email.Subject.ToUpper().Contains(search.SearchValue.ToUpper()) || email.Id.ToString().ToUpper().Contains(search.SearchValue.ToUpper())
                                          || email.Id.ToUpper().Contains(search.SearchValue.ToUpper()) || email.Id.ToString().ToUpper().Contains(search.SearchValue.ToUpper())
                                          || email.EmailTemplateType.Name.ToUpper().Contains(search.SearchValue.ToUpper()))
                                      .ToList();

                int totalCount = EmailTemplateQuery.Count;
                List<EmailTemplateOutputVM> outputList = EmailTemplateQuery.Select(email => new EmailTemplateOutputVM
                {
                    Id = email.Id,
                    Subject = email.Subject,
                    EmailType = email.EmailTemplateType.Name

                }).Skip(search.PageNo * search.PageSize).Take(search.PageSize).ToList();

                return new EmailTemplateListOutputVM<EmailTemplateOutputVM>
                {
                    List = outputList.ToList(),
                    TotalCount = totalCount
                };
            }
            catch (Exception exc)
            {

                throw exc;
            }


        }
        private List<EmailTemplates> SortEmailTemplates(EmailTemplateSearchSortVM search, List<EmailTemplates> query)
        {
            if (search.SortField == EmailTemplateSearchSortFieldsEnum.Id)
            {
                if (search.SortType == EmailTemplateSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Id).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id).ToList();
                }
            }
            if (search.SortField == EmailTemplateSearchSortFieldsEnum.Subject)
            {
                if (search.SortType == EmailTemplateSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Subject).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.Subject).ToList();
                }
            }
            if (search.SortField == EmailTemplateSearchSortFieldsEnum.EmailType)
            {
                if (search.SortType == EmailTemplateSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.EmailTemplateType.Name).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.EmailTemplateType.Name).ToList();
                }
            }

            return query;

        }
        public async Task<List<EmailTemplateTypesVM>> GetTemplateTypes()
        {
            try
            {
                var types = await _emailTemplateType.Query().Select(x => new EmailTemplateTypesVM
                {
                    Id = x.Id,
                    Name = x.Name

                }).ToListAsync();
                return types;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<EmailTemplateVM>> GetEmailTemplateByTypeId(int typeId, long SiteId)
        {
            try
            {
                var template = await _emailtemplatesRepository.Query().Where(x => x.EmailTemplateTypeId == typeId && x.SiteId == SiteId && x.Status == GeneralEnums.EStatus.Active).Select(x => new EmailTemplateVM
                {
                    Id = x.Id,
                    CultureId = x.CultureId,
                    EmailTemplateTypeId = x.EmailTemplateTypeId,
                    EmailTemplateTypes = new EmailTemplateTypesVM() { Name = x.EmailTemplateType.Name },
                    IsHTML = x.IsHTML,
                    SiteId = x.SiteId,
                    Subject = x.Subject,
                    EmailBody = x.EmailBody

                }
                ).ToListAsync();
                return template;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}
