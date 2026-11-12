partial class Harness
{
    // ---------------------------------------------------------------- the gift box (plan 40 · GE7)

    // The gift box (gf60 to gf66), in a new game of its own past the opening, through the game's own Interact, scripts,
    // phrase picker and the box's screen:
    //   gf60_questionnaire: Madame Ottilie Fray in Jubilife TV's lobby asking her first question;
    //   gf61_phrase_answered: the picker on her second question with SPARROW and CLOCKWORK chosen;
    //   gf62_courier: a Gift Runner upstairs in Jubilife's Pokémon Center, the box open and Sinnoh's Hall of Fame entered
    //     (set by hand), saying something waits;
    //   gf63_box_waiting: the box on WAITING with the Laurel Gallery's Egg;
    //   gf64_box_card: the card under the cursor a moment on, risen;
    //   gf65_gift_taken: the Egg handed over;
    //   gf66_box_album: the runner again, the words SPARROW and CLOCKWORK told, the box on CARDS with what was taken.
    // It prints the flag, what waited and what was taken.
    void GiftsBox()
    {
        GiftsNewGame();
        var story = game.Story;
        game.Story.Set("SinnohHallOfFame");

        // Madame Ottilie Fray, spoken to from the tile west of her
        At("JubilifeTV1F", 17, 16, Direction.Right);
        Frames(20);
        game.Interact();
        Frames(3);
        GiftsUntil(() => engine.Choice.IsOpen, "her question", answer: false);
        engine.Choice.Confirm(); Frames(4);
        GiftsUntil(() => GiftsSaying("The first."), "her first question", answer: false);
        game.Dialogue.FinishLine(); Frames(20);
        Shot("gf60_questionnaire");
        GiftsUntil(() => game.State == GameState.Phrases && game.Phrases.IsActive, "the first picker", answer: false);
        var picker = game.Phrases;
        Frames(10);
        picker.Entry!.Choose(0, "feelings:happy");
        picker.Entry!.Choose(1, "small:together");
        picker.ToDone(); picker.Confirm(); Frames(6);
        GiftsUntil(() => game.State == GameState.Phrases && game.Phrases.IsActive, "the second picker", answer: false);
        Frames(10);
        picker.Entry!.Choose(0, "small:sparrow");
        picker.Entry!.Choose(1, "small:clockwork");
        picker.ToDone(); Frames(10);
        Shot("gf61_phrase_answered");
        picker.Confirm(); Frames(6);
        GiftsUntil(() => !engine.ScriptRunning && !game.Dialogue.IsActive, "the questionnaire's end");
        Console.WriteLine($"  questionnaire: the box open {story.Has("FLAG_GIFT_BOX_OPEN")}");

        // The Gift Runner upstairs in Jubilife's Pokémon Center, spoken to from the tile south of him
        At("JubilifePokemonCenter2F", 14, 9, Direction.Up);
        Frames(20);
        var facts = engine.GiftFactsNow();
        Console.WriteLine($"  waiting: {string.Join(", ", game.Gifts.Due(facts).Select(g => g.Id).DefaultIfEmpty("nothing"))}");
        game.Interact();
        Frames(3);
        GiftsUntil(() => GiftsSaying("something in the box"), "the runner's word");
        game.Dialogue.FinishLine(); Frames(20);
        Shot("gf62_courier");
        GiftsUntil(() => game.State == GameState.GiftBox, "the gift box");
        Frames(8); Shot("gf63_box_waiting");
        Frames(40); Shot("gf64_box_card");
        game.GiftBoxScreen.Confirm(); Frames(4);
        GiftsUntil(() => GiftsSaying("received"), "the gift handed over");
        game.Dialogue.FinishLine(); Frames(20);
        Shot("gf65_gift_taken");
        GiftsUntil(() => !engine.ScriptRunning && !game.Dialogue.IsActive, "the runner's goodbye");
        Console.WriteLine($"  taken: {string.Join(", ", game.Gifts.Album().Select(t => t.Gift.Id))}; the team {string.Join(", ", game.Party.Members.Select(p => p.IsEgg ? "Egg" : p.Species.Name))}");

        // Again: nothing waits, the words are told and the box opens on what they called up; then its album
        game.Interact();
        Frames(3);
        GiftsUntil(() => game.State == GameState.Phrases && game.Phrases.IsActive, "the runner's picker");
        Frames(6);
        game.Phrases.Entry!.Choose(0, "small:sparrow");
        game.Phrases.Entry!.Choose(1, "small:clockwork");
        game.Phrases.ToDone(); game.Phrases.Confirm(); Frames(6);
        GiftsUntil(() => game.State == GameState.GiftBox, "the gift box again");
        game.GiftBoxScreen.ChangeTab(1); Frames(30);
        Shot("gf66_box_album");
        game.GiftBoxScreen.ChangeTab(-1); Frames(4);
        game.GiftBoxScreen.Confirm(); Frames(4);
        GiftsUntil(() => !engine.ScriptRunning && !game.Dialogue.IsActive && game.State == GameState.Overworld, "the second gift's end");
        Console.WriteLine($"  taken at last: {string.Join(", ", game.Gifts.Album().Select(t => t.Gift.Id))}");
    }

    // ---- helpers the gift parts share

    // A new game of Sinnoh past the opening, in slot 1, the field showing
    void GiftsNewGame()
    {
        engine.NewGameRegion = RegionDatabase.Sinnoh;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        PastTheOpening();
    }

    // The text box is saying a line that holds the words
    bool GiftsSaying(string words) => game.Dialogue.IsActive && Translated.Saying(game.Dialogue.CurrentLine, words);

    // Runs the field on, reading each line written out and (unless told not to) answering a question with its first
    // answer, until something holds; says so when it never does
    void GiftsUntil(Func<bool> holds, string what, bool answer = true, int most = 2400)
    {
        for (int i = 0; i < most && !holds(); i++)
        {
            if (answer && engine.Choice.IsOpen) { Frames(6); engine.Choice.Confirm(); Frames(2); continue; }
            if (game.State == GameState.Dialogue && game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion && !engine.Choice.IsOpen)
            {
                Frames(2);
                if (holds()) break;
                game.Dialogue.Advance();
            }
            Frames(1);
        }
        if (!holds()) Console.WriteLine($"  !! never happened: {what} (state {game.State})");
    }
}
