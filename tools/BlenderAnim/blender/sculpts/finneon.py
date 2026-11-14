# Finneon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/finneon.py -- <the export's Finneon.glb> <out.glb>
#
# From the references: a small plump fish lying along its bones (nose to -Y). Dark navy over the top, pale sky blue
# below, a pink stripe along the line between them from the nose to the tail's root; a round pink eye with a large
# dark pupil sitting on the stripe; a small smile under the nose; two rounded navy fins on the back like a pair of
# ears; small pale fins under the sides; and a tail like a butterfly's wings, pale blue, its two great upper lobes
# and two small lower ones opened in an X, each with a pink spot. The fins are loose flat plates (beautifly.py's way),
# their pink spots thicker plates inside them, so they show from the front and from behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

NAVY, SKY, PINK, INK = "#1C3A60", "#9ED8F2", "#EE6EA6", "#141C2A"


# --- flat plates (beautifly.py's) ---------------------------------------------------------------------------------
def smooth(pts, per=6):
    """A closed Catmull-Rom curve through the points, per points a span."""
    p = np.array(pts, float)
    n = len(p)
    o = []
    for i in range(n):
        p0, p1, p2, p3 = p[i - 1], p[i], p[(i + 1) % n], p[(i + 2) % n]
        for j in range(per):
            t = j / per
            o.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    return np.array(o)


def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def earclip(p):
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
                tris.append((a, b, c)); idx.pop(i); cut = True
                break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin), thick about its middle plane."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    V = [front, back]
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    off = 2 * m
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([off + 4 * i, off + 4 * i + 1, off + 4 * i + 2, off + 4 * i + 3])
    V.append(np.array(rim))
    return np.vstack(V), F


def oval(c, a, b, turn=0.0, n=36):
    th = np.linspace(0, 2 * math.pi, n, endpoint=False)
    ct, st = math.cos(math.radians(turn)), math.sin(math.radians(turn))
    x, y = a * np.cos(th), b * np.sin(th)
    return np.stack([c[0] + x * ct - y * st, c[1] + x * st + y * ct], axis=1)


# --- the body: a plump teardrop, blunt nose forward, narrowing into the tail's root -------------------------------
k.add("body", ell((0, 0.0, 0.172), (0.07, 0.13, 0.082)), SKY, "body")
k.add("head", ell((0, -0.07, 0.168), (0.068, 0.088, 0.076)), SKY, "head")
k.add("stalk", tube([(0, 0.08, 0.176), (0, 0.15, 0.184), (0, 0.215, 0.19)], [0.056, 0.036, 0.02]), SKY, "tail")


def zline(P):
    """The line between the navy back and the pale belly, low at the nose and curling up over the tail's root."""
    y = P[:, 1]
    return 0.156 + 0.075 * (y + 0.15) + 5.0 * np.clip(y - 0.06, 0, None) ** 2


def above(P):
    """How far a point stands over the line, measured across it (not in z), so the edge stays crisp where it climbs."""
    slope = 0.075 + 10.0 * np.clip(P[:, 1] - 0.06, 0, None)
    return (P[:, 2] - zline(P)) / np.sqrt(1 + slope * slope)


STRIPE = 0.0075
k.paint(lambda P: inside(STRIPE - above(P), 0.0025), NAVY)
k.paint(lambda P: inside(np.abs(above(P)) - STRIPE, 0.0025) * inside(P[:, 1] - 0.2, 0.004), PINK)

# The small smile under the nose, low on the pale face
def mouth(u, v):
    c = np.tile(srgb(SKY), (len(u), 1))
    c[np.abs(v - (0.85 * u * u - 0.3)) < 0.28] = srgb(INK)
    return c
k.decal((0, -0.152, 0.142), unit(0, -1, -0.25), (0.022, 0.01), mouth, eps=0.004, rings=8, segs=40)

# --- fins ---------------------------------------------------------------------------------------------------------
# Two rounded navy fins on the back, the front one taller, splayed a little apart
k.add("dorsal1", ell((0.012, 0.005, 0.29), (0.014, 0.036, 0.066), rot=(-22, 14, 0)), NAVY, "dorsal", fuse=False)
k.add("dorsal2", ell((-0.012, 0.05, 0.27), (0.013, 0.034, 0.054), rot=(-34, -14, 0)), NAVY, "dorsal", fuse=False)
# Small pale fins under the sides, swept back and down
k.add("pec", ell((0.072, -0.02, 0.112), (0.007, 0.034, 0.018), rot=(28, 0, -12)), SKY, "fin{s}", fuse=False,
      mirror=True)

# The tail: four lobes opened in an X behind the tail's root, the upper ones great, each with a pink spot
T0 = np.array([0.004, 0.21, 0.19])
TILT = math.radians(26)
EX, EZ = (0.0, 1.0, 0.0), (math.sin(TILT), 0.0, math.cos(TILT))
UPPER = smooth([(0.0, 0.0), (-0.012, 0.06), (-0.004, 0.13), (0.025, 0.19), (0.07, 0.225), (0.12, 0.228),
                (0.16, 0.195), (0.172, 0.14), (0.155, 0.085), (0.11, 0.04), (0.06, 0.01)])
LOWER = smooth([(0.0, 0.0), (0.045, -0.005), (0.085, -0.03), (0.105, -0.07), (0.098, -0.108), (0.068, -0.122),
                (0.035, -0.1), (0.01, -0.05)])
k.add("lobe", flat(UPPER, T0, EX, EZ, 0.009), SKY, "tailFin", fuse=False, mirror=True)
k.add("lobelow", flat(LOWER, T0 + np.array([0, 0.002, 0]), EX, EZ, 0.009), SKY, "tailFin", fuse=False, mirror=True)
k.add("spot", flat(oval((0.105, 0.135), 0.048, 0.068, turn=28), T0, EX, EZ, 0.013), PINK, "tailFin", fuse=False,
      mirror=True)
k.add("spotlow", flat(oval((0.064, -0.072), 0.024, 0.026), T0, EX, EZ, 0.013), PINK, "tailFin", fuse=False,
      mirror=True)

# --- the eyes: round pink eyes on the stripe, a great dark pupil and a glint ---------------------------------------
CN, CS, CP, CK, CW = srgb(NAVY), srgb(SKY), srgb(PINK), srgb(INK), np.array([1.0, 1.0, 1.0])
def eye(u, v):
    r2 = u * u + v * v
    c = np.tile(CP, (len(u), 1))
    c[(u / 0.56) ** 2 + ((v + 0.02) / 0.62) ** 2 < 1] = CK
    c[((u + 0.12) / 0.2) ** 2 + ((v - 0.22) / 0.22) ** 2 < 1] = CW
    c[r2 > 0.8] = CK
    return c
LIDMARK, INKMARK = np.array([0.5, 0.0, 0.5]), np.array([0.0, 0.5, 0.0])
def look(state):
    base = eye_look(state, eye, LIDMARK, INKMARK, line=0.17)
    def f(u, v):
        c = base(u, v)
        lid = np.where((v > 0.32)[:, None], CN, np.where((v > -0.32)[:, None], CP, CS))
        ink = np.where((v > 0.2)[:, None], CP, CK)
        isl = np.all(np.isclose(c, LIDMARK), axis=1)
        isi = np.all(np.isclose(c, INKMARK), axis=1)
        c[isl] = lid[isl]
        c[isi] = ink[isi]
        return c
    return f
de = unit(0.82, -0.56, 0.04)
def eyes(state):
    k.decal(np.array([0, -0.078, 0.163]) + de * 0.068, de, (0.022, 0.023), look(state), eps=0.005, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.0045, smooth=4)
k.eye_states(eyes)
k.export(out)
