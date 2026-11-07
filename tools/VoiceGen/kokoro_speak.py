"""Kokoro-82M (Apache-2.0) as VoiceTool generate runs it (plan 26 · V3 to V5; tools/VoiceGen/README.md).

One take of one line: the text, the voice's design from Data/voice/cast.json (a blend of Kokoro's built-in voices, a
speed and a pitch), the line's direction and a seed, written as a mono WAV. With --serve it stays up and takes a JSON
line per take on stdin (the same fields as the switches), answering "ok" or "error: ..." a line each on stdout, so the
model is loaded once for a whole run.

Run it only with the engine's own Python in the cache (setup-kokoro.sh), always with -I. Nothing here downloads
anything; the model and the voices are the files setup-kokoro.sh fetched into --models.
"""

import argparse
import json
import os
import re
import sys
import wave

import numpy as np
import onnxruntime as rt
import espeakng_loader
from phonemizer.backend.espeak.wrapper import EspeakWrapper
from phonemizer.backend import EspeakBackend

RATE = 24000
MAX_PHONEMES = 510

# How a direction moves the voice: a share of its speed and of its pitch. Kokoro has no emotion control, so the
# delivery is carried by pace and register alone; the text's own punctuation does the rest.
DIRECTIONS = {
    "calm": (1.00, 1.00),
    "excited": (1.08, 1.03),
    "shouting": (1.10, 1.05),
    "frustrated": (1.05, 1.01),
    "quiet": (0.92, 0.98),
    "whispering": (0.90, 0.97),
    "sad": (0.90, 0.97),
    "smug": (0.96, 0.99),
    "laughing": (1.06, 1.03),
}

# A seed's take: the same line a little slower, as written, or a little faster (Kokoro itself draws nothing, so the
# three takes differ only by this; the bake keeps the one whose pace reads best).
TAKE_SPEED = (1.00, 0.96, 1.04)


class Engine:
    def __init__(self, models, cast_path, lexicon_path):
        EspeakWrapper.set_data_path(espeakng_loader.get_data_path())
        EspeakWrapper.set_library(espeakng_loader.get_library_path())
        options = rt.SessionOptions()
        options.intra_op_num_threads = int(os.environ.get("KOKORO_THREADS", "8"))
        self.session = rt.InferenceSession(os.path.join(models, "model.onnx"), sess_options=options,
                                           providers=["CPUExecutionProvider"])
        with open(os.path.join(models, "config.json"), encoding="utf-8") as f:
            self.vocab = json.load(f)["vocab"]
        self.voice_folder = os.path.join(models, "voices")
        self.styles = {}
        with open(cast_path, encoding="utf-8") as f:
            self.cast = json.load(f)["voices"]
        self.backends = {}
        self.lexicon = {}
        if lexicon_path and os.path.exists(lexicon_path):
            with open(lexicon_path, encoding="utf-8") as f:
                entries = json.load(f)["entries"]
            for e in entries:
                self.lexicon[e["name"]] = e["say"]
        self.names = sorted(self.lexicon, key=len, reverse=True)
        self.name_ipa = {}

    def backend(self, lang):
        if lang not in self.backends:
            self.backends[lang] = EspeakBackend(lang, preserve_punctuation=True, with_stress=True)
        return self.backends[lang]

    def style(self, name):
        if name not in self.styles:
            raw = np.fromfile(os.path.join(self.voice_folder, name + ".bin"), dtype=np.float32)
            self.styles[name] = raw.reshape(-1, 1, 256)
        return self.styles[name]

    def blend(self, spec):
        """'am_puck:0.7,am_echo:0.3' as one style table: the weighted mean of the voices named."""
        parts = []
        for piece in spec.split(","):
            name, _, weight = piece.strip().partition(":")
            parts.append((name, float(weight) if weight else 1.0))
        total = sum(w for _, w in parts)
        return sum(self.style(n) * (w / total) for n, w in parts)

    def design(self, voice):
        entry = self.cast.get(voice)
        if entry is None or "design" not in entry:
            raise ValueError(f"{voice} has no design in cast.json")
        d = entry["design"]
        if d.get("engine", "kokoro-82m") != "kokoro-82m":
            raise ValueError(f"{voice}'s design is for {d.get('engine')}")
        return d

    def name_phonemes(self, name, lang):
        """A name of the lexicon as phonemes: its respelling's syllables each said by espeak, the stress where the capitals are."""
        key = (name, lang)
        if key not in self.name_ipa:
            words = []
            for word in self.lexicon[name].split(" "):
                syllables = word.split("-")
                said = self.backend(lang).phonemize([s.lower() for s in syllables], strip=True)
                out = ""
                for s, p in zip(syllables, said):
                    p = p.replace("ˈ", "").replace("ˌ", "").strip()
                    out += ("ˈ" + p) if s.isupper() and any(c.isalpha() for c in s) else p
                words.append(out)
            self.name_ipa[key] = " ".join(words)
        return self.name_ipa[key]

    def phonemize(self, text, lang):
        pieces = []
        if self.names:
            pattern = r"(?<![\w])(" + "|".join(re.escape(n) for n in self.names) + r")(?![\w])"
            at = 0
            for m in re.finditer(pattern, text):
                pieces.append(("text", text[at:m.start()]))
                pieces.append(("name", m.group(1)))
                at = m.end()
            pieces.append(("text", text[at:]))
        else:
            pieces = [("text", text)]
        out = []
        for kind, value in pieces:
            if kind == "name":
                out.append(self.name_phonemes(value, lang))
            elif value.strip():
                lead = " " if value[0].isspace() else ""
                trail = " " if value[-1].isspace() else ""
                said = self.backend(lang).phonemize([value], strip=True)[0]
                out.append(lead + said + trail)
            else:
                out.append(value)
        phonemes = "".join(out)
        phonemes = re.sub(r"\s+", " ", phonemes)
        return "".join(c for c in phonemes if c in self.vocab).strip()

    def batches(self, phonemes):
        parts = re.split(r"([.,!?;])", phonemes)
        batches, current = [], ""
        for part in parts:
            part = part.strip()
            if not part:
                continue
            if len(current) + len(part) + 1 >= MAX_PHONEMES:
                batches.append(current.strip())
                current = part
            elif part in ".,!?;":
                current += part
            else:
                current += (" " if current else "") + part
        if current:
            batches.append(current.strip())
        return batches

    def speak(self, text, voice, direction, seed):
        d = self.design(voice)
        style = self.blend(d["blend"])
        lang = d.get("accent", "en-us")
        speed_d, pitch_d = DIRECTIONS.get(direction, (1.0, 1.0))
        pitch = float(d.get("pitch", 1.0)) * pitch_d
        speed = float(d.get("speed", 1.0)) * speed_d * TAKE_SPEED[seed % len(TAKE_SPEED)]
        # A pitch is the take played faster than it was made (its WAV is stamped at a higher rate), so it is made slower
        # by the same share to keep its pace.
        model_speed = max(0.5, min(2.0, speed / pitch))
        audio = []
        for batch in self.batches(self.phonemize(text, lang)):
            tokens = [self.vocab[c] for c in batch if c in self.vocab][: MAX_PHONEMES - 2]
            ids = np.array([[0, *tokens, 0]], dtype=np.int64)
            ref = style[len(tokens)]
            out = self.session.run(None, {"input_ids": ids, "style": ref.astype(np.float32),
                                          "speed": np.array([model_speed], dtype=np.float32)})[0]
            audio.append(np.asarray(out, dtype=np.float32).reshape(-1))
            audio.append(np.zeros(int(RATE * 0.12), dtype=np.float32))
        if not audio:
            raise ValueError("nothing to say")
        samples = np.concatenate(audio[:-1])
        return samples, int(round(RATE * pitch))


def write_wav(path, samples, rate):
    # Kokoro's output can overshoot full scale; brought under it here, so the bake's clip check sees the take, not the 16 bits
    peak = float(np.max(np.abs(samples))) if len(samples) else 0.0
    data = samples * (0.89 / peak) if peak > 0.89 else samples
    pcm = (data * 32767.0).astype("<i2").tobytes()
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(pcm)


def take(engine, job):
    with open(job["textFile"], encoding="utf-8") as f:
        text = f.read().strip()
    samples, rate = engine.speak(text, job["voice"], job.get("direction", "calm"), int(job.get("seed", 0)))
    write_wav(job["out"], samples, rate)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--models", required=True)
    p.add_argument("--cast", required=True)
    p.add_argument("--lexicon")
    p.add_argument("--serve", action="store_true")
    p.add_argument("--text-file")
    p.add_argument("--out")
    p.add_argument("--voice")
    p.add_argument("--direction", default="calm")
    p.add_argument("--seed", default="0")
    a = p.parse_args()
    engine = Engine(a.models, a.cast, a.lexicon)
    if not a.serve:
        take(engine, {"textFile": a.text_file, "out": a.out, "voice": a.voice, "direction": a.direction, "seed": a.seed})
        return 0
    print("ready", flush=True)
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        try:
            take(engine, json.loads(line))
            print("ok", flush=True)
        except Exception as e:  # one line's failure is that line's, never the run's
            print("error: " + str(e).replace("\n", " "), flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
