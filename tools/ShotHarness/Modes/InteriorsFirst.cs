using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---- the last rooms at work (`in_v*`; plan 01 · M11, the hand-made rooms of the first chapters laid out again on
    // the original's plans), through the game's own arrival scripts, TryInteract and steps: the rival's room as the
    // opening finds it, his start at the sight of the player, his run round the furniture to the stairs and the room
    // without him; the Pokétch Company's president before his counter; a Center's PC used from the tile before it,
    // its basement's attendant before the Coal Badge, and a Mart's clerk across the counter. It prints what each
    // left behind.
    void InteriorsAtWorkE()
    {
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        // Reads on to the line that holds the words, or with none to the end of whatever is being said
        void ReadTo(string? text, int most = 2400)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); return; }
                if (!engine.ScriptRunning && !Box().IsActive && game.State == GameState.Overworld && (text == null || i > 60)) break;
                if (engine.Choice.IsOpen) { Frames(1); engine.Choice.Back(); Frames(1); }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Frames(1); }
                else if (Box().IsActive && !Box().IsQuestion) { Box().Advance(); Frames(1); }
                else Frames(1);
            }
            if (text != null) Console.WriteLine($"  !! never said: {text}");
        }
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            game.LocationSign.Hide();
            Frames(2);
        }
        NPC? Someone(string map, string key) => MapDatabase.Get(map).NPCs.FirstOrDefault(n => n.Key == key);

        // ---- the rival's room, as the opening finds it: he is packing at his desk when the player comes up the stairs
        const string room = "RivalHouse2F";
        bool gone = story.Has("FLAG_HIDE_TWINLEAF_TOWN_RIVAL_HOUSE_2F_RIVAL"), left = story.Has("FLAG_RIVAL_LEFT_HOME");
        int state = story.Var("VAR_RIVAL_HOUSE_STATE"), guitarist = story.Var("VAR_TWINLEAF_TOWN_GUITARIST_TRIGGER_STATE");
        story.Unset("FLAG_HIDE_TWINLEAF_TOWN_RIVAL_HOUSE_2F_RIVAL");
        story.Unset("FLAG_RIVAL_LEFT_HOME");
        story.SetVar("VAR_RIVAL_HOUSE_STATE", 0);
        ComeThrough(room, 4, 4, Direction.Right);
        ReadTo("Notebook"); Frames(4); Shot("in_v01_rivals_room_packing");
        ReadTo("Professor Rowan"); Frames(4); Shot("in_v02_rivals_room_he_sees_the_player");
        ReadTo("ten-million"); Box().Advance();
        Until(() => Someone(room, "rival") is { } r && r.GridX <= 6, "the rival on his way round", 400);
        Shot("in_v03_rivals_room_running_for_the_stairs");
        ReadTo(null);
        Frames(20); Shot("in_v04_rivals_room_without_him");
        Console.WriteLine($"rival's room: player at {game.Player.GridX},{game.Player.GridY}, rival there {Someone(room, "rival") != null}, " +
                          $"left home {story.Has("FLAG_RIVAL_LEFT_HOME")}, state {story.Var("VAR_RIVAL_HOUSE_STATE")}, guitarist {story.Var("VAR_TWINLEAF_TOWN_GUITARIST_TRIGGER_STATE")}");
        // (the harness's game is past the opening: as it was)
        if (gone) story.Set("FLAG_HIDE_TWINLEAF_TOWN_RIVAL_HOUSE_2F_RIVAL");
        if (!left) story.Unset("FLAG_RIVAL_LEFT_HOME");
        story.SetVar("VAR_RIVAL_HOUSE_STATE", state);
        story.SetVar("VAR_TWINLEAF_TOWN_GUITARIST_TRIGGER_STATE", guitarist);

        // ---- the Pokétch Company: the president before his family's counter, once the campaign has brought him in
        bool hidden = story.Has("FLAG_HIDE_POKETCH_CO_1F_POKETCH_CO_PRESIDENT");
        story.Unset("FLAG_HIDE_POKETCH_CO_1F_POKETCH_CO_PRESIDENT");
        ComeThrough("PoketchCompany", 16, 12, Direction.Up);
        Frames(20); Shot("in_v10_poketch_company_from_its_east_door");
        At("PoketchCompany", 18, 6, Direction.Up);
        game.Interact();
        ReadTo("president"); Frames(4); Shot("in_v11_poketch_company_the_president");
        ReadTo(null);
        if (hidden) story.Set("FLAG_HIDE_POKETCH_CO_1F_POKETCH_CO_PRESIDENT");

        // ---- a Pokémon Center: the PC from the tile before it, and the attendant before the way down
        bool open = story.Has("FLAG_HIDE_POKECENTER_BASEMENT_BLOCKADE");
        story.Unset("FLAG_HIDE_POKECENTER_BASEMENT_BLOCKADE");
        ComeThrough("JubilifePokemonCenter", 12, 4, Direction.Up);
        game.Interact();
        Whole(); Shot("in_v20_center_the_pc");
        Box().Advance(); Frames(20);
        Console.WriteLine($"center: the pc opened {game.State}");
        game.PcScreen.Close(); Frames(6);
        ReadTo(null);
        At("JubilifePokemonCenter", 12, 10, Direction.Right);
        game.Interact();
        ReadTo("downstairs"); Frames(4); Shot("in_v21_center_the_basements_attendant");
        ReadTo(null);
        engine.Steering = (Direction.Right, false);
        Frames(30);
        engine.Steering = null;
        Console.WriteLine($"center: before the Coal Badge the player is kept at {game.Player.GridX},{game.Player.GridY} of {game.Map.Name}");
        story.Set("FLAG_HIDE_POKECENTER_BASEMENT_BLOCKADE");
        ComeThrough("JubilifePokemonCenter", 12, 10, Direction.Right);
        Frames(10); Shot("in_v22_center_the_way_down_open");
        if (!open) story.Unset("FLAG_HIDE_POKECENTER_BASEMENT_BLOCKADE");

        // ---- a Mart: the town's own counter, across it
        ComeThrough("JubilifePokeMart", 2, 7, Direction.Up);
        game.Interact();
        ReadTo("only find here"); Frames(4); Shot("in_v30_mart_the_towns_own_counter");
        Box().Advance(); Frames(30);
        Console.WriteLine($"mart: the counter opened {game.State}");
        if (game.State == GameState.Shop) game.Shop.Close();
        Frames(6);
        ReadTo(null);
    }
}
