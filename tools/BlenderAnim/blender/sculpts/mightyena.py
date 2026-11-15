# Mightyena sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mightyena.py -- <the export's Mightyena.glb> <out.glb>
# Poochyena grown (poochyena.py, whose spikes, socks, ear linings and eyes it borrows). From the references: a hyena on
# four strong legs, pale grey below and black above: a long pointed grey muzzle with a red nose and a fang at each
# corner; a black stripe from the eye back along the side of the head under a grey brow; red eyes with a thin yellow
# rim, cut by a scowling brow; tall pointed ears, grey outside and black inside; a shaggy black mane from the back of
# the head down the neck and over the back, ragged along its lower edge over the grey chest and flanks; black legs
# below the elbow with jagged tops and white claws; a big bushy black tail raised behind.
# The pictures overrule the brief: the chest, belly and upper legs are pale grey, the black is the mane, legs and tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, tri, unit, blade, args, sibling

p_ = sibling("poochyena")

GREY, BLACK, NOSE, FANG, CLAW = "#B4B8C0", "#2E3038", "#D8404C", p_.FANG, "#F2F2EE"
SCLERA, IRIS, PUPIL, MOUTH = p_.SCLERA, "#D42A34", p_.PUPIL, p_.MOUTH
INK, LIGHT = p_.INK, "#9EA2AA"

# The frame (joints: body (0, 0, 0.34), head (0, -0.14, 0.4), ears (+-0.045, -0.18, 0.5), legs (+-0.065, -0.12, 0.27),
# hinds (+-0.065, 0.14, 0.27), tail (0, 0.18, 0.35))
HC = np.array([0.0, -0.2, 0.46])
HR = np.array([0.08, 0.083, 0.073])
LX = 0.066
FY, HY = -0.12, 0.14
BZ = 0.34
BROW = (0.2, 0.42)


def eye_top(u):
    return BROW[0] + BROW[1] * u


def mightyena(src, out):
    k = Kit(src, coat="fur")

    # A deep chest, a long trunk, a rump the thighs are part of
    k.add("chest", ell((0, -0.1, BZ + 0.005), (0.088, 0.09, 0.112)), GREY, "body")
    k.add("middle", ell((0, 0.02, BZ), (0.084, 0.17, 0.104)), GREY, "body")
    k.add("rump", ell((0, 0.13, BZ + 0.008), (0.086, 0.08, 0.104)), GREY, "body")
    k.add("neck", tube([(0, -0.1, 0.38), (0, -0.15, 0.43), (0, -0.18, 0.46)], [0.07, 0.064, 0.06]), GREY, "head")

    # Strong legs: a broad shoulder and thigh fused over the upper half, a tapering shin, a wide paw
    k.add("shoulder", ell((LX - 0.006, FY + 0.006, 0.27), (0.052, 0.062, 0.1)), GREY, "leg{s}", mirror=True)
    k.add("leg", tube([(LX, FY, 0.25), (LX + 0.004, FY - 0.004, 0.14), (LX + 0.006, FY - 0.006, 0.035)],
                      [0.045, 0.036, 0.033]), GREY, "leg{s}", mirror=True)
    k.add("paw", ell((LX + 0.006, FY - 0.022, 0.024), (0.038, 0.05, 0.025)), BLACK, "leg{s}", mirror=True)
    k.add("thigh", ell((LX - 0.006, HY + 0.01, 0.28), (0.056, 0.078, 0.11)), GREY, "hind{s}", mirror=True)
    k.add("hind", tube([(LX, HY + 0.02, 0.24), (LX + 0.004, HY + 0.035, 0.14), (LX + 0.006, HY + 0.012, 0.035)],
                       [0.045, 0.036, 0.033]), GREY, "hind{s}", mirror=True)
    k.add("hpaw", ell((LX + 0.006, HY - 0.008, 0.024), (0.038, 0.05, 0.025)), BLACK, "hind{s}", mirror=True)
    p_.socks(k, [(LX + 0.004, FY - 0.004, 0.15), (LX + 0.004, HY + 0.03, 0.15)], 0.15, 0.022, 5, BLACK,
             on=["leg", "paw", "hind", "hpaw", "shoulder", "thigh"])
    for bone, y in (("leg{s}", FY - 0.022), ("hind{s}", HY - 0.008)):
        p_.claws(k, (LX + 0.006, y), y - 0.05, 0.018, 0.024, CLAW, bone)

    # The head: a long pointed grey muzzle, a jaw under it
    k.add("head", ell(HC, HR), GREY, "head")
    k.add("snout", ell(HC + (0, -0.095, -0.026), (0.042, 0.075, 0.036), rot=(6, 0, 0)), GREY, "head")
    k.add("jaw", ell(HC + (0, -0.075, -0.054), (0.042, 0.062, 0.027)), GREY, "head")
    # Tall pointed ears, grey outside, black inside
    EB, ET = np.array([0.042, -0.19, 0.51]), np.array([0.062, -0.17, 0.63])
    k.add("ear", blade([EB, (EB + ET) / 2, ET], [0.04, 0.026, 0.004], (0, 1, 0), 0.4), GREY, "ear{s}", mirror=True)
    p_.ear_lining(k, EB + (0, 0, 0.01), ET, 0.024, INK)

    # The black stripe from the eye back along the side of the head into the mane, under a grey brow
    def stripe(P):
        d = (P - HC) / HR
        mid = 0.24 + 0.4 * (d[:, 1] + 0.7)
        band = inside(np.abs(d[:, 2] - mid) - 0.28, 0.04) * (d[:, 1] > -0.72) * (np.abs(d[:, 0]) > 0.3)
        crown = inside(0.45 - d[:, 2], 0.04) * inside(-0.15 - d[:, 1], 0.05) * inside(np.abs(d[:, 0]) - 0.75, 0.05)
        near = inside(np.linalg.norm(d, axis=1) - 1.12, 0.05)
        return np.maximum(band, crown) * (P[:, 1] < HC[1] + 0.12) * near
    k.paint(stripe, BLACK)

    # The mane: black over the back of the head, down the neck and over the back, ragged along its lower edge
    def mane(P):
        y = P[:, 1]
        line = BZ + 0.03 + 0.03 * tri(y * 16) - 0.25 * np.clip(-0.05 - y, 0, None)
        back = inside(line - P[:, 2], 0.004) * (y > -0.2)
        head = inside(-(P[:, 1] - (HC[1] + 0.05)), 0.006) * (P[:, 2] > HC[2] - 0.04)
        return np.maximum(back, head) * (y > HC[1] + 0.02)
    k.paint(mane, BLACK, on=["chest", "middle", "rump", "neck", "head", "shoulder", "thigh"])
    # Shaggy spikes along the mane: laid back over the neck and the back, hanging down its sides
    for i, (y, z, ln) in enumerate(((-0.17, 0.53, 0.09), (-0.12, 0.49, 0.1), (-0.06, 0.45, 0.1), (0.0, 0.445, 0.09),
                                    (0.06, 0.44, 0.08), (0.12, 0.44, 0.07))):
        for s in (-1, 0, 1):
            k.add(f"mane{i}{s + 1}", p_.spike((0.03 * s, y, z - 0.03), (0.4 * s, 0.85, 0.45 - 0.15 * abs(s)),
                                              ln - 0.015 * abs(s), 0.024, 0.017, side=(1, 0, 0) if s == 0 else None),
                  BLACK, "body" if y > -0.1 else "head", fuse=False, surface="fur")
    for j, (y, z) in enumerate(((-0.13, 0.41), (-0.06, 0.37), (0.02, 0.36), (0.1, 0.36), (0.17, 0.37))):
        base = (0.082, y, z)
        k.add(f"fringe{j}", p_.spike(base, (0.45, 0.4, -1), 0.065, 0.024, 0.015), BLACK, "body", fuse=False,
              mirror=True, surface="fur")

    # The big bushy black tail, back and up, swung out to the model's right, spiky at its end
    TAIL = [(0, 0.2, 0.37), (0.03, 0.26, 0.4), (0.07, 0.31, 0.46), (0.1, 0.34, 0.52)]
    k.add("tail", tube(TAIL, [0.04, 0.058, 0.055, 0.03], seg=20), BLACK, "tail")
    tip = np.array(TAIL[-1])
    for i, (d, ln) in enumerate((((0.3, 0.5, 0.8), 0.08), ((0.55, 0.75, 0.3), 0.07), ((0.1, 0.9, 0.3), 0.07),
                                  ((0.5, 0.2, 0.8), 0.06))):
        k.add(f"tailtip{i}", p_.spike(tip - unit(*d) * 0.02, d, ln + 0.02, 0.026, 0.018), BLACK, "tail", fuse=False,
              surface="fur")
    for i, t in enumerate((0.3, 0.6)):
        c = np.array(TAIL[1]) * (1 - t) + np.array(TAIL[2]) * t
        k.add(f"tailtuft{i}", p_.spike(c + (0, 0.03, -0.03), (0.3, 1, -0.6), 0.07, 0.024, 0.016), BLACK, "tail",
              fuse=False, surface="fur")

    # The red nose, a dark open mouth and a fang at each corner
    k.add("nose", ell(HC + (0, -0.168, -0.014), (0.017, 0.014, 0.014)), NOSE, "head", fuse=False, surface="skin")
    CG, CM = srgb(GREY), srgb(MOUTH)
    def mouth(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(u) < 0.9) & (v < 0.2 - 0.2 * u * u) & (v > -0.5 + 0.35 * u * u)] = CM
        return c
    k.decal(HC + (0, -0.142, -0.058), unit(0, -1, -0.25), (0.032, 0.016), mouth, eps=0.003, rings=12, segs=48)
    for s in (1, -1):
        a = HC + np.array((0.022 * s, -0.138, -0.052))
        k.add(f"fang{'R' if s > 0 else 'L'}", p_.spike(a, (0.15 * s, -0.25, -1), 0.022, 0.007, 0.006, side=(1, 0, 0)),
              FANG, "head", fuse=False, surface="shell")

    k.build(voxel=0.0065, smooth=3, band=0.006)

    # The eyes: red, a thin yellow rim, scowling
    de = unit(0.6, -0.76, 0.22)
    E = HC + de / np.linalg.norm(de / HR) + (0, 0, 0.004)
    out_ = p_.clipped(eye_top)
    look = p_.eye_open(SCLERA, IRIS, PUPIL, eye_top, iris_c=(-0.08, -0.06), iris_r=(0.78, 0.86), pupil_r=0.36)
    def eyes(state):
        p_.lay_eyes(k, state, E, de, (0.027, 0.023), look, out_, BLACK, LIGHT)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    mightyena(*args())
