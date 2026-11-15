# Blastoise sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown
# from Squirtle's script (its torso, rim, plates, limbs, paws, eyes and mouth) and Wartortle's (its claws):
#   blender --background --factory-startup --python sculpts/blastoise.py -- <the export's Blastoise.glb> <out.glb>
#
# The design, from the references: a big, broad, heavy blue turtle; a great brown shell over its back rimmed in white,
# its plates parted by dark lines; a short silver-grey cannon out of the shell above each shoulder, its back end in a
# rounded housing of the shell, pointing forward and a little out and up, its bore dark; a head on a short neck, a long
# snout with a cream jaw and throat, small brown eyes under a heavy brow, small pointed ears dark inside; a cream
# plastron; thick arms down along the sides with three white claws, thick thighs fused into the body, feet with white
# claws; a short pointed tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, hull, lathe, sibling, args

sq = sibling("squirtle")
wt = sibling("wartortle")

BLUE, CREAM, SHELL, RIM_COL = "#6FA6E0", sq.CREAM, "#94603A", sq.RIM
METAL, BORE, EARIN, CLAW = "#C9CED6", "#2A2C34", "#22242E", wt.CLAW
IRIS = "#7A4626"

H = np.array([0, -0.105, 0.805])
HR = np.array([0.136, 0.138, 0.115])           # 15% over the first build
HS = 1.15                                      # the same, for the parts placed about the head
TC = np.array([0, -0.03, 0.47])
TR = np.array([0.265, 0.2, 0.27])              # broad: 15% wider than the first build
SC = np.array([0, 0.075, 0.52])
SR = np.array([0.32, 0.21, 0.33])
RIM_R = 0.04
RIM_PLANE = sq.plane(0.035, 0.18, SC[2])
VOXEL = 0.0075

CB = np.array([0.17, 0.0, 0.715])              # a cannon's back end, inside its housing
CD = unit(0.12, -1, 0.12)                      # forward, a little out and up: both bores show from the front
CL, CR, CRI, DEPTH = 0.25, 0.06, 0.042, 0.035
HOUSING = (np.array([0.17, -0.02, 0.715]), (0.095, 0.08, 0.088))


def cannon(k):
    """A short silver-grey barrel out of a rounded housing of shell above the shoulder, a lip at its muzzle."""
    k.add("housing", ell(*HOUSING), SHELL, "body", mirror=True, surface="shell")
    k.add("cannon", lathe(CB, CD, [(0, 0.0), (CR, 0.0), (CR, CL - 0.04), (CR * 1.12, CL - 0.035), (CR * 1.12, CL),
                                   (CRI, CL), (CRI, CL - DEPTH), (0, CL - DEPTH)]),
          METAL, "body", fuse=False, mirror=True, surface="metal")
    k.add("bore", lathe(CB + CD * (CL - DEPTH + 0.002), CD, [(0, 0), (CRI * 0.98, 0), (CRI * 0.98, 0.004), (0, 0.004)]),
          BORE, "body", fuse=False, mirror=True, surface="metal")


def blastoise(src, out):
    k = Kit(src, coat="scales")

    def at(x, y, z):
        """A point placed about the first build's head, moved with the larger head."""
        return H + (np.array([x, y, z]) - np.array([0, -0.1, 0.795])) * HS

    # Head: a long snout, a short thick neck, a heavy brow over each eye
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell(at(0, -0.19, 0.76), (0.106, 0.115, 0.076)), BLUE, "head")
    k.add("neck", tube([(0, -0.04, 0.6), (0, -0.075, 0.73)], [0.12, 0.11]), BLUE, "head")
    de = unit(0.55, -0.78, 0.26)
    k.add("brow", ell(H + unit(0.5, -0.74, 0.46) * 0.128, (0.048, 0.024, 0.02), rot=(0, 0, 32)), BLUE, "head",
          mirror=True)
    # Small pointed ears, dark inside
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        def P(x, y, z):
            return at(x * s, y, z)
        k.add(f"ear{sd}", hull([P(0.045, -0.085, 0.865), P(0.115, -0.07, 0.85), P(0.085, -0.03, 0.86),
                                P(0.088, -0.07, 0.935)]), BLUE, "head", fuse=False, surface="scales")
        k.add(f"earin{sd}", hull([P(0.058, -0.088, 0.875), P(0.102, -0.078, 0.866), P(0.082, -0.07, 0.875),
                                  P(0.086, -0.08, 0.918)]), EARIN, "head", fuse=False, surface="scales")

    sq.torso(k, TC, TR, SC, SR, RIM_PLANE, RIM_R, CREAM, SHELL, RIM_COL, collar=0.68)
    sq.plastron_lines(k, TC, TR, RIM_PLANE, scale=1.9, w=0.005)
    sq.shell_plates(k, SC, SR, RIM_PLANE, RIM_R, w=0.006, hexr=0.11, reach=0.45)
    cannon(k)

    # Thick arms down along the sides and a little forward, the hands before the plastron's edge, three white claws
    sq.limb(k, "arm", [(0.2, -0.03, 0.63), (0.29, -0.07, 0.52), (0.28, -0.15, 0.4)], [0.105, 0.09, 0.078], BLUE,
            "arm{s}")
    sq.paw(k, "hand", (0.28, -0.185, 0.375), (0.072, 0.08, 0.062), (0.15, -1), BLUE, "arm{s}", amp=0.18)
    wt.claws(k, "handclaw", (0.29, -0.25, 0.36), (0.15, -1, -0.35), 0.035, 0.034, "arm{s}")
    # Thick thighs fused into the body, short shins, broad feet with white claws
    k.add("thigh", ell((0.16, -0.01, 0.28), (0.155, 0.15, 0.16)), BLUE, "leg{s}", mirror=True)
    sq.limb(k, "leg", [(0.17, -0.02, 0.22), (0.18, -0.03, 0.08)], [0.115, 0.1], BLUE, "leg{s}")
    sq.paw(k, "foot", (0.185, -0.07, 0.045), (0.105, 0.13, 0.048), (0.1, -1), BLUE, "leg{s}", amp=0.12)
    wt.claws(k, "footclaw", (0.192, -0.198, 0.035), (0.08, -1, -0.1), 0.045, 0.036, "leg{s}")
    # A short pointed tail, back and out to +X
    k.add("tail", tube([(0, 0.14, 0.27), (0.06, 0.29, 0.18), (0.11, 0.39, 0.08)], [0.075, 0.048, 0.012]), BLUE,
          "tail")

    # The cream jaw and throat, below a line along the snout's side, clear down the neck to the plastron
    jz = at(0, -0.19, 0.76)[2] - 0.01
    def jaw(P):
        line = jz + 0.05 * np.clip(P[:, 1] - (H[1] - 0.1), 0, 1)
        return inside(P[:, 2] - line, 0.003) * (P[:, 1] < H[1] + 0.03)
    k.paint(jaw, CREAM, on=["head", "snout", "neck"])
    # The mouth: a long line along the snout, rising a little at its corners
    mc = at(0, -0.288, 0.755)
    sq.smile(k, mc, unit(0, -1, 0.05), (0.086, 0.025), curve=0.4, thick=0.5)

    # Small brown eyes under a heavy brow
    base = sq.round_eye(iris=IRIS, look_in=0.1)
    LID, INK = srgb(BLUE), srgb(sq.INK)
    def eye(u, v):
        c = base(u, v)
        edge = 0.42 + 0.2 * u
        c[v > edge + 0.18] = LID
        c[np.abs(v - edge - 0.02) <= 0.17] = INK
        return c
    def eyes(state):
        sq.eye_patch(k, state, H, HR[1], de, (0.034, 0.041), eye, BLUE, sq.INK)

    k.build(voxel=VOXEL, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    blastoise(*args())
