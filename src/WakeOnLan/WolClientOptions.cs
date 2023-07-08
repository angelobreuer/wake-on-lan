namespace WakeOnLan;

using System.Net.Sockets;

public readonly record struct WolClientOptions(
    AddressFamily AddressFamily = AddressFamily.Unspecified,
    int? Port = WolClientOptions.DefaultPort,
    bool? UseSingleInterface = null,
    bool PreferBroadcast = false)
{
    private const int DefaultPort = 9;

    public static WolClientOptions Default => default;
}