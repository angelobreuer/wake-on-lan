namespace WakeOnLan.Tests;

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Internal;
using Moq;

public sealed class WolClientFactoryTests
{
    [Fact]
    public void TestFactoryCreatesClient()
    {
        // Arrange
        var systemClock = new SystemClock();
        var factory = new WolClientFactory(systemClock);

        // Act
        var client = factory.Create();

        // Assert
        Assert.False(client.WolInterfaces.IsDefault);
    }

    [Fact]
    public void TestFactoryCachesClient()
    {
        // Arrange
        var systemClock = new SystemClock();
        var factory = new WolClientFactory(systemClock);

        // Act
        var client1 = factory.Create();
        var client2 = factory.Create();

        // Assert
        var wolInterfaces1 = client1.WolInterfaces;
        var wolInterfaces2 = client2.WolInterfaces;

        Assert.Same(
            expected: Unsafe.As<ImmutableArray<WolInterface>, WolInterface[]>(ref wolInterfaces1),
            actual: Unsafe.As<ImmutableArray<WolInterface>, WolInterface[]>(ref wolInterfaces2));
    }

    [Fact]
    public void TestFactoryRecreatesClientAfterRefresh()
    {
        // Arrange
        var time = DateTimeOffset.UtcNow;

        var systemClock = new Mock<ISystemClock>();
        systemClock.SetupGet(x => x.UtcNow).Returns(() => time);

        var factory = new WolClientFactory(systemClock.Object);

        // Act
        var client1 = factory.Create();

        time = time.AddDays(4);

        var client2 = factory.Create();

        // Assert
        var wolInterfaces1 = client1.WolInterfaces;
        var wolInterfaces2 = client2.WolInterfaces;

        Assert.NotSame(
            expected: Unsafe.As<ImmutableArray<WolInterface>, WolInterface[]>(ref wolInterfaces1),
            actual: Unsafe.As<ImmutableArray<WolInterface>, WolInterface[]>(ref wolInterfaces2));
    }
}
