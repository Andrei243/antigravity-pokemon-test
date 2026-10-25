partial class Harness
{
    // The ways to play (plan 13; not part of `all`; wy*): choices made once at NEW GAME, each shot in a game begun with
    // the run it shows (`engine.StartNewGame(name, look, rival, run)`), so the harness's own game and every other mode
    // are untouched. V3: the level cap and the line a battle says when it holds a Pokémon back (wy_level_cap_line), and
    // Roark under Challenge (wy_roark_normal beside wy_roark_challenge: his levels raised, a fourth Pokémon and a held
    // item, which it prints, since the menu shows only the first Pokémon out). Later V sessions add their shots below.
    public void WaysMode()
    {
        LevelCapMode();
        RoarkMode();
        NuzlockeMode();
        RestrictionsMode();
        RandomizerMode();
    }

    // V4, the Nuzlocke: wy_nuzlocke_ball_refused (a second wild Pokémon met on Route 201, whose catch the run has had, and
    // the bag's Poké Ball refused with the battle's line), wy_nickname (the keyboard over the field for a Pokémon just
    // caught) and wy_trainer_card_graveyard (the card's Run page with three fallen). It prints the place's key, the line,
    // and the graveyard as the run keeps it
    void NuzlockeMode()
    {
        BeginRun(RunRules.NuzlockePreset);
        game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 5);
        var sinnoh = MapDatabase.Get("Sinnoh");
        var (gx, gy) = NearestGrass(sinnoh, 116, 886);
        At("Sinnoh", gx, gy, Direction.Up);
        string place = Nuzlocke.PlaceKeyOf(sinnoh, gx, gy);
        game.RunState.Meet(place);
        Console.WriteLine($"nuzlocke: Route 201's grass at {gx},{gy} is {place}; met there already: {game.RunState.HasMet(place)}");
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 3, MaxLevel = 3, Weight = 100 });
        for (int i = 0; i < 900 && game.State != GameState.Battle; i += 2) Frames(2);
        var b = game.Battle;
        ToMainMenu(b);
        b.HUD.MenuState = BattleMenuState.SelectBagPocket;
        b.SelectBagPocket(2);
        int ball = b.BagListed.ToList().FindIndex(s => s.Name == "Poké Ball");
        b.SelectBagItem(Math.Max(0, ball));
        Skip(0.6);
        Shot("wy_nuzlocke_ball_refused");
        Console.WriteLine($"nuzlocke: \"{b.CurrentMessage}\"");
        game.State = GameState.Overworld;
        Frames(10);

        // A Pokémon just caught: the keyboard opens over the field once it is back
        var caught = new Pokemon(PokemonDatabase.Get("Starly")!, 4);
        game.Party.Add(caught);
        game.NameAfterBattle = caught;
        Frames(30);
        Shot("wy_nickname");
        Console.WriteLine($"nuzlocke: the keyboard is {(game.State == GameState.Nickname ? "open" : "shut")} for {game.Nickname.Target?.Species.Name}");
        game.Nickname.Cancel();
        Frames(10);
        game.State = GameState.Overworld;

        // Three fallen, as the run keeps them, and the card turned to its Run page
        foreach (var (species, level, nickname) in new[] { ("Turtwig", 9, "Leafy"), ("Starly", 7, ""), ("Shinx", 11, "Sparks") })
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level) { Nickname = nickname, CurrentHP = 0 };
            var team = new Party();
            team.Add(p);
            Nuzlocke.Bury(game.RunState, team, place, sinnoh.DisplayNameAt(gx, gy), 600);
        }
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        game.TrainerCard.Turn(1);
        game.TrainerCard.Turn(1);
        Frames(40);
        Shot("wy_trainer_card_graveyard");
        Console.WriteLine($"nuzlocke: graveyard {string.Join(", ", game.RunState.Graveyard.Select(f => $"{f.Pokemon.SpeciesName} {f.Pokemon.Level} at {f.Place}"))}");
        game.TrainerCard.Close();
        game.State = GameState.Overworld;
        Frames(10);
    }

    // V5, the restrictions: wy_nurse_declines (a run without the Centers, the nurse's line of our own once she is spoken to,
    // the team still hurt) and wy_bag_greyed (a run with no items in battle, in Roark's battle: BAG painted grey in its
    // place, which choosing says why). It prints the lines, the team's HP and what BagClosed says in a trainer's battle
    void RestrictionsMode()
    {
        BeginRun(new RunRules { NoCenter = true });
        var lead = game.Party.Members[0];
        lead.CurrentHP = 1;
        At("PokemonCenter", 8, 6, Direction.Up);
        game.Interact();
        Frames(3);
        var box = game.Dialogue;
        for (int i = 0; i < 300 && !box.IsActive; i++) Frames(1);
        box.FinishLine();
        Frames(2);
        box.Advance();
        Frames(2);
        box.FinishLine();
        Frames(2);
        Shot("wy_nurse_declines");
        Console.WriteLine($"restrictions: the nurse says \"{box.CurrentLine}\"");
        for (int i = 0; i < 8 && box.IsActive; i++) { box.FinishLine(); Frames(2); box.Advance(); Frames(2); }
        for (int i = 0; i < 120 && engine.ScriptRunning; i++) Frames(1);
        Console.WriteLine($"restrictions: the lead has {lead.CurrentHP} of {lead.MaxHP} HP after the visit (hurt: {lead.CurrentHP < lead.MaxHP}), script running {engine.ScriptRunning}");

        BeginRun(new RunRules { NoBattleItems = true });
        var hall = MapDatabase.Get("OreburghGym");
        var person = hall.Everyone.First(n => n.Name.Equals("Roark", StringComparison.OrdinalIgnoreCase));
        At("OreburghGym", person.GridX, person.GridY + 1, Direction.Up);
        Frames(30);
        var met = Carrying("leader_roark");
        met.NpcType = person.NpcType;
        game.StartTrainerBattle(met);
        for (int i = 0; i < 900 && game.State != GameState.Battle; i += 2) Frames(2);
        var b = game.Battle;
        ToMainMenu(b);
        Shot("wy_bag_greyed");
        Console.WriteLine($"restrictions: Roark's battle, BAG closed {b.BagClosed}");
        b.SelectMainMenuOption(1);
        Skip(0.4);
        Console.WriteLine($"restrictions: choosing BAG says \"{b.CurrentMessage}\"");
        game.State = GameState.Overworld;
        Frames(10);
    }

    // The tall grass nearest a tile, open to walk on
    static (int X, int Y) NearestGrass(Map map, int x, int y)
    {
        for (int r = 0; r < 80; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && map.IsTallGrass(x + dx, y + dy) && map.IsWalkable(x + dx, y + dy))
                        return (x + dx, y + dy);
        return (x, y);
    }

    // A new game of its own under a run, past the first chapter, in front of the house
    void BeginRun(RunRules run)
    {
        engine.StartNewGame(null, PlayerLook.Boy, null, run);
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        PastTheOpening();
    }

    // wy_level_cap_line: a Turtwig of level 13 beats a foe that gives far more than the way to level 14 and the cap
    // (Roark's best, 14) holds it there: it grows to 14, the battle says it can't grow past it yet, and its EXP is the
    // very start of 14. Read a line at a time, as a player reads, to the line itself
    void LevelCapMode()
    {
        BeginRun(new RunRules { LevelCap = true });
        var turtwig = game.Party.Members[0];
        turtwig.Relevel(13);
        int cap = game.LevelCap ?? 0;
        Console.WriteLine($"level cap: {cap} with no Badge (Roark's best is 14); the Turtwig is level {turtwig.Level} with {turtwig.CurrentExp} EXP");

        var (name, x, y) = Place("Route201", -1, -1);
        game.Map = MapDatabase.Get(name);
        if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
        var foe = Meet("Onix", 100);
        foe.CurrentHP = 1;
        // a level 100 Onix is faster and strikes first: it knows only Harden, so the Turtwig is there to be given the EXP
        foe.Moves.Clear();
        foe.Moves.Add(MoveDatabase.Create("Harden"));
        var b = new BattleEngine(new BattleSetup
        {
            PlayerParty = game.Party, Inventory = game.Bag, Pokedex = game.Pokedex, WildPokemon = new List<Pokemon> { foe },
            Difficulty = game.Run.Difficulty, RunLevelCap = cap
        });
        game.BattleRenderer.SetArena(game.Map, x, y);
        game.Battle = b;
        game.State = GameState.Battle;
        ToMainMenu(b);
        Shot("wy_level_cap_menu");
        for (int i = 0; i < 60 && !b.CurrentMessage.Contains("can't grow past") && !b.IsBattleOver; i++)
        {
            if (b.HUD.MenuState == BattleMenuState.Main) b.SelectMove(0);
            else Confirm(b);
            Skip(0.4);
        }
        Skip(0.5);
        Shot("wy_level_cap_line");
        Console.WriteLine($"level cap: \"{b.CurrentMessage}\"; the Turtwig is level {turtwig.Level} with {turtwig.CurrentExp} EXP, the start of 14 is {Pokemon.GetExpForLevel(14, turtwig.Species.GrowthRate)}");
        for (int i = 0; i < 40 && !b.IsBattleOver; i++) { Confirm(b); Skip(0.3); }
        game.State = GameState.Overworld;
    }

    // Someone who carries a trainer of Platinum's data, as a script's battle makes them
    static NPC Carrying(string id)
    {
        var record = TrainerDatabase.Get(id) ?? throw new InvalidOperationException($"No trainer {id}");
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        return new NPC { Name = trainer.Name, NpcType = "Trainer", TrainerData = trainer };
    }

    // wy_roark_normal and wy_roark_challenge: Roark in his Gym as the game begins his battle, once under Normal and once
    // under Challenge (V2's table: four levels more, a fourth Pokémon from the team he has for a rematch, a held item
    // for his best and for the new one). A Turtwig of level 14 leads, so the shots are of the same battle but for the run
    void RoarkMode()
    {
        foreach (var (shot, run) in new[] { ("wy_roark_normal", RunRules.Normal), ("wy_roark_challenge", new RunRules { Difficulty = Difficulty.Challenge }) })
        {
            BeginRun(run);
            game.Party.Members[0].Relevel(14);
            var hall = MapDatabase.Get("OreburghGym");
            var person = hall.Everyone.First(n => n.Name.Equals("Roark", StringComparison.OrdinalIgnoreCase));
            At("OreburghGym", person.GridX, person.GridY + 1, Direction.Up);
            Frames(30);
            var met = Carrying("leader_roark");
            met.NpcType = person.NpcType;
            game.StartTrainerBattle(met);
            for (int i = 0; i < 900 && game.State != GameState.Battle; i += 2) Frames(2);
            var b = game.Battle;
            var team = b.Trainers[0].Party.Members.Select(p => $"{p.Species.Name} {p.Level}{(p.HeldItem != null ? " holding " + p.HeldItem.Name : "")}");
            Console.WriteLine($"{shot}: {run.Difficulty}: {string.Join(", ", team)}; prize money {b.Trainers[0].PrizeMoney}");
            ToMainMenu(b);
            Shot(shot);
            game.State = GameState.Overworld;
            Frames(10);
        }
    }
}
