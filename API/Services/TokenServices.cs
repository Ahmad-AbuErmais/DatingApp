using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Interfaces;
using API.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace API.Services
{
    public class TokenServices : ITokenServices
    {
        private readonly SymmetricSecurityKey _key;
        public TokenServices(IConfiguration configuration)
        {
            _key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["TokenKey"]));
        }

        public string CreateToken(AppUser user)
        {
         var claims =new List<Claim>
         {
             new Claim(JwtRegisteredClaimNames.NameId,user.UserName)
         };
         var cred=new SigningCredentials(_key,SecurityAlgorithms.HmacSha512Signature);
         var TokenDiscriptor=new SecurityTokenDescriptor
         {
             Subject=new ClaimsIdentity(claims),
             Expires=DateTime.Now.AddDays(7),
             SigningCredentials=cred
         };
         var tokenhandler=new JwtSecurityTokenHandler();
         var token=tokenhandler.CreateToken(TokenDiscriptor);
         return tokenhandler.WriteToken(token);
        }
    }
    //مهم جد جد فهم ودراست هاذا الموضوع
}