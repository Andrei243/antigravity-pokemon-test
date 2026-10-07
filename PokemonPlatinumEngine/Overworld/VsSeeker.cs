using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>What using the Vs. Seeker came to.</summary>
public enum VsSeekerResult
{
    /// <summary>Its battery isn't full yet.</summary>
    NotCharged,
    /// <summary>No trainer is in range, or it is used where it can't be (indoors, in a cave).</summary>
    NoTrainers,
    /// <summary>It was used: some trainers want a rematch, or none do.</summary>
    Used
}

/// <summary>
/// The Vs. Seeker and rematches (plan 06 · R12; the original's <c>vs_seeker.c</c>). Its battery charges a step at a
/// time while it is in the bag, to a hundred; used on the open map with a trainer about, it empties, and every
/// trainer the player has beaten within about a screen (seven tiles each way, six below) wants a rematch one time
/// in two: they spin where they stand until spoken to. The trainers not yet beaten are shown with a "!" instead.
/// A hundred steps, or going anywhere else, and those still waiting give up. Which team a rematch brings is the
/// first level of the trainer's row (<see cref="TrainerRecord.Rematches"/>) not yet beaten, held back to the
/// highest level the story has unlocked. No drawing or input.
/// </summary>
public static class VsSeeker
{
    public const string Item = "Vs. Seeker";
    public const string Battery = "VAR_VS_SEEKER_BATTERY_LEVEL", StepCount = "VAR_VS_SEEKER_STEP_COUNT", Used = "FLAG_VS_SEEKER_USED";
    public const int FullBattery = 100, ActiveSteps = 100, Chance = 50;

    /// <summary>How far it reaches from the player: <c>VS_SEEKER_SEARCH_RADIUS_LEFT/RIGHT/UP/DOWN</c>.</summary>
    public const int Left = 7, Right = 7, Up = 7, Down = 6;

    /// <summary>The flags that unlock each level of rematches (<c>FLAG_UNLOCKED_VS_SEEKER_LVL_1</c> to 5).</summary>
    public static string LevelFlag(int level) => $"FLAG_UNLOCKED_VS_SEEKER_LVL_{level}";

    /// <summary>
    /// A step taken (<c>VsSeeker_UpdateStepCount</c>): the battery charges while the Vs. Seeker is in the bag, and
    /// after it is used the steps are counted. True when the hundredth step ends the rematches it found.
    /// </summary>
    public static bool Step(StoryState story, bool inBag)
    {
        if (inBag && story.Var(Battery) < FullBattery) story.SetVar(Battery, story.Var(Battery) + 1);
        if (!story.Has(Used)) return false;
        story.SetVar(StepCount, story.Var(StepCount) + 1);
        if (story.Var(StepCount) < ActiveSteps) return false;
        Reset(story);
        return true;
    }

    /// <summary>The rematches found are forgotten (<c>SystemVars_ResetVsSeeker</c>): on its hundredth step and on any change of map.</summary>
    public static void Reset(StoryState story)
    {
        story.Unset(Used);
        story.SetVar(StepCount, 0);
    }

    /// <summary>The trainers it reaches: those of the map standing in the box round the player.</summary>
    public static IEnumerable<NPC> InRange(Map map, int x, int y) =>
        map.NPCs.Where(n => n.IsTrainer && n.TrainerData != null
            && n.GridX >= x - Left && n.GridX <= x + Right && n.GridY >= Math.Max(0, y - Up) && n.GridY <= y + Down);

    /// <summary>
    /// Uses it (<c>VsSeeker_ExecuteTask</c>): only on the open map (<paramref name="outdoors"/>), with a full battery
    /// and a trainer in range. The battery empties, and each trainer beaten wants a rematch on the original's roll;
    /// a trainer of a pair wants it with the other. <paramref name="ready"/> are those now waiting, <paramref name="notYet"/>
    /// those still to be beaten for the first time.
    /// </summary>
    public static VsSeekerResult Use(Map map, int x, int y, bool outdoors, StoryState story, Random rng, out List<NPC> ready, out List<NPC> notYet)
    {
        ready = new List<NPC>();
        notYet = new List<NPC>();
        if (story.Var(Battery) < FullBattery) return VsSeekerResult.NotCharged;
        var near = outdoors ? InRange(map, x, y).ToList() : new List<NPC>();
        if (near.Count == 0) return VsSeekerResult.NoTrainers;

        story.SetVar(Battery, 0);
        foreach (var trainer in near)
        {
            string id = trainer.TrainerData!.Id;
            if (!story.HasDefeated(id) && !trainer.HasBattled)
            {
                notYet.Add(trainer);
                continue;
            }
            // The roll is drawn even for one spinning already
            if (rng.Next(100) >= Chance || trainer.ReadyForRematch) continue;
            trainer.ReadyForRematch = true;
            ready.Add(trainer);
            foreach (var partner in map.NPCs.Where(n => n != trainer && n.TrainerData?.Id == id))
            {
                partner.ReadyForRematch = true;
                ready.Add(partner);
            }
        }
        story.Set(Used);
        story.SetVar(StepCount, 0);
        return VsSeekerResult.Used;
    }

    /// <summary>
    /// The team a trainer brings to a rematch (<c>VsSeeker_GetRematchTrainerID</c>): the first level of its row that
    /// has a team of its own not beaten yet (the last level that has one once every one is beaten), held back to the
    /// highest level whose flag is set, falling back past the levels with no team of their own to the trainer's own
    /// first team. Null for a trainer with no row.
    /// </summary>
    public static string? RematchTeam(TrainerRecord trainer, StoryState story)
    {
        if (trainer.Rematches is not { Count: > 0 } row) return null;
        // The first level (1 to 5) not beaten, or the last one there is
        int level = 0;
        for (int i = 0; i < row.Count; i++)
        {
            if (row[i] is not { } id) continue;
            level = i + 1;
            if (!story.HasDefeated(id)) break;
        }
        if (level == 0) return trainer.Id;
        // Not unlocked yet: the highest level below it with a team, or the trainer's own
        if (!story.Has(LevelFlag(level)))
        {
            level--;
            while (level > 0 && row[level - 1] == null) level--;
        }
        return level == 0 ? trainer.Id : row[level - 1];
    }
}
