# Hribarde (No. 10030, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/hribarde.py -- <the export's Hribarde.glb> <out.glb>
#
# From its picture (10030-Hribarde.jpg) and its brief (CP-dex-1-plateau.md, 30): the old forest bolete. Largest first:
# a broad cracked chestnut cap as wide as the whole is tall, tipped back a little so its deep yellow-green gills show
# at the front; a fat pale stem; a long white beard of mycelium threads from the cheeks down over the belly, and more
# threads hanging round the stem's sides and back from under the cap; thick roots splayed out for feet; a drowsy old
# face under the brim: half-lidded eyes under heavy bushy grey brows, a little nose, a small round mouth in a white
# moustache; on the cap two clumps of fern, small mushrooms and moss; moss on the stem's sides.
#
# Grown from hribling.py (sibling): the same palette, cap with its gill paint, egg of a stem, moss and sleepy eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, outward, slab, srgb, inside, smoothstep, unit, axes, args, sibling)

hb = sibling("hribling")

FACE, BEARD, BEARD2, BROWC, MOUTHC = "#CDBC9C", "#E8E6E0", "#CFCCC4", "#B4B2AC", "#3A2C26"
FERN, CRACK, SMALLCAP = "#5C9A44", "#5A2C16", "#9A5630"

CAPC = np.array([0.0, 0.07, 0.722])
CAPR = np.array([0.58, 0.53, 0.4])
CAPROT = (-12.0, -5.0, 0.0)
STEMC = np.array([0.0, 0.0, 0.375])
STEMR = np.array([0.325, 0.292, 0.32])
FACEC = np.array([0.0, -0.1, 0.565])
FACER = np.array([0.215, 0.18, 0.15])
VOXEL = 0.012


def cracks(k, C, R, rot, width=0.011):
    """Cracks across the cap's top: dark lines painted along polylines in the cap's own frame (x, y over its radii)."""
    lines = [[(-0.62, -0.5), (-0.42, -0.3), (-0.36, -0.05), (-0.15, 0.1)],
             [(0.2, -0.86), (0.26, -0.56), (0.1, -0.36), (0.16, -0.1)],
             [(0.86, -0.2), (0.6, -0.14), (0.5, 0.05), (0.3, 0.16)],
             [(0.56, -0.72), (0.44, -0.46), (0.56, -0.3)],
             [(-0.86, 0.1), (-0.6, 0.2), (-0.5, 0.46)],
             [(0.1, 0.86), (0.05, 0.6), (-0.15, 0.45)],
             [(0.76, 0.5), (0.55, 0.35), (0.6, 0.15)],
             [(-0.3, -0.85), (-0.25, -0.62), (-0.42, -0.45)]]
    m = hb.rotm(rot); C = np.array(C, float); R = np.array(R, float)
    segs = []
    for ln in lines:
        for a, b in zip(ln, ln[1:]):
            segs.append((np.array(a) * R[:2], np.array(b) * R[:2]))
    def mask(P):
        q = (P - C) @ m
        xy = q[:, :2]
        best = np.full(len(P), 9.0)
        for a, b in segs:
            e = b - a
            t = np.clip(((xy - a) @ e) / (e @ e), 0, 1)
            best = np.minimum(best, np.linalg.norm(xy - (a + t[:, None] * e), axis=1))
        return inside(best - width, 0.0025) * (q[:, 2] > R[2] * 0.08)
    k.paint(mask, CRACK, on=["cap"])


def small_mushroom(k, name, C, R, rot, d, size, bone="head"):
    """A little bolete growing on the cap: a pale stem up out of the surface and a small chestnut dome."""
    p, n = hb.on_ell(C, R, d, rot=rot)
    up = unit(*(n * 0.5 + np.array([0, 0, 1.0])))
    top = p + up * 0.075 * size
    k.add(name + "stem", outward(tube([p - n * 0.02, top], [0.02 + 0.004 * size, 0.018 + 0.003 * size], seg=12)),
          hb.STEM, bone, fuse=False, surface="skin")
    a, b = axes(up)
    V, F = ell((0, 0, 0), (0.05 * size, 0.05 * size, 0.034 * size), deform=hb.dome(0.35), sub=3)
    V = V @ np.stack([a, b, up]) + top
    k.add(name + "cap", outward((V, F)), SMALLCAP, bone, fuse=False, surface="shell")


def frond(k, name, base, d, length, width, face, bone="head"):
    """A fern's frond: one plate with its leaflets cut along its edges as teeth, its face turned toward face."""
    d = unit(*d)
    ex = np.cross(face, d); ex /= np.linalg.norm(ex)
    pts_r, pts_l = [], []
    N = 9
    for i in range(N + 1):
        t = i / N
        w = width * math.sin(math.pi * min(t * 1.15, 1.0)) ** 0.7 * (1 - 0.35 * t)
        w = w if i % 2 else w * 0.45
        if i in (0, N):
            w = width * 0.12
        pts_r.append((w, t * length)); pts_l.append((-w, t * length))
    outline = pts_r + [(0.0, length * 1.04)] + pts_l[::-1]
    k.add(name, slab(outline, base, ex, d, 0.02), FERN, bone, fuse=False, surface="leaf")


def fern(k, name, C, R, rot, d, lean, face, bone="head"):
    p, n = hb.on_ell(C, R, d, rot=rot)
    for j, (sx, l) in enumerate([(0.0, 0.25), (-0.7, 0.2), (0.72, 0.19)]):
        dd = unit(*(np.array([0, 0, 1.0]) + np.array(lean) * 0.9 + np.cross(face, (0, 0, 1)) * sx))
        side = np.cross(unit(*face), (0, 0, 1.0))
        frond(k, f"{name}{j}", p - n * 0.02 + side * sx * 0.05 - np.array(face) * 0.012 * j, dd, l, 0.06,
              unit(*face), bone)
    hb.moss(k, name + "moss", C, R, d, 0.045, bone, rot=rot, flat=0.4)


def lock(k, name, pts, r0, r1, colour, bone):
    """A lock of the beard or a hanging thread: a tapering loose tube."""
    n = len(pts)
    k.add(name, outward(tube(pts, list(np.linspace(r0, r1, n)), seg=14)), colour, bone, fuse=False, surface="hair")


def hribarde(src, out_path):
    k = Kit(src, coat="skin")

    # ---- the stem: fat and pale, the face pushed forward under the brim
    k.add("stem", ell(STEMC, STEMR, deform=hb.egg), hb.STEM, "body")
    k.add("face", ell(FACEC, FACER), hb.STEM, "head")
    k.add("neck", tube([(0, 0.02, 0.6), (0, 0.05, 0.69)], [0.21, 0.2]), hb.STEM, "head")
    k.add("nose", ell((0.0, -0.272, 0.572), (0.034, 0.03, 0.03)), FACE, "head")

    # ---- the roots: thick, splayed out for feet, each turned up a little at its tip
    for i, a in enumerate([35, -35, 90, -90, 148, -148]):
        r = math.radians(a)
        sx, sy = math.sin(r), -math.cos(r)
        P = [(sx * 0.22, sy * 0.2, 0.17), (sx * 0.33, sy * 0.3, 0.095), (sx * 0.43, sy * 0.39, 0.062),
             (sx * 0.49, sy * 0.445, 0.055)]
        k.add(f"root{i}", tube(P, [0.115, 0.085, 0.056, 0.044]), hb.STEM, "legR" if a > 0 else "legL")

    # ---- the cap: broad and tipped back, cracked, its gills deep
    hb.cap(k, CAPC, CAPR, CAPROT, under=0.26, stem_r=0.22)
    cracks(k, CAPC, CAPR, CAPROT)
    hb.shine(k, CAPC, CAPR, CAPROT, (0.1, -0.5, 0.8), (0.2, 0.035), up=unit(0.0, 0.6, 0.8))

    # ---- the cap's shadow under the brim, and the old face's tan
    k.paint(lambda P: inside(0.655 - P[:, 2], 0.004), hb.BAND, on=["stem", "neck", "face"])
    k.paint(lambda P: inside(((P[:, 0] / 0.17) ** 2 + ((P[:, 2] - 0.6) / 0.065) ** 2) - 1, 0.25)
            * (P[:, 1] < -0.15) * inside(P[:, 2] - 0.65, 0.004), FACE, on=["face", "nose"])

    # ---- moss on the stem's sides and on two roots
    for i, (d, s) in enumerate([((0.96, -0.15, -0.3), 0.05), ((0.85, -0.45, -0.05), 0.03),
                                ((-0.95, -0.22, -0.25), 0.055), ((-0.78, -0.5, -0.45), 0.03),
                                ((0.55, 0.75, -0.3), 0.04), ((-0.4, 0.85, -0.2), 0.035)]):
        hb.moss(k, f"moss{i}", STEMC, STEMR, d, s, "body", deform=hb.egg)

    # ---- on the cap: ferns, small mushrooms and moss
    fern(k, "fernL", CAPC, CAPR, CAPROT, (-0.62, -0.28, 0.75), (-0.55, -0.15, 0.0), (0.35, -1.0, 0.2))
    fern(k, "fernR", CAPC, CAPR, CAPROT, (0.62, 0.32, 0.72), (0.55, 0.2, 0.0), (0.5, -0.8, 0.2))
    for i, (d, s) in enumerate([((-0.86, -0.25, 0.42), 1.0), ((-0.76, -0.4, 0.48), 0.7), ((-0.3, -0.5, 0.8), 0.9),
                                ((-0.2, -0.56, 0.76), 0.65), ((0.55, -0.28, 0.76), 1.1), ((0.64, -0.16, 0.7), 0.8),
                                ((0.46, 0.18, 0.86), 0.75), ((0.82, -0.38, 0.44), 0.8)]):
        small_mushroom(k, f"shroom{i}", CAPC, CAPR, CAPROT, d, s)
    hb.moss(k, "capmoss0", CAPC, CAPR, (0.1, -0.35, 0.92), 0.04, "head", rot=CAPROT, flat=0.4)
    hb.moss(k, "capmoss1", CAPC, CAPR, (-0.2, 0.5, 0.85), 0.04, "head", rot=CAPROT, flat=0.4)

    # ---- the face: bushy grey brows over the eyes, a small round mouth in a white moustache
    for s, side in ((1, "R"), (-1, "L")):
        for j, (dz, l) in enumerate([(0.0, 1.0), (0.018, 0.85), (-0.012, 0.7)]):
            P = [(s * 0.035, -0.258, 0.656 + dz), (s * (0.035 + 0.07 * l), -0.24, 0.664 + dz),
                 (s * (0.035 + 0.13 * l), -0.205, 0.636 + dz)]
            lock(k, f"brow{side}{j}", P, 0.02, 0.014, BROWC, "head")
        P = [(s * 0.012, -0.322, 0.54), (s * 0.075, -0.33, 0.522), (s * 0.13, -0.3, 0.478), (s * 0.16, -0.275, 0.43)]
        lock(k, f"moustache{side}", P, 0.03, 0.02, BEARD, "head")
    k.add("mouth", outward(ell((0.0, -0.33, 0.508), (0.024, 0.012, 0.018), sub=2)), MOUTHC, "head", fuse=False)

    # ---- the beard: a white mass over the stem's upper front, as wide as the stem, its locks from the cheeks and
    # chin down over the belly, longest in the middle
    k.add("beardmass", ell((0.0, -0.245, 0.41), (0.17, 0.08, 0.15)), BEARD2, "body", surface="hair")
    for i, x in enumerate(np.linspace(-0.185, 0.185, 10)):
        f = (x / 0.185) ** 2
        z0 = 0.5 + 0.04 * f
        y0 = -0.275 + 0.08 * f
        end = 0.19 + 0.2 * f
        j = 0.012 * (1 if i % 2 else -1)
        P = [(x, y0, z0), (x * 1.04, y0 - 0.04, z0 - 0.08), (x * 0.96 + j, -0.355 + 0.1 * f, (z0 + end) / 2 - 0.03),
             (x * 0.9 - j, -0.335 + 0.1 * f, end)]
        lock(k, f"beard{i}", P, 0.05, 0.026, BEARD if i % 2 else BEARD2, "body")

    # ---- the threads hanging from under the cap round the sides and the back
    for i, a in enumerate([55, 74, 94, 115, 136, -55, -74, -94, -115, -136]):
        r = math.radians(a)
        sx, sy = math.sin(r), -math.cos(r)
        end = 0.44 + 0.06 * ((i * 37) % 5) / 4
        P = [(sx * 0.25, sy * 0.24, 0.64), (sx * 0.31, sy * 0.29, 0.57), (sx * 0.33, sy * 0.31, (0.57 + end) / 2),
             (sx * 0.335, sy * 0.315, end)]
        lock(k, f"thread{i}", P, 0.022, 0.017, BEARD if i % 2 else BEARD2, "body")

    k.build(voxel=VOXEL, smooth=4, band=0.01, wband=0.05)

    # ---- the eyes: drowsy, heavy-lidded, under the brows
    ep, en = hb.on_ell(FACEC, FACER, (0.52, -0.84, 0.24))
    lay = hb.lay_sleepy(k, ep, unit(0.45, -1.0, 0.05), (0.056, 0.041),
                        hb.sleepy_open(lid_top=0.24, slope=-0.16, lid=FACE), lid=FACE)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    hribarde(*args())
