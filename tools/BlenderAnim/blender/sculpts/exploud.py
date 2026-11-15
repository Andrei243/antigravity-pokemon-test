# Exploud sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Loudred's script (loudred.py, through sibling): the same periwinkle blue and cream-yellow, the same way of carving
# a great mouth and lining it, of laying the eyes, of white pegs for teeth and claws.
#   blender --background --factory-startup --python sculpts/exploud.py -- <the export's Exploud.glb> <out.glb>
#
# The design, from the references: a big hunched periwinkle beast, its head carried low and forward on the chest; a
# blunt snout over a gaping mouth that runs from under the snout down to the chest, dark red inside with lines, a big
# pink tongue on its floor and a dark throat; two square white front teeth under the snout's middle and two white pegs
# standing on the lower jaw; small angry pink eyes under a heavy brow; tubes like organ pipes grow from it, blue with
# cream-yellow ends: a crown of four on the head, rising up and back; one on each side of the head with a yellow ring
# round an open bore, one more at each corner of the jaw; two great barrels behind the shoulders; the thick tail rising
# behind ends in a yellow barrel. Thick arms with round fists carried forward and down, grooves across the forearms;
# thick legs fused into the body, a dark hole on each thigh, big feet with three white claws each.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, outward, srgb, inside, unit, boxy, eye_look, polygon, args, sibling

L = sibling("loudred")
W = L.W

BLUE, CREAM, WHITE, INK, MOUTH, THROAT, TONGUE = L.BLUE, L.CREAM, L.WHITE, L.INK, L.MOUTH, L.THROAT, L.TONGUE
EYE = "#E87C9A"
BORE = "#24202C"

# --- the frame (the skeleton's joints: body (0, 0, 0.42), head (0, -0.06, 0.56), arms (+-0.13, -0.02, 0.48),
#     legs (+-0.1, 0.03, 0.24), tail (0, 0.13, 0.3)) ------------------------------------------------------------------
B = np.array([0.0, 0.07, 0.43])            # the trunk's middle
BR = np.array([0.22, 0.2, 0.25])
SK = np.array([0.0, -0.1, 0.68])           # the skull's middle
SKR = np.array([0.165, 0.165, 0.12])
SN = np.array([0.0, -0.215, 0.645])        # the snout over the mouth
SNR = np.array([0.115, 0.085, 0.072])
JW = np.array([0.0, -0.13, 0.37])          # the lower jaw and chest under the mouth
JWR = np.array([0.175, 0.15, 0.16])
MC = np.array([0.0, -0.26, 0.47])           # the mouth's cutter: middle and half sizes
MR = np.array([0.122, 0.15, 0.142])
EYE_AT = np.array([0.098, -0.2, 0.7])


def barrel(c, d, radius, length, bore=None):
    """A yellow end of a pipe: a short solid of turning along d from c, its outer end open in a dark bore when
    `bore` is a depth."""
    if bore:
        prof = [(0, 0), (radius, 0), (radius, length), (radius * 0.55, length), (radius * 0.55, length - bore),
                (0, length - bore)]
    else:
        prof = [(0, 0), (radius, 0), (radius, length * 0.82), (radius * 0.82, length), (0, length)]
    return lathe(c, d, prof, seg=36)


def pipe(k, name, root, d, length, radius, end, bone, bore=None, mirror=True):
    """A pipe: a blue tube fused out of the body along d, its last `end` a yellow barrel (loose, crisp), and in an
    open pipe a dark disc at the bottom of the bore."""
    root = np.array(root, float); d = unit(*d)
    tip = root + d * (length - end)
    k.add(name, tube([root, root + d * (length - end) * 0.5, tip - d * radius * 0.85], [radius, radius * 0.97, radius * 0.94]),
          BLUE, bone, mirror=mirror)
    k.add(name + "end", barrel(tip - d * 0.006, d, radius * 1.08, end + 0.006, bore), CREAM, bone, fuse=False,
          mirror=mirror, surface="plastic")
    if bore:
        floor = tip + d * (end - bore + 0.0005)
        k.add(name + "bore", lathe(floor, d, [(0, 0), (radius * 0.6, 0), (radius * 0.6, 0.002), (0, 0.002)], seg=24),
              BORE, bone, fuse=False, mirror=mirror, surface="plastic")


def add_mouth(k):
    """The gaping mouth: carved from under the snout down to the chest, lined dark red, the throat darker at its back,
    a big pink tongue on its floor; two square front teeth under the snout, two pegs on the lower jaw."""
    k.add("mouth", ell(MC, MR, deform=boxy(3.0)), MOUTH, "head", carve=True)

    def lining(P):
        q = (P - MC) / (MR * np.array([1.06, 1.0, 1.08]))
        return inside(np.abs(q[:, 0]) - 1, 0.003) * inside(np.abs(q[:, 2]) - 1, 0.003) \
            * (1 - inside(P[:, 1] + 0.215, 0.003)) * inside(P[:, 1] + 0.06, 0.003)
    k.paint(lining, MOUTH)
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.06, (P[:, 2] - MC[2] - 0.02) / 0.05) - 1, 0.004)
            * (1 - inside(P[:, 1] + 0.16, 0.004)) * inside(P[:, 1] + 0.06, 0.003), THROAT)
    k.add("tongue", ell((0, -0.2, MC[2] - MR[2] + 0.045), (0.1, 0.09, 0.045)), TONGUE, "head", fuse=False, surface="skin")
    for x in (-0.022, 0.022):
        k.add(f"incisor{x > 0}", ell((x, -0.268, MC[2] + MR[2] - 0.012), (0.019, 0.012, 0.026), deform=boxy(3)),
              WHITE, "head", fuse=False, surface="shell")
    k.add("peg", ell((0.075, -0.275, MC[2] - MR[2] + 0.012), (0.017, 0.013, 0.026), deform=boxy(2.6)), WHITE,
          "body", fuse=False, mirror=True, surface="shell")


# --- eyes: small, pink with a dark pupil, angry: the top edge slanting down to the nose under a brow ---------------
CB, CE, CI = srgb(BLUE), srgb(EYE), srgb(INK)
EYE_OUTLINE = polygon([(1.0, 0.45), (0.3, 0.2), (-0.4, -0.15), (-1.0, -0.35), (-0.7, -0.8), (0.1, -0.9),
                       (0.8, -0.5)])


def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[np.hypot((u + 0.05) / 0.3, (v + 0.3) / 0.42) < 1] = CI
    return c


def lay_eyes(k, state):
    n = unit(0.55, -0.78, 0.25)
    look = eye_look(state, eye, CB, CI, line=0.2)
    if state == "Open":
        k.decal(EYE_AT, n, (0.042, 0.036), look, eps=0.004, rings=20, segs=56, mirror=True, outline=EYE_OUTLINE)
    else:
        k.decal(EYE_AT + (0, 0, -0.006), n, (0.042, 0.026), look, eps=0.004, rings=20, segs=56, mirror=True)


def exploud(src, out):
    k = Kit(src, coat="fur")
    # The trunk, the skull and snout, the jaw and chest: one mass, the head low and forward
    k.add("body", ell(B, BR, deform=L.pear), BLUE, "body")
    k.add("hump", ell((0, 0.06, 0.6), (0.19, 0.17, 0.13)), BLUE, "body")
    k.add("skull", ell(SK, SKR, deform=boxy(2.3)), BLUE, "head")
    k.add("snout", ell(SN, SNR, deform=boxy(2.4)), BLUE, "head")
    k.add("jaw", ell(JW, JWR, deform=boxy(2.3)), BLUE, "body")
    k.add("brow", ell((0.09, -0.205, 0.735), (0.05, 0.03, 0.022), rot=(0, -18, 0)), BLUE, "head", mirror=True)
    k.add("nostril", ell((0.035, -0.295, 0.67), (0.014, 0.012, 0.011)), BLUE, "head", mirror=True)
    add_mouth(k)
    # The crown of four pipes, rising up and back from the skull
    pipe(k, "crownin", (0.04, -0.08, 0.76), (0.18, 0.45, 1.0), 0.2, 0.07, 0.04, "head")
    pipe(k, "crownout", (0.11, -0.06, 0.73), (0.65, 0.45, 0.75), 0.17, 0.06, 0.037, "head")
    # Open pipes on the sides of the head and at the jaw's corners
    pipe(k, "cheek", (0.13, -0.08, 0.66), (1.0, 0.05, 0.3), 0.13, 0.05, 0.038, "head", bore=0.03)
    pipe(k, "jawpipe", (0.14, -0.15, 0.44), (1.0, -0.2, 0.05), 0.11, 0.05, 0.036, "body", bore=0.03)
    # Two great barrels behind the shoulders
    pipe(k, "back", (0.11, 0.12, 0.6), (0.55, 0.45, 0.7), 0.18, 0.085, 0.052, "body")
    # The thick tail, rising behind and out to the +X side, ending in a yellow barrel
    TP = [(0.0, 0.2, 0.3), (0.02, 0.32, 0.32), (0.04, 0.41, 0.42), (0.055, 0.45, 0.54)]
    k.add("tail", tube(TP[:-1] + [np.array(TP[-1]) - unit(*(np.array(TP[-1]) - np.array(TP[-2]))) * 0.065], [0.1, 0.09, 0.08, 0.075]), BLUE, "tail")
    td = unit(*(np.array(TP[-1]) - np.array(TP[-2])))
    k.add("tailend", barrel(np.array(TP[-1]) - td * 0.01, td, 0.082, 0.12), CREAM, "tail", fuse=False,
          surface="plastic")
    # Arms: thick, from the shoulders forward and down, round fists, grooves across the forearm
    k.add("arm", tube([(0.17, -0.02, 0.55), (0.25, -0.08, 0.47), (0.28, -0.17, 0.36)], [0.085, 0.075, 0.068]), BLUE,
          "arm{s}", mirror=True)
    k.add("fist", ell((0.285, -0.2, 0.315), (0.075, 0.07, 0.07), deform=boxy(2.4)), BLUE, "arm{s}", mirror=True)
    a, b = np.array((0.25, -0.08, 0.47)), np.array((0.28, -0.17, 0.36))
    for t in (0.3, 0.5, 0.7):
        p = a + (b - a) * t

        def groove(P, p=p):
            """A short dark line across the forearm's outer side, square to its length."""
            x = np.abs(P[:, 0])
            across = (P[:, 2] - p[2]) + 0.8 * (P[:, 1] - p[1])
            return inside(np.abs(across) - 0.004, 0.0015) * inside(np.hypot(P[:, 1] - p[1], P[:, 2] - p[2]) - 0.04,
                                                                    0.003) * (x > p[0] + 0.03)
        k.paint(groove, INK, on=["arm"])
    # Legs: fat thighs fused into the body, a dark hole on each, short shins, big feet with three white claws
    k.add("thigh", ell((0.15, 0.04, 0.2), (0.12, 0.13, 0.13)), BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.17, -0.06, 0.04), (0.095, 0.13, 0.045), rot=(0, 0, -12)), BLUE, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.05, 0.0, 0.05)):
        cx, y = 0.18 + dx, -0.18 + abs(dx) * 0.4
        k.add(f"claw{i}", ell((cx, y, 0.022), (0.019, 0.026, 0.022), deform=boxy(2.6)), WHITE, "leg{s}", fuse=False,
              mirror=True, surface="shell")
    k.decal((0.265, 0.0, 0.2), unit(1, -0.25, 0.1), (0.028, 0.022), BORE, eps=0.004, rings=8, segs=40, mirror=True)
    k.build(voxel=0.008, band=0.005)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    exploud(*args())
