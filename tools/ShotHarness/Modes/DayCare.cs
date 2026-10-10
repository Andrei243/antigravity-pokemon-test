partial class Harness
{
    // ---------------------------------------------------------------- the Day Care, Eggs and hatching (plan 06 · R15)

    // Solaceon Town's Pokémon Day Care and its couple, two Pokémon left with them, an Egg found and handed over, the Egg
    // on the team's cards and its summary, the Day-Care Checker's fan, an Egg hatching through each phase of its scene,
    // and Cynthia offering her Togepi Egg in Eterna City (dc*; not part of "all"). Everything goes through the game's own
    // scripts, steps and screens; it prints what each left behind (the Day Care, the Egg, the hatched Pokémon).
    public void DayCareMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var team = game.Party;
        var dayCare = game.DayCare;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        // Reads every line on to the end of the script, answering the questions as given (yes when none is), and
        // shooting the line that holds a word
        Pokemon? pick = null;
        void ReadOn(string? word = null, string? shot = null, params int[] answers)
        {
            int asked = 0;
            for (int i = 0; i < 60 && (Box().IsActive || engine.Choice.IsOpen || game.State is GameState.Dialogue or GameState.PartyMenu); i++)
            {
                if (game.State == GameState.PartyMenu)
                {
                    if (pick != null) game.PartyScreen.SelectedIndex = team.Members.IndexOf(pick);
                    game.PartyScreen.Confirm(team); Frames(3);
                    continue;
                }
                if (engine.Choice.IsOpen)
                {
                    if (word != null && shot != null && Box().VisibleText.Contains(word)) { Frames(18); Shot(shot); shot = null; }
                    if (asked < answers.Length && answers[asked++] == 1) engine.Choice.Move(1);
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                Whole();
                if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                Next();
            }
            Until(() => game.State == GameState.Overworld && !Box().IsActive, "the field again");
        }

        // The team: Chimchar a female for the Day Care (a starter's female is one in eight), Piplup the father, and a
        // Bidoof, as the Lady won't take a Pokémon that would leave the player with just one (CountAliveMonsAndBoxMons)
        team.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 8));
        var chimchar = team.Members.First(p => p.Species.Name == "Chimchar");
        chimchar.Gender = Gender.Female;
        var piplup = team.Members.First(p => p.Species.Name == "Piplup");
        piplup.Gender = Gender.Male;

        // ---- Solaceon Town: the Day Care's door and the Day-Care Man by his gate
        At("Sinnoh", 555, 649, Direction.Up);
        game.LocationSign.Hide();
        Frames(4);
        Shot("dc01_solaceon_day_care");
        game.Interact(); Frames(3);
        Whole();
        Shot("dc02_day_care_man");
        ReadOn();

        // ---- Inside: the Day-Care Lady takes Chimchar, then Piplup
        At("PokemonDayCare", 9, 11, Direction.Up);
        Frames(4);
        Shot("dc03_day_care_room");
        At("PokemonDayCare", 9, 6, Direction.Up);
        game.PartyScreen.SelectedIndex = 0;
        game.Interact(); Frames(3);
        Whole();
        Until(() => engine.Choice.IsOpen, "the Lady's question"); Frames(18);
        Shot("dc04_lady_question");
        engine.Choice.Confirm(); Frames(4);
        Whole(); Next();
        Until(() => game.State == GameState.PartyMenu, "the team to choose from");
        game.PartyScreen.SelectedIndex = team.Members.IndexOf(chimchar);
        Frames(20);
        Shot("dc05_choose_pokemon");
        game.PartyScreen.Confirm(team); Frames(3);
        // Yes to another: Piplup, and with two left there is nothing more to ask
        pick = piplup;
        ReadOn("raise your", "dc06_well_raise_it", 0);
        Console.WriteLine($"  the Day Care: {dayCare.State}, {string.Join(", ", dayCare.Left.Select(p => $"{p.DisplayName} {p.Gender}"))}; the team {team.Count}");
        if (dayCare.Count < 2)
        {
            At("PokemonDayCare", 9, 6, Direction.Up);
            game.PartyScreen.SelectedIndex = team.Members.IndexOf(piplup);
            game.Interact(); Frames(3);
            ReadOn(null, null, 0);
        }
        Console.WriteLine($"  then: {dayCare.State}, they get on {dayCare.Compatibility}; flag {game.Story.Has("FLAG_STORED_POKEMON_AT_DAY_CARE")}");

        // The Day-Care Checker's fan, there once a Pokémon has been left
        game.Poketch.Enabled = true;
        At("PokemonDayCare", 5, 9, Direction.Left);
        Frames(4);
        game.Interact(); Frames(3);
        Whole();
        Shot("dc07_checker_fan");
        ReadOn();
        Console.WriteLine($"  the Day-Care Checker: {game.Poketch.Has(PoketchApp.DayCareChecker)}");

        // ---- Steps until the couple find an Egg (every 256 steps of the second, one chance in five for these two)
        int steps = 0;
        while (!dayCare.HasEgg && steps < 60000)
        {
            game.DayCareStep();
            steps++;
        }
        Console.WriteLine($"  an Egg after {steps} steps: {dayCare.HasEgg}, personality {dayCare.Offspring:X8}; {dayCare.LevelsGained(0)} and {dayCare.LevelsGained(1)} levels grown");
        game.State = GameState.Overworld;
        At("Sinnoh", 555, 649, Direction.Up);
        game.LocationSign.Hide();
        game.Interact(); Frames(3);
        Whole();
        Until(() => engine.Choice.IsOpen, "the Man's question"); Frames(18);
        Shot("dc08_man_offers_egg");
        engine.Choice.Confirm(); Frames(4);
        Whole();
        Shot("dc09_egg_received");
        ReadOn();
        var egg = team.Members.FirstOrDefault(p => p.IsEgg);
        Console.WriteLine($"  the Egg: {egg?.Species.Name} {egg?.Nature} {egg?.Gender}, {egg?.EggCycles} cycles, moves {string.Join(", ", egg?.Moves.Select(m => m.Name) ?? Array.Empty<string>())}");

        // ---- The Egg on the team's cards and its summary
        game.ChooseFromStartMenu(StartMenuChoice.Pokemon);
        game.PartyScreen.SelectedIndex = team.Members.IndexOf(egg!);
        Frames(30);
        Shot("dc10_party_with_egg");
        game.PartyScreen.ShowSummary = true;
        Frames(20);
        Shot("dc11_egg_summary");
        game.PartyScreen.Close();
        game.State = GameState.Overworld;

        // ---- Hatching: its cycles spent, the field finds it at the end of the cycle under way
        egg!.EggCycles = 0;
        int walked = 0;
        while (game.State == GameState.Overworld && !Box().IsActive && walked < 300)
        {
            if (game.DayCareStep()) break;
            walked++;
        }
        Frames(3);
        Whole();
        Shot("dc12_oh");
        Next();
        Until(() => game.State == GameState.Hatch && game.Hatch.IsActive, "the hatching", 400);
        var hatch = game.Hatch;
        Skip(1.4);
        Shot("dc13_hatch_wobble");
        Until(() => hatch.Phase == HatchPhase.Crack, "the cracks", 400);
        Skip(1.0);
        Shot("dc14_hatch_cracks");
        Until(() => hatch.Phase == HatchPhase.Burst, "the burst", 400);
        Frames(4);
        Shot("dc15_hatch_burst");
        Until(() => hatch.Phase == HatchPhase.Reveal, "the reveal", 400);
        Skip(0.6);
        Shot("dc16_hatch_reveal");
        Until(() => hatch.Phase == HatchPhase.Announce, "the announcement", 400);
        Skip(1.2);
        Shot("dc17_hatched");
        hatch.PressConfirm();
        Until(() => game.State == GameState.Overworld, "the field after hatching", 400);
        Console.WriteLine($"  hatched: {egg.Species.Name} after {walked} steps, egg {egg.IsEgg}, friendship {egg.Friendship}, met {egg.MetLocation} at {egg.MetLevel}, in the Pokédex {game.Pokedex.IsCaught(egg.Species.DexNumber)}");

        // ---- Eterna City: Cynthia catches the player up with her Togepi Egg (the original's trigger 4)
        game.Story.SetVar("VAR_ETERNA_CITY_STATE", 3);
        At("Sinnoh", 307, 543, Direction.Right);
        game.LocationSign.Hide();
        engine.Steering = (Direction.Right, false);
        Until(() => engine.ScriptRunning, "Cynthia's scene", 120);
        engine.Steering = null;
        Until(() => engine.Choice.IsOpen, "Cynthia's question", 1200);
        Frames(18);
        Shot("dc18_cynthia_egg");
        ReadOn();
        Console.WriteLine($"  Cynthia: state {game.Story.Var("VAR_ETERNA_CITY_STATE")}, the team {string.Join(", ", team.Members.Select(p => p.DisplayName))}");
    }
}
