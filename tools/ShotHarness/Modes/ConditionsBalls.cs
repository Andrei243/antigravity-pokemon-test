partial class Harness
{
    // Each its own ball (plan 25 · Y4), the very last part of `conditions`: the player's lead sent out of every kind
    // of ball in turn, shot a moment after the lid opens (96_release_<kind>: poke, great, ultra, master, premier,
    // safari, net, dive, nest, repeat, timer, luxury, dusk, heal, quick, cherish), and called back into a Luxury Ball
    // at three moments (96_recall_a_flight, 96_recall, 96_recall_c_out, and 96_recall_d_gone once the line would go
    // on). It prints the ball each place holds, how many quads the release draws, and that the recall is over.
    static readonly string[] BallKinds =
    {
        "Poké Ball", "Great Ball", "Ultra Ball", "Master Ball", "Premier Ball", "Safari Ball", "Net Ball", "Dive Ball",
        "Nest Ball", "Repeat Ball", "Timer Ball", "Luxury Ball", "Dusk Ball", "Heal Ball", "Quick Ball", "Cherish Ball"
    };

    void BallsMode()
    {
        var lead = new Pokemon(PokemonDatabase.Get("Luxray")!, 30, new Random(5));
        party.HealAll();
        party.Members.Insert(0, lead);
        var b = StartBattle("Bidoof", 12, chance: new Random(9));
        ToMainMenu(b);
        var side = BattleSide.Player;

        foreach (var kind in BallKinds)
        {
            lead.Ball = kind == "Poké Ball" ? null : kind;
            b.Anim.SendOut(side, lead);
            Skip(BattleAnimator.SendOutBallTime + 0.14);
            var fx = new FxList();
            var release = BattleBall.ReleaseOf(BattleBall.LookOf(BattleBall.NameFor(lead)));
            release.Emit(fx, System.Numerics.Vector3.Zero, 1.5f, 0.14f, 3);
            Console.WriteLine($"  release: {kind} held as {b.Anim[side, 0].Ball}, {release.Kind}, {fx.Quads.Count} quads at 0.14 s");
            Shot("96_release_" + kind.Replace(" Ball", "").Replace("Poké", "poke").ToLowerInvariant());
            Skip(1.0);
        }

        // Called back: the ball it came out of flies in, opens, draws it in, shuts and flies out
        lead.Ball = "Luxury Ball";
        b.Anim.SendOut(side, lead);
        Skip(BattleAnimator.SendOutTime + 0.2);
        b.Anim.Recall(side);
        Skip(BattleAnimator.RecallTime * 0.26); Shot("96_recall_a_flight");
        Skip(BattleAnimator.RecallTime * 0.24); Shot("96_recall");
        Skip(BattleAnimator.RecallTime * 0.45); Shot("96_recall_c_out");
        Skip(BattleAnimator.RecallTime * 0.2);
        var view = b.Anim[side, 0];
        Console.WriteLine($"  recall: ball {view.Ball}, age {view.RecallAge}, present {view.Present}");
        Shot("96_recall_d_gone");

        party.Members.Remove(lead);
        game.State = GameState.Overworld;
    }
}
