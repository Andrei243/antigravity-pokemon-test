using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The ways into a battle by place and by whom (plan 09 · L13; style guide, "Life", "Into battle"): the choice,
/// held to the original's row by row, the twelve ways' times, what leads in and rides over the picture, what
/// their dark covers, and the Galactic mark; and the bands of the Gym Leaders, the Elite Four and the Champion
/// (plan 09 · L15): their rows, what a band shows at each beat, what its white covers, and whose colours it wears.
/// All of it runs without a window.
/// </summary>
public class IntoBattleTests
{
    private const float W = 1920f, H = 1080f;
    private static readonly Vector2 Middle = new(W / 2f, H / 2f);

    // ------------------------------------------------------------------ the choice

    /// <summary>A trainer of the game's data, as a meeting knows them: their class and their name.</summary>
    private static MetTrainer Of(string id)
    {
        var record = TrainerDatabase.Get(id) ?? throw new InvalidOperationException($"No trainer {id}");
        return new MetTrainer(record.Class, record.Name);
    }

    private static BattleMeeting Wild(BattleTerrain ground, int theirs = 5, int own = 5, bool two = false, string species = "Starly", BattleKind kind = BattleKind.Normal) =>
        new() { WildSpecies = species, Double = two, Kind = kind, Terrain = ground, TheirLevel = theirs, OwnLevel = own };

    private static BattleMeeting With(BattleTerrain ground, int theirs, int own, bool two, params MetTrainer[] trainers) =>
        new() { Trainers = trainers, Double = two, Terrain = ground, TheirLevel = theirs, OwnLevel = own };

    private static BattleMeeting With(string id, BattleTerrain ground = BattleTerrain.Plain, int theirs = 5, int own = 5, bool two = false) =>
        With(ground, theirs, own, two, Of(id));

    private static void Meets(TransitionKind way, MeetingRow row, BattleMeeting meeting)
    {
        Assert.Equal(row, meeting.Row);
        Assert.Equal(way, SceneTransition.ForBattle(meeting));
    }

    /// <summary>
    /// The choice, row by row, against the original's at the pinned commit: <c>EncEffects_GetEffectPair</c>,
    /// <c>EncEffects_TrainerClassEffect</c> and <c>sEncEffectsTable</c> of <c>src/enc_effects.c</c>, and
    /// <c>CutInEffects_ForBattle</c> of <c>src/overlay005/encounter_effect.c</c>. The lines are theirs.
    /// </summary>
    [Fact]
    public void TheWayInIsTheOriginalsChoiceRowByRow()
    {
        // enc_effects.c 215-238 (a class to each of the eight Gym Leaders) and the table's rows 75-82
        // (ENCEFF_CUTIN_LEADER_ROARK to …_VOLKNER): a Leader alone is met on their band, and so are the teams they
        // bring to the Battleground, which are the same class and the same person
        foreach (string leader in new[] { "roark", "gardenia", "wake", "maylene", "fantina", "candice", "byron", "volkner" })
        {
            Meets(TransitionKind.LeaderBand, MeetingRow.Leader, With("leader_" + leader, BattleTerrain.Building, 14, 20));
            Meets(TransitionKind.LeaderBand, MeetingRow.Leader, With($"leader_{leader}_rematch", BattleTerrain.Building, 62, 60));
        }
        // The band whatever the ground and however strong: a row with a way of its own never asks either (186-190)
        Meets(TransitionKind.LeaderBand, MeetingRow.Leader, With("leader_roark", BattleTerrain.Cave, 14, 5));
        Meets(TransitionKind.LeaderBand, MeetingRow.Leader, With("leader_wake", BattleTerrain.Water, 33, 60));
        // 239-250 and rows 83-86 (ENCEFF_CUTIN_ELITE_FOUR_…): each of the Elite Four, and their rematches
        foreach (string member in new[] { "aaron", "bertha", "flint", "lucian" })
        {
            Meets(TransitionKind.EliteBand, MeetingRow.EliteFour, With("elite_four_" + member, BattleTerrain.Special, 49, 60));
            Meets(TransitionKind.EliteBand, MeetingRow.EliteFour, With($"elite_four_{member}_rematch", BattleTerrain.Special, 65, 60));
        }
        // 251-253 and row 87 (ENCEFF_CUTIN_CHAMPION_CYNTHIA): the Champion, and her rematch
        Meets(TransitionKind.EliteBand, MeetingRow.Champion, With("champion_cynthia", BattleTerrain.Special, 58, 70));
        Meets(TransitionKind.EliteBand, MeetingRow.Champion, With("champion_cynthia_rematch", BattleTerrain.Special, 74, 70));
        // 148-158 stand before the class's own row of 164: in a double battle the band is lost (the slip the original's
        // own comment owns), a Leader's and an Elite Four's alike; Volkner's beside Flint in the Fight Area is told
        // apart only for its music (153-155, row 107: ENCEFF_CUTIN_DOUBLE)
        Meets(TransitionKind.Double, MeetingRow.DoubleLeader, With(BattleTerrain.Plain, 59, 60, true, Of("leader_volkner_fight_area"), Of("elite_four_flint_fight_area")));
        Meets(TransitionKind.Double, MeetingRow.DoubleBattle, With(BattleTerrain.Building, 14, 20, true, Of("leader_roark")));
        Meets(TransitionKind.Double, MeetingRow.DoubleBattle, With(BattleTerrain.Special, 49, 60, true, Of("elite_four_aaron")));
        // The first trainer's class alone is read (dto->trainer[1], 130): Flint first, and Volkner's own row is not met
        Meets(TransitionKind.Double, MeetingRow.DoubleBattle, With(BattleTerrain.Plain, 59, 60, true, Of("elite_four_flint_fight_area"), Of("leader_volkner_fight_area")));
        // 132-142 and 160-162 stand before 164 too: a Leader met at the Battle Frontier, or the Champion in a link
        // battle, would be met the Frontier's way
        Meets(TransitionKind.Frontier, MeetingRow.Frontier, new BattleMeeting { Trainers = new[] { Of("leader_roark") }, Frontier = true, Terrain = BattleTerrain.Special });
        Meets(TransitionKind.Frontier, MeetingRow.Link, new BattleMeeting { Trainers = new[] { Of("champion_cynthia") }, Link = true, Terrain = BattleTerrain.Building });
        // The band is the class's (EncEffects_TrainerClassEffect switches on the class alone, 214): someone named
        // Roark or Cynthia of another class is met as anyone is, by the ground and the first levels
        Meets(TransitionKind.Trainer, MeetingRow.Trainer, With(BattleTerrain.Building, 14, 20, false, new MetTrainer("Hiker", "Roark")));
        Meets(TransitionKind.TrainerCaveStrong, MeetingRow.Trainer, With(BattleTerrain.Cave, 30, 20, false, new MetTrainer("Ace Trainer", "Cynthia")));

        // enc_effects.c 265-268 (the grunts' two classes) and 144-146 (Team Galactic's own): a grunt, alone
        Meets(TransitionKind.Grunt, MeetingRow.GalacticGrunt, With("galactic_grunt_floaroma_meadow_1"));
        // 144-146 stand before the doubles check of 152: a pair of grunts, or a grunt in a tag battle, is a grunt's still
        Meets(TransitionKind.Grunt, MeetingRow.GalacticGrunt, With(BattleTerrain.Building, 9, 9, true, Of("galactic_grunt_jubilife_city_1"), Of("galactic_grunt_jubilife_city_2")));
        // 260-264 and the table's row 100: a Commander is met the boss's way
        Meets(TransitionKind.GalacticBoss, MeetingRow.GalacticCommander, With("commander_mars_valley_windworks"));
        Meets(TransitionKind.GalacticBoss, MeetingRow.GalacticCommander, With(BattleTerrain.Mountain, 44, 40, true, Of("commander_mars_lake_verity"), Of("commander_mars_valley_windworks")));
        // 257-259 and row 101: Cyrus
        Meets(TransitionKind.GalacticBoss, MeetingRow.GalacticBoss, With("galactic_boss_cyrus_galactic_hq"));
        Meets(TransitionKind.GalacticBoss, MeetingRow.GalacticBoss, With("galactic_boss_cyrus_distortion_world", BattleTerrain.Special, 45, 60));
        // Whoever is met on water or in a cave, Team Galactic's way is theirs (144-146 never ask the ground)
        Meets(TransitionKind.Grunt, MeetingRow.GalacticGrunt, With("galactic_grunt_floaroma_meadow_1", BattleTerrain.Cave, 30, 20));

        // 152-158 and row 104: the twins' double battle, and two trainers who see the player together
        Meets(TransitionKind.Double, MeetingRow.DoubleBattle, With("twins_liv_and_liz", two: true));
        Meets(TransitionKind.Double, MeetingRow.DoubleBattle, With(BattleTerrain.Grass, 20, 22, true, new MetTrainer("Bug Catcher", "Jack"), new MetTrainer("Lass", "Briana")));
        // The double's way whatever the ground and however strong (the ground is asked only by ENCEFF_CUTIN_USE_LOCAL)
        Meets(TransitionKind.Double, MeetingRow.DoubleBattle, With("twins_liv_and_liz", BattleTerrain.Water, 50, 5, two: true));

        // 132-142 and row 102: the Battle Frontier alone; 137-139: its doubles the double's way
        Meets(TransitionKind.Frontier, MeetingRow.Frontier, new BattleMeeting { Trainers = new[] { new MetTrainer("Ace Trainer", "Alina") }, Frontier = true, Terrain = BattleTerrain.Special, TheirLevel = 50, OwnLevel = 50 });
        Meets(TransitionKind.Double, MeetingRow.DoubleBattle, new BattleMeeting { Trainers = new[] { new MetTrainer("Ace Trainer", "Alina"), new MetTrainer("Idol", "Gabby") }, Double = true, Frontier = true, Terrain = BattleTerrain.Special });
        // 133-135 and 269-275: a Frontier Brain, the Frontier's way even in a double
        foreach (string brain in new[] { "Tower Tycoon", "Hall Matron", "Factory Head", "Arcade Star", "Castle Valet" })
        {
            Meets(TransitionKind.Frontier, MeetingRow.FrontierBrain, new BattleMeeting { Trainers = new[] { new MetTrainer(brain, "Whoever") }, Frontier = true, Terrain = BattleTerrain.Special });
            Meets(TransitionKind.Frontier, MeetingRow.FrontierBrain, new BattleMeeting { Trainers = new[] { new MetTrainer(brain, "Whoever") }, Frontier = true, Double = true, Terrain = BattleTerrain.Special });
            // 164 and row 106: a Brain met outside the Frontier is met the Frontier's way
            Meets(TransitionKind.Frontier, MeetingRow.FrontierBrain, new BattleMeeting { Trainers = new[] { new MetTrainer(brain, "Whoever") }, Terrain = BattleTerrain.Plain });
        }
        // The Frontier's check stands first (132): a grunt met there would be met its way
        Meets(TransitionKind.Frontier, MeetingRow.Frontier, new BattleMeeting { Trainers = new[] { new MetTrainer("Galactic", "Grunt") }, Frontier = true, Terrain = BattleTerrain.Special });

        // 160-162 and row 103: a link battle, the Frontier's way; in a double the doubles check of 152 comes first
        Meets(TransitionKind.Frontier, MeetingRow.Link, new BattleMeeting { Trainers = new[] { new MetTrainer("Pokémon Trainer", "Someone") }, Link = true, Terrain = BattleTerrain.Building });
        Meets(TransitionKind.Double, MeetingRow.DoubleBattle, new BattleMeeting { Trainers = new[] { new MetTrainer("Pokémon Trainer", "Someone") }, Link = true, Double = true, Terrain = BattleTerrain.Building });

        // 254-256 and row 88: the rival, met as anyone is (ENCEFF_CUTIN_USE_LOCAL): by the ground and the first levels
        // (encounter_effect.c 1282-1335)
        Meets(TransitionKind.Trainer, MeetingRow.Rival, With("rival_route_203_piplup", BattleTerrain.Grass, 9, 9));
        Meets(TransitionKind.TrainerStrong, MeetingRow.Rival, With("rival_route_203_piplup", BattleTerrain.Grass, 9, 8));
        Meets(TransitionKind.TrainerWater, MeetingRow.Rival, With("rival_route_203_piplup", BattleTerrain.Water, 9, 9));
        Meets(TransitionKind.TrainerWaterStrong, MeetingRow.Rival, With("rival_route_203_piplup", BattleTerrain.Water, 10, 9));
        Meets(TransitionKind.TrainerCave, MeetingRow.Rival, With("rival_route_203_piplup", BattleTerrain.Cave, 9, 12));
        Meets(TransitionKind.TrainerCaveStrong, MeetingRow.Rival, With("rival_route_203_piplup", BattleTerrain.Cave, 13, 12));
        // Any other trainer the same (row 108)
        Meets(TransitionKind.TrainerWater, MeetingRow.Trainer, With(BattleTerrain.Water, 30, 30, false, new MetTrainer("Swimmer", "Evan")));
        Meets(TransitionKind.TrainerCaveStrong, MeetingRow.Trainer, With(BattleTerrain.Cave, 31, 30, false, new MetTrainer("Hiker", "Louis")));

        // Row 109 and encounter_effect.c 1322-1327: a wild Pokémon met surfing, and in a cave, and a stronger one of each
        Meets(TransitionKind.WildWater, MeetingRow.Wild, Wild(BattleTerrain.Water, 20, 20));
        Meets(TransitionKind.WildWaterStrong, MeetingRow.Wild, Wild(BattleTerrain.Water, 21, 20));
        Meets(TransitionKind.WildCave, MeetingRow.Wild, Wild(BattleTerrain.Cave, 5, 9));
        Meets(TransitionKind.WildCaveStrong, MeetingRow.Wild, Wild(BattleTerrain.Cave, 10, 9));
        // 1309-1321: sand, snow and every other ground as on grass
        foreach (var ground in Enum.GetValues<BattleTerrain>().Where(g => g is not (BattleTerrain.Water or BattleTerrain.Cave)))
        {
            Meets(TransitionKind.Wild, MeetingRow.Wild, Wild(ground, 5, 5));
            Meets(TransitionKind.WildStrong, MeetingRow.Wild, Wild(ground, 6, 5));
            Meets(TransitionKind.Trainer, MeetingRow.Trainer, With(ground, 5, 5, false, new MetTrainer("Youngster", "Tristan")));
            Meets(TransitionKind.TrainerStrong, MeetingRow.Trainer, With(ground, 6, 5, false, new MetTrainer("Youngster", "Tristan")));
        }
        // 1330-1332: stronger is a difference above nought, so a first Pokémon of the same level is not
        Assert.False(Wild(BattleTerrain.Grass, 7, 7).Stronger);
        Assert.True(Wild(BattleTerrain.Grass, 8, 7).Stronger);
        // A battle of its own kind is met by its ground like any other: the Great Marsh's, Pal Park's, a roamer's
        Meets(TransitionKind.Wild, MeetingRow.Wild, Wild(BattleTerrain.GreatMarsh, 22, 30, kind: BattleKind.Safari));
        Meets(TransitionKind.WildWater, MeetingRow.Wild, Wild(BattleTerrain.Water, 50, 60, kind: BattleKind.PalPark));
        Meets(TransitionKind.WildStrong, MeetingRow.Wild, Wild(BattleTerrain.Grass, 50, 12, kind: BattleKind.Roamer));

        // 173-175 and row 105: two wild Pokémon at once, beside a partner
        Meets(TransitionKind.Double, MeetingRow.DoubleWild, Wild(BattleTerrain.Grass, 12, 14, two: true));
        Meets(TransitionKind.Double, MeetingRow.DoubleWild, Wild(BattleTerrain.Cave, 40, 14, two: true));
    }

    /// <summary>
    /// The rows plan 09 · L14 will take up: until then a legendary and a mythical Pokémon are met the wild one's
    /// way, by place.
    /// </summary>
    [Fact]
    public void TheRowsStillToComeKeepTheWaysTheyHad()
    {
        Meets(TransitionKind.WildStrong, MeetingRow.Wild, Wild(BattleTerrain.Special, 70, 60, species: "Dialga"));
        Meets(TransitionKind.WildCave, MeetingRow.Wild, Wild(BattleTerrain.Cave, 47, 60, species: "Giratina"));
    }

    /// <summary>
    /// Whose band a meeting opens on is the first trainer's, by class and name (the original has a way in to each of
    /// the thirteen); a meeting that opens any other way has none.
    /// </summary>
    [Fact]
    public void ABandIsTheFirstTrainersOwn()
    {
        var roark = SceneTransition.BandFor(With("leader_roark", BattleTerrain.Building, 14, 20));
        Assert.Equal(("Leader", "Roark"), (roark!.Value.Class, roark.Value.Name));
        Assert.Equal(roark, SceneTransition.BandFor(With("leader_roark_rematch", BattleTerrain.Building, 62, 60)));
        Assert.Equal(SceneTransition.BandOf("Champion", "Cynthia"), SceneTransition.BandFor(With("champion_cynthia", BattleTerrain.Special, 58, 70)));
        Assert.Equal(SceneTransition.BandOf("Elite Four", "Flint"), SceneTransition.BandFor(With("elite_four_flint_rematch", BattleTerrain.Special, 68, 70)));
        Assert.Null(SceneTransition.BandFor(With(BattleTerrain.Plain, 59, 60, true, Of("leader_volkner_fight_area"), Of("elite_four_flint_fight_area"))));
        Assert.Null(SceneTransition.BandFor(With(BattleTerrain.Building, 14, 20, false, new MetTrainer("Hiker", "Roark"))));
        Assert.Null(SceneTransition.BandFor(With("galactic_boss_cyrus_galactic_hq")));
        Assert.Null(SceneTransition.BandFor(Wild(BattleTerrain.Grass)));
        // A Leader the table doesn't name (another region's, before their band is written) wears the plain band under their own name
        var brock = SceneTransition.BandFor(With(BattleTerrain.Building, 12, 14, false, new MetTrainer("Leader", "Brock")));
        Assert.Equal(SceneTransition.PlainBand with { Class = "Leader", Name = "Brock" }, brock);
        Assert.DoesNotContain(brock!.Value, SceneTransition.Bands);
    }

    /// <summary>
    /// The thirteen bands (the style guide's table): every entry names a trainer of the game's data of that class,
    /// every Gym Leader, Elite Four member and Champion of the data has one, and no two wear the same colours.
    /// </summary>
    [Fact]
    public void EveryBandIsSomeonesAndEveryoneWithABandHasOne()
    {
        Assert.Equal(13, SceneTransition.Bands.Count);
        Assert.Equal((8, 4, 1), (SceneTransition.Bands.Count(b => b.Class == "Leader"), SceneTransition.Bands.Count(b => b.Class == "Elite Four"), SceneTransition.Bands.Count(b => b.Class == "Champion")));
        foreach (var band in SceneTransition.Bands)
        {
            Assert.Contains(TrainerDatabase.All, t => t.Class == band.Class && t.Name == band.Name);
            Assert.Equal(band, SceneTransition.BandOf(band.Class, band.Name));
            // Not the same name under another class
            Assert.Equal(SceneTransition.PlainBand with { Class = "Hiker", Name = band.Name }, SceneTransition.BandOf("Hiker", band.Name));
            // Its deep colour is darker than the band and its light one lighter, so lines of both read on it and white reads on the plate
            int Light(Color c) => c.R * 3 + c.G * 6 + c.B;
            Assert.True(Light(band.Deep) < Light(band.Base) - 300 && Light(band.Light) > Light(band.Base) + 300, $"{band.Name}'s colours");
            Assert.True(Light(band.Deep) < 1200, $"{band.Name}'s plate is too pale for a white name");
        }
        foreach (var trainer in TrainerDatabase.All.Where(t => t.Class is "Leader" or "Elite Four" or "Champion"))
            Assert.Contains(SceneTransition.Bands, b => b.Class == trainer.Class && b.Name == trainer.Name);
        Assert.Equal(13, SceneTransition.Bands.Select(b => Rgb(b.Base)).Distinct().Count());
        Assert.Equal(13, SceneTransition.Bands.Select(b => (b.Class, b.Name)).Distinct().Count());
        // The style guide's rows, a few by hand
        Assert.Equal(((172, 136, 92), (104, 76, 48), (232, 204, 152)), Colours("Leader", "Roark"));
        Assert.Equal(((236, 184, 44), (152, 100, 20), (252, 236, 148)), Colours("Leader", "Volkner"));
        Assert.Equal(((232, 100, 48), (148, 44, 28), (252, 188, 104)), Colours("Elite Four", "Flint"));
        Assert.Equal(((52, 56, 84), (22, 24, 40), (236, 200, 108)), Colours("Champion", "Cynthia"));
        Assert.Equal(((120, 132, 152), (64, 72, 92), (204, 212, 224)), (Rgb(SceneTransition.PlainBand.Base), Rgb(SceneTransition.PlainBand.Deep), Rgb(SceneTransition.PlainBand.Light)));
        // The card behind a portrait is the band's light colour, but for the two whose hair is that colour
        foreach (var band in SceneTransition.Bands.Where(b => b.Name is not ("Volkner" or "Cynthia")))
            Assert.Equal(band.Light, band.Backing);
        Assert.Equal(((196, 224, 252), (132, 140, 196)), (Rgb(SceneTransition.BandOf("Leader", "Volkner").Backing), Rgb(SceneTransition.BandOf("Champion", "Cynthia").Backing)));
        Assert.Equal(SceneTransition.PlainBand.Light, SceneTransition.PlainBand.Backing);
        // The white it all goes to, and the silhouette's one colour
        Assert.Equal(((250, 250, 252), (32, 32, 40)), (Rgb(SceneTransition.White), Rgb(SceneTransition.Silhouette)));
    }

    private static ((int, int, int), (int, int, int), (int, int, int)) Colours(string trainerClass, string name)
    {
        var band = SceneTransition.BandOf(trainerClass, name);
        return (Rgb(band.Base), Rgb(band.Deep), Rgb(band.Light));
    }

    /// <summary>
    /// The portrait is of the person the battle itself stands on its platform: the look the band asks for
    /// (<c>CharacterStyles.InBattle</c> of the trainer) is the one the battle's screen comes to for them.
    /// </summary>
    [Theory]
    [InlineData("leader_roark", "Roark")]
    [InlineData("leader_wake", "crasher_wake")]
    [InlineData("elite_four_aaron", "aaron")]
    [InlineData("champion_cynthia_rematch", "Cynthia")]
    [InlineData("leader_gardenia", null)]
    public void TheBandsPortraitIsOfWhomTheBattleDraws(string id, string? carriedBy)
    {
        var record = TrainerDatabase.Get(id)!;
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        trainer.Look = carriedBy;
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 50));
        var battle = new BattleEngine(party, trainer.Party.Members[0], new Inventory(), new Pokedex(), trainer);
        string look = CharacterStyles.Default.InBattle(trainer);
        Assert.Equal(battle.Anim.EnemyTrainer, look);
        // The person who carries them, where there is one: a Leader's class names nobody in particular
        Assert.Equal(carriedBy ?? record.Class, look);
        if (carriedBy != null) Assert.True(CharacterStyles.Default.Has(look), $"{look} is no look of the table");
    }

    /// <summary>Every trainer of the game's data falls in the row its class has in the original, and every class with a row of its own is one the data has.</summary>
    [Fact]
    public void EveryTrainersClassHasItsRow()
    {
        var rows = new Dictionary<string, MeetingRow>
        {
            ["Leader"] = MeetingRow.Leader, ["Elite Four"] = MeetingRow.EliteFour, ["Champion"] = MeetingRow.Champion, ["Rival"] = MeetingRow.Rival,
            ["Galactic"] = MeetingRow.GalacticGrunt, ["Commander"] = MeetingRow.GalacticCommander, ["Galactic Boss"] = MeetingRow.GalacticBoss,
            ["Tower Tycoon"] = MeetingRow.FrontierBrain, ["Hall Matron"] = MeetingRow.FrontierBrain, ["Factory Head"] = MeetingRow.FrontierBrain,
            ["Arcade Star"] = MeetingRow.FrontierBrain, ["Castle Valet"] = MeetingRow.FrontierBrain
        };
        var classes = TrainerDatabase.All.Select(t => t.Class).Distinct().ToList();
        foreach (var (name, row) in rows)
        {
            Assert.Contains(name, classes);
            Assert.Equal(row, BattleMeeting.ClassRow(name));
        }
        foreach (string name in classes.Where(c => !rows.ContainsKey(c)))
            Assert.Equal(MeetingRow.Trainer, BattleMeeting.ClassRow(name));
        // The Frontier's own trainers: its five Brains, and nobody else with a row of their own
        var frontier = FrontierDatabase.Trainers.Select(t => t.Class).Distinct().ToList();
        Assert.Equal(5, frontier.Count(c => BattleMeeting.ClassRow(c) == MeetingRow.FrontierBrain));
        Assert.All(frontier, c => Assert.Contains(BattleMeeting.ClassRow(c), new[] { MeetingRow.FrontierBrain, MeetingRow.Trainer }));
        // Cyrus is the boss, Mars a Commander, and the grunts are many
        Assert.Equal("Cyrus", Of("galactic_boss_cyrus_galactic_hq").Name);
        Assert.True(TrainerDatabase.All.Count(t => t.Class == "Galactic") > 40);
    }

    // ------------------------------------------------------------------ the times

    public static IEnumerable<object[]> Ways() => new (TransitionKind Kind, float Lead, float Shut, float Whole)[]
    {
        (TransitionKind.WildWater, 0.35f, 0.6f, 1.2f), (TransitionKind.WildWaterStrong, 0.35f, 0.6f, 1.2f),
        (TransitionKind.WildCave, 0f, 0.75f, 1f), (TransitionKind.WildCaveStrong, 0f, 0.75f, 1f),
        (TransitionKind.TrainerWater, 0.4f, 0.55f, 1.2f), (TransitionKind.TrainerWaterStrong, 0.2f, 0.9f, 1.35f),
        (TransitionKind.TrainerCave, 0.45f, 0.6f, 1.3f), (TransitionKind.TrainerCaveStrong, 0f, 1.2f, 1.45f),
        (TransitionKind.Double, 0f, 0.8f, 1.05f), (TransitionKind.Frontier, 0.45f, 0.55f, 1.25f),
        (TransitionKind.Grunt, 0f, 1.75f, 2f), (TransitionKind.GalacticBoss, 1f, 0.85f, 2.1f),
        // The bands (plan 09 · L15): the lead is the whole of the band's show, the closing its white
        (TransitionKind.LeaderBand, 2.7f, 0.5f, 3.45f), (TransitionKind.EliteBand, 2.6f, 0.3f, 3.15f)
    }.Select(w => new object[] { w.Kind, w.Lead, w.Shut, w.Whole });

    /// <summary>The style guide's table: each way's lead, its closing and the whole, flashes included.</summary>
    [Theory]
    [MemberData(nameof(Ways))]
    public void EachWayTakesTheTimeTheStyleGuideGivesIt(TransitionKind kind, float lead, float shut, float whole)
    {
        Assert.Equal(lead, SceneTransition.LeadSeconds(kind), 3);
        Assert.Equal(whole, SceneTransition.OutSeconds(kind), 3);
        Assert.Equal(SceneTransition.FlashSeconds + lead + shut, SceneTransition.OutSeconds(kind), 3);
        Assert.Equal(SceneTransition.OpenSeconds, SceneTransition.InSeconds(kind));
        // The lead runs from the flashes' end; nothing is dark until it is over, and half way through the closing some is
        Assert.Equal(0f, SceneTransition.Lead(kind, SceneTransition.FlashSeconds - 0.01f));
        Assert.Equal(1f, SceneTransition.Lead(kind, SceneTransition.FlashSeconds + lead + 0.001f));
        if (lead > 0f) Assert.InRange(SceneTransition.Lead(kind, SceneTransition.FlashSeconds + lead / 2f), 0.45f, 0.55f);
        Assert.Equal(0f, SceneTransition.Closed(kind, true, SceneTransition.FlashSeconds + lead));
        Assert.InRange(SceneTransition.Closed(kind, true, SceneTransition.FlashSeconds + lead + shut / 2f), 0.45f, 0.55f);
    }

    [Fact]
    public void TheWaysWithAPaceOfTheirOwnAreAllTheKindsButTheFirstFourAndTheFade()
    {
        // (The fifth of plan 04 · G9's ways, a Gym Leader's diamonds, went with the bands: plan 09, decision 5)
        var first = new[] { TransitionKind.Fade, TransitionKind.Wild, TransitionKind.WildStrong, TransitionKind.Trainer, TransitionKind.TrainerStrong };
        Assert.Equal(Enum.GetValues<TransitionKind>().Except(first).OrderBy(k => k), Ways().Select(w => (TransitionKind)w[0]).OrderBy(k => k));
        Assert.DoesNotContain("Leader", Enum.GetNames<TransitionKind>());
        // The first four lead in with nothing, and nothing rides over them
        foreach (var kind in first)
        {
            Assert.Equal(0f, SceneTransition.LeadSeconds(kind));
            Assert.Empty(PiecesAt(kind, 0.5f));
            Assert.False(SceneTransition.Ripple(kind, true, 0.5f).Plays);
            Assert.Equal(0, SceneTransition.Veil(kind, true, 0.5f));
            Assert.False(SceneTransition.IsBand(kind));
            Assert.False(SceneTransition.BandAt(kind, 0.5f, W, H).Shown);
        }
        // Only the bands close to white; every other way to the dark
        foreach (var kind in Enum.GetValues<TransitionKind>())
            Assert.Equal(SceneTransition.IsBand(kind) ? SceneTransition.White : SceneTransition.Dark, SceneTransition.CoverOf(kind));
    }

    // ------------------------------------------------------------------ what rides over the picture

    private static List<TransitionPiece> PiecesAt(TransitionKind kind, float seconds)
    {
        var into = new List<TransitionPiece>();
        SceneTransition.Pieces(kind, seconds, W, H, into);
        return into;
    }

    /// <summary>The moment a way's dark is this far closed.</summary>
    private static float When(TransitionKind kind, float closed)
    {
        float from = SceneTransition.FlashSeconds + SceneTransition.LeadSeconds(kind), until = SceneTransition.OutSeconds(kind);
        for (float t = from; t < until; t += 0.0005f)
            if (SceneTransition.Closed(kind, true, t) >= closed) return t;
        return until;
    }

    private static bool Covers(TransitionKind kind, float closed, float x, float y, bool closing = true, Vector2? focus = null) =>
        SceneTransition.Covers(kind, closing, closed, x, y, W, H, focus);

    [Theory]
    [MemberData(nameof(Ways))]
    public void NothingRidesOverTheFlashesNorOverTheShutPicture(TransitionKind kind, float lead, float shut, float whole)
    {
        Assert.Empty(PiecesAt(kind, 0f));
        Assert.Empty(PiecesAt(kind, SceneTransition.FlashSeconds - 0.01f));
        Assert.Empty(PiecesAt(kind, whole));
        Assert.Empty(PiecesAt(kind, whole + 1f));
        // The same moment gives the same pieces, and every piece is on or about the picture
        float mid = SceneTransition.FlashSeconds + lead + shut * 0.4f;
        Assert.Equal(PiecesAt(kind, mid), PiecesAt(kind, mid));
        foreach (var piece in PiecesAt(kind, mid))
        {
            Assert.InRange(piece.At.X, -W, 2f * W);
            Assert.InRange(piece.At.Y, -H, 2f * H);
            Assert.True(piece.Size >= 0f);
        }
    }

    [Fact]
    public void ATrainerOnWaterSendsABallSpinningInAtTheMiddle()
    {
        var kind = TransitionKind.TrainerWater;
        float from = SceneTransition.FlashSeconds, lead = SceneTransition.LeadSeconds(kind);
        // Nothing yet, then growing as it turns, then whole after one full turn
        var early = Assert.Single(PiecesAt(kind, from + lead * 0.25f));
        var late = Assert.Single(PiecesAt(kind, from + lead * 0.75f));
        var landed = Assert.Single(PiecesAt(kind, from + lead));
        Assert.All(new[] { early, late, landed }, p => Assert.Equal((TransitionPieceKind.Ball, Middle), (p.Kind, p.At)));
        Assert.True(early.Size < late.Size && late.Size < landed.Size);
        Assert.Equal(SceneTransition.BallSize, landed.Size, 2);
        Assert.Equal(MathF.Tau, landed.Turn, 3);
        Assert.True(early.Turn < late.Turn);
        // It stays whole, and turns on, as the dark wells out from under it; and it rides on the dark
        var riding = Assert.Single(PiecesAt(kind, When(kind, 0.5f)));
        Assert.Equal(SceneTransition.BallSize, riding.Size, 2);
        Assert.True(riding.Turn > landed.Turn);
        Assert.False(riding.Under);
        Assert.True(Covers(kind, 0.2f, Middle.X + 60f, Middle.Y));
        Assert.False(Covers(kind, 0.2f, 100f, 100f));
        Assert.True(Covers(kind, 0.97f, 100f, 100f));
    }

    [Fact]
    public void AStrongerTrainerOnWaterSendsThreeBallsUpEachWithItsThirdOfTheDark()
    {
        var kind = TransitionKind.TrainerWaterStrong;
        // One after another: at a sixth, five sixths and the half of the width
        float[] across = { W / 6f, W * 5f / 6f, W / 2f };
        Assert.Equal(new[] { across[0] }, PiecesAt(kind, When(kind, 0.2f)).Select(p => p.At.X));
        Assert.Equal(new[] { across[0], across[1] }, PiecesAt(kind, When(kind, 0.4f)).Select(p => p.At.X));
        Assert.Equal(new[] { across[1], across[2] }, PiecesAt(kind, When(kind, 0.6f)).Select(p => p.At.X));
        Assert.Equal(new[] { across[2] }, PiecesAt(kind, When(kind, 0.9f)).Select(p => p.At.X));
        // Each rises, turning once, and its third of the picture is dark below it and open above
        foreach (float closed in new[] { 0.2f, 0.4f, 0.6f, 0.9f })
        {
            float t = When(kind, closed), now = SceneTransition.Closed(kind, true, t);
            foreach (var ball in PiecesAt(kind, t))
            {
                Assert.Equal((TransitionPieceKind.Ball, SceneTransition.BallSize), (ball.Kind, ball.Size));
                Assert.InRange(ball.Turn, 0f, MathF.Tau);
                Assert.True(Covers(kind, now, ball.At.X, ball.At.Y + 12f));
                Assert.False(Covers(kind, now, ball.At.X, ball.At.Y - 12f));
            }
        }
        var first = PiecesAt(kind, When(kind, 0.1f))[0];
        var later = PiecesAt(kind, When(kind, 0.3f))[0];
        Assert.True(later.At.Y < first.At.Y && later.Turn > first.Turn);
        // A third that no ball has reached is open to its foot
        Assert.False(Covers(kind, 0.3f, W / 2f, H - 5f));
        Assert.True(Covers(kind, 0.3f, W / 6f, H - 5f));
    }

    [Fact]
    public void ATrainerInACaveSendsABallDownAndTheDarkShutsOnIt()
    {
        var kind = TransitionKind.TrainerCave;
        float from = SceneTransition.FlashSeconds, lead = SceneTransition.LeadSeconds(kind);
        var start = Assert.Single(PiecesAt(kind, from));
        var half = Assert.Single(PiecesAt(kind, from + lead / 2f));
        var landed = Assert.Single(PiecesAt(kind, from + lead));
        // From just above the picture, faster as it falls, growing from 24 across to 96 in one turn
        Assert.True(start.At.Y + start.Size / 2f <= 0f);
        Assert.Equal(24f, start.Size, 2);
        Assert.True(half.At.Y < Middle.Y / 2f, "it has fallen less than half the way in half the time");
        Assert.Equal(Middle.X, landed.At.X);
        Assert.Equal(Middle.Y, landed.At.Y, 1);
        Assert.Equal(SceneTransition.BallSize, landed.Size, 2);
        Assert.Equal(MathF.Tau, landed.Turn, 3);
        // Then it shrinks away between the lids
        var closing = Assert.Single(PiecesAt(kind, When(kind, 0.5f)));
        Assert.Equal(Middle, closing.At);
        Assert.Equal(SceneTransition.BallSize * 0.5f, closing.Size, 0);
        // The lids: the head and the foot go first, the middle row last, and the sides before the middle of a row
        Assert.True(Covers(kind, 0.5f, Middle.X, 20f));
        Assert.True(Covers(kind, 0.5f, Middle.X, H - 20f));
        Assert.False(Covers(kind, 0.5f, Middle.X, Middle.Y));
        Assert.False(Covers(kind, 0.5f, Middle.X, Middle.Y - 300f));
        Assert.True(Covers(kind, 0.5f, 5f, Middle.Y - 300f));
        Assert.False(Covers(kind, 0.95f, 20f, Middle.Y));
    }

    [Fact]
    public void AStrongerTrainerInACaveSendsThreeBallsDownEachTearingAStrip()
    {
        var kind = TransitionKind.TrainerCaveStrong;
        float[] across = { W / 2f, W * 13f / 16f, W * 3f / 16f };
        Assert.Equal(new[] { across[0] }, PiecesAt(kind, When(kind, 0.05f)).Select(p => p.At.X));
        Assert.Equal(new[] { across[0], across[1] }, PiecesAt(kind, When(kind, 0.15f)).Select(p => p.At.X));
        Assert.Equal(new[] { across[1], across[2] }, PiecesAt(kind, When(kind, 0.3f)).Select(p => p.At.X));
        Assert.Empty(PiecesAt(kind, When(kind, 0.5f)));
        // The second turns the other way
        var pair = PiecesAt(kind, When(kind, 0.15f));
        Assert.True(pair[0].Turn > 0f && pair[1].Turn < 0f);
        // Behind a falling ball its strip is dark, a ball's width across; before it and beside it the picture is open
        float t = When(kind, 0.15f), now = SceneTransition.Closed(kind, true, t);
        var ball = PiecesAt(kind, t)[0];
        Assert.True(Covers(kind, now, ball.At.X, ball.At.Y - 30f));
        Assert.False(Covers(kind, now, ball.At.X, ball.At.Y + 30f));
        Assert.True(Covers(kind, now, ball.At.X + 30f, 6f));
        Assert.False(Covers(kind, now, ball.At.X + 60f, 6f));
        // Its edge is toothed: twelve units narrower half a tooth down
        Assert.True(Covers(kind, now, ball.At.X + 46f, 0.5f));
        Assert.False(Covers(kind, now, ball.At.X + 46f, 12f));
        // Once all three are through the strips widen until they meet
        Assert.False(Covers(kind, 0.46f, across[0] + 200f, 500f));
        Assert.True(Covers(kind, 0.8f, across[0] + 200f, 500f));
        Assert.True(Covers(kind, 0.999f, 2f, 500f));
        Assert.True(Covers(kind, 0.999f, W - 2f, 500f));
    }

    [Fact]
    public void TheFrontiersMarkComesUpInThreeStepsAndShrinksAsItsIrisCloses()
    {
        var kind = TransitionKind.Frontier;
        float from = SceneTransition.FlashSeconds;
        Assert.Equal(new[] { 90, 170, 255, 255 }, new[] { 0.05f, 0.15f, 0.25f, 0.4f }.Select(s => Assert.Single(PiecesAt(kind, from + s)).Alpha));
        var standing = Assert.Single(PiecesAt(kind, from + 0.3f));
        Assert.Equal((TransitionPieceKind.FrontierMark, Middle, SceneTransition.FrontierMarkSize), (standing.Kind, standing.At, standing.Size));
        // As the five blades close on it, it shrinks to nothing and turns with them, a fifth of a turn in all
        var half = Assert.Single(PiecesAt(kind, When(kind, 0.5f)));
        Assert.InRange(half.Size, 140f, 160f);
        Assert.InRange(half.Turn, MathF.Tau / 10f - 0.02f, MathF.Tau / 10f + 0.02f);
        Assert.True(Assert.Single(PiecesAt(kind, When(kind, 0.98f))).Size < 10f);
        // The opening has five sides: the middle is the last to go, and the dark is the same a fifth of a turn round
        Assert.False(Covers(kind, 0.9f, Middle.X, Middle.Y));
        Assert.True(Covers(kind, 0.9f, Middle.X + 300f, Middle.Y));
        for (float angle = 0f; angle < MathF.Tau; angle += 0.37f)
            foreach (float far in new[] { 200f, 400f, 500f })
            {
                var p = Middle + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * far;
                var q = Middle + new Vector2(MathF.Cos(angle + MathF.Tau / 5f), MathF.Sin(angle + MathF.Tau / 5f)) * far;
                Assert.Equal(Covers(kind, 0.6f, p.X, p.Y), Covers(kind, 0.6f, q.X, q.Y));
            }
    }

    [Fact]
    public void AGruntsSixShardsFlyInOneAfterAnotherAndMeetAsTheMark()
    {
        var kind = TransitionKind.Grunt;
        int[] order = { 0, 3, 5, 2, 1, 4 };
        // One more is on its way, or in its place, after each sets out
        float[] sets = { 0.02f, 0.13f, 0.26f, 0.34f, 0.45f, 0.56f };
        for (int k = 0; k < sets.Length; k++)
        {
            var shards = PiecesAt(kind, When(kind, sets[k]));
            Assert.All(shards, s => Assert.Equal(TransitionPieceKind.GalacticShard, s.Kind));
            Assert.Equal(order.Take(k + 1), shards.Select(s => s.Part));
        }
        // A shard sets out at the rim, three times its size, and comes to its own place and size, turning as it comes
        var setting = PiecesAt(kind, When(kind, 0.005f))[0];
        Assert.True(Vector2.Distance(setting.At, Middle) > 1000f);
        Assert.InRange(setting.Size, 3f * SceneTransition.GruntMarkSize - 20f, 3f * SceneTransition.GruntMarkSize);
        Assert.InRange(MathF.Abs(setting.Turn), 1.9f * MathF.Tau, 2f * MathF.Tau);
        var come = PiecesAt(kind, When(kind, 0.31f))[0];
        Assert.Equal(0, come.Part);
        Assert.Equal(SceneTransition.GruntMarkSize, come.Size, 1);
        Assert.Equal(0f, come.Turn, 3);
        Assert.True(Vector2.Distance(come.At, Middle + GalacticMark.MiddleOf(0) * SceneTransition.GruntMarkSize / 2f) < 0.5f);
        // Behind each, its sixth of the picture is dark from the rim inward, and no other
        float reach = Middle.Length();
        Vector2 In(int facet, float far) => Middle + new Vector2(MathF.Cos((-60f + 60f * facet) * MathF.PI / 180f), MathF.Sin((-60f + 60f * facet) * MathF.PI / 180f)) * far;
        for (int k = 0; k < sets.Length; k++)
            for (int facet = 0; facet < 6; facet++)
            {
                var rim = In(facet, reach * 0.995f);
                Assert.Equal(Array.IndexOf(order, facet) <= k, Covers(kind, sets[k] + 0.005f, rim.X, rim.Y));
            }
        var half = In(0, reach * 0.5f);
        Assert.False(Covers(kind, 0.1f, half.X, half.Y));
        Assert.True(Covers(kind, 0.2f, half.X, half.Y));
        // The dark's edge is where the shard is: just behind it the picture is gone
        var flying = PiecesAt(kind, When(kind, 0.15f))[0];
        float now = SceneTransition.Closed(kind, true, When(kind, 0.15f));
        var behind = Middle + (flying.At - Middle) * 1.15f;
        Assert.True(Covers(kind, now, behind.X, behind.Y));
        // Then the mark stands whole on the dark, for the last sixth of the closing
        var whole = Assert.Single(PiecesAt(kind, When(kind, 0.85f)));
        Assert.Equal((TransitionPieceKind.GalacticMark, Middle, SceneTransition.GruntMarkSize, 0f), (whole.Kind, whole.At, whole.Size, whole.Turn));
        Assert.All(new[] { new Vector2(5, 5), new Vector2(W - 5, 5), new Vector2(5, H - 5), new Vector2(W - 5, H - 5), Middle + new Vector2(3, 4) },
            p => Assert.True(Covers(kind, 0.85f, p.X, p.Y)));
    }

    [Fact]
    public void TheBossesMarkRisesOutOfADimmingPictureAndItsCoreSwallowsIt()
    {
        var kind = TransitionKind.GalacticBoss;
        float from = SceneTransition.FlashSeconds;
        // The veil: three steps a sixth of a second apart, kept until the picture is shut; nothing of it as the battle opens
        Assert.Equal(new[] { 0, 70, 130, 180, 180, 180 }, new[] { from - 0.01f, from + 0.05f, from + 0.2f, from + 0.4f, from + 0.9f, from + 1.5f }.Select(s => SceneTransition.Veil(kind, true, s)));
        Assert.Equal(0, SceneTransition.Veil(kind, false, 0.2f));
        Assert.Equal(0, SceneTransition.Veil(TransitionKind.Grunt, true, from + 0.4f));
        // The mark grows from nothing to its size in half a second, stands, and lies under the dark that comes
        var rising = Assert.Single(PiecesAt(kind, from + 0.2f));
        var risen = Assert.Single(PiecesAt(kind, from + 0.5f));
        var standing = Assert.Single(PiecesAt(kind, from + 0.95f));
        var swallowed = Assert.Single(PiecesAt(kind, When(kind, 0.6f)));
        Assert.All(new[] { rising, risen, standing, swallowed }, p => Assert.Equal((TransitionPieceKind.GalacticMark, Middle, true), (p.Kind, p.At, p.Under)));
        Assert.InRange(rising.Size, 100f, SceneTransition.BossMarkSize - 20f);
        Assert.Equal(new[] { SceneTransition.BossMarkSize, SceneTransition.BossMarkSize, SceneTransition.BossMarkSize }, new[] { risen.Size, standing.Size, swallowed.Size });
        // The dark is a lozenge growing from the mark's own core: inside the core first, then over the mark, then the corners
        float core = GalacticMark.CoreReach * SceneTransition.BossMarkSize / 2f, mark = SceneTransition.BossMarkSize / 2f;
        Assert.True(Covers(kind, 0.03f, Middle.X + 20f, Middle.Y));
        Assert.False(Covers(kind, 0.03f, Middle.X + core + 5f, Middle.Y));
        Assert.True(Covers(kind, 0.2f, Middle.X, Middle.Y - mark));
        Assert.False(Covers(kind, 0.2f, Middle.X + 200f, Middle.Y + 200f));
        Assert.False(Covers(kind, 0.9f, 4f, 4f));
        Assert.True(Covers(kind, 0.9f, 300f, 300f));
        // A lozenge: as far along one axis as along the other
        Assert.Equal(Covers(kind, 0.3f, Middle.X + 440f, Middle.Y), Covers(kind, 0.3f, Middle.X, Middle.Y + 440f));
    }

    // ------------------------------------------------------------------ the dark

    [Fact]
    public void OnWaterThePictureWaversAndSinks()
    {
        // The ripple: none before the flashes are over, then a texel further every tenth of a second, to four
        foreach (var kind in new[] { TransitionKind.WildWater, TransitionKind.TrainerWater })
        {
            Assert.False(SceneTransition.Ripple(kind, true, 0.1f).Plays);
            Assert.Equal(new[] { 1, 2, 3, 4, 4, 4 }, new[] { 0.26f, 0.36f, 0.46f, 0.56f, 0.8f, 1.15f }.Select(s => SceneTransition.Ripple(kind, true, s).Reach));
            Assert.False(SceneTransition.Ripple(kind, true, SceneTransition.OutSeconds(kind)).Plays);
            Assert.False(SceneTransition.Ripple(kind, false, 0.1f).Plays);
        }
        // A stronger foe: to six
        foreach (var kind in new[] { TransitionKind.WildWaterStrong, TransitionKind.TrainerWaterStrong })
            Assert.Equal(new[] { 1, 4, 6, 6 }, new[] { 0.26f, 0.56f, 0.8f, 1.15f }.Select(s => SceneTransition.Ripple(kind, true, s).Reach));
        // The wave travels, stepped thirty times a second: the same within a thirtieth, further on in the next
        var a = SceneTransition.Ripple(TransitionKind.WildWater, true, 0.701f);
        var b = SceneTransition.Ripple(TransitionKind.WildWater, true, 0.72f);
        var c = SceneTransition.Ripple(TransitionKind.WildWater, true, 0.74f);
        Assert.Equal(a.Phase, b.Phase);
        Assert.Equal(MathF.Tau * 2f / 30f, c.Phase - b.Phase, 3);
        Assert.Equal(MathF.Tau * 3f / 30f, SceneTransition.Ripple(TransitionKind.WildWaterStrong, true, 0.74f).Phase - SceneTransition.Ripple(TransitionKind.WildWaterStrong, true, 0.72f).Phase, 3);
        // No other ground wavers
        foreach (var kind in Enum.GetValues<TransitionKind>().Except(new[] { TransitionKind.WildWater, TransitionKind.WildWaterStrong, TransitionKind.TrainerWater, TransitionKind.TrainerWaterStrong }))
            Assert.False(SceneTransition.Ripple(kind, true, 0.6f).Plays);
        // Its offsets are whole texels of the field, as the scene effects' are
        Assert.All(Enumerable.Range(0, SceneFx.RippleRows), row => Assert.InRange(SceneFx.RippleOffset(row, SceneTransition.Ripple(TransitionKind.WildWaterStrong, true, 1f)), -6, 6));

        // The dark rises from the foot: half way, the foot is gone and the head is still there
        Assert.True(Covers(TransitionKind.WildWater, 0.5f, 700f, H - 100f));
        Assert.False(Covers(TransitionKind.WildWater, 0.5f, 700f, 100f));
        // Its edge is a wave eighteen units high about the line it rests on, in blocks six units wide and three tall
        var edges = new List<float>();
        for (float x = 3f; x < W; x += 6f)
        {
            float edge = 0f;
            for (float y = 0f; y < H; y += 1.5f)
                if (Covers(TransitionKind.WildWater, 0.5f, x, y)) { edge = y; break; }
            edges.Add(edge);
            Assert.Equal(Covers(TransitionKind.WildWater, 0.5f, x - 2.9f, edge - 1f), Covers(TransitionKind.WildWater, 0.5f, x + 2.9f, edge - 1f));
            Assert.Equal(0f, edge % 3f, 3);
        }
        Assert.InRange(edges.Min(), H / 2f - 19f, H / 2f - 15f);
        Assert.InRange(edges.Max(), H / 2f + 15f, H / 2f + 19f);
        // A stronger one: from the foot and the head at once, the middle last
        Assert.True(Covers(TransitionKind.WildWaterStrong, 0.5f, 700f, H - 100f));
        Assert.True(Covers(TransitionKind.WildWaterStrong, 0.5f, 700f, 100f));
        Assert.False(Covers(TransitionKind.WildWaterStrong, 0.5f, 700f, H / 2f));
        Assert.False(Covers(TransitionKind.WildWaterStrong, 0.9f, 700f, H / 2f));
    }

    [Fact]
    public void InACaveTheDarkClosesInARingOnThePlayer()
    {
        // Round the middle when nobody says where the player stands
        Assert.False(Covers(TransitionKind.WildCave, 0.5f, Middle.X, Middle.Y));
        Assert.True(Covers(TransitionKind.WildCave, 0.5f, 30f, 30f));
        Assert.Equal(Covers(TransitionKind.WildCave, 0.5f, Middle.X + 500f, Middle.Y), Covers(TransitionKind.WildCave, 0.5f, Middle.X, Middle.Y + 500f));
        // On the player, wherever they stand on the picture: the last point to go, and nothing is covered too soon
        var player = new Vector2(420f, 760f);
        Assert.False(Covers(TransitionKind.WildCave, 0.97f, player.X, player.Y, focus: player));
        Assert.True(Covers(TransitionKind.WildCave, 0.97f, Middle.X, Middle.Y, focus: player));
        foreach (var kind in new[] { TransitionKind.WildCave, TransitionKind.WildCaveStrong })
        {
            foreach (var corner in new[] { new Vector2(1, 1), new Vector2(W - 1, 1), new Vector2(1, H - 1), new Vector2(W - 1, H - 1) })
                Assert.False(Covers(kind, 0.0001f, corner.X, corner.Y, focus: player));
            // The battle opens through the iris at the picture's middle, whatever the field's ring closed on
            Assert.False(Covers(kind, 0.9f, Middle.X, Middle.Y, closing: false, focus: player));
            Assert.True(Covers(kind, 0.9f, player.X, player.Y, closing: false, focus: player));
        }
        // A stronger one's ring is toothed: twenty-four teeth, each a fifth of the ring's radius deep
        float open = 0.5f * Middle.Length() / 0.8f;
        Vector2 Round(float teeth, float far) => Middle + new Vector2(MathF.Cos(teeth * MathF.Tau / 24f), MathF.Sin(teeth * MathF.Tau / 24f)) * far;
        for (int tooth = 0; tooth < 24; tooth++)
        {
            var between = Round(tooth, open * 0.97f);
            var tip = Round(tooth + 0.5f, open * 0.83f);
            var past = Round(tooth + 0.5f, open * 0.77f);
            Assert.False(Covers(TransitionKind.WildCaveStrong, 0.5f, between.X, between.Y));
            Assert.True(Covers(TransitionKind.WildCaveStrong, 0.5f, tip.X, tip.Y));
            Assert.False(Covers(TransitionKind.WildCaveStrong, 0.5f, past.X, past.Y));
        }
    }

    [Fact]
    public void TwoAtOnceCloseInFourPiecesFromTheCorners()
    {
        var kind = TransitionKind.Double;
        foreach (var corner in new[] { new Vector2(10, 10), new Vector2(W - 10, 10), new Vector2(10, H - 10), new Vector2(W - 10, H - 10) })
            Assert.True(Covers(kind, 0.1f, corner.X, corner.Y));
        // What is left is a cross: the middle, and the whole of the middle row and column
        Assert.False(Covers(kind, 0.9f, Middle.X, Middle.Y));
        Assert.False(Covers(kind, 0.9f, 5f, Middle.Y));
        Assert.False(Covers(kind, 0.9f, Middle.X, 5f));
        Assert.True(Covers(kind, 0.9f, Middle.X - 200f, Middle.Y - 120f));
        // Each piece is a quarter of the picture and has come as far as the dark has closed
        Assert.True(Covers(kind, 0.5f, W / 4f - 5f, H / 4f - 5f));
        Assert.False(Covers(kind, 0.5f, W / 4f + 5f, H / 4f - 5f));
        Assert.False(Covers(kind, 0.5f, W / 4f - 5f, H / 4f + 5f));
    }

    /// <summary>
    /// Every way's dark is a shape of blocks or straight and round edges, never a haze: a point is under it or not,
    /// and the answer is the same a hair's breadth away almost everywhere.
    /// </summary>
    [Theory]
    [MemberData(nameof(Ways))]
    public void TheDarkHasCleanEdges(TransitionKind kind, float lead, float shut, float whole)
    {
        Assert.True(lead + shut < whole);
        int edges = 0, points = 0;
        for (float y = 11f; y < H; y += 37f)
            for (float x = 13f; x < W; x += 41f)
            {
                points++;
                if (Covers(kind, 0.55f, x, y) != Covers(kind, 0.55f, x + 0.25f, y + 0.25f)) edges++;
            }
        Assert.True(edges < points / 50, $"{edges} of {points} points lie on an edge");
    }

    // ------------------------------------------------------------------ the bands

    /// <summary>What a band shows this long after its flashes.</summary>
    private static BandLook Band(TransitionKind kind, float since) => SceneTransition.BandAt(kind, SceneTransition.FlashSeconds + since, W, H);

    /// <summary>The style guide's table of beats and its sizes, held to the code.</summary>
    [Fact]
    public void ABandsBeatsAndSizesAreTheStyleGuides()
    {
        Assert.Equal(new BandBeats(Opens: 0.2f, MarkFrom: 0.55f, MarkTakes: 0.5f, SlideFrom: 1.1f, SlideTakes: 0.15f, FlashFrom: 1.6f, LitAt: 1.7f,
            FlashEnds: 1.8f, NameFrom: 1.7f, TrembleFrom: 2.7f, Lead: 2.7f, Shut: 0.5f), SceneTransition.Beats(TransitionKind.LeaderBand));
        Assert.Equal(new BandBeats(Opens: 0.2f, MarkFrom: 0.2f, MarkTakes: 0.5f, SlideFrom: 0f, SlideTakes: 0.2f, FlashFrom: 1f, LitAt: 1.1f,
            FlashEnds: 1.3f, NameFrom: 0.2f, TrembleFrom: 1.55f, Lead: 2.6f, Shut: 0.3f), SceneTransition.Beats(TransitionKind.EliteBand));
        Assert.Equal(default, SceneTransition.Beats(TransitionKind.Grunt));
        foreach (var kind in new[] { TransitionKind.LeaderBand, TransitionKind.EliteBand })
        {
            // The beats come in their order, and the transition's own two are the band's
            var beats = SceneTransition.Beats(kind);
            Assert.True(beats.Opens <= beats.MarkFrom && beats.MarkFrom + beats.MarkTakes <= beats.FlashFrom);
            Assert.True(beats.SlideFrom + beats.SlideTakes < beats.FlashFrom && beats.FlashFrom < beats.LitAt && beats.LitAt < beats.FlashEnds);
            Assert.True(beats.FlashEnds <= beats.TrembleFrom && beats.TrembleFrom <= beats.Lead);
            Assert.Equal(beats.Lead, SceneTransition.LeadSeconds(kind));
            Assert.Equal(SceneTransition.FlashSeconds + beats.Lead + beats.Shut, SceneTransition.OutSeconds(kind), 3);
        }
        // Near the original's frames at thirty a second: a Leader's eighty or so of show and fifteen of white, the Elite
        // Four's eighty or so and eight (EncounterEffect_GymLeader, EncounterEffect_EliteFourChampion)
        Assert.InRange(SceneTransition.Beats(TransitionKind.LeaderBand).Lead * 30f, 75f, 90f);
        Assert.Equal(15f, SceneTransition.Beats(TransitionKind.LeaderBand).Shut * 30f, 1);
        Assert.InRange(SceneTransition.Beats(TransitionKind.EliteBand).Lead * 30f, 70f, 90f);
        Assert.InRange(SceneTransition.Beats(TransitionKind.EliteBand).Shut * 30f, 8f, 9.5f);

        Assert.Equal((300f, 440f, 150f, 52f), (SceneTransition.BandHeight, SceneTransition.PortraitSize, SceneTransition.MarkSize, SceneTransition.NameSize));
        // The card behind a portrait is narrower than the portrait's picture and wider than a head
        Assert.Equal(380f, SceneTransition.CardWide);
        Assert.Equal((0.78f, 0.22f, 0.28f), (SceneTransition.TheirPlace, SceneTransition.OwnPlace, SceneTransition.LeaderMarkPlace));
        Assert.Equal((1500f, 200, 130, 15f), (SceneTransition.BandPace, SceneTransition.BandDim, SceneTransition.BandHalfDim, SceneTransition.TrembleReach));
        // A portrait is rendered at the size it is drawn at on the 4K picture, turned 20° toward the middle, with the angry face
        Assert.Equal(2 * (int)SceneTransition.PortraitSize, BandPortraits.Pixels);
        Assert.Equal(20f, BandPortraits.Turn * 180f / MathF.PI, 0);
        Assert.Equal(Expression.Angry, BandPortraits.Face);
        Assert.Equal((0.84f, 0.04f), (CharacterStudio.BustHeight, CharacterStudio.BustLift));
    }

    [Fact]
    public void ALeadersBandOpensThenTheirPortraitSlidesInAndLightsUpBesideTheirName()
    {
        var kind = TransitionKind.LeaderBand;
        // Nothing during the flashes, nothing once the picture is shut, nothing for a kind that is no band
        Assert.False(SceneTransition.BandAt(kind, 0.1f, W, H).Shown);
        Assert.False(SceneTransition.BandAt(kind, SceneTransition.OutSeconds(kind), W, H).Shown);
        Assert.False(SceneTransition.BandAt(TransitionKind.Grunt, 1f, W, H).Shown);
        // The same moment gives the same look
        Assert.Equal(Band(kind, 1.3f), Band(kind, 1.3f));

        // The band: 300 units across the middle, wiped in at an even pace in a fifth of a second
        var opening = Band(kind, 0.1f);
        Assert.True(opening.Shown);
        Assert.Equal((390f, 690f), (opening.Top, opening.Bottom));
        Assert.Equal(0.5f, opening.Open, 3);
        Assert.Equal(1f, Band(kind, 0.21f).Open);
        Assert.Equal(1f, Band(kind, 2f).Open);
        // Its lines run on in whole units, 1500 a second, from the first moment to the last
        Assert.InRange(opening.Scroll, 149, 150);
        Assert.InRange(Band(kind, 1f).Scroll, 1499, 1500);
        Assert.InRange(Band(kind, 3f).Scroll, 4499, 4500);
        // Nobody yet: no second portrait in a Leader's band, theirs beyond the right edge, no name, no mark, nothing dimmed
        Assert.False(opening.Two);
        Assert.True(opening.Theirs.X - SceneTransition.PortraitSize / 2f >= W);
        Assert.Equal((false, 0, 0, 0f, 0f, 0f), (opening.Lit, opening.Dim, opening.NameAlpha, opening.MarkCome, opening.Flash, opening.White));

        // The mark stamps in from 0.55 s for half a second, at 0.28 of the width on the band's middle row
        Assert.Equal(0f, Band(kind, 0.54f).MarkCome);
        Assert.Equal(0.5f, Band(kind, 0.8f).MarkCome, 3);
        Assert.Equal(1f, Band(kind, 1.06f).MarkCome);
        Assert.Equal(new Vector2(0.28f * W, H / 2f), Band(kind, 0.8f).Mark);

        // Their portrait slides in from 1.1 s, fast and then slowing, to 0.78 of the width with its foot on the band's
        // foot: a silhouette, and nothing else has changed
        Assert.True(Band(kind, 1.09f).Theirs.X - SceneTransition.PortraitSize / 2f >= W);
        float from = W + SceneTransition.PortraitSize / 2f + 20f, to = 0.78f * W;
        var sliding = Band(kind, 1.175f);
        Assert.InRange(sliding.Theirs.X, to, to + (from - to) * 0.3f);
        var standing = Band(kind, 1.4f);
        Assert.Equal(new Vector2(to, 690f), standing.Theirs);
        Assert.Equal((false, 0, 0, 0f), (standing.Lit, standing.Dim, standing.NameAlpha, standing.Flash));

        // The flash: three steps up to 1.7 s, where the silhouette lights, the name stands and the picture round the
        // band lies under the dark, and three steps back
        Assert.Equal(new[] { 0f, 0.25f, 0.5f, 0.75f, 0.75f, 0.5f, 0.25f, 0f },
            new[] { 1.58f, 1.615f, 1.65f, 1.685f, 1.715f, 1.75f, 1.785f, 1.82f }.Select(s => Band(kind, s).Flash));
        Assert.Equal((false, 0, 0), (Band(kind, 1.69f).Lit, Band(kind, 1.69f).Dim, Band(kind, 1.69f).NameAlpha));
        var lit = Band(kind, 1.71f);
        Assert.Equal((true, 200, 255), (lit.Lit, lit.Dim, lit.NameAlpha));
        // The name's plate ends behind the portrait's picture, 96 units in from its edge, low in the band
        Assert.Equal(new Vector2(to - 220f + 96f, 540f + 84f), lit.Name);
        Assert.True(lit.Name.Y + 38f < lit.Bottom && lit.Name.X < standing.Theirs.X);

        // Held to 2.7 s, nothing moving but the lines; then the white closes for half a second
        var held = Band(kind, 2.6f);
        Assert.Equal((standing.Theirs, lit.Name, lit.Mark, true, 200, 255, 0f, 0f), (held.Theirs, held.Name, held.Mark, held.Lit, held.Dim, held.NameAlpha, held.Flash, held.White));
        Assert.InRange(Band(kind, 2.95f).White, 0.45f, 0.55f);
        Assert.True(Band(kind, 3.19f).White > 0.99f);
        Assert.Equal(standing.Theirs, Band(kind, 3.1f).Theirs);
    }

    [Fact]
    public void TheLeaguesBandBringsBothPortraitsInThenTheyTrembleAndFlyApart()
    {
        var kind = TransitionKind.EliteBand;
        float ownAt = 0.22f * W, theirAt = 0.78f * W, half = SceneTransition.PortraitSize / 2f;
        // Two halves from the sides, slowing as they meet, each with its portrait riding it in: both silhouettes, and
        // the picture round them half dimmed from the first moment
        var first = Band(kind, 0.001f);
        Assert.True(first.Two);
        Assert.True(first.Own.X + half < 0f && first.Theirs.X - half > W);
        var coming = Band(kind, 0.05f);
        Assert.InRange(coming.Open, 0.4f, 0.48f);
        float off = (1f - coming.Open) * W / 2f;
        Assert.Equal(new Vector2(ownAt - off, 690f), coming.Own);
        Assert.Equal(new Vector2(theirAt + off, 690f), coming.Theirs);
        Assert.Equal((false, 130, 0, 0f), (coming.Lit, coming.Dim, coming.NameAlpha, coming.MarkCome));
        var met = Band(kind, 0.21f);
        Assert.Equal((1f, new Vector2(ownAt, 690f), new Vector2(theirAt, 690f)), (met.Open, met.Own, met.Theirs));
        // Once they have met: the mark at the middle, and the name under theirs, hanging from the band's foot
        Assert.Equal(new Vector2(W / 2f, H / 2f), met.Mark);
        Assert.Equal((255, new Vector2(theirAt, 690f + 28f)), (met.NameAlpha, met.Name));
        Assert.InRange(Band(kind, 0.45f).MarkCome, 0.45f, 0.55f);
        Assert.Equal(1f, Band(kind, 0.71f).MarkCome);

        // The flash: up to 1.1 s, where both light and the dark round the band deepens, and back by 1.3 s
        Assert.Equal(new[] { 0f, 0.25f, 0.5f, 0.75f, 0.75f, 0.5f, 0.25f, 0f },
            new[] { 0.98f, 1.015f, 1.05f, 1.085f, 1.13f, 1.2f, 1.27f, 1.32f }.Select(s => Band(kind, s).Flash));
        Assert.Equal((false, 130), (Band(kind, 1.09f).Lit, Band(kind, 1.09f).Dim));
        Assert.Equal((true, 200), (Band(kind, 1.11f).Lit, Band(kind, 1.11f).Dim));

        // Held to 1.55 s. Then they tremble: thrown apart and at one another in turn every fifteenth of a second,
        // further each time, in whole units, to 15; their feet stay on the band's foot and the name where it was
        var held = Band(kind, 1.5f);
        Assert.Equal((met.Own, met.Theirs, met.Name), (held.Own, held.Theirs, held.Name));
        float last = 0f;
        for (int step = 0; step < 15; step++)
        {
            var now = Band(kind, 1.55f + (step + 0.5f) / 15f);
            float thrown = now.Own.X - ownAt;
            Assert.Equal(-thrown, now.Theirs.X - theirAt, 3);
            Assert.Equal((690f, 690f, 255, 0f), (now.Own.Y, now.Theirs.Y, now.NameAlpha, now.White));
            Assert.Equal(MathF.Round(thrown), thrown, 3);
            Assert.True(MathF.Abs(thrown) >= last, $"step {step} is thrown less far than the one before");
            if (MathF.Abs(thrown) > 0.5f) Assert.Equal(step % 2 == 0 ? -1 : 1, MathF.Sign(thrown));
            last = MathF.Abs(thrown);
        }
        Assert.InRange(last, 12f, SceneTransition.TrembleReach);

        // Then the white, for three tenths of a second: the name gone, the portraits flying apart along the falling
        // diagonal, the player's up and to the left and theirs down and to the right, fast at first, and out of the
        // picture before it is shut
        var flying = Band(kind, 2.75f);
        Assert.InRange(flying.White, 0.45f, 0.55f);
        Assert.Equal(0, flying.NameAlpha);
        Assert.True(flying.Own.X < ownAt - 400f && flying.Own.Y < 690f - 300f);
        Assert.True(flying.Theirs.X > theirAt + 400f && flying.Theirs.Y > 690f + 300f);
        Assert.True(Vector2.Distance(flying.Own - met.Own, met.Theirs - flying.Theirs) < 0.01f);
        float early = Vector2.Distance(Band(kind, 2.65f).Own, Band(kind, 2.61f).Own), late = Vector2.Distance(Band(kind, 2.89f).Own, Band(kind, 2.85f).Own);
        Assert.True(early > late * 1.3f, $"{early} at first, {late} at the end");
        var gone = Band(kind, 2.89f);
        Assert.True(gone.Own.X + half < 0f && gone.Theirs.X - half > W);
    }

    [Fact]
    public void ABandsWhiteClosesAsACleanShapeAndTheBattleOpensOnItInReverse()
    {
        // A Leader's: a bar that widens from the band's middle row to the head and the foot of the picture
        var leader = TransitionKind.LeaderBand;
        Assert.True(Covers(leader, 0.1f, 5f, Middle.Y + 40f));
        Assert.True(Covers(leader, 0.1f, W - 5f, Middle.Y - 40f));
        Assert.False(Covers(leader, 0.1f, Middle.X, Middle.Y + 60f));
        // It has swallowed the band itself by 0.28 of the way (150 units of 540)
        Assert.False(Covers(leader, 0.25f, Middle.X, 392f));
        Assert.True(Covers(leader, 0.3f, Middle.X, 392f));
        Assert.False(Covers(leader, 0.95f, Middle.X, 10f));
        Assert.False(Covers(leader, 0.95f, 3f, H - 10f));
        // The same all along a row, and the same above the middle row as below it
        for (float x = 3f; x < W; x += 97f)
            Assert.Equal(Covers(leader, 0.6f, 3f, 300f), Covers(leader, 0.6f, x, 300f));
        for (float far = 5f; far < H / 2f; far += 31f)
            Assert.Equal(Covers(leader, 0.6f, 700f, Middle.Y - far), Covers(leader, 0.6f, 700f, Middle.Y + far));

        // The League's: a slash along the rising diagonal, from the lower left corner to the upper right, which
        // widens to the two corners the portraits fly to
        var league = TransitionKind.EliteBand;
        Assert.True(Covers(league, 0.05f, 10f, H - 6f));
        Assert.True(Covers(league, 0.05f, W - 10f, 6f));
        Assert.True(Covers(league, 0.05f, Middle.X, Middle.Y));
        Assert.False(Covers(league, 0.05f, Middle.X - 200f, Middle.Y));
        Assert.False(Covers(league, 0.95f, 8f, 8f));
        Assert.False(Covers(league, 0.95f, W - 8f, H - 8f));
        Assert.True(Covers(league, 0.95f, 200f, 200f));
        // The same on either side of the slash, and along it
        for (float x = 40f; x < W; x += 173f)
            for (float y = 30f; y < H; y += 119f)
            {
                Assert.Equal(Covers(league, 0.4f, x, y), Covers(league, 0.4f, W - x, H - y));
                float along = 0.1f;
                if (x + along * W / 2f < W && y - along * H / 2f > 0f)
                    Assert.Equal(Covers(league, 0.4f, x, y), Covers(league, 0.4f, x + along * W / 2f, y - along * H / 2f));
            }

        // The battle opens on the same shape in reverse, out of the white; and where the player stands changes neither
        foreach (var kind in new[] { leader, league })
            for (float closed = 0.15f; closed < 1f; closed += 0.2f)
                for (float x = 40f; x < W; x += 173f)
                    for (float y = 30f; y < H; y += 119f)
                    {
                        Assert.Equal(Covers(kind, closed, x, y), Covers(kind, closed, x, y, closing: false));
                        Assert.Equal(Covers(kind, closed, x, y), Covers(kind, closed, x, y, focus: new Vector2(300f, 800f)));
                    }
    }

    /// <summary>
    /// A band's portraits are asked for as it begins and gone when it ends; until a frame has rendered them the band is
    /// drawn with plain busts, so nothing waits for a model. (Rendering itself needs a window: the harness's <c>life</c>
    /// mode prints how many are rendered at each of its shots.)
    /// </summary>
    [Fact]
    public void ABandsPortraitsAreAskedForAtItsBeginningAndGoneAtItsEnd()
    {
        var portraits = new BandPortraits();
        Assert.False(portraits.Active);
        Assert.Null(portraits.Picture);
        var roark = SceneTransition.BandOf("Leader", "Roark");
        portraits.Begin(roark, "Roark", null);
        Assert.True(portraits.Active);
        Assert.Equal(("Roark", null, 0), (portraits.TheirLook, portraits.OwnLook, portraits.Rendered));
        Assert.Equal(new BandPicture(roark, null, null), portraits.Picture);
        // Another band begun over it takes its place
        var cynthia = SceneTransition.BandOf("Champion", "Cynthia");
        portraits.Begin(cynthia, "Cynthia", "PLAYER");
        Assert.Equal(("Cynthia", "PLAYER", 0, cynthia), (portraits.TheirLook, portraits.OwnLook, portraits.Rendered, portraits.Picture!.Value.Band));
        portraits.End();
        Assert.False(portraits.Active);
        Assert.Null(portraits.Picture);
        Assert.Equal((null, null), (portraits.TheirLook, portraits.OwnLook));
        // Ending what was never begun is nothing
        portraits.End();
    }

    // ------------------------------------------------------------------ the Galactic mark

    private static (int R, int G, int B) Rgb(Color c) => (c.R, c.G, c.B);

    [Fact]
    public void TheGalacticMarkIsALozengeOfSixFacetsRoundADarkCore()
    {
        // The style guide's colours
        Assert.Equal(((248, 228, 128), (232, 204, 76), (196, 164, 48)), (Rgb(GalacticMark.Light), Rgb(GalacticMark.Base), Rgb(GalacticMark.Shade)));
        Assert.Equal(((52, 44, 62), (120, 230, 220)), (Rgb(GalacticMark.Ink), Rgb(GalacticMark.Teal)));
        Assert.Equal((6, 1f / 3f), (GalacticMark.Facets, GalacticMark.CoreReach));

        // The six facets fill the lozenge between them (its area is 2), each starting at the middle, and each
        // facet's own middle is a point of it
        float area = 0f;
        for (int facet = 0; facet < GalacticMark.Facets; facet++)
        {
            var shape = GalacticMark.Facet(facet).ToArray();
            Assert.Equal(Vector2.Zero, shape[0]);
            Assert.InRange(shape.Length, 3, 4);
            for (int i = 1; i + 1 < shape.Length; i++)
                area += MathF.Abs((shape[i].X - shape[0].X) * (shape[i + 1].Y - shape[0].Y) - (shape[i + 1].X - shape[0].X) * (shape[i].Y - shape[0].Y)) / 2f;
            foreach (var corner in shape) Assert.True(MathF.Abs(corner.X) + MathF.Abs(corner.Y) <= 1.0001f);
            var middle = GalacticMark.MiddleOf(facet);
            Assert.Equal(MarkPart.Facet, GalacticMark.At(middle.X, middle.Y, out int found));
            Assert.Equal(facet, found);
            // Lit from above: the upper two, the two at the sides, the lower two
            Assert.Equal(middle.Y < -0.1f ? GalacticMark.Light : middle.Y > 0.1f ? GalacticMark.Shade : GalacticMark.Base, GalacticMark.ShadeOf(facet));
        }
        Assert.Equal(2f, area, 4);
        // The same mark in a mirror: a facet on the left for each on the right
        for (int facet = 0; facet < GalacticMark.Facets; facet++)
        {
            var here = GalacticMark.MiddleOf(facet);
            var there = GalacticMark.MiddleOf(5 - facet);
            Assert.Equal((-here.X, here.Y), (there.X, there.Y));
            Assert.Equal(GalacticMark.ShadeOf(facet), GalacticMark.ShadeOf(5 - facet));
        }

        // What a point is: the heart, the core round it, a seam to the top point and to the middle of a side, nothing outside
        Assert.Equal(MarkPart.Heart, GalacticMark.At(0.02f, -0.03f, out _));
        Assert.Equal(MarkPart.Core, GalacticMark.At(0.2f, 0.05f, out _));
        Assert.Equal(MarkPart.Seam, GalacticMark.At(0.005f, -0.7f, out _));
        Assert.Equal(MarkPart.Seam, GalacticMark.At(0.4f, 0.41f, out _));
        Assert.Equal(MarkPart.Facet, GalacticMark.At(0.75f, 0f, out int right));
        Assert.Equal(1, right);
        Assert.Equal(MarkPart.None, GalacticMark.At(0.6f, 0.6f, out _));
        // No seam runs to the left or the right point: the two side facets are whole
        Assert.Equal(MarkPart.Facet, GalacticMark.At(-0.8f, 0.001f, out int left));
        Assert.Equal(4, left);
    }

    /// <summary>The sign over the doors of Team Galactic's buildings as it was painted before there was one painter: a flat lozenge with a core four texels square.</summary>
    private static PixelCanvas SignAsItWas(int size, Color yellow, Color ink)
    {
        var c = new PixelCanvas(40, 40);
        int cx = 20, y = 8, half = size / 2;
        for (int py = 0; py < size; py++)
        {
            int span = half - Math.Abs(py - half);
            c.Rect(cx - span, y + py, Math.Max(1, span * 2), 1, yellow);
        }
        c.Rect(cx - 2, y + half - 2, 4, 4, ink);
        return c;
    }

    [Fact]
    public void ThePainterLaysTheSameMarkDownAtEverySize()
    {
        // Small, it is what the sign over their doors always was, texel for texel
        var accent = new Color(232, 204, 76, 255);
        var ink = new Color(52, 44, 62, 255);
        foreach (int size in new[] { 10, 12, 13, 14, 18, 22 })
        {
            var was = SignAsItWas(size, accent, ink);
            var now = new PixelCanvas(40, 40);
            GalacticMark.Paint(now, 20, 8, size, ink, accent);
            for (int y = 0; y < 40; y++)
                for (int x = 0; x < 40; x++)
                    Assert.Equal(was.Get(x, y), now.Get(x, y));
        }

        // Larger, the same outline cut into its facets: three shades, ink for the seams, the rim and the core
        foreach (int size in new[] { 24, 32, 48, 64, 96 })
        {
            var c = new PixelCanvas(size + 8, size + 8);
            int cx = (size + 8) / 2, top = 4, half = size / 2;
            GalacticMark.Paint(c, cx, top, size);
            var seen = new HashSet<(int, int, int)>();
            for (int py = 0; py < size; py++)
            {
                int span = half - Math.Abs(py - half), x0 = cx - span, w = Math.Max(1, span * 2);
                // The row is as wide as the lozenge is there, and nothing is painted beside it
                Assert.False(c.IsOpaque(x0 - 1, top + py));
                Assert.False(c.IsOpaque(x0 + w, top + py));
                // Its ends are the rim
                Assert.Equal(GalacticMark.Ink, c.Get(x0, top + py));
                Assert.Equal(GalacticMark.Ink, c.Get(x0 + w - 1, top + py));
                for (int px = x0; px < x0 + w; px++)
                {
                    Assert.True(c.IsOpaque(px, top + py));
                    seen.Add(Rgb(c.Get(px, top + py)));
                    // The same on the left as on the right (but for the top point's one texel)
                    if (w > 1) Assert.Equal(c.Get(px, top + py), c.Get(2 * cx - 1 - px, top + py));
                }
            }
            var expected = new HashSet<(int, int, int)> { Rgb(GalacticMark.Light), Rgb(GalacticMark.Base), Rgb(GalacticMark.Shade), Rgb(GalacticMark.Ink) };
            // The spark at its heart comes in at forty-eight texels
            if (size >= GalacticMark.HeartFrom) expected.Add(Rgb(GalacticMark.Teal));
            Assert.Equal(expected, seen);
            // The upper facets are lit, the lower in shade, the side ones between; the core is ink and the seam to the top point is too
            Assert.Equal(GalacticMark.Light, c.Get(cx + half / 6, top + half / 2));
            Assert.Equal(GalacticMark.Shade, c.Get(cx + half / 6, top + half + half / 2));
            Assert.Equal(GalacticMark.Base, c.Get(cx + half * 2 / 3, top + half));
            Assert.Equal(GalacticMark.Ink, c.Get(cx + half / 5, top + half));
            Assert.Equal(GalacticMark.Ink, c.Get(cx, top + half / 3));
            Assert.Equal(size >= GalacticMark.HeartFrom ? GalacticMark.Teal : GalacticMark.Ink, c.Get(cx, top + half));
        }

        // In someone else's palette: the yellow given, a lighter and a darker of it, and their ink
        var theirs = new PixelCanvas(72, 72);
        var gold = new Color(214, 170, 72, 255);
        var black = new Color(20, 20, 28, 255);
        GalacticMark.Paint(theirs, 36, 4, 64, black, gold);
        Assert.Equal(gold, theirs.Get(36 + 21, 4 + 32));
        Assert.Equal(black, theirs.Get(36 + 6, 4 + 32));
        Assert.True(theirs.Get(36 + 8, 4 + 10).G > gold.G);
        Assert.True(theirs.Get(36 + 8, 4 + 54).G < gold.G);
    }
}
