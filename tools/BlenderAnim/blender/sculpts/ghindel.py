# Ghindel (No. 10096, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/ghindel.py -- <the export's Ghindel.glb> <out.glb>
#
# From its picture (10096-Ghindel.jpg) and its brief (CP-dex-2b-mountains-more.md, 96): a tiny glossy acorn. Largest
# first: the nut, golden ochre, round above and drawn to a blunt point below, some 0.55 of the height and as wide;
# the cap, a rough lumpy brown dome some 0.64 of the height across, tipped down over the -X eye (the picture shows one
# eye only, the other under the cap: here the -X eye's top is cut by the brim and the +X eye shows whole to the game's
# front camera); two green leaves in a V on a short stem out of the cap's top; two stubby arm nubs at the nut's sides,
# each with a little pair of tan leaf buds. A round shy face: dot eyes with a highlight, a small "o" of a mouth, a
# faint blush, a pale gloss streak down the nut.
#
# Written to be grown from (stejarel.py and stejaron.py import it with sibling("ghindel")): the line's palette, the
# rough acorn cap, a flat leaf or oak leaf of any outline, a small acorn, and the dot eye are functions here.
# Skeleton: body (0, 0, 0.168), cap (0, 0, 0.248), arms (+-0.09, 0, 0.128).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, slab, outward, srgb, inside, smoothstep, unit, eye_look, args

# --- the line's colours -----------------------------------------------------------------------------------------
NUT, NUT_D, NUT_HI = "#D49A46", "#B37A34", "#F3D08E"
CAPC, CAP_D, CAP_HI = "#7E5C40", "#5C4130", "#9C7856"
LEAF, LEAF_D, STEM = "#5EA83E", "#3F8A2E", "#6E9A3C"
BUD = "#B08450"
INK, IRIS, WHITE = "#22160F", "#3B2618", "#FBF6EE"
BLUSH, MOUTH = "#E58C74", "#8E3A30"

# --- Ghindel's frame --------------------------------------------------------------------------------------------
NUTC = np.array([0.0, 0.0, 0.146])
NUTR = np.array([0.112, 0.104, 0.135])
CAP = np.array([0.0, 0.006, 0.256])
CAPR = np.array([0.138, 0.13, 0.094])
CAPROT = (12.0, -20.0, 0.0)                # the front and the -X side down: the -X eye under the brim
EYE = np.array([0.043, -0.0866, 0.200])
EYE_HALF = (0.0225, 0.0245)
VOXEL = 0.004


def rotm(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def pointed(V):
    """The nut: its lower half drawn in to a blunt point."""
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 - 0.42 * low ** 1.7)[:, None]
    V[:, 2] -= 0.07 * low ** 3
    return V


def rough(seed, n=120, amp=0.06, s=0.16, under=0.3):
    """A deform that makes a sphere an acorn's cap: flat underneath, its dome raised in random knobs."""
    rs = np.random.RandomState(seed)
    Cn = rs.normal(size=(n, 3)); Cn /= np.linalg.norm(Cn, axis=1, keepdims=True)
    A = amp * (0.5 + rs.rand(n))
    def f(V):
        V = V.copy()
        bump = np.zeros(len(V))
        for c, a in zip(Cn, A):
            bump += a * np.exp(-((V - c) ** 2).sum(1) / (s * s))
        top = np.clip(V[:, 2] * 3 + 0.4, 0, 1)            # the knobs on the dome and its brim, not underneath
        V = V * (1 + np.minimum(bump, 1.6 * amp) * top)[:, None]
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * under, V[:, 2])
        return V
    return f


def acorn_cap(k, name, c, r, rot, bone, seed=1, fuse=False, flecks=10, sub=5, surface="shell"):
    """The rough brown acorn cap: a knobbly dome flat underneath, tipped by rot, with darker flecks on its top."""
    r = np.array(r, float)
    V, F = ell((0, 0, 0), r, deform=rough(seed), sub=sub)
    m = rotm(rot)
    V = V @ m.T + np.array(c, float)
    k.add(name, (V, F) if fuse else outward((V, F)), CAPC, bone, fuse=fuse, surface=surface)
    rs = np.random.RandomState(seed + 7)
    for i in range(flecks):
        az = 2 * math.pi * (i + 0.6 * rs.rand()) / flecks
        el = math.radians(25 + 50 * rs.rand())
        d = np.array([math.cos(az) * math.cos(el), math.sin(az) * math.cos(el), math.sin(el)])
        p = m @ (d * r * 0.98) + np.array(c, float)
        n = m @ (d / r ** 2); n /= np.linalg.norm(n)
        a = np.cross(n, (0, 0, 1.0))
        if np.linalg.norm(a) < 1e-3:
            a = np.array([1.0, 0, 0])
        a /= np.linalg.norm(a); b = np.cross(n, a)
        sz = r[0] * (0.13 + 0.08 * rs.rand())
        Vf, Ff = ell((0, 0, 0), (sz, sz * 0.7, sz * 0.22), sub=2)
        Vf = Vf @ np.stack([a, b, n]) + p + n * sz * 0.02
        k.add(f"{name}fleck{i}", outward((Vf, Ff)), CAP_D if i % 3 else CAP_HI, bone, fuse=False, surface=surface)


def leaf_outline(length, width, lobes=0, n=30):
    """A leaf's outline in (across, along): pointed at both ends, widest a little below its middle; with lobes, an
    oak leaf's rounded lobes along each side."""
    pts = []
    for t in np.linspace(0, 1, n):
        w = width * math.sin(math.pi * t) ** 0.75 * (1.05 - 0.3 * t)
        if lobes:
            w *= 0.62 + 0.38 * abs(math.sin(lobes * math.pi * t)) ** 0.6
        pts.append((w, t * length))
    right = pts
    left = [(-w, y) for w, y in reversed(pts[1:-1])]
    return right + left


def flat_leaf(base, d, across, length, width, thick, lobes=0):
    """A flat leaf from base along d, its face square to across x d: a loose plate with a hard rim."""
    d = unit(*d)
    a = np.array(across, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    return slab(leaf_outline(length, width, lobes), base, a, d, thick)


def acorn(k, name, top, size, bone, seed=3):
    """A small acorn hanging from top: a glossy nut drawn to a point and a rough cap over it (loose pieces)."""
    top = np.array(top, float)
    nc = top + (0, 0, -size * 0.95)
    k.add(name + "nut", outward(ell(nc, (size * 0.72, size * 0.72, size * 0.9), deform=pointed, sub=3)), NUT, bone,
          fuse=False, surface="shell")
    V, F = ell((0, 0, 0), (size * 0.85, size * 0.85, size * 0.5), deform=rough(seed, n=24, amp=0.08, s=0.35), sub=3)
    k.add(name + "cap", outward((V + top + (0, 0, -size * 0.38), F)), CAPC, bone, fuse=False, surface="shell")
    k.add(name + "tip", outward(ell(top + (0, 0, size * 0.05), (size * 0.16, size * 0.16, size * 0.22), sub=2)), CAP_D,
          bone, fuse=False, surface="shell")


def dot_eye(iris=IRIS, ink=INK, hi=WHITE, hi_at=(0.28, 0.36), hi_r=0.3, ring=0.0):
    """The line's dot eye (u outward, v up): dark, a lighter iris within a dark rim, a round highlight high and out."""
    CI, CR, CH = srgb(iris), srgb(ink), srgb(hi)
    def look(u, v):
        r = np.hypot(u, v)
        c = np.tile(CI, (len(u), 1))
        c[r > 0.8] = CR
        if ring:
            c[r < ring] = CR
        c[(u - hi_at[0]) ** 2 + (v - hi_at[1]) ** 2 < hi_r ** 2] = CH
        return c
    return look


def board(ob):
    """The game board's check: the four-corner sum against the square it frames."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h = V[:, 2].max(); w = V[:, 0].max() - V[:, 0].min(); l = V[:, 1].max() - V[:, 1].min()
    s = 0.853 * w + 0.522 * l; q = 1.3 * max(h, 0.85 * max(w, l))
    print(f"KIT: board h {h:.3f} w {w:.3f} l {l:.3f}: sum {s:.3f} of square {q:.3f} = {s / q:.2f}")


def ghindel(src, out_path):
    k = Kit(src, coat="shell")

    # ---- the nut: golden, drawn to a blunt point, darker toward it
    k.add("nut", ell(NUTC, NUTR, deform=pointed, sub=5), NUT, "body")
    k.add("tip", ell((0, 0, 0.022), (0.018, 0.018, 0.022)), NUT_D, "body")
    k.paint(lambda P: smoothstep(0.075, 0.02, P[:, 2]), NUT_D)
    # the arms: stubby nubs at the sides
    k.add("arm", tube([(0.08, 0.0, 0.132), (0.108, -0.006, 0.126), (0.124, -0.012, 0.12)], [0.024, 0.02, 0.017]),
          NUT, "arm{s}", mirror=True)

    # ---- the cap, tipped down over the -X eye, and its two leaves
    acorn_cap(k, "cap", CAP, CAPR, CAPROT, "cap", seed=5, flecks=18)
    m = rotm(CAPROT)
    top = CAP + m @ np.array([0, 0, CAPR[2] * 0.9])
    stem_top = top + np.array([0.004, 0.0, 0.03])
    k.add("stem", outward(tube([top - (0, 0, 0.01), top + (0.002, 0, 0.016), stem_top], [0.008, 0.0075, 0.007])),
          STEM, "cap", fuse=False, surface="leaf")
    th = math.radians(25)
    e1 = np.array([math.cos(th), math.sin(th), 0.0])                 # the V's plane turned toward the front camera
    for i, (sgn, rise, L, W) in enumerate(((1, 0.55, 0.082, 0.021), (-1, 0.8, 0.078, 0.02))):
        d = unit(*(e1 * sgn * 0.82 + np.array([0, 0, rise])))
        k.add(f"leaf{i}", flat_leaf(stem_top - d * 0.004, d, e1, L, W, 0.011), LEAF if i == 0 else LEAF_D, "cap",
              fuse=False, surface="leaf")

    # ---- the leaf buds at the arms' ends
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        tip = np.array([0.128 * s, -0.013, 0.12])
        for j, (dz, dy) in enumerate(((0.45, -0.1), (-0.15, 0.15))):
            d = unit(0.8 * s, dy, dz)
            k.add(f"bud{j}{side}", flat_leaf(tip - d * 0.006, d, (0, 1.0, 0.0) if j == 0 else (0, 0.3, 1.0), 0.036,
                                             0.012, 0.009), BUD, "arm" + side, fuse=False, surface="leaf")

    # ---- the face's marks (in every state): a small mouth, a blush, the gloss
    k.decal((0, -0.1018, 0.168), (0, -1, -0.12), (0.0085, 0.0065), MOUTH, eps=0.002, rings=8, segs=32)
    k.decal((0.064, -0.074, 0.178), unit(0.52, -0.85, 0.12), (0.016, 0.0085), BLUSH, eps=0.0018, rings=8, segs=32,
            mirror=True)
    k.decal((-0.068, -0.068, 0.15), unit(-0.62, -0.74, 0.05), (0.011, 0.036), NUT_HI, eps=0.002, rings=8, segs=36)
    k.decal((0.074, -0.062, 0.092), unit(0.66, -0.7, -0.25), (0.009, 0.009), NUT_HI, eps=0.002, rings=6, segs=24)

    body = k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)
    board(body)

    n = unit(EYE[0] / NUTR[0] ** 2, EYE[1] / NUTR[1] ** 2, (EYE[2] - NUTC[2]) / NUTR[2] ** 2)
    look = dot_eye()
    def lay(state):
        k.decal(EYE, n, EYE_HALF, eye_look(state, look, srgb(NUT), srgb(INK), line=0.17), eps=0.0018, rings=20,
                segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    ghindel(*args())
