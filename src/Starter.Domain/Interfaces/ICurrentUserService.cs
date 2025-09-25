namespace Starter.Domain.Interfaces;

public record UserInfo(Guid Id, string UserName, string Email);

public interface ICurrentUserService
{
    UserInfo? GetUserInfo();
}