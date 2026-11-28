# Nufarel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nufarel.py -- <the export's Nufarel.glb> <out.glb>
#
# Carpathia's water lily bud (No. 10156, Grass/Water), from its picture
# (docs/art/reference/gemini/carpathia/10156-Nufarel.jpg) and its brief (CP-dex-3b-river-and-caves-more.md, 156): a
# closed, pointed water lily bud of pale pink petals, overlapping in two layers to a point, cupped low by five broad
# green sepals; two small dark eyes peeping from the gaps either side of the front sepal; it sits on a round green
# lily pad (its raft, part of the creature) with a notch cut into it, and a thin pale root trails behind it on the pad,
# curling up at its end.
# Written to be grown from: nufarina.py imports it (sibling("nufarel")) for the petals, the pad, the bud and the eye.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, outward, srgb, inside, smoothstep, unit, eye_look, args

# The line's palette
PETAL, PETAL2, PETALTIP = "#F3D0D8", "#E8B2C2", "#D998AE"
SEPAL, SEPALDK = "#8FB866", "#6F9C4E"
PAD, PADDK, ROOT = "#5F9C4A", "#4C8640", "#CBBB8E"
INK = "#1A1214"
WHITEC, INKC = np.array([1.0, 1.0, 1.0]), srgb(INK)


def board(k):
    """The game's board check on the built body: the span it shows at the board's turn against its square."""
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); four corners %.3f" % (np.ptp(proj), sq, np.ptp(proj) / sq,
                                                                                  0.853 * w + 0.522 * l))


def around(a, r=1.0, z=0.0):
    """A point at angle a (degrees from the front, -Y, toward +X) round the upright axis."""
    t = math.radians(a)
    return np.array([r * math.sin(t), -r * math.cos(t), z])


def petal(centre, out, up, length, width, thick, curve=0.55, point=1.6, bend=0.0):
    """
    A thick pointed petal (or sepal, or leaf): its face toward `out`, its length along `up` (made square to out), its
    edges curved back by `curve` (a share of its half width) so that it hugs what it grows round, its tip bent in
    toward -out by `bend` (a share of its length); its tip pointed, its root rounded.
    """
    out = unit(*out)
    up = np.array(up, float); up = up - out * (up @ out); up /= np.linalg.norm(up)
    side = np.cross(up, out)                       # right-handed: side, out, up
    def shape(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        w = np.sqrt(np.clip(t * 1.15, 0, 1)) * (1 - np.clip(t, 0, 1) ** point) * 1.45 + 0.04
        V[:, 0] *= w
        return V
    V, F = ell((0, 0, 0), (width, thick, length / 2), deform=shape, sub=3)
    t = (V[:, 2] / (length / 2) + 1) / 2
    V[:, 1] -= curve * width * 0.5 * (V[:, 0] / width) ** 2 + bend * length * t ** 2
    M = np.stack([side, out, up], axis=1)
    return outward((V @ M.T + np.array(centre, float), F))


def lily_pad(k, c, r, notch, bone, thick, name="pad", colour=PAD, dark=PADDK):
    """A round flat pad lying on the ground, a wedge cut in at angle `notch`, a darker heart where its stem meets it."""
    pts = [tuple(around(notch, r * 0.08)[:2])]          # the notch's inner point, then round from one lip to the other
    for i in range(65):
        a = notch + 10 + 340 * i / 64
        rr = r * (1 + 0.015 * math.sin(math.radians(a) * 5))
        pts.append(tuple(around(a, rr)[:2]))
    k.add(name, slab(pts, (c[0], c[1], c[2] + thick / 2), (1, 0, 0), (0, 1, 0), thick), colour, bone, fuse=False,
          surface="leaf")
    # veins: a few thin dark spokes from the heart, laid on the pad's top
    for j, a in enumerate(range(0, 360, 45)):
        if abs((a - notch + 180) % 360 - 180) < 25:
            continue
        p0, p1 = around(a, r * 0.12), around(a, r * 0.85)
        dirv = unit(*(p1 - p0)); sidev = np.cross((0, 0, 1.0), dirv) * 0.0035
        V = [p0[:2] + sidev[:2] * 1.4, p1[:2] + sidev[:2] * 0.5, p1[:2] - sidev[:2] * 0.5, p0[:2] - sidev[:2] * 1.4]
        k.add(f"{name}vein{j}", slab([tuple(v) for v in V], (c[0], c[1], c[2] + thick + 0.0012), (1, 0, 0), (0, 1, 0),
                                     0.0018), dark, bone, fuse=False, surface="leaf")


class Cfg:
    """Nufarel's numbers; nufarina.py copies and changes them for the bud on her head."""
    g = 1.0
    C = np.array([0.0, 0.0, 0.142])              # the bud's core
    R = np.array([0.112, 0.112, 0.126])
    EYE_A, EYE_Z, EYE = 31.0, 0.096, (0.019, 0.021)
    FRONT_LEN, FRONT_W, FRONT_Z = 0.15, 0.04, -0.045   # the front sepal: its length, width and root under the core
    PAD_R, ROOT_G = 0.178, 1.0
    PETAL_A_Z = 0.03                             # the outer petals' middle over the core's


class Own(Cfg):
    """What Nufarel builds itself with (the game's cell): a taller bud on a smaller pad, a short front sepal under
    both eyes. Cfg keeps the numbers nufarina.py grows from."""
    C = np.array([0.0, 0.0, 0.156])
    R = np.array([0.112, 0.112, 0.14])
    EYE_A, EYE_Z, EYE = 28.0, 0.112, (0.019, 0.021)
    FRONT_LEN, FRONT_W, FRONT_Z = 0.075, 0.03, -0.12
    PAD_R, ROOT_G = 0.14, 0.8
    PETAL_A_Z = 0.09


def pointed_top(V):
    """The bud's core: round below, drawn up to a soft point above."""
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.55 * t ** 1.6
    V[:, 1] *= 1 - 0.55 * t ** 1.6
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 1.25, V[:, 2])
    return V


def core_radius(C, z):
    """The core's radius round its axis at height z (before its point)."""
    t = np.clip((z - C.C[2]) / C.R[2], -1, 1)
    return C.R[0] * np.sqrt(1 - t * t)


def bud(k, C, bone, sepals=True, eyegap=True):
    """The bud: a fused pink core, two layers of loose petals closing to a point over it, five green sepals below."""
    g = C.g
    c = C.C
    k.add("budcore", ell(c, C.R, deform=pointed_top), PETAL, bone, surface="leaf")
    top = c[2] + C.R[2] * 1.25
    # the outer layer, behind the sepals, rising from low on the bud
    for j, a in enumerate((0, 72, 144, 216, 288)):
        o = around(a)
        z = c[2] + C.PETAL_A_Z * g
        p = np.array([c[0], c[1], 0]) + o * (core_radius(C, z) + 0.004 * g) + np.array([0, 0, z])
        up = unit(*(np.array([c[0], c[1], top - 0.015 * g]) - p))
        k.add(f"petalA{j}", petal(p, o, up, 0.23 * g, 0.08 * g, 0.014 * g, curve=0.85, bend=0.13), PETAL if j % 2 else PETAL2,
              bone, fuse=False, surface="leaf")
    # the inner layer between them, higher, so that the eyes peep out under it
    for j, a in enumerate((36, 108, 180, 252, 324)):
        o = around(a)
        z = c[2] + 0.07 * g
        p = np.array([c[0], c[1], 0]) + o * (core_radius(C, z) + 0.009 * g) + np.array([0, 0, z])
        up = unit(*(np.array([c[0], c[1], top - 0.005 * g]) - p))
        k.add(f"petalB{j}", petal(p, o, up, 0.18 * g, 0.07 * g, 0.014 * g, curve=0.85, bend=0.11), PETAL2 if j % 2 else PETAL, bone,
              fuse=False, surface="leaf")
    # the tip: the innermost petals' point, a little deeper
    k.add("budtip", petal(np.array([c[0], c[1] - 0.008 * g, top - 0.03 * g]), (0, -1, 0.3), (0, 0, 1), 0.07 * g,
                          0.024 * g, 0.012 * g, curve=0.3), PETALTIP, bone, fuse=False, surface="leaf")
    if not sepals:
        return
    # five broad green sepals cupping the bud's foot, the front one between the eyes
    for j, a in enumerate((0, 72, 144, 216, 288)):
        o = around(a)
        z = c[2] + (C.FRONT_Z if a == 0 else -0.045) * g
        p = np.array([c[0], c[1], 0]) + o * (core_radius(C, z) + 0.016 * g) + np.array([0, 0, z])
        up = unit(*(o * 0.32 + np.array([0, 0, 1.0])))
        w = C.FRONT_W if a == 0 else 0.064
        k.add(f"sepal{j}", petal(p, o, up, (C.FRONT_LEN if a == 0 else 0.17) * g, w * g, 0.016 * g, curve=0.7),
              SEPAL if j % 2 == 0 else SEPALDK, bone, fuse=False, surface="leaf")


def eye_open():
    def look(u, v):
        c = np.tile(INKC, (len(u), 1))
        c[(u + 0.28) ** 2 + (v - 0.34) ** 2 < 0.3 ** 2] = WHITEC
        c[(u - 0.3) ** 2 + (v + 0.38) ** 2 < 0.12 ** 2] = WHITEC * 0.85
        return c
    return look


def eyes(k, C, lid=PETAL):
    L = srgb(lid)
    def lay(state):
        for s in (1, -1):
            a = C.EYE_A * s
            o = around(a)
            p = np.array([C.C[0], C.C[1], 0]) + o * core_radius(C, C.EYE_Z) + np.array([0, 0, C.EYE_Z])
            base = eye_look(state, eye_open(), L, INKC, line=0.18)
            # a sideways patch laid by hand: +u toward the back on +X; turned round on -X so +u is outward on both
            look = base if s > 0 else (lambda u, v, f=base: f(-u, v))
            k.decal(p, o + np.array([0, 0, 0.05]), C.EYE, look, eps=0.0018, rings=20, segs=56)
    return lay


def root(k, bone, g=1.0):
    """A thin pale root trailing behind on the pad toward +X, curling up at its end inside the pad's rim."""
    pts = np.array([(0.05, 0.06, 0.03), (0.09, 0.1, 0.024), (0.13, 0.11, 0.025), (0.155, 0.085, 0.035),
                    (0.15, 0.06, 0.05), (0.135, 0.06, 0.06)]) * np.array([g, g, g])
    k.add("root", outward(tube(pts, [0.0075, 0.007, 0.0065, 0.006, 0.0055, 0.005])), ROOT, bone, fuse=False,
          surface="leaf")


def nufarel(src, out_path, C=Own):
    k = Kit(src, coat="leaf")
    lily_pad(k, (0.0, 0.0, 0.0), C.PAD_R, 315, "body", 0.018)
    bud(k, C, "body")
    root(k, "body", C.ROOT_G)
    k.build(voxel=0.0034, band=0.004, wband=0.03)
    board(k)
    k.eye_states(eyes(k, C))
    k.export(out_path)


if __name__ == "__main__":
    nufarel(*args())
