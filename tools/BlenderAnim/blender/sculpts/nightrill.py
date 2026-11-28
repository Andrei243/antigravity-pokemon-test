# Nightrill (Carpathia No. 10011) sculpted from nothing on the skeleton and clips of the game's own export (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nightrill.py -- <the export's Nightrill.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10011-Nightrill.jpg): a slim nightingale carried tilted, chest
# up; warm russet-brown crown, back and folded wings with darker flight feathers; a cream throat warming to peach on the
# upper breast, the cream breast below speckled with dark dots; a pale ring round a round dark eye; a long thin grey
# beak open, orange inside; and its signature, a great rufous tail fanned up behind it like a fan of sheet music, each
# feather crossed by dark bands with a thin cream line between them (the stave). Thin dark legs, longer than Trillet's.
#
# Grown from trillet.py (sibling("trillet")): its eyes, open beak, banded feathers, speckles and feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, blade, srgb, inside, unit, args, sibling

t = sibling("trillet")
g = t.g

BROWN, DARK, CREAM, PEACH, BELLY = "#B8763F", "#7A4626", "#F3E3C0", "#EFB486", "#D2AE92"
RUF_A, RUF_B, BAND, STAVE = "#B5582E", "#A34C29", "#5C2C1A", "#EBCB9E"
RING, BEAK, MOUTH = "#F1E3C2", "#4B4648", "#F2A35E"

# --- the frame (skeleton: body (0, 0.02, 0.25), head (0, -0.06, 0.33), wings (+-0.08, -0.02, 0.3),
#     tail (0, 0.15, 0.25), legs (+-0.045, 0, 0.14); the export stands 0.44 tall) -----------------------------------
H = np.array([0, -0.072, 0.352]); HR = np.array([0.08, 0.079, 0.075])
BODY = ((0, 0.03, 0.222), (0.094, 0.135, 0.1))
CHEST = ((0, -0.045, 0.255), (0.084, 0.074, 0.096))
EYEDIR = (0.62, -0.7, 0.33)
VOXEL = 0.0042


def nightrill(src, out_path):
    k = Kit(src, coat="fur")
    k.add("body", ell(*BODY, rot=(-26, 0, 0)), BROWN, "body")
    k.add("chest", ell(*CHEST), BROWN, "body")
    k.add("rump", ell((0, 0.13, 0.245), (0.05, 0.05, 0.04)), BROWN, "tail")
    k.add("head", ell(H, HR), BROWN, "head")
    k.add("wing", blade([(0.07, -0.005, 0.31), (0.082, 0.055, 0.272), (0.077, 0.125, 0.225), (0.054, 0.195, 0.175)],
                        [0.032, 0.044, 0.034, 0.012], (1, 0, 0), 0.25), BROWN, "wing{s}", mirror=True)
    for j, (dz, ln) in enumerate(((0.0, 0.1), (-0.25, 0.088), (0.22, 0.08))):
        base = np.array([0.068, 0.13, 0.218 + dz * 0.05])
        tip = base + unit(-0.18, 1.0, -0.5 + dz) * ln
        k.add(f"primary{j}", g.spike(base, tip, 0.036, 0.013, side=(0.25, 0, 1)), DARK, "wing{s}", fuse=False,
              mirror=True)
    # the wing's lower half a shade darker
    k.paint(lambda P: inside(P[:, 2] - (0.262 - 0.32 * P[:, 1]), 0.004), "#99582F", on=["wing"])
    # cream throat and breast, peach across the upper breast, tan-pink under the tail
    k.paint(lambda P: inside((P[:, 0] / 0.07) ** 2 + ((P[:, 2] - 0.262) / 0.1) ** 2 - 1, 0.06)
            * inside(P[:, 1] + 0.03, 0.006) * inside(P[:, 2] - 0.335, 0.004), CREAM, on=["body", "chest", "head"])
    k.paint(lambda P: inside((P[:, 0] / 0.075) ** 2 + ((P[:, 2] - 0.3) / 0.045) ** 2 - 1, 0.25)
            * inside(P[:, 1] + 0.05, 0.01), PEACH, on=["chest", "body"])
    k.paint(lambda P: inside(P[:, 2] - 0.175, 0.01) * inside(P[:, 1] - 0.05, 0.02), BELLY, on=["body", "chest"])
    t.speckles(k, [(-0.04, 0.27), (-0.012, 0.275), (0.018, 0.265), (0.045, 0.255), (-0.055, 0.245),
                   (-0.025, 0.248), (0.005, 0.24), (0.032, 0.235), (-0.04, 0.22), (-0.01, 0.215), (0.02, 0.21),
                   (0.05, 0.225), (-0.02, 0.192), (0.01, 0.188)], [CHEST, BODY], 0.0048)
    # the pale ring round the eye: in every state, so laid before build()
    E = t.on_ell(H, HR, EYEDIR)
    D = unit(*EYEDIR)
    k.decal(E, D, (0.027, 0.026), RING, eps=0.004, rings=12, segs=48, mirror=True)
    # the great fan of sheet music
    fan = []
    for i in range(8):
        yaw = -58 + 116 * i / 7
        ln = 0.245 - 0.04 * (abs(yaw) / 58) ** 2
        fan.append((yaw, ln, 0.068, RUF_A if i % 2 else RUF_B,
                    [(0.42, 0.07, BAND), (0.6, 0.035, STAVE), (0.72, 0.07, BAND)]))
    t.fan(k, "tail", (0, 0.15, 0.248), fan, pitch=47, roll=0.35, lean=0, thick=0.0075)
    root = t.on_ell(H, HR, (0, -1, -0.05)) + np.array([0, 0.007, 0])
    t.beak(k, root, (0, -1, 0.12), (0, 0, 1), 0.068, 0.026, 0.013, 26, colour=BEAK, mouth=MOUTH)
    t.legs(k, 0.042, 0.01, 0.16, 0.012, (0.0078, 0.0068), 0.04, 0.0058)
    k.build(voxel=VOXEL, band=0.005, wband=0.035)
    k.eye_states(t.eyes(k, H, HR, EYEDIR, (0.019, 0.019), t.eye_ringed("#140F0C", "#33291F", "#FFFFFF"),
                        RING, "#3A2414", eps=0.008))
    k.export(out_path)


if __name__ == "__main__":
    nightrill(*args())
