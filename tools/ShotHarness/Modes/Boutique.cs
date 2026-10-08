partial class Harness
{
    // ---------------------------------------------------------------- the boutique and the wardrobe (plan 11 · C10)

    // Jubilife City's boutique and the wardrobe at home (not part of `all`): the door, the shop, the wardrobe screen on
    // each slot with clothes tried on, a garment bought, the player back in the street in it, and the bedroom's wardrobe
    public void BoutiqueMode()
    {
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        void ReadOn(int most = 12)
        {
            for (int i = 0; i < most && Box().IsActive && !Box().IsQuestion; i++) { Whole(); Next(); }
        }
        void Talk() { game.Interact(); Frames(3); }
        var screen = game.WardrobeScreen;
        // The figure is made in the background the first time an outfit is tried on: the shot waits for it
        void Dressed() { Until(() => !screen.Dressing, "the outfit made", 3000); Frames(4); }
        void To(string garment)
        {
            int row = screen.Rows.ToList().FindIndex(r => r == garment);
            while (screen.SelectedIndex != row) screen.Move(1);
            Dressed();
        }

        At("Sinnoh", 140, 790, Direction.Up); Frames(2); Shot("bq01_door");
        At("JubilifeBoutique", 5, 7, Direction.Up); Frames(2); Shot("bq02_inside");

        // ---- the clerk, and the wardrobe on every slot with something tried on
        game.Money = 20000;
        At("JubilifeBoutique", 5, 4, Direction.Up);
        Talk(); Whole(); Shot("bq03_clerk");
        ReadOn();
        Until(() => State() == GameState.Wardrobe, "the wardrobe open");
        Frames(30); Dressed(); Shot("bq04_hats");
        To("blue_cap"); Shot("bq05_try_cap");
        screen.Confirm(game.Money, _ => { }); Frames(20); Shot("bq06_buy_question");
        game.Money = game.Money + screen.Confirm(game.Money, m => Console.WriteLine("  notice: " + m)); Frames(20);
        Shot("bq07_bought");
        foreach (var (tab, garment, name) in new[] { (1, "winter_coat", "bq08_tops"), (2, "khaki_shorts", "bq09_bottoms"), (3, "red_sneakers", "bq10_shoes"), (4, "green_backpack", "bq11_bags") })
        {
            while (screen.Tab != tab) screen.MoveTab(1);
            To(garment);
            Frames(40);
            Shot(name);
            // Bought and worn at once
            screen.Confirm(game.Money, _ => { }); Frames(4);
            game.Money = game.Money + screen.Confirm(game.Money, _ => { }); Frames(4);
        }
        var wardrobe = game.Wardrobe;
        Console.WriteLine($"worn: {wardrobe.Worn.Key}; owned: {string.Join(", ", wardrobe.Owned)}; money left {game.Money}");
        screen.Cancel(); Frames(4);
        ReadOn();
        Console.WriteLine($"after the boutique: {PlayerIdentity.Character}, state {State()}");

        // ---- out in the street in the new clothes, and at home
        At("Sinnoh", 140, 790, Direction.Down); Frames(30); Shot("bq12_street_outfit");
        ShotCrop("bq12b_street_outfit_close", 860, 400, 200, 200, 4);
        At("PlayerHouse2F", 3, 3, Direction.Up); Frames(2); Shot("bq13_bedroom");
        Talk(); Whole(); Next();
        Until(() => State() == GameState.Wardrobe, "the wardrobe at home");
        Frames(30); Dressed(); Shot("bq14_home_wardrobe");
        while (screen.Tab != 1) screen.MoveTab(1);
        screen.Move(-1); while (screen.SelectedIndex != 0) screen.Move(-1);
        Dressed(); Shot("bq15_home_own_top");
        screen.Cancel(); Frames(4); ReadOn();

        // ---- turntables of a few outfits on both looks, for review
        var context = game.RenderContext;
        var pose = new CharacterPose { Time = 0.4f };
        var outfits = new (string Name, Outfit Outfit)[]
        {
            ("own", Outfit.Own),
            ("cap_tee_shorts", new Outfit("red_cap", "striped_tee", "khaki_shorts", "white_sneakers", "blue_backpack")),
            ("coat_jeans", new Outfit("navy_beret", "winter_coat", "denim_jeans", "black_boots", "no_bag")),
            ("hoodie_skirt", new Outfit("no_hat", "forest_hoodie", "red_skirt", "green_runners", "pink_backpack")),
            ("jacket_trousers", new Outfit("black_cap", "black_jacket", "black_trousers", "red_sneakers", "black_backpack")),
            ("rose_coat", new Outfit("mint_beret", "rose_coat", "navy_skirt", "white_sneakers", "green_backpack"))
        };
        foreach (var look in new[] { "PLAYER", "DAWN" })
            foreach (var (name, outfit) in outfits)
            {
                string shot = $"bq20_turntable_{look.ToLowerInvariant()}_{name}";
                if (!Wanted(shot)) continue;
                var img = CharacterStudio.Turntable(context, Outfit.Dress(look, outfit), new[] { 0f, 0.65f, MathF.PI / 2f, MathF.PI }, 300, 420, pose, 1.7f, 0.7f);
                Save(img, shot);
            }
    }
}
