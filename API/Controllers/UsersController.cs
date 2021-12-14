using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTO;
using API.Interfaces;
using API.Modules;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{

    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepstory _userRepstory;
        private readonly IMapper _mapper;
        public UsersController(IUserRepstory userRepstory, IMapper mapper)
        {
            this._mapper = mapper;
            this._userRepstory = userRepstory;

        }
        [HttpGet]

        public async Task<ActionResult<IEnumerable<MembersDto>>> GetUsers()
        {
            // var users = await _userRepstory.GetUsersAsync();
            // var returnsUsers= _mapper.Map<IEnumerable<MembersDto>>(users);
            // return Ok(returnsUsers);
            var users=await _userRepstory.GetALlMEmbers();
            return Ok(users);
        }
        [HttpGet("{username}")]

        public async Task<ActionResult<MembersDto>> GetUser(string username)
        {
            // var user=await _userRepstory.GetUserByUserNameAsync(username);
            // return _mapper.Map<MembersDto>(user);
            return await _userRepstory.GetMember(username);
        }
        [HttpPut]
        public async Task<ActionResult> UpdateUser(MEmberUpdateDTO mEmberUpdateDTO) 
        {
            var username=User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user=await _userRepstory.GetUserByUserNameAsync(username);
            _mapper.Map(mEmberUpdateDTO,user);
             _userRepstory.Update(user);
            if( await _userRepstory.SaveALlAsync())
            return NoContent();
            return BadRequest("faild to Update User");
        }
    }
}