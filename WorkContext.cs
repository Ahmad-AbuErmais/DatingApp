using EskaCMS.Core.Entities;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.Core.Extensions
{
    public class WorkContext : IWorkContext
    {

        private User _currentUser;
        private UserManager<User> _userManager;
        private readonly IRepository<UsersSites> _UsersSitesRepository;
        private HttpContext _httpContext;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public WorkContext(
         UserManager<User> userManager,
         IHttpContextAccessor contextAccessor,
         IRepository<UsersSites> UsersSitesRepository
            )
        {
            _userManager = userManager;
            _UsersSitesRepository = UsersSitesRepository;
            _httpContext = contextAccessor.HttpContext;
            _httpContextAccessor = contextAccessor;
        }

        public async Task UpdateAnynoumsUser(string Username, string Email, long SiteId, string PhoneNumber = null)
        {
            try
            {
                var CurrentUser = await GetCurrentUser();
                if (CurrentUser != null)
                {
                    if (CurrentUser.Type == EUsersTypes.GuestClient)
                    {
                                  if (!string.IsNullOrEmpty(Email))
                            CurrentUser.Email = Email;
                        if (!string.IsNullOrEmpty(PhoneNumber))
                            CurrentUser.PhoneNumber = PhoneNumber;

                        if (!string.IsNullOrEmpty(Username))
                            CurrentUser.FullName = Username;
                        await _userManager.UpdateAsync(CurrentUser);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string GetIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "";
            } 
            catch (Exception ex)  
            {
                throw ex;
                 
            }
        }


        public async Task<User> GetCurrentUser()
        {
            var contextUser = _httpContext.User;
            _currentUser = await _userManager.GetUserAsync(contextUser);
            return _currentUser;
        }


        public async Task<bool> IsGuestUser()
        {
            var contextUser = _httpContext.User;
            if (contextUser.FindFirst("UserType") != null)
            {
                var UserType = (EUsersTypes)Enum.Parse(typeof(EUsersTypes), contextUser.FindFirst("UserType").Value);
                return UserType == EUsersTypes.GuestClient;
            }
            return false;
        }

        public async Task<long> GetCurrentUserId()
        {
            var contextUser = _httpContext.User;
            if (contextUser.FindFirst(ClaimTypes.NameIdentifier) != null)
                return long.Parse(contextUser.FindFirst(ClaimTypes.NameIdentifier).Value);
            else
                return 0;
        }
        public async Task<UserInfoClaimsVM> GetCurrentUserInfo()
        {
            var contextUser = _httpContext.User;
            var UserRolearr = contextUser.FindFirst(ClaimTypes.Role).Value;
            string[] UserRoles = UserRolearr.Split(',');

            UserInfoClaimsVM UserInfo = new UserInfoClaimsVM();
            UserInfo.UserId = long.Parse(contextUser.FindFirst(ClaimTypes.NameIdentifier).Value);
            UserInfo.Email = contextUser.FindFirst(ClaimTypes.Email).Value;
            UserInfo.FullName = contextUser.FindFirst(ClaimTypes.Name).Value;
            UserInfo.MobileNumber = contextUser.FindFirst(ClaimTypes.MobilePhone).Value;
            UserInfo.RoleId = UserRoles;
            UserInfo.Username = contextUser.FindFirst("Username").Value;

            if (contextUser.FindFirst("DefaultShippingAddressId") != null)
                UserInfo.DefaultShippingAddressId = contextUser.FindFirst("DefaultShippingAddressId").Value;

            if (contextUser.FindFirst("CurrencyCultureId") != null)
                UserInfo.CurrencyCultureId = long.Parse(contextUser.FindFirst("CurrencyCultureId").Value);
            if (contextUser.FindFirst("CultureId") != null)
                UserInfo.CultureId = long.Parse(contextUser.FindFirst("CultureId").Value);

            UserInfo.UserType = (EUsersTypes)Enum.Parse(typeof(EUsersTypes), contextUser.FindFirst("UserType").Value);

            if (contextUser.FindFirst(ClaimTypes.DateOfBirth) != null)
                UserInfo.DateofBirth = DateTimeOffset.Parse(contextUser.FindFirst(ClaimTypes.DateOfBirth).Value);

            return UserInfo;

        }
        public async Task<long> GetCurrentUserCultureCode()
        {
            try
            {
                var UserId = await GetCurrentUserId();
                var SiteId = await GetCurrentSiteIdAsync();

                var UserSiteObj = _UsersSitesRepository
                                    .Query()
                                    .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                                    .Include(u => u.Culture)
                                    .FirstOrDefault();
                return UserSiteObj.CultureId.GetValueOrDefault();

            }
            catch (Exception)
            {
                return 0;
            }
        }


        public async Task<long> GetCurrentUserCultureId(long SiteId = 0)
        {
            try
            {
                var UserId = await GetCurrentUserId();
                SiteId = SiteId == 0 ? await GetCurrentSiteIdAsync() : SiteId;

                var UserSiteObj = _UsersSitesRepository
                                    .Query()
                                    .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                                    .Include(u => u.Culture)
                                    .FirstOrDefault();

                if (UserSiteObj != null && UserSiteObj.CultureId.HasValue)
                {
                    return UserSiteObj.CultureId.Value;
                }
                else
                {
                    return 0;
                }


            }
            catch (Exception)
            {
                return 0;
            }
        }
        public long GetCurrentSiteId()
        {
            try
            {
                var siteId = _httpContext.Request.Headers["siteId"];
                if (!string.IsNullOrEmpty(siteId))
                {
                    return long.Parse(siteId);
                }
                return 0;
            }
            catch (Exception exc)
            {

                return 0;
            }


        }


        public string GetUserPublicKey()
        {
            try
            {
                var UserPublicKey = _httpContext.Request.Headers["handshaking"];
                
                return UserPublicKey;
            }
            catch (Exception exc)
            {

                throw exc;
            }


        }

        
        public long GetCurrentCultureId()
        {
            try
            {

                var CultureId = _httpContext.Request.Headers["cultureId"];

                return long.Parse(CultureId);

            }
            catch (Exception E)
            {
                return 0;
            }

        }

       

        public async Task<long> GetCurrentSiteIdAsync()
        {
            try
            {
                var siteId = _httpContext.Request.Headers["siteId"];

                return long.Parse(siteId);

            }
            catch (Exception E)
            {
                return 0;
            }
        }

        public async Task<long> GetCurrentUserSiteId(long? UserId=null ,long? SiteId=null)
        {
            try
            {
                if (!UserId.HasValue)
                {
                    UserId = await GetCurrentUserId();
                }


                if (!SiteId.HasValue)
                {
                    SiteId = await GetCurrentSiteIdAsync();

                }

                var UserSiteObj = _UsersSitesRepository
                                    .Query()
                                    .Where(u => u.SiteId == SiteId && u.UserId == UserId)
                                    .FirstOrDefault();

                return UserSiteObj.Id;

            }
            catch (Exception E)
            {
                return 0;
            }
        }
        public string GetCurrentHostname()
        {
            try
            {
                var Hostname = _httpContext.Request.Host;
                if (!string.IsNullOrEmpty(Hostname.Value))
                {
                    return Hostname.ToString();
                }
                return string.Empty;
            }
            catch (Exception exc)
            {

                return string.Empty;
            }


        }

        public long GetSource()
        {
            var source = _httpContext.Request.Headers["source"];
            if (!string.IsNullOrEmpty(source))
            {
                return long.Parse(source);
            }
            return 0;
        }
        // var issuer = _configuration.GetSection("Authentication").GetSection("Jwt").GetSection("Issuer").Value;



        #region Private Fields

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int saltBytes = 32; //  bytes
        private const int ivBytes = 16; // bytes
        private const string Key = "DCMSSecretKey";
        #endregion Private Fields

        #region Public Methods

        /// <summary>Decrypts the specified cipher text.</summary>
        /// <param name="cipherText">The cipher text.</param>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <returns></returns>
        public string Decrypt(string cipherText)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [16 bytes of IV] + [n bytes of CipherText]
            byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            byte[] saltStringBytes = cipherTextBytesWithSaltAndIv.Take(saltBytes).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            byte[] ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(saltBytes).Take(ivBytes).ToArray();
            // Get the actual cipher text bytes by removing the first 48 bytes from the cipherText string.
            byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip(saltBytes + ivBytes).Take(cipherTextBytesWithSaltAndIv.Length - (saltBytes + ivBytes)).ToArray();

            using (var password = new Rfc2898DeriveBytes(Key, saltStringBytes, DerivationIterations))
            {
                byte[] keyBytes = password.GetBytes(saltBytes);

                using (var symmetricKey = new AesCryptoServiceProvider())
                {
                    symmetricKey.BlockSize = 128;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes)) using (var memoryStream = new MemoryStream(cipherTextBytes))
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        var plainTextBytes = new byte[cipherTextBytes.Length];
                        int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memoryStream.Close();
                        cryptoStream.Close();
                        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                    }
                }
            }
        }

        /// <summary>Encrypts the specified plain text.</summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <returns></returns>
        public string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            byte[] saltStringBytes = GenerateBitsOfRandomEntropy(32);
            byte[] ivStringBytes = GenerateBitsOfRandomEntropy(16);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            using (var password = new Rfc2898DeriveBytes(Key, saltStringBytes, DerivationIterations))
            {
                byte[] keyBytes = password.GetBytes(saltBytes);
                using (var symmetricKey = new AesCryptoServiceProvider())
                {
                    symmetricKey.BlockSize = 128;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();

                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                byte[] cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>Generate bits of random entropy.</summary>
        /// <returns></returns>
        private static byte[] GenerateBitsOfRandomEntropy(int num)
        {
            var randomBytes = new byte[num]; // 32 Bytes will give us 256 bits.

            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }

            return randomBytes;
        }

        #endregion Private Methods
    }
}
