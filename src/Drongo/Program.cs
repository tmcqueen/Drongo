using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Drongo;
using Drongo.Core.Dialogs;
using Drongo.Core.Parsing;
using Drongo.Core.Registration;
using Drongo.Core.Transport;
using Drongo.Hosting;

var builder = DrongoApplication.CreateBuilder();

builder.Services.AddSingleton<IDialogFactory, DialogFactory>();
builder.Services.AddSingleton<ISipParser, SipParser>();
builder.Services.AddSingleton<IRegistrar, InMemoryRegistrar>();

builder.Services.AddSingleton<IUdpTransport, UdpTransport>();

builder.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

builder.MapInvite(async context =>
{
    var logger = context.Items.TryGetValue("logger", out var l) ? (ILogger)l! : null;
    logger?.LogInformation("Received INVITE for {Uri}", context.Request.RequestUri);
    
    await context.SendResponseAsync(180, "Ringing");
    await context.SendResponseAsync(200, "OK");
});

builder.MapRegister(async context =>
{
    var logger = context.Items.TryGetValue("logger", out var l) ? (ILogger)l! : null;
    logger?.LogInformation("Processing REGISTER for {Uri}", context.Request.To);
});

var services = builder.Services.BuildServiceProvider();
var app = services.GetRequiredService<DrongoApplication>();

Console.WriteLine("Drongo - SIP Runtime");
Console.WriteLine("Starting on 0.0.0.0:5060...");

await app.RunAsync(new IPEndPoint(IPAddress.Any, 5060));
