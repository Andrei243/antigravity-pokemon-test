using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Amity Square's walk as the game plays it (plan 06 · R16c): a Pokémon of the team let out of its ball walks behind
/// the player as the square's <c>follower_mon</c>. The rules are <see cref="AmitySquare"/> and the square's own
/// script; what is here is the game's side of them. The one that walks is the game's one <see cref="Follower"/>
/// (<c>along</c>), as a partner of the story is, so everything that keeps a follower behind the player serves both.
/// </summary>
public partial class GameEngine
{
    // The model of a Pokémon let out of its ball is made behind the script's black screen: the script waits for it,
    // this long at most, so the first thing seen of it is itself and not the ball that stands in for a sprite unbaked
    private string? strollModel;
    private float strollWait;
    private const float StrollPatience = 4f;

    /// <summary>
    /// A Pokémon drawn taller than this, in tiles, keeps two tiles behind the player and not one: a tile south of the
    /// player it would hide them to the shoulders from the field's camera (style guide, "Pokémon in the field"). Of the
    /// square's species that is Torterra and Empoleon.
    /// </summary>
    public const float TallStroller = 1.3f;

    private static int StrollGap(Pokemon pokemon) => FieldSprites.FieldHeight(FieldSprites.MetresOf(pokemon.ModelName)) > TallStroller ? 2 : 1;

    /// <summary>A script is waiting for the model of the Pokémon it let out.</summary>
    private bool AwaitingStroll => strollModel != null;

    /// <summary>
    /// A Pokémon of the team walks behind the player as a thing of the map from now on (<c>amity walk</c>), or, with
    /// nobody named, the one that did stops where it stands (<c>amity leave</c>).
    /// </summary>
    private void WalkWith(NPC? body, Pokemon? pokemon)
    {
        if (body == null || pokemon == null)
        {
            if (flow.Along?.Pokemon != null) SetAlong(null);
            return;
        }
        var stroller = new Follower(body, pokemon) { Gap = StrollGap(pokemon) };
        SetAlong(stroller);
        PutBehind(stroller, flow.Player.GridX, flow.Player.GridY, flow.Player.Facing);
        flow.WantModel(pokemon.ModelName);
        strollModel = pokemon.ModelName;
        strollWait = StrollPatience;
    }

    /// <summary>
    /// A frame of the field, whatever has it: the wait for the model ends, and the place of the team the story keeps
    /// for whoever walks along goes with the Pokémon when the team is put in another order.
    /// </summary>
    private void KeepTheStroll(float dt)
    {
        if (strollModel != null && ((strollWait -= dt) <= 0f || PokemonModels.TryGet(strollModel, out _))) strollModel = null;
        if (flow.Along?.Pokemon is not { } walker) return;
        int slot = flow.Party.Members.IndexOf(walker);
        if (slot < 0) EndStroll();
        else if (flow.Story.Var(AmitySquare.PartyVar) != slot) flow.Story.SetVar(AmitySquare.PartyVar, slot);
    }

    /// <summary>The walk is over without the receptionist's goodbye: the Pokémon is back in its ball and its object off the map.</summary>
    private void EndStroll()
    {
        if (flow.Along?.Pokemon != null) SetAlong(null);
        strollModel = null;
        if (flow.Story.Var(AmitySquare.ActiveVar) != 0) flow.Story.SetVar(AmitySquare.ActiveVar, 0);
        if (!flow.Story.Has(AmitySquare.HideFlag)) flow.Story.Set(AmitySquare.HideFlag);
    }

    /// <summary>
    /// The player has come to a map: a Pokémon out of its ball belongs to the square alone, so whatever way the square
    /// was left by (the original has only its two gates), the walk has ended.
    /// </summary>
    private void LeaveStrollBehind()
    {
        if (flow.Map.Name == AmitySquare.Map) return;
        if (flow.Along?.Pokemon != null || flow.Story.Var(AmitySquare.ActiveVar) == 1) EndStroll();
    }

    /// <summary>
    /// A game saved in the square with a Pokémon out of its ball goes on with it behind the player: the story's own
    /// variables say that one walks and which place of the team it has.
    /// </summary>
    private void ResumeStroll()
    {
        if (flow.Map.Name != AmitySquare.Map || flow.Story.Var(AmitySquare.ActiveVar) != 1) return;
        if (AmitySquare.Walker(flow.Story, flow.Party) is not { } pokemon || flow.Map.FindPerson(AmitySquare.Body) is not { } body)
        {
            EndStroll();
            return;
        }
        flow.Along = new Follower(body, pokemon) { Gap = StrollGap(pokemon) };
        KeepPartnerAlong();
        flow.WantModel(pokemon.ModelName);
    }

    private sealed partial class FieldHost
    {
        public void WalkWith(NPC? body, Pokemon? pokemon) => game.WalkWith(body, pokemon);
    }
}
