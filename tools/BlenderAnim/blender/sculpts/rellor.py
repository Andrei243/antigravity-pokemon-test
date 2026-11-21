# Rellor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rellor.py -- <the export's Rellor.glb> <out.glb>
#
# From the references: a great round ball of brown mud, the largest mass by far, marked all over with short curved
# streaks, orange and pale grey, raised a little like dabs of clay; behind it on its right (+X) a small beetle doing a
# handstand against it: a round grey-olive shell turned to the front, on its side toward the ball a stack of three
# wavy cream folds, under them a pink grub-like face pressed to the ball with a dark bristly beard below it and one
# eye looking out at the viewer; four thin grey legs, two braced on the ground and two flung up into the air with
# hooked feet. The ball is a loose piece the beetle's face and lamellae touch.
# Skeleton: body (0.08, 0.03, 0.08), head (0.08, -0.01, 0.075), leg{s}/hind{s} (0.06 or 0.12, 0.01 / 0.06, 0.065).
# Rabsca (rabsca.py) grows from it through sibling: the eye and the leg are its functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, args, outward, axes, lumpy

MUD, STREAK, STREAK_GREY = "#8A6A42", "#F0A032", "#BDB9B2"
SHELL, CREAM, FACE, BEARD = "#706B5E", "#EAD8A0", "#DC6A94", "#34322C"
LEG, WHITE, PUPIL = "#7C7668", "#FAFAF6", "#1E1A1E"

BALL_C, BALL_R = np.array([-0.062, -0.02, 0.121]), 0.121


def ball(k, bone="body"):
    """The mud ball, loose so it stays a true ball, and its streaks: short tubes laid along arcs, half sunk in."""
    k.add("ball", outward(ell(BALL_C, (BALL_R, BALL_R, BALL_R), sub=5)), MUD, bone, fuse=False, surface="skin")
    rs = np.random.RandomState(4)
    g = (math.sqrt(5) - 1) / 2
    n = 18
    for i in range(n):
        zz = 1 - 2 * (i + 0.5) / n
        th = 2 * math.pi * g * i
        d = np.array([math.sqrt(1 - zz * zz) * math.cos(th), math.sqrt(1 - zz * zz) * math.sin(th), zz])
        if d[2] < -0.75:
            continue
        a, b = axes(d)
        ang = rs.rand() * math.pi
        t = a * math.cos(ang) + b * math.sin(ang)
        L = 0.3 + 0.12 * rs.rand()
        bend = (rs.rand() - 0.5) * 0.5
        pts = []
        for s in np.linspace(-L / 2, L / 2, 6):
            dd = d * math.cos(s) + t * math.sin(s)
            dd = dd + np.cross(d, t) * bend * (s * s * 4 - 0.1)
            dd /= np.linalg.norm(dd)
            pts.append(BALL_C + dd * (BALL_R - 0.003))
        col = STREAK_GREY if i % 5 in (1, 3) else STREAK
        k.add(f"streak{i}", outward(tube(pts, [0.0105] * 6, seg=12, per=4)), col, bone, fuse=False, surface="skin")


SHELL_C = np.array([0.1, 0.03, 0.13])


def beetle(k):
    k.add("shell", ell(SHELL_C, (0.068, 0.036, 0.07), rot=(0, 0, 19)), SHELL, "body", surface="shell")
    k.add("shellback", ell(SHELL_C + (0.006, 0.02, -0.004), (0.06, 0.03, 0.062), rot=(0, 0, 19)), SHELL, "body",
          surface="shell")
    for i, (c, r) in enumerate([((0.07, -0.008, 0.176), (0.034, 0.024, 0.018)),
                                ((0.064, -0.012, 0.146), (0.036, 0.026, 0.019)),
                                ((0.062, -0.014, 0.115), (0.033, 0.025, 0.018))]):
        k.add(f"fold{i}", ell(c, r, rot=(0, 15, 19), deform=lumpy(3, 1, 0.12)), CREAM, "body", surface="shell")


FACE_C, FACE_R = np.array([0.046, -0.03, 0.074]), np.array([0.039, 0.034, 0.031])
FACE_N = unit(-0.45, -0.9, -0.05)


def face(k):
    k.add("face", ell(FACE_C, FACE_R, rot=(0, 0, 25)), FACE, "head", surface="skin")
    k.add("brow", ell(FACE_C + (0.006, -0.004, 0.018), FACE_R * (0.85, 0.75, 0.55), rot=(0, 0, 25)), FACE, "head",
          surface="skin")
    rs = np.random.RandomState(2)
    side = unit(0.9, -0.45, 0)
    for i, s in enumerate(np.linspace(-0.026, 0.026, 6)):
        base = FACE_C + side * s + np.array([0, -0.006, -0.018])
        d = unit(side[0] * s * 10, -0.3, -1)
        a, b = axes(d)
        w = 0.008
        tip = base + d * (0.024 + 0.008 * rs.rand())
        k.add(f"beard{i}", hull([base + a * w, base - a * w + b * w, base - a * w - b * w, tip]), BEARD, "head",
              fuse=False, surface="hair")


def leg(k, name, pts, bone, r=(0.0088, 0.0078, 0.0068, 0.006), hook=None, color=LEG):
    k.add(name, outward(tube(pts, r[:len(pts)], seg=12, per=6)), color, bone, fuse=False, surface="shell")
    for i in range(1, len(pts) - 1):
        k.add(f"{name}knee{i}", outward(ell(pts[i], (r[i] * 1.25,) * 3, sub=2)), color, bone, fuse=False,
              surface="shell")
    if hook is not None:
        k.add(name + "hook", outward(tube([pts[-1], hook], [r[-1], 0.0045], seg=10, per=4)), color, bone,
              fuse=False, surface="shell")


def legs(k):
    leg(k, "legup1", [(0.13, 0.05, 0.14), (0.185, 0.045, 0.19), (0.17, 0.035, 0.245), (0.2, 0.03, 0.265)],
        "hindR", hook=(0.215, 0.028, 0.252))
    leg(k, "legup2", [(0.105, 0.06, 0.18), (0.125, 0.07, 0.235), (0.18, 0.075, 0.272), (0.198, 0.075, 0.292)],
        "hindL", hook=(0.215, 0.075, 0.286))
    leg(k, "legdown1", [(0.135, 0.01, 0.1), (0.192, -0.015, 0.085), (0.205, -0.03, 0.006)], "legR",
        hook=(0.232, -0.036, 0.005))
    leg(k, "legdown2", [(0.085, 0.05, 0.09), (0.085, 0.09, 0.05), (0.075, 0.115, 0.006)], "legL",
        hook=(0.09, 0.135, 0.005))


# ---- the eyes: small, white with a big dark pupil, on the pink face's outer side ----------------------------------
CW, CP, CF = srgb(WHITE), srgb(PUPIL), srgb(FACE)
EYE_HALF = (0.0132, 0.0138)


def round_eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[(u / 0.58) ** 2 + ((v + 0.05) / 0.66) ** 2 < 1] = CP
    c[((u + 0.18) / 0.2) ** 2 + ((v - 0.26) / 0.2) ** 2 < 1] = CW
    return c


def eye_spots():
    side = unit(0.9, -0.45, 0)
    out = []
    for s in (1, -1):
        d = FACE_N + side * 0.62 * s + np.array([0, 0, 0.45])
        d /= np.linalg.norm(d)
        out.append((s, FACE_C + d * FACE_R * 0.98, d))
    return out


def lay(k):
    def eyes(state):
        look = eye_look(state, round_eye, CF, CP, line=0.17)
        for s, c, n in eye_spots():
            k.decal(c, n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0015,
                    rings=20, segs=56)
    return eyes


def rellor(src, out):
    k = Kit(src, coat="shell")
    beetle(k)
    face(k)
    ball(k)
    legs(k)
    k.build(voxel=0.0028, smooth=3, band=0.004, wband=0.03)
    k.eye_states(lay(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    rellor(src, out)
