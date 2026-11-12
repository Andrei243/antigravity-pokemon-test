using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.StageShow;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

public enum DressingPhase { ChoosePokemon, Slots, Pieces }

/// <summary>
/// The theatre's dressing room (plan 42 · PS1; style guide, "Interface (vector)", "The stage show"): the team to choose
/// from (no Egg), then the slots down the left, the trunk's pieces for the slot under the cursor at the right, and the
/// Pokémon on a dais in the middle, turning, wearing what is on it, with how many pieces of each mood it wears. The steps
/// take no input of their own, so tests and the harness drive it; <see cref="Outcome"/> is 1 when a dressing was kept.
/// </summary>
public sealed class DressingRoomScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>The side of the picture the Pokémon on the dais is rendered into.</summary>
    public const int PictureSize = 720;

    public bool IsActive { get; private set; }
    public DressingPhase Phase { get; private set; }
    public float Age { get; private set; }

    /// <summary>The team's members that may be dressed (no Egg), by their place in the team.</summary>
    public IReadOnlyList<Pokemon> Team { get; private set; } = Array.Empty<Pokemon>();

    public int TeamCursor { get; private set; }
    public int SlotCursor { get; private set; }
    public int PieceCursor { get; private set; }

    /// <summary>The Pokémon being dressed and its dressing.</summary>
    public Pokemon? Dressed { get; private set; }
    public Dressing Dressing { get; private set; } = new();
    public ShowRecords? Records { get; private set; }

    public int Outcome { get; private set; }

    /// <summary>The slots the Pokémon's model offers, once it is built (null while it is being made: every slot is shown).</summary>
    public Func<Pokemon, IReadOnlyList<PropSlot>?> SlotsOf { get; set; } = _ => null;

    public static IReadOnlyList<PropSlot> AllSlots { get; } = Enum.GetValues<PropSlot>();

    /// <summary>The slots shown: those the body has, in their order.</summary>
    public IReadOnlyList<PropSlot> Slots => Dressed == null ? AllSlots : SlotsOf(Dressed) ?? AllSlots;

    public PropSlot? SlotHere => SlotCursor >= 0 && SlotCursor < Slots.Count ? Slots[SlotCursor] : null;

    /// <summary>The pieces offered for the slot under the cursor: none first (to take a piece off), then the trunk's.</summary>
    public IReadOnlyList<ShowProp?> Pieces =>
        SlotHere is { } slot && Records != null ? new ShowProp?[] { null }.Concat(Records.Trunk.Of(slot)).ToList() : new ShowProp?[] { null };

    public void Open(IEnumerable<Pokemon> team, ShowRecords records)
    {
        Team = team.Where(p => !p.IsEgg).ToList();
        Records = records;
        Phase = DressingPhase.ChoosePokemon;
        TeamCursor = SlotCursor = PieceCursor = 0;
        Dressed = null;
        Dressing = new Dressing();
        Outcome = 0;
        Age = 0f;
        IsActive = Team.Count > 0;
        if (IsActive) AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    public void Move(int dx, int dy)
    {
        switch (Phase)
        {
            case DressingPhase.ChoosePokemon when Team.Count > 1 && (dx != 0 || dy != 0):
                TeamCursor = UiNav.Wrap(TeamCursor, dy != 0 ? Math.Sign(dy) : Math.Sign(dx), Team.Count);
                AudioManager.PlaySound("cursor");
                break;
            case DressingPhase.Slots when dy != 0 && Slots.Count > 1:
                SlotCursor = UiNav.Wrap(SlotCursor, Math.Sign(dy), Slots.Count);
                AudioManager.PlaySound("cursor");
                break;
            case DressingPhase.Slots when dx > 0:
                ToPieces();
                break;
            case DressingPhase.Pieces when dy != 0 && Pieces.Count > 1:
                PieceCursor = UiNav.Wrap(PieceCursor, Math.Sign(dy), Pieces.Count);
                AudioManager.PlaySound("cursor");
                break;
            case DressingPhase.Pieces when dx < 0:
                Phase = DressingPhase.Slots;
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    private void ToPieces()
    {
        if (SlotHere is not { } slot) return;
        Phase = DressingPhase.Pieces;
        // The cursor on the piece worn there, or on "none"
        string? worn = Dressing.In(slot);
        PieceCursor = Math.Max(0, Pieces.ToList().FindIndex(p => p?.Id == worn));
        AudioManager.PlaySound("select");
    }

    public void Confirm()
    {
        switch (Phase)
        {
            case DressingPhase.ChoosePokemon:
                Dressed = Team[TeamCursor];
                Dressing = Records?.DressingOf(Dressed) ?? new Dressing();
                // A piece kept for a slot this body has no more is taken off
                foreach (var slot in AllSlots.Where(s => !Slots.Contains(s)).ToList()) Dressing.Remove(slot);
                Phase = DressingPhase.Slots;
                SlotCursor = 0;
                AudioManager.PlaySound("select");
                break;
            case DressingPhase.Slots:
                ToPieces();
                break;
            case DressingPhase.Pieces:
                Choose(PieceCursor);
                break;
        }
    }

    /// <summary>The piece at <paramref name="index"/> of <see cref="Pieces"/> put on the slot (none takes it off); refused past four pieces.</summary>
    public bool Choose(int index)
    {
        if (SlotHere is not { } slot || index < 0 || index >= Pieces.Count) return false;
        var piece = Pieces[index];
        var old = Dressing.In(slot);
        Dressing.Remove(slot);
        if (piece != null && !Dressing.Wear(piece, Slots))
        {
            if (old != null && Data.Lookup(old) is { } back) Dressing.Wear(back);
            AudioManager.PlaySound("error");
            return false;
        }
        AudioManager.PlaySound(piece == null ? "cancel" : "select");
        Phase = DressingPhase.Slots;
        return true;
    }

    public void Cancel()
    {
        switch (Phase)
        {
            case DressingPhase.Pieces:
                Phase = DressingPhase.Slots;
                AudioManager.PlaySound("cancel");
                break;
            case DressingPhase.Slots:
                Keep();
                Phase = DressingPhase.ChoosePokemon;
                AudioManager.PlaySound("cancel");
                break;
            default:
                Close();
                break;
        }
    }

    /// <summary>The dressing kept for the Pokémon being dressed.</summary>
    public void Keep()
    {
        if (Dressed == null || Records == null) return;
        Records.Keep(Dressed, Dressing);
        Outcome = 1;
    }

    /// <summary>How many pieces worn are of each mood.</summary>
    public int MoodCount(PropMood mood) => Dressing.Matching(mood);

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else Move(InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Up, GameAction.Down));
    }

    // ------------------------------------------------------------------ the picture

    private RenderTexture2D picture;
    private bool pictureLoaded;
    public bool HasPicture { get; private set; }
    internal Texture2D Picture => picture.Texture;

    /// <summary>The Pokémon being dressed (or under the team's cursor) turning on the dais with its pieces, before the 2D pass.</summary>
    internal void Render(RenderContext context)
    {
        HasPicture = false;
        if (!IsActive) return;
        var pokemon = Dressed ?? (Team.Count > 0 ? Team[TeamCursor] : null);
        if (pokemon == null) return;
        if (!PokemonModels.TryGet(pokemon.LookName, out var model))
        {
            PokemonModels.Request(pokemon.LookName);
            return;
        }
        if (!pictureLoaded)
        {
            picture = Gpu.Target(PictureSize, PictureSize, "ui");
            Raylib.SetTextureFilter(picture.Texture, TextureFilter.Bilinear);
            pictureLoaded = true;
        }
        var props = Dressed != null ? Dressing.Props.ToList() : Records?.DressingOf(pokemon).Props.ToList();
        PokemonSprites.Render(context, model, SpriteView.Turntable, new PokePose { Time = Age }, picture, turn: Age * 0.8f, props: props);
        HasPicture = true;
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawDressingRoom(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }

    /// <summary>Lets go of the picture and the props' meshes once the room is left.</summary>
    internal void Free()
    {
        if (pictureLoaded) Gpu.Unload(picture);
        pictureLoaded = false;
        HasPicture = false;
        ShowPropModels.Unload();
    }

    private static class Data
    {
        public static ShowProp? Lookup(string id) => ShowPropDatabase.Get(id);
    }
}
