using System.Text.Json;
using Connect.Contracts.States;
using Connect.Presentation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Connect.Presentation.UnitTests.Transport;

public sealed class SignalRJsonProtocolTests
{
    [Fact]
    public void RepeatMode_IsSerializedUsingTheV2StringContract()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "http://localhost"
                })
            .Build();
        services.AddConnectModulePresentation(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        JsonSerializerOptions options = provider
            .GetRequiredService<IOptions<JsonHubProtocolOptions>>()
            .Value
            .PayloadSerializerOptions;

        JsonSerializer.Serialize(RepeatModeDto.Queue, options).ShouldBe("\"Queue\"");
    }
}
