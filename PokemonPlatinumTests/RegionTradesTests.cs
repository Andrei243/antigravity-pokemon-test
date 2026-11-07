using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE2: the regions' own trades. <c>Data/trades.json</c> is written by <c>tools/DataImporter -- --trades</c> from
/// the decompilations' in-game trade tables; every trade in it is some person's, in their region's own place, and each is
/// played here headless: the wrong species refused with nothing changed, the right one received with its numbers.
/// </summary>
public class RegionTradesTests
{
    private static readonly DateTime Day = new(2026, 11, 7);

    private static Pokemon Mon(string species, int level, Gender gender = Gender.Male) =>
        new(PokemonDatabase.Get(species)!, level, gender, Nature.Hardy, false);

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var m in members) party.Add(m);
        return party;
    }

    /// <summary>Every script that makes a trade, with the trades it makes in the order it makes them.</summary>
    private static List<(Script Script, List<string> Trades)> TradeScripts() =>
        ScriptLibrary.Default.All
            .Select(s => (Script: s, Trades: s.Everything().Where(i => i.Op == Op.Trade).Select(i => i.Name).ToList()))
            .Where(t => t.Trades.Count > 0)
            .ToList();

    private static string FlagOf(string id) => "FLAG_TRADED_FOR_" + id.ToUpperInvariant();

    [Fact]
    public void EveryTradeIsMadeOfRealSpeciesItemsAndAbilities()
    {
        Assert.Equal(NpcTrades.All.Count, NpcTrades.All.Select(t => t.Id).Distinct().Count());
        foreach (var t in NpcTrades.All)
        {
            Assert.NotNull(PokemonDatabase.Get(t.Wants));
            var given = PokemonDatabase.Get(t.Gives);
            Assert.NotNull(given);
            Assert.True(string.IsNullOrEmpty(t.Item) || ItemDatabase.Get(t.Item) != null, $"{t.Id}: item {t.Item}");
            Assert.Equal(6, t.Ivs.Length);
            Assert.All(t.Ivs, iv => Assert.InRange(iv, 0, 31));
            Assert.True(given!.Abilities.Contains(t.Ability) || given.HiddenAbility == t.Ability, $"{t.Id}: {t.Ability}");
            Assert.NotNull(t.Region);
            // The personality gives the nature, and the gender where the species draws one
            Assert.Equal(t.Nature, Personality.NatureOf(t.Personality));
            if (Personality.HasRandomGender(given)) Assert.Equal(t.Gender, Personality.GenderOf(given, t.Personality));
        }
    }

    [Fact]
    public void EveryTradeIsSomeonesAndEveryTradeScriptNamesATradeThatExists()
    {
        var made = TradeScripts().SelectMany(t => t.Trades).ToList();
        Assert.All(made, id => Assert.NotNull(NpcTrades.Get(id)));
        var nobody = NpcTrades.All.Select(t => t.Id).Where(id => !made.Contains(id)).ToList();
        Assert.True(nobody.Count == 0, "Trades no person makes: " + string.Join(", ", nobody));
    }

    [Fact]
    public void EveryTradeIsMadeInItsOwnRegion()
    {
        // The script's place is an area of the trade's region: its overlay lives in that region's world folder
        foreach (var (script, trades) in TradeScripts())
            foreach (string id in trades)
            {
                string region = NpcTrades.Get(id)!.Region!;
                string file = script.File.Replace('\\', '/');
                string area = System.IO.Path.GetFileNameWithoutExtension(file);
                string overlay = System.IO.Path.Combine("world", region.ToLowerInvariant(), "overlays", area + ".json");
                string room = System.IO.Path.Combine("maps", area + ".json");
                Assert.True(System.IO.File.Exists(GameDataFiles.PathOf(overlay)) || System.IO.File.Exists(GameDataFiles.PathOf(room)), $"{id}: {overlay}");
            }
    }

    [Fact]
    public void EachTradeIsPlayedWithTheWrongSpeciesRefusedAndTheRightOneReceived()
    {
        int played = 0;
        // Route 7's hiker walks people about: a map of its own is needed to play it, and the story tests of Unova do
        var playedElsewhere = new List<string>();
        foreach (var (script, trades) in TradeScripts())
        {
            for (int k = 0; k < trades.Count; k++)
            {
                var trade = NpcTrades.Get(trades[k])!;
                // The earlier trades of the same script are done already (the Lass of Accumula Town offers two in turn)
                string[] done = trades.Take(k).Select(FlagOf).ToArray();
                var library = ScriptLibrary.Default;

                // Wrong: a Pokémon that is not the one asked for, nothing changes
                var wrong = new HeadlessScriptHost(party: PartyOf(Mon("Rattata", 12), Mon("Pidgey", 9))) { PokemonChoice = 0 };
                foreach (string f in done) wrong.Story.Set(f);
                var r1 = new ScriptRunner(library, wrong);
                r1.Start(script, new NPC { Name = "Trader" });
                try { r1.RunToEnd(); }
                catch (ScriptException e) when (e.Message.Contains("nobody")) { playedElsewhere.Add(trade.Id); continue; }
                Assert.False(wrong.Story.Has(FlagOf(trade.Id)), $"{trade.Id}: a Rattata was taken");
                Assert.DoesNotContain(wrong.Party.Members, m => m.Species.Name == trade.Gives);
                Assert.Equal("Rattata", wrong.Party.Members[0].Species.Name);

                // Right: the species asked for (a female where the trade asks for one), at the level it had
                var gender = trade.WantsGender ?? Gender.Male;
                var wanted = Mon(trade.Wants, 23, gender);
                var host = new HeadlessScriptHost(party: PartyOf(Mon("Rattata", 12), wanted)) { PokemonChoice = 1 };
                foreach (string f in done) host.Story.Set(f);
                var r2 = new ScriptRunner(library, host);
                r2.Start(script, new NPC { Name = "Trader" });
                r2.RunToEnd();
                var got = host.Party.Members[1];
                Assert.True(got.Species.Name == trade.Gives, $"{trade.Id}: not made ({string.Join("; ", host.Transcript.TakeLast(3))})");
                Assert.Equal((trade.Gives, trade.Nickname, 23), (got.Species.Name, got.Nickname, got.Level));
                Assert.Equal((trade.Nature, trade.Ability), (got.Nature, got.AbilityName));
                Assert.Equal(trade.Ivs, new[] { got.IvHP, got.IvAttack, got.IvDefense, got.IvSpeed, got.IvSpAttack, got.IvSpDefense });
                Assert.Equal(trade.TrainerName, got.OriginalTrainer?.Name);
                played++;
            }
        }
        Assert.Equal(new[] { "gigalith" }, playedElsewhere.OrderBy(i => i).ToArray());
        Assert.Equal(NpcTrades.All.Count - playedElsewhere.Count, played);
    }

    [Fact]
    public void BlackthornsGirlTakesOnlyAFemaleDragonair()
    {
        var trade = NpcTrades.Get("doris")!;
        Assert.Equal(Gender.Female, trade.WantsGender);
        var male = PartyOf(Mon("Dragonair", 30, Gender.Male));
        Assert.Null(NpcTrades.Trade(trade, male, 0, Day));
        Assert.Equal("Dragonair", male.Members[0].Species.Name);
        var female = PartyOf(Mon("Dragonair", 30, Gender.Female));
        Assert.Equal("Dodrio", NpcTrades.Trade(trade, female, 0, Day)!.Species.Name);
    }

    [Fact]
    public void FireRedsTradesAreFireRedsAndTheirNumbersAreTheTables()
    {
        // ingame_trades.h, the FireRed branch: Ms. Nido holds a Tiny Mushroom and wants a male Nidoran; Zynx's mail is Fab Mail
        var nido = NpcTrades.Get("msnido")!;
        Assert.Equal(("Nidoran\u2642", "Nidoran\u2640", "Tiny Mushroom", 63184), (nido.Wants, nido.Gives, nido.Item, nido.TrainerId));
        var mimien = NpcTrades.Get("mimien")!;
        Assert.Equal(("Abra", "Mr. Mime", 1985, 0x9caeu), (mimien.Wants, mimien.Gives, mimien.TrainerId, mimien.Personality));
        Assert.Equal(new[] { 5, 5, 5, 30, 5 }, mimien.Conditions);
        Assert.Equal(10, mimien.Sheen);
        Assert.Equal("Golduck", NpcTrades.Get("marc")!.Wants);
    }

    [Fact]
    public void BlackTwosNumbersCameFromItsFilesAndTheNatureAndSexAreKept()
    {
        var petilil = NpcTrades.Get("petulia")!;
        Assert.Equal(("Cottonee", "Petilil", Nature.Timid, Gender.Female, "Power Anklet"),
            (petilil.Wants, petilil.Gives, petilil.Nature, petilil.Gender, petilil.Item));
        Assert.Equal(new[] { 20, 20, 20, 20, 31, 20 }, petilil.Ivs);
        Assert.Equal(("Hippowdon", "Alakazam"), (NpcTrades.Get("beardy")!.Wants, NpcTrades.Get("beardy")!.Gives));
        Assert.Equal(("Excadrill", "Ambipom"), (NpcTrades.Get("ambidexter")!.Wants, NpcTrades.Get("ambidexter")!.Gives));
    }

    [Fact]
    public void TheTradesGiveTheSpeciesTheRegionsOwedAndTheirCheckHoldsIt()
    {
        // FireRed's four were Kanto's to be given by its own people; Black 2's Petilil Unova's
        var kanto = SpeciesSources.Of("Kanto");
        foreach (string s in new[] { "Farfetch'd", "Lickitung", "Mr. Mime", "Jynx" }) Assert.True(kanto.Has(s), s);
        Assert.True(SpeciesSources.Of("Unova").Has("Petilil"));
    }
}
