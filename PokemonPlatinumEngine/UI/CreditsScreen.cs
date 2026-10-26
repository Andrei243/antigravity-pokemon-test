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

public enum CreditLineKind { Heading, Name, Note }

/// <summary>A line of the credits' roll: what it is, what it says, and how far down the roll its top stands.</summary>
public readonly record struct CreditLine(CreditLineKind Kind, string Text, float Y);

/// <summary>
/// The credits laid out as one column (style guide, "The credits"): every section's heading, its names and the notes
/// under them, each at its place down the roll. No drawing: a note is wrapped by whoever can measure text.
/// </summary>
public static class CreditsRoll
{
    public const float ColumnWidth = 860f;
    public const float HeadingSize = 28f, NameSize = 38f, NoteSize = 24f;
    public const float HeadingPitch = 58f, NamePitch = 52f, NotePitch = 34f, EntryGap = 14f, SectionGap = 110f;

    /// <summary>Wraps a note to about this many characters where nobody measures it (a test).</summary>
    public const int RoughColumn = 66;

    /// <summary>
    /// The roll for these sections: <paramref name="player"/> for <c>{player}</c>, and <paramref name="team"/> listed
    /// in the section that asks for it, each Pokémon a name and a note. Returns the lines and the roll's whole height.
    /// </summary>
    public static (List<CreditLine> Lines, float Height) Lay(IReadOnlyList<CreditSection> sections, string player,
        IReadOnlyList<(string Name, string Note)> team, Func<string, IReadOnlyList<string>>? wrap = null)
    {
        wrap ??= RoughWrap;
        var lines = new List<CreditLine>();
        float y = 0f;
        foreach (var section in sections)
        {
            if (lines.Count > 0) y += SectionGap;
            lines.Add(new CreditLine(CreditLineKind.Heading, section.Heading.ToUpperInvariant(), y));
            y += HeadingPitch;
            var entries = section.Entries.Select(e => (Name: e.Name.Replace("{player}", player), e.Note));
            if (section.Team) entries = entries.Concat(team.Select(m => (m.Name, (string?)m.Note)));
            foreach (var (name, note) in entries)
            {
                lines.Add(new CreditLine(CreditLineKind.Name, name, y));
                y += NamePitch;
                if (string.IsNullOrEmpty(note))
                {
                    y += EntryGap;
                    continue;
                }
                foreach (string part in wrap(note))
                {
                    // A middle dot parts two things on a line; where the line breaks between them the break does
                    string shown = part.Trim();
                    if (shown.StartsWith("· ", StringComparison.Ordinal)) shown = shown[2..];
                    if (shown.EndsWith(" ·", StringComparison.Ordinal)) shown = shown[..^2];
                    lines.Add(new CreditLine(CreditLineKind.Note, shown, y));
                    y += NotePitch;
                }
                y += EntryGap + 6f;
            }
        }
        return (lines, y);
    }

    private static IReadOnlyList<string> RoughWrap(string text)
    {
        var lines = new List<string>();
        string line = "";
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > RoughColumn)
            {
                lines.Add(line);
                line = word;
            }
            else line = line.Length == 0 ? word : line + " " + word;
        }
        if (line.Length > 0) lines.Add(line);
        return lines;
    }

    /// <summary>
    /// How a Pokémon of the Champion's team is listed: its name, and its species and level under it (its level alone
    /// where it goes by its species' name).
    /// </summary>
    public static (string Name, string Note) Listing(Pokemon pokemon) =>
        (pokemon.DisplayName, string.IsNullOrWhiteSpace(pokemon.Nickname) ? Text.Of("creditsRoll.level", ("level", pokemon.Level)) : Text.Of("creditsRoll.speciesLevel", ("species", pokemon.Species.DisplayName), ("level", pokemon.Level)));
}

public enum CreditsPhase
{
    /// <summary>The title's own lettering, alone.</summary>
    Title,
    /// <summary>The roll climbs the picture, the team standing beside it in turn.</summary>
    Roll,
    /// <summary>"THE END", waiting for the button.</summary>
    End,
    /// <summary>The picture closes to black.</summary>
    Out,
    Done
}

/// <summary>
/// The credits (plan 02 · S14; style guide, "The credits"): this remake's own, played once the Hall of Fame has
/// recorded the team and the game has been saved. A title card, the roll of <c>credits.json</c> with the Champion's
/// team standing beside it one after another, and a last card that waits for the button.
///
/// Like <see cref="EvolutionScreen"/> and <see cref="HatchScreen"/> it is a function of time and one button
/// (<see cref="Advance"/>, <see cref="PressConfirm"/>, and <see cref="Held"/> for the button kept down, which hurries
/// the roll), so tests and the harness play it without a keyboard.
/// </summary>
public sealed class CreditsScreen
{
    public const float TitleTime = 5f, TitleIn = 1.2f, TitleOut = 0.8f;
    public const float Pace = 64f, Hurry = 4f, FadeBand = 150f;
    public const float MemberFade = 0.8f;
    public const float EndIn = 1.2f, EndReady = 1.5f, EndWait = 30f, OutTime = 1f;
    public const int ViewHeight = GameEngine.VirtualHeight;
    private const int PictureSize = 1024;

    private List<CreditLine> lines = new();
    private IReadOnlyList<Pokemon> team = Array.Empty<Pokemon>();
    private float time, phaseTime;
    private RenderTexture2D picture;
    private bool pictureLoaded;
    private int pictured = -1;

    public CreditsPhase Phase { get; private set; } = CreditsPhase.Done;
    public bool IsActive => Phase != CreditsPhase.Done;

    /// <summary>The roll's lines, each at its place down it.</summary>
    public IReadOnlyList<CreditLine> Lines => lines;

    /// <summary>The roll's height, and how far it has climbed: a line shows at <see cref="ViewHeight"/> less this, plus its own place.</summary>
    public float Height { get; private set; }
    public float Scroll { get; private set; }

    /// <summary>How far the roll climbs before the last card: until its last line has faded at the top of the picture.</summary>
    public float Length => Height + ViewHeight - FadeBand;

    /// <summary>The button kept down by a tool (the game reads the key itself): the title card and the roll go by four times as fast.</summary>
    public bool Held { get; set; }
    private bool keyHeld;

    /// <summary>The Champion's team, which stands beside the roll in turn.</summary>
    public IReadOnlyList<Pokemon> Team => team;

    /// <summary>Begins the credits for a Champion and their team (Eggs are left out, as the Hall of Fame leaves them out).</summary>
    public void Begin(string player, IReadOnlyList<Pokemon> champions, Func<string, IReadOnlyList<string>>? wrap = null)
    {
        team = champions.Where(p => !p.IsEgg).ToList();
        (lines, Height) = CreditsRoll.Lay(CreditsDatabase.Sections, player, team.Select(CreditsRoll.Listing).ToList(), wrap);
        Scroll = 0f;
        time = 0f;
        Held = keyHeld = false;
        pictured = -1;
        Go(CreditsPhase.Title);
        // The region's own credits song where it has one (plan 19 · K8: kanto/credits); the title's theme stands for the rest
        AudioManager.PlaySceneMusic("credits", MusicRole.Title);
    }

    private void Go(CreditsPhase phase)
    {
        Phase = phase;
        phaseTime = 0f;
    }

    /// <summary>Reads the button, then lets time pass.</summary>
    public void Update(float dt)
    {
        if (!IsActive) return;
        keyHeld = InputManager.IsActionDown(GameAction.Confirm);
        if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel)) PressConfirm();
        Advance(dt);
    }

    public void Advance(float dt)
    {
        if (!IsActive) return;
        time += dt;
        float pace = (Held || keyHeld) && Phase is CreditsPhase.Title or CreditsPhase.Roll ? Hurry : 1f;
        phaseTime += dt * pace;
        switch (Phase)
        {
            case CreditsPhase.Title:
                if (phaseTime >= TitleTime) Go(CreditsPhase.Roll);
                break;
            case CreditsPhase.Roll:
                Scroll = Math.Min(Length, Scroll + Pace * pace * dt);
                if (Scroll >= Length) Go(CreditsPhase.End);
                break;
            case CreditsPhase.End:
                if (phaseTime >= EndWait) Go(CreditsPhase.Out);
                break;
            case CreditsPhase.Out:
                if (phaseTime >= OutTime)
                {
                    AudioManager.StopMusic();
                    Go(CreditsPhase.Done);
                }
                break;
        }
    }

    /// <summary>The button: on the last card, once it has had a moment, the picture closes.</summary>
    public void PressConfirm()
    {
        if (Phase == CreditsPhase.End && phaseTime >= EndReady)
        {
            AudioManager.PlaySound("select");
            Go(CreditsPhase.Out);
        }
    }

    /// <summary>
    /// The picture at this moment: how strongly the title card and the last card show, which of the team stands beside
    /// the roll and how strongly (-1 for nobody), whether the last card's arrow shows, and how far the picture has
    /// closed to black.
    /// </summary>
    public readonly record struct Look(float Title, int Member, float MemberAlpha, float End, bool Arrow, float Black);

    /// <summary>The scene now (no GPU calls).</summary>
    public Look Appearance()
    {
        switch (Phase)
        {
            case CreditsPhase.Title:
            {
                float t = phaseTime;
                float alpha = Math.Clamp(t / TitleIn, 0f, 1f) * Math.Clamp((TitleTime - t) / TitleOut, 0f, 1f);
                return new Look(alpha, -1, 0f, 0f, false, 0f);
            }
            case CreditsPhase.Roll:
            {
                var (member, alpha) = MemberAt(Scroll);
                return new Look(0f, member, alpha, 0f, false, 0f);
            }
            case CreditsPhase.End:
                return new Look(0f, -1, 0f, Math.Clamp(phaseTime / EndIn, 0f, 1f), phaseTime >= EndReady, 0f);
            case CreditsPhase.Out:
                return new Look(0f, -1, 0f, 1f, false, Math.Clamp(phaseTime / OutTime, 0f, 1f));
            default:
                return new Look(0f, -1, 0f, 0f, false, 1f);
        }
    }

    /// <summary>
    /// Who of the team stands beside the roll once it has climbed this far, and how strongly: each for an equal share
    /// of the roll, fading in at the start of their share and out at its end.
    /// </summary>
    public (int Member, float Alpha) MemberAt(float scroll)
    {
        if (team.Count == 0 || Length <= 0f) return (-1, 0f);
        float share = Length / team.Count;
        int member = Math.Clamp((int)(scroll / share), 0, team.Count - 1);
        float into = scroll - member * share;
        float fade = Math.Min(MemberFade * Pace, share / 2f);
        return (member, Math.Clamp(Math.Min(into, share - into) / fade, 0f, 1f));
    }

    /// <summary>How strongly a line shows at a height of the picture: it fades over the last stretch at the top and at the foot.</summary>
    public static float LineAlpha(float screenY) =>
        Math.Clamp(Math.Min(screenY, ViewHeight - screenY) / FadeBand, 0f, 1f);

    /// <summary>Renders whoever of the team stands beside the roll. Call outside any other texture mode.</summary>
    public void Render(RenderContext renderContext)
    {
        if (!IsActive) return;
        var look = Appearance();
        if (look.Member < 0 || look.MemberAlpha <= 0.01f) return;
        renderContext.EnsureLoaded();
        if (!pictureLoaded)
        {
            picture = Gpu.Target(PictureSize, PictureSize, "ui");
            Raylib.SetTextureFilter(picture.Texture, TextureFilter.Bilinear);
            pictureLoaded = true;
        }
        var pokemon = team[look.Member];
        // A model that isn't built is asked for and waited on behind the fade that opened the credits; one let go of
        // since is simply not shown until it is back
        if (!PokemonModels.TryGet(pokemon.LookName, out var model))
        {
            PokemonModels.Request(pokemon.LookName);
            pictured = -1;
            return;
        }
        PokemonSprites.Render(renderContext, model, SpriteView.Front, new PokePose { Time = time }, picture);
        pictured = look.Member;
    }

    public void Draw(int sw, int sh)
    {
        if (!IsActive) return;
        var look = Appearance();
        Texture2D? member = pictureLoaded && pictured == look.Member && look.Member >= 0 ? picture.Texture : null;
        ModernUi.DrawCredits(sw, sh, this, look, member, time);
    }
}
