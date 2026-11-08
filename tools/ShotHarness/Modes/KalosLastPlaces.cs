using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// Kalos's last places (plan 23, after Z4b; <c>KALOS_FROM=lastplaces</c>, not part of the whole <c>kalos</c> mode): the
    /// places from outside (<c>wk500</c> on: the Chamber of Emptiness's mouth in Santalune City, Kiloude's street with its
    /// station and Battle Maison, the Poké Ball Factory in Laverre City, the Battle Château on Route 7, Lumiose's two shops'
    /// doors), the rooms (<c>wk510</c> on), and the scenes (<c>wks300</c> on: Yveltal in the chamber and its battle, the
    /// Banettite in the evening hour, the stone shop's owner, the factory's president, a guest of the Château and the
    /// butler's title, the TMV into Kiloude's station, a round of the Battle Maison begun and its first battle). It wins
    /// every battle and prints what each scene left behind.
    /// </summary>
    public void KalosLastPlaces()
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
            for (int guard = 0; guard < 600 && !fight.IsBattleOver; guard++)
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
        void EnterTeam()
        {
            var screen = game.PartyScreen;
            Frames(60);
            for (int slot = 0; slot < screen.EnterCount; slot++)
            {
                screen.SelectedIndex = slot;
                Frames(2);
                screen.Confirm(game.Party);
                Frames(6);
            }
            screen.Confirm(game.Party);
            Frames(4);
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
                if (State() == GameState.PartyMenu) { EnterTeam(); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void Talk(string map, int x, int y, Direction facing) { At(map, x, y, facing); Frames(2); game.Interact(); }
        void ComeThrough(string map, int x, int y, Direction facing) { At(map, x, y, facing); game.ArriveOnMap(); Frames(2); }
        void Left(string what) =>
            Console.WriteLine($"  {what}: map {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), Yveltal gone {story.Has("FLAG_KALOS_YVELTAL_GONE")}, "
                + $"factory {story.Has("FLAG_KALOS_FACTORY_DONE")}, Kanto stone {story.Has("FLAG_KALOS_KANTO_STONE_GIVEN")}, château points {story.Var("VAR_KALOS_CHATEAU_POINTS")} "
                + $"title {story.Var("VAR_KALOS_CHATEAU_TITLE")}, glint {story.Has(ClockFlags.KalosStonesGlint)}, Maison streak {game.Frontier.Maison.Streak}, "
                + $"round {(game.Venue == null ? "none" : $"{game.Venue.Venue} battle {game.Venue.Battle + 1}")}, BP {game.Frontier.BattlePoints}");

        // A game of Kalos past its Hall of Fame with the TMV pass and the Mega Ring upgraded
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        foreach (var (species, level) in new[] { ("Greninja", 80), ("Talonflame", 78), ("Venusaur", 78), ("Lucario", 78) })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        foreach (string badge in RegionDatabase.Get(RegionDatabase.Kalos)!.BadgeNames) story.GiveBadge(RegionDatabase.Kalos, badge);
        story.SetVar("VAR_KALOS_STORY", 31);
        story.Set("KalosHallOfFame");
        story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 3);
        story.Set("FLAG_KALOS_DEFEATED_LAVERRE_GYM");
        story.Set("FLAG_KALOS_RECEIVED_KANTO_STARTER");
        story.Set("FLAG_KALOS_TMV_PASS");
        story.SetVar("VAR_KALOS_POSTGAME", 3);
        story.Set("FLAG_KALOS_RIVAL_KILOUDE_BEATEN");
        story.Set("FLAG_KALOS_MEGA_RING_UPGRADED");
        story.Set("FLAG_KALOS_HIDE_SNORLAX_ROUTE_7");
        Frames(2);
        Left("brought past the Hall of Fame");

        // ---- the places from outside
        At("Kalos", 320, 501, Direction.Up); Frames(6); Shot("wk500_chamber_mouth");
        At("Kalos", 570, 353, Direction.Up); Frames(6); Shot("wk501_kiloude_station_and_maison");
        At("Kalos", 592, 352, Direction.Up); Frames(6); Shot("wk502_kiloude_maison");
        At("Kalos", 383, 196, Direction.Up); Frames(6); Shot("wk503_laverre_factory");
        At("Kalos", 188, 398, Direction.Up); Frames(6); Shot("wk504_route7_chateau");
        At("Kalos", 377, 320, Direction.Up); Frames(6); Shot("wk505_lumiose_herboriste");
        At("Kalos", 331, 400, Direction.Up); Frames(6); Shot("wk506_lumiose_ball_boutique");

        // ---- the rooms
        foreach (var (shot, map, x, y) in new[]
                 {
                     ("wk510_kiloude_station", "KiloudeCity_Station", 6, 8), ("wk511_maison_lobby", "KiloudeCity_BattleMaison", 8, 12),
                     ("wk512_maison_hall", "KiloudeCity_BattleMaisonHall", 8, 12), ("wk513_factory", "LaverreCity_PokeBallFactory", 10, 14),
                     ("wk514_chateau", "KalosRoute7_BattleChateau", 9, 13), ("wk515_herboriste", "LumioseCity_Herboriste", 4, 8),
                     ("wk516_ball_boutique", "LumioseCity_BallBoutique", 4, 8), ("wk517_stone_shop", "LumioseCity_StoneEmporium", 4, 8)
                 })
        {
            ComeThrough(map, x, y, Direction.Up);
            ReadTo(null);
            Frames(6);
            Shot(shot);
        }

        // ---- the scenes
        // The Chamber of Emptiness: the chamber, Yveltal, its battle
        ComeThrough("ChamberOfEmptiness", 16, 26, Direction.Up); Frames(6); Shot("wk520_chamber_way_in");
        At("ChamberOfEmptiness", 16, 10, Direction.Up); Frames(6); Shot("wks300_chamber_yveltal");
        Talk("ChamberOfEmptiness", 16, 8, Direction.Up);
        ReadTo("Its wings open", fight: false); Frames(10); Shot("wks301_yveltal_wakes");
        for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks302_yveltal_battle"); Win(); }
        ReadTo(null);
        Left("Yveltal");
        // The evening hour: the Banettite glints by the chamber's wall
        ClockFlags.Keep(story, ClockFlags.StonesHour);
        game.Map.ApplyPresence(story.Has);
        At("ChamberOfEmptiness", 11, 11, Direction.Up); Frames(6); Shot("wks303_banettite_evening");
        ClockFlags.Keep(story, 12);
        Left("the stones' hour");

        // The stone shop's owner and the Kanto Pokémon's stone
        Talk("LumioseCity_StoneEmporium", 2, 5, Direction.Up);
        ReadTo("Which one is yours"); Frames(10); Shot("wks304_stone_owner");
        ReadTo(null);
        Left("the stone shop");

        // The Poké Ball Factory: the president held, the grunts beaten, the Master Ball
        foreach (var npc in MapDatabase.Get("LaverreCity_PokeBallFactory").Everyone.Where(n => n.TrainerData?.Id?.StartsWith("kalos_flare_grunt_factory") == true))
        {
            story.Defeat(npc.TrainerData!.Id);
            npc.HasBattled = true;
        }
        Talk("LaverreCity_PokeBallFactory", 10, 5, Direction.Up);
        ReadTo("It catches any Pokémon there is"); Frames(10); Shot("wks305_factory_president");
        ReadTo(null);
        Left("the factory");

        // The Battle Château: a guest of the day, a win, the butler's title
        ComeThrough("KalosRoute7_BattleChateau", 9, 13, Direction.Up); Frames(6); Shot("wks306_chateau_guests");
        var guest = game.Map.NPCs.FirstOrDefault(n => n.Script == "Guest" && n.GridX < 9);
        if (guest != null)
        {
            story.SetVar("VAR_KALOS_CHATEAU_POINTS", 2);
            Talk("KalosRoute7_BattleChateau", guest.GridX + 1, guest.GridY, Direction.Left);
            ReadTo("From today you are its Guest"); Frames(10); Shot("wks307_chateau_title");
            ReadTo(null);
        }
        else Console.WriteLine("  !! nobody in the Château's west place");
        Left("the Château");

        // The TMV into Kiloude's own station
        Talk("LumioseCity_Station", 12, 5, Direction.Up);
        ReadTo(null);
        Frames(30); Shot("wks308_kiloude_station_arrived");
        Left("the TMV");

        // A round of the Battle Maison: the desk, three entered, the hall and its first battle
        Talk("KiloudeCity_BattleMaison", 4, 5, Direction.Up);
        ReadTo("Would you like to take on", fight: false); Frames(10); Shot("wks309_maison_desk");
        ReadTo("Battle 1 of seven", fight: false); Frames(10); Shot("wks310_maison_first_battle");
        ReadTo("steps forward", fight: false); Frames(10); Shot("wks311_maison_challenger");
        for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks312_maison_battle"); Win(); }
        ReadTo("Battle 2 of seven", fight: false); Frames(10); Shot("wks313_maison_second");
        Left("the Maison");
    }
}
