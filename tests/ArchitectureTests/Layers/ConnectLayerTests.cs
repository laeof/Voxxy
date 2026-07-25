using NetArchTest.Rules;
using Shouldly;
using Connect.Presentation.Hubs;
using Connect.Presentation.Cleanup;

namespace ArchitectureTests.Layers;

public sealed class ConnectLayerTests
{
    private static readonly System.Reflection.Assembly ConnectApplicationAssembly =
        typeof(Connect.Application.DependencyInjection).Assembly;

    [Theory]
    [InlineData("Connect.Infrastructure")]
    [InlineData("StackExchange.Redis")]
    [InlineData("Microsoft.AspNetCore.SignalR")]
    [InlineData("Connect.Presentation")]
    public void ConnectApplication_MustNotReferenceForbiddenLayer(string dependency)
    {
        TestResult result = Types.InAssembly(ConnectApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(dependency)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void ConnectHandlers_MustNotReferenceInfrastructure()
    {
        TestResult result = Types.InAssembly(ConnectApplicationAssembly)
            .That()
            .ResideInNamespace("Connect.Application.DeviceLifecycle")
            .Should()
            .NotHaveDependencyOn("Connect.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void ConnectPlayerQueueHandlers_MustNotReferenceInfrastructure()
    {
        TestResult result = Types.InAssembly(ConnectApplicationAssembly)
            .That()
            .ResideInNamespace("Connect.Application.PlayerQueue")
            .Should()
            .NotHaveDependencyOn("Connect.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Connect.Infrastructure.Redis")]
    [InlineData("StackExchange.Redis")]
    public void ConnectHub_MustNotReferenceRedis(string dependency)
    {
        TestResult result = Types.InAssembly(typeof(PlayerHub).Assembly)
            .That()
            .HaveName(nameof(PlayerHub))
            .Should()
            .NotHaveDependencyOn(dependency)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void ConnectHub_PublicMethodsMustNotExposeDomainStateTypes()
    {
        Type[] parameterTypes = typeof(PlayerHub)
            .GetMethods(System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        parameterTypes.All(type =>
                !(type.Namespace ?? string.Empty).StartsWith(
                    "Connect.Domain",
                    StringComparison.Ordinal))
            .ShouldBeTrue();
    }

    [Theory]
    [InlineData("Connect.Infrastructure.Redis")]
    [InlineData("StackExchange.Redis")]
    [InlineData("Connect.Domain")]
    public void ConnectCleanupWorker_MustNotReferencePersistenceImplementationOrDomain(
        string dependency)
    {
        TestResult result = Types.InAssembly(typeof(ConnectLeaseCleanupWorker).Assembly)
            .That()
            .HaveName(nameof(ConnectLeaseCleanupWorker))
            .Should()
            .NotHaveDependencyOn(dependency)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
