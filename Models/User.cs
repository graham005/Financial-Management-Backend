using Financial_management_backend.Services.Dtos;

namespace Financial_management_backend.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public ERole Role { get; set; } 
    }
}
