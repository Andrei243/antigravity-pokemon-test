using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The information panel (plan 12 · Q11): what <see cref="BattleInfo"/> tells of the field after a turn, and that it
/// never tells what the player hasn't been told; the <see cref="AbilityShown"/> and <see cref="ItemShown"/> lines
/// carry, which the panel and the trainer AI's memory read in place of the lines' words.
/// </summary>
public class BattleInfoTests
{
    [Fact]
    public void TheLinesAfterATurnOfSandstormReflectSpikesAndSwordsDanceReadAsExpected()
    {
        // The foe does nothing; a Ground type isn't hurt by the sand
        var battle = Battle(Mon("Garchomp", 50, "Sandstorm", "Reflect", "Spikes", "Swords Dance"), Mon("Snorlax", 50));

        Assert.Empty(battle.Info.Field);
        Turn(battle, 0);
        Assert.Equal(new[] { "Sandstorm" }, battle.Info.Field.Select(l => l.Text));

        // Reflect is raised for five turns and the turn's end takes one; the player's own count is told
        Turn(battle, 1);
        Assert.Equal(new[] { "Reflect 4 turns" }, battle.Info.Side(BattleSide.Player).Lines.Select(l => l.Text));

        Turn(battle, 2);
        Assert.Equal(new[] { "Spikes ×1" }, battle.Info.Side(BattleSide.Enemy).Lines.Select(l => l.Text));
        Assert.Equal(InfoTone.Bad, battle.Info.Side(BattleSide.Enemy).Lines[0].Tone);

        Turn(battle, 3);
        var garchomp = battle.Info.Side(BattleSide.Player).Pokemon.Single();
        Assert.Equal(new[] { "Attack +2" }, garchomp.Stages.Select(l => l.Text));
        Assert.Equal(InfoTone.Good, garchomp.Stages[0].Tone);

        // All of it still there two turns on, in the panel's order (the field's, then each side's with its Pokémon's):
        // the sand's count is never told, a held rock would lengthen it
        Assert.Equal(new[] { "Sandstorm", "Reflect 2 turns", "Garchomp: Attack +2", "Spikes ×1" }, battle.Info.Lines);
    }

    [Fact]
    public void AFoesLeftoversIsOnTheSheetOnlyAfterItHasGoneOff()
    {
        var snorlax = Mon("Snorlax", 50);
        snorlax.HeldItem = ItemDatabase.Get("Leftovers");
        var battle = Battle(Mon("Bidoof", 5, "Tackle", "Growl"), snorlax);
        var foe = battle.EnemyPokemon;

        // At full HP the Leftovers do nothing, and nothing says they are there
        Turn(battle, 1);
        Assert.Null(battle.Info.Side(BattleSide.Enemy).Pokemon.Single().Item);
        Assert.Null(battle.Info.Side(BattleSide.Enemy).Pokemon.Single().Ability);

        // Hurt, the foe is healed at the turn's end: the item is known from the line that names it, and not before
        battle.SelectMove(0);
        bool named = false;
        for (int i = 0; i < 40 && battle.HUD.MenuState == BattleMenuState.Message; i++)
        {
            if (battle.IsWaitingForConfirm && battle.CurrentMessage.Contains("Leftovers")) named = true;
            Assert.Equal(named ? "Leftovers" : null, battle.Revealed.ItemOf(foe));
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        Assert.True(named);
        Assert.Equal("Leftovers", battle.Info.Side(BattleSide.Enemy).Pokemon.Single().Item);

        // The player's own Pokémon is known whole
        var bidoof = battle.Info.Side(BattleSide.Player).Pokemon.Single();
        Assert.Equal("Simple", bidoof.Ability);
        Assert.Equal("None", bidoof.Item);
    }

    [Fact]
    public void AFoesScreensAreToldWithoutTheirCountForLightClayWouldLengthenThem()
    {
        var battle = Battle(Mon("Bidoof", 5), Mon("Snorlax", 50, "Reflect"));
        Turn(battle);
        var reflect = battle.Info.Side(BattleSide.Enemy).Lines.Single();
        Assert.Equal("Reflect", reflect.Text);
        Assert.True(battle.Core.Field.Side(BattleSide.Enemy).ReflectTurns > 0);
    }

    [Fact]
    public void AnAbilityNamedOnEntryIsKnownToThePanelAndToTheTrainerAi()
    {
        // Intimidate names itself as the wild Gyarados appears (BattleMessage_AbilityTag)
        var battle = Battle(Mon("Bidoof", 5), Mon("Gyarados", 30));
        Assert.Equal("Intimidate", battle.Info.Side(BattleSide.Enemy).Pokemon.Single().Ability);
        var told = battle.Core.Log.OfType<Said>().Single(s => s.Shows.OfType<AbilityShown>().Any());
        Assert.Contains("Intimidate", told.Text);
        Assert.Equal(new AbilityShown(new Place(BattleSide.Enemy, 0), "Intimidate"), told.Shows.OfType<AbilityShown>().Single());

        battle.Core.AiMemory.CatchUp(battle.Core);
        Assert.Equal("Intimidate", battle.Core.AiMemory.AbilityShown(battle.Core.EnemySlots[0]));
        Assert.Null(battle.Core.AiMemory.AbilityShown(battle.Core.PlayerSlots[0]));
    }

    [Fact]
    public void FriskTellsTheTrainerAiOfTheItemItFinds()
    {
        // BattleAI_SetHeldItem from the Frisk line's item tag: the AI learns of the player's item from the event, not the words
        var mine = Mon("Bidoof", 5);
        mine.HeldItem = ItemDatabase.Get("Oran Berry");
        var foe = Mon("Banette", 30);
        foe.AbilityName = "Frisk";
        var battle = Battle(mine, foe);

        battle.Core.AiMemory.CatchUp(battle.Core);
        Assert.Equal("Oran Berry", battle.Core.AiMemory.ItemShown(battle.Core.PlayerSlots[0])?.Name);
        // The line names the item it found, not the ability that found it
        Assert.Null(battle.Core.AiMemory.AbilityShown(battle.Core.EnemySlots[0]));
        Assert.Null(battle.Info.Side(BattleSide.Enemy).Pokemon.Single().Ability);
    }

    [Fact]
    public void ThePanelOpensFromTheCommandsAndClosesBackToThemChangingNothing()
    {
        var battle = Battle(Mon("Garchomp", 50, "Swords Dance"), Mon("Snorlax", 50));
        Assert.False(battle.HUD.MenuState == BattleMenuState.Info);
        Assert.True(battle.OpenInfo());
        Assert.Equal(BattleMenuState.Info, battle.HUD.MenuState);
        Assert.NotNull(battle.InfoShown);
        Assert.False(battle.OpenInfo());

        battle.CloseInfo();
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Null(battle.InfoShown);
        Turn(battle);
        Assert.Equal(2, battle.PlayerPokemon.StatStages[StatType.Attack]);
    }
}
