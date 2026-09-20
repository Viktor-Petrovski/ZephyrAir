using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Service.Interface;

namespace Service.Implementation;

public class CurrentUserService(IHttpContextAccessor accessor) 
    : ICurrentUserService
{
    public string? GetUserId()
        => accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
