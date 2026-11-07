partial class Harness
{
    // ---------------------------------------------------------------- the charms (plan 40 · GE5)

    // The charms a Pokédex filled earns, a critical capture and the EXP a catch gives (gf40 to gf44), through the game's
    // own Interact, scripts, bag and battle:
    //   gf40_charm_given: Sinnoh's game director in the Hotel Grand Lake's west cottage handing over the Oval Charm, the
    //     first of the two charms a Pokédex with Sinnoh's and Kanto's seen whole has earned (gf40b_charm_given_exp, the
    //     second, the Exp. Charm);
    //   gf41_bag_charms: the bag's Key Items with the two in it;
    //   gf42_critical_capture: a Poké Ball thrown at a wild Shinx under the modern rules by a player with 601 species
    //     caught (the caught count's whole share), its critical roll and its one shake check fixed to come up, rattling in
    //     the air where it shut over the foe;
    //   gf43_critical_capture_caught: its one wobble done, clicking shut;
    //   gf44_catch_exp: the EXP the catch gives the team, the Exp. Charm's half again on the line.
    // It prints the charms earned and due before and after the director, the bag's charms, the throw (its shakes and
    // whether it was critical) and each EXP line beside the formula's number without the charm. The battle is fought on
    // copies of the team, so the parts after this one find the team as it was; the charms stay in the bag.
    void GiftsCharms()
    {
        var dex = game.Pokedex;
        var bag = game.Bag;
        DialogueManager Box() => game.Dialogue;
        void Tick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            engine.Draw();
        }
        // Reads on until the text box says a line holding the words, leaving it on screen
        bool ReadTo(string words, int most = 3000)
        {
            for (int i = 0; i < most; i++)
            {
                if (Box().IsActive && Translated.Saying(Box().CurrentLine, words)) return true;
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Tick(); Tick(); }
                else Tick();
            }
            Console.WriteLine($"  !! never said: {words}");
            return false;
        }
        void ReadOn(int most = 3000)
        {
            for (int i = 0; i < most && (engine.ScriptRunning || Box().IsActive); i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Tick(); Tick(); }
                else Tick();
            }
        }
        string[] all = { Charms.Oval, Charms.Exp, Charms.Catching, Charms.Mark, Charms.Shiny };
        string Held() => string.Join(", ", all.Where(c => Charms.Held(bag, c)).DefaultIfEmpty("none"));
        string Due() => string.Join(", ", Charms.Due(dex, bag).DefaultIfEmpty("nothing"));

        // Two regional Pokédexes seen whole, Sinnoh's and Kanto's (Kanto's is the player's once Kanto has been entered)
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        dex.Enter(kanto);
        foreach (string id in new[] { Pokedex.Sinnoh, kanto.Dex! })
            foreach (var entry in Pokedex.Entries(id)) dex.RegisterSeen(entry.Species.DexNumber);
        Console.WriteLine($"  the Pokédex: {Charms.RegionalDone(dex)} regional seen whole; earned {string.Join(", ", Charms.Earned(dex))}; due {Due()}; in the bag {Held()}");

        // The game director, spoken to from the tile beside him (from in front, the player's head hides him)
        game.State = GameState.Overworld;
        At("GrandLakeValorLakefrontWestHouse", 4, 3, Direction.Right);
        game.LocationSign.Hide();
        Frames(30);
        game.Interact();
        Frames(3);
        if (ReadTo("received the Oval Charm")) { Box().FinishLine(); Frames(24); Shot("gf40_charm_given"); }
        if (ReadTo("received the Exp. Charm")) { Box().FinishLine(); Frames(24); Shot("gf40b_charm_given_exp"); }
        ReadOn();
        Console.WriteLine($"  after the director: in the bag {Held()}; due {Due()}");
        // Spoken to again, with nothing due, he talks of other things and gives nothing
        int before = bag.AllItems.Sum(s => s.Quantity);
        game.Interact();
        Frames(3);
        ReadOn();
        Console.WriteLine($"  the director again: {(bag.AllItems.Sum(s => s.Quantity) == before ? "nothing given" : "!! something given")}");

        // The bag's Key Items, the Oval Charm's row
        game.State = GameState.BagMenu;
        var screen = game.BagScreen;
        screen.Open();
        screen.CurrentPocket = ItemPocket.KeyItems;
        screen.SelectedIndex = Math.Max(0, bag.GetPocketItems(ItemPocket.KeyItems).FindIndex(s => s.Data.Name == Charms.Oval));
        Frames(30); Shot("gf41_bag_charms");
        screen.Close();
        game.State = GameState.Overworld;
        Frames(4);

        // A wild Shinx in the grass, a Poké Ball thrown by a player whose Pokédex is well filled (601 species caught: the
        // harness's own Pokédex has a few), the critical roll fixed to come up and its one shake check to hold. The team is
        // copied, the Exp. Charm in the bag
        var team = new Party();
        foreach (var p in game.Party.Members) team.Add(p.Clone());
        var wild = new Pokemon(PokemonDatabase.Get("Shinx")!, 14, new Random(5));
        PokemonModels.Get(wild.ModelName);
        var rolls = new BattleRandom(20261107).Force(RollKind.CriticalCapture, 0).Force(RollKind.CatchShake, 0);
        var conditions = new BattleConditions
        {
            Terrain = BattleTerrain.Grass, HasCaught = s => dex.IsCaught(s.DexNumber), CaughtSpecies = 601,
            CatchingCharm = Charms.Held(bag, Charms.Catching), ExpCharm = Charms.Held(bag, Charms.Exp)
        };
        Console.WriteLine($"  the throw's chance: {Formulas.CriticalCapture(Formulas.CatchRate(wild.CatchRate, 10, wild.MaxHP, wild.CurrentHP, wild.Status), 601, conditions.CatchingCharm)} of 256 (the roll fixed at 0); the Exp. Charm {(conditions.ExpCharm ? "in the bag" : "!! not in the bag")}");
        game.Map = MapDatabase.Get("Sinnoh");
        game.BattleRenderer.SetArena(BattleArena.Grass);
        var c = new BattleEngine(new BattleSetup
        {
            PlayerParty = team, Inventory = bag, Pokedex = dex, PcStorage = new PcBoxes(), WildPokemon = new List<Pokemon> { wild },
            Random = rolls, Rules = Ruleset.Current, Conditions = conditions, PlayerName = PlayerIdentity.Name
        });
        game.Battle = c;
        game.State = GameState.Battle;
        // To the menu past the Shinx's Intimidate, then the ball, whose throw shows once its line has been read
        Skip(2.2);
        ToMainMenu(c);
        c.UseItem(ItemDatabase.Get("Poké Ball")!);
        for (int i = 0; i < 600 && c.Anim.Ball == null; i++)
        {
            if (c.IsWaitingForConfirm) c.ConfirmMessage();
            Frames(1);
        }
        void Until(float age)
        {
            for (int i = 0; i < 600 && c.Anim.Ball is { } ball && ball.Age < age; i++) Frames(1);
        }
        // A quarter into its rattle, tilted to one side
        Until(BattleAnimator.BallRattleAt + BattleAnimator.BallRattleTime * 0.12f);
        Shot("gf42_critical_capture");
        Console.WriteLine($"  the ball: {BattleAnimator.BallStage(c.Anim.Ball?.Age ?? 0f, c.Anim.Ball?.Shakes ?? 0, c.Anim.Ball?.Critical ?? false).Phase} at {c.Anim.Ball?.Age:0.00}s");
        Until(BattleAnimator.BallSettleTime(4, critical: true) + 0.12f);
        Shot("gf43_critical_capture_caught");
        foreach (var thrown in c.Core.Log.OfType<BallThrown>())
            Console.WriteLine($"  thrown: {thrown.Ball}, {thrown.Shakes} shakes, {(thrown.Critical ? "a critical capture" : "!! not critical")}");

        // On to the catch's EXP: its line with the charm's share in it
        for (int guard = 0; guard < 30 && !c.IsBattleOver; guard++)
        {
            if (c.IsWaitingForConfirm && c.CurrentMessage.Contains(" gained "))
            {
                Skip(0.4);
                Shot("gf44_catch_exp");
                break;
            }
            Confirm(c);
            Skip(0.3);
        }
        // What the formula gives each without the charm, at the level it had before (the team's own, which the copies left
        // as it was), and the charm's half again of that
        foreach (var gained in c.Core.Log.OfType<ExpGained>())
        {
            int at = c.Core.PlayerParty.Members.IndexOf(gained.Pokemon);
            if (at < 0 || at >= game.Party.Count) continue;
            var own = game.Party.Members[at];
            bool fought = c.Core.SentOut.Contains(gained.Pokemon);
            bool past = own.Species.LevelEvolution is { } next && own.Level >= next.Level;
            int plain = Formulas.ScaledExp(wild.BaseExpYield, wild.Level, own.Level, fought, traded: false, luckyEgg: false, pastEvolution: past);
            int charmed = Charms.ExpShare(plain);
            Console.WriteLine($"  EXP: {own.DisplayName} (Lv. {own.Level}{(fought ? ", fought" : "")}) gained {gained.Amount}: {plain} by the formula, {charmed} with the Exp. Charm" +
                (gained.Amount == charmed ? "" : "  !! not the charm's share"));
        }
        foreach (var line in c.Core.Log.OfType<Said>().Select(s => s.Text).Where(t => t.Contains(" gained ") || t.StartsWith("Gotcha")))
            Console.WriteLine($"  said: {line}");
        for (int guard = 0; guard < 80 && !c.IsBattleOver; guard++) { Confirm(c); Skip(0.3); }
        Console.WriteLine($"  the battle: {c.Result}; the caught one {(team.Members.Any(p => p.Species.Name == "Shinx") ? "joined the copied team" : "!! is not in the team")}");
        game.State = GameState.Overworld;
        Frames(4);
    }
}
