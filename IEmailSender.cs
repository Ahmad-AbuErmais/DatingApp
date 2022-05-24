using EskaCMS.Core.Model;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using System.Collections.Generic;

using System.Threading.Tasks;


namespace EskaCMS.EmailSender.SMTP.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(List<string> Emails, string SenderModule, string subject, string body, long SiteId, bool isHtml = true, List<FileViewModel> attachment = null);
        //Task SendEmailAsync(EmailSenderVM model,long SiteId);
    }
}