partial class Harness
{
    // Auto-battle (plan 12 · Q12), last in `battle` so no shot before it moves: a wild battle of its own team and chance
    // with the options' AUTO-BATTLE row on, the cursor on AUTO (23e_battle_auto_button), AUTO at work on a line and at
    // the next turn's line (23e_battle_auto_running, 23e_battle_auto_next_turn), and the commands again once a faint on
    // the player's side has stopped it (23e_battle_auto_stopped); then the row is put back
    void AutoShots()
    {
        Pokemon Own(string species, int level, int seed, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level, new Random(seed));
            p.Moves.Clear();
            foreach (var move in moves) p.Moves.Add(new Move(MoveDatabase.Get(move)));
            return p;
        }
        var team = new Party();
        team.Add(Own("Bidoof", 16, 51, "Growl", "Tackle"));
        team.Add(Own("Luxray", 40, 52, "Spark", "Crunch"));
        game.Map = MapDatabase.Get("Sinnoh");
        var e = new BattleEngine(new BattleSetup
        {
            PlayerParty = team, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Own("Machoke", 16, 53, "Karate Chop") },
            Random = new PokemonPlatinumEngine.Battle.Sim.BattleRandom(17)
        });
        game.BattleRenderer.SetArena(game.Map);
        game.Battle = e;
        game.State = GameState.Battle;
        engine.Settings.AutoBattle = true;
        try
        {
            ToMainMenu(e);
            e.MoveCursor(-1, 0);
            Frames(2); Shot("23e_battle_auto_button");
            Console.WriteLine($"  auto: offered {e.AutoOffered}, cursor {e.HUD.MainMenuIndex}, would use {e.AutoCommandNow()}");

            e.AutoUntilStopped();
            Skip(0.5); Shot("23e_battle_auto_running");
            Console.WriteLine($"  auto: running {e.AutoRunning}, line \"{e.CurrentMessage}\"");
            int answers = e.Core.Record!.Answers.Count;
            for (int guard = 0; guard < 400 && e.AutoRunning && e.Core.Record!.Answers.Count == answers; guard++) Skip(0.1);
            Skip(0.3); Shot("23e_battle_auto_next_turn");
            Console.WriteLine($"  auto: running {e.AutoRunning}, answers {e.Core.Record!.Answers.Count}, line \"{e.CurrentMessage}\"");

            for (int guard = 0; guard < 600 && e.AutoRunning && !e.IsBattleOver; guard++) Skip(0.1);
            for (int guard = 0; guard < 12 && e.HUD.MenuState == BattleMenuState.Message && !e.IsBattleOver; guard++) { Confirm(e); Skip(30 / 60.0); }
            Skip(0.3); Shot("23e_battle_auto_stopped");
            Console.WriteLine($"  auto: stopped for {e.AutoStoppedFor}, menu {e.HUD.MenuState}, answers {string.Join(" ", e.Core.Record!.Answers.SelectMany(a => a).Select(c => c.Kind))}");
        }
        finally
        {
            engine.Settings.AutoBattle = false;
        }
    }
}
