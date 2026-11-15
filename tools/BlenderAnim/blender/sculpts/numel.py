# Numel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/numel.py -- <the export's Numel.glb> <out.glb>
#
# From the references: a plump yellow camel calf on four stub legs, its belly hanging low; a thick short neck under a
# head that is mostly a great round cream muzzle, a small open pink mouth low on the muzzle; dopey half-shut eyes in
# thick grey-brown rings, the lid's dark line straight across, a small black pupil hanging under it; small round ears
# standing up at the top of the head with a little fold of fur between them; a green cap over the hump of its back
# with a jagged lower edge and a few green flecks under it; cream over the chest and belly. No tail shows.
# Reference ratios (3D picture): head with ears 0.47 of the height, the muzzle 0.32 of the height across, the belly
# 0.06 of the height off the ground, length nose to rump 1.0 of the height.
# The export's joints: body (0, 0, 0.2), head (0, -0.1, 0.26), ears (+-0.05, -0.13, 0.42), legs (+-0.07, -0.08, 0.12),
# hind (+-0.07, 0.09, 0.12), tail (0, 0.15, 0.2).
#
# Written to be grown from (camerupt.py imports it with sibling("numel")): the helpers and the eyes are functions, the
# build is under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, polygon, tri, args, outward

YELLOW, CREAM, GREEN, RING = "#F4C66E", "#F7EBC6", "#74C25A", "#8C8076"
WHITE, INK, PUPIL = "#FFFFFF", "#2A221E", "#1A1412"
MOUTH, MOUTHIN = "#6A2A30", "#E4788A"
EDGE = 0.0035


# --- helpers (the line's) ---------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre."""
    d = np.array(d, float)
    return np.array(c, float) + d / np.linalg.norm(d / np.array(r, float))


def normal_on(c, r, d):
    """The surface normal of an ellipsoid at its point in direction d."""
    p = on_ell(c, r, d) - np.array(c, float)
    n = p / np.array(r, float) ** 2
    return n / np.linalg.norm(n)


def oriented(c, axes_, r, deform=None, sub=4):
    """An ellipsoid about c whose radii r lie along the three axes given (unit vectors)."""
    V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
    return V @ np.stack(axes_, axis=0) + np.array(c, float), F


def spike(base, d, L, w, flat=0.65, sink=0.02):
    """A solid point of fur or rock: a kite section w across at its root (flat of it the other way), L long along d."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    o = np.array(base, float) - d * sink
    return hull([o + a * w, o - a * w, o + b * w * flat, o - b * w * flat,
                 o + d * L * 0.45 + a * w * 0.55, o + d * L * 0.45 - a * w * 0.55, o + d * (L + sink)])


# --- the eyes: white, a heavy lid straight across with its dark line, the pupil hanging under it ------------------------
def eye_open(lid, white=WHITE, pupil=PUPIL, ink=INK, line=0.1, slope=0.0, ring=None, ring_at=0.74, pr=(0.26, 0.34),
             rim=True):
    """u outward, v up. The lid covers the eye over v = line + slope*u (slope < 0: it falls toward the outer corner)."""
    L, W, P, K = srgb(lid), srgb(white), srgb(pupil), srgb(ink)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        edge = line + slope * u
        c[((u + 0.04) / pr[0]) ** 2 + ((v - (edge - pr[1] * 0.85)) / pr[1]) ** 2 < 1] = P
        c[v > edge] = L
        c[np.abs(v - edge) < 0.1] = K
        if ring is None and rim:
            c[np.hypot(u, v) > 0.83] = K
        return c
    return look


def eye_state(state, lid, ring=None, ring_at=0.74, ink=INK, **kw):
    """The eye's colour in a state; the ring (Numel's) stays whatever the eye does."""
    L, K = srgb(lid), srgb(ink)
    if state == "Fierce":
        kw = dict(kw)
        kw["slope"] = kw.get("slope", 0.0) + 0.45          # the lid falling toward the inner corner (u < 0)
        kw["line"] = kw.get("line", 0.1) - 0.05
        f = eye_open(lid, ring=ring, ink=ink, **kw)
    else:
        f = eye_look(state, eye_open(lid, ring=ring, ink=ink, **kw), L, K, line=0.14)
    def look(u, v):
        c = f(u, v)
        if ring is not None:
            r = np.hypot(u, v)
            c[r > ring_at] = srgb(ring)
        return c
    return look


def lay_eyes(k, state, centre, d, half, lid, ring=None, **kw):
    k.decal(centre, d, half, eye_state(state, lid, ring=ring, **kw), eps=0.004, rings=20, segs=56, mirror=True)


# --- Numel ----------------------------------------------------------------------------------------------------------------
BODY = ((0, 0.035, 0.195), (0.14, 0.18, 0.155))
H = np.array([0.0, -0.15, 0.41])
HR = np.array([0.088, 0.092, 0.084])
MUZ = np.array([0.0, -0.235, 0.385])
MUZR = np.array([0.085, 0.078, 0.082])
EYE_D = unit(0.78, -0.36, 0.42)
EYE_HALF = (0.036, 0.036)


def numel(src, out):
    k = Kit(src, coat="fur")

    # The body: a plump barrel, a chest under the neck, a hump over the back, a round rump
    k.add("body", ell(*BODY), YELLOW, "body")
    k.add("chest", ell((0, -0.09, 0.2), (0.12, 0.1, 0.135)), YELLOW, "body")
    k.add("hump", ell((0, 0.06, 0.255), (0.13, 0.15, 0.1)), YELLOW, "body")
    k.add("rump", ell((0, 0.12, 0.195), (0.135, 0.11, 0.135)), YELLOW, "body")

    # The neck, thick and short, and the head: a crown and the great muzzle in front
    k.add("neck", tube([(0, -0.08, 0.25), (0, -0.12, 0.32), (0, -0.14, 0.38)], [0.098, 0.08, 0.075]), YELLOW, "head")
    k.add("head", ell(H, HR), YELLOW, "head")
    k.add("muzzle", ell(MUZ, MUZR), CREAM, "head")
    # the fold of fur on the crown, between the ears
    for i, (y, z, r) in enumerate([(-0.19, 0.478, 0.028), (-0.155, 0.49, 0.03), (-0.12, 0.482, 0.027)]):
        k.add(f"fold{i}", ell((0, y, z), (r * 1.05, r, r * 0.8)), YELLOW, "head")
    # small round ears, standing up and a little out
    for s, bone in ((1, "earR"), (-1, "earL")):
        d = unit(0.35 * s, 0.1, 1.0)
        a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a)
        b = np.cross(d, a)
        k.add(f"ear{bone[-1]}", oriented(np.array((0.055 * s, -0.125, 0.475)) + d * 0.02, [a, b, d],
                                         (0.03, 0.02, 0.045)), YELLOW, bone)

    # The legs: stubs under fat haunches, wide round feet
    for name, y, bone in (("leg", -0.085, "leg{s}"), ("hind", 0.115, "hind{s}")):
        k.add(name + "top", ell((0.08, y, 0.12), (0.07, 0.08, 0.085)), YELLOW, bone, mirror=True)
        k.add(name, tube([(0.082, y, 0.12), (0.086, y - 0.005, 0.06)], [0.056, 0.054]), YELLOW, bone, mirror=True)
        k.add(name + "foot", ell((0.087, y - 0.008, 0.03), (0.057, 0.063, 0.03)), YELLOW, bone, mirror=True)

    # Cream over the chest's front and under the belly (not the legs)
    def belly(P):
        lim = 0.17 - 0.45 * (P[:, 1] + 0.08)
        return inside(P[:, 2] - lim, EDGE) * inside(np.abs(P[:, 0]) - (0.1 - 0.02 * (P[:, 1] > 0)), EDGE)
    k.paint(belly, CREAM, on=["body", "chest", "rump"])

    # The green cap over the hump: a jagged edge low on the sides, falling toward the rump; flecks under it
    def cap(P):
        zz = 0.27 - 0.17 * np.clip(P[:, 1], 0, 0.25) + 0.022 * tri(P[:, 1] / 0.09 + 0.25)
        front = P[:, 1] - (-0.005 + 0.012 * tri(np.abs(P[:, 0]) / 0.05))
        return inside(zz - P[:, 2], EDGE) * inside(-front, EDGE)
    k.paint(cap, GREEN)
    DIAMOND = polygon([(0, 1), (1, 0), (0, -1), (-1, 0)])
    BC, BR = BODY
    for d, h in (((0.78, -0.05, 0.4), (0.016, 0.014)), ((0.85, 0.25, 0.28), (0.013, 0.012)),
                 ((0.6, 0.55, 0.22), (0.014, 0.012))):
        c = on_ell(BC, BR, d)
        k.decal(c, normal_on(BC, BR, d), h, GREEN, eps=0.004, rings=6, segs=24, outline=DIAMOND, mirror=True)

    # The mouth: a small open oval low on the muzzle's front, pink within a dark rim
    MD = unit(0.0, -0.62, -0.78)
    mc = on_ell(MUZ, MUZR, MD)
    def mouth(u, v):
        c = np.tile(srgb(MOUTHIN), (len(u), 1))
        c[np.hypot(u, v) > 0.66] = srgb(MOUTH)
        return c
    k.decal(mc, normal_on(MUZ, MUZR, MD), (0.017, 0.012), mouth, eps=0.004, rings=8, segs=36)

    k.build(voxel=0.006, smooth=3, band=0.005)

    centre = on_ell(H, HR, EYE_D)
    n = normal_on(H, HR, EYE_D)
    k.eye_states(lambda st: lay_eyes(k, st, centre, n, EYE_HALF, YELLOW, ring=RING))
    k.export(out)


if __name__ == "__main__":
    numel(*args())
