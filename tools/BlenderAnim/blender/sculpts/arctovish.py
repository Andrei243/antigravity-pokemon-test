# Arctovish sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arctovish.py -- <the export's Arctovish.glb> <out.glb>
#
# From the references: Dracovish's fish head (dracovish.py's `fish_head`, `fish_eyes`) set upside down on Arctozolt's
# frozen sea beast (arctozolt.py's `ice_body`, `flipper`, `tail_fin`), drawn hovering. The head is by far the largest
# mass, four fifths of the whole height: an upright pale blue egg, wide at the top and narrowing to its foot, leaning
# back a little. On its front a great dark blue oval (its mouth) under a wavy white band; small white eyes with a level
# black bar high on the front of its sides; a dark blue zigzag down its crown from the front. Two white fins hang from
# low on its front sides. Behind it the small blue body under a ruff of snow, two blue flippers hanging, their tips
# white with blue spots, and a dark blue fin like a shark's rising behind on the tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, tri, args, sibling

dv = sibling("dracovish")
az = sibling("arctozolt")

PALE = "#A9D4F4"
MOUTH = "#3E6EB8"
BAND = "#E8F4FB"
ZIG = "#3A6AB2"

HEAD_C = np.array([0.0, -0.05, 0.335])
HEAD_G = 1.12
PITCH = 72          # the snout down: the head stands upside down, wide at the top


def arctovish(src, out):
    k = Kit(src, coat="scales")

    # --- the frozen beast behind: body under a ruff of snow, flippers, tail ----------------------------------------
    az.ice_body(k, (0.0, 0.11, 0.22), (0.115, 0.13, 0.095), 0.225, lobes=8, lobe=0.045)
    az.flipper(k, (0.08, 0.08, 0.17), (0.19, 0.17, 0.075), 0.035, "body", spots=False)
    k.add("tail", tube([(0, 0.2, 0.2), (0, 0.27, 0.18), (0, 0.33, 0.165)], [0.06, 0.042, 0.026]), az.BLUE, "tail")
    az.tail_fin(k, (0.0, 0.3, 0.17), h=0.15)

    # --- the great head, upside down ------------------------------------------------------------------------------
    W, local, SC, SR, R = dv.fish_head(k, HEAD_C, G=HEAD_G, pitch=PITCH, body=PALE, top=None, rim=None,
                                       open_mouth=False, head="head", jaw="head")

    # the mouth: a great dark blue oval on the front, under a wavy white band
    def front(cx, cz, ax, bz, wave=0.0):
        def m(P):
            x, z = P[:, 0], P[:, 2]
            zz = z - cz - wave * np.cos(x / ax * 5 * math.pi / 2) * (z > cz)
            return inside(np.hypot(x / ax, zz / bz) - 1, 0.004) * inside(P[:, 1] - (HEAD_C[1] - 0.02), 0.01)
        return m
    k.paint(front(0.0, 0.37, 0.14, 0.095, wave=0.012), BAND, on=["fhead", "fsnout", "fjaw"])
    k.paint(front(0.0, 0.345, 0.13, 0.088), MOUTH, on=["fhead", "fsnout", "fjaw"])

    # the zigzag down the crown from the front
    def zig(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        cx = 0.022 * tri((y + 0.02) / 0.07)
        return inside(np.abs(x - cx) - 0.013, 0.003) * (1 - inside(z - 0.5, 0.008)) * \
            inside(y - 0.09, 0.005) * (1 - inside(y + 0.13, 0.005))
    k.paint(zig, ZIG, on=["fhead", "fsnout", "fjaw"])

    # two white fins hanging from low on the head's front sides
    FO = [(0.0, -0.03), (0.07, -0.04), (0.13, -0.02), (0.16, 0.005), (0.12, 0.035), (0.05, 0.04), (0.0, 0.03)]
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        k.add(f"jawfin{sd}", slab(FO, (0.1 * s, -0.1, 0.2), unit(0.3 * s, -0.25, -1), unit(s, 0.25, 0.1), 0.022),
              dv.FINWHITE, f"fin{sd}", fuse=False, surface="skin")

    # the eyes high on the front of the sides, a level bar
    dw = unit(0.6, -0.5, 0.66)
    lay = dv.fish_eyes(k, W, SC, SR, R, G=HEAD_G, lid=PALE, bar="level", d=tuple(R.T @ dw), half=0.03)

    k.build(voxel=0.006, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    arctovish(*args())
