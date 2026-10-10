using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// CLAUDE.md's rules of where things may be used, held to the engine's IL (plan 24 · X1): every call, construction
/// and field read of every method, the caller taken by its outermost type (a lambda's or an iterator's body lives in
/// a type the compiler nests in it). The bans of single calls (an unseeded <c>Random</c>, <c>DateTime.Now</c>…) are
/// build errors instead (<c>PokemonPlatinumEngine/BannedSymbols.txt</c>).
/// </summary>
public class ArchitectureTests
{
    private const string Engine = "PokemonPlatinumEngine";

    /// <summary>A use of a member in a method: who uses (outermost type, its namespace, the method) what.</summary>
    public sealed record Use(string Namespace, string Caller, string Method, string TargetType, string Member, bool Constructs)
    {
        /// <summary>The target's namespace (the part before its last dot).</summary>
        public string TargetNamespace => TargetType.Contains('.') ? TargetType[..TargetType.LastIndexOf('.')] : "";

        /// <summary>The target's own name, without its namespace or the types it is nested in.</summary>
        public string TargetName => TargetType[(TargetType.LastIndexOf('.') + 1)..].Split('/')[0];

        public override string ToString() => $"{Namespace}.{Caller}.{Method} uses {TargetType}::{Member}";
    }

    /// <summary>A rule: what it says, and the uses that break it, given the namespace the rules are relative to.</summary>
    public sealed record Rule(string Says, Func<string, Use, bool> Breaks);

    private static readonly string[] Presentation = { "InputManager", "AudioManager", "BattleAnimator", "BattleEngine", "BattleHUD", "BattleRenderer" };
    private static readonly string[] Sculptors = { "PokemonModels", "PokeBuilder", "PokemonGenerator", "PokemonGenomes" };

    private static bool In(string root, Use use, string space) => use.Namespace == $"{root}.{space}" || use.Namespace.StartsWith($"{root}.{space}.");

    // A colour is four bytes and no part of the GPU: the table of looks (Data.CharacterStyles) reads and writes them
    private static bool Raylib(Use use) => use.TargetNamespace == "Raylib_cs" && use.TargetName != "Color";

    /// <summary>
    /// Where Graphics reads the walked height, not to place anything but as a rule's input, each with its reason
    /// (plan 24: a rule that needs more than three of these is reconsidered rather than widened).
    /// </summary>
    public static readonly (string Caller, string Method, string Why)[] WalkedHeights =
    {
        ("GymPieces", "BuildPastoria", "the Pastoria Gym's rafts and water levels are the puzzle's walked heights"),
        ("WorldRenderer", "GatherActors", "asks the Canalave Gym's puzzle whether someone is on a floor not yet in view")
    };

    public static readonly Rule[] Rules =
    {
        new("Logic stays free of the GPU: Battle.Sim, Battle.Effects, Story, Models and Data use no raylib type but a Color",
            (root, u) => (In(root, u, "Battle.Sim") || In(root, u, "Battle.Effects") || In(root, u, "Story") || In(root, u, "Models") || In(root, u, "Data")) && Raylib(u)),
        new("Logic stays free of the screen: Battle.Sim, Battle.Effects, Story, Models and Data use none of the input, sound or battle's face",
            (root, u) => (In(root, u, "Battle.Sim") || In(root, u, "Battle.Effects") || In(root, u, "Story") || In(root, u, "Models") || In(root, u, "Data"))
                && u.TargetType.StartsWith(Engine + ".") && Presentation.Contains(u.TargetName)),
        new("The field's rules use no raylib type, but DialogueManager",
            (root, u) => In(root, u, "Overworld") && u.Caller != "DialogueManager" && Raylib(u)),
        new("Nothing in a battle draws from anything but the battle's generator: Battle.Sim and Battle.Effects make no Random but a BattleRandom",
            (root, u) => (In(root, u, "Battle.Sim") || In(root, u, "Battle.Effects")) && u.Constructs && u.TargetType == "System.Random"),
        new("A battle reads Dice only as it is made with no generator (BattleCore's constructor)",
            (root, u) => (In(root, u, "Battle.Sim") || In(root, u, "Battle.Effects")) && u.TargetType == $"{Engine}.Core.Dice"
                && !(u.Caller == "BattleCore" && u.Method == ".ctor")),
        new("A sculptor draws from its GenomeRandom, never System.Random",
            (root, u) => In(root, u, "Graphics") && Sculptors.Contains(u.Caller) && u.TargetType == "System.Random"),
        new("Things are placed with Relief.At: Graphics asks Map.HeightAt only in Relief",
            (root, u) => In(root, u, "Graphics") && u.Caller != "Relief" && u.TargetType == $"{Engine}.Overworld.Map" && u.Member == "HeightAt"
                && !WalkedHeights.Any(w => w.Caller == u.Caller && w.Method == u.Method)),
        new("Rules read Battler.Ability, the ability in force: Battle reads Pokemon.Ability only in Battler.Ability",
            (root, u) => In(root, u, "Battle") && u.TargetType == $"{Engine}.Models.Pokemon" && u.Member == "get_Ability"
                && !(u.Caller == "Battler" && u.Method == "get_Ability"))
    };

    [Fact]
    public void TheEngineKeepsEveryRule()
    {
        var uses = Uses(typeof(PokemonPlatinumEngine.Core.GameEngine).Assembly.Location);
        Assert.True(uses.Count > 100_000, $"only {uses.Count} uses read from the engine");
        var broken = Rules.SelectMany(r => uses.Where(u => r.Breaks(Engine, u)).Select(u => $"{r.Says}: {u}")).Distinct().ToList();
        Assert.True(broken.Count == 0, string.Join("\n", broken.Take(40)));
    }

    [Fact]
    public void EachRuleFindsItsOwnBreak()
    {
        // The probes below break each rule once, under a root of their own; a rule that finds nothing there would
        // find nothing in the engine either
        const string root = "PokemonPlatinumTests.Probes";
        var uses = Uses(typeof(ArchitectureTests).Assembly.Location).Where(u => u.Namespace.StartsWith(root + ".")).ToList();
        foreach (var rule in Rules)
            Assert.True(uses.Any(u => rule.Breaks(root, u)), $"no probe breaks \"{rule.Says}\"");
        // And what is allowed is allowed
        Assert.DoesNotContain(uses, u => u.Caller is "Relief" or "BattleCore" or "Battler" or "DialogueManager" && Rules.Any(r => r.Breaks(root, u)));
    }

    [Fact]
    public void EveryEngineFileIsInItsFoldersNamespace()
    {
        string engine = Path.Combine(Repo(), Engine);
        var wrong = new List<string>();
        foreach (string file in Directory.EnumerateFiles(engine, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(engine, Path.GetDirectoryName(file)!);
            if (relative.Split(Path.DirectorySeparatorChar)[0] is "obj" or "bin") continue;
            string expected = relative == "." ? Engine : Engine + "." + relative.Replace(Path.DirectorySeparatorChar, '.');
            string? declared = File.ReadLines(file).Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("namespace "))?["namespace ".Length..].TrimEnd(';', ' ', '{');
            if (declared != expected) wrong.Add($"{Path.GetRelativePath(engine, file)}: {declared ?? "no namespace"}, not {expected}");
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    private static string Repo()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("The tests don't run from inside the repository");
    }

    // ------------------------------------------------------------------ reading IL

    private static readonly Dictionary<short, OperandType> Operands = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => o.Value, o => o.OperandType);

    private static readonly HashSet<short> Reading = new[]
    {
        OpCodes.Call, OpCodes.Callvirt, OpCodes.Newobj, OpCodes.Ldfld, OpCodes.Ldsfld, OpCodes.Ldflda, OpCodes.Ldsflda,
        OpCodes.Stfld, OpCodes.Stsfld, OpCodes.Ldftn, OpCodes.Ldvirtftn, OpCodes.Initobj, OpCodes.Newarr, OpCodes.Box,
        OpCodes.Ldtoken, OpCodes.Castclass, OpCodes.Isinst, OpCodes.Ldobj, OpCodes.Stobj
    }.Select(o => o.Value).ToHashSet();

    /// <summary>Every use of a member or a type in the IL of the assembly at <paramref name="path"/>.</summary>
    public static List<Use> Uses(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();
        var names = new TypeNames(md);
        var uses = new List<Use>();
        foreach (var typeHandle in md.TypeDefinitions)
        {
            var type = md.GetTypeDefinition(typeHandle);
            var outer = typeHandle;
            while (md.GetTypeDefinition(outer).GetDeclaringType() is { IsNil: false } parent) outer = parent;
            var outerDef = md.GetTypeDefinition(outer);
            string space = md.GetString(outerDef.Namespace), caller = md.GetString(outerDef.Name);
            foreach (var methodHandle in type.GetMethods())
            {
                var method = md.GetMethodDefinition(methodHandle);
                if (method.RelativeVirtualAddress == 0) continue;
                string methodName = OwnMethod(md, typeHandle, outer, method);
                var il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILReader();
                while (il.RemainingBytes > 0)
                {
                    short code = il.ReadByte();
                    if (code == 0xFE) code = (short)(0xFE00 | il.ReadByte());
                    var operand = Operands[code];
                    if (operand is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok)
                    {
                        int token = il.ReadInt32();
                        if (!Reading.Contains(code)) continue;
                        var (target, member) = names.Of(MetadataTokens.EntityHandle(token));
                        if (target != null) uses.Add(new Use(space, caller, methodName, target, member, code == OpCodes.Newobj.Value));
                    }
                    else Skip(ref il, operand);
                }
            }
        }
        return uses;
    }

    /// <summary>
    /// The method a use is counted to: the method itself, or for a compiler's nested type (a lambda's, an
    /// iterator's) the method of the outermost type whose name it carries (<c>&lt;Course&gt;d__12</c> is Course's).
    /// </summary>
    private static string OwnMethod(MetadataReader md, TypeDefinitionHandle type, TypeDefinitionHandle outer, MethodDefinition method)
    {
        string name = md.GetString(method.Name);
        if (type == outer) return name.StartsWith('<') ? Between(name) : name;
        string typeName = md.GetString(md.GetTypeDefinition(type).Name);
        return typeName.StartsWith('<') && !typeName.StartsWith("<>c") ? Between(typeName) : name.StartsWith('<') ? Between(name) : name;
    }

    private static string Between(string name) => name.IndexOf('>') is > 1 and var end ? name[1..end] : name;

    private static void Skip(ref BlobReader il, OperandType operand)
    {
        switch (operand)
        {
            case OperandType.InlineNone: break;
            case OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar: il.Offset += 1; break;
            case OperandType.InlineVar: il.Offset += 2; break;
            case OperandType.InlineI8 or OperandType.InlineR: il.Offset += 8; break;
            case OperandType.InlineSwitch: { int count = il.ReadInt32(); il.Offset += count * 4; break; }
            default: il.Offset += 4; break;
        }
    }

    /// <summary>The full names (nested types after a slash) of the types and members a token names.</summary>
    private sealed class TypeNames : ISignatureTypeProvider<string, object?>
    {
        private readonly MetadataReader md;
        public TypeNames(MetadataReader md) => this.md = md;

        public (string? Type, string Member) Of(EntityHandle handle)
        {
            switch (handle.Kind)
            {
                case HandleKind.MethodDefinition:
                {
                    var m = md.GetMethodDefinition((MethodDefinitionHandle)handle);
                    return (Name(m.GetDeclaringType()), md.GetString(m.Name));
                }
                case HandleKind.FieldDefinition:
                {
                    var f = md.GetFieldDefinition((FieldDefinitionHandle)handle);
                    return (Name(f.GetDeclaringType()), md.GetString(f.Name));
                }
                case HandleKind.MemberReference:
                {
                    var r = md.GetMemberReference((MemberReferenceHandle)handle);
                    string? parent = r.Parent.Kind switch
                    {
                        HandleKind.TypeReference or HandleKind.TypeDefinition or HandleKind.TypeSpecification => Of(r.Parent).Type,
                        HandleKind.MethodDefinition => Of(r.Parent).Type,
                        _ => null
                    };
                    return (parent, md.GetString(r.Name));
                }
                case HandleKind.MethodSpecification:
                    return Of(md.GetMethodSpecification((MethodSpecificationHandle)handle).Method);
                case HandleKind.TypeDefinition:
                    return (Name((TypeDefinitionHandle)handle), "");
                case HandleKind.TypeReference:
                    return (Name((TypeReferenceHandle)handle), "");
                case HandleKind.TypeSpecification:
                    return (md.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(this, null), "");
                default:
                    return (null, "");
            }
        }

        private string Name(TypeDefinitionHandle handle)
        {
            var t = md.GetTypeDefinition(handle);
            string name = md.GetString(t.Name);
            return t.GetDeclaringType() is { IsNil: false } outer ? Name(outer) + "/" + name : Join(md.GetString(t.Namespace), name);
        }

        private string Name(TypeReferenceHandle handle)
        {
            var t = md.GetTypeReference(handle);
            string name = md.GetString(t.Name);
            return t.ResolutionScope.Kind == HandleKind.TypeReference
                ? Name((TypeReferenceHandle)t.ResolutionScope) + "/" + name
                : Join(md.GetString(t.Namespace), name);
        }

        private static string Join(string space, string name) => space.Length == 0 ? name : space + "." + name;

        // A constructed type counts as its definition, an array or a pointer as what it holds
        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => genericType;
        public string GetArrayType(string elementType, ArrayShape shape) => elementType;
        public string GetSZArrayType(string elementType) => elementType;
        public string GetByReferenceType(string elementType) => elementType;
        public string GetPointerType(string elementType) => elementType;
        public string GetPinnedType(string elementType) => elementType;
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => "System." + typeCode;
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => Name(handle);
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => Name(handle);
        public string GetTypeFromSpecification(MetadataReader reader, object? context, TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, context);
        public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
        public string GetGenericMethodParameter(object? context, int index) => "!!" + index;
        public string GetGenericTypeParameter(object? context, int index) => "!" + index;
    }
}
