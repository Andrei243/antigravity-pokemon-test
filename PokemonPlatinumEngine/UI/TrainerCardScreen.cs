using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the Trainer Card shows of the player.</summary>
public sealed record TrainerCardInfo(string Name, int TrainerId, int Money, int Seen, int Caught, float PlayTimeSeconds, int Badges, DateTime? Started)
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
}

/// <summary>
/// The Trainer Card: who the player is, what they have, how long they have played and the badges they have
/// won, with their own field sprite as the portrait; its colour and stars by what they have done, and A turns it
/// over to its back (plan 06 · R12).
/// </summary>
public class TrainerCardScreen
{
    private const float AppearTime = 0.35f;

    /// <summary>How long turning the card over takes: it narrows to its edge, then opens on the other side.</summary>
    public const float FlipTime = 0.3f;

    private float openAge, flipAge = FlipTime;
    private Texture2D? portrait;

    public bool IsActive { get; set; }

    /// <summary>Whether the card is turned over to its back (A turns it, as in the original).</summary>
    public bool ShowingBack { get; private set; }

    /// <summary>Turns the card over.</summary>
    public void Flip()
    {
        ShowingBack = !ShowingBack;
        flipAge = 0f;
        AudioManager.PlaySound("page");
    }

    /// <param name="portrait">The player's field sprite (baked outside any texture mode); none in tests.</param>
    public void Open(Texture2D? portrait = null)
    {
        IsActive = true;
        ShowingBack = false;
        openAge = 0f;
        flipAge = FlipTime;
        this.portrait = portrait;
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
    }

    public void Draw(int screenWidth, int screenHeight, TrainerCardInfo info)
    {
        if (!IsActive) return;
        ModernUi.DrawTrainerCard(screenWidth, screenHeight, info, portrait, Math.Clamp(openAge / AppearTime, 0f, 1f), ShowingBack,
            Math.Clamp(flipAge / FlipTime, 0f, 1f));
    }
}
