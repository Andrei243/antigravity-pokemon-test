partial class Harness
{
    /// <summary>
    /// What "Finishing Kalos" left owed on the harness (plan 23; <c>KALOS_FROM=owed</c>, not part of the whole mode, so
    /// nothing in it moves): the thirteen bands of Kalos's Leaders, Elite Four and Champion, each lit, met in Kalos so their
    /// portraits are the grown looks (<c>kwb_band_&lt;name&gt;</c>); a species of ours met in Route 18's grass from the
    /// place's own slots of ours after Kalos's Hall of Fame (<c>kwo01</c>); and Lumiose's groomer trimming a Furfrou
    /// (<c>kwg01</c> to <c>kwg03</c>). It prints each band's colours and portrait, the grass's slots of ours and what was
    /// met, and the Furfrou's form before and after.
    /// </summary>
    void KalosOwed()
    {
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        story.ChooseStarter("Froakie");
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        Frames(2);

        // ---- the thirteen bands
        string[] bands =
        {
            "kalos_viola_1", "kalos_grant_1", "kalos_korrina_1", "kalos_ramos_1", "kalos_clemont_1", "kalos_valerie_1",
            "kalos_olympia_1", "kalos_wulfric_1", "kalos_malva_1", "kalos_siebold_1", "kalos_wikstrom_1", "kalos_drasna_1",
            "kalos_diantha_1"
        };
        foreach (var id in bands)
        {
            var record = TrainerDatabase.Get(id, "Kalos") ?? TrainerDatabase.Get(id);
            if (record == null) { Console.WriteLine($"  !! no trainer {id}"); continue; }
            var trainer = new Trainer { Id = record.Id };
            TrainerDatabase.Fill(trainer, record);
            var met = new NPC { Name = trainer.Name, NpcType = trainer.Name.ToLowerInvariant(), TrainerData = trainer };
            At("Kalos", 255, 624, Direction.Down);
            game.LocationSign.Hide();
            Frames(20);
            // (the grown bust made first: a band never waits for its rig, and a rig still meshing shows the silhouette)
            CharacterModels.Get(CharacterStyle.InKit(CharacterStyles.Default.InBattle(trainer), CharacterKit.Grown), game.RenderContext.Shaders);
            game.StartTrainerBattle(met);
            for (int i = 0; i < 400 && !BandLit(); i++) Frames(1);
            Frames(12);
            if (game.BandRendered == 0) Console.WriteLine($"  !! {record.Id}: the portrait was not rendered by the shot");
            var whose = game.Band?.Band;
            Console.WriteLine($"  band of {record.Id}: {game.TransitionKind}, {whose?.Class} {whose?.Name} base {whose?.Base.R},{whose?.Base.G},{whose?.Base.B} light {whose?.Light.R},{whose?.Light.G},{whose?.Light.B}, portrait of {game.BandTheirLook}");
            Shot($"kwb_band_{trainer.Name.ToLowerInvariant()}");
            Frames((int)(SceneTransition.OutSeconds(game.TransitionKind) * 60) + 40);
            game.State = GameState.Overworld;
            Frames(2);
        }

        // ---- a species of ours met in Route 18's grass, from the place's own slots after Kalos's Hall of Fame
        story.Set("KalosHallOfFame");
        var kalos = MapDatabase.Get("Kalos");
        (int X, int Y)? grass = null;
        for (int y = 0; y < kalos.Height && grass == null; y++)
            for (int x = 0; x < kalos.Width && grass == null; x++)
                if (kalos.IsTallGrass(x, y) && kalos.AreaAt(x, y)?.Key == "kalos_route_18") grass = (x, y);
        if (grass is { } g)
        {
            At("Kalos", g.X, g.Y, Direction.Up);
            game.LocationSign.Hide();
            Frames(4);
            var area = kalos.AreaAt(g.X, g.Y)!;
            var slots = EncounterSlots.Grass(area, game.EncounterMomentNow());
            var ours = slots.Where(s => s.Unmapped).ToList();
            Console.WriteLine($"  kwo01: Route 18's grass at {g.X},{g.Y}; slots of ours {string.Join(", ", ours.Select(s => $"{s.SpeciesName} {s.MinLevel}-{s.MaxLevel}"))}");
            if (ours.Count > 0)
            {
                game.StartWildBattle(ours[0]);
                for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
                if (game.State == GameState.Battle)
                {
                    Skip(1.0);
                    ToMainMenu(game.Battle);
                    var foe = game.Battle!.EnemyPokemon;
                    Console.WriteLine($"  kwo01: met {foe.Species.Name} (No. {foe.Species.DexNumber}, ours {foe.Species.IsOurs}) at {foe.Level}");
                    Shot("kwo01_ours_in_route_18_grass");
                }
                game.State = GameState.Overworld;
                Frames(4);
            }
            else Console.WriteLine("  !! kwo01: Route 18's grass has no slot of ours after the Hall of Fame");
        }
        else Console.WriteLine("  !! kwo01: no tall grass of Route 18 found");

        KalosGroomer();
    }

    /// <summary>Lumiose's groomer trims a Furfrou of the team (kwg01 the question, kwg02 the trims offered, kwg03 the Furfrou trimmed on its summary's page).</summary>
    void KalosGroomer()
    {
        var team = game.Party;
        var furfrou = new Pokemon(PokemonDatabase.Get("Furfrou")!, 40);
        team.Add(furfrou);
        game.Money = Math.Max(game.Money, 5000);
        var answers = new Queue<int>(new[] { 0, 0 });
        string? choiceShot = "kwg02_groomer_trims";
        At("LumioseCity_GroomingSalon", 5, 5, Direction.Up);
        game.LocationSign.Hide();
        Frames(10);
        game.Interact();
        bool asked = false;
        for (int i = 0; i < 4000; i++)
        {
            if (!engine.ScriptRunning && !game.Dialogue.IsActive && game.State == GameState.Overworld && i > 10) break;
            if (game.State == GameState.PartyMenu)
            {
                Frames(30);
                var screen = game.PartyScreen;
                screen.SelectedIndex = team.Members.IndexOf(furfrou);
                Frames(2);
                screen.Confirm(team);
                Frames(6);
                if (game.State == GameState.PartyMenu) { screen.Confirm(team); Frames(6); }
                continue;
            }
            if (engine.Choice.IsOpen)
            {
                Frames(12);
                if (!asked) { Shot("kwg01_groomer_asks"); asked = true; }
                else if (choiceShot != null) { Shot(choiceShot); choiceShot = null; }
                int answer = answers.Count > 0 ? answers.Dequeue() : 0;
                for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                Frames(2);
                engine.Choice.Confirm();
                Frames(4);
                continue;
            }
            if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
            else Frames(1);
        }
        Console.WriteLine($"  kwg: the Furfrou's form {furfrou.Form ?? "natural"}, {furfrou.TrimDays} days of trim; money {game.Money}");
        // The trimmed Furfrou leading a battle, from behind as the player's own is seen
        team.Swap(0, team.Members.IndexOf(furfrou));
        At("Kalos", 255, 624, Direction.Up); game.LocationSign.Hide(); Frames(4);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bunnelby", MinLevel = 20, MaxLevel = 20 });
        for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
        if (game.State == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("kwg03_furfrou_trimmed"); }
        game.State = GameState.Overworld;
        Frames(4);
    }
}
