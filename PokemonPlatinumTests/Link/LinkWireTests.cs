using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumTests.Golden;
using BattleText = PokemonPlatinumTests.Golden.BattleText;

namespace PokemonPlatinumTests.Link;

/// <summary>Plan 28 · I1: the log, the questions and the answers on the wire, and nothing in the rules that two runs could part on.</summary>
public class LinkWireTests
{
    private static IEnumerable<Type> Kinds<T>() =>
        typeof(BattleEvent).Assembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract);

    [Fact]
    public void EveryEventQuestionAndMessageHasAWireName()
    {
        // A new event of the log that nobody gave a wire name fails here, before it can go out as something else
        Assert.Empty(Kinds<BattleEvent>().Except(BattleWire.EventTypes).Select(t => t.Name));
        Assert.Empty(Kinds<BattleRequest>().Except(BattleWire.RequestTypes).Select(t => t.Name));
        Assert.Empty(Kinds<LinkMessage>().Except(BattleWire.MessageTypes).Select(t => t.Name));
        var names = BattleWire.EventTypes.Concat(BattleWire.RequestTypes).Concat(BattleWire.MessageTypes).Select(t => t.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void EveryGoldenBattlesLogCrossesTheWireUnchanged()
    {
        for (int seed = 1; seed <= BattleCorpus.Battles; seed++)
        {
            var corpus = new BattleCorpus(seed);
            corpus.Core.Start();
            var roster = BattleRoster.Of(corpus.Core);
            string json = BattleWire.WriteEvents(corpus.Core.Log, roster);

            // Read back on the same teams: the same battle, event for event, and the same text again
            var back = BattleWire.ReadEvents(json, roster);
            var text = BattleText.Of(corpus.Core);
            Assert.Equal(text.Lines(corpus.Core.Log), text.Lines(back));
            Assert.Equal(json, BattleWire.WriteEvents(back, roster));

            // And on another machine's copies of the same teams: the replay's, named by the same references
            var again = new BattleCorpus(seed);
            var elsewhere = BattleWire.ReadEvents(json, BattleRoster.Of(again.Core));
            again.Core.Start();
            Assert.Equal(BattleText.Of(again.Core).Lines(again.Core.Log), BattleText.Of(again.Core).Lines(elsewhere));

            // No Pokémon object crosses: only references
            Assert.DoesNotContain("\"species\":{", json);
        }
    }

    [Fact]
    public void ChoicesAndQuestionsCrossTheWire()
    {
        var choices = new List<BattleChoice>
        {
            BattleChoice.Fight(new Place(BattleSide.Player, 0), 2, new Place(BattleSide.Enemy, 1)) with { Mega = true, Terastallize = true },
            BattleChoice.Switch(new Place(BattleSide.Player, 1), 4),
            BattleChoice.UseItem(new Place(BattleSide.Enemy, 0), "Potion", 1, 2),
            BattleChoice.Forfeit(new Place(BattleSide.Enemy, 0)),
            BattleChoice.GoOn(new Place(BattleSide.Player, 0)) with { Rotation = -1 }
        };
        var back = BattleWire.ReadChoices(BattleWire.WriteChoices(choices));
        Assert.Equal(choices, back);

        var corpus = new BattleCorpus(4);
        var roster = BattleRoster.Of(corpus.Core);
        var requests = new BattleRequest[]
        {
            new ActionRequest(new[] { new Place(BattleSide.Player, 0), new Place(BattleSide.Enemy, 1) }),
            new ReplacementRequest(new Place(BattleSide.Enemy, 0)),
            new SwitchOfferRequest(new Place(BattleSide.Player, 0), corpus.Core.Trainers[0].Party.Members[^1])
        };
        foreach (var request in requests)
        {
            string json = BattleWire.WriteRequest(request, roster);
            var read = BattleWire.ReadRequest(json, roster);
            Assert.Equal(request.GetType(), read.GetType());
            Assert.Equal(json, BattleWire.WriteRequest(read, roster));
        }
        var offer = (SwitchOfferRequest)BattleWire.ReadRequest(BattleWire.WriteRequest(requests[2], roster), roster);
        Assert.Same(corpus.Core.Trainers[0].Party.Members[^1], offer.Upcoming);
    }

    [Fact]
    public void AnotherProtocolOrSomethingUnknownIsRefused()
    {
        string json = BattleWire.WriteChoices(new[] { BattleChoice.Run(new Place(BattleSide.Player, 0)) });
        Assert.Throws<BattleWireException>(() => BattleWire.ReadChoices(json.Replace($"\"v\":{BattleWire.Version}", "\"v\":99")));
        Assert.Throws<BattleWireException>(() => BattleWire.ReadMessage("{\"v\":1,\"message\":{\"wire\":\"Nothing\"}}", null));
        Assert.Throws<BattleWireException>(() => BattleWire.ReadMessage("not json", null));

        // A Pokémon the roster doesn't know is refused rather than guessed
        var corpus = new BattleCorpus(5);
        var stranger = new BattleCorpus(6).Core.PlayerParty.Members[0];
        Assert.Throws<BattleWireException>(() => BattleWire.WriteEvents(new[] { new Entered(new Place(BattleSide.Player, 0), stranger, true) }, BattleRoster.Of(corpus.Core)));
    }

    [Fact]
    public void TheRulesDrawFromNothingButTheirDice()
    {
        // Anything that would make two runs of the same battle differ, on any machine (plan 28 · I1's scan)
        var banned = new Regex(@"Random\.Shared|new\s+Random\s*\(|DateTime\.(Now|UtcNow|Today)|Environment\.TickCount|Stopwatch|Guid\.NewGuid|GetHashCode\(\)");
        var sim = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Battle", "Sim");
        var found = new List<string>();
        foreach (var file in Directory.EnumerateFiles(sim, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].Split("//")[0];
                if (banned.IsMatch(code)) found.Add($"{Path.GetRelativePath(sim, file)}:{i + 1}: {lines[i].Trim()}");
            }
        }
        Assert.True(found.Count == 0, "Things two runs could part on, in the battle's rules:\n" + string.Join("\n", found));
    }
}
