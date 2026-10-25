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
    /// <summary>The battle's bag: its four pockets and the item used last (plan 06 · R11).</summary>
    SelectBagPocket,
    /// <summary>The items of the pocket opened.</summary>
    SelectBagItem,
    /// <summary>The Pokémon an item from the bag is for.</summary>
    SelectBagTarget,
    /// <summary>The move of that Pokémon's an item restores (an Ether, a Leppa Berry).</summary>
    SelectBagMove,
    /// <summary>Choosing which Pokémon a move is aimed at (double battles).</summary>
    SelectTarget,
    Message,
    /// <summary>A Pokémon wants a fifth move: a question of Yes and No, or which move to forget (plan 06 · R10).</summary>
    LearnMove,
    /// <summary>The information panel over the main menu (plan 12 · Q11): the field's state and each Pokémon's, as far as the player has been told.</summary>
    Info,
    /// <summary>A Pokémon of the team's summary, opened from the switching cards (plan 08 · P4).</summary>
    Summary
}

public class BattleHUD
{
    private const float BoxSlideTime = 0.35f;

    public BattleMenuState MenuState { get; set; } = BattleMenuState.Main;
    public int MainMenuIndex { get; set; } = 0;
    public int MoveMenuIndex { get; set; } = 0;
    public int SwitchMenuIndex { get; set; } = 0;
    public int BagMenuIndex { get; set; } = 0;

    /// <summary>The cursor on the bag's pockets (4: the item used last), the first row of a pocket's list that shows, and the cursors on the team and on a Pokémon's moves.</summary>
    public int BagPocketIndex { get; set; } = 0;
    public int BagFirstRow { get; set; } = 0;
    public int BagTargetIndex { get; set; } = 0;
    public int BagMoveIndex { get; set; } = 0;
    public int TargetMenuIndex { get; set; } = 0;

    /// <summary>
    /// The level-up panel over the message box (plan 08 · P5), while a Pokémon's level is being shown: the battle
    /// puts it up with the level's line and takes it down once its two pages have been read. Null otherwise.
    /// </summary>
    public UI.Kit.LevelUpPanel? LevelUp { get; set; }

    /// <summary>Draws the HP boxes and the bottom panel over the battle field.</summary>
    /// <param name="anim">What the field shows: the HP boxes follow the Pokémon on the platforms and their draining bars.</param>
    public void Draw(int screenWidth, int screenHeight, BattleEngine battle, string battleMessage, BattleAnimator anim, Inventory inventory)
    {
        UI.ModernUi.DrawBattle(this, screenWidth, screenHeight, battle, inventory, battleMessage, anim);
    }

    /// <summary>How far a side's HP box is slid off screen: 0 = in place, 1 = fully out, -1 = hidden.</summary>
    internal static float BoxSlide(BattleAnimator anim, CombatantView view)
    {
        if (view.Shown == null || !view.Present) return -1f;

        float t = 1f;
        if (view.SendOutAge >= 0f) t = view.SendOutAge / BoxSlideTime;
        // An ally that answered a call (plan 14 · B5): its box slides in as it hops on
        else if (view.ArriveAge >= 0f) t = view.ArriveAge / BoxSlideTime;
        else if (OnTheFoesSide(anim, view) && anim.Time < 1.2f + BoxSlideTime) t = (anim.Time - 1.2f) / BoxSlideTime; // wild Pokémon, after the camera sweep

        t = Math.Clamp(t, 0f, 1f);
        return (1f - t) * (1f - t) * (1f - t);
    }

    /// <summary>Whether a view is one of the foes' places: any of a horde's five (plan 14 · B4).</summary>
    private static bool OnTheFoesSide(BattleAnimator anim, CombatantView view)
    {
        for (int slot = 0; slot < anim.SlotsOf(BattleSide.Enemy); slot++)
            if (view == anim[BattleSide.Enemy, slot]) return true;
        return false;
    }
}
