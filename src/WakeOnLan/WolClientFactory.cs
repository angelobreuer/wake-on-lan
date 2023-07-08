namespace WakeOnLan;

using System;
using Microsoft.Extensions.Internal;

public sealed class WolClientFactory : IWolClientFactory
{
    private readonly WolClientOptions _options;
    private readonly ISystemClock _systemClock;
    private DateTimeOffset? _refreshAt;
    private WolClient? _wolClient;

    public WolClientFactory(ISystemClock systemClock, WolClientOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(systemClock);

        _systemClock = systemClock;
        _options = options ?? WolClientOptions.Default;
    }

    public WolClient Create()
    {
        if (_wolClient is null || _systemClock.UtcNow > _refreshAt)
        {
            _wolClient = new WolClient(_options);
            _refreshAt = _systemClock.UtcNow.AddMinutes(5);
        }

        return _wolClient.Value;
    }
}
