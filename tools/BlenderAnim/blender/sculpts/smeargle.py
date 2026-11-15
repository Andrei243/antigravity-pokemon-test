# Smeargle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/smeargle.py -- <the export's Smeargle.glb> <out.glb>
#
# From the references: a cream beagle standing upright, nearly half of it head. Over the head a wide cream beret of fur
# with a point standing up from its crown, leaning back; long brown ears hanging from under it; a long muzzle carried
# forward and a little down, the tip of a pink tongue out under it; round eyes on the sides of the head, a white with a
# black pupil in a thick brown ring. A brown collar round the neck; slim arms held forward and down, two brown stripes
# round each forearm, small hands of three fingers; legs on cream feet, a brown band at each ankle. A long thin tail out
# of the back, round the model's right (+X) side to the front, up through the right hand to a brush: a white bulb and a
# green tip with a ragged lower edge. A brown paw print on the back. (The brief's "brown cap": the pictures show the
# beret cream like the coat; its brown is the ears, the eye rings, the collar and the stripes.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, ring_band, outward, srgb, inside, tri, unit, eye_look, args)

# --- colours ----------------------------------------------------------------------------------------------------------
CREAM = "#EEE6D6"
CAP = "#E6DDC8"
BROWN = "#9A4C2E"
GREEN = "#8EC43E"
TONGUE = "#E58A9A"
NOSE = "#3A2E30"
WHITE, INK = "#FBFAF6", "#1C1A1E"

# --- the frame (the skeleton's joints: body (0, 0, 0.35), head (0, -0.01, 0.48), arms (+-0.07, 0, 0.41),
#     ears (+-0.08, 0, 0.6), legs (+-0.055, 0, 0.25), tail (0, 0.07, 0.28)) -----------------------------------------------
H = np.array([0, -0.03, 0.53])           # the head's middle
HR = np.array([0.112, 0.102, 0.096])     # a tenth over the reference's measure
CAP_C = np.array([0, -0.005, 0.615])
CAP_R = np.array([0.15, 0.142, 0.072])


def on_head(x, z, h=H, hr=HR):
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / hr ** 2; n /= np.linalg.norm(n)
    return p, n


def flat_under(V):
    """The beret: domed above, nearly flat under its rim."""
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.35, V[:, 2])
    return V


def add_head(k):
    k.add("head", ell(H, HR), CREAM, "head")
    k.add("muzzle", tube([(0, -0.07, 0.515), (0, -0.14, 0.492), (0, -0.19, 0.476)], [0.056, 0.048, 0.044]), CREAM,
          "head")
    k.add("neck", tube([(0, 0.0, 0.39), (0, -0.015, 0.46)], [0.052, 0.054]), CREAM, "head")
    # long brown ears hanging from under the beret's rim
    k.add("ear", ell((0.122, -0.005, 0.522), (0.032, 0.05, 0.078), rot=(0, -12, 0)), BROWN, "ear{s}", mirror=True)


def add_cap(k):
    k.add("cap", ell(CAP_C, CAP_R, deform=flat_under), CAP, "head", fuse=False, surface="fur")
    k.add("point", outward(blade([(0, 0.0, 0.66), (0, 0.018, 0.72), (0, 0.04, 0.775)], [0.042, 0.024, 0.006],
                                 (1, 0, 0), 0.8)), CAP, "head", fuse=False, surface="fur")


def add_face(k):
    k.add("nose", ell((0, -0.224, 0.49), (0.014, 0.009, 0.01)), NOSE, "head", fuse=False, surface="shell")
    k.add("tongue", ell((0.008, -0.178, 0.438), (0.019, 0.012, 0.024), rot=(-10, 0, 0)), TONGUE, "head", fuse=False,
          surface="skin")


def add_body(k):
    def pear(V):
        V = V.copy()
        s = 1 - 0.14 * np.clip(V[:, 2], 0, 1) + 0.08 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.005, 0.31), (0.088, 0.078, 0.115), deform=pear), CREAM, "body")
    k.add("collar", ring_band((0, -0.008, 0.428), unit(0, -0.2, 1), 0.053, 0.032, thick=0.012), BROWN, "body",
          fuse=False, surface="fur")


def add_legs(k):
    k.add("thigh", ell((0.058, 0.0, 0.225), (0.058, 0.056, 0.068)), CREAM, "leg{s}", mirror=True)
    k.add("shin", tube([(0.06, 0.0, 0.2), (0.064, -0.004, 0.12), (0.066, -0.008, 0.05)], [0.045, 0.039, 0.035]),
          CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.067, -0.032, 0.03), (0.042, 0.062, 0.031)), CREAM, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.022, 0.0, 0.022)):
        k.add(f"toe{i}", ell((0.067 + dx, -0.082, 0.024), (0.015, 0.018, 0.02)), CREAM, "leg{s}", mirror=True)
    k.add("ankle", ring_band((0.066, -0.007, 0.072), (0, 0, 1), 0.034, 0.022, thick=0.008), BROWN, "leg{s}",
          fuse=False, mirror=True, surface="fur")


def add_arm(k, side, pts, hand, fingers):
    """A slim arm through pts to a small hand of three thick fingers, two brown stripes round the forearm."""
    s = "R" if side > 0 else "L"
    k.add("arm" + s, tube(pts, [0.03, 0.028, 0.026, 0.025]), CREAM, "arm" + s)
    k.add("hand" + s, ell(hand, (0.03, 0.03, 0.028)), CREAM, "arm" + s)
    for i, d in enumerate(fingers):
        d = unit(*d)
        k.add(f"finger{i}{s}", tube([hand + d * 0.015, hand + d * 0.042], [0.015, 0.013]), CREAM, "arm" + s)
    elbow, wrist = np.array(pts[-2]), np.array(pts[-1])
    d = unit(*(wrist - elbow))
    for j, t in enumerate((0.28, 0.62)):
        c = elbow + (wrist - elbow) * t
        k.add(f"stripe{j}{s}", ring_band(c, d, 0.025, 0.015, thick=0.007), BROWN, "arm" + s, fuse=False,
              surface="fur")


HAND_R = np.array([0.205, -0.112, 0.43])        # the right hand, round the tail


def add_arms(k):
    # the right (+X) forearm comes forward to hold the tail; the left hangs forward and down
    add_arm(k, 1, [(0.05, 0.0, 0.415), (0.072, 0.0, 0.41), (0.14, -0.03, 0.37), (0.195, -0.088, 0.412)], HAND_R,
            [(0.3, -0.6, 0.2), (0.0, -0.8, -0.1), (-0.4, -0.6, 0.05)])
    add_arm(k, -1, [(-0.05, 0.0, 0.415), (-0.072, 0.0, 0.41), (-0.12, -0.03, 0.355), (-0.135, -0.085, 0.315)],
            np.array([-0.138, -0.112, 0.302]), [(-0.4, -0.6, -0.4), (0.0, -0.7, -0.6), (0.4, -0.6, -0.4)])


TAIL = [(0, 0.06, 0.29), (0.04, 0.16, 0.255), (0.15, 0.18, 0.26), (0.245, 0.07, 0.3), (0.25, -0.05, 0.37),
        (0.212, -0.11, 0.43), (0.205, -0.118, 0.5)]
BRUSH = np.array([0.205, -0.12, 0.548])


def add_tail(k):
    """Out of the back, round the right side to the front, up through the right hand to a brush tipped green."""
    k.add("tail", tube(TAIL, [0.026, 0.022, 0.02, 0.02, 0.02, 0.02, 0.022]), CREAM, "tail")
    k.add("brush", ell(BRUSH, (0.036, 0.036, 0.05)), CREAM, "tail")

    def taper(V):
        V = V.copy()
        s = 1 - 0.55 * np.clip(V[:, 2], 0, 1) ** 1.5
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("brushtip", ell(BRUSH + (0.0, 0.004, 0.068), (0.04, 0.036, 0.075), rot=(8, 0, 0), deform=taper), CREAM,
          "tail")

    def green(P):
        dx, dy = P[:, 0] - BRUSH[0], P[:, 1] - BRUSH[1]
        th = np.arctan2(dy, dx) / (2 * math.pi)
        edge = BRUSH[2] + 0.03 + 0.016 * tri(5 * th)
        return inside(edge - P[:, 2], 0.003) * (np.hypot(dx, dy) < 0.07) * (P[:, 2] > BRUSH[2] - 0.02)
    k.paint(green, GREEN)


def add_print(k):
    """A brown paw print on the back: a round pad and three toes."""
    k.decal((0, 0.08, 0.35), (0, 1, 0), (0.026, 0.021), BROWN, eps=0.003, rings=10, segs=44)
    for x, z in ((-0.027, 0.383), (0.0, 0.394), (0.027, 0.383)):
        k.decal((x, 0.075, z), (0, 1, 0.1), (0.011, 0.012), BROWN, eps=0.003, rings=6, segs=32)


# The eyes: on the sides of the head, a white with a black pupil inside a thick brown ring that stays in every state
CW, CI, CB, CC = srgb(WHITE), srgb(INK), srgb(BROWN), srgb(CREAM)
RING = 0.66


def inner(u, v):
    c = np.tile(CW, (len(u), 1))
    c[np.hypot(u + 0.08, v + 0.02) < 0.34] = CI
    return c


def lay_eyes(k, state, x=0.074, z=0.538, half=(0.036, 0.036)):
    f = eye_look(state, inner, CC, CI, line=0.13)

    def look(u, v):
        c = f(u / RING, v / RING)
        c[np.hypot(u, v) > RING] = CB
        return c
    for s in (1, -1):
        p, n = on_head(x * s, z)
        col = look if s == 1 else (lambda a, b: look(-a, b))
        k.decal(p, n, half, col, eps=0.004, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    add_tail(k)
    add_cap(k)
    add_face(k)
    add_print(k)
    k.build(voxel=0.0055, band=0.005)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
