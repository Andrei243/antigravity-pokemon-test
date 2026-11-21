# Koraidon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/koraidon.py -- <the export's Koraidon.glb> <out.glb>
#
# From the references (the Apex Build, as the game's own sculptor stands it): a great red lizard dragon in a wide,
# crouched stance, its trunk leaning forward. A black tyre bulges over the front of its chest from under the throat to
# the belly, its tread in segments, the red round it cut into white teeth along its edges; the belly under it white
# between the thighs. A lizard's head carried forward on a short thick neck, a gold eye with a black slit under swept
# navy feathers, a crown of white feathers rosy at their roots fanned up and back behind it, and a long braided white
# plume arching up and trailing down behind. Blue feathers hang over the back of the neck and the shoulders; white
# squares on the upper arms; muscular red arms forward and down to great three-clawed hands, a blue tuft at the elbow.
# Fans of white feathers, blue-violet at their roots, spread back from the hips. Great red haunches fused into the hips
# over short thick shins and long clawed feet. A thick red tail out back and to the side, a white saw along its top
# and black beneath.
# Bones: body (0, 0, 0.5); arms (+-0.1, -0.08, 0.74); head (0, -0.1, 0.8); plume (0, -0.17, 1.03); tail (0, 0.08, 0.48);
# legs (+-0.1, 0.03, 0.42).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, polygon, eye_look, boxy, tri, args

RED = "#E2402E"
REDD = "#B8302A"
TYRE = "#2C2A31"
TREAD = "#4A4752"
WHITE = "#F2F0EC"
CREAM = "#E8E2DA"
NAVY = "#2C46B8"
ROSE = "#D8409C"
VIOLET = "#6E52C8"
GOLD = "#F4C22E"
INK = "#151318"

H = np.array([0.0, -0.22, 0.96])         # the skull's middle
HR = np.array([0.092, 0.12, 0.083])
SNOUT = np.array([0.0, -0.335, 0.93])
TYRE_C, TYRE_R = np.array([0.0, -0.115, 0.625]), np.array([0.13, 0.1, 0.18])
LEAN, DROP = 0.2, 0.04                     # the upper body leant forward and lowered by a shear above the hips


def lean(P):
    """The pose's forward lean: above z 0.5 each point goes forward by LEAN and down by DROP per unit of height."""
    P = np.array(P, float).copy()
    t = np.maximum(P[..., 2] - 0.5, 0)
    P[..., 1] -= LEAN * t
    P[..., 2] -= DROP * t
    return P


def unlean(P):
    """Back from the leant pose to the upright one, so that paint masks are written in upright coordinates."""
    P = np.array(P, float).copy()
    t = np.maximum(P[..., 2] - 0.5, 0) / (1 - DROP)
    P[..., 2] += DROP * t
    P[..., 1] += LEAN * t
    return P


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def kite(base, d, length, width, thick, up=(0, 0, 1), sink=0.015):
    """A solid point: a kite in section, wide across up, thick across the other way (a claw, a spike, a tooth)."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * sink
    m = base + d * length * 0.45
    return hull([b + up * width / 2, b - up * width / 2, b + side * thick / 2, b - side * thick / 2,
                 m + up * width * 0.34, m - up * width * 0.34, m + side * thick * 0.34, m - side * thick * 0.34,
                 base + d * length])


def feather(root, d, length, width, thick, nrm):
    """A broad feather: flat across nrm, widest past its middle, pointed (a hull)."""
    root, d = np.array(root, float), unit(*d)
    n = np.array(nrm, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    w = np.cross(n, d)
    P = []
    for t, ww, tt in ((0.0, 0.32, 0.5), (0.45, 0.5, 0.42), (0.78, 0.34, 0.3)):
        c = root + d * length * t
        P += [c + w * width * ww, c - w * width * ww, c + n * thick * tt, c - n * thick * tt]
    P.append(root + d * length)
    return hull(P)


# --- the body ---------------------------------------------------------------------------------------------------------
def body(k):
    k.add("belly", ell((0, 0.0, 0.49), (0.135, 0.12, 0.125)), RED, "body")
    k.add("chest", ell((0, -0.03, 0.635), (0.145, 0.115, 0.15), rot=(-14, 0, 0)), RED, "body")
    k.add("tyre", ell(TYRE_C, TYRE_R, rot=(-14, 0, 0)), TYRE, "body")
    k.add("neck", tube([(0, -0.06, 0.72), (0, -0.12, 0.83), (0, -0.18, 0.91)], [0.1, 0.085, 0.072]), RED, "head")
    # the tyre's tread: dark seams across its front
    def tread(P):
        q = P - TYRE_C
        return sum(inside(np.abs(q[:, 2] - z) - 0.006, 0.0025) for z in (-0.12, -0.065, -0.01, 0.045, 0.1)) \
            * inside(P[:, 1] + 0.1, 0.004)
    k.paint(tread, TREAD, on=["tyre"])
    # white teeth along the tyre's edge, cut into the red round it
    def teeth(P):
        m = np.array(Euler([math.radians(-14), 0, 0]).to_matrix())      # the tyre's own turn, undone: V = V' m
        q = ((P - TYRE_C) @ m) / TYRE_R
        r = np.linalg.norm(q, axis=1)
        tooth = 0.06 + 0.3 * (0.5 + 0.5 * tri((P[:, 2] - 0.4) / 0.065))
        return inside(r - 1 - tooth, 0.004) * inside(0.99 - r, 0.004) * inside(np.abs(q[:, 2]) - 0.85, 0.01) \
            * inside(0.3 - np.abs(q[:, 0]), 0.02) * inside(P[:, 1] - 0.0, 0.004)
    k.paint(teeth, WHITE, on=["chest", "belly"])
    # the belly white between the thighs, under the tyre
    k.paint(lambda P: inside(P[:, 2] - 0.46, 0.006) * inside(P[:, 1] + 0.035, 0.006) * inside(np.abs(P[:, 0]) - 0.1, 0.006)
            * inside(0.33 - P[:, 2], 0.006), CREAM, on=["belly"])


def shoulders(k):
    # blue feathers hanging over the back of the neck and the shoulders
    for j, (x, y, z, d, L) in enumerate(((0.0, -0.08, 0.95, (0, 1, -0.55), 0.15), (0.05, -0.05, 0.9, (0.45, 1, -0.7), 0.15),
                                         (-0.05, -0.05, 0.9, (-0.45, 1, -0.7), 0.15), (0.0, -0.02, 0.84, (0, 1, -0.9), 0.13))):
        k.add(f"mane{j}", feather((x, y, z), d, L, 0.065, 0.032, (0, -0.4, 1)), NAVY, "head", fuse=False, surface="fur")
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        for j, (b, d, L) in enumerate((((0.13, -0.0, 0.79), (0.55, 0.55, -0.6), 0.13),
                                       ((0.1, 0.03, 0.8), (0.3, 0.8, -0.5), 0.12))):
            k.add(f"shmane{sd}{j}", feather(np.array(b) * m, np.array(d) * m, L, 0.06, 0.03, np.array((0.3, 0, 1)) * m),
                  NAVY, f"arm{sd}", fuse=False, surface="fur")


def arms(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"arm{sd}"
        k.add(f"shoulder{sd}", ell(np.array((0.145, -0.06, 0.73)) * m, (0.072, 0.075, 0.07)), RED, bone)
        k.add(f"arm{sd}", tube(np.array([(0.15, -0.06, 0.72), (0.19, -0.09, 0.62), (0.17, -0.17, 0.555),
                                         (0.132, -0.24, 0.525)]) * m, [0.056, 0.052, 0.046, 0.042]), RED, bone)
        k.add(f"hand{sd}", ell(np.array((0.128, -0.26, 0.515)) * m, (0.05, 0.05, 0.042)), RED, bone)
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            c = np.array((0.128 + dx, -0.295, 0.51 - abs(dx) * 0.3))
            k.add(f"finger{sd}{j}", tube(np.array([c, c + (dx * 0.6, -0.035, -0.03)]) * m, [0.02, 0.017]), RED, bone)
            k.add(f"claw{sd}{j}", kite((c + (dx * 0.8, -0.04, -0.04)) * m, np.array((dx * 4, -0.55, -1)) * m, 0.045, 0.026,
                                       0.02, up=(0, 1, 0)), WHITE, bone, fuse=False, surface="shell")
        # a blue-violet tuft streaming back from the elbow
        for j, (d, L, col) in enumerate((((0.5, 1, 0.05), 0.17, NAVY), ((0.6, 0.9, -0.35), 0.15, VIOLET))):
            k.add(f"elbow{sd}{j}", feather(np.array((0.19, -0.08, 0.62)) * m, np.array(d) * m, L, 0.06, 0.03,
                                           np.array((0, 0, 1.0))), col, bone, fuse=False, surface="fur")
        # white squares on the upper arm's outside
    DIAMOND = polygon([(0, -1), (1, 0), (0, 1), (-1, 0)])
    for c, h in (((0.214, -0.078, 0.69), 0.024), ((0.232, -0.088, 0.637), 0.02)):
        k.decal(c, unit(1, -0.15, 0.25), (h, h), WHITE, eps=0.003, outline=DIAMOND, rings=6, segs=48, mirror=True)


def hip_fans(k):
    """Fans of white feathers spread back from the hips, blue-violet at their roots, turned out from straight back."""
    for s, sd in ((1, "R"), (-1, "L")):
        B = unit(0.85 * s, 0.5, 0)
        Z = np.array([0, 0, 1.0])
        n = np.cross(B, Z) * s
        root = np.array((0.12 * s, 0.05, 0.52))
        for j, (a, L) in enumerate(((60, 0.27), (36, 0.33), (12, 0.36), (-14, 0.33), (-38, 0.26), (-60, 0.2))):
            d = math.cos(math.radians(a)) * B + math.sin(math.radians(a)) * Z
            r0 = root + d * 0.02 + Z * 0.01 * (2 - j)
            k.add(f"fan{sd}{j}", feather(r0, d, L, 0.095, 0.032, n), WHITE, "body", fuse=False, surface="fur")
            k.add(f"fanroot{sd}{j}", feather(r0 - d * 0.01, d, L * 0.3, 0.09, 0.038, n), VIOLET, "body",
                  fuse=False, surface="fur")


def legs(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"leg{sd}"
        k.add(f"thigh{sd}", ell(np.array((0.15, -0.015, 0.33)) * m, (0.105, 0.12, 0.13), rot=(-18, 0, -14 * s)), RED, bone)
        k.add(f"shin{sd}", tube(np.array([(0.2, -0.05, 0.24), (0.215, -0.03, 0.14), (0.22, -0.01, 0.075)]) * m,
                                [0.066, 0.056, 0.05]), RED, bone)
        k.add(f"foot{sd}", ell(np.array((0.22, -0.055, 0.034)) * m, (0.065, 0.105, 0.034)), RED, bone)
        for j, dx in enumerate((-0.035, 0.0, 0.035)):
            c = np.array((0.22 + dx, -0.15, 0.03))
            k.add(f"toe{sd}{j}", tube(np.array([c + (0, 0.04, 0.004), c]) * m, [0.024, 0.02]), RED, bone)
            k.add(f"toeclaw{sd}{j}", kite((c + (dx * 0.3, -0.012, -0.004)) * m, np.array((dx * 3, -1, -0.35)) * m,
                                          0.045, 0.028, 0.02), WHITE, bone, fuse=False, surface="shell")
        k.add(f"heel{sd}", kite(np.array((0.22, 0.045, 0.03)) * m, (0, 1, -0.3), 0.04, 0.026, 0.02), WHITE, bone,
              fuse=False, surface="shell")


TAIL = np.array([(0, 0.07, 0.48), (0.0, 0.2, 0.37), (0.05, 0.34, 0.24), (0.12, 0.47, 0.15), (0.2, 0.57, 0.1),
                 (0.27, 0.64, 0.085)])
TAIL_R = [0.1, 0.085, 0.068, 0.05, 0.034, 0.02]


def tail(k):
    k.add("tail", tube(TAIL, TAIL_R), RED, "tail")
    # black beneath: under the line through the tail's middle
    def under(P):
        zc = np.interp(P[:, 1], TAIL[:, 1], TAIL[:, 2])
        return inside(P[:, 2] - zc + 0.006, 0.004) * inside(0.12 - P[:, 1], 0.01)
    k.paint(under, TYRE, on=["tail"])
    # a white saw along its top
    dense = np.vstack([TAIL[i] + (TAIL[i + 1] - TAIL[i]) * t for i in range(len(TAIL) - 1) for t in np.linspace(0, 1, 6)])
    rad = np.interp(np.linspace(0, len(TAIL) - 1, len(dense)), np.arange(len(TAIL)), TAIL_R)
    for j, i in enumerate(range(5, len(dense) - 2, 3)):
        p, d = dense[i], unit(*(dense[i + 1] - dense[i - 1]))
        up = np.array([0, 0, 1.0]) - d * d[2]; up /= np.linalg.norm(up)
        L = 0.03 + 0.045 * rad[i] / 0.1
        k.add(f"saw{j}", kite(p + up * rad[i] * 0.75, unit(*(up * 0.85 + d * 0.55)), L, 0.05 * (0.4 + rad[i] / 0.1 * 0.6),
                              0.024, up=d, sink=0.02), WHITE, "tail", fuse=False, surface="shell")


# --- the head ---------------------------------------------------------------------------------------------------------
def head(k):
    k.add("skull", ell(H, HR, rot=(-8, 0, 0)), RED, "head")
    k.add("snout", ell(SNOUT, (0.063, 0.092, 0.055), rot=(-6, 0, 0), deform=boxy(2.5)), RED, "head")
    k.add("jaw", ell(SNOUT + (0, 0.025, -0.046), (0.057, 0.085, 0.03), rot=(-10, 0, 0)), RED, "head")
    k.add("brow", ell(H + (0, -0.046, 0.052), (0.08, 0.08, 0.034), rot=(-14, 0, 0)), RED, "head")
    # navy feathers swept back over and behind the eyes
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        for j, (b, d, L) in enumerate((((0.055, -0.235, 1.015), (0.45, 1, 0.15), 0.15),
                                       ((0.035, -0.2, 1.035), (0.3, 1, 0.45), 0.15),
                                       ((0.07, -0.17, 0.985), (0.6, 1, -0.2), 0.13))):
            k.add(f"crest{'RL'[s < 0]}{j}", feather(np.array(b) * m, np.array(d) * m, L, 0.05, 0.03,
                                                       np.array((0.5, 0, 1)) * m), NAVY, "head", fuse=False, surface="fur")
    # the crown: white feathers rosy at their roots, fanned up and back behind the head
    crown = np.array((0.0, -0.15, 1.02))
    back = np.array((0.0, 1.0, 0.0))
    up = np.array((0.0, 0.0, 1.0))
    for j, (x, a, L) in enumerate(((0.0, 58, 0.24), (0.5, 46, 0.23), (-0.5, 46, 0.23), (0.85, 28, 0.21), (-0.85, 28, 0.21),
                                   (0.3, 16, 0.2), (-0.3, 16, 0.2))):
        d = unit(*(math.cos(math.radians(a)) * back + math.sin(math.radians(a)) * up + np.array((x * 0.55, 0, 0))))
        n = unit(*np.cross(d, (1, 0, 0))) if abs(x) < 0.6 else unit(*np.cross(d, (0, 1, 0)))
        r0 = crown + np.array((x * 0.04, 0.01 * abs(x), 0))
        k.add(f"crown{j}", feather(r0, d, L, 0.07, 0.03, n), WHITE, "head", fuse=False, surface="fur")
        k.add(f"crownroot{j}", feather(r0 - d * 0.01, d, L * 0.34, 0.065, 0.036, n), ROSE, "head", fuse=False,
              surface="fur")
    # the long braided plume: up from the crown, arching back and trailing down behind
    pp = np.array([(0, -0.17, 1.02), (0.02, -0.13, 1.11), (0.06, -0.03, 1.17), (0.11, 0.1, 1.17), (0.16, 0.23, 1.1),
                   (0.2, 0.33, 1.0), (0.22, 0.38, 0.88)])
    k.add("braid", tube(pp, [0.027, 0.026, 0.025, 0.024, 0.023, 0.022, 0.021]), WHITE, "plume")
    dense = np.vstack([pp[i] + (pp[i + 1] - pp[i]) * t for i in range(len(pp) - 1) for t in np.linspace(0, 1, 30)])
    seg = np.r_[0, np.cumsum(np.linalg.norm(np.diff(dense, axis=0), axis=1))]
    def braid(P):
        i = np.argmin(((P[:, None, :] - dense[None]) ** 2).sum(2), axis=1)
        return ((seg[i] / 0.04) % 1 < 0.38).astype(float)
    k.paint(braid, "#C4C0CA", on=["braid"])
    # a dark line for the mouth along each side of the snout
    RECT = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])
    k.decal(SNOUT + (0.058, 0.025, -0.026), unit(1, -0.15, -0.25), (0.055, 0.0055), INK, up=(0, 0, 1), eps=0.003,
            outline=RECT, rings=3, segs=32, mirror=True)
    # nostrils
    k.decal(SNOUT + (0.027, -0.085, 0.026), unit(0.4, -1, 0.5), (0.008, 0.006), INK, eps=0.003, rings=4, segs=24,
            mirror=True)


# --- the eyes: gold, a black slit, under the brow ----------------------------------------------------------------------
EYE_OUT = polygon([(-1, -0.25), (-0.45, -0.75), (0.5, -0.7), (1.05, -0.1), (0.8, 0.45), (0.1, 0.62), (-0.7, 0.4)])
CG, CK, CW, LID = srgb(GOLD), srgb(INK), srgb("#FFFFFF"), srgb(RED)


def eye_open(u, v):
    c = np.tile(CG, (len(u), 1))
    rim = (u / 1.0) ** 2 + (v / 0.62) ** 2
    c[rim > 0.72] = CK
    c[np.abs(u + 0.02) < 0.15] = CK
    c[(u - 0.35) ** 2 + (v - 0.22) ** 2 < 0.02] = CW
    return c


DE = unit(0.74, -0.56, 0.25)


def eyes(k, state):
    E = lean(on_ell(H, HR, DE) + np.array([0, -0.01, -0.01]))
    look = eye_look(state, eye_open, LID, CK, line=0.2)
    m = np.array([-1, 1, 1.0])
    k.decal(E, DE, (0.038, 0.027), look, eps=0.002, rings=20, segs=56, outline=EYE_OUT)
    k.decal(E * m, DE * m, (0.038, 0.027), lambda u, v: look(-u, v), eps=0.002, rings=20, segs=56,
            outline=lambda th: EYE_OUT(math.pi - th))


def koraidon(src, out_path):
    k = Kit(src, coat="scales")
    body(k)
    shoulders(k)
    arms(k)
    hip_fans(k)
    legs(k)
    tail(k)
    head(k)
    # the pose's lean: every part, patch and paint above the hips sheared forward (masks keep upright coordinates)
    for p in k.parts:
        p["V"] = lean(p["V"])
    for d in k.decals:
        d["c"] = lean(d["c"])
    k.paints = [(lambda P, f=mask: f(unlean(P)), col, on) for mask, col, on in k.paints]
    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out_path)


if __name__ == "__main__":
    koraidon(*args())
