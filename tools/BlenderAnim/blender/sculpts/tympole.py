# Tympole sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tympole.py -- <the export's Tympole.glb> <out.glb>
#
# The design, from the references: a round near-black ball, a little flattened at the front and tilted up, where a
# big cream disc is its face (some four fifths of the ball across); on each side a headphone-like lobe, a grey band
# against the ball and a light blue cap outside it; two dark oval eyes, taller than wide, with a white glint, under
# brows drawn like little musical notes (a round head at the outer end, a stroke rising to the inner end: a worried
# look); a wide smile with its corners ticked up; under the ball a short black stub and a broad flat light blue
# paddle of a tail hanging down, a crease down its middle.
#
# Written to be grown from (Palpitoad, Seismitoad: sibling("tympole")): the line's way of making a knob (a dark
# band fused into the body and a crisp loose cap of light colour over it), its eye and its brow are one function each.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, unit, axes, eye_look, outward, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BLACK, CREAM, GREY, SKY = "#2E2E36", "#F2DBAA", "#6C6C74", "#5CC2EE"
INK, BROW = "#231E20", "#2C2426"

# --- the frame ------------------------------------------------------------------------------------------------------
B = np.array([0.0, 0.0, 0.245])            # the ball's middle (the body bone is at 0, 0, 0.241)
BR = np.array([0.104, 0.098, 0.102])
FACE_DIR = unit(0, -1, 0.26)               # the face disc looks forward and a little up
FACE_COS = 0.56                            # the disc's edge, as the cosine of its angle from FACE_DIR
FACE_HALF = 0.084                          # the disc's half width, seen along FACE_DIR
EYE_DIR = unit(0.40, -0.86, 0.36)
EYE_HALF = (0.0175, 0.0225)
BROW_DIR = unit(0.33, -0.70, 0.66)
MOUTH_DIR = unit(0, -0.985, 0.0)
VOXEL = 0.005


def ell_along(c, r, d, sub=4):
    """An ellipsoid whose third radius lies along d (the first two square to it, right-handed)."""
    V, F = ell((0, 0, 0), r, sub=sub)
    a, b = axes(d)
    d = unit(*d)
    return V[:, :1] * a + V[:, 1:2] * b + V[:, 2:3] * d + np.array(c, float), F


def on_ell(c, r, d, lift=0.0):
    """The point of an ellipsoid along direction d from its centre, and its outward normal there."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c, float) + d * s
    n = (p - c) / np.array(r) ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


def soft(ground, ink, d, edge):
    """ink where d is above nought, eased over edge either side, so a line's edge falls between a patch's rings."""
    m = np.clip(0.5 + d / (2 * edge), 0, 1)[:, None]
    return ground[None] * (1 - m) + ink[None] * m


# --- a knob: a dark band fused into the body, a crisp loose cap of light colour over it -----------------------------
def knob(k, name, base, d, band_r, band_off, cap_r, cap_off, bone, band=GREY, cap=SKY, mirror=True, sub=4,
         cap_surface="shell"):
    """
    base: where the knob stands on the body; d: the way it points out. band_r, cap_r: radii (across, across, along d)
    of the fused band and of the loose cap, each centred band_off and cap_off along d from base. The cap is set so
    that its rim meets the band some way up, which is the crisp line between them.
    """
    d = unit(*d)
    base = np.array(base, float)
    k.add(name + "band", ell_along(base + d * band_off, band_r, d, sub=sub), band, bone, mirror=mirror)
    k.add(name + "cap", outward(ell_along(base + d * cap_off, cap_r, d, sub=sub)), cap, bone, fuse=False,
          mirror=mirror, surface=cap_surface)


# --- an eye ---------------------------------------------------------------------------------------------------------
def dark_eye(ground=INK, glint=(0.2, 0.36), glint_r=0.3):
    """A dark oval eye with one white glint high on its outer side, no rim (Tympole's and Palpitoad's line)."""
    G, W = srgb(ground), np.ones(3)
    E = 0.05
    def eye(u, v):
        c = np.tile(G, (len(u), 1))
        gr = np.sqrt((u - glint[0]) ** 2 + (v - glint[1]) ** 2)
        m = np.clip(0.5 + (glint_r - gr) / (2 * E), 0, 1)[:, None]
        return c * (1 - m) + W[None] * m
    return eye


def brow_look(ground, ink):
    """A brow like a musical note: a round head at the outer end, a bold stroke rising from it to the inner end."""
    G, K = srgb(ground), srgb(ink)
    def look(u, v):
        # the stroke: from the head at the outer end (u 0.55, v -0.25) curving up to the inner end (u -0.8, v 0.35)
        t = np.clip((0.55 - u) / 1.35, 0, 1)
        line = -0.25 + 0.6 * t + 0.25 * np.sin(math.pi * t)
        d = 0.24 - np.abs(v - line)
        d = np.where((u > 0.62) | (u < -0.85), -1.0, d)
        hd = 0.42 - np.sqrt(((u - 0.5) / 1.0) ** 2 + ((v + 0.2) / 1.0) ** 2)
        return soft(G, K, np.maximum(d, hd), 0.06)
    return look


def smile_look(ground, ink, w=0.26):
    """A wide smile line, its corners ticked up."""
    G, K = srgb(ground), srgb(ink)
    def look(u, v):
        line = -0.22 + 0.62 * u * u
        d = w / 2 - np.abs(v - line)
        d = np.where(np.abs(u) > 0.86, -1.0, d)
        # the ticks at the corners, short strokes up and out
        for s in (-1, 1):
            tk = w / 2 - np.abs((u - s * 0.86) * 0.9 + s * (v - 0.4) * 0.45)
            tk = np.where((v < 0.2) | (v > 0.72) | (np.abs(u) < 0.7) | (np.abs(u) > 1.0), -1.0, tk)
            d = np.maximum(d, tk)
        return soft(G, K, d, 0.06)
    return look


# --- Tympole --------------------------------------------------------------------------------------------------------
def tympole(src, out):
    k = Kit(src, coat="fur")

    fd = np.array(FACE_DIR)
    def ball(V):
        # flattened over the face: what stands out past 0.8 along the face's way is pressed back
        V = V.copy()
        s = V @ fd
        ex = np.clip(s - 0.8, 0, None)
        return V - np.outer(ex * 0.6, fd)
    k.add("body", ell(B, BR, deform=ball), BLACK, "body")

    # The cream face disc, painted by its angle from the face's way (a crisp edge)
    def face(P):
        q = (P - B) / BR
        q /= np.linalg.norm(q, axis=1, keepdims=True)
        return inside(FACE_COS + 0.06 - q @ fd, 0.012)
    k.paint(face, CREAM)
    # over it a patch whose outline is the disc's exact edge (paint alone frays on the remeshed ball)
    fp, fn = on_ell(B, BR, FACE_DIR)
    k.decal(fp, FACE_DIR, (FACE_HALF, FACE_HALF), CREAM, eps=0.003, rings=40, segs=160)

    # The headphone lobes: a grey band against the ball, a light blue cap outside it
    knob(k, "lobe", (0.088, 0.012, 0.248), (1, 0.0, 0.05), (0.058, 0.056, 0.034), 0.006, (0.054, 0.052, 0.04),
         0.014, "ear{s}")

    # The short black stub under the ball, and the paddle of a tail hanging from it
    k.add("stub", tube([(0, 0.03, 0.17), (0, 0.04, 0.135)], [0.026, 0.02]), BLACK, "tail")
    paddle(k, (0.0, 0.045, 0.128), unit(0.18, 0.28, -1.0), 0.15, 0.05)

    # The face's marks, in every eye state: the brows and the smile
    bp, bn = on_ell(B, BR, BROW_DIR)
    k.decal(bp, bn, (0.019, 0.012), brow_look(CREAM, BROW), eps=0.0055, rings=20, segs=64, mirror=True)
    mp, mn = on_ell(B, BR, MOUTH_DIR)
    k.decal(mp + np.array([0, 0, -0.006]), unit(0, -1, 0.2), (0.03, 0.017), smile_look(CREAM, INK), eps=0.0055,
            rings=22, segs=72)

    k.build(voxel=VOXEL, band=0.004)

    ep, en = on_ell(B, BR, EYE_DIR)
    eye = dark_eye()
    def eyes(state):
        f = eye_look(state, eye, srgb(CREAM), srgb(INK), line=0.2)
        k.decal(ep, unit(0.26, -0.9, 0.34), EYE_HALF, f, eps=0.007, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


def paddle(k, root, d, length, half_w, color=SKY, bone="tail", w=(0.92, -0.38, 0.0), name="paddle", thick=0.016):
    """A broad flat tail: an oval narrow at its root, folded a little along its middle so the crease shows. Its face
    looks forward (and so back), turned some 25 degrees toward +X; w is the way across it."""
    d = unit(*d)
    w = unit(*w)
    w = w - d * (w @ d); w /= np.linalg.norm(w)
    n = np.cross(w, d)                     # the face's way (forward-ish)
    root = np.array(root, float)
    def at(u, v):
        u = 0.015 + 0.97 * u
        s = 2 * v - 1
        hw = half_w * np.sqrt(np.clip(1 - ((u - 0.56) / 0.46) ** 2, 0.0, 1))
        hw = np.maximum(hw, half_w * 0.34 * (1 - u))
        hw = np.maximum(hw, 0.004)
        fold = 0.18 * hw * np.abs(s)
        return (root[None, None] + d[None, None] * (length * u)[..., None] + w[None, None] * (hw * s)[..., None]
                + n[None, None] * fold[..., None])
    k.add(name, outward(sheet(at, 34, 17, thick)), color, bone, fuse=False, surface="shell")


if __name__ == "__main__":
    tympole(*args())
