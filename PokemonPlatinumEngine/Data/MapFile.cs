using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using TownArchitecture = PokemonPlatinumEngine.Overworld.Architecture;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One map as stored in <c>Data/maps/&lt;Name&gt;.json</c>. The tile layers are rows of characters, one per tile
/// (see <see cref="TileCodes"/>); everything placed on the map is a list of plain records.
/// </summary>
public sealed class MapFile
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BgmTrack { get; set; } = "";
    public InteriorStyle Interior { get; set; } = InteriorStyle.None;
    public TreeStyle Trees { get; set; } = TreeStyle.Round;

    /// <summary>How the town's houses are built; left out for the default, <see cref="TownArchitecture.Timber"/>.</summary>
    public TownArchitecture? Architecture { get; set; }

    /// <summary>The stage its battles are fought on; left out to let the map decide (see <see cref="Map.ArenaAt"/>).</summary>
    public BattleArena? BattleArena { get; set; }

    /// <summary>For a gym or a room of the League: the type its stage is themed on.</summary>
    public PokemonType? ArenaType { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Ground tiles, one string per row, one <see cref="TileCodes"/> character per tile.</summary>
    public List<string> Ground { get; set; } = new();

    /// <summary>Blocked tiles: '#' is solid, '.' is open. Furniture props are already marked here.</summary>
    public List<string> Solid { get; set; } = new();

    /// <summary>Optional overhead tiles; '-' means none.</summary>
    public List<string>? Overhead { get; set; }

    public List<PropRecord> Props { get; set; } = new();

    /// <summary>
    /// Buildings whose kind can't be told from where their door leads: each names one tile of the building.
    /// Left out when there are none.
    /// </summary>
    public List<BuildingRecord>? Buildings { get; set; }

    public List<Warp> Warps { get; set; } = new();
    public List<SignRecord> Signboards { get; set; } = new();
    public List<NpcRecord> Npcs { get; set; } = new();
    public List<WildEncounterEntry> WildEncounters { get; set; } = new();

    public sealed class PropRecord
    {
        public PropType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 1;
        public int Depth { get; set; } = 1;
    }

    public sealed class BuildingRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
        public BuildingKind Kind { get; set; }
    }

    public sealed class SignRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public sealed class NpcRecord
    {
        /// <summary>Only for NPCs other data refers to; the rest get a fresh id when the map loads.</summary>
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NpcType { get; set; } = "Trainer";
        public int X { get; set; }
        public int Y { get; set; }
        public Direction Facing { get; set; } = Direction.Down;
        public List<string>? Dialog { get; set; }
        public bool? IsStarterBriefcase { get; set; }
        public bool? IsHealingNurse { get; set; }
        public bool? IsPokeMartClerk { get; set; }
        public bool? IsPCTerminal { get; set; }
        public bool? IsTransportAttendant { get; set; }
        public TrainerRecord? Trainer { get; set; }
    }

    public sealed class TrainerRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TrainerClass { get; set; } = "Trainer";
        public List<PartyMember> Party { get; set; } = new();
        public int PrizeMoney { get; set; } = 300;
        public string DialogueBefore { get; set; } = string.Empty;
        public string DialogueAfter { get; set; } = string.Empty;
        public int SightRange { get; set; } = 3;
    }

    public sealed class PartyMember
    {
        public string Species { get; set; } = string.Empty;
        public int Level { get; set; }
    }

    /// <summary>Builds a fresh, playable map: new NPC state and newly rolled trainer Pokémon each time.</summary>
    public Map ToMap()
    {
        var map = new Map(Width, Height)
        {
            Name = Name,
            DisplayName = DisplayName,
            BgmTrack = BgmTrack,
            Interior = Interior,
            Trees = Trees,
            Architecture = Architecture ?? TownArchitecture.Timber,
            Arena = BattleArena,
            ArenaType = ArenaType
        };

        CheckRows(Ground, "ground");
        CheckRows(Solid, "solid");
        if (Overhead != null) CheckRows(Overhead, "overhead");

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char solid = Solid[y][x];
                if (solid != '#' && solid != '.')
                    throw new InvalidDataException($"Map {Name}: solid row {y} has '{solid}' at column {x}; use '#' or '.'.");
                map.SetGroundTile(x, y, TileCodes.Parse(Ground[y][x], Name), isSolid: solid == '#');

                if (Overhead != null && Overhead[y][x] != TileCodes.None)
                    map.SetOverheadTile(x, y, TileCodes.Parse(Overhead[y][x], Name));
            }
        }

        // The solid grid already includes furniture, so props are listed without marking tiles again
        foreach (var p in Props)
            map.Props.Add(new Prop { Type = p.Type, X = p.X, Y = p.Y, Width = p.Width, Depth = p.Depth });

        foreach (var b in Buildings ?? new())
        {
            if (!map.InBounds(b.X, b.Y))
                throw new InvalidDataException($"Map {Name}: the {b.Kind} building at {b.X},{b.Y} is outside the map.");
            map.BuildingKinds[(b.X, b.Y)] = b.Kind;
        }

        foreach (var w in Warps)
            map.Warps.Add(new Warp { SourceX = w.SourceX, SourceY = w.SourceY, TargetMap = w.TargetMap, TargetX = w.TargetX, TargetY = w.TargetY, TargetFacing = w.TargetFacing });

        foreach (var s in Signboards)
            map.Signboards[(s.X, s.Y)] = s.Text;

        foreach (var n in Npcs)
            map.NPCs.Add(BuildNpc(n));

        foreach (var e in WildEncounters)
            map.WildEncounters.Add(new WildEncounterEntry { SpeciesName = e.SpeciesName, MinLevel = e.MinLevel, MaxLevel = e.MaxLevel, Weight = e.Weight });

        return map;
    }

    private NPC BuildNpc(NpcRecord n)
    {
        var npc = new NPC
        {
            Name = n.Name,
            NpcType = n.NpcType,
            GridX = n.X,
            GridY = n.Y,
            Facing = n.Facing,
            DialogLines = n.Dialog != null ? new List<string>(n.Dialog) : new(),
            IsStarterBriefcase = n.IsStarterBriefcase ?? false,
            IsHealingNurse = n.IsHealingNurse ?? false,
            IsPokeMartClerk = n.IsPokeMartClerk ?? false,
            IsPCTerminal = n.IsPCTerminal ?? false,
            IsTransportAttendant = n.IsTransportAttendant ?? false
        };
        if (n.Id != null) npc.Id = n.Id;

        if (n.Trainer is { } t)
        {
            var party = new Party();
            foreach (var member in t.Party)
            {
                var species = PokemonDatabase.Get(member.Species)
                    ?? throw new InvalidDataException($"Map {Name}: trainer {t.Id} has unknown species '{member.Species}'.");
                party.Add(new Pokemon(species, member.Level));
            }

            npc.IsTrainer = true;
            npc.TrainerData = new Trainer
            {
                Id = t.Id,
                Name = t.Name,
                TrainerClass = t.TrainerClass,
                Party = party,
                PrizeMoney = t.PrizeMoney,
                DialogueBefore = t.DialogueBefore,
                DialogueAfter = t.DialogueAfter,
                SightRange = t.SightRange
            };
        }

        return npc;
    }

    private void CheckRows(List<string> rows, string layer)
    {
        if (rows.Count != Height)
            throw new InvalidDataException($"Map {Name}: {layer} has {rows.Count} rows, expected {Height}.");
        for (int y = 0; y < rows.Count; y++)
        {
            if (rows[y].Length != Width)
                throw new InvalidDataException($"Map {Name}: {layer} row {y} has {rows[y].Length} tiles, expected {Width}.");
        }
    }

    /// <summary>Captures a map as it stands, for writing back to a file.</summary>
    public static MapFile FromMap(Map map)
    {
        var file = new MapFile
        {
            Name = map.Name,
            DisplayName = map.DisplayName,
            BgmTrack = map.BgmTrack,
            Interior = map.Interior,
            Trees = map.Trees,
            Architecture = map.Architecture == TownArchitecture.Timber ? null : map.Architecture,
            BattleArena = map.Arena,
            ArenaType = map.ArenaType,
            Width = map.Width,
            Height = map.Height
        };

        bool anyOverhead = false;
        var overhead = new List<string>();
        for (int y = 0; y < map.Height; y++)
        {
            var ground = new StringBuilder(map.Width);
            var solid = new StringBuilder(map.Width);
            var over = new StringBuilder(map.Width);
            for (int x = 0; x < map.Width; x++)
            {
                ground.Append(TileCodes.CodeOf(map.GetGroundTile(x, y)));
                solid.Append(map.IsSolid(x, y) ? '#' : '.');
                var top = map.GetOverheadTile(x, y);
                anyOverhead |= top.HasValue;
                over.Append(top.HasValue ? TileCodes.CodeOf(top.Value) : TileCodes.None);
            }
            file.Ground.Add(ground.ToString());
            file.Solid.Add(solid.ToString());
            overhead.Add(over.ToString());
        }
        if (anyOverhead) file.Overhead = overhead;

        file.Props = map.Props.Select(p => new PropRecord { Type = p.Type, X = p.X, Y = p.Y, Width = p.Width, Depth = p.Depth }).ToList();
        if (map.BuildingKinds.Count > 0)
            file.Buildings = map.BuildingKinds.Select(kv => new BuildingRecord { X = kv.Key.X, Y = kv.Key.Y, Kind = kv.Value }).ToList();
        file.Warps = map.Warps.ToList();
        file.Signboards = map.Signboards.Select(kv => new SignRecord { X = kv.Key.X, Y = kv.Key.Y, Text = kv.Value }).ToList();
        file.Npcs = map.NPCs.Select(ToRecord).ToList();
        file.WildEncounters = map.WildEncounters.ToList();
        return file;
    }

    private static NpcRecord ToRecord(NPC npc) => new()
    {
        Id = Guid.TryParse(npc.Id, out _) ? null : npc.Id,
        Name = npc.Name,
        NpcType = npc.NpcType,
        X = npc.GridX,
        Y = npc.GridY,
        Facing = npc.Facing,
        Dialog = npc.DialogLines.Count > 0 ? npc.DialogLines.ToList() : null,
        IsStarterBriefcase = npc.IsStarterBriefcase ? true : null,
        IsHealingNurse = npc.IsHealingNurse ? true : null,
        IsPokeMartClerk = npc.IsPokeMartClerk ? true : null,
        IsPCTerminal = npc.IsPCTerminal ? true : null,
        IsTransportAttendant = npc.IsTransportAttendant ? true : null,
        Trainer = npc.IsTrainer && npc.TrainerData is { } t ? new TrainerRecord
        {
            Id = t.Id,
            Name = t.Name,
            TrainerClass = t.TrainerClass,
            Party = t.Party.Members.Select(p => new PartyMember { Species = p.Species.Name, Level = p.Level }).ToList(),
            PrizeMoney = t.PrizeMoney,
            DialogueBefore = t.DialogueBefore,
            DialogueAfter = t.DialogueAfter,
            SightRange = t.SightRange
        } : null
    };
}

/// <summary>The one-character codes for tiles in map files.</summary>
public static class TileCodes
{
    public const char None = '-';

    private static readonly (TileType Type, char Code)[] Table =
    {
        (TileType.Grass, '.'),
        (TileType.FlowerGrass, '*'),
        (TileType.TallGrass, 'w'),
        (TileType.Path, ':'),
        (TileType.Water, '~'),
        (TileType.LedgeDown, 'v'),
        (TileType.Tree, 'T'),
        (TileType.TreeTrunk, 't'),
        (TileType.RoofRed, 'r'),
        (TileType.RoofBlue, 'b'),
        (TileType.RoofGreen, 'g'),
        (TileType.Wall, '#'),
        (TileType.Door, 'D'),
        (TileType.Floor, '_'),
        (TileType.Signpost, 'S'),
        (TileType.PC, 'P'),
        (TileType.Sand, ','),
        (TileType.Dirt, ';'),
        (TileType.Snow, '^'),
        (TileType.CaveFloor, 'c')
    };

    public static char CodeOf(TileType type)
    {
        foreach (var (t, c) in Table)
            if (t == type) return c;
        throw new ArgumentOutOfRangeException(nameof(type), type, "Tile type has no map file code.");
    }

    public static TileType Parse(char code, string mapName)
    {
        foreach (var (t, c) in Table)
            if (c == code) return t;
        throw new InvalidDataException($"Map {mapName}: unknown tile code '{code}'.");
    }

    public static IEnumerable<TileType> AllTypes => Table.Select(e => e.Type);
}
