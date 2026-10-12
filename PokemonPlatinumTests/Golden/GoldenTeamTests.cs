using System;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// Every trainer's team as the game builds it (plan 24 · X2), held to <c>trainers.txt</c>: each trainer of
/// <c>trainers.json</c> by its id, with its class, the flags it thinks with and the items it uses, and each of its
/// Pokémon as <see cref="TrainerDatabase.PartyOf"/> makes it (species or form, level, gender, nature, ability,
/// item, moves and the six stats). An overlay that names a trainer by its id has the team it reads held here.
/// </summary>
public class GoldenTeamTests
{
    [Fact]
    public void EveryTrainersTeamIsBuiltAsItWas()
    {
        var text = new StringBuilder($"# Every trainer of trainers.json as the game builds its team (plan 24 · X2), made by GoldenTeamTests. Rewritten by: {GoldenFile.AcceptCommand}\n");
        int trainers = 0;
        foreach (var record in TrainerDatabase.All.OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            trainers++;
            text.Append($"{GoldenFile.BlockMark}{record.Id}\n");
            text.Append($"{record.Class} {record.Name}{(record.DoubleBattle ? ", a double battle" : "")}, prize {record.PrizeMoney}\n");
            text.Append($"ai {BattleCorpus.Flags(AiFlagNames.Parse(record.Ai))}, items {(record.Items.Count == 0 ? "none" : string.Join(", ", record.Items))}\n");
            var party = TrainerDatabase.PartyOf(record);
            Assert.Equal(record.Party.Count, party.Count);
            for (int i = 0; i < party.Count; i++) text.Append($"  #{i} {BattleText.Member(party.Members[i])}\n");
        }
        Assert.True(trainers > 900, $"only {trainers} trainers");

        GoldenFile.Check("trainers.txt", text.ToString());
    }
}
