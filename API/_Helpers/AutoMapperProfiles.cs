using System.Linq;
using API.DTO;
using API.Extensions;
using API.Modules;
using AutoMapper;

namespace API._Helpers
{
    public class AutoMapperProfiles:Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser,MembersDto>().
            ForMember(dest=>dest.PhotoUrl,option=>option.
            MapFrom(src=>src.photos.FirstOrDefault(x=>x.IsMain).Url))
            .ForMember(dest=>dest.Age,opt=>opt.MapFrom(src=>src.DateOfBirth.CalculateAge()));
            CreateMap<Photo,PhotoDto>();
            

        }

  
    }
}