// The lines a Gemini run's log shows with a real error (no audio, or a take that failed its checks), not those
// left only because the day's requests or the credits ran out; those that the pack holds a take of since are left out.
// node tools/VoiceGen/failed-lines.mjs <log> <pack.json> <out.json>
import fs from 'node:fs';

const [logPath, packPath, outPath] = process.argv.slice(2);
const lines = fs.readFileSync(logPath, 'utf8').split(/\r?\n/);
const pack = JSON.parse(fs.readFileSync(packPath, 'utf8'));
const made = new Set(pack.lines.map(e => e.key));

const keyLine = /^((?:script|map|world|battle):\S+): (.*)$/;
const quota = /made nothing|said (429|503|500|402)|used up|^seed \d+ of|^the request failed/;
const found = new Map();   // key -> { reason, at }
const pending = [];        // keys of this voice's block, given their voice at its summary line
for (let i = 0; i < lines.length; i++) {
    const summary = /^(\S+): \d+ lines generated with/.exec(lines[i]);
    if (summary) { for (const k of pending.splice(0)) found.get(k).voice = summary[1]; continue; }
    const m = keyLine.exec(lines[i]);
    if (!m || quota.test(m[2])) continue;
    let reason = m[2];
    if (reason.startsWith('Gemini gave no audio')) {
        // The answer's own word for it, from the lines of its body that follow
        let why = 'no audio part';
        for (let j = i + 1; j < Math.min(lines.length, i + 30) && !keyLine.test(lines[j]); j++) {
            const r = /"(finishReason|blockReason)":\s*"([^"]+)"/.exec(lines[j]);
            if (r) { why = `${r[1]} ${r[2]}`; break; }
        }
        reason = `Gemini gave no audio (${why})`;
    }
    found.set(m[1], { key: m[1], voice: found.get(m[1])?.voice ?? null, reason, at: i + 1 });
    pending.push(m[1]);
}
const out = [...found.values()].filter(f => !made.has(f.key)).sort((a, b) => a.key.localeCompare(b.key));
fs.writeFileSync(outPath, JSON.stringify(out, null, 2) + '\n');
const byReason = {};
for (const f of out) { const r = f.reason.replace(/[\d.]+/g, 'N'); byReason[r] = (byReason[r] ?? 0) + 1; }
console.log(`${out.length} lines with errors and no take, written to ${outPath}`);
for (const [r, n] of Object.entries(byReason).sort((a, b) => b[1] - a[1])) console.log(`  ${n}  ${r}`);
