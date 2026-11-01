using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The requests (plan 12 · Q21; <see cref="Story.Requests"/>): the start menu's REQUESTS and its page, the request pinned
/// over the field and the bubble over whoever its next step is taken to. All of it is worked out from the story whenever
/// it is shown; the one thing kept is which request is pinned (<see cref="SaveData.PinnedRequest"/>).
/// </summary>
public partial class GameEngine
{
    /// <summary>Whether the options' REQUESTS row is on.</summary>
    internal bool RequestsOn => Settings.Requests.Holds(Ruleset.Current.Preset);

    /// <summary>Whether the start menu has REQUESTS: the row on, and an errand known or the area's checklist (whose tab is on the page) on.</summary>
    internal bool HasRequests => RequestsOn && (Requests.AnyKnown(flow.Story) || Settings.AreaChecklist);

    /// <summary>The request pinned and its next step, while the row is on and the request is under way; null otherwise.</summary>
    internal (Request Request, RequestStep Step)? PinnedNow()
    {
        if (!RequestsOn || flow.PinnedRequest is not { } id || Requests.Find(id) is not { } request) return null;
        if (Requests.State(request, flow.Story) != RequestState.Open || Requests.Next(request, flow.Story) is not { } step) return null;
        return (request, step);
    }

    private void OpenRequests()
    {
        string name = flow.Map?.ShownNameAt(flow.Player.GridX, flow.Player.GridY) ?? "";
        flow.RequestsScreen.Open(flow.Story, flow.PinnedRequest, AreaCountsNow(), name);
        flow.State = GameState.Requests;
    }

    private void UpdateRequests(float dt)
    {
        flow.RequestsScreen.Update(dt);
        if (flow.RequestsScreen.IsActive) return;
        flow.PinnedRequest = flow.RequestsScreen.Pinned;
        flow.State = GameState.Overworld;
    }

    /// <summary>
    /// On coming to a place: a bubble over whoever the pinned request's next step is taken to (the giver where the step
    /// names nobody), when they are of this place.
    /// </summary>
    private void PointOutThePinnedRequest()
    {
        if (PinnedNow() is not { } pin || flow.CurrentArea is not { } area || area.Key != pin.Request.Place) return;
        string who = pin.Step.Person ?? pin.Request.Giver;
        flow.Map.FindPerson(who, area.Key)?.ShowBubble(EmoteBubble.Note, 1.6f);
    }

    /// <summary>The pinned request's line at the top left of the field, while nothing else stands there.</summary>
    private void DrawPinnedRequest(GameState shown)
    {
        if (shown != GameState.Overworld || ScriptRunning || flow.LocationSign.Visible) return;
        if (PinnedNow() is not { } pin) return;
        ModernUi.PinnedRequest(pin.Request.Title, pin.Request.StepLine(pin.Step));
    }
}
