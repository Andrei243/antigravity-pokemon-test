partial class Harness
{
    /// <summary>
    /// The Pokégear on its own (plan 20 · J3; not part of <c>all</c>): a game of Johto past its first steps, in New Bark
    /// Town, and then <see cref="PokegearShots"/>.
    /// </summary>
    public void PokegearMode()
    {
        engine.NewGameRegion = "Johto";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        // The opening as an older save is taken past it, with Elm's first Pokémon
        engine.StartScript("common.JohtoOpeningDone");
        Frames(4);
        engine.StartScript("common.JohtoPokegear");
        Frames(4);
        if (game.Party.Count == 0) game.Party.Add(new Pokemon(PokemonDatabase.Get("Chikorita")!, 9));
        PokegearShots();
        engine.NewGameRegion = "Sinnoh";
    }

    /// <summary>
    /// The Pokégear's cards (plan 20 · J3; <c>pg*</c>, the end of the <c>johto</c> mode): the phone with Mom's, Elm's and
    /// Joey's numbers, Joey ringing on his day to ask for a rematch, the map card and the map it opens, the radio tuned
    /// to the Pokémon Music Channel on a Monday (the March) and to Professor Oak's talk; and the March given up for the
    /// area's theme as the radio is tuned away. It prints the contacts, what each call was, the songs and the stations.
    /// </summary>
    private void PokegearShots()
    {
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        void ReadOn(int most = 1200)
        {
            for (int i = 0; i < most; i++)
            {
                if (!engine.ScriptRunning && !Box().IsActive && game.State == GameState.Overworld) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine("  !! the call never ended");
        }

        // A Monday by day, Joey's hour (the harness's own date is put back at the end)
        var date = GameClock.FixedDate;
        var monday = (date ?? new DateTime(2026, 10, 19)).Date;
        while (monday.DayOfWeek != DayOfWeek.Monday) monday = monday.AddDays(1);
        GameClock.FixedDate = monday;
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // Joey beaten on Route 30, and his number given; the Radio Card as the Radio Tower's quiz would give it (J7)
        story.Set(Pokegear.Flag);
        story.Set(Pokegear.MapCardFlag);
        story.Set(Pokegear.RadioCardFlag);
        story.Defeat("johto_youngster_joey");
        Pokegear.Register(story, "mother");
        Pokegear.Register(story, "prof_elm");
        Pokegear.Register(story, "joey");
        Console.WriteLine($"pokegear: cards {string.Join(", ", Pokegear.Cards(story))}; numbers {string.Join(", ", Pokegear.Contacts(story).Select(c => c.Name))}");

        At("Johto", 690, 400, Direction.Down); Frames(4);
        game.OpenPokegear(PokegearCard.Phone);
        // The cursor on Joey
        game.PokegearScreen.Move(0, 2);
        Frames(12); Shot("pg01_phone");

        // Joey rings: his day and hour, so he asks for a rematch, and waits on Route 30
        game.PokegearScreen.Cancel(); Frames(2);
        game.PlaceCall(PhoneBook.Get("joey")!, incoming: true);
        for (int i = 0; i < 90 && !(Box().IsActive && Box().IsCurrentLineComplete); i++) Frames(1);
        Frames(6); Shot("pg02_call");
        Console.WriteLine($"  Joey's call: VAR_PHONE_CALL {story.Var(Pokegear.CallVar)}, waiting {story.Has(PhoneRematch.WaitingFlag("joey"))}, "
            + $"on Route 30 spinning {MapDatabase.Get("Johto").Everyone.Any(n => n.TrainerData?.Id == "johto_youngster_joey" && n.ReadyForRematch)}");
        ReadOn();

        // The map card, and the map it opens
        game.OpenPokegear(PokegearCard.Map); Frames(12); Shot("pg03_map");
        game.PokegearScreen.Confirm(); Frames(20); Shot("pg03b_map_opened");
        game.TownMapScreen.Cancel(); Frames(20);
        Console.WriteLine($"  the map card: state {game.State}, card {game.PokegearScreen.Card}, at {game.PokegearScreen.PlayerCell}, {game.PokegearScreen.PlaceHere}");

        // The radio: the music channel on a Monday is the March, over the field
        game.OpenPokegear(PokegearCard.Radio);
        game.PokegearScreen.Dial.Place(112, 76); Frames(20); Shot("pg04_radio_march");
        Console.WriteLine($"  tuned to {game.PokegearScreen.Station}, clear {game.PokegearScreen.Clear}, song {game.RadioSong}");
        // Oak's talk, a moment later
        game.PokegearScreen.Dial.Place(152, 76); Frames(20); Shot("pg05_radio_talk");
        Console.WriteLine($"  tuned to {game.PokegearScreen.Station}: \"{game.PokegearScreen.StationLine}\", song {game.RadioSong}");
        // Back to the March, out into the field with it, and tuned away: the area's theme again
        game.PokegearScreen.Dial.Place(112, 76); Frames(4);
        game.PokegearScreen.Cancel(); Frames(4);
        Console.WriteLine($"  in the field the radio plays {game.RadioSong}; the encounters' moment hears {game.EncounterMomentNow().Radio}");
        game.OpenPokegear(PokegearCard.Radio);
        game.PokegearScreen.Dial.Place(RadioDial.CenterX, RadioDial.CenterY); Frames(4);
        game.PokegearScreen.Cancel(); Frames(4);
        Console.WriteLine($"  tuned away, the radio plays {game.RadioSong}: the area's theme, {game.Map.BgmTrackAt(game.Player.GridX, game.Player.GridY)}");

        GameClock.FixedDate = date;
    }
}
