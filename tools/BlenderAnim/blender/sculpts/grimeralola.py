# Grimer-Alola sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grimeralola.py -- <the export's Grimer-Alola.glb> <out.glb>
#
# From the references: Grimer's heap, puddle, column, head lump and arms, gone a deep green with darker green wavy
# bands; the great open mouth edged all round in a thick yellow lip whose yellow runs down in a drip from the lower
# lip; dark within, the lower part a broad blue tongue; two white crystal teeth hanging from the upper lip under the
# eyes; round white eyes with small black pupils and a dark rim, on bumps at the head's front-top.
# Grown from grimer.py (sibling("grimer")): its puddle, drips, hands, arms, bands, eye and two-cutter mouth; its main
# block is copied here and changed, since it builds outside a function.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, hull, args, sibling

g = sibling("grimer")
GREEN, STRIPE, MOUTH, TONGUE, LIP = "#4C8A6C", "#2C6448", "#1E1A24", "#3B7BDB", "#F2D33A"
TOOTH = "#F4F4F8"
g.PURPLE = GREEN                     # the line's makes colour by the module's own name

HEAD, HEADR = g.HEAD, g.HEADR
EYE = np.array([0.054, -0.098, 0.47])
EYER = 0.038
MOUTHC = np.array([0.0, -0.13, 0.295])
MOUTHR = (0.078, 0.06, 0.095)
FLOORC = np.array([0.0, -0.11, 0.258])
FLOORR = (0.062, 0.05, 0.042)
SHOULDER, ARM, HAND = g.SHOULDER, g.ARM, g.HAND


def lip(P):
    """A thick yellow lip all round the mouth's opening, on the front."""
    q = (P[:, 0] / MOUTHR[0]) ** 2 + ((P[:, 2] - MOUTHC[2]) / MOUTHR[2]) ** 2
    return inside(np.sqrt(q) - 1.32, 0.0025) * (P[:, 1] < -0.07) * (P[:, 2] > 0.17)


def mouth_box(P):
    q = ((P[:, 0] / 0.13) ** 2 + ((P[:, 2] - MOUTHC[2]) / 0.15) ** 2)
    return 1 - inside(q - 1, 0.1) * (P[:, 1] < -0.06)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")

    g.puddle(k, "puddle", (0, 0.01, 0.028), 0.25, 0.23, 0.04, "body")
    k.add("mound", ell((0, 0.02, 0.105), (0.195, 0.175, 0.1)), GREEN, "body")
    k.add("torso", ell((0, 0.0, 0.215), (0.158, 0.14, 0.13)), GREEN, "body")
    k.add("upper", ell((0, -0.005, 0.32), (0.13, 0.118, 0.11)), GREEN, "body")
    k.add("head", ell(HEAD, HEADR * (1.05, 1.05, 1.05)), GREEN, "body")
    k.add("eyebump", ell(EYE, (EYER, EYER * 0.9, EYER)), GREEN, "body", mirror=True)
    for i, (x, y, z, r) in enumerate([(0.15, -0.06, 0.12, 0.05), (-0.16, -0.04, 0.11, 0.055), (0.12, 0.12, 0.13, 0.055),
                                      (-0.1, 0.13, 0.15, 0.05), (0.0, 0.15, 0.1, 0.06), (0.13, 0.02, 0.24, 0.045),
                                      (-0.13, 0.03, 0.25, 0.045), (0.0, 0.1, 0.3, 0.05)]):
        k.add(f"lump{i}", ell((x, y, z), (r, r, r * 0.85)), GREEN, "body")
    for i, (x, y, z) in enumerate([(0.165, -0.05, 0.13), (-0.17, 0.0, 0.14), (0.1, 0.15, 0.15)]):
        g.drip(k, f"sidedrip{i}", (x, y, z), 0.08, 0.022, "body", colour=GREEN)

    for s in (1, -1):
        b = "armR" if s > 0 else "armL"
        g.arm(k, s, ARM, [0.058, 0.05, 0.046], b)
        g.hand(k, s, HAND, 0.05, b)
        g.drip(k, f"armdrip{'R' if s > 0 else 'L'}", np.array([0.19, -0.15, 0.165]) * (s, 1, 1), 0.05, 0.017, b, colour=GREEN)

    # The mouth: a tall oval, dark, a broad blue tongue in its lower part; the yellow lip round it, a yellow drip
    k.add("mouth", ell(MOUTHC, MOUTHR, rot=(-14, 0, 0)), MOUTH, "body", carve=True)
    k.add("floor", ell(FLOORC, FLOORR), TONGUE, "body", carve=True)
    g.drip(k, "lipdrip", (0.035, -0.152, 0.205), 0.075, 0.019, "body", colour=LIP)
    # Two white crystal teeth hanging from the upper lip, a little to -X as the art has them
    for x in (-0.034, 0.018):
        b = np.array([x, -0.158, 0.388])
        k.add(f"tooth{x:+.2f}", hull([b + (-0.014, 0.004, 0.006), b + (0.014, 0.004, 0.006), b + (-0.012, -0.01, 0.0),
                                       b + (0.012, -0.008, 0.002), b + (0.0, 0.012, 0.004), b + (-0.002, -0.004, -0.026),
                                       b + (0.006, 0.0, -0.022)]), TOOTH, "body", fuse=False, surface="shell")

    body = ["puddle", "mound", "torso", "upper", "head", "lump", "sidedrip"]
    k.paint(g.bands([0.06, 0.13, 0.2, 0.275, 0.355], 0.012, 0.0085, keep=mouth_box), STRIPE, on=body)
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.448 + 0.04 * (P[:, 1] + 0.03) / 0.1)) - 0.008, 0.003)
            * (P[:, 1] > -0.05), STRIPE, on=["head"])
    for s in (1, -1):
        k.paint(g.arm_bands(s, SHOULDER, HAND, (0.35, 0.6, 0.85), 0.009, 0.075), STRIPE, on=["arm", "hand"])
    k.paint(lip, LIP)

    k.build(voxel=0.0055, smooth=3)

    CL, CI = srgb(GREEN), srgb(g.INK)

    def eyes(state):
        for s in (1, -1):
            n = unit(0.42 * s, -0.8, 0.42)
            look = eye_look(state, g.eye_open, CL, CI, line=0.16)
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            k.decal(EYE * (s, 1, 1) + n * EYER * 0.92, n, (0.036, 0.036), lk, eps=0.004, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
