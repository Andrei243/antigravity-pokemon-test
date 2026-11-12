using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The close-up's tools, in the tray's order (plan 35 · BN2).</summary>
public enum CareTool { Hand, Treat, Brush, Cloth, Medicine }

/// <summary>What the close-up asks of the engine.</summary>
public enum CareRequest { None, Treat, Close }

/// <summary>
/// One Pokémon close up (plan 35 · BN2; style guide, "The care close-up"): a hand and a tray of five tools. The hand held
/// and moved over the body strokes it (strokes over the spot its body plan likes count double toward a pat), the brush and
/// the cloth wear away the marks of soil they take, the medicine cures, and a treat or a berry from the bag is eaten. The
/// rules are the care book's (<see cref="CareRules"/>); where things lie on the picture is <see cref="CareStage"/>'s. Its
/// logic takes no input (<see cref="MoveHand"/>, <see cref="Hold"/>, <see cref="Release"/>, <see cref="ChooseTool"/>,
/// <see cref="Feed"/>, <see cref="Cancel"/>, <see cref="Advance"/>); <see cref="Update"/> reads the keys into those.
/// </summary>
public sealed class CareScreen
{
    /// <summary>How fast the hand moves, in the picture's widths a second.</summary>
    public const float HandSpeed = 0.9f;

    /// <summary>How far strokes must go over the body for a pat (strokes over the liked spot count double).</summary>
    public const float PatStroke = 1.2f;

    /// <summary>How far the right tool must go over a mark to wear it away.</summary>
    public const float MarkWear = 0.9f;

    /// <summary>How long a lean into a pat and a chew last.</summary>
    public const float HappyTime = 1.4f, EatingTime = 1.6f;

    public bool IsActive { get; private set; }
    public Pokemon? Pokemon { get; private set; }
    public CareBook? Book { get; private set; }
    public int Day { get; private set; }
    public bool Night { get; private set; }

    public CareTool Tool { get; private set; }
    public Vector2 Hand { get; private set; } = new(0.5f, 0.7f);
    public bool Holding { get; private set; }
    public string Note { get; private set; } = "";
    public float Age { get; private set; }

    /// <summary>How far the strokes toward the next pat have gone.</summary>
    public float Stroked { get; private set; }

    /// <summary>How far each mark has been worn by its tool.</summary>
    public float WearOf(int mark) => mark is 0 or 1 ? wear[mark] : 0f;
    private readonly float[] wear = new float[CareRules.MostMarks];

    /// <summary>Where things lie on the picture: the body plan's until the model is built, the model's after.</summary>
    internal CareStage.Layout Layout { get; set; } = CareStage.Fallback(BodyPlan.Biped);

    /// <summary>The layout was worked out from the built model (not the body plan's stand-in).</summary>
    internal bool LaidOut { get; set; }

    private float happyFor = -1f, eatingFor = -1f;
    private CareRequest request;

    /// <summary>The page of the Pokémon looked after.</summary>
    public CareRecord? Page => Pokemon != null && Book != null ? Book.Of(Pokemon) : null;

    /// <summary>Opens on a Pokémon (not an Egg) with the book and the day; <paramref name="night"/> makes it sleepy.</summary>
    public bool Open(Pokemon pokemon, CareBook book, int day, bool night)
    {
        if (pokemon.IsEgg) return false;
        Pokemon = pokemon;
        Book = book;
        Day = day;
        Night = night;
        Tool = CareTool.Hand;
        Hand = new Vector2(0.5f, 0.7f);
        Holding = false;
        Stroked = 0f;
        Array.Clear(wear);
        happyFor = eatingFor = -1f;
        request = CareRequest.None;
        Layout = CareStage.Fallback(BodyPlan.Biped);
        LaidOut = false;
        Age = 0f;
        Note = Text.Of("care.note.welcome", ("name", pokemon.DisplayName));
        IsActive = true;
        return true;
    }

    /// <summary>The tool to the left (−1) or the right (+1), wrapping round.</summary>
    public void NextTool(int step)
    {
        if (!IsActive || step == 0) return;
        int count = Enum.GetValues<CareTool>().Length;
        ChooseTool((CareTool)(((int)Tool + Math.Sign(step) + count) % count));
    }

    public void ChooseTool(CareTool tool)
    {
        if (!IsActive || tool == Tool) return;
        Tool = tool;
        Holding = false;
        Note = Text.Of($"care.tool.help.{tool}");
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// The hand pressed: the HAND, the brush and the cloth start their strokes; the medicine cures; the treat asks the bag
    /// for a berry or a treat.
    /// </summary>
    public void Hold()
    {
        if (!IsActive || Pokemon is not { } p || Page is not { } page) return;
        switch (Tool)
        {
            case CareTool.Treat:
                if (!CareRules.WouldEat(page, p, Day)) { Refuse(Text.Of("camp.note.full", ("name", p.DisplayName))); return; }
                request = CareRequest.Treat;
                return;
            case CareTool.Medicine:
                if (!Layout.OnBody(Hand)) { Refuse(Text.Of("care.note.reach")); return; }
                if (CareRules.Cure(page, p))
                {
                    Note = Text.Of("care.note.cured", ("name", p.DisplayName));
                    happyFor = 0f;
                    AudioManager.PlaySound("heal");
                }
                else Refuse(Text.Of("care.note.nothingToCure", ("name", p.DisplayName)));
                return;
        }
        Holding = true;
    }

    // The hold came from the key, which lets it go when released (a tool's Hold is let go by Release alone)
    private bool heldByKey;

    public void Release()
    {
        Holding = false;
        heldByKey = false;
    }

    /// <summary>Moves the hand by a share of the picture; held, it strokes as it goes.</summary>
    public void MoveHand(Vector2 by)
    {
        if (!IsActive || by == Vector2.Zero) return;
        var from = Hand;
        Hand = Vector2.Clamp(Hand + by, new Vector2(0.04f), new Vector2(0.96f));
        if (Holding) Stroke(Vector2.Distance(from, Hand));
    }

    // A stroke of the length given at the hand's place, with the tool in hand
    private void Stroke(float length)
    {
        if (Pokemon is not { } p || Page is not { } page || length <= 0f) return;
        switch (Tool)
        {
            case CareTool.Hand:
                if (!Layout.OnBody(Hand)) return;
                bool liked = Vector2.Distance(Hand, Layout.Liked) <= CareStage.Reach;
                Stroked += liked ? 2f * length : length;
                if (Stroked < PatStroke) return;
                Stroked = 0f;
                bool counted = CareRules.Pat(page, p, Day);
                happyFor = 0f;
                Note = Text.Of(counted ? "care.note.petted" : "care.note.pettedAgain", ("name", p.DisplayName));
                AudioManager.PlayCry(p, CryMode.FieldEvent);
                return;
            case CareTool.Brush:
            case CareTool.Cloth:
                for (int i = 0; i < page.Marks.Count; i++)
                {
                    if (Vector2.Distance(Hand, Layout.Mark(i)) > CareStage.Reach) continue;
                    var mark = page.Marks[i];
                    if (CareRules.BrushTakes(mark) != (Tool == CareTool.Brush))
                    {
                        Note = Text.Of($"care.note.wrongTool.{Tool}");
                        return;
                    }
                    wear[i] += length;
                    if (wear[i] < MarkWear) return;
                    CareRules.Groom(page, p, mark, Day);
                    Array.Clear(wear);
                    happyFor = 0f;
                    Note = Text.Of($"care.note.cleaned.{mark}", ("name", p.DisplayName));
                    AudioManager.PlaySound("select");
                    return;
                }
                return;
        }
    }

    /// <summary>A berry or a treat picked from the bag is eaten (one of it taken out). True if it was.</summary>
    public bool Feed(ItemData item, Inventory bag)
    {
        if (Pokemon is not { } p || Page is not { } page || bag.GetQuantity(item) == 0) return false;
        bool food = item.Berry != null || item.Treat != null;
        bool ate = item.Treat != null ? Treats.Feed(page, p, Day, item) : item.Berry != null && CareRules.FeedBerry(page, p, Day, item);
        if (!ate)
        {
            Refuse(!food ? Text.Of("camp.note.noBerry") : Text.Of("camp.note.full", ("name", p.DisplayName)));
            return false;
        }
        bag.RemoveItem(item);
        eatingFor = 0f;
        var taste = Treats.TasteOf(item, p.Nature);
        Note = Text.Of($"care.note.ate.{taste}", ("name", p.DisplayName), ("item", item.DisplayName));
        AudioManager.PlayCry(p, CryMode.FieldEvent);
        return true;
    }

    /// <summary>The bag was put away with nothing picked.</summary>
    public void NothingPicked() => Note = Text.Of("care.note.welcome", ("name", Pokemon?.DisplayName ?? ""));

    private void Refuse(string why)
    {
        Note = why;
        AudioManager.PlaySound("error");
    }

    /// <summary>Back to the camp.</summary>
    public void Cancel()
    {
        request = CareRequest.Close;
        IsActive = false;
        Holding = false;
        AudioManager.PlaySound("cancel");
    }

    public CareRequest TakeRequest()
    {
        var r = request;
        request = CareRequest.None;
        return r;
    }

    public void Advance(float dt)
    {
        Age += dt;
        if (happyFor >= 0f) happyFor = happyFor + dt > HappyTime ? -1f : happyFor + dt;
        if (eatingFor >= 0f) eatingFor = eatingFor + dt > EatingTime ? -1f : eatingFor + dt;
    }

    /// <summary>The model's pose now: its head toward the hand, leaning into a pat, chewing, drooping at night or in a low mood.</summary>
    internal PokePose Pose()
    {
        var head = Layout.Head;
        var toward = new Vector2(-(Hand.X - head.X) * 2.5f, -(Hand.Y - head.Y) * 2.5f);
        var mood = Page is { } page ? CareRules.MoodOf(page.Mood) : CareMood.Calm;
        return new PokePose
        {
            Time = Age,
            Happy = happyFor >= 0f ? MathF.Sin(MathF.PI * happyFor / HappyTime) : Holding && Tool == CareTool.Hand && Layout.OnBody(Hand) ? 0.4f : 0f,
            Sleepy = Night || mood is CareMood.Glum or CareMood.Low ? 0.8f : 0f,
            LookAt = Vector2.Clamp(toward, new Vector2(-1f), new Vector2(1f)),
            Eating = eatingFor >= 0f ? MathF.Sin(MathF.PI * eatingFor / EatingTime) : 0f
        };
    }

    /// <summary>A frame of the keys: the arrows move the hand, Z holds it, the shoulders change the tool, Esc goes back.</summary>
    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        if (InputManager.IsActionPressed(GameAction.Cancel)) { Cancel(); return; }
        if (InputManager.IsActionPressed(GameAction.PageLeft)) NextTool(-1);
        else if (InputManager.IsActionPressed(GameAction.PageRight)) NextTool(1);
        if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            Hold();
            heldByKey = Holding;
        }
        else if (heldByKey && !InputManager.IsActionDown(GameAction.Confirm)) Release();
        float dx = InputManager.IsActionDown(GameAction.Right) ? 1f : InputManager.IsActionDown(GameAction.Left) ? -1f : 0f;
        float dy = InputManager.IsActionDown(GameAction.Down) ? 1f : InputManager.IsActionDown(GameAction.Up) ? -1f : 0f;
        if (dx != 0f || dy != 0f) MoveHand(new Vector2(dx, dy) * HandSpeed * dt);
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>The picture's side in pixels.</summary>
    public const int PictureSize = 1024;

    private RenderTexture2D picture;
    private bool pictureLoaded;

    /// <summary>What the picture holds this frame: the look rendered, or null while the still sprite stands in.</summary>
    public string? PictureShows { get; private set; }

    internal Texture2D? Picture => pictureLoaded ? picture.Texture : null;

    /// <summary>Renders the Pokémon offscreen if its model is built, never waiting; call before the virtual screen's pass.</summary>
    internal void Render(RenderContext context)
    {
        if (!IsActive || Pokemon is not { } p) return;
        context.EnsureLoaded();
        if (!pictureLoaded)
        {
            picture = Gpu.Target(PictureSize, PictureSize, "care");
            Raylib.SetTextureFilter(picture.Texture, TextureFilter.Bilinear);
            pictureLoaded = true;
        }
        PictureShows = null;
        if (PokemonModels.TryGet(p.LookName, out var model))
        {
            if (!LaidOut)
            {
                Layout = CareStage.Of(model);
                LaidOut = true;
            }
            PokemonSprites.Render(context, model, SpriteView.Turntable, Pose(), picture, turn: CareStage.Turn, zoom: CareStage.Zoom);
            PictureShows = p.LookName;
        }
    }

    /// <summary>Lets the picture go (the camp is closed).</summary>
    internal void Unload()
    {
        if (!pictureLoaded) return;
        Gpu.Unload(picture);
        pictureLoaded = false;
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawCare(screenWidth, screenHeight, this);
    }
}
