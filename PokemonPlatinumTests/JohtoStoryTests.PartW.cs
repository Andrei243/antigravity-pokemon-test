using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The cabin's night by the clock (plan 20, part w; the rulings' "The night aboard by its own light"): the crossing
/// holds the field's light at night in the cabin and at morning on the deck, the picture's alone, until the player next
/// goes somewhere.
/// </summary>
public partial class JohtoStoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheNightAboardIsLitAsNightInTheCabinAndAsMorningOnTheDeck(bool homeward)
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 40));
        if (homeward) game.Story.Set("FLAG_JOHTO_SS_AQUA_AT_VERMILION");
        game.Arrive("SSAqua_1F", 24, 18);
        game.Answers.Enqueue(0);
        var host = game.Talk("p01r0301_seaman_2_2");
        var log = host.Log.ToList();
        int cabin = log.FindIndex(l => l.StartsWith("warp SSAqua_1F_SouthwestRooms"));
        int deck = log.FindIndex(l => l.StartsWith("warp SSAqua_1F "));
        Assert.True(cabin >= 0 && deck > cabin);
        // Night is held just after the warp into the cabin, before its fade opens; morning just after the warp back
        Assert.Equal("lighting 21", log[cabin + 1]);
        Assert.Equal("lighting 7", log[deck + 1]);
        // ...and the deck is still held at morning as the sailor says they've docked
        Assert.Equal(7f, host.HeldLight);
        // The hour the rules read is the clock's all the while
        Assert.Equal(10, host.Hour);
    }

    [Fact]
    public void AWarpGivesAHeldLightBackToTheClock()
    {
        var host = new HeadlessScriptHost();
        host.HoldLight(21f);
        Assert.Equal(21f, host.HeldLight);
        host.Warp("SSAqua_1F", 24, 18, null);
        Assert.Null(host.HeldLight);
        host.HoldLight(7f);
        host.HoldLight(null);
        Assert.Null(host.HeldLight);
    }

    [Fact]
    public void LightingReadsAnHourOrTheClock()
    {
        var script = ScriptParser.Parse("x", "script S\n  lighting 20.5\n  lighting clock\n  end\n").Single();
        Assert.Equal(Op.Lighting, script.Code[0].Op);
        Assert.True(script.Code[0].Option);
        Assert.Equal(20.5f, script.Code[0].Factor);
        Assert.False(script.Code[1].Option);
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("x", "script S\n  lighting 25\n  end\n"));
    }
}
