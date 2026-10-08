partial class Harness
{
    // ---------------------------------------------------------------- species in battle (plan 03 · D6)

    // versus <mine> <foe> [<mine> <foe> ...]: a wild battle for each pair, the first species (or form) leading the player's
    // party and the second met in the wild, shot at the main menu (95_versus_<mine>_<foe>). Not part of "all"
    public void VersusMode()
    {
        var names = args.Skip(2).ToArray();
        for (int i = 0; i + 1 < names.Length; i += 2)
        {
            var mine = Meet(names[i], 20);
            party.Members.Insert(0, mine);
            var vb = StartBattle(names[i + 1], 20);
            ToMainMenu(vb);
            Shot($"95_versus_{names[i].ToLowerInvariant()}_{names[i + 1].ToLowerInvariant()}");
            party.Members.Remove(mine);
            game.State = GameState.Overworld;
        }
    }
}
