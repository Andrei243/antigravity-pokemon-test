using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumTests.Golden;
using BattleText = PokemonPlatinumTests.Golden.BattleText;

namespace PokemonPlatinumTests.Link;

/// <summary>
/// Plan 28 · I3 and I4: two players battling over loopback links, both sides asked from outside, a server that sends
/// each only its own view, and a link battle's setup, giving up and timer.
/// </summary>
public class LinkBattleBenchTests
{
    private static readonly LinkRoom Doubles = LinkRoom.Default with { Format = BattleFormat.Double };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoPlayersPlayWholeBattlesOverTheLoopback(bool doubles)
    {
        for (uint seed = 1; seed <= 24; seed++)
        {
            var bench = new LinkBench(seed, doubles ? Doubles : null).Play();
            var result = bench.Server.Result!;
            Assert.Equal(LinkEnding.KnockedOut, result.How);
            Assert.NotNull(result.Winner);
            Assert.Equal(result, bench.A.Result);
            Assert.Equal(result, bench.B.Result);
            Assert.Empty(bench.A.Refusals);
            Assert.Empty(bench.B.Refusals);
            Assert.Equal(doubles ? BattleFormat.Double : BattleFormat.Single, bench.Server.Core!.Format);

            // Both players' views end where the rules did: the same winner, and as many of each side fainted
            var core = bench.Server.Core;
            var expected = core.Result;
            Assert.Equal(expected, bench.A.State.Result);
            Assert.Equal(expected, bench.B.State.Result);
            int playerDown = LinkBench.Flatten(core.Log).OfType<Fainted>().Count(f => f.Place.Side == BattleSide.Player);
            int enemyDown = LinkBench.Flatten(core.Log).OfType<Fainted>().Count(f => f.Place.Side == BattleSide.Enemy);
            Assert.Equal(playerDown, LinkBench.Flatten(bench.A.Events).OfType<Fainted>().Count(f => f.Place.Side == BattleSide.Player));
            Assert.Equal(enemyDown, LinkBench.Flatten(bench.B.Events).OfType<Fainted>().Count(f => f.Place.Side == BattleSide.Enemy));

            // Every answer the rules took came from outside: neither side had a chooser inside
            Assert.All(core.Record!.Answers, turn => Assert.NotEmpty(turn));
            Assert.Contains(core.Record.Answers.SelectMany(t => t), c => c.Who.Side == BattleSide.Enemy);
        }
    }

    [Fact]
    public void NeitherPlayerIsSentTheOthersHiddenThings()
    {
        for (uint seed = 1; seed <= 16; seed++)
        {
            var bench = new LinkBench(seed).Play();
            var core = bench.Server.Core!;
            var log = LinkBench.Flatten(core.Log).ToList();
            foreach (var (side, link, foes, mine) in new[]
            {
                (BattleSide.Player, bench.LinkA, bench.Server.Setup.TeamB.ToList(), bench.Server.Setup.TeamA.ToList()),
                (BattleSide.Enemy, bench.LinkB, bench.Server.Setup.TeamA.ToList(), bench.Server.Setup.TeamB.ToList())
            })
            {
                string sent = string.Join("\n", link.SentToPlayer);
                // What the player's own side knows (the teams as brought: a berry eaten is gone from the battle's own), which can't count against the view
                var known = mine.SelectMany(p => p.Moves.Select(m => m.Name).Append(p.HeldItem?.Name ?? "").Append(p.AbilityName ?? ""))
                    .Concat(mine.Select(p => p.Species.Name)).Concat(foes.Select(p => p.Species.Name))
                    // And the names of types, which a move's own type field says whoever used it (Psychic the move and the type)
                    .Concat(Enum.GetNames<PokemonType>()).Where(n => n.Length > 0).ToHashSet();

                // The foe's moves that were never used, its items and abilities never shown, cross in no message
                var used = log.OfType<MoveShown>().Where(m => m.From.Side != side).Select(m => m.Move)
                    .Concat(log.OfType<Charging>().Where(c => c.Place.Side != side).Select(c => c.Move))
                    // A move is shown as it is used: the line of its use, whether or not it reaches anyone (Grudge) or works (Last Resort)
                    .Concat(core.Log.OfType<Said>()
                        .SelectMany(s => foes.SelectMany(p => p.Moves).Where(m => s.Text.Contains($" used {m.DisplayName}!")).Select(m => m.Name)))
                    .ToHashSet();
                var shownItems = log.OfType<ItemShown>().Where(i => i.Place.Side != side).Select(i => i.Item).ToHashSet();
                var shownAbilities = log.OfType<AbilityShown>().Where(a => a.Place.Side != side).Select(a => a.Ability).ToHashSet();
                var secret = foes.SelectMany(p => p.Moves.Select(m => m.Name)).Where(n => !used.Contains(n))
                    .Concat(foes.Select(p => p.HeldItem?.Name).OfType<string>().Where(n => !shownItems.Contains(n)))
                    .Concat(foes.Select(p => p.AbilityName).OfType<string>().Where(n => !shownAbilities.Contains(n)))
                    .Where(n => !known.Contains(n)).Distinct().ToList();
                foreach (var name in secret)
                {
                    // A name inside a longer one (Acid in Gastro Acid) is the longer one's
                    string text = Names.Where(n => n.Length > name.Length && n.Contains(name)).OrderByDescending(n => n.Length).Aggregate(sent, (s, n) => s.Replace(n, " "));
                    Assert.False(text.Contains($"\"{name}\"") || text.Contains($" {name}!") || text.Contains($" {name}."), $"seed {seed}: {side} was sent {name}: " + (new[] { $"\"{name}\"", $" {name}!", $" {name}." }.Select(k => text.IndexOf(k, StringComparison.Ordinal)).Where(i => i >= 0).DefaultIfEmpty(-1).First() is var at && at >= 0 ? text.Substring(Math.Max(0, at - 200), Math.Min(400, text.Length - Math.Max(0, at - 200))) : ""));
                }

                // Only its own places are ever asked of it, and the foe's HP only as the bar
                foreach (var text in link.SentToPlayer)
                {
                    using var json = JsonDocument.Parse(text);
                    var message = json.RootElement.GetProperty("message");
                    string kind = message.GetProperty(BattleWire.TypeField).GetString()!;
                    Assert.Contains(kind, new[] { nameof(LinkHello), nameof(LinkTurn), nameof(LinkAsk), nameof(LinkOver) });
                    if (kind == nameof(LinkAsk))
                        foreach (var option in message.GetProperty("options").EnumerateArray())
                            Assert.Equal(side.ToString(), option.GetProperty("place").GetProperty("side").GetString());
                    foreach (var hp in Values(message, "hp", foeOf: side)) Assert.InRange(hp, 0, SideView.BarPixels);
                    // A foe is named by its place in the preview: never past it, never by the team's own order
                    foreach (var index in Refs(message, foeOf: side)) Assert.InRange(index, 0, 5);
                }

                // The foe's six as the preview shows them, whatever order they were brought or picked in
                var hello = (LinkHello)BattleWire.ReadMessage(link.SentToPlayer[0], null);
                Assert.Equal(hello.FoePreview.OrderBy(e => e.Dex).ToList(), hello.FoePreview);
            }
        }
    }

    private static readonly List<string> Names = MoveDatabase.GetAll().Select(m => m.Name).Concat(ItemDatabase.GetAll().Select(i => i.Name))
        .Concat(AbilityDatabase.GetAll().Select(a => a.Name)).Concat(PokemonDatabase.Everyone().Select(s => s.Name)).Distinct().ToList();

    /// <summary>The numbers under a field of every object of a message that stands in a foe's place.</summary>
    private static IEnumerable<int> Values(JsonElement e, string field, BattleSide foeOf)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            if (e.TryGetProperty("place", out var place) && place.ValueKind == JsonValueKind.Object && place.GetProperty("side").GetString() != foeOf.ToString()
                && e.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.Number)
                yield return value.GetInt32();
            foreach (var property in e.EnumerateObject())
                foreach (var v in Values(property.Value, field, foeOf)) yield return v;
        }
        else if (e.ValueKind == JsonValueKind.Array)
            foreach (var item in e.EnumerateArray())
                foreach (var v in Values(item, field, foeOf)) yield return v;
    }

    /// <summary>The indices of every reference to a foe's Pokémon in a message.</summary>
    private static IEnumerable<int> Refs(JsonElement e, BattleSide foeOf)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            if (e.TryGetProperty("party", out var party) && e.TryGetProperty("index", out var index) && e.TryGetProperty("side", out var side)
                && side.GetString() != foeOf.ToString())
            {
                Assert.Equal(0, party.GetInt32());
                yield return index.GetInt32();
            }
            foreach (var property in e.EnumerateObject())
                foreach (var v in Refs(property.Value, foeOf)) yield return v;
        }
        else if (e.ValueKind == JsonValueKind.Array)
            foreach (var item in e.EnumerateArray())
                foreach (var v in Refs(item, foeOf)) yield return v;
    }

    [Fact]
    public void OnePlayersChoiceWaitsUnseenForTheOthers()
    {
        var bench = new LinkBench(7, autoAnswerB: false);
        bench.Settle();
        // A has answered the first question; B hasn't
        Assert.Null(bench.A.Asked);
        Assert.NotNull(bench.B.Asked);
        Assert.Equal(Text.Of("linkRoom.waiting", ("player", "Dawn")), bench.A.WaitingLine);
        Assert.Equal("", bench.B.WaitingLine);
        int sentToA = bench.LinkA.SentToPlayer.Count, sentToB = bench.LinkB.SentToPlayer.Count;
        bench.Settle();
        // Nothing more for anyone until B answers: no turn, and nothing of A's choice
        Assert.Equal(sentToA, bench.LinkA.SentToPlayer.Count);
        Assert.Equal(sentToB, bench.LinkB.SentToPlayer.Count);

        bench.B.Answer(new SeededChooser(1).Choose(bench.B, bench.B.Asked!));
        bench.Settle();
        Assert.True(bench.LinkA.SentToPlayer.Count > sentToA);
        Assert.Contains(bench.A.Events, e => e is Said);
    }

    [Fact]
    public void AFixedSeedPlaysTheSameBattleAndItsRecordReplaysIt()
    {
        for (uint seed = 1; seed <= 8; seed++)
        {
            var first = new LinkBench(seed).Play();
            var second = new LinkBench(seed).Play();
            Assert.Equal(first.LinkA.SentToPlayer, second.LinkA.SentToPlayer);
            Assert.Equal(first.LinkB.SentToPlayer, second.LinkB.SentToPlayer);

            // The server's record, played on the same picks and seed with nobody asked, is the same battle
            var server = first.Server;
            var replayed = new BattleCore(server.Setup.Battle(server.PickOf(BattleSide.Player)!, server.PickOf(BattleSide.Enemy)!, seed));
            replayed.Replay(server.Core!.Record!);
            Assert.Equal(BattleText.Written(server.Core), BattleText.Written(replayed));

            // And the Vs. Recorder's file of it fights it again by itself
            var file = server.Opening!.Closing(server.Core, DateTime.MinValue)!;
            var read = BattleRecordFile.Read(file.Serialize()).File!;
            Assert.True(read.Link);
            Assert.Equal(BattleText.Written(server.Core), BattleText.Written(read.Replay()));
        }
    }

    [Fact]
    public void APlayerMayGiveUpAtAnyQuestionAndLosesTheMatch()
    {
        var bench = new LinkBench(3);
        // Two turns in
        for (int i = 0; i < 3; i++)
        {
            bench.A.Pump();
            bench.B.Pump();
            bench.Server.Pump();
        }
        Assert.Null(bench.Server.Result);
        bench.B.Forfeit();
        bench.Settle();
        var result = bench.Server.Result!;
        Assert.Equal(new LinkResult(BattleSide.Player, bench.Server.Core!.Turn, LinkEnding.Forfeit), result);
        Assert.Equal(BattleResult.PlayerVictory, bench.Server.Core.Result);
        Assert.IsType<Ended>(bench.Server.Core.Log[^1]);
        Assert.Equal(result, bench.A.Result);

        // The record keeps the forfeit: a replay ends where the battle did
        var server = bench.Server;
        var replayed = new BattleCore(server.Setup.Battle(server.PickOf(BattleSide.Player)!, server.PickOf(BattleSide.Enemy)!, server.Seed));
        replayed.Replay(server.Core.Record!);
        Assert.Equal(BattleText.Written(server.Core), BattleText.Written(replayed));
    }

    [Fact]
    public void GivingUpAndRunningAreALinkBattlesAlone()
    {
        var corpus = new BattleCorpus(8);
        corpus.Core.Start();
        Assert.NotNull(corpus.Core.WhyNot(BattleChoice.Forfeit(new Place(BattleSide.Player, 0))));

        var bench = new LinkBench(9);
        bench.Settle();
        Assert.NotNull(bench.Server.Core!.WhyNot(BattleChoice.Run(new Place(BattleSide.Player, 0))));
    }

    [Fact]
    public void ATimerThatRunsOutAnswersForThePlayerAndTheThirdGivesTheMatchUp()
    {
        var bench = new LinkBench(11, autoAnswerB: false);
        bench.Settle();
        var core = bench.Server.Core!;
        var place = bench.Server.PlacesOf(BattleSide.Enemy).Single();
        var expected = core.DefaultChoice(place);
        // The first move it may use
        Assert.Equal(ChoiceKind.Fight, expected.Kind);
        Assert.Equal(Enumerable.Range(0, 4).First(i => core.WhyNot(BattleChoice.Fight(place, i)) == null), expected.Move);

        int answers = core.Record!.Answers.Count;
        bench.Server.TimeOut(BattleSide.Enemy);
        Assert.Equal(answers + 1, core.Record!.Answers.Count);
        Assert.Contains(expected, core.Record.Answers[^1]);

        bench.Settle();
        bench.Server.TimeOut(BattleSide.Enemy);
        bench.Settle();
        Assert.Null(bench.Server.Result);
        bench.Server.TimeOut(BattleSide.Enemy);
        bench.Settle();
        Assert.Equal(LinkEnding.Timer, bench.Server.Result!.How);
        Assert.Equal(BattleSide.Player, bench.Server.Result.Winner);
    }

    [Fact]
    public void ADroppedPlayerIsBroughtBackToTheSameBattle()
    {
        var bench = new LinkBench(13);
        for (int i = 0; i < 4; i++)
        {
            bench.A.Pump();
            bench.B.Pump();
            bench.Server.Pump();
        }
        // What the old link had brought in before it dropped
        bench.A.Pump();
        var before = bench.A.Events.ToList();
        var back = bench.Reconnect(13 * 7 + 3);
        back.Pump();
        // The whole view so far again, event for event
        Assert.Equal(BattleWire.WriteEvents(before, Roster(bench.A)), BattleWire.WriteEvents(back.Events, Roster(back)));
        for (int round = 0; round < 10_000 && bench.Server.Result == null; round++)
        {
            back.Pump();
            bench.B.Pump();
            bench.Server.Pump();
        }
        back.Pump();
        Assert.NotNull(bench.Server.Result);
        Assert.Equal(bench.Server.Result, back.Result);
    }

    private static BattleRoster Roster(LinkClient client) =>
        new(BattleRoster.Team(client.Side, 0, client.Own).Concat(BattleRoster.Team(client.Side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player, 0, client.FoeStandIns)));

    [Fact]
    public void ARoomChecksItsClausesAndLevelsItsTeams()
    {
        var room = LinkRoom.Default;
        var team = LinkBench.Team(5, room);
        Assert.Empty(LinkSetup.Check(room, new LinkPlayer("Lucas", team)));

        // Two of one species, two holding one item, an Egg, seven, a species the rules don't have
        var twin = team.Select(Clone).ToList();
        twin[1] = Clone(twin[0]);
        Assert.Contains(Text.Of("linkRoom.fault.speciesClause"), LinkSetup.Check(room, new LinkPlayer("Lucas", twin)));
        Assert.Empty(LinkSetup.Check(room with { SpeciesClause = false }, new LinkPlayer("Lucas", twin)).Where(f => f == Text.Of("linkRoom.fault.speciesClause")));
        var items = team.Select(Clone).ToList();
        items[0].Pokemon.HeldItem = "Leftovers";
        items[1].Pokemon.HeldItem = "Leftovers";
        Assert.Contains(Text.Of("linkRoom.fault.itemClause"), LinkSetup.Check(room, new LinkPlayer("Lucas", items)));
        var egg = team.Select(Clone).ToList();
        egg[2].Pokemon.IsEgg = true;
        Assert.Contains(Text.Of("linkRoom.fault.egg"), LinkSetup.Check(room, new LinkPlayer("Lucas", egg)));
        Assert.NotEmpty(LinkSetup.Check(room, new LinkPlayer("Lucas", team.Append(Clone(team[0])).ToList())));
        var later = team.Select(Clone).ToList();
        later[3] = RecordedPokemon.Of(new Pokemon(PokemonDatabase.Get("Sprigatito")!, 30, new BattleRandom(1)));
        Assert.NotEmpty(LinkSetup.Check(room, new LinkPlayer("Lucas", later)));
        Assert.Empty(LinkSetup.Check(room with { Rules = RulesPreset.Modern }, new LinkPlayer("Lucas", later)).Where(f => f.Contains("Sprigatito")));
        Assert.Throws<LinkRefusedException>(() => LinkSetup.Bring(room, new LinkPlayer("Lucas", twin), new LinkPlayer("Dawn", team)));

        // Flat 50, Flat 100, as it is: copies, whole, and the team brought unchanged
        foreach (var (level, wanted) in new[] { (LinkLevel.FlatFifty, 50), (LinkLevel.FlatHundred, 100) })
            Assert.All(LinkSetup.Copies(room with { Level = level }, team), p => Assert.Equal(wanted, p.Level));
        var asIs = LinkSetup.Copies(room with { Level = LinkLevel.AsIs }, team);
        Assert.Equal(team.Select(r => r.Pokemon.Level), asIs.Select(p => p.Level));
        Assert.All(asIs, p => Assert.Equal(p.MaxHP, p.CurrentHP));

        // Pick three of six: three, each once
        var setup = LinkSetup.Bring(room with { Picks = 3 }, new LinkPlayer("Lucas", team), new LinkPlayer("Dawn", LinkBench.Team(6, room)));
        Assert.Null(setup.WhyNot(new[] { 5, 0, 2 }, BattleSide.Player));
        Assert.NotNull(setup.WhyNot(new[] { 5, 0 }, BattleSide.Player));
        Assert.NotNull(setup.WhyNot(new[] { 5, 5, 2 }, BattleSide.Player));
        var battle = new BattleCore(setup.Battle(new[] { 5, 0, 2 }, new[] { 1, 2, 3 }, 1));
        Assert.Equal(3, battle.PlayerParty.Count);
        Assert.Equal(setup.TeamA[5].Species, battle.PlayerParty.Members[0].Species);
        Assert.True(battle.Link && battle.Frontier);
    }

    [Fact]
    public void ARoomOfPickThreeAndModernRulesPlaysThrough()
    {
        var bench = new LinkBench(17, LinkRoom.Default with { Picks = 3, Rules = RulesPreset.Modern, Level = LinkLevel.FlatHundred }).Play();
        Assert.Equal(3, bench.Server.Core!.PlayerParty.Count);
        Assert.Equal(6, bench.A.FoeStandIns.Count);
        Assert.Same(Ruleset.Modern, bench.Server.Core.Rules);
    }

    private static RecordedPokemon Clone(RecordedPokemon r) =>
        JsonSerializer.Deserialize<RecordedPokemon>(JsonSerializer.Serialize(r, GameDataFiles.Json), GameDataFiles.Json)!;
}
