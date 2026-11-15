# Snubbull sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/snubbull.py -- <the export's Snubbull.glb> <out.glb>
#
# From the references: a small pink bulldog standing upright, nearly half of it head. A big round head, fullest at the
# cheeks; two big rounded ears standing up and out, their outer halves dark grey; small scowling eyes under a black brow
# sloping down to a small black nose; a broad lower jaw thrust out under the face, its top line the mouth, two bold
# white fangs standing up from its corners. A pale blue frill round the neck, scalloped along its lower edge. A body
# like a dress flaring to a zigzag hem, with round blue spots; short stubby arms; short legs and cream feet with a pink
# pad under the toes. (The brief's "black collar": the pictures show the frill pale blue.)
#
# Written to be grown from (granbull.py imports it): the scowling eye, the fangs, the nose, the colours and on_head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import (Kit, ell, tube, blade, sheet, hull, outward, srgb, inside, tri, unit, eye_look, polygon, args)

# --- colours ----------------------------------------------------------------------------------------------------------
PINK = "#F29CB4"
EAR_TIP = "#3E3F45"
BLUE = "#86C6EA"
FOOT, PAD = "#EFD3A6", "#E98AA0"
NOSE, INK = "#1E1A22", "#1C1820"
WHITE = "#FBFAF6"

# --- the frame (the skeleton's joints: body (0, 0, 0.15), head (0, -0.02, 0.27), arms (+-0.08, -0.02, 0.21),
#     ears (+-0.1, 0, 0.41), legs (+-0.05, 0, 0.1)) ---------------------------------------------------------------------
H = np.array([0, -0.025, 0.37])         # the head's middle
HR = np.array([0.138, 0.12, 0.122])      # a tenth over the reference's measure
JAW = H + np.array([0, -0.088, -0.078])  # the thrust-out lower jaw
JAW_R = np.array([0.08, 0.052, 0.054])


def hit(shape, o, d):
    """Where a ray from o along d first meets a part's own mesh."""
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return np.array(h[0]), np.array(h[1])


def on_head(x, z, h=H, hr=HR, lift=0.0):
    """The head's front point at (x, z) and its normal."""
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / hr ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def head_shape(V):
    """Round, fuller at the cheeks below the middle."""
    V = V.copy()
    V[:, 0] *= 1 + 0.12 * np.clip(-V[:, 2], 0, 1) * (1 - 0.5 * np.clip(-V[:, 2], 0, 1))
    return V


def add_head(k):
    k.add("head", ell(H, HR, deform=head_shape), PINK, "head")
    k.add("jaw", ell(JAW, JAW_R), PINK, "head")


# --- the ears: big, rounded, standing up and out, their outer halves dark ----------------------------------------------
EAR_C = np.array([0.155, 0.012, 0.52])
EAR_R = (0.066, 0.03, 0.078)
EAR_TILT = 30                            # degrees out from upright
EAR_D = np.array([math.sin(math.radians(EAR_TILT)), 0, math.cos(math.radians(EAR_TILT))])


def add_ears(k):
    k.add("ear", ell(EAR_C, EAR_R, rot=(0, EAR_TILT, 0)), PINK, "ear{s}", mirror=True)

    def dark(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        s = (Q - EAR_C) @ EAR_D
        return inside(0.008 - s, 0.003) * (np.linalg.norm(Q - EAR_C, axis=1) < 0.11) * (P[:, 2] > H[2] + 0.06)
    k.paint(dark, EAR_TIP)


# --- the body: a torso and a skirt flaring to a zigzag hem ------------------------------------------------------------
SKIRT_C = np.array([0, 0.004, 0.14])
SKIRT_R = np.array([0.122, 0.108, 0.082])


def flare(V):
    V = V.copy()
    s = 1 + 0.16 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def add_body(k):
    k.add("torso", ell((0, 0.0, 0.215), (0.085, 0.076, 0.07)), PINK, "body")
    k.add("skirt", ell(SKIRT_C, SKIRT_R, deform=flare), PINK, "body")
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8     # none straight ahead, so the feet show between two
        d = np.array([math.sin(a), -math.cos(a), 0])
        root = SKIRT_C + (0, 0, -0.05) + d * np.array([0.122, 0.108, 0])
        tip = SKIRT_C + (0, 0, -0.088) + d * np.array([0.148, 0.132, 0])
        k.add(f"hem{i}", blade([root, (root + tip) / 2, tip], [0.048, 0.03, 0.008], d, 0.45), PINK, "body")


SPOTS = [(-0.55, 0.16, 0.024), (0.5, 0.175, 0.022), (1.4, 0.15, 0.024), (-1.4, 0.15, 0.022),
         (2.3, 0.16, 0.024), (-2.3, 0.155, 0.024), (math.pi, 0.165, 0.022)]


def add_spots(k):
    """Round blue spots on the skirt: crisp patches cast in from round it."""
    for a, z, r in SPOTS:
        d = np.array([math.sin(a), -math.cos(a), -0.15])
        c = SKIRT_C + np.array([0, 0, z - SKIRT_C[2]]) + np.array([d[0] * 0.13, d[1] * 0.12, 0])
        k.decal(c, d, (r, r * 1.15), BLUE, eps=0.003, rings=10, segs=44)


# --- the frill: a pale blue ring round the neck, flaring down, scalloped along its lower edge -------------------------
def add_frill(k, z=0.262, r0=0.074, r1=0.118, drop=0.032, waves=11):
    def at(u, v):
        th = math.pi + 2 * math.pi * u             # its seam straight behind
        r = r0 + (r1 - r0) * v
        scallop = 0.5 + 0.5 * np.cos(waves * 2 * math.pi * u)
        zz = z - drop * v - 0.012 * v * scallop
        return np.stack([r * np.sin(th), -r * np.cos(th), zz], axis=-1)
    k.add("frill", outward(sheet(at, 121, 6, 0.008)), BLUE, "body", fuse=False, surface="fur")


# --- arms and legs: short, stubby -------------------------------------------------------------------------------------
def add_limbs(k):
    k.add("arm", tube([(0.055, -0.02, 0.22), (0.1, -0.042, 0.212), (0.135, -0.085, 0.192)], [0.038, 0.035, 0.032]),
          PINK, "arm{s}", mirror=True)
    k.add("leg", tube([(0.05, 0.0, 0.1), (0.055, -0.01, 0.06), (0.058, -0.02, 0.045)], [0.036, 0.034, 0.032]),
          PINK, "leg{s}", mirror=True)
    k.add("foot", ell((0.06, -0.042, 0.03), (0.044, 0.058, 0.032), rot=(-14, 0, 0)), FOOT, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.05, 0.0025) * (np.abs(P[:, 0]) > 0.015) * (P[:, 1] < 0.06), FOOT,
            on=["leg", "foot"])
    k.decal((0.06, -0.09, 0.026), unit(0.0, -1, -0.35), (0.026, 0.02), PAD, eps=0.003, rings=8, segs=40,
            mirror=True)


# --- the face ----------------------------------------------------------------------------------------------------------
def add_nose(k, at=None, r=(0.017, 0.011, 0.012)):
    p, n = on_head(0.0, 0.352)
    k.add("nose", ell(p + n * 0.004 if at is None else at, r, rot=(12, 0, 0)), NOSE, "head", fuse=False,
          surface="shell")


def fang(base, tip, half=0.012, out=(1, 0, 0)):
    """A bold white fang: a kite of four corners round its base up to a point."""
    base, tip = np.array(base, float), np.array(tip, float)
    d = unit(*(tip - base))
    a = np.array(out, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    pts = [base + a * half, base - a * half, base + b * half * 0.85, base - b * half * 0.85,
           base - d * 0.006, tip]
    return hull(pts)


def add_fangs(k):
    for s in (1, -1):
        base = np.array([0.046 * s, JAW[1] - 0.037, JAW[2] + 0.03])
        tip = base + np.array([0.012 * s, -0.008, 0.036])
        k.add(f"fang{'R' if s > 0 else 'L'}", fang(base, tip, 0.012, (s, 0, 0)), WHITE, "head", fuse=False,
              surface="shell")


def add_mouth(k):
    """The mouth: a dark line along the jaw's top, arching up over the middle."""
    p, n = hit(ell(JAW, JAW_R), JAW + (0, -0.2, 0.026), (0, 1, 0))
    def look(u, v):
        c = np.tile(srgb(PINK), (len(u), 1))
        c[(np.abs(v - (0.35 - 0.75 * u * u)) < 0.2) & (np.abs(u) < 0.92)] = srgb(INK)
        return c
    k.decal(p, unit(0, -1, 0.25), (0.05, 0.024), look, eps=0.003, rings=14, segs=64)


# The eyes: small and scowling, a white with a black pupil toward the nose, the top cut off by a black brow sloping
# down toward the nose. In an eye's own units (u outward, v up). Laid by hand, a side each.
BROW = (0.32, 0.5)                       # the cut: v = BROW[0] + BROW[1] * u


def clipped(brow=BROW, n=96):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > brow[0] + brow[1] * du:
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > brow[0] + brow[1] * t * du:
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


def scowl(white=WHITE, ink=INK, brow=BROW, band=0.36, pupil=(-0.18, -0.12, 0.42)):
    """The open eye: a white, a round black pupil, a black brow band along the cut top."""
    CW, CI = srgb(white), srgb(ink)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[np.hypot(u - pupil[0], v - pupil[1]) < pupil[2]] = CI
        c[v > brow[0] + brow[1] * u - band] = CI
        return c
    return eye


def lay_eyes(k, state, x=0.06, z=0.392, half=(0.031, 0.027), h=H, hr=HR, lid=PINK, look=None, brow=BROW):
    look = look or scowl(brow=brow)
    out = clipped(brow)
    f = eye_look(state, look, srgb(lid), srgb(INK), line=0.17)
    for s in (1, -1):
        p, n = on_head(x * s, z, h, hr)
        outline = polygon([(s * a, b) for a, b in out]) if state in ("Open", "Fierce") else None
        col = f if s == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(p, n, half, col, eps=0.004, outline=outline, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_limbs(k)
    add_head(k)
    add_ears(k)
    add_frill(k)
    add_spots(k)
    add_nose(k)
    add_fangs(k)
    add_mouth(k)
    k.build(voxel=0.0055, band=0.005)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
