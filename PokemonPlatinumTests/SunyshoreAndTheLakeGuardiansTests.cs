using System;
using System.Collections.Generic;
using System.IO;
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
/// The tenth chapter (plan 02 · S13, "Sunyshore") played through with no screen, in the order a player meets it: the
/// professor's lab after the Distortion World, the three Pokémon of the lakes back in their caverns (Mesprit flying
/// off to roam with the professor after it, Azelf and Uxie battled where they stand), the road to Sunyshore City open,
/// Route 222's Technical Machine, Flint at the city's gate and before its Gym's door, the Vista Lighthouse's lift and
/// Volkner at its top, the Beacon Badge won by the Gym's own script, and the rival and Jasmine on the beach with HM07.
/// Each scene is started as the game starts it, on maps of its own (<see cref="OpeningTests.Game"/>).
/// </summary>
public class SunyshoreAndTheLakeGuardiansTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string LabState = "VAR_SANDGEM_TOWN_LAB_STATE", CityState = "VAR_SUNYSHORE_CITY_STATE", Mesprit = "VAR_ROAMING_MESPRIT_STATE";

    private static OpeningTests.Game NewGame(BattleOutcome fight = BattleOutcome.Won, PlayerLook look = PlayerLook.Boy)
    {
        var game = new OpeningTests.Game(0, fight) { Look = look };
        game.Story.ChooseStarter(StoryState.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Torterra")!, 55));
        return game;
    }

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    private static Script Find(string name, string file) => Scripts.Find(name, file) ?? throw new InvalidOperationException($"no {file}.{name}");

    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    /// <summary>Speaks to someone of a place from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, Direction facing, string? place = null)
    {
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        Assert.False(game.Map.IsSolid(game.Tile.X, game.Tile.Y), $"nobody can stand at {game.Tile} to speak to {key}");
        return game.Play(Find(FieldScripts.For(npc)!, npc.ScriptFile ?? place ?? game.Map.Name), npc);
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

    /// <summary>The story as the scene at Sendoff Spring leaves it (sendoff_spring.txt, plan 02 · S12).</summary>
    private static void OutOfTheDistortionWorld(OpeningTests.Game game) =>
        Run(game, "sendoff_spring", "setvar VAR_EXITED_DISTORTION_WORLD_STATE 2\nsetvar VAR_SANDGEM_TOWN_LAB_STATE 2\nsetflag FLAG_GALACTIC_LEFT_LAKE_VALOR\n"
            + "setflag FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN\nsetflag FLAG_HIDE_LAKE_VALOR_GALACTIC\nsetflag FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY\n"
            + "setflag FLAG_HIDE_LAKE_VERITY_TEAM_GALACTIC\nsetflag FLAG_HIDE_LAKE_ACUITY_JUPITER\nsetflag FLAG_HIDE_LAKE_ACUITY_RIVAL");

    /// <summary>Into the professor's lab by its door, which plays whatever waits there.</summary>
    private static HeadlessScriptHost IntoTheLab(OpeningTests.Game game)
    {
        game.Facing = Direction.Up;
        game.Arrive("RowanLab", 7, 15);
        return game.Last;
    }

    /// <summary>The professor has heard it all (RowanLab.txt): the road east is open and the guardians are home.</summary>
    private static OpeningTests.Game AfterTheLab(BattleOutcome fight = BattleOutcome.Won, PlayerLook look = PlayerLook.Boy)
    {
        var game = NewGame(fight, look);
        OutOfTheDistortionWorld(game);
        IntoTheLab(game);
        Assert.Equal(3, game.Story.Var(LabState));
        return game;
    }

    /// <summary>Into Sunyshore City by the gate from Route 222, on its upper row or its lower.</summary>
    private static HeadlessScriptHost ThroughTheGate(OpeningTests.Game game, int y = 790)
    {
        game.Facing = Direction.Right;
        game.Arrive("Sinnoh", 833, y);
        return game.Last;
    }

    /// <summary>Up the lighthouse by its lift, from the city's door.</summary>
    private static void UpTheLighthouse(OpeningTests.Game game)
    {
        game.Arrive("Sinnoh", 886, 791);
        var door = game.Map.GetWarpAt(886, 790)!;
        Assert.Equal(("VistaLighthouseElevator", 3, 6), (door.TargetMap, door.TargetX, door.TargetY));
        game.Facing = Direction.Up;
        game.Arrive(door.TargetMap, door.TargetX, door.TargetY);
    }

    /// <summary>Volkner beaten, as his Gym's script beats him (SunyshoreGymRoom3.txt, plan 01 · M9), and out again.</summary>
    private static void WinTheBeaconBadge(OpeningTests.Game game)
    {
        game.Arrive("SunyshoreGymRoom3", 11, 4);
        game.Talk("volkner");
        Assert.True(game.Story.HasBadge("Sinnoh", "Beacon"));
        Assert.Equal(2, game.Story.Var(CityState));
        game.Arrive("Sinnoh", 845, 748);
    }

    // ------------------------------------------------------------------ before the chapter's scenes

    [Fact]
    public void ANewGameStandsTheChaptersPeopleWhereTheOriginalDoes()
    {
        var game = NewGame();
        // What a new game hides until its scene: the three Pokémon of the lakes, the professor who comes into Verity
        // Cavern, and the rival on Sunyshore's beach
        foreach (string flag in new[] { "FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_ACUITY_CAVERN_UXIE",
            "FLAG_HIDE_VERITY_CAVERN_PROF_OAK", "FLAG_HIDE_SUNYSHORE_CITY_RIVAL" })
            Assert.True(game.Story.Has(flag), flag);
        // Flint and Volkner are there from the start, and the road east is shut
        foreach (string flag in new[] { "FLAG_HIDE_SUNYSHORE_CITY_FLINT", "FLAG_HIDE_VISTA_LIGHTHOUSE_VOLKNER", "FLAG_VOLKNER_RETURNED_TO_GYM", "FLAG_HIDE_VALOR_LAKEFRONT_COLLECTOR" })
            Assert.False(game.Story.Has(flag), flag);

        var sinnoh = game.Maps["Sinnoh"];
        NPC Of(string key) => sinnoh.Everyone.Single(n => n.Key == key && n.ScriptFile == "sunyshore_city");
        // Sunyshore City: Flint's own tile is before the Gym's door (his scene stands him at the gate), Jasmine looks
        // out to sea from the beach, and the rival's place is south of her
        Assert.Equal((845, 748, Direction.Down, "FLAG_HIDE_SUNYSHORE_CITY_FLINT"), (Of("flint").GridX, Of("flint").GridY, Of("flint").Facing, Of("flint").HiddenBy));
        Assert.Equal((857, 741, Direction.Right), (Of("jasmine").GridX, Of("jasmine").GridY, Of("jasmine").Facing));
        Assert.Equal((855, 747, "FLAG_HIDE_SUNYSHORE_CITY_RIVAL"), (Of("rival").GridX, Of("rival").GridY, Of("rival").HiddenBy));
        Assert.DoesNotContain(sinnoh.NPCs, n => n.Key == "rival" && n.ScriptFile == "sunyshore_city");
        // Nobody of the city stands as the boy the player is: Flint and Jasmine are their own looks (plan 11 · C6)
        Assert.All(sinnoh.Everyone.Where(n => n.ScriptFile == "sunyshore_city" && !n.IsThing && !n.IsItemBall && n.Key != "rival"),
            n => Assert.Contains(n.NpcType, new[] { "Youngster", "Lass", "Lady", "Gentleman", "flint", "jasmine" }));
        // The Gym's door is the tile behind Flint, with wall either side of it
        Assert.Equal("SunyshoreGym", sinnoh.GetWarpAt(845, 747)!.TargetMap);
        Assert.True(sinnoh.IsSolid(844, 747) && sinnoh.IsSolid(846, 747));
        // The beach's trigger waits for the Badge
        var beach = Assert.Single(sinnoh.Triggers, t => t.Script == "RivalAndJasmine");
        Assert.Equal((853, 743, 5, 1, CityState, 2), (beach.X, beach.Y, beach.Width, beach.Depth, beach.Variable, beach.Value));

        // The caverns: nobody home yet
        foreach (var (map, key, x, y) in new[] { ("VerityCavern", "mesprit", 12, 13), ("ValorCavern", "azelf", 16, 14), ("AcuityCavern", "uxie", 14, 10) })
        {
            game.Arrive(map, 14, 28);
            Assert.Null(game.Present(key));
            var guardian = game.Map.Everyone.Single(n => n.Key == key);
            Assert.Equal((x, y, Direction.Down, char.ToUpperInvariant(key[0]) + key[1..]), (guardian.GridX, guardian.GridY, guardian.Facing, guardian.Species));
            Assert.True(game.Story.Has($"FLAG_FIRST_ARRIVAL_{map[..^6].ToUpperInvariant()}_CAVERN"));
        }
        Assert.Null(game.Maps["VerityCavern"].NPCs.FirstOrDefault(n => n.Key == "prof_rowan"));

        // The Vista Lighthouse's gallery: Volkner before the binoculars, looking out, and two visitors at the windows
        var gallery = game.Maps["VistaLighthouse"];
        foreach (var (key, x, y) in new[] { ("volkner", 6, 4), ("sailor", 9, 3), ("youngster", 8, 3) })
        {
            var npc = gallery.NPCs.Single(n => n.Key == key);
            Assert.Equal((x, y, Direction.Up), (npc.GridX, npc.GridY, npc.Facing));
        }
    }

    [Fact]
    public void TheRoadToSunyshoreIsShutUntilTheProfessorHasHeardItAll()
    {
        var game = NewGame();
        OutOfTheDistortionWorld(game);
        game.Arrive("Sinnoh", 722, 790);
        Assert.NotNull(game.Present("collector", "valor_lakefront"));
        foreach (int y in new[] { 789, 790, 791 })
        {
            game.Arrive("Sinnoh", 722, 790);
            var turned = StepOnto(game, 724, y, "BlockSunyshore");
            Assert.Contains(turned.Transcript, l => l.Text.Contains("blackout"));
            Assert.Equal((723, y), turned.PlayerTile);
        }

        IntoTheLab(game);
        game.Arrive("Sinnoh", 722, 790);
        Assert.Null(game.Present("collector", "valor_lakefront"));
        Assert.False(game.Fires("BlockSunyshore"));
        Assert.All(new[] { 789, 790, 791 }, y => Assert.Null(FieldScripts.TriggerAt(game.Map, 724, y, game.Story)));
    }

    // ------------------------------------------------------------------ the professor's lab

    [Theory]
    [InlineData(PlayerLook.Boy, "Welcome back!")]
    [InlineData(PlayerLook.Girl, "Not that you needed it")]
    public void TheProfessorHearsOfTheDistortionWorldAndSendsThePlayerOnToSunyshore(PlayerLook look, string assistants)
    {
        var game = NewGame(look: look);
        game.Seen = 143;
        game.Story.SetBadges("Sinnoh", 0x7F);
        // Until Sendoff Spring nothing waits in the lab: the professor is at the head of his hall and the assistant long gone
        Run(game, "RowanLab", "setvar VAR_SANDGEM_TOWN_LAB_STATE 1\nsetvar VAR_SANDGEM_TOWN_STATE 2\nsetflag FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART");
        Assert.Empty(IntoTheLab(game).Transcript);
        Assert.Equal((7, 5), (game.Present("prof_rowan")!.GridX, game.Present("prof_rowan")!.GridY));
        Assert.Null(game.Present("counterpart"));

        OutOfTheDistortionWorld(game);
        var scene = IntoTheLab(game);
        // The two stand inside the door, facing it (three tiles further up the lab than the original's, clear of the
        // text), and the scene plays as the player comes in and walks up to them
        var rowan = game.Present("prof_rowan")!;
        var assistant = game.Present("counterpart")!;
        Assert.Equal((7, 11, Direction.Down), (rowan.GridX, rowan.GridY, rowan.Facing));
        Assert.Equal((6, 11, Direction.Down), (assistant.GridX, assistant.GridY, assistant.Facing));
        Assert.Equal((7, 12), scene.PlayerTile);
        Assert.Equal(Direction.Up, scene.PlayerFacing);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("You saw it through"));
        string name = look == PlayerLook.Boy ? "Dawn" : "Lucas";
        Assert.Contains(scene.Transcript, l => l.Speaker == name && l.Text.Contains(assistants));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("all three lakes"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("143 seen"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Seven!") && l.Text.Contains("Sunyshore City's Gym"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Pokémon League"));

        // What the original's scene leaves
        Assert.Equal(3, game.Story.Var(LabState));
        Assert.Equal(3, game.Story.Var("VAR_EXITED_DISTORTION_WORLD_STATE"));
        Assert.Equal(1, game.Story.Var("VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_VALOR_LAKEFRONT_COLLECTOR"));
        foreach (string flag in new[] { "FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_ACUITY_CAVERN_UXIE" })
            Assert.False(game.Story.Has(flag), flag);

        // It plays once. Until the player has been to Sunyshore City the two wait by the door: he sends his regards to
        // Cynthia, and the assistant wonders about the Pokémon the player met
        Assert.Empty(IntoTheLab(game).Transcript);
        Assert.Equal((7, 11), (game.Present("prof_rowan")!.GridX, game.Present("prof_rowan")!.GridY));
        Assert.Contains(TalkFrom(game, "prof_rowan", Direction.Up).Transcript, l => l.Text.Contains("Cynthia") && l.Text.Contains("remembers her kindly"));
        Assert.Contains(TalkFrom(game, "counterpart", Direction.Up).Transcript, l => l.Speaker == name && l.Text.Contains("Sinnoh began with"));
        // Cynthia at the spring has her later line
        game.Arrive("SendoffSpring", 32, 17);
        Assert.Contains(TalkFrom(game, "cynthia", Direction.Left).Transcript, l => l.Text.Contains("been the one to battle it"));

        // Coming to Sunyshore City sends the assistant off, and the professor is at the head of his hall again
        ThroughTheGate(game);
        Assert.Equal(4, game.Story.Var(LabState));
        Assert.True(game.Story.Has("FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART"));
        Assert.Empty(IntoTheLab(game).Transcript);
        Assert.Null(game.Present("counterpart"));
        Assert.Equal((7, 5, Direction.Down), (game.Present("prof_rowan")!.GridX, game.Present("prof_rowan")!.GridY, game.Present("prof_rowan")!.Facing));
        Assert.DoesNotContain(game.Talk("prof_rowan").Transcript, l => l.Text.Contains("Cynthia"));
    }

    [Theory]
    [InlineData(0x3F, "6, I see")]
    [InlineData(0xFF, "All eight!")]
    public void TheProfessorCountsWhateverBadgesThePlayerHas(int badges, string said)
    {
        var game = NewGame();
        game.Story.SetBadges("Sinnoh", badges);
        OutOfTheDistortionWorld(game);
        Assert.Contains(IntoTheLab(game).Transcript, l => l.Text.Contains(said));
        Assert.Equal(3, game.Story.Var(LabState));
    }

    [Fact]
    public void MomStopsAskingAboutTheTremorOnceTheDistortionWorldIsBehindThePlayer()
    {
        var game = NewGame();
        Run(game, "PlayerHouse", "setvar VAR_PLAYER_HOUSE_STATE 6\nsetflag FLAG_HAS_POKEDEX\nsetflag FLAG_RECEIVED_PARCEL\nsetvar VAR_CANALAVE_LIBRARY_STATE 2");
        game.Arrive("PlayerHouse", 6, 10);
        Assert.Contains(game.Talk("mom").Transcript, l => l.Text.Contains("whole house shook"));
        Run(game, "PlayerHouse", "setvar VAR_EXITED_DISTORTION_WORLD_STATE 1");
        Assert.Contains(game.Talk("mom").Transcript, l => l.Text.Contains("whole house shook"));
        // Sendoff Spring ends it (the original's VAR_EXITED_DISTORTION_WORLD_STATE 2), and she still rests the team
        OutOfTheDistortionWorld(game);
        game.Party.Members[0].CurrentHP = 1;
        var rest = game.Talk("mom");
        Assert.DoesNotContain(rest.Transcript, l => l.Text.Contains("shook"));
        Assert.NotEmpty(rest.Transcript);
        Assert.Equal(game.Party.Members[0].MaxHP, game.Party.Members[0].CurrentHP);
    }

    // ------------------------------------------------------------------ the three Pokémon of the lakes

    [Fact]
    public void TheCavernsAreComeToOverTheWaterOfTheLakes()
    {
        var game = AfterTheLab();
        foreach (var (lake, cavern) in new[] { ("LakeVerityGalactic", "VerityCavern"), ("LakeValor", "ValorCavern"), ("LakeAcuityGalactic", "AcuityCavern") })
        {
            // The mouth is on the island in the middle, and leads in to the foot of the cavern; the cavern leads back out
            game.Arrive(lake, 32, 33);
            var mouth = game.Map.GetWarpAt(32, 32)!;
            Assert.Equal((cavern, 14, 28), (mouth.TargetMap, mouth.TargetX, mouth.TargetY));
            game.Arrive(cavern, 14, 28);
            var way = game.Map.GetWarpAt(14, 29)!;
            Assert.Equal((lake, 32, 33), (way.TargetMap, way.TargetX, way.TargetY));
            Assert.False(game.Map.IsDark);
            // Its battles are fought on the cave's stage, wherever in it they start: the wet floor is no cave floor to the field
            Assert.All(new[] { (14, 28), (12, 14), (16, 15), (14, 11), (5, 12) }, t => Assert.Equal(BattleArena.Cave, game.Map.ArenaAt(t.Item1, t.Item2)));
        }
        // The lakefronts lead to the lakes with the caverns in them: Lake Valor is full again
        game.Arrive("Sinnoh", 718, 760);
        Assert.Equal("LakeValor", game.Map.GetWarpAt(717, 760)!.TargetMap);
        game.Arrive("Sinnoh", 80, 844);
        Assert.Equal("LakeVerityGalactic", game.Map.GetWarpAt(80, 843)!.TargetMap);
        game.Arrive("Sinnoh", 308, 230);
        Assert.Equal("LakeAcuityGalactic", game.Map.GetWarpAt(308, 229)!.TargetMap);
    }

    [Theory]
    [InlineData(Direction.Up, 12, 15)]
    [InlineData(Direction.Down, 12, 13)]
    [InlineData(Direction.Left, 13, 14)]
    [InlineData(Direction.Right, 11, 14)]
    public void MespritFliesOffToRoamAndTheProfessorComesInAfterIt(Direction facing, int rowanX, int rowanY)
    {
        var game = AfterTheLab();
        game.Arrive("VerityCavern", 14, 28);
        var mesprit = game.Present("mesprit")!;
        Assert.Equal((12, 13, "Mesprit", true), (mesprit.GridX, mesprit.GridY, mesprit.Species, mesprit.IsPokemon));

        var scene = TalkFrom(game, "mesprit", facing);
        var here = game.Tile;
        // Nobody battles it: it cries, is gone, and roams from now on
        Assert.Contains("cry Mesprit", scene.Log);
        Assert.DoesNotContain(scene.Log, l => l.StartsWith("wildbattle") || l.StartsWith("battle"));
        Assert.Contains(scene.Transcript, l => l.Speaker == null && l.Text.Contains("flew off"));
        Assert.True(game.Story.Has("FLAG_HIDE_VERITY_CAVERN_MESPRIT"));
        Assert.Null(game.Present("mesprit"));
        var roamer = scene.Encounters.Roamers[Roamers.SlotOf("Mesprit")!.Value];
        Assert.Equal((true, "Mesprit", 50), (roamer.Active, roamer.Species, roamer.Level));
        Assert.Contains(Roamers.PlaceOf(roamer), Roamers.Routes);
        Assert.Equal(Roamers.Roaming, game.Story.Var(Mesprit));

        // The professor came up the cave to the player's side, said his piece, and has gone again
        Assert.Equal(here, scene.PlayerTile);
        Assert.Equal(Direction.Down, scene.PlayerFacing);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("that was Mesprit"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("nothing whatever about evolution"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Marking Map on your Pokétch"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("never picked up the Marking Map"));
        var rowan = game.Map.Everyone.Single(n => n.Key == "prof_rowan");
        Assert.Equal((rowanX, rowanY + 7), (rowan.GridX, rowan.GridY));
        Assert.False(game.Map.IsSolid(rowanX, rowanY));
        Assert.True(game.Story.Has("FLAG_HIDE_VERITY_CAVERN_PROF_OAK"));
        Assert.Null(game.Present("prof_rowan"));

        // The cavern is empty afterwards
        game.Arrive("VerityCavern", 14, 28);
        Assert.Null(game.Present("mesprit"));
        Assert.Null(game.Present("prof_rowan"));
    }

    [Fact]
    public void TheProfessorSaysNothingOfTheMarkingMapToSomeoneWhoHasIt()
    {
        var game = AfterTheLab();
        game.Arrive("VerityCavern", 14, 28);
        game.Tile = (12, 14);
        game.Facing = Direction.Up;
        var scene = Run(game, "verity_cavern", "poketch on\npoketchapp MarkingMap\ncall Mesprit");
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Marking Map on your Pokétch"));
        Assert.DoesNotContain(scene.Transcript, l => l.Text.Contains("never picked up the Marking Map"));
    }

    [Theory]
    [InlineData("ValorCavern", "azelf", "Azelf", BattleOutcome.Won)]
    [InlineData("ValorCavern", "azelf", "Azelf", BattleOutcome.Fled)]
    [InlineData("ValorCavern", "azelf", "Azelf", BattleOutcome.Caught)]
    [InlineData("AcuityCavern", "uxie", "Uxie", BattleOutcome.Won)]
    [InlineData("AcuityCavern", "uxie", "Uxie", BattleOutcome.Fled)]
    [InlineData("AcuityCavern", "uxie", "Uxie", BattleOutcome.Caught)]
    public void AzelfAndUxieBattleInTheirCavernsAndAreGoneWhateverTheBattleComesTo(string cavern, string key, string species, BattleOutcome outcome)
    {
        var game = AfterTheLab(outcome);
        game.Arrive(cavern, 14, 28);
        var guardian = game.Present(key)!;
        Assert.Equal((species, true), (guardian.Species, guardian.IsPokemon));
        // It is walked up to from every side
        foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
        {
            var (dx, dy) = FieldMovement.Delta(way);
            Assert.False(game.Map.IsSolid(guardian.GridX - dx, guardian.GridY - dy));
        }

        var battle = TalkFrom(game, key, Direction.Up);
        Assert.Contains($"cry {species}", battle.Log);
        Assert.Contains($"wildbattle {species} 50 {outcome}", battle.Log);
        string hidden = $"FLAG_HIDE_{cavern[..^6].ToUpperInvariant()}_CAVERN_{species.ToUpperInvariant()}", caught = $"FLAG_CAUGHT_{species.ToUpperInvariant()}";
        Assert.True(game.Story.Has(hidden));
        Assert.Null(game.Present(key));
        if (outcome == BattleOutcome.Caught)
        {
            Assert.True(game.Story.Has(caught));
            Assert.Equal((species, 50), (game.Party.Members[^1].Species.Name, game.Party.Members[^1].Level));
            Assert.DoesNotContain(battle.Transcript, l => l.Text.Contains("slipped away"));
        }
        else
        {
            Assert.False(game.Story.Has(caught));
            Assert.Contains(battle.Transcript, l => l.Speaker == null && l.Text.Contains($"{species} slipped away"));
        }
        game.Arrive(cavern, 14, 28);
        Assert.Null(game.Present(key));

        // The Hall of Fame brings it back unless it was caught, as the original's does
        game.Arrive("PokemonLeagueHallOfFame", 8, 15);
        Assert.Equal(outcome == BattleOutcome.Caught, game.Story.Has(hidden));
        game.Arrive(cavern, 14, 28);
        Assert.Equal(outcome != BattleOutcome.Caught, game.Present(key) != null);
    }

    [Theory]
    [InlineData("Azelf", "Lake Valor", "Valor Cavern")]
    [InlineData("Uxie", "Lake Acuity", "Acuity Cavern")]
    public void ThePokedexSaysWhereTheGuardiansOfTheCavernsWait(string species, string lake, string cavern)
    {
        var place = Assert.Single(Habitats.Sinnoh!.Of(species));
        Assert.Equal((lake, cavern), (place.Name, place.How));
        Assert.True(place.Ways.HasFlag(HabitatWays.Special));
        // Mesprit roams: it has no place of its own
        Assert.Empty(Habitats.Sinnoh!.Of("Mesprit"));
    }

    [Theory]
    [InlineData("ValorCavern", "azelf")]
    [InlineData("AcuityCavern", "uxie")]
    public void ALostBattleLeavesTheGuardianWaiting(string cavern, string key)
    {
        var game = AfterTheLab(BattleOutcome.Lost);
        game.Arrive(cavern, 14, 28);
        var battle = TalkFrom(game, key, Direction.Up);
        Assert.Contains(battle.Log, l => l.StartsWith("wildbattle") && l.EndsWith("Lost"));
        Assert.Empty(battle.Transcript.Where(l => l.Text.Contains("slipped away")));
        game.Arrive(cavern, 14, 28);
        Assert.NotNull(game.Present(key));
        Assert.DoesNotContain(game.Story.Flags, f => f.StartsWith("FLAG_CAUGHT_", StringComparison.Ordinal));
    }

    [Fact]
    public void AMespritKnockedOutIsSetLooseAgainAfterTheHallOfFameAndOneCaughtNeverIs()
    {
        var game = AfterTheLab();
        game.Arrive("VerityCavern", 14, 28);
        TalkFrom(game, "mesprit", Direction.Up);

        // Met on a route and knocked out (Roamers.AfterBattle sets the state): it roams no more, and the cavern is empty
        Run(game, "verity_cavern", $"setvar {Mesprit} {Roamers.Defeated}");
        game.Arrive("VerityCavern", 14, 28);
        Assert.Null(game.Present("mesprit"));
        // The Hall of Fame puts it back in its cavern, to be set loose again
        game.Arrive("PokemonLeagueHallOfFame", 8, 15);
        Assert.Equal(Roamers.Reset, game.Story.Var(Mesprit));
        Assert.False(game.Story.Has("FLAG_HIDE_VERITY_CAVERN_MESPRIT"));
        game.Arrive("VerityCavern", 14, 28);
        var again = TalkFrom(game, "mesprit", Direction.Up);
        Assert.True(again.Encounters.Roamers[0].Active);
        Assert.Contains(again.Transcript, l => l.Text.Contains("flew off"));
        // The professor doesn't come a second time
        Assert.DoesNotContain(again.Transcript, l => l.Speaker == "Prof. Rowan");
        Assert.Equal(Roamers.Roaming, game.Story.Var(Mesprit));
        Assert.True(game.Story.Has("FLAG_HIDE_VERITY_CAVERN_MESPRIT"));
        Assert.True(game.Story.Has("FLAG_HIDE_VERITY_CAVERN_PROF_OAK"));

        // Caught, it is the player's for good: the Hall of Fame leaves its cavern empty
        Run(game, "verity_cavern", $"setvar {Mesprit} {Roamers.Captured}");
        game.Arrive("PokemonLeagueHallOfFame", 8, 15);
        Assert.Equal(Roamers.Captured, game.Story.Var(Mesprit));
        Assert.True(game.Story.Has("FLAG_HIDE_VERITY_CAVERN_MESPRIT"));
    }

    [Fact]
    public void AGuardianAlreadyCaughtOrRoamingIsNotBroughtBackByTheLab()
    {
        var game = NewGame();
        OutOfTheDistortionWorld(game);
        Run(game, "RowanLab", $"setflag FLAG_CAUGHT_AZELF\nsetvar {Mesprit} {Roamers.Captured}");
        IntoTheLab(game);
        Assert.True(game.Story.Has("FLAG_HIDE_VALOR_CAVERN_AZELF"));
        Assert.True(game.Story.Has("FLAG_HIDE_VERITY_CAVERN_MESPRIT"));
        Assert.False(game.Story.Has("FLAG_HIDE_ACUITY_CAVERN_UXIE"));
    }

    // ------------------------------------------------------------------ Route 222

    [Fact]
    public void TheRichBoyOfRoute222GivesFlingOnce()
    {
        var game = AfterTheLab();
        game.Arrive("Sinnoh", 760, 790);
        var boy = game.Maps["Sinnoh"].NPCs.Single(n => n.Key == "rich_boy" && n.ScriptFile == "route_222");
        game.Tile = (boy.GridX, boy.GridY + 1);
        var gift = game.Play(Find("RichBoy", "route_222"), boy);
        Assert.Equal(1, game.Bag.GetQuantity(Item("TM56")));
        Assert.True(game.Story.Has("FLAG_RECEIVED_ROUTE_222_TM56"));
        Assert.Contains(gift.Transcript, l => l.Text.Contains("Fling"));
        var again = game.Play(Find("RichBoy", "route_222"), boy);
        Assert.Equal(1, game.Bag.GetQuantity(Item("TM56")));
        Assert.Contains(again.Transcript, l => l.Text.Contains("holding nothing"));
    }

    // ------------------------------------------------------------------ Sunyshore City

    [Theory]
    [InlineData(790)]
    [InlineData(791)]
    public void FlintMeetsWhoeverComesInByTheGateAndGoesToWaitBeforeTheGym(int y)
    {
        var game = AfterTheLab();
        var scene = ThroughTheGate(game, y);
        Assert.Contains("emote Flint Exclaim", scene.Log);
        Assert.All(scene.Transcript, l => Assert.Equal("Flint", l.Speaker));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Elite Four"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Volkner") && l.Text.Contains("rebuild his Gym"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("blacked out"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("riding on you"));
        Assert.Equal((833, y), scene.PlayerTile);
        Assert.Equal(Direction.Right, scene.PlayerFacing);
        Assert.Equal(1, game.Story.Var(CityState));
        // He stands before the Gym's door, which nobody passes while he does
        var flint = game.Present("flint", "sunyshore_city")!;
        Assert.Equal((845, 748, Direction.Down), (flint.GridX, flint.GridY, flint.Facing));
        Assert.False(game.Story.Has("FLAG_HIDE_SUNYSHORE_CITY_FLINT"));

        // It plays once, and he says Volkner isn't back, from wherever he is spoken to
        Assert.Empty(ThroughTheGate(game, y).Transcript);
        Assert.Equal((845, 748), (game.Present("flint", "sunyshore_city")!.GridX, game.Present("flint", "sunyshore_city")!.GridY));
        foreach (var facing in new[] { Direction.Up, Direction.Left, Direction.Right })
        {
            var waiting = TalkFrom(game, "flint", facing, "sunyshore_city");
            Assert.Contains(waiting.Transcript, l => l.Text.Contains("not back yet") && l.Text.Contains("lighthouse"));
            Assert.NotNull(game.Present("flint", "sunyshore_city"));
        }
    }

    [Fact]
    public void SomeoneWhoFirstComesToSunyshoreAnotherWayFindsFlintBeforeTheGym()
    {
        var game = AfterTheLab();
        game.Arrive("Sinnoh", 860, 785);
        Assert.Empty(game.Last.Transcript);
        Assert.Equal(1, game.Story.Var(CityState));
        Assert.Equal((845, 748), (game.Present("flint")!.GridX, game.Present("flint")!.GridY));
    }

    [Fact]
    public void TheLiftCarriesItsRiderUpTheLighthouseAndDownAgain()
    {
        var game = AfterTheLab();
        ThroughTheGate(game);
        UpTheLighthouse(game);
        // Into the car from the city: two steps in, the ride, two steps out, and the gallery, before the car's open door
        var up = game.Played[^2].Host;
        Assert.Equal("VistaLighthouseElevator.OnEnter", game.Played[^2].Script);
        // The ride is the lift's own set piece, with the original's four rounds of the car's lights (plan 09 · L12)
        Assert.Contains("anim elevator up 4", up.Log);
        Assert.Contains("warp VistaLighthouse 6 10", up.Log);
        Assert.Equal(("VistaLighthouse", (6, 10)), (game.Map.Name, game.Tile));
        Assert.Equal(1, game.Story.Var("VAR_VISTA_LIGHTHOUSE_FLOOR"));

        // Back into the car from the gallery: down to the city, before the lighthouse's door
        var car = game.Map.GetWarpAt(6, 9)!;
        Assert.Equal(("VistaLighthouseElevator", 3, 6), (car.TargetMap, car.TargetX, car.TargetY));
        game.Arrive(car.TargetMap, car.TargetX, car.TargetY);
        Assert.Equal(("Sinnoh", (886, 791)), (game.Map.Name, game.Tile));
        Assert.Equal(0, game.Story.Var("VAR_VISTA_LIGHTHOUSE_FLOOR"));
        Assert.False(game.Map.IsSolid(886, 791));
        // And up again: the car goes by where its rider came from, every time
        UpTheLighthouse(game);
        Assert.Equal("VistaLighthouse", game.Map.Name);
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    public void VolknerSizesThePlayerUpAtTheTopOfTheLighthouseAndGoesBackToHisGym(Direction facing)
    {
        var game = AfterTheLab();
        ThroughTheGate(game);
        UpTheLighthouse(game);
        // He stands before the binoculars, which nobody reaches past him
        Assert.Equal("Binoculars", game.Map.SignScripts[(6, 3)]);
        Assert.True(game.Map.IsSolid(6, 3));
        Assert.All(new[] { (5, 3), (7, 3) }, t => Assert.False(game.Map.IsSolid(t.Item1, t.Item2)));
        var scene = TalkFrom(game, "volkner", facing);
        Assert.All(scene.Transcript, l => Assert.Equal("Volkner", l.Speaker));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("challenge the Pokémon League myself"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("strongest Gym Leader in Sinnoh"));
        // He walked round to the lift and is gone, for good
        var volkner = game.Map.Everyone.Single(n => n.Key == "volkner");
        Assert.Equal((6, 9), (volkner.GridX, volkner.GridY));
        Assert.Null(game.Present("volkner"));
        Assert.True(game.Story.Has("FLAG_HIDE_VISTA_LIGHTHOUSE_VOLKNER"));
        Assert.True(game.Story.Has("FLAG_VOLKNER_RETURNED_TO_GYM"));
        Assert.Equal(Direction.Right, scene.PlayerFacing);

        // The binoculars are free: the Pokémon League across the sea
        game.Tile = (6, 4);
        game.Facing = Direction.Up;
        var view = game.Play(Find("Binoculars", "VistaLighthouse"));
        Assert.Contains(view.Transcript, l => l.Text.Contains("looked through the binoculars"));
        Assert.Contains(view.Transcript, l => l.Text.Contains("Pokémon League"));
        // The two visitors have their say
        Assert.Contains(game.Talk("sailor").Transcript, l => l.Text.Contains("safe home"));
        Assert.Contains(game.Talk("youngster").Transcript, l => l.Text.Contains("binoculars"));
    }

    [Theory]
    [InlineData(Direction.Up, 854, 750)]
    [InlineData(Direction.Left, 854, 750)]
    [InlineData(Direction.Right, 854, 750)]
    public void FlintLeavesTheGymsDoorOnceVolknerIsBack(Direction facing, int endX, int endY)
    {
        var game = AfterTheLab();
        ThroughTheGate(game);
        UpTheLighthouse(game);
        TalkFrom(game, "volkner", Direction.Up);
        game.Arrive("Sinnoh", 886, 791);

        var leaving = TalkFrom(game, "flint", facing, "sunyshore_city");
        Assert.Contains(leaving.Transcript, l => l.Text.Contains("He's back at last"));
        Assert.Contains(leaving.Transcript, l => l.Text.Contains("waiting for you at the Pokémon League"));
        var flint = game.Map.Everyone.Single(n => n.Key == "flint" && n.ScriptFile == "sunyshore_city");
        Assert.Equal((endX, endY), (flint.GridX, flint.GridY));
        Assert.True(game.Story.Has("FLAG_HIDE_SUNYSHORE_CITY_FLINT"));
        Assert.Null(game.Present("flint", "sunyshore_city"));
        Assert.Equal(Direction.Right, leaving.PlayerFacing);
        // The way into the Gym is free
        game.Arrive("Sinnoh", 845, 749);
        Assert.Null(game.Map.NpcIn(845, 748, game.Map.HeightAt(845, 748)));
        Assert.Equal("SunyshoreGym", game.Map.GetWarpAt(845, 747)!.TargetMap);
        Assert.Equal(1, game.Story.Var(CityState));
    }

    [Theory]
    [InlineData(853, 854, 743, 853)]
    [InlineData(854, 855, 743, 854)]
    [InlineData(855, 855, 744, 855)]
    [InlineData(856, 855, 743, 856)]
    [InlineData(857, 856, 743, 857)]
    public void TheRivalComesRunningUpTheBeachAndJasmineGivesWaterfall(int x, int rivalX, int rivalY, int jasmineX)
    {
        var game = AfterTheLab();
        ThroughTheGate(game);
        // The beach waits for the Badge
        game.Arrive("Sinnoh", 855, 745);
        Assert.False(game.Fires("RivalAndJasmine"));
        Assert.Null(game.Present("rival", "sunyshore_city"));
        WinTheBeaconBadge(game);

        game.Arrive("Sinnoh", 855, 745);
        var scene = StepOnto(game, x, 743, "RivalAndJasmine");
        Assert.Contains("emote Jasmine Exclaim", scene.Log);
        Assert.Contains(scene.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Eight Badges?!"));
        Assert.Contains(scene.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("like my dad"));
        Assert.Contains(scene.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Pokémon League"));
        // Nobody battles: he runs off down the beach, for good
        Assert.DoesNotContain(scene.Log, l => l.StartsWith("battle"));
        var rival = game.Map.Everyone.Single(n => n.Key == "rival" && n.ScriptFile == "sunyshore_city");
        Assert.Equal((rivalX, rivalY + 8), (rival.GridX, rival.GridY));
        Assert.True(game.Story.Has("FLAG_HIDE_SUNYSHORE_CITY_RIVAL"));
        Assert.Null(game.Present("rival", "sunyshore_city"));
        // Jasmine came down to the player with HM07 and is back by the water
        Assert.Contains(scene.Transcript, l => l.Speaker == "Jasmine" && l.Text.Contains("friend of yours"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Jasmine" && l.Text.Contains("Waterfall"));
        Assert.Equal(1, game.Bag.GetQuantity(Item("HM07")));
        Assert.True(game.Story.Has("FLAG_RECEIVED_SUNYSHORE_CITY_HM07"));
        var jasmine = game.Present("jasmine", "sunyshore_city")!;
        Assert.Equal((857, 741, Direction.Down), (jasmine.GridX, jasmine.GridY, jasmine.Facing));
        Assert.Equal((x, 743), scene.PlayerTile);
        Assert.Equal(Direction.Up, scene.PlayerFacing);
        Assert.Equal(jasmineX, x);
        Assert.Equal(3, game.Story.Var(CityState));

        // Once only
        Assert.False(game.Fires("RivalAndJasmine"));
        Assert.Contains(TalkFrom(game, "jasmine", Direction.Up, "sunyshore_city").Transcript, l => l.Text.Contains("become stronger"));
        Assert.Equal(1, game.Bag.GetQuantity(Item("HM07")));
    }

    [Fact]
    public void JasmineSpeaksOfTheBadgeUntilItIsWon()
    {
        var game = AfterTheLab();
        ThroughTheGate(game);
        game.Arrive("Sinnoh", 855, 745);
        var before = TalkFrom(game, "jasmine", Direction.Up, "sunyshore_city").Transcript;
        Assert.All(before, l => Assert.Equal("Jasmine", l.Speaker));
        Assert.Contains(before, l => l.Text.Contains("at home in this city"));
        Assert.Contains(before, l => l.Text.Contains("Gym Badge") && l.Text.Contains("turn you back"));
        WinTheBeaconBadge(game);
        game.Arrive("Sinnoh", 855, 745);
        var after = TalkFrom(game, "jasmine", Direction.Up, "sunyshore_city").Transcript;
        Assert.Contains(after, l => l.Text.Contains("at home in this city"));
        Assert.Contains(after, l => l.Text.Contains("become stronger"));
        Assert.DoesNotContain(after, l => l.Text.Contains("turn you back"));
    }

    [Fact]
    public void TheTownsRoomsHaveTheirPeople()
    {
        var game = AfterTheLab();
        game.Arrive("SunyshorePokemonCenter", 8, 12);
        foreach (string key in new[] { "old_man", "ninja_boy", "middle_aged_man" }) Assert.NotNull(game.Present(key));
        Assert.Contains(game.Talk("ninja_boy").Transcript, l => l.Text.Contains("seven Gym Badges"));
        Assert.Contains(game.Talk("middle_aged_man").Transcript, l => l.Text.Contains("solar panels"));
        game.Story.SetBadges("Sinnoh", 0xFF);
        Assert.Contains(game.Talk("ninja_boy").Transcript, l => l.Text.Contains("All eight Gym Badges"));
        game.Arrive("SunyshorePokeMart", 3, 11);
        Assert.Contains(game.Talk("ruin_maniac").Transcript, l => l.Text.Contains("Victory Road"));
        Assert.Contains(game.Talk("sailor").Transcript, l => l.Text.Contains("Seals"));
    }

    // ------------------------------------------------------------------ the whole chapter, and the way through it

    [Fact]
    public void TheChapterPlaysThroughInOrderFromTheLabToHM07()
    {
        var game = NewGame();
        OutOfTheDistortionWorld(game);
        IntoTheLab(game);

        // The lakes, in any order
        game.Arrive("VerityCavern", 14, 28);
        TalkFrom(game, "mesprit", Direction.Up);
        game.Arrive("ValorCavern", 14, 28);
        TalkFrom(game, "azelf", Direction.Up);
        game.Arrive("AcuityCavern", 14, 28);
        TalkFrom(game, "uxie", Direction.Up);

        // East along Route 222 and into the city
        game.Arrive("Sinnoh", 722, 790);
        Assert.False(game.Fires("BlockSunyshore"));
        ThroughTheGate(game);
        UpTheLighthouse(game);
        TalkFrom(game, "volkner", Direction.Up);
        game.Through("VistaLighthouseElevator");
        Assert.Equal("Sinnoh", game.Map.Name);
        TalkFrom(game, "flint", Direction.Up, "sunyshore_city");
        game.Through("SunyshoreGym");
        WinTheBeaconBadge(game);
        game.Arrive("Sinnoh", 855, 745);
        StepOnto(game, 855, 743, "RivalAndJasmine");

        Assert.Equal(3, game.Story.Var(CityState));
        Assert.Equal(4, game.Story.Var(LabState));
        Assert.True(game.Story.HasBadge("Sinnoh", "Beacon"));
        Assert.Equal(1, game.Bag.GetQuantity(Item("HM07")));
        Assert.Equal(1, game.Bag.GetQuantity(Item("TM57")));
        foreach (string flag in new[] { "FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_ACUITY_CAVERN_UXIE", "FLAG_VOLKNER_RETURNED_TO_GYM",
            "FLAG_HIDE_SUNYSHORE_CITY_FLINT", "FLAG_HIDE_SUNYSHORE_CITY_RIVAL", "FLAG_HIDE_VISTA_LIGHTHOUSE_VOLKNER", "FLAG_RECEIVED_SUNYSHORE_CITY_HM07" })
            Assert.True(game.Story.Has(flag), flag);
        // Nothing of the chapter's own was left unplayed on the way
        foreach (string script in new[] { "RowanLab.OnEnter", "verity_cavern.Mesprit", "valor_cavern.Azelf", "acuity_cavern.Uxie", "sunyshore_city.OnEnter",
            "VistaLighthouseElevator.OnEnter", "VistaLighthouse.Volkner", "sunyshore_city.Flint", "SunyshoreGymRoom3.Volkner", "sunyshore_city.RivalAndJasmine" })
            Assert.Contains(game.Played, p => p.Script == script);
    }

    [Fact]
    public void TheChaptersPlacesAreReachedOnFootAndByTheLift()
    {
        var game = NewGame();
        Map Named(string name) => game.Maps[name];
        var reached = WorldWalk.From(Named, new MapSpot("Sinnoh", 722, 790, Direction.Right));
        // Along Route 222 into Sunyshore City, to the Gym's door, the beach and the lighthouse's door
        foreach (var (x, y) in new[] { (833, 790), (845, 749), (855, 743), (886, 791), (860, 785) }) Assert.Contains((x, y), reached[Named("Sinnoh")]);
        // Up the lift to the gallery, all round its shaft, and before the binoculars
        Assert.Contains((3, 4), reached[Named("VistaLighthouseElevator")]);
        foreach (var (x, y) in new[] { (6, 10), (6, 5), (2, 7), (10, 7), (5, 3), (9, 4) }) Assert.Contains((x, y), reached[Named("VistaLighthouse")]);
        // The three caverns over their lakes, as far as where each guardian stands
        foreach (var (cavern, x, y) in new[] { ("VerityCavern", 12, 14), ("ValorCavern", 16, 15), ("AcuityCavern", 14, 11) })
            Assert.Contains((x, y), reached[Named(cavern)]);
        // And on north by sea, as far as Victory Road's foot (Waterfall and the Badge are what let the player up)
        Assert.True(reached.ContainsKey(Named("VictoryRoad1F")));
    }

    // ------------------------------------------------------------------ saves from before

    [Fact]
    public void ASaveFromBeforeTheChapterHidesItsPeopleAndKeepsWhatItHasDone()
    {
        Assert.True(StoryState.CurrentVersion >= 16);
        string[] always = { "FLAG_HIDE_VERITY_CAVERN_PROF_OAK", "FLAG_HIDE_SUNYSHORE_CITY_RIVAL" };
        string[] past = { "FLAG_HIDE_VALOR_LAKEFRONT_COLLECTOR", StoryMigration.VolknerReturnedFlag, "FLAG_HIDE_VISTA_LIGHTHOUSE_VOLKNER", "FLAG_HIDE_SUNYSHORE_CITY_FLINT" };

        // Never been east of Valor Lakefront: the chapter waits whole
        var before = new StoryState();
        StoryMigration.Upgrade(before, 15, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always) Assert.True(before.Has(flag), flag);
        foreach (string flag in past) Assert.False(before.Has(flag), flag);
        Assert.Equal(0, before.Var(CityState));
        Assert.Equal(0, before.Var("VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE"));

        // Past Sunyshore's gate when nobody stood at it: Flint has said his piece and gone, Volkner is in his Gym, and
        // the road back to Valor Lakefront is open
        var visited = new StoryState();
        visited.Set(StoryMigration.ArrivedInSunyshoreFlag);
        StoryMigration.Upgrade(visited, 15, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always.Concat(past)) Assert.True(visited.Has(flag), flag);
        Assert.Equal(1, visited.Var(CityState));
        Assert.Equal(1, visited.Var("VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE"));

        // The Beacon Badge already won, by the Gym's script (which leaves the state at 2) or by an older Gym's (which
        // left none): the rival's scene on the beach waits, where Jasmine gives HM07
        foreach (int state in new[] { 0, 2 })
        {
            var champion = new StoryState();
            champion.GiveBadge("Sinnoh", "Beacon");
            champion.SetVar(CityState, state);
            StoryMigration.Upgrade(champion, 15, Array.Empty<Pokemon>(), Scripts, new Inventory());
            foreach (string flag in always.Concat(past)) Assert.True(champion.Has(flag), flag);
            Assert.Equal(2, champion.Var(CityState));
            Assert.False(champion.Has("FLAG_RECEIVED_SUNYSHORE_CITY_HM07"));
        }

        // A guardian that is the player's already counts as caught, and one knocked out as a roamer stays so
        var owner = new StoryState();
        StoryMigration.Upgrade(owner, 15, new[] { new Pokemon(PokemonDatabase.Get("Azelf")!, 50), new Pokemon(PokemonDatabase.Get("Mesprit")!, 50) }, Scripts, new Inventory());
        Assert.True(owner.Has("FLAG_CAUGHT_AZELF"));
        Assert.False(owner.Has("FLAG_CAUGHT_UXIE"));
        Assert.Equal(Roamers.Captured, owner.Var(Mesprit));
        var beaten = new StoryState();
        beaten.SetVar(Mesprit, Roamers.Defeated);
        StoryMigration.Upgrade(beaten, 15, new[] { new Pokemon(PokemonDatabase.Get("Uxie")!, 50) }, Scripts, new Inventory());
        Assert.True(beaten.Has("FLAG_CAUGHT_UXIE"));
        Assert.Equal(Roamers.Defeated, beaten.Var(Mesprit));

        // From any older version the same holds, through the chapters' own scripts
        var old = new StoryState();
        old.Set(StoryMigration.ArrivedInSunyshoreFlag);
        StoryMigration.Upgrade(old, 9, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always.Concat(past)) Assert.True(old.Has(flag), flag);

        // A save of today is left as it is
        var today = new StoryState();
        today.Set(StoryMigration.ArrivedInSunyshoreFlag);
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.Equal(new[] { StoryMigration.ArrivedInSunyshoreFlag }, today.Flags);
    }

    [Fact]
    public void ASaveThatCaughtAGuardianBeforeFindsItsCavernEmptyAfterTheLab()
    {
        var game = NewGame();
        StoryMigration.Upgrade(game.Story, 15, new[] { new Pokemon(PokemonDatabase.Get("Uxie")!, 50) }, Scripts, game.Bag);
        OutOfTheDistortionWorld(game);
        IntoTheLab(game);
        game.Arrive("AcuityCavern", 14, 28);
        Assert.Null(game.Present("uxie"));
        game.Arrive("ValorCavern", 14, 28);
        Assert.NotNull(game.Present("azelf"));
        // Nor does the Hall of Fame bring it back
        game.Arrive("PokemonLeagueHallOfFame", 8, 15);
        game.Arrive("AcuityCavern", 14, 28);
        Assert.Null(game.Present("uxie"));
    }

    [Fact]
    public void ASavePastSunyshoreWalksIntoItsGymAndFindsTheBeachWaitingOnceItHasTheBadge()
    {
        var game = NewGame();
        game.Story.Set(StoryMigration.ArrivedInSunyshoreFlag);
        StoryMigration.Upgrade(game.Story, 15, game.Party.Members, Scripts, game.Bag);
        // Nobody at the gate, nobody before the Gym, nobody at the top of the lighthouse
        Assert.Empty(ThroughTheGate(game).Transcript);
        Assert.Null(game.Present("flint", "sunyshore_city"));
        UpTheLighthouse(game);
        Assert.Null(game.Present("volkner"));
        // The Badge, and the beach
        WinTheBeaconBadge(game);
        game.Arrive("Sinnoh", 855, 745);
        StepOnto(game, 854, 743, "RivalAndJasmine");
        Assert.Equal(1, game.Bag.GetQuantity(Item("HM07")));
    }

    [Fact]
    public void ANewGameStartsWithTheChapterAsAnOlderSaveIsGivenIt()
    {
        var fresh = new StoryState();
        StoryMigration.BeginNewGame(fresh, Scripts);
        var upgraded = new StoryState();
        StoryMigration.Upgrade(upgraded, 0, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in fresh.Flags.Where(f => f.Contains("CAVERN") || f.Contains("SUNYSHORE") || f.Contains("VISTA")))
            Assert.True(upgraded.Has(flag), flag);
        Assert.Equal(fresh.Var(CityState), upgraded.Var(CityState));
        // The chapter's script is a link of the chain a new game runs (the eleventh chapter's follows it, plan 02 ·
        // S14), and may only set things up
        Assert.Contains(Find("ChapterNine", ScriptLibrary.Common).Everything(), i => i.Op == Op.Call && i.Name == "ChapterTen");
        Assert.All(Find("ChapterTen", ScriptLibrary.Common).Everything(), i => Assert.Contains(i.Op, new[] { Op.SetFlag, Op.ClearFlag, Op.SetVar, Op.If, Op.Call, Op.Return }));
    }

    // ------------------------------------------------------------------ the lighthouse's rooms and the lift's pieces

    [Fact]
    public void TheGalleryIsTheOriginalsRingRoundItsShaftWithTheCarOpenInIt()
    {
        var gallery = Room("VistaLighthouse");
        Assert.Equal((13, 13, (1, 3)), (gallery.Width, gallery.Height, gallery.RoomCorner()));
        // The original's grid (events_vista_lighthouse, land m_lhouse01): a ring of floor three tiles wide round a
        // shaft five by four, the corners cut off, and the binoculars' tile in the middle of the back row
        string[] plan =
        {
            "#############", "#############", "#############", "##....#....##", "##.........##", "#...........#",
            "#...#####...#", "#...#####...#", "#...#####...#", "#...##.##...#", "#...........#", "##.........##", "#############"
        };
        for (int y = 0; y < plan.Length; y++)
            for (int x = 0; x < plan[y].Length; x++)
                Assert.True(gallery.IsSolid(x, y) == (plan[y][x] == '#'), $"{x},{y}");
        // No door in its walls: the car in the shaft's south face is the only way out, and it leads into the lift
        Assert.DoesNotContain(Enumerable.Range(0, gallery.Width), x => gallery.GetGroundTile(x, gallery.Height - 1) == TileType.Door);
        var car = Assert.Single(gallery.Warps);
        Assert.Equal((6, 9, "VistaLighthouseElevator", 3, 6, Direction.Up), (car.SourceX, car.SourceY, car.TargetMap, car.TargetX, car.TargetY, car.TargetFacing));
        Assert.Single(gallery.Props, p => p.Type == PropType.LiftCar && (p.X, p.Y) == (6, 9));
        Assert.Single(gallery.Props, p => p.Type == PropType.Binoculars && (p.X, p.Y) == (6, 3));
        Assert.Equal(3, gallery.Props.Count(p => p.Type == PropType.Window && p.Y == 2));
        Assert.Equal(InteriorStyle.Lab, gallery.Interior);

        // The lift's car: a small room whose door leads out to the foot of the lighthouse in Sunyshore City
        var lift = Room("VistaLighthouseElevator");
        Assert.Equal((7, 8, (1, 3)), (lift.Width, lift.Height, lift.RoomCorner()));
        var door = Assert.Single(lift.Warps);
        Assert.Equal((3, 7, "Sinnoh", 886, 791, Direction.Down), (door.SourceX, door.SourceY, door.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        Assert.Single(lift.Props, p => p.Type == PropType.LiftIndicator);
        // Its ride needs two free tiles ahead of whoever comes in
        Assert.All(new[] { (3, 6), (3, 5), (3, 4) }, t => Assert.True(lift.IsWalkable(t.Item1, t.Item2)));
        // Both ends say where they are, so the car knows which way to go
        Assert.Contains(Find("OnEnter", "VistaLighthouse").Everything(), i => i.Op == Op.SetVar && i.Name == "VAR_VISTA_LIGHTHOUSE_FLOOR" && i.Number == 1);
        Assert.Contains(Find("OnEnter", "sunyshore_city").Everything(), i => i.Op == Op.SetVar && i.Name == "VAR_VISTA_LIGHTHOUSE_FLOOR" && i.Number == 0);
    }

    [Fact]
    public void ALiftsCarIsAPlateBetweenTwoJambsAndBlocksNothing()
    {
        Assert.False(new Prop { Type = PropType.LiftCar }.IsSolid);
        Assert.False(new Prop { Type = PropType.LiftIndicator }.IsSolid);
        var gallery = Room("VistaLighthouse");
        var kit = new KitBuilder(new ArtSheet(), 1.9f);
        PropModels.Build(kit, gallery.Props.Single(p => p.Type == PropType.LiftCar), gallery);
        // The plate lies on the floor of its own tile, and the jambs stand as high as an inner wall is cut
        var (floorMin, floorMax) = kit.Flat.Bounds();
        Assert.InRange(floorMin.X, 6f, 6.2f);
        Assert.InRange(floorMax.X, 6.8f, 7f);
        Assert.InRange(floorMax.Z, 9.9f, 10.01f);
        var (min, max) = kit.Solid.Bounds();
        Assert.InRange(min.X, 5.99f, 6.01f);
        Assert.InRange(max.X, 6.99f, 7.01f);
        Assert.Equal(PropModels.InnerWallHeight / 32f * 1.9f, max.Y, 3);
        Assert.True(min.Z >= 9.8f);
    }
}
