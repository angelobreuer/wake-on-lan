namespace WakeOnLan;

using System.Collections.Immutable;
using System.Net;

public readonly record struct WolInterface(IPAddress LocalAddress, ImmutableArray<WolEndPoint> MulticastEndPoints);