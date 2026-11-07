using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests.Link;

/// <summary>
/// Plan 28 · I3, the screen's half: a battle screen fed from a link client with no rules of its own, each player's
/// screen standing on its own side, the lines in each player's own words, and the waiting line.
/// </summary>
public class LinkScreenTests
{
    private static readonly LinkRoom Doubles = LinkRoom.Default with { Format = BattleFormat.Double };

    /// <summary>A bench whose two players answer through their screens, greeted and with the first question asked.</summary>
    private static (LinkBench Bench, BattleEngine A, BattleEngine B) Screens(uint seed, LinkRoom? room = null)
    {
        var bench = new LinkBench(seed, room, autoAnswerB: false);
        bench.A.AnswersByItself = false;
        bench.Settle();
        return (bench, BattleEngine.ForLink(bench.A), BattleEngine.ForLink(bench.B));
    }

    /// <summary>
    /// One step of a player at their screen: a line read on, the first move the server allows at the commands (aimed
    /// at the first place offered), the first Pokémon it allows where one is to come in. False once nothing is left to do.
    /// </summary>
    internal static bool Act(BattleEngine face, List<string> lines)
    {
        face.Update(0.05f);
        if (face.IsBattleOver) return false;
        if (face.IsLearningMove)
        {
            face.ChooseLearn(1);
            return true;
        }
        if (face.IsWaitingForConfirm)
        {
            if (lines.Count == 0 || lines[^1] != face.CurrentMessage) lines.Add(face.CurrentMessage);
            face.ConfirmMessage();
            return true;
        }
        var asked = face.LinkClient!.Asked;
        if (asked == null) return true;
        var options = asked.Options;
        switch (face.HUD.MenuState)
        {
            case BattleMenuState.SwitchPokemon when face.IsChoosingReplacement:
                face.SelectSwitch(options[0].Switches[0]);
                break;
            case BattleMenuState.Main:
                // The place the menus are open for, on the battle's own side
                var place = options.FirstOrDefault(o => o.Place.Slot == face.MenuBattler.Slot) ?? options[0];
                if (place.Moves.Count == 0)
                {
                    face.SelectMainMenuOption(0);
                    break;
                }
                face.SelectMainMenuOption(0);
                face.SelectMove(place.Moves[0]);
                if (face.HUD.MenuState == BattleMenuState.SelectTarget) face.SelectTarget(0);
                break;
        }
        return true;
    }

    private static (List<string> LinesA, List<string> LinesB) PlayOut(LinkBench bench, BattleEngine a, BattleEngine b)
    {
        var linesA = new List<string>();
        var linesB = new List<string>();
        for (int i = 0; i < 400_000; i++)
        {
            bool goingA = Act(a, linesA);
            bool goingB = Act(b, linesB);
            bench.Server.Pump();
            if (!goingA && !goingB) return (linesA, linesB);
        }
        throw new InvalidOperationException("The link battle never ended on screen");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoPlayersFinishABattleOnTheirOwnScreens(bool doubles)
    {
        for (uint seed = 1; seed <= 6; seed++)
        {
            var (bench, a, b) = Screens(seed, doubles ? Doubles : null);
            var (linesA, linesB) = PlayOut(bench, a, b);
            var result = bench.Server.Result!;
            Assert.NotNull(result.Winner);
            Assert.Empty(bench.A.Refusals);
            Assert.Empty(bench.B.Refusals);

            // Each screen ends where the server did, from its own side: one won, the other lost
            var aWon = result.Winner == BattleSide.Player;
            Assert.Equal(aWon ? BattleResult.PlayerVictory : BattleResult.PlayerDefeat, a.Result);
            Assert.Equal(aWon ? BattleResult.PlayerDefeat : BattleResult.PlayerVictory, b.Result);

            // Neither screen worked anything out: its stage was never started
            Assert.Empty(a.Core.Log);
            Assert.Empty(b.Core.Log);
            Assert.Equal(0, a.Core.Turn);
            Assert.True(a.IsLink && b.IsLink);

            // Every line in its reader's own words: nothing of the markers, and nobody's own Pokémon called the foe's
            Assert.All(linesA.Concat(linesB), line => Assert.False(Narration.IsMarked(line), line));
            // (a species both brought is left out: the other's is the foe's by right)
            Assert.DoesNotContain(linesA, line => OwnAlone(bench.A).Any(name => line.Contains(Text.Of("battle.foe", ("pokemon", name)))));
            Assert.DoesNotContain(linesB, line => OwnAlone(bench.B).Any(name => line.Contains(Text.Of("battle.foe", ("pokemon", name)))));
            Assert.Contains(linesB, line => line.StartsWith(GoPrefix));
        }
    }

    /// <summary>The names of a player's own that the other player didn't bring too.</summary>
    private static IEnumerable<string> OwnAlone(LinkClient client) =>
        client.Own.Select(p => p.DisplayName).Except(client.FoeStandIns.Select(p => p.DisplayName));

    /// <summary>What a player's own send-out line begins with ("Go! ").</summary>
    private static string GoPrefix => Text.Of("battle.go", ("pokemon", "\u0000")).Split('\u0000')[0];

    [Fact]
    public void SideBsScreenStandsOnItsOwnSide()
    {
        var (bench, a, b) = Screens(3);
        // Read the opening on, to the first menu
        var lines = new List<string>();
        for (int i = 0; i < 2000 && b.HUD.MenuState != BattleMenuState.Main; i++) Act(b, lines);
        Assert.Equal(BattleMenuState.Main, b.HUD.MenuState);
        Assert.Contains(b.PlayerSlots[0].Pokemon!, bench.B.Own);
        Assert.Contains(b.EnemySlots[0].Pokemon!, bench.B.FoeStandIns);
        Assert.Contains(a.EnemySlots[0].Pokemon ?? bench.A.FoeStandIns[0], bench.A.FoeStandIns);
        // A foe stand-in's bar is drawn full on its own scale
        Assert.Equal(b.EnemySlots[0].Pokemon!.MaxHP, b.EnemySlots[0].Pokemon!.CurrentHP);
    }

    [Fact]
    public void TheScreenWaitsForTheOtherPlayerAndSaysSo()
    {
        var (bench, a, b) = Screens(5);
        var lines = new List<string>();
        // A reads on and chooses; B doesn't
        for (int i = 0; i < 4000 && !a.WaitingForOther; i++)
        {
            Act(a, lines);
            bench.Server.Pump();
        }
        Assert.True(a.WaitingForOther);
        Assert.Equal(Text.Of("linkRoom.waiting", ("player", "Dawn")), a.WaitingLine);
        Assert.Equal(BattleMenuState.Waiting, a.HUD.MenuState);
        // Nothing more comes until B answers: the server never sends A anything of B's choice
        int before = bench.LinkA.SentToPlayer.Count;
        for (int i = 0; i < 50; i++)
        {
            a.Update(0.05f);
            bench.Server.Pump();
        }
        Assert.Equal(before, bench.LinkA.SentToPlayer.Count);
        Assert.True(a.WaitingForOther);
    }

    [Fact]
    public void RunAnsweredYesGivesTheMatchUp()
    {
        var (bench, a, b) = Screens(7);
        var lines = new List<string>();
        for (int i = 0; i < 2000 && a.HUD.MenuState != BattleMenuState.Main; i++) Act(a, lines);
        a.SelectMainMenuOption(3);
        Assert.True(a.IsLearningMove);
        a.ChooseLearn(0);
        bench.Settle();
        Assert.Equal(LinkEnding.Forfeit, bench.Server.Result!.How);
        Assert.Equal(BattleSide.Enemy, bench.Server.Result.Winner);
    }

    [Fact]
    public void AForeignBarIsPutOnItsStandInsScale()
    {
        Assert.Equal(0, LinkScreenFeed.OnItsScale(0, 150));
        Assert.Equal(150, LinkScreenFeed.OnItsScale(48, 150));
        Assert.Equal(4, LinkScreenFeed.OnItsScale(1, 150));
        Assert.Equal(75, LinkScreenFeed.OnItsScale(24, 150));
        // A share the bar can draw stays the share: a 48th or more of the stand-in's HP for each pixel
        for (int bar = 1; bar <= 48; bar++) Assert.Equal(bar, SideView.Bar(LinkScreenFeed.OnItsScale(bar, 211), 211));
    }

    [Fact]
    public void AViewTurnedTwiceIsWhatItWas()
    {
        var bench = new LinkBench(9).Play();
        var roster = bench.A.Roster!;
        var once = BattleWire.Turned(bench.A.Events, roster);
        var twice = BattleWire.Turned(once, roster);
        Assert.Equal(BattleWire.WriteEvents(bench.A.Events, roster), BattleWire.WriteEvents(twice, roster));
        // Turned, the player's places are the other side's and the win the other's
        var firstEntry = LinkBench.Flatten(bench.A.Events).OfType<Entered>().First();
        var turnedEntry = LinkBench.Flatten(once).OfType<Entered>().First();
        Assert.Equal(firstEntry.Place.Side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player, turnedEntry.Place.Side);
        Assert.Same(firstEntry.Pokemon, turnedEntry.Pokemon);
        var ended = bench.A.Events.OfType<Ended>().Single().Result;
        var turnedEnded = once.OfType<Ended>().Single().Result;
        Assert.Equal(ended == BattleResult.PlayerVictory ? BattleResult.PlayerDefeat : BattleResult.PlayerVictory, turnedEnded);
        Assert.Equal(turnedEnded == BattleResult.PlayerVictory, once.OfType<Won>().Any());
    }

    [Fact]
    public void ALinkBattlesLinesReadRightToBothPlayers()
    {
        Assert.Equal("abc", Narration.For("abc", BattleSide.Player));
        var bench = new LinkBench(11).Play();
        // The server's log carries both readings; each side's view one
        Assert.Contains(bench.Server.Core!.Log.OfType<Said>(), s => Narration.IsMarked(s.Text));
        Assert.DoesNotContain(bench.A.Events.OfType<Said>(), s => Narration.IsMarked(s.Text));
        Assert.DoesNotContain(bench.B.Events.OfType<Said>(), s => Narration.IsMarked(s.Text));
        Assert.Contains(bench.A.Events.OfType<Said>(), s => s.Text.StartsWith(Text.Of("battle.go", ("pokemon", bench.A.Own[0].DisplayName))));
        Assert.Contains(bench.B.Events.OfType<Said>(), s => s.Text.StartsWith(Text.Of("battle.go", ("pokemon", bench.B.Own[0].DisplayName))));
        // Each reads the other's send-out as the other player's
        Assert.Contains(bench.A.Events.OfType<Said>(), s => s.Text.Contains("Dawn") && s.Text.Contains(bench.A.FoeStandIns.First(f => bench.B.Own[0].Species == f.Species).DisplayName));
    }

    [Fact]
    public void NoBattleButALinkBattleIsWrittenForBothSides()
    {
        // The game's own battles are told from the player's side as they always were (the golden battles hold the words)
        var party = new Party();
        party.Add(new Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Turtwig")!, 10, new BattleRandom(1)));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party,
            WildPokemon = new List<Pokemon> { new(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Starly")!, 5, new BattleRandom(2)) },
            Random = new BattleRandom(3)
        });
        core.Start();
        Assert.DoesNotContain(core.Log.OfType<Said>(), s => Narration.IsMarked(s.Text));
        Assert.False(Narration.Neutral);
    }
}
