using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.OpenWorld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW9 in the whole game with no window: what the open renderer is handed to draw beside the player is one
/// list (<c>GameEngine.GatherOpenFigures</c>): the map's people in their looks of the grown kit and plan 32 · OW7's wild
/// Pokémon at their feet, facing their way, at their species' heights. Nothing of it is drawn here. It seeds the dice,
/// so it runs with the recordings, alone.
/// </summary>
[Collection("Recordings")]
public class OpenFiguresGameTests
{
    [Fact]
    public void ThePeopleAndTheWildAreTheFiguresTheRendererIsHanded()
    {
        Headless.Play(engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            var map = OpenWorldPeopleTests.LabWithPeople();
            game.AddOpenMap(map);
            game.PlaceOpen(map, "north_meadow");
            var zone = map.Zones.Single(z => z.Name == "south_meadow");
            var at = OpenPos.FromMetres(16, map.Ground.Footing(16, 22, 20, -20, walkableOnly: true)!.Value.Y, 22);
            var bidoof = game.OpenWild!.Put(map, zone, zone.Slots.Single(s => s.Species == "Bidoof"), 4, at, OpenYaw.East);

            var figures = engine.GatherOpenFigures(game.OpenField!);
            // The lab's three people, each in a look of the grown kit: the walker his own, the trainers their classes'
            var people = figures.Where(f => f.Kind == OpenFigureKind.Person).ToList();
            Assert.Equal(new[] { "person:camper_west", "person:picnicker_east", "person:walker" }, people.Select(p => p.Key).OrderBy(k => k).ToArray());
            Assert.Equal(CharacterStyle.InKit(CharacterStyles.Default.CharacterFor("gentleman"), CharacterKit.Grown), people.Single(p => p.Key == "person:walker").Look);
            Assert.All(people, p => Assert.False(p.Exclaiming));
            // The wild Bidoof where it was put, facing east, at its species' height
            var wild = Assert.Single(figures, f => f.Key == "wild:" + bidoof.Id);
            Assert.Equal((OpenFigureKind.Pokemon, "Bidoof"), (wild.Kind, wild.Look));
            Assert.Equal(PokemonDatabase.Get("Bidoof")!.Height, wild.Height);
            Assert.Equal(16f, wild.Feet.X, 3);
            Assert.Equal(22f, wild.Feet.Z, 3);
            Assert.Equal((float)OpenYaw.Radians(OpenYaw.East), wild.Yaw, 4);
            // Nothing else stands there
            Assert.Equal(4, figures.Count);
        });
    }
}
