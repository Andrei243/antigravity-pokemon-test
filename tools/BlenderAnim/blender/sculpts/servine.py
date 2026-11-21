# Servine sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/servine.py -- <the export's Servine.glb> <out.glb>
# Snivy grown taller (built by snivy.py's functions: the line's green, cream and yellow, its cranium and tapering snout,
# its lidded eye, its cream face, throat and belly, its stubby arms and short cream legs, its flat leaves). From the
# references: the head carried high on a long thick neck, the snout level and forward, cream below the line from its
# tip to under the eye; red eyes under a heavy green lid, no yellow round them; a green crest rising from the back of
# the head and hooking forward at its top, yellow on its front face. One long yellow leaf on each side rising from the
# shoulders up and back past the neck. A plump teardrop of a body, green behind, cream in front; small arms forward
# and down with leafy fingers; short cream legs. Three green leaves of two and three points along the back, a long tail
# held out back and out to +X, ending in a three-pointed leaf.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, unit, args, outward, sibling

S = sibling("snivy")
T = S.T
GREEN, LEAF, CREAM, YELLOW = S.GREEN, S.LEAF, S.CREAM, S.YELLOW
VOXEL = 0.0048

F = SimpleNamespace(**vars(S.F))
F.H, F.HR = np.array([0.0, -0.035, 0.505]), np.array([0.07, 0.077, 0.074])
F.snout_dir, F.snout_mid, F.snout_r, F.snout_taper, F.snout_flat = (0, -0.99, -0.06), 0.095, (0.051, 0.125, 0.044), 0.62, 0.5
F.chin_at, F.chin_r = (0, -0.035, -0.04), (0.055, 0.06, 0.046)
F.cream_line, F.face_reach = (0.0, 0.2, -0.026), 0.092
F.front_axis = [(0.0, 0.0), (0.15, -0.02), (0.3, -0.035), (0.42, -0.04), (0.47, -0.04)]
F.front_span = (0.03, 0.47, 1.05)
F.mouth_at, F.mouth_h = (0.125, -0.7), (0.018, 0.01)
F.eye_dir, F.eye_shift, F.eye_h = (0.8, -0.58, 0.06), (0, 0, -0.004), (0.035, 0.036)
F.lid, F.iris, F.lid_col = (0.22, 0.1), (-0.15, -0.2, 0.62), GREEN
S_IRIS = "#C8282E"

F.thigh_c, F.thigh_r = (0.038, 0.012, 0.085), (0.036, 0.04, 0.05)
F.shin, F.shin_r = [(0.04, 0.008, 0.06), (0.042, 0.0, 0.02)], [0.022, 0.019]
F.foot_c, F.foot_r = (0.043, -0.016, 0.012), (0.02, 0.036, 0.012)
F.arm = [(0.05, -0.04, 0.27), (0.07, -0.072, 0.24), (0.074, -0.098, 0.218)]
F.arm_r = [0.017, 0.015, 0.014]
F.hand_c, F.hand_r = (0.076, -0.108, 0.21), (0.016, 0.016, 0.015)
F.fingers = [(0.55, -0.6, -0.4), (0.15, -0.8, -0.55), (-0.3, -0.7, -0.6)]
F.finger_len, F.finger_r = 0.026, 0.01

TAIL = [(0.0, 0.07, 0.13), (0.03, 0.15, 0.1), (0.075, 0.23, 0.095), (0.12, 0.3, 0.1), (0.155, 0.355, 0.105)]
TAIL_R = [0.045, 0.034, 0.026, 0.021, 0.018]


def crest(k):
    """The crest: one green horn fused to the back of the head, rising, a little back, its top hooking forward, its
    front face yellow."""
    H = F.H
    root = H + np.array([0.0, 0.045, 0.04])
    pts = np.array([root, root + (0, 0.025, 0.05), root + (0, 0.02, 0.095), root + (0, -0.008, 0.12)])
    k.add("crest", tube(pts, [0.03, 0.022, 0.017, 0.015]), GREEN, "head")
    dense = np.vstack([pts[i] + (pts[i + 1] - pts[i]) * t for i in range(len(pts) - 1) for t in np.linspace(0, 1, 24)])

    def front(P):
        j = ((P[:, None, :] - dense[None]) ** 2).sum(2).argmin(1)
        rel = P - dense[j]
        r = np.linalg.norm(rel, axis=1) + 1e-9
        return S.inside(0.25 + rel[:, 1] / r, 0.1) * (P[:, 2] > root[2] + 0.02)
    k.paint(front, YELLOW, on=["crest"])


def shoulder_leaves(k):
    """A long yellow leaf on each side from the shoulders, up and back past the neck, its face turned out."""
    for s, side in ((1, "R"), (-1, "L")):
        base = np.array([0.04 * s, -0.025, 0.3])
        d = unit(0.28 * s, 0.6, 0.75)
        k.add(f"collar{side}", T.leaf_blade(base, d, (s, 0.15, -0.2), 0.24, 0.03, 0.03, widest=0.35), YELLOW, "body",
              fuse=False, surface="leaf")


def back_leaves(k):
    """Three green leaves along the back, two and three points each, standing up and back, their faces turned out to
    the +X side so both of the game's cameras see them."""
    ex = unit(0.6, 0.8, 0.0)
    shapes = [
        (np.array([0.0, 0.08, 0.27]), 0.9, [(-0.02, 0.0), (0.03, 0.0), (0.06, 0.05), (0.04, 0.045), (0.03, 0.09),
                                            (0.0, 0.05), (-0.022, 0.075), (-0.02, 0.03)]),
        (np.array([0.012, 0.13, 0.2]), 1.0, [(-0.02, 0.0), (0.03, 0.0), (0.065, 0.05), (0.04, 0.045), (0.03, 0.095),
                                             (0.0, 0.055), (-0.025, 0.08), (-0.02, 0.03)]),
        (np.array([0.04, 0.2, 0.135]), 0.9, [(-0.02, 0.0), (0.03, 0.0), (0.065, 0.045), (0.035, 0.04), (0.025, 0.085),
                                             (0.0, 0.05), (-0.02, 0.07), (-0.018, 0.03)]),
    ]
    for j, (at, sc, out) in enumerate(shapes):
        ez = unit(0.0, 0.35, 1.0)
        k.add(f"backleaf{j}", slab([(a * sc, b * sc) for a, b in out], at, ex, ez, 0.02), LEAF, "body" if j < 2 else "tail",
              fuse=False, surface="leaf")


def tail(k):
    pts = np.array(TAIL)
    k.add("tail", tube(pts, TAIL_R), GREEN, "tail")
    ex = unit(0.58, 0.81, 0.0)
    out = [(-0.02, -0.015), (0.03, -0.02), (0.08, -0.06), (0.14, -0.065), (0.1, -0.025), (0.15, 0.0), (0.1, 0.02),
           (0.14, 0.065), (0.08, 0.06), (0.03, 0.025), (-0.02, 0.015)]
    k.add("leaf", slab(out, pts[-1] - ex * 0.01, ex, unit(0, 0, 1.0), 0.02), LEAF, "tail", fuse=False, surface="leaf")
    # the green back runs along the tail's top; its underside cream
    dense = np.vstack([pts[i] + (pts[i + 1] - pts[i]) * t for i in range(len(pts) - 1) for t in np.linspace(0, 1, 24)])

    def under(P):
        j = ((P[:, None, :] - dense[None]) ** 2).sum(2).argmin(1)
        rel = P - dense[j]
        r = np.linalg.norm(rel, axis=1) + 1e-9
        return S.inside(rel[:, 2] / r + 0.15, 0.08)
    k.paint(under, CREAM, on=["tail"])


def servine(src, out):
    k = Kit(src, coat="scales")
    S.IRIS = S_IRIS

    # The body: a plump teardrop, its belly round and low in front, leaning back toward the tail
    def tear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.12 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.01, 0.155), (0.072, 0.082, 0.112), rot=(-12, 0, 0), deform=tear), GREEN, "body")
    k.add("chest", ell((0, -0.02, 0.255), (0.052, 0.05, 0.06)), GREEN, "body")
    k.add("neck", tube([(0, -0.02, 0.26), (0, -0.03, 0.33), (0, -0.03, 0.4), (0, -0.02, 0.46)],
                       [0.056, 0.05, 0.047, 0.047]), GREEN, "head")
    S.head(k, F)
    crest(k)

    for s, side in ((1, "R"), (-1, "L")):
        S.leg(k, s, side, F)
        S.arm(k, s, side, F)

    tail(k)
    back_leaves(k)
    shoulder_leaves(k)

    k.paint(S.face_cream(F), CREAM)
    k.paint(S.front_cream(F), CREAM, on=["torso", "chest", "neck", "chin", "thigh"])
    S.mouth(k, F)

    lay, E, de = S.eyes(k, F)
    k.build(voxel=VOXEL, smooth=4, band=0.005)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    servine(*args())
