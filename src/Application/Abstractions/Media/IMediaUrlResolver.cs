namespace Application.Abstractions.Media;

public interface IMediaUrlResolver
{
    Uri GetPublicUrl(string containerName, string key);
    Uri GetSasUrl(string containerName, string key, TimeSpan ttl);
}
