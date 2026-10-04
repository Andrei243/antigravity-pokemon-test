using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the Trainer Card shows of the player.</summary>
public sealed record TrainerCardInfo(string Name, int TrainerId, int Money, int Seen, int Caught, float PlayTimeSeconds, int Badges, DateTime? Started);

/// <summary>
/// The Trainer Card: who the player is, what they have, how long they have played and the badges they have
/// won, with their own field sprite as the portrait. The card's stars and its back are plan 06 · R12's.
/// </summary>
public class TrainerCardScreen
{
    private const float AppearTime = 0.35f;

    private float openAge;
    private Texture2D? portrait;

    public bool IsActive { get; set; }

    /// <param name="portrait">The player's field sprite (baked outside any texture mode); none in tests.</param>
    public void Open(Texture2D? portrait = null)
    {
        IsActive = true;
        openAge = 0f;
        this.portrait = portrait;
    }

    public void Close() => IsActive = false;

    /// <summary>A trainer's number as the card prints it: five digits.</summary>
    public static string FormatId(int trainerId) => (((trainerId % 100000) + 100000) % 100000).ToString("D5");

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;

        if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Menu))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
    }

    public void Draw(int screenWidth, int screenHeight, TrainerCardInfo info)
    {
        if (!IsActive) return;
        ModernUi.DrawTrainerCard(screenWidth, screenHeight, info, portrait, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
