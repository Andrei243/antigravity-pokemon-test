using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The Matchup Checker (<c>matchup_checker/main.c</c> and <c>graphics.c</c>): two Pokémon of the team, one either
/// side, each changed by touching it, and the button between them that tells how well the pair would get on at the
/// Day Care. Two heart-shaped fish swim toward one another a step for each heart the pair earns: three and they meet
/// with a kiss and the meter's hearts flash; none and they turn their backs and swim apart
/// (<c>Task_RunMatchupAnimation</c>). The answer is the Day Care's own (<c>BoxMon_GetPairDaycareCompatibilityLevel</c>).
/// </summary>
public sealed class MatchupCheckerApp : PoketchAppState
{
    /// <summary>The buttons (<c>sHitboxes</c>): the check between them, and each side's Pokémon.</summary>
    public const int Check = 0, ChangeLeft = 1, ChangeRight = 2;

    public static readonly PoketchButton CheckButton = new(Check, 18, 26, 9, 9);
    public static readonly PoketchButton LeftButton = new(ChangeLeft, 2, 27, 11, 8);
    public static readonly PoketchButton RightButton = new(ChangeRight, 32, 27, 11, 8);

    private static readonly PoketchButton[] buttons = { CheckButton, LeftButton, RightButton };

    /// <summary>How a pair gets on (<c>PARENTS_*</c>), as its level: 0 the best, 3 not at all (<c>DaycareCompatibilityScoreToLevel</c>).</summary>
    public const int Best = 0, Good = 1, Poor = 2, None = 3;

    private const float Frame = 1f / 60f;

    private enum Op { Forward, Backward, Wait, Sound, TurnAway, Kiss }

    private readonly record struct Command(Op Op, int Frames = 0, int Pixels = 0, string? Sound = null);

    // The original's four sequences (sCommandsIncompatible ... sCommandsMaxCompatibility); its sounds 012 to 014 are
    // stood in for by the bank's own (docs/mechanics/rulings.md, "The Pokétch")
    private static readonly Command[][] sequences =
    {
        new Command[]
        {
            new(Op.Forward, 16, 16), new(Op.Sound, Sound: "poketch"), new(Op.Forward, 16, 16), new(Op.Sound, Sound: "poketch"),
            new(Op.Forward, 16, 16), new(Op.Sound, Sound: "poketch"), new(Op.Wait, 16), new(Op.Sound, Sound: "dowsing_ping"),
            new(Op.Kiss), new(Op.Wait, 16)
        },
        new Command[] { new(Op.Forward, 16, 16), new(Op.Sound, Sound: "poketch"), new(Op.Forward, 16, 16), new(Op.Sound, Sound: "poketch") },
        new Command[] { new(Op.Forward, 16, 16), new(Op.Sound, Sound: "poketch") },
        new Command[]
        {
            new(Op.Forward, 16, 16), new(Op.Wait, 16), new(Op.TurnAway), new(Op.Sound, Sound: "poketch_beep"),
            new(Op.Backward, 16, 16)
        }
    };

    private Command[]? running;
    private int step, timer;
    private float moveFrom, moveTo;
    private float clock;
    private int frames;

    public override PoketchApp App => PoketchApp.MatchupChecker;

    /// <summary>The team's places of the two Pokémon (<c>leftMonIdx</c>, <c>rightMonIdx</c>): the first two to begin with.</summary>
    public int Left { get; private set; }
    public int Right { get; private set; } = 1;

    /// <summary>The last pair's level, <see cref="Best"/> to <see cref="None"/>; null before a check or after a change.</summary>
    public int? Result { get; private set; }

    /// <summary>How far each fish has swum toward the middle, in the original's pixels: 16 a step.</summary>
    public float Offset { get; private set; }

    /// <summary>Whether the fish have turned their backs on one another (no match).</summary>
    public bool TurnedAway { get; private set; }

    /// <summary>Whether the fish are kissing and the meter's hearts flash (the best match).</summary>
    public bool Kissing { get; private set; }

    /// <summary>Hearts lit on the meter: none to three, three for the best.</summary>
    public int Hearts => Result is { } level ? 3 - level : 0;

    /// <summary>Whether the fish are still swimming: nothing can be touched meanwhile.</summary>
    public bool Busy => running != null;

    /// <summary>Whether the check button is shown pressed in: while a check runs, or for good with no pair to check.</summary>
    public bool CheckPressed(PoketchContext context) => Busy || Team(context).Count <= 1;

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    /// <summary>The Pokémon on one side, or null with none there.</summary>
    public Pokemon? LeftPokemon(PoketchContext context) => At(context, Left);
    public Pokemon? RightPokemon(PoketchContext context) => Team(context).Count > 1 ? At(context, Right) : null;

    private static Pokemon? At(PoketchContext context, int i) => i >= 0 && i < Team(context).Count ? Team(context)[i] : null;

    // The team as the app counts it: its Pokémon that aren't Eggs (the original's matchup data skips them)
    private static List<Pokemon> Team(PoketchContext context) => context.Party.Members.Where(p => !p.IsEgg).ToList();

    // A team that has shrunk since the app came up keeps the two within it
    private void KeepInTeam(PoketchContext context)
    {
        int count = Team(context).Count;
        if (Left >= count) Left = 0;
        if (Right >= count || Right == Left) Right = count > 1 ? (Left == 0 ? 1 : 0) : 0;
    }

    public override void Press(int button, PoketchContext context)
    {
        if (Busy) return;
        KeepInTeam(context);
        int count = Team(context).Count;
        switch (button)
        {
            case ChangeLeft:
                // UpdateLeftMon: the next of the team, past the one on the right; only with more than two
                if (count <= 2) return;
                do Left = (Left + 1) % count; while (Left == Right);
                Changed(context, Team(context)[Left]);
                break;
            case ChangeRight:
                if (count <= 2) return;
                do Right = (Right + 1) % count; while (Right == Left);
                Changed(context, Team(context)[Right]);
                break;
            case Check:
                if (count <= 1)
                {
                    context.Sound("poketch_beep");
                    return;
                }
                context.Sound("poketch_count");
                Result = Level(Team(context)[Left], Team(context)[Right]);
                Reset();
                running = sequences[Result.Value];
                step = 0;
                Run(context);
                break;
        }
    }

    // Task_UpdateLeftMonIcon: the new one cries, and the fish go back to their places (ResetIndicatorPositions)
    private void Changed(PoketchContext context, Pokemon pokemon)
    {
        context.Cry(pokemon);
        Result = null;
        Reset();
    }

    private void Reset()
    {
        Offset = 0;
        TurnedAway = false;
        Kissing = false;
    }

    public override void Update(float dt, PoketchContext context)
    {
        clock = Math.Min(clock + dt, 10 * Frame);
        while (clock >= Frame)
        {
            clock -= Frame;
            frames++;
            Tick(context);
        }
    }

    /// <summary>Frames since the app came up, for the meter's flash.</summary>
    public int Frames => frames;

    // RunAnimationSequence, case 1 and 2: one frame of a move or a wait
    private void Tick(PoketchContext context)
    {
        if (running == null || timer <= 0) return;
        var command = running[step - 1];
        timer--;
        if (command.Op is Op.Forward or Op.Backward)
            Offset = timer > 0 ? moveTo - (moveTo - moveFrom) * timer / command.Frames : moveTo;
        if (timer == 0) Run(context);
    }

    // RunAnimationSequence, case 0: the commands up to the next that takes time
    private void Run(PoketchContext context)
    {
        while (running != null && step < running.Length)
        {
            var command = running[step++];
            switch (command.Op)
            {
                case Op.Forward:
                case Op.Backward:
                    moveFrom = Offset;
                    moveTo = Offset + (command.Op == Op.Forward ? command.Pixels : -command.Pixels);
                    timer = command.Frames;
                    return;
                case Op.Wait:
                    timer = command.Frames;
                    return;
                case Op.Sound:
                    context.Sound(command.Sound!);
                    break;
                case Op.TurnAway:
                    TurnedAway = true;
                    break;
                case Op.Kiss:
                    Kissing = true;
                    break;
            }
        }
        running = null;
    }

    /// <summary>
    /// How well two Pokémon would get on at the Day Care (<c>BoxMon_GetPairDaycareCompatibilityScore</c>), as a level
    /// (<c>DaycareCompatibilityScoreToLevel</c>): never if either is of the Undiscovered group or both are Ditto; with
    /// one Ditto, <see cref="Poor"/> from the same trainer and <see cref="Good"/> from two; never if their genders are
    /// the same or either has none, or they share no egg group; then <see cref="Best"/> for the same species from two
    /// trainers, <see cref="Good"/> for the same species from one or two species from two, <see cref="Poor"/> for
    /// two species from one. The egg groups are the species' own, whatever its form, as the original reads them.
    /// </summary>
    public static int Level(Pokemon a, Pokemon b) => Breeding.Level(Breeding.Compatibility(a, b));

}
