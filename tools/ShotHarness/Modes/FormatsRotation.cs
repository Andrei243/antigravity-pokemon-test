partial class Harness
{
    // Plan 14 · B7, a rotation battle (ft40 to ft42), last in `formats` so nothing before it moves: a trainer of the
    // harness's own who battles on a turntable (Trainer.Format "rotation", which no trainer of the game has) met through
    // the game's own StartTrainerBattle in Route 225; the commands with the rotation arrows armed toward the seat after
    // the front, its name on FIGHT (ft40); the move menu of the Pokémon brought forward (ft40b); the turntable turning as
    // its line is read, the camera on it (ft41); then two more turns the other ways (ft42 the third) and the battle won.
    // It prints the format, each side's turntable at each menu, the turns said, the record's turns and whether the
    // Vs. Recorder's file of it replays to the same lines.
    void RotationShots()
    {
        var team = game.Party;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            engine.Draw();
        }
        void ReadOn(BattleEngine b)
        {
            for (int i = 0; i < 120 && !b.IsBattleOver && b.HUD.MenuState != BattleMenuState.Main; i++) { Confirm(b); Skip(0.4); }
        }
        string Table(BattleEngine b, BattleSide side) => string.Join(" | ", b.Core.Turntable(side).Select(p => p == null ? "-" : $"{p.Species.Name} {p.CurrentHP}/{p.MaxHP}"));
        Pokemon Mine(string species, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, 50);
            p.Moves.Clear();
            foreach (string move in moves) p.Moves.Add(new Move(MoveDatabase.Get(move)));
            return p;
        }
        team.Members.Clear();
        team.Members.Add(Mine("Golem", "Rock Slide", "Earthquake"));
        team.Members.Add(Mine("Gyarados", "Waterfall", "Ice Fang"));
        team.Members.Add(Mine("Luxray", "Spark", "Crunch"));

        var trainer = new Trainer
        {
            Id = "harness_rotation", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 0, Format = "rotation",
            Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic
        };
        foreach (string species in new[] { "Staravia", "Ponyta", "Bibarel" }) trainer.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 44));
        var (gx, gy) = GrassOf("route_225");
        At("Sinnoh", gx, gy, Direction.Up);
        game.EnterArea();
        game.LocationSign.Hide();
        Frames(4);
        game.StartTrainerBattle(new NPC { Name = trainer.Name, NpcType = "Trainer", TrainerData = trainer });
        for (int i = 0; i < 900 && game.State != GameState.Battle; i++) Quick();
        if (game.State != GameState.Battle) { Console.WriteLine("  !! never happened: the rotation battle"); return; }
        var rotation = game.Battle;
        Console.WriteLine($"the rotation: format {rotation.Format} (rules {rotation.Rules.Preset}); turntables {rotation.Anim.Turntables}");
        ToMainMenu(rotation);
        ReadOn(rotation);

        // Three turns: +1 (shot as the arrows are armed and as the table turns), then −1, then −1 again
        int[] ways = { 1, -1, -1 };
        for (int turn = 0; turn < ways.Length && !rotation.IsBattleOver; turn++)
        {
            ReadOn(rotation);
            if (rotation.HUD.MenuState != BattleMenuState.Main) break;
            foreach (var p in team.Members) { p.CurrentHP = p.MaxHP; foreach (var m in p.Moves) m.CurrentPP = m.MaxPP; }
            Console.WriteLine($"  turn {turn + 1} at the menu: mine {Table(rotation, BattleSide.Player)}; theirs {Table(rotation, BattleSide.Enemy)}");
            if (!rotation.RotateChoice(ways[turn])) Console.WriteLine($"  !! the table won't turn {ways[turn]:+0;-0}");
            var fighter = rotation.Fighter;
            Frames(10);
            if (turn == 0)
            {
                Skip(0.6);
                Shot("ft40_rotation_menu");
            }
            rotation.SelectMainMenuOption(0);
            Frames(10);
            if (turn == 0) Shot("ft40b_rotation_moves");
            rotation.SelectMove(0);
            string turning = Text.Of("battle.rotatedToTheFront", ("pokemon", fighter.DisplayName));
            bool shot = false;
            for (int i = 0; i < 60 && !rotation.IsBattleOver && rotation.HUD.MenuState == BattleMenuState.Message; i++)
            {
                Skip(0.05);
                if (!shot && rotation.CurrentMessage == turning)
                {
                    Skip(BattleAnimator.RotationTime * 0.45);
                    if (turn == 0) Shot("ft41_rotation_turn");
                    if (turn == 2) Shot("ft42_rotation_third_turn");
                    Console.WriteLine($"  said: {turning} (the table at {rotation.Anim.TurntableTurn(BattleSide.Player):0.00} thirds)");
                    shot = true;
                }
                Confirm(rotation);
                Skip(0.3);
            }
            if (!shot) Console.WriteLine($"  !! never said: {turning}");
        }

        // The rest of the trainer's three felled at a single HP each
        for (int guard = 0; guard < 300 && !rotation.IsBattleOver; guard++)
        {
            if (rotation.HUD.MenuState == BattleMenuState.Main)
            {
                foreach (var p in team.Members.Where(p => !p.IsFainted)) p.CurrentHP = p.MaxHP;
                foreach (var foe in rotation.EnemySlots.Select(s => s.Pokemon).Where(p => p is { CurrentHP: > 0 })) foe!.CurrentHP = 1;
                rotation.SelectMainMenuOption(0);
                rotation.SelectMove(0);
            }
            else Confirm(rotation);
            Skip(0.4);
        }
        var record = rotation.Core.Record;
        Console.WriteLine($"the rotation's end: {rotation.Result}; turns {rotation.Core.Turn}; record {record?.Format} with "
            + $"{record?.Answers.SelectMany(a => a).Count(c => c.Rotation != 0)} turn(s) of the player's table; the foe's turned "
            + $"{rotation.Core.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<Rotated>().Count(r => r.Place.Side == BattleSide.Enemy)} time(s)");
        if (rotation.RecordFile(GameClock.Today) is { } file)
        {
            var again = file.Replay();
            bool same = again.Log.OfType<Said>().Select(s => s.Text).SequenceEqual(rotation.Core.Log.OfType<Said>().Select(s => s.Text));
            Console.WriteLine($"  the Vs. Recorder's file replays to the same lines: {same}{(same ? "" : "  !!")}");
        }
        else Console.WriteLine("  !! no record file");
        for (int i = 0; i < 1500 && game.State != GameState.Overworld; i++) { if (!FieldAgain()) Quick(); }
        Frames(10);
    }
}
