# Melcut sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/melcut.py -- <the export's Melcut.glb> <out.glb>
# Carpathia No. 10043 (plan 46), a garden snail, Bug. From its picture: a soft cream body rising at the front into a
# round head, a long low foot tapering to a point behind with a faint shining trail; two eye stalks lifting from the
# head's top, a little apart and out, each ending in a cream ball with a black dot; two short lower feelers drooping at
# the snout's corners, a small mouth between them; on the back a large round shell, its height some three quarters of
# the whole, striped in pale yellow and brown: brown bands running round the whorls and a brown suture coiling in.
# Written to be grown from (melcobor.py): the foot, the stalks, the eye and the shell's coil are functions.
# The skeleton: body (0, 0, 0.07), head (0, -0.12, 0.12), eye stalks earL/earR (-+0.018, -0.145, 0.185),
# shell (0, 0.045, 0.155).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

CREAM, CREAM_D = "#F4E2BE", "#E2C99E"
YELLOW, BROWN = "#E8E28A", "#9A7650"
WHITE, INK, MOUTHC = "#FBF8EE", "#1C1612", "#7A5236"
TRAIL = "#DDE4DA"


# --- pieces of the line --------------------------------------------------------------------------------------------
def foot(k, name, front, back, z, rx, rz, colour, bone="body"):
    """The foot: a long low body with a flat sole, round in front, tapering to a point behind (y front..back)."""
    cy, ry = (front + back) / 2, (back - front) / 2
    def shape(V):
        V = V.copy()
        V[:, 2] = np.maximum(V[:, 2], -0.86)
        t = smoothstep(0.05, 1.0, V[:, 1])
        V[:, 0] *= 1 - 0.72 * t
        V[:, 2] = V[:, 2] * (1 - 0.55 * t) - 0.3 * t * (V[:, 2] + 0.86)
        return V
    k.add(name, ell((0, cy, z), (rx, ry, rz), deform=shape), colour, bone)


def stalks(k, root, bend, tip, r0, r1, ball_r, colour):
    """Two eye stalks (mirrored, on earR/earL) from the head's top, each ending in a ball. Returns the ball's middle."""
    k.add("stalk", tube([root, bend, tip], [r0, (r0 + r1) / 2, r1]), colour, "ear{s}", mirror=True)
    ball = np.array(tip) + unit(*(np.array(tip) - np.array(bend))) * ball_r * 0.55
    k.add("ball", ell(ball, (ball_r, ball_r, ball_r * 0.96)), colour, "ear{s}", mirror=True)
    return ball


def eye_open(white=WHITE, ink=INK, rim=0.8):
    W, K = srgb(white), srgb(ink)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[u * u + v * v > rim * rim] = K
        return c
    return look


def lay_eyes(k, state, ball, ball_r, n, half, lid, pupil=0.5, ink=INK):
    """An eye on each ball: a white with a dark rim, a large black pupil of its own patch (open and fierce)."""
    E = ball + n * ball_r
    k.decal(E, n, half, eye_look(state, eye_open(ink=ink), srgb(lid), srgb(ink), line=0.17), eps=0.0012 + ball_r * 0.06,
            rings=20, segs=56, mirror=True)
    if state in ("Open", "Fierce"):
        up = np.array([0, 0, 1.0])
        right = np.cross(up, n); right /= np.linalg.norm(right)
        upv = np.cross(n, right)
        dv = 0 if state == "Open" else -0.22
        c = E + right * (-0.08 * half[0]) + upv * dv * half[1]
        s = pupil if state == "Open" else pupil * 0.85
        k.decal(c, n, (s * half[0], s * half[1] * 1.05), ink, eps=0.0022 + ball_r * 0.08, rings=10, segs=48, mirror=True)


def coil(C, R, turn=0.56, bands=8, curve=0.3):
    """
    The shell's coil seen on its sides, as plain numbers of a point (in y, z about the coil's middle C): the log
    spiral's phase (its suture where it is whole), and the radial bands' phase. Both go round the top continuously,
    so the bands carry over the shell's back.
    """
    def phases(P):
        dy, dz = P[:, 1] - C[1], P[:, 2] - C[2]
        r = np.sqrt(dy * dy + dz * dz) / R + 1e-4
        th = np.arctan2(dz, dy)
        ph = np.log(r) / math.log(turn) + th / (2 * math.pi)          # whole numbers on the suture
        band = th * bands / (2 * math.pi) + curve * np.log(r) * bands / 4
        return r, ph, band
    return phases


def frac(x):
    return x - np.floor(x)


# --- Melcut --------------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.132, 0.18]), np.array([0.06, 0.058, 0.054])
SHELL_C, SHELL_R = np.array([0.0, 0.05, 0.188]), np.array([0.082, 0.118, 0.115])
COIL_C = SHELL_C + np.array([0.0, 0.018, -0.012])


def build(src, out):
    k = Kit(src, coat="skin")
    # the foot, the column rising into the head, the hump under the shell
    foot(k, "foot", -0.165, 0.17, 0.038, 0.054, 0.04, CREAM)
    k.add("column", tube([(0, -0.105, 0.056), (0, -0.118, 0.11), (0, -0.126, 0.16)], [0.054, 0.052, 0.05]), CREAM, "head")
    k.add("hump", ell((0, 0.03, 0.075), (0.05, 0.1, 0.045)), CREAM, "body")
    k.add("head", ell(HEAD_C, HEAD_R), CREAM, "head")
    k.add("snout", ell((0, -0.176, 0.162), (0.04, 0.032, 0.032)), CREAM, "head")
    # the lower feelers drooping from the snout's corners
    k.add("feeler", tube([(0.024, -0.19, 0.16), (0.036, -0.206, 0.148), (0.04, -0.211, 0.136)], [0.0085, 0.0075, 0.007]),
          CREAM, "head", mirror=True)
    # the eye stalks
    ball = stalks(k, (0.02, -0.14, 0.218), (0.03, -0.15, 0.255), (0.038, -0.157, 0.282), 0.0115, 0.009, 0.021, CREAM)

    # the shell, fused, striped by paint
    k.add("shell", ell(SHELL_C, SHELL_R), YELLOW, "shell", surface="shell")
    k.add("boss", ell(COIL_C + np.array([0.07, 0, 0]), (0.022, 0.03, 0.03)), YELLOW, "shell", mirror=True, surface="shell")
    phases = coil(COIL_C, 0.115)
    def bands(P):
        r, ph, band = phases(P)
        b = inside(np.abs(frac(band) - 0.3) - 0.2, 0.03)
        return b * smoothstep(0.16, 0.24, r)
    def suture(P):
        r, ph, band = phases(P)
        return inside(np.abs(frac(ph + 0.5) - 0.5) - 0.1, 0.02) * (r > 0.1) + inside(r - 0.1, 0.01)
    k.paint(bands, BROWN, on=["shell", "boss"])
    k.paint(suture, BROWN, on=["shell", "boss"])
    # the foot's sole and the column's front a shade deeper
    k.paint(lambda P: inside(P[:, 2] - 0.012, 0.004), CREAM_D, on=["foot", "column"])

    # the shining trail behind
    k.add("trail", ell((0, 0.15, 0.0025), (0.034, 0.07, 0.0025), sub=3), TRAIL, "body", fuse=False, surface="skin")

    # the mouth: a small smile between the feelers
    M, B = srgb(MOUTHC), srgb(CREAM)
    def smile(u, v):
        c = np.tile(B, (len(u), 1))
        c[(np.abs(v - (0.3 - 0.75 * (1 - u * u))) < 0.28) & (np.abs(u) < 0.85)] = M
        return c
    k.decal((0, -0.205, 0.152), unit(0, -1, -0.15), (0.017, 0.009), smile, eps=0.0015, rings=8, segs=40)

    k.build(voxel=0.0028, smooth=4, band=0.005)

    n = unit(0.3, -0.92, 0.25)
    k.eye_states(lambda st: lay_eyes(k, st, ball, 0.021, n, (0.0165, 0.0165), lid=CREAM))
    k.export(out)


if __name__ == "__main__":
    build(*args())
