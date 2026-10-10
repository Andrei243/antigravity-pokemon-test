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
            if (view.TakeInHand(poketch, context))
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
