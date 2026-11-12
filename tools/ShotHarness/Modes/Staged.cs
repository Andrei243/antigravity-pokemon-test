partial class Harness
{
    // Staged battles (docs/mechanics/rulings.md, "Staged battles"; not part of `all`; sg*): a script's
    // `wildbattle … hidelevel untouchable passive endafter 3` through the game's own StartScript, the foe's box with its
    // level drawn as ??? at the menu (sg01, with a close crop sg01b), the move's own line, the line "It can't be touched!"
    // (sg02), the line "It only watches..." (sg03) and the end as it leaves (sg04). It prints each line, the foe's HP as
    // the rules have it, the result and what the script was told.
    public void StagedMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();

        engine.StartScript(ScriptParser.Parse("twinleaf_town", "script Staged\n"
            + "  wildbattle \"Zacian\" 70 hidelevel untouchable passive endafter 3 canlose\n"
            + "  setvar VAR_STAGED_RESULT RESULT\n  end\n")[0]);
        for (int i = 0; i < 1200 && game.State != GameState.Battle; i++) Frames(1);
        var battle = game.State == GameState.Battle ? game.Battle : null;
        if (battle == null) { Console.WriteLine("  !! never happened: the battle"); return; }
        ToMainMenu(battle);
        Console.WriteLine($"staged: hides the level {battle.HidesLevel(BattleSide.Enemy)}, shown {battle.ShownLevel(battle.EnemySlots[0])?.ToString() ?? "???"}, rules' level {battle.EnemySlots[0].Pokemon!.Level}");
        Shot("sg01_staged_hidden_level");
        ShotCrop("sg01b_staged_hidden_level_close", 0, 0, 900, 260, 2);

        int foeHp = battle.Core.EnemySlots[0].Pokemon!.CurrentHP;
        battle.SelectMainMenuOption(0);
        Frames(10);
        battle.SelectMove(0);
        bool touched = false, watched = false;
        for (int i = 0; i < 80 && !battle.IsBattleOver; i++)
        {
            Skip(0.25);
            string line = battle.CurrentMessage ?? "";
            if (line.Length > 0) Console.WriteLine("  line: " + line);
            if (!touched && line == Text.Of("battle.cantBeTouched")) { touched = true; Skip(0.3); Shot("sg02_staged_cant_be_touched"); }
            if (!watched && line == Text.Of("battle.onlyWatches")) { watched = true; Skip(0.3); Shot("sg03_staged_only_watches"); }
            if (line.Contains("turned away")) { Skip(0.3); Shot("sg04_staged_leaves"); }
            if (battle.HUD.MenuState == BattleMenuState.Message) Confirm(battle);
            else if (battle.HUD.MenuState == BattleMenuState.Main) { battle.SelectMainMenuOption(0); Frames(10); battle.SelectMove(0); }
        }
        Console.WriteLine($"the foe's HP {foeHp} -> {battle.Core.EnemySlots[0].Pokemon!.CurrentHP}; result {battle.Result}; seen in the Pokédex {game.Pokedex.IsSeen(PokemonDatabase.Get("Zacian")!.DexNumber)}");
        for (int i = 0; i < 600 && game.State != GameState.Overworld; i++) Frames(1);
        Frames(30);
        Console.WriteLine($"the script was told: RESULT {game.Story.Var("VAR_STAGED_RESULT")}");
    }
}
