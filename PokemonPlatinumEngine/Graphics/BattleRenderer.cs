using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Draws a battle in 3D: the modelled stage under the time of day's sky and light, the trainers on their
/// platforms, and both Pokémon as lit, shadowed models with send-out, attack, hit, faint and capture animations.
/// </summary>
public sealed class BattleRenderer
{
    private const float PlatformTop = 0.16f;
    private const float Near = 0.5f, Far = 300f;

    private readonly RenderContext context;
    private readonly Dictionary<TreeStyle, BattleStage> stages = new(); // one per forest style, built on first use
    private Camera3D camera;

    // Layout projected from the 3D camera each frame, in virtual-screen pixels
    private Vector2 playerFeet = new(470, 800), enemyFeet = new(1430, 400);
    private float playerScale = 4f, enemyScale = 3f;

    /// <summary>Forest style behind the field; set from the map the battle started on.</summary>
    public TreeStyle Trees { get; set; } = TreeStyle.Round;

    public BattleRenderer(RenderContext context) => this.context = context;

    /// <summary>Renders the stage and the Pokémon offscreen. Call outside any other texture mode.</summary>
    public void Render(BattleEngine battle)
    {
        context.EnsureLoaded();
        if (!stages.TryGetValue(Trees, out var stage))
        {
            stage = BattleStage.Build(context.Shaders, Trees);
            stages[Trees] = stage;
        }

        var anim = battle.Anim;
        float shake = anim.ShakeAge >= 0f ? 1f - anim.ShakeAge / 0.4f : 0f;
        camera = BattleStage.Camera(anim.Time, shake);
        UpdateLayout();

        var rig = ArtLook.BattleRig(GameClock.Hour);
        RenderStage(stage, anim, rig);
        context.PreparePost(rig.Post, new DepthRange(Near, Far, camera.FovY, (float)context.Width / context.Height));
    }

    /// <summary>Draws the stage and the Pokémon into the current target (the virtual screen).</summary>
    public void DrawField(BattleEngine battle, int screenWidth, int screenHeight)
    {
        if (!context.Loaded) return;
        context.Composite(new Rectangle(0, 0, screenWidth, screenHeight));
        var anim = battle.Anim;
        DrawSendOutRing(Appear(anim, anim.Enemy, isPlayer: false), enemyFeet, PokemonSprites.Size * enemyScale, enemyScale);
        DrawSendOutRing(Appear(anim, anim.Player, isPlayer: true), playerFeet, PokemonSprites.Size * playerScale, playerScale);
    }

    // ------------------------------------------------------------------ layout

    private void UpdateLayout()
    {
        Vector2 Project(Vector3 p) => Raylib.GetWorldToScreenEx(p, camera, context.Width, context.Height);
        var up = new Vector3(0, PlatformTop, 0);
        enemyFeet = Project(BattleStage.EnemySpot + up);
        playerFeet = Project(BattleStage.PlayerSpot + up);

        float enemyWidth = Vector2.Distance(Project(BattleStage.EnemySpot + up - Vector3.UnitX * BattleStage.EnemyPlatformRadius),
            Project(BattleStage.EnemySpot + up + Vector3.UnitX * BattleStage.EnemyPlatformRadius));
        float playerWidth = Vector2.Distance(Project(BattleStage.PlayerSpot + up - Vector3.UnitX * BattleStage.PlayerPlatformRadius),
            Project(BattleStage.PlayerSpot + up + Vector3.UnitX * BattleStage.PlayerPlatformRadius));

        // Screen size of a 128-px sprite frame on each platform (the size rule the 3D models follow)
        enemyScale = enemyWidth / 158f;
        playerScale = playerWidth / 150f;

        BattleHUD.EnemyFeet = enemyFeet;
        BattleHUD.PlayerFeet = playerFeet;
        BattleHUD.EnemyCenter = enemyFeet - new Vector2(0, 58 * enemyScale);
        BattleHUD.PlayerCenter = playerFeet - new Vector2(0, 58 * playerScale);
    }

    // ------------------------------------------------------------------ offscreen passes

    private void RenderStage(BattleStage stage, BattleAnimator anim, LightRig rig)
    {
        var shaders = context.Shaders;
        var light = rig.Light;
        shaders.SetTime(anim.Time);

        // 1. Shadow map over the whole field
        var lightCamera = context.Shadows.LightCamera(new Vector3(-6f, 0, -6f), light.SunDirection, 72f);
        Raylib.BeginTextureMode(context.Shadows.Target);
        Raylib.ClearBackground(Color.White);
        Rlgl.SetClipPlanes(1.0, 200.0);
        Raylib.BeginMode3D(lightCamera);
        var lightView = Rlgl.GetMatrixModelview();
        var lightProjection = Rlgl.GetMatrixProjection();
        Rlgl.DisableBackfaceCulling();
        stage.DrawDepth();
        DrawTrainers(anim, CharacterPass.Depth);
        DrawPokemon3D(anim, CharacterPass.Depth);
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        shaders.SetLighting(Raymath.MatrixMultiply(lightView, lightProjection), light, camera.Position, context.Shadows.Texel);
        shaders.SetCharacterStyle(shadowStrength: 1f, rimStrength: rig.Rim);
        shaders.SetWorldRamp(ArtLook.BattleRamp);
        shaders.SetFog(rig.FogColor, rig.FogAmount, rig.FogNear, rig.FogFar);
        shaders.SetCloudShade(rig.CloudShade);

        // 2. Sky, then the field
        var target = context.Target;
        int tw = target.Texture.Width, th = target.Texture.Height;
        Raylib.BeginTextureMode(target);
        SkyPainter.Draw(anim.Time, tw, th, rig.Sky);

        Rlgl.SetClipPlanes(Near, Far);
        Raylib.BeginMode3D(camera);
        Rlgl.DisableBackfaceCulling();
        context.BindShadowMap();

        stage.Draw();
        DrawPokemonShadows(anim);
        DrawTrainers(anim, CharacterPass.Color);
        DrawPokemon3D(anim, CharacterPass.Color);
        Rlgl.EnableBackfaceCulling();
        DrawTrainers(anim, CharacterPass.Outline);
        DrawPokemon3D(anim, CharacterPass.Outline);

        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        context.UnbindShadowMap();
        Rlgl.SetClipPlanes(0.01, 1000.0);
    }

    /// <summary>Soft contact shadows on the platforms under the Pokémon, below their cast shadows.</summary>
    private static void DrawPokemonShadows(BattleAnimator anim)
    {
        var tex = SceneTextures.ShadowBlob;
        Rlgl.DisableDepthMask();

        void Blob(Vector3 spot, float radius)
        {
            float y = PlatformTop + 0.01f;
            Rlgl.CheckRenderBatchLimit(4);
            Rlgl.SetTexture(tex.Id);
            Rlgl.Begin(DrawMode.Quads);
            Rlgl.Color4ub(255, 255, 255, 255);
            Rlgl.TexCoord2f(0, 1); Rlgl.Vertex3f(spot.X - radius, y, spot.Z + radius * 0.6f);
            Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(spot.X + radius, y, spot.Z + radius * 0.6f);
            Rlgl.TexCoord2f(1, 0); Rlgl.Vertex3f(spot.X + radius, y, spot.Z - radius * 0.6f);
            Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(spot.X - radius, y, spot.Z - radius * 0.6f);
            Rlgl.End();
        }

        if (anim.Enemy.Present && anim.Enemy.Shown != null)
            Blob(BattleStage.EnemySpot, BattleStage.EnemyPlatformRadius * 0.62f * PokemonModels.Get(anim.Enemy.Shown.Species.Name).Fill);
        if (anim.Player.Present && anim.Player.Shown != null)
            Blob(BattleStage.PlayerSpot, BattleStage.PlayerPlatformRadius * 0.62f * PokemonModels.Get(anim.Player.Shown.Species.Name).Fill);

        Rlgl.SetTexture(0);
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }

    /// <summary>Trainers stand on their platforms at the start, then run off as they send their Pokémon out.</summary>
    private void DrawTrainers(BattleAnimator anim, CharacterPass pass)
    {
        if (anim.PlayerTrainer != null)
            DrawTrainer(anim.PlayerTrainer, BattleStage.PlayerSpot, MathF.PI, 1.15f, anim.PlayerTrainerExit, -1f, anim.Time, pass);
        if (anim.EnemyTrainer != null)
            DrawTrainer(anim.EnemyTrainer, BattleStage.EnemySpot, 0f, 2.3f, anim.EnemyTrainerExit, 1f, anim.Time + 1.3f, pass);
    }

    private void DrawTrainer(string type, Vector3 spot, float yaw, float scale, float exit, float direction, float time, CharacterPass pass)
    {
        var rig = CharacterModels.Get(type, context.Shaders.Character);
        var pose = new CharacterPose { Time = time, Blink = time % 3.7f < 0.12f };
        var feet = spot + new Vector3(0, PlatformTop, 0);
        if (exit >= 0f)
        {
            float p = Math.Clamp(exit / BattleAnimator.TrainerExitTime, 0f, 1f);
            feet.X += direction * p * p * 9f * scale;
            yaw = direction > 0 ? MathF.PI / 2f : -MathF.PI / 2f;
            pose.Walk = exit * 2.6f;
            pose.WalkBlend = 1f;
            pose.Running = true;
        }
        var root = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(feet);
        CharacterRenderer.Draw(context, rig, pose, root, pass, trueProportions: true);
    }

    // ------------------------------------------------------------------ Pokémon

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * MathF.Pow(t - 1f, 3f) + c1 * MathF.Pow(t - 1f, 2f);
    }

    /// <summary>How a Pokémon looks this frame.</summary>
    private struct Appearance
    {
        public bool Visible;
        public float Grow;            // send-out growth times recall/capture shrink
        public float Lunge;           // 0..1 toward the opponent
        public float Shake;           // -1..1 sideways jolt after a hit
        public float Sink;            // 0..1 of its height sunk below the platform while fainting
        public float Flash;
        public Color FlashColor;
        public float Silhouette;      // 0..1 dark silhouette of a wild Pokémon before the camera settles
        public float SendOut;         // send-out progress for the ring effect (-1 when not sending out)
    }

    private static Appearance Appear(BattleAnimator anim, CombatantView v, bool isPlayer)
    {
        var a = new Appearance { Grow = 1f, FlashColor = Color.White, SendOut = -1f, Visible = true };
        bool fainting = v.FaintAge >= 0f, recalling = v.RecallAge >= 0f, capturing = v.CaptureAge >= 0f;
        if (v.Shown == null || (!v.Present && !fainting && !recalling && !capturing))
        {
            a.Visible = false;
            return a;
        }

        // Bursting out of the ball: grow from a flash of light
        float p = BattleAnimator.Progress(v.SendOutAge, BattleAnimator.SendOutTime);
        if (p >= 0f)
        {
            a.Grow = 0.12f + 0.88f * EaseOutBack(p);
            a.Flash = 1f - p;
            a.SendOut = p;
        }

        // Attack: lunge toward the opponent and back
        p = BattleAnimator.Progress(v.AttackAge, BattleAnimator.AttackTime);
        if (p >= 0f) a.Lunge = MathF.Sin(p * MathF.PI);

        // Hit: flicker and shake
        p = BattleAnimator.Progress(v.HitAge, BattleAnimator.HitTime);
        if (p >= 0f)
        {
            a.Visible = (int)(p * 10f) % 2 == 0;
            a.Shake = MathF.Sin(p * 48f) * (1f - p);
        }

        // Faint: sink into the platform, which hides the model as it goes
        if (fainting && v.FaintDelay <= 0f)
        {
            p = Math.Clamp(v.FaintAge / BattleAnimator.FaintTime, 0f, 1f);
            a.Sink = p * p;
        }

        // Recall or capture: shrink away into a red beam of light
        float shrink = Math.Max(BattleAnimator.Progress(v.RecallAge, BattleAnimator.RecallTime),
            capturing && v.CaptureDelay <= 0f ? BattleAnimator.Progress(v.CaptureAge, BattleAnimator.CaptureTime) : -1f);
        if (shrink >= 0f)
        {
            a.Grow *= 1f - shrink;
            a.Flash = Math.Min(1f, shrink * 1.6f);
            a.FlashColor = new Color(255, 70, 70, 255);
        }

        // Wild Pokémon start as a dark silhouette while the camera sweeps in
        if (!isPlayer && anim.EnemyTrainer == null && anim.Time < 1.6f && v.SendOutAge < 0f)
            a.Silhouette = 1f - Math.Clamp((anim.Time - 0.7f) / 0.8f, 0f, 1f);

        if (a.Grow <= 0.01f) a.Visible = false;
        return a;
    }

    private static readonly Color SilhouetteColor = new(34, 30, 46, 255);

    /// <summary>The ring of light around a Pokémon being sent out, drawn over the composited field.</summary>
    private static void DrawSendOutRing(Appearance a, Vector2 feet, float size, float scale)
    {
        if (a.SendOut < 0f) return;
        float p = a.SendOut;
        float ring = size * 0.45f * EaseOutBack(Math.Min(1f, p * 1.4f));
        var ringColor = new Color(255, 255, 255, (int)(220 * (1f - p)));
        var center = feet - new Vector2(0, size * 0.3f);
        Raylib.DrawRing(center, ring * 0.86f, ring, 0, 360, 48, ringColor);
        for (int i = 0; i < 8; i++)
        {
            float ang = i * MathF.Tau / 8f + p * 2f;
            var spark = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * ring * 1.15f;
            Raylib.DrawCircleV(spark, 7f * (1f - p) * scale / 3f, new Color(255, 250, 200, (int)(255 * (1f - p))));
        }
    }

    private static readonly Matrix4x4[] BoneMatrices = new Matrix4x4[64];

    /// <summary>
    /// The Pokémon as a 3D model standing on its platform, lit and shadowed with the scene. It is sized
    /// so it covers the same part of the screen as the sprite would.
    /// </summary>
    private void DrawPokemon3D(BattleAnimator anim, CombatantView v, bool isPlayer, CharacterPass pass)
    {
        if (v.Shown == null) return;
        var a = Appear(anim, v, isPlayer);
        if (!a.Visible) return;

        var model = PokemonModels.Get(v.Shown.Species.Name);
        PokemonSprites.EnsureSceneOutline(model);
        var view = isPlayer ? SpriteView.Back : SpriteView.Front;
        var framing = PokemonSprites.Framing(model, view, PokemonSprites.Size);
        var spot = isPlayer ? BattleStage.PlayerSpot : BattleStage.EnemySpot;
        var other = isPlayer ? BattleStage.EnemySpot : BattleStage.PlayerSpot;
        float radius = isPlayer ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius;

        // Same size rule as the sprites: the sprite frame spans the platform's width divided by 158 (or 150) pixels
        float scale = 2f * radius / ((isPlayer ? 150f : 158f) * framing.WorldPerPixel);
        float frame = scale * framing.WorldPerPixel * PokemonSprites.Size;
        var toward = Vector3.Normalize(other - spot);
        var feet = spot + new Vector3(0, PlatformTop, 0)
            + toward * a.Lunge * frame * (22f / 128f)
            + Vector3.UnitX * a.Shake * frame * (4f / 128f)
            - Vector3.UnitY * a.Sink * frame;
        var root = Matrix4x4.CreateScale(scale * a.Grow) * Matrix4x4.CreateRotationY(framing.Yaw) * Matrix4x4.CreateTranslation(feet);

        float phase = isPlayer ? 0.37f : 0f;
        var pose = new PokePose
        {
            Time = anim.Time + phase,
            Blink = (anim.Time + phase * 3f) % 3.3f < 0.12f ? 1f : 0f,
            Attack = Math.Max(0f, BattleAnimator.Progress(v.AttackAge, BattleAnimator.AttackTime)),
            Hurt = Math.Max(0f, BattleAnimator.Progress(v.HitAge, BattleAnimator.HitTime))
        };
        model.BoneTransforms(pose, BoneMatrices);

        if (pass == CharacterPass.Color)
        {
            if (a.Flash > 0f) context.Shaders.SetFlash(a.FlashColor, Math.Clamp(a.Flash, 0f, 1f) * 0.85f);
            else if (a.Silhouette > 0f) context.Shaders.SetFlash(SilhouetteColor, a.Silhouette * 0.92f);
        }
        for (int i = 0; i < model.Bones.Count; i++)
        {
            var bone = model.Bones[i];
            if (!bone.Uploaded) continue;
            var m = Matrix4x4.Transpose(BoneMatrices[i] * root);
            switch (pass)
            {
                case CharacterPass.Depth: Raylib.DrawMesh(bone.Mesh, context.Depth, m); break;
                case CharacterPass.Color: Raylib.DrawMesh(bone.Mesh, context.Toon, m); break;
                case CharacterPass.Outline: if (bone.SceneOutlineUploaded) Raylib.DrawMesh(bone.SceneOutline, context.Outline, m); break;
            }
        }
        if (pass == CharacterPass.Color) context.Shaders.SetFlash(default, 0f);
    }

    private void DrawPokemon3D(BattleAnimator anim, CharacterPass pass)
    {
        DrawPokemon3D(anim, anim.Enemy, isPlayer: false, pass);
        DrawPokemon3D(anim, anim.Player, isPlayer: true, pass);
    }
}
