using System.Collections.Generic;
using System.Threading.Tasks;
using API.Modules;
using API.DTO;

namespace API.Interfaces
{
    public interface IUserRepstory
    {
        void Update (AppUser user);

        Task<bool> SaveALlAsync();
        Task<IEnumerable<AppUser>> GetUsersAsync();
        Task<AppUser> GetByIdAsync(int id);
        Task<AppUser> GetUserByUserNameAsync(string username);

        Task <IEnumerable<MembersDto>> GetALlMEmbers();
        Task <MembersDto> GetMember( string username);
    }
}