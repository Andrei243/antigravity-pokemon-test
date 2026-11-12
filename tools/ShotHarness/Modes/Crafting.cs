using System;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- materials, crafting and cooking (plan 39 · CR1)

    // Sunyshore City's workshop and the kit (cr*; not part of "all"): the way in at the dead end of the alley shut before
    // the Hall of Fame (cr01) and open after it (cr02), the workshop from its door (cr03), the craftswoman giving the kit
    // through her own script (cr04), a Bidoof felled on Route 201 and the toast of what it left (cr05) and the kit looked
    // into (cr06). Everything goes through the game's own steps, scripts and screens; it prints what each left behind
    // (the kit and its flag, the pouch, the toast).
    public void CraftingMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }

        // ---- the way in, shut: the player stands before the dead end with the Hall of Fame not yet entered
        At("Sinnoh", 870, 763, Direction.Right);
        game.LocationSign.Hide();
        Frames(20);
        Shot("cr01_workshop_door_shut");
        var door = game.Map.Warps.First(w => w.TargetMap == "SunyshoreWorkshop");
        Console.WriteLine($"door: {door.SourceX},{door.SourceY} opened by {door.OpenedBy}, solid {game.Map.IsSolid(door.SourceX, door.SourceY)}");

        // ---- and open, once the Hall of Fame has been entered (the story's flag, as its scene sets it)
        game.Story.Set("FLAG_GAME_COMPLETED");
        Frames(20);
        Shot("cr02_workshop_door_open");
        Console.WriteLine($"door: solid {game.Map.IsSolid(door.SourceX, door.SourceY)}");

        // ---- inside
        At("SunyshoreWorkshop", 6, 8, Direction.Up);
        game.LocationSign.Hide();
        Frames(20);
        Shot("cr03_workshop");

        // ---- the craftswoman: her lines read to the one that gives the kit
        At("SunyshoreWorkshop", 6, 4, Direction.Up);
        game.LocationSign.Hide();
        game.Interact(); Frames(3);
        bool shot = false;
        for (int i = 0; i < 40 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue); i++)
        {
            if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(4); continue; }
            Until(() => Box().IsActive, "text on the screen");
            Box().FinishLine(); Frames(2);
            if (!shot && Box().VisibleText.Contains("received")) { Shot("cr04_kit_given"); shot = true; }
            Box().Advance(); Frames(2);
        }
        Until(() => game.State == GameState.Overworld && !Box().IsActive, "the field again");
        var kit = ItemDatabase.Get(GameEngine.CraftingKitItem)!;
        Console.WriteLine($"kit: {game.Bag.GetQuantity(kit)} in the bag, flag {game.Story.Has(GameEngine.CraftingKitFlag)}");

        // ---- a Bidoof felled on Route 201 leaves its material, and the field says so once it is back
        game.Crafting.Pouch.Clear();
        At("Sinnoh", 116, 886, Direction.Right);
        game.LocationSign.Hide();
        game.Party.Clear();
        var lead = new Pokemon(PokemonDatabase.Get("Turtwig")!, 60);
        game.Party.Add(lead);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 3, MaxLevel = 3, Weight = 100 });
        Until(() => game.State == GameState.Battle, "the battle");
        var battle = game.Battle!;
        int hit = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status);
        for (int guard = 0; guard < 400 && !battle.IsBattleOver; guard++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Main) { battle.SelectMove(hit); Frames(1); }
            else Confirm(battle);
        }
        Until(() => game.State == GameState.Overworld, "the field after the battle");
        Frames(40);
        Shot("cr05_left_behind");
        Console.WriteLine($"toast: {(game.Toast.Visible ? game.Toast.Message : "(none)")}; pouch: {string.Join(", ", game.Crafting.Pouch.Held.Select(m => $"{m.Id} {game.Crafting.Pouch.Count(m.Id)}"))}");

        // ---- the kit, with a few things in it
        game.Crafting.Pouch.Add("emberCore", 2);
        game.Crafting.Pouch.Add("tidePearl", 1);
        game.Crafting.Pouch.Add("quarryStone", 14);
        game.Crafting.Pouch.Add("looseFeather", 6);
        game.OpenCrafting();
        Frames(30);
        Shot("cr06_pouch");
        Console.WriteLine($"kit screen: {game.CraftingScreen.Selected?.Id}, {game.CraftingScreen.Pouch.Kinds} kinds");
        game.CraftingScreen.Cancel();
        Frames(4);

        CraftingMakePart(Until);
        CraftingTmMakerPart(Until);
        CraftingKitchenPart(Until);
    }

    // Reads a script's talk through the game's own box: choice answered by its index (a number in `answers`, in the order
    // the questions come), and a shot of a line the moment its text holds the word asked for
    void CraftingTalk(Action<Func<bool>, string, int> until, int[] answers, params (string Word, string Name)[] shots)
    {
        int asked = 0;
        var taken = new HashSet<string>();
        for (int i = 0; i < 60 && (game.Dialogue.IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue); i++)
        {
            if (engine.Choice.IsOpen)
            {
                until(() => engine.Choice.Shown >= 0.99f, "the question's answers", 120);
                int answer = asked < answers.Length ? answers[asked] : 0;
                asked++;
                for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                engine.Choice.Confirm(); Frames(6);
                continue;
            }
            until(() => game.Dialogue.IsActive || game.State != GameState.Dialogue, "text on the screen", 120);
            if (game.State == GameState.Crafting || !game.Dialogue.IsActive) break;
            game.Dialogue.FinishLine(); Frames(2);
            foreach (var (word, name) in shots)
                if (!taken.Contains(name) && game.Dialogue.VisibleText.Contains(word)) { Frames(30); Shot(name); taken.Add(name); }
            game.Dialogue.Advance(); Frames(2);
        }
    }

    // ---------------------------------------------------------------- the kit's MAKE tab (plan 39 · CR3)

    // The kit from its bag: the MAKE list with what is held against what each recipe takes (cr20), the Feather Ball's
    // recipe (cr21), "how many?" (cr22), a few made (cr23), the ball thrown at a Starly and caught (cr24) and the
    // apprentice pressing a rare material out of three common ones (cr25).
    void CraftingMakePart(Action<Func<bool>, string, int> until)
    {
        game.Crafting.Pouch.Clear();
        game.State = GameState.Overworld;
        foreach (var (id, count) in new[] { ("looseFeather", 20), ("leafScrap", 14), ("softFluff", 15), ("ironFilings", 12), ("rockFlake", 9), ("grittySand", 7), ("galePlume", 2) })
            game.Crafting.Pouch.Add(id, count);
        game.Money = 3000;
        game.OpenCrafting();
        var screen = game.CraftingScreen;
        for (int i = 0; i < 4 && screen.Tab != CraftingTab.Make; i++) screen.ChangeTab(1);
        Frames(30);
        Shot("cr20_make_list");
        Console.WriteLine($"make: tab {screen.Tab}, {screen.Recipes.Count} recipes, tabs {string.Join("/", screen.Tabs)}");
        int feather = screen.Recipes.ToList().FindIndex(r => r.Id == "featherBall");
        for (int k = 0; k < feather; k++) screen.Move(1);
        Frames(8);
        Shot("cr21_recipe");
        Console.WriteLine($"recipe: {screen.SelectedRecipe?.Id}, most {screen.MostNow}");
        screen.Confirm();
        screen.ChangeAmount(1);
        Frames(8);
        Shot("cr22_how_many");
        Console.WriteLine($"how many: {screen.HowMany} of at most {screen.MostNow}");
        screen.Confirm();
        Frames(12);
        Shot("cr23_made");
        var featherBall = ItemDatabase.Get("Feather Ball")!;
        Console.WriteLine($"made: {screen.Notice}; Feather Balls in the bag {game.Bag.GetQuantity(featherBall)}; feathers left {game.Crafting.Pouch.Count("looseFeather")}");
        screen.Cancel();
        Frames(6);

        // A Feather Ball thrown at a Starly (a Flying type: the ball's own rule is R28a's)
        game.State = GameState.Overworld;
        At("Sinnoh", 116, 886, Direction.Right);
        game.LocationSign.Hide();
        game.Party.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 60));
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 4, MaxLevel = 4, Weight = 100 });
        until(() => game.State == GameState.Battle, "the battle", 900);
        var b = game.Battle!;
        ToMainMenu(b);
        b.EnemySlots[0].Pokemon!.CurrentHP = 1;
        bool caught = false;
        for (int throws = 0; throws < 6 && !b.IsBattleOver && !caught; throws++)
        {
            if (game.Bag.GetQuantity(featherBall) == 0) break;
            b.UseBagItem("Feather Ball");
            for (int i = 0; i < 60 && !caught && !b.IsBattleOver; i++)
            {
                Skip(0.4);
                string line = b.CurrentMessage ?? "";
                if (line.Contains("Gotcha") || line.Contains("was caught")) { Skip(0.3); Shot("cr24_feather_ball_caught"); caught = true; break; }
                if (b.HUD.MenuState == BattleMenuState.Main) break;
                Confirm(b);
            }
        }
        Console.WriteLine($"feather ball: caught {caught}; battle over {b.IsBattleOver}; balls left {game.Bag.GetQuantity(featherBall)}");
        for (int guard = 0; guard < 60 && game.State == GameState.Battle; guard++) { Confirm(b); Skip(0.3); }
        until(() => game.State == GameState.Overworld, "the field after the catch", 900);
        Frames(220);   // the toast of what the Starly left lifts away first

        // The apprentice: three of a type's common material for one of its rare
        game.Crafting.Pouch.Clear();
        game.Crafting.Pouch.Add("softFluff", 7);
        At("SunyshoreWorkshop", 2, 6, Direction.Up);
        game.LocationSign.Hide();
        Frames(20);
        game.Interact(); Frames(3);
        CraftingTalk(until, new[] { 0 }, ("Shall I have a go", "cr25_apprentice"));
        until(() => game.State == GameState.Overworld && !game.Dialogue.IsActive, "the field again", 300);
        Console.WriteLine($"apprentice: soft fluff {game.Crafting.Pouch.Count("softFluff")}, rare {string.Join(", ", game.Crafting.Pouch.Held.Where(m => m.Id != "softFluff").Select(m => $"{m.Id} {game.Crafting.Pouch.Count(m.Id)}"))}");
    }

    // ---------------------------------------------------------------- cooking at the camp (plan 39 · CR6)

    // The pot at the camp: the pantry with some in the pot (cr50), the stirring (cr51), the dish served (cr52), the team
    // eating (cr53) and the camp afterwards with the power's line (cr54); and the workshop's pantry counter (cr55).
    void CraftingKitchenPart(Action<Func<bool>, string, int> until)
    {
        game.State = GameState.Overworld;
        game.Crafting.Pouch.Clear();
        foreach (var (id, count) in new[] { ("beans", 3), ("onions", 2), ("potatoes", 2), ("rice", 2), ("flour", 1) })
            game.Crafting.Pouch.Add(id, count);
        game.Bag.AddItem(ItemDatabase.Get("Pecha Berry")!, 2);
        game.Party.Clear();
        foreach (var (species, level, hp, status) in new[]
        {
            ("Turtwig", 18, 0.45f, StatusCondition.None), ("Starly", 14, 0.2f, StatusCondition.Poison), ("Shinx", 15, 0.6f, StatusCondition.None),
            ("Bidoof", 12, 0.3f, StatusCondition.Burn), ("Buneary", 13, 1f, StatusCondition.None),
        })
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            p.CurrentHP = Math.Max(1, (int)(p.MaxHP * hp));
            p.Status = status;
            game.Party.Add(p);
        }
        At("Sinnoh", 116, 886, Direction.Right);
        game.LocationSign.Hide();
        Frames(10);
        Console.WriteLine($"camp offered: {engine.Drive.HasCamp}");
        engine.Drive.ChooseFromStartMenu(StartMenuChoice.Camp);
        Frames(40);
        var camp = game.Camp;
        Shot("cr50_camp_team");
        camp.AskToCook();
        Frames(8);
        Console.WriteLine($"pantry: {string.Join(", ", camp.OnHand.Select(i => $"{i.Id}{(i.IsMaterial ? "" : "*")}"))}");
        foreach (var id in new[] { "beans", "beans", "onions", "potatoes" })
        {
            int at = camp.OnHand.FindIndex(i => i.Id == id);
            while (camp.PantryCursor != at) camp.PantryMove(1);
            camp.PantryConfirm();
        }
        Frames(10);
        Shot("cr50_camp_pot");
        Console.WriteLine($"pot: {string.Join(", ", camp.Pot.Select(i => i.Id))} makes {Meals.NameOf(camp.Expected)}");
        while (!camp.OnCook) camp.PantryMove(1);
        camp.PantryConfirm();
        Frames(6);
        bool shotStir = false;
        for (int guard = 0; guard < 1500 && camp.Phase == CampPhase.Stirring; guard++)
        {
            if (camp.Stirring is { } pot)
            {
                if (pot.WindowOpen) camp.Stir();
                if (!shotStir && pot.Round >= 2 && pot.RoundFrame >= 14) { Frames(1); Shot("cr51_stirring"); shotStir = true; Console.WriteLine($"stirring: round {pot.Round + 1}, scores {string.Join(",", pot.Scores)}"); }
            }
            Frames(1);
        }
        if (!shotStir) Console.WriteLine("  !! the stirring never reached a shot");
        until(() => camp.Phase == CampPhase.Served, "the dish served", 600);
        Frames(30);
        Shot("cr52_dish");
        var served = camp.Served;
        Console.WriteLine($"served: {(served == null ? "(none)" : $"{Meals.NameOf(served.Dish)} {served.Grade} healed {served.Healed} cured {served.Cured} fed {served.Fed} power {served.Power?.Kind} L{served.Power?.Level} {served.Power?.SecondsLeft}s")}");
        camp.ServedConfirm();
        Frames(24);
        Shot("cr53_team_eats");
        Console.WriteLine($"team: {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.CurrentHP}/{p.MaxHP} {p.Status}"))}");
        Frames(90);
        Shot("cr54_power");
        Console.WriteLine($"powers: {string.Join(", ", game.Powers.InForce.Select(p => $"{p.Kind} L{p.Level} {p.SecondsLeft:0}s"))}; note: {camp.Note}");
        camp.Cancel();
        Frames(10);

        // The workshop's counter: the craftswoman's THE PANTRY (the second of her answers)
        game.State = GameState.Overworld;
        game.Money = 2000;
        game.Crafting.Pouch.Clear();
        At("SunyshoreWorkshop", 6, 4, Direction.Up);
        game.LocationSign.Hide();
        Frames(20);
        game.Interact(); Frames(3);
        CraftingTalk(until, new[] { 1 });
        until(() => game.State == GameState.Crafting, "the pantry counter", 300);
        var screen = game.CraftingScreen;
        Frames(30);
        Shot("cr55_pantry");
        Console.WriteLine($"pantry counter: tab {screen.Tab}, {screen.Pantry.Count} goods, money {screen.Money}");
        screen.Confirm();
        screen.ChangeAmount(2);
        Frames(8);
        Shot("cr55b_pantry_how_many");
        screen.Confirm();
        Frames(12);
        Shot("cr55c_pantry_bought");
        Console.WriteLine($"bought: {screen.Notice}; pouch {string.Join(", ", game.Crafting.Pouch.Held.Select(m => $"{m.Id} {game.Crafting.Pouch.Count(m.Id)}"))}; money {game.Money - screen.TakeSpent()}");
        screen.Cancel();
        Frames(10);
    }

    // ---------------------------------------------------------------- the TM maker (plan 39 · CR4)

    // A Pokémon Center's PC with the kit in the bag: the menu with TM MAKER among its answers (cr30), the maker's list of
    // every machine the player has carried (cr31), the cost of a strong one against what is held (cr32) and a machine made
    // (cr33). Through the game's own TryInteract, script and screen; it prints the bag, the pouch and the money after.
    void CraftingTmMakerPart(Action<Func<bool>, string, int> until)
    {
        game.Crafting.Pouch.Clear();
        game.State = GameState.Overworld;
        foreach (var name in new[] { "TM27", "TM13", "TM01", "TM68", "TM06" })
            if (ItemDatabase.Get(name) is { } machine) game.Bag.AddItem(machine, 1);
        // Some used up, as they are: the maker remembers them
        foreach (var name in new[] { "TM27", "TM13", "TM06" })
            if (ItemDatabase.Get(name) is { } machine) game.Bag.RemoveItem(machine, 1);
        var cost = TmMaker.CostOf(ItemDatabase.Get("TM27")!)!;
        var big = TmMaker.CostOf(ItemDatabase.Get("TM68")!)!;
        game.Crafting.Pouch.Add(cost.CommonId, cost.Common + 1);
        game.Crafting.Pouch.Add(big.CommonId, big.Common);
        game.Money = 5000;

        var center = MapDatabase.Get("PokemonCenter");
        var terminal = center.NPCs.First(n => n.IsPCTerminal);
        At("PokemonCenter", terminal.GridX, terminal.GridY + 1, Direction.Up);
        game.LocationSign.Hide();
        game.Interact(); Frames(3);
        bool menuShot = false;
        for (int i = 0; i < 40 && game.State != GameState.Crafting; i++)
        {
            if (engine.Choice.IsOpen)
            {
                until(() => engine.Choice.Shown >= 0.99f, "the PC's menu", 120);
                if (!menuShot && engine.Choice.Options.Contains("TM MAKER")) { Shot("cr30_pc_menu"); menuShot = true; }
                Console.WriteLine($"pc menu: {string.Join(" / ", engine.Choice.Options)}");
                int maker = engine.Choice.Options.ToList().IndexOf("TM MAKER");
                for (int k = 0; k < maker; k++) engine.Choice.Move(1);
                engine.Choice.Confirm(); Frames(6);
                continue;
            }
            if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
            Frames(4);
        }
        until(() => game.State == GameState.Crafting, "the TM maker", 120);
        var screen = game.CraftingScreen;
        Frames(30);
        Shot("cr31_tm_list");
        Console.WriteLine($"maker: {string.Join(", ", screen.Machines.Select(m => m.Name))}");
        // The strong one: Hyper Beam, its common and its rare material against what is held
        int beam = screen.Machines.ToList().FindIndex(m => m.Name == "TM68");
        for (int k = 0; k < beam; k++) screen.Move(1);
        Frames(6);
        Shot("cr32_tm_cost");
        // And one made: Return
        while (screen.SelectedMachine?.Name != "TM27") screen.Move(-1);
        screen.Confirm();
        Frames(12);
        Shot("cr33_tm_made");
        Console.WriteLine($"made: {screen.Notice}; TM27 in the bag {game.Bag.GetQuantity(ItemDatabase.Get("TM27")!)}, money {game.Money} (screen {screen.Money})");
        screen.Cancel();
        Frames(10);
        until(() => !engine.ScriptRunning, "the PC switched off", 240);
    }
}
