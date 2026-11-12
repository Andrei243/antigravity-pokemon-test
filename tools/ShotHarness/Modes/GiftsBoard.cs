partial class Harness
{
    // ---------------------------------------------------------------- the trade board (plan 40 · GE8)

    // The trade board and blind trades (gf70 to gf75), in a new game of its own past the opening with Kanto entered (set
    // by hand, so that a blind trade may bring another region's species), through the game's own Interact, scripts, the
    // board's screen and the team's:
    //   gf70_board: the Global Terminal's receptionist's board, the day's three requests and the blind trade's row;
    //   gf71_board_request: the cursor on the first request, a Pokémon it asks for put in the team by hand;
    //   gf72_board_traded: the trade's line, the requested Pokémon sent and the offered one received;
    //   gf73_blind_trade: the board again, the first request met and the cursor on the blind trade's row;
    //   gf74_blind_received: the blind trade's line;
    //   gf75_backdrop_lady: the breeder upstairs from the board handing over a backdrop for the first trade.
    // It prints the day's requests, what was sent and received and the board's state after each.
    void GiftsBoard()
    {
        GiftsNewGame();
        game.Story.Set("KantoEntered");
        var today = GameClock.Today;
        var requests = TradeBoard.Requests(today, RegionDatabase.Sinnoh, engine.RegionsEnteredNow());
        foreach (var r in requests) Console.WriteLine($"  request {r.Index + 1}: {r.Trainer.Name} wants {r.Wants} at {r.AtLeast}+, offers {r.Offers}{(r.Item != null ? " holding " + r.Item : "")}");
        // What the first request asks for, put in the team by hand, and a Bidoof for the blind trade
        var first = requests[0];
        game.Party.Add(new Pokemon(PokemonDatabase.Get(first.Wants)!, System.Math.Max(first.AtLeast, 10)));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 8));
        game.Pokedex.RegisterSeen(PokemonDatabase.Get(first.Wants)!.DexNumber);

        // The receptionist, across her counter from the tile south of it
        At("GlobalTerminal1F", 7, 5, Direction.Up);
        Frames(20);
        game.Interact();
        Frames(3);
        GiftsUntil(() => game.State == GameState.TradeBoard, "the trade board");
        Frames(30); Shot("gf70_board");
        game.TradeBoardScreen.Move(1, 0); game.TradeBoardScreen.Move(-1, 0); Frames(30);
        Shot("gf71_board_request");
        game.TradeBoardScreen.Confirm(); Frames(4);
        GiftsUntil(() => game.State == GameState.PartyMenu && game.PartyScreen.IsActive, "the team to choose from");
        game.PartyScreen.SelectedIndex = game.Party.Members.Count - 2;
        game.PartyScreen.Confirm(game.Party); Frames(4);
        GiftsUntil(() => GiftsSaying("over for"), "the trade's line");
        game.Dialogue.FinishLine(); Frames(20);
        Shot("gf72_board_traded");
        GiftsUntil(() => !engine.ScriptRunning && !game.Dialogue.IsActive && game.State == GameState.Overworld, "the trade's end", most: 6000);
        Console.WriteLine($"  after the request: the team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}; met today {game.TradeBoardState.IsMet(today, first)}");

        // Again, to the blind trade's row
        game.Interact();
        Frames(3);
        GiftsUntil(() => game.State == GameState.TradeBoard, "the trade board again");
        game.TradeBoardScreen.Move(0, 1); Frames(30);
        Shot("gf73_blind_trade");
        game.TradeBoardScreen.Confirm(); Frames(4);
        GiftsUntil(() => game.State == GameState.PartyMenu && game.PartyScreen.IsActive, "the team for the blind trade");
        game.PartyScreen.SelectedIndex = game.Party.Members.Count - 1;
        game.PartyScreen.Confirm(game.Party); Frames(4);
        GiftsUntil(() => GiftsSaying("over for"), "the blind trade's line");
        game.Dialogue.FinishLine(); Frames(20);
        Shot("gf74_blind_received");
        GiftsUntil(() => !engine.ScriptRunning && !game.Dialogue.IsActive && game.State == GameState.Overworld, "the blind trade's end", most: 6000);
        Console.WriteLine($"  after the blind trade: the team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}; blind trades left {game.TradeBoardState.BlindLeft(today)}");

        // The breeder on the board's floor, by the panels: spoken to from the tile east of her
        At("GlobalTerminal1F", 14, 3, Direction.Left);
        Frames(20);
        game.Interact();
        Frames(3);
        GiftsUntil(() => GiftsSaying("received"), "the backdrop handed over");
        game.Dialogue.FinishLine(); Frames(20);
        Shot("gf75_backdrop_lady");
        GiftsUntil(() => !engine.ScriptRunning && !game.Dialogue.IsActive, "her goodbye");
        Console.WriteLine($"  the backdrop's flag {game.Story.Has("FLAG_GOT_TRADE_BOARD_BACKDROP")}");
    }
}
