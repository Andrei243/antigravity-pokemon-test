using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Draws a photo tour's course (plan 44 · SD5; style guide, "The photo tour"): the course's scenery under the battle's
/// rig for a meadow by a lake at the ride's hour and weather, its residents posed by what they do, and the fruit, all
/// from one <see cref="TourFrame"/>, so the live ride and a kept photograph are the same drawing. One 3D scene a frame,
/// into the shared scene target, every uniform set here each frame, as <see cref="BattleRenderer"/> does.
/// </summary>
internal sealed class CourseRenderer
{
    private const float Near = 0.3f, Far = 400f;

    private readonly RenderContext context;
    private SceneMeshes? meshes;
    private string? built;
    private Mesh fruit;
    private bool fruitLoaded;

    public CourseRenderer(RenderContext context) => this.context = context;

    /// <summary>The look of the light for a ride: its course's kind of ground (a meadow by a lake for the Reed River) at its hour, in its weather.</summary>
    public static LightRig RigFor(TourFrame frame, TourCourseData course)
    {
        var rig = ArtLook.ArenaRig(CourseScene.ArenaOf(course.Look), frame.Hour);
        return frame.Weather switch
        {
            TourWeather.Rain => ArtLook.InBattleWeather(rig, BattleWeather.Rain),
            TourWeather.Snow => ArtLook.InBattleWeather(rig, BattleWeather.Snow),
            _ => rig
        };
    }

    /// <summary>Renders the frame offscreen; call outside any other texture mode. Residents whose models aren't made are left out.</summary>
    public void Render(TourFrame frame, TourCourseData course, float time)
    {
        context.EnsureLoaded();
        Ensure(course);
        var shaders = context.Shaders;
        var rig = RigFor(frame, course);
        var light = rig.Light;
        var cam = frame.Camera;
        var camera = new Camera3D(cam.Eye, cam.Eye + cam.Forward, Vector3.UnitY,
            Canvas2D.Layout.FovYFor(cam.FovY * 180f / MathF.PI), CameraProjection.Perspective);

        shaders.SetTime(time);
        shaders.SetWind(frame.Weather == TourWeather.Clear ? 0.25f : 0.6f);
        shaders.SetWalkers(Array.Empty<Vector3>());

        // 1. The shadow map round where the lens looks
        var focus = cam.Eye + Vector3.Normalize(new Vector3(cam.Forward.X, 0, cam.Forward.Z)) * 14f;
        var lightCamera = context.Shadows.LightCamera(new Vector3(focus.X, 0, focus.Z), light.SunDirection, 40f);
        Raylib.BeginTextureMode(context.Shadows.Target);
        Raylib.ClearBackground(Color.White);
        Rlgl.SetClipPlanes(1.0, 200.0);
        Raylib.BeginMode3D(lightCamera);
        var lightView = Rlgl.GetMatrixModelview();
        var lightProjection = Rlgl.GetMatrixProjection();
        Rlgl.DisableBackfaceCulling();
        meshes!.DrawDepth();
        DrawResidents(frame, time, CharacterPass.Depth);
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        // 2. The lens's view
        shaders.SetLighting(Raymath.MatrixMultiply(lightView, lightProjection), light, camera.Position, context.Shadows.Texel);
        shaders.SetCharacterStyle(shadowStrength: 1f, rimStrength: rig.Rim);
        shaders.SetWorldRamp(ArtLook.BattleRamp);
        shaders.SetFog(rig.FogColor, rig.FogAmount, rig.FogNear, rig.FogFar);
        shaders.SetCloudShade(rig.CloudShade);
        shaders.SetGlow(0f, 0f, Vector3.Zero);
        shaders.SetUpright(0f, 0f);
        shaders.SetStageWash(StageLight.None);
        shaders.SetFlash(default, 0f);

        int tw = context.PartWidth, th = context.PartHeight;
        context.BeginScene();
        SkyPainter.Draw(time, tw, th, rig.Sky, clouds: true);
        Rlgl.SetClipPlanes(Near, Far);
        Raylib.BeginMode3D(camera);
        Rlgl.DisableBackfaceCulling();
        context.BindShadowMap();
        meshes.Draw();
        meshes.DrawLights(rig.LampGlow, 0f, Vector3.One);
        DrawResidents(frame, time, CharacterPass.Color);
        DrawFruit(frame);
        Rlgl.EnableBackfaceCulling();
        DrawResidents(frame, time, CharacterPass.Outline);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        context.UnbindShadowMap();
        Rlgl.SetClipPlanes(0.01, 1000.0);
        context.PreparePost(rig.Post, new DepthRange(Near, Far, camera.FovY, (float)context.Width / context.Height));
    }

    /// <summary>Draws the rendered scene through the post passes into the current target (the virtual screen).</summary>
    public void DrawToScreen()
    {
        if (!context.Loaded) return;
        Canvas2D.Draw((w, h) => context.Composite(new Rectangle(0, 0, w, h)));
    }

    /// <summary>The pose a resident is drawn in for what it does (style guide, "The photo tour").</summary>
    public static PokePose PoseOf(TourSubject s, float time, int index)
    {
        float t = s.PoseTime;
        var pose = new PokePose { Time = time + index * 0.61f, Blink = (time + index * 0.9f) % 3.4f < 0.12f ? 1f : 0f };
        switch (s.Act)
        {
            case TourAct.Walk:
            case TourAct.Flee:
                pose.Walk = t * (s.Act == TourAct.Flee ? 1.4f : 0.8f) % 1f;
                pose.Moving = 1f;
                break;
            case TourAct.Fruit:
                // walking to the fruit, then eating it
                if (t < 1.2f) { pose.Walk = t * 0.8f % 1f; pose.Moving = 1f; }
                else pose.Eating = 1f;
                break;
            case TourAct.Sleep:
                pose.Asleep = true;
                pose.Sleepy = 1f;
                break;
            case TourAct.Eat:
                pose.Eating = 1f;
                break;
            case TourAct.Play:
                pose.Cheer = t * 0.6f % 1f;
                pose.Happy = 0.6f;
                break;
            case TourAct.Show:
                pose.Attack = Math.Clamp(t / 1.2f % 1f, 0f, 1f);
                pose.Kind = s.Move;
                break;
            case TourAct.Woke:
            case TourAct.Turn:
                pose.Stat = 0.5f;
                pose.Happy = 0.3f;
                break;
            case TourAct.Rare:
                pose.Cheer = t * 0.5f % 1f;
                pose.Happy = 1f;
                break;
        }
        return pose;
    }

    /// <summary>How high a resident is lifted for what it does: a hop of a third of its height through its rare action.</summary>
    public static float LiftOf(TourSubject s) => s.Act == TourAct.Rare ? s.Height / 3f * MathF.Abs(MathF.Sin(s.PoseTime * MathF.PI)) : 0f;

    private void DrawResidents(TourFrame frame, float time, CharacterPass pass)
    {
        for (int i = 0; i < frame.Subjects.Count; i++)
        {
            var s = frame.Subjects[i];
            if (!PokemonModels.TryGet(s.Look, out var model) || model.Height <= 0.001f) continue;
            float scale = s.Height / model.Height;
            var root = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(s.Facing) * Matrix4x4.CreateTranslation(s.X, s.Y + LiftOf(s), s.Z);
            PokemonRenderer.Draw(context, model, PoseOf(s, time, i), root, pass);
        }
    }

    private void DrawFruit(TourFrame frame)
    {
        if (frame.Fruits.Count == 0) return;
        if (!fruitLoaded)
        {
            var b = new MeshBuilder();
            Color Tone(int k) => PixelCanvas.Mix(new Color(190, 56, 44, 255), new Color(250, 196, 90, 255), k / 7f);
            b.Ellipsoid(Vector3.Zero, new Vector3(0.18f, 0.18f, 0.18f), Tone, 12, 8);
            fruit = b.Upload("tour");
            fruitLoaded = true;
        }
        foreach (var f in frame.Fruits)
            Raylib.DrawMesh(fruit, context.Toon, Matrix4x4.Transpose(Matrix4x4.CreateTranslation(f[0], f[1] + 0.18f, f[2])));
    }

    /// <summary>The course's scenery built and uploaded once, for the course asked.</summary>
    private void Ensure(TourCourseData course)
    {
        if (meshes != null && built == course.Id) return;
        Unload();
        var batches = new MeshBatches();
        CourseScene.Build(batches, course);
        meshes = SceneMeshes.Upload(batches, context.Shaders, prepassEverything: true);
        built = course.Id;
    }

    /// <summary>Lets go of the course's meshes (the ride's end).</summary>
    public void Unload()
    {
        meshes?.Unload();
        meshes = null;
        built = null;
        if (fruitLoaded)
        {
            Gpu.Unload(fruit);
            fruitLoaded = false;
        }
    }
}
