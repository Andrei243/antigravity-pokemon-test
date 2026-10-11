using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where one of the Battle Castle's counters stands (the original's <c>MAIN_SUBSTATE_…</c> of its two applications, for one player).</summary>
public enum CastleStep
{
    /// <summary>The cursor is on the three Pokémon, or on the way out.</summary>
    Browse,
    /// <summary>A menu is up: a Pokémon's own, or one it led to.</summary>
    Menu,
    /// <summary>The berries or the items that are lent, with their prices.</summary>
    List,
    /// <summary>"That will cost so many Castle Points": a yes and a no.</summary>
    Ask,
    /// <summary>What came of it, said until a button is pressed.</summary>
    Note,
    /// <summary>A Pokémon's summary is being read.</summary>
    Summary,
    /// <summary>A Pokémon's moves are being read.</summary>
    Moves
}

/// <summary>What the counters' menus offer (<c>sMonMenuEntries</c>, <c>sHealMenuEntries</c>, <c>sRentalMenuEntries</c>, <c>sSummaryMenuEntries</c>, the level menu).</summary>
public enum CastleOption
{
    Heal, Rental, Summary, Moves, Cancel,
    RestoreHp, RestorePp, RestoreAll, RankUp,
    Berries, Items,
    Check, Level, Info,
    LevelUp, LevelDown, Stats
}

/// <summary>
/// The two counters of the Battle Castle's valet (plan 06 · R18c; style guide, "The Battle Hall and the Battle
/// Castle"), on the original's two applications (<c>src/applications/frontier/battle_castle/self_app.c</c>,
/// <c>opponent_app.c</c>). Opened for one's own three: a Pokémon's HP, PP or both are restored, a berry or an item
/// is rented for it to hold, its summary and its moves are read, and the healing and rental counters' ranks are
/// raised. Opened for the trainer to come: each of their three is identified, made five levels higher or lower, has
/// its stats and (from the counter's second rank) its moves told, all for Castle Points, which every purchase asks
/// about first. The rules and the prices are the challenge's (<see cref="BattleCastleChallenge"/>); this is the
/// menus. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and
/// the harness drive it.
/// </summary>
public sealed class CastleScreen
{
    public const int VisibleRows = 7;
    private const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }

    /// <summary>The counter for one's own three, or the one about the trainer to come.</summary>
    public bool Own { get; private set; }
    public CastleStep Step { get; private set; }
    public BattleCastleChallenge? Challenge { get; private set; }
    public FrontierRecords? Records { get; private set; }

    /// <summary>The Pokémon under the cursor, 0 to 2; 3 is the way out.</summary>
    public int Cursor { get; private set; }
    public const int ExitPlace = BattleCastleChallenge.TeamSize;

    /// <summary>The cursor in the menu that is up, in the list, or in the yes and the no.</summary>
    public int MenuCursor { get; private set; }

    /// <summary>The menu that is up, and what it is the menu of ("" for a Pokémon's own).</summary>
    public IReadOnlyList<CastleOption> Options { get; private set; } = Array.Empty<CastleOption>();
    public CastleOption Opened { get; private set; } = CastleOption.Cancel;

    /// <summary>What is lent, while the list is up: the berries, or the items.</summary>
    public IReadOnlyList<FrontierPrizeRecord> Listed { get; private set; } = Array.Empty<FrontierPrizeRecord>();
    public int ListTop { get; private set; }

    /// <summary>What is asked, or said.</summary>
    public string Line { get; private set; } = "";
    public string Title { get; private set; } = "";

    public float Age { get; private set; }
    public float StepAge { get; private set; }

    private Action? onYes;
    private CastleStep back;
    private int backCursor;
    private readonly HeldKey held = new();

    // ------------------------------------------------------------------ opening

    public void Open(BattleCastleChallenge challenge, FrontierRecords records, bool own)
    {
        Challenge = challenge;
        Records = records;
        Own = own;
        IsActive = true;
        Cursor = MenuCursor = 0;
        Options = Array.Empty<CastleOption>();
        Listed = Array.Empty<FrontierPrizeRecord>();
        Age = 0f;
        Go(CastleStep.Browse);
        AudioManager.PlaySound("select");
    }

    private void Go(CastleStep step)
    {
        Step = step;
        StepAge = 0f;
    }

    /// <summary>The three the counter is about: one's own, or the trainer's.</summary>
    public IReadOnlyList<Pokemon> Shown => Challenge == null ? Array.Empty<Pokemon>() : Own ? Challenge.Team.Members : (IReadOnlyList<Pokemon>?)Challenge.Foe?.Pokemon ?? Array.Empty<Pokemon>();

    /// <summary>The Pokémon under the cursor; null on the way out.</summary>
    public Pokemon? Subject => Cursor < Shown.Count ? Shown[Cursor] : null;

    /// <summary>The Castle Points held.</summary>
    public int Points => Challenge != null && Records != null ? Challenge.Points(Records) : 0;

    /// <summary>What an option costs as it stands; 0 for one that costs nothing or leads to another menu.</summary>
    public int PriceOf(CastleOption option)
    {
        if (Challenge == null || Records == null) return 0;
        return option switch
        {
            CastleOption.RestoreHp => BattleCastleChallenge.HealPrices[0],
            CastleOption.RestorePp => BattleCastleChallenge.HealPrices[1],
            CastleOption.RestoreAll => BattleCastleChallenge.HealPrices[2],
            CastleOption.RankUp => Challenge.RankPrice(Records, RankOf(Opened)),
            CastleOption.Check => Cursor < ExitPlace && Challenge.Identified[Cursor] ? 0 : BattleCastleChallenge.IdentifyPrice,
            CastleOption.LevelUp => BattleCastleChallenge.LevelUpPrice,
            CastleOption.LevelDown => BattleCastleChallenge.LevelDownPrice,
            CastleOption.Stats => Cursor < ExitPlace && Challenge.StatsShown[Cursor] ? 0 : BattleCastleChallenge.StatsPrice,
            CastleOption.Moves when !Own => Cursor < ExitPlace && Challenge.MovesShown[Cursor] ? 0 : BattleCastleChallenge.MovesPrice,
            _ => 0
        };
    }

    /// <summary>The rank an option asks of its counter: 1 where any will do.</summary>
    public int RankNeeded(CastleOption option) => option switch
    {
        CastleOption.RestorePp => 2,
        CastleOption.RestoreAll => 3,
        CastleOption.Items => 2,
        CastleOption.Moves when !Own => 2,
        _ => 1
    };

    /// <summary>The counter whose rank a menu's RANK UP raises.</summary>
    public static CastleRank RankOf(CastleOption menu) => menu switch
    {
        CastleOption.Heal => CastleRank.Healing,
        CastleOption.Rental => CastleRank.Rentals,
        _ => CastleRank.Summary
    };

    public static string LabelOf(CastleOption option) => option switch
    {
        CastleOption.RestoreHp => "RESTORE HP",
        CastleOption.RestorePp => "RESTORE PP",
        CastleOption.RestoreAll => "RESTORE ALL",
        CastleOption.RankUp => "RANK UP",
        CastleOption.LevelUp => "LEVEL +5",
        CastleOption.LevelDown => "LEVEL -5",
        CastleOption.Check => "IDENTIFY",
        CastleOption.Info => "INFO",
        _ => option.ToString().ToUpperInvariant()
    };

    private static IReadOnlyList<CastleOption> MenuOf(CastleOption menu, bool own) => menu switch
    {
        CastleOption.Heal => new[] { CastleOption.RestoreHp, CastleOption.RestorePp, CastleOption.RestoreAll, CastleOption.RankUp, CastleOption.Cancel },
        CastleOption.Rental => new[] { CastleOption.Berries, CastleOption.Items, CastleOption.RankUp, CastleOption.Cancel },
        CastleOption.Level => new[] { CastleOption.LevelUp, CastleOption.LevelDown, CastleOption.Cancel },
        CastleOption.Info => new[] { CastleOption.Stats, CastleOption.Moves, CastleOption.RankUp, CastleOption.Cancel },
        _ when own => new[] { CastleOption.Heal, CastleOption.Rental, CastleOption.Summary, CastleOption.Moves, CastleOption.Cancel },
        _ => new[] { CastleOption.Check, CastleOption.Level, CastleOption.Info, CastleOption.Cancel }
    };

    private void OpenMenu(CastleOption menu, int cursor = 0)
    {
        Opened = menu;
        Options = MenuOf(menu, Own);
        MenuCursor = Math.Clamp(cursor, 0, Options.Count - 1);
        Go(CastleStep.Menu);
    }

    /// <summary>The name a Pokémon goes by at the counter: one of the trainer's not yet identified is nobody yet.</summary>
    public string NameOf(int place)
    {
        if (Challenge == null || place >= Shown.Count) return "";
        return Own || Challenge.Identified[place] ? Shown[place].DisplayName : "the Pokémon";
    }

    // ------------------------------------------------------------------ the buttons

    public void Move(int dx, int dy)
    {
        if (!IsActive) return;
        switch (Step)
        {
            case CastleStep.Browse:
            {
                int step = dx != 0 ? dx : dy;
                if (step == 0) return;
                Cursor = (Cursor + step + ExitPlace + 1) % (ExitPlace + 1);
                AudioManager.PlaySound("cursor");
                break;
            }
            case CastleStep.Menu:
                if (dy == 0) return;
                MenuCursor = (MenuCursor + dy + Options.Count) % Options.Count;
                AudioManager.PlaySound("cursor");
                break;
            case CastleStep.List:
                if (dy == 0 || Listed.Count == 0) return;
                MenuCursor = Math.Clamp(MenuCursor + dy, 0, Listed.Count - 1);
                ListTop = UiNav.Window(ListTop, MenuCursor, Listed.Count, VisibleRows);
                AudioManager.PlaySound("cursor");
                break;
            case CastleStep.Ask:
                if (dx == 0) return;
                MenuCursor = 1 - MenuCursor;
                AudioManager.PlaySound("cursor");
                break;
            case CastleStep.Summary when Own:
                // One's own three are read one after another, as the team's summary is
                if (dy == 0 || Shown.Count == 0) return;
                Cursor = (Cursor + dy + Shown.Count) % Shown.Count;
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    public void Confirm()
    {
        if (!IsActive || Challenge == null || Records == null) return;
        switch (Step)
        {
            case CastleStep.Browse:
                if (Cursor == ExitPlace || Subject == null) { Close(); return; }
                AudioManager.PlaySound("select");
                OpenMenu(CastleOption.Cancel);
                break;
            case CastleStep.Menu:
                Choose(Options[MenuCursor]);
                break;
            case CastleStep.List:
                if (Listed.Count == 0) { Cancel(); return; }
                RentListed(Listed[MenuCursor]);
                break;
            case CastleStep.Ask:
                if (MenuCursor != 0) { Cancel(); return; }
                AudioManager.PlaySound("select");
                var answer = onYes;
                onYes = null;
                Restore();
                answer?.Invoke();
                break;
            case CastleStep.Note:
            case CastleStep.Summary:
            case CastleStep.Moves:
                AudioManager.PlaySound("select");
                Restore();
                break;
        }
    }

    public void Cancel()
    {
        if (!IsActive) return;
        switch (Step)
        {
            case CastleStep.Browse:
                // B puts the cursor on the way out, and takes it from there
                if (Cursor == ExitPlace) { Close(); return; }
                Cursor = ExitPlace;
                AudioManager.PlaySound("cancel");
                break;
            case CastleStep.Menu:
                AudioManager.PlaySound("cancel");
                if (Opened == CastleOption.Cancel) Go(CastleStep.Browse);
                else OpenMenu(CastleOption.Cancel, MenuOf(CastleOption.Cancel, Own).ToList().IndexOf(Opened));
                break;
            case CastleStep.List:
                AudioManager.PlaySound("cancel");
                OpenMenu(CastleOption.Rental, Listed.Count > 0 && FrontierDatabase.Castle.Items.Contains(Listed[0]) ? 1 : 0);
                break;
            default:
                AudioManager.PlaySound("cancel");
                onYes = null;
                Restore();
                break;
        }
    }

    private void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("cancel");
    }

    // Back to where a question, a word or a page was come to from
    private void Restore()
    {
        Go(back);
        MenuCursor = backCursor;
    }

    private void Remember()
    {
        if (Step is CastleStep.Ask or CastleStep.Note or CastleStep.Summary or CastleStep.Moves) return;
        back = Step;
        backCursor = MenuCursor;
    }

    private void Say(string title, string line)
    {
        Remember();
        Title = title;
        Line = line;
        Go(CastleStep.Note);
    }

    private void Ask(string title, string line, Action yes)
    {
        Remember();
        Title = title;
        Line = line;
        onYes = yes;
        MenuCursor = 0;
        Go(CastleStep.Ask);
    }

    private void Refused(CastleAnswer answer, string useless)
    {
        AudioManager.PlaySound("error");
        Say("Not this time", answer switch
        {
            CastleAnswer.NotEnough => "There aren't enough Castle Points for that.",
            CastleAnswer.RankTooLow => "This counter's rank is too low for that. Raise its rank first.",
            _ => useless
        });
    }

    // ------------------------------------------------------------------ what the menus do

    private void Choose(CastleOption option)
    {
        var challenge = Challenge!;
        var records = Records!;
        int place = Cursor;
        string name = NameOf(place);
        AudioManager.PlaySound("select");
        switch (option)
        {
            case CastleOption.Cancel:
                Cancel();
                break;
            case CastleOption.Heal:
            case CastleOption.Rental:
            case CastleOption.Level:
            case CastleOption.Info:
                OpenMenu(option);
                break;

            case CastleOption.RestoreHp:
            case CastleOption.RestorePp:
            case CastleOption.RestoreAll:
            {
                var what = option switch { CastleOption.RestoreHp => CastleHeal.Hp, CastleOption.RestorePp => CastleHeal.Pp, _ => CastleHeal.All };
                if (challenge.Rank(records, CastleRank.Healing) < RankNeeded(option)) { Refused(CastleAnswer.RankTooLow, ""); return; }
                Ask(LabelOf(option), $"That will cost {PriceOf(option)} CP. Is that all right?", () =>
                {
                    var answer = challenge.Heal(records, place, what);
                    if (answer != CastleAnswer.Done) { Refused(answer, "It would have no effect."); return; }
                    AudioManager.PlaySound("heal");
                    Say("Restored", what switch
                    {
                        CastleHeal.Hp => $"{name}'s HP was restored.",
                        CastleHeal.Pp => $"{name}'s PP was restored.",
                        _ => $"{name}'s HP and PP were restored."
                    });
                });
                break;
            }
            case CastleOption.RankUp:
            {
                var rank = RankOf(Opened);
                int price = challenge.RankPrice(records, rank);
                if (price == 0) { AudioManager.PlaySound("error"); return; }
                Ask("RANK UP", $"Raise this counter's rank for {price} CP?", () =>
                {
                    var answer = challenge.RankUp(records, rank);
                    if (answer != CastleAnswer.Done) { Refused(answer, "The rank is as high as it goes."); return; }
                    AudioManager.PlaySound("levelup");
                    Say("The rank went up!", (rank, challenge.Rank(records, rank)) switch
                    {
                        (CastleRank.Healing, 2) => "PP can now be restored.",
                        (CastleRank.Healing, _) => "HP and PP can now be restored together.",
                        (CastleRank.Rentals, 2) => "Items can now be rented.",
                        (CastleRank.Rentals, _) => "More berries and items can now be rented.",
                        _ => "The moves of the trainer's Pokémon can now be told."
                    });
                });
                break;
            }

            case CastleOption.Berries:
            case CastleOption.Items:
                if (challenge.Rank(records, CastleRank.Rentals) < RankNeeded(option)) { Refused(CastleAnswer.RankTooLow, ""); return; }
                Listed = challenge.Lent(records, berries: option == CastleOption.Berries);
                MenuCursor = ListTop = 0;
                Go(CastleStep.List);
                break;

            case CastleOption.Summary:
                Remember();
                Go(CastleStep.Summary);
                break;
            case CastleOption.Moves when Own:
                Remember();
                Go(CastleStep.Moves);
                break;

            case CastleOption.Check:
                if (challenge.Identified[place]) { Say("Identified", $"It is {Shown[place].DisplayName}."); return; }
                Ask("IDENTIFY", $"Have the Pokémon identified for {BattleCastleChallenge.IdentifyPrice} CP?", () =>
                {
                    var answer = challenge.Identify(records, place);
                    if (answer != CastleAnswer.Done) { Refused(answer, ""); return; }
                    Say("Identified", $"It is {Shown[place].DisplayName}!");
                });
                break;
            case CastleOption.LevelUp:
            case CastleOption.LevelDown:
            {
                bool up = option == CastleOption.LevelUp;
                if (challenge.Levels[place] == (up ? CastleLevel.Up : CastleLevel.Down))
                {
                    Refused(CastleAnswer.NoUse, up ? "Its level can't go any higher." : "Its level can't go any lower.");
                    return;
                }
                Ask(LabelOf(option), $"That will cost {PriceOf(option)} CP. Is that all right?", () =>
                {
                    var answer = challenge.AdjustLevel(records, place, up);
                    if (answer != CastleAnswer.Done) { Refused(answer, ""); return; }
                    Say(up ? "Five levels up" : "Five levels down", $"{char.ToUpperInvariant(name[0])}{name[1..]} is now at level {Shown[place].Level}.");
                });
                break;
            }
            case CastleOption.Stats:
                if (challenge.StatsShown[place]) { Remember(); Go(CastleStep.Summary); return; }
                Ask("STATS", $"Its stats can be told for {BattleCastleChallenge.StatsPrice} CP. Is that all right?", () =>
                {
                    var answer = challenge.ShowStats(records, place);
                    if (answer != CastleAnswer.Done) { Refused(answer, ""); return; }
                    Remember();
                    Go(CastleStep.Summary);
                });
                break;
            case CastleOption.Moves:
                if (challenge.Rank(records, CastleRank.Summary) < 2) { Refused(CastleAnswer.RankTooLow, ""); return; }
                if (challenge.MovesShown[place]) { Remember(); Go(CastleStep.Moves); return; }
                Ask("MOVES", $"Its moves can be told for {BattleCastleChallenge.MovesPrice} CP. Is that all right?", () =>
                {
                    var answer = challenge.ShowMoves(records, place);
                    if (answer != CastleAnswer.Done) { Refused(answer, ""); return; }
                    Remember();
                    Go(CastleStep.Moves);
                });
                break;
        }
    }

    private void RentListed(FrontierPrizeRecord lent)
    {
        var challenge = Challenge!;
        var records = Records!;
        int place = Cursor;
        string name = NameOf(place);
        AudioManager.PlaySound("select");
        Ask(lent.Item, $"That will cost {lent.Price} CP. Is that all right?", () =>
        {
            var answer = challenge.Rent(records, place, lent.Item);
            if (answer == CastleAnswer.Holding)
            {
                string holding = challenge.Team.Members[place].HeldItem?.Name ?? "";
                Ask(lent.Item, $"{name} is already holding the {holding}. Swap the two?", () =>
                {
                    var swapped = challenge.Rent(records, place, lent.Item, swap: true);
                    if (swapped != CastleAnswer.Done) { Refused(swapped, ""); return; }
                    Say("Rented", $"The {holding} was handed back, and {name} was given the {lent.Item} to hold.");
                });
                return;
            }
            if (answer != CastleAnswer.Done) { Refused(answer, ""); return; }
            Say("Rented", $"{name} was given the {lent.Item} to hold.");
        });
    }

    // ------------------------------------------------------------------ the frame

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        StepAge += dt;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        int dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (Step == CastleStep.List) dy = Math.Sign(held.Advance(dt, dy, InputManager.Axis(GameAction.Up, GameAction.Down, held: true)));
        else held.Release();
        if (dx != 0 || dy != 0) Move(dx, dx != 0 ? 0 : dy);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive || Challenge == null) return;
        // One's own Pokémon is read in full; the trainer's stats are a panel of the counter, since its moves are
        // another purchase (ModernUi.CastleStats)
        if (Step == CastleStep.Summary && Own && Subject is { } shown)
            ModernUi.DrawSummary(screenWidth, screenHeight, shown, Cursor, Shown.Count, Math.Clamp(StepAge / AppearTime, 0f, 1f), rental: true, title: "YOUR POK\u00c9MON");
        else ModernUi.DrawCastle(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
