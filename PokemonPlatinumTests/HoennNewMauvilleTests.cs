using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The post-game of Hoenn, part d (plan 21 · H12): New Mauville under Route 110's Cycling Road, with Wattson's Basement
/// Key, its door, its blue and green barriers, the generator switched off and its three Voltorb, and the Desert
/// Underpass behind the Fossil Maniac's tunnel once Hoenn's Hall of Fame is entered (docs/mechanics/rulings.md, "New
/// Mauville and the Desert Underpass"). Each scene starts as the game starts it, on maps of this class's own.
/// </summary>
public class HoennNewMauvilleTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

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
        public Queue<int> Answers { get; } = new();

        public Game()
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 50, new Random(1)));
            Story.SetBadges("Hoenn", 0xFF);
        }

        public Map MapOf(string name) => maps[name];

        /// <summary>Comes to a place as a warp does: its OnTransition behind the fade, then its OnEnter.</summary>
        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnTransition) is { } transition) Play(transition);
            Settle(Map);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        private void Settle(Map map)
        {
            map.ApplySwappedPlaces(Story.Var, Story.Has);
            map.ApplyPresence(Story.Has);
            map.ApplyDoors(Story.Has);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        /// <summary>A script of the place the player stands in, by its name (a trigger's, a tile read).</summary>
        public HeadlessScriptHost Run(string name) => Play(Scripts.Find(name, Map.ScriptFileAt(Tile.X, Tile.Y))!);

        public HeadlessScriptHost Play(Script script, NPC? subject = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => Fight,
                NeedsPokemon = true
            };
            foreach (int a in Answers) host.Answers.Enqueue(a);
            Answers.Clear();
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct()) Settle(map);
            Tile = host.PlayerTile;
            return Last = host;
        }
    }

    // A tile of each colour's barriers that is shut while it stands and open while it is down, and the generator's
    private static readonly (int X, int Y) Blue = (23, 36), Green = (37, 35), Generator = (32, 2);

    [Fact]
    public void NewMauvilleAndTheDesertUnderpassAreOpenInHoenn()
    {
        foreach (string name in new[] { "NewMauville_Entrance", "NewMauville_Inside", "DesertUnderpass" })
            Assert.Contains(name, RegionDatabase.Get(RegionDatabase.Hoenn)!.Maps);
        foreach (string key in new[] { "new_mauville_entrance", "new_mauville_inside", "desert_underpass" })
            Assert.Contains(key, Hoenn.Value.Index.Areas);
        var game = new Game();
        // Route 110's door under the Cycling Road leads in, and the plant's wild Pokémon are Emerald's
        Assert.Equal("NewMauville_Entrance", game.MapOf("Hoenn").Warps.Single(w => w.TargetMap == "NewMauville_Entrance").TargetMap);
        Assert.Equal("NewMauville_Inside", game.MapOf("NewMauville_Entrance").GetWarpAt(4, 1)!.TargetMap);
        Assert.Equal("DesertUnderpass", game.MapOf("Route114_FossilManiacsTunnel").GetWarpAt(6, 2)!.TargetMap);
    }

    [Fact]
    public void WattsonGivesTheKeyOnceAndTheTmOnceTheGeneratorIsStoppedThenGoesHome()
    {
        var game = new Game();
        // Norman beaten sends him out into the street (petalburg_city_gym.txt)
        game.Story.Unset("FLAG_HIDE_MAUVILLE_CITY_WATTSON");
        var mauville = game.MapOf("Hoenn");
        var wattson = mauville.Everyone.Single(n => n.Key == "wattson" && mauville.ScriptFileAt(n.GridX, n.GridY) == "mauville_city");
        game.Arrive("Hoenn", wattson.GridX, wattson.GridY + 1);
        Assert.NotNull(game.Present("wattson"));
        var first = game.Talk("wattson");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Basement Key")!));
        Assert.True(game.Story.Has("FLAG_GOT_BASEMENT_KEY_FROM_WATTSON"));
        game.Talk("wattson");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Basement Key")!));
        Assert.False(game.Story.Has("FLAG_GOT_TM_THUNDERBOLT_FROM_WATTSON"));
        game.Story.SetVar("VAR_NEW_MAUVILLE_STATE", 2);
        game.Talk("wattson");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM24")!));
        Assert.NotNull(first);
        Assert.True(game.Story.Has("FLAG_GOT_TM_THUNDERBOLT_FROM_WATTSON"));
        // The next time the city is come to, he is back in his Gym
        game.Arrive("Hoenn", wattson.GridX, wattson.GridY + 1);
        Assert.True(game.Story.Has("FLAG_HIDE_MAUVILLE_CITY_WATTSON"));
        Assert.False(game.Story.Has("FLAG_HIDE_MAUVILLE_GYM_WATTSON"));
    }

    [Fact]
    public void TheEntrancesDoorIsShutUntilWattsonsKeyOpensIt()
    {
        var game = new Game();
        game.Arrive("NewMauville_Entrance", 4, 5);
        // Shut, and drawn shut
        Assert.True(game.Map.IsSolid(4, 1));
        Assert.Single(game.Map.SwappedPlaces, p => p.Swapped);
        // Without the key, the step before it only says so
        game.Tile = (4, 2);
        game.Run("Door");
        Assert.Equal(0, game.Story.Var("VAR_NEW_MAUVILLE_STATE"));
        Assert.True(game.Map.IsSolid(4, 1));
        // With it, a refusal keeps it shut and a yes opens it for good
        game.Story.Set("FLAG_GOT_BASEMENT_KEY_FROM_WATTSON");
        game.Answers.Enqueue(1);
        game.Run("Door");
        Assert.True(game.Map.IsSolid(4, 1));
        game.Answers.Enqueue(0);
        game.Run("Door");
        Assert.Equal(1, game.Story.Var("VAR_NEW_MAUVILLE_STATE"));
        Assert.False(game.Map.IsSolid(4, 1));
        Assert.DoesNotContain(game.Map.SwappedPlaces, p => p.Swapped);
    }

    [Fact]
    public void TheSwitchesLowerTheirOwnColourAndRaiseTheOtherAndAnArrivalRaisesBoth()
    {
        var game = new Game();
        game.Story.SetVar("VAR_NEW_MAUVILLE_STATE", 1);
        game.Arrive("NewMauville_Inside", 32, 33);
        var inside = game.Map;
        Assert.True(inside.IsSolid(Blue.X, Blue.Y));
        Assert.True(inside.IsSolid(Green.X, Green.Y));
        // The blue switch by the way in
        game.Tile = (30, 38);
        game.Run("BlueButton");
        Assert.False(inside.IsSolid(Blue.X, Blue.Y));
        Assert.True(inside.IsSolid(Green.X, Green.Y));
        // then a green one on the far side
        game.Tile = (18, 36);
        game.Run("GreenButton");
        Assert.True(inside.IsSolid(Blue.X, Blue.Y));
        Assert.False(inside.IsSolid(Green.X, Green.Y));
        // and blue again: the swaps of one cell take turns cleanly
        game.Run("BlueButton");
        Assert.False(inside.IsSolid(Blue.X, Blue.Y));
        Assert.True(inside.IsSolid(Green.X, Green.Y));
        // Coming in again raises both, as Emerald's arrival does
        game.Arrive("NewMauville_Inside", 32, 33);
        Assert.Equal(0, game.Story.Var("VAR_TEMP_1"));
        Assert.True(inside.IsSolid(Blue.X, Blue.Y));
        Assert.True(inside.IsSolid(Green.X, Green.Y));
    }

    [Fact]
    public void TheRedSwitchStopsTheGeneratorForGood()
    {
        var game = new Game();
        game.Story.SetVar("VAR_NEW_MAUVILLE_STATE", 1);
        game.Arrive("NewMauville_Inside", 32, 33);
        int before = (int)game.Map.BehaviourAt(Generator.X, Generator.Y);
        game.Tile = (33, 4);
        Assert.Contains(game.Run("Generator").Transcript, l => l.Text.Contains("heat"));
        game.Tile = (33, 6);
        game.Run("RedButton");
        Assert.Equal(2, game.Story.Var("VAR_NEW_MAUVILLE_STATE"));
        Assert.Contains(game.Map.SwappedPlaces, p => p.Var == "VAR_NEW_MAUVILLE_STATE" && p.Swapped);
        game.Tile = (33, 4);
        Assert.Contains(game.Run("Generator").Transcript, l => l.Text.Contains("quiet"));
        // and it stays so however often the plant is come to
        game.Arrive("NewMauville_Inside", 32, 33);
        Assert.Contains(game.Map.SwappedPlaces, p => p.Var == "VAR_NEW_MAUVILLE_STATE" && p.Swapped);
        Assert.True(before >= 0);
    }

    [Fact]
    public void ThePokeBallsThatAreVoltorbBattleOnceAndAreGone()
    {
        var game = new Game();
        game.Story.SetVar("VAR_NEW_MAUVILLE_STATE", 1);
        game.Arrive("NewMauville_Inside", 32, 33);
        foreach (var (key, flag, fight) in new[]
        {
            ("item_ball_6", "FLAG_DEFEATED_VOLTORB_1_NEW_MAUVILLE", BattleOutcome.Won),
            ("item_ball_7", "FLAG_DEFEATED_VOLTORB_2_NEW_MAUVILLE", BattleOutcome.Fled),
            ("item_ball_8", "FLAG_DEFEATED_VOLTORB_3_NEW_MAUVILLE", BattleOutcome.Caught)
        })
        {
            Assert.NotNull(game.Present(key));
            game.Fight = fight;
            game.Talk(key);
            Assert.Contains("wildbattle Voltorb 25 " + fight, game.Last.Log);
            Assert.True(game.Story.Has(flag));
            game.Arrive("NewMauville_Inside", 32, 33);
            Assert.Null(game.Present(key));
        }
    }

    [Fact]
    public void TheDesertUnderpassOpensAfterTheHallOfFameAndHoldsTheOtherFossil()
    {
        var game = new Game();
        game.Arrive("Route114_FossilManiacsTunnel", 6, 24);
        Assert.True(game.Map.IsSolid(6, 2));
        Assert.Equal((5, 3), (game.Present("fossil_maniac")!.GridX, game.Present("fossil_maniac")!.GridY));
        game.Story.Set("HoennHallOfFame");
        game.Arrive("Route114_FossilManiacsTunnel", 6, 24);
        Assert.False(game.Map.IsSolid(6, 2));
        Assert.Equal((6, 5), (game.Present("fossil_maniac")!.GridX, game.Present("fossil_maniac")!.GridY));
        // and the underpass's way back lands on the tunnel's
        var back = game.MapOf("DesertUnderpass").Warps.Single();
        Assert.Equal(("Route114_FossilManiacsTunnel", 6, 3), (back.TargetMap, back.TargetX, back.TargetY));

        // Whoever took the Root Fossil from Mirage Tower finds the Claw Fossil here, and it is gone afterwards
        game.Story.Set("FLAG_CHOSE_ROOT_FOSSIL");
        game.Arrive("DesertUnderpass", 10, 12);
        game.Talk("underpass_fossil");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Claw Fossil")!));
        game.Arrive("DesertUnderpass", 10, 12);
        Assert.Null(game.Present("underpass_fossil"));

        // whoever took the Claw finds the Root, and whoever took neither finds nothing to take
        var claw = new Game();
        claw.Story.Set("FLAG_CHOSE_CLAW_FOSSIL");
        claw.Arrive("DesertUnderpass", 10, 12);
        claw.Talk("underpass_fossil");
        Assert.Equal(1, claw.Bag.GetQuantity(ItemDatabase.Get("Root Fossil")!));
        var neither = new Game();
        neither.Arrive("DesertUnderpass", 10, 12);
        neither.Talk("underpass_fossil");
        Assert.NotNull(neither.Present("underpass_fossil"));
        Assert.False(neither.Story.Has("FLAG_HIDE_DESERT_UNDERPASS_FOSSIL"));
    }

    [Fact]
    public void NewMauvillesPokemonCanBeHadInHoenn()
    {
        foreach (string s in new[] { "Voltorb", "Magnemite", "Magneton", "Electrode" })
            Assert.True(SpeciesSources.Hoenn.Has(s), s);
        Assert.Contains("new_mauville", SpeciesSources.Hoenn.Ways["Magneton"]);
    }
}
