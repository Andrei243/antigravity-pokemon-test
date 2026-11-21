# Blastoise-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/blastoisemega.py -- <the export's Blastoise-Mega.glb> <out.glb>
#
# Grown from blastoise.py (sibling: its frame, head, torso, plates, limbs, paws, claws and eyes). From the two pictures,
# what the Mega changes, largest mass first: the two shoulder cannons gone, one great silver cannon in their place
# out of a raised housing on the shell's top, laid over the head and pointing forward and up, a black band near its
# back end and a dark bore; a heavier shell reaching higher behind the head, its white rim broad; the forearms
# carried forward, each in a brown cuff of shell rimmed in white with a short cannon on its top, the hand under its
# muzzle; red eyes; an open mouth; a short pointed tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, hull, lathe, ring_band, sibling, args

bl = sibling("blastoise")
sq, wt = bl.sq, bl.wt

BLUE, CREAM, SHELL, RIM_COL = bl.BLUE, bl.CREAM, "#8E5C38", bl.RIM_COL
METAL, BORE, EARIN, BAND = "#D2D6DE", "#2A2C34", bl.EARIN, "#24242A"
IRIS, MOUTH, TONGUE = "#C8323A", "#8E2E3A", "#E88A96"

H, HR, HS = bl.H, bl.HR, bl.HS
TC, TR = bl.TC, bl.TR
SC = np.array([0, 0.08, 0.53])
SR = np.array([0.335, 0.225, 0.35])               # heavier than Blastoise's (0.32, 0.21, 0.33)
RIM_R = 0.048
RIM_PLANE = sq.plane(0.035, 0.18, SC[2])
VOXEL = 0.0078

# The great cannon: from inside its housing on the shell's top, forward over the head and up
GB = np.array([0, 0.25, 0.9])
GD = unit(0, -1, 0.3)
GL, GR, GRI, GDEPTH = 0.66, 0.08, 0.058, 0.05
HOUSING = (np.array([0, 0.16, 0.86]), (0.15, 0.16, 0.12))

# The forearm: shoulder, elbow, wrist, carried forward
SHOULDER, ELBOW, WRIST = np.array([0.2, -0.03, 0.63]), np.array([0.3, -0.08, 0.53]), np.array([0.29, -0.24, 0.47])
FD = unit(0.02, -1, -0.08)


def at(x, y, z):
    return H + (np.array([x, y, z]) - np.array([0, -0.1, 0.795])) * HS


def great_cannon(k):
    k.add("housing", ell(*HOUSING), SHELL, "body", surface="shell")
    k.add("gcannon", lathe(GB, GD, [(0, 0.0), (GR, 0.0), (GR, GL - 0.05), (GR * 1.1, GL - 0.045), (GR * 1.1, GL),
                                     (GRI, GL), (GRI, GL - GDEPTH), (0, GL - GDEPTH)], seg=48),
          METAL, "body", fuse=False, surface="metal")
    k.add("gbore", lathe(GB + GD * (GL - GDEPTH + 0.002), GD, [(0, 0), (GRI * 0.98, 0), (GRI * 0.98, 0.004), (0, 0.004)]),
          BORE, "body", fuse=False, surface="metal")
    # the black band near its back end, just out of the housing
    k.add("gband", ring_band(GB + GD * 0.2, GD, GR + 0.004, 0.045, thick=0.008), BAND, "body", fuse=False,
          surface="metal")


def forearms(k):
    sq.limb(k, "arm", [SHOULDER, ELBOW, WRIST], [0.1, 0.088, 0.078], BLUE, "arm{s}")
    sq.paw(k, "hand", WRIST + (-0.005, -0.04, -0.03), (0.07, 0.078, 0.062), (0.05, -1), BLUE, "arm{s}", amp=0.18)
    wt.claws(k, "handclaw", WRIST + (0.0, -0.105, -0.045), (0.05, -1, -0.35), 0.034, 0.034, "arm{s}")
    # the cuff of shell round the forearm, rimmed in white at both ends
    cc = (ELBOW + WRIST) / 2 + np.array([0.008, 0.0, 0.012])
    ad = unit(*(WRIST - ELBOW))
    k.add("cuff", lathe(cc - ad * 0.1, ad, [(0, 0), (0.088, 0), (0.096, 0.03), (0.096, 0.17), (0.086, 0.2), (0, 0.2)]),
          SHELL, "arm{s}", fuse=False, mirror=True, surface="shell")
    for t0 in (-0.105, 0.075):
        k.add(f"cuffrim{'f' if t0 > 0 else 'b'}", lathe(cc + ad * t0, ad, [(0, 0), (0.101, 0), (0.104, 0.015),
                                                                          (0.101, 0.03), (0, 0.03)]),
              RIM_COL, "arm{s}", fuse=False, mirror=True, surface="shell")
    # the short cannon on the cuff's top, its muzzle past the hand
    cb = cc + np.array([0.025, 0.06, 0.07])
    L, R, RI = 0.29, 0.046, 0.032
    k.add("acannon", lathe(cb, FD, [(0, 0.0), (R, 0.0), (R, L - 0.03), (R * 1.12, L - 0.026), (R * 1.12, L), (RI, L),
                                    (RI, L - 0.03), (0, L - 0.03)]), METAL, "arm{s}", fuse=False, mirror=True,
          surface="metal")
    k.add("abore", lathe(cb + FD * (L - 0.028), FD, [(0, 0), (RI * 0.98, 0), (RI * 0.98, 0.004), (0, 0.004)]), BORE,
          "arm{s}", fuse=False, mirror=True, surface="metal")


def head(k):
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell(at(0, -0.19, 0.76), (0.108, 0.118, 0.08)), BLUE, "head")
    k.add("neck", tube([(0, -0.04, 0.6), (0, -0.075, 0.73)], [0.125, 0.115]), BLUE, "head")
    k.add("brow", ell(H + unit(0.5, -0.74, 0.46) * 0.128, (0.05, 0.026, 0.022), rot=(0, 0, 32)), BLUE, "head",
          mirror=True)
    # short pointed ears, laid back, dark inside
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        def P(x, y, z):
            return at(x * s, y, z)
        k.add(f"ear{sd}", hull([P(0.045, -0.075, 0.865), P(0.118, -0.06, 0.85), P(0.09, -0.01, 0.86),
                                P(0.1, -0.03, 0.915)]), BLUE, "head", fuse=False, surface="scales")
        k.add(f"earin{sd}", hull([P(0.06, -0.078, 0.872), P(0.104, -0.066, 0.864), P(0.086, -0.05, 0.874),
                                  P(0.094, -0.05, 0.902)]), EARIN, "head", fuse=False, surface="scales")


def blastoisemega(src, out):
    k = Kit(src, coat="scales")
    head(k)
    sq.torso(k, TC, TR, SC, SR, RIM_PLANE, RIM_R, CREAM, SHELL, RIM_COL, collar=0.7)
    sq.plastron_lines(k, TC, TR, RIM_PLANE, scale=1.9, w=0.005)
    sq.shell_plates(k, SC, SR, RIM_PLANE, RIM_R, w=0.006, hexr=0.11, reach=0.45)
    great_cannon(k)
    forearms(k)
    # Thick thighs fused into the body, short shins, broad feet with white claws, set a little wider
    k.add("thigh", ell((0.17, -0.01, 0.28), (0.165, 0.155, 0.165)), BLUE, "leg{s}", mirror=True)
    sq.limb(k, "leg", [(0.18, -0.02, 0.22), (0.19, -0.03, 0.08)], [0.12, 0.105], BLUE, "leg{s}")
    sq.paw(k, "foot", (0.195, -0.07, 0.045), (0.108, 0.13, 0.048), (0.12, -1), BLUE, "leg{s}", amp=0.12)
    wt.claws(k, "footclaw", (0.205, -0.198, 0.035), (0.1, -1, -0.1), 0.045, 0.036, "leg{s}")
    # a short tail with a pointed fin, back and out to +X
    k.add("tail", tube([(0, 0.14, 0.27), (0.07, 0.3, 0.17), (0.13, 0.41, 0.07)], [0.078, 0.05, 0.014]), BLUE, "tail")
    k.add("tailfin", hull([(0.07, 0.31, 0.2), (0.11, 0.33, 0.21), (0.16, 0.47, 0.16), (0.09, 0.32, 0.14),
                           (0.12, 0.34, 0.15)]), BLUE, "tail", fuse=False, surface="scales")

    # the cream jaw and throat
    jz = at(0, -0.19, 0.76)[2] - 0.01
    def jaw(P):
        line = jz + 0.05 * np.clip(P[:, 1] - (H[1] - 0.1), 0, 1)
        return inside(P[:, 2] - line, 0.003) * (P[:, 1] < H[1] + 0.03)
    k.paint(jaw, CREAM, on=["head", "snout", "neck"])
    # the mouth open: a dark crescent along the snout with a pink tongue in it
    mc = at(0, -0.285, 0.742)
    n = unit(0, -1, -0.1)
    sq.smile(k, mc, n, (0.09, 0.05), color=MOUTH, curve=0.25, thick=1.0)
    k.decal(mc + np.array([0, 0.0, -0.012]), n, (0.045, 0.018), TONGUE, eps=0.006, rings=10, segs=40)

    base = sq.round_eye(iris=IRIS, look_in=0.1)
    LID, INK = srgb(BLUE), srgb(sq.INK)
    def eye(u, v):
        c = base(u, v)
        edge = 0.42 + 0.2 * u
        c[v > edge + 0.18] = LID
        c[np.abs(v - edge - 0.02) <= 0.17] = INK
        return c
    de = unit(0.55, -0.78, 0.26)
    def eyes(state):
        sq.eye_patch(k, state, H, HR[1], de, (0.034, 0.041), eye, BLUE, sq.INK)

    k.build(voxel=VOXEL, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    blastoisemega(*args())
