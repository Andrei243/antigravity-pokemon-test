using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

public class Player
{
    public const int TileSize = 32; // Full HD 32x32 grid tile size

    public int GridX { get; set; } = 8;
    public int GridY { get; set; } = 8;
    public Direction Facing { get; set; } = Direction.Down;

    public float PixelX { get; private set; }
    public float PixelY { get; private set; }

    /// <summary>Height of the ledge-hop arc above the ground, in pixels (0 when on the ground).</summary>
    public float HopHeight { get; private set; }

    /// <summary>Current walk-cycle frame: 0/2 standing, 1/3 mid-step.</summary>
    public int AnimFrame => animFrame;

    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsHoppingLedge { get; private set; }
    private float moveProgress = 0f;
    private int targetGridX = 0;
    private int targetGridY = 0;
    private float animTimer = 0f;
    private int animFrame = 0;

    // Walking into something plays one thud per step-length, not one per frame
    private const float BumpInterval = 0.4f;
    private float bumpCooldown = 0f;

    // Grass stepping effects
    public bool InTallGrass { get; private set; }
    public float GrassRustleTimer { get; private set; }

    public Player(int startX, int startY)
    {
        SetPosition(startX, startY, Direction.Down);
    }

    public void SetPosition(int gx, int gy, Direction facing)
    {
        GridX = gx;
        GridY = gy;
        targetGridX = gx;
        targetGridY = gy;
        Facing = facing;
        PixelX = gx * TileSize;
        PixelY = gy * TileSize;
        HopHeight = 0f;
        IsMoving = false;
        IsHoppingLedge = false;
        moveProgress = 0f;
    }

    public void Update(float dt, Map map, Action<WildEncounterEntry> onWildEncounter, Action<Warp> onWarpTrigger)
    {
        if (bumpCooldown > 0f) bumpCooldown -= dt;

        if (IsMoving)
        {
            float speed = IsRunning ? 8.0f : 4.5f;
            moveProgress += speed * dt;

            // Frame animation
            animTimer += dt * (IsRunning ? 16f : 10f);
            if (animTimer >= 1f)
            {
                animTimer = 0f;
                animFrame = (animFrame + 1) % 4;
            }

            if (moveProgress >= 1f)
            {
                // Finished moving to target tile
                GridX = targetGridX;
                GridY = targetGridY;
                PixelX = GridX * TileSize;
                PixelY = GridY * TileSize;
                HopHeight = 0f;
                IsMoving = false;
                IsHoppingLedge = false;
                moveProgress = 0f;
                animFrame = 0;

                // Check warp
                var warp = map.GetWarpAt(GridX, GridY);
                if (warp != null)
                {
                    onWarpTrigger(warp);
                    return;
                }

                // Check tall grass step & encounter
                if (map.IsTallGrass(GridX, GridY))
                {
                    InTallGrass = true;
                    GrassRustleTimer = 0.2f;
                    AudioManager.PlaySound("grass");

                    // Roll for wild encounter
                    var wild = map.RollWildEncounter();
                    if (wild != null)
                    {
                        onWildEncounter(wild);
                        return;
                    }
                }
                else
                {
                    InTallGrass = false;
                }
            }
            else
            {
                // Smooth interpolation
                float startX = GridX * TileSize;
                float startY = GridY * TileSize;
                float endX = targetGridX * TileSize;
                float endY = targetGridY * TileSize;

                PixelX = startX + (endX - startX) * moveProgress;
                PixelY = startY + (endY - startY) * moveProgress;

                // Ledge hop arc (height above the ground, in the same pixel units)
                HopHeight = IsHoppingLedge ? MathF.Sin(moveProgress * MathF.PI) * 16f : 0f;
            }
        }
        else
        {
            // Idle state: check movement input
            IsRunning = InputManager.IsActionDown(GameAction.Run);
            Vector2 dir = InputManager.GetMovementVector();

            if (dir != Vector2.Zero)
            {
                Direction desiredDir = Facing;
                if (Math.Abs(dir.X) > Math.Abs(dir.Y))
                {
                    desiredDir = dir.X > 0 ? Direction.Right : Direction.Left;
                }
                else
                {
                    desiredDir = dir.Y > 0 ? Direction.Down : Direction.Up;
                }

                if (Facing != desiredDir)
                {
                    Facing = desiredDir;
                }
                else
                {
                    TryStep(desiredDir, map);

                    if (!IsMoving)
                    {
                        // Blocked: walk in place against the obstacle, a little slower than walking
                        animTimer += dt * 6f;
                        if (animTimer >= 1f)
                        {
                            animTimer = 0f;
                            animFrame = (animFrame + 1) % 4;
                        }
                    }
                }
            }
            else
            {
                animFrame = 0;
                bumpCooldown = 0f;
            }
        }

        if (GrassRustleTimer > 0f)
        {
            GrassRustleTimer -= dt;
        }
    }

    private void TryStep(Direction dir, Map map)
    {
        int dx = 0, dy = 0;
        switch (dir)
        {
            case Direction.Up: dy = -1; break;
            case Direction.Down: dy = 1; break;
            case Direction.Left: dx = -1; break;
            case Direction.Right: dx = 1; break;
        }

        int nx = GridX + dx;
        int ny = GridY + dy;

        // Check bounds
        if (!map.InBounds(nx, ny))
        {
            // Check if there is an edge warp trigger here
            var edgeWarp = map.GetWarpAt(GridX, GridY);
            if (edgeWarp != null)
            {
                return;
            }
            Bump();
            return;
        }

        // Check Ledge jump (only jumping Down over LedgeDown)
        if (dir == Direction.Down && map.IsLedge(nx, ny))
        {
            int landY = ny + 1;
            if (map.InBounds(nx, landY) && map.IsWalkable(nx, landY, isLedgeLanding: true))
            {
                targetGridX = nx;
                targetGridY = landY;
                IsMoving = true;
                IsHoppingLedge = true;
                moveProgress = 0f;
                AudioManager.PlaySound("select");
                return;
            }
        }

        // Check standard solid collision
        if (map.IsWalkable(nx, ny))
        {
            targetGridX = nx;
            targetGridY = ny;
            IsMoving = true;
            moveProgress = 0f;
        }
        else
        {
            Bump();
        }
    }

    private void Bump()
    {
        if (bumpCooldown > 0f) return;
        AudioManager.PlaySound("bump");
        bumpCooldown = BumpInterval;
    }
}
