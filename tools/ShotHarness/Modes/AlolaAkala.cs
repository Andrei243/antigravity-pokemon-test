partial class Harness
{
    /// <summary>
    /// Plan 23 · Z6's shots (the <c>akala</c> mode, not part of <c>all</c>): a new game in Alola, then the rest of Akala by
    /// day (<c>wz01</c> to <c>wz12</c>: Route 6, Royal Avenue's dome, Route 8's motel, the Lush Jungle's clearing, Route
    /// 7's beach, Wela Volcano Park's crater, Diglett's Tunnel, Route 9, Konikoni City, Memorial Hill, the Ruins of Life's
    /// door, Hano Beach) and two of them after dark (<c>wz13</c>, <c>wz14</c>), the new rooms (<c>wz20</c> to
    /// <c>wz27</c>), the Trainer Card with Alola's stamps (<c>wz40</c>), and last the totem Gumshoos in Verdant Cavern
    /// through its own script (<c>wz30</c> on: the battle opening on its aura, the menu with the aura drawn, its first
    /// call answered, and the Normalium Z's Z-Move armed and used against it), which is plan 23 · Z6's "done when".
    /// </summary>
    public void AlolaAkalaMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        var story = game.Story;

        (string Name, int X, int Y, Direction Facing)[] places =
        {
            ("wz01_route6", 208, 218, Direction.Right),
            ("wz02_royal_avenue_dome", 265, 214, Direction.Up),
            ("wz03_route8_motel", 270, 140, Direction.Up),
            ("wz04_lush_jungle_clearing", 207, 104, Direction.Up),
            ("wz05_route7_beach", 318, 110, Direction.Up),
            ("wz06_wela_crater", 331, 50, Direction.Up),
            ("wz08_route9", 322, 205, Direction.Up),
            ("wz09_konikoni", 322, 252, Direction.Up),
            ("wz10_memorial_hill", 366, 206, Direction.Up),
            ("wz11_ruins_of_life", 361, 156, Direction.Up),
            ("wz12_hano_beach", 266, 262, Direction.Up),
        };
        foreach (var (name, x, y, facing) in places)
        {
            At("Akala", x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(name);
        }
        At("DiglettsTunnel", 30, 22, Direction.Left); Frames(2); game.LocationSign.Hide(); Shot("wz07_digletts_tunnel");

        (string Name, string Map, int X, int Y, Direction Facing)[] rooms =
        {
            ("wz20_royal_avenue_center", "RoyalAvenue_PokemonCenter_1F", 8, 10, Direction.Up),
            ("wz21_royal_avenue_house", "RoyalAvenue_House", 5, 7, Direction.Up),
            ("wz22_route8_center", "AkalaRoute8_PokemonCenter_1F", 8, 10, Direction.Up),
            ("wz23_konikoni_center", "KonikoniCity_PokemonCenter_1F", 8, 10, Direction.Up),
            ("wz24_olivias_shop", "KonikoniCity_OliviasShop", 5, 7, Direction.Up),
            ("wz25_konikoni_house", "KonikoniCity_House", 5, 7, Direction.Up),
            ("wz26_ruins_of_life", "AkalaOutskirts_RuinsOfLife", 4, 7, Direction.Up),
            ("wz27_hano_grand_resort", "HanoGrandResort", 8, 10, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in rooms) { At(map, x, y, facing); Frames(2); Shot(name); }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Akala", 265, 214, Direction.Up); Frames(2); game.LocationSign.Hide(); Shot("wz13_royal_avenue_night");
        At("Akala", 322, 252, Direction.Up); Frames(2); game.LocationSign.Hide(); Shot("wz14_konikoni_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // ---- Alola's stamps on the Trainer Card (style guide, "Badges", "Alola's island stamps")
        story.GiveBadge(RegionDatabase.Alola, "Melemele");
        story.GiveBadge(RegionDatabase.Alola, "Akala");
        At("Akala", 322, 252, Direction.Up); Frames(2);
        game.State = GameState.Overworld;
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        Frames(40); Shot("wz40_card_alola_stamps");
        game.TrainerCard.Close();
        Frames(10);

        // ---- the totem Gumshoos, through Verdant Cavern's own script, and the Normalium Z used against it
        var lead = new Pokemon(PokemonDatabase.Get("Litten")!, 16);
        lead.Moves.Clear();
        lead.Moves.Add(new Move(MoveDatabase.Get("Scratch")!));
        lead.Moves.Add(new Move(MoveDatabase.Get("Ember")!));
        lead.HeldItem = ItemDatabase.Get("Normalium Z");
        game.Party.Members.Insert(0, lead);
        game.Bag.AddItem(ItemDatabase.Get(PokemonPlatinumEngine.Battle.Sim.BattleCore.ZRingItem)!);
        foreach (string flag in new[] { "FLAG_ALOLA_VERDANT_TRIAL", "FLAG_ALOLA_VERDANT_DEN_1", "FLAG_ALOLA_VERDANT_DEN_2", "FLAG_ALOLA_VERDANT_DEN_3" }) story.Set(flag);
        At("VerdantCavern", 29, 9, Direction.Up); Frames(4);
        game.LocationSign.Hide();
        Shot("wz29_before_the_totem");
        game.Interact();
        for (int i = 0; i < 1500 && game.State != GameState.Battle; i++)
        {
            if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
            else Frames(1);
        }
        if (game.State != GameState.Battle) { Console.WriteLine("  !! never happened: the totem's battle"); return; }
        var b = game.Battle;
        Console.WriteLine($"  the totem's battle: format {b.Core.Format}, aura {string.Join(",", b.Core.Totem?.Stats ?? Array.Empty<StatType>())}, cannot flee {b.Core.CannotFlee}");
        // The opening: the totem comes out and is wrapped in its aura, a shot every half second to the menu
        for (int i = 0; i < 24 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++)
        {
            Skip(0.5);
            if (i % 2 == 0) Shot($"wz30_totem_opening_{i / 2}");
            if (b.IsWaitingForConfirm) Confirm(b);
        }
        Skip(0.5);
        Shot("wz31_totem_menu");
        ShotCrop("wz31_totem_menu_close", 1100, 120, 640, 360, 2);
        // The first turn: the totem calls, and its ally comes
        b.SelectMainMenuOption(0); Frames(5);
        b.SelectMove(1); Frames(1);
        if (b.HUD.MenuState == BattleMenuState.SelectTarget) b.SelectTarget(0);
        for (int i = 0; i < 40 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++)
        {
            Skip(0.3);
            if (i % 3 == 0) Shot($"wz32_totem_calls_{i / 3}");
            if (b.IsWaitingForConfirm) Confirm(b);
        }
        Skip(0.5);
        Shot("wz33_ally_answered");
        Console.WriteLine($"  after the first turn: {string.Join(", ", b.Core.EnemySlots.Where(s => s.IsActive).Select(s => s.Pokemon!.Species.Name))}");
        // The Normalium Z armed and used on the totem
        if (b.Core.EnemySlots[0].Pokemon is { } totem) totem.CurrentHP = Math.Max(1, totem.MaxHP / 3);
        b.SelectMainMenuOption(0); Frames(5);
        Console.WriteLine($"  Z-Move armed: {b.ToggleLaterMechanic()}");
        Shot("wz34_z_move_armed");
        b.SelectMove(0); Frames(1);
        if (b.HUD.MenuState == BattleMenuState.SelectTarget) b.SelectTarget(0);
        for (int i = 0; i < 40 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++)
        {
            Skip(0.25);
            if (i % 2 == 0) Shot($"wz35_z_move_{i / 2}");
            if (b.IsWaitingForConfirm) Confirm(b);
        }
        foreach (var said in b.Core.Log.OfType<PokemonPlatinumEngine.Battle.Sim.Said>()) Console.WriteLine($"  said: {said.Text}");
        Console.WriteLine($"  the battle is over: {b.IsBattleOver}, result {b.Core.Result}");
        game.DeleteSave();
    }
}
