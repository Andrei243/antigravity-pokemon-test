# Incineroar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Litten's and Torracat's scripts (litten.py, torracat.py, imported as siblings: the line's colours, its solid points
# of fur, its cut eye, its bands round a limb and its spiky tail tuft):
#   blender --background --factory-startup --python sculpts/incineroar.py -- <the export's Incineroar.glb> <out.glb>
# From the references: a great heel of a tiger wrestler standing upright. A big red head with a broad black muzzle and
# jaw, a grin of white teeth across it; fierce yellow eyes under heavy red brows, the black running back from under
# them; red ears swept back and red points of fur swept back from the cheeks; a mane of black spikes behind the head
# and over the shoulders; a broad grey chest and belly with black marks (a bar across the chest, one down the middle,
# short bars slanting off it), the waist pinched at a belt of fire (yellow at its heart, orange and red at its tips);
# great red arms with three black bands each, huge red hands with white claws (carried forward and down here, raised
# in the art); thick red thighs, a black band at the knee and one at the ankle, big red feet; a red tail with black
# bands and a black spiky ruff near its tip. The back is black after the C# sculpt (the pictures show only the front).
# Reference ratios (3D picture, to the crown): head crown to chin / height 0.25 (0.31 here, crown 0.808, jaw 0.558);
# chest width / height 0.36 (0.38 here); arm thickness / height 0.11 (0.13 here); hand / head width 0.55 (0.47 here);
# arms: hand centres at x +-0.135 inside the chest's half-width 0.155, y -0.2 before the belly's front (-0.09), the elbows
# bent out to x +-0.275 (the model's width);
# belt's height / height 0.45 (0.44 here).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, polygon, axes, eye_look, args,
                       sibling)

lt = sibling("litten")

RED = "#E2392E"
BLACK = "#2A2C33"
GREY = "#6E7079"
WHITE, CLAW = "#F4F4F2", "#ECECEE"
YELLOW, AMBER = lt.YELLOW, lt.AMBER
IRIS = "#3E9A4A"
INK = lt.INK
PAD = "#F4904A"
FLAME_RED, FLAME, FLAME_CORE = "#E8452A", "#F68A2A", "#FFD23A"

# --- the frame (the skeleton's joints: body (0, 0, 0.42), arms (+-0.12, 0, 0.54), head (0, -0.02, 0.58), legs
#     (+-0.07, 0, 0.28), tail (0, 0.08, 0.34)) -----------------------------------------------------------------------
H = np.array([0.0, -0.065, 0.69])
HR = np.array([0.124, 0.113, 0.118])
BELT = 0.36
SH = np.array([0.155, 0.0, 0.565])            # the shoulder's middle
EL = np.array([0.225, -0.03, 0.455])          # the elbow, bent out
WR = np.array([0.155, -0.16, 0.37])           # the wrist, in front of the belly
HAND = np.array([0.135, -0.2, 0.33])


def tongue(base, d, length, width, depth=None, bend=0.0):
    """A flame's tongue: round at its root, tapering to a point along d, its tip bent up by bend."""
    d = unit(*d)
    depth = depth or width * 0.8
    def shape(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        w = 1 - 0.9 * t ** 1.3
        V[:, 0] *= w; V[:, 1] *= w
        return V
    V, F = ell((0, 0, 0), (width, depth, length / 2), deform=shape, sub=3)
    a, b = axes(d)
    t = V[:, 2] / length + 0.5
    V = V[:, :1] * a + V[:, 1:2] * b + V[:, 2:] * d + np.outer(t ** 2 * bend * length, (0, 0, 1.0))
    return outward((V + np.array(base, float) + d * length / 2, F))


class Pieces:
    """Several shapes joined into one loose piece (one material for a colour of flame)."""
    def __init__(self):
        self.V, self.F = [], []

    def put(self, shape):
        V, F = outward(shape)
        o = sum(len(v) for v in self.V)
        self.V.append(V); self.F.extend([[i + o for i in f] for f in F])

    def shape(self):
        return np.vstack(self.V), self.F


# --- the trunk: a broad chest, the waist pinched at the belt, red hips ------------------------------------------------
def add_body(k):
    k.add("chest", ell((0, -0.005, 0.53), (0.155, 0.112, 0.1)), GREY, "body")
    k.add("abs", ell((0, -0.0, 0.435), (0.112, 0.092, 0.1)), GREY, "body")
    k.add("hips", ell((0, 0.012, 0.33), (0.128, 0.1, 0.075)), RED, "body")
    k.add("neck", tube([(0, -0.02, 0.58), (0, -0.045, 0.65)], [0.085, 0.08]), RED, "head")
    # the grey stops at the belt, red below
    k.paint(lambda P: inside(P[:, 2] - (BELT - 0.004), 0.003), RED, on=["abs", "hips", "chest"])
    # the black marks on the front: a bar across the chest, a bar down the middle, short bars slanting off it
    def marks(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        ax = np.abs(x)
        front = (y < -0.04)
        across = inside(np.abs(z - 0.49) - 0.016 + 0.12 * np.clip(ax - 0.05, 0, 1), 0.003) * inside(ax - 0.1, 0.003)
        down = inside(ax - 0.015, 0.003) * inside(z - 0.5, 0.003) * inside(0.405 - z, 0.003)
        slant = inside(np.abs(z - (0.45 - 0.5 * (ax - 0.01))) - 0.012, 0.003) * inside(ax - 0.07, 0.003)
        top = 0 * z
        return np.maximum.reduce([across, down, slant, top]) * front
    k.paint(marks, BLACK)
    # the back black above the belt, the grey only on the front
    k.paint(lambda P: inside(-0.015 - P[:, 1], 0.004) * inside(BELT - P[:, 2], 0.003), BLACK, on=["chest", "abs"])


def add_belt(k):
    """The belt of fire round the waist: tongues out and up all round, red and orange at the tips, yellow at heart."""
    red, mid, core = Pieces(), Pieces(), Pieces()
    n = 16
    for i in range(n):
        a = 2 * math.pi * i / n
        s_, c = math.sin(a), math.cos(a)
        base = np.array([0.112 * s_, 0.012 - 0.096 * c, BELT - 0.01])
        out = np.array([s_, -c, 0.0])
        (red if i % 2 else mid).put(tongue(base, out * 0.55 + (0, 0, 0.85), 0.09 + 0.02 * (i % 3 == 0), 0.034,
                                           bend=0.1))
        core.put(tongue(base + out * 0.012 + (0, 0, -0.005), out * 0.4 + (0, 0, 0.9), 0.06, 0.024))
    k.add("flamered", red.shape(), FLAME_RED, "body", fuse=False, glow=FLAME_RED)
    k.add("flamemid", mid.shape(), FLAME, "body", fuse=False, glow=FLAME)
    k.add("flamecore", core.shape(), FLAME_CORE, "body", fuse=False)


# --- the legs: thick red thighs fused into the hips, short shins bent back, big red feet, black bands ------------------
def add_legs(k):
    knee, ankle = np.array((0.095, -0.03, 0.175)), np.array((0.1, 0.02, 0.075))
    k.add("thigh", ell((0.085, 0.0, 0.265), (0.078, 0.088, 0.11)), RED, "leg{s}", mirror=True)
    k.add("shin", tube([knee, ankle], [0.058, 0.045]), RED, "leg{s}", mirror=True)
    k.add("foot", ell((0.102, -0.035, 0.033), (0.055, 0.085, 0.034)), RED, "leg{s}", mirror=True)
    lt.add_bands(k, "legband", knee, ankle, (0.058, 0.045), (0.22, 0.78), 0.022, color=BLACK, bone="leg{s}",
                 thick=0.006)


# --- the arms: carried forward and down, the elbows bent out, three black bands, huge red hands with white claws ------
def add_arms(k):
    k.add("shoulder", ell(SH, (0.075, 0.08, 0.075)), RED, "arm{s}", mirror=True)
    k.add("upper", tube([SH, EL], [0.058, 0.052]), RED, "arm{s}", mirror=True)
    k.add("fore", tube([EL, WR], [0.052, 0.048]), RED, "arm{s}", mirror=True)
    k.add("hand", ell(HAND, (0.058, 0.062, 0.06), deform=lambda V: V / (np.abs(V) ** 2.3).sum(1, keepdims=True) ** (1 / 2.3)),
          RED, "arm{s}", mirror=True)
    lt.add_bands(k, "armband", SH, EL, (0.058, 0.052), (0.42, 0.85), 0.026, color=BLACK, bone="arm{s}", thick=0.006)
    lt.add_bands(k, "foreband", EL, WR, (0.052, 0.048), (0.55,), 0.026, color=BLACK, bone="arm{s}", thick=0.006)
    # four white claws hooking down out of the hand's front and a thumb's on its inner side
    for i, dx in enumerate((-0.036, -0.012, 0.012, 0.036)):
        b = HAND + np.array((dx, -0.055, -0.03))
        k.add(f"claw{i}", hull([b + (0.009, 0.012, 0.012), b + (-0.009, 0.012, 0.012), b + (0.009, -0.006, 0.01),
                                b + (-0.009, -0.006, 0.01), b + (0, -0.02, -0.03)]), CLAW, "arm{s}", fuse=False,
              mirror=True, surface="shell")
    b = HAND + np.array((-0.05, -0.035, 0.0))
    k.add("thumbclaw", hull([b + (0.012, 0.01, 0.01), b + (0.012, -0.01, 0.01), b + (0.012, 0.01, -0.01),
                             b + (0.012, -0.01, -0.01), b + (-0.022, -0.018, -0.022)]), CLAW, "arm{s}", fuse=False,
          mirror=True, surface="shell")


# --- the mane: black spikes behind the head and over the shoulders -----------------------------------------------------
def add_mane(k):
    k.add("manebase", ell((0, 0.04, 0.6), (0.15, 0.09, 0.09)), BLACK, "body")
    k.add("shouldermane", ell((0.12, 0.01, 0.6), (0.07, 0.08, 0.055)), BLACK, "body", mirror=True)
    spikes = []
    for i, a in enumerate(np.radians(np.linspace(-150, 150, 11))):
        o = np.array([math.sin(a), math.cos(a), 0])
        root = np.array([0, 0.0, 0.6]) + o * np.array([0.15, 0.12, 0])
        d = o * (1.0 if abs(a) > 1.2 else 0.7) + (0, 0, 0.55 + 0.25 * (i % 2))
        spikes.append((root, d, 0.075 + 0.02 * (i % 2)))
    for j, (x, z) in enumerate(((0.07, 0.72), (0.0, 0.73), (-0.07, 0.72), (0.05, 0.66), (-0.05, 0.66))):
        root = np.array([x, H[1] + HR[1] * 0.78, z])
        spikes.append((root, (x * 3, 1.0, 0.45), 0.1))
    for i, (root, d, ln) in enumerate(spikes):
        k.add(f"manespike{i}", lt.spike(root, d, ln, 0.03, 0.017), BLACK, "body" if root[2] < 0.65 else "head",
              fuse=False, surface="fur")


# --- the head: red, a broad black muzzle and jaw, red ears and cheek points swept back ---------------------------------
def add_head(k):
    k.add("head", ell(H, HR, deform=lambda V: V * (1 + 0.06 * np.clip(-V[:, 2], 0, 1))[:, None]), RED, "head")
    k.add("muzzle", ell(H + (0, -0.08, -0.045), (0.086, 0.066, 0.06)), BLACK, "head")
    k.add("jaw", ell(H + (0, -0.04, -0.09), (0.094, 0.072, 0.042)), BLACK, "head")
    # black from under the eyes back along the cheeks, a crisp line
    def mask(P):
        s, z = lt.round_head(P, H, HR)
        poly = lt.mirrored([(0.0, -0.03), (0.03, -0.018), (0.06, -0.013), (0.09, -0.002), (0.13, 0.004),
                            (0.15, -0.04), (0.12, -0.12), (0.0, -0.14)])
        return lt.poly_mask(s, z - H[2], poly, 0.002) * (P[:, 1] < H[1] + 0.05)
    k.paint(mask, BLACK, on=["head"])
    # the ears: red points swept back from the head's top corners
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        root = H + np.array([0.075, 0.02, 0.07]) * m
        k.add("ear" + ("R" if s > 0 else "L"), lt.spike(root, (0.35 * s, 0.85, 0.55), 0.095, 0.032, 0.018), RED,
              "head", fuse=False, surface="fur")
    # red points of fur swept back from the cheeks, over the black
    for i, (dz, ln) in enumerate(((0.0, 0.08), (-0.04, 0.07))):
        for s in (1, -1):
            root = H + np.array([0.1 * s, -0.02, dz])
            k.add(f"cheek{i}{'R' if s > 0 else 'L'}", lt.spike(root, (0.75 * s, 0.6, 0.2 - 0.25 * i), ln, 0.026, 0.014),
                  RED, "head", fuse=False, surface="fur")


def add_mouth(k):
    """A grin of white teeth across the black muzzle, notched between the teeth."""
    CM, CW = srgb(BLACK), srgb(WHITE)
    def grin(u, v):
        c = np.tile(CM, (len(u), 1))
        c[np.abs(v - 0.12 * u * u) < 0.5] = CW
        c[(np.abs(v - 0.12 * u * u) < 0.52) & (np.abs(np.abs(u) - 0.5) < 0.07)] = CM
        c[(np.abs(v - 0.12 * u * u) < 0.06)] = CM
        return c
    at = H + np.array([0, -0.142, -0.07])
    k.decal(at, unit(0, -1, -0.25), (0.058, 0.019), grin, eps=0.003, rings=10, segs=64,
            outline=polygon([(-1, 0.6), (1, 0.6), (1.0, -0.6), (-1, -0.6)]))


# --- the eyes: yellow with a green iris and a black pupil, cut along the top by the heavy red brow -------------------
OUTLINE = [(-1.0, -0.05), (-0.5, 0.18), (0.2, 0.42), (0.9, 0.58), (1.0, 0.2), (0.82, -0.42), (0.38, -0.72),
           (-0.2, -0.74), (-0.7, -0.5)]


def eye_colour():
    CY, CA, CG, CI = srgb(YELLOW), srgb(AMBER), srgb(IRIS), srgb(INK)
    rim = polygon(OUTLINE)
    def eye(u, v):
        t = np.clip((0.1 - v) / 0.9, 0, 1)[:, None]
        c = CY[None] * (1 - t) + CA[None] * t
        c[np.hypot((u + 0.05) / 0.34, (v + 0.08) / 0.48) < 1] = CG
        c[np.hypot((u + 0.05) / 0.17, (v + 0.08) / 0.3) < 1] = CI
        c[np.hypot((u - 0.32) / 0.13, (v + 0.1) / 0.15) < 1] = srgb(WHITE)
        rr = np.hypot(u, v) / np.array([rim(t_) for t_ in np.arctan2(v, u)])
        c[(rr > 0.6) & (v > 0.05 + 0.3 * u)] = CI
        c[rr > 0.82] = CI
        return c
    return eye


# --- the tail: red, out and back, black bands, a black spiky ruff near its red tip -------------------------------------
TAIL = [(0.0, 0.1, 0.34), (0.0, 0.17, 0.3), (0.0, 0.24, 0.3), (0.0, 0.3, 0.34), (0.0, 0.33, 0.4)]


def add_tail(k):
    pts = lt.tail_pts(TAIL)
    r = [0.04, 0.034, 0.03, 0.028, 0.026]
    k.add("tail", tube(pts, r), RED, "tail")
    for i, (a, b) in enumerate(((1, 2), (2, 3))):
        lt.add_bands(k, f"tailband{i}", pts[a], pts[b], (r[a], r[b]), (0.5,), 0.024, color=BLACK, bone="tail",
                     mirror=False, thick=0.006)
    # the ruff: black spikes all round, a little short of the tip
    end, d = pts[-1], unit(*(pts[-1] - pts[-2]))
    a, b = axes(d)
    c = pts[-2] + (pts[-1] - pts[-2]) * 0.45
    for i in range(7):
        ang = 2 * math.pi * i / 7
        out = unit(*(a * math.cos(ang) + b * math.sin(ang) + d * 0.3))
        k.add(f"ruff{i}", lt.spike(c + out * 0.02, out, 0.05, 0.02, 0.012), BLACK, "tail", fuse=False, surface="fur")


def incineroar(src, out_path):
    k = Kit(src, coat="fur")
    add_body(k)
    add_belt(k)
    add_legs(k)
    add_arms(k)
    add_mane(k)
    add_head(k)
    add_tail(k)
    add_mouth(k)
    k.build(voxel=0.0075, smooth=4, band=0.003, wband=0.06)
    look = eye_colour()
    k.eye_states(lambda s: lt.lay_eyes(k, s, H, HR, x=0.054, dz=0.006, half=(0.034, 0.026), lid=RED, line=INK,
                                       outline=OUTLINE, look=look, eps=0.0018))
    k.export(out_path)


if __name__ == "__main__":
    incineroar(*args())
