using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// A preset chosen by measuring, and dynamic resolution (plan 16 · T23): the probe's choice on fed timings, the measuring
/// frame by frame, the step dynamic resolution takes on fed series, FXAA asked by the part's width, the options' row and
/// the two fields the options file keeps. No window: what renders is the harness's (`look_19*`).
/// </summary>
[Collection("Shared clocks and dice")]
public class ResolutionTests
{
    private static IReadOnlyDictionary<GraphicsQuality, IReadOnlyList<double>> Times(params (GraphicsQuality Quality, double[] Ms)[] presets) =>
        presets.ToDictionary(p => p.Quality, p => (IReadOnlyList<double>)p.Ms);

    private static double[] Steady(double ms, int count = QualityProbe.TimedFrames) => Enumerable.Repeat(ms, count).ToArray();

    // ------------------------------------------------------------------ the probe

    [Fact]
    public void AFastCardKeepsHighWithoutDynamicResolution()
    {
        var choice = QualityProbe.Choose(Times((GraphicsQuality.High, Steady(4.2))));
        Assert.Equal((GraphicsQuality.High, false), (choice.Quality, choice.DynamicResolution));
        Assert.Equal(4.2, choice.Medians[GraphicsQuality.High], 6);
    }

    [Fact]
    public void ASlowCardStartsOnLowWithDynamicResolution()
    {
        var choice = QualityProbe.Choose(Times((GraphicsQuality.High, Steady(30)), (GraphicsQuality.Medium, Steady(18)), (GraphicsQuality.Low, Steady(9))));
        // Plan 16's decision 12: only when even Low misses the budget does dynamic resolution come on
        Assert.Equal((GraphicsQuality.Low, true), (choice.Quality, choice.DynamicResolution));
    }

    [Fact]
    public void ACardBetweenGetsTheHighestThatFits()
    {
        var choice = QualityProbe.Choose(Times((GraphicsQuality.High, Steady(9.1)), (GraphicsQuality.Medium, Steady(5.8))));
        Assert.Equal((GraphicsQuality.Medium, false), (choice.Quality, choice.DynamicResolution));
        // ... and Low that fits too is no reason to go lower
        choice = QualityProbe.Choose(Times((GraphicsQuality.High, Steady(9.1)), (GraphicsQuality.Medium, Steady(5.8)), (GraphicsQuality.Low, Steady(2))));
        Assert.Equal(GraphicsQuality.Medium, choice.Quality);
        // A median exactly at the budget misses it
        Assert.Equal(GraphicsQuality.Low, QualityProbe.Choose(Times((GraphicsQuality.High, Steady(QualityProbe.Budget)), (GraphicsQuality.Low, Steady(3)))).Quality);
    }

    [Fact]
    public void NoisySamplesAreJudgedByTheirMedian()
    {
        // A third of High's frames held up by the desktop's compositor: the middle of them still fits
        var noisy = Enumerable.Range(0, QualityProbe.TimedFrames).Select(i => i % 3 == 0 ? 14.0 : 5.5).ToArray();
        Assert.Equal(GraphicsQuality.High, QualityProbe.Choose(Times((GraphicsQuality.High, noisy))).Quality);
        // ... and one too many puts it over
        var worse = Enumerable.Range(0, QualityProbe.TimedFrames).Select(i => i % 2 == 0 ? 14.0 : 5.5).ToArray();
        Assert.NotEqual(GraphicsQuality.High, QualityProbe.Choose(Times((GraphicsQuality.High, worse))).Quality);
        Assert.Equal(3.5, QualityProbe.Median(new[] { 5.0, 2.0, 3.0, 4.0 }));
        Assert.Equal(0, QualityProbe.Median(Array.Empty<double>()));
    }

    [Fact]
    public void TheMeasuringGoesDownFromHighAndStopsAtTheFirstThatFits()
    {
        var m = new QualityProbe.Measurement();
        Assert.Equal(GraphicsQuality.High, m.Current);
        // The settling frames count for nothing, however long (a preset's targets being made)
        for (int i = 0; i < QualityProbe.SettleFrames; i++)
        {
            Assert.False(m.Timing);
            m.Record(200);
        }
        Assert.True(m.Timing);
        for (int i = 0; i < QualityProbe.TimedFrames; i++) m.Record(9);
        Assert.False(m.Done);
        Assert.Equal((GraphicsQuality.Medium, 0), (m.Current, m.Frame));
        for (int i = 0; i < QualityProbe.SettleFrames + QualityProbe.TimedFrames; i++) m.Record(i < QualityProbe.SettleFrames ? 100 : 5);
        Assert.True(m.Done);
        Assert.Equal((GraphicsQuality.Medium, false), (m.Result!.Quality, m.Result.DynamicResolution));
        // Low was never rendered
        Assert.False(m.Timings.ContainsKey(GraphicsQuality.Low));
        // Nothing more is taken once done
        m.Record(1);
        Assert.Equal(QualityProbe.TimedFrames, m.Timings[GraphicsQuality.Medium].Count);
    }

    [Fact]
    public void TheMeasuringEndsOnLowWhenNothingFits()
    {
        var m = new QualityProbe.Measurement();
        while (!m.Done) m.Record(20);
        Assert.Equal((GraphicsQuality.Low, true), (m.Result!.Quality, m.Result.DynamicResolution));
        Assert.Equal(3, m.Timings.Count);
        // Stopped early, it decides on what it has
        var stopped = new QualityProbe.Measurement();
        stopped.Stop();
        Assert.Equal(GraphicsQuality.Low, stopped.Result!.Quality);
    }

    // ------------------------------------------------------------------ dynamic resolution

    // Samples four a second (one frame in fifteen at sixty), each a load in milliseconds
    private static int Run(ResolutionScale scale, IEnumerable<double> loads, ref double now, int cap = 60, List<(double At, int Step)>? steps = null)
    {
        foreach (double load in loads)
        {
            now += 0.25;
            if (scale.Feed(load, now, cap)) steps?.Add((now, scale.Step));
        }
        return scale.Step;
    }

    [Fact]
    public void ASteadyLoadStaysPut()
    {
        var scale = new ResolutionScale();
        double now = 0;
        // 10 ms of work in a 16.7 ms frame: under the frame less a quarter (12.5 ms), over three fifths of it (10 ms)
        Assert.Equal(12.5, ResolutionScale.DownOver(60), 6);
        Assert.Equal(0, Run(scale, Enumerable.Repeat(11.0, 200), ref now));
        Assert.Equal(1f, scale.Scale);
    }

    [Fact]
    public void OneLongFrameMovesNothing()
    {
        var scale = new ResolutionScale();
        double now = 0;
        var loads = Enumerable.Repeat(11.0, 30).Concat(new[] { 80.0 }).Concat(Enumerable.Repeat(11.0, 30));
        Assert.Equal(0, Run(scale, loads, ref now));
    }

    [Fact]
    public void AClimbingLoadStepsDownAStepAtATimeAndComesBackSlowly()
    {
        var scale = new ResolutionScale();
        double now = 0;
        var steps = new List<(double At, int Step)>();
        // A load far over the frame: down it goes, a step at a time, never two within a second, to half and no further
        Run(scale, Enumerable.Repeat(30.0, 400), ref now, steps: steps);
        Assert.Equal(ResolutionScale.Steps.Count - 1, scale.Step);
        Assert.Equal(0.5f, scale.Scale);
        Assert.Equal(Enumerable.Range(1, ResolutionScale.Steps.Count - 1), steps.Select(s => s.Step));
        for (int i = 1; i < steps.Count; i++) Assert.True(steps[i].At - steps[i - 1].At >= ResolutionScale.StepGap);

        // The load gone: a step up only after three seconds of calm, and three more for the next
        steps.Clear();
        double calmFrom = now;
        Run(scale, Enumerable.Repeat(4.0, 11), ref now, steps: steps);
        Assert.Empty(steps);
        Run(scale, Enumerable.Repeat(4.0, 400), ref now, steps: steps);
        Assert.Equal(0, scale.Step);
        Assert.True(steps[0].At - calmFrom >= ResolutionScale.CalmSeconds);
        for (int i = 1; i < steps.Count; i++) Assert.True(steps[i].At - steps[i - 1].At >= ResolutionScale.CalmSeconds);
    }

    [Fact]
    public void ALoadBetweenTheTwoLinesHoldsItsStep()
    {
        var scale = new ResolutionScale();
        double now = 0;
        Run(scale, Enumerable.Repeat(14.0, 12), ref now);
        Assert.Equal(1, scale.Step);
        // Neither over 12.5 ms nor under 10 ms: no step either way
        Assert.Equal(1, Run(scale, Enumerable.Repeat(11.0, 200), ref now));
        scale.Reset();
        Assert.Equal(0, scale.Step);
        // Under a cap of thirty the frame is twice as long, and 14 ms is calm
        Assert.Equal(0, Run(scale, Enumerable.Repeat(14.0, 200), ref now, cap: 30));
        // No cap counts as sixty
        Assert.Equal(ResolutionScale.FrameOf(60), ResolutionScale.FrameOf(0));
    }

    [Fact]
    public void FxaaIsAskedByThePartsWidth()
    {
        var high = QualityProfile.For(GraphicsQuality.High);
        // The whole 4K scene in a 1080p window needs none; drawn at half (1920 across) it does
        Assert.False(RenderContext.NeedsFxaa(high, 3840, 1920));
        Assert.True(RenderContext.NeedsFxaa(high, 1920, 1920));
        Assert.False(RenderContext.NeedsFxaa(high, 3456, 1920));
    }

    // ------------------------------------------------------------------ the options

    [Fact]
    public void TheRowDefaultsOffCyclesAndSaysWhereItCantBeUsed()
    {
        var s = new GameSettings();
        Assert.False(s.DynamicResolution);
        var rows = Enum.GetValues<OptionRow>();
        Assert.True(Array.IndexOf(rows, OptionRow.DynamicResolution) < Array.IndexOf(rows, OptionRow.Help));
        Assert.Equal(Text.Of("options.dynamicResolution.help.off"), OptionsScreen.Describe(s, OptionRow.DynamicResolution).Help);
        OptionsScreen.Change(s, OptionRow.DynamicResolution, 1);
        Assert.True(s.DynamicResolution);
        Assert.Equal(Text.Of("options.dynamicResolution.help.on"), OptionsScreen.Describe(s, OptionRow.DynamicResolution).Help);
        OptionsScreen.Change(s, OptionRow.DynamicResolution, -1);
        Assert.False(s.DynamicResolution);

        bool was = OptionsScreen.DynamicResolutionUsable;
        try
        {
            OptionsScreen.DynamicResolutionUsable = false;
            OptionsScreen.Change(s, OptionRow.DynamicResolution, 1);
            Assert.False(s.DynamicResolution);
            var (_, value, help) = OptionsScreen.Describe(s, OptionRow.DynamicResolution);
            Assert.Equal((Text.Of("options.dynamicResolution.unusable"), Text.Of("options.dynamicResolution.help.unusable")), (value, help));
        }
        finally
        {
            OptionsScreen.DynamicResolutionUsable = was;
        }
    }

    [Fact]
    public void TheQualityRowTellsWhatWasMeasured()
    {
        var s = new GameSettings { Quality = GraphicsQuality.Medium };
        Assert.Equal(Text.Of("options.quality.help.Medium"), OptionsScreen.Describe(s, OptionRow.Quality).Help);
        s.MeasuredQuality = GraphicsQuality.Medium;
        s.MeasuredMilliseconds = 5.84;
        string help = OptionsScreen.Describe(s, OptionRow.Quality).Help;
        Assert.StartsWith(Text.Of("options.quality.help.Medium"), help);
        Assert.Contains("5.8", help);
        Assert.Contains(Text.Of("options.quality.value.Medium"), help);
    }

    [Fact]
    public void TheOptionsFileKeepsTheNewFieldsAndTheResetKeepsWhatWasMeasured()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pp-resolution-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(folder, "settings.json");
            var s = new GameSettings { DynamicResolution = true, MeasuredQuality = GraphicsQuality.Low, MeasuredMilliseconds = 7.2, Quality = GraphicsQuality.Low };
            s.Save(path);
            var read = GameSettings.Load(path);
            Assert.Equal((true, GraphicsQuality.Low, 7.2), (read.DynamicResolution, read.MeasuredQuality!.Value, read.MeasuredMilliseconds!.Value));
            Assert.False(read.FirstRun);

            // A file from before the rows: nothing measured, nothing scaled
            File.WriteAllText(path, "{ \"Quality\": \"Medium\" }");
            var old = GameSettings.Load(path);
            Assert.Equal((false, (GraphicsQuality?)null), (old.DynamicResolution, old.MeasuredQuality));

            read.ResetToDefaults();
            Assert.False(read.DynamicResolution);
            Assert.Equal(GraphicsQuality.High, read.Quality);
            Assert.Equal(GraphicsQuality.Low, read.MeasuredQuality);
            // No file is a first run, which is what measures
            Assert.True(GameSettings.Load(Path.Combine(folder, "none.json")).FirstRun);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void TheNoticeHeldStaysWhateverTimeAndButtonsSay()
    {
        var title = new TitleScreen(null);
        title.HoldNotice = true;
        title.Advance(TitleScreen.NoticeTime * 3);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Notice, title.Phase);
        title.HoldNotice = false;
        title.Advance(0.01f);
        Assert.Equal(TitlePhase.Journey, title.Phase);
    }
}
