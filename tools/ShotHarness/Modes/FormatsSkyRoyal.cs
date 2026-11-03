partial class Harness
{
    // Plan 14 · B2, a sky battle (ft50 to ft52), and B13, the Battle Royal (ft70 to ft73), last in `formats` so nothing
    // before them moves. Each is a trainer, or three, of the harness's own met through the game's own StartTrainerBattle
    // (and Driver.StartBattleRoyal) in Route 225.
    //
    // The sky: a trainer whose Format is "sky" (none of the game's is) against a team of a Golem, a Staraptor and a
    // Gyarados: the opening line over the sky's stage (ft50_sky_intro), the move menu with Gyarados's Earthquake greyed
    // (ft51_sky_moves), the line refusing it (ft51b_sky_refused) and a battle won; then the same trainer met with a team
    // of none that fly, the line that turns the player away (ft52_sky_none). It prints the format, the conditions, the
    // team the battle took and whether the Vs. Recorder's file replays.
    void SkyShots()
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
        Pokemon Mine(string species, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, 50);
            p.Moves.Clear();
            foreach (string move in moves) p.Moves.Add(new Move(MoveDatabase.Get(move)));
            return p;
        }
        team.Members.Clear();
        team.Members.Add(Mine("Golem", "Rock Slide", "Earthquake"));
        team.Members.Add(Mine("Gyarados", "Earthquake", "Waterfall", "Ice Fang"));
        team.Members.Add(Mine("Staraptor", "Brave Bird", "Close Combat"));

        Trainer Gale()
        {
            var t = new Trainer
            {
                Id = "harness_sky", Name = "Gale", TrainerClass = "Ace Trainer", PrizeMoney = 0, Format = "sky",
                Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic
            };
            foreach (string species in new[] { "Staravia", "Drifblim" }) t.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 44));
            return t;
        }
        var (gx, gy) = GrassOf("route_225");
        At("Sinnoh", gx, gy, Direction.Up);
        game.EnterArea();
        game.LocationSign.Hide();
        Frames(4);
        game.StartTrainerBattle(new NPC { Name = "Gale", NpcType = "Trainer", TrainerData = Gale() });
        for (int i = 0; i < 900 && game.State != GameState.Battle; i++) Quick();
        if (game.State != GameState.Battle) { Console.WriteLine("  !! never happened: the sky battle"); return; }
        var sky = game.Battle;
        Console.WriteLine($"the sky battle: format {sky.Format}; in the air {sky.Core.InTheAir}; the team it took {string.Join(", ", sky.PlayerParty.Members.Select(p => p.Species.Name))}");
        string opening = PokemonPlatinumEngine.Battle.Sim.BattleCore.SkyLine;
        for (int i = 0; i < 80 && !sky.IsBattleOver && sky.HUD.MenuState != BattleMenuState.Main; i++)
        {
            if (sky.CurrentMessage == opening) { Skip(0.4); Shot("ft50_sky_intro"); break; }
            Confirm(sky);
            Skip(0.3);
        }
        ReadOn(sky);
        if (sky.HUD.MenuState == BattleMenuState.Main)
        {
            sky.SelectMainMenuOption(0);
            Frames(10);
            Shot("ft51_sky_moves");
            sky.SelectMove(0);
            Frames(10);
            Shot("ft51b_sky_refused");
            Console.WriteLine($"  Earthquake refused: {sky.CurrentMessage}");
        }
        for (int guard = 0; guard < 300 && !sky.IsBattleOver; guard++)
        {
            if (sky.HUD.MenuState == BattleMenuState.Moves) sky.HUD.MenuState = BattleMenuState.Main;
            if (sky.HUD.MenuState == BattleMenuState.Main)
            {
                foreach (var p in team.Members.Where(p => !p.IsFainted)) p.CurrentHP = p.MaxHP;
                foreach (var foe in sky.EnemySlots.Select(s => s.Pokemon).Where(p => p is { CurrentHP: > 0 })) foe!.CurrentHP = 1;
                sky.SelectMainMenuOption(0);
                sky.SelectMove(1);
            }
            else Confirm(sky);
            Skip(0.4);
        }
        Console.WriteLine($"the sky battle's end: {sky.Result}; record in the air {sky.Core.Record?.InTheAir}");
        for (int i = 0; i < 1500 && game.State != GameState.Overworld; i++) { if (!FieldAgain()) Quick(); }
        Frames(10);

        // Met again with nobody that flies: no battle, a line
        var ground = team.Members.ToList();
        team.Members.RemoveAll(p => PokemonPlatinumEngine.Data.SkyBattles.CanFly(p));
        game.StartTrainerBattle(new NPC { Name = "Gale", NpcType = "Trainer", TrainerData = Gale() });
        Frames(30);
        Shot("ft52_sky_none");
        Console.WriteLine($"  with none that fly: state {game.State}");
        for (int i = 0; i < 600 && (game.State == GameState.Dialogue || game.Dialogue.IsActive); i++)
        {
            if (!game.Dialogue.IsCurrentLineComplete) game.Dialogue.FinishLine(); else game.Dialogue.Advance();
            Quick();
        }
        team.Members.Clear();
        team.Members.AddRange(ground);
        Frames(10);
    }

    // The Battle Royal: three trainers of the harness's own, each for themselves, met through Driver.StartBattleRoyal: the
    // four out with the opening line (ft70_royal_intro), Tackle's target menu over the three others (ft71_royal_targets),
    // a turn's lines with the others at one another (ft72_royal_turn) and the placing as it is said (ft73_royal_placed).
    // It prints the format, every camp's score and place, the result and whether the Vs. Recorder's file replays.
    void RoyalShots()
    {
        var team = game.Party;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            engine.Draw();
        }
        Pokemon Mine(string species, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, 50);
            p.Moves.Clear();
            foreach (string move in moves) p.Moves.Add(new Move(MoveDatabase.Get(move)));
            return p;
        }
        team.Members.Clear();
        team.Members.Add(Mine("Luxray", "Spark", "Crunch"));
        team.Members.Add(Mine("Gyarados", "Waterfall", "Ice Fang"));
        team.Members.Add(Mine("Golem", "Rock Slide", "Tackle"));

        NPC Other(string name, string trainerClass, params string[] species)
        {
            var t = new Trainer
            {
                Id = "harness_royal_" + name.ToLowerInvariant(), Name = name, TrainerClass = trainerClass, PrizeMoney = 0,
                Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic
            };
            foreach (string s in species) t.Party.Add(new Pokemon(PokemonDatabase.Get(s)!, 44));
            return new NPC { Name = name, NpcType = "Trainer", TrainerData = t };
        }
        var (gx, gy) = GrassOf("route_225");
        At("Sinnoh", gx, gy, Direction.Up);
        game.EnterArea();
        game.LocationSign.Hide();
        Frames(4);
        game.StartBattleRoyal(Other("Ana", "Ace Trainer", "Staravia", "Bibarel", "Ponyta"), Other("Bo", "Ace Trainer", "Shinx", "Buizel", "Budew"),
            Other("Cy", "Ace Trainer", "Machop", "Geodude", "Psyduck"));
        for (int i = 0; i < 900 && game.State != GameState.Battle; i++) Quick();
        if (game.State != GameState.Battle) { Console.WriteLine("  !! never happened: the Battle Royal"); return; }
        var royal = game.Battle;
        Console.WriteLine($"the Battle Royal: format {royal.Format} (rules {royal.Rules.Preset}); others {string.Join(", ", royal.EnemySlots.Select(s => s.Trainer?.Name))}");
        string begins = Text.Of("battle.royalBegins");
        bool introShot = false;
        for (int i = 0; i < 80 && !royal.IsBattleOver && royal.HUD.MenuState != BattleMenuState.Main; i++)
        {
            if (!introShot && royal.CurrentMessage == begins) { Skip(0.4); Shot("ft70_royal_intro"); introShot = true; }
            Confirm(royal);
            Skip(0.3);
        }
        if (!introShot) Console.WriteLine("  !! never said: the Royal's opening");
        if (royal.HUD.MenuState == BattleMenuState.Main)
        {
            royal.SelectMainMenuOption(0);
            royal.SelectMove(0);
            Frames(10);
            Shot("ft71_royal_targets");
            royal.SelectTarget(0);
            for (int i = 0; i < 60 && !royal.IsBattleOver && royal.HUD.MenuState == BattleMenuState.Message; i++)
            {
                if (i == 6) Shot("ft72_royal_turn");
                Confirm(royal);
                Skip(0.4);
            }
        }
        bool placedShot = false;
        for (int guard = 0; guard < 600 && !royal.IsBattleOver; guard++)
        {
            if (royal.HUD.MenuState == BattleMenuState.Main)
            {
                foreach (var p in team.Members.Where(p => !p.IsFainted)) p.CurrentHP = p.MaxHP;
                royal.SelectMainMenuOption(0);
                royal.SelectMove(0);
                if (royal.HUD.MenuState == BattleMenuState.SelectTarget) royal.SelectTarget(0);
            }
            else
            {
                if (!placedShot && royal.CurrentMessage?.StartsWith(Text.Of("battle.royalPlaced", ("place", 1), ("trainer", ""), ("score", 0))[..4]) == true)
                {
                    Skip(0.4);
                    Shot("ft73_royal_placed");
                    placedShot = true;
                }
                Confirm(royal);
            }
            Skip(0.4);
        }
        var core = royal.Core;
        foreach (int camp in core.RoyalStanding)
            Console.WriteLine($"  {core.RoyalName(camp)}: {core.RoyalKnockOuts(camp)} knock-out(s), score {core.RoyalScore(camp)}");
        Console.WriteLine($"the Battle Royal's end: {royal.Result}; the player placed {core.RoyalPlace}; turns {core.Turn}");
        if (!placedShot) Console.WriteLine("  !! never shot: the placing");
        if (royal.RecordFile(GameClock.Today) is { } file)
        {
            var again = file.Replay();
            bool same = again.Log.OfType<Said>().Select(s => s.Text).SequenceEqual(core.Log.OfType<Said>().Select(s => s.Text));
            Console.WriteLine($"  the Vs. Recorder's file replays to the same lines: {same}{(same ? "" : "  !!")}");
        }
        else if (royal.ChangedFromOutside) Console.WriteLine("  the harness changed the battle's Pokémon in the middle, so the Vs. Recorder keeps no file of it (BattleRoyalTests replays one fought untouched)");
        for (int i = 0; i < 1500 && game.State != GameState.Overworld; i++) { if (!FieldAgain()) Quick(); }
        Frames(10);
    }
}
