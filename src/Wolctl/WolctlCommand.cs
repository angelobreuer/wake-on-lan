namespace Wolctl;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using WakeOnLan;

internal sealed class WolctlCommand : RootCommand, ICommandHandler
{
    private static readonly Argument<string> _addressArgument = new(
        name: "Address",
        description: "The MAC address of the target device to wake over lan (either EUI-48 or EUI-64); or IPv4 address used to try to resolve the MAC address using ARP. Specifying an IP address requires ARP resolution which will only work if the device is already online, the resolved address is still cached, or the device has static ARP entries.");

    private static readonly Option<int> _countOption = new(
        aliases: new[] { "--count", "-c" },
        getDefaultValue: () => 1,
        description: "The number of times to send the magic packet.");

    private static readonly Option<int> _intervalOption = new(
        aliases: new[] { "--interval", "-i" },
        getDefaultValue: () => 100,
        description: "The interval in milliseconds between each magic packet.");

    private static readonly Option<int> _portOption = new(
        aliases: new[] { "--port", "-p" },
        getDefaultValue: () => 9,
        description: "The port to send the magic packet to.");

    private static readonly Option<bool> _ipv4OnlyOption = new(
        aliases: new[] { "--ipv4-only", "-4" },
        description: "Only send the magic packet over IPv4.");

    private static readonly Option<bool> _ipv6OnlyOption = new(
        aliases: new[] { "--ipv6-only", "-6" },
        description: "Only send the magic packet over IPv6.");

    private static readonly Option<bool> _verboseOption = new(
        aliases: new[] { "--verbose", "-v" },
        description: "Enable verbose logging.");

    private static readonly Option<bool> _useSingleInterfaceOption = new(
        aliases: new[] { "--use-single-interface", "-s" },
        description: "Use a single network interface to send the magic packet.");

    public WolctlCommand() : base("Wake-on-LAN client")
    {
        AddArgument(_addressArgument);

        AddOption(_verboseOption);
        AddOption(_countOption);
        AddOption(_intervalOption);
        AddOption(_portOption);
        AddOption(_ipv4OnlyOption);
        AddOption(_ipv6OnlyOption);
        AddOption(_useSingleInterfaceOption);

        Handler = this;
    }

    public int Invoke(InvocationContext context)
    {
        return InvokeAsync(context).GetAwaiter().GetResult();
    }

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cancellationToken = context.GetCancellationToken();

        var address = context.ParseResult.GetValueForArgument(_addressArgument);
        var count = context.ParseResult.GetValueForOption(_countOption);
        var interval = context.ParseResult.GetValueForOption(_intervalOption);
        var port = context.ParseResult.GetValueForOption(_portOption);
        var ipv4Only = context.ParseResult.GetValueForOption(_ipv4OnlyOption);
        var ipv6Only = context.ParseResult.GetValueForOption(_ipv6OnlyOption);
        var verbose = context.ParseResult.GetValueForOption(_verboseOption);
        var useSingleInterface = context.ParseResult.GetValueForOption(_useSingleInterfaceOption);

        var logLevel = verbose ? LogLevel.Debug : LogLevel.Error;
        using var loggerFactory = LoggerFactory.Create(x => x.AddConsole().SetMinimumLevel(logLevel));
        var logger = loggerFactory.CreateLogger<WolctlCommand>();

        var addressFamily = (ipv4Only, ipv6Only) switch
        {
            (false, false) => AddressFamily.Unspecified,
            (true, false) => AddressFamily.InterNetwork,
            (false, true) => AddressFamily.InterNetworkV6,
            _ => default(AddressFamily?),
        };

        if (addressFamily is null)
        {
            logger.LogError("Cannot specify both --ipv4-only and --ipv6-only.");
            return 1;
        }

        if (!WolAddress.TryParse(address, provider: null, out var wolAddress))
        {
            if (!IPAddress.TryParse(address, out var ipAddress))
            {
                logger.LogError("Invalid MAC/IP address: {Address}", address);
                return 1;
            }

            if (!OperatingSystem.IsWindows())
            {
                logger.LogError("Resolving MAC addresses from an IP address on non-Windows operating systems is not supported.");
                return 1;
            }

            wolAddress = await AddressResolution
                .ResolveAddressAsync(ipAddress, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation("Resolved address {IpAddress} to {WolAddress}.", ipAddress, wolAddress);
        }

        var wolClientOptions = new WolClientOptions(addressFamily.Value, port, useSingleInterface);
        var wolClient = new WolClient(wolClientOptions);

        if (wolClient.WolInterfaces.IsEmpty)
        {
            logger.LogDebug("No network interfaces found; falling back to broadcast address.");
        }
        else
        {
            foreach (var (localIpAddress, multicastIpAddresses) in wolClient.WolInterfaces)
            {
                logger.LogDebug(
                    "Network interface {LocalIpAddress} has the following multicast addresses: {MulticastIpAddresses}.",
                    localIpAddress, multicastIpAddresses);
            }
        }

        using var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(interval));
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < count; index++)
        {
            Console.WriteLine($"#{index + 1,-3} {stopwatch.ElapsedMilliseconds,6}ms     {wolAddress}");

            await wolClient
                .WakeAsync(wolAddress, cancellationToken)
                .ConfigureAwait(false);

            if (index < count - 1)
            {
                await periodicTimer
                    .WaitForNextTickAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return 0;
    }
}