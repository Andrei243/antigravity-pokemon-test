using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>The boxes' tools (plan 12 · Q26): searching, sorting and moving many in the PC, behind one options row.</summary>
public partial class GameEngine
{
    /// <summary>Whether the options' BOX TOOLS row is on under the rules in force; off, the PC is plan 06 · R12's.</summary>
    internal bool BoxToolsOn => FieldHelp.On(Settings.BoxTools, Ruleset.Current.Preset);
}
