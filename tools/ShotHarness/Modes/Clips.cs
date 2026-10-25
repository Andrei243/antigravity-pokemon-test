partial class Harness
{
    // ---------------------------------------------------------------- a model file's clips in battle

    // clips <species> [...]: each species leads the player's party against one of its own kind met in the wild, so
    // every clip is seen from behind and from the front in one picture, and each of the battle's moments a model file
    // can have a clip for is played on both and shot at three moments: out of the ball, standing, a physical, special
    // and status move, struck, a miss dodged, tired in the red, a cheer and a faint (99_clips_<species>_<nn>_<role>_<n>, numbered in that order).
    // It prints which clip of the file plays for each, or that the body plan moves it. A model file is looked for in
    // <out dir>/overrides/models, as the harness's working directory is its out dir. `contact 99_clips` puts them on
    // sheets. Not part of "all".
    public void ClipsMode()
    {
        foreach (var name in args.Skip(2))
        {
            string key = name.ToLowerInvariant();
            var mine = Meet(name, 50);
            party.Members.Insert(0, mine);
            var vb = StartBattle(name, 50);
            ToMainMenu(vb);
            TellClips(mine);
            var anim = vb.Anim;
            int shown = 0;

            void Moments(string role, double[] at)
            {
                double now = 0;
                for (int i = 0; i < at.Length; i++)
                {
                    Skip(at[i] - now);
                    now = at[i];
                    Shot($"99_clips_{key}_{++shown:00}_{role}_{i + 1}");
                }
            }
            void Both(Action<BattleSide> start)
            {
                start(BattleSide.Player);
                start(BattleSide.Enemy);
            }

            Moments("idle", new[] { 0.3, 0.8, 1.3 });

            Both(side => anim.SendOut(side, side == BattleSide.Player ? vb.PlayerPokemon : vb.EnemyPokemon));
            Moments("entry", new[] { BattleAnimator.SendOutBallTime + 0.1, BattleAnimator.SendOutBallTime + 0.35, BattleAnimator.SendOutTime + 0.3 });
            Skip(1.0);

            foreach (var (role, category) in new[] { ("physical", MoveCategory.Physical), ("special", MoveCategory.Special), ("status", MoveCategory.Status) })
            {
                Both(side => anim.Attack(side, 0, category));
                Moments(role, new[] { 0.18, (double)BattleAnimator.ImpactTime, 0.55 });
                Skip(0.6);
            }

            Both(side => anim.Hit(side));
            // A struck Pokémon is drawn on alternate tenths of the hit only (BattleRenderer.Appear): shot in the tenths it shows
            Moments("hit", new[] { 0.05, 0.25, 0.45 }.Select(p => p * BattleAnimator.HitTime).ToArray());
            Skip(0.6);

            Both(side => anim.Dodge(side, 0, side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player, 0));
            Moments("dodge", new[] { 0.15, 0.35, 0.6 });
            Skip(0.6);

            // Tired is the bar in the red: both brought down to a tenth and the bars let drain
            int mineHp = vb.PlayerPokemon.CurrentHP, foeHp = vb.EnemyPokemon.CurrentHP;
            vb.PlayerPokemon.CurrentHP = Math.Max(1, vb.PlayerPokemon.MaxHP / 10);
            vb.EnemyPokemon.CurrentHP = Math.Max(1, vb.EnemyPokemon.MaxHP / 10);
            Skip(2.5);
            Moments("tired", new[] { 0.2, 0.9, 1.6 });
            vb.PlayerPokemon.CurrentHP = mineHp;
            vb.EnemyPokemon.CurrentHP = foeHp;
            Skip(2.5);

            Both(side => anim.Cheer(side));
            Moments("cheer", new[] { 0.3, 0.6, 0.9 });
            Skip(0.6);

            Both(side => anim.Faint(side, 0f));
            Moments("faint", new[] { 0.25, 0.6, 0.95 });

            party.Members.Remove(mine);
            game.State = GameState.Overworld;
        }
    }

    // Which clip of a model file plays for each of the battle's moments, as the game matched them
    static void TellClips(Pokemon p)
    {
        var model = PokemonModels.Get(p.LookName);
        if (model.Imported is not { } rig)
        {
            Console.WriteLine($"  {p.LookName}: no model file (overrides/models in the out dir); the body plan moves it all");
            return;
        }
        Console.WriteLine($"  {p.LookName}: {Path.GetFileName(rig.Source)}");
        foreach (var role in Enum.GetValues<ClipRole>())
            Console.WriteLine($"    {role,-8} {(rig.Clips.TryGetValue(role, out int i) ? rig.Asset.Animations[i].Name : "(the body plan's)")}");
    }
}
