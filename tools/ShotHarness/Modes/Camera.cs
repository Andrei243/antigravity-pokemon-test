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

        foreach (var manner in manners)
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
        foreach (var manner in manners.Where(m => m.Splits != CameraSplits.None))
        {
            engine.FixedCameraManner = manner;
            Console.WriteLine($"manner {manner.Name}: splits at {manner.Splits}");

            party.HealAll();
            var leader = new Trainer { Name = "Roark", TrainerClass = "Leader" };
            leader.Party.Add(new Pokemon(PokemonDatabase.Get("Geodude")!, 6));
            leader.Party.Add(new Pokemon(PokemonDatabase.Get("Onix")!, 7));
            var single = StartBattle(null, 0, leader);
            SplitReel(single, $"bc_{manner.Name}_split_leads_single");

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
        engine.FixedCameraManner = null;
        game.State = GameState.Overworld;
    }

    // Reads the battle on (a line every 0.8 s) until the picture splits, then a picture every tenth of a second, at
    // half size, until the split has gone, with how far its halves had come in printed for each
    void SplitReel(BattleEngine b, string prefix)
    {
        float waited = 0f;
        for (int guard = 0; guard < 120 && game.BattleRenderer.Split == null && !b.IsBattleOver; guard++)
        {
            Skip(0.1);
            waited += 0.1f;
            if (waited >= 0.8f && b.HUD.MenuState == BattleMenuState.Message && game.BattleRenderer.Split == null)
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
            Console.WriteLine($"  {name}: {state}, the line \"{b.CurrentMessage}\"");
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
