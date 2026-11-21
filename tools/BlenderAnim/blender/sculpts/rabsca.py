# Rabsca sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rabsca.py -- <the export's Rabsca.glb> <out.glb>
#
# Rellor grown (sibling("rellor"): its eye and its legs). From the references: the ball is now its largest mass by far,
# high over the beetle: a lumpy violet cloud wrapped round a coral-red sphere that bulges out of it at the front, a
# little to the model's right of the viewer's left, its rim a deeper crimson and the violet lumps lapping over its
# edge. The beetle hangs under it upside down with its back to the viewer: a small teal oval with a dark zigzag seam
# down the middle; its head at the bottom is red, with a white four-pointed star in its middle and white tufts round
# it. Two teal legs fringed with leaflets reach up to hold the ball; two long white feathery legs, serrated along
# their outer edge, spread out and bend down at an elbow, held forward and down so they frame the beetle.
# Skeleton: body (0, 0, 0.323) the ball, head (0, 0, 0.173) the beetle, leg{s} (0.03, -0.05, 0.113) the white legs,
# hind{s} (0.03, 0.01, 0.113) the teal legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, leaf, srgb, inside, unit, eye_look, args, outward, polygon,
                       sibling, tri)

rl = sibling("rellor")

VIOLET, CORAL, CRIMSON, GLINT = "#A03AB6", "#F25E6E", "#C42E52", "#FFB4C0"
TEAL, TEAL_DK, RED, WHITE, PUPIL = "#4AB0C4", "#246C7E", "#D8323E", "#F4F4F6", "#1E1A1E"

BALL_C, BALL_R = np.array([0.0, 0.01, 0.336]), np.array([0.126, 0.12, 0.122])
PINK_C, PINK_R = np.array([-0.03, -0.045, 0.346]), 0.094
PINK_A = unit(-0.25, -1, 0.05)


def ball(k):
    k.add("cloud", ell(BALL_C, BALL_R), VIOLET, "body", surface="skin")
    rs = np.random.RandomState(9)
    g = (math.sqrt(5) - 1) / 2
    n = 13
    for i in range(n):
        zz = 1 - 2 * (i + 0.5) / n
        th = 2 * math.pi * g * i
        d = np.array([math.sqrt(1 - zz * zz) * math.cos(th), math.sqrt(1 - zz * zz) * math.sin(th), zz])
        if np.dot(d, PINK_A) > 0.62:
            continue                                     # the pink shows through here
        r = 0.062 + 0.016 * rs.rand()
        k.add(f"lump{i}", ell(BALL_C + d * BALL_R * 0.8, (r, r, r * 0.92)), VIOLET, "body", surface="skin")
    k.add("pink", ell(PINK_C, (PINK_R, PINK_R, PINK_R)), CORAL, "body", surface="skin")
    # the coral sphere's rim, a deeper crimson where it turns away under the violet
    def rim(P):
        d = P - PINK_C
        r = np.linalg.norm(d, axis=1)
        c = (d @ PINK_A) / np.maximum(r, 1e-9)
        return inside(r - PINK_R - 0.006, 0.002) * inside(c - 0.6, 0.02)
    k.paint(rim, CRIMSON, on=["pink"])
    gl = PINK_C + unit(-0.45, -1, 0.5) * PINK_R
    k.decal(gl, unit(-0.45, -1, 0.5), (0.02, 0.014), GLINT, eps=0.003)


BUG_C, BUG_R = np.array([0.0, -0.004, 0.112]), np.array([0.046, 0.04, 0.054])
HEAD_C, HEAD_R = np.array([0.0, -0.014, 0.058]), np.array([0.037, 0.032, 0.026])


def beetle(k):
    k.add("bug", ell(BUG_C, BUG_R), TEAL, "head", surface="shell")
    # the seam down its back: a dark zigzag
    def seam(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        line = 0.007 * tri((z - 0.08) * 22)
        return (inside(np.abs(x - line) - 0.0035, 0.0018) * inside(y + 0.01) * inside(z - 0.158)
                * inside(0.078 - z))
    k.paint(seam, TEAL_DK, on=["bug"])
    k.add("head", ell(HEAD_C, HEAD_R), RED, "head", surface="skin")
    for i in range(9):
        a = math.radians(-200 + 400 / 9 * (i + 0.5) * 0.9)
        a = math.radians(i * 40)
        c = HEAD_C + np.array([0.042 * math.cos(a), 0.034 * math.sin(a) + 0.004, 0.022])
        k.add(f"tuft{i}", ell(c, (0.017, 0.017, 0.014)), WHITE, "head", surface="hair")
    # the white star in the middle of the face
    star = polygon([(math.cos(t) * (1.0 if j % 2 == 0 else 0.36), math.sin(t) * (1.0 if j % 2 == 0 else 0.36))
                    for j, t in enumerate(np.linspace(math.pi / 2, math.pi / 2 + 2 * math.pi, 8, endpoint=False))])
    k.decal(HEAD_C + unit(0, -1, -0.45) * 0.028, unit(0, -1, -0.45), (0.013, 0.013), WHITE, eps=0.002,
            outline=star, segs=96)


def teal_legs(k):
    for side, sn in ((1, "R"), (-1, "L")):
        m = np.array([side, 1, 1.0])
        P = [np.array(p) * m for p in [(0.026, 0.006, 0.15), (0.064, 0.0, 0.19), (0.052, 0.004, 0.232)]]
        k.add("arm" + sn, outward(tube(P, [0.013, 0.012, 0.011], seg=12, per=6)), TEAL, "hind" + sn, fuse=False,
              surface="shell")
        for j, (t, ang) in enumerate([(0.35, -20), (0.6, 10), (0.85, 35)]):
            base = P[0] + (P[2] - P[0]) * t + np.array([0.004 * side, 0, 0]) + (P[1] - (P[0] + P[2]) / 2) * 0.9 * math.sin(math.pi * t)
            d = unit(side * math.cos(math.radians(ang)), -0.25, math.sin(math.radians(ang)))
            k.add(f"frond{j}{sn}", outward(leaf(base, d, 0.038, 0.012, thick=0.008)), TEAL, "hind" + sn,
                  fuse=False, surface="shell")


def white_legs(k):
    """Long white feathery legs: out from the beetle's sides and bent down at an elbow, toothed on the outer edge."""
    for side, sn in ((1, "R"), (-1, "L")):
        m = np.array([side, 1, 1.0])
        root = np.array([0.03, -0.035, 0.115]) * m
        elbow = np.array([0.135, -0.075, 0.105]) * m
        tip = np.array([0.16, -0.105, 0.005]) * m
        for seg, (a, b, w0, w1) in enumerate([(root, elbow, 0.03, 0.042), (elbow, tip, 0.046, 0.01)]):
            ex = b - a; L = np.linalg.norm(ex); ex /= L
            front = unit(0.45 * side, -1, 0)
            ez = np.cross(front, ex)
            if ez[2] < 0 and seg == 0:
                ez = -ez
            if seg == 1 and ez[0] * side < 0:
                ez = -ez                                      # the teeth on the outer edge
            n = 7
            top, bot = [], []
            for i in range(n + 1):
                t = i / n
                w = w0 + (w1 - w0) * t
                top.append((t * L, w * (1.0 if i % 2 == 0 else 0.55)))
                bot.append((t * L, -w * 0.45))
            outline = bot + top[::-1]
            k.add(f"blade{seg}{sn}", slab(outline, a - ex * 0.01, ex, ez, 0.011), WHITE, "leg" + sn, fuse=False,
                  surface="hair")
        k.add("elbow" + sn, outward(ell(elbow, (0.02, 0.012, 0.022), sub=2)), WHITE, "leg" + sn, fuse=False,
              surface="hair")


CR, CP = srgb(RED), srgb(PUPIL)
EYE_HALF = (0.0105, 0.011)


def lay(k):
    def eyes(state):
        look = eye_look(state, rl.round_eye, CR, CP, line=0.17)
        for s in (1, -1):
            d = unit(0.5 * s, -1, -0.12)
            c = HEAD_C + d * HEAD_R * 0.97 + np.array([0, 0, 0.006])
            k.decal(c, d, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.001,
                    rings=20, segs=56)
    return eyes


def rabsca(src, out):
    k = Kit(src, coat="shell")
    ball(k)
    beetle(k)
    teal_legs(k)
    white_legs(k)
    k.build(voxel=0.0045, smooth=4, band=0.005, wband=0.04)
    k.eye_states(lay(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    rabsca(src, out)
