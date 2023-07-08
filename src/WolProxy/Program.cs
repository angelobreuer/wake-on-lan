using WakeOnLan;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWakeOnLan();

var app = builder.Build();

app.MapPost("/wake/{macAddress}", async (IWolClientFactory wolClientFactory, WolAddress macAddress) =>
{
    var wolClient = wolClientFactory.Create();

    await wolClient
        .WakeAsync(macAddress)
        .ConfigureAwait(false);
});

app.Run();
