partial class Harness
{
    // V14, the daily run: wy_daily_room (the battle room on its DAILY RUN row, a day already played in the records),
    // wy_daily_rentals (the day's six on the Battle Factory's rental screen, opened from the room as DAILY RUN does) and
    // wy_daily_between (the room's panel after a first win, its TRADE row offered). The day is the harness's fixed date.
    // It prints the day, its round, its seed and its seven trainers, which two runs of the harness print the same
    void DailyRunMode()
    {
        var today = DailyRun.Today();
        var records = new DailyRecords();
        records.Days.Add(new DailyResult { Date = DailyRun.KeyOf(today.AddDays(-1)), Wins = 7, Over = true });
        records.Days.Add(new DailyResult { Date = DailyRun.KeyOf(today.AddDays(-2)), Wins = 4, Over = true });

        game.OpenBattleRoom();
        game.BattleRoom.Open(Array.Empty<BuiltTeam>(), Puzzles.All, new BattleRoomRecords(), records, today);
        while (BattleRoomScreen.Entries[game.BattleRoom.Cursor] != RoomEntry.Daily) game.BattleRoom.Move(1);
        Frames(30);
        Shot("wy_daily_room");

        var daily = DailyRun.For(today, counts: false);
        Console.WriteLine($"daily run: {DailyRun.KeyOf(today)}, round {daily.Factory.Round + 1}, seed {daily.Seed:X8}");
        Console.WriteLine($"daily run: offered {string.Join(", ", daily.Offer.Select(r => r.Species))}");
        Console.WriteLine($"daily run: trainers {string.Join(", ", daily.Trainers.Select(t => FrontierDatabase.Trainer(t).Name))}");
        game.BattleRoom.Close();
        game.BeginDailyRun(daily);
        // The six's pictures are asked for as the screen opens; give them time to bake
        Frames(90);
        Shot("wy_daily_rentals");
        game.FactoryRentals.Cancel();
        Frames(10);

        // A first win, as the room shows it after the battle
        daily.Rent(new[] { 0, 1, 2 });
        daily.Ready();
        daily.Won();
        game.State = GameState.BattleRoom;
        game.BattleRoom.AfterDailyBattle(daily, false);
        game.BattleRoom.Move(1);
        Frames(30);
        Shot("wy_daily_between");
        game.BattleRoom.Close();
    }
}
