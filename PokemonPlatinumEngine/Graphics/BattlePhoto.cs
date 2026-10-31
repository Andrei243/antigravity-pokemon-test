using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>The rows of photo mode's panel in battle, in their order.</summary>
public enum BattlePhotoRow { Zoom, Time, Pose, Filter, Frame }

/// <summary>
/// Photo mode in battle (plan 12 · Q17; style guide, "Camera (G8)", "A held shot"): the shot the camera director had
/// on screen as the photo began, turned round the point it looked at, brought closer or further, and the hour, the
/// pose's step, the filter and the frame the picture borrows. GPU-free and changes nothing of the battle: the renderer
/// holds the director on <see cref="Shot"/> and reads <see cref="Hour"/>, <see cref="PoseStep"/> and
/// <see cref="Filter"/> for the picture alone.
/// </summary>
internal sealed class BattlePhotoView
{
    /// <summary>How fast the arrows turn the camera, radians a second: round the vertical, and up or down.</summary>
    public const float TurnSpeed = 1.2f, LiftSpeed = 0.6f;

    /// <summary>The pitch's bounds, radians (2° to 70° looking down), and the lowest the camera stands over the ground.</summary>
    public const float LowestPitch = 2f * MathF.PI / 180f, HighestPitch = 70f * MathF.PI / 180f, LowestHeight = 0.4f;

    /// <summary>How much closer than it began the camera may come (<see cref="Zoom"/>), and its step.</summary>
    public const float WidestZoom = 0.6f, TightestZoom = 2.5f, ZoomStep = 1.25f;

    /// <summary>A step of the pose, seconds, and how many there are before it comes round again.</summary>
    public const float PoseTick = 0.1f;
    public const int PoseSteps = 30;

    /// <summary>The shot the director had on screen as the photo began.</summary>
    public CameraShot Start { get; }

    /// <summary>
    /// The field's middle, a unit over the ground between the two platforms. A shot that looks near it (the overview,
    /// whose target lies 20 units ahead and under the ground) is turned round the point of its line nearest the middle;
    /// a shot of a Pokémon or a trainer, round what it looks at.
    /// </summary>
    public static readonly Vector3 FieldMiddle = (BattleStage.EnemySpot + BattleStage.PlayerSpot) / 2f + Vector3.UnitY;

    /// <summary>How near the field's middle a shot's target must be for the shot to be turned round the middle instead.</summary>
    public const float NearTheMiddle = 4f;

    /// <summary>The point the camera turns round and looks at.</summary>
    public Vector3 Pivot { get; }

    public BattlePhotoView(CameraShot start)
    {
        Start = start;
        Pivot = PivotOf(start);
        var arm = start.Position - Pivot;
        float length = arm.Length();
        Distance = length < 0.01f ? 1f : length;
        BaseYaw = MathF.Atan2(arm.X, arm.Z);
        BasePitch = length < 0.01f ? 0.2f : MathF.Asin(Math.Clamp(arm.Y / Distance, -1f, 1f));
    }

    /// <summary>What a shot is turned round: its target, or near the field's middle the point of its line nearest the middle.</summary>
    public static Vector3 PivotOf(CameraShot shot)
    {
        if (Vector3.Distance(shot.Target, FieldMiddle) > NearTheMiddle) return shot.Target;
        var line = shot.Target - shot.Position;
        float length = line.Length();
        if (length < 0.01f) return shot.Target;
        var dir = line / length;
        float t = Math.Max(1f, Vector3.Dot(FieldMiddle - shot.Position, dir));
        return shot.Position + dir * t;
    }

    private float Distance { get; }
    private float BaseYaw { get; }
    private float BasePitch { get; }

    /// <summary>How far the camera has been turned round the vertical from the shot it began on, radians.</summary>
    public float Yaw { get; private set; }

    /// <summary>The camera's pitch looking down, radians, within <see cref="LowestPitch"/> and <see cref="HighestPitch"/>.</summary>
    public float Pitch => Math.Clamp(BasePitch + Lift, LowestPitch, HighestPitch);

    // How far the arrows have raised (or lowered) it, kept so the pitch never leaves its bounds
    private float Lift { get; set; }

    /// <summary>How much closer than it began, <see cref="WidestZoom"/> to <see cref="TightestZoom"/>.</summary>
    public float Zoom { get; private set; } = 1f;

    /// <summary>The time of day borrowed for the stage's light; null is the clock's.</summary>
    public TimeOfDay? Time { get; private set; }

    /// <summary>The steps of a tenth of a second the Pokémon's idle has been moved on, 0 to <see cref="PoseSteps"/> − 1.</summary>
    public int Pose { get; private set; }

    public PhotoFilter Filter { get; private set; }

    public PhotoFrame Frame { get; private set; }

    public bool PanelOpen { get; set; }

    public BattlePhotoRow Row { get; private set; }

    public bool InterfaceHidden { get; set; }

    /// <summary>The seconds the Pokémon's idle and blink are moved on by.</summary>
    public float PoseStep => Pose * PoseTick;

    /// <summary>The time a Pokémon's pose is drawn at: the animator's (which stands still) with its phase and the step.</summary>
    public float PoseTime(float animatorTime, float phase) => animatorTime + phase + PoseStep;

    /// <summary>The hour the stage is lit at, or null for the clock's.</summary>
    public float? Hour => PhotoView.HourOf(Time);

    /// <summary>The shot the camera is held on.</summary>
    public CameraShot Shot
    {
        get
        {
            float pitch = Pitch;
            float yaw = BaseYaw + Yaw;
            float dist = Distance / Zoom;
            var dir = new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
            var position = Pivot + dir * dist;
            if (position.Y < LowestHeight) position.Y = LowestHeight;
            return new CameraShot(position, Pivot, Start.Fov);
        }
    }

    /// <summary>Turns the camera by the arrows held for <paramref name="seconds"/>: x round the vertical, y up (negative) or down.</summary>
    public void Turn(Vector2 way, float seconds)
    {
        if (seconds <= 0f) return;
        Yaw = Wrap(Yaw + way.X * TurnSpeed * seconds);
        // Up on the arrows raises the camera, which looks down more
        Lift = Math.Clamp(Lift - way.Y * LiftSpeed * seconds, LowestPitch - BasePitch, HighestPitch - BasePitch);
    }

    private static float Wrap(float a)
    {
        while (a > MathF.PI) a -= 2f * MathF.PI;
        while (a < -MathF.PI) a += 2f * MathF.PI;
        return a;
    }

    /// <summary>Brings the camera a step closer (positive) or further, within the bounds.</summary>
    public void ZoomBy(int steps)
    {
        float next = Zoom * MathF.Pow(ZoomStep, steps);
        Zoom = Math.Clamp(MathF.Round(next * 100f) / 100f, WidestZoom, TightestZoom);
    }

    /// <summary>Moves the panel's cursor a row up (negative) or down, wrapping.</summary>
    public void MoveRow(int by)
    {
        int count = Enum.GetValues<BattlePhotoRow>().Length;
        Row = (BattlePhotoRow)Step((int)Row, by, count);
    }

    /// <summary>Changes the row under the cursor a value on (positive) or back. Returns false where nothing changed.</summary>
    public bool Change(int by)
    {
        switch (Row)
        {
            case BattlePhotoRow.Zoom:
                float before = Zoom;
                ZoomBy(by);
                return Zoom != before;
            case BattlePhotoRow.Time:
                TimeOfDay?[] times = { null, TimeOfDay.Morning, TimeOfDay.Day, TimeOfDay.Twilight, TimeOfDay.Night, TimeOfDay.LateNight };
                Time = times[Step(Array.IndexOf(times, Time), by, times.Length)];
                return true;
            case BattlePhotoRow.Pose:
                Pose = Step(Pose, by, PoseSteps);
                return true;
            case BattlePhotoRow.Filter:
                Filter = Cycle(Filter, by);
                return true;
            default:
                Frame = Cycle(Frame, by);
                return true;
        }
    }

    private static int Step(int at, int by, int count) => (((at + by) % count) + count) % count;

    private static T Cycle<T>(T value, int by) where T : struct, Enum
    {
        var all = Enum.GetValues<T>();
        return all[Step(Array.IndexOf(all, value), by, all.Length)];
    }
}
