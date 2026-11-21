# Maushold (the family of four) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/maushold.py -- <the export's Maushold.glb> <out.glb>
#
# The design, from the references: Tandemaus grown into a family. Two grown mice a little larger than a Tandemaus
# stand in the middle holding paws, the left one in pale blue-grey shorts, the right one in a pale blue-grey vest; a
# little one stands at each side, holding a parent's outer paw, its head even larger for its size, plain white. Each
# keeps the line's great round head, big round ears, bead eyes and tiny nose (tandemaus.py's Mouse and mouse()).
# The family stands in one line across the front, the little ones a step forward and turned a little outward, so that
# none hides another and each face is clear of its neighbours from the game's front three-quarter camera.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, sibling, args

t = sibling("tandemaus")

BIG, SMALL, SMALL_HEAD = 1.15, 0.52, 1.15
PARENT_X, KID_X = 0.13, 0.285


def curl(m, side, lift=0.0):
    """A tail from a mouse's rump curling round behind it, out to its side (+1 its +X)."""
    pts = [(0.0, 0.038, 0.05), (0.03 * side, 0.08, 0.036), (0.07 * side, 0.1, 0.046), (0.085 * side, 0.09, 0.07 + lift),
           (0.07 * side, 0.078, 0.082 + lift), (0.056 * side, 0.088, 0.068 + lift)]
    return [m.world(p) for p in pts]


def maushold(src, out):
    k = Kit(src, coat="fur")
    lp = t.Mouse((-PARENT_X, -0.036, 0), BIG, 0, "a", "head")
    rp = t.Mouse((PARENT_X, 0.036, 0), BIG, 0, "b", "head2")
    lk = t.Mouse((-KID_X, -0.055, 0), SMALL, -12, "c", "head3", head_k=SMALL_HEAD, eye_k=1.25)
    rk = t.Mouse((KID_X, -0.035, 0), SMALL, 12, "d", "head4", head_k=SMALL_HEAD, eye_k=1.25)
    held = np.array([0.0, -0.05, 0.142])                 # the parents' paws, held between their heads
    hand_l = np.array([-0.212, -0.062, 0.076])            # a parent's outer paw, held by its little one
    hand_r = np.array([0.212, -0.048, 0.076])
    t.mouse(k, lp, "shorts", hands=(held + (-0.013, 0, 0), hand_l), tail=curl(lp, -1))
    t.mouse(k, rp, "vest", hands=(hand_r, held + (0.013, 0, 0)), tail=curl(rp, 1))
    t.mouse(k, lk, None, hands=(hand_l + (-0.01, 0, -0.004), None), tail=curl(lk, -1, 0.01))
    t.mouse(k, rk, None, hands=(None, hand_r + (0.01, 0, -0.004)), tail=curl(rk, 1, 0.01))
    k.build(voxel=0.0039, smooth=4, band=0.006, wband=0.03)
    t.board(k)
    k.eye_states(lambda st: t.lay_eyes(k, (lp, rp, lk, rk), st))
    k.export(out)


if __name__ == "__main__":
    maushold(*args())
