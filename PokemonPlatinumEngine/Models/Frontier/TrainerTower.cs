using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>FireRed's four challenges of the Trainer Tower (<c>CHALLENGE_TYPE_*</c>), in its order.</summary>
public enum TrainerTowerChallenge { Single, Double, Knockout, Mixed }

/// <summary>
/// FireRed's Trainer Tower on Seven Island (plan 37 · FC6; <c>docs/mechanics/rulings.md</c>, "The Trainer Tower"), with
/// the Trainer Hill's rules where the two games agree (<see cref="TrainerHill.LevelFor"/>, <see cref="TrainerHill.TimeOf"/>,
/// a battle of the Frontier's on the player's own team): a challenge of eight floors climbed against the clock to the
/// owner on the roof. A floor is FireRed's kind: a single battle against one trainer bringing two of their six, a double
/// battle against two trainers bringing one each, or a knockout of three trainers one after another with one each, which
/// of the six by the floors cleared (<c>BuildEnemyParty</c>'s tables), every Pokémon at the highest level of the player's
/// team (<c>GetPartyMaxLevel</c>). The owner gives the challenge's prize, its first floor's (<c>GiveChallengePrize</c>),
/// once a challenge, and keeps the best time of each. What a climb keeps is the story's own variables (<see cref="Vars"/>),
/// so a game saved on a floor lays the floor again as it is come back to. No drawing or input.
/// </summary>
public static class TrainerTower
{
    /// <summary>Floors to a challenge (<c>MAX_TRAINER_TOWER_FLOORS</c>); the roof is the ninth.</summary>
    public const int Floors = 8, Roof = 9;

    /// <summary>The longest time, in frames of sixty a second: an hour less a frame, as the Trainer Hill's.</summary>
    public const int MaxFrames = TrainerHill.MaxFrames;

    /// <summary>The story's variables a climb keeps its state in.</summary>
    public static class Vars
    {
        /// <summary>0 while no climb is under way, 1 climbing, 2 lost, 3 at the roof.</summary>
        public const string State = "VAR_KANTO_TRAINER_TOWER_STATE";
        /// <summary>The challenge taken, as <see cref="TrainerTowerChallenge"/>.</summary>
        public const string Challenge = "VAR_KANTO_TRAINER_TOWER_CHALLENGE";
        /// <summary>The floor the player is on, 1 to 8, or 9 on the roof.</summary>
        public const string Floor = "VAR_KANTO_TRAINER_TOWER_FLOOR";
        /// <summary>The play time in seconds as the climb began, which the clock counts from.</summary>
        public const string Start = "VAR_KANTO_TRAINER_TOWER_START";
        /// <summary>The time the roof was reached in, in frames.</summary>
        public const string Time = "VAR_KANTO_TRAINER_TOWER_TIME";
        /// <summary>How many of the floor's trainers are beaten (a knockout's one after another; the others' at once).</summary>
        public const string Beaten = "VAR_KANTO_TRAINER_TOWER_BEATEN";
        /// <summary>1 once the owner has held this climb's time against the best (<c>checkedFinalTime</c>).</summary>
        public const string Checked = "VAR_KANTO_TRAINER_TOWER_CHECKED";
        /// <summary>The best time of each challenge in frames; 0 for none yet.</summary>
        public static string Best(TrainerTowerChallenge challenge) => $"VAR_KANTO_TRAINER_TOWER_BEST_{challenge.ToString().ToUpperInvariant()}";
        /// <summary>1 once the owner has given a challenge's prize (<c>receivedPrize</c>, kept for each challenge).</summary>
        public static string Prize(TrainerTowerChallenge challenge) => $"VAR_KANTO_TRAINER_TOWER_PRIZE_{challenge.ToString().ToUpperInvariant()}";
    }

    /// <summary>The values of <see cref="Vars.State"/>.</summary>
    public static class States
    {
        public const int None = 0, Climbing = 1, Lost = 2, Reached = 3;
    }

    /// <summary>The kinds of a floor (FireRed's <c>CHALLENGE_TYPE_*</c> of each floor).</summary>
    public const int Single = 0, Double = 1, Knockout = 2;

    public static TrainerTowerChallenge ChallengeOf(StoryState story) => (TrainerTowerChallenge)Math.Clamp(story.Var(Vars.Challenge), 0, 3);

    public static int FloorOf(StoryState story) => Math.Clamp(story.Var(Vars.Floor), 1, Roof);

    /// <summary>A climb begun (<c>StartTrainerTowerChallenge</c>): the clock started, the first floor next.</summary>
    public static void Begin(StoryState story, TrainerTowerChallenge challenge, int playSeconds)
    {
        story.SetVar(Vars.State, States.Climbing);
        story.SetVar(Vars.Challenge, (int)challenge);
        story.SetVar(Vars.Floor, 1);
        story.SetVar(Vars.Start, playSeconds);
        story.SetVar(Vars.Time, 0);
        story.SetVar(Vars.Beaten, 0);
        story.SetVar(Vars.Checked, 0);
    }

    /// <summary>The floor's kind: FireRed's for the challenge and floor (a Mixed challenge's floors are each of one kind).</summary>
    public static int KindOf(TrainerTowerChallenge challenge, int floor) => TrainerTowerDatabase.Floor((int)challenge, floor).Kind;

    /// <summary>How many trainers a floor has: one, two or three.</summary>
    public static int TrainersOn(TrainerTowerChallenge challenge, int floor) => KindOf(challenge, floor) + 1;

    /// <summary>
    /// The trainers of the battle to come on a floor: the single floor's one with two of their six, the double floor's two
    /// with one each, the knockout floor's next unbeaten with one, each Pokémon at <paramref name="level"/>
    /// (<c>BuildEnemyParty</c>: which of the six by the floors cleared, the floor before this one).
    /// </summary>
    public static List<Trainer> OpponentsOf(TrainerTowerChallenge challenge, int floor, int beaten, int level)
    {
        var record = TrainerTowerDatabase.Floor((int)challenge, floor);
        int cleared = Math.Clamp(floor - 1, 0, Floors - 1);
        switch (record.Kind)
        {
            case Single:
                return new List<Trainer> { TrainerOf(record.Trainers[0], 0, TrainerTowerDatabase.SingleSlots[cleared], level, challenge, floor) };
            case Double:
                return Enumerable.Range(0, 2)
                    .Select(t => TrainerOf(record.Trainers[t], t, new[] { TrainerTowerDatabase.DoubleSlots[cleared][t] }, level, challenge, floor)).ToList();
            default:
            {
                int t = Math.Clamp(beaten, 0, 2);
                return new List<Trainer> { TrainerOf(record.Trainers[t], t, new[] { TrainerTowerDatabase.KnockoutSlots[cleared][t] }, level, challenge, floor) };
            }
        }
    }

    private static Trainer TrainerOf(TrainerHillTrainerRecord record, int place, IEnumerable<int> slots, int level, TrainerTowerChallenge challenge, int floor)
    {
        var made = new Trainer
        {
            Id = $"trainer_tower_{(int)challenge}_{floor}_{place}",
            Name = record.Name,
            TrainerClass = record.Class,
            PrizeMoney = 0,
            Ai = FrontierTrainers.Mind,
            Look = CharacterStyles.Default.CharacterFor(record.Look),
            Items = new List<string>()
        };
        foreach (int slot in slots) made.Party.Add(TrainerHill.Build(record.Team[slot], level));
        return made;
    }

    /// <summary>A floor's trainer dressed for the room (their class, name and look, with the Pokémon they bring).</summary>
    public static FrontierOpponent DressOf(Trainer trainer) => new()
    {
        Number = 0, Pokemon = trainer.Party.Members.ToList(),
        Dress = new FrontierDress(trainer.Id, trainer.TrainerClass, trainer.Name, trainer.Look ?? "Trainer")
    };

    /// <summary>A battle of the floor won: the knockout's next stands, any other floor is cleared. True once the floor is cleared.</summary>
    public static bool Won(StoryState story)
    {
        var challenge = ChallengeOf(story);
        int floor = FloorOf(story);
        int beaten = KindOf(challenge, floor) == Knockout ? story.Var(Vars.Beaten) + 1 : TrainersOn(challenge, floor);
        story.SetVar(Vars.Beaten, beaten);
        return beaten >= TrainersOn(challenge, floor);
    }

    /// <summary>The time the clock shows: frames since the climb began, up to the limit, stopped at the roof.</summary>
    public static int Frames(StoryState story, int playSeconds) =>
        story.Var(Vars.State) == States.Reached ? story.Var(Vars.Time)
        : (int)Math.Clamp((long)(playSeconds - story.Var(Vars.Start)) * 60, 0, MaxFrames);

    /// <summary>The stairs climbed: the next floor with nobody of it beaten, or the roof after the eighth, where the clock stops. True at the roof.</summary>
    public static bool Climb(StoryState story, int playSeconds)
    {
        int floor = Math.Min(Roof, FloorOf(story) + 1);
        story.SetVar(Vars.Floor, floor);
        story.SetVar(Vars.Beaten, 0);
        if (floor < Roof) return false;
        story.SetVar(Vars.Time, Frames(story, playSeconds));
        story.SetVar(Vars.State, States.Reached);
        return true;
    }

    /// <summary>The challenge's prize: its first floor's (<c>GiveChallengePrize</c>).</summary>
    public static string PrizeOf(TrainerTowerChallenge challenge) => TrainerTowerDatabase.Floor((int)challenge, 1).Prize;

    /// <summary>The time held against the challenge's best (<c>CheckFinalTime</c>): 0 a new record (kept), 1 not, 2 already held this climb.</summary>
    public static int CheckTime(StoryState story)
    {
        if (story.Var(Vars.Checked) == 1) return 2;
        story.SetVar(Vars.Checked, 1);
        var challenge = ChallengeOf(story);
        int time = story.Var(Vars.Time), best = story.Var(Vars.Best(challenge));
        if (best != 0 && best <= time) return 1;
        story.SetVar(Vars.Best(challenge), Math.Max(1, time));
        return 0;
    }
}
