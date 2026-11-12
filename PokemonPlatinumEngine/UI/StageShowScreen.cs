using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.StageShow;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

public enum StagePhase { ChooseShow, ChoosePokemon, Curtain, Show, Results }

/// <summary>
/// The stage (plan 42 · PS2; style guide, "Interface (vector)", "The stage show"): the choice of show and of the team's
/// performer, the curtain, the show itself (<see cref="ShowRun"/>, a press of Confirm a trick), and the results with each
/// one's share of the crowd's eyes. The four performers are rendered each frame into pictures of their own in
/// <see cref="Render"/>, before the 2D pass. Steps take no input of their own; <see cref="Outcome"/> is the player's
/// place (1 to 4), or 0 backed out of.
/// </summary>
public sealed class StageShowScreen
{
    public const float CurtainTime = 1.6f;
    public const int PictureSize = 400;

    public bool IsActive { get; private set; }
    public StagePhase Phase { get; private set; }
    public float Age { get; private set; }
    public float PhaseAge { get; private set; }

    public IReadOnlyList<StageShowData> Shows { get; private set; } = Array.Empty<StageShowData>();
    public IReadOnlyList<Pokemon> Team { get; private set; } = Array.Empty<Pokemon>();
    public int ShowCursor { get; private set; }
    public int TeamCursor { get; private set; }
    public ShowRecords? Records { get; private set; }
    public ShowRun? Run { get; private set; }
    public int Outcome { get; private set; }

    /// <summary>The fan who waits after the show and the piece they hold, once it is over.</summary>
    public (string Fan, string Prop)? Gift { get; private set; }

    private int seed;
    private TimingAssist assist;

    /// <summary>A tool's hand on the presses: the autopilot's when set, else the Confirm action's.</summary>
    public Func<ShowRun, bool>? Pilot { get; set; }

    public void Open(IEnumerable<StageShowData> shows, IEnumerable<Pokemon> team, ShowRecords records, int seed, TimingAssist assist)
    {
        Shows = shows.ToList();
        Team = team.Where(p => !p.IsEgg).ToList();
        Records = records;
        this.seed = seed;
        this.assist = assist;
        ShowCursor = TeamCursor = 0;
        Run = null;
        Gift = null;
        Outcome = 0;
        Age = PhaseAge = 0f;
        Phase = StagePhase.ChooseShow;
        IsActive = Shows.Count > 0 && Team.Count > 0;
        if (IsActive) AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    private void To(StagePhase phase)
    {
        Phase = phase;
        PhaseAge = 0f;
    }

    public void Move(int dy)
    {
        if (dy == 0) return;
        if (Phase == StagePhase.ChooseShow && Shows.Count > 1) ShowCursor = UiNav.Wrap(ShowCursor, Math.Sign(dy), Shows.Count);
        else if (Phase == StagePhase.ChoosePokemon && Team.Count > 1) TeamCursor = UiNav.Wrap(TeamCursor, Math.Sign(dy), Team.Count);
        else return;
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        switch (Phase)
        {
            case StagePhase.ChooseShow:
                To(StagePhase.ChoosePokemon);
                AudioManager.PlaySound("select");
                break;
            case StagePhase.ChoosePokemon:
                Begin(TeamCursor);
                break;
            case StagePhase.Results when PhaseAge > 0.6f:
                Close();
                break;
        }
    }

    /// <summary>The show under the cursor begun with the team's member at <paramref name="member"/> wearing its last dressing.</summary>
    public void Begin(int member)
    {
        var show = Shows[ShowCursor];
        var pokemon = Team[Math.Clamp(member, 0, Team.Count - 1)];
        var dressing = Records?.DressingOf(pokemon) ?? new Dressing();
        Run = new ShowRun(show, new Performer { Species = pokemon.LookName, Dressing = dressing }, seed, assist);
        To(StagePhase.Curtain);
        AudioManager.PlaySound("select");
    }

    public void Cancel()
    {
        if (Phase == StagePhase.ChoosePokemon) { To(StagePhase.ChooseShow); AudioManager.PlaySound("cancel"); }
        else if (Phase == StagePhase.ChooseShow) Close();
    }

    /// <summary>The time on, and the press: the curtain rises, the show steps by its own frames, and at its end the results.</summary>
    public void Advance(float dt, bool press)
    {
        if (!IsActive) return;
        Age += dt;
        PhaseAge += dt;
        switch (Phase)
        {
            case StagePhase.Curtain when PhaseAge >= CurtainTime:
                To(StagePhase.Show);
                if (MusicLibrary.Exists(Run!.Show.Song)) AudioManager.PlayMusic(Run.Show.Song);
                break;
            case StagePhase.Show:
            {
                var run = Run!;
                int tricks = run.Tricks.Count;
                run.Advance(dt, Pilot != null ? Pilot(run) : press);
                for (int k = tricks; k < run.Tricks.Count; k++)
                    AudioManager.PlaySound(run.Tricks[k].OnBeat ? "contest_appeal" : "contest_groan");
                if (run.Over)
                {
                    Outcome = run.PlayerPlace;
                    if (Records != null) Gift = ShowFans.Finish(Records, run);
                    To(StagePhase.Results);
                    AudioManager.PlaySound("contest_cheer");
                }
                break;
            }
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        if (Phase == StagePhase.Show || Phase == StagePhase.Curtain)
        {
            Advance(dt, InputManager.IsActionPressed(GameAction.Confirm));
            return;
        }
        Advance(dt, false);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else Move(InputManager.Axis(GameAction.Up, GameAction.Down));
    }

    // ------------------------------------------------------------------ the performers

    private readonly RenderTexture2D[] pictures = new RenderTexture2D[4];
    private bool picturesLoaded;
    private readonly bool[] drawn = new bool[4];

    /// <summary>Whether performer <paramref name="i"/>'s picture holds them this frame.</summary>
    public bool Drawn(int i) => drawn[i];
    internal Texture2D PictureOf(int i) => pictures[i].Texture;

    /// <summary>How long since performer <paramref name="i"/>'s last trick on the beat, in seconds (infinity for none).</summary>
    public float TrickAge(int i)
    {
        if (Run == null) return float.PositiveInfinity;
        for (int k = Run.Tricks.Count - 1; k >= 0; k--)
            if (Run.Tricks[k].Performer == i && Run.Tricks[k].OnBeat) return (Run.Frame - Run.Tricks[k].Frame) / (float)ShowRun.FramesPerSecond;
        return float.PositiveInfinity;
    }

    /// <summary>The four rendered with their props, each posed by the show's time: a cheer as a trick lands, idle between.</summary>
    internal void Render(RenderContext context)
    {
        Array.Clear(drawn);
        if (!IsActive || Run == null) return;
        if (!picturesLoaded)
        {
            for (int i = 0; i < 4; i++)
            {
                pictures[i] = Gpu.Target(PictureSize, PictureSize, "ui");
                Raylib.SetTextureFilter(pictures[i].Texture, TextureFilter.Bilinear);
            }
            picturesLoaded = true;
        }
        float time = Run.Frame / (float)ShowRun.FramesPerSecond;
        for (int i = 0; i < 4; i++)
        {
            var performer = Run.Performers[i];
            if (!PokemonModels.TryGet(performer.Species, out var model))
            {
                PokemonModels.Request(performer.Species);
                continue;
            }
            float since = TrickAge(i);
            float cheer = since < 0.6f ? since / 0.6f : 0f;
            var pose = new PokePose { Time = time + i * 0.37f, Cheer = cheer };
            PokemonSprites.Render(context, model, SpriteView.Front, pose, pictures[i], props: performer.Dressing.Props.ToList());
            drawn[i] = true;
        }
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawStageShow(screenWidth, screenHeight, this);
    }

    internal void Free()
    {
        if (picturesLoaded)
            for (int i = 0; i < 4; i++) Gpu.Unload(pictures[i]);
        picturesLoaded = false;
        ShowPropModels.Unload();
    }
}
