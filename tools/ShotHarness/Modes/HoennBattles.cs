partial class Harness
{
    /// <summary>
    /// Plays the battle on screen to its end and reads the script that began it on to its end (plan 21 · H12, part h):
    /// the post-game's modes used to put the state back to the field with the battle still on, which left its script
    /// waiting for an outcome that never came and its text box over every later shot (Deoxys's and Wattson's in part f,
    /// Mew's and Roxanne's in part g). The foes are left a single HP and only Struggle, the player's own kept whole, as
    /// the other modes win theirs.
    /// </summary>
    void WinAndReadOut(string what)
    {
        var fight = game.Battle;
        for (int guard = 0; guard < 400 && game.State == GameState.Battle && fight != null && !fight.IsBattleOver; guard++)
        {
            if (fight.HUD.MenuState == BattleMenuState.Main)
            {
                var foes = (fight.EnemyParty?.Members ?? new List<Pokemon>()).Concat(fight.EnemySlots.Select(s => s.Pokemon));
                if (fight.EnemyPokemon is { } met) foes = foes.Append(met);
                foreach (var foe in foes)
                    if (foe != null)
                    {
                        foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                        foreach (var move in foe.Moves) move.CurrentPP = 0;
                    }
                foreach (var trainer in fight.Trainers) trainer.Items.Clear();
                var lead = fight.PlayerPokemon;
                foreach (var mine in fight.PlayerSlots.Select(s => s.Pokemon).Append(lead))
                    if (mine != null && !mine.IsFainted)
                    {
                        mine.CurrentHP = mine.MaxHP;
                        foreach (var move in mine.Moves) move.CurrentPP = move.MaxPP;
                    }
                fight.SelectMainMenuOption(0);
                int hurts = lead == null ? 0 : Math.Max(0, lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status));
                fight.SelectMove(hurts);
                if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
            }
            else Confirm(fight);
            Skip(0.5);
        }
        for (int i = 0; i < 1500 && !FieldAgain(); i++) Frames(1);
        if (!FieldAgain()) Console.WriteLine($"  !! never came back to the field after {what}");
        // what the script says after the battle, to its end
        var box = game.Dialogue;
        for (int i = 0; i < 1800 && (engine.ScriptRunning || box.IsActive); i++)
        {
            if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
            else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
            else Frames(1);
        }
        if (engine.ScriptRunning) Console.WriteLine($"  !! the script is still running after {what}");
        Frames(6);
    }
}
