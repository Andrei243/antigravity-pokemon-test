using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

// The later games' held items, medicine and balls (plan 06 · R28a). PokeAPI says nothing of what a later item does in a
// battle and Pokémon Showdown says it in code, so each later item that does something while held is given a hold
// effect of our own here (the item's Showdown id written out, Platinum's style: "WeaknessPolicy", "GemFire"), and the
// engine's table (Battle/Effects/HeldItemEffects.Later.cs) runs it by that name as it runs Platinum's. A later medicine
// is given the use parameters Platinum's medicine has (Models/ItemUse.cs, BattleCore.Items.cs), and a Showdown item its
// Fling power. docs/mechanics/rulings.md, "The later games' held items, medicine and balls", has the rulings.
public sealed partial class Importer
{
    /// <summary>
    /// The Johto balls of HeartGold and SoulSilver (generation 4, but not Platinum's) and the Park Ball, brought in by
    /// name since PokeAPI keeps them in a category of their own (plan 06 · R28a). Johto's Heavy Ball comes before
    /// Legends: Arceus's of the same name, which is left out.
    /// </summary>
    private static readonly string[] JohtoBalls =
        { "lure-ball", "level-ball", "moon-ball", "heavy-ball", "fast-ball", "friend-ball", "love-ball", "park-ball", "sport-ball" };

    private static bool JohtoBall(string identifier, out ItemPocket pocket)
    {
        pocket = ItemPocket.PokeBalls;
        return JohtoBalls.Contains(identifier);
    }

    /// <summary>Sword and Shield's version group in PokeAPI, whose machine table numbers the hundred TRs from 100.</summary>
    private const int SwordShield = 20, FirstRecord = 100;

    /// <summary>A TR of Sword and Shield (plan 06 · R28b: <c>tr00</c> to <c>tr99</c>), brought in by name to the TMs pocket.</summary>
    private static bool TechnicalRecord(string identifier, out ItemPocket pocket)
    {
        pocket = ItemPocket.TMsAndHMs;
        return identifier.Length == 4 && identifier.StartsWith("tr", StringComparison.Ordinal) && char.IsDigit(identifier[2]) && char.IsDigit(identifier[3]);
    }

    private Dictionary<int, string>? recordMoves;

    /// <summary>Each TR's item id and the move it teaches, from PokeAPI's machine table for Sword and Shield.</summary>
    private Dictionary<int, string> TechnicalRecordMoves() => recordMoves ??= api.Table("machines")
        .Where(r => r.Int("version_group_id") == SwordShield && r.Int("machine_number") >= FirstRecord)
        .ToDictionary(r => r.Int("item_id"), r => api.MoveName(r.Int("move_id")));

    private static string RecordText(string move, string? moveText) =>
        $"Teaches {move} to a compatible Pokémon, and is used up. {moveText}".TrimEnd();

    /// <summary>
    /// Which species learn each TR (plan 06 · R28b): a species learns a TR when it learns the TR's move by a machine in
    /// Sword and Shield, the one game that had them; its TRs go at the end of its <see cref="PokemonSpecies.TmMoves"/>,
    /// after its own machines. A species Sword and Shield left out learns none.
    /// </summary>
    public void AddTechnicalRecords(List<PokemonSpecies> species, List<ItemData> items)
    {
        var records = items.Where(i => i.Pocket == ItemPocket.TMsAndHMs && i.Id >= 1000 && i.TeachesMove != null).OrderBy(i => i.Name, StringComparer.Ordinal).ToList();
        var byMachine = api.Table("pokemon_moves").Where(r => r.Int("version_group_id") == SwordShield && r.Int("pokemon_move_method_id") == 4)
            .GroupBy(r => r.Int("pokemon_id")).ToDictionary(g => g.Key, g => g.Select(r => api.MoveName(r.Int("move_id"))).ToHashSet());
        foreach (var s in species)
        {
            if (!byMachine.TryGetValue(s.DexNumber, out var moves)) continue;
            var learned = records.Where(r => moves.Contains(r.TeachesMove!)).Select(r => r.Name).ToList();
            if (learned.Count == 0) continue;
            s.TmMoves = (s.TmMoves ?? new()).Concat(learned).ToList();
        }
    }

    /// <summary>
    /// The machines of the regions after Sinnoh that Platinum's table has no number for (plan 21 · H8: Hoenn's HM09,
    /// Dive, Emerald's HM08): an override's machine (an id of 9000 and up in the TMs pocket) goes at the end of the
    /// <see cref="PokemonSpecies.TmMoves"/> of every species that learns its move by a machine in any game PokeAPI
    /// lists, Platinum's own species too, since Platinum taught Dive by none.
    /// </summary>
    public void AddHoennMachines(List<PokemonSpecies> species, List<ItemData> items)
    {
        var machines = items.Where(i => i.Pocket == ItemPocket.TMsAndHMs && i.Id >= 9000 && i.TeachesMove != null).OrderBy(i => i.Id).ToList();
        if (machines.Count == 0) return;
        var byMachine = api.Table("pokemon_moves").Where(r => r.Int("pokemon_move_method_id") == 4)
            .GroupBy(r => r.Int("pokemon_id")).ToDictionary(g => g.Key, g => g.Select(r => api.MoveName(r.Int("move_id"))).ToHashSet());
        foreach (var s in species)
        {
            if (!byMachine.TryGetValue(s.DexNumber, out var moves)) continue;
            var learned = machines.Where(m => moves.Contains(m.TeachesMove!) && s.TmMoves?.Contains(m.Name) != true).Select(m => m.Name).ToList();
            if (learned.Count == 0) continue;
            s.TmMoves = (s.TmMoves ?? new()).Concat(learned).ToList();
        }
    }

    /// <summary>
    /// The later items used on a Pokémon outside a battle (plan 06 · R28b): what the bag does with each, by a field use
    /// of our own (<c>Models/LaterItemUse.cs</c>), and the effort the feathers, mochi and Legends: Arceus's candies give
    /// as Platinum's vitamins' use parameters, which <c>EffortRules</c> carries out. A feather gives one point, a mochi
    /// ten, a candy of Legends: Arceus 25, 50 or 75 (its effort level, our ruling), and the Fresh Start Mochi takes
    /// every point away. docs/mechanics/rulings.md, "Items used outside a battle", has the rulings.
    /// </summary>
    private static Dictionary<string, (string FieldUse, Dictionary<string, int>? Use)> LaterShaping()
    {
        var shaping = new Dictionary<string, (string, Dictionary<string, int>?)>();
        string[] keys = { "hpEVs", "atkEVs", "defEVs", "spatkEVs", "spdefEVs", "speedEVs" };
        string[][] names =
        {
            new[] { "Health", "Muscle", "Resist", "Genius", "Clever", "Swift" },
            new[] { "Health", "Mighty", "Tough", "Smart", "Courage", "Quick" }
        };
        for (int i = 0; i < 6; i++)
        {
            shaping[$"{names[0][i]} Feather"] = ("Healing", new() { [keys[i]] = 1 });
            shaping[$"{names[0][i]} Mochi"] = ("Healing", new() { [keys[i]] = 10 });
            shaping[$"{names[1][i]} Candy"] = ("Healing", new() { [keys[i]] = 25 });
            shaping[$"{names[1][i]} Candy L"] = ("Healing", new() { [keys[i]] = 50 });
            shaping[$"{names[1][i]} Candy XL"] = ("Healing", new() { [keys[i]] = 75 });
        }
        shaping["Fresh Start Mochi"] = ("Healing", keys.ToDictionary(k => k, _ => -EffortRules.MaxStat));
        foreach (var (candy, exp) in new[] { ("XS", 100), ("S", 800), ("M", 3000), ("L", 10000), ("XL", 30000) })
            shaping[$"Exp. Candy {candy}"] = ("ExpCandy", new() { ["exp"] = exp });
        foreach (var nature in Enum.GetNames<Nature>()) shaping[$"{nature} Mint"] = ("Mint", null);
        shaping["Ability Capsule"] = ("AbilityCapsule", null);
        shaping["Ability Patch"] = ("AbilityPatch", null);
        shaping["Dynamax Candy"] = ("DynamaxCandy", null);
        foreach (var nectar in new[] { "Red", "Yellow", "Pink", "Purple" }) shaping[$"{nectar} Nectar"] = ("Nectar", null);
        // Plan 06 · R29: the key items that change a form, never used up (Models/FormChanges.cs)
        foreach (var key in new[] { "DNA Splicers", "Reveal Glass", "Prison Bottle", "Zygarde Cube", "N-Solarizer", "N-Lunarizer", "Rotom Catalog", "Reins of Unity" })
            shaping[key] = ("FormChange", null);
        return shaping;
    }

    /// <summary>Each later item's hold effect and its number (a Gem's and a plate's power in a hundred more, a Rocky Helmet's share of HP).</summary>
    private static readonly Dictionary<string, (string Hold, int Param)> LaterHoldEffects = new()
    {
        // Generation 5
        ["Eviolite"] = ("Eviolite", 50), ["Float Stone"] = ("FloatStone", 50), ["Rocky Helmet"] = ("RockyHelmet", 6),
        ["Air Balloon"] = ("AirBalloon", 0), ["Red Card"] = ("RedCard", 0), ["Ring Target"] = ("RingTarget", 0),
        ["Binding Band"] = ("BindingBand", 6), ["Absorb Bulb"] = ("AbsorbBulb", 1), ["Cell Battery"] = ("CellBattery", 1),
        ["Eject Button"] = ("EjectButton", 0),
        ["Douse Drive"] = ("DriveWater", 0), ["Shock Drive"] = ("DriveElectric", 0), ["Burn Drive"] = ("DriveFire", 0), ["Chill Drive"] = ("DriveIce", 0),
        // Generation 6
        ["Weakness Policy"] = ("WeaknessPolicy", 2), ["Assault Vest"] = ("AssaultVest", 50), ["Pixie Plate"] = ("ArceusFairy", 20),
        ["Luminous Moss"] = ("LuminousMoss", 1), ["Snowball"] = ("Snowball", 1), ["Safety Goggles"] = ("SafetyGoggles", 0),
        ["Roseli Berry"] = ("WeakenSeFairy", 0), ["Kee Berry"] = ("KeeBerry", 1), ["Maranga Berry"] = ("MarangaBerry", 1),
        // Generation 7
        ["Adrenaline Orb"] = ("AdrenalineOrb", 1), ["Terrain Extender"] = ("TerrainExtender", 3), ["Protective Pads"] = ("ProtectivePads", 0),
        ["Electric Seed"] = ("ElectricSeed", 1), ["Psychic Seed"] = ("PsychicSeed", 1), ["Misty Seed"] = ("MistySeed", 1), ["Grassy Seed"] = ("GrassySeed", 1),
        // Generation 8
        ["Throat Spray"] = ("ThroatSpray", 1), ["Eject Pack"] = ("EjectPack", 0), ["Heavy-Duty Boots"] = ("HeavyDutyBoots", 0),
        ["Blunder Policy"] = ("BlunderPolicy", 2), ["Room Service"] = ("RoomService", 1), ["Utility Umbrella"] = ("UtilityUmbrella", 0),
        // Generation 9
        ["Booster Energy"] = ("BoosterEnergy", 0), ["Ability Shield"] = ("AbilityShield", 0), ["Clear Amulet"] = ("ClearAmulet", 0),
        ["Mirror Herb"] = ("MirrorHerb", 0), ["Punching Glove"] = ("PunchingGlove", 10), ["Covert Cloak"] = ("CovertCloak", 0),
        ["Loaded Dice"] = ("LoadedDice", 0), ["Fairy Feather"] = ("StrengthenFairy", 20),
        // Plan 06 · R28b: the Ability Shield, Zacian's and Zamazenta's swords and shields, Ogerpon's masks (a fifth more,
        // 4915/4096, for the Ogerpon whose mask it is) and Legends: Arceus's two plates (the Blank Plate is Normal's)
        ["Ability Shield"] = ("AbilityShield", 0), ["Rusted Sword"] = ("RustedSword", 0), ["Rusted Shield"] = ("RustedShield", 0),
        ["Wellspring Mask"] = ("MaskWellspring", 20), ["Hearthflame Mask"] = ("MaskHearthflame", 20), ["Cornerstone Mask"] = ("MaskCornerstone", 20),
        ["Legend Plate"] = ("LegendPlate", 0), ["Blank Plate"] = ("ArceusNormal", 20),
        // Plan 06 · R29: Legends: Arceus's three, which hold their Pokémon in its Origin Forme and power it as the orbs do
        ["Adamant Crystal"] = ("DialgaBoost", 20), ["Lustrous Globe"] = ("PalkiaBoost", 20), ["Griseous Core"] = ("GiratinaBoost", 20),
    };

    private static readonly string[] GemTypes =
        { "Normal", "Fire", "Water", "Electric", "Grass", "Ice", "Fighting", "Poison", "Ground", "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel", "Fairy" };

    private static readonly string[] MemoryTypes =
        { "Fire", "Water", "Electric", "Grass", "Ice", "Fighting", "Poison", "Ground", "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel", "Fairy" };

    /// <summary>
    /// The later medicine and battle items the bag uses in a battle: their use parameters as Platinum's would have them
    /// (a Sweet Heart as a Potion's 20 HP, the regions' sweets as a Full Heal, Max Honey as a Max Revive, Max Mushrooms
    /// as the five X items at once).
    /// </summary>
    private static readonly Dictionary<string, (string Pocket, Dictionary<string, int> Use)> LaterUses = new()
    {
        ["Sweet Heart"] = ("RecoverHp", new() { ["hpRestored"] = 20 }),
        ["Casteliacone"] = ("RecoverStatus", FullHeal()), ["Lumiose Galette"] = ("RecoverStatus", FullHeal()),
        ["Shalour Sable"] = ("RecoverStatus", FullHeal()), ["Big Malasada"] = ("RecoverStatus", FullHeal()),
        ["Pewter Crunchies"] = ("RecoverStatus", FullHeal()),
        ["Max Honey"] = ("RecoverStatus", new() { ["revive"] = 1, ["hpRestored"] = -1 }),
        ["Max Mushrooms"] = ("BattleItems", new() { ["atkStages"] = 1, ["defStages"] = 1, ["speedStages"] = 1, ["spatkStages"] = 1, ["spdefStages"] = 1 }),
    };

    private static Dictionary<string, int> FullHeal() => new()
    {
        ["healSleep"] = 1, ["healPoison"] = 1, ["healBurn"] = 1, ["healFreeze"] = 1, ["healParalysis"] = 1, ["healConfusion"] = 1
    };

    /// <summary>
    /// Gives the later items what they do (plan 06 · R28a): a hold effect to each that does something held, the use
    /// parameters to the later medicine, and Fling's power from Showdown to every later item that has one.
    /// </summary>
    public void AddLaterItemEffects(List<ItemData> items, Showdown showdown)
    {
        var holds = new Dictionary<string, (string Hold, int Param)>(LaterHoldEffects);
        foreach (var type in GemTypes) holds[$"{type} Gem"] = ($"Gem{type}", 30);
        foreach (var type in MemoryTypes) holds[$"{type} Memory"] = ($"Memory{type}", 0);
        var fling = showdown.Items.Where(e => e.Text("isNonstandard") != "CAP" && Showdown.Block(e.Body, "fling") != null)
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => Showdown.Inner(Showdown.Block(g.First().Body, "fling"), "basePower") ?? 0, StringComparer.OrdinalIgnoreCase);

        var shaping = LaterShaping();
        foreach (var item in items.Where(i => i.Id >= 1000 && i.Id < 9000))
        {
            if (shaping.TryGetValue(item.Name, out var shape))
            {
                item.FieldUse = shape.FieldUse;
                item.Use = shape.Use;
                item.CanUseInOverworld = true;
                // Nothing of these is used in a battle: the games' bag keeps them for outside one
                item.CanUseInBattle = false;
            }
            if (holds.TryGetValue(item.Name, out var hold))
            {
                item.HoldEffect = hold.Hold;
                item.HoldParam = hold.Param;
            }
            if (LaterUses.TryGetValue(item.Name, out var use))
            {
                item.Use = new Dictionary<string, int>(use.Use);
                item.BattlePocket = use.Pocket;
                item.BattleUse = use.Pocket == "BattleItems" ? null : "Healing";
                if (item.Pocket == ItemPocket.Medicine) item.FieldUse = "Healing";
            }
            if (item.FlingPower == 0 && fling.TryGetValue(item.Name, out int power)) item.FlingPower = power;
        }
    }
}
