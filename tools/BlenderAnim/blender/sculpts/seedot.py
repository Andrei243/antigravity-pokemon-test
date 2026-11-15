# Seedot sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/seedot.py -- <the export's Seedot.glb> <out.glb>
#
# The design, from the references: an acorn, round above and coming to a little point below; its lower half brown,
# its upper half (the face) a pale yellow-tan, the brown rising in a point between the eyes. A grey cap like a bell
# sits down over the top, its dome ringed by three grooves, its brim flared and rolled at the edge, and a short grey
# stalk, wider at its top, stands out of it. Under the brim two large eyes peer out, their tops cut by it: each a thick
# black rim round pale yellow, a black ring inside that, and pale yellow in the middle. Two round pale feet stick out in
# front at the bottom.
# The skeleton: body (0, 0, 0.2), head (0, 0, 0.26) (the cap and stalk), legs (+-0.06, 0, 0.06).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, smoothstep, unit, eye_look, lathe, ring_band, hull, args

BROWN, BROWN_D, TAN, CAP, GROOVE, FOOT, INK = "#8C6646", "#73523A", "#E2CF92", "#A3A4A8", "#6E6F75", "#F0DCA0", "#1C1614"

BODY_C = np.array([0.0, 0.0, 0.178])     # the acorn's middle, on the body bone
BODY_R = (0.142, 0.134, 0.15)            # its radii
CAP_Z = 0.276                            # the cap's base plane (the brim's middle)
FACE_Z = 0.172                          # where the tan of the face meets the brown below


def acorn(V):
    """The lower half drawn in to a blunt point."""
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 - 0.2 * low ** 2.2)[:, None]
    V[:, 2] -= 0.06 * low ** 4
    return V


def body(k):
    k.add("body", ell(BODY_C, BODY_R, deform=acorn, sub=5), BROWN, "body")
    # the little point at the bottom
    k.add("tip", ell((0, 0.0, 0.035), (0.03, 0.03, 0.03)), BROWN_D, "body")
    # the face: tan above, the brown rising to a point between the eyes in front
    def face(P):
        front = smoothstep(-0.02, -0.08, P[:, 1])
        line = FACE_Z + 0.03 * front * np.exp(-(P[:, 0] / 0.02) ** 2)
        return inside(line - P[:, 2], 0.004)
    k.paint(face, TAN)


def cap(k):
    """The bell of a cap: a dome with three grooves, a flared brim rolled at its edge, the stalk standing out of it."""
    # a round dome (a squashed half ball) down to the brim, which flares out and rolls under
    DR, DH = 0.138, 0.118
    prof = [(DR * math.sin(t), 0.012 + DH * math.cos(t)) for t in np.linspace(0, math.radians(84), 12)]
    prof[0] = (0.0, prof[0][1])
    prof += [(0.142, 0.006), (0.152, -0.001), (0.157, -0.009), (0.153, -0.016), (0.14, -0.016), (0.12, -0.006),
             (0.0, 0.02)]
    c = np.array([0, 0.004, CAP_Z])
    k.add("cap", lathe(c, (0, 0, 1), prof, seg=72), CAP, "head", fuse=False, surface="shell")
    # the grooves: dark bands round the dome, at three heights (each at the dome's radius there)
    for i, h in enumerate((0.04, 0.075, 0.103)):
        r = DR * math.sqrt(max(0.0, 1 - ((h - 0.012) / DH) ** 2))
        k.add(f"groove{i}", ring_band(c + (0, 0, h), (0, 0, 1), r - 0.0005, 0.006, thick=0.004), GROOVE, "head",
              fuse=False, surface="shell")
    # the stalk, wider at its rounded top
    st = [(0.0, 0.12), (0.022, 0.12), (0.021, 0.15), (0.024, 0.176), (0.032, 0.188), (0.033, 0.196), (0.028, 0.201),
          (0.0, 0.203)]
    k.add("stalk", lathe(c, (0, 0, 1), st, seg=40), CAP, "head", fuse=False, surface="shell")


def feet(k):
    k.add("foot", ell((0.062, -0.07, 0.038), (0.046, 0.052, 0.038), rot=(0, 0, 18)), FOOT, "leg{s}", fuse=False,
          mirror=True, surface="shell")


# The eyes: a thick black rim, pale yellow, a black ring, pale yellow in the middle (u outward, v up)
EYE_HALF = (0.06, 0.064)
EYE_X, EYE_Z = 0.066, 0.238
CT, CI = srgb(TAN), srgb(INK)


def eye_open(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CT, (len(u), 1))
    c[r > 0.79] = CI
    c[(r > 0.33) & (r < 0.58)] = CI
    return c


def eyes(k, state):
    p = BODY_C + np.array([EYE_X, -0.12, EYE_Z - BODY_C[2]])
    n = unit(0.5, -1, 0.12)
    k.decal(p, n, EYE_HALF, eye_look(state, eye_open, CT, CI, line=0.18), eps=0.004, rings=20, segs=56, mirror=True)


def seedot(src, out):
    k = Kit(src, coat="shell")
    body(k)
    cap(k)
    feet(k)
    k.build(voxel=0.005, smooth=4)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    seedot(*args())
