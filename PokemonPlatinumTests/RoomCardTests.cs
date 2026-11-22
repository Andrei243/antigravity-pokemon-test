using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// A room's cards (people, Pokémon) are straightened about the floor each stands on (style guide, "Rooms"): the room
/// keeps its perspective, so a card on a raised floor straightened about the floor far below was drawn beside its tile
/// (the Canalave Gym's upper decks).
/// </summary>
public class RoomCardTests
{
    // The vertex shader's divisor for a vertex at depth w and height y: w + (y · pitchSin + uprightBase) · upright
    private static float Divisor(float w, float y, float pitchSin, float uprightBase) => w + (y * pitchSin + uprightBase) * ArtLook.FieldUpright;

    [Theory]
    [InlineData(0f)]
    [InlineData(1.5f)]
    [InlineData(10f)]
    [InlineData(30f)]
    public void ACardsFeetStayWhereTheRoomDrawsTheirTile(float floor)
    {
        float sin = System.MathF.Sin(MapScene.OutdoorPitchDeg * System.MathF.PI / 180f);
        float b = FieldShaders.CardBase(floor, sin);
        // At the feet the divisor is the vertex's own depth: the card's foot is drawn in the room's true perspective
        Assert.Equal(40f, Divisor(40f, floor, sin, b), 4);
        // A tile above the feet it is the depth of the point at the feet's height under it, whatever the floor
        Assert.Equal(Divisor(40f, 1f, sin, 0f), Divisor(40f, floor + 1f, sin, b), 3);
    }

    [Fact]
    public void AFloorAtNoughtChangesNothing() => Assert.Equal(0f, FieldShaders.CardBase(0f, 0.86f));
}
