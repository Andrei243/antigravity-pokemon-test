partial class Harness
{
    // The turn's moments (plan 25 · Y3), the last part of `conditions`: each played by its own battle through the
    // game's own menus and read on to the line that tells of it, then shot a moment into its mark, whole
    // (96_turn_<moment>) and with its Pokémon's platform at the screen's own resolution (96_turn_<moment>_close).
    // Poison and a burn biting at the turn's end, Leech Seed's sap on its way to the seeder, Fire Spin, a curse,
    // Leftovers on the player's own Pokémon, a berry eaten, the Great Marsh's bait as it lands and as it is eaten,
    // a Pokémon fast asleep, one confused, a level with its panel over the line, a Potion used; an attack sent ahead
    // landing (96_future_sight, 96_doom_desire); two poisoned foes of a double battle, each bitten in its turn
    // (96_turn_double_poison, _second). It reads each battle on a line at a time as a player would, so the
    // camera is back on the overview when a turn ends, and prints what the screen holds at each shot: the mark
    // playing, whether the body flinched, and the HP as the bar has it. Every line each battle says is printed too.
    //
    // The same file builds against a tree from before the moments (it asks the engine for nothing they brought but
    // through MomentCues below), which is how the "before" of each of these pictures is taken.
    void MomentsMode()
    {
        BattleEngine Wild(string foe, int level, string foeMove, uint seed, Action<Pokemon> ready = null, BattleKind kind = BattleKind.Normal)
        {
            var enemy = new Pokemon(PokemonDatabase.Get(foe)!, level, new Random((int)seed));
            enemy.Moves.Clear();
            enemy.Moves.Add(new Move(MoveDatabase.Get(foeMove)));
            ready?.Invoke(enemy);
            var (name, x, y) = Place("Route201", -1, -1);
            game.Map = MapDatabase.Get(name);
            if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { enemy },
                Random = new BattleRandom(seed).Force(RollKind.Critical, 1).Force(RollKind.Damage, 0).Force(RollKind.Accuracy, 0)
                    .Force(RollKind.SideEffect, 99).Force(RollKind.Duration, 1).Force(RollKind.Safari, 3, 254, 3, 254),
                Kind = kind, SpecialBalls = kind == BattleKind.Safari ? 30 : 0
            });
            game.BattleRenderer.SetArena(game.Map, x, y);
            game.Battle = b;
            game.State = GameState.Battle;
            ToMainMenu(b);
            return b;
        }

        Pokemon Mine(string species, int level, params string[] moves)
        {
            var mine = new Pokemon(PokemonDatabase.Get(species)!, level, new Random(species.Length * 31 + level));
            mine.Moves.Clear();
            foreach (var m in moves) mine.Moves.Add(new Move(MoveDatabase.Get(m)));
            party.HealAll();
            party.Members.Insert(0, mine);
            return mine;
        }

        void Over(Pokemon mine)
        {
            party.Members.Remove(mine);
            game.State = GameState.Overworld;
        }

        // Reads on, a line every 1.2 seconds as a player reads, to the line that holds the words, and stops on its
        // first frame: what the line starts has only just begun
        bool ReadTo(BattleEngine b, string words)
        {
            // (A choice's first line is on screen as the choice is made: the bait's is the one wanted)
            if (b.IsWaitingForConfirm && b.CurrentMessage.Contains(words)) return true;
            for (int i = 0; i < 40 && !b.IsBattleOver; i++)
            {
                if (b.HUD.MenuState is not (BattleMenuState.Message or BattleMenuState.LearnMove)) break;
                // The line on screen is read before it is sent on: what it started has played, and the camera is back
                Skip(1.2);
                Confirm(b);
                if (b.CurrentMessage.Contains(words)) return true;
            }
            Console.WriteLine($"  !! \"{words}\" was never said");
            return false;
        }

        void ToMenu(BattleEngine b)
        {
            for (int i = 0; i < 40 && b.HUD.MenuState is BattleMenuState.Message or BattleMenuState.LearnMove && !b.IsBattleOver; i++)
            {
                Confirm(b);
                Skip(1.0);
            }
            Skip(0.6);
        }

        // The two platforms at the screen's own resolution (the player's own from higher up: what sits over a tall
        // Pokémon's head there is a third of the way up the picture)
        void Close(string name, bool foe) => ShotCrop(name, foe ? 1090 : 60, foe ? 60 : 190, 760, foe ? 520 : 600, 2);

        // A shot `after` seconds into the line on screen, with what the screen holds then (the close-up under another
        // name where `<name>_close` is taken: 96_future_sight_close is the attack waiting, in ConditionsField.cs)
        void Mark(BattleEngine b, string name, bool foe, double after, string close = null)
        {
            Skip(after);
            var side = foe ? BattleSide.Enemy : BattleSide.Player;
            var view = b.Anim[side];
            Console.WriteLine($"  {name}: {MomentCues(b)}; the body {(view.HitAge >= 0f ? "flinches" : "is still")}, the camera {(b.Anim.ShakeAge >= 0f ? "shakes" : "is steady")}; " +
                $"HP {view.Shown?.CurrentHP}/{view.Shown?.MaxHP}, the bar at {view.DisplayedHp:0.0}; {game.BattleRenderer.Effects.Count} quads");
            Shot(name);
            Close(close ?? name + "_close", foe);
        }

        // ---- poison and a burn biting at the turn's end
        var idler = Mine("Empoleon", 40, "Splash");
        var pb = Wild("Bidoof", 40, "Splash", 51, foe => foe.Status = StatusCondition.Poison);
        pb.SelectMove(0);
        if (ReadTo(pb, "is hurt by poison")) Mark(pb, "96_turn_poison", foe: true, 0.3);
        Skip(0.25);
        Mark(pb, "96_turn_poison_landed", foe: true, 0);
        ToMenu(pb);

        var bb = Wild("Bidoof", 40, "Splash", 52, foe => foe.Status = StatusCondition.Burn);
        bb.SelectMove(0);
        if (ReadTo(bb, "is hurt by its burn")) Mark(bb, "96_turn_burn", foe: true, 0.4);
        ToMenu(bb);

        // ---- fast asleep and confused, as each would have moved
        var ab = Wild("Bidoof", 40, "Splash", 53, foe => { foe.Status = StatusCondition.Sleep; foe.SleepTurns = 4; });
        ab.SelectMove(0);
        if (ReadTo(ab, "is fast asleep")) Mark(ab, "96_turn_asleep", foe: true, 0.55);
        ToMenu(ab);
        Over(idler);

        var dazer = Mine("Gengar", 40, "Confuse Ray", "Splash");
        var cb = Wild("Bidoof", 40, "Splash", 54);
        cb.SelectMove(0);
        if (ReadTo(cb, "is confused!")) Mark(cb, "96_turn_confused", foe: true, 0.45);
        ToMenu(cb);
        Over(dazer);

        // ---- Leech Seed: the seeded foe's health on its way to the seeder
        var seeder = Mine("Roserade", 40, "Leech Seed", "Splash");
        seeder.CurrentHP = seeder.MaxHP / 2;
        var lb = Wild("Bidoof", 40, "Splash", 55);
        lb.SelectMove(0);
        if (ReadTo(lb, "health is sapped by Leech Seed"))
        {
            Mark(lb, "96_turn_leech_seed", foe: true, 0.4);
            Skip(0.3);
            Mark(lb, "96_turn_leech_seed_arriving", foe: false, 0);
        }
        ToMenu(lb);
        Over(seeder);

        // ---- Fire Spin and a curse
        var spinner = Mine("Infernape", 40, "Fire Spin", "Splash");
        var fb = Wild("Bidoof", 40, "Splash", 56);
        fb.SelectMove(0);
        if (ReadTo(fb, "is hurt by Fire Spin")) Mark(fb, "96_turn_fire_spin", foe: true, 0.4);
        ToMenu(fb);
        Over(spinner);

        var curser = Mine("Gengar", 40, "Curse", "Splash");
        var ub = Wild("Bidoof", 40, "Splash", 57);
        ub.SelectMove(0);
        if (ReadTo(ub, "is afflicted by the curse")) Mark(ub, "96_turn_curse", foe: true, 0.4);
        ToMenu(ub);
        Over(curser);

        // ---- Leftovers on the player's own Pokémon, and a berry eaten by the foe
        var holder = Mine("Empoleon", 40, "Splash", "Tackle");
        holder.HeldItem = ItemDatabase.Get("Leftovers");
        holder.CurrentHP = holder.MaxHP / 2;
        var hb = Wild("Bidoof", 40, "Splash", 58, foe =>
        {
            foe.HeldItem = ItemDatabase.Get("Sitrus Berry");
            foe.CurrentHP = foe.MaxHP / 2 + 3;
        });
        hb.SelectMove(1);
        if (ReadTo(hb, "restored its health using its Sitrus Berry")) Mark(hb, "96_turn_berry", foe: true, 0.25);
        if (ReadTo(hb, "restored a little HP using its Leftovers")) Mark(hb, "96_turn_leftovers", foe: false, 0.3);
        ToMenu(hb);

        // ---- a Potion from the bag
        inventory.AddItem(ItemDatabase.Get("Potion")!, 1);
        hb.UseBagItem("Potion");
        if (ReadTo(hb, "used the Potion")) Mark(hb, "96_turn_bag_item", foe: false, 0.4);
        ToMenu(hb);
        Over(holder);

        // ---- a level, with its panel over the line
        var grower = Mine("Turtwig", 8, "Tackle");
        grower.CurrentExp = grower.ExpForNextLevel - 1;
        var gb = Wild("Bidoof", 3, "Splash", 59);
        gb.EnemyPokemon.CurrentHP = 1;
        gb.SelectMove(0);
        if (ReadTo(gb, "grew to Lv.")) Mark(gb, "96_turn_level", foe: false, 0.45);
        ToMenu(gb);
        Over(grower);

        // ---- the Great Marsh: bait as it lands, and as it is eaten
        var sb = Wild("Carnivine", 25, "Splash", 60, kind: BattleKind.Safari);
        sb.SelectMainMenuOption(1);
        if (ReadTo(sb, "tossed some bait")) Mark(sb, "96_turn_bait", foe: true, 0.35);
        if (ReadTo(sb, "munching away")) Mark(sb, "96_turn_bait_eating", foe: true, 0.4);
        ToMenu(sb);
        if (!sb.IsBattleOver)
        {
            sb.SelectMainMenuOption(2);
            if (ReadTo(sb, "is fuming")) Mark(sb, "96_turn_mud", foe: true, 0.4);
        }
        game.State = GameState.Overworld;

        // ---- an attack sent ahead, landing: Future Sight's rings and Doom Desire's pillar
        foreach (var (species, move, shot) in new[] { ("Alakazam", "Future Sight", "96_future_sight"), ("Jirachi", "Doom Desire", "96_doom_desire") })
        {
            var seer = Mine(species, 40, move, "Splash");
            var wb = Wild("Snorlax", 40, "Splash", 61);
            wb.SelectMove(0);
            ToMenu(wb);
            wb.SelectMove(1);
            ToMenu(wb);
            wb.SelectMove(1);
            if (ReadTo(wb, $"took the {move} attack"))
            {
                Mark(wb, shot + "_falling", foe: true, 0.25);
                Skip(0.15);
                Mark(wb, shot, foe: true, 0, close: shot + "_landed_close");
            }
            ToMenu(wb);
            Over(seer);
        }

        // ---- a double battle, from its wider overview: two poisoned foes, each bitten in its turn
        var second = Mine("Roserade", 40, "Splash");
        var first = Mine("Empoleon", 40, "Splash");
        var pair = new List<Pokemon>();
        for (int i = 0; i < 2; i++)
        {
            var foe = new Pokemon(PokemonDatabase.Get(i == 0 ? "Bidoof" : "Starly")!, 40, new Random(62 + i)) { Status = StatusCondition.Poison };
            foe.Moves.Clear();
            foe.Moves.Add(new Move(MoveDatabase.Get("Splash")));
            pair.Add(foe);
        }
        var (map, px, py) = Place("Route201", -1, -1);
        game.Map = MapDatabase.Get(map);
        if (px >= 0) game.Player.SetPosition(px, py, Direction.Up);
        var db = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = pair, Format = BattleFormat.Double,
            Random = new BattleRandom(62).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0)
        });
        game.BattleRenderer.SetArena(game.Map, px, py);
        game.Battle = db;
        game.State = GameState.Battle;
        ToMainMenu(db);
        for (int i = 0; i < 2 && db.HUD.MenuState is BattleMenuState.Main or BattleMenuState.SelectTarget; i++)
        {
            db.SelectMove(0);
            if (db.HUD.MenuState == BattleMenuState.SelectTarget) db.SelectTarget(0);
        }
        foreach (string shot in new[] { "96_turn_double_poison", "96_turn_double_poison_second" })
        {
            // (The first foe's line is still up as the second's is asked for: read it on first)
            if (db.IsWaitingForConfirm && db.CurrentMessage.Contains("is hurt by poison")) { Skip(1.2); Confirm(db); }
            if (!ReadTo(db, "is hurt by poison")) break;
            Skip(0.4);
            Console.WriteLine($"  {shot}: \"{db.CurrentMessage}\"; {MomentCues(db)}; {game.BattleRenderer.Effects.Count} quads");
            Shot(shot);
        }
        ToMenu(db);
        Over(first);
        Over(second);

        MomentBoards();
    }

    // The moment cues playing, by name: read through the cues' own text so that this file also builds against a tree
    // from before there were any (the "before" pictures)
    static string MomentCues(BattleEngine b)
    {
        var playing = b.Anim.Cues.Where(c => c.Kind.ToString() == "Moment").Select(c => $"{c.GetType().GetProperty("Moment")?.GetValue(c)} at {c.Age:0.00}").ToList();
        return playing.Count == 0 ? "no moment plays" : string.Join(", ", playing);
    }
}
