using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;

namespace DataImporter;

// Platinum's Super Contests (plan 06 · R17a): each move's quality and effect on stage, and the tables the contests
// are held by, read from the one archive the original keeps them in (res/prebuilt/contest/data/contest_data.narc)
// and its banks of names. Names and numbers only: no picture, no line of what anyone says.
public sealed partial class Importer
{
    /// <summary>A move's quality and effect in a contest (<c>data.json</c>'s <c>contest</c>).</summary>
    public static MoveContest ContestOf(JsonElement move)
    {
        var contest = move.GetProperty("contest");
        return new MoveContest
        {
            Type = Enum.Parse<ContestStat>(Names.Pascal(contest.GetProperty("type").GetString()!, "CONTEST_TYPE_")),
            Effect = ContestEffectOf(contest.GetProperty("effect").GetString()!)
        };
    }

    // CONTEST_EFFECT_2_HEARTS_WHEN_VOLTAGE_UP begins with a digit; the rest read as their constants do
    private static ContestEffect ContestEffectOf(string constant) => constant == "CONTEST_EFFECT_2_HEARTS_WHEN_VOLTAGE_UP"
        ? ContestEffect.TwoHeartsWhenVoltageUp
        : Enum.Parse<ContestEffect>(Names.Pascal(constant, "CONTEST_EFFECT_"));

    /// <summary>
    /// The contests' tables. The archive's files, as the original reads them (<c>src/unk_02094EDC.c</c>): 0 the
    /// contestants, 48 bytes each (<c>UnkStruct_ov6_02248BE8</c>); 1 the judges, 8 bytes each
    /// (<c>UnkStruct_020954F0</c>); 2 the dressings, 84 bytes each (<c>UnkStruct_020951B0</c>); 3 to 14 the twelve
    /// themes, a byte of points for each of the hundred accessories.
    /// </summary>
    public SuperContestData Contests()
    {
        var files = Narc(decomp.Bytes("res", "prebuilt", "contest", "data", "contest_data.narc"));
        if (files.Count != 15) throw new InvalidDataException($"contest_data.narc has {files.Count} files, not 15");
        var names = decomp.Messages("contest_opponent_names");
        var judgeNames = decomp.Messages("contest_judge_names");
        var looks = decomp.Constants("object_events_gfx");
        var data = new SuperContestData
        {
            // The masks are written with their species in capitals, as the original writes every species: ours are not
            Accessories = decomp.Messages("contest_accessory_names").Select(Names.Clean)
                .Select(n => System.Text.RegularExpressions.Regex.Replace(n, "[A-Z]{3,}", m => m.Value[0] + m.Value[1..].ToLowerInvariant())).ToList()
        };

        // The themes' names are the visual competition's own bank's (unk_0385, entries 7 to 18)
        var themeNames = decomp.Messages("unk_0385");
        for (int theme = 0; theme < 12; theme++)
        {
            byte[] points = files[3 + theme];
            if (points.Length != data.Accessories.Count || points.Any(p => p > 9))
                throw new InvalidDataException($"theme {theme}'s points don't fit the accessories");
            data.Themes.Add(new ContestTheme { Name = Names.Clean(themeNames[7 + theme]), Points = string.Concat(points.Select(p => (char)('0' + p))) });
        }

        byte[] judges = files[1];
        for (int at = 0; at + 8 <= judges.Length; at += 8)
        {
            int bits = U16(judges, at + 4);
            data.Judges.Add(new ContestJudge
            {
                Name = Names.Clean(judgeNames[U16(judges, at)]),
                Look = U16(judges, at + 2) is var judgeLook && judgeLook < looks.Count ? looks[judgeLook]["OBJ_EVENT_GFX_".Length..].ToLowerInvariant() : "",
                Rank = (ContestRank)((bits >> 10) & 3),
                Seats = Enumerable.Range(0, 5).Select(q => (bits >> (q * 2)) & 3).ToList()
            });
        }

        byte[] dressings = files[2];
        for (int at = 0; at + 84 <= dressings.Length; at += 84)
        {
            int count = dressings[at + 80];
            data.Dressings.Add(new ContestDressing
            {
                Accessories = Enumerable.Range(0, count)
                    .Select(i => $"{dressings[at + i * 4]},{dressings[at + i * 4 + 1]},{dressings[at + i * 4 + 2]},{(sbyte)dressings[at + i * 4 + 3]}").ToList(),
                PokemonPriority = (sbyte)dressings[at + 81],
                Backdrop = (sbyte)dressings[at + 82]
            });
        }

        byte[] opponents = files[0];
        for (int at = 0; at + 48 <= opponents.Length; at += 48)
        {
            int bits = U16(opponents, at + 0x20);
            int look = U16(opponents, at + 0x08);
            data.Opponents.Add(new ContestOpponent
            {
                Look = look < looks.Count ? looks[look]["OBJ_EVENT_GFX_".Length..].ToLowerInvariant() : "",
                Moves = Enumerable.Range(0, 4).Select(i => U16(opponents, at + 0x0C + i * 2)).Where(m => m != 0).Select(api.MoveName).ToList(),
                Species = api.SpeciesName(U16(opponents, at + 0x14)),
                Nickname = Names.Clean(names[U16(opponents, at + 0x16)]),
                Name = Names.Clean(names[U16(opponents, at + 0x18)]),
                Cool = opponents[at + 0x1A],
                Beauty = opponents[at + 0x1B],
                Cute = opponents[at + 0x1C],
                Smart = opponents[at + 0x1D],
                Tough = opponents[at + 0x1E],
                Sheen = opponents[at + 0x1F],
                Rank = (ContestRank)(bits & 7),
                Contests = Enumerable.Range(0, 5).Where(q => (bits >> (3 + q) & 1) != 0).Select(q => (ContestStat)q).ToList(),
                SingleRound = (bits >> 8 & 1) != 0,
                Practice = (bits >> 9 & 1) != 0,
                Season = bits >> 10 & 3,
                Gender = bits >> 12 & 3,
                Mind = bits >> 14 & 3,
                Dressings = Enumerable.Range(0, 12).Select(i => (int)opponents[at + 0x22 + i]).ToList(),
                Fame = opponents[at + 0x2E]
            });
        }
        foreach (var o in data.Opponents)
            if (o.Dressings.Any(d => d >= data.Dressings.Count)) throw new InvalidDataException($"{o.Name}'s dressing isn't in the archive");
        return data;
    }

    private static int U16(byte[] bytes, int at) => bytes[at] | bytes[at + 1] << 8;

    /// <summary>
    /// The files of a Nitro archive in order: the header, the allocation table of each file's start and end
    /// (<c>BTAF</c>), the names (<c>BTNF</c>, skipped) and the files themselves (<c>GMIF</c>).
    /// </summary>
    private static List<byte[]> Narc(byte[] narc)
    {
        if (narc.Length < 0x1C || narc[0] != 'N' || narc[1] != 'A' || narc[2] != 'R' || narc[3] != 'C') throw new InvalidDataException("not a NARC archive");
        int btaf = 0x10;
        int btafSize = BitConverter.ToInt32(narc, btaf + 4);
        int count = BitConverter.ToUInt16(narc, btaf + 8);
        int btnf = btaf + btafSize;
        int data = btnf + BitConverter.ToInt32(narc, btnf + 4) + 8;
        var files = new List<byte[]>();
        for (int i = 0; i < count; i++)
        {
            int start = BitConverter.ToInt32(narc, btaf + 12 + i * 8), end = BitConverter.ToInt32(narc, btaf + 16 + i * 8);
            files.Add(narc[(data + start)..(data + end)]);
        }
        return files;
    }
}
