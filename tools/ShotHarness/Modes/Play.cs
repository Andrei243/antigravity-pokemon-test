using PokemonPlatinumEngine.Core.Autoplay;

partial class Harness
{
    // ---------------------------------------------------------------- the playthrough bot (plan 16 · T7)

    // The playthrough bot plays the game for some minutes of game time with the harness's seed: from the bedroom of a
    // new game, as a player begins (plan 02 · S4), or with --past-opening in front of the house as the other modes
    // begin. A shot at each place it comes to the first time (play_<place>), and play.md in the out dir: the places, the
    // people, items, triggers and ways it went for, the scripts that ran, the battles, and every finding (an exception,
    // a softlock, a script gone wrong, a value out of range, a goal dropped). With --keys it is the key fuzzer instead:
    // seeded actions and nothing decided; with --field the field fuzzer (plan 16 · T13); with --badge the bot follows
    // the story's way to the Coal Badge. `play [minutes] [seed] [--keys|--field] [--badge] [--past-opening]`; not part of "all".
    public void PlayMode()
    {
        var words = args.Skip(2).ToList();
        bool keys = words.Remove("--keys");
        bool pastOpening = words.Remove("--past-opening");
        bool badge = words.Remove("--badge");
        bool field = words.Remove("--field");
        double minutes = words.Count > 0 && double.TryParse(words[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double m) ? m : 10;
        int seed = words.Count > 1 && int.TryParse(words[1], out int s) ? s : 1;

        if (!pastOpening)
        {
            // A game of its own, begun where a player begins it
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            game.SaveSlot = 1;
            game.State = GameState.Overworld;
        }
        game.LocationSign.Hide();
        // People about their day, as a player meets them: the bot plans again at every tile
        engine.PeopleStayPut = false;

        // --badge: on the story's way to the Coal Badge (plan 16 · T13); --field: the field fuzzer's random walk instead
        var bot = field ? new Autoplayer(engine, new FieldFuzzer(seed)) : new Autoplayer(engine, seed, keys);
        if (badge) bot.Itinerary = new Itinerary(Itineraries.TwinleafToCoalBadge);
        int shots = 0;
        // A place is noted only once the field is the player's again, so the shot is never of a fade
        bot.FirstArrival = place => Shot($"play_{shots++:00}_{place}");
        Console.WriteLine($"play: {minutes:0.#} minutes of game time, seed {seed}, {(keys ? "the key fuzzer" : "the bot")}, from {(pastOpening ? "in front of the house" : "the bedroom")}");
        long frames = (long)(minutes * 60 * 60);
        for (long f = 0; f < frames && !bot.Stopped && !engine.QuitRequested; f++)
        {
            FrameClock.Fixed = ++tick / 60.0;
            bot.Frame(1f / 60f);
            if (f % 6 == 5) engine.Draw();
            if (f % (60 * 60) == 0) Console.WriteLine($"  {f / 3600} min: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}, {game.State}, goal {bot.Goal?.Key ?? "none"}");
        }
        bot.Release();

        var report = bot.Report;
        File.WriteAllText(Path.Combine(outDir, "play.md"), report.ToMarkdown());
        Console.WriteLine($"play: {report.Seconds:0} s, {report.Tiles} tiles, {report.Maps.Count} maps, {report.Places.Count} places, {report.Spoken.Count} spoken to, {report.Items.Count} items, {report.Warps.Count} ways, {report.Battles} battles");
        foreach (string finding in report.Exceptions.Concat(report.Softlocks).Concat(report.OutOfRange).Concat(report.ScriptsGoneWrong))
            Console.WriteLine("!! " + finding.Split('\n')[0]);
        Console.WriteLine($"wrote play.md ({(report.Clean ? "nothing found" : "findings above")})");
    }
}
