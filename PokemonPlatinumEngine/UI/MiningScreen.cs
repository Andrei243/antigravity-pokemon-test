using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Underground;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the wall's game is at.</summary>
public enum MiningPhase
{
    /// <summary>"Something in the wall!": how many things it holds, and for a first wall a word of advice.</summary>
    Intro,
    Digging,
    /// <summary>Everything is out, or the wall has come down: a line says so.</summary>
    Ended,
    /// <summary>What was dug out, a line each.</summary>
    Finds,
    Done
}

/// <summary>
/// The wall's game on screen (plan 06 · R16a; <c>Mining_GameTask</c>): the wall with a cursor for the stylus, the
/// hammer and the pick, the crack that runs along its top as it weakens, and at the end what was dug out, put away
/// thing by thing. Its logic is <see cref="Move"/>, <see cref="PressConfirm"/>, <see cref="PressTool"/> and
/// <see cref="Advance"/>, which take no input; <see cref="MiningGame"/> holds the rules.
/// </summary>
public sealed class MiningScreen
{
    public bool IsActive { get; private set; }
    public MiningGame Game { get; private set; } = null!;
    public MiningPhase Phase { get; private set; }
    public int CursorX { get; private set; }
    public int CursorY { get; private set; }

    /// <summary>Seconds since the screen opened, since the phase began and since the last blow.</summary>
    public float Age { get; private set; }
    public float PhaseAge { get; private set; }
    public float SinceBlow { get; private set; } = 10f;
    public MiningBlow LastBlow { get; private set; }

    /// <summary>What the box at the foot says now.</summary>
    public string Line { get; private set; } = "";

    /// <summary>What was dug out whole, and how many of them found room in the bag.</summary>
    public IReadOnlyList<MiningFind> Finds => finds;
    public int Kept { get; private set; }

    /// <summary>Whether the first wall's advice is still to be read.</summary>
    public bool Lesson { get; private set; }

    private readonly List<MiningFind> finds = new();
    private readonly Queue<string> lines = new();
    private UndergroundState state = new();
    private int shown;

    public void Open(MiningGame game, UndergroundState into)
    {
        Game = game;
        state = into;
        IsActive = true;
        Phase = MiningPhase.Intro;
        CursorX = MiningGame.Width / 2;
        CursorY = MiningGame.Height / 2;
        Age = PhaseAge = 0f;
        SinceBlow = 10f;
        finds.Clear();
        lines.Clear();
        Kept = 0;
        shown = 0;
        Lesson = !into.HasMined;
        Line = Text.Of("digging.ringsHollow", ("n", game.ItemCount));
        AudioManager.PlaySound("poketch_beep");
    }

    public void Close() => IsActive = false;

    public void Move(int dx, int dy)
    {
        if (Phase != MiningPhase.Digging || dx == 0 && dy == 0) return;
        CursorX = Math.Clamp(CursorX + dx, 0, MiningGame.Width - 1);
        CursorY = Math.Clamp(CursorY + dy, 0, MiningGame.Height - 1);
    }

    /// <summary>The other tool is taken in hand (the original's two buttons beside the wall).</summary>
    public void PressTool()
    {
        if (Phase != MiningPhase.Digging) return;
        Game.PickSelected = !Game.PickSelected;
        AudioManager.PlaySound("select");
    }

    /// <summary>The A button: a blow at the square under the cursor, or the next line.</summary>
    public void PressConfirm()
    {
        switch (Phase)
        {
            case MiningPhase.Intro:
                if (Lesson)
                {
                    Lesson = false;
                    Line = Text.Of("digging.lesson");
                    break;
                }
                Phase = MiningPhase.Digging;
                PhaseAge = 0f;
                Line = "";
                break;
            case MiningPhase.Digging:
                Strike();
                break;
            case MiningPhase.Ended:
                if (PhaseAge < 0.6f) break;
                Phase = MiningPhase.Finds;
                PhaseAge = 0f;
                NextFind();
                break;
            case MiningPhase.Finds:
                NextFind();
                break;
        }
    }

    private void Strike()
    {
        LastBlow = Game.Strike(CursorX, CursorY);
        if (!LastBlow.Landed) return;
        SinceBlow = 0f;
        AudioManager.PlaySound(LastBlow.HitRock ? "bump" : Game.PickSelected ? "ledge" : "rock_smash");
        if (LastBlow.FoundItem) AudioManager.PlaySound("select");
        if (!Game.Over) return;

        // Mining_MainGameLoop: the first wall is behind the player either way
        state.HasMined = true;
        finds.AddRange(Game.Finds());
        Phase = MiningPhase.Ended;
        PhaseAge = 0f;
        if (Game.AllDug)
        {
            Line = Text.Of("digging.allDug");
            AudioManager.PlaySound("levelup");
        }
        else
        {
            Line = Text.Of("digging.cameDown");
            AudioManager.PlaySound("boulder");
        }
    }

    /// <summary>
    /// The next thing dug out goes into the bag, with its line (<c>Mining_PrintNextDugUpItem</c>,
    /// <c>Mining_ProcessNextDugUpItem</c>): it counts in the records whether or not the bag has room for it.
    /// </summary>
    private void NextFind()
    {
        if (lines.Count > 0)
        {
            Line = lines.Dequeue();
            return;
        }
        if (shown >= finds.Count)
        {
            Phase = MiningPhase.Done;
            IsActive = false;
            return;
        }
        var find = finds[shown++];
        var records = state.Records;
        bool kept;
        if (UndergroundDatabase.IsSphere(find.Id))
        {
            var kind = UndergroundDatabase.SphereOf(find.Id);
            Line = Text.Of("digging.sphereFound", ("sphere", UndergroundDatabase.SphereName(kind)), ("size", find.SphereSize));
            records.SpheresDug = UndergroundRecords.Add(records.SpheresDug, 1);
            kept = state.TryAddSphere(kind, find.SphereSize);
        }
        else
        {
            Line = Text.Of("digging.treasureFound", ("treasure", UndergroundDatabase.TreasureName(find.Id)));
            if (MiningGame.CountsAsFossil(find.Id)) records.FossilsDug = UndergroundRecords.Add(records.FossilsDug, 1);
            else records.TreasuresDug = UndergroundRecords.Add(records.TreasuresDug, 1);
            kept = state.TryAddTreasure(find.Id);
            if (kept) state.NotePlate(find.Id);
        }
        if (kept) Kept++;
        else lines.Enqueue(Text.Of("digging.noRoom"));
        AudioManager.PlaySound("select");
    }

    public void Advance(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        PhaseAge += dt;
        SinceBlow += dt;
    }

    private readonly Kit.HeldKey heldX = new(), heldY = new();

    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        int dx = heldX.Advance(dt, InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Left, GameAction.Right, held: true));
        int dy = heldY.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        if (dx != 0 || dy != 0) Move(dx, dy);
        if (InputManager.IsActionPressed(GameAction.Confirm)) PressConfirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) PressTool();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (IsActive) ModernUi.DrawMining(screenWidth, screenHeight, this);
    }
}
