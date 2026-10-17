partial class Harness
{
    // Bodies that answer (plan 25 · Y5), the very last part of `conditions` (CONDITIONS_FROM=bodies runs it alone):
    // a miss dodged, from the front as the foe hops out of Stone Edge's way (96_dodge_a_coming, 96_dodge at its
    // furthest, 96_dodge_c_back) and from behind as the player's own banks out of Rock Slide's (96_dodge_mine); both
    // Pokémon tired at the menu, their bars in the red (96_tired, with both platforms close up, _close and
    // _mine_close, and half a breath later, _b); the player's Pokémon cheering a win at its two hops (96_cheer,
    // 96_cheer_b, _close) and the foe's cheering the player's loss (96_cheer_foe); and a charge turn, Skull Bash's tuck
    // with its aura as its line is read (96_charge, _close) and held through the next menu (96_charge_menu). Each battle
    // has a team, a Pokédex and chance of its own, so nothing of the harness's is touched; it prints what each view
    // holds at each shot.
    void BodiesMode()
    {
        BattleEngine Battle(string mineSpecies, int mineLevel, string[] mineMoves, string foeSpecies, int foeLevel, string[] foeMoves,
            BattleRandom chance, System.Action<Pokemon, Pokemon> ready = null)
        {
            var team = new Party();
            var mine = new Pokemon(PokemonDatabase.Get(mineSpecies)!, mineLevel, new Random(mineLevel * 7 + 1));
            mine.Moves.Clear();
            foreach (var m in mineMoves) mine.Moves.Add(new Move(MoveDatabase.Get(m)));
            team.Add(mine);
            var foe = new Pokemon(PokemonDatabase.Get(foeSpecies)!, foeLevel, new Random(foeLevel * 7 + 2));
            foe.Moves.Clear();
            foreach (var m in foeMoves) foe.Moves.Add(new Move(MoveDatabase.Get(m)));
            ready?.Invoke(mine, foe);
            var (name, x, y) = Place("Route201", -1, -1);
            game.Map = MapDatabase.Get(name);
            if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = team, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { foe }, Random = chance
            });
            game.BattleRenderer.SetArena(game.Map, x, y);
            game.Battle = b;
            game.State = GameState.Battle;
            ToMainMenu(b);
            return b;
        }

        // Reads on to the line that holds the words and stops on its first frame
        bool Upon(BattleEngine b, string words)
        {
            for (int i = 0; i < 60 * 30 && !b.IsBattleOver; i++)
            {
                if (b.CurrentMessage.Contains(words)) return true;
                if (b.IsWaitingForConfirm)
                {
                    Skip(0.5);
                    Confirm(b);
                }
                else Frames(1);
            }
            Console.WriteLine($"  !! \"{words}\" was never said");
            return false;
        }

        void Tell(BattleEngine b, string shot)
        {
            foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
            {
                var v = b.Anim[side, 0];
                Console.WriteLine($"  {shot}: {side} {v.Shown?.DisplayName} dodge {v.DodgeAge:0.00} cheer {v.CheerAge:0.00} charge {v.ChargeAge:0.00} " +
                    $"tired {v.Tired:0.00} bar {v.DisplayedHp:0.0}/{v.Shown?.MaxHP} stat {v.StatAge:0.00}");
            }
        }

        // A miss dodged: the foe out of Stone Edge's way (nothing below 96 hits), seen from the front
        var b1 = Battle("Luxray", 40, new[] { "Stone Edge" }, "Bibarel", 30, new[] { "Rock Slide" },
            new BattleRandom(61).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 97).Force(RollKind.SideEffect, 99));
        b1.SelectMove(0);
        if (Upon(b1, "Stone Edge"))
        {
            Skip(0.15); Tell(b1, "96_dodge_a_coming"); Shot("96_dodge_a_coming");
            Skip(0.18); Tell(b1, "96_dodge"); Shot("96_dodge");
            ShotCrop("96_dodge_close", 1090, 60, 760, 520, 2);
            Skip(0.35); Tell(b1, "96_dodge_c_back"); Shot("96_dodge_c_back");
        }
        // And the player's own, from behind, out of the foe's Rock Slide
        if (Upon(b1, "Rock Slide"))
        {
            Skip(0.33); Tell(b1, "96_dodge_mine"); Shot("96_dodge_mine");
        }
        game.State = GameState.Overworld;

        // Both tired at the menu: their bars in the red
        var b2 = Battle("Infernape", 40, new[] { "Flamethrower" }, "Bibarel", 34, new[] { "Tackle" },
            new BattleRandom(62).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0), (mine, foe) =>
            {
                mine.CurrentHP = Math.Max(1, mine.MaxHP / 7);
                foe.CurrentHP = Math.Max(1, foe.MaxHP / 6);
            });
        Skip(1.0); Tell(b2, "96_tired"); Shot("96_tired");
        ShotCrop("96_tired_close", 1090, 60, 760, 520, 2);
        ShotCrop("96_tired_mine_close", 60, 190, 760, 600, 2);
        Skip(1.3); Shot("96_tired_b");
        game.State = GameState.Overworld;

        // A cheer for a win: the foe at a single HP goes down to the player's first blow
        var b3 = Battle("Luxray", 40, new[] { "Spark" }, "Bidoof", 20, new[] { "Growl" },
            new BattleRandom(63).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0), (mine, foe) => foe.CurrentHP = 1);
        b3.SelectMove(0);
        for (int i = 0; i < 60 * 30 && b3.Anim.Player.CheerAge < 0f && !b3.IsBattleOver; i++)
        {
            if (b3.IsWaitingForConfirm)
            {
                Skip(0.5);
                Confirm(b3);
            }
            else Frames(1);
        }
        Skip(0.25); Tell(b3, "96_cheer"); Shot("96_cheer");
        ShotCrop("96_cheer_close", 60, 190, 760, 600, 2);
        Skip(0.35); Tell(b3, "96_cheer_b"); Shot("96_cheer_b");
        game.State = GameState.Overworld;

        // The foe's cheer for the player's loss: the player's last Pokémon at a single HP
        var b4 = Battle("Bidoof", 10, new[] { "Growl" }, "Luxray", 40, new[] { "Spark" },
            new BattleRandom(64).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0), (mine, foe) => mine.CurrentHP = 1);
        b4.SelectMove(0);
        for (int i = 0; i < 60 * 30 && b4.Anim.Enemy.CheerAge < 0f && !b4.IsBattleOver; i++)
        {
            if (b4.IsWaitingForConfirm)
            {
                Skip(1.2);
                Confirm(b4);
            }
            else Frames(1);
        }
        Skip(0.25); Tell(b4, "96_cheer_foe"); Shot("96_cheer_foe");
        ShotCrop("96_cheer_foe_close", 1090, 60, 760, 520, 2);
        // Taken away before the loss is read to its end, which the game would answer with a white-out
        game.State = GameState.Overworld;

        // A charge turn: the foe's Skull Bash, its head tucked down and its aura gathering, and held through the menu
        var b5 = Battle("Luxray", 40, new[] { "Splash" }, "Rhyhorn", 36, new[] { "Skull Bash" },
            new BattleRandom(65).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0));
        b5.SelectMove(0);
        if (Upon(b5, "lowered its head"))
        {
            Skip(0.5); Tell(b5, "96_charge"); Shot("96_charge");
        }
        ToMainMenu(b5);
        Skip(0.8); Tell(b5, "96_charge_menu"); Shot("96_charge_menu");
        game.State = GameState.Overworld;
    }
}
