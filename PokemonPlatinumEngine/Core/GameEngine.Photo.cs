using System;
using System.Numerics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

// Photo mode in the field (plan 12 · Q16, its second half; style guide, "Field menus and notices", "Photo mode")
public partial class GameEngine
{
    // The photo being composed; null outside photo mode
    private PhotoView? photo;

    /// <summary>
    /// The Photo action (F9, the pad's Select, <see cref="GameAction.Photo"/>) in the field with nothing under way:
    /// photo mode. Nothing in a game with no window, which has no picture to take.
    /// </summary>
    private void KeepThePhotoKey()
    {
        if (IsHeadless || InputManager.CaptureRequested || !InputManager.IsActionPressed(GameAction.Photo)) return;
        OpenPhoto();
    }

    /// <summary>Begins a photo where the player stands; false where the field isn't free (the start menu could not SAVE).</summary>
    internal bool OpenPhoto()
    {
        if (IsHeadless || !CanSaveNow || flow.StartMenu.IsActive) return false;
        var map = flow.Map;
        bool outdoors = !map.IsIndoors && !map.IsCave && !map.IsVoid;
        var origin = new Vector2(flow.Player.PixelX / Player.TileSize + 0.5f, flow.Player.PixelY / Player.TileSize + 0.5f);
        photo = new PhotoView(origin, outdoors);
        barsWanted = false;
        zoomAsked = world.Camera.Zoom;
        world.Photo = photo;
        world.Camera.Pan(origin.X, origin.Y, 0.01f);
        flow.State = GameState.Photo;
        AudioManager.PlaySound("menu_open");
        return true;
    }

    /// <summary>Leaves photo mode: the camera brought back to the player over 0.3 s, and the field going on.</summary>
    internal void ClosePhoto()
    {
        if (photo == null) return;
        photo = null;
        barsWanted = false;
        zoomAsked = 1f;
        world.Photo = null;
        world.Camera.Release(0.3f);
        if (flow.State == GameState.Photo) flow.State = GameState.Overworld;
    }

    /// <summary>A frame of photo mode: the buttons read, and the photo handed to the camera kit. The field stands still.</summary>
    private void UpdatePhoto(float dt)
    {
        var p = photo;
        if (p == null)
        {
            flow.State = GameState.Overworld;
            return;
        }
        if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            AudioManager.PlaySound("cancel");
            if (p.PanelOpen) p.PanelOpen = false;
            else
            {
                ClosePhoto();
                return;
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            // The picture is the next frame drawn, with nothing of the interface in it
            AskForScreenshot();
        }
        else if (InputManager.IsActionPressed(GameAction.Menu))
        {
            p.PanelOpen = !p.PanelOpen;
            p.InterfaceHidden = false;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Item))
        {
            p.InterfaceHidden = !p.InterfaceHidden;
            AudioManager.PlaySound("cursor");
        }

        if (InputManager.IsActionPressed(GameAction.PageLeft)) p.ZoomBy(-1);
        if (InputManager.IsActionPressed(GameAction.PageRight)) p.ZoomBy(1);

        if (p.PanelOpen)
        {
            if (InputManager.IsActionPressed(GameAction.Up)) { p.MoveRow(-1); AudioManager.PlaySound("cursor"); }
            if (InputManager.IsActionPressed(GameAction.Down)) { p.MoveRow(1); AudioManager.PlaySound("cursor"); }
            int by = InputManager.IsActionPressed(GameAction.Left) ? -1 : InputManager.IsActionPressed(GameAction.Right) ? 1 : 0;
            if (by != 0) AudioManager.PlaySound(p.Change(by) ? "cursor" : "error");
        }
        else
        {
            var way = Vector2.Zero;
            if (InputManager.IsActionDown(GameAction.Left)) way.X -= 1f;
            if (InputManager.IsActionDown(GameAction.Right)) way.X += 1f;
            if (InputManager.IsActionDown(GameAction.Up)) way.Y -= 1f;
            if (InputManager.IsActionDown(GameAction.Down)) way.Y += 1f;
            p.Pan(way, dt);
        }

        KeepThePhotoInView(p);
        world.RunCamera(dt);
    }

    /// <summary>Hands the photo to the camera kit: where it looks, its lens and the cinema bars.</summary>
    private void KeepThePhotoInView(PhotoView p)
    {
        var rig = world.Camera;
        rig.Pan(p.Spot.X, p.Spot.Y, 0.01f);
        // Asked once for each new factor: asked every frame, the ease would begin again and never arrive
        if (p.Zoom != zoomAsked)
        {
            rig.ZoomTo(p.Zoom, 0.15f);
            zoomAsked = p.Zoom;
        }
        bool cinema = p.Frame == PhotoFrame.Cinema;
        if (cinema != barsWanted)
        {
            rig.Letterbox(cinema, 0.2f);
            barsWanted = cinema;
        }
    }

    // Whether the cinema bars were asked for (they take a moment to come in), and the zoom last asked of the rig
    private bool barsWanted;
    private float zoomAsked = 1f;

    /// <summary>The field under a photo, its frame, and the panel and hint unless the picture is being taken or they are hidden.</summary>
    private void DrawPhoto()
    {
        world.DrawToScreen(VirtualWidth, VirtualHeight);
        DrawLetterbox();
        var p = photo;
        if (p == null) return;
        if (p.Frame == PhotoFrame.Mat) ModernUi.PhotoMat(VirtualWidth, VirtualHeight);
        if (screenshotWanted || p.InterfaceHidden) return;
        ModernUi.PhotoPanel(p);
        ModernUi.PhotoHint(VirtualWidth, VirtualHeight, p.PanelOpen);
    }

    internal sealed partial class Driver
    {
        /// <summary>Photo mode begun where the player stands, as the Photo key begins it; false where the field isn't free.</summary>
        public bool OpenPhoto() => game.OpenPhoto();

        /// <summary>The photo being composed; null outside photo mode. A tool changes it and the next frame hands it to the camera.</summary>
        public PhotoView? Photo => game.photo;

        /// <summary>Photo mode left, as B leaves it.</summary>
        public void ClosePhoto() => game.ClosePhoto();
    }
}
