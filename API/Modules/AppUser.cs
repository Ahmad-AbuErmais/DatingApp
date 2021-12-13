using System;
using System.Collections.Generic;
using API.Extensions;

namespace API.Modules
{
    public class AppUser
    {
        public int Id { get; set; }
        public string UserName { get; set; }     
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string  knownAs { get; set; }

        public DateTime Created { get; set; }=DateTime.Now;
        public DateTime LastActive { get; set; }=DateTime.Now;
        public string Gender { get; set; }      
        public string Itroduction { get; set; }
        public string lookingFor { get; set; }
        public string Interests { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public ICollection<Photo> photos{get; set;}
        // public int GetAge()
        // {

        //     return DateOfBirth.CalculateAge();
        // }
    }
}