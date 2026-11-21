# Smoliv sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/smoliv.py -- <the export's Smoliv.glb> <out.glb>
#
# From the references: a yellow-green body shaped like an olive, broad below and narrowing to its top, on two tiny
# pointed nubs of feet; the face low on the body itself: two small black eyes with a big white glint, set wide, and a
# pink wavy mouth between and a little below them; on top a pale yellow olive, a little tipped toward the front, with a
# dark socket where its stem was; round the olive's foot a calyx of fleshy green leaves: one out to each side, one
# behind, a small one up in front, and one hanging down the front of the body like a tongue.
# (The task called the body pale with an olive for a head: the pictures put the face on the green body, and the yellow
# olive on top has no face.)
#
# Written to be grown from (dolliv.py and arboliva.py import it with sibling("smoliv")): the olive with its socket, the
# fleshy leaf, the eye's look and the mouth are the functions below; the calls that build Smoliv are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward

# --- the line's colours -------------------------------------------------------------------------------------------
BODY = "#B4CC4C"                  # Smoliv's yellow-green
OLIVE_Y, SOCKET, SOCKET_RIM = "#E6DD72", "#5E5222", "#B8AA48"
LEAF, LEAF_LIGHT = "#4C9E3A", "#6CB84A"
MOUTH, MOUTH_RIM = srgb("#E08088"), srgb("#8A3444")
INK = srgb("#1A1A18")
WHITE = np.array([1.0, 1.0, 1.0])

# --- Smoliv's frame (joints: body (0, 0, 0.12), head (0, 0, 0.27), legs (+-0.04, 0, 0.04); about 0.36 tall) --------
C = np.array([0.0, 0.0, 0.13])           # the body's middle
R = np.array([0.104, 0.095, 0.126])
OC = np.array([0.0, 0.01, 0.298])        # the olive on top
OR = (0.058, 0.058, 0.062)
O_AXIS = unit(0.12, -0.32, 1.0)
EYE_D = (0.5, -0.83, -0.42)
EYE_HALF = (0.0155, 0.0165)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def olive_body(top=0.3):
    """The unit sphere made an egg: drawn in evenly toward its top, its bottom flattened to sit."""
    def f(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 0.4) / 1.4, 0, 1)
        V[:, 0] *= 1 - top * t ** 1.5
        V[:, 1] *= 1 - top * t ** 1.5
        V[:, 2] = np.where(V[:, 2] < -0.75, -0.75 - (V[:, 2] + 0.75) * 0.4, V[:, 2])
        return V
    return f


def frame(axis):
    """A right-handed frame (a, b, axis) round an axis."""
    d = np.array(axis, float); d /= np.linalg.norm(d)
    a = np.cross(d, (0, 1.0, 0)) if abs(d[1]) < 0.9 else np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    return a, np.cross(d, a), d


def olive(k, name, c, r, axis, colour, bone, fuse=True, socket=(SOCKET, SOCKET_RIM), surface=None):
    """
    An olive: an ellipsoid along its axis (r: across, across, along), with the dark socket of its stem at the axis's
    far end (a pale rim round a dark middle, loose discs standing just proud). Fused to the body by default.
    """
    a, b, d = frame(axis)
    c = np.array(c, float)
    V, F = ell((0, 0, 0), r, sub=4)
    M = np.stack([a, b, d], axis=1)
    shape = (V @ M.T + c, F)
    k.add(name, shape if fuse else outward(shape), colour, bone, fuse=fuse, surface=surface)
    if socket:
        end = c + d * r[2]
        for i, (col, rad, lift) in enumerate(((socket[1], 0.42, 0.0015), (socket[0], 0.27, 0.003))):
            Vd, Fd = ell((0, 0, 0), (r[0] * rad, r[1] * rad, 0.006), sub=3)
            k.add(f"{name}socket{i}", outward((Vd @ M.T + end - d * (0.005 - lift), Fd)), col, bone, fuse=False,
                  surface=surface)


def fleshy_leaf(root, d, n, length, width, thick, bend=0.0, cup=0.0):
    """
    A thick fleshy pointed leaf from root along d, its face toward n: a flattened ball drawn to a point, widest a
    little under its middle, bending toward n by bend (a share of its length) and cupped across. A loose piece.
    """
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    s = np.cross(n, d)
    root = np.array(root, float)
    V, F = ell((0, 0, 0), (1, 1, 1), sub=3)
    t = (V[:, 2] + 1) / 2
    env = np.sin(np.pi * np.clip(t, 0, 1)) ** 0.7 * (1.15 - 0.35 * t)
    x = V[:, 0] * width * env
    y = V[:, 1] * thick * 0.5 * np.sqrt(np.clip(env, 0, None))
    along = t * length
    lift = bend * length * t * t + cup * (x / max(width, 1e-6)) ** 2 * width
    P = root + np.outer(along, d) + np.outer(x, s) + np.outer(y + lift, n)
    return outward((P, F))


def dot_eye(white=WHITE, ink=INK, glint=0.46):
    """The line's eye: black with a big white glint high toward the inside (u outward, v up)."""
    def look(u, v):
        c = np.tile(ink, (len(u), 1))
        c[((u + 0.12) / glint) ** 2 + ((v - 0.16) / glint) ** 2 < 1] = white
        return c
    return look


def wavy_mouth(ground, fill=MOUTH, rim=MOUTH_RIM, waves=1.5, amp=0.42, half=0.34, edge=0.14):
    """A wavy stroke across a patch of the ground's colour: the line's worried mouth."""
    g = srgb(ground) if isinstance(ground, str) else ground
    def look(u, v):
        mid = amp * np.sin(u * waves * 2 * math.pi)
        d = np.abs(v - mid)
        c = np.tile(g, (len(u), 1))
        stroke = (d < half + edge) & (np.abs(u) < 0.94)
        c[stroke] = rim
        c[(d < half) & (np.abs(u) < 0.88)] = fill
        return c
    return look


def smoliv(src, out_path):
    k = Kit(src, coat="leaf")
    # the body: an olive, broad below, the face on it
    k.add("body", ell(C, R, deform=olive_body(), sub=5), BODY, "body")
    # two tiny pointed nubs of feet, out to the sides and a little forward
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        k.add("foot" + side, tube([(0.05 * sx, -0.02, 0.03), (0.07 * sx, -0.034, 0.016), (0.08 * sx, -0.04, 0.011)],
                                  [0.018, 0.011, 0.0065]), BODY, "leg" + side)
    # the olive on top, a little tipped to the front so its socket shows
    olive(k, "olive", OC, OR, O_AXIS, OLIVE_Y, "head", surface="leaf")
    # the calyx of fleshy leaves round the olive's foot
    base = np.array([0.0, 0.006, 0.256])
    sky = (0, -0.35, 1.0)
    for sx in (1, -1):
        k.add("leafS" + ("R" if sx > 0 else "L"),
              fleshy_leaf(base + (0.03 * sx, 0, 0), (sx, 0.05, 0.34), sky, 0.12, 0.046, 0.026, bend=-0.14,
                          cup=0.25), LEAF, "head", fuse=False, surface="leaf")
    k.add("leafB", fleshy_leaf(base + (0, 0.03, 0), (0.15, 1.0, 0.55), (0, 0.4, 1.0), 0.09, 0.032, 0.02, bend=-0.1,
                               cup=0.25), LEAF, "head", fuse=False, surface="leaf")
    k.add("leafF", fleshy_leaf(base + (0.012, -0.03, 0.004), (0.35, -0.45, 0.85), (0.1, -1.0, 0.2), 0.072, 0.028, 0.02,
                               bend=-0.08, cup=0.2), LEAF_LIGHT, "head", fuse=False, surface="leaf")
    # the one hanging down the front like a tongue, ending well above the eyes
    k.add("leafD", fleshy_leaf((0.012, -0.048, 0.25), (0.2, -0.32, -1.0), (0.1, -1.0, 0.1), 0.072, 0.024, 0.02,
                               bend=0.08, cup=0.2), LEAF, "head", fuse=False, surface="leaf")
    # the wavy mouth, low between the eyes (in every state, so before the build)
    mp, mn = on_ell(C, R, (0.04, -1, -0.6))
    k.decal(mp, mn, (0.034, 0.013), wavy_mouth(BODY), eps=0.002, rings=28, segs=80)
    k.build(voxel=0.0038, smooth=4, band=0.004)

    ep, en = on_ell(C, R, EYE_D)
    lid = srgb(BODY)
    def eyes(state):
        k.decal(ep, en, EYE_HALF, eye_look(state, dot_eye(), lid, INK, line=0.16), eps=0.002, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    smoliv(*args())
