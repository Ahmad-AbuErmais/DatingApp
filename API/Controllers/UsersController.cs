using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTO;
using API.Extensions;
using API.Interfaces;
using API.Modules;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{

    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepstory _userRepstory;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        public UsersController(IUserRepstory userRepstory, IMapper mapper, IPhotoService photoService)
        {
            this._photoService = photoService;
            this._mapper = mapper;
            this._userRepstory = userRepstory;

        }
        [HttpGet]

        public async Task<ActionResult<IEnumerable<MembersDto>>> GetUsers()
        {
            // var users = await _userRepstory.GetUsersAsync();
            // var returnsUsers= _mapper.Map<IEnumerable<MembersDto>>(users);
            // return Ok(returnsUsers);
            var users = await _userRepstory.GetALlMEmbers();
            return Ok(users);
        }
        [HttpGet("{username}", Name ="GetUser")]

        public async Task<ActionResult<MembersDto>> GetUser(string username)
        {
            // var user=await _userRepstory.GetUserByUserNameAsync(username);
            // return _mapper.Map<MembersDto>(user);
            return await _userRepstory.GetMember(username);
        }
        [HttpPut]
        public async Task<ActionResult> UpdateUser(MEmberUpdateDTO mEmberUpdateDTO)
        {
            // var username=User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // User.GetUser();
            var user = await _userRepstory.GetUserByUserNameAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            _mapper.Map(mEmberUpdateDTO, user);
            _userRepstory.Update(user);
            if (await _userRepstory.SaveALlAsync())
                return NoContent();
            return BadRequest("faild to Update User");
        }
        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _userRepstory.GetUserByUserNameAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
 
 
            var result= await _photoService.AddphotoAsync(file);

            if(result.Error != null)
            {
                return BadRequest(result.Error.Message);
             }
           var photo=new Photo{
               Url=result.SecureUrl.AbsoluteUri,
               PublicId=result.PublicId
           };
            if (user.photos.Count == 0)                                                                                                                                     // Here we need to check if the user has any photo's at this stage // current // if 0 == true it is the first photo being uploaded
          {
            photo.IsMain = true;
          }
 
      user.photos.Add(photo);     
      if (await _userRepstory.SaveALlAsync())    
      {
        //   return _mapper.Map<PhotoDto>(photo); 
        return CreatedAtRoute("GetUser", new{username=user.UserName},_mapper.Map<PhotoDto>(photo));

      }                                                                                                                // if true save all photo's
                                                                                                                        // map photo via Dto
 
         return BadRequest("Problem Adding Photo ..");                                                                                                                                                                                                                       // if fail
 
        }


        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMAinPhoto(int PhotoId)
        {
            var user=await _userRepstory.GetUserByUserNameAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var photo=user.photos.FirstOrDefault(x=>x.Id==PhotoId);
            if(photo.IsMain)
            return BadRequest("The Photo Is already is the main ");

            var currentmain=user.photos.FirstOrDefault(x=>x.IsMain);
            if(currentmain!=null)
            currentmain.IsMain=false;
            photo.IsMain=true;

            if(await _userRepstory.SaveALlAsync())
            {
                return NoContent();
            }
            return BadRequest("failed to make the photo is the main ");
        }
        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _userRepstory.GetUserByUserNameAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var photo = user.photos.FirstOrDefault(x => x.Id == photoId);

            if (photo.IsMain) return BadRequest("You cannot delete your main photo");

            if (photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            user.photos.Remove(photo);

            if (await _userRepstory.SaveALlAsync()) return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }

}