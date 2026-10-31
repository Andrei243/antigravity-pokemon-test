using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The battle interface: HP boxes, the command buttons, the move menu, and the panels for switching and the bag.</summary>
internal static partial class ModernUi
{
    /// <summary>Draws the HP boxes and whichever panel the battle menu is showing.</summary>
    /// <summary>
    /// The battle's four commands: FIGHT and BAG, POKÉMON, RUN; in the Great Marsh the Safari Ball (with how many are
    /// left), BAIT, MUD and RUN; in Pal Park the Park Ball and RUN (an empty label is a command that isn't there).
    /// </summary>
    /// <summary>A command that can't be used in this battle (plan 14 · B14: BAG in a Master Trainer's battle).</summary>
    public static readonly Color Closed = new(150, 156, 174, 255);

    public static (string Big, string[] Small) CommandLabels(BattleEngine battle) => battle.Kind switch
    {
        BattleKind.Safari => (Text.Of("battleMenu.safariBall", ("n", battle.SpecialBalls)), new[] { Text.Of("battleMenu.bait"), Text.Of("battleMenu.mud"), Text.Of("battleMenu.run") }),
        BattleKind.PalPark => (Text.Of("battleMenu.parkBall", ("n", battle.SpecialBalls)), new[] { "", "", Text.Of("battleMenu.run") }),
        // The Bug-Catching Contest's Sport Balls stand where the bag does (plan 15 · E5)
        _ when battle.OnlyBall != null => (Text.Of("battleMenu.fight"), new[] { Text.Of("battleMenu.sportBall", ("n", battle.SpecialBalls)), Text.Of("battleMenu.pokemon"), Text.Of("battleMenu.run") }),
        // At an end of a triple battle's side SHIFT stands where RUN does (plan 14 · B6)
        _ when battle.OffersShift => (Text.Of("battleMenu.fight"), new[] { Text.Of("battleMenu.bag"), Text.Of("battleMenu.pokemon"), Text.Of("battleMenu.shift") }),
        _ => (Text.Of("battleMenu.fight"), new[] { Text.Of("battleMenu.bag"), Text.Of("battleMenu.pokemon"), Text.Of("battleMenu.run") })
    };

    /// <summary>
    /// A rotation battle's arrows beside FIGHT (plan 14 · B7; style guide, "Battle panels", "The rotation arrows"): a
    /// disc on each end of the FIGHT button, inset 40, with an arrow pointing out, the one the turntable is armed toward
    /// lit gold; and while it is turned, the name of the Pokémon brought forward along the button's foot.
    /// </summary>
    public static void RotationArrows(Rectangle fight, int armed, string fighter)
    {
        float cy = fight.Y + fight.Height / 2f;
        for (int way = -1; way <= 1; way += 2)
        {
            float cx = way < 0 ? fight.X + 40 : fight.X + fight.Width - 40;
            bool lit = armed == way;
            UiShapes.Circle(new Vector2(cx, cy), 26, lit ? Gold : new Color(255, 255, 255, 230));
            float tip = cx + way * 12, back = cx - way * 8;
            var color = lit ? Ink : Red;
            if (way > 0) UiShapes.Triangle(new Vector2(back, cy - 13), new Vector2(back, cy + 13), new Vector2(tip, cy), color, 2f);
            else UiShapes.Triangle(new Vector2(tip, cy), new Vector2(back, cy + 13), new Vector2(back, cy - 13), color, 2f);
        }
        if (armed == 0) return;
        float w = UiFonts.Measure(fighter, 26, UiWeight.Black);
        UiFonts.DrawCentered(fighter, fight.X + (fight.Width - w) / 2f, fight.Y + fight.Height - 30, 26, Color.White, UiWeight.Black);
    }

    public static void DrawBattle(BattleHUD hud, int sw, int sh, BattleEngine battle, Inventory inventory, string message, BattleAnimator anim)
    {
        var active = battle.PlayerPokemon;
        var party = battle.PlayerParty;
        bool choosing = hud.MenuState is BattleMenuState.Main or BattleMenuState.Moves or BattleMenuState.SelectTarget;
        // A level's panel stands where the player's HP box is, as a move to learn does (plan 08 · P5)
        bool boxCovered = CoversPlayerBox(hud.MenuState) || (hud.LevelUp != null && hud.MenuState == BattleMenuState.Message);
        // The information panel dims the stage under the foes' boxes, which stay bright over it
        if (hud.MenuState == BattleMenuState.Info) Dim(sw, sh, 150);

        if (battle.IsSos)
        {
            // An SOS battle (plan 14 · B5): the caller and its ally on compact boxes as a double's foes, the player's own as in a single
            for (int slot = 0; slot < 2; slot++)
            {
                var foe = anim[BattleSide.Enemy, slot];
                float slide = BattleHUD.BoxSlide(anim, foe);
                if (slide >= 0f) CompactBox(56 + slot * 40 - 720 * slide, 36 + slot * 118, foe, false, false, -0.2f, battle.ShowsCaughtMark(foe.Shown!));
            }
            float playerSlide = BattleHUD.BoxSlide(anim, anim.Player);
            if (playerSlide >= 0f && !boxCovered) PlayerBox(1240 + 760 * playerSlide, 650, anim.Player);
        }
        else if (battle.IsDouble)
        {
            // Two compact boxes a side: the foes' stacked at the top left, the player's above the menu on the right
            for (int slot = 0; slot < 2; slot++)
            {
                var foe = anim[BattleSide.Enemy, slot];
                float slide = BattleHUD.BoxSlide(anim, foe);
                if (slide >= 0f) CompactBox(56 + slot * 40 - 720 * slide, 36 + slot * 118, foe, false, false, -0.2f, battle.ShowsCaughtMark(foe.Shown!));
            }
            if (!boxCovered)
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
        else if (battle.IsTriple)
        {
            // A triple battle (plan 14 · B6): the foes' three small boxes stacked down the left as a horde's, the player's
            // three down the right above the commands, the one whose action is being chosen lit
            if (hud.MenuState != BattleMenuState.Info)
                for (int slot = 0; slot < anim.SlotsOf(BattleSide.Enemy); slot++)
                {
                    var foe = anim[BattleSide.Enemy, slot];
                    float slide = BattleHUD.BoxSlide(anim, foe);
                    var at = HordeBoxAt(slot);
                    if (slide >= 0f) HordeBox(at.X - 640 * slide, at.Y, foe, battle.ShowsCaughtMark(foe.Shown!));
                }
            if (!boxCovered && hud.MenuState != BattleMenuState.Info)
                for (int slot = 0; slot < anim.SlotsOf(BattleSide.Player); slot++)
                {
                    var mine = anim[BattleSide.Player, slot];
                    float slide = BattleHUD.BoxSlide(anim, mine);
                    var at = TripleBoxAt(slot);
                    if (slide >= 0f) TripleBox(at.X + 640 * slide, at.Y, mine, choosing && battle.MenuBattler.Slot == slot);
                }
        }
        else if (battle.IsHorde)
        {
            // A horde (plan 14 · B4): the foes' small boxes stacked down the left, the player's own box as in a single.
            // The information panel shows every foe on a card of its own, so the boxes stand back while it is open
            if (hud.MenuState != BattleMenuState.Info)
                for (int slot = 0; slot < anim.SlotsOf(BattleSide.Enemy); slot++)
                {
                    var foe = anim[BattleSide.Enemy, slot];
                    float slide = BattleHUD.BoxSlide(anim, foe);
                    var at = HordeBoxAt(slot);
                    if (slide >= 0f) HordeBox(at.X - 640 * slide, at.Y, foe, battle.ShowsCaughtMark(foe.Shown!));
                }
            float playerSlide = BattleHUD.BoxSlide(anim, anim.Player);
            if (playerSlide >= 0f && !boxCovered) PlayerBox(1240 + 760 * playerSlide, 650, anim.Player);
        }
        else
        {
            float enemySlide = BattleHUD.BoxSlide(anim, anim.Enemy);
            // A Mega Max team's ace once it has Mega Dynamaxed: the boss box (plan 25 · YD6)
            float boss = enemySlide == 0f ? battle.BossShare(anim.Enemy) : 0f;
            if (boss > 0f) BossBox(anim.Enemy, boss, battle.EnemyParty, Text.Of("battle.fullTitle", ("class", Trainer.ClassName("Champion")), ("name", PlayerIdentity.Fill(battle.Trainers[0].Name))));
            else if (enemySlide >= 0f) EnemyBox(56 - 720 * enemySlide, 52, anim.Enemy, battle.EnemyParty, battle.ShowsCaughtMark(anim.Enemy.Shown!));
            float playerSlide = BattleHUD.BoxSlide(anim, anim.Player);
            // The team's cards cover the player's box while switching, and a question about a move to learn takes its place
            if (playerSlide >= 0f && !boxCovered) PlayerBox(1240 + 760 * playerSlide, 650, anim.Player);
        }

        switch (hud.MenuState)
        {
            case BattleMenuState.Main:
                // The Great Marsh and Pal Park are played with balls alone: the commands are the ball and what else can be thrown
                bool balls = battle.Kind is BattleKind.Safari or BattleKind.PalPark;
                // In a rotation battle the box names the one the turntable is turned to (plan 14 · B7)
                MessageBox(new Rectangle(48, 858, 1060, 172), null, balls ? PlayerIdentity.Name : battle.IsRotation ? battle.Fighter.DisplayName : active.DisplayName);
                var (big, labels) = CommandLabels(battle);
                Color[] colors = { Gold, Green, Blue };
                Button(new Rectangle(1140, 848, 432, 190), balls ? 30 : 34, Red, big, balls ? 40 : 60, hud.MainMenuIndex == 0);
                if (battle.IsRotation) RotationArrows(new Rectangle(1140, 848, 432, 190), battle.RotationArmed, battle.Fighter.DisplayName);
                // The bag closed (plan 14 · B14): BAG stands greyed in its place, and choosing it says why
                if (battle.BagClosed) colors[0] = Closed;
                for (int i = 0; i < 3; i++)
                    if (labels[i].Length > 0) Button(new Rectangle(1600, 848 + i * 66, 272, 58), 29, colors[i], labels[i], 28, hud.MainMenuIndex == i + 1);
                break;
            case BattleMenuState.Moves:
                // A rotation battle's moves are those of the one the turntable is turned to (plan 14 · B7)
                MoveMenu(hud, battle.IsRotation ? battle.Fighter : active, battle);
                break;
            case BattleMenuState.SelectTarget:
                TargetMenu(hud, battle);
                break;
            case BattleMenuState.SwitchPokemon:
                SwitchPanel(hud, sw, sh, party, active, battle);
                break;
            case BattleMenuState.SelectBagPocket:
                BagPockets(hud, sw, sh, battle);
                break;
            case BattleMenuState.SelectBagItem:
                BagPanel(hud, sw, sh, battle);
                break;
            case BattleMenuState.SelectBagTarget:
                BagTargets(hud, sw, sh, party, battle);
                break;
            case BattleMenuState.SelectBagMove:
                BagTargets(hud, sw, sh, party, battle);
                if (battle.BagTarget >= 0 && battle.BagTarget < party.Count && battle.BagItemChosen is { } restoring)
                    MovePick(new Rectangle(sw - 64 - 760, 64, 760, 476), party.Members[battle.BagTarget], hud.BagMoveIndex, Text.Of("battleMenu.useOnWhichMove", ("item", restoring.DisplayName)));
                break;
            case BattleMenuState.Info:
                if (battle.InfoShown is { } info) InfoPanel(info, battle.IsDouble || battle.IsSos);
                break;
            case BattleMenuState.Summary:
                if (battle.SummaryIndex < party.Count)
                    DrawSummary(sw, sh, party.Members[battle.SummaryIndex], battle.SummaryIndex, party.Count, 1f, (int)battle.Summary.Page,
                        battle.Summary.RibbonCursor, battle.Summary.MarkCursor, battle.Summary.MoveSelection, canMark: false, heldAt: battle.Core.RunLevelCap);
                break;
            case BattleMenuState.LearnMove:
                // The question in the message box, and beside it its Yes and No or the moves to forget
                MessageBox(new Rectangle(48, 858, MessageBoxWidth, 172), message, null);
                if (battle.LearnStep == LearnStep.Choose && battle.Learner is { } learner && battle.MoveToLearn is { } wanted)
                    MoveChoice(new Rectangle(sw - 64 - 820, 64, 820, 716), learner, wanted, battle.LearnCursor, 1f);
                else
                    // 22 above the message box, its right edge on the box's
                    DrawChoices(sw + 48, sh + 40, new[] { Text.Of("battleMenu.yes"), Text.Of("battleMenu.no") }, battle.LearnCursor, 1f);
                break;
            default:
                // No arrow while a growth holds the lines (plan 25 · YD6): a press would do nothing then
                MessageBox(new Rectangle(48, 858, MessageBoxWidth, 172), message, null, battle.CurrentSpeaker, arrow: !battle.HeldForAGrowth);
                // A level's panel stands on the message box's right end, over the line that tells of the level (plan 08 · P5)
                if (hud.LevelUp is { } level) LevelUp(48 + 1824, 858 - 22, level);
                // The Battle Palace's commands stand closed, and the Battle Arena's judge shows its card (plan 14 · B9)
                if (battle.Palace) PalaceClosed();
                if (battle.JudgeCard is { } verdict) JudgeCard(verdict, anim.Player.Shown?.DisplayName ?? "", anim.Enemy.Shown?.DisplayName ?? "");
                break;
        }
    }

    // ------------------------------------------------------------------ the level-up panel

    /// <summary>The level-up panel's size (style guide, "Battle panels", "A level").</summary>
    public const float LevelUpWidth = 560, LevelUpHeight = 484;

    /// <summary>The six stats as the level-up panel names them, in <see cref="LevelUpPanel.Order"/>: the first row is the most HP, not the HP left.</summary>
    private static string LevelUpName(StatType stat) => stat == StatType.HP ? Text.Of("battleMenu.maxHp") : Words.Stat(stat);

    /// <summary>
    /// The level-up panel (plan 08 · P5; style guide, "Battle panels", "A level"), its right edge at
    /// <paramref name="right"/> and its foot at <paramref name="foot"/>: whose level it is and the level reached,
    /// then the six stats with what the level added to each (the first page) or as they now stand (the second).
    /// A battle draws it over its message box and the bag over the team's cards.
    /// </summary>
    public static void LevelUp(float right, float foot, LevelUpPanel panel)
    {
        float slide = (1f - UiMotion.EaseOut(panel.Appear)) * 80f;
        var r = new Rectangle(right - LevelUpWidth + slide, foot - LevelUpHeight, LevelUpWidth, LevelUpHeight);
        Panel(r, 30);

        // The head: its icon, its name and the level it reached; a long name is set smaller to keep clear of the level
        var p = panel.Pokemon;
        Portrait(new Vector2(r.X + 76, r.Y + 70), 44, p, 2, false);
        float nameX = r.X + 140, size = 34;
        float levelW = UiFonts.Measure(panel.Level.ToString(), 34, UiWeight.Black) + UiFonts.Measure(Text.Of("party.level"), 34 * 0.62f, UiWeight.ExtraBold) + 4;
        float room = r.X + r.Width - 40 - levelW - 16 - nameX - 34 * 0.9f;
        float nameW = UiFonts.Measure(p.DisplayName, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        NameWithGender(p, nameX, r.Y + 70, size);
        Level(r.X + r.Width - 40, r.Y + 70, panel.Level, 34);
        UiShapes.Fill(new Rectangle(r.X + 36, r.Y + 132, r.Width - 72, 3), 1.5f, Rule);

        // The rows: a caption over the numbers says which page this is
        bool gains = panel.Page == LevelUpPage.Gains;
        string caption = Text.Of(gains ? "battleMenu.levelUp.gained" : "battleMenu.levelUp.now");
        UiFonts.Draw(caption, r.X + r.Width - 44 - UiFonts.Measure(caption, 20, UiWeight.Black), r.Y + 148, 20, Muted, UiWeight.Black);
        for (int i = 0; i < LevelUpPanel.Order.Count; i++)
        {
            var stat = LevelUpPanel.Order[i];
            float cy = r.Y + 204 + i * 48;
            UiFonts.DrawCentered(LevelUpName(stat), r.X + 44, cy, 30, Ink, UiWeight.ExtraBold);
            int gain = panel.Gain(stat);
            string number = !gains ? panel.Value(stat).ToString() : gain >= 0 ? $"+{gain}" : gain.ToString();
            var color = !gains ? Ink : gain > 0 ? Green : gain < 0 ? Red : Muted;
            UiFonts.DrawCentered(number, r.X + r.Width - 44 - UiFonts.Measure(number, 32, UiWeight.Black), cy, 32, color, UiWeight.Black);
        }
    }

    /// <summary>
    /// A double battle's HP box: name, level, status and HP bar (with the numbers and EXP for the player's). The
    /// Pokémon whose action is being chosen gets a glowing frame.
    /// </summary>
    private static void CompactBox(float x, float y, CombatantView view, bool mine, bool turn, float skew, bool caught = false)
    {
        var p = view.Shown!;
        var (jolt, ghost, thump) = PunchOf(view);
        x += jolt.X;
        y += jolt.Y;
        var r = new Rectangle(x, y, 560, mine ? 116 : 104);
        if (turn) UiShapes.Shadow(r, 20, 20, Vector2.Zero, Selection with { A = 200 }, skew);
        Panel(r, 20, skew, turn ? 5f : 4f, turn ? Selection : null);

        float pad = mine ? 46 : 34;
        float nameW = NameWithGender(p, x + pad, y + 32, 30);
        if (caught) nameW += CaughtMark(x + pad + nameW, y + 32, 30);
        StatusPill(x + pad + nameW + 12, y + 17, p.Status, 28);
        Level(x + r.Width - 40, y + 32, p.Level, 30);
        HpBar(x + pad, y + 62, r.Width - pad - 44, 20, view.DisplayedHp / Math.Max(1, p.MaxHP), ghost);
        if (mine)
        {
            int hp = (int)MathF.Ceiling(view.DisplayedHp);
            string hpText = $"{hp} / {p.MaxHP}";
            float hw = UiFonts.Measure(hpText, 24 * thump, UiWeight.Black);
            UiFonts.DrawCentered(hpText, x + r.Width - 40 - hw, y + 94, 24 * thump, Ink, UiWeight.Black);
            ExpBar(new Rectangle(x + pad, y + r.Height - 18, 300, 7), view.DisplayedExp);
        }
        GiantTags(r, view);
    }

    /// <summary>A horde's small HP box (plan 14 · B4; style guide, "Battle panels", "A horde's boxes").</summary>
    public const float HordeBoxWidth = 440, HordeBoxHeight = 86;

    /// <summary>
    /// Where a horde's foe's box stands: stacked down the left from the top, each a step to the right of the one over
    /// it, as a double's two are; the fifth ends above the middle of the picture, clear of the message box.
    /// </summary>
    public static Vector2 HordeBoxAt(int slot) => new(48 + slot * 12, 28 + slot * 96);

    /// <summary>A horde's foe's box: its name, condition and level over its HP bar, smaller than a double's.</summary>
    private static void HordeBox(float x, float y, CombatantView view, bool caught)
    {
        var p = view.Shown!;
        var (jolt, ghost, _) = PunchOf(view);
        x += jolt.X;
        y += jolt.Y;
        var r = new Rectangle(x, y, HordeBoxWidth, HordeBoxHeight);
        Panel(r, 18, -0.2f, 4f, null);
        float nameW = NameWithGender(p, x + 30, y + 27, 26);
        if (caught) nameW += CaughtMark(x + 30 + nameW, y + 27, 26);
        StatusPill(x + 30 + nameW + 10, y + 14, p.Status, 24);
        Level(x + r.Width - 34, y + 27, p.Level, 26);
        HpBar(x + 30, y + 52, r.Width - 30 - 38, 16, view.DisplayedHp / Math.Max(1, p.MaxHP), ghost);
        GiantTags(r, view);
    }

    /// <summary>
    /// Where the player's box of a triple battle stands (plan 14 · B6; style guide, "Battle panels", "A triple's boxes"):
    /// three down the right above the commands, each 12 to the left of the one over it, the third ending 10 above the
    /// target menu's cards.
    /// </summary>
    public static Vector2 TripleBoxAt(int slot) => new(1384 - slot * 12, 548 + slot * 96);

    /// <summary>
    /// The player's own small box in a triple battle: a horde's box with the HP's numbers after its bar, leaning the
    /// player's way, and lit for the Pokémon whose action is being chosen.
    /// </summary>
    private static void TripleBox(float x, float y, CombatantView view, bool turn)
    {
        var p = view.Shown!;
        var (jolt, ghost, thump) = PunchOf(view);
        x += jolt.X;
        y += jolt.Y;
        var r = new Rectangle(x, y, HordeBoxWidth, HordeBoxHeight);
        if (turn) UiShapes.Shadow(r, 18, 18, Vector2.Zero, Selection with { A = 200 }, 0.2f);
        Panel(r, 18, 0.2f, turn ? 5f : 4f, turn ? Selection : null);
        float nameW = NameWithGender(p, x + 34, y + 27, 26);
        StatusPill(x + 34 + nameW + 10, y + 14, p.Status, 24);
        Level(x + r.Width - 34, y + 27, p.Level, 26);
        int hp = (int)MathF.Ceiling(view.DisplayedHp);
        string hpText = $"{hp}/{p.MaxHP}";
        float hw = UiFonts.Measure(hpText, 20, UiWeight.Black);
        HpBar(x + 34, y + 52, r.Width - 34 - 34 - hw - 14, 16, view.DisplayedHp / Math.Max(1, p.MaxHP), ghost);
        UiFonts.DrawCentered(hpText, x + r.Width - 34 - UiFonts.Measure(hpText, 20 * thump, UiWeight.Black), y + 60, 20 * thump, Ink, UiWeight.Black);
        GiantTags(r, view);
    }

    /// <summary>
    /// Where a card of the target menu stands (style guide, "Battle panels"): two to a row in a double, as ever, and a
    /// horde's five foes in a row over the player's one, the cards narrower to fit the same width.
    /// </summary>
    public static Rectangle TargetCard(int index, int columns)
    {
        if (columns <= 2) return new Rectangle(48 + (index % 2) * 556, 836 + (index / 2) * 104, 536, 96);
        const float gap = 12f, across = 1092f;
        float width = (across - gap * (columns - 1)) / columns;
        return new Rectangle(48 + (index % columns) * (width + gap), 836 + (index / columns) * 104, width, 96);
    }

    /// <summary>
    /// Where to aim a move in a double battle or a horde: the places as cards laid out like the field (foes on top),
    /// the ones that can be chosen lit, with the move described beside them.
    /// </summary>
    private static void TargetMenu(BattleHUD hud, BattleEngine battle)
    {
        var choices = battle.TargetChoices;
        var places = battle.TargetLayout;
        int columns = battle.TargetColumns;
        bool narrow = columns > 2;
        for (int i = 0; i < places.Count; i++)
        {
            var r = TargetCard(i, columns);
            var place = places[i];
            int choice = place == null ? -1 : IndexOf(choices, place);
            // A horde's row of one has no empty cards beside the player's
            if (narrow && place == null) continue;
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
            if (narrow)
            {
                // A horde's narrow card: the portrait, the name fitted to the card, the HP bar; no level
                Portrait(new Vector2(r.X + 40, r.Y + r.Height / 2f), 28, p, 1, selected);
                float fits = Math.Min(24f, 24f * (r.Width - 96) / Math.Max(1f, UiFonts.Measure(p.DisplayName, 24, UiWeight.Black)));
                NameWithGender(p, r.X + 76, r.Y + 32, fits);
                HpBar(r.X + 76, r.Y + 58, r.Width - 96, 14, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
            }
            else
            {
                Portrait(new Vector2(r.X + 56, r.Y + r.Height / 2f), 38, p, 1, selected);
                NameWithGender(p, r.X + 112, r.Y + 34, 30);
                Level(r.X + r.Width - 30, r.Y + 34, p.Level, 28);
                HpBar(r.X + 112, r.Y + 62, r.Width - 142, 18, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
            }
            if (choice < 0) UiShapes.Fill(r, 26, new Color(40, 40, 60, 60));
        }

        // How the move will do against each place it can be aimed at, hung on the card's top edge (plan 12 · Q10)
        if (battle.TargetingMove is { } aimed)
            for (int i = 0; i < places.Count; i++)
                if (places[i] is { } place && IndexOf(choices, place) >= 0)
                {
                    var card = TargetCard(i, columns);
                    HintPill(card.X + card.Width - 24, card.Y, battle.HintFor(aimed, place), alignRight: true);
                }

        var info = new Rectangle(1172, 836, 700, 200);
        Panel(info, 28);
        UiFonts.DrawCentered(Text.Of("battleMenu.target"), info.X + 44, info.Y + 44, 24, Muted, UiWeight.Black);
        HintsDark(info.X + info.Width - 24, info.Y + 18, Hint("shared.keys.x", "shared.hints.back"));
        var move = battle.TargetingMove;
        if (move != null)
        {
            UiFonts.DrawCentered(move.DisplayName, info.X + 44, info.Y + 108, 38, Ink, UiWeight.Black);
            var target = choices.ElementAtOrDefault(hud.TargetMenuIndex);
            if (target?.Pokemon != null)
                UiFonts.DrawCentered(Text.Of("battleMenu.atTarget", ("target", target.Name)), info.X + 44, info.Y + 156, 28, Muted, UiWeight.ExtraBold);
        }
    }

    /// <summary>The colour of a hint's pill: green for a strong hit, gold for a weak one, muted for none.</summary>
    public static Color HintColor(MoveHint hint) => hint switch
    {
        MoveHint.SuperEffective => Green,
        MoveHint.NotVeryEffective => Gold,
        _ => Muted
    };

    /// <summary>A hint's sign, about 2 × <paramref name="s"/> across: a triangle up, a triangle down, a cross.</summary>
    public static void HintSign(MoveHint hint, Vector2 c, float s, Color color)
    {
        switch (hint)
        {
            case MoveHint.SuperEffective:
                UiShapes.Triangle(c + new Vector2(0, -0.8f) * s, c + new Vector2(-0.9f, 0.7f) * s, c + new Vector2(0.9f, 0.7f) * s, color, 0.15f * s);
                break;
            case MoveHint.NotVeryEffective:
                UiShapes.Triangle(c + new Vector2(-0.9f, -0.7f) * s, c + new Vector2(0, 0.8f) * s, c + new Vector2(0.9f, -0.7f) * s, color, 0.15f * s);
                break;
            case MoveHint.NoEffect:
                UiShapes.Line(c + new Vector2(-0.7f, -0.7f) * s, c + new Vector2(0.7f, 0.7f) * s, 0.36f * s, color);
                UiShapes.Line(c + new Vector2(-0.7f, 0.7f) * s, c + new Vector2(0.7f, -0.7f) * s, 0.36f * s, color);
                break;
        }
    }

    /// <summary>
    /// The hint pill (style guide, "Battle panels"): 28 tall, hung on a card's top edge at <paramref name="top"/>,
    /// starting at <paramref name="x"/> or ending there; nothing for a hint that shows nothing. Returns its width.
    /// </summary>
    public static float HintPill(float x, float top, MoveHint hint, bool alignRight)
    {
        if (MoveHints.Words(hint) is not { } words) return 0f;
        const float h = 28, size = 16;
        float textW = UiFonts.Measure(words, size, UiWeight.Black);
        float w = 14 + 16 + 8 + textW + 14;
        var pill = new Rectangle(alignRight ? x - w : x, top - 12, w, h);
        var color = HintColor(hint);
        UiShapes.Shadow(pill, h / 2f, 10, new Vector2(0, 3), ShadowColor);
        UiShapes.Shape(pill, h / 2f, Lighter(color, 0.1f), Darker(color, 0.08f), Darker(color, 0.3f), 2);
        HintSign(hint, new Vector2(pill.X + 14 + 8, pill.Y + h / 2f), 7, Color.White);
        UiFonts.DrawCentered(words, pill.X + 14 + 16 + 8, pill.Y + h / 2f, size, Color.White, UiWeight.Black);
        return w;
    }

    private static int IndexOf(IReadOnlyList<Battler> list, Battler b)
    {
        for (int i = 0; i < list.Count; i++) if (list[i] == b) return i;
        return -1;
    }

    private static void EnemyBox(float x, float y, CombatantView view, Party? trainerParty, bool caught = false)
    {
        var p = view.Shown!;
        var (jolt, ghost, _) = PunchOf(view);
        x += jolt.X;
        y += jolt.Y;
        var r = new Rectangle(x, y, 580, 120);

        TrainerTray(x, y + r.Height, trainerParty);

        Panel(r, 22, skew: -0.2f);
        float nameW = NameWithGender(p, x + 38, y + 38, 36);
        if (caught) nameW += CaughtMark(x + 38 + nameW, y + 38, 36);
        StatusPill(x + 38 + nameW + 14, y + 22, p.Status, 30);
        Level(x + r.Width - 46, y + 38, p.Level, 34);
        HpBar(x + 38, y + 72, r.Width - 90, 24, view.DisplayedHp / Math.Max(1, p.MaxHP), ghost);
        GiantTags(r, view);
    }

    /// <summary>
    /// A trainer's team on a tray hanging under a box whose foot is at <paramref name="foot"/>: a ball for each Pokémon
    /// (grey once it has fainted) and a dot for each empty place. Nothing for a wild battle.
    /// </summary>
    private static void TrainerTray(float x, float foot, Party? trainerParty)
    {
        if (trainerParty != null)
        {
            var tray = new Rectangle(x + 30, foot - 14, Party.MaxSize * 36 + 26, 62);
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
    }

    /// <summary>
    /// The caught mark after a wild foe's name (style guide, "Caught mark"): the bag's Poké Ball, 0.8× the name's
    /// size, 12 after it, centred on the name's line. Gives how far the name's row now reaches.
    /// </summary>
    private static float CaughtMark(float afterName, float centerY, float size)
    {
        var ball = PixelArtGenerator.GetBallTexture("Poké Ball");
        float d = size * 0.8f;
        Raylib.DrawTexturePro(ball, new Rectangle(0, 0, ball.Width, ball.Height), new Rectangle(afterName + 12, centerY - d / 2, d, d),
            Vector2.Zero, 0f, Color.White);
        return 12 + d;
    }

    private static void PlayerBox(float x, float y, CombatantView view)
    {
        var p = view.Shown!;
        var (jolt, ghost, thump) = PunchOf(view);
        x += jolt.X;
        y += jolt.Y;
        var r = new Rectangle(x, y, 628, 162);
        Panel(r, 22, skew: 0.2f);
        NameWithGender(p, x + 52, y + 40, 36);
        Level(x + r.Width - 40, y + 40, p.Level, 34);
        int hp = (int)MathF.Ceiling(view.DisplayedHp);
        HpBar(x + 52, y + 74, r.Width - 96, 24, view.DisplayedHp / Math.Max(1, p.MaxHP), ghost);
        string hpText = $"{hp} / {p.MaxHP}";
        float hw = UiFonts.Measure(hpText, 30 * thump, UiWeight.Black);
        UiFonts.DrawCentered(hpText, x + r.Width - 44 - hw, y + 122, 30 * thump, Ink, UiWeight.Black);
        StatusPill(x + 52, y + 106, p.Status, 30);

        // EXP runs along the bottom edge of the box
        ExpBar(new Rectangle(x + 40, y + r.Height - 18, 360, 8), view.DisplayedExp);
        GiantTags(r, view);
    }

    /// <summary>
    /// The message panel: either "What will X do?" or a message waiting for the A button (two lines if it is long).
    /// A line someone says (a trainer speaking in battle, plan 11 · C12) has their name on a pill over the box.
    /// </summary>
    /// <summary>The message box's width as the battle's lines are shown in it, and how much of it a line's two rows may take (plan 17 ? N2).</summary>
    public const float MessageBoxWidth = 1824, MessageMargin = 190, MessageSize = 40;

    public static void MessageBox(Rectangle r, string? message, string? prompting, string? speaker = null, bool arrow = true)
    {
        Panel(r, 28);
        if (prompting == null && !string.IsNullOrEmpty(speaker)) SpeakerPill(r, speaker);
        float x = r.X + 52, cy = r.Y + r.Height / 2f;
        if (prompting != null)
        {
            // The question around the name, as the table has it: "What will {name} do?", the name in red
            string question = Text.Of("battleMenu.whatWill");
            int at = question.IndexOf("{name}", StringComparison.Ordinal);
            string before = at < 0 ? question : question[..at], after = at < 0 ? "" : question[(at + "{name}".Length)..];
            UiFonts.DrawCentered(before.TrimEnd(), x, cy, 40, Ink, UiWeight.ExtraBold);
            float w = UiFonts.Measure(before, 40, UiWeight.ExtraBold);
            UiFonts.DrawCentered(prompting, x + w, cy, 40, Red, UiWeight.Black);
            float w2 = UiFonts.Measure(prompting + (after.StartsWith(' ') ? " " : ""), 40, UiWeight.Black);
            UiFonts.DrawCentered(after.TrimStart(), x + w + w2, cy, 40, Ink, UiWeight.ExtraBold);
            return;
        }

        var lines = Wrap(message ?? "", r.Width - MessageMargin, MessageSize, UiWeight.ExtraBold);
        if (lines.Count <= 1) UiFonts.DrawCentered(message ?? "", x, cy, 40, Ink, UiWeight.ExtraBold);
        else
        {
            UiFonts.DrawCentered(lines[0], x, cy - 28, 40, Ink, UiWeight.ExtraBold);
            UiFonts.DrawCentered(lines[1], x, cy + 28, 40, Ink, UiWeight.ExtraBold);
        }
        if (arrow) AdvanceArrow(r.X + r.Width - 64, r.Y + r.Height - 52);
    }

    /// <summary>
    /// The name of whoever says the message (style guide, "Battle panels", "A trainer's line"): a pill 44 tall hung on
    /// the box's top edge (22 above it, 22 over it), 40 in from its left, the name in white Black 26 on Red.
    /// </summary>
    private static void SpeakerPill(Rectangle box, string speaker)
    {
        const float h = 44, size = 26, pad = 22;
        float w = pad + UiFonts.Measure(speaker, size, UiWeight.Black) + pad;
        var pill = new Rectangle(box.X + 40, box.Y - h / 2f, w, h);
        UiShapes.Shadow(pill, h / 2f, 10, new Vector2(0, 3), ShadowColor);
        UiShapes.Shape(pill, h / 2f, Lighter(Red, 0.1f), Darker(Red, 0.08f), Darker(Red, 0.3f), 2);
        UiFonts.DrawCentered(speaker, pill.X + pad, pill.Y + h / 2f, size, Color.White, UiWeight.Black);
    }

    private static void MoveMenu(BattleHUD hud, Pokemon p, BattleEngine battle)
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
            // With the Z-Power armed a card names its Z-Move, and one with none is greyed (plan 25 · Y14)
            string? cardName = CardName(battle, move, i);
            var type = cardName == null ? Closed : Palette.GetTypeColor(move.Type.ToString());
            if (selected) UiShapes.Shadow(r, 26, 26, Vector2.Zero, type with { A = 170 });
            else UiShapes.Shadow(r, 26, 14, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(r, 26, PanelTop, PanelBottom, selected ? Darker(type, 0.15f) : Frame, selected ? 5f : 3f);
            var band = new Rectangle(r.X + 10, r.Y + 10, 124, r.Height - 20);
            UiShapes.Shape(band, 18, Lighter(type, 0.1f), Darker(type, 0.1f));
            string typeName = Words.Type(move.Type).ToUpperInvariant();
            float tw = UiFonts.Measure(typeName, 20, UiWeight.Black);
            UiFonts.DrawCentered(typeName, band.X + (band.Width - tw) / 2f, band.Y + band.Height / 2f, 20, Color.White, UiWeight.Black);
            string named = cardName ?? move.DisplayName;
            float nameSize = Math.Min(32f, 32f * 250f / Math.Max(1f, UiFonts.Measure(named, 32, UiWeight.Black)));
            UiFonts.DrawCentered(named, r.X + 156, r.Y + r.Height / 2f, nameSize, move.CurrentPP > 0 && cardName != null ? Ink : Muted, UiWeight.Black);
            string pp = $"{move.CurrentPP}/{move.MaxPP}";
            float pw = UiFonts.Measure(pp, 26, UiWeight.Black);
            UiFonts.DrawCentered(pp, r.X + r.Width - 30 - pw, r.Y + r.Height / 2f, 26, move.CurrentPP > 0 ? Ink : Red, UiWeight.Black);
            UiFonts.DrawCentered(Text.Of("summary.pp"), r.X + r.Width - 40 - pw - UiFonts.Measure(Text.Of("summary.pp"), 18, UiWeight.ExtraBold), r.Y + r.Height / 2f + 3, 18, Muted, UiWeight.ExtraBold);
        }

        // How each move will do, hung on its card's top edge: one foe's at the right, two foes' as they stand (plan 12 · Q10)
        if (battle.ShowsMoveHints)
        {
            var left = battle.IsDouble || battle.IsSos ? battle.EnemySlots.ElementAtOrDefault(1) : null;
            // A horde's move cards tell of the first foe still standing; the target menu tells of each (plan 14 · B4)
            // and a triple battle's of the first in reach of the Pokémon choosing (plan 14 · B6)
            var right = battle.IsHorde || battle.IsTriple
                ? battle.EnemySlots.FirstOrDefault(b => b.IsActive && PokemonPlatinumEngine.Battle.Sim.BattleCore.InReach(battle.Format, battle.MenuBattler.Place, b.Place)) ?? battle.EnemySlots[0]
                : battle.EnemySlots[0];
            for (int i = 0; i < Math.Min(4, p.Moves.Count); i++)
            {
                var r = new Rectangle(48 + (i % 2) * 556, 848 + (i / 2) * 100, 536, 88);
                if (left != null) HintPill(r.X + 150, r.Y, battle.HintFor(p.Moves[i], left), alignRight: false);
                HintPill(r.X + r.Width - 24, r.Y, battle.HintFor(p.Moves[i], right), alignRight: true);
            }
        }

        // The item button: a Mega Evolution, the Z-Power or an Ultra Burst armed for the move (plan 25 · Y14)
        LaterButton(battle);

        var info = new Rectangle(1172, 848, 700, 188);
        Panel(info, 28);
        if (hud.MoveMenuIndex < p.Moves.Count)
        {
            var move = p.Moves[hud.MoveMenuIndex];
            string cat = Words.Category(move.Category).ToUpperInvariant();
            UiFonts.DrawCentered(cat, info.X + 44, info.Y + 44, 24, Muted, UiWeight.Black);
            int power = move.Power;
            // With the Z-Power armed, the Z-Move's power (a type's Z-Move takes it from the move, plan 25 · Y14)
            if (battle.ZArmed && battle.ZMoveAt(hud.MoveMenuIndex) is { } zMove && zMove != move.Data) power = zMove.Power > 0 ? zMove.Power : move.Data.ZPower;
            string pwr = power > 0 ? power.ToString() : "—";
            string acc = move.Accuracy > 0 ? move.Accuracy.ToString() : "—";
            UiFonts.DrawCentered(Text.Of("summary.power"), info.X + 260, info.Y + 44, 20, Muted, UiWeight.ExtraBold);
            UiFonts.DrawCentered(pwr, info.X + 350, info.Y + 44, 30, Ink, UiWeight.Black);
            UiFonts.DrawCentered(Text.Of("summary.accuracy"), info.X + 440, info.Y + 44, 20, Muted, UiWeight.ExtraBold);
            UiFonts.DrawCentered(acc, info.X + 580, info.Y + 44, 30, Ink, UiWeight.Black);
            DrawWrapped(move.DisplayDescription, info.X + 44, info.Y + 92, info.Width - 88, 26, Ink, 36);
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
        string heading = forced ? Text.Of("battleMenu.whoGoesNext", ("pokemon", active.DisplayName))
            : battle.OfferedFoe is { } coming ? Text.Of("battleMenu.comingOutNext", ("pokemon", coming.DisplayName))
            : Text.Of("battleMenu.sendOutWhich");
        UiFonts.DrawCentered(heading, head.X + 48, head.Y + head.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        string sendOut = Text.Of("battleMenu.hint.sendOut");
        var keyZ = "shared.keys.z"; var keyX = "shared.keys.x";
        var keyC = "shared.keys.c"; string summaryHint = Text.Of("battleMenu.hint.summary");
        if (forced) HintsDark(head.X + head.Width - 24, head.Y + 16, HintOf(keyC, summaryHint), HintOf(keyZ, sendOut));
        // Opened by Battle Style Shift's Yes (plan 08 · P1): backing out stays in, and says so
        else if (battle.IsOfferingSwitch) HintsDark(head.X + head.Width - 24, head.Y + 16, HintOf(keyC, summaryHint), HintOf(keyZ, sendOut), HintOf(keyX, Text.Of("battleMenu.hint.stayIn")));
        else HintsDark(head.X + head.Width - 24, head.Y + 16, HintOf(keyC, summaryHint), HintOf(keyZ, sendOut), HintOf(keyX, Text.Of("shared.hints.back")));

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
            // An Egg is listed but can't battle (plan 06 · R15)
            if (p.IsEgg) continue;
            Level(r.X + r.Width - 34, r.Y + 46, p.Level, 32);
            HpBar(x, r.Y + 76, r.Width - 184 - 34, 22, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
            string hpText = $"{p.CurrentHP} / {p.MaxHP}";
            UiFonts.DrawCentered(hpText, r.X + r.Width - 34 - UiFonts.Measure(hpText, 26, UiWeight.Black), r.Y + 136, 26, Ink, UiWeight.Black);

            float tagX = x;
            if (battle.PlayerSlots.Any(b => b.Pokemon == p) && !p.IsFainted) tagX += Tag(tagX, r.Y + 120, Text.Of("battleMenu.inBattle"), Blue, 32) + 8;
            StatusPill(tagX, r.Y + 120, p.IsFainted ? StatusCondition.Faint : p.Status, 32);
            if (p.IsFainted) UiShapes.Fill(r, 30, new Color(40, 40, 60, 90));
        }
    }

    /// <summary>Key hints on a light panel (dark pills instead of the translucent ones used over backdrops).</summary>
    private static void HintsDark(float rightX, float y, params HintItem[] hints)
    {
        float x = rightX;
        for (int i = hints.Length - 1; i >= 0; i--)
        {
            float width = HintWidth(hints[i]);
            x -= width;
            UiShapes.Fill(new Rectangle(x, y, width, 52), 26, Frame);
            HintPill(x, y, hints[i]);
            x -= 16;
        }
    }

    // ------------------------------------------------------------------ bag

    // A property, not a field: the colour tokens are fields of another file of this partial class, and a static field
    // here may be set before them (it read them as transparent black)
    private static Color[] PocketColors => new[] { Green, Gold, Red, Blue };

    /// <summary>
    /// The battle's bag (plan 06 · R11): its four pockets as big buttons two to a row, each with how many items it
    /// holds, and under them the item used last, as Platinum's touch screen has them.
    /// </summary>
    /// <summary>The menus whose panels stand where the player's HP box is: the team's cards, the bag's, a move to learn, the information panel.</summary>
    private static bool CoversPlayerBox(BattleMenuState state) => state is BattleMenuState.SwitchPokemon or BattleMenuState.LearnMove or BattleMenuState.Info
        or BattleMenuState.SelectBagPocket or BattleMenuState.SelectBagItem or BattleMenuState.SelectBagTarget or BattleMenuState.SelectBagMove;

    private static void BagPockets(BattleHUD hud, int sw, int sh, BattleEngine battle)
    {
        Dim(sw, sh, 120);
        var head = new Rectangle(48, 548, 1824, 84);
        Panel(head, 26);
        UiFonts.DrawCentered(Text.Of("battleMenu.bagTitle"), head.X + 48, head.Y + head.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        HintsDark(head.X + head.Width - 24, head.Y + 16, Hint("shared.keys.z", "shared.hints.open"), Hint("shared.keys.x", "shared.hints.back"));

        for (int i = 0; i < BattleBag.Pockets.Length; i++)
        {
            var r = new Rectangle(48 + (i % 2) * 920, 648 + (i / 2) * 132, 904, 116);
            int count = BattleBag.Items(battle.PlayerInventory, i).Count;
            Button(r, 34, PocketColors[i], BattleBag.Pockets[i], 38, hud.BagPocketIndex == i);
            string held = Text.Of("battleMenu.kinds", ("n", count));
            UiFonts.DrawCentered(held, r.X + r.Width - 40 - UiFonts.Measure(held, 24, UiWeight.Black), r.Y + r.Height / 2f, 24,
                new Color(255, 255, 255, 220), UiWeight.Black);
        }
        var last = new Rectangle(48, 912, 1824, 108);
        Button(last, 34, new Color(126, 132, 156, 255), battle.LastUsedItem is { } name ? Text.Of("battleMenu.lastItemUsedWas", ("item", name.ToUpperInvariant())) : Text.Of("battleMenu.lastItemUsed"),
            34, hud.BagPocketIndex == BattleBag.LastUsed);
    }

    /// <summary>
    /// A pocket of the battle's bag: its items as cards two to a row with how many are left, a page at a time, and
    /// the chosen one described beside them.
    /// </summary>
    private static void BagPanel(BattleHUD hud, int sw, int sh, BattleEngine battle)
    {
        Dim(sw, sh, 120);
        var listed = battle.BagListed;
        var head = new Rectangle(48, 548, 1824, 84);
        Panel(head, 26);
        UiFonts.DrawCentered(BattleBag.Pockets[battle.BagPocket], head.X + 48, head.Y + head.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        HintsDark(head.X + head.Width - 24, head.Y + 16, Hint("shared.keys.z", "shared.hints.use"), Hint("shared.keys.x", "shared.hints.back"));

        ItemData? chosen = null;
        int first = hud.BagFirstRow * BattleEngine.BagColumns;
        for (int slot = 0; slot < BattleEngine.BagRows * BattleEngine.BagColumns; slot++)
        {
            int i = first + slot;
            var r = new Rectangle(48 + (slot % 2) * 584, 648 + (slot / 2) * 96, 568, 86);
            if (i >= listed.Count)
            {
                EmptySlot(r, 26);
                continue;
            }
            var item = listed[i].Data;
            bool selected = hud.BagMenuIndex == i;
            if (selected) chosen = item;
            if (selected) UiShapes.Shadow(r, 26, 26, Vector2.Zero, Gold with { A = 190 });
            else UiShapes.Shadow(r, 26, 14, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(r, 26, PanelTop, PanelBottom, selected ? Darker(Gold, 0.12f) : Frame, selected ? 5f : 3f);
            var icon = PixelArtGenerator.GetItemIcon(item);
            UiShapes.Circle(new Vector2(r.X + 52, r.Y + r.Height / 2f), 34, new Color(226, 234, 246, 255));
            Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(r.X + 26, r.Y + 17, 52, 52), Vector2.Zero, 0f, Color.White);
            UiFonts.DrawCentered(item.DisplayName, r.X + 104, r.Y + r.Height / 2f, 30, Ink, UiWeight.Black);
            string count = $"×{listed[i].Quantity}";
            UiFonts.DrawCentered(count, r.X + r.Width - 30 - UiFonts.Measure(count, 28, UiWeight.Black), r.Y + r.Height / 2f, 28, Ink, UiWeight.Black);
        }
        if (listed.Count == 0) UiFonts.DrawCentered(Text.Of("battleMenu.emptyPocket"), 96, 648 + 2 * 96, 32, Muted, UiWeight.ExtraBold);

        var info = new Rectangle(1232, 648, 640, 374);
        Panel(info, 28);
        if (chosen != null)
        {
            UiFonts.DrawCentered(chosen.DisplayName, info.X + 40, info.Y + 52, 34, Ink, UiWeight.Black);
            DrawWrapped(chosen.DisplayDescription, info.X + 40, info.Y + 104, info.Width - 80, 26, Ink, 36);
        }
    }

    /// <summary>The team, to pick the Pokémon an item from the bag is for, with the item named over them.</summary>
    private static void BagTargets(BattleHUD hud, int sw, int sh, Party party, BattleEngine battle)
    {
        Dim(sw, sh, 120);
        var head = new Rectangle(48, 548, 1824, 84);
        Panel(head, 26);
        string heading = battle.BagItemChosen is { } chosen ? Text.Of("battleMenu.useOnWhichPokemon", ("item", chosen.DisplayName)) : Text.Of("battleMenu.useItOnWhichPokemon");
        UiFonts.DrawCentered(heading, head.X + 48, head.Y + head.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        HintsDark(head.X + head.Width - 24, head.Y + 16, Hint("shared.keys.z", "shared.hints.use"), Hint("shared.keys.x", "shared.hints.back"));

        for (int i = 0; i < Party.MaxSize; i++)
        {
            var r = new Rectangle(48 + (i % 3) * 616, 648 + (i / 3) * 196, 592, 180);
            if (i >= party.Count)
            {
                EmptySlot(r, 30);
                continue;
            }
            var p = party.Members[i];
            bool selected = hud.BagTargetIndex == i;
            Card(r, 30, selected);
            Portrait(new Vector2(r.X + 96, r.Y + r.Height / 2f), 68, p, 2, selected);
            float x = r.X + 184;
            NameWithGender(p, x, r.Y + 46, 34);
            // An Egg is listed but can't battle (plan 06 · R15)
            if (p.IsEgg) continue;
            Level(r.X + r.Width - 34, r.Y + 46, p.Level, 32);
            HpBar(x, r.Y + 76, r.Width - 184 - 34, 22, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
            string hpText = $"{p.CurrentHP} / {p.MaxHP}";
            UiFonts.DrawCentered(hpText, r.X + r.Width - 34 - UiFonts.Measure(hpText, 26, UiWeight.Black), r.Y + 136, 26, Ink, UiWeight.Black);
            float tagX = x;
            if (battle.PlayerSlots.Any(b => b.Pokemon == p) && !p.IsFainted) tagX += Tag(tagX, r.Y + 120, Text.Of("battleMenu.inBattle"), Blue, 32) + 8;
            StatusPill(tagX, r.Y + 120, p.IsFainted ? StatusCondition.Faint : p.Status, 32);
            if (p.IsFainted) UiShapes.Fill(r, 30, new Color(40, 40, 60, 90));
        }
    }

    // ------------------------------------------------------------------ the information panel

    /// <summary>
    /// The information panel (plan 12 · Q11; style guide, "Battle panels"): the field's state on a strip under the
    /// foes' boxes, and under it a sheet of two columns, the foe's side on the left and the player's on the right as
    /// they stand on the screen, each with what its side has up and a card for each of its Pokémon on the field.
    /// </summary>
    private static void InfoPanel(BattleInfo info, bool doubles)
    {
        HintsDark(1872, 52, Hint("shared.keys.x", "shared.hints.close"));

        // The strip: FIELD, then the weather, Trick Room and Gravity, or a word that nothing is
        float x = 56, y = doubles ? 286 : 240;
        x += Tag(x, y, Text.Of("battleMenu.info.field"), Frame, 48) + 10;
        if (info.Field.Count == 0) Tag(x, y, Text.Of("battleMenu.info.nothing"), Muted, 48);
        foreach (var line in info.Field) x += Tag(x, y, line.Text.ToUpperInvariant(), FieldColor(line.Name), 48) + 10;

        const float top = 352, bottom = 1032;
        InfoColumn(new Rectangle(48, top, 900, bottom - top), info.Side(BattleSide.Enemy), Text.Of("battleMenu.info.foesSide"), Red);
        InfoColumn(new Rectangle(972, top, 900, bottom - top), info.Side(BattleSide.Player), Text.Of("battleMenu.info.yourSide"), Blue);
    }

    /// <summary>The weather in its own colour; Trick Room and Gravity in theirs.</summary>
    private static Color FieldColor(string name) =>
        name == Text.Of("battleInfo.rain") ? Blue
        : name == Text.Of("battleInfo.sun") ? Selection
        : name == Text.Of("battleInfo.sandstorm") ? Darker(Gold, 0.1f)
        : name == Text.Of("battleInfo.hail") ? new Color(96, 168, 214, 255)
        : name == Text.Of("battleInfo.fog") ? Muted
        : name == Text.Of("battleInfo.trickRoom") ? new Color(150, 96, 200, 255)
        : Frame;

    private static Color ToneColor(InfoTone tone, Color good) => tone switch
    {
        InfoTone.Good => good,
        InfoTone.Bad => Red,
        _ => Frame
    };

    /// <summary>One side's column: its name on a tag, what it has up as tags under it, and a card for each Pokémon.</summary>
    private static void InfoColumn(Rectangle r, SideInfo side, string title, Color color)
    {
        Panel(r, 28);
        Tag(r.X + 32, r.Y + 28, title, color, 44);

        float y = r.Y + 92;
        if (side.Lines.Count == 0)
        {
            UiFonts.DrawCentered(Text.Of("battleMenu.info.noScreens"), r.X + 36, y + 20, 26, Muted, UiWeight.ExtraBold);
            y += 40;
        }
        else y = Chips(side.Lines.Select(l => (l.Text.ToUpperInvariant(), ToneColor(l.Tone, Blue))), r.X + 32, y, r.X + r.Width - 32, 40, r.Y + r.Height);
        y += 16;

        int n = Math.Max(1, side.Pokemon.Count);
        float room = r.Y + r.Height - 24 - y;
        float height = Math.Min(300, (room - 16 * (n - 1)) / n);
        foreach (var card in side.Pokemon)
        {
            // A horde's five foes share the column on small cards (plan 14 · B4)
            if (height < InfoCardSmallest) InfoCardSmall(new Rectangle(r.X + 24, y, r.Width - 48, height), card);
            else InfoCard(new Rectangle(r.X + 24, y, r.Width - 48, height), card);
            y += height + 16;
        }
    }

    /// <summary>The least height a whole information card is drawn at; under it a card is a small one.</summary>
    public const float InfoCardSmallest = 140f;

    /// <summary>
    /// A small information card, for a horde's foes (plan 14 · B4): the icon, name, status and level, and on one line
    /// its ability and item, or a word that the player hasn't seen them. Its stages and conditions are left to the
    /// whole card of a battle with fewer.
    /// </summary>
    private static void InfoCardSmall(Rectangle r, PokemonInfo card)
    {
        var p = card.Pokemon;
        Card(r, 22, false);
        float mid = r.Y + r.Height / 2f;
        Portrait(new Vector2(r.X + 48, mid), Math.Min(34f, r.Height / 2f - 6f), p, 1, false);
        float x = r.X + 96;
        float nameW = NameWithGender(p, x, r.Y + 26, 26);
        StatusPill(x + nameW + 10, r.Y + 12, p.Status, 24);
        Level(r.X + r.Width - 26, r.Y + 26, p.Level, 26);
        string ability = card.Ability ?? Text.Of("battleMenu.info.notSeen");
        string item = card.Item ?? Text.Of("battleMenu.info.notSeen");
        UiFonts.DrawCentered(ability, x, r.Y + r.Height - 24, 22, card.Ability != null ? Ink : Muted, UiWeight.ExtraBold);
        UiFonts.DrawCentered(item, x + 320, r.Y + r.Height - 24, 22, card.Item != null ? Ink : Muted, UiWeight.ExtraBold);
    }

    /// <summary>
    /// A Pokémon's card: its icon, name, status and level; its ability and item, or a word that the player hasn't
    /// seen them; its stat stages and passing conditions as tags.
    /// </summary>
    private static void InfoCard(Rectangle r, PokemonInfo card)
    {
        var p = card.Pokemon;
        Card(r, 26, false);
        Portrait(new Vector2(r.X + 76, r.Y + 76), 56, p, 2, false);

        float x = r.X + 152;
        float nameW = NameWithGender(p, x, r.Y + 42, 32);
        StatusPill(x + nameW + 12, r.Y + 26, p.Status, 30);
        Level(r.X + r.Width - 30, r.Y + 42, p.Level, 30);

        InfoFact(x, r.Y + 74, Text.Of("summary.ability"), card.Ability);
        InfoFact(x + 320, r.Y + 74, Text.Of("battleMenu.info.item"), card.Item);

        var chips = card.Stages.Select(l => (l.Text.ToUpperInvariant(), ToneColor(l.Tone, Green)))
            .Concat(card.Conditions.Select(l => (l.Text.ToUpperInvariant(), ToneColor(l.Tone, Blue))))
            .ToList();
        float y = r.Y + 148;
        if (chips.Count == 0) UiFonts.DrawCentered(Text.Of("battleMenu.info.noChanges"), r.X + 36, y + 18, 24, Muted, UiWeight.ExtraBold);
        else Chips(chips, r.X + 32, y, r.X + r.Width - 28, 36, r.Y + r.Height - 12);
    }

    /// <summary>A caption over a value; a value the player hasn't seen is said so in Muted.</summary>
    private static void InfoFact(float x, float y, string caption, string? value)
    {
        Label(caption, x, y);
        if (value != null) UiFonts.DrawCentered(value, x, y + 40, 28, Ink, UiWeight.Black);
        else UiFonts.DrawCentered(Text.Of("battleMenu.info.notSeen"), x, y + 40, 28, Muted, UiWeight.ExtraBold);
    }

    /// <summary>Tags laid one after another, a new row where one would pass <paramref name="right"/>, none below <paramref name="bottom"/>; gives where the last row ends.</summary>
    private static float Chips(IEnumerable<(string Text, Color Color)> chips, float left, float y, float right, float height, float bottom)
    {
        float x = left;
        foreach (var (text, color) in chips)
        {
            float w = UiFonts.Measure(text, height * 0.58f, UiWeight.Black) + height * 0.9f;
            if (x > left && x + w > right)
            {
                x = left;
                y += height + 10;
            }
            if (y + height > bottom) break;
            x += Tag(x, y, text, color, height) + 10;
        }
        return y + height;
    }
}
