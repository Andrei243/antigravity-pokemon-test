# Togepi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/togepi.py -- <the export's Togepi.glb> <out.glb>
#
# The design, from the references: one egg of an outline, its lower two thirds a white eggshell whose broken rim runs
# round it in a zigzag, its top a cream-yellow head that ends in a crown of broad blunt points spreading up and out
# all round; hollow marks on the shell, red rounded quadrilaterals and blue rounded triangles, round the back too;
# stubby cream arms out of the shell's sides just under the rim, held up and out; two cream feet under the shell
# pointing forward and out; small upright black eyes with a glint; a small open happy mouth, dark red with a pink
# tongue, just above the rim.
# Togetic and Togekiss grow from this script: its colours, crown_point(), mark() and the way the eyes and the mouth are
# made are copied into theirs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, tri, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# --- the line's colours ------------------------------------------------------------------------------------------------
WHITE, CREAM, RED, BLUE = "#F7F6F1", "#F6EBA6", "#E4463C", "#3E9BE6"
INKH, MOUTHH, TONGUEH = "#17151C", "#A42C3C", "#F2879A"

# --- the frame ---------------------------------------------------------------------------------------------------------
SHELL_C = np.array([0.0, 0.0, 0.165])        # the shell's middle; it is widest a little below
SHELL_R = np.array([0.162, 0.148, 0.15])
HEAD_C = np.array([0.0, -0.004, 0.282])      # the head that fills the shell's top (the head bone is at 0, 0, 0.26)
HEAD_R = np.array([0.136, 0.126, 0.08])
RIM_Z, RIM_AMP, RIM_TEETH = 0.25, 0.024, 8   # the rim's middle height, its zigzag's half height, teeth round it
ARM_ROOT, ARM_TIP = np.array([0.105, -0.02, 0.2]), np.array([0.205, -0.035, 0.262])
FOOT_C, FOOT_R, FOOT_TURN = np.array([0.075, -0.05, 0.022]), np.array([0.036, 0.05, 0.023]), 22


def shell_egg(V):
    """An egg as it stands: a little fuller below its middle, narrower toward its top, its bottom a little flatter."""
    V = V.copy()
    s = 1 + 0.04 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1) ** 1.6
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < 0, np.maximum(V[:, 2], -0.96), V[:, 2])
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


def star_point(k, name, base, d, length, r0, color, bone, tip=0.3):
    """A soft point of a star: an egg half sunk in the head, its outer half tapering smoothly to a rounded tip, as
    wide at its foot as it stands out (a tube of a few radii left knobs at its joints)."""
    d = unit(*d)
    def taper(V):
        V = V.copy()
        t = np.clip(V[:, 2], 0, 1)
        w = 1 - (1 - tip) * t ** 1.15
        V[:, 0] *= w; V[:, 1] *= w
        return V
    V, F = ell((0, 0, 0), (r0, r0, length), deform=taper)
    x = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(x) < 1e-3:
        x = np.array([1.0, 0, 0])
    x /= np.linalg.norm(x)
    y = np.cross(d, x)
    k.add(name, (V @ np.stack([x, y, d], axis=1).T + np.array(base, float), F), color, bone)


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


# --- the body ----------------------------------------------------------------------------------------------------------
k.add("shell", ell(SHELL_C, SHELL_R, deform=shell_egg), WHITE, "body", surface="shell")
k.add("head", ell(HEAD_C, HEAD_R), CREAM, "head")

# The crown: a soft star of five broad short points, as wide at the foot as they are long, with rounded tips: one
# straight up, two up and out, two low at the sides behind the arms; none stands forward over the face
CROWN = [(0, 90, 0.085, 0.062), (74, 42, 0.085, 0.062), (-74, 42, 0.085, 0.062), (102, 10, 0.078, 0.058),
         (-102, 10, 0.078, 0.058)]
for i, (az, el, ln, r0) in enumerate(CROWN):
    p, nn = on_ell(HEAD_C, HEAD_R, az, el)
    a, e = math.radians(az), math.radians(el)
    d = (math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))
    star_point(k, f"crown{i}", p - nn * 0.01, d, ln, r0, CREAM, "head")

# The rim: white below a zigzag round the upright axis, cream above it, crisp between shell and head
def rim(P):
    th = np.arctan2(P[:, 0], -P[:, 1])
    return RIM_Z + RIM_AMP * tri(th * RIM_TEETH / (2 * math.pi))
k.paint(lambda P: inside(P[:, 2] - rim(P), 0.0022), WHITE, on=["shell", "head", "crown"])
k.paint(lambda P: inside(rim(P) - P[:, 2], 0.0022), CREAM, on=["shell", "head", "crown"])

# Stubby arms out of the shell's sides just under the rim, held up and out
k.add("arm", tube([ARM_ROOT, ARM_ROOT + (ARM_TIP - ARM_ROOT) * 0.55, ARM_TIP], [0.034, 0.03, 0.024]), CREAM,
      "arm{s}", mirror=True)
# Feet under the shell, pointing forward and out
k.add("foot", ell(FOOT_C, FOOT_R, rot=(0, 0, FOOT_TURN)), CREAM, "leg{s}", mirror=True)
# the limbs' own cream back over the white the rim's paint left on their roots
k.paint(lambda P: np.ones(len(P)), CREAM, on=["arm", "foot"])

# --- the marks: hollow red rhombi and blue triangles, round the back too ----------------------------------------------
MARKS = [(-50, 24, 0.02, QUAD, 10, RED), (42, 18, 0.031, QUAD, -18, RED), (-12, 6, 0.042, TRIANGLE, 4, BLUE),
         (-58, -18, 0.038, QUAD, 30, RED), (32, -32, 0.034, TRIANGLE, 176, BLUE),
         (155, 12, 0.036, TRIANGLE, 10, BLUE), (-150, 20, 0.03, QUAD, -20, RED), (112, -22, 0.03, QUAD, 15, RED),
         (-108, -6, 0.032, TRIANGLE, 190, BLUE)]
for az, el, size, shape, roll, col in MARKS:
    p, nn = on_ell(SHELL_C, SHELL_R * np.array([1.0, 1.0, 1.0]), az, el)
    mark(k, p, nn, size, shape, roll, col)

# --- the mouth: small, open and happy, flat across its top and round below, a tongue in it ----------------------------
INK, MOUTH, TONGUE, LID = srgb(INKH), srgb(MOUTHH), srgb(TONGUEH), srgb(CREAM)
MOUTH_AT, MOUTH_HALF = (0.0, -0.125, 0.268), (0.019, 0.014)


def mouth(u, v):
    c = np.tile(MOUTH, (len(u), 1))
    c[(u / 0.6) ** 2 + ((v + 0.8) / 0.55) ** 2 < 1] = TONGUE
    return c


def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.3 / s) if s > 0 else 1.0


def lay_mouth(k, at, n, half):
    k.decal(at, n, half, mouth, eps=0.003, outline=mouth_shape, rings=16, segs=64)


lay_mouth(k, MOUTH_AT, unit(0, -1, 0.1), MOUTH_HALF)

k.build(voxel=0.0045, smooth=4, band=0.006)


# --- the eyes: small upright black ovals with a white glint high on the outer side -------------------------------------
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u - 0.1) / 0.42) ** 2 + ((v - 0.42) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c


EYE_AT, EYE_HALF = np.array([0.042, -0.116, 0.302]), (0.0115, 0.0175)


def eyes(state):
    k.decal(EYE_AT, unit(0.34, -0.92, 0.12), EYE_HALF, eye_look(state, eye, LID, INK, line=0.22), eps=0.004,
            rings=20, segs=56, mirror=True)


k.eye_states(eyes)
k.export(out)
