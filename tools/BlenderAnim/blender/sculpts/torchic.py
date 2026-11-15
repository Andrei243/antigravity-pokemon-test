# Torchic sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/torchic.py -- <the export's Torchic.glb> <out.glb>
# A round orange chick, its head as big as its body and grown into it: a crest of three yellow plumes laid up and
# back from the crown with a little orange tuft before them, a short yellow beak, bold black eyes with a purple sheen
# below and a white glint, set wide on the head's sides; a ruff of yellow down where head meets body, and a stubby
# wing of yellow points at each side; short pale legs, three toes forward and one back.
#
# Written to be grown from (Combusken and Blaziken import it with sibling("torchic")): the line's colours and its
# feathers, spikes, claws, ruffs, feet and eyes are the functions below; the calls that build Torchic are under
# __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, outward, axes, polygon, srgb, inside, smoothstep, tri, unit,
                       eye_look, args)

# --- the line's colours ---------------------------------------------------------------------------------------------
ORANGE, YELLOW, CREST, PALE, BEAK = "#EE7A2A", "#F8D03C", "#FBDF46", "#F4D88A", "#F7C04A"
INK, WHITE = srgb("#151219"), np.array([1.0, 1.0, 1.0])
PURPLE = srgb("#4A3A86")
VOXEL = 0.005


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def feather(base, d, length, width, thick=0.026, across=None, roll=0.0, root=0.45):
    """A plume: a flat pointed plate with a hard rim from base along d, widest a third of the way, its face across
    `across` (by default square to d and to the model's length, so the front and back cameras see its faces), turned
    about d by roll degrees."""
    d = unit(*d)
    ex = np.cross(d, (0, 1.0, 0)) if across is None else np.array(across, float)
    if np.linalg.norm(ex) < 1e-3:
        ex = np.array([1.0, 0, 0])
    ex = ex - d * (ex @ d); ex /= np.linalg.norm(ex)
    if roll:
        a = math.radians(roll)
        ex = ex * math.cos(a) + np.cross(d, ex) * math.sin(a)
    ts = np.linspace(0, 1, 15)[:-1]
    w = width * (root + (1 - root) * np.sin(math.pi * np.clip(ts * 1.4, 0, 1) * 0.5)) * (1 - ts) ** 0.75
    right = [(wi, t * length) for wi, t in zip(w, ts)]
    outline = right + [(0.0, length)] + [(-a, b) for a, b in right[::-1]]
    return slab(outline, base, ex, d, thick)


def spike(base, d, length, width, thick, across=None, belly=1.0):
    """A point of fur, a horn or a claw as one flat-faced solid: a kite at its root (width across, thick the other
    way) to its tip along d, full at a third of its length."""
    d = unit(*d)
    ex = axes(d)[0] if across is None else np.array(across, float)
    ex = ex - d * (ex @ d); ex /= np.linalg.norm(ex)
    n = np.cross(d, ex)
    b = np.array(base, float)
    pts = [b + ex * width, b - ex * width, b + n * thick, b - n * thick]
    m = b + d * length * 0.33
    pts += [m + ex * width * 0.85 * belly, m - ex * width * 0.85 * belly, m + n * thick * 0.85, m - n * thick * 0.85]
    pts.append(b + d * length)
    return hull(pts)


def claw(base, d, length, r, curl=(0, 0, -1), bend=0.35):
    """A curved claw: a tapering round tube from base along d, bending toward curl."""
    d = unit(*d); c = unit(*curl)
    b = np.array(base, float)
    mid = b + d * length * 0.55 + c * length * bend * 0.25
    tip = b + d * length * 0.9 + c * length * bend
    return outward(tube([b, mid, tip], [r, r * 0.72, r * 0.3]))


def ruff(c, r, n, depth, flare, rot=(0, 0, 0), up=False):
    """A ring of down with a jagged edge: an ellipsoid whose lower (or upper) half is drawn out into n points."""
    def deform(V):
        V = V.copy()
        ph = np.arctan2(V[:, 0], -V[:, 1])
        h = np.clip(V[:, 2] if up else -V[:, 2], 0, 1)
        w = 0.5 + 0.5 * tri(ph * n / (2 * math.pi))
        s = 1 + flare * h * w
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] += (depth if up else -depth) * h * w
        return V
    return ell(c, r, rot=rot, deform=deform)


def pointed(V):
    """A beak's deform: drawn to a point toward the front (-Y)."""
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.7 * t; V[:, 2] *= 1 - 0.7 * t
    return V


def foot(k, ankle, toe_len, toe_r, color, bone="leg{s}", spread=30, back_len=None, claws=None, claw_len=0.04,
         surface=None):
    """Three toes forward and one back from an ankle on the +X side, mirrored; claws: a colour for a claw at each
    toe's tip."""
    a = np.array(ankle, float)
    for i, ang in enumerate((-spread, 0, spread)):
        t = math.radians(ang)
        d = np.array([math.sin(t), -math.cos(t), -0.18])
        tip = a + d * toe_len
        k.add(f"toe{i}", tube([a, a + d * toe_len * 0.55, tip], [toe_r, toe_r * 0.95, toe_r * 0.85]), color, bone,
              mirror=True, surface=surface)
        if claws:
            k.add(f"toeclaw{i}", claw(tip - d * toe_r * 0.4, (d[0], d[1], 0.0), claw_len, toe_r * 0.85),
                  claws, bone, fuse=False, mirror=True, surface="shell")
    bl = back_len or toe_len * 0.5
    k.add("toeback", tube([a, a + np.array([0.0, bl, -0.012])], [toe_r * 0.95, toe_r * 0.8]), color, bone,
          mirror=True, surface=surface)


def eye_open(base=INK, iris=None, iris_c=(0.0, -0.4), iris_r=0.5, pupil=None, pupil_c=(0.0, 0.0), pupil_r=(0.36, 0.5),
             glint=(-0.18, 0.4, 0.25)):
    """An open eye's colour (u outward, v up, -1..1): a ground, an iris disc, a pupil, a white glint."""
    def eye(u, v):
        c = np.tile(base, (len(u), 1))
        if iris is not None:
            c[(u - iris_c[0]) ** 2 + (v - iris_c[1]) ** 2 < iris_r ** 2] = iris
        if pupil is not None:
            c[((u - pupil_c[0]) / pupil_r[0]) ** 2 + ((v - pupil_c[1]) / pupil_r[1]) ** 2 < 1] = pupil
        if glint is not None:
            c[(u - glint[0]) ** 2 + (v - glint[1]) ** 2 < glint[2] ** 2] = WHITE
        return c
    return eye


def lay_eyes(k, state, E, de, half, look_open, lid, ink, outline=None, rim=None, inset=0.8, eps=0.007):
    """Both eyes for a state: a patch a side at E (and its mirror) facing de. outline: (u, v) points of the eye's
    shape (u outward); rim: a colour for an outer patch the eye's own sits in (Open and Fierce only)."""
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        c, n = np.array(E, float) * m, np.array(de, float) * m
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        ol = None
        if outline is not None:
            ol = polygon([(side * u, v) for u, v in (outline if side == 1 else outline[::-1])])
        look = flip(eye_look(state, look_open, lid, ink))
        if rim is not None and state in ("Open", "Fierce"):
            k.decal(c, n, half, rim, eps=eps, outline=ol, rings=20, segs=56)
            k.decal(c, n, (half[0] * inset, half[1] * inset), look, eps=eps + 0.002, outline=ol, rings=20, segs=56)
        else:
            k.decal(c, n, half, look, eps=eps, outline=ol, rings=20, segs=56)


# --- Torchic's frame ------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.012, 0.395])          # the head's centre, a tenth over the measure
HR = np.array([0.14, 0.13, 0.122])
DE = unit(0.6, -0.78, 0.12)                  # where the eyes look out
EYE_HALF = (0.037, 0.046)


def torchic(src, out):
    k = Kit(src, coat="fur")

    # The body and the head, as big as one another, grown together with no neck
    k.add("body", ell((0, 0.01, 0.195), (0.108, 0.104, 0.108)), ORANGE, "body")
    k.add("head", ell(H, HR), ORANGE, "head")
    # The ruff of yellow down where they meet, its points hanging over the body
    k.add("ruff", ruff((0, -0.004, 0.29), (0.104, 0.1, 0.032), 9, 0.95, 0.32), YELLOW, "body")

    # Stubby wings: a yellow lump at each side with three points of down
    k.add("wing", ell((0.108, -0.004, 0.232), (0.032, 0.042, 0.046), rot=(0, -15, 0)), YELLOW, "wing{s}", mirror=True)
    for i, d in enumerate([(0.55, -0.35, -0.75), (0.8, 0.05, -0.6), (0.75, -0.45, -0.35)]):
        k.add(f"down{i}", spike((0.124, -0.006, 0.226), d, 0.05, 0.017, 0.016), YELLOW, "wing{s}", fuse=False,
              mirror=True)

    # Short pale legs, three toes forward and one back
    k.add("leg", tube([(0.05, 0.0, 0.14), (0.053, -0.006, 0.08), (0.055, -0.012, 0.03)], [0.023, 0.02, 0.019]), PALE,
          "leg{s}", mirror=True)
    foot(k, (0.055, -0.014, 0.022), 0.062, 0.0165, PALE)

    # The beak, short and wide
    k.add("beak", ell((0, -0.153, 0.37), (0.05, 0.044, 0.026), rot=(8, 0, 0), deform=pointed), BEAK, "head",
          fuse=False, surface="shell")

    # The crest: three yellow plumes up and back from the crown, a little orange tuft before them
    for i, (base, d, length, width) in enumerate([
            ((0.008, 0.035, 0.49), (0.14, 0.52, 0.84), 0.17, 0.044),
            ((0.012, 0.05, 0.48), (0.3, 0.85, 0.45), 0.13, 0.036),
            ((-0.012, 0.0, 0.5), (-0.12, -0.3, 0.95), 0.08, 0.026)]):
        k.add(f"plume{i}", feather(base, d, length, width), CREST, "head", fuse=False)
    k.add("tuft", feather((0.0, -0.02, 0.495), (0.05, -0.55, 0.85), 0.05, 0.022, thick=0.022), ORANGE, "head",
          fuse=False)

    k.build(voxel=VOXEL)

    E = on_ell(H, HR, DE)
    look = eye_open(INK, iris=PURPLE, iris_c=(0.05, -0.8), iris_r=0.55, glint=(-0.2, 0.38, 0.27))
    lid = srgb(ORANGE)
    k.eye_states(lambda state: lay_eyes(k, state, E, DE, EYE_HALF, look, lid, INK))
    k.export(out)


if __name__ == "__main__":
    torchic(*args())
