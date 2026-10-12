partial class Harness
{
    // Every moment of the turn on one board (plan 25 · Y3), the very last of `conditions`: each started on the foe by
    // the animator's own method in a battle at its menu and cropped at the screen's own resolution as it lands and a
    // little later (96_moment_<name>_a, _b; `contact 96_moment` puts them on sheets), and the ones that sit over a
    // head on the player's own Pokémon too, seen from behind (96_moment_mine_<name>). A board is for looking at the
    // looks side by side; what plays when is the battles' own (96_turn_*).
    void MomentBoards()
    {
        var mine = new Pokemon(PokemonDatabase.Get("Empoleon")!, 40, new Random(71));
        party.HealAll();
        party.Members.Insert(0, mine);
        var foe = new Pokemon(PokemonDatabase.Get("Bidoof")!, 40, new Random(72));
        var (name, x, y) = Place("Route201", -1, -1);
        game.Map = MapDatabase.Get(name);
        if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
        var b = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { foe }, Random = new BattleRandom(70)
        });
        game.BattleRenderer.SetArena(game.Map, x, y);
        game.Battle = b;
        game.State = GameState.Battle;
        ToMainMenu(b);

        void Play(string shot, BattleSide side, BattleMoment moment, bool berry = false)
        {
            // The last cue has run out by now: one moment to a picture
            Skip(BattleAnimator.MomentTime + 0.1);
            var other = side == BattleSide.Enemy ? BattleSide.Player : BattleSide.Enemy;
            b.Anim.Moment(side, 0, moment, moment == BattleMoment.LeechSeed ? (other, 0) : null, berry);
            bool foeSide = side == BattleSide.Enemy;
            Skip(BattleAnimator.ImpactTime - 0.07);
            ShotCrop(shot + "_a", foeSide ? 1090 : 60, foeSide ? 60 : 190, 760, foeSide ? 520 : 600, 2);
            Skip(0.22);
            ShotCrop(shot + "_b", foeSide ? 1090 : 60, foeSide ? 60 : 190, 760, foeSide ? 520 : 600, 2);
        }

        foreach (var moment in Enum.GetValues<BattleMoment>())
            Play($"96_moment_{moment}", BattleSide.Enemy, moment);
        Play("96_moment_HeldItem_berry", BattleSide.Enemy, BattleMoment.HeldItem, berry: true);

        foreach (var moment in new[] { BattleMoment.Asleep, BattleMoment.Confused, BattleMoment.Infatuated, BattleMoment.LevelUp, BattleMoment.BagItem, BattleMoment.EscapeItem,
                     BattleMoment.HeldItem, BattleMoment.Poisoned, BattleMoment.Wrap, BattleMoment.Whirlpool })
            Play($"96_moment_mine_{moment}", BattleSide.Player, moment);
        Play("96_moment_mine_HeldItem_berry", BattleSide.Player, BattleMoment.HeldItem, berry: true);

        // Leech Seed whole: the seeded foe to the player's own, and the other way
        Skip(BattleAnimator.MomentTime + 0.1);
        b.Anim.Moment(BattleSide.Enemy, 0, BattleMoment.LeechSeed, (BattleSide.Player, 0));
        Skip(0.5);
        Shot("96_moment_leech_seed_to_mine");
        Skip(BattleAnimator.MomentTime);
        b.Anim.Moment(BattleSide.Player, 0, BattleMoment.LeechSeed, (BattleSide.Enemy, 0));
        Skip(0.5);
        Shot("96_moment_leech_seed_to_foe");

        party.Members.Remove(mine);
        game.State = GameState.Overworld;
    }
}
