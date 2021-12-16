using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTO;
using API.Interfaces;
using API.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountUser : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenServices _token;

        public AccountUser(DataContext context, ITokenServices token)
        {
            this._token = token;
            _context = context;
        }
        [HttpPost("register")]
        public async Task<ActionResult<DtoUser>> Register(DtoRegister dtoRegister)
        {
            if (await CheckUsername(dtoRegister.Username))
                return BadRequest("the username is taken");
            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                UserName = dtoRegister.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dtoRegister.Password)),
                PasswordSalt = hmac.Key
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return new DtoUser
            {
                Username = user.UserName,
                Token =_token.CreateToken(user)
            };

        }

        [HttpPost("login")]
        public async Task<ActionResult<DtoUser>> Login(DtoLogin dtoLogin)
        {
            var user = await _context.Users.
            Include(x=>x.photos).SingleOrDefaultAsync(x => x.UserName == dtoLogin.username);
            if (user == null)
                return Unauthorized("this username is not Authorized");
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computehash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dtoLogin.password));
            for (int i = 0; i < computehash.Length; i++)
            {
                if (computehash[i] != user.PasswordHash[i])
                    return Unauthorized("INvalid Password");
            }
               return new DtoUser
            {
                Username = user.UserName,
                Token =_token.CreateToken(user),
                PhotoUrl=user.photos.FirstOrDefault(x=>x.IsMain)?.Url
            };

        }
        private async Task<bool> CheckUsername(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}