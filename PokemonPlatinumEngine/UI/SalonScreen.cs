using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>The salon's tabs (plan 43 · LF3), in their order.</summary>
public enum SalonTab { Cut, Colour, Eyes, Skin }

/// <summary>What the dye house's counter is choosing: the garment, then its dye.</summary>
public enum DyeStep { Garment, Dye }

/// <summary>
/// The salon and the dye house (plan 43 · LF3; style guide, "Menu screens", "Wardrobe"): the wardrobe's two new modes on
/// a screen of their own. The salon (<c>wardrobe salon</c>) has four tabs, CUT, COLOUR, EYES and SKIN, the first row of
/// each the look's own, and a change is paid at the chair with a yes; the dye house (<c>wardrobe dye "jubilife"</c>)
/// takes a garment owned, then a dye, and with a yes the fee and the dye's berry from the bag. The player's figure beside
/// the list wears what the cursor is on. Its logic takes no input (<see cref="MoveTab"/>, <see cref="Move"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>); <see cref="Render"/> draws the figure offscreen.
/// </summary>
public sealed class SalonScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>How many rows of a list show at once.</summary>
    public const int VisibleRows = 8;

    private readonly List<string?> rows = new();
    private readonly UiReveal asking = new(0.18f, 0.1f);
    private readonly HeldKey upDown = new();
    private readonly HashSet<string> tried = new(StringComparer.OrdinalIgnoreCase);
    private Wardrobe wardrobe = new();
    private Func<string, bool> hasItem = _ => false;
    private float openAge, time;
    private RenderTexture2D figure;
    private bool figureLoaded;
    private string? shown;

    public bool IsActive { get; private set; }

    /// <summary>The dye house's counter (true), or the salon's chair.</summary>
    public bool Dyeing { get; private set; }

    public SalonTab Tab { get; private set; }
    public DyeStep Step { get; private set; }

    /// <summary>The garment chosen to be dyed, on the dye step.</summary>
    public string? Garment { get; private set; }

    public int SelectedIndex { get; private set; }
    public int FirstRow { get; private set; }
    public IReadOnlyList<string?> Rows => rows;

    public bool Asking { get; private set; }
    public int AskIndex { get; private set; }

    /// <summary>The berry the last dye took, for the engine to take from the bag; null for none.</summary>
    public string? BerryTaken { get; private set; }

    /// <summary>What this visit came to: the changes made and the money spent.</summary>
    public int Changes { get; private set; }
    public int Spent { get; private set; }

    public Outfit Worn => wardrobe.Worn;

    /// <summary>The salon's chair, on <paramref name="owned"/>'s look.</summary>
    public void OpenSalon(Wardrobe owned) => Open(owned, dyeing: false, _ => false);

    /// <summary>The dye house's counter, on <paramref name="owned"/>'s garments; <paramref name="has"/> says whether the bag holds an item.</summary>
    public void OpenDye(Wardrobe owned, Func<string, bool> has) => Open(owned, dyeing: true, has);

    private void Open(Wardrobe owned, bool dyeing, Func<string, bool> has)
    {
        wardrobe = owned;
        hasItem = has;
        Dyeing = dyeing;
        IsActive = true;
        Asking = false;
        asking.Snap(false);
        Changes = Spent = 0;
        BerryTaken = null;
        openAge = time = 0f;
        Tab = SalonTab.Cut;
        Step = DyeStep.Garment;
        Garment = null;
        upDown.Release();
        tried.Clear();
        Fill();
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
        Asking = false;
    }

    /// <summary>The garments the dye house can dye: those owned with a colour, not dyed already.</summary>
    public IEnumerable<string> Dyeable() =>
        ClothingDatabase.All.Where(g => !g.IsNothing && !g.IsFree && wardrobe.Owns(g.Id)).Select(g => g.Id);

    private string? WornFor(SalonTab tab) => tab switch
    {
        SalonTab.Cut => wardrobe.Worn.Hair,
        SalonTab.Colour => wardrobe.Worn.HairColour,
        SalonTab.Eyes => wardrobe.Worn.Eyes,
        _ => wardrobe.Worn.Skin
    };

    private void Fill()
    {
        rows.Clear();
        string? keepOn = null;
        if (!Dyeing)
        {
            rows.Add(null);
            rows.AddRange(Tab switch
            {
                SalonTab.Cut => ClothingDatabase.Cuts,
                SalonTab.Colour => ClothingDatabase.HairColours.Select(c => c.Id),
                SalonTab.Eyes => ClothingDatabase.EyeColours.Select(c => c.Id),
                _ => ClothingDatabase.SkinTones.Select(c => c.Id)
            });
            keepOn = WornFor(Tab);
        }
        else if (Step == DyeStep.Garment) rows.AddRange(Dyeable());
        else rows.AddRange(ClothingDatabase.Dyes.Where(d => d.Sold).Select(d => d.Id));
        int at = rows.FindIndex(r => string.Equals(r, keepOn, StringComparison.OrdinalIgnoreCase));
        SelectedIndex = Math.Max(0, at);
        FirstRow = UiNav.Window(0, SelectedIndex, rows.Count, VisibleRows);
    }

    /// <summary>The row under the cursor; null for the look's own (the salon) or an empty list.</summary>
    public string? Selected => SelectedIndex < rows.Count ? rows[SelectedIndex] : null;

    /// <summary>What the figure wears: what is worn, with what the cursor is on tried on.</summary>
    public Outfit Trial
    {
        get
        {
            var worn = wardrobe.Worn;
            if (!Dyeing)
                return Tab switch
                {
                    SalonTab.Cut => worn with { Hair = Selected },
                    SalonTab.Colour => worn with { HairColour = Selected },
                    SalonTab.Eyes => worn with { Eyes = Selected },
                    _ => worn with { Skin = Selected }
                };
            string? garment = Step == DyeStep.Garment ? Selected : Garment;
            if (garment == null || ClothingDatabase.Get(garment) is not { } g) return worn;
            string id = Step == DyeStep.Dye && Selected != null ? ClothingDatabase.DyedId(garment, Selected) : garment;
            return worn.With(g.Slot, id);
        }
    }

    /// <summary>What a change under the cursor costs.</summary>
    public int Price => Dyeing ? ClothingDatabase.Prices.Dye : Tab switch
    {
        SalonTab.Cut => ClothingDatabase.Prices.Cut,
        SalonTab.Colour => ClothingDatabase.Prices.Colour,
        SalonTab.Eyes => ClothingDatabase.Prices.Eyes,
        _ => ClothingDatabase.Prices.Skin
    };

    /// <summary>Left and right: the salon's tab before or after, wrapping (the dye house has no tabs).</summary>
    public void MoveTab(int dx)
    {
        if (dx == 0 || Asking || Dyeing) return;
        Tab = (SalonTab)UiNav.Wrap((int)Tab, Math.Sign(dx), Enum.GetValues<SalonTab>().Length);
        Fill();
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Up and down the list; while the question is up, between its two answers.</summary>
    public void Move(int dy, bool held = false)
    {
        if (dy == 0) return;
        if (Asking)
        {
            AskIndex = UiNav.Wrap(AskIndex, Math.Sign(dy), 2);
            AudioManager.PlaySound("cursor");
            return;
        }
        if (rows.Count == 0) return;
        int row = held ? Math.Clamp(SelectedIndex + dy, 0, rows.Count - 1) : UiNav.Wrap(SelectedIndex, dy, rows.Count);
        if (held && row == SelectedIndex) return;
        SelectedIndex = row;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, rows.Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// The A button: on the salon's row a change asks "do it?" (nothing for what is worn already); on the dye house's
    /// garment the dyes come up; on a dye, "dye it?" (or why not: the money, the berry). On YES the change is made and
    /// paid. Returns what the player's money changes by.
    /// </summary>
    public int Confirm(int money, Action<string> onNotification)
    {
        if (Asking)
        {
            Asking = false;
            asking.Close();
            if (AskIndex != 0)
            {
                AudioManager.PlaySound("cancel");
                return 0;
            }
            return Pay(money, onNotification);
        }
        if (rows.Count == 0) return 0;
        if (Dyeing && Step == DyeStep.Garment)
        {
            Garment = Selected;
            Step = DyeStep.Dye;
            Fill();
            AudioManager.PlaySound("select");
            return 0;
        }
        if (!Dyeing && string.Equals(WornFor(Tab), Selected, StringComparison.OrdinalIgnoreCase)) return 0;
        if (Price > money)
        {
            onNotification(Text.Of("shop.notEnoughMoney"));
            AudioManager.PlaySound("error");
            return 0;
        }
        if (Dyeing && ClothingDatabase.DyeOf(Selected) is { } dye && !hasItem(dye.Berry))
        {
            onNotification(Text.Of("salon.noBerry", ("berry", LocalNames.Item(dye.Berry))));
            AudioManager.PlaySound("error");
            return 0;
        }
        Asking = true;
        AskIndex = 0;
        asking.Open();
        AudioManager.PlaySound("select");
        return 0;
    }

    private int Pay(int money, Action<string> onNotification)
    {
        int cost = Price;
        if (cost > money) return 0;
        if (Dyeing)
        {
            if (Garment == null || Selected == null || ClothingDatabase.DyeOf(Selected) is not { } dye || !hasItem(dye.Berry)) return 0;
            if (wardrobe.Dye(Garment, Selected) is not { } dyed) return 0;
            BerryTaken = dye.Berry;
            onNotification(Text.Of("salon.dyed", ("garment", ClothingDatabase.Get(dyed)!.Name)));
            // Back to the garments, the cursor on the new one
            Step = DyeStep.Garment;
            Garment = null;
            Fill();
        }
        else
        {
            var trial = Trial;
            wardrobe.Restyle(trial);
            onNotification(Text.Of("salon.done"));
        }
        Changes++;
        Spent += cost;
        AudioManager.PlaySound("select");
        return -cost;
    }

    /// <summary>The berry the last dye took, once (the engine takes it from the bag).</summary>
    public string? TakeBerry()
    {
        string? berry = BerryTaken;
        BerryTaken = null;
        return berry;
    }

    /// <summary>The B button: out of the question, back from the dyes to the garments, then out.</summary>
    public void Cancel()
    {
        if (Asking)
        {
            Asking = false;
            asking.Close();
        }
        else if (Dyeing && Step == DyeStep.Dye)
        {
            Step = DyeStep.Garment;
            string? keep = Garment;
            Garment = null;
            Fill();
            SelectedIndex = Math.Max(0, rows.IndexOf(keep));
        }
        else Close();
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The looks tried on and not kept, for the engine to let go of once the screen is closed.</summary>
    public IEnumerable<string> TakeTried()
    {
        string kept = Outfit.Dress(PlayerIdentity.CharacterOf(PlayerIdentity.Look), wardrobe.Worn);
        var left = tried.Where(t => !t.Equals(kept, StringComparison.OrdinalIgnoreCase) && t.Contains('@')).ToList();
        tried.Clear();
        return left;
    }

    public void Update(ref int playerMoney, Action<string> onNotification, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        time += dt;
        asking.Update(dt);
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), Asking ? 0 : InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        if (Asking) Move(dx != 0 ? dx : dy);
        else
        {
            MoveTab(dx);
            for (int i = 0; i < Math.Abs(dy); i++) Move(Math.Sign(dy), upDown.Repeating);
        }
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) playerMoney += Confirm(playerMoney, onNotification);
    }

    /// <summary>A row's name as the list writes it.</summary>
    public string NameOf(string? row)
    {
        if (row == null) return Text.Of("salon.own");
        if (Dyeing) return Step == DyeStep.Garment ? ClothingDatabase.Get(row)?.Name ?? row : Text.Of($"salon.dye.{row}");
        return Tab switch
        {
            SalonTab.Cut => Text.Of($"salon.cut.{row}"),
            SalonTab.Colour => Text.Of($"salon.hairColour.{row}"),
            SalonTab.Eyes => Text.Of($"salon.eyeColour.{row}"),
            _ => Text.Of($"salon.skinTone.{row}")
        };
    }

    /// <summary>A row's swatch: its colour, or null for a cut, a garment, the look's own.</summary>
    public Color? SwatchOf(string? row)
    {
        if (row == null) return null;
        string? hex = Dyeing ? (Step == DyeStep.Dye ? ClothingDatabase.DyeOf(row)?.Color : ClothingDatabase.Get(row)?.Color) : Tab switch
        {
            SalonTab.Colour => ClothingDatabase.HairColourOf(row)?.Color,
            SalonTab.Eyes => ClothingDatabase.EyeColourOf(row)?.Color,
            SalonTab.Skin => ClothingDatabase.SkinToneOf(row)?.Color,
            _ => null
        };
        if (hex == null) return null;
        var (r, g, b) = Data.Garment.Rgb(hex);
        return new Color(r, g, b, (byte)255);
    }

    // ------------------------------------------------------------------ the figure

    /// <summary>Renders the player's figure in the trial, offscreen, as the wardrobe does: a look not made yet never waits.</summary>
    internal void Render(RenderContext context)
    {
        if (!IsActive) return;
        context.EnsureLoaded();
        if (!figureLoaded)
        {
            figure = Gpu.Target(WardrobeScreen.FigureWidth, WardrobeScreen.FigureHeight, "ui");
            Raylib.SetTextureFilter(figure.Texture, TextureFilter.Bilinear);
            figureLoaded = true;
        }
        string look = PlayerIdentity.CharacterOf(PlayerIdentity.Look);
        string wanted = Outfit.Dress(look, Trial);
        tried.Add(wanted);
        if (CharacterModels.TryGet(wanted, context.Shaders) != null) shown = wanted;
        else if (shown == null || CharacterModels.TryGet(shown, context.Shaders) == null)
        {
            string worn = Outfit.Dress(look, wardrobe.Worn);
            tried.Add(worn);
            shown = CharacterModels.TryGet(worn, context.Shaders) != null ? worn : null;
        }
        if (shown == null) return;
        var pose = new CharacterPose { Time = time, Blink = (time + 0.7f) % 3.6f < 0.12f };
        float yaw = MathF.Sin(time * 0.6f) * 0.32f;
        CharacterStudio.Render(context, shown, figure, pose, yaw, viewHeight: 1.62f, lookY: 0.66f);
    }

    public void Draw(int screenWidth, int screenHeight, int playerMoney)
    {
        if (!IsActive) return;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, rows.Count, VisibleRows);
        ModernUi.DrawSalon(screenWidth, screenHeight, this, playerMoney, figureLoaded && shown != null ? figure.Texture : null,
            Math.Clamp(openAge / AppearTime, 0f, 1f), asking.Shown, asking.Visible);
    }
}
