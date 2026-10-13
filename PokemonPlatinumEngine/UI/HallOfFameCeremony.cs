using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

public enum CeremonyPhase
{
    /// <summary>A Pokémon of the team comes onto the stage, and its card beside it.</summary>
    Enter,
    /// <summary>It stands there while its card fills in, part by part.</summary>
    Show,
    /// <summary>It and its card leave the way they came.</summary>
    Leave,
    /// <summary>The Champion in the middle, the team to either side, the slips of paper falling: waits for the button.</summary>
    Champion,
    /// <summary>The picture closes to black.</summary>
    Out,
    Done
}

/// <summary>What a Pokémon's card says of it in the ceremony: who it is, who first trained it and where it came from.</summary>
public sealed record CeremonyCard(string Name, Gender Gender, int Level, string Species, IReadOnlyList<PokemonType> Types, string Trainer, string TrainerId, string Met);

/// <summary>
/// The scene of a team's entry into the Hall of Fame (plan 02 · S14; style guide, "The Hall of Fame's ceremony"), on
/// the beats of the original's (<c>src/cutscenes/hall_of_fame.c</c>): each Pokémon of the team is shown in turn with
/// who it is, who first trained it and where it was met; then the Champion stands among them all, with their name,
/// their ID number and the time played, and paper falls until the button is pressed. Eggs are left out, as the
/// record leaves them out. Our own staging.
///
/// Like <see cref="EvolutionScreen"/> and <see cref="HatchScreen"/> it is a function of time and one button
/// (<see cref="Advance"/>, <see cref="PressConfirm"/>), so tests and the harness play it without a keyboard.
/// </summary>
public sealed class HallOfFameCeremony
{
    public const float EnterTime = 0.6f, ShowTime = 3.4f, LeaveTime = 0.45f;
    /// <summary>The card's three parts (the welcome and the name, the species and its types, the trainer and the place) come this far apart.</summary>
    public const float PartStep = 0.4f;
    public const int CardParts = 3;
    public const float FigureIn = 0.7f, TitleAt = 0.7f, TitleIn = 0.5f, TeamAt = 1.2f, TeamStep = 0.18f, TeamIn = 0.5f;
    public const float ConfettiAt = 2.6f, ArrowAt = 3f, OutTime = 1f;
    private const int PictureSize = 1024, FigureWidth = 720, FigureHeight = 1024;

    private List<Pokemon> team = new();
    private string player = "", character = "";
    private int trainerId;
    private float playSeconds, time, phaseTime, paperSince = -1f;
    private readonly RenderTexture2D[] pictures = new RenderTexture2D[Party.MaxSize];
    private readonly bool[] rendered = new bool[Party.MaxSize];
    private RenderTexture2D figure;
    private bool picturesLoaded, figureRendered;

    public CeremonyPhase Phase { get; private set; } = CeremonyPhase.Done;
    public bool IsActive => Phase != CeremonyPhase.Done;

    /// <summary>The team being entered, Eggs left out, and which of them is on the stage (while one is).</summary>
    public IReadOnlyList<Pokemon> Team => team;
    public int Member { get; private set; }

    public string Champion => player;

    /// <summary>The Champion's line under the picture: their ID number and the time played.</summary>
    public string ChampionId => trainerId.ToString("D5");
    public string TimePlayed => TitleScreen.FormatPlayTime(playSeconds);

    /// <summary>
    /// Begins the ceremony for a team and its Champion: the name, the card's ID number, the time played so far and
    /// the character the player is drawn as (<see cref="PlayerIdentity.Character"/>).
    /// </summary>
    public void Begin(IEnumerable<Pokemon> entered, string champion, int id, float secondsPlayed, string drawnAs)
    {
        team = entered.Where(p => !p.IsEgg).Take(Party.MaxSize).ToList();
        (player, trainerId, playSeconds, character) = (champion, id, secondsPlayed, drawnAs);
        time = 0f;
        paperSince = -1f;
        Member = 0;
        Array.Clear(rendered);
        figureRendered = false;
        Go(team.Count > 0 ? CeremonyPhase.Enter : CeremonyPhase.Champion);
        // A song of the Hall of Fame's own is plan 05 · A6's: the Champion's victory theme stands for it
        AudioManager.PlayMusic(MusicRole.VictoryChampion);
    }

    private void Go(CeremonyPhase phase)
    {
        Phase = phase;
        phaseTime = 0f;
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
        if (!IsActive) return;
        time += dt;
        phaseTime += dt;
        switch (Phase)
        {
            case CeremonyPhase.Enter:
                if (phaseTime >= EnterTime)
                {
                    // It cries as it comes to rest
                    AudioManager.PlayCry(team[Member]);
                    Go(CeremonyPhase.Show);
                }
                break;
            case CeremonyPhase.Show:
                if (phaseTime >= ShowTime) Go(CeremonyPhase.Leave);
                break;
            case CeremonyPhase.Leave:
                if (phaseTime < LeaveTime) break;
                if (Member + 1 < team.Count)
                {
                    Member++;
                    Go(CeremonyPhase.Enter);
                }
                else Go(CeremonyPhase.Champion);
                break;
            case CeremonyPhase.Champion:
                if (paperSince < 0f && phaseTime >= ConfettiAt) paperSince = time;
                break;
            case CeremonyPhase.Out:
                if (phaseTime >= OutTime)
                {
                    AudioManager.StopMusic();
                    Go(CeremonyPhase.Done);
                }
                break;
        }
    }

    /// <summary>How many of the card's parts show now.</summary>
    public int PartsShown => Phase switch
    {
        CeremonyPhase.Show => Math.Min(CardParts, 1 + (int)(phaseTime / PartStep)),
        CeremonyPhase.Leave => CardParts,
        _ => 0
    };

    /// <summary>
    /// The button: a card that is whole gives way to the next Pokémon, and the last picture, once it has had a moment,
    /// closes.
    /// </summary>
    public void PressConfirm()
    {
        if (Phase == CeremonyPhase.Show && PartsShown == CardParts && phaseTime >= (CardParts - 1) * PartStep + 0.3f) Go(CeremonyPhase.Leave);
        else if (Phase == CeremonyPhase.Champion && phaseTime >= ArrowAt)
        {
            AudioManager.PlaySound("select");
            Go(CeremonyPhase.Out);
        }
    }

    /// <summary>
    /// The picture at this moment: how far the Pokémon and its card have come in (0 off the picture, 1 in place), how
    /// far the Champion has stepped up, how strongly the title shows, how long paper has been falling (less than
    /// nought before it does), whether the arrow shows, and how far the picture has closed to black.
    /// </summary>
    public readonly record struct Look(float Slide, float Figure, float Title, float Confetti, bool Arrow, float Black);

    /// <summary>The scene now (no GPU calls).</summary>
    public Look Appearance()
    {
        float t = phaseTime;
        switch (Phase)
        {
            case CeremonyPhase.Enter:
                return new Look(UiMotionEase(t / EnterTime), 0f, 0f, -1f, false, 0f);
            case CeremonyPhase.Show:
                return new Look(1f, 0f, 0f, -1f, false, 0f);
            case CeremonyPhase.Leave:
                return new Look(1f - Math.Clamp(t / LeaveTime, 0f, 1f), 0f, 0f, -1f, false, 0f);
            case CeremonyPhase.Champion:
                return new Look(0f, UiMotionEase(t / FigureIn), Math.Clamp((t - TitleAt) / TitleIn, 0f, 1f), Paper, t >= ArrowAt, 0f);
            case CeremonyPhase.Out:
                return new Look(0f, 1f, 1f, Paper, false, Math.Clamp(t / OutTime, 0f, 1f));
            default:
                return new Look(0f, 0f, 0f, -1f, false, 1f);
        }
    }

    private float Paper => paperSince < 0f ? -1f : time - paperSince;

    private static float UiMotionEase(float t) => Kit.UiMotion.EaseOut(Math.Clamp(t, 0f, 1f));

    /// <summary>How far the team's Pokémon at a place has come in beside the Champion (0 off the picture, 1 in place).</summary>
    public float TeamSlide(int place) => Phase switch
    {
        CeremonyPhase.Champion => UiMotionEase((phaseTime - TeamAt - place * TeamStep) / TeamIn),
        CeremonyPhase.Out => 1f,
        _ => 0f
    };

    /// <summary>
    /// Where the team's Pokémon at a place stands beside the Champion, in steps from the middle: the first to the
    /// right, the second to the left, and so outward (the original's order).
    /// </summary>
    public static int SideOf(int place) => (place / 2 + 1) * (place % 2 == 0 ? 1 : -1);

    /// <summary>The card of a Pokémon of the team: its own trainer, or the Champion where it has none of its own.</summary>
    public CeremonyCard CardOf(Pokemon pokemon)
    {
        var (name, id) = pokemon.OriginalTrainer is { } mark ? (mark.Name, mark.Id) : (player, trainerId);
        string met = pokemon.MetLocation is not { Length: > 0 } place ? Text.Of("ceremony.metAlong")
            : pokemon.MetLevel == 0 ? Text.Of("ceremony.hatchedAt", ("place", place))
            : Text.Of("ceremony.metAt", ("place", place), ("level", pokemon.MetLevel));
        var types = new List<PokemonType> { pokemon.PrimaryType };
        if (pokemon.SecondaryType is { } second) types.Add(second);
        return new CeremonyCard(pokemon.DisplayName, pokemon.Gender, pokemon.Level, pokemon.Species.Name, types, name, id.ToString("D5"), met);
    }

    /// <summary>Renders whoever stands on the stage. Call outside any other texture mode.</summary>
    public void Render(RenderContext renderContext)
    {
        if (!IsActive) return;
        renderContext.EnsureLoaded();
        if (!picturesLoaded)
        {
            for (int i = 0; i < pictures.Length; i++)
            {
                pictures[i] = Raylib.LoadRenderTexture(PictureSize, PictureSize);
                Raylib.SetTextureFilter(pictures[i].Texture, TextureFilter.Bilinear);
            }
            figure = Raylib.LoadRenderTexture(FigureWidth, FigureHeight);
            Raylib.SetTextureFilter(figure.Texture, TextureFilter.Bilinear);
            picturesLoaded = true;
        }

        bool together = Phase is CeremonyPhase.Champion or CeremonyPhase.Out;
        for (int i = 0; i < team.Count; i++)
        {
            if (!(together ? TeamSlide(i) > 0.01f : i == Member)) continue;
            // The models were asked for and waited on behind the fade that opened the scene
            if (!PokemonModels.TryGet(team[i].ModelName, out var model))
            {
                PokemonModels.Request(team[i].ModelName);
                continue;
            }
            PokemonSprites.Render(renderContext, model, SpriteView.Front, new PokePose { Time = time + i * 0.37f }, pictures[i]);
            rendered[i] = true;
        }
        if (together)
        {
            var pose = new CharacterPose { Time = time, Blink = (time + 0.9f) % 3.4f < 0.12f };
            CharacterStudio.Render(renderContext, character, figure, pose);
            figureRendered = true;
        }
    }

    public void Draw(int sw, int sh)
    {
        if (!IsActive) return;
        ModernUi.DrawCeremony(sw, sh, this, Appearance(), time,
            i => picturesLoaded && rendered[i] ? pictures[i].Texture : null,
            picturesLoaded && figureRendered ? figure.Texture : null);
    }
}
