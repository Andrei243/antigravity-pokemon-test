using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- statues (plan 38 · EX6, part b; part of `explore`: EXPLORE_PART=statues)

    // A Sinnoh Champion in the Lake Cavern before one of the day's walls: its question, a wall of ours (the screen handed a
    // wall of the hideaway's own table whose seed holds a statue, since which wall the field's chance gives can't be
    // planned) dug out with the statue bare (ex63); then three Water statues stood in the base (ex64) and the Lake Cavern
    // come to again with them weighing its day (ex65). It prints the wall, what came out, and the day's Water share
    // without the statues and with them.
    private void ExploreStatues()
    {
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Party.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Infernape")!, 60));
        game.Story.Set(Hideaways.OpenedBy);
        game.Story.SetVar("VAR_HAS_SEEN_UNDERGROUND_ROARK_INTRO", 1);
        game.TrainersLookOnArrival = false;
        game.Underground.HasMined = true;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }

        var lake = Hideaways.All.First(h => h.Map == "HideawayLakeCavern");
        int day = (int)(game.Encounters.DailyNumber & 0x7FFFFFFF);
        var wall = Hideaways.WallsOf(lake, day)[0];
        At(lake.Map, wall.X, wall.Z + 1, Direction.Up);
        game.ArriveOnMap();
        Frames(30);
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "the wall's line"); Box().FinishLine(); Frames(2); Box().Advance(); Frames(2);
        Until(() => engine.Choice.IsOpen, "the wall's question"); Frames(20);
        engine.Choice.Confirm(); Frames(4);
        Until(() => game.State == GameState.Mining, "the wall's game", 300);
        Console.WriteLine($"explore: the Lake Cavern's wall at {wall.X},{wall.Z} dug, its flag {game.Story.Has(Hideaways.WallFlag(lake, 0))}");

        var mining = game.Mining;
        var table = Hideaways.WallTable(lake, day, game.Pokedex.NationalUnlocked);
        ulong seed = 1;
        while (!new MiningGame(new Rand32(seed), table).Objects.Take(4).Any(o => UndergroundDatabase.IsStatue(o.Id))) seed++;
        mining.Open(new MiningGame(new Rand32(seed), table), game.Underground);
        Frames(30);
        var statue = mining.Game.Objects.First(o => UndergroundDatabase.IsStatue(o.Id));
        void Strike(int x, int y)
        {
            mining.Move(x - mining.CursorX, y - mining.CursorY);
            mining.PressConfirm();
            Frames(3);
        }
        mining.Game.PickSelected = false;
        for (int y = statue.Y; y < statue.Y + statue.Object.Height; y++)
            for (int x = statue.X; x < statue.X + statue.Object.Width; x++)
                while (!mining.Game.Over && mining.Game.DirtAt(x, y) > 0) Strike(x, y);
        Frames(30);
        Shot("ex63_statue_dug");
        Console.WriteLine($"ex63_statue_dug: wall seed {seed}, things {string.Join(", ", mining.Game.Objects.Take(mining.Game.ItemCount).Select(o => UndergroundDatabase.IsStatue(o.Id) ? UndergroundDatabase.StatueName(o.Id) : UndergroundDatabase.IsSphere(o.Id) ? UndergroundDatabase.SphereName(UndergroundDatabase.SphereOf(o.Id)) : o.Id.ToString()))}, the statue bare {mining.Game.IsBare(mining.Game.Objects.ToList().IndexOf(statue))}");
        for (int i = 0; i < 80 && mining.IsActive; i++) { Frames(20); mining.PressConfirm(); Frames(2); }
        Until(() => game.State == GameState.Overworld, "the hideaway again", 300);
        Console.WriteLine($"   in the bag's goods: {string.Join(", ", game.Underground.Goods.Select(UndergroundDatabase.GoodName))}");

        // ---- Three Water statues stood in the base
        var room = SecretBaseRules.Create(30, 40, Direction.Up, new Rand32(7));
        room.Boulders.Clear();
        string[] water = { "Golduck", "Dewgong", "Seaking" };
        for (int i = 0; i < water.Length; i++)
        {
            int id = UndergroundDatabase.StatueOf(water[i]);
            game.Underground.TryAddPcGood(id);
            room.Goods.Add(new PlacedGood { Good = id, X = SecretBaseRules.FloorLeft + 3 + i * 3, Z = SecretBaseRules.FloorTop + 4, Slot = i });
        }
        game.Underground.Base = room;
        At(GameEngine.UndergroundMap, SecretBaseRules.FloorLeft + 6, SecretBaseRules.FloorTop + 7, Direction.Up);
        game.ArriveOnMap();
        Frames(40);
        Shot("ex64_base_with_statues");
        var weights = Hideaways.Weights(room);
        Console.WriteLine($"ex64_base_with_statues: {room.Goods.Count} statues, weights {string.Join(", ", weights.Select(w => $"{w.Key} {w.Value}"))}");

        // ---- The Lake Cavern with them
        double Share(IReadOnlyDictionary<PokemonType, int>? w)
        {
            var rows = Hideaways.Table(lake, day, true, w);
            return rows.Where(r => Hideaways.TypesOf(PokemonDatabase.Get(r.SpeciesName)!).Contains(PokemonType.Water)).Sum(r => r.Weight) / (double)rows.Sum(r => r.Weight);
        }
        At(lake.Map, Hideaways.ArriveX, Hideaways.ArriveY, Direction.Down);
        game.ArriveOnMap();
        for (int f = 0; f < 60 * 30 && game.WildInSight.All.Count < FieldPokemon.MostInView; f++) { game.KeepWildInSight(1f / 60f); Frames(1); }
        Frames(30);
        Shot("ex65_hideaway_pulled");
        Console.WriteLine($"ex65_hideaway_pulled: the day's twelve {string.Join(", ", MapDatabase.Get(lake.Map).WildEncounters.Select(e => e.SpeciesName))}; Water's share of the slots {Share(null):P0} without, {Share(weights):P0} with; the first pick's {Hideaways.PickShare(lake, true, new Dictionary<PokemonType, int>(), PokemonType.Water):P0} without, {Hideaways.PickShare(lake, true, weights, PokemonType.Water):P0} with");
    }
}
