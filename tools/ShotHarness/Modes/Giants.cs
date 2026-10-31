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
    //      plan 25 · YD1);
    //   5. the Max Move finisher (plan 25 · YD2): Max Flare, Max Quake and G-Max Wildfire from the user to the set piece,
    //      and the foe's HP box punched close up (gi21_* to gi24_*);
    //   6. Max Guard, the countdown and the fall (plan 25 · YD3): a trainer's Max Move on the player's Max Guard (gi33_*),
    //      the pill ticking (gi34_*_close), the last turn (gi35_*), a Dynamax's fall (gi36_*) and a giant fainting (gi37_*).
    // Every cue that starts is shot at the same moments from its start: gi<part>_<what>_<n>, with its kinds, quads, the
    // camera's shot and the giant's sky printed; a growth with a trainer's call (YD1) has four shots of the call first
    // (_call_1 to _4) and then the growth's at the same moments of the growth as YD0's. Not part of "all".
    // GIANTS_PART=cynthia|dynamax|double|own|finisher|guard runs one part.
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

        // What a finisher shot prints: the camera, the cue's shown age, the quads, the target's body and its box's punch
        void Finisher(BattleEngine b, string prefix, EffectCue cue, params double[] ages) => FinisherWith(b, prefix, cue, null, ages);

        // The same, and a crop of the target's HP box after the shot whose number is given (the box punched, close up)
        void FinisherWith(BattleEngine b, string prefix, EffectCue cue, (int After, string Name)? crop, params double[] ages)
        {
            double at = cue.Age;
            for (int i = 0; i < ages.Length; i++)
            {
                if (ages[i] > at) Skip(ages[i] - at);
                at = Math.Max(at, ages[i]);
                Shot($"{prefix}_{i + 1}");
                if (crop is { } c && c.After == i + 1)
                {
                    if (cue.ToSide == BattleSide.Enemy) ShotCrop(c.Name, 36, 30, 640, 200, 2);
                    else ShotCrop(c.Name, 1220, 600, 680, 230, 2);
                }
                var to = b.Anim[cue.ToSide, cue.ToSlot];
                var body = BodyMotions.Of(b.Anim, cue.ToSide, cue.ToSlot);
                Console.WriteLine($"  {prefix}_{i + 1} age {cue.Age:0.00} shown {cue.ShownAge:0.00}: camera {game.BattleRenderer.Shot}, {game.BattleRenderer.Effects.Count} quads and ribbons; "
                    + $"target thrown {body.Toward:0.00} h, pressed to {body.Shape.Height:0.00}; box punch {to.PunchAge:0.00} s, bar {to.DisplayedHp:0}, white to {to.GhostHp:0} of {to.Shown?.MaxHP}");
            }
        }

        // Plays on until a cue the predicate likes starts, confirming lines; null if none came
        EffectCue? Until(BattleEngine b, Func<EffectCue, bool> wanted, int frames = 120)
        {
            for (int i = 0; i < frames && !b.IsBattleOver; i++)
            {
                if (b.Anim.Cues.FirstOrDefault(wanted) is { } c) return c;
                if (b.IsWaitingForConfirm) Confirm(b);
                else Skip(0.05);
            }
            return b.Anim.Cues.FirstOrDefault(wanted);
        }

        BattleEngine OwnBattle(Pokemon mine, uint seed, Pokemon foe)
        {
            var party = new Party();
            party.Add(mine);
            var bag = new Inventory();
            bag.AddItem(ItemDatabase.Get(PokemonPlatinumEngine.Battle.Sim.BattleCore.DynamaxBandItem)!);
            game.Map = MapDatabase.Get("Sinnoh");
            game.BattleRenderer.SetArena(BattleArena.Grass);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = bag, Pokedex = pokedex, WildPokemon = new List<Pokemon> { foe },
                Random = new BattleRandom(seed).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0), Rules = Ruleset.Modern
            });
            game.Battle = b;
            game.State = GameState.Battle;
            return b;
        }

        Pokemon Giant(string species, bool gmax, int level, params string[] moves)
        {
            var p = Meet(species, level);
            p.Moves.Clear();
            foreach (var m in moves) p.Moves.Add(new Move(MoveDatabase.Get(m)));
            p.DynamaxLevel = 10;
            p.GigantamaxFactor = gmax;
            return p;
        }

        Pokemon Sitter(string species, int level, string move = "Splash")
        {
            var p = Meet(species, level);
            p.Moves.Clear();
            p.Moves.Add(new Move(MoveDatabase.Get(move)));
            return p;
        }

        // ---- 5. the Max Move finisher (plan 25 · YD2): the player's own Max Flare, Max Quake and G-Max Wildfire, each shot
        // at the user from below, the whip, the hit-stop, the punch-in, the pull-back and the set piece (gi21_* to gi24_*),
        // with the foe's HP box punched close up (gi23_punch_close)
        if (only is "" or "finisher")
        {
            var b = OwnBattle(Giant("Charizard", false, 50, "Flamethrower", "Earthquake"), 311, Sitter("Snorlax", 70));
            ToMainMenu(b);
            b.SelectMainMenuOption(0);
            Frames(5);
            b.ToggleLaterMechanic();
            b.SelectMove(0);
            Frames(1);
            var flare = Until(b, c => c.MaxMove, 400);
            if (flare == null) Console.WriteLine("  !! no Max Flare");
            else
            {
                // The foe's box punched close up at the screen's own resolution after the fourth: the jolt gone, the white ghost held
                FinisherWith(b, "gi21_finisher_flare", flare, (3, "gi23_punch_close"), 0.12, 0.28, 0.4, 0.55, 0.62, 0.95, 1.3);
            }
            // The next turn, Max Quake
            for (int i = 0; i < 400 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++) { if (b.IsWaitingForConfirm) Confirm(b); else Skip(0.05); }
            if (!b.IsBattleOver)
            {
                foreach (var p in b.EnemySlots.Where(s => s.Pokemon != null)) p.Pokemon!.CurrentHP = p.Pokemon.MaxHP;
                b.SelectMainMenuOption(0);
                Frames(5);
                b.SelectMove(1);
                var quake = Until(b, c => c.MaxMove && c.Move.Contains("Quake"), 400);
                if (quake != null)
                {
                    FinisherWith(b, "gi22_finisher_quake", quake, (3, "gi23_punch_quake_close"), 0.12, 0.28, 0.4, 0.62, 0.95, 1.3);
                }
                else Console.WriteLine("  !! no Max Quake");
            }

            var gb = OwnBattle(Giant("Charizard", true, 50, "Flamethrower", "Growl"), 312, Sitter("Snorlax", 70));
            ToMainMenu(gb);
            gb.SelectMainMenuOption(0);
            Frames(5);
            gb.ToggleLaterMechanic();
            gb.SelectMove(0);
            Frames(1);
            var wildfire = Until(gb, c => c.MaxMove, 400);
            if (wildfire != null) Finisher(gb, "gi24_finisher_wildfire", wildfire, 0.12, 0.28, 0.4, 0.62, 0.95, 1.3);
            else Console.WriteLine("  !! no G-Max Wildfire");
        }

        // ---- 6. Max Guard, the countdown and the fall (plan 25 · YD3): the player's giant puts up Max Guard against a
        // trainer's Max Move and a wild Tackle (gi33_*), the pill ticking at the three menus close up (gi34_*), the last
        // turn (gi35_*), the fall at its three moments (gi36_*), and a foe giant fainting (gi37_*)
        if (only is "" or "guard")
        {
            // A trainer's giant Max Moves into the player's Max Guard
            game.Bag.AddItem(ItemDatabase.Get(PokemonPlatinumEngine.Battle.Sim.BattleCore.DynamaxBandItem)!);
            var trainer = new Trainer
            {
                Id = "harness_guard", Name = "Ida", TrainerClass = "Ace Trainer", PrizeMoney = 0, DynamaxBand = true,
                Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic | PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.EvalAttack | PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Expert
            };
            var foeGiant = Giant("Snorlax", false, 55, "Body Slam", "Earthquake");
            trainer.Party.Add(foeGiant);
            var b = Begin(trainer, "Ace Trainer", Giant("Charizard", false, 70, "Growl", "Flamethrower"));
            if (b != null)
            {
                ToMainMenu(b);
                Whole(b);
                b.SelectMainMenuOption(0);
                Frames(5);
                // The first turn grows it with a Flamethrower (a Max Flare), so its Max Guard of the next turn is put up by a
                // giant that stands: the call and the growth have played out by then
                b.ToggleLaterMechanic();
                b.SelectMove(1);
                Frames(1);
                bool shotHit = false;
                // The pill ticking at each menu, close up, the blow on Max Guard as it comes, and the last turn
                for (int turn = 0; turn < 4 && !b.IsBattleOver; turn++)
                {
                    for (int i = 0; i < 600 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver
                        && !b.Anim.Cues.Any(c => c.Kind == CueKind.DynamaxEnd && c.ToSide == BattleSide.Player); i++)
                    {
                        if (!shotHit && b.Anim.Cues.FirstOrDefault(c => c.OnGuard) is { } hit)
                        {
                            shotHit = true;
                            Finisher(b, $"gi33_guard_{(hit.MaxMove ? "max" : "plain")}", hit, 0.2, 0.4, 0.55, 0.8);
                        }
                        if (b.IsWaitingForConfirm) Confirm(b); else Skip(0.05);
                    }
                    if (b.Anim.Cues.Any(c => c.Kind == CueKind.DynamaxEnd && c.ToSide == BattleSide.Player)) break;
                    if (b.IsBattleOver) break;
                    var mine = b.Anim.Player;
                    Skip(0.15);
                    ShotCrop($"gi34_tick_{turn}_close", 1220, 600, 680, 230, 2);
                    Console.WriteLine($"  gi34_tick_{turn}: {mine.DynamaxTurnsLeft} turns left, ticking from {mine.TickFrom} ({mine.TickAge:0.00} s); wreath of {MoveFx.WreathClouds(mine.DynamaxTurnsLeft)}, pulse {MoveFx.PulseHz(mine.DynamaxTurnsLeft)} Hz; last turn {b.Anim.GiantOnItsLastTurn}");
                    // A growth read past at the harness's pace is still playing: let it end, as a player who reads at
                    // a reader's pace would see it end, before the next turn's choice
                    for (int i = 0; i < 60 && b.Anim.Cues.Any(c => c.Kind == CueKind.Dynamax); i++) Skip(0.05);
                    if (mine.DynamaxTurnsLeft == 1) { Skip(0.4); Shot("gi35_last_turn"); }
                    Whole(b);
                    b.SelectMainMenuOption(0);
                    Frames(5);
                    b.SelectMove(0);
                    Frames(1);
                }
            }

            // The fall, alone: the player's giant against a foe that only Splashes, three turns of Flamethrower and its end
            var eb = OwnBattle(Giant("Charizard", false, 70, "Flamethrower", "Growl"), 314, Sitter("Blissey", 100));
            ToMainMenu(eb);
            eb.SelectMainMenuOption(0);
            Frames(5);
            eb.ToggleLaterMechanic();
            eb.SelectMove(0);
            Frames(1);
            EffectCue? fall = null;
            for (int turn = 0; turn < 4 && fall == null && !eb.IsBattleOver; turn++)
            {
                for (int i = 0; i < 600 && eb.HUD.MenuState != BattleMenuState.Main && fall == null && !eb.IsBattleOver; i++)
                {
                    fall = eb.Anim.Cues.FirstOrDefault(c => c.Kind == CueKind.DynamaxEnd);
                    if (fall != null) break;
                    if (eb.IsWaitingForConfirm) Confirm(eb); else Skip(0.05);
                }
                if (fall != null || eb.IsBattleOver) break;
                eb.SelectMainMenuOption(0);
                Frames(5);
                eb.SelectMove(0);
                Frames(1);
            }
            if (fall != null) Finisher(eb, "gi36_fall", fall, 0.15, 0.5, 0.8);
            else Console.WriteLine("  !! no end of the Dynamax");

            // A wild giant fainting: its HP brought down to one by the harness, the player's Flamethrower felling it
            var fb = OwnBattle(Giant("Charizard", false, 80, "Flamethrower", "Growl"), 313, Giant("Snorlax", false, 40, "Splash"));
            ToMainMenu(fb);
            // The wild one grows by the harness's hand (it has no band), as the scene would show it
            var wild = fb.EnemySlots[0].Pokemon!;
            fb.Anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false);
            Skip(2.0);
            wild.CurrentHP = 1;
            fb.SelectMainMenuOption(0);
            Frames(5);
            fb.SelectMove(0);
            var fell = Until(fb, c => c.Kind == CueKind.DynamaxEnd && c.Fell, 600);
            if (fell != null) Finisher(fb, "gi37_faint", fell, 0.1, 0.3, 0.55, 0.9);
            else Console.WriteLine($"  !! the giant never fell (faint age {fb.Anim.Enemy.FaintAge:0.00}, fall age {fb.Anim.Enemy.FallAge:0.00})");
        }
        game.State = GameState.Overworld;
    }
}
