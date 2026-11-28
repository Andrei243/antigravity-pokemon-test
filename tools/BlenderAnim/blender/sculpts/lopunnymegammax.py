# Lopunny's Mega Dynamax form sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lopunnymegammax.py -- <the export's Lopunny-Mega-Mmax.glb> <out.glb>
# Lopunny-Mega's own script, whole and unchanged (lopunnymega.py, which builds as it is read), with what the Mega
# Dynamax form lays over it added to its kit just before the body is meshed. From the picture
# (Mega-Dynamax-Lopunny.jpg), largest first: long ribbons of pink light with cream streaks streaming from the ears'
# cream tops, back over the head and out to the side; the Mega's cream fleece at wrists, ankles and ear joints turned
# to pink light-fluff; the clouds of Max energy, crimson and violet with cyan curls, in a ring on the ground about the
# feet; the Key Stone's emblem, a pale gem in a pink ring, glowing on the chest above the fists. The body, the ears'
# shape, the face and the eyes are the Mega's. The picture's flame-like collar round the gem and the lightning in the
# clouds are left out.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, blade, lathe, ring_band, unit, sibling

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
FLUFF = "#F99AC6"                       # the fleece turned to pink light-fluff
RIBBON_PINK, RIBBON_CREAM = "#FC86BE", "#FFE6A8"
STONE, STONE_RING = "#E6FFF0", "#FF6EB4"


def join(shapes):
    """Several closed shapes as one piece (one material a colour, as a glowing piece wants)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in t] for t in f]); off += len(v)
    return np.vstack(V), F


def clouds(k, cx, cy, rx, ry, n, size, seed, bone="root"):
    """The ground's ring of clouds: mounds of five round puffs of mixed sizes packed close (a big one, two lower at its
    sides, one behind, a small one riding on top), each mound one colour with one puff of another, a cyan curl on
    every other. The puffs are loose and coarse (the Mega's fused body is already some 59,000 vertices)."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP)
    alt = (VIOLET, DVIOLET, CRIMSON)
    PUFFS = ((1.0, 0.0, 1.0, 0.95), (0.74, 0.2, 1.02, 0.9), (0.7, -0.2, 0.99, 0.9), (0.72, 0.04, 1.12, 0.9),
             (0.5, -0.03, 0.98, 1.75))
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        for j, (f, da, sc, zf) in enumerate(PUFFS):
            s = size * f * (1 + 0.1 * rnd.uniform(-1, 1))
            t = a + da * 2 * math.pi / n
            p = np.array([cx + rx * sc * math.cos(t), cy + ry * sc * math.sin(t), s * zf])
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.88), sub=2),
                  alt[i % 3] if j == (1 if i % 2 else 2) else cols[i % 3], bone, fuse=False)
        if i % 2 == 0:
            q = np.array([cx + rx * 1.06 * math.cos(a + 0.12), cy + ry * 1.06 * math.sin(a + 0.12), size * 1.45])
            curls.append(ell(q, (size * 0.3, size * 0.3, size * 0.24), sub=2))
    k.add("curls", join(curls), CYAN, bone, fuse=False, glow=CYAN)


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def maxed(k):
    """What the Mega Dynamax form lays over Lopunny-Mega: called with the Mega's kit just before it is meshed."""
    # 1. Ribbons of pink light streaming from the ears' cream tops (lopunnymega.py's petals stand at x 0.062, z 1.01
    #    on the ear bones), back over the head and out to the +X side, where the game's front camera sees them; each a
    #    broad pink ribbon with a cream streak along its top, flat across the way the cameras look
    RIBBONS = {
        "R": [(0.07, 0.02, 1.04), (0.15, 0.1, 1.07), (0.27, 0.17, 1.045), (0.39, 0.21, 0.98), (0.5, 0.24, 0.94),
              (0.6, 0.27, 0.97), (0.66, 0.29, 1.01)],
        "L": [(-0.07, 0.02, 1.04), (-0.05, 0.13, 1.075), (0.06, 0.22, 1.065), (0.2, 0.29, 1.01), (0.34, 0.33, 0.95),
              (0.46, 0.35, 0.93), (0.55, 0.37, 0.96)],
    }
    flat = unit(0, 1, 0.25)
    for side, pts in RIBBONS.items():
        pts = np.array(pts)
        k.add(f"ribbon{side}", blade(pts, [0.026, 0.042, 0.052, 0.054, 0.048, 0.036, 0.02], flat, 0.62),
              RIBBON_PINK, f"ear{side}", fuse=False, glow=RIBBON_PINK)
        k.add(f"streak{side}", blade(pts[1:] + np.array([0, -0.012, 0.036]), [0.02, 0.026, 0.026, 0.022, 0.016, 0.01],
                                     flat, 0.62), RIBBON_CREAM, f"ear{side}", fuse=False, glow=RIBBON_CREAM)

    # 2. The cream fleece at wrists, ankles and ear joints turned to pink light-fluff (the Mega's own fused parts,
    #    recoloured only)
    for p in k.parts:
        if p["name"].startswith(("cuff", "anklefleece", "earfleece")):
            p["color"] = sculptkit.srgb(FLUFF)

    # 3. The clouds: a ring on the ground about the feet, clear of the ankles' fleece
    clouds(k, 0.0, -0.01, 0.34, 0.28, 7, 0.068, 3)

    # 4. The Key Stone's emblem on the chest, just above the fists held before it (the chest's front, lopunnymega.py's
    #    ell (0, 0.005, 0.625) of radii (0.082, 0.064, 0.077), is at y -0.05 at this height)
    c = np.array([0.0, -0.057, 0.672])
    d = unit(0, -1, 0.25)
    k.add("emblem", lathe(c, d, [(0, 0), (0.022, 0), (0.022, 0.008), (0, 0.008)], seg=6), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.029, 0.009, thick=0.007), STONE_RING, "body", fuse=False,
          glow=STONE_RING)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    first = not _laid
    if first:                   # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    ob = _build(self, *a, **kw)
    if first:
        board(ob)
    return ob
Kit.build = build

sibling("lopunnymega")          # builds as it is read, with this script's own arguments
