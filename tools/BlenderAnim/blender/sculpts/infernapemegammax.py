# Mega Dynamax Infernape (a form of our own, plan 48) on the skeleton and clips of the game's own export of the form:
#   blender --background --factory-startup --python sculpts/infernapemegammax.py -- <Infernape-Mega-Mmax.glb> <out.glb>
# Mega Infernape's Blender sculpt (infernapemega.py, which builds as it is read) whole and unchanged, with what the form
# lays over it added to its kit just before it is meshed (the design is
# docs/art/reference/gemini/megadynamax/Mega-Dynamax-Infernape.jpg; the game's sculptor is PokemonModels.MegaMax2.cs,
# InfernapeMmax): its mane of fire grown out in long thick tongues streaming back over its shoulders and down its back,
# a great ring of fire round each forearm with a tongue rising from it, the Key Stone's emblem glowing in the middle of
# its chest, clouds of Max energy about its feet and a broken ring of crimson puffs floating about its head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, ring_band, unit, srgb, sibling

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
FIRE, FIREGLOW = "#F26028", "#FF6A2A"                 # infernapemega.py's wrists' fire
STONE, RING = "#FFF2B0", "#FF4E86"                    # a pale heart in a rose ring, apart from the gold guards

# From infernapemega.py: its frame's stretch to the form's (only the height), its wrist and hand
K = np.array([1.0, 1.0, 1.06])
WRIST = (np.array([0.317, -0.016, 0.485]) + np.array([0.332, -0.025, 0.415])) / 2 * K


class Plain:
    """The Mega's kit seen through the plain Kit's add: what is added here is in the form's own frame, not stretched."""

    def __init__(self, k):
        self.k = k

    def add(self, *a, **kw):
        Kit.add(self.k, *a, **kw)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Infernape: called with the Mega's kit just before it is meshed."""
    cq = sibling("cyndaquil")
    pk = Plain(k)
    f = cq.Flames()

    def tongue(root, d, length, width, bone, flick=0.25, side=None):
        d = unit(*d)
        if side is None:
            side = np.cross(d, (0, 0, 1.0))
            if np.linalg.norm(side) < 1e-3:
                side = np.array([1.0, 0, 0])
        f.flame(np.array(root, float), d, length, width, bone, unit(*side), flick=flick)

    # The mane grown out: long thick tongues streaming back from the crown, over the shoulders and down the back
    tongue((0.0, 0.03, 1.24), (0, 1.0, 0.55), 0.56, 0.11, "flame")                  # the highest, back from the crown
    for s in (1, -1):
        tongue((0.09 * s, 0.05, 1.2), (0.45 * s, 1.0, 0.2), 0.6, 0.1, "flame")      # back and out from the crown
        tongue((0.15 * s, 0.06, 1.1), (0.75 * s, 1.0, -0.15), 0.52, 0.1, "flame")    # out over the shoulders
        tongue((0.1 * s, 0.12, 1.0), (0.35 * s, 1.0, -0.75), 0.6, 0.1, "body")       # down the back
    tongue((0.0, 0.14, 0.98), (0, 1.0, -0.6), 0.66, 0.11, "body")                    # down the spine

    # A great ring of fire round each forearm above the Mega's, a tongue rising from it beside the arm
    for s, bone in ((1, "armR"), (-1, "armL")):
        a = np.array([0.31 * s, -0.012, 0.53]) * K
        b = np.array([0.325 * s, -0.02, 0.46]) * K
        ax = b - a
        pk.add(f"mmfire{bone}", ring_band((a + b) / 2, ax, 0.085, 0.07, thick=0.02), FIRE, bone, fuse=False,
               glow=FIREGLOW)
        tongue((a + b) / 2 + np.array([0.07 * s, 0.0, 0.02]), (0.25 * s, 0.1, 1.0), 0.26, 0.07, bone, flick=0.3,
               side=(0, 1, 0))
    f.add(pk)

    # The Key Stone's emblem in the middle of the chest, between the Mega's two little chest guards
    n = unit(0, -1, 0.12)
    c = np.array([0.0, -0.124, 0.765]) * K + n * 0.008
    pk.add("emblem", lathe(c, n, [(0, 0), (0.032, 0), (0.032, 0.01), (0, 0.01)]), STONE, "body", fuse=False, glow=STONE)
    pk.add("emblemring", ring_band(c + n * 0.004, n, 0.043, 0.012, thick=0.01), RING, "body", fuse=False, glow=RING)

    # The clouds about the feet: a ring of fused mounds on the root bone, crimson and violet by turns
    rnd = np.random.default_rng(31)
    n_ = 10
    for i in range(n_):
        a = 2 * math.pi * (i + 0.5) / n_
        col = (CRIMSON, VIOLET, DEEP, DVIOLET)[i % 4]
        other = (VIOLET, CRIMSON, DVIOLET, CRIMSON)[i % 4]
        for j in range(4):
            s = 0.06 * (1.0, 0.78, 0.66, 0.52)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.2, -0.18, 0.05)[j]
            p = np.array([0.33 * math.cos(t), -0.02 + 0.3 * math.sin(t), s * (0.95, 0.9, 0.9, 2.3)[j]])
            pk.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.92)), other if j == 2 else col, "root")
        if i % 3 == 1:
            q = np.array([0.33 * math.cos(a), -0.02 + 0.3 * math.sin(a), 0.11])
            pk.add(f"curl{i}", ell(q, (0.022, 0.022, 0.02)), CYAN, "root", fuse=False, glow=CYAN)

    # A broken ring of crimson puffs floating about the head
    for i, (x, y, z, r) in enumerate(((0.3, -0.06, 1.28, 0.045), (0.25, 0.14, 1.36, 0.038), (-0.3, -0.04, 1.3, 0.045),
                                      (-0.24, 0.16, 1.38, 0.036))):
        for j, (dx, dz, f) in enumerate(((0.0, 0.0, 1.0), (0.9, -0.25, 0.8), (-0.85, -0.2, 0.75), (0.15, 0.6, 0.6))):
            pk.add(f"halo{i}_{j}", ell((x + dx * r * math.copysign(1, x), y + 0.3 * dx * r, z + dz * r), (r * f,) * 3),
                   (CRIMSON, DEEP, CRIMSON, DEEP)[(i + j) % 4], "body", fuse=False)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("infernapemega")        # builds as it is read, with this script's own arguments
