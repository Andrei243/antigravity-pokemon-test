# Togetic sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/togetic.py -- <the export's Togetic.glb> <out.glb>
#
# Grown from togepi.py: its white, its red and blue, its crown_point(), its hollow mark() and its way of making the eyes
# and the mouth. From the references: all white now, out of the shell; a pear of a body, fullest low down, narrowing
# into a short thick neck; a big head, wider at its top, as wide as the body nearly, whose two top corners are the
# crown's two broad points, with a lower third point behind them; small black upright eyes with a glint, a small open
# happy mouth; stubby flat arms out at the sides; two leaf-shaped wings rising up, out and back from the upper back,
# each with a groove round inside its edge; two oval feet pointing forward; hollow marks on the belly, a red triangle in
# the middle, a blue one on its right side and a red one on its left. The brief's "small round head" is not the
# pictures': the head is large, about as wide as the body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, tri, polygon, leaf, args

src, out = args()
k = Kit(src, coat="fur")

# --- the line's colours (togepi.py's) ----------------------------------------------------------------------------------
WHITE, CREAM, RED, BLUE = "#F7F6F1", "#F6EBA6", "#E4463C", "#3E9BE6"
INKH, MOUTHH, TONGUEH = "#17151C", "#A42C3C", "#F2879A"
GROOVE = "#C9CDD6"                            # the wings' grooves, the soft grey the pictures draw them in

# --- the frame ---------------------------------------------------------------------------------------------------------
BODY_C = np.array([0.0, 0.0, 0.215])         # the pear's middle (the body bone is at 0, 0, 0.274)
BODY_R = np.array([0.112, 0.104, 0.145])
NECK = [(0.0, -0.002, 0.3), (0.0, -0.008, 0.35), (0.0, -0.014, 0.395)]
HEAD_C = np.array([0.0, -0.018, 0.46])       # the head bone is at 0, -0.01, 0.394
HEAD_R = np.array([0.1, 0.09, 0.083])
ARM_ROOT, ARM_TIP = np.array([0.075, -0.04, 0.3]), np.array([0.13, -0.065, 0.272])
FOOT_C, FOOT_R, FOOT_TURN = np.array([0.058, -0.05, 0.088]), np.array([0.034, 0.058, 0.03]), 14
WING_ROOT, WING_DIR, WING_LEN, WING_W = np.array([0.06, 0.06, 0.33]), (0.72, 0.32, 0.62), 0.3, 0.072
WING_FACE = (0.3, -0.92, 0.12)               # the wing's face turned to the front and out, its back to behind


def pear(V):
    """Fullest low down, narrowing upward into the neck, its bottom a little flatter."""
    V = V.copy()
    s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.34 * np.clip(V[:, 2], 0, 1) ** 1.6
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < 0, np.maximum(V[:, 2], -0.94), V[:, 2])
    return V


def wide_top(V):
    """A head wider across its top than at its chin."""
    V = V.copy()
    V[:, 0] *= 1 + 0.16 * V[:, 2]
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


def vane(base, d, face, length, width, thick):
    """A flat pointed leaf from base along d, its flat face turned toward face (the kit's leaf() always faces the
    sky's side of d, which left the wings edge-on from the front)."""
    d = unit(*d)
    face = np.array(face, float); face = face - d * np.dot(face, d); face /= np.linalg.norm(face)
    side = np.cross(face, d)          # (side, face, d) right-handed, or the faces are wound inside out
    def shape(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        w = np.sqrt(np.clip(t, 0, 1)) * (1 - t ** 1.6) * 1.6
        V[:, 0] *= w; V[:, 1] *= w
        return V
    V, F = ell((0, 0, 0), (width, thick, length / 2), deform=shape, sub=3)
    return V @ np.stack([side, face, d], axis=1).T + np.array(base, float) + d * length / 2, F


def wing(k, name, root, d, face, length, width, bone):
    """A leaf of a wing with a groove round inside its edge: a white leaf, a thicker grey one a little smaller
    through it, and a thicker white one smaller still, so that only a band of grey shows on both faces."""
    k.add(name, vane(root, d, face, length, width, 0.024), WHITE, bone, fuse=False)
    d_ = unit(*d)
    k.add(name + "groove", vane(root + d_ * length * 0.13, d, face, length * 0.78, width * 0.74, 0.028), GROOVE, bone,
          fuse=False)
    k.add(name + "inner", vane(root + d_ * length * 0.17, d, face, length * 0.7, width * 0.6, 0.032), WHITE, bone,
          fuse=False)


# --- the body ----------------------------------------------------------------------------------------------------------
k.add("body", ell(BODY_C, BODY_R, deform=pear), WHITE, "body")
k.add("necklow", tube(NECK[:2], [0.052, 0.046]), WHITE, "body")
k.add("neckup", tube(NECK[1:], [0.046, 0.05]), WHITE, "head")
k.add("head", ell(HEAD_C, HEAD_R, deform=wide_top), WHITE, "head")

# The crown: the head's two top corners drawn out into broad points, and a lower one behind them
CROWN = [(62, 50, 0.085, 0.05), (-62, 50, 0.085, 0.05), (180, 52, 0.06, 0.042)]
for i, (az, el, ln, r0) in enumerate(CROWN):
    p, nn = on_ell(HEAD_C, HEAD_R, az, max(el - 15, 0))
    a, e = math.radians(az), math.radians(el)
    d = (math.sin(a) * math.cos(e) * 0.75, -math.cos(a) * math.cos(e) * 0.6 + 0.12, math.sin(e) * 1.25)
    crown_point(k, f"crown{i}", p - nn * 0.012, d, ln, r0, WHITE, "head")

# Stubby flat arms out at the sides, a little down
k.add("arm", tube([ARM_ROOT, ARM_ROOT + (ARM_TIP - ARM_ROOT) * 0.55, ARM_TIP], [0.03, 0.028, 0.022]), WHITE,
      "arm{s}", mirror=True)
# Oval feet pointing forward, on short legs out of the body's bottom
k.add("leg", tube([(0.045, 0.0, 0.15), (0.055, -0.02, 0.1)], [0.036, 0.032]), WHITE, "leg{s}", mirror=True)
k.add("foot", ell(FOOT_C, FOOT_R, rot=(0, 0, FOOT_TURN)), WHITE, "leg{s}", mirror=True)

# The wings: up, out and back from the upper back, so they show from the front three-quarter and from behind
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    wing(k, f"wing{sd}", WING_ROOT * np.array([s, 1, 1]), (WING_DIR[0] * s, WING_DIR[1], WING_DIR[2]),
         (WING_FACE[0] * s, WING_FACE[1], WING_FACE[2]), WING_LEN, WING_W, f"wing{sd}")

# --- the marks on the belly: hollow triangles, red in the middle, blue on its right side, red on its left -----------
MARKS = [(0, -12, 0.036, TRIANGLE, 0, RED), (-62, -18, 0.032, TRIANGLE, 8, BLUE), (62, -18, 0.032, TRIANGLE, -8, RED)]
for az, el, size, shape, roll, col in MARKS:
    p, nn = on_ell(BODY_C, BODY_R, az, el)
    mark(k, p, nn, size, shape, roll, col)

# --- the mouth (togepi.py's) -------------------------------------------------------------------------------------------
INK, MOUTH, TONGUE, LID = srgb(INKH), srgb(MOUTHH), srgb(TONGUEH), srgb(WHITE)
MOUTH_AT, MOUTH_HALF = (0.0, -0.1, 0.428), (0.022, 0.016)


def mouth(u, v):
    c = np.tile(MOUTH, (len(u), 1))
    c[(u / 0.6) ** 2 + ((v + 0.8) / 0.55) ** 2 < 1] = TONGUE
    return c


def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.3 / s) if s > 0 else 1.0


def lay_mouth(k, at, n, half):
    k.decal(at, n, half, mouth, eps=0.003, outline=mouth_shape, rings=16, segs=64)


lay_mouth(k, MOUTH_AT, unit(0, -1, 0.15), MOUTH_HALF)

k.build(voxel=0.0055, smooth=4, band=0.006)


# --- the eyes (togepi.py's) --------------------------------------------------------------------------------------------
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u - 0.1) / 0.42) ** 2 + ((v - 0.42) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c


EYE_AT, EYE_HALF = np.array([0.04, -0.1, 0.468]), (0.0125, 0.019)


def eyes(state):
    k.decal(EYE_AT, unit(0.4, -0.9, 0.08), EYE_HALF, eye_look(state, eye, LID, INK, line=0.22), eps=0.004,
            rings=20, segs=56, mirror=True)


k.eye_states(eyes)
k.export(out)
