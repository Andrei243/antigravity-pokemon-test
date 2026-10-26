using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>The field's rules drawn as layers over the picture (plan 16 · T18; <see cref="DebugLayers"/>).</summary>
public sealed partial class WorldRenderer
{
    /// <summary>
    /// The layers drawn over the field: none unless the console, a scenario or the harness turns some on. Never a
    /// setting, and with none on nothing at all is drawn or worked out for them.
    /// </summary>
    internal DebugLayer Layers { get; set; }

    /// <summary>What the layers drew the last frame (null while none is on): what a tool reads to say what a shot shows.</summary>
    internal LayerPicture? LastLayers { get; private set; }

    // The chunks the frame stopped for (the waits of ScenesFor), by map, column and row: told as "waited" this frame
    private readonly HashSet<(Map Map, int X, int Y)> waitedThisFrame = new();

    // The labels of the last frame, at their places on a picture 1920 by 1080
    private readonly List<(Vector2 At, string Text, Color Colour)> layerLabels = new();

    /// <summary>What a chunk of a map's scene has come to, as the last frame left it.</summary>
    internal ChunkState ChunkStateOf(Map map, int x, int y)
    {
        if (!chunks.TryGetValue((map, x, y), out var slot)) return ChunkState.None;
        if (slot.Scene == null) return ChunkState.Preparing;
        return waitedThisFrame.Contains((map, x, y)) ? ChunkState.Waited : ChunkState.Ready;
    }

    /// <summary>
    /// The layers on, in the 3D pass after the scene: tinted tiles and lines lying on the ground (lifted to where it is
    /// drawn, each corner straightened outdoors as the blob shadows are), with the default shader, over everything; and
    /// the labels' places on the picture, for <see cref="DrawLayerLabels"/> in the 2D pass.
    /// </summary>
    private void DrawDebugLayers(Map map, Camera3D camera, GroundRect? view)
    {
        layerLabels.Clear();
        if (Layers == DebugLayer.None)
        {
            LastLayers = null;
            return;
        }
        // The window round the camera: what the view takes in outdoors, the whole room inside
        TileWindow window = view is { } v
            ? new TileWindow((int)MathF.Floor(v.MinX), (int)MathF.Floor(v.MinZ), (int)MathF.Ceiling(v.MaxX - MathF.Floor(v.MinX)) + 1, (int)MathF.Ceiling(v.MaxZ - MathF.Floor(v.MinZ)) + 1)
            : new TileWindow((int)camera.Target.X - DebugLayers.MostTiles / 2, (int)camera.Target.Z - DebugLayers.MostTiles / 2, DebugLayers.MostTiles, DebugLayers.MostTiles);
        var picture = DebugLayers.Build(map, window, Layers, (x, y) => ChunkStateOf(map, x, y));
        LastLayers = picture;

        float pitchDeg = MapScene.PitchOf(map);
        bool straighten = !map.IsIndoors;
        Vector3 Corner(float x, float y, float z) => new(straighten && y != 0f ? Straighten(camera, pitchDeg, x, y, z) : x, y, z);

        Rlgl.DrawRenderBatchActive();
        Rlgl.DisableDepthTest();
        Rlgl.DisableDepthMask();
        Rlgl.SetTexture(0);

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color colour)
        {
            Rlgl.CheckRenderBatchLimit(4);
            Rlgl.Begin(DrawMode.Quads);
            Rlgl.Color4ub(colour.R, colour.G, colour.B, colour.A);
            Rlgl.Vertex3f(a.X, a.Y, a.Z);
            Rlgl.Vertex3f(b.X, b.Y, b.Z);
            Rlgl.Vertex3f(c.X, c.Y, c.Z);
            Rlgl.Vertex3f(d.X, d.Y, d.Z);
            Rlgl.End();
        }

        foreach (var q in picture.Quads)
        {
            float y = q.Lift + 0.02f;
            // (a texel in from each side, so neighbouring tiles of one layer read as tiles)
            float x0 = q.X + 1f / 32f, x1 = q.X + 1f - 1f / 32f, z0 = q.Y + 1f / 32f, z1 = q.Y + 1f - 1f / 32f;
            Quad(Corner(x0, y, z1), Corner(x1, y, z1), Corner(x1, y, z0), Corner(x0, y, z0), q.Colour);
        }
        foreach (var line in picture.Lines)
        {
            // A line is a thin band on the ground, two texels wide, at the height of the ground at its middle
            const float half = 1f / 32f;
            int tx = (int)MathF.Floor(Math.Min(line.X0, line.X1)), ty = (int)MathF.Floor(Math.Min(line.Z0, line.Z1));
            float y = (map.InBounds(tx, ty) ? DebugLayers.LiftOf(map, tx, ty) : 0f) + 0.03f;
            bool upright = line.X0 == line.X1;
            if (upright)
                Quad(Corner(line.X0 - half, y, Math.Max(line.Z0, line.Z1)), Corner(line.X0 + half, y, Math.Max(line.Z0, line.Z1)),
                    Corner(line.X0 + half, y, Math.Min(line.Z0, line.Z1)), Corner(line.X0 - half, y, Math.Min(line.Z0, line.Z1)), line.Colour);
            else
                Quad(Corner(Math.Min(line.X0, line.X1), y, line.Z0 + half), Corner(Math.Max(line.X0, line.X1), y, line.Z0 + half),
                    Corner(Math.Max(line.X0, line.X1), y, line.Z0 - half), Corner(Math.Min(line.X0, line.X1), y, line.Z0 - half), line.Colour);
        }
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
        Rlgl.EnableDepthTest();

        foreach (var label in picture.Labels)
        {
            var at = Corner(label.X, label.Lift + 0.02f, label.Z);
            layerLabels.Add((Raylib.GetWorldToScreenEx(at, camera, 1920, 1080), label.Text, label.Colour));
        }
    }

    /// <summary>The layers' labels, in the 2D pass, at caption size over a shadow of their own.</summary>
    private void DrawLayerLabels(int destWidth, int destHeight)
    {
        if (layerLabels.Count == 0) return;
        const float size = 15f;
        var shadow = new Color(0, 0, 0, 200);
        foreach (var (at, text, colour) in layerLabels)
        {
            float x = at.X * destWidth / 1920f, y = at.Y * destHeight / 1080f;
            if (x < -200f || y < -20f || x > destWidth || y > destHeight) continue;
            float w = UiFonts.Measure(text, size);
            float left = MathF.Round(x - w / 2f), top = MathF.Round(y - size / 2f);
            UiFonts.Draw(text, left + 1f, top + 1f, size, shadow);
            UiFonts.Draw(text, left, top, size, colour);
        }
    }
}
