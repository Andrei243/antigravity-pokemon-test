using System.Globalization;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace DataImporter;

// The Underground's tables (plan 06 · R16a): what a wall can hold with its shapes and odds, what each treasure becomes
// above ground and what the hikers give and ask, the traps, the base's goods and the hikers' tiles. Names and numbers
// only: no picture, no line anyone says.
public sealed partial class Importer
{
    /// <summary>
    /// <c>sMiningObjects</c> and its shapes (<c>src/underground/mining.c</c>), <c>sMiningItems</c>
    /// (<c>src/underground.c</c>), <c>sTreasurePrices</c>, <c>sTrapPrices</c>, <c>sGoodData</c> (<c>src/goods.c</c>) and
    /// <c>sUndergroundVendors</c> (<c>src/underground/vendors.c</c>), named from the banks of treasures', traps' and
    /// goods' names.
    /// </summary>
    public UndergroundData Underground(IReadOnlyCollection<string> itemNames)
    {
        var data = new UndergroundData();
        string mining = decomp.Source("underground", "mining.c");

        // The shapes, as the console's memory holds them: rows padded with nothing to the declared width
        var shapes = new Dictionary<string, (int Columns, List<char> Cells)>();
        foreach (Match m in Regex.Matches(mining, @"static u8 (s\w+)\[(\d+)\]\[(\d+)\] = \{(.*?)\n\};", RegexOptions.Singleline))
        {
            int rows = int.Parse(m.Groups[2].Value), columns = int.Parse(m.Groups[3].Value);
            var cells = new List<char>();
            var lines = Regex.Matches(m.Groups[4].Value, @"\{([^{}]*)\}").Select(r => Regex.Matches(r.Groups[1].Value, @"'(.)'").Select(c => c.Groups[1].Value[0]).ToList()).ToList();
            if (lines.Count != rows) throw new InvalidDataException($"{m.Groups[1].Value} has {lines.Count} rows, not {rows}");
            foreach (var line in lines)
            {
                if (line.Count > columns) throw new InvalidDataException($"{m.Groups[1].Value} has a row too long");
                cells.AddRange(line);
                cells.AddRange(Enumerable.Repeat('\0', columns - line.Count));
            }
            shapes[m.Groups[1].Value] = (columns, cells);
        }

        string table = Regex.Match(mining, @"static MiningObject sMiningObjects\[\] = \{(.*?)\n\};", RegexOptions.Singleline).Groups[1].Value;
        foreach (Match row in Regex.Matches(table, @"\{ \.itemID = MINING_(\w+), \.oddTIDWeight = (\d+), \.evenTIDWeight = (\d+), \.oddTIDNatDexWeight = (\d+), \.evenTIDNatDexWeight = (\d+), \.width = (\d+) \* 2, \.height = (\d+) \* 2, \.shape = (\w+),"))
        {
            int width = int.Parse(row.Groups[6].Value), height = int.Parse(row.Groups[7].Value);
            var record = new MiningObjectRecord
            {
                Id = row.Groups[1].Value, Width = width, Height = height,
                Odd = int.Parse(row.Groups[2].Value), Even = int.Parse(row.Groups[3].Value),
                OddNational = int.Parse(row.Groups[4].Value), EvenNational = int.Parse(row.Groups[5].Value)
            };
            if (row.Groups[8].Value != "NULL")
            {
                // Mining_AreCoordinatesWithinObjectShape: the cell at row * (the object's width) + column of the shape's
                // memory, which is a hole only where it holds an 'o' (the Damp Rock's shape is declared a column
                // wider than it is written, so read by its own width it has no hole at all)
                var (_, cells) = shapes[row.Groups[8].Value];
                var lines = new List<string>();
                for (int y = 0; y < height; y++)
                    lines.Add(string.Concat(Enumerable.Range(0, width).Select(x => y * width + x < cells.Count && cells[y * width + x] == 'o' ? 'o' : 'x')));
                if (lines.Any(l => l.Contains('o'))) record.Shape = lines;
            }
            data.Objects.Add(record);
        }
        if (data.Objects.Count != 85) throw new InvalidDataException($"{data.Objects.Count} mining objects, not 85");

        // The Spheres and treasures: MiningObjectID's constants from 1 to MINING_TREASURE_MAX
        string header = decomp.Include("underground", "mining.h");
        var ids = Regex.Matches(Regex.Match(header, @"enum MiningObjectID \{(.*?)\};", RegexOptions.Singleline).Groups[1].Value, @"MINING_(\w+?)(?: = \w+)?,")
            .Select(m => m.Groups[1].Value).Where(id => id != "SPHERES_MAX").ToList();
        int end = ids.IndexOf("TREASURE_MAX");
        var itemNamesOf = decomp.Messages("underground_items").Select(n => Names.Clean(Regex.Replace(n, @"\{[^}]*\}", ""))).ToList();
        string save = decomp.Source("underground.c");
        var bagItems = Regex.Matches(Regex.Match(save, @"static u16 sMiningItems\[\] = \{(.*?)\};", RegexOptions.Singleline).Groups[1].Value, @"\[(\d+)\] = ITEM_(\w+)")
            .ToDictionary(m => int.Parse(m.Groups[1].Value), m => m.Groups[2].Value);
        var treasurePrices = Prices(decomp.Source("underground", "treasure_prices.c"), "MINING_");
        string[] sizes = { "", "Small ", "Small ", "Small ", "Small ", "Small ", "Large ", "Large ", "Large ", "Large ", "Large " };
        // The bank names the Spheres by kind alone; the wall's small and large ones are told apart here
        string[] kinds = { "", "Prism", "Pale", "Red", "Blue", "Green" };
        for (int id = 1; id < end; id++)
        {
            var record = new TreasureRecord { Id = ids[id], Price = treasurePrices.GetValueOrDefault(ids[id]) ?? new() };
            if (id < UndergroundDatabase.FirstTreasure) record.Name = $"{sizes[id]}{kinds[(id - 1) % 5 + 1]} Sphere";
            else
            {
                string constant = bagItems[id - UndergroundDatabase.FirstTreasure];
                string item = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(constant.Replace('_', ' ').ToLowerInvariant());
                // The data names the Thunderstone as the later games do
                if (!itemNames.Contains(item) && item.EndsWith("stone")) item = item[..^5] + " Stone";
                if (!itemNames.Contains(item)) throw new InvalidDataException($"the treasure {ids[id]} becomes {item}, which is no item");
                record.Item = item;
                record.Name = item;
            }
            data.Treasures.Add(record);
        }
        if (data.Treasures.Count != 59 || itemNamesOf.Count < 6) throw new InvalidDataException($"{data.Treasures.Count} Spheres and treasures, not 59");

        // The traps
        var trapIds = decomp.Constants("traps");
        var trapNames = decomp.Messages("underground_traps").Select(Names.Clean).ToList();
        var trapPrices = Prices(decomp.Source("underground", "trap_prices.c"), "");
        for (int id = 1; id < trapIds.IndexOf("TRAP_MAX"); id++)
            data.Traps.Add(new TrapRecord
            {
                Id = trapIds[id],
                // The game's font has no arrows: a trap that sends one a way says the way in a word
                Name = trapNames[id].Replace("↑", "(Up)").Replace("→", "(Right)").Replace("↓", "(Down)").Replace("←", "(Left)"),
                Price = trapPrices.GetValueOrDefault(trapIds[id]) ?? new()
            });
        if (data.Traps.Count != 33) throw new InvalidDataException($"{data.Traps.Count} traps, not 33");

        // The goods
        var goodIds = decomp.Constants("goods");
        var goodNames = decomp.Messages("underground_goods").Select(Names.Clean).ToList();
        string goods = decomp.Source("goods.c");
        for (int id = 1; id < goodIds.IndexOf("UG_GOOD_MAX"); id++)
        {
            var entry = Regex.Match(goods, $@"\[{goodIds[id]}\] = \{{(.*?)\n    \}},", RegexOptions.Singleline);
            if (!entry.Success) throw new InvalidDataException($"{goodIds[id]} isn't in the goods' table");
            string body = entry.Groups[1].Value;
            int Field(string name) => int.Parse(Regex.Match(body, $@"\.{name} = (\d+)").Groups[1].Value);
            string collision = string.Concat(Regex.Matches(Regex.Match(body, @"\.collision = \{(.*?)\}", RegexOptions.Singleline).Groups[1].Value, @"\d").Select(m => m.Value));
            if (collision.Length != 9) throw new InvalidDataException($"{goodIds[id]}'s collision has {collision.Length} cells");
            string message = Regex.Match(body, @"\.interactMessageID = (\w+)").Groups[1].Value;
            data.Goods.Add(new GoodRecord
            {
                // The dolls are named for their species in capitals, as the original writes species: here in the game's own case
                Id = goodIds[id], Name = goodNames[id].EndsWith(" Doll") ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(goodNames[id].ToLowerInvariant()) : goodNames[id], Width = Field("width"), Depth = Field("depth"), Collision = collision,
                Price = Price(body), Money = Field("moneyPrice"),
                Record = message.StartsWith("UndergroundRecords_Text_SomethingWritten") ? message["UndergroundRecords_Text_SomethingWritten".Length..] : null,
                National = body.Contains(".natDexRequired = TRUE"), Ungiftable = body.Contains(".ungiftable = TRUE")
            });
        }
        if (data.Goods.Count != 138) throw new InvalidDataException($"{data.Goods.Count} goods, not 138");

        // The hikers
        string vendors = decomp.Source("underground", "vendors.c");
        foreach (Match m in Regex.Matches(vendors, @"\{ \.vendorType = VENDOR_TYPE_(\w+), \.x = (\d+), \.z = (\d+) \}"))
            data.Vendors.Add(new VendorRecord { Kind = Names.Pascal(m.Groups[1].Value, ""), X = int.Parse(m.Groups[2].Value), Z = int.Parse(m.Groups[3].Value) });
        if (data.Vendors.Count != 20) throw new InvalidDataException($"{data.Vendors.Count} hikers, not 20");
        return data;
    }

    /// <summary>A table of <c>[CONSTANT] = { .sphereType = …, .minSize = …, .maxSize = … }</c> rows, by constant without its prefix.</summary>
    private static Dictionary<string, SpherePriceRecord> Prices(string source, string prefix) =>
        Regex.Matches(source, @"\[(\w+)\] = (\{[^{}]*\})").ToDictionary(m => m.Groups[1].Value[prefix.Length..], m => Price(m.Groups[2].Value));

    private static SpherePriceRecord Price(string body)
    {
        string kind = Regex.Match(body, @"\.sphereType = (\w+)").Groups[1].Value;
        return new SpherePriceRecord
        {
            Any = kind == "RANDOM_SPHERE_TYPE",
            Kind = kind.StartsWith("SPHERE_") && Enum.TryParse(Names.Pascal(kind, "SPHERE_"), out SphereKind parsed) ? parsed : SphereKind.None,
            Min = int.Parse(Regex.Match(body, @"\.minSize = (\d+)").Groups[1].Value),
            Max = int.Parse(Regex.Match(body, @"\.maxSize = (\d+)").Groups[1].Value)
        };
    }
}
