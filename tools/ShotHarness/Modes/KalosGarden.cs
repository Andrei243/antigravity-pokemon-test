using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// What Kalos still lacked after "Kalos finished" (plan 23; <c>KALOS_FROM=garden</c>, not part of the whole <c>kalos</c>
    /// mode): Parfum Palace's garden maze behind its hall by day and after dark with the owner by the fountain
    /// (<c>wk720</c> to <c>wk723</c>), his fireworks at three moments (<c>wks540</c> to <c>wks542</c>), Camphrier castle's
    /// keeper showing the player round (<c>wks550</c>, <c>wks551</c>) and Kalos's designer in the Hotel Richissime
    /// (<c>wks560</c>). It prints what each scene left behind.
    /// </summary>
    public void KalosGarden()
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
        void ReadTo(string? text, int most = 2400)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                idle = !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld ? idle + 1 : 0;
                if (text != null && idle > 90) break;
                if (text == null && idle > 30) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void Talk(string map, int x, int y, Direction facing) { At(map, x, y, facing); Frames(2); game.Interact(); }
        void ComeThrough(string map, int x, int y, Direction facing) { At(map, x, y, facing); game.ArriveOnMap(); Frames(2); }

        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        story.SetVar("VAR_KALOS_STORY", 16);
        story.Set("FLAG_KALOS_RECEIVED_HM01");
        story.Set("FLAG_KALOS_HIDE_CASTELLAN_STREET");
        story.Set("FLAG_KALOS_PALACE_FURFROU_FOUND");
        story.Set("FLAG_KALOS_PALACE_GIFT");
        Frames(2);

        // ---- the hall's way out to the garden, in its back wall
        At("KalosRoute6_ParfumPalace", 9, 4, Direction.Up); Frames(8); Shot("wk723_palace_hall_garden_door");

        // ---- the garden maze by day: nobody by the fountain
        ComeThrough("KalosRoute6_ParfumGarden", 13, 21, Direction.Up); ReadTo(null); Frames(8); Shot("wk720_palace_garden_entrance");
        At("KalosRoute6_ParfumGarden", 13, 14, Direction.Up); Frames(8); Shot("wk721_palace_garden_fountain");

        // ---- after dark: the owner by the fountain and his fireworks
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        ComeThrough("KalosRoute6_ParfumGarden", 13, 13, Direction.Up); ReadTo(null); Frames(30); Shot("wk722_palace_garden_night");
        game.Interact();
        ReadTo("Would you care to watch");
        Frames(10);
        Until(() => engine.Choice.IsOpen, "the question's answers");
        engine.Choice.Confirm();
        Frames(70); Shot("wks540_fireworks_first");
        Frames(60); Shot("wks541_fireworks_many");
        Frames(55); Shot("wks542_fireworks_last");
        ReadTo(null);
        Console.WriteLine($"  the fireworks seen {story.Has("FLAG_KALOS_PALACE_FIREWORKS")}");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // ---- the castle's keeper at home
        ComeThrough("CamphrierTown_Castle", 7, 10, Direction.Up);
        ReadTo("some three hundred years"); Frames(10); Shot("wks550_castle_keeper_meets");
        ReadTo("suits of armour"); Frames(30); Shot("wks551_castle_armour");
        ReadTo(null);
        Console.WriteLine($"  the castle: tour {story.Has("FLAG_KALOS_CASTLE_TOUR")}");

        // ---- Kalos's designer
        Talk("LumioseCity_HotelRichissime", 4, 7, Direction.Up);
        ReadTo("sketching"); Frames(10); Shot("wks560_kalos_designer");
        ReadTo(null);
    }
}
