using EskaCMS.Core.Entities;
using EskaCMS.Core.Entities.Localization;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public interface ICurrencyService
    {
        public SiteSettings GetDefaultCurrencyCulture();
        Task<CurrencyRates> GetCurrentUserCurrency(long SiteId = 0);
        public string GetDefaultDecimalPlaces(long siteId);

        public string FormatCurrency(decimal value);
        public string FormatCurrency(decimal value, string CurrCulture);

        Task<string> FormatCurrency(decimal value, string decimalPlace, User CurrentUser, string OrderCurrency, SiteSettings CurrencyCulture);

        decimal ConvertToDefault(decimal value);
        string GetUserCurrencyCode();
        string GetDefaultCurrencyCode();
    }
}
