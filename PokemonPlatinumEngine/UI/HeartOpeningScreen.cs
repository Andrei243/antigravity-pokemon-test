using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

public enum HeartOpeningPhase
{
    /// <summary>The Pokémon stands in its grey haze, which thickens and pulls in.</summary>
    Haze,
    /// <summary>The haze lifts away from it in a soft light.</summary>
    Lift,
    /// <summary>It is itself again, and cries.</summary>
    Reveal,
    /// <summary>"X's heart opened!", waiting to be read.</summary>
    Announce,
    /// <summary>The level panel for the EXP its lock held, two presses as the bag's Rare Candy's.</summary>
    Levels,
    Done
}

/// <summary>
/// A locked heart opening (plan 44 · SD2; style guide, "A locked heart", "The opening"): the Pokémon on the evolution
/// scene's stage in its grey haze, the haze lifting, its cry, the line that says so, and the level panel for the EXP
/// its lock held, if that EXP brought a level. Like <see cref="HatchScreen"/> it is a function of time and one button
/// (<see cref="Advance"/>, <see cref="PressConfirm"/>), so tests and the harness drive it without a keyboard; the opening
/// itself (<see cref="HeartLock.Open"/>) is done before it begins.
/// </summary>
public sealed class HeartOpeningScreen
{
    public const float HazeTime = 1.6f, LiftTime = 1.4f, RevealTime = 0.9f;
    private const int PictureSize = 1280;
    private static readonly Color Grey = new(118, 116, 128, 255);

    private HeartOpening? opening;
    private float time, phaseTime, messageAge;
    private RenderTexture2D picture;
    private bool pictureLoaded;

    public HeartOpeningPhase Phase { get; private set; } = HeartOpeningPhase.Done;
    public bool IsActive => Phase != HeartOpeningPhase.Done;

    /// <summary>The opening shown.</summary>
    public HeartOpening? Opening => opening;

    /// <summary>The level panel while it is up (the held EXP brought a level).</summary>
    public UI.Kit.LevelUpPanel? Panel { get; private set; }

    public string Message { get; private set; } = "";

    public void Begin(HeartOpening shown)
    {
        opening = shown;
        time = 0f;
        Message = "";
        Panel = null;
        Go(HeartOpeningPhase.Haze);
        AudioManager.PlayMusic(MusicRole.Evolution);
    }

    private void Go(HeartOpeningPhase phase, string? message = null)
    {
        Phase = phase;
        phaseTime = 0f;
        if (message != null)
        {
            Message = message;
            messageAge = 0f;
        }
    }

    /// <summary>Reads the button, then lets time pass.</summary>
    public void Update(float dt)
    {
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel)) PressConfirm();
        Advance(dt);
    }

    public void Advance(float dt)
    {
        if (!IsActive || opening == null) return;
        time += dt;
        phaseTime += dt;
        messageAge += dt;
        Panel?.Advance(dt);
        switch (Phase)
        {
            case HeartOpeningPhase.Haze:
                if (phaseTime >= HazeTime)
                {
                    AudioManager.PlaySound("heal");
                    Go(HeartOpeningPhase.Lift);
                }
                break;
            case HeartOpeningPhase.Lift:
                if (phaseTime >= LiftTime)
                {
                    AudioManager.PlayCry(opening.Pokemon);
                    Go(HeartOpeningPhase.Reveal);
                }
                break;
            case HeartOpeningPhase.Reveal:
                if (phaseTime >= RevealTime)
                {
                    AudioManager.PlayFanfare(MusicRole.FanfarePokemon);
                    Go(HeartOpeningPhase.Announce, Text.Of("hearts.openedScene", ("pokemon", opening.Pokemon.DisplayName)));
                }
                break;
        }
    }

    /// <summary>
    /// The button: the announcement read on (to the level panel when the held EXP brought a level, else the end), then
    /// the panel's two pages, as the bag's Rare Candy's.
    /// </summary>
    public void PressConfirm()
    {
        if (opening == null) return;
        if (Phase == HeartOpeningPhase.Announce && messageAge * 70f >= Message.Length)
        {
            if (opening.Levelled)
            {
                Panel = new UI.Kit.LevelUpPanel(opening.Pokemon, opening.LevelAfter, opening.Before, opening.After);
                Go(HeartOpeningPhase.Levels, Text.Of("hearts.openedLevel", ("pokemon", opening.Pokemon.DisplayName), ("level", opening.LevelAfter)));
            }
            else Go(HeartOpeningPhase.Done);
            return;
        }
        if (Phase == HeartOpeningPhase.Levels && Panel != null && Panel.Press()) Go(HeartOpeningPhase.Done);
    }

    /// <summary>How thick the haze is (1 locked, 0 lifted), the light round the Pokémon and the stage's glow, at this moment (no GPU calls).</summary>
    public readonly record struct Appearance(float Haze, float Light, float Glow);

    public Appearance Look()
    {
        float t = phaseTime;
        return Phase switch
        {
            HeartOpeningPhase.Haze => new(1f, 0f, 0.12f + 0.1f * Math.Clamp(t / HazeTime, 0f, 1f)),
            HeartOpeningPhase.Lift => new(1f - Smooth(t / LiftTime), 0.7f * MathF.Sin(Math.Clamp(t / LiftTime, 0f, 1f) * MathF.PI), 0.22f + 0.4f * Smooth(t / LiftTime)),
            HeartOpeningPhase.Reveal => new(0f, 0f, 0.62f - 0.24f * Smooth(t / RevealTime)),
            _ => new(0f, 0f, 0.38f)
        };
    }

    private static float Smooth(float u)
    {
        u = Math.Clamp(u, 0f, 1f);
        return u * u * (3f - 2f * u);
    }

    /// <summary>Renders the Pokémon offscreen. Call outside any other texture mode.</summary>
    public void Render(RenderContext renderContext)
    {
        if (!IsActive || opening == null) return;
        renderContext.EnsureLoaded();
        if (!pictureLoaded)
        {
            picture = Gpu.Target(PictureSize, PictureSize, "hearts");
            Raylib.SetTextureFilter(picture.Texture, TextureFilter.Bilinear);
            pictureLoaded = true;
        }
        var p = opening.Pokemon;
        var look = Look();
        PokemonSprites.Render(renderContext, PokemonModels.Get(PokemonLook.Of(p.ModelName, p.Gender == Data.Gender.Female, p.IsShiny)), SpriteView.Front,
            new PokePose { Time = time }, picture, flash: (Grey, look.Haze * 0.45f));
    }

    public void Draw(int sw, int sh)
    {
        if (!IsActive) return;
        var look = Look();
        var c = new Vector2(sw / 2f, 420f);
        ModernUi.EvolutionStage(sw, sh, c, time, look.Glow);
        if (pictureLoaded)
        {
            const float size = 720f;
            ModernUi.EvolutionFigure(picture.Texture, c, size);
            ModernUi.HeartHaze(c, size, look.Haze, time);
            if (look.Light > 0.01f) ModernUi.EvolutionAura(c, size, look.Light);
        }
        if (Message.Length > 0 && Phase is HeartOpeningPhase.Announce or HeartOpeningPhase.Levels)
        {
            int shown = Math.Min(Message.Length, (int)(messageAge * 70f));
            ModernUi.DrawDialogue(sw, sh, "", Message.Substring(0, shown), shown == Message.Length);
        }
        if (Phase == HeartOpeningPhase.Levels && Panel != null) ModernUi.LevelUp(sw - 60f, sh - 260f, Panel);
    }
}
