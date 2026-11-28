# Skarmory-Mega-Mmax: Mega Skarmory (skarmorymega.py, built unchanged) with what its Mega Dynamax form lays over it,
# added to the Mega's own kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skarmorymegammax.py -- <the export's Skarmory-Mega-Mmax.glb> <out.glb>
#
# From the picture (Mega-Dynamax-Skarmory.jpg): (1) long blades of red and golden crystal fanned through each wing among
# the Mega's own spears, the largest addition; (2) the Key Stone's emblem on the breast, a cyan stone with a gold star in
# a gold ring; (3) clouds of Max energy, crimson and violet with cyan curls, billowing under the hovering bird, and a
# broken ring of crimson puffs floating over it.
# Taken from the Mega's module: the wing's frame (wing_axes(), ROOT) and the head's centre H; from its function (local
# there): the breast (0, -0.06, 0.45) r (0.098, 0.09, 0.11), the neck from (0, -0.07, 0.5) r 0.064.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, unit, axes, sibling, args

CRIMSON, DEEPCRIMSON, VIOLET, DEEPVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
RED, GOLDC = "#EE4058", "#F6CA4E"
RING, DISC, STAR = "#F4C850", "#6EE6EE", "#FFE69A"

m = sibling("skarmorymega")


def star4(c, d, r, thick):
    """A four-pointed star standing on d at c (a flat-faced hull)."""
    a, b = axes(d)
    c, d = np.array(c, float), np.array(d, float)
    pts = []
    for e in (a, -a, b, -b):
        pts += [c + e * r, c + e * r + d * thick]
    for e in (a + b, a - b, -a + b, -a - b):
        pts += [c + e * r * 0.2, c + e * r * 0.2 + d * thick * 1.6]
    return hull(pts)


def emblem(k, c, d, r, bone):
    """The Key Stone's emblem: a cyan stone with a gold star, in a glowing gold ring, a hair off the body."""
    d = np.array(d, float) / np.linalg.norm(d)
    k.add("emblemdisc", lathe(c, d, [(0, 0), (r * 0.76, 0), (r * 0.76, 0.008), (0, 0.012)]), DISC, bone,
          fuse=False, glow=DISC)
    k.add("emblemring", lathe(c, d, [(r * 0.7, 0), (r, 0), (r, 0.011), (r * 0.7, 0.011)], seg=48), RING, bone,
          fuse=False, glow=RING)
    k.add("emblemstar", star4(c + d * 0.01, d, r * 0.62, 0.006), STAR, bone, fuse=False, glow=STAR)


def crystal_blade(r, ang, length, width, thick, off, ex, ez, n):
    """A faceted blade of crystal in the wing's plane from (r) along ang degrees from the span toward the chord:
    a kite in section, widest a third of the way, a long point (a flat-faced hull)."""
    t = math.radians(ang)
    d = math.cos(t) * ex + math.sin(t) * ez
    s = -math.sin(t) * ex + math.cos(t) * ez
    o = off + r[0] * ex + r[1] * ez
    pts = [o + s * width * 0.4, o - s * width * 0.4,
           o + d * length * 0.32 + s * width * 0.5 + n * thick, o + d * length * 0.32 + s * width * 0.5 - n * thick,
           o + d * length * 0.32 - s * width * 0.5 + n * thick, o + d * length * 0.32 - s * width * 0.5 - n * thick,
           o + d * length * 0.32 + n * thick * 1.5, o + d * length * 0.32 - n * thick * 1.5,
           o + d * length * 0.78 + s * width * 0.22, o + d * length * 0.78 - s * width * 0.22,
           o + d * length, o + n * thick * 0.8, o - n * thick * 0.8]
    return hull(pts)


def clump(k, name, c, size, cols, bone, rnd):
    """One cloud mound: round puffs of mixed sizes, one riding on top, fused."""
    c = np.array(c, float)
    for j, (dx, dy, dz, f) in enumerate(((0, 0, 0, 1.0), (0.8, 0.3, -0.1, 0.78), (-0.75, 0.25, -0.12, 0.7),
                                           (0.1, -0.1, 0.85, 0.6))):
        s = size * f * (1 + 0.1 * rnd.uniform(-1, 1))
        k.add(f"{name}_{j}", ell(c + np.array([dx, dy, dz]) * size, (s, s, s * 0.88)), cols[j % len(cols)], bone)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Skarmory: called with the Mega's kit just before it is meshed."""
    # (1) the crystal blades fanned through each wing, between and over the Mega's spears, red and gold in turn
    ex, ez = m.wing_axes()
    n = np.cross(ex, ez)
    fan = ((0.05, -0.01, -4, 0.62, 0.075, RED, 0.024), (0.07, 0.025, 8, 0.58, 0.07, GOLDC, -0.026),
           (0.09, 0.055, 19, 0.53, 0.068, RED, 0.026), (0.1, 0.085, 30, 0.47, 0.064, GOLDC, -0.026),
           (0.1, 0.115, 43, 0.4, 0.06, RED, 0.026))
    for j, (a0, b0, ang, L, w, col, side) in enumerate(fan):
        k.add(f"xblade{j}", crystal_blade((a0, b0), ang, L, w, 0.014, m.ROOT + n * side, ex, ez, n), col,
              "wing{s}", fuse=False, mirror=True, glow=col, surface="shell")

    # (2) the emblem on the breast under the neck
    emblem(k, np.array([0.0, -0.148, 0.405]), unit(0, -0.97, -0.2), 0.047, "body")

    # (3) the clouds billowing under the bird (it hovers: nothing fused is near the ground, so they stand alone)
    rnd = np.random.default_rng(5)
    under = [(0.0, -0.12, 0.07), (0.17, -0.02, 0.07), (-0.17, -0.02, 0.07), (0.2, 0.16, 0.065), (-0.2, 0.16, 0.065),
             (0.08, 0.32, 0.06), (-0.1, 0.33, 0.06), (0.0, 0.1, 0.075)]
    pal = [(CRIMSON, VIOLET, DEEPCRIMSON), (VIOLET, CRIMSON, DEEPVIOLET), (DEEPCRIMSON, CRIMSON, VIOLET)]
    for i, (x, y, s) in enumerate(under):
        clump(k, f"cloud{i}", (x, y, s * 0.8), s, pal[i % 3], "root", rnd)
    for i, (x, y) in enumerate(((0.17, -0.02), (-0.2, 0.16), (0.0, -0.12))):
        k.add(f"curl{i}", ell((x, y, 0.15), (0.022, 0.022, 0.019), sub=3), CYAN, "root", fuse=False, glow=CYAN)
    # a broken ring of small crimson puffs floating over the head and the wings' roots
    for i, a in enumerate((20, 75, 140, 205, 290)):
        t = math.radians(a)
        c = (0.17 * math.cos(t), 0.05 + 0.15 * math.sin(t), 0.97)
        clump(k, f"halo{i}", c, 0.026, (CRIMSON, DEEPCRIMSON, CRIMSON), "body", rnd)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m.skarmorymega(*args())
