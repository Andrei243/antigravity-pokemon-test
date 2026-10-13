partial class Harness
{
    // The formats of the later games (plan 14; not part of `all`; ft*). B1, inverse battles, through the game's own
    // TryInteract, scripts, question box and battle menus: the Fight Area's plaza before the tag battle (nobody by the
    // Mart) and after it, the scientist, what an inverse battle is, his offer refused and taken; the battle opening
    // with its line, the move cards' hints, a hit that Platinum's chart resists (ft01), one a type would have stopped
    // (ft02) and one that was four times as strong (ft03), and whatever the scientist's own Pokémon lands on a Ground
    // type (ft04); his word after a win, the same day's "come back tomorrow", the next day's offer, and a battle lost.
    // The player's Pokémon and the scientist's are made whole at every menu, so each hit is seen on a full bar. It
    // prints what each left behind (the day's flag, the battle's chart and its record's, each hit with what was said
    // of it, the money, who is beaten). B14, last: the party screen choosing a Bidoof with a Starly greyed (ft60), the
    // Bidoof Master's battle with the bag shut (ft61), and what it left (the master beaten, the Bidoof's level, the money).
    public void FormatsMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var team = game.Party;

        // A Ground type that knows a move of each kind the chart turns round for the scientist's Magneton (Electric and
        // Steel): Normal, which Steel resists; Poison, which Steel stops; Ground, which both fear. It is of a level at
        // which neither Strength nor Earthquake brings a whole Magneton down even as a critical hit, so the hits that
        // follow are seen too; and there are three of it, since the Magneton's Discharge, which a Ground type shrugged
        // off in Platinum, fells one at a stroke
        Pokemon Lead()
        {
            var nidoking = new Pokemon(PokemonDatabase.Get("Nidoking")!, 44);
            nidoking.Moves.Clear();
            foreach (string move in new[] { "Strength", "Poison Jab", "Earthquake", "Roar" }) nidoking.Moves.Add(new Move(MoveDatabase.Get(move)));
            return nidoking;
        }
        team.Members.Clear();
        for (int i = 0; i < 3; i++) team.Members.Add(Lead());
        foreach (var badge in Enum.GetValues<Badge>()) story.GiveBadge(badge);
        // The cards say how a move will do, of a species seen before (plan 12 · Q10): they must tell the truth here too
        engine.Settings.MoveHints = RulesDefault.On;
        engine.ApplySettings(window: false);
        foreach (string seen in new[] { "Magneton", "Bronzong" }) game.Pokedex.RegisterSeen(PokemonDatabase.Get(seen)!.DexNumber);

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        int quick = 0;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (++quick % 6 == 0) engine.Draw();
        }
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Quick();
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        // Reads on to a line (null: to the script's end), leaving a question open for the mode to answer
        void ReadTo(string text, int most = 3000)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle || engine.Choice.IsOpen) return;
                if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void ToQuestion(string what, int most = 3000)
        {
            for (int i = 0; i < most && !engine.Choice.IsOpen; i++)
            {
                if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            if (!engine.Choice.IsOpen) Console.WriteLine($"  !! never asked: {what}");
            Frames(10);
        }
        void Answer(int answer)
        {
            for (int a = 0; a < answer; a++) engine.Choice.Move(1);
            engine.Choice.Confirm();
            Quick();
        }
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            game.SteppedOutOfWarp = true;
            game.LocationSign.Hide();
            Frames(2);
        }
        const string Sinnoh = "Sinnoh", Place = "fight_area", Who = "inverse_scientist";
        const string Day = "FLAG_DAILY_FIGHT_AREA_INVERSE_BATTLE", Explained = "FLAG_FIGHT_AREA_INVERSE_EXPLAINED";
        NPC Scientist() => MapDatabase.Get(Sinnoh).NPCs.FirstOrDefault(n => n.Key == Who && n.ScriptFile == Place);
        // Spoken to from the west, so both are seen whole
        void Speak()
        {
            var npc = Scientist();
            if (npc == null) { Console.WriteLine("  !! the scientist isn't there"); return; }
            At(Sinnoh, npc.GridX - 1, npc.GridY, Direction.Right);
            game.Interact();
        }
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));
        string Hp(Pokemon p) => $"{p.Species.Name} {p.CurrentHP}/{p.MaxHP}";

        // Into the battle the script starts, read on to its menu; the opening's own line is shot on the way
        BattleEngine IntoBattle(string openingShot)
        {
            for (int i = 0; i < 2400 && State() != GameState.Battle; i++)
            {
                if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            var fight = State() == GameState.Battle ? game.Battle : null;
            if (fight == null) { Console.WriteLine("  !! never happened: the battle"); return null; }
            Skip(1.0);
            for (int i = 0; i < 40 && fight.HUD.MenuState == BattleMenuState.Message && !fight.IsBattleOver; i++)
            {
                if (fight.CurrentMessage == BattleCore.InverseLine && openingShot != null) { Skip(0.6); Shot(openingShot); }
                Confirm(fight);
                Skip(0.8);
            }
            Console.WriteLine($"the battle: inverse {fight.Core.Inverse}, its field {fight.Core.Field.Inverse}, its record {fight.Core.Record?.Inverse}; "
                + $"{fight.Trainers[0].FullTitle} with {string.Join(", ", fight.EnemyParty.Members.Select(p => $"{p.Species.Name} {p.Level} ({string.Join("/", p.Moves.Select(m => m.Name))})"))}");
            return fight;
        }
        bool foeShot = false, keptOffShot = false;
        const string KeptOff = "ft03b_inverse_magnetism_still_keeps_ground_off";
        // A turn with one of the lead's moves, both sides whole as it begins: the move's own line and the line about
        // how it did are shot (<shot>_a_move, <shot>), with the first line about a move of the scientist's Pokémon's
        // (ft04) and the first time its magnetism keeps a Ground move off (ft03b). True when the lead's move was seen
        // to land: the scientist's Pokémon may fell it first
        bool PlayTurn(BattleEngine fight, int move, string shot)
        {
            // Whoever fell last turn is followed by the next of the three
            if (fight.HUD.MenuState == BattleMenuState.SwitchPokemon)
            {
                int next = fight.PlayerParty.Members.FindIndex(p => !p.IsFainted && !fight.PlayerSlots.Any(s => s.Pokemon == p));
                Console.WriteLine($"  the next Nidoking goes in ({next})");
                fight.SelectSwitch(Math.Max(0, next));
                for (int i = 0; i < 20 && !fight.IsBattleOver && fight.HUD.MenuState == BattleMenuState.Message; i++) { Confirm(fight); Skip(0.5); }
            }
            if (fight.IsBattleOver || fight.HUD.MenuState is not (BattleMenuState.Main or BattleMenuState.Moves)) { Console.WriteLine($"  !! no menu for {shot ?? "a turn"}"); return false; }
            foreach (var mine in team.Members.Where(p => p.CurrentHP > 0)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; foreach (var m in mine.Moves) m.CurrentPP = m.MaxPP; }
            foreach (var foe in fight.EnemyParty.Members.Where(p => p.CurrentHP > 0)) { foe.CurrentHP = foe.MaxHP; foe.Status = StatusCondition.None; }
            var target = fight.EnemyPokemon;
            var user = fight.PlayerPokemon;
            int before = target.CurrentHP, mineBefore = user.CurrentHP;
            if (fight.HUD.MenuState == BattleMenuState.Main) fight.SelectMainMenuOption(0);
            fight.SelectMove(move);
            string used = "";
            bool landed = false;
            // Each line is looked at a fifth of a second after it appears: a move's own line then shows the move on
            // its way, before it lands (0.35 s in) and its target flickers
            Skip(0.2);
            for (int i = 0; i < 60 && !fight.IsBattleOver && fight.HUD.MenuState == BattleMenuState.Message; i++)
            {
                string line = fight.CurrentMessage;
                if (line.Contains(" used "))
                {
                    used = line;
                    if (shot != null && !line.StartsWith("Foe ") && !line.Contains(" used the ")) Shot(shot + "_a_move");
                }
                else if (line is "It's super effective!" or "It's not very effective..." || line.StartsWith("It doesn't affect"))
                {
                    Skip(0.5);
                    bool theirs = used.StartsWith("Foe ");
                    bool stopped = line.StartsWith("It doesn't affect");
                    // A Ground move kept off by the Magneton's magnetism is a picture of its own, and no hit landed
                    if (!theirs && stopped && !keptOffShot) { Shot(KeptOff); keptOffShot = true; }
                    if (!theirs && !stopped && shot != null) Shot(shot);
                    if (theirs && !foeShot) { Shot("ft04_inverse_line_the_foes"); foeShot = true; }
                    landed |= !theirs && !stopped;
                    Console.WriteLine($"  {used} {line}");
                }
                Skip(0.2);
                Confirm(fight);
                Skip(0.2);
            }
            Console.WriteLine($"  after it: Foe {target.Species.Name} {before} -> {target.CurrentHP}; {user.Species.Name} {mineBefore} -> {user.CurrentHP}");
            return landed;
        }
        // The same, tried again where the lead fell before it could move
        void Hit(BattleEngine fight, int move, string shot)
        {
            for (int tries = 0; tries < 3; tries++) if (PlayTurn(fight, move, shot)) return;
            Console.WriteLine($"  !! never landed: {shot}");
        }
        // The battle played out: the scientist's Pokémon left a single HP at every menu, or the player's (a loss)
        void Finish(BattleEngine fight, bool win)
        {
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    if (win)
                    {
                        foreach (var mine in team.Members.Where(p => p.CurrentHP > 0)) mine.CurrentHP = mine.MaxHP;
                        foreach (var foe in fight.EnemyParty.Members) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    }
                    else foreach (var mine in team.Members) mine.CurrentHP = Math.Min(mine.CurrentHP, 1);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(0);
                }
                else if (fight.HUD.MenuState == BattleMenuState.SwitchPokemon)
                {
                    int next = fight.PlayerParty.Members.FindIndex(p => !p.IsFainted && !fight.PlayerSlots.Any(s => s.Pokemon == p));
                    fight.SelectSwitch(Math.Max(0, next));
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Console.WriteLine($"the battle's end: {fight.Result}");
            Until(() => State() != GameState.Battle, "the field again", 1500);
            Frames(10);
        }

        Frames(5);
        ReadTo(null, 600);

        // ---- before the tag battle before the gate nobody stands by the Mart
        ComeThrough(Sinnoh, 661, 432, Direction.Right);
        Frames(40); Shot("ft00a_fight_area_before_the_tag_battle");
        Console.WriteLine($"before the tag battle: the scientist there {Scientist() != null}; {Flags("FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS")}");

        // ---- the Fight Area as that scene leaves it (scripts/fight_area.txt, Battle): the two, their crowd, Palmer, the
        // rival and Buck gone, a Champion with the National Pokédex
        engine.StartScript(ScriptParser.Parse(Place, "script Test\n"
            + "setflag FLAG_GAME_COMPLETED\nsetflag FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS\nsetflag FLAG_HIDE_FIGHT_AREA_PALMER\n"
            + "setflag FLAG_HIDE_FIGHT_AREA_RIVAL\nsetflag FLAG_HIDE_FIGHT_AREA_BUCK\nsetflag FLAG_HIDE_FIGHT_AREA_BLOCKADE\nsetvar VAR_FIGHT_AREA_STATE 2")[0]);
        ReadTo(null, 300);
        ComeThrough(Sinnoh, 660, 434, Direction.Up);
        Frames(40); Shot("ft00b_fight_area_the_scientist");
        var him = Scientist();
        Console.WriteLine($"after it: the scientist there {him != null}, at {him?.GridX},{him?.GridY}, a trainer of the map {him?.IsTrainer}, a team on the map {him?.TrainerData != null}");

        // ---- what an inverse battle is, the offer, a refusal
        Speak();
        ReadTo("stood on its head"); Frames(4); Shot("ft00c_a_battle_stood_on_its_head");
        ReadTo("turned round"); Frames(4); Shot("ft00d_every_matchup_turned_round");
        ReadTo("takes double"); Frames(4); Shot("ft00e_it_takes_double");
        ToQuestion("the offer"); Shot("ft00f_the_offer");
        Answer(1);
        ReadTo("unlearning"); Frames(4); Shot("ft00g_refused");
        ReadTo(null);
        Console.WriteLine("refused: " + Flags(Day, Explained));

        // ---- asked again he goes straight to the offer, and it is taken
        Speak();
        ToQuestion("the offer again"); Shot("ft00h_the_offer_again");
        Answer(0);
        ReadTo("Begin!"); Frames(4); Shot("ft00i_begin");
        int money = game.Money;
        var battle = IntoBattle("ft00j_inverse_battle_opens");
        if (battle != null)
        {
            Skip(0.6); Shot("ft00k_inverse_battle_menu");
            // The cards' hints, which are the battle's own chart: Strength and Poison Jab super effective, Earthquake not
            battle.SelectMainMenuOption(0);
            Frames(30); Shot("ft00l_inverse_battle_hints");
            var lead = battle.PlayerPokemon;
            Console.WriteLine("the hints: " + string.Join(", ", lead.Moves.Select(m => $"{m.Name} {battle.HintFor(m, battle.EnemySlots[0])}")));

            // Earthquake goes first, before the Magneton has risen on its magnetism (which still keeps Ground off), and
            // Poison Jab, which may bring it down, last
            Hit(battle, 2, "ft03_inverse_line_a_weakness");       // Earthquake: Ground against Electric and Steel, four times in Platinum
            Hit(battle, 0, "ft01_inverse_line");                  // Strength: Normal against Steel, resisted in Platinum
            Hit(battle, 1, "ft02_inverse_line_an_immunity");      // Poison Jab: Poison against Steel, nothing in Platinum
            // And on with Earthquake until the Magneton has struck with a move of its own that the chart has a word for
            // (ft04) and its magnetism has kept a Ground move off (ft03b), a few turns at most
            for (int more = 0; more < 6 && (!foeShot || !keptOffShot) && !battle.IsBattleOver && battle.EnemyPokemon.Species.Name == "Magneton"; more++) PlayTurn(battle, 2, null);
            Console.WriteLine($"seen: the Magneton's own hit {foeShot}, its magnetism keeping Ground off {keptOffShot}");
            Finish(battle, win: true);
        }
        ReadTo("straight into my notes"); Frames(4); Shot("ft05_after_a_win");
        ReadTo("come back tomorrow"); Frames(4); Shot("ft06_a_fresh_page");
        ReadTo(null);
        Console.WriteLine($"won: {Flags(Day, Explained)}; money {money} -> {game.Money}; beaten {string.Join(", ", story.DefeatedTrainers.Where(id => id.StartsWith("scientist_")))}; "
            + $"the scientist's own on Route 212 beaten {story.HasDefeated("scientist_shaun")}");

        // ---- the same day he has had his battle
        Speak();
        ReadTo("Come back tomorrow"); Frames(4); Shot("ft07_come_back_tomorrow");
        ReadTo(null);

        // ---- a new day clears the day's flag (the game's own daily events), and he offers again; this one is lost
        GameClock.FixedDate = GameClock.FixedDate!.Value.AddDays(1);
        Frames(4);
        Console.WriteLine($"the next day ({GameClock.Today:yyyy-MM-dd}): " + Flags(Day, Explained));
        team.Members.Clear();
        var weak = new Pokemon(PokemonDatabase.Get("Bidoof")!, 5);
        team.Members.Add(weak);
        Speak();
        ToQuestion("the next day's offer"); Shot("ft08_the_next_days_offer");
        Answer(0);
        ReadTo("Begin!");
        battle = IntoBattle(null);
        if (battle != null) Finish(battle, win: false);
        ReadTo("Old habits"); Frames(4); Shot("ft09_after_a_loss");
        ReadTo(null);
        var where = game.Player;
        Console.WriteLine($"lost: {Flags(Day)}; the player still at {where.GridX},{where.GridY} of {game.Map.Name}, {Hp(weak)}; state {State()}");

        // ---- plan 14 · B14, a Master Trainer: the party screen choosing with only Bidoof let through (a Starly greyed
        // beside it), and the master's battle with the bag shut to both sides. No master stands in the world yet (plan
        // 14, decision 8), so a script of the harness's own has the scientist fight as the Bidoof Master
        team.Members.Clear();
        var starly = new Pokemon(PokemonDatabase.Get("Starly")!, 30);
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 60);
        team.Members.Add(starly);
        team.Members.Add(bidoof);
        int masterLevel = bidoof.Level, moneyBefore = game.Money;
        var standing = Scientist();
        if (standing != null) At(Sinnoh, standing.GridX - 1, standing.GridY, Direction.Right);
        engine.StartScript(ScriptParser.Parse(Place, "script Master\n  choosepokemon \"Bidoof\"\n  if result == 255 end\n  master " + Who + " canlose\n  end\n")[0]);
        Until(() => game.PartyScreen.IsActive, "the party screen choosing");
        game.PartyScreen.SelectedIndex = 1;
        Frames(30); Shot("ft60_master_choose");
        Console.WriteLine($"choosing: only some {game.PartyScreen.ChoosingSome}; tags {string.Join(", ", team.Members.Select(p => $"{p.Species.Name} {game.PartyScreen.ChooseTag(p) ?? "-"}"))}");
        game.PartyScreen.SelectedIndex = 0;
        game.PartyScreen.Confirm(team);
        Console.WriteLine($"the Starly chosen: {game.PartyScreen.Message}; still open {game.PartyScreen.IsActive}");
        game.PartyScreen.SelectedIndex = 1;
        game.PartyScreen.Confirm(team);
        var master = IntoBattle(null);
        if (master != null)
        {
            Skip(0.6); Shot("ft61_master_bag_closed");
            Console.WriteLine($"the master's battle: bag closed {master.BagClosed}; the player's side {string.Join(", ", master.PlayerParty.Members.Select(p => $"{p.Species.Name} {p.Level}"))}; "
                + $"{master.Trainers[0].FullTitle}'s {string.Join(", ", master.EnemyParty.Members.Select(p => $"{p.Species.Name} {p.Level} ({string.Join("/", p.Moves.Select(m => m.Name))})"))}");
            master.SelectMainMenuOption(1);
            Console.WriteLine($"BAG: {master.CurrentMessage}");
            Finish(master, win: true);
        }
        ReadTo(null);
        Console.WriteLine($"the master beaten: master_bidoof {story.HasDefeated("master_bidoof")}; the Bidoof {masterLevel} -> {bidoof.Level}, still the team's {team.Members.Contains(bidoof)}; "
            + $"money {moneyBefore} -> {game.Money}");
    }
}
