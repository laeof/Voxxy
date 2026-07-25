using Connect.Presentation.Cleanup;
using Connect.Presentation.Hubs;
using NetArchTest.Rules;
using Shouldly;

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

    [Fact]
    public void ConnectApplication_MustNotReferenceSignalRRedisBackplane()
    {
        TestResult result = Types.InAssembly(ConnectApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("Microsoft.AspNetCore.SignalR.StackExchangeRedis")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.SignalR")]
    [InlineData("Microsoft.AspNetCore.SignalR.StackExchangeRedis")]
    public void ConnectDomain_MustNotReferenceSignalR(string dependency)
    {
        TestResult result = Types.InAssembly(typeof(Connect.Domain.Player.PlayerState).Assembly)
            .Should()
            .NotHaveDependencyOn(dependency)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.SignalR")]
    [InlineData("Connect.Presentation")]
    public void ConnectStateStore_MustNotPublishHubEvents(string dependency)
    {
        TestResult result = Types.InAssembly(
                typeof(Connect.Infrastructure.Redis.RedisConnectStateStore).Assembly)
            .That()
            .HaveName(nameof(Connect.Infrastructure.Redis.RedisConnectStateStore))
            .Should()
            .NotHaveDependencyOn(dependency)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void ConnectGroupName_MustNotContainInstanceIdentity()
    {
        Type groupNames = typeof(Connect.Presentation.DependencyInjection).Assembly.GetType(
            "Connect.Presentation.Broadcasting.ConnectGroupNames",
            throwOnError: true)!;
        System.Reflection.MethodInfo userGroup = groupNames.GetMethod(
            "User",
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic)!;
        string group = (string)userGroup.Invoke(null, [Guid.NewGuid()])!;

        group.ShouldStartWith("connect:user:");
        group.Contains("instance", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }
}
