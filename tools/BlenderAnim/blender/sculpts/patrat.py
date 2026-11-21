# Patrat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/patrat.py -- <the export's Patrat.glb> <out.glb>
# From the references: a brown chipmunk standing up, a pear of a body with an oval cream belly; a big head as wide as
# the body, a brown cap with a little ledge over the eyes and small pointed ears; a dark band across the face holding
# great round eyes, red with a yellow ring and a small black pupil, their tops cut slanting by the cap; puffed cream
# cheeks and muzzle, a small dark nose and two big white front teeth; short arms; short legs on dark three-toed feet;
# a thin tail up from behind to a white jagged tuft.
# Written to be grown from (watchog.py): the colours, the eye, the band, the teeth and the tuft are names and
# functions here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, polygon, args

BROWN, CREAM, BAND, FEET = "#A8784E", "#E6D2B0", "#2C2424", "#4C403C"
RED, YELLOW, PUPIL, WHITE, NOSE = "#E2382E", "#F6C030", "#141010", "#FFFFFF", "#3A2620"
SHUTLINE = "#B88A60"


# --- the line's own parts, shared with Watchog ----------------------------------------------------------------------
def ring_eye(rings, pupil, white, pupil_half=(0.2, 0.34), glint=(0.28, 0.42, 0.16)):
    """A round eye of rings from the outside in ((outer radius, colour) pairs) round a small black pupil, one glint."""
    R = [(r, srgb(c)) for r, c in rings]
    P, W = srgb(pupil), srgb(white)
    pu, pv = pupil_half
    gu, gv, gr = glint
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(R[0][1], (len(u), 1))
        for rr, col in R[1:]:
            c[r < rr] = col
        c[(u / pu) ** 2 + (v / pv) ** 2 < 1] = P
        c[((u - gu) / gr) ** 2 + ((v - gv) / gr) ** 2 < 1] = W
        return c
    return look


def cut_eye(a, b, n=64):
    """The eye's outline: the unit circle with its top cut by the line v = a + b u (u outward): a slanting cap."""
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        u, v = math.cos(t), math.sin(t)
        pts.append((u, min(v, a + b * u)))
    return polygon(pts)


def lay_eyes(k, state, E, n, half, look, lid, ink, outline, up=(0, 0, 1)):
    k.decal(E, n, half, eye_look(state, look, srgb(lid), srgb(ink), line=0.15), up=up, eps=0.003, outline=outline,
            rings=20, segs=56, mirror=True)


def band(HC, HR, bottom, top, reach=1.9):
    """The dark band across the face round the eyes, from the front round to the sides of the head."""
    def mask(P):
        d = P - HC
        ang = np.abs(np.arctan2(d[:, 0], -d[:, 1]))
        near = np.linalg.norm(d / (HR * 1.35), axis=1) < 1
        return inside(P[:, 2] - top, 0.002) * inside(bottom - P[:, 2], 0.002) * inside(ang - reach, 0.05) * near
    return mask


def teeth(k, at, w, h, t, bone):
    """Two big front teeth side by side, a hair apart, hanging from at (the middle of their top edge)."""
    at = np.array(at, float)
    g = w * 0.06
    for s in (1, -1):
        x0, x1 = s * g, s * w
        P = [(x0, 0, 0), (x1, 0, 0), (x0, t, 0), (x1, t, 0),
             (x0, 0, -h), (x1 * 0.97, 0, -h * 0.97), (x0, t, -h), (x1 * 0.97, t, -h * 0.97)]
        k.add("tooth" + ("R" if s > 0 else "L"), hull([at + np.array(p) for p in P]), WHITE, bone, fuse=False,
              surface="shell")


def tuft(k, T, n, length, width, colour, bone, count=6, spread=150, ball=None):
    """A jagged tuft: a fan of thick pointed spikes in the plane square to n, round a ball at T, up along the plane."""
    T = np.array(T, float)
    n = np.array(n, float); n /= np.linalg.norm(n)
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    side = np.cross(n, up)
    k.add("tuftball", ell(T, (ball or width * 1.4,) * 3), colour, bone, fuse=False)
    for i in range(count):
        a = math.radians(-spread / 2 + spread * i / (count - 1))
        d = math.cos(a) * up + math.sin(a) * side
        s = np.cross(n, d)
        L = length * (1.0 if i % 2 == 0 else 0.72) * (1 - 0.25 * abs(a) / math.radians(spread / 2 + 1))
        R = T + d * width * 0.5
        th = width * 0.42
        k.add(f"tuft{i}", hull([R + s * width, R - s * width, R + n * th, R - n * th,
                                R + d * L * 0.45 + s * width * 0.75, R + d * L * 0.45 - s * width * 0.75,
                                R + d * L * 0.45 + n * th * 0.7, R + d * L * 0.45 - n * th * 0.7, R + d * L]),
              colour, bone, fuse=False)


def claws(k, foot, toes, colour, bone):
    """Three short thick toes forward off a foot (foot: the front middle of the foot)."""
    foot = np.array(foot, float)
    for j, dx in enumerate((-1, 0, 1)):
        d = unit(0.45 * dx, -1, -0.15)
        r = foot + np.array([dx * toes[0], 0, 0])
        s = np.cross(d, (0, 0, 1.0)); s /= np.linalg.norm(s)
        w, th, L = toes[1], toes[2], toes[3]
        k.add(f"toe{j}", hull([r + s * w, r - s * w, r + (0, 0, th), r - (0, 0, th * 0.6), r + d * L,
                               r + d * L * 0.5 + s * w * 0.7, r + d * L * 0.5 - s * w * 0.7]),
              colour, bone, fuse=False, mirror=True, surface="shell")


# --- Patrat ----------------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.012, 0.29])       # the head's middle (a tenth larger than measured)
HR = np.array([0.1, 0.088, 0.083])
EYE_RINGS = [(1.0, RED), (0.84, YELLOW), (0.64, RED)]


def patrat(src, out):
    k = Kit(src, coat="fur")

    # The pear of a body, wide below, and its chest up into the head
    k.add("body", ell((0, 0.006, 0.125), (0.108, 0.092, 0.1)), BROWN, "body")
    k.add("chest", ell((0, -0.002, 0.195), (0.088, 0.078, 0.065)), BROWN, "body")

    # The big head: a brown cap with a ledge over the eyes, cream cheeks puffed out at the sides and a cream muzzle
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("brow", ell(HC + (0, -0.03, 0.045), (0.088, 0.062, 0.032)), BROWN, "head")
    k.add("cheek", ell(HC + (0.068, -0.03, -0.045), (0.056, 0.052, 0.046)), CREAM, "head", mirror=True)
    k.add("muzzle", ell(HC + (0, -0.062, -0.045), (0.05, 0.036, 0.036)), CREAM, "head")
    def face(P):
        d = P - HC
        near = np.linalg.norm(d / (HR * 1.45), axis=1) < 1
        return inside(P[:, 2] - (HC[2] - 0.022), 0.002) * (d[:, 1] < -0.01) * near * (P[:, 2] > 0.2)
    k.paint(face, CREAM)

    # Small pointed ears on the cap's corners
    for s, nm in ((1, "R"), (-1, "L")):
        pts = [(0.058 * s, 0.004, 0.35), (0.072 * s, 0.008, 0.383), (0.082 * s, 0.012, 0.405)]
        k.add("ear" + nm, blade(pts, [0.026, 0.017, 0.004], (0, 1, 0), 0.55), BROWN, "head")

    # The dark band across the face
    k.paint(band(HC, HR, HC[2] - 0.02, HC[2] + 0.04), BAND)

    # Short arms held forward and a little down, round paws
    k.add("arm", tube([(0.068, -0.02, 0.2), (0.11, -0.06, 0.18), (0.135, -0.09, 0.163)], [0.03, 0.027, 0.025]),
          BROWN, "arm{s}", mirror=True)
    k.add("paw", ell((0.14, -0.098, 0.158), (0.029, 0.029, 0.03)), BROWN, "arm{s}", mirror=True)

    # Short legs fused into the hips, dark feet with three toes
    k.add("thigh", ell((0.056, -0.012, 0.062), (0.052, 0.052, 0.052)), BROWN, "leg{s}", mirror=True)
    k.add("shin", tube([(0.058, -0.02, 0.05), (0.06, -0.028, 0.026)], [0.032, 0.03]), BROWN, "leg{s}", mirror=True)
    k.add("foot", ell((0.06, -0.035, 0.016), (0.036, 0.046, 0.016)), FEET, "leg{s}", mirror=True)
    claws(k, (0.06, -0.07, 0.013), (0.018, 0.009, 0.007, 0.026), FEET, "leg{s}")

    # The thin tail up from behind to a white jagged tuft, turned out to the right
    TP = [(0, 0.05, 0.09), (0, 0.11, 0.1), (0.035, 0.15, 0.16), (0.075, 0.165, 0.24), (0.1, 0.16, 0.3)]
    k.add("tail", tube(TP, [0.017, 0.016, 0.015, 0.014, 0.013], seg=16), BROWN, "tail")
    tuft(k, (0.104, 0.158, 0.31), unit(0.55, 0.83, 0), 0.062, 0.016, WHITE, "tail", count=6, spread=170)

    # The oval cream belly
    k.decal((0, -0.088, 0.12), unit(0, -1, -0.05), (0.064, 0.078), CREAM, eps=0.003, rings=18, segs=56)

    # A small dark nose over the teeth, the line down from it
    NOSE_T = polygon([(-1, 0.7), (1, 0.7), (0, -1)])
    k.decal(HC + (0, -0.096, -0.024), unit(0, -1, 0.25), (0.012, 0.009), NOSE, eps=0.003, outline=NOSE_T, rings=6,
            segs=24)
    teeth(k, HC + (0, -0.094, -0.052), 0.016, 0.027, 0.008, "head")

    # The great round eyes in the band, their tops cut slanting by the cap
    de = unit(0.5, -0.85, 0.14)
    E = HC + de / np.linalg.norm(de / HR)
    look = ring_eye(EYE_RINGS, PUPIL, WHITE)
    cut = cut_eye(0.62, 0.32)
    def eyes(state):
        lay_eyes(k, state, E, de, (0.031, 0.031), look, BAND, SHUTLINE, cut)

    k.build(voxel=0.0034)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    patrat(*args())
