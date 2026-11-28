# Hribling (No. 10029, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/hribling.py -- <the export's Hribling.glb> <out.glb>
#
# From its picture (10029-Hribling.jpg) and its brief (CP-dex-1-plateau.md, 29): a small fat bolete. Largest first:
# a broad glossy chestnut cap, about nine tenths as wide as the whole is tall and some two fifths of its height, tipped
# down to -X and a little back so that its yellow-green gills show at the front and +X; under it a stout cream stem
# that is the body, fullest low; the cap's shadow a dark band across the stem's top in which two sleepy eyes sit,
# heavy pale lids over half of each; patches of green moss on the stem's lower sides; two little stubby feet.
#
# Written to be grown from (hribarde.py imports it with sibling("hribling")): the palette, the cap (a dome drawn flat
# underneath, tipped) with its gill paint, the egg of a stem, a clump of moss and the sleepy eyes are functions here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import (Kit, ell, tube, outward, axes, srgb, inside, smoothstep, unit, lumpy, eye_look, args)

# --- the line's colours -----------------------------------------------------------------------------------------
CHESTNUT, SHINE, GILLS, STEM = "#8C4A26", "#B47A52", "#CFD37C", "#F1E5C8"
BAND, LID, WHITE, PUPIL, INK = "#4C3829", "#E2D2B6", "#F6F4EE", "#2A201A", "#2A201A"
MOSS, MOSSDARK = "#6F9A3C", "#527A2C"

# --- Hribling's frame (the skeleton: body 0.17, head 0.27, legs (+-0.05, 0, 0.06)) ---------------------------------
CAPC = np.array([0.0, 0.012, 0.302])
CAPR = np.array([0.226, 0.2, 0.164])
CAPROT = (-10.0, -12.0, 0.0)              # the front tipped up, +X up: the gills show to the front camera
STEMC = np.array([0.0, -0.004, 0.162])
STEMR = np.array([0.14, 0.126, 0.152])
VOXEL = 0.0042


def rotm(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def dome(under=0.25):
    def f(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * under, V[:, 2])
        return V
    return f


def egg(V):
    """The stem: fullest low, drawn in near the top."""
    V = V.copy(); z = V[:, 2]
    s = 1 + 0.1 * np.clip(-z, 0, 1) - 0.16 * np.clip(z, 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def on_ell(c, r, d, deform=None, rot=None):
    """A point of an ellipsoid along d from its middle (in its own frame, before its turn) and its normal."""
    u = unit(*d)[None]
    if deform is not None:
        u = deform(u)
    c, r = np.array(c, float), np.array(r, float)
    p = (u * r)[0]
    n = p / r ** 2
    if rot is not None:
        m = rotm(rot); p = m @ p; n = m @ n
    return c + p, n / np.linalg.norm(n)


def cap(k, C, R, rot, under=0.25, bone="head", gill_edge=0.97, stem_r=0.0):
    """The bolete's cap: a dome drawn flat underneath and tipped by rot, chestnut, its underside yellow-green gills
    (painted by the cap's own frame) but where the stem goes up into it."""
    k.add("cap", ell(C, R, rot=rot, deform=dome(under)), CHESTNUT, bone, surface="shell")
    m = rotm(rot); C = np.array(C, float); R = np.array(R, float)
    def gills(P):
        q = (P - C) @ m                      # the cap's own frame
        rr = np.hypot(q[:, 0] / R[0], q[:, 1] / R[1])
        return (inside(q[:, 2] + R[2] * 0.02, 0.0015) * inside(rr - gill_edge, 0.004)
                * smoothstep(stem_r * 0.9, stem_r * 1.05, np.hypot(q[:, 0], q[:, 1])))
    k.paint(gills, GILLS, on=["cap"])


def shine(k, C, R, rot, d, half, up=(0, 0, 1)):
    """The cap's gloss: a pale streak laid along its top."""
    p, n = on_ell(C, R, d, rot=rot)
    CS, CC = srgb(SHINE), srgb(CHESTNUT)
    def look(u, v):
        return np.where((u * u + (v * 1.4) ** 2 < 0.55)[:, None], CS[None], CC[None])
    k.decal(p, n, half, look, up=up, eps=0.003, rings=12, segs=48)


def moss(k, name, c, r, d, size, bone, deform=None, rot=None, flat=0.45):
    """A clump of moss on a surface: a lumpy cushion lying on it (flat along the surface's normal), half sunk."""
    p, n = on_ell(c, r, d, deform, rot)
    a, b = axes(n)
    for j, (da, db, s) in enumerate([(0, 0, 1.0), (0.9, 0.5, 0.7), (-0.8, 0.7, 0.65), (0.2, -1.0, 0.6),
                                     (-0.6, -0.8, 0.5)]):
        q = p + (a * da + b * db) * size * 1.1
        V, F = ell((0, 0, 0), (size * s, size * s * 1.1, size * s * flat * 1.4), deform=lumpy(4, 3, 0.3), sub=3)
        V = V @ np.stack([a, b, n]) + (q - n * size * s * 0.25)
        k.add(f"{name}_{j}", (V, F), MOSSDARK if j % 2 else MOSS, bone, surface="leaf")


def sleepy_open(lid_top=0.12, slope=0.0, pupil=0.4, rim=0.84, lid=LID):
    """The open sleepy eye (u outward, v up): a white with a dark rim, the heavy lid over its top, a dark line on
    the lid's edge and a big dark pupil just under it."""
    CW, CP, CL, CI = srgb(WHITE), srgb(PUPIL), srgb(lid), srgb(INK)
    def look(u, v):
        c = np.tile(CW, (len(u), 1))
        edge = lid_top + slope * u
        c[u * u + (v - edge + 0.2) ** 2 < pupil ** 2] = CP
        c[u * u + v * v > rim * rim] = CI
        c[v > edge] = CL
        c[(np.abs(v - edge) < 0.12) & (u * u + v * v < 1.0)] = CI
        return c
    return look


def lay_sleepy(k, ep, en, half, open_look, mirror=True, lid=LID):
    """The line's eyes in the five states: Open is the sleepy half-lidded eye, the rest drawn on the lid."""
    CL, CI = srgb(lid), srgb(INK)
    def lay(state):
        look = eye_look(state, open_look, CL, CI, line=0.16)
        k.decal(ep, en, half, look, eps=0.0022, rings=20, segs=56, mirror=mirror)
    return lay


def hribling(src, out_path):
    k = Kit(src, coat="skin")

    # ---- the stem: a stout cream egg, its top going up into the cap
    k.add("stem", ell(STEMC, STEMR, deform=egg), STEM, "body")
    k.add("neck", tube([(0, 0.004, 0.24), (0, 0.01, 0.29)], [0.1, 0.095]), STEM, "head")
    k.add("foot", ell((0.062, -0.05, 0.024), (0.042, 0.046, 0.025)), STEM, "leg{s}", mirror=True)

    # ---- the cap: broad, glossy, tipped
    cap(k, CAPC, CAPR, CAPROT, stem_r=0.105)
    shine(k, CAPC, CAPR, CAPROT, (0.1, -0.5, 0.8), (0.085, 0.017), up=unit(0.0, 0.6, 0.8))

    # ---- the cap's shadow across the stem's top, the eyes set in it
    def band(P):
        z = P[:, 2]; ax = np.abs(P[:, 0])
        low = 0.232 + 0.038 * np.clip(ax / 0.12, 0, 1) ** 2
        return inside(low - z, 0.0018) * (z < 0.33)
    k.paint(band, BAND, on=["stem", "neck"])

    # ---- moss on the stem's lower sides
    for i, (d, s) in enumerate([((0.96, -0.12, -0.25), 0.026), ((0.86, -0.38, 0.02), 0.014),
                                ((-0.95, -0.2, -0.22), 0.028), ((-0.8, -0.5, -0.4), 0.014),
                                ((0.5, 0.75, -0.35), 0.02), ((-0.35, 0.85, -0.45), 0.016)]):
        moss(k, f"moss{i}", STEMC, STEMR, d, s, "body", deform=egg)

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)

    # ---- the eyes: sleepy, half-lidded, in the dark band under the brim
    ep, en = on_ell(STEMC, STEMR, (0.34, -0.72, 0.6), egg)
    lay = lay_sleepy(k, ep, unit(0.36, -1.0, 0.08), (0.027, 0.021), sleepy_open(lid_top=0.12, slope=-0.12))
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    hribling(*args())
