using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using PokemonPlatinumEngine.Models.StageShow;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Where a slot's piece sits on a model (plan 42 · PS1), in the model's sculpted pose: on bone <see cref="Bone"/>, at
/// <see cref="Position"/> (a vertex of the mesh, on its surface), with a frame of <see cref="Up"/> (off the surface)
/// and <see cref="Forward"/> (the wearer's front), and <see cref="Size"/>, the head's half width, which is a unit of a
/// prop's sculpt. A neck's anchor also has the seam's middle (<see cref="Center"/>) and its half width and half depth
/// (<see cref="Ring"/>), which the pieces that go round the neck are fitted to.
/// </summary>
internal readonly record struct PropAnchor(PropSlot Slot, int Bone, Vector3 Position, Vector3 Up, Vector3 Forward, float Size, Vector3 Center, Vector2 Ring);

/// <summary>
/// The anchors of the stage show's slots on a built model (plan 42 · PS1; style guide, "Pokémon (version 2, G7)",
/// "Props worn"). GPU-free: read from the model's mesh as <see cref="Footprints.Make"/> reads a foot, from the vertices
/// weighing at least <see cref="Weight"/> on a bone of the slot's role. A slot whose anchor isn't found is not offered
/// (<see cref="SlotsOf"/>); a model read from a file offers none. The same model always gives the same anchors.
/// </summary>
internal static class PropAnchors
{
    /// <summary>The weight a vertex gives a bone to be part of it (the decals' and the footprints' rule).</summary>
    public const float Weight = 0.3f;

    /// <summary>The fewest vertices a head must have to be one (a head that is part of the body's shape has next to none).</summary>
    public const int LeastHead = 12;

    /// <summary>The fewest vertices an arm's or a tail's end must have.</summary>
    public const int LeastLimb = 3;

    private static readonly ConditionalWeakTable<PokeModel, IReadOnlyDictionary<PropSlot, PropAnchor>> Known = new();

    /// <summary>The anchors of every slot the model has, made the first time and kept with the model.</summary>
    public static IReadOnlyDictionary<PropSlot, PropAnchor> Of(PokeModel model) => Known.GetValue(model, Make);

    /// <summary>The slots a model offers, in the slots' order.</summary>
    public static IReadOnlyList<PropSlot> SlotsOf(PokeModel model) => Of(model).Keys.OrderBy(s => s).ToList();

    /// <summary>
    /// Where a piece of <paramref name="shape"/> is in the model's sculpted space, as a matrix of the prop's unit space
    /// (System.Numerics row vectors: the piece's point times this, then times the anchor bone's skinning matrix).
    /// </summary>
    public static Matrix4x4 Placement(PropAnchor a, string shape)
    {
        var up = a.Up;
        var forward = a.Forward;
        var right = Vector3.Normalize(Vector3.Cross(up, forward));
        var basis = new Matrix4x4(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            forward.X, forward.Y, forward.Z, 0f,
            0f, 0f, 0f, 1f);
        if (ShowPropModels.WrapsNeck(shape) && a.Ring.X > 0f)
            return Matrix4x4.CreateScale(a.Ring.X * 1.08f, a.Size, a.Ring.Y * 1.08f) * basis * Matrix4x4.CreateTranslation(a.Center);
        return Matrix4x4.CreateScale(a.Size) * basis * Matrix4x4.CreateTranslation(a.Position);
    }

    // ------------------------------------------------------------------ finding them

    private static IReadOnlyDictionary<PropSlot, PropAnchor> Make(PokeModel model)
    {
        var found = new Dictionary<PropSlot, PropAnchor>();
        var mesh = model.Mesh;
        if (model.Imported != null || mesh.VertexCount == 0 || model.Bones.Count == 0) return found;

        int headBone = model.Find(PokeRole.Head);
        var head = headBone >= 0 ? On(model, headBone) : new List<int>();
        var eyes = model.Decals.Where(d => d.IsEye).ToList();
        int eyeBone = eyes.Count > 0 ? eyes.GroupBy(e => e.Bone).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key : -1;

        // The head: its own bone, or where a head is part of the body's shape (Happiny's line), the bone its eyes are on
        bool ownHead = head.Count >= LeastHead;
        int crownBone = ownHead ? headBone : eyeBone;
        var crown = ownHead ? head : eyeBone >= 0 ? On(model, eyeBone) : new List<int>();

        int bodyBone = model.Find(PokeRole.Body);
        var body = bodyBone >= 0 ? On(model, bodyBone) : new List<int>();

        // The size every piece is scaled by: the head's half width (the body's, a little less, without one)
        float size;
        if (ownHead) size = HalfWidth(mesh, head);
        else if (eyes.Count >= 2) size = MathF.Max(EyeSpread(eyes) * 1.1f, HalfWidth(mesh, crown) * 0.55f);
        else size = HalfWidth(mesh, body.Count > 0 ? body : Enumerable.Range(0, mesh.VertexCount).ToList()) * 0.55f;
        size = MathF.Max(size, 3f * MathF.Max(model.Cell, 0.002f));

        if (crown.Count > 0 && (ownHead || eyeBone >= 0) && Crown(model, crown) is int top)
            found[PropSlot.Head] = new PropAnchor(PropSlot.Head, crownBone, mesh.Positions[top], Vector3.UnitY, Vector3.UnitZ, size, mesh.Positions[top], Vector2.Zero);

        if (Face(model, eyes, eyeBone, ownHead ? head : null, size) is { } face)
            found[PropSlot.Face] = face with { Size = size };

        if (ownHead && Neck(model, headBone, size) is { } neck)
            found[PropSlot.Neck] = neck;

        if (body.Count > 0 && Back(model, bodyBone, body, size) is { } back)
            found[PropSlot.Back] = back;

        if (LimbEnd(model, PokeRole.Arm, size) is { } hand)
            found[PropSlot.Hand] = hand with { Slot = PropSlot.Hand };

        if (LimbEnd(model, PokeRole.Tail, size) is { } tail)
            found[PropSlot.Tail] = tail with { Slot = PropSlot.Tail };

        return found;
    }

    /// <summary>The vertices weighing at least <see cref="Weight"/> on a bone, in index order.</summary>
    internal static List<int> On(PokeModel model, int bone)
    {
        var mesh = model.Mesh;
        var list = new List<int>();
        for (int v = 0; v < mesh.VertexCount; v++)
            if (mesh.WeightOf(v, bone) >= Weight) list.Add(v);
        return list;
    }

    private static float HalfWidth(SdfMesh mesh, List<int> verts)
    {
        if (verts.Count == 0) return 0f;
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (int v in verts)
        {
            lo = MathF.Min(lo, mesh.Positions[v].X);
            hi = MathF.Max(hi, mesh.Positions[v].X);
        }
        return (hi - lo) / 2f;
    }

    private static float EyeSpread(List<PokeDecal> eyes)
    {
        float lo = eyes.Min(e => e.Center.X), hi = eyes.Max(e => e.Center.X);
        return (hi - lo) / 2f;
    }

    private static (float X, float Z) MiddleXZ(SdfMesh mesh, List<int> verts)
    {
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
        foreach (int v in verts)
        {
            var p = mesh.Positions[v];
            x0 = MathF.Min(x0, p.X); x1 = MathF.Max(x1, p.X);
            z0 = MathF.Min(z0, p.Z); z1 = MathF.Max(z1, p.Z);
        }
        return ((x0 + x1) / 2f, (z0 + z1) / 2f);
    }

    /// <summary>
    /// The top of a head: the highest vertex facing up near the head's middle, so a crest, a horn or an ear on the
    /// head's own bone doesn't lift the hat (then wider, then any vertex facing up, then the highest).
    /// </summary>
    private static int? Crown(PokeModel model, List<int> verts)
    {
        var mesh = model.Mesh;
        float half = HalfWidth(mesh, verts);
        var (cx, cz) = MiddleXZ(mesh, verts);
        foreach (var (reach, facing) in new[] { (0.45f, 0.55f), (0.8f, 0.35f), (float.MaxValue, 0.2f), (float.MaxValue, -2f) })
        {
            int best = -1;
            float bestY = float.MinValue;
            foreach (int v in verts)
            {
                var p = mesh.Positions[v];
                if (mesh.Normals[v].Y < facing) continue;
                float dx = p.X - cx, dz = p.Z - cz;
                if (reach != float.MaxValue && dx * dx + dz * dz > reach * reach * half * half) continue;
                if (p.Y > bestY) { bestY = p.Y; best = v; }
            }
            if (best >= 0) return best;
        }
        return null;
    }

    /// <summary>
    /// The face's front between the eyes: the surface met going out from the eyes' middle along their normals (the
    /// outermost vertex of the eyes' bone near that line), framed by the line; without eyes, the head's frontmost
    /// vertex near its middle.
    /// </summary>
    private static PropAnchor? Face(PokeModel model, List<PokeDecal> eyes, int eyeBone, List<int>? head, float size)
    {
        var mesh = model.Mesh;
        if (eyes.Count > 0 && eyeBone >= 0)
        {
            var mine = eyes.Where(e => e.Bone == eyeBone).ToList();
            var mid = mine.Aggregate(Vector3.Zero, (s, e) => s + e.Center) / mine.Count;
            var n = mine.Aggregate(Vector3.Zero, (s, e) => s + e.Normal);
            n = n.LengthSquared() > 1e-8f ? Vector3.Normalize(n) : Vector3.UnitZ;
            if (n.Z < 0.2f) n = Vector3.Normalize(n with { Z = MathF.Max(n.Z, 0.2f) });
            var verts = On(model, eyeBone);
            float reach = MathF.Max(2f * model.Cell, 0.25f * size);
            int best = -1;
            float bestOut = float.MinValue;
            foreach (int v in verts)
            {
                var d = mesh.Positions[v] - mid;
                float along = Vector3.Dot(d, n);
                if ((d - along * n).Length() > reach) continue;
                if (along > bestOut) { bestOut = along; best = v; }
            }
            if (best < 0)
            {
                // The eyes' own middle, at the nearest vertex
                float nearest = float.MaxValue;
                foreach (int v in verts)
                {
                    float d = Vector3.DistanceSquared(mesh.Positions[v], mid);
                    if (d < nearest) { nearest = d; best = v; }
                }
            }
            if (best < 0) return null;
            var up = Vector3.UnitY - Vector3.Dot(Vector3.UnitY, n) * n;
            up = up.LengthSquared() > 1e-6f ? Vector3.Normalize(up) : Vector3.UnitY;
            return new PropAnchor(PropSlot.Face, eyeBone, mesh.Positions[best], up, n, size, mesh.Positions[best], Vector2.Zero);
        }
        if (head == null || head.Count == 0) return null;
        {
            float half = HalfWidth(mesh, head);
            var (cx, _) = MiddleXZ(mesh, head);
            int best = -1;
            float bestZ = float.MinValue;
            foreach (int v in head)
            {
                var p = mesh.Positions[v];
                if (MathF.Abs(p.X - cx) > 0.35f * half || mesh.Normals[v].Z < 0.4f) continue;
                if (p.Z > bestZ) { bestZ = p.Z; best = v; }
            }
            if (best < 0) return null;
            int bone = model.Find(PokeRole.Head);
            return new PropAnchor(PropSlot.Face, bone, mesh.Positions[best], Vector3.UnitY, Vector3.UnitZ, size, mesh.Positions[best], Vector2.Zero);
        }
    }

    /// <summary>
    /// The neck: the seam where the head's weight gives way to the bone it hangs from (vertices weighing between a
    /// fifth and four fifths on the head and as much again on its parent). The anchor is the seam's front; its middle
    /// and its half width and depth are what a ring is fitted to.
    /// </summary>
    private static PropAnchor? Neck(PokeModel model, int headBone, float size)
    {
        var mesh = model.Mesh;
        int parent = model.Skeleton[headBone].Parent;
        if (parent < 0) return null;
        var seam = new List<int>();
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            float wh = mesh.WeightOf(v, headBone), wp = mesh.WeightOf(v, parent);
            if (wh >= 0.2f && wh <= 0.8f && wp >= 0.2f) seam.Add(v);
        }
        if (seam.Count < 6) return null;
        var c = Vector3.Zero;
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
        int front = -1;
        float frontZ = float.MinValue;
        foreach (int v in seam)
        {
            var p = mesh.Positions[v];
            c += p;
            x0 = MathF.Min(x0, p.X); x1 = MathF.Max(x1, p.X);
            z0 = MathF.Min(z0, p.Z); z1 = MathF.Max(z1, p.Z);
            if (p.Z > frontZ && MathF.Abs(p.X - (x0 + x1) / 2f) < float.MaxValue) { frontZ = p.Z; front = v; }
        }
        c /= seam.Count;
        // The front: the seam's frontmost vertex near its middle line
        float halfW = (x1 - x0) / 2f, mx = (x0 + x1) / 2f;
        frontZ = float.MinValue;
        foreach (int v in seam)
        {
            var p = mesh.Positions[v];
            if (MathF.Abs(p.X - mx) > 0.4f * halfW + model.Cell) continue;
            if (p.Z > frontZ) { frontZ = p.Z; front = v; }
        }
        var ring = new Vector2(MathF.Max(halfW, 2f * model.Cell), MathF.Max((z1 - z0) / 2f, 2f * model.Cell));
        var middle = new Vector3(mx, c.Y, (z0 + z1) / 2f);
        return new PropAnchor(PropSlot.Neck, headBone, mesh.Positions[front], Vector3.UnitY, Vector3.UnitZ, size, middle, ring);
    }

    /// <summary>
    /// The back's highest point: the body bone's highest vertex behind its joint facing up or back near its middle line,
    /// framed by its surface (up out of the back, forward toward the front, or toward the head where the back faces
    /// straight behind).
    /// </summary>
    private static PropAnchor? Back(PokeModel model, int bodyBone, List<int> body, float size)
    {
        var mesh = model.Mesh;
        float joint = model.Skeleton[bodyBone].Joint.Z;
        float half = HalfWidth(mesh, body);
        var (cx, _) = MiddleXZ(mesh, body);
        int best = -1;
        float bestY = float.MinValue;
        foreach (var (reach, behind) in new[] { (0.3f, true), (0.6f, true), (0.6f, false) })
        {
            foreach (int v in body)
            {
                var p = mesh.Positions[v];
                var n = mesh.Normals[v];
                if (MathF.Abs(p.X - cx) > reach * half + model.Cell) continue;
                if (behind && p.Z > joint) continue;
                if (n.Y < 0.15f && n.Z > -0.3f) continue;
                if (p.Y > bestY) { bestY = p.Y; best = v; }
            }
            if (best >= 0) break;
        }
        if (best < 0) return null;
        var up = mesh.Normals[best];
        up = up.LengthSquared() > 1e-6f ? Vector3.Normalize(up with { X = 0f }) : Vector3.UnitY;
        if (up.LengthSquared() < 1e-6f || float.IsNaN(up.X)) up = Vector3.UnitY;
        var forward = Vector3.UnitZ - Vector3.Dot(Vector3.UnitZ, up) * up;
        forward = forward.LengthSquared() > 1e-4f ? Vector3.Normalize(forward) : Vector3.Normalize(Vector3.UnitY - Vector3.Dot(Vector3.UnitY, up) * up);
        return new PropAnchor(PropSlot.Back, bodyBone, mesh.Positions[best], up, forward, size, mesh.Positions[best], Vector2.Zero);
    }

    /// <summary>
    /// The end of a chain of bones of a role (an arm, a tail): the chain's last bone (the right arm's first, as the
    /// footprints pick a foot; the deepest tail), and on it the vertex furthest from the chain's first joint.
    /// </summary>
    private static PropAnchor? LimbEnd(PokeModel model, PokeRole role, float size)
    {
        var mesh = model.Mesh;
        var bones = model.Bones;
        int Depth(int b)
        {
            int d = 0;
            for (int p = model.Skeleton[b].Parent; p >= 0 && p < bones.Count && bones[p].Role == role; p = model.Skeleton[p].Parent) d++;
            return d;
        }
        int ChainRoot(int b)
        {
            int r = b;
            for (int p = model.Skeleton[b].Parent; p >= 0 && p < bones.Count && bones[p].Role == role; p = model.Skeleton[p].Parent) r = p;
            return r;
        }
        var ends = new List<int>();
        for (int b = 0; b < bones.Count && b < model.Skeleton.Count; b++)
        {
            if (bones[b].Role != role) continue;
            bool hasChild = false;
            for (int c = 0; c < bones.Count && c < model.Skeleton.Count; c++)
                if (model.Skeleton[c].Parent == b && bones[c].Role == role) { hasChild = true; break; }
            if (!hasChild) ends.Add(b);
        }
        if (ends.Count == 0) return null;
        // Arms: the right one (x < 0, the wearer's right as it faces us) first, then the middle, then the left;
        // tails: the deepest end. Ties to the first bone.
        IEnumerable<int> order = role == PokeRole.Arm
            ? ends.OrderBy(b => bones[b].Side < 0 ? 0 : bones[b].Side == 0 ? 1 : 2).ThenBy(b => b)
            : ends.OrderByDescending(Depth).ThenBy(b => b);
        foreach (int end in order)
        {
            // An end with too little of its own gives way to the bone it hangs from, within the chain
            for (int b = end; b >= 0 && bones[b].Role == role; b = model.Skeleton[b].Parent)
            {
                var verts = On(model, b);
                if (verts.Count < LeastLimb) continue;
                var from = model.Skeleton[ChainRoot(b)].Joint;
                int tip = -1;
                float far = -1f;
                foreach (int v in verts)
                {
                    float d = Vector3.DistanceSquared(mesh.Positions[v], from);
                    if (d > far) { far = d; tip = v; }
                }
                return new PropAnchor(role == PokeRole.Arm ? PropSlot.Hand : PropSlot.Tail, b, mesh.Positions[tip], Vector3.UnitY, Vector3.UnitZ,
                    size * (role == PokeRole.Arm ? 0.6f : 0.55f), mesh.Positions[tip], Vector2.Zero);
            }
        }
        return null;
    }
}
