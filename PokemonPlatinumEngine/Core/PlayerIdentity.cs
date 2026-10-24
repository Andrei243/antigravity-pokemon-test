using System;

namespace PokemonPlatinumEngine.Core;

/// <summary>The two characters the player can be, as in the games: the boy and the girl.</summary>
public enum PlayerLook { Boy, Girl }

/// <summary>
/// Who the player is in the game being played: the name and the look chosen in the introduction. Whoever
/// shows the player (the field, a battle, the Trainer Card) or names them (messages, written lines) asks
/// here. The character the player didn't choose is the professor's assistant, as in the games.
/// Written lines name them with <c>{player}</c> and <c>{assistant}</c>, and the rival, the friend from next door
/// whom the player names last in the introduction (plan 02 · S4), with <c>{rival}</c>.
/// </summary>
public static class PlayerIdentity
{
    /// <summary>The longest name the games allow.</summary>
    public const int MaxNameLength = 7;

    public static string Name { get; private set; } = DefaultName(PlayerLook.Boy);
    public static PlayerLook Look { get; private set; }

    /// <summary>The rival's name, as the player gave it.</summary>
    public static string RivalName { get; private set; } = "Barry";

    /// <summary>
    /// The trainer's whole number (plan 06 · R15): the Trainer Card's ID in the low half and the hidden half the card
    /// doesn't show in the high (the original's 32-bit <c>TrainerInfo_ID</c>). A Pokémon the player meets, is given or
    /// hatches is shiny by its personality against this number (<see cref="Models.Personality"/>). Set as a game
    /// begins or loads; nought until then, which is what tests (that must not set it) make their Pokémon against.
    /// </summary>
    public static uint Number { get; private set; }

    public static void SetNumber(uint number) => Number = number;

    /// <summary>
    /// The rival's own name for a game that has none of its region's to go by: Sinnoh's (the record of a region
    /// holds the name its introduction offers, <see cref="Data.Region.Rival"/>; plan 18 · W1), which an older save and
    /// a test that names no region read.
    /// </summary>
    public static string DefaultRivalName => Data.RegionDatabase.Get(Data.RegionDatabase.Sinnoh)?.Rival ?? "Barry";

    /// <summary>
    /// Names the rival; an empty name takes the region's own (Sinnoh's when none is given). With a region (plan 18 · W4,
    /// part b), the name is that region's rival's and the region is the one the player is in; with none, every region's
    /// name is forgotten and this is the one rival (what a test puts back as it ends).
    /// </summary>
    public static void SetRival(string? name, string? regionsOwn = null, string? region = null)
    {
        RivalName = Clean(name) is { Length: > 0 } given ? given : regionsOwn is { Length: > 0 } own ? own : DefaultRivalName;
        if (region == null)
        {
            rivals.Clear();
            RivalRegion = null;
            return;
        }
        rivals[region] = RivalName;
        RivalRegion = region;
    }

    // Each region's rival as the player named them (plan 18 · W4, part b), by the region's id
    private static readonly System.Collections.Generic.Dictionary<string, string> rivals = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The names the player gave each region's rival, by the region's id: the introduction's for the region the game began in, a <c>namerival</c>'s for the others.</summary>
    public static System.Collections.Generic.IReadOnlyDictionary<string, string> RivalNames => rivals;

    /// <summary>The region whose rival <see cref="RivalName"/> is: the one the player is in (<see cref="InRegion"/>); null before any.</summary>
    public static string? RivalRegion { get; private set; }

    /// <summary>
    /// Every region's rival as a save keeps them (plan 18 · W4, part b). An older save's one name, <c>RivalName</c>, is
    /// the rival of the region it began in, which is the one its introduction asked about.
    /// </summary>
    public static void RestoreRivals(System.Collections.Generic.IReadOnlyDictionary<string, string>? names, string? oldName, string oldRegion)
    {
        rivals.Clear();
        if (names != null)
            foreach (var (region, given) in names)
                if (Clean(given) is { Length: > 0 } kept) rivals[region] = kept;
        if (!rivals.ContainsKey(oldRegion) && Clean(oldName) is { Length: > 0 } old) rivals[oldRegion] = old;
    }

    /// <summary>The rival of a region: the name the player gave them, or the region's own (the hero the player isn't, where that is the rival).</summary>
    public static string RivalOf(Data.Region region) =>
        rivals.TryGetValue(region.Id, out var given) ? given : region.RivalFor(Look) ?? DefaultRivalName;

    /// <summary>Whether the player has named a region's rival yet.</summary>
    public static bool HasNamedRival(string region) => rivals.ContainsKey(region);

    /// <summary>
    /// The player is in a region (plan 18 · W4, part b): <c>{rival}</c> names its rival from now on. The engine calls it
    /// on every arrival, as a game begins and as it loads.
    /// </summary>
    public static void InRegion(Data.Region region)
    {
        RivalRegion = region.Id;
        RivalName = RivalOf(region);
    }

    /// <summary>Sets who the player is; an empty name takes the look's own.</summary>
    public static void Set(string? name, PlayerLook look)
    {
        Look = look;
        Name = Clean(name) is { Length: > 0 } given ? given : DefaultName(look);
    }

    /// <summary>The name the games give each character when none is entered.</summary>
    public static string DefaultName(PlayerLook look) => look == PlayerLook.Girl ? "Dawn" : "Lucas";

    /// <summary>The character type (as <c>CharacterModels</c> knows it) of a look.</summary>
    public static string CharacterOf(PlayerLook look) => look == PlayerLook.Girl ? "DAWN" : "PLAYER";

    public static PlayerLook Other(PlayerLook look) => look == PlayerLook.Girl ? PlayerLook.Boy : PlayerLook.Girl;

    /// <summary>
    /// What the player wears (plan 11 · C9): set as a game begins or loads and when the wardrobe closes. Only drawing
    /// reads it, through <see cref="Character"/>.
    /// </summary>
    public static Outfit Outfit { get; private set; } = Outfit.Own;

    public static void SetOutfit(Outfit? outfit) => Outfit = outfit ?? Outfit.Own;

    /// <summary>The player's own character type, dressed in their outfit (the plain look's when nothing is chosen).</summary>
    public static string Character => Outfit.Dress(CharacterOf(Look), Outfit);

    /// <summary>The professor's assistant: the one the player isn't.</summary>
    public static string AssistantName => DefaultName(Other(Look));

    /// <summary>
    /// The character type a person of the maps is drawn as: the assistant is whichever character the player
    /// isn't, the player's own type follows their look (and, for the game's own player, their outfit), anyone else
    /// is themselves.
    /// </summary>
    public static string CharacterFor(string npcType) =>
        npcType.Equals("Player", StringComparison.OrdinalIgnoreCase) ? Character : CharacterFor(npcType, Look);

    public static string CharacterFor(string npcType, PlayerLook look) =>
        npcType.Equals("Assistant", StringComparison.OrdinalIgnoreCase) ? CharacterOf(Other(look))
        : npcType.Equals("Player", StringComparison.OrdinalIgnoreCase) ? CharacterOf(look)
        : RivalLook(npcType, look) ?? npcType;

    /// <summary>
    /// A rival who is the hero the player isn't (plan 21 · H2: Hoenn's May and Brendan) is written
    /// <c>Rival:MAY:BRENDAN</c>, the look of a boy's rival and then of a girl's; anything else is no such rival.
    /// </summary>
    private static string? RivalLook(string npcType, PlayerLook look)
    {
        if (!npcType.StartsWith("Rival:", StringComparison.OrdinalIgnoreCase)) return null;
        var parts = npcType.Split(':');
        return parts.Length == 3 ? parts[look == PlayerLook.Girl ? 2 : 1] : null;
    }

    /// <summary>A written line with the player's, the assistant's and the rival's names put in.</summary>
    public static string Fill(string text) => Fill(text, Name, Look, RivalName);

    public static string Fill(string text, string name, PlayerLook look, string? rival = null) =>
        text.Contains('{') ? text.Replace("{player}", name).Replace("{assistant}", DefaultName(Other(look))).Replace("{rival}", rival ?? DefaultRivalName) : text;

    /// <summary>A name as it may be kept: trimmed, at most seven characters, nothing that would break a line.</summary>
    public static string Clean(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var kept = new System.Text.StringBuilder();
        foreach (char c in name.Trim())
        {
            if (char.IsControl(c) || c is '{' or '}') continue;
            kept.Append(c);
            if (kept.Length == MaxNameLength) break;
        }
        return kept.ToString().TrimEnd();
    }
}
