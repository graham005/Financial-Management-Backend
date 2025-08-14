using System.Security.Claims;

namespace Financial_management_backend.Services.Helpers
{
    public static class UserClaimsExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier) ?? user?.FindFirst("sub");
            if (userIdClaim == null)
                return null;

            if (Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return null;
        }
    }
}