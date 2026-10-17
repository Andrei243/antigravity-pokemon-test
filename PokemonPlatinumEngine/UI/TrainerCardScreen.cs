using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// One region's badges as the Trainer Card shows them (plan 18 · W2): the region, the names of its badges in its own
/// order (none to eight, from its file), the ones won as a mask, and whether its Hall of Fame has been entered.
/// </summary>
public sealed record CardRegion(string Id, int Generation, IReadOnlyList<string> BadgeNames, int Badges, bool Champion = false)
{
    /// <summary>The species of the region's own Pokédex seen and caught (plan 18 · W3); null for a region whose Pokédex the player hasn't.</summary>
    public (int Seen, int Caught, int Of)? Pokedex { get; init; }

    /// <summary>How many of the region's badges are won.</summary>
    public int Count => TitleScreen.CountBadges(Badges);

    public bool Has(int index) => (Badges & (1 << index)) != 0;
}

/// <summary>What the Trainer Card shows of the player. <see cref="Here"/> is the region the player is in, whose badges the front shows.</summary>
public sealed record TrainerCardInfo(string Name, int TrainerId, int Money, int Seen, int Caught, float PlayTimeSeconds, CardRegion Here, DateTime? Started)
{
    /// <summary>The card's score and colour (plan 06 · R12: <see cref="TrainerScore"/>, <see cref="TrainerCardRules"/>).</summary>
    public int Score { get; init; }
    public TrainerCardRules.CardColour Colour { get; init; } = TrainerCardRules.CardColour.Normal;

    /// <summary>How many of the five things that colour the card are done (its stars).</summary>
    public int Stars { get; init; }

    /// <summary>The back: when the player first entered the Hall of Fame, and their link battles and trades (none until plan 07).</summary>
    public DateTime? HallOfFameDebut { get; init; }
    public int LinkWins { get; init; }
    public int LinkLosses { get; init; }
    public int LinkTrades { get; init; }

    /// <summary>The Super Contests entered and won (plan 06 · R17b).</summary>
    public int ContestsEntered { get; init; }
    public int ContestsWon { get; init; }

    /// <summary>
    /// Every region the player has been in, in the journey's order, each a page of its own once there are two or more
    /// (plan 18 · W2, decision 1); with one, the card has its front and its back only.
    /// </summary>
    public IReadOnlyList<CardRegion> Regions { get; init; } = Array.Empty<CardRegion>();

    /// <summary>The regions that have a page: every one, once there are two or more.</summary>
    public IReadOnlyList<CardRegion> RegionPages => Regions.Count > 1 ? Regions : Array.Empty<CardRegion>();
}

/// <summary>
/// The pages of the Trainer Card (plan 18 · W2; plan 13 · V1, V13 add theirs at the end): the front, its back, and a
/// page for each region the player has been in.
/// </summary>
public enum CardPage { Front, Back, Region }

/// <summary>
/// The Trainer Card: who the player is, what they have, how long they have played and the badges they have
/// won, with their own field sprite as the portrait; its colour and stars by what they have done, and A turns it
/// over to its back (plan 06 · R12). Once the player has been in two regions, left and right go on to a page for
/// each (plan 18 · W2).
/// </summary>
public class TrainerCardScreen
{
    private const float AppearTime = 0.35f;

    /// <summary>How long turning the card over takes: it narrows to its edge, then opens on the other side.</summary>
    public const float FlipTime = 0.3f;

    private float openAge, flipAge = FlipTime;
    private Texture2D? portrait;

    // The page shown and the one it is turning from: 0 is the front, 1 the back, 2 and on the regions' pages
    private int page, from;

    public bool IsActive { get; set; }

    /// <summary>How many regions' pages the card has (none with only one region: <see cref="TrainerCardInfo.RegionPages"/>).</summary>
    public int RegionPages { get; private set; }

    /// <summary>The page shown.</summary>
    public CardPage Page => PageOf(page);

    /// <summary>On a region's page, which of <see cref="TrainerCardInfo.RegionPages"/> it is; -1 elsewhere.</summary>
    public int RegionShown => page >= 2 ? page - 2 : -1;

    /// <summary>Whether the card is turned over to its back (A turns it, as in the original).</summary>
    public bool ShowingBack => Page == CardPage.Back;

    private static CardPage PageOf(int index) => index switch { 0 => CardPage.Front, 1 => CardPage.Back, _ => CardPage.Region };

    /// <summary>Turns the card over: the front to its back and the back to its front; a region's page goes back to the front.</summary>
    public void Flip() => TurnTo(page == 0 ? 1 : 0);

    /// <summary>Goes to the next page or the one before (left and right), while there are regions' pages; false at either end.</summary>
    public bool Turn(int step)
    {
        int to = page + Math.Sign(step);
        if (RegionPages == 0 || step == 0 || to < 0 || to > 1 + RegionPages) return false;
        TurnTo(to);
        return true;
    }

    private void TurnTo(int to)
    {
        from = page;
        page = to;
        flipAge = 0f;
        AudioManager.PlaySound("page");
    }

    /// <param name="portrait">The player's field sprite (baked outside any texture mode); none in tests.</param>
    /// <param name="regionPages">How many regions' pages the card has (<see cref="TrainerCardInfo.RegionPages"/>).</param>
    public void Open(Texture2D? portrait = null, int regionPages = 0)
    {
        IsActive = true;
        page = from = 0;
        openAge = 0f;
        flipAge = FlipTime;
        this.portrait = portrait;
        RegionPages = Math.Max(0, regionPages);
    }

    public void Close() => IsActive = false;

    /// <summary>A trainer's number as the card prints it: five digits.</summary>
    public static string FormatId(int trainerId) => (((trainerId % 100000) + 100000) % 100000).ToString("D5");

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        flipAge += dt;

        if (InputManager.IsActionPressed(GameAction.Confirm)) Flip();
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
        else if (InputManager.IsActionPressed(GameAction.Left)) Turn(-1);
        else if (InputManager.IsActionPressed(GameAction.Right)) Turn(1);
    }

    public void Draw(int screenWidth, int screenHeight, TrainerCardInfo info)
    {
        if (!IsActive) return;
        ModernUi.DrawTrainerCard(screenWidth, screenHeight, info, portrait, Math.Clamp(openAge / AppearTime, 0f, 1f),
            new CardSide(Page, RegionShown), new CardSide(PageOf(from), from >= 2 ? from - 2 : -1), Math.Clamp(flipAge / FlipTime, 0f, 1f));
    }
}

/// <summary>A side of the Trainer Card being drawn: a page, and on a region's page which region.</summary>
public readonly record struct CardSide(CardPage Page, int Region = -1);
