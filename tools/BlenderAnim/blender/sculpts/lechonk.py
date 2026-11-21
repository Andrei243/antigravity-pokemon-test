# Lechonk sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lechonk.py -- <the export's Lechonk.glb> <out.glb>
# Written to be grown from: oinkologne.py keeps this frame (sibling("lechonk")): the snout and its nostrils, the
# half-lidded eye with the yellow mark under it, the hooves, the curly tail, the face mask painted round the head.
#
# From the references: a round dark slate-grey piglet that is one ball, the face the front of the ball; a brown mask
# over the front of the face, from the brow down round the snout to the chin and back past the eyes; a big pink snout
# standing forward out of the mask, its flat front with two big dark upright nostrils; half-lidded eyes, set well round
# the sides, a heavy brown lid over their top half, the white lower half with a big black pupil toward the snout; a
# yellow mark under each eye, a crescent along its lower edge ending in a little curl behind; two ears from the top of
# the head flopping forward and out, brown, darker grey along their upper edge; four stubby dark legs with pink hooves,
# the belly low; a thin dark tail curled in a loop, up behind.
# Reference ratios (3D picture, hooves to the tail's top): the ball 0.86 of the height across and deep; legs showing
# under it 0.08; the snout standing 0.12 out of the ball, 0.3 of the ball's width across; the eye's white 0.1 of the
# height wide; the ear 0.36 of the height long.
# The export's joints: body (0, 0, 0.14), head (0, -0.07, 0.16), ears (+-0.06, -0.08, 0.22), legs (+-0.05, -0.06,
# 0.08), hind legs (+-0.05, 0.06, 0.08), tail (0, 0.1, 0.17).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, eye_look, polygon, args, outward

# --- colours (the line's) -----------------------------------------------------------------------------------------------
GREY = "#4C525C"
MASK = "#8C6C52"
SNOUT = "#F08C9C"
NOSTRIL = "#2A1C22"
HOOF = "#F29CB0"
YELLOW = "#F0D27A"
WHITE = "#F6F4EE"
PUPIL = "#16141A"
INK = "#2A2226"

# --- the frame ----------------------------------------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.158])            # the ball's middle
BR = np.array([0.122, 0.132, 0.126])
VOXEL = 0.0036


def on_ball(c, r, az, z, lift=0.0):
    """The point of an ellipsoid at azimuth az (0 straight ahead, -Y; + toward +X) and height z, and its normal."""
    c, r = np.array(c, float), np.array(r, float)
    zn = np.clip((z - c[2]) / r[2], -0.999, 0.999)
    s = math.sqrt(1 - zn * zn)
    p = c + np.array([r[0] * s * math.sin(az), -r[1] * s * math.cos(az), z - c[2]])
    n = (p - c) / r ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


def mask_paint(k, c, r, col, az_half, el_mid, el_half, on, edge=0.03):
    """A face mask painted round a ball's middle: an oval in (azimuth, elevation) on its front."""
    c, r = np.array(c, float), np.array(r, float)
    def m(P):
        q = (P - c) / r
        az = np.arctan2(q[:, 0], -q[:, 1])
        el = np.arcsin(np.clip(q[:, 2] / np.maximum(np.linalg.norm(q, axis=1), 1e-6), -1, 1))
        return inside((az / az_half) ** 2 + ((el - el_mid) / el_half) ** 2 - 1, edge) * (q[:, 1] < 0.15)
    k.paint(m, col, on=on)


def snout(k, c, r, bone, color, nostril, tilt=-6, nostril_size=(0.22, 0.5), name="snout"):
    """A pig's snout: a flat round-ended oval standing forward; two big dark upright nostrils on its flat front."""
    c = np.array(c, float)
    k.add(name, ell(c, r, rot=(tilt, 0, 0), deform=boxy(2.8)), color, bone, surface="skin")
    t = math.radians(tilt)
    d = unit(0, -math.cos(t), math.sin(t))
    front = c + d * r[1] * 0.97
    for s in (1, -1):
        k.decal(front + np.array([0.4 * r[0] * s, 0, 0.0]), d, (r[0] * nostril_size[0], r[2] * nostril_size[1]),
                nostril, eps=0.002, rings=10, segs=36)
    return front, d


def lidded_eye(white, pupil, ink, rim=0.84, pu=-0.12, pv=-0.25, pr=0.5, glint=True):
    """The open eye of the line: white, a dark rim, a big pupil low and toward the snout (its top cut by the lid)."""
    W_, P_, I_ = srgb(white), srgb(pupil), srgb(ink)
    def look(u, v):
        c = np.tile(W_, (len(u), 1))
        c[u * u + v * v > rim ** 2] = I_
        c[((u - pu) / pr) ** 2 + ((v - pv) / (pr * 1.05)) ** 2 < 1] = P_
        if glint:
            c[((u - pu - 0.16) / 0.13) ** 2 + ((v - pv - 0.2) / 0.13) ** 2 < 1] = W_
        return c
    return look


def lid_outline(cut=0.18, slope=-0.12):
    """The eye's outline: its oval, its top cut straight across by the heavy lid (a little lower outward)."""
    line = polygon([(-2.0, -2.0), (2.0, -2.0), (2.0, cut + slope * 2), (-2.0, cut - slope * 2)])
    return lambda th: min(1.0, line(th))


def lay_eyes(k, state, E, n, half, look, lid, ink, cut=0.18, slope=-0.12, line=0.16):
    """Both eyes, each laid by hand (u outward on both, so the left's colour function and outline are mirrored)."""
    f = eye_look(state, look, srgb(lid), srgb(ink), line=line)
    o = lid_outline(cut, slope)
    for side in (1, -1):
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        out_l = o if side == 1 else (lambda th, g=o: g(math.pi - th))
        k.decal(np.array(E) * np.array([side, 1, 1.0]), np.array(n) * np.array([side, 1, 1.0]), half, col,
                eps=0.002, rings=20, segs=56, outline=out_l)


def under_mark(k, E, n, half, col, drop, tail_at):
    """The yellow mark under each eye: a crescent along the eye's lower edge and a round end behind it (two patches)."""
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        p = (np.array(E) + np.array([0, 0, -drop])) * m
        k.decal(p, np.array(n) * m, (half[0] * 0.95, half[1] * 0.42), col, eps=0.0018, rings=10, segs=40)
        q = (np.array(E) + np.array(tail_at)) * m
        k.decal(q, np.array(n) * m, (half[1] * 0.44, half[1] * 0.44), col, eps=0.0018, rings=8, segs=32)


def hoof(k, c, r, bone, color, name="hoof"):
    """A pink hoof under a leg: a flat round cap (mirrored)."""
    k.add(name, ell(c, r, deform=boxy(2.4)), color, bone, mirror=True, surface="shell")


def curl_tail(k, root, size, ex, r, color, bone="tail", name="tail", turns=1.15):
    """A thin tail out of the rump along ex, curled in a loop in the plane of ex and up; returns its end."""
    root = np.array(root, float); ex = unit(*ex); ez = np.array([0, 0, 1.0])
    c = root + ex * size * 1.5 + ez * size * 1.3
    th = np.linspace(0, 2 * math.pi * turns, 48)
    rr = size * (1.0 - 0.25 * th / th[-1])
    ang = math.pi + 0.6 - th
    loop = c[None] + (np.cos(ang) * rr)[:, None] * ex[None] + (np.sin(ang) * rr)[:, None] * ez[None] \
        + (th / th[-1] * size * 0.35)[:, None] * ex[None]
    pts = np.vstack([root, root + ex * size * 0.6 + ez * size * 0.35, loop[3:]])
    k.add(name, outward(tube(pts, [r] * len(pts), seg=14, per=3)), color, bone, fuse=False, surface="fur")
    return pts[-1]


def flop_ear(k, base, mid, tip, r, top, under, flat, bone, drop=0.006):
    """An ear flopping forward and out: a loose blade of the coat's colour over one of the lining's, so it shows
    grey above and the lining below (two thick overlapping solids, not plates)."""
    base, mid, tip = (np.array(p, float) for p in (base, mid, tip))
    pts = [base, mid, tip]
    k.add("ear", outward(blade(pts, [r[0], r[1], r[2]], flat, 0.45)), top, bone, fuse=False, mirror=True,
          surface="fur")
    low = [p + np.array([0, -0.002, -drop]) for p in pts]
    k.add("earlining", outward(blade(low, [r[0] * 0.92, r[1] * 0.95, r[2]], flat, 0.45)), under, bone, fuse=False,
          mirror=True, surface="fur")


# --- Lechonk -------------------------------------------------------------------------------------------------------------
def lechonk(src, out):
    k = Kit(src, coat="fur")
    # the ball
    k.add("body", ell(BC, BR), GREY, "body")
    k.add("bodyrump", ell(BC + (0, 0.04, -0.012), (0.116, 0.1, 0.11)), GREY, "body")
    k.add("bodychin", ell((0, -0.07, 0.1), (0.09, 0.07, 0.065)), GREY, "head")
    # legs: haunches in the ball, stubs of shin, pink hooves
    for nm, y, bone in (("leg", -0.062, "leg{s}"), ("hind", 0.066, "hind{s}")):
        k.add(nm + "haunch", ell((0.06, y, 0.068), (0.046, 0.05, 0.048)), GREY, bone, mirror=True)
        k.add(nm, tube([(0.062, y, 0.06), (0.064, y - 0.004, 0.026)], [0.031, 0.029]), GREY, bone, mirror=True)
        hoof(k, (0.064, y - 0.006, 0.011), (0.027, 0.03, 0.011), bone, HOOF, name=nm + "hoof")
    k.paint(lambda P: inside(P[:, 2] - 0.021, 0.0025) * (np.abs(P[:, 0]) > 0.025), HOOF,
            on=["leg", "hind"])
    # the brow over each eye: a low brown bulge
    az, ez = 0.62, 0.178
    E, en = on_ball(BC, BR, az, ez)
    bp, bn = on_ball(BC, BR, az - 0.03, ez + 0.024)
    k.add("brow", ell(bp - bn * 0.008, (0.036, 0.022, 0.012), rot=(0, -14, -math.degrees(az))), MASK, "head",
          mirror=True)
    # the brown mask over the face
    mask_paint(k, BC, BR, MASK, 0.86, -0.05, 0.62, ["body", "brow"], edge=0.015)
    # the snout
    snout(k, (0, -0.152, 0.128), (0.05, 0.042, 0.044), "head", SNOUT, NOSTRIL)
    # ears flopping forward and out from the head's top
    flop_ear(k, (0.06, -0.06, 0.245), (0.102, -0.112, 0.228), (0.122, -0.15, 0.172), (0.024, 0.03, 0.009),
             GREY, MASK, (0.45, 0.1, 1.0), "ear{s}")
    # the yellow mark under each eye (in every state)
    en2 = unit(en[0] * 1.1, en[1], en[2] * 0.6)
    half = (0.024, 0.025)
    under_mark(k, E + np.array([0.002, 0.003, 0.0]), en2, half, YELLOW, 0.02, (0.021, 0.012, -0.016))
    # the curly tail, up behind, its loop turned out to +X
    curl_tail(k, (0.0, 0.118, 0.215), 0.024, (0.5, 0.86, 0.0), 0.0085, GREY)
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.03)
    look = lidded_eye(WHITE, PUPIL, INK)
    k.eye_states(lambda s: lay_eyes(k, s, E, en2, half, look, MASK, INK))
    k.export(out)


if __name__ == "__main__":
    lechonk(*args())
