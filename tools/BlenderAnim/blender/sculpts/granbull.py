# Granbull sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/granbull.py -- <the export's Granbull.glb> <out.glb>
#
# Grown from Snubbull's script (its scowling eye, its fangs, its nose): a heavy purple bulldog standing upright, broad
# in the hips, on thick legs with wide toed feet, a small pointed tail. Over a short neck a head that is mostly jaw: an
# upper face with small scowling eyes under black brows and a short snout with a black nose; under it the mouth held
# open, pink inside, two small teeth in the upper jaw; an enormous lower jaw thrust forward, its lip a thick roll, and
# from its corners two great white fangs rising past the snout to the brows. Big ears out at the top corners of the
# head, their upper edge rolled over. A black collar wide in front like a bib, a pale blue tag at it; black bands at
# the wrists; thick arms held forward and down with big fists. (The brief's "small ears": the pictures show them big.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, outward, ring_band, srgb, inside, unit, sibling, args)

sb = sibling("snubbull")

# --- colours ----------------------------------------------------------------------------------------------------------
PURPLE = "#D6A6DE"
MOUTH = "#E27484"
BLACK = "#2A262C"
TAG = "#9CC6DC"
WHITE, INK, NOSE = sb.WHITE, sb.INK, sb.NOSE

# --- the frame (the skeleton's joints: body (0, 0, 0.36), head (0, -0.04, 0.52), arms (+-0.13, -0.03, 0.46),
#     ears (+-0.07, -0.04, 0.68), legs (+-0.09, 0, 0.2)) ----------------------------------------------------------------
H = np.array([0, -0.08, 0.648])          # the upper head's middle (brow, eyes, snout)
HR = np.array([0.118, 0.104, 0.094])
SNOUT = np.array([0, -0.152, 0.648])
JAW = np.array([0, -0.155, 0.488])         # the great lower jaw
JAW_R = np.array([0.126, 0.092, 0.07])
CHEST = np.array([0, -0.005, 0.44])
CHEST_R = np.array([0.135, 0.11, 0.122])


def add_head(k):
    k.add("head", ell(H, HR), PURPLE, "head")
    k.add("snout", ell(SNOUT, (0.058, 0.046, 0.034)), PURPLE, "head")
    k.add("neck", ell((0, -0.05, 0.53), (0.1, 0.09, 0.075)), PURPLE, "head")
    # the mouth held open: a pink hollow between the snout and the lower jaw, the jaw's lip a thick roll in front
    k.add("mouth", ell((0, -0.13, 0.575), (0.092, 0.075, 0.06)), MOUTH, "head")
    k.add("jaw", ell(JAW, JAW_R), PURPLE, "head")
    lip = [(0.108, -0.13, 0.528), (0.085, -0.195, 0.548), (0.045, -0.228, 0.556), (0.0, -0.235, 0.558)]
    lip = lip + [(-x, y, z) for x, y, z in lip[::-1][1:]]
    k.add("lip", tube(lip, [0.03] * len(lip)), PURPLE, "head")


def add_ears(k):
    """Big flaps out at the top corners of the head, faces forward, the outer end raised, the top edge rolled over."""
    k.add("ear", ell((0.165, -0.03, 0.735), (0.09, 0.022, 0.062), rot=(0, -18, 0)), PURPLE, "ear{s}", mirror=True)
    k.add("earroll", tube([(0.09, -0.05, 0.775), (0.16, -0.055, 0.8), (0.235, -0.05, 0.79), (0.255, -0.046, 0.765)],
                          [0.016, 0.02, 0.02, 0.016]), PURPLE, "ear{s}", mirror=True)


def add_body(k):
    k.add("chest", ell(CHEST, CHEST_R), PURPLE, "body")
    k.add("hips", ell((0, 0.005, 0.29), (0.19, 0.15, 0.15)), PURPLE, "body")
    k.add("tail", tube([(0, 0.13, 0.25), (0, 0.18, 0.27), (0, 0.205, 0.3)], [0.032, 0.022, 0.012]), PURPLE, "body")


def add_legs(k):
    k.add("thigh", ell((0.1, 0.01, 0.2), (0.085, 0.085, 0.1)), PURPLE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.1, 0.0, 0.16), (0.105, -0.01, 0.09), (0.108, -0.02, 0.045)], [0.064, 0.06, 0.056]),
          PURPLE, "leg{s}", mirror=True)
    k.add("foot", ell((0.108, -0.05, 0.032), (0.066, 0.09, 0.034)), PURPLE, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.036, 0.0, 0.036)):
        k.add(f"toe{i}", ell((0.108 + dx, -0.122, 0.027), (0.022, 0.026, 0.026)), PURPLE, "leg{s}", mirror=True)


SHOULDER, ELBOW, WRIST = np.array([0.13, -0.03, 0.47]), np.array([0.19, -0.065, 0.425]), np.array([0.212, -0.135, 0.38])
FIST = np.array([0.216, -0.175, 0.37])


def add_arms(k):
    k.add("arm", tube([SHOULDER - (0.03, 0, 0), SHOULDER, ELBOW, WRIST], [0.056, 0.054, 0.05, 0.046]), PURPLE,
          "arm{s}", mirror=True)
    k.add("fist", ell(FIST, (0.054, 0.052, 0.05)), PURPLE, "arm{s}", mirror=True)
    for i, dz in enumerate((0.026, 0.0, -0.026)):
        k.add(f"knuckle{i}", ell(FIST + (0.004, -0.04, dz), (0.03, 0.02, 0.016)), PURPLE, "arm{s}", mirror=True)
    d = unit(*(WRIST - ELBOW))
    k.add("band", ring_band(WRIST - d * 0.012, d, 0.044, 0.034, thick=0.007), BLACK, "arm{s}", fuse=False,
          mirror=True, surface="leather")


def chest_radius(th, z, off=0.006):
    """The chest's surface distance from its upright axis at an angle (nought straight ahead) and a height."""
    a = np.sin(th) / CHEST_R[0]; b = np.cos(th) / CHEST_R[1]
    f = np.sqrt(np.clip(1 - ((z - CHEST[2]) / CHEST_R[2]) ** 2, 0.05, 1))
    return f / np.sqrt(a * a + b * b) + off


def add_collar(k, top=0.495, width=0.034, bib=0.1):
    """A black collar round the neck, dipping in front to a wide bib."""
    def at(u, v):
        th = math.pi + 2 * math.pi * u                     # its seam straight behind
        front = np.clip(np.cos(th), 0, 1) ** 1.5             # 1 straight ahead
        zt = top - 0.05 * front
        zz = zt - (width + bib * front) * v
        r = chest_radius(th, zz)
        return np.stack([CHEST[0] + r * np.sin(th), CHEST[1] - r * np.cos(th), zz], axis=-1)
    k.add("collar", outward(sheet(at, 97, 7, 0.008)), BLACK, "body", fuse=False, surface="leather")
    # the tag: a pale blue drop hanging at the collar's front, a little to the model's left
    th = 0.35
    z = top - 0.05 - (width + 0.1) * 0.6
    r = chest_radius(th, z, 0.016)
    c = np.array([-r * math.sin(th), CHEST[1] - r * math.cos(th), z])
    k.add("tag", ell(c, (0.016, 0.009, 0.022), rot=(0, 0, -20)), TAG, "body", fuse=False, surface="metal")


def add_face(k):
    sb.add_nose(k, at=SNOUT + np.array([0, -0.046, 0.016]), r=(0.022, 0.014, 0.014))
    # two small teeth hanging from the upper jaw
    for s in (1, -1):
        base = np.array([0.034 * s, -0.19, 0.622])
        k.add(f"tooth{'R' if s > 0 else 'L'}", sb.fang(base, base + (0, -0.002, -0.026), 0.008, (s, 0, 0)), WHITE,
              "head", fuse=False, surface="shell")
    # the two great fangs from the lower jaw's corners, up past the snout to the brows, a little out
    for s in (1, -1):
        base = np.array([0.088 * s, -0.212, 0.556])
        tip = base + np.array([0.02 * s, -0.012, 0.15])
        k.add(f"fang{'R' if s > 0 else 'L'}", sb.fang(base, tip, 0.032, (s, 0, 0)), WHITE, "head", fuse=False,
              surface="shell")


def lay_eyes(k, state):
    sb.lay_eyes(k, state, x=0.054, z=0.686, half=(0.03, 0.025), h=H, hr=HR, lid=PURPLE)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    add_ears(k)
    add_collar(k)
    add_face(k)
    k.build(voxel=0.0065, band=0.005)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
