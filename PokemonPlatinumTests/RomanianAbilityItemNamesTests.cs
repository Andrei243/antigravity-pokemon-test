using System.Text;
using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 17 · N6: Romanian's names and descriptions of the abilities and the items, made up and written by hand
/// (no official Romanian exists), in <c>Data/text/ro/names.abilities.json</c> and <c>names.items.json</c>: the same shape
/// as the sections of an importer's <c>names.json</c> (English name to the shown one, and a <c>descriptions</c> block).
/// The files are read here directly, with no loader, so nothing of the process's language is touched: every ability
/// and item of the data has a name and a description, nothing else is named, two items never share a name, the
/// comma below and never the cedilla, every character one the typeface draws, and the words fit where the bag, the
/// battle's bag and the battle's information panel draw them, measured as they draw them.
/// </summary>
public class RomanianAbilityItemNamesTests
{
    private static string PathOf(string file) => Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "text", "ro", file);

    /// <summary>One section of a file (<c>items</c>, or <c>descriptions.items</c>).</summary>
    private static Dictionary<string, string> Section(string file, string section)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(PathOf(file)));
        var element = document.RootElement;
        foreach (var part in section.Split('.')) element = element.GetProperty(part);
        return element.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);
    }

    private static Dictionary<string, string> AbilityNames => Section("names.abilities.json", "abilities");
    private static Dictionary<string, string> AbilityTexts => Section("names.abilities.json", "descriptions.abilities");
    private static Dictionary<string, string> ItemNames => Section("names.items.json", "items");
    private static Dictionary<string, string> ItemTexts => Section("names.items.json", "descriptions.items");

    private static List<string> DataAbilities => AbilityDatabase.GetAll().Select(a => a.Name).ToList();
    private static List<ItemData> DataItems => ItemDatabase.GetAll().ToList();

    [Fact]
    public void TheFilesHaveTheShapeOfANamesFile()
    {
        using (var abilities = JsonDocument.Parse(File.ReadAllText(PathOf("names.abilities.json"))))
            Assert.Equal(new[] { "abilities", "descriptions" }, abilities.RootElement.EnumerateObject().Select(p => p.Name));
        using (var items = JsonDocument.Parse(File.ReadAllText(PathOf("names.items.json"))))
            Assert.Equal(new[] { "items", "descriptions" }, items.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.All(new[] { "abilities", "items" }, s => Assert.Contains(s, LocalNames.Sections));
        Assert.All(new[] { "abilities", "items" }, s => Assert.Contains(s, LocalNames.DescriptionSections));
    }

    [Fact]
    public void EveryAbilityHasANameAndADescriptionAndNothingElseIsNamed()
    {
        var names = AbilityNames;
        var texts = AbilityTexts;
        Assert.Empty(DataAbilities.Where(a => !names.ContainsKey(a) || names[a].Trim().Length == 0).ToList());
        Assert.Empty(DataAbilities.Where(a => !texts.ContainsKey(a) || texts[a].Trim().Length == 0).ToList());
        Assert.Empty(names.Keys.Concat(texts.Keys).Except(DataAbilities));
    }

    [Fact]
    public void EveryItemHasANameAndADescriptionAndNothingElseIsNamed()
    {
        var names = ItemNames;
        var texts = ItemTexts;
        var all = DataItems.Select(i => i.Name).ToList();
        Assert.Empty(all.Where(i => !names.ContainsKey(i) || names[i].Trim().Length == 0).ToList());
        Assert.Empty(all.Where(i => !texts.ContainsKey(i) || texts[i].Trim().Length == 0).ToList());
        Assert.Empty(names.Keys.Concat(texts.Keys).Except(all));
    }

    [Fact]
    public void NoTwoItemsAndNoTwoAbilitiesShareAName()
    {
        static IEnumerable<string> Shared(Dictionary<string, string> names) =>
            names.GroupBy(p => p.Value, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => p.Key))}");
        Assert.Empty(Shared(ItemNames));
        Assert.Empty(Shared(AbilityNames));
    }

    [Fact]
    public void EveryCharacterIsOneTheTypefaceDrawsWithTheCommaBelowAndNoCedilla()
    {
        var wrong = new List<string>();
        foreach (var (file, section) in new[] { ("names.abilities.json", "abilities"), ("names.abilities.json", "descriptions.abilities"), ("names.items.json", "items"), ("names.items.json", "descriptions.items") })
            foreach (var (key, text) in Section(file, section))
            {
                if (text.IndexOfAny(new[] { 'ş', 'ţ', 'Ş', 'Ţ', 'ã', 'Ã' }) >= 0) wrong.Add($"{section} {key}: a cedilla or a tilde in \"{text}\"");
                if (text != text.Trim() || text.Contains("  ")) wrong.Add($"{section} {key}: stray spaces in \"{text}\"");
                foreach (var weight in new[] { UiWeight.Bold, UiWeight.Black })
                    if (TextMetrics.Undrawable(text, weight).Count > 0) wrong.Add($"{section} {key}: \"{text}\" has a character the {weight} face lacks");
            }
        Assert.Empty(wrong);
    }

    [Fact]
    public void ADescriptionIsSentencesEachEndingInAFullStop()
    {
        var wrong = new List<string>();
        foreach (var (key, text) in AbilityTexts.Concat(ItemTexts))
        {
            if (!char.IsUpper(text[0])) wrong.Add($"{key}: \"{text}\" begins in lower case");
            if (!text.EndsWith('.')) wrong.Add($"{key}: \"{text}\" has no full stop at its end");
            int sentences = text.Split(". ").Length;
            if (sentences > 3) wrong.Add($"{key}: \"{text}\" is {sentences} sentences");
        }
        Assert.Empty(wrong);
    }

    // ------------------------------------------------------------------ what fits

    // The bag (ModernUi.DrawBag, ItemDetail): a row's name at 34 Black between its icon and its count in a list 920
    // wide; the description under the item at 30 Bold, 42 apart, in the detail panel's width less its margins, from 306
    // below its top to the foot of the panel.
    private const float BagNameRoom = 600, BagNameSize = 34;
    private static readonly float BagTextWidth = 1920 - ModernUi.Margin * 2 - 920 - ModernUi.Gutter - 88;
    private const float BagTextSize = 30;
    private static readonly int BagTextRows = (int)((ModernUi.ContentBottom - (ModernUi.ContentTop + 100) - 306) / 42);

    // The battle's bag (ModernUi.Battle.cs, the bag's pocket): a slot 568 wide, the name at 30 Black from 104 in to its
    // count at the right; the information box 640 wide at 26 Bold, 36 apart, from 104 below its top in a box 374 high.
    private const float BattleSlotNameRoom = 568 - 104 - 30 - 70, BattleNameSize = 30;
    private const float BattleTextWidth = 640 - 80, BattleTextSize = 26;
    private const int BattleTextRows = (374 - 104 - 16) / 36;

    // The battle's information panel (ModernUi.InfoCard): the ability at 28 Black, with the held item 320 to its right.
    private const float InfoAbilityRoom = 320 - 16, InfoSize = 28;

    private static float Black(string text, float size) => TextMetrics.Measure(text, size, UiWeight.Black);
    private static int Rows(string text, float width, float size) => TextMetrics.Wrap(text, width, size, UiWeight.Bold).Count;

    [Fact]
    public void AnItemsNameFitsTheBagsRowAndTheBattleBagsSlot()
    {
        var names = ItemNames;
        var wrong = new List<string>();
        foreach (var item in DataItems)
        {
            string ro = names[item.Name];
            // Where the English is wider than the room already, the Romanian is held to the English
            float bag = Math.Max(BagNameRoom, Black(item.Name, BagNameSize));
            if (Black(ro, BagNameSize) > bag) wrong.Add($"{item.Name}: \"{ro}\" is {Black(ro, BagNameSize):0} wide in the bag's row, which holds {bag:0}");
            if (item.CanUseInBattle || item.BattlePocket != null)
            {
                float slot = Math.Max(BattleSlotNameRoom, Black(item.Name, BattleNameSize));
                if (Black(ro, BattleNameSize) > slot) wrong.Add($"{item.Name}: \"{ro}\" is {Black(ro, BattleNameSize):0} wide in the battle bag's slot, which holds {slot:0}");
            }
        }
        Assert.Empty(wrong);
    }

    [Fact]
    public void AnItemsDescriptionFitsTheBagAndTheBattleBag()
    {
        var texts = ItemTexts;
        var wrong = new List<string>();
        Assert.True(BagTextRows >= 6, $"the bag's description holds {BagTextRows} rows");
        foreach (var item in DataItems)
        {
            string ro = texts[item.Name];
            int bag = Rows(ro, BagTextWidth, BagTextSize);
            if (bag > Math.Min(BagTextRows, 5)) wrong.Add($"{item.Name}: \"{ro}\" is {bag} rows in the bag");
            if (item.CanUseInBattle || item.BattlePocket != null)
            {
                int battle = Rows(ro, BattleTextWidth, BattleTextSize);
                if (battle > BattleTextRows) wrong.Add($"{item.Name}: \"{ro}\" is {battle} rows in the battle bag, which holds {BattleTextRows}");
            }
        }
        Assert.Empty(wrong);
    }

    [Fact]
    public void AnAbilitysNameFitsTheInformationPanelAndItsDescriptionIsShort()
    {
        var names = AbilityNames;
        var texts = AbilityTexts;
        var wrong = new List<string>();
        foreach (var ability in DataAbilities)
        {
            string ro = names[ability];
            float room = Math.Max(InfoAbilityRoom, Black(ability, InfoSize));
            if (Black(ro, InfoSize) > room) wrong.Add($"{ability}: \"{ro}\" is {Black(ro, InfoSize):0} wide on the information panel, which holds {room:0}");
            // No screen shows an ability's description yet: it is held to what the battle's information box would hold
            int rows = Rows(texts[ability], BattleTextWidth, BattleTextSize);
            if (rows > BattleTextRows) wrong.Add($"{ability}: \"{texts[ability]}\" is {rows} rows, more than the battle's box holds ({BattleTextRows})");
        }
        Assert.Empty(wrong);
    }

    [Fact]
    public void TheGlossaryIsKept()
    {
        var names = ItemNames;
        // Berries are a Boabă before the made-up word; TMs and HMs keep their numbers; the Poké Ball is the Poké Ball
        Assert.All(DataItems.Where(i => i.Name.EndsWith(" Berry", StringComparison.Ordinal)),
            i => Assert.Equal("Boabă " + i.Name[..^" Berry".Length], names[i.Name]));
        Assert.All(DataItems.Where(i => i.TeachesMove != null), i => Assert.Equal(i.Name, names[i.Name]));
        Assert.Equal("Poké Ball", names["Poké Ball"]);
        Assert.Equal("Pantofii de Alergare", names["Running Shoes"]);
        // The names the screens' words already gave these things
        Assert.Equal("Cutia de Modă", names["Fashion Case"]);
        Assert.Equal("Cutia de Poffinuri", names["Poffin Case"]);
        Assert.Equal("Cutia de Stickere", names["Seal Case"]);
        Assert.Equal("Harta Regiunii", names["Town Map"]);
    }
}
