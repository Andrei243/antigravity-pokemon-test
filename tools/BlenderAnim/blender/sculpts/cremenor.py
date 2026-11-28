# Cremenor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cremenor.py -- <the export's Cremenor.glb> <out.glb>
#
# Cremenel grown (its colours, its flint, its dark eye slot and glowing orange eyes, from cremenel.py): from its picture
# (docs/art/reference/gemini/carpathia/10107-Cremenor.jpg) and its brief ("107. Cremenor"), a hulking hunched golem of
# knapped flint. Every mass is dark glassy plates with gaps between them, and in the gaps a core of orange fire glows
# (the brief's "glowing cracks"): a great hump of plates over the shoulders and behind the head; the head small and
# sunk low and forward between the shoulders, a heavy brow sloping to the nose over two glaring orange eyes in a dark
# slot, a blunt muzzle with a frown; huge angular pauldrons on both shoulders; arms thick as the legs and longer, a
# long flint shard down the outside of each forearm, clenched fists of plates at knee height; short thick bent legs
# on broad clawed feet. The sparks in the air are an effect and are left out.
#
# Written to be grown from: aurifer.py imports it (sibling("cremenor")) for the golem's frame and its plated masses
# (plated, Lot, golem); what each wears on it (pauldrons, shards, crystals, bands) is each species' own.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, polygon, args, hull, axes, boxy, sibling, _ico

c_ = sibling("cremenel")
FLINT, FLINT_DK, FLINT_LT, GLINT = c_.FLINT, c_.FLINT_DK, c_.FLINT_LT, c_.GLINT
EMBER, EMBER_GLOW = "#F0661C", "#FF7426"       # the fire in the cracks: its colour and its light
MOUTH = "#141418"


# --- shapes -------------------------------------------------------------------------------------------------------
def frame_of(d):
    """A turn that takes z to d (x and y square to it): the axes an ellipsoid along a limb is laid in."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    a, b = axes(d)
    return np.stack([a, b, d], axis=1)


def turn(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def seeds_on(n, seed, jitter=0.35):
    """n points spread over the unit sphere (a spiral, jittered): the middles of a mass's plates."""
    rs = np.random.RandomState(seed)
    i = np.arange(n) + 0.5
    z = 1 - 2 * i / n
    t = math.pi * (3 - math.sqrt(5)) * i
    S = np.stack([np.sqrt(1 - z * z) * np.cos(t), np.sqrt(1 - z * z) * np.sin(t), z], axis=1)
    S = S + rs.normal(size=S.shape) * jitter / math.sqrt(n)
    return S / np.linalg.norm(S, axis=1, keepdims=True)


def plated(c, r, n, seed, M=None, gap=0.06, thick=0.08, rise=0.05, skip=None):
    """
    A mass of hard plates: an ellipsoid (centre, radii, turned by M) cut into n cells round its surface, each cell a
    flat-faced chunk standing from 0.95 to 1 + thick of the radius, shrunk toward its own middle by gap so the core
    shows between. rise: each plate stood out a little more or less at random. skip(direction): cells left out.
    Returns [(shape, the plate's middle direction, its draw)].
    """
    M = np.eye(3) if M is None else M
    c, r = np.array(c, float), np.array(r, float)
    U, _ = _ico(4)
    S = seeds_on(n, seed)
    rs = np.random.RandomState(seed + 1)
    cell = np.argmax(U @ S.T, axis=1)
    out = []
    for j in range(n):
        pts = U[cell == j]
        if len(pts) < 4 or (skip is not None and skip(S[j])):
            rs.rand(); continue
        mid = pts.mean(axis=0); mid /= np.linalg.norm(mid)
        q = mid + (pts - mid) * (1 - gap)
        q /= np.linalg.norm(q, axis=1, keepdims=True)
        lift = 1 + thick * (1 + rise / thick * (rs.rand() - 0.3))
        inner = c + (q * r * 0.95) @ M.T
        cos = q @ mid
        plane = max(1 + 0.45 * thick, lift * max(cos.min(), 0.5)) / lift      # the outer face a plane: a knapped facet
        flat = q * (0.45 + 0.55 * plane / np.maximum(cos, 0.5))[:, None]
        outer = c + (flat * r * lift) @ M.T
        out.append((hull(np.vstack([inner, outer])), mid, rs.rand()))
    return out


class Lot:
    """Loose pieces gathered by bone, colour and light, each lot added as one piece."""
    def __init__(self):
        self.lots = {}

    def put(self, shape, colour, bone, glow=None, surface="shell"):
        self.lots.setdefault((bone, colour, glow or "", surface), []).append(shape)

    def flush(self, k, name):
        for i, ((bone, colour, glow, surface), shapes) in enumerate(sorted(self.lots.items())):
            V, F, off = [], [], 0
            for sv, sf in shapes:
                V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
            k.add(f"{name}{i}", (np.vstack(V), F), colour, bone, fuse=False, surface=surface, glow=glow or None)
        self.lots = {}


# --- the golem's frame: every mass from the skeleton's joints (+X side) ---------------------------------------------
def frame():
    G = SimpleNamespace()
    G.H = np.array([0.0, -0.15, 0.6])              # the head, forward and low of its joint (0, -0.08, 0.66)
    G.HR = np.array([0.135, 0.125, 0.125])
    G.EYE_X, G.EYE_Z = 0.056, 0.622
    G.EYE = (0.03, 0.02)
    G.SLOT = (0.108, 0.04)
    # the trunk's masses: (name, centre, radii, turn, plates, seed)
    G.TRUNK = [("chest", (0.0, -0.03, 0.5), (0.22, 0.165, 0.175), (-12, 0, 0), 26, 11),
               ("hump", (0.0, 0.06, 0.69), (0.215, 0.16, 0.135), (-8, 0, 0), 16, 12),
               ("belly", (0.0, -0.035, 0.345), (0.165, 0.13, 0.09), (0, 0, 0), 8, 13),
               ("pelvis", (0.0, 0.02, 0.29), (0.17, 0.125, 0.08), (0, 0, 0), 8, 14)]
    G.SHOULDER = np.array([0.255, 0.0, 0.6])
    G.ELBOW = np.array([0.31, -0.04, 0.41])
    G.WRIST = np.array([0.305, -0.1, 0.25])
    G.FIST = (np.array([0.295, -0.12, 0.165]), np.array([0.085, 0.08, 0.075]))
    G.UPPER, G.FORE = (0.085, 0.08), (0.098, 0.09)    # radii across of the upper arm and the forearm
    G.HIP = np.array([0.125, 0.0, 0.32])
    G.KNEE = np.array([0.16, -0.045, 0.17])
    G.ANKLE = np.array([0.165, -0.03, 0.06])
    G.THIGH, G.SHIN = (0.095, 0.09), (0.082, 0.078)
    G.FOOT = (np.array([0.165, -0.07, 0.034]), np.array([0.085, 0.105, 0.036]))
    G.VOXEL = 0.0075
    return G


def limb(lot, k, a, b, rad, n, seed, bone, tone, core=None, gap=0.06, thick=0.09, grow=1.15):
    """A limb's mass between two joints: plated over a core of its own."""
    a, b = np.array(a, float), np.array(b, float)
    M = frame_of(b - a)
    L = np.linalg.norm(b - a) / 2 * grow
    r = (rad[0], rad[1], max(L, rad[0]))
    c = (a + b) / 2
    if core is not None:
        V, F = ell((0, 0, 0), (r[0] * 0.97, r[1] * 0.97, r[2] * 0.97), sub=3)
        lot.put((c + V @ M.T, F), core[0], bone, glow=core[1], surface=core[2] if len(core) > 2 else "skin")
    for shape, mid, draw in plated(c, r, n, seed, M=M, gap=gap, thick=thick):
        lot.put(shape, tone(mid, draw), bone)


def mass(lot, c, r, rot, n, seed, bone, tone, core=None, gap=0.06, thick=0.08, skip=None):
    """A trunk's mass, plated over its core."""
    M = turn(rot)
    if core is not None:
        V, F = ell((0, 0, 0), np.array(r) * 0.97, sub=3)
        lot.put((np.array(c) + V @ M.T, F), core[0], bone, glow=core[1], surface=core[2] if len(core) > 2 else "skin")
    for shape, mid, draw in plated(c, r, n, seed, M=M, gap=gap, thick=thick, skip=skip):
        lot.put(shape, tone(mid, draw), bone)


def golem(k, G, tone, core, lot=None):
    """The plated trunk, arms, fists, legs and feet, each mass over its own core; the head is each species' own."""
    lot = lot or Lot()
    T = getattr(G, "TONES", {})          # a tone of its own for a mass by name (the trunk's, upper, fore, fist, thigh, shin, foot)
    for name, c, r, rot, n, seed in G.TRUNK:
        # the plates under the head left out, so the head sits into the trunk
        skip = (lambda d: d[1] < -0.72 and abs(d[2]) < 0.45) if name == "hump" else None
        mass(lot, c, r, rot, n, seed, "body", T.get(name, tone), core=core, skip=skip)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        sh, el, wr = G.SHOULDER * m, G.ELBOW * m, G.WRIST * m
        limb(lot, k, sh, el, G.UPPER, 12, 21 + (s > 0), f"arm{side}", T.get("upper", tone), core=core)
        limb(lot, k, el, wr, G.FORE, 13, 23 + (s > 0), f"arm{side}", T.get("fore", tone), core=core, grow=1.25)
        fc, fr = G.FIST
        mass(lot, fc * m, fr, (14, 0, -8 * s), 7, 25 + (s > 0), f"arm{side}", T.get("fist", tone), core=core, gap=0.05, thick=0.12)
        hp, kn, an = G.HIP * m, G.KNEE * m, G.ANKLE * m
        limb(lot, k, hp, kn, G.THIGH, 11, 31 + (s > 0), f"leg{side}", T.get("thigh", tone), core=core, grow=1.25)
        limb(lot, k, kn, an, G.SHIN, 10, 33 + (s > 0), f"leg{side}", T.get("shin", tone), core=core, grow=1.2)
        fc, fr = G.FOOT
        mass(lot, fc * m, fr, (0, 0, 0), 6, 35 + (s > 0), f"leg{side}", T.get("foot", tone), core=core, gap=0.05, thick=0.1,
             skip=lambda d: d[2] < -0.5)
    return lot


def flint_tone(mid, draw):
    """Mostly flint, lit glassy plates toward the top, a few dark."""
    if mid[2] > 0.45 and draw < 0.45:
        return FLINT_LT
    if draw < 0.22:
        return FLINT_DK
    if draw > 0.86:
        return FLINT_LT
    return FLINT


def shard(a, b, w, t, out):
    """A long knapped shard from a to b, w wide, t thick, its edge turned toward out: a flint blade."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; L = np.linalg.norm(d); d /= L
    o = np.array(out, float); o = o - d * (o @ d); o /= np.linalg.norm(o)
    n = np.cross(d, o)
    pts = [a - o * w * 0.2 + n * t * 0.5, a - o * w * 0.2 - n * t * 0.5, a + o * w * 0.35,
           a + d * L * 0.4 + o * w, a + d * L * 0.4 - o * w * 0.25 + n * t * 0.5,
           a + d * L * 0.4 - o * w * 0.25 - n * t * 0.5, a + d * L * 0.75 + o * w * 0.7, b + o * w * 0.1,
           b - o * w * 0.15]
    return hull(pts)


# --- Cremenor's own ----------------------------------------------------------------------------------------------
def pauldrons(lot, G):
    """Huge angular slabs over each shoulder, cracked in three, standing out and up from the arm."""
    for s, side in ((1, "R"), (-1, "L")):
        # the cap of a ball over the shoulder, out and up: a thick curved shield in a few knapped pieces
        c = np.array([0.235 * s, 0.0, 0.575])
        out = unit(0.6 * s, 0.0, 0.8)
        mass(lot, c, (0.15, 0.165, 0.15), (0, 0, 0), 9, 41 + (s > 0), f"arm{side}", flint_tone, gap=0.05,
             thick=0.22, skip=lambda d, out=out: d @ out < 0.5)


def shards(lot, G):
    """A long flint shard down the outside of each forearm, and three along the hump's top."""
    for s, side in ((1, "R"), (-1, "L")):
        el, wr = G.ELBOW * np.array([s, 1, 1.0]), G.WRIST * np.array([s, 1, 1.0])
        a = el + np.array([0.07 * s, 0.02, 0.06])
        b = wr + np.array([0.06 * s, 0.03, -0.02])
        lot.put(shard(a, b, 0.07, 0.04, (s, 0.4, 0)), FLINT_LT, f"arm{side}")
    for x, y, h in ((0.0, 0.11, 0.09), (0.11, 0.08, 0.07), (-0.11, 0.08, 0.07)):
        a = np.array([x, y, 0.73])
        lot.put(shard(a, a + np.array([x * 0.4, 0.05, h]), 0.06, 0.04, (0.2 * np.sign(x + 0.001), 1, 0)), FLINT_LT,
                "body")


def claws(lot, G):
    for s, side in ((1, "R"), (-1, "L")):
        fc, fr = G.FOOT
        for t in (-1, 0, 1):
            root = fc * np.array([s, 1, 1.0]) + np.array([0.05 * t * s, -fr[1] * 0.85, -0.01])
            tip = root + np.array([0.012 * t * s, -0.05, -0.016])
            lot.put(shard(root, tip, 0.03, 0.03, (0, 0, 1)), FLINT_DK, f"leg{side}")


def head(k, G, colour=FLINT, muzzle=FLINT_LT):
    """A small head sunk forward between the shoulders, a blunt muzzle; the brow is a loose slab."""
    k.add("head", ell(G.H, G.HR, deform=boxy(2.4)), colour, "head", surface="shell")
    k.add("muzzle", ell(G.H + np.array([0, -0.085, -0.06]), (0.1, 0.07, 0.055), deform=boxy(2.6)), muzzle, "head",
          surface="shell")
    k.add("neck", ell(G.H + np.array([0, 0.09, -0.02]), (0.12, 0.1, 0.1)), colour, "head", surface="shell")


def brow(lot, G, colour=FLINT_LT):
    """The heavy brow: a slab each side sloping down to the nose, over the slot."""
    y = G.H[1] - G.HR[1] * 0.86
    for s in (1, -1):
        inner = np.array([0.012 * s, y - 0.022, G.EYE_Z + 0.02])
        outer = np.array([0.13 * s, y + 0.035, G.EYE_Z + 0.062])
        pts = []
        for p in (inner, outer):
            for dz in (0.0, 0.034):
                for dy in (0.0, 0.05):
                    pts.append(p + np.array([0, dy, dz]))
        lot.put(hull(pts), colour, "head")


def front(G, x, z):
    q = 1 - (x / G.HR[0]) ** 2 - ((z - G.H[2]) / G.HR[2]) ** 2
    return np.array([x, G.H[1] - G.HR[1] * math.sqrt(max(q, 0.0)), z])


# the glaring eye's outline, u outward: its top sloping down toward the nose
GLARE = [(-1.0, 0.05), (-0.6, 0.32), (0.0, 0.62), (0.6, 0.86), (1.0, 0.6), (1.0, -0.3), (0.6, -0.8), (0.0, -0.95),
         (-0.6, -0.75), (-1.0, -0.35)]
MOUTH_OUT = [(-1.0, -0.3), (-0.6, 0.4), (0.0, 0.7), (0.6, 0.4), (1.0, -0.3), (0.7, -0.65), (0.0, -0.2), (-0.7, -0.65)]


def eyes(k, G, at, normal, outline=GLARE, lid=c_.SLOT, ink=c_.SHUT, eps=0.004, open_look=c_.eye_open):
    """Cremenel's glowing eyes, glaring: their tops cut by the brow's slope; closed, the slot's dark and a line."""
    m = np.array([-1, 1, 1.0])
    LID, INK = srgb(lid), srgb(ink)
    def lay(state):
        look = eye_look(state, open_look, LID, INK, line=0.24)
        # +u is +X on both patches (their right is the world's +X): outward on the right eye, inward on the left
        k.decal(at, normal, G.EYE, look, eps=eps, outline=polygon(outline), rings=20, segs=56)
        k.decal(at * m, normal * m, G.EYE, lambda u, v, look=look: look(-u, v), eps=eps,
                outline=polygon([(-a, b) for a, b in outline]), rings=20, segs=56)
    return lay


def cremenor(src, out_path):
    G = frame()
    k = Kit(src, coat="shell")
    head(k, G)
    lot = golem(k, G, flint_tone, (EMBER, EMBER_GLOW))
    pauldrons(lot, G)
    shards(lot, G)
    claws(lot, G)
    brow(lot, G)
    lot.flush(k, "plates")
    sc = front(G, 0.0, G.EYE_Z)
    c_.slot(k, sc, (0, -1, 0.0), G.SLOT, eps=0.004)
    mz = G.H + np.array([0, -0.085 - 0.07, -0.075])
    k.decal(mz, (0, -1, 0.1), (0.05, 0.016), MOUTH, eps=0.004, outline=polygon(MOUTH_OUT), rings=8, segs=48)
    ob = k.build(voxel=G.VOXEL, smooth=3, band=0.008)
    c_.board(ob)
    k.eye_states(eyes(k, G, front(G, G.EYE_X, G.EYE_Z), unit(0.25, -1, 0.0)))
    k.export(out_path)


if __name__ == "__main__":
    cremenor(*args())
