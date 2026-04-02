using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LifeHub.Utilidades
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal? user)
        {
            if (user == null)
                return string.Empty;

            return user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("sub")?.Value
                ?? string.Empty;
        }
    }
}