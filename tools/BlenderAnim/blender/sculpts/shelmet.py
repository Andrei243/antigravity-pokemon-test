# Shelmet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shelmet.py -- <the export's Shelmet.glb> <out.glb>
#
# From the references: a pink snail inside a pale grey helmet of a shell. The shell's back is a big round coil, a
# raised spiral on each side; its front is a wide open mouth between a broad visor above, flaring forward and a little
# up with a sharp front edge, and a lower lip below, both lined dark inside. On the visor's top, navy oval spots. In
# the dark of the mouth the pink creature: a round face, a pink snout poking out forward ending in a knob of two lobes
# (a bone's end), and an eye a side low beside the snout, white with a black upright pupil and a flat lid line across
# its top, set in a patch of green. A small pointed foot under the shell.
#
# Written to be grown from (accelgor.py takes its colours and its eye with sibling): the frame is in the constants
# and the functions that take the kit; the build is under the main guard. Bones: root, body, head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args
from mathutils import Euler

SHELL, RIDGE, LINING, SPOT = "#CAC4B6", "#B2AC9E", "#2A2630", "#34488E"
PINK, GREEN, WHITE, PUPIL, INK = "#F27C8E", "#7CC86E", "#F6F6F0", "#16141A", "#2A2228"


def rotm(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


# ---- the shell --------------------------------------------------------------------------------------------------
COIL_C, COIL_R = np.array([0.0, 0.035, 0.13]), np.array([0.084, 0.085, 0.1])
VISOR_C, VISOR_R, VISOR_ROT = np.array([0.0, -0.06, 0.162]), np.array([0.08, 0.1, 0.032]), (-12, 0, 0)
LIP_C, LIP_R, LIP_ROT = np.array([0.0, -0.05, 0.052]), np.array([0.072, 0.088, 0.026]), (10, 0, 0)
INNER_C, INNER_R = np.array([0.0, -0.035, 0.105]), np.array([0.068, 0.06, 0.055])


def flare(V):
    """Wider toward the front (-y of the unit sphere)."""
    V = V.copy()
    V[:, 0] *= 1 + 0.18 * np.clip(-V[:, 1], 0, 1)
    return V


def shell(k):
    k.add("coil", ell(COIL_C, COIL_R, sub=5), SHELL, "body")
    k.add("visor", ell(VISOR_C, VISOR_R, rot=VISOR_ROT, deform=flare), SHELL, "body")
    k.add("lip", ell(LIP_C, LIP_R, rot=LIP_ROT, deform=flare), SHELL, "body")
    k.add("inner", ell(INNER_C, INNER_R), LINING, "body")
    k.add("foot", ell((0.0, -0.005, 0.03), (0.022, 0.034, 0.022), rot=(-25, 0, 0)), SHELL, "body")
    # the linings: the visor's underside and the lip's top, in front of the coil
    for name, c, rot, under in (("visor", VISOR_C, VISOR_ROT, True), ("lip", LIP_C, LIP_ROT, False)):
        M = rotm(rot)
        def mask(P, c=c, M=M, under=under):
            L = (P - c) @ M
            side = -L[:, 2] if under else L[:, 2]
            return inside(-side + 0.009, 0.0015) * inside(P[:, 1] + 0.0, 0.004)
        k.paint(mask, LINING, on=[name])
    # a raised spiral on each side of the coil
    for s in (1, -1):
        t = np.linspace(0, 1, 40)
        th = 0.6 + t * 2.6 * math.pi
        rr = 0.058 * (1 - 0.8 * t)
        cy, cz = COIL_C[1] + 0.008, COIL_C[2] + 0.018
        y, z = cy + rr * np.cos(th), cz + rr * np.sin(th)
        q = 1 - ((y - COIL_C[1]) / COIL_R[1]) ** 2 - ((z - COIL_C[2]) / COIL_R[2]) ** 2
        x = s * (COIL_R[0] * np.sqrt(np.clip(q, 0.02, 1)) + 0.001)
        k.add(f"ridge{'R' if s > 0 else 'L'}", tube(np.stack([x, y, z], 1), np.full(40, 0.0062), seg=12, per=3),
              RIDGE, "body")


def spots(k):
    for x, y, half in ((0.026, -0.125, (0.0105, 0.018)), (0.044, -0.07, (0.012, 0.022))):
        k.decal((x, y, 0.21), unit(0, -0.15, 1), half, SPOT, up=(0, -1, 0), eps=0.003, rings=12, segs=44,
                mirror=True)


# ---- the creature in the mouth --------------------------------------------------------------------------------------
FACE_C, FACE_R = np.array([0.0, -0.068, 0.1]), np.array([0.056, 0.045, 0.04])
SNOUT = [(0.0, -0.1, 0.104), (0.0, -0.135, 0.108), (0.0, -0.162, 0.114)]
EYE_N = unit(0.95, -1, 0.0)


def creature(k):
    k.add("face", ell(FACE_C, FACE_R), PINK, "head", surface="skin")
    k.add("snout", tube(SNOUT, [0.018, 0.016, 0.015], seg=16, per=6), PINK, "head", surface="skin")
    k.add("knobup", ell((0.0, -0.172, 0.126), (0.016, 0.014, 0.015)), PINK, "head", surface="skin")
    k.add("knobdn", ell((0.0, -0.172, 0.103), (0.016, 0.014, 0.015)), PINK, "head", surface="skin")


def eye_centre():
    d = EYE_N
    return FACE_C + np.array([0, 0, -0.006]) + d / math.sqrt(((d / FACE_R) ** 2).sum())


def green_patch(k):
    k.decal(eye_centre(), EYE_N, (0.023, 0.021), GREEN, eps=0.0025, rings=12, segs=48, mirror=True)


def open_eye(u, v, lid=0.42):
    c = np.tile(srgb(INK), (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[r < 0.84] = srgb(WHITE)
    c[(u / 0.24) ** 2 + ((v + 0.05) / 0.5) ** 2 < 1] = srgb(PUPIL)
    c[v > lid + 0.14] = srgb(GREEN)
    c[np.abs(v - lid) < 0.14] = srgb(INK)
    return c


def eyes(k):
    def lay(state):
        k.decal(eye_centre(), EYE_N, (0.0175, 0.0175), eye_look(state, open_eye, srgb(GREEN), srgb(INK), line=0.17),
                eps=0.005, rings=20, segs=56, mirror=True)
    return lay


def shelmet(src, out):
    k = Kit(src, coat="shell")
    shell(k)
    creature(k)
    spots(k)
    green_patch(k)
    k.build(voxel=0.0038, smooth=3, band=0.005)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    shelmet(src, out)
