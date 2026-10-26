using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Plan 12 · Q16, the screenshot key: the pictures' names, the action read, the shutter.</summary>
public class ScreenshotKeyTests
{
    [Fact]
    public void ThePicturesNamesSortAsTheyWereTaken()
    {
        var start = new DateTime(2026, 10, 26, 9, 59, 59, 998);
        var taken = new HashSet<string>();
        var order = new List<string>();
        // Five in one millisecond, then the next moments, across a second, a minute and an hour
        for (int k = 0; k < 5; k++) Take(start);
        for (int ms = 1; ms < 5; ms++) Take(start.AddMilliseconds(ms));
        Assert.Equal(order.Count, taken.Count);
        Assert.Equal(order, order.OrderBy(n => n, StringComparer.Ordinal).ToList());
        Assert.All(order, n => Assert.Matches(@"^shot-\d{8}-\d{6}-\d{3}-\d{2}\.png$", Path.GetFileName(n)));
        Assert.Equal("shot-20261026-095959-998-00.png", Path.GetFileName(order[0]));
        Assert.Equal("shot-20261026-095959-998-04.png", Path.GetFileName(order[4]));
        Assert.Equal("shot-20261026-100000-000-00.png", Path.GetFileName(order[6]));

        void Take(DateTime when)
        {
            string path = Screenshots.PathFor("pictures", when, taken.Contains);
            taken.Add(path);
            order.Add(path);
        }
    }

    [Fact]
    public void TheScreenshotKeyIsReadAndHasItsShutter()
    {
        Assert.DoesNotContain(GameAction.Screenshot, InputManager.NotReadYet);
        Assert.Contains(Raylib_cs.KeyboardKey.F12, InputBindings.For(ButtonMode.Normal)[GameAction.Screenshot].Keys);
        Assert.True(SoundBank.Exists("shutter"));
        Assert.Equal("screenshots", SavePaths.ScreenshotsName);
    }
}
