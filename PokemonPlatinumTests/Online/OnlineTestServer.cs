using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PokemonPlatinum.Server;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinumTests.Online;

/// <summary>A clock a test moves by hand.</summary>
internal sealed class ManualClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
    public void Advance(double seconds) => Now = Now.AddSeconds(seconds);
}

/// <summary>
/// The online server started in-process on a free localhost port (plan 07 · O4), on a database file of the test's own
/// that outlives the server, so a test can kill it (<see cref="ServerOptions.CrashAt"/>) and start another on the same
/// file. The clock is the test's and nothing ticks unless the test says <see cref="Tick"/>.
/// </summary>
internal sealed class OnlineTestServer : IAsyncDisposable
{
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(20);

    public WebApplication App { get; }
    public Hub Hub { get; }
    public Uri Address { get; }
    public ManualClock Clock { get; }
    public string Database { get; }
    public string Secret { get; }

    private OnlineTestServer(WebApplication app, ManualClock clock, string database, string secret)
    {
        App = app;
        Clock = clock;
        Database = database;
        Secret = secret;
        Hub = app.Services.GetRequiredService<Hub>();
        Address = LinkHost.Address(app);
    }

    /// <summary>A new database file in a folder of the test's own.</summary>
    public static string NewDatabase()
    {
        var folder = Path.Combine(Path.GetTempPath(), "pokemon-online-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "link.db");
    }

    public static async Task<OnlineTestServer> StartAsync(string? database = null, string? crashAt = null, ManualClock? clock = null,
        string? trustedProxy = null, string secret = "a-test-secret")
    {
        clock ??= new ManualClock();
        database ??= NewDatabase();
        var options = new ServerOptions
        {
            Local = true,
            Port = 0,
            Database = database,
            IpHashSecret = secret,
            Clock = clock,
            CrashAt = crashAt,
            Ticking = false,
            TrustedProxy = trustedProxy
        };
        var app = LinkHost.Build(options);
        await app.StartAsync();
        return new OnlineTestServer(app, clock, database, secret);
    }

    public void Tick() => Hub.Tick();

    /// <summary>A game: a client with an install token of its own (or the one given), connected and welcomed.</summary>
    public async Task<OnlineClient> ConnectAsync(string name, string? token = null, string? dataVersion = null)
    {
        var client = new OnlineClient(Address, token ?? OnlineClient.NewToken(), name, dataVersion);
        await client.ConnectAsync(Wait);
        return client;
    }

    /// <summary>Two games in a room of the kind, made by the first and joined by the second.</summary>
    public async Task<(OnlineClient A, OnlineClient B)> RoomAsync(RoomKind kind, string? tokenA = null, string? tokenB = null,
        PokemonPlatinumEngine.Battle.Sim.Link.LinkRoom? rules = null)
    {
        var a = await ConnectAsync("Lucas", tokenA);
        var b = await ConnectAsync("Dawn", tokenB);
        var made = await a.MakeCodeAsync(kind, Wait, rules);
        await b.JoinAsync(made.Code, Wait);
        await a.NextAsync<Joined>(Wait);
        return (a, b);
    }

    public async ValueTask DisposeAsync()
    {
        try { await App.StopAsync(); } catch (OperationCanceledException) { }
        await App.DisposeAsync();
    }
}

/// <summary>A save as a trade sees it (plan 07 · O5): the Pokémon it holds by id, and the swap, written at once.</summary>
internal sealed class FakeKeeper : ITradeKeeper
{
    public Dictionary<Guid, SavedPokemonData> Held { get; } = new();
    public int Saves { get; private set; }

    public FakeKeeper(params SavedPokemonData[] pokemon)
    {
        foreach (var p in pokemon) Held[p.Id!.Value] = p;
    }

    public bool Holds(Guid id) => Held.ContainsKey(id);

    public void Swap(Guid given, SavedPokemonData received)
    {
        Held.Remove(given);
        Held[received.Id!.Value] = received;
        Saves++;
    }
}

internal static class Tradeable
{
    /// <summary>A Pokémon that is what the data says it may be, with its own id.</summary>
    public static SavedPokemonData Make(string species, int level, uint seed)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level, new BattleRandom(seed));
        p.Id = Guid.Parse($"{seed:x8}-504b-8001-8000-0000{seed:x8}");
        return SavedPokemonData.FromPokemon(p);
    }
}
