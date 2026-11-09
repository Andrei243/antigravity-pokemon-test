partial class Harness
{
    /// <summary>
    /// The hub (plan 41 · EG1; <c>JOURNEY_FROM=hub</c>): a Champion of Sinnoh and Kanto at Canalave's pier, the captain's
    /// question with the hub offered last (<c>eg10</c>), the hub's quay on arrival (<c>eg11</c>), the town by day and after
    /// dark (<c>eg12</c>, <c>eg13</c>), Fly's choice with the hub's tab last (<c>eg14</c>), its Pokémon Center
    /// (<c>eg15</c>) and the facility guide's question in the harbour hall (<c>eg16</c>). Each through the game's own
    /// <c>Interact</c>, scripts and question box; it prints where each landed and the rules the hub borrows.
    /// </summary>
    private void JourneyHub()
    {
        DialogueManager Box() => game.Dialogue;
        void ReadToTheQuestion(int most = 1200)
        {
            for (int i = 0; i < most && !engine.Choice.IsOpen; i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(12);
        }
        void ReadOn(int most = 1200)
        {
            for (int i = 0; i < most && (game.State != GameState.Overworld || Box().IsActive || engine.Choice.IsOpen); i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(20);
        }

        engine.NewGameRegion = RegionDatabase.Sinnoh;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        game.Party.Members.Clear();
        foreach (string species in new[] { "Garchomp", "Staraptor", "Infernape" })
            game.Party.Members.Add(new Pokemon(PokemonDatabase.Get(species)!, 70));
        foreach (string region in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto })
        {
            game.Story.EnterRegion(region);
            game.Story.SetBadges(region, 0xFF);
            game.Story.Set(region + "HallOfFame");
        }
        game.Pokedex.Enter(RegionDatabase.Get(RegionDatabase.Kanto)!);

        // ---- Canalave's captain: Kanto on, the hub last
        var pier = MapDatabase.Get("Sinnoh");
        var captain = pier.Everyone.Single(n => n.IsTransportAttendant);
        At("Sinnoh", 44, 751, Direction.Right);
        game.Interact();
        ReadToTheQuestion();
        Shot("eg10_harbour_offers_the_hub");
        engine.Choice.Move(1);
        engine.Choice.Confirm();
        // Read on through the crossing's lines and dark until the hub's quay is under the player's feet
        for (int i = 0; i < 2400 && (game.Map.Name != "Hub" || game.State != GameState.Overworld || Box().IsActive); i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Frames(90);
        game.LocationSign.Hide();
        Frames(10);
        Shot("eg11_hub_arrival");
        Console.WriteLine($"  landed on {game.Map.Name} {game.Player.GridX},{game.Player.GridY}; the rules here are {engine.RulesHere.Id}'s");

        // ---- The town by day and after dark, from the avenue
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        At("Hub", 31, 19, Direction.Up); Frames(4); Shot("eg12_hub_by_day");
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Hub", 31, 19, Direction.Up); Frames(4); Shot("eg13_hub_after_dark");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // ---- Fly's choice from Kanto: the hub's tab last
        At("Kanto", 70, 272, Direction.Up);
        Frames(4);
        // (the regions Crossing.FlyRegions gives, as the party menu's Fly opens them, the hub's tab chosen)
        var flyRegions = Crossing.FlyRegions(engine.Here, game.Story, game.Pokedex.Entered);
        Console.WriteLine($"  Fly's tabs: {string.Join(", ", flyRegions.Select(r => r.Id))}");
        var flyTabs = flyRegions.Select(r => new TownMapState(RegionMap.Of(r.Id)!, game.Story)).ToList();
        game.TownMapScreen.Open(flyTabs, flyTabs.Count - 1, TownMapMode.Fly, new Pokemon(PokemonDatabase.Get("Staraptor")!, 70));
        game.State = GameState.FlyMap;
        Frames(40);
        Shot("eg14_fly_hub_tab");
        game.TownMapScreen.Cancel(); Frames(4);
        game.State = GameState.Overworld;

        // ---- Its Pokémon Center, and the facility guide's question in the harbour hall
        At("Hub_PokemonCenter", 8, 9, Direction.Up); Frames(4); Shot("eg15_hub_pokemon_center");
        At("Hub_HarbourHall", 9, 4, Direction.Up);
        Frames(4);
        game.Interact();
        ReadToTheQuestion();
        Shot("eg16_facility_guide");
        engine.Choice.Move(3);
        engine.Choice.Confirm();
        ReadOn();
    }
}
