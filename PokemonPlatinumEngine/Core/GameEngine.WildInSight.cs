using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Wild Pokémon in sight as the game plays them (plan 10 · F5): with the options' WILD POKéMON row at VISIBLE the
/// rules of <see cref="FieldPokemon"/> stand them in the grass and on the water round the player, as things of the
/// map, and one walked into, or faced and spoken to, is met at once, with the Pokémon seen. With the row at HIDDEN
/// nothing here runs and nothing is drawn or rolled: Platinum's grass.
/// </summary>
public partial class GameEngine
{
    private readonly FieldPokemon wildInSight = new();

    // The battle about to be made is with a wild Pokémon walked into from behind (BattleConditions.Surprised)
    private bool wildSurprised;

    /// <summary>Those standing in sight now, for a tool (the harness, the tests).</summary>
    internal FieldPokemon WildInSight => wildInSight;

    /// <summary>Whether wild Pokémon stand in sight here: the row is VISIBLE, outdoors or in a cave, never below ground nor in the Bug-Catching Contest's park.</summary>
    private bool WildSeenHere => Settings.WildInSight && !IsUnderground && !InBugContest && !flow.Map.IsIndoors;

    /// <summary>
    /// A frame of the field: those in sight come, wander and go, and one that walks into the player, or that the
    /// player walks into, is met. True when a battle begins.
    /// </summary>
    private bool KeepWildInSight(float dt)
    {
        flow.Player.WildInSight = WildSeenHere;
        if (!WildSeenHere)
        {
            if (wildInSight.All.Count > 0) wildInSight.Clear(PutWild);
            return false;
        }
        var touched = wildInSight.Update(dt, flow.Map, flow.Player.GridX, flow.Player.GridY, fieldRandom,
            (x, y, water) => flow.Map.DrawOutWild(x, y, water, flow.Player.Lead, EncounterMomentNow()),
            entry => MakeSeenWild(entry, flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)), PutWild,
            // Ten stand in the grass of a place with an outbreak today (plan 38 · EX1)
            OutbreakHere() != null ? FieldPokemon.MostInOutbreak : FieldPokemon.MostInView);
        // Nothing is met while a script, a rod or a fade has the field
        if (flow.Runner.IsRunning || flow.Fishing != null || flow.State != GameState.Overworld) return false;
        if (touched != null) return MeetSeen(touched);
        // The player walked into one ahead of them
        if (flow.Player.PushedAgainst)
        {
            var (dx, dy) = FieldMovement.Delta(flow.Player.Facing);
            if (flow.Map.GetNpcAt(flow.Player.GridX + dx, flow.Player.GridY + dy) is { } ahead && wildInSight.Of(ahead) is { } seen) return MeetSeen(seen);
        }
        return false;
    }

    /// <summary>A body of one in sight onto its map, or off it.</summary>
    private void PutWild(NPC body, bool on)
    {
        if (on)
        {
            // A Pokémon of the water floats a fifth under it, as the surfer's mount does (style guide, "Wild Pokémon in sight")
            if (TileBehaviors.IsSurfable(flow.Map.BehaviourAt(body.GridX, body.GridY)) && body.Species is { } look)
                body.Wading = (int)System.MathF.Round(FieldSprites.FieldHeight(FieldSprites.MetresOf(look)) * CharacterSprites.TexelsPerUnit * 0.2f);
            flow.Map.NPCs.Add(body);
            if (!FieldSprites.Ready(body.Species!)) flow.WantModel(body.Species!);
        }
        else (wildInSight.Map ?? flow.Map).NPCs.Remove(body);
    }

    /// <summary>One in sight faced and spoken to is met, as walking into it is (plan 10 · F5).</summary>
    private bool TouchWild(NPC npc) => wildInSight.Of(npc) is { } seen && !flow.Runner.IsRunning && MeetSeen(seen);

    /// <summary>
    /// The one seen is met, with the Pokémon seen: in the Great Marsh's game a Safari battle, under the Nuzlocke the
    /// place's chance, and from behind a back strike under the later rules.
    /// </summary>
    private bool MeetSeen(FieldPokemon.Wild seen)
    {
        if (!flow.Party.HasUsablePokemon) return false;
        wildInSight.Remove(seen, PutWild);
        flow.FieldEncounter = true;
        flow.Player.Encounters.Reset();
        wildSurprised = FieldPokemon.FromBehind(flow.Player.Facing, seen.Body.Facing);
        // Its end goes on the catch streak or breaks it (plan 38 · EX3)
        metInSight = true;
        MeetWildPokemon(seen.Pokemon, flow.Safari.Active ? BattleKind.Safari : BattleKind.Normal, nuzlockeCatch: MeetUnderTheRun(seen.Pokemon));
        return true;
    }
}
