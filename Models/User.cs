using System.ComponentModel.DataAnnotations;
using Financial_management_backend.Services.Dtos;

namespace Financial_management_backend.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } // Admin, Accountant, StockManager, ExpenseManager

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastPasswordChange { get; set; } // Track when password was last changed
        public bool RequirePasswordChange { get; set; } = false; // Force password change on next login
    }
}
