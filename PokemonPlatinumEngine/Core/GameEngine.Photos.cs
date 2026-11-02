using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Johto's last leftovers in the game (plan 20, part q): Cameron's photo album opened from a script (its rules are
/// GPU-free in <c>Models/PhotoAlbum.cs</c>, its screen <c>UI/PhotoAlbumScreen.cs</c>), and HeartGold's calls the story
/// makes (<see cref="StoryCalls"/>), rung on a step as Kurt's is.
/// </summary>
public partial class GameEngine
{
    /// <summary>Opens Cameron's photo album at its first page, a place's name told as the language names it.</summary>
    private void OpenPhotoAlbum()
    {
        var screen = flow.PhotoAlbumScreen;
        screen.PlaceOf = key => MapDatabase.MapNames.Select(MapDatabase.Get).SelectMany(m => m.Areas).FirstOrDefault(a => a.Key == key)?.DisplayName is { Length: > 0 } name
            ? PlayerIdentity.Fill(LocalNames.PlaceShown(key, name)) : key;
        flow.State = GameState.PhotoAlbum;
        screen.Open(flow.Photos);
    }

    private void UpdatePhotoAlbum(float dt)
    {
        flow.PhotoAlbumScreen.Update(dt);
        if (flow.PhotoAlbumScreen.IsActive) return;
        flow.State = GameState.Overworld;
    }

    /// <summary>
    /// A step's chance of a call the story makes (<see cref="StoryCalls"/>): rung at once when one is due, on the same
    /// conditions as every other call (the Pokégear, no script, the field quiet, not underground or in a cave).
    /// </summary>
    private bool StoryCallOnAStep()
    {
        if (!HasPokegear || ScriptRunning || flow.State != GameState.Overworld || IsUnderground) return false;
        if (flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.IsCave ?? flow.Map.IsCave) return false;
        if (StoryCalls.Ring(flow.Story, flow.Party, flow.Boxes) is not { } script) return false;
        flow.Story.SetVar(Pokegear.CallVar, Pokegear.CallTalk);
        AudioManager.PlaySound("phone_ring");
        return StartScript(script, file: PhoneScripts);
    }
}
