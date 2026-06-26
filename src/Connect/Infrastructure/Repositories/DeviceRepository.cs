using Connect.Application.Abstractions.Repositories;
using Connect.Domain.Devices;
using Connect.Infrastructure.Redis;
using StackExchange.Redis;

namespace Connect.Infrastructure.Repositories;

public sealed class DeviceRepository : RedisCacheRepository<Device>, IDeviceRepository
{
    public DeviceRepository(IConnectionMultiplexer multiplexer) : base(multiplexer, TableConstants.DeviceTable)
    {

    }
}