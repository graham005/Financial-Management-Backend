using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }
        

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserDto userDto)
        {
            if (!new[] { "Admin", "Accountant", "StockManager", "ExpenseManager" }.Contains(userDto.Role))
                return BadRequest("Invalid role.");

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userDto.Username || u.Email == userDto.Email);
            if (existingUser != null)
                return Conflict("Username or email already exists.");

            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                Role = userDto.Role,
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();
            if (users == null || users.Count == 0) return NotFound("User not found");
            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(Guid id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound("User with that ID not found");

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody ] UserDto updateUserDto)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound("User with that ID not found");

            user.Username = updateUserDto.Username;
            user.Email = updateUserDto.Email;
            user.Role = updateUserDto.Role;

            if (!string.IsNullOrEmpty(updateUserDto.Password))
                user.Password = BCrypt.Net.BCrypt.HashPassword(updateUserDto.Password);

            await _context.SaveChangesAsync();
            return Ok("User updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound("User with that ID not found");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok("User Deleted successfully");
        }
    }
}
