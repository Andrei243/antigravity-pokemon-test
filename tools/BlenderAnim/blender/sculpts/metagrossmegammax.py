# Metagross's Mega Dynamax form sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/metagrossmegammax.py -- <the export's Metagross-Mega-Mmax.glb> <out.glb>
# Metagross-Mega's own script, whole and unchanged (metagrossmega.py, called), with what the Mega Dynamax form lays over
# it added to its kit just before the body is meshed. From the picture (Mega-Dynamax-Metagross.jpg), largest first:
# a ring of six-sided crystal plates, cyan, violet and silver, glowing, turning round the body between and about its
# four fists; the clouds of Max energy, crimson and violet with cyan curls, in a ring on the ground under the body
# where the front fists rest in them, and a broken ring of crimson puffs floating over the body; the Key Stone's
# emblem glowing at the crossing of the gold X, a pale star-disc in a cyan ring. The body, the fists, the X and the
# eyes are the Mega's. The sculptor's crystal claws on the fists are not in the picture and are left out.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
PLATE_CYAN, PLATE_VIOLET, PLATE_SILVER = "#6EE2EE", "#AA6EEC", "#D2DAEA"
STONE, STONE_RING = "#FFF2C8", "#5EE6F0"


def join(shapes):
    """Several closed shapes as one piece (one material a colour, as a glowing piece wants)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in t] for t in f]); off += len(v)
    return np.vstack(V), F


def clouds(k, cx, cy, rx, ry, n, size, seed, bone="root"):
    """The ground's ring of clouds: mounds of five round puffs of mixed sizes packed close (a big one, two lower at its
    sides, one behind, a small one riding on top), each mound one colour with one puff of another, a cyan curl on
    every other. The puffs are loose pieces (fused at the Mega's voxel of 0.0035 they would be far too many)."""
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
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.88), sub=3 if j < 4 else 2),
                  alt[i % 3] if j == (1 if i % 2 else 2) else cols[i % 3], bone, fuse=False)
        if i % 2 == 0:
            q = np.array([cx + rx * 1.06 * math.cos(a + 0.12), cy + ry * 1.06 * math.sin(a + 0.12), size * 1.45])
            curls.append(ell(q, (size * 0.3, size * 0.3, size * 0.24), sub=3))
    k.add("curls", join(curls), CYAN, bone, fuse=False, glow=CYAN)


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def near_fist(p, clear):
    """Whether p comes within `clear` of a fist's drum (the Mega's FISTS, both sides)."""
    for name, (root, at, d, L, r) in m.FISTS.items():
        for s in (1, -1):
            a = at * np.array([s, 1, 1]); dd = d * np.array([s, 1, 1])
            t = np.clip((p - a) @ dd, -0.05, L + 0.06)
            if np.linalg.norm(p - (a + dd * t)) < r * 1.1 + clear:
                return True
    return False


def maxed(k):
    """What the Mega Dynamax form lays over Metagross-Mega: called with the Mega's kit just before it is meshed."""
    C = m.C
    # 1. The ring of six-sided crystal plates turning round the body, tilted, each plate's face turned out from the
    #    body's middle; a plate that would cut into a fist is left out
    plates = {PLATE_CYAN: [], PLATE_VIOLET: [], PLATE_SILVER: []}
    cols = (PLATE_CYAN, PLATE_VIOLET, PLATE_SILVER, PLATE_CYAN, PLATE_VIOLET)
    n = 26
    for i in range(n):
        a = 2 * math.pi * i / n + 0.2
        p = C + np.array([0.47 * math.cos(a), 0.3 * math.sin(a), 0.2 * math.sin(a + 0.9) + 0.05 * math.sin(3 * a)])
        r = 0.054 + 0.012 * math.sin(2.3 * i)
        if near_fist(p, r + 0.01) or abs(p[0]) > 0.5 - r or (p[1] < -0.12 and abs(p[0]) < 0.3):
            continue
        out_ = p - C; out_[2] *= 0.3; out_ /= np.linalg.norm(out_)
        d = unit(*(out_ + np.array([0.4 * math.cos(1.7 * i), 0, 0.5 * math.sin(1.3 * i)])))
        plates[cols[i % 5]].append(lathe(p - d * 0.008, d, [(0, 0), (r, 0), (r, 0.016), (0, 0.016)], seg=6))
    # a few more drifting higher, between the raised hind fists, as the picture's arc rises over the body
    for i, (x, y, z, col) in enumerate(((0.13, 0.06, 0.72, PLATE_CYAN), (-0.14, 0.1, 0.7, PLATE_VIOLET),
                                         (0.02, 0.17, 0.66, PLATE_SILVER), (0.05, -0.02, 0.79, PLATE_VIOLET),
                                         (-0.06, -0.04, 0.76, PLATE_CYAN))):
        p = np.array([x, y, z]); r = 0.046 - 0.004 * (i % 2)
        if near_fist(p, r + 0.01):
            continue
        d = unit(0.5 * math.cos(2.1 * i), 0.6 + 0.3 * math.sin(i), 0.6)
        plates[col].append(lathe(p - d * 0.008, d, [(0, 0), (r, 0), (r, 0.016), (0, 0.016)], seg=6))
    for col, shapes in plates.items():
        if shapes:
            k.add(f"plates{col[1:]}", join(shapes), col, "body", fuse=False, glow=col, surface="shell")
    print("KIT: plates", sum(len(s) for s in plates.values()))

    # 2. The clouds: a ring on the ground under the body, the front fists' talons resting in it; a broken ring of
    #    crimson puffs floating over the body, between the raised hind fists
    clouds(k, 0.0, 0.0, 0.34, 0.27, 8, 0.062, 5)
    top = np.array([0.0, 0.02, 0.86])
    for i, a in enumerate((15, 70, 130, 200, 250, 310)):
        t = math.radians(a)
        p = top + np.array([0.16 * math.cos(t), 0.12 * math.sin(t), 0.012 * math.sin(3 * t)])
        s = (0.03, 0.024, 0.028, 0.022, 0.03, 0.024)[i]
        k.add(f"halo{i}", ell(p, (s, s, s * 0.82), sub=3), CRIMSON if i % 3 else DEEP, "body", fuse=False)
        k.add(f"halob{i}", ell(p + np.array([0.016 * math.cos(t + 1.2), 0.016 * math.sin(t + 1.2), 0.008]),
                                (s * 0.7, s * 0.7, s * 0.6), sub=2), CRIMSON, "body", fuse=False)

    # 3. The Key Stone's emblem at the crossing of the gold X, standing just before it, clear of the eyes
    p, nrm = m.on_body(0.0, C[2] - 0.005)
    c = p + nrm * 0.026
    k.add("emblem", lathe(c, nrm, [(0, 0), (0.024, 0), (0.024, 0.007), (0, 0.007)], seg=4), STONE, "head",
          fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + nrm * 0.003, nrm, 0.031, 0.008, thick=0.007), STONE_RING, "head", fuse=False,
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

m = sibling("metagrossmega")
m.metagross_mega(*args())
