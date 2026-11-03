using System;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Cameron's photo album looked through (plan 20, part q; style guide, "Menu screens", "The photo album"): one
/// photograph to a page, left and right through them, and with the album open at a page the Confirm button asks whether
/// to throw that one away (a second Confirm does, Cancel keeps it). Its steps take no input of their own, so tests and
/// the harness drive it.
/// </summary>
public sealed class PhotoAlbumScreen
{
    private const float AppearTime = 0.35f;

    public bool IsActive { get; private set; }

    /// <summary>The page open, 0 for the first photograph.</summary>
    public int Page { get; private set; }

    /// <summary>Whether the page's photograph is being asked about, to be thrown away.</summary>
    public bool AskingToRemove { get; private set; }

    public float Age { get; private set; }

    private PhotoAlbum album = new();

    public PhotoAlbum Album => album;

    /// <summary>The photograph on the open page, or null for an empty album.</summary>
    public Photo? Shown => Page < album.Count ? album.Photos[Page] : null;

    /// <summary>The name a place's key is shown by (the engine hands it the world's areas); the key itself until then.</summary>
    public Func<string, string> PlaceOf { get; set; } = key => key;

    /// <summary>The kind of place a key is (plan 20, part t: the scene its print is drawn on); a route until the engine says.</summary>
    public Func<string, PhotoSetting> SettingOf { get; set; } = _ => PhotoSetting.Route;

    /// <summary>Whether a place is in HeartGold's Kanto (its place card says KANTO, otherwise JOHTO).</summary>
    public Func<string, bool> InKantoOf { get; set; } = _ => false;

    /// <summary>The scene the open page's photograph is printed on, or null for an empty album.</summary>
    public PhotoSceneLook? Scene => Shown is { } photo ? PhotoScenes.Look(SettingOf(photo.Place)) : null;

    public void Open(PhotoAlbum from)
    {
        album = from;
        IsActive = true;
        Page = 0;
        AskingToRemove = false;
        Age = 0f;
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AskingToRemove = false;
        AudioManager.PlaySound("menu_close");
    }

    /// <summary>A page back or on, wrapping round; nothing while a question is asked.</summary>
    public void Turn(int dx)
    {
        if (dx == 0 || AskingToRemove || album.Count < 2) return;
        Page = UiNav.Wrap(Page, Math.Sign(dx), album.Count);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Asks to throw the page's photograph away, and a second time does it.</summary>
    public void Confirm()
    {
        if (Shown == null) return;
        if (!AskingToRemove)
        {
            AskingToRemove = true;
            AudioManager.PlaySound("select");
            return;
        }
        album.RemoveAt(Page);
        AskingToRemove = false;
        if (Page >= album.Count) Page = Math.Max(0, album.Count - 1);
        AudioManager.PlaySound("select");
    }

    /// <summary>Keeps the photograph if asked about it, and closes the album otherwise.</summary>
    public void Cancel()
    {
        if (AskingToRemove)
        {
            AskingToRemove = false;
            AudioManager.PlaySound("cancel");
            return;
        }
        Close();
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (dx != 0) Turn(dx);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawPhotoAlbum(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
