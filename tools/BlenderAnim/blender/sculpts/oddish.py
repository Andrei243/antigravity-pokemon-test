# Oddish sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/oddish.py -- <the export's Oddish.glb> <out.glb>
#
# From the references: a round blue bulb of a body, a little wider than tall, domed on top and fuller below; two long
# oval feet hanging under it, pointing down and a little forward; small red eyes (a dark red rim, a white glint) set
# high and well apart; an open mouth between them and a little lower, flat along its top and round below, dark red
# with a pink tongue; and out of the crown five long pointed leaves in two greens with a darker vein, fanned up and
# over to the model's +X side, creased along the midrib and drooping at their tips.
#
# Written to be grown from (gloom.py and vileplume.py import it with sibling("oddish")): the line's colours, its
# bulb, its feet, its leaves, its eyes and its mouth are the functions below; the calls that build Oddish are under
# __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, args, outward

# --- the line's colours ------------------------------------------------------------------------------------------
BLUE = "#4C7CB4"
LEAF1, LEAF2, VEIN = "#4FA844", "#68BE52", "#2F7A2E"
EYE_RED, EYE_DARK, EYE_RIM = srgb("#E2343E"), srgb("#A81C2A"), srgb("#4A0E16")
INK = srgb("#1C2440")              # a shut eye's line on the blue
MOUTH, TONGUE = "#7E1A2A", "#F08494"
WHITE = np.array([1.0, 1.0, 1.0])

# --- Oddish's frame ------------------------------------------------------------------------------------------------
B = np.array([0.0, 0.0, 0.205])        # the bulb's middle
BR = np.array([0.136, 0.124, 0.108])   # its radii
CROWN = np.array([0.0, 0.012, 0.296])  # where the leaves spring from


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def bulb(flat=0.8, slim=0.1):
    """The unit sphere made the line's bulb: flatter underneath, a little narrower toward the top."""
    def f(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * flat, V[:, 2])
        s = 1 - slim * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= s; V[:, 1] *= s
        return V
    return f


def foot(k, top, bottom, radii, color, bone="leg{s}"):
    """A hanging oval foot from inside the body down to its round end, mirrored."""
    top, bottom = np.array(top, float), np.array(bottom, float)
    mid = top * 0.5 + bottom * 0.5 + np.array([0.004, -0.004, 0])
    k.add("foot", tube([top, mid, bottom], radii), color, bone, mirror=True)


def leafsheet(base, d, n, length, width, droop=0.25, fold=0.35, curl=0.0, thick=0.012, lift=0.0, narrow=1.0,
              tipcurl=None):
    """
    A long pointed leaf from base along d, its face toward n (made square to d), creased along the midrib (its edges
    raised toward n by fold of its half-width), its tip falling by droop of its length toward -z and bent sideways by
    curl. lift: the whole leaf moved along n (a vein laid on it). narrow: the width's share (a vein is a narrow leaf).
    tipcurl(v) -> (along n, along the side) of the tip's own curl, in lengths. A loose piece, wound outward.
    """
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    s = np.cross(n, d)
    base = np.array(base, float)
    def at(u, v):
        v = 0.012 + 0.976 * v
        c = 2 * u - 1
        w = width * np.sin(np.pi * np.clip(v, 0, 1) ** 0.72) ** 0.85 * narrow + 0.0015
        off_n = fold * np.abs(c) * w * (1 if narrow > 0.5 else 0) + lift
        off_s = c * w + curl * length * v ** 2
        P = (base[None, None, :] + d[None, None, :] * (length * v)[..., None]
             + s[None, None, :] * off_s[..., None] + n[None, None, :] * off_n[..., None]
             + np.array([0, 0, -1.0])[None, None, :] * (droop * length * v ** 2.2)[..., None])
        if tipcurl is not None:
            a, b = tipcurl(v)
            P = P + n[None, None, :] * (a * length)[..., None] + s[None, None, :] * (b * length)[..., None]
        return P
    return outward(sheet(at, 9 if narrow > 0.5 else 3, 30, thick))


def add_leaf(k, name, base, d, n, length, width, color, vein, bone, droop=0.25, fold=0.35, curl=0.0, thick=0.012,
             tipcurl=None, veins=True):
    """A leaf and its vein on both faces, loose, on the leaf surface."""
    k.add(name, leafsheet(base, d, n, length, width, droop, fold, curl, thick, tipcurl=tipcurl), color, bone,
          fuse=False, surface="leaf")
    if veins:
        for side, lift in (("f", thick / 2 + 0.0016), ("b", -thick / 2 - 0.0016)):
            k.add(f"{name}vein{side}", leafsheet(base, d, n, length, width, droop, fold, curl, 0.003, lift=lift,
                                                 narrow=0.1, tipcurl=tipcurl),
                  vein, bone, fuse=False, surface="leaf")


def round_eye(red=EYE_RED, dark=EYE_DARK, rim=EYE_RIM):
    """The line's small round eye: a red ball with a darker lower half, a dark rim and a white glint (u out, v up)."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(red, (len(u), 1))
        c[(v < -0.15) & (r < 0.8)] = dark * 0.5 + red * 0.5
        c[((u + 0.18) / 0.3) ** 2 + ((v - 0.3) / 0.28) ** 2 < 1] = WHITE
        c[r > 0.76] = rim
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, ink, state, eps=0.005):
    """The pair of eyes at the direction d (+X side) from the ellipsoid (c, r), mirrored."""
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16),
            eps=eps, rings=20, segs=56, mirror=True)


def open_mouth(k, c, r, d, half, inside_col=MOUTH, tongue=TONGUE):
    """An open mouth on the fused face: flat along its top, round below, a tongue in its lower part."""
    p, nrm = on_ell(c, r, d)
    CM, CT = srgb(inside_col), srgb(tongue)
    def look(u, v):
        col = np.tile(CM, (len(u), 1))
        col[(u / 0.62) ** 2 + ((v + 0.62) / 0.5) ** 2 < 1] = CT
        return col
    def shape(th):
        s = math.sin(th)
        return min(1.0, 0.42 / s) if s > 0 else 1.0
    k.decal(p, nrm, half, look, eps=0.004, outline=shape, rings=12, segs=48)


# --- the five leaves: (sideways from up toward +X, back from up, length in body heights, width, colour) -----------
LEAVES = [(-34, 12, 1.2, 0.19, LEAF2), (-4, 22, 1.36, 0.2, LEAF1), (30, 26, 1.42, 0.2, LEAF2),
          (60, 24, 1.32, 0.19, LEAF1), (88, 16, 1.15, 0.18, LEAF2)]


def oddish(src, out):
    k = Kit(src, coat="fur")
    H2 = 2 * BR[2]
    k.add("bulb", ell(B, BR, deform=bulb()), BLUE, "body")
    foot(k, (0.056, -0.004, 0.15), (0.072, -0.058, 0.044), [0.034, 0.043, 0.041], BLUE)

    for i, (side, back, L, w, col) in enumerate(LEAVES):
        a, b = math.radians(side), math.radians(back)
        d = unit(math.sin(a) * math.cos(b), math.sin(b), math.cos(a) * math.cos(b))
        n = np.array([0.0, -1.0, 0.55])
        base = CROWN - d * 0.03
        add_leaf(k, f"leaf{i}", base, d, n, L * H2, w * L * H2, col, VEIN, "leaves", droop=0.22, fold=0.32,
                 curl=0.04 * (1 if side > 0 else -1))

    # the mouth, open, between the eyes and a little lower
    open_mouth(k, B, BR, (0, -1, -0.04), (0.021, 0.019))
    k.build(voxel=0.005, smooth=4, band=0.006)

    lid = srgb(BLUE)
    def eyes(state):
        lay_eyes(k, B, BR, (0.42, -0.86, 0.26), (0.0175, 0.021), round_eye(), lid, INK, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    oddish(*args())
