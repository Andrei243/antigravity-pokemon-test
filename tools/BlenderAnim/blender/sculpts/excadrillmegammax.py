# Excadrill-Mega-Mmax, Excadrill's Mega Dynamax form (plan 48), on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/excadrillmegammax.py -- <the export's Excadrill-Mega-Mmax.glb> <out.glb>
# Mega Excadrill (excadrillmega.py) whole and unchanged, with what its Mega Dynamax form lays over it, from the
# project's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Excadrill.jpg): the two arm drills turned to
# violet crystal, glowing, ringed by spirals of violet light; the Key Stone's emblem on the chest, a dark disc with a
# white four-pointed star in a pale ring set round with spikes like a sun; the Max clouds, crimson and violet mounds
# about the feet with a few cyan curls, a broken ring of crimson puffs about the head's drill and a wisp at each
# shoulder. The head's drill stays steel, as the picture has it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, slab, ring_band, unit, srgb, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
CRYSTAL, CRYSTAL_LIGHT, CRYSTAL_DEEP = "#B078F6", "#D6B8FF", "#8C52E0"
TOOTH, LIGHT = "#F0C8FF", "#B47CFF"
DISC, STAR, RING = "#3C1E58", "#FFF4FF", "#DCB4FF"

# From excadrillmega.py: the arm drill's base, direction, length and radius, and its radius along it (mega_drill's rad)
ARM_BASE, ARM_DIR, ARM_LEN, ARM_R = np.array((0.215, -0.15, 0.18)), (0.335, -0.58, 0.743), 0.4, 0.086
FEET = (0.11, -0.065)                       # each foot's middle (x, y), from the Mega's legs


def drill_radius(s):
    t = min(max(s / ARM_LEN, 0), 1)
    return ARM_R * (1 - t) ** 0.85 if t > 0.12 else ARM_R * (0.93 + 0.07 * t / 0.12)


def crystal_drills(k):
    """The Mega's own arm drills recoloured to glowing violet crystal (their shapes untouched), ringed in light."""
    for p in k.parts:
        nm = p["name"]
        if not nm.startswith("drill"):
            continue
        rest = nm[6:]                       # after drillR / drillL
        if rest == "cap":
            col = CRYSTAL_DEEP
        elif rest.startswith("b"):
            col = CRYSTAL if int(rest[1:]) % 2 == 0 else CRYSTAL_LIGHT
        elif rest.startswith("f"):
            col = TOOTH
        else:
            col = CRYSTAL_LIGHT
        p["color"], p["glow"] = srgb(col), col
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        d = unit(*(np.array(ARM_DIR) * m))
        base = ARM_BASE * m
        e = np.array((0.25 * s, 0.0, -1)); e = e - d * (e @ d); e /= np.linalg.norm(e)
        for i, sd in enumerate((0.09, 0.19, 0.29)):
            axis = unit(*(d + e * 0.28 * (1 if i % 2 == 0 else -1)))      # tilted by turns: a spiral of light
            k.add(f"spiral{side}{i}", ring_band(base + d * sd, axis, drill_radius(sd) + 0.018, 0.018, thick=0.018),
                  LIGHT, f"arm{side}", fuse=False, glow=LIGHT)


def emblem(k):
    """
    The Key Stone's emblem on the chest, over the Mega's grey triangle (the picture puts it there): a dark disc whose
    drum runs back into the belly, a white four-pointed star, a pale ring set round with spikes leaning back to the body.
    """
    c = np.array((0.0, -0.153, 0.2))
    d = unit(0, -1, 0.0)
    # the drum: from the plate's front back into the belly (about y -0.12 there), so nothing stands in the air
    k.add("emblemdisc", lathe(c, d, [(0, -0.045), (0.031, -0.045), (0.031, 0.004), (0, 0.004)], seg=40), DISC, "body",
          fuse=False, glow="#6A3A9A")
    star = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = 0.027 if i % 2 == 0 else 0.009
        star.append((r * math.cos(a), r * math.sin(a)))
    k.add("emblemstar", slab(star, c + d * 0.0065, np.array((1.0, 0, 0)), np.array((0, 0, 1.0)), 0.004), STAR, "body",
          fuse=False, glow=STAR)
    k.add("emblemring", ring_band(c + d * 0.001, d, 0.04, 0.014, thick=0.01), RING, "body", fuse=False, glow=RING)
    for i in range(10):
        a = 2 * math.pi * (i + 0.5) / 10
        r_, t_ = np.array((math.cos(a), 0, math.sin(a))), np.array((-math.sin(a), 0, math.cos(a)))
        b = c + r_ * 0.043
        k.add(f"emblemspike{i}", hull([b + t_ * 0.009 + (0, 0.004, 0), b - t_ * 0.009 + (0, 0.004, 0),
                                       b + t_ * 0.009 + (0, -0.008, 0), b - t_ * 0.009 + (0, -0.008, 0),
                                       c + r_ * 0.064 + (0, 0.012, 0)]), RING, "body", fuse=False, glow=RING)


def clump(k, name, at, sizes, cols, bone):
    """A cloud: a clump of round fused puffs of mixed sizes, one small riding on top."""
    a = math.atan2(at[1], at[0])
    for j, (s, (dt, dr, h)) in enumerate(zip(sizes, ((0, 0, 0.75), (0.75, 0.02, 0.85), (-0.7, 0.01, 0.8), (0.1, -0.3, 1.9)))):
        p = np.array(at, float) + np.array((math.cos(a + math.pi / 2), math.sin(a + math.pi / 2), 0)) * dt * sizes[0] \
            + np.array((math.cos(a), math.sin(a), 0)) * dr * sizes[0]
        p[2] = at[2] + s * h
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.88)), cols[j], bone)


def clouds(k):
    rnd = np.random.default_rng(530)
    # a rounded square more than a ring, so that the clouds stand 0.07 or more off the thighs at the corners too
    # (fused parts nearer than the kit's weight band lend one another their bones)
    cx, cy, rx, ry, n = 0.0, -0.06, 0.3, 0.24, 9
    for i in range(n):
        a = 2 * math.pi * (i + 0.25) / n
        f = 1 + 0.12 * math.sin(2 * a) ** 2
        at = (cx + rx * f * math.cos(a), cy + ry * f * math.sin(a), 0.0)
        c0 = (CRIMSON, VIOLET, DEEP)[i % 3]
        c1 = (VIOLET, DEEP, CRIMSON)[i % 3]
        sz = [x * (1 + 0.12 * rnd.uniform(-1, 1)) for x in (0.055, 0.042, 0.036, 0.026)]
        clump(k, f"cloud{i}", at, sz, (c0, c0, c1, c0), "root")
        if i % 3 == 1:
            q = np.array(at) + np.array((0, 0, sz[0] * 1.75))
            k.add(f"curl{i}", ell(q, (0.02, 0.02, 0.017)), CYAN, "root", fuse=False, glow=CYAN)
    # a broken ring of crimson cloud floating about the head's drill: three arcs of overlapping puffs
    for i, a0 in enumerate((0.2, 2.3, 4.4)):
        cloud_arc(k, f"halo{i}", lambda a: np.array((0.0, 0.09, 0.62)) + (0.16 * math.cos(a), 0.135 * math.sin(a),
                                                                       0.02 * math.sin(a)),
                  a0, a0 + 1.3, 5, 0.036, (CRIMSON, DEEP, CRIMSON)[i], "head")
    # a wisp of cloud curling up off each shoulder
    for s, side in ((1, "R"), (-1, "L")):
        cloud_arc(k, f"wisp{side}", lambda a, s=s: np.array((0.2 * s, 0.1, 0.44)) + (0.07 * s * math.cos(a), 0.0,
                                                                                0.07 * math.sin(a)),
                  -1.6, 1.4, 5, 0.036, CRIMSON if s > 0 else VIOLET, "body")


def cloud_arc(k, name, at, a0, a1, n, size, col, bone):
    """A wisp or an arc of cloud: puffs along a curve, overlapping, largest in the middle, a smaller one on each."""
    for j in range(n):
        t = j / (n - 1)
        s = size * (0.6 + 0.4 * math.sin(math.pi * t))
        p = at(a0 + (a1 - a0) * t)
        k.add(f"{name}_{j}", ell(p, (s, s, s * 0.9)), col, bone)
        if j % 2 == 1:
            k.add(f"{name}_{j}t", ell(p + (0, 0, s * 0.75), (s * 0.6, s * 0.6, s * 0.55)), col, bone)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    crystal_drills(k)
    emblem(k)
    clouds(k)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("excadrillmega")
m.build(*args())
