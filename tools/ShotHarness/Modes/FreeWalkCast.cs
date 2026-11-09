partial class Harness
{
    // ---------------------------------------------------------------- plan 30 · FM10: Kalos's cast in the grown kit

    // fw60_kalos_NN: every grown look of characters.json's grown block (Kalos's whole cast, from its heroes to its
    // classes) in the grown kit, twelve to a board, from the front and from three-quarter, each labelled with its name
    // and build and framed by its own height; fw61_kalos_fixes: the two sculpt fixes of FM10 at work, a skirted Teen
    // and a skirted Grown at the knee-up of a run from the side (the thigh under the skirt) and three looks at the top of
    // a throw and a cheer from the front (the armpit). It prints each look's build, height and vertices.
    void FreeWalkCast(RenderContext context, CharacterPose pose, Color backdrop)
    {
        const int W = 200, H = 440, Label = 34, PerBoard = 12;
        var names = CharacterStyles.Default.GrownNames.ToArray();

        void Labelled(ref Image board, string text, int x, int y, int w)
        {
            Raylib.ImageDrawRectangle(ref board, x, y, w, Label, new Color(52, 64, 96, 255));
            Raylib.ImageDrawText(ref board, text, x + 8, y + 8, 18, Color.White);
        }

        for (int first = 0, n = 0; first < names.Length; first += PerBoard, n++)
        {
            string name = $"fw60_kalos_{n:00}";
            if (!Wanted(name)) continue;
            var looks = names.Skip(first).Take(PerBoard).Select(l => CharacterStyle.InKit(l, CharacterKit.Grown)).ToArray();
            var board = Raylib.GenImageColor(W * looks.Length, (H + Label) * 2, backdrop);
            for (int i = 0; i < looks.Length; i++)
            {
                var style = CharacterStyle.For(looks[i]);
                var rig = CharacterModels.Get(looks[i], context.Shaders);
                Console.WriteLine($"  {looks[i]}: {style.Build}{(style.CurvedFigure() ? ", curved" : "")}, height {style.Height:0.00}, body {rig.Mesh.VertexCount} vertices" +
                    (rig.Clothes is { } c ? $", clothes {c.VertexCount}" : ""));
                for (int row = 0; row < 2; row++)
                {
                    float scale = style.Height;
                    var view = CharacterStudio.Turntable(context, looks[i], new[] { row == 0 ? 0f : 0.65f }, W, H, pose, 1.95f / scale, 0.88f / scale);
                    Raylib.ImageDraw(ref board, view, new Rectangle(0, 0, W, H), new Rectangle(i * W, row * (H + Label) + Label, W, H), Color.White);
                    Raylib.UnloadImage(view);
                    Labelled(ref board, row == 0 ? names[first + i] : style.Build.ToString(), i * W, row * (H + Label), W);
                }
            }
            Save(board, name);
            CharacterModels.Trim(Array.Empty<string>());
        }

        if (Wanted("fw61_kalos_fixes"))
        {
            (string Text, CharacterPose Pose, string Type, float Yaw)[] shots =
            {
                ("serena run", pose with { Walk = 0.25f, WalkBlend = 1f, RunBlend = 1f }, "serena~Teen", MathF.PI / 2f),
                ("serena run 3/4", pose with { Walk = 0.25f, WalkBlend = 1f, RunBlend = 1f }, "serena~Teen", 0.65f),
                ("malva run", pose with { Walk = 0.75f, WalkBlend = 1f, RunBlend = 1f }, "malva~Grown", MathF.PI / 2f),
                ("shauna walk", pose with { Walk = 0.875f, WalkBlend = 1f }, "shauna~Teen", 0.65f),
                ("calem throw", pose with { Emote = Emote.Throw, EmoteTime = CharacterAnimation.ThrowRelease }, "calem~Teen", 0.3f),
                ("sycamore cheer", pose with { Emote = Emote.Cheer, EmoteTime = 0.7f }, "sycamore~Grown", 0f),
                ("wulfric wave", pose with { Emote = Emote.Wave, EmoteTime = 0.8f }, "wulfric~GrownBroad", 0.3f)
            };
            const int SW = 260;
            var board = Raylib.GenImageColor(SW * shots.Length, H + Label, backdrop);
            for (int i = 0; i < shots.Length; i++)
            {
                float scale = CharacterStyle.For(shots[i].Type).Height;
                var view = CharacterStudio.Turntable(context, shots[i].Type, new[] { shots[i].Yaw }, SW, H, shots[i].Pose, 1.95f / scale, 0.88f / scale);
                Raylib.ImageDraw(ref board, view, new Rectangle(0, 0, SW, H), new Rectangle(i * SW, Label, SW, H), Color.White);
                Raylib.UnloadImage(view);
                Labelled(ref board, shots[i].Text, i * SW, 0, SW);
            }
            Save(board, "fw61_kalos_fixes");
        }
    }
}
