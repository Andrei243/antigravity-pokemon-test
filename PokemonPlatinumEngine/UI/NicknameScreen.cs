using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Naming a Pokémon (plan 12 · Q19; plan 13 · V4's prompt, built here first): the introduction's keyboard
/// (<see cref="NameEntry"/>, drawn by <see cref="ModernUi.DrawNameEntry"/>) over the field, ten letters at most, on the
/// name the Pokémon has now. OK closes it with the name given, or with the species' own name when it was left empty
/// (<see cref="Nickname"/> is then empty, the Pokémon having none). The party menu's RENAME opens it; plan 06's question
/// after a catch, the Nuzlocke's and Eterna's Name Rater ask the same prompt. Its logic takes no input
/// (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="ToDone"/>, <see cref="Advance"/>).
/// </summary>
public sealed class NicknameScreen
{
    /// <summary>The most letters a Pokémon's name has.</summary>
    public const int MaxLength = 10;

    /// <summary>How long the keyboard takes to slide into place.</summary>
    public const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public NameEntry? Entry { get; private set; }

    /// <summary>The Pokémon being named.</summary>
    public Pokemon? Target { get; private set; }

    /// <summary>Seconds since the keyboard opened.</summary>
    public float Age { get; private set; }

    /// <summary>True when the keyboard was left with B on an empty name: the Pokémon's name stands as it was.</summary>
    public bool Left { get; private set; }

    /// <summary>The species' own name, which an empty name goes back to.</summary>
    public string SpeciesName => Target?.Species.DisplayName ?? "";

    /// <summary>
    /// The nickname to keep once the keyboard has closed (empty for none: the species' name given, or the keyboard left
    /// empty); null while it is open or when it was left with B.
    /// </summary>
    public string? Nickname
    {
        get
        {
            if (IsActive || Entry == null || Left) return null;
            string given = Entry.Result(SpeciesName);
            return string.Equals(given, SpeciesName, StringComparison.OrdinalIgnoreCase) ? string.Empty : given;
        }
    }

    /// <summary>Opens the keyboard for a Pokémon, on the name it has now (empty for none).</summary>
    public void Open(Pokemon pokemon)
    {
        Target = pokemon;
        Entry = new NameEntry(pokemon.Nickname, MaxLength);
        IsActive = true;
        Left = false;
        Age = 0f;
    }

    public void Move(int dx, int dy)
    {
        if (!IsActive || Entry == null || (dx == 0 && dy == 0)) return;
        Entry.Move(dx, dy);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: the key under the cursor; OK closes the keyboard.</summary>
    public void Confirm()
    {
        if (!IsActive || Entry == null) return;
        var key = Entry.Press();
        AudioManager.PlaySound(key == NameKey.Delete ? "cancel" : "select");
        if (Entry.Done) IsActive = false;
    }

    /// <summary>The B button: a letter deleted, and with none left the keyboard is left with the name as it was.</summary>
    public void Cancel()
    {
        if (!IsActive || Entry == null) return;
        if (Entry.Backspace()) AudioManager.PlaySound("cancel");
        else
        {
            Left = true;
            IsActive = false;
            AudioManager.PlaySound("cancel");
        }
    }

    /// <summary>The Start button: the cursor to OK.</summary>
    public void ToDone()
    {
        if (!IsActive || Entry == null) return;
        Entry.ToDone();
        AudioManager.PlaySound("cursor");
    }

    public void Advance(float dt)
    {
        if (IsActive) Age += dt;
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0) Move(dx, dx != 0 ? 0 : dy);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Menu)) ToDone();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive || Entry == null || Target == null) return;
        // The Pokémon being named stands on the plate where the introduction shows the player, at its summary's scale
        ModernUi.DrawNameEntry(screenWidth, screenHeight, Entry, PixelArtGenerator.SpriteOf(Target), PlayerIdentity.Look,
            UiMotion.EaseOut(Math.Clamp(Age / AppearTime, 0f, 1f)), Text.Of("party.rename.title", ("name", Target.DisplayName.ToUpperInvariant())),
            SpeciesName, Text.Of("party.rename.blurb", ("species", SpeciesName)), spriteScale: 3);
    }
}
