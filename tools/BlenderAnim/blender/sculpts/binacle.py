# Binacle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/binacle.py -- <the export's Binacle.glb> <out.glb>
#
# From the references: two goose barnacles growing from one rock. The rock is a broad faceted lump, pale grey on top
# with a few dark pits, slate blue down its broken sides. From it rise two orange stalks, each banded once in white;
# the one on the viewer's right is taller. Each carries a brown cup of a shell, wider at its top, its rim broken into
# points, orange flesh showing inside the rim; out of the cup rise three pale yellow-green claws, the middle one
# tallest, curving. On the front of each cup an angry face: small white eyes with black pupils under heavy brown
# brows slanting down to the middle, and under them two big round orange cheeks side by side.
#
# Written to be grown from (barbaracle.py imports it with sibling): a barnacle is one unit, `hand`, placed by a frame
# (its middle, the way its claws point and the way its face looks), a size and a bone, with two eyes, one or none.
# Bones: root, body, armR (the viewer's right, the taller), armL.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, unit, eye_look, args, outward, hull, slab, ring_band, \
    polygon, tri

BROWN, BROWNDEEP, FLESH, CLAW, CHEEK = "#7C5A3A", "#4A3222", "#EE7E3A", "#DCE4A8", "#EE8A48"
STALK, BAND = "#EE7E3A", "#F6F2EC"
ROCKTOP, ROCKSIDE, PIT = "#BEC2CE", "#5C6C9C", "#46506E"
WHITE, PUPIL, INK = "#FAFAF6", "#16120E", "#2A1E16"
X = np.array([1.0, 0, 0]); Z = np.array([0, 0, 1.0])


class Frame:
    """A barnacle's own axes: up (the way its claws go), front (the way its face looks), side = front x up."""
    def __init__(self, c, up, front, size):
        self.c = np.array(c, float)
        self.up = unit(*up)
        f = np.array(front, float); f = f - self.up * (f @ self.up)
        self.front = f / np.linalg.norm(f)
        self.side = np.cross(self.front, self.up)
        self.s = size

    def at(self, x, y, z):
        """A point in the frame: x along side, y along front (negative is behind), z along up, in units of size."""
        return self.c + (self.side * x + self.front * y + self.up * z) * self.s

    def dir(self, x, y, z):
        return unit(*(self.side * x + self.front * y + self.up * z))

    def shape(self, V, F):
        """A shape made about the origin in (x side, y front, z up) placed in the frame."""
        M = np.stack([self.side, self.front, self.up], axis=0)
        return np.asarray(V) * self.s @ M + self.c, F


def cup(V):
    """A ball widened toward its top and flattened there: the shell."""
    V = V.copy()
    V[:, 2] = np.minimum(V[:, 2], 0.62)
    V[:, :2] *= (1 + 0.16 * V[:, 2])[:, None]
    return V


def hand(k, fr, bone, name, claws=3, color=BROWN, flesh=FLESH, claw=CLAW, cheeks=True, rim=True, spread=1.0,
         claw_len=1.0):
    """
    One barnacle: a cup of shell (unit radius 0.05 at size 1), a rim of points round its top, flesh inside, claws out
    of it along up. The face is laid by hand_eyes and hand_marks.
    """
    k.add(name, fr.shape(*ell((0, 0, 0), (0.05, 0.044, 0.05), deform=cup)), color, bone)
    if flesh:
        k.add(name + "flesh", fr.shape(*ell((0, 0, 0.026), (0.047, 0.04, 0.014))), flesh, bone)
    if rim:
        def at(u, v):
            th = 2 * math.pi * u + math.pi / 2           # the seam behind
            top = 0.036 + 0.013 * (0.5 + 0.5 * tri(u * 9))
            zz = 0.02 + (top - 0.02) * (0.015 + 0.97 * v)
            r = 0.0565 + 0.004 * v
            P = np.stack([r * np.cos(th), r * 0.86 * np.sin(th) * -1, zz], -1)
            return P
        V, F = sheet(at, 109, 6, 0.007)
        k.add(name + "rim", outward(fr.shape(V, F)), color, bone, fuse=False)
    # The claws: tapering blades out of the flesh, the middle one tallest, the sides leaning out
    for i in range(claws):
        t = 0.0 if claws == 1 else (i / (claws - 1) - 0.5) * 2
        lean = t * 0.55 * spread
        L = (0.095 if abs(t) < 0.01 else 0.075) * claw_len
        root = np.array([t * 0.03, 0.002, 0.02])
        d1 = np.array([lean, -0.05, 1.0]); d1 /= np.linalg.norm(d1)
        bend = np.array([-0.22 + lean * 0.4, 0.05, 0.0])
        pts = [root, root + d1 * L * 0.4, root + d1 * L * 0.75 + bend * L * 0.25, root + d1 * L + bend * L * 0.55]
        V, F = blade(pts, [0.0165, 0.014, 0.009, 0.0032], (0, 1, 0), 0.62)
        k.add(f"{name}claw{i}", outward(fr.shape(V, F)), claw, bone, fuse=False, surface="shell")


def face_point(fr, x, z):
    """A point on the cup's front at side x and height z (unit size), and its outward normal."""
    r = np.array([0.05, 0.044, 0.05])
    q = cup(np.array([[x / 0.05, 0, z / 0.05]]))[0]
    w = 1 + 0.16 * (z / 0.05)
    y = -0.044 * w * math.sqrt(max(1 - (x / (0.05 * w)) ** 2 - (z / 0.05) ** 2, 0.05))
    p = fr.at(x, -y, z)
    n = fr.dir(x / (0.05 * w) ** 2, -y / (0.044 * w) ** 2, z / 0.05 ** 2 * 0.5)
    return p, n


EYE_SHAPE = [(-1.0, -0.3), (-0.6, -0.85), (0.5, -0.85), (1.0, -0.2), (0.9, 0.62), (-0.85, 0.05)]


def open_eye(u, v):
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[((u + 0.28) / 0.42) ** 2 + ((v + 0.12) / 0.5) ** 2 < 1] = srgb(PUPIL)
    return c


def hand_eyes(k, fr, state, xs=(0.019,), z=0.006, half=(0.0125, 0.011), lid=BROWN, mirror=True):
    """The angry eyes of a barnacle's face, at side xs (and their mirror), in a state."""
    look = eye_look(state, open_eye, srgb(lid), srgb(INK), line=0.18)
    for x in xs:
        for s in ((1, -1) if mirror else (1,)):
            p, n = face_point(fr, x * s, z)
            # the patch's u runs along up x normal: keep +u outward, away from the face's middle
            right = np.cross(fr.up, n)
            o = 1 if (p - fr.at(0, 0, z)) @ right > 0 else -1
            col = look if o > 0 else (lambda u, v, f=look: f(-u, v))
            shape = EYE_SHAPE if o > 0 else [(-u, v) for u, v in EYE_SHAPE]
            k.decal(p, n, (half[0] * fr.s, half[1] * fr.s), col, outline=polygon(shape), eps=0.0018 * max(fr.s, 1),
                    rings=20, segs=56, up=fr.up)


def hand_marks(k, fr, cheeks=True, brows=True, x=0.019, z=0.006, color=CHEEK, brow=BROWNDEEP):
    """What is in every state of a barnacle's face: the brows over the eyes and the round cheeks under them."""
    if brows:
        for s in (1, -1):
            p, n = face_point(fr, (x + 0.002) * s, z + 0.0125)
            k.decal(p, n, (0.016 * fr.s, 0.0042 * fr.s), brow, eps=0.0026 * max(fr.s, 1), rings=8, segs=32,
                    up=fr.dir(-0.42 * s, 0, 1))
    if cheeks:
        for s in (1, -1):
            p, n = face_point(fr, 0.0185 * s, -0.022)
            k.decal(p, n, (0.0165 * fr.s, 0.0145 * fr.s), color, eps=0.0015 * max(fr.s, 1), rings=12, segs=44,
                    up=fr.up)


# ---- the rock -------------------------------------------------------------------------------------------------------
ROCK_C, ROCK_R = np.array([0.0, 0.0, 0.058]), np.array([0.135, 0.095, 0.07])


def rock_points(c, r, n, seed, flat_top=0.62, flat_bottom=None):
    rs = np.random.RandomState(seed)
    d = rs.normal(size=(n, 3)); d /= np.linalg.norm(d, axis=1, keepdims=True)
    d *= (0.84 + 0.16 * rs.rand(n))[:, None]
    P = d * np.array(r)
    P[:, 2] = np.clip(P[:, 2], -c[2] if flat_bottom is None else -r[2] * flat_bottom, r[2] * flat_top)
    return P + np.array(c)


def rock(k, c=ROCK_C, r=ROCK_R, seed=5, bone="body", top=ROCKTOP, side=ROCKSIDE, pits=PIT, name="rock", n=34,
         flat_bottom=None):
    P = rock_points(c, r, n, seed, flat_bottom=flat_bottom)
    k.add(name, hull(P, merge=5.0), side, bone, fuse=False, surface="shell")
    # the pale top: the upper points a little higher and wider, so it caps the slate sides with a broken edge
    up = P[P[:, 2] > c[2] + r[2] * 0.05]
    cap = (up - c) * np.array([1.015, 1.015, 1.0]) + c + Z * 0.003 * max(r[2] / 0.07, 1)
    lip = up.copy(); lip[:, 2] = c[2] + r[2] * 0.05
    rs = np.random.RandomState(seed + 1)
    lip[:, 2] += rs.rand(len(lip)) * r[2] * 0.3
    capshape = hull(np.vstack([cap, (lip - c) * 1.012 + c]), merge=5.0)
    k.add(name + "top", capshape, top, bone, fuse=False, surface="shell")
    return capshape


def pit(k, shape, x, y, r, color=PIT, bone="body", name="pit"):
    """A dark pit laid on the rock's pale top where a ray from above meets it: a thin plate in the face's plane."""
    V, F = shape
    tree = BVHTree.FromPolygons(np.asarray(V).tolist(), F)
    hit = tree.ray_cast(Vector((x, y, 2.0)), Vector((0, 0, -1.0)))
    if hit[0] is None:
        return
    p, n = np.array(hit[0]), np.array(hit[1])
    a = np.cross(n, (0, 1.0, 0)); a /= np.linalg.norm(a); b = np.cross(n, a)
    th = np.linspace(0, 2 * math.pi, 7, endpoint=False)
    out_ = [(r * math.cos(t) * (1 + 0.15 * math.sin(3 * t)), r * 0.7 * math.sin(t)) for t in th]
    k.add(name, slab(out_, p + n * 0.0008, a, b, 0.0025), color, bone, fuse=False, surface="shell")


# ---- the stalks -----------------------------------------------------------------------------------------------------
def stalk(k, a, b, r, bone, name, color=STALK, band=BAND, band_at=0.45, band_len=0.28):
    a, b = np.array(a, float), np.array(b, float)
    k.add(name, tube([a, (a + b) / 2, b], [r, r, r * 0.97], seg=18, per=6), color, bone)
    L = np.linalg.norm(b - a)
    k.add(name + "band", ring_band(a + (b - a) * band_at, b - a, r - 0.0015, L * band_len, thick=0.004), band, bone,
          fuse=False)


TALL = Frame((0.082, -0.004, 0.222), (0.06, 0.0, 1.0), (0.12, -1.0, 0.0), 1.1)
SHORT = Frame((-0.078, -0.01, 0.178), (-0.08, 0.0, 1.0), (-0.16, -1.0, 0.0), 1.04)


def binacle(src, out):
    k = Kit(src, coat="shell")
    cap = rock(k)
    for i, (x, y, r) in enumerate(((-0.02, -0.035, 0.009), (0.02, 0.04, 0.007), (-0.07, 0.0, 0.006), (0.075, -0.035, 0.006))):
        pit(k, cap, x, y, r, name=f"pit{i}")
    stalk(k, (0.062, 0.0, 0.085), TALL.at(0, 0, -0.04), 0.0205, "armR", "stalkR", band_at=0.55)
    stalk(k, (-0.058, 0.0, 0.085), SHORT.at(0, 0, -0.04), 0.0205, "armL", "stalkL", band_at=0.58, band_len=0.4)
    for fr, bone, nm in ((TALL, "armR", "handR"), (SHORT, "armL", "handL")):
        hand(k, fr, bone, nm)
        hand_marks(k, fr)
    k.build(voxel=0.0034, smooth=3, band=0.004, wband=0.025)

    def lay(state):
        for fr in (TALL, SHORT):
            hand_eyes(k, fr, state)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    binacle(src, out)
