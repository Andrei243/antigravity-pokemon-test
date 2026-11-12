using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>A guest of the house of titles, as the day's draw has them: who, of which kind, and what a win counts.</summary>
public enum TitlesGuestKind
{
    /// <summary>A member of ours in the west place (Mirabelle, Tristan, Celeste).</summary>
    West,
    /// <summary>A member of ours in the east place, from the title of a Friend of the House on (Octave, Isaure, Corentin).</summary>
    East,
    /// <summary>Kalos's Leader of the day, met with their rematch team.</summary>
    Leader,
    /// <summary>A visitor of another finished region: a Leader, an Elite Four member or a Champion by the title.</summary>
    Visitor
}

/// <summary>
/// The house of titles on Kalos's Route 7 (plan 37 · FC4, after X and Y's Battle Château; plan 23 stood its hall,
/// <c>KalosRoute7_BattleChateau</c>, and its first part in scripts alone; <c>docs/mechanics/rulings.md</c>, "The house of
/// titles"). The player's own team battles at its own levels and is paid EXP and money as after any trainer's battle
/// (decision 4). The house's own members bring three of the later sets (<see cref="FacilityTrainers"/>) at a level the
/// title sets over the highest of the player's team, made at met levels and never relevelled (<see cref="Trainer.LevelsKept"/>);
/// Kalos's Leaders come on their days of the week with their rematch teams, and once another region's Hall of Fame is set
/// a visitor of that region comes each day, its Leaders at first, its Elite Four from the house's seventh title (Steward)
/// and its Champions at the eighth (Master of the House), each met as <see cref="RematchLevels"/> meets them. Eight titles
/// of ours at thresholds of points. Every draw is on a generator seeded by the day's seed (a story variable the hall's draw
/// sets once a day) and who is drawn, so the day's guests are the same all day and new the next. No drawing or input.
/// </summary>
public static class TitlesHouse
{
    /// <summary>The house's word in a script (<c>facility titles ...</c>).</summary>
    public const string Place = "titles";

    /// <summary>The story variables the house keeps: the points, the title (0 to 7), the day's seed and the day's visitor (their number in the tree's cast, from 1).</summary>
    public const string PointsVariable = "VAR_KALOS_CHATEAU_POINTS", TitleVariable = "VAR_KALOS_CHATEAU_TITLE",
        SeedVariable = "VAR_KALOS_CHATEAU_SEED", VisitorVariable = "VAR_KALOS_CHATEAU_VISITOR";

    /// <summary>What a visitor's team is battled under: the house's own id before the trainer's.</summary>
    public const string VisitorTrainer = "titles_visitor_";

    /// <summary>The titles, 0 to 7 (the names are <c>ui.json</c>'s <c>titlesHouse.title.N</c>).</summary>
    public const int HighestTitle = 7;

    /// <summary>The points each title is reached at (title 1 at 3, ... title 7 at 90), a table of ours.</summary>
    public static readonly int[] Thresholds = { 0, 3, 8, 15, 25, 40, 60, 90 };

    /// <summary>The title a number of points earns.</summary>
    public static int TitleFor(int points)
    {
        int title = 0;
        for (int t = 1; t <= HighestTitle; t++)
            if (points >= Thresholds[t]) title = t;
        return title;
    }

    /// <summary>How far over (or under) the player's highest level the members stand at each title, a table of ours.</summary>
    public static readonly int[] LevelOffsets = { -6, -4, -2, 0, 1, 2, 3, 5 };

    /// <summary>The members' level at a title, over the highest level of the player's team.</summary>
    public static int LevelFor(int title, int highest) =>
        Math.Clamp(highest + LevelOffsets[Math.Clamp(title, 0, HighestTitle)], 1, Ruleset.HighestLevel);

    /// <summary>The tiers of the later sets the members bring: the west place's by the title, the east place's a step up.</summary>
    public static (int Lowest, int Highest) TiersFor(int title, bool east)
    {
        int step = title <= 2 ? 0 : title <= 5 ? 1 : 2;
        if (east) step++;
        return step switch { 0 => (1, 2), 1 => (2, 3), 2 => (3, 4), _ => (4, 4) };
    }

    /// <summary>The points a win over a guest counts toward the titles: west 1, east 2, a Leader 3, a visiting Elite Four member 4, a Champion 5.</summary>
    public static int PointsFor(TitlesGuestKind kind, string? role = null) => kind switch
    {
        TitlesGuestKind.West => 1,
        TitlesGuestKind.East => 2,
        TitlesGuestKind.Leader => 3,
        _ => role switch { "champion" => 5, "elite" => 4, _ => 3 }
    };

    /// <summary>The house's members of ours, by their person's key and the trainer of Kalos's table they are (their name and class).</summary>
    public static readonly IReadOnlyDictionary<string, (string Trainer, bool East)> Members = new Dictionary<string, (string, bool)>(StringComparer.Ordinal)
    {
        ["mirabelle"] = ("kalos_chateau_mirabelle", false),
        ["tristan"] = ("kalos_chateau_tristan", false),
        ["celeste"] = ("kalos_chateau_celeste", false),
        ["octave"] = ("kalos_chateau_octave", true),
        ["isaure"] = ("kalos_chateau_isaure", true),
        ["corentin"] = ("kalos_chateau_corentin", true),
    };

    /// <summary>How many a member brings.</summary>
    public const int MemberBrings = 3;

    private static Func<int> DrawsFor(int seed, string who)
    {
        uint mix = 0x811C9DC5;
        foreach (char c in who) mix = (mix ^ c) * 0x01000193;
        var rng = new BattleRandom(((uint)seed & 0xFFFF) ^ mix);
        return rng.Next16;
    }

    /// <summary>
    /// A member's battle: their class and name from Kalos's table, three later sets of their place's tiers at the title's
    /// level over the player's highest, the same all day for the day's seed. Marked as kept, so no region relevels them.
    /// </summary>
    public static Trainer MemberTeam(string member, int title, int highest, int seed)
    {
        if (!Members.TryGetValue(member, out var who)) throw new ArgumentException($"The house of titles has no member {member}", nameof(member));
        var record = TrainerDatabase.Get(who.Trainer) ?? throw new InvalidOperationException($"No trainer {who.Trainer} for the house of titles");
        var (low, high) = TiersFor(title, who.East);
        int level = LevelFor(title, highest);
        var drawn = FacilityTrainers.Draw(MemberBrings, low, high, FacilityTrainers.IvsFor(title), DrawsFor(seed, member), level);
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        trainer.Party.Clear();
        foreach (var p in drawn.Pokemon) trainer.Party.Add(p);
        trainer.LevelsKept = true;
        return trainer;
    }

    /// <summary>
    /// Who of the tree's cast (<see cref="PartnersTree.Cast"/>) may visit at a title: of the regions finished but Kalos (its
    /// own come on their days), Leaders and kahunas below the seventh title, Elite Four from it, Champions at the eighth.
    /// </summary>
    public static List<TreeCastRecord> Visitors(int title, Func<string, bool> has)
    {
        string[] roles = title >= HighestTitle ? new[] { "champion" } : title >= HighestTitle - 1 ? new[] { "elite" } : new[] { "leader", "kahuna" };
        return PartnersTree.Eligible(has).Where(c => c.Region != "Kalos" && roles.Contains(c.Role)).ToList();
    }

    /// <summary>The day's visitor drawn for the day's seed, their number in the cast from 1; 0 when no region but Kalos is finished.</summary>
    public static int VisitorOfTheDay(int title, Func<string, bool> has, int seed)
    {
        var from = Visitors(title, has);
        if (from.Count == 0) return 0;
        var member = from[(DrawsFor(seed, "visitor")() & 0xFFFF) % from.Count];
        return PartnersTree.NumberOf(member.Trainer);
    }

    /// <summary>A visitor's battle: their own team of the tables under their own id, which the battle meets as rematches are met.</summary>
    public static Trainer VisitorTeam(TreeCastRecord visitor)
    {
        var record = PartnersTree.RecordOf(visitor) ?? throw new InvalidOperationException($"No trainer {visitor.Trainer} to visit the house of titles");
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        return trainer;
    }

    /// <summary>The visitor dressed for the hall (their class, name and look).</summary>
    public static FrontierOpponent VisitorDress(TreeCastRecord visitor)
    {
        var record = PartnersTree.RecordOf(visitor) ?? throw new InvalidOperationException($"No trainer {visitor.Trainer} to visit the house of titles");
        return new FrontierOpponent { Number = 0, Pokemon = new List<Pokemon>(), Dress = new FrontierDress(record.Id, record.Class, record.Name, visitor.Look) };
    }
}
