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
}
