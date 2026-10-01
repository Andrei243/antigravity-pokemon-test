using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public enum BattleMenuState
{
    Main,
    Moves,
    SwitchPokemon,
    SelectBagItem,
    Message
}

public class BattleHUD
{
    // Battlefield layout in virtual-screen pixels. The battle renderer projects these from its 3D camera every
    // frame; the defaults match the settled camera.

    /// <summary>Where each Pokémon's feet touch its platform.</summary>
    public static Vector2 EnemyFeet { get; set; } = new(1430, 400);
    public static Vector2 PlayerFeet { get; set; } = new(470, 800);

    /// <summary>Rough body centres, used as targets for move effects and Poké Ball throws.</summary>
    public static Vector2 EnemyCenter { get; set; } = new(1430, 226);
    public static Vector2 PlayerCenter { get; set; } = new(470, 568);

    private const float BoxSlideTime = 0.35f;

    public BattleMenuState MenuState { get; set; } = BattleMenuState.Main;
    public int MainMenuIndex { get; set; } = 0;
    public int MoveMenuIndex { get; set; } = 0;
    public int SwitchMenuIndex { get; set; } = 0;
    public int BagMenuIndex { get; set; } = 0;

    /// <summary>Draws the HP boxes, move effects and the bottom panel over the battle field.</summary>
    /// <param name="playerPokemon">The Pokémon the menus act for.</param>
    /// <param name="anim">What the field shows: the HP boxes follow the Pokémon on the platforms and their draining bars.</param>
    public void Draw(
        int screenWidth,
        int screenHeight,
        Pokemon playerPokemon,
        Party playerParty,
        Party? enemyTrainerParty,
        string battleMessage,
        BattleVFX vfx,
        BattleAnimator anim,
        Inventory inventory)
    {
        UI.ModernUi.DrawBattle(this, screenWidth, screenHeight, playerPokemon, playerParty, enemyTrainerParty, inventory, battleMessage, vfx, anim);
    }

    /// <summary>How far a side's HP box is slid off screen: 0 = in place, 1 = fully out, -1 = hidden.</summary>
    internal static float BoxSlide(BattleAnimator anim, CombatantView view)
    {
        if (view.Shown == null || !view.Present) return -1f;

        float t = 1f;
        if (view.SendOutAge >= 0f) t = view.SendOutAge / BoxSlideTime;
        else if (view == anim.Enemy && anim.Time < 1.2f + BoxSlideTime) t = (anim.Time - 1.2f) / BoxSlideTime; // wild Pokémon, after the camera sweep

        t = Math.Clamp(t, 0f, 1f);
        return (1f - t) * (1f - t) * (1f - t);
    }
}
