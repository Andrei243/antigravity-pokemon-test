# Lupodac (Carpathia No. 10056) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/lupodac.py -- <the export's Lupodac.glb> <out.glb>
# Lupling grown (lupling.py, whose spikes, ears, eyes, tail bands and colours it keeps). From its picture
# (docs/art/reference/gemini/carpathia/10056-Lupodac.jpg) and its brief (CP-dex-2-mountains.md, 56): a powerful grey
# wolf braced on four strong legs; a great shaggy dark mane, its largest mass, from behind the ears over the neck, the
# shoulders and down the chest; a long grey muzzle drawn back in a snarl over a row of white teeth, a black nose; the
# cheeks and the jaw pale, a pale bib under the chin in the dark of the chest; a dark brow scowling over burning amber
# eyes; tall pointed ears, grey with pale linings and dark tips; bands of bronze fur wrapped round the forelegs like a
# warrior's bracers; dark claws; a big bushy tail curling up behind, banded dark, pale and grey.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, outward, srgb, inside, tri, unit, args, sibling

lp = sibling("lupling")
spike = lp.spike

GREY, DARK, PALE, BELLY = "#8C8C94", "#2E2E36", lp.PALE, "#A9A7B6"
NOSE, GUM, TOOTH = lp.NOSE, "#3A1A20", "#F6F4EE"
BRONZE, BRONZE2, CLAW = "#B2743E", "#7A4A26", "#3C3A40"
AMBER = "#E8902A"

# The frame (joints: body (0, 0, 0.42), head (0, -0.25, 0.6), ears (+-0.045, -0.28, 0.69),
# legs (+-0.075, -0.15, 0.38), hinds (+-0.075, 0.17, 0.38), tail (0, 0.24, 0.5))
LX = 0.078
FY, HY = -0.15, 0.17
BZ = 0.39
HC = np.array([0.0, -0.345, 0.632])
HR = np.array([0.098, 0.1, 0.092])
BROW = (0.12, 0.5)              # a scowl: the inner corner low


def eye_top(u):
    return BROW[0] + BROW[1] * u


def lupodac(src, out_path):
    k = Kit(src, coat="fur")

    # --- a deep chest, a long trunk, the rump the thighs are part of ------------------------------------------
    k.add("chest", ell((0, -0.13, BZ + 0.02), (0.1, 0.11, 0.13)), GREY, "body")
    k.add("middle", ell((0, 0.02, BZ - 0.005), (0.09, 0.2, 0.118)), GREY, "body")
    k.add("rump", ell((0, 0.165, BZ + 0.01), (0.094, 0.1, 0.115)), GREY, "body")
    k.add("neck", tube([(0, -0.15, 0.47), (0, -0.24, 0.57), (0, -0.29, 0.61)], [0.088, 0.08, 0.075]), DARK, "head")

    # --- strong legs: broad shoulders and thighs over the upper half, thick tapering shins, wide paws ------------
    k.add("shoulder", ell((LX - 0.008, FY + 0.01, 0.3), (0.056, 0.066, 0.105)), GREY, "leg{s}", mirror=True)
    k.add("leg", tube([(LX, FY, 0.3), (LX + 0.004, FY - 0.006, 0.16), (LX + 0.006, FY - 0.01, 0.04)],
                      [0.05, 0.042, 0.039]), GREY, "leg{s}", mirror=True)
    k.add("paw", ell((LX + 0.006, FY - 0.03, 0.026), (0.046, 0.06, 0.028)), GREY, "leg{s}", mirror=True)
    k.add("thigh", ell((LX - 0.006, HY + 0.012, 0.31), (0.062, 0.088, 0.118)), GREY, "hind{s}", mirror=True)
    k.add("hind", tube([(LX, HY + 0.02, 0.27), (LX + 0.004, HY + 0.04, 0.15), (LX + 0.006, HY + 0.016, 0.04)],
                       [0.05, 0.041, 0.039]), GREY, "hind{s}", mirror=True)
    k.add("hpaw", ell((LX + 0.006, HY - 0.006, 0.026), (0.046, 0.06, 0.028)), GREY, "hind{s}", mirror=True)
    # the bronze bracers: a wrapped cuff of fur round each foreleg, its strips running slantwise
    k.add("cuff", tube([(LX + 0.0055, FY - 0.0085, 0.085), (LX + 0.005, FY - 0.007, 0.13), (LX + 0.0045, FY - 0.006, 0.18)],
                       [0.048, 0.05, 0.05]), BRONZE, "leg{s}", mirror=True)
    def strips(P):
        q = np.abs(P[:, 0]) - LX; r = P[:, 1] - FY
        ang = np.arctan2(r, q) / (2 * math.pi)
        return (tri(P[:, 2] * 26 + ang * 2) > 0.55) * (P[:, 2] > 0.06) * (P[:, 2] < 0.2)
    k.paint(strips, BRONZE2, on=["cuff"])
    for bone, y in (("leg{s}", FY - 0.03), ("hind{s}", HY - 0.006)):
        for i, dx in enumerate((-0.022, 0.0, 0.022)):
            a = np.array([LX + 0.006 + dx, y - 0.05, 0.016])
            k.add(f"claw{bone[:4]}{i}", spike(a + (0, 0.014, 0), (dx * 2, -1, -0.45), 0.034, 0.011, 0.008), CLAW, bone,
                  fuse=False, mirror=True, surface="shell")

    # --- the head: a long grey muzzle, a pale jaw, cheeks of pale ruff ------------------------------------------
    k.add("head", ell(HC, HR), GREY, "head")
    k.add("cheek", ell(HC + (0.055, -0.022, -0.055), (0.052, 0.058, 0.052)), PALE, "head", mirror=True)
    k.add("snout", ell(HC + (0, -0.108, -0.03), (0.05, 0.095, 0.044), rot=(5, 0, 0)), GREY, "head")
    k.add("jaw", ell(HC + (0, -0.092, -0.068), (0.045, 0.075, 0.03)), PALE, "head")
    k.add("nose", outward(ell(HC + (0, -0.2, -0.019), (0.023, 0.019, 0.018))), NOSE, "head", fuse=False,
          surface="skin")
    for i, (x, d) in enumerate(((0.07, (0.5, 0.6, -0.7)), (0.075, (0.55, 0.8, -0.3)))):
        k.add(f"ruff{i}", spike(HC + (x, 0.0 + 0.02 * i, -0.075 + 0.025 * i), d, 0.05, 0.024, 0.014), PALE, "head",
              fuse=False, mirror=True, surface="fur")

    # the pale jaw and cheeks, the bib down the throat; the dark brow over the eyes and up the crown
    def face(P):
        d = P - HC
        low = inside(d[:, 2] - (-0.03 + 0.25 * np.clip(-d[:, 1] - 0.04, 0, 0.1)), 0.003) * (d[:, 1] < 0.04)
        near = inside(np.linalg.norm(d / (HR * 1.5), axis=1) - 1, 0.01)
        return low * near * inside(P[:, 2] - (HC[2] + 0.0), 0.004)
    k.paint(face, PALE, on=["head", "cheek", "snout", "jaw"])
    k.paint(lambda P: inside(P[:, 1] - (-0.27 - 0.12 * np.clip(0.56 - P[:, 2], 0, None) + 0.01 * tri(P[:, 2] * 30)), 0.003)
            * inside(np.abs(P[:, 0]) - (0.05 - 0.15 * np.clip(0.55 - P[:, 2], 0, None)), 0.004)
            * (P[:, 2] > 0.4) * (P[:, 2] < 0.58), PALE, on=["neck", "chest", "mane", "head"])

    def brow(P):
        d = (P - HC) / HR
        mid = inside(np.abs(d[:, 0]) - (0.18 + 0.6 * np.clip(d[:, 2] - 0.25, 0, None)), 0.04)
        return mid * (d[:, 2] > 0.25) * (d[:, 1] < -0.2) * inside(np.linalg.norm(d, axis=1) - 1.15, 0.05)
    k.paint(brow, DARK, on=["head"])
    k.paint(lambda P: inside(-(P[:, 1] - (HC[1] + 0.02)), 0.006) * (P[:, 2] > HC[2] - 0.02), DARK, on=["head"])

    # --- the great dark mane: from behind the ears over the neck and shoulders and down the chest -------------
    k.add("mane", ell((0, -0.17, 0.53), (0.13, 0.14, 0.15)), DARK, "body")
    k.add("ruffle", ell((0, -0.21, 0.42), (0.11, 0.09, 0.13)), DARK, "body")
    k.add("withers", ell((0, -0.04, 0.5), (0.1, 0.12, 0.07)), DARK, "body")
    # laid back along the top, hanging down the sides and the chest
    for i, (y, z, ln) in enumerate(((-0.27, 0.69, 0.1), (-0.21, 0.67, 0.12), (-0.14, 0.645, 0.12), (-0.07, 0.6, 0.11),
                                    (0.0, 0.56, 0.09), (0.06, 0.53, 0.07))):
        for s in (-1, 0, 1):
            k.add(f"maneT{i}{s + 1}", spike((0.05 * s, y, z - 0.04), (0.45 * s, 0.85, 0.5 - 0.2 * abs(s)),
                                            ln - 0.02 * abs(s), 0.03, 0.02, side=(1, 0, 0) if s == 0 else None),
                  DARK, "body", fuse=False, surface="fur")
    for j, (y, z) in enumerate(((-0.24, 0.52), (-0.17, 0.47), (-0.1, 0.45), (-0.2, 0.39), (-0.13, 0.36))):
        k.add(f"maneS{j}", spike((0.11 + 0.01 * (j < 3), y, z), (0.55, 0.35, -1), 0.09, 0.03, 0.018), DARK, "body",
              fuse=False, mirror=True, surface="fur")
    for j, (x, d) in enumerate(((0.0, (0, -0.3, -1)), (0.05, (0.35, -0.25, -1)), (-0.05, (-0.35, -0.25, -1)))):
        k.add(f"maneC{j}", spike((x, -0.25, 0.33), d, 0.08, 0.03, 0.018), DARK, "body", fuse=False, surface="fur")
    # the dark running back from the mane over the back, the belly paler
    def back(P):
        y = P[:, 1]
        line = BZ + 0.075 + 0.012 * tri(y * 18) + 0.25 * np.clip(y - 0.12, 0, None)
        return inside(line - P[:, 2], 0.003) * (y < 0.25)
    k.paint(back, DARK, on=["middle", "rump", "chest"])
    k.paint(lambda P: inside(P[:, 2] - (BZ - 0.075), 0.006) * inside(np.abs(P[:, 0]) - 0.07, 0.01)
            * (P[:, 1] > -0.1) * (P[:, 1] < 0.22), BELLY, on=["chest", "middle", "rump"])

    # --- tall pointed ears ------------------------------------------------------------------------------------
    lp.ear(k, (0.055, -0.33, 0.7), (0.082, -0.315, 0.8), 0.046, unit(0.2, 1, 0.1), GREY, PALE, DARK, tip_from=0.74,
           lining_w=0.55)

    # --- the big bushy tail curling up behind, out to the model's right, banded ---------------------------------
    TAIL = [(0, 0.25, 0.5), (0.04, 0.33, 0.535), (0.088, 0.39, 0.6), (0.11, 0.405, 0.66), (0.104, 0.385, 0.71)]
    lp.banded_tail(k, TAIL, [0.046, 0.068, 0.068, 0.052, 0.02], DARK,
                   [(0.4, 0.55, GREY), (0.55, 0.64, PALE), (0.8, 1.1, GREY)],
                   [((0.1, -0.1, 1), 0.06, GREY), ((0.6, 0.3, 0.75), 0.055, GREY), ((-0.4, 0.4, 0.8), 0.05, GREY)],
                   0.06, 0.026, 0.017)

    # --- the snarl: lips drawn back along each side over a row of white teeth, a fang at each corner ----------
    CG, CT, CP = srgb(GUM), srgb(TOOTH), srgb(PALE)
    def snarl(u, v):
        c = np.tile(CG, (len(u), 1))
        teeth = (v > 0.3 - 0.75 * np.abs(tri(u * 1.8 + 0.25))) & (np.abs(u) < 0.9)
        low = (v < -0.35 + 0.6 * np.abs(tri(u * 1.8))) & (np.abs(u) < 0.8)
        c[teeth | low] = CT
        return c
    k.decal(HC + (0.036, -0.13, -0.056), unit(1, -0.6, -0.25), (0.056, 0.021), snarl, up=unit(0, 0.1, 1), eps=0.003,
            rings=12, segs=56, mirror=True)
    for s in (1, -1):
        a = HC + np.array((0.028 * s, -0.172, -0.052))
        k.add(f"fang{'R' if s > 0 else 'L'}", spike(a, (0.1 * s, -0.2, -1), 0.022, 0.009, 0.007, side=(1, 0, 0)),
              TOOTH, "head", fuse=False, surface="shell")

    k.build(voxel=0.0065, smooth=3, band=0.006)

    # --- the eyes: burning amber under the scowling dark brow --------------------------------------------------
    de = unit(0.6, -0.76, 0.17)
    E = lp.on_ell(HC, HR, de) + (0, 0, 0.004)
    out_ = lp.clipped(eye_top)
    look = lp.eye_open(AMBER, eye_top, pupil_r=0.36)

    def eyes(state):
        lp.lay_eyes(k, state, E, de, (0.025, 0.019), look, out_, GREY, PALE)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    lupodac(*args())
