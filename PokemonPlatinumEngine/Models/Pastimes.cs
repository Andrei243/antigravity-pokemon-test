using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What the game keeps of the pastimes of plan 06 · R16b that the story's flags and variables don't hold: the Coin
/// Case's coins, the six Pokémon waiting for Pal Park's Catching Show and the show under way, and four of the
/// original's game records that the Villa's catalogue asks about. Saved as <c>SaveData.Pastimes</c>.
/// </summary>
public sealed class Pastimes
{
    public CoinCase Coins { get; } = new();
    public PalParkState PalPark { get; } = new();

    /// <summary><c>RECORD_STEPS</c>, <c>RECORD_BERRIES_PLANTED</c> and <c>RECORD_EGGS_HATCHED</c>.</summary>
    public int Steps { get; set; }
    public int BerriesPlanted { get; set; }
    public int EggsHatched { get; set; }
    /// <summary><c>RECORD_POKEMON_RECEIVED_FROM_FOSSIL_REVIVAL</c>.</summary>
    public int FossilsRevived { get; set; }

    public void Clear() => Load(null);

    public void Load(PastimesSave? save)
    {
        Coins.Set(save?.Coins ?? 0);
        Steps = save?.Steps ?? 0;
        BerriesPlanted = save?.BerriesPlanted ?? 0;
        EggsHatched = save?.EggsHatched ?? 0;
        FossilsRevived = save?.FossilsRevived ?? 0;
        PalPark.Load(save?.Waiting);
    }

    public PastimesSave ToSave() => new()
    {
        Coins = Coins.Coins, Steps = Steps, BerriesPlanted = BerriesPlanted, EggsHatched = EggsHatched, FossilsRevived = FossilsRevived,
        Waiting = PalPark.Waiting.Count > 0 ? PalPark.Waiting.Select(Core.SavedPokemonData.FromPokemon).ToList() : null
    };
}

public sealed class PastimesSave
{
    public int Coins { get; set; }
    public int Steps { get; set; }
    public int BerriesPlanted { get; set; }
    public int EggsHatched { get; set; }
    public int FossilsRevived { get; set; }
    /// <summary>The six Pokémon brought over and not yet caught again in the Catching Show (plan 08 · P21 fills them).</summary>
    public List<Core.SavedPokemonData>? Waiting { get; set; }
}

/// <summary>
/// Pal Park (plan 06 · R16b): the six Pokémon that wait in the park and the Catching Show under way. The original
/// stocked the park from a Game Boy Advance cartridge; this game's migration (plan 08 · P21) puts six here
/// (<see cref="Stock"/>), and until it does the lobby says that none have been brought over. A show isn't saved: one
/// broken off by leaving the game leaves its six waiting, as the original's do.
/// </summary>
public sealed class PalParkState
{
    public const string StateVariable = "VAR_PAL_PARK_STATE", RecordVariable = "VAR_CATCHING_SHOW_RECORD";
    /// <summary>What the lobby's worker tells the player apart by: nothing, a show finished, a show given up.</summary>
    public const int Idle = 0, Finished = 1, Retired = 2;

    private readonly List<Pokemon> waiting = new();
    public IReadOnlyList<Pokemon> Waiting => waiting;
    public bool Stocked => waiting.Count >= Overworld.CatchingShow.Size;

    /// <summary>The show under way, and the seconds it has run.</summary>
    public Overworld.CatchingShow? Show { get; private set; }
    public float Seconds { get; private set; }
    /// <summary>Whether the clock runs: from the countdown's end until the last catch or the way out.</summary>
    public bool Running { get; private set; }

    public void Load(IEnumerable<Core.SavedPokemonData>? saved)
    {
        waiting.Clear();
        if (saved != null) waiting.AddRange(saved.Select(s => s.ToPokemon()));
        Show = null;
        Running = false;
        Seconds = 0;
    }

    /// <summary>Six Pokémon are brought over (<c>GBAMigrator</c>'s part, which plan 08 · P21 writes). False while six already wait.</summary>
    public bool Stock(IEnumerable<Pokemon> six)
    {
        var list = six.ToList();
        if (Stocked || list.Count != Overworld.CatchingShow.Size) return false;
        waiting.Clear();
        waiting.AddRange(list);
        return true;
    }

    /// <summary><c>CatchingShow_Start</c>: the six are let loose.</summary>
    public bool Begin(Random chance)
    {
        if (!Stocked) return false;
        Show = new Overworld.CatchingShow(waiting, chance);
        Seconds = 0;
        Running = false;
        return true;
    }

    public void StartClock() => Running = Show != null;

    public void Tick(float dt)
    {
        if (Running) Seconds += dt;
    }

    /// <summary><c>CatchingShow_End</c>: the clock stops and the time's points are worked out.</summary>
    public void Stop()
    {
        if (Show == null || !Running) return;
        Running = false;
        Show.End((int)Seconds);
    }

    /// <summary>The show is over and nothing is kept of it: the six wait again.</summary>
    public void Abandon()
    {
        Show = null;
        Running = false;
    }

    /// <summary>
    /// <c>CatchingShow_MoveMonsToPCBoxes</c>: the six leave the park for the boxes as Pokémon met at Pal Park, and the
    /// park is empty again. Whoever calls it has made sure of six free places.
    /// </summary>
    public List<Pokemon> TakeSix(string place, DateTime today)
    {
        var six = waiting.ToList();
        foreach (var pokemon in six)
        {
            pokemon.Ball = "Park Ball";
            pokemon.Met(place, today);
            PcBoxes.AsStored(pokemon);
        }
        waiting.Clear();
        Show = null;
        Running = false;
        return six;
    }

    /// <summary>
    /// The berry a show's score earns (<c>PalParkLobby_Worker_SetPrize</c>): under 3,000 one of eight, under 3,300
    /// one of five, under 3,500 one of seven, and under 10,000 one of six. The script checks the four levels one
    /// after another, and the level that gives a berry sets the score it checks to 10,000 (<c>CheckIfCanGivePrize</c>),
    /// so only one gives; a score of 10,000 or more would earn nothing.
    /// </summary>
    public static string Prize(int score, Random chance)
    {
        if (score < 3000) return Level1[chance.Next(Level1.Length)];
        if (score < 3300) return Level2[chance.Next(Level2.Length)];
        if (score < 3500) return Level3[chance.Next(Level3.Length)];
        if (score < 10000) return Level4[chance.Next(Level4.Length)];
        return "";
    }

    public static readonly string[] Level1 = { "Cheri Berry", "Chesto Berry", "Pecha Berry", "Rawst Berry", "Aspear Berry", "Leppa Berry", "Oran Berry", "Persim Berry" };
    public static readonly string[] Level2 = { "Figy Berry", "Wiki Berry", "Mago Berry", "Aguav Berry", "Iapapa Berry" };
    public static readonly string[] Level3 = { "Razz Berry", "Bluk Berry", "Nanab Berry", "Wepear Berry", "Pinap Berry", "Lum Berry", "Sitrus Berry" };
    public static readonly string[] Level4 = { "Pomeg Berry", "Kelpsy Berry", "Qualot Berry", "Hondew Berry", "Grepa Berry", "Tamato Berry" };
}

/// <summary>The Oreburgh Mining Museum's revival (<c>scrcmd_fossil.c</c>): the seven fossils and what each was.</summary>
public static class Fossils
{
    public const string RevivedVariable = "VAR_REVIVED_POKEMON_SPECIES";
    public const int Level = 20;

    /// <summary><c>sFossilItemToSpeciesMapping</c>, in its order.</summary>
    public static readonly (string Item, string Species)[] All =
    {
        ("Old Amber", "Aerodactyl"), ("Helix Fossil", "Omanyte"), ("Dome Fossil", "Kabuto"), ("Root Fossil", "Lileep"),
        ("Claw Fossil", "Anorith"), ("Armor Fossil", "Shieldon"), ("Skull Fossil", "Cranidos")
    };

    public static string? SpeciesOf(string item) => All.FirstOrDefault(f => f.Item == item).Species;

    /// <summary><c>GetFossilCount</c>: every fossil in the bag, of whatever kind.</summary>
    public static int Count(Inventory bag) => All.Sum(f => ItemDatabase.Get(f.Item) is { } item ? bag.GetQuantity(item) : 0);

    /// <summary>The kinds the bag holds, in the table's order.</summary>
    public static List<string> Held(Inventory bag) =>
        All.Where(f => ItemDatabase.Get(f.Item) is { } item && bag.GetQuantity(item) > 0).Select(f => f.Item).ToList();
}

/// <summary>A piece of the Villa's furniture: its flag, price, what the catalogue asks before listing it, and where it stands.</summary>
public sealed record VillaFurniture(string Name, string Flag, int Price, VillaNeed Need, VillaPlace[] Places);

/// <summary>Where a piece stands (the tiles it blocks, in the original's room; none for what hangs or lies on something else) and the tile it is seen at.</summary>
public readonly record struct VillaPlace(int X, int Y, int Width, int Depth, bool Blocks);

public enum VillaNeed { None, Rack, GuestSet, FrontierEverywhere, FrontierPrint, HallOfFameTenTimes, BattlegroundFifty, BerriesFifty, EggsThirty, StepsThreeHundredThousand }

/// <summary>What the catalogue's conditions read (<c>ScrCmd_CheckMetFurnitureRequirements</c>'s game records).</summary>
public readonly record struct VillaRecords(bool FrontierEverywhere, bool FrontierPrint, int HallOfFameEntries, int BattlegroundBattles, int BerriesPlanted, int EggsHatched, int Steps);

/// <summary>
/// The Villa in the Resort Area (plan 06 · R16b; the original's <c>villa_furniture.c</c>, <c>scripts_villa.s</c> and
/// <c>SystemVars_UpdateVillaVisitor</c>): twenty pieces of furniture ordered from the catalogue on the table, each
/// with its price and its own fixed place, nine of them listed only once something has been done; and the day's
/// visitor, drawn the oftener and from the more people the more furniture stands in the room. Everything it
/// remembers is the story's flags and variables.
/// </summary>
public static class VillaRules
{
    public const string VisitorVariable = "VAR_RESORT_VILLA_VISITOR", MessageVariable = "VAR_RESORT_VILLA_VISITOR_MESSAGE_NUM";
    public const string StateVariable = "VAR_VILLA_STATE", AreaStateVariable = "VAR_RESORT_AREA_STATE";
    public const string InsideFlag = "FLAG_VILLA_VISITOR_INSIDE", OutsideFlag = "FLAG_VILLA_VISITOR_OUTSIDE";
    public const string BattlegroundVariable = "VAR_TIMES_BATTLED_AT_BATTLEGROUND";
    public const int NoVisitor = 0xFF;
    public const string Room = "ResortAreaVilla", Area = "resort_area";

    /// <summary>The visitors by their number (<c>villa_visitors.h</c>): the keys of the room's people; thirteen and fourteen are pairs.</summary>
    public static readonly string[] Visitors =
    {
        "rival", "counterpart", "prof_rowan", "mom", "roark", "gardenia", "fantina", "maylene", "crasher_wake", "byron", "candice",
        "volkner", "cynthia", "flint_volkner", "maylene_candice"
    };

    private static VillaFurniture Piece(string name, string flag, int price, VillaNeed need, params VillaPlace[] places) => new(name, "FLAG_VILLA_FURNITURE_" + flag, price, need, places);
    private static VillaPlace At(int x, int y, int w = 1, int d = 1) => new(x, y, w, d, true);
    private static VillaPlace On(int x, int y) => new(x, y, 1, 1, false);

    /// <summary><c>sVillaFurnitures</c> and the catalogue's prices, in the catalogue's order.</summary>
    public static readonly IReadOnlyList<VillaFurniture> Furniture = new[]
    {
        Piece("Table", "TABLE", 0, VillaNeed.None, At(11, 5, 3, 4)),
        Piece("Big Sofa", "BIG_SOFA", 120000, VillaNeed.None, At(14, 5, 2, 4)),
        Piece("Small Sofa", "SMALL_SOFA", 90000, VillaNeed.None, At(11, 9, 3, 2)),
        Piece("Bed", "BED", 187000, VillaNeed.None, At(18, 6, 3, 4)),
        Piece("Night Table", "NIGHT_TABLE", 58000, VillaNeed.None, At(17, 6, 1, 2)),
        Piece("TV", "TV", 220000, VillaNeed.None, At(11, 3, 2, 1)),
        Piece("Audio System", "AUDIO_SYSTEM", 160000, VillaNeed.None, At(13, 3, 3, 1)),
        Piece("Bookshelf", "BOOKSHELF", 150000, VillaNeed.None, At(16, 3, 2, 1)),
        Piece("Rack", "RACK", 127000, VillaNeed.None, At(18, 3, 2, 1)),
        Piece("Houseplant", "HOUSEPLANT", 120000, VillaNeed.None, At(1, 3), At(20, 3), At(1, 11), At(20, 11)),
        Piece("PC Desk", "PC_DESK", 168000, VillaNeed.None, At(1, 7, 2, 3)),
        Piece("Music Box", "MUSIC_BOX", 25300, VillaNeed.Rack, On(18, 3)),
        Piece("Pokémon Bust", "POKEMON_BUST", 150000, VillaNeed.FrontierEverywhere, At(2, 3)),
        Piece("Silver Pokémon Bust", "POKEMON_BUST_SILVER", 150000, VillaNeed.FrontierPrint, At(6, 3)),
        Piece("Piano", "PIANO", 146700, VillaNeed.HallOfFameTenTimes, At(1, 4, 2, 3)),
        Piece("Guest Set", "GUEST_SET", 208000, VillaNeed.BattlegroundFifty, At(4, 9, 4, 2)),
        Piece("Wall Clock", "WALL_CLOCK", 52000, VillaNeed.BerriesFifty, On(11, 2)),
        Piece("Masterpiece", "MASTERPIECE", 140000, VillaNeed.EggsThirty, On(8, 2)),
        Piece("Tea Set", "TEA_SET", 108000, VillaNeed.GuestSet, On(6, 10)),
        Piece("Chandelier", "CHANDELIER", 120000, VillaNeed.StepsThreeHundredThousand, On(7, 6))
    };

    public static VillaFurniture? Find(string name) => Furniture.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static bool Owns(StoryState story, VillaFurniture piece) => story.Has(piece.Flag);
    public static int Owned(StoryState story) => Furniture.Count(f => story.Has(f.Flag));

    /// <summary>Whether the catalogue lists a piece (the script's <c>TryAddMenuEntry</c> checks).</summary>
    public static bool Listed(VillaFurniture piece, StoryState story, VillaRecords records) => piece.Need switch
    {
        VillaNeed.None => true,
        VillaNeed.Rack => story.Has("FLAG_VILLA_FURNITURE_RACK"),
        VillaNeed.GuestSet => story.Has("FLAG_VILLA_FURNITURE_GUEST_SET"),
        VillaNeed.FrontierEverywhere => records.FrontierEverywhere,
        VillaNeed.FrontierPrint => records.FrontierPrint,
        VillaNeed.HallOfFameTenTimes => records.HallOfFameEntries >= 10,
        VillaNeed.BattlegroundFifty => records.BattlegroundBattles >= 50,
        VillaNeed.BerriesFifty => records.BerriesPlanted >= 50,
        VillaNeed.EggsThirty => records.EggsHatched >= 30,
        _ => records.Steps >= 300000
    };

    public static List<VillaFurniture> Catalogue(StoryState story, VillaRecords records) => Furniture.Where(f => Listed(f, story, records)).ToList();

    /// <summary>
    /// <c>CalcVillaVisitorIndex</c>: with fewer than eight pieces one of the first four comes on 26 days in a hundred,
    /// with eight one of twelve on 76, with twelve one of all fifteen on 91 (the original's comparison lets the
    /// chance itself through).
    /// </summary>
    public static int DrawVisitor(int ownedPieces, Random chance)
    {
        int row = ownedPieces >= 12 ? 2 : ownedPieces >= 8 ? 1 : 0;
        int[] chances = { 25, 75, 90 }, people = { 4, 12, 15 };
        if (chance.Next(100) > chances[row]) return NoVisitor;
        return chance.Next(people[row]);
    }

    /// <summary>
    /// <c>SystemVars_UpdateVillaVisitor</c>, on a new day: unless the player is at the Resort Area or in the Villa, the
    /// day's visitor and which of their five things they will say are drawn afresh.
    /// </summary>
    public static void NewDay(StoryState story, string? place, Random chance)
    {
        if (place is Area or Room) return;
        story.Unset(InsideFlag);
        story.Unset(OutsideFlag);
        story.SetVar(VisitorVariable, DrawVisitor(Owned(story), chance));
        story.SetVar(MessageVariable, chance.Next(5));
    }

    /// <summary>The tiles of the room that furniture blocks now (<c>Villa_DynamicMapFeaturesCheckCollision</c>).</summary>
    public static IEnumerable<(int X, int Y)> Blocked(StoryState story)
    {
        foreach (var piece in Furniture)
        {
            if (!story.Has(piece.Flag)) continue;
            foreach (var place in piece.Places)
            {
                if (!place.Blocks) continue;
                for (int y = place.Y; y < place.Y + place.Depth; y++)
                    for (int x = place.X; x < place.X + place.Width; x++) yield return (x, y);
            }
        }
    }

    /// <summary>The piece standing on a tile (<c>FieldSystem_TrySetVillaFurnitureScript</c>), for what looking at it says.</summary>
    public static VillaFurniture? PieceAt(StoryState story, int x, int y) =>
        Furniture.FirstOrDefault(f => story.Has(f.Flag) && f.Places.Any(p => p.Blocks && x >= p.X && x < p.X + p.Width && y >= p.Y && y < p.Y + p.Depth));
}
