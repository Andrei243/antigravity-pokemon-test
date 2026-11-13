# Beautifly sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/beautifly.py -- <the export's Beautifly.glb> <out.glb>
# The wings are its whole character: each a loose flat plate with its pattern laid as thicker plates inside it, so
# that every panel shows on the front and on the back alike (patches land on the fused body alone).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

GREY, DARK, WHITE = "#5E5E66", "#5C5C64", "#ECECF0"
YELLOW, RED, BLUE = "#F7C11F", "#DE2A4A", "#2F9BDA"


# --- flat plates ------------------------------------------------------------------------------------------------
def smooth(pts, per=6):
    """A closed Catmull-Rom curve through the points, per points a span."""
    p = np.array(pts, float)
    n = len(p)
    out_ = []
    for i in range(n):
        p0, p1, p2, p3 = p[i - 1], p[i], p[(i + 1) % n], p[(i + 2) % n]
        for j in range(per):
            t = j / per
            out_.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    return np.array(out_)


def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def earclip(p):
    """Triangles of a simple polygon, counter-clockwise."""
    idx = list(range(len(p)))
    tris = []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    guard = 0
    while len(idx) > 3 and guard < 100000:
        guard += 1
        m = len(idx)
        cut = False
        for i in range(m):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % m]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            ok = True
            for j in idx:
                if j in (a, b, c):
                    continue
                q = p[j]
                if cross(p[a], p[b], q) >= 0 and cross(p[b], p[c], q) >= 0 and cross(p[c], p[a], q) >= 0:
                    ok = False
                    break
            if ok:
                tris.append((a, b, c))
                idx.pop(i)
                cut = True
                break
        if not cut:
            # a sliver left: clip the flattest corner
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick, shift=0.0):
    """
    A flat plate of a polygon (a, b in the plane of ex, ez about origin), thick about its middle plane (moved by shift
    along the plane's normal), with sharp edges: its faces and rim do not share points, so the faces shade flat.
    """
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    o = np.array(origin, float) + nrm * shift
    W = o + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    V = [front, back]
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c])
        F.append([m + c, m + b, m + a])
    off = 2 * m
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([off + 4 * i, off + 4 * i + 1, off + 4 * i + 2, off + 4 * i + 3])
    V.append(np.array(rim))
    return np.vstack(V), F


def band(curve, w0, w1, out_=0.0015):
    """A strip along an open curve (counter-clockwise along its polygon): out a hair, in by w0..w1 along it."""
    c = np.array(curve, float)
    t = np.gradient(c, axis=0); t /= np.linalg.norm(t, axis=1, keepdims=True)
    inward = np.stack([-t[:, 1], t[:, 0]], axis=1)
    s = np.linspace(0, 1, len(c))
    w = w0 + (w1 - w0) * s
    w = w * np.minimum(1, np.minimum(s, 1 - s) / 0.12)          # tapered to nothing at both ends
    return np.vstack([c - inward * out_, (c + inward * w[:, None])[::-1]])


def lens(a0, a1, width, bulge=0.5, n=24):
    """A long pointed-round panel from a0 to a1 in the wing's plane, widest at bulge of the way."""
    a0, a1 = np.array(a0, float), np.array(a1, float)
    d = a1 - a0; L = np.linalg.norm(d); d /= L
    side = np.array([-d[1], d[0]])
    pts = []
    for i in range(n):
        t = i / (n - 1)
        r = width * np.sin(np.pi * t ** (math.log(0.5) / math.log(bulge))) ** 0.7
        pts.append(a0 + d * L * t + side * r)
    for i in range(n - 2, 0, -1):
        t = i / (n - 1)
        r = width * np.sin(np.pi * t ** (math.log(0.5) / math.log(bulge))) ** 0.7
        pts.append(a0 + d * L * t - side * r)
    return np.array(pts)


def oval(c, a, b, turn=0.0, n=36):
    th = np.linspace(0, 2 * math.pi, n, endpoint=False)
    ct, st = math.cos(math.radians(turn)), math.sin(math.radians(turn))
    x, y = a * np.cos(th), b * np.sin(th)
    return np.stack([c[0] + x * ct - y * st, c[1] + x * st + y * ct], axis=1)


# --- the body ---------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.03, 0.43])        # the head's centre
HR = 0.078
k.add("head", ell(H, (HR, HR * 0.95, HR * 0.97)), WHITE, "head")
# A dark hood over the top and back of the head, the face white below and in front
hood = unit(0, 0.25, 0.97)
k.paint(lambda P: inside(0.42 - ((P - H) / HR) @ hood, 0.0025), GREY, on=["head"])
# A white chest of fluff under it, with two white forepaws curled in front
k.add("chest", ell((0, -0.015, 0.335), (0.052, 0.048, 0.05)), WHITE, "body")
k.add("paw", ell((0.03, -0.06, 0.32), (0.022, 0.024, 0.026), rot=(20, 0, 0)), WHITE, "body", mirror=True)
k.add("paw2", ell((0.026, -0.05, 0.278), (0.019, 0.02, 0.02)), WHITE, "body", mirror=True)
# A slim grey abdomen trailing down and a little back
k.add("abdomen", ell((0, 0.012, 0.235), (0.036, 0.036, 0.085), rot=(14, 0, 0),
                     deform=lambda V: V * np.array([1, 1, 1.0]) * (1 - 0.3 * np.clip(-V[:, 2:3], 0, 1))), GREY, "body")
k.add("thorax", ell((0, 0.02, 0.31), (0.034, 0.034, 0.04)), GREY, "body")

# Long thin legs to oval pads
k.add("leg", tube([(0.022, -0.02, 0.29), (0.045, -0.04, 0.22), (0.045, -0.035, 0.14), (0.05, -0.02, 0.095)],
                  [0.0075, 0.0065, 0.006, 0.005]), GREY, "leg{s}", fuse=False, mirror=True)
k.add("pad", ell((0.053, -0.026, 0.088), (0.013, 0.026, 0.008), rot=(-20, 0, 8)), GREY, "leg{s}", fuse=False,
      mirror=True)

# The long proboscis down from the face, coiled at its end
def proboscis():
    pts = [(0, -0.1, 0.41), (0, -0.112, 0.375), (0, -0.112, 0.33), (0, -0.108, 0.29)]
    c = np.array([0, -0.074, 0.268])
    n = 26
    for i in range(1, n + 1):
        t = i / n
        a = math.radians(-30 - 520 * t)          # from the front, down, back, round again
        r = 0.036 * (1 - 0.62 * t)
        pts.append((0, c[1] - r * math.cos(a), c[2] + r * math.sin(a)))
    rad = [0.0075, 0.0068, 0.0062, 0.0058] + [0.0056 - 0.002 * i / n for i in range(1, n + 1)]
    return tube(pts, rad)
k.add("tongue", proboscis(), DARK, "head", fuse=False)

# Antennae: thin stalks up and out from the crown, each ending in a club
def antenna():
    pts = [(0.018, -0.045, 0.49), (0.045, -0.055, 0.55), (0.075, -0.06, 0.6), (0.095, -0.062, 0.63),
           (0.105, -0.063, 0.648), (0.116, -0.064, 0.666), (0.124, -0.064, 0.68)]
    rad = [0.0065, 0.0055, 0.005, 0.009, 0.013, 0.011, 0.004]
    return tube(pts, rad)
k.add("antenna", antenna(), GREY, "ear{s}", fuse=False, mirror=True)

# --- the wings --------------------------------------------------------------------------------------------------
WO = np.array([0.03, 0.035, 0.335])         # the wings' root, at the wing joint
EX, EZ = (1.0, 0.22, 0.0), (0.0, 0.08, 1.0)    # outward, swept a little back; and up

# The great upper wing: steep leading edge to a round tip high and out, its trailing edge stepped
UP = smooth([(0.0, 0.0), (0.012, 0.06), (0.05, 0.15), (0.11, 0.235), (0.19, 0.29), (0.28, 0.31), (0.36, 0.295),
             (0.415, 0.245), (0.43, 0.175), (0.405, 0.105), (0.35, 0.06), (0.33, 0.035), (0.27, 0.02), (0.25, -0.005),
             (0.18, -0.02), (0.16, -0.04), (0.09, -0.045), (0.03, -0.035)], per=6)
k.add("wing", flat(UP, WO, EX, EZ, 0.008), DARK, "wing{s}", fuse=False, mirror=True)
# its yellow leading edge, from the root round the tip
k.add("edge", flat(band(UP[6:8 * 6 + 3][::-1], 0.02, 0.012), WO, EX, EZ, 0.0125), YELLOW, "wing{s}", fuse=False, mirror=True)
# three long yellow panels fanning from the root, grey veins between, and the red marks of the tip
for i, (a0, a1, w) in enumerate((((0.06, 0.085), (0.36, 0.212), 0.024), ((0.07, 0.048), (0.375, 0.128), 0.025),
                                 ((0.075, 0.012), (0.3, 0.042), 0.019))):
    k.add(f"yel{i}", flat(lens(a0, a1, w, bulge=0.62), WO, EX, EZ, 0.0125), YELLOW,
          "wing{s}", fuse=False, mirror=True)
k.add("red0", flat(oval((0.3, 0.255), 0.05, 0.019, turn=-8), WO, EX, EZ, 0.0125), RED, "wing{s}", fuse=False,
      mirror=True)
k.add("red1", flat(oval((0.125, 0.195), 0.028, 0.014, turn=38), WO, EX, EZ, 0.0125), RED, "wing{s}", fuse=False,
      mirror=True)

# The small lower wing, a little behind, its edge scalloped; blue and red panels near the body
LO = np.array([0.0, -0.02])
LOW = smooth([(0.0, -0.01), (0.07, -0.01), (0.14, -0.03), (0.19, -0.065), (0.2, -0.1), (0.185, -0.115),
              (0.19, -0.14), (0.165, -0.16), (0.165, -0.185), (0.135, -0.2), (0.13, -0.245), (0.12, -0.275), (0.1, -0.272), (0.09, -0.23),
              (0.05, -0.19), (0.015, -0.12), (0.0, -0.06)], per=5)
WL = WO + np.array([0, 0.012, 0.0])
k.add("hind", flat(LOW, WL, EX, EZ, 0.008), DARK, "wing{s}", fuse=False, mirror=True)
k.add("blue", flat(lens((0.02, -0.03), (0.15, -0.06), 0.022, bulge=0.6), WL, EX, EZ, 0.0125), BLUE, "wing{s}",
      fuse=False, mirror=True)
k.add("red2", flat(lens((0.025, -0.075), (0.15, -0.135), 0.024, bulge=0.6), WL, EX, EZ, 0.0125), RED, "wing{s}",
      fuse=False, mirror=True)

# --- the eyes: great blue rounds on the sides of the white face ---------------------------------------------------
LID, INK = srgb(WHITE), srgb("#16161C")
IRIS, DEEP = srgb("#3CA6EA"), srgb("#1E6FB8")
def eye(u, v):
    c = np.tile(IRIS, (len(u), 1))
    r2 = u * u + v * v
    c[r2 < 0.55] = DEEP * 0.0 + IRIS
    c[(v < -0.1) & (r2 < 0.72)] = IRIS * 0.85 + DEEP * 0.15
    c[((u + 0.12) / 0.34) ** 2 + ((v + 0.02) / 0.42) ** 2 < 1] = INK
    c[((u + 0.0) / 0.16) ** 2 + ((v - 0.32) / 0.16) ** 2 < 1] = (1, 1, 1)
    c[r2 > 0.82] = INK
    return c
de = unit(0.8, -0.56, 0.16)
def eyes(state):
    k.decal(H + de * HR, de, (0.042, 0.046), eye_look(state, eye, LID, INK), eps=0.007, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.006, smooth=4)
k.eye_states(eyes)
k.export(out)
