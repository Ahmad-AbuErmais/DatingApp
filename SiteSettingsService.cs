using EskaCMS.Core.Entities;
using EskaCMS.Core.Model;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public class SiteSettingsService : ISiteSettingsService
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingsRepository;

        public SiteSettingsService(IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository)
        {
            _siteSettingsRepository = siteSettingsRepository;
        }
        public async Task<SiteSettingsVM> GetSiteSettingById(string Id, long SiteId)
        {
            var setting = await _siteSettingsRepository.Query().Where(x => x.SiteId == SiteId && x.Id == Id).Select(x => new SiteSettingsVM
            {
                Id = x.Id,
                IsVisibleInCommonSettingPage = x.IsVisibleInCommonSettingPage,
                Module = x.Module,
                SiteId = x.SiteId,
                Value = x.Value
            }).FirstOrDefaultAsync();
            return setting;
        }
        public async Task<List<SiteSettingsVM>> GetSiteSettingsByModule(string Module, long SiteId)
        {
            var setting = await _siteSettingsRepository.Query().Where(x => x.SiteId == SiteId && x.Module == Module).Select(x => new SiteSettingsVM
            {
                Id = x.Id,
                IsVisibleInCommonSettingPage = x.IsVisibleInCommonSettingPage,
                Module = x.Module,
                SiteId = x.SiteId,
                Value = x.Value

            }).ToListAsync();
            return setting;
        }
        public async Task<List<SiteSettingsVM>> GetAllSiteSettings(long SiteId)
        {
            var setting = await _siteSettingsRepository.Query().Where(x => x.SiteId == SiteId).Select(x => new SiteSettingsVM
            {
                Id = x.Id,
                IsVisibleInCommonSettingPage = x.IsVisibleInCommonSettingPage,
                Module = x.Module,
                SiteId = x.SiteId,
                Value = x.Value

            }).ToListAsync();
            return setting;
        }

        public async Task<EmailConfig> GetEmailSmtpSettings(long SiteId,string SenderModule)
        {
            if (string.IsNullOrEmpty(SenderModule))
                SenderModule = "EmailSenderSmpt";
            var Settings = await _siteSettingsRepository.Query().Where(x => x.SiteId == SiteId && x.Module == SenderModule).ToListAsync();

            if (Settings.Count() > 0)
            {

                EmailConfig objconfig = new EmailConfig();

                objconfig.RequiresAuthentication = Convert.ToBoolean(Settings.Find(x => x.Id == "SmtpRequiresAuthentication").Value);
                objconfig.SmtpPassword = Settings.Find(x => x.Id == "SmtpPassword").Value;
                objconfig.SmtpPort = Convert.ToInt32(Settings.Find(x => x.Id == "SmtpPort").Value);
                objconfig.SmtpServer = Settings.Find(x => x.Id == "SmtpServer").Value;
                objconfig.SmtpUsername = Settings.Find(x => x.Id == "SmtpUsername").Value;
                objconfig.SenderEmail = Settings.Find(x => x.Id == "SmtpSenderEmail").Value;
                return objconfig;

            }
            else
            {
                throw new Exception("Error in Site Email Settings");
            }

        }
        public async Task<EmailConfig> GetAdminEmailSmtpSettings()
        {

            var Settings = await _siteSettingsRepository.Query().Where(x => x.SiteId == 1 &&x.Module == "EmailSenderSmpt").ToListAsync();

            if (Settings.Count() > 0)
            {

                EmailConfig objconfig = new EmailConfig();

                objconfig.RequiresAuthentication = Convert.ToBoolean(Settings.Find(x => x.Id == "SmtpRequiresAuthenticationAdmin").Value);
                objconfig.SmtpPassword = Settings.Find(x => x.Id == "SmtpPasswordAdmin").Value;
                objconfig.SmtpPort = Convert.ToInt32(Settings.Find(x => x.Id == "SmtpPortAdmin").Value);
                objconfig.SmtpServer = Settings.Find(x => x.Id == "SmtpServerAdmin").Value;
                objconfig.SmtpUsername = Settings.Find(x => x.Id == "SmtpUsernameAdmin").Value;
                objconfig.SenderEmail = Settings.Find(x => x.Id == "SmtpSenderEmailAdmin").Value;
                return objconfig;

            }
            else
            {
                throw new Exception("Error in Site Email Setting ");
            }

        }


    }
}
