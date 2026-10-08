partial class Harness
{
    // ---------------------------------------------------------------- evolution
    public void EvolutionMode()
    {
        var scene = game.Evolution;
        var before = party.Members.ToList();
        EvolutionContext Now() => new() { Party = party, Bag = inventory };
        Pokemon Fresh(string species, int level) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);
        void Until(Func<bool> done, int limit = 1500) { for (int i = 0; i < limit && !done(); i++) Frames(1); }
        void Dismiss() { for (int i = 0; i < 8 && scene.IsActive; i++) { scene.PressConfirm(); Frames(2); } }

        // A level-up evolution from the first message to the last: Turtwig into Grotle
        var turtwig = Fresh("Turtwig", 18);
        game.State = GameState.Evolution;
        scene.Begin(turtwig, Evolution.Find(turtwig, EvolutionTrigger.LevelUp, Now())!, Now(), cancellable: true);
        Frames(40); Shot("e01_notice");
        Until(() => scene.Phase == EvolutionPhase.Gather); Frames(60); Shot("e02_gather");
        Until(() => scene.Phase == EvolutionPhase.Morph);
        Until(() => scene.Look().OldScale > 0.8f); Shot("e03_morph_old");
        Until(() => scene.Look().NewScale > 0.4f); Shot("e04_morph_new");
        Timing("evolution scene", 60);
        Until(() => scene.Look().NewScale > 0.8f || scene.Phase != EvolutionPhase.Morph); Shot("e05_morph_late");
        Until(() => scene.Phase == EvolutionPhase.Burst); Frames(10); Shot("e06_burst");
        Until(() => scene.Phase == EvolutionPhase.Reveal); Frames(22); Shot("e07_reveal");
        Until(() => scene.Phase == EvolutionPhase.Congratulate); Frames(70); Shot("e08_congratulations");
        Dismiss();
        Console.WriteLine($"evolved into {turtwig.Species.Name}; scene active: {scene.IsActive}");

        // Four moves and a fifth on offer: Chimchar into Monferno, which learns Mach Punch at that level
        var chimchar = Fresh("Chimchar", 14);
        Frames(60);
        game.State = GameState.Evolution;
        scene.Begin(chimchar, Evolution.Find(chimchar, EvolutionTrigger.LevelUp, Now())!, Now(), cancellable: true);
        Until(() => scene.Phase == EvolutionPhase.Congratulate);
        scene.PressConfirm();
        Frames(40); Shot("e09_move_choice");
        scene.MoveCursor(-1);
        Frames(4); Shot("e09b_move_choice_keep");
        scene.MoveCursor(3);
        scene.PressConfirm();
        Frames(50); Shot("e10_move_learned");
        Dismiss();
        Console.WriteLine($"{chimchar.Species.Name} knows {string.Join(", ", chimchar.Moves.Select(m => m.Name))}");

        // Stopped with B
        var starly = Fresh("Starly", 14);
        Frames(60);
        game.State = GameState.Evolution;
        scene.Begin(starly, Evolution.Find(starly, EvolutionTrigger.LevelUp, Now())!, Now(), cancellable: true);
        Until(() => scene.Phase == EvolutionPhase.Morph); Frames(50);
        scene.PressCancel();
        Frames(50); Shot("e11_stopped");
        Dismiss();
        Console.WriteLine($"still a {starly.Species.Name}");

        // A stone from the bag: who it works on, then the scene through the game's own states and back to the bag
        var eevee = Fresh("Eevee", 12);
        party.Add(eevee);
        inventory.AddItem(ItemDatabase.Get("Fire Stone")!, 1);
        var bag = game.BagScreen;
        game.State = GameState.BagMenu;
        bag.Open();
        bag.BeginTargetChoice(ItemDatabase.Get("Fire Stone")!);
        Frames(40); Shot("e12_bag_choice");
        bag.MoveTarget(1, 0, party.Count);
        bag.MoveTarget(0, 1, party.Count);
        Frames(4); Shot("e12b_bag_choice_able");
        bag.UseOnTarget(inventory, party, engine.ShowNotification, Now());
        Frames(12); Shot("e13_fade_to_scene");
        Until(() => scene.Phase == EvolutionPhase.Morph && game.State == GameState.Evolution);
        Until(() => scene.Look().NewScale > 0.5f); Shot("e14_stone_morph");
        Until(() => scene.Phase == EvolutionPhase.Congratulate); Frames(60); Shot("e15_stone_evolved");
        Dismiss();
        Frames(70);
        Console.WriteLine($"after the stone: {eevee.Species.Name}, state {game.State}, stones left {inventory.GetQuantity(ItemDatabase.Get("Fire Stone")!)}");
        bag.Close();

        // A trade: the Pokémon given leaves, the one received arrives and evolves, and the field comes back
        GoTo("TwinleafTown", 11, 8, Direction.Down);
        var kadabra = Fresh("Kadabra", 30);
        engine.ReceiveTradedPokemon(kadabra, eevee);
        Until(() => game.State == GameState.Evolution);
        Until(() => scene.Phase == EvolutionPhase.Congratulate); Frames(60); Shot("e16_trade_evolved");
        Dismiss();
        Frames(70);
        Console.WriteLine($"after the trade: {string.Join(", ", party.Members.Select(m => m.Species.Name))}, state {game.State}, " +
                          $"Alakazam caught: {pokedex.IsCaught(PokemonDatabase.Get("Alakazam")!.DexNumber)}");

        // A battle won: the Pokémon that grew in it evolves once it is over, before the field comes back
        var grower = Fresh("Starly", 13);
        grower.CurrentExp = grower.ExpForNextLevel - 1;
        party.Members[0] = grower;
        GoTo("Route201", 24, 14, Direction.Up);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 2, MaxLevel = 2 });
        Frames(60);
        var won = game.Battle;
        for (int i = 0; i < 4000 && game.State != GameState.Evolution; i++)
        {
            if (game.State == GameState.Battle && !won.IsBattleOver)
            {
                if (won.IsWaitingForConfirm) won.ConfirmMessage();
                else if (won.HUD.MenuState != BattleMenuState.Message) won.SelectMove(0);
            }
            Frames(1);
        }
        Console.WriteLine($"after the battle: {grower.Species.Name} Lv {grower.Level}, state {game.State}");
        Frames(30); Shot("e17_after_battle");
        Until(() => scene.Phase == EvolutionPhase.Congratulate);
        Dismiss();
        Frames(70);
        Console.WriteLine($"after its evolution: {grower.Species.Name}, state {game.State}");

        party.Clear();
        foreach (var member in before) party.Add(member);
        game.State = GameState.Overworld;
    }
}
