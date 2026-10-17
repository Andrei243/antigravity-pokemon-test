using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// The player's choices: one for each of their Pokémon on the field, then the rules play the turn. Every choice is
// a method that takes no input, so tests and the screenshot harness can drive battles; HandleMenuInput maps keys
// onto them. What a menu refuses (no PP, a Pokémon already out) it refuses here, before the rules are asked.
public partial class BattleEngine
{
    /// <summary>How many of a pocket's items the battle's bag shows at once (two to a row).</summary>
    public const int BagRows = 4, BagColumns = 2;

    // The battle's bag (plan 06 · R11): the pocket open, the item chosen and the Pokémon it is for
    private int bagPocket;
    private ItemData? bagItem;
    private int bagTarget = -1;

    /// <summary>The pocket of the battle's bag that is open (0 to 3; <see cref="BattleBag.Pockets"/>).</summary>
    public int BagPocket => bagPocket;

    /// <summary>The items of the pocket that is open.</summary>
    public IReadOnlyList<ItemStack> BagListed => BattleBag.Items(PlayerInventory, bagPocket);

    /// <summary>The item chosen from the bag while its Pokémon or move is being picked.</summary>
    public ItemData? BagItemChosen => bagItem;

    /// <summary>The Pokémon of the team the chosen item is for, while its move is being picked.</summary>
    public int BagTarget => bagTarget;

    /// <summary>The item used last from the bag in this battle (the fifth button).</summary>
    public string? LastUsedItem { get; private set; }

    private int menuSlot;
    private List<int> asked = new();
    private readonly Dictionary<int, BattleChoice> choices = new();
    private Move? targetingMove;
    private List<Battler> targetChoices = new();

    /// <summary>A fainted Pokémon is being replaced (the switch menu can't be closed).</summary>
    private bool replacing;

    /// <summary>The row of the commands' stack (BAG, POKÉMON, RUN) the cursor was on last: where a step sideways from FIGHT goes.</summary>
    private int stackRow = 1;

    /// <summary>The move each of the player's Pokémon chose last in this battle: where its move menu opens.</summary>
    private readonly Dictionary<Pokemon, int> lastMove = new();

    /// <summary>The move whose target is being chosen.</summary>
    public Move? TargetingMove => targetingMove;

    /// <summary>The Pokémon a move can be aimed at while choosing a target, foes first.</summary>
    public IReadOnlyList<Battler> TargetChoices => targetChoices;

    /// <summary>
    /// The four places as the target menu lays them out, two to a row: the foes on top as the player sees them, the
    /// player's own under them. A place a single battle doesn't have is null.
    /// </summary>
    public IReadOnlyList<Battler?> TargetLayout => new[]
    {
        EnemySlots.ElementAtOrDefault(1), EnemySlots[0],
        PlayerSlots[0], PlayerSlots.ElementAtOrDefault(1)
    };

    private readonly HashSet<int> seenBefore;

    /// <summary>The move cards say how a move will do (plan 12 · Q10; <see cref="BattleSetup.MoveHints"/>).</summary>
    public bool ShowsMoveHints { get; }

    /// <summary>
    /// What the move card says of <paramref name="move"/>, used by the Pokémon whose move is being chosen, against
    /// <paramref name="target"/> (one of the screen's places): the matchup <see cref="DamageCalculator.Effectiveness"/>
    /// gives on the rules' own battlers, which are the screen's whenever a menu is open; nothing for a status move,
    /// an empty place, or a species the Pokédex hadn't seen before the battle.
    /// </summary>
    public MoveHint HintFor(Move move, Battler? target)
    {
        if (!ShowsMoveHints || move.Category == MoveCategory.Status) return MoveHint.None;
        if (target?.Pokemon is not { IsFainted: false } foe || !seenBefore.Contains(foe.Species.DexNumber)) return MoveHint.None;
        var user = core.At(MenuBattler.Place);
        if (user.Pokemon == null) return MoveHint.None;
        return MoveHints.Of(DamageCalculator.Effectiveness(user, core.At(target.Place), move, core.Rules));
    }

    /// <summary>
    /// The abilities and held items the battle's lines have named (plan 12 · Q11): all the information panel says of
    /// a Pokémon that isn't the player's own.
    /// </summary>
    public Revealed Revealed { get; } = new();

    /// <summary>What the information panel shows, made as it opens: nothing changes while it is open.</summary>
    public BattleInfo? InfoShown { get; private set; }

    /// <summary>What the information panel would show now: the field, both sides and every Pokémon on it, as far as the player has been told.</summary>
    public BattleInfo Info => BattleInfo.Of(core.Field, PlayerSlots, EnemySlots, Revealed);

    /// <summary>
    /// Opens the information panel over the commands (the <c>Item</c> action, C or the pad's left face button). It
    /// changes no rule and answers nothing: closing it is back at the same commands.
    /// </summary>
    public bool OpenInfo()
    {
        if (HUD.MenuState != BattleMenuState.Main) return false;
        InfoShown = Info;
        HUD.MenuState = BattleMenuState.Info;
        return true;
    }

    /// <summary>Closes the information panel, back at the commands.</summary>
    public void CloseInfo()
    {
        if (HUD.MenuState != BattleMenuState.Info) return;
        InfoShown = null;
        HUD.MenuState = BattleMenuState.Main;
    }

    /// <summary>
    /// What answers a menu: the A button, and the Menu action (Enter among its keys), which reads a battle's messages
    /// on as well. Folded into the Menu action (plan 16 · T11c, as plan 12 · Q4 describes) so a game steered by actions
    /// alone can answer it too.
    /// </summary>
    private static bool ConfirmPressed =>
        InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Menu);

    /// <summary>The switch menu is open because a fainted Pokémon has to be replaced.</summary>
    public bool IsChoosingReplacement => replacing;

    /// <summary>Opens the menus for the first of the player's Pokémon that can act.</summary>
    private void BeginChoosing()
    {
        choices.Clear();
        targetingMove = null;
        MegaArmed = false;
        // Only the Pokémon the rules ask about: one in the middle of a move has nothing to choose
        asked = core.Request is ActionRequest request ? request.Places.Where(p => p.Side == BattleSide.Player).Select(p => p.Slot).ToList() : new List<int>();
        menuSlot = asked.Count > 0 ? asked[0] : 0;
        HUD.MenuState = BattleMenuState.Main;
    }

    /// <summary>
    /// The move menu's Mega Evolution is armed (plan 06 · R20): the move chosen next goes with it. Toggled with the
    /// item button in the move menu (<see cref="ToggleMega"/>); plan 25 · Y14 draws the button.
    /// </summary>
    public bool MegaArmed { get; private set; }

    /// <summary>The Pokémon whose moves are being chosen may Mega Evolve with its move: the rules let it, and no other of the player's has been chosen to this turn.</summary>
    public bool CanMegaEvolveNow =>
        core.Request is ActionRequest && !playingTheLog && MenuBattler.Pokemon != null && !choices.Values.Any(c => c.Mega) && core.CanMegaEvolve(InCore(MenuBattler));

    /// <summary>Arms or disarms the Mega Evolution for the move to be chosen; false (and disarmed) where it can't be had.</summary>
    public bool ToggleMega()
    {
        if (!CanMegaEvolveNow)
        {
            MegaArmed = false;
            return false;
        }
        MegaArmed = !MegaArmed;
        return true;
    }

    /// <summary>Stores the choice for the Pokémon whose menu is open and moves on to the next one, or hands the turn to the rules.</summary>
    private void Commit(BattleChoice choice)
    {
        // A move goes with the Mega Evolution armed for it (plan 06 · R20)
        if (MegaArmed && choice.Kind == ChoiceKind.Fight && CanMegaEvolveNow) choice = choice with { Mega = true };
        MegaArmed = false;
        choices[menuSlot] = choice;

        // Running is for the whole side: nobody else is asked
        if (choice.Kind == ChoiceKind.Run)
        {
            foreach (var other in PlayerSlots.Where(b => asked.Contains(b.Slot) && !choices.ContainsKey(b.Slot)))
                choices[other.Slot] = BattleChoice.Run(other.Place);
        }

        var next = PlayerSlots.FirstOrDefault(b => b.Slot > menuSlot && asked.Contains(b.Slot) && !choices.ContainsKey(b.Slot));
        if (next != null)
        {
            menuSlot = next.Slot;
            HUD.MenuState = BattleMenuState.Main;
            HUD.MainMenuIndex = 0;
            return;
        }

        Submit(PlayerSlots.Where(b => choices.ContainsKey(b.Slot)).Select(b => choices[b.Slot]).ToList());
    }

    /// <summary>Answers what the rules asked, and shows what they made of it.</summary>
    private void Submit(IReadOnlyList<BattleChoice> answer)
    {
        // While lines are still being shown the screen is behind the rules, and what it holds must never be
        // copied over theirs (a choice made early by a tool would otherwise undo a turn's worth of state)
        if (!playingTheLog) SyncToCore();
        core.Submit(answer);
        Play(core.TakeLog());
        Pump();
    }

    /// <summary>Goes back to the previous Pokémon's choice (double battles); returns false when there is none.</summary>
    public bool CancelChoice()
    {
        var previous = PlayerSlots.LastOrDefault(b => b.Slot < menuSlot && choices.ContainsKey(b.Slot));
        if (previous == null) return false;

        // An item set aside for that Pokémon goes back in the bag
        if (choices[previous.Slot] is { Kind: ChoiceKind.Item, Item: { } item } && ItemDatabase.Get(item) is { } data) PlayerInventory.AddItem(data, 1);
        choices.Remove(previous.Slot);
        menuSlot = previous.Slot;
        HUD.MenuState = BattleMenuState.Main;
        return true;
    }

    /// <summary>
    /// Acts on a choice from the main battle menu: 0 FIGHT, 1 BAG, 2 POKÉMON, 3 RUN; in the Great Marsh 0 the Safari
    /// Ball, 1 bait, 2 mud, 3 RUN; in Pal Park 0 the Park Ball and 3 RUN.
    /// </summary>
    public void SelectMainMenuOption(int index)
    {
        if (Kind is BattleKind.Safari or BattleKind.PalPark)
        {
            var place = PlayerSlots[0].Place;
            switch (index)
            {
                case 0:
                    AudioManager.PlaySound("select");
                    // Pal Park's Park Ball is thrown as the original throws it, as a Safari Ball that can't miss
                    Commit(BattleChoice.UseItem(place, "Safari Ball"));
                    break;
                case 1 when Kind == BattleKind.Safari:
                    AudioManager.PlaySound("select");
                    Commit(BattleChoice.Bait(place));
                    break;
                case 2 when Kind == BattleKind.Safari:
                    AudioManager.PlaySound("select");
                    Commit(BattleChoice.Mud(place));
                    break;
                case 3:
                    Commit(BattleChoice.Run(place));
                    break;
            }
            return;
        }

        switch (index)
        {
            case 0:
                if (core.UsableMoves(InCore(MenuBattler)).Count == 0)
                {
                    // Nothing left to use: it struggles
                    var place = MenuBattler.Place;
                    QueueMessage(Text.Of("battle.hasNoMovesLeft", ("pokemon", MenuBattler.Name)), () => Commit(BattleChoice.Fight(place, -1)));
                    Pump();
                    return;
                }
                HUD.MenuState = BattleMenuState.Moves;
                // The cursor waits on the move this Pokémon chose last
                HUD.MoveMenuIndex = Math.Clamp(lastMove.GetValueOrDefault(MenuBattler.Pokemon!), 0, Math.Max(0, MenuBattler.Pokemon!.Moves.Count - 1));
                break;
            case 1:
                // The Battle Frontier lets nothing out of the Bag, and says so at once (PLAYER_INPUT_ITEM)
                if (IsFrontier)
                {
                    Refuse(BattleCore.FrontierBagRefusal, BattleMenuState.Main);
                    return;
                }
                // A Master Trainer's battle closes the bag to both sides (plan 14 · B14)
                if (BagClosed)
                {
                    Refuse(BattleCore.NoItemsRefusal, BattleMenuState.Main);
                    return;
                }
                HUD.MenuState = BattleMenuState.SelectBagPocket;
                break;
            case 2:
                HUD.MenuState = BattleMenuState.SwitchPokemon;
                HUD.SwitchMenuIndex = 0;
                break;
            case 3:
                AttemptRun();
                break;
        }
    }

    /// <summary>Uses the move in the given slot, as chosen from the FIGHT menu.</summary>
    public void SelectMove(int index)
    {
        var user = MenuBattler;
        if (index < 0 || index >= user.Pokemon!.Moves.Count) return;

        var move = user.Pokemon.Moves[index];

        // The rules say what can't be picked, and why: no PP, a Choice item, a Taunt, an Encore, a Disable…
        var mine = InCore(user);
        if (core.WhyNotMove(mine, mine.Pokemon!.Moves[index]) is { } refusal)
        {
            Refuse(refusal, BattleMenuState.Moves);
            return;
        }

        AudioManager.PlaySound("select");
        lastMove[user.Pokemon] = index;

        // In a double battle a single-target move asks where to aim it
        if (IsDouble && move.Target == MoveTarget.Selected)
        {
            // In the order the target menu lays them out: the foes left to right, then the partner
            targetChoices = EnemySlots.Reverse().Where(b => b.IsActive).Concat(PlayerSlots.Where(b => b.IsActive && b != user)).ToList();
            if (targetChoices.Count > 1)
            {
                targetingMove = move;
                HUD.TargetMenuIndex = 0;
                HUD.MenuState = BattleMenuState.SelectTarget;
                return;
            }
        }

        Commit(BattleChoice.Fight(user.Place, index));
    }

    /// <summary>Aims the move being chosen at <see cref="TargetChoices"/>[index].</summary>
    public void SelectTarget(int index)
    {
        if (targetingMove == null || index < 0 || index >= targetChoices.Count) return;
        int move = MenuBattler.Pokemon!.Moves.IndexOf(targetingMove);
        targetingMove = null;
        AudioManager.PlaySound("select");
        Commit(BattleChoice.Fight(MenuBattler.Place, move, targetChoices[index].Place));
    }

    /// <summary>Sends in the party's Pokémon at <paramref name="partyIndex"/>: a switch for this turn, or a fainted Pokémon's replacement.</summary>
    public void SelectSwitch(int partyIndex)
    {
        if (partyIndex < 0 || partyIndex >= PlayerParty.Count) return;
        var chosen = PlayerParty.Members[partyIndex];

        string? refusal = null;
        if (chosen.IsEgg) refusal = Text.Of("battle.anEggCantBattle");
        else if (PlayerSlots.Any(b => b.Pokemon == chosen && !chosen.IsFainted)) refusal = Text.Of("battle.isAlreadyInBattle", ("pokemon", chosen.DisplayName));
        else if (chosen.IsFainted) refusal = Text.Of("battle.hasNoEnergyLeft", ("pokemon", chosen.DisplayName));
        else if (choices.Values.Any(a => a.Kind == ChoiceKind.Switch && a.SwitchTo == partyIndex)) refusal = Text.Of("battle.isAlreadyGoingIn", ("pokemon", chosen.DisplayName));

        // Something may be holding the Pokémon on the field (a fainted one's place is free to fill)
        if (refusal == null && !replacing)
        {
            InCore(MenuBattler);
            refusal = core.WhyNot(BattleChoice.Switch(MenuBattler.Place, partyIndex));
        }

        if (refusal != null)
        {
            Refuse(refusal, BattleMenuState.SwitchPokemon);
            return;
        }

        AudioManager.PlaySound("select");
        var place = MenuBattler.Place;
        // Battle Style Shift's offer, answered with a switch (plan 08 · P1)
        if (offerPanel)
        {
            SwitchAtOffer(partyIndex);
            return;
        }
        if (replacing)
        {
            replacing = false;
            Submit(new[] { BattleChoice.Switch(place, partyIndex) });
            return;
        }

        Commit(BattleChoice.Switch(place, partyIndex));
    }

    /// <summary>Opens a pocket of the battle's bag (0 to 3), or with 4 goes straight to the item used last.</summary>
    public void SelectBagPocket(int pocket)
    {
        if (pocket == BattleBag.LastUsed)
        {
            if (LastUsedItem is { } name && ItemDatabase.Get(name) is { } last && PlayerInventory.GetQuantity(last) > 0) ChooseBagItem(last);
            else Refuse(Text.Of("battle.thereNoItemTo"), BattleMenuState.SelectBagPocket);
            return;
        }
        if (pocket < 0 || pocket >= BattleBag.Pockets.Length) return;
        bagPocket = pocket;
        HUD.BagMenuIndex = 0;
        HUD.BagFirstRow = 0;
        HUD.MenuState = BattleMenuState.SelectBagItem;
        AudioManager.PlaySound("select");
    }

    /// <summary>Chooses the item at this place in the open pocket: it is used, or goes on to the Pokémon it is for.</summary>
    public void SelectBagItem(int index)
    {
        var listed = BagListed;
        if (index < 0 || index >= listed.Count) return;
        ChooseBagItem(listed[index].Data);
    }

    private void ChooseBagItem(ItemData item)
    {
        if (BattleBag.ForAPartyMember(item))
        {
            bagItem = item;
            bagTarget = -1;
            // The cursor starts on the Pokémon whose menu is open
            HUD.BagTargetIndex = Math.Max(0, PlayerParty.Members.IndexOf(MenuBattler.Pokemon!));
            HUD.MenuState = BattleMenuState.SelectBagTarget;
            AudioManager.PlaySound("select");
            return;
        }
        UseFromBag(item, -1, -1, BattleMenuState.SelectBagItem);
    }

    /// <summary>The Pokémon of the team the chosen item is for: it is used on it, or goes on to its moves.</summary>
    public void SelectBagTarget(int partyIndex)
    {
        if (bagItem is not { } item || partyIndex < 0 || partyIndex >= PlayerParty.Count) return;
        if (BattleBag.ForAMove(item))
        {
            bagTarget = partyIndex;
            HUD.BagMoveIndex = 0;
            HUD.MenuState = BattleMenuState.SelectBagMove;
            AudioManager.PlaySound("select");
            return;
        }
        UseFromBag(item, partyIndex, -1, BattleMenuState.SelectBagTarget);
    }

    /// <summary>The move of the chosen Pokémon the item restores.</summary>
    public void SelectBagMove(int move)
    {
        if (bagItem is not { } item || bagTarget < 0) return;
        UseFromBag(item, bagTarget, move, BattleMenuState.SelectBagMove);
    }

    /// <summary>
    /// Uses an item of the bag as the menus would after choosing it: the Pokémon of the team it is for and its move,
    /// where it needs them. For tests and tools that drive a battle.
    /// </summary>
    public void UseBagItem(string name, int onPartyMember = -1, int onMove = -1)
    {
        if (ItemDatabase.Get(name) is { } item) UseFromBag(item, onPartyMember, onMove, BattleMenuState.Main);
    }

    private void UseFromBag(ItemData item, int onPartyMember, int onMove, BattleMenuState backTo)
    {
        InCore(MenuBattler);
        var choice = BattleChoice.UseItem(MenuBattler.Place, item.Name, onPartyMember, onMove);
        string? refusal = PlayerInventory.GetQuantity(item) <= 0 ? Text.Of("battle.youDontHaveAny", ("item", item.DisplayName)) : core.WhyNot(choice);
        if (refusal != null)
        {
            Refuse(refusal, backTo);
            return;
        }

        PlayerInventory.RemoveItem(item, 1);
        LastUsedItem = item.Name;
        bagItem = null;
        bagTarget = -1;
        AudioManager.PlaySound("select");
        Commit(choice);
    }

    /// <summary>The B button in the battle's bag: a step back, from a move to the team, the team to the pocket, the pocket to the bag, the bag to the commands.</summary>
    public void BagBack()
    {
        AudioManager.PlaySound("cancel");
        HUD.MenuState = HUD.MenuState switch
        {
            BattleMenuState.SelectBagMove => BattleMenuState.SelectBagTarget,
            BattleMenuState.SelectBagTarget => BattleMenuState.SelectBagItem,
            BattleMenuState.SelectBagItem => BattleMenuState.SelectBagPocket,
            _ => BattleMenuState.Main
        };
        if (HUD.MenuState is BattleMenuState.SelectBagItem or BattleMenuState.SelectBagPocket) bagItem = null;
    }

    /// <summary>
    /// Uses an item for the Pokémon whose menu is open without going through the bag, which offers only a few and
    /// counts them: for tools that stage a battle (the harness throwing a ball of each kind).
    /// </summary>
    public void UseItem(ItemData item) => Commit(BattleChoice.UseItem(MenuBattler.Place, item.Name));

    private void AttemptRun()
    {
        // In the Battle Frontier RUN is a question: to give the match up, and lose it (COMMAND_SELECTION_RUN_SELECT_INIT)
        if (IsFrontier)
        {
            AskToForfeit();
            return;
        }
        if (IsTrainerBattle)
        {
            QueueMessage(Text.Of("battle.noThereNoRunning"), () => HUD.MenuState = BattleMenuState.Main);
            Pump();
            return;
        }

        // Held on the field: the try isn't made, and the turn isn't spent
        InCore(MenuBattler);
        if (core.WhyNot(BattleChoice.Run(MenuBattler.Place)) is { } held)
        {
            Refuse(held, BattleMenuState.Main);
            return;
        }
        Commit(BattleChoice.Run(MenuBattler.Place));
    }

    /// <summary>Says why a choice can't be made and goes back to the menu it was made in.</summary>
    private void Refuse(string why, BattleMenuState backTo)
    {
        AudioManager.PlaySound("error");
        QueueMessage(why, () => HUD.MenuState = backTo);
        Pump();
    }

    /// <summary>
    /// Moves the cursor of the menu that is open one step, as the arrow keys do, and follows the way that menu is
    /// drawn. With both a step sideways and one up or down, a menu that isn't a plain grid takes the one up or down.
    /// </summary>
    public void MoveCursor(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return;

        int before, after;
        switch (HUD.MenuState)
        {
            case BattleMenuState.Main:
                before = HUD.MainMenuIndex;
                after = HUD.MainMenuIndex = CommandStep(before, dx, dy);
                break;
            case BattleMenuState.Moves:
                // Two to a row; a Pokémon with fewer than four moves has no cursor on the empty places
                before = HUD.MoveMenuIndex;
                after = HUD.MoveMenuIndex = UI.Kit.UiNav.Grid(before, MenuBattler.Pokemon!.Moves.Count, 2, dx, dy);
                break;
            case BattleMenuState.SelectTarget:
                before = HUD.TargetMenuIndex;
                after = HUD.TargetMenuIndex = TargetStep(before, dx, dy);
                break;
            case BattleMenuState.SwitchPokemon:
                // The team is laid out three to a row
                before = HUD.SwitchMenuIndex;
                after = HUD.SwitchMenuIndex = UI.Kit.UiNav.Grid(before, PlayerParty.Count, 3, dx, dy);
                break;
            case BattleMenuState.SelectBagPocket:
                // The four pockets two to a row, and the item used last under them
                before = HUD.BagPocketIndex;
                after = HUD.BagPocketIndex = UI.Kit.UiNav.Grid(before, BattleBag.Pockets.Length + 1, 2, dx, dy);
                break;
            case BattleMenuState.SelectBagItem:
                int listed = BagListed.Count;
                before = HUD.BagMenuIndex;
                after = HUD.BagMenuIndex = UI.Kit.UiNav.Grid(before, listed, BagColumns, dx, dy);
                HUD.BagFirstRow = UI.Kit.UiNav.Window(HUD.BagFirstRow, after / BagColumns, (listed + BagColumns - 1) / BagColumns, BagRows);
                break;
            case BattleMenuState.SelectBagTarget:
                before = HUD.BagTargetIndex;
                after = HUD.BagTargetIndex = UI.Kit.UiNav.Grid(before, PlayerParty.Count, 3, dx, dy);
                break;
            case BattleMenuState.SelectBagMove:
                before = HUD.BagMoveIndex;
                int moves = bagTarget >= 0 && bagTarget < PlayerParty.Count ? PlayerParty.Members[bagTarget].Moves.Count : 0;
                after = HUD.BagMoveIndex = moves == 0 ? 0 : UI.Kit.UiNav.Grid(before, moves, 2, dx, dy);
                break;
            default:
                return;
        }
        if (after != before) AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// A step among the commands, which are drawn as FIGHT (0), tall on the left, with BAG, POKÉMON and RUN (1 to 3)
    /// stacked beside it. Up and down go through the four in that order and round again. Left and right cross
    /// between FIGHT and the stack, and come back into the stack on the row the cursor stood on last.
    /// </summary>
    private int CommandStep(int index, int dx, int dy)
    {
        if (index != 0) stackRow = index;
        if (dy != 0) return UI.Kit.UiNav.Wrap(index, Math.Sign(dy), 4);
        return index == 0 ? stackRow : 0;
    }

    /// <summary>
    /// A step among the targets, over the four cards as they lie (<see cref="TargetLayout"/>). A card that can't
    /// be aimed at (the Pokémon using the move, an empty place) is never stopped on: a step up or down that would
    /// land on one takes the card beside it, and a step sideways stays where it is.
    /// </summary>
    private int TargetStep(int index, int dx, int dy)
    {
        if (index < 0 || index >= targetChoices.Count) return 0;
        var layout = TargetLayout;
        int at = -1;
        for (int i = 0; i < layout.Count; i++) if (layout[i] == targetChoices[index]) at = i;
        if (at < 0) return index;

        int column = at % 2, row = at / 2;
        if (dy != 0) row = 1 - row;
        else column = 1 - column;

        var tries = dy != 0 ? new[] { row * 2 + column, row * 2 + 1 - column } : new[] { row * 2 + column };
        foreach (int card in tries)
        {
            int choice = layout[card] is { } there ? targetChoices.IndexOf(there) : -1;
            if (choice >= 0) return choice;
        }
        return index;
    }

    private void HandleMenuInput()
    {
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        bool confirm = ConfirmPressed, cancel = InputManager.IsActionPressed(GameAction.Cancel);

        switch (HUD.MenuState)
        {
            case BattleMenuState.Main:
                if (cancel && CancelChoice())
                {
                    AudioManager.PlaySound("cancel");
                    return;
                }
                if (InputManager.IsActionPressed(GameAction.Item) && OpenInfo())
                {
                    AudioManager.PlaySound("select");
                    return;
                }
                MoveCursor(dx, dy);
                if (confirm)
                {
                    AudioManager.PlaySound("select");
                    SelectMainMenuOption(HUD.MainMenuIndex);
                }
                break;

            case BattleMenuState.Info:
                if (cancel || confirm || InputManager.IsActionPressed(GameAction.Item))
                {
                    AudioManager.PlaySound("cancel");
                    CloseInfo();
                }
                break;

            case BattleMenuState.Moves:
                if (cancel)
                {
                    AudioManager.PlaySound("cancel");
                    HUD.MenuState = BattleMenuState.Main;
                    return;
                }
                // The item button arms the Mega Evolution for the move (plan 06 · R20)
                if (InputManager.IsActionPressed(GameAction.Item))
                {
                    AudioManager.PlaySound(ToggleMega() ? "select" : "cancel");
                    return;
                }
                MoveCursor(dx, dy);
                if (confirm) SelectMove(HUD.MoveMenuIndex);
                break;

            case BattleMenuState.SelectTarget:
                if (cancel)
                {
                    AudioManager.PlaySound("cancel");
                    targetingMove = null;
                    HUD.MenuState = BattleMenuState.Moves;
                    return;
                }
                MoveCursor(dx, dy);
                if (confirm) SelectTarget(HUD.TargetMenuIndex);
                break;

            case BattleMenuState.SwitchPokemon:
                if (cancel)
                {
                    // The team opened by Battle Style Shift's Yes: backing out of it stays in (plan 08 · P1)
                    if (offerPanel)
                    {
                        AudioManager.PlaySound("cancel");
                        StayIn();
                    }
                    else if (!replacing)
                    {
                        AudioManager.PlaySound("cancel");
                        HUD.MenuState = BattleMenuState.Main;
                    }
                    return;
                }
                MoveCursor(dx, dy);
                if (confirm) SelectSwitch(HUD.SwitchMenuIndex);
                break;

            case BattleMenuState.SelectBagPocket or BattleMenuState.SelectBagItem or BattleMenuState.SelectBagTarget or BattleMenuState.SelectBagMove:
                if (cancel)
                {
                    BagBack();
                    return;
                }
                MoveCursor(dx, dy);
                if (!confirm) break;
                switch (HUD.MenuState)
                {
                    case BattleMenuState.SelectBagPocket: SelectBagPocket(HUD.BagPocketIndex); break;
                    case BattleMenuState.SelectBagItem: SelectBagItem(HUD.BagMenuIndex); break;
                    case BattleMenuState.SelectBagTarget: SelectBagTarget(HUD.BagTargetIndex); break;
                    default: SelectBagMove(HUD.BagMoveIndex); break;
                }
                break;
        }
    }
}
