partial class Harness
{
    // ---------------------------------------------------------------- the later mechanics' looks (plan 25 · Y14)

    // later [Z-Move ...]: Mega Evolution, Primal Reversion, Ultra Burst and the Z-Moves on screen, under the modern
    // rules, each played by the game's own battle (the item button armed through the move menu's own toggle, the move
    // chosen, the lines read on): the move menu with the button and armed (lt01, lt02, with the button close up), a
    // Mega Evolution at five moments (lt03_mega_1 to _5), Kyogre's and Groudon's Primal Reversions as they come in
    // (lt04_primal_kyogre_*, lt05_primal_groudon_*), Necrozma's Ultra Burst (lt06_ultra_*), the Z-Power and the Z-Move
    // after it (lt07_zpower_*, lt08_zmove_*); then the 35 Z-Moves started by the animator itself on two boards, at
    // their landing and at their set piece (lt_board_landing, lt_board_piece). Given Z-Moves (`later "Bloom Doom"`),
    // four moments of each instead of the boards (lt_<key>_1 to _4). It prints the form the screen shows at each
    // moment and the quads drawn. Not part of "all".
    public void LaterMode()
    {
        var named = args.Skip(2).ToArray();

        Pokemon Holding(string species, string item, params string[] moves)
        {
            var p = Meet(species, 50);
            p.Moves.Clear();
            foreach (var m in moves) p.Moves.Add(new Move(MoveDatabase.Get(m)));
            p.HeldItem = ItemDatabase.Get(item);
            return p;
        }

        BattleEngine Battle(Pokemon mine, string key, string foe, uint seed, int foeLevel = 30)
        {
            var team = new Party();
            team.Add(mine);
            var bag = new Inventory();
            bag.AddItem(ItemDatabase.Get(key)!);
            var enemy = Meet(foe, foeLevel);
            enemy.Moves.Clear();
            enemy.Moves.Add(new Move(MoveDatabase.Get("Splash")));
            game.Map = MapDatabase.Get("Sinnoh");
            game.BattleRenderer.SetArena(BattleArena.Grass);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = team, Inventory = bag, Pokedex = pokedex, WildPokemon = new List<Pokemon> { enemy },
                Random = new BattleRandom(seed).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0), Rules = Ruleset.Modern
            });
            game.Battle = b;
            game.State = GameState.Battle;
            return b;
        }

        // Reads the battle on to the line that says the text, and lets it play for a frame
        bool ReadTo(BattleEngine b, string text)
        {
            for (int i = 0; i < 80 && !b.IsBattleOver; i++)
            {
                if (b.CurrentMessage.Contains(text)) return true;
                if (b.IsWaitingForConfirm) Confirm(b);
                else Skip(0.1);
            }
            Console.WriteLine($"  !! never said \"{text}\"");
            return false;
        }

        int Drawn() => game.BattleRenderer.Effects.Count;

        // Shots of the newest cue of a kind at its ages, counted from the cue itself (the line is found a little after it
        // appears), each printing the form the screen shows
        void Moments(BattleEngine b, string prefix, Pokemon who, Func<EffectCue, bool> which, params double[] ages)
        {
            var cue = b.Anim.Cues.LastOrDefault(which);
            if (cue == null)
            {
                Console.WriteLine($"  !! no cue for {prefix}");
                return;
            }
            for (int i = 0; i < ages.Length; i++)
            {
                if (ages[i] > cue.Age) Skip(ages[i] - cue.Age);
                Shot($"{prefix}_{i + 1}");
                Console.WriteLine($"  {prefix}_{i + 1} at {cue.Age:0.00} s: {who.Form ?? who.Species.Name} on screen, {Drawn()} quads and ribbons");
            }
        }

        void Button(string name)
        {
            Shot(name);
            ShotCrop(name + "_close", 1150, 750, 760, 300, 2);
            var (mark, armed) = ModernUi.LaterButtonOf(game.Battle!);
            Console.WriteLine($"  {name}: the button {mark}{(armed ? ", armed" : "")}");
        }

        if (named.Length == 0)
        {
            // A Mega Evolution, through the move menu's button
            var charizard = Holding("Charizard", "Charizardite X", "Flamethrower", "Dragon Claw", "Air Slash", "Roost");
            var mega = Battle(charizard, BattleCore.KeyStoneItem, "Bidoof", 141);
            ToMainMenu(mega);
            mega.SelectMainMenuOption(0);
            Frames(20);
            Button("lt01_mega_menu");
            mega.ToggleMega();
            Frames(20);
            Button("lt02_mega_menu_armed");
            mega.SelectMove(0);
            Frames(1);
            if (ReadTo(mega, "Mega Evolved")) Moments(mega, "lt03_mega", charizard, c => c.Kind == CueKind.Mega, 0.3, 0.6, 0.8, 1.0, 1.4);

            // The Primal Reversions, as each comes in
            foreach (var (species, orb, key) in new[] { ("Kyogre", "Blue Orb", "kyogre"), ("Groudon", "Red Orb", "groudon") })
            {
                var primal = Holding(species, orb, "Surf");
                var b = Battle(primal, BattleCore.KeyStoneItem, "Bidoof", 142);
                Skip(130 / 60.0);
                Confirm(b);
                Skip(50 / 60.0);
                if (ReadTo(b, "Primal")) Moments(b, $"lt0{(key == "kyogre" ? 4 : 5)}_primal_{key}", primal, c => c.Kind == CueKind.Mega, 0.4, 0.8, 1.1);
            }

            // Necrozma's Ultra Burst, through the same button
            var necrozma = Holding("Necrozma", BattleCore.UltranecroziumZ, "Tackle", "Photon Geyser");
            necrozma.ChangeForm("Necrozma-Dusk");
            var ultra = Battle(necrozma, BattleCore.ZRingItem, "Bidoof", 143);
            ToMainMenu(ultra);
            ultra.SelectMainMenuOption(0);
            Frames(20);
            Button("lt06_ultra_menu");
            ultra.ToggleLaterMechanic();
            ultra.SelectMove(0);
            Frames(1);
            if (ReadTo(ultra, "Ultra Burst")) Moments(ultra, "lt06_ultra", necrozma, c => c.Kind == CueKind.UltraBurst, 0.3, 0.7, 0.86, 1.1, 1.5);

            // The Z-Power and its Z-Move: the cards name their Z-Moves while it is armed
            var pikachu = Holding("Pikachu", "Electrium Z", "Thunderbolt", "Quick Attack", "Iron Tail", "Thunder Wave");
            var z = Battle(pikachu, BattleCore.ZRingItem, "Bidoof", 144);
            ToMainMenu(z);
            z.SelectMainMenuOption(0);
            Frames(20);
            Button("lt07_zpower_menu");
            z.ToggleLaterMechanic();
            Frames(20);
            Button("lt07_zpower_menu_armed");
            z.SelectMove(0);
            Frames(1);
            if (ReadTo(z, "Z-Power")) Moments(z, "lt07_zpower", pikachu, c => c.Kind == CueKind.ZPower, 0.2, 0.5, 0.9);
            if (ReadTo(z, "Gigavolt Havoc")) Moments(z, "lt08_zmove", pikachu, c => c.ZMove, 0.15, 0.37, 0.7, 1.1);

            // ---- plan 25 · Y15: Dynamax, Max Moves and Terastallization

            void Box(string name, BattleEngine b)
            {
                Shot(name);
                ShotCrop(name + "_close", 1200, 600, 720, 240, 2);
                var v = b.Anim.Player;
                Console.WriteLine($"  {name}: Dynamaxed {v.Dynamaxed}, {v.DynamaxTurnsLeft} turns left, Tera {v.Shown?.Terastallized?.ToString() ?? "none"}, HP {v.Shown?.CurrentHP}/{v.Shown?.MaxHP}");
            }

            // A Dynamax through the button, its Max Move, Max Guard on the next two turns, and its end at the third's
            var dyna = Holding("Charizard", "Leftovers", "Flamethrower", "Growl");
            dyna.DynamaxLevel = 10;
            var d = Battle(dyna, BattleCore.DynamaxBandItem, "Blissey", 146, 100);
            ToMainMenu(d);
            d.SelectMainMenuOption(0);
            Frames(20);
            Button("lt10_dynamax_menu");
            d.ToggleLaterMechanic();
            Frames(20);
            Button("lt11_dynamax_menu_armed");
            Console.WriteLine($"  the cards: {string.Join(", ", dyna.Moves.Select((m, i) => ModernUi.CardName(d, m, i)))}");
            d.SelectMove(0);
            Frames(1);
            if (ReadTo(d, "giant size")) Moments(d, "lt12_dynamax", dyna, c => c.Kind == CueKind.Dynamax, 0.2, 0.5, 0.75, 0.95, 1.3, 1.62);
            if (ReadTo(d, "Max Flare")) Moments(d, "lt13_maxmove", dyna, c => c.MaxMove, 0.15, 0.37, 0.7, 1.1);
            ToMainMenu(d);
            d.SelectMainMenuOption(0);
            Frames(20);
            Box("lt14_dynamax_box", d);
            d.SelectMove(1);
            Frames(1);
            if (ReadTo(d, "Max Guard")) Moments(d, "lt14_maxguard", dyna, c => c.Move == "Max Guard", 0.3, 0.8);
            ToMainMenu(d);
            Box("lt14_dynamax_box_2", d);
            d.SelectMainMenuOption(0);
            Frames(5);
            d.SelectMove(1);
            Frames(1);
            if (ReadTo(d, "usual size")) Moments(d, "lt15_shrink", dyna, c => c.Kind == CueKind.DynamaxEnd, 0.15, 0.4, 0.8);

            // A Gigantamax: its form taken inside the red light, and its G-Max Move
            var gmax = Holding("Charizard", "Leftovers", "Flamethrower");
            gmax.DynamaxLevel = 10;
            gmax.GigantamaxFactor = true;
            var g = Battle(gmax, BattleCore.DynamaxBandItem, "Blissey", 147, 100);
            ToMainMenu(g);
            g.SelectMainMenuOption(0);
            g.ToggleLaterMechanic();
            Frames(10);
            Button("lt16_gmax_menu_armed");
            g.SelectMove(0);
            Frames(1);
            if (ReadTo(g, "Gigantamax")) Moments(g, "lt16_gmax", gmax, c => c.Kind == CueKind.Dynamax, 0.4, 0.7, 0.95, 1.62);
            if (ReadTo(g, "G-Max Wildfire")) Moments(g, "lt17_gmaxmove", gmax, c => c.MaxMove, 0.37, 0.7, 1.1);

            // A Terastallization through the button, and the crown on the HP box
            var tera = Holding("Snorlax", "Leftovers", "Body Slam", "Shadow Ball");
            tera.TeraType = TeraType.Of(PokemonType.Ghost);
            var t = Battle(tera, BattleCore.TeraOrbItem, "Blissey", 148, 100);
            ToMainMenu(t);
            t.SelectMainMenuOption(0);
            Frames(20);
            Button("lt20_tera_menu");
            t.ToggleLaterMechanic();
            Frames(20);
            Button("lt20_tera_menu_armed");
            t.SelectMove(1);
            Frames(1);
            if (ReadTo(t, "Terastallized")) Moments(t, "lt21_tera", tera, c => c.Kind == CueKind.Tera, 0.3, 0.6, 0.85, 0.95, 1.2, 1.75);
            ToMainMenu(t);
            Box("lt22_tera_box", t);

            // Terapagos: its Terastal Form as it comes in, and its Stellar Form as it Terastallizes
            var terapagos = Holding("Terapagos", "Leftovers", "Tera Starstorm", "Tackle");
            var st = Battle(terapagos, BattleCore.TeraOrbItem, "Blissey", 149, 100);
            ToMainMenu(st);
            st.SelectMainMenuOption(0);
            st.ToggleLaterMechanic();
            Frames(10);
            Button("lt23_stellar_menu_armed");
            st.SelectMove(1);
            Frames(1);
            if (ReadTo(st, "Terastallized")) Moments(st, "lt23_stellar", terapagos, c => c.Kind == CueKind.Tera, 0.5, 0.95, 1.3, 1.75);
            ToMainMenu(st);
            Box("lt24_stellar_box", st);
        }

        // The 35 Z-Moves, each started by the animator itself from the player's Pokémon at the foe in a battle at its menu
        var user = Holding("Empoleon", "Leftovers", "Surf");
        var board = Battle(user, BattleCore.ZRingItem, "Snorlax", 145);
        ToMainMenu(board);
        EffectCue Start(MoveData move)
        {
            Skip(BattleAnimator.ZMoveTime + 0.4);
            bool self = move.Category == MoveCategory.Status;
            return board.Anim.Cue(new EffectCue
            {
                Kind = CueKind.Move, Move = move.Name, Type = move.Type, Category = move.Category,
                ZMove = move.Kind == MoveKind.ZMove, MaxMove = move.Kind is MoveKind.MaxMove or MoveKind.GMaxMove,
                FromSide = BattleSide.Player, ToSide = self ? BattleSide.Player : BattleSide.Enemy
            });
        }
        var zmoves = MoveDatabase.GetAll().Where(m => m.Kind == MoveKind.ZMove).OrderBy(m => m.Id).ToList();
        var maxMoves = MoveDatabase.GetAll().Where(m => m.Kind is MoveKind.MaxMove or MoveKind.GMaxMove).OrderBy(m => m.Id).ToList();
        if (named.Length > 0)
        {
            foreach (string moveName in named)
            {
                var move = MoveDatabase.Get(moveName);
                if (move == null || move.Kind is not (MoveKind.ZMove or MoveKind.MaxMove or MoveKind.GMaxMove))
                {
                    Console.WriteLine($"!! no Z-Move or Max Move called {moveName}");
                    continue;
                }
                string key = PokemonPlatinumEngine.Audio.SoundBank.MoveKey(move.Name);
                Start(move);
                double at = 0;
                int n = 0;
                foreach (double age in new[] { 0.2, BattleAnimator.ImpactTime + 0.03, 0.7, 1.15 })
                {
                    Skip(age - at);
                    at = age;
                    Shot($"lt_{key}_{++n}");
                    Console.WriteLine($"  {move.Name} at {age:0.00} s: {Drawn()} quads and ribbons");
                }
            }
        }
        else
        {
            // A board of cues, each started by the animator and shot at an age
            void Board(string boardName, int n, Func<int, (string Label, Action Start)> each, double age)
            {
                const int columns = 6, tileW = 320, tileH = 180, label = 22;
                bool wanted = Wanted(boardName);
                var sheet = Raylib.GenImageColor(columns * tileW, (n + columns - 1) / columns * (tileH + label), new Color(24, 26, 34, 255));
                for (int i = 0; i < n; i++)
                {
                    var (name, start) = each(i);
                    start();
                    Skip(age);
                    int count = Drawn();
                    Console.WriteLine($"  {name} at {age:0.00} s: {count} quads and ribbons");
                    if (!wanted) continue;
                    var img = Capture();
                    Raylib.ImageResize(ref img, tileW, tileH);
                    int tx = i % columns * tileW, ty = i / columns * (tileH + label);
                    Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, tileW, tileH), new Rectangle(tx, ty, tileW, tileH), Color.White);
                    Raylib.UnloadImage(img);
                    Raylib.ImageDrawText(ref sheet, $"{name} {count}", tx + 4, ty + tileH + 3, 16, new Color(150, 240, 150, 255));
                }
                if (wanted) Save(sheet, boardName);
                else Raylib.UnloadImage(sheet);
            }

            foreach (var (boardName, age) in new[] { ("lt_board_landing", BattleAnimator.ImpactTime + 0.03), ("lt_board_piece", 0.75) })
                Board(boardName, zmoves.Count, i => (zmoves[i].Name, () => Start(zmoves[i])), age);
            // The 52 Max and G-Max Moves (plan 25 · Y15) at their landing and their set piece
            foreach (var (boardName, age) in new[] { ("lt_max_board_landing", BattleAnimator.ImpactTime + 0.03), ("lt_max_board_piece", 0.75) })
                Board(boardName, maxMoves.Count, i => (maxMoves[i].Name, () => Start(maxMoves[i])), age);
            // A Terastallization of every type and a Stellar one, as the shell is whole and as the crown stands
            var teras = Enum.GetValues<PokemonType>().Select(TeraType.Of).Append(TeraType.Stellar).ToList();
            foreach (var (boardName, age) in new[] { ("lt_tera_board_shell", 0.8), ("lt_tera_board_crown", 1.3) })
                Board(boardName, teras.Count, i => (teras[i].Name, () =>
                {
                    Skip(BattleAnimator.TeraTime + 0.4);
                    board.Anim.Terastallize(BattleSide.Enemy, 0, teras[i]);
                }), age);

            // The foe giant, and the player's own: how they stand in the overview at their full size
            board.Anim.Dynamax(BattleSide.Enemy, 0, false);
            Skip(BattleAnimator.DynamaxTime + 0.3);
            Shot("lt18_foe_giant");
            board.Anim.Dynamax(BattleSide.Player, 0, false);
            Skip(BattleAnimator.DynamaxTime + 0.3);
            Shot("lt19_both_giant");
            Console.WriteLine($"  lt19: foe x{BattleAnimator.GiantOf(board.Anim.Enemy, false):0.00}, own x{BattleAnimator.GiantOf(board.Anim.Player, true):0.00}");

            // The Gigantamax signatures (plan 25 · YD7): the 33 G-Max Moves alone, at their set piece and late in it
            // (the five with a set piece of their own beside the rest's marks), and every Gigantamax's growth in its own
            // colours as its clouds gather on the foe. Last, so every shot before them stands as it was.
            board.Anim.Undynamax(BattleSide.Enemy, 0, played: false);
            board.Anim.Undynamax(BattleSide.Player, 0, played: false);
            var gmaxMoves = maxMoves.Where(m => m.Kind == MoveKind.GMaxMove).ToList();
            foreach (var (boardName, age) in new[] { ("lt_gmax_board_piece", 0.75), ("lt_gmax_board_late", 1.1) })
                Board(boardName, gmaxMoves.Count, i => (gmaxMoves[i].Name, () => Start(gmaxMoves[i])), age);
            Board("lt_gmax_board_growth", gmaxMoves.Count, i => (gmaxMoves[i].Name, () =>
            {
                board.Anim.Undynamax(BattleSide.Enemy, 0, played: false);
                Skip(BattleAnimator.DynamaxTime + 0.4);
                board.Anim.Dynamax(BattleSide.Enemy, 0, true, gmax: gmaxMoves[i].Name);
            }), 0.45);
            board.Anim.Undynamax(BattleSide.Enemy, 0, played: false);
        }
        game.State = GameState.Overworld;
    }
}
