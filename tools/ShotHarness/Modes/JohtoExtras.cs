partial class Harness
{
    /// <summary>
    /// Johto's own leftovers (plan 20), by themselves in a new game of Johto's (<c>JOHTO_FROM=extras</c>; not part of the
    /// mode's run, so nothing in it moves): the thirteen bands of Johto's Leaders, Elite Four and Champion, each lit
    /// (<c>jx_band_&lt;name&gt;</c>); the Lake of Rage's fishing guru measuring a Magikarp for the record and the TM10 of the
    /// house beside him (<c>jx10</c> to <c>jx12</c>); Mr. Pokémon's trade of the Red Scale (<c>jx20</c>, <c>jx21</c>);
    /// Mahogany's souvenir shop selling Rage Candy Bars (<c>jx30</c>, <c>jx31</c>); and the radio during Team Rocket's
    /// takeover and in Mahogany while the hideout's signal is on (<c>jx40</c>, <c>jx41</c>). It prints the band's colours,
    /// the record, the bag and the station tuned.
    /// </summary>
    void JohtoExtrasShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        var box = game.Dialogue;
        // (A new game of Johto begins with nobody: a battle wants someone on the player's side)
        if (game.Party.Count == 0) game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 50));

        // The bands: each trainer met in Johto as a script's battle makes them, drawn as the person they are
        string[] bands =
        {
            "johto_leader_falkner_falkner", "johto_leader_bugsy_bugsy", "johto_leader_whitney", "johto_leader_morty_morty",
            "johto_leader_chuck_chuck", "johto_leader_jasmine_jasmine", "johto_leader_pryce_pryce", "johto_leader_clair_clair",
            "johto_elite_four_will_will", "johto_elite_four_koga_koga", "johto_elite_four_bruno_bruno", "johto_elite_four_karen_karen",
            "johto_champion_lance"
        };
        foreach (var id in bands)
        {
            var record = TrainerDatabase.Get(id) ?? throw new InvalidOperationException($"No trainer {id}");
            var trainer = new Trainer { Id = record.Id };
            TrainerDatabase.Fill(trainer, record);
            var met = new NPC { Name = trainer.Name, NpcType = trainer.Name.ToLowerInvariant(), TrainerData = trainer };
            At("Johto", 534, 184, Direction.Down);
            Frames(20);
            game.StartTrainerBattle(met);
            for (int i = 0; i < 400 && !BandLit(); i++) Frames(1);
            Frames(12);
            var whose = game.Band?.Band;
            Console.WriteLine($"band of {record.Id}: {game.TransitionKind}, {whose?.Class} {whose?.Name} in {whose?.Base.R},{whose?.Base.G},{whose?.Base.B}, portrait of {game.BandTheirLook}");
            Shot($"jx_band_{trainer.Name.ToLowerInvariant()}");
            Frames((int)(SceneTransition.OutSeconds(game.TransitionKind) * 60) + 40);
            game.State = GameState.Overworld;
            Frames(2);
        }

        void Talk(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            Frames(4);
            for (int i = 0; i < 300 && (engine.ScriptRunning || box.IsActive); i++)
            {
                if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) box.Advance();
                Frames(1);
            }
            game.Interact();
        }
        // Reads on to a line, shoots it, and reads on (answering yes) to the script's end or the next shot's line
        void ShotAt(string text, string shot)
        {
            for (int i = 0; i < 2400; i++)
            {
                if (box.IsActive && box.CurrentLine.Contains(text)) { box.FinishLine(); Frames(4); Shot(shot); return; }
                if (game.State == GameState.Shop) break;
                // (a choosepokemon: the last of the team, the Magikarp brought for the measuring)
                if (game.State == GameState.PartyMenu) { game.PartyScreen.SelectedIndex = game.Party.Count - 1; game.PartyScreen.Confirm(game.Party); Frames(3); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text}");
        }
        void ReadOut()
        {
            for (int i = 0; i < 2400 && (engine.ScriptRunning || box.IsActive || engine.Choice.IsOpen); i++)
            {
                if (game.State == GameState.Shop) { game.Shop.Cancel(); Frames(4); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
                else Frames(1);
            }
            game.State = GameState.Overworld;
        }

        // The fishing guru, shown a Magikarp, and the man beside him
        var magikarp = new Pokemon(PokemonDatabase.Get("Magikarp")!, 20);
        game.Party.Add(magikarp);
        Talk("LakeOfRage_FishingGuruHouse", 5, 6, Direction.Up);
        ShotAt("Oh! You've brought a Magikarp", "jx10_the_guru_sees_a_magikarp");
        ShotAt("new record", "jx11_the_lakes_new_record");
        ReadOut();
        Console.WriteLine($"  the guru's record: {game.Story.Var("VAR_JOHTO_MAGIKARP_SIZE_RECORD")}; Elixirs {game.Bag.GetQuantity(ItemDatabase.Get("Elixir")!)}");
        game.Party.Members.Remove(magikarp);
        Talk("LakeOfRage_HiddenPowerHouse", 5, 6, Direction.Up);
        ShotAt("let this help you find it", "jx12_the_man_and_tm10");
        ReadOut();
        Console.WriteLine($"  TM10 in the bag: {game.Bag.GetQuantity(ItemDatabase.Get("TM10")!)}");

        // Mr. Pokémon and the Red Scale
        game.Story.Set("FLAG_GOT_MYSTERY_EGG");
        game.Story.Set("FLAG_HIDE_MR_POKEMONS_HOUSE_OAK");
        game.Bag.AddItem(ItemDatabase.Get("Red Scale")!);
        Talk("Route30_MrPokemonsHouse", 9, 8, Direction.Up);
        ShotAt("a red one", "jx20_mr_pokemon_sees_the_red_scale");
        ShotAt("Here you are", "jx21_the_exp_share");
        ReadOut();
        Console.WriteLine($"  Red Scales {game.Bag.GetQuantity(ItemDatabase.Get("Red Scale")!)}, Exp. Shares {game.Bag.GetQuantity(ItemDatabase.Get("Exp. Share")!)}");

        // The souvenir shop once the Radio Tower is freed
        game.Story.Set("FLAG_HIDE_MAHOGANY_SHOP_SHADY_SALESMAN");
        game.Story.Set("FLAG_HIDE_MAHOGANY_SHOP_LANCE");
        game.Story.Unset("FLAG_HIDE_MAHOGANY_SHOP_SALESWOMAN");
        game.Story.SetVar("VAR_UNK_40A8", 2);
        Talk("Mahogany_SouvenirShop", 1, 4, Direction.Right);
        ShotAt("Welcome back to the souvenir shop", "jx30_the_saleswoman");
        for (int i = 0; i < 600 && game.State != GameState.Shop; i++)
        {
            if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) box.Advance();
            Frames(1);
        }
        game.Shop.Confirm(game.Bag, 3000, _ => { }); Frames(20);
        Shot("jx31_rage_candy_bars");
        Console.WriteLine($"  the shop's state: {game.State}");
        ReadOut();

        // The radio: Team Rocket's broadcast in the takeover, then Mahogany's signal (the Pokégear and its Radio Card held)
        game.Story.Set(Pokegear.Flag);
        game.Story.Set(Pokegear.RadioCardFlag);
        At("Johto", 534, 184, Direction.Down);
        Frames(10);
        game.Story.SetVar("VAR_SCENE_ROCKET_TAKEOVER", 3);
        game.OpenPokegear(PokegearCard.Radio);
        game.PokegearScreen.Dial.Place(152, 76); Frames(20); Shot("jx40_radio_team_rocket");
        Console.WriteLine($"  in the takeover: {game.PokegearScreen.Dial.Signal}, {game.PokegearScreen.Station}: \"{game.PokegearScreen.StationLine}\"");
        game.PokegearScreen.Cancel(); Frames(4);
        game.State = GameState.Overworld;
        game.Story.SetVar("VAR_SCENE_ROCKET_TAKEOVER", 0);
        game.Story.Unset("FLAG_HIDE_MAHOGANY_SHOP_SHADY_SALESMAN");
        game.Story.Set("FLAG_HIDE_LAKE_OF_RAGE_RED_GYARADOS");
        game.OpenPokegear(PokegearCard.Radio);
        game.PokegearScreen.Dial.Place(112, 76); Frames(20); Shot("jx41_radio_mahogany_signal");
        Console.WriteLine($"  in Mahogany: {game.PokegearScreen.Dial.Signal}, {game.PokegearScreen.Station}: \"{game.PokegearScreen.StationLine}\"");
        game.PokegearScreen.Cancel(); Frames(4);
        game.State = GameState.Overworld;
    }
}
