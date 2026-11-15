# Smoochum sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/smoochum.py -- <the export's Smoochum.glb> <out.glb>
# From the references (Jynx's baby: its lips and its hair, as jynx.py makes them, grown down to a child): a big head,
# a round pink face looking out of a blonde bob cut blunt across the brow with a small pink peak in the middle, the
# sides falling straight to the shoulders; a sprig of five leaf-like locks standing out of the crown; large round
# white eyes with teal irises looking up and out; round pink lips pushed forward with a small dark O; a cream top
# with short cream arms whose hands rest on the hips; a round pink belly under it; two short cream feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

SKIN, HAIR, CREAM = "#E2649E", "#F2DC6C", "#F4E8B4"
LIPS, LIPDARK = "#F2A6B0", "#B8445E"
WHITE, INK, IRIS, PUPIL = "#FBFBFB", "#1C1424", "#2A9AA4", "#14262C"

# --- the frame (the skeleton's joints: body (0, 0, 0.2), head (0, 0, 0.27), arms (+-0.08, 0, 0.22),
#     legs (+-0.05, 0, 0.1)) ------------------------------------------------------------------------------------------
F = np.array([0, -0.014, 0.335])            # the face's middle
FR = np.array([0.108, 0.094, 0.1])
HC = np.array([0, 0.014, 0.37])             # the hair's middle
HCR = np.array([0.138, 0.124, 0.122])
FRINGE = 0.378                              # the fringe's straight cut across the brow


def smoochum(src, out):
    k = Kit(src, coat="fur")

    # The body: a round pink belly, a cream top over the chest
    k.add("belly", ell((0, 0.0, 0.125), (0.086, 0.078, 0.078)), SKIN, "body")
    k.add("top", ell((0, 0.004, 0.205), (0.1, 0.086, 0.06), deform=boxy(2.2)), CREAM, "body")
    # the top's lower edge: two shallow scallops meeting a little lower in the middle; pink below it, crisp
    def hem(P):
        x = np.abs(P[:, 0])
        return 0.166 - 0.012 * np.cos(np.clip(x / 0.09, 0, 1) * math.pi * 0.5) + 0.006 * np.sin(x / 0.09 * math.pi)
    k.paint(lambda P: inside(P[:, 2] - hem(P), 0.0025) * (P[:, 2] < 0.2), SKIN, on=["belly", "top"])
    k.paint(lambda P: inside(hem(P) - P[:, 2], 0.0025) * (P[:, 2] > 0.13) * (P[:, 2] < 0.26), CREAM)

    # The legs: short cream stubs on rounded feet
    k.add("leg", tube([(0.05, 0.0, 0.095), (0.054, -0.008, 0.04)], [0.032, 0.03]), CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.056, -0.022, 0.026), (0.036, 0.046, 0.027)), CREAM, "leg{s}", mirror=True)

    # The arms: short cream stubs, elbows out a little, the hands resting on the hips
    k.add("arm", tube([(0.08, 0.0, 0.215), (0.122, -0.012, 0.19), (0.118, -0.016, 0.152)], [0.032, 0.03, 0.028]),
          CREAM, "arm{s}", mirror=True)
    k.add("hand", ell((0.11, -0.016, 0.146), (0.03, 0.03, 0.028)), CREAM, "arm{s}", mirror=True)

    # The face: a round pink ball under the hair, its cheeks full
    k.add("face", ell(F, FR, deform=boxy(2.1)), SKIN, "head")
    k.add("neck", ell((0, 0.0, 0.25), (0.06, 0.055, 0.04)), SKIN, "head")

    # The hair: a bob over the head, the front lower half drawn back behind the face (a window for it), the sides
    # flaring a little toward their straight cut at the jaw
    def bob(U):
        x, y, z = U[:, 0].copy(), U[:, 1].copy(), U[:, 2].copy()
        flare = 1 + 0.1 * np.clip(-z, 0, 1)
        x *= flare; y *= flare
        m = ((1 - smoothstep(0.08, 0.22, z)) * smoothstep(0.0, 0.35, -y) * (1 - smoothstep(0.68, 0.84, np.abs(x))))
        floor = 0.05
        y = y + m * (floor - y)
        z = np.maximum(z, -0.82)                    # the cut at the jaw: flat underneath
        return np.stack([x, y, z], axis=1)
    k.add("hair", ell(HC, HCR, deform=bob, sub=5), HAIR, "head")
    # the fringe: a blunt brim of hair over the brow
    k.add("fringe", ell((0, -0.07, FRINGE + 0.03), (0.11, 0.055, 0.04), deform=boxy(2.4)), HAIR, "head")
    # side locks down beside the cheeks to the jaw
    k.add("lock", ell((0.12, -0.022, 0.305), (0.044, 0.066, 0.07), deform=boxy(2.3)), HAIR, "head", mirror=True)
    # the colour line: pink under the fringe's straight cut and inside the side locks, a small peak in the middle
    def facemask(P):
        x = np.abs(P[:, 0])
        cut = FRINGE + 0.024 * np.clip(1 - x / 0.018, 0, 1)         # the peak, up into the fringe
        return (inside(P[:, 2] - cut, 0.0025) * inside(x - 0.1, 0.0025) * (P[:, 1] < -0.035)
                * (P[:, 2] > 0.25))
    k.paint(facemask, SKIN)

    # The sprig on the crown: five leaf-like locks, the outer ones leaning out, all laid a little back
    for name, d, ln, w in (("sprigmid", (0.0, 0.25, 1.0), 0.1, 0.034), ("sprigR1", (0.32, 0.25, 1.0), 0.09, 0.032),
                           ("sprigL1", (-0.32, 0.25, 1.0), 0.09, 0.032), ("sprigR2", (0.9, 0.3, 0.6), 0.1, 0.032),
                           ("sprigL2", (-0.9, 0.3, 0.6), 0.1, 0.032)):
        d = unit(*d)
        base = np.array([0.0, 0.02, HC[2] + HCR[2] - 0.02]) + np.array([d[0], 0, 0]) * 0.012
        pts = [base, base + d * ln * 0.45, base + d * ln * 0.85, base + d * ln]
        side = np.cross(d, (1.0, 0, 0)) if abs(d[0]) < 0.5 else np.cross(d, (0, 1.0, 0))
        k.add(name, blade(pts, [w * 0.8, w, w * 0.7, w * 0.2], side, 0.45), HAIR, "head")

    # The lips: round, pushed forward, with a small dark O
    k.add("lips", ell((0, -0.098, 0.3), (0.036, 0.03, 0.03)), LIPS, "head")
    CL, CD = srgb(LIPS), srgb(LIPDARK)
    def lipo(u, v):
        c = np.tile(CL, (len(u), 1))
        c[(u / 0.34) ** 2 + ((v + 0.12) / 0.4) ** 2 < 1] = CD
        return c
    k.decal(np.array([0, -0.128, 0.298]), unit(0, -1, -0.12), (0.026, 0.024), lipo, eps=0.004, rings=12, segs=56)

    k.build(voxel=0.005, band=0.004)

    # The eyes: large round whites with a dark rim, teal irises looking up and out, a dark pupil and a glint
    CS, CW, CI, CIR, CPU = srgb(SKIN), srgb(WHITE), srgb(INK), srgb(IRIS), srgb(PUPIL)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[u * u + v * v < 0.8] = CW
        c[((u - 0.26) / 0.5) ** 2 + ((v - 0.16) / 0.5) ** 2 < 1] = CIR
        c[((u - 0.3) / 0.25) ** 2 + ((v - 0.2) / 0.25) ** 2 < 1] = CPU
        c[((u - 0.15) / 0.13) ** 2 + ((v - 0.34) / 0.13) ** 2 < 1] = CW
        c[u * u + v * v >= 0.96] = CI
        return c
    de = unit(0.42, -0.9, 0.12)
    E = np.array([0.044, -0.088, 0.345])
    def lay(state):
        k.decal(E, de, (0.034, 0.036), eye_look(state, eye, CS, CI, line=0.16), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    smoochum(*args())
