# Nidoking sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nidoking.py -- <the export's Nidoking.glb> <out.glb>
# Grown from nidoranm.py and nidorino.py (the line's lilac, its ears with their teal lining and notched edges, its
# horn and spines as solid pieces, its white claws and fangs, Nidorino's white scowling eye), by their functions.
# From the references: a heavy biped reptile, the thighs one with a leaning body, short thick shins and broad feet
# each with one great white claw; a pale belly from the throat down, crossed by lines like plates; a long thick
# horn on the forehead and a spike behind it; great ears, rounder than Nidorino's, teal inside, notched behind;
# the mouth open and pink, white fangs above and below; thick arms carried forward and down, two white claws a hand;
# spines down the back; a thick tail ringed by grooves, curling up to one side, two white spikes along it.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, inside, unit, boxy, sibling, args

N = sibling("nidoranm")
LILAC, CLAW = N.LILAC, N.CLAW
BELLY, PLATE, GROOVE, MOUTH = "#E6E0E8", "#AEA4B6", "#9A6AA8", "#D8707E"

L = SimpleNamespace(
    H=np.array([0.0, -0.13, 0.69]),                    # the head bone is at 0, -0.06, 0.64
    HR=np.array([0.112, 0.118, 0.1]),
    ear_base=(0.07, -0.08, 0.75), ear_d=(0.52, 0.32, 0.8), ear_n=(0.5, -1.0, 0.15),
    ear_len=0.25, ear_w=0.23, ear_t=0.04, ear_point=1.0, notches=3, notch=0.4,
    lining_at=0.48, lining_w=0.74, lining_h=0.35,
    horn_base=(0, -0.2, 0.765), horn_d=(0, -0.58, 0.81), horn_len=0.17, horn_r=0.046,
    iris=N.SCLERA, pupil="#1A0C10", pupil_w=0.42, brow=0.28, brow_slope=0.95,
    eye_c=None, eye_n=None, eye_h=(0.032, 0.03), eye_up=(0, 0.15, 1),
)
L.eye_n = unit(0.55, -0.8, 0.22)
L.eye_c = L.H + np.array([0.0, -0.02, 0.0]) + L.eye_n * 0.105

# The tail's line: thick at the hips, back along the ground and curling up to the model's +X side
TAIL = np.array([(0, 0.12, 0.33), (0.02, 0.27, 0.22), (0.1, 0.42, 0.17), (0.23, 0.5, 0.21), (0.33, 0.48, 0.3),
                 (0.36, 0.4, 0.4)])
TAIL_R = [0.13, 0.11, 0.085, 0.062, 0.042, 0.012]


def nidoking(src, out):
    k = Kit(src, coat="scales")
    H, HR = L.H, L.HR

    # The body: a heavy leaning trunk, wide at the hips, the thighs one with it
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.12 * low)[:, None]
        return V
    TC, TR = np.array([0.0, 0.02, 0.46]), np.array([0.2, 0.17, 0.2])
    k.add("torso", ell(TC, TR, deform=pear), LILAC, "body")
    k.add("chest", ell((0, -0.03, 0.58), (0.19, 0.15, 0.11)), LILAC, "body")
    k.add("hips", ell((0, 0.04, 0.32), (0.2, 0.16, 0.12)), LILAC, "body")
    k.add("neck", tube([(0, -0.02, 0.6), (0, -0.07, 0.66), (0, -0.1, 0.69)], [0.12, 0.1, 0.09]), LILAC, "head")

    # The legs: thighs fused into the hips, short thick shins, broad feet with one great white claw
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.16, 0.0, 0.27)) * m, (0.13, 0.145, 0.15)), LILAC, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.165, -0.01, 0.18), (0.17, -0.02, 0.12), (0.172, -0.03, 0.09)]) * m,
                                  [0.1, 0.09, 0.082]), LILAC, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.172, -0.06, 0.035)) * m, (0.09, 0.115, 0.04)), LILAC, f"leg{side}")
        b = np.array((0.172, -0.15, 0.03))
        k.add(f"toe{side}", hull(np.array([b + (-0.035, 0.03, -0.022), b + (0.035, 0.03, -0.022),
                                            b + (-0.03, 0.03, 0.03), b + (0.03, 0.03, 0.03),
                                            b + (0.012, -0.1, -0.008)]) * m), CLAW, f"leg{side}", fuse=False,
              surface="shell")

    # The arms: thick, forward and a little down, a great forearm, two white claws a hand
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.17, -0.04, 0.57), (0.26, -0.09, 0.53), (0.3, -0.18, 0.49)]) * m,
                                 [0.085, 0.078, 0.082]), LILAC, f"arm{side}")
        k.add(f"fore{side}", ell(np.array((0.3, -0.2, 0.485)) * m, (0.085, 0.1, 0.08), rot=(0, 0, 20 * s)), LILAC,
              f"arm{side}")
        for j, (dx, dz) in enumerate(((0.03, 0.02), (-0.035, -0.015))):
            base = np.array((0.3 + dx, -0.285, 0.475 + dz))
            N.fang(k, f"handclaw{side}{j}", base * m, (0.25 * s * np.sign(dx), -1, -0.35), 0.05, 0.018,
                   bone=f"arm{side}")

    # The tail, ringed by grooves, two white spikes along its outer side
    k.add("tail", tube(TAIL, TAIL_R), LILAC, "tail")
    for i in range(1, len(TAIL) - 1):
        a = TAIL[i + 1] - TAIL[i - 1]
        k.add(f"groove{i}", ring_band(TAIL[i], a, TAIL_R[i] + 0.004, 0.014), GROOVE, "tail", fuse=False,
              surface="scales")
    for j, (i, out_) in enumerate(((3, (0.6, 0.6, 0.55)), (4, (0.75, 0.15, 0.65)))):
        c = TAIL[i]; d = unit(*out_)
        N.spine(k, f"tailspike{j}", c + d * TAIL_R[i] * 0.7, d, 0.11, 0.06, 0.04, bone="tail", color=CLAW,
                lean=(0, 0, 1))

    # The head: a broad crown, a snout over a dropped jaw, the mouth open and pink
    k.add("cranium", ell(H, HR), LILAC, "head")
    def wedge(V):
        V = boxy(2.4)(V)
        V[:, 0] *= 1 + 0.15 * V[:, 1]
        return V
    k.add("snout", ell(H + (0, -0.105, -0.025), (0.085, 0.1, 0.058), rot=(-6, 0, 0), deform=wedge), LILAC, "head")
    k.add("jaw", ell(H + (0, -0.075, -0.105), (0.08, 0.1, 0.038), rot=(16, 0, 0), deform=wedge), LILAC, "head")
    k.add("cheek", ell(H + (0, -0.02, -0.06), (0.1, 0.07, 0.065)), LILAC, "head")
    k.add("brow", ell(H + (0.055, -0.07, 0.045), (0.045, 0.055, 0.022), rot=(-15, 0, -12)), LILAC, "head",
          mirror=True)
    def gape(V):
        V = V.copy()
        V[:, 2] += 0.3 * V[:, 1] ** 2
        return V
    k.add("mouth", ell(H + (0, -0.14, -0.07), (0.066, 0.1, 0.028), rot=(-4, 0, 0), deform=gape), MOUTH, "head",
          carve=True)
    for m, side in ((1, "R"), (-1, "L")):
        N.fang(k, "fang" + side, H + (0.045 * m, -0.195, -0.05), (0.1 * m, -0.15, -1), 0.034, 0.012)
        N.fang(k, "lowfang" + side, H + (0.045 * m, -0.17, -0.098), (0.1 * m, -0.2, 1), 0.026, 0.011)

    # The ears, the horn, the spike behind it and the spines down the back
    N.ear(k, L)
    N.horn(k, L)
    N.spine(k, "crown", H + (0, 0.03, 0.09), (0, 0.25, 1), 0.1, 0.07, 0.04, bone="head", lean=(0, 1, 0))
    for j, (y, z, ln, lean) in enumerate([(0.1, 0.62, 0.15, 1), (0.15, 0.53, 0.16, -1), (0.18, 0.43, 0.14, 1),
                                          (0.18, 0.34, 0.11, -1)]):
        N.spine(k, f"spine{j}", (0.015 * lean, y, z), (0.1 * lean, 0.8, 0.6), ln, 0.1, 0.045, lean=(0, 1, -0.4))

    # The pale belly from the throat down, crossed by lines like plates
    def belly(P):
        a = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - 0.02)))
        top = 0.66 - 0.05 * (a / 1.0) ** 2
        return inside(a - 0.95, 0.006) * inside(P[:, 2] - top, 0.004) * inside(0.24 - P[:, 2], 0.004) * (P[:, 1] < 0.06)
    def plates(P):
        return belly(P) * sum(inside(np.abs(P[:, 2] - z) - 0.004, 0.0025) for z in (0.33, 0.42, 0.51, 0.59))
    k.paint(belly, BELLY, on=["torso", "chest", "hips", "neck"])
    k.paint(plates, PLATE, on=["torso", "chest", "hips", "neck"])

    k.build(voxel=0.0075, smooth=4)
    k.eye_states(N.eyes_of(k, L))
    k.export(out)


if __name__ == "__main__":
    nidoking(*args())
