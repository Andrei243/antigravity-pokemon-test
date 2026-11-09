using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// TR6, <c>breeding</c> (tr50 to tr52): the modern Day Care's Egg. A Bidoof mother with her hidden ability, in a
    /// Premier Ball, and a father: Eggs are made by <c>Breeding.MakeEgg</c> under the modern rules until one carries the
    /// hidden ability, hatched, and its summary shown (tr50 the skills page, tr50b the trainer memo); the hatched Pokémon
    /// sent out of its mother's ball in a battle (tr51); then Route 3's Day Care in Unova (the room, tr52a; the lady
    /// taking the pair; the man's question when the couple found an Egg, tr52; the Egg received, tr52b). It prints the
    /// rate of the hidden ability over many seeds and the ball it came in.
    /// </summary>
    void TrainingBreeding()
    {
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        Pokemon? pick = null;
        void ReadOn(string? word = null, string? shot = null, params int[] answers)
        {
            int asked = 0;
            for (int i = 0; i < 60 && (Box().IsActive || engine.Choice.IsOpen || game.State is GameState.Dialogue or GameState.PartyMenu); i++)
            {
                if (game.State == GameState.PartyMenu)
                {
                    if (pick != null) game.PartyScreen.SelectedIndex = game.Party.Members.IndexOf(pick);
                    game.PartyScreen.Confirm(game.Party); Frames(3);
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

        Console.WriteLine($"breeding: rules in force {(Ruleset.Current.ModernBreeding ? "modern breeding" : "Platinum's")}");

        // ---- The pair and the Egg: Bidoof's hidden ability is Moxie
        var bidoof = PokemonDatabase.Get("Bidoof")!;
        string hiddenName = bidoof.HiddenAbility ?? "";
        var mother = new Pokemon(bidoof, 32, new Random(11)) { Gender = Gender.Female, Ball = "Premier Ball" };
        mother.AbilityName = hiddenName;
        var father = new Pokemon(bidoof, 30, new Random(12)) { Gender = Gender.Male, Ball = "Poké Ball" };
        Console.WriteLine($"  the mother: {mother.AbilityName} (hidden: {mother.AbilityName == mother.HiddenAbilityName}), ball {mother.Ball}; the father: {father.AbilityName}");

        // A hundred and fifty Eggs for the rate (the rule's 60 in 100 for a hidden mother), the first with the hidden ability kept
        int hiddenCount = 0, total = 150;
        Pokemon? egg = null;
        for (int seed = 0; seed < total; seed++)
        {
            var made = Breeding.MakeEgg(mother, father, 0x1F2E3D4Cu + (uint)seed * 77, (uint)PlayerIdentity.Number, new Random(1000 + seed), Ruleset.Current);
            if (made.AbilityName == made.HiddenAbilityName) { hiddenCount++; egg ??= made; }
        }
        Console.WriteLine($"  {total} Eggs from a hidden-ability mother: {hiddenCount} with the hidden ability ({hiddenCount * 100 / total} in 100; the rule says 60), ball {egg?.Ball}");
        if (egg == null) return;

        // ---- Hatched (the hatching scene itself is `daycare`'s); its summary on the skills page and the memo
        party.Clear();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 20));
        egg.EggCycles = 0;
        Breeding.Hatch(egg, "Route 3's Day Care", GameClock.Today);
        party.Members.Insert(0, egg);
        game.Pokedex.RegisterSeen(egg.Species.DexNumber);
        game.Pokedex.RegisterCaught(egg.Species.DexNumber);
        Console.WriteLine($"  hatched: {egg.DisplayName}, ability {egg.AbilityName} (hidden {egg.AbilityName == egg.HiddenAbilityName}), regular {string.Join("/", egg.Abilities)}, ball {egg.Ball}, moves {string.Join(", ", egg.Moves.Select(m => m.Name))}");
        var screen = game.PartyScreen;
        game.State = GameState.PartyMenu;
        screen.Open();
        screen.SelectedIndex = 0;
        screen.ShowSummary = true;
        while (screen.SummaryPage != PartyScreen.SkillsPage) screen.TurnSummaryPage(1);
        Frames(40); Shot("tr50_hatched_hidden");
        while (screen.SummaryPage != PartyScreen.MemoPage) screen.TurnSummaryPage(1);
        Frames(40); Shot("tr50b_hatched_memo");
        screen.ShowSummary = false;
        game.State = GameState.Overworld;
        Frames(4);

        // ---- Sent out of its mother's ball
        party.HealAll();
        var b = StartBattle("Bidoof", 12, chance: new Random(9));
        ToMainMenu(b);
        b.Anim.SendOut(BattleSide.Player, egg);
        Console.WriteLine($"  sent out of: {b.Anim[BattleSide.Player, 0].Ball} (the Pokémon's own: {BattleBall.NameFor(egg)})");
        Skip(0.12); Shot("tr51a_egg_ball_flight");
        Skip(0.12); Shot("tr51b_egg_ball_landing");
        Skip(0.12); Shot("tr51_egg_ball");              // the lid opening
        Skip(0.1); Shot("tr51c_egg_ball_released");
        Skip(1.0);
        game.State = GameState.Overworld;
        Frames(4);

        // ---- Unova's Day Care on Route 3: the pair left with the lady, the Egg handed over by the man
        engine.NewGameRegion = RegionDatabase.Unova;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        var team = game.Party;
        var dayCare = game.DayCare;
        var m2 = new Pokemon(bidoof, 32, new Random(11)) { Gender = Gender.Female, Ball = "Premier Ball" };
        m2.AbilityName = hiddenName;
        var f2 = new Pokemon(bidoof, 30, new Random(12)) { Gender = Gender.Male };
        // (the Lady will not take one that leaves the player with fewer than two able to battle)
        team.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 20));
        team.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 20));
        team.Add(m2);
        team.Add(f2);
        var room = AreaSpot("unova_route_3_3").Map;
        Console.WriteLine($"  Unova's Day Care: {room.Name}, people {string.Join(", ", room.NPCs.Select(n => $"{n.Key}:{n.Name}@{n.GridX},{n.GridY}"))}");
        bool Beside(string key)
        {
            if (room.NPCs.FirstOrDefault(n => n.Key == key) is not { } npc)
            {
                Console.WriteLine($"  !! nobody called {key} in {room.Name}");
                return false;
            }
            // (the Lady sits in a nook whose walls hide anyone standing beside her: from the north, the player is seen)
            (int dx, int dy, Direction facing)[] sides = { (0, -1, Direction.Down), (1, 0, Direction.Left), (-1, 0, Direction.Right), (0, 1, Direction.Up) };
            var side = sides.First(s => room.IsWalkable(npc.GridX + s.dx, npc.GridY + s.dy) && room.GetNpcAt(npc.GridX + s.dx, npc.GridY + s.dy) == null);
            At(room.Name, npc.GridX + side.dx, npc.GridY + side.dy, side.facing);
            game.LocationSign.Hide();
            Frames(30);
            return true;
        }
        At(room.Name, 9, 9, Direction.Up);
        game.LocationSign.Hide();
        Frames(30);
        Shot("tr52a_unova_day_care_room");
        if (Beside("2"))
        {
            game.Interact(); Frames(3);
            Whole();
            Until(() => engine.Choice.IsOpen, "the lady's question"); Frames(18);
            Shot("tr52c_unova_lady_question");
            engine.Choice.Confirm(); Frames(4);
            Whole(); Next();
            Until(() => game.State == GameState.PartyMenu, "the team to choose from");
            game.PartyScreen.SelectedIndex = team.Members.IndexOf(m2);
            Frames(20);
            Shot("tr52d_unova_choose_pokemon");
            game.PartyScreen.Confirm(team); Frames(3);
            // Yes to another: the father, and with two left there is nothing more to ask
            pick = f2;
            ReadOn("raise your", "tr52e_unova_lady_second", 0);
            if (dayCare.Count < 2)
            {
                Beside("2");
                pick = f2;
                game.Interact(); Frames(3);
                ReadOn(null, null, 0);
            }
        }
        Console.WriteLine($"  the Day Care: {dayCare.State}, {string.Join(", ", dayCare.Left.Select(p => $"{p.DisplayName} {p.Gender}"))}; they get on {dayCare.Compatibility}");
        int steps = 0;
        while (!dayCare.HasEgg && steps < 60000) { game.DayCareStep(); steps++; }
        Console.WriteLine($"  an Egg after {steps} steps: {dayCare.HasEgg}");
        if (Beside("1"))
        {
            game.Interact(); Frames(3);
            Whole();
            Until(() => engine.Choice.IsOpen, "the Man's question"); Frames(18);
            Shot("tr52_unova_day_care");
            engine.Choice.Confirm(); Frames(4);
            ReadOn();
        }
        var got = team.Members.FirstOrDefault(p => p.IsEgg);
        Console.WriteLine($"  the Egg: {got?.Species.Name}, ability {got?.AbilityName}, ball {got?.Ball}, moves {string.Join(", ", got?.Moves.Select(m => m.Name) ?? Array.Empty<string>())}");
    }
}
