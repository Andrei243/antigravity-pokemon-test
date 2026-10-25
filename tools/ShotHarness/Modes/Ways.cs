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
