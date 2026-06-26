using Connect.Domain.Player;

namespace Connect.Application.Abstractions.Repositories;

public interface IPlayerSessionRepository : ICacheRepository<PlayerState>
{

}