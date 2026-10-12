using System;
using System.IO;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// The golden file's check itself (plan 24 · X2), on a folder of its own: a file that isn't there yet, the same
/// text, a block that moved, and accepting. The switch is handed in, never read here, so a run under
/// <c>GOLDEN=accept</c> checks the same.
/// </summary>
public class GoldenFileTests
{
    private const string Before =
        "# a head\n" +
        "=== battle 1\nsay A wild Bidoof appeared!\nresult: PlayerVictory after 2 turns\n" +
        "=== battle 2\nsay A wild Shinx appeared!\nresult: PlayerDefeat after 5 turns\n";

    [Fact]
    public void ADifferenceIsWrittenBesideTheFileAndNamesItsBlocks()
    {
        string folder = Path.Combine(Path.GetTempPath(), "golden-" + Guid.NewGuid().ToString("N"));
        try
        {
            string file = Path.Combine(folder, "battles.txt"), received = Path.Combine(folder, "battles.received.txt");
            Assert.Equal(received, GoldenFile.ReceivedPath(file));

            // A file that isn't there yet fails, with what the code gives beside it, until it is accepted
            string? why = GoldenFile.Compare(folder, "battles.txt", Before, accept: false);
            Assert.NotNull(why);
            Assert.Contains("doesn't exist yet", why);
            Assert.Contains(GoldenFile.AcceptCommand, why);
            Assert.Equal(Before, File.ReadAllText(received));
            Assert.False(File.Exists(file));

            Assert.Null(GoldenFile.Compare(folder, "battles.txt", Before, accept: true));
            Assert.Equal(Before, File.ReadAllText(file));
            Assert.False(File.Exists(received));

            // The same text passes, whatever its line endings
            Assert.Null(GoldenFile.Compare(folder, "battles.txt", Before.Replace("\n", "\r\n"), accept: false));
            Assert.False(File.Exists(received));

            // A block that moved is named, and the other isn't; the file stays as it was
            string after = Before.Replace("after 5 turns", "after 6 turns");
            why = GoldenFile.Compare(folder, "battles.txt", after, accept: false);
            Assert.NotNull(why);
            Assert.StartsWith("1 of 3 blocks of battles.txt moved: battle 2.", why);
            Assert.DoesNotContain("battle 1", why);
            Assert.Contains("result: PlayerDefeat after 6 turns", why);
            Assert.Equal(after, File.ReadAllText(received));
            Assert.Equal(Before, File.ReadAllText(file));

            // So is one that came and one that went
            string another = Before.Replace("=== battle 1", "=== battle 3");
            why = GoldenFile.Compare(folder, "battles.txt", another, accept: false);
            Assert.StartsWith("2 of 3 blocks of battles.txt moved: battle 1, battle 3.", why);

            // Accepting rewrites the file and takes the received one away
            Assert.Null(GoldenFile.Compare(folder, "battles.txt", after, accept: true));
            Assert.Equal(after, File.ReadAllText(file));
            Assert.False(File.Exists(received));
            Assert.Null(GoldenFile.Compare(folder, "battles.txt", after, accept: false));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void AFileIsCutIntoItsBlocksByName()
    {
        var blocks = GoldenFile.Blocks(Before + "=== battle 2\nagain\n");
        Assert.Equal(new[] { "(the head)", "battle 1", "battle 2", "battle 2 (2)" }, blocks.ConvertAll(b => b.Name));
        Assert.Equal("=== battle 1\nsay A wild Bidoof appeared!\nresult: PlayerVictory after 2 turns", blocks[1].Text);
    }
}
