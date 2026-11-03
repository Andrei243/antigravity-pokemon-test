using System;
using System.Linq;
using System.Threading.Tasks;
using PokemonPlatinum.Server;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinumTests.Online;

/// <summary>
/// The trade broker (plan 07 · O5): two fake games trade through the server in-process, each with a save of its own
/// (<see cref="FakeKeeper"/>). A whole trade, a Pokémon refused for being impossible or for coming from other data, the
/// attempts to make one Pokémon two, and the server killed between every step of a trade and started again on the same
/// file: whatever happens, each Pokémon ends in exactly one save.
/// </summary>
public class OnlineTradeTests
{
    private static readonly TimeSpan Wait = OnlineTestServer.Wait;

    /// <summary>Both offer, both confirm: each game gets its receipt.</summary>
    private static async Task<(TradeReceipt ForA, TradeReceipt ForB)> TradeAsync(OnlineClient a, OnlineClient b, SavedPokemonData x, SavedPokemonData y)
    {
        await a.OfferAsync(x, Wait);
        await b.OfferAsync(y, Wait);
        await a.SendAsync(new TradeConfirm());
        await b.SendAsync(new TradeConfirm());
        return (await a.NextAsync<TradeReceipt>(Wait), await b.NextAsync<TradeReceipt>(Wait));
    }

    [Fact]
    public async Task AWholeTradeSwapsThePokemonAndEndsDoneForBoth()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, b) = await server.RoomAsync(RoomKind.Trade);
        var x = Tradeable.Make("Bidoof", 14, 101);
        var y = Tradeable.Make("Shinx", 12, 102);
        var keepA = new FakeKeeper(x);
        var keepB = new FakeKeeper(y);

        await a.OfferAsync(x, Wait);
        // The other sees what is offered, as its summary needs
        var seen = await b.NextAsync<TradeOffered>(Wait, o => !o.Mine);
        Assert.Equal("Bidoof", seen.Pokemon.SpeciesName);
        await b.OfferAsync(y, Wait);
        await a.SendAsync(new TradeConfirm());
        Assert.False((await b.NextAsync<TradeConfirmed>(Wait)).Mine);
        await b.SendAsync(new TradeConfirm());
        var forA = await a.NextAsync<TradeReceipt>(Wait);
        var forB = await b.NextAsync<TradeReceipt>(Wait);
        Assert.Equal(forA.Receipt, forB.Receipt);
        Assert.Equal(x.Id, forA.Given);
        Assert.Equal(y.Id, forA.Received.Id);
        Assert.Equal(b.FriendCode, forA.OtherFriendCode);

        Assert.Equal(ReceiptOutcome.Applied, await a.Apply(forA, keepA));
        Assert.Equal(ReceiptOutcome.Applied, await b.Apply(forB, keepB));
        await a.NextAsync<TradeDone>(Wait);
        await b.NextAsync<TradeDone>(Wait);
        Assert.Equal(new[] { y.Id!.Value }, keepA.Held.Keys);
        Assert.Equal(new[] { x.Id!.Value }, keepB.Held.Keys);
        Assert.True(server.Hub.Store.Trade(forA.Receipt)!.Done);
        Assert.Equal(b.FriendCode, server.Hub.Store.OwnerOf(x.Id.Value));
    }

    [Fact]
    public async Task EitherMayCancelUntilBothConfirmAndANewOfferUndoesTheConfirmations()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, b) = await server.RoomAsync(RoomKind.Trade);
        await a.OfferAsync(Tradeable.Make("Bidoof", 14, 111), Wait);
        await b.OfferAsync(Tradeable.Make("Shinx", 12, 112), Wait);
        await a.SendAsync(new TradeConfirm());
        await b.NextAsync<TradeConfirmed>(Wait);
        await b.SendAsync(new TradeCancel());
        await a.NextAsync<TradeCancelled>(Wait);
        // With the offers gone, a confirmation is refused
        await a.SendAsync(new TradeConfirm());
        Assert.Equal(RefusalCode.BadMessage, (await a.NextAsync<Refused>(Wait)).Code);
        Assert.Empty(server.Hub.Store.OpenTrades());
    }

    [Fact]
    public async Task AnImpossiblePokemonIsRefusedWithItsFaults()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, _) = await server.RoomAsync(RoomKind.Trade);
        var hacked = Tradeable.Make("Bidoof", 14, 121);
        hacked.Moves[0].MoveName = "Spacial Rend";
        hacked.EvAttack = 255;
        var refused = await Assert.ThrowsAsync<OnlineRefusedException>(() => a.OfferAsync(hacked, Wait));
        Assert.Equal(RefusalCode.Illegal, refused.Refusal.Code);
        Assert.Contains("Spacial Rend", refused.Refusal.Details);
        Assert.Contains("effort", refused.Refusal.Details);
    }

    [Fact]
    public async Task AGameOnOtherDataCantTrade()
    {
        await using var server = await OnlineTestServer.StartAsync();
        await using var a = await server.ConnectAsync("Lucas", dataVersion: "0000000000000000");
        await using var b = await server.ConnectAsync("Dawn");
        Assert.Equal(server.Hub.DataVersion, a.Welcome!.DataVersion);
        var made = await a.MakeCodeAsync(RoomKind.Trade, Wait);
        await b.JoinAsync(made.Code, Wait);
        var refused = await Assert.ThrowsAsync<OnlineRefusedException>(() => a.OfferAsync(Tradeable.Make("Bidoof", 14, 131), Wait));
        Assert.Equal(RefusalCode.DataVersion, refused.Refusal.Code);
        Assert.Equal(server.Hub.DataVersion, refused.Refusal.Details);
    }

    [Fact]
    public async Task APokemonCantBeTradedTwiceOrToItself()
    {
        await using var server = await OnlineTestServer.StartAsync();
        string tokenA = OnlineClient.NewToken();
        var (a, b) = await server.RoomAsync(RoomKind.Trade, tokenA);
        var x = Tradeable.Make("Bidoof", 14, 141);
        var y = Tradeable.Make("Shinx", 12, 142);
        var keepA = new FakeKeeper(x);
        var keepB = new FakeKeeper(y);

        // Both offering the same Pokémon (one copied into two saves) is refused at the confirmation
        await a.OfferAsync(x, Wait);
        await b.OfferAsync(x, Wait);
        await a.SendAsync(new TradeConfirm());
        await b.SendAsync(new TradeConfirm());
        Assert.Equal(RefusalCode.Duplicate, (await a.NextAsync<Refused>(Wait)).Code);
        Assert.Equal(RefusalCode.Duplicate, (await b.NextAsync<Refused>(Wait)).Code);
        Assert.Empty(server.Hub.Store.OpenTrades());

        var (forA, forB) = await TradeAsync(a, b, x, y);
        // A receipt applied a second time (sent again after a crash) changes nothing
        Assert.Equal(ReceiptOutcome.Applied, await a.Apply(forA, keepA));
        Assert.Equal(ReceiptOutcome.AlreadyApplied, await a.Apply(forA, keepA));
        Assert.Equal(1, keepA.Saves);
        Assert.Equal(ReceiptOutcome.Applied, await b.Apply(forB, keepB));

        // A reloads a save from before the trade and offers the Bidoof again, to someone else: the log knows whose it is now
        await a.SendAsync(new LeaveRoom());
        await b.NextAsync<RoomClosed>(Wait);
        await using var c = await server.ConnectAsync("Barry");
        var made = await a.MakeCodeAsync(RoomKind.Trade, Wait);
        await c.JoinAsync(made.Code, Wait);
        var refused = await Assert.ThrowsAsync<OnlineRefusedException>(() => a.OfferAsync(x, Wait));
        Assert.Equal(RefusalCode.Duplicate, refused.Refusal.Code);
    }

    [Fact]
    public async Task AGameThatCrashedBeforeItsAcknowledgementFinishesTheTradeOnItsNextConnection()
    {
        await using var server = await OnlineTestServer.StartAsync();
        string tokenA = OnlineClient.NewToken();
        var (a, b) = await server.RoomAsync(RoomKind.Trade, tokenA);
        var x = Tradeable.Make("Bidoof", 14, 151);
        var y = Tradeable.Make("Shinx", 12, 152);
        var keepA = new FakeKeeper(x);
        var keepB = new FakeKeeper(y);
        var (forA, forB) = await TradeAsync(a, b, x, y);

        // A's game swaps and saves, then dies before it tells the server
        keepA.Swap(forA.Given, forA.Received);
        a.Abort();
        await b.Apply(forB, keepB);

        await using var again = await server.ConnectAsync("Lucas", tokenA);
        var pending = Assert.Single(again.Welcome!.Pending);
        Assert.Equal(forA.Receipt, pending.Receipt);
        Assert.Equal(ReceiptOutcome.AlreadyApplied, await again.Apply(pending, keepA));
        await again.NextAsync<TradeDone>(Wait);
        Assert.True(server.Hub.Store.Trade(forA.Receipt)!.Done);
        Assert.Equal(new[] { y.Id!.Value }, keepA.Held.Keys);
    }

    [Fact]
    public async Task AReceiptForAnotherSaveIsNeitherAppliedNorAcknowledged()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, b) = await server.RoomAsync(RoomKind.Trade);
        var (forA, _) = await TradeAsync(a, b, Tradeable.Make("Bidoof", 14, 161), Tradeable.Make("Shinx", 12, 162));
        var otherSave = new FakeKeeper(Tradeable.Make("Starly", 9, 163));
        Assert.Equal(ReceiptOutcome.NotThisSave, await a.Apply(forA, otherSave));
        Assert.Single(otherSave.Held);
        Assert.Single(server.Hub.Store.PendingFor(a.FriendCode!));
    }

    public static TheoryData<string> Steps => new(TradeSteps.All);

    /// <summary>
    /// The server is killed at a step of a trade, as a process is killed: it writes nothing more and every connection
    /// drops. Another starts on the same file, both games come back with the same install tokens, apply what the
    /// welcome says still waits, and every Pokémon is in exactly one save: swapped if the trade was written before the
    /// kill, where it was if not. Nothing is left open.
    /// </summary>
    [Theory]
    [MemberData(nameof(Steps))]
    public async Task AServerKilledAtAnyStepOfATradeLosesAndDuplicatesNothing(string step)
    {
        string database = OnlineTestServer.NewDatabase();
        string tokenA = OnlineClient.NewToken(), tokenB = OnlineClient.NewToken();
        var x = Tradeable.Make("Bidoof", 14, 171);
        var y = Tradeable.Make("Shinx", 12, 172);
        var keepA = new FakeKeeper(x);
        var keepB = new FakeKeeper(y);

        var first = await OnlineTestServer.StartAsync(database, crashAt: step);
        var (a, b) = await first.RoomAsync(RoomKind.Trade, tokenA, tokenB);
        await a.OfferAsync(x, Wait);
        await b.OfferAsync(y, Wait);
        await a.SendAsync(new TradeConfirm());
        await b.NextAsync<TradeConfirmed>(Wait);
        await b.SendAsync(new TradeConfirm());

        // Whatever came before the kill is taken as the games would take it
        if (step is TradeSteps.AfterFirstAck or TradeSteps.AfterSecondAck)
        {
            var forA = await a.NextAsync<TradeReceipt>(Wait);
            var forB = await b.NextAsync<TradeReceipt>(Wait);
            await a.Apply(forA, keepA);
            if (step == TradeSteps.AfterSecondAck)
            {
                await a.NextAsync<TradeDone>(Wait);
                await b.Apply(forB, keepB);
            }
        }
        await WaitUntil(() => first.Hub.Dead);
        await WaitUntil(() => !a.Connected && !b.Connected);
        await first.DisposeAsync();

        await using var second = await OnlineTestServer.StartAsync(database);
        await using var a2 = await second.ConnectAsync("Lucas", tokenA);
        await using var b2 = await second.ConnectAsync("Dawn", tokenB);
        Assert.Equal(a.FriendCode, a2.FriendCode);
        foreach (var (client, keeper) in new[] { (a2, keepA), (b2, keepB) })
            foreach (var receipt in client.Welcome!.Pending)
            {
                Assert.NotEqual(ReceiptOutcome.NotThisSave, await client.Apply(receipt, keeper));
                await client.NextAsync<TradeDone>(Wait);
            }

        bool written = step != TradeSteps.BeforeCommit;
        Assert.Equal(new[] { written ? y.Id!.Value : x.Id!.Value }, keepA.Held.Keys);
        Assert.Equal(new[] { written ? x.Id!.Value : y.Id!.Value }, keepB.Held.Keys);
        Assert.Empty(second.Hub.Store.OpenTrades());
        Assert.True(keepA.Saves + keepB.Saves == (written ? 2 : 0), $"saves {keepA.Saves} + {keepB.Saves}");
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 2000 && !condition(); i++) await Task.Delay(10);
        Assert.True(condition());
    }
}
