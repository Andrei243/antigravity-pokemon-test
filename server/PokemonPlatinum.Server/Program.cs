using PokemonPlatinumEngine.Online;

namespace PokemonPlatinum.Server;

/// <summary>
/// The online server (plan 07): <c>PokemonPlatinumServer</c> behind Caddy, as <c>docs/online/README.md</c> sets it
/// up, or <c>--local</c> on localhost for two game windows on one machine (<c>--port</c>, <c>--database</c>).
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true).AddEnvironmentVariables().Build();
        var options = LinkHost.OptionsFrom(config, args);
        WebApplication app;
        try
        {
            app = LinkHost.Build(options);
        }
        catch (InvalidOperationException e)
        {
            Console.Error.WriteLine(e.Message);
            return 2;
        }
        app.Logger.LogInformation("Link server, protocol {Protocol}, data {Data}, database {Database}{Local}",
            OnlineProtocol.Version, app.Services.GetRequiredService<Hub>().DataVersion, options.Database, options.Local ? ", local" : "");
        app.Run();
        return 0;
    }
}
