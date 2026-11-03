using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests.Link;

/// <summary>
/// The link battles' test bench (plan 28 · I3): a server and two players joined by loopback links, no network and no
/// screen, played to their end by seeded choosers; and teams drawn from a seed as plain data, as a player brings them.
/// </summary>
internal sealed class LinkBench
{
    public LinkServer Server { get; }
    public LinkClient A { get; }
    public LinkClient B { get; }
    public LoopbackLink LinkA { get; private set; }
    public LoopbackLink LinkB { get; }

    /// <summary>The held items the teams are given, each at most once (the item clause).</summary>
    private static readonly string[] Items =
    {
        "Leftovers", "Sitrus Berry", "Choice Band", "Focus Sash", "Life Orb", "Lum Berry", "Expert Belt", "Quick Claw",
        "Choice Specs", "Muscle Band", "Wise Glasses", "Scope Lens", "Bright Powder", "Shell Bell", "Oran Berry", "King's Rock"
    };

    public LinkBench(uint seed, LinkRoom? room = null, bool autoAnswerB = true)
    {
        room ??= LinkRoom.Default;
        var a = new LinkPlayer("Lucas", Team(seed * 2 + 1, room));
        var b = new LinkPlayer("Dawn", Team(seed * 2 + 2, room));
        LinkA = new LoopbackLink();
        LinkB = new LoopbackLink();
        Server = new LinkServer(LinkSetup.Bring(room, a, b), seed, LinkA.Server, LinkB.Server);
        A = new LinkClient(a.Name, a.Team, LinkA.Player, new SeededChooser(seed * 7 + 3));
        B = new LinkClient(b.Name, b.Team, LinkB.Player, new SeededChooser(seed * 11 + 5)) { AnswersByItself = autoAnswerB };
    }

    /// <summary>Passes everything sent until nobody has anything more to say.</summary>
    public void Settle()
    {
        for (int round = 0; round < 10_000; round++)
        {
            int before = LinkA.SentToPlayer.Count + LinkA.SentToServer.Count + LinkB.SentToPlayer.Count + LinkB.SentToServer.Count;
            A.Pump();
            B.Pump();
            Server.Pump();
            int after = LinkA.SentToPlayer.Count + LinkA.SentToServer.Count + LinkB.SentToPlayer.Count + LinkB.SentToServer.Count;
            if (after == before) return;
        }
        throw new InvalidOperationException("The link never settled");
    }

    /// <summary>Plays to the end.</summary>
    public LinkBench Play()
    {
        Settle();
        Assert.NotNull(Server.Result);
        return this;
    }

    /// <summary>A's link dropped; a new one is made, which <see cref="LinkServer.Rejoin"/> takes.</summary>
    public LinkClient Reconnect(uint chooserSeed)
    {
        LinkA = new LoopbackLink();
        var client = new LinkClient(A.Name, A.Team, LinkA.Player, new SeededChooser(chooserSeed));
        Server.Rejoin(BattleSide.Player, LinkA.Server);
        return client;
    }

    /// <summary>Six Pokémon of Platinum's 493 drawn from a seed, no species twice and no item twice, as plain data.</summary>
    public static List<RecordedPokemon> Team(uint seed, LinkRoom room)
    {
        var rng = new BattleRandom(seed);
        var pool = PokemonDatabase.GetAll().Where(s => s.DexNumber is >= 1 and <= 493).OrderBy(s => s.DexNumber).ToList();
        var team = new List<RecordedPokemon>();
        var species = new HashSet<int>();
        var items = Items.ToList();
        while (team.Count < 6)
        {
            var kind = pool[rng.Next(pool.Count)];
            if (!species.Add(kind.DexNumber)) continue;
            var p = new Pokemon(kind, rng.Next(30, 71), rng);
            if (rng.Next(3) > 0)
            {
                int i = rng.Next(items.Count);
                p.HeldItem = ItemDatabase.Get(items[i]);
                items.RemoveAt(i);
            }
            team.Add(RecordedPokemon.Of(p));
        }
        return team;
    }

    /// <summary>Every Pokémon-bearing event of a log, the lines' own included.</summary>
    public static IEnumerable<BattleEvent> Flatten(IEnumerable<BattleEvent> log)
    {
        foreach (var e in log)
        {
            yield return e;
            if (e is Said said)
                foreach (var inner in said.Shows.Concat(said.OnImpact)) yield return inner;
        }
    }

    /// <summary>One answer to whatever the core asks, drawn among what the rules allow (a test that drives a core with no server).</summary>
    public static List<BattleChoice> AnyAnswer(BattleCore core, BattleRandom rng)
    {
        var request = core.Request!;
        var places = request switch
        {
            ActionRequest action => action.Places.ToList(),
            ReplacementRequest replacement => new List<Place> { replacement.Place },
            SwitchOfferRequest offer => new List<Place> { offer.Place },
            _ => new List<Place>()
        };
        var choices = new List<BattleChoice>();
        var taken = new HashSet<(BattleSide, int)>();
        foreach (var place in places)
        {
            var b = core.At(place);
            int party = b.Roster?.Count ?? 0;
            if (request is ReplacementRequest)
            {
                var can = Enumerable.Range(0, party).Where(i => core.CanSendIn(b, i)).ToList();
                choices.Add(BattleChoice.Switch(place, can[rng.Next(can.Count)]));
                continue;
            }
            var moves = Enumerable.Range(0, b.Pokemon!.Moves.Count).Where(i => core.WhyNot(BattleChoice.Fight(place, i)) == null).ToList();
            var switches = Enumerable.Range(0, party).Where(i => !taken.Contains((place.Side, i)) && core.WhyNot(BattleChoice.Switch(place, i)) == null).ToList();
            if (switches.Count > 0 && (moves.Count == 0 && core.WhyNot(BattleChoice.Fight(place, -1)) != null || rng.Next(8) == 0))
            {
                int to = switches[rng.Next(switches.Count)];
                taken.Add((place.Side, to));
                choices.Add(BattleChoice.Switch(place, to));
            }
            else choices.Add(BattleChoice.Fight(place, moves.Count > 0 ? moves[rng.Next(moves.Count)] : -1));
        }
        return choices;
    }
}
