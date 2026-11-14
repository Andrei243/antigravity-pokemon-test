# Spiritomb sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/spiritomb.py -- <the export's Spiritomb.glb> <out.glb>
#
# From the references: a round cloud of pink-mauve mist standing upright and facing forward, wider than tall, torn
# into sharp jagged points round its edge (most at the upper right, the right, the left and the lower left, the top
# left a smooth arc); a ring of green orbs on it, six large ones with yellow hearts and smaller plain ones between; in
# its middle one green face: a zigzag band of a mouth, a thin stem rising from it that forks into two eyes, a thin
# hook on the -X side and a fat leaf with a black spiral in it on the +X side; under the cloud a low round keystone,
# beige-grey, a little wider at its foot, with a crack, the cloud's lower edge hanging in front of its top.
# The skeleton: root (0, 0, 0) and body (0, 0, 0.48). Everything hangs from body, the stone too, as the C# sculpt has it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

MIST, MISTEDGE, MISTDEEP, SWIRL = "#CF90BC", "#B97CA8", "#A9719A", "#B57BA5"
ORB, CORE, FACE, SPIRAL = "#64AE28", "#F7D75A", "#74C238", "#161A12"
STONE, CRACK = "#C9BBA7", "#8C7F6E"

C = np.array([0.0, 0.0, 0.475])        # the cloud's middle, by the body bone
A, B, CZ = 0.42, 0.11, 0.375           # its half width, half thickness, half height
P_LENS = 0.7                           # the lens: a flatter front than a ball's, a rounded edge


# The cloud's outline is torn, not round: its radius is a function of the angle (0 at +X, counter-clockwise from the
# front), with a few broad pointed tongues and notches, unequal and unevenly spaced, smooth stretches between.
# (angle in degrees, how far out (+) or in (-) as a share of the radius, half width in degrees)
LOBES = [(52, 0.21, 24),            # the big torn peak at the upper right
         (29, -0.08, 11),           # the notch under it
         (-9, 0.15, 18),            # a tongue trailing off at the right
         (-33, -0.06, 11),
         (-90, 0.06, 45),           # the lower edge hanging a little lower, over the stone's top
         (98, 0.05, 12),            # a small tear at the top
         (149, 0.12, 15),           # the left bay: a tongue above it,
         (172, -0.11, 15),          # the bay,
         (198, 0.08, 11),           # a short tear below it,
         (223, 0.18, 19)]           # and a long tongue trailing off at the lower left


def radius(th):
    """The outline's radius (1 is the plain ellipse's) at angles th, in radians."""
    r = np.ones_like(th, dtype=float)
    for deg, h, w in LOBES:
        d = np.abs((th - math.radians(deg) + math.pi) % (2 * math.pi) - math.pi) / math.radians(w)
        r += h * np.clip(1 - d, 0, 1) ** 1.6          # pointed, not round: torn
    return r


def lens(V):
    V = V.copy()
    V[:, 1] = np.sign(V[:, 1]) * np.abs(V[:, 1]) ** P_LENS
    R = radius(np.arctan2(V[:, 2], V[:, 0]))
    V[:, 0] *= R; V[:, 2] *= R
    return V


def front(x, z):
    """The cloud's front surface (its y) at x, z, through the same torn outline."""
    nx, nz = x / A, (z - C[2]) / CZ
    R = float(radius(np.array([math.atan2(nz, nx)]))[0])
    q = 1 - (nx * nx + nz * nz) / (R * R)
    return -B * max(q, 0.0) ** (P_LENS / 2)


def rho(P):
    """How far out on the cloud a point is: 1 at its torn rim."""
    nx, nz = P[:, 0] / A, (P[:, 2] - C[2]) / CZ
    return np.sqrt(nx * nx + nz * nz) / radius(np.arctan2(nz, nx))


# --- the cloud ------------------------------------------------------------------------------------------------------
k.add("mist", ell(C, (A, B, CZ), deform=lens, sub=5), MIST, "body")
# Four ragged flaps carrying the tongues on, flat in the cloud's plane and leaning clockwise as the swirl turns
FLAPS = [(54, 0.09, 0.075, 1), (-8, 0.07, 0.065, 0), (148, 0.06, 0.06, 0), (224, 0.09, 0.07, 1)]
for i, (deg, ln, w, twin) in enumerate(FLAPS):
    t = math.radians(deg)
    R = float(radius(np.array([t]))[0])
    rim = np.array([A * math.cos(t), 0.0, CZ * math.sin(t)]) * R
    out_ = unit(math.cos(t) / A, 0, math.sin(t) / CZ)
    lean = unit(math.sin(t), 0, -math.cos(t))           # clockwise
    tipd = unit(*(out_ + lean * 0.5))
    k.add(f"flap{i}", blade([C + rim * 0.86, C + rim * 0.97 + tipd * ln * 0.3, C + rim * 0.97 + tipd * ln],
                            [w, w * 0.5, 0.006], (0, 1, 0), 0.3), MIST, "body")
    if twin:
        t2 = t + math.radians(13)
        R2 = float(radius(np.array([t2]))[0])
        rim2 = np.array([A * math.cos(t2), 0.0, CZ * math.sin(t2)]) * R2
        d2 = unit(*(unit(math.cos(t2) / A, 0, math.sin(t2) / CZ) + lean * 0.3))
        k.add(f"flap{i}b", blade([C + rim2 * 0.88, C + rim2 * 0.97 + d2 * ln * 0.2, C + rim2 * 0.97 + d2 * ln * 0.55],
                                 [w * 0.65, w * 0.35, 0.006], (0, 1, 0), 0.3), MIST, "body")
# Where it rises from the stone, behind: a short column of mist, so from behind it reads as coming out of the stone
k.add("column", tube([(0, 0.035, 0.1), (0, 0.04, 0.2), (0, 0.03, 0.3)], [0.075, 0.09, 0.1]), MIST, "body")

# Shading: darker toward the torn edge and toward the stone; the face's middle stays the plain mist the lids are
k.paint(lambda P: smoothstep(0.86, 1.02, rho(P)) * 0.85, MISTEDGE)
k.paint(lambda P: smoothstep(0.24, 0.1, P[:, 2]) * 0.9, MISTDEEP)
# From behind: three dark arms of a swirl, so the back reads as mist turning and not a plain plate
def swirl(P):
    x, z = P[:, 0] / A, (P[:, 2] - C[2]) / CZ
    r = np.sqrt(x * x + z * z); ph = np.arctan2(z, x)
    band = np.sin(3 * (ph + 3.2 * r))
    return smoothstep(0.25, 0.55, band) * smoothstep(0.12, 0.3, r) * smoothstep(0.01, 0.04, P[:, 1])
k.paint(swirl, SWIRL)

# --- the orbs: loose, sunk a little into the front; the large ones with a yellow heart -------------------------------
LARGE = [(-0.178, 0.743), (0.046, 0.75), (0.306, 0.532), (-0.345, 0.452), (0.202, 0.272), (-0.096, 0.211)]
SMALL = [(0.165, 0.702, 0.033), (0.232, 0.648, 0.023), (0.304, 0.424, 0.033), (0.274, 0.352, 0.023),
         (0.098, 0.217, 0.029), (-0.211, 0.243, 0.035), (-0.276, 0.302, 0.024), (-0.324, 0.582, 0.038),
         (-0.29, 0.656, 0.023)]
RL = 0.06
for i, (x, z) in enumerate(LARGE):
    y = front(x, z) + RL * 0.3
    k.add(f"orb{i}", ell((x, y, z), (RL, RL * 0.75, RL), sub=3), ORB, "body", fuse=False, surface="shell")
    rc = RL * 0.47
    k.add(f"core{i}", ell((x, y - RL * 0.75 + rc * 0.12, z), (rc, rc * 0.32, rc), sub=3), CORE, "body",
          fuse=False, surface="shell")
for i, (x, z, r) in enumerate(SMALL):
    y = front(x, z) + r * 0.3
    k.add(f"seed{i}", ell((x, y, z), (r, r * 0.75, r), sub=3), ORB, "body", fuse=False, surface="shell")

# --- the face: the mouth's zigzag band and the stem, crisp patches on the cloud's front -------------------------------
MOUTH = [(-0.18, 0.461), (-0.141, 0.374), (-0.093, 0.426), (-0.028, 0.33), (0.028, 0.413), (0.076, 0.343),
         (0.15, 0.456)]
HT = 0.034                             # the band's half thickness, measured upright
J = np.array([-0.045, 0.497])          # where the stem forks into the eyes
FN = (0, -1.0, 0)


def strip(p0, p1, half, eps, extend=0.006, upright=True, half1=None):
    """
    A patch of a band from p0 to p1 (x, z): a parallelogram half as thick upright (upright) or a quad as thick across
    its length, half at p0 and half1 at p1 (a point where half1 is nought), its ends lapped a little.
    """
    p0, p1 = np.array(p0, float), np.array(p1, float)
    d = (p1 - p0) / np.linalg.norm(p1 - p0)
    a, b = p0 - d * extend, p1 + d * extend
    m = (a + b) / 2
    S = 0.1
    if upright:
        o0 = o1 = np.array([0, half])
    else:
        n = np.array([-d[1], d[0]])
        o0, o1 = n * half, n * (half if half1 is None else half1)
    pts = [(a - m - o0) / S, (b - m - o1) / S, (b - m + o1) / S, (a - m + o0) / S]
    k.decal((m[0], front(m[0], m[1]), m[1]), FN, (S, S), FACE, eps=eps, outline=polygon(pts), rings=12, segs=120)


for i in range(len(MOUTH) - 1):
    strip(MOUTH[i], MOUTH[i + 1], HT, 0.005 + 0.0008 * (i % 2))
# The band's two ends rise to points, as the drawing's do
strip((-0.178, 0.445), (-0.19, 0.515), 0.024, 0.0058, upright=False, half1=0.003)
strip((0.148, 0.44), (0.168, 0.508), 0.024, 0.0058, upright=False, half1=0.003)
# The stem from the mouth's top up to the fork, and a round knot at the fork
strip((-0.04, 0.37), tuple(J), 0.014, 0.006, upright=False, half1=0.011)
k.decal((J[0], front(*J), J[1]), FN, (0.017, 0.017), FACE, eps=0.006, rings=8, segs=40)

# --- the stone it is bound to: a low round keystone, wider at its foot, with a crack ---------------------------------
def lathe(prof, cy, seg=48):
    """A solid of turning: prof is (radius, height) from the bottom's middle round to the top's middle."""
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    V = [(0, cy, prof[0][1])]
    for r, h in prof[1:-1]:
        V.extend((r * math.cos(a), cy + r * math.sin(a), h) for a in ang)
    V.append((0, cy, prof[-1][1]))
    n = len(prof) - 2
    F = []
    for j in range(seg):
        F.append([0, 1 + (j + 1) % seg, 1 + j])
        for i in range(n - 1):
            a0, a1 = 1 + i * seg + j, 1 + i * seg + (j + 1) % seg
            F.append([a0, a1, a1 + seg, a0 + seg])
        top = len(V) - 1
        F.append([top, 1 + (n - 1) * seg + j, 1 + (n - 1) * seg + (j + 1) % seg])
    return np.array(V, float), F


SY = 0.07                               # set back, so the cloud's lower edge hangs in front of its top
k.add("stone", lathe([(0, 0), (0.18, 0.0), (0.188, 0.012), (0.185, 0.03), (0.172, 0.105), (0.162, 0.122),
                      (0.145, 0.132), (0.0, 0.134)], SY), STONE, "body", fuse=False, surface="shell")
# a crack down its front, on the +X side, as the drawing has a notch
cr = []
for x, z in ((0.07, 0.134), (0.085, 0.1), (0.075, 0.075), (0.095, 0.04)):
    rr = 0.18 - 0.08 * z
    cr.append((x, SY - math.sqrt(rr * rr - x * x) + 0.004, z))
k.add("crack", tube(cr, [0.007, 0.006, 0.006, 0.004], seg=8, per=4), CRACK, "body", fuse=False, surface="shell")

k.build(voxel=0.0065, smooth=3)

# --- the eyes: a thin hook on -X, a fat leaf with a black spiral on +X, both from the fork ----------------------------
LID, GREEN, INK = srgb(MIST), srgb(FACE), srgb(FACE)


def circle3(p, q, r):
    """The middle and radius of the circle through three points (x, z)."""
    p, q, r = (np.array(t, float) for t in (p, q, r))
    m = np.array([[2 * (q[0] - p[0]), 2 * (q[1] - p[1])], [2 * (r[0] - p[0]), 2 * (r[1] - p[1])]])
    c = np.linalg.solve(m, [q @ q - p @ p, r @ r - p @ p])
    return c, float(np.linalg.norm(c - p))


def crescent(tip, outer, inner):
    """In a circle through the fork, the tip and a point of the outer edge, not in the one through an inner one."""
    co, ro = circle3(J, tip, outer)
    ci, ri = circle3(J, tip, inner)
    def f(x, z):
        return (np.hypot(x - co[0], z - co[1]) < ro) & (np.hypot(x - ci[0], z - ci[1]) > ri)
    return f


# The hook: both its edges bow out toward the lower -X side, so it is a thin horn and not a half disc
HOOK = crescent((-0.143, 0.598), (-0.13, 0.513), (-0.107, 0.535))
LEAF = crescent((0.15, 0.586), (0.07, 0.462), (0.05, 0.54))
# (middle, half sizes, shape, lift): each patch holds its whole shape, so no edge of it is cut by the patch's rim
EYES = {1: (np.array([0.052, 0.53]), (0.135, 0.09), LEAF, 0.004),
        -1: (np.array([-0.094, 0.548]), (0.075, 0.075), HOOK, 0.0048)}


# Each eye's own tight box round its shape (middle, half sizes): the frame its state's lines are drawn in, so a shut
# line is as long as the eye and no ink falls on bare mist
BOX = {1: (np.array([0.0525, 0.525]), (0.103, 0.07)), -1: (np.array([-0.095, 0.5475]), (0.055, 0.058))}


def world_look(state):
    """The state's colour at world points (x, z), the same for both patches wherever they overlap at the fork."""
    looks = {}
    for side, shape in ((1, LEAF), (-1, HOOK)):
        b0, bh = BOX[side]
        def own(u, v, side=side, shape=shape, b0=b0, bh=bh):        # u outward
            c = np.tile(LID, (len(u), 1))
            c[shape(b0[0] + side * u * bh[0], b0[1] + v * bh[1])] = GREEN
            return c
        looks[side] = eye_look(state, own, LID, INK, line=0.2)
    def look(x, z):
        c = np.tile(LID, (len(x), 1))
        for side in (1, -1):
            b0, bh = BOX[side]
            u, v = side * (x - b0[0]) / bh[0], (z - b0[1]) / bh[1]
            pick = ((x >= J[0]) if side == 1 else (x < J[0])) & (np.abs(u) <= 1) & (np.abs(v) <= 1)
            if pick.any():
                c[pick] = looks[side](u[pick], v[pick])
        return c
    return look


SC, SR, TURNS = np.array([0.062, 0.502]), 0.03, 1.6


def spiral(u, v):
    """A bold black spiral of TURNS turns on the leaf's green (u, v over the spiral's own patch)."""
    r = np.sqrt(u * u + v * v) * 1.0
    ph = np.mod(np.arctan2(v, u), 2 * math.pi)
    a = 1.0 / (TURNS * 2 * math.pi)
    d = np.full(len(u), 9.0)
    for n in range(3):
        th = ph + 2 * math.pi * n
        ok = th <= TURNS * 2 * math.pi
        d = np.where(ok, np.minimum(d, np.abs(r - a * th)), d)
    c = np.tile(GREEN, (len(u), 1))
    c[(d < 0.14) & (r < 1.0)] = srgb(SPIRAL)
    c[r < 0.12] = srgb(SPIRAL)
    return c


def eyes(state):
    look = world_look(state)
    for side in (1, -1):
        c0, h, _, lift = EYES[side]
        # the patch's u runs toward +X on both (laid by hand along -Y): back to world x, z
        col = lambda u, v, c0=c0, h=h: look(c0[0] + u * h[0], c0[1] + v * h[1])
        k.decal((c0[0], front(*c0), c0[1]), FN, h, col, eps=lift, rings=20, segs=56)
    if state in ("Open", "Fierce"):
        k.decal((SC[0], front(*SC), SC[1]), FN, (SR * 1.18, SR * 1.18), spiral, eps=0.0075, rings=30, segs=96)


k.eye_states(eyes)
k.export(out)
