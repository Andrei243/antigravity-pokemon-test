using System;
using System.Numerics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The 3D battle field's layout and its built scenery. The opponent's platform sits at the origin; the player's
/// is closer to the camera, so it appears lower-left and larger, as in the DS games. What surrounds them depends on
/// the arena (<see cref="BattleArenas"/>).
/// </summary>
internal sealed class BattleStage
{
    public static readonly Vector3 EnemySpot = Vector3.Zero;
    public static readonly Vector3 PlayerSpot = new(-6.7f, 0, 15.6f);
    public const float EnemyPlatformRadius = 2.4f;
    public const float PlayerPlatformRadius = 1.2f;

    /// <summary>Where the player runs in to throw a ball at a wild Pokémon: left of their own platform.</summary>
    public static readonly Vector3 ThrowSpot = PlayerSpot + new Vector3(-1.7f, 0, 0.5f);

    /// <summary>Height of the grass platforms' tops; other arenas raise or lower theirs (<see cref="PlatformTopOf"/>).</summary>
    public const float PlatformHeight = 0.16f;

    // The overview camera: pitched 9° down with a 24° field of view, which puts the opponent's feet at about
    // (1430, 400) and the player's at (470, 800) on the 1920x1080 screen (see BattleCamera for the other shots)
    public static readonly Vector3 CameraPosition = new(-4.72f, 2.6f, 25.4f);
    public const float PitchDeg = 9f;
    public const float FovYDeg = 24f;

    /// <summary>Where the Pokémon stand: the top of the arena's platforms.</summary>
    public static float PlatformTopOf(BattleArena arena) => arena switch
    {
        BattleArena.Indoors => 0.05f,
        BattleArena.Gym or BattleArena.League => 0.3f,
        BattleArena.Cave => PlatformHeight + 0.06f,
        _ => PlatformHeight
    };

    private readonly SceneMeshes meshes;

    public ArenaSpec Spec { get; }

    private BattleStage(SceneMeshes meshes, ArenaSpec spec)
    {
        this.meshes = meshes;
        Spec = spec;
    }

    public void Draw() => meshes.Draw();
    public void DrawDepth() => meshes.DrawDepth();

    /// <summary>Adds the arena's own lights (crystals, lamps, glowing rims) on top of the scene.</summary>
    public void DrawLights(float level, Vector3 tint) => meshes.DrawLights(level, 0f, tint);

    public static BattleStage Build(FieldShaders shaders, ArenaSpec spec)
    {
        var batches = new MeshBatches();
        BattleArenas.Build(batches, spec);
        // Seen from the trainer's shoulder, the stage is one thing behind another all the way to the horizon
        return new BattleStage(SceneMeshes.Upload(batches, shaders, prepassEverything: true), spec);
    }
}
