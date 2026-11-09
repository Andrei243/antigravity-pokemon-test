using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// The wave after the Battle Tree (the <c>alolalast</c> mode's <c>wt20</c> on; plan 23): the reef's islet and its current
    /// by day and after dark (<c>wt20</c>, <c>wt21</c>), a Lapras carried back (<c>wt22</c>, <c>wt23</c>) and a Sharpedo
    /// through (<c>wt24</c>, <c>wt25</c>), Mantine on the swell and the beach's four gates (<c>wt26</c>, <c>wt27</c>), the old
    /// woman and her Machamp (<c>wt30</c>, <c>wt31</c>), a cell of the day and one of the night each by day and after dark
    /// (<c>wt32</c>) and the Battle Tree's Multi room with its partner and the pair of trainers (<c>wt40</c>, <c>wt41</c>).
    /// </summary>
    private void AlolaOddsAndEnds(StoryState story)
    {
        story.Set("FLAG_ALOLA_RIDE_LAPRAS");
        story.Set(RidePager.SharpedoFlag);
        story.Set(RidePager.MantineFlag);

        void Water(string map, int x, int y, Direction facing, string? jet)
        {
            game.Player.SetJet(null);
            game.Player.SetMode(TravelMode.OnFoot);
            At(map, x, y, facing);
            game.Player.SetMode(TravelMode.Surfing);
            game.Player.Carrier = "Lapras";
            if (jet != null) game.Player.SetJet(jet);
            Frames(6); game.LocationSign.Hide();
        }
        void Report(string what) => Console.WriteLine($"  {what}: {game.Player.GridX},{game.Player.GridY} {game.Player.Mode} on {game.Player.Carrier} ({game.Player.Steed ?? "no jet"}), sliding {game.Player.IsSliding}");

        // ---- The reef's islet off the Secluded Shore, its one gap holding the current: by day, then after dark
        Water("Ulaula", 168, 344, Direction.Right, null); Shot("wt20_reef_islet_day"); Report("the islet by day");
        engine.Settings.TimeOfDay = TimeOfDay.Night; engine.ApplySettings(window: false);
        Frames(30); game.LocationSign.Hide(); Shot("wt21_reef_islet_night"); Report("the islet after dark");
        engine.Settings.TimeOfDay = TimeOfDay.Day; engine.ApplySettings(window: false);

        // ---- A Lapras steps onto the current and is carried straight back out
        Water("Ulaula", 175, 344, Direction.Right, null);
        engine.Steering = (Direction.Right, false);
        Frames(9); Shot("wt22_lapras_carried_back"); Report("Lapras on the current");
        engine.Steering = null;
        Frames(40); game.LocationSign.Hide(); Shot("wt23_lapras_turned_out"); Report("Lapras after");

        // ---- Sharpedo crosses it and lands on the islet
        Water("Ulaula", 175, 344, Direction.Right, "Sharpedo");
        engine.Steering = (Direction.Right, false);
        Frames(14); Shot("wt24_sharpedo_in_the_current"); Report("Sharpedo in the gap");
        Frames(40);
        engine.Steering = null;
        Frames(30); game.LocationSign.Hide(); Shot("wt25_sharpedo_on_the_islet"); Report("Sharpedo after");
        game.Player.SetJet(null); game.Player.SetMode(TravelMode.OnFoot);

        // ---- Mantine on the swell south of Big Wave Beach, and the four gates of the course
        Water("Melemele", 167, 168, Direction.Down, "Mantine"); Shot("wt26_mantine_on_the_water"); Report("Mantine on the swell");
        foreach (var (n, gx, gz) in new[] { (1, 160, 170), (2, 172, 173), (3, 162, 176), (4, 174, 178) })
        {
            Water("Melemele", gx + 1, gz - 2, Direction.Down, "Mantine");
            Shot($"wt27_mantine_gate_{n}");
            Report($"gate {n}");
        }
        game.Player.SetJet(null); game.Player.SetMode(TravelMode.OnFoot);

        // ---- The old woman and her Machamp by day
        NextTo("wt30_widow", "Melemele", n => n.Key == "widow");
        NextTo("wt31_widow_machamp", "Melemele", n => n.Key == "widow_machamp");

        // ---- A cell that shows by day and one that shows by night: each place by day and after dark
        story.Set(ClockFlags.CubeGiven);
        var melemele = MapDatabase.Get("Melemele");
        var dayCell = melemele.Everyone.First(n => n.NpcType == NPC.ZygardeCellType && n.ShownBy == ClockFlags.Daylight);
        var nightCell = melemele.Everyone.First(n => n.NpcType == NPC.ZygardeCellType && n.ShownBy == ClockFlags.Nightfall);
        foreach (var (hour, label) in new[] { (TimeOfDay.Day, "day"), (TimeOfDay.Night, "night") })
        {
            engine.Settings.TimeOfDay = hour; engine.ApplySettings(window: false);
            At("Melemele", 0, 0, Direction.Up); Frames(30);
            foreach (var (cell, name) in new[] { (dayCell, "daycell"), (nightCell, "nightcell") })
            {
                if (cell == nightCell) At("Melemele", cell.GridX + 2, cell.GridY, Direction.Left); else At("Melemele", cell.GridX - 2, cell.GridY, Direction.Right); Frames(8); game.LocationSign.Hide();
                Shot($"wt32_{name}_at_{label}");
                Console.WriteLine($"  {name} {cell.Key} at {cell.GridX},{cell.GridY} at {label}: {(game.Map.NPCs.Contains(cell) ? "shown" : "hidden")} (daylight {story.Has(ClockFlags.Daylight)}, nightfall {story.Has(ClockFlags.Nightfall)})");
            }
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day; engine.ApplySettings(window: false);
        Frames(10);

        // ---- The Battle Tree's Multi room: two entered, the partner beside the player, the pair of trainers coming down
        TreeMulti();
    }

    private void TreeMulti()
    {
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        string? battleShot = null;
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 900 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    if (battleShot != null) { Skip(0.5); Shot(battleShot); battleShot = null; }
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    foreach (var slot in fight.EnemySlots) if (slot.Pokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    foreach (var mine in fight.PlayerParty.Members) if (!mine.IsFainted) mine.CurrentHP = mine.MaxHP;
                    fight.SelectMainMenuOption(0);
                    if (lead != null) foreach (var m in lead.Moves) m.CurrentPP = m.MaxPP;
                    int hurts = lead == null ? 0 : lead.Moves.Select((m, i) => (m, i)).Where(p => p.m.Data.Category != MoveCategory.Status && p.m.Data.Type != PokemonType.Ghost)
                        .OrderByDescending(p => p.m.Data.Power).Select(p => p.i).DefaultIfEmpty(0).First();
                    fight.SelectMove(hurts);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        void EnterTeam()
        {
            var screen = game.PartyScreen;
            Frames(60);
            for (int slot = 0; slot < screen.EnterCount; slot++)
            {
                screen.SelectedIndex = slot; Frames(2); screen.Confirm(game.Party); Frames(6);
            }
            screen.Confirm(game.Party);
            Frames(4);
        }
        var answers = new Queue<int>();
        void ReadTo(string? text, string? shotWhenSaid = null, int most = 6000)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text))
                {
                    if (shotWhenSaid != null) { Frames(30); Box().FinishLine(); Frames(4); game.LocationSign.Hide(); Shot(shotWhenSaid); }
                    Box().FinishLine(); Frames(2); Console.WriteLine($"  said: {text}"); return;
                }
                idle = !engine.ScriptRunning && !Box().IsActive && game.State == GameState.Overworld ? idle + 1 : 0;
                if (text != null && idle > 90) break;
                if (text == null && idle > 30) return;
                if (game.State == GameState.Battle) { Win(); continue; }
                if (game.State == GameState.PartyMenu) { EnterTeam(); continue; }
                if (engine.Choice.IsOpen)
                {
                    Frames(10);
                    int answer = answers.Count > 0 ? answers.Dequeue() : 0;
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    Frames(4);
                    engine.Choice.Confirm(); Frames(4);
                }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }

        foreach (var (species, level) in new[] { ("Decidueye", 60), ("Incineroar", 60), ("Primarina", 60), ("Lycanroc", 60) })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        Frames(2);
        // Enter, Multi, Normal; two entered, and the hall's first battle at its menu
        answers = new Queue<int>(new[] { 0, 2, 0 });
        At("PoniGauntlet_BattleTree", 4, 5, Direction.Up); Frames(2); game.Interact();
        battleShot = "wt41_tree_multi_battle";
        ReadTo("step forward", "wt40_tree_multi_hall");
        Console.WriteLine($"  the challenge: {(game.Facility == null ? "none" : game.Facility.Room.Key)}, partner {game.Facility?.PartnerName ?? "none"}");
        ReadTo("Your Pokémon are whole again");
        answers = new Queue<int>(new[] { 2 });
        ReadTo(null);
        Console.WriteLine($"  after: map {game.Map.Name}, challenge {(game.Facility == null ? "none" : game.Facility.Room.Key)}");
    }
}
