using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.Modules;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedUsers(DataContext context)
        {
            if( await context.Users.AnyAsync())
            return ;
            var UserData=await System.IO.File.ReadAllTextAsync("Data/UserSeedData.json");
            var  users = JsonSerializer.Deserialize<List<AppUser>>(UserData);
            foreach (var user in users)
            {
                var hmac=new HMACSHA512();
                user.UserName=user.UserName.ToLower();
                user.PasswordHash=hmac.ComputeHash(Encoding.UTF8.GetBytes("ahmed999Ll"));
                user.PasswordSalt=hmac.Key;
                await context.Users.AddAsync(user);
            }
            await context.SaveChangesAsync();
        }
    }
}