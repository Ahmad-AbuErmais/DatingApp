using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTO;
using API.Interfaces;
using API.Modules;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepisotry : IUserRepstory
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public UserRepisotry(DataContext context, IMapper mapper)
        {
            this._mapper = mapper;
            this._context = context;

        }

        public async Task<IEnumerable<MembersDto>> GetALlMEmbers()
        {
            return await _context.Users.ProjectTo<MembersDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

       public async Task<MembersDto> GetMember(string username)
        {
            return await _context.Users.Where(user => user.UserName == username).
            ProjectTo<MembersDto>(_mapper.ConfigurationProvider).FirstOrDefaultAsync();
        }

        public async Task<AppUser> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

          public async Task<AppUser> GetUserByUserNameAsync(string username)
        {
            return await _context.Users
                .Include(p => p.photos)
                .SingleOrDefaultAsync(x => x.UserName == username);
        }
    
        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _context.Users.Include(p => p.photos).ToListAsync();
        }

        public async Task<bool> SaveALlAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public void Update(AppUser user)
        {
            _context.Entry(user).State = EntityState.Modified;
        }
    }
}