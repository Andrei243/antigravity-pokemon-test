using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// The Pokéathlon's last part (plan 15: the bonuses, the halls' honours, Whitney), at the very end of the
    /// <c>athlon</c> mode: the Speed Course from the clerk, each event's points with the bonuses won under its bars
    /// (<c>at17a</c> to <c>at17c</c>) and the scoreboard with its BONUS column (<c>at17d_speed_scoreboard</c>); then, with
    /// the records made to hold every honour, the trust hall's shoes, the potential hall's ball and the last hall's
    /// statue read (<c>at18a</c> to <c>at18c</c>); and the lobby on a Saturday with Whitney in it (<c>at19a</c>, <c>at19b</c>).
    /// It prints each event's bonuses and the honours held.
    /// </summary>
    private void AthlonHonours(AthlonScreen screen, System.Action<System.Func<bool>, string, int> until, System.Action<string, string> readOn)
    {
        // ---- The Speed Course: Hurdle Dash, Pennant Capture, Relay Run, each event's points page shot with its bonuses
        At("PokeathlonDome_1F", 23, 10, Direction.Up);
        Frames(10);
        game.Interact();
        Frames(3);
        readOn(null, null);
        until(() => game.State == GameState.Athlon, "the Pokéathlon's screen", 300);
        Frames(10);
        int index = AthlonCourses.All.ToList().FindIndex(c => c.Key == "speed");
        for (int i = 0; i < index; i++) screen.Move(0, 1);
        Frames(10);
        screen.Confirm();
        Frames(10);
        screen.Confirm();          // Quilava
        screen.Move(0, 1);
        screen.Confirm();          // Pikachu
        screen.Move(0, 1);
        screen.Move(0, 1);
        screen.Confirm();          // Machop
        Frames(20);
        string[] shots = { "at17a_points_bonus_hurdle_dash", "at17b_points_bonus_pennant_capture", "at17c_points_bonus_relay_run" };
        for (int e = 0; e < shots.Length; e++)
        {
            screen.Confirm();
            Frames(40);
            screen.Play(int.MaxValue, ev => ev.Autopilot());
            Frames(40);
            Shot(shots[e]);
            int done = screen.Run!.EventIndex - 1;
            Console.WriteLine($"  {screen.Run.Course.Events[done]}: " + string.Join(", ", Enumerable.Range(0, AthlonRun.TeamCount).Select(t =>
                $"{(t == 0 ? "player" : screen.Run.Teams[t].Name)} {screen.Run.PointsOf(done, t)}"
                + string.Concat(screen.Run.BonusesOf(done, t).Select(b => $" +{b.Kind}")))));
            screen.Confirm();
            Frames(20);
        }
        Shot("at17d_speed_scoreboard");
        Console.WriteLine($"  speed: earned {screen.Run!.Earned} (bonuses {screen.Run.BonusTotal(0)}), place {screen.Outcome}, medal {screen.Medal}; "
            + $"Steady {game.Athlon.SteadyBonuses}, Star {game.Athlon.StarBonuses}");
        screen.Confirm();
        until(() => game.State is GameState.Overworld or GameState.Dialogue, "the field again", 120);
        readOn(null, null);

        // ---- The halls' honours, the records made to hold them all
        var records = game.Athlon;
        records.SteadyBonuses = System.Math.Max(records.SteadyBonuses, AthlonRecords.ForShoes);
        records.StarBonuses = System.Math.Max(records.StarBonuses, AthlonRecords.ForClothes);
        records.Wins = System.Math.Max(records.Wins, AthlonRecords.ForFlag);
        foreach (var c in AthlonData.Courses) records.Award(c.Key, AthlonMedal.Gold);
        Console.WriteLine("  honours: " + string.Join(", ", System.Enum.GetValues<AthlonHonour>().Select(h => $"{h} {records.Has(h)}")));
        void Read(string map, int x, int y, string word, string shot)
        {
            At(map, x, y, Direction.Up);
            Frames(20);
            game.Interact();
            Frames(3);
            readOn(word, shot);
        }
        Read("PokeathlonDome_B1F_Trust", 13, 4, "running shoes", "at18a_trust_room_shoes");
        Read("PokeathlonDome_B1F_Potential", 10, 4, "silver Poké Ball", "at18b_potential_room_ball");
        Read("PokeathlonDome_B1F_Friendship", 7, 9, "statue", "at18c_friendship_room_statue");

        // ---- Whitney in the lobby on a Saturday, once her Badge is won
        var day = GameClock.FixedDate;
        GameClock.FixedDate = new System.DateTime(2026, 6, 6);
        game.Story.Set("FLAG_DEFEATED_WHITNEY");
        At("PokeathlonDome_1F", 23, 24, Direction.Up);
        game.ArriveOnMap();
        Frames(30);
        var whitney = game.Map.NPCs.FirstOrDefault(n => n.Script == "Whitney");
        Console.WriteLine($"  Whitney on Saturday: {(whitney == null ? "not in" : $"in at {whitney.GridX}, {whitney.GridY}")}");
        Shot("at19a_lobby_whitney");
        if (whitney != null) Read("PokeathlonDome_1F", whitney.GridX, whitney.GridY + 1, "Saturdays", "at19b_whitney_talks");
        GameClock.FixedDate = day;
    }
}
