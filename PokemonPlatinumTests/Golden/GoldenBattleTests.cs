using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// The golden battles (plan 24 · X2): two hundred battles made from their seeds alone (<see cref="BattleCorpus"/>),
/// played to their end and held, line for line, to <c>battles-platinum.txt</c> and <c>battles-modern.txt</c>. A
/// change to the rules moves the battles it reaches; one that is meant is accepted (<see cref="GoldenFile"/>) and
/// its diff read, and a battle of Platinum's rules that moved in a session about anything else is a bug.
/// </summary>
public class GoldenBattleTests
{
    [Theory]
    [InlineData(RulesPreset.Platinum, "battles-platinum.txt")]
    [InlineData(RulesPreset.Modern, "battles-modern.txt")]
    public void TheCorpusPlaysAsItDid(RulesPreset preset, string file)
    {
        var rules = Ruleset.Of(preset);
        var text = new StringBuilder($"# The golden battles under {rules.Name} (plan 24 · X2), made by GoldenBattleTests. Rewritten by: {GoldenFile.AcceptCommand}\n");
        int doubles = 0, againstTheData = 0;
        foreach (int seed in BattleCorpus.SeedsOf(rules))
        {
            var battle = new BattleCorpus(seed);
            battle.Core.Start();

            // Both sides chosen for from inside: the battle is over once it has started
            Assert.Null(battle.Core.Request);
            Assert.True(battle.Core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat, $"battle {seed} ended as {battle.Core.Result}");
            Assert.IsType<Ended>(battle.Core.Log[^1]);
            if (battle.Core.Format == BattleFormat.Double) doubles++;
            if (battle.Opponent != null) againstTheData++;
            text.Append(battle.Block());
        }

        // A third are double battles, and a quarter against a trainer of the data
        Assert.Equal(BattleCorpus.SeedsOf(rules).Count(BattleCorpus.DrawnDouble), doubles);
        Assert.Equal(BattleCorpus.SeedsOf(rules).Count(BattleCorpus.AgainstTheData), againstTheData);
        Assert.InRange(doubles, 30, 40);
        Assert.InRange(againstTheData, 20, 30);

        GoldenFile.Check(file, text.ToString());
    }

    [Theory]
    [InlineData(RulesPreset.Platinum)]
    [InlineData(RulesPreset.Modern)]
    public void EachBattlesRecordReplaysToItsOwnText(RulesPreset preset)
    {
        foreach (int seed in BattleCorpus.SeedsOf(Ruleset.Of(preset)))
        {
            var played = new BattleCorpus(seed);
            played.Core.Start();
            var record = played.Core.Record!;
            Assert.Equal((uint)seed, record.Seed);

            // The same teams drawn again, and the record played through them
            var replayed = new BattleCorpus(seed);
            replayed.Core.Replay(record);
            Assert.Equal(played.Block(), replayed.Block());
        }
    }

    [Fact]
    public void ALogIsWrittenAnEventToALine()
    {
        var corpus = new BattleCorpus(4);
        corpus.Core.Start();
        var lines = BattleText.Written(corpus.Core);

        // A Pokémon by its side, its place in its party and its species; a place by its side and slot
        Assert.Contains(lines, l => l.StartsWith("say ") && l.Contains(" used "));
        Assert.Contains(lines, l => l.Contains("Entered P0 Pokemon=mine#0 "));
        Assert.Contains(lines, l => l.Contains("Entered E0 Pokemon=foe#0 "));
        Assert.Contains(lines, l => l.StartsWith("  ! Struck "));
        Assert.Equal($"Ended Result={corpus.Core.Result}", lines[^1]);
        Assert.Equal("E1", BattleText.PlaceOf(new Place(BattleSide.Enemy, 1)));
        Assert.Equal("P0", BattleText.PlaceOf(new Place(BattleSide.Player, 0)));
    }
}
