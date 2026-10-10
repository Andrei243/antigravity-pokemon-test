using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- Ball Capsules, Seals and ribbons (plan 06 · R17b)

    // The Seal Case's house in Solaceon Town and its two givers, the Sunyshore Market with the Seal merchant's stall and
    // the woman of the Effort Ribbon, the PC's question and the Ball Capsules' screen step by step (a capsule chosen,
    // its menu, Seals taken, carried, stuck on and put back, a capsule kept, put on a Pokémon's ball, two changing
    // places), the Seals bursting as that Pokémon is sent out and as a trainer's is, Julia's and Dr. Footstep's houses
    // and their ribbons, the summary's page of ribbons, the Fashion Case with its backdrops, a winner's photograph in
    // the Contest Hall and the Trainer Card's back (bc*; not part of "all"). Everything goes through the game's own
    // TryInteract, scripts and the screens' own buttons; it prints what each left behind.
    public void CapsulesMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var team = game.Party;
        var seals = game.SealCase;
        var capsules = game.BallCapsules;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        // Reads every line on to the end of the script, shooting the line that holds a word; a question is answered YES
        void ReadOn(string? word = null, string? shot = null, int answer = 0)
        {
            for (int i = 0; i < 60 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue); i++)
            {
                if (engine.Choice.IsOpen)
                {
                    Frames(14);
                    if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                Whole();
                if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                Next();
            }
            Until(() => game.State != GameState.Dialogue && !Box().IsActive, "the text gone");
        }
        // Waits for a capsule tried out on its little stage to be in the middle of its burst
        void MidBurst(Func<float> age, float at = 0.75f)
        {
            float loop = PokemonPlatinumEngine.Graphics.SealFx.Length + PokemonPlatinumEngine.Graphics.SealFx.Pause;
            for (int i = 0; i < 400 && Math.Abs(age() % loop - at) > 0.02f; i++) Frames(1);
        }
        string Loose() => string.Join(", ", seals.Owned.Select(s => $"{SealDatabase.Name(s)} ×{seals.Count(s)}"));

        // ---- Solaceon Town's east house: the Seal Case and its first five Seals, then the boy's Seals for an Unown
        At("Sinnoh", 581, 657, Direction.Up);
        game.LocationSign.Hide();
        Frames(4);
        Shot("bc01_solaceon_door");
        At("SolaceonTownEastHouse", 4, 7, Direction.Up);
        Frames(6);
        Shot("bc02_seal_house");
        At("SolaceonTownEastHouse", 3, 4, Direction.Left);
        game.Player.SetPosition(2, 4, Direction.Down);
        game.Interact(); Frames(3);
        ReadOn("Seal Case", "bc03_mother");
        Console.WriteLine($"  the Seal Case: {inventory.GetQuantity(ItemDatabase.Get("Seal Case")!)}; {seals.Total} Seals: {Loose()}");
        var unown = Meet("Unown-R", 20);
        team.Add(unown);
        game.Player.SetPosition(5, 4, Direction.Down);
        game.Interact(); Frames(3);
        ReadOn("looks just like", "bc04_boy_unown");
        Console.WriteLine($"  after the boy: {Loose()}");
        game.Player.SetPosition(5, 4, Direction.Down);
        game.Interact(); Frames(3);
        ReadOn();
        Console.WriteLine($"  shown the same Unown again: {seals.Anywhere(CapsuleRules.UnownSeal(unown))} of its Seal");
        team.Members.Remove(unown);

        // ---- The Sunyshore Market: the merchant's stall of the day, and the Effort Ribbon
        At("Sinnoh", 849, 763, Direction.Up);
        game.LocationSign.Hide();
        Frames(4);
        Shot("bc05_market_door");
        At("SunyshoreMarket", 5, 11, Direction.Up);
        Frames(6);
        Shot("bc06_market");
        game.Money = 5000;
        game.Player.SetPosition(2, 5, Direction.Left);
        game.Interact(); Frames(3);
        Whole(); Next();
        Until(() => game.State == GameState.SealShop, "the Seal merchant's stall");
        Frames(40);
        var shop = game.SealShop;
        Shot("bc07_seal_shop");
        Console.WriteLine($"  {GameClock.Today.DayOfWeek}'s Seals: {string.Join(", ", shop.Stock.Select(s => $"{SealDatabase.Name(s)} {SealDatabase.Price(s)}"))}");
        shop.Move(0, 1, game.Money);
        shop.Move(0, 1, game.Money);
        MidBurst(() => shop.Age, 0.7f);
        Shot("bc08_seal_shop_burst");
        int money = game.Money;
        game.Money += shop.Confirm(game.Money, _ => { });
        shop.Move(1, 0, game.Money);
        shop.Move(1, 0, game.Money);
        Frames(20);
        Shot("bc09_how_many");
        game.Money += shop.Confirm(game.Money, _ => { });
        Console.WriteLine($"  bought {shop.Bought} for {money - game.Money}: {Loose()}");
        shop.Cancel();
        Until(() => game.State == GameState.Overworld, "the field again");
        ReadOn();

        var lead = team.Members[0];
        game.Player.SetPosition(3, 9, Direction.Left);
        game.Interact(); Frames(3);
        ReadOn("more effort", "bc10_effort_not_yet");
        lead.EvAttack = 252; lead.EvSpeed = 252; lead.EvHP = 6;
        game.Player.SetPosition(3, 9, Direction.Left);
        game.Interact(); Frames(3);
        ReadOn("received", "bc11_effort_ribbon");
        Console.WriteLine($"  {lead.DisplayName}'s ribbons: {string.Join(", ", lead.Ribbons)}");

        // ---- A PC: with a Seal Case it asks which to open
        foreach (string kind in new[] { "Heart Seal B", "Star Seal A", "Song Seal C", "Fire Seal A", "Party Seal B", "Flora Seal A", "Foamy Seal B", "Ele-Seal A", "Smoke Seal A", "Line Seal B", "Burst Seal", "Twinkle Seal", "Liquid Seal" })
            seals.Give(SealDatabase.Seal(kind), 6);
        foreach (char letter in "PIKA") seals.Give(SealDatabase.Seal($"{letter} Seal"), 4);
        seals.Give(SealDatabase.Seal("Shock Seal"), 4);
        At("SunyshorePokemonCenter", 9, 3, Direction.Up);
        Frames(6);
        game.Interact(); Frames(3);
        Whole(); Next();
        Until(() => engine.Choice.IsOpen, "the PC's question"); Frames(20);
        Shot("bc12_pc_question");
        engine.Choice.Move(1);
        engine.Choice.Confirm(); Frames(4);
        Until(() => game.State == GameState.SealCase, "the Ball Capsules");
        Frames(40);
        Shot("bc13_capsules_empty");

        // ---- The first capsule: its menu, Seals taken out of the case, carried and stuck on
        capsules.Confirm(); Frames(20);
        Shot("bc14_capsule_menu");
        capsules.Choose(CapsuleAction.Edit); Frames(20);
        Shot("bc15_edit");
        void Stick(string kind, int right, int up)
        {
            if (!capsules.PointAt(SealDatabase.Seal(kind))) { Console.WriteLine($"  !! no {kind} in the case"); return; }
            capsules.Confirm();
            capsules.Carry(right, up);
            capsules.Confirm();
            Frames(2);
        }
        Stick("Heart Seal B", -34, 22);
        // A Seal in hand, on the ball and then off it
        Stick("Star Seal A", 30, 26);
        capsules.PointAt(SealDatabase.Seal("Song Seal C"));
        capsules.Confirm();
        capsules.Carry(-4, -30);
        Frames(20);
        Shot("bc16_holding");
        capsules.Carry(70, 0);
        Frames(10);
        Shot("bc17_off_the_ball");
        capsules.Carry(-70, 0);
        capsules.Confirm();
        Stick("Fire Seal A", 0, 40);
        Stick("Party Seal B", -40, -20);
        Stick("Twinkle Seal", 40, -18);
        Frames(30);
        Shot("bc18_edit_placed");
        Console.WriteLine($"  on the ball: {string.Join(", ", capsules.Edit!.Seals.Select(s => $"{SealDatabase.Name(s.Seal)} at {s.X},{s.Y} ({s.Distance} dots, bursts after {CapsuleRules.BurstDelay(s)} frames)"))}");
        capsules.SwitchSide();
        capsules.Move(1, 0);
        Frames(12);
        Shot("bc19_on_the_ball");
        capsules.SwitchSide();
        MidBurst(() => capsules.Age, 0.95f);
        Shot("bc20_edit_burst");
        capsules.Cancel(); Frames(20);
        Shot("bc21_quit_question");
        capsules.Confirm(); Frames(4);
        capsules.Keep(); Frames(30);
        Shot("bc22_kept");

        // ---- Onto a Pokémon's ball
        capsules.Confirm();
        capsules.Choose(CapsuleAction.Attach); Frames(30);
        Shot("bc23_attach_team");
        capsules.Confirm(); Frames(4);
        MidBurst(() => capsules.Age, 1.1f);
        Shot("bc24_attached");
        Console.WriteLine($"  {lead.DisplayName}: capsule {lead.CapsuleId} with {lead.Capsule?.Seals.Count} Seals; kept {capsules.Kept}");

        // ---- A second capsule spelling a word, and the two changing places
        capsules.Move(1, 0);
        capsules.Confirm();
        capsules.Choose(CapsuleAction.Edit);
        int x = -45;
        foreach (string kind in new[] { "P Seal", "I Seal", "K Seal", "A Seal", "Shock Seal" })
        {
            Stick(kind, x, 6);
            x += 22;
        }
        MidBurst(() => capsules.Age, 0.8f);
        Shot("bc25_letters");
        capsules.Keep(); Frames(10);
        capsules.Confirm();
        capsules.Choose(CapsuleAction.Swap);
        capsules.Move(-1, 0); Frames(20);
        Shot("bc26_swap");
        capsules.Confirm(); Frames(40);
        Shot("bc27_swapped");
        Console.WriteLine($"  after the swap {lead.DisplayName} carries capsule {lead.CapsuleId}: {string.Join(" ", lead.Capsule!.Seals.Select(s => SealDatabase.Name(s.Seal)))}");
        capsules.Cancel();
        Until(() => game.State == GameState.Overworld, "the field again");
        ReadOn();
        Console.WriteLine($"  the Trainer Card's score: {game.TrainerScore}");

        // ---- In battle: the Seals burst as the Pokémon comes out of its ball
        var wild = StartBattle("Starly", 4);
        Skip(130 / 60.0);
        Confirm(wild);
        Skip(BattleAnimator.SendOutBallTime + 0.12);
        Frames(1); Shot("bc30_burst_1");
        Skip(0.25);
        Frames(1); Shot("bc31_burst_2");
        Skip(0.3);
        Frames(1); Shot("bc32_burst_3");
        Skip(0.4);
        Frames(1); Shot("bc33_burst_4");
        game.State = GameState.Overworld;

        // The word, put on the ball of the same Pokémon
        CapsuleRules.Attach(seals, 0, lead);
        wild = StartBattle("Starly", 4);
        Skip(130 / 60.0);
        Confirm(wild);
        Skip(BattleAnimator.SendOutBallTime + 0.55);
        Frames(1); Shot("bc34_burst_word");
        game.State = GameState.Overworld;

        // A trainer's Pokémon with a capsule of the original's own: Volkner's Electivire
        var volkner = TrainerDatabase.Get("leader_volkner")!;
        var electivire = TrainerDatabase.Build(volkner.Party.First(p => p.BallSeal != 0));
        var leader = new Trainer { Name = "Volkner", TrainerClass = "Leader", Look = "Gentleman" };
        leader.Party.Add(electivire);
        Console.WriteLine($"  Volkner's {electivire.DisplayName}: {string.Join(", ", electivire.Capsule!.Seals.Select(s => SealDatabase.Name(s.Seal)))}");
        var gym = StartBattle("", 0, leader);
        Skip(130 / 60.0);
        Confirm(gym);
        Skip(BattleAnimator.SendOutBallTime + 0.3);
        Frames(1); Shot("bc35_trainer_burst_1");
        Skip(0.35);
        Frames(1); Shot("bc36_trainer_burst_2");
        game.State = GameState.Overworld;
        game.Battle = null;

        // ---- Julia's house: the first visit, and on a later day a story and the day's ribbon
        At("SunyshoreCityNortheastHouse", 4, 7, Direction.Up);
        Frames(6);
        Shot("bc40_julia_house");
        game.Player.SetPosition(3, 6, Direction.Up);
        game.Interact(); Frames(3);
        ReadOn("tomorrow", "bc41_julia_first");
        game.Story.Unset("FLAG_DAILY_VISIT_TOMORROW_SUNYSHORE_CITY_NORTHEAST_HOUSE");
        game.Player.SetPosition(3, 6, Direction.Up);
        game.Interact(); Frames(3);
        ReadOn("received", "bc42_julia_ribbon");
        Console.WriteLine($"  {GameClock.Today.DayOfWeek}: {string.Join(", ", lead.Ribbons)}");

        // ---- Dr. Footstep: a Pokémon whose heart is open
        At("Sinnoh", 663, 835, Direction.Up);
        game.LocationSign.Hide();
        Frames(4);
        Shot("bc43_footstep_door");
        At("FootstepHouse", 4, 7, Direction.Up);
        Frames(6);
        Shot("bc44_footstep_house");
        lead.Friendship = 255;
        game.Player.SetPosition(5, 7, Direction.Up);
        game.Interact(); Frames(3);
        ReadOn();
        // The doctor reads the prints behind a fade: the lines come back after it
        Until(() => Box().IsActive, "the doctor's reading");
        ReadOn("received", "bc45_footprint_ribbon");
        Console.WriteLine($"  after Dr. Footstep: {string.Join(", ", lead.Ribbons)}");

        // ---- The summary's page of ribbons
        foreach (string ribbon in new[] { "RIBBON_COOL", "RIBBON_COOL_GREAT", "RIBBON_BEAUTY", "RIBBON_CUTE_MASTER", RibbonRules.SinnohChamp }) RibbonRules.Give(lead, ribbon);
        game.ChooseFromStartMenu(StartMenuChoice.Pokemon);
        Frames(4);
        game.PartyScreen.Confirm(team);
        game.PartyScreen.Confirm(team);
        while (game.PartyScreen.SummaryPage != PartyScreen.RibbonsPage) game.PartyScreen.TurnSummaryPage(1);
        Frames(20);
        Shot("bc50_ribbons_page");
        game.PartyScreen.SelectRibbons(lead);
        game.PartyScreen.MoveRibbon(1, 0, lead.Ribbons.Count);
        Frames(8);
        Shot("bc51_ribbon_looked_at");
        // Every ribbon there is, to see each sign
        foreach (var ribbon in RibbonDatabase.All) RibbonRules.Give(lead, ribbon.Id);
        game.PartyScreen.LeaveRibbons();
        game.PartyScreen.SelectRibbons(lead);
        for (int page = 0; page < 5; page++)
        {
            Frames(4);
            Shot($"bc52_every_ribbon_{page + 1}");
            for (int row = 0; row < 3; row++) game.PartyScreen.MoveRibbon(0, 1, lead.Ribbons.Count);
        }
        game.PartyScreen.LeaveRibbons();
        game.PartyScreen.Close();
        game.State = GameState.Overworld;
        Console.WriteLine($"  the team shows {RibbonRules.CountPartyRibbons(team)} different ribbons");

        // ---- The Fashion Case: what Jubilife TV's man gives, the accessories, every backdrop
        var fashion = game.FashionCase;
        fashion.FillAsGiven(new Random(11));
        Console.WriteLine($"  the Fashion Case as given: {fashion.Total} accessories, backdrops {string.Join(", ", fashion.Backdrops.Select(SuperContestDatabase.BackdropName))}");
        game.OpenFashionCase();
        Frames(40);
        Shot("bc60_fashion_accessories");
        var viewer = game.FashionCaseScreen;
        viewer.MoveTab(1);
        Frames(20);
        Shot("bc61_fashion_backdrops");
        for (int b = 0; b < SuperContestDatabase.Backdrops.Count; b++) fashion.AddBackdrop(b);
        for (int page = 0; page < 6; page++)
        {
            for (int k = 0; k < (page == 0 ? 0 : 3); k++) viewer.Move(0, 1);
            Frames(6);
            Shot($"bc62_backdrops_{page + 1}");
        }
        viewer.Cancel();
        Until(() => game.State == GameState.Overworld, "the field again");

        // ---- A winner's photograph in the Contest Hall's frame, and the card's back
        var outcome = ContestAwards.Conclude(lead, ContestStat.Cool, ContestRank.Ultra, ContestMode.Official, 0, fashion);
        game.ContestRecords.Count(outcome, lead, fashion.Held.Take(6).Select(h => h.Accessory), PlayerIdentity.Name, GameClock.Today);
        At("ContestHallLobby", 2, 12, Direction.Up);
        Frames(6);
        game.Interact(); Frames(3);
        Until(() => game.State == GameState.FashionCase, "the photograph");
        Frames(40);
        Shot("bc63_contest_photo");
        viewer.Confirm();
        Until(() => game.State == GameState.Overworld, "the field again");
        ReadOn();
        game.Player.SetPosition(4, 12, Direction.Up);
        game.Interact(); Frames(3);
        ReadOn("photo frame", "bc64_empty_frame");
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        game.TrainerCard.Flip();
        Frames(60);
        Shot("bc65_card_back");
        game.TrainerCard.Close();
        game.State = GameState.Overworld;
        Console.WriteLine($"  contests: {game.ContestRecords.Entered} entered, {game.ContestRecords.Won} won, {game.ContestRecords.Ribbons} ribbons");
    }
}
