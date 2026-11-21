# Simipour sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Panpour's script (panpour.py, imported as sibling("panpour")): the same blue, cream and pale lining, the same white
# eyes with their lid line, the cream face painted round the head's axis, the nose and smile, the hands, feet and tail.
#   blender --background --factory-startup --python sculpts/simipour.py -- <the export's Simipour.glb> <out.glb>
#
# The design, from the references: a taller, upright blue-and-cream monkey; a great mane of thick blue locks parted
# over the crown, falling either side of a narrow cream face down to the shoulders, where each rolls up into a curl,
# the cream face rising to a point on the forehead between the parted locks; round blue ears with pale linings out from under the hair; a white
# scalloped bib at the throat; blue fur over the chest and belly like a tunic, ending at the hips in long jagged
# points; slim cream arms with long-fingered hands; long cream legs, big feet; a long cream tail curling round to the
# +X side, blue toward its end, ending in a big blue puff of five balls.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, tri, args, sibling

p = sibling("panpour")
BLUE, PALE, CREAM, WHITE = p.BLUE, p.PALE, p.CREAM, "#F6F8FA"

# The skeleton's joints: body (0, 0, 0.3), head (0, -0.01, 0.336), ears (+-0.091, -0.02, 0.448), arms
# (+-0.07, -0.01, 0.294), legs (+-0.056, 0, 0.14), tail (0, 0.056, 0.14)
SIMIPOUR = dict(p.PANPOUR)
SIMIPOUR.update(
    H=(0.0, -0.014, 0.44), HR=(0.104, 0.096, 0.106),
    jowl=((0.0, -0.045, 0.405), (0.07, 0.055, 0.055)),
    ear=((0.185, 0.01, 0.47), (0.056, 0.019, 0.062), 26), lining=0.7,
    eye_x=0.045, eye_z=0.08, eye_half=(0.032, 0.038), eye_tilt=0.08,
    face_top=([0, 0.1, 0.28, 0.55, 0.8, 1.0, 1.3, 1.65], [0.9, 0.72, 0.66, 0.64, 0.45, 0.0, -0.45, -0.7]),
    face_w=0.95,
    nose_z=-0.16, mouth_z=-0.4, mouth_half=(0.03, 0.016),
    shoulder=(0.062, -0.01, 0.294), elbow=(0.098, -0.066, 0.244), wrist=(0.11, -0.128, 0.196),
    arm_r=(0.036, 0.031, 0.028), hand_r=(0.04, 0.045, 0.018),
    hip=(0.056, 0.0, 0.16), knee=(0.072, -0.03, 0.092), ankle=(0.066, -0.008, 0.038),
    leg_r=(0.042, 0.03, 0.023), foot_r=(0.035, 0.052, 0.02),
    tail=[(0.0, 0.05, 0.15), (0.06, 0.13, 0.085), (0.15, 0.16, 0.065), (0.225, 0.145, 0.1), (0.255, 0.115, 0.17),
          (0.255, 0.095, 0.215)],
    tail_r=0.017, tail_blue=0.6, tail_balls=5, ball_r=0.03, ball_start=90,
)


def mane(k, Pr):
    """The locks: a crown parted in the middle, two front locks either side of the face down to curls at the
    shoulders, a mass behind the head to the shoulders with its own curls."""
    H = np.array(Pr["H"])
    k.add("crown", ell(H + np.array([0, 0.04, 0.055]), (0.112, 0.1, 0.1)), BLUE, "head")
    k.add("back", ell(H + np.array([0, 0.06, -0.04]), (0.12, 0.085, 0.13)), BLUE, "head")
    for s in (1, -1):
        b = "head"
        pts = [H + np.array([0.035 * s, -0.07, 0.11]), H + np.array([0.085 * s, -0.06, 0.07]),
               H + np.array([0.115 * s, -0.04, -0.01]), H + np.array([0.12 * s, -0.035, -0.09]),
               H + np.array([0.122 * s, -0.035, -0.13])]
        k.add(f"lock{'R' if s > 0 else 'L'}", tube(pts, [0.04, 0.046, 0.046, 0.042, 0.04]), BLUE, b)
        # The curl at its end: rolled up toward the face
        k.add(f"curl{'R' if s > 0 else 'L'}", ell(H + np.array([0.125 * s, -0.035, -0.15]), (0.048, 0.044, 0.046)),
              BLUE, b)
        # Outer locks behind the ears, down to curls of their own
        q = [H + np.array([0.08 * s, 0.04, 0.08]), H + np.array([0.125 * s, 0.045, 0.0]),
             H + np.array([0.135 * s, 0.04, -0.1])]
        k.add(f"outer{'R' if s > 0 else 'L'}", tube(q, [0.044, 0.048, 0.042]), BLUE, b)
        k.add(f"ocurl{'R' if s > 0 else 'L'}", ell(H + np.array([0.138 * s, 0.035, -0.125]), (0.046, 0.044, 0.044)),
              BLUE, b)
    # The part over the crown: a shallow groove of shade, painted
    def part(P):
        r = P - H
        return (inside(np.abs(r[:, 0]) - 0.006, 0.003) * (r[:, 2] > 0.08) * (r[:, 1] < 0.03) * (r[:, 1] > -0.1))
    k.paint(part, "#3A9CC0")
    k.paint(lambda P: np.ones(len(P)), BLUE, on=["lock", "curl", "outer", "ocurl", "crown", "back"])


def tunic(k, Pr):
    """Blue fur over chest and belly to long jagged points at the hips, the white scalloped bib at the throat."""
    def blue(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - 0.005))
        edge = 0.2 - 0.035 * (0.5 + 0.5 * tri(a * 7 / (2 * math.pi) + 0.5))
        return (inside(edge - P[:, 2], 0.0025) * inside(P[:, 2] - 0.36, 0.003)
                * (np.hypot(P[:, 0], P[:, 1]) < 0.09))
    k.paint(blue, BLUE)
    def bib(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - 0.0))
        edge = 0.296 - 0.012 * np.abs(np.cos(a * 5))
        return (inside(edge - P[:, 2], 0.0025) * inside(P[:, 2] - 0.375, 0.003) * (np.abs(a) < 1.2)
                * (np.hypot(P[:, 0], P[:, 1]) < 0.08))
    k.paint(bib, WHITE)


def simipour(src, out, Pr=SIMIPOUR):
    k = Kit(src, coat="fur")
    p.head(k, Pr)
    mane(k, Pr)
    p.ears(k, Pr)
    k.add("chest", ell((0, -0.002, 0.28), (0.07, 0.056, 0.065)), CREAM, "body")
    k.add("waist", ell((0, 0.004, 0.215), (0.06, 0.05, 0.06)), CREAM, "body")
    k.add("hips", ell((0, 0.008, 0.165), (0.066, 0.054, 0.045)), CREAM, "body")
    k.add("neck", ell((0, -0.008, 0.33), (0.045, 0.042, 0.04)), CREAM, "body")
    tunic(k, Pr)
    p.arms(k, Pr, fingers=4, open_hand=True)
    p.legs(k, Pr)
    p.tail(k, Pr)
    p.nose_and_mouth(k, Pr)
    k.build(voxel=0.005, smooth=3, band=0.006)
    k.eye_states(lambda s: p.lay_eyes(k, Pr, s))
    k.export(out)


if __name__ == "__main__":
    simipour(*args())
