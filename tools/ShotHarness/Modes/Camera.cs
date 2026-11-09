partial class Harness
{
    // ---------------------------------------------------------------- the battle's manners (plan 45)

    // For each manner but Classic (CAMERA_ONLY=sinnoh,galar shoots some): a wild single battle and a double at their
    // menus, a picture every half second through two of the manner's cycles (bc_<manner>_<battle>_<nnn>), at half size
    // for `contact` to put on sheets, printing the shot the director had at each. Not part of `all`; later sessions of
    // plan 45 add a move, a hit, a faint, the splits and the closing
    public void CameraMode()
    {
        string only = Environment.GetEnvironmentVariable("CAMERA_ONLY") ?? "";
        var wanted = only.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var manners = CameraManners.All.Where(m => m != CameraManners.Classic
            && (wanted.Length == 0 || wanted.Contains(m.Name, StringComparer.OrdinalIgnoreCase))).ToList();
        if (party.Count < 2) party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 8));
        // CAMERA_PART=cycles,splits,moves,ends shoots some of the parts below
        var parts = (Environment.GetEnvironmentVariable("CAMERA_PART") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool Part(string p) => parts.Length == 0 || parts.Contains(p, StringComparer.OrdinalIgnoreCase);

        foreach (var manner in manners.Where(_ => Part("cycles")))
        {
            engine.FixedCameraManner = manner;
            float cycle = manner.Cycle.Count > 0 ? manner.Cycle.Count * (manner.CycleHold + manner.CycleEase) : manner.DriftPeriod;
            int pictures = (int)MathF.Ceiling(2f * cycle / 0.5f);
            Console.WriteLine($"manner {manner.Name}: a cycle of {cycle:0.0} s, {pictures + 1} pictures a battle");

            // A wild single battle at its menu
            party.HealAll();
            var single = StartBattle("Starly", 8);
            ToMainMenu(single);
            Reel(single, $"bc_{manner.Name}_single", pictures);

            // A double battle with a trainer of two at its menu
            party.HealAll();
            var twins = new Trainer { Name = "Liv & Liz", TrainerClass = "Lass", DoubleBattle = true };
            twins.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 6));
            twins.Party.Add(new Pokemon(PokemonDatabase.Get("Gible")!, 6));
            game.Map = MapDatabase.Get("Sinnoh");
            var two = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double,
                Trainers = new List<Trainer> { twins }, WildPokemon = new List<Pokemon>(), Random = new Random(5)
            });
            game.BattleRenderer.SetArena(game.Map);
            game.Battle = two;
            game.State = GameState.Battle;
            Skip(130 / 60.0);
            for (int guard = 0; guard < 12 && two.HUD.MenuState == BattleMenuState.Message && !two.IsBattleOver; guard++) { Confirm(two); Skip(70 / 60.0); }
            Reel(two, $"bc_{manner.Name}_double", pictures);
        }
        // The split (plan 45 · BC3): for each manner that splits, a Leader's single battle's leads and the last
        // Pokémon, and a double's leads, a picture every tenth of a second from the moment the split begins until it
        // has gone (bc_<manner>_split_<moment>_<nn>); the lines are left waiting, as a player reading them would
        foreach (var manner in manners.Where(m => m.Splits != CameraSplits.None && Part("splits")))
        {
            engine.FixedCameraManner = manner;
            Console.WriteLine($"manner {manner.Name}: splits at {manner.Splits}");

            party.HealAll();
            var leader = new Trainer { Name = "Roark", TrainerClass = "Leader" };
            leader.Party.Add(new Pokemon(PokemonDatabase.Get("Geodude")!, 6));
            leader.Party.Add(new Pokemon(PokemonDatabase.Get("Onix")!, 7));
            var single = StartBattle(null, 0, leader);
            if (manner.Splits.HasFlag(CameraSplits.Leads)) SplitReel(single, $"bc_{manner.Name}_split_leads_single");
            else ToMainMenu(single);

            // The last Pokémon: the first knocked out, its successor sent out
            if (manner.Splits.HasFlag(CameraSplits.LastPokemon) || manner.Splits.HasFlag(CameraSplits.BossLast))
            {
                for (int guard = 0; guard < 12 && single.HUD.MenuState == BattleMenuState.Message && !single.IsBattleOver; guard++) { Confirm(single); Skip(0.8); }
                single.EnemyPokemon.CurrentHP = 1;
                single.SelectMove(0);
                SplitReel(single, $"bc_{manner.Name}_split_last_single");
            }

            if (!manner.Splits.HasFlag(CameraSplits.Leads)) continue;
            party.HealAll();
            var twins = new Trainer { Name = "Liv & Liz", TrainerClass = "Lass", DoubleBattle = true };
            twins.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 6));
            twins.Party.Add(new Pokemon(PokemonDatabase.Get("Gible")!, 6));
            game.Map = MapDatabase.Get("Sinnoh");
            var two = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double,
                Trainers = new List<Trainer> { twins }, WildPokemon = new List<Pokemon>(), Random = new Random(5)
            });
            game.BattleRenderer.SetArena(game.Map);
            game.Battle = two;
            game.State = GameState.Battle;
            SplitReel(two, $"bc_{manner.Name}_split_leads_double");
        }

        // Push in, pull out, reactions (plan 45 · BC4): for each manner that pushes or reacts, a wild battle with a
        // Chimchar of its own: a plain move (Fury Swipes), a super-effective one (Ember on a Turtwig) and the faint it brings,
        // a picture every tenth of a second (bc_<manner>_move_<what>_<nn>), the line left waiting through the move
        foreach (var manner in manners.Where(m => (m.Push > 0f || m.React) && Part("moves")))
        {
            engine.FixedCameraManner = manner;
            Console.WriteLine($"manner {manner.Name}: push {manner.Push}, reactions {(manner.React ? "on" : "off")}");
            var mine = new Party();
            mine.Add(new Pokemon(PokemonDatabase.Get("Chimchar")!, 20, new Random(3)));
            var turtwig = new Pokemon(PokemonDatabase.Get("Turtwig")!, 22, new Random(4));
            var (stage, x, y) = Harness.Place("Route201", -1, -1);
            game.Map = MapDatabase.Get(stage);
            if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = mine, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { turtwig }, Random = new Random(9)
            });
            game.BattleRenderer.SetArena(game.Map, x, y);
            game.Battle = b;
            game.State = GameState.Battle;
            ToMainMenu(b);
            foreach (var (what, move) in new[] { ("plain", "Fury Swipes"), ("super", "Ember"), ("faint", "Ember") })
            {
                for (int guard = 0; guard < 16 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; guard++) { Confirm(b); Skip(0.8); }
                if (b.IsBattleOver) break;
                turtwig.CurrentHP = what == "faint" ? 1 : turtwig.MaxHP;
                mine.Members[0].CurrentHP = mine.Members[0].MaxHP;
                int index = b.PlayerPokemon!.Moves.FindIndex(m => m.Data.Name == move);
                Console.WriteLine($"  {what}: {move} ({index})");
                b.SelectMove(Math.Max(0, index));
                MoveReel(b, $"bc_{manner.Name}_move_{what}", what == "faint" ? 50 : 22, confirmEvery: what == "faint" ? 10 : 0);
            }
        }
        // Opening and closing (plan 45 · BC5): for each manner that opens or closes, a wild battle and a Leader's (whose
        // battle Galar's crane films), each from its first frame a picture every tenth of a second, the first line left
        // waiting 1.5 s (bc_<manner>_open_<battle>_<nn>), then won with the foes left one HP and filmed from the win's
        // line a picture every fifth of a second, a line read every two seconds, to the battle's end (bc_<manner>_close_<battle>_<nn>)
        foreach (var manner in manners.Where(m => (m.Open || m.Close) && Part("ends")))
        {
            engine.FixedCameraManner = manner;
            Console.WriteLine($"manner {manner.Name}: opens {manner.Open}, closes {manner.Close}, crane {manner.Crane}");
            foreach (string battle in new[] { "wild", "leader" })
            {
                party.HealAll();
                Trainer leader = null;
                if (battle == "leader")
                {
                    leader = new Trainer { Name = "Roark", TrainerClass = "Leader" };
                    leader.Party.Add(new Pokemon(PokemonDatabase.Get("Geodude")!, 6));
                    leader.Party.Add(new Pokemon(PokemonDatabase.Get("Onix")!, 7));
                }
                var b = StartBattle(battle == "wild" ? "Starly" : null, 8, leader);
                MoveReel(b, $"bc_{manner.Name}_open_{battle}", 36, confirmEvery: 15);
                // Won: every foe left one HP and struck
                for (int guard = 0; guard < 8 && b.Anim.Closing == null && !b.IsBattleOver; guard++)
                {
                    for (int read = 0; read < 16 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver && b.Anim.Closing == null; read++) { Confirm(b); Skip(0.8); }
                    if (b.HUD.MenuState != BattleMenuState.Main) continue;
                    b.EnemyPokemon.CurrentHP = 1;
                    party.Members[0].CurrentHP = party.Members[0].MaxHP;
                    b.SelectMove(0);
                    for (int wait = 0; wait < 60 && b.Anim.Closing == null && !b.IsBattleOver; wait++)
                    {
                        Skip(0.1);
                        if (wait % 12 == 11 && b.HUD.MenuState == BattleMenuState.Message) Confirm(b);
                    }
                }
                if (b.Anim.Closing == null)
                {
                    Console.WriteLine($"  !! {manner.Name} {battle}: the battle never closed");
                    continue;
                }
                Console.WriteLine($"  closing {b.Anim.Closing}, a boss's battle {b.Anim.BossBattle}");
                MoveReel(b, $"bc_{manner.Name}_close_{battle}", 40, confirmEvery: 10, every: 0.2);
            }
        }
        // The formats and the rest (plan 45 · BC7), only asked for: CAMERA_PART=formats, or triple, rotation, royal, sos, photo, halls, link, replay
        if (parts.Any(p => p is "formats" or "triple" or "rotation" or "royal" or "sos" or "photo" or "halls" or "link" or "replay"))
            CameraFormats(manners, p => parts.Contains("formats", StringComparer.OrdinalIgnoreCase) || parts.Contains(p, StringComparer.OrdinalIgnoreCase));
        engine.FixedCameraManner = null;
        game.State = GameState.Overworld;
    }

    // A picture every tenth of a second, at half size, n of them, with the director's shot printed for each; the line
    // read on every so many pictures (none for 0)
    void MoveReel(BattleEngine b, string prefix, int n, int confirmEvery, double every = 0.1)
    {
        for (int i = 0; i < n && !b.IsBattleOver; i++)
        {
            string name = $"{prefix}_{i:00}";
            if (Wanted(name))
            {
                var img = Capture();
                Raylib.ImageResize(ref img, GameEngine.VirtualWidth / 2, GameEngine.VirtualHeight / 2);
                Save(img, name);
            }
            Console.WriteLine($"  {name}: {game.BattleRenderer.Shot}, the line \"{b.CurrentMessage}\"");
            Skip(every);
            if (confirmEvery > 0 && i % confirmEvery == confirmEvery - 1 && b.HUD.MenuState == BattleMenuState.Message) Confirm(b);
        }
    }

    // Reads the battle on (a line every 2 s, long enough for a send-out to end) until the picture splits, then a picture every tenth of a second, at
    // half size, until the split has gone, with how far its halves had come in printed for each
    void SplitReel(BattleEngine b, string prefix)
    {
        float waited = 0f;
        for (int guard = 0; guard < 400 && game.BattleRenderer.Split == null && !b.IsBattleOver; guard++)
        {
            Skip(0.1);
            waited += 0.1f;
            if (waited >= 2.0f && b.HUD.MenuState == BattleMenuState.Message && game.BattleRenderer.Split == null)
            {
                Confirm(b);
                waited = 0f;
            }
        }
        if (game.BattleRenderer.Split == null)
        {
            Console.WriteLine($"  !! {prefix}: the picture never split");
            return;
        }
        for (int i = 0; i < 40 && game.BattleRenderer.Split != null; i++)
        {
            string name = $"{prefix}_{i:00}";
            if (Wanted(name))
            {
                var img = Capture();
                Raylib.ImageResize(ref img, GameEngine.VirtualWidth / 2, GameEngine.VirtualHeight / 2);
                Save(img, name);
            }
            string state = game.BattleRenderer.Split is { } split ? $"the halves {split.In:0.00} in" : "no split";
            Console.WriteLine($"  {name}: {state}, the line \"{b.CurrentMessage}\" ({b.HUD.MenuState}, cut short at {b.Anim.SplitMoments.LastOrDefault()?.CutShortAt:0.00}, the clock {b.Anim.Time:0.00})");
            Skip(0.1);
        }
    }

    // A picture of the battle every half second, n + 1 of them, at half size, with the director's shot printed for each
    void Reel(BattleEngine b, string prefix, int n)
    {
        for (int i = 0; i <= n; i++)
        {
            string name = $"{prefix}_{i:000}";
            if (Wanted(name))
            {
                var img = Capture();
                Raylib.ImageResize(ref img, GameEngine.VirtualWidth / 2, GameEngine.VirtualHeight / 2);
                Save(img, name);
            }
            Console.WriteLine($"  {name}: {game.BattleRenderer.Shot}, the menu open {b.Anim.MenuAge:0.00} s");
            Skip(0.5);
        }
    }
}
