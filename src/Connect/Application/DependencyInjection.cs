using Application.QueuePlaybacks;
using Connect.Application.Abstractions.Services;
using Connect.Application.Devices;
using Connect.Application.Player;
using Microsoft.Extensions.DependencyInjection;

namespace Connect.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddConnectModuleApplication(this IServiceCollection services)
    {
        services.AddScoped<IPlayerSessionService, PlayerSessionService>();
        services.AddScoped<IQueuePlaybackService, QueuePlaybackService>();
        services.AddScoped<IDeviceService, DeviceService>();

        return services;
    }
}