using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

// Photo mode in battle (plan 12 · Q17; style guide, "Camera (G8)", "A held shot")
public partial class GameEngine
{
    // The photo being composed in a battle; null outside it. While it is there the battle is not updated.
    private BattlePhotoView? battlePhoto;

    // The battle the photo was begun in: another battle on screen lets it go
    private PokemonPlatinumEngine.Battle.BattleEngine? battlePhotographed;

    /// <summary>
    /// The Photo action in a battle: the battle stilled, the director held on the shot it had on screen. Nothing in a
    /// game with no window.
    /// </summary>
    private void KeepTheBattlePhotoKey()
    {
        if (battlePhoto != null || IsHeadless || InputManager.CaptureRequested || !InputManager.IsActionPressed(GameAction.Photo)) return;
        OpenBattlePhoto();
    }

    /// <summary>Begins a photo of the battle on screen from the shot the director has; false with no battle shown.</summary>
    internal bool OpenBattlePhoto()
    {
        if (IsHeadless || flow.State != GameState.Battle || flow.Battle == null || battlePhoto != null) return false;
        battlePhoto = new BattlePhotoView(battleRenderer.CurrentShot);
        battlePhotographed = flow.Battle;
        battleRenderer.Photo = battlePhoto;
        AudioManager.PlaySound("menu_open");
        return true;
    }

    /// <summary>Leaves photo mode in battle: the director let go, and the battle going on from where it stood.</summary>
    internal void CloseBattlePhoto()
    {
        battlePhoto = null;
        battlePhotographed = null;
        battleRenderer.Photo = null;
    }

    /// <summary>A frame of photo mode in battle: the buttons read and nothing of the battle advanced.</summary>
    /// <returns>Whether the frame was the photo's (the battle is then not updated).</returns>
    private bool UpdateBattlePhoto(float dt)
    {
        var p = battlePhoto;
        if (p == null) return false;
        if (flow.State != GameState.Battle || flow.Battle == null || !ReferenceEquals(flow.Battle, battlePhotographed))
        {
            CloseBattlePhoto();
            return false;
        }
        if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            AudioManager.PlaySound("cancel");
            if (p.PanelOpen) p.PanelOpen = false;
            else CloseBattlePhoto();
            // B never reaches the battle: the frame is the photo's either way
            return true;
        }
        if (InputManager.IsActionPressed(GameAction.Confirm)) AskForScreenshot();
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
            p.Turn(way, dt);
        }
        return true;
    }

    /// <summary>A battle under a photo: the stage, its frame, and the panel and hint unless taken or hidden; no HUD.</summary>
    private bool DrawBattlePhoto()
    {
        var p = battlePhoto;
        if (p == null || flow.Battle == null) return false;
        battleRenderer.DrawField(flow.Battle, VirtualWidth, VirtualHeight);
        if (p.Frame == PhotoFrame.Mat) ModernUi.PhotoMat(VirtualWidth, VirtualHeight);
        else if (p.Frame == PhotoFrame.Cinema) ModernUi.PhotoBars(VirtualWidth, VirtualHeight);
        if (screenshotWanted || p.InterfaceHidden) return true;
        ModernUi.BattlePhotoPanel(p);
        ModernUi.PhotoHint(VirtualWidth, VirtualHeight, p.PanelOpen);
        return true;
    }

    internal sealed partial class Driver
    {
        /// <summary>Photo mode begun in the battle on screen, as the Photo key begins it; false with no battle shown.</summary>
        public bool OpenBattlePhoto() => game.OpenBattlePhoto();

        /// <summary>The photo being composed in a battle; null outside it. A tool changes it and the next frame draws it.</summary>
        public BattlePhotoView? BattlePhoto => game.battlePhoto;

        /// <summary>Photo mode in battle left, as B leaves it.</summary>
        public void CloseBattlePhoto() => game.CloseBattlePhoto();
    }
}
