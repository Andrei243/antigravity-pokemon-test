using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>An evolution waiting to be played: who, into what, and whether B may stop it (only one from a level-up).</summary>
public sealed record EvolutionRequest(Pokemon Pokemon, EvolutionData Evolution, bool Cancellable);

public enum EvolutionPhase
{
    /// <summary>"What? It is evolving!"</summary>
    Notice,
    /// <summary>Light gathers and the Pokémon turns white.</summary>
    Gather,
    /// <summary>The old and the new shape take turns, faster and faster. B still stops it here.</summary>
    Morph,
    /// <summary>The flash in which it becomes the new species.</summary>
    Burst,
    /// <summary>The light drains away from the new Pokémon.</summary>
    Reveal,
    /// <summary>"Congratulations!"</summary>
    Congratulate,
    /// <summary>A message about a move learned (or not), waiting to be dismissed.</summary>
    MoveNotice,
    /// <summary>It knows four moves and wants another: which one goes?</summary>
    MoveChoice,
    /// <summary>"It stopped evolving!"</summary>
    Stopped,
    Done
}

/// <summary>
/// The evolution scene: the Pokémon gathers light, flickers between its two shapes and comes out as the new
/// species; then it learns what the new species knows at that level, asking which move to forget when it has
/// four. B stops an evolution that came from a level-up, as in the games; one from an item or a trade can't be
/// stopped.
///
/// The scene is a function of time and of three buttons (<see cref="Advance"/>, <see cref="PressConfirm"/>,
/// <see cref="PressCancel"/>, <see cref="MoveCursor"/>), so tests and the harness drive it without a keyboard. The
/// species changes at the start of <see cref="EvolutionPhase.Burst"/>, through <see cref="Evolution.Evolve"/>.
/// </summary>
public sealed class EvolutionScreen
{
    public const float NoticeTime = 1.6f, GatherTime = 1.4f, MorphTime = 3.8f, BurstTime = 0.55f, RevealTime = 1.3f;
    private const int PictureSize = 1280;
    private static readonly Color Light = new(255, 255, 255, 255);

    private Pokemon? pokemon;
    private EvolutionData? evolution;
    private EvolutionContext context = new();
    private string fromSpecies = "", toSpecies = "", oldName = "";
    private float time, phaseTime, messageAge, morphAngle, stoppedWhite;
    private readonly Queue<string> movesToLearn = new();
    private RenderTexture2D oldPicture, newPicture;
    private bool picturesLoaded;

    public EvolutionPhase Phase { get; private set; } = EvolutionPhase.Done;
    public bool IsActive => Phase != EvolutionPhase.Done;

    /// <summary>The Pokémon the scene is about.</summary>
    public Pokemon? Pokemon => pokemon;

    /// <summary>B stops it (a level-up evolution).</summary>
    public bool Cancellable { get; private set; }

    public string Message { get; private set; } = "";

    /// <summary>What the evolution did; null until it happens, and for good if it was stopped.</summary>
    public EvolutionOutcome? Outcome { get; private set; }

    /// <summary>The move being weighed in <see cref="EvolutionPhase.MoveChoice"/>.</summary>
    public string? MoveToLearn { get; private set; }

    /// <summary>In <see cref="EvolutionPhase.MoveChoice"/>: 0 to 3 is the move to forget, 4 keeps them all.</summary>
    public int Cursor { get; private set; }

    public void Begin(Pokemon pokemon, EvolutionData evolution, EvolutionContext context, bool cancellable)
    {
        this.pokemon = pokemon;
        this.evolution = evolution;
        this.context = context;
        Cancellable = cancellable;
        // The models shown: the forms it is in and goes into, where it has one (plan 03 · D11)
        fromSpecies = pokemon.ModelName;
        toSpecies = Evolution.ModelAfter(pokemon, evolution);
        oldName = pokemon.DisplayName;
        Outcome = null;
        MoveToLearn = null;
        movesToLearn.Clear();
        time = 0f;
        morphAngle = MathF.PI / 2f;
        Go(EvolutionPhase.Notice, $"What? {oldName} is evolving!");
        AudioManager.PlayCry(pokemon);
    }

    private void Go(EvolutionPhase phase, string? message = null)
    {
        Phase = phase;
        phaseTime = 0f;
        if (message != null)
        {
            Message = message;
            messageAge = 0f;
        }
    }

    /// <summary>Reads the buttons, then lets time pass.</summary>
    public void Update(float dt)
    {
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.Confirm)) PressConfirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) PressCancel();
        else if (InputManager.IsActionPressed(GameAction.Up)) MoveCursor(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) MoveCursor(1);
        Advance(dt);
    }

    public void Advance(float dt)
    {
        if (!IsActive) return;
        time += dt;
        phaseTime += dt;
        messageAge += dt;

        switch (Phase)
        {
            case EvolutionPhase.Notice when phaseTime >= NoticeTime:
                Go(EvolutionPhase.Gather);
                break;
            case EvolutionPhase.Gather when phaseTime >= GatherTime:
                Go(EvolutionPhase.Morph);
                break;
            case EvolutionPhase.Morph:
                // The two shapes swap slowly at first and in a blur by the end
                float u = Math.Clamp(phaseTime / MorphTime, 0f, 1f);
                morphAngle += dt * MathF.Tau * (0.9f + 5.6f * u * u);
                if (phaseTime >= MorphTime) Evolve();
                break;
            case EvolutionPhase.Burst when phaseTime >= BurstTime:
                Go(EvolutionPhase.Reveal);
                break;
            case EvolutionPhase.Reveal when phaseTime >= RevealTime:
                Go(EvolutionPhase.Congratulate, $"Congratulations! Your {oldName} evolved into {pokemon!.Species.Name}!");
                AudioManager.PlayFanfare(MusicRole.FanfarePokemon);
                break;
        }
    }

    private void Evolve()
    {
        Outcome = Evolution.Evolve(pokemon!, evolution!, context);
        foreach (var move in Outcome.NewMoves) movesToLearn.Enqueue(move);
        Go(EvolutionPhase.Burst);
        AudioManager.PlaySound("levelup");
        // What it has become cries out in its new voice
        AudioManager.PlayCry(pokemon!);
    }

    public void PressConfirm()
    {
        switch (Phase)
        {
            case EvolutionPhase.Notice:
                Go(EvolutionPhase.Gather);
                break;
            case EvolutionPhase.Congratulate:
            case EvolutionPhase.MoveNotice:
                NextMove();
                break;
            case EvolutionPhase.MoveChoice:
                ChooseMove(Cursor);
                break;
            case EvolutionPhase.Stopped:
                Go(EvolutionPhase.Done);
                break;
        }
    }

    public void PressCancel()
    {
        switch (Phase)
        {
            case EvolutionPhase.Gather or EvolutionPhase.Morph when Cancellable:
                stoppedWhite = Look().White;
                Go(EvolutionPhase.Stopped, $"Huh? {oldName} stopped evolving!");
                AudioManager.PlaySound("cancel");
                break;
            case EvolutionPhase.MoveChoice:
                ChooseMove(4);
                break;
            case EvolutionPhase.Congratulate or EvolutionPhase.MoveNotice or EvolutionPhase.Stopped:
                PressConfirm();
                break;
        }
    }

    public void MoveCursor(int dy)
    {
        if (Phase != EvolutionPhase.MoveChoice || dy == 0) return;
        Cursor = ((Cursor + dy) % 5 + 5) % 5;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The next move the new species brings: learned outright with a free place, asked about without one.</summary>
    private void NextMove()
    {
        var p = pokemon!;
        while (movesToLearn.Count > 0)
        {
            string move = movesToLearn.Dequeue();
            if (p.Knows(move)) continue;
            if (p.TryLearn(move))
            {
                Go(EvolutionPhase.MoveNotice, $"{p.DisplayName} learned {move}!");
                AudioManager.PlaySound("select");
                return;
            }
            MoveToLearn = move;
            Cursor = 0;
            Go(EvolutionPhase.MoveChoice, $"{p.DisplayName} wants to learn {move}, but it already knows four moves. Should one be forgotten?");
            return;
        }
        MoveToLearn = null;
        Go(EvolutionPhase.Done);
    }

    private void ChooseMove(int choice)
    {
        var p = pokemon!;
        string move = MoveToLearn!;
        if (choice >= 0 && choice < p.Moves.Count)
        {
            string forgotten = p.Moves[choice].Name;
            p.ReplaceMove(choice, move);
            Go(EvolutionPhase.MoveNotice, $"{p.DisplayName} forgot {forgotten} and learned {move}!");
            AudioManager.PlaySound("select");
        }
        else
        {
            Go(EvolutionPhase.MoveNotice, $"{p.DisplayName} did not learn {move}.");
            AudioManager.PlaySound("cancel");
        }
        MoveToLearn = null;
    }

    // ---------------------------------------------------------------- what it looks like

    /// <summary>How big each shape is, how white, how strong the glow and the flash over the screen (all 0 to 1).</summary>
    public readonly record struct Appearance(float OldScale, float NewScale, float White, float Glow, float Flash, float Gather, float Scatter);

    /// <summary>The scene at this moment (no GPU calls).</summary>
    public Appearance Look()
    {
        float t = phaseTime;
        switch (Phase)
        {
            case EvolutionPhase.Notice:
                return new(1f, 0f, 0f, 0.12f, 0f, 0f, 0f);
            case EvolutionPhase.Gather:
            {
                float u = Math.Clamp(t / GatherTime, 0f, 1f);
                return new(1f, 0f, UiMotion.EaseInOut(u), 0.12f + 0.55f * u, 0f, u, 0f);
            }
            case EvolutionPhase.Morph:
            {
                float u = Math.Clamp(t / MorphTime, 0f, 1f);
                float s = MathF.Sin(morphAngle);
                float oldScale = MathF.Sqrt(Math.Max(0f, s)) * (1f - 0.55f * u);
                float newScale = MathF.Sqrt(Math.Max(0f, -s)) * (0.45f + 0.55f * u);
                return new(oldScale, newScale, 1f, 0.67f + 0.33f * u, Math.Clamp((u - 0.92f) / 0.08f, 0f, 1f), 1f, 0f);
            }
            case EvolutionPhase.Burst:
                return new(0f, 1f, 1f, 1f, 1f - UiMotion.EaseIn(Math.Clamp(t / BurstTime, 0f, 1f)), 0f, 0f);
            case EvolutionPhase.Reveal:
            {
                float u = Math.Clamp(t / RevealTime, 0f, 1f);
                return new(0f, 1f, 1f - UiMotion.EaseOut(u), 1f - 0.62f * u, 0f, 0f, u);
            }
            case EvolutionPhase.Stopped:
                return new(1f, 0f, Math.Max(0f, stoppedWhite - t * 2.5f), 0.12f, 0f, 0f, 0f);
            default:
                return new(0f, 1f, 0f, 0.38f, 0f, 0f, 0f);
        }
    }

    /// <summary>Renders the Pokémon's two shapes offscreen. Call outside any other texture mode.</summary>
    public void Render(RenderContext renderContext)
    {
        if (!IsActive) return;
        renderContext.EnsureLoaded();
        if (!picturesLoaded)
        {
            oldPicture = Raylib.LoadRenderTexture(PictureSize, PictureSize);
            newPicture = Raylib.LoadRenderTexture(PictureSize, PictureSize);
            Raylib.SetTextureFilter(oldPicture.Texture, TextureFilter.Bilinear);
            Raylib.SetTextureFilter(newPicture.Texture, TextureFilter.Bilinear);
            picturesLoaded = true;
        }

        var look = Look();
        if (look.OldScale > 0.01f) RenderShape(renderContext, fromSpecies, look.White, oldPicture);
        if (look.NewScale > 0.01f) RenderShape(renderContext, toSpecies, look.White, newPicture);
    }

    private void RenderShape(RenderContext renderContext, string species, float white, RenderTexture2D target)
    {
        // Pure light has no ink line round it; the outline comes back as the colours do
        PokemonSprites.Render(renderContext, PokemonModels.Get(species), SpriteView.Front, new PokePose { Time = time }, target,
            hullOutline: white < 0.5f, flash: (Light, white));
    }

    public void Draw(int sw, int sh)
    {
        if (!IsActive) return;
        var look = Look();

        // The Pokémon stands in the middle, and steps aside for the list of moves
        bool choosing = Phase == EvolutionPhase.MoveChoice;
        float aside = choosing ? UiMotion.EaseOut(Math.Clamp(phaseTime / 0.3f, 0f, 1f)) : 0f;
        var c = new Vector2(sw / 2f - 400f * aside, 420f);

        ModernUi.EvolutionStage(sw, sh, c, time, look.Glow);
        ModernUi.EvolutionMotes(c, time, look.Gather, 0f);
        if (picturesLoaded)
        {
            const float size = 720f;
            ModernUi.EvolutionFigure(oldPicture.Texture, c, size * look.OldScale);
            ModernUi.EvolutionFigure(newPicture.Texture, c, size * look.NewScale);
            ModernUi.EvolutionAura(c, size * Math.Max(look.OldScale, look.NewScale), look.White);
        }
        ModernUi.EvolutionMotes(c, time, 0f, look.Scatter);
        if (look.Flash > 0.004f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(255, 255, 255, (int)(255 * look.Flash)));

        if (choosing && MoveToLearn != null)
            ModernUi.MoveChoice(new Rectangle(sw - 64 - 820, 64, 820, 716), pokemon!, MoveToLearn, Cursor, Math.Clamp(phaseTime / 0.3f, 0f, 1f));

        if (Message.Length > 0 && Phase is not (EvolutionPhase.Burst or EvolutionPhase.Reveal))
        {
            int shown = Math.Min(Message.Length, (int)(messageAge * 70f));
            bool waits = Phase is EvolutionPhase.Congratulate or EvolutionPhase.MoveNotice or EvolutionPhase.Stopped;
            ModernUi.DrawDialogue(sw, sh, "", Message.Substring(0, shown), waits && shown == Message.Length);
        }

        if (Cancellable && Phase is EvolutionPhase.Gather or EvolutionPhase.Morph) ModernUi.Hints(sw - 64, 44, ("X", "Stop"));
    }
}
