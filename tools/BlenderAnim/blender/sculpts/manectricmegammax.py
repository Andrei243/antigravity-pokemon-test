# Manectric-Mega-Mmax: Mega Manectric's own sculpt (manectricmega.py, whole and unchanged) with the Mega Dynamax
# form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/manectricmegammax.py -- <the export's Manectric-Mega-Mmax.glb> <out.glb>
#
# From the design (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Manectric.jpg): the mane of lightning blades grown
# into a thicker crown of crystal, the design's own addition and its largest: three more pairs of glowing pale yellow
# crystal blades among the Mega's, the tallest rising a tenth over them, cyan lightning crackling along the blades'
# outer faces; the Key Stone's emblem, a cyan star in a pale ring, on the great forward blade beside the head (on +X,
# where the front camera looks); the Max cloud in mounds about the four feet with cyan and violet lightning in it, a
# small crimson cloud floating by each shoulder.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, unit, axes, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
CRYSTAL, SPARK, LILAC = "#FFF8C8", "#7CF0F4", "#BE96FF"
STONE, RING = "#7EE8F0", "#E8FCFF"


def zig(k, name, pts, w, col, bone):
    """Lightning through pts: each leg a flat-faced solid, glowing."""
    pts = [np.array(p, float) for p in pts]
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        d = unit(*(b - a))
        e1, e2 = axes(d)
        q = [a + e1 * w, a - e1 * w, a + e2 * w, a - e2 * w, b + e1 * w, b - e1 * w, b + e2 * w, b - e2 * w]
        k.add(f"{name}{i}", hull(q), col, bone, fuse=False, glow=col)


def clump(k, name, p0, s0, col, other, bone="root"):
    """One mound of cloud: four round puffs of mixed sizes about p0 on the ground, one of another colour."""
    for j, (dx, dy, f) in enumerate(((0, 0, 1.0), (0.9, 0.35, 0.78), (-0.85, 0.3, 0.64), (0.1, -0.05, 0.5))):
        s = s0 * f
        p = np.array([p0[0] + dx * s0, p0[1] + dy * s0, s * (0.75, 0.85, 0.8, 2.6)[j]])
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.88)), col if j != 2 else other, bone)


def blade_frame(root, d, normal):
    root, d = np.array(root, float), unit(*d)
    n = np.array(normal, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    return root, d, n, np.cross(n, d)


def maxed(k, mm):
    """What the Mega Dynamax form lays over Mega Manectric: called with the Mega's kit just before it is meshed."""
    # --- three more pairs of crystal blades among the Mega's (manectricmega.py's BLADES rise to z 0.91) ------------
    CRYSTALS = (
        # root, direction, length, width, thickness
        ((0.03, -0.06, 0.46), (0.08, 0.25, 1.0), 0.53, 0.15, 0.042),
        ((0.07, -0.04, 0.44), (0.3, 0.6, 0.75), 0.5, 0.16, 0.042),
        ((0.085, 0.04, 0.42), (0.45, 0.85, 0.35), 0.42, 0.15, 0.04),
    )
    NORMAL = (1, 0, 0.45)
    for i, (root, d, ln, w, th) in enumerate(CRYSTALS):
        k.add(f"crystal{i}", mm.bolt_blade(root, d, ln, w, th, NORMAL), CRYSTAL, "body", fuse=False, mirror=True,
              glow=CRYSTAL)
    # cyan lightning along the outer faces of the Mega's blades and the crystals
    LIT = (((0.05, -0.08, 0.45), (0.18, 0.5, 0.85), 0.54, 0.055),     # manectricmega.py's blade1
           ((0.07, -0.01, 0.44), (0.3, 0.78, 0.55), 0.56, 0.055),     # blade2
           ((0.07, -0.04, 0.44), (0.3, 0.6, 0.75), 0.5, 0.042))       # crystal1
    for i, (root, d, ln, th) in enumerate(LIT):
        r, dd, n, a = blade_frame(root, d, NORMAL)
        off = n * (th * 0.42 + 0.012)
        pts = [r + dd * ln * t + a * ln * (0.05 if j % 2 else -0.05) * (1 - t) + off * (1 - 0.6 * t)
               for j, t in enumerate((0.2, 0.36, 0.52, 0.68, 0.84))]
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            zig(k, f"spark{i}{'R' if s > 0 else 'L'}", [p * m for p in pts], 0.0145, SPARK if i != 1 else LILAC,
                "body")

    # --- the emblem on the +X cheek, under and behind the eye and clear of it and the mark behind it: a glowing cyan
    # stone with a white star in a pale glowing ring, 0.056 across (0.06 of the height). The point is read off Mega
    # Manectric's mesh (a ray from the head's centre (0, -0.24, 0.425) along (0.762, -0.457, -0.457), normal there
    # (0.813, -0.393, -0.43)); the disc is turned half way from that normal toward the front so the front camera
    # sees it whole (a ray from it to the camera meets nothing), its back edge sunk in the cheek ------------------
    n = unit(0.813, -0.393 - 0.45, -0.43 + 0.1)
    c = np.array([0.0893, -0.2936, 0.3714]) + n * 0.006
    k.add("emblem", lathe(c, n, [(0, 0), (0.018, 0), (0.018, 0.008), (0, 0.008)]), STONE, "head", fuse=False,
          glow=STONE)
    e1 = np.cross(n, (0, 0, 1.0)); e1 /= np.linalg.norm(e1)
    e2 = np.cross(n, e1)
    star = [c + n * 0.008 + e1 * 0.012, c + n * 0.008 - e1 * 0.012, c + n * 0.008 + e2 * 0.012,
            c + n * 0.008 - e2 * 0.012, c + n * 0.013, c + n * 0.006]
    k.add("emblemstar", hull(star), "#FFFFFF", "head", fuse=False, glow="#FFFFFF")
    k.add("emblemring", ring_band(c + n * 0.003, n, 0.024, 0.01, thick=0.008), RING, "head", fuse=False, glow=RING)

    # --- the clouds about the four feet (the forepaws at y -0.16, the hind at 0.13, x +-0.076) --------------------
    cols = (CRIMSON, VIOLET, DEEP, DVIOLET)
    n_ = 9
    for i in range(n_):
        t = 2 * math.pi * (i + 0.5) / n_
        x, y = 0.24 * math.cos(t), -0.02 + 0.33 * math.sin(t)
        clump(k, f"cloud{i}", (x, y), 0.046, cols[i % 4], cols[(i + 1) % 4])
    zig(k, "boltR", [(0.22, -0.2, 0.13), (0.28, -0.14, 0.09), (0.24, -0.09, 0.07), (0.31, -0.02, 0.02)], 0.012,
        SPARK, "root")
    zig(k, "boltL", [(-0.21, 0.16, 0.13), (-0.28, 0.1, 0.09), (-0.23, 0.05, 0.07), (-0.3, -0.02, 0.02)], 0.012,
        LILAC, "root")
    # a small crimson cloud floating by each shoulder
    for i, s in enumerate((1, -1)):
        p = np.array([0.23 * s, -0.2, 0.37])
        for j, (o, f) in enumerate((((0, 0, 0), 1.0), ((0.9, 0.2, -0.25), 0.75), ((-0.85, -0.2, -0.3), 0.7),
                                    ((0.1, 0.0, 0.75), 0.6))):
            r = 0.03 * f
            k.add(f"puff{i}_{j}", ell(p + np.array(o) * 0.03, (r, r, r * 0.85)), (CRIMSON, DEEP)[i], "body")


m = sibling("manectricmega")
_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self, m)
    return _build(self, *a, **kw)


Kit.build = build

m.manectricmega(*args())
