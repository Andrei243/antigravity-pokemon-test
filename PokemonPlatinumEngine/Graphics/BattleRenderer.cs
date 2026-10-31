using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Draws a battle in 3D (plan 04 · G8): the arena's stage under its light, the trainers throwing their balls and
/// stepping aside, the Pokémon as lit, shadowed models, the Poké Balls, and the moves' effects, all seen through
/// the camera director's shots.
/// </summary>
public sealed class BattleRenderer
{
    private const float Near = 0.5f, Far = 300f;

    private readonly RenderContext context;
    private readonly Dictionary<ArenaSpec, BattleStage> stages = new(); // one per kind of arena, built on first use
    private readonly BattleCamera director = new();
    private readonly FxList fx = new();
    private readonly FxPlaces places = new();
    private readonly List<TrainerPlacement> trainers = new();
    private readonly List<BallDraw> balls = new();
    private Camera3D camera;
    private BattleEngine? lastBattle;
    private float lastTime;
    private float platformTop = BattleStage.PlatformHeight;

    /// <summary>The stage the next battles are fought on.</summary>
    internal ArenaSpec Arena { get; set; } = new(BattleArena.Grass);

    /// <summary>What the camera director is showing (for the harness and tests).</summary>
    internal ShotKind Shot => director.Kind;

    /// <summary>The effects drawn in the last frame.</summary>
    internal FxList Effects => fx;

    /// <summary>The stage's light in the last frame (plan 25 · Y16): what a move washed it toward, if anything.</summary>
    internal StageLight StageLit { get; private set; } = StageLight.None;

    public BattleRenderer(RenderContext context) => this.context = context;

    /// <summary>The stage for a battle starting with the player on (<paramref name="x"/>, <paramref name="y"/>) of <paramref name="map"/>.</summary>
    public void SetArena(Map map, int x, int y) => Arena = ArenaSpec.For(map, x, y);

    /// <summary>The map's own stage, whatever the ground under the player.</summary>
    public void SetArena(Map map) => SetArena(map, -1, -1);

    /// <summary>A stage by kind (the harness's arena gallery).</summary>
    public void SetArena(BattleArena kind, PokemonType? theme = null, TreeStyle trees = TreeStyle.Round, bool lakeside = false) =>
        Arena = new ArenaSpec(kind, theme, trees, lakeside);

    private BattleStage StageFor(ArenaSpec spec)
    {
        if (!stages.TryGetValue(spec, out var stage))
        {
            stage = BattleStage.Build(context.Shaders, spec);
            stages[spec] = stage;
        }
        return stage;
    }

    /// <summary>Renders the stage, the Pokémon and the effects offscreen. Call outside any other texture mode.</summary>
    public void Render(BattleEngine battle)
    {
        context.EnsureLoaded();
        var stage = StageFor(Arena);
        var anim = battle.Anim;
        if (!ReferenceEquals(battle, lastBattle) || anim.Time < lastTime)
        {
            director.Reset();
            lastBattle = battle;
            lastTime = anim.Time;
        }
        float dt = Math.Clamp(anim.Time - lastTime, 0f, 0.1f);
        lastTime = anim.Time;

        platformTop = BattleStage.PlatformTopOf(Arena.Kind);
        UpdatePlaces(anim);
        var arenaRig = ArtLook.ArenaRig(Arena, GameClock.Hour);
        bool night = Arena.Outdoors && arenaRig.Sky.Stars > 0f;
        // The weather's light (plan 25 · Y1): the one clearing away under the one coming in, each by how far it has come
        var rig = ArtLook.InBattleWeather(ArtLook.InBattleWeather(arenaRig, anim.FadingWeather, anim.FadingStrength), anim.Weather, anim.WeatherStrength);
        // Gravity lowers the stage's light for as long as it lasts (plan 25 · Y2)
        rig = ArtLook.UnderGravity(rig, anim.FieldLook.Of(Battle.Sim.FieldCondition.Gravity).Presence);

        fx.Clear();
        MoveFx.Emit(anim, places, fx);
        // What lasts about each Pokémon: Z's, bubbles, flames, sparks, a shell of ice, stars, hearts (plan 09 · L2)
        MoveFx.Conditions(anim, places, fx);
        // And a giant's wreath of red clouds and a Terastallized Pokémon's crown (plan 25 · Y15)
        MoveFx.Giants(anim, places, fx);
        ArenaFx.Emit(Arena, anim.Time, night, fx, Math.Max(anim.WeatherStrength, anim.FadingStrength));
        ArenaFx.Weather(anim.FadingWeather, BattleAnimator.WeatherFadeTime, anim.Time, places, fx, anim.FadingStrength);
        ArenaFx.Weather(anim.Weather, anim.WeatherAge, anim.Time, places, fx);
        // What lies on the field: hazards, screens, the rooms, what was sent ahead (plan 25 · Y2)
        BattleFieldFx.Emit(anim.FieldLook, anim.Time, places, anim.SlotsOf(BattleSide.Player), anim.SlotsOf(BattleSide.Enemy), fx);
        // A rotation battle's turntables (plan 14 · B7)
        RotationFx.Emit(anim, places, fx);
        DollEffects(anim);
        PlaceTrainers(anim);
        PlaceBalls(anim);

        float shake = (anim.ShakeAge >= 0f ? (1f - anim.ShakeAge / 0.4f) * anim.ShakeStrength : 0f) + fx.Shake;
        camera = director.Update(dt, anim, places, shake, choosing: battle.HUD.MenuState != BattleMenuState.Message);

        // A move's light over the stage alone (plan 25 · Y16): the Pokémon and the effects keep the scene's own
        StageLit = StageLights.Of(anim);

        FrameProfiler.Lap(FrameSection.Prepare);
        RenderStage(stage, anim, rig, StageLit);
        FrameProfiler.Lap(FrameSection.Scene);
        context.PreparePost(rig.Post, new DepthRange(Near, Far, camera.FovY, (float)context.Width / context.Height));
    }

    /// <summary>Draws the stage and the Pokémon into the current target (the virtual screen), and any flash over them.</summary>
    public void DrawField(BattleEngine battle, int screenWidth, int screenHeight)
    {
        if (!context.Loaded) return;
        context.Composite(new Rectangle(0, 0, screenWidth, screenHeight));
        if (fx.Flash > 0.004f)
            Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(fx.FlashColor.R, fx.FlashColor.G, fx.FlashColor.B, (byte)(255 * fx.Flash)));
    }

    // ------------------------------------------------------------------ places

    /// <summary>How much the Pokémon on a side's platform are scaled by how many share it (<see cref="BattlePlaces.Fit"/>).</summary>
    private static float SlotFit(BattleAnimator anim, BattleSide side) => BattlePlaces.Fit(anim.SlotsOf(side), anim.Spread(side));

    /// <summary>Where a place on the field stands (<see cref="BattlePlaces.Spot"/>).</summary>
    private static Vector3 Spot(BattleSide side, int slot, int slots) => BattlePlaces.Spot(side, slot, slots);

    /// <summary>The most places either side has: what a loop over both sides' places goes up to.</summary>
    private static int MostSlotsShown(BattleAnimator anim) => Math.Max(anim.SlotsOf(BattleSide.Player), anim.SlotsOf(BattleSide.Enemy));

    /// <summary>Where a place stands in the battle shown: its side's count of places.</summary>
    private static Vector3 Spot(BattleAnimator anim, BattleSide side, int slot) => BattlePlaces.Spot(side, slot, anim.SlotsOf(side), anim.Spread(side));

    /// <summary>
    /// How much a Pokémon's model is scaled on its platform: the same size rule as the sprites, the sprite frame
    /// spanning the platform's width divided by 158 (opponent) or 150 (player) pixels.
    /// </summary>
    private static float ModelScale(PokeModel model, bool isPlayer, BattleAnimator anim)
    {
        var framing = PokemonSprites.Framing(model, isPlayer ? SpriteView.Back : SpriteView.Front, PokemonSprites.Size);
        float radius = isPlayer ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius;
        return 2f * radius / ((isPlayer ? 150f : 158f) * framing.WorldPerPixel) * SlotFit(anim, isPlayer ? BattleSide.Player : BattleSide.Enemy);
    }

    /// <summary>Measures where each Pokémon stands and how tall it is, for the effects and the camera.</summary>
    private void UpdatePlaces(BattleAnimator anim)
    {
        for (int side = 0; side < 2; side++)
            for (int slot = 0; slot < BattleAnimator.MostSlots; slot++)
            {
                var s = (BattleSide)side;
                bool isPlayer = s == BattleSide.Player;
                int slots = anim.SlotsOf(s);
                // A place the battle doesn't have stands where its side's last does (none in a single or a double moves)
                if (slot >= Math.Max(slots, 2))
                {
                    places.Set(s, slot, places.Feet(s, slots - 1), places.Height(s, slots - 1), places.HalfWidth(s, slots - 1));
                    continue;
                }
                var feet = Spot(s, Math.Min(slot, slots - 1), slots) + new Vector3(0, platformTop, 0);
                float height = (isPlayer ? 1.2f : 2.2f) * SlotFit(anim, s);
                // How wide its body is as the battle turns it, for what is drawn beside it (plan 09 · L2)
                float halfWidth = 0f;
                var shown = slot < slots ? anim[s, slot].Shown : null;
                if (shown != null)
                {
                    var model = PokemonModels.Get(shown.LookName);
                    // A Dynamaxed Pokémon stands giant, and the effects are sized for it (plan 25 · Y15)
                    float scale = ModelScale(model, isPlayer, anim) * BattleAnimator.GiantOf(anim[s, slot], isPlayer);
                    height = model.Height * scale;
                    halfWidth = PokemonSprites.Framing(model, isPlayer ? SpriteView.Back : SpriteView.Front, PokemonSprites.Size).HalfWidth * scale;
                }
                // Behind a doll, a move's effect lands on the doll: the place is as tall as what shows of the two
                if (slot < slots)
                {
                    var look = DollOf(anim[s, slot]);
                    if (look.Visible)
                        height = height * look.PokemonGrow + DollSize(s, anim) * (1f - look.PokemonGrow);
                }
                places.Set(s, slot, feet, height, halfWidth);
            }
    }

    // ------------------------------------------------------------------ offscreen passes

    private void RenderStage(BattleStage stage, BattleAnimator anim, LightRig rig, StageLight stageLight)
    {
        var shaders = context.Shaders;
        var light = rig.Light;
        shaders.SetTime(anim.Time);
        shaders.SetWind(0f);
        shaders.SetWalkers(Array.Empty<Vector3>());

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
        DrawTrainers(CharacterPass.Depth);
        DrawPokemon3D(anim, CharacterPass.Depth);
        DrawDolls(anim, CharacterPass.Depth);
        DrawBalls(CharacterPass.Depth);
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        FrameProfiler.Lap(FrameSection.Shadows);

        shaders.SetLighting(Raymath.MatrixMultiply(lightView, lightProjection), light, camera.Position, context.Shadows.Texel);
        shaders.SetCharacterStyle(shadowStrength: 1f, rimStrength: rig.Rim);
        shaders.SetWorldRamp(ArtLook.BattleRamp);
        shaders.SetFog(rig.FogColor, rig.FogAmount, rig.FogNear, rig.FogFar);
        shaders.SetCloudShade(rig.CloudShade);
        shaders.SetGlow(0f, 0f, Vector3.Zero);
        shaders.SetUpright(0f, 0f);

        // 2. Sky (or the dark behind a cave's walls), then the field
        // (The part of the target dynamic resolution allows, plan 16 · T23: all of it at a step of 1)
        int tw = context.PartWidth, th = context.PartHeight;
        context.BeginScene();
        // The sky, the stage, its lights and whoever stands on it take a move's light; the Pokémon do not (plan 25 · Y16)
        SkyPainter.Draw(anim.Time, tw, th, stageLight.Apply(rig.Sky), clouds: Arena.Outdoors);

        Rlgl.SetClipPlanes(Near, Far);
        Raylib.BeginMode3D(camera);
        Rlgl.DisableBackfaceCulling();
        context.BindShadowMap();

        shaders.SetStageWash(stageLight);
        stage.Draw();
        // A room's windows take the colour of the sky outside; every other arena's lights are their own colour
        stage.DrawLights(rig.LampGlow * stageLight.LightsLeft, Arena.Kind == BattleArena.Indoors ? rig.GlowColor : Vector3.One);
        DrawPokemonShadows(anim);
        DrawTrainers(CharacterPass.Color);
        shaders.SetStageWash(StageLight.None);
        DrawPokemon3D(anim, CharacterPass.Color);
        DrawDolls(anim, CharacterPass.Color);
        DrawBalls(CharacterPass.Color);
        Rlgl.EnableBackfaceCulling();
        DrawTrainers(CharacterPass.Outline);
        DrawPokemon3D(anim, CharacterPass.Outline);
        DrawDolls(anim, CharacterPass.Outline);
        DrawBalls(CharacterPass.Outline);
        FxRenderer.Draw(fx, camera);

        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        context.UnbindShadowMap();
        Rlgl.SetClipPlanes(0.01, 1000.0);
    }

    /// <summary>Soft contact shadows on the platforms under the Pokémon, below their cast shadows.</summary>
    private void DrawPokemonShadows(BattleAnimator anim)
    {
        var tex = SceneTextures.ShadowBlob;
        Rlgl.DisableDepthMask();
        float y = platformTop + 0.01f;

        void Blob(Vector3 spot, float radius)
        {
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

        float foeFit = SlotFit(anim, BattleSide.Enemy), ownFit = SlotFit(anim, BattleSide.Player);
        for (int slot = 0; slot < MostSlotsShown(anim); slot++)
        {
            var foe = anim[BattleSide.Enemy, slot];
            if (slot < anim.SlotsOf(BattleSide.Enemy) && foe.Present && foe.Shown != null && Appear(anim, foe, false).Visible)
                Blob(Spot(anim, BattleSide.Enemy, slot), BattleStage.EnemyPlatformRadius * 0.62f * foeFit * PokemonModels.Get(foe.Shown.LookName).Fill);
            var mine = anim[BattleSide.Player, slot];
            if (slot < anim.SlotsOf(BattleSide.Player) && mine.Present && mine.Shown != null && Appear(anim, mine, true).Visible)
                Blob(Spot(anim, BattleSide.Player, slot), BattleStage.PlayerPlatformRadius * 0.62f * ownFit * PokemonModels.Get(mine.Shown.LookName).Fill);
            for (int side = 0; side < 2; side++)
            {
                var s = (BattleSide)side;
                if (slot >= anim.SlotsOf(s)) continue;
                var look = DollOf(anim[s, slot]);
                if (look.Visible && look.Drop < 0.5f)
                    Blob(DollFeet(s, slot, anim), DollSize(s, anim) * 0.42f * (1f - 0.7f * look.Fade));
            }
        }

        Rlgl.SetTexture(0);
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }

    // ------------------------------------------------------------------ trainers

    /// <summary>A trainer drawn this frame: their rig's type, where they stand and their pose.</summary>
    private readonly record struct TrainerPlacement(string Type, Matrix4x4 Root, CharacterPose Pose);

    private const float PlayerTrainerScale = 1.15f;

    /// <summary>A trainer on their platform throws a ball out, then runs off (<paramref name="exit"/> seconds since the send-out).</summary>
    private void PlaceOnPlatform(string type, Vector3 spot, float yaw, float scale, float exit, float direction, float time)
    {
        var pose = new CharacterPose { Time = time, Blink = time % 3.7f < 0.12f };
        var feet = spot + new Vector3(0, platformTop, 0);
        if (exit >= 0f && exit < ThrowLeadIn)
        {
            // The overarm throw, already wound up as the message appears
            pose.Emote = Emote.Throw;
            pose.EmoteTime = exit + EmoteSkip;
        }
        else if (exit >= ThrowLeadIn)
        {
            float p = Math.Clamp((exit - ThrowLeadIn) / (BattleAnimator.TrainerExitTime - ThrowLeadIn), 0f, 1f);
            feet.X += direction * p * p * 9f * scale;
            yaw = direction > 0 ? MathF.PI / 2f : -MathF.PI / 2f;
            pose.Walk = exit * 2.6f;
            pose.WalkBlend = 1f;
            pose.Running = true;
        }
        trainers.Add(new TrainerPlacement(type, Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(feet), pose));
    }

    // A send-out's throw starts part-way into the emote, so the ball leaves the hand 0.12 s after the message
    private const float EmoteSkip = 0.18f;
    private const float ThrowLeadIn = 0.25f;

    /// <summary>When a trainer's send-out ball leaves their hand.</summary>
    private const float SendOutRelease = CharacterAnimation.ThrowRelease - EmoteSkip;

    /// <summary>The way a thrower at the throw spot faces: toward the foe.</summary>
    private static float ThrowYaw(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    private void PlaceTrainers(BattleAnimator anim)
    {
        trainers.Clear();
        if (anim.PartnerTrainer != null)
        {
            // Beside a partner the player stands behind their own Pokémon's place and the partner behind theirs (a tag
            // battle), and each steps off their own side as the Pokémon are sent out
            if (anim.PlayerTrainer != null)
                PlaceOnPlatform(anim.PlayerTrainer, Spot(BattleSide.Player, 0, 2), MathF.PI, PlayerTrainerScale, anim.PlayerTrainerExit, -1f, anim.Time);
            PlaceOnPlatform(anim.PartnerTrainer, Spot(BattleSide.Player, 1, 2), MathF.PI, PlayerTrainerScale, anim.PlayerTrainerExit, 1f, anim.Time + 0.7f);
        }
        else if (anim.PlayerTrainer != null)
            PlaceOnPlatform(anim.PlayerTrainer, BattleStage.PlayerSpot, MathF.PI, PlayerTrainerScale, anim.PlayerTrainerExit, -1f, anim.Time);
        if (anim.EnemyTrainer2 != null)
        {
            // Two trainers stand side by side, each behind their Pokémon's place
            if (anim.EnemyTrainer != null)
                PlaceOnPlatform(anim.EnemyTrainer, Spot(BattleSide.Enemy, 0, 2), 0f, 2.1f, anim.EnemyTrainerExit, 1f, anim.Time + 1.3f);
            PlaceOnPlatform(anim.EnemyTrainer2, Spot(BattleSide.Enemy, 1, 2), 0f, 2.1f, anim.EnemyTrainerExit, -1f, anim.Time + 0.6f);
        }
        else if (anim.EnemyTrainer != null)
            PlaceOnPlatform(anim.EnemyTrainer, BattleStage.EnemySpot, 0f, 2.3f, anim.EnemyTrainerExit, 1f, anim.Time + 1.3f);

        // A trainer back on their platform to say a line, and off again once it is read (plan 11 · C13)
        if (anim.Speaking is { } speaking)
            trainers.Add(Speaker(speaking, anim.EnemyTrainer2Look != null, platformTop, anim.Time));

        // The player runs back in to throw a ball at a wild Pokémon, and off again
        if (anim.Ball is { } ball && anim.PlayerTrainer == null && ball.Age < CaptureThrowerGone)
            trainers.Add(CaptureThrower(ball, anim.Time));
    }

    /// <summary>
    /// A trainer who steps back to speak (plan 11 · C13): walking in from beside their platform to its edge, turned
    /// to the field with the line's face and its gesture once there, and walking off the way they came once it is
    /// read. A function of <paramref name="speaking"/>'s clock alone, which <see cref="SpeakerAt"/> shares with the camera.
    /// </summary>
    internal static (Vector3 Feet, float Yaw, float Scale, CharacterPose Pose) SpeakerAt(SpeakingTrainer speaking, bool two, float platformTop, float time)
    {
        float scale = two ? 2.1f : 2.3f;
        float direction = speaking.Second ? -1f : 1f;
        var pose = new CharacterPose
        {
            Time = time + (speaking.Second ? 0.6f : 1.3f),
            Blink = time % 3.7f < 0.12f,
            Expression = CharacterAnimation.ExpressionOf(speaking.Mood)
        };
        var feet = BattleStage.SpeakerSpot(speaking.Second) + new Vector3(0, platformTop, 0);
        float back = speaking.Back;
        // Turned a little toward the player's side of the field while they speak
        float yaw = -direction * 0.35f;
        if (back < 1f)
        {
            // Eased in and out, as a walk would slow to a stop
            float off = 1f - back;
            feet.X += direction * off * off * BattleStage.SpeakerFrom;
            yaw = (speaking.LeaveAge >= 0f ? direction : -direction) * MathF.PI / 2f;
            pose.Walk = (speaking.LeaveAge >= 0f ? speaking.LeaveAge : speaking.Age) * 2.6f;
            pose.WalkBlend = Math.Clamp(off * 3f, 0f, 1f);
        }
        else if (speaking.Gesture is { } gesture && speaking.GestureAge >= 0f && speaking.GestureAge < Acting.Length(gesture))
        {
            CharacterAnimation.Act(ref pose, gesture, speaking.GestureAge);
            if (gesture == Overworld.Gesture.Jump) feet.Y += CharacterAnimation.JumpLift(speaking.GestureAge) / 32f * scale;
        }
        return (feet, yaw, scale, pose);
    }

    private static TrainerPlacement Speaker(SpeakingTrainer speaking, bool two, float platformTop, float time)
    {
        var (feet, yaw, scale, pose) = SpeakerAt(speaking, two, platformTop, time);
        return new TrainerPlacement(speaking.Look, Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(feet), pose);
    }

    private const float RunInTime = 0.3f, RunOffAt = 1.05f, CaptureThrowerGone = 1.6f;

    private static TrainerPlacement CaptureThrower(BallThrowView ball, float time) => CaptureThrowerAt(ball.Age, ball.Slot, time);

    private static TrainerPlacement CaptureThrowerAt(float age, int slot, float time)
    {
        var pose = new CharacterPose { Time = time, Blink = time % 3.7f < 0.12f };
        var feet = BattleStage.ThrowSpot;
        float yaw = ThrowYaw(BattleStage.ThrowSpot, BattleStage.EnemySpot);
        if (age < RunInTime)
        {
            float p = age / RunInTime;
            feet.X -= (1f - p) * (1f - p) * 7f;
            yaw = MathF.PI / 2f;
            pose.Walk = age * 2.6f;
            pose.WalkBlend = 1f - Math.Clamp((p - 0.8f) / 0.2f, 0f, 1f);
            pose.Running = true;
        }
        else if (age < RunOffAt)
        {
            pose.Emote = Emote.Throw;
            pose.EmoteTime = age - RunInTime;
        }
        else
        {
            float p = (age - RunOffAt) / (CaptureThrowerGone - RunOffAt);
            feet.X -= p * p * 7f;
            yaw = -MathF.PI / 2f;
            pose.Walk = age * 2.6f;
            pose.WalkBlend = 1f;
            pose.Running = true;
        }
        return new TrainerPlacement(PlayerIdentity.Character, Matrix4x4.CreateScale(PlayerTrainerScale) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(feet), pose);
    }

    private void DrawTrainers(CharacterPass pass)
    {
        foreach (var t in trainers)
            CharacterRenderer.Draw(context, CharacterModels.Get(t.Type, context.Shaders), t.Pose, t.Root, pass);
    }

    /// <summary>Where a trainer's right hand is in this pose (the ball rests there until it is thrown).</summary>
    private Vector3 HandOf(string type, Matrix4x4 root, CharacterPose pose)
    {
        var rig = CharacterModels.Get(type, context.Shaders);
        if (rig.Skeleton.Count <= HumanBones.HandR) return Vector3.Transform(new Vector3(0, 1.3f, 0.2f), root);
        rig.Animate(pose);
        var hand = rig.Skeleton[HumanBones.HandR].Joint;
        return Vector3.Transform(hand, rig.Skin[HumanBones.HandR] * Matrix4x4.CreateScale(rig.Scale) * root);
    }

    // ------------------------------------------------------------------ Poké Balls

    private readonly record struct BallDraw(BallModel Model, Matrix4x4 World, float Open);

    // A ball grows as it flies from a hand to a platform; the foe's ball ends larger, as it is farther away
    private const float HandBall = 0.17f, EnemyBall = 0.42f, PlayerBall = 0.3f;

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Turns a ball's button toward the camera's side of the field.</summary>
    private static float FaceCamera(Vector3 at) => MathF.Atan2(BattleStage.CameraPosition.X - at.X, BattleStage.CameraPosition.Z - at.Z);

    /// <summary>Works out every ball in view this frame (send-outs and a thrown ball) and the light they give off.</summary>
    private void PlaceBalls(BattleAnimator anim)
    {
        balls.Clear();
        for (int side = 0; side < 2; side++)
            for (int slot = 0; slot < anim.SlotsOf((BattleSide)side); slot++)
            {
                PlaceSendOutBall(anim, (BattleSide)side, slot);
                PlaceRecallBall(anim, (BattleSide)side, slot);
            }
        if (anim.Ball != null) PlaceCaptureBall(anim, anim.Ball);
    }

    /// <summary>A Pokémon being sent out: the ball flies from the trainer's hand (or from off the field) and bursts open.</summary>
    private void PlaceSendOutBall(BattleAnimator anim, BattleSide side, int slot)
    {
        var v = anim[side, slot];
        float age = v.SendOutAge;
        if (age < 0f || age > BattleAnimator.SendOutBallTime + 0.3f) return;
        // A Pokémon breaking out of a thrown ball has no flight of its own
        if (side == BattleSide.Enemy && anim.Ball != null && anim.Ball.Slot == slot) return;

        bool isPlayer = side == BattleSide.Player;
        float h = places.Height(side, slot);
        var feet = places.Feet(side, slot);
        float size = isPlayer ? PlayerBall : EnemyBall;
        var model = BattleBall.Get(v.Ball);

        // From the hand of the trainer sending it out, if they are still on the platform
        TrainerPlacement? thrower = null;
        float exit = isPlayer ? anim.PlayerTrainerExit : anim.EnemyTrainerExit;
        if (exit >= 0f && MathF.Abs(exit - age) < 0.05f && trainers.Count > 0)
        {
            // (The list is the player, a partner, then the trainers across the field: PlaceTrainers)
            int mine = (anim.PlayerTrainer != null ? 1 : 0) + (anim.PartnerTrainer != null ? 1 : 0);
            int index = isPlayer ? (slot == 1 && anim.PartnerTrainer != null ? mine - 1 : 0) : mine + (anim.EnemyTrainer2 != null && slot == 1 ? 1 : 0);
            if (index < trainers.Count) thrower = trainers[index];
        }

        // It opens over the place, or in front of a trainer who is still stepping off it (so they don't hide it)
        var open = feet + Vector3.UnitY * h * 0.45f;
        if (thrower != null)
        {
            float radius = isPlayer ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius;
            var toCamera = Vector3.Normalize(new Vector3(BattleStage.CameraPosition.X - feet.X, 0, BattleStage.CameraPosition.Z - feet.Z));
            open += toCamera * radius * 0.6f;
        }

        if (age < BattleAnimator.SendOutBallTime)
        {
            Vector3 from;
            float start = 0f;
            if (thrower is { } t)
            {
                var releasePose = t.Pose with { Emote = Emote.Throw, EmoteTime = CharacterAnimation.ThrowRelease };
                from = HandOf(t.Type, t.Root, releasePose);
                start = SendOutRelease;
                if (age < start)
                {
                    balls.Add(new BallDraw(model, BattleBall.Place(HandOf(t.Type, t.Root, t.Pose), HandBall, FaceCamera(from)), 0f));
                    return;
                }
            }
            else from = FarSpot(side);

            float f = (age - start) / (BattleAnimator.SendOutBallTime - start);
            var at = BattleBall.Arc(from, open, h * 0.3f + 0.25f, f);
            balls.Add(new BallDraw(model, BattleBall.Place(at, HandBall + (size - HandBall) * f, FaceCamera(at), f * MathF.Tau * 2f), 0f));
            return;
        }

        // Burst open: the lid flies up and the ball shrinks away in the light the Pokémon grows out of
        float k = (age - BattleAnimator.SendOutBallTime) / 0.3f;
        balls.Add(new BallDraw(model, BattleBall.Place(open, size * (1f - k), FaceCamera(open)), Smooth(0f, 0.3f, k)));
        BattleBall.ReleaseOf(model.Look).Emit(fx, open, h, age - BattleAnimator.SendOutBallTime, (int)side * 7 + slot);
    }

    /// <summary>Where a ball comes from when nobody throws it: off the field on its side (the trainer has stepped off the platform).</summary>
    private static Vector3 FarSpot(BattleSide side) =>
        side == BattleSide.Player ? BattleStage.PlayerSpot + new Vector3(-8f, 4f, 5f) : BattleStage.EnemySpot + new Vector3(9f, 7f, -6f);

    /// <summary>A Pokémon called back (plan 25 · Y4): its own ball flies in, opens, draws it in with a red beam, shuts and flies out.</summary>
    private void PlaceRecallBall(BattleAnimator anim, BattleSide side, int slot)
    {
        var v = anim[side, slot];
        if (v.RecallAge < 0f) return;
        bool isPlayer = side == BattleSide.Player;
        float h = places.Height(side, slot);
        var feet = places.Feet(side, slot);
        float size = isPlayer ? PlayerBall : EnemyBall;
        var model = BattleBall.Get(v.Ball);
        var toCamera = Vector3.Normalize(new Vector3(BattleStage.CameraPosition.X - feet.X, 0, BattleStage.CameraPosition.Z - feet.Z));
        var open = places.Body(side, slot) + Vector3.UnitY * h * 0.55f + toCamera * h * 0.1f;
        var step = BallRecall.At(v.RecallAge, BattleAnimator.RecallTime, FarSpot(side), open, h, size, HandBall);
        if (!step.Visible) return;
        balls.Add(new BallDraw(model, BattleBall.Place(step.At, step.Size, FaceCamera(step.At), 0f), step.Open));
        BallRecall.Beam(fx, step.Beam, anim.Time, step.At, places.Body(side, slot), h);
    }

    /// <summary>A ball thrown at a wild Pokémon, through every step of its timeline.</summary>
    private void PlaceCaptureBall(BattleAnimator anim, BallThrowView ball)
    {
        var model = BattleBall.Get(ball.Ball);
        float h = places.Height(BattleSide.Enemy, ball.Slot);
        var feet = places.Feet(BattleSide.Enemy, ball.Slot);
        var toCamera = Vector3.Normalize(new Vector3(BattleStage.CameraPosition.X - feet.X, 0, BattleStage.CameraPosition.Z - feet.Z));
        var open = BattleBall.CaptureOpen(places, ball.Slot);
        var rest = feet + toCamera * 0.4f + Vector3.UnitY * EnemyBall * 0.5f;
        float yaw = FaceCamera(rest);
        var (phase, p, _) = BattleAnimator.BallStage(ball.Age, ball.Shakes);

        switch (phase)
        {
            case BallPhase.RunIn:
            {
                var thrower = CaptureThrowerAt(ball.Age, ball.Slot, anim.Time);
                balls.Add(new BallDraw(model, BattleBall.Place(HandOf(thrower.Type, thrower.Root, thrower.Pose), HandBall, yaw), 0f));
                break;
            }
            case BallPhase.Flight:
            {
                var release = CaptureThrowerAt(BattleAnimator.BallReleaseTime, ball.Slot, anim.Time);
                var from = HandOf(release.Type, release.Root, release.Pose);
                var at = BattleBall.Arc(from, open, BattleBall.CaptureArc, p);
                balls.Add(new BallDraw(model, BattleBall.Place(at, HandBall + (EnemyBall - HandBall) * p, yaw, p * MathF.Tau * 3f), 0f));
                break;
            }
            case BallPhase.Open:
            {
                // It pops open over the foe and a red light draws it in, then it shuts
                float lid = Smooth(0f, 0.18f, p) * (1f - Smooth(0.78f, 1f, p));
                var at = open + Vector3.UnitY * 0.08f * MathF.Sin(p * MathF.PI);
                balls.Add(new BallDraw(model, BattleBall.Place(at, EnemyBall, yaw), lid));
                float pull = Fx.Envelope(p, 0.12f, 0.25f, 0.6f, 0.8f);
                if (pull > 0f)
                {
                    var body = places.Body(BattleSide.Enemy, ball.Slot);
                    for (int k = 0; k < 3; k++)
                    {
                        var (across, up) = Fx.Frame(body - at);
                        var pts = new Vector3[6];
                        for (int i = 0; i < pts.Length; i++)
                        {
                            float u = i / (float)(pts.Length - 1);
                            float wave = MathF.Sin(u * MathF.PI) * h * 0.25f * MathF.Sin(anim.Time * 9f + k * 2.1f + u * 4f);
                            pts[i] = Vector3.Lerp(at, body, u) + (k == 1 ? up : across) * wave * (k == 2 ? -1f : 1f);
                        }
                        fx.Ribbon(pts, h * 0.07f * pull, new Color(255, 80, 80, (int)(220 * pull)));
                    }
                    fx.Sprite(FxShape.Glow, at, h * 0.6f * pull, new Color(255, 90, 90, (int)(200 * pull)));
                }
                break;
            }
            case BallPhase.Drop:
            {
                // Falls onto the platform with a little bounce
                float fall = p < 0.75f ? (p / 0.75f) * (p / 0.75f) : 1f;
                var at = Vector3.Lerp(open, rest, fall);
                if (p >= 0.75f) at.Y += MathF.Sin((p - 0.75f) / 0.25f * MathF.PI) * 0.12f;
                balls.Add(new BallDraw(model, BattleBall.Place(at, EnemyBall, yaw), 0f));
                if (p >= 0.75f) Fx.Burst(fx, 431, (p - 0.75f) * BattleAnimator.BallDropTime, rest - Vector3.UnitY * EnemyBall * 0.4f, 1f, 0f, 0.4f, 5,
                    FxShape.Smoke, new Color(220, 212, 196, 150), new Color(196, 188, 172, 120), 0.12f, 0.4f, -0.2f, FxBlend.Alpha);
                break;
            }
            case BallPhase.Wobble:
            {
                float rock = MathF.Sin(p * MathF.Tau) * 0.5f * (1f - 0.3f * p);
                balls.Add(new BallDraw(model, BattleBall.Place(rest, EnemyBall, yaw, 0f, rock), 0f));
                break;
            }
            case BallPhase.Pause:
            case BallPhase.Rest:
                balls.Add(new BallDraw(model, BattleBall.Place(rest, EnemyBall, yaw), 0f));
                break;
            case BallPhase.Click:
            {
                // Click: stars pop out of the ball
                balls.Add(new BallDraw(model, BattleBall.Place(rest, EnemyBall, yaw), 0f));
                for (int i = 0; i < 3; i++)
                {
                    float a = -0.7f + i * 0.7f;
                    var dir = new Vector3(MathF.Sin(a) * 0.9f, MathF.Cos(a), 0);
                    fx.Sprite(FxShape.Star, rest + dir * (0.25f + 0.6f * Fx.EaseOut(p)), 0.12f * (1f - p * 0.5f), new Color(255, 230, 110, (int)(255 * (1f - p))), FxBlend.Alpha);
                }
                Fx.Glint(fx, p * BattleAnimator.BallEndTime, rest + new Vector3(0, 0, EnemyBall * 0.5f), 1f, 0f, Color.White, 0.25f);
                break;
            }
            case BallPhase.Burst:
            {
                // It bursts open in a flash of light as the Pokémon breaks free
                float k = Math.Min(1f, p / 0.4f);
                if (k < 1f) balls.Add(new BallDraw(model, BattleBall.Place(rest, EnemyBall * (1f - k), yaw), 1f));
                // In the release of the ball it was thrown in (plan 25 · Y4)
                BattleBall.ReleaseOf(model.Look).Emit(fx, rest + Vector3.UnitY * h * 0.2f, h, p * BattleAnimator.BallEndTime, 77);
                break;
            }
        }
    }

    private void DrawBalls(CharacterPass pass)
    {
        foreach (var b in balls) BattleBall.Draw(context, b.Model, b.World, b.Open, pass);
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
        public float Faint;           // 0..1 of the faint clip
        public float Flash;
        public Color FlashColor;
        public float Silhouette;      // 0..1 dark silhouette of a wild Pokémon before the camera settles
        public float Entry;           // 0..1 of the entry clip as it comes out of the ball
        public float ArriveBack;      // 0..1 of its way still to come from behind the platform, an ally answering a call (plan 14 · B5)
        public float ArriveHop;       // 0..1 of its hop's height on the way
    }

    private static Appearance Appear(BattleAnimator anim, CombatantView v, bool isPlayer)
    {
        var a = new Appearance { Grow = 1f, FlashColor = Color.White, Visible = true };
        bool fainting = v.FaintAge >= 0f, recalling = v.RecallAge >= 0f, capturing = v.CaptureAge >= 0f;
        // Out of sight for its move's turn, once it has finished leaving (plan 25 · Y16)
        if (v.Shown == null || (v.Away && v.AwayAge < 0f) || (!v.Present && !fainting && !recalling && !capturing))
        {
            a.Visible = false;
            return a;
        }

        // Out of the ball once it opens: grow out of a flash of light (nothing shows while the ball flies)
        if (v.SendOutAge >= 0f)
        {
            float e = BattleAnimator.Emerge(v.SendOutAge);
            if (e < 0f)
            {
                a.Visible = false;
                return a;
            }
            a.Grow = 0.12f + 0.88f * EaseOutBack(e);
            a.Flash = 1f - e;
            a.Entry = e;
        }

        // An ally that answered a call hops in from behind the platform and lands on its place (plan 14 · B5): BattleAllies has the curve
        if (v.ArriveAge >= 0f)
        {
            var hop = BattleAllies.Hop(v.ArriveAge);
            a.ArriveBack = hop.Back;
            a.ArriveHop = hop.Height;
            a.Grow *= hop.Grow;
        }

        // A physical move lunges toward the opponent and back, in step with the clip's strike
        float p = BattleAnimator.Progress(v.AttackAge, BattleAnimator.AttackTime);
        if (p >= 0f && v.AttackCategory == MoveCategory.Physical) a.Lunge = PokemonAnimation.Bump(p, 0.28f, 0.5f, 0.85f);

        // Hit: flicker and shake
        p = BattleAnimator.Progress(v.HitAge, BattleAnimator.HitTime);
        if (p >= 0f)
        {
            a.Visible = (int)(p * 10f) % 2 == 0;
            a.Shake = MathF.Sin(p * 48f) * (1f - p);
        }

        // Faint: the clip collapses it, then it sinks into the platform, which hides it as it goes
        if (fainting && v.FaintDelay <= 0f)
        {
            p = Math.Clamp(v.FaintAge / BattleAnimator.FaintTime, 0f, 1f);
            a.Faint = Math.Min(1f, p / 0.62f);
            float sink = Math.Clamp((p - 0.6f) / 0.4f, 0f, 1f);
            a.Sink = sink * sink;
        }

        // Recall or capture: shrink away into a red beam of light
        float shrink = Math.Max(BallRecall.Shrink(v.RecallAge, BattleAnimator.RecallTime),
            capturing && v.CaptureDelay <= 0f ? BattleAnimator.Progress(v.CaptureAge, BattleAnimator.CaptureTime) : -1f);
        if (shrink >= 0f)
        {
            a.Grow *= 1f - shrink;
            a.Flash = Math.Min(1f, shrink * 1.6f);
            a.FlashColor = new Color(255, 70, 70, 255);
        }

        // A Substitute's doll stands in front of it: it shrinks out as the doll drops in and grows back as it fades
        // (a Pokémon that faints or is called back behind one shows as it goes)
        if (v.Substitute != null && !fainting && !recalling) a.Grow *= DollOf(v).PokemonGrow;

        // Wild Pokémon start as a dark silhouette while the camera sweeps in
        if (!isPlayer && anim.EnemyTrainer == null && anim.Time < 1.6f && v.SendOutAge < 0f)
            a.Silhouette = 1f - Math.Clamp((anim.Time - 0.7f) / 0.8f, 0f, 1f);

        if (a.Grow <= 0.01f) a.Visible = false;
        return a;
    }

    private static readonly Color SilhouetteColor = new(34, 30, 46, 255);

    /// <summary>What a Mega Evolution, an Ultra Burst or the Z-Power playing on this place shows of the body itself (plan 25 · Y14).</summary>
    private static (Color Color, float Amount) ChangeCast(BattleAnimator anim, BattleSide side, int slot)
    {
        (Color Color, float Amount) strongest = (default, 0f);
        foreach (var cue in anim.Cues)
        {
            if (cue.Kind is not (CueKind.Mega or CueKind.UltraBurst or CueKind.ZPower or CueKind.Dynamax or CueKind.DynamaxEnd or CueKind.Tera)
                || cue.ToSide != side || cue.ToSlot != slot) continue;
            var cast = MoveFx.BodyCast(cue);
            if (cast.Amount > strongest.Amount) strongest = cast;
        }
        return strongest;
    }

    /// <summary>
    /// The Pokémon as a 3D model standing on its platform, lit and shadowed with the scene. It is sized
    /// so it covers the same part of the screen as the sprite would.
    /// </summary>
    private void DrawPokemon3D(BattleAnimator anim, CombatantView v, bool isPlayer, int slot, CharacterPass pass)
    {
        if (v.Shown == null) return;
        var a = Appear(anim, v, isPlayer);
        if (!a.Visible) return;

        var model = PokemonModels.Get(v.Shown.LookName);
        var view = isPlayer ? SpriteView.Back : SpriteView.Front;
        var framing = PokemonSprites.Framing(model, view, PokemonSprites.Size);
        var spot = Spot(anim, isPlayer ? BattleSide.Player : BattleSide.Enemy, slot);
        var other = isPlayer ? BattleStage.EnemySpot : BattleStage.PlayerSpot;

        // A Dynamaxed Pokémon stands giant (plan 25 · Y15): the lunge and the shake grow with it
        float scale = ModelScale(model, isPlayer, anim) * BattleAnimator.GiantOf(v, isPlayer);
        float frame = scale * framing.WorldPerPixel * PokemonSprites.Size;
        var toward = Vector3.Normalize(other - spot);
        var feet = spot + new Vector3(0, platformTop, 0)
            + toward * a.Lunge * frame * (22f / 128f)
            + Vector3.UnitX * a.Shake * frame * (4f / 128f)
            - Vector3.UnitY * a.Sink * frame
            // An ally hopping in comes from behind its place, away from the other side, in an arc (plan 14 · B5)
            - toward * a.ArriveBack * frame * BattleAllies.HopFrom
            + Vector3.UnitY * a.ArriveHop * frame * BattleAllies.HopHeight;
        // A dodge takes the whole body aside, away from the miss's path, and back (plan 25 · Y5)
        float dodge = Math.Max(0f, BattleAnimator.Progress(v.DodgeAge, BattleAnimator.DodgeTime));
        if (dodge > 0f)
        {
            var from = Spot(anim, v.DodgeFromSide, v.DodgeFromSlot);
            feet += PokemonAnimation.DodgeAway(from, spot) * PokemonAnimation.DodgeAside(model, dodge) * model.Height * scale;
        }
        // What a move does to the body itself (plan 25 · Y16): a rush, a leap, a spin, a squash, a squeeze, a collapse,
        // and its going and coming back for a turn out of sight, in its own height
        var motion = BodyMotions.Of(anim, isPlayer ? BattleSide.Player : BattleSide.Enemy, slot);
        float body = model.Height * scale;
        feet += new Vector3(toward.X, 0f, toward.Z) * motion.Toward * body + Vector3.UnitY * (motion.Up - motion.Sink) * body;
        var (width, tall) = motion.Shape;
        float size = scale * a.Grow * motion.Grow;
        if (size <= 0.001f) return;
        var root = Matrix4x4.CreateScale(size * width, size * tall, size * width) * Matrix4x4.CreateRotationY(framing.Yaw + motion.Spin) * Matrix4x4.CreateTranslation(feet);

        float phase = (isPlayer ? 0.37f : 0f) + slot * 0.53f;
        var pose = new PokePose
        {
            Time = anim.Time + phase,
            Blink = (anim.Time + phase * 3f) % 3.3f < 0.12f ? 1f : 0f,
            Attack = Math.Max(0f, BattleAnimator.Progress(v.AttackAge, BattleAnimator.AttackTime)),
            Kind = v.AttackCategory,
            Hurt = Math.Max(0f, BattleAnimator.Progress(v.HitAge, BattleAnimator.HitTime)),
            Faint = a.Faint,
            Entry = a.Entry,
            // The body's own part of a condition that lasts (plan 09 · L2): as the lines have told of it, never the rules
            Asleep = v.Condition.Status == StatusCondition.Sleep,
            Frozen = v.Condition.Status == StatusCondition.Freeze,
            // A battle's answers (plan 25 · Y5), as the lines and the bar have told of them
            Dodge = dodge,
            Tired = v.Tired,
            Cheer = Math.Max(0f, BattleAnimator.Progress(v.CheerAge, BattleAnimator.CheerTime)),
            Charge = BattleAnimator.Gathered(v),
            Stat = BattleAnimator.StatShown(v)
        };

        if (pass == CharacterPass.Color)
        {
            // A change of shape's light, or the Z-Power's cast (plan 25 · Y14), gives way only to a send-out's or a recall's flash
            var change = ChangeCast(anim, isPlayer ? BattleSide.Player : BattleSide.Enemy, slot);
            // A giant's red pulse or a Terastallized body's sheen (plan 25 · Y15), under any cue's own
            var giant = MoveFx.GiantCast(v, anim.Time);
            if (giant.Amount > change.Amount) change = giant;
            // The ball's flash on the body, a quarter at most under the options' REDUCE FLASHES (plan 12 · Q9)
            if (a.Flash > 0f) context.Shaders.SetFlash(a.FlashColor, Core.Accessibility.Flash(Math.Clamp(a.Flash, 0f, 1f) * 0.85f));
            else if (change.Amount > 0f) context.Shaders.SetFlash(change.Color, Math.Clamp(change.Amount, 0f, 1f));
            else if (a.Silhouette > 0f) context.Shaders.SetFlash(SilhouetteColor, a.Silhouette * 0.92f);
            else
            {
                // A condition's cast, blended into its own colours: it gives way to the flashes above
                var cast = MoveFx.Cast(v.Condition);
                if (cast.Amount > 0f) context.Shaders.SetFlash(cast.Color, Math.Clamp(cast.Amount, 0f, 1f));
            }
        }
        PokemonRenderer.Draw(context, model, pose, root, pass);
        if (pass == CharacterPass.Color) context.Shaders.SetFlash(default, 0f);
    }

    private void DrawPokemon3D(BattleAnimator anim, CharacterPass pass)
    {
        for (int slot = 0; slot < MostSlotsShown(anim); slot++)
        {
            if (slot < anim.SlotsOf(BattleSide.Enemy)) DrawPokemon3D(anim, anim[BattleSide.Enemy, slot], isPlayer: false, slot, pass);
            if (slot < anim.SlotsOf(BattleSide.Player)) DrawPokemon3D(anim, anim[BattleSide.Player, slot], isPlayer: true, slot, pass);
        }
    }

    // ------------------------------------------------------------------ the Substitute's doll (plan 25 · Y1)

    /// <summary>How a place's doll looks this frame (none on a Pokémon that is fainting).</summary>
    private static DollLook DollOf(CombatantView v) =>
        v.FaintAge >= 0f ? DollLook.None : SubstituteDoll.LookOf(v.Substitute);

    /// <summary>How tall a doll stands on its side's platform.</summary>
    private static float DollSize(BattleSide side, BattleAnimator anim) =>
        SubstituteDoll.SizeOn(side == BattleSide.Player ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius, SlotFit(anim, side));

    /// <summary>Where a doll's feet are on the platform: on its Pokémon's spot, a little toward the other side.</summary>
    private Vector3 DollFeet(BattleSide side, int slot, BattleAnimator anim)
    {
        var spot = Spot(anim, side, slot);
        var other = side == BattleSide.Player ? BattleStage.EnemySpot : BattleStage.PlayerSpot;
        var toward = Vector3.Normalize(new Vector3(other.X - spot.X, 0, other.Z - spot.Z));
        return spot + new Vector3(0, platformTop, 0) + toward * DollSize(side, anim) * 0.15f;
    }

    private void DrawDolls(BattleAnimator anim, CharacterPass pass)
    {
        for (int slot = 0; slot < MostSlotsShown(anim); slot++)
            for (int side = 0; side < 2; side++)
            {
                var s = (BattleSide)side;
                if (slot >= anim.SlotsOf(s)) continue;
                var look = DollOf(anim[s, slot]);
                if (!look.Visible) continue;
                var feet = DollFeet(s, slot, anim);
                var spot = Spot(anim, s, slot);
                var other = s == BattleSide.Player ? BattleStage.EnemySpot : BattleStage.PlayerSpot;
                SubstituteDoll.Draw(context, SubstituteDoll.Place(feet, other - spot, DollSize(s, anim), look), pass, look.Fade);
            }
    }

    /// <summary>The dust a doll lands in, the stars of its flinch and the puff it fades into.</summary>
    private void DollEffects(BattleAnimator anim)
    {
        for (int slot = 0; slot < MostSlotsShown(anim); slot++)
            for (int side = 0; side < 2; side++)
            {
                var s = (BattleSide)side;
                if (slot >= anim.SlotsOf(s)) continue;
                var v = anim[s, slot];
                if (v.Substitute == null || v.FaintAge >= 0f) continue;
                SubstituteDoll.Effects(v.Substitute, DollFeet(s, slot, anim), DollSize(s, anim), fx);
            }
    }
}
