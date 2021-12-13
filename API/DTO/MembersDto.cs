using System;
using System.Collections.Generic;

namespace API.DTO
{
    public class MembersDto
    {
        
        public int Id { get; set; }
        public string Username { get; set; }   
        public string PhotoUrl { get; set; }  
        public string Age { get; set; }
        public string  knownAs { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastActive { get; set; }
        public string Gender { get; set; }      
        public string Itroduction { get; set; }
        public string lookingFor { get; set; }
        public string Interests { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public ICollection<PhotoDto> photos{get; set;}
    }
}