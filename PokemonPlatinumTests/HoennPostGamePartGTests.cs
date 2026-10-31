using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The post-game of Hoenn, part g (plan 21 · H12; docs/mechanics/rulings.md, "Faraway Island, the S.S. Tidal, the
/// Gym Leaders' rematches and the Tower's shields"): the Old Sea Map for three Frontier prints and Faraway Island with
/// Mew stepping away in the grass, the S.S. Tidal between Slateport and Lilycove, the seven other Gym Leaders' rematches
/// over Match Call, and Scott's two shields. Each scene starts as the game starts it, on maps of this class's own.
/// </summary>
public class HoennPostGamePartGTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

    private static readonly string[] Places =
        { "FarawayIsland_Entrance", "FarawayIsland_Interior", "SSTidalCorridor", "SSTidalLowerDeck", "SSTidalRooms" };

    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public HeadlessScriptHost Last { get; private set; } = null!;

        public Game(string map, int x, int y)
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Story.Set("HoennHallOfFame");
            Story.SetBadges("Hoenn", 0xFF);
            Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 70, new Random(1)));
            Party.Add(new Pokemon(PokemonDatabase.Get("Manectric")!, 70, new Random(2)));
            Arrive(map, x, y);
        }

        public Map MapOf(string name) => maps[name];

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Map.ApplyDoors(Story.Has);
            Wandering.SendHome(Map);
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key, params int[] answers)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc, answers);
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null, params int[] answers)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => Fight,
                NeedsPokemon = true
            };
            foreach (int answer in answers) host.Answers.Enqueue(answer);
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct())
            {
                map.ApplyPresence(Story.Has);
                map.ApplyDoors(Story.Has);
            }
            Tile = host.PlayerTile;
            if (host.Map is { } there && there != Map) Arrive(there.Name, host.PlayerTile.X, host.PlayerTile.Y);
            return Last = host;
        }
    }

    [Fact]
    public void FarawayIslandAndTheSSTidalAreHoennsAndJoined()
    {
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        foreach (string name in Places) Assert.Contains(name, hoenn.Maps);
        var built = Hoenn.Value.BuildMaps().ToDictionary(m => m.Name);
        foreach (string name in Places) Assert.True(built.ContainsKey(name), name);
        // The shore the ferry sets the player down on, and the corridor's gangway, are open
        Assert.True(built["FarawayIsland_Entrance"].IsWalkable(13, 38));
        Assert.True(built["SSTidalCorridor"].IsWalkable(1, 10));
        Assert.Equal("FarawayIsland_Interior", built["FarawayIsland_Entrance"].GetWarpAt(22, 7)!.TargetMap);
        // The cabins and the hold lead off the corridor and back
        Assert.Equal("SSTidalRooms", built["SSTidalCorridor"].GetWarpAt(4, 9)!.TargetMap);
        Assert.Equal("SSTidalLowerDeck", built["SSTidalCorridor"].GetWarpAt(16, 2)!.TargetMap);
        Assert.Equal("SSTidalCorridor", built["SSTidalLowerDeck"].GetWarpAt(15, 2)!.TargetMap);
        // Mew plays in tall grass
        var interior = built["FarawayIsland_Interior"];
        var mew = interior.Everyone.First(n => n.Key == "faraway_island_mew");
        Assert.Equal(PersonMoves.Mirror, mew.Movement!.Kind);
        // ...three steps north of the area file's tile, where Emerald's arrival walks it, and kept round it
        Assert.Equal((13, 14), (mew.GridX, mew.GridY));
        Assert.True(interior.IsTallGrass(13, 14));
        Assert.Equal((13, 14), (mew.Movement.HomeX, mew.Movement.HomeY));
    }

    [Fact]
    public void TheMirrorMovementStepsTheOtherWayAndOnlyOntoGrass()
    {
        Assert.Equal(PersonMoves.Mirror, PersonMovement.Parse("copy_player_opposite_in_grass", 15, 15, 0, 0, Direction.Down)!.Kind);
        var map = new Map(20, 20);
        for (int x = 0; x < 20; x++)
            for (int y = 0; y < 20; y++)
                if (x >= 5 && x <= 12) map.SetGroundTile(x, y, TileType.TallGrass);
        var mew = new NPC { Name = "Mew", GridX = 8, GridY = 10, Facing = Direction.Down };
        mew.Movement = PersonMovement.Parse("copy_player_opposite_in_grass", 15, 15, 8, 10, Direction.Down);
        map.NPCs.Add(mew);
        var wandering = new Wandering();
        var rng = new Random(3);
        void Frames((int, int) player, int count = 40)
        {
            for (int i = 0; i < count; i++) wandering.Update(1f / 30f, map, player, player, rng);
        }
        // Standing still, it stays where it is
        Frames((15, 10));
        Assert.Equal((8, 10), (mew.GridX, mew.GridY));
        // The player steps west: Mew steps east
        Frames((14, 10));
        Assert.Equal((9, 10), (mew.GridX, mew.GridY));
        Assert.Equal(Direction.Right, mew.Facing);
        // ...and north for a step south
        Frames((14, 11));
        Assert.Equal((9, 9), (mew.GridX, mew.GridY));
        // At the grass's eastern edge it can go no further east
        foreach (int x in new[] { 13, 12, 11, 10 }) Frames((x, 11));
        Assert.Equal((12, 9), (mew.GridX, mew.GridY));
        Frames((9, 11));
        Assert.Equal((12, 9), (mew.GridX, mew.GridY));
        Assert.Equal(Direction.Right, mew.Facing);
    }

    [Fact]
    public void TheOldSeaMapComesWithThreePrintsAndTheFerrySailsToFarawayIsland()
    {
        var game = new Game("LilycoveCity_Harbor", 8, 11);
        game.Story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND");
        game.Story.SetVar("VAR_BATTLE_TOWER_PRINT_STATE", 2);
        game.Story.SetVar("VAR_BATTLE_ARCADE_PRINT_STATE", 2);
        // Two prints: the two tickets and no map, and the choice of five as before (4 is nowhere)
        game.Talk("lilycove_harbor_attendant", 4);
        Assert.True(game.Story.Has("FLAG_ENABLE_SHIP_BIRTH_ISLAND"));
        Assert.False(game.Story.Has("FLAG_ENABLE_SHIP_FARAWAY_ISLAND"));
        Assert.Equal("LilycoveCity_Harbor", game.Map.Name);

        // A third facility's print brings the Old Sea Map, and Faraway Island is the fifth choice
        game.Story.SetVar("VAR_BATTLE_HALL_PRINT_STATE", 4);
        game.Talk("lilycove_harbor_attendant", 4);
        Assert.True(game.Story.Has("FLAG_ENABLE_SHIP_FARAWAY_ISLAND"));
        Assert.Contains(game.Last.Log, l => l.StartsWith("warp FarawayIsland_Entrance 13 38"));
        Assert.Equal("FarawayIsland_Entrance", game.Map.Name);

        // The sailor on the shore sails back; the other places of the longer choice still go where they did
        game.Talk("faraway_island_sailor");
        Assert.Equal("LilycoveCity_Harbor", game.Map.Name);
        game.Talk("lilycove_harbor_attendant", 2);
        Assert.Equal("NavelRock_Harbor", game.Map.Name);
        game.Arrive("LilycoveCity_Harbor", 8, 11);
        game.Talk("lilycove_harbor_attendant", 5);
        Assert.Equal("LilycoveCity_Harbor", game.Map.Name);
    }

    [Fact]
    public void MewIsMetWhenCorneredAndGoneOnceBeatenOrCaught()
    {
        var game = new Game("FarawayIsland_Interior", 12, 18);
        Assert.NotNull(game.Present("faraway_island_mew"));
        // Run from, it is back the next time
        game.Fight = BattleOutcome.Fled;
        game.Talk("faraway_island_mew");
        Assert.Contains("wildbattle Mew 30 Fled", game.Last.Log);
        Assert.Null(game.Present("faraway_island_mew"));
        game.Arrive("FarawayIsland_Interior", 12, 18);
        Assert.NotNull(game.Present("faraway_island_mew"));
        // Beaten, gone for good
        game.Fight = BattleOutcome.Won;
        game.Talk("faraway_island_mew");
        Assert.True(game.Story.Has("FLAG_DEFEATED_MEW"));
        game.Arrive("FarawayIsland_Interior", 12, 18);
        Assert.Null(game.Present("faraway_island_mew"));
        // Caught (in another game), the same
        var other = new Game("FarawayIsland_Interior", 12, 18) { Fight = BattleOutcome.Caught };
        other.Talk("faraway_island_mew");
        Assert.True(other.Story.Has("FLAG_CAUGHT_MEW"));
        other.Arrive("FarawayIsland_Interior", 12, 18);
        Assert.Null(other.Present("faraway_island_mew"));
    }

    [Fact]
    public void TheSSTidalSailsBetweenSlateportAndLilycoveOnARestInTheCabin()
    {
        var game = new Game("SlateportCity_Harbor", 8, 11);
        // No ticket, no voyage
        game.Talk("beauty");
        Assert.Equal("SlateportCity_Harbor", game.Map.Name);

        game.Story.Set("FLAG_RECEIVED_SS_TICKET");
        game.Talk("beauty", 0);
        Assert.Equal("SSTidalCorridor", game.Map.Name);
        Assert.Equal(1, game.Story.Var("VAR_SS_TIDAL_STATE"));
        // At sea, the gangway's sailor lets nobody off
        game.Talk("ss_tidal_exit_sailor");
        Assert.Equal("SSTidalCorridor", game.Map.Name);

        // A rest in the cabin heals the team and brings the ship in at Lilycove
        game.Party.Members[0].CurrentHP = 1;
        game.Arrive("SSTidalRooms", 15, 13);
        game.Play(Scripts.Find("Bed", "ss_tidal_rooms")!, null, 0);
        Assert.Equal(3, game.Story.Var("VAR_SS_TIDAL_STATE"));
        Assert.Equal(game.Party.Members[0].MaxHP, game.Party.Members[0].CurrentHP);
        game.Arrive("SSTidalCorridor", 1, 10);
        game.Talk("ss_tidal_exit_sailor");
        Assert.Equal("LilycoveCity_Harbor", game.Map.Name);
        Assert.Equal(0, game.Story.Var("VAR_SS_TIDAL_STATE"));

        // and back the other way from Lilycove's quay
        game.Talk("sailor", 0);
        Assert.Equal(2, game.Story.Var("VAR_SS_TIDAL_STATE"));
        game.Arrive("SSTidalRooms", 15, 13);
        game.Play(Scripts.Find("Bed", "ss_tidal_rooms")!, null, 0);
        Assert.Equal(4, game.Story.Var("VAR_SS_TIDAL_STATE"));
        game.Arrive("SSTidalCorridor", 1, 10);
        game.Talk("ss_tidal_exit_sailor");
        Assert.Equal("SlateportCity_Harbor", game.Map.Name);
    }

    [Fact]
    public void TheShipsPassengersBattleAsTheirTrainers()
    {
        var built = Hoenn.Value.BuildMaps().ToDictionary(m => m.Name);
        var trainers = built["SSTidalRooms"].Everyone.Concat(built["SSTidalLowerDeck"].Everyone)
            .Where(n => n.IsTrainer).Select(n => n.TrainerData!.Id).ToHashSet();
        foreach (string id in new[] { "hoenn_colton", "hoenn_micah", "hoenn_thomas", "hoenn_lea_and_jed", "hoenn_garret", "hoenn_naomi", "hoenn_phillip", "hoenn_leonard" })
            Assert.Contains(id, trainers);
    }

    public static TheoryData<string, string, string, int, int, string> Leaders => new()
    {
        // contact, the leader's person, the Gym, a tile in it, the room's own setup
        { "roxanne", "roxanne", "RustboroCity_Gym", 5, 3, "" },
        { "brawly", "brawly", "DewfordTown_Gym", 5, 3, "" },
        { "flannery", "flannery", "LavaridgeTown_Gym_1F", 5, 3, "" },
        { "norman", "petalburg_gym_norman", "PetalburgCity_Gym", 4, 3, "VAR_PETALBURG_GYM_STATE" },
        { "winona", "winona", "FortreeCity_Gym", 5, 3, "" },
        { "tate_and_liza", "tate", "MossdeepCity_Gym", 5, 3, "" },
        { "juan", "juan", "SootopolisCity_Gym_1F", 5, 3, "" },
    };

    [Theory]
    [MemberData(nameof(Leaders))]
    public void EveryGymLeaderAsksForARematchOnTheirDayAndGrowsStrongerEachTime(string contactId, string person, string gym, int x, int y, string state)
    {
        var contact = PhoneBook.Get(contactId)!;
        string first = contact.Trainer!;
        string stem = first[..^2];
        Assert.Equal(new[] { stem + "_2", stem + "_3", stem + "_4", stem + "_5" }, TrainerDatabase.Get(first)!.Rematches!);
        var story = new StoryState();
        story.Set("HoennHallOfFame");
        story.Set(Pokegear.ContactFlag(contactId));
        story.Defeat(first);
        var day = new DateTime(2026, 10, 26);
        while (!string.Equals(day.DayOfWeek.ToString(), contact.RematchDay, StringComparison.OrdinalIgnoreCase)) day = day.AddDays(1);
        var time = contact.RematchTime switch { "Morning" => TimeOfDay.Morning, "Night" => TimeOfDay.Night, _ => TimeOfDay.Day };
        Assert.True(PhoneRematch.Wants(contact, story, day, time));
        Assert.False(PhoneRematch.Wants(contact, story, day.AddDays(1), time));
        // Not before the Hall of Fame
        story.Unset("HoennHallOfFame");
        Assert.False(PhoneRematch.Wants(contact, story, day, time));

        var game = new Game(gym, x, y);
        if (state.Length > 0) game.Story.SetVar(state, 7);
        game.Story.Set(Pokegear.ContactFlag(contactId));
        game.Arrive(gym, x, y);
        string flag = PhoneRematch.WaitingFlag(contactId);
        foreach (string team in new[] { stem + "_2", stem + "_3", stem + "_4", stem + "_5", stem + "_5" })
        {
            game.Story.SetVar("VAR_PHONE_CALL", 2);
            game.Play(Scripts.Find(contact.Script, "phone")!);
            Assert.True(game.Story.Has(flag));
            game.Arrive(gym, x, y);
            Assert.NotNull(game.Present(person));
            game.Talk(person);
            Assert.Contains(game.Last.Log, l => l.StartsWith("battle") && l.Contains(team));
            Assert.False(game.Story.Has(flag));
        }
        // With one Pokémon there is no double battle
        game.Story.Set(flag);
        game.Party.Remove(game.Party.Members[1]);
        game.Talk(person);
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("battle"));
        Assert.True(game.Story.Has(flag));
    }

    [Theory]
    [InlineData("roxanne", "roxanne", "RustboroCity_Gym", "")]
    [InlineData("norman", "petalburg_gym_norman", "PetalburgCity_Gym", "VAR_PETALBURG_GYM_STATE")]
    public void RoxanneAndNormanGiveTheirNumbersWhenSpokenToWithTheirBadge(string contactId, string person, string gym, string state)
    {
        var game = new Game(gym, 5, 3);
        if (state.Length > 0) game.Story.SetVar(state, 7);
        game.Story.Set("FLAG_RECEIVED_TM_ROCK_TOMB");
        game.Story.Set("FLAG_RECEIVED_TM_FACADE");
        game.Arrive(gym, 5, 3);
        // No PokéNav, no number
        game.Talk(person);
        Assert.False(game.Story.Has(Pokegear.ContactFlag(contactId)));
        game.Story.Set("FLAG_SYS_POKENAV_GET");
        game.Talk(person);
        Assert.True(game.Story.Has(Pokegear.ContactFlag(contactId)));
        Assert.Contains(PhoneBook.Hoenn, c => c.Id == contactId);
    }

    [Fact]
    public void ScottGivesTheShieldsForTheTowersPrints()
    {
        var game = new Game("BattleFrontier_ScottsHouse", 4, 4);
        game.Story.Set("FLAG_RECEIVED_SCOTTS_BATTLE_POINTS");
        game.Talk("scotts_house_scott", 1);
        Assert.False(game.Story.Has("FLAG_RECEIVED_SILVER_SHIELD"));
        game.Story.SetVar("VAR_BATTLE_TOWER_PRINT_STATE", 2);
        game.Talk("scotts_house_scott", 1);
        Assert.True(game.Story.Has("FLAG_RECEIVED_SILVER_SHIELD"));
        Assert.False(game.Story.Has("FLAG_RECEIVED_GOLD_SHIELD"));
        game.Story.SetVar("VAR_BATTLE_TOWER_PRINT_STATE", 4);
        game.Talk("scotts_house_scott", 1);
        Assert.True(game.Story.Has("FLAG_RECEIVED_GOLD_SHIELD"));
        // Given once each: another visit gives nothing more
        game.Talk("scotts_house_scott", 1);
        Assert.DoesNotContain(game.Last.Transcript, l => l.Text.Contains("Shield"));
    }
}
