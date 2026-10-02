using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>The SDF modelling kit (plan 04 · G6): shapes, smooth unions, cuts, paint, meshing, weights and the mesh cache.</summary>
public class SdfKitTests
{
    private static readonly Color Red = new(220, 40, 40, 255);
    private static readonly Color Blue = new(40, 60, 220, 255);

    /// <summary>
    /// A closed, consistently wound surface: every edge is used as often in one direction as in the other. (Surface
    /// nets may pinch two sheets together along an edge where a feature is about two cells thin; that edge then
    /// has four triangles, two each way, which is still watertight.)
    /// </summary>
    private static void AssertClosed(SdfMesh mesh)
    {
        var edges = new Dictionary<(int, int), int>();
        for (int t = 0; t < mesh.Indices.Length; t += 3)
            for (int e = 0; e < 3; e++)
            {
                int a = mesh.Indices[t + e], b = mesh.Indices[t + (e + 1) % 3];
                edges[(a, b)] = edges.GetValueOrDefault((a, b)) + 1;
            }
        int pinched = 0;
        foreach (var ((a, b), n) in edges)
        {
            Assert.Equal(n, edges.GetValueOrDefault((b, a)));
            if (n > 1) pinched++;
        }
        Assert.True(pinched <= edges.Count / 200, $"{pinched} of {edges.Count} edges are pinched");
    }

    /// <summary>Volume by the divergence theorem: positive when the triangles face outward.</summary>
    private static float SignedVolume(SdfMesh mesh)
    {
        float v = 0f;
        for (int t = 0; t < mesh.Indices.Length; t += 3)
        {
            var a = mesh.Positions[mesh.Indices[t]];
            var b = mesh.Positions[mesh.Indices[t + 1]];
            var c = mesh.Positions[mesh.Indices[t + 2]];
            v += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
        }
        return v;
    }

    [Fact]
    public void ASphereMeshesIntoAClosedOutwardSurfaceOnTheSphere()
    {
        var model = new SdfModel("ball");
        model.Sphere(new Vector3(0.1f, 0.5f, -0.2f), 0.3f, Red, 0);
        var mesh = SdfMesher.Mesh(model, 0.02f);

        Assert.True(mesh.VertexCount > 500);
        AssertClosed(mesh);
        float volume = SignedVolume(mesh);
        Assert.InRange(volume, 0.95f * 4f / 3f * MathF.PI * 0.027f, 1.03f * 4f / 3f * MathF.PI * 0.027f);
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            var p = mesh.Positions[v] - new Vector3(0.1f, 0.5f, -0.2f);
            Assert.InRange(p.Length(), 0.297f, 0.303f);
            Assert.True(Vector3.Dot(mesh.Normals[v], Vector3.Normalize(p)) > 0.99f);
            Assert.Equal(Red, mesh.Colors[v]);
            Assert.Equal(1f, mesh.WeightOf(v, 0), 3);
        }
    }

    [Theory]
    [InlineData("Ellipsoid")]
    [InlineData("RoundCone")]
    [InlineData("RoundBox")]
    [InlineData("Torus")]
    [InlineData("Cylinder")]
    public void EveryShapeMeshesClosedAndFacingOut(string shapeName)
    {
        var shape = Enum.Parse<SdfShape>(shapeName);
        var model = new SdfModel(shapeName);
        var tilt = Quaternion.CreateFromYawPitchRoll(0.4f, 0.3f, 0.2f);
        switch (shape)
        {
            case SdfShape.Ellipsoid: model.Ellipsoid(Vector3.Zero, new Vector3(0.3f, 0.2f, 0.12f), Red, 0, rotation: tilt); break;
            case SdfShape.RoundCone: model.Capsule(new Vector3(0, -0.2f, 0), new Vector3(0.1f, 0.25f, 0.05f), 0.12f, 0.05f, Red, 0); break;
            case SdfShape.RoundBox: model.Box(Vector3.Zero, new Vector3(0.25f, 0.15f, 0.1f), 0.04f, Red, 0, rotation: tilt); break;
            case SdfShape.Torus: model.Torus(Vector3.Zero, 0.25f, 0.06f, Red, 0, rotation: tilt); break;
            case SdfShape.Cylinder: model.Cylinder(Vector3.Zero, 0.15f, 0.2f, 0.03f, Red, 0, rotation: tilt); break;
        }
        var mesh = SdfMesher.Mesh(model, 0.015f);
        AssertClosed(mesh);
        Assert.True(SignedVolume(mesh) > 0f);

        // Every vertex sits on the surface, and its normal points out of the shape
        foreach (var (p, n) in mesh.Positions.Zip(mesh.Normals))
        {
            Assert.InRange(model.Distance(p), -0.004f, 0.004f);
            Assert.True(model.Distance(p + n * 0.01f) > model.Distance(p - n * 0.01f));
        }
    }

    [Fact]
    public void ASmoothUnionFillsTheCreaseAndBlendsColourAndWeight()
    {
        var hard = new SdfModel("hard");
        hard.Sphere(new Vector3(-0.15f, 0, 0), 0.2f, Red, 0);
        hard.Sphere(new Vector3(0.15f, 0, 0), 0.2f, Blue, 1);
        hard.BoneCount = 2;
        var smooth = new SdfModel("smooth") { BoneCount = 2 };
        smooth.Sphere(new Vector3(-0.15f, 0, 0), 0.2f, Red, 0);
        smooth.Sphere(new Vector3(0.15f, 0, 0), 0.2f, Blue, 1, blend: 0.1f);

        // Where the spheres meet, above the middle, the smooth union bulges out over the crease
        var crease = new Vector3(0, 0.14f, 0);
        Assert.True(smooth.Distance(crease) < hard.Distance(crease) - 0.01f);

        Span<float> w = stackalloc float[2];
        var middle = smooth.Surface(new Vector3(0, 0.16f, 0), w);
        Assert.InRange(middle.Color.X, 0.3f, 0.7f);    // half red, half blue
        Assert.InRange(w[0], 0.3f, 0.7f);
        Assert.Equal(1f, w[0] + w[1], 3);
        var side = smooth.Surface(new Vector3(-0.35f, 0, 0), w);
        Assert.Equal(1f, w[0], 3);
        Assert.True(side.Color.X > 0.8f);
    }

    [Fact]
    public void ACutCarvesAndPaintRecoloursWithoutChangingTheShape()
    {
        var model = new SdfModel("carved");
        model.Box(Vector3.Zero, new Vector3(0.3f, 0.3f, 0.3f), 0.02f, Red, 0);
        model.Sphere(new Vector3(0, 0.3f, 0), 0.15f, Red, 0, op: SdfOp.Cut);
        Assert.True(model.Distance(new Vector3(0, 0.25f, 0)) > 0f);   // carved out
        Assert.True(model.Distance(new Vector3(0.2f, 0.25f, 0)) < 0f); // still solid beside the cut

        float before = model.Distance(new Vector3(0.3f, 0f, 0f));
        model.Paint(model.Box(new Vector3(0.3f, 0, 0), new Vector3(0.1f, 0.5f, 0.5f), 0f, Blue, 0), paintsMaterial: true).Material = SurfaceMaterial.Metal;
        Assert.Equal(before, model.Distance(new Vector3(0.3f, 0f, 0f)), 5);
        Span<float> w = stackalloc float[1];
        var painted = model.Surface(new Vector3(0.3f, 0f, 0f), w);
        Assert.True(painted.Color.Z > 0.8f);
        Assert.Equal(SurfaceMaterial.Metal, painted.Material);
        Assert.True(model.Surface(new Vector3(-0.3f, 0f, 0f), w).Color.X > 0.8f);

        var mesh = SdfMesher.Mesh(model, 0.02f);
        AssertClosed(mesh);
    }

    [Fact]
    public void MeshesSurviveTheDiskCacheUnchanged()
    {
        var model = new SdfModel("cache test " + Guid.NewGuid().ToString("N")[..6]) { BoneCount = 2 };
        model.Capsule(new Vector3(0, 0, 0), new Vector3(0, 0.4f, 0), 0.1f, 0.08f, Red, 0);
        model.Sphere(new Vector3(0, 0.5f, 0), 0.12f, Blue, 1, blend: 0.05f);
        var mesh = SdfMesher.Mesh(model, 0.02f);

        string path = Path.Combine(Path.GetTempPath(), $"sdf-{Guid.NewGuid():N}.mesh");
        try
        {
            SdfCache.Write(path, mesh);
            var back = SdfCache.Read(path);
            Assert.Equal(mesh.Positions, back.Positions);
            Assert.Equal(mesh.Normals, back.Normals);
            Assert.Equal(mesh.Colors, back.Colors);
            Assert.Equal(mesh.Materials, back.Materials);
            Assert.Equal(mesh.BoneIndices, back.BoneIndices);
            Assert.Equal(mesh.BoneWeights, back.BoneWeights);
            Assert.Equal(mesh.Indices, back.Indices);
        }
        finally
        {
            File.Delete(path);
        }

        // The key follows the model: any change to a shape gives a new one
        string key = SdfCache.Key(model, 0.02f);
        Assert.Equal(key, SdfCache.Key(model, 0.02f));
        model.Shapes[1].Blend = 0.06f;
        Assert.NotEqual(key, SdfCache.Key(model, 0.02f));
    }
}
