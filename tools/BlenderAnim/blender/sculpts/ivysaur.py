# Ivysaur sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ivysaur.py -- <the export's Ivysaur.glb> <out.glb>
#
# The design, from the references: Bulbasaur grown, bluer and on longer legs, its patches a darker blue; the head
# smaller against the body, the ears pointed and dark inside; the eyes narrowed under a top line falling toward the
# nose; the mouth shut, a white fang hanging at each corner; the bulb opened into a pink bud wrapped tight, its petals'
# points parted at the top, with four great fern leaves spread round it, serrated, a dark midrib down each.
#
# Grown from bulbasaur.py (sibling): the same body, legs, claws, head, ears, patches, mouth and eyes, with Ivysaur's
# sizes; the fern is this script's, and venusaur.py borrows it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, args, outward, sheet, sibling

b = sibling("bulbasaur")

TEAL, SPOT, INK, CLAW = "#68BCD0", "#2E7FA0", "#1C2830", "#F2F0EA"
LINING = "#26343C"
RED, PUPIL, WHITE = b.RED, b.PUPIL, b.WHITE
BUD, BUD2 = "#EE6A88", "#DC5676"
LEAF, RIB = "#4C9E44", "#2D6A2C"


def fern(k, name, base, d, face, length, width, bone, col=LEAF, rib=RIB, droop=0.3, teeth=9, cut=0.4, mirror=True):
    """
    A fern leaf from base along d, its face turned toward face, drooping toward its tip; its edges cut into teeth
    pointing to the tip, a short stalk, widest a little past its middle; a dark midrib on both faces.
    """
    base, d = np.array(base, float), unit(*d)
    nt = np.array(face, float); nt = nt - d * (nt @ d); nt /= np.linalg.norm(nt)
    w_dir = np.cross(nt, d)
    g = -nt * 0.6 + np.array([0, 0, -1.0]) * 0.4; g = g - d * (g @ d); g /= np.linalg.norm(g)

    def mid(uu):
        return base[None, None] + length * (uu[..., None] * d[None, None] + droop * uu[..., None] ** 2 * g[None, None])

    def at(u, v):
        uu = 0.015 + 0.97 * u
        w = width * np.sin(np.pi * np.clip(uu, 0, 1) ** 0.85) ** 0.65
        saw = (uu * teeth) % 1.0
        w = w * (1 - cut * saw * smoothstep(0.08, 0.2, uu)) + 0.004
        s = (v - 0.5) * 2 * w
        cup = 0.012 * (1 - (2 * v - 1) ** 2)
        return mid(uu) + s[..., None] * w_dir[None, None] + cup[..., None] * nt[None, None]

    k.add(name, outward(sheet(at, 140, 9, 0.012)), col, bone, fuse=False, mirror=mirror, surface="leaf")
    for sgn in (1, -1):
        vein = [mid(np.array([[t]]))[0, 0] + nt * (0.008 + 0.008 * sgn) for t in np.linspace(0.02, 0.9, 10)]
        k.add(f"{name}rib{'a' if sgn > 0 else 'b'}", tube(vein, [0.008] * 6 + [0.007, 0.006, 0.005, 0.004], seg=8,
                                                           per=4), rib, bone, fuse=False, mirror=mirror,
              surface="leaf")


def bud_tips(k, top, r, col, bone, n=3):
    """The petals' points parted a little at the top of a bud."""
    for i in range(n):
        a = 2 * math.pi * i / n + 0.3
        o = np.array([math.cos(a), math.sin(a), 0.0])
        p0 = top - np.array([0, 0, r * 1.2])
        pts = [p0 + o * r * 0.5, p0 + o * r * 0.15 + np.cross(o, (0, 0, 1)) * r * 0.45,
               p0 + o * r * 0.15 - np.cross(o, (0, 0, 1)) * r * 0.45, p0 - o * r * 0.2,
               top + o * r * 0.55 + np.array([0, 0, r * 0.25])]
        k.add(f"budtip{i}", hull(pts), col, bone, fuse=False, surface="leaf")


H = np.array([0, -0.29, 0.47])
HR = np.array([0.245, 0.19, 0.158])
JAW = ((0, -0.335, 0.385), (0.21, 0.16, 0.095))
EYE_D = unit(0.62, -0.76, 0.18)
EYE_HALF = (0.058, 0.05)
# Narrowed: the top line falls from the outer corner toward the nose
EYE = [(-1, -0.22), (-0.7, 0.02), (0.2, 0.52), (0.88, 0.7), (1, 0.12), (0.8, -0.58), (0.1, -0.95),
       (-0.6, -0.8), (-0.95, -0.5)]
LEGS = dict(x=0.175, front=-0.15, hind=0.24, top=0.27, ankle=0.06, r=0.09, thigh=(0.14, 0.16, 0.16), splay=0.05,
            foot=(0.1, 0.105, 0.046), toe=0.02, spread=0.034, claw=0.032)


def ivysaur(src, out):
    k = Kit(src, coat="scales")
    BC, BR = (0, 0.05, 0.3), (0.215, 0.27, 0.155)
    b.body(k, BC, BR, TEAL, chest=((0, -0.13, 0.36), (0.17, 0.13, 0.13)))
    b.legs(k, LEGS, TEAL, CLAW)
    b.underside(k, ((0, 0.05, 0.2), (0.21, 0.29, 0.15)), ((0, 0.22, 0.22), (0.23, 0.18, 0.17)), TEAL)
    b.head(k, H, HR, JAW, TEAL)
    ear0, ear1 = (0.135, -0.27, 0.57), (0.19, -0.29, 0.69)
    b.ears(k, ear0, ear1, 0.056, TEAL)
    b.ear_lining(k, ear0, ear1, 0.056, LINING)

    # The bud, tight and pink, its petals' points parted at the top, on the bud bone
    top = b.bulb(k, (0, 0.1, 0.44), 0.36, 0.125, 0.03, (BUD, BUD2), "bud", n=5, tuft=False)
    bud_tips(k, top, 0.045, BUD, "bud")

    # Four fern leaves from under the bud: the front pair out and forward, faces to the front; the hind pair out and
    # back, faces behind; all raised in a V and drooping at their tips
    fern(k, "fernf", (0.05, 0.02, 0.47), (0.62, -0.42, 0.62), (0.2, -0.9, 0.2), 0.43, 0.13, "bud", teeth=8, cut=0.5, droop=0.55)
    fern(k, "fernb", (0.05, 0.14, 0.47), (0.6, 0.58, 0.55), (0.2, 0.85, 0.3), 0.41, 0.12, "bud", teeth=8, cut=0.5, droop=0.55)

    # The patches: a small one on the brow, two on the head's sides, the flanks and the legs
    b.patch(k, b.on_ell(H, HR, (0, -0.35, 0.95)), (0, -0.35, 0.95), (0.042, 0.028), SPOT, b.WIDE)
    b.patch(k, b.on_ell(H, HR, (0.6, -0.3, 0.75)), (0.6, -0.3, 0.75), (0.026, 0.02), SPOT, b.PATCH2, mirror=True)
    for d, h in (((0.85, 0.05, 0.35), (0.048, 0.04)), ((0.7, 0.62, 0.3), (0.036, 0.032)), ((0.8, -0.4, 0.35),
                                                                                           (0.03, 0.026))):
        b.patch(k, b.on_ell(BC, BR, d), d, h, SPOT, b.PATCH, mirror=True)
    b.patch(k, (0.305, -0.14, 0.24), (1, -0.2, 0.2), (0.03, 0.038), SPOT, b.PATCH2, mirror=True, depth=0.04)
    b.patch(k, (0.305, 0.25, 0.25), (1, 0.1, 0.2), (0.03, 0.036), SPOT, b.PATCH, mirror=True, depth=0.04)

    # The mouth shut, a white fang hanging at each corner
    b.mouth(k, H, 0.21, 0.385, 0.16, 0.0, (b.PINK, b.MOUTH, INK, WHITE), fangs=[(0.12, 0.017, 0.04)], opened=False,
            rise=-0.006, line_w=0.006)

    k.build(voxel=0.0075, band=0.008)
    centre = b.on_ell(H, HR, EYE_D)
    cols = dict(iris=RED, pupil=PUPIL, light=WHITE)
    k.eye_states(lambda state: b.lay_eyes(k, state, centre, EYE_D, EYE_HALF, b.eye_colours(WHITE, INK, EYE), TEAL,
                                          INK, EYE, cols))
    k.export(out)


if __name__ == "__main__":
    ivysaur(*args())
