using EskaCMS.Core.Entities;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Model;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;
using static EskaCMS.Core.Models.TokenVM;

namespace EskaCMS.Core.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
       // private readonly IWorkContext _IWorkContext;
       // private readonly IRepository<UsersSites> _UsersSitesRepository;
        public TokenService(IConfiguration configuration,
                            UserManager<User> userManager
                           )
        {
            _configuration = configuration;
            _userManager = userManager;

        }
        private string GenerateAccessToken(User user, out DateTime ExpiryDate)
        {

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Authentication:Jwt:Key"]));
            var Duration = _configuration["Authentication:Jwt:AccessTokenDurationInMinutes"];
            var Issuer = _configuration["Authentication:Jwt:Issuer"];
            var TokenExpiryDate = user.Type == EUsersTypes.GuestClient ? DateTime.Now.AddYears(5) : DateTime.Now.AddMinutes(long.Parse(Duration));
            var roles = _userManager.GetRolesAsync(user).Result;
            var Claims = BuildClaims(user, roles).Result;
            JwtSecurityToken jwtToken = new JwtSecurityToken(
                    claims: Claims,
                    issuer: Issuer,
                    notBefore: DateTime.UtcNow,
                    expires: TokenExpiryDate,
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            string token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            ExpiryDate = TokenExpiryDate;
            return token;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            try
            {


                var secret = _configuration.GetSection("Authentication").GetSection("Jwt").GetSection("Key").Value;
                var issuer = _configuration.GetSection("Authentication").GetSection("Jwt").GetSection("Issuer").Value;
                var key = Encoding.ASCII.GetBytes(secret);


                var tokencvcvHandler = new JwtSecurityTokenHandler();
                var securityvbvbToken = tokencvcvHandler.ReadToken(token) as JwtSecurityToken;

               // var stringClaimValue = securityToken.Claims.First(claim => claim.Type == claimType).Value;

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidIssuer = issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = false,
                    RequireExpirationTime = false,
                    ClockSkew = TimeSpan.Zero
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !string.Equals(jwtSecurityToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                return principal;
            }
            catch (Exception exc)
            {

                throw exc;
            }

        }
        private async Task<IList<Claim>> BuildClaims(User user, IList<string> roles)
        {
            try
            {

                //var SiteId = _IWorkContext.GetCurrentSiteId();
                //if (SiteId == 0)
                //    throw new Exception("SiteId cannot be null");

                string UserRoles = string.Empty;
                var claims = new List<Claim>();
                foreach (var Role in roles)
                {
                    UserRoles += Role + ",";
                }
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                claims.Add(new Claim(ClaimTypes.Name, user.FullName));
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber == null ? "" : user.PhoneNumber));
                claims.Add(new Claim(ClaimTypes.Role, UserRoles));
                claims.Add(new Claim("Username", user.UserName));
                claims.Add(new Claim("UserType", user.Type.ToString()));
                claims.Add(new Claim("DefaultShippingAddressId", user.DefaultShippingAddressId == null ? "" : user.DefaultShippingAddressId.ToString()));
                claims.Add(new Claim("CurrencyCultureId", ""));
                claims.Add(new Claim("CultureId", ""));
                //claims.Add(new Claim("SiteId", SiteId.ToString()));
                claims.Add(new Claim(ClaimTypes.DateOfBirth, user.DateOfBirth == null ? "" : user.DateOfBirth.ToString()));
                claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString()));
                await _userManager.AddClaimsAsync(user, claims);
                

                return claims;
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        public async Task<TokenResponse> CreateToken(User user, bool includeRefreshToken)
        {
            try
            {


                return await CreateTokenWithProcess(user, includeRefreshToken);
            }
            catch (Exception exc)
            {

                throw exc;
            }

        }

        private async Task<TokenResponse> CreateTokenWithProcess(User user, bool includeRefreshToken)
        {
            try
            {
                TokenResponse runTimeObject = new TokenResponse();
                if (user.Status == EStatus.Inactive)
                    return new TokenResponse();

                user.LastLoginDate = DateTimeOffset.Now;
                await _userManager.UpdateAsync(user);
                DateTime ExpiryDate = DateTime.MinValue;
                var token = GenerateAccessToken(user, out ExpiryDate);
                if (includeRefreshToken)
                {
                    var refreshToken = GenerateRefreshToken();
                    user.RefreshTokenHash = _userManager.PasswordHasher.HashPassword(user, refreshToken);
                    await _userManager.UpdateAsync(user);

                    runTimeObject.Token = token;
                    runTimeObject.RefreshToken = refreshToken;
                    runTimeObject.ExpiryDate = ExpiryDate;
                    return runTimeObject;
                }

                runTimeObject.Token = token;
                runTimeObject.ExpiryDate = ExpiryDate;
                return runTimeObject;
            }
            catch (Exception e)
            {

                throw e;
            }
        }


        public async Task<TokenResponse> RefeshToken(RefreshTokenModel model)
        {
            TokenResponse runTimeObject = new TokenResponse();
            var principal = GetPrincipalFromExpiredToken(model.Token);
            if (principal == null)
            {
                throw new Exception("Invalid token");
            }

            var user = await _userManager.GetUserAsync(principal);
            var verifyRefreshTokenResult = _userManager.PasswordHasher.VerifyHashedPassword(user, user.RefreshTokenHash, model.RefreshToken);
            if (verifyRefreshTokenResult == PasswordVerificationResult.Success)
            {
                DateTime ExpiryDate = DateTime.MinValue;
                var token = GenerateAccessToken(user,out ExpiryDate);
                var refreshToken = GenerateRefreshToken();
                user.RefreshTokenHash = _userManager.PasswordHasher.HashPassword(user, refreshToken);
                await _userManager.UpdateAsync(user);
                runTimeObject.Token = token;
                runTimeObject.RefreshToken = refreshToken;
                runTimeObject.ExpiryDate = ExpiryDate;
                return runTimeObject;

            }
            throw new Exception("Invalid token");

        }


    }
}
