using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Models;

public class Trainer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TrainerClass { get; set; } = "Trainer"; // e.g. "Youngster", "Lass", "Rival", "Champion"
    public Party Party { get => party; init => party = value; }

    private Party party = new();

    /// <summary>
    /// The trainer as a battle in a region the game crossed into meets them (plan 18 · W4): a copy whose team is copies,
    /// each put at the level <paramref name="levelOf"/> gives for its own (<see cref="Pokemon.Relevel"/>). The trainer of
    /// the map keeps their own team at their game's levels, so a rematch or a refreshed team is mapped afresh, never twice.
    /// </summary>
    public Trainer Relevelled(Func<int, int> levelOf)
    {
        var copy = (Trainer)MemberwiseClone();
        copy.party = new Party();
        foreach (var p in party.Members)
        {
            var mine = p.Clone();
            // What its level taught it stops at the games' own 100: the moves of our own past it (plan 06 · R31) are the
            // player's to earn, and a later region's trainer keeps their own game's moves (R31's question 8)
            mine.Relevel(levelOf(p.Level), Data.Ruleset.Platinum.LevelCap);
            copy.party.Add(mine);
        }
        copy.Items = Items.ToList();
        return copy;
    }
    /// <summary>
    /// A copy that shares nothing with the original: the team is clones and the items a list of its own (plan 13 · V2, the
    /// trainer a difficulty raises or lowers; the trainer of the map keeps its team as the import made it).
    /// </summary>
    public Trainer Copied()
    {
        var copy = (Trainer)MemberwiseClone();
        copy.party = new Party();
        foreach (var p in party.Members) copy.party.Add(p.Clone());
        copy.Items = Items.ToList();
        return copy;
    }

    /// <summary>
    /// A copy whose team is what <paramref name="change"/> makes of each Pokémon (plan 13 · V6: the randomiser's species);
    /// like the others it shares nothing with the original and the trainer of the map keeps their team.
    /// </summary>
    public Trainer WithTeamOf(Func<Pokemon, Pokemon> change)
    {
        var copy = (Trainer)MemberwiseClone();
        copy.party = new Party();
        foreach (var p in party.Members) copy.party.Add(change(p));
        copy.Items = Items.ToList();
        return copy;
    }

    public int PrizeMoney { get; set; } = 300;
    public string DialogueBefore { get; set; } = string.Empty;
    public string DialogueAfter { get; set; } = string.Empty;
    public bool IsDefeated { get; set; } = false;
    public int SightRange { get; set; } = 3; // Tiles of vision for line of sight challenge

    /// <summary>Battles two Pokémon at a time (twins, couples), if the player has two that can fight.</summary>
    public bool DoubleBattle { get; set; }

    /// <summary>
    /// A format of the later games the trainer battles in (plan 14): <c>"triple"</c> three at a time (B6), where the rules
    /// have triples and both sides can field three (<see cref="Battle.BattleFormats.FallBack"/>); null for Platinum's
    /// single or double (<see cref="DoubleBattle"/> stays, and means <c>"double"</c>). None of Platinum's trainers has one.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>The format the trainer's <see cref="Format"/> asks for: a triple for <c>"triple"</c> (plan 14 · B6), else null.</summary>
    public Battle.BattleFormat? AskedFormat => Format switch
    {
        "triple" => Battle.BattleFormat.Triple,
        // Three on a turntable, the front one fighting (plan 14 · B7)
        "rotation" => Battle.BattleFormat.Rotation,
        _ => null
    };

    /// <summary>How the trainer thinks in battle (plan 06 · R9): Platinum's flags from the trainer's data.</summary>
    public Battle.Sim.Ai.AiFlags Ai { get; set; } = Battle.Sim.Ai.AiFlags.Basic;

    /// <summary>The items the trainer can use in battle, by name (Platinum's: up to four, Potions to Full Restores).</summary>
    public List<string> Items { get; set; } = new();

    /// <summary>
    /// The trainer has a Key Stone and may Mega Evolve one Pokémon a battle (plan 06 · R20), under rules that have
    /// it. None of Platinum's trainers has one; the region that hands Mega Evolution over gives its trainers theirs.
    /// </summary>
    public bool KeyStone { get; set; }

    /// <summary>
    /// The trainer has a Z-Ring and may use one Z-Move and one Ultra Burst a battle (plan 06 · R21), under rules that
    /// have them. None of Platinum's trainers has one; Alola's chapter gives its trainers theirs.
    /// </summary>
    public bool ZRing { get; set; }

    /// <summary>The trainer has a Dynamax Band and may Dynamax one Pokémon a battle (plan 06 · R22), under rules that have it. None of Platinum's trainers has one; Galar's chapter gives its trainers theirs.</summary>
    public bool DynamaxBand { get; set; }

    /// <summary>The trainer has a Mega Max Band and may Dynamax a Mega of theirs (plan 06 · R20c), under rules that have it. None of Platinum's trainers has one.</summary>
    public bool MegaMaxBand { get; set; }

    /// <summary>The trainer has a Tera Orb and may Terastallize one Pokémon a battle (plan 06 · R23), under rules that have it. None of Platinum's trainers has one; Paldea's chapter gives its trainers theirs.</summary>
    public bool TeraOrb { get; set; }

    /// <summary>
    /// The team, prize money and mind were taken from Platinum's data (<c>trainers.json</c>), because the map wrote
    /// only the trainer's id: a map written back out leaves them to the data again (<c>MapFile.FromMap</c>).
    /// </summary>
    public bool FromPlatinum { get; set; }

    /// <summary>
    /// The look of the person who carries the trainer in the field (their <c>npcType</c>), or null where nobody does
    /// (plan 11 · C1). The battle draws the class's look, and this where the class names nobody in particular (a
    /// Leader, a Commander) or has a look for each of two people (<see cref="Data.CharacterStyles.OfClass"/>).
    /// </summary>
    public string? Look { get; set; }

    /// <summary>The class and the name as the battle's lines say them: "Youngster Tristan" (plan 17 · N2).</summary>
    public string FullTitle => Text.Of("battle.fullTitle", ("class", ClassName(TrainerClass)), ("name", Name));

    /// <summary>
    /// A trainer class as the chosen language names it: the <c>classes</c> section keyed by the English class's
    /// letters and digits ("Poké Kid" is <c>classes.PokeKid</c>), a Master Trainer's by its species, and a class the
    /// table doesn't have as it is written.
    /// </summary>
    public static string ClassName(string trainerClass)
    {
        const string master = " Master";
        if (trainerClass.EndsWith(master, StringComparison.Ordinal) && trainerClass.Length > master.Length)
            return Text.Of("classes.master", ("species", trainerClass[..^master.Length]));
        string name = KeyOf(trainerClass);
        string key = $"classes.{name}";
        return Text.Has(key) ? Text.Of(key) : trainerClass;
    }

    /// <summary>A class's key in the table: its letters without their accents, and its digits.</summary>
    public static string KeyOf(string trainerClass)
    {
        var key = new System.Text.StringBuilder();
        foreach (char c in trainerClass.Normalize(System.Text.NormalizationForm.FormD))
            if (char.IsAsciiLetterOrDigit(c)) key.Append(c);
        return key.ToString();
    }
}
