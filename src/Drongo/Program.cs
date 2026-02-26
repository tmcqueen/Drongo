using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Drongo.Core.SIP.Dialogs;
using Drongo.Core.SIP.Parsing;
using Drongo.Core.SIP.Registration;
using Drongo.Core.Transport;
using Drongo.Core.Hosting;

IDrongoApplicationBuilder builder = DrongoApplication.CreateBuilder();

builder.Services.AddSingleton<IDialogFactory, DialogFactory>();
builder.Services.AddSingleton<ISipParser, SipParser>();
builder.Services.AddSingleton<IRegistrar, InMemoryRegistrar>();

builder.Services.AddSingleton<IUdpTransport, UdpTransport>();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});


var services = builder.Services.BuildServiceProvider();

IDrongoApplication app = builder.Build();


app.MapInvite(async context =>
{
    var logger = context.Items.TryGetValue("logger", out var l) ? (ILogger)l! : null;
    logger?.LogInformation("Received INVITE for {Uri}", context.Request.RequestUri);
    
    await context.SendResponseAsync(180, "Ringing");
    await context.SendResponseAsync(200, "OK");
});

app.MapRegister(async context =>
{
    var logger = context.Items.TryGetValue("logger", out var l) ? (ILogger)l! : null;
    logger?.LogInformation("Processing REGISTER for {Uri}", context.Request.To);
});

app
    .MapEndpoint(IPAddress.Any, 5060) // Returns an IEndpointBuilder, you can use it to configure the endpoint, for example, you can specify the transport protocol, TLS settings, etc.
    .MapEndpoint(IPAddress.Any, 5061)
    .WithTransport<IUdpTransport>(); // This will configure the endpoint to use UDP transport, you can also use WithTransport<ITcpTransport>() or WithTransport<ITlsTransport>() for TCP or TLS transport respectively.
                                     // returns IEndpointBuilder, you can chain multiple calls to configure the endpoint further, for example, you can specify the TLS settings for a TLS endpoint, etc.

// Register application lifecycle events
// context is ApplicationContext, a record that contains information about the 
// application and its environment, you can use it to access services, configuration, etc.
app.AppLifetime.ApplicationStarting.Register((context) =>
{
    // GetEndpoints() should return an array of Record(System.Net.Sockets.ProtocolType Protocol, IPAddress Address, int Port)
    var endpoints = app.GetEndpoints();
    var ep1 = endpoints[0];
    var ep2 = endpoints[1];

    Console.WriteLine("Drongo - SIP Runtime");
    Console.WriteLine($"Starting on {ep1.Protocol} {ep1.Address}:{ep1.Port}...");
    Console.WriteLine($"Starting on {ep2.Protocol} {ep2.Address}:{ep2.Port}...");
    Console.WriteLine("Drongo has started successfully.");
    return Task.CompletedTask;
});

app.AppLifetime.ApplicationStopping.Register((context) =>
{
    Console.WriteLine("Drongo is stopping...");
    return Task.CompletedTask;
});

app.AppLifetime.ApplicationStarted.Register((context) =>
{
    Console.WriteLine("Drongo has started successfully.");
    return Task.CompletedTask;
});

// Run the application, this will block until the application is stopped

// await app.RunAsync(); // This will run the application without any callbacks

await app.RunAsync((context) => 
// This callback is called when the application has started, you can use it to perform any initialization or logging
{
    Console.WriteLine("Drongo is running. Press Ctrl+C to stop.");
    return Task.CompletedTask;
},
// This callback is called when the application is stopping, you can use it to perform any cleanup or final logging
(context) =>
{
    Console.WriteLine("Drongo is shutting down...");
    return Task.CompletedTask;
});
