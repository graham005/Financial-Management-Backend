using Financial_management_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
