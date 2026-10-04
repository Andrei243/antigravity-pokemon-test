using System;

namespace PokemonPlatinumEngine.Core;

/// <summary>The two characters the player can be, as in the games: the boy and the girl.</summary>
public enum PlayerLook { Boy, Girl }

/// <summary>
/// Who the player is in the game being played: the name and the look chosen in the introduction. Whoever
/// shows the player (the field, a battle, the Trainer Card) or names them (messages, written lines) asks
/// here. The character the player didn't choose is the professor's assistant, as in the games.
/// Written lines name them with <c>{player}</c> and <c>{assistant}</c>; plan 02's scripts will take this over.
/// </summary>
public static class PlayerIdentity
{
    /// <summary>The longest name the games allow.</summary>
    public const int MaxNameLength = 7;

    public static string Name { get; private set; } = DefaultName(PlayerLook.Boy);
    public static PlayerLook Look { get; private set; }

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

    /// <summary>The player's own character type.</summary>
    public static string Character => CharacterOf(Look);

    /// <summary>The professor's assistant: the one the player isn't.</summary>
    public static string AssistantName => DefaultName(Other(Look));

    /// <summary>
    /// The character type a person of the maps is drawn as: the assistant is whichever character the player
    /// isn't, the player's own type follows their look, anyone else is themselves.
    /// </summary>
    public static string CharacterFor(string npcType) => CharacterFor(npcType, Look);

    public static string CharacterFor(string npcType, PlayerLook look) =>
        npcType.Equals("Assistant", StringComparison.OrdinalIgnoreCase) ? CharacterOf(Other(look))
        : npcType.Equals("Player", StringComparison.OrdinalIgnoreCase) ? CharacterOf(look)
        : npcType;

    /// <summary>A written line with the player's and the assistant's names put in.</summary>
    public static string Fill(string text) => Fill(text, Name, Look);

    public static string Fill(string text, string name, PlayerLook look) =>
        text.Contains('{') ? text.Replace("{player}", name).Replace("{assistant}", DefaultName(Other(look))) : text;

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
