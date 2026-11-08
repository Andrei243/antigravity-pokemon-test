using System;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    /// <summary>
    /// Alola after its Hall of Fame (plan 23 · Z8; not part of <c>all</c>): the Lightseekers' visit at home
    /// (<c>wa50</c>), the Ultra Wormhole over the Altar's dais (<c>wa51</c>), each of the seven Ultra Beasts' worlds
    /// with its beast ahead (<c>wa52_*</c>), one beast's staged beat (<c>wa53</c>), Ultra Megalopolis and Megalo
    /// Tower's top with Necrozma (<c>wa54</c>, <c>wa55</c>), the Battle Royal Dome with its ring of a rank
    /// (<c>wa56</c>) and that rank's Battle Royal at its menu (<c>wa57</c>). It prints where each stands and what
    /// each scene left (the variable, the flags, the bag).
    /// </summary>
    public void AlolaUltraSpaceMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        var story = game.Story;
        story.Set("AlolaHallOfFame");
        story.SetVar("VAR_ALOLA_STORY", 54);
        game.Party.Members.Clear();
        foreach (string species in new[] { "Decidueye", "Lycanroc", "Primarina", "Togedemaru" })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 65));

        void ReadOn(int most = 1200)
        {
            for (int i = 0; i < most && (engine.ScriptRunning || game.Dialogue.IsActive || game.State == GameState.Dialogue); i++)
            {
                if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(2); continue; }
                if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
                Frames(2);
            }
        }
        void Arrive(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            Frames(2);
            game.LocationSign.Hide();
        }
        void Told(string shot) => Console.WriteLine($"{shot}: {game.Map.Name} {game.Player.GridX},{game.Player.GridY}; VAR_ALOLA_ULTRA {story.Var("VAR_ALOLA_ULTRA")}");

        // ---- Home after the credits: the Lightseekers' visit, shot as Veyra speaks
        Arrive("AlolaRoute1_PlayersHouse", 6, 6, Direction.Down);
        for (int i = 0; i < 400 && !(game.Dialogue.IsActive && game.Dialogue.CurrentLine.Contains("Veyra")); i++)
        {
            if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
            Frames(2);
        }
        game.Dialogue.FinishLine(); Frames(20);
        Shot("wa50_lightseekers_at_home"); Told("wa50");
        ReadOn();
        Console.WriteLine($"  the bag holds {game.Bag.GetQuantity(ItemDatabase.Get("Beast Ball")!)} Beast Balls; the team {string.Join(", ", game.Party.Members.Select(p => p.Species.Name))}");

        // ---- The Altar: the wormhole open over the dais
        story.SetVar("VAR_ALOLA_ULTRA", 2);
        Arrive("PoniAltar", 32, 20, Direction.Up);
        ReadOn();
        Frames(40);
        Shot("wa51_altar_wormhole"); Told("wa51");

        // ---- The seven worlds, each with its beast ahead
        foreach (var (map, key) in new[] { ("UltraDeepSea", "deep_sea"), ("UltraJungle", "jungle"), ("UltraDesert", "desert"), ("UltraPlant", "plant"),
                                           ("UltraCrater", "crater"), ("UltraForest", "forest"), ("UltraRuin", "ruin") })
        {
            Arrive(map, 32, 21, Direction.Up);
            ReadOn();
            Frames(40);
            Shot($"wa52_{key}"); Told($"wa52_{key}");
        }
        // The way home from inside, the wormhole one came in by
        Arrive("UltraDeepSea", 32, 51, Direction.Down);
        ReadOn();
        Frames(40);
        Shot("wa52_deep_sea_wormhole");

        // ---- A beast's staged beat: Nihilego, as its line is said
        Arrive("UltraDeepSea", 32, 17, Direction.Up);
        ReadOn();
        game.Interact();
        for (int i = 0; i < 600 && !game.Dialogue.IsActive; i++) Frames(1);
        game.Dialogue.FinishLine(); Frames(12);
        Shot("wa53_nihilego_beat"); Told("wa53");
        for (int i = 0; i < 600 && game.State != GameState.Battle; i++)
        {
            if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
            Frames(2);
        }
        if (game.State == GameState.Battle) { ToMainMenu(game.Battle!); Shot("wa53b_nihilego_battle"); }
        else Console.WriteLine("  !! never happened: Nihilego's battle");

        // ---- Ultra Megalopolis and the top of Megalo Tower
        game.State = GameState.Overworld;
        story.SetVar("VAR_ALOLA_ULTRA", 3);
        story.Set("FLAG_ALOLA_ULTRA_AT_TOWER");
        Arrive("UltraMegalopolis", 32, 27, Direction.Up);
        ReadOn();
        Frames(40);
        Shot("wa54_megalopolis"); Told("wa54");
        Arrive("MegaloTower", 7, 9, Direction.Up);
        ReadOn();
        Frames(40);
        Shot("wa55_megalo_tower_necrozma"); Told("wa55");

        // ---- The Battle Royal Dome: the Normal Rank's ring, and its Royal at its menu
        story.Set("FLAG_ALOLA_ROYAL_SHOW_DONE");
        story.SetVar("VAR_ALOLA_ROYAL_SHOW", 1);
        story.Set("FLAG_ALOLA_ROYAL_RING_NORMAL");
        Arrive("RoyalAvenue_BattleRoyalDome", 10, 12, Direction.Up);
        ReadOn();
        Frames(30);
        Shot("wa56_dome_ring"); Told("wa56");
        story.Unset("FLAG_ALOLA_ROYAL_RING_NORMAL");
        Arrive("RoyalAvenue_BattleRoyalDome", 5, 12, Direction.Left);
        ReadOn();
        Frames(20);
        game.Interact();
        for (int i = 0; i < 600 && !engine.Choice.IsOpen; i++)
        {
            if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
            Frames(2);
        }
        engine.Choice.Confirm();
        for (int i = 0; i < 900 && game.State != GameState.Battle; i++)
        {
            if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
            Frames(2);
        }
        if (game.State == GameState.Battle)
        {
            var royal = game.Battle!;
            ToMainMenu(royal);
            Shot("wa57_dome_royal"); Told("wa57");
            Console.WriteLine($"  the Royal: format {royal.Format}; others {string.Join(", ", royal.EnemySlots.Select(s => s.Trainer?.Name))}");
        }
        else Console.WriteLine("  !! never happened: the dome's Battle Royal");
    }
}
