using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.OpenWorld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>A line of an open map's layer, from A to B in metres, drawn over the scene.</summary>
internal readonly record struct OpenLayerLine(Vector3 A, Vector3 B, Color Colour, DebugLayer Layer);

/// <summary>Words at a point of an open map, in metres, drawn in the 2D pass.</summary>
internal readonly record struct OpenLayerLabel(Vector3 At, string Text, Color Colour, DebugLayer Layer);

/// <summary>What the layers on draw over an open map round a place.</summary>
internal sealed class OpenLayerPicture
{
    public List<OpenLayerLine> Lines { get; } = new();
    public List<OpenLayerLabel> Labels { get; } = new();
    public bool IsEmpty => Lines.Count == 0 && Labels.Count == 0;
    public int LinesOf(DebugLayer layer) => Lines.Count(l => l.Layer == layer);
}

/// <summary>
/// An open map's rules drawn as layers (plan 32 · OW10), GPU-free: the grid's layers (plan 16 · T18) read for the open
/// world, as lines in metres and labels, built round a place and never for the whole map. The names are the grid's
/// (<see cref="DebugLayers.Names"/>, the console's and the scenarios' <c>layers</c>), each read for what the open world
/// has in its place:
/// <list type="bullet">
/// <item><c>solid</c>: the collision surface the capsule walks on (level 0's own ground, its triangles' edges near the
/// place, green where walked, orange where steep, red where turned down) and the map's solid boxes.</item>
/// <item><c>nav</c>: the navigation mesh's polygons (swum ones cyan), its steps (yellow) and drops (red).</item>
/// <item><c>triggers</c> and <c>warps</c>: the triggers' and the doors' volumes, named with what they wait for and lead to.</item>
/// <item><c>sight</c>: each trainer's cone on the ground, from their feet out to its length.</item>
/// <item><c>areas</c>: the areas' outlines with their keys; <c>encounters</c>: the zones' prisms, their Pokémon over their
/// targets, and each wild Pokémon standing with its species, level and mood.</item>
/// <item><c>chunks</c>: the chunks' borders, each named with the level of detail the renderer has drawn it at.</item>
/// <item><c>markers</c>: each marker, a cross with a tick the way it faces and its name.</item>
/// </list>
/// <c>behaviour</c> and <c>height</c> are the grid's alone. With no layer on nothing is built.
/// </summary>
internal static class OpenDebugLayers
{
    /// <summary>How far round the place the layers reach, in metres.</summary>
    public const double Reach = 48;

    /// <summary>How far round the place the collision surface's triangles are drawn, in metres (half a metre's cells are many).</summary>
    public const double SurfaceReach = 10;

    public static readonly Color WalkedColour = new(70, 220, 90, 200);
    public static readonly Color SteepColour = new(250, 150, 30, 220);
    public static readonly Color DownColour = new(230, 40, 40, 220);
    public static readonly Color SolidColour = new(230, 40, 40, 230);
    public static readonly Color NavColour = new(60, 90, 230, 230);
    public static readonly Color SwimColour = new(40, 210, 230, 230);
    public static readonly Color StepColour = new(250, 226, 40, 240);
    public static readonly Color DropColour = new(240, 40, 40, 240);

    /// <summary>The mount's links (plan 34 · PD2): a hop orange, a high jump brown, a climb magenta, a glide sky blue, as the baker's --nav draws them.</summary>
    public static readonly Color HopColour = new(255, 140, 30, 240), HighJumpColour = new(150, 80, 30, 240),
        ClimbColour = new(230, 60, 220, 240), GlideColour = new(120, 190, 255, 240);
    public static readonly Color TriggerColour = DebugLayers.TriggerColour with { A = 240 };
    public static readonly Color DoorColour = DebugLayers.WarpColour with { A = 240 };
    public static readonly Color SightColour = DebugLayers.SightColour with { A = 240 };
    public static readonly Color AreaColour = DebugLayers.AreaColour;
    public static readonly Color ZoneColour = new(230, 255, 90, 240);
    public static readonly Color WildColour = new(120, 250, 120, 255);
    public static readonly Color MarkerColour = new(255, 255, 255, 240);

    /// <summary>The colour of a chunk's border by the level it is drawn at (nought finest; null for not drawn).</summary>
    public static Color ChunkColour(int? level) => level switch
    {
        0 => new Color(60, 220, 90, 230),
        1 => new Color(250, 210, 40, 230),
        2 => new Color(250, 130, 40, 230),
        _ => new Color(150, 150, 150, 230)
    };

    /// <summary>
    /// What the layers on show round <paramref name="centre"/> (metres): never anything farther than <see cref="Reach"/>
    /// on the ground. <paramref name="field"/> gives the field's people and wild Pokémon (none without it), and
    /// <paramref name="chunkLevel"/> the level each chunk is drawn at (by its column and row; none told without it).
    /// </summary>
    public static OpenLayerPicture Build(OpenMap map, OpenField? field, Vector3 centre, DebugLayer layers, Func<int, int, int?>? chunkLevel = null)
    {
        var picture = new OpenLayerPicture();
        if (layers == DebugLayer.None) return picture;
        if ((layers & DebugLayer.Solid) != 0) Surface(map, centre, picture);
        if ((layers & DebugLayer.Nav) != 0) Nav(map, centre, picture);
        if ((layers & DebugLayer.Triggers) != 0) Triggers(map, centre, picture);
        if ((layers & DebugLayer.Warps) != 0) Doors(map, centre, picture);
        if ((layers & DebugLayer.Sight) != 0) Sight(map, centre, picture);
        if ((layers & DebugLayer.Areas) != 0) Areas(map, centre, picture);
        if ((layers & DebugLayer.Encounters) != 0) Encounters(map, field, centre, picture);
        if ((layers & DebugLayer.Chunks) != 0) Chunks(map, centre, picture, chunkLevel);
        if ((layers & DebugLayer.Markers) != 0) Markers(map, centre, picture);
        return picture;
    }

    private static Vector3 V(OpenPos p) => new(p.X / 1000f, p.Y / 1000f, p.Z / 1000f);
    private static Vector3 V(OpenVec v) => new((float)v.X, (float)v.Y, (float)v.Z);

    private static bool Near(Vector3 centre, float x, float z, double reach) =>
        (x - centre.X) * (x - centre.X) + (z - centre.Z) * (z - centre.Z) <= reach * reach;

    private static void Add(OpenLayerPicture p, Vector3 a, Vector3 b, Color c, DebugLayer layer) => p.Lines.Add(new OpenLayerLine(a, b, c, layer));

    /// <summary>The ground's height at a column between two heights (the highest face up there), else the fallback.</summary>
    private static float GroundY(OpenMap map, float x, float z, float top, float bottom, float fallback) =>
        map.Ground.Footing(x, z, top, bottom) is { } f ? (float)f.Y : fallback;

    // ------------------------------------------------------------------ the layers

    private static void Surface(OpenMap map, Vector3 centre, OpenLayerPicture picture)
    {
        var ground = map.Ground;
        var near = new List<int>();
        ground.Near(centre.X, centre.Z, SurfaceReach, near);
        foreach (int i in near)
        {
            var t = ground[i];
            var (a, b, c) = (V(t.A), V(t.B), V(t.C));
            var mid = (a + b + c) / 3f;
            if (!Near(centre, mid.X, mid.Z, SurfaceReach)) continue;
            var colour = t.Walkable ? WalkedColour : t.FacesUp ? SteepColour : DownColour;
            // a hair over the face, so the line is not lost in it
            var lift = new Vector3(0, 0.02f, 0);
            Add(picture, a + lift, b + lift, colour, DebugLayer.Solid);
            Add(picture, b + lift, c + lift, colour, DebugLayer.Solid);
            Add(picture, c + lift, a + lift, colour, DebugLayer.Solid);
        }
        foreach (var box in map.Solids)
        {
            var (min, max) = (V(box.Min), V(box.Max));
            if (!Near(centre, (min.X + max.X) / 2, (min.Z + max.Z) / 2, Reach + Math.Max(max.X - min.X, max.Z - min.Z))) continue;
            Box(picture, min, max, SolidColour, DebugLayer.Solid);
            picture.Labels.Add(new OpenLayerLabel(new Vector3((min.X + max.X) / 2, max.Y + 0.3f, (min.Z + max.Z) / 2), box.Name, SolidColour, DebugLayer.Solid));
        }
    }

    private static void Nav(OpenMap map, Vector3 centre, OpenLayerPicture picture)
    {
        if (map.Land?.Nav is not { } nav) return;
        var lift = new Vector3(0, 0.05f, 0);
        foreach (var polygon in nav.Polygons)
        {
            var hub = V(polygon.Hub);
            if (!Near(centre, hub.X, hub.Z, Reach)) continue;
            var colour = polygon.Swim ? SwimColour : NavColour;
            for (int k = 0; k < polygon.Corners.Length; k++)
            {
                var a = V(nav.Corner(polygon, k)) + lift;
                var b = V(nav.Corner(polygon, (k + 1) % polygon.Corners.Length)) + lift;
                Add(picture, a, b, colour, DebugLayer.Nav);
            }
        }
        foreach (var link in nav.Links)
        {
            if (!link.OffMesh) continue;
            var from = V(link.A);
            if (!Near(centre, from.X, from.Z, Reach)) continue;
            var f = link.Flags;
            var colour = (f & OpenNavFlags.Glide) != 0 ? GlideColour : (f & OpenNavFlags.Climb) != 0 ? ClimbColour
                : (f & OpenNavFlags.HighJump) != 0 ? HighJumpColour : (f & OpenNavFlags.Jump) != 0 ? HopColour
                : (f & OpenNavFlags.Drop) != 0 ? DropColour : StepColour;
            Add(picture, from + lift, V(link.Land) + lift, colour, DebugLayer.Nav);
        }
    }

    private static void Shape(OpenLayerPicture picture, OpenShape shape, Color colour, DebugLayer layer)
    {
        var (min, max) = (V(shape.Min), V(shape.Max));
        if (shape.Kind == OpenShapeKind.Box)
        {
            Box(picture, min, max, colour, layer);
            return;
        }
        // An upright cylinder: its rims of sixteen sides and four uprights
        var basePoint = V(shape.Base);
        float r = shape.Radius / 1000f;
        Vector3 Rim(int k, float y) => new(basePoint.X + r * MathF.Sin(k * MathF.PI / 8), y, basePoint.Z + r * MathF.Cos(k * MathF.PI / 8));
        for (int k = 0; k < 16; k++)
        {
            Add(picture, Rim(k, min.Y), Rim(k + 1, min.Y), colour, layer);
            Add(picture, Rim(k, max.Y), Rim(k + 1, max.Y), colour, layer);
            if (k % 4 == 0) Add(picture, Rim(k, min.Y), Rim(k, max.Y), colour, layer);
        }
    }

    private static void Box(OpenLayerPicture picture, Vector3 min, Vector3 max, Color colour, DebugLayer layer)
    {
        Vector3 C(int i) => new((i & 1) != 0 ? max.X : min.X, (i & 2) != 0 ? max.Y : min.Y, (i & 4) != 0 ? max.Z : min.Z);
        foreach (var (a, b) in new[] { (0, 1), (2, 3), (4, 5), (6, 7), (0, 2), (1, 3), (4, 6), (5, 7), (0, 4), (1, 5), (2, 6), (3, 7) })
            Add(picture, C(a), C(b), colour, layer);
    }

    private static void Triggers(OpenMap map, Vector3 centre, OpenLayerPicture picture)
    {
        foreach (var trigger in map.Triggers)
        {
            var middle = V(trigger.Shape.Middle);
            if (!Near(centre, middle.X, middle.Z, Reach)) continue;
            Shape(picture, trigger.Shape, TriggerColour, DebugLayer.Triggers);
            string waits = trigger.Variable != null ? $" ({trigger.Variable} == {trigger.Value})" : "";
            picture.Labels.Add(new OpenLayerLabel(V(trigger.Shape.Base) + new Vector3(0, 0.3f, 0), $"{trigger.Name}: {trigger.Script}{waits}", TriggerColour, DebugLayer.Triggers));
        }
    }

    private static void Doors(OpenMap map, Vector3 centre, OpenLayerPicture picture)
    {
        foreach (var door in map.Doors)
        {
            var middle = V(door.Shape.Middle);
            if (!Near(centre, middle.X, middle.Z, Reach)) continue;
            Shape(picture, door.Shape, DoorColour, DebugLayer.Warps);
            string opened = door.OpenedBy != null ? $" once {door.OpenedBy}" : "";
            picture.Labels.Add(new OpenLayerLabel(V(door.Shape.Base) + new Vector3(0, 0.3f, 0), $"{door.Name} -> {door.Room} {door.RoomX},{door.RoomY}{opened}", DoorColour, DebugLayer.Warps));
        }
    }

    private static void Sight(OpenMap map, Vector3 centre, OpenLayerPicture picture)
    {
        foreach (var person in map.People)
        {
            if (person.Sight is not { } sight || !person.Present) continue;
            var feet = V(person.Pos) + new Vector3(0, 0.05f, 0);
            if (!Near(centre, feet.X, feet.Z, Reach + sight.Length / 1000.0)) continue;
            double yaw = OpenYaw.Radians(person.Yaw), half = OpenYaw.Radians(sight.HalfAngle);
            float length = sight.Length / 1000f;
            Vector3 Out(double angle) => feet + new Vector3((float)Math.Sin(angle) * length, 0, (float)Math.Cos(angle) * length);
            Add(picture, feet, Out(yaw - half), SightColour, DebugLayer.Sight);
            Add(picture, feet, Out(yaw + half), SightColour, DebugLayer.Sight);
            const int arc = 12;
            for (int k = 0; k < arc; k++)
                Add(picture, Out(yaw - half + 2 * half * k / arc), Out(yaw - half + 2 * half * (k + 1) / arc), SightColour, DebugLayer.Sight);
            picture.Labels.Add(new OpenLayerLabel(feet + new Vector3(0, 2.1f, 0), $"{person.Key}: {length:0.#} m, {OpenYaw.Radians(sight.HalfAngle) * 360 / Math.PI:0}°", SightColour, DebugLayer.Sight));
        }
    }

    private static void Polygon(OpenLayerPicture picture, IReadOnlyList<(int X, int Z)> polygon, Func<float, float, float> heightAt, Vector3 centre, Color colour, DebugLayer layer)
    {
        for (int k = 0; k < polygon.Count; k++)
        {
            var (x0, z0) = (polygon[k].X / 1000f, polygon[k].Z / 1000f);
            var (x1, z1) = (polygon[(k + 1) % polygon.Count].X / 1000f, polygon[(k + 1) % polygon.Count].Z / 1000f);
            // An edge cut into pieces of two metres or so, each lying on the ground, and only those near the place
            int pieces = Math.Max(1, (int)(MathF.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0)) / 2f));
            for (int i = 0; i < pieces; i++)
            {
                float ax = x0 + (x1 - x0) * i / pieces, az = z0 + (z1 - z0) * i / pieces;
                float bx = x0 + (x1 - x0) * (i + 1) / pieces, bz = z0 + (z1 - z0) * (i + 1) / pieces;
                if (!Near(centre, (ax + bx) / 2, (az + bz) / 2, Reach)) continue;
                Add(picture, new Vector3(ax, heightAt(ax, az), az), new Vector3(bx, heightAt(bx, bz), bz), colour, layer);
            }
        }
    }

    private static Vector3 MiddleOf(IReadOnlyList<(int X, int Z)> polygon)
    {
        double x = 0, z = 0;
        foreach (var p in polygon) { x += p.X; z += p.Z; }
        return new Vector3((float)(x / polygon.Count / 1000), 0, (float)(z / polygon.Count / 1000));
    }

    private static void Areas(OpenMap map, Vector3 centre, OpenLayerPicture picture)
    {
        foreach (var area in map.Areas)
        {
            float top = area.MaxY == int.MaxValue ? 400f : area.MaxY / 1000f, bottom = area.MinY == int.MinValue ? -400f : area.MinY / 1000f;
            float Height(float x, float z) => GroundY(map, x, z, top, bottom, centre.Y) + 0.1f;
            Polygon(picture, area.Polygon, Height, centre, AreaColour, DebugLayer.Areas);
            var middle = MiddleOf(area.Polygon);
            // Its key where the place is in it, else at its middle when that is near
            var at = area.Contains(OpenPos.FromMetres(centre.X, centre.Y, centre.Z)) ? centre : middle with { Y = Height(middle.X, middle.Z) };
            if (Near(centre, at.X, at.Z, Reach)) picture.Labels.Add(new OpenLayerLabel(at + new Vector3(0, 2.6f, 0), area.Key, AreaColour, DebugLayer.Areas));
        }
    }

    private static void Encounters(OpenMap map, OpenField? field, Vector3 centre, OpenLayerPicture picture)
    {
        foreach (var zone in map.Zones)
        {
            float top = zone.MaxY / 1000f, bottom = zone.MinY / 1000f;
            Polygon(picture, zone.Polygon, (x, z) => GroundY(map, x, z, top, bottom, bottom) + 0.15f, centre, ZoneColour, DebugLayer.Encounters);
            var middle = MiddleOf(zone.Polygon);
            if (!Near(centre, middle.X, middle.Z, Reach)) continue;
            int standing = field?.Wild.All.Count(w => w.Zone == zone) ?? 0;
            string slots = string.Join(", ", zone.Slots.Select(s => s.Time == OpenSlotTime.Any ? s.Species : $"{s.Species} ({s.Time})"));
            picture.Labels.Add(new OpenLayerLabel(middle with { Y = GroundY(map, middle.X, middle.Z, top, bottom, bottom) + 1.5f },
                $"{zone.Name}: {standing} of {OpenWildField.TargetOf(zone)}; {slots}", ZoneColour, DebugLayer.Encounters));
        }
        if (field == null) return;
        foreach (var wild in field.Wild.All)
        {
            var feet = V(wild.Pos) + new Vector3(0, 0.05f, 0);
            if (!Near(centre, feet.X, feet.Z, Reach)) continue;
            const float r = 0.5f;
            for (int k = 0; k < 8; k++)
            {
                float a0 = k * MathF.PI / 4, a1 = (k + 1) * MathF.PI / 4;
                Add(picture, feet + new Vector3(r * MathF.Sin(a0), 0, r * MathF.Cos(a0)), feet + new Vector3(r * MathF.Sin(a1), 0, r * MathF.Cos(a1)), WildColour, DebugLayer.Encounters);
            }
            picture.Labels.Add(new OpenLayerLabel(feet + new Vector3(0, 1.2f, 0), $"{wild.Species} {wild.Level} {wild.Kind.Temperament.ToString().ToLowerInvariant()}, {wild.Mood.ToString().ToLowerInvariant()}", WildColour, DebugLayer.Encounters));
        }
    }

    private static void Chunks(OpenMap map, Vector3 centre, OpenLayerPicture picture, Func<int, int, int?>? chunkLevel)
    {
        const float side = OpenMap.ChunkSide / 1000f;
        int cx0 = (int)MathF.Floor((centre.X - (float)Reach) / side), cx1 = (int)MathF.Floor((centre.X + (float)Reach) / side);
        int cz0 = (int)MathF.Floor((centre.Z - (float)Reach) / side), cz1 = (int)MathF.Floor((centre.Z + (float)Reach) / side);
        var have = map.Land?.Chunks.Select(c => (c.X, c.Z)).ToHashSet();
        for (int cz = cz0; cz <= cz1; cz++)
            for (int cx = cx0; cx <= cx1; cx++)
            {
                if (have != null && !have.Contains((cx, cz))) continue;
                int? level = chunkLevel?.Invoke(cx, cz);
                var colour = ChunkColour(level);
                float x0 = cx * side, z0 = cz * side, y = centre.Y + 0.2f;
                // its west and north borders (each border is a chunk's own), and its label at its middle
                Add(picture, new Vector3(x0, y, z0), new Vector3(x0 + side, y, z0), colour, DebugLayer.Chunks);
                Add(picture, new Vector3(x0, y, z0), new Vector3(x0, y, z0 + side), colour, DebugLayer.Chunks);
                string told = level is { } l ? $"level {l}" : "not drawn";
                picture.Labels.Add(new OpenLayerLabel(new Vector3(x0 + side / 2, y + 1f, z0 + side / 2), $"{cx},{cz} {told}", colour, DebugLayer.Chunks));
            }
    }

    private static void Markers(OpenMap map, Vector3 centre, OpenLayerPicture picture)
    {
        foreach (var marker in map.Markers)
        {
            var at = V(marker.Pos) + new Vector3(0, 0.06f, 0);
            if (!Near(centre, at.X, at.Z, Reach)) continue;
            const float arm = 0.4f;
            Add(picture, at - new Vector3(arm, 0, 0), at + new Vector3(arm, 0, 0), MarkerColour, DebugLayer.Markers);
            Add(picture, at - new Vector3(0, 0, arm), at + new Vector3(0, 0, arm), MarkerColour, DebugLayer.Markers);
            double yaw = OpenYaw.Radians(marker.Yaw);
            Add(picture, at, at + new Vector3((float)Math.Sin(yaw), 0, (float)Math.Cos(yaw)) * 0.9f, MarkerColour, DebugLayer.Markers);
            Add(picture, at, at + new Vector3(0, 1.6f, 0), MarkerColour, DebugLayer.Markers);
            picture.Labels.Add(new OpenLayerLabel(at + new Vector3(0, 1.8f, 0), marker.Name, MarkerColour, DebugLayer.Markers));
        }
    }

    /// <summary>A line of what the picture holds, by layer, for a tool's print.</summary>
    public static string Describe(OpenLayerPicture picture) =>
        string.Join(", ", picture.Lines.GroupBy(l => l.Layer).OrderBy(g => g.Key).Select(g => string.Create(CultureInfo.InvariantCulture, $"{g.Key} {g.Count()} lines")))
        + $"; {picture.Labels.Count} labels";
}
