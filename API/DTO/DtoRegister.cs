using System.ComponentModel.DataAnnotations;

namespace API.DTO
{
    public class DtoRegister
    {
        [Required]
        public string  Username { get; set; }
        [Required]
        public string  Password { get; set; }
    }
}