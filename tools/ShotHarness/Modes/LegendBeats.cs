using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Plan 29's beats of Hoenn and Unova (LS6 to LS9) by themselves (not part of <c>all</c>): each legendary's meeting
    /// started on its own place with nothing else of the story played, shot a fifth of a second apart through the look and
    /// the sign (<c>lb_&lt;who&gt;_tNN</c>), at every line said (<c>_lineNN</c>), as the battle opens and at its menu
    /// (<c>_battle_open</c>, <c>_battle</c>). It prints where the Pokémon stands, the rig at each shot and what was left
    /// out. <c>BEATS_ONLY=rayquaza,kyurem</c> shoots those alone. <c>contact lb_&lt;who&gt;</c> puts a beat on a sheet.
    /// </summary>
    public void LegendBeatsMode()
    {
        var only = (Environment.GetEnvironmentVariable("BEATS_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        bool Wanted(string who) => only.Count == 0 || only.Contains(who);
        var story = game.Story;

        void Begin(string region)
        {
            engine.NewGameRegion = region;
            engine.StartNewGame();
            if (region == "Hoenn") game.Story.SetVar("VAR_LITTLEROOT_INTRO_STATE", 7);
            game.State = GameState.Overworld;
            game.SaveSlot = 1;
            game.LocationSign.Hide();
            engine.Settings.TimeOfDay = TimeOfDay.Day;
            engine.ApplySettings(window: false);
            var lead = new Pokemon(PokemonDatabase.Get("Garchomp")!, 90);
            lead.ReplaceMove(0, "Dragon Claw");
            game.Party.Add(lead);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Lucario")!, 90));
            Frames(4);
        }

        // ---- Hoenn
        string[] hoenn = { "rayquaza", "kyogre", "groudon", "regirock", "regice", "registeel", "latios", "deoxys", "mew", "lugia", "hooh" };
        if (hoenn.Any(Wanted))
        {
            Begin("Hoenn");
            story.Set("HoennHallOfFame");
            if (Wanted("rayquaza"))
                Beat("rayquaza", "SkyPillar_Top", "sky_pillar_top", "rayquaza_still", "Rayquaza", 14, 8, Direction.Up,
                    () => { story.SetVar("VAR_SKY_PILLAR_STATE", 2); story.Set("FLAG_HIDE_SKY_PILLAR_TOP_RAYQUAZA"); story.SetVar("VAR_SKY_PILLAR_RAYQUAZA_CRY_DONE", 1); });
            if (Wanted("kyogre")) Beat("kyogre", "MarineCave_End", "marine_cave_end", null, "Kyogre", 9, 26, Direction.Up);
            if (Wanted("groudon")) Beat("groudon", "TerraCave_End", "terra_cave_end", null, "Groudon", 17, 26, Direction.Up);
            if (Wanted("regirock")) Beat("regirock", "DesertRuins", "desert_ruins", "regirock", "Regirock", 8, 8, Direction.Up);
            if (Wanted("regice")) Beat("regice", "IslandCave", "island_cave", "regice", "Regice", 8, 8, Direction.Up);
            if (Wanted("registeel")) Beat("registeel", "AncientTomb", "ancient_tomb", "registeel", "Registeel", 8, 8, Direction.Up);
            if (Wanted("latios"))
                Beat("latios", "SouthernIsland_Interior", "southern_island_interior", null, "Stone", 13, 12, Direction.Up,
                    () => { story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND"); story.SetVar("VAR_ROAMER_POKEMON", 0); });
            if (Wanted("deoxys"))
                Beat("deoxys", "BirthIsland_Exterior", "birth_island_exterior", "birth_island_exterior_rock", "Triangle", 15, 11, Direction.Up,
                    after: () =>
                    {
                        engine.StartScript(ScriptParser.Parse("birth_island_exterior", "script T\nplace birth_island_exterior_rock 15 10\nend")[0]);
                        Frames(4);
                        story.SetVar("VAR_DEOXYS_ROCK_LEVEL", 10);
                    });
            if (Wanted("mew")) Beat("mew", "FarawayIsland_Interior", "faraway_island_interior", "faraway_island_mew", "Mew", 13, 15, Direction.Up);
            if (Wanted("lugia")) Beat("lugia", "NavelRock_Bottom", "navel_rock_bottom", "navel_rock_lugia", "Lugia", 11, 14, Direction.Up);
            if (Wanted("hooh")) Beat("hooh", "NavelRock_Top", "navel_rock_top", null, "HoOh", 12, 10, Direction.Up);
        }

        // ---- Unova
        string[] unova = { "fusion", "cobalion", "virizion", "terrakion", "azelf", "mesprit", "kyurem", "heatran", "regirock_u", "regice_u", "registeel_u" };
        if (unova.Any(Wanted))
        {
            Begin("Unova");
            if (Wanted("fusion"))
                Beat("fusion", "GiantChasm_Cavern", "giant_chasm_6", null, "Ghetsis", 15, 25, Direction.Up,
                    () =>
                    {
                        story.SetVar("VAR_UNOVA_CHASM", 3);
                        story.Unset("FLAG_UNOVA_HIDE_GHETSIS_CAVERN");
                        story.Unset("FLAG_UNOVA_HIDE_KYUREM_CAVERN");
                        story.Set("FLAG_UNOVA_HIDE_BLACK_KYUREM");
                        story.Set("FLAG_UNOVA_HIDE_ZEKROM_CAVERN");
                        story.Set("FLAG_UNOVA_HIDE_N_CAVERN");
                    }, everyLine: true);
            story.Set("UnovaHallOfFame");
            story.Set("FLAG_UNOVA_MAGMA_STONE_SET");
            if (Wanted("cobalion")) Beat("cobalion", "Unova", "unova_route_13", "21", "Cobalion", 684, 191, Direction.Up);
            if (Wanted("virizion")) Beat("virizion", "Unova", "unova_route_11", "0", "Virizion", 468, 171, Direction.Up);
            if (Wanted("terrakion")) Beat("terrakion", "Unova", "unova_route_22", "6", "Terrakion", 725, 139, Direction.Up);
            if (Wanted("azelf")) Beat("azelf", "Unova", "unova_route_23", "16", "Azelf", 584, 81, Direction.Up);
            if (Wanted("mesprit")) Beat("mesprit", "CelestialTower_Summit", "celestial_tower", "0", "Mesprit", 16, 23, Direction.Up);
            if (Wanted("kyurem")) Beat("kyurem", "GiantChasm_Cavern", "giant_chasm_6", "kyurem_rest", "KyuremRests", 15, 18, Direction.Up);
            if (Wanted("heatran")) Beat("heatran", "ReversalMountain_4", "reversal_mountain_5", "0", "Heatran", -1, -1, Direction.Up);
            if (Wanted("regirock_u")) Beat("regirock_u", "RockPeakChamber", "rock_peak_chamber", "0", "Regirock", -1, -1, Direction.Up);
            if (Wanted("regice_u")) Beat("regice_u", "IcebergChamber", "iceberg_chamber", "0", "Regice", -1, -1, Direction.Up);
            if (Wanted("registeel_u")) Beat("registeel_u", "IronChamber", "iron_chamber", "0", "Registeel", -1, -1, Direction.Up);
        }
        engine.NewGameRegion = "Sinnoh";
    }

    /// <summary>
    /// One beat: the place come to with its people shown, the script started (on its Pokémon, or as a trigger with none),
    /// and shots through it. A player tile of -1 stands the player two tiles south of the Pokémon.
    /// </summary>
    void Beat(string who, string mapName, string area, string? key, string script, int px, int py, Direction facing,
        Action? before = null, Action? after = null, bool everyLine = false)
    {
        var box = game.Dialogue;
        string prefix = "lb_" + who;
        Console.WriteLine($"== {who}: {mapName} ({area}), {script}");
        before?.Invoke();
        var map = MapDatabase.Get(mapName);
        NPC? Find(Map m) => key == null ? null : m.Everyone.FirstOrDefault(n => n.Key == key && n.ScriptFile == area);
        if (Find(map) is { } hidden)
        {
            if (hidden.HiddenBy != null) game.Story.Unset(hidden.HiddenBy);
            if (hidden.ShownBy != null) game.Story.Set(hidden.ShownBy);
            if (px < 0) { px = hidden.GridX; py = hidden.GridY + 1; }
        }
        At(mapName, px, py, facing);
        game.ArriveOnMap();
        for (int i = 0; i < 600 && (engine.ScriptRunning || box.IsActive); i++)
        {
            if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
            else Frames(1);
        }
        At(mapName, px, py, facing);
        Frames(10);
        after?.Invoke();
        var subject = Find(game.Map);
        if (key != null && (subject == null || !game.Map.NPCs.Contains(subject)))
        {
            Console.WriteLine($"  !! {who}: {key} of {area} is not on {game.Map.Name}");
            Shot(prefix + "_missing");
            return;
        }
        Console.WriteLine($"  player {game.Player.GridX},{game.Player.GridY}" + (subject != null ? $", {key} at {subject.GridX},{subject.GridY}" : ""));
        Shot(prefix + "_t00");
        if (!engine.StartScript(script, subject, file: area)) { Console.WriteLine($"  !! no script {script}"); return; }
        Frames(1);
        Console.WriteLine($"  started: running {engine.ScriptRunning}, state {game.State}, box {box.IsActive}, script {game.ScriptNow}");

        int t = 1, line = 0, idle = 0, battles = 0;
        string? lastLine = null;
        for (int guard = 0; guard < 6000; guard++)
        {
            if (game.State == GameState.Battle && game.Battle is { } fight)
            {
                Skip(0.5); Shot($"{prefix}_battle_open");
                ToMainMenu(fight); Shot($"{prefix}_battle");
                BeatWin(fight);
                battles++;
                if (!everyLine) break;
                continue;
            }
            if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); continue; }
            if (box.IsActive)
            {
                if (!box.IsCurrentLineComplete) { Frames(1); continue; }
                if (box.CurrentLine != lastLine) { Frames(6); Shot($"{prefix}_line{line++:00}"); lastLine = box.CurrentLine; }
                if (box.IsQuestion) { Frames(1); continue; }
                box.Advance(); Frames(2);
                continue;
            }
            idle = engine.ScriptRunning || game.State != GameState.Overworld ? 0 : idle + 1;
            if (idle > 30) break;
            Frames(12);
            if (t < 60 && game.State == GameState.Overworld) Shot($"{prefix}_t{t++:00}");
        }
        // read on through whatever is left (the line after a battle)
        for (int i = 0; i < 1200 && (engine.ScriptRunning || box.IsActive || game.State != GameState.Overworld); i++)
        {
            if (game.State == GameState.Battle && game.Battle is { } f2) { BeatWin(f2); continue; }
            if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
            else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
            else Frames(1);
        }
        game.State = GameState.Overworld;
        Console.WriteLine($"  {who}: {t} timed shots, {line} lines, {battles} battle(s)");
    }

    void BeatWin(BattleEngine fight)
    {
        for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
        {
            if (fight.HUD.MenuState == BattleMenuState.Main)
            {
                foreach (var foe in (fight.EnemyParty?.Members ?? new List<Pokemon>()).Concat(fight.EnemySlots.Select(s => s.Pokemon)))
                    if (foe != null) { foe.CurrentHP = Math.Min(foe.CurrentHP, 1); foe.HeldItem = null; foreach (var move in foe.Moves) move.CurrentPP = 0; }
                if (fight.EnemyPokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                var lead = fight.PlayerPokemon;
                if (lead != null) { lead.CurrentHP = lead.MaxHP; foreach (var move in lead.Moves) move.CurrentPP = move.MaxPP; }
                fight.SelectMainMenuOption(0);
                int hurts = lead == null ? 0 : Math.Max(0, lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status));
                fight.SelectMove(hurts);
                if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
            }
            else Confirm(fight);
            Skip(0.5);
        }
        for (int i = 0; i < 1500 && !FieldAgain(); i++) Frames(1);
        Frames(10);
    }
}
