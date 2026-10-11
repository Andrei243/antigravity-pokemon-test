using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The ninth chapter (plan 02 · S12, "Spear Pillar and the Distortion World") played through with no screen, in the
/// order a player meets it: Team Galactic on the climb through Mt. Coronet, the broken painting and Looker, Spear
/// Pillar with its grunts, the commanders and the rival, Cyrus and the three Pokémon of time, space and what is set
/// against them, the torn pillar and Cynthia, the Distortion World floor by floor with its boulders, Cyrus's last
/// battle, Giratina, and the way out at Sendoff Spring. Each scene is started as the game starts it, on maps of its own
/// (<see cref="OpeningTests.Game"/>).
/// </summary>
public class SpearPillarAndTheDistortionWorldTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string Progress = "VAR_DISTORTION_WORLD_PROGRESS";
    private static readonly string[] Guardians = { "MESPRIT", "AZELF", "UXIE" };

    private static OpeningTests.Game NewGame(int starter = 0, BattleOutcome fight = BattleOutcome.Won, PlayerLook look = PlayerLook.Boy)
    {
        var game = new OpeningTests.Game(starter, fight) { Look = look };
        game.Story.ChooseStarter(StoryState.Starters[starter]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Torterra")!, 52));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 50));
        return game;
    }

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    private static Script Find(string name, string file) => Scripts.Find(name, file) ?? throw new InvalidOperationException($"no {file}.{name}");

    /// <summary>Speaks to someone of a place from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, Direction facing)
    {
        var npc = game.Present(key) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        return game.Play(Find(FieldScripts.For(npc)!, npc.ScriptFile ?? game.Map.Name), npc);
    }

    /// <summary>A step onto a tile of a trigger, which must start its script now.</summary>
    private static HeadlessScriptHost StepOnto(OpeningTests.Game game, int x, int y, string script)
    {
        game.Tile = (x, y);
        var trigger = FieldScripts.TriggerAt(game.Map, x, y, game.Story);
        Assert.NotNull(trigger);
        Assert.Equal(script, trigger!.Script);
        return game.Play(Find(trigger.Script, trigger.ScriptFile ?? game.Map.Name));
    }

    /// <summary>The story as the button in Team Galactic's control room leaves it (GalacticHQControlRoom.txt, plan 02 · S11).</summary>
    private static void PressTheButton(OpeningTests.Game game) =>
        Run(game, "mt_coronet_2f", "setflag FLAG_FREED_GALACTIC_HQ_POKEMON\nclearflag FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F\n"
            + "setflag FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING\nclearflag FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS\nsetvar VAR_MT_CORONET_2F_STATE 1");

    /// <summary>Into the Distortion World at a point of its story, the floors' own scripts not yet played there.</summary>
    private static void SetProgress(OpeningTests.Game game, int progress) => Run(game, "distortion_world_1f", $"setvar {Progress} {progress}");

    private static int ProgressOf(OpeningTests.Game game) => game.Story.Var(Progress);

    // ------------------------------------------------------------------ before the chapter's scenes

    [Fact]
    public void ANewGameStandsTheChaptersPeopleWhereTheOriginalDoes()
    {
        var game = NewGame();
        // What a new game hides until its scene, the original's flags and ours beside them
        foreach (string flag in new[] { "FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS", "FLAG_HIDE_MT_CORONET_2F_LOOKER", "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F",
            "FLAG_HIDE_SPEAR_PILLAR_RIVAL", "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_CYNTHIA", "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_MESPRIT", "FLAG_HIDE_SPEAR_PILLAR_LEGENDS",
            "FLAG_HIDE_DISTORTION_WORLD_APPARITIONS", "FLAG_HIDE_DISTORTION_WORLD_B6F_MESPRIT_BOULDER", "FLAG_HIDE_DISTORTION_WORLD_B6F_AZELF_BOULDER",
            "FLAG_HIDE_DISTORTION_WORLD_B6F_UXIE_BOULDER", "FLAG_HIDE_GALACTIC_HQ_1F_SATURN", "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM" })
            Assert.True(game.Story.Has(flag), flag);
        // Team Galactic on the mountain, and Giratina in Turnback Cave, are there from the start, as the original's are
        foreach (string flag in new[] { "FLAG_HIDE_MT_CORONET_GALACTIC_GRUNTS", "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA", "FLAG_HIDE_SENDOFF_SPRING_CYNTHIA",
            "FLAG_HIDE_SPEAR_PILLAR_GRUNTS", "FLAG_HIDE_SPEAR_PILLAR_MARS_JUPITER", "FLAG_HIDE_SPEAR_PILLAR_CYRUS", "FLAG_SPEAR_PILLAR_IS_DISTORTED" })
            Assert.False(game.Story.Has(flag), flag);

        // Spear Pillar: the two grunts either side of the head of the steps, the commanders either side of the way to the
        // altar, Cyrus before it, and nobody else
        game.Arrive("SpearPillar", 31, 52);
        Assert.Empty(game.Last.Transcript);
        foreach (var (key, x, y) in new[] { ("grunt_f", 30, 48), ("grunt_m", 32, 48), ("jupiter", 29, 32), ("mars", 33, 32), ("cyrus", 31, 28) })
            Assert.Equal((x, y), (game.Present(key)!.GridX, game.Present(key)!.GridY));
        foreach (string key in new[] { "rival", "dialga", "palkia", "giratina" }) Assert.Null(game.Present(key));
        Assert.Equal(Direction.Up, game.Present("cyrus")!.Facing);
        Assert.Equal("Dialga", game.Map.Everyone.First(n => n.Key == "dialga").Species);
        Assert.Equal("Giratina-Origin", game.Map.Everyone.First(n => n.Key == "giratina").Species);

        // The torn pillar: Team Galactic's four facing the dark, and nobody by the tear yet
        game.Arrive("SpearPillarDistorted", 31, 50);
        Assert.Empty(game.Last.Transcript);
        foreach (var (key, x, y) in new[] { ("grunt_f", 30, 48), ("grunt_m", 32, 48), ("jupiter", 29, 33), ("mars", 33, 33) })
            Assert.Equal((x, y, Direction.Up), (game.Present(key)!.GridX, game.Present(key)!.GridY, game.Present(key)!.Facing));
        foreach (string key in new[] { "cynthia", "mesprit", "uxie", "azelf" }) Assert.Null(game.Present(key));
        // Its two rifts and the dark under them are the world's own models
        Assert.Equal(2, game.Map.Props.Count(p => p.Type == PropType.Rift));
        Assert.Single(game.Map.Props, p => p.Type == PropType.RiftShadow);

        // Mt. Coronet's way out onto the summit leads to the pillar as the story finds it
        game.Arrive("MtCoronet6F", 7, 7);
        Assert.Equal("SpearPillar", game.Map.GetWarpAt(7, 5)!.TargetMap);
        Assert.Equal(new[] { "SpearPillar", "SpearPillarDistorted" }, game.Map.WarpsAt(7, 5).Select(w => w.TargetMap).OrderBy(n => n));
    }

    // ------------------------------------------------------------------ the climb through Mt. Coronet

    [Fact]
    public void TheGruntOfTheNorthHallComesWithTheButtonAndGoesWithSendoffSpring()
    {
        var game = NewGame();
        game.Arrive("MtCoronet1FNorthRoom1", 19, 43);
        Assert.Null(game.Present("grunt_f"));

        PressTheButton(game);
        game.Arrive("MtCoronet1FNorthRoom1", 19, 43);
        var grunt = game.Present("grunt_f")!;
        Assert.Equal((19, 41), (grunt.GridX, grunt.GridY));
        var said = game.Talk("grunt_f").Transcript;
        Assert.All(said, l => Assert.Equal("Galactic Grunt", l.Speaker));
        Assert.Contains(said, l => l.Text.Contains("no way up to the peak from this side"));

        Run(game, "sendoff_spring", "setvar VAR_EXITED_DISTORTION_WORLD_STATE 1");
        game.Arrive("SendoffSpring", 32, 17);
        game.Arrive("MtCoronet1FNorthRoom1", 19, 43);
        Assert.Null(game.Present("grunt_f"));
    }

    [Fact]
    public void TheCavePaintingShutsTheHoleUntilTeamGalacticBreaksIt()
    {
        var game = NewGame();
        game.Arrive("MtCoronet2F", 7, 25);
        // The painting stands over the hole: rock, read as the painting; no shards, no Looker, his trigger dead
        Assert.True(game.Map.IsSolid(7, 23));
        Assert.Null(game.Map.GetWarpAt(7, 23));
        var painting = game.Present("cave_painting") ?? throw new InvalidOperationException("The painting isn't on its tile");
        Assert.Equal((7, 23), (painting.GridX, painting.GridY));
        Assert.Equal(PropType.CavePainting, painting.GymThing);
        Assert.Equal("CavePainting", painting.Script);
        var read = game.Play(Find("CavePainting", "mt_coronet_2f"));
        Assert.Contains(read.Transcript, l => l.Speaker == null && l.Text.Contains("very old"));
        Assert.Null(game.Present("looker"));
        Assert.DoesNotContain(game.Map.NPCs, n => n.NpcType == NPC.RubbleType);
        Assert.Null(FieldScripts.TriggerAt(game.Map, 11, 25, game.Story));
        // The way down from the floor's upper half lands in front of it all the same
        Assert.Equal(("MtCoronet2F", 7, 24), (game.Map.GetWarpAt(7, 12)!.TargetMap, game.Map.GetWarpAt(7, 12)!.TargetX, game.Map.GetWarpAt(7, 12)!.TargetY));

        PressTheButton(game);
        game.Arrive("MtCoronet2F", 7, 25);
        // The hole is open and leads to the upper half; the shards lie either side of it and are read; the painting is not
        Assert.False(game.Map.IsSolid(7, 23));
        var hole = game.Map.GetWarpAt(7, 23)!;
        Assert.Equal("MtCoronet2F", hole.TargetMap);
        Assert.True(hole.TargetY < 14, $"the hole leads to {hole.TargetX},{hole.TargetY}");
        Assert.False(game.Map.IsSolid(hole.TargetX, hole.TargetY));
        var shards = game.Map.NPCs.Where(n => n.NpcType == NPC.RubbleType).OrderBy(n => n.GridX).ToList();
        Assert.Equal(new[] { (6, 24), (8, 24) }, shards.Select(n => (n.GridX, n.GridY)));
        Assert.All(shards, n => Assert.True(n.IsThing && n.GymThing == PropType.Rubble));
        Assert.Contains(game.Play(Find(FieldScripts.For(shards[0])!, "mt_coronet_2f"), shards[0]).Transcript, l => l.Text.Contains("all that is left of the cave painting"));
        Assert.Null(game.Present("cave_painting"));
        Assert.Empty(game.Play(Find("CavePainting", "mt_coronet_2f")).Transcript);
        Assert.NotNull(FieldScripts.TriggerAt(game.Map, 11, 25, game.Story));
    }

    [Theory]
    [InlineData(24)]
    [InlineData(25)]
    [InlineData(26)]
    public void LookerCatchesThePlayerUpBeforeTheHoleWithTheBlackFlute(int y)
    {
        var game = NewGame();
        PressTheButton(game);
        game.Arrive("MtCoronet2F", 12, y);
        game.Facing = Direction.Left;
        var scene = StepOnto(game, 11, y, "Looker");
        Assert.Equal(2, game.Story.Var("VAR_MT_CORONET_2F_STATE"));
        Assert.False(game.Story.Has("FLAG_HIDE_MT_CORONET_2F_LOOKER"));
        Assert.True(game.Story.Has("FLAG_RECEIVED_MT_CORONET_2F_BLACK_FLUTE"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Black Flute")!));
        // He ran up the passage to stand beside the player, on their row
        var looker = game.Present("looker")!;
        Assert.Equal((12, y, Direction.Left), (looker.GridX, looker.GridY, looker.Facing));
        Assert.Equal((11, y), game.Tile);
        Assert.All(scene.Transcript.Where(l => l.Speaker != null), l => Assert.Equal("Looker", l.Speaker));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Observe that hole"));
        Assert.Contains("emote Looker Exclaim", scene.Log);
        // Once only; spoken to afterwards he gives nothing more
        Assert.Null(FieldScripts.TriggerAt(game.Map, 11, y, game.Story));
        Assert.Contains(TalkFrom(game, "looker", Direction.Right).Transcript, l => l.Text.Contains("stop Team Galactic"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Black Flute")!));

        // He waits there, where the original stands him, until the player is out of the Distortion World
        game.Arrive("MtCoronet2F", 12, y);
        Assert.Equal((10, 26), (game.Present("looker")!.GridX, game.Present("looker")!.GridY));
    }

    [Fact]
    public void TheClimbsGruntsArePlatinumsAndAreGoneOnceThePlayerIsOutOfTheDistortionWorld()
    {
        var game = NewGame();
        var posts = new (string Map, string Key, string Trainer)[]
        {
            ("MtCoronet1FTunnelRoom", "galactic_grunt_mt_coronet_tunnel_room_1", "galactic_grunt_mt_coronet_tunnel_room_1"),
            ("MtCoronet1FTunnelRoom", "galactic_grunt_mt_coronet_tunnel_room_2", "galactic_grunt_mt_coronet_tunnel_room_2"),
            ("MtCoronet1FTunnelRoom", "galactic_grunt_mt_coronet_tunnel_room_3", "galactic_grunt_mt_coronet_tunnel_room_3"),
            ("MtCoronet3F", "galactic_grunt_1", "galactic_grunt_mt_coronet_3f_1"),
            ("MtCoronet3F", "galactic_grunt_2", "galactic_grunt_mt_coronet_3f_2"),
            ("MtCoronet4FRooms1And2", "_galactic_grunt_1", "galactic_grunt_mt_coronet_4f_1"),
            ("MtCoronet4FRooms1And2", "_galactic_grunt_2", "galactic_grunt_mt_coronet_4f_2"),
            ("MtCoronet5F", "galactic_grunt_1", "galactic_grunt_mt_coronet_5f_1"),
            ("MtCoronet5F", "galactic_grunt_2", "galactic_grunt_mt_coronet_5f_2"),
            ("MtCoronet6F", "galactic_grunt", "galactic_grunt_mt_coronet_6f")
        };
        foreach (var (map, key, trainer) in posts)
        {
            var grunt = game.Maps[map].NPCs.Single(n => n.Key == key);
            Assert.True(grunt.IsTrainer, $"{map}: {key} doesn't battle");
            var record = TrainerDatabase.Get(trainer)!;
            Assert.Equal(trainer, grunt.TrainerData!.Id);
            Assert.Equal(record.Party.Select(p => (p.Species, p.Level)), grunt.TrainerData.Party.Members.Select(p => (p.Species.Name, p.Level)));
            Assert.Equal("FLAG_HIDE_MT_CORONET_GALACTIC_GRUNTS", grunt.HiddenBy);
            Assert.StartsWith("Galactic Grunt: ", grunt.TrainerData.DialogueBefore);
        }
        // And the one who has lost his way, who only talks
        game.Arrive("MtCoronet1FTunnelRoom", 16, 16);
        var lost = game.Present("grunt_m")!;
        Assert.False(lost.IsTrainer);
        Assert.Contains(game.Talk("grunt_m").Transcript, l => l.Text.Contains("hopelessly"));
        Assert.Equal(Direction.Up, lost.Facing);

        Run(game, "sendoff_spring", "setvar VAR_EXITED_DISTORTION_WORLD_STATE 1");
        game.Arrive("SendoffSpring", 32, 17);
        foreach (var (map, key, _) in posts) Assert.DoesNotContain(game.Maps[map].NPCs, n => n.Key == key);
        Assert.DoesNotContain(game.Maps["MtCoronet1FTunnelRoom"].NPCs, n => n.Key == "grunt_m");
    }

    // ------------------------------------------------------------------ Spear Pillar

    [Fact]
    public void TheGruntsAtTheHeadOfTheStepsBattleTogetherAndWantTwoPokemon()
    {
        // With one Pokémon they turn the player back down the steps, and the trigger waits
        var alone = new OpeningTests.Game(0);
        alone.Story.ChooseStarter(StoryState.Starters[0]);
        alone.Party.Add(new Pokemon(PokemonDatabase.Get("Torterra")!, 52));
        alone.Arrive("SpearPillar", 31, 49);
        var turned = StepOnto(alone, 31, 48, "GruntsStop");
        Assert.Contains(turned.Transcript, l => l.Text.Contains("One Pokémon?"));
        Assert.DoesNotContain(turned.Log, l => l.StartsWith("battle "));
        Assert.Equal((31, 49), alone.Tile);
        Assert.Equal(0, alone.Story.Var("VAR_SPEAR_PILLAR_STATE"));
        Assert.Equal((Direction.Down, Direction.Down), (alone.Present("grunt_f")!.Facing, alone.Present("grunt_m")!.Facing));

        // With two, both at once, each with Platinum's team
        var game = NewGame();
        game.Arrive("SpearPillar", 31, 49);
        var fought = StepOnto(game, 31, 48, "GruntsStop");
        Assert.Contains("battle galactic_grunt_spear_pillar_1 and galactic_grunt_spear_pillar_2 Won", fought.Log);
        Assert.Equal(1, game.Story.Var("VAR_SPEAR_PILLAR_STATE"));
        Assert.True(game.Story.HasDefeated("galactic_grunt_spear_pillar_1") && game.Story.HasDefeated("galactic_grunt_spear_pillar_2"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 31, 48, game.Story));
        // They stand where they stood, and only say they let the player by
        Assert.Contains(TalkFrom(game, "grunt_f", Direction.Left).Transcript, l => l.Text.Contains("let you past"));
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("battle "));

        // Lost, the scene starts again from its trigger
        var beaten = NewGame(fight: BattleOutcome.Lost);
        beaten.Arrive("SpearPillar", 31, 49);
        StepOnto(beaten, 31, 48, "GruntsStop");
        Assert.Equal(0, beaten.Story.Var("VAR_SPEAR_PILLAR_STATE"));
        Assert.NotNull(FieldScripts.TriggerAt(beaten.Map, 31, 48, beaten.Story));
    }

    [Theory]
    [InlineData(30, 0)]
    [InlineData(31, 1)]
    [InlineData(32, 2)]
    public void TheCommandersCloseInTheRivalFightsBesideThePlayerAndCyrusIsTaken(int x, int starter)
    {
        var game = NewGame(starter);
        Run(game, "spear_pillar", "setvar VAR_SPEAR_PILLAR_STATE 1");
        game.Arrive("SpearPillar", x, 33);
        game.Facing = Direction.Up;
        var scene = StepOnto(game, x, 32, "MarsAndJupiter");

        // The tag battle: both commanders, the rival's team by the player's starter
        string partner = "rival_spear_pillar_" + StoryState.Starters[starter].ToLowerInvariant();
        Assert.Contains($"battle commander_mars_spear_pillar and commander_jupiter_spear_pillar with {partner} Won", scene.Log);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Mars" && l.Text.Contains("through me first"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Jupiter" && l.Text.Contains("take turns"));
        Assert.Contains(scene.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("start without me"));
        // The rival heals the team and leaves the rest to the player
        Assert.Contains("fanfare FanfareHeal", scene.Log);
        Assert.Contains(scene.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("your show"));
        // Cyrus calls the two, and they go into the Pokédex as seen; the shadow comes for him
        Assert.Equal(new[] { "Dialga", "Palkia" }, scene.SeenSpecies);
        Assert.Contains("cry Dialga", scene.Log);
        Assert.Contains("cry Palkia", scene.Log);
        Assert.Contains("cry Giratina-Origin", scene.Log);
        Assert.Contains("camera Pan 31 26", scene.Log);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Cyrus" && l.Text.Contains("Red Chain"));
        Assert.Contains(scene.Transcript, l => l.Speaker == null && l.Text.Contains("sank into the floor"));

        // What the original sets as the summit is torn
        Assert.Equal(3, game.Story.Var("VAR_SPEAR_PILLAR_STATE"));
        foreach (string flag in new[] { "FLAG_HIDE_SPEAR_PILLAR_GRUNTS", "FLAG_HIDE_SPEAR_PILLAR_MARS_JUPITER", "FLAG_HIDE_SPEAR_PILLAR_CYRUS", "FLAG_UNLOCKED_VS_SEEKER_LVL_3",
            "FLAG_SPEAR_PILLAR_IS_DISTORTED", "FLAG_HIDE_SPEAR_PILLAR_RIVAL", "FLAG_HIDE_SPEAR_PILLAR_LEGENDS" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.False(game.Story.Has("FLAG_HIDE_SPEAR_PILLAR_DISTORTED_TEAM_GALACTIC"));
        Assert.Contains("warp SpearPillarDistorted 30 30", scene.Log);
        Assert.DoesNotContain(game.Maps["SpearPillar"].NPCs, n => n.ScriptFile == "spear_pillar");

        // The scene on the torn pillar follows at once, and with a yes the way in, and the first floor's own scene
        Assert.Equal(new[] { "spear_pillar.MarsAndJupiter", "spear_pillar_distorted.OnEnter", "distortion_world_1f.OnEnter" }, game.Played.TakeLast(3).Select(p => p.Script));
        Assert.Equal("DistortionWorld1F", game.Map.Name);
        Assert.Equal(1, ProgressOf(game));
    }

    [Fact]
    public void ALostBattleWithTheCommandersLeavesTheSceneToStartAgain()
    {
        var game = NewGame(fight: BattleOutcome.Lost);
        Run(game, "spear_pillar", "setvar VAR_SPEAR_PILLAR_STATE 1");
        game.Arrive("SpearPillar", 31, 33);
        var scene = StepOnto(game, 31, 32, "MarsAndJupiter");
        Assert.Contains(scene.Log, l => l.StartsWith("battle commander_mars_spear_pillar and commander_jupiter_spear_pillar") && l.EndsWith("Lost"));
        Assert.Equal(1, game.Story.Var("VAR_SPEAR_PILLAR_STATE"));
        Assert.False(game.Story.Has("FLAG_SPEAR_PILLAR_IS_DISTORTED"));
        Assert.DoesNotContain(scene.Log, l => l.StartsWith("warp "));
        // Mars had stepped in: coming up again finds both where they stand, the rival out of sight, the trigger waiting
        Assert.Equal((32, 32), (game.Maps["SpearPillar"].Everyone.First(n => n.Key == "mars").GridX, game.Maps["SpearPillar"].Everyone.First(n => n.Key == "mars").GridY));
        game.Arrive("SpearPillar", 31, 49);
        Assert.Equal((33, 32), (game.Present("mars")!.GridX, game.Present("mars")!.GridY));
        Assert.Equal((29, 32), (game.Present("jupiter")!.GridX, game.Present("jupiter")!.GridY));
        Assert.Null(game.Present("rival"));
        Assert.NotNull(FieldScripts.TriggerAt(game.Map, 31, 32, game.Story));
    }

    // ------------------------------------------------------------------ the torn pillar

    [Fact]
    public void CynthiaComesToTheTornPillarAndAsksWhetherThePlayerIsReady()
    {
        var game = NewGame();
        Run(game, "spear_pillar_distorted", "setvar VAR_SPEAR_PILLAR_DISTORTED_STATE 1\nsetflag FLAG_SPEAR_PILLAR_IS_DISTORTED");
        // A no leaves her waiting by the tear
        game.Answers.Enqueue(1);
        game.Arrive("SpearPillarDistorted", 30, 30);
        var scene = game.Last;
        Assert.Equal(2, game.Story.Var("VAR_SPEAR_PILLAR_DISTORTED_STATE"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("too late"));
        foreach (string who in new[] { "Mesprit", "Uxie", "Azelf" }) Assert.Contains($"cry {who}", scene.Log);
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Giratina"));
        Assert.Contains(scene.Asked, a => a.Question.Contains("Are you ready?") && a.Answer == "No");
        Assert.DoesNotContain(scene.Log, l => l.StartsWith("warp "));
        Assert.Equal("SpearPillarDistorted", game.Map.Name);
        // Charon has left the control room, the three are gone again, and she stands beside the player
        Assert.True(game.Story.Has("FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_CHARON"));
        Assert.True(game.Story.Has("FLAG_HIDE_SPEAR_PILLAR_DISTORTED_MESPRIT"));
        Assert.False(game.Story.Has("FLAG_HIDE_SPEAR_PILLAR_DISTORTED_CYNTHIA"));
        var cynthia = game.Present("cynthia")!;
        Assert.Equal((32, 30), (cynthia.GridX, cynthia.GridY));
        foreach (string key in new[] { "mesprit", "uxie", "azelf" }) Assert.Null(game.Present(key));

        // It does not play again; she asks again when spoken to, and a yes leads in beside the portal
        game.Arrive("SpearPillarDistorted", 31, 33);
        Assert.Empty(game.Last.Transcript);
        game.Answers.Enqueue(1);
        Assert.Contains(game.Talk("cynthia").Asked, a => a.Answer == "No");
        var into = game.Talk("cynthia");
        Assert.Contains("warp DistortionWorld1F 34 30", into.Log);
        Assert.Equal(("DistortionWorld1F", (33, 30)), (game.Map.Name, game.Tile));

        // Team Galactic's four have nothing but their shock, and the rifts are read
        game.Arrive("SpearPillarDistorted", 31, 36);
        Assert.Contains(game.Talk("mars").Transcript, l => l.Speaker == "Mars" && l.Text.Contains("Where did he go"));
        Assert.Contains(game.Talk("jupiter").Transcript, l => l.Speaker == "Jupiter");
        Assert.Contains(TalkFrom(game, "grunt_m", Direction.Up).Transcript, l => l.Speaker == "Galactic Grunt");
        Assert.Equal("RiftDialga", game.Map.TileScripts[(29, 17)]);
        Assert.Equal("RiftPalkia", game.Map.TileScripts[(33, 17)]);
        Assert.Contains(game.Play(Find("RiftDialga", "spear_pillar_distorted")).Transcript, l => l.Text.Contains("A rift hangs"));
    }

    [Fact]
    public void MtCoronetsSummitIsTheTornPillarFromCyrussTakingUntilSendoffSpring()
    {
        var game = NewGame();
        game.Arrive("MtCoronet6F", 7, 7);
        Assert.Equal("SpearPillar", game.Map.GetWarpAt(7, 5)!.TargetMap);
        Run(game, "mt_coronet_6f", "setflag FLAG_SPEAR_PILLAR_IS_DISTORTED");
        Assert.Equal("SpearPillarDistorted", game.Map.GetWarpAt(7, 5)!.TargetMap);
        // And the torn pillar's own way down comes back out on the sixth floor
        Assert.Equal("MtCoronet6F", game.Maps["SpearPillarDistorted"].GetWarpAt(31, 53)!.TargetMap);
        Run(game, "mt_coronet_6f", "clearflag FLAG_SPEAR_PILLAR_IS_DISTORTED");
        Assert.Equal("SpearPillar", game.Map.GetWarpAt(7, 5)!.TargetMap);
    }

    // ------------------------------------------------------------------ the Distortion World, 1F to B4F

    [Fact]
    public void TheFirstFloorBringsCynthiaThroughThePortalAndStandsHerOnTheSlab()
    {
        var game = NewGame();
        game.Arrive("DistortionWorld1F", 34, 30);
        var scene = game.Last;
        Assert.Equal(1, ProgressOf(game));
        Assert.Equal((33, 30), game.Tile);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("distortion world"));
        Assert.Contains("cry Giratina-Origin", scene.Log);
        Assert.Contains(scene.Transcript, l => l.Text.Contains("That was Giratina"));
        // She has gone ahead to the slab that leads down
        var cynthia = game.Present("cynthia")!;
        Assert.Equal((18, 42, Direction.Right), (cynthia.GridX, cynthia.GridY, cynthia.Facing));
        // The scene plays once
        game.Arrive("DistortionWorld1F", 34, 30);
        Assert.Empty(game.Last.Transcript);

        // The step onto the slab's stone beside her
        var warned = StepOnto(game, 19, 42, "SlabMoves");
        Assert.Equal(2, ProgressOf(game));
        Assert.Contains(warned.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("it moves"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 19, 42, game.Story));
        Assert.Contains(TalkFrom(game, "cynthia", Direction.Left).Transcript, l => l.Text.Contains("different colour"));
        Assert.Equal(("DistortionWorldB1F", 40, 19), (game.Map.GetWarpAt(19, 44)!.TargetMap, game.Map.GetWarpAt(19, 44)!.TargetX, game.Map.GetWarpAt(19, 44)!.TargetY));

        // The portal behind leads back to the torn pillar, or not
        var portal = game.Present("portal")!;
        Assert.Equal((34, 29, "Rift"), (portal.GridX, portal.GridY, portal.NpcType));
        game.Answers.Enqueue(1);
        Assert.DoesNotContain(game.Talk("portal").Log, l => l.StartsWith("warp "));
        Assert.Contains("warp SpearPillarDistorted 31 33", game.Talk("portal").Log);
        Assert.Equal("SpearPillarDistorted", game.Map.Name);

        // Once the player has been down, she is no longer on the first floor
        SetProgress(game, 3);
        game.Arrive("DistortionWorld1F", 34, 30);
        Assert.Null(game.Present("cynthia"));
    }

    [Fact]
    public void CynthiaGoesAheadOnB1FAndMespritShowsTheWayUpTheWall()
    {
        var game = NewGame();
        SetProgress(game, 2);
        game.Arrive("DistortionWorldB1F", 40, 19);
        Assert.Equal(3, ProgressOf(game));
        Assert.Contains(game.Last.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("no sign of it"));
        Assert.Null(game.Present("cynthia"));
        Assert.Null(game.Present("mesprit"));

        var cry = StepOnto(game, 15, 23, "Mesprit");
        Assert.Equal(4, ProgressOf(game));
        Assert.Contains("cry Mesprit", cry.Log);
        Assert.Null(game.Present("mesprit"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 15, 23, game.Story));
        // The wall it went up is the way on
        Assert.Equal((12, 14), (game.Map.GetWarpAt(12, 23)!.TargetX, game.Map.GetWarpAt(12, 23)!.TargetY));

        // Coming down again finds nobody at the slab's foot
        game.Arrive("DistortionWorldB1F", 40, 19);
        Assert.Empty(game.Last.Transcript);
        Assert.Null(game.Present("cynthia"));
    }

    [Fact]
    public void CynthiaProposesToSearchApartOnB2FAndCyrusSpeaksOfTwoStrandsOnB3F()
    {
        var game = NewGame();
        SetProgress(game, 3);
        game.Arrive("DistortionWorldB2F", 18, 25);
        Assert.Null(game.Present("cynthia"));

        SetProgress(game, 4);
        game.Arrive("DistortionWorldB2F", 18, 25);
        var cynthia = game.Present("cynthia")!;
        Assert.Equal((20, 22), (cynthia.GridX, cynthia.GridY));
        Assert.Contains(TalkFrom(game, "cynthia", Direction.Right).Transcript, l => l.Text.Contains("more of it apart"));
        Assert.Equal(5, ProgressOf(game));
        Assert.Contains(TalkFrom(game, "cynthia", Direction.Right).Transcript, l => l.Text.Contains("needn't keep together"));
        Assert.Equal(5, ProgressOf(game));

        // Cyrus, whether or not she was spoken to, and whatever the player answers
        foreach (var (progress, answer, said) in new[] { (4, 0, "paid more attention"), (5, 1, "Of course you don't") })
        {
            var other = NewGame();
            SetProgress(other, progress);
            other.Arrive("DistortionWorldB3F", 18, 17);
            Assert.Null(other.Present("cyrus"));
            other.Answers.Enqueue(answer);
            other.Tile = (18, 20);
            var trigger = FieldScripts.TriggerAt(other.Map, 18, 20, other.Story)!;
            var scene = other.Play(Find(trigger.Script, "distortion_world_b3f"));
            Assert.Equal(6, ProgressOf(other));
            Assert.Contains(scene.Asked, a => a.Question.Contains("genes"));
            Assert.Contains(scene.Transcript, l => l.Speaker == "Cyrus" && l.Text.Contains(said));
            Assert.Contains(scene.Transcript, l => l.Speaker == "Cyrus" && l.Text.Contains("two strands"));
            Assert.Null(other.Present("cyrus"));
            Assert.Null(FieldScripts.TriggerAt(other.Map, 18, 20, other.Story));
        }

        // With Cyrus met she has gone on from B2F
        SetProgress(game, 6);
        game.Arrive("DistortionWorldB2F", 18, 25);
        Assert.Null(game.Present("cynthia"));
    }

    [Fact]
    public void GiratinasShadowCrossesB4FOnceEachSide()
    {
        var game = NewGame();
        SetProgress(game, 6);
        game.Arrive("DistortionWorldB4F", 22, 29);
        var west = StepOnto(game, 8, 23, "ShadowWest");
        Assert.Contains("cry Giratina-Origin", west.Log);
        Assert.True(game.Story.Has("FLAG_DISTORTION_WORLD_GIRATINA_SHADOW_1"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 8, 23, game.Story));
        foreach (int y in new[] { 22, 23 }) Assert.Equal("ShadowEast", FieldScripts.TriggerAt(game.Map, 41, y, game.Story)!.Script);
        StepOnto(game, 41, 23, "ShadowEast");
        Assert.True(game.Story.Has("FLAG_DISTORTION_WORLD_GIRATINA_SHADOW_2"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 41, 22, game.Story));
    }

    // ------------------------------------------------------------------ the boulders of B5F and B6F

    [Fact]
    public void ABoulderIsPushedOntoAPitWhateverStandsThereAndThePitsScriptIsItsOwn()
    {
        var game = NewGame();
        var b5 = game.Maps["DistortionWorldB5F"];
        b5.ApplyPresence(game.Story.Has);
        // The three boulders stand where the original's do, each a step from its hole, which is the island's rim
        foreach (var (key, x, y, way, pit) in new[] { ("mesprit_boulder", 18, 34, Direction.Right, (19, 34)), ("azelf_boulder", 41, 33, Direction.Left, (40, 33)), ("uxie_boulder", 29, 22, Direction.Down, (29, 23)) })
        {
            var boulder = b5.NPCs.Single(n => n.Key == key);
            Assert.Equal((x, y, PropType.StrengthBoulder), (boulder.GridX, boulder.GridY, boulder.Obstacle));
            Assert.True(b5.IsSolid(pit.Item1, pit.Item2), $"{key}'s hole is open ground");
            Assert.True(b5.BoulderPits.ContainsKey(pit));
            // Nobody walks onto the hole, and a boulder is pushed onto it; any other way it goes only over open ground
            var (dx, dy) = FieldMovement.Delta(way);
            Assert.False(FieldMovement.Step(b5, x - dx, y - dy, way, new Walker(TravelMode.OnFoot, b5.HeightAt(x - dx, y - dy))).Moves);
            Assert.True(FieldMovement.CanPush(b5, boulder, way));
            Assert.Null(FieldScripts.PitUnder(b5, boulder));
            (boulder.GridX, boulder.GridY) = pit;
            Assert.Equal(b5.BoulderPits[pit], FieldScripts.PitUnder(b5, boulder));
            (boulder.GridX, boulder.GridY) = (x, y);
        }
        // A rim that is no pit refuses a boulder as it always did, and so does a pit with someone standing on it
        var uxies = b5.NPCs.Single(n => n.Key == "uxie_boulder");
        Assert.False(FieldMovement.CanPush(b5, uxies, Direction.Up) && b5.IsSolid(29, 21));
        var azelfs = b5.NPCs.Single(n => n.Key == "azelf_boulder");
        (azelfs.GridX, azelfs.GridY) = (29, 23);
        Assert.False(FieldMovement.CanPush(b5, uxies, Direction.Down));
        b5.ResetObstacles();
        Assert.Equal((41, 33), (azelfs.GridX, azelfs.GridY));
        // Someone who is no boulder falls into nothing
        Assert.Null(FieldScripts.PitUnder(b5, new NPC { NpcType = "Cynthia", GridX = 19, GridY = 34 }));
    }

    [Fact]
    public void TheGuardiansShowWhatTheirBouldersAreForOnce()
    {
        var game = NewGame();
        SetProgress(game, 6);
        game.Arrive("DistortionWorldB5F", 30, 34);
        foreach (string key in new[] { "mesprit", "azelf", "uxie" }) Assert.Null(game.Present(key));
        foreach (var (x, y, script, who) in new[] { (29, 19, "UxieShows", "Uxie"), (30, 19, "UxieShows", "Uxie"), (43, 33, "AzelfShows", "Azelf"), (43, 34, "AzelfShows", "Azelf"),
            (23, 33, "MespritShows", "Mesprit"), (23, 34, "MespritShows", "Mesprit") })
            Assert.Equal(script, FieldScripts.TriggerAt(game.Map, x, y, game.Story)!.Script);
        var shown = StepOnto(game, 30, 19, "UxieShows");
        Assert.Contains("cry Uxie", shown.Log);
        Assert.Contains(shown.Transcript, l => l.Text.Contains("toward the boulder"));
        Assert.Null(game.Present("uxie"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 29, 19, game.Story));
        Assert.Contains("cry Azelf", StepOnto(game, 43, 34, "AzelfShows").Log);
        Assert.Contains("cry Mesprit", StepOnto(game, 23, 33, "MespritShows").Log);
    }

    /// <summary>Pushes a wing's boulder into its hole on B5F, as the field does when the boulder comes to rest there.</summary>
    private static HeadlessScriptHost DropFromB5F(OpeningTests.Game game, string who)
    {
        game.Arrive("DistortionWorldB5F", 30, 34);
        // As the game does on coming to a place: every boulder back where it first stood
        game.Map.ResetObstacles();
        var boulder = game.Present($"{who}_boulder") ?? throw new InvalidOperationException($"{who}'s boulder isn't on B5F");
        var (way, pit) = who switch { "mesprit" => (Direction.Right, (19, 34)), "azelf" => (Direction.Left, (40, 33)), _ => (Direction.Down, (29, 23)) };
        Assert.True(FieldMovement.CanPush(game.Map, boulder, way));
        (boulder.GridX, boulder.GridY) = pit;
        return game.Play(Find(FieldScripts.PitUnder(game.Map, boulder)!, "distortion_world_b5f"), boulder);
    }

    /// <summary>Pushes a wing's boulder along B6F, a tile at a time by the field's own rule, and into a pit.</summary>
    private static HeadlessScriptHost PushOnB6F(OpeningTests.Game game, string who, params (Direction Way, int Times)[] pushes)
    {
        game.Arrive("DistortionWorldB6F", 31, 30);
        game.Map.ResetObstacles();
        var boulder = game.Present($"{who}_boulder") ?? throw new InvalidOperationException($"{who}'s boulder isn't on B6F");
        foreach (var (way, times) in pushes)
            for (int i = 0; i < times; i++)
            {
                var (dx, dy) = FieldMovement.Delta(way);
                // The player stands behind it, on open ground
                Assert.False(game.Map.IsSolid(boulder.GridX - dx, boulder.GridY - dy), $"nowhere to push {who}'s boulder {way} from at {boulder.GridX},{boulder.GridY}");
                Assert.True(FieldMovement.CanPush(game.Map, boulder, way), $"{who}'s boulder can't be pushed {way} from {boulder.GridX},{boulder.GridY}");
                boulder.GridX += dx;
                boulder.GridY += dy;
            }
        string pit = FieldScripts.PitUnder(game.Map, boulder) ?? throw new InvalidOperationException($"{who}'s boulder came to rest at {boulder.GridX},{boulder.GridY}, which is no pit");
        return game.Play(Find(pit, "distortion_world_b6f"), boulder);
    }

    private static readonly Dictionary<string, (Direction Way, int Times)[]> RightPit = new()
    {
        ["mesprit"] = new[] { (Direction.Left, 7), (Direction.Up, 1) },
        ["azelf"] = new[] { (Direction.Left, 2), (Direction.Down, 1) },
        ["uxie"] = new[] { (Direction.Down, 4), (Direction.Left, 1) }
    };

    [Fact]
    public void TheThreeBouldersFallToB6FAndComeToRestInTheirGuardiansPits()
    {
        var game = NewGame();
        SetProgress(game, 6);
        game.Arrive("DistortionWorldB6F", 31, 30);
        // The guardians hang beyond their pits, no boulder has come down, and Cynthia stands in the one passage south
        foreach (var (key, x, y) in new[] { ("mesprit", 12, 26), ("uxie", 26, 22), ("azelf", 40, 33) })
            Assert.Equal((x, y, 1f), (game.Present(key)!.GridX, game.Present(key)!.GridY, game.Present(key)!.Level));
        Assert.DoesNotContain(game.Map.NPCs, n => n.Obstacle == PropType.StrengthBoulder);
        Assert.Equal((29, 42), (game.Present("cynthia")!.GridX, game.Present("cynthia")!.GridY));
        Assert.Contains(game.Talk("cynthia").Transcript, l => l.Text.Contains("one great puzzle"));
        Assert.Equal(6, ProgressOf(game));
        Assert.Contains(game.Play(Find(game.Map.TileScripts[(12, 29)], "distortion_world_b6f")).Transcript, l => l.Text.Contains("A boulder might fit"));

        foreach (string who in new[] { "mesprit", "azelf", "uxie" })
        {
            string flag = who.ToUpperInvariant();
            var fell = DropFromB5F(game, who);
            Assert.Contains(fell.Transcript, l => l.Text.Contains("dropped through the hole"));
            Assert.True(game.Story.Has($"FLAG_DISTORTION_WORLD_{flag}_BOULDER_FELL"));
            Assert.False(game.Story.Has($"FLAG_HIDE_DISTORTION_WORLD_B6F_{flag}_BOULDER"));
            Assert.Null(game.Present($"{who}_boulder"));

            var rest = PushOnB6F(game, who, RightPit[who]);
            Assert.Contains($"cry {char.ToUpperInvariant(who[0])}{who[1..]}", rest.Log);
            Assert.True(game.Story.Has($"FLAG_DISTORTION_WORLD_{flag}_BOULDER_IN_PIT"));
            Assert.True(game.Story.Has($"FLAG_HIDE_DISTORTION_WORLD_B6F_{flag}_BOULDER"));
            // It rests in the pit, the guardian has gone home, and nothing of it is left on B5F
            game.Arrive("DistortionWorldB6F", 31, 30);
            Assert.Null(game.Present(who));
            Assert.Null(game.Present($"{who}_boulder"));
            var atRest = game.Present($"{who}_boulder_in_pit")!;
            Assert.True(game.Map.BoulderPits.ContainsKey((atRest.GridX, atRest.GridY)));
            Assert.Contains(game.Play(Find(FieldScripts.For(atRest)!, "distortion_world_b6f"), atRest).Transcript, l => l.Text.Contains("won't be moved again"));
            // From wherever a player can stand beside it, it goes nowhere
            foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var (dx, dy) = FieldMovement.Delta(way);
                if (!game.Map.IsSolid(atRest.GridX - dx, atRest.GridY - dy)) Assert.False(FieldMovement.CanPush(game.Map, atRest, way), $"{who}'s boulder is pushed {way} out of its pit");
            }
            Assert.DoesNotContain(game.Maps["DistortionWorldB5F"].NPCs, n => n.Key == $"{who}_boulder");
            Assert.Equal(who == "uxie", game.Story.Has("FLAG_DISTORTION_WORLD_PUZZLE_FINISHED"));
        }

        // Cynthia, with all three at rest: the stepping stones, and she crosses to the slab
        game.Arrive("DistortionWorldB6F", 31, 30);
        var done = game.Talk("cynthia");
        Assert.Contains(done.Transcript, l => l.Text.Contains("gone home to their lakes"));
        Assert.True(game.Story.Has("FLAG_DISTORTION_WORLD_STEPPING_STONES"));
        Assert.Equal(7, ProgressOf(game));
        Assert.Equal((28, 46, Direction.Right), (game.Present("cynthia")!.GridX, game.Present("cynthia")!.GridY, game.Present("cynthia")!.Facing));
        game.Arrive("DistortionWorldB6F", 31, 30);
        Assert.Equal((28, 46), (game.Present("cynthia")!.GridX, game.Present("cynthia")!.GridY));
        Assert.Contains(TalkFrom(game, "cynthia", Direction.Left).Transcript, l => l.Text.Contains("getting close"));
        Assert.Equal(("DistortionWorldB7F", 11, 54), (game.Map.GetWarpAt(29, 48)!.TargetMap, game.Map.GetWarpAt(29, 48)!.TargetX, game.Map.GetWarpAt(29, 48)!.TargetY));
    }

    [Theory]
    [InlineData("mesprit", Direction.Up, 1, Direction.Up, 0)]
    [InlineData("mesprit", Direction.Right, 4, Direction.Up, 1)]
    [InlineData("azelf", Direction.Right, 2, Direction.Down, 1)]
    [InlineData("azelf", Direction.Right, 7, Direction.Down, 1)]
    [InlineData("uxie", Direction.Left, 1, Direction.Left, 0)]
    [InlineData("uxie", Direction.Down, 1, Direction.Left, 1)]
    [InlineData("uxie", Direction.Up, 4, Direction.Left, 1)]
    [InlineData("uxie", Direction.Up, 3, Direction.Left, 1)]
    public void ABoulderPushedIntoAnyOtherPitGoesBackToWhereItFirstStood(string who, Direction first, int times, Direction then, int more)
    {
        var game = NewGame();
        SetProgress(game, 6);
        DropFromB5F(game, who);
        var gone = PushOnB6F(game, who, (first, times), (then, more));
        string flag = who.ToUpperInvariant();
        Assert.Contains(gone.Transcript, l => l.Text.Contains("out of sight"));
        Assert.False(game.Story.Has($"FLAG_DISTORTION_WORLD_{flag}_BOULDER_FELL"));
        Assert.True(game.Story.Has($"FLAG_HIDE_DISTORTION_WORLD_B6F_{flag}_BOULDER"));
        Assert.False(game.Story.Has($"FLAG_DISTORTION_WORLD_{flag}_BOULDER_IN_PIT"));
        // The guardian waits still, nothing is left on this floor, and the boulder is on B5F where it began
        game.Arrive("DistortionWorldB6F", 31, 30);
        Assert.NotNull(game.Present(who));
        Assert.Null(game.Present($"{who}_boulder"));
        game.Arrive("DistortionWorldB5F", 30, 34);
        Assert.NotNull(game.Present($"{who}_boulder"));
        // And it can be brought down again, to stand where a fallen boulder stands
        DropFromB5F(game, who);
        game.Arrive("DistortionWorldB6F", 31, 30);
        game.Map.ResetObstacles();
        var again = game.Present($"{who}_boulder")!;
        Assert.Equal(who switch { "mesprit" => (19, 30), "azelf" => (42, 29), _ => (30, 18) }, (again.GridX, again.GridY));
    }

    // ------------------------------------------------------------------ B7F and Cyrus

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Left)]
    public void CyrusIsBeatenOnB7FAndCynthiaLeadsOnToGiratinasRoom(Direction talkFacing)
    {
        var game = NewGame();
        SetProgress(game, 7);
        game.Arrive("DistortionWorldB7F", 11, 54);
        // Cynthia at the slab's foot goes up the path to where Cyrus waits
        Assert.Equal(8, ProgressOf(game));
        Assert.Contains(game.Last.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("Giratina is up ahead"));
        Assert.Equal((11, 42, 11, 38), (game.Present("cynthia")!.GridX, game.Present("cynthia")!.GridY, game.Present("cyrus")!.GridX, game.Present("cyrus")!.GridY));

        // The row before them, from each of its tiles
        foreach (int x in new[] { 10, 11, 12 }) Assert.Equal("CynthiaAndCyrus", FieldScripts.TriggerAt(game.Map, x, 44, game.Story)!.Script);
        var argued = StepOnto(game, 12, 44, "CynthiaAndCyrus");
        Assert.Equal(9, ProgressOf(game));
        Assert.Contains(argued.Transcript, l => l.Speaker == "Cyrus" && l.Text.Contains("my justice"));
        Assert.Equal((10, 42, Direction.Right), (game.Present("cynthia")!.GridX, game.Present("cynthia")!.GridY, game.Present("cynthia")!.Facing));
        Assert.Equal((11, 42, Direction.Left), (game.Present("cyrus")!.GridX, game.Present("cyrus")!.GridY, game.Present("cyrus")!.Facing));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 11, 44, game.Story));
        // Coming down again finds them as they were left
        game.Arrive("DistortionWorldB7F", 11, 54);
        Assert.Empty(game.Last.Transcript);
        Assert.Equal((10, 42, 11, 42), (game.Present("cynthia")!.GridX, game.Present("cynthia")!.GridY, game.Present("cyrus")!.GridX, game.Present("cyrus")!.GridY));

        // The battle, from the south or from the east of him
        var battle = TalkFrom(game, "cyrus", talkFacing);
        Assert.Contains("battle galactic_boss_cyrus_distortion_world Won", battle.Log);
        Assert.True(game.Story.HasDefeated("galactic_boss_cyrus_distortion_world"));
        Assert.Equal(10, ProgressOf(game));
        Assert.Contains("fanfare FanfareHeal", battle.Log);
        Assert.Contains(battle.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("meet Giratina"));
        Assert.Null(game.Present("cyrus"));
        // The two stand on the last stretch, she behind the player, the way to Giratina's room north of them
        Assert.Equal((15, 37, Direction.Up), (game.Present("cynthia")!.GridX, game.Present("cynthia")!.GridY, game.Present("cynthia")!.Facing));
        Assert.Equal((15, 36), game.Tile);
        Assert.Equal(("DistortionWorldGiratinaRoom", 15, 23), (game.Map.GetWarpAt(15, 24)!.TargetMap, game.Map.GetWarpAt(15, 24)!.TargetX, game.Map.GetWarpAt(15, 24)!.TargetY));
        game.Arrive("DistortionWorldB7F", 15, 25);
        Assert.Null(game.Present("cyrus"));
        Assert.Contains(TalkFrom(game, "cynthia", Direction.Down).Transcript, l => l.Text.Contains("bound to each other"));
    }

    [Fact]
    public void ALostBattleWithCyrusLeavesHimStandingThere()
    {
        var game = NewGame(fight: BattleOutcome.Lost);
        SetProgress(game, 9);
        game.Arrive("DistortionWorldB7F", 11, 54);
        var battle = TalkFrom(game, "cyrus", Direction.Up);
        Assert.Contains("battle galactic_boss_cyrus_distortion_world Lost", battle.Log);
        Assert.Equal(9, ProgressOf(game));
        game.Arrive("DistortionWorldB7F", 11, 54);
        Assert.Equal((11, 42), (game.Present("cyrus")!.GridX, game.Present("cyrus")!.GridY));
        Assert.Contains(TalkFrom(game, "cynthia", Direction.Up).Transcript, l => l.Text.Contains("no justice"));
    }

    // ------------------------------------------------------------------ Giratina

    private static OpeningTests.Game BeforeGiratina(BattleOutcome fight)
    {
        var game = NewGame(fight: fight);
        SetProgress(game, 10);
        game.Arrive("DistortionWorldGiratinaRoom", 15, 23);
        Assert.Equal(11, ProgressOf(game));
        Assert.Contains("cry Giratina-Origin", game.Last.Log);
        foreach (string key in new[] { "giratina", "portal", "cynthia", "cyrus" }) Assert.Null(game.Present(key));
        StepOnto(game, 15, 17, "SecondShadow");
        Assert.Equal(12, ProgressOf(game));
        var arrival = StepOnto(game, 15, 14, "Arrival");
        Assert.Equal(13, ProgressOf(game));
        Assert.Contains(arrival.Transcript, l => l.Text.Contains("eyes do not leave"));
        var giratina = game.Present("giratina")!;
        Assert.Equal((15, 13, "Giratina-Origin", 1.8f), (giratina.GridX, giratina.GridY, giratina.Species, giratina.Level));
        return game;
    }

    [Theory]
    [InlineData(BattleOutcome.Won, "Beaten, it has slipped away")]
    [InlineData(BattleOutcome.Fled, "You wouldn't fight it")]
    [InlineData(BattleOutcome.Caught, "chose to go with you")]
    public void GiratinaIsMetInItsOriginFormeAndWhateverTheBattleComesToQuietsIt(BattleOutcome outcome, string said)
    {
        var game = BeforeGiratina(outcome);
        // It waits there for as long as it takes
        game.Arrive("DistortionWorldGiratinaRoom", 15, 23);
        Assert.Empty(game.Last.Transcript);
        Assert.NotNull(game.Present("giratina"));

        int team = game.Party.Members.Count;
        var battle = TalkFrom(game, "giratina", Direction.Up);
        // A wild Pokémon's battle at level 47, which may be run from
        Assert.Contains($"wildbattle Giratina 47 {outcome}", battle.Log);
        Assert.Equal(14, ProgressOf(game));
        Assert.Contains(battle.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains(said));
        // Cyrus hears that no new world will be made, and leaves alone
        Assert.Contains(battle.Transcript, l => l.Speaker == "Cyrus" && l.Text.Contains("cannot be made"));
        Assert.Contains(battle.Transcript, l => l.Speaker == "Cyrus" && l.Text == "......");
        Assert.Null(game.Present("cyrus"));
        Assert.Null(game.Present("giratina"));
        Assert.Equal((15, 17), (game.Present("cynthia")!.GridX, game.Present("cynthia")!.GridY));
        Assert.NotNull(game.Present("portal"));

        // What each end leaves for Turnback Cave
        bool caught = outcome == BattleOutcome.Caught;
        Assert.Equal(caught, game.Story.Has("FLAG_CAUGHT_GIRATINA"));
        Assert.Equal(caught, game.Story.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA"));
        Assert.Equal(!caught, game.Story.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM"));
        Assert.Equal(caught ? team + 1 : team, game.Party.Members.Count);
        if (caught) Assert.Equal(("Giratina", "Giratina-Origin", 47), (game.Party.Members[^1].Species.Name, game.Party.Members[^1].Form, game.Party.Members[^1].Level));
        game.Arrive("TurnbackCaveGiratinaRoom", 11, 18);
        Assert.Equal(!caught, game.Present("giratina") != null);
        Assert.Equal(caught, game.Present("portal") != null);

        // The room afterwards: the portal where it hung, Cynthia on the stone behind, who answers from there
        game.Arrive("DistortionWorldGiratinaRoom", 15, 23);
        Assert.Empty(game.Last.Transcript);
        Assert.Null(game.Present("giratina"));
        Assert.Equal((15, 13, 1.8f), (game.Present("portal")!.GridX, game.Present("portal")!.GridY, game.Present("portal")!.Level));
        Assert.Contains(game.Play(Find(game.Map.TileScripts[(15, 15)], "distortion_world_giratina_room")).Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("go home"));
    }

    [Fact]
    public void ALostBattleLeavesGiratinaWaiting()
    {
        var game = BeforeGiratina(BattleOutcome.Lost);
        var battle = TalkFrom(game, "giratina", Direction.Up);
        Assert.Contains("wildbattle Giratina 47 Lost", battle.Log);
        Assert.Equal(13, ProgressOf(game));
        Assert.DoesNotContain(battle.Transcript, l => l.Speaker == "Cynthia");
        game.Arrive("DistortionWorldGiratinaRoom", 15, 23);
        Assert.NotNull(game.Present("giratina"));
        Assert.Null(game.Present("portal"));
        Assert.False(game.Story.Has("FLAG_CAUGHT_GIRATINA"));
    }

    // ------------------------------------------------------------------ the way out

    [Fact]
    public void ThePortalLetsOutAtSendoffSpringAndSetsWhatTheNextChapterFinds()
    {
        var game = NewGame();
        SetProgress(game, 14);
        Run(game, "sendoff_spring", "setflag FLAG_SPEAR_PILLAR_IS_DISTORTED\nclearflag FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F\nclearflag FLAG_HIDE_MT_CORONET_2F_LOOKER");
        game.Arrive("DistortionWorldGiratinaRoom", 15, 14);
        Assert.Null(game.Maps["Sinnoh"].AreaAt(752, 690) is { Open: true } ? null : "the Spring Path is hidden still");
        // A no leaves the player in the room
        game.Answers.Enqueue(1);
        Assert.DoesNotContain(TalkFrom(game, "portal", Direction.Up).Log, l => l.StartsWith("warp "));
        Assert.Equal(0, game.Story.Var("VAR_EXITED_DISTORTION_WORLD_STATE"));

        var leap = TalkFrom(game, "portal", Direction.Up);
        Assert.Contains("warp SendoffSpring 32 17", leap.Log);
        Assert.Equal(786, game.Story.Var("VAR_HIDDEN_LOCATION_SPRING_PATH"));
        // The scene by the spring follows at once
        Assert.Equal("sendoff_spring.OnEnter", game.Played[^1].Script);
        var scene = game.Last;
        Assert.Equal(("SendoffSpring", (32, 17)), (game.Map.Name, game.Tile));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("Sendoff Spring"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Professor Rowan"));
        Assert.Equal(2, game.Story.Var("VAR_EXITED_DISTORTION_WORLD_STATE"));
        Assert.Equal(2, game.Story.Var("VAR_SANDGEM_TOWN_LAB_STATE"));
        foreach (string flag in new[] { "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_TEAM_GALACTIC", "FLAG_HIDE_MT_CORONET_GALACTIC_GRUNTS", "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F",
            "FLAG_HIDE_MT_CORONET_2F_LOOKER", "FLAG_ALT_MUSIC_GALACTIC_HQ_1F", "FLAG_GALACTIC_LEFT_LAKE_VALOR" })
            Assert.True(game.Story.Has(flag), flag);
        foreach (string flag in new[] { "FLAG_SPEAR_PILLAR_IS_DISTORTED", "FLAG_HIDE_GALACTIC_HQ_1F_SATURN", "FLAG_HIDE_SENDOFF_SPRING_CYNTHIA" })
            Assert.False(game.Story.Has(flag), flag);

        // It plays once; Cynthia stays by the water, before the mouth of Turnback Cave, which nobody enters past her
        game.Arrive("SendoffSpring", 32, 17);
        Assert.Empty(game.Last.Transcript);
        var cynthia = game.Present("cynthia")!;
        Assert.Equal((31, 17), (cynthia.GridX, cynthia.GridY));
        Assert.Equal("TurnbackCaveEntrance", game.Map.GetWarpAt(31, 16)!.TargetMap);
        Assert.All(new[] { (30, 16), (32, 16), (30, 15), (31, 15), (32, 15) }, t => Assert.True(game.Map.IsSolid(t.Item1, t.Item2)));
        Assert.Contains(TalkFrom(game, "cynthia", Direction.Left).Transcript, l => l.Text.Contains("Tell Professor Rowan"));
        Run(game, "sendoff_spring", "setvar VAR_EXITED_DISTORTION_WORLD_STATE 3");
        Assert.Contains(TalkFrom(game, "cynthia", Direction.Left).Transcript, l => l.Text.Contains("been the one to battle it"));
        // The Hall of Fame takes her from the cave's mouth
        Assert.Contains(Find("OnEnter", "PokemonLeagueHallOfFame").Everything(), i => i.Op == Op.SetFlag && i.Name == "FLAG_HIDE_SENDOFF_SPRING_CYNTHIA");
        Run(game, "sendoff_spring", "setflag FLAG_HIDE_SENDOFF_SPRING_CYNTHIA");
        game.Arrive("SendoffSpring", 32, 17);
        Assert.Null(game.Present("cynthia"));

        // The mountain is clear, its summit whole, and Team Galactic's four gone from the torn one
        game.Arrive("MtCoronet6F", 7, 7);
        Assert.Equal("SpearPillar", game.Map.GetWarpAt(7, 5)!.TargetMap);
        Assert.Null(game.Present("galactic_grunt"));
        Assert.DoesNotContain(game.Maps["MtCoronet2F"].NPCs, n => n.Key == "looker");
        Assert.DoesNotContain(game.Maps["SpearPillarDistorted"].NPCs, n => n.Key is "mars" or "jupiter" or "grunt_f" or "grunt_m");

        // Commander Saturn is in the lobby of their headquarters, and its people speak of what is over
        game.Arrive("GalacticHQ1F", 8, 22);
        Assert.Contains(game.Talk("saturn").Transcript, l => l.Speaker == "Saturn" && l.Text.Contains("Spear Pillar"));
        Assert.Contains(game.Play(Find("Receptionist", "GalacticHQ1F")).Transcript, l => l.Text.Contains("nothing whatever is planned"));
        Assert.Contains(game.Play(Find("Scientist", "GalacticHQ1F")).Transcript, l => l.Text.Contains("Not that anybody has told me how"));
        Assert.Contains(game.Play(Find("GruntM", "GalacticHQ1F")).Transcript, l => l.Text.Contains("nothing left to take"));
        Assert.Contains(game.Play(Find("GruntF2", "GalacticHQ1F")).Transcript, l => l.Text.Contains("Where has our boss got to"));

        // And Lake Valor is full again: the lakefront leads to the lake as it was
        var lakefront = game.Maps["Sinnoh"].Warps.Where(w => w.TargetMap.StartsWith("LakeValor", StringComparison.Ordinal)).ToList();
        Assert.All(lakefront, w => Assert.Equal(w.TargetMap == "LakeValorDrained", w.Shut));
        Assert.Contains(lakefront, w => w.TargetMap == "LakeValor" && !w.Shut);
    }

    [Fact]
    public void TheLobbyOfTheHeadquartersSaysItsFirstLinesUntilThen()
    {
        var game = NewGame();
        game.Arrive("GalacticHQ1F", 8, 22);
        Assert.Null(game.Present("saturn"));
        Assert.Contains(game.Play(Find("Receptionist", "GalacticHQ1F")).Transcript, l => l.Text.Contains("energy of the cosmos"));
        Assert.Contains(game.Play(Find("Scientist", "GalacticHQ1F")).Transcript, l => l.Text.Contains("New sources of energy"));
        Assert.Contains(game.Play(Find("GruntM", "GalacticHQ1F")).Transcript, l => l.Text.Contains("That's how we do things!"));
        Assert.Contains(game.Play(Find("GruntF2", "GalacticHQ1F")).Transcript, l => l.Text.Contains("far too young"));
    }

    // ------------------------------------------------------------------ Turnback Cave

    [Fact]
    public void GiratinaWaitsInTurnbackCaveUnlessCaughtAndAPortalHangsWhereItStood()
    {
        var game = NewGame();
        game.Arrive("TurnbackCaveGiratinaRoom", 11, 18);
        // Not caught in the Distortion World: it stands there, in its Altered Forme, and no portal
        var giratina = game.Present("giratina")!;
        Assert.Equal((11, 14, "Giratina"), (giratina.GridX, giratina.GridY, giratina.Species));
        Assert.Null(game.Present("portal"));
        // Beaten, it is gone until the Hall of Fame is next entered
        var battle = game.Talk("giratina");
        Assert.Contains("wildbattle Giratina 47 nofleeing Won", battle.Log);
        Assert.True(game.Story.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA"));
        Assert.False(game.Story.Has("FLAG_CAUGHT_GIRATINA"));
        game.Arrive("TurnbackCaveGiratinaRoom", 11, 18);
        Assert.Null(game.Present("giratina"));
        Assert.Contains(Find("OnEnter", "PokemonLeagueHallOfFame").Everything(), i => i.Op == Op.If && i.Then is { Op: Op.ClearFlag, Name: "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA" });

        // The portal hangs where it stood, and leads into the Distortion World's room behind the cave, or not
        var portal = game.Present("portal")!;
        Assert.Equal((11, 14, "Rift"), (portal.GridX, portal.GridY, portal.NpcType));
        game.Answers.Enqueue(1);
        Assert.DoesNotContain(game.Talk("portal").Log, l => l.StartsWith("warp "));
        Assert.Contains("warp DistortionWorldTurnbackCaveRoom 46 45", game.Talk("portal").Log);
        Assert.Equal(("DistortionWorldTurnbackCaveRoom", (46, 45)), (game.Map.Name, game.Tile));
        Assert.False(game.Map.IsSolid(46, 45));

        // There the Griseous Orb lies in its ball, once, and the portal behind leads back
        var ball = game.Present("item_griseous_orb")!;
        Assert.Equal((19, 25, true, "Griseous Orb"), (ball.GridX, ball.GridY, ball.IsItemBall, ball.Item));
        game.Talk("item_griseous_orb");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Griseous Orb")!));
        Assert.True(game.Story.Has("FLAG_OBTAINED_DISTORTION_WORLD_TURNBACK_CAVE_ROOM_GRISEOUS_ORB"));
        game.Arrive("DistortionWorldTurnbackCaveRoom", 46, 45);
        Assert.Null(game.Present("item_griseous_orb"));
        var back = game.Present("portal")!;
        Assert.Equal((46, 44), (back.GridX, back.GridY));
        Assert.Contains("warp TurnbackCaveGiratinaRoom 11 15", game.Talk("portal").Log);
        Assert.Equal("TurnbackCaveGiratinaRoom", game.Map.Name);
    }

    // ------------------------------------------------------------------ the whole chapter, and the way through it

    [Fact]
    public void TheChapterPlaysThroughInOrderFromTheButtonToSendoffSpring()
    {
        var game = NewGame(1);
        PressTheButton(game);

        // The climb: Looker before the hole, and through it
        game.Arrive("MtCoronet2F", 12, 25);
        StepOnto(game, 11, 25, "Looker");
        game.Through("MtCoronet2F");
        Assert.True(game.Tile.Y < 14);

        // Spear Pillar
        game.Arrive("MtCoronet6F", 7, 7);
        game.Through("SpearPillar");
        StepOnto(game, 31, 48, "GruntsStop");
        StepOnto(game, 31, 32, "MarsAndJupiter");
        Assert.Equal("DistortionWorld1F", game.Map.Name);

        // Down through the Distortion World
        StepOnto(game, 19, 42, "SlabMoves");
        game.Through("DistortionWorldB1F");
        StepOnto(game, 15, 23, "Mesprit");
        game.Arrive("DistortionWorldB2F", 18, 45);
        TalkFrom(game, "cynthia", Direction.Right);
        game.Arrive("DistortionWorldB3F", 18, 10);
        StepOnto(game, 18, 20, "CyrusToo");
        game.Arrive("DistortionWorldB4F", 22, 28);
        StepOnto(game, 41, 22, "ShadowEast");
        foreach (string who in new[] { "uxie", "azelf", "mesprit" })
        {
            DropFromB5F(game, who);
            PushOnB6F(game, who, RightPit[who]);
        }
        game.Arrive("DistortionWorldB6F", 31, 30);
        game.Talk("cynthia");
        game.Through("DistortionWorldB7F");
        Assert.Equal(8, ProgressOf(game));
        StepOnto(game, 11, 44, "CynthiaAndCyrus");
        TalkFrom(game, "cyrus", Direction.Up);
        game.Through("DistortionWorldGiratinaRoom");
        StepOnto(game, 15, 17, "SecondShadow");
        StepOnto(game, 15, 14, "Arrival");
        TalkFrom(game, "giratina", Direction.Up);
        TalkFrom(game, "portal", Direction.Up);

        Assert.Equal("SendoffSpring", game.Map.Name);
        Assert.Equal(14, ProgressOf(game));
        Assert.Equal(2, game.Story.Var("VAR_EXITED_DISTORTION_WORLD_STATE"));
        Assert.Equal(3, game.Story.Var("VAR_SPEAR_PILLAR_STATE"));
        Assert.True(game.Story.Has("FLAG_GALACTIC_LEFT_LAKE_VALOR"));
        foreach (string trainer in new[] { "galactic_grunt_spear_pillar_1", "galactic_grunt_spear_pillar_2", "commander_mars_spear_pillar", "commander_jupiter_spear_pillar", "galactic_boss_cyrus_distortion_world" })
            Assert.True(game.Story.HasDefeated(trainer), trainer);
        // Nothing of the chapter's own was left unplayed on the way
        foreach (string script in new[] { "mt_coronet_2f.Looker", "spear_pillar.GruntsStop", "spear_pillar.MarsAndJupiter", "spear_pillar_distorted.OnEnter", "distortion_world_1f.OnEnter",
            "distortion_world_b1f.OnEnter", "distortion_world_b7f.OnEnter", "distortion_world_giratina_room.OnEnter", "sendoff_spring.OnEnter" })
            Assert.Contains(game.Played, p => p.Script == script);
    }

    [Fact]
    public void TheDistortionWorldIsComeToThroughTheScenesAndLeftThroughItsPortals()
    {
        var game = NewGame();
        Map Named(string name) => game.Maps[name];
        // From the torn pillar: Cynthia's question leads in, the floors lead down to Giratina's room, and its portal out
        var fromThePillar = WorldWalk.From(Named, new MapSpot("SpearPillarDistorted", 30, 30, Direction.Up));
        foreach (string floor in new[] { "DistortionWorld1F", "DistortionWorldB1F", "DistortionWorldB2F", "DistortionWorldB3F", "DistortionWorldB4F", "DistortionWorldB5F",
            "DistortionWorldB6F", "DistortionWorldB7F", "DistortionWorldGiratinaRoom", "SendoffSpring", "MtCoronet6F" })
            Assert.True(fromThePillar.ContainsKey(Named(floor)), $"{floor} isn't come to from the torn pillar");
        Assert.Contains((15, 14), fromThePillar[Named("DistortionWorldGiratinaRoom")]);
        Assert.Contains((32, 17), fromThePillar[Named("SendoffSpring")]);
        // Every boulder's place on both floors is walked up to, and so is Cynthia's in the passage
        foreach (var (x, y) in new[] { (17, 34), (42, 33), (29, 21) }) Assert.Contains((x, y), fromThePillar[Named("DistortionWorldB5F")]);
        foreach (var (x, y) in new[] { (12, 31), (40, 28), (31, 22), (29, 41) }) Assert.Contains((x, y), fromThePillar[Named("DistortionWorldB6F")]);

        // From Turnback Cave's last room: the portal where Giratina stood, the room behind it, and the Griseous Orb
        var fromTheCave = WorldWalk.From(Named, new MapSpot("TurnbackCaveGiratinaRoom", 11, 18, Direction.Up));
        Assert.True(fromTheCave.ContainsKey(Named("DistortionWorldTurnbackCaveRoom")));
        Assert.Contains((18, 25), fromTheCave[Named("DistortionWorldTurnbackCaveRoom")]);
    }

    // ------------------------------------------------------------------ saves from before

    [Fact]
    public void ASaveFromBeforeTheChapterHidesItsPeopleAndKeepsWhatItHasDone()
    {
        string[] always = { "FLAG_HIDE_MT_CORONET_2F_LOOKER", "FLAG_HIDE_SPEAR_PILLAR_RIVAL", "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_CYNTHIA", "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_MESPRIT",
            "FLAG_HIDE_SPEAR_PILLAR_LEGENDS", "FLAG_HIDE_DISTORTION_WORLD_APPARITIONS", "FLAG_HIDE_DISTORTION_WORLD_B6F_MESPRIT_BOULDER", "FLAG_HIDE_DISTORTION_WORLD_B6F_AZELF_BOULDER",
            "FLAG_HIDE_DISTORTION_WORLD_B6F_UXIE_BOULDER" };
        Assert.True(StoryState.CurrentVersion >= 15);

        // Before the headquarters fell: the shards are hidden with the rest, and Giratina, which a game of ours hid in
        // Turnback Cave until the Hall of Fame, stands there again
        var before = new StoryState();
        before.Set("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA");
        StoryMigration.Upgrade(before, 14, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always) Assert.True(before.Has(flag), flag);
        Assert.True(before.Has("FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS"));
        Assert.False(before.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA"));
        Assert.False(before.Has(StoryMigration.SendoffCynthiaGoneFlag));

        // The button already pressed: the shards it showed stay on show, and the way up is open
        var pressed = new StoryState();
        pressed.Set("FLAG_FREED_GALACTIC_HQ_POKEMON");
        pressed.Set("FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING");
        pressed.SetVar("VAR_MT_CORONET_2F_STATE", 1);
        StoryMigration.Upgrade(pressed, 14, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always) Assert.True(pressed.Has(flag), flag);
        Assert.False(pressed.Has("FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS"));

        // The Hall of Fame entered, and Giratina caught in Turnback Cave since: it stays caught and gone, and Cynthia,
        // who would stand before the cave's mouth, has gone
        var champion = new StoryState();
        foreach (string flag in new[] { "FLAG_GAME_COMPLETED", "FLAG_CAUGHT_GIRATINA", "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA" }) champion.Set(flag);
        StoryMigration.Upgrade(champion, 14, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(champion.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA"));
        Assert.True(champion.Has(StoryMigration.SendoffCynthiaGoneFlag));

        // From any older version the same holds, through the chapters' own scripts
        var old = new StoryState();
        StoryMigration.Upgrade(old, 9, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always) Assert.True(old.Has(flag), flag);

        // A save of today is left as it is
        var today = new StoryState();
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.Empty(today.Flags);
    }

    [Fact]
    public void ANewGameStartsWithTheChapterAsAnOlderSaveIsGivenIt()
    {
        var fresh = new StoryState();
        StoryMigration.BeginNewGame(fresh, Scripts);
        var upgraded = new StoryState();
        StoryMigration.Upgrade(upgraded, 0, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in fresh.Flags.Where(f => f.Contains("SPEAR_PILLAR") || f.Contains("DISTORTION_WORLD") || f.Contains("MT_CORONET_2F")))
            Assert.True(upgraded.Has(flag), flag);
        // The chapter's script is the last link of the chain a new game runs
        Assert.Contains(Find("Headquarters", ScriptLibrary.Common).Everything(), i => i.Op == Op.Call && i.Name == "ChapterNine");
        Assert.All(Find("ChapterNine", ScriptLibrary.Common).Everything(), i => Assert.Contains(i.Op, new[] { Op.SetFlag, Op.ClearFlag, Op.SetVar, Op.If, Op.Return }));
    }

    // ------------------------------------------------------------------ the rules the chapter added

    [Fact]
    public void AWildPokemonMayBeMetInAFormOfItsSpecies()
    {
        var host = new HeadlessScriptHost { Fight = _ => BattleOutcome.Caught };
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Torterra")!, 50));
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(ScriptParser.Parse("test", "script Test\n  wildbattle \"Giratina-Origin\" 47\n  wildbattle \"Starly\" 3")[0]);
        runner.RunToEnd();
        Assert.Equal(new (string, string?)[] { ("Giratina", "Giratina-Origin"), ("Starly", null) }, host.Party.Members.Skip(1).Select(p => (p.Species.Name, p.Form)));
        // A name that is neither a species nor a form is told apart from a typing slip as the game starts
        var wrong = ScriptLibrary.FromSources(("test", "script Test\n  wildbattle \"Giratina-Sideways\" 47"));
        Assert.Contains(wrong.Problems(_ => true, _ => true, _ => true), p => p.Contains("Giratina-Sideways"));
        Assert.Empty(ScriptLibrary.FromSources(("test", "script Test\n  wildbattle \"Giratina-Origin\" 47")).Problems(_ => true, _ => true, _ => true));
    }

    [Fact]
    public void TheStoryTellsThePokedexOfAPokemonItShows()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(ScriptParser.Parse("test", "script Test\n  seen \"Dialga\"\n  seen \"Palkia\"\n  seen \"Dialga\"")[0]);
        runner.RunToEnd();
        Assert.Equal(new[] { "Dialga", "Palkia" }, host.SeenSpecies);
        Assert.Equal(3, host.Log.Count(l => l.StartsWith("seen ")));
    }

    [Fact]
    public void AWayTheStoryOpensAloneOnItsTileIsAWallUntilThen()
    {
        var map = new Map(4, 4);
        map.Warps.Add(new Warp { SourceX = 1, SourceY = 1, TargetMap = "Test", TargetX = 2, TargetY = 2, From = "FLAG_OPEN" });
        // Two that share a tile leave it as it is: one of them is always open
        map.Warps.Add(new Warp { SourceX = 2, SourceY = 1, TargetMap = "Test", TargetX = 2, TargetY = 2, Until = "FLAG_OPEN" });
        map.Warps.Add(new Warp { SourceX = 2, SourceY = 1, TargetMap = "Test", TargetX = 3, TargetY = 3, From = "FLAG_OPEN" });
        map.ApplyDoors(_ => false);
        Assert.True(map.IsSolid(1, 1));
        Assert.Null(map.GetWarpAt(1, 1));
        Assert.False(map.IsSolid(2, 1));
        Assert.Equal(2, map.GetWarpAt(2, 1)!.TargetX);
        map.ApplyDoors(_ => true);
        Assert.False(map.IsSolid(1, 1));
        Assert.NotNull(map.GetWarpAt(1, 1));
        Assert.False(map.IsSolid(2, 1));
        Assert.Equal(3, map.GetWarpAt(2, 1)!.TargetX);
    }

    [Fact]
    public void WhoeverTheStoryHangsOverNothingIsDrawnAtTheHeightItGivesThem()
    {
        var game = NewGame();
        var room = game.Maps["DistortionWorldGiratinaRoom"];
        // The stone the player stands on, the nothing north of it, and Giratina hanging there level with the stone
        float stone = Relief.At(room, 15.5f, 14.5f);
        Assert.True(room.IsVoid && room.IsSolid(15, 13) && room.GetGroundTile(15, 13) == TileType.Void);
        Assert.True(Relief.At(room, 15.5f, 13.5f) < stone - 2f);
        Assert.Equal(stone, Relief.Under(room, 15.5f, 13.5f, room.HeightAt(15, 14)));
        // On the stone itself a height changes nothing
        Assert.Equal(stone, Relief.Under(room, 15.5f, 14.5f, room.HeightAt(15, 14)));
        // Giratina hangs clear of the head of whoever stands before it, and is still found from the stone beside it
        var giratina = room.Everyone.First(n => n.Key == "giratina");
        Assert.InRange(giratina.Level!.Value - room.HeightAt(15, 14), 0.5f, FieldMovement.StepLimit - 0.1f);
        Assert.Same(giratina, new[] { giratina }.FirstOrDefault(n => MathF.Abs(n.Level!.Value - room.HeightAt(15, 14)) < FieldMovement.StepLimit));
        // Over an island's rim, which stands above the island, they are on the rim: Mesprit on B1F
        var b1 = game.Maps["DistortionWorldB1F"];
        var mesprit = b1.Everyone.First(n => n.Key == "mesprit");
        Assert.True(b1.IsSolid(mesprit.GridX, mesprit.GridY) && b1.GetGroundTile(mesprit.GridX, mesprit.GridY) == TileType.Rock);
        float rim = Relief.At(b1, mesprit.GridX + 0.5f, mesprit.GridY + 0.5f);
        Assert.True(rim > mesprit.Level!.Value - b1.GroundLevel);
        Assert.Equal(rim, Relief.Under(b1, mesprit.GridX + 0.5f, mesprit.GridY + 0.5f, mesprit.Level.Value));
    }

    [Theory]
    [InlineData("DistortionWorldB5F")]
    [InlineData("DistortionWorldB6F")]
    public void APitIsNothingLetIntoTheIslandAndABoulderSinksIntoItsMouth(string floor)
    {
        var game = NewGame();
        var map = game.Maps[floor];
        Assert.NotEmpty(map.BoulderPits);
        foreach (var (x, y) in map.BoulderPits.Keys)
        {
            // Nothing, shut to whoever walks, as deep as the nothing round the islands, with open floor beside it
            Assert.True(map.GetGroundTile(x, y) == TileType.Void && map.IsSolid(x, y), $"the pit at {x},{y} of {floor} is {map.GetGroundTile(x, y)}");
            var beside = new[] { (x, y + 1), (x + 1, y), (x - 1, y), (x, y - 1) }.Where(n => !map.IsSolid(n.Item1, n.Item2)).ToList();
            Assert.NotEmpty(beside);
            float level = beside.Max(n => Relief.At(map, n.Item1 + 0.5f, n.Item2 + 0.5f));
            Assert.Equal(level - WorldMapBuilder.VoidDrop, Relief.At(map, x + 0.5f, y + 0.5f), 3);
            // A boulder pushed over it: level with the floor to the lip, sinking from there, in its mouth at rest
            Assert.Equal(level, Relief.InPit(map, x, y, 1f), 3);
            Assert.Equal(level, Relief.InPit(map, x, y, 0.6f), 3);
            Assert.InRange(Relief.InPit(map, x, y, 0.3f), level - Relief.PitSink, level - 0.01f);
            Assert.Equal(level - Relief.PitSink, Relief.InPit(map, x, y, 0f), 3);
            float last = level;
            for (float left = 1f; left >= 0f; left -= 0.05f)
            {
                float now = Relief.InPit(map, x, y, left);
                Assert.True(now <= last + 1e-4f, $"the boulder rises again at {left}");
                last = now;
            }
        }
        // Its top stays above the floor: a boulder's card is most of a tile tall
        Assert.InRange(Relief.PitSink, 0.3f, 0.75f);
    }

    [Fact]
    public void TheSummitsBattlesAreFoughtOnRockAndByTheMountainsRules()
    {
        var game = NewGame();
        foreach (string name in new[] { "SpearPillar", "SpearPillarDistorted" })
        {
            var map = game.Maps[name];
            Assert.Equal(BattleArena.Cave, map.ArenaAt(31, 32));
            Assert.Equal("Mountain", map.AreaAt(31, 32)!.BattleBackground);
        }
    }

    [Fact]
    public void RubbleIsAThingOfItsOwnPaintedInTheCavesRock()
    {
        var rubble = new NPC { NpcType = NPC.RubbleType };
        Assert.True(rubble.IsThing);
        Assert.Equal(PropType.Rubble, rubble.GymThing);
        Assert.Null(rubble.Obstacle);
        var card = ThingCards.Paint(PropType.Rubble);
        Assert.Equal((30, 20), (card.Width, card.Height));
        var colours = new HashSet<(byte, byte, byte)>();
        for (int y = 0; y < card.Height; y++)
            for (int x = 0; x < card.Width; x++)
                if (card.Get(x, y).A == 255) colours.Add((card.Get(x, y).R, card.Get(x, y).G, card.Get(x, y).B));
        // The style guide's tones: the slabs' three, and the painting's ochre on the largest
        foreach (var tone in new (byte, byte, byte)[] { (150, 138, 150), (118, 106, 122), (84, 74, 92), (196, 108, 64) }) Assert.Contains(tone, colours);
        Assert.Equal(ThingCards.FootOf(PropType.CrackedRock), ThingCards.FootOf(PropType.Rubble));
    }

    [Fact]
    public void TheCavePaintingIsASlabThatCoversTheWayBehindIt()
    {
        var painting = new NPC { NpcType = NPC.CavePaintingType };
        Assert.True(painting.IsThing);
        Assert.Equal(PropType.CavePainting, painting.GymThing);
        Assert.Null(painting.Obstacle);
        var card = ThingCards.Paint(PropType.CavePainting);
        // A whole tile across, and with where it stands tall enough to cover the tile behind its own from the field's
        // camera, under which a tile of ground is 0.857 of a tile up the picture (sin 59.05)
        Assert.Equal(32, card.Width);
        float foot = ThingCards.FootOf(PropType.CavePainting);
        Assert.True(card.Height - 2 >= (foot + 1f) * 0.857f * 32f - 0.5f, $"a card of {card.Height} standing {foot} into its tile leaves the tile behind it showing");
        // One slab: every row has stone from side to side but for the rounding of its head, and nothing is see-through inside it
        var colours = new HashSet<(byte, byte, byte)>();
        for (int y = 1; y < card.Height - 1; y++)
        {
            int first = Enumerable.Range(0, card.Width).First(x => card.Get(x, y).A == 255);
            int last = Enumerable.Range(0, card.Width).Last(x => card.Get(x, y).A == 255);
            Assert.True(first <= (y <= 7 ? 8 : 1) && last >= card.Width - 1 - (y <= 7 ? 8 : 1), $"row {y} runs from {first} to {last}");
            for (int x = first; x <= last; x++)
            {
                Assert.Equal(255, card.Get(x, y).A);
                colours.Add((card.Get(x, y).R, card.Get(x, y).G, card.Get(x, y).B));
            }
        }
        // The rubble's stone, and the painting's two colours
        foreach (var tone in new (byte, byte, byte)[] { (150, 138, 150), (118, 106, 122), (84, 74, 92), (196, 108, 64), (222, 210, 196) }) Assert.Contains(tone, colours);
    }
}
