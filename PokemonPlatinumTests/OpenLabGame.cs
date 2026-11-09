using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The open test world laid into a whole game with no window (plan 32 · OW10): past the first chapter with a team
/// strong enough for the lab's trainers and wild Pokémon, the lab's map with its places, people, zones, level 0 and
/// navigation mesh (<see cref="OpenWorldPeopleTests.LabWithPeople"/>), the house's grid room for its door, and the
/// scripts its places name: the cavern's trigger, a person's talk and the grid's own trainer scripts.
/// </summary>
internal static class OpenLabGame
{
    public static readonly ScriptLibrary Scripts = ScriptLibrary.FromSources(
        ("lab_cave", """
            script Cavern
              say "The cavern's air is cold."
              setvar VAR_LAB_CAVE 1
              end
            """),
        ("common", """
            script Talk
              sayown
              end
            script Trainer
              if defeated self goto Beaten
              trainerline before
              battle self
              end
            label Beaten
              if rematch self goto Rematch
              trainerline after
              if result == 0 sayown
              end
            label Rematch
              trainerline before
              battle self rematch
              end
            script TrainerPair
              trainerline before
              trainerline before pair
              battle self and pair with partner
              end
            """));

    /// <summary>The game past the opening with two strong Pokémon, and the lab laid in: the map (not yet stood in) and the house's room.</summary>
    public static OpenMap Lay(GameEngine engine)
    {
        PokemonPlatinumTests.Headless.PastTheOpening(engine, level: 50);
        var game = engine.Drive;
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 50));
        game.UseScripts(Scripts);
        var map = OpenWorldPeopleTests.LabWithPeople();
        game.AddOpenMap(map);
        game.AddRoom(OpenWorldPlacesTests.LabHouse());
        return map;
    }
}
