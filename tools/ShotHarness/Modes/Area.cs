partial class Harness
{
    // ---------------------------------------------------------------- the imported world (plan 01 · M2)
    public void AreaMode()
    {
        string key = args.Length > 2 ? args[2] : "twinleaf_town";
        var (areaMap, ax, ay) = AreaSpot(key);
        At(areaMap.Name, ax, ay, Direction.Down); Frames(2); Shot("area_" + key);
        Console.WriteLine($"{key}: {areaMap.Name} ({ax}, {ay})");
    }
}
