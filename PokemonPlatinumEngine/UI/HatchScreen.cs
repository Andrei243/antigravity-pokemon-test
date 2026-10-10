using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

public enum HatchPhase
{
    /// <summary>The Egg rocks on the stage, more and more often.</summary>
    Wobble,
    /// <summary>Cracks run over it and it shakes hard.</summary>
    Crack,
    /// <summary>The flash in which the Egg becomes the Pokémon (<see cref="Breeding.Hatch"/>).</summary>
    Burst,
    /// <summary>The light drains from the Pokémon, which cries.</summary>
    Reveal,
    /// <summary>"X hatched from the Egg!", waiting to be dismissed.</summary>
    Announce,
    Done
}

/// <summary>
/// The scene of an Egg hatching (plan 06 · R15; the original's <c>egg_hatch.c</c>, played by <c>HatchEgg</c> once the
/// field finds an Egg with no cycles left): the Egg rocks on the evolution scene's stage, cracks and bursts in a flash,
/// and the Pokémon is there, crying. It becomes the Pokémon as the flash begins. Our own staging, in the interface
/// kit's style (style guide, "Hatching").
///
/// Like <see cref="EvolutionScreen"/> it is a function of time and one button (<see cref="Advance"/>,
/// <see cref="PressConfirm"/>), so tests and the harness drive it without a keyboard.
/// </summary>
public sealed class HatchScreen
{
    public const float WobbleTime = 3.2f, CrackTime = 1.5f, BurstTime = 0.55f, RevealTime = 1.2f;
    private const int PictureSize = 1280;
    private static readonly Color Light = new(255, 255, 255, 255);

    private Pokemon? pokemon;
    private string place = "";
    private DateTime day;
    private float time, phaseTime, messageAge;
    private int wobblesHeard;
    private RenderTexture2D picture;
    private bool pictureLoaded;

    public HatchPhase Phase { get; private set; } = HatchPhase.Done;
    public bool IsActive => Phase != HatchPhase.Done;

    /// <summary>The Egg the scene is about, and the Pokémon it becomes.</summary>
    public Pokemon? Pokemon => pokemon;

    public string Message { get; private set; } = "";

    /// <summary>The Egg has hatched (from the flash on).</summary>
    public bool Hatched => pokemon is { IsEgg: false };

    /// <summary>Begins the scene for an Egg that will be met (hatched) at this place on this day.</summary>
    public void Begin(Pokemon egg, string place, DateTime day)
    {
        pokemon = egg;
        this.place = place;
        this.day = day;
        time = 0f;
        wobblesHeard = 0;
        Message = "";
        Go(HatchPhase.Wobble);
        AudioManager.PlayMusic(MusicRole.Evolution);
    }

    private void Go(HatchPhase phase, string? message = null)
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
        if (!IsActive || pokemon == null) return;
        time += dt;
        phaseTime += dt;
        messageAge += dt;
        switch (Phase)
        {
            case HatchPhase.Wobble:
                // Each rock of the Egg is heard as it comes back to rest
                int rocks = Rocks(phaseTime);
                while (wobblesHeard < rocks)
                {
                    wobblesHeard++;
                    AudioManager.PlaySound("ball_shake");
                }
                if (phaseTime >= WobbleTime) Go(HatchPhase.Crack);
                break;
            case HatchPhase.Crack:
                if (phaseTime >= CrackTime)
                {
                    Breeding.Hatch(pokemon, place, day);
                    AudioManager.PlaySound("ball_break");
                    Go(HatchPhase.Burst);
                }
                break;
            case HatchPhase.Burst:
                if (phaseTime >= BurstTime)
                {
                    AudioManager.PlayCry(pokemon);
                    Go(HatchPhase.Reveal);
                }
                break;
            case HatchPhase.Reveal:
                if (phaseTime >= RevealTime)
                {
                    AudioManager.PlayFanfare(MusicRole.FanfarePokemon);
                    Go(HatchPhase.Announce, $"{pokemon.Species.Name} hatched from the Egg!");
                }
                break;
        }
    }

    /// <summary>The Egg's rocks so far: three, closer and closer together, over the wobbling.</summary>
    private static int Rocks(float t) => t < 0.9f ? 0 : t < 1.9f ? 1 : t < 2.6f ? 2 : 3;

    public void PressConfirm()
    {
        if (Phase == HatchPhase.Announce && messageAge * 70f >= Message.Length) Go(HatchPhase.Done);
    }

    /// <summary>How the Egg leans (radians), how far it has cracked, its whiteness, the Pokémon's size and whiteness, and the flash.</summary>
    public readonly record struct Appearance(float EggScale, float Lean, float Cracks, float EggWhite, float NewScale, float White, float Flash, float Glow);

    /// <summary>The scene at this moment (no GPU calls).</summary>
    public Appearance Look()
    {
        float t = phaseTime;
        switch (Phase)
        {
            case HatchPhase.Wobble:
            {
                // A rock is a quick swing either way that dies away, each one stronger than the last
                float lean = 0f;
                foreach (var (at, strength) in new[] { (0.4f, 0.10f), (1.4f, 0.16f), (2.2f, 0.22f) })
                {
                    float u = t - at;
                    if (u is > 0f and < 0.6f) lean += strength * MathF.Sin(u * MathF.PI * 4f) * (1f - u / 0.6f);
                }
                return new(1f, lean, 0f, 0f, 0f, 0f, 0f, 0.12f);
            }
            case HatchPhase.Crack:
            {
                float u = Math.Clamp(t / CrackTime, 0f, 1f);
                float shake = 0.06f + 0.10f * u;
                float lean = shake * MathF.Sin(t * MathF.PI * 14f);
                return new(1f + 0.04f * u, lean, u, Math.Clamp((u - 0.7f) / 0.3f, 0f, 1f), 0f, 1f, Math.Clamp((u - 0.85f) / 0.15f, 0f, 1f), 0.12f + 0.5f * u);
            }
            case HatchPhase.Burst:
                return new(0f, 0f, 1f, 1f, 1f, 1f, 1f - Math.Clamp(t / BurstTime, 0f, 1f), 0.8f);
            case HatchPhase.Reveal:
            {
                float u = Math.Clamp(t / RevealTime, 0f, 1f);
                return new(0f, 0f, 1f, 1f, 1f, 1f - u * u * (3f - 2f * u), 0f, 0.8f - 0.42f * u);
            }
            default:
                return new(0f, 0f, 1f, 1f, 1f, 0f, 0f, 0.38f);
        }
    }

    /// <summary>Renders the hatched Pokémon offscreen once there is one. Call outside any other texture mode.</summary>
    public void Render(RenderContext renderContext)
    {
        if (!IsActive || pokemon == null) return;
        var look = Look();
        if (look.NewScale <= 0.01f || pokemon.IsEgg) return;
        renderContext.EnsureLoaded();
        if (!pictureLoaded)
        {
            picture = Raylib.LoadRenderTexture(PictureSize, PictureSize);
            Raylib.SetTextureFilter(picture.Texture, TextureFilter.Bilinear);
            pictureLoaded = true;
        }
        // Pure light has no ink line round it; the outline comes back as the colours do
        PokemonSprites.Render(renderContext, PokemonModels.Get(pokemon.ModelName), SpriteView.Front, new PokePose { Time = time }, picture,
            hullOutline: look.White < 0.5f, flash: (Light, look.White));
    }

    public void Draw(int sw, int sh)
    {
        if (!IsActive) return;
        var look = Look();
        var c = new Vector2(sw / 2f, 420f);

        ModernUi.EvolutionStage(sw, sh, c, time, look.Glow);
        if (look.EggScale > 0.01f) ModernUi.HatchEgg(c + new Vector2(0, 250f), 400f * look.EggScale, look.Lean, look.Cracks, look.EggWhite);
        if (pictureLoaded && look.NewScale > 0.01f && Hatched)
        {
            const float size = 720f;
            ModernUi.EvolutionFigure(picture.Texture, c, size * look.NewScale);
            ModernUi.EvolutionAura(c, size * look.NewScale, look.White);
        }
        if (look.Flash > 0.004f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(255, 255, 255, (int)(255 * look.Flash)));

        if (Message.Length > 0 && Phase == HatchPhase.Announce)
        {
            int shown = Math.Min(Message.Length, (int)(messageAge * 70f));
            ModernUi.DrawDialogue(sw, sh, "", Message.Substring(0, shown), shown == Message.Length);
        }
    }
}
