namespace WakeOnLan;

using System.Net;

public readonly record struct WolEndPoint(IPAddress Address, int Port)
{
    public bool IsBroadcast => Address == IPAddress.Broadcast;
}
