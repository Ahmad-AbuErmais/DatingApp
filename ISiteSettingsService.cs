using EskaCMS.Core.Model;
using EskaCMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public interface ISiteSettingsService
    {
        Task<SiteSettingsVM> GetSiteSettingById(string Id, long SiteId);
        Task<List<SiteSettingsVM>> GetSiteSettingsByModule(string Module, long SiteId);
        Task<List<SiteSettingsVM>> GetAllSiteSettings(long SiteId);
        Task<EmailConfig> GetEmailSmtpSettings(long SiteId, string SenderModule);
        Task<EmailConfig> GetAdminEmailSmtpSettings();

    }
}
