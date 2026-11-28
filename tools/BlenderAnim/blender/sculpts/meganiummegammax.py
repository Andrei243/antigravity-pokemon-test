# Mega Dynamax Meganium (Meganium-Mega-Mmax), a form of this project's own: Mega Meganium (meganiummega.py) whole and
# unchanged, with what the form lays over it added to its kit just before the body is meshed:
#   blender --background --factory-startup --python sculpts/meganiummegammax.py -- <the export's Meganium-Mega-Mmax.glb> <out.glb>
#
# What the form adds, from its picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Meganium.jpg), largest first:
#   1. a great sun of golden petals standing behind the rosette, reaching out past it on every side but below;
#   2. the rosette's red and pink tiers turned to glowing crystal, and the feelers' loops lit gold;
#   3. the clouds of Max energy about the feet, and a broken ring of crimson puffs about the feelers;
#   4. the Key Stone's emblem on the chest under the rosette.
# The clouds are a mesh of their own, built before the body, so that the Mega's fused body is meshed exactly as it is.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, ring_band, hull, unit, srgb, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
SUN, SUN2 = "#E8961C", "#F2B834"
STONE, RING = "#FFF0B4", "#4EE2EC"


# A mound's puffs: (along the ring, outward, size, height of its middle in its own size): a big one, two at its sides,
# one behind, one small riding on top; every one standing on the ground or above it
PUFFS = ((0, 0, 1.0, 0.95), (0.95, 0.15, 0.8, 0.95), (-0.9, 0.25, 0.75, 0.95), (0.15, 0.6, 0.72, 0.95),
         (0.1, 0.05, 0.6, 2.2))


def clump(k, name, c, size, cols, bone="root", flat=0.9):
    """A mound of cloud: four round puffs of mixed sizes, one small one riding on top."""
    c = np.array(c, float)
    o = unit(c[0], c[1] - 0.06, 0) if np.hypot(c[0], c[1] - 0.06) > 1e-6 else np.array([1.0, 0, 0])
    tg = np.array([-o[1], o[0], 0.0])
    for j, (dt, dr, f, h) in enumerate(PUFFS):
        s = size * f
        p = c + tg * dt * size + o * dr * size + np.array([0, 0, s * h])
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.92)), cols[j % len(cols)], bone)


def clouds(k, m):
    """A mesh of its own: mounds in an oval about the four feet, and a broken ring of crimson puffs about the feelers."""
    n = 8
    for i in range(n):
        a = 2 * math.pi * (i + 0.25) / n
        cc = (0.28 * math.cos(a), 0.06 + 0.34 * math.sin(a), 0.0)
        cols = [(CRIMSON, DEEP, VIOLET, CRIMSON), (VIOLET, DVIOLET, CRIMSON, VIOLET), (DEEP, CRIMSON, DVIOLET, DEEP)][i % 3]
        clump(k, f"cloud{i}", cc, 0.068, cols)
        if i % 3 == 1:
            k.add(f"curl{i}", ell(np.array(cc) + (0, 0, 0.135), (0.022, 0.022, 0.02), sub=3), CYAN, "root", fuse=False,
                  glow=CYAN)
    # the ring over the head, about the feelers: four short arcs of puffs with gaps between
    top = m.H + np.array([0, 0.02, 0.2])
    for j in range(4):
        for q in range(3):
            a = 2 * math.pi * (j / 4 + 0.05 + q * 0.055)
            p = top + np.array([0.17 * math.cos(a), 0.13 * math.sin(a), 0.012 * math.sin(3 * a)])
            r = (0.028, 0.034, 0.024)[q]
            k.add(f"halo{j}_{q}", ell(p, (r, r, r * 0.85)), (CRIMSON, DEEP, CRIMSON)[q], "head")


def petal(base, d, side, length, width, thick):
    """A broad pointed petal of gold: a flat-faced solid from its root along d, broad across side."""
    n = np.cross(d, side)
    mid = base + d * length * 0.45
    return hull([base + side * width * 0.3, base - side * width * 0.3, base + n * thick / 2, base - n * thick / 2,
                 mid + side * width / 2, mid - side * width / 2, mid + n * thick / 2, mid - n * thick / 2,
                 base + d * length])


def maxed(k, m):
    """What the Mega Dynamax form lays over Mega Meganium: called with the Mega's kit just before it is meshed."""
    # 1. the sun: golden petals fanned behind the rosette in its own plane, an oval wider than it is tall (the height
    # is kept under the feelers'), from straight down on the left round over the top to straight down on the right
    FN = m.FN
    e1 = np.cross(FN, (0, 0, 1.0)); e1 /= np.linalg.norm(e1)      # across
    e2 = np.cross(FN, e1)                                          # up in the rosette's plane
    if e2[2] < 0:
        e2 = -e2
    heart = np.array([0, 0.04, 0.6])
    n = 19
    for i in range(n):
        a = math.radians(-108 + 216 * i / (n - 1))                 # from up, round to each side
        d = math.sin(a) * e1 + math.cos(a) * e2
        reach = 1 / math.sqrt((math.sin(a) / 0.56) ** 2 + (math.cos(a) / 0.4) ** 2)
        reach *= 1.0 + 0.07 * (i % 2)
        side = np.cross(FN, d)
        base = heart + d * 0.17 - FN * (0.012 * (i % 2))
        k.add(f"sun{i}", petal(base, d, side, reach - 0.17, 0.12, 0.03), (SUN, SUN2)[i % 2], "body", fuse=False,
              glow=(SUN, SUN2)[i % 2], surface="leaf")

    # 2. the rosette's red and pink tiers as glowing crystal, the feelers lit
    for p in k.parts:
        nm = p["name"]
        if nm.startswith("red") and not nm.startswith("redstreak"):
            p["glow"] = m.RED
        elif nm.startswith("pink") and nm[4:].isdigit():
            p["glow"] = m.PINK
        elif nm.startswith("feeler"):
            p["color"], p["glow"] = srgb("#FFD84A"), "#FFD84A"

    # 4. the emblem on the chest, under the rosette: a pale glowing disc in a cyan ring, a hair off the chest
    d = unit(0, -1, 0.12)
    c = np.array([0.0, -0.206, 0.3])
    k.add("emblem", lathe(c, d, [(0, 0.0), (0.03, 0.0), (0.03, 0.009), (0, 0.011)]), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.038, 0.012, thick=0.011), RING, "body", fuse=False, glow=RING)


m = sibling("meganiummega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        keep = self.parts, self.paints, self.decals
        self.parts, self.paints, self.decals = [], [], []
        clouds(self, m)
        _build(self, voxel=0.009, smooth=4, band=0.006, name="maxclouds")
        self.parts, self.paints, self.decals = keep
        maxed(self, m)
    return _build(self, *a, **kw)
Kit.build = build

m.meganium_mega(*args())
