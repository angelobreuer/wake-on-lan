namespace WakeOnLan.Tests;

using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;

public sealed class WolClientTests
{
    [Fact]
    public async Task TestWakeAsync()
    {
        // Arrange
        using var listenerContext = new ListenerContext();

        var endpoint = new WolEndPoint(listenerContext.EndPoint.Address, listenerContext.EndPoint.Port);

        var wolInterface = new WolInterface(
            LocalAddress: IPAddress.Loopback,
            MulticastEndPoints: ImmutableArray.Create(endpoint));

        var expectedMagicPacket = new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
        };

        var receiveTask = listenerContext.ReceiveAsync().AsTask();

        var wolClient = new WolClient(wolInterfaces: ImmutableArray.Create(wolInterface));
        var macAddress = new WolAddress(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, });

        // Act
        await wolClient
            .WakeAsync(macAddress)
            .ConfigureAwait(false);

        // Assert
        var result = await receiveTask.ConfigureAwait(false);
        Assert.True(result.Span.SequenceEqual(expectedMagicPacket));
    }

    [OnlyWindowsFact]
    public void TestCanQueryInterfaces()
    {
        // Arrange
        var wolClient = new WolClient();

        // Act
        var wolInterfaces = wolClient.WolInterfaces;

        // Assert
        Assert.NotEmpty(wolInterfaces);
    }

    [OnlyWindowsFact]
    public void TestQueryOnlyReturnsIPv4Interfaces()
    {
        // Arrange
        var options = new WolClientOptions(AddressFamily.InterNetwork);
        var wolClient = new WolClient(options);

        // Act
        var wolInterfaces = wolClient.WolInterfaces;

        // Assert
        Assert.All(wolInterfaces, x => Assert.Equal(AddressFamily.InterNetwork, x.LocalAddress.AddressFamily));
    }

    [OnlyWindowsFact]
    public void TestQueryOnlyReturnsIPv6Interfaces()
    {
        // Arrange
        var options = new WolClientOptions(AddressFamily.InterNetworkV6);
        var wolClient = new WolClient(options);

        // Act
        var wolInterfaces = wolClient.WolInterfaces;

        // Assert
        Assert.All(wolInterfaces, x => Assert.Equal(AddressFamily.InterNetworkV6, x.LocalAddress.AddressFamily));
    }

    [Fact]
    public void TestPreferBroadcastReturnsBroadcastInterface()
    {
        // Arrange
        var options = new WolClientOptions(PreferBroadcast: true);
        var wolClient = new WolClient(options);

        // Act
        var wolInterfaces = wolClient.WolInterfaces;

        // Assert
        Assert.All(wolInterfaces, x => Assert.True(x.MulticastEndPoints.Single().IsBroadcast));
    }

    [OnlyWindowsFact]
    public void TestQueryOnlyReturnsSingleInterfaceIfSpecifiedInOptions()
    {
        // Arrange
        var options = new WolClientOptions(AddressFamily.InterNetworkV6, UseSingleInterface: true);
        var wolClient = new WolClient(options);

        // Act
        var wolInterfaces = wolClient.WolInterfaces;

        // Assert
        Assert.Single(wolInterfaces);
    }
}

file sealed class ListenerContext : IDisposable
{
    public ListenerContext()
    {
        EndPoint = new IPEndPoint(IPAddress.Loopback, Random.Shared.Next(1024, 65536));

        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        Socket.Bind(EndPoint);
    }

    public IPEndPoint EndPoint { get; }

    public Socket Socket { get; }

    public void Dispose()
    {
        Socket.Dispose();
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var buffer = GC.AllocateUninitializedArray<byte>(0x10000);

        var bytesReceived = await Socket
            .ReceiveAsync(buffer, SocketFlags.None, cancellationToken)
            .ConfigureAwait(false);

        return buffer.AsMemory(0, bytesReceived);
    }
}

file sealed class MockSocket : Socket
{
    public MockSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        : base(addressFamily, socketType, protocolType)
    {
    }
}

file sealed class OnlyWindowsFactAttribute : FactAttribute
{
    public OnlyWindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Only supported on Windows";
        }
    }
}