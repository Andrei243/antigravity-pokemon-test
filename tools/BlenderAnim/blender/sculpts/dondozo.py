# Dondozo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dondozo.py -- <the export's Dondozo.glb> <out.glb>
#
# From the references: a vast blue catfish curled into a ring lying on its side: the great head in front, the back
# arching up and over, the rear bending down and the tail running forward again underneath, its small pale fan under
# the mouth. Blue over the top of the ring, a dark blue band along each side whose upper edge is stepped like a row of
# hexagons, pale grey on the ring's inner side and down the tail's flank. A huge mouth: the lower jaw a wide scoop, its
# inside light blue under a dark rim, a dark line where it meets the head. A small stern eye low on each side, white
# with a black pupil under a flat brow; a white ring at each mouth corner from which a long white whisker curls. Two
# white marks on top of the snout, a pair of small grey fan fins on the crown, a big grey fan on the back, grey
# pectoral fans under the belly. (The pictures show no yellow: the task's "yellow tongue or lips" is light blue.)
#
# Joints: body (0, 0, 0.381), finR/L (+-0.17, -0.09, 0.311), tail (0, 0.22, 0.381).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, polygon, args

BLUE, DARK, BELLY, LIGHT, NAVY, FIN, WHITE, INK = ("#2F8CDC", "#1D58A6", "#D4DAE4", "#62BAF2", "#123064",
                                                   "#CBD1DA", "#F4F6FA", "#141418")

# The ring's centre line in the side plane (y, z) with its radius; the first five are the body's, the rest the tail's
RING = [(-0.19, 0.45, 0.16), (0.01, 0.54, 0.17), (0.2, 0.51, 0.15), (0.32, 0.39, 0.125),
        (0.335, 0.235, 0.105), (0.245, 0.12, 0.088), (0.08, 0.08, 0.072), (-0.07, 0.078, 0.058)]
RC = np.array([0.03, 0.33])                       # the middle of the ring, for inner and outer
HC, HR = np.array([0.0, -0.16, 0.45]), np.array([0.245, 0.2, 0.16])
JC, JR = np.array([0.0, -0.285, 0.24]), np.array([0.22, 0.19, 0.075])
JROT = (-15, 0, 0)                                  # the jaw's front tipped up, so its inside faces the camera
WIDE = 1.25                                         # the ring is wider than it is deep


def on_head(d):
    """The head's surface toward a direction, and its outward normal there."""
    d = np.array(d, float)
    p = HC + d / np.sqrt(((d / HR) ** 2).sum())
    n = (p - HC) / HR ** 2
    return p, n / np.linalg.norm(n)


def ring_line(n=400):
    p = np.array(RING)
    t = np.linspace(0, len(p) - 1, n)
    y = np.interp(t, range(len(p)), p[:, 0]); z = np.interp(t, range(len(p)), p[:, 1])
    r = np.interp(t, range(len(p)), p[:, 2])
    s = np.concatenate([[0], np.cumsum(np.hypot(np.diff(y), np.diff(z)))])
    return np.stack([y, z], 1), r, s


LINE, LR, LS = ring_line()


def body(k):
    pts = [(0, y, z) for y, z, _ in RING]
    rad = [r for _, _, r in RING]
    def wide(shape):
        V, F = shape
        V = V.copy(); V[:, 0] *= WIDE
        return V, F
    k.add("trunk", wide(tube(pts[:5], rad[:5], seg=32)), BLUE, "body")
    k.add("tailpart", wide(tube(pts[3:], rad[3:], seg=32)), BLUE, "tail")
    k.add("head", ell(HC, HR, sub=5), BLUE, "body")
    k.add("belly", ell((0, 0.05, 0.37), (0.19, 0.19, 0.1)), BELLY, "body")
    k.add("jaw", ell(JC, JR, rot=JROT, sub=5), DARK, "body")


def where(P):
    """For points: the outward share of each from the ring's centre line (1 outer top, -1 inner) and its length along."""
    yz = P[:, 1:3]
    d2 = ((yz[:, None, :] - LINE[None]) ** 2).sum(2)
    i = d2.argmin(1)
    q = LINE[i]
    out = q - RC; out /= np.linalg.norm(out, axis=1, keepdims=True)
    rel = P[:, 1:3] - q
    return (rel * out).sum(1) / LR[i], LS[i]


def markings(k):
    def band(P):
        o, s = where(P)
        step = np.sign(np.sin(2 * math.pi * s / 0.07))
        edge = 0.08 + 0.1 * step
        return inside(o - edge, 0.006) * (P[:, 1] > -0.32)
    k.paint(band, DARK, on=["trunk", "tailpart", "head"])

    def pale(P):
        o, _ = where(P)
        return inside(o + 0.42, 0.01)
    k.paint(pale, BELLY, on=["trunk", "tailpart", "belly"])

    # the jaw's inside: light blue inside a dark rim
    a = math.radians(JROT[0])
    m = np.array([[1, 0, 0], [0, math.cos(a), -math.sin(a)], [0, math.sin(a), math.cos(a)]])
    def scoop(P):
        Q = (P - JC) @ m
        q = (Q / (JR * np.array([0.86, 0.88, 1]))) ** 2
        return inside(q[:, 0] + q[:, 1] - 1, 0.01) * inside(0.01 - Q[:, 2], 0.004)
    k.paint(scoop, LIGHT, on=["jaw"])
    # the mouth line where head meets jaw, in front
    def lips(P):
        Q = (P - JC) @ m
        q = np.sqrt((Q[:, 0] / JR[0]) ** 2 + (Q[:, 1] / JR[1]) ** 2)
        return inside(np.abs(Q[:, 2] - 0.035) - 0.012, 0.003) * inside(q - 1.02, 0.02) * inside(P[:, 1] + 0.1, 0.01)
    k.paint(lips, NAVY, on=["head"])


# --- loose pieces --------------------------------------------------------------------------------------------------
def fan(r, root, lobes=3, n=24):
    """A fan's outline: a root of half-width root at a = 0, a scalloped rim of radius r round the far side."""
    pts = [(0.0, -root)]
    for t in np.linspace(-1.2, 1.2, n):
        rr = r * (1 - 0.08 * abs(math.sin(lobes * (t + 1.2) * math.pi / 2.4)))
        pts.append((rr * math.cos(t), rr * math.sin(t) * 0.9))
    pts.append((0.0, root))
    return pts


def fins(k):
    th = 0.022
    # the big fan on the back, leaning back, turned to +X
    up = np.array([0, 0, 1.0])
    back = unit(math.sin(math.radians(40)), math.cos(math.radians(40)), 0)       # the midline turned 40 to +X
    k.add("dorsal", slab(fan(0.15, 0.06), (0.02, 0.22, 0.6), unit(*(0.55 * back + 0.85 * up)),
                         unit(*(back - 0.55 * up)), th), FIN, "body", fuse=False)
    # the small fans on the crown, splayed up and out, faces to the front
    k.add("crown", slab(fan(0.095, 0.03), (0.13, -0.12, 0.55), unit(0.5, 0.1, 0.85), (1, 0, 0), th), FIN,
          "body", fuse=False, mirror=True)
    # the pectorals under the belly, back and down, faces out
    k.add("pec", slab(fan(0.11, 0.035), (0.19, -0.05, 0.31), unit(0.35, 0.7, -0.6), (0, 0, 1), th), FIN,
          "fin{s}", fuse=False, mirror=True)
    # the tail's small fan under the mouth, out to +X, its face to the front
    k.add("tailfin", slab(fan(0.09, 0.03), (0.04, -0.11, 0.08), unit(0.85, 0.2, 0.45), (0, 0, 1), th), FIN,
          "tail", fuse=False)
    # the whiskers: from the mouth's corners forward, out and down, curling at their ends
    p, _ = on_head((0.8, -0.55, -0.45))
    k.add("whisker", tube([p, p + (0.07, -0.03, 0.0), p + (0.11, -0.09, -0.02), p + (0.12, -0.15, -0.06),
                           p + (0.1, -0.18, -0.09), p + (0.08, -0.16, -0.085)],
                          [0.012, 0.012, 0.0115, 0.011, 0.011, 0.0105], per=6), WHITE, "body", fuse=False,
          mirror=True, surface="skin")


def marks(k):
    # two white marks on top of the snout
    p, n = on_head((0.3, -0.55, 0.8))
    k.decal(p, n, (0.028, 0.042), WHITE, up=(0, -1, 0.3), eps=0.003, mirror=True)
    # a white ring at each mouth corner, the whisker's root
    def ring(u, v):
        c = np.tile(srgb(WHITE), (len(u), 1))
        c[u * u + v * v < 0.45 ** 2] = srgb(DARK)
        return c
    p, n = on_head((0.8, -0.55, -0.45))
    k.decal(p, n, (0.024, 0.024), ring, eps=0.003, mirror=True)


# --- the eyes: small, stern, white with a black pupil, a flat brow cutting their top --------------------------------
CW, CK, CD, CL = srgb(WHITE), srgb(INK), srgb(DARK), srgb(BELLY)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[(u + 0.1) ** 2 + (v + 0.05) ** 2 < 0.5 ** 2] = CK
    c[u * u + v * v > 0.9 ** 2] = CK
    return c


def outline(side):
    """An oval whose top is cut by a brow sloping down toward the snout (u toward the back on +X, forward on -X)."""
    pts = []
    for t in np.linspace(0, 2 * math.pi, 40, endpoint=False):
        u, v = math.cos(t), 0.85 * math.sin(t)
        cut = 0.32 + 0.3 * u * side
        pts.append((u, min(v, cut)))
    return polygon(pts)


EYE_AT, EYE_N = on_head((0.78, -0.6, -0.05))
EYE_HALF = (0.046, 0.046)


def eyes(k, state):
    look = eye_look(state, eye_open, CD, CK if state in ("Open", "Fierce") else CL, line=0.16)
    k.decal(EYE_AT, EYE_N, EYE_HALF, look, outline=outline(1), eps=0.0025, rings=20, segs=56)
    k.decal(EYE_AT * np.array([-1, 1, 1]), EYE_N * np.array([-1, 1, 1]), EYE_HALF,
            lambda u, v: look(-u, v), outline=outline(-1), eps=0.0025, rings=20, segs=56)


def dondozo(src, out):
    k = Kit(src, coat="scales")
    body(k)
    markings(k)
    marks(k)
    fins(k)
    k.build(voxel=0.0066, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    dondozo(*args())
