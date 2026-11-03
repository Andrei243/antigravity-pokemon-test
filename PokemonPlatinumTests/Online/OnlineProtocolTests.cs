using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinumTests.Online;

/// <summary>The online protocol's messages and the server's check of a Pokémon, with no server (plan 07 · O4, O5).</summary>
public class OnlineProtocolTests
{
    private static IEnumerable<OnlineMessage> OneOfEach()
    {
        var p = Tradeable.Make("Bidoof", 12, 7);
        var receipt = new TradeReceipt(Guid.NewGuid(), p.Id!.Value, Tradeable.Make("Starly", 10, 8), "1234-5678-9012");
        yield return new Hello("abcd", "token", "Lucas");
        yield return new Welcome("1234-5678-9012", OnlineProtocol.Version, "abcd", new[] { receipt }, "12345678");
        yield return new Refused(RefusalCode.Illegal, "a line\nanother");
        yield return new MakeCode(RoomKind.Battle, LinkRoom.Default with { Level = LinkLevel.FlatHundred });
        yield return new CodeMade("12345678", RoomKind.Trade, 600);
        yield return new JoinCode("12345678");
        yield return new Joined("12345678", RoomKind.Battle, LinkRoom.Default, "1234-5678-9012", "Dawn");
        yield return new Rejoin("12345678");
        yield return new LeaveRoom();
        yield return new RoomClosed("12345678");
        yield return new OtherAway(60);
        yield return new OtherBack();
        yield return new TradeOffer(p);
        yield return new TradeOffered(true, p);
        yield return new TradeConfirm();
        yield return new TradeConfirmed(false);
        yield return new TradeCancel();
        yield return new TradeCancelled();
        yield return receipt;
        yield return new TradeAck(receipt.Receipt);
        yield return new TradeDone(receipt.Receipt);
        yield return new BattleTeam(new LinkPlayer("Lucas", new[] { new RecordedPokemon { Pokemon = p } }));
        yield return new BattleMessage("{\"v\":1}");
        yield return new BattleRecorded("1234-5678-9012", LinkOutcome.Win, LinkEnding.Forfeit);
        yield return new ServerClosing();
    }

    [Fact]
    public void EveryMessageHasAWireNameAndATestHere()
    {
        var named = typeof(OnlineMessage).GetCustomAttributes(typeof(JsonDerivedTypeAttribute), false).Cast<JsonDerivedTypeAttribute>()
            .Select(a => a.DerivedType).ToHashSet();
        var kinds = typeof(OnlineMessage).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(OnlineMessage)) && !t.IsAbstract).ToHashSet();
        Assert.Equal(kinds.OrderBy(t => t.Name), named.OrderBy(t => t.Name));
        Assert.Equal(kinds.OrderBy(t => t.Name), OneOfEach().Select(m => m.GetType()).ToHashSet().OrderBy(t => t.Name));
    }

    [Fact]
    public void EveryMessageCrossesAndComesBackTheSame()
    {
        foreach (var message in OneOfEach())
        {
            var text = OnlineProtocol.Write(message);
            Assert.Contains("\"v\":1", text);
            var back = OnlineProtocol.Read(text);
            Assert.Equal(message.GetType(), back.GetType());
            Assert.Equal(text, OnlineProtocol.Write(back));
        }
    }

    [Fact]
    public void AnotherVersionIsToldToUpdateAndNonsenseIsABadMessage()
    {
        var other = OnlineProtocol.Write(new LeaveRoom()).Replace("\"v\":1", "\"v\":2");
        Assert.Equal(RefusalCode.Outdated, Assert.Throws<OnlineProtocolException>(() => OnlineProtocol.Read(other)).Code);
        Assert.Equal(RefusalCode.BadMessage, Assert.Throws<OnlineProtocolException>(() => OnlineProtocol.Read("{\"v\":1,\"msg\":{\"type\":\"nothing\"}}")).Code);
        Assert.Equal(RefusalCode.BadMessage, Assert.Throws<OnlineProtocolException>(() => OnlineProtocol.Read("not json")).Code);
    }

    [Fact]
    public void APokemonTheDataAllowsPassesTheCheck()
    {
        foreach (var (species, level, seed) in new[] { ("Bidoof", 12, 1u), ("Garchomp", 60, 2u), ("Pikachu", 30, 3u), ("Magikarp", 5, 4u), ("Arceus", 100, 5u) })
            Assert.Empty(PokemonCheck.Faults(Tradeable.Make(species, level, seed)));
    }

    [Fact]
    public void AnImpossiblePokemonIsRefusedWithWhy()
    {
        static List<string> FaultsOf(Action<SavedPokemonData> change)
        {
            var p = Tradeable.Make("Bidoof", 20, 11);
            change(p);
            return PokemonCheck.Faults(p);
        }
        Assert.Contains(FaultsOf(p => p.Id = null), f => f.Contains("no id"));
        Assert.Contains(FaultsOf(p => p.SpeciesName = "Missingno"), f => f.Contains("no species"));
        Assert.Contains(FaultsOf(p => p.Moves[0].MoveName = "Spacial Rend"), f => f.Contains("can't know Spacial Rend"));
        Assert.Contains(FaultsOf(p => p.Moves[0].MoveName = "Not A Move"), f => f.Contains("no move"));
        Assert.Contains(FaultsOf(p => p.EvAttack = 253), f => f.Contains("effort value"));
        Assert.Contains(FaultsOf(p => { p.EvAttack = 252; p.EvHP = 252; p.EvSpeed = 10; }), f => f.Contains("past 510"));
        Assert.Contains(FaultsOf(p => p.IvSpeed = 32), f => f.Contains("IV"));
        Assert.Contains(FaultsOf(p => p.Level = 300), f => f.Contains("level 300"));
        Assert.Contains(FaultsOf(p => p.CurrentExp += 1_000_000), f => f.Contains("EXP"));
        Assert.Contains(FaultsOf(p => p.Gender = Gender.Genderless), f => f.Contains("can't be Genderless"));
        Assert.Contains(FaultsOf(p => p.Ability = "Levitate"), f => f.Contains("can't have Levitate"));
        Assert.Contains(FaultsOf(p => p.HeldItem = "Golden Banana"), f => f.Contains("no item"));
        Assert.Contains(FaultsOf(p => p.Moves[0].CurrentPP = 99), f => f.Contains("PP"));
        Assert.Contains(FaultsOf(p => p.Moves.Add(p.Moves[0])), f => f.Contains("twice"));
        Assert.Contains(FaultsOf(p => p.Friendship = 300), f => f.Contains("friendship"));
        Assert.Contains(FaultsOf(p => p.CurrentHP = 999), f => f.Contains("HP"));
    }

    [Fact]
    public void ABattleTeamNeedsNoIds()
    {
        var p = Tradeable.Make("Bidoof", 20, 11);
        p.Id = null;
        Assert.Empty(PokemonCheck.Faults(p, needsId: false));
    }
}
