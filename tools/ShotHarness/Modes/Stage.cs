using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models.StageShow;

partial class Harness
{
    // ---------------------------------------------------------------- the stage show (plan 42 · PS1, PS2)
    // sh04_body_<plan>: a hat and a bow (and a piece for each other slot the body offers) worn by a species of each body
    // plan, rendered by PokemonSprites.Render with the pieces handed, from the front and three turns round, under the
    // same render without them. Later sessions add sh01 to sh03 and sh05 (the trunk and the dressing room).
    public void StageMode()
    {
        var context = game.RenderContext;
        var backdrop = new Color(206, 218, 232, 255);
        var bodies = new (string Plan, string Species)[]
        {
            ("biped", "Riolu"), ("quadruped", "Shinx"), ("bird", "Starly"), ("serpent", "Onix"), ("fish", "Magikarp"), ("floating", "Bronzor"),
        };
        var byslot = new Dictionary<PropSlot, string>
        {
            [PropSlot.Head] = "top_hat_black", [PropSlot.Face] = "round_glasses", [PropSlot.Neck] = "bow_tie_red",
            [PropSlot.Back] = "short_cape_red", [PropSlot.Hand] = "star_wand", [PropSlot.Tail] = "tail_ribbon_pink",
        };
        const int Box = 360;
        var turns = new[] { 0f, 0.9f, MathF.PI / 2f, MathF.PI };
        foreach (var (plan, species) in bodies)
        {
            string name = "sh04_body_" + plan;
            if (!Wanted(name)) continue;
            var model = PokemonModels.Get(species);
            var slots = PropAnchors.SlotsOf(model);
            // A hat and a bow on every body; a bow tie where the body has a neck and a hat goes on its head, the rest where it can
            var worn = new List<ShowProp>();
            foreach (var slot in slots)
                worn.Add(ShowPropDatabase.Get(slot == PropSlot.Neck ? "bow_tie_red" : byslot[slot])!);
            if (!slots.Contains(PropSlot.Neck) && slots.Contains(PropSlot.Back)) worn.RemoveAll(p => p.Slot == PropSlot.Back);
            if (!slots.Contains(PropSlot.Neck) && slots.Contains(PropSlot.Back)) worn.Add(ShowPropDatabase.Get("back_bow")!);
            Console.WriteLine($"{name}: {species} offers {string.Join(", ", slots)}; wears {string.Join(", ", worn.Select(p => p.Id))}");
            foreach (var (slot, a) in PropAnchors.Of(model))
                Console.WriteLine($"  {slot}: bone {a.Bone} ({model.Bones[a.Bone].Role}) at {a.Position.X:0.000},{a.Position.Y:0.000},{a.Position.Z:0.000} up {a.Up.X:0.00},{a.Up.Y:0.00},{a.Up.Z:0.00} size {a.Size:0.000}{(slot == PropSlot.Neck ? $" ring {a.Ring.X:0.000}x{a.Ring.Y:0.000}" : "")}");

            var target = Raylib.LoadRenderTexture(Box, Box);
            var board = Raylib.GenImageColor(turns.Length * Box, 2 * Box, backdrop);
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < turns.Length; col++)
                {
                    PokemonSprites.Render(context, model, SpriteView.Front, new PokePose { Time = 0.4f }, target, turn: turns[col], zoom: 0.85f,
                        props: row == 0 ? worn : null);
                    var image = Raylib.LoadImageFromTexture(target.Texture);
                    Raylib.ImageFlipVertical(ref image);
                    Raylib.ImageDraw(ref board, image, new Rectangle(0, 0, Box, Box), new Rectangle(col * Box, row * Box, Box, Box), Color.White);
                    Raylib.UnloadImage(image);
                }
            Raylib.UnloadRenderTexture(target);
            Save(board, name);
            PokemonModels.Release(species);
        }
        ShowPropModels.Unload();
    }
}
