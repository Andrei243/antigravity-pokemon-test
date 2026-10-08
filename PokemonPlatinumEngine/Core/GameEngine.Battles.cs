using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Battles started from the field: a wild Pokémon met, a trainer's challenge, the models made ready behind the fade,
/// the partner who fights beside the player, and what the field does once a battle is over.
/// </summary>
public partial class GameEngine
{
    private void StartWildBattle(WildEncounterEntry entry)
    {
        // No wild Pokémon is met by a player with none able to fight: the story gives the first before the grass is
        // ever reached, and a game where that went wrong must not start a battle it can't play (it ended the game)
        if (!playerParty.HasUsablePokemon) return;
        // Met in the field: the roamers and the Poké Radar hear how it ends (plan 06 · R13)
        fieldEncounter = true;
        // A roamer is battled as it is, and runs (AddRoamerToEnemyParty)
        if (entry.Roamer is { } roamer)
        {
            MeetWildPokemon(RoamerToBattle(roamer), BattleKind.Roamer);
            return;
        }
        var wildSpecies = PokemonDatabase.Get(entry.SpeciesName)!;
        Random rng = fieldRandom;
        int lvl = rng.Next(entry.MinLevel, entry.MaxLevel + 1);
        // The gender and the nature are the lead's ability's choice when it made one (Cute Charm, Synchronize)
        var wild = new Pokemon(wildSpecies, lvl, gender: entry.Gender, nature: entry.Nature);
        // A Poké Radar patch's sparkle
        if (entry.Shiny) wild.IsShiny = true;
        // Shellos and Gastrodon east of Mt. Coronet, Unown in their room's letters (AddWildMonToParty; plan 06 · R10)
        var area = currentMap.AreaAt(player.GridX, player.GridY);
        if (FormRules.WildForm(wildSpecies, area?.EastSea ?? false, area?.UnownTable ?? 0, rng) is { } form) wild.ChangeForm(form);
        // Beside a partner every Pokémon of the grass comes with a second (TryGenerateGrassEncounter_DoubleBattle): both
        // are drawn as the first was, and the lead's ability scaring either off leaves the grass quiet
        Pokemon? second = null;
        if (partner != null && player.Mode != TravelMode.Surfing && !safari.Active)
        {
            if (currentMap.MeetAnother(player.GridX, player.GridY, player.Lead, EncounterMomentNow()) is not { } other)
            {
                fieldEncounter = false;
                return;
            }
            var otherSpecies = PokemonDatabase.Get(other.SpeciesName)!;
            second = new Pokemon(otherSpecies, other.MinLevel, gender: other.Gender, nature: other.Nature);
            if (FormRules.WildForm(otherSpecies, area?.EastSea ?? false, area?.UnownTable ?? 0, rng) is { } otherForm) second.ChangeForm(otherForm);
        }
        // In the Great Marsh's game every Pokémon is met in a Safari battle (plan 01 · M7)
        MeetWildPokemon(wild, safari.Active ? BattleKind.Safari : BattleKind.Normal, second: second);
    }

    /// <summary>
    /// A battle with one wild Pokémon: one met in the grass, or one a script puts in the player's way, perhaps one
    /// that can't be run from (a legendary the story brings), or the assistant's catching lesson, which the player
    /// watches (plan 06 · R9). (Not a second <c>StartWildBattle</c>: the screenshot harness finds that one by its
    /// name alone.)
    /// </summary>
    private void MeetWildPokemon(Pokemon wildPkmn, BattleKind kind = BattleKind.Normal, bool cannotFlee = false, Pokemon? second = null)
    {
        if (second != null) playerPokedex.RegisterSeen(second.Species.DexNumber);
        // The lesson is fought with a Pokédex of its own (FieldBattleDTO_NewCatchingTutorial): the player's isn't told
        if (kind != BattleKind.CatchingLesson) playerPokedex.RegisterSeen(wildPkmn.Species.DexNumber);
        // The lesson is the assistant's: their own Pokémon, their name, a bag of twenty Poké Balls (FieldBattleDTO_NewCatchingTutorial)
        var party = playerParty;
        var bag = playerInventory;
        string? name = null;
        if (kind == BattleKind.CatchingLesson)
        {
            party = new Party();
            party.Add(new Pokemon(PokemonDatabase.Get(story.AssistantStarter ?? "Piplup")!, 5));
            bag = new Inventory();
            bag.AddItem(ItemDatabase.Get("Poké Ball")!, 20);
            name = PlayerIdentity.Fill("{assistant}");
        }

        // The battle theme cuts in as the screen starts to flash, before the battle itself appears: a legendary's or
        // a mythical's own, or the wild battle theme
        eyeThemePlaying = false;
        AudioManager.PlayWildBattleMusic(wildPkmn.Species);
        // Whoever travels with the player battles beside them, with a fresh team of their own
        var beside = kind == BattleKind.Normal ? PartnerTrainer() : null;
        var wilds = second != null ? new List<Pokemon> { wildPkmn, second } : new List<Pokemon> { wildPkmn };
        // What wild Pokémon hold, and the Poké Radar's chain, which only a Pokémon of the field's own lets go on (plan 06 · R13)
        GiveWildItems(wilds, kind);
        if (!fieldEncounter) EndRadarChain();
        var shown = PrepareModels(wilds.Concat(beside?.Party.Members ?? Enumerable.Empty<Pokemon>()));
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(shown);
            battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = party,
                Inventory = bag,
                Pokedex = playerPokedex,
                PcStorage = pcBoxStorage,
                Place = PlaceName(),
                WildPokemon = wilds,
                Format = second != null ? BattleFormat.Double : BattleFormat.Single,
                Partner = beside,
                Conditions = BattleConditionsHere(),
                Kind = kind,
                CannotFlee = cannotFlee,
                PlayerName = name,
                SpecialBalls = kind == BattleKind.Safari ? safari.Balls : 0
            });
            // A Pokémon hooked on a rod is fought on the water's stage, as a surfer's is (the original's water
            // terrain gives both the same platforms), whatever ground the player cast from
            if (hooked) battleRenderer.SetArena(BattleArena.Water, trees: currentMap.TreesAt(player.GridX, player.GridY));
            else battleRenderer.SetArena(currentMap, player.GridX, player.GridY);
        }, SceneTransition.ForBattle(trainer: false, leader: false, wildPkmn.Level, LeadLevel()));
    }

    /// <summary>
    /// What a battle's rules ask of where and when it is fought: the Dive, Dusk and Repeat Balls do, and the
    /// weather of the place comes into the battle with it. And of the player (plan 06 · R10): the badges a traded
    /// Pokémon obeys by, the money a loss takes a share of, and who the player is.
    /// </summary>
    private Battle.Sim.BattleConditions BattleConditionsHere() => new()
    {
        // A Pokémon hooked on a rod is fought on the water, wherever the player stands (FieldBattleDTO_SetWaterTerrain)
        Terrain = hooked ? Battle.Sim.BattleTerrain.Water : TerrainAt(currentMap, player.GridX, player.GridY),
        Night = GameClock.IsNight,
        HasCaught = species => playerPokedex.IsCaught(species.DexNumber),
        Weather = Weathers.InBattle(currentMap.WeatherAt(player.GridX, player.GridY)),
        Badges = story.BadgeCount,
        Money = playerMoney,
        Player = PlayerMark
    };

    /// <summary>
    /// What the clock does to the team (plan 06 · R10): each new day takes a day off Pokérus
    /// (<c>Party_UpdatePokerusStatus</c>), and from eight in the evening a Shaymin in its Sky Forme is back in its
    /// Land Forme (<c>Party_SetShayminForm</c>, which reverts it as the clock passes eight).
    /// </summary>
    private void KeepTheClock()
    {
        var today = GameClock.Today;
        int days = lastDay is { } before && today > before ? (today - before).Days : 0;
        if (days > 0) PokerusRules.DaysPass(playerParty, days);
        lastDay = today;
        // The day's numbers, the daily flags and the honey trees' minutes (plan 06 · R13)
        KeepTheEncounterClock(days);
        if (FormRules.ShayminNight((int)GameClock.Hour))
            foreach (var p in playerParty.Members) FormRules.BackToLand(p);
    }

    // The day the clock was last looked at, for Pokérus's days (saved)
    private DateTime? lastDay;

    /// <summary>Who the player is, as a Pokémon's original trainer is marked (plan 06 · R10).</summary>
    private TrainerMark PlayerMark => new(PlayerIdentity.Name, trainerId, PlayerIdentity.Look);

    /// <summary>
    /// What follows any battle in the field (plan 06 · R10): Pokérus may come to the team and spread through it
    /// (<c>BattleControllerPlayer_EndFight</c>, on the field's own chance), and an item the battle left in a
    /// Pokémon's hands (Thief, Pickup) puts Giratina and Arceus in its form.
    /// </summary>
    private void AfterBattle(BattleEngine fought)
    {
        if (fought.Kind != BattleKind.CatchingLesson)
        {
            PokerusRules.TryInfect(playerParty, fieldRandom);
            PokerusRules.Spread(playerParty, fieldRandom);
        }
        foreach (var p in playerParty.Members) FormRules.ByHeldItem(p);
    }

    /// <summary>
    /// The ground a battle here is fought on, as the original picks it (<c>CalcTerrain</c>, <c>sTerrainForBackground</c>):
    /// the tile under the player first (ice, tall grass, sand, snow, the marsh's mud, a cave floor, water), then the
    /// area's battle background, and the map's stage where it has none. The Dive and Dusk Balls, Camouflage,
    /// Nature Power and Secret Power go by it.
    /// </summary>
    internal static Battle.Sim.BattleTerrain TerrainAt(Map map, int x, int y)
    {
        var b = map.BehaviourAt(x, y);
        switch (b)
        {
            case TileBehavior.Ice: return Battle.Sim.BattleTerrain.Ice;
            case TileBehavior.TallGrass or TileBehavior.VeryTallGrass: return Battle.Sim.BattleTerrain.Grass;
            case TileBehavior.Sand: return Battle.Sim.BattleTerrain.Sand;
            case TileBehavior.ShallowSnow or TileBehavior.ShadedSnow or TileBehavior.DeepSnow or TileBehavior.DeeperSnow or TileBehavior.DeepestSnow:
                return Battle.Sim.BattleTerrain.Snow;
            case TileBehavior.Mud or TileBehavior.DeepMud or TileBehavior.MarshGrass or TileBehavior.DeepMarshGrass:
                return Battle.Sim.BattleTerrain.GreatMarsh;
            case TileBehavior.CaveFloor: return Battle.Sim.BattleTerrain.Cave;
        }
        if (TileBehaviors.IsSurfable(b)) return Battle.Sim.BattleTerrain.Water;

        switch (map.AreaAt(x, y)?.BattleBackground)
        {
            case "Plain": return Battle.Sim.BattleTerrain.Plain;
            case "Water": return Battle.Sim.BattleTerrain.Water;
            case "City": return Battle.Sim.BattleTerrain.Building;
            case "Forest": return Battle.Sim.BattleTerrain.Grass;
            case "Mountain": return Battle.Sim.BattleTerrain.Mountain;
            case "Snow": return Battle.Sim.BattleTerrain.Snow;
            case "Indoors1" or "Indoors2" or "Indoors3": return Battle.Sim.BattleTerrain.Building;
            case "Cave1" or "Cave2" or "Cave3": return Battle.Sim.BattleTerrain.Cave;
            case null or "": break;
            // The League's rooms, the Distortion World and the Battle Frontier
            default: return Battle.Sim.BattleTerrain.Special;
        }

        return map.ArenaAt(x, y) switch
        {
            BattleArena.Forest => Battle.Sim.BattleTerrain.Grass,
            BattleArena.Cave => Battle.Sim.BattleTerrain.Cave,
            BattleArena.Water => Battle.Sim.BattleTerrain.Water,
            BattleArena.Snow => Battle.Sim.BattleTerrain.Snow,
            BattleArena.Sand => Battle.Sim.BattleTerrain.Sand,
            BattleArena.Indoors or BattleArena.Gym => Battle.Sim.BattleTerrain.Building,
            BattleArena.League => Battle.Sim.BattleTerrain.Special,
            _ => Battle.Sim.BattleTerrain.Plain
        };
    }

    /// <summary>
    /// Someone of the map starts travelling with the player (plan 02 · S6): they walk behind and battle beside the
    /// player as a trainer of Platinum's data; null for both and they stop, where they stand.
    /// </summary>
    private void SetPartner(NPC? who, string? trainerId)
    {
        if (partner != null)
        {
            partner.Settle();
            foreach (var map in MapDatabase.MapNames.Select(MapDatabase.Get)) if (map.Follower == partner.Who) map.Follower = null;
        }
        partner = who != null && trainerId != null ? new Follower(who, trainerId) : null;
        if (partner == null) return;
        // Off the Bicycle: nobody rides with someone walking beside them (SetPlayerBike FALSE as Cheryl joins)
        if (player.Mode == TravelMode.Cycling && player.SetCycling(false)) PlayFieldMusic();
        wandering.Settle(partner.Who);
        currentMap.Follower = partner.Who;
        partnerHeading = player.Heading;
    }

    /// <summary>The player has come to a map: whoever travels with them is behind them if they belong to it.</summary>
    private void KeepPartnerAlong()
    {
        if (partner == null) return;
        if (!currentMap.Everyone.Contains(partner.Who)) return;
        currentMap.Follower = partner.Who;
        partner.Behind(player.GridX, player.GridY, player.Facing);
        partnerHeading = player.Heading;
    }

    /// <summary>A fresh team of whoever travels with the player, as the original builds it for every battle; null when nobody does.</summary>
    private Trainer? PartnerTrainer()
    {
        if (partner == null || TrainerDatabase.Get(partner.TrainerId) is not { } record) return null;
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        return trainer;
    }

    /// <summary>The level of the first Pokémon the player would send out: what Platinum measures a foe against to pick the way into the battle.</summary>
    private int LeadLevel() => playerParty.Members.FirstOrDefault(p => !p.IsFainted)?.Level ?? 0;

    /// <summary>A battle with a trainer of the map; with a <paramref name="partner"/>, a tag battle beside them (plan 06 · R9).</summary>
    private void StartTrainerBattle(NPC trainerNpc, NPC? secondNpc = null, Trainer? partner = null, bool firstBattle = false)
    {
        // A trainer's battle ends the Poké Radar's chain (Encounter_NewVsTrainer)
        EndRadarChain();
        var trainer = trainerNpc.TrainerData!;
        if (trainer.Party.Count == 0)
        {
            trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
        }
        // Two trainers at once (a tag battle's foes), each with a team of their own
        var trainers = new List<Trainer> { trainer };
        if (secondNpc?.TrainerData is { } second) trainers.Add(second);

        eyeThemePlaying = false;
        AudioManager.PlayMusic(MusicDirector.BattleRole(trainers.Select(t => t.TrainerClass)), immediate: true);
        var shown = PrepareModels(trainers.SelectMany(t => t.Party.Members).Concat(partner?.Party.Members ?? Enumerable.Empty<Pokemon>()));
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(shown);
            battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = playerParty,
                Inventory = playerInventory,
                Pokedex = playerPokedex,
                PcStorage = pcBoxStorage,
                Place = PlaceName(),
                Format = trainer.DoubleBattle || trainers.Count > 1 || partner != null ? BattleFormat.Double : BattleFormat.Single,
                Trainers = trainers,
                Partner = partner,
                FirstBattle = firstBattle,
                Conditions = BattleConditionsHere()
            });
            battleRenderer.SetArena(currentMap, player.GridX, player.GridY);
            battleTrainers.Clear();
            battleTrainers.Add(trainerNpc);
            if (secondNpc?.TrainerData != null) battleTrainers.Add(secondNpc);
        }, SceneTransition.ForBattle(trainer: true, trainer.TrainerClass.Contains("Leader", StringComparison.OrdinalIgnoreCase),
            trainer.Party.Members[0].Level, LeadLevel()));
    }

    /// <summary>
    /// Starts building the models a battle will show (the foes and the whole team, who may be sent out), so they are
    /// ready by the time the screen has faded out; returns their species.
    /// </summary>
    private List<string> PrepareModels(IEnumerable<Pokemon> foes)
    {
        var all = foes.Concat(playerParty.Members).ToList();
        var species = all.Select(p => p.ModelName).Concat(all.SelectMany(BattleShapesOf)).Distinct().ToList();
        foreach (var name in species) PokemonModels.Request(name);
        return species;
    }

    /// <summary>The shapes a Pokémon may take in the battle itself (plan 06 · R7): Castform's weather, Cherrim's sun, Arceus's plate. Their models are asked for with its own.</summary>
    private static IEnumerable<string> BattleShapesOf(Pokemon p)
    {
        switch (p.Species.Name)
        {
            case "Castform" when p.AbilityName == "Forecast":
                return new[] { "Castform-Sunny", "Castform-Rainy", "Castform-Snowy" };
            case "Cherrim":
                return new[] { "Cherrim-Sunshine" };
            case "Arceus" when p.AbilityName == "Multitype" && p.HeldItem?.HoldEffect is { } hold && hold.StartsWith("Arceus") && p.Species.Form("Arceus-" + hold["Arceus".Length..]) != null:
                return new[] { "Arceus-" + hold["Arceus".Length..] };
            default:
                return Array.Empty<string>();
        }
    }

    /// <summary>Waits for models still being built, while the screen is black between scenes.</summary>
    private static void AwaitModels(IEnumerable<string> species)
    {
        foreach (var name in species) PokemonModels.Get(name);
    }

    private void EndBattle()
    {
        bool isDefeat = battle?.Result == BattleResult.PlayerDefeat;
        ScoreBattle();
        // The roamers and the Poké Radar hear how a battle met in the field ended (plan 06 · R13)
        if (battle != null) EncountersAfterBattle(battle, isDefeat);
        // Travelling with someone, the team is healed after every battle that isn't lost (encounter.c,
        // Party_HealAllMembers when the partner flag is set)
        if (!isDefeat && partner != null && battle != null) playerParty.HealAll();
        // A battle a script says may be lost is lost without waking up at home: the story goes on from the loss
        bool goesOn = isDefeat && runner.IsRunning && battleMayBeLost;
        battleMayBeLost = false;

        // Pokémon that gained a level evolve now that the battle is over, before the field comes back, and so do
        // those waiting for the battle's end (Sirfetch'd's critical hits)
        if (!isDefeat && battle != null)
        {
            var evolutions = FindEvolutions(battle.LeveledUp, EvolutionTrigger.LevelUp, cancellable: true);
            evolutions.AddRange(FindEvolutions(playerParty.Members.Where(p => evolutions.All(r => r.Pokemon != p)), EvolutionTrigger.BattleEnd, cancellable: true));
            if (PlayEvolutions(evolutions, GameState.Overworld)) return;
        }

        StartTransition(GameState.Overworld, () =>
        {
            // The foes' models are no longer needed; the team's stay for the next battle
            PokemonModels.Trim(playerParty.Members.Select(p => p.ModelName));
            if (goesOn)
            {
                playerParty.HealAll();
            }
            else if (isDefeat)
            {
                WhiteOut();
            }
            PlayFieldMusic();
            // The Safari Game keeps the balls the battle didn't throw, and ends with the last of them
            if (safari.Active && battle?.Kind == BattleKind.Safari)
            {
                safari.Balls = battle.SpecialBalls;
                if (safari.OutOfBalls) StartScript(FieldScripts.SafariOutOfBalls);
            }
        });
    }
}
