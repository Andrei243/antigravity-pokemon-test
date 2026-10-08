partial class Harness
{
    // ---------------------------------------------------------------- wild Pokémon (plan 06 · R13)

    // The wild Pokémon beyond the tables, in a game of their own (en*; not part of "all"): the Poké Radar's patches
    // shaking and a patch walked into, a honey tree's question, its honey, the tree shaking with what came and its
    // battle, poison biting in the field and a Pokémon pulling through, the assistant's sister's news of swarms and a
    // swarm's Pokémon, a roaming Pokémon met and fleeing, and Feebas at Mt. Coronet's lake. It prints what each left
    // behind (the chain, the tree, the roamer's HP and place, the day's Feebas tiles).
    public void EncountersMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var team = game.Party;
        var bag = game.Bag;
        var state = game.Encounters;
        var radar = game.Radar;
        var sinnoh = MapDatabase.Get("Sinnoh");

        // At a spot, with no arrival sign left over from the last place (At moves the player without arriving)
        void Go(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.LocationSign.Hide();
        }
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        void ReadOn(int most = 12)
        {
            for (int i = 0; i < most && Box().IsActive && !Box().IsQuestion; i++) { Whole(); Next(); }
        }
        // A battle ended: the foe brought down to 1 HP and struck, or run from
        void EndBattle(bool run)
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 200 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    if (run) fight.SelectMainMenuOption(3);
                    else
                    {
                        if (fight.EnemyPokemon is { } wild) wild.CurrentHP = Math.Min(wild.CurrentHP, 1);
                        fight.SelectMainMenuOption(0);
                        fight.SelectMove(0);
                    }
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => State() == GameState.Overworld, "the field again", 1500);
            Frames(10);
        }

        // A strong lead, so nothing here is a struggle, and the items each scene uses
        team.Clear();
        var lead = new Pokemon(PokemonDatabase.Get("Staraptor")!, 50);
        team.Add(lead);
        team.Add(new Pokemon(PokemonDatabase.Get("Bibarel")!, 40));
        bag.AddItem(ItemDatabase.Get("Poké Radar")!, 1);
        bag.AddItem(ItemDatabase.Get("Honey")!, 3);
        bag.AddItem(ItemDatabase.Get("Super Rod")!, 1);

        // ---- the Poké Radar on Route 202: the tile of tall grass with the most tall grass round it
        var r202 = sinnoh.FindArea("route_202")!;
        var b202 = sinnoh.AreaBounds("route_202")!.Value;
        (int X, int Y) best = default;
        int most = -1;
        for (int y = b202.Y + 4; y < b202.Y + b202.Height - 4; y++)
            for (int x = b202.X + 4; x < b202.X + b202.Width - 4; x++)
            {
                if (sinnoh.AreaAt(x, y) != r202 || sinnoh.BehaviourAt(x, y) != TileBehavior.TallGrass || !sinnoh.IsWalkable(x, y)) continue;
                int grass = 0;
                for (int dy = -4; dy <= 4; dy++)
                    for (int dx = -4; dx <= 4; dx++)
                        if (sinnoh.BehaviourAt(x + dx, y + dy) == TileBehavior.TallGrass) grass++;
                if (grass > most) (best, most) = ((x, y), grass);
            }
        Go("Sinnoh", best.X, best.Y, Direction.Down);
        state.RadarCharge = RadarChain.BatterySteps;
        game.UseFieldItem(ItemDatabase.Get("Poké Radar")!);
        Frames(50); Shot("en01_radar_shaking");
        Console.WriteLine($"radar: at {best.X},{best.Y}, {radar.Patches.Count(p => p.Active)} patches shaking: " +
            string.Join(", ", radar.Patches.Where(p => p.Active).Select(p => $"{p.X},{p.Y} {p.Shake}{(p.ContinuesChain ? " goes on" : "")}")));

        // Into a shaking patch, from the tile beside it: a Pokémon whatever the odds, which starts a chain
        var patch = radar.Patches.Where(p => p.Active).OrderBy(p => Math.Abs(p.X - best.X) + Math.Abs(p.Y - best.Y)).FirstOrDefault();
        if (patch != null)
        {
            var from = new[] { (0, 1, Direction.Up), (0, -1, Direction.Down), (1, 0, Direction.Left), (-1, 0, Direction.Right) }
                .Select(s => (X: patch.X + s.Item1, Y: patch.Y + s.Item2, Way: s.Item3))
                .FirstOrDefault(s => sinnoh.IsWalkable(s.X, s.Y) && !radar.Patches.Any(p => p.Active && (p.X, p.Y) == (s.X, s.Y)));
            game.Player.SetPosition(from.X, from.Y, from.Way);
            Frames(2);
            engine.Steering = (from.Way, false);
            Until(() => State() != GameState.Overworld, "the patch's Pokémon", 120);
            engine.Steering = null;
            Until(() => State() == GameState.Battle, "the patch's battle", 300);
            var fight = game.Battle;
            ToMainMenu(fight);
            Shot("en02_radar_patch_battle");
            Console.WriteLine($"radar: met {fight.EnemyPokemon?.Species.Name} at {fight.EnemyPokemon?.Level}, chain {radar.Count} of {radar.Species}");
            EndBattle(run: false);
            Frames(40); Shot("en03_radar_after_a_win");
            Console.WriteLine($"radar: after the win the chain is {radar.Count}, {radar.Patches.Count(p => p.Active)} patches shaking, {radar.Patches.Count(p => p.Active && p.ContinuesChain)} going on with it");
        }

        // ---- the honey tree on Route 205's south: faced from the south, with Honey in the bag
        var r205 = sinnoh.FindArea("route_205_south")!;
        var tree = sinnoh.Props.First(p => p.Type == PropType.HoneyTree && sinnoh.AreaAt(p.X, p.Y) == r205);
        int tx = Enumerable.Range(tree.X, tree.Width).OrderBy(x => Math.Abs(x - (tree.X + tree.Width / 2))).First(x => sinnoh.IsWalkable(x, tree.Y + tree.Depth));
        Go("Sinnoh", tx, tree.Y + tree.Depth, Direction.Up);
        Frames(10);
        game.Interact(); Frames(3);
        Whole(); Next();
        Until(() => engine.Choice.IsOpen, "the tree's question"); Frames(20); Shot("en04_honey_tree_asks");
        engine.Choice.Confirm(); Frames(4);
        Until(() => Box().IsActive, "the honey's line"); Whole(); Shot("en05_honey_slathered");
        ReadOn();
        Until(() => !engine.ScriptRunning, "the tree's script to end");
        var honeyTree = state.Trees[HoneyTrees.IdOf("route_205_south")!.Value];
        Console.WriteLine($"honey tree: {honeyTree.MinutesLeft} minutes of honey, group {honeyTree.Group}, slot {honeyTree.Slot}, shakes {honeyTree.Shakes}");

        // Six hours later: it shakes with what came (the uncommon group, shaking hard), and the battle begins at a look
        honeyTree.MinutesLeft = 600;
        honeyTree.Group = 2;
        honeyTree.Slot = 5;
        honeyTree.Shakes = 3;
        Frames(45); Shot("en06_honey_tree_shaking");
        game.Interact(); Frames(3);
        Until(() => State() == GameState.Battle, "the tree's battle", 300);
        var treeFight = game.Battle;
        ToMainMenu(treeFight);
        Shot("en07_honey_tree_battle");
        Console.WriteLine($"honey tree: met {treeFight.EnemyPokemon?.Species.Name} at {treeFight.EnemyPokemon?.Level}, holding {treeFight.EnemyPokemon?.HeldItem?.Name ?? "nothing"}");
        EndBattle(run: true);
        for (int i = 0; i < 300 && (engine.ScriptRunning || Box().IsActive); i++)
        {
            if (engine.Choice.IsOpen) { Frames(4); engine.Choice.Back(); Frames(4); }
            else if (Box().IsActive) { Whole(); Next(); }
            else Frames(1);
        }
        Console.WriteLine($"honey tree: afterwards {honeyTree.MinutesLeft} minutes of honey");

        // ---- poison in the field: a step in four bites, and a Pokémon down to one hit point pulls through
        Go("Sinnoh", 112, 880, Direction.Down);
        lead.Status = StatusCondition.Poison;
        lead.CurrentHP = 3;
        state.PoisonSteps = 0;
        var walks = new[] { Direction.Down, Direction.Up, Direction.Down, Direction.Up };
        foreach (var way in walks)
        {
            engine.Steering = (way, false);
            Until(() => game.Player.IsMoving, "a step", 30);
            engine.Steering = null;
            Until(() => !game.Player.IsMoving, "the step's end", 60);
        }
        Frames(2); Shot("en08_poison_bites");
        foreach (var way in walks)
        {
            engine.Steering = (way, false);
            Until(() => game.Player.IsMoving || Box().IsActive, "a step", 30);
            engine.Steering = null;
            Until(() => !game.Player.IsMoving, "the step's end", 60);
        }
        Until(() => Box().IsActive, "the poison's line", 120);
        Whole(); Shot("en09_poison_survived");
        ReadOn();
        Until(() => !engine.ScriptRunning, "the poison's script to end");
        Console.WriteLine($"poison: {lead.Nickname} at {lead.CurrentHP} HP, {lead.Status}");
        Frames(30);   // the flash fades

        // ---- swarms: the assistant's sister tells of them (her house is to come: a stand-in for her), and a swarm's Pokémon
        state.SwarmDaily = 1;   // Route 202's, its Zigzagoon
        Go("Sinnoh", 116, 888, Direction.Down);
        engine.StartScript(FieldScripts.SwarmNews, new NPC { Name = "{assistant}'s sister", NpcType = "Woman" });
        Frames(3); Whole(); Shot("en10_swarms_begin");
        ReadOn();
        Until(() => !engine.ScriptRunning, "the sister's first news");
        engine.StartScript(FieldScripts.SwarmNews, new NPC { Name = "{assistant}'s sister", NpcType = "Woman" });
        Frames(3); Whole(); Shot("en11_swarm_today");
        ReadOn();
        Until(() => !engine.ScriptRunning, "the sister's news");
        Go("Sinnoh", best.X, best.Y, Direction.Down);
        var moment = game.EncounterMomentNow();
        var slots = sinnoh.WildAt(best.X, best.Y, moment: moment).Table;
        Console.WriteLine($"swarm: on {Swarms.Today(state)}, Route 202's grass is now {string.Join(", ", slots.Select(s => s.SpeciesName))}");
        WildEncounterEntry? swarming = null;
        for (int i = 0; i < 200 && swarming?.SpeciesName != "Zigzagoon"; i++) swarming = sinnoh.DrawOutWild(best.X, best.Y, lead: null, moment: moment);
        game.StartWildBattle(swarming!);
        Until(() => State() == GameState.Battle, "the swarm's battle", 300);
        var swarmFight = game.Battle;
        ToMainMenu(swarmFight);
        Shot("en12_swarm_battle");
        EndBattle(run: true);

        // ---- a roaming Pokémon: Mesprit set loose, met on Route 202, fleeing at once and keeping its HP
        lead.CurrentHP = lead.MaxHP;
        Roamers.SetLoose(state, Roamers.SlotOf("Mesprit")!.Value, new Random(5));
        state.Roamers[0].Route = Array.IndexOf(Roamers.Routes, "route_202");
        state.Roamers[0].Pokemon!.CurrentHP -= 30;
        Go("Sinnoh", best.X, best.Y, Direction.Down);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Mesprit", MinLevel = 50, MaxLevel = 50, Roamer = 0 });
        Until(() => State() == GameState.Battle, "the roamer's battle", 300);
        var roam = game.Battle;
        ToMainMenu(roam);
        Shot("en13_roamer_met");
        roam.SelectMainMenuOption(0);
        roam.SelectMove(0);
        Skip(0.5);
        // Read on to the line that says it ran, and catch it there
        for (int i = 0; i < 12 && !roam.IsBattleOver && !roam.CurrentMessage.Contains("ran away"); i++) { Confirm(roam); Skip(0.6); }
        Skip(0.4);
        Shot("en14_roamer_flees");
        EndBattle(run: true);
        Console.WriteLine($"roamer: Mesprit at {state.Roamers[0].Pokemon?.CurrentHP} HP, now on {Roamers.PlaceOf(state.Roamers[0])}, roaming {state.Roamers[0].Active}");

        // ---- Feebas: the day's four tiles of Mt. Coronet's lake, and what a rod cast onto one brings up
        var lakeTiles = SpecialEncounterTables.FeebasTiles;
        var today = Feebas.TilesToday(lakeTiles, state.DailyNumber);
        var coronet = MapDatabase.Get("MtCoronetB1F");
        var feebasMoment = new EncounterMoment { State = state };
        var bites = new Dictionary<string, int>();
        for (int i = 0; i < 200; i++)
            if (coronet.Fish(today[0].X, today[0].Y, FishingRod.Super, null, feebasMoment) is { } bite) bites[bite.SpeciesName] = bites.GetValueOrDefault(bite.SpeciesName) + 1;
        Console.WriteLine($"feebas: today's tiles {string.Join(" ", today.Select(t => $"{t.X},{t.Y}"))}; 200 casts onto the first: {string.Join(", ", bites.Select(b => $"{b.Key} {b.Value}"))}");
        // The lake seen from a shore beside one of its tiles
        var shore = lakeTiles.SelectMany(t => new[] { (0, 1, Direction.Up), (0, -1, Direction.Down), (1, 0, Direction.Left), (-1, 0, Direction.Right) }
                .Select(s => (X: t.X + s.Item1, Y: t.Y + s.Item2, Way: s.Item3)))
            .FirstOrDefault(s => coronet.IsWalkable(s.X, s.Y));
        if (shore != default)
        {
            Go("MtCoronetB1F", shore.X, shore.Y, shore.Way);
            Frames(20); Shot("en15_feebas_lake");
            game.Fishing = new FishingAttempt(FishingRod.Super, new WildEncounterEntry { SpeciesName = "Feebas", MinLevel = 15, MaxLevel = 15 }, new Random(3));
            Until(() => engine.CastStage == FishingStage.Hooked, "the bite", 400);
            engine.FishingPress = true; Frames(4);
            Whole(); Next();
            Until(() => State() == GameState.Battle, "Feebas's battle", 600);
            var feebasFight = game.Battle;
            ToMainMenu(feebasFight);
            Shot("en16_feebas_battle");
            EndBattle(run: true);
        }
        else Console.WriteLine("  !! no shore beside Feebas's lake");
    }
}
