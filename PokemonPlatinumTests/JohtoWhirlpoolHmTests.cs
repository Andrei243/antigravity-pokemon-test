using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// HM10, Whirlpool (plan 20 · J11; the user's decision of 2026-10-31): HeartGold's HM05, the tenth HM here since
/// Platinum's eight keep their numbers and Hoenn's Dive is the ninth. Lance gives it as the hideout's generator stops
/// (<c>JohtoStoryTests</c> plays that); a game that cleared the hideout before is handed it in Mahogany Town. Its
/// compatibility is PokeAPI's: whoever learns Whirlpool by a machine in any game. docs/mechanics/rulings.md, "HM10,
/// Whirlpool".
/// </summary>
public class JohtoWhirlpoolHmTests
{
    private static ItemData Hm => ItemDatabase.Get("HM10")!;

    [Fact]
    public void HmTenTeachesWhirlpoolAndIsAnHm()
    {
        Assert.Equal("Whirlpool", Hm.TeachesMove);
        Assert.Equal(ItemPocket.TMsAndHMs, Hm.Pocket);
        Assert.True(MoveTeaching.IsHm(Hm));
        Assert.True(MoveTeaching.IsHmMove("Whirlpool"));
        Assert.Same(Hm, FieldHelp.HmOf(FieldMove.Whirlpool));
        // Dive keeps the ninth
        Assert.Equal("Dive", ItemDatabase.Get("HM09")!.TeachesMove);
        // The field move it teaches is the one the whirlpools ask, with Johto's Glacier Badge
        Assert.Equal("Glacier", FieldMoveRules.BadgeFor(FieldMove.Whirlpool, "Johto"));
    }

    [Theory]
    [InlineData("Lapras")]
    [InlineData("Totodile")]
    [InlineData("Feraligatr")]
    [InlineData("Lugia")]
    [InlineData("Suicune")]
    [InlineData("Gyarados")]
    [InlineData("Dragonite")]
    [InlineData("Piplup")]
    [InlineData("Buizel")]
    public void WaterPokemonThatLearnWhirlpoolByAMachineLearnTheHm(string species) =>
        Assert.Contains("HM10", PokemonDatabase.Get(species)!.TmMoves!);

    [Theory]
    [InlineData("Charmander")]
    [InlineData("Pidgey")]
    [InlineData("Pikachu")]
    [InlineData("Ho-Oh")]
    public void PokemonThatNeverLearnedItByAMachineDont(string species) =>
        Assert.DoesNotContain("HM10", PokemonDatabase.Get(species)!.TmMoves ?? new());

    [Fact]
    public void EveryWaterTypeOfTheFirstTwoGenerationsLearnsIt()
    {
        // Whirlpool was an HM in Gold, Silver and Crystal and in HeartGold: every Water type of Kanto and Johto but Magikarp, which learns by no machine, took it
        var water = PokemonDatabase.GetAll().Where(s => s.DexNumber <= 251 && s.Name != "Magikarp" && (s.PrimaryType == PokemonType.Water || s.SecondaryType == PokemonType.Water)).ToList();
        Assert.NotEmpty(water);
        Assert.All(water, s => Assert.Contains("HM10", s.TmMoves ?? new()));
    }

    [Fact]
    public void TheBagTeachesItAndTheMoveCantThenBeForgotten()
    {
        var lapras = new Pokemon(PokemonDatabase.Get("Lapras")!, 30);
        while (lapras.Moves.Count > 3) lapras.Moves.RemoveAt(lapras.Moves.Count - 1);
        Assert.Equal(TeachAnswer.Able, MoveTeaching.Answer(lapras, Hm));
        MoveTeaching.Learn(lapras, Hm.TeachesMove!);
        Assert.Equal(TeachAnswer.Learned, MoveTeaching.Answer(lapras, Hm));
        Assert.NotNull(MoveTeaching.WhyNotForget(lapras, lapras.Moves.FindIndex(m => m.Name == "Whirlpool")));
        Assert.Equal(TeachAnswer.NotAble, MoveTeaching.Answer(new Pokemon(PokemonDatabase.Get("Charmander")!, 30), Hm));
    }

    [Fact]
    public void AGameThatClearedTheHideoutBeforeTheHmFindsItInMahoganyOnce()
    {
        var story = new StoryState();
        var bag = new Inventory();
        story.Set("FLAG_ROCKET_HIDEOUT_CLEARED");
        var onEnter = ScriptLibrary.Default.In("mahogany", ScriptLibrary.OnEnter)!;
        for (int i = 0; i < 2; i++)
        {
            var host = new HeadlessScriptHost(story, new Party(), bag);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(onEnter, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            Assert.Empty(host.Transcript);
        }
        Assert.Equal(1, bag.GetQuantity(Hm));
        Assert.True(story.Has("FLAG_JOHTO_GOT_HM_WHIRLPOOL"));

        // Before the hideout is cleared nothing is given
        var early = new Inventory();
        var before = new HeadlessScriptHost(new StoryState(), new Party(), early);
        var run = new ScriptRunner(ScriptLibrary.Default, before);
        run.Start(onEnter, null);
        run.RunToEnd();
        Assert.Equal(0, early.GetQuantity(Hm));
    }
}
