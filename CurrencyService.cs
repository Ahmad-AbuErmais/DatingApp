using EskaCMS.Core.Entities;
using EskaCMS.Core.Entities.Localization;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IConfiguration _config;
        private readonly IRepository<CurrencyRates> _currencyRepository;
        private readonly IWorkContext _WorkContext;
        private readonly IRepository<User> _userrepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        
        public CurrencyService(IRepository<CurrencyRates> currencyRepository, IRepositoryWithTypedId<SiteSettings, string> appSettingRepository, IRepository<User> userrepository, IWorkContext workContext)
        {
            _appSettingRepository = appSettingRepository;
            _currencyRepository = currencyRepository;
            _userrepository = userrepository;
            _WorkContext = workContext;
        }


        public string GetDefaultDecimalPlaces(long siteId)
        {
            var decimalPlace = _appSettingRepository.Query().Where(x => x.Id.Contains("Global.CurrencyDecimalPlace") && x.SiteId == siteId).FirstOrDefault()?.Value;
            return decimalPlace;
        }


        public async  Task<CurrencyRates> GetCurrentUserCurrency(long SiteId=0)
        {
            try
            {
                var UserId = await _WorkContext.GetCurrentUserId();
                SiteId = SiteId==0 ? _WorkContext.GetCurrentSiteId(): SiteId;
               
                var CurrentUser = await _userrepository.Query()
                                                    .Include(x => x.UserSites)
                                                    .ThenInclude(x => x.Currency)
                                                    .FirstOrDefaultAsync(x => x.Id == UserId);

                var CurrentUserCurrency = CurrentUser.UserSites.FirstOrDefault(u => u.SiteId == SiteId);
                return CurrentUserCurrency == null ?null : CurrentUserCurrency.Currency;
            }
            catch(Exception E)
            {
                return null; 
            }
          
        }



        public SiteSettings GetDefaultCurrencyCulture()
        {
            var CurrencyCulture = _appSettingRepository.Query().Where(x => x.Id.Contains("Global.CurrencyCulture") && x.SiteId == _WorkContext.GetCurrentSiteId()).FirstOrDefault();
            return CurrencyCulture;
        }



        public async Task<string> FormatCurrency(decimal value, string decimalPlace, User CurrentUser, string OrderCurrency, SiteSettings CurrencyCulture)
        {
            if (CurrentUser.UserSites != null) //Check the user culture
            {
                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();
                CurrencyRates UserCurrency = CurrentUser.UserSites.FirstOrDefault(u => u.SiteId == SiteId)?.Currency;
                
                if (UserCurrency == null)
                    throw new Exception("Could not find User Currency");
                
                value = value * UserCurrency.Rate;

                var FormattedValue = value.ToString("n" + decimalPlace);

                return FormattedValue + OrderCurrency;

            }
            else
            {
                CultureInfo newCurrencyCulture = new CultureInfo(CurrencyCulture.Value.ToString());

                var CurrencyCode = new RegionInfo(newCurrencyCulture.Name).ISOCurrencySymbol;
                var FormattedValue = value.ToString("n" + decimalPlace);
                return FormattedValue + CurrencyCode;

            }
        }


        // use this when you want to render price of any thing Based On User Or Default Currency
        public string FormatCurrency(decimal value)
        {
            long siteId = _WorkContext.GetCurrentSiteId();
           
            var decimalPlace = GetDefaultDecimalPlaces(siteId);


            //Check the user culture
            var CurrentUserCurrency = GetCurrentUserCurrency().Result;

            if (CurrentUserCurrency != null) 
            {
                CultureInfo newCurrencyCulture = new CultureInfo(CurrentUserCurrency.CultureCode);

                value = value * CurrentUserCurrency.Rate;


                var CurrencyCode = new RegionInfo(CurrentUserCurrency.CultureCode).ISOCurrencySymbol;

                var FormattedValue = value.ToString("n" + decimalPlace);

                return FormattedValue + CurrencyCode;

            }
            else
            {
                var CurrencyCulture = _appSettingRepository.Query().Where(x => x.Id.Contains("Global.CurrencyCulture") && x.SiteId == _WorkContext.GetCurrentSiteId()).FirstOrDefault();
               
                CultureInfo newCurrencyCulture = new CultureInfo(CurrencyCulture.Value.ToString());
              
                var CurrencyCode = new RegionInfo(newCurrencyCulture.Name).ISOCurrencySymbol;
                
                var FormattedValue = value.ToString("n" + decimalPlace);
                
                return FormattedValue + CurrencyCode;

            }
        }



        // use this when you have the currency code
        // and the ammount is already converted to the Currency
        // ( just for REPRESENTATIONAL Perposes )
        public string FormatCurrency(decimal value, string CurrencyCode)
        {
            long siteId = _WorkContext.GetCurrentSiteId();
            var decimalPlace = GetDefaultDecimalPlaces(siteId);
            var FormattedValue = value.ToString("n" + decimalPlace);
            return FormattedValue + CurrencyCode;

        }

        public string GetUserCurrencyCode()
        {
            long siteId = _WorkContext.GetCurrentSiteId();

            var CurrentUserCurrency = GetCurrentUserCurrency().Result;

            if (CurrentUserCurrency != null) 
            {
                CultureInfo newCurrencyCulture = new CultureInfo(CurrentUserCurrency.CultureCode);
               
                var CurrencyCode = new RegionInfo(CurrentUserCurrency.CultureCode).ISOCurrencySymbol;
                return CurrencyCode;


            }
            else
            {
                return GetDefaultCurrencyCode();
            }
        }


        public string GetDefaultCurrencyCode()
        {
            var DefaultCurrency = _currencyRepository.Query().Where(c => c.IsDefaultRate).FirstOrDefault();
            if (DefaultCurrency != null)
            {
                return DefaultCurrency.Name;
            }
            else
            {
                var CurrencyCulture = GetDefaultCurrencyCulture();

                var CurrencyCode = new RegionInfo(CurrencyCulture.ToString()).ISOCurrencySymbol;
                return CurrencyCode;

            }
        }

   
        public decimal ConvertToDefault(decimal value)
        {

            var CurrentUserCurrency = GetCurrentUserCurrency().Result;
            if (CurrentUserCurrency != null)
            {
                value = value * CurrentUserCurrency.Rate;
                return value;


            }
            return value; 
        }



    }

}
