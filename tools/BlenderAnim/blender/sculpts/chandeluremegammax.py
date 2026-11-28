# Chandelure-Mega-Mmax, Chandelure's Mega Dynamax form (plan 48), on the skeleton and clips of the game's own export of
# it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chandeluremegammax.py -- <the export's Chandelure-Mega-Mmax.glb> <out.glb>
# Mega Chandelure (chandeluremega.py) whole and unchanged, with what its Mega Dynamax form lays over it, from the
# project's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Chandelure.jpg): the tall flame over the globe
# grown into a great violet blaze, broad at its foot with a tongue curling out to each side and a pale cyan heart; the
# Key Stone's emblem lit on the glass, under the eyes' bar (the picture shows it in the globe's middle, where the bar
# and the eyes are: here it is set below them, small, so the face stays as it is); the Max clouds, crimson and violet
# mounds in a ring round the hanging point, under the lower tier, with a few cyan curls.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, blade, lathe, slab, ring_band, unit, srgb, sibling, args, outward

CRIMSON, DEEP, VIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#4EE2EC"
BLAZE, BLAZE_LIGHT, HEART = "#9664EC", "#B898FA", "#8CE6F4"
RING, STAR, DISC = "#FFC8FF", "#FFFFFF", "#4EC8E0"


def blaze(k):
    """The great flame: broad tongues round the Mega's own tall flame, which stands in their middle as it was."""
    # the broad main tongue, a little behind the Mega's flame so that it shows in front of it
    main = [(0.0, 0.012, 0.455), (0.012, 0.012, 0.51), (-0.015, 0.012, 0.57), (0.005, 0.012, 0.63), (0.03, 0.012, 0.69),
            (0.03, 0.012, 0.735), (0.01, 0.012, 0.765)]
    k.add("mmblaze", outward(blade(main, [0.08, 0.085, 0.075, 0.06, 0.044, 0.027, 0.011], (0, 1, 0), 0.5)), BLAZE,
          "head", fuse=False, glow=BLAZE)
    # a tongue licking out to each side from the blaze's foot and curling up, the +X one higher (the front camera's)
    for s, side, top in ((1, "R", 0.66), (-1, "L", 0.62)):
        P = [(0.03 * s, 0.004, 0.47), (0.075 * s, 0.004, 0.51), (0.105 * s, 0.004, 0.555), (0.125 * s, 0.004, top - 0.03),
             (0.135 * s, 0.004, top)]
        k.add(f"mmtongue{side}", outward(blade(P, [0.04, 0.038, 0.028, 0.016, 0.007], (0, 1, 0), 0.5)), BLAZE_LIGHT,
              "head", fuse=False, glow=BLAZE_LIGHT)
    # two short tongues behind, so the back camera sees a blaze and not a plate
    for s, side in ((1, "R"), (-1, "L")):
        P = [(0.02 * s, 0.035, 0.46), (0.05 * s, 0.045, 0.53), (0.045 * s, 0.05, 0.6), (0.065 * s, 0.05, 0.645)]
        k.add(f"mmback{side}", outward(blade(P, [0.04, 0.034, 0.02, 0.008], unit(0.3 * s, 1, 0), 0.5)), BLAZE,
              "head", fuse=False, glow=BLAZE)
    # the pale cyan heart, in front of the Mega's flame at its foot
    heart = [(0.0, -0.03, 0.46), (0.008, -0.03, 0.5), (-0.006, -0.03, 0.54), (0.004, -0.03, 0.575)]
    k.add("mmheart", outward(blade(heart, [0.03, 0.027, 0.018, 0.007], (0, 1, 0), 0.5)), HEART, "head", fuse=False,
          glow=HEART)


def emblem(k, m):
    """The Key Stone's emblem lit on the glass's front, under the bar between the eyes and above the dish."""
    p, n = m.ch.on_glass(0.0, -0.028)
    d = unit(*n)
    c = p + d * 0.001
    ex = np.cross((0, 0, 1.0), d); ex /= np.linalg.norm(ex)
    ez = np.cross(d, ex)
    k.add("mmembdisc", lathe(c, d, [(0, -0.012), (0.015, -0.012), (0.015, 0.002), (0, 0.002)], seg=32), DISC, "body",
          fuse=False, glow=DISC)
    k.add("mmembring", ring_band(c + d * 0.002, d, 0.0175, 0.008, thick=0.006), RING, "body", fuse=False, glow=RING)
    star = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = 0.014 if i % 2 == 0 else 0.0045
        star.append((r * math.cos(a), r * math.sin(a)))
    k.add("mmembstar", slab(star, c + d * 0.0045, ex, ez, 0.003), STAR, "body", fuse=False, glow=STAR)


def clump(k, name, at, sizes, cols, bone):
    """A cloud: a clump of round fused puffs of mixed sizes, one small riding on top."""
    a = math.atan2(at[1], at[0])
    t_ = np.array((math.cos(a + math.pi / 2), math.sin(a + math.pi / 2), 0))
    r_ = np.array((math.cos(a), math.sin(a), 0))
    for j, (s, (dt, dr, h)) in enumerate(zip(sizes, ((0, 0, 0.0), (0.75, 0.02, 0.1), (-0.7, 0.01, 0.05), (0.1, -0.3, 0.9)))):
        p = np.array(at, float) + t_ * dt * sizes[0] + r_ * dr * sizes[0]
        p[2] = at[2] + s * h
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.88), sub=3), cols[j], bone)


def clouds(k, rx, ry, z, n, size):
    """A ring of cloud round the hanging point, under the lower tier: the Mega hovers, so it floats with its puffs."""
    rnd = np.random.default_rng(609)
    for i in range(n):
        a = 2 * math.pi * (i + 0.25) / n
        at = (rx * math.cos(a), ry * math.sin(a), z + 0.012 * math.sin(3 * a))
        c0, c1 = (CRIMSON, VIOLET, DEEP)[i % 3], (VIOLET, DEEP, CRIMSON)[i % 3]
        sz = [x * size * (1 + 0.12 * rnd.uniform(-1, 1)) for x in (1.0, 0.76, 0.66, 0.48)]
        clump(k, f"mmcloud{i}", at, sz, (c0, c0, c1, c0), "root")
        if i % 3 == 1:
            k.add(f"mmcurl{i}", ell(np.array(at) + (0, 0, sz[0] * 1.2), (0.012, 0.012, 0.01)), CYAN, "root",
                  fuse=False, glow=CYAN)


def maxed(k, m):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    blaze(k)
    emblem(k, m)
    clouds(k, 0.2, 0.2, 0.1, 9, 0.042)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self, m)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("chandeluremega")
m.chandelure_mega(*args())
