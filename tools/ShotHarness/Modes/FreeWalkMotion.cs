partial class Harness
{
    // freewalk, FM5's part (plan 30 · FM5; style guide, "Grown animation"): the grown clips. fw41_blend is the walk to
    // the run at five speeds (a share of walking pace each), four moments of the cycle from the side, driven by the
    // game's own GrownMotion so the stride and the blends are what the field will give; fw41_follow a coat's tails and a
    // skirt walking and running; fw42_turn a step-turn from standing at six moments with the body's yaw as the motion
    // has it, then a start, a stop and the lean into a turn; fw43_stances the grown stances and rides and the hop;
    // fw44_emotes every emote at its middle and talking, from the front and the side. It prints each speed's stride,
    // blends and cadence.
    void FreeWalkMotion(RenderContext context, CharacterPose rest, Color backdrop)
    {
        const int W = 220, H = 400, Label = 34;

        void Labelled(ref Image board, string text, int x, int y, int w)
        {
            Raylib.ImageDrawRectangle(ref board, x, y, w, Label, new Color(52, 64, 96, 255));
            Raylib.ImageDrawText(ref board, text, x + 10, y + 7, 18, Color.White);
        }

        void Cell(ref Image board, string type, float yaw, CharacterPose pose, int col, int row, string text)
        {
            float scale = CharacterStyle.For(type).Height;
            var view = CharacterStudio.Turntable(context, type, new[] { yaw }, W, H, pose, 1.95f / scale, 0.88f / scale);
            Raylib.ImageDraw(ref board, view, new Rectangle(0, 0, W, H), new Rectangle(col * W, row * (H + Label) + Label, W, H), Color.White);
            Raylib.UnloadImage(view);
            Labelled(ref board, text, col * W, row * (H + Label), W);
        }

        // The motion settled at a speed, as the field will drive it
        GrownMotion Settled(string type, float share)
        {
            var motion = GrownMotion.For(CharacterModels.Get(type, context.Shaders));
            for (int i = 0; i < 120; i++) motion.Step(1f / 60f, new Vector2(0f, share * motion.WalkPace));
            return motion;
        }

        float side = MathF.PI / 2f;
        if (Wanted("fw41_blend"))
        {
            float[] speeds = { 0.4f, 0.8f, 1.2f, 1.6f, 2.0f };
            float[] phases = { 0f, 0.25f, 0.5f, 0.75f };
            var board = Raylib.GenImageColor(W * phases.Length, (H + Label) * speeds.Length, backdrop);
            for (int r = 0; r < speeds.Length; r++)
            {
                var motion = Settled("PLAYER~Teen", speeds[r]);
                Console.WriteLine($"  {speeds[r]:0.0} of walking pace ({speeds[r] * motion.WalkPace:0.00} a second): stride {motion.Stride:0.000}, " +
                    $"walk {motion.WalkBlend:0.00}, run {motion.RunBlend:0.00}, {speeds[r] * motion.WalkPace / motion.Stride:0.00} cycles a second");
                for (int c = 0; c < phases.Length; c++)
                {
                    var pose = rest;
                    motion.Fill(ref pose);
                    pose.Walk = phases[c];
                    Cell(ref board, "PLAYER~Teen", side, pose, c, r, $"{speeds[r]:0.0}x pace, {phases[c]:0.00}");
                }
            }
            Save(board, "fw41_blend");
        }

        if (Wanted("fw41_follow"))
        {
            (string Type, float Share, float Phase, float Yaw, string Text)[] cells =
            {
                ("ROWAN~Grown", 1f, 0.2f, side, "coat walk"), ("ROWAN~Grown", 1f, 0.2f, MathF.PI, "coat walk, back"), ("ROWAN~Grown", 2f, 0.2f, side, "coat run"),
                ("LASS~Teen", 1f, 0.2f, side, "skirt walk"), ("LASS~Teen", 2f, 0.7f, 0.5f, "skirt run")
            };
            var board = Raylib.GenImageColor(W * cells.Length, H + Label, backdrop);
            for (int i = 0; i < cells.Length; i++)
            {
                var pose = rest;
                Settled(cells[i].Type, cells[i].Share).Fill(ref pose);
                pose.Walk = cells[i].Phase;
                Cell(ref board, cells[i].Type, cells[i].Yaw, pose, i, 0, cells[i].Text);
            }
            Save(board, "fw41_follow");
        }

        if (Wanted("fw42_turn"))
        {
            // A step-turn from standing of 150° to the left, at six moments, the body's yaw the motion's own
            var board = Raylib.GenImageColor(W * 6, (H + Label) * 2, backdrop);
            var motion = new GrownMotion(GrownAnimation.LegsOf(CharacterModels.Get("PLAYER~Teen", context.Shaders).Skeleton).Length);
            float target = 150f * MathF.PI / 180f, view = 0.35f;
            motion.Step(1f / 60f, Vector2.Zero, target);
            int col = 0;
            for (int frame = 0; frame <= 15 && col < 6; frame++)
            {
                if (frame % 3 == 0)
                {
                    var pose = rest;
                    motion.Fill(ref pose);
                    Cell(ref board, "PLAYER~Teen", view + motion.Yaw, pose, col++, 0, $"turn {pose.StepTurn:0.00} yaw {motion.Yaw:0.00}");
                    Console.WriteLine($"  step-turn frame {frame}: {pose.StepTurn:0.000} of the way, yaw {motion.Yaw:0.000}");
                }
                motion.Step(1f / 60f, Vector2.Zero, target);
            }
            var walking = Settled("PLAYER~Teen", 1f);
            var running = Settled("PLAYER~Teen", 2f);
            (CharacterPose Pose, float Yaw, string Text)[] moves =
            {
                (rest with { Start = 0.5f }, side, "start"), (rest with { Stop = 0.5f }, side, "stop"),
                (Fill(walking, 0.3f) with { Lean = GrownMotion.MaxLean }, 0f, "walk, lean left"), (Fill(walking, 0.3f) with { Lean = -GrownMotion.MaxLean }, 0f, "walk, lean right"),
                (Fill(running, 0.3f) with { Lean = GrownMotion.MaxLean }, 0f, "run, lean left"), (Fill(running, 0.3f) with { Lean = GrownMotion.MaxLean }, MathF.PI, "run, lean, back")
            };
            for (int i = 0; i < moves.Length; i++) Cell(ref board, "PLAYER~Teen", moves[i].Yaw, moves[i].Pose, i, 1, moves[i].Text);
            Save(board, "fw42_turn");

            CharacterPose Fill(GrownMotion m, float phase)
            {
                var pose = rest;
                m.Fill(ref pose);
                pose.Walk = phase;
                return pose;
            }
        }

        if (Wanted("fw43_stances"))
        {
            (CharacterPose Pose, string Type, string Text)[] stances =
            {
                (rest with { Stance = PokemonPlatinumEngine.Overworld.Stance.Sit }, "PLAYER~Teen", "sit"), (rest with { Stance = PokemonPlatinumEngine.Overworld.Stance.Sit }, "ROWAN~Grown", "sit (coat)"),
                (rest with { Stance = PokemonPlatinumEngine.Overworld.Stance.SitGround }, "LASS~Teen", "on the ground"), (rest with { Stance = PokemonPlatinumEngine.Overworld.Stance.Swim }, "PLAYER~Teen", "swim"),
                (rest with { Stance = PokemonPlatinumEngine.Overworld.Stance.Tug }, "PLAYER~Teen", "tug"), (rest with { Riding = Riding.Surf }, "PLAYER~Teen", "surf"),
                (rest with { Riding = Riding.Bicycle, Walk = 0.15f, WalkBlend = 1f }, "PLAYER~Teen", "bicycle"),
                (rest with { Riding = Riding.Mount, RunBlend = 1f }, "PLAYER~Teen", "mount, sprint"), (rest with { Hop = 0.5f }, "PLAYER~Teen", "hop")
            };
            var board = Raylib.GenImageColor(W * stances.Length, (H + Label) * 2, backdrop);
            for (int i = 0; i < stances.Length; i++)
            {
                Cell(ref board, stances[i].Type, side, stances[i].Pose, i, 0, stances[i].Text);
                Cell(ref board, stances[i].Type, 0.6f, stances[i].Pose, i, 1, stances[i].Text);
            }
            Save(board, "fw43_stances");
        }

        if (Wanted("fw44_emotes"))
        {
            var emotes = new List<(CharacterPose Pose, string Text)>();
            foreach (var emote in new[] { Emote.Wave, Emote.Surprised, Emote.Nod, Emote.Cheer, Emote.Throw, Emote.Bow, Emote.Shrug, Emote.Point, Emote.Laugh })
            {
                float at = emote == Emote.Throw ? CharacterAnimation.ThrowRelease : emote == Emote.Surprised ? 0.3f : 0.5f * CharacterAnimation.Duration(emote);
                emotes.Add((rest with { Emote = emote, EmoteTime = at }, emote.ToString().ToLowerInvariant()));
            }
            emotes.Add((rest with { Talking = true, Time = 1.1f }, "talking"));
            var board = Raylib.GenImageColor(W * emotes.Count, (H + Label) * 2, backdrop);
            for (int i = 0; i < emotes.Count; i++)
            {
                Cell(ref board, "CLERK~Grown", 0.35f, emotes[i].Pose, i, 0, emotes[i].Text);
                Cell(ref board, "CLERK~Grown", side, emotes[i].Pose, i, 1, emotes[i].Text);
            }
            Save(board, "fw44_emotes");
        }
    }
}
