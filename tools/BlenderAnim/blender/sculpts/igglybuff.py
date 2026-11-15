# Igglybuff sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Jigglypuff's script (jigglypuff.py, imported: its ball, front and normals, arms, feet and the way its face is laid):
#   blender --background --factory-startup --python sculpts/igglybuff.py -- <the export's Igglybuff.glb> <out.glb>
# From the references: a pale pink ball that is all of it, no ears; a puffy tuft of three round lobes on its crown;
# a dark spiral drawn on the brow; large eyes, a dark pupil in a red iris with a dark rim and a glint; a small open
# mouth with a pink tongue; little rounded stubs of arms and two small oval feet under the ball.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, boxy, eye_look, sibling, args

j = sibling("jigglypuff")

PINK = "#F8CDD8"
SPIRAL = "#3A2630"
INK, RED, PUPIL = "#2A1A20", "#D2384A", "#1C1216"
WHITE = "#FFFFFF"
MOUTH, TONGUE = "#B83A52", "#F28A9E"

# --- the frame (the skeleton's joints: body (0, 0, 0.17), arms (+-0.13, -0.04, 0.16), legs (+-0.06, -0.02, 0.05));
#     no head bone, so the face and the tuft go on the body ------------------------------------------------------------
j.C = np.array([0, 0, 0.192])
j.R = np.array([0.162, 0.152, 0.152])
C, R = j.C, j.R
TOP = C[2] + R[2]
EYE_X, EYE_Z = 0.074, 0.012
EYE_HALF = (0.04, 0.042)
MOUTH_Z = -0.038


def ball(V):
    """Round, a little fuller below its middle."""
    V = boxy(2.1)(V)
    s = 1 + 0.04 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def add_tuft(k, color=PINK):
    """The tuft on the crown: a short stem and three round lobes, the middle one tallest, a fourth behind."""
    k.add("tuftstem", ell((0, 0.012, TOP - 0.01), (0.05, 0.045, 0.04)), color, "body")
    for name, c, r in (("tuftmid", (0.0, -0.004, TOP + 0.05), (0.043, 0.04, 0.042)),
                       ("tuftR", (0.05, 0.006, TOP + 0.03), (0.036, 0.034, 0.034)),
                       ("tuftL", (-0.047, 0.01, TOP + 0.028), (0.034, 0.032, 0.033)),
                       ("tuftback", (0.0, 0.045, TOP + 0.022), (0.035, 0.032, 0.032))):
        k.add(name, ell(c, r), color, "body")


CP, CS = srgb(PINK), srgb(SPIRAL)


def spiral(u, v):
    """A dark spiral line of a turn and a half, from the middle outward, counter-clockwise."""
    c = np.tile(CP, (len(u), 1))
    r = np.hypot(u, v)
    th = np.mod(np.arctan2(v, u), 2 * math.pi)
    pitch = 0.42                                  # how far the line moves out in a whole turn
    best = np.full(len(u), 9.0)
    for turn in range(3):
        rr = 0.1 + pitch * (th + 2 * math.pi * turn) / (2 * math.pi)
        ok = rr < 0.98
        best = np.where(ok, np.minimum(best, np.abs(r - rr)), best)
    c[(best < 0.085) & (r < 0.98)] = CS
    return c


def add_spiral(k):
    at = j.front(0.0, C[2] + 0.66 * R[2])
    k.decal(at, j.normal_at(at), (0.05, 0.05), spiral, eps=0.003, rings=40, segs=120, up=(0, -0.3, 1))


def open_mouth(u, v):
    """A small open mouth, its top a gentle double arc, a pink tongue in its foot."""
    c = np.tile(CP, (len(u), 1))
    top = 0.55 - 0.25 * np.abs(np.sin(u * math.pi))
    m = (np.hypot(u / 0.95, (v - 0.2) / 1.2) < 1) & (v < top)
    c[m] = srgb(MOUTH)
    c[m & (np.hypot(u / 0.55, (v + 0.65) / 0.42) < 1)] = srgb(TONGUE)
    return c


CI, CR, CPU, CW = srgb(INK), srgb(RED), srgb(PUPIL), srgb(WHITE)


def eye(u, v):
    """A dark rim round a red iris and a large dark pupil, one glint up and out."""
    c = np.tile(CI, (len(u), 1))
    r = np.hypot(u, v)
    c[r < 0.84] = CR
    c[np.hypot((u + 0.02) / 0.56, (v + 0.02) / 0.58) < 1] = CPU
    c[np.hypot((u - 0.22) / 0.2, (v - 0.26) / 0.2) < 1] = CW
    return c


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    j.add_body(k, color=PINK, deform=ball)
    add_tuft(k)
    j.add_arms(k, root=(0.13, -0.04, 0.168), tip=(0.19, -0.08, 0.152), radii=(0.044, 0.037, 0.031), color=PINK)
    j.add_feet(k, at=(0.064, -0.04, 0.024), r=(0.036, 0.05, 0.025), turn=15, color=PINK,
               leg=((0.06, -0.02, 0.07), (0.062, -0.032, 0.04)), leg_r=(0.036, 0.032))
    add_spiral(k)
    j.add_mouth(k, z=MOUTH_Z, half=(0.026, 0.022), look=open_mouth)
    k.build(voxel=0.005, band=0.008)
    k.eye_states(lambda s: j.lay_eyes(k, s, look=eye, half=EYE_HALF, x=EYE_X, z=EYE_Z, lid=PINK))
    k.export(out)
