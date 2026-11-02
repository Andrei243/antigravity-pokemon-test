using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 22 · U11's Hidden Grottoes (<see cref="HiddenGrottoes"/>, <c>grotto</c>, common.GrottoMouth, hidden_grotto.txt)
/// and plan 22 · U10, part c's P2 Laboratory with Genesect (plan 29 · LS11d's last row).
/// </summary>
public class HiddenGrottoTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<Dictionary<string, Map>> Maps =
        new(() => World.LoadAll().Single(w => w.Index.Region == "Unova").BuildMaps().ToDictionary(m => m.Name));

    private static HeadlessScriptHost Play(Map map, (int X, int Y) tile, StoryState story, Party party, Inventory bag, Script script,
        NPC? subject = null, int seed = 0, int steps = 0, params int[] answers)
    {
        var host = new HeadlessScriptHost(story, party, bag)
        {
            Map = map, PlayerTile = tile, MapNamed = name => Maps.Value.GetValueOrDefault(name), Rng = new Random(seed),
            Fight = _ => BattleOutcome.Caught
        };
        host.Pastimes.Steps = steps;
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(script, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void AGrottoIsFullTheFirstTimeAndRollsTheSameOnTheSameSeed()
    {
        var grotto = HiddenGrottoes.Get(2)!;
        var a = new StoryState(); var b = new StoryState();
        var first = HiddenGrottoes.Visit(a, grotto, 0, new Random(7));
        Assert.NotNull(first);
        Assert.Same(first, HiddenGrottoes.Visit(b, grotto, 0, new Random(7)));
        // A full grotto is only read: coming again changes nothing, whatever the draw
        Assert.Same(first, HiddenGrottoes.Visit(a, grotto, 5000, new Random(99)));
    }

    [Fact]
    public void AnEmptiedGrottoRefillsOnlyByStepsAndTheSameOnTheSameSeed()
    {
        var grotto = HiddenGrottoes.Get(3)!;
        var story = new StoryState();
        HiddenGrottoes.Visit(story, grotto, 0, new Random(1));
        HiddenGrottoes.Take(story, grotto, 1000);
        Assert.Equal(HiddenGrottoes.Empty, story.Var(HiddenGrottoes.Vars.Content(3)));

        // Under 256 steps since: no roll at all
        for (int seed = 0; seed < 50; seed++)
        {
            var copy = new StoryState();
            copy.SetVar(HiddenGrottoes.Vars.Content(3), HiddenGrottoes.Empty);
            copy.SetVar(HiddenGrottoes.Vars.EmptiedAt(3), 1000);
            Assert.Null(HiddenGrottoes.Visit(copy, grotto, 1000 + HiddenGrottoes.StepsPerRoll - 1, new Random(seed)));
        }

        // Many rolls: a seed that fills it fills it to the same thing every time
        int filled = 0;
        for (int seed = 0; seed < 50; seed++)
        {
            GrottoEntry? Roll()
            {
                var s = new StoryState();
                s.SetVar(HiddenGrottoes.Vars.Content(3), HiddenGrottoes.Empty);
                s.SetVar(HiddenGrottoes.Vars.EmptiedAt(3), 1000);
                return HiddenGrottoes.Visit(s, grotto, 1000 + 20 * HiddenGrottoes.StepsPerRoll, new Random(seed));
            }
            var once = Roll();
            Assert.Same(once, Roll());
            if (once != null) filled++;
        }
        // Twenty rolls at five in a hundred fill about two grottoes in three
        Assert.InRange(filled, 15, 48);
    }

    [Fact]
    public void EveryGrottosTableIsTheDatasAndItsPokemonHaveAHiddenAbility()
    {
        Assert.Equal(HiddenGrottoes.All.Count, HiddenGrottoes.All.Select(g => g.Id).Distinct().Count());
        foreach (var grotto in HiddenGrottoes.All)
        {
            Assert.NotEmpty(grotto.Table);
            foreach (var entry in grotto.Table)
            {
                if (entry.IsPokemon)
                {
                    var species = PokemonDatabase.Get(entry.Species!);
                    Assert.NotNull(species);
                    Assert.False(string.IsNullOrEmpty(species!.HiddenAbility), $"{entry.Species} has no hidden ability");
                    Assert.InRange(entry.MinLevel, 1, entry.MaxLevel);
                    var made = HiddenGrottoes.Make(species, entry, new Random(4));
                    Assert.Equal(species.HiddenAbility, made.AbilityName);
                    Assert.InRange(made.Level, entry.MinLevel, entry.MaxLevel);
                }
                else Assert.NotNull(ItemDatabase.Get(entry.Item!));
            }
        }
    }

    [Fact]
    public void EveryMouthStandsOnItsMapAndTheWayOutPutsThePlayerBeforeIt()
    {
        var grottoMap = Maps.Value["HiddenGrotto"];
        foreach (var grotto in HiddenGrottoes.All)
        {
            var map = Maps.Value[grotto.Map];
            var mouth = map.Everyone.Single(n => n.NpcType == NPC.GrottoHollowType && n.GridX == grotto.X && n.GridY == grotto.Y);
            Assert.Equal(PropType.GrottoHollow, mouth.GymThing);
            Assert.Equal(grotto.Area, map.AreaAt(grotto.X, grotto.Y)?.Key);
            Assert.True(map.IsWalkable(grotto.X, grotto.Y + 1), $"grotto {grotto.Id}: the tile before its mouth is open");

            // In through the mouth, and out again before it
            var story = new StoryState();
            var party = new Party(); party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 30));
            var into = Play(map, (grotto.X, grotto.Y + 1), story, party, new Inventory(), Scripts.Find(FieldScripts.For(mouth)!, mouth.ScriptFile)!, mouth, answers: 0);
            Assert.Equal(grotto.Id, story.Var(HiddenGrottoes.Vars.Current));
            Assert.Contains("warp HiddenGrotto 15 17", into.Log.Last(l => l.StartsWith("warp ")));
            var leave = Play(grottoMap, (15, 17), story, party, new Inventory(), Scripts.Find("Leave", "hidden_grotto")!, answers: 0);
            Assert.Contains($"warp {grotto.Map} {grotto.X} {grotto.Y + 1}", leave.Log.Last(l => l.StartsWith("warp ")));
        }
    }

    [Fact]
    public void AGrottosPokemonIsMetWithItsHiddenAbilityAndLeavesTheGrottoEmpty()
    {
        var map = Maps.Value["HiddenGrotto"];
        var story = new StoryState();
        story.SetVar(HiddenGrottoes.Vars.Current, 1);
        // A seed whose first draw is a Pokémon
        int seed = Enumerable.Range(0, 100).First(s => HiddenGrottoes.Get(1)!.Table[HiddenGrottoes.Draw(HiddenGrottoes.Get(1)!, new Random(s))].IsPokemon);
        var party = new Party(); party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 30));
        Play(map, (15, 17), story, party, new Inventory(), Scripts.In("hidden_grotto", ScriptLibrary.OnEnter)!, seed: seed);
        Assert.True(story.Has("FLAG_UNOVA_GROTTO_SHOW_MON"));
        Assert.False(story.Has("FLAG_UNOVA_GROTTO_SHOW_ITEM"));
        var holds = HiddenGrottoes.Holds(story, HiddenGrottoes.Get(1)!)!;

        map.ApplyPresence(story.Has);
        var mon = map.NPCs.Single(n => n.Key == "grotto_mon");
        Assert.Equal(holds.Species, mon.Species);
        var met = Play(map, (15, 13), story, party, new Inventory(), Scripts.Find("Mon", "hidden_grotto")!, mon, seed: seed, steps: 300);
        Assert.Contains(met.Log, l => l.StartsWith($"wildbattle {holds.Species} "));
        var caught = party.Members.Last();
        Assert.Equal(holds.Species, caught.Species.Name);
        Assert.Equal(caught.HiddenAbilityName, caught.AbilityName);

        // Empty now, and nothing shows on the next coming before the steps have been walked
        Assert.Equal(HiddenGrottoes.Empty, story.Var(HiddenGrottoes.Vars.Content(1)));
        Assert.Equal(300, story.Var(HiddenGrottoes.Vars.EmptiedAt(1)));
        Play(map, (15, 17), story, party, new Inventory(), Scripts.In("hidden_grotto", ScriptLibrary.OnEnter)!, seed: seed, steps: 400);
        Assert.False(story.Has("FLAG_UNOVA_GROTTO_SHOW_MON"));
        Assert.False(story.Has("FLAG_UNOVA_GROTTO_SHOW_ITEM"));
    }

    [Fact]
    public void AGrottosItemIsPickedUp()
    {
        var map = Maps.Value["HiddenGrotto"];
        var grotto = HiddenGrottoes.Get(1)!;
        var story = new StoryState();
        story.SetVar(HiddenGrottoes.Vars.Current, 1);
        int entry = grotto.Table.FindIndex(e => !e.IsPokemon);
        story.SetVar(HiddenGrottoes.Vars.Content(1), entry + 1);
        var bag = new Inventory();
        var party = new Party(); party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 30));
        Play(map, (15, 17), story, party, bag, Scripts.In("hidden_grotto", ScriptLibrary.OnEnter)!);
        Assert.True(story.Has("FLAG_UNOVA_GROTTO_SHOW_ITEM"));
        map.ApplyPresence(story.Has);
        var ball = map.NPCs.Single(n => n.Key == "grotto_item");
        var took = Play(map, (15, 13), story, party, bag, Scripts.Find("Item", "hidden_grotto")!, ball, steps: 50);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(grotto.Table[entry].Item!)!));
        Assert.Contains(took.Transcript, l => l.Text.Contains(ItemDatabase.Get(grotto.Table[entry].Item!)!.DisplayName));
        Assert.Equal(HiddenGrottoes.Empty, story.Var(HiddenGrottoes.Vars.Content(1)));
        Assert.True(story.Has("FLAG_UNOVA_GROTTO_ITEM_TAKEN"));
    }

    [Fact]
    public void GenesectWaitsInTheP2LaboratoryForUnovasChampion()
    {
        var lab = Maps.Value["P2Laboratory"];
        var story = new StoryState();
        lab.ApplyPresence(story.Has);
        Assert.DoesNotContain(lab.NPCs, n => n.Key == "genesect");

        story.Set("UnovaHallOfFame");
        lab.ForgetForced();
        lab.ApplyPresence(story.Has);
        var genesect = lab.NPCs.Single(n => n.Key == "genesect");
        var party = new Party(); party.Add(new Pokemon(PokemonDatabase.Get("Serperior")!, 70));
        var bag = new Inventory();
        var met = Play(lab, (5, 3), story, party, bag, Scripts.Find("Genesect", "p2_laboratory_2")!, genesect);
        Assert.Contains("wildbattle Genesect 65 Caught", met.Log);
        // (met from the tile south of it, the player steps back first so it isn't hidden behind their head)
        Assert.Contains(met.Log, l => l.StartsWith("emote player", StringComparison.OrdinalIgnoreCase));
        Assert.Equal((5, 5), met.PlayerTile);
        Assert.True(story.Has("FLAG_UNOVA_FOUGHT_GENESECT"));
        lab.ForgetForced();
        lab.ApplyPresence(story.Has);
        Assert.DoesNotContain(lab.NPCs, n => n.Key == "genesect");

        // The researcher's Shock Drive, once
        var scientist = lab.NPCs.Single(n => n.Name == "Scientist");
        Play(lab, (scientist.GridX, scientist.GridY + 1), story, party, bag, Scripts.Find("Scientist", "p2_laboratory_2")!, scientist);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Shock Drive")!));
        Play(lab, (scientist.GridX, scientist.GridY + 1), story, party, bag, Scripts.Find("Scientist", "p2_laboratory_2")!, scientist);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Shock Drive")!));
    }
}
