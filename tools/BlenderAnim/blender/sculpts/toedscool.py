# Toedscool sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/toedscool.py -- <the export's Toedscool.glb> <out.glb>
#
# From the references: a pink mushroom-jellyfish head, a bulb fuller at its top, with round yellow spots (one on the
# front over the face, a great one on each side, one behind); its face low on the front: a broad drooping nose between
# two heavy-lidded eyes (white under a pink lid, a dark pupil at the bottom), a scowl of brow ridges over them; round
# its foot a wavy pink frill, pale underneath. It walks on two pale grey-white tentacles: the one on its left long,
# sweeping out along the ground to a flat spatula of a pad; the other short, hanging and curled up at its end.
#
# Written to be grown from (toedscruel.py imports it): the colours, a tentacle, a pad, a frill and the heavy-lidded eye
# are functions here; the calls that build Toedscool are under __main__.
# Skeleton: body (0, 0, 0.56), legL (-0.03, -0.01, 0.47), legR (0.03, 0, 0.47).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- the line's colours ----------------------------------------------------------------------------------------------
PINK, PINK_D, UNDER, LEG, YELLOW = "#F2B5A2", "#D98D80", "#EFE6D8", "#E4E0D4", "#F2DE2C"
WHITE, PUPIL, LIDLINE = "#F8F6F2", "#1E1A1C", "#5A3434"

# --- Toedscool's frame -------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.69])          # the bulb's middle
R = np.array([0.188, 0.182, 0.215])         # its radii
FLARE = 0.16                             # how much fuller its top is than its foot
VOXEL = 0.0055


def bulb(V):
    V = V.copy()
    V[:, :2] *= (1 + FLARE * V[:, 2:3])
    return V


def on(az, el, c=None, r=None, flare=None):
    """A point of the bulb at az degrees round from the front (+ toward +X) and el up, and its outward way."""
    c = C if c is None else c
    r = R if r is None else r
    flare = FLARE if flare is None else flare
    a, e = math.radians(az), math.radians(el)
    z = math.sin(e)
    w = math.cos(e) * (1 + flare * z)
    p = c + np.array([math.sin(a) * w * r[0], -math.cos(a) * w * r[1], z * r[2]])
    n = np.array([math.sin(a) * math.cos(e) / r[0], -math.cos(a) * math.cos(e) / r[1], z / r[2]])
    return p, n / np.linalg.norm(n)


def tentacle(k, name, pts, r0, r1, bone, colour=LEG, surface="skin", fuse=True):
    pts = np.array(pts, float)
    k.add(name, tube(pts, np.linspace(r0, r1, len(pts)), seg=16, per=8), colour, bone, fuse=fuse, surface=surface)


def pad(k, name, c, r, turn, bone, colour=LEG, surface="skin"):
    """A flat spatula of a pad at a tentacle's end, turned about the upright by turn degrees."""
    def flat(V):
        V = V.copy()
        V[:, 2] *= 1 - 0.35 * np.clip(V[:, 2], 0, 1)   # its sole flat, its top a little domed
        return V
    k.add(name, ell(c, r, rot=(0, 0, turn), deform=flat), colour, bone, surface=surface)


def frill(z0, r0, r1, drop, waves, lower):
    """A wavy frill round the foot: from r0 at z0 out to r1, falling drop, its edge waving waves times."""
    def at(u, v):
        th = 2 * math.pi * u                        # 0 at the back, the seam behind
        v = 0.015 + 0.97 * v
        wave = np.cos(waves * th)
        r = r0 + (r1 - r0) * v * (1 + 0.06 * wave * v)
        z = z0 - drop * v ** 1.6 * (1 + 0.5 * wave) - (0.006 if lower else 0)
        return np.stack([r * np.sin(th), r * np.cos(th), z], -1)
    return at


# The eye: an almond, its upper part under a heavy pink lid (u outward, v up)
ALMOND = [(1.0, 0.05), (0.7, -0.6), (0.0, -0.95), (-0.75, -0.65), (-1.0, -0.05), (-0.5, 0.55), (0.45, 0.6)]


def heavy_open(lid, ink, pupil_u=-0.2, lid_at=0.12):
    CL, CK, CW, CP = srgb(lid), srgb(ink), srgb(WHITE), srgb(PUPIL)
    def look(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u - pupil_u) / 0.36) ** 2 + ((v + 0.4) / 0.42) ** 2 < 1] = CP
        c[v > lid_at] = CL
        c[np.abs(v - lid_at) < 0.13] = CK
        return c
    return look


def lay_eyes(k, state, places, half, lid, ink, eps=0.0025):
    """Both eyes: places [(point, normal)] on +X then -X; Fierce drops the lid lower with a slant."""
    for s, (p, n) in zip((1, -1), places):
        base = heavy_open(lid, ink)
        if state == "Open":
            look = base
        elif state == "Fierce":
            CL, CK = srgb(lid), srgb(ink)
            def look(u, v, base=base):
                c = base(u, v)
                edge = -0.05 + 0.35 * u
                c[v > edge] = CL
                c[np.abs(v - edge) < 0.13] = CK
                return c
        else:
            look = eye_look(state, base, srgb(lid), srgb(ink), line=0.15)
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        out = ALMOND if s > 0 else [(-a, b) for a, b in ALMOND]
        k.decal(p, n, half, col, eps=eps, outline=polygon(out), rings=20, segs=56)


# --- Toedscool ----------------------------------------------------------------------------------------------------------
SPOTS = [(28, 38, 0.05), (96, 14, 0.072), (-98, 22, 0.066), (178, 28, 0.064), (-42, 58, 0.038)]
EYE_AZ, EYE_EL = 27.0, -25.0
EYE_HALF = (0.046, 0.038)


def toedscool(src, out):
    k = Kit(src, coat="scales")
    k.add("bulb", ell(C, R, deform=bulb, sub=5), PINK, "body")
    # the broad nose drooping from the brow between the eyes, and the brow ridges in a scowl over them
    np_, nn = on(0, -22)
    k.add("nose", ell(np_ + nn * 0.004 + np.array([0, 0, -0.03]), (0.032, 0.03, 0.062), rot=(12, 0, 0)), PINK, "body")
    for s in (1, -1):
        bp, bn = on(17 * s, -8)
        k.add("brow" + ("R" if s > 0 else "L"), ell(bp - bn * 0.008, (0.045, 0.02, 0.014), rot=(0, 20 * s, 0)),
              PINK, "body")
    # the frill round its foot, pale underneath, and the pale under side of the bulb
    k.add("frill", sheet(frill(0.505, 0.075, 0.2, 0.05, 6, False), 96, 10, 0.014), PINK, "body", fuse=False,
          surface="skin")
    k.add("frillunder", sheet(frill(0.505, 0.075, 0.197, 0.05, 6, True), 96, 10, 0.008), UNDER, "body", fuse=False,
          surface="skin")
    k.paint(lambda P: inside(P[:, 2] - 0.5, 0.004), UNDER, on=["bulb"])
    # the two tentacles: the left long, out along the ground to its pad; the right short and curled
    tentacle(k, "legL", [(-0.03, -0.01, 0.5), (-0.045, -0.035, 0.36), (-0.1, -0.07, 0.17), (-0.18, -0.085, 0.06),
                         (-0.25, -0.08, 0.03)], 0.026, 0.02, "legL")
    pad(k, "padL", (-0.29, -0.075, 0.022), (0.065, 0.042, 0.02), 8, "legL")
    tentacle(k, "legR", [(0.03, 0.0, 0.5), (0.05, -0.02, 0.37), (0.065, -0.03, 0.27), (0.085, -0.035, 0.205),
                         (0.115, -0.04, 0.2), (0.13, -0.04, 0.23)], 0.024, 0.017, "legR")
    # the spots, round and yellow
    for i, (az, el, r) in enumerate(SPOTS):
        p, n = on(az, el)
        k.decal(p, n, (r * 1.1, r * 1.1), YELLOW, eps=0.0018, rings=12, segs=48)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)

    places = []
    for s in (1, -1):
        p, n = on(EYE_AZ * s, EYE_EL)
        places.append((p, unit(*(n + np.array([0, -0.3, 0])))))
    k.eye_states(lambda state: lay_eyes(k, state, places, EYE_HALF, PINK, LIDLINE))
    k.export(out)


if __name__ == "__main__":
    toedscool(*args())
