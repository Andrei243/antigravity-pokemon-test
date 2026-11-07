using PokemonPlatinumEngine.Battle.Sim.Link;

partial class Harness
{
    // ---------------------------------------------------------------- a link battle on both screens (plan 28 · I3)

    // link: a link battle between two players over loopback links, a server in between, each player's screen a
    // BattleEngine fed from its client with no rules of its own (BattleEngine.ForLink). Both screens are played side by
    // side, frame for frame, and the shown one swapped, so each pair of shots is the same moment of the same turn from
    // both sides: the commands (lk01, lk02), the moves (lk03), one player waiting for the other (lk04) while the other
    // chooses (lk05), a turn's lines (lk06, lk07), its hit landed (lk08, lk09), and the end (lk10, lk11). Prints every
    // line each player read, which must name their own Pokémon plainly and the other's as the foe's. Not part of `all`.
    LinkServer linkServer;
    BattleEngine linkHidden;

    public void LinkMode()
    {
        var room = LinkRoom.Default with { Picks = 3 };
        var lucas = new LinkPlayer("Lucas", LinkTeam(11, "Infernape", "Staraptor", "Luxray"));
        var dawn = new LinkPlayer("Dawn", LinkTeam(23, "Empoleon", "Roserade", "Garchomp"));
        var toLucas = new LoopbackLink();
        var toDawn = new LoopbackLink();
        linkServer = new LinkServer(LinkSetup.Bring(room, lucas, dawn), 2026, toLucas.Server, toDawn.Server);
        var a = new LinkClient(lucas.Name, lucas.Team, toLucas.Player, new InOrder()) { AnswersByItself = false };
        var b = new LinkClient(dawn.Name, dawn.Team, toDawn.Player, new InOrder()) { AnswersByItself = false };
        for (int i = 0; i < 8; i++)
        {
            a.Pump();
            b.Pump();
            linkServer.Pump();
        }

        var faceA = BattleEngine.ForLink(a, playerLook: "PLAYER", foeLook: "DAWN");
        var faceB = BattleEngine.ForLink(b, playerLook: "DAWN", foeLook: "PLAYER");
        var (name, x, y) = Place("Route201", -1, -1);
        game.Map = MapDatabase.Get(name);
        if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
        game.BattleRenderer.SetArena(game.Map, x, y);
        ShowLink(faceA, faceB);
        game.State = GameState.Battle;

        // The opening on both screens, to the commands
        Pass(130 / 60.0);
        for (int i = 0; i < 20 && (faceA.HUD.MenuState != BattleMenuState.Main || faceB.HUD.MenuState != BattleMenuState.Main); i++)
        {
            ReadOn(faceA);
            ReadOn(faceB);
            Pass(0.8);
        }
        Pass(0.6);
        Shot("lk01_a_commands");
        ShowLink(faceB, faceA);
        Pass(0.1);
        Shot("lk02_b_commands");

        // Lucas chooses first and waits; Dawn is still choosing
        faceA.SelectMainMenuOption(0);
        ShowLink(faceA, faceB);
        Pass(0.3);
        Shot("lk03_a_moves");
        faceA.SelectMove(Strongest(faceA, faceA.LinkClient!.Asked!.Options[0].Moves));
        Pass(0.6);
        Shot("lk04_a_waiting");
        Console.WriteLine($"  waiting: {faceA.WaitingForOther}, \"{faceA.WaitingLine}\"");
        ShowLink(faceB, faceA);
        faceB.SelectMainMenuOption(0);
        Pass(0.3);
        Shot("lk05_b_choosing_while_a_waits");

        // Dawn's answer: the server works the turn out and both screens play it
        faceB.SelectMove(Strongest(faceB, faceB.LinkClient!.Asked!.Options[0].Moves));
        Pass(0.5);
        Shot("lk06_b_turn");
        ShowLink(faceA, faceB);
        Pass(0.05);
        Shot("lk07_a_turn");
        Pass(0.6);
        Shot("lk08_a_turn_landed");
        ShowLink(faceB, faceA);
        Pass(0.05);
        Shot("lk09_b_turn_landed");

        // On to the end: each player's first move the server allows, the first Pokémon able where one is to come in
        var linesA = new List<string>();
        var linesB = new List<string>();
        // (to the line that ends it, before the game would take the battle down)
        for (int i = 0; i < 3000 && !(OnLastLine(faceA) && OnLastLine(faceB)); i++)
        {
            Answer(faceA, linesA);
            Answer(faceB, linesB);
            Pass(0.4);
        }
        ShowLink(faceA, faceB);
        Pass(0.3);
        Shot("lk10_a_end");
        ShowLink(faceB, faceA);
        Pass(0.1);
        Shot("lk11_b_end");
        Console.WriteLine($"  result: {linkServer.Result}; Lucas's screen {faceA.Result}, Dawn's {faceB.Result}");
        Console.WriteLine($"  stages started: Lucas's {faceA.Core.Log.Count} events, Dawn's {faceB.Core.Log.Count} (none: no rules of their own)");
        Console.WriteLine("  Lucas read: " + string.Join(" | ", linesA.Take(24)));
        Console.WriteLine("  Dawn read: " + string.Join(" | ", linesB.Take(24)));
        // Taken down by hand: a link battle's end is the room's, never the field's
        game.Battle = null;
        linkHidden = null;
        linkServer = null;
        game.State = GameState.Overworld;
    }

    // The screen shown and the other, which is played along unseen
    void ShowLink(BattleEngine shown, BattleEngine hidden)
    {
        game.Battle = shown;
        linkHidden = hidden;
    }

    // Some seconds of both screens and the server, drawing every sixth frame of the shown one
    void Pass(double seconds)
    {
        int n = Math.Max(1, (int)Math.Round(seconds * 60.0));
        for (int i = 0; i < n; i++)
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            linkHidden?.Update(1f / 60f);
            linkServer?.Pump();
            if (i % 6 == 5 || i == n - 1) engine.Draw();
        }
    }

    // The battle is over at the server and the screen shows its last line, which the game would take down once read
    static bool OnLastLine(BattleEngine face) => face.LinkClient!.Result != null && face.IsWaitingForConfirm && face.LinesToCome == 0;

    // Reads a screen's waiting lines on
    static void ReadOn(BattleEngine face)
    {
        if (face.IsWaitingForConfirm && !face.IsLearningMove) face.ConfirmMessage();
    }

    // A player at their screen: a line read on, the first move the server allows, the first Pokémon it allows to come in
    static void Answer(BattleEngine face, List<string> lines)
    {
        if (OnLastLine(face)) return;
        if (face.IsWaitingForConfirm)
        {
            if (lines.Count == 0 || lines[^1] != face.CurrentMessage) lines.Add(face.CurrentMessage);
            if (face.IsLearningMove) face.ChooseLearn(1);
            else face.ConfirmMessage();
            return;
        }
        if (face.LinkClient!.Asked is not { } asked) return;
        if (face.HUD.MenuState == BattleMenuState.SwitchPokemon && face.IsChoosingReplacement) face.SelectSwitch(asked.Options[0].Switches[0]);
        else if (face.HUD.MenuState == BattleMenuState.Main)
        {
            face.SelectMainMenuOption(0);
            if (asked.Options[0].Moves.Count > 0) face.SelectMove(Strongest(face, asked.Options[0].Moves));
        }
    }

    // The strongest of the moves the server allows (the first of the strongest), so a turn shows a hit
    static int Strongest(BattleEngine face, IReadOnlyList<int> allowed) =>
        allowed.OrderByDescending(i => face.MenuBattler.Pokemon!.Moves[i].Power).ThenBy(i => i).First();

    // A team of the harness's own, as plain data a player brings: level 50 before the room levels it, a seed of its own
    static List<RecordedPokemon> LinkTeam(uint seed, params string[] species) =>
        species.Select((s, i) => RecordedPokemon.Of(new Pokemon(PokemonDatabase.Get(s)!, 50, new BattleRandom(seed + (uint)i)))).ToList();

    // Picks the six brought in the order brought; answers nothing (the screens do)
    sealed class InOrder : ILinkChooser
    {
        public IReadOnlyList<int> Pick(LinkHello hello, int brought) => Enumerable.Range(0, Math.Min(hello.Room.Picks, brought)).ToList();
        public IReadOnlyList<BattleChoice> Choose(LinkClient client, LinkAsk ask) => throw new InvalidOperationException("The screens answer");
    }
}
