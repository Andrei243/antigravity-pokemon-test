# Scraggy sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scraggy.py -- <the export's Scraggy.glb> <out.glb>
#
# From the references: a small lizard that is mostly a great round orange-yellow head. Big round white eyes bulging
# from the head's front corners, a thick black rim and an upright black oval pupil; two nostril dots; a wide grimace of
# big white teeth with black lines between them, the lower jaw and everything under it pale cream. A short red tuft on
# the crown. A thin cream neck and chest with a red belly plate in bands; cream arms bent out at the elbow, the fists on
# the hips holding up its shed skin: baggy yellow trousers with folds round them and a ragged hem, the orange feet
# showing under it, and the skin's loose end trailing back and out to the side, ending in a zigzag.
#
# Written to be grown from (scrafty.py imports it with sibling("scraggy")): the line's way of making its eyes, its
# grimace of teeth, its belly plate in bands, its fists and its trousers of shed skin are the functions below; the calls
# that build Scraggy are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, hull, outward, srgb, inside, smoothstep, unit, polygon, tri,
                       eye_look, args)

# --- the line's colours ---------------------------------------------------------------------------------------------
ORANGE = "#F4AE3E"          # Scraggy's head and feet
CREAM = "#F6E7A6"           # the jaw, neck, chest and arms
SKIN = "#E3A033"            # the shed skin
SKIN_DARK = "#C98A2A"       # its folds
RED, RED_LINE = "#E0574E", "#A8332E"
CREST = "#E2564A"
TOOTH, TOOTH_LINE = "#F7F7F4", "#1E1F26"
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def round_eye(rim=0.82, pupil=(-0.08, 0.0), pupil_r=(0.3, 0.52), lid=None, lid_line=None):
    """A big round eye: a black rim, white, an upright black oval pupil. lid (a function of u giving v) closes the
    eye's top under a lid's colour (Scrafty's sleepy grey lid), lid_line its dark edge."""
    def eye(u, v):
        rho = np.sqrt(u * u + v * v)
        c = np.tile(INK, (len(u), 1))
        c[rho < rim] = WHITE
        d = ((u - pupil[0]) / pupil_r[0]) ** 2 + ((v - pupil[1]) / pupil_r[1]) ** 2
        c[(d < 1) & (rho < rim)] = INK
        if lid is not None:
            top = lid(u)
            c[v > top] = lid_line[0]
            c[(v > top + lid_line[2]) & (rho < 1.0)] = lid_line[1]
        return c
    return eye


def teeth_look(n, rim=0.12, gap=0.075, lower=None):
    """A grimace: a white bar of n teeth with thick black lines between them and a black rim round it. lower: v
    under which a second, lower row begins (a line across)."""
    edges = [-1 + 2 * (i + 1) / n for i in range(n - 1)]
    CT, CL = srgb(TOOTH), srgb(TOOTH_LINE)
    def look(u, v):
        c = np.tile(CT, (len(u), 1))
        for x in edges:
            c[np.abs(u - x) < gap] = CL
        if lower is not None:
            c[np.abs(v - lower) < gap * 1.2] = CL
        r = np.maximum(np.abs(u), np.abs(v) ** 1.0)
        c[(np.abs(u) > 1 - rim * 0.6) | (np.abs(v) > 1 - rim)] = CL
        return c
    return look


def grimace(k, c, n, half, normal=(0, -1, 0), gap=0.0055, rim=0.0055, wobble=None):
    """A grimace of n teeth: a black bar, a white bar inside it, and a black line laid between each two teeth as
    patches of their own (lines drawn inside one patch's colour step at its rings into a row of hairs). wobble(i)
    moves each line up or down a little (crooked teeth)."""
    c = np.array(c, float)
    n_ = unit(*normal)
    side = np.cross((0, 0, 1.0), n_); side /= np.linalg.norm(side)
    k.decal(c, n_, (half[0] + rim, half[1] + rim), TOOTH_LINE, eps=0.004, rings=10, segs=80,
            outline=rounded_rect(5))
    k.decal(c, n_, half, TOOTH, eps=0.0055, rings=12, segs=80, outline=rounded_rect(5))
    for i in range(n - 1):
        x = -half[0] + 2 * half[0] * (i + 1) / n
        z = wobble(i) if wobble else 0.0
        k.decal(c + side * x + np.array([0, 0, z]), n_, (gap, half[1] * 1.02), TOOTH_LINE, eps=0.007, rings=4,
                segs=40, outline=rounded_rect(8))


def rounded_rect(p=6.0):
    """An outline for a patch: a rectangle with rounded corners."""
    return lambda th: 1.0 / (abs(math.cos(th)) ** p + abs(math.sin(th)) ** p) ** (1 / p)


def plate_look(color, line, bands, width=0.09):
    """A belly plate: its colour cut across by dark lines at the v of each band's edge."""
    C, L = srgb(color), srgb(line)
    def look(u, v):
        c = np.tile(C, (len(u), 1))
        for b in bands:
            c[np.abs(v - b) < width] = L
        return c
    return look


def banded_plate(k, c, n, half, bands, color, line, width=0.0045, outline=None, widths=None):
    """A belly plate in bands: the plate a patch, each line across it a thin patch of its own laid over it (at the
    fraction v of the plate's half height, as wide as widths gives or the plate)."""
    c = np.array(c, float)
    n_ = unit(*n)
    up = np.cross(n_, np.cross((0, 0, 1.0), n_)); up /= np.linalg.norm(up)
    k.decal(c, n_, half, color, eps=0.004, rings=12, segs=60, outline=outline or rounded_rect(4))
    for i, v in enumerate(bands):
        w = widths[i] if widths else half[0] * 0.92
        k.decal(c + up * v * half[1], n_, (w, width), line, eps=0.0055, rings=4, segs=40, outline=rounded_rect(8))


def nostrils(k, c, n, dx, half=0.0075):
    for s in (1, -1):
        k.decal(np.array(c) + np.array([dx * s, 0, 0]), n, (half, half * 1.15), "#3A2410", eps=0.004, rings=4, segs=24)


def zigzag(n, lo, hi, phase=0.0):
    """A ragged hem: for the angle round a part, a height between lo and hi in n teeth."""
    def f(th):
        return lo + (hi - lo) * (tri(n * th / (2 * math.pi) + phase) + 1) / 2
    return f


def trousers(k, c, r, color, hem, ridges, bone="body", rim_z=None, rim_r=None, fold=SKIN_DARK):
    """Shed skin worn as baggy trousers: a squat round mass, folds round it, a rolled rim at its top, and a ragged hem
    (hem(theta) raises the bottom by that much at each angle round)."""
    c = np.array(c, float)
    def ragged(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        lift = np.array([hem(t) for t in th]) / r[2]
        low = np.clip(-V[:, 2], 0, 1)
        V[:, 2] = V[:, 2] + lift * low ** 1.5
        return V
    k.add("pants", ell(c, r, deform=ragged), color, bone)
    for z, gr in ridges:
        k.add("fold", ell((c[0], c[1], z), (r[0] * gr, r[1] * gr, 0.014)), color, bone)
    if rim_z is not None:
        k.add("rim", ell((c[0], c[1], rim_z), rim_r), color, bone)


def scraggy(src, out):
    k = Kit(src, coat="fur")

    # --- the head: a great ball, the face flattened a little, eyes on bulges at its front corners -------------------
    H, HR = np.array([0.0, -0.02, 0.385]), np.array([0.142, 0.13, 0.126])
    k.add("head", ell(H, HR), ORANGE, "head")
    k.add("face", ell((0, -0.068, 0.345), (0.118, 0.085, 0.075)), ORANGE, "head")
    k.add("jaw", ell((0, -0.045, 0.29), (0.108, 0.088, 0.045)), CREAM, "head")
    EB, EBR = np.array([0.112, -0.078, 0.392]), np.array([0.048, 0.046, 0.054])
    k.add("bulge", ell(EB, EBR), ORANGE, "head", mirror=True)
    # Under a line just below the teeth the head is cream all round
    k.paint(lambda P: inside(P[:, 2] - 0.304, 0.0025) * (P[:, 2] > 0.24), CREAM)

    # the red tuft on the crown: a short thick stub leaning a little back
    k.add("crest", outward(blade([(0, -0.012, 0.485), (0, -0.004, 0.522), (0, 0.012, 0.552)], [0.025, 0.022, 0.019],
                                 (1, 0, 0), 0.72)), CREST, "head", fuse=False, surface="skin")

    # --- neck, chest, the red plate in bands ------------------------------------------------------------------------
    k.add("neck", tube([(0, -0.012, 0.215), (0, -0.022, 0.29)], [0.046, 0.05]), CREAM, "body")
    k.add("chest", ell((0, -0.008, 0.18), (0.066, 0.056, 0.068)), CREAM, "body")
    banded_plate(k, (0, -0.062, 0.19), (0, -1, 0.05), (0.033, 0.058), (-0.42, 0.08, 0.55), RED, RED_LINE)

    # --- arms bent out at the elbow, fists on the hips holding the trousers up ------------------------------------
    SH, EL, WR = np.array((0.058, -0.012, 0.21)), np.array((0.105, 0.0, 0.172)), np.array((0.09, -0.04, 0.142))
    k.add("arm", tube([SH, (0.085, -0.004, 0.195), EL, WR], [0.023, 0.022, 0.022, 0.021]), CREAM, "arm{s}", mirror=True)
    k.add("fist", ell((0.088, -0.05, 0.13), (0.032, 0.032, 0.029)), CREAM, "arm{s}", mirror=True)

    # --- the trousers of shed skin: a squat mass with folds, a rolled rim under the fists, a ragged hem ------------
    trousers(k, (0, 0.0, 0.08), (0.108, 0.098, 0.068), SKIN, zigzag(7, 0.0, 0.024),
             [(0.058, 1.03), (0.095, 1.0)], rim_z=0.135, rim_r=(0.1, 0.09, 0.018))
    # the folds a shade darker in a thin line under each ridge
    for z in (0.045, 0.082):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.0035, 0.002) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.125),
                SKIN_DARK, on=["pants", "fold"])
    # the orange feet under the hem
    k.add("foot", ell((0.05, -0.06, 0.018), (0.036, 0.05, 0.02)), ORANGE, "leg{s}", mirror=True)
    k.add("leg", tube([(0.045, -0.02, 0.06), (0.05, -0.04, 0.025)], [0.035, 0.03]), SKIN, "leg{s}", mirror=True)

    # the skin's loose end: a broad flat tongue trailing back and out to +X, ending in a zigzag standing up
    TD = unit(0.6, 0.8, 0)
    k.add("tail", blade([(0.0, 0.07, 0.07), (0.04, 0.115, 0.055), (0.075, 0.15, 0.042)],
                        [0.055, 0.042, 0.032], (0, 0, 1), 0.6), SKIN, "tail")
    bolt = [(-0.01, -0.025), (0.055, -0.02), (0.045, 0.01), (0.085, 0.02), (0.07, 0.05), (0.105, 0.075),
            (0.04, 0.05), (0.05, 0.035), (0.01, 0.03), (0.02, 0.012), (-0.015, 0.01)]
    k.add("bolt", slab(bolt, (0.065, 0.14, 0.04), TD, (0, 0, 1), 0.032), SKIN, "tail", fuse=False)

    # --- the face: nostrils, the grimace -------------------------------------------------------------------------
    nostrils(k, (0, -0.152, 0.384), unit(0, -1, 0.1), 0.022)
    grimace(k, (0, -0.148, 0.328), 5, (0.08, 0.026))

    # --- the eyes: on the bulges, looking out and forward -----------------------------------------------------------
    de = unit(0.66, -0.74, 0.04)
    E = on_ell(EB, EBR, de)
    CO = srgb(ORANGE)
    look = round_eye(pupil=(-0.1, -0.02), pupil_r=(0.27, 0.5))
    def eyes(state):
        k.decal(E, de, (0.044, 0.05), eye_look(state, look, CO, INK, line=0.13), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0055, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    scraggy(*args())
