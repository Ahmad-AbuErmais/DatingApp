using EskaCMS.Core.Entities;
using EskaCMS.Core.Models;
using System.Security.Claims;
using System.Threading.Tasks;


namespace EskaCMS.Core.Extensions
{
    public interface IWorkContext
    {
        string GetIPAddress();
        Task<User> GetCurrentUser();
        Task<bool> IsGuestUser();
        Task<long> GetCurrentUserId();
        Task<long> GetCurrentSiteIdAsync();
        long GetCurrentSiteId();
        long GetCurrentCultureId();
        string GetCurrentHostname();
        Task<long> GetCurrentUserCultureCode();

        Task<long> GetCurrentUserCultureId(long SiteId = 0);

        Task<UserInfoClaimsVM> GetCurrentUserInfo();
        long GetSource();
       Task UpdateAnynoumsUser(string Username, string Email, long SiteId, string PhoneNumber = null);

        Task<long> GetCurrentUserSiteId(long? UserId = null , long? SiteId = null);

        string Decrypt(string EncryptedText);
        string GetUserPublicKey();
         string EncryptString(string plainText);
    }
}
