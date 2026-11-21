# Mega Heracross sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/heracrossmega.py -- <the export's Heracross-Mega.glb> <out.glb>
#
# Grown from heracross.py (its blue shell, its belly plate outlined in a line and its round yellow eye in a black rim).
# From the references: the fork is gone; ONE great horn rises from the face high over everything, curving back a little,
# with two pairs of short spikes on its sides. The hands have become two great round armoured fists raised either side
# of the head (the largest mass after the horn), each with an orange seam round it and three white claws at its top
# curling inward. A small head between them, an orange-red brow plate over a fierce yellow eye. A round armoured egg of
# a body, its belly plate banded across and outlined orange; yellow wing undersides at the back's foot. Short legs:
# round thighs with a spike, feet with white claws.
# The export's joints: body (0, 0, 0.42), head (0, -0.04, 0.56), arms (+-0.17, -0.02, 0.52), legs (+-0.11, 0, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, inside, unit, eye_look, args, axes

BLUE, DEEP, LIGHT, BAND = "#2F5CA0", "#24497F", "#3A69AE", "#1B3462"
SEAM, BROW, CLAW, YELLOW, WING, INK = "#EC5A2C", "#E4512C", "#F2F2EE", "#F6CC2E", "#F2C23A", "#14161C"


def spike(base, tip, w, thin=0.6):
    """A solid kite of a spike from base to tip, w wide at its root (a flat-faced hull)."""
    base, tip = np.array(base, float), np.array(tip, float)
    a, b = axes(tip - base)
    return hull([tip, base + a * w, base - a * w, base + b * w * thin, base - b * w * thin,
                 base + (tip - base) * 0.45 + a * w * 0.55, base + (tip - base) * 0.45 - a * w * 0.55])


def heracrossmega(src, out):
    k = Kit(src, coat="shell")

    # --- the body: a round armoured egg, a chest rising to the head ----------------------------------------------
    k.add("body", ell((0, 0, 0.34), (0.23, 0.2, 0.225)), BLUE, "body")
    k.add("chest", ell((0, -0.01, 0.48), (0.19, 0.16, 0.1)), BLUE, "body")
    k.add("wingfoot", ell((0, 0.13, 0.22), (0.16, 0.09, 0.1)), BLUE, "body")

    # --- the head: small, between the fists, carried forward ------------------------------------------------------
    HC = np.array([0.0, -0.075, 0.6])
    k.add("head", ell(HC, (0.108, 0.1, 0.096)), BLUE, "head")
    k.add("hornfoot", ell((0, -0.13, 0.66), (0.06, 0.055, 0.07)), BLUE, "head")

    # --- the great horn: rising from over the face, curving back, two pairs of short spikes on its sides ----------
    STEM = [(0, -0.135, 0.64), (0, -0.14, 0.76), (0, -0.11, 0.9), (0, -0.055, 1.04), (0, 0.0, 1.15), (0, 0.04, 1.235)]
    k.add("horn", tube(STEM, [0.068, 0.058, 0.047, 0.035, 0.022, 0.006]), BLUE, "head", fuse=False)
    for z, y, L, w in ((0.79, -0.14, 0.085, 0.026), (0.9, -0.11, 0.07, 0.022)):
        for s in (1, -1):
            b0 = np.array([0.03 * s, y, z])
            k.add(f"hspike{z}{s}", spike(b0, b0 + np.array([L * s, -0.03, L * 0.55]), w), BLUE, "head",
                  fuse=False, surface="shell")

    # --- the arms: a short thick arm up from the shoulder to a great round fist raised beside the head --------------
    FIST = np.array([0.305, -0.06, 0.72])
    FR = 0.158
    k.add("shoulder", ell((0.19, -0.02, 0.5), (0.085, 0.08, 0.08)), BLUE, "arm{s}", mirror=True)
    k.add("forearm", tube([(0.2, -0.03, 0.5), (0.26, -0.05, 0.56), (0.29, -0.06, 0.62)], [0.055, 0.05, 0.055]), DEEP,
          "arm{s}", mirror=True)
    k.add("fist", ell(FIST, (FR, FR * 0.95, FR)), BLUE, "arm{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        c = FIST * m
        # an orange seam round the fist's outer half, and one round the knuckle cap at its top
        d = 0.05
        k.add(f"seam{side}", ring_band(c + np.array([d * s, 0, 0]), (1, 0, 0), math.sqrt(FR ** 2 - d ** 2) + 0.003,
                                        0.013, thick=0.008), SEAM, f"arm{side}", fuse=False, surface="shell")
        ax = unit(-0.25 * s, -0.12, 1)
        d2 = 0.115
        k.add(f"cap{side}", ring_band(c + ax * d2, ax, math.sqrt(FR ** 2 - d2 ** 2) + 0.004, 0.013, thick=0.008), SEAM,
              f"arm{side}", fuse=False, surface="shell")
        # three white claws at the fist's top, curling inward over it
        top = c + ax * (FR * 0.98)
        a, b = axes(ax)
        for j, off in enumerate((-0.055, 0.0, 0.055)):
            root = top + np.array([0, off, 0]) + np.array([0.025 * s, 0, 0.0])
            tip = root + np.array([-0.09 * s, -0.04, -0.005])
            k.add(f"claw{j}{side}", spike(root, tip, 0.038, 0.75), CLAW, f"arm{side}", fuse=False, surface="shell")

    # --- legs: round thighs fused into the egg, short shins, feet with white claws and a spike on each thigh ------
    k.add("thigh", ell((0.14, -0.01, 0.17), (0.1, 0.1, 0.1)), BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.16, -0.02, 0.13), (0.17, -0.03, 0.07)], [0.06, 0.055]), DEEP, "leg{s}", mirror=True)
    k.add("foot", ell((0.175, -0.05, 0.04), (0.065, 0.085, 0.042)), BLUE, "leg{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"tspike{side}", spike(np.array([0.22, 0.0, 0.17]) * m, np.array([0.31, 0.03, 0.12]) * m, 0.032), BLUE,
              f"leg{side}", fuse=False, surface="shell")
        k.add(f"toe{side}", spike(np.array([0.2, -0.11, 0.035]) * m, np.array([0.27, -0.17, 0.012]) * m, 0.028), CLAW,
              f"leg{side}", fuse=False, surface="shell")
        k.add(f"heel{side}", spike(np.array([0.2, 0.02, 0.04]) * m, np.array([0.26, 0.08, 0.02]) * m, 0.024), CLAW,
              f"leg{side}", fuse=False, surface="shell")

    # --- the belly's plate: lighter, banded across, outlined orange --------------------------------------------------
    ON = ["body", "chest"]

    def plate_d(P):
        a = np.arctan2(P[:, 0], -P[:, 1])
        r = np.sqrt(P[:, 0] ** 2 + P[:, 1] ** 2)
        side = (np.abs(a) - 0.82) * r
        top = P[:, 2] - (0.47 - 0.25 * P[:, 0] ** 2)
        low = 0.15 - P[:, 2]
        return np.maximum(np.maximum(side, top), low) * (P[:, 1] < 0.02) + (P[:, 1] >= 0.02) * 1.0

    k.paint(lambda P: inside(plate_d(P) + 0.006, 0.0025), LIGHT, on=ON)
    for zb in (0.2, 0.255, 0.31, 0.365):
        k.paint(lambda P, zb=zb: inside(np.abs(P[:, 2] - zb) - 0.006, 0.0025) * inside(plate_d(P) + 0.014, 0.002),
                BAND, on=ON)
    k.paint(lambda P: inside(np.abs(plate_d(P)) - 0.007, 0.0022), SEAM, on=ON)
    # the wing cases' seam down the back, and the yellow undersides below them
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.0045, 0.0022) * (P[:, 1] > 0.05) * inside(P[:, 2] - 0.55, 0.004)
            * inside(0.26 - P[:, 2], 0.004), BAND, on=ON)
    # the yellow undersides showing below the wing cases: a crescent low on the back, its top edge an arch
    k.paint(lambda P: inside(P[:, 2] - (0.215 - 0.9 * P[:, 0] ** 2), 0.0025) * inside(np.abs(P[:, 0]) - 0.11, 0.003)
            * inside(0.07 - P[:, 1], 0.004), WING)

    # --- the brow plate over the eyes: orange-red, its edge sloping down to the middle -------------------------------
    k.paint(lambda P: inside(0.571 + 0.45 * np.abs(P[:, 0]) - P[:, 2], 0.002) * inside(P[:, 2] - 0.7, 0.004)
            * (P[:, 1] < -0.06), BROW, on=["head", "hornfoot"])

    k.build(voxel=0.0085)

    # --- the eyes: yellow in a black rim, a black pupil, a glint; the brow cuts their tops ------------------------
    CY, CK, CW, CR = srgb(YELLOW), srgb(INK), np.array([1.0, 1.0, 1.0]), srgb(BROW)

    def eye(u, v):
        r = u * u + v * v
        c = np.tile(CK, (len(u), 1))
        c[r < 0.8 ** 2] = CY
        c[((u + 0.05) / 0.3) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = CK
        c[((u + 0.15) / 0.13) ** 2 + ((v - 0.08) / 0.13) ** 2 < 1] = CW
        c[v > 0.3 + 0.4 * u] = CR
        return c

    de = unit(0.5, -0.85, 0.12)
    E = np.array([0.056, -0.155, 0.588])

    def eyes(state):
        k.decal(E, de, (0.036, 0.04), eye_look(state, eye, CR, CK, line=0.15), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    heracrossmega(*args())
