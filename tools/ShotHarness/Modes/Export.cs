partial class Harness
{
    // ---------------------------------------------------------------- model files

    // Writes species' models as .glb files into <out dir>/models, to refine in a 3D editor and drop into overrides/models
    // (the hand-built ones when no species are named)
    public void ExportMode()
    {
        var named = args.Skip(2).ToArray();
        var list = named.Length > 0 ? named : PokemonModels.Species;
        string folder = Path.Combine(outDir, "models");
        Directory.CreateDirectory(folder);
        foreach (var species in list)
        {
            var model = PokemonModels.Get(species);
            string file = Path.Combine(folder, species + ".glb");
            GltfWriter.Save(model, file);
            Console.WriteLine($"wrote models/{species}.glb ({new FileInfo(file).Length / 1024} KB)");
        }
    }
}
