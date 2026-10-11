using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 06 · R16c: <c>amity</c>, Amity Square's walk (<see cref="AmitySquare"/>), each word on the
/// lines of the original's script it stands for (<c>scripts_amity_square.s</c>).
/// </summary>
public sealed partial class ScriptRunner
{
    // The place of the team the receptionist last asked about, the name of whoever she asked about or walks along,
    // and what was last found or offered (a berry's or an accessory's name, and how many)
    private int amityOffer = -1;
    private string lastWalker = "", amityThing = "";
    private bool amityAccessory;
    private int amityCount = 1;

    /// <summary>
    /// <c>arrive</c> draws the day's gift and where the man of the park stands (the story's
    /// <c>VAR_AMITY_SQUARE_GIFT_ID</c> and <c>VAR_AMITY_SQUARE_GIFT_MAN_PLACE</c>). <c>may</c> is 1 when the team has
    /// one of the species the square lets in, and starts the receptionist's asking over; <c>next</c> is 1 while
    /// another Pokémon of the team may walk, which <c>{walker}</c> then names, and 0 once there is none left.
    /// <c>walk</c> lets the one last named out of its ball: it is the square's <c>follower_mon</c> from here and walks
    /// behind the player, its steps counted from nought and its first find decided; <c>leave</c> ends that.
    /// <c>walker</c> is 1 while one walks along (and names it), <c>grown</c> 1 when it is one of the six starters grown
    /// up. <c>find</c> is what speaking to it comes to: 0 nothing, 1 a berry, 2 an accessory (<c>{thing}</c>), which
    /// <c>take</c> hands over. <c>gift</c> is what the man offers today, 0 five of a berry or 1 an accessory
    /// (<c>{thing}</c>), which <c>give</c> hands over (RESULT 0 when the Fashion Case has no room for it).
    /// </summary>
    private void Amity(Instruction i)
    {
        var team = host.Party;
        switch (i.Name)
        {
            // AmitySquare_OnTransition: CalcAmitySquareBerryAndAccessoryManOptionID, then GetRandom 5 for his place
            case "arrive":
                AmitySquare.Arrive(host.Story, host.Random);
                Result = host.Story.Var(AmitySquare.GiftManPlaceVar);
                break;

            // AmitySquare_CheckPartyHasSpecies
            case "may":
                amityOffer = -1;
                Result = AmitySquare.HasPermitted(team, host.NationalDex) ? 1 : 0;
                break;

            // AmitySquare_CheckCurrentSlotAllowedMon, a place of the team at a time
            case "next":
                amityOffer = AmitySquare.Next(team, host.NationalDex, amityOffer + 1);
                if (amityOffer < 0)
                {
                    amityOffer = team.Count;
                    Result = 0;
                    break;
                }
                lastWalker = team.Members[amityOffer].DisplayName;
                Result = 1;
                break;

            // AmitySquare_SetFollowerMon and AmitySquare_EnterWithFollowerMon: VAR_FOLLOWER_MON_PARTY_ID and _SPECIES,
            // the object's looks, ClearAmitySquareStepCount, SetNextPickUpType, and MOVEMENT_TYPE_FOLLOW_PLAYER
            case "walk":
            {
                if (amityOffer < 0 || amityOffer >= team.Count || !AmitySquare.MayWalk(team.Members[amityOffer], host.NationalDex))
                    throw Wrong(i, "nobody of the team has been offered a walk: 'amity next' comes first");
                var pokemon = team.Members[amityOffer];
                var body = host.FindNpc(AmitySquare.Body, place.Length > 0 ? place : null)
                    ?? throw Wrong(i, $"this place has no '{AmitySquare.Body}' for a Pokémon to walk as");
                AmitySquare.Begin(host.Story, amityOffer, pokemon, host.Random);
                lastWalker = pokemon.DisplayName;
                host.WalkWith(body, pokemon);
                break;
            }

            // ClearHasPartner: it follows nobody any more (the script takes its object off the map by its flag)
            case "leave":
                host.WalkWith(null, null);
                break;

            case "walker":
            case "grown":
            {
                var walker = host.Story.Var(AmitySquare.ActiveVar) == 1 ? AmitySquare.Walker(host.Story, team) : null;
                if (walker != null) lastWalker = walker.DisplayName;
                Result = walker == null ? 0 : i.Name == "walker" || AmitySquare.GrownStarters.Contains(walker.Species.Name) ? 1 : 0;
                break;
            }

            // AmitySquare_FollowerMon: two hundred steps, and it is holding what was decided ahead
            case "find":
            {
                Result = 0;
                if (AmitySquare.Walker(host.Story, team) is not { } walker) break;
                lastWalker = walker.DisplayName;
                var found = AmitySquare.Find(host.Story, walker.Species.Name, host.FashionCase, host.Random, out string name);
                amityThing = lastThing = name;
                amityAccessory = found == AmityFind.Accessory;
                amityCount = 1;
                Result = (int)found;
                break;
            }

            // Common_GiveItemQuantityNoLineFeed and Common_GiveAccessoryWaitForConfirm
            case "take":
            case "give":
                Result = HandOverAmityThing() ? 1 : 0;
                break;

            // AmitySquare_GiftMan: GetAmitySquareBerryOrAccessoryIDFromMan, CheckAmitySquareManGiftIsAccessory
            case "gift":
            {
                int gift = System.Math.Clamp(host.Story.Var(AmitySquare.GiftVar), 0, AmitySquare.Gifts.Count - 1);
                amityThing = lastThing = AmitySquare.Gifts[gift];
                amityAccessory = AmitySquare.GiftIsAccessory(gift);
                amityCount = amityAccessory ? 1 : AmitySquare.GiftBerries;
                Result = amityAccessory ? 1 : 0;
                break;
            }
        }
    }

    /// <summary>What was found or offered goes into the bag or the Fashion Case, with the fanfare and the lines <c>give</c> and <c>giveaccessory</c> say.</summary>
    private bool HandOverAmityThing()
    {
        if (amityThing.Length == 0) return false;
        string received = amityCount == 1 ? $"{{player}} received the {amityThing}!" : $"{{player}} received {amityCount} × {amityThing}!";
        string it = amityCount == 1 ? "it" : "them";
        if (amityAccessory)
        {
            lastAccessory = amityThing;
            if (!host.FashionCase.Add(SuperContestDatabase.Accessory(amityThing), amityCount)) return false;
            host.Fanfare(MusicRole.FanfareItem);
            host.Say(null, new List<string> { received, $"{{player}} put {it} away in the Fashion Case." });
            return true;
        }
        var item = ItemDatabase.Get(amityThing);
        if (item == null) return false;
        host.Bag.AddItem(item, amityCount);
        lastItem = item.Name;
        host.Fanfare(FanfareFor(item));
        host.Say(null, new List<string> { received, $"{{player}} put {it} away in the {PocketName(item.Pocket)} pocket." });
        return true;
    }
}
