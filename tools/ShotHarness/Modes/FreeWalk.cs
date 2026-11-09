partial class Harness
{
    // ---------------------------------------------------------------- plan 30: free movement and the grown kit

    /// <summary>The grown kit's five builds (plan 30 · FM4), each asked from a chibi look of the table it stands for.</summary>
    static readonly (string Look, BodyBuild Build)[] GrownBuilds =
    {
        ("PLAYER", BodyBuild.Teen), ("CLERK", BodyBuild.Grown), ("BLACKBELT", BodyBuild.GrownBroad), ("YOUNGSTER", BodyBuild.Child), ("OLD_MAN", BodyBuild.GrownElder)
    };

    // freewalk: plan 30's shots of free movement and the grown kit (not part of "all"). FM4, part a: each of the grown
    // kit's five builds turning (front, three-quarter, side, back) under its chibi self, at one scale, so a grown body
    // stands as much taller as it is (fw40_turntable_<build>); the five side by side from the front and in profile at one
    // scale, with a curved Teen and Grown beside them (fw40_builds); their heads close up from the front, three-quarter
    // and side (fw40_heads); and the chibi kit's clips on a grown Teen: standing, two frames of the walk, the run, a bow,
    // sitting (fw40_poses). It prints each rig's bones, vertices and triangles and its height. Later sessions of plan 30
    // add their parts (FM5's fw41 to fw44, FM6's fw45 to fw47, FM3's fw50).
    public void FreeWalkMode()
    {
        var context = game.RenderContext;
        var pose = new CharacterPose { Time = 0.4f, Expression = Expression.Neutral };
        float[] yaws = { 0f, 0.65f, MathF.PI / 2f, MathF.PI };
        var backdrop = new Color(206, 218, 232, 255);
        const int ViewW = 240, ViewH = 440, Label = 34;

        void Print(string type)
        {
            var rig = CharacterModels.Get(type, context.Shaders);
            float top = rig.Mesh.Positions.Max(p => p.Y), wide = rig.Mesh.Positions.Max(p => p.X) - rig.Mesh.Positions.Min(p => p.X);
            Console.WriteLine($"  {type}: {rig.Style.Kit} {rig.Style.Build}, {rig.Skeleton.Count} bones, {rig.Mesh.VertexCount} vertices, {rig.Mesh.TriangleCount} triangles " +
                $"(shell {rig.Shell.TriangleCount}), {top:0.000} tall and {wide:0.000} wide in its A-pose before its scale {rig.Scale:0.00}");
        }

        void Labelled(ref Image board, string text, int x, int y, int w)
        {
            Raylib.ImageDrawRectangle(ref board, x, y, w, Label, new Color(52, 64, 96, 255));
            Raylib.ImageDrawText(ref board, text, x + 12, y + 7, 20, Color.White);
        }

        foreach (var (look, build) in GrownBuilds)
        {
            string name = $"fw40_turntable_{build.ToString().ToLowerInvariant()}";
            if (!Wanted(name)) continue;
            string grown = $"{look}~{build}";
            Print(look);
            Print(grown);
            var board = Raylib.GenImageColor(ViewW * yaws.Length, (ViewH + Label) * 2, backdrop);
            foreach (var (type, row, text) in new[] { (look, 0, $"{look.ToLowerInvariant()} (chibi)"), (grown, 1, $"{look.ToLowerInvariant()} as {build}") })
            {
                var views = CharacterStudio.Turntable(context, type, yaws, ViewW, ViewH, pose, 1.95f, 0.88f);
                Raylib.ImageDraw(ref board, views, new Rectangle(0, 0, views.Width, views.Height), new Rectangle(0, row * (ViewH + Label) + Label, views.Width, ViewH), Color.White);
                Raylib.UnloadImage(views);
                Labelled(ref board, text, 0, row * (ViewH + Label), ViewW * yaws.Length);
            }
            Save(board, name);
        }

        // The five side by side, and a curved Teen and Grown (the girl and the nurse), from the front and in profile at
        // one scale: each framed by its own look's height so a look that stands taller doesn't make the others smaller
        string[] lineup = GrownBuilds.Select(b => $"{b.Look}~{b.Build}").Concat(new[] { "DAWN~Teen", "NURSE~Grown" }).ToArray();
        if (Wanted("fw40_builds"))
        {
            foreach (var type in lineup.Skip(GrownBuilds.Length)) Print(type);
            const int W = 200, H = 460;
            var board = Raylib.GenImageColor(W * lineup.Length, (H + Label) * 2, backdrop);
            for (int i = 0; i < lineup.Length; i++)
                for (int row = 0; row < 2; row++)
                {
                    float scale = CharacterStyle.For(lineup[i]).Height;
                    var view = CharacterStudio.Turntable(context, lineup[i], new[] { row == 0 ? 0f : MathF.PI / 2f }, W, H, pose, 1.95f / scale, 0.88f / scale);
                    Raylib.ImageDraw(ref board, view, new Rectangle(0, 0, W, H), new Rectangle(i * W, row * (H + Label) + Label, W, H), Color.White);
                    Raylib.UnloadImage(view);
                    Labelled(ref board, CharacterStyle.For(lineup[i]).Build.ToString(), i * W, row * (H + Label), W);
                }
            Save(board, "fw40_builds");
        }

        // The heads close up: the nose, the jaw, the ears and the hair's cap (the painted face is part b's)
        if (Wanted("fw40_heads"))
        {
            const int W = 220, H = 260;
            float[] round = { 0f, 0.65f, MathF.PI / 2f };
            var board = Raylib.GenImageColor(W * round.Length * 2, (H + Label) * 4, backdrop);
            for (int i = 0; i < lineup.Length; i++)
            {
                var style = CharacterStyle.For(lineup[i]);
                var frame = CharacterModels.GrownFrameOf(style.Build, style.CurvedFigure());
                var views = CharacterStudio.Turntable(context, lineup[i], round, W, H, pose, 0.34f, frame.HeadC.Y - 0.03f);
                int x = i % 2 * W * round.Length, y = i / 2 * (H + Label);
                Raylib.ImageDraw(ref board, views, new Rectangle(0, 0, views.Width, views.Height), new Rectangle(x, y + Label, views.Width, H), Color.White);
                Raylib.UnloadImage(views);
                Labelled(ref board, style.Build.ToString(), x, y, W * round.Length);
            }
            Save(board, "fw40_heads");
        }

        // One grown face large, from the front, three-quarter and side, where the sculpt's forms and the light on them are judged
        if (Wanted("fw40_face"))
        {
            var frame = CharacterModels.GrownFrameOf(BodyBuild.Grown);
            var views = CharacterStudio.Turntable(context, "CLERK~Grown", new[] { 0f, 0.65f, MathF.PI / 2f }, 520, 600, pose, 0.3f, frame.HeadC.Y - 0.02f);
            Save(views, "fw40_face");
        }

        // The hips close up in a stride and a run, from the front, the side and behind: where the thighs turn under the pelvis
        if (Wanted("fw40_hips"))
        {
            var frame = CharacterModels.GrownFrameOf(BodyBuild.Teen);
            float[] round = { 0.3f, MathF.PI / 2f, MathF.PI };
            var board = Raylib.GenImageColor(300 * round.Length, (340 + Label) * 2, backdrop);
            foreach (var (row, text, stride) in new[] { (0, "walk a", pose with { Walk = 0.25f, WalkBlend = 1f }), (1, "run", pose with { Walk = 0.25f, WalkBlend = 1f, Running = true }) })
            {
                var views = CharacterStudio.Turntable(context, "PLAYER~Teen", round, 300, 340, stride, 0.62f, (frame.HipY + frame.Knee.Y) / 2f);
                Raylib.ImageDraw(ref board, views, new Rectangle(0, 0, views.Width, views.Height), new Rectangle(0, row * (340 + Label) + Label, views.Width, 340), Color.White);
                Raylib.UnloadImage(views);
                Labelled(ref board, text, 0, row * (340 + Label), 300 * round.Length);
            }
            Save(board, "fw40_hips");
        }

        // FM4, part b: every cut, every hat, every garment and everything held at grown scale, and the grown face
        FreeWalkPartB(context, pose, backdrop);

        // FM5's shots of the grown clips (fw41 to fw44)
        FreeWalkMotion(context, pose, backdrop);

        // A grown body standing, walking, running, bowing and sitting: the chibi kit's clips in FM4, the grown kit's own
        // since FM5 (a grown rig's Animate plays them)
        if (Wanted("fw40_poses"))
        {
            (string Text, CharacterPose Pose, string Type)[] poses =
            {
                ("idle", pose, "PLAYER~Teen"),
                ("walk a", pose with { Walk = 0.25f, WalkBlend = 1f }, "PLAYER~Teen"),
                ("walk b", pose with { Walk = 0.75f, WalkBlend = 1f }, "PLAYER~Teen"),
                ("run", pose with { Walk = 0.25f, WalkBlend = 1f, Running = true }, "PLAYER~Teen"),
                ("bow", pose with { Emote = Emote.Bow, EmoteTime = 0.6f }, "PLAYER~Teen"),
                ("sit", pose with { Stance = PokemonPlatinumEngine.Overworld.Stance.Sit }, "PLAYER~Teen"),
                ("sit (broad)", pose with { Stance = PokemonPlatinumEngine.Overworld.Stance.Sit }, "BLACKBELT~GrownBroad")
            };
            const int W = 240, H = 440;
            var board = Raylib.GenImageColor(W * poses.Length, (H + Label) * 2, backdrop);
            for (int i = 0; i < poses.Length; i++)
                for (int row = 0; row < 2; row++)
                {
                    float scale = CharacterStyle.For(poses[i].Type).Height;
                    var view = CharacterStudio.Turntable(context, poses[i].Type, new[] { row == 0 ? 0.5f : MathF.PI / 2f }, W, H, poses[i].Pose, 1.95f / scale, 0.88f / scale);
                    Raylib.ImageDraw(ref board, view, new Rectangle(0, 0, W, H), new Rectangle(i * W, row * (H + Label) + Label, W, H), Color.White);
                    Raylib.UnloadImage(view);
                    Labelled(ref board, poses[i].Text, i * W, row * (H + Label), W);
                }
            Save(board, "fw40_poses");
        }

        // FM3: the crowd of thirty drawn as a grown region's people (last: it begins a game of its own in Kalos)
        FreeWalkCrowd();
    }
}
