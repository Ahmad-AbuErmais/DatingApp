using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public interface ISmsSender
    {
       
        Task SendSmsAsync(SMSProviders SMSProvider, string PhoneNumber, string Message, long SiteId, string AdditionInfo = "");

     
    }
}