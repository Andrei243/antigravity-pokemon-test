// Plan 18 · W4b-2 with plan 03 · D15-A: the lines that name a trainer's Pokémon by a species its team no longer has.
//
// In a region the game crossed into, a trainer's team is met at the region's mapped levels (plan 18 · W4) and each
// Pokémon takes the stage its level allows: the games' own stages, then a species of our own where its level reaches
// one (Models/OursGrowth.cs). A Youngster who says "my Rattata" has a Ratchisel there. This script finds every such
// line (a trainer's own before and after lines in an overlay, their lines in battle in trainer-lines.json, the lines
// of a script that battles them, and any line of the same place that names them or says "my", "her" or "his" before
// the species), says what each trainer's Pokémon become in each crossed region, and with --write puts the new
// species' name in the English line and in Romanian's translation of it (whose "en" it brings up to date).
//
//   node tools/dev/grown-names.mjs            list the trainers whose Pokémon change and the lines that name them
//   node tools/dev/grown-names.mjs --write    rewrite those lines (English and Romanian)
//
// It mirrors the engine's rule for the listing; GrownNamesTests holds the result with the engine's own code.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const data = path.join(root, 'PokemonPlatinumEngine', 'Data');
const write = process.argv.includes('--write');

// JSON with // comments, as the game's files are
const read = file => JSON.parse(fs.readFileSync(file, 'utf8').replace(/^﻿/, '')
    .replace(/("(?:[^"\\]|\\.)*")|\/\/[^\n]*/g, (m, s) => s ?? ''));

// ---- the species and their stages
const species = read(path.join(data, 'species.json'));
const byName = new Map(species.map(s => [s.name, s]));
const isOurs = s => s.dexNumber >= 2000;

const leveledWay = e => (e.level ?? 0) > 0;
const friendship = m => m === 'Friendship' || m === 'FriendshipDay' || m === 'FriendshipNight';

/** What a way asks of the Pokémon itself: its gender and its personality (Tyrogue's stats are taken as the first way). */
function inherent(e, s, personality) {
  const r = s.genderRatio ?? -1;
  const threshold = r < 0 ? 255 : r === 0 ? 0 : r >= 8 ? 254 : r * 32 - 1;
  const female = threshold === 254 || (threshold !== 0 && threshold !== 255 && threshold > (personality & 0xFF));
  switch (e.method) {
    case 'LevelMale': case 'UseItemMale': return threshold !== 255 && !female;
    case 'LevelFemale': case 'UseItemFemale': return female;
    case 'LevelPersonalityLow': return Math.floor(personality / 65536) % 10 < 5;
    case 'LevelPersonalityHigh': return Math.floor(personality / 65536) % 10 >= 5;
    default: return true;
  }
}

/** The games' own next stage a trainer's Pokémon of this level takes, or null (OursGrowth.GamesStageAt). */
function gamesStage(name, form, level, personality) {
  const s = byName.get(name);
  const ways = (s?.evolutions ?? []).filter(e => byName.has(e.targetSpecies) && !isOurs(byName.get(e.targetSpecies))
    && !e.region && e.method !== 'LevelShedinja' && (e.fromForm ? e.fromForm === form : !form));
  if (ways.length === 0) return null;
  const leveled = ways.filter(leveledWay);
  if (leveled.length > 0) return leveled.find(e => e.level <= level && inherent(e, s, personality)) ?? null;
  if (ways.length !== 1) return null;
  return level >= (friendship(ways[0].method) ? 16 : 30) && inherent(ways[0], s, personality) ? ways[0] : null;
}

/** The first evolution of ours this level reaches, never a legendary set's (OursGrowth.OursAt). */
function oursStage(name, form, level) {
  const s = byName.get(name);
  return (s?.evolutions ?? []).find(e => {
    const t = byName.get(e.targetSpecies);
    return t && isOurs(t) && !t.legendary && !t.mythical && e.level <= level && (e.fromForm ? e.fromForm === form : true);
  }) ?? null;
}

function grow(name, form, level, personality) {
  for (let guard = 0; guard < 6; guard++) {
    const e = gamesStage(name, form, level, personality) ?? oursStage(name, form, level);
    if (!e) break;
    name = e.targetSpecies;
    form = e.targetForm ?? null;
  }
  return name;
}

// ---- the regions crossed into and their levels
const regions = [];
for (const file of fs.readdirSync(path.join(data, 'regions'))) {
  const r = read(path.join(data, 'regions', file));
  if (r.levels) regions.push({ id: r.id, folder: r.folder, levels: r.levels });
}
function mapLevel(levels, level) {
  if (levels.length === 1) return Math.min(260, Math.max(1, level + levels[0][1] - levels[0][0]));
  let i = 0;
  while (i < levels.length - 2 && level > levels[i + 1][0]) i++;
  const [f1, t1] = levels[i], [f2, t2] = levels[i + 1];
  const n = (level - f1) * (t2 - t1), d = f2 - f1;
  return Math.min(260, Math.max(1, t1 + Math.floor((2 * n + d) / (2 * d))));
}

// ---- the trainers of each crossed region, and what their Pokémon become
// A Leader, one of the Elite Four, a Champion, a rival or a boss: whose name a line anywhere in the region may give
const named = c => /^(Leader|Elite Four|Champion|Rival|Commander|Boss|Aqua|Magma|Team|Pokémon Trainer Rival)/.test(c ?? '');
const trainers = new Map(); // id -> { region, folder, name, named, changes: Map(old -> new) }
const ourTrainers = read(path.join(data, 'our-trainers.json'));
for (const region of regions) {
  const list = read(path.join(data, 'world', region.folder, 'trainers.json'))
    .concat(ourTrainers.filter(t => t.id.startsWith(region.folder + '_')));
  for (const t of list) {
    const changes = new Map();
    for (const m of t.party ?? []) {
      const into = grow(m.species, m.form ?? null, mapLevel(region.levels, m.level), m.personality ?? 0);
      // Two of one species that become two things, or one that stays beside one that changes, keep the forebear's
      // name: the line can't say which
      if (changes.has(m.species) && changes.get(m.species) !== (into !== m.species ? into : m.species)) changes.set(m.species, null);
      else if (!changes.has(m.species)) changes.set(m.species, into !== m.species ? into : m.species);
    }
    const kept = [...changes].filter(([a, v]) => v && v !== a);
    trainers.set(t.id, { region: region.id, folder: region.folder, name: t.name, named: named(t.class), changes: new Map(kept) });
  }
}

// ---- what a person has written by hand: a line kept as it is, and a line said otherwise than a plain swap would say it
const choices = read(path.join(root, 'tools', 'dev', 'grown-names.json'));
const kept = new Map(Object.entries(choices.kept ?? {}));
const written = new Map(Object.entries(choices.written ?? {}));

// ---- the lines, with the trainers each belongs to
const gamesNames = species.filter(s => !isOurs(s)).map(s => s.name).sort((a, b) => b.length - a.length);
const escape = s => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const nameRe = n => new RegExp(`(?<![A-Za-zÀ-ÿ])${escape(n)}(?![A-Za-zÀ-ÿ])`, 'g');

/**
 * A line's species that its trainers' Pokémon no longer are, and what to say instead. A loose line (a place's, not a
 * trainer's own) counts only a species given as "her", "his" or the trainer's name's.
 */
function rewrite(line, ids, loose) {
  let changed = line;
  const found = [];
  for (const n of gamesNames) {
    if (!nameRe(n).test(changed)) continue;
    const intos = new Set();
    for (const id of ids) {
      const t = trainers.get(id);
      if (!t) continue;
      if (loose && !new RegExp(`(?<![A-Za-z])(her|Her|his|His|${escape(t.name)}'s)\\s+(\\w+\\s+)?${escape(n)}`).test(changed)) continue;
      if (t.changes.get(n)) intos.add(t.changes.get(n));
    }
    if (intos.size !== 1) continue;
    const into = [...intos][0];
    changed = changed.replace(nameRe(n), into);
    found.push(`${n} → ${into}`);
  }
  if (changed === line) return null;
  if (kept.has(line)) return { kept: kept.get(line), found };
  return { line: written.get(line) ?? changed, found };
}

const edits = []; // { file, old, new, found, ids }
const keptLines = []; // { file, line, reason }
function note(file, old, r, ids) {
  if (!r) return;
  if (r.kept) keptLines.push({ file, line: old, reason: r.kept, found: r.found });
  else edits.push({ file, old, new: r.line, found: r.found, ids });
}
const areaTrainers = new Map(); // area key -> Set of trainer ids battled or standing there
const areaRegion = new Map(); // area key -> region
const personScript = new Map(); // area key -> Map(script name -> trainer id)

function trainerOfConstant(folder, constant) {
  if (!constant || !constant.startsWith('TRAINER_')) return null;
  const id = `${folder}_${constant.slice(8).toLowerCase()}`;
  return trainers.has(id) ? id : null;
}

/** The named trainers of a region whose name a line gives. */
const namedIn = (region, line) => [...trainers].filter(([, t]) => t.named && t.region === region && line.includes(t.name)).map(([id]) => id);

// Overlays: each person's trainer lines belong to their trainer; the rest of the place's lines loosely to all of them
for (const region of regions) {
  const overlays = path.join(data, 'world', region.folder, 'overlays');
  for (const file of fs.readdirSync(path.join(data, 'world', region.folder, 'areas'))) areaRegion.set(file.replace(/\.json$/, ''), region.id);
  for (const file of fs.readdirSync(overlays)) {
    const key = file.replace(/\.json$/, '');
    areaRegion.set(key, region.id);
    const areaFile = path.join(data, 'world', region.folder, 'areas', file);
    const objects = fs.existsSync(areaFile) ? read(areaFile).objects ?? [] : [];
    const overlay = read(path.join(overlays, file));
    const ids = areaTrainers.get(key) ?? new Set();
    areaTrainers.set(key, ids);
    const scripts = new Map();
    personScript.set(key, scripts);
    const personTrainer = {};
    for (const [person, p] of Object.entries(overlay.people ?? {})) {
      const obj = objects.find(o => o.id === person);
      const id = (p.trainer?.id && trainers.has(p.trainer.id) ? p.trainer.id : null) ?? trainerOfConstant(region.folder, obj?.script);
      if (id) {
        personTrainer[person] = id;
        ids.add(id);
        if (p.script) scripts.set(p.script, id);
      }
    }
    const at = path.join(overlays, file);
    for (const [person, p] of Object.entries(overlay.people ?? {})) {
      const own = personTrainer[person] ? [personTrainer[person]] : null;
      for (const field of ['dialogueBefore', 'dialogueAfter']) {
        const line = p.trainer?.[field];
        if (line && own) note(at, line, rewrite(line, own, false), own);
      }
      for (const line of p.dialog ?? []) {
        const ids2 = own ?? [...ids, ...namedIn(region.id, line)];
        note(at, line, rewrite(line, ids2, !own), ids2);
      }
    }
    for (const line of Object.values(overlay.signs ?? {}))
      if (typeof line === 'string') note(at, line, rewrite(line, [...ids, ...namedIn(region.id, line)], true), [...ids]);
  }
}

// Phone calls: a contact's lines are their trainer's, rematch teams and all
const phoneTrainers = new Map(); // script name (lower case, with or without the region's name) -> ids
for (const region of regions) {
  const book = path.join(data, 'world', region.folder, 'phone.json');
  if (!fs.existsSync(book)) continue;
  for (const contact of read(book)) {
    if (!contact.trainer) continue;
    const ids = [...trainers.keys()].filter(id => id === contact.trainer || id.startsWith(contact.trainer + '_') && /_\d+$/.test(id.slice(contact.trainer.length)));
    phoneTrainers.set(contact.id.toLowerCase().replace(/_/g, ''), ids);
    phoneTrainers.set((region.id + contact.id).toLowerCase().replace(/_/g, ''), ids);
  }
}

// Scripts: a script's lines belong to whoever it battles; the rest of the place's file loosely to the place's trainers
const quoted = /(?:\b(?:say|text|ask|sayown)\s+)"((?:[^"\\]|\\.)*)"/g;
for (const file of fs.readdirSync(path.join(data, 'scripts'))) {
  const key = file.replace(/\.txt$/, '');
  const text = fs.readFileSync(path.join(data, 'scripts', file), 'utf8');
  const blocks = text.split(/^(?=script )/m);
  const placeIds = new Set(areaTrainers.get(key) ?? []);
  const scripts = personScript.get(key) ?? new Map();
  const blockIds = blocks.map(b => {
    const ids = new Set();
    const name = /^script (\S+)/.exec(b)?.[1];
    for (const m of b.matchAll(/^\s*battle\b[^\n]*/gm)) {
      for (const a of m[0].matchAll(/(?:as|with) "([a-z0-9_]+)"/g)) if (trainers.has(a[1])) ids.add(a[1]);
      if (/^\s*battle self\b(?! as)/.test(m[0]) && name && scripts.has(name)) ids.add(scripts.get(name));
    }
    if (key === 'phone' && name) for (const id of phoneTrainers.get(name.toLowerCase()) ?? []) ids.add(id);
    ids.forEach(id => placeIds.add(id));
    return ids;
  });
  const region = areaRegion.get(key) ?? (placeIds.size > 0 ? trainers.get([...placeIds][0]).region : null);
  if (!region) continue;
  const at = path.join(data, 'scripts', file);
  blocks.forEach((b, i) => {
    for (const m of b.matchAll(quoted)) {
      const own = blockIds[i].size > 0;
      const ids = own ? [...blockIds[i]] : [...placeIds, ...namedIn(region, m[1])];
      note(at, m[1], rewrite(m[1], ids, !own), ids);
    }
  });
}

// Lines in battle
const battleLines = read(path.join(data, 'trainer-lines.json'));
for (const [key, set] of Object.entries(battleLines)) {
  const ids = [...trainers.keys()].filter(id => key.endsWith('*') ? id.startsWith(key.slice(0, -1)) : id === key);
  if (ids.length === 0) continue;
  for (const cue of Object.values(set)) note(path.join(data, 'trainer-lines.json'), cue.text, rewrite(cue.text, ids, false), ids);
}

// ---- the report
const rel = f => path.relative(root, f).replaceAll('\\', '/');
let changing = 0;
for (const region of regions) {
  const list = [...trainers].filter(([, t]) => t.region === region.id && t.changes.size > 0);
  changing += list.length;
  console.log(`== ${region.id}: ${list.length} trainers whose Pokémon change`);
  for (const [id, t] of list) console.log(`  ${id}: ${[...t.changes].map(([a, b]) => `${a} → ${b}`).join(', ')}`);
}
console.log(`\n== ${edits.length} lines that name a Pokémon their trainer's team no longer has`);
for (const e of edits) console.log(`  ${rel(e.file)} [${e.ids.join(' ')}] ${e.found.join(', ')}\n    - ${e.old}\n    + ${e.new}`);
console.log(`\n== ${keptLines.length} lines kept as they are (tools/dev/grown-names.json)`);
for (const k of keptLines) console.log(`  ${rel(k.file)} ${k.found.join(', ')}: ${k.line}\n    (${k.reason})`);

if (!write) process.exit(0);

// ---- the rewrite: English in place, and Romanian's translation of the same line
const jsonString = s => JSON.stringify(s).slice(1, -1);
const byFile = new Map();
for (const e of edits) (byFile.get(e.file) ?? byFile.set(e.file, []).get(e.file)).push(e);
for (const [file, list] of byFile) {
  let text = fs.readFileSync(file, 'utf8');
  for (const e of list) {
    const from = file.endsWith('.txt') ? e.old : jsonString(e.old), to = file.endsWith('.txt') ? e.new : jsonString(e.new);
    text = text.split(`"${from}"`).join(`"${to}"`);
  }
  fs.writeFileSync(file, text);
}
const ro = path.join(data, 'text', 'ro');
const roFiles = [];
const walk = dir => { for (const f of fs.readdirSync(dir)) { const p = path.join(dir, f); fs.statSync(p).isDirectory() ? walk(p) : p.endsWith('.json') && roFiles.push(p); } };
walk(ro);
let romanian = 0;
const renames = e => e.found.map(pair => pair.split(' → '));
const translate = (said, e) => renames(e).reduce((s, [old, into]) => s.replace(nameRe(old), into), said);
for (const file of roFiles) {
  const text = fs.readFileSync(file, 'utf8');
  if (!edits.some(e => text.includes(`"${jsonString(e.old)}"`))) continue;
  const crlf = text.includes('\r\n');
  const eol = crlf ? '\r\n' : '\n';
  const json = JSON.parse(text.replace(/^﻿/, ''));
  const back = o => JSON.stringify(o, null, 2).replace(/\n/g, eol);
  if (back(json) + eol !== text && back(json) !== text) { console.log(`!! ${rel(file)} isn't written as this script writes JSON: left for TextTool`); continue; }
  let changed = false;
  // A translation is { en: [...], text: [...] }: its "en" is brought up to date and the species named in its text
  // (Romanian keeps the species' names as they are) changed with it. data.json is English to Romanian, one to one
  const visit = o => {
    if (Array.isArray(o)) { o.forEach(visit); return; }
    if (!o || typeof o !== 'object') return;
    if (Array.isArray(o.en) && Array.isArray(o.text))
      o.en.forEach((said, i) => {
        const e = edits.find(x => x.old === said);
        if (!e) return;
        o.en[i] = e.new;
        if (typeof o.text[i] === 'string') o.text[i] = translate(o.text[i], e);
        changed = true;
      });
    Object.values(o).forEach(visit);
  };
  let out = json;
  if (path.basename(file) === 'data.json') {
    out = {};
    for (const [en, said] of Object.entries(json)) {
      const e = edits.find(x => x.old === en);
      if (e) changed = true;
      out[e ? e.new : en] = e ? translate(said, e) : said;
    }
  }
  else visit(json);
  if (changed) { fs.writeFileSync(file, back(out) + eol); romanian++; }
}
console.log(`\nWrote ${byFile.size} English files and ${romanian} Romanian files.`);
