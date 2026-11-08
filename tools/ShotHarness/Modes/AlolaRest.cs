partial class Harness
{
    /// <summary>
    /// The rest of the islands (the <c>alolarest</c> mode, not part of <c>all</c>; plan 23, the wave after the loose
    /// ends): a new game in Alola, then the new places by day (<c>wr01</c> to <c>wr13</c>: Malie Garden, Routes 14, 15
    /// and 17, Ula'ula Meadow, Haina Desert and the Ruins of Abundance's door, Kala'e Bay, Seaward Cave, the meadow's
    /// hole, the Ruins of Conflict's door and the Fossil Restoration Center), two after dark (<c>wr14</c>, <c>wr15</c>),
    /// the rooms behind the doors Z5 to Z7 left shut (<c>wr20</c> to <c>wr38</c>), and last Tapu Koko's beat in the
    /// Ruins of Conflict after the Hall of Fame, to its battle (<c>wr40</c> on).
    /// </summary>
    public void AlolaRestMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        var story = game.Story;

        (string Name, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("wr01_malie_garden", "Ulaula", 112, 340, Direction.Right),
            ("wr02_route14", "Ulaula", 226, 284, Direction.Down),
            ("wr03_route15_aether_house", "Ulaula", 291, 272, Direction.Up),
            ("wr04_ulaula_meadow", "Ulaula", 178, 174, Direction.Up),
            ("wr05_route17_police", "Ulaula", 174, 118, Direction.Up),
            ("wr06_haina_desert", "Ulaula", 318, 230, Direction.Right),
            ("wr07_ruins_of_abundance", "Ulaula", 339, 224, Direction.Up),
            ("wr08_kalae_bay", "Melemele", 440, 108, Direction.Up),
            ("wr09_seaward_cave", "SeawardCave", 30, 22, Direction.Up),
            ("wr10_meadow_hole", "Melemele", 378, 78, Direction.Up),
            ("wr11_ruins_of_conflict", "Melemele", 250, 74, Direction.Up),
            ("wr12_fossil_center", "Akala", 245, 163, Direction.Up),
            ("wr13_route16_to_meadow", "Ulaula", 222, 174, Direction.Left),
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(name);
        }

        (string Name, string Map)[] rooms =
        {
            ("wr20_hauoli_city_hall", "HauoliCity_CityHall"), ("wr21_hauoli_police", "HauoliCity_PoliceStation"),
            ("wr22_hauoli_apparel", "HauoliCity_ApparelShop"), ("wr23_route2_motel", "AlolaRoute2_Motel"),
            ("wr24_ruins_of_conflict", "MahaloTrail_RuinsOfConflict"), ("wr25_heahea_bureau", "HeaheaCity_TouristBureau"),
            ("wr26_heahea_hotel", "HeaheaCity_Hotel"), ("wr27_heahea_lab", "HeaheaCity_ResearchLab"),
            ("wr28_konikoni_restaurant", "KonikoniCity_Restaurant"), ("wr29_paniola_nursery", "PaniolaRanch_Nursery"),
            ("wr30_kiawes_house", "PaniolaTown_KiawesHouse"), ("wr31_megamart", "RoyalAvenue_Megamart"),
            ("wr32_royal_boutique", "RoyalAvenue_Shop"), ("wr33_route8_motel", "AkalaRoute8_Motel"),
            ("wr34_fossil_center", "AkalaRoute8_FossilCenter"), ("wr35_route9_police", "AkalaRoute9_PoliceStation"),
            ("wr36_aether_house", "UlaulaRoute15_AetherHouse"), ("wr37_route17_police", "UlaulaRoute17_PoliceStation"),
            ("wr38_ruins_of_abundance", "HainaDesert_RuinsOfAbundance"),
        };
        foreach (var (name, map) in rooms)
        {
            var room = PokemonPlatinumEngine.Data.MapDatabase.Get(map);
            At(map, room.Width / 2 - (room.Width == 11 ? 1 : 0), room.Height - 3, Direction.Up); Frames(2); Shot(name);
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Ulaula", 112, 340, Direction.Right); Frames(2); game.LocationSign.Hide(); Shot("wr14_malie_garden_night");
        At("Melemele", 440, 108, Direction.Up); Frames(2); game.LocationSign.Hide(); Shot("wr15_kalae_bay_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // ---- Tapu Koko after the Hall of Fame, through the Ruins of Conflict's own script, to its battle
        story.Set("AlolaHallOfFame");
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Incineroar")!, 60));
        At("MahaloTrail_RuinsOfConflict", 5, 6, Direction.Up); Frames(4);
        Shot("wr40_before_tapu_koko");
        game.Interact();
        for (int i = 0; i < 600 && game.State != GameState.Battle; i++)
        {
            if (i % 30 == 10) Shot($"wr41_tapu_koko_beat_{i / 30}");
            if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
            else Frames(1);
        }
        if (game.State != GameState.Battle) { Console.WriteLine("  !! never happened: Tapu Koko's battle"); game.DeleteSave(); return; }
        var b = game.Battle;
        for (int i = 0; i < 24 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++)
        {
            Skip(0.5);
            if (b.IsWaitingForConfirm) Confirm(b);
        }
        Shot("wr42_tapu_koko_battle");
        Console.WriteLine($"  the guardian's battle: {string.Join(", ", b.Core.EnemySlots.Where(s => s.IsActive).Select(s => s.Pokemon!.Species.Name + " " + s.Pokemon.Level))}");
        game.DeleteSave();
    }
}
