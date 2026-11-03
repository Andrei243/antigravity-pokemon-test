using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.HttpOverrides;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinum.Server;

/// <summary>
/// The web host (plan 07 · O4): Kestrel with the forwarded-headers middleware (believing only the proxy named, Caddy
/// on the same machine, so a player's own address is the one hashed), <c>/health</c>, the WebSocket at
/// <see cref="OnlineRules.Path"/>, the hub's clock and a graceful shutdown. <see cref="Build"/> is what the program
/// runs and what the tests start in-process on a free port.
/// </summary>
public static class LinkHost
{
    /// <summary>The settings from the configuration and the command line.</summary>
    public static ServerOptions OptionsFrom(IConfiguration config, string[] args)
    {
        var options = new ServerOptions();
        config.GetSection("LinkServer").Bind(options);
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--local": options.Local = true; break;
                case "--port" when i + 1 < args.Length && int.TryParse(args[i + 1], out int port): options.Port = port; i++; break;
                case "--database" when i + 1 < args.Length: options.Database = args[++i]; break;
            }
        }
        return options;
    }

    public static WebApplication Build(ServerOptions options, string[]? args = null)
    {
        args ??= Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(options.IpHashSecret))
        {
            if (!options.Local)
                throw new InvalidOperationException("LinkServer:IpHashSecret is not set (the setup guide's step 7 makes it); --local makes one for the run");
            options.IpHashSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });
        builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(10));
        if (options.Local) builder.WebHost.UseUrls($"http://127.0.0.1:{options.Port}");
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
            if (!string.IsNullOrWhiteSpace(options.TrustedProxy) && IPAddress.TryParse(options.TrustedProxy, out var proxy))
                o.KnownProxies.Add(proxy);
            o.ForwardLimit = 1;
        });

        var store = new Store(options.Database, options.IpHashSecret!);
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(sp => new Hub(store, options, sp.GetRequiredService<ILoggerFactory>().CreateLogger("LinkServer")));
        if (options.Ticking) builder.Services.AddHostedService<Ticker>();

        var app = builder.Build();
        app.UseForwardedHeaders();
        app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });
        var hub = app.Services.GetRequiredService<Hub>();
        app.Lifetime.ApplicationStopping.Register(() => hub.ShutDownAsync().Wait(TimeSpan.FromSeconds(5)));
        app.Lifetime.ApplicationStopped.Register(store.Dispose);

        app.MapGet("/health", () => Results.Text($"ok {OnlineProtocol.Version} {hub.DataVersion}\n"));
        app.Map(OnlineRules.Path, async (HttpContext http) =>
        {
            if (!http.WebSockets.IsWebSocketRequest)
            {
                http.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            var address = http.Connection.RemoteIpAddress ?? IPAddress.Loopback;
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            using var socket = await http.WebSockets.AcceptWebSocketAsync();
            var connection = new Connection(socket, store.Hash("ip", address.ToString()), IPAddress.IsLoopback(address));
            if (!hub.Open(connection))
            {
                connection.Send(new Refused(hub.Closing ? RefusalCode.Closing : RefusalCode.TooFast));
                connection.StartWriting(http.RequestAborted);
                await connection.CloseAsync("refused");
                return;
            }
            connection.StartWriting(http.RequestAborted);
            try
            {
                // A shutdown closes the connection from the hub (ServerClosing first); the loop ends as the game answers the close
                while (await connection.ReadAsync(http.RequestAborted) is { } text)
                    hub.Receive(connection, text);
            }
            finally
            {
                hub.Closed(connection);
                await connection.CloseAsync("bye");
            }
        });
        return app;
    }

    /// <summary>The address the host listens on, once started (a test's free port).</summary>
    public static Uri Address(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()?.Addresses;
        var url = addresses?.FirstOrDefault() ?? app.Urls.First();
        return new Uri(url.Replace("http://", "ws://").TrimEnd('/') + OnlineRules.Path);
    }
}

/// <summary>The hub's clock: a tick four times a second.</summary>
public sealed class Ticker : BackgroundService
{
    private readonly Hub hub;

    public Ticker(Hub hub) => this.hub = hub;

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        try
        {
            while (await timer.WaitForNextTickAsync(stop)) hub.Tick();
        }
        catch (OperationCanceledException) { }
    }
}
