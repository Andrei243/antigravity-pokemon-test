# Flabébé (the red flower) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/flabébé.py -- <the export's Flabébé.glb> <out.glb>
#
# From the references: a tiny white sprite sitting in the heart of a great red flower, which is the largest mass; the
# flower's five rounded petals lie open about a raised yellow heart, tilted a little toward the front; three thin black
# stamens curl up out of the heart, each with a yellow ball; a green stem under the flower with two long leaves falling
# down and apart. The sprite: a round white head with a ring of yellow puffs round its crown, two white wings like
# long rounded petals out of the head's sides, up and out; black oval eyes with a white glint; a red blush on each
# cheek; a small smile; a white chest with tiny arms that hug a yellow stalk ending in a ball before its chin; a little
# green body under the chest standing in the flower's heart.
#
# Written to be grown from (floette.py and florges.py import it with sibling("flabébé")): the colours, the petal, the
# stamen, the wing, the eye and the face's patches are the functions below; the calls that build Flabébé are under
# __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, srgb, unit, eye_look, args, outward, sibling

pt = sibling("petilil")          # its pointed leaf (leaf_blade)

# --- the line's colours -------------------------------------------------------------------------------------------
WHITE_ = "#F6F6EE"
RED, RED_BACK = "#E2404C", "#B42C38"
YELLOW, CROWN_Y = "#F2D658", "#F4CE46"
GREEN, LEAF_G = "#78C252", "#4E9E44"
WIRE = "#26242A"
BLUSH = "#E8506A"
EYE_BLACK, MOUTH_INK = srgb("#1E1E28"), srgb("#4A2420")
INK = srgb("#2A2228")
GLINT = np.array([1.0, 1.0, 1.0])

# --- Flabébé's frame (joints: body (0, 0, 0.225), head (0, 0, 0.235)) ----------------------------------------------
HEART = np.array([0.0, 0.0, 0.15])        # the flower's heart
FLOWER_N = unit(0, -0.3, 1)               # the flower's face, tilted a little to the front
HC = np.array([0.0, -0.002, 0.266])       # the head's middle
HR = np.array([0.052, 0.047, 0.048])
EYE_D = (0.4, -0.9, -0.06)
EYE_HALF = (0.0125, 0.018)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def flower_axes(n):
    """Two directions in the flower's plane: a toward +X, b toward the back."""
    a = np.array([1.0, 0, 0]) - n * n[0]
    a /= np.linalg.norm(a)
    b = np.cross(n, a)
    return a, b


def petal(k, name, centre, n, d, length, width, thick, colour, bone, cup=12.0, inset=0.0):
    """
    A rounded petal out of centre along d (in the plane of the flower's face n), narrow at its root and broad and round
    at its end, raised by cup degrees toward the face. A loose piece, wound outward.
    """
    c = math.radians(cup)
    dd = math.cos(c) * np.array(d, float) + math.sin(c) * np.array(n, float)
    dd /= np.linalg.norm(dd)
    e = np.cross(n, dd); e /= np.linalg.norm(e)
    nn = np.cross(dd, e)
    def fan(V):
        V = V.copy()
        t = (V[:, 0] + 1) / 2
        V[:, 1] *= 0.42 + 0.58 * np.sqrt(np.clip(t, 0, 1))
        V[:, 2] += 0.35 * V[:, 1] ** 2          # cupped across
        return V
    V, F = ell((0, 0, 0), (1, 1, 1), deform=fan, sub=3)
    V = V * np.array([length / 2, width, thick])
    M = np.stack([dd, e, nn], axis=1)
    V = V @ M.T + np.array(centre, float) + dd * (length / 2 + inset)
    k.add(name, outward((V, F)), colour, bone, fuse=False, surface="leaf")


def flower(k, centre, n, reach, width, thick, bone, cup=12.0, heart=0.045, prefix="", first=-90.0):
    """Five petals about a red hub, a raised yellow heart in the middle; one petal toward first (degrees in the plane)."""
    a, b = flower_axes(n)
    for i in range(5):
        t = math.radians(first + 72 * i)
        d = math.cos(t) * a + math.sin(t) * b
        petal(k, f"{prefix}petal{i}", centre, n, d, reach, width, thick, RED, bone, cup=cup, inset=0.006)
    V, F = ell((0, 0, 0), (reach * 0.62, reach * 0.62, thick * 0.8), sub=3)
    k.add(f"{prefix}hub", outward((V @ np.stack([a, b, n], axis=1).T + centre - n * thick * 0.4, F)), RED_BACK, bone,
          fuse=False, surface="leaf")
    if heart:
        k.add(f"{prefix}heart", lathe(centre, n, [(0, 0.0), (heart, 0.0), (heart * 1.08, heart * 0.22),
                                                 (heart * 0.8, heart * 0.42), (0, heart * 0.5)], seg=40),
              YELLOW, bone, fuse=False, surface="leaf")


def stamen(k, name, pts, wire, ball, bone):
    """A thin black stalk curling up and out, a yellow ball at its end."""
    k.add(name + "wire", outward(tube(pts, [wire] * len(pts))), WIRE, bone, fuse=False, surface="leaf")
    p, q = np.array(pts[-1], float), np.array(pts[-2], float)
    d = (p - q) / np.linalg.norm(p - q)
    k.add(name + "ball", outward(ell(p + d * ball * 0.6, (ball,) * 3, sub=3)), YELLOW, bone, fuse=False,
          surface="leaf")


def wing_outline(length, width, notch=0.0, n=40):
    """A long rounded petal from its root (a = 0) to its broad round end (a = length); notch cuts a heart's dip there."""
    pts = []
    for i in range(n):
        ph = 2 * math.pi * i / n
        if ph > math.pi:
            ph -= 2 * math.pi
        x = length / 2 * (1 + math.cos(ph))
        x -= notch * length * math.exp(-(ph / 0.32) ** 2)
        y = width * math.sin(ph) * (0.38 + 0.62 * (1 + math.cos(ph)) / 2)
        pts.append((x, y))
    return pts


def wing(k, name, root, d, length, width, thick, colour, bone, notch=0.0, mirror=True):
    """A flat wing or ear from root along d, its face to the front: a loose plate on the +X side, mirrored."""
    ex = unit(*d)
    ez = np.array([0, 0, 1.0]) - ex * ex[2]
    if np.linalg.norm(ez) < 1e-3:
        ez = np.array([0, -1.0, 0])
    ez /= np.linalg.norm(ez)
    k.add(name, slab(wing_outline(length, width, notch), root, ex, ez, thick), colour, bone, fuse=False,
          mirror=mirror, surface="skin")


def black_eye(fill=EYE_BLACK):
    """The line's eye: a solid dark oval with one white glint high up."""
    def look(u, v):
        c = np.tile(fill, (len(u), 1))
        c[((u - 0.12) / 0.34) ** 2 + ((v - 0.38) / 0.3) ** 2 < 1] = GLINT
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=INK, eps=0.002):
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16),
            eps=eps, rings=20, segs=56, mirror=True)


def blush(k, c, r, d, half, colour=BLUSH, eps=0.0015):
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, colour, eps=eps, rings=8, segs=36, mirror=True)


def smile(k, c, r, d, half, face, eps=0.0015):
    """A small closed smile, one clean curve on the face's own colour."""
    p, nrm = on_ell(c, r, d)
    f = srgb(face)
    def look(u, v):
        col = np.tile(f, (len(u), 1))
        curve = -0.25 + 0.75 * (1 - u * u)
        col[(np.abs(u) < 0.86) & (np.abs(v + curve - 0.25) < 0.34)] = MOUTH_INK
        return col
    k.decal(p, nrm, half, look, eps=eps, rings=10, segs=40)


def flabebe(src, out):
    k = Kit(src, coat="leaf")
    # --- the flower, its stem and leaves ---
    flower(k, HEART, FLOWER_N, 0.135, 0.056, 0.013, "body", cup=10, heart=0.044)
    a, b = flower_axes(FLOWER_N)
    for i, t in enumerate((-100, -150, 128)):
        w = math.cos(math.radians(t)) * a + math.sin(math.radians(t)) * b
        base = HEART + FLOWER_N * 0.012 + w * 0.02
        stamen(k, f"stamen{i}", [base, base + w * 0.04 + np.array([0, 0, 0.04]), base + w * 0.075 + np.array([0, 0, 0.052]),
                                 base + w * 0.098 + np.array([0, 0, 0.046])], 0.0052, 0.013, "body")
    k.add("stem", outward(tube([HEART - (0, 0, 0.01), (0.002, 0.008, 0.09), (0.008, 0.0, 0.03)], [0.011, 0.01, 0.009])),
          GREEN, "body", fuse=False, surface="leaf")
    for sx in (1, -1):
        k.add("leaf" + ("R" if sx > 0 else "L"),
              pt.leaf_blade((0.006 * sx, 0.004, 0.092), (0.5 * sx, -0.25, -1.0), (0.4 * sx, -1.0, 0.05), 0.085, 0.017,
                            thick=0.011, bend=0.05, cup=0.12), LEAF_G, "body", fuse=False, surface="leaf")
    # --- the sprite: its green body in the heart, white chest, tiny arms round the stalk ---
    k.add("lower", ell((0, 0.0, 0.178), (0.022, 0.02, 0.026)), GREEN, "body")
    k.add("chest", ell((0, -0.002, 0.208), (0.025, 0.022, 0.022)), WHITE_, "body")
    for sx in (1, -1):
        k.add("arm" + ("R" if sx > 0 else "L"),
              tube([(0.02 * sx, -0.006, 0.214), (0.022 * sx, -0.026, 0.206), (0.01 * sx, -0.036, 0.208)],
                   [0.0095, 0.009, 0.0095]), WHITE_, "body")
    # the yellow stalk it hugs, a ball before its chin on the +X side
    k.add("club", outward(tube([HEART + FLOWER_N * 0.015 + (0, -0.02, 0), (0.01, -0.042, 0.196), (0.024, -0.056, 0.224)],
                               [0.0075, 0.0075, 0.0072])), YELLOW, "body", fuse=False, surface="leaf")
    k.add("clubball", outward(ell((0.028, -0.06, 0.235), (0.017, 0.017, 0.017), sub=3)), YELLOW, "body", fuse=False,
          surface="leaf")
    # --- the head, its crown of puffs and its wings ---
    k.add("neck", ell((0, -0.002, 0.226), (0.02, 0.018, 0.016)), WHITE_, "head")
    k.add("head", ell(HC, HR), WHITE_, "head")
    cn = unit(0, 0.3, 1)
    ca, cb = flower_axes(cn)
    cc = HC + np.array([0, 0.006, 0.036])
    for i in range(12):
        t = 2 * math.pi * i / 12
        p = cc + 0.037 * (math.cos(t) * ca + math.sin(t) * cb)
        k.add(f"puff{i}", ell(p, (0.0145, 0.0145, 0.013), sub=3), CROWN_Y, "head")
    wing(k, "wing", (0.04, 0.008, 0.276), (1.0, 0.14, 0.42), 0.088, 0.03, 0.011, WHITE_, "head")
    # the face's marks, in every state
    blush(k, HC, HR, (0.66, -0.7, -0.32), (0.0095, 0.0065))
    smile(k, HC, HR, (0.0, -1.0, -0.36), (0.009, 0.006), WHITE_)
    k.build(voxel=0.0034, smooth=4, band=0.004, wband=0.03)

    def eyes(state):
        lay_eyes(k, HC, HR, EYE_D, EYE_HALF, black_eye(), WHITE_, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    flabebe(*args())
