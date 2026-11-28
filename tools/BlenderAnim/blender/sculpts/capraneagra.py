# Capraneagra (Carpathia No. 10058) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/capraneagra.py -- <the export's Capraneagra.glb> <out.glb>
# Caprita grown (caprita.py, whose hooves, pebbles, horns, face stripe and eyes it keeps). From its picture
# (docs/art/reference/gemini/carpathia/10058-Capraneagra.jpg) and its brief (CP-dex-2-mountains.md, 58): a strong
# chamois in its near-black winter coat, deep in the chest on thick legs; the same white face with a black stripe
# through each eye, a tan bib from the jaw down the throat and a tan line along the lower flank; great slate-grey horns
# swept up from the crown, back and hooked down, ridged across; tan ears out to the sides; shards of grey stone
# armouring its shoulders, its haunches and its knees, pebbles clinging to its shins; a thick ridge of black fur along
# the spine; a short black tuft of a tail; cloven hooves of blue slate.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, tri, unit, args, sibling

cp = sibling("caprita")
lp = cp.lp
spike, on_ell = lp.spike, lp.on_ell

BLACK, WHITE, TAN, EARIN = "#2E2D33", cp.WHITE, "#8A5E3A", "#C9A27E"
HORN, RIDGE, SLATE = "#5E6878", "#434B58", "#4A5464"
STONE, STONE2, NOSE = "#8F897E", "#6E695F", cp.NOSE
IRIS, STRIPE = "#6E4426", "#1C1B20"

# The frame (joints: body (0, 0, 0.5), head (0, -0.28, 0.73), ears (+-0.045, -0.29, 0.805),
# legs (+-0.07, -0.15, 0.46), hinds (+-0.07, 0.17, 0.46), tail (0, 0.26, 0.57))
LX = 0.074
FY, HY = -0.15, 0.17
HC = np.array([0.0, -0.345, 0.742])
HR = np.array([0.076, 0.082, 0.075])


def shard(base, d, length, w, seed):
    """A shard of stone armour: a flat-faced pointed crystal, w across its root, a little uneven."""
    rs = np.random.RandomState(seed)
    base = np.array(base, float); d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d, a)
    pts = []
    for i in range(6):
        t = 2 * math.pi * i / 6 + 0.3
        r = w * (0.85 + 0.3 * rs.rand())
        pts.append(base + (math.cos(t) * a + math.sin(t) * b * 0.7) * r)
        pts.append(base + d * length * 0.55 + (math.cos(t) * a + math.sin(t) * b * 0.7) * r * 0.75)
    pts.append(base + d * length + a * w * 0.15 * (rs.rand() - 0.5))
    return hull(pts)


def capraneagra(src, out_path):
    k = Kit(src, coat="fur")

    # --- a deep strong trunk, the chest forward, the rump round ---------------------------------------------------
    k.add("barrel", ell((0, 0.01, 0.455), (0.118, 0.22, 0.158)), BLACK, "body")
    k.add("chest", ell((0, -0.14, 0.485), (0.118, 0.125, 0.165)), BLACK, "body")
    k.add("rump", ell((0, 0.165, 0.48), (0.112, 0.105, 0.15)), BLACK, "body")
    k.add("neck", tube([(0, -0.16, 0.56), (0, -0.255, 0.665), (0, -0.305, 0.71)], [0.1, 0.086, 0.074]), BLACK,
          "head")
    # the tan line along the lower flank and the tan bib down the throat
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.322) - 0.016 - 0.004 * tri(P[:, 1] * 14), 0.003)
            * (P[:, 1] > -0.16) * (P[:, 1] < 0.2) * inside(-np.abs(P[:, 0]) + 0.05, 0.01), TAN,
            on=["barrel", "chest", "rump"])
    k.paint(lambda P: inside(P[:, 1] - (-0.3 - 0.35 * np.clip(0.68 - P[:, 2], 0, None)), 0.004)
            * inside(np.abs(P[:, 0]) - (0.06 - 0.18 * np.clip(0.66 - P[:, 2], 0, None)), 0.004)
            * (P[:, 2] > 0.5) * (P[:, 2] < 0.72), TAN, on=["neck", "chest", "head"])

    # --- thick legs: broad shoulders and thighs over the upper half, strong shins, slate hooves ------------------
    k.add("shoulder", ell((LX - 0.008, FY + 0.012, 0.37), (0.058, 0.07, 0.13)), BLACK, "leg{s}", mirror=True)
    k.add("fore", tube([(LX, FY, 0.36), (LX + 0.003, FY - 0.005, 0.2), (LX + 0.003, FY - 0.004, 0.045)],
                       [0.048, 0.038, 0.034]), BLACK, "leg{s}", mirror=True)
    k.add("thigh", ell((LX - 0.005, HY + 0.012, 0.38), (0.064, 0.09, 0.14)), BLACK, "hind{s}", mirror=True)
    k.add("hind", tube([(LX, HY + 0.02, 0.33), (LX + 0.003, HY + 0.045, 0.19), (LX + 0.003, HY + 0.018, 0.045)],
                       [0.048, 0.037, 0.034]), BLACK, "hind{s}", mirror=True)
    cp.hoof(k, "fhoof", LX + 0.003, FY - 0.006, 0.036, 0.05, SLATE, "leg{s}", split=0.005)
    cp.hoof(k, "hhoof", LX + 0.003, HY + 0.016, 0.036, 0.05, SLATE, "hind{s}", split=0.005)
    cp.pebbles(k, "fpeb", [(LX + 0.034, FY - 0.016, 0.1), (LX - 0.03, FY - 0.016, 0.13), (LX + 0.033, FY - 0.01, 0.16),
                           (LX + 0.01, FY - 0.04, 0.085)], 0.014, STONE, "leg{s}", 5)
    cp.pebbles(k, "hpeb", [(LX + 0.034, HY + 0.03, 0.11), (LX - 0.03, HY + 0.035, 0.09), (LX + 0.03, HY + 0.045, 0.16)],
               0.014, STONE, "hind{s}", 9)

    # --- the stone armour: a cluster on each shoulder, one on each haunch, a plate on each knee ------------------
    for i, (c, d, ln, w) in enumerate((((0.106, -0.12, 0.56), (0.25, -0.25, 1), 0.15, 0.045),
                                       ((0.112, -0.07, 0.52), (0.25, 0.3, 1), 0.1, 0.035),
                                       ((0.105, -0.17, 0.47), (0.3, -0.5, 0.8), 0.07, 0.03),
                                       ((0.108, 0.2, 0.46), (0.25, 0.5, 0.9), 0.1, 0.04),
                                       ((0.108, 0.15, 0.41), (0.3, 0.3, 0.9), 0.065, 0.028))):
        k.add(f"armour{i}", shard(c, d, ln, w, 20 + i), STONE, "body" if i < 3 else "hind{s}", fuse=False, mirror=True,
              surface="shell")
    k.add("kneeplate", shard((LX + 0.018, FY - 0.04, 0.23), (0.15, -0.5, 1), 0.11, 0.036, 40), STONE, "leg{s}",
          fuse=False, mirror=True, surface="shell")
    k.add("hockplate", shard((LX + 0.022, HY + 0.07, 0.24), (0.3, 0.4, 1), 0.08, 0.03, 41), STONE2, "hind{s}",
          fuse=False, mirror=True, surface="shell")

    # --- the head: the same white face, a black crown, a muzzle forward and down -------------------------------
    k.add("head", ell(HC, HR), BLACK, "head")
    k.add("muzzle", ell(HC + (0, -0.08, -0.04), (0.045, 0.07, 0.044), rot=(22, 0, 0)), WHITE, "head")
    k.add("nose", outward(ell(HC + (0, -0.144, -0.052), (0.02, 0.014, 0.014))), NOSE, "head", fuse=False,
          surface="skin")
    def crown(P):
        d = (P - HC) / HR
        top = inside(0.45 - d[:, 2] + 0.35 * np.clip(-d[:, 1], 0, None), 0.05) * inside(0.3 - np.abs(d[:, 0]), 0.05)
        back = inside(0.1 - d[:, 1], 0.06)
        return np.clip(np.maximum(top, back), 0, 1)
    k.paint(lambda P: (1 - crown(P)) * inside(np.linalg.norm((P - HC) / HR, axis=1) - 1.25, 0.04), WHITE,
            on=["head", "muzzle"])
    de = unit(0.72, -0.64, 0.15)
    E = on_ell(HC, HR, de) + (0, 0, 0.001)
    k.paint(cp.eye_stripe(HC, E, 0.55, 0.022, 0.06, 0.07, 0.018), STRIPE, on=["head", "muzzle"])
    # the tan where the black of the cheek meets the throat
    k.paint(lambda P: inside(np.linalg.norm((P - (HC + (0.05, 0.01, -0.05))) * np.array([1.0, 1.2, 1.0]), axis=1)
                             - 0.035, 0.004) * (np.abs(P[:, 0]) > 0.03), TAN, on=["head", "neck"])
    k.paint(lambda P: inside(np.linalg.norm((P - (HC * np.array([-1, 1, 1]) + (-0.05, 0.01, -0.05)))
                                            * np.array([1.0, 1.2, 1.0]), axis=1) - 0.035, 0.004)
            * (np.abs(P[:, 0]) > 0.03), TAN, on=["head", "neck"])
    k.decal(HC + (0.034, -0.11, -0.07), unit(0.6, -0.6, -0.5), (0.018, 0.004), cp.INK, up=unit(0, 0.4, 1), eps=0.003,
            rings=3, segs=24, mirror=True)

    # --- tan ears out to the sides under the horns; the great ridged horns, up, back and hooked down -----------
    lp.ear(k, (0.055, -0.335, 0.79), (0.13, -0.315, 0.83), 0.032, unit(0.25, 1, 0.4), TAN, EARIN, BLACK,
           tip_from=0.82, lining_w=0.6)
    HP = [(0.024, -0.36, 0.795), (0.03, -0.355, 0.86), (0.045, -0.315, 0.915), (0.066, -0.25, 0.94),
          (0.084, -0.185, 0.92), (0.094, -0.155, 0.87), (0.094, -0.168, 0.83)]
    cp.horn(k, "horn", HP, [0.03, 0.028, 0.025, 0.022, 0.019, 0.016, 0.014], HORN, fuse=True,
            ridges=[(0.12 + 0.1 * i, 0.16 + 0.1 * i) for i in range(8)], ridge_colour=RIDGE)

    # --- the thick ridge of fur along the spine, the short tuft of a tail ----------------------------------------
    for i, (y, z, ln) in enumerate(((-0.2, 0.67, 0.07), (-0.14, 0.645, 0.085), (-0.08, 0.63, 0.09), (-0.02, 0.62, 0.09),
                                    (0.04, 0.615, 0.085), (0.1, 0.62, 0.08), (0.16, 0.625, 0.07), (0.21, 0.62, 0.055))):
        for s in (-1, 0, 1):
            k.add(f"ridge{i}{s + 1}", spike((0.025 * s, y, z - 0.03), (0.35 * s, 0.75, 0.65 - 0.2 * abs(s)),
                                            ln - 0.02 * abs(s), 0.026, 0.018, side=(1, 0, 0) if s == 0 else None),
                  BLACK, "body", fuse=False, surface="fur")
    k.add("tail", spike((0, 0.26, 0.56), (0, 0.6, 0.6), 0.08, 0.03, 0.02), BLACK, "tail", fuse=False, surface="fur")

    k.build(voxel=0.007, smooth=3, band=0.006)

    # --- the eyes: brown, set in the black stripe, a little stern -----------------------------------------------
    look = cp.brown_eye(IRIS)

    def eyes(state):
        cp.lay_eyes(k, state, E, de, (0.021, 0.018), look, STRIPE, "#B8B4AC")
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    capraneagra(*args())
