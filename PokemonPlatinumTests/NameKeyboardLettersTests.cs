using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 17 · N5, the Romanian letters on the name keyboard: a language's own letters (<c>keyboard</c> in
/// <c>languages.json</c>) are a fifth row of keys, English keeps its board of four rows, and a name with such letters
/// is drawn and saved like any other. The language is the whole process's, so the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class NameKeyboardLettersTests
{
    private static T In<T>(string tag, Func<T> read)
    {
        Text.Use(tag);
        try { return read(); }
        finally { Text.Use(Text.English); }
    }

    [Fact]
    public void OnlyALanguageThatNeedsLettersGetsAFifthRow()
    {
        var english = new NameEntry();
        Assert.Equal((4, 4, ""), (english.RowCount, english.WideRow, english.Extras));
        Assert.Equal("ĂÂÎȘȚ", Language.Of("ro")!.Keyboard);
        Assert.Equal("", Language.Of("en")!.Keyboard);

        var romanian = In("ro", () => new NameEntry());
        Assert.Equal((5, 5, "ĂÂÎȘȚ"), (romanian.RowCount, romanian.WideRow, romanian.Extras));
        Assert.Equal("ĂÂÎȘȚ", romanian.RowKeys(4));
        // The board is fixed when it opens: English again does not take the row away
        Assert.Equal(5, romanian.RowCount);
    }

    [Fact]
    public void TheCursorReachesEveryLetterOfTheRowAndKeepsToIt()
    {
        var entry = new NameEntry(extras: "ĂÂÎȘȚ");
        entry.Move(0, 1); entry.Move(0, 1); entry.Move(0, 1); entry.Move(0, 1);
        Assert.Equal((4, 0), (entry.Row, entry.Column));
        Assert.Equal(NameKey.Letter, entry.KeyUnderCursor);

        // Wraps within its five keys
        entry.Move(-1, 0);
        Assert.Equal(4, entry.Column);
        entry.Move(1, 0);
        Assert.Equal(0, entry.Column);

        // Down from the row are the wide keys; up from the digits' row at column 9 lands on the last of the five
        entry.Move(0, 1);
        Assert.Equal(NameKey.Case, entry.KeyUnderCursor);
        entry.Move(0, -1); entry.Move(0, -1);
        entry.Move(-1, 0); entry.Move(-1, 0); entry.Move(-1, 0); entry.Move(-1, 0); entry.Move(-1, 0);
        Assert.Equal((3, 5), (entry.Row, entry.Column));
        entry.Move(1, 0); entry.Move(1, 0); entry.Move(1, 0); entry.Move(1, 0);
        Assert.Equal(9, entry.Column);
        entry.Move(0, 1);
        Assert.Equal((4, 4), (entry.Row, entry.Column));

        // Start still jumps to OK, which is on the sixth row now
        entry.ToDone();
        Assert.Equal((5, NameKey.Done), (entry.Row, entry.KeyUnderCursor));
    }

    [Fact]
    public void ALetterOfTheRowIsTypedInTheCaseTheBoardIsIn()
    {
        var entry = new NameEntry(extras: "ĂÂÎȘȚ");
        for (int i = 0; i < 4; i++) entry.Move(0, 1);
        entry.Move(1, 0); entry.Move(1, 0); entry.Move(1, 0); // Ș
        Assert.Equal('Ș', entry.RowKeys(4)[entry.Column]);
        entry.Press();
        Assert.Equal(("Ș", false), (entry.Text, entry.UpperCase));
        Assert.Equal("ăâîșț", entry.RowKeys(4));
        entry.Press();
        Assert.Equal("Șș", entry.Text);
        // Names keep their length
        foreach (char c in "Ștefănița") entry.Type(c);
        Assert.Equal(PlayerIdentity.MaxNameLength, entry.Text.Length);
        Assert.Equal("ȘșȘtefă", entry.Text);
    }

    [Fact]
    public void EveryKeyOfTheRowCanBeDrawnInEveryWeight()
    {
        foreach (var weight in new[] { UiWeight.Bold, UiWeight.ExtraBold, UiWeight.Black })
            foreach (char c in "ĂÂÎȘȚăâîșț")
                Assert.True(TextMetrics.CanDraw(new System.Text.Rune(c), weight), $"{c} in {weight}");
    }

    [Fact]
    public void ANameWithTheLettersSurvivesASave()
    {
        const string name = "Țurcanu Ștefăniță";
        string json = SaveManager.Write(new SaveData { PlayerName = name, RivalName = "Ștefan", CurrentMapName = "Sinnoh" });
        var back = SaveManager.Read(json)!;
        Assert.Equal((name, "Ștefan"), (back.PlayerName, back.RivalName));

        // And through a file, as a slot is written
        string path = Path.Combine(Path.GetTempPath(), "nk-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            Assert.True(SaveManager.SaveGame(new SaveData { PlayerName = name, CurrentMapName = "Sinnoh" }, path));
            Assert.Equal(name, SaveManager.Load(path).Save!.PlayerName);
        }
        finally { File.Delete(path); }

        // Cleaning a name keeps them
        Assert.Equal("Țurcan", PlayerIdentity.Clean("Țurcanu Ștefăniță")[..6]);
    }
}
