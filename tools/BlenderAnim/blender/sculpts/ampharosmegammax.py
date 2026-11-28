# Mega Dynamax Ampharos (Ampharos-Mega-Mmax), a form of this project's own: Mega Ampharos (ampharosmega.py) whole and
# unchanged, with what the form lays over it added to its kit just before the body is meshed:
#   blender --background --factory-startup --python sculpts/ampharosmegammax.py -- <the export's Ampharos-Mega-Mmax.glb> <out.glb>
#
# What the form adds, from its picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Ampharos.jpg), largest first:
#   1. the clouds of Max energy, deep violet and crimson, rolling about the feet and under the tail's fleece, with cyan
#      sparks of lightning in them, and crimson curls in the air;
#   2. a lantern on the tip of the tail's rising wisp: a cage of silver about a glowing yellow light, a spike on its roof;
#   3. the mane turned silver, faintly lavender;
#   4. the Key Stone's emblem on the upper chest, on the side the front camera sees.
# The clouds are a mesh of their own, built before the body, so that the Mega's fused body is meshed exactly as it is.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, ring_band, hull, tube, unit, srgb, outward, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
SILVER, LAMP, MANE = "#D2D6E2", "#FFE27A", "#D6D8EE"
EMBLEM, RING = "#FF7AC8", "#5EDCF4"


# A mound's puffs: (along the ring, outward, size, height of its middle in its own size): a big one, two at its sides,
# one behind, one small riding on top; every one standing on the ground or above it
PUFFS = ((0, 0, 1.0, 0.95), (0.95, 0.15, 0.8, 0.95), (-0.9, 0.25, 0.75, 0.95), (0.15, 0.6, 0.72, 0.95),
         (0.1, 0.05, 0.6, 2.2))


def clump(k, name, c, size, cols, bone="root"):
    c = np.array(c, float)
    o = unit(c[0] - 0.05, c[1] - 0.15, 0)
    tg = np.array([-o[1], o[0], 0.0])
    for j, (dt, dr, f, h) in enumerate(PUFFS):
        s = size * f
        p = c + tg * dt * size + o * dr * size + np.array([0, 0, s * h])
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.92)), cols[j % len(cols)], bone)


def bolt(k, name, pts, bone="root"):
    """A spark of lightning: a short zigzag of glowing cyan."""
    k.add(name, outward(tube(pts, [0.011] * len(pts), seg=8, per=2)), CYAN, bone, fuse=False, glow=CYAN)


def clouds(k):
    """A mesh of its own: mounds in an oval about the feet and the tail's fleece, crimson curls in the air."""
    n = 11
    for i in range(n):
        a = 2 * math.pi * (i + 0.2) / n
        cc = (0.05 + 0.29 * math.cos(a), 0.15 + 0.4 * math.sin(a), 0.0)
        cols = [(DVIOLET, CRIMSON, VIOLET, DVIOLET), (CRIMSON, DEEP, DVIOLET, CRIMSON), (DEEP, DVIOLET, CRIMSON, DEEP)][i % 3]
        clump(k, f"cloud{i}", cc, 0.058, cols)
        if i % 3 == 0:
            c = np.array(cc)
            bolt(k, f"bolt{i}", [c + (0, 0, 0.09), c + (0.025, -0.01, 0.13), c + (-0.005, -0.015, 0.155),
                                 c + (0.02, -0.02, 0.2)])
    # crimson curls in the air: beside the chest on each side, and over the lantern
    for tag, c0, bone in (("R", (0.21, -0.06, 0.56), "body"), ("L", (-0.21, 0.02, 0.48), "body")):
        for q in range(3):
            a = math.radians(210 + 75 * q)
            p = np.array(c0) + np.array([0.0, 0.045 * math.cos(a), 0.04 * math.sin(a)])
            r = (0.024, 0.02, 0.016)[q]
            k.add(f"curl{tag}{q}", ell(p, (r * 0.85, r, r)), (CRIMSON, DEEP, CRIMSON)[q], bone)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Ampharos: called with the Mega's kit just before it is meshed."""
    # 2. the lantern on the wisp's tip (ampharosmega.py's WISP ends at (0.22, 0.54, 0.6), radius 0.017)
    tip = np.array([0.222, 0.541, 0.605])
    lo, hi = tip + (0, 0, 0.01), tip + (0, 0, 0.085)
    k.add("lampbase", lathe(lo - (0, 0, 0.012), (0, 0, 1), [(0, 0), (0.03, 0), (0.034, 0.012), (0.026, 0.016),
                                                           (0, 0.016)], seg=6), SILVER, "tail", fuse=False,
          surface="metal")
    k.add("lamp", lathe(lo, (0, 0, 1), [(0, 0), (0.022, 0.004), (0.024, 0.04), (0.02, 0.07), (0, 0.072)], seg=6),
          LAMP, "tail", fuse=False, glow=LAMP)
    for i in range(6):                              # the cage's posts, at the corners of the six-sided light
        t = math.pi * i / 3 + math.pi / 6
        e = np.array([math.cos(t), math.sin(t), 0])
        k.add(f"lamppost{i}", hull([lo + e * 0.022, lo + e * 0.034, hi + e * 0.022, hi + e * 0.03,
                                    lo + e * 0.028 + np.cross((0, 0, 1), e) * 0.006,
                                    hi + e * 0.026 + np.cross((0, 0, 1), e) * 0.006]), SILVER, "tail", fuse=False,
              surface="metal")
    k.add("lamproof", lathe(hi - (0, 0, 0.004), (0, 0, 1), [(0, 0), (0.036, 0), (0.03, 0.012), (0.012, 0.03),
                                                           (0.006, 0.06), (0, 0.062)], seg=6), SILVER, "tail",
          fuse=False, surface="metal")

    # 3. the mane silvered
    for p in k.parts:
        if p["name"].startswith("lock"):
            p["color"] = srgb(MANE)

    # 4. the emblem on the upper chest, a little to +X, where the front camera sees it, a hair off the body
    d = unit(0.3, -1, 0.25)
    c = np.array([0.032, -0.088, 0.418]) + d * 0.002
    k.add("emblem", lathe(c, d, [(0, 0.0), (0.026, 0.0), (0.026, 0.009), (0, 0.011)]), EMBLEM, "body", fuse=False,
          glow=EMBLEM)
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.033, 0.012, thick=0.011), RING, "body", fuse=False, glow=RING)
    for i in range(8):                              # the ring's points
        a = 2 * math.pi * i / 8
        e1 = np.array([math.cos(a), 0, math.sin(a)])
        e1 = e1 - d * (e1 @ d); e1 /= np.linalg.norm(e1)
        b = c + d * 0.004 + e1 * 0.036
        sd = np.cross(d, e1)
        k.add(f"emblempt{i}", hull([b + sd * 0.008, b - sd * 0.008, b + e1 * 0.014, b + d * 0.008, b - d * 0.004]),
              RING, "body", fuse=False, glow=RING)


m = sibling("ampharosmega")
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

m.ampharos_mega(*args())
