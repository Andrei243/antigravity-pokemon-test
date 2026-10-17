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

        BattleEngine Battle(Pokemon mine, string key, string foe, uint seed)
        {
            var team = new Party();
            team.Add(mine);
            var bag = new Inventory();
            bag.AddItem(ItemDatabase.Get(key)!);
            var enemy = Meet(foe, 30);
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
                Kind = CueKind.Move, Move = move.Name, Type = move.Type, Category = move.Category, ZMove = true,
                FromSide = BattleSide.Player, ToSide = self ? BattleSide.Player : BattleSide.Enemy
            });
        }
        var zmoves = MoveDatabase.GetAll().Where(m => m.Kind == MoveKind.ZMove).OrderBy(m => m.Id).ToList();
        if (named.Length > 0)
        {
            foreach (string moveName in named)
            {
                var move = MoveDatabase.Get(moveName);
                if (move == null || move.Kind != MoveKind.ZMove)
                {
                    Console.WriteLine($"!! no Z-Move called {moveName}");
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
            foreach (var (boardName, age) in new[] { ("lt_board_landing", BattleAnimator.ImpactTime + 0.03), ("lt_board_piece", 0.75) })
            {
                const int columns = 6, tileW = 320, tileH = 180, label = 22;
                bool wanted = Wanted(boardName);
                var sheet = Raylib.GenImageColor(columns * tileW, (zmoves.Count + columns - 1) / columns * (tileH + label), new Color(24, 26, 34, 255));
                for (int i = 0; i < zmoves.Count; i++)
                {
                    var move = zmoves[i];
                    Start(move);
                    Skip(age);
                    int count = Drawn();
                    Console.WriteLine($"  {move.Name} at {age:0.00} s: {count} quads and ribbons");
                    if (!wanted) continue;
                    var img = Capture();
                    Raylib.ImageResize(ref img, tileW, tileH);
                    int tx = i % columns * tileW, ty = i / columns * (tileH + label);
                    Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, tileW, tileH), new Rectangle(tx, ty, tileW, tileH), Color.White);
                    Raylib.UnloadImage(img);
                    Raylib.ImageDrawText(ref sheet, $"{move.Name} {count}", tx + 4, ty + tileH + 3, 16, new Color(150, 240, 150, 255));
                }
                if (wanted) Save(sheet, boardName);
                else Raylib.UnloadImage(sheet);
            }
        game.State = GameState.Overworld;
    }
}
