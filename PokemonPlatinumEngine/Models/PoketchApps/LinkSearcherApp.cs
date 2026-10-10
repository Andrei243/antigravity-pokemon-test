using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The link searcher (<c>link_searcher/main.c</c>): a touch searches for people nearby playing over the wireless
/// link, and says how many. This game has no other players near (online play is plan 07), so every search finds
/// nobody. As in the original, it won't search while the player is walking (<c>State_DontMoveError</c>); in hand
/// the player always stands still.
/// </summary>
public sealed class LinkSearcherApp : PoketchAppState
{
    /// <summary>How long a search runs before it shows what it found.</summary>
    public const float SearchSeconds = 2f;

    private static readonly PoketchButton[] screen = { new(0, 0, 0, Columns, Rows) };

    public override PoketchApp App => PoketchApp.LinkSearcher;

    /// <summary>Seconds left of a search under way; 0 when none is.</summary>
    public float Searching { get; private set; }

    /// <summary>Whether a search has ended, and its result is on the screen.</summary>
    public bool Searched { get; private set; }

    /// <summary>The people found by the last search: always none here.</summary>
    public int Found => 0;

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => screen;

    public override void Press(int button, PoketchContext context)
    {
        if (Searching > 0f) return;
        Searching = SearchSeconds;
        Searched = false;
        context.Sound("poketch");
    }

    public override void Update(float dt, PoketchContext context)
    {
        if (Searching <= 0f) return;
        Searching = System.Math.Max(0f, Searching - dt);
        if (Searching == 0f)
        {
            Searched = true;
            context.Sound("poketch_beep");
        }
    }
}
