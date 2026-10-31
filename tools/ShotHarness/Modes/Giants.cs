partial class Harness
{
    // ---------------------------------------------------------------- giants (plan 25 · YD0)

    // giants: the baseline of plan 25 · Y-dynamax-punch, how a Dynamax looks today when a trainer's AI does it and a
    // player only watches (gi00 to gi2x). Three parts, each a battle begun through the game's own StartTrainerBattle in
    // Route 225's grass under the modern rules, the player's side a tank that is made whole at every menu and only
    // Splashes, so the foe's choices are the AI's alone:
    //   1. Cynthia's Mega Max team (champion_cynthia_megamax, the record the game fights once the player holds the Mega
    //      Max Band), her Garchomp put at the head of the team so it comes out first: the Mega Evolution, the Mega
    //      Dynamax and the Max Move of the AI (gi00_*), the next Max Move, the end at the third turn's (gi01_*);
    //   2. a foe of the harness's own with the Dynamax Band alone (a Dynamax and a Gigantamax-less Snorlax) (gi10_*);
    //   3. a double battle where one foe grows and the other does not, the player's two beside (gi20_*);
    //   4. the player's own Dynamax through the button, by day and at night, and a Gigantamax with its end (gi3x_*,
    //      plan 25 · YD1).
    // Every cue that starts is shot at the same moments from its start: gi<part>_<what>_<n>, with its kinds, quads, the
    // camera's shot and the giant's sky printed; a growth with a trainer's call (YD1) has four shots of the call first
    // (_call_1 to _4) and then the growth's at the same moments of the growth as YD0's. Not part of "all".
    // GIANTS_PART=cynthia|dynamax|double|own runs one part.
    public void GiantsMode()
    {
        string only = Environment.GetEnvironmentVariable("GIANTS_PART") ?? "";
        var team = game.Party;

        Pokemon Tank(string species, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, 100);
            p.Moves.Clear();
            foreach (string m in moves) p.Moves.Add(new Move(MoveDatabase.Get(m)));
            return p;
        }

        void Whole(BattleEngine b)
        {
            foreach (var p in team.Members) { p.CurrentHP = p.MaxHP; foreach (var m in p.Moves) m.CurrentPP = m.MaxPP; }
            foreach (var slot in b.PlayerSlots.Where(s => s.Pokemon != null)) slot.Pokemon!.CurrentHP = slot.Pokemon.MaxHP;
        }

        string Side(BattleEngine b, BattleSide side) => string.Join(", ", (side == BattleSide.Player ? b.PlayerSlots : b.EnemySlots)
            .Select(s => s.Pokemon == null ? $"{s.Slot}:-" : $"{s.Slot}:{s.Pokemon.Form ?? s.Pokemon.Species.Name} {s.Pokemon.CurrentHP}/{s.Pokemon.MaxHP}"));

        BattleEngine Begin(Trainer trainer, string look, params Pokemon[] mine)
        {
            team.Members.Clear();
            foreach (var p in mine) team.Members.Add(p);
            var (gx, gy) = GrassOf("route_225");
            At("Sinnoh", gx, gy, Direction.Up);
            game.EnterArea();
            game.LocationSign.Hide();
            Frames(4);
            game.StartTrainerBattle(new NPC { Name = trainer.Name, NpcType = look, TrainerData = trainer });
            for (int i = 0; i < 900 && game.State != GameState.Battle; i++)
            {
                FrameClock.Fixed = ++tick / 60.0;
                engine.Update(1f / 60f);
                engine.Draw();
            }
            if (game.State != GameState.Battle) { Console.WriteLine("  !! never happened: the battle"); return null!; }
            return game.Battle;
        }

        // Which cues of interest are playing now, and how many quads and ribbons the frame draws
        string Playing(BattleEngine b) => string.Join(", ", b.Anim.Cues
            .Where(c => c.Kind is CueKind.Mega or CueKind.Dynamax or CueKind.DynamaxEnd or CueKind.Tera || c.MaxMove)
            .Select(c => $"{(c.MaxMove ? "MaxMove " + c.Move : c.Kind.ToString())} {c.Age:0.00}s"));

        // Shots at moments counted from the first of the newest interesting cues, never earlier than they are found
        void Beat(BattleEngine b, string prefix, params double[] times)
        {
            double at = 0;
            for (int i = 0; i < times.Length; i++)
            {
                Skip(Math.Max(0, times[i] - at));
                at = times[i];
                Shot($"{prefix}_{i + 1}");
                Console.WriteLine($"  {prefix}_{i + 1} +{times[i]:0.00} s: {game.BattleRenderer.Effects.Count} quads and ribbons; camera {game.BattleRenderer.Shot}; sky {GiantSky.Presence(b.Anim):0.00}; playing: {Playing(b)}; "
                    + $"foes {Side(b, BattleSide.Enemy)}; giants own {string.Join("/", Enumerable.Range(0, b.Anim.SlotsOf(BattleSide.Player)).Select(s => b.Anim[BattleSide.Player, s].Dynamaxed))} foe {string.Join("/", Enumerable.Range(0, b.Anim.SlotsOf(BattleSide.Enemy)).Select(s => b.Anim[BattleSide.Enemy, s].Dynamaxed))}");
            }
        }

        // A growth's beats (plan 25 · YD1): with a trainer's call before it, four of the call (<prefix>_call_1 to _4) and
        // then the growth's own seven at the same moments of the growth as before the call was built, so a shot of YD0's
        // run and one of this compare the same moment
        void Growth(BattleEngine b, string prefix, EffectCue c)
        {
            if (c.Kind == CueKind.Dynamax && c.Lead > 0f)
            {
                double found = c.Age;
                Beat(b, prefix + "_call", 0.12 - found, 0.3 - found, 0.55 - found, 0.85 - found);
                double after = 0.85;
                double lead = c.Lead;
                Beat(b, prefix, lead + 0.2 - after, lead + 0.5 - after, lead + 0.75 - after, lead + 0.95 - after, lead + 1.3 - after, lead + 1.62 - after, lead + 2.2 - after);
                PrintCamera(b, prefix);
            }
            else
            {
                Beat(b, prefix, 0.2, 0.5, 0.75, 0.95, 1.3, 1.62, 2.2);
                PrintCamera(b, prefix);
            }
        }

        void PrintCamera(BattleEngine b, string prefix) =>
            Console.WriteLine($"  {prefix}: camera {game.BattleRenderer.Shot}, sky {GiantSky.Presence(b.Anim):0.00}, shown age of the growth {b.Anim.Cues.LastOrDefault(x => x.Kind == CueKind.Dynamax)?.ShownAge.ToString("0.00") ?? "-"}");

        // Plays the battle on, one turn after another with the player's first move (a Splash), reading each line for a
        // fifth of a second and shooting the beats of every growth, Max Move and end that starts. Returns the beats shot.
        int Watch(BattleEngine b, string part, int turns, int maxBeats)
        {
            var seen = new HashSet<EffectCue>();
            int beats = 0, grown = 0, moves = 0, ends = 0;
            for (int turn = 0; turn < turns && !b.IsBattleOver && beats < maxBeats; turn++)
            {
                if (b.HUD.MenuState == BattleMenuState.Main)
                {
                    Whole(b);
                    if (turn == 0 || grown > 0 && turn == 1) Shot($"{part}_menu_{turn}");
                    Console.WriteLine($"  turn {turn}: mine {Side(b, BattleSide.Player)}; theirs {Side(b, BattleSide.Enemy)}");
                    b.SelectMainMenuOption(0);
                    b.SelectMove(0);
                    if (b.HUD.MenuState == BattleMenuState.SelectTarget) b.SelectTarget(0);
                    if (b.HUD.MenuState == BattleMenuState.Main) { b.SelectMove(0); if (b.HUD.MenuState == BattleMenuState.SelectTarget) b.SelectTarget(0); }
                    Frames(1);
                }
                for (int i = 0; i < 40 && !b.IsBattleOver && b.HUD.MenuState is not (BattleMenuState.Main or BattleMenuState.SelectTarget); i++)
                {
                    Skip(0.05);
                    var fresh = b.Anim.Cues.Where(c => !seen.Contains(c) && (c.Kind is CueKind.Mega or CueKind.Dynamax or CueKind.DynamaxEnd || c.MaxMove)).ToList();
                    foreach (var c in fresh) seen.Add(c);
                    if (fresh.Count > 0)
                    {
                        var c = fresh[0];
                        if (c.MaxMove && moves < 3) Beat(b, $"{part}_maxmove{++moves}", 0.15, 0.37, 0.7, 1.1, 1.5);
                        else if (c.Kind == CueKind.DynamaxEnd && ends < 2) Beat(b, $"{part}_end{++ends}", 0.15, 0.4, 0.8);
                        else if (c.Kind is CueKind.Mega or CueKind.Dynamax && grown < 3) Growth(b, $"{part}_growth{++grown}", c);
                        beats++;
                        foreach (var d in b.Anim.Cues) seen.Add(d);
                    }
                    if (b.IsWaitingForConfirm && fresh.Count == 0) Confirm(b);
                    else if (b.IsWaitingForConfirm) Confirm(b);
                }
            }
            Console.WriteLine($"  {part}: {grown} growths, {moves} Max Moves, {ends} ends shot over the turns");
            return beats;
        }

        // ---- 1. Cynthia's Mega Max team: her Garchomp first
        if (only is "" or "cynthia")
        {
            var record = TrainerDatabase.Get("champion_cynthia_megamax");
            if (record == null) { Console.WriteLine("  !! no champion_cynthia_megamax"); }
            else
            {
                var cynthia = new Trainer { Id = record.Id };
                TrainerDatabase.Fill(cynthia, record);
                int ace = cynthia.Party.Members.FindIndex(p => p.Species.Name == "Garchomp");
                if (ace > 0) { var g = cynthia.Party.Members[ace]; cynthia.Party.Members.RemoveAt(ace); cynthia.Party.Members.Insert(0, g); }
                Console.WriteLine($"Cynthia: key stone {cynthia.KeyStone}, Dynamax Band {cynthia.DynamaxBand}, Mega Max Band {cynthia.MegaMaxBand}; ace {cynthia.Party.Members[0].Species.Name} {cynthia.Party.Members[0].Level} holding {cynthia.Party.Members[0].HeldItem?.Name}");
                var b = Begin(cynthia, "Champion", Tank("Shuckle", "Splash", "Protect"), Tank("Blissey", "Splash", "Softboiled"), Tank("Steelix", "Splash"));
                if (b != null)
                {
                    ToMainMenu(b);
                    Whole(b);
                    Skip(1.0);
                    Shot("gi00_cynthia_intro");
                    Watch(b, "gi00_cynthia", 8, 8);
                }
            }
        }

        // ---- 2. a foe of the harness's own with the Dynamax Band alone: a real trainer, the AI deciding
        if (only is "" or "dynamax")
        {
            var trainer = new Trainer
            {
                Id = "harness_dynamax", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 0, DynamaxBand = true,
                Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic | PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.EvalAttack | PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Expert
            };
            var snorlax = new Pokemon(PokemonDatabase.Get("Snorlax")!, 60);
            snorlax.Moves.Clear();
            foreach (string m in new[] { "Body Slam", "Earthquake", "Crunch", "Rest" }) snorlax.Moves.Add(new Move(MoveDatabase.Get(m)));
            snorlax.DynamaxLevel = 10;
            trainer.Party.Add(snorlax);
            trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 55));
            var b = Begin(trainer, "Ace Trainer", Tank("Shuckle", "Splash"), Tank("Blissey", "Splash"));
            if (b != null)
            {
                ToMainMenu(b);
                Whole(b);
                Skip(1.0);
                Shot("gi10_dynamax_intro");
                Watch(b, "gi10_dynamax", 8, 6);
            }
        }

        // ---- 3. a double battle with a giant: the trainer sends two, the AI grows one
        if (only is "" or "double")
        {
            var trainer = new Trainer
            {
                Id = "harness_double_giant", Name = "Cal", TrainerClass = "Ace Trainer", PrizeMoney = 0, DoubleBattle = true, DynamaxBand = true,
                Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic | PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.EvalAttack | PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Expert
            };
            foreach (var (species, level) in new[] { ("Snorlax", 60), ("Charizard", 60), ("Staraptor", 55) })
            {
                var p = new Pokemon(PokemonDatabase.Get(species)!, level);
                p.DynamaxLevel = 10;
                trainer.Party.Add(p);
            }
            var b = Begin(trainer, "Ace Trainer", Tank("Shuckle", "Splash"), Tank("Blissey", "Splash"), Tank("Steelix", "Splash"));
            if (b != null)
            {
                Console.WriteLine($"the double: format {b.Format}, places {b.Anim.SlotsOf(BattleSide.Player)} against {b.Anim.SlotsOf(BattleSide.Enemy)}");
                ToMainMenu(b);
                for (int i = 0; i < 8 && b.HUD.MenuState == BattleMenuState.Message; i++) { Confirm(b); Skip(0.5); }
                Whole(b);
                Skip(1.0);
                Shot("gi20_double_intro");
                Watch(b, "gi20_double", 8, 6);
            }
        }
        // ---- 4. the player's own (plan 25 · YD1): a Dynamax through the move menu's button with the trainer's call, by
        // day (gi30_*) and at night (gi31_*), and a Gigantamax with the call and its end (gi32_*); each in a wild battle
        // on grass begun as the later mode's are
        if (only is "" or "own")
        {
            BattleEngine Own(Pokemon mine, uint seed, string foe = "Blissey")
            {
                var party = new Party();
                party.Add(mine);
                var bag = new Inventory();
                bag.AddItem(ItemDatabase.Get(PokemonPlatinumEngine.Battle.Sim.BattleCore.DynamaxBandItem)!);
                var enemy = Meet(foe, 100);
                enemy.Moves.Clear();
                enemy.Moves.Add(new Move(MoveDatabase.Get("Splash")));
                game.Map = MapDatabase.Get("Sinnoh");
                game.BattleRenderer.SetArena(BattleArena.Grass);
                var b = new BattleEngine(new BattleSetup
                {
                    PlayerParty = party, Inventory = bag, Pokedex = pokedex, WildPokemon = new List<Pokemon> { enemy },
                    Random = new BattleRandom(seed).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0), Rules = Ruleset.Modern
                });
                game.Battle = b;
                game.State = GameState.Battle;
                return b;
            }

            Pokemon Grower(string species, bool gmax, params string[] moves)
            {
                var p = Meet(species, 50);
                p.Moves.Clear();
                foreach (var m in moves) p.Moves.Add(new Move(MoveDatabase.Get(m)));
                p.DynamaxLevel = 10;
                p.GigantamaxFactor = gmax;
                return p;
            }

            void Grow(BattleEngine b, string prefix, int move = 0)
            {
                ToMainMenu(b);
                b.SelectMainMenuOption(0);
                Frames(5);
                b.ToggleLaterMechanic();
                b.SelectMove(move);
                Frames(1);
                for (int i = 0; i < 60 && !b.Anim.Cues.Any(c => c.Kind == CueKind.Dynamax); i++)
                {
                    if (b.IsWaitingForConfirm) Confirm(b);
                    else Skip(0.05);
                }
                var cue = b.Anim.Cues.LastOrDefault(c => c.Kind == CueKind.Dynamax);
                if (cue == null) { Console.WriteLine($"  !! no growth for {prefix}"); return; }
                Console.WriteLine($"  {prefix}: the call {(cue.Lead > 0f ? "played" : "not played")}, the cue found at {cue.Age:0.00} s");
                Growth(b, prefix, cue);
            }

            foreach (var (name, time) in new[] { ("gi30_own_day", TimeOfDay.Day), ("gi31_own_night", TimeOfDay.Night) })
            {
                engine.Settings.TimeOfDay = time;
                engine.ApplySettings(window: false);
                var b = Own(Grower("Charizard", false, "Flamethrower", "Growl"), 301);
                Grow(b, name);
            }
            engine.Settings.TimeOfDay = TimeOfDay.Day;
            engine.ApplySettings(window: false);

            var gb = Own(Grower("Charizard", true, "Flamethrower", "Growl"), 302);
            Grow(gb, "gi32_own_gmax");
            // Its end, and a second Dynamax of the same battle (the growth alone)
            for (int turn = 0; turn < 4 && !gb.Anim.Cues.Any(c => c.Kind == CueKind.DynamaxEnd); turn++)
            {
                ToMainMenu(gb);
                gb.SelectMainMenuOption(0);
                gb.SelectMove(1);
                for (int i = 0; i < 60 && gb.HUD.MenuState != BattleMenuState.Main && !gb.Anim.Cues.Any(c => c.Kind == CueKind.DynamaxEnd); i++)
                {
                    if (gb.IsWaitingForConfirm) Confirm(gb);
                    else Skip(0.05);
                }
            }
            if (gb.Anim.Cues.Any(c => c.Kind == CueKind.DynamaxEnd)) Beat(gb, "gi32_own_gmax_end", 0.15, 0.4, 0.8);
            Console.WriteLine($"  a second Dynamax of this battle would be called: {(gb.Anim.CallerFor(BattleSide.Player, 0, false) == null ? "no, the growth alone" : "yes")}");
        }
        game.State = GameState.Overworld;
    }
}
