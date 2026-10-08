partial class Harness
{
    // ---------------------------------------------------------------- the real encounter flow
    public void FlowMode()
    {
        // Grass encounter -> fade -> battle -> run -> fade back to the field (in grass no trainer is watching)
        GoTo("Route201", 24, 14, Direction.Up);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 3 });
        Frames(12); Shot("80_flow_fade_out");
        Frames(36); Shot("81_flow_fade_in");
        Frames(60); Shot("82_flow_battle");
        var fb = game.Battle;
        Confirm(fb); Frames(40);
        Confirm(fb); Frames(2);
        fb.SelectMainMenuOption(3); Frames(2); Shot("83_flow_run");
        Confirm(fb);
        Frames(14); Shot("84_flow_leave");
        Frames(60); Shot("85_flow_back");
        Console.WriteLine("state: " + game.State);
    }
}
