# Quilladin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Chespin's script (chespin.py, imported: the line's colours, the soft quill, the nose, the open mouth and the way a
# face is painted into the green):
#   blender --background --factory-startup --python sculpts/quilladin.py -- <the export's Quilladin.glb> <out.glb>
# From the references: a round green ball of a body, its tan face set into the upper front of it under a shock of six
# brown quills; dark brown bars down both cheeks at the face's edge; round eyes, white with a brown iris and a black
# pupil; a salmon nose pointing up; a wide open D-shaped mouth (white teeth, pink tongue); two green quills out of the
# ball's upper sides, up and out, tipped orange; the lower body brown, its top edge a jagged leaf of points over the
# belly; thick brown arms with three small white claws at the hands; short thick brown legs with a big white spike at
# each knee; a thin green tail tipped orange, hanging down behind.
# (The pictures overrule the task's text: there is no "chestnut point" on the head; what stands up are the six brown
# quills of the hair and the two green, orange-tipped quills of the shoulders; the arms are held out in the art and
# carried forward and down here.)
# Reference ratios (3D picture, the ball): ball width / ball height 0.92; face width / ball width 0.69; face height /
# ball height 0.43; brown quills above the ball / height 0.13; arm thickness / ball width 0.24; legs under the ball /
# height 0.14.
# The export's joints: body (0, 0, 0.2), arms (+-0.11, -0.02, 0.25), head (0, -0.03, 0.29), legs (+-0.06, 0, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, axes, polygon, hull, sibling

cp = sibling("chespin")
GREEN, TAN, BROWN, NOSE, WHITE, ORANGE, INK = cp.GREEN, cp.TAN, cp.BROWN, cp.NOSE, cp.WHITE, cp.ORANGE, cp.INK
IRIS = "#6A4A36"
CLAW = "#F2F0EA"

# --- the frame ----------------------------------------------------------------------------------------------------------
BALL = np.array([0.0, 0.0, 0.218])
BALLR = np.array([0.152, 0.138, 0.152])
FACE = np.array([0.0, -0.05, 0.298])        # the face's bulge out of the ball's upper front
FACER = np.array([0.1, 0.075, 0.072])
FACE_Z, FACE_HALF = 0.3, (0.097, 0.068)
EYE_X, EYE_Z, EYE_HALF = 0.043, 0.323, (0.0165, 0.0205)
SH = np.array([0.112, -0.03, 0.252])
HAND = np.array([0.142, -0.145, 0.205])


def add_ball(k):
    k.add("ball", ell(BALL, BALLR), GREEN, "body", surface="shell")
    k.add("face", ell(FACE, FACER), GREEN, "head")          # painted tan below
    def face(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        e = (x / FACE_HALF[0]) ** 2 + ((z - FACE_Z) / FACE_HALF[1]) ** 2
        return inside(e - 1, 0.025) * (y < -0.06)
    k.paint(face, TAN)
    # the lower body brown, its top a jagged leaf of points over the belly and lower behind
    def lower(P):
        x, y, z = P[:, 0], P[:, 1] - BALL[1], P[:, 2]
        a = np.arctan2(x, -y)                                   # 0 at the front
        front = np.clip(1 - np.abs(a) / 1.6, 0, 1)
        t = (a / (2 * math.pi) * 9.0) % 1.0
        points = 1 - np.abs(2 * t - 1)                           # a point every fortieth of a turn
        edge = 0.142 + 0.03 * front + 0.03 * points * front + 0.05 * np.clip(1 - np.abs(a) / 0.28, 0, 1)
        return inside(z - edge, 0.0018) * (z < 0.3)
    k.paint(lower, BROWN, on=["ball", "thigh", "hip"])


def add_hair(k):
    """Six brown quills over the face, fanning up and out of the ball's top."""
    for i, (x, ln, lean) in enumerate(((-0.05, 0.06, -0.7), (-0.026, 0.075, -0.35), (-0.006, 0.085, -0.05),
                                       (0.014, 0.08, 0.2), (0.036, 0.07, 0.5), (0.056, 0.055, 0.85))):
        base = (x, -0.06 + abs(x) * 0.3, 0.352 - abs(x) * 0.35)
        cp.quill(k, f"hair{i}", base, (lean, 0.25, 1.0), ln, 0.0165, BROWN, "head", bend=0.1)


def add_face(k):
    # the dark brown bars down the cheeks at the face's edge
    p, n = cp.on_ell(0.08, 0.298, FACE, FACER)
    k.decal(p, unit(n[0], n[1], n[2] * 0.4), (0.014, 0.03), BROWN, eps=0.002, rings=10, segs=40, mirror=True,
            outline=polygon([(-0.7, 1), (1, 0.85), (0.75, -1), (-0.95, -0.8)]))
    p, n = cp.on_ell(0.0, 0.276, FACE, FACER)
    cp.open_mouth(k, p, unit(0, n[1], n[2] * 0.5), (0.043, 0.03), eps=0.0022)
    # the nose: a salmon cone pointing up and forward from between the eyes
    q, m = cp.on_ell(0.0, 0.318, FACE, FACER)
    d = unit(0, -0.55, 0.85)
    a, b = axes(d)
    tip = q + d * 0.038
    k.add("nose", outward(tube([q - m * 0.004, q + d * 0.012, tip], [0.015, 0.012, 0.003], seg=16, per=5)), cp.NOSE,
          "head", fuse=False, surface="skin")


def add_horns(k):
    """The two green quills out of the ball's upper sides, up and out, their tips orange."""
    for side, s in (("R", 1), ("L", -1)):
        base = np.array((0.105 * s, 0.012, 0.31))
        d = unit(0.62 * s, 0.12, 0.8)
        cp.quill(k, "horn" + side, base, d, 0.13, 0.024, GREEN, "body", bend=0.05)
        tipz = base + d * 0.13 * 0.62
        k.paint(lambda P, t=tipz, d=d: inside(((P - t) @ d) * -1, 0.0015), ORANGE, on=["horn" + side])


def add_arms(k):
    """Short thick brown arms carried forward and a little down, a round fist with three white claws."""
    d = unit(*(HAND - SH))
    k.add("arm", tube([SH - d * 0.02, SH + (HAND - SH) * 0.5, HAND], [0.036, 0.037, 0.037]), BROWN, "arm{s}",
          mirror=True)
    k.add("hand", ell(HAND + d * 0.01, (0.044, 0.046, 0.041)), BROWN, "arm{s}", mirror=True)
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.118, 0.0016) * (P[:, 2] > 0.17) * (P[:, 2] < 0.33)
            * (np.abs(P[:, 0]) > 0.09), GREEN, on=["ball"])
    # three white claws seated in the fist: two at its end, one over it
    a, b = axes(d)
    c = HAND + d * 0.01
    for i, (da, db) in enumerate(((0.62, -0.3), (-0.62, -0.3), (0.0, 0.75))):
        out = unit(*(d * 0.75 + (a * da + b * db) * 0.75))
        q = c + out * 0.034
        oa, ob = axes(out)
        k.add(f"claw{i}", hull([q + oa * 0.013, q - oa * 0.013, q + ob * 0.013, q - ob * 0.013, q - out * 0.014,
                                q + out * 0.036]), CLAW, "arm{s}", fuse=False, mirror=True, surface="shell")


def add_legs(k):
    k.add("hip", ell((0.065, 0.0, 0.105), (0.06, 0.06, 0.05)), BROWN, "leg{s}", mirror=True)
    k.add("thigh", tube([(0.068, -0.005, 0.085), (0.072, -0.02, 0.035)], [0.04, 0.036]), BROWN, "leg{s}", mirror=True)
    k.add("foot", ell((0.074, -0.04, 0.02), (0.04, 0.055, 0.022)), BROWN, "leg{s}", mirror=True)
    # a big white spike at each knee's outer side, pointing out and up, its root in the leg
    q = np.array((0.098, -0.03, 0.075))
    d = unit(0.8, -0.35, 0.5)
    a, b = axes(d)
    k.add("kneespike", hull([q + a * 0.019, q - a * 0.019, q + b * 0.019, q - b * 0.019, q - d * 0.02,
                             q + d * 0.058]), CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")


def add_tail(k):
    pts = [(0, 0.12, 0.12), (0.012, 0.16, 0.095), (0.025, 0.188, 0.066), (0.034, 0.205, 0.045)]
    k.add("tail", tube(pts, [0.017, 0.015, 0.011, 0.005]), GREEN, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.078, 0.0015) * (P[:, 1] > 0.15), ORANGE, on=["tail"])


def ringed_eye():
    """White with a brown iris, a black pupil and a glint; a thin dark rim."""
    W_, R_, I_ = srgb(WHITE), srgb(IRIS), srgb(INK)
    def look(u, v):
        c = np.tile(W_, (len(u), 1))
        c[u * u + v * v > 0.84 ** 2] = I_
        c[((u + 0.06) / 0.6) ** 2 + ((v + 0.02) / 0.66) ** 2 < 1] = R_
        c[((u + 0.06) / 0.34) ** 2 + ((v + 0.02) / 0.4) ** 2 < 1] = I_
        c[((u - 0.12) / 0.17) ** 2 + ((v - 0.25) / 0.2) ** 2 < 1] = W_
        return c
    return look


def lay_eyes(k, state):
    look = eye_look(state, ringed_eye(), srgb(TAN), srgb(INK), line=0.17)
    p, n = cp.on_ell(EYE_X, EYE_Z, FACE, FACER)
    k.decal(p, unit(n[0] * 0.8, n[1], n[2] * 0.6), EYE_HALF, look, eps=0.0022, rings=20, segs=56, mirror=True)


def quilladin(src, out):
    k = Kit(src, coat="shell")
    add_ball(k)
    add_hair(k)
    add_face(k)
    add_horns(k)
    add_arms(k)
    add_legs(k)
    add_tail(k)
    k.build(voxel=0.0045, smooth=4, band=0.005, wband=0.035)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    quilladin(*args())
