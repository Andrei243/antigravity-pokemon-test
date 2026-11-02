using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>The Pokédex's research (plan 15 · E1) and its TASKS page (E2).</summary>
public class ResearchTests
{
    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    [Fact]
    public void EverySpeciesTasksAreAchievableWithItsOwnData()
    {
        foreach (var species in PokemonDatabase.GetAll())
        {
            var tasks = ResearchTasks.For(species);
            Assert.InRange(tasks.Count, 4, 8);
            Assert.Equal(tasks.Count, tasks.Select(t => t.Key).Distinct().Count());
            foreach (var task in tasks.Where(t => t.Kind == ResearchKind.MoveUsed))
                Assert.Contains(species.Learnset, m => m.MoveName == task.Move && m.Level >= 1);
            Assert.Equal(species.Evolutions is { Count: > 0 }, tasks.Any(t => t.Kind == ResearchKind.Evolved));
            // What a game gives a legendary only once has a ladder of one
            if (species.Legendary || species.Mythical)
                Assert.Equal(ResearchTasks.Once, tasks.Single(t => t.Kind == ResearchKind.Caught).Rungs);
        }
    }

    [Fact]
    public void TheRungsGiveTenLevelsExactly()
    {
        foreach (var species in PokemonDatabase.GetAll().Where(s => s.DexNumber % 7 == 0))
        {
            var research = new Research();
            Assert.Equal(0, research.LevelOf(species));
            foreach (var task in ResearchTasks.For(species)) research.Tell(species, task.Kind, task.Move, task.Rungs[^1]);
            Assert.Equal(Research.MaxLevel, research.LevelOf(species));
            // A rung short of the end of one ladder is short of 10
            var fresh = new Research();
            var tasks = ResearchTasks.For(species);
            foreach (var task in tasks) fresh.Tell(species, task.Kind, task.Move, task == tasks[0] ? task.Rungs[^1] - 1 : task.Rungs[^1]);
            Assert.True(fresh.LevelOf(species) < Research.MaxLevel);
        }
    }

    [Fact]
    public void AKindTheSpeciesDoesntAskForIsNotCounted()
    {
        var research = new Research();
        var starly = Species("Starly");
        research.Tell(starly, ResearchKind.MoveUsed, "Hydro Pump");
        Assert.True(research.IsEmpty);
        // Unown learns only one move and doesn't evolve: no second move, no evolution
        var unown = Species("Unown");
        Assert.Single(ResearchTasks.For(unown), t => t.Kind == ResearchKind.MoveUsed);
        research.Tell(unown, ResearchKind.Evolved);
        Assert.True(research.IsEmpty);
    }

    [Fact]
    public void AWildBattleFillsStarlysRecordOnceForEachThing()
    {
        var starly = Species("Starly");
        string move = ResearchTasks.TwoMoves(starly)[0];
        var mine = Mon("Starly", 40, move);
        var foe = Mon("Starly", 2);
        var battle = Battle(mine, foe);
        var research = battle.Pokedex.Research;
        // Met as the battle opened
        Assert.Equal(1, research.RecordOf(starly.DexNumber).CountOf("met"));

        Turn(battle);
        Assert.True(battle.IsBattleOver);
        var record = research.RecordOf(starly.DexNumber);
        Assert.Equal(1, record.CountOf("move:" + move));
        Assert.Equal(1, record.CountOf("defeated"));
        Assert.Equal(1, record.CountOf("met"));
        Assert.Equal(0, record.CountOf("caught"));
        // Three first rungs of Starly's eight ladders: not yet a level
        Assert.Equal(3, research.Points(starly).Points);
        Assert.Equal(0, research.LevelOf(starly));
        Assert.Empty(research.TakeRisen());

        // A level that rises waits for the field to tell it, once
        research.Tell(starly, ResearchKind.Met, n: 24);
        Assert.Equal(1, research.LevelOf(starly));
        Assert.Equal(new[] { starly.DexNumber }, research.TakeRisen());
        Assert.Empty(research.TakeRisen());
    }

    [Fact]
    public void ACatchIsCountedOnce()
    {
        var bidoof = Species("Bidoof");
        var mine = Mon("Starly", 30);
        var foe = Mon("Bidoof", 3);
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Master Ball")!, 1);
        var party = new Party();
        party.Add(mine);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { foe }, Random = Steady(), Rules = Ruleset.Platinum
        });
        Settle(battle);
        battle.UseBagItem("Master Ball");
        Settle(battle);
        Assert.Equal(1, battle.Pokedex.Research.RecordOf(bidoof.DexNumber).CountOf("caught"));
        Assert.Equal(0, battle.Pokedex.Research.RecordOf(bidoof.DexNumber).CountOf("defeated"));
    }

    [Fact]
    public void ASpreadMoveIsCountedOnceWhateverItHits()
    {
        // A species whose own research asks for a move that hits both foes
        var (species, move) = PokemonDatabase.GetAll()
            .SelectMany(s => ResearchTasks.TwoMoves(s).Select(m => (s, m)))
            .First(p => MoveDatabase.Get(p.m)!.Target == MoveTarget.AllFoes && p.s.Evolutions is { Count: > 0 } && !p.s.Legendary);
        var party = new Party();
        party.Add(Mon(species.Name, 30, move));
        party.Add(Mon("Bidoof", 30));
        var twins = new Trainer { Name = "Liv & Liz", TrainerClass = "Twins", PrizeMoney = 400 };
        twins.Party.Add(Mon("Bidoof", 30));
        twins.Party.Add(Mon("Starly", 30));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            Format = BattleFormat.Double, Trainers = new() { twins }, Random = Steady(), Rules = Ruleset.Platinum
        });
        Settle(battle);
        battle.SelectMove(0);
        if (battle.HUD.MenuState == BattleMenuState.SelectTarget) battle.SelectTarget(0);
        battle.SelectMove(0);
        if (battle.HUD.MenuState == BattleMenuState.SelectTarget) battle.SelectTarget(0);
        Settle(battle);
        Assert.Equal(1, battle.Pokedex.Research.RecordOf(species.DexNumber).CountOf("move:" + move));
    }

    [Fact]
    public void ASaveRoundTripsTheRecord()
    {
        var research = new Research();
        var starly = Species("Starly");
        research.Tell(starly, ResearchKind.Met, n: 3);
        research.Tell(starly, ResearchKind.Caught);
        var save = new SaveData { Research = research.ToSaved(), ResearchComplete = true };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        var back = new Research();
        back.Restore(read.Research, read.ResearchComplete);
        Assert.Equal(3, back.RecordOf(starly.DexNumber).CountOf("met"));
        Assert.Equal(1, back.RecordOf(starly.DexNumber).CountOf("caught"));
        Assert.True(back.Complete);
        Assert.Equal(research.LevelOf(starly), back.LevelOf(starly));

        // An older save, with nothing of it, reads as nothing counted
        var older = JsonSerializer.Deserialize<SaveData>("{}")!;
        back.Restore(older.Research, older.ResearchComplete);
        Assert.True(back.IsEmpty);
        Assert.False(back.Complete);
    }

    [Fact]
    public void TheCertificateComesAtTheSinnohPokedexsTwoHundredAndTenAndNeverTwice()
    {
        var research = new Research();
        var sinnoh = Pokedex.Entries(Pokedex.Sinnoh).Select(e => e.Species).ToList();
        Assert.Equal(210, sinnoh.Count);
        void Finish(PokemonSpecies s) { foreach (var t in ResearchTasks.For(s)) research.Tell(s, t.Kind, t.Move, t.Rungs[^1]); }
        foreach (var s in sinnoh.Take(209)) Finish(s);
        Assert.False(research.AwardCertificate());
        Finish(sinnoh[209]);
        Assert.Equal((210, 210), research.SinnohProgress());
        Assert.True(research.AwardCertificate());
        Assert.False(research.AwardCertificate());
        Assert.True(research.Complete);
    }

    // ------------------------------------------------------------------ the TASKS page (E2)

    [Fact]
    public void TheTasksPageIsTheLastOfEverySpeciesSeenAndAnUnseenOneHasNone()
    {
        var pokedex = new Pokedex();
        pokedex.RegisterSeen(Species("Starly").DexNumber);
        var dex = new PokedexScreen();
        dex.Open(pokedex);
        Assert.Equal("Starly", dex.Selected!.Value.Species.Name);
        Assert.Equal(PokedexPage.Tasks, dex.PagesOf(dex.Selected)[^1]);
        dex.Confirm();
        for (int i = 0; i < 4; i++) dex.Sideways(1);
        Assert.Equal(PokedexPage.Tasks, dex.Page);

        // An unseen species opens no entry at all, so no tasks
        dex.Cancel();
        dex.SelectedIndex = dex.Rows.ToList().FindIndex(r => r.Species.Name == "Bibarel");
        dex.Confirm();
        Assert.Equal(PokedexFocus.List, dex.Focus);
    }

    [Fact]
    public void TheCertificateIsShownAsThePokedexOpensOnce()
    {
        var pokedex = new Pokedex();
        foreach (var s in Pokedex.Entries(Pokedex.Sinnoh).Select(e => e.Species))
            foreach (var t in ResearchTasks.For(s)) pokedex.Research.Tell(s, t.Kind, t.Move, t.Rungs[^1]);
        // Every species seen as well: the Sinnoh diploma comes first, then the certificate
        foreach (var e in Pokedex.Entries(Pokedex.Sinnoh)) pokedex.RegisterSeen(e.Species.DexNumber);
        var dex = new PokedexScreen();
        dex.Open(pokedex);
        Assert.Equal((PokedexFocus.Diploma, Pokedex.Sinnoh), (dex.Focus, dex.ShownDiploma));
        dex.Confirm();
        Assert.Equal((PokedexFocus.Diploma, Research.CertificateMode), (dex.Focus, dex.ShownDiploma));
        dex.Confirm();
        Assert.Equal(PokedexFocus.List, dex.Focus);
        dex.Close();
        dex.Open(pokedex);
        Assert.Equal(PokedexFocus.List, dex.Focus);
    }
}
