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
        if (!flow.Party.HasUsablePokemon) return;
        // Met in the field: the roamers and the Poké Radar hear how it ends (plan 06 · R13)
        flow.FieldEncounter = true;
        // A roamer is battled as it is, and runs (AddRoamerToEnemyParty)
        if (entry.Roamer is { } roamer)
        {
            var met = RoamerToBattle(roamer);
            MeetWildPokemon(met, BattleKind.Roamer, nuzlockeCatch: MeetUnderTheRun(met));
            return;
        }
        // A randomised run meets the species its table's species go to (plan 13 · V6); every other run, the table's own
        var random = Randomizer.For(flow.Run);
        var wildSpecies = PokemonDatabase.Get(random?.Species(entry.SpeciesName) ?? entry.SpeciesName)!;
        Random rng = fieldRandom;
        // At the region's mapped level in a region the game crossed into (plan 18 · W4), drawn as ever
        int lvl = LevelHere(rng.Next(entry.MinLevel, entry.MaxLevel + 1));
        // The gender and the nature are the lead's ability's choice when it made one (Cute Charm, Synchronize); a Poké
        // Radar patch's sparkle is a personality found shiny for the player (plan 06 · R15)
        var wild = entry.Shiny
            ? new Pokemon(wildSpecies, lvl, Personality.ShinyWith(wildSpecies, PlayerIdentity.Number, rng, entry.Gender, entry.Nature), rng)
            : new Pokemon(wildSpecies, lvl, gender: entry.Gender, nature: entry.Nature);
        // The Shiny Charm's two extra rolls (plan 13 · V5), keeping what the lead's ability chose; none without the charm
        if (!entry.Shiny) ShinyCharm.Roll(wild, flow.Bag, rng, entry.Gender, entry.Nature);
        // Shellos and Gastrodon east of Mt. Coronet, Unown in their room's letters (AddWildMonToParty; plan 06 · R10)
        var area =flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY);
        if (FormRules.WildForm(wildSpecies, area?.EastSea ?? false, area?.UnownTable ?? 0, rng) is { } form) wild.ChangeForm(form);
        // Deerling and Sawsbuck in the season's coat where the region has seasons (plan 22 · U4)
        DressForTheSeason(wild);
        // A horde (plan 14 · B3): the slot's species again for each of the others, each at the level drawn for it, met
        // one against all of them. Not beside a partner, on the water, in the Great Marsh's game or in the Contest
        if (entry.Count > 1 && partner == null && flow.Player.Mode != TravelMode.Surfing && !flow.Safari.Active && !InBugContest)
        {
            var horde = new List<Pokemon> { wild };
            for (int i = 1; i < entry.Count; i++)
            {
                int drawn = entry.OthersLevels is { } levels && i - 1 < levels.Length ? levels[i - 1] : rng.Next(entry.MinLevel, entry.MaxLevel + 1);
                var other = new Pokemon(wildSpecies, LevelHere(drawn));
                ShinyCharm.Roll(other, flow.Bag, rng, null, null);
                if (FormRules.WildForm(wildSpecies, area?.EastSea ?? false, area?.UnownTable ?? 0, rng) is { } otherForm) other.ChangeForm(otherForm);
                DressForTheSeason(other);
                horde.Add(other);
            }
            MeetWildPokemon(wild, BattleKind.Normal, horde: horde, nuzlockeCatch: MeetUnderTheRun(wild));
            return;
        }
        // Beside a partner every Pokémon of the grass comes with a second (TryGenerateGrassEncounter_DoubleBattle): both
        // are drawn as the first was, and the lead's ability scaring either off leaves the grass quiet. In Black 2's dark
        // grass a second comes now and then by itself, to a player with two able to fight (plan 22 · U4)
        Pokemon? second = null;
        bool darkPair = partner == null && flow.Player.Mode != TravelMode.Surfing && !flow.Safari.Active
            && Overworld.Map.DarkGrassPairs(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Party.Members.Count(p => !p.IsFainted), rng);
        if ((partner != null || darkPair) && flow.Player.Mode != TravelMode.Surfing && !flow.Safari.Active)
        {
            // A pair of the dark grass whose second the lead scared off is one Pokémon, met alone
            var other = flow.Map.MeetAnother(flow.Player.GridX, flow.Player.GridY, flow.Player.Lead, EncounterMomentNow());
            if (other == null && !darkPair)
            {
                flow.FieldEncounter = false;
                return;
            }
            if (other != null)
            {
                var otherSpecies = PokemonDatabase.Get(random?.Species(other.SpeciesName) ?? other.SpeciesName)!;
                second = new Pokemon(otherSpecies, LevelHere(other.MinLevel), gender: other.Gender, nature: other.Nature);
                ShinyCharm.Roll(second, flow.Bag, rng, other.Gender, other.Nature);
                if (FormRules.WildForm(otherSpecies, area?.EastSea ?? false, area?.UnownTable ?? 0, rng) is { } otherForm) second.ChangeForm(otherForm);
                DressForTheSeason(second);
            }
        }
        // In the Great Marsh's game every Pokémon is met in a Safari battle (plan 01 · M7)
        // Under the Nuzlocke the first Pokémon met in a place is its one chance (plan 13 · V4)
        // A Pokémon met alone where the place's wild Pokémon call for help is met in an SOS battle (plan 14 · B5)
        bool sos = second == null && partner == null && !flow.Safari.Active && !InBugContest && (entry.CallsForHelp || area?.CallsForHelp == true);
        MeetWildPokemon(wild, flow.Safari.Active ? BattleKind.Safari : BattleKind.Normal, second: second, nuzlockeCatch: MeetUnderTheRun(wild), sos: sos);
    }

    /// <summary>
    /// The Nuzlocke's word on a wild Pokémon met in the field where the player stands (plan 13 · V4): whether it may be
    /// caught, the place spent when it is the first met there. Free under any other run, which writes nothing, and in the
    /// Bug-Catching Contest, whose catch is the contest's.
    /// </summary>
    private NuzlockeCatch MeetUnderTheRun(Pokemon wild) => InBugContest ? NuzlockeCatch.Free
        : Nuzlocke.Meet(flow.Run, flow.RunState, Nuzlocke.PlaceKeyOf(flow.Map, flow.Player.GridX, flow.Player.GridY), wild, flow.Pokedex);

    /// <summary>What the run restricts in a battle (plan 13 · V5): none in the assistant's lesson, which isn't the player's; null under a run with nothing to restrict.</summary>
    private RunTerms? RunTermsFor(BattleKind kind) => kind == BattleKind.CatchingLesson ? null : flow.Run.BattleTerms();

    /// <summary>The Nuzlocke's word on a Pokémon a script puts in the player's way (a static encounter, plan 13 · V4's gifts clause); free without the clause.</summary>
    private NuzlockeCatch MeetStaticUnderTheRun(Pokemon wild) =>
        Nuzlocke.MeetStatic(flow.Run, flow.RunState, Nuzlocke.PlaceKeyOf(flow.Map, flow.Player.GridX, flow.Player.GridY), wild, flow.Pokedex);

    /// <summary>What a battle is told of the Nuzlocke (plan 13 · V4): null under any other run and in the assistant's lesson, which isn't the player's.</summary>
    private NuzlockeTerms? NuzlockeTermsFor(BattleKind kind, NuzlockeCatch catching = NuzlockeCatch.Free) =>
        flow.Run.Nuzlocke && kind != BattleKind.CatchingLesson ? new NuzlockeTerms(catching) : null;

    /// <summary>
    /// A battle with one wild Pokémon: one met in the grass, or one a script puts in the player's way, perhaps one
    /// that can't be run from (a legendary the story brings), or the assistant's catching lesson, which the player
    /// watches (plan 06 · R9). (Not a second <c>StartWildBattle</c>: the screenshot harness finds that one by its
    /// name alone.)
    /// </summary>
    private void MeetWildPokemon(Pokemon wildPkmn, BattleKind kind = BattleKind.Normal, bool cannotFlee = false, Pokemon? second = null,
        Story.LessonCatcher? catcher = null, NuzlockeCatch nuzlockeCatch = NuzlockeCatch.Free, IReadOnlyList<Pokemon>? horde = null, bool sos = false)
    {
        if (second != null) flow.Pokedex.RegisterSeen(second);
        // A horde's others are seen with the first (plan 14 · B3)
        foreach (var other in horde?.Skip(1) ?? Enumerable.Empty<Pokemon>()) flow.Pokedex.RegisterSeen(other);
        // The lesson is fought with a Pokédex of its own (FieldBattleDTO_NewCatchingTutorial): the player's isn't told
        if (kind != BattleKind.CatchingLesson) flow.Pokedex.RegisterSeen(wildPkmn);
        // The lesson is the assistant's: their own Pokémon, their name, a bag of twenty Poké Balls (FieldBattleDTO_NewCatchingTutorial)
        var party = flow.Party;
        var bag = flow.Bag;
        string? name = null, lessonLook = null;
        if (kind == BattleKind.CatchingLesson)
        {
            party = new Party();
            // Or whoever else shows it, with their own Pokémon (plan 21 · H2: Wally, with his father's Zigzagoon)
            // (the assistant's starter is the species its slot goes to in a randomised run, as the lines name it: plan 13 · V6)
            string assistants = flow.Story.AssistantStarter ?? "Piplup";
            party.Add(catcher?.Own ?? new Pokemon(PokemonDatabase.Get(Randomizer.For(flow.Run)?.Species(assistants) ?? assistants)!, 5));
            bag = new Inventory();
            bag.AddItem(ItemDatabase.Get("Poké Ball")!, 20);
            name = catcher != null ? PlayerIdentity.Fill(catcher.Name) : PlayerIdentity.Fill("{assistant}");
            // And someone else's lesson has their figure on the player's platform, made behind the fade: their own look
            // by their name where the looks have one (plan 21 · H2: Wally's). The assistant's is left as it was
            if (catcher != null)
            {
                lessonLook = CharacterStyles.Default.CharacterFor(catcher.Name.ToLowerInvariant());
                CharacterModels.Preload(new[] { lessonLook });
            }
        }

        // The battle theme cuts in as the screen starts to flash, before the battle itself appears: a legendary's or
        // a mythical's own, or the wild battle theme
        eyeThemePlaying = false;
        AudioManager.PlayWildBattleMusic(wildPkmn.Species);
        // Whoever travels with the player battles beside them, with a fresh team of their own
        var beside = kind == BattleKind.Normal ? PartnerTrainer() : null;
        // They stand on the platform beside the player, and their rig is made behind the fade (plan 06 · R16c)
        if (beside != null) CharacterModels.Preload(new[] { CharacterStyles.Default.InBattle(beside.TrainerClass, beside.Look is { } look ? PlayerIdentity.CharacterFor(look) : null) });
        var wilds = horde != null ? horde.ToList() : second != null ? new List<Pokemon> { wildPkmn, second } : new List<Pokemon> { wildPkmn };
        // What wild Pokémon hold, and the Poké Radar's chain, which only a Pokémon of the field's own lets go on (plan 06 · R13)
        GiveWildItems(wilds, kind);
        if (!flow.FieldEncounter) EndRadarChain();
        var shown = PrepareModels(wilds.Concat(beside?.Party.Members ?? Enumerable.Empty<Pokemon>()));
        // Every ally an SOS battle's Pokémon may call is made ready behind the fade too, so none comes in waiting for its model (plan 14 · B5)
        if (sos) shown.AddRange(SosAllyLooks(wildPkmn).Where(name => !shown.Contains(name)).Select(name => { flow.WantModel(name); return name; }).ToList());
        // Who is met and where, which is what the way into the battle goes by (plan 09 · L13): the ground is the
        // battle's own, so a Pokémon hooked on a rod is met the water's way from any bank, and the first Pokémon
        // measured against it is the first of the team that fights (the assistant's, in the lesson)
        var meeting = new BattleMeeting
        {
            WildSpecies = wildPkmn.Species.Name,
            Double = second != null || horde != null,
            Kind = kind,
            Terrain = BattleConditionsHere().Terrain,
            TheirLevel = wildPkmn.Level,
            OwnLevel = party == flow.Party ? LeadLevel() : party.Members.FirstOrDefault(p => !p.IsFainted)?.Level ?? 0
        };
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(shown);
            flow.Battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = party,
                Inventory = bag,
                Pokedex = flow.Pokedex,
                PcStorage = flow.Boxes,
                Place = PlaceName(),
                WildPokemon = wilds,
                Format = horde != null ? BattleFormat.Horde : second != null ? BattleFormat.Double : sos ? BattleFormat.Sos : BattleFormat.Single,
                Partner = beside,
                Conditions = BattleConditionsHere(),
                Kind = kind,
                CannotFlee = cannotFlee,
                PlayerName = name,
                PlayerLook = lessonLook,
                SpecialBalls = kind == BattleKind.Safari ? flow.Safari.Balls : kind == BattleKind.PalPark ? flow.Pastimes.PalPark.Show?.ParkBalls ?? 0
                    : kind == BattleKind.Normal && InBugContest ? flow.BugContest.Balls : 0,
                MoveHints = MoveHintsOn,
                Difficulty = flow.Run.Difficulty,
                RunLevelCap = CurrentLevelCap() ?? 0,
                Nuzlocke = NuzlockeTermsFor(kind, nuzlockeCatch),
                Restrictions = RunTermsFor(kind),
                Scene = Settings.BattleScene
            });
            // A Pokémon hooked on a rod is fought on the water's stage, as a surfer's is (the original's water
            // terrain gives both the same platforms), whatever ground the player cast from
            if (flow.Hooked) battleRenderer.SetArena(BattleArena.Water, trees: flow.Map.TreesAt(flow.Player.GridX, flow.Player.GridY));
            else battleRenderer.SetArena(flow.Map, flow.Player.GridX, flow.Player.GridY);
        }, SceneTransition.ForBattle(meeting));
    }

    /// <summary>
    /// What a battle's rules ask of where and when it is fought: the Dive, Dusk and Repeat Balls do, and the
    /// weather of the place comes into the battle with it. And of the player (plan 06 · R10): the badges a traded
    /// Pokémon obeys by, the money a loss takes a share of, and who the player is. What a script or a venue asks of
    /// the battle itself (<paramref name="terms"/>, plan 14 · B1: an inverse battle) goes in with them, which is the
    /// only way it reaches a battle: nothing sets it on one that is running.
    /// </summary>
    private Battle.Sim.BattleConditions BattleConditionsHere(BattleTerms? terms = null) => new()
    {
        Inverse = terms?.Inverse ?? false,
        // A Pokémon hooked on a rod is fought on the water, wherever the player stands (FieldBattleDTO_SetWaterTerrain)
        Terrain = flow.Hooked ? Battle.Sim.BattleTerrain.Water : TerrainAt(flow.Map, flow.Player.GridX, flow.Player.GridY),
        Fishing = flow.Hooked,
        Night = GameClock.IsNight,
        HasCaught = species => flow.Pokedex.IsCaught(species.DexNumber),
        Weather = Weathers.InBattle(flow.Map.WeatherAt(flow.Player.GridX, flow.Player.GridY)),
        Badges = flow.Story.BadgeCount(Here.Id),
        Money = flow.Money,
        Player = PlayerMark,
        // The Bug-Catching Contest throws its Sport Balls and nothing else (plan 15 · E5)
        OnlyBall = InBugContest ? Battle.Sim.BattleCore.BugContestBall : null
    };

    /// <summary>
    /// What the clock does to the team (plan 06 · R10): each new day takes a day off Pokérus
    /// (<c>Party_UpdatePokerusStatus</c>), and from eight in the evening a Shaymin in its Sky Forme is back in its
    /// Land Forme (<c>Party_SetShayminForm</c>, which reverts it as the clock passes eight).
    /// </summary>
    private void KeepTheClock()
    {
        var today = GameClock.Today;
        int days = flow.LastDay is { } before && today > before ? (today - before).Days : 0;
        if (days > 0) PokerusRules.DaysPass(flow.Party, days);
        flow.LastDay = today;
        // The day's numbers, the daily flags and the honey trees' minutes (plan 06 · R13)
        KeepTheEncounterClock(days);
        if (FormRules.ShayminNight((int)GameClock.Hour))
            foreach (var p in flow.Party.Members) FormRules.BackToLand(p);
    }

    /// <summary>Who the player is, as a Pokémon's original trainer is marked (plan 06 · R10).</summary>
    private TrainerMark PlayerMark => new(PlayerIdentity.Name, flow.TrainerId, PlayerIdentity.Look);

    /// <summary>
    /// What follows any battle in the field (plan 06 · R10): Pokérus may come to the team and spread through it
    /// (<c>BattleControllerPlayer_EndFight</c>, on the field's own chance), and an item the battle left in a
    /// Pokémon's hands (Thief, Pickup) puts Giratina and Arceus in its form.
    /// </summary>
    private void AfterBattle(BattleEngine fought)
    {
        if (fought.Kind != BattleKind.CatchingLesson)
        {
            PokerusRules.TryInfect(flow.Party, fieldRandom);
            PokerusRules.Spread(flow.Party, fieldRandom);
        }
        foreach (var p in flow.Party.Members)
        {
            FormRules.ByHeldItem(p);
            FormRules.ByMoves(p);
        }
        // One caught in a form its item doesn't give it (Giratina, met in its Origin Forme in the Distortion World, plan
        // 02 · S12) is in the form it has outside, wherever the catch was put: the team or a box
        if (fought.Caught is { } caught) FormRules.ByHeldItem(caught);
    }

    /// <summary>
    /// The ground a battle here is fought on, as the original picks it (<c>CalcTerrain</c>, <c>sTerrainForBackground</c>):
    /// the tile under the player first (ice, tall grass, sand, snow, the marsh's mud, a cave floor, water), then the
    /// area's battle background, and the map's stage where it has none. The Dive and Dusk Balls, Camouflage,
    /// Nature Power and Secret Power go by it.
    /// </summary>
    internal static Battle.Sim.BattleTerrain TerrainAt(Map map, int x, int y)
    {
        // The sea floor is Emerald's underwater terrain, which is the water's here: the Dive Ball's (plan 21 · H5)
        if (map.IsUnderwater) return Battle.Sim.BattleTerrain.Water;
        var b = map.BehaviourAt(x, y);
        switch (b)
        {
            case TileBehavior.Ice: return Battle.Sim.BattleTerrain.Ice;
            case TileBehavior.TallGrass or TileBehavior.VeryTallGrass or TileBehavior.DarkGrass or TileBehavior.VeryTallDarkGrass: return Battle.Sim.BattleTerrain.Grass;
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
            BattleArena.Water or BattleArena.Underwater => Battle.Sim.BattleTerrain.Water,
            BattleArena.Snow => Battle.Sim.BattleTerrain.Snow,
            BattleArena.Sand => Battle.Sim.BattleTerrain.Sand,
            BattleArena.Indoors or BattleArena.Gym or BattleArena.City => Battle.Sim.BattleTerrain.Building,
            BattleArena.League => Battle.Sim.BattleTerrain.Special,
            _ => Battle.Sim.BattleTerrain.Plain
        };
    }

    /// <summary>
    /// Someone of the map starts travelling with the player (plan 02 · S6): they walk behind and battle beside the
    /// player as a trainer of Platinum's data; null for both and they stop, where they stand.
    /// </summary>
    private void SetPartner(NPC? who, string? trainerId) => SetAlong(who != null && trainerId != null ? new Follower(who, trainerId) : null);

    /// <summary>
    /// Whoever walks behind the player from now on, in place of whoever did: someone who travels along, a Pokémon of
    /// the team (plan 06 · R16c), or nobody, and then whoever followed stops where they stand.
    /// </summary>
    private void SetAlong(Follower? next)
    {
        if (flow.Along != null)
        {
            flow.Along.Settle();
            foreach (var map in MapDatabase.MapNames.Select(MapDatabase.Get)) if (map.Follower == flow.Along.Who) map.Follower = null;
        }
        flow.Along = next;
        if (flow.Along == null) return;
        // Off the Bicycle: nobody rides with someone walking beside them (SetPlayerBike FALSE as Cheryl joins)
        if (flow.Player.Mode == TravelMode.Cycling && flow.Player.SetCycling(false)) PlayFieldMusic();
        flow.Wandering.Settle(flow.Along.Who);
        flow.Map.Follower = flow.Along.Who;
        partnerHeading = flow.Player.Heading;
    }

    /// <summary>The player has come to a map: whoever walks behind them is behind them if they belong to it.</summary>
    private void KeepPartnerAlong()
    {
        if (flow.Along == null) return;
        if (!flow.Map.Everyone.Contains(flow.Along.Who))
        {
            // Someone who travels along through a place of several maps (plan 22 · U7, part b: Reversal Mountain's floors) is,
            // on each, the person of that map with their key, who the flags keep off it until they come
            if (flow.Along.TrainerId == null || flow.Map.FindPerson(flow.Along.Who.Key) is not { } twin) return;
            flow.Along.Rehome(twin);
        }
        // and is on the map while they do (a game saved on a walk is taken up with them there)
        if (flow.Along.TrainerId != null && flow.Along.Who.Forced != true)
        {
            flow.Along.Who.Forced = true;
            flow.Map.ApplyPresence(flow.Story.Has);
        }
        flow.Map.Follower = flow.Along.Who;
        PutBehind(flow.Along, flow.Player.GridX, flow.Player.GridY, flow.Player.Facing);
        partnerHeading = flow.Player.Heading;
    }

    /// <summary>Whoever follows is put behind a tile of the player's, as far back as their gap where the ground there is open, else a tile.</summary>
    private void PutBehind(Follower who, int x, int y, Direction facing)
    {
        var (dx, dy) = FieldMovement.Delta(facing);
        bool room = who.Gap > 1 && Enumerable.Range(1, who.Gap).All(k => flow.Map.InBounds(x - dx * k, y - dy * k) && !flow.Map.IsSolid(x - dx * k, y - dy * k));
        who.Behind(x, y, facing, room ? who.Gap : 1);
    }

    /// <summary>A fresh team of whoever travels with the player, as the original builds it for every battle; null when nobody does.</summary>
    private Trainer? PartnerTrainer()
    {
        if (partner is not { TrainerId: { } id } beside || TrainerDatabase.Get(id) is not { } record) return null;
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        // On the battle's platform they are who they are in the field (plan 06 · R16c)
        trainer.Look = beside.Who.NpcType;
        return trainer;
    }

    /// <summary>
    /// The levels of the region the player stands in for a team that crossed into it (plan 18 · W4, the region file's
    /// <c>levels</c>): null in the region the game began in, where its own game's levels stand, and where the file maps
    /// none (Sinnoh).
    /// </summary>
    private LevelMapping? CrossedLevels()
    {
        var here = Here;
        return string.Equals(here.Id, flow.BeganIn, StringComparison.OrdinalIgnoreCase) ? null : here.Levels;
    }

    /// <summary>The level a Pokémon of the region's own game's <paramref name="level"/> is met at where the player stands (plan 18 · W4).</summary>
    private int LevelHere(int level) => CrossedLevels()?.Map(level) ?? level;

    /// <summary>The level of the first Pokémon the player would send out: what Platinum measures a foe against to pick the way into the battle.</summary>
    private int LeadLevel() => flow.Party.Members.FirstOrDefault(p => !p.IsFainted)?.Level ?? 0;

    /// <summary>The level the run's cap holds the player's Pokémon to now (plan 13 · V3), null for none: Normal, no table for the region, or its story done.</summary>
    internal int? CurrentLevelCap() => LevelCaps.For(flow.Run, Here.Id, flow.Story);

    /// <summary>
    /// A battle with a trainer of the map; with a <paramref name="partner"/>, a tag battle beside them (plan 06 · R9);
    /// with <paramref name="terms"/>, one of the formats a script may ask for (plan 14 · B1: an inverse battle; B14: a
    /// Master Trainer's, one Pokémon of the team alone and the bag closed).
    /// </summary>
    private void StartTrainerBattle(NPC trainerNpc, NPC? secondNpc = null, Trainer? partner = null, bool firstBattle = false, BattleTerms? terms = null)
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
        // Each is drawn on the platform as their class, or as the person who carries them where the class names
        // nobody in particular (plan 11 · C1), whose rig is built behind the fade
        trainer.Look = trainerNpc.NpcType;
        if (secondNpc?.TrainerData is { } carried) carried.Look = secondNpc.NpcType;
        // A randomised run's trainers bring the species theirs go to, on copies (plan 13 · V6), the rival's first battle
        // included, so his team is the one the starter taken names, mapped; a Master Trainer's one species stays
        if (Randomizer.For(flow.Run) is { MapsSpecies: true } random && terms?.Alone == null)
        {
            trainers = trainers.Select(random.ForTrainer).ToList();
            trainer = trainers[0];
            if (partner != null) partner = random.ForTrainer(partner);
        }
        // Whoever battles beside the player stands on the platform too, as the person they are in the field: whoever
        // travels along, or someone of the map a script named (plan 06 · R16c)
        if (partner != null)
            partner.Look ??= (this.partner is { } beside && beside.TrainerId == partner.Id ? beside.Who : flow.Map.Everyone.FirstOrDefault(n => n.TrainerData == partner))?.NpcType;
        CharacterModels.Preload(trainers.Concat(partner == null ? Enumerable.Empty<Trainer>() : new[] { partner })
            .Select(t => CharacterStyles.Default.InBattle(t)));
        // In a region the game crossed into, everyone fights at the region's mapped levels (plan 18 · W4): the battle is
        // handed copies, and the trainers of the map keep their teams at their game's levels
        if (CrossedLevels() is { } levels)
        {
            trainers = trainers.Select(t => t.Relevelled(levels.Map)).ToList();
            trainer = trainers[0];
            partner = partner?.Relevelled(levels.Map);
        }
        // The difficulty chosen at NEW GAME is laid on a copy of each foe (plan 13 · V2): the trainers of the map keep
        // their teams, and under Normal the very same objects are handed on. The first battle of a game, the tutorial
        // the rival fights beside the player's first Pokémon, and a Master Trainer's (a Pokémon chosen to fight alone, at
        // a level of its own) are left as they are
        if (!firstBattle && terms?.Alone == null)
            trainers = trainers.Select(t => DifficultyRules.Prepare(t, flow.Run, LeadLevel())).ToList();
        trainer = trainers[0];

        eyeThemePlaying = false;
        AudioManager.PlayMusic(MusicDirector.BattleRole(trainers.Select(t => t.TrainerClass)), immediate: true);
        var shown = PrepareModels(trainers.SelectMany(t => t.Party.Members).Concat(partner?.Party.Members ?? Enumerable.Empty<Pokemon>()));
        bool two = trainer.DoubleBattle || trainers.Count > 1 || partner != null;
        // A trainer of a later game's format alone against the player asks for it (plan 14 · B6: a triple), which the
        // battle fights where the rules and both sides allow and otherwise makes the double or single they can field
        var asked = trainers.Count == 1 && partner == null && trainer.AskedFormat is { } format ? format : two ? BattleFormat.Double : BattleFormat.Single;
        // Who is met and where, which is what the way into the battle goes by (plan 09 · L13): the first trainer's
        // class decides for both, as in the original, and the first Pokémon of theirs that can fight is measured
        // against the player's
        var meeting = new BattleMeeting
        {
            Trainers = BattleMeeting.Of(trainers),
            Double = two || asked == BattleFormat.Triple,
            Terrain = BattleConditionsHere().Terrain,
            TheirLevel = (trainer.Party.Members.FirstOrDefault(p => !p.IsFainted) ?? trainer.Party.Members[0]).Level,
            OwnLevel = terms?.Alone?.Level ?? LeadLevel()
        };
        // A Master Trainer's battle (plan 14 · B14): the player's side is the one Pokémon put forward, alone, the
        // team's own object, so the EXP, friendship and PP the battle changes stay with it (decision 9)
        var side = flow.Party;
        if (terms?.Alone is { } alone)
        {
            side = new Party();
            side.Add(alone);
        }
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(shown);
            flow.Battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = side,
                Inventory = flow.Bag,
                Pokedex = flow.Pokedex,
                PcStorage = flow.Boxes,
                Place = PlaceName(),
                Format = asked,
                Trainers = trainers,
                Partner = partner,
                FirstBattle = firstBattle,
                NoBattleItems = terms?.NoBattleItems ?? false,
                Conditions = BattleConditionsHere(terms),
                MoveHints = MoveHintsOn,
                TrainerLines = TrainerLinesOn,
                // Platinum's two battle options (plan 08 · P1): the core offers only where a single battle allows it
                OffersSwitch = Settings.BattleStyle == BattleStyle.Shift && !flow.Run.SetStyle,
                Difficulty = flow.Run.Difficulty,
                RunLevelCap = CurrentLevelCap() ?? 0,
                Nuzlocke = NuzlockeTermsFor(BattleKind.Normal),
                Restrictions = RunTermsFor(BattleKind.Normal),
                Scene = Settings.BattleScene
            });
            battleRenderer.SetArena(flow.Map, flow.Player.GridX, flow.Player.GridY);
            flow.BattleTrainers.Clear();
            flow.BattleTrainers.Add(trainerNpc);
            if (secondNpc?.TrainerData != null) flow.BattleTrainers.Add(secondNpc);
        }, SceneTransition.ForBattle(meeting));
        // A Gym Leader, one of the Elite Four and the Champion are met on a band with their portrait (plan 09 · L15):
        // of the look the battle itself will draw them in, and beside the player's own in the League's. The rigs
        // are asked for above; the portraits are rendered as the closing's frames find them ready
        if (SceneTransition.BandFor(meeting) is { } whose)
            band.Begin(whose, CharacterStyles.Default.InBattle(trainer), flow.Kind == TransitionKind.EliteBand ? PlayerIdentity.Character : null);
    }

    /// <summary>Whether the move cards say how a move will do: the option, with Rules following the game's own rules (plan 12 · Q10).</summary>
    private bool MoveHintsOn => Settings.MoveHints.Holds(Ruleset.Current.Preset);

    /// <summary>Whether the game says what to do next: the option, with Rules following the game's own rules (plan 12 · Q14).</summary>
    internal bool NextObjectiveOn => Settings.NextObjective.Holds(Ruleset.Current.Preset);

    /// <summary>The line that says what to do next, for the region the player is in; empty when the option is off or the region has none.</summary>
    internal string NextObjectiveLine => NextObjectiveOn ? Objectives.Line(flow.Story, Here.Id) : "";

    /// <summary>Whether the trainers across the field speak in battle: the option, with Rules following the game's own rules (plan 11 · C12).</summary>
    private bool TrainerLinesOn => Settings.TrainerLines.Holds(Ruleset.Current.Preset);

    /// <summary>
    /// Starts building the models a battle will show (the foes and the whole team, who may be sent out), so they are
    /// ready by the time the screen has faded out; returns their species.
    /// </summary>
    private List<string> PrepareModels(IEnumerable<Pokemon> foes)
    {
        var all = foes.Concat(flow.Party.Members).ToList();
        var species = all.Select(p => p.LookName).Concat(all.SelectMany(p => BattleShapesOf(p).Select(shape => PokemonLook.Of(shape, p.Gender == Gender.Female, p.IsShiny)))).Distinct().ToList();
        foreach (var name in species) flow.WantModel(name);
        return species;
    }

    /// <summary>
    /// The models of every ally an SOS battle's Pokémon may call (plan 14 · B5; <see cref="SosAllies.AnyAlly"/>): each plain,
    /// and in the caller's regional form where it has one. A shiny or a female ally's look is made as it comes.
    /// </summary>
    private static IEnumerable<string> SosAllyLooks(Pokemon caller)
    {
        string? suffix = caller.FormData is { Kind: FormKind.Regional } form && form.Name.StartsWith(caller.Species.Name) ? form.Name[caller.Species.Name.Length..] : null;
        foreach (var species in SosAllies.AnyAlly(caller.Species))
            yield return suffix != null && species.Form(species.Name + suffix) is { Kind: FormKind.Regional } regional ? regional.Name : species.Name;
    }

    /// <summary>The shapes a Pokémon may take in the battle itself (plan 06 · R7): Castform's weather, Cherrim's sun, Arceus's plate. Their models are asked for with its own.</summary>
    private static IEnumerable<string> BattleShapesOf(Pokemon p)
    {
        // A Mega or primal form it may take (plan 06 · R20), only where the rules of the game have them
        if (Ruleset.Current.MegaEvolution && (PokemonPlatinumEngine.Battle.Sim.BattleCore.MegaFormOf(p) ?? PokemonPlatinumEngine.Battle.Sim.BattleCore.PrimalFormOf(p)) is { } mega)
            return new[] { mega };
        // And the form an Ultra Burst gives (plan 06 · R21)
        if (Ruleset.Current.ZMoves && PokemonPlatinumEngine.Battle.Sim.BattleCore.UltraBurstFormOf(p) is { } ultra)
            return new[] { ultra };
        // And the Gigantamax form a Dynamax gives (plan 06 · R22)
        if (Ruleset.Current.Dynamax && PokemonPlatinumEngine.Battle.Sim.BattleCore.GigantamaxFormOf(p) is { } gmax)
            return new[] { gmax };
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
        bool isDefeat = flow.Battle?.Result == BattleResult.PlayerDefeat;
        // A battle over, or a Pokémon caught in it, is worth an autosave once the field is free again (plan 12 · Q3)
        AutosaveSoon(flow.Battle?.Caught != null ? AutosaveMoment.Catch : AutosaveMoment.Battle);
        ScoreBattle();
        // The Vs. Recorder keeps a league battle now and asks about any other trainer's once the field is back (plan 08 · P10)
        KeepTheRecord(flow.Battle);
        // A battle stops the Pokégear's radio: the field comes back to its own theme (plan 20 · J3)
        SilenceTheRadio();
        // The roamers and the Poké Radar hear how a battle met in the field ended (plan 06 · R13)
        if (flow.Battle != null) EncountersAfterBattle(flow.Battle, isDefeat);
        // A battle a script says may be lost is lost without waking up at home: the story goes on from the loss
        bool goesOn = isDefeat && flow.Runner.IsRunning && flow.BattleMayBeLost;
        flow.BattleMayBeLost = false;
        // Under the Nuzlocke (plan 13 · V4) the team's fallen go into the graveyard, out of the team for good, before
        // anything heals them; a whiteout ends the run; and a Pokémon caught is named once the field is back
        bool runEnds = TheRunsFallen(flow.Battle, isDefeat && !goesOn);
        // Travelling with someone, the team is healed after every battle that isn't lost (encounter.c,
        // Party_HealAllMembers when the partner flag is set)
        if (!isDefeat && partner != null && flow.Battle != null) flow.Party.HealAll();

        // Pokémon that gained a level evolve now that the battle is over, before the field comes back, and so do
        // those waiting for the battle's end (Sirfetch'd's critical hits)
        if (!isDefeat && flow.Battle != null)
        {
            var evolutions = FindEvolutions(flow.Battle.LeveledUp, EvolutionTrigger.LevelUp, cancellable: true);
            evolutions.AddRange(FindEvolutions(flow.Party.Members.Where(p => evolutions.All(r => r.Pokemon != p)), EvolutionTrigger.BattleEnd, cancellable: true));
            if (PlayEvolutions(evolutions, GameState.Overworld)) return;
        }

        StartTransition(GameState.Overworld, () =>
        {
            // The foes' models are no longer needed; the team's stay for the next battle
            PokemonModels.Trim(flow.Party.Members.Select(p => p.LookName));
            if (goesOn)
            {
                flow.Party.HealAll();
            }
            else if (isDefeat)
            {
                AbandonBugContest();
                WhiteOut();
                // The run's end is written once, as it ended, and never again (plan 13 · V4)
                if (runEnds)
                {
                    WriteSave(runsLastWord: true);
                    ShowNotification(Text.Of("run.ended.whiteout"));
                }
            }
            PlayFieldMusic();
            // The Safari Game keeps the balls the battle didn't throw, and ends with the last of them
            if (flow.Safari.Active && flow.Battle?.Kind == BattleKind.Safari)
            {
                flow.Safari.Balls = flow.Battle.SpecialBalls;
                if (flow.Safari.OutOfBalls) StartScript(FieldScripts.SafariOutOfBalls);
            }
            // A catch in Pal Park is the Catching Show's next (plan 06 · R16b)
            AfterParkBattle();
            // A battle of the Bug-Catching Contest: its balls, its catch, its last ball (plan 15 · E5)
            AfterContestBattle();
        });
    }

    /// <summary>
    /// What a battle leaves under the Nuzlocke (plan 13 · V4): the team's fallen into the graveyard (not after the
    /// assistant's lesson, which isn't the player's, nor the first battle, the tutorial), the run ended by a
    /// <paramref name="whiteout"/>, and a Pokémon caught waiting to be named once the field is back. True when the run
    /// ends here. Nothing under any other run.
    /// </summary>
    private bool TheRunsFallen(BattleEngine? fought, bool whiteout)
    {
        if (!flow.Run.Nuzlocke || fought == null || flow.RunState.Ended) return false;
        if (fought.Kind == BattleKind.CatchingLesson || fought.Core.FirstBattle) return false;
        var fallen = Nuzlocke.Bury(flow.RunState, flow.Party, Nuzlocke.PlaceKeyOf(flow.Map, flow.Player.GridX, flow.Player.GridY),
            flow.Map.DisplayNameAt(flow.Player.GridX, flow.Player.GridY), flow.PlayTime);
        if (fallen.Count > 0) Diagnostics.Log.Info($"The run lost {string.Join(", ", fallen.Select(p => p.Species.Name))}.");
        if (whiteout)
        {
            flow.RunState.Ended = true;
            flow.RunState.EndedBy = "whiteout";
            return true;
        }
        if (fought.Caught is { } caught && fought.Kind is BattleKind.Normal or BattleKind.Safari or BattleKind.Roamer && !InBugContest)
            flow.NameAfterBattle = caught;
        return false;
    }

    /// <summary>A Pokémon caught under the Nuzlocke is named as the field comes back (plan 13 · V4): the keyboard opens over it, and closes back to it.</summary>
    private bool NameTheCatch()
    {
        if (flow.NameAfterBattle is not { } caught || flow.Runner.IsRunning) return false;
        flow.NameAfterBattle = null;
        flow.NicknameScreen.Open(caught);
        flow.NicknameReturn = GameState.Overworld;
        flow.State = GameState.Nickname;
        return true;
    }
}
