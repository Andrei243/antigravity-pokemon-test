partial class Harness
{
    // ---------------------------------------------------------------- the workshop's garden (plan 39 · CR8)

    // The garden behind the workshop's back door, with a few of its soils grown by hand (cr70), and its keeper's CROSSES
    // (the second of her answers), the kit opened on its table of crosses (cr71). Last in the mode, so nothing before it
    // moves. It prints where the back door put the player and what the soils show.
    void CraftingGardenPart(Action<Func<bool>, string, int> until)
    {
        game.State = GameState.Overworld;
        var patches = game.Berries;
        void Grow(int patch, BerryStage stage, string berry)
        {
            var p = patches[patch];
            p.Berry = berry;
            p.Stage = stage;
            p.Yield = stage == BerryStage.Fruit ? 4 : 0;
            patches.Water(patch); // moves the patches' revision on, so the soil looks again
        }
        int first = BerryPatches.GardenFirst;
        Grow(first + 3, BerryStage.Fruit, "Cheri Berry");
        Grow(first + 4, BerryStage.Fruit, "Figy Berry");
        Grow(first + 5, BerryStage.Blooming, "Oran Berry");
        Grow(first + 6, BerryStage.Growing, "Pecha Berry");
        Grow(first + 15, BerryStage.Sprouted, "Rawst Berry");
        Grow(first + 16, BerryStage.Planted, "Chesto Berry");

        // In by the workshop's back door, where its warp puts the player
        At("SunyshoreWorkshop", 8, 2, Direction.Up);
        game.LocationSign.Hide();
        Frames(4);
        var back = game.Map.Warps.First(w => w.TargetMap == "SunyshoreGarden");
        At(back.TargetMap, back.TargetX, back.TargetY, back.TargetFacing);
        game.LocationSign.Hide();
        Frames(30);
        Shot("cr70_garden");
        Console.WriteLine($"garden: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}; soils {game.Map.NPCs.Count(n => n.IsBerrySoil)}");

        // The keeper's CROSSES: the kit on its table of crosses
        At("SunyshoreGarden", 7, 10, Direction.Left);
        game.LocationSign.Hide();
        Frames(10);
        game.Interact(); Frames(3);
        CraftingTalk(until, new[] { 1 });
        until(() => game.State == GameState.Crafting, "the kit on its crosses", 300);
        Frames(30);
        Shot("cr71_keeper");
        Console.WriteLine($"keeper: kit on {game.CraftingScreen.Tab}");
        game.CraftingScreen.Cancel();
        Frames(10);
    }
}
