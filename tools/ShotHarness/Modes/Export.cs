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
            // a name is a file's without what a file's name may not hold (Type: Null's colon)
            string name = string.Concat(species.Where(c => !"<>:\"/\\|?*".Contains(c)));
            string file = Path.Combine(folder, name + ".glb");
            GltfWriter.Save(model, file);
            Console.WriteLine($"wrote models/{name}.glb ({new FileInfo(file).Length / 1024} KB)");
        }
    }
}
