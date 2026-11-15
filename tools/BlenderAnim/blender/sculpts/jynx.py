# Jynx sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/jynx.py -- <the export's Jynx.glb> <out.glb>
#
# The design, from the references: a humanoid figure with no legs seen; a purple face, a tall egg, looking out of a
# great mass of long straight pale-blonde hair parted in the middle, which falls in front of the shoulders to about a
# third of the way up from the ground and covers the whole back; large round white eyes with small dark pupils, big
# pink lips pursed in an O; a red bodice of two round cups framed above by a gold band that dips between them; a red
# gown widening from the waist to the ground in soft folds; white puffy sleeves out from under the hair, carried
# forward and down here (the art holds them out wide); big purple hands with five spread fingers, palms forward.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

RED, GOLD, HAIR, PART = "#D8352E", "#F2C238", "#F2E4A6", "#C9B36A"
PURPLE, SLEEVE, WHITE, INKH = "#6E4FA8", "#F4F4F8", "#FBFBFB", "#1C1424"
LIPS, LIPDARK = "#F0A0B0", "#7A2440"

F = np.array([0, -0.04, 0.64])             # the face's middle
FR = np.array([0.13, 0.1, 0.15])
HC = np.array([0, 0.02, 0.68])             # the hair's crown
HCR = np.array([0.19, 0.155, 0.18])


def jynx(src, out):
    k = Kit(src, coat="fur")

    # The gown: a bell from the waist to the ground, flat on the ground, in soft folds widening toward the hem
    V, Fc = tube([(0, 0.0, 0.0), (0, 0.0, 0.07), (0, 0.0, 0.2), (0, 0.0, 0.4)], [0.31, 0.23, 0.155, 0.11])
    r = np.hypot(V[:, 0], V[:, 1]) + 1e-9
    phi = np.arctan2(V[:, 0], -V[:, 1])
    t = np.clip(1 - V[:, 2] / 0.4, 0, 1)
    s = 1 + 0.08 * t ** 1.5 * np.cos(6 * phi + 0.3)
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], 0.004)
    k.add("gown", (V, Fc), RED, "body")

    # The bodice: two round cups under a gold band
    k.add("bodice", ell((0, 0.0, 0.43), (0.125, 0.095, 0.08)), RED, "body")
    k.add("cup", ell((0.056, -0.062, 0.43), (0.06, 0.046, 0.052)), RED, "body", mirror=True)
    def cupline(x):
        a = np.abs(x)
        return 0.428 + 0.036 * np.sqrt(np.clip(1 - ((a - 0.056) / 0.064) ** 2, 0, 1))
    def gold(P):
        z0 = cupline(P[:, 0])
        return (inside(z0 - P[:, 2], 0.0025) * inside(P[:, 2] - (z0 + 0.028), 0.0025)
                * inside(np.abs(P[:, 0]) - 0.1, 0.0025) * (P[:, 1] < -0.01) * (P[:, 2] < 0.53))
    k.paint(gold, GOLD)

    # The face: a tall purple egg
    k.add("face", ell(F, FR), PURPLE, "head")

    # The hair: a crown over the head, opened at the front on the face; curtains falling in front of the shoulders;
    # a wide fall down the back
    def crown(U):
        x, y, z = U[:, 0], U[:, 1].copy(), U[:, 2]
        m = ((1 - smoothstep(0.3, 0.5, z)) * smoothstep(0.05, 0.45, -y) * (1 - smoothstep(0.62, 0.85, np.abs(x))))
        floor = -0.2
        y = y + m * (0.5 * (y - floor + np.sqrt((y - floor) ** 2 + 0.02)) + floor - y)
        return np.stack([x, y, z], axis=1)
    k.add("hair", ell(HC, HCR, deform=crown, sub=5), HAIR, "head")
    k.add("hairside", ell((0.165, -0.035, 0.46), (0.07, 0.075, 0.27), rot=(0, -6, 0)), HAIR, "head", mirror=True)
    k.add("hairback", ell((0, 0.095, 0.47), (0.225, 0.085, 0.3)), HAIR, "head")
    # the parting down the crown's middle
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.0035, 0.002) * (P[:, 2] > 0.79) * (P[:, 1] < 0.08), PART)

    # White puffy sleeves out from under the hair, carried forward and down; a puff at the wrist
    k.add("sleeve", tube([(0.11, 0.0, 0.52), (0.2, -0.04, 0.47), (0.265, -0.09, 0.43)], [0.048, 0.056, 0.05]),
          SLEEVE, "arm{s}", mirror=True)
    k.add("cuff", ell((0.278, -0.1, 0.425), (0.048, 0.048, 0.05)), SLEEVE, "arm{s}", mirror=True)

    # Big purple hands, palms forward, five fingers spread; loose so they keep their edge against the sleeve
    P0 = np.array([0.325, -0.14, 0.43])
    side, up = unit(0.85, -0.53, 0), np.array([0, 0, 1.0])
    k.add("palm", ell(P0, (0.045, 0.026, 0.048), rot=(0, 0, -32)), PURPLE, "arm{s}", fuse=False, mirror=True)
    for i, deg in enumerate((-55, 5, 32, 58, 85)):
        dd = math.cos(math.radians(deg)) * side + math.sin(math.radians(deg)) * up
        a = P0 + dd * 0.03 - np.array([0, 0.004, 0])
        b = P0 + dd * (0.08 if deg < 0 else 0.097)
        k.add(f"finger{i}", tube([a, b], [0.0145, 0.012], seg=12), PURPLE, "arm{s}", fuse=False, mirror=True)

    # Big pink lips pursed in an O
    CL, CD, CP = srgb(LIPS), srgb(LIPDARK), srgb(PURPLE)
    def lips(u, v):
        c = np.tile(CL, (len(u), 1))
        c[(u / 0.42) ** 2 + (v / 0.5) ** 2 < 1] = CD
        return c
    dm = unit(0, -1, -0.2)
    k.decal(np.array([0, -0.13, 0.565]), dm, (0.038, 0.045), lips, eps=0.005, rings=12, segs=56)

    k.build(voxel=0.0065, band=0.004)

    # Large round white eyes, small dark pupils, a dark rim
    CW, CI = srgb(WHITE), srgb(INKH)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[u * u + v * v > 0.74] = CI
        c[(u + 0.05) ** 2 + v ** 2 < 0.2 ** 2] = CI
        return c
    de = unit(0.4, -0.92, 0.08)
    def lay(state):
        k.decal(np.array([0.058, -0.12, 0.69]), de, (0.042, 0.045), eye_look(state, eye, CP, CI, line=0.16),
                eps=0.005, rings=20, segs=56, mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    jynx(*args())
