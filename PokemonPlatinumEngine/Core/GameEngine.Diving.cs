using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Dive (plan 21 · H5; Emerald's <c>TrySetDiveWarp</c> and <c>FldEff_UseDive</c>): a surfer on dark water goes down to
/// the same tile of the sea floor under it, and a diver on the floor comes back up to the same tile of the sea above,
/// behind a fade. The rules of where are <see cref="FieldMovement.CanDive"/> and <see cref="FieldMovement.CanSurface"/>;
/// the question and the badge are the scripts' (<c>common.Dive</c>, <c>common.Surface</c>).
/// </summary>
public partial class GameEngine
{
    /// <summary>
    /// Takes the player down or up where they are, as a script's <c>dive</c> asks. False, with nothing done, where there is
    /// no floor below that is open, no sea above that may be come up to, or nowhere to arrive on the other side.
    /// </summary>
    internal bool Dive()
    {
        var map = flow.Map;
        var player = flow.Player;
        int x = player.GridX, y = player.GridY;
        bool up = map.IsUnderwater;
        var onto = up
            ? FieldMovement.CanSurface(map, x, y) ? map.EmergeTargetAt(x, y) : null
            : FieldMovement.CanDive(map, x, y, new Walker(player.Mode, player.HeightOn(map), Moves: FieldMoves.Dive)) ? map.DiveTargetAt(x, y) : null;
        if (onto == null) return false;
        var target = MapDatabase.Get(onto.Map);
        // The floor and the sea are laid tile for tile, so the other side is the same tile (or, where the original names
        // one place, that place: plan 21 · H8, part b): open sea to surf on going up, open floor to swim on going down
        // (the import holds the original's maps to it; this keeps a slip from stranding anyone)
        int tx = onto.X, ty = onto.Y;
        if (up ? !FieldMovement.SurfacesOnto(target, tx, ty) : !target.InBounds(tx, ty) || target.IsSolid(tx, ty)) return false;

        AudioManager.PlaySound("dive");
        PlayAreaMusic(target, tx, ty);
        // Laid tile for tile the diver keeps their facing; put down at one place, they face as it says
        bool laid = (up ? map.AreaAt(x, y)?.Emerge : map.AreaAt(x, y)?.Dive) != null;
        var facing = laid ? player.Facing : onto.Facing;
        StartTransition(GameState.Overworld, () =>
        {
            flow.Map = target;
            flow.Player.SetPosition(tx, ty, facing);
            if (up) flow.Player.Surface();
            else flow.Player.StartDive();
            world.Life.Clear();
            ArriveOnMap();
            AnnounceLocation();
        });
        return true;
    }
}
