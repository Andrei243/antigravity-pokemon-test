# Sharpedo-Mega-Mmax: Mega Sharpedo's own sculpt (sharpedomega.py, whole and unchanged) with the Mega Dynamax form's
# additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sharpedomegammax.py -- <the export's Sharpedo-Mega-Mmax.glb> <out.glb>
#
# From the design (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Sharpedo.jpg): its fins turned to blades of ice
# with water streaming round them, the design's own addition and its largest: the pectorals drawn out half as long
# again into pointed ice blades, each wound in a swirl of water, the dorsal fin a taller sail of ice, the fin under the
# rear ice too; the three white spikes of the snout's ridge turned to glowing gold crystal and a fourth on the crown
# behind them; the Key Stone's emblem on the snout's top between the crystals; the Max cloud in mounds in a ring under
# the hovering shark, a broken ring of crimson puffs over its back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, slab, srgb, unit, axes, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
ICE, WATER, GOLD = "#BEE4F6", "#E4F6FF", "#FAC43C"
STONE, RING = "#FAC43C", "#E8F6FF"

# sharpedo.py's fins' frames (origin, ex, ez) and the ice that encases each, thicker than the fin and its yellow bars
# (0.034) so that the fin is all ice, and longer
PEC_ICE = [(-0.012, 0.042), (0.1, 0.03), (0.2, 0.006), (0.27, -0.024), (0.17, -0.046), (0.1, -0.046), (-0.012, -0.04)]
DORSAL_ICE = [(-0.078, -0.006), (0.0, 0.09), (0.09, 0.19), (0.15, 0.24), (0.13, 0.15), (0.122, 0.08), (0.126, -0.006)]
VENTRAL_ICE = [(-0.046, 0.006), (0.0, -0.07), (0.04, -0.15), (0.05, -0.1), (0.072, -0.086), (0.062, 0.006)]


def frame(o, ex, ez):
    o, ex = np.array(o, float), unit(*ex)
    ez = np.array(ez, float); ez = ez - ex * (ex @ ez); ez /= np.linalg.norm(ez)
    return o, ex, ez, np.cross(ex, ez)


def swirl(k, name, o, ex, ez, n, a0, a1, rad, turns, bone):
    """A stream of water wound round a blade from a0 to a1 along it."""
    pts = []
    for t in np.linspace(0, 1, 28):
        a = a0 + (a1 - a0) * t
        th = 2 * math.pi * turns * t
        r = rad * (0.6 + 0.4 * math.sin(math.pi * t))
        pts.append(o + ex * a + ez * (r * math.cos(th) - 0.008) + n * r * math.sin(th))
    k.add(name, tube(pts, [0.0085] * len(pts)), WATER, bone, fuse=False, glow=WATER)


def clump(k, name, p0, s0, col, other, bone="root"):
    """One mound of cloud: four round puffs of mixed sizes about p0, one of another colour."""
    for j, (dx, dy, dz, f) in enumerate(((0, 0, 0, 1.0), (0.9, 0.35, -0.1, 0.78), (-0.85, 0.3, -0.15, 0.64),
                                         (0.1, -0.05, 0.75, 0.5))):
        s = s0 * f
        p = np.array([p0[0] + dx * s0, p0[1] + dy * s0, p0[2] + dz * s0])
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.88)), col if j != 2 else other, bone, fuse=False)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Sharpedo: called with the Mega's kit just before it is meshed."""
    # --- the snout's ridge spikes turned to gold crystal (sharpedomega.py's ridge0..2), a fourth on the crown ------
    for p in k.parts:
        if p["name"] in ("ridge0", "ridge1", "ridge2"):
            p["color"], p["glow"] = srgb(GOLD), GOLD
    b = np.array([0.0, -0.075, 0.37]); d = unit(0, -0.55, 0.84)
    e1, e2 = axes(d)
    k.add("crowncrystal", hull([b - d * 0.02 + e1 * 0.034, b - d * 0.02 - e1 * 0.034, b - d * 0.02 + e2 * 0.026,
                                b - d * 0.02 - e2 * 0.026, b + d * 0.085]), GOLD, "body", fuse=False, glow=GOLD)

    # --- the fins of ice ---------------------------------------------------------------------------------------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        o, ex, ez, n = frame((0.092 * s, -0.06, 0.212), (0.72 * s, 0.6, -0.34), (0.0, -0.15, 1.0))
        k.add(f"pecice{side}", slab(PEC_ICE, o, ex, ez, 0.042), ICE, f"fin{side}", fuse=False, glow=ICE)
        swirl(k, f"pecwater{side}", o, ex, ez, n, 0.05, 0.24, 0.048, 1.6, f"fin{side}")
    o, ex, ez, n = frame((0.0, 0.0, 0.352), (0.6, 0.8, 0.12), (0.0, 0.0, 1.0))
    k.add("dorsalice", slab(DORSAL_ICE, o, ex, ez, 0.042), ICE, "body", fuse=False, glow=ICE)
    o2, ex2, ez2, n2 = frame((0.0, 0.11, 0.2), (0.6, 0.8, 0.0), (0.0, 0.0, 1.0))
    k.add("ventralice", slab(VENTRAL_ICE, o2, ex2, ez2, 0.042), ICE, "tail", fuse=False, glow=ICE)
    # a stream of water rising up the dorsal sail's trailing edge
    pts = [o + ex * (0.13 + 0.03 * math.sin(t * 5)) + ez * (0.02 + 0.2 * t) + n * (0.03 * math.cos(t * 5))
           for t in np.linspace(0, 1, 16)]
    k.add("dorsalwater", tube(pts, [0.0085] * len(pts)), WATER, "body", fuse=False, glow=WATER)

    # --- the emblem on the snout's top, between the first two gold crystals, turned up and to +X -------------------
    nrm = unit(0.5, -0.2, 0.85)
    c = np.array([0.045, -0.245, 0.374]) + nrm * 0.007
    k.add("emblem", lathe(c, nrm, [(0, 0), (0.024, 0), (0.024, 0.008), (0, 0.008)]), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + nrm * 0.003, nrm, 0.031, 0.01, thick=0.009), RING, "body", fuse=False, glow=RING)

    # --- the clouds: a ring of mounds under the shark, on the ground, crimson and violet -----------------------------
    cols = (CRIMSON, VIOLET, DEEP, DVIOLET)
    n_ = 8
    for i in range(n_):
        a = 2 * math.pi * (i + 0.5) / n_
        x, y = 0.2 * math.cos(a), -0.06 + 0.3 * math.sin(a)
        clump(k, f"cloud{i}", (x, y, 0.042), 0.045, cols[i % 4], cols[(i + 1) % 4])
        if i % 3 == 0:
            k.add(f"curl{i}", ell((x * 1.05, y, 0.1), (0.016, 0.016, 0.014)), CYAN, "root", fuse=False, glow=CYAN)
    # a broken ring of crimson puffs floating over the back
    for i, a in enumerate((0.4, 1.5, 2.7, 3.9, 5.2)):
        p = np.array([0.16 * math.cos(a), 0.05 + 0.12 * math.sin(a), 0.5])
        k.add(f"halo{i}", ell(p, (0.027, 0.027, 0.022)), (CRIMSON, DEEP)[i % 2], "body")
        k.add(f"halo{i}b", ell(p + np.array([0.018 * math.cos(a + 1.6), 0.016 * math.sin(a + 1.6), 0.012]),
                                (0.018, 0.018, 0.016)), (CRIMSON, DEEP)[i % 2], "body")


_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)


Kit.build = build

m = sibling("sharpedomega")
m.sharpedomega(*args())
