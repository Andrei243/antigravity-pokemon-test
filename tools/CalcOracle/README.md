# CalcOracle

The damage oracles of plan 24 · X6 (the modern rules against generation 9) and X5 (Platinum's rules against generation 4): Pokémon Showdown's damage calculator ([`@smogon/calc`](https://github.com/smogon/damage-calc), MIT), pinned to an exact version in `package.json` and `package-lock.json`, works out the damage of every scenario the tests draw, and its answers are committed beside them. The tests never run node; this tool and the workflow `.github/workflows/oracle.yml` do.

```bash
npm ci                        # the pinned calculator, into node_modules (git-ignored)
node oracle.mjs               # PokemonPlatinumTests/Oracle/scenarios-gen9.json -> damage-gen9.json
node oracle.mjs --check       # exits 1 when the committed answers are not what the pinned calculator gives
node oracle.mjs --gen 4       # PokemonPlatinumTests/Oracle/scenarios-gen4.json -> damage-gen4.json (Platinum's rules, X5); --check with it too
```

- **The scenarios** are drawn by `PokemonPlatinumTests/Oracle/OracleScenarios.cs` from a `BattleRandom` of a fixed seed and written with `ORACLE=write dotnet test PokemonPlatinumTests --filter OracleTests`. After changing what is drawn, write them, run `node oracle.mjs`, and commit both files: `OracleTests.TheAnswersAreTheCalculatorsToTheseScenarios` holds the answers to the scenarios by a SHA-256 of the scenarios file, which the answers carry.
- **An answer** is the calculator's sixteen rolls, weakest first, both Pokémon's stats as it reckons them, and its notes: an item it doesn't know is worked out as none (they are items with no part in damage: Exp. Share, Smoke Ball), and a form it spells otherwise is named as it takes it (`Pyroar-Female taken as Pyroar`). A scenario it refuses keeps its reason in `error`.
- **The comparison** is `OracleTests.TheModernRulesAreGeneration9s`: each scenario built as two battlers under `Ruleset.Modern` and `DamageCalculator.Calculate` asked for each roll. A difference fails unless `PokemonPlatinumTests/Oracle/known-gen9.json` names it, with the session of plan 06 that owes it and a row in `docs/mechanics/rulings.md` ("Oracle disagreements").
- **Generation 4** (plan 24 · X5) is the same frame for Platinum's rules: `OracleScenariosGen4` draws 3,000 scenarios of Platinum's 493 species, moves, abilities and items (seed 4; `ORACLE=write` writes them with the rest), `node oracle.mjs --gen 4` answers them, and `OracleGen4Tests.PlatinumsDamageIsGeneration4s` compares under `Ruleset.Platinum`, excused only by `known-gen4.json`. Platinum's data does not move, so `OracleGen4Tests.TheCommittedScenariosAreTheOnesDrawn` holds the file to what is drawn today. The decompilation decides every disagreement: our number wrong is a fix, the calculator wrong an entry naming the calculator. `ORACLE=report` writes `disagreements-gen4.received.txt`. `ORACLE=report` writes the tally of what each entry excuses into `disagreements.received.txt` (git-ignored) even when the test passes.
- **Moving the pin**: change the version in `package.json`, `npm install`, run `node oracle.mjs`, and read the diff of the answers before committing them with the lock file.

Nothing of the calculator's own code or data is written into the repository: only the numbers it gives for our scenarios.
