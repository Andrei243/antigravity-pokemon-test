# Weepinbell sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown
# from bellsprout.py (its colours, lips, mouth's cutter, leaves and eyes):
#   blender --background --factory-startup --python sculpts/weepinbell.py -- <the export's Weepinbell.glb> <out.glb>
#
# From the references: a floating yellow bell lying on its side, its narrow top up and back with a little knob from
# which a brown stalk rises and hooks back and down, its broad end forward and down opening into a great round mouth
# ringed by thick pink lips, dark inside; two big round white eyes with black pupils on the front over the mouth,
# set apart; a few olive spots over the top behind the eyes; and a broad pointed light green leaf out of each side,
# rising up and out like ears, their tips curling back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, unit, eye_look, args, polygon, sibling

bs = sibling("bellsprout")
BELL, STEM, LEAF, LIP, INK = bs.BELL, bs.STEM, bs.LEAF, bs.LIP, bs.INK
MOUTH = "#3C1828"                  # the mouth's inside, a dark plum
SPOT = "#9EA44A"                   # the olive spots

# --- Weepinbell's frame --------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.27])                 # the bell's middle
A = unit(0, -1, -0.68)                         # its axis, from the top back down to the mouth in front
# the bell's profile along A from C: (radius, distance), from the top round to the mouth
PROFILE = [(0, -0.205), (0.035, -0.2), (0.062, -0.18), (0.095, -0.135), (0.125, -0.07), (0.142, 0.0),
           (0.143, 0.06), (0.134, 0.11), (0.12, 0.145), (0.1, 0.168), (0, 0.18)]
MOUTH_AT = 0.165                               # where along A the lips sit
TOP = C - A * 0.2


def weepinbell(src, out):
    k = Kit(src, coat="leaf")

    k.add("bell", lathe(C, A, PROFILE, seg=48), BELL, "body")
    k.add("knob", ell(TOP + (0, 0.004, 0.006), (0.038, 0.038, 0.034)), BELL, "body")

    # the mouth: an oval hollow into the broad end, ringed by thick pink lips
    mc = C + A * MOUTH_AT
    k.add("mouth", bs.bore(mc, A, 0.066, 0.12, 0.12, sy=1.15), MOUTH, "body", carve=True)
    k.add("lips", bs.lips(mc, A, 0.094, 0.032, sy=1.12), LIP, "body", fuse=False, surface="skin")
    bs.lining(k, mc, A, 0.08, -0.11, MOUTH, sy=1.15)

    # the stalk: out of the knob, up and hooked back and down
    k.add("stalk", tube([TOP + (0, -0.004, 0.0), TOP + (0, 0.004, 0.06), TOP + (0, 0.04, 0.115),
                         TOP + (0.012, 0.095, 0.125), TOP + (0.03, 0.125, 0.09)], [0.017, 0.016, 0.014, 0.012, 0.009]),
          STEM, "body", fuse=False)

    # the spots: olive patches over the top behind the eyes
    rs = np.random.RandomState(7)
    for j, (x, y, z) in enumerate([(0.0, 0.07, 0.41), (0.08, 0.06, 0.4), (-0.085, 0.07, 0.39),
                                       (0.03, 0.12, 0.41), (-0.04, -0.02, 0.43), (0.12, -0.02, 0.36),
                                       (-0.12, 0.0, 0.35), (0.0, 0.16, 0.36)]):
        p = np.array([x, y, z])
        n = unit(*(p - (C + (0, 0.02, 0.0))))
        shape = [(math.cos(t) * (0.8 + 0.3 * rs.rand()), math.sin(t) * (0.7 + 0.3 * rs.rand()))
                 for t in np.linspace(0, 2 * math.pi, 7, endpoint=False)]
        k.decal(p, n, (0.02, 0.02), SPOT, eps=0.004, outline=polygon(shape), rings=8, segs=36)

    # the leaves: broad, out of the sides, rising up and out, their tips curling back
    k.add("stalklet", tube([(0.09, 0.03, 0.31), (0.125, 0.032, 0.322)], [0.016, 0.014]), LEAF, "wing{s}",
          mirror=True)
    k.add("leaf", bs.leaf_sheet((0.115, 0.032, 0.32), (0.62, 0.18, 1.0), (0, -1, 0.3), 0.26, 0.1,
                                droop=0.0, fold=0.22, wide=0.42,
                                tipcurl=lambda v: (-0.18 * v ** 3, -0.06 * v ** 3)),
          LEAF, "wing{s}", fuse=False, mirror=True, surface="leaf")

    k.build(voxel=0.005, smooth=4, band=0.006)

    lid = srgb(BELL)
    look = bs.round_eye(pupil=0.5, rim=0.86)
    EYE, EYEN = (0.074, -0.13, 0.335), (0.38, -0.9, 0.32)
    def eyes(state):
        k.decal(EYE, EYEN, (0.034, 0.036), eye_look(state, look, lid, INK, line=0.15), eps=0.005, rings=20, segs=56,
                mirror=True)
        if state == "Open":           # the pupil a patch of its own, for a round edge
            k.decal(EYE, EYEN, (0.017, 0.018), INK, eps=0.008, rings=6, segs=32, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    weepinbell(*args())
