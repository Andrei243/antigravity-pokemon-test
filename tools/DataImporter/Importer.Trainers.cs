using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

// Platinum's trainers (plan 06 · R9): what each thinks with, the items it uses, its team as the original builds it,
// and the prize money its formula gives. Never a line of what they say: that is the game's text.
public sealed partial class Importer
{
    /// <summary>
    /// Every trainer of <c>res/trainers/data</c>, by the id its constant gives (<c>TRAINER_YOUNGSTER_TRISTAN</c> is
    /// <c>youngster_tristan</c>), in the original's order. Each Pokémon carries the personality the original works out
    /// for it (<c>TrainerData_BuildParty</c>): the original's generator seeded with the IV scale, level, species and
    /// trainer's number, run once for each class before it, shifted and given the class's gender's number.
    /// </summary>
    public List<TrainerRecord> Trainers(IReadOnlyList<PokemonSpecies> species)
    {
        var trainers = new List<TrainerRecord>();
        var trainerConstants = decomp.Constants("trainers");
        var classConstants = decomp.Constants("trainer_classes");
        var prizeMul = ClassTable(decomp.Include("data", "trainer_class_prize_mul.h"), v => int.Parse(v));
        var female = ClassTable(decomp.Include("data", "trainer_class_genders.h"), v => v == "GENDER_FEMALE");
        var bySpecies = species.ToDictionary(s => s.Name);

        for (int id = 1; id < trainerConstants.Count; id++)
        {
            string key = trainerConstants[id]["TRAINER_".Length..].ToLowerInvariant();
            if (decomp.Trainer(key) is not { } d) continue;
            string classConstant = d.GetProperty("class").GetString()!;
            int classId = classConstants.IndexOf(classConstant);
            int genderMod = female.GetValueOrDefault(classConstant) ? 120 : 136;

            var t = new TrainerRecord
            {
                Id = key,
                Name = Names.Clean(d.GetProperty("name").GetString()!),
                Class = ClassName(classConstant),
                Ai = d.GetProperty("ai_flags").EnumerateArray().Select(f => Names.Pascal(f.GetString()!, "AI_FLAG_"))
                    .Select(f => f == "CheckHp" ? "CheckHp" : f == "Harrassment" ? "Harassment" : f).ToList(),
                Items = d.GetProperty("items").EnumerateArray().Select(i => i.GetString()!).Where(i => i != "ITEM_NONE").Select(ItemName).ToList(),
                DoubleBattle = d.GetProperty("double_battle").GetBoolean()
            };

            foreach (var m in d.GetProperty("party").EnumerateArray())
            {
                string speciesConstant = m.GetProperty("species").GetString()!;
                int dex = speciesConstants.IndexOf(speciesConstant);
                string name = api.SpeciesName(dex);
                int level = m.GetProperty("level").GetInt32();
                int ivScale = m.GetProperty("iv_scale").GetInt32();
                int form = m.GetProperty("form").GetInt32();

                // TrainerData_BuildParty: the seed, one draw for each class before the trainer's, then the gender's number
                uint seed = (uint)(ivScale + level + dex + id);
                uint rnd = seed;
                for (int j = 0; j < classId; j++)
                {
                    seed = unchecked(seed * 1103515245u + 24691u);
                    rnd = seed >> 16;
                }
                uint personality = unchecked((rnd << 8) + (uint)genderMod);

                var mon = new TrainerPokemonRecord
                {
                    Species = name,
                    Level = level,
                    IvScale = ivScale,
                    Personality = personality,
                    Form = form > 0 && bySpecies[name].Forms is { } forms && form <= forms.Count ? forms[form - 1].Name : null
                };
                if (m.TryGetProperty("item", out var item) && item.ValueKind == JsonValueKind.String && item.GetString() != "ITEM_NONE") mon.Item = ItemName(item.GetString()!);
                if (m.TryGetProperty("moves", out var moves) && moves.ValueKind == JsonValueKind.Array)
                {
                    var known = moves.EnumerateArray().Select(x => x.GetString()!).Where(x => x != "MOVE_NONE")
                        .Select(x => api.MoveName(moveConstants.IndexOf(x))).ToList();
                    if (known.Count > 0) mon.Moves = known;
                }
                t.Party.Add(mon);
            }

            // BattleScript_CalcPrizeMoney: the last Pokémon's level × 4 × the class's number, twice that in a double battle
            int last = t.Party.Count > 0 ? t.Party[^1].Level : 0;
            t.PrizeMoney = last * 4 * prizeMul.GetValueOrDefault(classConstant) * (t.DoubleBattle ? 2 : 1);
            trainers.Add(t);
        }
        AddRematches(trainers);
        return trainers;
    }

    /// <summary>
    /// The Vs. Seeker's rematch table (plan 06 · R12; <c>gVsSeekerRematchData</c> in <c>src/overlay005/vs_seeker.c</c>):
    /// for each trainer who can be battled again, the team of each of the five levels the Vs. Seeker unlocks, a
    /// level with none of its own left out (<c>_</c>, null here) and the row ending where the original's does. A
    /// trainer whose row is <c>NoUniqueRematches</c> battles again with the same team (one level, its own id).
    /// </summary>
    private void AddRematches(List<TrainerRecord> trainers)
    {
        string text = decomp.Source("overlay005", "vs_seeker.c");
        // The rows, from inside the array's own brace to its end
        int start = text.IndexOf('{', text.IndexOf("gVsSeekerRematchData[]", StringComparison.Ordinal)) + 1;
        string table = text[start..text.IndexOf("};", start, StringComparison.Ordinal)];
        var byId = trainers.ToDictionary(t => t.Id);
        static string Id(string constant) => constant["TRAINER_".Length..].ToLowerInvariant();
        foreach (Match row in Regex.Matches(table, @"NoUniqueRematches\((TRAINER_\w+)\)|\{([^}]*)\}"))
        {
            List<string?> levels;
            string first;
            if (row.Groups[1].Success)
            {
                first = Id(row.Groups[1].Value);
                levels = new List<string?> { first };
            }
            else
            {
                var cells = row.Groups[2].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                first = Id(cells[0]);
                levels = cells.Skip(1).TakeWhile(c => c != "VS_SEEKER_REMATCH_DATA_END").Select(c => c == "_" ? null : Id(c)).ToList();
            }
            if (byId.TryGetValue(first, out var trainer)) trainer.Rematches = levels;
        }
    }

    /// <summary>A table of the original's indexed by trainer class (<c>[TRAINER_CLASS_X] = value,</c>).</summary>
    private static Dictionary<string, T> ClassTable<T>(string text, Func<string, T> parse) =>
        Regex.Matches(text, @"\[(TRAINER_CLASS_\w+)\]\s*=\s*(\w+)").ToDictionary(m => m.Groups[1].Value, m => parse(m.Groups[2].Value));

    /// <summary>
    /// The class as a trainer is called by it ("Ace Trainer" for both of <c>ACE_TRAINER_MALE</c> and
    /// <c>_FEMALE</c>; a gym leader is a "Leader", an Elite Four member "Elite Four"). The game's own spellings of
    /// the classes are text, so these are made from the constants.
    /// </summary>
    private static string ClassName(string constant)
    {
        string body = constant["TRAINER_CLASS_".Length..];
        foreach (var (prefix, name) in new[] { ("LEADER_", "Leader"), ("ELITE_FOUR_", "Elite Four"), ("CHAMPION_", "Champion"), ("COMMANDER_", "Commander"), ("TRAINER_", "Pokémon Trainer") })
            if (body.StartsWith(prefix)) return name;
        body = Regex.Replace(body, "_(MALE|FEMALE)(_2)?$", "");
        body = body.Replace("_SNOW", "");
        return body switch
        {
            "POKE_KID" => "Poké Kid",
            "POKEFAN" => "Pokéfan",
            "PI" => "PI",
            "RANGER" => "Pokémon Ranger",
            "BREEDER" => "Pokémon Breeder",
            "DP_PLAYER" or "PLAYER" => "Pokémon Trainer",
            "GALACTIC_BOSS" => "Galactic Boss",
            "SIS_AND_BRO" => "Sis and Bro",
            "BELLE_AND_PA" => "Belle & Pa",
            _ => string.Join(' ', body.Split('_').Select(w => w[0] + w[1..].ToLowerInvariant()))
        };
    }
}
