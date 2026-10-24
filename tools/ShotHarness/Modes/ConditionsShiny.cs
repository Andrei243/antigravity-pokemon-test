partial class Harness
{
    // Shiny Pokémon (plan 09 · L1), the very last part of `conditions` (CONDITIONS_FROM=shiny alone): a shiny Gyarados
    // met in the wild, its sparkle as it appears (96_shiny_wild), the battle at its menu with a shiny Luxray leading,
    // both in their shiny colours and with their stars by their names (96_shiny_menu), and the Luxray sent out of its
    // ball again, its sparkle as its stars burst and fly out (96_shiny, 96_shiny_b). It prints the look each place
    // shows and how many quads the sparkle draws.
    void ShinyMode()
    {
        var lead = new Pokemon(PokemonDatabase.Get("Luxray")!, 30, new Random(5)) { IsShiny = true };
        party.HealAll();
        party.Members.Insert(0, lead);
        var b = StartBattle("Gyarados+s", 30, chance: new Random(9));
        PokemonModels.Get("Gyarados+s");
        PokemonModels.Get("Luxray+s");
        Skip(BattleAnimator.ShinyFromAppearing + 0.25);
        Shot("96_shiny_wild");
        ToMainMenu(b);
        Console.WriteLine($"  shiny: the foe is {b.Anim[BattleSide.Enemy, 0].Shown?.LookName}, the lead {b.Anim[BattleSide.Player, 0].Shown?.LookName}");
        Shot("96_shiny_menu");

        b.Anim.SendOut(BattleSide.Player, lead);
        b.Anim.ShinySparkle(BattleSide.Player, 0, fromBall: true);
        Skip(BattleAnimator.ShinyFromBall + 0.15);
        var fx = new FxList();
        MoveFx.Emit(b.Anim, new FxPlaces(), fx);
        Console.WriteLine($"  sparkle: {fx.Count} quads 0.15 s in");
        Shot("96_shiny");
        Skip(0.35);
        Shot("96_shiny_b");

        party.Members.Remove(lead);
        game.State = GameState.Overworld;
    }
}
