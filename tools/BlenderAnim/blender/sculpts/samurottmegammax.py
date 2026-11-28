# Mega Dynamax Samurott (a form of our own, plan 06 · R20c) on the skeleton and clips of the game's own export of the
# form (sculptkit.py): Samurott-Mega's own script, whole and unchanged (sibling("samurottmega")), with the form's
# additions laid on its kit just before it is meshed:
#   blender --background --factory-startup --python sculpts/samurottmegammax.py -- <the export's Samurott-Mega-Mmax.glb> <out.glb>
# The design is docs/art/reference/gemini/megadynamax/Mega-Dynamax-Samurott.jpg: both seamitars blazing cyan along the
# forelegs and grown longer into points of glowing crystal past the shoulders (its largest addition), the Key Stone's
# emblem on the kabuto's brow where the crescent rises, and clouds of Max energy rolling about the four paws (the
# picture has none over the head). The sculptor's list (PokemonModels.MegaMax2.cs, SamurottMmax) is the same.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, ring_band, slab, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#4EE2EC"
BLADE, BLAZE = "#A8F8FF", "#4EE2EC"                       # the seamitar's pale cyan steel and its blaze of light
STONE, RING = "#FFD866", "#4EE2EC"                        # the emblem: a gold disc in a cyan ring, as the picture's


def keep_clear(c, s, keep):
    P, R = keep
    return float(np.min(np.linalg.norm(P - c, axis=1) - R)) - s


def start(c, rx, ry, a, H, keep, gap):
    """How far out (a share of the ring) a clump at angle a must stand to clear keep."""
    s, f = H * 0.072, 1.0
    for _ in range(40):
        p = np.array([c[0] + rx * f * math.cos(a), c[1] + ry * f * math.sin(a), s * 0.75])
        if keep_clear(p, s, keep) >= gap:
            break
        f += 0.02
    return f


def clouds(k, c, rx, ry, H, n, keep, gap, seed, phase=0.0, most=1.25):
    """A ring of fused mounds on the ground: clumps of four round puffs, each pushed out until it stands clear of keep."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP)
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n + phase
        f0 = start(c, rx, ry, a, H, keep, gap)
        if f0 > most:
            continue
        for j in range(4):
            s = H * (0.072, 0.056, 0.046, 0.036)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.15, -0.14, 0.04)[j] * (0.33 / max(rx, ry))
            f = f0 + (0.0, 0.04, 0.03, -0.045)[j]
            z = s * (0.75, 0.9, 0.8, 1.9)[j]
            for _ in range(40):
                p = np.array([c[0] + rx * f * math.cos(t), c[1] + ry * f * math.sin(t), z])
                if keep_clear(p, s, keep) >= gap:
                    break
                f += 0.02
            if f > most + 0.05:
                continue
            col = cols[i % 3] if j != 2 else cols[(i + 1) % 3]
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.9), sub=3), col, "root")
            if j == 0 and i % 3 == 0:
                q = p + np.array([0.0, 0.0, s * 1.0]) + np.array([math.cos(t), math.sin(t), 0]) * s * 0.5
                k.add(f"curl{i}", outward(ell(q, (s * 0.42, s * 0.42, s * 0.36), sub=3)), CYAN, "root", fuse=False,
                      glow=CYAN)


def emblem(k, c, d, r, bone):
    """The Key Stone's emblem: a glowing disc in a glowing ring, a hair off the body."""
    k.add("emblem", lathe(c, d, [(0, 0), (r, 0), (r, 0.006), (0, 0.006)]), STONE, bone, fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.003, d, r * 1.25, 0.008, thick=r * 0.32), RING, bone, fuse=False,
          glow=RING)


def maxed(k):
    H = 0.91
    # (a) the seamitars set blazing: their steel turned to glowing pale cyan, each carried on into a longer point of
    # cyan crystal in the same plane (the frame is samurottmega.py's seamitar())
    for p in k.parts:
        if p["name"].startswith("seamitar"):
            p["color"] = np.array(sculpt_srgb(BLADE)); p["glow"] = BLADE
    for sx in (1, -1):
        side = "RL"[sx < 0]
        root = np.array([0.205 * sx, -0.235, 0.13])
        tip = np.array([0.255 * sx, -0.02, 0.56])
        d = tip - root - (0, 0, 0.05)
        L = float(np.linalg.norm(d)); du = d / L
        face = unit(sx, -0.55, 0.0)
        ez = np.cross(face, du)
        o = root + (0, 0, 0.05) + du * (0.82 * L)
        ext = [(0.0, -0.024), (0.08, -0.03), (0.16, -0.022), (0.24, 0.022), (0.17, 0.02), (0.07, 0.012), (0.0, 0.01)]
        k.add(f"blaze{side}", slab(ext, o, du, ez, 0.026), BLAZE, f"leg{side}", fuse=False, glow=BLAZE)
        # the blaze down the blade's back edge to the guard: a glowing bar along the steel, a little out from it
        k.add(f"edge{side}", slab([(0.0, -0.034), (0.85 * L, -0.032), (0.85 * L, -0.02), (0.0, -0.02)],
                                  root + (0, 0, 0.05), du, ez, 0.03), BLAZE, f"leg{side}", fuse=False, glow=BLAZE)
    # (c) the emblem on the kabuto's brow, in front of the crescent's gold disc
    emblem(k, np.array([0.0, -0.330, 0.603]), unit(0, -1, 0.12), 0.026, "head")
    # (b) the clouds about the four paws, kept clear of the legs, paws, sheaths and claws
    pts, rads = [], []
    for sx in (1, -1):
        m = np.array([sx, 1, 1.0])
        for P, R in (((0.11, -0.15, 0.22), 0.07), ((0.165, -0.235, 0.11), 0.054), ((0.195, -0.28, 0.035), 0.05),
                     ((0.2, -0.33, 0.02), 0.05), ((0.205, -0.235, 0.1), 0.04), ((0.12, 0.16, 0.18), 0.068),
                     ((0.165, 0.235, 0.085), 0.053), ((0.185, 0.265, 0.03), 0.05), ((0.19, 0.3, 0.02), 0.05),
                     ((0.0, 0.04, 0.12), 0.06)):
            pts.append(np.array(P) * m); rads.append(R)
    clouds(k, (0.0, -0.02), 0.24, 0.41, H, 16, (np.array(pts), np.array(rads)), 0.02, 5, phase=0.2, most=1.5)


from sculptkit import srgb as sculpt_srgb

m = sibling("samurottmega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m.samurottmega(*args())
