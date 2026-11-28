# Mega Dynamax Aerodactyl (a form of our own) on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aerodactylmegammax.py -- <the export's Aerodactyl-Mega-Mmax.glb> <out.glb>
#
# Mega Aerodactyl as aerodactylmega.py builds it, whole and unchanged in shape, with what the form lays over it, as its
# picture draws it: every spike of dark rock the Mega broke out in (at the wrists, along the arms, the back, the neck,
# the cheeks, the chin, the horn, the tail, the knees and the claws) turned to glowing amber crystal, the rock set into
# its chest and belly with them; a great cluster of amber crystals erupting from its back between the wings, the
# largest thing the form adds; the Key Stone's emblem on its belly, a pink ring of light with a cyan star in it; and
# the clouds of Max energy, a ring of crimson and violet mounds on the ground under the hovering body, winding in, with
# cyan curls and a bolt of lightning.
#
# The new pieces are a build of their own ("mmclouds", made just before the Mega's body is meshed, through the kit's
# build), so the Mega's fused surface, colours, weights and patches come out as aerodactylmega.py makes them; the
# spikes are recoloured where the Mega added them (their colour and glow only, never their shape).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, outward, axes, unit, srgb, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AMBER, DEEPAMBER, PALEAMBER = "#F8B036", "#D2741F", "#FFD27A"
RING, STAR = "#FF78B4", "#78DCFF"
ROCK = "#4C4858"                       # aerodactylmega.py's ROCK, the colour of what turns to crystal
TURNS = ("wristrock", "wspike", "aspike", "backspike", "neckspike", "rock", "horn", "cheek", "chin", "tspike", "knee",
         "toe")


def crystal(k, name, base, d, L, r, col, bone):
    """A six-sided crystal: a prism from a little under base out along d, its end cut to a point."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    a, b = axes(d)
    pts = []
    for s, rr in ((-0.5 * r, 0.9), (0.7 * L, 1.0)):
        for j in range(6):
            t = math.pi / 3 * j
            pts.append(base + d * s + (a * math.cos(t) + b * math.sin(t)) * r * rr)
    pts.append(base + d * L)
    k.add(name, hull(pts), col, bone, fuse=False, glow=col, surface="shell")


def amber(k):
    """The Mega's rock turned to crystal: its colour and glow, never its shape."""
    n = 0
    for p in k.parts:
        if not p["fuse"] and np.allclose(p["color"], srgb(ROCK)) and p["name"].startswith(TURNS):
            col = (AMBER, DEEPAMBER)[n % 2]
            p["color"], p["glow"] = srgb(col), col
            n += 1


def cluster(k):
    """The great cluster erupting from the back between the wings, up and back, fanned out to both sides."""
    root = np.array([0.0, 0.07, 0.44])
    for j, (d, L, r) in enumerate([((0.0, 0.5, 1.0), 0.22, 0.042), ((0.45, 0.55, 0.8), 0.18, 0.036),
                                   ((-0.45, 0.55, 0.8), 0.17, 0.036), ((0.2, 0.95, 0.5), 0.16, 0.034),
                                   ((-0.25, 0.9, 0.55), 0.15, 0.032), ((0.7, 0.4, 0.45), 0.13, 0.03),
                                   ((-0.7, 0.4, 0.5), 0.12, 0.03), ((0.1, 0.25, 1.0), 0.14, 0.03)]):
        d = unit(*d)
        off = np.array([d[0], 0.0, 0.0]) * 0.03
        crystal(k, f"mmcluster{j}", root + off, d, L, r, (AMBER, DEEPAMBER, PALEAMBER)[j % 3], "body")
    # and lower down the back, toward the tail
    for j, (p, d, L) in enumerate([((0.0, 0.085, 0.33), (0.3, 0.9, 0.6), 0.12), ((0.0, 0.08, 0.36), (-0.35, 0.9, 0.5), 0.11)]):
        crystal(k, f"mmback{j}", np.array(p), unit(*d), L, 0.03, (PALEAMBER, AMBER)[j], "body")


def emblem(k):
    n = unit(0, -1, 0.12)
    c = np.array([0.0, -0.088, 0.33])
    k.add("mmemblem", lathe(c, n, [(0, 0), (0.032, 0), (0.032, 0.01), (0, 0.01)], seg=32), "#5A2448", "body",
          fuse=False, glow="#5A2448")
    k.add("mmemblemring", ring_band(c + n * 0.006, n, 0.038, 0.012, thick=0.011), RING, "body", fuse=False, glow=RING)
    a, b = axes(n)
    for j, e in enumerate((a, b)):
        f = np.cross(n, e)
        pts = [c + n * 0.008 + e * sd * 0.03 for sd in (1, -1)] + [c + n * 0.008 + f * sd * 0.007 for sd in (1, -1)]
        pts += [p + n * 0.007 for p in pts]
        k.add(f"mmemblemstar{j}", hull(pts), STAR, "body", fuse=False, glow=STAR)


def clump(k, name, c, tangent, radial, s0, col, other, rnd, z0=0.0):
    """A mound of cloud: three round puffs of mixed sizes and a small one riding on top."""
    for j, (sz, t, r, top) in enumerate([(1.0, 0.0, 0.0, 0), (0.78, 0.62, 0.25, 0), (0.66, -0.58, 0.18, 0),
                                         (0.5, 0.1, -0.2, 1)]):
        s = s0 * sz * (1 + 0.12 * rnd.uniform(-1, 1))
        p = c + tangent * t * s0 + radial * r * s0
        z = z0 + (s0 * 1.45 if top else s * 0.9)
        k.add(f"{name}_{j}", ell((p[0], p[1], z), (s, s, s * 0.9)), other if j == 2 else col, "root")


def clouds(k):
    rnd = np.random.default_rng(5)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    centres = []
    # A ring winding inward on the ground under the hovering body, open at the front
    for i, ang in enumerate([-50, -10, 30, 70, 110, 150, 190, 230]):
        a = math.radians(ang)
        r = 0.36 - 0.012 * i
        c = np.array([0.0, 0.06]) + np.array([r * math.cos(a), r * 0.95 * math.sin(a)])
        tg = np.array([-math.sin(a), math.cos(a)])
        rd = np.array([math.cos(a), math.sin(a)])
        clump(k, f"mmcloud{i}", c, tg, rd, 0.062, cols[i % 4], cols[(i + 1) % 4], rnd)
        centres.append(c)
    # The ring's tail winding in under the body
    for j, (c, s) in enumerate([((-0.18, -0.12), 0.05), ((-0.08, -0.16), 0.042)]):
        k.add(f"mmtail{j}", ell((c[0], c[1], s * 0.9), (s, s, s * 0.9)), (DEEP, CRIMSON)[j], "root")
    for j, i in enumerate((0, 3, 5)):
        c = centres[i]
        k.add(f"mmcurl{j}", ell((c[0], c[1], 0.11), (0.02, 0.02, 0.018)), CYAN, "root", fuse=False, glow=CYAN)
    pts = [(0.33, -0.12, 0.03), (0.36, -0.1, 0.1), (0.31, -0.08, 0.14), (0.35, -0.06, 0.21)]
    k.add("mmbolt0", outward(tube(pts, [0.012] * len(pts), seg=8)), CYAN, "root", fuse=False, glow=CYAN)


def maxed(k):
    clouds(k)
    cluster(k)
    emblem(k)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the Mega's body; the new pieces are made into a mesh of their own first
        _laid.append(True)
        amber(self)
        saved = (self.parts, self.paints, self.decals)
        self.parts, self.paints, self.decals = [], [], []
        maxed(self)
        clo = _build(self, voxel=0.007, smooth=4, name="mmclouds")
        self.parts, self.paints, self.decals = saved
        body = _build(self, *a, **kw)
        V = np.vstack([np.array([v.co[:] for v in o.data.vertices]) for o in (clo, body)])
        w, l, h = np.ptp(V[:, 0]), np.ptp(V[:, 1]), V[:, 2].max()
        sq = 1.3 * max(h, 0.85 * max(w, l))
        print(f"KIT: board: width {w:.3f} length {l:.3f} height {h:.3f}; sum {0.853 * w + 0.522 * l:.3f} "
              f"of square {sq:.3f} ({(0.853 * w + 0.522 * l) / sq:.2f}); lowest {V[:, 2].min():.3f}")
        return body
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("aerodactylmega")
m.aerodactylmega(*args())
