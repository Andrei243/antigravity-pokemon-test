# Aerodactyl sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aerodactyl.py -- <the export's Aerodactyl.glb> <out.glb>
# From the references: a grey-lavender pterosaur hovering, its body small and its head large, carried up and forward
# on a short neck: a long head with its jaws thrown wide open, pink inside, a row of bold white teeth along each jaw,
# a crest swept back off the top of the skull and a spike back from each cheek, small narrow eyes under a hard brow.
# Wide wings of light purple skin hung from long grey arms, raised in a V and swept back, a three-fingered hand at each
# wrist with its claws up, the trailing edge scalloped; a small spike on the back; short legs tucked under with small
# clawed feet; a long thin tail out behind to the +X side ending in an arrowhead.
# The export's joints: body (0, 0, 0.326), head (0, -0.06, 0.446), jaw (0, -0.15, 0.496), wings (+-0.08, 0, 0.406),
# tail (0, 0.08, 0.246), legs (+-0.06, 0.02, 0.246).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, unit, eye_look, polygon, args

GREY, MEMBRANE, MOUTH, TOOTH = "#B3ACCB", "#A78ACD", "#D9707C", "#F6F5EF"
INK, IRIS, CLAW = "#1C1826", "#46B060", "#E2DEEC"


def rot(deg):
    return np.array(Euler([math.radians(a) for a in deg]).to_matrix())


def spike(base, d, length, r, flat=1.0, up=(0, 0, 1.0)):
    """A solid spike with flat faces: a diamond of points round base (flattened across `up` by `flat`) and a tip."""
    d = unit(*d); up = np.array(up, float)
    a = np.cross(d, up); a /= np.linalg.norm(a); b = np.cross(a, d)
    base = np.array(base, float)
    return hull([base + a * r, base - a * r, base + b * r * flat, base - b * r * flat, base + d * length])


# --- the wings: long grey arms raised in a V and swept back, the skin hung from them --------------------------------
WING_ROOT = np.array([0.06, 0.0, 0.43])
WING_EX = unit(0.82, 0.38, 0.42)            # out, swept back, raised in a V
WING_EZ = unit(-0.1, 0.55, 1.0)             # across the plate: up and back
WING_SCALE = 0.92
SKIN = [(0.0, 0.03), (0.16, 0.075), (0.3, 0.11), (0.44, 0.04), (0.38, -0.01), (0.32, -0.07), (0.24, -0.06),
        (0.18, -0.13), (0.11, -0.11), (0.05, -0.17), (0.0, -0.13)]
ARM = [(0.0, 0.03), (0.16, 0.075), (0.3, 0.11), (0.44, 0.04)]
WRIST = (0.3, 0.11)


def wing_frame():
    ex = WING_EX
    ez = WING_EZ - ex * (WING_EZ @ ex); ez /= np.linalg.norm(ez)
    return ex, ez, np.cross(ex, ez)


def wing_point(a, b, lift=0.0):
    ex, ez, n = wing_frame()
    return WING_ROOT + WING_SCALE * (a * ex + b * ez) + n * lift


def wings(k):
    ex, ez, n = wing_frame()
    k.add("skin", slab([(a * WING_SCALE, b * WING_SCALE) for a, b in SKIN], WING_ROOT, ex, ez, 0.016), MEMBRANE,
          "wing{s}", fuse=False, mirror=True)
    k.add("arm", outward(tube([wing_point(*p) for p in ARM], [0.034, 0.028, 0.024, 0.014])), GREY, "wing{s}",
          fuse=False, mirror=True)
    # The hand at the wrist: three short fingers with claws, up and a little forward
    w = wing_point(*WRIST)
    k.add("hand", outward(ell(w, (0.026, 0.024, 0.024), sub=3)), GREY, "wing{s}", fuse=False, mirror=True)
    for j, d in enumerate([(0.35, -0.3, 1), (0.0, -0.25, 1), (-0.3, -0.05, 1)]):
        d = unit(*d)
        b = w + d * 0.018
        k.add(f"finger{j}", outward(tube([b, b + d * 0.035], [0.013, 0.011], seg=10, per=3)), GREY, "wing{s}",
              fuse=False, mirror=True)
        k.add(f"fclaw{j}", spike(b + d * 0.04, d, 0.026, 0.011, up=(0, 1, 0)), CLAW, "wing{s}", fuse=False,
              mirror=True, surface="shell")


def aerodactyl(src, out):
    k = Kit(src, coat="scales")

    # The body: a small leaning trunk, a fuller chest, a short neck up and forward to the head
    k.add("torso", ell((0, 0.01, 0.33), (0.088, 0.085, 0.12), rot=(-12, 0, 0)), GREY, "body")
    k.add("chest", ell((0, -0.025, 0.42), (0.085, 0.08, 0.08)), GREY, "body")
    k.add("neck", tube([(0, -0.02, 0.43), (0, -0.07, 0.5), (0, -0.1, 0.55)], [0.055, 0.048, 0.05]), GREY, "head")
    k.add("backspike", spike((0, 0.06, 0.45), (0, 0.6, 0.8), 0.06, 0.022, flat=0.6, up=(1, 0, 0)), GREY, "body",
          fuse=False)

    # The head: a skull at the hinge, a long upper jaw tilted up, the lower jaw dropped wide open, pink between.
    # Everything of the head is placed from the hinge J and scaled by HS: the art's head is large.
    J, HS = np.array([0.0, -0.13, 0.535]), 1.2
    def hp(x, y, z):
        return J + HS * np.array([x, y, z])
    UC, UR, UROT = hp(0, -0.095, 0.065), HS * np.array((0.05, 0.11, 0.036)), (-30, 0, 0)
    LC, LR, LROT = hp(0, -0.09, -0.042), HS * np.array((0.043, 0.1, 0.024)), (20, 0, 0)
    k.add("skull", ell(hp(0, 0.0, 0.04), HS * np.array((0.07, 0.075, 0.062))), GREY, "head")
    k.add("upper", ell(UC, UR, rot=UROT), GREY, "head")
    k.add("lower", ell(LC, LR, rot=LROT), GREY, "jaw")
    k.add("throat", ell(hp(0, -0.025, -0.02), HS * np.array((0.05, 0.05, 0.042))), GREY, "jaw")
    k.add("inside", ell(hp(0, -0.065, 0.008), HS * np.array((0.028, 0.07, 0.03)), rot=(-5, 0, 0)), MOUTH, "head")

    # The crest swept back off the top of the skull, and a spike back from each cheek
    k.add("crest", hull([hp(0.022, -0.02, 0.085), hp(-0.022, -0.02, 0.085), hp(0, -0.05, 0.075),
                         hp(0, 0.01, 0.11), hp(0, 0.1, 0.13)]), GREY, "head", fuse=False)
    k.add("cheek", spike(hp(0.045, 0.02, 0.02), (0.45, 1, 0.25), 0.08, 0.022, flat=0.7, up=(0, 0, 1)), GREY,
          "head", fuse=False, mirror=True)

    # The teeth: a row of bold white points down along each side of the upper jaw and up along the lower
    def rim(c, r, rr, t, z):
        p = np.array([0.72 * r[0], -t * r[1], z * r[2]])
        return c + p @ rot(rr).T
    for n_, t in enumerate([0.25, 0.48, 0.7, 0.9]):
        b = rim(UC, UR, UROT, t, -0.55)
        k.add(f"tooth{n_}", spike(b, np.array((0, 0, -1.0)) @ rot(UROT).T, 0.032 - 0.004 * n_, 0.013, flat=0.8,
                                   up=(0, 1, 0)), TOOTH, "head", fuse=False, mirror=True, surface="shell")
    for n_, t in enumerate([0.35, 0.6, 0.84]):
        b = rim(LC, LR, LROT, t, 0.5)
        k.add(f"fang{n_}", spike(b, np.array((0, 0, 1.0)) @ rot(LROT).T, 0.028 - 0.004 * n_, 0.012, flat=0.8,
                                  up=(0, 1, 0)), TOOTH, "jaw", fuse=False, mirror=True, surface="shell")

    wings(k)

    # The legs: short, tucked under, small feet with three claws
    k.add("thigh", ell((0.065, 0.02, 0.25), (0.048, 0.052, 0.06)), GREY, "leg{s}", mirror=True)
    k.add("shin", tube([(0.07, 0.01, 0.21), (0.075, -0.0, 0.15)], [0.032, 0.026]), GREY, "leg{s}", mirror=True)
    k.add("foot", ell((0.075, -0.02, 0.135), (0.03, 0.045, 0.02)), GREY, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.02, 0.0, 0.02)):
        k.add(f"toe{j}", spike((0.075 + dx, -0.055, 0.132), (dx * 0.8, -0.7, -0.7), 0.026, 0.01, up=(0, 1, 0)),
              CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")

    # The tail: long and thin, back and out to the +X side, an arrowhead at its end standing on edge
    TAIL = [(0.0, 0.06, 0.27), (0.03, 0.18, 0.22), (0.1, 0.3, 0.2), (0.17, 0.37, 0.21), (0.23, 0.42, 0.22)]
    k.add("tail", tube(TAIL, [0.05, 0.032, 0.024, 0.019, 0.016]), GREY, "tail")
    e = np.array(TAIL[-1]); d = unit(*(e - np.array(TAIL[-2])))
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.array((0, 0, 1.0))
    th = side * 0.012
    pts = []
    for p in [e - d * 0.01, e + d * 0.07, e - d * 0.02 + up * 0.04, e - d * 0.02 - up * 0.04, e + d * 0.005]:
        pts += [p + th, p - th]
    k.add("arrow", hull(pts), GREY, "tail", fuse=False)

    # The eyes: small and narrow, under a hard brow sloping down to the snout; a green iris, a black pupil
    K, I, G = srgb(INK), srgb(IRIS), srgb("#FFFFFF")
    EYE = [(-1.0, -0.35), (-0.55, 0.15), (0.2, 0.55), (1.0, 0.7), (0.95, -0.3), (0.2, -0.7), (-0.5, -0.65)]
    reach = polygon(EYE)
    def eye(u, v):
        r = np.sqrt(u * u + v * v) / np.array([reach(t) for t in np.arctan2(v, u)])
        c = np.tile(K, (len(u), 1))
        c[r < 0.78] = I
        c[((u - 0.05) / 0.22) ** 2 + (v / 0.55) ** 2 < 1] = K
        c[((u - 0.35) / 0.17) ** 2 + ((v - 0.2) / 0.17) ** 2 < 1] = G
        return c
    O = srgb(GREY)
    de = unit(0.82, -0.42, 0.38)
    E = hp(0.0, -0.02, 0.055) + de * HS * np.array([0.07, 0.075, 0.062])
    def eyes(state):
        look = eye_look(state, eye, O, K, line=0.2)
        k.decal(E, de, (0.03, 0.023), look, eps=0.005, rings=20, segs=56, outline=reach)
        m = np.array([-1, 1, 1.0])
        k.decal(E * m, de * m, (0.026, 0.02), lambda u, v: look(-u, v), eps=0.005, rings=20, segs=56,
                outline=lambda t: reach(math.pi - t))

    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    aerodactyl(*args())
