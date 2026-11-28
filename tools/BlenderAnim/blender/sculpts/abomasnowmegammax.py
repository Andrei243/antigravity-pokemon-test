# Abomasnow's Mega Dynamax form sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/abomasnowmegammax.py -- <the export's Abomasnow-Mega-Mmax.glb> <out.glb>
# Abomasnow-Mega's own script, whole and unchanged (abomasnowmega.py, which builds as it is read), with what the Mega
# Dynamax form lays over it added to its kit just before the body is meshed. From the picture
# (Mega-Dynamax-Abomasnow.jpg), largest first: a crown of great crystals of glowing ice rising behind the head and over
# the shoulders, leaning back, with two small snow-capped pines standing among them; icicles of glowing ice hanging
# from the boughs over the arms; the clouds of Max energy, crimson and violet with cyan curls, in a ring on the ground
# about the feet and hands, and a wisp curling by each shoulder; the Key Stone's emblem, a pale violet star-disc in a
# cyan ring, glowing on the chest under the mouth. The snow, the boughs, the face and the eyes are the Mega's; the
# picture's cyan edging of the frost and its taller crown (a quarter of the body's height again) are not followed, the
# crown kept under 1.07 so that the game draws the form no more than a tenth smaller than its Mega.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, ring_band, unit, sibling

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
ICE, DEEP_ICE = "#9EE6F8", "#54ACDE"          # the sculptor's Ice and DeepIce
PINE, SNOWCAP = "#30644A", "#EEF4FA"
STONE, STONE_RING = "#C27CF4", "#5EE6F0"


def join(shapes):
    """Several closed shapes as one piece (one material a colour, as a glowing piece wants)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in t] for t in f]); off += len(v)
    return np.vstack(V), F


def clouds(k, cx, cy, rx, ry, n, size, seed, bone="root"):
    """The ground's ring of clouds: mounds of five round puffs of mixed sizes packed close (a big one, two lower at its
    sides, one behind, a small one riding on top), each mound one colour with one puff of another, a cyan curl on
    every other. The puffs are loose and coarse (the Mega's fused body is already some 50,000 vertices)."""
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


def crystal(root, tip, r):
    """A crystal of ice: a six-sided prism swelling a little before its point (abomasnowmega.py's way)."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root; d /= np.linalg.norm(d)
    a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a); b = np.cross(d, a)
    ring = [root + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
    mid = [root + (tip - root) * 0.66 + r * 1.06 * (math.cos(t) * a + math.sin(t) * b)
           for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
    return hull(ring + mid + [tip])


def maxed(k):
    """What the Mega Dynamax form lays over Abomasnow-Mega: called with the Mega's kit just before it is meshed."""
    # 1. The crown of ice crystals behind the head and over the shoulders, leaning back, roots in the snow's top
    #    (abomasnowmega.py's heap: centre (0, 0.02, 0.52), radii (0.29, 0.26, 0.29), so its top is at 0.81)
    light, deep = [], []
    CROWN = [  # root, tip, radius
        ((0.0, 0.12, 0.74), (0.0, 0.25, 1.065), 0.06),
        ((0.07, 0.1, 0.74), (0.13, 0.2, 1.0), 0.048),
        ((-0.07, 0.1, 0.74), (-0.14, 0.22, 0.99), 0.048),
        ((0.03, 0.17, 0.72), (0.06, 0.33, 0.97), 0.04),
        ((-0.04, 0.17, 0.72), (-0.06, 0.34, 0.95), 0.038),
        ((0.15, 0.08, 0.72), (0.24, 0.15, 0.95), 0.042),
        ((-0.15, 0.08, 0.72), (-0.25, 0.16, 0.94), 0.042),
        ((0.22, 0.04, 0.68), (0.34, 0.08, 0.87), 0.036),
        ((-0.22, 0.04, 0.68), (-0.35, 0.09, 0.88), 0.036),
        ((0.1, 0.16, 0.7), (0.17, 0.3, 0.9), 0.034),
        ((-0.11, 0.16, 0.7), (-0.18, 0.31, 0.91), 0.034),
    ]
    for i, (root, tip, r) in enumerate(CROWN):
        (deep if i % 3 == 1 else light).append(crystal(root, tip, r))
    k.add("crownice", join(light), ICE, "body", fuse=False, glow=ICE, surface="shell")
    k.add("crowndeep", join(deep), DEEP_ICE, "body", fuse=False, glow=DEEP_ICE, surface="shell")
    # the two small pines among them, their tips under snow
    pines, caps = [], []
    for s in (1, -1):
        base = np.array([0.1 * s, 0.07, 0.76])
        tip = np.array([0.105 * s, 0.09, 1.0])
        for h0, h1, r in ((0.0, 0.55, 0.06), (0.35, 0.85, 0.045)):
            ring = [base + (tip - base) * h0 + r * np.array([math.cos(t), math.sin(t), 0])
                    for t in np.linspace(0, 2 * math.pi, 7, endpoint=False)]
            pines.append(hull(ring + [base + (tip - base) * h1]))
        caps.append(hull([tip + np.array([0.02 * math.cos(t), 0.02 * math.sin(t), -0.045])
                          for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)] + [tip + np.array([0, 0, 0.004])]))
    k.add("pines", join(pines), PINE, "body", fuse=False, surface="leaf")
    k.add("pinesnow", join(caps), SNOWCAP, "body", fuse=False, surface="leaf")

    # 2. Icicles hanging from the boughs over the arms, behind the arm so that it swings clear of them
    icicles = []
    for s in (1, -1):
        for i, (x, y, z, L) in enumerate(((0.27, 0.1, 0.6, 0.15), (0.33, 0.09, 0.57, 0.12), (0.39, 0.08, 0.53, 0.1),
                                          (0.3, 0.16, 0.62, 0.09))):
            root = np.array([x * s, y, z])
            icicles.append(crystal(root, root + np.array([0.01 * s, 0.0, -L]), 0.018 if i != 3 else 0.015))
    k.add("icicles", join(icicles), ICE, "body", fuse=False, glow=ICE, surface="shell")

    # 3. The clouds: a ring on the ground about the feet and the hands on the ground beside them; a wisp curling by
    #    each shoulder
    clouds(k, 0.0, 0.0, 0.46, 0.32, 8, 0.066, 9)
    wisps = {CRIMSON: [], VIOLET: []}
    for s in (1, -1):
        c = np.array([0.45 * s, 0.14, 0.66])
        wisps[CRIMSON].append(ell(c, (0.034, 0.032, 0.03), sub=3))
        wisps[VIOLET].append(ell(c + np.array([0.01 * s, 0.025, 0.035]), (0.025, 0.024, 0.022), sub=3))
        wisps[CRIMSON].append(ell(c + np.array([-0.005 * s, 0.05, 0.065]), (0.018, 0.017, 0.016), sub=2))
    for col, shapes in wisps.items():
        k.add(f"wisp{col[1:]}", join(shapes), col, "body", fuse=False)

    # 4. The Key Stone's emblem on the chest, under the mouth's beak (abomasnowmega.py's mouth ends at z 0.505),
    #    standing before the cards of snow there
    c = np.array([0.0, -0.33, 0.445])
    d = unit(0, -1, 0.12)
    k.add("emblem", lathe(c, d, [(0, 0), (0.034, 0), (0.034, 0.01), (0, 0.01)], seg=4), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.005, d, 0.045, 0.012, thick=0.01), STONE_RING, "body", fuse=False,
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

sibling("abomasnowmega")        # builds as it is read, with this script's own arguments
