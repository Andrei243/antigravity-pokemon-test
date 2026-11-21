# Petilil sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/petilil.py -- <the export's Petilil.glb> <out.glb>
#
# From the references: a pale yellow-green bulb like an onion, round below and drawn up to a point, a raised lobe down
# its front from the point to an arch just over the face; a white face in the lower front of the bulb under that arch;
# small upright dark red eyes with a white glint, set close and low; no mouth; three green leaves out of the point, the
# middle one tallest and two out to the sides, their faces to the front; a ruff of small dark green leaves under the
# bulb; and under it a pale green foot of round lumps like a little cloud.
# (The task called the bulb white and the body green: the pictures have a pale yellow-green bulb with a white face, and
# a pale green foot.)
#
# Written to be grown from (lilligant.py imports it with sibling("petilil")): the leaf, the eye and the bulb's way of
# making a lobed shape are the functions below; the calls that build Petilil are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, unit, eye_look, args, outward

# --- the line's colours -------------------------------------------------------------------------------------------
BULB, LOBE = "#CFE288", "#DDEC9C"
FACE = "#F8F8F4"
LEAF, RUFF = "#45A24E", "#2E8E3E"
FOOT = "#BEDC86"
RED, RED_DARK, RIM = srgb("#B42A2C"), srgb("#801A20"), srgb("#4A1014")
INK = srgb("#3A2A1A")
WHITE = np.array([1.0, 1.0, 1.0])

# --- Petilil's frame (joints: body (0, 0, 0.14), head (0, 0, 0.25)) -----------------------------------------------
C = np.array([0.0, 0.0, 0.158])          # the bulb's middle
R = np.array([0.092, 0.086, 0.088])
CROWN = np.array([0.0, 0.004, 0.256])    # where the leaves leave the point
EYE_D = (0.26, -0.92, -0.38)
EYE_HALF = (0.0105, 0.018)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def onion(point=0.55, rise=1.18):
    """The unit sphere made an onion: its upper half drawn in to a point and raised."""
    def f(V):
        V = V.copy()
        t = np.clip(V[:, 2], 0, 1)
        s = 1 - point * t ** 2
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * rise, V[:, 2])
        return V
    return f


def leaf_blade(root, d, n, length, width, thick=0.014, bend=0.12, cup=0.18):
    """
    A pointed leaf from root along d, its face toward n: widest a little under its middle, bending back toward its face
    side along its length and cupped across. A sheet, wound outward: a loose piece.
    """
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    s = np.cross(n, d)
    root = np.array(root, float)
    def at(u, v):
        v = 0.015 + 0.97 * v
        c = 2 * u - 1
        w = width * np.sin(np.pi * v) ** 0.75 * (1.15 - 0.4 * v)
        P = (root[None, None, :] + d[None, None, :] * (length * v)[..., None]
             + s[None, None, :] * (c * w)[..., None]
             - n[None, None, :] * (bend * length * v * v + cup * w * (c * c - 0.5))[..., None])
        return P
    return outward(sheet(at, 11, 30, thick))


def red_eye(fill=RED, dark=RED_DARK, rim=RIM):
    """The line's upright eye: red, deeper below, a dark rim and a white glint high toward the inside."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(fill, (len(u), 1))
        c[(v < -0.3) & (r < 0.8)] = dark
        c[((u + 0.15) / 0.26) ** 2 + ((v - 0.32) / 0.3) ** 2 < 1] = WHITE
        c[r > 0.76] = rim
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=INK, eps=0.004):
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16),
            eps=eps, rings=20, segs=56, mirror=True)


def crown_leaves(k, root, scale=1.0, bone="head", color=LEAF):
    """Three leaves out of the point: the middle one up, the others out to the sides, all facing the front."""
    front = (0, -1.0, 0.1)
    k.add("leafM", leaf_blade(root, (0, 0.12, 1), front, 0.15 * scale, 0.034 * scale), color, bone, fuse=False,
          surface="leaf")
    for sx in (1, -1):
        k.add("leaf" + ("R" if sx > 0 else "L"),
              leaf_blade(root + np.array([0.006 * sx, 0, -0.004]), (0.78 * sx, 0.08, 0.62), front, 0.13 * scale,
                         0.032 * scale), color, bone, fuse=False, surface="leaf")


def petilil(src, out):
    k = Kit(src, coat="leaf")
    k.add("bulb", ell(C, R, deform=onion()), BULB, "body")
    # the raised lobe down the front, from the point to the arch over the face
    k.add("lobe", ell((0.0, -0.05, 0.206), (0.056, 0.04, 0.058), rot=(-18, 0, 0), deform=onion(0.4, 1.0)), LOBE, "body")
    k.add("stalk", tube([(0, 0.004, 0.236), (0, 0.004, 0.262)], [0.016, 0.013]), LEAF, "head")
    crown_leaves(k, CROWN)
    # the white face under the arch, its sides falling straight
    def face(P):
        x, z = P[:, 0], P[:, 2]
        return (inside(np.abs(x) - 0.068, 0.003) * inside(z - (0.158 - 3.0 * x * x), 0.003)
                * inside(P[:, 1] + 0.035, 0.004) * inside(0.07 - z, 0.003))
    k.paint(face, FACE, on=["bulb"])
    # the ruff of small dark leaves under the bulb, pointing out and down, and the lumpy foot under it
    for i in range(7):
        a = 2 * math.pi * (i + 0.5) / 7
        d = unit(math.sin(a), -math.cos(a), -0.35)
        root = np.array([math.sin(a) * 0.038, -math.cos(a) * 0.036, 0.084])
        k.add(f"ruff{i}", leaf_blade(root, d, (0, 0, 1.0), 0.05, 0.026, thick=0.012, bend=-0.1, cup=0.1), RUFF,
              "body", fuse=False, surface="leaf")
    k.add("foot", ell((0, 0, 0.046), (0.058, 0.052, 0.04)), FOOT, "body")
    for i in range(6):
        a = 2 * math.pi * i / 6 + 0.3
        k.add(f"lump{i}", ell((math.sin(a) * 0.05, -math.cos(a) * 0.046, 0.034), (0.033, 0.032, 0.03), sub=3), FOOT,
              "body")
    k.build(voxel=0.0045, smooth=4, band=0.005)

    def eyes(state):
        lay_eyes(k, C, R, EYE_D, EYE_HALF, red_eye(), FACE, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    petilil(*args())
