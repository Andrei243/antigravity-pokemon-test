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
    Controls,
    Jukebox,
    Packs,
    Help,
    ResetDefaults
}

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
    public IReadOnlyList<OptionRow> Rows { get; }

    public OptionsScreen(IReadOnlyList<OptionRow>? rows = null) => Rows = rows ?? AllRows;

    /// <summary>The rows the screen shows at once, above the help panel.</summary>
    public const int VisibleRows = 9;

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
        SelectedIndex = 0;
        FirstRow = 0;
        ResetAsking = false;
        Help.Close();
        Controls.Close();
        Packs.Close();
    }

    /// <summary>Moves the reset question's slide-in and slide-out along; call every frame the options are up.</summary>
    public void Animate(float dt) => resetReveal.Update(dt);

    /// <summary>The question that comes before every option is put back (plan 12 · Q3); it opens on NO.</summary>
    public void AskReset()
    {
        ResetAsking = true;
        ResetYes = false;
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
        resetReveal.Close();
        AudioManager.PlaySound(yes ? "select" : "cancel");
        if (!yes) return false;
        settings.ResetToDefaults();
        return true;
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
            if (InputManager.IsActionPressed(GameAction.Left) || InputManager.IsActionPressed(GameAction.Right)
                || InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Down))
            {
                ResetYes = !ResetYes;
                AudioManager.PlaySound("cursor");
            }
            else if (InputManager.IsActionPressed(GameAction.Cancel)) AnswerReset(settings, false);
            else if (InputManager.IsActionPressed(GameAction.Confirm)) return AnswerReset(settings, ResetYes);
            return false;
        }

        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
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
            case OptionRow.BoxesAnywhere:
                // Off, as the rules have it, On (plan 12 · Q13), and round again
                s.BoxesAnywhere = (RulesDefault)Wrap((int)s.BoxesAnywhere + step, 3);
                break;
            case OptionRow.PartyTools:
                // Off, as the rules have it, On (plan 12 · Q19), and round again
                s.PartyTools = (RulesDefault)Wrap((int)s.PartyTools + step, 3);
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
        OptionRow.BoxesAnywhere => (Text.Of("options.boxesAnywhere.label"), Text.Of($"options.boxesAnywhere.value.{s.BoxesAnywhere}"), Text.Of($"options.boxesAnywhere.help.{s.BoxesAnywhere}")),
        OptionRow.Autosave => (Text.Of("options.autosave.label"), OnOff(s.Autosave), Text.Of(s.Autosave ? "options.autosave.help.on" : "options.autosave.help.off")),
        OptionRow.DynamicResolution => (Text.Of("options.dynamicResolution.label"),
            DynamicResolutionUsable ? OnOff(s.DynamicResolution) : Text.Of("options.dynamicResolution.unusable"),
            Text.Of(!DynamicResolutionUsable ? "options.dynamicResolution.help.unusable" : s.DynamicResolution ? "options.dynamicResolution.help.on" : "options.dynamicResolution.help.off")),
        OptionRow.TimingAssist => (Text.Of("options.timingAssist.label"), Text.Of($"options.timingAssist.value.{s.TimingAssist}"), Text.Of($"options.timingAssist.help.{s.TimingAssist}")),
        OptionRow.Rumble => (Text.Of("options.rumble.label"), Text.Of($"options.rumble.value.{s.Rumble}"), Text.Of($"options.rumble.help.{s.Rumble}")),
        OptionRow.ResetDefaults => (Text.Of("options.resetDefaults.label"), Text.Of("options.resetDefaults.value"), Text.Of("options.resetDefaults.help")),
        OptionRow.Controls => (Text.Of("options.controls.label"), Text.Of("options.controls.value"), Text.Of("options.controls.help")),
        OptionRow.Jukebox => (Text.Of("options.jukebox.label"), Text.Of("options.jukebox.value"), Text.Of("options.jukebox.help")),
        OptionRow.Packs => (Text.Of("options.packs.label"), Text.Of("options.packs.value", ("n", Data.DataLayers.Packs.Count)), Text.Of("options.packs.help")),
        // HELP's line names the build too (plan 16 · T2), as the title's corner does
        _ => (Text.Of("options.help.label"), Text.Of("options.help.value"), Text.Of("options.help.help", ("version", Core.Diagnostics.GameVersion.Shown)))
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
        if (Rows[SelectedIndex] is OptionRow.Help or OptionRow.Controls or OptionRow.Jukebox or OptionRow.Packs or OptionRow.ResetDefaults) ModernUi.Hints(sw - 64, 44, ModernUi.Hint("shared.keys.z", "shared.hints.open"), ModernUi.Hint("shared.keys.esc", "shared.hints.back"));
        else ModernUi.Hints(sw - 64, 44, ModernUi.Hint("shared.keys.leftRight", "shared.hints.change"), ModernUi.Hint("shared.keys.esc", "shared.hints.back"));

        const float pitch = 86, height = 78;
        bool scrolls = Rows.Count > VisibleRows;
        float width = sw - 128 - (scrolls ? 36 : 0);
        for (int k = 0; k < Math.Min(VisibleRows, Rows.Count); k++)
        {
            int i = FirstRow + k;
            var (label, value, _) = Described(settings, Rows[i]);
            ModernUi.ValueRow(new Rectangle(64, ModernUi.ContentTop + k * pitch, width, height), label, value, i == SelectedIndex);
        }
        if (scrolls)
        {
            // Where the window is among all the rows: a thin track with a thumb at the right
            var track = new Rectangle(sw - 64 - 12, ModernUi.ContentTop, 12, VisibleRows * pitch - (pitch - height));
            UiShapes.Fill(track, 6, ModernUi.Rule);
            float size = track.Height * VisibleRows / Rows.Count;
            float at = (track.Height - size) * FirstRow / (Rows.Count - VisibleRows);
            UiShapes.Fill(new Rectangle(track.X, track.Y + at, track.Width, size), 6, ModernUi.Frame);
        }

        float top = ModernUi.ContentTop + Math.Min(VisibleRows, Rows.Count) * pitch + 6;
        var help = new Rectangle(64, top, sw - 128, ModernUi.ContentBottom - top);
        ModernUi.Panel(help, 30);
        ModernUi.DrawWrapped(Described(settings, Rows[SelectedIndex]).Help, help.X + 52, help.Y + (help.Height - 30) / 2f - 2, help.Width - 104, 30, ModernUi.Ink, 40);
        // The question over everything else (plan 12 · Q3): NO first, YES in Red since it throws the choices away
        if (resetReveal.Visible)
            ModernUi.Prompt(sw, sh, Text.Of("options.reset.heading"), Text.Of("options.reset.body"),
                new[] { (Text.Of("options.reset.no"), ModernUi.Blue), (Text.Of("options.reset.yes"), ModernUi.Red) }, ResetYes ? 1 : 0, resetReveal.Shown);
    }
}
