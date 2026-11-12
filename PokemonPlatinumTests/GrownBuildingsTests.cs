using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// A grown region's world sized to a grown body (style guide, "Scale in a grown region", "A grown region's buildings",
/// "A ride's seat"; "Festivals", "In a grown region"): which regions build so, a grown building's storey, door and
/// windows, a ride's seat by species and the lei arch's grown size; and that a chibi region and Kalos keep their numbers.
/// </summary>
public class GrownBuildingsTests
{
    [Fact]
    public void AlolaAndGalarBuildToAGrownBodyAndKalosAndTheChibiRegionsDoNot()
    {
        Assert.True(GrownScale.Of(RegionDatabase.Get(RegionDatabase.Alola)));
        Assert.True(GrownScale.Of(RegionDatabase.Get(RegionDatabase.Galar)));
        Assert.False(GrownScale.Of(RegionDatabase.Get(RegionDatabase.Kalos)));
        Assert.False(GrownScale.Of(RegionDatabase.Get(RegionDatabase.Sinnoh)));
        Assert.False(GrownScale.Of((Region?)null));
    }

    [Fact]
    public void AGrownBuildingsGroundStoreyIsTwentyTexelsTallerAndAChibiOneIsAsItWas()
    {
        var chibi = new BuildingStyle { Roof = RoofShape.Hip };
        var grown = chibi with { Grown = true };
        Assert.Equal(BuildingArt.PitchedWall, chibi.WallHeight);
        Assert.Equal(BuildingArt.PitchedWall + BuildingArt.GrownLift, grown.WallHeight);
        var flat = new BuildingStyle { Roof = RoofShape.Flat, Storeys = 2 };
        Assert.Equal(flat.WallHeight + BuildingArt.GrownLift, (flat with { Grown = true }).WallHeight);
        Assert.Equal(BuildingArt.PortalHeight, BuildingArt.PortalHeightOf(chibi));
        Assert.Equal(BuildingArt.PortalHeight + BuildingArt.GrownLift, BuildingArt.PortalHeightOf(grown));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AGrownOpenDoorIsFiftySixTexelsHighWithItsTopAndFootAsTheChibiDoorsAre(bool glass, bool pair)
    {
        foreach (int frame in new[] { 1, 2 })
        {
            var chibi = BuildingArt.OpenDoor(glass, pair, frame, ArtSheet.HomeLight);
            var grown = BuildingArt.OpenDoor(glass, pair, frame, ArtSheet.HomeLight, grown: true);
            Assert.Equal(chibi.Width, grown.Width);
            Assert.Equal(chibi.Height + BuildingArt.GrownDoorRise, grown.Height);
            Assert.InRange(grown.Height, 56, 57);
            // The top (the lintel, the glass) and the foot (the sill) are the chibi door's own, texel for texel
            for (int x = 0; x < chibi.Width; x++)
            {
                for (int y = 0; y < chibi.Height - 5; y++) Assert.Equal(chibi.Get(x, y), grown.Get(x, y));
                for (int y = 1; y <= 5; y++) Assert.Equal(chibi.Get(x, chibi.Height - y), grown.Get(x, grown.Height - y));
            }
        }
    }

    [Fact]
    public void ARidesSeatIsItsSpeciesOwnAndKalossMamoswineKeepsTheGates()
    {
        foreach (var (species, seat) in RideSeats.Table)
        {
            Assert.InRange(seat.Seat, 0.3f, 1f);
            Assert.InRange(seat.Tuck, 0f, 1f);
            Assert.InRange(seat.Back, 0f, 0.6f);
            Assert.True(PokemonDatabase.Get(species) != null, species);
        }
        Assert.Equal(RideSeats.Land, RideSeats.Of("Mamoswine", water: false));
        Assert.Equal(RideSeats.Land, RideSeats.Of("Nosuchmon", water: false));
        Assert.Equal(RideSeats.Water, RideSeats.Of(null, water: true));
        // A small ride's rider folds the legs up and sits under the horns; a Mudsdale's sits upright
        Assert.True(RideSeats.Of("Stoutland", false).Tuck > RideSeats.Of("Mudsdale", false).Tuck);
        Assert.True(RideSeats.Of("Tauros|grown", false).Seat < RideSeats.Land.Seat);
    }

    [Fact]
    public void TheLeiArchIsLargerWhereTheWorldIsGrownAndAsItWasElsewhere()
    {
        var plain = ThingCards.Paint(PropType.FestivalLeiArch, grown: false);
        Assert.Equal((FestivalArt.LeiWidth, FestivalArt.LeiHeight), (plain.Width, plain.Height));
        var grown = ThingCards.Paint(PropType.FestivalLeiArch, grown: true);
        Assert.Equal((FestivalArt.GrownLeiWidth, FestivalArt.GrownLeiHeight), (grown.Width, grown.Height));
        // Only the lei arch has a grown size; another festival thing is the same either way
        var lanterns = ThingCards.Paint(PropType.FestivalLanterns, grown: true);
        Assert.Equal((FestivalArt.LanternsWidth, FestivalArt.LanternsHeight), (lanterns.Width, lanterns.Height));
    }
}
