# Inkay sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/inkay.py -- <the export's Inkay.glb> <out.glb>
#
# From the references: a little squid floating upside down. A round blue face, its lower half a bundle of short
# tentacles drawn together to a point below; two great round eyes high on it (white, a big grey-brown iris, a black
# pupil, two glints) and a small pink beak between them; over it a pink hood, a bell narrowing to a rounded top,
# spotted yellow (two big spots high on the front, a smaller one low on each side, more behind), set in a white
# pointed frame: a kite of white fin round the hood, its point over the top, its corners out at the sides, a white
# lip round the hood's lower edge. Two long white ribbon arms hang from under the hood's sides, each ending in a flat
# white paddle turned outward and down. The cap is opaque here (the task's note; the art draws it so too).
#
# Written to be grown from (malamar.py imports it with sibling): colours are passed, the shapes are functions.
# Bones: root, body, head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, hull, srgb, inside, unit, eye_look, args, outward, slab, ring_band, polygon

BLUE, BLUEDEEP, PINK, PINKDEEP, YELLOW, WHITE = "#5A8AD6", "#3E64AE", "#EE9AAA", "#D86A82", "#F2E46A", "#F4F4F6"
IRIS, PUPIL, INK = "#5C4E4A", "#16141A", "#1A1C2A"

X = np.array([1.0, 0, 0]); Z = np.array([0, 0, 1.0])

FACE_C, FACE_R = np.array([0.0, -0.002, 0.108]), np.array([0.058, 0.054, 0.057])
CAP_C, CAP_R = np.array([0.0, 0.004, 0.172]), np.array([0.083, 0.07, 0.097])
CAP_FLOOR = -0.11            # the hood's underside, in the unit sphere's z


# ---- the face and its tentacles -------------------------------------------------------------------------------------
def face(k, color=BLUE):
    k.add("face", ell(FACE_C, FACE_R), color, "head")
    # A flatter front where the great eyes lie, one mass with the ball
    k.add("front", ell(FACE_C + np.array([0, -0.017, 0.026]), (0.05, 0.036, 0.034)), color, "head")


def tentacles(k, color=BLUE, n=8):
    """The lower half: short tentacles side by side down the ball's surface, drawn together to a point below."""
    zs = [0.112, 0.09, 0.066, 0.046, 0.032]
    rr = [0.05, 0.047, 0.034, 0.017, 0.005]
    rad = [0.011, 0.0145, 0.0135, 0.0105, 0.0075]
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        curl = np.array([-d[1], d[0], 0]) * 0.004
        pts = [FACE_C * np.array([1, 1, 0]) + d * r + Z * z + curl * j for j, (r, z) in enumerate(zip(rr, zs))]
        k.add(f"tentacle{i}", tube(pts, rad, seg=14, per=6), color, "body")


# ---- the hood -------------------------------------------------------------------------------------------------------
def bell(V):
    V = V.copy()
    V[:, 2] = np.maximum(V[:, 2], CAP_FLOOR)
    t = np.clip(V[:, 2], 0, 1)
    V[:, :2] *= (1 - 0.24 * t ** 1.6)[:, None]
    return V


def hood(k, color=PINK):
    k.add("hood", ell(CAP_C, CAP_R, deform=bell), color, "head")


FRAME = [(0.0, 0.305), (0.101, 0.244), (0.097, 0.158), (0.0, 0.153), (-0.097, 0.158), (-0.101, 0.244)]


def frame(k, color=WHITE):
    """The white kite of fin round the hood, standing in the plane of the face, and the lip round the hood's edge."""
    k.add("frame", fin(FRAME, CAP_C[1] + 0.008), color, "head", fuse=False)
    s = math.sqrt(1 - CAP_FLOOR ** 2)
    k.add("lip", band(CAP_C + Z * (CAP_FLOOR * CAP_R[2] + 0.004), CAP_R[0] * s + 0.0015, CAP_R[1] * s + 0.0015, 0.011),
          color, "head", fuse=False)


def fin(outline, y, depth=0.022, inset=0.86):
    """A kite of fin with a ridge round its rim: the outline at y, a smaller copy of it in front and behind, hulled."""
    o = np.array(outline, float)
    mid = o.mean(axis=0)
    small = mid + (o - mid) * inset
    pts = [(a, y, b) for a, b in o] + [(a, y + dy, b) for a, b in small for dy in (-depth, depth)]
    return hull(pts)


def band(c, rx, ry, length, thick=0.006):
    """A loose band round an oval (the hood's lower edge), its seam behind."""
    def at(u, v):
        th = 2 * math.pi * u
        z = (v - 0.5) * length
        return np.stack([rx * np.sin(th), ry * np.cos(th), np.full_like(th, 0.0) + z], -1) + c
    return outward(sheet(at, 97, 3, thick))


def on_hood(az, el):
    """A point on the hood and its outward normal, by azimuth (0 straight ahead, + toward +X) and height in its sphere."""
    u = np.array([math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)])
    p = bell(u[None])[0] * CAP_R + CAP_C
    n = unit(*(u / CAP_R))
    return p, n


SPOTS = [(0.34, 0.5, 0.017, 0.015), (-0.34, 0.5, 0.017, 0.015), (1.05, 0.12, 0.013, 0.015), (-1.05, 0.12, 0.013, 0.015),
         (2.2, 0.35, 0.015, 0.015), (-2.2, 0.35, 0.015, 0.015), (math.pi, 0.55, 0.016, 0.015)]


def spots(k, color=YELLOW, spots=SPOTS):
    for az, el, a, b in spots:
        p, n = on_hood(az, el)
        k.decal(p, n, (a, b), color, eps=0.002, rings=10, segs=40, up=Z if abs(n[2]) < 0.9 else (0, 1, 0))


# ---- the beak -------------------------------------------------------------------------------------------------------
def beak(k, at, size, color=PINK, deep=PINKDEEP, bone="head", tilt=-20):
    """A small rounded beak, its point down; a darker tip."""
    at = np.array(at, float)
    k.add("beak", ell(at, size, rot=(tilt, 0, 0)), color, bone, fuse=False)
    k.add("beaktip", ell(at + np.array([0, -size[1] * 0.25, -size[2] * 0.55]), np.array(size) * (0.62, 0.62, 0.5),
                         rot=(tilt, 0, 0)), deep, bone, fuse=False)


# ---- the ribbon arms ------------------------------------------------------------------------------------------------
def ribbon(k, name, pts, radii, color, bone, flat=0.55, axis=(0, 1, 0), paddle=None):
    """
    A flat ribbon of a tentacle down pts, and at its end, if paddle is (direction, length, width, thick), a flat
    leaf of a paddle in the plane of the ribbon's flat side, pointing along direction. Authored on +X, mirrored.
    """
    k.add(name, blade(pts, radii, axis, flat), color, bone, fuse=False, mirror=True)
    if paddle is None:
        return
    d, length, width, thick = paddle
    d = unit(*d)
    a = np.array(axis, float); a /= np.linalg.norm(a)
    ez = np.cross(a, d); ez /= np.linalg.norm(ez)
    root = np.array(pts[-1], float) - d * radii[-1] * 0.6
    outline = [(0, -0.55 * width), (0.35 * length, -0.95 * width), (0.75 * length, -0.75 * width), (length, 0.0),
               (0.7 * length, 0.8 * width), (0.3 * length, 1.0 * width), (0, 0.6 * width)]
    k.add(name + "paddle", slab(outline, root, d, ez, thick), color, bone, fuse=False, mirror=True)


def arms(k, color=WHITE):
    pts = [(0.066, 0.0, 0.17), (0.08, -0.008, 0.13), (0.087, -0.014, 0.095), (0.088, -0.018, 0.07)]
    ribbon(k, "arm", pts, [0.0085, 0.0095, 0.0095, 0.009], color, "body",
           paddle=((0.55, -0.12, -0.83), 0.052, 0.012, 0.009))


# ---- the eyes -------------------------------------------------------------------------------------------------------
EYE_X, EYE_Z, EYE_HALF = 0.028, 0.141, (0.0195, 0.0205)
EYE_N = unit(0.33, -0.94, 0.06)


def open_eye(u, v, white=WHITE, iris=IRIS, pupil=PUPIL, ink=INK):
    c = np.tile(srgb(white), (len(u), 1))
    r = np.sqrt(u * u + v * v)
    di = np.sqrt((u + 0.06) ** 2 + (v + 0.04) ** 2)
    c[di < 0.66] = srgb(iris)
    c[di < 0.4] = srgb(pupil)
    c[(u - 0.18) ** 2 + (v - 0.26) ** 2 < 0.2 ** 2] = srgb(white)
    c[(u + 0.25) ** 2 + (v + 0.3) ** 2 < 0.09 ** 2] = srgb(white)
    c[r > 0.84] = srgb(ink)
    return c


def eyes(k, lid=BLUE):
    def lay(state):
        look = eye_look(state, open_eye, srgb(lid), srgb(INK), line=0.16)
        k.decal(np.array([EYE_X, FACE_C[1] - 0.048, EYE_Z]), EYE_N, EYE_HALF, look, eps=0.0022, rings=20, segs=56,
                mirror=True)
    return lay


def inkay(src, out):
    k = Kit(src, coat="scales")
    face(k)
    tentacles(k)
    hood(k)
    frame(k)
    arms(k)
    beak(k, (0.0, FACE_C[1] - 0.056, 0.139), (0.0085, 0.0075, 0.0115))
    spots(k)
    k.build(voxel=0.003, smooth=3, band=0.004, wband=0.025)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    inkay(src, out)
