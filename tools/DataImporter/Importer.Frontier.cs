using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace DataImporter;

// The Battle Frontier's tables (plan 06 · R18a): its 315 trainers with the sets their Pokémon are drawn from, the 950
// sets, the Battle Tower's trainers room by room, what the Exchange Service Corner hands over for Battle Points and
// the species no facility lets in. Names and numbers only: the trainers' files also hold what they say, which is the
// game's text and is never read.
public sealed partial class Importer
{
    /// <summary>
    /// <c>res/trainers/frontier</c> (the trainers in the order of <c>generated/frontier_trainers.txt</c>, the sets in
    /// that of <c>frontier_pokemon.order</c>, packed as <c>tools/dataproc/src/frontierproc.c</c> packs them: a trainer's
    /// empty set is not counted), <c>sTrainerClassToObjectID</c> (<c>src/overlay104/frontier_opponents.c</c>),
    /// <c>sBattleTowerTrainerRangesPerRoom</c> and its boss table (<c>src/unk_0204AEE8.c</c>), the prize list of
    /// <c>ScrCmd_GetExchangeServiceCornerItemAndCost</c> (<c>src/unk_020494DC.c</c>) and <c>sBattleFrontierBanlist</c>
    /// (<c>src/pokemon.c</c>).
    /// </summary>
    public FrontierData Frontier()
    {
        var data = new FrontierData();

        // The sets
        var setNames = decomp.Text("res", "trainers", "frontier", "frontier_pokemon.order").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var natures = Enum.GetNames<Nature>();
        string[] evFlags = { "hp", "attack", "defense", "speed", "special_attack", "special_defense" };
        foreach (string name in setNames)
        {
            var d = Json(decomp.Text("res", "trainers", "frontier", "pokemon", name + ".json"));
            string speciesConstant = d.GetProperty("species").GetString()!;
            var set = new FrontierSetRecord { Id = name };
            if (speciesConstant != "SPECIES_NONE")
            {
                set.Species = api.SpeciesName(speciesConstants.IndexOf(speciesConstant));
                set.Moves = d.GetProperty("moves").EnumerateArray().Select(m => m.GetString()!).Where(m => m != "MOVE_NONE")
                    .Select(m => api.MoveName(moveConstants.IndexOf(m))).ToList();
                var flags = d.GetProperty("evFlags");
                for (int i = 0; i < evFlags.Length; i++)
                    if (flags.GetProperty(evFlags[i]).GetBoolean()) set.EvFlags |= 1 << i;
                string nature = Names.Pascal(d.GetProperty("nature").GetString()!, "NATURE_");
                set.Nature = Enum.Parse<Nature>(natures.First(n => n == nature));
                string item = d.GetProperty("item").GetString()!;
                if (item != "ITEM_NONE") set.Item = ItemName(item);
                if (d.GetProperty("form").GetInt32() != 0) throw new InvalidDataException($"The set {name} is in a form: the importer has no name for it yet");
            }
            data.Sets.Add(set);
        }
        if (data.Sets[0].Species != "") throw new InvalidDataException("The first set is not the empty one");

        // The trainers
        var looks = Regex.Matches(decomp.Source("overlay104", "frontier_opponents.c"), @"\{ (TRAINER_CLASS_\w+), OBJ_EVENT_GFX_(\w+) \}")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.ToLowerInvariant());
        var constants = decomp.Constants("frontier_trainers");
        foreach (string constant in constants)
        {
            string stem = constant["FRONTIER_TRAINER_".Length..].ToLowerInvariant();
            var d = Json(decomp.Text("res", "trainers", "frontier", "data", stem + ".json"));
            string classConstant = d.GetProperty("class").GetString()!;
            data.Trainers.Add(new FrontierTrainerRecord
            {
                Id = stem,
                Name = Names.Clean(d.GetProperty("name").GetString()!),
                Class = ClassName(classConstant),
                // BattleFrontier_GetObjectIDFromTrainerClass: a class the table doesn't name walks in as a school kid
                Look = looks.GetValueOrDefault(classConstant, "school_kid_m"),
                Sets = d.GetProperty("availableSets").EnumerateArray().Select(s => setNames.IndexOf(s.GetString()!)).Where(s => s != 0).ToList()
            });
            if (data.Trainers[^1].Sets.Contains(-1)) throw new InvalidDataException($"{stem} names a set that doesn't exist");
        }

        // The Tower's rooms
        string tower = decomp.Source("unk_0204AEE8.c");
        List<int[]> Rows(string table)
        {
            string body = Regex.Match(tower, table + @"\[\]\[2\] = \{(.*?)\n\};", RegexOptions.Singleline).Groups[1].Value;
            return Regex.Matches(body, @"\{ (FRONTIER_TRAINER_\w+), (FRONTIER_TRAINER_\w+) \}")
                .Select(m => new[] { constants.IndexOf(m.Groups[1].Value), constants.IndexOf(m.Groups[2].Value) }).ToList();
        }
        data.TowerRooms = Rows("sBattleTowerTrainerRangesPerRoom");
        data.TowerBossRooms = Rows("sBattleTowerBossTrainerRangesPerRoom");
        if (data.TowerRooms.Count != 8 || data.TowerBossRooms.Count != 8 || data.TowerRooms.Concat(data.TowerBossRooms).Any(r => r[0] < 0 || r[1] < r[0]))
            throw new InvalidDataException("The Battle Tower's rooms were not read");

        // The Exchange Service Corner: the items from FRONTIER_MART_ITEMS_START_ID, the machines from FRONTIER_MART_TMS_START_ID
        string exchange = decomp.Source("unk_020494DC.c");
        string list = Regex.Match(exchange, @"static const u16 prizeList\[\]\[2\] = \{(.*?)\n    \};", RegexOptions.Singleline).Groups[1].Value;
        int machinesFrom = int.Parse(Regex.Match(exchange, @"#define FRONTIER_MART_TMS_START_ID\s+(\d+)").Groups[1].Value);
        var prizes = Regex.Matches(list, @"\{ (ITEM_\w+), (\d+) \}")
            .Select(m => new FrontierPrizeRecord { Item = ItemName(m.Groups[1].Value), Price = int.Parse(m.Groups[2].Value) }).ToList();
        data.ExchangeItems = prizes.Take(machinesFrom).ToList();
        data.ExchangeMachines = prizes.Skip(machinesFrom).ToList();
        if (data.ExchangeItems.Count != 26 || data.ExchangeMachines.Count != 15) throw new InvalidDataException("The Exchange Service Corner's list was not read");

        // The banlist
        string banlist = Regex.Match(decomp.Source("pokemon.c"), @"sBattleFrontierBanlist\[BATTLE_FRONTIER_BANLIST_SIZE\] = \{(.*?)\};", RegexOptions.Singleline).Groups[1].Value;
        data.Banned = Regex.Matches(banlist, @"SPECIES_\w+").Select(m => api.SpeciesName(speciesConstants.IndexOf(m.Value))).ToList();
        if (data.Banned.Count != 18) throw new InvalidDataException($"{data.Banned.Count} banned species, not 18");

        return data;
    }

    private static JsonElement Json(string text)
    {
        using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true });
        return doc.RootElement.Clone();
    }
}
