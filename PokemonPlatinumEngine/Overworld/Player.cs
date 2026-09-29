using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.Overworld;

public class Player
{
    public const int TileSize = 32; // Full HD 32x32 grid tile size

    public int GridX { get; set; } = 8;
    public int GridY { get; set; } = 8;
    public Direction Facing { get; set; } = Direction.Down;

    public float PixelX { get; private set; }
    public float PixelY { get; private set; }

    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsHoppingLedge { get; private set; }
    private float moveProgress = 0f;
    private int targetGridX = 0;
    private int targetGridY = 0;
    private float animTimer = 0f;
    private int animFrame = 0;

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
        IsMoving = false;
        IsHoppingLedge = false;
        moveProgress = 0f;
    }

    public void Update(float dt, Map map, Action<WildEncounterEntry> onWildEncounter, Action<Warp> onWarpTrigger)
    {
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

                // Ledge hop arc
                if (IsHoppingLedge)
                {
                    float arc = MathF.Sin(moveProgress * MathF.PI) * 16f;
                    PixelY -= arc;
                }
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
                }
            }
            else
            {
                animFrame = 0;
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
            AudioManager.PlaySound("bump");
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
            AudioManager.PlaySound("bump");
        }
    }

    public void Draw(int cameraX = 0, int cameraY = 0)
    {
        var sheet = PixelArtGenerator.GetPlayerSpriteSheet();
        int frameSize = 48;
        int row = (int)Facing;
        int col = animFrame;

        Rectangle src = new(col * frameSize, row * frameSize, frameSize, frameSize);
        // Center 48x48 sprite on 32x32 tile (offset -8px X, -14px Y relative to camera)
        Vector2 pos = new(PixelX - cameraX - 8, PixelY - cameraY - 14);

        Raylib.DrawTextureRec(sheet, src, pos, Color.White);

        // Grass blades overlay when walking in tall grass
        if (InTallGrass && !IsHoppingLedge)
        {
            Raylib.DrawRectangle((int)(PixelX - cameraX), (int)(PixelY - cameraY) + 20, TileSize, 12, new Color(52, 160, 68, 190));
        }
    }
}
