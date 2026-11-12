using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The studio's rows (plan 43 · LF5), in their order down the panel.</summary>
public enum StudioRow { Backdrop, Pose, Face, Pokemon, Place, Stickers, Frame, Take }

/// <summary>What the studio is doing: its rows, choosing a Seal from the case, or placing it on the stage.</summary>
public enum StudioMode { Rows, PickSeal, PlaceSeal }

/// <summary>
/// The photo studio (plan 43 · LF5; style guide, "The photo studio"): the stage with the player, a Pokémon of the team
/// and stickers of Seals the player owns, and rows BACKDROP, POSE, FACE, POKéMON, PLACE, STICKERS, FRAME and TAKE.
/// Left and right change a row; A on STICKERS opens the case's Seals, then a Seal is carried over the stage with the
/// arrows and put down with A (a Seal is not used up); left on STICKERS takes the last one off. TAKE asks the game for
/// the picture (<see cref="TakeTakeRequest"/>) and the print is kept in the photo book with its composition. The logic
/// takes no input, so tests and the harness drive it.
/// </summary>
public sealed class StudioScreen
{
    /// <summary>How far a carried Seal moves at a press, as a share of the picture.</summary>
    public const float CursorStep = 0.025f;

    /// <summary>How many Seals of the list show at once.</summary>
    public const int VisibleSeals = 10;

    private readonly List<string> backdrops = new(), frames = new();
    private readonly List<CardCandidate> candidates = new();
    private readonly List<int> seals = new();
    private SealCase? sealCase;
    private bool takeWanted;

    public bool IsActive { get; private set; }
    public StudioShot Shot { get; private set; } = new();
    public StudioMode Mode { get; private set; }
    public StudioRow Row { get; private set; }

    /// <summary>True while the picture itself is drawn alone, for the game to take (no panel over it).</summary>
    public bool Taking { get; set; }

    public float Time { get; private set; }

    public IReadOnlyList<string> Backdrops => backdrops;
    public IReadOnlyList<string> Frames => frames;
    public IReadOnlyList<CardCandidate> Candidates => candidates;

    /// <summary>The Seals of the case, in the case's order, the carried Seal's list.</summary>
    public IReadOnlyList<int> Seals => seals;

    public int SealIndex { get; private set; }
    public int FirstSeal { get; private set; }

    /// <summary>The carried Seal's place on the stage, as shares.</summary>
    public float CursorX { get; private set; } = 0.5f;
    public float CursorY { get; private set; } = 0.5f;

    /// <summary>The words of the last thing refused (a Seal not owned, a full picture), until the next press.</summary>
    public string? Refusal { get; private set; }

    /// <summary>The Pokémon chosen of the team; null for none.</summary>
    public CardCandidate? Chosen => candidates.FirstOrDefault(c => c.Id == Shot.PokemonId);

    public void Open(StudioShot? last, IEnumerable<string> backdropsOpen, IEnumerable<string> framesOpen, IEnumerable<CardCandidate> team, SealCase ownedSeals)
    {
        sealCase = ownedSeals;
        backdrops.Clear();
        backdrops.AddRange(backdropsOpen);
        frames.Clear();
        frames.AddRange(framesOpen);
        candidates.Clear();
        candidates.AddRange(team.Where(c => StudioShot.MayStand(c.Pokemon)));
        seals.Clear();
        seals.AddRange(ownedSeals.Owned);
        var shot = (last ?? new StudioShot()).Kept(ownedSeals);
        // A look or a Pokémon no longer open (a save moved between games, a Pokémon released) falls back
        if (backdrops.Count > 0 && !backdrops.Contains(shot.Backdrop)) shot = shot with { Backdrop = backdrops[0] };
        if (frames.Count > 0 && !frames.Contains(shot.Frame)) shot = shot with { Frame = frames[0] };
        if (shot.PokemonId != Guid.Empty && candidates.All(c => c.Id != shot.PokemonId)) shot = shot with { PokemonId = Guid.Empty };
        Shot = shot;
        Mode = StudioMode.Rows;
        Row = StudioRow.Backdrop;
        SealIndex = FirstSeal = 0;
        (CursorX, CursorY) = (0.5f, 0.5f);
        Refusal = null;
        takeWanted = Taking = false;
        Time = 0f;
        IsActive = true;
        AudioManager.PlaySound("select");
    }

    public void Close() => IsActive = false;

    public void Move(int dy)
    {
        if (dy == 0) return;
        Refusal = null;
        switch (Mode)
        {
            case StudioMode.Rows:
            {
                int count = Enum.GetValues<StudioRow>().Length;
                Row = (StudioRow)UiNav.Wrap((int)Row, dy, count);
                AudioManager.PlaySound("cursor");
                break;
            }
            case StudioMode.PickSeal:
                if (seals.Count == 0) return;
                SealIndex = UiNav.Wrap(SealIndex, dy, seals.Count);
                FirstSeal = UiNav.Window(FirstSeal, SealIndex, seals.Count, VisibleSeals);
                AudioManager.PlaySound("cursor");
                break;
            case StudioMode.PlaceSeal:
                CursorY = Math.Clamp(CursorY + dy * CursorStep, 0f, 1f);
                break;
        }
    }

    public void Change(int dx)
    {
        if (dx == 0) return;
        Refusal = null;
        if (Mode == StudioMode.PlaceSeal) { CursorX = Math.Clamp(CursorX + dx * CursorStep, 0f, 1f); return; }
        if (Mode != StudioMode.Rows) return;
        int step = Math.Sign(dx);
        switch (Row)
        {
            case StudioRow.Backdrop when backdrops.Count > 1:
                Shot = Shot with { Backdrop = backdrops[UiNav.Wrap(Math.Max(0, backdrops.IndexOf(Shot.Backdrop)), step, backdrops.Count)] };
                break;
            case StudioRow.Pose:
                Shot = Shot.WithPose(Shot.PoseIndex + step);
                break;
            case StudioRow.Face:
                Shot = Shot with { Mood = (Mood)UiNav.Wrap((int)Shot.Mood, step, Enum.GetValues<Mood>().Length) };
                break;
            case StudioRow.Pokemon when candidates.Count > 0:
            {
                // None, then each Pokémon of the team
                int at = Shot.PokemonId == Guid.Empty ? 0 : candidates.FindIndex(c => c.Id == Shot.PokemonId) + 1;
                at = UiNav.Wrap(at, step, candidates.Count + 1);
                Shot = Shot with { PokemonId = at == 0 ? Guid.Empty : candidates[at - 1].Id };
                break;
            }
            case StudioRow.Place:
                Shot = Shot with { Place = Shot.Place == StudioPlace.Right ? StudioPlace.Left : StudioPlace.Right };
                break;
            case StudioRow.Stickers when step < 0 && Shot.Stickers.Count > 0:
                Shot = Shot.WithoutSticker(Shot.Stickers.Count - 1);
                break;
            case StudioRow.Frame when frames.Count > 1:
                Shot = Shot with { Frame = frames[UiNav.Wrap(Math.Max(0, frames.IndexOf(Shot.Frame)), step, frames.Count)] };
                break;
            default:
                return;
        }
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        Refusal = null;
        switch (Mode)
        {
            case StudioMode.PickSeal:
                if (seals.Count == 0) return;
                Mode = StudioMode.PlaceSeal;
                (CursorX, CursorY) = (0.5f, 0.5f);
                AudioManager.PlaySound("select");
                return;
            case StudioMode.PlaceSeal:
                Place();
                return;
        }
        switch (Row)
        {
            case StudioRow.Stickers:
                if (seals.Count == 0) { Refusal = "noSeals"; AudioManager.PlaySound("cancel"); return; }
                Mode = StudioMode.PickSeal;
                SealIndex = FirstSeal = 0;
                AudioManager.PlaySound("select");
                break;
            case StudioRow.Take:
                takeWanted = true;
                AudioManager.PlaySound("select");
                break;
        }
    }

    /// <summary>The carried Seal put on the stage where the cursor is; refused where the case doesn't hold it or the picture is full.</summary>
    public void Place()
    {
        if (sealCase == null || SealIndex >= seals.Count) return;
        int seal = seals[SealIndex];
        string? why = Shot.WhyNotSticker(seal, CursorX, CursorY, sealCase);
        if (why != null)
        {
            Refusal = why;
            AudioManager.PlaySound("cancel");
            return;
        }
        Shot = Shot.WithSticker(seal, CursorX, CursorY, sealCase);
        AudioManager.PlaySound("select");
    }

    public void Cancel()
    {
        Refusal = null;
        if (Mode == StudioMode.PlaceSeal) Mode = StudioMode.PickSeal;
        else if (Mode == StudioMode.PickSeal) Mode = StudioMode.Rows;
        else Close();
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The TAKE row pressed since last asked, once: the game draws the picture alone and keeps it.</summary>
    public bool TakeTakeRequest()
    {
        bool wanted = takeWanted;
        takeWanted = false;
        return wanted;
    }

    // ------------------------------------------------------------------ the pointer

    /// <summary>The steps a point of the stage is told in, across and down, for the pointer (<see cref="HitTest"/>).</summary>
    public const int StageSteps = 1000;

    /// <summary>
    /// What the pointer is over (plan 43 · LF5's leftover): a row (its number) while the rows are chosen, a Seal of the
    /// case (100 and on) while one is picked, or a point of the stage (10000 and on, row by row in
    /// <see cref="StageSteps"/>) while one is carried; null for nothing. From the rectangles the drawing uses.
    /// </summary>
    public int? HitTest(System.Numerics.Vector2 at)
    {
        if (!IsActive || Taking) return null;
        switch (Mode)
        {
            case StudioMode.PlaceSeal:
            {
                var stage = ModernUi.StudioStage;
                if (at.X < stage.X || at.Y < stage.Y || at.X > stage.X + stage.Width || at.Y > stage.Y + stage.Height) return null;
                int cx = (int)MathF.Round((at.X - stage.X) / stage.Width * StageSteps), cy = (int)MathF.Round((at.Y - stage.Y) / stage.Height * StageSteps);
                return 10000 + Math.Clamp(cy, 0, StageSteps) * (StageSteps + 1) + Math.Clamp(cx, 0, StageSteps);
            }
            case StudioMode.PickSeal:
            {
                int shown = Math.Min(VisibleSeals, seals.Count - FirstSeal);
                for (int i = 0; i < shown; i++)
                    if (System.Numerics.Vector2.Distance(at, ModernUi.StudioSealSpot(i)) <= ModernUi.StudioSealRadius) return 100 + FirstSeal + i;
                return null;
            }
            default:
                foreach (var row in Enum.GetValues<StudioRow>())
                {
                    var r = ModernUi.StudioRowRect(GameEngine.VirtualWidth, row);
                    if (at.X >= r.X && at.Y >= r.Y && at.X <= r.X + r.Width && at.Y <= r.Y + r.Height) return (int)row;
                }
                return null;
        }
    }

    /// <summary>The pointer over something: the cursor to that row or Seal, or the carried Seal to that point of the stage.</summary>
    public void PointAt(int index)
    {
        if (!IsActive) return;
        if (index >= 10000 && Mode == StudioMode.PlaceSeal)
        {
            int cell = index - 10000;
            CursorX = (cell % (StageSteps + 1)) / (float)StageSteps;
            CursorY = (cell / (StageSteps + 1)) / (float)StageSteps;
        }
        else if (index >= 100 && index < 10000 && Mode == StudioMode.PickSeal && seals.Count > 0)
        {
            SealIndex = Math.Clamp(index - 100, 0, seals.Count - 1);
            FirstSeal = UiNav.Window(FirstSeal, SealIndex, seals.Count, VisibleSeals);
        }
        else if (index >= 0 && index < Enum.GetValues<StudioRow>().Length && Mode == StudioMode.Rows) Row = (StudioRow)index;
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        Time += dt;
        int dx = InputManager.IsActionPressed(GameAction.Left) ? -1 : InputManager.IsActionPressed(GameAction.Right) ? 1 : 0;
        if (Mode == StudioMode.PlaceSeal)
        {
            dx = InputManager.IsActionDown(GameAction.Left) ? -1 : InputManager.IsActionDown(GameAction.Right) ? 1 : 0;
            int held = InputManager.IsActionDown(GameAction.Up) ? -1 : InputManager.IsActionDown(GameAction.Down) ? 1 : 0;
            Change(dx);
            Move(held);
        }
        else
        {
            int dy = InputManager.IsActionPressed(GameAction.Up) ? -1 : InputManager.IsActionPressed(GameAction.Down) ? 1 : 0;
            Change(dx);
            Move(dy);
        }
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
    }
}
