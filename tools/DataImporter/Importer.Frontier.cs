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

        // The Battle Factory (plan 06 · R18b): each round's trainers, and the rows its rentals and its trainers' Pokémon
        // are drawn from (src/overlay104/battle_factory_helpers.c)
        string factory = decomp.Source("overlay104", "battle_factory_helpers.c");
        string rounds = Regex.Match(factory, @"sBattleFactoryOpponentPools\[\] = \{(.*?)\n\};", RegexOptions.Singleline).Groups[1].Value;
        data.Factory.Opponents = Regex.Matches(rounds, @"\{ (FRONTIER_TRAINER_\w+), (FRONTIER_TRAINER_\w+), (FRONTIER_TRAINER_\w+), (FRONTIER_TRAINER_\w+) \}")
            .Select(m => Enumerable.Range(1, 4).Select(g => constants.IndexOf(m.Groups[g].Value)).ToArray()).ToList();
        if (data.Factory.Opponents.Count != 8 || data.Factory.Opponents.Any(r => r.Any(n => n < 0) || r[1] <= r[0] || r[3] <= r[2]))
            throw new InvalidDataException("The Battle Factory's rounds were not read");
        List<FactoryPoolRecord> Pools(string table)
        {
            string body = Regex.Match(factory, @"BattleFactoryPokemonPool " + table + @"\[\] = \{(.*?)\n\};", RegexOptions.Singleline).Groups[1].Value;
            var rows = Regex.Matches(body, @"\.trainerIDMax = (\w+),\s*\.setIDMin = (\d+),\s*\.setIDMax = (\d+),\s*\.ivs = (\d+),\s*\.canBeUpgraded = (TRUE|FALSE)")
                .Select(m => new FactoryPoolRecord
                {
                    TrainerMax = constants.IndexOf(m.Groups[1].Value),
                    SetMin = int.Parse(m.Groups[2].Value),
                    SetMax = int.Parse(m.Groups[3].Value),
                    Ivs = int.Parse(m.Groups[4].Value),
                    Upgrades = m.Groups[5].Value == "TRUE"
                }).ToList();
            if (rows.Count != 10 || rows.Any(r => r.TrainerMax < 0 || r.SetMin < 1 || r.SetMax < r.SetMin || r.SetMax >= data.Sets.Count))
                throw new InvalidDataException($"The Battle Factory's table {table} was not read");
            return rows;
        }
        data.Factory.Level50 = Pools("sLevel50Pools");
        data.Factory.OpenFoes = Pools("sOpenLevelOpponentPools");
        data.Factory.OpenRentals = Pools("sOpenLevelPlayerPools");

        Hall(data, constants);
        Castle(data, constants);
        return data;
    }

    /// <summary>
    /// The Battle Hall (plan 06 · R18c): the Pokémon met there, from the archive of its own
    /// (<c>res/prebuilt/battle/b_pl_stage/pl_bsdpm.narc</c>: sixteen bytes a set, as <c>FrontierPokemonBase</c> lays
    /// them out), checked against <c>sBattleHallPotentialOpponents</c>, with the two types each counts as, the four
    /// groups, and the classes its trainers are drawn by (<c>src/overlay104/battle_hall_helpers.c</c>). A trainer's
    /// class is kept by its constant, since the Hall tells classes apart that share a name.
    /// </summary>
    private void Hall(FrontierData data, List<string> trainerConstants)
    {
        string source = decomp.Source("overlay104", "battle_hall_helpers.c");
        string Table(string head) => Regex.Match(source, Regex.Escape(head) + @" = \{(.*?)\n\};", RegexOptions.Singleline).Groups[1].Value;
        static string ClassKey(string constant) => constant["TRAINER_CLASS_".Length..].ToLowerInvariant();

        var species = Regex.Matches(Table("sBattleHallPotentialOpponents[]"), @"SPECIES_\w+").Select(m => m.Value).ToList();
        var types = Regex.Matches(Table("sBattleHallPotentialOpponentTypes[][2]"), @"\{ (TYPE_\w+), (TYPE_\w+) \}")
            .Select(m => new[] { m.Groups[1].Value, m.Groups[2].Value }).ToList();
        if (species.Count == 0 || species.Count != types.Count) throw new InvalidDataException($"The Battle Hall's {species.Count} Pokémon and {types.Count} pairs of types were not read");
        if (types.Any(t => t.Contains("TYPE_MYSTERY"))) throw new InvalidDataException("A Pokémon of the Battle Hall has no type");

        var natureConstants = decomp.Constants("natures");
        var natures = Enum.GetNames<Nature>();
        var files = Narc(decomp.Bytes("res", "prebuilt", "battle", "b_pl_stage", "pl_bsdpm.narc"));
        if (files.Count != species.Count + 1) throw new InvalidDataException($"pl_bsdpm.narc has {files.Count} sets for {species.Count} Pokémon");
        for (int i = 0; i < files.Count; i++)
        {
            byte[] f = files[i];
            if (f.Length != 16) throw new InvalidDataException($"Set {i} of pl_bsdpm.narc is {f.Length} bytes, not 16");
            int dex = BitConverter.ToUInt16(f, 0);
            var set = new FrontierSetRecord { Id = "hall_" + i };
            if (dex != 0)
            {
                if (i == 0 || speciesConstants[dex] != species[i - 1]) throw new InvalidDataException($"Set {i} of pl_bsdpm.narc is {speciesConstants[dex]}, not the table's Pokémon");
                set.Species = api.SpeciesName(dex);
                set.Moves = Enumerable.Range(0, 4).Select(m => (int)BitConverter.ToUInt16(f, 2 + m * 2)).Where(m => m != 0).Select(api.MoveName).ToList();
                set.EvFlags = f[10];
                string nature = Names.Pascal(natureConstants[f[11]], "NATURE_");
                set.Nature = Enum.Parse<Nature>(natures.First(n => n == nature));
                int item = BitConverter.ToUInt16(f, 12);
                if (item != 0) set.Item = ItemName(itemConstants[item]);
                // The only Pokémon of the Frontier in forms: Wormadam's other two cloaks
                set.Form = (set.Species, (int)BitConverter.ToUInt16(f, 14)) switch
                {
                    (_, 0) => null,
                    ("Wormadam", 1) => "Wormadam-Sandy",
                    ("Wormadam", 2) => "Wormadam-Trash",
                    _ => throw new InvalidDataException($"Set {i} of pl_bsdpm.narc is in a form the importer has no name for")
                };
            }
            else if (i != 0) throw new InvalidDataException($"Set {i} of pl_bsdpm.narc is empty");
            data.Hall.Pokemon.Add(set);
            data.Hall.Types.Add(i == 0 ? Array.Empty<PokemonType>() : types[i - 1].Select(Gen4Type).ToArray());
        }

        data.Hall.Groups = Regex.Matches(source, @"#define GROUP_\d_START (\d+)").Select(m => int.Parse(m.Groups[1].Value)).ToList();
        if (data.Hall.Groups.Count != 4 || data.Hall.Groups[0] != 1) throw new InvalidDataException("The Battle Hall's groups were not read");

        List<string[]> Classes(string head, int width)
        {
            var rows = Regex.Matches(Table(head), @"\{([^{}]*)\}").Select(m => Regex.Matches(m.Groups[1].Value, @"TRAINER_CLASS_\w+").Select(c => ClassKey(c.Value)).ToArray()).ToList();
            if (rows.Count == 0 || rows.Any(r => r.Length != width)) throw new InvalidDataException($"The Battle Hall's table {head} was not read");
            return rows;
        }
        data.Hall.ClassesByRank = Classes("sHallTrainerClassesByRank[HALL_MAX_TYPE_RANK][8]", 8);
        data.Hall.ClassesByType = Classes("sHallTrainerClassesBySelectedType[NUM_POKEMON_TYPES][4]", 4);
        if (data.Hall.ClassesByRank.Count != 10 || data.Hall.ClassesByType.Count != 18) throw new InvalidDataException("The Battle Hall's classes were not read");

        // sFrontierTrainerClasses, the header the original's own tool writes: every trainer's class in the trainers' order
        data.Hall.TrainerClasses = trainerConstants.Select(constant =>
            ClassKey(Json(decomp.Text("res", "trainers", "frontier", "data", constant["FRONTIER_TRAINER_".Length..].ToLowerInvariant() + ".json")).GetProperty("class").GetString()!)).ToList();
    }

    /// <summary>
    /// The Battle Castle (plan 06 · R18c): each round's trainers (<c>sBattleCastleOpponentPools</c>,
    /// <c>src/overlay104/battle_castle_helpers.c</c>) and what the valet's counter lends for Castle Points, with the
    /// prices and how much of each list a rank opens (<c>src/applications/frontier/battle_castle/self_app.c</c>).
    /// </summary>
    private void Castle(FrontierData data, List<string> trainerConstants)
    {
        string rounds = Regex.Match(decomp.Source("overlay104", "battle_castle_helpers.c"), @"sBattleCastleOpponentPools\[\] = \{(.*?)\n\};", RegexOptions.Singleline).Groups[1].Value;
        data.Castle.Opponents = Regex.Matches(rounds, @"\{ (FRONTIER_TRAINER_\w+), (FRONTIER_TRAINER_\w+), (FRONTIER_TRAINER_\w+), (FRONTIER_TRAINER_\w+) \}")
            .Select(m => Enumerable.Range(1, 4).Select(g => trainerConstants.IndexOf(m.Groups[g].Value)).ToArray()).ToList();
        if (data.Castle.Opponents.Count != 8 || data.Castle.Opponents.Any(r => r.Any(n => n < 0) || r[1] <= r[0] || r[3] <= r[2]))
            throw new InvalidDataException("The Battle Castle's rounds were not read");

        string counter = decomp.Source("applications", "frontier", "battle_castle", "self_app.c");
        string Body(string head) => Regex.Match(counter, Regex.Escape(head) + @" = \{(.*?)\n\};", RegexOptions.Singleline).Groups[1].Value;
        List<FrontierPrizeRecord> Lent(string names, string prices)
        {
            var items = Regex.Matches(Body(names), @"ITEM_\w+").Select(m => ItemName(m.Value)).ToList();
            var costs = Regex.Matches(Body(prices), @"\d+").Select(m => int.Parse(m.Value)).ToList();
            if (items.Count == 0 || items.Count != costs.Count) throw new InvalidDataException($"The Battle Castle's {names} and {prices} were not read");
            return items.Zip(costs, (item, price) => new FrontierPrizeRecord { Item = item, Price = price }).ToList();
        }
        data.Castle.Berries = Lent("sBerriesForRent[]", "sBerryPrices[NELEMS(sBerriesForRent)]");
        data.Castle.Items = Lent("sItemsForRent[]", "sItemPrices[NELEMS(sItemsForRent)]");
        List<int> ByRank(string head, int all) => Regex.Matches(Body(head), @"NELEMS\(\w+\)|\d+").Select(m => char.IsDigit(m.Value[0]) ? int.Parse(m.Value) : all).ToList();
        data.Castle.BerriesByRank = ByRank("sBerriesAvailableByRank[]", data.Castle.Berries.Count);
        data.Castle.ItemsByRank = ByRank("sItemsAvailableByRank[]", data.Castle.Items.Count);
        if (data.Castle.Berries.Count != 32 || data.Castle.Items.Count != 27 || data.Castle.BerriesByRank.Count != 3 || data.Castle.ItemsByRank.Count != 3)
            throw new InvalidDataException("The Battle Castle's counters were not read");
    }

    private static JsonElement Json(string text)
    {
        using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true });
        return doc.RootElement.Clone();
    }
}
