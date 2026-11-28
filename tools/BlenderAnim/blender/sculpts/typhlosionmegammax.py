# Mega Dynamax Typhlosion (a form of our own, plan 48) on the skeleton and clips of the game's own export of the form:
#   blender --background --factory-startup --python sculpts/typhlosionmegammax.py -- <Typhlosion-Mega-Mmax.glb> <out.glb>
# Mega Typhlosion's Blender sculpt (typhlosionmega.py) whole and unchanged, with what the form lays over it added to its
# kit just before it is meshed (the design is docs/art/reference/gemini/megadynamax/Mega-Dynamax-Typhlosion.jpg; the
# game's sculptor is PokemonModels.MegaMax2.cs, TyphlosionMmax): the crater's three tongues of fire grown into great
# blades of flame-coloured crystal (the Mega's own flames recoloured to the crystal's, as their cores), the Key Stone's
# emblem glowing on the brow, clouds of Max energy rolling about the feet and wisps of it by the shoulders.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, outward, unit, axes, srgb, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
PEACH, HEART = "#FF8A46", "#FFC46A"
STONE, RING = "#F8CE54", "#FF78C8"

# From typhlosionmega.py: the form's stretch of Typhlosion's frame, its head and the crater's middle (unstretched)
K = np.array([1.1, 1.1, 0.94])
H, HR = np.array([0.0, -0.08, 0.75]) * K, np.array([0.112, 0.114, 0.1]) * 1.04 * K
CR = np.array([0.0, 0.11, 0.555])


def shard(root, tip, r, twist=0.0):
    """A blade of crystal: a six-sided prism from root narrowing to a point at tip, flat-faced."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root
    L = np.linalg.norm(d)
    d = d / L
    a, b = axes(d)
    pts = []
    for t, rr, tw in ((0.0, r, 0.0), (0.45, r * 0.92, 0.5), (0.78, r * 0.55, 0.15)):
        for i in range(6):
            ang = twist + tw + i * math.pi / 3
            pts.append(root + d * L * t + (a * math.cos(ang) + b * math.sin(ang)) * rr)
    pts.append(tip)
    return hull(pts)


def clouds(k, cx, cy, rx, ry, n, size, seed, skip=()):
    """A ring of fused mounds about the feet on the root bone: clumps of round puffs, crimson and violet by turns."""
    rnd = np.random.default_rng(seed)
    for i in range(n):
        if i in skip:
            continue
        a = 2 * math.pi * (i + 0.5) / n
        col = (CRIMSON, VIOLET, DEEP, DVIOLET)[i % 4]
        other = (VIOLET, CRIMSON, DVIOLET, CRIMSON)[i % 4]
        for j in range(4):
            s = size * (1.0, 0.78, 0.66, 0.52)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.22, -0.2, 0.06)[j] * (0.3 / max(rx, ry))
            f = (1.0, 1.02, 1.0, 0.96)[j]
            p = np.array([cx + rx * f * math.cos(t), cy + ry * f * math.sin(t), s * (0.95, 0.9, 0.9, 2.3)[j]])
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.92)), other if j == 2 else col, "root")
        if i % 3 == 1:
            q = np.array([cx + rx * math.cos(a), cy + ry * math.sin(a), size * 1.9])
            k.add(f"curl{i}", ell(q, (size * 0.38, size * 0.38, size * 0.34)), CYAN, "root", fuse=False, glow=CYAN)


def wisp(k, name, pts, r, bone):
    """A wisp of Max energy streaming off: a trail of loose round puffs along pts, shrinking, a cyan bead at its end."""
    pts = np.array(pts, float)
    for i, p in enumerate(pts):
        s = r * (1.0, 0.85, 0.7, 0.55)[i]
        k.add(f"{name}_{i}", ell(p, (s, s, s * 0.9)), (VIOLET, CRIMSON, DVIOLET, VIOLET)[i], bone, fuse=False)
    k.add(name + "bead", ell(pts[-1] + (pts[-1] - pts[-2]) * 0.8, (r * 0.4,) * 3), CYAN, bone, fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Typhlosion: called with the Mega's kit just before it is meshed."""
    k.stretch = False                       # the Mega's Stretched kit: what is added here is in the form's own frame

    # The Mega's own tongues of fire in the crater turned to the crystal's colours: they are the blades' cores
    for p in k.parts:
        if p["name"].startswith("flame") and p["bone"] == "flame":
            core = p["glow"] == "#FFD860"   # cyndaquil.py's GLOW_CORE; its flames' outer glow is GLOW_FLAME
            p["color"] = srgb(HEART if core else PEACH)
            p["glow"] = HEART if core else PEACH

    # Three great blades of crystal standing up out of the crater, the middle one tallest, and small shards among them
    for nm, root, tip, r, tw in (("mid", (0.0, 0.115, 0.57), (0.0, 0.17, 1.0), 0.078, 0.0),
                                 ("R", (0.08, 0.12, 0.57), (0.16, 0.17, 0.9), 0.062, 0.4),
                                 ("L", (-0.08, 0.12, 0.57), (-0.16, 0.17, 0.9), 0.062, -0.4)):
        k.add(f"crystal{nm}", shard(root, tip, r, tw), PEACH, "flame", fuse=False, glow=PEACH, surface="shell")
    for nm, root, tip, r in (("fr", (0.05, 0.04, 0.58), (0.075, 0.0, 0.76), 0.03),
                             ("fl", (-0.05, 0.04, 0.58), (-0.075, 0.0, 0.76), 0.03),
                             ("bk", (0.03, 0.2, 0.58), (0.05, 0.27, 0.8), 0.035)):
        k.add(f"shard{nm}", shard(root, tip, r, 0.3), HEART, "flame", fuse=False, glow=HEART, surface="shell")

    # The Key Stone's emblem on the brow: a glowing disc in a ring of another colour, a hair off the fur
    d = unit(0, -0.6, 0.8)
    n = unit(0, -0.48, 0.88)
    t = 1 / math.sqrt((d[1] / HR[1]) ** 2 + (d[2] / HR[2]) ** 2)
    c = H + d * t + n * 0.004
    k.add("emblem", lathe(c, n, [(0, 0), (0.024, 0), (0.024, 0.008), (0, 0.008)]), STONE, "head", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + n * 0.003, n, 0.031, 0.01, thick=0.008), RING, "head", fuse=False, glow=RING)

    # The clouds about the feet, a gap where the tail lies on the ground behind
    clouds(k, 0.0, -0.02, 0.3, 0.28, 10, 0.058, 17, skip=(2,))

    # Wisps of it rising behind the shoulders
    for s in (1, -1):
        wisp(k, f"wisp{s}", [(0.235 * s, 0.13, 0.57), (0.26 * s, 0.155, 0.595), (0.275 * s, 0.175, 0.62), (0.28 * s, 0.19, 0.64)],
             0.038, "body")

    k.stretch = True                        # back as the Mega left it: its eyes' five states are laid after this


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("typhlosionmega")
m.typhlosion_mega(*args())
