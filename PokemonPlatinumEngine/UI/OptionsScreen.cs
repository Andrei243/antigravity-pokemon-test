using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The settings the options screen offers, in the order of its rows. A new row goes at the end of the settings,
/// before <see cref="Help"/>: that is no setting but the way into the help pages, and <see cref="ResetDefaults"/>
/// (plan 12 · Q3), the last row, puts every setting back behind a question.
/// </summary>
public enum OptionRow
{
    Language, TextSpeed, Quality, WindowSize, Fullscreen, VSync, TimeOfDay,
    Sound, MusicVolume, SoundVolume, CryVolume, AmbienceVolume, Speakers,
    MoveHints, TrainerLines,
    FocusPause, FrameCap,
    SkipScenes,
    BattleScene, BattleStyle,
    SpeedUp, AutoAdvance, Autorun, Frame,
    ButtonMode, StickDeadZone, Gamepad,
    RecordBattles,
    SlotTwo,
    NextObjective,
    Season,
    FieldHelpers,
    PartyTools,
    BoxesAnywhere,
    Autosave,
    DynamicResolution,
    TimingAssist, Rumble,
    ReduceFlashes, ReduceMotion,
    MedalNotices,
    RunTimer,
    Captions, HoldToConfirm,
    BoxTools,
    AutoBattle,
    AreaChecklist,
    Requests,
    TextSize, ColourVision, HighContrast, Tips,
    RetryBattles,
    Follower, WildPokemon,
    Movement,
    Voices, VoiceVolume, LetVoicesFinish, EnglishVoices,
    Controls,
    Jukebox,
    Packs,
    StreamFeed, StreamSafe,
    Coop,
    BattleCamera,
    ItemGlints,
    Help,
    ResetDefaults
}

/// <summary>
/// The tabs the options are grouped under: every row is in exactly one (<see cref="OptionsScreen.CategoryOf"/>). A new
/// row names its tab there as well as going at the enum's end before HELP.
/// </summary>
public enum OptionCategory { Game, Field, Battle, Display, Sound, Controls, Access, More }

/// <summary>
/// The options screen: the language (plan 17 · N1), text speed, graphics quality, window size, full screen, V-Sync, time of day, and the sound:
/// on or off, a volume for each of the mixer's buses (the music with its fanfares, the sound effects, the cries,
/// the ambience) and the speakers it plays through; then the helps of plan 12 (the move hints), the trainers' lines in battle (plan 11 · C12),
/// the two rows of plan 16 · T3 (whether the game waits behind another window, and the most frames it draws a second),
/// which scenes of the story may be skipped (plan 12 · Q20), Platinum's Battle Scene and Battle Style (plan 08 · P1), the pad's rows (plan 12 · Q4), whether a trainer's battle may be kept on the Vs. Recorder (plan 08 · P10) and the HELP pages
/// (<see cref="HelpScreen"/>), which open over the options. Changes apply at once; the caller saves the settings
/// when the screen reports a change. More rows than fit scroll in a window (<see cref="VisibleRows"/>).
/// </summary>
public class OptionsScreen
{
    /// <summary>Every row of the options, in the order the screen shows them.</summary>
    public static IReadOnlyList<OptionRow> AllRows { get; } = Enum.GetValues<OptionRow>();

    /// <summary>
    /// The rows this screen shows: every one unless it was made with fewer (plan 16 · T24: the first start's page shows a
    /// few and shares <see cref="Change"/> and <see cref="Describe"/> with this screen).
    /// </summary>
    public IReadOnlyList<OptionRow> Rows => fixedRows ?? RowsIn(Category);

    private readonly IReadOnlyList<OptionRow>? fixedRows;

    public OptionsScreen(IReadOnlyList<OptionRow>? rows = null) => fixedRows = rows;

    /// <summary>The rows the screen shows at once, under the tabs and above the help panel.</summary>
    public const int VisibleRows = 8;

    // ---------------------------------------------------------------- the tabs

    /// <summary>Every tab, in the order the strip shows them.</summary>
    public static IReadOnlyList<OptionCategory> Categories { get; } = Enum.GetValues<OptionCategory>();

    /// <summary>Whether the screen is cut into tabs: the options' own screen is; one made with rows of its own shows them in one list.</summary>
    public bool Tabbed => fixedRows == null;

    /// <summary>The tab on show; its rows are <see cref="Rows"/>.</summary>
    public OptionCategory Category { get; private set; }

    /// <summary>The row under the cursor.</summary>
    public OptionRow Selected => Rows[SelectedIndex];

    /// <summary>The rows of a tab, in the enum's order.</summary>
    public static IReadOnlyList<OptionRow> RowsIn(OptionCategory category) => byCategory[(int)category];

    private static readonly OptionRow[][] byCategory =
        Enum.GetValues<OptionCategory>().Select(c => Enum.GetValues<OptionRow>().Where(r => CategoryOf(r) == c).ToArray()).ToArray();

    /// <summary>The tab a row is under.</summary>
    public static OptionCategory CategoryOf(OptionRow row) => row switch
    {
        OptionRow.Language or OptionRow.TextSpeed or OptionRow.AutoAdvance or OptionRow.SpeedUp or OptionRow.SkipScenes
            or OptionRow.Autosave or OptionRow.NextObjective or OptionRow.Tips or OptionRow.Requests or OptionRow.MedalNotices
            or OptionRow.TimeOfDay or OptionRow.Season or OptionRow.SlotTwo => OptionCategory.Game,
        OptionRow.Autorun or OptionRow.Movement or OptionRow.Follower or OptionRow.WildPokemon or OptionRow.FieldHelpers
            or OptionRow.PartyTools or OptionRow.BoxesAnywhere or OptionRow.BoxTools or OptionRow.AreaChecklist or OptionRow.ItemGlints => OptionCategory.Field,
        OptionRow.BattleScene or OptionRow.BattleStyle or OptionRow.MoveHints or OptionRow.TrainerLines or OptionRow.AutoBattle
            or OptionRow.RetryBattles or OptionRow.RecordBattles or OptionRow.Coop or OptionRow.BattleCamera => OptionCategory.Battle,
        OptionRow.Quality or OptionRow.WindowSize or OptionRow.Fullscreen or OptionRow.VSync or OptionRow.FrameCap
            or OptionRow.DynamicResolution or OptionRow.FocusPause or OptionRow.Frame => OptionCategory.Display,
        OptionRow.Sound or OptionRow.MusicVolume or OptionRow.SoundVolume or OptionRow.CryVolume or OptionRow.AmbienceVolume
            or OptionRow.Speakers or OptionRow.Voices or OptionRow.VoiceVolume or OptionRow.LetVoicesFinish or OptionRow.EnglishVoices => OptionCategory.Sound,
        OptionRow.ButtonMode or OptionRow.StickDeadZone or OptionRow.Gamepad or OptionRow.Rumble or OptionRow.Controls => OptionCategory.Controls,
        OptionRow.TextSize or OptionRow.ColourVision or OptionRow.HighContrast or OptionRow.ReduceFlashes or OptionRow.ReduceMotion
            or OptionRow.Captions or OptionRow.HoldToConfirm or OptionRow.TimingAssist => OptionCategory.Access,
        OptionRow.RunTimer or OptionRow.StreamFeed or OptionRow.StreamSafe or OptionRow.Jukebox or OptionRow.Packs
            or OptionRow.Help or OptionRow.ResetDefaults => OptionCategory.More,
        _ => throw new ArgumentOutOfRangeException(nameof(row), row, "every option row is under a tab: name it in OptionsScreen.CategoryOf")
    };

    /// <summary>The next or the previous tab, wrapping round, with the cursor on its first row.</summary>
    public void ChangeCategory(int step)
    {
        if (!Tabbed) return;
        ShowCategory((OptionCategory)(((int)Category + step + Categories.Count) % Categories.Count));
    }

    /// <summary>A tab put on show, the cursor on its first row.</summary>
    public void ShowCategory(OptionCategory category)
    {
        if (!Tabbed || category == Category) return;
        Category = category;
        SelectedIndex = 0;
        FirstRow = 0;
        AudioManager.PlaySound("page");
    }

    /// <summary>The cursor onto a row, its tab put on show first: how a tool or a test finds a row by its name.</summary>
    public void Select(OptionRow row)
    {
        if (Tabbed) Category = CategoryOf(row);
        int at = Rows.ToList().IndexOf(row);
        if (at < 0) return;
        SelectedIndex = at;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, Rows.Count, VisibleRows);
    }

    /// <summary>Where the strip of tabs stands.</summary>
    public static Rectangle TabStrip => new(64, ModernUi.ContentTop, GameEngine.VirtualWidth - 128, 76);

    /// <summary>Where the first row stands: under the strip when there is one.</summary>
    private float ListTop => ModernUi.ContentTop + (Tabbed ? 76 + 16 : 0);

    /// <summary>A tab's rectangle on the strip, as <see cref="ModernUi.Tabs"/> lays them out.</summary>
    public static Rectangle TabRect(int i)
    {
        const float gap = 12;
        var strip = TabStrip;
        float w = (strip.Width - gap * (Categories.Count - 1)) / Categories.Count;
        return new Rectangle(strip.X + i * (w + gap), strip.Y, w, strip.Height);
    }

    /// <summary>What <see cref="HitTest"/> gives for a tab: this plus the tab's index.</summary>
    public const int TabHit = 1000;

    public bool IsActive { get; private set; }
    public int SelectedIndex { get; set; }

    /// <summary>The first row in the window.</summary>
    public int FirstRow { get; private set; }

    /// <summary>The help pages, open over the options while <see cref="HelpScreen.IsActive"/>.</summary>
    public HelpScreen Help { get; } = new();

    /// <summary>The Controls page (plan 12 · Q5), open over the options while <see cref="ControlsScreen.IsActive"/>.</summary>
    public ControlsScreen Controls { get; } = new();

    /// <summary>The packs screen (plan 16 · T15), open over the options while <see cref="PacksScreen.IsActive"/>.</summary>
    public PacksScreen Packs { get; } = new();

    private bool jukeboxRequested;

    /// <summary>True while the reset question (plan 12 · Q3) is up over the options.</summary>
    public bool ResetAsking { get; private set; }

    /// <summary>The reset question's answer under the cursor: true for YES. NO is first, so it is the one it opens on.</summary>
    public bool ResetYes { get; private set; }

    private readonly UiReveal resetReveal = new(0.18f, 0.1f);

    /// <summary>Whether the JUKEBOX row was opened since the last ask (plan 12 · Q15), said once: the game opens the jukebox over the options.</summary>
    public bool TakeJukeboxRequest()
    {
        bool asked = jukeboxRequested;
        jukeboxRequested = false;
        return asked;
    }

    public void Open()
    {
        IsActive = true;
        Category = OptionCategory.Game;
        SelectedIndex = 0;
        FirstRow = 0;
        ResetAsking = false;
        Help.Close();
        Controls.Close();
        Packs.Close();
    }

    /// <summary>Moves the reset question's slide-in and slide-out along; call every frame the options are up.</summary>
    public void Animate(float dt)
    {
        resetReveal.Update(dt);
        frameTime = dt;
    }

    /// <summary>The last frame's time, which the reset's hold counts by (plan 12 · Q9).</summary>
    private float frameTime;

    /// <summary>The hold the reset's YES waits for while the options' HOLD TO CONFIRM row is on (plan 12 · Q9).</summary>
    public ConfirmHold ResetHold { get; } = new();

    /// <summary>
    /// The confirm button on the reset question: NO is answered at once, and so is YES unless the hold is asked for, in
    /// which case the hold begins and nothing is reset yet (true: the options were reset, as <see cref="AnswerReset"/>).
    /// </summary>
    public bool ConfirmReset(GameSettings settings)
    {
        if (!ResetAsking) return false;
        if (ResetYes && !ResetHold.Press()) return false;
        return AnswerReset(settings, ResetYes);
    }

    /// <summary>Any arrow on the reset question: the other answer.</summary>
    public void ToggleReset()
    {
        if (!ResetAsking) return;
        ResetYes = !ResetYes;
        ResetHold.Stop();
        AudioManager.PlaySound("cursor");
    }

    /// <summary>A frame of the reset's hold: <paramref name="down"/> whether the button is still down; true once the hold is done and the options reset.</summary>
    public bool HoldReset(GameSettings settings, float dt, bool down) =>
        ResetHold.Advance(dt, down) && AnswerReset(settings, true);

    /// <summary>The question that comes before every option is put back (plan 12 · Q3); it opens on NO.</summary>
    public void AskReset()
    {
        ResetAsking = true;
        ResetYes = false;
        ResetHold.Stop();
        resetReveal.Open();
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>
    /// The reset question answered: YES puts every option back as <c>new GameSettings()</c> has it and says so (true: the
    /// caller applies and saves them); NO leaves them. Either puts the question away.
    /// </summary>
    public bool AnswerReset(GameSettings settings, bool yes)
    {
        if (!ResetAsking) return false;
        ResetAsking = false;
        ResetHold.Stop();
        resetReveal.Close();
        AudioManager.PlaySound(yes ? "select" : "cancel");
        if (!yes) return false;
        settings.ResetToDefaults();
        return true;
    }

    // ---------------------------------------------------------------- the pointer (plan 12 · Q6)

    /// <summary>A row of the list as it stands on the screen: the <paramref name="k"/>th shown.</summary>
    public Rectangle RowRect(int k)
    {
        const float pitch = 86, height = 78;
        float width = GameEngine.VirtualWidth - 128 - (Rows.Count > VisibleRows ? 36 : 0);
        return new Rectangle(64, ListTop + k * pitch, width, height);
    }

    /// <summary>
    /// What lies under a point: a row of the list (its index among the rows; a click is the A button, which steps a
    /// setting on or opens a page), or NO and YES while the reset question is up. The pages opened from here are the keys'.
    /// </summary>
    public int? HitTest(System.Numerics.Vector2 at)
    {
        if (!IsActive || Help.IsActive || Controls.IsActive || Packs.IsActive) return null;
        if (ResetAsking) return UiPointer.Hit(ModernUi.PromptButtons(GameEngine.VirtualWidth, GameEngine.VirtualHeight, 2), at);
        if (Tabbed)
            for (int i = 0; i < Categories.Count; i++)
                if (UiPointer.Inside(TabRect(i), at)) return TabHit + i;
        for (int k = 0; k < Math.Min(VisibleRows, Rows.Count); k++)
            if (UiPointer.Inside(RowRect(k), at)) return FirstRow + k;
        return null;
    }

    /// <summary>A click: on a tab it puts the tab on show (true: nothing more is done with it); a row's click is the A button.</summary>
    public bool Press(int index)
    {
        if (ResetAsking || index < TabHit) return false;
        ShowCategory(Categories[index - TabHit]);
        return true;
    }

    /// <summary>A hover: the cursor onto the row (or the reset question's answer) under the pointer.</summary>
    public void PointAt(int index)
    {
        if (ResetAsking)
        {
            if ((index == 1) != ResetYes) ToggleReset();
            return;
        }
        if (index >= TabHit) return;
        if (index != SelectedIndex && index >= 0 && index < Rows.Count) Move(index - SelectedIndex);
    }

    /// <summary>One step of the cursor up or down the rows, wrapping round; the window follows it.</summary>
    public void Move(int dy)
    {
        SelectedIndex = (SelectedIndex + dy + Rows.Count) % Rows.Count;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, Rows.Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Handles input; returns true when a setting changed and should be applied and saved.</summary>
    public bool Update(GameSettings settings)
    {
        if (!IsActive) return false;
        if (Help.IsActive)
        {
            Help.Update();
            return false;
        }
        if (Controls.IsActive) return Controls.Update(settings);
        if (Packs.IsActive) return Packs.Update(settings);
        // The reset question holds the screen until it is answered: B is NO
        if (ResetAsking)
        {
            // YES held for the hold (plan 12 · Q9): nothing else is read until it is done or let go
            if (ResetHold.Holding) return HoldReset(settings, frameTime, InputManager.IsActionDown(GameAction.Confirm));
            if (InputManager.IsActionPressed(GameAction.Left) || InputManager.IsActionPressed(GameAction.Right)
                || InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Down))
                ToggleReset();
            else if (InputManager.IsActionPressed(GameAction.Cancel)) AnswerReset(settings, false);
            else if (InputManager.IsActionPressed(GameAction.Confirm)) return ConfirmReset(settings);
            return false;
        }

        if (Tabbed && InputManager.IsActionPressed(GameAction.PageLeft)) ChangeCategory(-1);
        else if (Tabbed && InputManager.IsActionPressed(GameAction.PageRight)) ChangeCategory(1);
        else if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu))
        {
            IsActive = false;
            AudioManager.PlaySound("cancel");
        }
        else if (Rows[SelectedIndex] is OptionRow.Help or OptionRow.Controls or OptionRow.Jukebox or OptionRow.Packs or OptionRow.ResetDefaults)
        {
            if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm(settings);
        }
        else
        {
            int step = InputManager.IsActionPressed(GameAction.Left) ? -1
                : InputManager.IsActionPressed(GameAction.Right) || InputManager.IsActionPressed(GameAction.Confirm) ? 1 : 0;
            if (step != 0) return Step(settings, step);
        }
        return false;
    }

    /// <summary>
    /// A press of left, right or A on the row the cursor is on: moves its setting a step and returns true; or, for the Battle
    /// Style row a run keeps on Set (plan 13 · V5), says no with the error sound and changes nothing.
    /// </summary>
    public bool Step(GameSettings settings, int step)
    {
        if (Rows[SelectedIndex] == OptionRow.BattleStyle && StyleLocked)
        {
            AudioManager.PlaySound("error");
            return false;
        }
        Change(settings, Rows[SelectedIndex], step);
        AudioManager.PlaySound("select");
        return true;
    }

    /// <summary>The A button on the row the cursor is on: HELP opens the help pages; any other row is changed by Update.</summary>
    public void Confirm(GameSettings? settings = null)
    {
        if (Rows[SelectedIndex] == OptionRow.Controls && settings != null)
        {
            Controls.Open(settings);
            AudioManager.PlaySound("select");
            return;
        }
        if (Rows[SelectedIndex] == OptionRow.Packs && settings != null)
        {
            Packs.OpenHere(settings);
            AudioManager.PlaySound("select");
            return;
        }
        if (Rows[SelectedIndex] == OptionRow.Jukebox)
        {
            jukeboxRequested = true;
            AudioManager.PlaySound("select");
            return;
        }
        if (Rows[SelectedIndex] == OptionRow.ResetDefaults)
        {
            AskReset();
            return;
        }
        if (Rows[SelectedIndex] != OptionRow.Help) return;
        Help.Open();
        AudioManager.PlaySound("select");
    }

    /// <summary>Moves one setting to its next or previous value, wrapping round.</summary>
    public static void Change(GameSettings s, OptionRow row, int step)
    {
        switch (row)
        {
            case OptionRow.Language:
                // Only the languages a player can read with the faces the game has, and never the pseudo-language
                var offered = Core.Language.Offered;
                int at = offered.ToList().FindIndex(l => string.Equals(l.Tag, s.Language, StringComparison.OrdinalIgnoreCase));
                s.Language = offered[Wrap((at < 0 ? 0 : at) + step, offered.Count)].Tag;
                break;
            case OptionRow.TextSpeed:
                s.TextSpeed = (TextSpeed)Wrap((int)s.TextSpeed + step, 3);
                break;
            case OptionRow.Quality:
                s.Quality = (GraphicsQuality)Wrap((int)s.Quality + step, 3);
                break;
            case OptionRow.WindowSize:
                var sizes = GameSettings.WindowSizes;
                int current = Array.FindIndex(sizes, z => z.Width == s.WindowWidth && z.Height == s.WindowHeight);
                if (current < 0) current = Array.IndexOf(sizes, GameSettings.DefaultWindow);
                var next = sizes[Wrap(current + step, sizes.Length)];
                s.WindowWidth = next.Width;
                s.WindowHeight = next.Height;
                break;
            case OptionRow.Fullscreen:
                s.Fullscreen = !s.Fullscreen;
                break;
            case OptionRow.VSync:
                s.VSync = !s.VSync;
                break;
            case OptionRow.TimeOfDay:
                // Clock, then the five fixed times in the order of the day
                int index = s.TimeOfDay.HasValue ? (int)s.TimeOfDay.Value + 1 : 0;
                index = Wrap(index + step, 6);
                s.TimeOfDay = index == 0 ? null : (TimeOfDay)(index - 1);
                break;
            case OptionRow.Sound:
                s.Muted = !s.Muted;
                break;
            case OptionRow.MusicVolume:
                s.MusicVolume = Math.Clamp(s.MusicVolume + step * GameSettings.VolumeStep, 0, 100);
                break;
            case OptionRow.SoundVolume:
                s.SoundVolume = Math.Clamp(s.SoundVolume + step * GameSettings.VolumeStep, 0, 100);
                break;
            case OptionRow.CryVolume:
                s.CryVolume = Math.Clamp(s.CryVolume + step * GameSettings.VolumeStep, 0, 100);
                break;
            case OptionRow.AmbienceVolume:
                s.AmbienceVolume = Math.Clamp(s.AmbienceVolume + step * GameSettings.VolumeStep, 0, 100);
                break;
            case OptionRow.Speakers:
                // Stereo, the handheld's speakers, one channel (plan 08 · P1), and round again
                s.Speakers = (SpeakerMode)Wrap((int)s.Speakers + step, 3);
                break;
            case OptionRow.MoveHints:
                s.MoveHints = s.MoveHints == RulesDefault.Off ? RulesDefault.On : RulesDefault.Off;
                break;
            case OptionRow.TrainerLines:
                s.TrainerLines = s.TrainerLines == RulesDefault.Off ? RulesDefault.On : RulesDefault.Off;
                break;
            case OptionRow.NextObjective:
                s.NextObjective = s.NextObjective == RulesDefault.Off ? RulesDefault.On : RulesDefault.Off;
                break;
            case OptionRow.FocusPause:
                s.FocusPause = !s.FocusPause;
                break;
            case OptionRow.FrameCap:
                // 30, 60, 120, none, and round again; a number the file was given by hand counts as 60
                var caps = GameSettings.FrameCaps;
                int cap = Array.IndexOf(caps, s.FrameCap);
                s.FrameCap = caps[Wrap((cap < 0 ? Array.IndexOf(caps, 60) : cap) + step, caps.Length)];
                break;
            case OptionRow.SkipScenes:
                s.SkipScenes = (SkipScenes)Wrap((int)s.SkipScenes + step, 3);
                break;
            case OptionRow.BattleScene:
                s.BattleScene = !s.BattleScene;
                break;
            case OptionRow.BattleStyle:
                s.BattleStyle = s.BattleStyle == BattleStyle.Shift ? BattleStyle.Set : BattleStyle.Shift;
                break;
            case OptionRow.SpeedUp:
                s.SpeedUp = (SpeedUp)Wrap((int)s.SpeedUp + step, 3);
                break;
            case OptionRow.AutoAdvance:
                s.AutoAdvance = (AutoAdvance)Wrap((int)s.AutoAdvance + step, 3);
                break;
            case OptionRow.Autorun:
                s.Autorun = !s.Autorun;
                break;
            case OptionRow.Frame:
                s.Frame = (TextFrame)Wrap((int)s.Frame + step, 6);
                break;
            case OptionRow.ButtonMode:
                // Normal, Start=X, L=A, and round again (plan 12 · Q4)
                s.ButtonMode = (ButtonMode)Wrap((int)s.ButtonMode + step, 3);
                break;
            case OptionRow.StickDeadZone:
                // 10 to 60 percent along GameSettings.StickDeadZones; a number the file was given by hand counts as the nearest
                var zones = GameSettings.StickDeadZones;
                int zone = Array.IndexOf(zones, s.StickDeadZone);
                if (zone < 0) zone = Array.IndexOf(zones, zones.OrderBy(z => Math.Abs(z - s.StickDeadZone)).First());
                s.StickDeadZone = zones[Math.Clamp(zone + step, 0, zones.Length - 1)];
                break;
            case OptionRow.Gamepad:
                // Pad 1 to 4, and round again
                s.GamepadIndex = Wrap(Math.Clamp(s.GamepadIndex, 0, 3) + step, 4);
                break;
            case OptionRow.RecordBattles:
                s.RecordBattles = !s.RecordBattles;
                break;
            case OptionRow.SlotTwo:
                // None, Ruby, Sapphire, Emerald, FireRed, LeafGreen, and round again (plan 08 · P23)
                s.SlotTwo = (SlotTwoGame)Wrap((int)s.SlotTwo + step, 6);
                break;
            case OptionRow.Season:
                // The calendar, then the four seasons in the order the months bring them (plan 22 · U3)
                int season = s.Season.HasValue ? (int)s.Season.Value + 1 : 0;
                season = Wrap(season + step, 5);
                s.Season = season == 0 ? null : (Season)(season - 1);
                break;
            case OptionRow.FieldHelpers:
                // Off, as the rules have it, On (plan 12 · Q18), and round again
                s.FieldHelpers = (RulesDefault)Wrap((int)s.FieldHelpers + step, 3);
                break;
            case OptionRow.BoxTools:
                // Off, as the rules have it, On (plan 12 · Q26), and round again
                s.BoxTools = (RulesDefault)Wrap((int)s.BoxTools + step, 3);
                break;
            case OptionRow.AutoBattle:
                s.AutoBattle = !s.AutoBattle;
                break;
            case OptionRow.Requests:
                s.Requests = s.Requests == RulesDefault.Off ? RulesDefault.On : RulesDefault.Off;
                break;
            case OptionRow.RetryBattles:
                s.RetryBattles = !s.RetryBattles;
                break;
            case OptionRow.Follower:
                s.Follower = !s.Follower;
                break;
            case OptionRow.WildPokemon:
                s.WildInSight = !s.WildInSight;
                break;
            case OptionRow.Movement:
                // As the region says, or the grid's four ways everywhere (plan 30 · FM1)
                s.Movement = s.Movement == MovementChoice.AsTheRegion ? MovementChoice.FourWays : MovementChoice.AsTheRegion;
                break;
            case OptionRow.AreaChecklist:
                s.AreaChecklist = !s.AreaChecklist;
                break;
            case OptionRow.Voices:
                // Off, Story only, All, and round again (plan 26 · V2)
                s.Voices = (VoiceMode)Wrap((int)s.Voices + step, 3);
                break;
            case OptionRow.VoiceVolume:
                s.VoiceVolume = Math.Clamp(s.VoiceVolume + step * GameSettings.VolumeStep, 0, GameSettings.MaxVoiceVolume);
                break;
            case OptionRow.LetVoicesFinish:
                s.LetVoicesFinish = !s.LetVoicesFinish;
                break;
            case OptionRow.EnglishVoices:
                s.EnglishVoices = !s.EnglishVoices;
                break;
            case OptionRow.Tips:
                // Off, as the rules have it, On (plan 12 · Q23), and round again
                s.Tips = (RulesDefault)Wrap((int)s.Tips + step, 3);
                break;
            case OptionRow.BoxesAnywhere:
                // Off, as the rules have it, On (plan 12 · Q13), and round again
                s.BoxesAnywhere = (RulesDefault)Wrap((int)s.BoxesAnywhere + step, 3);
                break;
            case OptionRow.PartyTools:
                // Off, as the rules have it, On (plan 12 · Q19), and round again
                s.PartyTools = (RulesDefault)Wrap((int)s.PartyTools + step, 3);
                break;
            case OptionRow.ItemGlints:
                // Off, as the rules have it, On (plan 38 · EX3), and round again
                s.ItemGlints = (RulesDefault)Wrap((int)s.ItemGlints + step, 3);
                break;
            case OptionRow.Autosave:
                s.Autosave = !s.Autosave;
                break;
            case OptionRow.DynamicResolution:
                // Only where the card can be waited for (plan 16 · T23): elsewhere the row says why and stays off
                if (DynamicResolutionUsable) s.DynamicResolution = !s.DynamicResolution;
                break;
            case OptionRow.TimingAssist:
                // Off, Generous, Automatic, and round again (plan 12 · Q24)
                s.TimingAssist = (TimingAssist)Wrap((int)s.TimingAssist + step, 3);
                break;
            case OptionRow.Rumble:
                // Off, Light, Full, and round again (plan 12 · Q24)
                s.Rumble = (Rumble)Wrap((int)s.Rumble + step, 3);
                break;
            case OptionRow.ReduceFlashes:
                s.ReduceFlashes = !s.ReduceFlashes; // plan 12 · Q9
                break;
            case OptionRow.ReduceMotion:
                s.ReduceMotion = !s.ReduceMotion;
                break;
            case OptionRow.MedalNotices:
                s.MedalNotices = !s.MedalNotices; // plan 13 · V9
                break;
            case OptionRow.RunTimer:
                // Off, the Hall of Fame alone, the Badges, the whole story, and round again (plan 13 · V10)
                s.RunTimer = (Models.TimerCategory)Wrap((int)s.RunTimer + step, 4);
                break;
            case OptionRow.StreamFeed:
                // Off, a file, a file and a LiveSplit splits file, and round again (plan 13 · V24)
                s.StreamFeed = (StreamFeedMode)Wrap((int)s.StreamFeed + step, 3);
                break;
            case OptionRow.StreamSafe:
                s.StreamSafe = !s.StreamSafe;
                break;
            case OptionRow.Coop:
                s.Coop = !s.Coop; // plan 13 · V20
                break;
            case OptionRow.BattleCamera:
                // Classic, Region, Lively, and round again (plan 45 · BC1)
                s.BattleCamera = (BattleCameraChoice)Wrap((int)s.BattleCamera + step, 3);
                break;
            case OptionRow.Captions:
                s.Captions = !s.Captions; // plan 12 · Q9
                break;
            case OptionRow.HoldToConfirm:
                s.HoldToConfirm = !s.HoldToConfirm;
                break;
            case OptionRow.TextSize:
                s.TextSize = s.TextSize == Core.TextSize.Normal ? Core.TextSize.Large : Core.TextSize.Normal; // plan 12 · Q8
                break;
            case OptionRow.ColourVision:
                // Normal, Protan, Deutan, Tritan, and round again
                s.ColourVision = (ColourVision)Wrap((int)s.ColourVision + step, 4);
                break;
            case OptionRow.HighContrast:
                s.HighContrast = !s.HighContrast;
                break;
        }
    }

    private static int Wrap(int value, int count) => ((value % count) + count) % count;

    /// <summary>
    /// Whether the run in force keeps the Battle Style on Set (plan 13 · V5, <c>RunRules.SetStyle</c>): the row then reads Set
    /// whatever the file holds, says why, and cannot be changed. The game sets it as the screen is used; false from the title.
    /// </summary>
    public bool StyleLocked { get; set; }

    /// <summary>A row as it is drawn: <see cref="Describe"/>, but for the Battle Style row a run has locked on Set.</summary>
    private (string Label, string Value, string Help) Described(GameSettings s, OptionRow row) =>
        row == OptionRow.BattleStyle && StyleLocked
            ? (Text.Of("options.battleStyle.label"), Text.Of("options.battleStyle.value.Set"), Text.Of("options.battleStyle.help.locked"))
            : Describe(s, row);

    /// <summary>
    /// Whether dynamic resolution can be used here (plan 16 · T23): it times a frame by waiting for the card, which
    /// needs <c>Gl.Available</c>. The game sets it as it starts; a screen nobody told is usable.
    /// </summary>
    public static bool DynamicResolutionUsable { get; set; } = true;

    /// <summary>The Quality row's help: the preset's, and what the first run measured on this machine where it measured (plan 16 · T23).</summary>
    private static string QualityHelp(GameSettings s) => s.MeasuredQuality is { } measured
        ? Text.Of($"options.quality.help.{s.Quality}") + " " + Text.Of("options.quality.measured",
            ("quality", Text.Of($"options.quality.value.{measured}")), ("ms", (s.MeasuredMilliseconds ?? 0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)))
        : Text.Of($"options.quality.help.{s.Quality}");

    public static (string Label, string Value, string Help) Describe(GameSettings s, OptionRow row) => row switch
    {
        OptionRow.Language => (Text.Of("options.language.label"), (Core.Language.Of(s.Language) ?? Text.Language).Name, Text.Of("options.language.help")),
        OptionRow.TextSpeed => (Text.Of("options.textSpeed.label"), Text.Of($"options.textSpeed.value.{s.TextSpeed}"), Text.Of("options.textSpeed.help")),
        OptionRow.Quality => (Text.Of("options.quality.label"), Text.Of($"options.quality.value.{s.Quality}"), QualityHelp(s)),
        OptionRow.WindowSize => (Text.Of("options.windowSize.label"), Text.Of("options.windowSize.value", ("width", s.WindowWidth), ("height", s.WindowHeight)), Text.Of("options.windowSize.help")),
        OptionRow.Fullscreen => (Text.Of("options.fullscreen.label"), OnOff(s.Fullscreen), Text.Of("options.fullscreen.help")),
        OptionRow.VSync => (Text.Of("options.vsync.label"), OnOff(s.VSync), Text.Of("options.vsync.help")),
        OptionRow.TimeOfDay => (Text.Of("options.timeOfDay.label"), s.TimeOfDay is { } time ? Text.Of($"options.timeOfDay.value.{time}") : Text.Of("options.timeOfDay.value.Clock"),
            Text.Of(s.TimeOfDay.HasValue ? "options.timeOfDay.help.fixed" : "options.timeOfDay.help.clock")),
        OptionRow.Sound => (Text.Of("options.sound.label"), OnOff(!s.Muted), Text.Of("options.sound.help")),
        OptionRow.MusicVolume => (Text.Of("options.music.label"), Percent(s.MusicVolume), Text.Of("options.music.help")),
        OptionRow.SoundVolume => (Text.Of("options.soundEffects.label"), Percent(s.SoundVolume), Text.Of("options.soundEffects.help")),
        OptionRow.CryVolume => (Text.Of("options.cries.label"), Percent(s.CryVolume), Text.Of("options.cries.help")),
        OptionRow.AmbienceVolume => (Text.Of("options.ambience.label"), Percent(s.AmbienceVolume), Text.Of("options.ambience.help")),
        OptionRow.Speakers => (Text.Of("options.speakers.label"), Text.Of($"options.speakers.value.{s.Speakers}"), Text.Of($"options.speakers.help.{s.Speakers}")),
        OptionRow.MoveHints => (Text.Of("options.moveHints.label"), OnOff(s.MoveHints != RulesDefault.Off), Text.Of("options.moveHints.help")),
        OptionRow.TrainerLines => (Text.Of("options.trainerLines.label"), OnOff(s.TrainerLines != RulesDefault.Off), Text.Of("options.trainerLines.help")),
        OptionRow.FocusPause => (Text.Of("options.focusPause.label"), OnOff(s.FocusPause), Text.Of(s.FocusPause ? "options.focusPause.help.on" : "options.focusPause.help.off")),
        OptionRow.FrameCap => (Text.Of("options.frameCap.label"),
            s.FrameCap > 0 ? Text.Of("options.frameCap.value", ("n", s.FrameCap)) : Text.Of("options.frameCap.none"),
            Text.Of(s.FrameCap > 0 ? "options.frameCap.help.capped" : "options.frameCap.help.none")),
        OptionRow.SkipScenes => (Text.Of("options.skipScenes.label"), Text.Of($"options.skipScenes.value.{s.SkipScenes}"), Text.Of($"options.skipScenes.help.{s.SkipScenes}")),
        OptionRow.BattleScene => (Text.Of("options.battleScene.label"), OnOff(s.BattleScene), Text.Of(s.BattleScene ? "options.battleScene.help.on" : "options.battleScene.help.off")),
        OptionRow.BattleStyle => (Text.Of("options.battleStyle.label"), Text.Of($"options.battleStyle.value.{s.BattleStyle}"), Text.Of($"options.battleStyle.help.{s.BattleStyle}")),
        OptionRow.SpeedUp => (Text.Of("options.speedUp.label"), Text.Of($"options.speedUp.value.{s.SpeedUp}"), Text.Of($"options.speedUp.help.{s.SpeedUp}")),
        OptionRow.AutoAdvance => (Text.Of("options.autoAdvance.label"), Text.Of($"options.autoAdvance.value.{s.AutoAdvance}"), Text.Of($"options.autoAdvance.help.{s.AutoAdvance}")),
        OptionRow.Autorun => (Text.Of("options.autorun.label"), OnOff(s.Autorun), Text.Of(s.Autorun ? "options.autorun.help.on" : "options.autorun.help.off")),
        OptionRow.Frame => (Text.Of("options.frame.label"), Text.Of($"options.frame.value.{s.Frame}"), Text.Of("options.frame.help")),
        OptionRow.ButtonMode => (Text.Of("options.buttonMode.label"), Text.Of($"options.buttonMode.value.{s.ButtonMode}"), Text.Of($"options.buttonMode.help.{s.ButtonMode}")),
        OptionRow.StickDeadZone => (Text.Of("options.stickDeadZone.label"), Text.Of("options.stickDeadZone.value", ("n", s.StickDeadZone)), Text.Of("options.stickDeadZone.help")),
        OptionRow.Gamepad => (Text.Of("options.gamepad.label"), Text.Of("options.gamepad.value", ("n", Math.Clamp(s.GamepadIndex, 0, 3) + 1)), Text.Of("options.gamepad.help")),
        OptionRow.RecordBattles => (Text.Of("options.recordBattles.label"), OnOff(s.RecordBattles), Text.Of(s.RecordBattles ? "options.recordBattles.help.on" : "options.recordBattles.help.off")),
        OptionRow.SlotTwo => (Text.Of("options.slotTwo.label"), Text.Of($"options.slotTwo.value.{s.SlotTwo}"),
            Text.Of(s.SlotTwo == SlotTwoGame.None ? "options.slotTwo.help.None" : "options.slotTwo.help.game", ("game", Text.Of($"options.slotTwo.value.{s.SlotTwo}")))),
        OptionRow.NextObjective => (Text.Of("options.nextObjective.label"), OnOff(s.NextObjective != RulesDefault.Off), Text.Of(s.NextObjective != RulesDefault.Off ? "options.nextObjective.help.on" : "options.nextObjective.help.off")),
        OptionRow.Season => (Text.Of("options.season.label"), s.Season is { } season ? Words.Season(season) : Text.Of("options.season.value.Calendar"),
            Text.Of(s.Season.HasValue ? "options.season.help.fixed" : "options.season.help.calendar")),
        OptionRow.FieldHelpers => (Text.Of("options.fieldHelpers.label"), Text.Of($"options.fieldHelpers.value.{s.FieldHelpers}"), Text.Of($"options.fieldHelpers.help.{s.FieldHelpers}")),
        OptionRow.PartyTools => (Text.Of("options.partyTools.label"), Text.Of($"options.partyTools.value.{s.PartyTools}"), Text.Of($"options.partyTools.help.{s.PartyTools}")),
        OptionRow.ItemGlints => (Text.Of("options.itemGlints.label"), Text.Of($"options.itemGlints.value.{s.ItemGlints}"), Text.Of($"options.itemGlints.help.{s.ItemGlints}")),
        OptionRow.BoxTools => (Text.Of("boxTools.option.label"), Text.Of($"boxTools.option.value.{s.BoxTools}"), Text.Of($"boxTools.option.help.{s.BoxTools}")),
        OptionRow.AutoBattle => (Text.Of("options.autoBattle.label"), OnOff(s.AutoBattle), Text.Of(s.AutoBattle ? "options.autoBattle.help.on" : "options.autoBattle.help.off")),
        OptionRow.RetryBattles => (Text.Of("options.retryBattles.label"), OnOff(s.RetryBattles), Text.Of(s.RetryBattles ? "options.retryBattles.help.on" : "options.retryBattles.help.off")),
        OptionRow.Follower => (Text.Of("options.follower.label"), OnOff(s.Follower), Text.Of(s.Follower ? "options.follower.help.on" : "options.follower.help.off")),
        OptionRow.WildPokemon => (Text.Of("options.wildPokemon.label"), Text.Of(s.WildInSight ? "options.wildPokemon.value.visible" : "options.wildPokemon.value.hidden"), Text.Of(s.WildInSight ? "options.wildPokemon.help.visible" : "options.wildPokemon.help.hidden")),
        OptionRow.Movement => s.Movement == MovementChoice.FourWays
            ? (Text.Of("options.movement.label"), Text.Of("options.movement.value.fourWays"), Text.Of("options.movement.help.fourWays"))
            : (Text.Of("options.movement.label"), Text.Of("options.movement.value.region"), Text.Of("options.movement.help.region")),
        OptionRow.Requests => (Text.Of("options.requests.label"), OnOff(s.Requests != RulesDefault.Off), Text.Of(s.Requests != RulesDefault.Off ? "options.requests.help.on" : "options.requests.help.off")),
        OptionRow.Tips => (Text.Of("options.tips.label"), Text.Of($"options.tips.value.{s.Tips}"), Text.Of($"options.tips.help.{s.Tips}")),
        OptionRow.AreaChecklist => (Text.Of("options.areaChecklist.label"), OnOff(s.AreaChecklist), Text.Of(s.AreaChecklist ? "options.areaChecklist.help.on" : "options.areaChecklist.help.off")),
        OptionRow.BoxesAnywhere => (Text.Of("options.boxesAnywhere.label"), Text.Of($"options.boxesAnywhere.value.{s.BoxesAnywhere}"), Text.Of($"options.boxesAnywhere.help.{s.BoxesAnywhere}")),
        OptionRow.Autosave => (Text.Of("options.autosave.label"), OnOff(s.Autosave), Text.Of(s.Autosave ? "options.autosave.help.on" : "options.autosave.help.off")),
        OptionRow.DynamicResolution => (Text.Of("options.dynamicResolution.label"),
            DynamicResolutionUsable ? OnOff(s.DynamicResolution) : Text.Of("options.dynamicResolution.unusable"),
            Text.Of(!DynamicResolutionUsable ? "options.dynamicResolution.help.unusable" : s.DynamicResolution ? "options.dynamicResolution.help.on" : "options.dynamicResolution.help.off")),
        OptionRow.TimingAssist => (Text.Of("options.timingAssist.label"), Text.Of($"options.timingAssist.value.{s.TimingAssist}"), Text.Of($"options.timingAssist.help.{s.TimingAssist}")),
        OptionRow.Rumble => (Text.Of("options.rumble.label"), Text.Of($"options.rumble.value.{s.Rumble}"), Text.Of($"options.rumble.help.{s.Rumble}")),
        OptionRow.ReduceFlashes => (Text.Of("options.reduceFlashes.label"), OnOff(s.ReduceFlashes), Text.Of(s.ReduceFlashes ? "options.reduceFlashes.help.on" : "options.reduceFlashes.help.off")),
        OptionRow.ReduceMotion => (Text.Of("options.reduceMotion.label"), OnOff(s.ReduceMotion), Text.Of(s.ReduceMotion ? "options.reduceMotion.help.on" : "options.reduceMotion.help.off")),
        OptionRow.MedalNotices => (Text.Of("options.medalNotices.label"), OnOff(s.MedalNotices), Text.Of(s.MedalNotices ? "options.medalNotices.help.on" : "options.medalNotices.help.off")),
        OptionRow.RunTimer => (Text.Of("options.runTimer.label"), Text.Of($"options.runTimer.value.{s.RunTimer}"), Text.Of($"options.runTimer.help.{s.RunTimer}")),
        OptionRow.StreamFeed => (Text.Of("options.streamFeed.label"), Text.Of($"options.streamFeed.value.{s.StreamFeed}"), Text.Of($"options.streamFeed.help.{s.StreamFeed}")),
        OptionRow.StreamSafe => (Text.Of("options.streamSafe.label"), OnOff(s.StreamSafe), Text.Of(s.StreamSafe ? "options.streamSafe.help.on" : "options.streamSafe.help.off")),
        OptionRow.Coop => (Text.Of("options.coop.label"), OnOff(s.Coop), Text.Of(s.Coop ? "options.coop.help.on" : "options.coop.help.off")),
        OptionRow.BattleCamera => (Text.Of("options.battleCamera.label"), Text.Of($"options.battleCamera.value.{s.BattleCamera}"), Text.Of($"options.battleCamera.help.{s.BattleCamera}")),
        OptionRow.Captions => (Text.Of("options.captions.label"), OnOff(s.Captions), Text.Of(s.Captions ? "options.captions.help.on" : "options.captions.help.off")),
        OptionRow.HoldToConfirm => (Text.Of("options.holdToConfirm.label"), OnOff(s.HoldToConfirm), Text.Of(s.HoldToConfirm ? "options.holdToConfirm.help.on" : "options.holdToConfirm.help.off")),
        OptionRow.TextSize => (Text.Of("options.textSize.label"), Text.Of($"options.textSize.value.{s.TextSize}"), Text.Of($"options.textSize.help.{s.TextSize}")),
        OptionRow.ColourVision => (Text.Of("options.colourVision.label"), Text.Of($"options.colourVision.value.{s.ColourVision}"), Text.Of($"options.colourVision.help.{s.ColourVision}")),
        OptionRow.HighContrast => (Text.Of("options.highContrast.label"), OnOff(s.HighContrast), Text.Of(s.HighContrast ? "options.highContrast.help.on" : "options.highContrast.help.off")),
        OptionRow.Voices => (Text.Of("options.voices.label"), Text.Of($"options.voices.value.{s.Voices}"), Text.Of($"options.voices.help.{s.Voices}")),
        OptionRow.VoiceVolume => (Text.Of("options.voiceVolume.label"), Percent(s.VoiceVolume), Text.Of("options.voiceVolume.help")),
        OptionRow.LetVoicesFinish => (Text.Of("options.letVoicesFinish.label"), OnOff(s.LetVoicesFinish), Text.Of(s.LetVoicesFinish ? "options.letVoicesFinish.help.on" : "options.letVoicesFinish.help.off")),
        OptionRow.EnglishVoices => (Text.Of("options.englishVoices.label"), OnOff(s.EnglishVoices), Text.Of(s.EnglishVoices ? "options.englishVoices.help.on" : "options.englishVoices.help.off")),
        OptionRow.ResetDefaults => (Text.Of("options.resetDefaults.label"), Text.Of("options.resetDefaults.value"), Text.Of("options.resetDefaults.help")),
        OptionRow.Controls => (Text.Of("options.controls.label"), Text.Of("options.controls.value"), Text.Of("options.controls.help")),
        OptionRow.Jukebox => (Text.Of("options.jukebox.label"), Text.Of("options.jukebox.value"), Text.Of("options.jukebox.help")),
        OptionRow.Packs => (Text.Of("options.packs.label"), Text.Of("options.packs.value", ("n", Data.DataLayers.Packs.Count)), Text.Of("options.packs.help")),
        // HELP's line names the build too (plan 16 · T2), as the title's corner does
        _ => (Text.Of("options.help.label"), Text.Of("options.help.value"), Text.Of("options.help.help", ("version", Core.Diagnostics.GameVersion.Shown)))
    };

    private static UiIcon IconOf(OptionCategory c) => c switch
    {
        OptionCategory.Game => UiIcon.Star,
        OptionCategory.Field => UiIcon.MapCard,
        OptionCategory.Battle => UiIcon.Ball,
        OptionCategory.Display => UiIcon.Size,
        OptionCategory.Sound => UiIcon.Radio,
        OptionCategory.Controls => UiIcon.Gear,
        OptionCategory.Access => UiIcon.Trainer,
        _ => UiIcon.Boxes
    };

    private static Color ColourOf(OptionCategory c) => c switch
    {
        OptionCategory.Game => ModernUi.Gold,
        OptionCategory.Field => ModernUi.Green,
        OptionCategory.Battle => ModernUi.Red,
        OptionCategory.Display => ModernUi.Blue,
        OptionCategory.Sound => new Color(150, 96, 220, 255),
        OptionCategory.Controls => ModernUi.Track,
        OptionCategory.Access => new Color(16, 150, 160, 255),
        _ => ModernUi.Selection
    };

    private static string OnOff(bool on) => Text.Of(on ? "shared.on" : "shared.off");

    private static string Percent(int volume) => Text.Of("shared.percent", ("n", volume));

    public void Draw(int sw, int sh, GameSettings settings)
    {
        if (!IsActive) return;
        if (Help.IsActive)
        {
            Help.Draw(sw, sh);
            return;
        }
        if (Controls.IsActive)
        {
            Controls.Draw(sw, sh);
            return;
        }
        if (Packs.IsActive)
        {
            Packs.Draw(sw, sh);
            return;
        }
        ModernUi.Backdrop(sw, sh);
        ModernUi.ScreenTitle(Text.Of("options.title"));
        var hints = new List<HintItem>();
        if (Tabbed) hints.Add(ModernUi.Hint("options.keys.shoulders", "options.hint.tab"));
        hints.Add(Rows[SelectedIndex] is OptionRow.Help or OptionRow.Controls or OptionRow.Jukebox or OptionRow.Packs or OptionRow.ResetDefaults
            ? ModernUi.Hint("shared.keys.z", "shared.hints.open")
            : ModernUi.Hint("shared.keys.leftRight", "shared.hints.change"));
        hints.Add(ModernUi.Hint("shared.keys.esc", "shared.hints.back"));
        ModernUi.Hints(sw - 64, 44, hints.ToArray());
        if (Tabbed)
            ModernUi.Tabs(TabStrip, Categories.Select(c => (Text.Of($"options.category.{c}"), IconOf(c), ColourOf(c))).ToArray(), (int)Category);

        const float pitch = 86, height = 78;
        bool scrolls = Rows.Count > VisibleRows;
        float width = sw - 128 - (scrolls ? 36 : 0);
        for (int k = 0; k < Math.Min(VisibleRows, Rows.Count); k++)
        {
            int i = FirstRow + k;
            var (label, value, _) = Described(settings, Rows[i]);
            ModernUi.ValueRow(RowRect(k), label, value, i == SelectedIndex);
        }
        if (scrolls)
        {
            // Where the window is among all the rows: a thin track with a thumb at the right
            var track = new Rectangle(sw - 64 - 12, ListTop, 12, VisibleRows * pitch - (pitch - height));
            UiShapes.Fill(track, 6, ModernUi.Rule);
            float size = track.Height * VisibleRows / Rows.Count;
            float at = (track.Height - size) * FirstRow / (Rows.Count - VisibleRows);
            UiShapes.Fill(new Rectangle(track.X, track.Y + at, track.Width, size), 6, ModernUi.Frame);
        }

        float top = ListTop + VisibleRows * pitch + 6;
        var help = new Rectangle(64, top, sw - 128, ModernUi.ContentBottom - top);
        ModernUi.Panel(help, 30);
        ModernUi.DrawWrapped(Described(settings, Rows[SelectedIndex]).Help, help.X + 52, help.Y + (help.Height - 30) / 2f - 2, help.Width - 104, 30, ModernUi.Ink, 40);
        // The question over everything else (plan 12 · Q3): NO first, YES in Red since it throws the choices away
        if (resetReveal.Visible)
            ModernUi.Prompt(sw, sh, Text.Of("options.reset.heading"), Text.Of("options.reset.body"),
                new[] { (Text.Of("options.reset.no"), ModernUi.Blue), (Text.Of("options.reset.yes"), ModernUi.Red) }, ResetYes ? 1 : 0, resetReveal.Shown,
                ResetHold.Holding ? (1, ResetHold.Fill) : null);
    }
}
