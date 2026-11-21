# Vanillish sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vanillish.py -- <the export's Vanillish.glb> <out.glb>
#
# Grown from vanillite.py (its colours, eye, mouth, diamonds, swirl and cone, through sibling). The design, from the
# references: the scoop has grown into an egg of snow, narrow at its top and full below, with a small puffed tier and a
# pointed tip curling to +X on top. Its eyes are smaller than Vanillite's and set close together, the same pale blue
# with a large dark blue pupil and a dark rim; under them a wide dark blue grin, flat along its top, with a drip
# hanging from it right of the middle; a small diamond of ice beside each eye. Round its foot a ruff of snow puffs, a
# few diamonds of ice set in its front and a pointed crystal of ice standing out of it at each side (the art's are
# not a pair, one higher than the other: made here as a mirror pair, out and a little up). Under the ruff a long
# faceted cone of blue ice to a point, with short icicles hanging from its rim. (The art's puffs of cold air are left
# out, as the 3D model leaves them out.)
# The skeleton: body (0, 0, 0.349), head (0, 0, 0.459), tail (0, 0, 0.279). The egg is the head's, the ruff the
# body's, the cone the tail's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, smoothstep, unit, sibling, args

v = sibling("vanillite")

# --- the frame ----------------------------------------------------------------------------------------------------------
EGG_C, EGG_R = np.array([0.0, 0.0, 0.435]), np.array([0.138, 0.126, 0.158])
EYE_X, EYE_Z, EYE_HALF = 0.03, 0.478, (0.022, 0.041)
MOUTH_Z, MOUTH_HALF = 0.414, (0.08, 0.035)
GEM_X, GEM_Z, GEM_HALF = 0.072, 0.482, (0.012, 0.016)
RUFF_Z, RUFF_A, RUFF_B = 0.29, 0.135, 0.11
CONE_TOP, CONE_R, CONE_TIP = 0.27, 0.118, (-0.012, 0.0, 0.02)
VOXEL = 0.0055


def egg_shape(V):
    """An egg of snow: full below its middle, narrowing toward its top."""
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    s = 1.04 - 0.3 * t ** 1.6
    V[:, 0] *= s; V[:, 1] *= s
    return V


def egg(k, c, r, bone, name="egg", tip_lean=1.0, tip_scale=0.8):
    """The egg with its small tier and its tip (Vanilluxe's heads are two of these)."""
    c = np.array(c, float)
    k.add(name, ell(c, r, deform=egg_shape), v.SNOW, bone)
    top = c + (0.0, 0.0, r[2] * 0.93)
    k.add(name + "tier", ell(top + (0, 0.002, 0.008), (r[0] * 0.4, r[1] * 0.4, r[2] * 0.2)), v.SNOW, bone)
    v.swirl(k, top + (0, 0.0, 0.03), tip_scale, bone, name=name + "swirl", lean=tip_lean)


def egg_point(c, r, x, z):
    """A point on the egg's front at (x, z), and its normal (the patches find the true surface themselves)."""
    c, r = np.array(c, float), np.array(r, float)
    t = np.clip((z - c[2]) / r[2] * 0.5 + 0.5, 0, 1)
    s = 1.04 - 0.3 * t ** 1.6
    return v.on_ell(c, r * np.array([s, s, 1]), x, z)


def ruff(k, c, a, b, n, puff, bone, seed=5):
    """The ruff of snow puffs round the foot: a core and a ring of balls, jittered so they do not read as petals."""
    rs = np.random.RandomState(seed)
    c = np.array(c, float)
    k.add("ruffcore", ell(c, (a, b, puff * 0.9)), v.SNOW, bone)
    for i in range(n):
        th = 2 * math.pi * (i + 0.3 * rs.rand()) / n
        rr = puff * (0.9 + 0.25 * rs.rand())
        p = c + (a * math.sin(th), -b * math.cos(th), puff * 0.25 * (rs.rand() - 0.5))
        k.add(f"puff{i}", ell(p, (rr, rr, rr * 0.88)), v.SNOW, bone)
    # the shade on the ruff's underside
    k.paint(lambda P: smoothstep(c[2] - 0.005, c[2] - 0.06, P[:, 2]) * 0.8, v.SHADE)


def icicles(k, c, a, b, z, n, length, bone, seed=7):
    rs = np.random.RandomState(seed)
    for i in range(n):
        th = 2 * math.pi * (i + 0.5) / n
        p = np.array([c[0] + a * math.sin(th), c[1] - b * math.cos(th), z])
        L = length * (0.7 + 0.6 * rs.rand())
        k.add(f"icicle{i}", v.crystal(p, p + (0, 0, -L), [(0, 0.012), (1, 0)], n=5, turn=rs.rand() * 70), v.ICEDEEP,
              bone, fuse=False, surface="shell")


def arm_crystals(k, root, d, length, radius, bone):
    """A pointed crystal standing out of the ruff at each side, a mirror pair."""
    for s in (1, -1):
        r0 = np.array(root, float) * np.array([s, 1, 1])
        dd = np.array(d, float) * np.array([s, 1, 1])
        p1 = r0 + unit(*dd) * length
        k.add("arm" + ("R" if s > 0 else "L"),
              v.crystal(r0, p1, [(0, radius * 0.55), (0.35, radius), (0.62, radius * 0.92), (1, 0)], n=5, turn=18,
                        jitter=0.1, seed=11 if s > 0 else 12), v.ICE, bone, fuse=False, surface="shell")


def front_gems(k, spots, bone):
    for i, (p, size) in enumerate(spots):
        p = np.array(p, float)
        d = unit(p[0] * 2, -1, 0.3)
        k.add(f"gem{i}", v.gem(p - d * size * 0.9, p + d * size * 0.9, size * 0.75, n=4, turn=45), v.ICE, bone,
              fuse=False, surface="shell")


def mouth_look(navy=v.NAVY):
    return srgb(navy)


# The grin: flat along its top with two soft dips, round below, and a drip right of the middle
GRIN = [(-1.0, 0.45), (-0.62, 0.62), (-0.2, 0.56), (0.2, 0.6), (0.62, 0.56), (1.0, 0.45), (0.97, 0.0), (0.84, -0.5),
        (0.5, -0.82), (0.0, -0.9), (-0.5, -0.82), (-0.84, -0.5), (-0.97, 0.0)]
DRIP_X, DRIP_HALF = 0.3, (0.13, 0.55)     # in the grin's half sizes: a drop hanging under it right of the middle


def face(k, c, r, gem_x, gem_z, mouth_z, mouth_half, mouth_colour, gem_half=GEM_HALF, grin=True, gems=(1, -1)):
    """The face but its eyes: the grin with its drip (or, grin=False, a plain smile flat on top), the diamonds."""
    p, n = egg_point(c, r, c[0], mouth_z)
    k.decal(p - n * 0.005, n, mouth_half, mouth_colour, eps=0.0035, outline=v.polygon(GRIN) if grin else v.mouth_outline,
            rings=16, segs=96)
    for s in gems:
        p, n = egg_point(c, r, c[0] + gem_x * s, gem_z)
        look = v.gem_look()
        k.decal(p - n * 0.004, n, gem_half, look if s > 0 else (lambda u, vv, f=look: f(-u, vv)), eps=0.0035,
                outline=v.DIAMOND, rings=10, segs=64)
    if not grin:
        return
    dz = mouth_z - mouth_half[1] * 0.75
    p, n = egg_point(c, r, c[0] + DRIP_X * mouth_half[0], dz)
    k.decal(p - n * 0.005, n, (DRIP_HALF[0] * mouth_half[0], DRIP_HALF[1] * mouth_half[1]), mouth_colour, eps=0.0036,
            rings=10, segs=40)


def vanillish(src, out):
    k = Kit(src, coat="fur")
    egg(k, EGG_C, EGG_R, "head")
    ruff(k, (0, 0.0, RUFF_Z), RUFF_A, RUFF_B, 11, 0.062, "body")
    v.ice_cone(k, "cone", (0, 0, CONE_TOP), CONE_R, CONE_TIP, 0.0, "tail", n=9, seed=4)
    icicles(k, (0, 0, 0), 0.1, 0.085, 0.255, 9, 0.04, "tail")
    arm_crystals(k, (0.15, -0.02, 0.3), (1, -0.3, 0.75), 0.09, 0.034, "body")
    front_gems(k, [((-0.062, -0.15, 0.292), 0.024), ((0.012, -0.163, 0.276), 0.027), ((0.068, -0.146, 0.3), 0.021)],
               "body")
    face(k, EGG_C, EGG_R, GEM_X, GEM_Z, MOUTH_Z, MOUTH_HALF, v.NAVY)
    k.build(voxel=VOXEL, smooth=3, band=0.007)
    look = v.eye_open(v.EYEFILL, v.NAVY, v.NAVY)
    k.eye_states(lambda st: v.lay_eyes(k, st, EGG_C, EGG_R * np.array([0.93, 0.93, 1]), EYE_X, EYE_Z, EYE_HALF, look,
                                       v.SNOW, v.NAVY))
    k.export(out)


if __name__ == "__main__":
    vanillish(*args())
