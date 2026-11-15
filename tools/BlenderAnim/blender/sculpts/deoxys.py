# Deoxys (its Normal Forme) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/deoxys.py -- <the export's Deoxys.glb> <out.glb>
#
# From the references: a tall orange alien. A head like a helmet drawn up into a point, with a broad flange standing
# out square on each side (a teal slot along each flange's front) and a teal stripe running down the middle of the
# face from the point to the chin; two narrow white eyes with black pupils, slanted, either side of the stripe. A
# short neck; a rounded chest with small round shoulders and a purple crystal set in a dark ring in the middle of the
# chest; a grey ribbed waist like a spine; round hips. The legs are long pointed blades, their tops rising in points
# beside the hips and their feet in points on the ground, each with a long teal slot down its front. From each
# shoulder two tentacles, one orange and one teal, twisted round each other like DNA, hang forward and down to the
# knees (the art flings them out; here they hang, so the model stays narrow).
# Bones: root 0; body (0, 0, 0.5); head (0, -0.01, 0.66); armR (0.09, 0, 0.62); legR (0.06, 0, 0.42).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, srgb, inside, unit, eye_look, polygon, args, outward, hull, slab, ring_band,
                       lathe)

ORANGE, TEAL, GREY, DARK = "#E5774F", "#45B8D4", "#7A7884", "#4A4552"
CRYSTAL, CRYSTAL_GLOW = "#A979DA", "#B88CF2"
INKH = "#141018"

HC, HR = np.array([0.0, -0.012, 0.732]), np.array([0.054, 0.05, 0.078])


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def helmet(V):
    """The head: drawn up into a point at the top, a little narrower at the chin."""
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    s = 1 - 0.72 * t ** 1.5
    b = 1 - 0.2 * np.clip(-V[:, 2], 0, 1) ** 2
    V[:, 0] *= s * b; V[:, 1] *= s * b
    V[:, 1] += 0.12 * t ** 2
    return V


def wider_top(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.18 * V[:, 2]
    return V


def deoxys(src, out):
    k = Kit(src, coat="shell")

    # --- the head: a helmet drawn up into a point, two square flanges ---------------------------------------------
    k.add("head", ell(HC, HR, deform=helmet, sub=5), ORANGE, "head")
    k.add("neck", tube([(0, 0.0, 0.64), (0, -0.006, 0.69)], [0.026, 0.024]), ORANGE, "head")
    FL = [(x, y, z) for x, ys, zs in ((0.032, (-0.022, 0.026), (0.71, 0.762)), (0.138, (-0.013, 0.017), (0.734, 0.772)))
          for y in ys for z in zs]
    k.add("flange", hull(FL), ORANGE, "head", fuse=False, mirror=True)
    # the teal slot along each flange's front
    ex = unit(0.106, 0.009, 0.017)
    k.add("slot", slab([(-0.032, -0.0055), (0.042, -0.0045), (0.042, 0.0045), (-0.032, 0.0055)],
                       (0.088, -0.0185, 0.75), ex, (0, 0, 1), 0.007), TEAL, "head", fuse=False, mirror=True)

    # --- the trunk ------------------------------------------------------------------------------------------------
    k.add("chest", ell((0, 0.0, 0.598), (0.072, 0.055, 0.068), deform=wider_top), ORANGE, "body")
    k.add("shoulder", ell((0.075, 0.0, 0.632), (0.034, 0.034, 0.032)), ORANGE, "arm{s}", mirror=True)
    k.add("waist", tube([(0, 0.006, 0.45), (0, 0.006, 0.505), (0, 0.004, 0.555)], [0.03, 0.028, 0.034]), GREY, "body")
    for i, z in enumerate((0.485, 0.508, 0.531)):
        k.add(f"rib{i}", ring_band((0, 0.006, z), (0, 0, 1), 0.0275, 0.008, thick=0.005), DARK, "body", fuse=False)
    k.add("hips", ell((0, 0.0, 0.43), (0.072, 0.058, 0.05)), ORANGE, "body")

    # the crystal: a glowing gem set in a dark ring in the middle of the chest
    CC = np.array([0.0, -0.052, 0.6])
    k.add("ring", lathe(CC + np.array([0, 0.008, 0]), (0, -1, 0),
                        [(0.021, 0.0), (0.032, 0.0), (0.033, 0.009), (0.022, 0.011), (0.021, 0.0)], seg=48),
          DARK, "body", fuse=False)
    gem = [CC + np.array(p) for p in ((0, -0.02, 0), (0.019, -0.006, 0), (-0.019, -0.006, 0), (0, -0.006, 0.019),
                                       (0, -0.006, -0.019), (0.013, -0.006, 0.013), (-0.013, -0.006, 0.013),
                                       (0.013, -0.006, -0.013), (-0.013, -0.006, -0.013), (0, 0.008, 0))]
    k.add("crystal", hull(gem), CRYSTAL, "body", fuse=False, glow=CRYSTAL_GLOW)

    # --- the legs: long pointed blades, their tops rising beside the hips -----------------------------------------
    LEG = [(0.092, 0.006, 0.535), (0.086, 0.002, 0.45), (0.098, 0.0, 0.36), (0.124, -0.004, 0.18),
           (0.15, -0.006, 0.012)]
    k.add("leg", tube(LEG, [0.008, 0.044, 0.046, 0.03, 0.007]), ORANGE, "leg{s}", mirror=True)

    # --- the face's stripe: teal from the point down the middle of the face to the chin ---------------------------
    k.paint(lambda Q: inside(np.abs(Q[:, 0]) - 0.0115, 0.0015) * inside(Q[:, 1] - (HC[1] + 0.012), 0.002)
            * inside(0.665 - Q[:, 2], 0.002), TEAL)

    # the legs' slots: long teal diamonds down each leg's front
    for s in (1, -1):
        P = np.array([0.114 * s, -0.036, 0.27])
        n = unit(0.22 * s, -1, 0.05)
        k.decal(P, n, (0.011, 0.075), TEAL, eps=0.004, up=unit(0.14 * s, 0, 1),
                outline=polygon([(0, 1), (0.9, 0.2), (0.8, -0.3), (0, -1), (-0.8, -0.3), (-0.9, 0.2)]),
                rings=10, segs=56)

    # --- the arms: an orange and a teal tentacle twisted like DNA, hanging forward and down -----------------------
    PATH = np.array([(0.08, 0.0, 0.64), (0.14, -0.02, 0.625), (0.19, -0.055, 0.55), (0.205, -0.075, 0.43),
                     (0.198, -0.07, 0.31), (0.185, -0.05, 0.2)])
    line = []
    for i in range(len(PATH) - 1):
        for t in np.linspace(0, 1, 16, endpoint=False):
            line.append(PATH[i] + (PATH[i + 1] - PATH[i]) * t)
    line.append(PATH[-1])
    line = np.array(line)
    # a smooth line: average neighbours a few times
    for _ in range(6):
        line[1:-1] = (line[:-2] + 2 * line[1:-1] + line[2:]) / 4
    T = np.gradient(line, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    nrm = np.cross(T[0], (0, 1.0, 0)); nrm /= np.linalg.norm(nrm)
    frames = []
    for i in range(len(line)):
        nrm = nrm - T[i] * (nrm @ T[i]); nrm /= np.linalg.norm(nrm)
        frames.append((nrm.copy(), np.cross(T[i], nrm)))
    L = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(line, axis=0), axis=1))])
    s_ = L / L[-1]
    turns = 2.6
    for name, col, ph in (("armorange", ORANGE, 0.0), ("armteal", TEAL, math.pi)):
        pts, rad = [], []
        for i in range(len(line)):
            a = ph + 2 * math.pi * turns * s_[i]
            off = 0.021 * min(1.0, s_[i] / 0.12)
            n_, b_ = frames[i]
            pts.append(line[i] + off * (math.cos(a) * n_ + math.sin(a) * b_))
            rad.append(0.021 - 0.005 * s_[i])
        for side, bone in ((1, "armR"), (-1, "armL")):
            P = np.array(pts) * np.array([side, 1, 1.0])
            k.add(f"{name}{bone[-1]}", outward(tube(P, rad, seg=14, per=2)), col, bone, fuse=False)

    k.build(voxel=0.0055, smooth=4, band=0.005)

    # --- the eyes: narrow white almonds, slanted up and out, a black pupil toward the stripe -----------------------
    LID, INK, WHITE = srgb(ORANGE), srgb(INKH), srgb("#F4F2EC")

    def top(u):
        return 0.25 + 0.45 * u

    ALMOND = [(u, top(u)) for u in np.linspace(1, -1, 9)] + \
             [(u, top(u) - 0.95 * (1 - u * u) ** 0.6 - 0.15) for u in np.linspace(-0.9, 0.9, 13)]

    def eye(u, v):
        c = np.tile(WHITE, (len(u), 1))
        c[((u + 0.25) / 0.36) ** 2 + ((v + 0.08) / 0.5) ** 2 < 1] = INK
        c[v > top(u) - 0.2] = INK
        return c

    E, EN = on_ell(HC, HR, (0.5, -0.84, 0.06))

    def eyes(state):
        for side in (1, -1):
            m = np.array([side, 1, 1.0])
            flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
            outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
            k.decal(E * m, EN * m, (0.019, 0.016), flip(eye_look(state, eye, LID, INK, line=0.2)), eps=0.004,
                    outline=outline, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    deoxys(src, out)
