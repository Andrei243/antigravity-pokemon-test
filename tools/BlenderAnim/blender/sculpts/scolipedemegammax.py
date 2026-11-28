# Scolipede-Mega-Mmax, Scolipede's Mega Dynamax form (plan 48), on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scolipedemegammax.py -- <the export's Scolipede-Mega-Mmax.glb> <out.glb>
# Mega Scolipede (scolipedemega.py) whole and unchanged, with what its Mega Dynamax form lays over it, from the
# project's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Scolipede.jpg): a ridge of glowing pink
# crystals thrust up and back along its spine, from the neck down onto the crimson rear, dripping; the Key Stone's
# emblem on its brow, a pale glowing ring with a four-pointed star; the Max clouds, which the picture draws smoky grey
# with crimson and violet in them, mounds about the feet with a few cyan curls, and crimson puffs floating over it.
# scolipedemega.py builds under __main__ with no function of its own: its main block is repeated here line for line
# (the base's eye colours, the three functions, the same build), with the additions laid in the body's build.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, slab, ring_band, unit, srgb, sibling, args

CRIMSON, DEEP, VIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#4EE2EC"
SMOKE, SMOKE_DARK = "#A49FB0", "#6E6880"           # the picture's grey clouds
PINK, DRIP, PINK_PALE = "#FA82E8", "#D25AE6", "#FFC4F6"
RING, STAR, DISC = "#F4D2FF", "#FFFFFF", "#7A3A8E"


def crystal(k, name, base, d, length, width, col, bone):
    """A six-sided crystal standing from base along d, its foot sunk a little into the body, a pointed tip."""
    d = unit(*d)
    k.add(name, lathe(base - d * 0.015, d, [(0, 0), (width, 0), (width * 1.05, length * 0.62), (0, length)], seg=6),
          col, bone, fuse=False, glow=col)


def crystals(k, m):
    vp = m.vp
    # along the armoured neck's spine: the reared segments, biggest in the middle
    for i, ((c, r, b), L) in enumerate(zip(m.REARED, (0.24, 0.27, 0.24, 0.19))):
        R = np.array(r) * m.SCALE
        p, n = vp.on_ell(np.array(c, float), R, (0, 0.85, 0.5))
        crystal(k, f"mmxtal{i}", p, n + np.array((0, 0.25, 0.45)), L, 0.05, PINK, b)
        for s in (1, -1):
            q, nq = vp.on_ell(np.array(c, float), R, (0.42 * s, 0.75, 0.5))
            crystal(k, f"mmxtal{i}{'R' if s > 0 else 'L'}", q, nq + np.array((0.25 * s, 0.1, 0.45)), L * 0.6, 0.036,
                    PINK_PALE if i % 2 else DRIP, b)
        # a drip hanging off the crystal's side
        crystal(k, f"mmdrip{i}", p + n * 0.03 + np.array((0.045, 0.01, 0.0)), (0.15, 0.2, -1), 0.09, 0.016, DRIP, b)
    # smaller ones along the crimson rear's top
    for i, ((c, r, b), L) in enumerate(zip(m.REAR[:4], (0.17, 0.15, 0.12, 0.1))):
        p, n = vp.on_ell(np.array(c, float), np.array(r), (0, 0.25, 1))
        crystal(k, f"mmrxtal{i}", p, n + np.array((0, 0.35, 0.2)), L, 0.035, PINK if i % 2 == 0 else DRIP, b)
        crystal(k, f"mmrdrip{i}", p + np.array((0.04, 0.0, -0.01)), (0.3, 0.1, -1), 0.07, 0.014, DRIP, b)


def emblem(k, m):
    """The Key Stone's emblem on the brow, between the horns' roots and above the beak, clear of the eyes."""
    p, n = m.vp.on_ell(np.array(m.HEAD_C, float), np.array(m.HEAD_R) * 1.14, (0, -0.72, 0.62))
    d = unit(*n)
    c = p + d * 0.002
    ex = np.cross((0, 0, 1.0), d); ex /= np.linalg.norm(ex)
    ez = np.cross(d, ex)
    k.add("mmembdisc", lathe(c, d, [(0, -0.02), (0.027, -0.02), (0.027, 0.003), (0, 0.003)], seg=32), DISC, "head",
          fuse=False, glow=DISC)
    k.add("mmembring", ring_band(c + d * 0.003, d, 0.032, 0.01, thick=0.009), RING, "head", fuse=False, glow=RING)
    star = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = 0.024 if i % 2 == 0 else 0.008
        star.append((r * math.cos(a), r * math.sin(a)))
    k.add("mmembstar", slab(star, c + d * 0.006, ex, ez, 0.004), STAR, "head", fuse=False, glow=STAR)


def clump(k, name, at, sizes, cols, bone):
    """A cloud: a clump of round fused puffs of mixed sizes, one small riding on top."""
    a = math.atan2(at[1] - 0.14, at[0])
    t_ = np.array((math.cos(a + math.pi / 2), math.sin(a + math.pi / 2), 0))
    r_ = np.array((math.cos(a), math.sin(a), 0))
    for j, (s, (dt, dr, h)) in enumerate(zip(sizes, ((0, 0, 0.75), (0.75, 0.02, 0.85), (-0.7, 0.01, 0.8), (0.1, -0.3, 1.9)))):
        p = np.array(at, float) + t_ * dt * sizes[0] + r_ * dr * sizes[0]
        p[2] = at[2] + s * h
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.88)), cols[j], bone)


def clouds(k, cx, cy, rx, ry, n, size):
    rnd = np.random.default_rng(545)
    for i in range(n):
        a = 2 * math.pi * (i + 0.25) / n
        at = (cx + rx * math.cos(a), cy + ry * math.sin(a), 0.0)
        c0 = (SMOKE, SMOKE_DARK, SMOKE)[i % 3]
        c1 = (CRIMSON, VIOLET, DEEP)[i % 3]
        sz = [x * size * (1 + 0.12 * rnd.uniform(-1, 1)) for x in (1.0, 0.76, 0.66, 0.48)]
        clump(k, f"mmcloud{i}", at, sz, (c0, c0, c1, c0), "root")
        if i % 3 == 1:
            k.add(f"mmcurl{i}", ell(np.array(at) + (0, 0, sz[0] * 1.75), (0.017, 0.017, 0.015)), CYAN, "root",
                  fuse=False, glow=CYAN)


def cloud_arc(k, name, at, a0, a1, n, size, col, bone):
    """An arc of cloud: puffs along a curve, overlapping, largest in the middle, a smaller one on each other."""
    for j in range(n):
        t = j / (n - 1)
        s = size * (0.65 + 0.35 * math.sin(math.pi * t))
        p = at(a0 + (a1 - a0) * t)
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.9)), col, bone)
        if j % 2 == 1:
            k.add(f"{name}_{j}t", ell(p + (0, 0, s * 0.75), (s * 0.6, s * 0.6, s * 0.55)), col, bone)


def maxed(k, m):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    crystals(k, m)
    emblem(k, m)
    clouds(k, 0.0, 0.14, 0.24, 0.42, 9, 0.072)
    # crimson puffs floating over and behind the head, clear of the horns
    for i, a0 in enumerate((0.35, 2.0)):
        cloud_arc(k, f"mmhalo{i}", lambda a: np.array((0.0, 0.02, 0.98)) + (0.15 * math.cos(a), 0.1 * math.sin(a),
                                                                          0.015 * math.sin(2 * a)),
                  a0, a0 + 1.1, 5, 0.045, (CRIMSON, DEEP)[i], "head")


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self, m)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("scolipedemega")

# scolipedemega.py's main block, as it stands there
src, path = args()
m.sc.CM, m.sc.CY = srgb(m.ARMOUR), srgb(m.EYE)
k = Kit(src, coat="shell")
m.body(k)
m.marks(k)
m.pieces(k)
k.build(voxel=0.007, smooth=3, band=0.004)
k.eye_states(lambda state: m.sc.eyes(k, state))
k.export(path)
