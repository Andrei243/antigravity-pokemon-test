# Mega Dynamax Scizor (Scizor-Mega-Mmax), a form of this project's own: Mega Scizor (scizormega.py, which builds as it is
# read) whole and unchanged, with what the form lays over it added to its kit just before the body is meshed:
#   blender --background --factory-startup --python sculpts/scizormegammax.py -- <the export's Scizor-Mega-Mmax.glb> <out.glb>
#
# What the form adds, from its picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Scizor.jpg), largest first:
#   1. four wings of crystal, violet with orange facets, in place of the pale pair: an upper pair, long and broad, rising
#      up and out behind the shoulders (the Mega's own wings, recoloured, lie on them as their inner facets), and a
#      lower pair reaching down and out beside the abdomen;
#   2. the clouds of Max energy rolling about the feet, crimson and violet by turns with cyan curls, and violet wisps
#      over the shoulders;
#   3. the Key Stone's emblem on the chest, glowing: a cyan ring about a pale disc.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, ring_band, slab, hull, unit, srgb, sibling

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
CRYSTAL, CRYSTAL2, FACET = "#7440C8", "#9C62E2", "#FF8A3E"          # the wings' violet, its light facets, orange
STONE = "#FFD2F2"

# From scizormega.py (read out of its file, since it builds as it is read): the thorax, the wings' root and axes
TH_C, TH_R = np.array([0.0, -0.025, 0.575]), (0.066, 0.056, 0.07)
WING_O = (0.03, 0.06, 0.61)                    # the Mega's wing origin, x times the side
FEET_X = 0.168                                 # the claws' x
GROUND_R = 0.3                                 # the clouds' ring, about the feet


def wing_axes(s, sweep, lift, twist):
    """scizormega.py's own frame of a wing (copied): ex along the wing, ez across it."""
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([s * math.cos(li) * math.cos(sw), -math.cos(li) * math.sin(sw), math.sin(li)])
    ez = np.array([0.0, 1.0, math.tan(math.radians(twist))])
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def crystal_wing(k, side, s, o, ex, ez, L, W, lo, bone, facets):
    """A wing of crystal: a violet plate, a lighter panel inside it, and bands of orange facets across it (thicker, so
    they show on both faces). lo shifts the outline toward -ez (below the line of the wing) by that share of W."""
    t = np.linspace(0.0, 1.0, 22)
    a = 0.004 + t * L
    w = W * np.sqrt(np.clip(1 - ((t - 0.42) / 0.58) ** 2, 0, 1)) * (1.0 - 0.3 * t)
    w = np.maximum(w, 0.003)
    top, bot = w * (1 - lo), -w * (1 + lo)
    outline = np.vstack([np.stack([a, top], 1), np.stack([a, bot], 1)[::-1]])
    k.add(f"xwing{side}", slab(outline, o, ex, ez, 0.012), CRYSTAL, bone, fuse=False, glow=CRYSTAL)
    inner = np.vstack([np.stack([a[2:-3], top[2:-3] * 0.55 + (top[2:-3] + bot[2:-3]) * 0.22], 1),
                       np.stack([a[2:-3], bot[2:-3] * 0.55 + (top[2:-3] + bot[2:-3]) * 0.22], 1)[::-1]])
    k.add(f"xwingin{side}", slab(inner, o, ex, ez, 0.016), CRYSTAL2, bone, fuse=False, glow=CRYSTAL2)
    # orange facets: slanted bands across the wing, each a lozenge between the two edges
    for j, (u0, u1) in enumerate(facets):
        def edge(u, f):
            i = min(int(u * 21), 20); g = u * 21 - i
            return top[i] * (1 - g) + top[i + 1] * g if f > 0 else bot[i] * (1 - g) + bot[i + 1] * g
        b = 0.09                                   # the band's breadth, as a share of the wing's length
        pts = [(0.004 + u0 * L, edge(u0, -1) * 0.85), (0.004 + (u0 + b) * L, edge(u0 + b, -1) * 0.85),
               (0.004 + min(u1 + b, 0.97) * L, edge(min(u1 + b, 0.97), 1) * 0.85), (0.004 + u1 * L, edge(u1, 1) * 0.85)]
        k.add(f"facet{j}{side}", slab(pts, o, ex, ez, 0.02), FACET, bone, fuse=False, glow=FACET)


def clump(k, name, c, size, cols, bone="root", rnd=None):
    """A mound of cloud: three or four round puffs of mixed sizes, one small one riding on top, fused."""
    c = np.array(c, float)
    out_ = unit(c[0], c[1], 0) if np.hypot(c[0], c[1]) > 1e-6 else np.array([1.0, 0, 0])
    tg = np.array([-out_[1], out_[0], 0.0])
    for j, (dt, dr, f, h) in enumerate(PUFFS):
        s = size * f * (1 + 0.12 * (rnd.uniform(-1, 1) if rnd is not None else 0))
        p = c + tg * dt * size + out_ * dr * size + np.array([0, 0, s * h])
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.92)), cols[j % len(cols)], bone)


# A mound's puffs: (along the ring, outward, size, height of its middle in its own size): a big one, two at its sides,
# one behind, one small riding on top; every one standing on the ground or above it
PUFFS = ((0, 0, 1.0, 0.95), (0.95, 0.15, 0.8, 0.95), (-0.9, 0.25, 0.75, 0.95), (0.15, 0.6, 0.72, 0.95),
         (0.1, 0.05, 0.6, 2.2))


def maxed(k):
    """What the Mega Dynamax form lays over Mega Scizor: called with the Mega's kit just before it is meshed."""
    # 1. the wings: the Mega's pale pair turned to crystal, a broad upper pair of crystal behind them in their own plane,
    # and a lower pair reaching down and out
    for p in k.parts:
        nm = p["name"]
        if nm.startswith("wingin"):
            p["color"], p["glow"] = srgb(CRYSTAL2), CRYSTAL2
        elif nm.startswith("wing") or nm.startswith("vein"):
            p["color"], p["glow"] = srgb(FACET), FACET
    for s, side in ((1, "R"), (-1, "L")):
        o = np.array([WING_O[0] * s, WING_O[1], WING_O[2]])
        ex, ez = wing_axes(s, -40, 34, 55)
        n = np.cross(ex, ez)
        n = n if n[1] > 0 else -n                   # toward the back: the crystal lies behind the Mega's own wing
        crystal_wing(k, "U" + side, s, o + n * 0.016, ex, ez, 0.4, 0.105, -0.25, f"wing{side}",
                     [(0.2, 0.36), (0.46, 0.6), (0.7, 0.84)])
        ex2 = unit(s * 0.55, 0.4, -0.73)
        ez2 = np.array([s * 0.25, 0.25, 0.4])
        crystal_wing(k, "D" + side, s, np.array([0.035 * s, 0.07, 0.55]), ex2, ez2, 0.27, 0.07, 0.0, f"wing{side}",
                     [(0.22, 0.4), (0.58, 0.74)])

    # 3. the emblem on the chest: a pale glowing disc in a cyan ring, a hair off the thorax's front
    d = unit(0, -1, 0.22)
    c = TH_C + np.array([0, -TH_R[1] + 0.008, 0.012])
    k.add("emblem", lathe(c, d, [(0, 0.0), (0.03, 0.0), (0.03, 0.009), (0, 0.011)]), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.037, 0.012, thick=0.011), CYAN, "body", fuse=False, glow=CYAN)
    for i in range(8):                             # the ring's points
        a = 2 * math.pi * i / 8
        e1 = np.array([math.cos(a), 0, math.sin(a)])
        e1 = e1 - d * (e1 @ d); e1 /= np.linalg.norm(e1)
        b = c + d * 0.004 + e1 * 0.04
        side_ = np.cross(d, e1)
        k.add(f"emblempt{i}", hull([b + side_ * 0.009, b - side_ * 0.009, b + e1 * 0.016, b + d * 0.008,
                                    b - d * 0.004]), CYAN, "body", fuse=False, glow=CYAN)



def clouds(k):
    """The clouds, built as a mesh of their own before the body (so the Mega's fused body is meshed exactly as it is):
    mounds in a ring about the feet, clear of the claws, crimson and violet by turns, and wisps over the shoulders."""
    rnd = np.random.default_rng(11)
    n = 8
    for i in range(n):
        a = 2 * math.pi * (i + 0.3) / n
        r = GROUND_R * (1 + 0.06 * math.sin(3 * a))
        cc = (r * math.cos(a), r * math.sin(a) * 0.95 + 0.01, 0.0)
        cols = [(CRIMSON, CRIMSON, VIOLET, DEEP, CRIMSON), (VIOLET, VIOLET, CRIMSON, DVIOLET, VIOLET), (DEEP, DEEP, DVIOLET, CRIMSON, DEEP)][i % 3]
        clump(k, f"cloud{i}", cc, 0.07, cols, rnd=rnd)
        if i % 3 == 1:
            q = np.array(cc) + np.array([0, 0, 0.14])
            k.add(f"curl{i}", ell(q, (0.022, 0.022, 0.02), sub=3), CYAN, "root", fuse=False, glow=CYAN)
    # violet wisps rising over the shoulders, a string of shrinking puffs on the body's bone
    for s in (1, -1):
        base = np.array([0.13 * s, 0.02, 0.725])         # clear of the shoulder ball (its top 0.664)
        # a short curl drifting out and up from over the shoulder, its puffs overlapping into one strand
        for j, (dx, dy, dz, r) in enumerate(((0, 0, 0, 0.024), (0.03, 0.01, 0.014, 0.021), (0.058, 0.025, 0.02, 0.018),
                                             (0.08, 0.04, 0.036, 0.015))):
            k.add(f"wisp{j}{'R' if s > 0 else 'L'}", ell(base + np.array([dx * s, dy, dz]), (r, r, r * 0.85)),
                  (VIOLET, VIOLET, DVIOLET, CRIMSON)[j], "body")


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        keep = self.parts, self.paints, self.decals
        self.parts, self.paints, self.decals = [], [], []
        clouds(self)
        _build(self, voxel=0.008, smooth=4, band=0.006, name="maxclouds")
        self.parts, self.paints, self.decals = keep
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("scizormega")           # builds as it is read, with this script's own arguments
