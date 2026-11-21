# Boltund sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/boltund.py -- <the export's Boltund.glb> <out.glb>
#
# Yamper grown (its frame through sibling("yamper"): the ring eye, the ear and its lining, the tongue): a lean greyhound,
# deep in the chest and drawn in at the waist. Its head, neck and a mantle over the withers and shoulders are yellow,
# the muzzle, cheeks, throat and chest white, the rest of the body and the legs dark green. A long white muzzle with a
# dark nose, the mouth open with the tongue out; round pale green eyes; yellow ears lined dark green. The forelegs green
# down to a yellow band, white below it; the hind legs green with a yellow band at the hock and yellow feet. Yellow
# tufts sweep back off the withers; the tail is a long yellow streamer with green spikes at its root.
# Reference ratios (3D picture): head with muzzle 0.18 of the body's length, the head 0.2 of the height, the chest as
# deep as the foreleg showing under it, the tail 0.4 of the body's length.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, ring_band, srgb, inside, unit, sibling, args

y_ = sibling("yamper")
ring_eye, lay_eyes, ear, ear_lining, tongue, on_ell, normal_at = (y_.ring_eye, y_.lay_eyes, y_.ear, y_.ear_lining,
                                                                  y_.tongue, y_.on_ell, y_.normal_at)

YELLOW, WHITE, GREEN, DARK = "#F4CF2A", "#F6F4EE", "#4F6B4A", "#2E3E2C"
NOSE, MOUTH, TONGUE, INK = "#2A302A", "#4A1E22", "#E8707C", "#2A3A20"

HC, HR = np.array([0.0, -0.33, 0.5]), np.array([0.112, 0.118, 0.106])
VOXEL = 0.0058


def spike(base, d, length, w, t):
    base = np.array(base, float); d = unit(*d)
    side = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    face = np.cross(d, side)
    return hull([base + side * w, base - side * w, base + face * t, base - face * t,
                 base + d * length * 0.5 + side * w * 0.5, base + d * length * 0.5 - side * w * 0.5, base + d * length])


def boltund(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: a ready stance, the chest deep and low between the forelegs, the back sloping to lean hips ---
    k.add("chest", ell((0, -0.13, 0.29), (0.105, 0.13, 0.13)), GREEN, "body")
    k.add("waist", ell((0, 0.03, 0.315), (0.08, 0.13, 0.088)), GREEN, "body")
    k.add("rump", ell((0, 0.16, 0.3), (0.09, 0.095, 0.092)), GREEN, "body")

    # --- legs: short and thick, bent; forelegs forward (elbows back), hind legs set back (hocks well back) -----
    k.add("shoulder", ell((0.07, -0.16, 0.235), (0.068, 0.085, 0.13)), GREEN, "leg{s}", mirror=True)
    k.add("leg", tube([(0.072, -0.17, 0.235), (0.075, -0.13, 0.14), (0.077, -0.2, 0.045)], [0.07, 0.054, 0.046]),
          GREEN, "leg{s}", mirror=True)
    k.add("paw", ell((0.078, -0.225, 0.026), (0.05, 0.066, 0.028)), WHITE, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.105, 0.003) * (P[:, 1] < -0.06), WHITE, on=["leg", "paw"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.127) - 0.022, 0.003) * (P[:, 1] < -0.06), YELLOW, on=["leg"])

    k.add("thigh", ell((0.072, 0.19, 0.27), (0.078, 0.105, 0.14)), GREEN, "hind{s}", mirror=True)
    k.add("hind", tube([(0.073, 0.19, 0.255), (0.076, 0.18, 0.17), (0.077, 0.285, 0.095), (0.078, 0.25, 0.04)],
                       [0.07, 0.056, 0.047, 0.045]), GREEN, "hind{s}", mirror=True)
    k.add("hpaw", ell((0.079, 0.23, 0.026), (0.05, 0.066, 0.028)), YELLOW, "hind{s}", mirror=True)
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.105) - 0.02, 0.003) * (P[:, 1] > 0.08), YELLOW, on=["hind"])
    k.paint(lambda P: inside(P[:, 2] - 0.05, 0.003) * (P[:, 1] > 0.08), YELLOW, on=["hind", "hpaw"])

    # --- neck and head: a thick neck carrying the head forward, a long white muzzle -------------------------------
    k.add("neck", tube([(0, -0.15, 0.355), (0, -0.24, 0.425), (0, -0.3, 0.465)], [0.1, 0.092, 0.086]), YELLOW, "head")
    k.add("head", ell(HC, HR), YELLOW, "head")
    def taper(V):
        V = V.copy(); t = np.clip((-V[:, 1] + 1) / 2, 0, 1)
        V[:, 0] *= 1 - 0.28 * t; V[:, 2] *= 1 - 0.18 * t
        return V
    MC, MR = np.array((0, -0.455, 0.46)), np.array((0.062, 0.118, 0.054))
    k.add("muzzle", ell(MC, MR, deform=taper), WHITE, "head")
    k.add("nose", ell((0, -0.57, 0.482), (0.022, 0.016, 0.017)), NOSE, "head", fuse=False, surface="skin")

    # The yellow mantle over the withers and shoulders, the white throat and chest in front, the white cheeks
    k.paint(lambda P: inside(P[:, 1] - (0.0 + 0.8 * (P[:, 2] - 0.36)), 0.006) * inside(0.27 - P[:, 2], 0.006)
            * (P[:, 1] < 0.05), YELLOW, on=["chest", "waist", "shoulder"])
    k.paint(lambda P: inside(P[:, 1] - (-0.2 + 0.6 * (P[:, 2] - 0.335)), 0.004) * inside(np.abs(P[:, 0]) - 0.1, 0.006)
            * inside(P[:, 2] - 0.465, 0.004) * inside(0.175 - P[:, 2], 0.004), WHITE,
            on=["chest", "neck", "shoulder", "head"])
    def cheeks(P):
        p = P - HC
        line = -0.005 + 0.25 * np.clip(-(p[:, 1]), 0, 0.2)
        return inside(p[:, 2] - line, 0.003) * (p[:, 1] < 0.01) * (np.linalg.norm(p / (HR * 1.3), axis=1) < 1)
    k.paint(cheeks, WHITE, on=["head", "muzzle"])

    # The mouth open under the muzzle, the tongue out
    mp = on_ell(MC, MR * np.array((0.85, 1, 0.9)), 0.0, MC[2] - 0.036)
    k.decal(mp, unit(0, -0.6, -1), (0.044, 0.056), MOUTH, eps=0.003, rings=12, segs=56)
    k.add("tongue", tongue(mp + (0.012, 0.012, -0.008), (0.25, -0.55, -1), 0.075, 0.023), TONGUE, "head", fuse=False,
          surface="skin")

    # Ears: yellow points up and out, a little forward, lined dark green
    for s in (1, -1):
        b, t = (0.068 * s, -0.3, 0.585), (0.128 * s, -0.29, 0.68)
        ear(k, "ear" + ("R" if s > 0 else "L"), b, t, 0.042, YELLOW, (0.3 * s, 1, 0))
        ear_lining(k, b, t, 0.034, (0.15 * s, -1, 0), DARK, DARK, ["ear" + ("R" if s > 0 else "L")])

    # --- yellow tufts sweeping back off the withers, the tail a thick yellow streamer with green spikes ---------
    for i, (x, y, z, d) in enumerate(((0.03, -0.1, 0.425, (0.25, 1, 0.5)), (-0.035, -0.06, 0.415, (-0.3, 1, 0.45)),
                                      (0.05, -0.02, 0.405, (0.45, 1, 0.4)))):
        k.add(f"tuft{i}", spike((x, y, z), d, 0.1, 0.03, 0.02), YELLOW, "body", fuse=False)
    # back and out to +X, its broad face turned up and out so both game cameras see it
    T = [(0.0, 0.22, 0.345), (0.07, 0.32, 0.38), (0.16, 0.41, 0.405), (0.26, 0.49, 0.415)]
    k.add("tail", blade(T, [0.062, 0.052, 0.038, 0.016], unit(0.55, -0.45, 0.7), 0.5), YELLOW, "tail")
    for i, (p, d) in enumerate((((0.0, 0.22, 0.375), (0.15, 0.8, 0.75)), ((0.04, 0.24, 0.355), (0.7, 0.75, 0.35)),
                                ((-0.03, 0.23, 0.345), (-0.4, 0.85, 0.35)))):
        k.add(f"tailspike{i}", spike(p, d, 0.11, 0.028, 0.02), DARK if i == 0 else GREEN, "tail", fuse=False)

    k.build(voxel=VOXEL, smooth=4, band=0.005)

    # round pale green eyes on the yellow, high on the head's sides toward the front
    E = on_ell(HC, HR, 0.064, 0.525)
    en = unit(*(normal_at(HC, HR, E) + np.array((0, -0.4, 0))))
    k.eye_states(lambda s: lay_eyes(k, s, E, en, (0.028, 0.03), ring_eye(), YELLOW, INK))
    k.export(out)


if __name__ == "__main__":
    boltund(*args())
