# CalcOracle

The damage oracle of plan 24 · X6: Pokémon Showdown's damage calculator ([`@smogon/calc`](https://github.com/smogon/damage-calc), MIT), pinned to an exact version in `package.json` and `package-lock.json`, works out the damage of every scenario the tests draw, and its answers are committed beside them. The tests never run node; this tool and the workflow `.github/workflows/oracle.yml` do.

```bash
npm ci                        # the pinned calculator, into node_modules (git-ignored)
node oracle.mjs               # PokemonPlatinumTests/Oracle/scenarios-gen9.json -> damage-gen9.json
node oracle.mjs --check       # exits 1 when the committed answers are not what the pinned calculator gives
```

- **The scenarios** are drawn by `PokemonPlatinumTests/Oracle/OracleScenarios.cs` from a `BattleRandom` of a fixed seed and written with `ORACLE=write dotnet test PokemonPlatinumTests --filter OracleTests`. After changing what is drawn, write them, run `node oracle.mjs`, and commit both files: `OracleTests.TheAnswersAreTheCalculatorsToTheseScenarios` holds the answers to the scenarios by a SHA-256 of the scenarios file, which the answers carry.
- **An answer** is the calculator's sixteen rolls, weakest first, both Pokémon's stats as it reckons them, and its notes: an item it doesn't know is worked out as none (they are items with no part in damage: Exp. Share, Smoke Ball), and a form it spells otherwise is named as it takes it (`Pyroar-Female taken as Pyroar`). A scenario it refuses keeps its reason in `error`.
- **The comparison** is `OracleTests.TheModernRulesAreGeneration9s`: each scenario built as two battlers under `Ruleset.Modern` and `DamageCalculator.Calculate` asked for each roll. A difference fails unless `PokemonPlatinumTests/Oracle/known-gen9.json` names it, with the session of plan 06 that owes it and a row in `docs/mechanics/rulings.md` ("Oracle disagreements"). `ORACLE=report` writes the tally of what each entry excuses into `disagreements.received.txt` (git-ignored) even when the test passes.
- **Moving the pin**: change the version in `package.json`, `npm install`, run `node oracle.mjs`, and read the diff of the answers before committing them with the lock file.

Nothing of the calculator's own code or data is written into the repository: only the numbers it gives for our scenarios.
