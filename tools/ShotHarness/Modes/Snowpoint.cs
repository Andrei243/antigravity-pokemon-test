partial class Harness
{
    // The eighth chapter's first half (plan 02 · S11, part a; not part of `all`), played by its own scripts: the rival
    // at the head of Acuity Lakefront's rock face, calling down and running on; the face refusing a climb with no
    // Badge; Snowpoint City, its temple's guard turning the player back, the sailor on the quay; Maylene thawing out in
    // the Pokémon Center; Candice's battle and the Icicle Badge; Maylene after it, and gone home once the lakefront is
    // come to again; Rock Climb up the face; Lake Acuity with Commander Jupiter and the rival, from the west tile of
    // its way in and from the east, the shore empty afterwards, the TM on the far shore; and the lake as it was, with
    // the rival keeping watch. SNOWPOINT_FROM=city starts in Snowpoint City, SNOWPOINT_FROM=lake at the rock face
    // with the Badge, the story set as the scenes before would have left it.
    public void SnowpointMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var infernape = new Pokemon(PokemonDatabase.Get("Infernape")!, 55);
        infernape.Moves.Clear();
        foreach (string move in new[] { "Flamethrower", "Rock Climb", "Close Combat", "Mach Punch" }) infernape.Moves.Add(new Move(MoveDatabase.Get(move)!));
        team.Members.Insert(0, infernape);
        // The Badges of the chapters before, and the story as Commander Mars's defeat at Lake Verity leaves it
        // (lake_verity.txt): Mt. Coronet's hall clear, Lake Acuity waiting, Maylene gone by on Route 217
        foreach (var badge in new[] { "Coal", "Forest", "Relic", "Cobble", "Fen", "Mine" }) story.GiveBadge("Sinnoh", badge);
        foreach (string flag in new[] { "FLAG_LAKE_VALOR_EXPLODED", "FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY", "FLAG_HIDE_LAKE_VERITY_TEAM_GALACTIC",
            "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNTS_M", "FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE" })
            story.Set(flag);
        foreach (string flag in new[] { "FLAG_HIDE_LAKE_ACUITY_JUPITER", "FLAG_HIDE_LAKE_ACUITY_LOW_WATER_RIVAL", "FLAG_HIDE_LAKE_VERITY_LOW_WATER_PROF_ROWAN", "FLAG_HIDE_LAKE_VERITY_LOW_WATER_COUNTERPART" })
            story.Unset(flag);
        story.SetVar("VAR_LAKE_ACUITY_STATE", 1);
        story.SetVar("VAR_ROUTE_217_STATE", 1);

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Player Me() => game.Player;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Quick();
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        // Between shots the game is moved on a frame at a time but drawn only every sixth frame (llvmpipe fills the 4K
        // screen slowly), and a line of text is shown whole at once
        int quick = 0;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (++quick % 6 == 0) engine.Draw();
        }
        void ReadTo(string? text, int most = 3000)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld && i > 60) break;
                if (text == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (engine.Choice.IsOpen) { Quick(); engine.Choice.Confirm(); Quick(); }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        // A battle won: every foe brought down to 1 HP and struck, the player's own team kept well
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var mine in team.Members.Where(p => p.CurrentHP > 0)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    foreach (var wild in fight.Core.EnemySlots.Where(b => b.Pokemon != null)) wild.Pokemon!.CurrentHP = Math.Min(wild.Pokemon.CurrentHP, 1);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(0);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => State() == GameState.Overworld, "the field again", 1500);
            foreach (var mine in team.Members.Where(p => !p.IsEgg)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
            Frames(10);
        }
        void IntoBattle(string what)
        {
            for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Quick(); engine.Choice.Confirm(); Quick(); }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            if (State() != GameState.Battle) Console.WriteLine($"  !! never happened: {what}");
            else Skip(1.0);
        }
        // A step onto a tile of a map, from the tile before it, that starts a scene
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = way switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            At(map, x - dx, y - dy, way);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }
        // Comes into a map through a warp onto a tile, as the game does: its own script, then the trigger stepped out onto
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            game.SteppedOutOfWarp = true;
            Frames(2);
        }
        // Talks to someone of a map (a room's people belong to no area: place null), from below unless told otherwise
        void TalkTo(string map, string key, string? place, int dx = 0, int dy = 1)
        {
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Key == key && (place == null || n.ScriptFile == place));
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        void Where(string what) => Console.WriteLine($"{what}: player at {Me().GridX},{Me().GridY} of {game.Map.Name}");
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));
        NPC? Someone(string map, string key, string? place) => MapDatabase.Get(map).NPCs.FirstOrDefault(n => n.Key == key && (place == null || n.ScriptFile == place));

        Frames(5);
        ReadTo(null, 600);

        string from = Environment.GetEnvironmentVariable("SNOWPOINT_FROM") ?? "";
        int stage = from switch { "" => 0, "city" => 1, "lake" => 2, _ => 0 };

        if (stage == 0)
        {
            // ---- Acuity Lakefront: the rival at the head of the rock face, calling down as the player comes up to its foot
            At("Sinnoh", 310, 240, Direction.Up); Frames(30); Shot("sn01_lakefront_the_rival_on_the_rock_face");
            At("Sinnoh", 311, 246, Direction.Up); Frames(30); Shot("sn02_lakefront_the_foot_of_the_face");
            StepOnto("Sinnoh", 310, 244, Direction.Up);
            ReadTo("so slow"); Frames(4); Shot("sn03_lakefront_the_rival_calls_down");
            ReadTo("Badge from Snowpoint"); Frames(4); Shot("sn04_lakefront_the_rocks_need_a_badge");
            ReadTo("here I come"); Frames(4); Shot("sn05_lakefront_the_rival_is_off");
            Box().Advance();
            for (int i = 0; i < 50; i++) Quick();
            Frames(2); Shot("sn06_lakefront_the_rival_runs_on");
            ReadTo(null); Frames(20);
            Shot("sn07_lakefront_the_face_clear");
            Console.WriteLine($"lakefront: state {story.Var("VAR_ACUITY_LAKEFRONT_STATE")}, {Flags("FLAG_HIDE_ACUITY_LAKEFRONT_RIVAL")}, way in leads to {game.Map.GetWarpAt(308, 229)?.TargetMap}");
            Where("lakefront");

            // ---- the face won't be climbed without Snowpoint's Badge
            At("Sinnoh", 310, 242, Direction.Up);
            game.Interact();
            ReadTo("steep, rough wall"); Frames(4); Shot("sn08_lakefront_no_badge_no_climb");
            ReadTo(null);
            Where("the face refused");
        }
        else
        {
            story.Set("FLAG_HIDE_ACUITY_LAKEFRONT_RIVAL");
            story.SetVar("VAR_ACUITY_LAKEFRONT_STATE", 1);
        }

        if (stage <= 1)
        {
            // ---- Snowpoint City: the town, its temple and the guard, the quay
            At("Sinnoh", 366, 226, Direction.Up); Frames(30); Shot("sn10_snowpoint_city");
            At("Sinnoh", 366, 201, Direction.Up); Frames(30); Shot("sn11_snowpoint_the_temple");
            StepOnto("Sinnoh", 366, 198, Direction.Up);
            ReadTo("chosen"); Frames(4); Shot("sn12_snowpoint_the_guard");
            ReadTo(null); Frames(20);
            Shot("sn13_snowpoint_turned_back");
            Where("the temple");
            TalkTo("Sinnoh", "sailor_ss_spiral", "snowpoint_city", 0, -1);
            ReadTo("Battle Zone"); Frames(4); Shot("sn14_snowpoint_the_sailor");
            ReadTo(null);
            Where("the quay");

            // ---- the Pokémon Center: Maylene thawing out
            ComeThrough("SnowpointPokemonCenter", 8, 12, Direction.Up);
            Frames(30); Shot("sn20_pokemon_center");
            TalkTo("SnowpointPokemonCenter", "maylene", null, 1, 0);
            ReadTo("Brrr"); Frames(4); Shot("sn21_pokemon_center_maylene");
            ReadTo("Fighting type"); Frames(4); Shot("sn22_pokemon_center_maylene_on_candice");
            ReadTo(null);
            TalkTo("SnowpointPokemonCenter", "psyduck", null, -1, 0);
            ReadTo("Gwa"); Frames(4); Shot("sn23_pokemon_center_the_psyduck");
            ReadTo(null);
            ComeThrough("SnowpointPokeMart", 3, 11, Direction.Up);
            Frames(30); Shot("sn24_mart");

            // ---- the Gym: Candice and the Icicle Badge (the Gym itself is plan 01 · M9's; `gyms snowpoint` shows its ice)
            TalkTo("SnowpointGym", "candice", null);
            ReadTo("challenger"); Frames(4); Shot("sn30_gym_candice");
            IntoBattle("Candice");
            ToMainMenu(game.Battle); Shot("sn31_gym_candices_battle");
            Win();
            ReadTo("Icicle Badge from Candice"); Frames(4); Shot("sn32_gym_the_icicle_badge");
            ReadTo("Avalanche"); Frames(4); Shot("sn33_gym_tm72");
            ReadTo(null); Frames(10);
            Console.WriteLine($"gym: icicle badge {story.HasBadge("Sinnoh", "Icicle")}, TM72 {bag.GetQuantity(ItemDatabase.Get("TM72")!)}, {Flags("FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS")}");

            // ---- Maylene once the Badge is won
            ComeThrough("SnowpointPokemonCenter", 8, 12, Direction.Up);
            TalkTo("SnowpointPokemonCenter", "maylene", null, 1, 0);
            ReadTo("That's the Icicle Badge"); Frames(4); Shot("sn34_pokemon_center_maylene_after_the_badge");
            ReadTo(null);
        }
        else
        {
            story.GiveBadge("Sinnoh", "Icicle");
            story.Set("FLAG_RECEIVED_CANDICE_TM72");
            story.Set("FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS");
        }

        // ---- Acuity Lakefront with the Badge: Maylene goes home to her Gym
        ComeThrough("Sinnoh", 320, 246, Direction.Left);
        ReadTo(null, 300);
        Console.WriteLine($"lakefront with the badge: {Flags("FLAG_HIDE_SNOWPOINT_CITY_POKECENTER_1F_MAYLENE", "FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE")}");
        if (stage <= 1)
        {
            ComeThrough("SnowpointPokemonCenter", 8, 12, Direction.Up);
            Frames(30); Shot("sn35_pokemon_center_maylene_gone_home");
            Console.WriteLine($"pokemon center: maylene there {Someone("SnowpointPokemonCenter", "maylene", null) != null}; veilstone gym: maylene there {Someone("VeilstoneGym", "maylene", null) != null}");
        }

        // ---- Rock Climb up the face, and the way into the lake
        At("Sinnoh", 310, 242, Direction.Up); Frames(20);
        game.Interact();
        ReadTo("Use Rock Climb?"); Frames(4); Shot("sn40_lakefront_rock_climb_asked");
        ReadTo("used Rock Climb"); Frames(4); Shot("sn41_lakefront_rock_climb_used");
        ReadTo(null);
        Until(() => Me().GridY <= 237, "the climb's end", 1500);
        Frames(40); Shot("sn42_lakefront_at_the_head_of_the_face");
        Where("the climb");
        At("Sinnoh", 309, 231, Direction.Up); Frames(30); Shot("sn43_lakefront_the_way_into_the_lake");
        Console.WriteLine($"lakefront: way in leads to {game.Map.GetWarpAt(308, 229)?.TargetMap} and {game.Map.GetWarpAt(309, 229)?.TargetMap}");

        // ---- Lake Acuity: Commander Jupiter has beaten the rival; she comes down to the player and leaves, and so does he
        ComeThrough("LakeAcuityGalactic", 14, 49, Direction.Up);
        ReadTo("not getting away"); Frames(4); Shot("sn50_lake_the_rival_and_jupiter");
        ReadTo("so weak"); Frames(4); Shot("sn51_lake_jupiter_mocks_him");
        ReadTo("Back to headquarters"); Frames(4); Shot("sn52_lake_back_to_veilstone");
        Box().Advance();
        for (int i = 0; i < 70; i++) Quick();
        Frames(2); Shot("sn53_lake_jupiter_comes_down_the_shore");
        ReadTo("Eterna City"); Frames(4); Shot("sn54_lake_jupiter_before_the_player");
        ReadTo("don't waste your time"); Frames(4); Shot("sn55_lake_jupiters_warning");
        ReadTo("couldn't do a thing"); Frames(4); Shot("sn56_lake_the_rival_beaten");
        ReadTo("Uxie"); Frames(4); Shot("sn57_lake_uxie_was_hurting");
        ReadTo("get tougher"); Frames(4); Shot("sn58_lake_the_rival_resolves");
        ReadTo("doesn't give up"); Frames(4); Shot("sn59_lake_a_heart_that_doesnt_give_up");
        Box().Advance();
        for (int i = 0; i < 60; i++) Quick();
        Frames(2); Shot("sn60_lake_the_rival_leaves");
        ReadTo(null); Frames(20);
        Shot("sn61_lake_alone");
        Where("the lake");
        Console.WriteLine($"lake acuity: state {story.Var("VAR_LAKE_ACUITY_STATE")}, "
            + Flags("FLAG_HIDE_LAKE_ACUITY_JUPITER", "FLAG_HIDE_LAKE_ACUITY_RIVAL", "FLAG_ALT_MUSIC_LAKE_ACUITY", "FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY",
                "FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_ROWAN", "FLAG_HIDE_LAKE_VERITY_PROF_ROWAN", "FLAG_HIDE_LAKE_VERITY_COUNTERPART", "FLAG_LAKE_VALOR_EXPLODED"));

        // ---- the lake afterwards, and the TM on its far shore
        ComeThrough("LakeAcuityGalactic", 15, 49, Direction.Up);
        ReadTo(null, 300);
        Frames(30); Shot("sn62_lake_the_shore_empty");
        var ball = MapDatabase.Get("LakeAcuityGalactic").NPCs.FirstOrDefault(n => n.IsItemBall);
        if (ball != null)
        {
            At("LakeAcuityGalactic", ball.GridX + 1, ball.GridY, Direction.Left); Frames(30); Shot("sn63_lake_the_tm_on_the_far_shore");
            game.Interact();
            ReadTo("TM14"); Frames(4); Shot("sn64_lake_tm14");
            ReadTo(null);
            Console.WriteLine($"lake acuity: TM14 {bag.GetQuantity(ItemDatabase.Get("TM14")!)}");
        }
        else Console.WriteLine("  !! no item ball on the lake's far shore");

        // ---- the scene again from the east tile of the way in, as it would have played from there
        story.SetVar("VAR_LAKE_ACUITY_STATE", 1);
        story.Unset("FLAG_HIDE_LAKE_ACUITY_JUPITER");
        story.Unset("FLAG_HIDE_LAKE_ACUITY_RIVAL");
        // Whoever a scene walked off stays where it left them (they only ever look one way): back to where the lake has them
        foreach (var (key, x, y, facing) in new[] { ("jupiter", 17, 44, Direction.Down), ("rival", 17, 45, Direction.Up) })
        {
            var who = MapDatabase.Get("LakeAcuityGalactic").Everyone.First(n => n.Key == key);
            (who.GridX, who.GridY, who.Facing) = (x, y, facing);
        }
        Frames(2);
        ComeThrough("LakeAcuityGalactic", 15, 49, Direction.Up);
        ReadTo("Eterna City"); Frames(4); Shot("sn70_lake_from_the_east_tile_jupiter");
        ReadTo("couldn't do a thing"); Frames(4); Shot("sn71_lake_from_the_east_tile_the_rival");
        ReadTo(null); Frames(20);
        Where("the lake, from the east tile");
        Console.WriteLine($"lake acuity again: state {story.Var("VAR_LAKE_ACUITY_STATE")}, {Flags("FLAG_HIDE_LAKE_ACUITY_JUPITER", "FLAG_HIDE_LAKE_ACUITY_RIVAL")}");

        // ---- the lake as it was before Team Galactic came, where the rival keeps watch (no way leads there once Mars is beaten)
        ComeThrough("LakeAcuity", 14, 49, Direction.Up);
        Frames(30); Shot("sn80_lake_as_it_was_the_rival_on_watch");
        TalkTo("LakeAcuity", "rival", null);
        ReadTo("nothing going on"); Frames(4); Shot("sn81_lake_as_it_was_the_rival");
        ReadTo(null);
    }
}
