# Mega Dynamax Steelix (a form of our own) on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/steelixmegammax.py -- <the export's Steelix-Mega-Mmax.glb> <out.glb>
#
# Mega Steelix as steelixmega.py builds it (it builds as it is read), whole and unchanged in shape, with what the form
# lays over it, as its picture draws it: the Mega's crystals turned to glowing violet and cyan and more of them, great
# ones thrust out of the drums between, the largest thing the form adds; the blue rings of its hexagons lit cyan; a
# broken crown of pale violet crystals floating over its head; the Key Stone's emblem on its brow, a violet ring of
# light with a star in it; and the clouds of Max energy, a broken ring of crimson and violet mounds about the coil with
# cyan curls, two mounds drifting at the coil's height and bolts of lightning.
#
# The new pieces are a build of their own ("mmclouds", made just before the Mega's body is meshed, through the kit's
# build): steelixmega.py shades its loose pieces flat by counting faces back from the end of its body's mesh, so
# nothing may be added to that build. The Mega's crystals and rings are recoloured where it added them (colour and
# glow only). Where the drums stand is read from steelixmega.py's own module (JOINTS, P, frame, hp) through the frame
# that calls the build.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, outward, axes, unit, srgb, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
CVIOLET, CCYAN, CPALE = "#B07AF0", "#6EE2EE", "#C8B4F4"
RING, STAR = "#B47CF8", "#F2E6FF"


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


def relight(k):
    """The Mega's crystals lit violet and cyan, its hexagons' blue rings lit cyan: colour and glow only."""
    for p in k.parts:
        n = p["name"]
        if n.startswith("crystal") or n.startswith("tailcrys"):
            col = CVIOLET if (n.endswith("L") or n.endswith("0")) else CCYAN
            p["color"], p["glow"] = srgb(col), col
        elif n.startswith("hexb"):
            p["color"], p["glow"] = srgb(CYAN), CYAN


def thrust(k, g):
    """Great crystals out of the drums the Mega left bare (3, 5 and 7), out, up and forward."""
    JOINTS, P, frame = g["JOINTS"], g["P"], g["frame"]
    for n, (i, length) in enumerate(((3, 0.26), (5, 0.32), (7, 0.3))):
        bone, at, r = JOINTS[i]
        at = np.array(at)
        t = P[i + 1] - P[i - 1]; t /= np.linalg.norm(t)
        side, up = frame(t)
        if up[2] < 0:
            up = -up
        for s, nm in ((1, "R"), (-1, "L")):
            d = unit(*(side * s + up * 0.45 + t * 0.25))
            crystal(k, f"mmthrust{i}{nm}", at + d * r * 0.75, d, length, r * 0.5, (CCYAN, CVIOLET)[(n + (s > 0)) % 2], bone)


def crown(k, g):
    """A broken crown of pale crystals floating over the head."""
    for j, x in enumerate((-0.2, -0.13, -0.06, 0.02, 0.1, 0.18)):
        y = -0.18 + 0.06 * abs(x) / 0.2
        L = 0.12 - 0.025 * (j % 2)
        crystal(k, f"mmcrown{j}", np.array([x, y, 1.13 + 0.01 * (j % 2)]), unit(x * 0.6, 0.15, 1.0), L, 0.022,
                (CPALE, CVIOLET)[j % 3 == 2], "head")


def emblem(k, g):
    """The emblem on the brow's slope, between the ridges (the head's block, worked out from steelixmega.py's skull)."""
    n = unit(0, -0.28, 0.96)
    c = np.array([0.0, -0.438, 1.06]) + n * 0.004
    k.add("mmemblem", lathe(c, n, [(0, 0), (0.033, 0), (0.033, 0.01), (0, 0.01)], seg=32), "#3A2460", "head",
          fuse=False, glow="#3A2460")
    k.add("mmemblemring", ring_band(c + n * 0.006, n, 0.038, 0.012, thick=0.011), RING, "head", fuse=False, glow=RING)
    a, b = axes(n)
    for j, e in enumerate((a, b)):
        f = np.cross(n, e)
        pts = [c + n * 0.008 + e * sd * 0.03 for sd in (1, -1)] + [c + n * 0.008 + f * sd * 0.007 for sd in (1, -1)]
        pts += [p + n * 0.007 for p in pts]
        k.add(f"mmemblemstar{j}", hull(pts), STAR, "head", fuse=False, glow=STAR)


def clump(k, name, c, tangent, radial, s0, col, other, rnd, z0=0.0):
    """A mound of cloud: three round puffs of mixed sizes and a small one riding on top."""
    for j, (sz, t, r, top) in enumerate([(1.0, 0.0, 0.0, 0), (0.78, 0.62, 0.25, 0), (0.66, -0.58, 0.18, 0),
                                         (0.5, 0.1, -0.2, 1)]):
        s = s0 * sz * (1 + 0.12 * rnd.uniform(-1, 1))
        p = c + tangent * t * s0 + radial * r * s0
        z = z0 + (s0 * 1.45 if top else s * 0.9)
        k.add(f"{name}_{j}", ell((p[0], p[1], z), (s, s, s * 0.9)), other if j == 2 else col, "root")


CC = np.array([0.27, 0.02])       # the coil's middle on the ground


def clouds(k):
    rnd = np.random.default_rng(3)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    centres = []
    # Open where the coil's drums and the tail reach out to the ring
    for i, ang in enumerate([-95, -55, -15, 25, 115, 155, 190, 255]):
        a = math.radians(ang)
        c = CC + np.array([0.4 * math.cos(a), 0.42 * math.sin(a)])
        tg = np.array([-math.sin(a), math.cos(a)])
        rd = np.array([math.cos(a), math.sin(a)])
        clump(k, f"mmcloud{i}", c, tg, rd, 0.085, cols[i % 4], cols[(i + 1) % 4], rnd)
        centres.append(c)
    for j, (c, s0, z0) in enumerate([((0.6, 0.12), 0.05, 0.42), ((-0.12, 0.25), 0.05, 0.5)]):
        clump(k, f"mmdrift{j}", np.array(c), np.array([0, 1.0]), np.array([1.0, 0]), s0, CRIMSON, DEEP, rnd, z0=z0)
    for j, i in enumerate((1, 4, 6)):
        c = centres[i]
        k.add(f"mmcurl{j}", ell((c[0], c[1], 0.16), (0.026, 0.026, 0.024)), CYAN, "root", fuse=False, glow=CYAN)
    for j, (pts, col) in enumerate([([(0.62, -0.2, 0.04), (0.66, -0.18, 0.13), (0.6, -0.16, 0.18), (0.65, -0.13, 0.27)], CYAN),
                                    ([(-0.09, -0.05, 0.04), (-0.13, -0.03, 0.13), (-0.07, -0.01, 0.18), (-0.12, 0.02, 0.27)], "#B98CF4")]):
        k.add(f"mmbolt{j}", outward(tube(pts, [0.017] * len(pts), seg=8)), col, "root", fuse=False, glow=col)


def maxed(k, g):
    clouds(k)
    thrust(k, g)
    crown(k, g)
    emblem(k, g)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the Mega's body; the new pieces are made into a mesh of their own first
        _laid.append(True)
        g = sys._getframe(1).f_globals
        relight(self)
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

sculptkit.sibling("steelixmega")      # builds as it is read, with this script's own arguments
