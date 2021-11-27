using API.Modules;

namespace API.Interfaces
{
    public interface ITokenServices
    {
         string CreateToken(AppUser user);
    }
}