# Armarouge sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/armarouge.py -- <the export's Armarouge.glb> <out.glb>
# Grown from charcadet.py (sibling): its black shell, its reds, its flames (glowing tongues gathered a bone and a
# colour), the red ring of its peeping eye kept as a mark on the helm's side.
#
# The design, from the references: a knight in yellow armour. Its largest masses are two great round yellow pauldrons,
# each wider than the head, in two layers with a short cannon-like block standing on the outer top. A small black helm
# drawn down to a pointed chin, a red stripe over its crown, narrow slanted yellow eyes with a dark pupil, a short gold
# point out of each side at the jaw, the red ring on its left side; a red plume of flame rising from the back of the
# helm with a yellow tongue sweeping to -X. A yellow pointed collar, a yellow breastplate with red chevron lines, a
# black waist and a yellow fauld hanging to a point. Dark red forearms coming out under the pauldrons, red fists. Long
# legs: black thighs, red boots widening to the ground, dark red at their tops.
# Skeleton: body (0, 0, 0.64), head (0, -0.01, 0.79), flame (0, 0.01, 0.94), arms (+-0.1, 0, 0.74), legs (+-0.055,
# 0, 0.52).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, boxy, srgb, inside, smoothstep, unit, eye_look, polygon, outward, args, sibling

cd = sibling("charcadet")
COAL, RED, INK = cd.COAL, cd.RED, cd.INK
HELM = cd.SHELL
GOLD, SEAM, DARKRED, FIST, BOOTTOP = "#F4D42C", "#C49A16", "#8C2A2E", "#E2342A", "#9A2622"
EYE, PUPIL = "#F8DC30", "#2A1A10"

HC, HR = np.array([0.0, -0.02, 0.86]), np.array([0.12, 0.12, 0.15])
PC = np.array([0.262, 0.0, 0.72])            # a pauldron's middle (+X)


def armarouge(src, out):
    k = Kit(src, coat="metal")
    flames = cd.Flames()

    # --- the helm: a black egg drawn down to a pointed chin -------------------------------------------------------
    def helm(V):
        V = V.copy()
        lo = np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= 1 - 0.5 * lo ** 1.3
        V[:, 1] *= 1 - 0.35 * lo ** 1.3
        V[:, 1] -= 0.28 * lo ** 2
        V[:, 2] -= 0.15 * lo ** 2
        return V
    k.add("head", ell(HC, HR, deform=helm), HELM, "head")
    k.add("neck", tube([(0, 0.0, 0.71), (0, -0.006, 0.79)], [0.045, 0.042]), COAL, "head")
    # the red stripe over its crown, from the brow to the back
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.016, 0.002) * inside(HC[2] + 0.045 - P[:, 2], 0.003), RED,
            on=["head"])
    # the red ring of Charcadet's eye, kept on the helm's left side
    # (a loose ring, not a patch: a patch's rays here meet the pauldron first)
    p, n = cd.on_ell(HC, HR, cd.az_dir(-62, 0.02))
    k.add("ring", lathe(p - n * 0.003, n, [(0.016, 0), (0.031, 0), (0.031, 0.008), (0.016, 0.008), (0.016, 0)], seg=32),
          "#E2402A", "head", fuse=False)
    # a short gold point out of each side at the jaw
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        b = np.array([0.074 * s, -0.012, 0.78])
        k.add(f"jawpoint{side}", hull([b + (0, 0.018, 0.018), b + (0, -0.018, 0.018), b + (0, 0.018, -0.016),
                                       b + (0, -0.018, -0.016), b + (-0.012 * s, 0, 0),
                                       b + (0.085 * s, -0.004, 0.036)]), GOLD, "head", fuse=False)
    # the plume: red flame rising from the back of the helm, a yellow tongue sweeping to -X
    top = np.array([0.0, 0.05, 0.975])
    flames.add(cd.tongue(top, unit(0.05, 0.45, 1), 0.3, 0.095, side=(1, 0, 0), flick=0.25, thick=0.7, root=0.9,
                         sub=4), cd.FLAME_RED, "flame", cd.FLAME_RED)
    flames.add(cd.tongue(top + (-0.03, 0.02, 0.06), unit(-1, 0.3, 0.45), 0.22, 0.055, side=(0, 0, 1), flick=0.35,
                         thick=0.7), cd.FLAME_ORANGE, "flame", cd.FLAME_ORANGE)
    flames.add(cd.tongue(top + (-0.04, 0.02, 0.068), unit(-1, 0.3, 0.5), 0.13, 0.03, side=(0, 0, 1), flick=0.3,
                         thick=0.8), cd.FLAME_HEART, "flame", cd.FLAME_HEART)
    flames.add(cd.tongue(top + (0.02, 0.09, 0.2), unit(0.4, 0.4, 1), 0.12, 0.035, side=(1, 0, 0), flick=0.3,
                         thick=0.7), cd.FLAME_VIOLET, "flame", cd.FLAME_VIOLET)

    # --- the trunk ------------------------------------------------------------------------------------------------
    # a yellow pointed collar round the neck's foot
    def collar(V):
        V = V.copy()
        a = np.arctan2(V[:, 0], -V[:, 1])
        V[:, 2] -= 0.6 * np.clip(np.cos(a), 0, 1) ** 4 * np.clip(-V[:, 2], 0, 1)
        return V
    k.add("collar", ell((0, -0.004, 0.73), (0.1, 0.08, 0.045), deform=collar), GOLD, "body")
    k.add("breast", ell((0, -0.004, 0.645), (0.102, 0.072, 0.1), deform=boxy(2.4)), GOLD, "body")
    k.add("waist", ell((0, 0.0, 0.55), (0.066, 0.052, 0.055)), COAL, "body")
    k.add("pelvis", ell((0, 0.0, 0.49), (0.088, 0.064, 0.05)), COAL, "body")
    # red chevrons over the breast and a stripe down its middle
    for z0 in (0.665, 0.62):
        k.paint(lambda P, z0=z0: inside(np.abs(P[:, 2] - (z0 + 0.45 * np.abs(P[:, 0]))) - 0.007, 0.0015)
                * inside(np.abs(P[:, 0]) - 0.085, 0.002) * inside(P[:, 1] + 0.02, 0.003), RED, on=["breast"])
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.007, 0.0015) * inside(P[:, 2] - 0.615, 0.002)
            * inside(0.555 - P[:, 2], 0.002) * inside(P[:, 1] + 0.02, 0.003), RED, on=["breast"])
    # the fauld: a yellow plate hanging over the hips to a point
    fy = -0.062
    k.add("fauld", hull([(-0.075, fy + 0.012, 0.57), (0.075, fy + 0.012, 0.57), (-0.075, fy - 0.014, 0.565),
                         (0.075, fy - 0.014, 0.565), (-0.082, fy + 0.012, 0.48), (0.082, fy + 0.012, 0.48),
                         (-0.077, fy - 0.012, 0.485), (0.077, fy - 0.012, 0.485), (0, fy + 0.01, 0.405),
                         (0, fy - 0.012, 0.415)]), GOLD, "body", fuse=False)

    # --- the pauldrons: the largest masses, two layers and a block standing on the outer top -----------------------
    k.add("pauldron", ell(PC, (0.16, 0.138, 0.14), deform=boxy(2.3)), GOLD, "arm{s}", mirror=True)
    k.add("pauldronlow", ell(PC + (0.024, -0.005, -0.13), (0.138, 0.125, 0.073), deform=boxy(2.2)), GOLD, "arm{s}",
          mirror=True)
    k.paint(lambda P: inside(np.abs(P[:, 2] - (PC[2] - 0.096)) - 0.005, 0.0015) * (np.abs(P[:, 0]) > 0.15),
            SEAM, on=["pauldron"])
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        d = unit(0.45 * s, 0, 1)
        base = PC * np.array([s, 1, 1.0]) + np.array([0.067 * s, 0.0, 0.093])
        a, b = np.array([1.0, 0, 0]) - d * d[0], np.array([0, 1.0, 0])
        a /= np.linalg.norm(a)
        corners = [base + d * h + a * x + b * y for h in (-0.027, 0.08) for x in (-0.06, 0.06) for y in (-0.073, 0.067)]
        k.add(f"block{side}", hull(corners), GOLD, f"arm{side}", fuse=False)
        k.add(f"notch{side}", hull([base + d * 0.081 + a * x + b * y for x in (-0.04, 0.04) for y in (-0.053, 0.047)]
                                   + [base + d * 0.089 + a * x + b * y for x in (-0.04, 0.04) for y in (-0.053, 0.047)]),
              SEAM, f"arm{side}", fuse=False)

    # --- the arms: dark red forearms out under the pauldrons, red fists held forward and down -----------------------
    ELB, WRIST = np.array([0.235, -0.015, 0.58]), np.array([0.19, -0.1, 0.44])
    k.add("forearm", tube([ELB, WRIST], [0.06, 0.057]), DARKRED, "arm{s}", mirror=True, surface="fur")
    k.add("fist", ell(WRIST + (-0.006, -0.027, -0.045), (0.066, 0.069, 0.069)), FIST, "arm{s}", mirror=True,
          surface="fur")

    # --- the legs: black thighs, red boots widening to the ground --------------------------------------------------
    # set apart in a wide stance; each boot flares like a bell to the ground, there as wide as the head
    HIP, KNEE, GROUND = np.array([0.065, 0.0, 0.5]), np.array([0.1, -0.01, 0.3]), np.array([0.13, -0.012, 0.012])
    k.add("thigh", tube([HIP, KNEE], [0.058, 0.05]), COAL, "leg{s}", mirror=True, surface="fur")
    up = KNEE - GROUND; L = float(np.linalg.norm(up))
    k.add("boot", lathe(GROUND, up, [(0, 0), (0.115, 0), (0.104, 0.05), (0.082, 0.14), (0.062, 0.23),
                                     (0.05, L - 0.01), (0, L + 0.03)], seg=48), FIST, "leg{s}", mirror=True,
          surface="fur")
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.29) - 0.04, 0.02) * (np.abs(P[:, 0]) > 0.03), BOOTTOP,
            on=["boot", "thigh"])

    flames.put(k)
    k.build(voxel=0.0074, smooth=4, band=0.008, wband=0.06)

    # The eyes: narrow slanted yellow slits with a dark pupil, cut by the helm; closed, a yellow line on the black
    shape = [(-1.0, -0.15), (-0.6, -0.75), (0.2, -0.85), (0.85, -0.35), (1.0, 0.55), (0.3, 0.35)]
    def open_eye(u, v):
        c = np.tile(srgb(EYE), (len(u), 1))
        c[((u + 0.1) / 0.3) ** 2 + ((v + 0.25) / 0.45) ** 2 < 1] = srgb(PUPIL)
        return c
    def lay(state):
        ink = INK if state in ("Open", "Fierce") else EYE
        look = eye_look(state, open_eye, srgb(HELM), srgb(ink), line=0.2)
        for s in (1, -1):
            p, n = cd.on_ell(HC, HR, cd.az_dir(26 * s, -0.12))
            col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
            o = shape if s == 1 else [(-a, b) for a, b in shape]
            k.decal(p, n, (0.046, 0.026), col, eps=0.0018, rings=20, segs=56, outline=polygon(o))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    armarouge(*args())
