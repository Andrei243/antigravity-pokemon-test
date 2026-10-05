using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumTests;

/// <summary>
/// The separate pieces of a meshed model, for the checks that a model is in one piece: a part that doesn't reach
/// the body floats beside it (the hand-built models in <see cref="PokemonModelTests"/>, the generated ones in
/// <see cref="PokemonGeneratorTests"/>).
/// </summary>
internal static class MeshPieces
{
    /// <summary>
    /// The most vertices a piece has that is only a speck: a few cells where a thin claw or horn tip breaks up in the
    /// mesh, left alone by the checks.
    /// </summary>
    public const int Speck = 40;

    /// <summary>The number of vertices in each of the mesh's separate pieces, largest first.</summary>
    public static List<int> Pieces(SdfMesh mesh)
    {
        var parent = Enumerable.Range(0, mesh.VertexCount).ToArray();
        int Find(int v)
        {
            while (parent[v] != v) v = parent[v] = parent[parent[v]];
            return v;
        }
        for (int t = 0; t < mesh.Indices.Length; t += 3)
        {
            int a = Find(mesh.Indices[t]);
            for (int k = 1; k < 3; k++)
            {
                int r = Find(mesh.Indices[t + k]);
                if (r != a) parent[r] = a;
            }
        }
        return Enumerable.Range(0, mesh.VertexCount).GroupBy(Find).Select(p => p.Count()).OrderByDescending(n => n).ToList();
    }
}
