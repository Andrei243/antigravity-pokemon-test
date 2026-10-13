using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// The events of the Battle Arcade's game board, by the original's numbers (<c>ARCADE_EFFECT_…</c>): nine that fall on
/// the trainer to come, the same nine on the challenger, six on the battle itself, three on the board, and five
/// bonuses. <see cref="None"/> is the original's <c>NUM_ARCADE_EFFECTS</c>: nothing has been drawn.
/// </summary>
public enum ArcadeEffect
{
    LowerFoeHp, PoisonFoe, ParalyzeFoe, BurnFoe, SleepFoe, FreezeFoe, FoeGetBerry, FoeGetItem, FoeLevelUp,
    LowerAllyHp, PoisonAlly, ParalyzeAlly, BurnAlly, SleepAlly, FreezeAlly, AllyGetBerry, AllyGetItem, AllyLevelUp,
    Sunny, Rainy, Sandy, Hail, Foggy, TrickRoom, SpeedUp, SlowDown, RandomizeCursor,
    SwapMons, Get1Bp, NoBattle, NoEvent, Get3Bp,
    None
}

/// <summary>Whom an event of the board falls on (<c>ARCADE_EFFECT_CATEGORY_…</c>): the trainer to come, the challenger, the battle and the board for both, or neither (a bonus).</summary>
public enum ArcadeCategory { Foe, Ally, Field, Bonus }

/// <summary>Where the game board stands (<c>State_OperateRoulette</c>'s steps).</summary>
public enum ArcadeBoardStep
{
    /// <summary>The board is coming up: nothing on it yet.</summary>
    Opening,
    /// <summary>Every panel counts down: three, two, one.</summary>
    Three,
    Two,
    One,
    /// <summary>The events are shown and the cursor runs over them.</summary>
    Running,
    /// <summary>The cursor has been stopped: every panel shows the event it stopped on.</summary>
    Stopped,
    Done
}

/// <summary>
/// The Battle Arcade's game board (plan 06 · R18d), on the original's application
/// (<c>src/applications/frontier/battle_arcade/main.c</c>): sixteen panels, four to a row, each with one event. Which
/// events may come up goes by the round and by the battle of the round; how many of them fall on the trainer, on the
/// challenger, on the battle or are bonuses goes by how the last battle was won. The panels count down from three,
/// the events are shown, and a cursor runs over them from panel to panel until the button is pressed or thirty
/// seconds are up: the event it stands on is the battle's. A frame is a thirtieth of a second
/// (<see cref="Advance"/>), as the original's. Every draw is on the sixteen-bit draw it was made with, in the
/// original's order. No drawing or input.
/// </summary>
public sealed class ArcadeBoard
{
    /// <summary><c>GRID_WIDTH</c>, <c>GRID_HEIGHT</c>, <c>GRID_SIZE</c>.</summary>
    public const int Columns = 4, Rows = 4, Size = Columns * Rows;

    /// <summary>The frames before the count begins, each number of the count stays, the chosen event is shown for, and the cursor runs at most (<c>State_OperateRoulette</c>, <c>rouletteTimer</c>).</summary>
    public const int OpeningFrames = 10, CountFrames = 24, ShownFrames = 30, TimeLimit = 30 * 30;

    /// <summary>The fastest the cursor goes (<c>ArcadeEffect_SpeedUp</c>): a panel a frame.</summary>
    public const int FastestSpeed = 7;

    public int Round { get; }

    /// <summary>The battles won before this one (<c>battleStreak</c>): the battle of the round is what is left of it by seven.</summary>
    public int Streak { get; }

    /// <summary>How the last battle went (<c>performance</c>), and the row of the weights it gives (<c>categoryTier</c>).</summary>
    public int Performance { get; }
    public int Tier { get; }

    /// <summary>The cursor's speed, 0 to 7, and whether it jumps about instead of going round (<c>randomizeCursorMovement</c>).</summary>
    public int Speed { get; }
    public bool Randomized { get; }

    /// <summary>A number drawn as the board opens (<c>randomIndex</c>): which Pokémon an event falls on, and which berry or item it gives.</summary>
    public int RandomIndex { get; }

    /// <summary>The events that may come up (<c>availableEffects</c>), in their numbers' order.</summary>
    public IReadOnlyList<ArcadeEffect> Available { get; }

    /// <summary>The sixteen panels (<c>rouletteEffects</c>).</summary>
    public IReadOnlyList<ArcadeEffect> Panels => panels;
    private List<ArcadeEffect> panels;

    /// <summary>The panel the cursor stands on, 0 to 15 (<c>cursorPos</c>).</summary>
    public int Cursor { get; private set; }

    public ArcadeBoardStep Step { get; private set; } = ArcadeBoardStep.Opening;

    /// <summary>The frames the cursor may still run (<c>rouletteTimer</c>).</summary>
    public int TimeLeft { get; private set; } = TimeLimit;

    /// <summary>The frames played so far.</summary>
    public int Frame { get; private set; }

    /// <summary>The cursor moved in the frame just played, and the step changed in it: what a screen plays its sounds by.</summary>
    public bool Ticked { get; private set; }
    public bool Stepped { get; private set; }

    /// <summary>The button stopped the cursor; false when the time ran out.</summary>
    public bool Pressed { get; private set; }

    public bool IsDone => Step == ArcadeBoardStep.Done;

    /// <summary>The event the cursor stopped on (<c>selectedEffect</c>); <see cref="ArcadeEffect.None"/> while it runs.</summary>
    public ArcadeEffect Effect => Step >= ArcadeBoardStep.Stopped ? Panels[Cursor] : ArcadeEffect.None;

    private readonly Func<int> next16;
    private int sub, timer, cursorTimer;

    /// <summary>
    /// <c>BattleArcadeApp_Init</c> and the three steps of its coming up (<c>GetAvailableEffects</c>,
    /// <c>GetEffectCategoryCounts</c>, <c>AssignEffectsToGrid</c>): a number for the events to go by, the panel the
    /// cursor starts on, and then each panel's kind and event, two draws a panel.
    /// </summary>
    public ArcadeBoard(int round, int streak, int performance, int speed, bool randomized, Func<int> next16)
    {
        this.next16 = () => next16() & 0xFFFF;
        Round = round;
        Streak = streak;
        Performance = performance;
        Speed = Math.Clamp(speed, 0, FastestSpeed);
        Randomized = randomized;
        RandomIndex = this.next16();
        Cursor = this.next16() % Size;
        Available = AvailableAt(round, streak);
        Tier = TierOf(performance);
        panels = Assign(Available, Tier, this.next16);
    }

    /// <summary>
    /// For a tool or a test that wants one event shown: the panels become these, one after another round the board,
    /// whatever was drawn. The game never calls it.
    /// </summary>
    public void Rig(params ArcadeEffect[] events)
    {
        if (events.Length == 0) return;
        panels = Enumerable.Range(0, Size).Select(i => events[i % events.Length]).ToList();
    }

    /// <summary><c>BattleArcade_GetCategoryFromEffect</c>.</summary>
    public static ArcadeCategory CategoryOf(ArcadeEffect effect) =>
        effect < ArcadeEffect.LowerAllyHp ? ArcadeCategory.Foe
        : effect < ArcadeEffect.Sunny ? ArcadeCategory.Ally
        : effect < ArcadeEffect.SwapMons ? ArcadeCategory.Field
        : ArcadeCategory.Bonus;

    /// <summary>
    /// <c>GetAvailableEffects</c>: the events of a round (the seventh round's from then on), less those that don't
    /// come up at this battle of it: the board is left alone before a round's last battle (no faster, slower or
    /// jumping cursor, no swap, nothing given, nothing passed), a Battle Point is to be had before its first, third
    /// and fifth, and three of them or a battle passed before its second, fourth and sixth.
    /// </summary>
    public static List<ArcadeEffect> AvailableAt(int round, int streak)
    {
        var available = new List<ArcadeEffect>();
        var effects = FrontierDatabase.Arcade.Effects;
        int battle = streak >= FrontierRecords.Most ? BattleArcadeChallenge.BattlesPerRound - 1 : streak % BattleArcadeChallenge.BattlesPerRound;
        for (int effect = 0; effect < effects.Count; effect++)
        {
            var record = effects[effect];
            if (record.Rounds[Math.Clamp(round, 0, record.Rounds.Length - 1)] == 0) continue;
            if (record.Battles != null && record.Battles[battle] == 0) continue;
            available.Add((ArcadeEffect)effect);
        }
        return available;
    }

    /// <summary><c>GetEffectCategoryCounts</c>: the first of the four scores the last battle reached, or the last row when it reached none.</summary>
    public static int TierOf(int performance)
    {
        var thresholds = FrontierDatabase.Arcade.Thresholds;
        int tier = 0;
        while (tier < thresholds.Count && performance < thresholds[tier]) tier++;
        return tier;
    }

    /// <summary>
    /// <c>AssignEffectsToGrid</c>: for each panel a kind by the row's weights (<c>GetRandomCategory</c>) and then one
    /// of that kind's events. A kind with no event to give (the battle's own, before the last battle of the first and
    /// of the fifth round) is walked past as the original walks: on through the list from where that kind would
    /// begin, to the first event of the next kind that has one. The draw of an event among none is the remainder of
    /// a division by nought, which the console's own routine leaves at nought (docs/mechanics/rulings.md).
    /// </summary>
    public static List<ArcadeEffect> Assign(IReadOnlyList<ArcadeEffect> available, int tier, Func<int> next16)
    {
        var weights = FrontierDatabase.Arcade.Weights[Math.Clamp(tier, 0, FrontierDatabase.Arcade.Weights.Count - 1)];
        int[] counts = new int[4];
        foreach (var effect in available) counts[(int)CategoryOf(effect)]++;
        var panels = new List<ArcadeEffect>();
        for (int i = 0; i < Size; i++)
        {
            int roll = (next16() & 0xFFFF) % 100, category = 0, sum = 0;
            for (; category < 4; category++)
            {
                sum += weights[category];
                if (roll < sum) break;
            }
            if (category >= 4) category = 0;
            int offset = counts.Take(category).Sum(), drawn = next16() & 0xFFFF;
            int start = (counts[category] == 0 ? 0 : drawn % counts[category]) + offset, index = start, loops = 0;
            var panel = ArcadeEffect.LowerFoeHp;
            while (loops < 50)
            {
                if (index < available.Count && (int)CategoryOf(available[index]) == category)
                {
                    panel = available[index];
                    break;
                }
                index++;
                loops++;
                if (index >= available.Count) index = 0;
                if (index == start && ++category >= 4) category = 0;
            }
            panels.Add(panel);
        }
        return panels;
    }

    /// <summary>
    /// One frame (<c>State_OperateRoulette</c>): the board comes up, counts down, and then the cursor runs, a panel
    /// every so many frames by its speed (<c>UpdateCursorPosition</c>), until <paramref name="press"/> stops it or
    /// its time is up. The cursor moves before the button is looked at, so a press takes the panel the cursor has
    /// just come to. The event stays up for a second, and the board is done.
    /// </summary>
    public void Advance(bool press = false)
    {
        Ticked = false;
        var before = Step;
        switch (sub)
        {
            case 0:
                timer = 0;
                sub = 1;
                break;
            case 1:
                if (++timer >= OpeningFrames) sub = 2;
                break;
            case 2:
                Step = ArcadeBoardStep.Three;
                timer = CountFrames;
                sub = 3;
                break;
            case 3:
                if (--timer > 0) break;
                Step = ArcadeBoardStep.Two;
                timer = CountFrames;
                sub = 4;
                break;
            case 4:
                if (--timer > 0) break;
                Step = ArcadeBoardStep.One;
                timer = CountFrames;
                sub = 5;
                break;
            case 5:
                if (--timer > 0) break;
                Step = ArcadeBoardStep.Running;
                sub = 6;
                break;
            case 6:
                if (++cursorTimer >= FrontierDatabase.Arcade.CursorFrames[Speed])
                {
                    cursorTimer = 0;
                    Cursor = Randomized ? next16() % Size : Cursor + 1;
                    if (Cursor >= Size) Cursor = 0;
                    Ticked = true;
                }
                if (TimeLeft > 0) TimeLeft--;
                if (press || TimeLeft == 0)
                {
                    Pressed = press;
                    sub = 7;
                }
                break;
            case 7:
                Step = ArcadeBoardStep.Stopped;
                timer = ShownFrames;
                sub = 8;
                break;
            case 8:
                if (--timer == 0) Step = ArcadeBoardStep.Done;
                break;
        }
        Frame++;
        Stepped = Step != before;
    }

    /// <summary>Plays the board to its end with the button pressed on the first frame the cursor shows <paramref name="panel"/> (or, with -1, on the first it runs at all): what a tool or a test stops it by.</summary>
    public ArcadeEffect PlayTo(int panel = -1)
    {
        while (!IsDone) Advance(Step == ArcadeBoardStep.Running && (panel < 0 || WouldLandOn(panel)));
        return Effect;
    }

    // Whether the frame to come brings the cursor to that panel: only told where it goes round (a jumping cursor is stopped wherever it is)
    private bool WouldLandOn(int panel)
    {
        if (Randomized) return true;
        bool moves = cursorTimer + 1 >= FrontierDatabase.Arcade.CursorFrames[Speed];
        return (moves ? (Cursor + 1) % Size : Cursor) == panel;
    }

    /// <summary>What an event is called on the board and by the host: our own words.</summary>
    public static string NameOf(ArcadeEffect effect) => effect switch
    {
        ArcadeEffect.LowerFoeHp or ArcadeEffect.LowerAllyHp => Text.Of("arcadeBoard.effect.hpDown"),
        ArcadeEffect.PoisonFoe or ArcadeEffect.PoisonAlly => Text.Of("arcadeBoard.effect.poison"),
        ArcadeEffect.ParalyzeFoe or ArcadeEffect.ParalyzeAlly => Text.Of("arcadeBoard.effect.paralysis"),
        ArcadeEffect.BurnFoe or ArcadeEffect.BurnAlly => Text.Of("arcadeBoard.effect.burn"),
        ArcadeEffect.SleepFoe or ArcadeEffect.SleepAlly => Text.Of("arcadeBoard.effect.sleep"),
        ArcadeEffect.FreezeFoe or ArcadeEffect.FreezeAlly => Text.Of("arcadeBoard.effect.freeze"),
        ArcadeEffect.FoeGetBerry or ArcadeEffect.AllyGetBerry => Text.Of("arcadeBoard.effect.berry"),
        ArcadeEffect.FoeGetItem or ArcadeEffect.AllyGetItem => Text.Of("arcadeBoard.effect.item"),
        ArcadeEffect.FoeLevelUp or ArcadeEffect.AllyLevelUp => Text.Of("arcadeBoard.effect.levelUp"),
        ArcadeEffect.Sunny => Text.Of("arcadeBoard.effect.sun"),
        ArcadeEffect.Rainy => Text.Of("arcadeBoard.effect.rain"),
        ArcadeEffect.Sandy => Text.Of("arcadeBoard.effect.sandstorm"),
        ArcadeEffect.Hail => Text.Of("arcadeBoard.effect.hail"),
        ArcadeEffect.Foggy => Text.Of("arcadeBoard.effect.fog"),
        ArcadeEffect.TrickRoom => Text.Of("arcadeBoard.effect.trickRoom"),
        ArcadeEffect.SpeedUp => Text.Of("arcadeBoard.effect.speedUp"),
        ArcadeEffect.SlowDown => Text.Of("arcadeBoard.effect.slowDown"),
        ArcadeEffect.RandomizeCursor => Text.Of("arcadeBoard.effect.random"),
        ArcadeEffect.SwapMons => Text.Of("arcadeBoard.effect.swap"),
        ArcadeEffect.Get1Bp => Text.Of("arcadeBoard.effect.oneBp"),
        ArcadeEffect.Get3Bp => Text.Of("arcadeBoard.effect.threeBp"),
        ArcadeEffect.NoBattle => Text.Of("arcadeBoard.effect.noBattle"),
        ArcadeEffect.NoEvent => Text.Of("arcadeBoard.effect.noEvent"),
        _ => ""
    };
}

/// <summary>A Battle Arcade challenge put down between two battles (what <c>BattleArcade_Save</c> writes).</summary>
public sealed class ArcadePause
{
    public FrontierChallenge Way { get; set; }
    public int Battle { get; set; }

    /// <summary><c>ARCADE_SAVE_ROULETTE_SPEED</c> and <c>ARCADE_SAVE_CURSOR_RANDOMIZED</c>: the board as the round's events have left it.</summary>
    public int Speed { get; set; }
    public bool Randomized { get; set; }
    public List<int> Slots { get; set; } = new();
    public List<int> Trainers { get; set; } = new();
    public List<int> FoeSets { get; set; } = new();

    /// <summary><c>ARCADE_SAVE_HELD_ITEMS</c>: what the board has given each of the three to hold, where they still hold it.</summary>
    public List<string?> Items { get; set; } = new();

    /// <summary>The species entered: a challenge put down is only taken up again by them.</summary>
    public List<string> Species { get; set; } = new();
}

/// <summary>What the Battle Arcade keeps beside the stats (the original's <c>BattleArcadeSave</c> and its game record).</summary>
public sealed class ArcadeRecord
{
    public ArcadePause? Paused { get; set; }

    /// <summary><c>RECORD_UNK_067</c>: the Battle Points the game board has given.</summary>
    public int BoardPoints { get; set; }
}

/// <summary>The teams and the conditions of one battle of the Battle Arcade, as the board left them (<c>BattleArcade_SetupBattle</c>).</summary>
/// <param name="Mine">The three the challenger battles with: copies of their own, or of the trainer's where the board swapped the teams.</param>
/// <param name="Theirs">The trainer, with the other three.</param>
/// <param name="Own">The copies of the challenger's own three, whichever side they went to: what they still hold afterwards is theirs to keep.</param>
public sealed record ArcadeBattle(Party Mine, Trainer Theirs, Party Own, FrontierConditions Conditions, bool Swapped);

/// <summary>
/// A Battle Arcade challenge under way (plan 06 · R18d): the original's <c>BattleArcade</c> and the functions that
/// work on it (<c>src/overlay104/battle_arcade.c</c>, <c>battle_arcade_helpers.c</c>, <c>frscrcmd_battle_arcade.c</c>).
/// Three Pokémon go in, brought down to level 50 and with nothing held, and meet seven trainers one after another.
/// Before each battle the game board is played (<see cref="ArcadeBoard"/>) and the event it stops on is carried out:
/// one side's HP lowered, a status given, a berry or an item handed to hold, three levels added; the weather set or a
/// Trick Room; the board itself made faster, slower or random for the battles after; the two teams swapped; Battle
/// Points given; the battle passed; or nothing. The three are whole again for every battle, and all that carries
/// from one to the next is what the board gave them to hold, as the battle left it. Dahlia, the Arcade Star, stands
/// in the way at battles 21 and 49 of a Single streak.
///
/// The original draws on the game's one generator (<c>LCRNG_Next</c>), so every draw here is on the sixteen-bit draw
/// the caller hands over, in the original's order. No drawing or input.
/// </summary>
public sealed class BattleArcadeChallenge
{
    /// <summary><c>ARCADE_BATTLES_PER_ROUND</c>, <c>ARCADE_PARTY_SIZE_SOLO</c>, <c>ARCADE_MAX_DISTINCT_ROUNDS</c>, <c>ARCADE_MAX_LEVEL</c>.</summary>
    public const int BattlesPerRound = 7, TeamSize = 3, DistinctRounds = 8, Level = 50;

    /// <summary>The levels the board adds to a side (<c>ArcadeEffect_LevelUp</c>), and the Battle Points a round earns that ended with the Arcade Star beaten.</summary>
    public const int LevelsRaised = 3, DahliaPoints = 20;

    /// <summary>What a round won adds to the Trainer Card's score (<c>TRAINER_SCORE_EVENT_BATTLE_ARCADE_ROUND_COMPLETED</c>).</summary>
    public const int RoundScore = 7;

    public const string WayVariable = "VAR_BATTLE_ARCADE_CHALLENGE_TYPE";

    public FrontierChallenge Way { get; }

    /// <summary>The battles won (or passed) in this round, 0 to 7 (<c>currentBattle</c>).</summary>
    public int Battle { get; private set; }
    public int Streak { get; private set; }
    public int Round { get; private set; }

    /// <summary>The places in the team of the three entered (<c>partySlots</c>).</summary>
    public List<int> Slots { get; private set; } = new();

    /// <summary>
    /// The three as the challenge carries them (<c>playersParty</c>): copies at level 50 or under, holding only
    /// what the board gave them. They are whole between battles: a battle is fought with copies of these.
    /// </summary>
    public Party Team { get; private set; } = new();

    /// <summary>The round's trainers by their numbers: fourteen, as the original draws them, of whom the first seven are met.</summary>
    public List<int> Trainers { get; private set; } = new();

    /// <summary>The trainer to come with their three (<c>opponentsParty</c>), holding nothing until the board says otherwise.</summary>
    public FrontierOpponent? Foe { get; private set; }

    /// <summary>The cursor's speed, 0 to 7 (<c>rouletteSpeed</c>), and whether it jumps about on the next board (<c>cursorRandomized</c>).</summary>
    public int Speed { get; private set; } = 3;
    public bool Randomized { get; private set; }

    /// <summary>How the last battle went, 0 to 24 (<c>performance</c>): what the next board's panels go by.</summary>
    public int Performance { get; private set; }

    /// <summary>The board being played (<see cref="OpenBoard"/>), and what it gave: the event (<c>activeEffect</c>), its number (<c>randomIndex</c>) and whether everyone it fell on shrugged it off (<c>immuneToEffect</c>).</summary>
    public ArcadeBoard? Board { get; private set; }
    public ArcadeEffect Effect { get; private set; } = ArcadeEffect.None;
    public int RandomIndex { get; private set; }
    public bool Immune { get; private set; }

    /// <summary>What the board gave a side to hold in the event just carried out; null when it gave nothing.</summary>
    public string? Given { get; private set; }

    /// <summary>The weather and the room the battle to come opens under (<c>weather</c>).</summary>
    public FrontierConditions Conditions { get; private set; } = new();

    public bool Resumed { get; private set; }

    /// <summary>The Arcade Star has been announced (<c>seenArcadeStarIntro</c>).</summary>
    public bool StarAnnounced { get; private set; }
    public bool RoundWon { get; private set; }
    public FrontierPrint BeatDahlia { get; private set; }

    // The levels the three went in at: what a side raised by the board goes back to
    private List<int> levels = new();

    private BattleArcadeChallenge(FrontierChallenge way) => Way = way;

    /// <summary><c>BattleFrontierStats_GetArcadeCurrentStreakIndex</c> (the record, for all its name) and <c>…LatestStreakIndex</c>.</summary>
    public static FrontierStat RecordStreakStat(FrontierChallenge way) => (FrontierStat)((int)FrontierStat.ArcadeRecordStreakSingle + (int)way * 2);
    public static FrontierStat LatestStreakStat(FrontierChallenge way) => (FrontierStat)((int)FrontierStat.ArcadeLatestStreakSingle + (int)way * 2);

    // ------------------------------------------------------------------ who may go in

    /// <summary>
    /// <c>BA_LOBBY_FUNC_CHECK_PARTY_ELIGIBLE</c> (<c>sub_02049EC4</c> told not to look at items, since they are taken
    /// at the door): three Pokémon that may enter, no two of one species.
    /// </summary>
    public static bool EnoughToEnter(Party party) =>
        party.Members.Where(FrontierRules.MayEnter).Select(p => p.Species.Name).Distinct().Count() >= TeamSize;

    /// <summary>Whether the three chosen can go in together: each allowed, and no two of one species.</summary>
    public static bool ValidEntry(Party party, IReadOnlyList<int> slots)
    {
        if (slots.Count != TeamSize || slots.Distinct().Count() != TeamSize || slots.Any(s => s < 0 || s >= party.Count)) return false;
        var chosen = slots.Select(s => party.Members[s]).ToList();
        return chosen.All(FrontierRules.MayEnter) && chosen.Select(p => p.Species.Name).Distinct().Count() == TeamSize;
    }

    // ------------------------------------------------------------------ beginning

    /// <summary>
    /// <c>BattleArcade_Init</c> for a new challenge: no battle won yet, and the streak of this way carried on where
    /// it is still running. The three go in as copies, nothing held, brought down to level 50 where they are above it.
    /// </summary>
    public static BattleArcadeChallenge Begin(FrontierRecords records, FrontierChallenge way, Party party, IReadOnlyList<int> slots)
    {
        if (way == FrontierChallenge.Multi) throw new InvalidOperationException("The Battle Arcade's Multi challenge needs another player");
        var challenge = new BattleArcadeChallenge(way) { Slots = slots.ToList() };
        records.Arcade.Paused = null;
        if (records.IsRunning(FrontierFacility.Arcade, (int)way)) challenge.Streak = records.Get(LatestStreakStat(way));
        challenge.Round = challenge.Streak / BattlesPerRound;
        challenge.TakeTeam(party);
        return challenge;
    }

    private void TakeTeam(Party party)
    {
        Team = new Party();
        foreach (int slot in Slots)
        {
            var copy = FrontierRules.ForBattle(party.Members[slot], Level);
            copy.HeldItem = null;
            Team.Add(copy);
        }
        levels = Team.Members.Select(p => p.Level).ToList();
    }

    /// <summary>
    /// <c>BattleArcade_Init</c> and <c>LoadTrainersAndMonsFromSave</c> for a challenge taken up again: the board's
    /// speed and whether its cursor jumps, the three with what they were holding, and the round's trainers. The
    /// trainer just beaten, whose three the original makes once more here from the sets it kept, is left alone: the
    /// next one is drawn as the challenge goes on. How the last battle went isn't kept, as in the original, so the
    /// first board after a rest is laid out as after a battle that went badly.
    /// </summary>
    public static BattleArcadeChallenge? Resume(FrontierRecords records, Party party)
    {
        if (records.Arcade.Paused is not { } paused) return null;
        var challenge = new BattleArcadeChallenge(paused.Way)
        {
            Battle = paused.Battle,
            Speed = Math.Clamp(paused.Speed, 0, ArcadeBoard.FastestSpeed),
            Randomized = paused.Randomized,
            Streak = records.Get(LatestStreakStat(paused.Way)),
            Slots = paused.Slots.ToList(),
            Trainers = paused.Trainers.ToList(),
            Resumed = true
        };
        challenge.Round = challenge.Streak / BattlesPerRound;
        challenge.TakeTeam(party);
        for (int i = 0; i < challenge.Team.Count && i < paused.Items.Count; i++)
            challenge.Team.Members[i].HeldItem = paused.Items[i] is { } item ? ItemDatabase.Get(item) : null;
        records.Arcade.Paused = null;
        return challenge;
    }

    /// <summary>A challenge put down can only be taken up by the three it was entered with.</summary>
    public static bool StillValid(FrontierRecords records, Party party) =>
        records.Arcade.Paused is { } paused && ValidEntry(party, paused.Slots)
        && paused.Slots.Select(s => party.Members[s].Species.Name).SequenceEqual(paused.Species);

    /// <summary>
    /// <c>SelectTrainersAndFirstBattlesMons</c>, as a round begins: the round's fourteen trainers are drawn, the
    /// first of them is given their three, and the cursor's speed is the round's (the fourth step from the first
    /// round, the fifth from the fifth, the sixth from the eighth).
    /// </summary>
    public void DrawRound(Func<int> next16)
    {
        Trainers = PickTrainers(Way, Round, BattlesPerRound * 2, next16);
        DrawFoe(next16);
        foreach (int[] from in FrontierDatabase.Arcade.SpeedByRound)
            if (Round >= from[0])
            {
                Speed = from[1];
                break;
            }
    }

    /// <summary>
    /// <c>BattleArcade_GetOpponentTrainerID</c>: who stands at a place of a round. In a Single challenge the battle
    /// that would be the streak's 21st is the Arcade Star's and the 49th hers again; otherwise the trainer is drawn
    /// from the round's row, the seventh of each seven from its second half. The row's last trainer is never drawn.
    /// </summary>
    public static int TrainerFor(FrontierChallenge way, int round, int place, Func<int> next16)
    {
        var rows = FrontierDatabase.Arcade.Opponents;
        if (way == FrontierChallenge.Single)
        {
            var (silverAt, goldAt) = FrontierBrains.Battles(FrontierFacility.Arcade);
            var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Arcade);
            int streak = round * BattlesPerRound + place + 1;
            if (streak == silverAt) return silver;
            if (streak == goldAt) return gold;
        }
        int[] row = rows[Math.Min(round, rows.Count - 1)];
        bool last = place == BattlesPerRound - 1 || place == BattlesPerRound * 2 - 1;
        int min = last ? row[2] : row[0], span = last ? row[3] - row[2] : row[1] - row[0];
        return min + (next16() & 0xFFFF) % span;
    }

    /// <summary><c>BattleArcade_PickOpponentTrainers</c>: a round's trainers, none twice.</summary>
    public static List<int> PickTrainers(FrontierChallenge way, int round, int count, Func<int> next16)
    {
        var trainers = new List<int>();
        while (trainers.Count < count)
        {
            int trainer = TrainerFor(way, round, trainers.Count, next16);
            if (!trainers.Contains(trainer)) trainers.Add(trainer);
        }
        return trainers;
    }

    /// <summary>
    /// <c>BattleArcade_SetupNextOpponentsParty</c>: the trainer of the battle to come is given their three, by the
    /// draw every facility but the Tower shares (<see cref="FrontierTrainers.ForTrainers"/>), at level 50, and what
    /// their sets would have them hold is taken away (<c>BattleArcade_SetupOpponentsParty</c>).
    /// </summary>
    public void DrawFoe(Func<int> next16)
    {
        Foe = FrontierTrainers.ForTrainers(TeamSize, Trainers[Battle], Trainers[Battle + BattlesPerRound], false, next16, Level)[0];
        foreach (var pokemon in Foe.Pokemon) pokemon.HeldItem = null;
        Resumed = false;
    }

    private FrontierOpponent Opposing => Foe ?? throw new InvalidOperationException("No trainer has been drawn yet");

    // ------------------------------------------------------------------ the game board

    /// <summary>
    /// The board is brought up for the battle to come (<c>SetupBattleArcadeAppArgs</c>, <c>BattleArcadeApp_Init</c>):
    /// the weather is cleared, a cursor made random by the last board jumps on this one and no further, and the
    /// panels are laid out by the round, the battle and how the last battle went.
    /// </summary>
    public ArcadeBoard OpenBoard(Func<int> next16)
    {
        _ = Opposing;
        Conditions = new FrontierConditions();
        bool jumping = Randomized;
        Randomized = false;
        Immune = false;
        Given = null;
        return Board = new ArcadeBoard(Round, Streak, Performance, Speed, jumping, next16);
    }

    /// <summary>
    /// The board has stopped (<c>StoreBattleArcadeAppResults</c>, <c>FrontierScrCmd_ApplyArcadeEffect</c>): its event
    /// is the battle's, and is carried out at once on whichever side it falls on.
    /// </summary>
    public ArcadeEffect TakeBoard(Func<int> next16)
    {
        var board = Board ?? throw new InvalidOperationException("The game board hasn't been played");
        if (!board.IsDone) board.PlayTo();
        return Apply(board.Effect, board.RandomIndex, next16);
    }

    /// <summary>
    /// <c>BattleArcade_ApplyEffect</c>: an event of the board carried out. The first nine fall on the trainer's
    /// three and the next nine on the challenger's: HP down to four fifths (<c>ArcadeEffect_LowerHP</c>, in the
    /// original's own arithmetic); poison, paralysis or a burn for everyone but those a type or an ability keeps
    /// from it (Poison and Steel types and Immunity; Ground types and Limber; Fire types and Water Veil); sleep or
    /// ice for one, the first from a place the board's number names whom nothing keeps from it (Insomnia and Vital
    /// Spirit; Ice types and Magma Armor), sleep for two to five turns on a draw of its own; one berry or one item
    /// for all three, by the round and the board's number; three levels. The rest fall on nobody's Pokémon: the
    /// weather or a Trick Room for the battle, the cursor a step faster or slower from now on, or random on the next
    /// board; and the bonuses, which the challenge's course carries out (<see cref="ForBattle"/>, <see cref="FreePoints"/>,
    /// <see cref="Skipped"/>).
    /// </summary>
    public ArcadeEffect Apply(ArcadeEffect effect, int randomIndex, Func<int> next16)
    {
        Effect = effect;
        RandomIndex = randomIndex;
        Immune = false;
        Given = null;
        var side = ArcadeBoard.CategoryOf(effect) == ArcadeCategory.Foe ? Opposing.Pokemon : (IReadOnlyList<Pokemon>)Team.Members;
        switch (effect)
        {
            case ArcadeEffect.LowerFoeHp or ArcadeEffect.LowerAllyHp:
                foreach (var pokemon in side) pokemon.CurrentHP = LoweredHp(pokemon.MaxHP);
                break;
            case ArcadeEffect.PoisonFoe or ArcadeEffect.PoisonAlly:
                Afflict(side, StatusCondition.Poison, p => p.HasType(PokemonType.Poison) || p.HasType(PokemonType.Steel) || p.AbilityName == "Immunity");
                break;
            case ArcadeEffect.ParalyzeFoe or ArcadeEffect.ParalyzeAlly:
                Afflict(side, StatusCondition.Paralyze, p => p.HasType(PokemonType.Ground) || p.AbilityName == "Limber");
                break;
            case ArcadeEffect.BurnFoe or ArcadeEffect.BurnAlly:
                Afflict(side, StatusCondition.Burn, p => p.HasType(PokemonType.Fire) || p.AbilityName == "Water Veil");
                break;
            case ArcadeEffect.SleepFoe or ArcadeEffect.SleepAlly:
                if (AfflictOne(side, p => p.AbilityName is "Insomnia" or "Vital Spirit") is { } sleeper)
                {
                    sleeper.Status = StatusCondition.Sleep;
                    sleeper.SleepTurns = (next16() & 0xFFFF) % 4 + 2;
                }
                break;
            case ArcadeEffect.FreezeFoe or ArcadeEffect.FreezeAlly:
                if (AfflictOne(side, p => p.HasType(PokemonType.Ice) || p.AbilityName == "Magma Armor") is { } frozen) frozen.Status = StatusCondition.Freeze;
                break;
            case ArcadeEffect.FoeGetBerry or ArcadeEffect.AllyGetBerry:
                Give(side, FrontierDatabase.Arcade.Berries);
                break;
            case ArcadeEffect.FoeGetItem or ArcadeEffect.AllyGetItem:
                Give(side, FrontierDatabase.Arcade.Items);
                break;
            case ArcadeEffect.FoeLevelUp or ArcadeEffect.AllyLevelUp:
                foreach (var pokemon in side) SetLevel(pokemon, Math.Min(100, pokemon.Level + LevelsRaised));
                break;
            case ArcadeEffect.Sunny:
                Conditions = new FrontierConditions(BattleWeather.Sun);
                break;
            case ArcadeEffect.Rainy:
                Conditions = new FrontierConditions(BattleWeather.Rain);
                break;
            case ArcadeEffect.Sandy:
                Conditions = new FrontierConditions(BattleWeather.Sandstorm);
                break;
            case ArcadeEffect.Hail:
                Conditions = new FrontierConditions(BattleWeather.Hail);
                break;
            case ArcadeEffect.Foggy:
                Conditions = new FrontierConditions(BattleWeather.Fog);
                break;
            case ArcadeEffect.TrickRoom:
                Conditions = new FrontierConditions(TrickRoom: true);
                break;
            case ArcadeEffect.SpeedUp:
                if (Speed < ArcadeBoard.FastestSpeed) Speed++;
                break;
            case ArcadeEffect.SlowDown:
                if (Speed > 0) Speed--;
                break;
            case ArcadeEffect.RandomizeCursor:
                Randomized = true;
                break;
        }
        return effect;
    }

    /// <summary><c>ArcadeEffect_LowerHP</c>: a fifth of the most, worked out as the original does (the most times 1.2, cut to a whole number, less the most), comes off.</summary>
    public static int LoweredHp(int max)
    {
        uint hp = (uint)(max * 1.2);
        hp -= (uint)max;
        return (int)((uint)max - hp);
    }

    // Everyone on a side: those kept from it are counted, and the event came to nothing if that was all of them
    private void Afflict(IReadOnlyList<Pokemon> side, StatusCondition status, Func<Pokemon, bool> keptFrom)
    {
        int immune = 0;
        foreach (var pokemon in side)
        {
            if (keptFrom(pokemon)) immune++;
            else SetStatus(pokemon, status);
        }
        if (immune >= side.Count) Immune = true;
    }

    // One of a side: from the place the board's number names, the first that nothing keeps from it; null when something keeps them all
    private Pokemon? AfflictOne(IReadOnlyList<Pokemon> side, Func<Pokemon, bool> keptFrom)
    {
        int slot = RandomIndex % side.Count;
        for (int i = 0; i < side.Count; i++)
        {
            if (!keptFrom(side[slot])) return side[slot];
            if (++slot >= side.Count) slot = 0;
        }
        Immune = true;
        return null;
    }

    private static void SetStatus(Pokemon pokemon, StatusCondition status)
    {
        pokemon.Status = status;
        pokemon.SleepTurns = 0;
        pokemon.ToxicCounter = 0;
    }

    // ArcadeEffect_GetBerry, ArcadeEffect_GetItem: the first list for the first three rounds, the second for the next three, the third after
    private void Give(IReadOnlyList<Pokemon> side, List<List<string>> lists)
    {
        var list = lists[Round < 3 ? 0 : Round < 6 ? 1 : 2];
        Given = list[RandomIndex % list.Count];
        var item = ItemDatabase.Get(Given);
        foreach (var pokemon in side) pokemon.HeldItem = item;
    }

    private static void SetLevel(Pokemon pokemon, int level)
    {
        if (pokemon.Level == level) return;
        pokemon.LevelTo(level);
        pokemon.CurrentExp = Pokemon.GetExpForLevel(level, pokemon.Species.GrowthRate);
        pokemon.RecalculateStats();
        pokemon.CurrentHP = pokemon.MaxHP;
    }

    /// <summary><c>BA_FUNC_GET_FREE_BP</c>: the Battle Points the event just drawn gives outright (one or three, whatever the round); none for any other.</summary>
    public int FreePoints => Effect switch { ArcadeEffect.Get1Bp => 1, ArcadeEffect.Get3Bp => 3, _ => 0 };

    /// <summary>The Battle Points of the event are handed over (<c>GiveBattlePoints</c>, <c>RECORD_UNK_067</c>).</summary>
    public int GiveFreePoints(FrontierRecords records)
    {
        int points = FreePoints;
        if (points <= 0) return 0;
        records.GivePoints(points);
        records.Arcade.BoardPoints = Math.Min(999_999, records.Arcade.BoardPoints + points);
        return points;
    }

    // ------------------------------------------------------------------ battle by battle

    /// <summary>Whether the battle about to be fought is the Arcade Star's (<c>BA_FUNC_GET_NEXT_BATTLE_TYPE</c>): a Single streak of 20 or of 48.</summary>
    public FrontierPrint DahliaNext => Way == FrontierChallenge.Single ? FrontierBrains.NextBattle(FrontierFacility.Arcade, Streak) : FrontierPrint.None;

    /// <summary><c>BA_FUNC_CHECK_SEEN_STAR_INTRO</c>: whether the Arcade Star had been announced already; she has been now.</summary>
    public bool AnnounceStar()
    {
        bool before = StarAnnounced;
        StarAnnounced = true;
        return before;
    }

    /// <summary>
    /// <c>BattleArcade_GetAIMask</c>: the trainers of the first two rounds choose their moves at random, those of the
    /// third and fourth with the basic routine alone, and everyone after, like the Arcade Star, with all three.
    /// </summary>
    public AiFlags Mind
    {
        get
        {
            var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Arcade);
            if (Way == FrontierChallenge.Single && Battle < Trainers.Count && (Trainers[Battle] == silver || Trainers[Battle] == gold)) return FrontierTrainers.Mind;
            return Round switch
            {
                0 or 1 => AiFlags.None,
                2 or 3 => AiFlags.Basic,
                _ => FrontierTrainers.Mind
            };
        }
    }

    /// <summary>
    /// <c>BattleArcade_SetupBattle</c>: the battle to come is given copies of both threes as the board left them,
    /// each to the other's side where the board swapped the teams, with the weather or the room the board set; and
    /// both threes themselves are made whole again (<c>Party_HealAllMembers</c>), so that nothing of the event but
    /// what is held outlasts the battle.
    /// </summary>
    public ArcadeBattle ForBattle()
    {
        var foe = Opposing;
        bool swapped = Effect == ArcadeEffect.SwapMons;
        var own = new Party();
        foreach (var pokemon in Team.Members) own.Add(pokemon.Clone());
        var theirs = new Party();
        foreach (var pokemon in foe.Pokemon) theirs.Add(pokemon.Clone());
        var template = foe.AsTrainer(Mind);
        var trainer = new Trainer
        {
            Id = template.Id,
            Name = template.Name,
            TrainerClass = template.TrainerClass,
            PrizeMoney = 0,
            Ai = template.Ai,
            Look = template.Look,
            Party = swapped ? own : theirs
        };
        foreach (var pokemon in Team.Members) pokemon.HealFull();
        foreach (var pokemon in foe.Pokemon) pokemon.HealFull();
        return new ArcadeBattle(swapped ? theirs : own, trainer, own, Conditions, swapped);
    }

    /// <summary>
    /// <c>BattleArcade_GetPerformance</c>: how a battle went, by the three the challenger fought it with: eight for
    /// none of them left with a status, six for one and four for two; six for none fainted, four for one and two for
    /// two; and ten for a battle over in under three turns, six in under five, four in under seven, two in under nine.
    /// </summary>
    public static int PerformanceOf(Party fought, int turns)
    {
        var tables = FrontierDatabase.Arcade;
        int fainted = fought.Members.Count(p => p.CurrentHP == 0), ailing = fought.Members.Count(p => p.Status != StatusCondition.None);
        int score = tables.ScoreByStatus[Math.Min(ailing, tables.ScoreByStatus.Count - 1)] + tables.ScoreByFainted[Math.Min(fainted, tables.ScoreByFainted.Count - 1)];
        foreach (int[] under in tables.ScoreByTurns)
            if (turns < under[0])
            {
                score += under[1];
                break;
            }
        return score;
    }

    /// <summary>
    /// <c>FrontierScrCmd_BattleArcade_CleanupBattle</c>: how the battle went is noted for the next board, what the
    /// challenger's own three still hold is theirs to keep (theirs even where the trainer battled with them), and
    /// three levels the board added come off again.
    /// </summary>
    public void AfterBattle(ArcadeBattle fought, int turns)
    {
        Performance = PerformanceOf(fought.Mine, turns);
        for (int i = 0; i < Team.Count && i < fought.Own.Count; i++) Team.Members[i].HeldItem = fought.Own.Members[i].HeldItem;
        if (Effect == ArcadeEffect.AllyLevelUp)
            for (int i = 0; i < Team.Count && i < levels.Count; i++) SetLevel(Team.Members[i], levels[i]);
    }

    /// <summary>
    /// After a battle won (<c>RECORD_BATTLE_ARCADE_VICTORIES</c>, <c>BA_FUNC_INCREMENT_CURRENT_BATTLE</c>,
    /// <c>…_STREAK</c>): the win is counted for the round, the streak and the battles won here.
    /// </summary>
    public void Won(FrontierRecords records)
    {
        records.CountVictories(FrontierFacility.Arcade, 1);
        Count();
    }

    /// <summary>The board passed the battle (<c>ARCADE_EFFECT_NO_BATTLE</c>, <c>_1FFC</c>): it counts for the round and for the streak as one won, but not for the battles won here.</summary>
    public void Skipped() => Count();

    private void Count()
    {
        Battle++;
        if (Streak < FrontierRecords.Most) Streak++;
    }

    public bool RoundDone => Battle >= BattlesPerRound;

    /// <summary>The Arcade Star was beaten (the battle room's script): the print waits in the lobby, the first time for each.</summary>
    public void BeatStar(StoryState story, FrontierPrint which)
    {
        BeatDahlia = which;
        FrontierBrains.Beaten(story, FrontierFacility.Arcade, which);
    }

    // ------------------------------------------------------------------ the end of a round, and of a challenge

    // BattleArcade_Save: the latest streak always; the record and whether the streak runs on unless the challenge is only being put down
    private void Save(FrontierRecords records, bool pausing)
    {
        records.Set(LatestStreakStat(Way), Streak);
        if (pausing) return;
        records.SetIfBetter(RecordStreakStat(Way), Streak);
        records.SetRunning(FrontierFacility.Arcade, (int)Way, RoundWon);
    }

    /// <summary><c>BattleArcade_SaveOnCompletingRound</c>: the streak runs on, and the next round is one further (up to the eighth).</summary>
    public void EndRound(FrontierRecords records)
    {
        RoundWon = true;
        if (Round < DistinctRounds) Round++;
        Battle = 0;
        Save(records, pausing: false);
    }

    /// <summary><c>BattleArcade_SaveOnLoss</c>: the streak ends where it stands.</summary>
    public void EndLost(FrontierRecords records)
    {
        RoundWon = false;
        Save(records, pausing: false);
    }

    /// <summary><c>BattleArcade_GetEarnedBP</c>, after <see cref="EndRound"/>: by the round the streak goes on to, and twenty for a Single round that ended with the Arcade Star beaten.</summary>
    public int PointsEarned
    {
        get
        {
            var (silverAt, goldAt) = FrontierBrains.Battles(FrontierFacility.Arcade);
            if (Way == FrontierChallenge.Single && (Streak == silverAt || Streak == goldAt)) return DahliaPoints;
            var byRound = FrontierDatabase.Arcade.PointsByRound;
            return byRound[Math.Min(Round, byRound.Count - 1)];
        }
    }

    /// <summary><c>BA_FUNC_QUICKSAVE</c>: the challenge is put down between two battles, with the board as it stands and what the three hold.</summary>
    public void Pause(FrontierRecords records, Party party)
    {
        Save(records, pausing: true);
        records.Arcade.Paused = new ArcadePause
        {
            Way = Way,
            Battle = Battle,
            Speed = Speed,
            Randomized = Randomized,
            Slots = Slots.ToList(),
            Trainers = Trainers.ToList(),
            FoeSets = Foe?.Sets.ToList() ?? new List<int>(),
            Items = Team.Members.Select(p => p.HeldItem?.Name).ToList(),
            Species = Slots.Where(s => s >= 0 && s < party.Count).Select(s => party.Members[s].Species.Name).ToList()
        };
    }

    /// <summary>
    /// <c>BattleArcade_ClearActiveStreak</c>: a challenge was left without being put down. The streak of its way is
    /// over: no longer running, and its latest streak nought.
    /// </summary>
    public static void Forfeit(FrontierRecords records, FrontierChallenge way)
    {
        records.SetRunning(FrontierFacility.Arcade, (int)way, false);
        records.Set(LatestStreakStat(way), 0);
        records.Arcade.Paused = null;
    }
}
