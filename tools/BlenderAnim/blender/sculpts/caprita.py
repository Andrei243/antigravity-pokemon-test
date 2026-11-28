# Caprita (Carpathia No. 10057) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/caprita.py -- <the export's Caprita.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10057-Caprita.jpg) and its brief (CP-dex-2-mountains.md, 57):
# a small nimble chamois kid on long legs; tawny fur, a paler belly, a fluffy ridge along the back and a short tuft of a
# tail; a white face, a dark brown stripe from each side of the muzzle through the eye back to the ear, a white blaze
# between them down to a small black nose, the crown and the back of the head tawny; brown eyes with a white glint;
# two tiny black horns standing up from the crown and hooked back at their tips; pointed ears out to the sides, tawny
# with pale linings and dark tips; cloven hooves of grey stone, and pebbles clinging to its shins.
# The picture balances it on a single point of rock with its four feet drawn together; the rock is the picture's,
# not the creature's, and is left out (the game stands every model on the ground).
#
# Written to be grown from (capraneagra.py imports it with sibling("caprita")): the hooves, the pebbles, the horns, the
# face's stripe and the eyes are the functions below; the calls that build Caprita are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, tri, unit, eye_look, args, sibling

lp = sibling("lupling")          # the shared helpers: spike, ear, along, on_ell
spike, along, on_ell = lp.spike, lp.along, lp.on_ell

TAWNY, BELLY, WHITE, STRIPE = "#C68A52", "#E2B784", "#F6F3EC", "#4A3426"
HORN, STONE, STONE2, NOSE = "#24201F", "#8F8C88", "#6E6B68", "#1E1A1A"
EARIN, IRIS, INK = "#F0D6B4", "#6A4024", "#141012"


# --- shared by the line ----------------------------------------------------------------------------------------------
def hoof(k, name, x, y, r, h, colour, bone, split=0.0035):
    """A cloven hoof of stone: two flat-faced halves with a split between them down the front, a loose piece each."""
    for side, sx in (("o", 1), ("i", -1)):
        cx = x + sx * (r * 0.5 + split / 2)
        pts = []
        for a in np.linspace(0, 2 * math.pi, 9, endpoint=False):
            pts.append((cx + math.cos(a) * r * 0.52, y + math.sin(a) * r * 1.0, 0.0))
            pts.append((cx + math.cos(a) * r * 0.42, y + 0.15 * r + math.sin(a) * r * 0.75, h))
        pts.append((cx, y - r * 1.15, h * 0.25))
        k.add(f"{name}{side}", hull(pts), colour, bone, fuse=False, mirror=True, surface="shell")


def pebbles(k, name, at, size, colour, bone, seed):
    """Small stones clinging to a leg: low bumps fused into it, stone grey."""
    rs = np.random.RandomState(seed)
    for i, c in enumerate(at):
        c = np.array(c, float)
        s = size * (0.8 + 0.4 * rs.rand())
        k.add(f"{name}{i}", ell(c, (s, s * (0.8 + 0.3 * rs.rand()), s * 0.8), sub=2), colour, bone, mirror=True,
              surface="shell")


def horn(k, name, pts, radii, colour, bone="head", fuse=False, ridges=None, ridge_colour=None, mirror=True):
    """A horn swept through points; fused, it may carry ridges painted across it at shares of its length."""
    k.add(name, outward(tube(pts, radii, seg=18)), colour, bone, fuse=fuse, mirror=mirror,
          surface="shell")
    if fuse and ridges:
        P0 = np.array(pts, float)
        for a, b in ridges:
            def m(P, a=a, b=b):
                q = P.copy(); q[:, 0] = np.abs(q[:, 0])
                s, dist = along(q, P0)
                return inside(a - s, 0.004) * inside(s - b, 0.004) * (dist < max(radii) * 1.6)
            k.paint(m, ridge_colour, on=[name])


def stripe_mask(a, b, width, centre_gap):
    """The dark stripe of the face: within width of the segment from a (the muzzle's side) to b (the ear's root), on
    both sides, kept off the white blaze down the middle."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; L = np.linalg.norm(d); d /= L
    def mask(P):
        q = P.copy(); q[:, 0] = np.abs(q[:, 0])
        t = np.clip((q - a) @ d, 0, L)
        dist = np.linalg.norm(q - (a + np.outer(t, d)), axis=1)
        w = width * (0.7 + 0.3 * t / L)
        return inside(dist - w, 0.0025) * inside(centre_gap - np.abs(P[:, 0]), 0.002)
    return mask


def eye_stripe(HC, E, slope, width, ahead, behind, gap):
    """The dark stripe of the face as a band along the head's side through the eye's own place: rising toward the ear
    by slope, ahead and behind the eye by so much, kept off the white blaze down the middle."""
    e = np.array(E, float) - HC
    def mask(P):
        d = P - HC
        mid = e[2] + slope * (d[:, 1] - e[1])
        dy = d[:, 1] - e[1]
        return (inside(np.abs(d[:, 2] - mid) - width, 0.0025) * inside(-ahead - dy, 0.004) * inside(dy - behind, 0.004)
                * inside(gap - np.abs(P[:, 0]), 0.002))
    return mask


def brown_eye(iris, rim=0.88):
    """A brown eye: a dark rim, a black pupil, a white glint up and out and a small one low."""
    I, K, W = srgb(iris), srgb(INK), np.array([1.0, 1.0, 1.0])
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[np.hypot(u, v) > rim] = K
        c[np.hypot(u + 0.04, v + 0.02) < 0.44] = K
        c[((u - 0.18) / 0.2) ** 2 + ((v - 0.26) / 0.18) ** 2 < 1] = W
        c[np.hypot(u + 0.3, v + 0.4) < 0.1] = W
        return c
    return look


def lay_eyes(k, state, E, n, half, look, lid, line_col, outline=None):
    f = eye_look(state, look, srgb(lid), srgb(line_col), line=0.16)
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        out_ = None
        if outline is not None and state in ("Open", "Fierce"):
            out_ = lp.polygon([(side * a, b) for a, b in outline])
        k.decal(np.array(E) * m, np.array(n) * m, half, col, eps=0.0035, outline=out_, rings=20, segs=56)


# --- Caprita ----------------------------------------------------------------------------------------------------------
LX = 0.042
FY, HY = -0.082, 0.096
HC = np.array([0.0, -0.152, 0.468])      # the head's middle (its bone is at 0, -0.13, 0.45)
HR = np.array([0.06, 0.064, 0.061])


def caprita(src, out_path):
    k = Kit(src, coat="fur")

    # --- a light trunk on long legs: shoulders and thighs fused over the legs' tops ----------------------------
    k.add("barrel", ell((0, 0.005, 0.262), (0.058, 0.12, 0.084)), TAWNY, "body")
    k.add("chest", ell((0, -0.07, 0.285), (0.054, 0.062, 0.08)), TAWNY, "body")
    k.add("rump", ell((0, 0.08, 0.285), (0.058, 0.062, 0.078)), TAWNY, "body")
    k.add("neck", tube([(0, -0.075, 0.31), (0, -0.112, 0.385), (0, -0.135, 0.43)], [0.042, 0.036, 0.034]), TAWNY,
          "head")
    k.paint(lambda P: inside(P[:, 2] - 0.215, 0.006) * inside(np.abs(P[:, 0]) - 0.045, 0.01)
            * (P[:, 1] > -0.1) * (P[:, 1] < 0.12), BELLY, on=["barrel", "chest", "rump"])

    k.add("shoulder", ell((LX - 0.004, FY + 0.006, 0.23), (0.03, 0.038, 0.072)), TAWNY, "leg{s}", mirror=True)
    k.add("fore", tube([(LX, FY, 0.22), (LX + 0.002, FY - 0.004, 0.12), (LX + 0.002, FY - 0.002, 0.03)],
                       [0.026, 0.021, 0.02]), TAWNY, "leg{s}", mirror=True)
    k.add("thigh", ell((LX - 0.003, HY + 0.008, 0.24), (0.036, 0.052, 0.085)), TAWNY, "hind{s}", mirror=True)
    k.add("hind", tube([(LX, HY + 0.015, 0.2), (LX + 0.002, HY + 0.03, 0.115), (LX + 0.002, HY + 0.012, 0.03)],
                       [0.026, 0.021, 0.02]), TAWNY, "hind{s}", mirror=True)
    hoof(k, "fhoof", LX + 0.002, FY - 0.004, 0.019, 0.028, STONE, "leg{s}")
    hoof(k, "hhoof", LX + 0.002, HY + 0.01, 0.019, 0.028, STONE, "hind{s}")
    pebbles(k, "fpeb", [(LX + 0.018, FY - 0.012, 0.07), (LX - 0.012, FY - 0.016, 0.1), (LX + 0.019, FY - 0.006, 0.135)],
            0.009, STONE, "leg{s}", 3)
    pebbles(k, "hpeb", [(LX + 0.018, HY + 0.02, 0.075), (LX - 0.015, HY + 0.022, 0.06), (LX + 0.018, HY + 0.03, 0.13)],
            0.009, STONE, "hind{s}", 7)

    # --- the head: a white face, a tawny crown, a muzzle forward and down, a small black nose ----------------------
    k.add("head", ell(HC, HR), TAWNY, "head")
    k.add("muzzle", ell(HC + (0, -0.062, -0.031), (0.035, 0.054, 0.035), rot=(22, 0, 0)), WHITE, "head")
    k.add("nose", outward(ell(HC + (0, -0.112, -0.041), (0.015, 0.011, 0.011))), NOSE, "head", fuse=False,
          surface="skin")
    def crown(P):
        d = (P - HC) / HR
        top = inside(0.45 - d[:, 2] + 0.35 * np.clip(-d[:, 1], 0, None), 0.05) * inside(0.3 - np.abs(d[:, 0]), 0.05)
        back = inside(0.1 - d[:, 1], 0.06)
        return np.clip(np.maximum(top, back), 0, 1) * inside(np.linalg.norm(d, axis=1) - 1.3, 0.05)
    k.paint(lambda P: (1 - crown(P)) * inside(np.linalg.norm((P - HC) / HR, axis=1) - 1.25, 0.04), WHITE, on=["head", "muzzle"])
    de = unit(0.72, -0.64, 0.14)
    E = on_ell(HC, HR, de) + (0, 0, 0.001)
    k.paint(eye_stripe(HC, E, 0.55, 0.017, 0.045, 0.05, 0.014), STRIPE, on=["head", "muzzle"])
    k.decal(HC + (0.024, -0.082, -0.054), unit(0.6, -0.65, -0.45), (0.013, 0.0032), INK, up=unit(0, 0.4, 1), eps=0.003,
            rings=3, segs=24, mirror=True)

    # --- the ears out to the sides, the tiny horns up from the crown and hooked back -----------------------------
    lp.ear(k, (0.042, -0.145, 0.51), (0.104, -0.13, 0.556), 0.025, unit(0.25, 1, 0.4), TAWNY, EARIN, STRIPE,
           tip_from=0.78, lining_w=0.6)
    horn(k, "horn", [(0.016, -0.162, 0.515), (0.018, -0.16, 0.553), (0.02, -0.147, 0.575), (0.021, -0.128, 0.578),
                     (0.021, -0.116, 0.566)], [0.013, 0.011, 0.0105, 0.0095, 0.009], HORN)

    # --- the fluffy ridge along the back and the short tuft of a tail -------------------------------------------
    for i, (y, z, ln) in enumerate(((-0.06, 0.355, 0.035), (-0.02, 0.352, 0.04), (0.02, 0.35, 0.04), (0.06, 0.352, 0.035))):
        k.add(f"ridge{i}", spike((0, y, z - 0.01), (0, 0.8, 0.6), ln, 0.016, 0.01, side=(1, 0, 0)), TAWNY, "body",
              fuse=False, surface="fur")
    k.add("tail", spike((0, 0.13, 0.32), (0, 0.6, 0.8), 0.06, 0.02, 0.014), TAWNY, "tail", fuse=False, surface="fur")

    k.build(voxel=0.0045, smooth=3, band=0.006)

    # --- the eyes: brown, in the dark stripe --------------------------------------------------------------------
    look = brown_eye(IRIS)

    def eyes(state):
        lay_eyes(k, state, E, de, (0.016, 0.0145), look, STRIPE, EARIN)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    caprita(*args())
