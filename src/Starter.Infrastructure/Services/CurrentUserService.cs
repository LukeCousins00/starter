using Microsoft.AspNetCore.Http;
using Starter.Domain.Interfaces;
using System.Security.Claims;

namespace Starter.Infrastructure.Services;

internal class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public UserInfo? GetUserInfo()
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is null) 
            return null;

        var userInfo = new UserInfo(
            Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value!),
            httpContext.User.FindFirst(ClaimTypes.GivenName)!.Value!,
            httpContext.User.FindFirst(ClaimTypes.Email)!.Value!);

        return userInfo;
    }
}
