# Krabby sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/krabby.py -- <the export's Krabby.glb> <out.glb>
#
# From the references: a small crab. A low round body, red-orange over the top and cream below, the line between them
# across the front a little under the eyes with a small cream point rising into the orange on each side; two big white
# eyes with small dark pupils, set on orange bumps at the top of the front; a little fork of two points on its crown.
# Two big orange pincers held up high and out to the sides on thin pale arms, each a thick C opening in and down, dark
# inside. Two thin pale legs a side, knobbed at the joints, splayed out to pointed feet.
#
# Written to be grown from (kingler.py imports it with sibling): the frame is in the constants and the functions that
# take the kit; the build is under the main guard. Bones: root, body, arm{s}, leg{s}.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, polygon

ORANGE, CREAM, LIMB = "#E8703C", "#F2E8D8", "#DCD4CA"
DARK, WHITE, INK, PUPIL = "#3A3A42", "#FAFAF6", "#2A1E1A", "#141014"

X = np.array([1.0, 0, 0]); Z = np.array([0, 0, 1.0])

# ---- the body: a low dome, wider than deep -------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.0, 0.135]), np.array([0.15, 0.122, 0.095])
BUMP_C, BUMP_R = np.array([0.066, -0.07, 0.197]), np.array([0.05, 0.042, 0.046])


def body(k, color=ORANGE):
    k.add("body", ell(BODY_C, BODY_R), color, "body")
    k.add("under", ell(BODY_C + (0, -0.01, -0.03), BODY_R * (0.94, 0.95, 0.7)), color, "body")
    k.add("bump", ell(BUMP_C, BUMP_R), color, "body", mirror=True)


def cream_line(k, front=0.127, back=0.12, fangs=((0.075, 0.035, 0.018),), color=CREAM):
    """The cream below a line round the body: low across the front, with points rising into the orange (|x|, rise,
    half width each)."""
    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        t = np.clip((y + 0.12) / 0.24, 0, 1)                      # 0 at the front, 1 at the back
        line = front + (back - front) * t
        frontness = np.clip(-y / 0.08, 0, 1)
        for ax, rise, w in fangs:
            line = line + rise * np.clip(1 - np.abs(np.abs(x) - ax) / w, 0, 1) * frontness
        return inside(z - line, 0.0025)
    k.paint(mask, color)


def crown_fork(k, color=ORANGE):
    """A little fork on the crown: a short stalk and two points leaning out."""
    k.add("stalk", tube([(0, -0.035, 0.21), (0, -0.04, 0.247)], [0.026, 0.02]), color, "body")
    k.add("prong", tube([(0.004, -0.04, 0.243), (0.02, -0.044, 0.275), (0.034, -0.046, 0.303)],
                        [0.016, 0.012, 0.007]), color, "body", mirror=True)


# ---- limbs: thin pale tubes with knobs at the joints (loose, so they keep their edges) -------------------------------
def limb(k, name, pts, radii, bone, knobs=(), knob_r=0.019, color=LIMB):
    k.add(name, outward(tube(pts, radii, seg=14, per=6)), color, bone, fuse=False, surface="shell")
    for i in knobs:
        k.add(f"{name}knob{i}", outward(ell(pts[i], (knob_r, knob_r, knob_r), sub=2)), color, bone, fuse=False,
              surface="shell")


LEGS = [  # hip at the body's side, knee, toe, for +X (y forward is -): short arches splayed out, two thick segments
    ("legfront", (0.125, -0.045, 0.095), (0.185, -0.08, 0.13), (0.212, -0.105, 0.008)),
    ("leghind", (0.125, 0.045, 0.095), (0.185, 0.075, 0.13), (0.212, 0.1, 0.008)),
]


def legs(k, legs=LEGS, r=(0.0225, 0.019, 0.015)):
    for side, sn in ((1, "R"), (-1, "L")):
        m = np.array([side, 1, 1.0])
        for name, *pts in legs:
            P = [np.array(p) * m for p in pts]
            limb(k, name + sn, P, r, "leg" + sn, knobs=(1,), knob_r=0.024)
            # a short toe forward at the foot
            toe = P[-1] + np.array([0.014 * side, -0.02, 0.002])
            k.add(name + sn + "toe", outward(tube([P[-2] * 0.2 + P[-1] * 0.8, toe], [0.013, 0.007], seg=10, per=4)),
                  LIMB, "leg" + sn, fuse=False, surface="shell")


# ---- the pincers: a thick C in the plane facing the front, opening in and down, dark inside, closed behind ---------
def pincer(k, side, centre, rx, rz, r, opening=205.0, gap=32.0, color=ORANGE, inner=DARK, depth=None, name="claw"):
    """side +1 on +X (armR), -1 on -X (armL). The C runs round from its lower finger's tip to its upper finger's,
    away from the opening (degrees in its own plane: 0 out to the side, 90 up, 180 in toward the body)."""
    sn = "R" if side > 0 else "L"
    bone = "arm" + sn
    out = X * side
    c = np.array(centre, float) * np.array([side, 1, 1])
    n = 15
    ang = np.radians(opening + gap + np.linspace(0, 1, n) * (360 - 2 * gap))
    P = [c + out * rx * math.cos(a) + Z * rz * math.sin(a) for a in ang]
    t = np.linspace(0, 1, n)
    # the lower finger short and thin, the upper one broad: the C thickest a little past its middle
    R = r * np.clip(0.28 + 0.9 * np.sin(np.pi * np.clip(t * 0.96 + 0.02, 0, 1)) ** 0.7, 0, 1.0)
    R[0] = r * 0.22; R[-1] = r * 0.26
    R[-2] = r * 0.55
    k.add(name + sn, outward(tube(P, R, seg=20, per=4)), color, bone, fuse=False, surface="shell")
    d = depth or r
    k.add(name + sn + "in", outward(ell(c + (0, -0.002, 0), (rx * 0.82, d * 0.6, rz * 0.82), sub=3)), inner, bone,
          fuse=False, surface="shell")
    k.add(name + sn + "back", outward(ell(c + (0, d * 0.62, 0), (rx * 0.9, d * 0.42, rz * 0.9), sub=3)), color, bone,
          fuse=False, surface="shell")


KRABBY_ARM = [(0.125, -0.04, 0.155), (0.2, -0.07, 0.2), (0.225, -0.085, 0.27)]
KRABBY_CLAW = dict(centre=(0.21, -0.09, 0.36), rx=0.078, rz=0.092, r=0.05, gap=40.0)


def arms_and_claws(k, arm=KRABBY_ARM, claws=(KRABBY_CLAW, KRABBY_CLAW)):
    """claws: (+X's, -X's)."""
    for side, spec in ((1, claws[0]), (-1, claws[1])):
        sn = "R" if side > 0 else "L"
        m = np.array([side, 1, 1.0])
        a = arm if callable(arm) is False else arm(side)
        limb(k, "arm" + sn, [np.array(p) * m for p in a], [0.017, 0.016, 0.015], "arm" + sn, knobs=(1,), knob_r=0.022)
        pincer(k, side, **spec)


# ---- the eyes: big white eyes with a thin dark rim and a small dark pupil, on the bumps ---------------------------
CO, CW, CK, CP = srgb(ORANGE), srgb(WHITE), srgb(INK), srgb(PUPIL)
EYE_N = unit(0.32, -1, 0.42)
EYE_HALF = (0.031, 0.028)


def krabby_eye(u, v):
    c = np.tile(CK, (len(u), 1))
    rr = np.sqrt(u * u + v * v)
    c[rr < 0.84] = CW
    c[((u + 0.1) / 0.36) ** 2 + ((v - 0.05) / 0.4) ** 2 < 1] = CP
    return c


def eye_centre():
    return BUMP_C + EYE_N * np.array([BUMP_R[0], BUMP_R[1], BUMP_R[2]]) * 0.95


def krabby_eyes(k):
    def lay(state):
        k.decal(eye_centre(), EYE_N, EYE_HALF, eye_look(state, krabby_eye, CO, CK, line=0.17), eps=0.005,
                rings=20, segs=56, mirror=True)
    return lay


def krabby(src, out):
    k = Kit(src, coat="shell")
    body(k)
    cream_line(k)
    crown_fork(k)
    legs(k)
    arms_and_claws(k)
    k.build(voxel=0.0045, smooth=3, band=0.006)
    k.eye_states(krabby_eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    krabby(src, out)
