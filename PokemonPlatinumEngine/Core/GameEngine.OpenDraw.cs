using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The open field's picture (plan 32 · OW8; the <c>Draw</c> half OW1 left): while the player is in an open map the
/// field's 3D scene is the open renderer's, its camera the orbit camera behind the player, and its picture is
/// composited where the grid's was. Nothing of a region of today reaches it: no region's file says <c>open</c>, so
/// <c>flow.Open</c> is null in every game and the grid's field is drawn as it always was.
/// </summary>
public partial class GameEngine
{
    private OpenRenderer? openRenderer;
    private readonly OrbitCamera openCamera = new();
    private readonly OpenUnderRock openUnderRock = new();
    // A place's own light (plan 33 · GL6): Spikemuth's night, the Darkest Day over Wyndon
    private readonly OpenMoodEase openMood = new();
    private float openFrameSeconds;
    private bool openScriptWasRunning, openViewSettled;
    private double openWalked;
    private OpenVec? openLastFeet;
    // The player's grown body's motion (plan 30 · FM6): its gait, yaw, lean and turns from how fast the drawn feet moved
    private GrownMotion? openMotion;
    private Vector3 openMotionFeet;

    /// <summary>The open renderer, made the first time an open map is drawn (it needs the window).</summary>
    private OpenRenderer OpenRendererNow => openRenderer ??= new OpenRenderer(renderContext);

    /// <summary>The open field's weather in place of its area's (a tool's: the harness's rain), or null.</summary>
    internal FieldWeather? OpenWeatherOverride { get; set; }

    /// <summary>A camera placed by hand in place of the orbit camera's (a tool's: the harness's views from over the land), or null.</summary>
    internal (Vector3 Eye, Vector3 Target)? OpenViewOverride { get; set; }

    /// <summary>
    /// The orbit camera begun afresh behind the player (a map come into, a save loaded, a place put down on): its arm let
    /// out whole and the cave's share settled at the next frame drawn, and the player's grown rig asked for ahead.
    /// </summary>
    private void BeginTheOpenCamera(int yaw)
    {
        openCamera.Reset(yaw);
        openMount.Reset();
        openMountAsked = 0;
        openViewSettled = false;
        openLastFeet = null;
        openWalked = 0;
        openMotion = null;
        if (!IsHeadless) CharacterModels.Preload(new[] { OpenPlayerLook });
        // The people's looks and the follower's, asked for behind the same fade (plan 32 · OW9)
        if (flow.Open is { } field) PreloadTheOpenLooks(field);
    }

    /// <summary>The player's look in the open world: plan 30 · FM4's grown kit (<c>PLAYER~Teen</c>), dressed as the player is.</summary>
    internal static string OpenPlayerLook => CharacterStyle.InKit(PlayerIdentity.Character, Data.CharacterKit.Grown);

    /// <summary>
    /// A frame of the open camera, beside the grid's camera rig (the field's states only): the shoulders turn it while
    /// the field is the player's, it eases behind a walking player, a script's look eases on and comes home once the
    /// script has ended, and the cave's share eases by whether rock is over it.
    /// </summary>
    private void KeepTheOpenCamera(float dt)
    {
        if (flow.Open is not { } open) return;
        openFrameSeconds = dt;
        bool theirs = flow.State == GameState.Overworld && !ScriptRunning && !flow.StartMenu.IsActive;
        if (theirs)
        {
            double turn = (InputManager.IsActionDown(GameAction.PageRight) ? 1 : 0) - (InputManager.IsActionDown(GameAction.PageLeft) ? 1 : 0);
            openCamera.Turn(turn, dt);
        }
        var feet = OpenVec.Of(open.Player.Pos);
        double speed = 0;
        if (openLastFeet is { } last && dt > 0)
        {
            double dx = feet.X - last.X, dz = feet.Z - last.Z;
            double moved = Math.Sqrt(dx * dx + dz * dz);
            speed = moved / dt;
            openWalked += moved;
        }
        openLastFeet = feet;
        // Over the mount while it is ridden (plan 30 · FM13): nothing of it reaches the camera on foot
        openCamera.Ride(open.Player.Body.Mounted, open.Player.Body.Stance, speed, dt);
        openCamera.Advance(dt, open.Player.Yaw, speed);
        if (openScriptWasRunning && !ScriptRunning) openCamera.Home();
        openScriptWasRunning = ScriptRunning;
    }

    /// <summary>Where the player's feet are drawn: between the field's last two ticks, by how far the next has come.</summary>
    private static OpenVec DrawnFeet(OpenField open)
    {
        var before = OpenVec.Of(open.Player.Before);
        var now = OpenVec.Of(open.Player.Pos);
        return before + (now - before) * open.Clock.Between;
    }

    /// <summary>The weather where the player stands: the tool's, else the area's of the day (<see cref="OpenWeather"/>).</summary>
    private FieldWeather OpenWeatherHere(OpenField open)
    {
        if (OpenWeatherOverride is { } forced) return forced;
        // The area's by the region's calendar for today where it keeps one (plan 33 · GL2), its own otherwise
        return OpenWeather.At(open.Map, open.Player.Pos, GameClock.Today);
    }

    /// <summary>The open frame's scene: the camera placed by its arm, the cave's share, the hour, the weather and the player's pose.</summary>
    private OpenScene OpenSceneNow(OpenField open)
    {
        var feet = DrawnFeet(open);
        var ground = open.Map.Ground;
        var (eye, target) = openCamera.Place(ground, feet, openViewSettled ? openFrameSeconds : 0);
        if (OpenViewOverride is { } byHand) (eye, target) = (new OpenVec(byHand.Eye.X, byHand.Eye.Y, byHand.Eye.Z), new OpenVec(byHand.Target.X, byHand.Target.Y, byHand.Target.Z));
        string? mood = OpenMoodEase.Wanted(open.Map.AreaAt(open.Player.Pos), flow.Story.Has);
        if (!openViewSettled) { openUnderRock.Settle(ground, eye); openMood.Settle(mood); openViewSettled = true; }
        else { openUnderRock.Advance(ground, eye, openFrameSeconds); openMood.Advance(mood, openFrameSeconds); }
        var body = open.Player.Body;
        double perTick = Math.Sqrt(open.Player.Pos.GroundDistanceSquared(open.Player.Before)) / OpenPos.PerMetre;
        double speed = perTick * OpenClock.TicksPerSecond;
        var pose = new CharacterPose
        {
            Time = (float)(FrameClock.Now % 3600.0),
            Walk = (float)(openWalked / 1.6 % 1.0),
            WalkBlend = speed > OrbitCamera.Walking ? 1f : 0f,
            Running = speed > 3.0
        };
        // The grown rig's gait, way, lean and turns from its own motion, handed the drawn feet's speed over the ground
        // (plan 30 · FM6); until the rig is made the walk above stands
        float yaw = (float)OpenYaw.Radians(body.Yaw);
        var drawn = V(feet);
        if (CharacterModels.TryGet(OpenPlayerLook, renderContext.Shaders) is { Style.Kit: Data.CharacterKit.Grown } rig)
        {
            if (openMotion == null || MathF.Abs(openMotion.Leg - GrownAnimation.LegsOf(rig.Skeleton).Length) > 1e-4f)
                (openMotion, openMotionFeet) = (GrownMotion.For(rig, yaw), drawn);
            float dt = openViewSettled ? openFrameSeconds : 0f;
            openMotion.Step(dt, FieldRigChoice.VelocityOf(openMotionFeet, drawn, dt, 1f), yaw);
            openMotionFeet = drawn;
            openMotion.Fill(ref pose);
            yaw = openMotion.Yaw;
        }
        // Who and what stands there beside the player, and whom the grass parts round (plan 32 · OW9)
        var figures = GatherOpenFigures(open);
        // The mount and the rider on it (plan 30 · FM13): nothing of it on foot once a dismount is over
        var (mount, riderRoot) = OpenMountNow(open, drawn, yaw, ref pose);
        // A script's zoom (`camera zoom`) is the grid's rig's, which runs beside the orbit camera: its lens, not its arm;
        // riding widens it by the mount's share (nothing of it on foot)
        return new OpenScene(open.Map, V(feet), yaw, pose, OpenPlayerLook,
            V(eye), V(target), GameClock.Hour, OpenWeatherHere(open), (float)openUnderRock.Share, FrameClock.Now,
            body.Stance == OpenStance.Swimming, (float)openCamera.RideLens(OrbitCamera.LensFor(world.Camera.Lens)), figures, OpenWalkersOf(open, figures),
            mount, riderRoot, OpenBeamsNow(open), openMood.Mood, (float)openMood.Share);
    }

    // ------------------------------------------------------------------ the dens' beams (plan 33 · GL3)

    private readonly List<DenBeam> openBeams = new();
    private (OpenMap? Map, uint Day, int Revision) openBeamsFor;

    /// <summary>
    /// The beams over the open map's lit Max Raid dens (plan 33 · GL3, second session; style guide, "Max Raid dens"): each
    /// trigger of a den (`den_<id>`) whose den stands lit today by the same question its script asks (`Raids.BeamStands`:
    /// the day's draw or a Wishing Piece, and not felled today), at the ground under the trigger's middle. Worked out
    /// again only when the map, the day or the story changes; nothing of it reaches the rules.
    /// </summary>
    internal IReadOnlyList<DenBeam> OpenBeamsNow(OpenField open)
    {
        var key = (open.Map, flow.Encounters.DailyNumber, flow.Story.Revision);
        if (openBeamsFor == key) return openBeams;
        openBeamsFor = key;
        openBeams.Clear();
        int version = Math.Max(1, flow.Story.Var("VAR_GALAR_VERSION"));
        int badges = flow.Story.BadgeCount(RegionDatabase.Galar);
        foreach (var trigger in open.Map.Triggers)
        {
            if (Raids.DenOfTrigger(trigger.Name) is not { } den) continue;
            bool wished = flow.Story.Has(Story.ScriptRunner.DenFlag(den.Id, "WISHED"));
            bool felled = flow.Story.Has(Story.ScriptRunner.DenFlag(den.Id, "DONE"));
            if (!Raids.BeamStands(den, key.DailyNumber, version, badges, wished, felled)) continue;
            var middle = trigger.Shape.Base;
            double x = middle.X / (double)OpenPos.PerMetre, z = middle.Z / (double)OpenPos.PerMetre;
            double top = trigger.Shape.Max.Y / (double)OpenPos.PerMetre, bottom = trigger.Shape.Min.Y / (double)OpenPos.PerMetre;
            double y = open.Map.Ground.Footing(x, z, top, bottom)?.Y ?? bottom + 1;
            int stars = Raids.Today(den, key.DailyNumber, version, badges, wished).Stars;
            openBeams.Add(new DenBeam(new Vector3((float)x, (float)y, (float)z), stars));
        }
        return openBeams;
    }

    // ------------------------------------------------------------------ the mount as a rig (plan 30 · FM13)

    private readonly MountRig openMount = new();

    /// <summary>
    /// The mount's frame and the rider placed on it (plan 30 · FM13; style guide, "Open world", "The mount"): the rig
    /// stepped by the frame's time with the body's stance, the ride form's model asked for (and its fellows ahead, so a
    /// change of form never waits), the rider's pose made a rider's and their root the seat's. Null and null on foot, and
    /// while the form's model is still being made (the rider is drawn standing at the feet meanwhile).
    /// </summary>
    private (OpenMountDraw? Mount, Matrix4x4? RiderRoot) OpenMountNow(OpenField open, Vector3 feet, float yaw, ref CharacterPose pose)
    {
        var body = open.Player.Body;
        int which = flow.Story.Var(MountRules.MountVariable);
        var cling = body.Cling;
        float dt = openViewSettled ? openFrameSeconds : 0f;
        var frame = openMount.Step(dt, new MountInput(body.Mounted, which, body.Stance, feet, yaw, ((float)cling.X, (float)cling.Z), FrameClock.Now));
        if (frame is not { } f) return (null, null);
        if (IsHeadless) return (null, null);
        AskForTheRideForms(which);
        // (asked for again on every miss: a battle's or a warp's trim may have let it go since the ride began)
        if (!PokemonModels.TryGet(f.Form, out var model))
        {
            PokemonModels.Request(f.Form);
            return (null, null);
        }
        var rig = CharacterModels.TryGet(OpenPlayerLook, renderContext.Shaders);
        float rigScale = rig?.Scale ?? 1f;
        if (f.Seated > 0f)
        {
            pose.Riding = Riding.Mount;
            pose.RideStance = f.Stance;
            pose.RunBlend = f.RunBlend;
            pose.Running = false;
            pose.Astride = MountLooks.HalfWidth(model, f) / rigScale;
            // (the walk's own fields stand as the motion filled them: the rock of the ride is the mount's stride)
            pose.Walk = f.Pose.Walk;
            pose.WalkBlend = f.Pose.Moving;
        }
        float saddle = rig != null ? GrownAnimation.SaddleOf(rig.Skeleton) * rigScale : 0.8f;
        var riderRoot = MountLooks.RiderRoot(model, f, saddle);
        var seat = MountLooks.SeatOf(model);
        openMountTold = $"{f.Form}: size {f.Scale:0.000} m a unit, flash {f.Flash:0.00}, the rider {(f.Seated >= 1f ? "seated" : $"{f.Seated:0.00} of the way to the seat")}, {f.Stance} (crouch {f.RunBlend:0.00}), seat {seat.Rest.Y * f.Scale:0.00} m up on its line {seat.Rest.Z:0.000}, half width {seat.HalfWidth * f.Scale:0.00} m, astride {pose.Astride:0.000}";
        return (new OpenMountDraw(model, MountLooks.Place(f, seat), f.Pose, f.Flash), riderRoot);
    }

    private int openMountAsked;
    private string? openMountTold;

    /// <summary>The mount's eight forms asked for once a mount is ridden, so the next form is made before it is wanted.</summary>
    private void AskForTheRideForms(int which)
    {
        if (openMountAsked == which) return;
        openMountAsked = which;
        foreach (var stance in new[] { OpenStance.Standing, OpenStance.Swimming, OpenStance.Gliding })
            foreach (bool ridden in new[] { false, true })
                if (MountRules.FormFor(which, stance, ridden) is { } form) PokemonModels.Request(form);
    }

    private static Vector3 V(OpenVec v) => new((float)v.X, (float)v.Y, (float)v.Z);

    /// <summary>The field's 3D scene this frame: the open map's where the player is in one, the grid's otherwise.</summary>
    private void RenderTheField()
    {
        if (flow.Open is { } open)
        {
            // The layers the console, a scenario or the harness turned on are the grid's and the open world's alike (plan 32 · OW10)
            OpenRendererNow.Layers = world.Layers;
            OpenRendererNow.LayerField = open;
            OpenRendererNow.Render(OpenSceneNow(open));
            return;
        }
        // Whose mouth is open is read as the frame is drawn, after the box's voice has moved on (plan 26 · V6)
        world.Talking = SpeakerNow();
        // A grown region's people are live rigs (plan 30 · FM3); everywhere else the sprites they always were
        world.GrownField = KitHere == Data.CharacterKit.Grown;
        // A talk under way (a box of text, a script): a grown player before the one they face stands back a little
        world.InTalk = flow.State == GameState.Dialogue || flow.Runner.IsRunning;
        world.Render(flow.Map, flow.Player);
    }

    /// <summary>The field's picture composited into the 2D pass: the open map's or the grid's.</summary>
    private void DrawTheField(SceneRipple ripple = default)
    {
        // Over the whole canvas, where the window is another shape than 16:9 (rulings, "Other shapes of screen")
        if (flow.Open != null && openRenderer != null) Canvas2D.Draw((w, h) => openRenderer.DrawToScreen(w, h, ripple));
        else Canvas2D.Draw((w, h) => world.DrawToScreen(w, h, ripple));
    }

    /// <summary>Where the player's middle is on the field's picture, in shares of it: the open map's or the grid's.</summary>
    private Vector2 FieldPlayerOnPicture => flow.Open != null && openRenderer != null ? openRenderer.PlayerOnPicture : world.PlayerOnPicture;

    internal sealed partial class Driver
    {
        /// <summary>The open map's orbit camera (plan 32 · OW8): the harness turns it to its four yaws.</summary>
        public OrbitCamera OpenCamera => game.openCamera;

        /// <summary>Whether rock is over the camera, and how far the cave's light is in.</summary>
        public OpenUnderRock OpenUnderRock => game.openUnderRock;

        /// <summary>The open renderer, once an open map has been drawn: what its last frame drew.</summary>
        public OpenRenderer? OpenRenderer => game.openRenderer;

        /// <summary>The open field's weather in place of its area's, or null for the area's.</summary>
        public FieldWeather? OpenWeather
        {
            get => game.OpenWeatherOverride;
            set => game.OpenWeatherOverride = value;
        }

        /// <summary>A camera placed by hand in place of the orbit camera's, or null for the orbit camera.</summary>
        public (Vector3 Eye, Vector3 Target)? OpenView
        {
            get => game.OpenViewOverride;
            set => game.OpenViewOverride = value;
        }

        /// <summary>The open map's weather where the player stands, as the renderer takes it.</summary>
        public FieldWeather OpenWeatherHere => game.flow.Open is { } open ? game.OpenWeatherHere(open) : FieldWeather.Clear;

        /// <summary>What the last frame drew of the mount and its rider (plan 30 · FM13), or null on foot.</summary>
        public string? MountTold => game.openMount.Target == null ? null : game.openMountTold;

        /// <summary>The form the mount's rig wants now, or null.</summary>
        public string? MountForm => game.openMount.Target;

        /// <summary>The player's look in the open world (the grown kit).</summary>
        public string OpenPlayerLook => GameEngine.OpenPlayerLook;

        /// <summary>
        /// The open view settled for a shot: the camera's arm let out whole where nothing holds it, the cave's share
        /// whole or nothing, and every chunk on the card at the level its ring wants, with the player's rig made.
        /// </summary>
        public void SettleOpenView()
        {
            if (game.flow.Open is not { } open || game.IsHeadless) return;
            game.openViewSettled = false;
            game.openCamera.Settle();
            CharacterModels.Get(GameEngine.OpenPlayerLook, game.renderContext.Shaders);
            var renderer = game.OpenRendererNow;
            var scene = game.OpenSceneNow(open);
            // Every figure's rig or model made now, so what a shot shows never waits on a background build (plan 32 · OW9)
            foreach (var figure in scene.Figures ?? Array.Empty<OpenFigure>())
                if (figure.Kind == OpenFigureKind.Person) CharacterModels.Get(figure.Look, game.renderContext.Shaders);
                else PokemonModels.Get(figure.Look);
            game.openCamera.Settle();
            renderer.Rings.Reset();
            renderer.Rings.Move(new OpenVec(scene.Feet.X, scene.Feet.Y, scene.Feet.Z));
            renderer.Stream.Use(open.Map);
            renderer.Stream.Settle(renderer.Rings, new OpenVec(scene.Feet.X, scene.Feet.Y, scene.Feet.Z));
            game.openViewSettled = false;
        }
    }
}
