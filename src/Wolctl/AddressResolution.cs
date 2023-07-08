namespace Wolctl;

using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using WakeOnLan;

internal static class AddressResolution
{
    [SupportedOSPlatform("windows")]
    public static ValueTask<WolAddress> ResolveAddressAsync(IPAddress address, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var task = Task.Factory.StartNew(static (object? addressObject) =>
        {
            var address = Unsafe.As<object?, IPAddress>(ref addressObject);

            Span<byte> physicalAddress = stackalloc byte[sizeof(ulong) * 2];
            var physicalAddressLength = physicalAddress.Length;

#pragma warning disable CS0618
            var result = SafeNativeMethods.SendARP(
                destinationAddress: (int)address.Address,
                sourceAddress: 0,
                physicalAddress: physicalAddress,
                physicalAddressLength: ref physicalAddressLength);
#pragma warning restore CS0618

            if (result is not 0)
            {
                throw new Win32Exception(result);
            }

            return new WolAddress(physicalAddress[..6]);

        }, address, cancellationToken);

        return new ValueTask<WolAddress>(task);
    }
}

internal static partial class SafeNativeMethods
{
    [LibraryImport("iphlpapi.dll")]
    public static partial int SendARP(int destinationAddress, int sourceAddress, Span<byte> physicalAddress, ref int physicalAddressLength);
}