using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests.Link;

/// <summary>
/// Four at one machine (plan 13 · V21): two trainers a side with a place each, a side's two asked on its one screen one
/// after the other, the curtain between the sides alone. The battle ends only as one side has both its teams down, its
/// record replays, each fainted place is filled by its own trainer from their own team, and nothing brought changes.
/// </summary>
public class VersusMultiTests
{
    private static readonly string[] FourNames = { "Ana", "Bea", "Cris", "Dan" };

    private static LinkPlayer[] Players(uint seed) => Enumerable.Range(0, 4)
        .Select(i => new LinkPlayer(FourNames[i], LinkBench.Team(seed * 4 + (uint)i + 1, LinkRoom.Default))).ToArray();

    private static VersusMatch Match(LinkPlayer[] p, int picks, uint seed) =>
        VersusMatch.OfFour(p[0], p[1], p[2], p[3], GameEngine.VersusRoom(picks, picks, picks, picks), seed);

    private static bool CanReshape(Pokemon p) =>
        p.AbilityName == "Imposter" || p.Moves.Any(m => m.Data.Name is "Transform" or "Mimic" or "Sketch");

    private sealed class Seen
    {
        public int Curtains, PartnerTurns, Switches, Replacements;
        public readonly HashSet<int> ReplacedSeats = new();
        // A trainer's whole team down while their partner fought on, by seat
        public readonly HashSet<int> FoughtOnWithoutSeat = new();
    }

    /// <summary>
    /// Plays a match as four people would: the shown screen answered at whichever of its two places it asks, now and then
    /// with a switch, the curtain lifted when it comes down. What must hold at every question is held as it comes.
    /// </summary>
    private static Seen PlayOut(VersusMatch match, uint seed)
    {
        var seen = new Seen();
        var rng = new Random((int)seed);
        var lines = new List<string>();
        for (int i = 0; i < 600_000; i++)
        {
            if (match.Curtain is { } waiting)
            {
                // The curtain only ever stands between the sides
                Assert.NotEqual(match.Shown, waiting);
                Assert.Equal(waiting * 2, match.Seat);
                seen.Curtains++;
                match.PassTo();
            }
            var face = match.Face;
            if (!Answer(match, face, rng, seen, lines))
            {
                match.Frame(0.05f);
                if (match.Over) return seen;
                continue;
            }
            match.Frame(0.05f);
        }
        throw new InvalidOperationException($"The battle of four never ended (seed {seed})");
    }

    private static bool Answer(VersusMatch match, BattleEngine face, Random rng, Seen seen, List<string> lines)
    {
        face.Update(0.05f);
        if (face.IsBattleOver) return false;
        if (face.IsWaitingForConfirm)
        {
            lines.Add(face.CurrentMessage);
            face.ConfirmMessage();
            return true;
        }
        var asked = face.LinkClient!.Asked;
        if (asked == null) return true;
        var state = face.HUD.MenuState;
        if (state is not (BattleMenuState.Main or BattleMenuState.SwitchPokemon)) return true;

        // The menus are open for one of the side's two places: its own team, its own trainer's name on the pill
        var menu = face.MenuBattler;
        int slot = menu.Slot;
        var client = face.LinkClient!;
        var team = slot == 0 ? client.Own : client.PartnerOwn;
        Assert.Equal(team, face.SwitchParty.Members);
        Assert.Contains(menu.Pokemon!, team);
        string who = slot == 0 ? client.Name : client.Seats!.Partner.Name;
        Assert.Equal(Text.Of("battleMenu.playersTurn", ("player", who)), face.TurnPill);
        Assert.Equal(match.Shown * 2 + slot, match.Seat);
        if (slot == 1) seen.PartnerTurns++;
        // A trainer with nobody left is asked nothing; the other fights on
        for (int s = 0; s < 2; s++)
        {
            var theirs = s == 0 ? client.Own : client.PartnerOwn;
            if (theirs.All(p => p.IsFainted)) seen.FoughtOnWithoutSeat.Add(match.Shown * 2 + s);
        }

        // The question is turned to this screen: its places are on the player's side
        var places = client.Side == BattleSide.Enemy ? asked.Options.Select(o => BattleWire.Turned(o)).ToList() : asked.Options.ToList();
        var option = places.First(o => o.Place.Slot == slot);

        if (state == BattleMenuState.SwitchPokemon && face.IsChoosingReplacement)
        {
            seen.Replacements++;
            seen.ReplacedSeats.Add(match.Shown * 2 + slot);
            // (a fainted one, or one leaving by its own move): the question is for the very place the menus are open for
            Assert.True(option.MustSendIn);
            Assert.Single(places);
            face.SelectSwitch(option.Switches[rng.Next(option.Switches.Count)]);
            return true;
        }
        if (state != BattleMenuState.Main) return true;
        if (option.Switches.Count > 0 && rng.Next(6) == 0)
        {
            face.SelectMainMenuOption(2);
            face.SelectSwitch(option.Switches[rng.Next(option.Switches.Count)]);
            seen.Switches++;
            return true;
        }
        face.SelectMainMenuOption(0);
        if (option.Moves.Count == 0) return true;
        face.SelectMove(option.Moves[rng.Next(option.Moves.Count)]);
        if (face.HUD.MenuState == BattleMenuState.SelectTarget) face.SelectTarget(rng.Next(face.TargetChoices.Count));
        return true;
    }

    [Fact]
    public void FourFinishBattlesThroughTheirScreensAndTheRecordsReplay()
    {
        var all = new List<Seen>();
        int fought = 0;
        for (uint seed = 1; fought < 40; seed++)
        {
            var players = Players(seed);
            // Left out: teams that can take another's shape or moves. After a Transform a link screen keeps its own move
            // list while the room offers the copied one (plan 28's screen, two players too; plan 13, V21's entry)
            if (players.Any(p => p.Team.Select(t => t.ToPokemon(Ruleset.Modern)).Any(CanReshape))) continue;
            fought++;
            string before = System.Text.Json.JsonSerializer.Serialize(players.SelectMany(p => p.Team).Select(t => t.Pokemon));
            var match = Match(players, seed % 2 == 0 ? 3 : 6, seed);
            var seen = PlayOut(match, seed);
            all.Add(seen);
            var core = match.Server.Core!;
            var result = match.Server.Result!;
            Assert.NotNull(result.Winner);
            Assert.All(match.Faces.Select(f => f.LinkClient!), c => Assert.Empty(c.Refusals));

            // The loser has nobody left in either team, and the winner somebody
            var teamsA = core.PlayerParty.Members.Concat(core.Partner!.Party.Members);
            var teamsB = core.Trainers.SelectMany(t => t.Party.Members);
            var (lost, won) = result.Winner == BattleSide.Player ? (teamsB, teamsA) : (teamsA, teamsB);
            Assert.All(lost, p => Assert.True(p.IsFainted, $"seed {seed}: a loser's Pokémon still stands"));
            Assert.Contains(won, p => !p.IsFainted);

            // Each screen ends from its own side, and holds its own two teams as the rules do
            for (int i = 0; i < 400 && match.Faces.Any(f => !f.IsBattleOver); i++)
                foreach (var f in match.Faces)
                {
                    f.Update(0.05f);
                    if (f.IsWaitingForConfirm) f.ConfirmMessage();
                }
            bool firstWon = result.Winner == BattleSide.Player;
            Assert.Equal(firstWon ? BattleResult.PlayerVictory : BattleResult.PlayerDefeat, match.Faces[0].Result);
            Assert.Equal(firstWon ? BattleResult.PlayerDefeat : BattleResult.PlayerVictory, match.Faces[1].Result);
            var mineA = match.Faces[0].LinkClient!;
            Assert.Equal(teamsA.Select(p => p.CurrentHP), mineA.Own.Concat(mineA.PartnerOwn).Select(p => p.CurrentHP));
            var mineB = match.Faces[1].LinkClient!;
            Assert.Equal(teamsB.Select(p => p.CurrentHP), mineB.Own.Concat(mineB.PartnerOwn).Select(p => p.CurrentHP));

            // The server's record replays to the same log
            var again = new BattleCore(match.Server.Setup.Battle(match.Server.PickOf(BattleSide.Player)!, match.Server.PickOf(BattleSide.Enemy)!, seed));
            Assert.True(core.Record!.FriendPartner);
            again.Replay(core.Record!);
            Assert.Equal(core.Log.Count, again.Log.Count);
            Assert.Equal(core.Result, again.Result);

            // Nothing any of the four brought changed
            Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(players.SelectMany(p => p.Team).Select(t => t.Pokemon)));
        }
        Assert.All(all, s => Assert.True(s.Curtains > 1));
        Assert.True(all.Sum(s => s.PartnerTurns) > 100);
        Assert.True(all.Sum(s => s.Switches) > 20);
        // Every one of the four was asked their own replacement, and each of the four was once out while their partner fought on
        Assert.Equal(new[] { 0, 1, 2, 3 }, all.SelectMany(s => s.ReplacedSeats).Distinct().OrderBy(s => s));
        Assert.Equal(new[] { 0, 1, 2, 3 }, all.SelectMany(s => s.FoughtOnWithoutSeat).Distinct().OrderBy(s => s));
    }

    [Fact]
    public void TheCurtainNeverComesBetweenPartners()
    {
        var match = Match(Players(7), 3, 7);
        var rng = new Random(7);
        var seen = new Seen();
        var lines = new List<string>();
        // The first side's two choose one after the other with no curtain, which comes down once both have
        for (int i = 0; i < 4000 && match.Face.MenuBattler.Slot == 0; i++)
        {
            Assert.Null(match.Curtain);
            Answer(match, match.Face, rng, new Seen(), lines);
            match.Frame(0.05f);
        }
        Assert.Null(match.Curtain);
        Assert.Equal(1, match.Face.MenuBattler.Slot);
        Assert.Equal(1, match.Seat);
        for (int i = 0; i < 4000 && match.Curtain == null; i++)
        {
            Answer(match, match.Face, rng, seen, lines);
            match.Frame(0.05f);
        }
        Assert.Equal(1, match.Curtain);
        Assert.Equal(2, match.Seat);
        Assert.Equal(Text.Of("battle.names", ("some", "Cris"), ("last", "Dan")), match.Names[1]);
    }

    [Fact]
    public void EachSideReadsTheOtherTwoAtTheOpening()
    {
        var match = Match(Players(11), 3, 11);
        var log = match.Faces.Select(f => f.LinkClient!.Events.OfType<Said>().Select(s => s.Text).ToList()).ToArray();
        string Class(string name) => Text.Of("battle.fullTitle", ("class", PokemonPlatinumEngine.Models.Trainer.ClassName(LinkSetup.PlayerClass)), ("name", name));
        Assert.Equal(Text.Of("battle.wouldLikeToBattle", ("trainer", Class("Cris")), ("trainer2", Class("Dan"))), log[0][0]);
        Assert.Equal(Text.Of("battle.wouldLikeToBattle", ("trainer", Class("Ana")), ("trainer2", Class("Bea"))), log[1][0]);
        // Each screen's four places stand as its own side sees them
        foreach (var (face, side) in match.Faces.Select((f, i) => (f, i)))
        {
            Assert.Equal(2, face.PlayerSlots.Count);
            Assert.Equal(2, face.EnemySlots.Count);
            Assert.True(face.HasFriendPartner);
            var foes = face.LinkClient!.FoeStandIns;
            Assert.Equal(6, foes.Count);
        }
    }

    [Fact]
    public void ASideGivesUpForBothOfItsTrainers()
    {
        var match = Match(Players(13), 3, 13);
        var lines = new List<string>();
        for (int i = 0; i < 4000 && match.Face.HUD.MenuState != BattleMenuState.Main; i++)
        {
            Answer(match, match.Face, new Random(1), new Seen(), lines);
            if (match.Face.HUD.MenuState != BattleMenuState.Main) match.Frame(0.05f);
        }
        match.Face.SelectMainMenuOption(3);
        Assert.True(match.Face.IsLearningMove);
        match.Face.ChooseLearn(0);
        for (int i = 0; i < 20; i++) match.Frame(0.05f);
        Assert.Equal(LinkEnding.Forfeit, match.Server.Result!.How);
        Assert.Equal(BattleSide.Enemy, match.Server.Result.Winner);
    }
}
