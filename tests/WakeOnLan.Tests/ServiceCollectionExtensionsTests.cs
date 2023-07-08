namespace WakeOnLan.Tests;

using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void TestFactoryCanBeResolved()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddWakeOnLan();

        // Assert
        var provider = serviceCollection.BuildServiceProvider();
        var factory = provider.GetRequiredService<IWolClientFactory>();
        Assert.NotNull(factory);
    }
}
