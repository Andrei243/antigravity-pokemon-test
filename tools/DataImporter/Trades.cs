using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// The trades people of the games make (plan 40 · GE2; <c>dotnet run --project tools/DataImporter -- --trades</c>), read as
/// numbers and names from each decompilation's in-game trade table and written into <c>trades.json</c>: Platinum's four
/// (<c>res/npc_trades/*.json</c>), FireRed's nine (<c>src/data/ingame_trades.h</c>, the FireRed branch of each), Emerald's four
/// (<c>sIngameTrades</c> in <c>src/data/trade.h</c>), HeartGold's (the narc <c>files/a/1/1/2</c> and the nicknames and first
/// trainers' names of <c>msg_0200</c>) and Black 2's (<c>res/field/trades/*.json</c>). Only a trade a person of this game
/// makes is kept: the loans of HeartGold (Steelix, Shuckle, Spearow, Pikachu, Rhyhorn, Beldum), its unplaced Rapidash and
/// Black 2's unused offers, which ask for a Pikachu, are left out. Never a line of text: the mail a Pokémon holds is only its
/// item. The rows of our own are <c>Overrides/trades.json</c>, added last.
/// </summary>
public static class TradesImport
{
    // The raw bases of the pinned commits; files fetched one by one
    private const string PlatinumRaw = "https://raw.githubusercontent.com/pret/pokeplatinum";
    private const string FireRedRaw = "https://raw.githubusercontent.com/pret/pokefirered";
    private const string HeartGoldRaw = "https://raw.githubusercontent.com/pret/pokeheartgold";
    private const string Bw2Raw = "https://raw.githubusercontent.com/fuddlesworth/pokebw2";

    private static readonly string[] PlatinumFiles = { "kazza_abra", "charap_chatot", "gaspar_haunter", "foppa_magikarp" };
    private static readonly string[] Bw2Files = { "petilil", "alakazam", "ambipom", "gigalith", "tangrowth", "rotom" };
    private static readonly string[] EmeraldFiles = { "src/data/trade.h" };

    /// <summary>The HeartGold trades a person makes (the narc's indices): Onix, Machop, Voltorb, Dodrio, Magneton, Xatu.</summary>
    private static readonly int[] HeartGoldPlain = { 0, 1, 2, 3, 8, 9 };

    /// <summary>The sex a trade asks for (HeartGold's girl in Blackthorn wants a female Dragonair, by <c>MonGetGender</c>).</summary>
    private static readonly Dictionary<string, Gender> WantsGender = new() { ["doris"] = Gender.Female };

    // The items the games hold that this game's table names otherwise or hasn't: Gen 3's Fab and Retro Mail
    private static readonly Dictionary<string, string> ItemAliases = new()
    {
        ["stick"] = "Leek", ["fabmail"] = "Heart Mail", ["retromail"] = "Mosaic Mail", ["dreammail"] = "Space Mail"
    };

    private static readonly Dictionary<int, string?> Languages = new()
    {
        [1] = "Japanese", [2] = null, [3] = "French", [4] = "Italian", [5] = "German", [7] = "Spanish", [8] = "Korean"
    };

    public static List<NpcTrade> Build(string repo, string cache)
    {
        string data = Path.Combine(repo, "PokemonPlatinumEngine", "Data");
        var species = SpeciesTable(Path.Combine(data, "species.json"));
        var byNumber = species.Values.Where(s => s.Number > 0).GroupBy(s => s.Number).ToDictionary(g => g.Key, g => g.First());
        var items = RowNames(Path.Combine(data, "items.json"));
        var itemsById = RowsById(Path.Combine(data, "items.json"));
        var abilitiesById = RowsById(Path.Combine(data, "abilities.json"));
        var problems = new List<string>();
        var rows = new List<NpcTrade>();

        string Item(string constant, string where)
        {
            string key = Normal(constant.StartsWith("ITEM_") ? constant[5..] : constant);
            if (key is "none" or "") return "";
            if (ItemAliases.TryGetValue(key, out var alias)) return alias;
            if (items.TryGetValue(key, out var name)) return name;
            problems.Add($"{where}: item {constant} is no item of items.json");
            return "";
        }
        SpeciesRow Mon(string constant, string where)
        {
            string c = constant.StartsWith("SPECIES_") ? constant[8..] : constant;
            string key = c switch { "NIDORAN_F" => "nidoranf", "NIDORAN_M" => "nidoranm", _ => Normal(c) };
            if (species.TryGetValue(key, out var row)) return row;
            problems.Add($"{where}: species {constant} is no species of species.json");
            return new SpeciesRow("?", 4, Array.Empty<string>(), null, 0);
        }

        // ---- Platinum (res/npc_trades): the personality decides nature, ability and gender
        string plat = Sources.Download(cache, "trades-pokeplatinum", PlatinumRaw, Sources.DecompCommit,
            PlatinumFiles.Select(f => $"res/npc_trades/{f}.json").ToArray());
        foreach (string file in PlatinumFiles)
        {
            var j = JsonNode.Parse(File.ReadAllText(Path.Combine(plat, $"res/npc_trades/{file}.json")))!;
            string where = $"Platinum {file}";
            var given = Mon(j["species"]!.GetValue<string>(), where);
            uint pid = j["personality"]!.GetValue<uint>();
            var cond = new[] { "cool", "beauty", "cute", "smart", "tough" }.Select(k => j[k]!.GetValue<int>()).ToArray();
            rows.Add(Row("Sinnoh", j["name"]!.GetValue<string>(), Mon(j["requestedSpecies"]!.GetValue<string>(), where).Name, given,
                j["otName"]!.GetValue<string>(), j["otID"]!.GetValue<int>(), j["otGender"]!.GetValue<string>() == "GENDER_FEMALE",
                pid, Item(j["heldItem"]!.GetValue<string>(), where),
                new[] { "hpIV", "atkIV", "defIV", "speedIV", "spAtkIV", "spDefIV" }.Select(k => j[k]!.GetValue<int>()).ToArray(),
                null, cond, 0, Language(j["language"]!.GetValue<string>()), null));
        }

        // ---- FireRed and Emerald: C tables, field by field
        string fr = Sources.Download(cache, "trades-pokefirered", FireRedRaw, Sources.FireRedCommit, new[] { "src/data/ingame_trades.h" });
        foreach (var t in CTable(File.ReadAllText(Path.Combine(fr, "src/data/ingame_trades.h")), keepFireRedOnly: true))
            rows.Add(FromC("Kanto", t, Mon, Item));
        string em = Sources.Download(cache, "trades-pokeemerald", Sources.EmeraldRaw, Sources.EmeraldCommit, EmeraldFiles);
        foreach (var t in CTable(File.ReadAllText(Path.Combine(em, "src/data/trade.h")), keepFireRedOnly: false))
            rows.Add(FromC("Hoenn", t, Mon, Item));

        // ---- HeartGold: the narc of 13 structures of 21 words, and the first 26 lines of msg_0200
        string hg = Sources.Download(cache, "trades-pokeheartgold", HeartGoldRaw, Sources.HeartGoldCommit,
            new[] { "files/a/1/1/2", "files/msgdata/msg/msg_0200.json" });
        var narc = Narc(File.ReadAllBytes(Path.Combine(hg, "files/a/1/1/2")));
        var messages = JsonNode.Parse(File.ReadAllText(Path.Combine(hg, "files/msgdata/msg/msg_0200.json")))!["messages"]!.AsArray()
            .Select(m => m!["en_US"]!.GetValue<string>()).ToList();
        foreach (int i in HeartGoldPlain)
        {
            int[] w = Enumerable.Range(0, 21).Select(k => BitConverter.ToInt32(narc[i], 4 * k)).ToArray();
            string where = $"HeartGold {i}";
            var given = ByNumber(byNumber, w[0], where, problems);
            var wants = ByNumber(byNumber, w[19], where, problems);
            string ability = abilitiesById.GetValueOrDefault(w[7], "");
            string itemName = itemsById.GetValueOrDefault(w[15], "");
            if (itemName == "") problems.Add($"{where}: item {w[15]} is no item of items.json");
            string nick = messages[i], ot = messages[i + 13];
            var cond = w[9..14];
            string id = Id(nick);
            rows.Add(Row("Johto", nick, wants.Name, given, ot, w[8], w[16] == 1, (uint)w[14], itemName,
                new[] { w[1], w[2], w[3], w[4], w[5], w[6] }, ability, cond, w[17], Languages.GetValueOrDefault(w[18]),
                WantsGender.TryGetValue(id, out var g) ? g : null));
        }

        // ---- Black 2: a file to a trade, the nature and sex given, so a personality is found that gives them
        string bw = Sources.Download(cache, "trades-pokebw2", Bw2Raw, Sources.Bw2Commit, Bw2Files.Select(f => $"res/field/trades/{f}.json").ToArray());
        foreach (string file in Bw2Files)
        {
            var j = JsonNode.Parse(File.ReadAllText(Path.Combine(bw, $"res/field/trades/{file}.json")))!;
            string where = $"Black 2 {file}";
            var given = Mon(j["species"]!.GetValue<string>(), where);
            var wants = Mon(j["wanted_species"]!.GetValue<string>(), where);
            int slot = j["ability_slot"]!.GetValue<int>();
            string ability = slot == 2 ? given.Hidden ?? given.Abilities[0] : given.Abilities[Math.Min(slot, given.Abilities.Length - 1)];
            var gender = j["sex"] is null ? Gender.Male : j["sex"]!.GetValue<string>() switch
            {
                "GENDER_FEMALE" => Gender.Female, "GENDER_MALE" => Gender.Male, _ => Gender.Genderless
            };
            if (given.GenderRatio < 0) gender = Gender.Genderless;
            var nature = Enum.Parse<Nature>(j["nature"]!.GetValue<string>()["NATURE_".Length..], ignoreCase: true);
            uint pid = FindPersonality(given, nature, gender, slot);
            var iv = j["ivs"]!;
            var c = j["contest"]!;
            rows.Add(Row("Unova", j["nickname"]!.GetValue<string>(), wants.Name, given, j["trainer_name"]!.GetValue<string>(),
                j["trainer_id"]!.GetValue<int>(), j["trainer_gender"]!.GetValue<string>() == "GENDER_FEMALE", pid,
                Item(j["held_item"]!.GetValue<string>(), where),
                new[] { "hp", "attack", "defense", "speed", "special_attack", "special_defense" }.Select(k => iv[k]!.GetValue<int>()).ToArray(),
                ability, new[] { "cool", "beauty", "cute", "smart", "tough" }.Select(k => c[k]!.GetValue<int>()).ToArray(), 0, null, null,
                nature, gender));
        }

        // ---- Ours
        string ours = Path.Combine(repo, "tools", "DataImporter", "Overrides", "trades.json");
        if (File.Exists(ours)) rows.AddRange(GameDataFiles.Deserialize<List<NpcTrade>>(File.ReadAllText(ours)));

        var duplicates = rows.GroupBy(r => r.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0) problems.Add("ids twice: " + string.Join(", ", duplicates));
        if (problems.Count > 0)
            throw new InvalidDataException("The trades have problems:\n  " + string.Join("\n  ", problems));
        Console.WriteLine($"  {rows.Count} trades");
        return rows;
    }

    // ------------------------------------------------------------------ one row

    private static NpcTrade Row(string region, string nickname, string wants, SpeciesRow given, string ot, int otId, bool girl,
        uint pid, string item, int[] ivs, string? ability, int[] condition, int sheen, string? language, Gender? wantsGender,
        Nature? nature = null, Gender? gender = null)
    {
        var n = nature ?? (Nature)(pid % 25);
        // The ability by the personality's lowest bit, unless the table names one
        ability ??= given.Abilities.Length == 0 ? "" : given.Abilities.Length > 1 && (pid & 1) == 1 ? given.Abilities[1] : given.Abilities[0];
        var sex = gender ?? GenderOf(given, pid);
        bool alike = condition.All(c => c == condition[0]);
        return new NpcTrade(Id(nickname), wants, given.Name, nickname, ot, otId, girl ? PlayerLook.Girl : PlayerLook.Boy, pid,
            item, ivs, n, ability, sex, alike ? condition[0] : 0, language, alike && region is not ("Kanto" or "Hoenn") ? null : condition,
            sheen, region, wantsGender);
    }

    private static NpcTrade FromC(string region, Dictionary<string, string> t, Func<string, string, SpeciesRow> species, Func<string, string, string> item)
    {
        string nick = Title(Str(t["nickname"]));
        string where = $"{region} {nick}";
        var given = species(t["species"], where);
        var wants = species(t["requestedSpecies"], where);
        uint pid = Convert.ToUInt32(t["personality"], t["personality"].StartsWith("0x") ? 16 : 10);
        int abilityNum = int.Parse(t["abilityNum"]);
        string ability = given.Abilities.Length == 0 ? "" : given.Abilities[Math.Min(abilityNum, given.Abilities.Length - 1)];
        return Row(region, nick, wants.Name, given, Title(Str(t["otName"])), int.Parse(t["otId"]), t["otGender"].Trim() == "FEMALE", pid,
            item(t["heldItem"], where), Numbers(t["ivs"]), ability, Numbers(t["conditions"]), int.Parse(t["sheen"]), null, null);
    }

    // ------------------------------------------------------------------ the pieces

    private sealed record SpeciesRow(string Name, int GenderRatio, string[] Abilities, string? Hidden, int Number);

    private static Dictionary<string, SpeciesRow> SpeciesTable(string path)
    {
        var d = new Dictionary<string, SpeciesRow>();
        foreach (var row in JsonNode.Parse(File.ReadAllText(path))!.AsArray())
        {
            string name = row!["name"]!.GetValue<string>();
            string key = name switch { "Nidoran♀" => "nidoranf", "Nidoran♂" => "nidoranm", _ => Normal(name) };
            d.TryAdd(key, new SpeciesRow(name, row["genderRatio"]!.GetValue<int>(),
                row["abilities"]!.AsArray().Select(a => a!.GetValue<string>()).ToArray(), row["hiddenAbility"]?.GetValue<string>(),
                row["dexNumber"]!.GetValue<int>()));
        }
        return d;
    }

    private static SpeciesRow ByNumber(Dictionary<int, SpeciesRow> byNumber, int number, string where, List<string> problems)
    {
        // HeartGold names its species by National number
        if (byNumber.TryGetValue(number, out var row)) return row;
        problems.Add($"{where}: species {number}");
        return new SpeciesRow("?", 4, Array.Empty<string>(), null, 0);
    }
    public static void Use(string dataFolder) { }

    private static Gender GenderOf(SpeciesRow s, uint pid)
    {
        int threshold = s.GenderRatio switch { < 0 => 255, 0 => 0, >= 8 => 254, var e => e * 32 - 1 };
        return threshold switch { 255 => Gender.Genderless, 0 => Gender.Male, 254 => Gender.Female, var r => r > (pid & 0xFF) ? Gender.Female : Gender.Male };
    }

    /// <summary>The smallest personality that gives the nature, the sex and the ability's slot asked for.</summary>
    private static uint FindPersonality(SpeciesRow s, Nature nature, Gender gender, int slot)
    {
        for (uint k = 0; k < 4096; k++)
        {
            uint pid = (uint)nature + 25 * k;
            if (GenderOf(s, pid) == gender && (pid & 1) == (uint)(slot & 1)) return pid;
        }
        return (uint)nature;
    }

    private static string? Language(string constant) => constant switch
    {
        "LANGUAGE_ENGLISH" => null, "LANGUAGE_GERMAN" => "German", "LANGUAGE_FRENCH" => "French", "LANGUAGE_ITALIAN" => "Italian",
        "LANGUAGE_SPANISH" => "Spanish", "LANGUAGE_JAPANESE" => "Japanese", "LANGUAGE_KOREAN" => "Korean", _ => null
    };

    /// <summary>The structures of a GBA game's trade table as field to text, FireRed's own branches only where it forks.</summary>
    private static List<Dictionary<string, string>> CTable(string source, bool keepFireRedOnly)
    {
        int start = Regex.Match(source, @"s[Ii]n?[Gg]ame[Tt]rades\[\]").Index;
        int end = source.IndexOf("};", start, StringComparison.Ordinal);
        var lines = new List<string>();
        bool skip = false;
        foreach (string line in source[start..end].Split('\n'))
        {
            string l = line.Trim();
            if (l.StartsWith("#if defined(FIRERED)") || l.StartsWith("#if defined(LEAFGREEN)")) { skip = !keepFireRedOnly ? false : l.Contains("LEAFGREEN"); continue; }
            if (l.StartsWith("#elif defined(LEAFGREEN)")) { skip = true; continue; }
            if (l.StartsWith("#elif defined(FIRERED)")) { skip = false; continue; }
            if (l.StartsWith("#endif")) { skip = false; continue; }
            if (!skip) lines.Add(l);
        }
        string text = string.Join("\n", lines);
        var result = new List<Dictionary<string, string>>();
        foreach (Match block in Regex.Matches(text, @"\[INGAME_TRADE_\w+\]\s*=\s*\{(.*?)\n\s*\}", RegexOptions.Singleline))
        {
            var d = new Dictionary<string, string>();
            foreach (Match f in Regex.Matches(block.Groups[1].Value, @"\.(\w+)\s*=\s*(\{[^}]*\}|_\(""[^""]*""\)|[^,\n]+)"))
                d[f.Groups[1].Value] = f.Groups[2].Value.Trim();
            result.Add(d);
        }
        return result;
    }

    private static string Str(string text) => Regex.Match(text, @"""([^""]*)""").Groups[1].Value;
    private static int[] Numbers(string text) => Regex.Matches(text, @"\d+").Select(m => int.Parse(m.Value)).ToArray();

    /// <summary>The nicknames of the GBA games are capitals: a word at a time, the first letter alone.</summary>
    private static string Title(string text) => string.Join(' ', text.Split(' ').Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));

    private static string Id(string nickname) => Normal(nickname);

    private static string Normal(string name) => new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static Dictionary<string, string> RowNames(string path)
    {
        var d = new Dictionary<string, string>();
        foreach (var row in JsonNode.Parse(File.ReadAllText(path))!.AsArray())
        {
            string name = row!["name"]!.GetValue<string>();
            d.TryAdd(Normal(name), name);
        }
        return d;
    }

    private static Dictionary<int, string> RowsById(string path)
    {
        var d = new Dictionary<int, string>();
        foreach (var row in JsonNode.Parse(File.ReadAllText(path))!.AsArray())
            d.TryAdd(row!["id"]!.GetValue<int>(), row["name"]!.GetValue<string>());
        return d;
    }

    /// <summary>The files of a NARC archive, in order (the NARC, BTAF, BTNF and GMIF chunks).</summary>
    private static List<byte[]> Narc(byte[] b)
    {
        int at = 16;
        var table = new List<(int Start, int End)>();
        int gmif = -1;
        while (at + 8 <= b.Length)
        {
            string tag = System.Text.Encoding.ASCII.GetString(b, at, 4);
            int size = BitConverter.ToInt32(b, at + 4);
            if (tag == "BTAF")
            {
                int count = BitConverter.ToInt32(b, at + 8);
                for (int i = 0; i < count; i++)
                    table.Add((BitConverter.ToInt32(b, at + 12 + 8 * i), BitConverter.ToInt32(b, at + 16 + 8 * i)));
            }
            else if (tag == "GMIF") gmif = at + 8;
            at += size;
        }
        if (gmif < 0) throw new InvalidDataException("A NARC with no GMIF");
        return table.Select(t => b[(gmif + t.Start)..(gmif + t.End)]).ToList();
    }
}
