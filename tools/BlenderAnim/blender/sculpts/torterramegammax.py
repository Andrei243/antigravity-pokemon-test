# Mega Dynamax Torterra (a form of our own, plan 48) on the skeleton and clips of the game's own export of the form:
#   blender --background --factory-startup --python sculpts/torterramegammax.py -- <Torterra-Mega-Mmax.glb> <out.glb>
# Mega Torterra's Blender sculpt (torterramega.py, which builds as it is read) whole and unchanged, with what the form
# lays over it added to its kit just before it is meshed (the design is
# docs/art/reference/gemini/megadynamax/Mega-Dynamax-Torterra.jpg; the game's sculptor is PokemonModels.MegaMax2.cs,
# TorterraMmax): the Mega's three stone mountains turned to violet crystal with rose caps where their snow was, more
# crystals thrust up among them, the Key Stone's emblem glowing on the brow and clouds of Max energy about the feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, ring_band, unit, axes, srgb, sibling

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
CRYSTAL, CRYSTAL2, ROSE = "#7A54D2", "#9A72E6", "#FAC4E6"        # the sculptor's violet and rose, a lighter violet
STONE, RING = "#F8CE54", "#FF78C8"

# From torterramega.py: the brow plate's middle and half sizes (it is turned 12 degrees, not counted here)
BROW, BROWR = np.array([0.0, -0.79, 0.665]), np.array([0.3, 0.29, 0.13])


def shard(root, tip, r, twist=0.0):
    """A crystal: a six-sided prism from root narrowing to a point at tip, flat-faced."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root
    L = np.linalg.norm(d)
    d = d / L
    a, b = axes(d)
    pts = []
    for t, rr, tw in ((0.0, r, 0.0), (0.5, r * 0.9, 0.5), (0.8, r * 0.5, 0.15)):
        for i in range(6):
            ang = twist + tw + i * math.pi / 3
            pts.append(root + d * L * t + (a * math.cos(ang) + b * math.sin(ang)) * rr)
    pts.append(tip)
    return hull(pts)


def clump(k, name, c, size, col, other, rnd, turn=0.0):
    """A cloud's mound: four round puffs of mixed sizes fused on the root bone, one small one riding on top."""
    c = np.array(c, float)
    for j, (dx, dy, f) in enumerate(((0, 0, 1.0), (0.9, 0.3, 0.78), (-0.85, 0.2, 0.68), (0.1, -0.1, 0.5))):
        s = size * f * (1 + 0.1 * rnd.uniform(-1, 1))
        ct, st = math.cos(turn), math.sin(turn)
        p = c + np.array([(dx * ct - dy * st) * size, (dx * st + dy * ct) * size, 0.0])
        p[2] = s * (0.95 if j < 3 else 2.4)
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.92)), other if j == 2 else col, "root")


def maxed(k):
    """What the Mega Dynamax form lays over Mega Torterra: called with the Mega's kit just before it is meshed."""
    # The Mega's mountains turned to crystal: the stone violet, the snow rose, both giving their own light
    for p in k.parts:
        if p["name"] in ("rock1", "rock2", "rock3"):
            p["color"], p["glow"] = srgb(CRYSTAL), CRYSTAL
        elif p["name"] in ("rock1snow", "rock2snow", "rock3snow"):
            p["color"], p["glow"] = srgb(ROSE), ROSE

    # More crystals thrust up among them: one between the two front mountains, one behind at the right, one at the back
    for nm, root, tip, r, col in (("mid", (0.02, -0.1, 0.8), (0.06, -0.14, 1.26), 0.1, CRYSTAL2),
                                  ("right", (0.34, 0.3, 0.72), (0.44, 0.34, 1.12), 0.09, CRYSTAL),
                                  ("back", (-0.12, 0.44, 0.72), (-0.18, 0.56, 1.06), 0.08, CRYSTAL2),
                                  ("small", (-0.42, -0.2, 0.72), (-0.54, -0.24, 0.92), 0.05, CRYSTAL2)):
        k.add(f"crystal{nm}", shard(root, tip, r, 0.4), col, "body", fuse=False, glow=col, surface="shell")
        k.add(f"crystal{nm}cap", shard(root + (np.array(tip) - root) * 0.7, tip, r * 0.42, 0.4), ROSE, "body",
              fuse=False, glow=ROSE, surface="shell")

    # The Key Stone's emblem on the front of the brow plate, above the beak, between the eyes
    d = unit(0, -0.93, 0.36)
    t = 1 / math.sqrt((d[1] / BROWR[1]) ** 2 + (d[2] / BROWR[2]) ** 2)
    n = unit(0, -0.85, 0.5)
    c = BROW + d * t + n * 0.03             # the plate is turned and smoothed: stood off so the ring shows whole
    k.add("emblem", lathe(c, n, [(0, 0), (0.058, 0), (0.058, 0.014), (0, 0.014)]), STONE, "head", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + n * 0.005, n, 0.076, 0.02, thick=0.016), RING, "head", fuse=False, glow=RING)

    # The clouds: mounds in front of the forelegs, behind the hind legs and between the legs at the sides, none at the
    # corners (the cell's board is tight on this long body), clear of the feet
    rnd = np.random.default_rng(23)
    spots = [(-0.45, -0.77), (-0.15, -0.8), (0.15, -0.8), (0.45, -0.77),
             (-0.44, 0.88), (-0.14, 0.92), (0.16, 0.92), (0.46, 0.88),
             (0.62, 0.03), (-0.62, 0.05)]
    for i, (x, y) in enumerate(spots):
        col = (CRIMSON, VIOLET, DEEP, DVIOLET)[i % 4]
        other = (VIOLET, CRIMSON, DVIOLET, CRIMSON)[i % 4]
        clump(k, f"cloud{i}", (x, y, 0), 0.13 if i < 8 else 0.11, col, other, rnd, turn=math.pi / 2 if i >= 8 else 0.0)
        if i % 2 == 0:
            k.add(f"curl{i}", ell((x + 0.05, y, 0.25), (0.042, 0.042, 0.038)), CYAN, "root", fuse=False, glow=CYAN)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("torterramega")         # builds as it is read, with this script's own arguments
