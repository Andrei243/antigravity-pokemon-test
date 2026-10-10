using System.Globalization;
using System.Text;

namespace MapImporter;

/// <summary>
/// A room of the original as text (<c>--room &lt;key&gt;</c>), for writing its map file by hand: the tiles that
/// block and the behaviours that matter, the props with their models and tiles, and the events where they stand.
/// The rooms the story needs before plan 01 · M11 imports the rest are written from this.
/// </summary>
public static class RoomPlan
{
    public static string Describe(DecompMaps decomp, string key)
    {
        var header = decomp.Headers.Values.FirstOrDefault(h => h.Key == key)
            ?? throw new ArgumentException($"No area called {key}");
        var matrix = decomp.Matrix(header.Matrix);
        var text = new StringBuilder();
        text.AppendLine($"{key}: matrix {matrix.Id} ({matrix.Width}x{matrix.Height}), music {header.DayMusic}, events {header.Events}");
        for (int my = 0; my < matrix.Height; my++)
            for (int mx = 0; mx < matrix.Width; mx++)
            {
                int id = matrix.LandAt(mx, my);
                if (id == Matrix.NoLand || matrix.Headers != null && matrix.HeaderAt(mx, my) != header.Id) continue;
                var land = decomp.Land(id);
                text.AppendLine($"chunk {mx},{my}: land {id} ({land.ModelName}); '#' blocked, '.' open, a letter a behaviour (listed below)");
                var letters = new Dictionary<byte, char>();
                int x0 = 32, y0 = 32, x1 = -1, y1 = -1;
                for (int z = 0; z < LandData.Tiles; z++)
                    for (int x = 0; x < LandData.Tiles; x++)
                        if (land.Behaviour(x, z) != 0 || !land.Solid(x, z))
                            (x0, y0, x1, y1) = (Math.Min(x0, x), Math.Min(y0, z), Math.Max(x1, x), Math.Max(y1, z));
                if (x1 < 0) continue;
                text.AppendLine($"used tiles {x0},{y0} to {x1},{y1}");
                text.Append("    ");
                for (int x = x0; x <= x1; x++) text.Append((char)('0' + x % 10));
                text.AppendLine();
                for (int z = y0; z <= y1; z++)
                {
                    text.Append(z.ToString("00", CultureInfo.InvariantCulture)).Append("  ");
                    for (int x = x0; x <= x1; x++)
                    {
                        byte b = land.Behaviour(x, z);
                        if (b != 0)
                        {
                            if (!letters.TryGetValue(b, out char c)) letters[b] = c = (char)('a' + letters.Count % 26);
                            text.Append(land.Solid(x, z) ? char.ToUpperInvariant(c) : c);
                        }
                        else text.Append(land.Solid(x, z) ? '#' : '.');
                    }
                    text.AppendLine();
                }
                foreach (var (b, c) in letters)
                    text.AppendLine($"  {c} = {(b < decomp.BehaviourNames.Count ? decomp.BehaviourNames[b] : b.ToString(CultureInfo.InvariantCulture))} (0x{b:X2}; upper case where it also blocks)");
                foreach (var prop in land.Props)
                {
                    string name = prop.ModelId < decomp.PropModelFiles.Count ? decomp.PropModelFiles[prop.ModelId] : "#" + prop.ModelId;
                    var info = decomp.PropModel(prop.ModelId);
                    string size = info == null ? "" : $" box {info.BoxSize.X / LandData.TileUnits:F1}x{info.BoxSize.Z / LandData.TileUnits:F1}x{info.BoxSize.Y / LandData.TileUnits:F1}";
                    text.AppendLine($"  prop {name} at {prop.TileX:F1},{prop.TileZ:F1} turned {prop.Rotation.Y:F0}{size}");
                }
            }

        var events = decomp.Events(header.Events);
        foreach (var o in events.Objects)
            text.AppendLine($"object {o.Id}: {o.GraphicsId} at {o.X},{o.Z} facing {o.InitialDir} moving {o.MovementType} script {o.Script} hidden by {o.HiddenFlag}");
        foreach (var w in events.Warps)
            text.AppendLine($"warp at {w.X},{w.Z} to {w.DestHeaderId} {w.DestWarpId}");
        foreach (var s in events.Signs)
            text.AppendLine($"bg event type {s.Type} at {s.X},{s.Z} script {s.Script} facing {s.PlayerFacingDir}");
        foreach (var t in events.Triggers)
            text.AppendLine($"trigger at {t.X},{t.Z} {t.Width}x{t.Length} script {t.Script} when {t.Var}");
        return text.ToString();
    }
}
