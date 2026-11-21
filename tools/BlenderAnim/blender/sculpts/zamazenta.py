# Zamazenta (Hero of Many Battles) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zamazenta.py -- <the export's Zamazenta.glb> <out.glb>
#
# Zacian's twin, built by zacian.py's functions (sibling("zacian")): the same trunk, legs, hooves, eyes and fur. From
# the references: a stockier deep red wolf, its coat in ragged darker red points along the back and down the flanks,
# black streaks among the points at the chest; white scars, a great cross on the flank and slashes on the shoulder.
# A red head held lower and forward, the muzzle and jaw white, a blue nose, a black band through the yellow eye and
# along the cheek; short red ears. Its largest mark is the great blue mane: a plume swept up from the brow and arching
# back over the neck, blue locks at the cheeks and a blue beard down the throat. Grey legs, blue fur at the back of
# each forearm and blue boots over the paws, black hooves; a blue brush of a tail. (The task's text spoke of a braided
# tail: the pictures' tail is a plain blue brush, and the mane a plume, not a ruff; no shield.)
# Bones: body (0, 0.02, 0.48); head (0, -0.16, 0.57); legs (+-0.085, -0.19, 0.42); hind legs (+-0.085, 0.2, 0.42);
# tail (0, 0.25, 0.52).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, outward, inside, unit, lumpy, args, sibling

Z = sibling("zacian")

C = dict(
    coat="#CC3646", deep="#962636", leg="#DCDCE6", hoof="#1C1A20", face="#F2F2F6", nose="#2C3E9E",
    mask="#1A1A22", blue="#2E4EC4", blue2="#22389C", streak="#2A1E26",
    eye="#F6CC3A", iris="#F2B22A", pupil="#16121A", line="#F2F2F6",
)

F = dict(Z.F)
F.update(
    x=0.085, bz=0.41, bw=0.122, deep=0.17,
    H=np.array([0.0, -0.38, 0.67]),             # held lower and forward than Zacian's
    HR=np.array([0.085, 0.095, 0.082]),
    eye_dir=unit(0.6, -0.74, 0.24), eye_r=0.026,
    neck=[(0, -0.19, 0.5), (0, -0.28, 0.57), (0, -0.35, 0.63)],
)


def add_plume(k, F, blue, blue2):
    """The great blue plume swept up from the brow and arching back over the neck: the design's largest mark."""
    H = F["H"]
    path = [H + (0, 0.05, 0.06), H + (0, 0.1, 0.15), H + (0, 0.2, 0.2), H + (0, 0.31, 0.17), H + (0, 0.39, 0.08),
            H + (0, 0.43, -0.01)]
    # Broad in the midline plane (a crescent from the side), flattened across: never a tube
    k.add("plume", blade(path, [0.07, 0.092, 0.095, 0.082, 0.06, 0.028], (1, 0, 0), 0.6), blue, "head")
    # Ragged points along its back edge and at its end
    for j, (t, d, ln) in enumerate(((2, (0.3, 0.5, 1), 0.07), (3, (-0.3, 0.9, 0.6), 0.075), (4, (0.3, 1, 0.1), 0.07),
                                    (5, (0, 0.6, -0.8), 0.06))):
        k.add(f"plumetip{j}", Z.tuft(path[t] + (0, 0.02, 0.01), d, ln, 0.034, 0.017), blue2 if j % 2 else blue, "head",
              fuse=False, surface="fur")
    # Blue locks back from the cheeks, and a blue beard down the throat
    for s in (1, -1):
        for j, (dy, dz, d, ln) in enumerate(((0.03, -0.02, (1, 0.6, -0.1), 0.085), (0.0, -0.06, (0.9, 0.5, -0.6), 0.08),
                                             (0.05, 0.03, (0.8, 0.8, 0.3), 0.07))):
            k.add(f"cheek{j}{'R' if s > 0 else 'L'}",
                  Z.tuft(H + (s * 0.07, dy, dz), (s * d[0], d[1], d[2]), ln, 0.034, 0.017),
                  blue if j % 2 == 0 else blue2, "head", fuse=False, surface="fur")
    k.add("beard", ell((0, -0.35, 0.53), (0.085, 0.065, 0.1), deform=lumpy(6, 3, 0.1)), blue, "head")
    for i, (x, ln) in enumerate(((0.0, 0.11), (0.05, 0.1), (-0.05, 0.095), (0.085, 0.08), (-0.085, 0.085))):
        k.add(f"beardtip{i}", Z.tuft((x, -0.37, 0.46), (x * 2, -0.25, -1), ln, 0.036, 0.018), blue2 if i % 2 else blue,
              "head", fuse=False, surface="fur")
    for s in (1, -1):
        for j, z in enumerate((0.58, 0.5)):
            k.add(f"beardside{j}{'R' if s > 0 else 'L'}", Z.tuft((s * 0.08, -0.34, z), (s, 0.3, -0.6), 0.075, 0.034, 0.017),
                  blue2 if j else blue, "head", fuse=False, surface="fur")


def add_boots(k, F, blue, blue2):
    """Blue fur over each paw and a blue tuft back from each forearm."""
    k.paint(lambda P: inside(P[:, 2] - 0.095, 0.003), blue, on=["uleg", "uhind", "pleg", "phind"])
    x = F["x"]
    for s, nm in ((1, "R"), (-1, "L")):
        for bone, y in (("leg", F["fy"]), ("hind", F["hy"])):
            k.add(f"boot{bone}{nm}", Z.tuft((s * (x + 0.02), y + 0.02, 0.09), (s * 0.7, 0.6, 0.35), 0.06, 0.03, 0.016),
                  blue2, bone + nm, fuse=False, surface="fur")
        k.add(f"forearm{nm}", Z.tuft((s * (x + 0.012), F["fy"] + 0.045, 0.2), (s * 0.4, 1, 0.5), 0.08, 0.034, 0.017),
              blue, "leg" + nm, fuse=False, surface="fur")


def add_streaks(k, F, color):
    """Black streaks among the red points down the chest."""
    for i, (x, y) in enumerate(((0.06, -0.27), (-0.06, -0.27), (0.1, -0.2), (-0.1, -0.2))):
        k.add(f"streak{i}", Z.tuft((x, y, 0.32), (x * 3, -0.2, -1), 0.07, 0.016, 0.014), color, "body", fuse=False,
              surface="fur")


def zamazenta(src, out):
    k = Kit(src, coat="fur")
    Z.add_trunk(k, F, C["coat"])
    Z.add_legs(k, F, C["coat"], C["leg"], C["hoof"], scale=1.08)
    Z.add_neck(k, F, C["coat"])
    Z.add_head(k, F, C["coat"], C["face"], C["nose"])
    add_plume(k, F, C["blue"], C["blue2"])
    Z.add_back_fur(k, F, (C["coat"], C["deep"]), z=0.56)
    Z.add_skirt(k, F, (C["deep"], C["coat"]))
    add_streaks(k, F, C["streak"])
    add_boots(k, F, C["blue"], C["blue2"])
    Z.add_scars(k, F, C["face"])
    Z.add_mask(k, F, C["mask"])
    Z.add_tail(k, C["blue"], C["blue2"], base=(0.0, 0.27, 0.51))
    k.build(voxel=0.0075, band=0.004)
    k.eye_states(lambda s: Z.eyes(k, F, s, C))
    k.export(out)


if __name__ == "__main__":
    zamazenta(*args())
