using System.Security.Claims;

namespace API.Extensions
{
    public static class ClaimsExtension
    {
         public static string GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}