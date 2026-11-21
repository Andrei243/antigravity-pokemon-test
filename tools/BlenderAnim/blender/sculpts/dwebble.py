# Dwebble sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dwebble.py -- <the export's Dwebble.glb> <out.glb>
#
# From the references: a small orange hermit crab in a grey stone. The stone is behind and over it, chunky and
# faceted, a big front chunk with a dark opening low on its front and a second chunk rising behind it, a small tan
# fleck on its side. Out of the opening comes a round orange body-and-head with a small pink "o" of a mouth; two short
# thick stalks rise from its top, each carrying a big glossy black oval eye with a white glint. Two great round orange
# claws (as big as the head) in front, low, each ending below in one thick pointed fang that reaches the ground; two
# short pointed orange legs a side under the stone.
#
# Written to be grown from (crustle.py imports it with sibling): the frame is in the constants and the functions that
# take the kit; the build is under the main guard. Bones: root, body, head, arm{s}, leg{s}, hind{s}.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, slab
from mathutils import Euler, Vector
from mathutils.bvhtree import BVHTree

ORANGE, FANG, LEG = "#EE8034", "#F39A52", "#E8782E"
STONE, STONE2, FLECK, HOLE = "#86817D", "#7A7571", "#D8C6A0", "#24201E"
EYE, GLINT, INK, MOUTH, LIP = "#1C1C22", "#F4F4F4", "#8C8C96", "#E58A9A", "#5A2A22"

X = np.array([1.0, 0, 0]); Z = np.array([0, 0, 1.0])


def rotm(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


# ---- a chunk of stone: points round an ellipsoid, jittered, made a hull ---------------------------------------------
def chunk(c, r, n=26, seed=1, jitter=(0.86, 1.0), rot=(0, 0, 0), clip=None):
    rs = np.random.RandomState(seed)
    d = rs.normal(size=(n, 3)); d /= np.linalg.norm(d, axis=1, keepdims=True)
    d *= (jitter[0] + (jitter[1] - jitter[0]) * rs.rand(n))[:, None]
    P = d * np.array(r)
    if clip is not None:
        P = clip(P)
    P = P @ rotm(rot).T + np.array(c)
    return hull(P, merge=6.0)


def onto(shape, origin, direction):
    """Where a ray from origin along direction meets a loose piece, and its normal there."""
    V, F = shape
    tree = BVHTree.FromPolygons(np.asarray(V).tolist(), F)
    hit = tree.ray_cast(Vector(origin), Vector(unit(*direction)))
    return np.array(hit[0]), np.array(hit[1])


def fleck(k, shape, origin, direction, r, color=FLECK, bone="body", name="fleck"):
    """A small flat stone chip laid on a loose piece where a ray meets it."""
    p, n = onto(shape, origin, direction)
    a = np.cross(n, Z if abs(n[2]) < 0.9 else X); a /= np.linalg.norm(a); b = np.cross(n, a)
    pts = [p + n * 0.0015 + r * f * (math.cos(t) * a + math.sin(t) * b * 0.75)
           for t, f in zip(np.linspace(0, 2 * math.pi, 6, endpoint=False), (1.0, 0.8, 1.1, 0.7, 0.95, 0.85))]
    pts += [q - n * 0.004 for q in pts]
    k.add(name, hull(pts, merge=0), color, bone, fuse=False, surface="shell")


# ---- the stone ------------------------------------------------------------------------------------------------------
FRONT_C, FRONT_R = np.array([0.0, 0.045, 0.128]), np.array([0.106, 0.09, 0.108])
BACK_C, BACK_R = np.array([0.0, 0.135, 0.15]), np.array([0.088, 0.078, 0.09])
FRONT_FACE = -0.036            # the front chunk's front is cut flat here, where the opening is


def stone(k):
    def flat_front(P):
        P = P.copy()
        P[:, 1] = np.maximum(P[:, 1], FRONT_FACE - FRONT_C[1])
        return P
    front = chunk(FRONT_C, FRONT_R, n=30, seed=4, clip=flat_front)
    back = chunk(BACK_C, BACK_R, n=26, seed=9, rot=(-14, 0, 6))
    k.add("stone", front, STONE, "body", fuse=False, surface="shell")
    k.add("stoneback", back, STONE2, "body", fuse=False, surface="shell")
    # the opening: a dark irregular plate on the flat front, low
    th = np.linspace(0, 2 * math.pi, 14, endpoint=False)
    f = 1 + 0.08 * np.cos(3 * th + 0.5) + 0.05 * np.sin(5 * th)
    outline = np.stack([0.054 * f * np.cos(th), 0.05 * f * np.sin(th)], 1)
    k.add("hole", slab(outline, (0, FRONT_FACE - 0.001, 0.1), X, Z, 0.004), HOLE, "body", fuse=False,
          surface="shell")
    fleck(k, back, (0.3, BACK_C[1], BACK_C[2] + 0.03), (-1, 0, 0.05), 0.012)
    return front, back


# ---- the body and head: one round orange mass in the opening --------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.07, 0.108]), np.array([0.042, 0.037, 0.039])


def body(k, color=ORANGE):
    k.add("head", ell(HEAD_C, HEAD_R), color, "head")
    k.add("body", ell((0.0, -0.05, 0.074), (0.036, 0.032, 0.03)), color, "body")


# ---- the eyes on their stalks: big glossy black ovals ---------------------------------------------------------------
STALK = [(0.012, -0.08, 0.135), (0.02, -0.087, 0.16), (0.026, -0.091, 0.178)]
EYE_C, EYE_R = np.array([0.03, -0.095, 0.196]), np.array([0.0145, 0.0125, 0.021])


def stalks(k, color=ORANGE):
    k.add("stalk", tube(STALK, [0.0095, 0.0088, 0.0082], seg=16, per=6), color, "head", mirror=True)
    k.add("eyeball", ell(EYE_C, EYE_R, rot=(0, 18, 0)), EYE, "head", mirror=True)


def open_eye(u, v):
    c = np.tile(srgb(EYE), (len(u), 1))
    c[((u + 0.12) / 0.3) ** 2 + ((v - 0.38) / 0.36) ** 2 < 1] = srgb(GLINT)
    return c


def eyes(k):
    p = EYE_C + np.array([0.004, -EYE_R[1] * 0.98, 0.002])
    def lay(state):
        k.decal(p, unit(0.3, -1, 0.12), (0.012, 0.0185), eye_look(state, open_eye, srgb(EYE), srgb(INK), line=0.17),
                eps=0.002, rings=20, segs=56, mirror=True)
    return lay


def mouth(k):
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(srgb(MOUTH), (len(u), 1))
        c[r > 0.62] = srgb(LIP)
        return c
    k.decal(HEAD_C + np.array([0.0, -HEAD_R[1] * 0.93, -0.012]), unit(0, -1, -0.25), (0.0068, 0.0085), look,
            eps=0.002, rings=10, segs=40)


# ---- the claws: great round balls, a thick pointed fang below each ---------------------------------------------------
def cone(root, tip, r, n=8):
    root, tip = np.array(root, float), np.array(tip, float)
    d = unit(*(tip - root))
    a = np.cross(d, Z if abs(d[2]) < 0.9 else X); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    pts = [root + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    pts.append(root - d * r * 0.5)
    pts.append(tip)
    return hull(pts, merge=0)


ARM = [(0.035, -0.06, 0.08), (0.058, -0.08, 0.074)]
CLAW_C, CLAW_R = np.array([0.08, -0.098, 0.068]), np.array([0.034, 0.032, 0.034])
FANG_ROOT, FANG_TIP, FANG_R = (0.074, -0.114, 0.044), (0.064, -0.136, 0.002), 0.0175


def claws(k, color=ORANGE):
    k.add("arm", tube(ARM, [0.013, 0.014], seg=14, per=6), color, "arm{s}", mirror=True)
    k.add("claw", ell(CLAW_C, CLAW_R), color, "arm{s}", mirror=True)
    k.add("fang", cone(FANG_ROOT, FANG_TIP, FANG_R), FANG, "arm{s}", fuse=False, mirror=True, surface="shell")


# ---- the legs: short pointed hooks from under the stone --------------------------------------------------------------
LEGS = [("leg{s}", [(0.04, 0.0, 0.05), (0.07, -0.014, 0.036), (0.078, -0.02, 0.0)]),
        ("hind{s}", [(0.04, 0.05, 0.05), (0.072, 0.058, 0.036), (0.082, 0.064, 0.0)])]


def legs(k, color=LEG):
    for i, (bone, pts) in enumerate(LEGS):
        k.add(f"leg{i}", outward(tube(pts, [0.0125, 0.011, 0.004], seg=14, per=6)), color, bone, fuse=False,
              mirror=True, surface="shell")


def dwebble(src, out):
    k = Kit(src, coat="shell")
    stone(k)
    body(k)
    stalks(k)
    claws(k)
    legs(k)
    mouth(k)
    k.build(voxel=0.0035, smooth=3, band=0.005)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    dwebble(src, out)
