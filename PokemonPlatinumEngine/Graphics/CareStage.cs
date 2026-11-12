using System;
using System.Numerics;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Where things lie on the care close-up's picture (plan 35 · BN2; style guide, "The care close-up"), with no GPU: the
/// Pokémon's head, the middle of its body (where the marks of soil stand) and the spot it likes stroked, each a point of
/// the picture from 0 to 1 across and down. Worked out from the model's own mesh through the close-up's camera (the
/// gallery's framing, turned to face the player), or, before the model is built, from its body plan alone.
/// </summary>
internal static class CareStage
{
    /// <summary>The close-up's turn over the gallery's framing: half a radian back, so the model faces the player.</summary>
    public const float Turn = 0.5f;

    /// <summary>How much of the picture the model's frame fills (the gallery's first zoom's share).</summary>
    public const float Zoom = 680f / 1120f;

    /// <summary>How near the hand must be to a spot to stroke it, in the picture's width.</summary>
    public const float Reach = 0.12f;

    /// <summary>The places of the picture the close-up asks for.</summary>
    public readonly record struct Layout(Vector2 Head, Vector2 Body, Vector2 Liked, Vector2 Low, Vector2 High)
    {
        /// <summary>Where the marks of soil stand: the first a little left of the body's middle, the second a little right.</summary>
        public Vector2 Mark(int index) => Body + new Vector2(index == 0 ? -0.07f : 0.07f, 0.02f);

        /// <summary>Whether a point of the picture is on the body: within the box of its silhouette, a tenth in from each side.</summary>
        public bool OnBody(Vector2 at)
        {
            var inset = (High - Low) * 0.1f;
            return at.X >= Low.X + inset.X && at.X <= High.X - inset.X && at.Y >= Low.Y + inset.Y && at.Y <= High.Y - inset.Y;
        }
    }

    /// <summary>The spot each body plan likes stroked: a biped's head, a quadruped's back, a bird's neck, a fish's fin.</summary>
    public static PokeRole LikedRole(BodyPlan plan) => plan switch
    {
        BodyPlan.Quadruped => PokeRole.Body,
        BodyPlan.Fish => PokeRole.Fin,
        _ => PokeRole.Head
    };

    /// <summary>Before the model is built: a head high in the middle, a body below it, the liked spot by the body plan.</summary>
    public static Layout Fallback(BodyPlan plan)
    {
        var head = new Vector2(0.5f, 0.36f);
        var body = new Vector2(0.5f, 0.58f);
        var liked = plan switch
        {
            BodyPlan.Quadruped => new Vector2(0.5f, 0.5f),
            BodyPlan.Bird => new Vector2(0.5f, 0.45f),
            BodyPlan.Fish => new Vector2(0.62f, 0.56f),
            _ => head
        };
        return new Layout(head, body, liked, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.85f));
    }

    /// <summary>The layout of a built model: each part the middle of the vertices its bones of that role carry most.</summary>
    public static Layout Of(PokeModel model)
    {
        var framing = PokemonSprites.Framing(model, SpriteView.Turntable, 512);
        var camera = framing.Camera;
        float frame = camera.FovY / Zoom;
        var up = Vector3.Normalize(camera.Up);
        var target = camera.Target;
        var turn = Matrix4x4.CreateRotationY(framing.Yaw + Turn);
        Vector2 Project(Vector3 p)
        {
            var q = Vector3.Transform(p, turn);
            return new Vector2(0.5f + (q.X - target.X) / frame, 0.5f - (Vector3.Dot(q, up) - Vector3.Dot(target, up)) / frame);
        }

        var mesh = model.Mesh;
        Vector3 sumHead = default, sumBody = default, sumLiked = default;
        int nHead = 0, nBody = 0, nLiked = 0;
        float bodyTop = float.MinValue;
        Vector2 low = new(float.MaxValue), high = new(float.MinValue);
        var liked = LikedRole(model.Plan);
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            int best = mesh.BoneIndices.Length > v * 4 ? mesh.BoneIndices[v * 4] : 0;
            float weight = 0f;
            for (int k = 0; k < 4 && mesh.BoneWeights.Length > v * 4 + k; k++)
                if (mesh.BoneWeights[v * 4 + k] > weight) { weight = mesh.BoneWeights[v * 4 + k]; best = mesh.BoneIndices[v * 4 + k]; }
            if (best >= model.Bones.Count) continue;
            var role = model.Bones[best].Role;
            var p = mesh.Positions[v];
            var at = Project(p);
            low = Vector2.Min(low, at);
            high = Vector2.Max(high, at);
            if (role == PokeRole.Head) { sumHead += p; nHead++; }
            if (role is PokeRole.Body or PokeRole.Segment or PokeRole.Root) { sumBody += p; nBody++; bodyTop = MathF.Max(bodyTop, p.Y); }
            if (role == liked) { sumLiked += p; nLiked++; }
        }
        var fallback = Fallback(model.Plan);
        var body = nBody > 0 ? Project(sumBody / nBody) : fallback.Body;
        var head = nHead > 0 ? Project(sumHead / nHead) : fallback.Head;
        Vector2 likedSpot;
        if (model.Plan == BodyPlan.Quadruped && nBody > 0)
        {
            // A quadruped's back: over the body's middle, most of the way to its top
            var middle = sumBody / nBody;
            likedSpot = Project(new Vector3(middle.X, middle.Y + 0.7f * (bodyTop - middle.Y), middle.Z));
        }
        else if (model.Plan == BodyPlan.Bird && nHead > 0 && nBody > 0) likedSpot = Vector2.Lerp(head, body, 0.4f);
        else likedSpot = nLiked > 0 ? Project(sumLiked / nLiked) : model.Plan == BodyPlan.Fish ? body : head;
        if (low.X > high.X) (low, high) = (fallback.Low, fallback.High);
        return new Layout(head, body, likedSpot, low, high);
    }
}
