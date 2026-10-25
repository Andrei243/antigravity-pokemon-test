using BlenderAnim;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumTests;

/// <summary>
/// tools/BlenderAnim names the clips it keys so the game plays each for its role: held here against the game's own
/// matcher, with no Blender.
/// </summary>
public class BlenderAnimTests
{
    [Fact]
    public void EveryClipTheToolKeysPlaysForItsRole()
    {
        var names = BattleClips.All.Select(c => c.Clip).ToList();
        var plays = BattleClips.AsTheGameReads(names).ToDictionary(x => x.Role, x => x.Clip);
        foreach (var (role, clip) in BattleClips.All)
            Assert.Equal(clip, plays[BattleClips.RoleOf(role)]);
        // and every role the game has a clip for is one the tool keys
        Assert.Equal(Enum.GetValues<ClipRole>().Length, BattleClips.All.Length);
    }

    [Fact]
    public void AClipOfTheFileThatWouldPlayInsteadIsDroppedAndTheRestKept()
    {
        var (clips, error) = BattleClips.Chosen(Array.Empty<string>());
        Assert.Null(error);
        // Tripo's lone walk is named for the idle, and a strike of the file's would win over the tool's lunge
        var drop = BattleClips.ToDrop(new[] { "idle_walk", "run", "Attack01_Strike" }, clips);
        Assert.Contains("idle_walk", drop);
        Assert.DoesNotContain("run", drop);
        var left = new[] { "idle_walk", "run", "Attack01_Strike" }.Except(drop).Concat(clips.Values).ToList();
        var plays = BattleClips.AsTheGameReads(left).ToDictionary(x => x.Role, x => x.Clip);
        foreach (var (role, clip) in clips) Assert.Equal(clip, plays[BattleClips.RoleOf(role)]);
    }

    [Fact]
    public void OnlyTheRolesAskedForAreKeyedAndTheFilesOwnStay()
    {
        var (clips, _) = BattleClips.Chosen(new[] { "hit", "faint" });
        Assert.Equal(new[] { "hit", "faint" }, clips.Keys);
        Assert.Empty(BattleClips.ToDrop(new[] { "idle_walk" }, clips));
        Assert.NotNull(BattleClips.Chosen(new[] { "jump" }).Error);
    }
}
