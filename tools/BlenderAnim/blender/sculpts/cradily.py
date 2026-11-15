# Cradily sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cradily.py -- <the export's Cradily.glb> <out.glb>
# Grown from lileep.py (sibling("lileep")): the same tentacles with their deeper pink tips, the same yellow spiral
# marks, the same yellow eye spots on black (here inside the mouth), the lavender turned to green.
#
# From the references: a green sea lily grown tall. A broad low foot of four rounded lobes, a yellow spiral mark on
# each front lobe; from its top a thick neck rises, ringed with three yellow bands low down, and bends forward at the
# top into the back of a big round hooded head that leans forward and looks down. On the face a wide black crescent of
# a mouth, smiling, with a yellow oval eye in each corner of it (the old sculpt and the brief put the eyes in the ringed
# marks; the pictures' rings are marks and the yellow ovals in the mouth are the eyes); above the mouth two yellow
# rings round a yellow ring (concentric marks). Eight pink tentacles with deeper pink tips round the head's top: those
# behind rise and arch out, the two in front hang down the sides of the face.
# Skeleton: body (0, 0, 0.1), stalk (0, 0.04, 0.1), head (0, -0.1, 0.42).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, ring_band, sibling, args

L = sibling("lileep")
YELLOW, BLACK, PINK, TIP = L.YELLOW, L.BLACK, L.PINK, L.TIP
GREEN = "#9ACC86"

# --- the frame ---------------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.1, 0.42]), np.array([0.142, 0.136, 0.132])
FACE = unit(0, -0.85, -0.45)                 # where the face looks: forward and down
UP = unit(0, -0.45, 0.85)                    # the face's own up
SIDE = np.array([1.0, 0, 0])
NECK = [(0, 0.05, 0.1), (0, 0.075, 0.22), (0, 0.095, 0.34), (0, 0.095, 0.45)]


def on_head(th, ph, lift=0.0):
    """A point of the head and its normal at th (radians round from the face toward +X) and ph (up from the face)."""
    d = math.cos(ph) * (math.cos(th) * FACE + math.sin(th) * SIDE) + math.sin(ph) * UP
    t = 1 / math.sqrt(((d / HEAD_R) ** 2).sum())
    p = HEAD_C + d * t
    n = unit(*((p - HEAD_C) / HEAD_R ** 2))
    return p + n * lift, n


def head_coords(P):
    """Each point's th, ph about the face (as on_head) and its share forward."""
    Q = P - HEAD_C
    f, u, s = Q @ FACE, Q @ UP, Q @ SIDE
    th = np.arctan2(s, f)
    ph = np.arctan2(u, np.hypot(f, s))
    return th, ph, f


TH_MOUTH = 1.25


def mouth_top(t):
    return -0.05 + 0.3 * t * t


def mouth_bottom(t):
    return -0.76 + 1.0 * t * t


def foot(k):
    k.add("foot", ell((0, 0.02, 0.072), (0.13, 0.13, 0.07)), GREEN, "body")
    for j, a in enumerate((40, -40, 135, -135)):
        d = np.array([math.sin(math.radians(a)), -math.cos(math.radians(a)), 0.0])
        k.add(f"lobe{j}", ell(d * 0.12 + (0, 0.02, 0.045), (0.062, 0.058, 0.043), rot=(0, 0, -a)), GREEN, "body")
    k.add("footcone", ell((0, 0.04, 0.13), (0.07, 0.07, 0.07)), GREEN, "body")
    for a in (40, -40):
        d = np.array([math.sin(math.radians(a)), -math.cos(math.radians(a)), 0.0])
        p = d * 0.162 + (0, 0.02, 0.058)
        gap = 1.9 if a > 0 else math.pi - 1.9
        k.decal(p, unit(*(d + (0, 0, 0.55))), (0.036, 0.036), L.spiral_mark(ground=GREEN, gap_at=gap), eps=0.003,
                rings=22, segs=72)


def neck(k):
    k.add("neck", tube(NECK, [0.056, 0.045, 0.04, 0.037]), GREEN, "stalk")
    # the bend forward into the back of the head: fused balls, a tube bent so sharply folds
    for j, (p, r) in enumerate((((0, 0.09, 0.48), 0.037), ((0, 0.072, 0.51), 0.039), ((0, 0.04, 0.52), 0.042))):
        k.add(f"bend{j}", ell(p, (r, r, r)), GREEN, "head")
    # three yellow bands round the neck low down, painted level (a loose band would have to follow the neck's lean)
    k.paint(lambda P: sum(inside(np.abs(P[:, 2] - z) - 0.0065, 0.0025) for z in (0.225, 0.255, 0.285))
            * inside(np.abs(P[:, 1] - (0.05 + 0.21 * (P[:, 2] - 0.1))) - 0.07, 0.004) * (P[:, 2] > 0.2),
            YELLOW)


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R), GREEN, "head")

    def mouth(P):
        th, ph, f = head_coords(P)
        t = th / TH_MOUTH
        r = 0.135
        d = np.maximum.reduce([(ph - mouth_top(t)) * r, (mouth_bottom(t) - ph) * r, (np.abs(t) - 1) * 0.1])
        return inside(d, 0.002) * (f > 0)
    k.paint(mouth, BLACK)
    # the ringed marks over the mouth: a yellow ring round a yellow middle
    CY, CG = srgb(YELLOW), srgb(GREEN)

    def rings(u, v):
        rho = np.sqrt(u * u + v * v)
        c = np.tile(CG, (len(u), 1))
        c[(rho > 0.66) & (rho < 0.98)] = CY
        c[(rho > 0.24) & (rho < 0.44)] = CY
        return c
    for s in (1, -1):
        p, n = on_head(0.5 * s, 0.32)
        k.decal(p, n, (0.04, 0.04), rings, eps=0.003, rings=24, segs=72)


ANGLES = (50, 86, 124, 160, -160, -124, -86, -50)


def crown(k):
    L.crown(k, HEAD_C + (0, 0.02, 0.0), (0, 0, 1), (0, -1, 0), (0.105, 0.075),
            back_path=[(0.03, 0.07), (0.07, 0.125), (0.115, 0.135), (0.148, 0.105)],
            front_path=[(0.05, 0.005), (0.09, -0.03), (0.105, -0.1), (0.098, -0.16)],
            r0=0.028, r1=0.024, angles=ANGLES)


def eyes(k, state):
    """The yellow ovals in the mouth's corners: the lid the mouth's black."""
    t = 0.52
    p, n = on_head(TH_MOUTH * t, (mouth_top(t) + mouth_bottom(t)) / 2)
    L.spot_eyes(k, state, p, n, (0.025, 0.032))


def cradily(src, out):
    k = Kit(src, coat="shell")
    foot(k)
    neck(k)
    head(k)
    crown(k)
    k.build(voxel=0.006, smooth=4, band=0.005)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    cradily(*args())
