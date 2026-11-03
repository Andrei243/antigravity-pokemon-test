using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumTests.Golden;
using BattleText = PokemonPlatinumTests.Golden.BattleText;

namespace PokemonPlatinumTests.Link;

/// <summary>Plan 28 · I5: a battle written out where it stands and carried on from there as if nothing had happened.</summary>
public class LinkSnapshotTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ABattleSnapshottedAfterEveryTurnResumesToTheSameEnd(bool doubles)
    {
        var room = LinkRoom.Default with { Format = doubles ? BattleFormat.Double : BattleFormat.Single };
        for (uint seed = 1; seed <= 6; seed++)
        {
            var setup = LinkSetup.Bring(room, new LinkPlayer("Lucas", LinkBench.Team(seed * 2 + 1, room)), new LinkPlayer("Dawn", LinkBench.Team(seed * 2 + 2, room)));
            var order = new[] { 0, 1, 2, 3, 4, 5 };

            // The whole battle once, with the answers it was given kept
            var battle = setup.Battle(order, order, seed);
            var opening = BattleRecordFile.Opening(battle);
            var core = new BattleCore(battle);
            core.Start();
            var rng = new BattleRandom(seed);
            var snapshots = new List<string>();
            while (core.Request != null)
            {
                snapshots.Add(BattleSnapshot.Take(opening, core).Serialize());
                core.Submit(LinkBench.AnyAnswer(core, rng));
            }
            var whole = BattleText.Written(core);
            var answers = core.Record!.Answers;

            // From every question asked: written out, read, resumed and answered as before, the same battle to the end
            for (int at = 0; at < snapshots.Count; at++)
            {
                var resumed = BattleCore.Resume(BattleSnapshot.Read(snapshots[at])!);
                Assert.Empty(resumed.TakeLog());
                foreach (var answer in answers.Skip(at)) resumed.Submit(answer);
                Assert.Equal(whole, BattleText.Written(resumed));
                Assert.Equal(core.Result, resumed.Result);
            }
        }
    }

    [Fact]
    public void ASnapshotThatWouldResumeAsAnotherBattleIsRefused()
    {
        var room = LinkRoom.Default;
        var setup = LinkSetup.Bring(room, new LinkPlayer("Lucas", LinkBench.Team(3, room)), new LinkPlayer("Dawn", LinkBench.Team(4, room)));
        var order = new[] { 0, 1, 2, 3, 4, 5 };
        var battle = setup.Battle(order, order, 9);
        var opening = BattleRecordFile.Opening(battle);
        var core = new BattleCore(battle);
        core.Start();
        var rng = new BattleRandom(9);
        for (int i = 0; i < 3 && core.Request != null; i++) core.Submit(LinkBench.AnyAnswer(core, rng));
        var snapshot = BattleSnapshot.Take(opening, core);

        snapshot.DiceState ^= 1;
        Assert.Throws<InvalidOperationException>(() => BattleCore.Resume(snapshot));
        Assert.Null(BattleSnapshot.Read("{ \"version\": 99 }"));
        Assert.Null(BattleSnapshot.Read("not a snapshot"));
    }
}
