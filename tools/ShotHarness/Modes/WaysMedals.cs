partial class Harness
{
    // V9, medals: wy_medal_notice, the notice of the first catch's medal over the field with the options' MEDAL NOTICES
    // row on, a third of a second in, once it has dropped into place. It prints what was won and whether the medal is in
    // the save, and puts the row back as it was
    void MedalsMode()
    {
        bool before = engine.Settings.MedalNotices;
        engine.Settings.MedalNotices = true;
        BeginRun(RunRules.Normal);
        Frames(4);
        game.Statistics.CountBattle(BattleResult.EnemyCaught, trainer: false, caughtNewSpecies: true);
        Frames(1);
        var notice = game.MedalNotice;
        Console.WriteLine($"medals: won {string.Join(", ", game.Medals.ToSaved() ?? new List<string>())}; on show {notice.Medal?.Id ?? "none"}, {notice.Waiting} waiting; in the save: {game.BuildSave().Medals?.Count ?? 0}");
        Frames(24);
        Shot("wy_medal_notice");
        Skip(4);
        Console.WriteLine($"medals: the notice after four seconds is {(notice.Visible ? "still up" : "gone")}");
        engine.Settings.MedalNotices = before;
    }
}
