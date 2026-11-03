// The oracles of plan 24 X5 and X6: Pokemon Showdown's damage calculator (@smogon/calc, pinned in package.json and
// package-lock.json) works out every scenario the tests draw, and its answers are written beside them, to be
// committed. Nothing of the calculator's own is written: only numbers it gives for our scenarios.
//
//   npm ci
//   node oracle.mjs [scenarios-gen9.json] [damage-gen9.json]
//   node oracle.mjs --gen 4      (Platinum's rules: scenarios-gen4.json -> damage-gen4.json, X5)
//   node oracle.mjs --check      (exits 1 when the committed answers are not what the pinned calculator gives)
import { calculate, Generations, Pokemon, Move, Field } from '@smogon/calc';
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const folder = join(here, '..', '..', 'PokemonPlatinumTests', 'Oracle');
const args = process.argv.slice(2);
const check = args.includes('--check');
const genAt = args.indexOf('--gen');
const generation = genAt >= 0 ? Number(args[genAt + 1]) : 9;
if (generation !== 4 && generation !== 9) throw new Error(`--gen is 4 or 9, not ${args[genAt + 1]}`);
const files = args.filter((a, i) => !a.startsWith('--') && !(genAt >= 0 && i === genAt + 1));
const scenariosPath = files[0] ?? join(folder, `scenarios-gen${generation}.json`);
const answersPath = files[1] ?? join(folder, `damage-gen${generation}.json`);

const calcVersion = JSON.parse(readFileSync(join(here, 'node_modules', '@smogon', 'calc', 'package.json'), 'utf8')).version;
const gen = Generations.get(generation);

const scenariosText = readFileSync(scenariosPath, 'utf8').replace(/\r\n/g, '\n');
const hash = createHash('sha256').update(scenariosText, 'utf8').digest('hex');
const scenarios = JSON.parse(scenariosText).scenarios;

// Showdown's ids: letters and digits, the accents dropped, the gender signs spelt out
const id = name => name.normalize('NFD').replace(/[̀-ͯ]/g, '').replace('♀', 'f').replace('♂', 'm')
  .toLowerCase().replace(/[^a-z0-9]+/g, '');

const known = (table, name, what) => {
  const found = table.get(id(name));
  if (!found) throw new Error(`the calculator doesn't know the ${what} ${name}`);
  return found.name;
};

// A form the calculator spells otherwise: a female's own form is Showdown's "-F"; a longer name of which ours is the
// start (Necrozma-Dusk is its Necrozma-Dusk-Mane); or ours with its last words dropped (Tauros-Paldea-Blaze-Breed,
// Zygarde-10-Power-Construct, Pikachu-Libre, whose looks alone differ). Whatever this takes is said in the answer's
// notes, and both Pokémon's stats are held to ours by the test, which finds a form taken for the wrong one.
const allSpecies = [...gen.species];
// Where Showdown's species is another form than ours of the same name: our Aegislash is its Shield Forme, our Minior its Meteor Form
const aliases = { aegislash: 'Aegislash-Shield', minior: 'Minior-Meteor' };
function speciesOf(name, notes) {
  if (aliases[id(name)]) { notes.push(`${name} taken as ${aliases[id(name)]}`); return aliases[id(name)]; }
  const exact = gen.species.get(id(name));
  if (exact) return exact.name;
  const tries = [];
  if (name.endsWith('-Female')) tries.push(name.slice(0, -'-Female'.length) + '-F');
  for (const t of tries) { const s = gen.species.get(id(t)); if (s) { notes.push(`${name} taken as ${s.name}`); return s.name; } }
  const longer = allSpecies.filter(s => id(s.name).startsWith(id(name)));
  if (longer.length === 1) { notes.push(`${name} taken as ${longer[0].name}`); return longer[0].name; }
  const words = name.split('-');
  for (let n = words.length - 1; n >= 1; n--) {
    const s = gen.species.get(id(words.slice(0, n).join('-')));
    if (s) { notes.push(`${name} taken as ${s.name}`); return s.name; }
  }
  throw new Error(`the calculator doesn't know the species ${name}`);
}

// An item the calculator doesn't know has no part in a battle's damage (Exp. Share, Smoke Ball): it is worked out with none
function itemOf(name, notes) {
  if (!name) return undefined;
  const found = gen.items.get(id(name));
  if (found) return found.name;
  notes.push(`the calculator doesn't know the item ${name}; worked out with none`);
  return undefined;
}

const stats = ['hp', 'atk', 'def', 'spa', 'spd', 'spe'];
const spread = values => Object.fromEntries(stats.map((s, i) => [s, values[i]]));

function mon(m, notes) {
  const options = {
    level: m.level,
    gender: m.gender,
    nature: m.nature,
    ability: m.ability ? known(gen.abilities, m.ability, 'ability') : 'No Ability',
    item: itemOf(m.item, notes),
    ivs: spread(m.ivs),
    evs: spread(m.evs),
    boosts: { atk: m.boosts[0], def: m.boosts[1], spa: m.boosts[2], spd: m.boosts[3] },
    status: m.status || '',
    // A hit in a battle that has only begun: Slow Start is still at work
    abilityOn: m.ability === 'Slow Start',
    // Protosynthesis and Quark Drive take hold under their sky or terrain, on the best stat by its stage (plan 06 · R27b)
    boostedStat: generation === 9 && (m.ability === 'Protosynthesis' || m.ability === 'Quark Drive') ? 'auto' : undefined
  };
  const species = speciesOf(m.species, notes);
  let p = new Pokemon(gen, species, options);
  // A share of its HP in thousandths, made whole as the test makes it
  if (m.hp > 0) p = new Pokemon(gen, species, { ...options, curHP: Math.max(1, Math.floor(p.maxHP() * m.hp / 1000)) });
  return p;
}

const statsOf = p => stats.map(s => p.rawStats[s]);

const answers = scenarios.map(s => {
  const notes = [];
  try {
    const attacker = mon(s.attacker, notes), defender = mon(s.defender, notes);
    const move = new Move(gen, known(gen.moves, s.move, 'move'), { isCrit: s.crit });
    const field = new Field({
      gameType: s.doubles ? 'Doubles' : 'Singles',
      weather: s.field.weather ?? undefined,
      // Generation 4 has no terrain and no Aurora Veil, and its scenarios draw neither
      terrain: generation === 9 ? s.field.terrain ?? undefined : undefined,
      defenderSide: { isReflect: s.field.reflect, isLightScreen: s.field.lightScreen, isAuroraVeil: generation === 9 && s.field.auroraVeil }
    });
    const result = calculate(gen, attacker, defender, move, field);
    let damage = result.damage;
    if (typeof damage === 'number') damage = Array(16).fill(damage);
    if (!Array.isArray(damage) || damage.length !== 16 || Array.isArray(damage[0])) throw new Error(`the calculator gave ${JSON.stringify(damage)}`);
    return { id: s.id, damage, attacker: statsOf(attacker), defender: statsOf(defender), error: null, notes };
  } catch (e) {
    return { id: s.id, damage: null, attacker: null, defender: null, error: String(e.message ?? e), notes };
  }
});

let out = '{\n';
out += `  "calculator": ${JSON.stringify(`@smogon/calc ${calcVersion}, generation ${generation}`)},\n`;
out += `  "scenarios": ${JSON.stringify(hash)},\n`;
out += '  "answers": [\n';
out += answers.map(a => '    ' + JSON.stringify(a)).join(',\n');
out += '\n  ]\n}\n';

if (check) {
  let kept = '';
  try { kept = readFileSync(answersPath, 'utf8').replace(/\r\n/g, '\n'); } catch { }
  if (kept !== out) {
    console.error(`${answersPath} is not what @smogon/calc ${calcVersion} gives for ${scenariosPath}: run node oracle.mjs and commit it`);
    process.exit(1);
  }
  console.log(`the committed answers are the calculator's (${answers.length} scenarios)`);
} else {
  writeFileSync(answersPath, out);
  const refused = answers.filter(a => a.error).length;
  console.log(`${answers.length} scenarios answered by @smogon/calc ${calcVersion}, ${refused} refused, into ${answersPath}`);
}
