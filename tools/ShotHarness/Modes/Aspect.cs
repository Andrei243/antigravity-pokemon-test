using System;
#nullable enable
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- other shapes of screen (rulings, "Other shapes of screen")

    // A few pictures at whatever shape SHOTS_WINDOW gives the hidden window (as_*; not part of "all"): the title's
    // journey and its menu, Twinleaf Town, a wild battle at its menu, the bag and the Town Map. Run at 2560x1080,
    // 3440x1440 and 1440x1080 to look at 21:9 and 4:3; at 1920x1080 it is the picture every other mode shoots. It prints
    // the canvas each was drawn on.
    public void AspectMode()
    {
        const float Dt = 1f / 60f;
        void Seconds(float s) => Frames((int)MathF.Round(s / Dt));
        void Take(string shot)
        {
            Frames(2);
            Shot(shot);
            Console.WriteLine($"{shot}: canvas {engine.Canvas.CanvasWidth}×{engine.Canvas.CanvasHeight}, safe area at {engine.Canvas.PadX},{engine.Canvas.PadY}, state {game.State}");
        }

        var title = new TitleScreen(null, (TitleNotice?)null);
        game.Title = title;
        game.State = GameState.Title;
        Seconds(TitleScreen.NoticeTime + 1.9f); Take("as_1_title_journey");
        Seconds(TitleScreen.SegmentTime * 3f + 3f);
        title.PressConfirm();
        Seconds(1f); Take("as_2_title_menu");

        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        At("Sinnoh", 116, 888, Direction.Down);
        Frames(30); Take("as_3_field");

        var battle = StartBattle("Starly", 5);
        ToMainMenu(battle);
        Take("as_4_battle_menu");
        game.Battle = null;
        game.State = GameState.Overworld;
        Frames(10);

        var bag = game.Bag;
        var townMap = ItemDatabase.Get("Town Map")!;
        bag.AddItem(townMap, 1);
        game.State = GameState.BagMenu;
        game.BagScreen.Open();
        Frames(20); Take("as_5_bag");
        game.BagScreen.Close();
        game.State = GameState.Overworld;
        Frames(4);
        game.OpenTownMap(TownMapMode.Item);
        Frames(40); Take("as_6_town_map");
    }
}
