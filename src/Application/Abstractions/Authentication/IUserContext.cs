namespace Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid UserId { get; }

    string Email { get; }

    string FullName { get; }
    string ImageUrl { get; }

    string[] UserClaims { get; }
}
