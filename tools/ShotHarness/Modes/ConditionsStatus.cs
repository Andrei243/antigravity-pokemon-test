partial class Harness
{
    // A condition that lasts (plan 09 · L2), the very last part of `conditions`: a battle at its menu for each of the
    // six conditions, with the foe and the player's own Pokémon both having it from before the battle (so each comes
    // in with it), shot whole (96_status_<condition>) and with each platform at the screen's own resolution
    // (96_status_<condition>_close, _mine_close); the same for a confusion and a love, each given by its own move
    // (96_status_confused, 96_status_infatuated); a Pokémon poisoned, confused and in love at once (96_status_all);
    // the marks from the director's other shots, over the attacker's shoulder and on the target close up
    // (96_status_shot_attacker, 96_status_shot_target); a sleeper as it wakes, its Z's gone (96_status_woken); and a
    // double battle with four conditions on the field (96_status_double). It prints what each shot stands on: what
    // the screen shows of each Pokémon, what the rules have of it, how much of its marks shows, and the quads drawn.
    void StatusLooksMode()
    {
        BattleRandom Chance(uint seed) => new BattleRandom(seed).Force(RollKind.Critical, 1).Force(RollKind.Damage, 0).Force(RollKind.Accuracy, 0)
            .Force(RollKind.SideEffect, 99).Force(RollKind.ConfusionTurns, 3).Force(RollKind.ConfusionSelfHit, 1).Force(RollKind.Infatuation, 1);

        Pokemon Made(string species, int level, Gender gender, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level, gender, Nature.Hardy, false);
            p.Moves.Clear();
            foreach (var m in moves) p.Moves.Add(new Move(MoveDatabase.Get(m)));
            return p;
        }

        BattleEngine Wild(Pokemon enemy, uint seed)
        {
            var (name, x, y) = Place("Route201", -1, -1);
            game.Map = MapDatabase.Get(name);
            if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { enemy }, Random = Chance(seed)
            });
            game.BattleRenderer.SetArena(game.Map, x, y);
            game.Battle = b;
            game.State = GameState.Battle;
            ToMainMenu(b);
            return b;
        }

        Pokemon Mine(Pokemon mine)
        {
            party.HealAll();
            party.Members.Insert(0, mine);
            return mine;
        }

        void Over(Pokemon mine)
        {
            party.Members.Remove(mine);
            game.State = GameState.Overworld;
        }

        void ToMenu(BattleEngine b)
        {
            for (int i = 0; i < 40 && b.HUD.MenuState is BattleMenuState.Message or BattleMenuState.LearnMove && !b.IsBattleOver; i++)
            {
                Skip(1.0);
                Confirm(b);
            }
            Skip(0.6);
        }

        // What the screen shows of the Pokémon in a place, and what the rules have of it
        string Of(BattleEngine b, BattleSide side, int slot = 0)
        {
            var look = b.Anim[side, slot].Condition;
            var rules = ConditionLooks.RulesHave(b.Core.At(new Place(side, slot)));
            string Say(StatusCondition status, bool confused, bool inLove) =>
                string.Join("+", new[] { status == StatusCondition.None ? null : status.ToString(), confused ? "confused" : null, inLove ? "in love" : null }.Where(s => s != null).DefaultIfEmpty("nothing"));
            return $"{(side == BattleSide.Enemy ? "the foe" : "mine")}{(slot > 0 ? " 2" : "")} shows {Say(look.Status, look.Confused, look.InLove)} " +
                $"(the rules: {Say(rules.Status, rules.Confused, rules.InLove)}; {b.Anim.LastingShown(side, slot):0.00} of its marks)";
        }

        void Told(BattleEngine b, string name) =>
            Console.WriteLine($"  {name}: {Of(b, BattleSide.Enemy)}; {Of(b, BattleSide.Player)}; {game.BattleRenderer.Effects.Count} quads");

        // A paralysed Pokémon's mark is drawn only in its fits: on to a moment into the next one
        void IntoAFit(BattleEngine b, BattleSide side)
        {
            var mark = b.Anim[side].Condition.Ailment;
            for (int i = 0; i < 240; i++)
            {
                float into = MoveFx.FitAt(mark.Age).Into;
                if (into >= 0.06f && into <= 0.1f) return;
                Frames(1);
            }
        }

        // The whole picture and both platforms at the screen's own resolution
        void Three(BattleEngine b, string name, bool fits = false)
        {
            if (fits) IntoAFit(b, BattleSide.Enemy);
            Told(b, name);
            Shot(name);
            ShotCrop(name + "_close", 1090, 60, 760, 520, 2);
            if (fits) IntoAFit(b, BattleSide.Player);
            ShotCrop(name + "_mine_close", 60, 190, 760, 600, 2);
        }

        // ---- the six conditions, each on both platforms from before the battle
        var conditions = new (string Name, StatusCondition Status)[]
        {
            ("asleep", StatusCondition.Sleep), ("poisoned", StatusCondition.Poison), ("badly_poisoned", StatusCondition.Toxic),
            ("burned", StatusCondition.Burn), ("paralysed", StatusCondition.Paralyze), ("frozen", StatusCondition.Freeze)
        };
        uint seed = 80;
        foreach (var (name, status) in conditions)
        {
            var mine = Mine(Made("Empoleon", 40, Gender.Male, "Growl", "Splash"));
            mine.Status = status;
            mine.SleepTurns = 6;
            var foe = Made("Bidoof", 40, Gender.Male, "Splash");
            foe.Status = status;
            foe.SleepTurns = 6;
            var b = Wild(foe, seed++);
            // Past the marks' first seconds: every Z, bubble and flame is on its way
            Skip(2.6);
            Three(b, $"96_status_{name}", fits: status == StatusCondition.Paralyze);

            if (status == StatusCondition.Poison)
            {
                // The director's other shots: over the shoulder of the player's poisoned Pokémon as it begins its move,
                // and the poisoned foe close up as the move reaches it
                b.SelectMove(0);
                Skip(0.2);
                Console.WriteLine($"  96_status_shot_attacker: \"{b.CurrentMessage}\", camera {game.BattleRenderer.Shot}; {Of(b, BattleSide.Player)}; {game.BattleRenderer.Effects.Count} quads");
                Shot("96_status_shot_attacker");
                Skip(0.25);
                Console.WriteLine($"  96_status_shot_target: \"{b.CurrentMessage}\", camera {game.BattleRenderer.Shot}; {Of(b, BattleSide.Enemy)}; {game.BattleRenderer.Effects.Count} quads");
                Shot("96_status_shot_target");
                ToMenu(b);
            }
            Over(mine);
        }

        // ---- a confusion and a love, each given to both by its own move
        foreach (var (name, move) in new[] { ("confused", "Confuse Ray"), ("infatuated", "Attract") })
        {
            var mine = Mine(Made("Empoleon", 40, Gender.Male, move, "Splash"));
            var b = Wild(Made("Bidoof", 40, Gender.Female, move), seed++);
            b.SelectMove(0);
            ToMenu(b);
            Skip(2.0);
            Three(b, $"96_status_{name}");
            Over(mine);
        }

        // ---- everything at once: a foe poisoned from the start, then confused, then in love
        {
            var mine = Mine(Made("Empoleon", 40, Gender.Male, "Confuse Ray", "Attract", "Splash"));
            var foe = Made("Bidoof", 40, Gender.Female, "Splash");
            foe.Status = StatusCondition.Poison;
            var b = Wild(foe, seed++);
            b.SelectMove(0);
            ToMenu(b);
            b.SelectMove(1);
            ToMenu(b);
            Skip(2.0);
            Told(b, "96_status_all");
            Shot("96_status_all");
            ShotCrop("96_status_all_close", 1090, 60, 760, 520, 2);
            Over(mine);
        }

        // ---- a sleeper as it wakes: its eyes open and its Z's gone on the line that says so
        {
            var mine = Mine(Made("Empoleon", 40, Gender.Male, "Splash"));
            var foe = Made("Bidoof", 40, Gender.Male, "Splash");
            foe.Status = StatusCondition.Sleep;
            foe.SleepTurns = 1;
            var b = Wild(foe, seed++);
            Skip(2.6);
            Told(b, "96_status_asleep_before_waking");
            ShotCrop("96_status_asleep_before_waking_close", 1090, 60, 760, 520, 2);
            b.SelectMove(0);
            for (int i = 0; i < 20 && !b.CurrentMessage.Contains("woke up") && !b.IsBattleOver; i++)
            {
                Skip(1.0);
                Confirm(b);
            }
            Skip(0.6);
            Told(b, "96_status_woken");
            Shot("96_status_woken");
            ShotCrop("96_status_woken_close", 1090, 60, 760, 520, 2);
            ToMenu(b);
            Over(mine);
        }

        // ---- a double battle from its wider overview: four conditions on the field at once
        {
            var second = Mine(Made("Roserade", 40, Gender.Female, "Splash"));
            second.Status = StatusCondition.Sleep;
            second.SleepTurns = 6;
            var first = Mine(Made("Empoleon", 40, Gender.Male, "Splash"));
            first.Status = StatusCondition.Poison;
            second.Status = StatusCondition.Sleep;
            var pair = new List<Pokemon> { Made("Bidoof", 40, Gender.Male, "Splash"), Made("Starly", 40, Gender.Male, "Splash") };
            pair[0].Status = StatusCondition.Burn;
            pair[1].Status = StatusCondition.Freeze;
            var (map, px, py) = Place("Route201", -1, -1);
            game.Map = MapDatabase.Get(map);
            if (px >= 0) game.Player.SetPosition(px, py, Direction.Up);
            var db = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = pair, Format = BattleFormat.Double, Random = Chance(seed++)
            });
            game.BattleRenderer.SetArena(game.Map, px, py);
            game.Battle = db;
            game.State = GameState.Battle;
            ToMainMenu(db);
            Skip(2.6);
            Console.WriteLine($"  96_status_double: {Of(db, BattleSide.Enemy)}; {Of(db, BattleSide.Enemy, 1)}; {Of(db, BattleSide.Player)}; {Of(db, BattleSide.Player, 1)}; {game.BattleRenderer.Effects.Count} quads");
            Shot("96_status_double");
            Over(first);
            Over(second);
        }
    }
}
