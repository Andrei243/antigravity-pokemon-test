# Veluza sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/veluza.py -- <the export's Veluza.glb> <out.glb>
#
# From the references: a long grey fish like a barracuda cut into fillets, its body cased in plates parted by dark
# seams: one round the body behind the head, one before the tail, chevrons of plates on the upper rear flank, and a
# thin violet line along each side. A long pointed snout over a lower jaw dropped wide open, pink inside and a dark
# throat, white triangular teeth standing up from the jaw and hanging from the snout. A round eye high behind the
# mouth: white with a black rim and a magenta pupil ringed in black. Sharp magenta fins: a tall blade up off the back
# and another down from the belly, crossing the body as one; two small blades behind each, a pectoral pair pointing
# back along the flanks, a small pair under the chest, and a forked tail. (The pictures show no pink flesh at its
# sides, only the fins.)
#
# The fish is built straight along -Y and then turned YAW degrees about the middle of its length, its head toward the
# model's left, so that the game's front camera sees its +X flank and the back one its -X flank broadside: straight,
# both looked at it nearly end-on and its tall midline blades edge-on. Both sides are therefore made by hand (add2,
# eye decals), never with mirror=True. Joints: body (0, 0, 0.306), finR/L (+-0.05, -0.1, 0.276), tail (0, 0.26, 0.306).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, tube, ell, hull, slab, srgb, inside, unit, eye_look, args

GREY, PINK, MOUTH, THROAT, WHITE, SEAM, LINE, MAGENTA, INK = ("#A9ABB4", "#E05CC2", "#E88C8E", "#8A3A54", "#F6F6F4",
                                                              "#30313A", "#7462CC", "#D8287E", "#141418")
Z0 = 0.306                    # the centre line
YAW = -18.0                   # degrees about Z: the head toward -X
PIVOT = np.array([0.0, 0.06, 0.0])
FLAT = 0.8                    # the body is a little narrower than deep

_a = math.radians(YAW)
ROT = np.array([[math.cos(_a), -math.sin(_a), 0], [math.sin(_a), math.cos(_a), 0], [0, 0, 1.0]])


def W(p):
    """A point (or points) of the fish's own frame in the model's."""
    p = np.asarray(p, float)
    return (p - PIVOT) @ ROT.T + PIVOT


def L(P):
    """The model's points in the fish's own frame (for paint masks)."""
    return (np.asarray(P, float) - PIVOT) @ ROT + PIVOT


def turned(shape):
    V, F = shape
    return W(V), F


def mirrored(shape):
    V, F = shape
    V = np.array(V, float) * np.array([-1, 1, 1.0])
    return V, [f[::-1] for f in F]


def add2(k, name, shape, color, bone, **kw):
    """Both sides by hand, built on +X in the fish's frame, then turned."""
    k.add(name + "R", turned(shape), color, bone.format(s="R"), **kw)
    k.add(name + "L", turned(mirrored(shape)), color, bone.format(s="L"), **kw)


def fish_tube(pts, radii, flat=FLAT):
    V, F = tube(pts, radii, seg=28)
    V = V.copy(); V[:, 0] *= flat
    return V, F


# --- the body ---------------------------------------------------------------------------------------------------------
UPPER = ([(0, -0.405, Z0 + 0.012), (0, -0.33, Z0 + 0.03), (0, -0.25, Z0 + 0.046), (0, -0.15, Z0 + 0.045)],
         [0.009, 0.037, 0.064, 0.088])
JAW = ([(0, -0.375, Z0 - 0.1), (0, -0.31, Z0 - 0.09), (0, -0.22, Z0 - 0.06), (0, -0.13, Z0 - 0.03)],
       [0.011, 0.026, 0.042, 0.06])


def body(k):
    k.add("head", turned(fish_tube(*UPPER)), GREY, "body")
    k.add("jaw", turned(fish_tube(*JAW, flat=0.86)), GREY, "body")
    k.add("throat", turned(ell((0, -0.17, Z0 - 0.022), (0.044, 0.06, 0.05))), THROAT, "body")
    k.add("trunk", turned(fish_tube([(0, -0.17, Z0 + 0.02), (0, -0.05, Z0 + 0.012), (0, 0.08, Z0 + 0.002),
                                     (0, 0.2, Z0)], [0.095, 0.1, 0.084, 0.054])), GREY, "body")
    k.add("stalk", turned(fish_tube([(0, 0.18, Z0), (0, 0.27, Z0), (0, 0.325, Z0)], [0.05, 0.03, 0.022])), GREY,
          "tail")


def centre(line, y):
    pts, radii = line
    ys = [p[1] for p in pts]
    return np.interp(y, ys, [p[2] for p in pts]), np.interp(y, ys, radii)


def markings(k):
    # inside the mouth: the upper jaw's underside and the lower jaw's top pink, the throat's mass dark
    def palate(P):
        P = L(P)
        zc, r = centre(UPPER, P[:, 1])
        return inside(P[:, 2] - (zc - 0.25 * r), 0.002) * inside(P[:, 1] + 0.14, 0.004) \
            * inside(np.abs(P[:, 0]) - 0.75 * r * FLAT, 0.003)
    k.paint(palate, MOUTH, on=["head"])

    def tongue(P):
        P = L(P)
        zc, r = centre(JAW, P[:, 1])
        return inside((zc + 0.2 * r) - P[:, 2], 0.002) * inside(P[:, 1] + 0.12, 0.004) \
            * inside(np.abs(P[:, 0]) - 0.78 * r * 0.86, 0.003)
    k.paint(tongue, MOUTH, on=["jaw"])

    # the seams: round the body behind the head, round it before the tail, and a lip line along the snout
    def seams(P):
        P = L(P)
        y, dz = P[:, 1], P[:, 2] - Z0
        m = inside(np.abs(y - (-0.1 + 0.18 * dz + 1.5 * dz * dz)) - 0.006, 0.0015) * inside(-0.095 - dz, 0.002)
        m = np.maximum(m, inside(np.abs(y - (0.205 + 0.25 * np.abs(dz))) - 0.0055, 0.0015))
        return m
    k.paint(seams, SEAM, on=["trunk", "stalk", "head"])

    # chevrons of plates on the upper rear flank, pointing back
    def chevrons(P):
        P = L(P)
        y, dz = P[:, 1], P[:, 2] - Z0
        m = np.zeros(len(P))
        for y0 in (0.03, 0.1, 0.165):
            t = y - (y0 + 0.9 * np.abs(dz - 0.035))
            m = np.maximum(m, inside(np.abs(t) - 0.0045, 0.0015) * inside(np.abs(dz - 0.035) - 0.03, 0.002))
        return m * (np.abs(P[:, 0]) > 0.02)
    k.paint(chevrons, SEAM, on=["trunk", "stalk"])

    # the violet line along each flank, from behind the eye to the tail
    def flank(P):
        P = L(P)
        y, dz = P[:, 1], P[:, 2] - Z0
        zl = np.interp(y, [-0.16, -0.06, 0.1, 0.3], [0.028, 0.006, -0.002, 0.0])
        return inside(np.abs(dz - zl) - 0.0045, 0.0015) * inside(-0.16 - y, 0.003) * inside(y - 0.3, 0.003) \
            * (np.abs(P[:, 0]) > 0.03)
    k.paint(flank, LINE, on=["trunk", "stalk", "head"])


# --- teeth: white kites on both jaws ----------------------------------------------------------------------------------
def tooth(root, down, length, width, thick):
    r = np.array(root, float); d = unit(*down)
    side = np.array([0, 1.0, 0]); n = np.array([1.0, 0, 0])
    pts = [r + side * width / 2, r - side * width / 2, r + n * thick / 2, r - n * thick / 2,
           r - d * 0.008 + side * width * 0.4, r - d * 0.008 - side * width * 0.4, r + d * length]
    return hull(pts)


def teeth(k):
    for i, y in enumerate((-0.36, -0.315, -0.265, -0.215)):          # hanging from the snout
        zc, r = centre(UPPER, y)
        x = 0.75 * r * FLAT
        add2(k, f"top{i}", tooth((x, y, zc - 0.55 * r), (0, 0, -1), 0.036 + 0.004 * i, 0.026, 0.018), WHITE,
             "body", fuse=False, surface="shell")
    for i, y in enumerate((-0.335, -0.285, -0.235)):                 # standing up from the jaw
        zc, r = centre(JAW, y)
        x = 0.72 * r * 0.86
        add2(k, f"low{i}", tooth((x, y, zc + 0.5 * r), (0, 0.15, 1), 0.034 + 0.004 * i, 0.026, 0.018), WHITE,
             "body", fuse=False, surface="shell")


# --- fins: solid magenta blades ---------------------------------------------------------------------------------------
def blade(base0, base1, tip, root_thick=0.022, tip_thick=0.004):
    """A blade from a base edge (two points on the midline) to a tip: a wedge, thick at its root."""
    b0, b1, t = (np.array(p, float) for p in (base0, base1, tip))
    rt, tt = np.array([root_thick / 2, 0, 0]), np.array([tip_thick / 2, 0, 0])
    mid = (b0 + b1) / 2
    return hull([b0 + rt, b0 - rt, b1 + rt, b1 - rt, mid + rt * 1.2, mid - rt * 1.2, t + tt, t - tt])


def side_blade(base0, base1, tip, out, thick=0.02):
    """A flank fin: a wedge whose thickness lies along out."""
    b0, b1, t = (np.array(p, float) for p in (base0, base1, tip))
    o = unit(*out) * thick / 2
    return hull([b0 + o, b0 - o, b1 + o, b1 - o, t + o * 0.2, t - o * 0.2])


def fins(k):
    m = dict(fuse=False)
    k.add("dorsal", turned(blade((0, -0.075, Z0 + 0.06), (0, 0.045, Z0 + 0.055), (0, 0.0, Z0 + 0.265))), PINK,
          "body", **m)
    k.add("ventral", turned(blade((0, -0.085, Z0 - 0.06), (0, 0.035, Z0 - 0.055), (0, -0.015, Z0 - 0.275))), PINK,
          "body", **m)
    k.add("back0", turned(blade((0, 0.085, Z0 + 0.045), (0, 0.15, Z0 + 0.035), (0, 0.175, Z0 + 0.14), 0.018)), PINK,
          "tail", **m)
    k.add("back1", turned(blade((0, 0.16, Z0 + 0.03), (0, 0.215, Z0 + 0.022), (0, 0.245, Z0 + 0.105), 0.016)), PINK,
          "tail", **m)
    k.add("under0", turned(blade((0, 0.07, Z0 - 0.045), (0, 0.13, Z0 - 0.037), (0, 0.155, Z0 - 0.145), 0.018)), PINK,
          "tail", **m)
    k.add("under1", turned(blade((0, 0.15, Z0 - 0.03), (0, 0.205, Z0 - 0.024), (0, 0.235, Z0 - 0.112), 0.016)), PINK,
          "tail", **m)
    # the pectorals along the flanks, their point back and a little up; a small pair under the chest
    add2(k, "pec", side_blade((0.058, -0.105, Z0 - 0.0), (0.055, -0.08, Z0 - 0.062), (0.1, 0.05, Z0 + 0.01),
                              (1, 0, 0.1)), PINK, "fin{s}", **m)
    add2(k, "chest", side_blade((0.03, -0.13, Z0 - 0.072), (0.03, -0.085, Z0 - 0.08), (0.07, -0.075, Z0 - 0.19),
                                (1, -0.1, 0)), PINK, "body", **m)
    # the forked tail
    tail = [(0.0, 0.026), (0.125, 0.13), (0.075, 0.004), (0.108, -0.105), (0.0, -0.026)]
    k.add("tailfin", turned(slab(tail, (0, 0.305, Z0), (0, 1, 0), (0, 0, 1), 0.02)), PINK, "tail", **m)


# --- the eyes: white, a black rim, a magenta pupil ringed in black ----------------------------------------------------
CW, CK, CM, CG = srgb(WHITE), srgb(INK), srgb(MAGENTA), srgb(GREY)


def eye_open(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CW, (len(u), 1))
    c[r > 0.85] = CK
    pr = np.sqrt((u + 0.08) ** 2 + v * v)            # the pupil a little toward the snout
    c[pr < 0.58] = CK
    c[pr < 0.4] = CM
    return c


EYE_AT = np.array([0.043, -0.205, Z0 + 0.062])
EYE_N = unit(0.72, -0.5, 0.42)
EYE_HALF = (0.03, 0.03)


def eyes(k, state):
    look = eye_look(state, eye_open, CG, CK, line=0.16)
    for s in (1, -1):
        c = W(EYE_AT * np.array([s, 1, 1]))
        n = ROT @ (EYE_N * np.array([s, 1, 1]))
        k.decal(c, n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0022, rings=20, segs=56)


def veluza(src, out):
    k = Kit(src, coat="metal")
    body(k)
    markings(k)
    teeth(k)
    fins(k)
    k.build(voxel=0.0042, smooth=4, band=0.004, wband=0.035)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    veluza(*args())
