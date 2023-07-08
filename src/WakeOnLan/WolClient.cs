namespace WakeOnLan;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public readonly record struct WolClient
{
    private const int DefaultWolPort = 9;

    public WolClient()
        : this(WolClientOptions.Default)
    {
    }

    public WolClient(WolClientOptions options)
        : this(QueryInterfaces(options))
    {
    }

    public WolClient(ImmutableArray<WolInterface> wolInterfaces)
    {
        WolInterfaces = wolInterfaces;
    }

    public ImmutableArray<WolInterface> WolInterfaces { get; }

    private static ImmutableArray<WolInterface> QueryInterfaces(WolClientOptions options)
    {
        const string IPv4Prefix = "224.0.0.1";
        const string IPv6Prefix = "ff02::1%";

        var port = options.Port.GetValueOrDefault(DefaultWolPort);
        var useSingleInterface = options.UseSingleInterface.GetValueOrDefault(false);
        var addressFamily = options.AddressFamily;

        static ImmutableArray<WolEndPoint> GetMulticastEndPoints(IPInterfaceProperties interfaceProperties, AddressFamily addressFamily, int port)
        {
            Debug.Assert(addressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6);

            var prefix = addressFamily is AddressFamily.InterNetwork ? IPv4Prefix : IPv6Prefix;

            return interfaceProperties.MulticastAddresses
                .Select(x => x.Address)
                .Where(x => x.AddressFamily == addressFamily)
                .Where(x => x.ToString().StartsWith(prefix))
                .Select(x => new WolEndPoint(x, port))
                .ToImmutableArray();
        }

        static void Query(NetworkInterface networkInterface, ImmutableArray<WolInterface>.Builder wolInterfaces, AddressFamily addressFamily, int port)
        {
            var interfaceProperties = networkInterface.GetIPProperties();
            var privateAddressingActive = OperatingSystem.IsWindows() && interfaceProperties.GetIPv4Properties().IsAutomaticPrivateAddressingActive;

            var queryIPv4Interfaces = addressFamily is AddressFamily.Unspecified or AddressFamily.InterNetwork;
            var queryIPv6Interfaces = addressFamily is AddressFamily.Unspecified or AddressFamily.InterNetworkV6;

            var ipv6UnicastAddress = !queryIPv6Interfaces ? null : interfaceProperties.UnicastAddresses
                .Where(static x => x.Address.AddressFamily is AddressFamily.InterNetworkV6)
                .FirstOrDefault(static x => !x.Address.IsIPv6LinkLocal);

            if (ipv6UnicastAddress is not null)
            {
                var ipv6MulticastEndPoints = GetMulticastEndPoints(interfaceProperties, AddressFamily.InterNetworkV6, port);

                if (!ipv6MulticastEndPoints.IsEmpty)
                {
                    wolInterfaces.Add(new WolInterface(
                        LocalAddress: ipv6UnicastAddress.Address,
                        MulticastEndPoints: ipv6MulticastEndPoints));
                }
            }

            var ipv4UnicastAddress = !queryIPv4Interfaces || privateAddressingActive ? null : interfaceProperties.UnicastAddresses
                .FirstOrDefault(static x => x.Address.AddressFamily is AddressFamily.InterNetwork);

            if (ipv4UnicastAddress is not null)
            {
                var ipv4MulticastEndPoints = GetMulticastEndPoints(interfaceProperties, AddressFamily.InterNetwork, port);

                if (!ipv4MulticastEndPoints.IsEmpty)
                {
                    wolInterfaces.Add(new WolInterface(
                        LocalAddress: ipv4UnicastAddress.Address,
                        MulticastEndPoints: ipv4MulticastEndPoints));
                }
            }
        }

        var builder = ImmutableArray.CreateBuilder<WolInterface>();

        if (!options.PreferBroadcast)
        {
            var interfaceCandidates = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(static x => x.NetworkInterfaceType is not NetworkInterfaceType.Loopback)
                .Where(static x => x.OperationalStatus is OperationalStatus.Up);

            foreach (var networkInterface in interfaceCandidates)
            {
                if (useSingleInterface && builder.Count > 0)
                {
                    break;
                }

                Query(networkInterface, builder, addressFamily, port);
            }
        }

        if (builder.Count is 0 && addressFamily is not AddressFamily.InterNetworkV6)
        {
            var broadcastEndPoint = new WolEndPoint(IPAddress.Broadcast, port);

            // Add fallback broadcast address
            builder.Add(new WolInterface(
                LocalAddress: IPAddress.Loopback,
                MulticastEndPoints: ImmutableArray.Create(broadcastEndPoint)));
        }

#if NET8_0_OR_GREATER
        return builder.DrainToImmutable();
#else
        return builder.ToImmutable();
#endif
    }

    private static ReadOnlyMemory<byte> BuildMagicPacket(WolAddress wolAddress)
    {
        var macAddress = wolAddress.Address;

        const int HeaderLength = 6; // 6 times 0xFF
        var encodedLength = HeaderLength + (16 * macAddress.Length);
        var magicPacket = GC.AllocateUninitializedArray<byte>(encodedLength).AsMemory();

        magicPacket[..6].Span.Fill(0xFF);

        var destination = magicPacket[6..];

        for (var index = 0; index < 16; index++)
        {
            macAddress.CopyTo(destination.Span);
            destination = destination[macAddress.Length..];
        }

        Debug.Assert(destination.IsEmpty);

        return magicPacket;
    }

    public ValueTask WakeAsync(WolAddress wolAddress, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return WakeInternalAsync(BuildMagicPacket(wolAddress), cancellationToken);
    }

    private async ValueTask WakeInternalAsync(ReadOnlyMemory<byte> magicPacket, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var (localIpAddress, endPoints) in WolInterfaces)
        {
            using var socket = new Socket(localIpAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(localIpAddress, 0));

            foreach (var wolEndPoint in endPoints)
            {
                var (multicastAddress, multicastPort) = wolEndPoint;
                var remoteEndPoint = new IPEndPoint(multicastAddress, multicastPort);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, wolEndPoint.IsBroadcast);

                await socket
                    .SendToAsync(magicPacket, remoteEndPoint, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
