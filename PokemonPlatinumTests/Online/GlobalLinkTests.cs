using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests.Online;

/// <summary>
/// The Global Link counter (plan 07 · O5 and O7, the game's half): two visits to the counter, each a
/// <see cref="GlobalLinkSession"/> on a save of its own (<see cref="FakeKeeper"/>), against the server started
/// in-process on localhost. The session is moved on a frame at a time as the game moves it, and its lines are the keys
/// the screen draws. Nothing here reaches beyond 127.0.0.1.
/// </summary>
public class GlobalLinkTests
{
    private static readonly TimeSpan Wait = OnlineTestServer.Wait;

    private static GlobalLinkSession Visit(OnlineTestServer server, FakeKeeper keeper, string name, string? token = null)
    {
        token ??= OnlineClient.NewToken();
        return new GlobalLinkSession(() => new OnlineClient(server.Address, token, name), keeper);
    }

    /// <summary>Moves the sessions on a frame at a time, as the game does, until <paramref name="done"/> holds.</summary>
    private static async Task Until(Func<bool> done, params GlobalLinkSession[] sessions)
    {
        var clock = Stopwatch.StartNew();
        while (!done())
        {
            if (clock.Elapsed > Wait) throw new TimeoutException($"Still waiting: {string.Join(", ", sessions.Select(s => $"{s.Phase} '{s.Line}'"))}");
            foreach (var s in sessions) s.Update(1f / 60f);
            await Task.Delay(5);
        }
    }

    private static async Task<(GlobalLinkSession A, GlobalLinkSession B)> Linked(OnlineTestServer server, FakeKeeper keepA, FakeKeeper keepB)
    {
        var a = Visit(server, keepA, "Lucas");
        var b = Visit(server, keepB, "Dawn");
        await Until(() => a.Phase == LinkPhase.Menu && b.Phase == LinkPhase.Menu, a, b);
        Assert.Matches(@"^\d{4}-\d{4}-\d{4}$", a.FriendCode);
        Assert.True(a.MakeCode());
        await Until(() => a.Phase == LinkPhase.WaitingForOther, a);
        Assert.Equal(OnlineRules.CodeDigits, a.Code!.Length);
        Assert.True(b.EnterCode());
        foreach (char c in a.Code) Assert.True(b.TypeDigit(c - '0'));
        Assert.False(b.TypeDigit(1));
        Assert.True(b.SubmitCode());
        await Until(() => a.Phase == LinkPhase.Room && b.Phase == LinkPhase.Room, a, b);
        return (a, b);
    }

    [Fact]
    public async Task ACodeMadeAndTypedLinksTwoVisitsAndATradeIsWrittenIntoBothSaves()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var x = Tradeable.Make("Bidoof", 14, 301);
        var y = Tradeable.Make("Shinx", 12, 302);
        var keepA = new FakeKeeper(x);
        var keepB = new FakeKeeper(y);
        var (a, b) = await Linked(server, keepA, keepB);
        Assert.Equal("Dawn", a.OtherName);
        Assert.Equal("Lucas", b.OtherName);
        Assert.Equal(b.FriendCode, a.OtherFriendCode);
        Assert.Equal("globalLink.line.choose", a.Line);

        // Nobody may agree before both have offered
        Assert.False(a.ConfirmTrade());
        Assert.True(a.Offer(x));
        await Until(() => a.MyOffer != null && b.TheirOffer != null, a, b);
        Assert.Equal("globalLink.line.theyOffered", b.Line);
        Assert.False(a.MayConfirm);
        Assert.True(b.Offer(y));
        await Until(() => a.TheirOffer != null && b.MyOffer != null, a, b);
        Assert.True(a.MayConfirm);
        Assert.True(a.ConfirmTrade());
        await Until(() => b.TheirsConfirmed, a, b);
        Assert.Equal("globalLink.line.theyConfirmed", b.Line);
        Assert.True(b.ConfirmTrade());
        await Until(() => a.Phase == LinkPhase.Traded && b.Phase == LinkPhase.Traded, a, b);

        Assert.Equal(y.Id, a.Received!.Id);
        Assert.Equal(x.Id, b.Received!.Id);
        Assert.Equal(new[] { y.Id!.Value }, keepA.Held.Keys);
        Assert.Equal(new[] { x.Id!.Value }, keepB.Held.Keys);
        Assert.Equal(1, keepA.Saves);
        Assert.Equal("globalLink.line.traded", a.Line);
        // Both acknowledged once saved: the trade is closed on the server
        await Until(() => server.Hub.Store.OpenTrades().Count == 0, a, b);

        // Read on: back in the room, both offers gone, to trade again
        a.Dismiss();
        Assert.Equal(LinkPhase.Room, a.Phase);
        Assert.Null(a.MyOffer);
        Assert.Null(a.TheirOffer);
        a.Close();
        b.Close();
        Assert.Equal(LinkPhase.Closed, a.Phase);
    }

    [Fact]
    public async Task EitherMayCallATradeOffAndBothChooseAgain()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var x = Tradeable.Make("Bidoof", 14, 311);
        var y = Tradeable.Make("Shinx", 12, 312);
        var (a, b) = await Linked(server, new FakeKeeper(x), new FakeKeeper(y));
        a.Offer(x);
        b.Offer(y);
        await Until(() => a.MayConfirm && b.MayConfirm, a, b);
        Assert.True(b.CancelTrade());
        await Until(() => a.MyOffer == null && a.TheirOffer == null && b.MyOffer == null, a, b);
        Assert.Equal("globalLink.line.cancelled", a.Line);
        Assert.Equal(LinkPhase.Room, a.Phase);
    }

    [Fact]
    public async Task AnImpossiblePokemonIsRefusedWithItsFaultsAndTheRoomStays()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var hacked = Tradeable.Make("Bidoof", 14, 321);
        hacked.EvAttack = 255;
        var (a, b) = await Linked(server, new FakeKeeper(hacked), new FakeKeeper(Tradeable.Make("Shinx", 12, 322)));
        Assert.True(a.Offer(hacked));
        await Until(() => !a.Checking, a, b);
        Assert.Equal("globalLink.refused.illegal", a.Line);
        Assert.False(string.IsNullOrEmpty(a.Details));
        Assert.Null(a.MyOffer);
        Assert.Equal(LinkPhase.Room, a.Phase);
    }

    [Fact]
    public async Task AWrongCodeSendsThePlayerBackToTheDigits()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var b = Visit(server, new FakeKeeper(), "Dawn");
        await Until(() => b.Phase == LinkPhase.Menu, b);
        b.EnterCode();
        foreach (char c in "12345678") b.TypeDigit(c - '0');
        Assert.True(b.Erase());
        Assert.False(b.SubmitCode());
        b.TypeDigit(9);
        Assert.True(b.SubmitCode());
        await Until(() => b.Phase == LinkPhase.EnterCode, b);
        Assert.Equal("globalLink.refused.noSuchRoom", b.Line);
        Assert.Equal("", b.Typed);
        b.Close();
    }

    [Fact]
    public async Task ACodeNobodyTypesRunsOutOnTheGamesOwnTime()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var a = Visit(server, new FakeKeeper(), "Lucas");
        await Until(() => a.Phase == LinkPhase.Menu, a);
        a.MakeCode();
        await Until(() => a.Phase == LinkPhase.WaitingForOther, a);
        Assert.Equal(OnlineRules.CodeSeconds, a.CodeSecondsLeft);
        a.Update(OnlineRules.CodeSeconds - 1);
        Assert.Equal(LinkPhase.WaitingForOther, a.Phase);
        a.Update(2);
        Assert.Equal(LinkPhase.Menu, a.Phase);
        Assert.Equal("globalLink.line.codeExpired", a.Line);
        Assert.Null(a.Code);
        // The room is gone on the server too: a new code may be made
        Assert.True(a.MakeCode());
        await Until(() => a.Phase == LinkPhase.WaitingForOther, a);
        a.Close();
    }

    [Fact]
    public async Task WhenTheOtherLeavesTheRoomThePlayerIsBackAtTheMenu()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, b) = await Linked(server, new FakeKeeper(), new FakeKeeper());
        Assert.True(b.Leave());
        Assert.Equal(LinkPhase.Menu, b.Phase);
        await Until(() => a.Phase == LinkPhase.Menu, a, b);
        Assert.Equal("globalLink.line.otherLeft", a.Line);
        Assert.Null(a.OtherName);
    }

    [Fact]
    public async Task ATradeLeftUnappliedIsFinishedAtTheNextVisit()
    {
        await using var server = await OnlineTestServer.StartAsync();
        string tokenB = OnlineClient.NewToken();
        var (a, b) = await server.RoomAsync(RoomKind.Trade, tokenB: tokenB);
        var x = Tradeable.Make("Bidoof", 14, 331);
        var y = Tradeable.Make("Shinx", 12, 332);
        await a.OfferAsync(x, Wait);
        await b.OfferAsync(y, Wait);
        await a.SendAsync(new TradeConfirm());
        await b.SendAsync(new TradeConfirm());
        await a.Apply(await a.NextAsync<TradeReceipt>(Wait), new FakeKeeper(x));
        await b.NextAsync<TradeReceipt>(Wait);
        // B's game dies before it writes its receipt
        b.Abort();
        server.Clock.Advance(OnlineRules.RejoinSeconds + 1);
        server.Tick();

        var keepB = new FakeKeeper(y);
        var visit = Visit(server, keepB, "Dawn", tokenB);
        await Until(() => visit.Phase == LinkPhase.Menu && visit.Caught == 1, visit);
        Assert.Equal(new[] { x.Id!.Value }, keepB.Held.Keys);
        Assert.Equal("globalLink.line.finished", visit.Line);
        await Until(() => server.Hub.Store.OpenTrades().Count == 0, visit);
        Assert.Empty(server.Hub.Store.PendingFor(visit.FriendCode!));
        visit.Close();
        await a.DisposeAsync();
    }

    [Fact]
    public void WithNoServerTheCounterSaysTheLinkIsDown()
    {
        var visit = new GlobalLinkSession(null, new FakeKeeper());
        Assert.Equal(LinkPhase.Failed, visit.Phase);
        Assert.Equal("globalLink.error.noServer", visit.Line);
        Assert.True(visit.MayClose);
        visit.Close();
        Assert.Equal(LinkPhase.Closed, visit.Phase);
    }

    [Fact]
    public async Task AServerNobodyAnswersAtIsSaidSo()
    {
        // A port on this machine nothing listens on: the connection is refused at once, nothing leaves localhost
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var visit = new GlobalLinkSession(() => new OnlineClient(new Uri($"ws://127.0.0.1:{port}/link"), OnlineClient.NewToken(), "Lucas"), new FakeKeeper());
        await Until(() => visit.Phase == LinkPhase.Failed, visit);
        Assert.Equal("globalLink.error.unreachable", visit.Line);
    }

    [Fact]
    public void EveryRefusalHasItsLineInEnglishAndRomanian()
    {
        foreach (var code in Enum.GetValues<RefusalCode>())
        {
            string key = $"globalLink.refused.{GlobalLinkSession.RefusedKey(code)}";
            Assert.True(Text.Has(key), key);
        }
    }

    [Fact]
    public void TheInstallTokenIsMadeOnceAndKept()
    {
        var folder = Path.Combine(Path.GetTempPath(), "pokemon-online-tests", Guid.NewGuid().ToString("N"));
        var file = Path.Combine(folder, SavePaths.InstallTokenName);
        string first = OnlineIdentity.TokenIn(file);
        Assert.True(OnlineIdentity.IsToken(first));
        Assert.Equal(first, OnlineIdentity.TokenIn(file));
        File.WriteAllText(file, "not a token");
        string second = OnlineIdentity.TokenIn(file);
        Assert.NotEqual(first, second);
        Assert.Equal(second, File.ReadAllText(file));
        Directory.Delete(folder, true);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("https://link.example/link", null)]
    [InlineData("not an address", null)]
    [InlineData("wss://link.example/link", "wss://link.example/link")]
    [InlineData("wss://link.example", "wss://link.example/link")]
    [InlineData("ws://127.0.0.1:5080/link", "ws://127.0.0.1:5080/link")]
    public void TheServersAddressIsAWebSocketsOrNothing(string? setting, string? expected)
    {
        Assert.Equal(expected, OnlineIdentity.ServerOf(setting)?.ToString());
    }

    [Fact]
    public void TheDeskOpensTheCounterOnlyWithAServerAndSaysItsOwnLinesOtherwise()
    {
        var desk = new PokemonPlatinumEngine.Overworld.NPC
        {
            Id = "teala_west",
            Name = "Attendant",
            DialogLines = new() { "The link is down." }
        };
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("common.GlobalLink")!, desk);
        runner.RunToEnd();
        Assert.DoesNotContain(host.Log, l => l.StartsWith("open GlobalLink"));
        Assert.Contains(host.Transcript, t => t.Text == "The link is down.");

        host = new HeadlessScriptHost { OnlineServerSet = true };
        runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("common.GlobalLink")!, desk);
        runner.RunToEnd();
        Assert.Contains("open GlobalLink", host.Log);
    }

    [Fact]
    public void EveryPokemonCenterUpstairsDeskOfTheUnionRoomIsTheCounter()
    {
        var desks = MapDatabase.MapNames.Where(n => n.EndsWith("PokemonCenter2F", StringComparison.Ordinal))
            .Select(MapDatabase.Get).SelectMany(m => m.Everyone).Where(p => p.Id == "teala_west").ToList();
        Assert.True(desks.Count >= 18);
        Assert.All(desks, d => Assert.Equal("GlobalLink", d.Script));
    }

    [Fact]
    public void TheScreensKeypadTypesTheDigitsOneToNineThenZero()
    {
        Assert.Equal(1, GlobalLinkScreen.DigitOf(0));
        Assert.Equal(9, GlobalLinkScreen.DigitOf(8));
        Assert.Equal(-1, GlobalLinkScreen.DigitOf(GlobalLinkScreen.EraseKey));
        Assert.Equal(0, GlobalLinkScreen.DigitOf(GlobalLinkScreen.ZeroKey));
        Assert.Equal(-1, GlobalLinkScreen.DigitOf(GlobalLinkScreen.OkKey));
        Assert.Equal(GlobalLinkScreen.VisibleRows, ModernUiRows());
    }

    private static int ModernUiRows() => ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop);
}
