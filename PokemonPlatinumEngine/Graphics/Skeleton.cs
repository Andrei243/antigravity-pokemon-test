using System;
using System.Collections.Generic;
using System.Numerics;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A hierarchy of bones in model space. Each bone turns about its joint (its position in the bind pose, the pose
/// the model was sculpted in); children follow their parent. Bones carry no rotation of their own at rest, so a
/// bone's skinning matrix maps a point of the sculpted model straight to where it is in a pose.
/// </summary>
internal sealed class Skeleton
{
    public readonly record struct Bone(string Name, int Parent, Vector3 Joint);

    private readonly List<Bone> bones = new();
    private readonly Dictionary<string, int> byName = new(StringComparer.OrdinalIgnoreCase);

    public int Count => bones.Count;

    public IReadOnlyList<Bone> Bones => bones;

    public Bone this[int index] => bones[index];

    /// <summary>Adds a bone; parents must be added before their children.</summary>
    public int Add(string name, int parent, Vector3 joint)
    {
        if (parent >= bones.Count) throw new ArgumentException("parents come before their children", nameof(parent));
        bones.Add(new Bone(name, parent, joint));
        byName[name] = bones.Count - 1;
        return bones.Count - 1;
    }

    public int Find(string name) => byName.TryGetValue(name, out int i) ? i : -1;

    /// <summary>
    /// Skinning matrices for <paramref name="pose"/> (System.Numerics row-vector convention): each maps a point
    /// of the sculpted model to where the bone has taken it.
    /// </summary>
    public void Evaluate(SkeletonPose pose, Span<Matrix4x4> skin)
    {
        for (int b = 0; b < bones.Count; b++)
        {
            var joint = bones[b].Joint;
            var local = Matrix4x4.CreateTranslation(-joint)
                * Matrix4x4.CreateScale(pose.Scale[b])
                * Matrix4x4.CreateFromQuaternion(pose.Rotation[b])
                * Matrix4x4.CreateTranslation(joint + pose.Offset[b]);
            int parent = bones[b].Parent;
            skin[b] = parent < 0 ? local : local * skin[parent];
        }
    }

    /// <summary>Where a point of bone <paramref name="b"/>'s sculpted geometry is in the pose that gave <paramref name="skin"/>.</summary>
    public static Vector3 Transform(Vector3 bindPoint, int b, ReadOnlySpan<Matrix4x4> skin) => Vector3.Transform(bindPoint, skin[b]);
}

/// <summary>A rotation, offset and scale for every bone of a <see cref="Skeleton"/>; identity is the sculpted pose.</summary>
internal sealed class SkeletonPose
{
    public readonly Quaternion[] Rotation;
    public readonly Vector3[] Offset;
    public readonly Vector3[] Scale;

    public SkeletonPose(int bones)
    {
        Rotation = new Quaternion[bones];
        Offset = new Vector3[bones];
        Scale = new Vector3[bones];
        Reset();
    }

    public int Count => Rotation.Length;

    public void Reset()
    {
        Array.Fill(Rotation, Quaternion.Identity);
        Array.Fill(Offset, Vector3.Zero);
        Array.Fill(Scale, Vector3.One);
    }

    /// <summary>Turns bone <paramref name="b"/> further by Euler angles in radians (pitch about X, yaw about Y, roll about Z).</summary>
    public void Turn(int b, float pitch, float yaw = 0f, float roll = 0f)
    {
        if (b < 0) return;
        Rotation[b] = Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll) * Rotation[b]);
    }

    public void CopyFrom(SkeletonPose other)
    {
        Array.Copy(other.Rotation, Rotation, Count);
        Array.Copy(other.Offset, Offset, Count);
        Array.Copy(other.Scale, Scale, Count);
    }

    /// <summary>Moves this pose toward <paramref name="other"/> by <paramref name="t"/> (0 keeps this pose, 1 takes the other).</summary>
    public void BlendToward(SkeletonPose other, float t)
    {
        if (t <= 0f) return;
        if (t >= 1f)
        {
            CopyFrom(other);
            return;
        }
        for (int b = 0; b < Count; b++)
        {
            Rotation[b] = Quaternion.Slerp(Rotation[b], other.Rotation[b], t);
            Offset[b] = Vector3.Lerp(Offset[b], other.Offset[b], t);
            Scale[b] = Vector3.Lerp(Scale[b], other.Scale[b], t);
        }
    }
}
