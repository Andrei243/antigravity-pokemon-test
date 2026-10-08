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

/// <summary>
/// The wardrobe (plan 11 · C10, first part): the player's clothes slot by slot (hat, top, bottoms, shoes, bag), with
/// the player's figure beside them wearing whatever the cursor is on. At home it holds the clothes owned, put on for
/// nothing; at a boutique (<c>wardrobe "jubilife"</c>) it holds the boutique's stock too, and a garment not owned is
/// bought with a yes before it is put on, then is the player's for good. The first row of every slot is the look's
/// own garment. Its logic takes no input (<see cref="MoveTab"/>, <see cref="Move"/>, <see cref="Confirm"/>,
/// <see cref="Cancel"/>), so tests and the harness drive it; <see cref="Render"/> draws the figure offscreen.
/// </summary>
public class WardrobeScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>How many rows of a slot's list show at once.</summary>
    public const int VisibleRows = 8;

    /// <summary>The buying question's answers, in their order.</summary>
    public static readonly string[] BuyChoices = { "BUY", "NO" };

    private readonly List<Garment> stock = new();
    private readonly List<string?> rows = new();
    private readonly UiReveal asking = new(0.18f, 0.1f);
    private readonly HeldKey upDown = new();
    private readonly CursorTick tick = new();
    private readonly HashSet<string> tried = new(StringComparer.OrdinalIgnoreCase);
    private Wardrobe wardrobe = new();
    private float openAge, time, yaw;
    private RenderTexture2D figure;
    private bool figureLoaded;
    private string? shown;

    public bool IsActive { get; private set; }

    /// <summary>The heading: the boutique's name, or WARDROBE at home.</summary>
    public string Name { get; private set; } = "WARDROBE";

    /// <summary>True where clothes are sold.</summary>
    public bool IsBoutique => stock.Count > 0;

    /// <summary>The slot whose tab is open.</summary>
    public int Tab { get; private set; }
    public ClothingSlot Slot => Outfit.Slots[Tab];

    public int SelectedIndex { get; private set; }
    public int FirstRow { get; private set; }

    /// <summary>The open slot's rows: null (the look's own) first, then the garments in the file's order.</summary>
    public IReadOnlyList<string?> Rows => rows;

    /// <summary>The garment under the cursor (null on the look's own).</summary>
    public Garment? Selected => SelectedIndex < rows.Count ? ClothingDatabase.Get(rows[SelectedIndex]) : null;

    /// <summary>What the figure wears: what is worn, with the garment under the cursor tried on in its slot.</summary>
    public Outfit Trial => wardrobe.Worn.With(Slot, SelectedIndex < rows.Count ? rows[SelectedIndex] : null);

    public Outfit Worn => wardrobe.Worn;

    /// <summary>True while "buy it?" is up, and the answer the cursor is on.</summary>
    public bool Asking { get; private set; }
    public int AskIndex { get; private set; }

    /// <summary>What this visit came to: the garments bought and the money spent.</summary>
    public int Bought { get; private set; }
    public int Spent { get; private set; }

    /// <summary>The outfit worn when the screen opened, to tell whether anything changed.</summary>
    public Outfit WornAtOpen { get; private set; } = Outfit.Own;

    public bool Owns(string? garment) => wardrobe.Owns(garment);

    /// <summary>Opens the wardrobe on <paramref name="owned"/>'s clothes, with a boutique's stock to buy from (none at home).</summary>
    public void Open(Wardrobe owned, IEnumerable<Garment>? sells = null, string? name = null)
    {
        wardrobe = owned;
        stock.Clear();
        stock.AddRange(sells ?? Enumerable.Empty<Garment>());
        Name = string.IsNullOrWhiteSpace(name) ? IsBoutique ? "BOUTIQUE" : "WARDROBE" : name.ToUpperInvariant();
        IsActive = true;
        Asking = false;
        asking.Snap(false);
        Bought = Spent = 0;
        WornAtOpen = owned.Worn;
        openAge = time = 0f;
        yaw = 0f;
        upDown.Release();
        tried.Clear();
        Tab = 0;
        Fill(keepOn: owned.Worn[Slot]);
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
        Asking = false;
    }

    /// <summary>Lists the open slot's rows, the cursor on <paramref name="keepOn"/> (what is worn there, when the tab changes).</summary>
    private void Fill(string? keepOn)
    {
        rows.Clear();
        rows.Add(null);
        foreach (var g in ClothingDatabase.All.Where(g => g.Slot == Slot && (g.IsFree || wardrobe.Owns(g.Id) || stock.Contains(g))))
            rows.Add(g.Id);
        int at = rows.FindIndex(r => string.Equals(r, keepOn, StringComparison.OrdinalIgnoreCase));
        SelectedIndex = Math.Max(0, at);
        FirstRow = UiNav.Window(0, SelectedIndex, rows.Count, VisibleRows);
    }

    /// <summary>Left and right: the slot before or after, wrapping; the cursor goes to what is worn there.</summary>
    public void MoveTab(int dx)
    {
        if (dx == 0 || Asking) return;
        Tab = UiNav.Wrap(Tab, Math.Sign(dx), Outfit.Slots.Length);
        Fill(keepOn: wardrobe.Worn[Slot]);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// Up and down the slot's list (a step from a key kept down stops at either end); while "buy it?" is up, any
    /// direction moves between its two answers.
    /// </summary>
    public void Move(int dy, bool held = false)
    {
        if (dy == 0) return;
        if (Asking)
        {
            AskIndex = UiNav.Wrap(AskIndex, Math.Sign(dy), BuyChoices.Length);
            AudioManager.PlaySound("cursor");
            return;
        }
        if (rows.Count == 0) return;
        int row = held ? Math.Clamp(SelectedIndex + dy, 0, rows.Count - 1) : UiNav.Wrap(SelectedIndex, dy, rows.Count);
        if (held && row == SelectedIndex) return;
        SelectedIndex = row;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, rows.Count, VisibleRows);
        if (tick.Sounds(held)) AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// The A button: on a garment owned (or the look's own), puts it on; on one for sale, asks whether to buy it (or
    /// says the money doesn't reach); on BUY, buys it and puts it on. Returns what the player's money changes by.
    /// </summary>
    public int Confirm(int money, Action<string> onNotification)
    {
        if (Asking)
        {
            Asking = false;
            asking.Close();
            if (AskIndex != 0 || Selected is not { } buying)
            {
                AudioManager.PlaySound("cancel");
                return 0;
            }
            int cost = wardrobe.Buy(buying, money);
            if (cost < 0)
            {
                onNotification("You don't have enough money.");
                AudioManager.PlaySound("error");
                return 0;
            }
            Bought++;
            Spent += cost;
            AudioManager.PlaySound("select");
            onNotification($"You bought the {buying.Name} and put it on!");
            return -cost;
        }

        string? garment = SelectedIndex < rows.Count ? rows[SelectedIndex] : null;
        if (wardrobe.Owns(garment))
        {
            if (string.Equals(wardrobe.Worn[Slot], garment, StringComparison.OrdinalIgnoreCase)) return 0;
            wardrobe.Wear(Slot, garment);
            AudioManager.PlaySound("select");
            return 0;
        }

        var g = Selected!;
        if (g.Price > money)
        {
            onNotification("You don't have enough money.");
            AudioManager.PlaySound("error");
            return 0;
        }
        Asking = true;
        AskIndex = 0;
        asking.Open();
        AudioManager.PlaySound("select");
        return 0;
    }

    /// <summary>The B button: out of "buy it?", then out of the wardrobe in what is worn.</summary>
    public void Cancel()
    {
        if (Asking)
        {
            Asking = false;
            asking.Close();
        }
        else Close();
        AudioManager.PlaySound("cancel");
    }

    /// <summary>
    /// The character types tried on and not kept, for the engine to let go of once the screen is closed (each is a
    /// rig of its own on the graphics card).
    /// </summary>
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
        tick.Update(dt);

        // Turned toward the camera, swaying a little; round to show the back while the bag's tab is open
        float target = Slot == ClothingSlot.Bag ? MathF.PI * 0.82f : MathF.Sin(time * 0.6f) * 0.32f;
        yaw += (target - yaw) * Math.Min(1f, dt * 6f);

        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down),
            Asking ? 0 : InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        if (Asking) Move(dx != 0 ? dx : dy);
        else
        {
            MoveTab(dx);
            for (int i = 0; i < Math.Abs(dy); i++) Move(Math.Sign(dy), upDown.Repeating);
        }

        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) playerMoney += Confirm(playerMoney, onNotification);
    }

    // ------------------------------------------------------------------ the figure

    public const int FigureWidth = 640, FigureHeight = 960;

    /// <summary>
    /// Renders the player's figure in the outfit tried on, offscreen (call before the virtual screen's texture mode).
    /// An outfit not built yet is started in the background and the last one shown stays meanwhile, so the cursor
    /// never waits for a model.
    /// </summary>
    internal void Render(RenderContext context)
    {
        if (!IsActive) return;
        context.EnsureLoaded();
        if (!figureLoaded)
        {
            figure = Raylib.LoadRenderTexture(FigureWidth, FigureHeight);
            Raylib.SetTextureFilter(figure.Texture, TextureFilter.Bilinear);
            figureLoaded = true;
        }

        string look = PlayerIdentity.CharacterOf(PlayerIdentity.Look);
        string wanted = Outfit.Dress(look, Trial);
        tried.Add(wanted);
        if (CharacterModels.TryGet(wanted, context.Shaders) != null) shown = wanted;
        else if (shown == null || CharacterModels.TryGet(shown, context.Shaders) == null)
        {
            // Nothing ready yet: what is worn, which the field has built already
            string worn = Outfit.Dress(look, wardrobe.Worn);
            tried.Add(worn);
            shown = CharacterModels.TryGet(worn, context.Shaders) != null ? worn : null;
        }
        if (shown == null) return;

        var pose = new CharacterPose { Time = time, Blink = (time + 0.7f) % 3.6f < 0.12f };
        CharacterStudio.Render(context, shown, figure, pose, yaw, viewHeight: 1.62f, lookY: 0.66f);
    }

    /// <summary>True while the figure shown isn't yet the outfit tried on (it is being made).</summary>
    public bool Dressing => shown == null || !string.Equals(shown, Outfit.Dress(PlayerIdentity.CharacterOf(PlayerIdentity.Look), Trial), StringComparison.OrdinalIgnoreCase);

    public void Draw(int screenWidth, int screenHeight, int playerMoney)
    {
        if (!IsActive) return;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, rows.Count, VisibleRows);
        ModernUi.DrawWardrobe(screenWidth, screenHeight, this, playerMoney, figureLoaded && shown != null ? figure.Texture : null,
            Math.Clamp(openAge / AppearTime, 0f, 1f), asking.Shown, asking.Visible, time);
    }
}
