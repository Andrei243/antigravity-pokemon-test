using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>What talking to the Pokémon that walks along in Amity Square comes to.</summary>
public enum AmityFind
{
    /// <summary>Too few steps walked, or nowhere to put what it has: it only has a mood.</summary>
    Nothing,
    Berry,
    Accessory,
}

/// <summary>A hut of the square's ruins: the tile in its middle, which its door (a tile south) leads onto.</summary>
public readonly record struct AmityHut(char Name, int X, int Y)
{
    /// <summary>The door: where whoever is carried here ends, having walked out of the middle.</summary>
    public (int X, int Y) Door => (X, Y + 1);
}

/// <summary>
/// Amity Square's walk (plan 06 · R16c), by the original's <c>scripts_amity_square.s</c> and
/// <c>scrcmd_amity_square.c</c>: which Pokémon the receptionists let out of their balls, what the one walking along
/// turns up after how many steps, what the man of the park gives away each day, and where the ruins' huts lead. What
/// the walk remembers is the story's own variables, under the original's names, so a game saved in the square keeps
/// it with no field of its own. No drawing or input; every draw is a die the caller rolls (a script's
/// <c>random</c>, the original's <c>LCRNG_Next() % n</c>), in the original's order.
/// </summary>
public static class AmitySquare
{
    /// <summary>The square's map.</summary>
    public const string Map = "AmitySquare";

    /// <summary>The thing of the square's map that the Pokémon walking along is (the original's <c>LOCALID_FOLLOWER_MON</c>).</summary>
    public const string Body = "follower_mon";

    public const string ActiveVar = "VAR_FOLLOWER_MON_ACTIVE", PartyVar = "VAR_FOLLOWER_MON_PARTY_ID", SpeciesVar = "VAR_FOLLOWER_MON_SPECIES";
    public const string NextKindVar = "VAR_FOLLOWER_MON_NEXT_PICKUP_TYPE", NextAccessoryVar = "VAR_FOLLOWER_MON_PICKUP_ACCESSORY_ID",
        NextBerryVar = "VAR_FOLLOWER_MON_PICKUP_ITEM_ID";
    public const string StepsVar = "VAR_AMITY_SQUARE_STEP_COUNT", GiftVar = "VAR_AMITY_SQUARE_GIFT_ID";

    /// <summary>Where the man of the park stands today, 0 to 4: a variable of ours (the original moves his object and its save keeps where).</summary>
    public const string GiftManPlaceVar = "VAR_AMITY_SQUARE_GIFT_MAN_PLACE";

    public const string HideFlag = "FLAG_HIDE_AMITY_SQUARE_FOLLOWER_MON", GiftFlag = "FLAG_DAILY_RECEIVED_AMITY_SQUARE_MAN_GIFT";

    /// <summary>The variables of the walk that the game itself keeps (<c>amity</c> in a script, a step in the field): a script may ask them and sets none.</summary>
    public static readonly IReadOnlyList<string> KeptByTheGame = new[]
    {
        PartyVar, SpeciesVar, NextKindVar, NextAccessoryVar, NextBerryVar, StepsVar, GiftVar, GiftManPlaceVar,
    };

    /// <summary>The steps a find takes (<c>AmitySquare_FollowerMon</c>), and where the count stops (<c>SystemVars_IncrementAmitySquareStepCount</c>).</summary>
    public const int StepsToFind = 200, MostSteps = 10000;

    // ------------------------------------------------------------------ who may walk

    /// <summary>The species let into the square (<c>AmitySquare_CheckAllowedMon</c>), in the original's order.</summary>
    public static readonly IReadOnlyList<string> Permitted = new[]
    {
        "Pikachu", "Clefairy", "Psyduck", "Pachirisu", "Drifloon", "Buneary", "Happiny",
        "Turtwig", "Grotle", "Torterra", "Chimchar", "Monferno", "Infernape", "Piplup", "Prinplup", "Empoleon",
    };

    /// <summary>The four more a National Pokédex lets in (<c>AmitySquare_CheckAllowedMon_NationalDex</c>).</summary>
    public static readonly IReadOnlyList<string> WithNationalDex = new[] { "Jigglypuff", "Torchic", "Skitty", "Shroomish" };

    /// <summary>The starters grown up, who have moods of their own (<c>AmitySquare_FollowerMon_Message</c>).</summary>
    public static readonly IReadOnlyList<string> GrownStarters = new[] { "Grotle", "Torterra", "Monferno", "Infernape", "Prinplup", "Empoleon" };

    public static bool IsPermitted(string species, bool nationalDex) =>
        Permitted.Contains(species) || (nationalDex && WithNationalDex.Contains(species));

    /// <summary>Whether a Pokémon of the team may be walked: one of the species, and no Egg (<c>GetPartyMonSpecies</c> gives an Egg none).</summary>
    public static bool MayWalk(Pokemon pokemon, bool nationalDex) => !pokemon.IsEgg && IsPermitted(pokemon.Species.Name, nationalDex);

    /// <summary>
    /// Whether the team has one of the species at all (<c>AmitySquare_CheckPartyHasSpecies</c>): without one the
    /// receptionist lists them. An Egg counts here, as <c>Party_HasSpecies</c> reads the species under the shell, though
    /// nobody is asked to walk with it: whoever brings only such an Egg is asked nothing and told to come back.
    /// </summary>
    public static bool HasPermitted(Party party, bool nationalDex) => party.Members.Any(p => IsPermitted(p.Species.Name, nationalDex));

    /// <summary>The first place of the team from <paramref name="from"/> on whose Pokémon may be walked, or -1 (the receptionist asks about each in turn).</summary>
    public static int Next(Party party, bool nationalDex, int from)
    {
        for (int slot = Math.Max(0, from); slot < party.Count; slot++)
            if (MayWalk(party.Members[slot], nationalDex)) return slot;
        return -1;
    }

    // ------------------------------------------------------------------ what it turns up

    /// <summary>
    /// The accessories a Pokémon turns up, by its family (<c>sMonFindableAccessories</c>), each row in the order of
    /// <see cref="AccessoryOdds"/>: four at 15 in a hundred, two at 10, one at 8, two at 5, one at 2.
    /// </summary>
    private static readonly string[][] Pools =
    {
        // Chimchar, Monferno, Infernape (and whoever is in none of the rows)
        new[] { "White Fluff", "Orange Fluff", "White Feather", "Round Pebble", "Small Leaf", "Blue Scale", "White Beard", "Thin Mushroom", "Big Scale", "Stump" },
        // Piplup, Prinplup, Empoleon
        new[] { "Pink Fluff", "Red Feather", "Yellow Feather", "Black Beard", "Narrow Scale", "White Fluff", "White Moustache", "Shed Claw", "Narrow Leaf", "Purple Scale" },
        // Turtwig, Grotle, Torterra
        new[] { "Jagged Boulder", "Snaggy Pebble", "Brown Fluff", "Round Pebble", "Black Moustache", "Shed Horn", "Narrow Scale", "Glitter Boulder", "Green Scale", "Thick Mushroom" },
        // Psyduck, Buneary, Drifloon
        new[] { "Pink Scale", "Blue Feather", "White Feather", "Blue Scale", "Thin Mushroom", "Snaggy Pebble", "White Beard", "Black Beard", "Big Leaf", "Glitter Boulder" },
        // Pikachu, Clefairy, Pachirisu, Happiny
        new[] { "Orange Fluff", "Brown Fluff", "Small Leaf", "Red Feather", "Yellow Feather", "Yellow Fluff", "Mini Pebble", "Big Scale", "Black Moustache", "Purple Scale" },
        // Jigglypuff, Torchic, Skitty, Shroomish
        new[] { "Pink Scale", "Shed Horn", "Pink Fluff", "Yellow Feather", "Shed Claw", "Black Fluff", "Jagged Boulder", "Big Leaf", "Green Scale", "Black Pebble" },
    };

    private static readonly string[][] Families =
    {
        new[] { "Chimchar", "Monferno", "Infernape" },
        new[] { "Piplup", "Prinplup", "Empoleon" },
        new[] { "Turtwig", "Grotle", "Torterra" },
        new[] { "Psyduck", "Buneary", "Drifloon" },
        new[] { "Pikachu", "Clefairy", "Pachirisu", "Happiny" },
        new[] { "Jigglypuff", "Torchic", "Skitty", "Shroomish" },
    };

    /// <summary>A roll of a hundred under each of these is the row's next place (<c>CalcAmitySquareFoundAccessory</c>).</summary>
    private static readonly int[] AccessoryOdds = { 15, 30, 45, 60, 70, 80, 88, 93, 98, 100 };

    /// <summary>The berries it turns up, and the roll of a hundred under which each is the one (<c>AmitySquare_FollowerMon_SetPickupItemVar</c>).</summary>
    public static readonly IReadOnlyList<string> Berries = new[]
    {
        "Magost Berry", "Cornn Berry", "Rabuta Berry", "Nomel Berry", "Spelon Berry", "Pamtre Berry", "Watmel Berry", "Durin Berry", "Belue Berry",
    };

    private static readonly int[] BerryOdds = { 20, 35, 50, 65, 72, 79, 86, 93, 100 };

    /// <summary>The row of accessories a species turns up; the first for one of no family, as the original's switch falls through.</summary>
    public static int PoolOf(string species)
    {
        int pool = Array.FindIndex(Families, family => family.Contains(species));
        return pool < 0 ? 0 : pool;
    }

    /// <summary>The accessory a roll of a hundred (0 to 99) finds for a species.</summary>
    public static string AccessoryFor(string species, int roll) => Pools[PoolOf(species)][Array.FindIndex(AccessoryOdds, odds => roll < odds)];

    /// <summary>The berry a roll of a hundred (0 to 99) finds.</summary>
    public static string BerryFor(int roll) => Berries[Array.FindIndex(BerryOdds, odds => roll < odds)];

    /// <summary>The steps walked since the walk began or the last find.</summary>
    public static int Steps(StoryState story) => story.Var(StepsVar);

    /// <summary>A step of the player's, anywhere: the count goes up until it stops at ten thousand (the last line of <c>Field_ProcessStep</c>).</summary>
    public static void CountStep(StoryState story)
    {
        if (story.Var(StepsVar) < MostSteps) story.AddVar(StepsVar, 1);
    }

    /// <summary>
    /// What the next find will be, decided ahead (<c>AmitySquare_FollowerMon_SetNextPickUpType</c>): a die of five
    /// (nought a berry, else an accessory), then a roll of a hundred for the accessory and one for the berry, both
    /// drawn whichever it is to be.
    /// </summary>
    public static void DrawNext(StoryState story, string species, Func<int, int> die)
    {
        story.SetVar(NextKindVar, die(5) == 0 ? 0 : 1);
        story.SetVar(NextAccessoryVar, Data.SuperContestDatabase.Accessory(AccessoryFor(species, die(100))));
        story.SetVar(NextBerryVar, Berries.ToList().IndexOf(BerryFor(die(100))));
    }

    /// <summary>The walk begins (<c>AmitySquare_EnterWithFollowerMon</c>): who walks, the count at nought, the first find decided.</summary>
    public static void Begin(StoryState story, int slot, Pokemon pokemon, Func<int, int> die)
    {
        story.SetVar(PartyVar, slot);
        story.SetVar(SpeciesVar, pokemon.Species.DexNumber);
        story.SetVar(StepsVar, 0);
        DrawNext(story, pokemon.Species.Name, die);
    }

    /// <summary>The Pokémon of the team that walks along, by the place the story keeps; null when that place is empty or holds an Egg.</summary>
    public static Pokemon? Walker(StoryState story, Party party)
    {
        int slot = story.Var(PartyVar);
        return slot >= 0 && slot < party.Count && !party.Members[slot].IsEgg ? party.Members[slot] : null;
    }

    /// <summary>
    /// Talking to the Pokémon walking along (<c>AmitySquare_FollowerMon</c>): with two hundred steps behind it, it is
    /// holding what was decided ahead, and the count starts again with the next find decided. The kind decided is
    /// tried first and the other when it doesn't fit (the bag, the Fashion Case's nine of a kind); with room for
    /// neither it has only a mood. <paramref name="name"/> is the item or the accessory.
    /// </summary>
    public static AmityFind Find(StoryState story, string species, FashionCase fashionCase, Func<int, int> die, out string name, bool bagHasRoom = true)
    {
        name = "";
        if (Steps(story) < StepsToFind) return AmityFind.Nothing;
        bool berryFirst = story.Var(NextKindVar) == 0;
        string berry = Berries[Math.Clamp(story.Var(NextBerryVar), 0, Berries.Count - 1)];
        int accessory = story.Var(NextAccessoryVar);
        story.SetVar(StepsVar, 0);
        DrawNext(story, species, die);

        bool fitsCase = fashionCase.CanFit(accessory);
        if (berryFirst ? bagHasRoom : !fitsCase && bagHasRoom)
        {
            name = berry;
            return AmityFind.Berry;
        }
        if (!fitsCase) return AmityFind.Nothing;
        name = Data.SuperContestDatabase.AccessoryName(accessory);
        return AmityFind.Accessory;
    }

    // ------------------------------------------------------------------ the man of the park

    /// <summary>What the man gives away, one a day (<c>sBerryAndAccessoryManOptions</c>): the nine berries, five of one, then seven accessories.</summary>
    public static readonly IReadOnlyList<string> Gifts = Berries.Concat(new[]
    {
        "Mini Pebble", "Glitter Boulder", "Black Pebble", "Big Scale", "Big Leaf", "Thick Mushroom", "Stump",
    }).ToList();

    /// <summary>How many of a berry he gives.</summary>
    public const int GiftBerries = 5;

    /// <summary>Whether a gift is an accessory (<c>CheckAmitySquareManGiftIsAccessory</c>: the first nine are berries).</summary>
    public static bool GiftIsAccessory(int gift) => gift >= Berries.Count;

    /// <summary>
    /// Where he stands by a die of five (<c>AmitySquare_OnTransition</c>): four places on the raised ground the huts
    /// lead to, and on a four where the square's own plan has him.
    /// </summary>
    public static readonly IReadOnlyList<(int X, int Y)> GiftManPlaces = new[] { (28, 14), (38, 8), (40, 21), (48, 41), (20, 13) };

    /// <summary>Coming into the square (<c>AmitySquare_OnTransition</c>): the day's gift is drawn, then where the man stands.</summary>
    public static void Arrive(StoryState story, Func<int, int> die)
    {
        story.SetVar(GiftVar, die(Gifts.Count));
        story.SetVar(GiftManPlaceVar, die(GiftManPlaces.Count));
    }

    // ------------------------------------------------------------------ the ruins' huts

    /// <summary>
    /// The nine huts that lead anywhere, by the tile in the middle of each (a tenth, in the east, has no way in).
    /// Whoever stands in a hut's middle and steps north, west or east is carried to another's.
    /// </summary>
    public static readonly IReadOnlyList<AmityHut> Huts = new AmityHut[]
    {
        new('A', 17, 18), new('B', 35, 17), new('C', 52, 27), new('D', 47, 10), new('E', 15, 12),
        new('F', 33, 9), new('G', 26, 11), new('H', 41, 18), new('I', 47, 36),
    };

    // Where each hut's north, west and east tile leads (AmitySquare_Warp1 to Warp27, by where the player is walked to)
    private static readonly string[] Leads = { "BBB", "AAA", "DFE", "CEF", "CGH", "CHG", "DIE", "DFI", "CEF" };

    /// <summary>
    /// The hut a step leads to: from the tile north, west or east of a hut's middle. Null for any other tile (the
    /// original's twenty-seven coordinate events, three to a hut).
    /// </summary>
    public static AmityHut? Leading(int x, int y)
    {
        for (int hut = 0; hut < Huts.Count; hut++)
        {
            int side = (x - Huts[hut].X, y - Huts[hut].Y) switch { (0, -1) => 0, (-1, 0) => 1, (1, 0) => 2, _ => -1 };
            if (side >= 0) return Huts.First(h => h.Name == Leads[hut][side]);
        }
        return null;
    }

    /// <summary>The number of the original's warp on a tile, 1 to 27, in its events' order (north, west, east of each hut); 0 for none.</summary>
    public static int WarpAt(int x, int y)
    {
        for (int hut = 0; hut < Huts.Count; hut++)
        {
            int side = (x - Huts[hut].X, y - Huts[hut].Y) switch { (0, -1) => 0, (-1, 0) => 1, (1, 0) => 2, _ => -1 };
            if (side >= 0) return hut * 3 + side + 1;
        }
        return 0;
    }
}
