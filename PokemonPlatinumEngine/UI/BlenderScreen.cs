using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the Berry Blender is.</summary>
public enum BlenderPhase
{
    /// <summary>The berries are in; the player is told what to do.</summary>
    Ready,
    /// <summary>Three, two, one.</summary>
    Count,
    /// <summary>The arm turns and the player presses as it passes their pocket.</summary>
    Play,
    /// <summary>The Pokéblock that came out.</summary>
    Result,
    Done
}

/// <summary>
/// The Berry Blender at Lilycove's Contest Lobby (plan 21 · H11; style guide, "The Berry Blender"): the berries of the
/// player and of the others at the drum, the arm turning, the pressing, and the Pokéblock that comes out into the
/// Pokéblock Case. The rules are <see cref="BlenderGame"/>'s and <see cref="Pokeblocks.Blend"/>'s; this screen is
/// their clock and their button. A function of <see cref="Advance"/> and <see cref="PressConfirm"/>, so tests and the
/// harness play it without a window.
/// </summary>
public sealed class BlenderScreen
{
    private const float Tick = 1f / BlenderGame.FramesPerSecond;

    /// <summary>The seconds each of three, two, one is up.</summary>
    public const float CountStep = 0.8f;

    public bool IsActive { get; private set; }
    public BlenderPhase Phase { get; private set; } = BlenderPhase.Done;
    public float PhaseTime { get; private set; }

    /// <summary>The berries at the drum, the player's first.</summary>
    public IReadOnlyList<BlenderBerry> Berries { get; private set; } = Array.Empty<BlenderBerry>();

    /// <summary>The names of those at the drum, the player's first.</summary>
    public IReadOnlyList<string> Names { get; private set; } = Array.Empty<string>();

    public BlenderGame? Game { get; private set; }

    /// <summary>What came out, and whether the case had room for it.</summary>
    public Pokeblock? Result { get; private set; }
    public bool Kept { get; private set; }

    /// <summary>A line at the foot of the screen.</summary>
    public string Line { get; private set; } = "";

    /// <summary>The number being counted down (3, 2, 1), and the speed the game is shown at (the fastest the arm turned so far).</summary>
    public int CountNumber => Math.Max(1, 3 - (int)(PhaseTime / CountStep));

    private PokeblockCase target = new();
    private Random rng = new(0);
    private float carry;

    /// <summary>
    /// Begins a blend: the player's berry and the others', each seat a name. <paramref name="chance"/> draws the other
    /// seats' presses and, for a black Pokéblock, its flavours.
    /// </summary>
    public void Begin(IReadOnlyList<BlenderBerry> berries, IReadOnlyList<string> names, PokeblockCase into, Random chance)
    {
        Berries = berries;
        Names = names;
        target = into;
        rng = chance;
        Game = new BlenderGame(berries.Count, chance);
        Result = null;
        Kept = false;
        carry = 0f;
        IsActive = true;
        Enter(BlenderPhase.Ready);
        Line = Text.Of("blender.ready");
        AudioManager.PlaySound("menu_open");
    }

    private void Enter(BlenderPhase phase)
    {
        Phase = phase;
        PhaseTime = 0f;
    }

    /// <summary>Leaves the screen (the berries are spent either way).</summary>
    public void Close()
    {
        IsActive = false;
        Phase = BlenderPhase.Done;
    }

    // ---------------------------------------------------------------- the buttons

    public void PressConfirm()
    {
        if (!IsActive || Game == null) return;
        switch (Phase)
        {
            case BlenderPhase.Ready:
                Enter(BlenderPhase.Count);
                Line = Text.Of("blender.count");
                AudioManager.PlaySound("select");
                break;
            case BlenderPhase.Play:
                var grade = Game.Press();
                AudioManager.PlaySound(grade == BlenderGrade.Miss ? "error" : "cursor");
                break;
            case BlenderPhase.Result:
                Close();
                break;
        }
    }

    // ---------------------------------------------------------------- the clock

    public void Advance(float dt)
    {
        if (!IsActive || Game == null) return;
        PhaseTime += dt;
        switch (Phase)
        {
            case BlenderPhase.Count:
                if (PhaseTime >= CountStep * 3)
                {
                    Enter(BlenderPhase.Play);
                    Line = Text.Of("blender.go");
                }
                break;
            case BlenderPhase.Play:
                carry += dt;
                while (carry >= Tick && !Game.IsOver)
                {
                    carry -= Tick;
                    Game.Advance();
                }
                if (Game.IsOver) Finish();
                break;
        }
    }

    // CalculatePokeblock on the speed the arm reached; the Pokéblock goes into the case if there is room
    private void Finish()
    {
        Result = Pokeblocks.Blend(Berries, Game!.MaxRpm, rng);
        Kept = target.Add(Result);
        Enter(BlenderPhase.Result);
        Line = Kept
            ? Text.Of("blender.made", ("block", Result.Name), ("level", Result.Level), ("feel", Result.ShownFeel))
            : Text.Of("blender.caseFull", ("block", Result.Name));
        AudioManager.PlaySound(Kept ? "stat_up" : "error");
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        if (InputManager.IsActionPressed(GameAction.Confirm)) PressConfirm();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive || Game == null) return;
        ModernUi.DrawBlender(screenWidth, screenHeight, this);
    }
}
