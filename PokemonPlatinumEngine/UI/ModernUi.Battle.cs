using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The battle interface: HP boxes, the command buttons, the move menu, and the panels for switching and the bag.</summary>
internal static partial class ModernUi
{
    /// <summary>Draws the HP boxes, the move effects and whichever panel the battle menu is showing.</summary>
    public static void DrawBattle(BattleHUD hud, int sw, int sh, BattleEngine battle, Inventory inventory, string message, BattleVFX vfx, BattleAnimator anim)
    {
        var active = battle.PlayerPokemon;
        var party = battle.PlayerParty;
        bool choosing = hud.MenuState is BattleMenuState.Main or BattleMenuState.Moves or BattleMenuState.SelectTarget;

        if (battle.IsDouble)
        {
            // Two compact boxes a side: the foes' stacked at the top left, the player's above the menu on the right
            for (int slot = 0; slot < 2; slot++)
            {
                var foe = anim[BattleSide.Enemy, slot];
                float slide = BattleHUD.BoxSlide(anim, foe);
                if (slide >= 0f) CompactBox(56 + slot * 40 - 720 * slide, 36 + slot * 118, foe, false, false, -0.2f);
            }
            if (hud.MenuState != BattleMenuState.SwitchPokemon)
            {
                for (int slot = 0; slot < 2; slot++)
                {
                    var mine = anim[BattleSide.Player, slot];
                    float slide = BattleHUD.BoxSlide(anim, mine);
                    bool turn = choosing && battle.MenuBattler.Slot == slot;
                    if (slide >= 0f) CompactBox(1240 + slot * 36 + 760 * slide, 586 + slot * 128, mine, true, turn, 0.2f);
                }
            }
        }
        else
        {
            float enemySlide = BattleHUD.BoxSlide(anim, anim.Enemy);
            if (enemySlide >= 0f) EnemyBox(56 - 720 * enemySlide, 52, anim.Enemy, battle.EnemyParty);
            float playerSlide = BattleHUD.BoxSlide(anim, anim.Player);
            // The team's cards cover the player's box while switching
            if (playerSlide >= 0f && hud.MenuState != BattleMenuState.SwitchPokemon) PlayerBox(1240 + 760 * playerSlide, 650, anim.Player);
        }

        vfx.Draw();

        switch (hud.MenuState)
        {
            case BattleMenuState.Main:
                MessageBox(new Rectangle(48, 858, 1060, 172), null, active.DisplayName);
                string[] labels = { "BAG", "POKÉMON", "RUN" };
                Color[] colors = { Gold, Green, Blue };
                Button(new Rectangle(1140, 848, 432, 190), 34, Red, "FIGHT", 60, hud.MainMenuIndex == 0);
                for (int i = 0; i < 3; i++)
                    Button(new Rectangle(1600, 848 + i * 66, 272, 58), 29, colors[i], labels[i], 28, hud.MainMenuIndex == i + 1);
                break;
            case BattleMenuState.Moves:
                MoveMenu(hud, active);
                break;
            case BattleMenuState.SelectTarget:
                TargetMenu(hud, battle);
                break;
            case BattleMenuState.SwitchPokemon:
                SwitchPanel(hud, sw, sh, party, active, battle);
                break;
            case BattleMenuState.SelectBagItem:
                BagPanel(hud, inventory);
                break;
            default:
                MessageBox(new Rectangle(48, 858, 1824, 172), message, null);
                break;
        }
    }

    /// <summary>
    /// A double battle's HP box: name, level, status and HP bar (with the numbers and EXP for the player's). The
    /// Pokémon whose action is being chosen gets a glowing frame.
    /// </summary>
    private static void CompactBox(float x, float y, CombatantView view, bool mine, bool turn, float skew)
    {
        var p = view.Shown!;
        var r = new Rectangle(x, y, 560, mine ? 116 : 104);
        if (turn) UiShapes.Shadow(r, 20, 20, Vector2.Zero, Selection with { A = 200 }, skew);
        Panel(r, 20, skew, turn ? 5f : 4f, turn ? Selection : null);

        float pad = mine ? 46 : 34;
        float nameW = NameWithGender(p, x + pad, y + 32, 30);
        StatusPill(x + pad + nameW + 12, y + 17, p.Status, 28);
        Level(x + r.Width - 40, y + 32, p.Level, 30);
        HpBar(x + pad, y + 62, r.Width - pad - 44, 20, view.DisplayedHp / Math.Max(1, p.MaxHP));
        if (mine)
        {
            int hp = (int)MathF.Ceiling(view.DisplayedHp);
            string hpText = $"{hp} / {p.MaxHP}";
            float hw = UiFonts.Measure(hpText, 24, UiWeight.Black);
            UiFonts.DrawCentered(hpText, x + r.Width - 40 - hw, y + 94, 24, Ink, UiWeight.Black);
            ExpBar(new Rectangle(x + pad, y + r.Height - 18, 300, 7), view.DisplayedExp);
        }
    }

    /// <summary>
    /// Where to aim a move in a double battle: the four places as cards laid out like the field (foes on top), the
    /// ones that can be chosen lit, with the move described beside them.
    /// </summary>
    private static void TargetMenu(BattleHUD hud, BattleEngine battle)
    {
        var choices = battle.TargetChoices;
        var places = new[]
        {
            battle.EnemySlots.ElementAtOrDefault(1), battle.EnemySlots[0],
            battle.PlayerSlots[0], battle.PlayerSlots.ElementAtOrDefault(1)
        };
        for (int i = 0; i < 4; i++)
        {
            var r = new Rectangle(48 + (i % 2) * 556, 836 + (i / 2) * 104, 536, 96);
            var place = places[i];
            int choice = place == null ? -1 : IndexOf(choices, place);
            if (place?.Pokemon == null || place.Pokemon.IsFainted)
            {
                UiShapes.Shape(r, 26, new Color(226, 232, 242, 220), new Color(214, 222, 236, 220), new Color(180, 190, 210, 255), 3);
                continue;
            }

            bool selected = choice >= 0 && choice == hud.TargetMenuIndex;
            var accent = place.IsPlayerSide ? Blue : Red;
            if (choice < 0)
            {
                // The Pokémon using the move
                UiShapes.Shape(r, 26, new Color(232, 236, 244, 235), new Color(220, 226, 238, 235), new Color(186, 196, 214, 255), 3);
            }
            else
            {
                if (selected) UiShapes.Shadow(r, 26, 26, Vector2.Zero, accent with { A = 190 });
                else UiShapes.Shadow(r, 26, 14, new Vector2(0, 6), ShadowColor);
                UiShapes.Shape(r, 26, PanelTop, PanelBottom, selected ? Darker(accent, 0.1f) : Frame, selected ? 5f : 3f);
            }

            var p = place.Pokemon;
            Portrait(new Vector2(r.X + 56, r.Y + r.Height / 2f), 38, p, 1, selected);
            NameWithGender(p, r.X + 112, r.Y + 34, 30);
            Level(r.X + r.Width - 30, r.Y + 34, p.Level, 28);
            HpBar(r.X + 112, r.Y + 62, r.Width - 142, 18, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
            if (choice < 0) UiShapes.Fill(r, 26, new Color(40, 40, 60, 60));
        }

        var info = new Rectangle(1172, 836, 700, 200);
        Panel(info, 28);
        UiFonts.DrawCentered("TARGET", info.X + 44, info.Y + 44, 24, Muted, UiWeight.Black);
        HintsDark(info.X + info.Width - 24, info.Y + 18, ("X", "Back"));
        var move = battle.TargetingMove;
        if (move != null)
        {
            UiFonts.DrawCentered(move.Name, info.X + 44, info.Y + 108, 38, Ink, UiWeight.Black);
            var target = choices.ElementAtOrDefault(hud.TargetMenuIndex);
            if (target?.Pokemon != null)
                UiFonts.DrawCentered($"at {target.Name}", info.X + 44, info.Y + 156, 28, Muted, UiWeight.ExtraBold);
        }
    }

    private static int IndexOf(IReadOnlyList<Battler> list, Battler b)
    {
        for (int i = 0; i < list.Count; i++) if (list[i] == b) return i;
        return -1;
    }

    private static void EnemyBox(float x, float y, CombatantView view, Party? trainerParty)
    {
        var p = view.Shown!;
        var r = new Rectangle(x, y, 580, 120);

        // A trainer's team on a tray hanging under the box: a ball for each Pokémon (grey once it has fainted)
        // and a dot for each empty place
        if (trainerParty != null)
        {
            var tray = new Rectangle(x + 30, y + r.Height - 14, Party.MaxSize * 36 + 26, 62);
            UiShapes.Shadow(tray, 22, 14, new Vector2(0, 5), ShadowColor, -0.2f);
            UiShapes.Shape(tray, 22, Lighter(Frame, 0.08f), Frame, Darker(Frame, 0.3f), 3, -0.2f);
            var ball = PixelArtGenerator.GetBallTexture("Poké Ball");
            for (int i = 0; i < Party.MaxSize; i++)
            {
                var c = new Vector2(tray.X + 28 + i * 36, tray.Y + 40);
                if (i >= trainerParty.Count)
                {
                    UiShapes.Circle(c, 6, new Color(255, 255, 255, 50));
                    continue;
                }
                bool fainted = trainerParty.Members[i].IsFainted;
                Raylib.DrawTexturePro(ball, new Rectangle(0, 0, ball.Width, ball.Height), new Rectangle(c.X - 15, c.Y - 15, 30, 30),
                    Vector2.Zero, 0f, fainted ? new Color(110, 116, 140, 190) : Color.White);
            }
        }

        Panel(r, 22, skew: -0.2f);
        float nameW = NameWithGender(p, x + 38, y + 38, 36);
        StatusPill(x + 38 + nameW + 14, y + 22, p.Status, 30);
        Level(x + r.Width - 46, y + 38, p.Level, 34);
        HpBar(x + 38, y + 72, r.Width - 90, 24, view.DisplayedHp / Math.Max(1, p.MaxHP));
    }

    private static void PlayerBox(float x, float y, CombatantView view)
    {
        var p = view.Shown!;
        var r = new Rectangle(x, y, 628, 162);
        Panel(r, 22, skew: 0.2f);
        NameWithGender(p, x + 52, y + 40, 36);
        Level(x + r.Width - 40, y + 40, p.Level, 34);
        int hp = (int)MathF.Ceiling(view.DisplayedHp);
        HpBar(x + 52, y + 74, r.Width - 96, 24, view.DisplayedHp / Math.Max(1, p.MaxHP));
        string hpText = $"{hp} / {p.MaxHP}";
        float hw = UiFonts.Measure(hpText, 30, UiWeight.Black);
        UiFonts.DrawCentered(hpText, x + r.Width - 44 - hw, y + 122, 30, Ink, UiWeight.Black);
        StatusPill(x + 52, y + 106, p.Status, 30);

        // EXP runs along the bottom edge of the box
        ExpBar(new Rectangle(x + 40, y + r.Height - 18, 360, 8), view.DisplayedExp);
    }

    /// <summary>The message panel: either "What will X do?" or a message waiting for the A button (two lines if it is long).</summary>
    public static void MessageBox(Rectangle r, string? message, string? prompting)
    {
        Panel(r, 28);
        float x = r.X + 52, cy = r.Y + r.Height / 2f;
        if (prompting != null)
        {
            UiFonts.DrawCentered("What will", x, cy, 40, Ink, UiWeight.ExtraBold);
            float w = UiFonts.Measure("What will ", 40, UiWeight.ExtraBold);
            UiFonts.DrawCentered(prompting, x + w, cy, 40, Red, UiWeight.Black);
            float w2 = UiFonts.Measure(prompting + " ", 40, UiWeight.Black);
            UiFonts.DrawCentered("do?", x + w + w2, cy, 40, Ink, UiWeight.ExtraBold);
            return;
        }

        var lines = Wrap(message ?? "", r.Width - 190, 40, UiWeight.ExtraBold);
        if (lines.Count <= 1) UiFonts.DrawCentered(message ?? "", x, cy, 40, Ink, UiWeight.ExtraBold);
        else
        {
            UiFonts.DrawCentered(lines[0], x, cy - 28, 40, Ink, UiWeight.ExtraBold);
            UiFonts.DrawCentered(lines[1], x, cy + 28, 40, Ink, UiWeight.ExtraBold);
        }
        AdvanceArrow(r.X + r.Width - 64, r.Y + r.Height - 52);
    }

    private static void MoveMenu(BattleHUD hud, Pokemon p)
    {
        for (int i = 0; i < 4; i++)
        {
            var r = new Rectangle(48 + (i % 2) * 556, 848 + (i / 2) * 100, 536, 88);
            bool selected = hud.MoveMenuIndex == i;
            if (i >= p.Moves.Count)
            {
                UiShapes.Shape(r, 26, new Color(226, 232, 242, 220), new Color(214, 222, 236, 220), new Color(180, 190, 210, 255), 3);
                UiFonts.DrawCentered("—", r.X + 40, r.Y + r.Height / 2f, 32, Muted, UiWeight.Black);
                continue;
            }
            var move = p.Moves[i];
            var type = Palette.GetTypeColor(move.Type.ToString());
            if (selected) UiShapes.Shadow(r, 26, 26, Vector2.Zero, type with { A = 170 });
            else UiShapes.Shadow(r, 26, 14, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(r, 26, PanelTop, PanelBottom, selected ? Darker(type, 0.15f) : Frame, selected ? 5f : 3f);
            var band = new Rectangle(r.X + 10, r.Y + 10, 124, r.Height - 20);
            UiShapes.Shape(band, 18, Lighter(type, 0.1f), Darker(type, 0.1f));
            string typeName = move.Type.ToString().ToUpperInvariant();
            float tw = UiFonts.Measure(typeName, 20, UiWeight.Black);
            UiFonts.DrawCentered(typeName, band.X + (band.Width - tw) / 2f, band.Y + band.Height / 2f, 20, Color.White, UiWeight.Black);
            UiFonts.DrawCentered(move.Name, r.X + 156, r.Y + r.Height / 2f, 32, move.CurrentPP > 0 ? Ink : Muted, UiWeight.Black);
            string pp = $"{move.CurrentPP}/{move.MaxPP}";
            float pw = UiFonts.Measure(pp, 26, UiWeight.Black);
            UiFonts.DrawCentered(pp, r.X + r.Width - 30 - pw, r.Y + r.Height / 2f, 26, move.CurrentPP > 0 ? Ink : Red, UiWeight.Black);
            UiFonts.DrawCentered("PP", r.X + r.Width - 40 - pw - UiFonts.Measure("PP", 18, UiWeight.ExtraBold), r.Y + r.Height / 2f + 3, 18, Muted, UiWeight.ExtraBold);
        }

        var info = new Rectangle(1172, 848, 700, 188);
        Panel(info, 28);
        if (hud.MoveMenuIndex < p.Moves.Count)
        {
            var move = p.Moves[hud.MoveMenuIndex];
            string cat = move.Category.ToString().ToUpperInvariant();
            UiFonts.DrawCentered(cat, info.X + 44, info.Y + 44, 24, Muted, UiWeight.Black);
            string pwr = move.Power > 0 ? move.Power.ToString() : "—";
            string acc = move.Accuracy > 0 ? move.Accuracy.ToString() : "—";
            UiFonts.DrawCentered("POWER", info.X + 260, info.Y + 44, 20, Muted, UiWeight.ExtraBold);
            UiFonts.DrawCentered(pwr, info.X + 350, info.Y + 44, 30, Ink, UiWeight.Black);
            UiFonts.DrawCentered("ACCURACY", info.X + 440, info.Y + 44, 20, Muted, UiWeight.ExtraBold);
            UiFonts.DrawCentered(acc, info.X + 580, info.Y + 44, 30, Ink, UiWeight.Black);
            DrawWrapped(move.Description, info.X + 44, info.Y + 92, info.Width - 88, 26, Ink, 36);
        }
    }

    // ------------------------------------------------------------------ switching

    /// <summary>The team as six cards over the dimmed field, three to a row.</summary>
    private static void SwitchPanel(BattleHUD hud, int sw, int sh, Party party, Pokemon active, BattleEngine battle)
    {
        Dim(sw, sh, 120);

        bool forced = active.IsFainted;
        var head = new Rectangle(48, 548, 1824, 84);
        Panel(head, 26);
        UiFonts.DrawCentered(forced ? $"{active.DisplayName} fainted! Who goes next?" : "Send out which Pokémon?", head.X + 48, head.Y + head.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        if (forced) HintsDark(head.X + head.Width - 24, head.Y + 16, ("Z", "Send out"));
        else HintsDark(head.X + head.Width - 24, head.Y + 16, ("Z", "Send out"), ("X", "Back"));

        for (int i = 0; i < Party.MaxSize; i++)
        {
            var r = new Rectangle(48 + (i % 3) * 616, 648 + (i / 3) * 196, 592, 180);
            if (i >= party.Count)
            {
                EmptySlot(r, 30);
                continue;
            }

            var p = party.Members[i];
            bool selected = hud.SwitchMenuIndex == i;
            Card(r, 30, selected);
            Portrait(new Vector2(r.X + 96, r.Y + r.Height / 2f), 68, p, 2, selected);

            float x = r.X + 184;
            NameWithGender(p, x, r.Y + 46, 34);
            Level(r.X + r.Width - 34, r.Y + 46, p.Level, 32);
            HpBar(x, r.Y + 76, r.Width - 184 - 34, 22, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
            string hpText = $"{p.CurrentHP} / {p.MaxHP}";
            UiFonts.DrawCentered(hpText, r.X + r.Width - 34 - UiFonts.Measure(hpText, 26, UiWeight.Black), r.Y + 136, 26, Ink, UiWeight.Black);

            float tagX = x;
            if (battle.PlayerSlots.Any(b => b.Pokemon == p) && !p.IsFainted) tagX += Tag(tagX, r.Y + 120, "IN BATTLE", Blue, 32) + 8;
            StatusPill(tagX, r.Y + 120, p.IsFainted ? StatusCondition.Faint : p.Status, 32);
            if (p.IsFainted) UiShapes.Fill(r, 30, new Color(40, 40, 60, 90));
        }
    }

    /// <summary>Key hints on a light panel (dark pills instead of the translucent ones used over backdrops).</summary>
    private static void HintsDark(float rightX, float y, params (string Key, string Label)[] hints)
    {
        float x = rightX;
        for (int i = hints.Length - 1; i >= 0; i--)
        {
            float width = HintWidth(hints[i].Key, hints[i].Label);
            x -= width;
            UiShapes.Fill(new Rectangle(x, y, width, 52), 26, Frame);
            HintPill(x, y, hints[i].Key, hints[i].Label);
            x -= 16;
        }
    }

    // ------------------------------------------------------------------ bag

    /// <summary>The items usable in battle as four cards, with the chosen one described beside them (like the move menu).</summary>
    private static void BagPanel(BattleHUD hud, Inventory inventory)
    {
        IReadOnlyList<string> names = BattleEngine.BagItems;
        ItemData? chosen = null;
        for (int i = 0; i < names.Count; i++)
        {
            var r = new Rectangle(48 + (i % 2) * 556, 836 + (i / 2) * 104, 536, 96);
            bool selected = hud.BagMenuIndex == i;
            var item = ItemDatabase.Get(names[i]);
            int quantity = item != null ? inventory.GetQuantity(item) : 0;
            if (selected) chosen = item;

            if (selected) UiShapes.Shadow(r, 26, 26, Vector2.Zero, Gold with { A = 190 });
            else UiShapes.Shadow(r, 26, 14, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(r, 26, PanelTop, PanelBottom, selected ? Darker(Gold, 0.12f) : Frame, selected ? 5f : 3f);

            if (item != null)
            {
                var icon = PixelArtGenerator.GetItemIcon(item);
                UiShapes.Circle(new Vector2(r.X + 58, r.Y + r.Height / 2f), 38, new Color(226, 234, 246, 255));
                Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(r.X + 28, r.Y + 18, 60, 60),
                    Vector2.Zero, 0f, quantity > 0 ? Color.White : new Color(255, 255, 255, 110));
            }
            UiFonts.DrawCentered(names[i], r.X + 116, r.Y + r.Height / 2f, 32, quantity > 0 ? Ink : Muted, UiWeight.Black);
            string count = $"×{quantity}";
            UiFonts.DrawCentered(count, r.X + r.Width - 34 - UiFonts.Measure(count, 30, UiWeight.Black), r.Y + r.Height / 2f, 30, quantity > 0 ? Ink : Muted, UiWeight.Black);
        }

        var info = new Rectangle(1172, 836, 700, 200);
        Panel(info, 28);
        UiFonts.DrawCentered("BAG", info.X + 44, info.Y + 44, 24, Muted, UiWeight.Black);
        HintsDark(info.X + info.Width - 24, info.Y + 18, ("X", "Back"));
        if (chosen != null) DrawWrapped(chosen.Description, info.X + 44, info.Y + 92, info.Width - 88, 26, Ink, 36);
    }
}
