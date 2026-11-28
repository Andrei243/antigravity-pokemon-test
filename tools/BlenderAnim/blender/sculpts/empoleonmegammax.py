# Mega Dynamax Empoleon (a form of our own, plan 48) on the skeleton and clips of the game's own export of the form:
#   blender --background --factory-startup --python sculpts/empoleonmegammax.py -- <Empoleon-Mega-Mmax.glb> <out.glb>
# Mega Empoleon's Blender sculpt (empoleonmega.py, which builds as it is read) whole and unchanged, with what the form
# lays over it added to its kit just before it is meshed (the design is
# docs/art/reference/gemini/megadynamax/Mega-Dynamax-Empoleon.jpg; the game's sculptor is PokemonModels.MegaMax1.cs,
# Empoleon): the Mega's crown of steel prongs turned to a trident of glowing cyan crystal, barbed at the tips, the Key
# Stone's emblem glowing on its breast, clouds of Max energy about its feet with cyan lightning playing in them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, ring_band, tube, outward, unit, srgb, sibling

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
TRIDENT = "#6EEEF4"                                     # the sculptor's cyan crystal
STONE, RING = "#A8F4FF", "#24A8DC"            # a pale cyan stone in a deeper ring: white on the white shirt was lost


def barb(root, tip, w, d):
    """A flat-faced barb of crystal: a kite in section from a root w across and d deep to a point."""
    root, tip = np.array(root, float), np.array(tip, float)
    a = unit(*(tip - root))
    side = np.cross(a, (0, 1.0, 0)); side /= np.linalg.norm(side)
    fwd = np.cross(side, a)
    return hull([root + side * w, root - side * w, root + fwd * d, root - fwd * d, tip])


def bolt(k, name, pts, r):
    """A bolt of cyan lightning: a loose glowing jagged tube."""
    k.add(name, outward(tube(np.array(pts, float), [r] * len(pts), seg=8, per=2)), CYAN, "root", fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Empoleon: called with the Mega's kit just before it is meshed."""
    # The crown's steel prongs and gold spikes turned to cyan crystal, giving their own light
    for p in k.parts:
        if p["name"] in ("prongc", "prongR", "prongL", "spikeR", "spikeL"):
            p["color"], p["glow"] = srgb(TRIDENT), TRIDENT
    # Barbs at the prongs' heads: two out from the middle prong under its point, one in from each side prong's
    # (the prongs' lines from empoleonmega.py: the middle from (0, -0.1, 1.2) to (0, -0.07, 1.6), the side ones from
    # (0.085, -0.08, 1.2) to (0.19, -0.04, 1.49))
    for s, sd in ((1, "R"), (-1, "L")):
        k.add(f"barbc{sd}", barb((0.02 * s, -0.08, 1.41), (0.095 * s, -0.075, 1.53), 0.028, 0.024), TRIDENT, "head",
              fuse=False, glow=TRIDENT, surface="metal")
        k.add(f"barb{sd}", barb((0.155 * s, -0.055, 1.36), (0.1 * s, -0.05, 1.47), 0.026, 0.022), TRIDENT, "head",
              fuse=False, glow=TRIDENT, surface="metal")

    # The Key Stone's emblem on the breast, at the top of the shirt
    n = unit(0, -0.97, 0.22)
    c = np.array([0.0, -0.205, 0.76]) + n * 0.008
    k.add("emblem", lathe(c, n, [(0, 0), (0.045, 0), (0.045, 0.012), (0, 0.012)]), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + n * 0.004, n, 0.06, 0.016, thick=0.012), RING, "body", fuse=False, glow=RING)

    # The clouds about the feet: a ring of fused mounds on the root bone, crimson and violet by turns, wide behind the
    # cape's hem
    rnd = np.random.default_rng(41)
    n_ = 11
    for i in range(n_):
        a = 2 * math.pi * (i + 0.5) / n_
        col = (CRIMSON, VIOLET, DEEP, DVIOLET)[i % 4]
        other = (VIOLET, CRIMSON, DVIOLET, CRIMSON)[i % 4]
        ry = 0.32 if math.sin(a) < 0 else 0.44
        for j in range(4):
            s = 0.095 * (1.0, 0.78, 0.66, 0.52)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.18, -0.16, 0.05)[j]
            p = np.array([0.44 * math.cos(t), 0.02 + ry * math.sin(t), s * (0.95, 0.9, 0.9, 2.3)[j]])
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.92)), other if j == 2 else col, "root")
    # Lightning playing in them at the front corners
    bolt(k, "boltR", [(0.36, -0.2, 0.07), (0.4, -0.22, 0.15), (0.35, -0.24, 0.21), (0.41, -0.26, 0.3)], 0.024)
    bolt(k, "boltL", [(-0.38, -0.14, 0.07), (-0.33, -0.16, 0.14), (-0.39, -0.18, 0.2), (-0.34, -0.2, 0.27)], 0.024)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("empoleonmega")         # builds as it is read, with this script's own arguments
