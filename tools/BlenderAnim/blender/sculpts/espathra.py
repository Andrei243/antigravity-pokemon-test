# Espathra sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/espathra.py -- <the export's Espathra.glb> <out.glb>
#
# The design, from the references: a tall ostrich. Its largest mass is a great orange dome of a back over a white
# underbody that fluffs out into a spiky white fringe at the chest; a row of pale triangles stands along the dome's rear
# edge. Behind it hangs a train of feather frills, yellow over white, trailing nearly to the ground. A thick S of a
# neck rises from the chest, banded orange and lilac, to a small head under a white hood of feathers with a jagged
# rim like a broken eggshell; a lilac beak juts from under the hood and a ringed cyan eye sits on each side of it.
# Long white legs with lilac bands, fat thighs hidden in the white underbody, lilac feet with pale claws.
#
# Grown from Flittle (sibling("flittle")): its colours, its frill (the skirt became the train and the fringe), its
# spikes (the crown became the triangles) and its way of laying eyes on an ellipsoid.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, slab, hull, outward, srgb, inside, unit, eye_look, args, sibling)

FL = sibling("flittle")

ORANGE, ORANGE_DK = "#F4A84A", "#E08E34"
YELLOW, WHITE, CREAM = "#F4E66A", FL.WHITE, "#F2E6C4"
LILAC, LILAC_DK = "#AE96DE", "#8E78C4"
CYAN, CYAN_DK = "#5EE0E6", "#1E8C9C"
INK = FL.INK

# --- the frame ----------------------------------------------------------------------------------------------------
BACK_C, BACK_R = np.array([0.0, 0.09, 0.71]), np.array([0.2, 0.25, 0.175])      # the orange dome
BELLY_C, BELLY_R = np.array([0.0, 0.02, 0.62]), np.array([0.175, 0.205, 0.14])   # the white underbody
HEAD_C, HEAD_R = np.array([0.0, -0.31, 0.975]), np.array([0.08, 0.097, 0.082])   # the hooded head
NECK = [(0.0, -0.13, 0.61), (0.0, -0.26, 0.68), (0.0, -0.3, 0.79), (0.0, -0.26, 0.87), (0.0, -0.29, 0.93)]
NECK_R = [0.054, 0.044, 0.04, 0.04, 0.044]
EYE_DIR = unit(0.78, -0.55, 0.12)
EYE_HALF = (0.031, 0.031)
VOXEL = 0.0085


def curve(pts, n=200):
    """A Catmull-Rom curve through pts, sampled: (points, the share along it of each)."""
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    P = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, n // (len(p) - 1), endpoint=False):
            P.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    P.append(p[-1])
    P = np.array(P)
    s = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(P, axis=0), axis=1))])
    return P, s / s[-1]


def neck_bands(P, pts=NECK, bands=((0.08, 0.27), (0.42, 0.6), (0.76, 0.93))):
    """1 on the neck where its share along the curve falls in one of the lilac bands."""
    C, t = curve(pts)
    d = np.linalg.norm(P[:, None, :] - C[None], axis=2)
    j = d.argmin(1)
    near = d.min(1) < 0.065
    share = t[j]
    m = np.zeros(len(P))
    for a, b in bands:
        m = np.maximum(m, FL.inside(a - share, 0.004) * FL.inside(share - b, 0.004))
    return m * near * (P[:, 2] > 0.6)


def ring_eye(u, v):
    """A ringed eye: a dark rim, a broad cyan ring, a white middle round a dark pupil, a glint."""
    r = np.hypot(u, v)
    c = np.tile(np.array([1.0, 1.0, 1.0]), (len(u), 1))
    c[r > 0.42] = srgb(CYAN)
    c[r > 0.84] = srgb(CYAN_DK)
    c[np.hypot(u + 0.02, v) < 0.24] = srgb("#16303A")
    c[np.hypot(u + 0.12, v - 0.12) < 0.08] = 1.0
    return c


def espathra(src, out):
    k = Kit(src, coat="fur")

    # --- the body: an orange dome over a white underbody ---------------------------------------------------------------
    k.add("back", ell(BACK_C, BACK_R), ORANGE, "body")
    k.add("belly", ell(BELLY_C, BELLY_R), WHITE, "body")
    k.add("chest", ell((0.0, -0.13, 0.6), (0.13, 0.11, 0.12)), WHITE, "body")
    # the dome's edge: orange above a line lower at the back, white below
    k.paint(lambda P: FL.inside(0.655 + 0.11 * np.clip(-P[:, 1], 0, 1) - 0.05 * np.clip(P[:, 1], 0, 1) - P[:, 2],
                                0.004) * (P[:, 2] < 0.95), ORANGE)
    k.paint(lambda P: FL.inside(P[:, 2] - (0.655 + 0.11 * np.clip(-P[:, 1], 0, 1) - 0.05 * np.clip(P[:, 1], 0, 1)),
                                0.004) * (P[:, 2] > 0.4) * (P[:, 2] < 0.75) * (np.hypot(P[:, 0], P[:, 1] + 0.05) > 0.0),
            WHITE, on=["back", "belly", "chest"])

    # --- the neck: a thick S banded orange and lilac -------------------------------------------------------------------
    k.add("neck", tube(NECK, NECK_R, seg=20, per=10), ORANGE, "head")
    k.paint(neck_bands, LILAC, on=["neck"])

    # --- the head: white above, lilac face below, a lilac beak ---------------------------------------------------------
    k.add("skull", ell(HEAD_C, HEAD_R), WHITE, "head")
    k.add("face", ell(HEAD_C + np.array([0, -0.035, -0.03]), (0.048, 0.06, 0.042)), LILAC, "head")
    k.paint(lambda P: FL.inside(P[:, 2] - (HEAD_C[2] - 0.022 + 0.25 * (P[:, 1] - HEAD_C[1] + 0.02)), 0.003)
            * (P[:, 2] > 0.9), LILAC, on=["skull", "face", "neck"])
    k.add("beak", outward(tube([HEAD_C + (0, -0.07, -0.024), HEAD_C + (0, -0.14, -0.04), HEAD_C + (0, -0.19, -0.07)],
                               [0.034, 0.023, 0.008], seg=20)), LILAC, "head", fuse=False, surface="shell")
    k.add("beakline", outward(tube([HEAD_C + (0, -0.085, -0.04), HEAD_C + (0, -0.16, -0.058)],
                                   [0.012, 0.006], seg=12)), LILAC_DK, "head", fuse=False, surface="shell")

    # the hood's jagged rim: a short white skirt round the back and sides of the head, flaring a little
    def hood(u, v):
        t = -0.29 + 0.58 * u                                  # from the left side round the back to the right
        th = 2 * math.pi * t + math.pi / 2
        jag = np.abs(((u * 9) % 1.0) - 0.5) * 2              # points along the rim
        r = HEAD_R[0] * 1.06 + 0.014 * v
        z = 0.0 - (0.05 + 0.02 * jag) * v
        return np.stack([HEAD_C[0] + r * np.cos(th) * 1.0, HEAD_C[1] + 0.006 + r * 1.15 * np.sin(th),
                         HEAD_C[2] + z], axis=-1)
    k.add("hood", outward(sheet(hood, 91, 6, 0.011)), WHITE, "head", fuse=False, surface="fur")
    # a few feather points over the crown, laid back
    for x, y in ((-0.03, 0.01), (0.03, 0.01), (0.0, 0.04)):
        b = HEAD_C + np.array([x, y, HEAD_R[2] * 0.85])
        k.add(f"plume{x}{y}", FL.spike(b, unit(x * 6, 1.0, 0.7), 0.045, 0.016), WHITE, "head", fuse=False,
              surface="fur")

    # --- the chest's spiky white fringe where the neck comes in -------------------------------------------------------
    for i, a in enumerate(np.linspace(-150, -30, 6)):
        r = math.radians(a)
        d = unit(math.cos(r), math.sin(r) * 0.7, -0.35)
        base = np.array([0.0, -0.13, 0.6]) + np.array([0.12 * math.cos(r), 0.1 * math.sin(r), -0.01])
        k.add(f"fringe{i}", FL.spike(base - d * 0.02, d, 0.07, 0.022), WHITE, "body", fuse=False, surface="fur")

    # --- the pale triangles along the dome's rear edge ---------------------------------------------------------------
    for i, a in enumerate((-42, -21, 0, 21, 42)):
        r = math.radians(a)
        dirn = np.array([math.sin(r), math.cos(r), 0.0])
        q = np.array([BACK_R[0] * dirn[0], BACK_R[1] * dirn[1], 0.03])
        base = BACK_C + q * 0.95
        nrm = (base - BACK_C) / BACK_R ** 2; nrm /= np.linalg.norm(nrm)
        down = np.array([0, 0, -1.0]) - nrm * (-nrm[2]); down /= np.linalg.norm(down)
        across = np.cross(nrm, down)
        tri = slab([(-0.02, -0.042), (-0.02, 0.042), (0.075, 0.0)], base + nrm * 0.006, down, across, 0.03)
        k.add(f"tri{i}", tri, CREAM, "body", fuse=False, surface="shell")

    # --- the train: frills of yellow over white hanging behind, nearly to the ground ---------------------------------
    TC = np.array([0.0, 0.08, 0.62])
    k.add("trainy", FL.frill(TC, 0.17, 0.27, 0.02, 0.13, lobes=9, wave=0.02, arc=(-0.27, 0.27), thick=0.016),
          YELLOW, "body", fuse=False, surface="fur")
    k.add("trainw", FL.frill(TC, 0.16, 0.29, -0.06, 0.2, lobes=9, wave=0.022, phase=1.5, arc=(-0.24, 0.24),
                             thick=0.016), WHITE, "body", fuse=False, surface="fur")
    k.add("trainw2", FL.frill(TC, 0.14, 0.27, -0.12, 0.25, lobes=7, wave=0.022, phase=0.6, arc=(-0.2, 0.2),
                              thick=0.016), WHITE, "body", fuse=False, surface="fur")
    # the long plume falling behind, turned out to +X so its face shows
    for name, col, off, ln, wd, th in (("plumey", YELLOW, 0.0, 0.42, 0.13, 0.018), ("plumew", WHITE, -0.012, 0.36,
                                                                                      0.1, 0.022)):
        root = np.array([0.03, 0.24, 0.56 + off])
        d = unit(0.25, 0.45, -1.0)
        w = unit(0.8, -0.55, 0.0)
        outline = [(0, -0.3 * wd), (0.4 * ln, -0.5 * wd), (0.85 * ln, -0.4 * wd), (ln, 0.0), (0.85 * ln, 0.4 * wd),
                   (0.4 * ln, 0.5 * wd), (0, 0.3 * wd)]
        k.add(name, slab(outline, root, d, w, th), col, "body", fuse=False, surface="fur")

    # --- the legs: fat white thighs in the underbody, tapering banded shins, lilac feet --------------------------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        k.add(f"thigh{side}", ell((0.075 * s, 0.03, 0.47), (0.07, 0.085, 0.1)), WHITE, f"leg{side}")
        k.add(f"shin{side}", tube([(0.08 * s, 0.02, 0.42), (0.082 * s, -0.045, 0.27), (0.08 * s, 0.015, 0.075)],
                                  [0.04, 0.028, 0.024], seg=16), WHITE, f"leg{side}")
        k.add(f"foot{side}", ell((0.08 * s, -0.025, 0.035), (0.04, 0.06, 0.03)), LILAC, f"leg{side}")
        for j, a in enumerate((-28, 0, 28)):
            r = math.radians(a)
            d = unit(math.sin(r) * s, -math.cos(r), -0.35)
            base = np.array([0.08 * s, -0.06, 0.03]) + np.array([math.sin(r) * 0.028 * s, -0.012, 0.0])
            k.add(f"claw{side}{j}", FL.spike(base, d, 0.04, 0.012), CREAM, f"leg{side}", fuse=False, surface="shell")
    # two lilac bands on each shin, and the ankle lilac
    for lo, hi in ((0.3, 0.345), (0.16, 0.205), (0.04, 0.09)):
        k.paint(lambda P, lo=lo, hi=hi: FL.inside(lo - P[:, 2], 0.003) * FL.inside(P[:, 2] - hi, 0.003)
                * (np.abs(P[:, 0]) > 0.035), LILAC, on=["shin", "foot"])

    k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.05)

    # --- the eyes: ringed cyan, one on each side of the hood ------------------------------------------------------------
    lid = srgb(WHITE)

    def eyes(state):
        lk = eye_look(state, ring_eye, lid, INK, line=0.14)
        for s in (1, -1):
            de = EYE_DIR * np.array([s, 1, 1])
            p = HEAD_C + de / np.linalg.norm(de / HEAD_R)
            k.decal(p, de, EYE_HALF, (lambda u, v, s=s: lk(s * u, v)), eps=0.003, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    espathra(*args())
