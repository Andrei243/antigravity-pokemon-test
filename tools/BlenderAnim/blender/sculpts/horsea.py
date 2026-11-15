# Horsea sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/horsea.py -- <the export's Horsea.glb> <out.glb>
#
# From the references: a small light blue sea horse floating upright. A big round head with a long tube of a snout
# straight forward, open in a dark round hole at its flared end; big eyes on the sides of the head, a red iris with a
# dark pupil and a glint, a crescent of white behind; three blue spikes out of the back of the head (up and back, back,
# down and back). A slender body under it, its front a cream belly crossed by a few lines; a small cream fin like a
# leaf on its back, ribbed; the tail curled forward under it in a thick coil.
#
# Written to be grown from (seadra.py imports it with sibling): the frame is in the constants and the functions that
# take the kit; the build is under the main guard. Bones: root, body, tail, fin, head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, slab, polygon

BLUE, CREAM, RIDGE, HOLE = "#72C2EA", "#F4E2A8", "#C8A86E", "#1C2030"
IRIS, DEEP, WHITE, INK, PUPIL = "#D8404A", "#A02434", "#FAFAF8", "#1E2230", "#16101A"

# ---- the frame ----------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, 0.0, 0.51]), np.array([0.1, 0.108, 0.1])
SNOUT = [(0.0, -0.06, 0.497), (0.0, -0.14, 0.491), (0.0, -0.198, 0.489)]
SNOUT_R = [0.038, 0.034, 0.033]
TIP_R = (0.043, 0.024, 0.043)
SPINE = [(0.0, 0.035, 0.47), (0.0, 0.01, 0.395), (0.0, -0.002, 0.33), (0.0, 0.012, 0.27)]
SPINE_R = [0.062, 0.066, 0.064, 0.055]
BELLY_C, BELLY_R = np.array([0.0, -0.022, 0.355]), np.array([0.058, 0.05, 0.085])
COIL_C = np.array([0.0, -0.022, 0.172])          # (y, z) round which the tail curls, in the midline plane
COIL = dict(a0=58.0, turns=1.2, r0=0.09, r1=0.024, t0=0.05, t1=0.018, turn=32.0)


def head(k, c=HEAD_C, r=HEAD_R, color=BLUE):
    k.add("head", ell(c, r), color, "head")
    k.add("jowl", ell(c + (0, -0.03, -0.045), r * (0.8, 0.75, 0.6)), color, "head")


def snout(k, pts=SNOUT, radii=SNOUT_R, tip_r=TIP_R, color=BLUE, hole=HOLE, hole_half=(0.025, 0.026)):
    k.add("snout", tube(pts, radii), color, "head")
    tip = np.array(pts[-1])
    k.add("snouttip", ell(tip, tip_r), color, "head")
    # the hole at its end: a dark oval cast straight back down the snout
    k.decal(tip + (0, -tip_r[1], 0), (0, -1, 0.0), hole_half, hole, eps=0.003, rings=10, segs=40)


def spikes(k, list_, color=BLUE, bone="head"):
    """Solid spikes: (root, tip, root radius) each, fused tubes tapering to a point."""
    for i, (a, b, r) in enumerate(list_):
        a, b = np.array(a), np.array(b)
        m = a * 0.45 + b * 0.55
        k.add(f"spike{i}", tube([a, m, b], [r, r * 0.6, r * 0.18]), color, bone)


HORSEA_SPIKES = [((0.0, 0.05, 0.565), (0.0, 0.1, 0.66), 0.026),
                 ((0.0, 0.075, 0.52), (0.0, 0.19, 0.548), 0.026),
                 ((0.0, 0.07, 0.465), (0.0, 0.155, 0.43), 0.024)]


def torso(k, spine=SPINE, radii=SPINE_R, belly_c=BELLY_C, belly_r=BELLY_R, color=BLUE):
    k.add("neck", tube(spine, radii), color, "body")
    k.add("belly", ell(belly_c, belly_r), color, "body")


def coil(k, centre=COIL_C, a0=58.0, turns=1.2, r0=0.088, r1=0.024, t0=0.046, t1=0.017, turn=0.0, start=None,
         color=BLUE):
    """The tail: from the body's foot down its back, round under and up the front, curling in. Its plane is the
    midline turned turn degrees about the upright toward +X at the back."""
    n = 40
    s = np.linspace(0, 1, n)
    th = np.radians(a0 - s * 360 * turns)
    rr = r0 + (r1 - r0) * s ** 0.9
    back = unit(math.sin(math.radians(turn)), math.cos(math.radians(turn)), 0)
    P = [centre + back * (rr[i] * math.cos(th[i])) + np.array([0, 0, rr[i] * math.sin(th[i])]) for i in range(n)]
    if start is not None:
        P = [np.array(start)] + P
    R = t0 + (t1 - t0) * s ** 1.1
    if start is not None:
        R = np.concatenate([[t0 * 1.05], R])
    k.add("tail", tube(P, R, seg=20, per=4), color, "tail")


def belly(k, z0=0.285, z1=0.44, front=-0.018, color=CREAM, ridge=RIDGE, rows=(0.305, 0.335, 0.365, 0.395),
          half=(0.042, 0.0036), lift=0.0):
    """The cream down the front, and a few crisp lines across it (thin lens patches cast from the front)."""
    def m(P):
        return (inside(P[:, 1] - front - 0.0004 * 0, 0.0025) * inside(z0 - P[:, 2], 0.004)
                * inside(P[:, 2] - z1, 0.004)) * (P[:, 1] < 0.0)
    k.paint(m, color, on=["belly", "neck"])
    for i, z in enumerate(rows):
        w = half[0] * (1 - 0.25 * abs((z - (z0 + z1) / 2) / ((z1 - z0) / 2)) ** 2)
        k.decal((0, -0.06, z), unit(0, -1, lift), (w, half[1]), ridge, eps=0.004, rings=4, segs=36)


def leaf_outline(L, W, n=20):
    """A leaf from (0, 0) to (L, 0), widest past its middle, rounded at its tip."""
    pts = []
    for i in range(n + 1):
        t = i / n
        pts.append((L * t, W * math.sin(math.pi * t ** 0.75) ** 0.8))
    for i in range(n - 1, 0, -1):
        t = i / n
        pts.append((L * t, -W * 0.85 * math.sin(math.pi * t ** 0.75) ** 0.8))
    return np.array(pts)


def back_fin(k, root=(0.012, 0.058, 0.36), direction=(0.55, 0.78, 0.3), L=0.15, W=0.048, color=CREAM, rib=RIDGE,
             ribs=(-12.0, 4.0, 20.0)):
    """A cream leaf of a fin on the back, run back and out to +X so the game's cameras see its faces, a few ribs."""
    d = unit(*direction)
    ez = np.cross(d, unit(-0.8, 0.55, 0))           # the plate's other in-plane direction
    ez = ez / np.linalg.norm(ez)
    if ez[2] < 0:
        ez = -ez
    o = np.array(root)
    k.add("backfin", slab(leaf_outline(L, W), o, d, ez, 0.008), color, "fin", fuse=False, surface="shell")
    for i, a in enumerate(ribs):
        t = math.radians(a)
        dd = np.array([math.cos(t), math.sin(t)])
        side = np.array([-dd[1], dd[0]])
        ln = L * 0.82
        out = [tuple(dd * 0.012 + side * 0.0026), tuple(dd * ln + side * 0.0012), tuple(dd * ln - side * 0.0012),
               tuple(dd * 0.012 - side * 0.0026)]
        k.add(f"finrib{i}", slab(out, o, d, ez, 0.0116), rib, "fin", fuse=False, surface="shell")


# ---- the eyes: big, on the sides of the head: a red iris with a dark pupil and a glint, white behind ---------------
CB, CW, CK, CI, CD, CP = srgb(BLUE), srgb(WHITE), srgb(INK), srgb(IRIS), srgb(DEEP), srgb(PUPIL)
EYE_N = unit(0.74, -0.6, 0.22)
EYE_HALF = (0.044, 0.05)


def horsea_eye(u, v):
    # +u runs toward the back on both sides of a sideways patch
    c = np.tile(CK, (len(u), 1))
    rr = u * u + v * v
    c[rr < 0.8] = CW
    iris = ((u + 0.16) / 0.66) ** 2 + (v / 0.72) ** 2
    c[(iris < 1) & (rr < 0.8)] = CI
    c[((u + 0.16) / 0.22) ** 2 + (v / 0.38) ** 2 < 1] = CP
    c[((u + 0.0) / 0.12) ** 2 + ((v - 0.36) / 0.13) ** 2 < 1] = CW
    return c


def eye_centre(c=HEAD_C, r=HEAD_R, n=EYE_N):
    return c + n * r * 0.98


def cheek(k, c=HEAD_C, r=HEAD_R, n=EYE_N, color=BLUE):
    """A flattened part under each eye, so a big eye lies on a flat face and does not wrap."""
    p = c + n * r * 0.72
    k.add("cheek", ell(p, (0.05, 0.05, 0.055), rot=(0, 0, -math.degrees(math.atan2(n[0], -n[1])))), color, "head",
          mirror=True)


def horsea_eyes(k, look=horsea_eye):
    def lay(state):
        k.decal(eye_centre(), EYE_N, EYE_HALF, eye_look(state, look, CB, CK, line=0.16), eps=0.005, rings=20,
                segs=56, mirror=True)
    return lay


def horsea(src, out):
    k = Kit(src, coat="scales")
    head(k)
    cheek(k)
    snout(k)
    spikes(k, HORSEA_SPIKES)
    torso(k)
    coil(k, start=SPINE[-1], **COIL)
    belly(k)
    back_fin(k)
    k.build(voxel=0.005, smooth=3, band=0.006)
    k.eye_states(horsea_eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    horsea(src, out)
