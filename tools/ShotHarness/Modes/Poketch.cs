using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- the Pokétch's apps (plan 06 · R14b)

    // Every app the game runs, one after another over Route 201 (pk*; not part of "all"): each as it comes up, and
    // each taken in hand and touched (its first button, then the one to the right of it), close up at the screen's
    // own resolution, and once the whole field with the Pokétch in hand. It prints what each app's state says.
    public void PoketchMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var poketch = game.Poketch;
        var view = game.PoketchView;
        poketch.Enabled = true;
        foreach (var app in System.Enum.GetValues<PoketchApp>()) poketch.Register(app);
        foreach (string s in new[] { "Starly", "Bidoof", "Shinx", "Budew", "Psyduck", "Machop" })
        {
            var p = new Pokemon(PokemonDatabase.Get(s)!, 8);
            poketch.Remember(p);
            if (game.Party.Count < 4) game.Party.Add(p);
        }
        for (int i = 0; i < 1234; i++) poketch.Step();
        At("Sinnoh", 116, 888, Direction.Down);
        Frames(4);

        // The watch's body stands 32 from the right and bottom edges: 456 by 368, taken with a margin
        void Close(string name) => ShotCrop(name, 1920 - 32 - 456 - 12, 1080 - 32 - 368 - 12, 456 + 24, 368 + 24, 2);

        view.Toggle(poketch);
        Frames(20);
        int n = 0;
        foreach (var app in poketch.Shown)
        {
            n++;
            string key = $"pk{n:D2}_{app}";
            var context = game.PoketchContext;
            Frames(4);
            Close(key);
            if (Plays.TryGetValue(app, out var play))
            {
                // An app whose touches tell more than a first touch: played through its own buttons
                view.TakeInHand(poketch, context);
                play(this, poketch, context);
                Close(key + "_touched");
                view.LetGo();
            }
            else if (view.TakeInHand(poketch, context))
            {
                view.Touch(poketch, context);
                Frames(6);
                view.MoveCursor(poketch, context, 1, 0);
                view.Touch(poketch, context);
                Frames(20);
                Close(key + "_touched");
                if (n == 1)
                {
                    Shot("pk00_field_in_hand");
                }
                view.LetGo();
            }
            System.Console.WriteLine($"poketch: {app}: {poketch.State?.GetType().Name}, buttons {poketch.State?.Buttons(context).Count ?? 0}");
            view.NextApp(poketch);
        }
        view.Toggle(poketch);
        Frames(20);
    }
}

partial class Harness
{
    private static void Press(Harness h, Poketch poketch, PoketchContext context, params int[] buttons)
    {
        foreach (int b in buttons)
        {
            poketch.State!.Press(b, context);
            h.Frames(3);
        }
    }

    // What each app is shown doing when a first touch says too little
    private static readonly System.Collections.Generic.Dictionary<PoketchApp, System.Action<Harness, Poketch, PoketchContext>> Plays = new()
    {
        // 12 + 34 =
        [PoketchApp.Calculator] = (h, p, c) => Press(h, p, c, 1, 2, CalculatorApp.Plus, 3, 4, CalculatorApp.EqualsKey),
        [PoketchApp.Counter] = (h, p, c) => Press(h, p, c, 0, 0, 0, 0, 0, 0, 0),
        // The coin in the air, a third of a second after the throw
        [PoketchApp.CoinToss] = (h, p, c) => { Press(h, p, c, 0); h.Frames(18); },
        // The arrow at speed
        [PoketchApp.Roulette] = (h, p, c) => { Press(h, p, c, RouletteApp.Start); h.Frames(60); },
        // Set to 01:30 and started
        [PoketchApp.KitchenTimer] = (h, p, c) => Press(h, p, c, KitchenTimerApp.MinutesOnesUp, KitchenTimerApp.SecondsTensUp,
            KitchenTimerApp.SecondsTensUp, KitchenTimerApp.SecondsTensUp, KitchenTimerApp.Start),
        [PoketchApp.MoveTester] = (h, p, c) => Press(h, p, c, MoveTesterApp.AttackUp, MoveTesterApp.AttackUp, MoveTesterApp.FirstUp, MoveTesterApp.FirstUp, MoveTesterApp.FirstUp, MoveTesterApp.FirstUp),
        [PoketchApp.MatchupChecker] = (h, p, c) => { Press(h, p, c, MatchupCheckerApp.Check); h.Frames(90); },
        [PoketchApp.ColorChanger] = (h, p, c) => { Press(h, p, c, 4); h.Frames(6); },
        // The player walked up near a hidden item, and its tile touched
        [PoketchApp.DowsingMachine] = (h, p, c) =>
        {
            var map = h.game.Map;
            var (x, y) = (h.game.Player.GridX, h.game.Player.GridY);
            var near = map.HiddenItems.Keys
                .Where(t => map.AreaAt(t.X, t.Y) == map.AreaAt(t.X - 2, t.Y) && map.IsWalkable(t.X - 2, t.Y))
                .OrderBy(t => System.Math.Abs(t.X - x) + System.Math.Abs(t.Y - y)).FirstOrDefault((-1, -1));
            if (near.Item1 < 0) { System.Console.WriteLine("poketch: no hidden item near to dowse"); return; }
            h.game.Place(map, near.Item1 - 2, near.Item2, Direction.Right);
            h.Frames(4);
            Press(h, p, h.game.PoketchContext, DowsingMachineApp.TileId(2, 0));
            h.Frames(40);
            System.Console.WriteLine($"poketch: dowsing from ({near.Item1 - 2}, {near.Item2}): {((DowsingMachineApp)p.State!).ShowingItems}");
        },
    };
}
