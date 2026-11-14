# Glaceon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glaceon.py -- <the export's Glaceon.glb> <out.glb>
# Grown from eevee.py (the same frame of four legs and a trunk, the same head, eyes, nose and mouth), changed where
# the pictures change it: a slender pale icy-blue fox on longer legs with bright blue socks; a blue cap that IS the top
# of the head, its edge coming down to a point between the eyes and cutting their tops; two thin blue locks hanging
# beside the cheeks, each ending in a diamond; ears that are flat diamonds lined bright blue; blue diamonds on the
# back; a slender tail ending in a big flat diamond, swung out to +X; dark-blue eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
ICE = "#C3E6F0"            # the coat: between the drawing's pale mint and the 3D model's pale icy blue
BLUE = "#2B9AD0"           # the cap, the locks, the socks, the diamonds' rims
DEEP = "#16679A"           # the diamonds' hearts
EARIN = "#46B6E4"          # inside the ears
EYE, PUPIL = "#1D4C84", "#0C1A32"   # dark-blue eyes, a darker middle
NOSE, INK = "#25364C", "#203047"

# --- the frame (the skeleton's joints: body 0.24 up, head joint (0, -0.13, 0.39), ears (+-0.055, -0.13, 0.49),
#     forelegs (+-0.05, -0.08, 0.22), hind legs (+-0.05, 0.13, 0.22), tail (0, 0.16, 0.26)) -------------------------------
HS = 1.22                                # the head's scale against the first draft's: large, as in the pictures
H = np.array([0, -0.155, 0.448])         # the head's centre, over and in front of its joint
HR = np.array([0.088, 0.082, 0.082]) * HS
LEG_X = 0.052
FORE_Y, HIND_Y = -0.08, 0.13
BODY_Z = 0.245
SOCK = 0.072                             # the socks' top


def head_shape(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head():
    k.add("head", ell(H, HR, deform=head_shape), ICE, "head")
    # The cap's mass: the skull rises a little into it at the top and the back, so the cap is the head's own top
    k.add("cap", ell(H + HS * np.array((0, 0.012, 0.012)), HS * np.array((0.087, 0.084, 0.076))), ICE, "head")
    k.add("muzzle", ell(H + HS * np.array((0, -0.068, -0.04)), HS * np.array((0.039, 0.03, 0.024))), ICE, "head")
    # A short thick neck, seen in front: Glaceon has no ruff
    k.add("neck", tube([(0, -0.085, 0.27), (0, -0.115, 0.33), (0, -0.14, 0.39)], [0.068, 0.064, 0.06]), ICE, "head")
    k.add("withers", ell((0, -0.09, 0.3), (0.064, 0.066, 0.075)), ICE, "body")      # the neck flows into the chest


def add_body():
    # Eevee's sturdy trunk carried higher on the longer legs: deep chest, broad shoulders and haunches
    k.add("chest", ell((0, -0.07, BODY_Z + 0.004), (0.074, 0.078, 0.09)), ICE, "body")
    k.add("middle", ell((0, 0.03, BODY_Z - 0.004), (0.066, 0.11, 0.078)), ICE, "body")
    k.add("rump", ell((0, 0.12, BODY_Z - 0.002), (0.07, 0.07, 0.084)), ICE, "body")


def add_legs(color=ICE):
    # Eevee's legs drawn out: shoulder and thigh masses over their upper halves, tapering to clear rounded paws
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.004, 0.2), (0.04, 0.052, 0.075)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X - 0.004, FORE_Y + 0.005, 0.22), (LEG_X, FORE_Y, 0.12), (LEG_X + 0.002, FORE_Y - 0.004, 0.03)],
                      [0.038, 0.03, 0.027]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.002, FORE_Y - 0.014, 0.019), (0.03, 0.037, 0.021)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.002, HIND_Y + 0.005, 0.19), (0.045, 0.062, 0.08)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.01, 0.17), (LEG_X + 0.002, HIND_Y + 0.008, 0.1),
                        (LEG_X + 0.002, HIND_Y - 0.002, 0.03)], [0.037, 0.03, 0.027]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.002, HIND_Y - 0.012, 0.019), (0.03, 0.037, 0.021)), color, "hind{s}", mirror=True)
    # Blue socks with a crisp top; the coat painted back above them
    low = lambda P: (P[:, 2] < 0.16) * (np.abs(P[:, 0]) > 0.015)
    k.paint(lambda P: inside(P[:, 2] - SOCK, 0.003) * low(P), BLUE)
    k.paint(lambda P: (1 - inside(P[:, 2] - SOCK, 0.003)) * low(P), ICE)


# --- flat plates with crisp edges: the ears, the locks' and the tail's diamonds ---------------------------------------
def slab(at, a_dir, b_dir, outline, thick):
    """An extruded flat polygon: outline in (a, b) counter-clockwise, at + a a_dir + b b_dir, thick along a x b."""
    a_dir = np.array(a_dir, float); b_dir = np.array(b_dir, float)
    n = np.cross(a_dir, b_dir); n /= np.linalg.norm(n)
    P = np.array([np.array(at, float) + a * a_dir + b * b_dir for a, b in outline])
    c = P.mean(axis=0)
    m = len(P)
    top, bot = P + n * thick / 2, P - n * thick / 2
    V = np.vstack([top, bot, [c + n * thick / 2, c - n * thick / 2]])
    ct, cb = 2 * m, 2 * m + 1
    F = []
    for i in range(m):
        j = (i + 1) % m
        F.append([ct, i, j])
        F.append([cb, m + j, m + i])
        F.append([i, m + i, m + j, j])
    return V, F


def frame(a, normal):
    """In-plane axes for a plate whose length runs along a and whose face looks along normal."""
    a = np.array(a, float); a /= np.linalg.norm(a)
    b = np.cross(normal, a); b /= np.linalg.norm(b)
    return a, b


DIAMOND = lambda L, W, at=0.45: [(0, 0), (L * at, -W), (L, 0), (L * at, W)]


# --- the ears: flat diamonds leaning out from the cap, their faces to the front, lined bright blue ------------------
EAR_ROOT = (0.056, -0.14, 0.485)
EAR_DIR = unit(0.6, 0.12, 1)


def add_ears():
    a, b = frame(EAR_DIR, unit(0, -1, 0.15))
    k.add("ear", slab(EAR_ROOT, a, b, [(0, -0.022), (0.1, -0.056), (0.24, 0), (0.1, 0.056), (0, 0.022)], 0.012),
          ICE, "ear{s}", fuse=False, mirror=True)
    k.add("earin", slab(EAR_ROOT, a, b, [(0.05, 0), (0.115, -0.036), (0.215, 0), (0.115, 0.036)], 0.017),
          EARIN, "ear{s}", fuse=False, mirror=True)


# --- the locks: thin blue ribbons from the cap's sides, hanging by the cheeks, each ending in a diamond --------------
LOCK = [H + HS * np.array(o) for o in ((0.074, -0.005, 0.005), (0.09, -0.025, -0.025), (0.097, -0.041, -0.06),
                                       (0.099, -0.05, -0.095))]
LOCK_FACE = unit(1, -0.85, 0)


def add_locks():
    k.add("lock", blade(LOCK, [0.013, 0.0145, 0.0145, 0.014], LOCK_FACE, 0.42), BLUE, "head", fuse=False, mirror=True)
    a, b = frame(unit(0.12, -0.12, -1), LOCK_FACE)
    top = np.array(LOCK[-1]) + (0, 0, 0.012)
    k.add("pendant", slab(top, a, b, DIAMOND(0.105, 0.036), 0.011), BLUE, "head", fuse=False, mirror=True)
    inner = [(0.025, 0), (0.058, -0.02), (0.093, 0), (0.058, 0.02)]
    k.add("pendantin", slab(top, a, b, inner, 0.015), DEEP, "head", fuse=False, mirror=True)


# --- the tail: a slender stem back and up, swung 35 degrees out to +X, ending in a big flat diamond in the same plane
TAIL_ROOT = np.array([0.0, 0.16, 0.27])
TAIL_H = unit(0.57, 0.82, 0)
UP = np.array([0, 0, 1.0])
STEM = [TAIL_ROOT + TAIL_H * s + UP * z for s, z in ((0, 0), (0.06, 0.004), (0.12, 0.03), (0.17, 0.075), (0.2, 0.13))]


def add_tail():
    k.add("tail", tube(STEM, [0.034, 0.024, 0.018, 0.016, 0.015]), ICE, "tail")
    a, b = frame(TAIL_H * 0.45 + UP, np.cross(TAIL_H, UP))
    base = STEM[-1] - (TAIL_H * 0.45 + UP) / np.linalg.norm(TAIL_H * 0.45 + UP) * 0.015
    k.add("fin", slab(base, a, b, [(0, -0.012), (0.105, -0.064), (0.225, 0), (0.105, 0.064), (0, 0.012)], 0.013),
          BLUE, "tail", fuse=False)
    k.add("finin", slab(base, a, b, [(0.04, 0), (0.105, -0.04), (0.19, 0), (0.105, 0.04)], 0.017),
          DEEP, "tail", fuse=False)


# --- the cap: paint round the head's upright axis; its edge slants down over each eye to a point between them, sweeps
#     down the temples where the locks hang, and covers the back of the head -----------------------------------------
CAP_A = [0, 0.21, 0.35, 0.49, 0.63, 0.77, 0.95, 1.3, math.pi]
CAP_Z = [HS * z for z in (-0.007, 0.001, 0.004, 0.006, 0.006, 0.0, 0.0, -0.018, -0.034)]


def add_cap():
    def cap(P):
        Q = P - H
        a = np.abs(np.arctan2(Q[:, 0], -Q[:, 1]))
        ze = np.interp(a, CAP_A, CAP_Z)
        # Wide at the front, where the face stands forward of the ball; close round the back, or it runs down the neck
        near = (((Q - (0, 0.01, 0.02)) / (HR * np.where(a < 1.6, 1.5, 1.1)[:, None])) ** 2).sum(1) < 1
        return inside(ze - Q[:, 2], 0.0025) * near * (P[:, 2] > 0.37)
    k.paint(cap, BLUE)
    # The cap's point between the eyes as a patch, so its V edge is crisp where the paint's would be ragged
    k.decal(H + unit(0, -0.96, 0.29) * 0.083 * HS, unit(0, -1, 0.3), (0.04 * HS, 0.03 * HS), BLUE, eps=0.003,
            outline=polygon([(-1, 1), (1, 1), (1, -0.33), (0, -1), (-1, -0.33)]), rings=10, segs=60)


# --- the back's diamonds: crisp patches cast from above ----------------------------------------------------------------
RHOMB = polygon([(1, 0), (0, 1), (-1, 0), (0, -1)])


def add_marks():
    k.decal((0, 0.035, 0.36), (0, 0, 1), (0.042, 0.052), BLUE, up=(0, -1, 0), outline=RHOMB, rings=10, segs=48)
    k.decal((0, 0.122, 0.36), (0, 0, 1), (0.03, 0.034), BLUE, up=(0, -1, 0), outline=RHOMB, rings=8, segs=40)


# --- the face: Eevee's, the eyes' tops cut by the cap ------------------------------------------------------------------
CB, CI = srgb(ICE), srgb(INK)
CN = srgb(BLUE)


def add_nose(at=H + HS * np.array((0, -0.098, -0.026)), half=(0.011, 0.0085), color=NOSE):
    k.decal(at, unit(0, -1, 0.1), half, color, eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]),
            rings=6, segs=24)


def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.2 + 1.4 * (np.abs(u) - 0.45) ** 2)) < 0.26) & (np.abs(u) < 0.88)] = CI
    return c


def add_mouth(at=H + HS * np.array((0, -0.092, -0.05)), half=(0.022, 0.013), look=mouth):
    k.decal(at, unit(0, -1, -0.2), half, look, eps=0.003, rings=16, segs=72)


EYE_DIR = unit(0.47, -0.88, -0.02)
EYE_HALF = (0.025 * HS, 0.028 * HS)          # as large on the face as Eevee's
CE, CP = srgb(EYE), srgb(PUPIL)


def cut(u):
    """The cap's edge across the eye: lower at the inner corner, rising outward."""
    return 0.38 + 0.15 * u


def eye(u, v):
    # A dark pupil filling the eye, a lighter blue glow in its lower part, a white glint up and out under the cap
    c = np.tile(CP, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.85) / 0.38) ** 2 < 1] = CE
    c[((u - 0.3) / 0.24) ** 2 + ((v - 0.02) / 0.2) ** 2 < 1] = (1, 1, 1)
    return c


# The eye's outline is its ellipse clipped by the cap's edge, so that edge is the patch's own crisp outline (a line
# drawn by the colour function steps at the patch's vertices); the cap's paint ends a little under it.
_th = np.linspace(0, 2 * math.pi, 64, endpoint=False)
EYE_OUTLINE = polygon([(math.cos(t), min(math.sin(t), cut(math.cos(t)))) for t in _th])
E = H + EYE_DIR * 0.083 * HS + np.array([0, 0, 0.004])


def eyes(state):
    k.decal(E, EYE_DIR, EYE_HALF, eye_look(state, eye, CB, CI), eps=0.004, outline=EYE_OUTLINE,
            rings=20, segs=56, mirror=True)


add_body()
add_legs()
add_head()
add_cap()
add_ears()
add_locks()
add_tail()
add_marks()
add_nose()
add_mouth()

k.build(voxel=0.0056)
k.eye_states(eyes)
k.export(out)
