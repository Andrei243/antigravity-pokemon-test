using System.Collections.Generic;

namespace PokemonPlatinumEngine.Data;

/// <summary>A name in the credits, and the smaller note under it (a licence, what a source was read for); a note is optional.</summary>
public sealed class CreditEntry
{
    public string Name { get; set; } = "";
    public string? Note { get; set; }
}

/// <summary>A section of the credits: a heading and its names. One section lists the Champion's team after its own names.</summary>
public sealed class CreditSection
{
    public string Heading { get; set; } = "";
    public List<CreditEntry> Entries { get; set; } = new();

    /// <summary>The section the Champion's team is listed in, each Pokémon a name with its species and level as its note.</summary>
    public bool Team { get; set; }
}

/// <summary>What <c>credits.json</c> holds: the sections in the order they roll, and the last card's two lines.</summary>
public sealed class CreditsFile
{
    public List<CreditSection> Sections { get; set; } = new();
    public string End { get; set; } = "THE END";
    public string Farewell { get; set; } = "";
}

/// <summary>
/// This remake's credits (plan 02 · S14; <c>Data/credits.json</c>, hand-written): the project, what it owes the
/// original's makers, and the sources <c>docs/art/CREDITS.md</c> and <c>THIRD-PARTY-NOTICES.md</c> list, in our own
/// words. Nothing of the original's credit roll is in it. A name may hold <c>{player}</c>. The file is Latin-1 only,
/// as the interface's font is. <see cref="UI.CreditsRoll"/> lays it out; <see cref="UI.CreditsScreen"/> plays it.
/// </summary>
public static class CreditsDatabase
{
    public const string FileName = "credits.json";

    private static readonly CreditsFile File = GameDataFiles.Load<CreditsFile>(FileName);

    public static IReadOnlyList<CreditSection> Sections => File.Sections;

    /// <summary>The last card's lettering, and the line under it.</summary>
    public static string End => File.End;
    public static string Farewell => File.Farewell;
}
