using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// What finished Kalos (plan 23, "Kalos finished"; <c>KALOS_FROM=finished</c>, not part of the whole <c>kalos</c>
    /// mode): the Friend Safari's gate in Kiloude City, its hall and guide, the grounds and a Pokémon of them in battle
    /// (<c>wk700</c> to <c>wk704</c>, <c>wks500</c>), Parfum Palace's front and hall and its owner's Furfrou (<c>wk710</c>,
    /// <c>wk711</c>, <c>wks510</c> to <c>wks512</c>), the Cyllage bicycle shop's Bicycle (<c>wks520</c>) and Malva's
    /// second round (<c>wks530</c>, <c>wks531</c>). It prints the player's three grounds and what each scene left behind.
    /// </summary>
    public void KalosFinished()
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
        void ToMenu() { Skip(1.0); ToMainMenu(game.Battle); }
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
        void Quiet() { for (int quiet = 0, i = 0; quiet < 10 && i < 8000; i++) { Frames(1); quiet = game.Toast.Visible ? 0 : quiet + 1; } }

        // A game of Kalos past its Hall of Fame, the flute lent and the power back
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        foreach (var (species, level) in new[] { ("Greninja", 80), ("Talonflame", 78), ("Venusaur", 78) })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        foreach (string badge in RegionDatabase.Get(RegionDatabase.Kalos)!.BadgeNames) story.GiveBadge(RegionDatabase.Kalos, badge);
        story.SetVar("VAR_KALOS_STORY", 31);
        story.Set("KalosHallOfFame");
        story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 3);
        story.Set("FLAG_KALOS_TMV_PASS");
        story.Set("FLAG_KALOS_RECEIVED_HM01");
        story.Set("FLAG_KALOS_HIDE_SNORLAX_ROUTE_7");
        game.Money = 200000;
        Frames(2);
        var grounds = FriendSafari.Of(PlayerIdentity.Number);
        Console.WriteLine($"  the player's grounds: {string.Join("; ", grounds.Select(g => $"{g.Type}: {string.Join(", ", g.Species)}"))}");

        // ---- the Friend Safari
        At("Kalos", 575, 367, Direction.Up); Frames(8); Shot("wk700_friend_safari_gate");
        ComeThrough(GameEngine.FriendSafariGate, 6, 8, Direction.Up); ReadTo(null); Frames(6); Shot("wk701_friend_safari_hall");
        Talk(GameEngine.FriendSafariGate, 6, 5, Direction.Up);
        ReadTo("Which of your grounds", fight: false); Frames(12); Shot("wks500_friend_safari_guide");
        ReadTo(null, fight: false);
        Until(() => game.Map.Name == GameEngine.FriendSafariMap, "the grounds");
        Frames(20);
        Console.WriteLine($"  grounds laid: {string.Join(", ", game.Map.WildEncounters.Select(e => $"{e.SpeciesName} {e.MinLevel}"))} (chosen {story.Var(FriendSafari.ChosenVar)})");
        Shot("wk702_friend_safari_grounds");
        At(GameEngine.FriendSafariMap, 6, 9, Direction.Down); Frames(8); Shot("wk703_friend_safari_grass");
        game.StartWildBattle(game.Map.WildEncounters[0]);
        Until(() => State() == GameState.Battle, "the grounds' battle", 600);
        if (State() == GameState.Battle) { ToMenu(); Shot("wk704_friend_safari_battle"); Win(); }
        Quiet();

        // ---- Parfum Palace
        At("Kalos", 201, 270, Direction.Up); Frames(8); Shot("wk710_parfum_palace_front");
        ComeThrough("KalosRoute6_ParfumPalace", 10, 12, Direction.Up); ReadTo(null); Frames(6); Shot("wk711_parfum_palace_hall");
        Talk("KalosRoute6_ParfumPalace", 10, 5, Direction.Up);
        ReadTo("Have you seen it", fight: false); Frames(10); Shot("wks510_palace_owner");
        ReadTo(null);
        Talk("KalosRoute6_ParfumPalace", 17, 10, Direction.Right);
        ReadTo("hiding in a corner of the east wing", fight: false); Frames(10); Shot("wks511_palace_furfrou");
        ReadTo(null);
        Talk("KalosRoute6_ParfumPalace", 10, 5, Direction.Up);
        ReadTo("accept this", fight: false); Frames(10); Shot("wks512_palace_thanks");
        ReadTo(null);
        Console.WriteLine($"  the palace: Furfrou found {story.Has("FLAG_KALOS_PALACE_FURFROU_FOUND")}, gift {story.Has("FLAG_KALOS_PALACE_GIFT")}, Sun Stones {Held("Sun Stone")}");

        // ---- the bicycle shop
        int bikes = Held("Bicycle");
        if (bikes > 0 && ItemDatabase.Get("Bicycle") is { } bicycle) game.Bag.RemoveItem(bicycle, bikes);
        var shop = MapDatabase.Get("CyllageCity_BikeShop");
        var owner = shop.Everyone.Single(n => n.Key == "owner");
        Talk("CyllageCity_BikeShop", owner.GridX, owner.GridY + 1, Direction.Up);
        ReadTo("first bicycle off the bench", fight: false); Frames(10); Shot("wks520_bike_shop");
        ReadTo(null);
        Console.WriteLine($"  the bicycle shop: Bicycles {Held("Bicycle")}");

        // ---- Malva's second round
        Talk("KalosLeague_MalvaRoom", 6, 5, Direction.Up);
        ReadTo("I've been stoking it", fight: false); Frames(10); Shot("wks530_malva_again");
        ReadTo(null, fight: false);
        Until(() => State() == GameState.Battle, "Malva's second battle", 900);
        if (State() == GameState.Battle)
        {
            ToMenu(); Shot("wks531_malva_again_battle");
            Console.WriteLine($"  Malva's team: {string.Join(", ", game.Battle.EnemyParty?.Members.Select(p => $"{p.Species.Name} {p.Level}") ?? Array.Empty<string>())}");
            Win();
        }
        ReadTo(null);
        Console.WriteLine($"  Malva beaten again {story.Has("FLAG_KALOS_DEFEATED_MALVA")}");
    }
}
