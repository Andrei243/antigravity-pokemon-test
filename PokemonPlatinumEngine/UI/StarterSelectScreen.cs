using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The choice of a partner from the professor's briefcase: Sinnoh's three as three cards, and a question
/// before the choice is final. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>,
/// <see cref="Cancel"/>), so tests and the harness drive it.
/// </summary>
public class StarterSelectScreen
{
    private const float AppearTime = 0.45f;

    /// <summary>The level the partners are given at.</summary>
    public const int Level = 5;

    /// <summary>The region whose briefcase this is (plan 18 · W1): its three partners, in the order the file lists them. Sinnoh's until opened.</summary>
    public Region Region { get; private set; } = RegionDatabase.Get(RegionDatabase.Sinnoh)!;

    /// <summary>The species on the three cards: the region's own, or in a randomised run (plan 13 · V6) the species its slots go to.</summary>
    public IReadOnlyList<string> Starters { get; private set; } = RegionDatabase.Get(RegionDatabase.Sinnoh)!.Starters;

    /// <summary>The slots the cards stand for, which the story goes by (the rival's team, <c>if starter</c>): the region's own three, whatever the cards show.</summary>
    public IReadOnlyList<string> Slots => Region.Starters;

    /// <summary>The slot of the card chosen: what <c>StoryState.ChooseStarter</c> is told, whichever species the card shows.</summary>
    public string ChosenSlot => Slots[Math.Clamp(SelectedIndex, 0, Slots.Count - 1)];

    private readonly UiReveal asking = new(0.18f, 0.1f);
    private float openAge;

    public int SelectedIndex { get; set; }
    public bool IsActive { get; set; }

    /// <summary>True while "Choose …?" is up; <see cref="AnswerYes"/> is where its cursor is (it opens on going back).</summary>
    public bool ConfirmingSelection { get; set; }
    public bool AnswerYes { get; private set; }

    public void Open(Region? region = null, Randomizer? random = null)
    {
        Region = region ?? RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        Starters = random?.Starters(Region.Starters) ?? Region.Starters;
        IsActive = true;
        SelectedIndex = 0;
        ConfirmingSelection = false;
        asking.Snap(false);
        openAge = 0f;
        AudioManager.PlaySound("select");
    }

    public void Close() => IsActive = false;

    /// <summary>Left and right: between the three, or between the two answers while the question is up.</summary>
    public void Move(int step)
    {
        if (step == 0) return;
        if (ConfirmingSelection) AnswerYes = step > 0;
        else SelectedIndex = UiNav.Wrap(SelectedIndex, step, Starters.Count);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: asks first, then gives the Pokémon once the answer is yes.</summary>
    public Pokemon? Confirm()
    {
        if (!ConfirmingSelection)
        {
            ConfirmingSelection = true;
            AnswerYes = false;
            asking.Open();
            AudioManager.PlaySound("select");
            if (PokemonDatabase.Get(Starters[SelectedIndex]) is { } starter) AudioManager.PlayCry(starter);
            return null;
        }
        if (!AnswerYes)
        {
            Cancel();
            return null;
        }
        var pokemon = new Pokemon(PokemonDatabase.Get(Starters[SelectedIndex])!, Level);
        AudioManager.PlayFanfare(MusicRole.FanfarePokemon);
        Close();
        return pokemon;
    }

    /// <summary>The B button: takes the question away. The briefcase itself can't be left without choosing.</summary>
    public void Cancel()
    {
        if (!ConfirmingSelection) return;
        ConfirmingSelection = false;
        asking.Close();
        AudioManager.PlaySound("cancel");
    }

    public Pokemon? Update(float dt = 1f / 60f)
    {
        if (!IsActive) return null;
        openAge += dt;
        asking.Update(dt);

        if (InputManager.IsActionPressed(GameAction.Left)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Right)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) return Confirm();
        return null;
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawStarters(screenWidth, screenHeight, this, Math.Clamp(openAge / AppearTime, 0f, 1f), asking.Shown, asking.Visible);
    }
}
