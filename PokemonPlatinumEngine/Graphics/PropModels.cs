using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// 3D models for interior furniture and wall decorations. Heights are given in screen rows and multiplied by
/// the scene's vertical stretch so furniture keeps its size relative to the (stretched) character sprites.
/// </summary>
internal static class PropModels
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color Wood = Rgb(184, 126, 78);
    private static readonly Color DarkWood = Rgb(128, 86, 56);

    public static void Build(Prop p, Map map, float vs, Func<Texture2D, MeshBuilder> batch)
    {
        var white = batch(SceneTextures.White);
        float H(float rows) => rows * vs;
        float x0 = p.X, x1 = p.X + p.Width, z0 = p.Y, z1 = p.Y + p.Depth;

        switch (p.Type)
        {
            case PropType.Table:
            {
                float top = H(0.72f);
                white.Box(new(x0 + 0.08f, top - H(0.07f), z0 + 0.14f), new(x1 - 0.08f, top, z1 - 0.1f), Wood, BoxFaces.All);
                foreach (var (lx, lz) in new[] { (x0 + 0.16f, z0 + 0.22f), (x1 - 0.26f, z0 + 0.22f), (x0 + 0.16f, z1 - 0.2f), (x1 - 0.26f, z1 - 0.2f) })
                    white.Box(new(lx, 0, lz), new(lx + 0.1f, top - H(0.07f), lz + 0.1f), DarkWood, BoxFaces.Sides);
                // Lace runner and a small vase of flowers
                white.Box(new(x0 + 0.3f, top, z0 + 0.36f), new(x1 - 0.3f, top + 0.01f, z1 - 0.32f), Rgb(250, 246, 236), BoxFaces.Top);
                float cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f;
                white.Cylinder(new(cx, top, cz), 0.07f, 0.09f, H(0.16f), 8, Rgb(90, 140, 210));
                LeafBall(batch, new(cx, top + H(0.2f), cz), 0.12f, 0.12f * vs, Rgb(236, 90, 110), 0f);
                break;
            }

            case PropType.Chair:
            {
                float seat = H(0.42f);
                white.Box(new(x0 + 0.2f, seat - H(0.06f), z0 + 0.24f), new(x1 - 0.2f, seat, z1 - 0.18f), Wood, BoxFaces.All);
                foreach (var (lx, lz) in new[] { (x0 + 0.24f, z0 + 0.28f), (x1 - 0.32f, z0 + 0.28f), (x0 + 0.24f, z1 - 0.26f), (x1 - 0.32f, z1 - 0.26f) })
                    white.Box(new(lx, 0, lz), new(lx + 0.08f, seat, lz + 0.08f), DarkWood, BoxFaces.Sides);
                // Backrest on the side facing away from the table next to it
                var (bx0, bz0, bx1, bz1) = ChairBack(p, map, x0, z0, x1, z1);
                white.Box(new(bx0, seat, bz0), new(bx1, seat + H(0.5f), bz1), Wood, BoxFaces.All);
                break;
            }

            case PropType.Sofa:
            {
                var fabric = Rgb(214, 96, 84);
                bool backWest = p.X <= 1, backEast = p.X + p.Width >= map.Width - 1;
                white.Box(new(x0 + 0.06f, 0, z0 + 0.08f), new(x1 - 0.06f, H(0.28f), z1 - 0.08f), MeshBuilder.Scale(fabric, 0.8f), BoxFaces.Visible);
                white.Box(new(x0 + 0.1f, H(0.28f), z0 + 0.12f), new(x1 - 0.1f, H(0.4f), z1 - 0.12f), fabric, BoxFaces.Visible);
                if (backWest) white.Box(new(x0 + 0.04f, 0, z0 + 0.06f), new(x0 + 0.3f, H(0.85f), z1 - 0.06f), MeshBuilder.Scale(fabric, 0.9f), BoxFaces.Visible);
                else if (backEast) white.Box(new(x1 - 0.3f, 0, z0 + 0.06f), new(x1 - 0.04f, H(0.85f), z1 - 0.06f), MeshBuilder.Scale(fabric, 0.9f), BoxFaces.Visible);
                else white.Box(new(x0 + 0.06f, 0, z0 + 0.04f), new(x1 - 0.06f, H(0.85f), z0 + 0.3f), MeshBuilder.Scale(fabric, 0.9f), BoxFaces.Visible);
                // Armrests at both ends
                white.Box(new(x0 + 0.04f, 0, z0 + 0.04f), new(x1 - 0.04f, H(0.55f), z0 + 0.2f), MeshBuilder.Scale(fabric, 0.85f), BoxFaces.Visible);
                white.Box(new(x0 + 0.04f, 0, z1 - 0.2f), new(x1 - 0.04f, H(0.55f), z1 - 0.04f), MeshBuilder.Scale(fabric, 0.85f), BoxFaces.Visible);
                break;
            }

            case PropType.Bookshelf:
            {
                float top = H(1.55f), zb = z0, zf = z0 + 0.52f;
                white.Box(new(x0 + 0.03f, 0, zb), new(x1 - 0.03f, top, zf), DarkWood, BoxFaces.Top | BoxFaces.West | BoxFaces.East);
                batch(SceneTextures.Books).Decal(new(x0 + 0.03f, 0, zf), new(x1 - 0.03f, 0, zf), new(x1 - 0.03f, top, zf), new(x0 + 0.03f, top, zf), Color.White, Vector3.UnitZ);
                break;
            }

            case PropType.Television:
            {
                white.Box(new(x0 + 0.06f, 0, z0), new(x1 - 0.06f, H(0.42f), z0 + 0.5f), Wood, BoxFaces.Visible);
                white.Box(new(x0 + 0.14f, H(0.42f), z0 + 0.08f), new(x1 - 0.14f, H(0.98f), z0 + 0.36f), Rgb(58, 58, 70), BoxFaces.Visible);
                batch(SceneTextures.TvScreen).Decal(new(x0 + 0.19f, H(0.47f), z0 + 0.365f), new(x1 - 0.19f, H(0.47f), z0 + 0.365f),
                    new(x1 - 0.19f, H(0.93f), z0 + 0.365f), new(x0 + 0.19f, H(0.93f), z0 + 0.365f), Color.White, Vector3.UnitZ);
                break;
            }

            case PropType.Plant:
            {
                float cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f;
                white.Cylinder(new(cx, 0, cz), 0.24f, 0.3f, H(0.34f), 10, Rgb(196, 110, 72), cap: true, capColor: Rgb(92, 64, 44));
                LeafBall(batch, new(cx, H(0.62f), cz), 0.36f, 0.3f * vs, Rgb(96, 176, 96), 0.3f);
                LeafBall(batch, new(cx - 0.12f, H(0.9f), cz + 0.05f), 0.24f, 0.22f * vs, Rgb(110, 190, 104), 0.5f);
                break;
            }

            case PropType.Fridge:
            {
                float top = H(1.75f), zf = z0 + 0.62f;
                var body = Rgb(236, 238, 244);
                white.Box(new(x0 + 0.06f, 0, z0), new(x1 - 0.06f, top, zf), body, BoxFaces.Visible);
                white.Box(new(x0 + 0.06f, H(1.12f), zf), new(x1 - 0.06f, H(1.15f), zf + 0.01f), Rgb(150, 154, 166), BoxFaces.South);
                white.Box(new(x1 - 0.2f, H(1.25f), zf), new(x1 - 0.15f, H(1.6f), zf + 0.05f), Rgb(170, 176, 190), BoxFaces.Visible);
                white.Box(new(x1 - 0.2f, H(0.55f), zf), new(x1 - 0.15f, H(1.0f), zf + 0.05f), Rgb(170, 176, 190), BoxFaces.Visible);
                break;
            }

            case PropType.KitchenCounter:
            case PropType.Stove:
            {
                float top = H(0.9f), zf = z0 + 0.62f;
                white.Box(new(x0, 0, z0), new(x1, top - 0.06f, zf), Rgb(236, 230, 214), BoxFaces.Top | BoxFaces.West | BoxFaces.East);
                batch(SceneTextures.Cabinet).Decal(new(x0, 0, zf), new(x1, 0, zf), new(x1, top - 0.06f, zf), new(x0, top - 0.06f, zf), Color.White, Vector3.UnitZ);
                white.Box(new(x0 - 0.01f, top - 0.06f, z0), new(x1 + 0.01f, top, zf + 0.04f), Rgb(196, 200, 210), BoxFaces.Visible);
                float cx = (x0 + x1) / 2f, cz = z0 + 0.32f;
                if (p.Type == PropType.Stove)
                {
                    white.Cylinder(new(cx - 0.2f, top, cz), 0.13f, 0.13f, 0.015f, 12, Rgb(54, 54, 62));
                    white.Cylinder(new(cx + 0.2f, top, cz), 0.13f, 0.13f, 0.015f, 12, Rgb(54, 54, 62));
                    white.Box(new(x0 + 0.14f, H(0.12f), zf), new(x1 - 0.14f, H(0.6f), zf + 0.02f), Rgb(70, 70, 82), BoxFaces.South);
                }
                else
                {
                    white.Box(new(cx - 0.28f, top - 0.005f, cz - 0.16f), new(cx + 0.28f, top + 0.005f, cz + 0.16f), Rgb(120, 130, 150), BoxFaces.Top);
                    white.Cylinder(new(cx, top, z0 + 0.1f), 0.03f, 0.03f, H(0.22f), 6, Rgb(190, 196, 210));
                }
                break;
            }

            case PropType.Stairs:
            {
                // Steps climb toward the back wall
                const int steps = 7;
                float rise = H(2.2f) / steps, run = (z1 - z0) / steps;
                for (int i = 0; i < steps; i++)
                {
                    float zFront = z1 - i * run;
                    white.Box(new(x0 + 0.06f, 0, zFront - run), new(x1 - 0.06f, rise * (i + 1), zFront), i % 2 == 0 ? Wood : MeshBuilder.Scale(Wood, 0.93f), BoxFaces.Top | BoxFaces.South | BoxFaces.West);
                }
                white.Box(new(x1 - 0.1f, 0, z0), new(x1, H(2.4f), z1), DarkWood, BoxFaces.Visible);
                white.Box(new(x0 + 0.02f, H(0.9f), z0), new(x0 + 0.08f, H(1.0f), z1), DarkWood, BoxFaces.Visible);
                break;
            }

            case PropType.Counter:
            {
                bool center = map.Interior == InteriorStyle.PokemonCenter;
                var panel = center ? Rgb(226, 92, 100) : Rgb(80, 128, 216);
                float top = H(0.95f);
                white.Box(new(x0 + 0.02f, 0, z0 + 0.12f), new(x1 - 0.02f, top - 0.08f, z1 - 0.12f), panel, BoxFaces.Visible);
                white.Box(new(x0 + 0.02f, H(0.55f), z1 - 0.12f), new(x1 - 0.02f, H(0.62f), z1 - 0.1f), Rgb(250, 250, 252), BoxFaces.South | BoxFaces.Top);
                white.Box(new(x0 - 0.04f, top - 0.08f, z0 + 0.06f), new(x1 + 0.04f, top, z1 - 0.04f), Rgb(248, 248, 252), BoxFaces.Visible);
                break;
            }

            case PropType.HealingMachine:
            {
                float top = H(0.85f);
                white.Box(new(x0 + 0.06f, 0, z0), new(x1 - 0.06f, top, z0 + 0.7f), Rgb(222, 226, 236), BoxFaces.Visible);
                batch(SceneTextures.PcScreen).Decal(new(x0 + 0.2f, H(0.4f), z0 + 0.705f), new(x1 - 0.2f, H(0.4f), z0 + 0.705f),
                    new(x1 - 0.2f, H(0.72f), z0 + 0.705f), new(x0 + 0.2f, H(0.72f), z0 + 0.705f), Color.White, Vector3.UnitZ);
                for (int i = 0; i < 6; i++)
                {
                    float bx = x0 + 0.24f + (i % 3) * 0.26f, bz = z0 + 0.2f + (i / 3) * 0.28f;
                    white.Cylinder(new(bx, top, bz), 0.1f, 0.06f, 0.1f, 10, i % 2 == 0 ? Rgb(236, 64, 60) : Rgb(248, 248, 252));
                }
                break;
            }

            case PropType.Bench:
            {
                var cushion = Rgb(116, 150, 216);
                white.Box(new(x0 + 0.06f, H(0.3f), z0 + 0.2f), new(x1 - 0.06f, H(0.42f), z1 - 0.16f), cushion, BoxFaces.Visible);
                foreach (float lx in new[] { x0 + 0.12f, x1 - 0.22f })
                    white.Box(new(lx, 0, z0 + 0.28f), new(lx + 0.1f, H(0.3f), z1 - 0.24f), Rgb(160, 164, 176), BoxFaces.Sides);
                white.Box(new(x0 + 0.06f, H(0.42f), z0 + 0.12f), new(x1 - 0.06f, H(0.8f), z0 + 0.24f), MeshBuilder.Scale(cushion, 0.9f), BoxFaces.Visible);
                break;
            }

            case PropType.StoreShelf:
            {
                float top = H(1.3f);
                bool againstWall = p.Y <= 2;
                float zb = z0 + (againstWall ? 0f : 0.1f), zf = z1 - 0.12f;
                white.Box(new(x0 + 0.02f, 0, zb), new(x1 - 0.02f, top, zf), Rgb(176, 184, 200), BoxFaces.Top | BoxFaces.West | BoxFaces.East);
                var goods = batch(SceneTextures.Goods);
                goods.Quad(new(x0 + 0.02f, 0, zf), new(x1 - 0.02f, 0, zf), new(x1 - 0.02f, top, zf), new(x0 + 0.02f, top, zf),
                    new(0, 1), new(p.Width, 1), new(p.Width, 0), new(0, 0), Color.White, Vector3.UnitZ);
                if (!againstWall)
                    goods.Quad(new(x1 - 0.02f, 0, zb), new(x0 + 0.02f, 0, zb), new(x0 + 0.02f, top, zb), new(x1 - 0.02f, top, zb),
                        new(0, 1), new(p.Width, 1), new(p.Width, 0), new(0, 0), Color.White, -Vector3.UnitZ);
                break;
            }

            case PropType.LabDesk:
            {
                float top = H(0.78f);
                white.Box(new(x0 + 0.02f, 0, z0 + 0.14f), new(x1 - 0.02f, top - 0.06f, z1 - 0.14f), Rgb(150, 160, 186), BoxFaces.Visible);
                white.Box(new(x0 - 0.02f, top - 0.06f, z0 + 0.08f), new(x1 + 0.02f, top, z1 - 0.08f), Rgb(240, 242, 248), BoxFaces.Visible);
                // A computer at one end and some papers
                float mx = x1 - 0.9f;
                white.Box(new(mx, top, z0 + 0.2f), new(mx + 0.6f, top + H(0.42f), z0 + 0.4f), Rgb(214, 218, 230), BoxFaces.Visible);
                batch(SceneTextures.PcScreen).Decal(new(mx + 0.05f, top + H(0.05f), z0 + 0.405f), new(mx + 0.55f, top + H(0.05f), z0 + 0.405f),
                    new(mx + 0.55f, top + H(0.37f), z0 + 0.405f), new(mx + 0.05f, top + H(0.37f), z0 + 0.405f), Color.White, Vector3.UnitZ);
                white.Box(new(x0 + 0.4f, top, z0 + 0.35f), new(x0 + 0.85f, top + 0.012f, z0 + 0.7f), Rgb(250, 250, 250), BoxFaces.Top);
                white.Box(new(x0 + 1.2f, top, z0 + 0.3f), new(x0 + 1.55f, top + 0.02f, z0 + 0.6f), Rgb(244, 238, 220), BoxFaces.Top);
                break;
            }

            case PropType.LabMachine:
            {
                float top = H(1.7f);
                bool facesEast = p.X <= 1, facesWest = p.X + p.Width >= map.Width - 1;
                var casing = Rgb(186, 192, 210);
                white.Box(new(x0 + 0.06f, 0, z0 + 0.06f), new(x1 - 0.06f, top, z1 - 0.06f), casing, BoxFaces.Visible);
                var console = batch(SceneTextures.Console);
                if (facesEast)
                    console.Quad(new(x1 - 0.055f, 0, z1 - 0.1f), new(x1 - 0.055f, 0, z0 + 0.1f), new(x1 - 0.055f, top - 0.1f, z0 + 0.1f), new(x1 - 0.055f, top - 0.1f, z1 - 0.1f),
                        new(0, 1), new(p.Depth, 1), new(p.Depth, 0), new(0, 0), Color.White, Vector3.UnitX);
                else if (facesWest)
                    console.Quad(new(x0 + 0.055f, 0, z0 + 0.1f), new(x0 + 0.055f, 0, z1 - 0.1f), new(x0 + 0.055f, top - 0.1f, z1 - 0.1f), new(x0 + 0.055f, top - 0.1f, z0 + 0.1f),
                        new(0, 1), new(p.Depth, 1), new(p.Depth, 0), new(0, 0), Color.White, -Vector3.UnitX);
                else
                    console.Quad(new(x0 + 0.1f, 0, z1 - 0.055f), new(x1 - 0.1f, 0, z1 - 0.055f), new(x1 - 0.1f, top - 0.1f, z1 - 0.055f), new(x0 + 0.1f, top - 0.1f, z1 - 0.055f),
                        new(0, 1), new(p.Width, 1), new(p.Width, 0), new(0, 0), Color.White, Vector3.UnitZ);
                break;
            }

            case PropType.Rug:
            {
                var rug = batch(SceneTextures.Rug(map.Interior == InteriorStyle.PokemonCenter));
                rug.Decal(new(x0 + 0.08f, 0.006f, z1 - 0.08f), new(x1 - 0.08f, 0.006f, z1 - 0.08f), new(x1 - 0.08f, 0.006f, z0 + 0.08f), new(x0 + 0.08f, 0.006f, z0 + 0.08f), Color.White, Vector3.UnitY);
                break;
            }

            // Wall decorations hang on the back wall (its face is at z = 2)
            case PropType.Window:
            {
                float y0 = H(1.05f), y1 = H(2.05f), z = 2f;
                white.Box(new(x0 + 0.1f, y0 - H(0.06f), z), new(x1 - 0.1f, y0, z + 0.14f), Rgb(250, 250, 252), BoxFaces.Visible);
                white.Box(new(x0 + 0.14f, y1, z), new(x1 - 0.14f, y1 + H(0.05f), z + 0.06f), Rgb(250, 250, 252), BoxFaces.Visible);
                white.Box(new(x0 + 0.14f, y0, z), new(x0 + 0.2f, y1, z + 0.06f), Rgb(250, 250, 252), BoxFaces.Visible);
                white.Box(new(x1 - 0.2f, y0, z), new(x1 - 0.14f, y1, z + 0.06f), Rgb(250, 250, 252), BoxFaces.Visible);
                batch(SceneTextures.Window).Decal(new(x0 + 0.2f, y0, z + 0.01f), new(x1 - 0.2f, y0, z + 0.01f), new(x1 - 0.2f, y1, z + 0.01f), new(x0 + 0.2f, y1, z + 0.01f), Color.White, Vector3.UnitZ);
                break;
            }

            case PropType.Painting:
            {
                float y0 = H(1.25f), y1 = H(1.85f), z = 2f;
                white.Box(new(x0 + 0.12f, y0 - 0.05f, z), new(x1 - 0.12f, y1 + 0.05f, z + 0.04f), Rgb(170, 128, 70), BoxFaces.Visible);
                batch(SceneTextures.Painting).Decal(new(x0 + 0.16f, y0, z + 0.045f), new(x1 - 0.16f, y0, z + 0.045f), new(x1 - 0.16f, y1, z + 0.045f), new(x0 + 0.16f, y1, z + 0.045f), Color.White, Vector3.UnitZ);
                break;
            }

            case PropType.Clock:
            {
                float cx = (x0 + x1) / 2f, cy = H(1.85f), r = 0.3f;
                batch(SceneTextures.ClockFace).Decal(new(cx - r, cy - r * vs, 2.01f), new(cx + r, cy - r * vs, 2.01f), new(cx + r, cy + r * vs, 2.01f), new(cx - r, cy + r * vs, 2.01f), Color.White, Vector3.UnitZ);
                break;
            }

            case PropType.WallEmblem:
            {
                float cx = (x0 + x1) / 2f, cy = H(1.5f), r = 0.55f;
                batch(SceneTextures.CenterEmblem).Decal(new(cx - r, cy - r * vs, 2.01f), new(cx + r, cy - r * vs, 2.01f), new(cx + r, cy + r * vs, 2.01f), new(cx - r, cy + r * vs, 2.01f), Color.White, Vector3.UnitZ);
                break;
            }
        }
    }

    /// <summary>A PC on a small desk, for PC tiles.</summary>
    public static void BuildPc(int tx, int ty, float vs, Func<Texture2D, MeshBuilder> batch)
    {
        var white = batch(SceneTextures.White);
        float deskTop = 0.45f * vs, monitorTop = deskTop + 0.5f * vs;
        white.Box(new(tx + 0.06f, 0, ty), new(tx + 0.94f, deskTop, ty + 0.8f), Wood, BoxFaces.Visible);
        white.Box(new(tx + 0.18f, deskTop, ty + 0.12f), new(tx + 0.82f, monitorTop, ty + 0.5f), Rgb(214, 218, 230), BoxFaces.Visible);
        batch(SceneTextures.PcScreen).Decal(new(tx + 0.23f, deskTop + 0.05f * vs, ty + 0.505f), new(tx + 0.77f, deskTop + 0.05f * vs, ty + 0.505f),
            new(tx + 0.77f, monitorTop - 0.05f * vs, ty + 0.505f), new(tx + 0.23f, monitorTop - 0.05f * vs, ty + 0.505f), Color.White, Vector3.UnitZ);
        white.Box(new(tx + 0.25f, deskTop, ty + 0.56f), new(tx + 0.75f, deskTop + 0.03f, ty + 0.74f), Rgb(190, 194, 206), BoxFaces.Visible);
    }

    /// <summary>A round clump of leaves: a solid core plus a cut-out shell for a leafy outline.</summary>
    public static void LeafBall(Func<Texture2D, MeshBuilder> batch, Vector3 center, float radius, float height, Color tint, float sway)
    {
        Icosphere.Add(batch(SceneTextures.Leaves), center, new Vector3(radius, height, radius) * 0.9f, MeshBuilder.Scale(tint, 0.8f), 0, 1.4f, sway);
        Icosphere.Add(batch(SceneTextures.LeafShell), center, new Vector3(radius, height, radius) * 1.08f, tint, 1, 1.4f, sway);
    }

    private static (float X0, float Z0, float X1, float Z1) ChairBack(Prop p, Map map, float x0, float z0, float x1, float z1)
    {
        bool TableAt(int x, int y) => map.Props.Exists(o => o.Type == PropType.Table && o.Covers(x, y));
        if (TableAt(p.X + 1, p.Y)) return (x0 + 0.2f, z0 + 0.24f, x0 + 0.28f, z1 - 0.18f);
        if (TableAt(p.X - 1, p.Y)) return (x1 - 0.28f, z0 + 0.24f, x1 - 0.2f, z1 - 0.18f);
        if (TableAt(p.X, p.Y - 1)) return (x0 + 0.2f, z1 - 0.26f, x1 - 0.2f, z1 - 0.18f);
        return (x0 + 0.2f, z0 + 0.24f, x1 - 0.2f, z0 + 0.32f);
    }
}

/// <summary>Unit icosphere triangles, used for foliage.</summary>
internal static class Icosphere
{
    private static readonly Vector3[][] Levels = Build();

    private static Vector3[][] Build()
    {
        float t = (1f + MathF.Sqrt(5f)) / 2f;
        var v = new[]
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        };
        int[] f =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };
        var level0 = new Vector3[f.Length];
        for (int i = 0; i < f.Length; i++) level0[i] = Vector3.Normalize(v[f[i]]);

        var level1 = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < level0.Length; i += 3)
        {
            var a = level0[i]; var b = level0[i + 1]; var c = level0[i + 2];
            var ab = Vector3.Normalize(a + b); var bc = Vector3.Normalize(b + c); var ca = Vector3.Normalize(c + a);
            level1.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
        }
        return new[] { level0, level1.ToArray() };
    }

    /// <summary>
    /// Adds an ellipsoid with smooth normals. Texture coordinates are projected along the dominant axis of each
    /// face (tileable foliage textures hide the seams); <paramref name="sway"/> lets the top move in the wind.
    /// </summary>
    public static void Add(MeshBuilder b, Vector3 center, Vector3 radii, Color color, int level, float uvScale, float sway, float jitter = 0.08f)
    {
        var tris = Levels[level];
        for (int i = 0; i < tris.Length; i += 3)
        {
            var n0 = tris[i]; var n1 = tris[i + 1]; var n2 = tris[i + 2];
            Vector3 P(Vector3 n) => center + n * radii * (1f + (Noise(n) - 0.5f) * jitter * 2f);
            var p0 = P(n0); var p1 = P(n1); var p2 = P(n2);

            var faceN = n0 + n1 + n2;
            var an = Vector3.Abs(faceN);
            Vector2 UV(Vector3 p) => an.X >= an.Y && an.X >= an.Z ? new(p.Z * uvScale, -p.Y * uvScale)
                : an.Y >= an.Z ? new(p.X * uvScale, p.Z * uvScale)
                : new(p.X * uvScale, -p.Y * uvScale);

            // Darker underneath (less skylight), and sway weighted toward the top of the clump
            Color C(Vector3 n) => MeshBuilder.Sway(MeshBuilder.Scale(color, 0.78f + 0.22f * (n.Y * 0.5f + 0.5f)), sway * (n.Y * 0.5f + 0.5f));
            b.Tri(p0, p1, p2, UV(p0), UV(p1), UV(p2), n0, n1, n2, C(n0), C(n1), C(n2));
        }
    }

    private static float Noise(Vector3 n)
    {
        int x = (int)MathF.Round(n.X * 50f), y = (int)MathF.Round(n.Y * 50f), z = (int)MathF.Round(n.Z * 50f);
        return GroundBaker.Rand01(x * 31 + z, y, 77);
    }
}
