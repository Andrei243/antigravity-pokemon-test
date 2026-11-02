using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The lead out of its ball (plan 10 · F3 and F4): with the options' FOLLOWER row on, the team's first Pokémon that
/// can stand walks behind the player as the game's one <see cref="Follower"/> (<c>along</c>), dressed onto a thing of
/// the map made for whatever map the player is on. The rules of when it may and what it says are
/// <see cref="LeadFollower"/>'s; this is the game's side of them. Nothing of it is saved: a game loaded calls it out
/// again behind the player on its first frame.
/// </summary>
public partial class GameEngine
{
    // The follower this file made (null while none is out), the thing of the map it is dressed as and the map that
    // holds it, the tile it was last on (for its prints) and the steps it has walked since it last found something
    private Follower? lead;
    private NPC? leadBody;
    private Map? leadMap;
    private (int X, int Y) leadTile;
    private int leadSteps;

    /// <summary>What of the place the player is in decides whether the lead may be out of its ball.</summary>
    private LeadFollower.Place LeadPlace() => new(
        flow.Player.Mode,
        flow.Map.IsDark && !flow.Map.Lit,
        IsUnderground,
        flow.Map.Name == AmitySquare.Map,
        flow.Along != null && flow.Along != lead);

    /// <summary>
    /// A frame of the field: the lead comes out behind the player, goes back in its ball, changes for another of the
    /// team, or is carried to the map the player has come to, as the row, the team and the place say.
    /// </summary>
    private void KeepTheLeadAlong(float dt)
    {
        // Someone else took the place behind the player (a partner of the story, the square's walk): ours steps aside
        if (lead != null && flow.Along != lead) PutTheLeadAway();
        var walker = LeadFollower.Walker(flow.Party);
        if (!LeadFollower.MayWalk(Settings.Follower, walker, LeadPlace()))
        {
            if (lead != null) PutTheLeadAway();
            return;
        }
        if (lead == null || lead.Pokemon != walker || leadBody == null)
        {
            CallTheLeadOut(walker!);
            return;
        }
        // The player has come to another map: the lead comes with them, behind them as they arrive
        if (leadMap != flow.Map) CarryTheLead();

        // Its steps leave prints, leaves and splashes where anyone's would, and a ledge's hop lands in dust
        if (lead.KeepHopping(dt)) world?.Life.Landing(flow.Map, leadBody.GridX, leadBody.GridY);
        if ((leadBody.GridX, leadBody.GridY) != leadTile)
        {
            leadTile = (leadBody.GridX, leadBody.GridY);
            leadSteps++;
            if (!lead.Hopping) world?.Life.Footstep(flow.Map, leadBody.GridX, leadBody.GridY, leadBody.Facing, false, TravelMode.OnFoot);
        }
    }

    /// <summary>The lead comes out of its ball behind the player, landing in a puff, as a thing of the map made for it.</summary>
    private void CallTheLeadOut(Pokemon walker)
    {
        if (lead != null) PutTheLeadAway();
        var body = new NPC { Key = LeadFollower.BodyKey, GridX = flow.Player.GridX, GridY = flow.Player.GridY };
        lead = new Follower(body, walker) { Gap = StrollGap(walker) };
        leadBody = body;
        leadMap = flow.Map;
        flow.Map.NPCs.Add(body);
        SetAlong(lead);
        PutBehind(lead, flow.Player.GridX, flow.Player.GridY, flow.Player.Facing);
        leadTile = (body.GridX, body.GridY);
        // Its sprite is baked from its model unless an earlier run left it in the cache (plan 03 · D13)
        if (!FieldSprites.Ready(walker.LookName)) flow.WantModel(walker.LookName);
        world?.Life.Landing(flow.Map, body.GridX, body.GridY);
    }

    /// <summary>The lead is back in its ball: off the map, and nobody walks behind the player.</summary>
    private void PutTheLeadAway()
    {
        if (leadBody != null) leadMap?.NPCs.Remove(leadBody);
        if (lead != null && flow.Along == lead) SetAlong(null);
        lead = null;
        leadBody = null;
        leadMap = null;
    }

    /// <summary>The player has come to another map: the lead's body leaves the map left and stands behind them on this one.</summary>
    private void CarryTheLead()
    {
        if (lead == null || leadBody == null) return;
        leadMap?.NPCs.Remove(leadBody);
        if (leadMap != null && leadMap.Follower == leadBody) leadMap.Follower = null;
        leadMap = flow.Map;
        flow.Map.NPCs.Add(leadBody);
        flow.Map.Follower = leadBody;
        PutBehind(lead, flow.Player.GridX, flow.Player.GridY, flow.Player.Facing);
        leadTile = (leadBody.GridX, leadBody.GridY);
    }

    /// <summary>
    /// The lead spoken to (plan 10 · F4): it has turned to the player already; a bubble by how it feels, its cry and a
    /// line of its friendship's band, or, now and then, something it found, which is the player's.
    /// </summary>
    private bool TalkToTheLead(NPC npc)
    {
        if (lead?.Pokemon is not { } pokemon || npc != leadBody) return false;
        int band = LeadFollower.BandOf(pokemon.Friendship);
        npc.ShowBubble(LeadFollower.BubbleOf(band));
        string name = pokemon.DisplayName;
        if (LeadFollower.Find(leadSteps, fieldRandom) is { } found)
        {
            leadSteps = 0;
            return StartScript(FieldScripts.FollowerFound, npc, own: new[] { Text.Of("followerTalk.found", ("name", name)) }, item: (found, 1));
        }
        return StartScript(FieldScripts.Pokemon, npc, own: new[] { Text.Of(LeadFollower.LineKey(band), ("name", name)) });
    }

    /// <summary>The follower the options' row has out now, for a tool (the harness, the tests); null when none is.</summary>
    internal Follower? Lead => lead;
}
