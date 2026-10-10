using System;
using System.Collections.Generic;
using System.Numerics;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The Friendship Checker (<c>friendship_checker/main.c</c> and <c>graphics.c</c>): the team wanders about the screen,
/// bumping into one another and off its edges. Touched, a Pokémon cries and shows how it feels: one that likes the
/// player stays under the stylus with one to three hearts over it, one that doesn't keeps still and shows nothing.
/// Touching the ground near them, those that like the player gather to the stylus, those that don't run from it,
/// the faster the stronger the feeling; two quick touches of the ground make the whole team jump.
///
/// The original's stylus is held down for as long as the player likes; here a touch holds it where it fell for
/// <see cref="HoldSeconds"/> (<c>docs/mechanics/rulings.md</c>, "The Pokétch"). It runs in the original's frames,
/// sixty a second, and its distances are the original's pixels at <see cref="Scale"/> screen units each.
/// </summary>
public sealed class FriendshipCheckerApp : PoketchAppState
{
    /// <summary>How the Pokémon feels about the player (<c>FRIENDSHIP_HATE</c>, <c>_NEUTRAL</c>, <c>_LIKE</c>).</summary>
    public enum Feeling { Dislikes, Neutral, Likes }

    /// <summary>What a Pokémon is doing (<c>enum IconActions</c>).</summary>
    public enum Action { Wander, Gather, RunAway, ShowLike, ShowDislike, Jump }

    /// <summary>The screen's units to one of the original's pixels: its 192 by 160 screen is our 360 by 296.</summary>
    public const float Scale = 1.875f;

    /// <summary>How long a touch holds the stylus down where it fell.</summary>
    public const float HoldSeconds = 1.5f;

    /// <summary>How soon after a touch of the ground a second one makes the team jump.</summary>
    public const float DoubleTapSeconds = 0.4f;

    /// <summary>The menu icon's size on the screen: the original's 32 pixels at two units each.</summary>
    public const float IconSize = 64f;

    private const float Frame = 1f / 60f;

    /// <summary>The original's collision distance (twice <c>ICON_RADIUS</c>) and its touch radius (<c>ICON_RADIUS</c>).</summary>
    private const float Apart = 32 * Scale, TouchRadius = 16 * Scale;

    // Where the icons' middles may go: the screen less half an icon, and room over the top for the hearts
    private const float Left = 32, Right = Columns * 8 - 32, Top = 56, Bottom = Rows * 8 - 32;

    /// <summary>The ground that can be touched: a spot at each corner and the middle of each long side.</summary>
    private static readonly PoketchButton[] ground =
    {
        new(GroundId + 0, 5, 5, 3, 3), new(GroundId + 1, 21, 5, 3, 3), new(GroundId + 2, 37, 5, 3, 3),
        new(GroundId + 3, 5, 29, 3, 3), new(GroundId + 4, 21, 29, 3, 3), new(GroundId + 5, 37, 29, 3, 3)
    };

    /// <summary>The first id of the ground's spots; a Pokémon's id is its place in <see cref="Team"/>.</summary>
    public const int GroundId = 10;

    /// <summary>One of the team on the screen (<c>PokemonGraphic</c>).</summary>
    public sealed class Walker
    {
        public required Pokemon Pokemon { get; init; }
        public Feeling Feeling { get; init; }

        /// <summary>One to three for a feeling either way (<c>intensity</c>), nought for neither.</summary>
        public int Intensity { get; init; }

        /// <summary>Its middle, in screen units.</summary>
        public Vector2 Position { get; internal set; }

        /// <summary>Screen units a frame.</summary>
        public Vector2 Velocity { get; internal set; }

        public Action Action { get; internal set; }

        /// <summary>How high it is off the ground in a jump, in screen units.</summary>
        public float Lift { get; internal set; }

        /// <summary>Whether its shadow shows under it (the first part of a jump).</summary>
        public bool Shadow { get; internal set; }

        /// <summary>Hearts shown over it: its intensity while it shows that it likes the player, else none.</summary>
        public int Hearts => Action == Action.ShowLike && Feeling == Feeling.Likes ? Intensity : 0;

        /// <summary>Which way it faces: right while moving right (<c>UpdateIconDirection</c>).</summary>
        public bool FacingRight { get; internal set; }

        internal int Cooldown;
        internal bool BumpedFromRest;
        internal float JumpProgress;
        internal int JumpPhase;
    }

    private readonly List<Walker> walkers = new();
    private float clock;
    private bool started;

    // The stylus: where it is down and for how many frames more
    private Vector2 touch;
    private int heldFrames;
    private int touched = -1, previousTouched = -1;
    private int sinceGroundTap = int.MaxValue;
    private bool jumping;

    public override PoketchApp App => PoketchApp.FriendshipChecker;

    /// <summary>The team as it wanders, in the party's order (eggs left out, as the original's <c>Init</c> does).</summary>
    public IReadOnlyList<Walker> Team => walkers;

    /// <summary>Where the stylus is down, in screen units; null when it isn't.</summary>
    public Vector2? Stylus => heldFrames > 0 ? touch : null;

    /// <summary>
    /// The level of a friendship (<c>GetFriendshipLevel</c>): 0 under 1, 1 under 35, 2 under 70, 3 under 150, 4 under
    /// 200, 5 under 255 and 6 at 255.
    /// </summary>
    public static int Level(int friendship)
    {
        ReadOnlySpan<int> tiers = stackalloc[] { 1, 35, 70, 150, 200, 255 };
        for (int i = 0; i < tiers.Length; i++)
            if (friendship < tiers[i]) return i;
        return tiers.Length;
    }

    /// <summary>
    /// What a friendship comes to on the screen (<c>Init</c>): levels 0 to 2 dislike the player with an intensity of
    /// 3 to 1, level 3 neither, and levels 4 to 6 like the player with an intensity of 1 to 3.
    /// </summary>
    public static (Feeling Feeling, int Intensity) FeelingOf(int friendship)
    {
        int level = Level(friendship);
        return level switch
        {
            <= 2 => (Feeling.Dislikes, 3 - level),
            3 => (Feeling.Neutral, 0),
            _ => (Feeling.Likes, level - 3)
        };
    }

    // The original's places for the six icons as the app starts (SetupSprites), on our screen
    private static Vector2 StartOf(int slot) =>
        new(((slot % 2 == 0 ? 48 : 176) - 16) * Scale, ((44 + 48 * (slot / 2)) - 16) * Scale);

    private void Start(PoketchContext context)
    {
        started = true;
        walkers.Clear();
        foreach (var p in context.Party.Members)
        {
            if (walkers.Count >= Party.MaxSize) break;
            var (feeling, intensity) = FeelingOf(p.Friendship);
            var w = new Walker { Pokemon = p, Feeling = feeling, Intensity = intensity, Position = StartOf(walkers.Count) };
            w.Velocity = RandomVelocity(context.Rng);
            Face(w);
            walkers.Add(w);
        }
    }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context)
    {
        if (!started) Start(context);
        var list = new List<PoketchButton>(walkers.Count + ground.Length);
        for (int i = 0; i < walkers.Count; i++)
        {
            var p = walkers[i].Position;
            int col = Math.Clamp((int)MathF.Round(p.X / 8f) - 4, 0, Columns - 8);
            int row = Math.Clamp((int)MathF.Round(p.Y / 8f) - 4, 0, Rows - 8);
            list.Add(new PoketchButton(i, col, row, 8, 8));
        }
        list.AddRange(ground);
        return list;
    }

    public override void Press(int button, PoketchContext context)
    {
        if (!started) Start(context);
        if (jumping) return;
        if (button >= 0 && button < walkers.Count)
        {
            // The stylus put down on a Pokémon: it cries as the stylus comes onto it
            touch = walkers[button].Position;
            heldFrames = (int)MathF.Round(HoldSeconds * 60);
            previousTouched = -1;
            sinceGroundTap = int.MaxValue;
            return;
        }
        int spot = button - GroundId;
        if (spot < 0 || spot >= ground.Length) return;

        // Two touches of the ground in quick succession: everyone jumps (TapStateReleasedFirst, TapStateDoubleTap)
        if (sinceGroundTap <= (int)MathF.Round(DoubleTapSeconds * 60))
        {
            sinceGroundTap = int.MaxValue;
            heldFrames = 0;
            TriggerJump(context);
            return;
        }
        var b = ground[spot];
        touch = new Vector2(b.CentreX * 8, b.CentreY * 8);
        heldFrames = (int)MathF.Round(HoldSeconds * 60);
        sinceGroundTap = 0;
    }

    public override void Update(float dt, PoketchContext context)
    {
        if (!started) Start(context);
        clock = Math.Min(clock + dt, 10 * Frame);
        while (clock >= Frame)
        {
            clock -= Frame;
            Tick(context);
        }
    }

    /// <summary>One of the original's frames (<c>Task_UpdateGraphics</c>).</summary>
    private void Tick(PoketchContext context)
    {
        if (sinceGroundTap != int.MaxValue) sinceGroundTap++;
        bool held = heldFrames > 0;
        if (held)
        {
            heldFrames--;
            touched = TouchedPokemon();
            if (touched >= 0 && touched != previousTouched) context.Cry(walkers[touched].Pokemon);
            previousTouched = touched;
        }
        else
        {
            touched = -1;
            previousTouched = -1;
        }

        // RunIconActions
        for (int i = 0; i < walkers.Count; i++)
        {
            var w = walkers[i];
            if (w.Cooldown > 0) w.Cooldown--;
            else Act(w, i, held, context);
        }

        if (jumping)
        {
            if (walkers.TrueForAll(w => w.Action != Action.Jump)) jumping = false;
        }
        else Move();
    }

    // CheckIfTouchingMon: the first whose middle is within the touch radius
    private int TouchedPokemon()
    {
        for (int i = 0; i < walkers.Count; i++)
            if (Vector2.DistanceSquared(walkers[i].Position, touch) < TouchRadius * TouchRadius) return i;
        return -1;
    }

    private bool Near(Walker w, float pixels) => Vector2.DistanceSquared(w.Position, touch) < pixels * Scale * pixels * Scale;

    private void Act(Walker w, int slot, bool held, PoketchContext context)
    {
        switch (w.Action)
        {
            case Action.Wander: Wander(w, slot, held, context); break;
            case Action.Gather: Gather(w, slot, held); break;
            case Action.RunAway: RunAway(w, held); break;
            case Action.ShowLike: ShowLike(w, held); break;
            case Action.ShowDislike: ShowDislike(w, held, context); break;
            case Action.Jump: Jump(w, context); break;
        }
    }

    // HandleWanderAction
    private void Wander(Walker w, int slot, bool held, PoketchContext context)
    {
        if (!held)
        {
            LimitSpeed(w, 768f / 4096f * 16 * Scale, 96);
            return;
        }
        if (touched < 0)
        {
            if (!Near(w, 48)) return;
            if (w.Feeling != Feeling.Dislikes)
            {
                Become(w, Action.Gather);
                Gather(w, slot, held);
            }
            else
            {
                Become(w, Action.RunAway);
                RunAway(w, held);
            }
        }
        else if (touched == slot && Near(w, 8))
        {
            Become(w, w.Feeling != Feeling.Dislikes ? Action.ShowLike : Action.ShowDislike);
        }
    }

    private static readonly int[] speedFactor = { 100, 150, 175, 200 };

    // HandleGatherAction: toward the stylus, faster the more it likes the player
    private void Gather(Walker w, int slot, bool held)
    {
        if (!held || !Near(w, 64))
        {
            Become(w, Action.Wander);
            return;
        }
        if (touched >= 0 && touched != slot) return;
        if (Near(w, 8))
        {
            Become(w, Action.ShowLike);
            return;
        }
        w.Velocity = Towards(touch - w.Position, w.Intensity);
    }

    // HandleRunAwayAction: away from the stylus, faster the less it likes the player
    private void RunAway(Walker w, bool held)
    {
        if (!held || !Near(w, 64))
        {
            Become(w, Action.Wander);
            return;
        }
        w.Velocity = Towards(w.Position - touch, w.Intensity);
    }

    private static Vector2 Towards(Vector2 way, int intensity)
    {
        if (way.LengthSquared() < 1e-6f) return Vector2.Zero;
        return Vector2.Normalize(way) * (speedFactor[Math.Clamp(intensity, 0, 3)] / 100f) * Scale;
    }

    // HandleShowLikeAction: kept under the stylus
    private void ShowLike(Walker w, bool held)
    {
        if (held)
        {
            if (Near(w, 8))
            {
                w.Velocity = Vector2.Zero;
                return;
            }
            if (Near(w, 64))
            {
                Become(w, Action.Gather);
                return;
            }
        }
        Become(w, Action.Wander);
    }

    // HandleShowDislikeAction: keeps still under the stylus, and wanders off once it is lifted
    private void ShowDislike(Walker w, bool held, PoketchContext context)
    {
        if (held)
        {
            if (Near(w, 8))
            {
                w.Velocity = Vector2.Zero;
                return;
            }
            if (Near(w, 64))
            {
                Become(w, Action.RunAway);
                return;
            }
        }
        else if (w.Velocity == Vector2.Zero) w.Velocity = RandomVelocity(context.Rng);
        Become(w, Action.Wander);
    }

    // TriggerJumpSequence
    private void TriggerJump(PoketchContext context)
    {
        foreach (var w in walkers)
        {
            Become(w, Action.Jump);
            w.Cooldown = 0;
        }
        jumping = walkers.Count > 0;
        if (jumping) context.Sound("poketch_count");
    }

    // HandleJumpAction: up and down in 23 frames, the shadow showing until the last few
    private void Jump(Walker w, PoketchContext context)
    {
        if (w.JumpProgress == 0 && w.JumpPhase == 0)
        {
            w.Velocity = Vector2.Zero;
            w.Shadow = true;
        }
        w.JumpProgress += 8;
        if (w.JumpPhase == 0 && w.JumpProgress > 140)
        {
            w.Shadow = false;
            w.JumpPhase = 1;
        }
        if (w.JumpProgress > 180)
        {
            w.JumpProgress = 180;
            w.JumpPhase = 2;
        }
        w.Lift = 20 * MathF.Sin(w.JumpProgress * MathF.PI / 180f) * Scale;
        if (w.JumpPhase == 2)
        {
            w.Velocity = RandomVelocity(context.Rng);
            Become(w, Action.Wander);
        }
    }

    // UpdateIconAction
    private static void Become(Walker w, Action action)
    {
        w.Action = action;
        w.JumpProgress = 0;
        w.JumpPhase = 0;
        w.Lift = 0;
        w.Shadow = false;
    }

    // LimitIconSpeed: anything faster than the most slows by the rate, a hundredth at a time
    private static void LimitSpeed(Walker w, float most, int rate)
    {
        float speed = w.Velocity.Length();
        if (speed > most) w.Velocity = Vector2.Normalize(w.Velocity) * (speed * rate / 100f);
    }

    // SetRandomVelocity: a way drawn from the generator, at one of the original's pixels a frame
    private static Vector2 RandomVelocity(Random rng)
    {
        var v = new Vector2(rng.Next(64) - 32, rng.Next(64) - 32);
        return v.LengthSquared() < 1e-6f ? Vector2.Zero : Vector2.Normalize(v) * Scale;
    }

    private static void Face(Walker w) => w.FacingRight = w.Velocity.X > 0;

    /// <summary>
    /// UpdateIconPositions, a frame at a time: everyone moves, bounces off the screen's edges, and two that touch
    /// trade their speeds along the line between them; one showing that it likes the player holds its ground and
    /// pushes the other off.
    /// </summary>
    private void Move()
    {
        foreach (var w in walkers) w.Position += w.Velocity;

        foreach (var w in walkers)
        {
            var p = w.Position;
            var v = w.Velocity;
            if ((p.X < Left && v.X < 0) || (p.X > Right && v.X > 0)) { v.X = -v.X; w.BumpedFromRest = false; }
            if ((p.Y < Top && v.Y < 0) || (p.Y > Bottom && v.Y > 0)) { v.Y = -v.Y; w.BumpedFromRest = false; }
            w.Velocity = v;
            w.Position = Vector2.Clamp(p, new Vector2(Left, Top), new Vector2(Right, Bottom));
        }

        for (int b = 0; b < walkers.Count; b++)
            for (int a = 0; a < b; a++)
                Collide(walkers[a], walkers[b]);

        foreach (var w in walkers)
            if (w.Action < Action.ShowLike) Face(w);
    }

    private static void Collide(Walker a, Walker b)
    {
        var between = a.Position - b.Position;
        float distance = between.Length();
        if (distance >= Apart || distance < 1e-4f) return;
        var normal = between / distance;
        float closing = Vector2.Dot(a.Velocity - b.Velocity, normal);
        if (closing >= 0) return;

        if (a.Action == Action.ShowLike) PushOff(b, a);
        else if (b.Action == Action.ShowLike) PushOff(a, b);
        else
        {
            a.Velocity -= closing * normal;
            b.Velocity += closing * normal;
            a.Cooldown = b.Cooldown = 20;
            a.BumpedFromRest = b.BumpedFromRest = false;
        }
    }

    // One that bumps into a Pokémon showing its liking is turned back, or nudged away if it was standing still
    private static void PushOff(Walker mover, Walker still)
    {
        if (mover.Velocity == Vector2.Zero || mover.BumpedFromRest)
        {
            var away = mover.Position - still.Position;
            mover.Velocity = away.LengthSquared() < 1e-6f ? Vector2.Zero : Vector2.Normalize(away) * 0.1f * 16 * Scale;
            mover.BumpedFromRest = true;
        }
        else mover.Velocity = -mover.Velocity;
        mover.Cooldown = 20;
    }
}
