partial class Harness
{
    // ---------------------------------------------------------------- 3D characters up close
    public void LineupMode()
    {
        game.Map = BuildLineup();
        game.Player.SetPosition(9, 9, Direction.Down);
        game.State = GameState.Overworld;
        Frames(2);
        Shot("30_lineup");
        ShotCrop("31_lineup_front", 380, 60, 940, 205, 2);
        ShotCrop("32_lineup_sides", 380, 280, 740, 195, 2);
        foreach (var (walk, label) in new[] { (0.25f, "a"), (0.75f, "b") })
        {
            SetPlayerAnim(walk, 1f);
            ShotCrop("33_player_walk_" + label, 870, 420, 180, 200, 4);
        }
        SetPlayerAnim(0.25f, 1f, running: true);
        ShotCrop("34_player_run", 870, 420, 180, 200, 4);
        foreach (var d in new[] { Direction.Left, Direction.Up, Direction.Right })
        {
            game.Player.SetPosition(9, 9, d);
            Frames(1);
            ShotCrop("35_player_" + d, 870, 420, 180, 200, 4);
        }
        game.Player.SetPosition(9, 9, Direction.Down);
        SetPlayerAnim(0f, 0f);

        // Every character in focus: a row facing the camera with the player in the middle, and a row turned away
        game.Map = BuildFocusLineup();
        game.Player.SetPosition(10, 7, Direction.Down);
        Frames(2);
        Shot("36_lineup_focus");
        ShotCrop("36b_lineup_focus_front", 300, 440, 1320, 175, 2);
        ShotCrop("36c_lineup_focus_turned", 300, 615, 1320, 185, 2);

        // Look-dev: the 3D models as battles show them (front, three-quarter, side, back), and the field's sprites
        var context = game.RenderContext;
        CharacterPose Pose(string expression = "Neutral", bool blink = false) =>
            new() { Time = 0.4f, Blink = blink, Expression = Enum.Parse<Expression>(expression) };
        string[] everyone = { "Player", "Dawn", "Rival", "Rowan", "Nurse", "Mom", "Lady", "Clerk", "Youngster", "Lass", "Clown", "Looker", "Gentleman", "StarterBriefcase", "Rift" };
        foreach (var type in everyone)
        {
            if (!Wanted("37_turntable_" + type.ToLowerInvariant())) continue;
            var img = CharacterStudio.Turntable(context, type, new[] { 0f, 0.65f, MathF.PI / 2f, MathF.PI }, 300, 420, Pose(), 1.7f, 0.7f);
            Save(img, "37_turntable_" + type.ToLowerInvariant());
        }

        // Sprite sheets: each facing a row (down, right, up, left), each frame of the strip a column, at 4x
        Image Strip(string type, string anim, int frames, bool blink = false, string expression = "Neutral")
        {
            var rows = Raylib.GenImageColor(40 * frames * 4, 58 * 4 * 4, new Color(206, 218, 232, 255));
            for (int facing = 0; facing < 4; facing++)
            {
                var row = CharacterSprites.Sheet(context, type, facing, Enum.Parse<SpriteAnim>(anim), frames, 4, blink, Enum.Parse<Expression>(expression));
                Raylib.ImageDraw(ref rows, row, new Rectangle(0, 0, row.Width, row.Height), new Rectangle(0, facing * 58 * 4, row.Width, row.Height), Color.White);
                Raylib.UnloadImage(row);
            }
            return rows;
        }
        foreach (var (type, anim, frames) in new[]
        {
            ("Player", "Walk", 8), ("Player", "Run", 8), ("Player", "Idle", 2), ("Player", "Hop", 3), ("Player", "Wave", 6), ("Player", "Surprised", 6),
            ("Player", "Cheer", 6), ("Player", "Nod", 6), ("Rival", "Walk", 8), ("Lass", "Walk", 8), ("Rowan", "Walk", 8), ("Nurse", "Idle", 2),
            // The gestures a script plays on cue (plan 11 · C11)
            ("Lass", "Bow", 6), ("Lass", "Shrug", 6), ("Lass", "Point", 6), ("Lass", "Laugh", 6)
        })
        {
            string name = $"38_sheet_{type.ToLowerInvariant()}_{anim.ToLowerInvariant()}";
            if (Wanted(name)) Save(Strip(type, anim, frames), name);
        }

        // Faces: every expression in 3D (front view) and as pixel faces on the standing sprite, eyes open and shut
        foreach (var type in new[] { "Player", "Lass", "Rowan" })
        {
            string name = "39_faces_" + type.ToLowerInvariant();
            if (!Wanted(name)) continue;
            var faces = Raylib.GenImageColor(6 * 240, 340 + 58 * 4, new Color(206, 218, 232, 255));
            int col = 0;
            foreach (var (expression, blink) in new[] { ("Neutral", false), ("Neutral", true), ("Happy", false), ("Surprised", false), ("Sad", false), ("Angry", false) })
            {
                // A close-up of the head (the adults' heads sit a little higher)
                float headY = type == "Player" || type == "Lass" ? 0.86f : 0.97f;
                var view = CharacterStudio.Turntable(context, type, new[] { 0f }, 240, 340, Pose(expression, blink), 0.75f, headY);
                Raylib.ImageDraw(ref faces, view, new Rectangle(0, 0, 240, 340), new Rectangle(col * 240, 0, 240, 340), Color.White);
                Raylib.UnloadImage(view);
                var sprite = CharacterSprites.Sheet(context, type, 0, SpriteAnim.Idle, 1, 4, blink, Enum.Parse<Expression>(expression));
                Raylib.ImageDraw(ref faces, sprite, new Rectangle(0, 0, sprite.Width, sprite.Height), new Rectangle(col * 240 + 40, 340, sprite.Width, sprite.Height), Color.White);
                Raylib.UnloadImage(sprite);
                col++;
            }
            Save(faces, name);
        }

        // Last, so nothing before moves: people on their furniture (plan 11 · C7), in a room built here, and the
        // stances' sprite strips
        foreach (var (type, anim) in new[] { ("Lass", "Sit"), ("Gentleman", "Lie"), ("Lass", "SitGround") })
        {
            string name = $"38_sheet_{type.ToLowerInvariant()}_{anim.ToLowerInvariant()}";
            if (Wanted(name)) Save(Strip(type, anim, CharacterSprites.IdleFrames), name);
        }
        var poses = BuildPosesRoom();
        game.Map = poses;
        game.Player.SetPosition(6, 7, Direction.Up);
        Frames(3);
        foreach (var npc in poses.NPCs) Console.WriteLine($"  {npc.Name} on {poses.SeatUnder(npc)?.Type.ToString() ?? "the floor"}: {poses.StanceOf(npc)}, facing {npc.Facing}");
        Shot("36d_lineup_poses");
        ShotCrop("36e_lineup_poses_close", 560, 260, 800, 450, 2);
    }

    // A room of a house with a seat of every kind and a bed, someone on each, and one posed on the floor (plan 11 · C7)
    static Map BuildPosesRoom()
    {
        var m = new Map(13, 10) { Name = "Poses", DisplayName = "Poses", Interior = InteriorStyle.House };
        for (int x = 0; x < 13; x++)
            for (int y = 0; y < 10; y++)
            {
                bool wall = y < 3 || x == 0 || x == 12 || y == 9;
                m.SetGroundTile(x, y, wall ? TileType.Wall : TileType.Floor, wall);
            }
        void Put(PropType type, int x, int y, int w = 1, int d = 1) => m.Props.Add(new Prop { Type = type, X = x, Y = y, Width = w, Depth = d });
        Put(PropType.Bench, 1, 3, 2);
        Put(PropType.Table, 5, 4);
        Put(PropType.Chair, 4, 4);
        Put(PropType.Sofa, 7, 3, 2);
        Put(PropType.Cushion, 3, 7);
        Put(PropType.Bed, 10, 3, 1, 2);
        void Seat(string type, int x, int y, Direction facing, PokemonPlatinumEngine.Overworld.Stance? posed = null) =>
            m.NPCs.Add(new NPC { Name = $"{type}_{x}_{y}", NpcType = type, GridX = x, GridY = y, Facing = facing, Posed = posed });
        Seat("Lass", 1, 3, Direction.Down);
        Seat("Gentleman", 2, 3, Direction.Down);
        Seat("Youngster", 4, 4, Direction.Right);
        Seat("Mom", 7, 3, Direction.Down);
        Seat("Lady", 8, 3, Direction.Down);
        Seat("Rival", 3, 7, Direction.Down);
        Seat("Rowan", 10, 3, Direction.Down);
        Seat("Clerk", 9, 7, Direction.Left, PokemonPlatinumEngine.Overworld.Stance.SitGround);
        Seat("Nurse", 5, 7, Direction.Down);
        return m;
    }
}
