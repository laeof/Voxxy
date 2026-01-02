namespace SharedKernel;

public sealed class ConnectionStringsOptions
{
    public string Database { get; init; } = default!;
    public string AzureStorage { get; init; } = default!;
}