using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// The Battle Tree, the power plant, Zygarde's cells and the rest of the islands (the <c>alolalast</c> mode, not part of
    /// <c>all</c>; plan 23, the wave after Poni's last places): a new game in Alola past its Hall of Fame, then the new places
    /// by day (<c>wt01</c> to <c>wt07</c>: Hau'oli Cemetery from its gate and its graves, Big Wave Beach, the Secluded Shore,
    /// the Battle Tree's lobby and hall, the power plant), a Zygarde Cell on Kala'e Bay and Dexio and Sina on Route 16
    /// (<c>wt08</c>, <c>wt09</c>), the Island Scan's Totodile in Seaward Cave (<c>wt10</c>), Stoutland ridden beside a
    /// buried item (<c>wt11</c>, shot a step on so the nose's "!" is up) and the cemetery's ghost girl after dark (<c>wt12</c>);
    /// then <see cref="AlolaOddsAndEnds"/>'s <c>wt20</c> to <c>wt41</c> (<c>AlolaLastOdds.cs</c>).
    /// </summary>
    public void AlolaLastMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        var story = game.Story;
        story.Set("AlolaHallOfFame");
        story.Set(RidePager.StoutlandFlag);
        story.Set("FLAG_DAILY_ALOLA_SCAN_TOTODILE");

        (string Name, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("wt01_cemetery_gate", "Melemele", 340, 129, Direction.Right),
            ("wt02_cemetery_graves", "Melemele", 352, 134, Direction.Up),
            ("wt03_big_wave_beach", "Melemele", 172, 152, Direction.Down),
            ("wt04_secluded_shore", "Ulaula", 172, 330, Direction.Up),
            ("wt05_battle_tree_lobby", "PoniGauntlet_BattleTree", 8, 9, Direction.Up),
            ("wt06_battle_tree_hall", "PoniGauntlet_BattleTreeHall", 8, 10, Direction.Up),
            ("wt07_power_plant", "BlushMountain_PowerPlant", 8, 9, Direction.Up),
            ("wt09_dexio_sina", "Ulaula", 248, 144, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(name);
        }

        // A Zygarde Cell on Kala'e Bay, and the scanned Totodile in Seaward Cave, each from two tiles west of it
        NextTo("wt08_zygarde_cell", "Melemele", n => n.NpcType == NPC.ZygardeCellType && n.ScriptFile == "kalae_bay");
        NextTo("wt10_island_scan_totodile", "SeawardCave", n => n.Key == "scan_totodile");

        // Stoutland ridden two steps from a buried item on Melemele
        At("Melemele", 172, 152, Direction.Up);
        var buried = game.Map.HiddenItems.First(h => h.Value.Buried).Key;
        // Stoutland's nose finds it on a step: from three tiles south, one step north and the "!" is up
        At("Melemele", buried.X, buried.Y + 3, Direction.Up);
        game.Player.SetRiding("Stoutland"); Frames(4);
        engine.Steering = (Direction.Up, false);
        for (int f = 0; f < 60 && game.Player.GridY > buried.Y + 2; f++) Frames(1);
        engine.Steering = null;
        // Let the step land and the nose find it, then shoot while the "!" is up
        for (int f = 0; f < 40 && game.Player.BubbleTimer <= 0f; f++) Frames(1);
        Frames(6); game.LocationSign.Hide(); Shot("wt11_stoutland_buried");
        Console.WriteLine($"  Stoutland at {game.Player.GridX},{game.Player.GridY} ({game.Player.Mode}, {game.Player.Steed}); bubble {game.Player.Bubble} for {game.Player.BubbleTimer:0.00}s; nearest hidden {StoutlandSearch.Nearest(game.Map, game.Player.GridX, game.Player.GridY, story)}");
        Console.WriteLine($"  buried item at {buried.X},{buried.Y}: {game.Map.HiddenItems[buried].Item}; riding {game.Player.Steed}");
        game.Player.SetRiding(null);

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        story.Set("FLAG_ALOLA_CEMETERY_NIGHT");
        At("Melemele", 352, 141, Direction.Up); Frames(2); game.LocationSign.Hide(); Shot("wt12_cemetery_ghost_girl_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        AlolaOddsAndEnds(story);
        game.DeleteSave();
    }

    /// <summary>Shoots someone of a map from two tiles west of them, facing them (from the south, the player's head would hide a thing as small as a cell).</summary>
    private void NextTo(string name, string map, Func<NPC, bool> who)
    {
        At(map, 0, 0, Direction.Up);
        var person = game.Map.Everyone.First(who);
        At(map, person.GridX - 2, person.GridY, Direction.Right); Frames(2); game.LocationSign.Hide(); Shot(name);
        Console.WriteLine($"  {name}: {person.Key} at {person.GridX},{person.GridY}, {(game.Map.NPCs.Contains(person) ? "shown" : "hidden")}");
    }
}
