# Xatu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/xatu.py -- <the export's Xatu.glb> <out.glb>
# Natu grown into a still, upright totem: the same green, yellow and red and the same half-lidded eye (natu.py,
# through sibling), a round green head with a long hooked yellow beak over a black jagged collar; a bell of a body
# wrapped in white folded wings that part at the top over a green chest bearing two red eye marks; a red and yellow
# slash on each wing's shoulder; a red band and a black hem round the wings' foot; the back green with yellow and red
# bands low down; two long red feathers falling from the back of the head; short red legs on pointed red feet with
# white toes.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, sheet, outward, srgb, inside, tri, unit, eye_look, polygon, args, sibling

n = sibling("natu")
p = sibling("pidgey")
GREEN, YELLOW, RED, BLACK, WHITE = n.GREEN, n.YELLOW, n.RED, n.BLACK, "#EEF1F4"
ORANGE = "#F2A72C"
INK, PUP = n.INK, n.PUP


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.012, 0.77])               # the head's centre, high over its joint (0, 0, 0.64)
    F.HR = np.array([0.158, 0.152, 0.15])
    F.C, F.CR = F.H, F.HR                           # for natu's eye_centre
    F.BODY = ((0, 0.015, 0.37), (0.19, 0.175, 0.31))
    F.YC = 0.015                                     # the body's axis
    F.R = 0.17                                     # the body's radius for its round coordinates
    F.EYEDIR = unit(0.7, -0.62, 0.27)
    F.EYE = (0.066, 0.036)
    F.BEAK = ((0, -0.205, 0.752), (0.052, 0.098, 0.046))
    F.BEAKTILT = 4
    F.BONE = "head"
    F.SIDE = 1.95                                   # the wings wrap the front to this angle round the axis (radians)
    F.HEMRED, F.HEMBLACK = 0.215, 0.165             # the red band's top, the black hem's top
    F.VTOP, F.VLOW = 0.655, 0.47                    # the green V: from the collar down to its point
    F.VOXEL = 0.0065
    return F


def bell(V):
    """The body: wider below than above, its foot flattened."""
    V = V.copy()
    t = (1 - V[:, 2]) / 2
    V[:, 0] *= 0.7 + 0.36 * t
    V[:, 1] *= 0.72 + 0.32 * t
    low = V[:, 2] < 0
    V[low, 2] = -(np.clip(-V[low, 2], 0, 1) ** 0.55)
    return V


def round_body(F, P):
    """(angle round the body's upright axis, 0 at the front, times its radius; z)."""
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - F.YC)), P[:, 2]


def hooked(V):
    V = n.blunt(V)
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 2] -= 0.42 * t ** 2.2
    return V


def body(k, F):
    c, r = F.BODY
    k.add("body", ell(c, r, deform=bell), GREEN, "body")
    k.add("neck", tube([(0, 0.0, 0.6), (0, -0.005, 0.7)], [0.11, 0.1]), GREEN, "head")
    k.add("head", ell(F.H, F.HR), GREEN, "head")


def cloak(k, F):
    """The white folded wings, their stripes, the hem and the back's bands: paint round the body's axis, crisp."""
    pm = p.poly_mask
    def front(P):
        th, z = round_body(F, P)
        return inside(np.abs(th) - F.SIDE, 0.012) * (P[:, 2] < 0.69) * (P[:, 2] > 0.04)
    k.paint(lambda P: front(P) * inside(P[:, 2] - 0.665, 0.0025), WHITE, on=["body"])
    # the green V where the wings part, pointed below
    def vee(P):
        th, z = round_body(F, P)
        w = 0.95 * np.clip((z - F.VLOW) / (F.VTOP - F.VLOW), 0, 1) ** 0.85
        return inside(np.abs(th) - w, 0.02) * inside(F.VLOW - z, 0.0025) * (P[:, 1] < F.YC)
    k.paint(vee, GREEN)
    # where the two wings meet down the front
    k.paint(lambda P: front(P) * inside(np.abs(P[:, 0]) - 0.0035, 0.0018) * (P[:, 1] < 0)
            * inside(P[:, 2] - F.VLOW, 0.003), "#9AA2AE")
    # the hem: a red band over a black foot, its lower edge in points
    k.paint(lambda P: front(P) * inside(P[:, 2] - F.HEMRED, 0.0025), RED)
    k.paint(lambda P: front(P) * inside(P[:, 2] - F.HEMBLACK - 0.012 * tri(round_body(F, P)[0] * F.R / 0.05), 0.0025),
            BLACK)
    # the slash on each wing's shoulder: red, a yellow stripe down its middle
    for s in (1, -1):
        out_ = [(s * 0.15, 0.63), (s * 0.235, 0.585), (s * 0.235, 0.5), (s * 0.15, 0.545)]
        mid = [(s * 0.17, 0.6), (s * 0.218, 0.574), (s * 0.218, 0.53), (s * 0.17, 0.556)]
        k.paint(lambda P, o=out_: pm(round_body(F, P)[0] * F.R, P[:, 2], o) * (P[:, 1] < 0.08), RED)
        k.paint(lambda P, o=mid: pm(round_body(F, P)[0] * F.R, P[:, 2], o) * (P[:, 1] < 0.08), ORANGE)
    # the back: green, banded yellow, red and yellow low down
    def back(P):
        th, z = round_body(F, P)
        return inside(F.SIDE - np.abs(th), 0.012)
    for z0, z1, col in ((0.12, 0.155, YELLOW), (0.18, 0.212, RED), (0.237, 0.27, YELLOW)):
        k.paint(lambda P, a=z0, b=z1: back(P) * inside(P[:, 2] - b, 0.0025) * inside(a - P[:, 2], 0.0025), col)


def collar(k, F):
    """A black ruff round the neck, its lower edge in points, the longest down the front."""
    def at(u, v):
        uu = 0.012 + 0.976 * u
        a = math.pi * (1 + 2.0 * uu)               # its seam at the back
        pts = 0.5 + 0.5 * tri(a * 7 / (2 * math.pi))
        r = 0.098 + v * (0.148 - 0.098)
        z = 0.705 - v * (0.072 + 0.04 * pts * (0.5 + 0.5 * np.cos(a))) - 0.01 * v * v
        rr = r + 0.012 * v * pts
        return np.stack([np.sin(a) * rr, -0.002 - np.cos(a) * rr, z], axis=-1)
    k.add("collar", sheet(at, 140, 8, 0.012), BLACK, "head", fuse=False)


def chest_eyes(k, F):
    """Two red eye marks on the green chest: a red almond, a black rim, a dark pupil."""
    RED_, DARK = srgb(RED), srgb("#3A0C18")
    def mark(u, v):
        c = np.tile(RED_, (len(u), 1))
        c[(u / 0.34) ** 2 + (v / 0.62) ** 2 < 1] = DARK
        c[u * u + v * v > 0.68] = INK
        return c
    ALM = [(-1.0, 0.05), (-0.6, 0.62), (0.0, 0.8), (0.6, 0.62), (1.0, 0.05), (0.6, -0.55), (0.0, -0.72), (-0.6, -0.55)]
    for s in (1, -1):
        d = unit(0.35 * s, -1, 0.12)
        c = np.array([0.05 * s, -0.12, 0.58])
        k.decal(c, d, (0.042, 0.026), mark, eps=0.004, outline=polygon(ALM), rings=12, segs=48)


def ribbons(k, F):
    """Two long red feathers falling from the back of the head, out and back, their faces to the back and side."""
    for s in (1, -1):
        top = F.H + np.array([0.06 * s, 0.11, 0.02])
        bot = np.array([0.125 * s, 0.27, 0.3])
        d = bot - top
        L = np.linalg.norm(d); d /= L
        nrm = unit(0.62 * s, 0.78, 0.0)
        nrm = nrm - d * (nrm @ d); nrm /= np.linalg.norm(nrm)
        w = np.cross(nrm, d)
        out_ = [(0, 0.022), (0.5 * L, 0.03), (L, 0.034), (L + 0.02, -0.034), (0.5 * L, -0.03), (0, -0.022)]
        k.add("ribbon", slab(out_, top, d, w, 0.026), RED, "head", fuse=False)


def legs(k, F):
    """Short red legs on pointed red feet turned out, their toes white."""
    for s, side in ((1, "R"), (-1, "L")):
        x = 0.055 * s
        k.add(f"leg{side}", tube([(x, 0.0, 0.12), (x * 1.08, -0.005, 0.03)], [0.022, 0.02]), RED, f"leg{side}",
              fuse=False, surface="scales")
        a = math.radians(24) * s
        d = unit(math.sin(a), -math.cos(a), -0.05)
        fc = np.array([x * 1.1, -0.01, 0.024]) + d * 0.035
        k.add(f"foot{side}", outward(n.oriented_ell(fc, (0.03, 0.068, 0.024), d)), RED, f"leg{side}", fuse=False,
              surface="scales")
        k.add(f"toe{side}", outward(n.oriented_ell(fc + d * 0.06 + np.array([0, 0, -0.004]), (0.022, 0.024, 0.017), d)),
              WHITE, f"leg{side}", fuse=False, surface="shell")


def xatu(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    cloak(k, F)
    chest_eyes(k, F)
    n.beak(k, F, deform=hooked)
    collar(k, F)
    ribbons(k, F)
    legs(k, F)
    k.build(voxel=F.VOXEL, band=0.004)
    k.eye_states(n.eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    xatu(*args())
