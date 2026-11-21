# Mega Aerodactyl sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aerodactylmega.py -- <the export's Aerodactyl-Mega.glb> <out.glb>
# Grown from aerodactyl.py (imported through sibling: its spike, its wings' frame and outline): the same hovering
# pterosaur with its jaws thrown open, now broken out in spikes of dark rock, which are the Mega's largest change: a
# cluster of great dark spikes at each wrist in place of the hand and more along the arm's leading edge, a long dark
# spike hanging from the chin, the crest gone dark with a horn up behind it, spikes from the cheeks and the sides of
# the neck, a row along the tail and the back, spurs on the knees, and dark rock set into the chest, belly and thighs.
# The body a little darker grey-lavender, the wings' skin a deeper purple, the eyes green.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, unit, eye_look, polygon, args, sibling

base = sibling("aerodactyl")
spike = base.spike
GREY, MEMBRANE, MOUTH, TOOTH = "#A9A1C2", "#8E66B8", "#D9707C", "#F6F5EF"
INK, IRIS, ROCK = "#1C1826", "#2EB07A", "#4C4858"


def rot(deg):
    return np.array(Euler([math.radians(a) for a in deg]).to_matrix())


def rocky(c, r, seed, n=9):
    """A lump of dark rock: a hull of points jittered round a ball, set half into the body."""
    rs = np.random.RandomState(seed)
    D = rs.normal(size=(n, 3)); D /= np.linalg.norm(D, axis=1, keepdims=True)
    return hull(np.array(c, float) + D * np.array(r) * (0.75 + 0.35 * rs.rand(n))[:, None])


def wings(k):
    ex, ez, n = base.wing_frame()
    S = base.WING_SCALE
    k.add("skin", slab([(a * S, b * S) for a, b in base.SKIN], base.WING_ROOT, ex, ez, 0.016), MEMBRANE,
          "wing{s}", fuse=False, mirror=True)
    k.add("arm", outward(tube([base.wing_point(*p) for p in base.ARM], [0.036, 0.03, 0.027, 0.018])), GREY, "wing{s}",
          fuse=False, mirror=True)
    # The wrist: a lump of dark rock and a cluster of great spikes, up, up and out, up and back
    w = base.wing_point(*base.WRIST)
    k.add("wristrock", rocky(w, (0.045, 0.04, 0.04), 3), ROCK, "wing{s}", fuse=False, mirror=True, surface="shell")
    for j, (d, L, r) in enumerate([((0.15, -0.15, 1.0), 0.16, 0.036), ((0.75, 0.1, 0.65), 0.14, 0.034),
                                   ((-0.2, 0.55, 0.8), 0.12, 0.032), ((0.4, -0.6, 0.6), 0.085, 0.026)]):
        k.add(f"wspike{j}", spike(w + unit(*d) * 0.012, d, L, r, flat=0.7, up=(0, 1, 0)), ROCK, "wing{s}",
              fuse=False, mirror=True, surface="shell")
    # Spikes along the arm's leading edge, up and back
    for j, (t, L) in enumerate([((0.1, 0.06), 0.09), ((0.2, 0.09), 0.11), ((0.38, 0.085), 0.07)]):
        p = base.wing_point(*t)
        k.add(f"aspike{j}", spike(p + np.array((0, 0, 0.015)), (0.15, 0.45, 1.0), L, 0.028, flat=0.7, up=(1, 0, 0)),
              ROCK, "wing{s}", fuse=False, mirror=True, surface="shell")


def aerodactylmega(src, out):
    k = Kit(src, coat="scales")

    k.add("torso", ell((0, 0.01, 0.33), (0.09, 0.087, 0.122), rot=(-12, 0, 0)), GREY, "body")
    k.add("chest", ell((0, -0.025, 0.42), (0.087, 0.082, 0.082)), GREY, "body")
    k.add("neck", tube([(0, -0.02, 0.43), (0, -0.07, 0.5), (0, -0.1, 0.55)], [0.057, 0.05, 0.052]), GREY, "head")
    # Spikes of dark rock out of the back, and from the sides of the neck
    k.add("backspike", spike((0, 0.06, 0.45), (0, 0.6, 0.8), 0.11, 0.034, flat=0.6, up=(1, 0, 0)), ROCK, "body",
          fuse=False, surface="shell")
    k.add("backspike2", spike((0, 0.075, 0.37), (0, 0.9, 0.45), 0.085, 0.03, flat=0.6, up=(1, 0, 0)), ROCK, "body",
          fuse=False, surface="shell")
    k.add("neckspike", spike((0.042, -0.05, 0.48), (1, 0.35, 0.35), 0.095, 0.03, up=(0, 0, 1)), ROCK, "head",
          fuse=False, mirror=True, surface="shell")
    # Dark rock set into the chest, the belly and the thighs
    for j, (c, r) in enumerate([((0.05, -0.085, 0.42), (0.036, 0.024, 0.042)), ((-0.055, -0.07, 0.31), (0.04, 0.026, 0.036)),
                                ((0.03, -0.08, 0.235), (0.032, 0.022, 0.028)), ((-0.075, -0.035, 0.42), (0.03, 0.026, 0.03))]):
        k.add(f"rock{j}", rocky(c, r, 10 + j), ROCK, "body", fuse=False, surface="shell")

    J, HS = np.array([0.0, -0.13, 0.535]), 1.22
    def hp(x, y, z):
        return J + HS * np.array([x, y, z])
    UC, UR, UROT = hp(0, -0.095, 0.065), HS * np.array((0.05, 0.11, 0.036)), (-30, 0, 0)
    LC, LR, LROT = hp(0, -0.09, -0.042), HS * np.array((0.043, 0.1, 0.024)), (20, 0, 0)
    k.add("skull", ell(hp(0, 0.0, 0.04), HS * np.array((0.07, 0.075, 0.062))), GREY, "head")
    k.add("upper", ell(UC, UR, rot=UROT), GREY, "head")
    k.add("lower", ell(LC, LR, rot=LROT), GREY, "jaw")
    k.add("throat", ell(hp(0, -0.025, -0.02), HS * np.array((0.05, 0.05, 0.042))), GREY, "jaw")
    k.add("inside", ell(hp(0, -0.065, 0.008), HS * np.array((0.028, 0.07, 0.03)), rot=(-5, 0, 0)), MOUTH, "head")

    # The crest gone dark, a horn up behind it, cheek spikes of rock, and the long spike hanging from the chin
    k.add("crest", hull([hp(0.022, -0.02, 0.085), hp(-0.022, -0.02, 0.085), hp(0, -0.05, 0.075),
                         hp(0, 0.01, 0.11), hp(0, 0.11, 0.14)]), ROCK, "head", fuse=False, surface="shell")
    k.add("horn", spike(hp(0, 0.035, 0.07), (0, 0.35, 1.0), 0.12, 0.032, flat=0.6, up=(1, 0, 0)), ROCK, "head",
          fuse=False, surface="shell")
    k.add("cheek", spike(hp(0.045, 0.02, 0.02), (0.5, 1, 0.3), 0.11, 0.03, flat=0.7, up=(0, 0, 1)), ROCK,
          "head", fuse=False, mirror=True, surface="shell")
    chin = LC + np.array((0, -0.045, -0.02)) @ rot(LROT).T
    k.add("chin", spike(chin, (0, -0.1, -1.0), 0.17, 0.045, flat=0.6, up=(1, 0, 0)), ROCK, "jaw", fuse=False,
          surface="shell")

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

    # The legs: short, tucked under, a spur of rock at each knee, small feet with three claws
    k.add("thigh", ell((0.065, 0.02, 0.25), (0.05, 0.054, 0.062)), GREY, "leg{s}", mirror=True)
    k.add("shin", tube([(0.07, 0.01, 0.21), (0.075, -0.0, 0.15)], [0.033, 0.027]), GREY, "leg{s}", mirror=True)
    k.add("foot", ell((0.075, -0.02, 0.135), (0.03, 0.045, 0.02)), GREY, "leg{s}", mirror=True)
    k.add("knee", spike((0.09, 0.05, 0.25), (0.35, 0.9, -0.2), 0.08, 0.028, up=(0, 0, 1)), ROCK, "leg{s}",
          fuse=False, mirror=True, surface="shell")
    for j, dx in enumerate((-0.02, 0.0, 0.02)):
        k.add(f"toe{j}", spike((0.075 + dx, -0.055, 0.132), (dx * 0.8, -0.7, -0.7), 0.026, 0.01, up=(0, 1, 0)),
              ROCK, "leg{s}", fuse=False, mirror=True, surface="shell")

    # The tail: back and out to the +X side, a row of dark spikes along its top, a broad spade at its end
    TAIL = [(0.0, 0.06, 0.27), (0.03, 0.18, 0.22), (0.1, 0.3, 0.2), (0.17, 0.37, 0.21), (0.23, 0.42, 0.22)]
    k.add("tail", tube(TAIL, [0.052, 0.035, 0.026, 0.021, 0.018]), GREY, "tail")
    for j, (i, L) in enumerate([(1, 0.065), (2, 0.06), (3, 0.05)]):
        p = np.array(TAIL[i]) + np.array((0, 0, 0.022 - 0.003 * j))
        k.add(f"tspike{j}", spike(p, (0.15, 0.35, 1.0), L * 1.4, 0.028, flat=0.6, up=(1, 0, 0)), ROCK, "tail", fuse=False,
              surface="shell")
    e = np.array(TAIL[-1]); d = unit(*(e - np.array(TAIL[-2])))
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.array((0, 0, 1.0))
    th = side * 0.014
    pts = []
    for p in [e - d * 0.01, e + d * 0.085, e - d * 0.025 + up * 0.05, e - d * 0.025 - up * 0.05, e + d * 0.005]:
        pts += [p + th, p - th]
    k.add("arrow", hull(pts), GREY, "tail", fuse=False)

    # The eyes: as aerodactyl.py's, small and narrow under a hard brow, a green iris, a black pupil
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
        k.decal(E, de, (0.032, 0.025), look, eps=0.005, rings=20, segs=56, outline=reach)
        m = np.array([-1, 1, 1.0])
        k.decal(E * m, de * m, (0.032, 0.025), lambda u, v: look(-u, v), eps=0.005, rings=20, segs=56,
                outline=lambda t: reach(math.pi - t))

    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    aerodactylmega(*args())
