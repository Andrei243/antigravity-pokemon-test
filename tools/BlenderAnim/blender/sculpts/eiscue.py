# Eiscue (its Ice Face) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/eiscue.py -- <the export's Eiscue.glb> <out.glb>
#
# The design, from the references: a small penguin whose head is a great cube of pale ice, the largest mass by far (some
# three fifths of the height and nearly three times the body's width), its edges rounded off. On the cube's front the
# face: two upright oval eyes drawn as dark rims round a deeper ice blue, and between and below them a pale blue beak of
# ice, a flat-faced pyramid. From the middle of the cube's top one black hair rises, kinked once. The body under it is
# a little egg, black behind and white in front, pale lilac at the top of the chest under the cube, a jagged cyan mark at
# the +X edge of the white; black flippers hang down and a little out and forward; short yellow legs and flat yellow feet.
# The pictures agree with the brief.
# Skeleton: root, body (0, 0, 0.18), head (0, -0.01, 0.3), armL/armR (+-0.06, 0, 0.24), legL/legR (+-0.035, -0.01, 0.06).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, boxy, eye_look, args, hull, outward, polygon, inside, smoothstep

# --- colours ----------------------------------------------------------------------------------------------------------
BLACK, WHITE, LILAC, CYAN, YELLOW = "#2A2E40", "#F4F6FA", "#D4CDE6", "#45B6E2", "#F2B33A"
ICE, ICE_DEEP, RIM, BEAK = "#BEDFEF", "#9CC8DE", "#4E7189", "#A3D2EC"

# --- the frame --------------------------------------------------------------------------------------------------------
B_C, B_R = np.array([0.0, 0.006, 0.15]), np.array([0.074, 0.068, 0.125])
C_C, C_R = np.array([0.0, 0.0, 0.47]), np.array([0.212, 0.196, 0.2])
FRONT = C_C[1] - C_R[1]                   # the cube's front face
EYE_X, EYE_Z, EYE_HALF = 0.078, 0.492, (0.036, 0.049)


def pear(V):
    V = V.copy()
    V[:, :2] *= (1 + 0.14 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1))[:, None]
    return V


def body(k):
    k.add("body", ell(B_C, B_R, deform=pear), BLACK, "body")
    def front(P):
        d = ((P[:, 0] / 0.056) ** 2 + ((P[:, 2] - 0.135) / 0.13) ** 2) - 1
        return inside(d * 0.03, 0.0015) * smoothstep(0.0, 0.02, -(P[:, 1] - B_C[1]) - 0.02)
    k.paint(front, WHITE, on=["body"])
    def chest(P):
        return front(P) * smoothstep(0.205, 0.225, P[:, 2])
    k.paint(chest, LILAC, on=["body"])
    # the jagged cyan mark at the +X edge of the white
    k.decal((0.05, -0.05, 0.19), unit(0.72, -0.69, 0.0), (0.018, 0.04), CYAN, eps=0.0018, rings=8, segs=36,
            outline=polygon([(-0.7, 1.0), (0.6, 0.55), (-0.1, 0.25), (0.8, -0.05), (0.0, -0.35), (0.6, -0.7),
                             (-0.7, -1.0), (-0.9, 0.0)]))

    # flippers: hanging down, a little out and forward
    k.add("flipper", ell((0.088, -0.008, 0.17), (0.017, 0.036, 0.075), rot=(-12, -24, 8)), BLACK, "arm{s}",
          mirror=True)
    # short yellow legs and flat feet with three toes
    def toes(V):
        ph = np.arctan2(V[:, 0], -V[:, 1])
        f = np.clip(-V[:, 1], 0, 1)
        s = 1 + 0.18 * np.cos(3 * ph) * f
        V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("leg", outward(tube([(0.034, -0.008, 0.05), (0.037, -0.018, 0.016)], [0.012, 0.011])), YELLOW, "leg{s}",
          fuse=False, mirror=True, surface="shell")
    k.add("foot", ell((0.04, -0.04, 0.011), (0.03, 0.044, 0.011), rot=(0, 0, -12), deform=toes), YELLOW, "leg{s}",
          fuse=False, mirror=True, surface="shell")


def cube(k):
    k.add("cube", ell(C_C, C_R, deform=boxy(9.0), sub=5), ICE, "head", surface="shell")
    # the beak: a flat-faced pyramid of ice
    z = 0.418
    k.add("beak", hull([(0.052, FRONT + 0.006, z), (-0.052, FRONT + 0.006, z), (0, FRONT + 0.006, z + 0.036),
                        (0, FRONT + 0.006, z - 0.032), (0, FRONT - 0.046, z + 0.002)]), BEAK, "head", fuse=False,
          surface="shell")
    # the one black hair from the top, kinked once
    top = C_C[2] + C_R[2]
    k.add("hair", outward(tube([(0, 0.0, top - 0.01), (0.004, 0.0, top + 0.035), (0.03, 0.0, top + 0.046),
                                (0.044, 0.0, top + 0.085)], [0.0085, 0.0085, 0.008, 0.007], seg=10)),
          BLACK, "head", fuse=False)


def eye_open():
    rim, deep = srgb(RIM), srgb(ICE_DEEP)
    def look(u, v):
        c = np.tile(rim, (len(u), 1))
        c[u * u + v * v < 0.72 ** 2] = deep
        return c
    return look


def eyes(k, state):
    look = eye_look(state, eye_open(), srgb(ICE), srgb(RIM), line=0.17)
    k.decal((EYE_X, FRONT, EYE_Z), (0, -1, 0), EYE_HALF, look, eps=0.0025, rings=20, segs=56, mirror=True)


def eiscue(src, dest):
    k = Kit(src, coat="fur")
    body(k)
    cube(k)
    k.build(voxel=0.0055, band=0.004, wband=0.04)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dest)


if __name__ == "__main__":
    eiscue(*args())
