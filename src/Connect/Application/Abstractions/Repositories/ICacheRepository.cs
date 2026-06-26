namespace Connect.Application.Abstractions.Repositories;

public interface ICacheRepository<T>
{
    Task<T> GetValueAsync(Guid id);
    Task SetValueAsync(Guid id, T value);
    Task RemoveValueAsync(Guid id);
}