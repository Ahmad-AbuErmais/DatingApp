using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;
using EskaCMS.Core.Services;
using MailKit.Security;
using static EskaCMS.Core.Entities.BusinessModels.BusinessModel;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using System.IO;
using EskaCMS.Core.Model;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using System.Reflection;
using System;

namespace EskaCMS.EmailSender.SMTP.Services
{
    public class EmailSender : IEmailSender
    {

        private readonly string _assembly;
        private readonly IdcmsLogService _dcmsLogService;
        //private readonly IEmailTemplateService _emailTemplateService;

        private readonly ISiteSettingsService _siteSettingsService;


        public EmailSender(
            IConfiguration config,
            IdcmsLogService dcmsLogService,
            IEmailTemplateService emailTemplateService,
            ISiteSettingsService siteSettingsService

            )
        {
            _dcmsLogService = dcmsLogService;
           // _emailTemplateService = emailTemplateService;
            _assembly = Assembly.GetExecutingAssembly().GetName().Name;
            _siteSettingsService = siteSettingsService;
        }


        //public async Task SendEmailAsync(EmailSenderVM model, long SiteId)
        //{

        //    try
        //    {
        //        var _emailConfig = await _siteSettingsService.GetEmailSmtpSettings(SiteId);
        //        var emailTemplateObj = await _emailTemplateService.GetEmailTemplateById(model.TemplateId);
        //        if (model.Params.Count > 0)
        //        {
        //            foreach (var item in model.Params)
        //            {
        //                emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace(item.Param, item.Value);
        //            }
        //        }

        //        var message = new MimeMessage();
        //        var builder = new BodyBuilder();
        //        message.From.Add(new MailboxAddress(_emailConfig.SenderEmail, _emailConfig.SenderEmail));

        //        foreach (var Email in model.Emails)
        //        {
        //            message.To.Add(new MailboxAddress(Email,Email));
        //        }

        //        message.Subject = emailTemplateObj.Subject;

        //        var textFormat = emailTemplateObj.IsHTML ? TextFormat.Html : TextFormat.Plain;
        //        message.Body = new TextPart(textFormat)
        //        {
        //            Text = emailTemplateObj.EmailBody
        //        };
        //        if (model.Attachment != null)
        //        {
        //            foreach (var item in model.Attachment)
        //            {
        //                var File = System.IO.Path.Combine(Directory.GetCurrentDirectory().Trim(), item.publicUrl, item.fileName);
        //                builder.Attachments.Add(File);
        //            }
        //        }

        //        if (emailTemplateObj.IsHTML)
        //        {
        //            builder.HtmlBody = emailTemplateObj.EmailBody;
        //        }
        //        else
        //            builder.TextBody = emailTemplateObj.EmailBody;

        //        message.Body = builder.ToMessageBody();

        //        using (var client = new SmtpClient())
        //        {
        //            // Accept all SSL certificates (in case the server supports STARTTLS)
        //            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        //            await client.ConnectAsync(_emailConfig.SmtpServer, _emailConfig.SmtpPort, SecureSocketOptions.StartTls);

        //            // Note: since we don't have an OAuth2 token, disable
        //            // the XOAUTH2 authentication mechanism.
        //            client.AuthenticationMechanisms.Remove("XOAUTH2");

        //            if (_emailConfig.RequiresAuthentication)
        //                await client.AuthenticateAsync(_emailConfig.SmtpUsername, _emailConfig.SmtpPassword);


        //            await client.SendAsync(message);
        //            await client.DisconnectAsync(true);
        //            for (int i = 0; i < model.Emails.Count; i++)
        //            {
        //                await _dcmsLogService.AddInfo(_assembly, "Email has been sent", "To : " + model.Emails[i].ToString(), null);
        //            }

        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        await _dcmsLogService.AddError(_assembly, exc.Message, "Email sending failure", exc.Message, null);
        //        throw exc;
        //    }
        //}

        public async Task SendEmailAsync(List<string> Emails,string SenderModule, string subject, string body, long SiteId, bool isHtml = true, List<FileViewModel> attachment = null)
        {
            try
            {
                //  SenderModule ==> The SMTP account for the same website, since some sites may have two different account to send emails

                var _emailConfig = SiteId != 0 ? await _siteSettingsService.GetEmailSmtpSettings(SiteId, SenderModule) : await _siteSettingsService.GetAdminEmailSmtpSettings();


                var message = new MimeMessage();
                var builder = new BodyBuilder();
                message.From.Add(new MailboxAddress(_emailConfig.SenderEmail, _emailConfig.SenderEmail));
                foreach (var Email in Emails)
                {
                    message.To.Add(new MailboxAddress(Email, Email));
                }

                message.Subject = subject;

                var textFormat = isHtml ? TextFormat.Html : TextFormat.Plain;
                message.Body = new TextPart(textFormat)
                {
                    Text = body
                };
                if (attachment != null)
                {
                    foreach (var item in attachment)
                    {
                        var File = System.IO.Path.Combine(Directory.GetCurrentDirectory().Trim(), item.publicUrl, item.fileName);
                        builder.Attachments.Add(File);
                    }
                }

                if (isHtml)
                {
                    builder.HtmlBody = body;
                }
                else
                    builder.TextBody = body;

                message.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    await client.ConnectAsync(_emailConfig.SmtpServer, _emailConfig.SmtpPort, SecureSocketOptions.Auto);

                    // Note: since we don't have an OAuth2 token, disable
                    // the XOAUTH2 authentication mechanism.
                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                    if (_emailConfig.RequiresAuthentication)
                        await client.AuthenticateAsync(_emailConfig.SmtpUsername, _emailConfig.SmtpPassword);


                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                    for (int i = 0; i < Emails.Count; i++)

                    {
                        await _dcmsLogService.AddInfo(_assembly, "Email has been sent", "To : " + Emails[i].ToString(), null);
                    }


                }
            }
            catch (Exception exc)
            {

                await _dcmsLogService.AddError(_assembly, exc.Message, "Email sending failure", exc.Message, null);
                throw exc;
            }
        }
    }
}
