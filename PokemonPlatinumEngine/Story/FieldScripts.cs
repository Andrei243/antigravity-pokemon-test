using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// Which script the field starts for what the player does in it (plan 02 · S1). What a nurse, a clerk, a PC, the
/// professor's briefcase, a trainer and a signboard do is written once, in <c>Data/scripts/common.txt</c>; a
/// person or a sign with a script of their own runs that instead.
/// </summary>
public static class FieldScripts
{
    public const string Nurse = "common.Nurse";
    public const string Clerk = "common.Clerk";
    public const string Pc = "common.PC";
    public const string Briefcase = "common.Briefcase";
    public const string Attendant = "common.Attendant";
    public const string Trainer = "common.Trainer";

    /// <summary>Two trainers who saw the player at once and came together (plan 02 · S6); <c>pair</c> is the second.</summary>
    public const string TrainerPair = "common.TrainerPair";
    public const string Talk = "common.Talk";
    public const string Sign = "common.Sign";
    public const string ItemBall = "common.ItemBall";
    public const string HiddenItem = "common.HiddenItem";

    /// <summary>A Pokémon standing in the field (plan 10 · F1): its cry, then its lines.</summary>
    public const string Pokemon = "common.Pokemon";

    // Field moves (plan 02 · S2): what an obstacle, the water, a waterfall and a rock face run when faced
    public const string CutTree = "common.CutTree";
    public const string Rock = "common.Rock";
    public const string Boulder = "common.Boulder";
    public const string Water = "common.Water";
    public const string Waterfall = "common.Waterfall";
    public const string RockFace = "common.RockFace";

    /// <summary>An Escape Rope used from the bag, and the Cycling Road's gate keepers turning back anyone not riding.</summary>
    public const string EscapeRope = "common.EscapeRope";
    public const string CyclistsOnly = "common.CyclistsOnly";

    /// <summary>Waking up after a lost battle (plan 06 · R10): the nurse of the last Pokémon Center, or Mom at home.</summary>
    public const string BlackOutCenter = "common.BlackOutCenter";
    public const string BlackOutHome = "common.BlackOutHome";

    /// <summary>The end of a Safari Game (plan 01 · M7): its last step taken, or its last ball thrown.</summary>
    public const string SafariTimeUp = "common.SafariTimeUp";

    /// <summary>A Repel's last step (plan 06 · R11).</summary>
    public const string RepelWoreOff = "common.RepelWoreOff";

    /// <summary>
    /// Wild Pokémon (plan 06 · R13): a honey tree faced from the south, Honey used from the bag, a Pokémon that came
    /// through poison in the field, and the day's swarm as the assistant's sister tells of it.
    /// </summary>
    public const string HoneyTree = "common.HoneyTree";
    public const string UseHoney = "common.UseHoney";
    public const string PoisonSurvived = "common.PoisonSurvived";
    public const string SwarmNews = "common.SwarmNews";
    public const string SafariOutOfBalls = "common.SafariOutOfBalls";

    /// <summary>
    /// Berries (plan 06 · R14a; the original's berry tree script and its entries 1 to 3): soft soil spoken to, a
    /// berry planted, mulch laid and the Sprayduck used from the bag on the soil faced.
    /// </summary>
    public const string BerryPatch = "common.BerryPatch";
    public const string PlantBerry = "common.PlantBerry";
    public const string UseMulch = "common.UseMulch";
    public const string UseSprayduck = "common.UseSprayduck";

    /// <summary>Jubilife TV's lottery corner (plan 06 · R14a): its clerk runs this, once the TV's rooms are built (plan 01 · M11).</summary>
    public const string Lottery = "common.Lottery";

    /// <summary>
    /// The script a field move chosen from the party menu runs (<c>common.UseCut</c>…); null for Milk Drink and
    /// Soft-Boiled, which the party menu carries out itself, and Chatter, which has nothing to do here.
    /// </summary>
    public static string? FromMenu(FieldMove move) => move switch
    {
        FieldMove.MilkDrink or FieldMove.Softboiled or FieldMove.Chatter => null,
        _ => "common.Use" + move
    };

    /// <summary>
    /// The script talking to someone runs: their own, or the common one for what they are. Null for someone with
    /// nothing to say or do.
    /// </summary>
    public static string? For(NPC npc)
    {
        if (!string.IsNullOrEmpty(npc.Script)) return npc.Script;
        if (npc.IsItemBall) return ItemBall;
        if (npc.IsBerryPatch) return BerryPatch;
        if (npc.IsPokemon) return Pokemon;
        if (npc.Obstacle is { } obstacle)
            return obstacle switch { PropType.CutTree => CutTree, PropType.CrackedRock => Rock, _ => Boulder };
        if (npc.IsStarterBriefcase) return Briefcase;
        if (npc.IsHealingNurse) return Nurse;
        if (npc.IsPokeMartClerk) return Clerk;
        if (npc.IsPCTerminal) return Pc;
        if (npc.IsTransportAttendant) return Attendant;
        if (npc.IsTrainer && npc.TrainerData != null) return Trainer;
        return npc.DialogLines.Count > 0 ? Talk : null;
    }

    /// <summary>What is hidden at a tile and hasn't been found yet, or null.</summary>
    public static HiddenItem? HiddenAt(Map map, int x, int y, StoryState story) =>
        map.HiddenItems.TryGetValue((x, y), out var hidden) && !story.Has(hidden.Flag) ? hidden : null;

    /// <summary>The first trigger under a tile whose state of the story has come: the one a step there starts.</summary>
    public static StepTrigger? TriggerAt(Map map, int x, int y, StoryState story)
    {
        foreach (var trigger in map.TriggersAt(x, y))
            if (trigger.Variable == null || story.Var(trigger.Variable) == trigger.Value) return trigger;
        return null;
    }
}
