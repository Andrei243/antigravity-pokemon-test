using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Pal Park's migration from the title (plan 08 · P21; style guide, "Opening and title screen" and "Menu screens",
/// "Migrate"; <c>docs/mechanics/rulings.md</c>, "Pal Park's migration"): MIGRATE on the title's menu, offered while the
/// <c>migrate</c> folder beside the saves holds a file the readers know, opens <see cref="MigrateScreen"/> over the
/// saves that can receive and the folder's sources. A game isn't loaded: the receiving save is read from its slot,
/// the six are brought into its park (<see cref="Migration.Bring(SaveData, Foreign.ForeignSave, IReadOnlyList{Foreign.ForeignPokemon}, DateTime, Ruleset?)"/>)
/// and the save is written back, so that the Catching Show finds them there when the game is next played. The source
/// is only read: nothing in the folder is written, renamed or moved.
/// </summary>
public partial class GameEngine
{
    /// <summary>Where the player's own files are looked for: the folder beside the saves unless a tool or a test names another.</summary>
    internal string MigrateFolder
    {
        get => migrateFolder ?? SavePaths.Current.Migrate;
        set => migrateFolder = value;
    }

    private string? migrateFolder;

    /// <summary>The time the migration is done at: the computer's clock unless a tool or a test fixes it.</summary>
    internal Func<DateTime>? MigrationClock { get; set; }

    private DateTime MigrationNow => MigrationClock?.Invoke() ?? WallClock.UtcNow;

    /// <summary>Whether the folder holds something the readers know: the title's MIGRATE is offered then.</summary>
    internal void OfferMigration() =>
        flow.TitleScreen.OfferMigration(MigrationFolder.Offers(MigrationFolder.Read(MigrateFolder)));

    private void OpenMigrate()
    {
        var targets = titleSlots.Where(s => s.Taken && s.Save != null).Select(s => new MigrateTarget(s.Number, s.Save!)).ToList();
        // The folder is read afresh, so that a file dropped in or taken out since the game began is seen
        var sources = MigrationFolder.Read(MigrateFolder);
        if (targets.Count == 0)
        {
            flow.TitleScreen.OfferMigration(false);
            return;
        }
        flow.MigrateScreen.Open(targets, sources, () => MigrationNow);
        flow.State = GameState.Migrate;
    }

    private void UpdateMigrate(float dt)
    {
        flow.MigrateScreen.Update(dt);
        if (flow.MigrateScreen.TakeRequest() is { } request) CarryOutMigration(request);
        else if (!flow.MigrateScreen.IsActive) CloseMigrate();
    }

    private void CloseMigrate()
    {
        flow.MigrateScreen.Close();
        flow.State = GameState.Title;
        // What was in the folder may be gone, or all given: MIGRATE is offered as the folder now stands
        OfferMigration();
    }

    /// <summary>
    /// The six chosen are brought into the save's park and the save is written. The slot is read afresh from its file, so
    /// that what is written is the save as it was kept; nothing is changed in the saves held for the title unless the
    /// write succeeded.
    /// </summary>
    internal MigrationBlock CarryOutMigration(MigrateRequest request)
    {
        var paths = SavePaths.Current;
        var fresh = SaveSlots.Read(paths, request.Target.Slot).Save;
        var block = MigrationBlock.CouldNotSave;
        if (fresh != null && request.Source.Save is { } source)
        {
            block = Migration.Bring(fresh, source, request.Six, MigrationNow);
            if (block == MigrationBlock.None && !SaveSlots.Write(paths, request.Target.Slot, fresh))
            {
                Log.Warn($"Pokémon were to be brought into slot {request.Target.Slot}, but its save could not be written; nothing was brought.");
                block = MigrationBlock.CouldNotSave;
            }
            if (block == MigrationBlock.None)
            {
                request.Target.Save.Pastimes = fresh.Pastimes;
                Log.Info($"{request.Six.Count} Pokémon of {request.Source.Name} were brought into slot {request.Target.Slot}.");
            }
        }
        flow.MigrateScreen.Finish(block);
        return block;
    }
}
