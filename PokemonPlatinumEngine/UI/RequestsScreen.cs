using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The requests page's tabs (plan 12 · Q21, Q22): the errands under way, those done, and what is left in the area underfoot.</summary>
public enum RequestsTab { Open, Done, Area }

/// <summary>
/// The requests page (plan 12 · Q21; style guide, "Menu screens", "Requests"): the start menu's REQUESTS, over the field.
/// OPEN lists the errands under way and DONE those finished, each with its giver, its steps (the done ones ticked, the
/// next one marked) and its reward; the A button pins an open one, whose next step then stands at the top left of the
/// field, or takes the pin off again. THIS AREA (plan 12 · Q22) is there only while the options' WHAT IS LEFT HERE row
/// is on: the counts of the area underfoot, never a name. Its logic takes no input (<see cref="Move"/>,
/// <see cref="ChangeTab"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it; what it
/// shows is worked out from the story as it opens, and what it leaves behind is <see cref="Pinned"/>.
/// </summary>
public class RequestsScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>The rows the list shows: as many as its panel holds.</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - (ModernUi.ContentTop + 76 + 24));

    private readonly int[] cursors = new int[2];
    private readonly int[] firsts = new int[2];
    private float openAge;

    public bool IsActive { get; private set; }
    public RequestsTab Tab { get; private set; }

    /// <summary>The story the page was opened on, which says what each step has come to.</summary>
    public StoryState Story { get; private set; } = new();

    public IReadOnlyList<Request> OpenList { get; private set; } = Array.Empty<Request>();
    public IReadOnlyList<Request> DoneList { get; private set; } = Array.Empty<Request>();

    /// <summary>The id of the request pinned over the field, or null; only ever an open one.</summary>
    public string? Pinned { get; private set; }

    /// <summary>What is left in the area underfoot (THIS AREA), or null while the options' row is off and the tab isn't there.</summary>
    public AreaCounts? Counts { get; private set; }

    /// <summary>The area's name as the sign writes it.</summary>
    public string AreaName { get; private set; } = "";

    public bool HasAreaTab => Counts != null;

    /// <summary>How many tabs there are: two, or three with THIS AREA.</summary>
    public int TabCount => HasAreaTab ? 3 : 2;

    /// <summary>How far the page has slid into place, 0 to 1.</summary>
    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    /// <summary>The list on the tab shown (none on THIS AREA).</summary>
    public IReadOnlyList<Request> Entries => Tab switch
    {
        RequestsTab.Open => OpenList,
        RequestsTab.Done => DoneList,
        _ => Array.Empty<Request>()
    };

    public int Cursor => Tab == RequestsTab.Area ? 0 : cursors[(int)Tab];
    public int First => Tab == RequestsTab.Area ? 0 : firsts[(int)Tab];

    public Request? Selected => Cursor >= 0 && Cursor < Entries.Count ? Entries[Cursor] : null;

    /// <summary>The tabs as the strip shows them, in order.</summary>
    public IReadOnlyList<RequestsTab> Tabs => HasAreaTab
        ? new[] { RequestsTab.Open, RequestsTab.Done, RequestsTab.Area }
        : new[] { RequestsTab.Open, RequestsTab.Done };

    /// <summary>
    /// Opens on the requests the story knows. <paramref name="pinned"/> is the request pinned so far (dropped if it is no
    /// longer open), <paramref name="counts"/> the area's counts for THIS AREA (null leaves the tab out).
    /// </summary>
    public void Open(StoryState story, string? pinned, AreaCounts? counts = null, string areaName = "", RequestsTab tab = RequestsTab.Open)
    {
        Story = story;
        OpenList = Requests.InState(story, RequestState.Open);
        DoneList = Requests.InState(story, RequestState.Done);
        Pinned = pinned != null && OpenList.Any(r => r.Id == pinned) ? pinned : null;
        Counts = counts;
        AreaName = areaName;
        Array.Clear(cursors);
        Array.Clear(firsts);
        // The cursor starts on the pinned request
        if (Pinned != null) cursors[0] = OpenList.ToList().FindIndex(r => r.Id == Pinned);
        Tab = tab == RequestsTab.Area && !HasAreaTab ? RequestsTab.Open : tab;
        openAge = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    public void Close() => IsActive = false;

    /// <summary>The cursor up (−1) or down (+1), wrapping round.</summary>
    public void Move(int delta)
    {
        if (Tab == RequestsTab.Area || Entries.Count == 0) return;
        int t = (int)Tab;
        int next = UiNav.Wrap(cursors[t], delta, Entries.Count);
        if (next == cursors[t]) return;
        cursors[t] = next;
        firsts[t] = UiNav.Window(firsts[t], next, Entries.Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The next tab (+1) or the one before (−1), wrapping round.</summary>
    public void ChangeTab(int delta)
    {
        var tabs = Tabs;
        int at = Math.Max(0, tabs.ToList().IndexOf(Tab));
        Tab = tabs[UiNav.Wrap(at, delta, tabs.Count)];
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: on OPEN pins the request under the cursor, or takes the pin off it if it is pinned already.</summary>
    public void Confirm()
    {
        if (Tab != RequestsTab.Open || Selected is not { } request)
        {
            AudioManager.PlaySound("error");
            return;
        }
        Pinned = Pinned == request.Id ? null : request.Id;
        AudioManager.PlaySound("select");
    }

    /// <summary>The B button: back to the field.</summary>
    public void Cancel()
    {
        Close();
        AudioManager.PlaySound("cancel");
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Left)) ChangeTab(-1);
        else if (InputManager.IsActionPressed(GameAction.Right)) ChangeTab(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawRequests(screenWidth, screenHeight, this, Appear);
}
