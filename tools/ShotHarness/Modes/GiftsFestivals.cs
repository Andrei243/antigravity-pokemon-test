partial class Harness
{
    // ---------------------------------------------------------------- the festivals (plan 40 · GE9)

    // The festivals' towns dressed on their days (gf80 to gf89), in a new game of its own past the opening, the harness's
    // day moved into each festival's days (GameClock.FixedDate, as `formats` moves it a day) and put back afterwards, so
    // the engine's clock sets the festival's flag and the overlays' things and stall keeper come out by it:
    //   gf80_lanterns_by_day: Hearthome City's plaza on 12 October, its strings of paper lanterns;
    //   gf80b_lanterns_day_after: the same on 17 October, the festival over and the plaza plain;
    //   gf81_lanterns_after_dark: the 12th again after dark, the lanterns' cores lit;
    //   gf82_midwinter: Snowpoint City's lit pines after dark on 20 December;
    //   gf83_blossom: Floaroma Town's blossom arches on 3 April;
    //   gf84_summer_night: Sunyshore City come to after dark on 22 July, the bursts over the sea;
    //   gf85_festival_stall: Hearthome's stall keeper spoken to, and her stall's stock (gf85b_festival_stall_stock);
    //   gf86_harvest_moon: Celadon City's lanterns and moon lantern after dark on 17 September (Kanto);
    //   gf87_bellfall: Ecruteak's bells and maple garlands on 3 November (Johto);
    //   gf88_tide_day: Slateport's bunting on 20 June (Hoenn);
    //   gf89_bridgelight: Castelia's plaza lit after dark on 3 February (Unova);
    //   gf8a_christmas: Jubilife City's park on 25 December with its decorated pine (Sinnoh's holidays);
    //   gf8b_easter, gf8c_new_year, gf8d_valentine: the other three in Celadon's park, Kanto's holidays;
    //   gf8e_serenade, gf8f_serenade_after_dark, gf8g_serenade_east: Kalos's Solstice Serenade in Lumiose's two parks on 21 June;
    //   gf8h_garland, gf8i_garland_after_dark: Alola's Garland Days in Hau'oli City's plaza on 9 August;
    //   gf8j_wool_fair: Galar's Wool Fair inside Wedgehurst's Pokémon Center on 2 May;
    //   gf8k_sheaf_feast: Hisui's Sheaf Feast on Jubilife Village's training ground on 23 September.
    // It prints the festivals on each day and how many of each town's festival things stand on its map.
    void GiftsFestivals()
    {
        GiftsNewGame();
        var harnessDay = GameClock.FixedDate;
        void Day(int year, int month, int day, bool night)
        {
            GameClock.FixedDate = new System.DateTime(year, month, day);
            engine.Settings.TimeOfDay = night ? TimeOfDay.Night : TimeOfDay.Day;
            engine.ApplySettings(window: false);
            Frames(3);
        }
        void Look(string shot, string map, int x, int y, Direction facing, int settle = 40, bool arrive = false)
        {
            At(map, x, y, facing);
            if (arrive) game.ArriveOnMap();
            game.LocationSign.Hide();
            Frames(settle);
            var m = MapDatabase.Get(map);
            int standing = m.NPCs.Count(n => n.ShownBy?.StartsWith("FLAG_FESTIVAL_", System.StringComparison.Ordinal) == true);
            Console.WriteLine($"  {shot}: {GameClock.Today:yyyy-MM-dd} {engine.Settings.TimeOfDay}, festivals {string.Join(", ", Festivals.On(GameClock.Today).Select(f => f.Id).DefaultIfEmpty("none"))}; festival things on {map} {standing}");
            Shot(shot);
        }

        // Hearthome City's Night of Lanterns
        Day(2026, 10, 12, night: false);
        Look("gf80_lanterns_by_day", "Sinnoh", 476, 703, Direction.Up);
        Day(2026, 10, 17, night: false);
        Look("gf80b_lanterns_day_after", "Sinnoh", 476, 703, Direction.Up);
        Day(2026, 10, 12, night: true);
        Look("gf81_lanterns_after_dark", "Sinnoh", 476, 703, Direction.Up);

        // Snowpoint City's Midwinter Lights, the snow country's light let in
        Day(2026, 12, 20, night: true);
        Look("gf82_midwinter", "Sinnoh", 366, 225, Direction.Up, settle: 90);

        // Floaroma Town's Blossom Days
        Day(2027, 4, 3, night: false);
        Look("gf83_blossom", "Sinnoh", 171, 660, Direction.Up);

        // Sunyshore City's Summer Night, come to after dark: its arrival lights the bursts over the sea, shot once the
        // burst's white flash is over
        Day(2026, 7, 22, night: true);
        Look("gf84_summer_night", "Sinnoh", 856, 763, Direction.Up, settle: 18, arrive: true);

        // Hearthome's stall: its keeper and her stock
        Day(2026, 10, 12, night: false);
        At("Sinnoh", 481, 714, Direction.Up);
        game.LocationSign.Hide();
        Frames(20);
        game.Interact();
        Frames(3);
        GiftsUntil(() => GiftsSaying("lit for the week"), "the stall keeper's greeting", answer: false);
        game.Dialogue.FinishLine(); Frames(20);
        Shot("gf85_festival_stall");
        GiftsUntil(() => game.State == GameState.Shop && game.Shop.IsActive, "the stall's counter", answer: false);
        game.Shop.Confirm(game.Bag, 3000, engine.ShowNotification); Frames(30);
        Shot("gf85b_festival_stall_stock");
        game.Shop.Close(); Frames(4);
        GiftsUntil(() => !engine.ScriptRunning && !game.Dialogue.IsActive && game.State == GameState.Overworld, "the stall's goodbye");

        // One festival of each other region
        Day(2026, 9, 17, night: true);
        Look("gf86_harvest_moon", "Kanto", 200, 141, Direction.Up);
        Day(2026, 11, 3, night: false);
        Look("gf87_bellfall", "Johto", 386, 159, Direction.Down);
        Day(2026, 6, 20, night: false);
        Look("gf88_tide_day", "Hoenn", 227, 300, Direction.Down);
        Day(2027, 2, 3, night: true);
        Look("gf89_bridgelight", "CasteliaCity_Plaza", 15, 19, Direction.Up);

        // The holidays: Christmas in Jubilife's park, the others in Celadon's
        Day(2026, 12, 25, night: false);
        Look("gf8a_christmas", "Sinnoh", 166, 767, Direction.Up);
        Day(2027, 3, 27, night: false);   // Easter Saturday, 2027
        Look("gf8b_easter", "Kanto", 193, 144, Direction.Up);
        Day(2027, 1, 1, night: true);
        Look("gf8c_new_year", "Kanto", 193, 144, Direction.Up);
        Day(2027, 2, 14, night: false);
        Look("gf8d_valentine", "Kanto", 193, 144, Direction.Up);

        // Kalos's own: the Solstice Serenade in Lumiose's west park, by day and with its lanterns lit after dark
        Day(2026, 6, 21, night: false);
        Look("gf8e_serenade", "Kalos", 337, 360, Direction.Down);
        Day(2026, 6, 21, night: true);
        Look("gf8f_serenade_after_dark", "Kalos", 337, 360, Direction.Down);
        Look("gf8g_serenade_east", "Kalos", 382, 366, Direction.Up);

        // Alola's Garland Days, Galar's Wool Fair (inside Wedgehurst's Pokémon Center) and Hisui's Sheaf Feast
        Day(2026, 8, 9, night: false);
        Look("gf8h_garland", "Melemele", 285, 174, Direction.Up);
        Day(2026, 8, 9, night: true);
        Look("gf8i_garland_after_dark", "Melemele", 285, 174, Direction.Up);
        Day(2027, 5, 2, night: false);
        Look("gf8j_wool_fair", "Wedgehurst_PokemonCenter", 8, 10, Direction.Up);
        Day(2026, 9, 23, night: false);
        Look("gf8k_sheaf_feast", "JubilifeVillage", 69, 33, Direction.Up);

        // The harness's day and the day's light back
        GameClock.FixedDate = harnessDay;
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        Frames(3);
    }
}
