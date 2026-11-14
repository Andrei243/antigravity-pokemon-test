# Togekiss sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/togekiss.py -- <the export's Togekiss.glb> <out.glb>
#
# Grown from togetic.py (and so from togepi.py): its white, its red and blue, its crown_point(), its mark() and its way
# of making the eyes and the mouth, its wings' grooves. From the references: a glider of a body, an egg lying level,
# the head the front of it with no neck between, the body tapering back to a small fan of tail feathers; the wings are
# the body's own sides, wide, flat and swept back to rounded points (held here with their tips raised and the span
# shorter than the pictures', so the body is not drawn small), each with a grey groove of an oval on its top; the crown
# three broad points sweeping back, the one on its right tipped in blue, the middle white, the one on its left tipped in
# red; small black eyes with a glint and a small open happy mouth; small solid red and blue triangles on the belly (not
# hollow, as Togepi's and Togetic's are); two small white feet under the belly.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, tri, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# --- the line's colours (togepi.py's) ----------------------------------------------------------------------------------
WHITE, CREAM, RED, BLUE = "#F7F6F1", "#F6EBA6", "#E4463C", "#3E9BE6"
INKH, MOUTHH, TONGUEH = "#17151C", "#A42C3C", "#F2879A"
GROOVE = "#C9CDD6"                            # the wings' grooves (togetic.py's)

# --- the frame ---------------------------------------------------------------------------------------------------------
BODY_C = np.array([0.0, 0.02, 0.172])         # the level egg (the body bone is at 0, 0, 0.159)
BODY_R = np.array([0.122, 0.178, 0.112])
HEAD_C = np.array([0.0, -0.088, 0.218])        # the head, the egg's front (the head bone is at 0, -0.14, 0.199)
HEAD_R = np.array([0.112, 0.1, 0.098])
WING_C, WING_R = np.array([0.2, 0.09, 0.195]), np.array([0.15, 0.24, 0.044])
WING_DIHEDRAL, WING_SWEEP = 18, 18            # degrees: the tips raised and swept back
FOOT_C, FOOT_R = np.array([0.045, -0.02, 0.06]), np.array([0.026, 0.034, 0.03])


def taper_back(V):
    """The body's egg, fullest toward the front and narrowing toward the tail."""
    V = V.copy()
    s = 1 - 0.3 * np.clip(V[:, 1], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 2] *= s
    return V


def wing_shape(V):
    """A wing in its own frame: the leading edge straight, the chord and the thickness narrowing to a rounded point
    at the tip (+x)."""
    V = V.copy()
    t = np.clip((V[:, 0] + 1) / 2, 0, 1)
    w = 1 - 0.76 * t ** 1.1
    V[:, 1] = (V[:, 1] + 1) * w - 1
    V[:, 2] *= 1 - 0.45 * t
    return V


def on_ell(c, r, az, el, lift=0.0):
    """The point of an ellipsoid at azimuth az (0 the front, -Y; + toward +X) and elevation el, in degrees, and its
    outward normal there."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    s = 1 / math.sqrt(((d / r) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


def crown_point(k, name, base, d, length, r0, color, bone, tip=0.2):
    """A broad blunt point of the crown, from inside the head out along d: a cone with a rounded tip."""
    d = unit(*d)
    pts = [base - d * r0 * 0.6, base + d * length * 0.35, base + d * length * 0.75, base + d * length]
    k.add(name, tube(pts, [r0, r0 * 0.78, r0 * (0.25 + tip), r0 * tip]), color, bone)


def rounded(corners, r=0.18, n=5):
    """A polygon's corners in u, v rounded off by arcs of radius r (in the unit square's measure)."""
    P = np.array(corners, float)
    out = []
    m = len(P)
    for i in range(m):
        a, b, c = P[i - 1], P[i], P[(i + 1) % m]
        da, dc = unit(*(a - b), 0)[:2], unit(*(c - b), 0)[:2]
        half = math.acos(np.clip(np.dot(da, dc), -1, 1)) / 2
        t = r / math.tan(half)
        p0, p1 = b + da * t, b + dc * t
        bis = unit(*(da + dc), 0)[:2]
        centre = b + bis * (r / math.sin(half))
        a0 = math.atan2(*(p0 - centre)[::-1]); a1 = math.atan2(*(p1 - centre)[::-1])
        da_ = (a1 - a0 + math.pi) % (2 * math.pi) - math.pi
        for j in range(n + 1):
            th = a0 + da_ * j / n
            out.append(centre + r * np.array([math.cos(th), math.sin(th)]))
    return out


TRIANGLE = [(-0.95, 0.62), (0.95, 0.62), (0.0, -0.95)]          # pointing down
QUAD = [(0.0, 0.95), (0.8, 0.0), (0.0, -0.95), (-0.8, 0.0)]     # a rhombus


def turned(pts, roll):
    a = math.radians(roll)
    return [(x * math.cos(a) - y * math.sin(a), x * math.sin(a) + y * math.cos(a)) for x, y in pts]


def mark(k, at, n, size, shape, roll, color, hollow=True, hole=0.32, up=(0, 0, 1)):
    """A mark of the line: a rounded triangle or rhombus of colour, hollow (a white one over its middle, laid higher)
    or solid; size is its half size, roll its turn in degrees."""
    outline = rounded(turned(shape, roll), 0.2)
    k.decal(at, n, (size, size), color, eps=0.003, outline=polygon(outline), rings=10, segs=90, up=up)
    if hollow:
        inner = rounded(turned([(x * hole, y * hole) for x, y in shape], roll), 0.07)
        k.decal(at, n, (size, size), WHITE, eps=0.0042, outline=polygon(inner), rings=8, segs=90, up=up)


# --- the body: one level egg, the head its front -----------------------------------------------------------------------
k.add("body", ell(BODY_C, BODY_R, deform=taper_back), WHITE, "body")
k.add("head", ell(HEAD_C, HEAD_R), WHITE, "head")

# The wings: the body's own sides, flat, swept back, their tips raised
k.add("wing", ell(WING_C, WING_R, rot=(0, -WING_DIHEDRAL, WING_SWEEP), deform=wing_shape, sub=5), WHITE, "wing{s}",
      mirror=True)

# Two small feet under the belly
k.add("foot", ell(FOOT_C, FOOT_R), WHITE, "leg{s}", mirror=True)

# The crown: three broad points sweeping back, red-tipped on the left (-X), white in the middle, blue on the right
CROWN = [(-58, 58, 0.172, 0.054, RED), (0, 74, 0.115, 0.054, None), (58, 58, 0.178, 0.054, BLUE)]
tips = []
for i, (az, el, ln, r0, col) in enumerate(CROWN):
    p, nn = on_ell(HEAD_C, HEAD_R, az, el)
    a, e = math.radians(az), math.radians(el)
    d = unit(math.sin(a) * 0.55, 0.85, 0.55)
    base = p - nn * 0.012
    crown_point(k, f"crown{i}", base, d, ln, r0, WHITE, "head")
    if col:
        tips.append((base, d, ln, col))
for base, d, ln, col in tips:
    def tip(P, base=base, d=d, ln=ln):
        q = P - base
        t = q @ d
        perp = np.linalg.norm(q - np.outer(t, d), axis=1)
        return inside(ln * 0.6 - t, 0.0022) * inside(perp - 0.045, 0.002)
    k.paint(tip, col)

# The tail: a small fan of feathers behind and under, turned out to the right so that it shows from the front
# three-quarter and from behind
for i, (sx, dz, ln) in enumerate(((-0.6, -0.1, 0.075), (-0.2, -0.05, 0.085), (0.2, -0.05, 0.085), (0.6, -0.1, 0.075))):
    root = np.array([sx * 0.03, 0.17, 0.13])
    d = unit(sx * 0.6 + 0.25, 1.0, -0.35 + dz)
    pts = [root, root + d * ln * 0.5, root + d * ln]
    k.add(f"tail{i}", blade(pts, [0.016, 0.018, 0.006], unit(0.3, 0, 1), 0.35), WHITE, "tail", fuse=False)

# --- the marks on the belly: small solid triangles of red and blue -----------------------------------------------------
MARKS = [(-26, -12, 0.024, TRIANGLE, 180, RED), (10, -16, 0.024, TRIANGLE, 0, BLUE), (-10, -34, 0.024, TRIANGLE, 0, RED),
         (30, -30, 0.024, TRIANGLE, 180, BLUE)]
for az, el, size, shape, roll, col in MARKS:
    p, nn = on_ell(BODY_C, BODY_R, az, el)
    mark(k, p, nn, size, shape, roll, col, hollow=False)

# The grooves on the wings' tops: a grey oval with a white one laid over its middle
for s in (1, -1):
    a = math.radians(WING_DIHEDRAL)
    n = np.array([-s * math.sin(a), 0.0, math.cos(a)])
    c = np.array([s * 0.24, 0.02, 0.27])
    k.decal(c, n, (0.07, 0.055), GROOVE, eps=0.003, up=(s * 0.35, -1, 0), rings=12, segs=72)
    k.decal(c, n, (0.06, 0.045), WHITE, eps=0.0042, up=(s * 0.35, -1, 0), rings=12, segs=72)

# --- the mouth (togepi.py's) -------------------------------------------------------------------------------------------
INK, MOUTH, TONGUE, LID = srgb(INKH), srgb(MOUTHH), srgb(TONGUEH), srgb(WHITE)
MOUTH_AT, MOUTH_HALF = (0.0, -0.185, 0.198), (0.02, 0.015)


def mouth(u, v):
    c = np.tile(MOUTH, (len(u), 1))
    c[(u / 0.6) ** 2 + ((v + 0.8) / 0.55) ** 2 < 1] = TONGUE
    return c


def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.3 / s) if s > 0 else 1.0


def lay_mouth(k, at, n, half):
    k.decal(at, n, half, mouth, eps=0.003, outline=mouth_shape, rings=16, segs=64)


lay_mouth(k, MOUTH_AT, unit(0, -1, -0.1), MOUTH_HALF)

k.build(voxel=0.0055, smooth=4, band=0.006)


# --- the eyes (togepi.py's) --------------------------------------------------------------------------------------------
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u - 0.1) / 0.42) ** 2 + ((v - 0.42) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c


EYE_AT, EYE_HALF = np.array([0.045, -0.172, 0.24]), (0.0115, 0.0165)


def eyes(state):
    k.decal(EYE_AT, unit(0.45, -0.88, 0.1), EYE_HALF, eye_look(state, eye, LID, INK, line=0.22), eps=0.004,
            rings=20, segs=56, mirror=True)


k.eye_states(eyes)
k.export(out)
