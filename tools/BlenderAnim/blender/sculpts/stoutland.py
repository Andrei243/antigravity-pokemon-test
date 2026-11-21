# Stoutland sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stoutland.py -- <the export's Stoutland.glb> <out.glb>
# Herdier grown (herdier.py and lillipup.py: the tufts, the brow, the moustache, the shaggy cape, the ears and lining,
# the paws, the nose, the eyes). From the references: a great long, low old dog; a brown head carried low and forward
# with a short snout and a black nose; a cream brow of pointed tufts over small scowling eyes; brown ears up and out
# with a navy lining; a broad cream moustache hanging to the chest, a brown tuft of beard under it; a great cream mane
# from the cheeks back over the neck and along the whole back like a cape, ragged at its edges, its end lifted in
# points behind the rump; a slate-blue shaggy coat over the long trunk, hanging nearly to the ground in points, navy
# on the chest; short thick brown legs with white claws.
# The pictures overrule the brief: the moustache hangs to the chest, not the ground; the cream that sweeps over the
# back is the mane from the cheeks, and the dark coat below it is slate blue with navy only on the chest.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, tri, unit, args, sibling

L = sibling("lillipup")
H_ = sibling("herdier")

BROWN, CREAM, SLATE, NAVY = "#A8653C", "#EEE0B8", "#4E6672", "#283A74"
NOSE, CLAW = "#18161A", "#F4F2EC"
WHITE, IRIS, PUPIL, INK = "#F2F0EA", "#3A3E4A", "#121216", "#2A1A12"

# The frame (joints: body (0, 0, 0.22), head (0, -0.16, 0.26), ears (+-0.05, -0.21, 0.34), legs (+-0.075, -0.12, 0.14),
# hinds (+-0.075, 0.14, 0.14), tail (0, 0.21, 0.24))
HC = np.array([0.0, -0.228, 0.318])       # the skull, carried low and forward
HR = np.array([0.086, 0.075, 0.076])
SC = HC + np.array((0, -0.062, -0.024))
SR = np.array((0.046, 0.04, 0.034))
LX, FY, HY = 0.075, -0.12, 0.14
BZ = 0.19
BROW = (0.12, 0.3)


def eye_top(u):
    return BROW[0] + BROW[1] * u


def mane(k, col):
    """The cream mane: from the back of the head over the neck and along the back, broad over the shoulders, its end
    lifted behind the rump; ragged points hang along its sides and fan out at its end."""
    path = [(-0.19, 0.345, 0.085, 0.06), (-0.11, 0.335, 0.115, 0.06), (-0.01, 0.315, 0.118, 0.055),
            (0.09, 0.305, 0.112, 0.052), (0.18, 0.31, 0.092, 0.046), (0.25, 0.33, 0.068, 0.038)]
    # One smooth mass along the back (a tube flattened top to bottom), its end on the tail's bone
    front = [(0, -0.175, 0.34), (0, -0.1, 0.332), (0, -0.01, 0.314), (0, 0.09, 0.306), (0, 0.18, 0.314)]
    k.add("mane", blade(front, [0.07, 0.115, 0.12, 0.112, 0.094], (0, 0, 1), 0.5), col, "body")
    rear = [(0, 0.15, 0.31), (0.01, 0.22, 0.325), (0.022, 0.27, 0.345), (0.03, 0.3, 0.36)]
    k.add("maneback", blade(rear, [0.1, 0.08, 0.06, 0.03], (0, 0, 1), 0.55), col, "tail")
    # Points hanging along its sides, out and down
    for i, (y, z, rx) in enumerate([(p[0], p[1], p[2]) for p in path[1:5]]):
        for j, dy in enumerate((-0.03, 0.02)):
            base = np.array((rx * 0.9, y + dy, z - 0.03))
            L.tuft(k, f"maneside{i}{j}", base, (0.75, 0.25, -1), 0.06 if j else 0.075, 0.032, col,
                   "tail" if y > 0.2 else "body", flat=(1, 0, 0.3), f=0.45)
            L.tuft(k, f"manesideL{i}{j}", base * np.array((-1, 1, 1)), (-0.75, 0.25, -1), 0.06 if j else 0.075, 0.032,
                   col, "tail" if y > 0.2 else "body", flat=(1, 0, -0.3), f=0.45)
    # Its end in points lifted behind the rump, swung a little to +X
    end = np.array((0.0, 0.27, 0.34))
    for i, (d, ln) in enumerate((((0.25, 1, 0.35), 0.09), ((0.5, 1, 0.0), 0.075), ((0.0, 1, 0.7), 0.07),
                                 ((-0.35, 1, 0.15), 0.065), ((0.3, 1, -0.4), 0.06))):
        L.tuft(k, f"maneend{i}", end, d, ln, 0.035, col, "tail", flat=(0, 0, 1), f=0.5)


def stoutland(src, out):
    k = Kit(src, coat="fur")

    # The long low trunk under its shaggy slate coat, hanging nearly to the ground
    k.add("chest", ell((0, -0.12, BZ + 0.01), (0.1, 0.09, 0.1)), SLATE, "body")
    k.add("rump", ell((0, 0.13, BZ), (0.098, 0.085, 0.098)), SLATE, "body")
    H_.cape(k, (0, 0.005, BZ + 0.01), (0.108, 0.215, 0.105), SLATE, n=26, drop=0.115, name="skirt")
    k.add("neck", tube([(0, -0.12, 0.24), (0, -0.17, 0.28), (0, -0.2, 0.3)], [0.07, 0.066, 0.06]), BROWN, "head")
    # Navy on the chest under the moustache
    k.paint(lambda P: inside(P[:, 1] + 0.15 - 0.012 * tri(P[:, 0] * 25), 0.003) * (P[:, 2] > 0.08) * (P[:, 2] < 0.3),
            NAVY, on=["chest", "skirt"])

    # The coat over the tops of the shoulders and thighs too, where they bulge through it
    k.paint(lambda P: inside(0.1 + 0.012 * tri(P[:, 1] * 30) - P[:, 2], 0.003) * (P[:, 1] > -0.17) * (P[:, 1] < 0.24)
            * (np.abs(P[:, 0]) > 0.04) * (P[:, 2] < 0.27), SLATE, on=["legtop", "hindtop", "leg", "hind"])
    # Short thick brown legs, only their stubs under the coat, wide paws with white claws
    PR = (0.036, 0.042, 0.022)
    pf = L.leg(k, LX, FY, 0.12, 0.034, PR, BROWN, "leg{s}", "leg", toes=False, shoulder=(0.048, 0.056, 0.06))
    ph = L.leg(k, LX, HY, 0.12, 0.034, PR, BROWN, "hind{s}", "hind", toes=False, shoulder=(0.05, 0.06, 0.065))
    for bone, pc in (("leg{s}", pf), ("hind{s}", ph)):
        for j, dx in enumerate((-0.018, 0.0, 0.018)):
            a = np.array((pc[0] + dx, pc[1] - PR[1] * 0.75, 0.012))
            k.add(f"claw{bone[0]}{j}", L.spike(a, (dx * 3, -1, -0.45), 0.026, 0.008, 0.006), CLAW, bone, fuse=False,
                  mirror=True, surface="shell")

    # The head: a round brown skull, a short snout
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("snout", ell(SC, SR), BROWN, "head")

    # Ears up, out and back, a navy lining
    EB, ET = np.array([0.052, -0.21, 0.36]), np.array([0.125, -0.165, 0.455])
    L.ear(k, EB, ET, 0.042, BROWN, flat=(0, 1, -0.3))
    L.ear_lining(k, EB + (0.004, 0, 0.01), ET, 0.026, NAVY, front=(0, -1, 0.3))

    # The cream brow over the eyes, the moustache to the chest, a brown tuft of beard under it
    H_.brow(k, HC + (0, -0.062, 0.022), [((0.35, 0.25, 1), 0.1, 0.03), ((-0.35, 0.25, 1), 0.1, 0.03),
                                          ((0.05, 0.3, 1), 0.08, 0.026), ((1, 0.15, 0.3), 0.09, 0.028),
                                          ((-1, 0.15, 0.3), 0.09, 0.028)], CREAM)
    H_.moustache(k, SC + (0, -0.03, -0.012), CREAM, width=0.07, drop=0.13, tufts=5, r=0.028)
    L.tuft(k, "beard", SC + (0, -0.034, -0.045), (0, -0.4, -1), 0.075, 0.032, BROWN, "head", flat=(0, 1, 0), f=0.5)
    # The cream of the cheeks running back into the mane
    for s in (1, -1):
        k.add(f"cheek{'R' if s > 0 else 'L'}", ell(HC + (s * 0.062, 0.03, -0.02), (0.035, 0.06, 0.05), rot=(20, 0, 0)),
              CREAM, "head")
    mane(k, CREAM)

    L.nose(k, SC + (0, -0.038, 0.012), (0.019, 0.013, 0.013), NOSE)

    k.build(voxel=0.0055, smooth=3, band=0.004)

    # Small scowling eyes under the brow
    ex, ez = 0.036, HC[2] + 0.004
    E = L.on_ell(HC, HR, ex, ez) + (0, -0.001, 0)
    n = unit(0.38, -1, 0.06)
    look = H_.terrier_eye(WHITE, IRIS, PUPIL, eye_top)
    out_ = H_.clipped(eye_top)
    k.eye_states(lambda s: L.lay_eyes(k, s, E, n, (0.02, 0.019), look, BROWN, INK, outline=out_))
    k.export(out)


if __name__ == "__main__":
    stoutland(*args())
