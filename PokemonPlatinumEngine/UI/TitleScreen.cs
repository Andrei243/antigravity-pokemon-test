using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Where the opening is: the notice, the fly-over shots, Giratina's reveal, the idle title, then the menu and the
/// two questions a new game asks (whether to leave a save behind, and whose rules to play by).
/// </summary>
public enum TitlePhase { Notice, Journey, Reveal, Idle, Menu, ConfirmNewGame, ChooseRules, Leaving }

/// <summary>What the player picked on the title menu.</summary>
public enum TitleChoice { None, Continue, NewGame, Options, Quit }

/// <summary>
/// The opening and the title screen: a short notice, fly-over shots of Sinnoh at different times of day, Giratina
/// appearing out of the dark with the title lettering, "press start", and the menu to continue the saved game
/// (showing its badges, play time, Pokédex and party) or begin a new one. Any button skips ahead to the title.
/// </summary>
public sealed class TitleScreen
{
    public const float NoticeTime = 3.4f, SegmentTime = 3.6f, RevealTime = 3.2f, LeaveTime = 0.8f;

    internal static readonly (string Map, Vector2 From, Vector2 To, float Hour)[] Segments =
    {
        // Up Twinleaf Town's road at dawn, east along Route 201 at midday, through Sandgem Town in the evening
        ("Sinnoh", new Vector2(112f, 888f), new Vector2(112f, 876f), 7f),
        ("Sinnoh", new Vector2(120f, 853f), new Vector2(142f, 851f), 13.5f),
        ("Sinnoh", new Vector2(166f, 846f), new Vector2(184f, 846f), 20.5f)
    };

    private readonly SaveData? save;
    private readonly List<TitleChoice> entries = new();
    private TitleChoice chosen = TitleChoice.None;
    private bool optionsRequested;
    private float phaseTime, totalTime, menuBlend, skipFlash;
    private int warmed;

    public TitlePhase Phase { get; private set; } = TitlePhase.Notice;
    public int SelectedIndex { get; private set; }

    /// <summary>In the "start a new game?" prompt: true while Yes is highlighted.</summary>
    public bool ConfirmYes { get; private set; }

    /// <summary>
    /// Whose rules the new game is played by: what is highlighted in the "which rules?" prompt, and the answer
    /// once New Game has been handed over. It is asked here because it can never be changed afterwards.
    /// </summary>
    public RulesPreset Rules { get; private set; }

    public bool HasSave => save != null;
    public IReadOnlyList<TitleChoice> Entries => entries;

    public TitleScreen(SaveData? save)
    {
        this.save = save;
        if (save != null) entries.Add(TitleChoice.Continue);
        entries.Add(TitleChoice.NewGame);
        entries.Add(TitleChoice.Options);
        entries.Add(TitleChoice.Quit);
    }

    /// <summary>Hours and minutes played, as on the continue panel: 0:07, 12:34, 123:05.</summary>
    public static string FormatPlayTime(float seconds)
    {
        int minutes = (int)(Math.Max(0f, seconds) / 60f);
        return $"{minutes / 60}:{minutes % 60:D2}";
    }

    public static int CountBadges(int mask)
    {
        int count = 0;
        for (int b = 0; b < 8; b++)
            if ((mask & (1 << b)) != 0) count++;
        return count;
    }

    // ------------------------------------------------------------------ logic

    private void Enter(TitlePhase phase)
    {
        Phase = phase;
        phaseTime = 0f;
    }

    /// <summary>Moves the opening forward in time.</summary>
    public void Advance(float dt)
    {
        phaseTime += dt;
        totalTime += dt;
        skipFlash = Math.Max(0f, skipFlash - dt);
        bool menuUp = Phase is TitlePhase.Menu or TitlePhase.ConfirmNewGame or TitlePhase.ChooseRules or TitlePhase.Leaving;
        menuBlend = Math.Clamp(menuBlend + (menuUp ? dt : -dt) / 0.45f, 0f, 1f);

        switch (Phase)
        {
            case TitlePhase.Notice when phaseTime >= NoticeTime: Enter(TitlePhase.Journey); break;
            case TitlePhase.Journey when phaseTime >= SegmentTime * Segments.Length: Enter(TitlePhase.Reveal); break;
            case TitlePhase.Reveal when phaseTime >= RevealTime: Enter(TitlePhase.Idle); break;
        }
    }

    /// <summary>The A button: skips the opening, opens the menu, picks the highlighted entry.</summary>
    public void PressConfirm()
    {
        switch (Phase)
        {
            case TitlePhase.Notice:
            case TitlePhase.Journey:
            case TitlePhase.Reveal:
                Enter(TitlePhase.Idle);
                skipFlash = 0.4f;
                break;
            case TitlePhase.Idle:
                Enter(TitlePhase.Menu);
                SelectedIndex = 0;
                AudioManager.PlaySound("select");
                // As in the original, Giratina answers the button
                if (PokemonDatabase.Get("Giratina") is { } giratina) AudioManager.PlayCry(giratina);
                break;
            case TitlePhase.Menu:
                AudioManager.PlaySound("select");
                switch (entries[SelectedIndex])
                {
                    case TitleChoice.Continue: Leave(TitleChoice.Continue); break;
                    case TitleChoice.NewGame when HasSave:
                        ConfirmYes = false;
                        Enter(TitlePhase.ConfirmNewGame);
                        break;
                    case TitleChoice.NewGame: AskRules(); break;
                    case TitleChoice.Options: optionsRequested = true; break;
                    case TitleChoice.Quit: Leave(TitleChoice.Quit); break;
                }
                break;
            case TitlePhase.ConfirmNewGame:
                AudioManager.PlaySound(ConfirmYes ? "select" : "cancel");
                if (ConfirmYes) AskRules();
                else Enter(TitlePhase.Menu);
                break;
            case TitlePhase.ChooseRules:
                AudioManager.PlaySound("select");
                Leave(TitleChoice.NewGame);
                break;
        }
    }

    /// <summary>The last question before a new game: whose rules. Platinum's are highlighted.</summary>
    private void AskRules()
    {
        Rules = RulesPreset.Platinum;
        Enter(TitlePhase.ChooseRules);
    }

    /// <summary>The B button: backs out of a prompt, then out of the menu.</summary>
    public void PressCancel()
    {
        if (Phase is TitlePhase.ConfirmNewGame or TitlePhase.ChooseRules) Enter(TitlePhase.Menu);
        else if (Phase == TitlePhase.Menu) Enter(TitlePhase.Idle);
        else return;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>Up/down in the menu; any direction flips the answer in a prompt.</summary>
    public void Move(int step)
    {
        if (step == 0) return;
        if (Phase == TitlePhase.Menu) SelectedIndex = ((SelectedIndex + step) % entries.Count + entries.Count) % entries.Count;
        else if (Phase == TitlePhase.ConfirmNewGame) ConfirmYes = !ConfirmYes;
        else if (Phase == TitlePhase.ChooseRules) Rules = Rules == RulesPreset.Platinum ? RulesPreset.Modern : RulesPreset.Platinum;
        else return;
        AudioManager.PlaySound("cursor");
    }

    private void Leave(TitleChoice choice)
    {
        chosen = choice;
        Enter(TitlePhase.Leaving);
    }

    /// <summary>
    /// The player's choice, once: Options straight away; Continue, New Game or Quit when the screen has faded out.
    /// </summary>
    public TitleChoice TakeChoice()
    {
        if (optionsRequested)
        {
            optionsRequested = false;
            return TitleChoice.Options;
        }
        if (Phase == TitlePhase.Leaving && phaseTime >= LeaveTime && chosen != TitleChoice.None)
        {
            var result = chosen;
            chosen = TitleChoice.None;
            return result;
        }
        return TitleChoice.None;
    }

    public void Update(float dt)
    {
        Advance(dt);
        if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Menu)) PressConfirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) PressCancel();
        else if (InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Left)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down) || InputManager.IsActionPressed(GameAction.Right)) Move(1);
    }

    // ------------------------------------------------------------------ drawing

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private float Reveal => Phase == TitlePhase.Reveal ? Smooth(1.2f, 2.4f, phaseTime) : 1f;

    /// <summary>Renders this frame's 3D picture offscreen. Call outside any other texture mode.</summary>
    internal void Render(RenderContext context, WorldRenderer world, TitleScene scene)
    {
        switch (Phase)
        {
            case TitlePhase.Notice:
                // Behind the notice, build what the opening needs (one map a frame, then Giratina) so no shot hitches
                if (warmed < Segments.Length)
                {
                    var first = Segments[warmed];
                    world.RenderCinematic(MapDatabase.Get(first.Map), first.From.X, first.From.Y, first.Hour);
                }
                else if (warmed == Segments.Length) scene.Render(context, 0f, 0f, 0f);
                warmed = Math.Min(warmed + 1, Segments.Length + 1);
                break;
            case TitlePhase.Journey:
                int index = Math.Min((int)(phaseTime / SegmentTime), Segments.Length - 1);
                var (map, from, to, hour) = Segments[index];
                float t = (phaseTime - index * SegmentTime) / SegmentTime;
                var focus = Vector2.Lerp(from, to, t);
                world.RenderCinematic(MapDatabase.Get(map), focus.X, focus.Y, hour);
                break;
            default:
                float ease = menuBlend * menuBlend * (3f - 2f * menuBlend);
                scene.Render(context, totalTime, Reveal, ease * 2.7f);
                break;
        }
    }

    /// <summary>Draws the opening or the title into the current target (the virtual screen).</summary>
    internal void Draw(int sw, int sh, RenderContext context, WorldRenderer world)
    {
        switch (Phase)
        {
            case TitlePhase.Notice:
                DrawNotice(sw, sh);
                return;
            case TitlePhase.Journey:
                DrawJourney(sw, sh, world);
                return;
        }

        context.Composite(new Rectangle(0, 0, sw, sh));
        DrawMotes(sw, sh);

        float ease = menuBlend * menuBlend * (3f - 2f * menuBlend);
        float logoAlpha = Phase == TitlePhase.Reveal ? Smooth(1.7f, 2.7f, phaseTime) : 1f;
        DrawLogo(sw, ease, logoAlpha);

        if (Phase == TitlePhase.Idle)
        {
            float pulse = 0.55f + 0.45f * MathF.Sin(totalTime * 3.2f);
            const string prompt = "PRESS  Z  OR  ENTER";
            float w = UiFonts.Measure(prompt, 40, UiWeight.Black);
            UiFonts.Draw(prompt, (sw - w) / 2f + 2, 954, 40, new Color(0, 0, 0, (int)(120 * pulse)), UiWeight.Black);
            UiFonts.Draw(prompt, (sw - w) / 2f, 952, 40, new Color(255, 255, 255, (int)(255 * pulse)), UiWeight.Black);
        }
        if (menuBlend > 0.01f) DrawMenu(sw, ease);
        if (Phase == TitlePhase.ConfirmNewGame) DrawConfirm(sw, sh);
        if (Phase == TitlePhase.ChooseRules) DrawRules(sw, sh);

        const string credit = "A fan-made project, not affiliated with Nintendo, Creatures or GAME FREAK.";
        float cw = UiFonts.Measure(credit, 20, UiWeight.Bold);
        UiFonts.Draw(credit, (sw - cw) / 2f, sh - 44, 20, new Color(220, 214, 240, (int)(150 * logoAlpha)), UiWeight.Bold);

        // Fades: up from black as Giratina appears, a flash as it lights up or the opening is skipped, out to black on leaving
        if (Phase == TitlePhase.Reveal)
        {
            float black = 1f - Smooth(0f, 1.1f, phaseTime);
            if (black > 0f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * black)));
            float flash = Math.Max(0f, 1f - MathF.Abs(phaseTime - 1.35f) / 0.3f);
            if (flash > 0f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(255, 250, 255, (int)(190 * flash)));
        }
        if (skipFlash > 0f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(255, 250, 255, (int)(200 * skipFlash / 0.4f)));
        if (Phase == TitlePhase.Leaving)
            Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * Math.Clamp(phaseTime / LeaveTime, 0f, 1f))));
    }

    private void DrawNotice(int sw, int sh)
    {
        Raylib.DrawRectangle(0, 0, sw, sh, Color.Black);
        float alpha = Smooth(0.2f, 0.9f, phaseTime) * (1f - Smooth(NoticeTime - 0.8f, NoticeTime - 0.1f, phaseTime));
        var white = new Color(255, 255, 255, (int)(255 * alpha));
        var dim = new Color(190, 196, 214, (int)(255 * alpha));
        void Centered(string text, float y, float size, Color color, UiWeight weight)
        {
            float w = UiFonts.Measure(text, size, weight);
            UiFonts.Draw(text, (sw - w) / 2f, y, size, color, weight);
        }
        Centered("A fan-made remake", sh / 2f - 96, 54, white, UiWeight.Black);
        Centered("This project is not affiliated with or endorsed by Nintendo, Creatures Inc. or GAME FREAK inc.", sh / 2f, 26, dim, UiWeight.Bold);
        Centered("Pokémon and its characters are trademarks of their owners. All art, models and music here are original.", sh / 2f + 42, 26, dim, UiWeight.Bold);
    }

    private void DrawJourney(int sw, int sh, WorldRenderer world)
    {
        world.DrawToScreen(sw, sh);

        // Cinema bars, and a dip to black between shots
        const int bar = 120;
        Raylib.DrawRectangle(0, 0, sw, bar, Color.Black);
        Raylib.DrawRectangle(0, sh - bar, sw, bar, Color.Black);
        float local = phaseTime % SegmentTime;
        float black = Math.Max(1f - Smooth(0f, 0.6f, local), Smooth(SegmentTime - 0.6f, SegmentTime, local));
        if (black > 0f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * black)));
    }

    /// <summary>Motes of light rising slowly through the void.</summary>
    private void DrawMotes(int sw, int sh)
    {
        var glow = SceneTextures.SoftGlow;
        var src = new Rectangle(0, 0, glow.Width, glow.Height);
        float reveal = Reveal;
        Raylib.BeginBlendMode(BlendMode.Additive);
        for (int i = 0; i < 70; i++)
        {
            float seed = i * 12.9898f;
            float fx = MathF.Abs(MathF.Sin(seed) * 43758.5453f) % 1f;
            float fy = MathF.Abs(MathF.Sin(seed * 1.7f) * 24634.6345f) % 1f;
            float speed = 0.012f + (i % 7) * 0.004f;
            float y = 1f - (fy + totalTime * speed) % 1f;
            float x = fx + 0.015f * MathF.Sin(totalTime * 0.6f + i);
            float size = 10f + (i % 5) * 7f;
            float twinkle = 0.5f + 0.5f * MathF.Sin(totalTime * (1.2f + i % 4 * 0.5f) + i);
            var color = i % 3 == 0 ? new Color(255, 214, 150, 255) : new Color(190, 150, 255, 255);
            color.A = (byte)(150 * twinkle * reveal * MathF.Sin(y * MathF.PI));
            Raylib.DrawTexturePro(glow, src, new Rectangle(x * sw - size / 2, y * sh - size / 2, size, size), Vector2.Zero, 0f, color);
        }
        Raylib.EndBlendMode();
    }

    /// <summary>
    /// The title lettering (our own, not the games' logo): large and centred on the idle title, small in the top
    /// left once the menu is up.
    /// </summary>
    private void DrawLogo(int sw, float menu, float alpha)
    {
        if (alpha <= 0.01f) return;
        float scale = 1f - 0.42f * menu;
        const string word = "PLATINUM", series = "Pokémon";
        float size = 196f * scale, spacing = 16f * scale;
        float width = UiFonts.MeasureDisplay(word, size, spacing);
        float x = (sw - width) / 2f * (1f - menu) + 96f * menu;
        float y = (150f - 30f * (1f - alpha)) * (1f - menu) + 86f * menu;

        float seriesSize = 78f * scale;
        var cream = new Color(255, 244, 224, (int)(255 * alpha));
        UiFonts.DrawDisplay(series, x + 6 * scale + 3, y - seriesSize * 0.78f + 4, seriesSize, 2f, new Color(10, 6, 24, (int)(170 * alpha)));
        UiFonts.DrawDisplay(series, x + 6 * scale, y - seriesSize * 0.78f, seriesSize, 2f, cream);

        // Platinum lettering: a dark drop, then a silver gradient with a band of light sweeping across now and then
        UiFonts.DrawDisplay(word, x + 5 * scale, y + 8 * scale, size, spacing, new Color(10, 6, 24, (int)(200 * alpha)));
        float sweep = totalTime % 5.5f;
        float shineX = sweep < 1.6f ? x - 200f + (width + 500f) * (sweep / 1.6f) : float.NaN;
        UiFonts.DrawDisplayGradient(word, x, y, size, spacing, new Color(255, 255, 255, 255), new Color(150, 162, 196, 255), alpha, shineX);

        // A thin rule with a diamond, and the subtitle
        float ruleY = y + size * 0.98f;
        var silver = new Color(214, 222, 240, (int)(230 * alpha));
        UiShapes.Fill(new Rectangle(x + 4, ruleY, width - 8, 4 * scale), 2 * scale, silver);
        const string sub = "A  FAN  REMAKE";
        UiFonts.Draw(sub, x + 6, ruleY + 14 * scale, 30 * scale, new Color(226, 220, 244, (int)(230 * alpha)), UiWeight.Black);
    }

    // ------------------------------------------------------------------ menu

    private void DrawMenu(int sw, float ease)
    {
        const float row = 92, gap = 20, card = 452;
        float slide = (1f - ease) * 520f;
        float x = 1016 + slide, w = 824;
        float total = entries.Count * (row + gap) - gap + (HasSave ? card - row : 0);
        float y = HasSave ? 196 : (1080 - total) / 2f + 40;
        for (int i = 0; i < entries.Count; i++)
        {
            bool selected = i == SelectedIndex && Phase != TitlePhase.Idle;
            float h = entries[i] == TitleChoice.Continue ? card : row;
            var r = new Rectangle(x, y, w, h);
            ModernUi.Card(r, 34, selected);

            string label = entries[i] switch
            {
                TitleChoice.NewGame => "NEW GAME",
                TitleChoice.Options => "OPTIONS",
                _ => "QUIT"
            };
            if (entries[i] == TitleChoice.Continue) ModernUi.SaveSummary(r, save!, "CONTINUE");
            else UiFonts.DrawCentered(label, r.X + 48, r.Y + r.Height / 2f, 40, ModernUi.Ink, UiWeight.Black);
            y += h + gap;
        }
    }

    private void DrawConfirm(int sw, int sh) =>
        ModernUi.Prompt(sw, sh, "Start a new game?", "Your saved game is kept until you save again in the new one.",
            new[] { ("NO, GO BACK", ModernUi.Blue), ("YES, NEW GAME", ModernUi.Red) }, ConfirmYes ? 1 : 0);

    /// <summary>What the "which rules?" prompt says about the answer that is highlighted.</summary>
    public static string RulesBlurb(RulesPreset rules) => rules == RulesPreset.Modern
        ? "Battles use the newest games' numbers for critical hits, burns, paralysis and moves. This can't be changed later."
        : "Battles play exactly as they do in Platinum, which the story is balanced for. This can't be changed later.";

    private void DrawRules(int sw, int sh) =>
        ModernUi.Prompt(sw, sh, "Which rules?", RulesBlurb(Rules),
            new[] { ("PLATINUM RULES", ModernUi.Blue), ("MODERN RULES", ModernUi.Green) }, Rules == RulesPreset.Modern ? 1 : 0);
}
