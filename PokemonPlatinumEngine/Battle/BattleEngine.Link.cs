using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// A link battle on screen (plan 28 · I3, the screen's half): a face fed from a link client, with no rules of its own.
public partial class BattleEngine
{
    private readonly LinkScreenFeed? link;

    // The question the server asked, as the screen asks it (its places on the player's side), while it stands
    private LinkAsk? linkAsk;

    /// <summary>
    /// A link battle (plan 28 · I3): what this screen plays comes from the server through a link client, and its
    /// choices go back the same way. Nothing on this machine works a turn out: the face's own <see cref="Core"/> is a
    /// stage that holds the places and is never started, asked or answered (its log stays empty), and what may be
    /// answered is what the server said each place may (<see cref="PlaceOptions"/>).
    /// </summary>
    public bool IsLink => link != null;

    /// <summary>The link client this screen plays, in a link battle.</summary>
    public LinkClient? LinkClient => link?.Client;

    /// <summary>
    /// The screen waits for the other player (plan 28 · I3; style guide, "Battle panels", "Waiting for the other player"):
    /// everything sent has been shown, nothing is asked of this player, and the battle isn't over.
    /// </summary>
    public bool WaitingForOther => link != null && HUD.MenuState == BattleMenuState.Waiting;

    /// <summary>The line the screen shows while it waits: "Waiting for {name}…", empty otherwise.</summary>
    public string WaitingLine => WaitingForOther ? Text.Of("linkRoom.waiting", ("player", link!.Client.Hello?.FoeName ?? "")) : "";

    /// <summary>
    /// A link battle's screen (plan 28 · I3): plays <paramref name="client"/>'s view as it comes and answers what the
    /// server asks through the menus. The client must have been greeted (its team and the foe's preview known) and must
    /// not answer by itself. Side B's view is turned round, so whoever chooses sees the battle from their own side.
    /// </summary>
    /// <param name="playerLook">Who stands on the player's platform (their look); the class's look when null.</param>
    /// <param name="foeLook">Who stands across the field.</param>
    public static BattleEngine ForLink(LinkClient client, bool scene = true, string? playerLook = null, string? foeLook = null)
    {
        if (client.Hello is not { } hello) throw new System.InvalidOperationException("The room hasn't greeted this player yet");
        client.AnswersByItself = false;
        var mine = new Party();
        foreach (var p in client.Own) mine.Add(p);
        var foe = new Trainer
        {
            Id = "link_" + hello.FoeName,
            Name = hello.FoeName,
            TrainerClass = LinkSetup.PlayerClass,
            Look = null,
            DoubleBattle = hello.Room.Format == BattleFormat.Double
        };
        // A foe is known by its preview: its bar is drawn on its stand-in's own scale, full until the view says otherwise
        foreach (var standIn in client.FoeStandIns)
        {
            standIn.CurrentHP = standIn.MaxHP;
            foe.Party.Add(standIn);
        }
        var setup = new BattleSetup
        {
            PlayerParty = mine,
            Inventory = new Inventory(),
            Pokedex = new Pokedex(),
            Trainers = new List<Trainer> { foe },
            Format = hello.Room.Format,
            // The stage is never started: it draws nothing, from these dice or the game's
            Random = new BattleRandom(0),
            Rules = Ruleset.Of(hello.Room.Rules),
            PlayerName = client.Name,
            PlayerLook = playerLook,
            Frontier = true,
            Scene = scene
        };
        var face = new BattleEngine(setup, null, new LinkScreenFeed(client));
        // The other player stands across the field in their own look, where the screen was told it
        if (foeLook != null) face.Anim.EnemyTrainer = foeLook;
        return face;
    }

    /// <summary>Takes in what the server has sent: the view's new events are played, and a question waiting is opened once they have been.</summary>
    private void PumpLink()
    {
        if (link == null) return;
        link.Client.Pump();
        var fresh = link.TakeEvents();
        if (fresh.Count > 0)
        {
            // A question already open stands until its lines come (it can't: the server asks only after a turn's lines)
            Play(fresh);
            Pump();
            return;
        }
        // Nothing new to show: a question that came on its own opens now
        if (HUD.MenuState == BattleMenuState.Waiting && !playingTheLog) LinkLogPlayed();
    }

    /// <summary>
    /// Everything the server sent has been shown (the link's <see cref="LogPlayed"/>): the question it asks this player
    /// opens, or the screen waits for the other player, or the battle is over.
    /// </summary>
    private void LinkLogPlayed()
    {
        SyncFromView();
        if (Result != BattleResult.None) return;
        linkAsk = link!.TakeAsk() ?? (linkAsk != null && link.Standing?.Number == linkAsk.Number ? linkAsk : null);
        switch (linkAsk?.Request)
        {
            case ActionRequest:
                BeginChoosing();
                break;
            case ReplacementRequest replacement:
                replacing = true;
                menuSlot = replacement.Place.Slot;
                HUD.SwitchMenuIndex = 0;
                HUD.MenuState = BattleMenuState.SwitchPokemon;
                break;
            default:
                // Nothing asked of this player: the other one is choosing
                currentMessage = "";
                waitingForMessageConfirm = false;
                HUD.MenuState = BattleMenuState.Waiting;
                break;
        }
    }

    /// <summary>The screen's places as the view has told them: the rules' copies are no part of a link battle.</summary>
    private void SyncFromView()
    {
        foreach (var b in AllBattlers)
            Anim.Conditions.Settle(b.Place, ConditionLooks.RulesHave(b));
    }

    /// <summary>The question standing, as the screen asks it: the server's in a link battle, the rules' otherwise.</summary>
    private BattleRequest? Asking => link != null ? linkAsk?.Request : core.Request;

    /// <summary>What the server said a place of the player's may answer with; null outside a link battle.</summary>
    private PlaceOptions? OptionsFor(Place place) => linkAsk?.Options.FirstOrDefault(o => o.Place == place);

    /// <summary>The moves the place may use this turn as the server allows them.</summary>
    private List<Move> LinkUsableMoves(Battler b)
    {
        var options = OptionsFor(b.Place);
        if (options == null || b.Pokemon == null) return new List<Move>();
        return options.Moves.Where(i => i >= 0 && i < b.Pokemon.Moves.Count).Select(i => b.Pokemon.Moves[i]).ToList();
    }

    /// <summary>Why the server won't take this move; null when it will.</summary>
    private string? LinkWhyNotMove(Battler b, int index) =>
        OptionsFor(b.Place) is { } options && options.Moves.Contains(index) ? null
            : Text.Of("linkRoom.moveRefused", ("pokemon", b.Pokemon?.DisplayName ?? ""), ("move", b.Pokemon is { } p && index < p.Moves.Count ? p.Moves[index].DisplayName : ""));

    /// <summary>Why the server won't take this switch; null when it will.</summary>
    private string? LinkWhyNotSwitch(Battler b, int partyIndex) =>
        OptionsFor(b.Place) is { } options && options.Switches.Contains(partyIndex) ? null : Text.Of("linkRoom.switchRefused", ("pokemon", b.Pokemon?.DisplayName ?? ""));

    /// <summary>
    /// The screen's answer goes to the server (turned back for side B), or the match is given up where RUN was answered
    /// yes; then the screen waits for what the server sends next.
    /// </summary>
    private void SubmitToLink(IReadOnlyList<BattleChoice> answer)
    {
        AudioManager.PlaySound("select");
        if (answer.Any(c => c.Kind is ChoiceKind.Run or ChoiceKind.Forfeit)) link!.Client.Forfeit();
        else link!.Answer(answer);
        linkAsk = null;
        replacing = false;
        currentMessage = "";
        waitingForMessageConfirm = false;
        HUD.MenuState = BattleMenuState.Waiting;
        // The server may already have everything (the other player answered first): what it sent is shown at once
        PumpLink();
    }
}
