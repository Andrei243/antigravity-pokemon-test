# Mega Dynamax Gyarados (a form of our own) on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gyaradosmegammax.py -- <the export's Gyarados-Mega-Mmax.glb> <out.glb>
#
# Mega Gyarados as gyaradosmega.py builds it (it builds as it is read), whole and unchanged, with what the form lays
# over it, as its picture draws it: a crest of glowing crystals, cyan and violet, standing in fans all along its back
# from the crown behind the horn down the rearing neck, over the coil and onto the tail; the Key Stone's emblem glowing
# on its brow above the snout; a whirlpool of dark water on the ground under the coil with rings of white foam winding
# into it; and the clouds of Max energy, a broken ring of crimson and violet mounds about the whirlpool's edge with cyan
# curls, two puffs drifting at the coil's height and bolts of lightning.
#
# The additions are a build of their own ("mmclouds", made just before the Mega's body is meshed, through the kit's
# build), so the Mega's fused surface, colours, weights and patches come out as gyaradosmega.py makes them. Where the
# crystals stand is read from gyaradosmega.py's own module (its `at`, `bone_at` and `hp`, through the frame that calls
# the build).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, outward, axes, unit, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
ICE, AMETHYST, PALEV = "#6EE2EE", "#9660E0", "#C8A4F8"
WATER, FOAM, EMBLEM, STAR = "#3A4E66", "#D2E6F4", "#4EE2EC", "#DCA0FF"
UP = np.array([0, 0, 1.0])


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


def fan(k, name, base, d, side, L, r, bone, flip):
    """Three crystals from one root: the longest along d, two shorter leaning to either side."""
    cols = (ICE, AMETHYST) if not flip else (AMETHYST, ICE)
    crystal(k, name + "a", base, d, L, r, cols[0], bone)
    for j, sd in enumerate((1, -1)):
        dd = unit(*(d + side * sd * 0.55 - d * 0.1))
        crystal(k, name + "bc"[j], base + side * sd * r * 0.7, dd, L * (0.68 - 0.08 * j), r * 0.78,
                cols[1] if j == 0 else PALEV, bone)


def crest(k, g):
    """The crystals along the back, read off gyaradosmega.py's curve."""
    at, bone_at, hp = g["at"], g["bone_at"], g["hp"]
    for i, (s, L) in enumerate([(0.75, 0.1), (1.4, 0.13), (2.2, 0.15), (3.0, 0.17), (3.8, 0.18), (4.55, 0.18),
                                (5.3, 0.18), (5.9, 0.17), (6.55, 0.16), (7.25, 0.15), (7.95, 0.14)]):
        c, t, bel, r = at(s)
        back = -bel
        side = np.cross(t, back); side /= np.linalg.norm(side)
        d = unit(*(back * 0.75 + UP * 0.55))
        fan(k, f"mmcrest{i}", c + back * r * 0.75, d, side, L * 1.4, 0.037 if L > 0.12 else 0.028, bone_at(s), i % 2)
    # A fan on the crown behind the horn
    fan(k, "mmcrown", hp((0.0, 0.0, 1.0)), unit(0, 0.75, 0.75), np.array([1.0, 0, 0]), 0.15, 0.03, "head", 0)


def emblem(k, g):
    """The Key Stone's emblem on the brow above the snout: a glowing disc in a ring, a star of two crossed bars."""
    hp = g["hp"]
    n = unit(0, -0.4, 1.0)
    c = hp((0.0, -0.31, 0.997)) + n * 0.004
    k.add("mmemblem", lathe(c, n, [(0, 0), (0.034, 0), (0.034, 0.01), (0, 0.01)], seg=32), "#1E4C66", "head",
          fuse=False, glow="#1E4C66")
    k.add("mmemblemring", ring_band(c + n * 0.006, n, 0.04, 0.012, thick=0.011), EMBLEM, "head", fuse=False, glow=EMBLEM)
    a, b = axes(n)
    for j, e in enumerate((a, b)):
        f = np.cross(n, e)
        pts = [c + n * 0.008 + e * sd * 0.03 for sd in (1, -1)] + [c + n * 0.008 + f * sd * 0.007 for sd in (1, -1)]
        pts += [p + n * 0.007 for p in pts]
        k.add(f"mmemblemstar{j}", hull(pts), STAR, "head", fuse=False, glow=STAR)


WC = np.array([0.0, 0.05])     # the whirlpool's middle, under the coil


def whirlpool(k):
    k.add("mmwater", lathe((WC[0], WC[1], 0.0), (0, 0, 1), [(0, 0), (0.4, 0), (0.42, 0.006), (0.4, 0.012), (0, 0.012)],
                           seg=64), WATER, "root", fuse=False, surface="skin")
    for j in range(2):
        pts = []
        for i in range(25):
            t = i / 24
            ang = math.radians(j * 180 + t * 300)
            r = 0.39 - 0.25 * t
            pts.append((WC[0] + r * math.cos(ang), WC[1] + r * math.sin(ang), 0.018))
        k.add(f"mmfoam{j}", outward(tube(pts, [0.016] * len(pts), seg=10)), FOAM, "root", fuse=False)
    k.add("mmfoamrim", ring_band((WC[0], WC[1], 0.012), (0, 0, 1), 0.41, 0.014, thick=0.014), FOAM, "root", fuse=False)


def clump(k, name, c, tangent, radial, s0, col, other, rnd, z0=0.0, bone="root"):
    """A mound of cloud: three round puffs of mixed sizes and a small one riding on top."""
    for j, (sz, t, r, top) in enumerate([(1.0, 0.0, 0.0, 0), (0.78, 0.62, 0.25, 0), (0.66, -0.58, 0.18, 0),
                                         (0.5, 0.1, -0.2, 1)]):
        s = s0 * sz * (1 + 0.12 * rnd.uniform(-1, 1))
        p = c + tangent * t * s0 + radial * r * s0
        z = z0 + (s0 * 1.45 if top else s * 0.9)
        k.add(f"{name}_{j}", ell((p[0], p[1], z), (s, s, s * 0.9)), other if j == 2 else col, bone)


def clouds(k):
    rnd = np.random.default_rng(11)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    centres = []
    # A broken ring on the whirlpool's edge, left open where the tail's fan lies and at the front
    for i, ang in enumerate([-60, -20, 15, 100, 140, 180, 215, 250]):
        a = math.radians(ang)
        c = WC + np.array([0.48 * math.cos(a), 0.46 * math.sin(a)])
        tg = np.array([-math.sin(a), math.cos(a)])
        rd = np.array([math.cos(a), math.sin(a)])
        clump(k, f"mmcloud{i}", c, tg, rd, 0.085, cols[i % 4], cols[(i + 1) % 4], rnd)
        centres.append(c)
    # Two small mounds drifting at the coil's height, as the picture has them by the neck and the tail
    for j, (c, s0) in enumerate([((-0.36, -0.05), 0.05), ((0.42, 0.2), 0.045)]):
        clump(k, f"mmdrift{j}", np.array(c), np.array([1.0, 0]), np.array([0, 1.0]), s0, CRIMSON, DEEP, rnd,
              z0=0.42 + 0.06 * j)
    for j, i in enumerate((0, 3, 6)):
        c = centres[i]
        k.add(f"mmcurl{j}", ell((c[0], c[1], 0.16), (0.026, 0.026, 0.024)), CYAN, "root", fuse=False, glow=CYAN)
    for j, (pts, col) in enumerate([([(0.36, -0.25, 0.04), (0.4, -0.22, 0.12), (0.33, -0.2, 0.17), (0.38, -0.17, 0.26)], CYAN),
                                    ([(-0.42, 0.2, 0.04), (-0.46, 0.22, 0.13), (-0.4, 0.24, 0.18), (-0.45, 0.26, 0.27)], "#F05A78")]):
        k.add(f"mmbolt{j}", outward(tube(pts, [0.017] * len(pts), seg=8)), col, "root", fuse=False, glow=col)


def maxed(k, g):
    clouds(k)
    whirlpool(k)
    crest(k, g)
    emblem(k, g)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the Mega's body; the additions are made into a mesh of their own first
        _laid.append(True)
        g = sys._getframe(1).f_globals
        saved = (self.parts, self.paints, self.decals)
        self.parts, self.paints, self.decals = [], [], []
        maxed(self, g)
        clo = _build(self, voxel=0.011, smooth=4, name="mmclouds")
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

sculptkit.sibling("gyaradosmega")      # builds as it is read, with this script's own arguments
