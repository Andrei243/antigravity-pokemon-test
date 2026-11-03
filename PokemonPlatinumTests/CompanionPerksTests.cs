using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 10 · F6, F7, F9, F11 and F12, their rules alone: the camp's petting, feeding and cure; affection's perks under
/// the modern rules and none under Platinum's; the marks of a catch and its title on the send-out line; the DexNav's
/// HERE list, its rustles and the bonus a find carries.
/// </summary>
public class CompanionPerksTests
{
    // ------------------------------------------------------------------ F7 · affection's perks

    private static Pokemon Loving(Pokemon p, int friendship)
    {
        p.Friendship = friendship;
        return p;
    }

    [Fact]
    public void AffectionIsReadFromFriendshipInTwoSteps()
    {
        var p = Mon("Starly", 10, "Tackle");
        Assert.Equal(0, Affection.Level(Loving(p, 219)));
        Assert.Equal(1, Affection.Level(Loving(p, 220)));
        Assert.Equal(2, Affection.Level(Loving(p, 255)));
        Assert.True(Ruleset.Modern.AffectionPerks);
        Assert.False(Ruleset.Platinum.AffectionPerks);
        Assert.Equal(1, Affection.CritStages(2));
        Assert.Equal(0, Affection.CritStages(1));
        Assert.Equal(0, Affection.DodgeChance(1));
    }

    [Fact]
    public void AModernPokemonThatLovesItsTrainerShakesOffASleepAndAPlatinumOneNeverDoes()
    {
        List<string> Play(Ruleset rules)
        {
            var mine = Loving(Mon("Starly", 20), 220);
            mine.Status = StatusCondition.Sleep;
            mine.SleepTurns = 10;
            var core = Wild(mine, Mon("Bidoof", 20), Calm().Force(RollKind.Affection, 0), rules);
            var said = Turn(core);
            Assert.Equal(rules.AffectionPerks ? StatusCondition.None : StatusCondition.Sleep, core.PlayerParty.Members[0].Status);
            return said;
        }
        Assert.Contains(Play(Ruleset.Modern), l => l.Contains("shrugged off"));
        Assert.DoesNotContain(Play(Ruleset.Platinum), l => l.Contains("shrugged off"));
    }

    [Fact]
    public void ARollAboveTheChanceShakesNothingOff()
    {
        var mine = Loving(Mon("Starly", 20), 220);
        mine.Status = StatusCondition.Poison;
        var core = Wild(mine, Mon("Bidoof", 20), Calm().Force(RollKind.Affection, 10), Ruleset.Modern);
        Turn(core);
        Assert.Equal(StatusCondition.Poison, core.PlayerParty.Members[0].Status);
    }

    [Fact]
    public void AModernPokemonThatLovesItsTrainerHoldsOnAtOneHp()
    {
        int Left(Ruleset rules)
        {
            var mine = Loving(Mon("Starly", 20), 220);
            mine.CurrentHP = 3;
            var core = Wild(mine, Mon("Bidoof", 40, "Tackle"), Calm().Force(RollKind.Affection, 0), rules);
            Turn(core);
            return core.PlayerParty.Members[0].CurrentHP;
        }
        Assert.Equal(1, Left(Ruleset.Modern));
        Assert.Equal(0, Left(Ruleset.Platinum));
    }

    [Fact]
    public void ADevotedModernPokemonDodgesAndAFoeNeverDoes()
    {
        var mine = Loving(Mon("Starly", 20, "Tackle"), 255);
        var foe = Loving(Mon("Bidoof", 20, "Tackle"), 255);
        int before = mine.CurrentHP, foeBefore = foe.CurrentHP;
        var core = Wild(mine, foe, Calm().Force(RollKind.Affection, 0), Ruleset.Modern);
        var said = Turn(core);
        Assert.Contains(said, l => l.Contains("slipped out of the way"));
        Assert.Equal(before, core.PlayerParty.Members[0].CurrentHP);
        // A wild Pokémon's friendship is nothing to its trainer: it is hit
        Assert.True(core.WildPokemon[0].CurrentHP < foeBefore);
    }

    [Fact]
    public void AffectionGivesAFifthMoreExpUnderTheModernRulesAlone()
    {
        int Gained(int friendship, Ruleset rules)
        {
            var foe = Mon("Bidoof", 5);
            foe.CurrentHP = 1;
            var core = Wild(Loving(Mon("Starly", 20, "Tackle"), friendship), foe, Calm(), rules);
            Turn(core);
            return core.Log.OfType<ExpGained>().Single().Amount;
        }
        int plain = Gained(70, Ruleset.Modern);
        Assert.Equal(plain * Affection.ExpPercent / 100, Gained(220, Ruleset.Modern));
        Assert.Equal(Gained(70, Ruleset.Platinum), Gained(255, Ruleset.Platinum));
    }

    [Fact]
    public void ABattleWithAPerkReplaysTheSame()
    {
        static (Pokemon Mine, Pokemon Foe) Teams() =>
            (Loving(Mon("Starly", 18, "Tackle", "Quick Attack"), 255), Mon("Bidoof", 21, "Tackle", "Headbutt"));
        var (mine, foe) = Teams();
        var core = Wild(mine, foe, new BattleRandom(424242), Ruleset.Modern);
        for (int i = 0; i < 12 && core.Request is ActionRequest; i++) Turn(core, i % 2, i % 2);
        var record = core.Record!;

        var (again, againFoe) = Teams();
        var party = new Party();
        party.Add(again);
        var replayed = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { againFoe }, Random = new BattleRandom(record.Seed), Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        replayed.Replay(record);
        Assert.Equal(Golden.BattleText.Written(core), Golden.BattleText.Written(replayed));
    }

    // ------------------------------------------------------------------ F9 · marks and titles

    private static uint PersonalityFor(MarkMoment moment, Mark wanted)
    {
        for (uint p = 0; p < 2_000_000; p++)
            if (EncounterMarks.For(p, moment) == wanted) return p;
        throw new InvalidOperationException($"No personality gives {wanted}");
    }

    [Fact]
    public void ACatchAtNightThatPassesTheOddsIsSleepyAndOneInTheRainIsRainSoaked()
    {
        var night = new MarkMoment(GameClock.ForHour(23), FieldWeather.Clear, false);
        uint sleepy = PersonalityFor(night, Mark.SleepyTime);
        var starly = Mon("Starly", 10);
        starly.Personality = sleepy;
        EncounterMarks.Give(starly, night);
        Assert.Equal(new[] { Mark.SleepyTime }, EncounterMarks.Of(starly));
        // The same personality at noon gives the noon's mark, never the night's
        Assert.Equal(Mark.Lunchtime, EncounterMarks.For(sleepy, night with { Time = TimeOfDay.Day }));

        var rain = new MarkMoment(TimeOfDay.Day, FieldWeather.Rain, false);
        uint rainy = PersonalityFor(rain, Mark.Rainy);
        Assert.Equal(Mark.Rainy, EncounterMarks.For(rainy, rain));
        // Under a clear sky the weather's try is never made
        Assert.NotEqual(Mark.Rainy, EncounterMarks.For(rainy, rain with { Weather = FieldWeather.Clear }));
        // The fishing mark only for one reeled in
        uint hooked = PersonalityFor(new MarkMoment(TimeOfDay.Day, FieldWeather.Clear, true), Mark.Fishing);
        Assert.Null(EncounterMarks.For(hooked, new MarkMoment(TimeOfDay.Day, FieldWeather.Clear, false)));
    }

    [Fact]
    public void NoCatchCarriesTwoMarksAndTheSamePersonalityGivesTheSameMark()
    {
        var moment = new MarkMoment(TimeOfDay.Night, FieldWeather.Thunderstorm, true);
        int marked = 0;
        for (uint p = 0; p < 5000; p++)
        {
            var mon = Mon("Bidoof", 5);
            mon.Personality = p * 2654435761u;
            EncounterMarks.Give(mon, moment);
            Assert.True(mon.EncounterMarks.Count <= 1);
            Assert.Equal(EncounterMarks.For(mon.Personality, moment), EncounterMarks.For(mon.Personality, moment));
            marked += mon.EncounterMarks.Count;
        }
        // About one in eight: 1/1000 + 1/100 + 1/50 + 1/50 + 1/25 of what is left
        Assert.InRange(marked, 300, 700);
    }

    [Fact]
    public void ATitleChosenChangesTheSendOutLineAndNoneChangesNothing()
    {
        var plain = Wild(Mon("Starly", 10, "Tackle"), Mon("Bidoof", 5));
        Assert.Contains("Go! Starly!", Lines(plain.Log));

        var marked = Mon("Starly", 10, "Tackle");
        marked.EncounterMarks.Add(nameof(Mark.SleepyTime));
        Assert.Contains("Go! Starly!", Lines(Wild(marked, Mon("Bidoof", 5)).Log));
        Assert.False(EncounterMarks.ChooseTitle(marked, Mark.Rainy));
        Assert.True(EncounterMarks.ChooseTitle(marked, Mark.SleepyTime));
        var titled = Wild(marked, Mon("Bidoof", 5));
        var opening = Lines(titled.TakeLog());
        Assert.Contains("Go! Starly the Sleepy!", opening);
        // Every other line names it plainly
        Assert.DoesNotContain(Turn(titled), l => l.Contains("the Sleepy"));
        Assert.Equal("Sleepy Mark", EncounterMarks.Name(Mark.SleepyTime));
    }

    [Fact]
    public void MarksAndTitlesAreSavedAndAnOlderSavesPokemonHasNone()
    {
        var p = Mon("Starly", 10, "Tackle");
        p.EncounterMarks.Add(nameof(Mark.Rare));
        EncounterMarks.ChooseTitle(p, Mark.Rare);
        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal(new[] { Mark.Rare }, EncounterMarks.Of(back));
        Assert.Equal(nameof(Mark.Rare), back.Title);

        var old = SavedPokemonData.FromPokemon(Mon("Starly", 10, "Tackle"));
        Assert.Null(old.EncounterMarks);
        Assert.Null(old.Title);
        var older = System.Text.Json.JsonSerializer.Deserialize<SavedPokemonData>("{\"speciesName\":\"Starly\",\"level\":10}", GameDataFiles.Json)!;
        Assert.Empty(older.ToPokemon().EncounterMarks);
    }

    [Fact]
    public void ACatchTakesTheMarkOfItsMoment()
    {
        var night = new MarkMoment(TimeOfDay.Night, FieldWeather.Clear, false);
        var foe = Mon("Bidoof", 3, "Tackle");
        foe.Personality = PersonalityFor(night, Mark.SleepyTime);
        var party = new Party();
        party.Add(Mon("Starly", 10, "Tackle"));
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Master Ball")!, 1);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { foe },
            Random = Steady(), Rules = Ruleset.Platinum, Encounter = night
        });
        Settle(battle);
        battle.UseBagItem("Master Ball");
        Settle(battle);
        Assert.NotNull(battle.Caught);
        Assert.True(EncounterMarks.Of(battle.Caught!).SequenceEqual(new[] { Mark.SleepyTime }), $"{battle.Caught!.Personality:X} {foe.Personality:X} [{string.Join(",", battle.Caught!.EncounterMarks)}] {EncounterMarks.For(battle.Caught!.Personality, night)}");
    }

    // ------------------------------------------------------------------ F6 · the camp

    [Fact]
    public void PettingAndFeedingRaiseFriendshipByTheTableOnceAVisit()
    {
        var party = new Party();
        var starly = Loving(Mon("Starly", 10), 70);
        party.Add(starly);
        var bag = new Inventory();
        var berry = ItemDatabase.Get("Oran Berry")!;
        bag.AddItem(berry, 2);
        var care = new CampCare();
        var screen = new CampScreen();
        screen.Open(party, care);

        Assert.True(screen.Pet());
        Assert.Equal(70 + FriendshipRules.ChangeFor(FriendshipEvent.Petted).Low, starly.Friendship);
        Assert.False(screen.Pet());
        Assert.True(screen.Feed(berry, bag));
        Assert.Equal(70 + 3 + 5, starly.Friendship);
        Assert.Equal(1, bag.GetQuantity(berry));
        Assert.False(screen.Feed(berry, bag));
        Assert.Equal(CampRefusal.NoBerry, new CampVisit().WhyNotFeed(starly, ItemDatabase.Get("Potion"), bag));

        // A new visit pets and feeds again
        screen.Open(party, care);
        Assert.True(screen.Pet());
    }

    [Fact]
    public void TheCampCuresTheTeamOncePerBattle()
    {
        var party = new Party();
        var a = Mon("Starly", 10);
        a.Status = StatusCondition.Burn;
        party.Add(a);
        var care = new CampCare();
        Assert.Equal(1, care.Cure(party));
        Assert.Equal(StatusCondition.None, a.Status);
        a.Status = StatusCondition.Paralyze;
        Assert.Equal(0, care.Cure(party));
        Assert.Equal(StatusCondition.Paralyze, a.Status);
        care.BattleEnded();
        Assert.Equal(1, care.Cure(party));
    }

    [Fact]
    public void TheCampsCursorMovesOverSixThreeAndOne()
    {
        static CampScreen With(int n)
        {
            var party = new Party();
            for (int i = 0; i < n; i++) party.Add(Mon("Starly", 10));
            var s = new CampScreen();
            s.Open(party, new CampCare());
            return s;
        }
        var six = With(6);
        six.MoveCursor(1, 0); six.MoveCursor(1, 0); six.MoveCursor(1, 0);
        Assert.Equal(2, six.Cursor);
        six.MoveCursor(0, 1);
        Assert.Equal(5, six.Cursor);
        var five = With(5);
        five.MoveCursor(2, 0); five.MoveCursor(0, 1);
        Assert.Equal(4, five.Cursor);
        var three = With(3);
        three.MoveCursor(0, 1); three.MoveCursor(5, 0);
        Assert.Equal(2, three.Cursor);
        var one = With(1);
        one.MoveCursor(1, 1);
        Assert.Equal(0, one.Cursor);
        one.Cancel();
        Assert.Equal(CampRequest.Close, one.TakeRequest());
        Assert.False(one.IsActive);
    }

    // ------------------------------------------------------------------ F11 · what lives here

    [Fact]
    public void WhatLivesAtATileOfRoute201AgreesWithWhereEachLives()
    {
        var habitats = Habitats.Sinnoh!;
        var route = habitats.Of("Starly").First(h => h.Name == "Route 201");
        var (cx, cy) = route.Cells[0];
        var here = habitats.At(cx, cy);
        Assert.Contains(here, h => h.Species == "Starly");
        // The other way round: every species here lives in this cell
        foreach (var h in here) Assert.Contains(habitats.Of(h.Species), place => place.Name == h.Place && place.Cells.Contains((cx, cy)));
        Assert.Equal(here, habitats.AtTile(cx * Habitats.CellTiles + 5, cy * Habitats.CellTiles + 7));

        var dex = new Pokedex();
        dex.RegisterSeen(PokemonDatabase.Get("Starly")!.DexNumber);
        dex.RegisterCaught(Mon("Bidoof", 3));
        var nav = new DexNavState();
        nav.Found("Kricketot");
        var morning = DexNavRules.Here(habitats, cx * Habitats.CellTiles, cy * Habitats.CellTiles, TimeOfDay.Morning, dex, nav);
        Assert.Equal(new[] { "Starly", "Bidoof", "Kricketot" }, morning.Select(e => e.Species).Take(3));
        Assert.True(morning[0].Seen && !morning[0].Caught);
        Assert.True(morning[1].Caught);
        Assert.Equal(1, morning[2].Level);
        Assert.False(morning[2].Seen);
    }

    [Fact]
    public void TheSearchLevelsAndTheChainAreSaved()
    {
        var nav = new DexNavState { On = true };
        nav.Found("Shinx");
        nav.Found("Shinx");
        var back = DexNavState.From(System.Text.Json.JsonSerializer.Deserialize<DexNavSave>(GameDataFiles.Serialize(nav.ToSave()), GameDataFiles.Json));
        Assert.True(back.On);
        Assert.Equal(2, back.LevelOf("Shinx"));
        Assert.Equal(2, back.ChainFor("Shinx"));
        Assert.Equal(0, back.ChainFor("Starly"));
        back.Found("Starly");
        Assert.Equal(1, back.ChainFor("Starly"));
        back.Break();
        Assert.Equal(0, back.ChainFor("Starly"));
        Assert.True(new DexNavState().IsEmpty);
        Assert.Equal(nameof(DexNavRules.Item), nameof(DexNavRules.Item));
        Assert.NotNull(ItemDatabase.Get(DexNavRules.Item));
    }

    // ------------------------------------------------------------------ F12 · rustles and chains

    private static Map GrassField(int grassFromX = 0)
    {
        var map = new Map(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = grassFromX; x < 32; x++) map.SetGroundTile(x, y, TileType.TallGrass);
        return map;
    }

    [Fact]
    public void ARustleLandsOnlyOnTheGrassInSight()
    {
        var map = GrassField(grassFromX: 16);
        var rng = new Random(9);
        var entry = new WildEncounterEntry { SpeciesName = "Shinx", MinLevel = 5, MaxLevel = 5, Weight = 1 };
        for (int i = 0; i < 200; i++)
        {
            if (DexNavRules.Place(map, 16, 16, rng, (_, _) => entry) is not { } r) continue;
            Assert.True(map.IsTallGrass(r.X, r.Y));
            Assert.InRange(Math.Abs(r.X - 16), 0, DexNavRules.ReachX);
            Assert.InRange(Math.Abs(r.Y - 16), 0, DexNavRules.ReachY);
            Assert.False(Math.Abs(r.X - 16) <= DexNavRules.NotWithin && Math.Abs(r.Y - 16) <= DexNavRules.NotWithin);
        }
        // Nothing lives there: nothing is found
        Assert.Null(DexNavRules.Place(map, 16, 16, rng, (_, _) => null));
    }

    [Fact]
    public void ARunScaresARustleAndACreepReachesIt()
    {
        var r = new Rustle(10, 10, new WildEncounterEntry { SpeciesName = "Shinx" });
        Assert.Equal(RustleAfterStep.Scared, DexNavRules.AfterStep(r, 14, 10, Pace.Fast, TravelMode.OnFoot, false));
        Assert.Equal(RustleAfterStep.Scared, DexNavRules.AfterStep(r, 12, 10, Pace.Walk, TravelMode.OnFoot, false));
        Assert.Equal(RustleAfterStep.Stays, DexNavRules.AfterStep(r, 15, 10, Pace.Walk, TravelMode.OnFoot, false));
        Assert.Equal(RustleAfterStep.Stays, DexNavRules.AfterStep(r, 11, 10, Pace.Sneak, TravelMode.OnFoot, false));
        Assert.Equal(RustleAfterStep.Scared, DexNavRules.AfterStep(r, 15, 10, Pace.Fast, TravelMode.Cycling, false));
        Assert.Equal(RustleAfterStep.Scared, DexNavRules.AfterStep(r, 15, 10, Pace.Walk, TravelMode.OnFoot, hopped: true));
        Assert.Equal(RustleAfterStep.Reached, DexNavRules.AfterStep(r, 10, 10, Pace.Sneak, TravelMode.OnFoot, false));
        Assert.Equal(RustleAfterStep.Lost, DexNavRules.AfterStep(r, 30, 10, Pace.Fast, TravelMode.OnFoot, false));
        // A creep is slower than a walk, and the pace of a step the run button takes when sneaking
        Assert.True(FieldMovement.TilesPerSecond(Pace.Sneak) < FieldMovement.TilesPerSecond(Pace.Walk));
        Assert.Equal(Pace.Sneak, FieldMovement.PaceOn(TileBehavior.TallGrass, new Walker(Running: false, Sneaking: true)));
    }

    [Fact]
    public void AFindsBonusGivesIvsByLevelAndChainAndAHiddenAbilityUnderTheModernRulesAlone()
    {
        Assert.Equal(0, DexNavRules.PerfectIvs(0, 0));
        Assert.Equal(1, DexNavRules.PerfectIvs(10, 0));
        Assert.Equal(2, DexNavRules.PerfectIvs(50, 0));
        Assert.Equal(3, DexNavRules.PerfectIvs(50, 5));
        Assert.Equal(3, DexNavRules.PerfectIvs(999, 999));

        static int Perfect(Pokemon p) => new[] { p.IvHP, p.IvAttack, p.IvDefense, p.IvSpAttack, p.IvSpDefense, p.IvSpeed }.Count(v => v == 31);
        var shinx = new Pokemon(PokemonDatabase.Get("Shinx")!, 5, new Random(3));
        int before = Perfect(shinx);
        DexNavRules.Bonus(shinx, 50, 5, Ruleset.Platinum, new Random(1));
        Assert.True(Perfect(shinx) >= Math.Max(before, 3));
        Assert.NotEqual(shinx.HiddenAbilityName, shinx.AbilityName);

        int hidden = 0;
        for (int seed = 0; seed < 400; seed++)
        {
            var p = new Pokemon(PokemonDatabase.Get("Shinx")!, 5, new Random(seed));
            DexNavRules.Bonus(p, 100, 0, Ruleset.Modern, new Random(seed));
            if (p.AbilityName == p.HiddenAbilityName) hidden++;
        }
        Assert.InRange(hidden, 40, 130);
    }
}
