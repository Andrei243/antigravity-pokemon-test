# Mega Mewtwo Y sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mewtwomegay.py -- <the export's Mewtwo-Mega-Y.glb> <out.glb>
#
# Grown from mewtwomegax.py's frame (mewtwo.py's, copied there as the class Mewtwo): the same head drawn in its own
# measure, the same almond eyes and mouth, thighs and knobbed feet, with Mega Y's numbers.
#
# What Mega Y changes, from the pictures, largest mass first:
#   1. one long purple mass from the back of the skull: thick and pale at its root, sweeping back over the model's
#      left and down behind the back, then running on, thin, as the tail round to the model's right in front, where
#      it ends in a bulb on the ground. The pictures show it as ONE curve, so the sculptor's loop and separate tail
#      are read as one piece here: its upper run on `head`, the run down the back on `body`, the thin run on `tail`;
#   2. a larger head on a slighter body (the head grown 1.38 against the base's 1.2), two pointed upright ears;
#   3. slender all over: a narrow chest, thin arms, NO purple belly;
#   4. three fingers and three toes tipped with long purple beans, a purple heel;
#   5. a thin loop arching back over the crown into the long mass;
#   6. red eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, args, sibling

mx = sibling("mewtwomegax")
PURPLE_Y = "#A07098"


def mega_y(src, dst):
    k = Kit(src, coat="fur")
    m = mx.Mewtwo(k, grow=1.38, forward=-0.03, purple=PURPLE_Y, iris="#D8303A")
    S, P = m.skin, m.purple

    # The trunk: slight, a narrow chest, a pale belly
    k.add("chest", ell((0, 0.012, 0.735), (0.1, 0.064, 0.085), deform=mx.wider_top), S, "body")
    k.add("pec", ell((0.04, -0.016, 0.74), (0.056, 0.04, 0.046)), S, "body", mirror=True)
    k.add("waist", tube([(0, 0.006, 0.56), (0, 0.01, 0.63), (0, 0.01, 0.68)], [0.064, 0.05, 0.066]), S, "body")
    k.add("belly", ell((0, -0.006, 0.52), (0.076, 0.08, 0.086)), S, "body")
    k.add("neck", tube([(0, 0.008, 0.79), (0, -0.008, 0.88)], [0.042, 0.038]), S, "body")
    k.add("shoulder", ell((0.11, 0.002, 0.785), (0.038, 0.038, 0.036)), S, "arm{s}", mirror=True)

    # The legs: the base's teardrop thighs, slimmer; purple toes and heel
    m.legs(thigh=(0.066, 0.074, 0.138), shin=(0.044, 0.032, 0.025),
           toes=((0.084, -0.128), (0.124, -0.142), (0.164, -0.124)), toe_r=0.023, toe_col=P)

    # The arms: thin, bent, the hands raised before the chest, three fingers tipped with long purple beans
    ELBOW, HAND = np.array([0.17, -0.04, 0.685]), np.array([0.125, -0.155, 0.71])
    k.add("upper", tube([(0.115, 0.0, 0.785), (0.15, -0.016, 0.735), ELBOW], [0.027, 0.024, 0.022]), S, "arm{s}",
          mirror=True)
    k.add("fore", tube([ELBOW, (0.15, -0.1, 0.69), HAND], [0.022, 0.021, 0.022]), S, "arm{s}", mirror=True)
    k.add("palm", ell(HAND + (0, -0.008, 0.005), (0.026, 0.022, 0.026)), S, "arm{s}", mirror=True)
    for i, d in enumerate(((-0.026, -0.03, 0.04), (0.0, -0.04, 0.05), (0.026, -0.03, 0.038))):
        tip = HAND + np.array(d)
        k.add(f"finger{i}", tube([HAND + (0, -0.01, 0.01), tip], [0.012, 0.011]), S, "arm{s}", mirror=True)
        dn = unit(*d)
        k.add(f"fingertip{i}", ell(tip + dn * 0.012, (0.018, 0.018, 0.028),
                                   rot=(math.degrees(math.atan2(-d[1], d[2])), 0, 0)), P, "arm{s}", mirror=True)

    # The head: mewtwo.py's skull grown, two pointed upright ears in place of the short horns
    m.head(horns=False)
    k.add("ear", blade([m.hd(p) for p in [(0.046, 0.012, 0.985), (0.058, 0.02, 1.03), (0.068, 0.026, 1.075)]],
                       [0.03 * 1.38, 0.021 * 1.38, 0.006], (0.3, 1, 0), 0.55), S, "head", mirror=True)
    # The thin loop over the crown, running back into the long mass
    k.add("loop", tube([m.hd(p) for p in [(0, -0.03, 1.005), (0, 0.0, 1.05), (0, 0.05, 1.065), (0, 0.1, 1.04),
                                          (0, 0.12, 0.99)]], [0.015, 0.016, 0.016, 0.018, 0.024]), S, "head")

    # The long mass: thick and pale at its root, purple from the shoulders on, down the back, then the thin tail
    # It sweeps out behind over the model's RIGHT (+X), where the game's front three-quarter camera sees it beside the
    # body (over the left it stood straight behind the body there), and the tail comes round to the left in front.
    def sw(pts):
        return [np.array(p, float) * (-1, 1, 1) for p in pts]
    root = m.hd((0, 0.07, 0.955))
    k.add("ponyhead", tube([root] + sw([(-0.06, 0.2, 0.965), (-0.15, 0.29, 0.86), (-0.19, 0.3, 0.72)]),
                           [0.05, 0.076, 0.088, 0.084]), P, "head")
    k.add("ponyback", tube(sw([(-0.15, 0.29, 0.86), (-0.19, 0.3, 0.72), (-0.14, 0.26, 0.6), (-0.04, 0.17, 0.51)]),
                           [0.088, 0.084, 0.064, 0.044]), P, "body")
    k.add("tail", tube(sw([(-0.04, 0.17, 0.51), (0.0, 0.2, 0.36), (0.08, 0.19, 0.2), (0.18, 0.1, 0.075),
                           (0.26, -0.02, 0.04), (0.3, -0.1, 0.04)]), [0.038, 0.032, 0.026, 0.022, 0.02, 0.02]), P,
          "tail")
    k.add("bulb", ell((-0.31, -0.14, 0.046), (0.04, 0.056, 0.04), rot=(0, 0, -20)), P, "tail")
    k.paint(lambda Q: smoothstep(0.84, 0.95, Q[:, 2]), S, on=["pony"])

    m.mouth()
    k.build(voxel=0.0062, band=0.004)
    k.eye_states(m.eyes)
    k.export(dst)


if __name__ == "__main__":
    mega_y(*args())
