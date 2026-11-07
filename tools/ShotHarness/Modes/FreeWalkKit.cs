partial class Harness
{
    // ---------------------------------------------------------------- plan 30 · FM4, part b: the grown kit dressed

    /// <summary>Every hair cut of the kit, each on a look of the table that has it, asked in a grown build.</summary>
    static readonly (string Type, string Text)[] GrownCuts =
    {
        ("CLERK~Grown", "short"), ("RIVAL~Teen", "spiky"), ("LASS~Teen", "long"), ("ROWAN~Grown", "swept"), ("FLINT~Grown", "afro"), ("BERTHA~GrownElder", "bun"),
        ("CANDICE~Grown", "braids"), ("JASMINE~Grown", "bunches"), ("GRUNT~Grown", "bowl"), ("ROXIE~Teen", "ponytail"), ("BURGH~Grown", "curls"), ("MARSHAL~GrownBroad", "bald")
    };

    /// <summary>Every hat of the kit, each on a look of the table that wears it, asked in a grown build.</summary>
    static readonly (string Type, string Text)[] GrownHats =
    {
        ("PLAYER~Teen", "beret"), ("YOUNGSTER~Child", "cap"), ("NURSE~Grown", "nurse cap"), ("MIRA~Child", "bow"), ("CRASHER_WAKE~GrownBroad", "mask"),
        ("RILEY~Grown", "fedora"), ("CYCLIST_M~Teen", "helmet"), ("WORKER~Grown", "hard hat"), ("CAMPER~Teen", "beanie"), ("NINJA_BOY~Teen", "bandana"),
        ("BLACKBELT~GrownBroad", "headband"), ("RANCHER~Grown", "cowboy"), ("JOGGER~Grown", "visor"), ("SOCIALITE~Grown", "wide hat")
    };

    /// <summary>Every garment of the clothes' layer and what lies on it, each on a look of the table that wears it.</summary>
    static readonly (string Type, string Text)[] GrownGarments =
    {
        ("PLAYER~Teen", "jacket, scarf, bag"), ("DAWN~Teen", "buttoned coat"), ("ROWAN~Grown", "open coat"), ("EXPERT_M~GrownElder", "robe"), ("LASS~Teen", "skirt"),
        ("JANITOR~Grown", "overalls"), ("CAFE_BROTHER_A~Grown", "apron"), ("WAITER~Grown", "vest"), ("CLERK~Grown", "stripes"), ("GRUNT~Grown", "the mark"),
        ("SWIMMER_F~Grown", "swimsuit"), ("CRASHER_WAKE~GrownBroad", "bare chest"), ("BYRON~GrownBroad", "beard"), ("LUCIAN~Grown", "glasses")
    };

    /// <summary>Everything a grown person may hold, each on the look of the table that holds it.</summary>
    static readonly (string Type, string Text)[] GrownHeld =
    {
        ("FISHERMAN~Grown", "rod"), ("PARASOL_LADY~Grown", "parasol"), ("GUITARIST~Grown", "guitar"), ("BUG_CATCHER~Child", "net"),
        ("RUIN_MANIAC~Grown", "pickaxe"), ("TUBER_M~Child", "float ring"), ("CAMERAMAN~Grown", "camera")
    };

    /// <summary>
    /// FM4, part b's boards (not part of "all"): every cut on a grown head turning (fw40_cuts), every hat (fw40_hats),
    /// every garment front and back (fw40_garments), everything held (fw40_held), the grown faces in their five
    /// expressions, shut and speaking, with and without glasses (fw40_faces), and one dressed look of each build large
    /// (fw40_dressed). It prints each rig's body, clothes and face patch.
    /// </summary>
    void FreeWalkPartB(RenderContext context, CharacterPose pose, Color backdrop)
    {
        const int Label = 34;
        void Labelled(ref Image board, string text, int x, int y, int w)
        {
            Raylib.ImageDrawRectangle(ref board, x, y, w, Label, new Color(52, 64, 96, 255));
            Raylib.ImageDrawText(ref board, text, x + 10, y + 7, 18, Color.White);
        }
        void Print(string type)
        {
            var rig = CharacterModels.Get(type, context.Shaders);
            float top = rig.Mesh.Positions.Max(p => p.Y), wide = rig.Mesh.Positions.Max(p => p.X) - rig.Mesh.Positions.Min(p => p.X);
            Console.WriteLine($"  {type}: {rig.Style.Build}, body {rig.Mesh.VertexCount} vertices, clothes {rig.Clothes?.VertexCount ?? 0}, " +
                $"face patch {rig.FacePatch?.TriangleCount ?? 0} triangles; {top:0.000} tall, {wide:0.000} wide");
        }
        float HeadY(string type)
        {
            var style = CharacterStyle.For(type);
            return CharacterModels.GrownFrameOf(style.Build, style.CurvedFigure()).HeadC.Y;
        }

        // Heads close up, turning: four views each, two looks to a row
        void Heads((string Type, string Text)[] looks, string name)
        {
            if (!Wanted(name)) return;
            const int W = 200, H = 230;
            float[] round = { 0f, 0.7f, MathF.PI / 2f, MathF.PI };
            int perRow = 2, rows = (looks.Length + perRow - 1) / perRow;
            var board = Raylib.GenImageColor(W * round.Length * perRow, (H + Label) * rows, backdrop);
            for (int i = 0; i < looks.Length; i++)
            {
                Print(looks[i].Type);
                float scale = CharacterStyle.For(looks[i].Type).Height;
                var views = CharacterStudio.Turntable(context, looks[i].Type, round, W, H, pose, 0.46f / scale, HeadY(looks[i].Type) + 0.02f);
                int x = i % perRow * W * round.Length, y = i / perRow * (H + Label);
                Raylib.ImageDraw(ref board, views, new Rectangle(0, 0, views.Width, views.Height), new Rectangle(x, y + Label, views.Width, H), Color.White);
                Raylib.UnloadImage(views);
                Labelled(ref board, $"{looks[i].Text}: {looks[i].Type.ToLowerInvariant()}", x, y, W * round.Length);
            }
            Save(board, name);
        }
        Heads(GrownCuts, "fw40_cuts");
        Heads(GrownHats, "fw40_hats");

        // Whole figures: from the front, three-quarter and behind
        void Figures((string Type, string Text)[] looks, string name, float[] round)
        {
            if (!Wanted(name)) return;
            const int W = 170, H = 400;
            int perRow = looks.Length > 8 ? (looks.Length + 1) / 2 : looks.Length, rows = (looks.Length + perRow - 1) / perRow;
            var board = Raylib.GenImageColor(W * round.Length * perRow, (H + Label) * rows, backdrop);
            for (int i = 0; i < looks.Length; i++)
            {
                Print(looks[i].Type);
                float scale = CharacterStyle.For(looks[i].Type).Height;
                var views = CharacterStudio.Turntable(context, looks[i].Type, round, W, H, pose, 2.0f / scale, 0.9f / scale);
                int x = i % perRow * W * round.Length, y = i / perRow * (H + Label);
                Raylib.ImageDraw(ref board, views, new Rectangle(0, 0, views.Width, views.Height), new Rectangle(x, y + Label, views.Width, H), Color.White);
                Raylib.UnloadImage(views);
                Labelled(ref board, looks[i].Text, x, y, W * round.Length);
            }
            Save(board, name);
        }
        Figures(GrownGarments, "fw40_garments", new[] { 0.35f, MathF.PI });
        Figures(GrownHeld, "fw40_held", new[] { 0.5f, -0.9f });

        // The grown face: its five expressions with the eyes open, then shut, then speaking; a woman's and a man's with glasses
        if (Wanted("fw40_faces"))
        {
            const int W = 220, H = 240;
            var faces = new (string Type, string Text)[] { ("DAWN~Teen", "dawn"), ("CLERK~Grown", "clerk"), ("LUCIAN~Grown", "lucian, glasses"), ("ROWAN~Grown", "rowan, brows and moustache") };
            var expressions = Enum.GetValues<Expression>();
            var board = Raylib.GenImageColor(W * expressions.Length, (H + Label) * faces.Length * 2, backdrop);
            for (int i = 0; i < faces.Length; i++)
            {
                Print(faces[i].Type);
                float scale = CharacterStyle.For(faces[i].Type).Height;
                for (int row = 0; row < 2; row++)
                    for (int e = 0; e < expressions.Length; e++)
                    {
                        var face = pose with { Expression = expressions[e], Blink = row == 1 && e % 2 == 1, MouthOpen = row == 1 && e % 2 == 0 };
                        var view = CharacterStudio.Turntable(context, faces[i].Type, new[] { 0f }, W, H, face, 0.3f / scale, HeadY(faces[i].Type) - 0.01f);
                        int y = (i * 2 + row) * (H + Label);
                        Raylib.ImageDraw(ref board, view, new Rectangle(0, 0, W, H), new Rectangle(e * W, y + Label, W, H), Color.White);
                        Raylib.UnloadImage(view);
                        Labelled(ref board, $"{faces[i].Text} {expressions[e].ToString().ToLowerInvariant()}{(row == 1 ? (e % 2 == 1 ? " shut" : " speaking") : "")}", e * W, y, W);
                    }
            }
            Save(board, "fw40_faces");
        }

        // One dressed look of each build, large, from the front, three-quarter, the side and behind
        if (Wanted("fw40_dressed"))
        {
            var looks = new[] { "PLAYER~Teen", "DAWN~Teen", "ROWAN~Grown", "CYNTHIA~Grown", "BYRON~GrownBroad", "YOUNGSTER~Child", "OLD_MAN~GrownElder" };
            float[] round = { 0f, 0.65f, MathF.PI / 2f, MathF.PI };
            const int W = 260, H = 520;
            var board = Raylib.GenImageColor(W * round.Length * 2, (H + Label) * ((looks.Length + 1) / 2), backdrop);
            for (int i = 0; i < looks.Length; i++)
            {
                Print(looks[i]);
                float scale = CharacterStyle.For(looks[i]).Height;
                var views = CharacterStudio.Turntable(context, looks[i], round, W, H, pose, 1.85f / scale, 0.86f / scale);
                int x = i % 2 * W * round.Length, y = i / 2 * (H + Label);
                Raylib.ImageDraw(ref board, views, new Rectangle(0, 0, views.Width, views.Height), new Rectangle(x, y + Label, views.Width, H), Color.White);
                Raylib.UnloadImage(views);
                Labelled(ref board, looks[i].ToLowerInvariant(), x, y, W * round.Length);
            }
            Save(board, "fw40_dressed");
        }
    }
}
