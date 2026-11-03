using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using PokemonPlatinum.Server;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinumTests.Online;

/// <summary>
/// The server's skeleton (plan 07 · O4), started in-process on a free localhost port: the health check, friend codes by
/// install token and address, link-code rooms bound to their two players, the guess limit, a code's expiry, the
/// forwarded headers and the hashed addresses, and a graceful shutdown.
/// </summary>
public class OnlineServerTests
{
    private static readonly TimeSpan Wait = OnlineTestServer.Wait;

    [Fact]
    public async Task TheHealthCheckSaysOkWithTheProtocolAndTheData()
    {
        await using var server = await OnlineTestServer.StartAsync();
        using var http = new HttpClient();
        var text = await http.GetStringAsync(new Uri(server.Address.ToString().Replace("ws://", "http://").Replace(OnlineRules.Path, "/health")));
        Assert.StartsWith($"ok {OnlineProtocol.Version} {server.Hub.DataVersion}", text);
    }

    [Fact]
    public async Task AFriendCodeIsMadeOnAFirstVisitAndKeptByTheInstallToken()
    {
        await using var server = await OnlineTestServer.StartAsync();
        string token = OnlineClient.NewToken();
        await using var first = await server.ConnectAsync("Lucas", token);
        Assert.Matches(@"^\d{4}-\d{4}-\d{4}$", first.FriendCode!);
        await using var again = await server.ConnectAsync("Lucas", token);
        Assert.Equal(first.FriendCode, again.FriendCode);
        // A new token from the same address is a second player (two behind one router)
        await using var sibling = await server.ConnectAsync("Dawn");
        Assert.NotEqual(first.FriendCode, sibling.FriendCode);
        // A game with no token is the address's own player, made once
        await using var bare = await server.ConnectAsync("Barry", token: "");
        await using var bareAgain = await server.ConnectAsync("Barry", token: "");
        Assert.Equal(bare.FriendCode, bareAgain.FriendCode);
        Assert.Equal(3, server.Hub.Store.PlayerCount);
    }

    [Fact]
    public async Task BehindTheProxyEachPlayersOwnAddressIsHashedAndNeverStored()
    {
        var server = await OnlineTestServer.StartAsync(trustedProxy: "127.0.0.1");
        async Task<string> FromAddress(string ip)
        {
            var socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("X-Forwarded-For", ip);
            await socket.ConnectAsync(server.Address, default);
            await socket.SendAsync(Encoding.UTF8.GetBytes(OnlineProtocol.Write(new Hello(server.Hub.DataVersion, "", "P"))), WebSocketMessageType.Text, true, default);
            var buffer = new byte[16384];
            var got = await socket.ReceiveAsync(buffer, default);
            var welcome = (Welcome)OnlineProtocol.Read(Encoding.UTF8.GetString(buffer, 0, got.Count));
            socket.Abort();
            return welcome.FriendCode;
        }
        // No token: the friend code follows the address, so two addresses are two players and one address one
        var a = await FromAddress("203.0.113.25");
        var b = await FromAddress("198.51.100.7");
        Assert.NotEqual(a, b);
        Assert.Equal(a, await FromAddress("203.0.113.25"));

        await server.DisposeAsync();
        var bytes = await File.ReadAllBytesAsync(server.Database);
        var all = Encoding.UTF8.GetString(bytes) + Encoding.Unicode.GetString(bytes);
        foreach (var wal in new[] { server.Database + "-wal" }.Where(File.Exists)) all += Encoding.UTF8.GetString(await File.ReadAllBytesAsync(wal));
        Assert.DoesNotContain("203.0.113.25", all);
        Assert.DoesNotContain("198.51.100.7", all);
    }

    [Fact]
    public async Task ALinkCodeWorksOnceForItsTwoPlayers()
    {
        await using var server = await OnlineTestServer.StartAsync();
        await using var a = await server.ConnectAsync("Lucas");
        await using var b = await server.ConnectAsync("Dawn");
        await using var c = await server.ConnectAsync("Barry");
        var made = await a.MakeCodeAsync(RoomKind.Trade, Wait);
        Assert.Matches(@"^\d{8}$", made.Code);
        Assert.Equal(OnlineRules.CodeSeconds, made.Seconds);

        // The maker can't join their own code
        await a.SendAsync(new JoinCode(made.Code));
        Assert.Equal(RefusalCode.InARoom, (await a.NextAsync<Refused>(Wait)).Code);

        var joined = await b.JoinAsync(made.Code, Wait);
        Assert.Equal(a.FriendCode, joined.OtherFriendCode);
        Assert.Equal("Lucas", joined.OtherName);
        Assert.Equal(b.FriendCode, (await a.NextAsync<Joined>(Wait)).OtherFriendCode);

        // A third who types the code is told the room is full
        await c.SendAsync(new JoinCode(made.Code));
        Assert.Equal(RefusalCode.RoomFull, (await c.NextAsync<Refused>(Wait)).Code);
    }

    [Fact]
    public async Task TenWrongCodesStopAnAddressJoiningForTenMinutes()
    {
        await using var server = await OnlineTestServer.StartAsync();
        await using var a = await server.ConnectAsync("Lucas");
        await using var guesser = await server.ConnectAsync("Barry");
        var made = await a.MakeCodeAsync(RoomKind.Trade, Wait);
        for (int i = 0; i < OnlineRules.WrongGuesses; i++)
        {
            await guesser.SendAsync(new JoinCode($"{i:D8}" == made.Code ? "99999999" : $"{i:D8}"));
            var refused = await guesser.NextAsync<Refused>(Wait);
            Assert.Equal(i == OnlineRules.WrongGuesses - 1 ? RefusalCode.TooManyGuesses : RefusalCode.NoSuchRoom, refused.Code);
        }
        // Even the right code is refused now, from any game at that address
        await guesser.SendAsync(new JoinCode(made.Code));
        Assert.Equal(RefusalCode.TooManyGuesses, (await guesser.NextAsync<Refused>(Wait)).Code);
        // Ten minutes on, the block is lifted (and the first code has expired, so a fresh one is made)
        server.Clock.Advance(OnlineRules.GuessWindowSeconds);
        server.Tick();
        await a.NextAsync<RoomClosed>(Wait);
        made = await a.MakeCodeAsync(RoomKind.Trade, Wait);
        var joined = await guesser.JoinAsync(made.Code, Wait);
        Assert.Equal(a.FriendCode, joined.OtherFriendCode);
    }

    [Fact]
    public async Task ACodeNobodyJoinsExpiresAfterTenMinutes()
    {
        await using var server = await OnlineTestServer.StartAsync();
        await using var a = await server.ConnectAsync("Lucas");
        await using var b = await server.ConnectAsync("Dawn");
        var made = await a.MakeCodeAsync(RoomKind.Battle, Wait);
        server.Clock.Advance(OnlineRules.CodeSeconds - 1);
        server.Tick();
        Assert.Equal(1, server.Hub.RoomCount);
        server.Clock.Advance(1);
        server.Tick();
        Assert.Equal(made.Code, (await a.NextAsync<RoomClosed>(Wait)).Code);
        await b.SendAsync(new JoinCode(made.Code));
        Assert.Equal(RefusalCode.NoSuchRoom, (await b.NextAsync<Refused>(Wait)).Code);
    }

    [Fact]
    public async Task AGameOfAnotherProtocolIsToldToUpdate()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var socket = new ClientWebSocket();
        await socket.ConnectAsync(server.Address, default);
        var text = OnlineProtocol.Write(new Hello("x", "t", "P")).Replace("\"v\":1", "\"v\":99");
        await socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, default);
        var buffer = new byte[4096];
        var got = await socket.ReceiveAsync(buffer, default);
        var refused = Assert.IsType<Refused>(OnlineProtocol.Read(Encoding.UTF8.GetString(buffer, 0, got.Count)));
        Assert.Equal(RefusalCode.Outdated, refused.Code);
        socket.Abort();
    }

    [Fact]
    public async Task AGracefulShutdownTellsEveryGame()
    {
        var server = await OnlineTestServer.StartAsync();
        await using var a = await server.ConnectAsync("Lucas");
        await server.DisposeAsync();
        await a.NextAsync<ServerClosing>(Wait);
    }
}
