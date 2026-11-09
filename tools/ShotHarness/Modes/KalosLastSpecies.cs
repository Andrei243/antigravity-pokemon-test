using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// Kalos's last species (plan 23, after Kalos's other four Gyms; <c>KALOS_FROM=lastspecies</c>, not part of the whole
    /// <c>kalos</c> mode), with plan 37 · FC5's restaurant shots: La Table à Trois's door, its table and host, a triple
    /// course at its menu, a rotation course of Le Carrousel at its menu, the dish by the turns and the second visit
    /// turned away (<c>fc50</c> to <c>fc55</c>); Lumiose's thirteen rooms opened last (<c>wk600</c> on), the Pokémon
    /// Village's water mouth (<c>wk615</c>), and the scenes (<c>wks400</c> on: the Glittering Cave's fossil seeker and his
    /// choice, a Jaw Fossil revived in Ambrette, the Battle Institute's test, and Diancie, Hoopa and Volcanion each at its
    /// beat and in battle). It wins every battle and prints what each scene left behind.
    /// </summary>
    public void KalosLastSpecies()
    {
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 900 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    if (fight.EnemyPokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    if (lead != null) lead.CurrentHP = lead.MaxHP;
                    fight.SelectMainMenuOption(0);
                    if (lead != null) foreach (var m in lead.Moves) m.CurrentPP = m.MaxPP;
                    int hurts = lead == null ? 0 : lead.Moves.Select((m, i) => (m, i)).Where(p => p.m.Data.Category != MoveCategory.Status && p.m.Data.Type != PokemonType.Ghost)
                        .OrderByDescending(p => p.m.Data.Power).Select(p => p.i).DefaultIfEmpty(0).First();
                    fight.SelectMove(hurts);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        void ToBattle()
        {
            for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
        }
        void ReadTo(string? text, int most = 2400, bool fight = true)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                idle = !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld ? idle + 1 : 0;
                if (text != null && idle > 90) break;
                if (text == null && idle > 30) return;
                if (State() == GameState.Battle && fight) { Win(); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void Talk(string map, int x, int y, Direction facing) { At(map, x, y, facing); Frames(2); game.Interact(); }
        void ComeThrough(string map, int x, int y, Direction facing) { At(map, x, y, facing); game.ArriveOnMap(); Frames(2); }
        int Held(string item) => ItemDatabase.Get(item) is { } data ? game.Bag.GetQuantity(data) : 0;
        void Left(string what) =>
            Console.WriteLine($"  {what}: map {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), jaw {Held("Jaw Fossil")}, sail {Held("Sail Fossil")}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, money {game.Money}, "
                + $"restaurant 2 {story.Has("FLAG_DAILY_KALOS_RESTAURANT_2")}, institute {story.Has("FLAG_DAILY_KALOS_BATTLE_INSTITUTE")} "
                + $"top {story.Has("FLAG_KALOS_BATTLE_INSTITUTE_TOP")}, Diancie gone {story.Has("FLAG_KALOS_DIANCIE_GONE")}, "
                + $"Hoopa gone {story.Has("FLAG_KALOS_HOOPA_GONE")}, Volcanion gone {story.Has("FLAG_KALOS_VOLCANION_GONE")}");

        // A game of Kalos past its Hall of Fame with the Mega Ring upgraded, and a Carbink for Diancie
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        foreach (var (species, level) in new[] { ("Greninja", 80), ("Talonflame", 78), ("Venusaur", 78), ("Carbink", 70) })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        foreach (string badge in RegionDatabase.Get(RegionDatabase.Kalos)!.BadgeNames) story.GiveBadge(RegionDatabase.Kalos, badge);
        story.SetVar("VAR_KALOS_STORY", 31);
        story.Set("KalosHallOfFame");
        story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 3);
        story.Set("FLAG_KALOS_TMV_PASS");
        story.SetVar("VAR_KALOS_POSTGAME", 3);
        story.Set("FLAG_KALOS_RIVAL_KILOUDE_BEATEN");
        story.Set("FLAG_KALOS_MEGA_RING_UPGRADED");
        story.Set("FLAG_KALOS_HIDE_SNORLAX_ROUTE_7");
        game.Money = 200000;
        Frames(2);
        Left("brought past the Hall of Fame");

        // ---- plan 37 · FC5: La Table à Trois's door and table, a triple course, Le Carrousel's rotation, the dish
        At("Kalos", 344, 349, Direction.Up); Frames(6); Shot("fc50_restaurant_door");
        ComeThrough("LumioseCity_TableATrois", 7, 10, Direction.Up); ReadTo(null); Frames(6);
        Talk("LumioseCity_TableATrois", 7, 5, Direction.Up);
        ReadTo("Will you take today's menu", fight: false); Frames(10); Shot("fc51_restaurant_table");
        ToBattle();
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("fc52_restaurant_triple"); Win(); }
        else Console.WriteLine("  !! the first course never came");
        ReadTo("Three courses in", fight: true); Frames(10); Shot("fc54_restaurant_dish");
        ReadTo(null);
        Left("La Table à Trois");
        Talk("LumioseCity_TableATrois", 7, 5, Direction.Up);
        ReadTo("You have eaten with us", fight: false); Frames(10); Shot("fc55_restaurant_second_visit");
        ReadTo(null);
        ComeThrough("LumioseCity_Carrousel", 7, 10, Direction.Up); ReadTo(null); Frames(6);
        Talk("LumioseCity_Carrousel", 7, 5, Direction.Up);
        ToBattle();
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("fc53_restaurant_rotation"); Win(); }
        else Console.WriteLine("  !! the rotation course never came");
        ReadTo(null);
        Left("Le Carrousel");

        // ---- the thirteen rooms opened last
        foreach (var (shot, map) in new[]
                 {
                     ("wk600_battle_institute", "LumioseCity_BattleInstitute"), ("wk601_pr_studio", "LumioseCity_PrStudio"),
                     ("wk602_cafe_introversion", "LumioseCity_CafeIntroversion"), ("wk603_cafe_ultimo", "LumioseCity_CafeUltimo"),
                     ("wk604_chapellerie", "LumioseCity_HatShop"), ("wk605_cafe_woof", "LumioseCity_CafeWoof"),
                     ("wk606_cafe_soleil", "LumioseCity_CafeSoleil"), ("wk607_shutterbug_cafe", "LumioseCity_ShutterbugCafe"),
                     ("wk608_cafe_kizahashi", "LumioseCity_CafeKizahashi"), ("wk609_coiffure_clips", "LumioseCity_CoiffureClips"),
                     ("wk610_vert_flat", "LumioseCity_VertApartment"), ("wk611_painter", "LumioseCity_PainterHouse"),
                     ("wk612_old_couple", "LumioseCity_OldCoupleHouse")
                 })
        {
            ComeThrough(map, 4, 8, Direction.Up);
            ReadTo(null);
            Frames(6);
            Shot(shot);
        }
        // The Pokémon Village's cave, its mouth now across the pond
        At("Kalos", 691, 108, Direction.Right); Frames(6); Shot("wk615_village_water_mouth");
        At("Kalos", 698, 108, Direction.Up); Frames(6); Shot("wk616_village_landing");

        // ---- the scenes
        // The Glittering Cave's fossil seeker: his choice, the jaw, and the sail on a later visit
        Talk("GlitteringCave", 11, 10, Direction.Up);
        ReadTo("Which would you like to take", fight: false); Frames(10); Shot("wks400_fossil_seeker");
        ReadTo(null);
        Left("the seeker's first Fossil");
        ComeThrough("Kalos", 344, 349, Direction.Up);
        ComeThrough("GlitteringCave", 11, 10, Direction.Up);
        Talk("GlitteringCave", 11, 10, Direction.Up);
        ReadTo("I've dug the other one clean", fight: false); Frames(10); Shot("wks401_fossil_seeker_second");
        ReadTo(null);
        Left("the seeker's second Fossil");
        // Ambrette's laboratory: the Jaw Fossil left with the restorer, and Tyrunt after a walk
        var lab = MapDatabase.Get("AmbretteTown_FossilLab");
        var restorer = lab.Everyone.FirstOrDefault(n => n.Script == "Restorer");
        if (restorer != null)
        {
            Talk("AmbretteTown_FossilLab", restorer.GridX, restorer.GridY + 1, Direction.Up);
            ReadTo("Shall I wake up", fight: false); Frames(10); Shot("wks402_lab_restorer");
            ReadTo(null);
            ComeThrough("Kalos", 344, 349, Direction.Up);
            ComeThrough("AmbretteTown_FossilLab", restorer.GridX, restorer.GridY + 1, Direction.Up);
            Talk("AmbretteTown_FossilLab", restorer.GridX, restorer.GridY + 1, Direction.Up);
            ReadTo("It worked", fight: false); Frames(10); Shot("wks403_lab_revived");
            ReadTo(null);
        }
        else Console.WriteLine("  !! nobody runs the laboratory's machine");
        Left("the laboratory");

        // The Battle Institute's test, three battles in a row
        Talk("LumioseCity_BattleInstitute", 4, 5, Direction.Up);
        ReadTo("Will you sit today's test", fight: false); Frames(10); Shot("wks404_institute_desk");
        ToBattle();
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks405_institute_battle"); Win(); }
        ReadTo("Here is your mark", fight: true); Frames(10); Shot("wks406_institute_mark");
        ReadTo(null);
        Left("the Battle Institute");

        // Diancie in Reflection Cave's north chamber, Hoopa in the Lost Hotel's gallery, Volcanion on Couriway's falls pool
        foreach (var (name, map, x, y, line, prefix) in new[]
                 {
                     ("Diancie", "ReflectionCave", 26, 8, "glitters back at the crystals", "wks410_diancie"),
                     ("Hoopa", "LostHotel", 40, 12, "pops out of it, laughing", "wks420_hoopa"),
                     ("Volcanion", "Kalos", 653, 378, "Steam bursts up off the pool", "wks430_volcanion")
                 })
        {
            ComeThrough(map, x, y, Direction.Up);
            ReadTo(null);
            // The research a battle before raised is told on the field's toast once it is free (the Battle Institute's
            // examiners' Doublade was told over Diancie's chamber): every toast is let go before the beat begins
            for (int quiet = 0, i = 0; quiet < 10 && i < 8000; i++) { Frames(1); quiet = game.Toast.Visible ? 0 : quiet + 1; }
            Frames(6); Shot(prefix + "_before");
            game.Interact();
            Skip(0.9); Shot(prefix + "_look");
            Skip(1.0); Shot(prefix + "_sign");
            ReadTo(line, fight: false); Frames(10); Shot(prefix + "_voice");
            ToBattle();
            if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot(prefix + "_battle"); Win(); }
            else Console.WriteLine($"  !! {name}'s battle never came");
            ReadTo(null);
            Left(name);
        }
    }
}
