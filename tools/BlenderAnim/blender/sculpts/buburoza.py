# Buburoza sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/buburoza.py -- <the export's Buburoza.glb> <out.glb>
#
# Buburel grown up (buburel.py's colours, eye, cheeks and antennae). From the project's picture (10018-Buburoza.jpg)
# and brief (CP-dex-1-plateau.md, 18): a larger ladybird, upright and hovering. Its largest masses are its red wing
# cases, opened behind and above the head like the cupped petals of a wild rose, their lips curled back, and
# pale gold spots on them (through both faces, so the back camera sees them on
# the dark red lining too). Under them two pairs of pink wings spread in a shallow V, swept back, veined darker.
# A big round black head: brown eyes in a pale rim with a glint, cream cheek dots, a small gentle smile; two black
# antennae ending in curls. A small black body, ringed below, short black legs dangling, black arms holding a little
# bouquet of wild flowers (yellow, pink, white) with leaves.
# The picture holds the bouquet on the viewer's left; here it is on +X, the side the game's front camera sees. The
# picture's smile is black on the black head; here it is a dark rose so that it reads at the game's size.
# Skeleton: body (0, 0.02, 0.372), head (0, -0.02, 0.472), wing{s} (+-0.05, 0.08, 0.452), arm{s} (+-0.08, -0.04,
# 0.412), leg{s} (+-0.05, 0, 0.252). The game draws it hovering.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, sheet, leaf, srgb, inside, unit, eye_look, outward, smoothstep, \
    args, sibling

b = sibling("buburel")
BLACK, CHEEK, INK_LT = "#3A363A", b.CHEEK, b.INK_LT
RED, RED_D, LINING = "#CE4A34", "#A8362A", "#8E2C24"
GOLD, PINK, VEIN = "#DCC67E", "#F2A8C4", "#DA80A6"
RING, ROSE = "#585258", "#8E3444"
IRIS, RIMC, PUPIL = "#7A5642", "#E9DFCC", "#1E1614"
STEM, LEAF = "#5E9A44", "#6EAE4C"
FLOWERS = {"yellow": ("#F4CC3A", "#E0902C"), "pink": ("#F09AB8", "#F4CC3A"), "white": ("#FAF8F0", "#F4CC3A")}

HC, HR = np.array([0.0, -0.035, 0.495]), np.array([0.13, 0.112, 0.12])
E = 0.003


def case_frame(s):
    """A wing case's frame on side s: its centre, its long axis, its width axis and its convex face's normal."""
    C = np.array([0.125 * s, 0.11, 0.565])
    a = unit(0.35 * s, 0.18, 1)
    n = unit(0.55 * s, -0.8, 0.25); n = n - a * (n @ a); n /= np.linalg.norm(n)
    w = np.cross(n, a); w /= np.linalg.norm(w)
    if w[0] * s < 0:
        w = -w
    return C, a, w, n


def cup(C, a, w, n, HL, HW, D, curl, off=0.0, scale=1.0):
    """A cupped petal's middle surface: a dome over an ellipse, its lip curled back."""
    def at(u, v):
        x, y = 2 * u - 1, 2 * v - 1
        xd, yd = x * np.sqrt(1 - y * y / 2), y * np.sqrt(1 - x * x / 2)
        rr = np.clip(xd * xd + yd * yd, 0, 1)
        h = D * (1 - rr) - curl * smoothstep(0.6, 1.0, np.sqrt(rr)) ** 2 + off
        return (C[None, None] + a[None, None] * (HL * scale * yd)[..., None] + w[None, None] * (HW * scale * xd)[..., None]
                + n[None, None] * h[..., None])
    return at


def wing_case(k, s, bone):
    C, a, w, n = case_frame(s)
    HL, HW, D, CURL = 0.17, 0.13, 0.05, 0.014
    k.add(f"case{s}", outward(sheet(cup(C, a, w, n, HL, HW, D, CURL), 26, 26, 0.014)), RED, bone, fuse=False)
    k.add(f"lining{s}", outward(sheet(cup(C, a, w, n, HL, HW, D, CURL, off=-0.014, scale=0.96), 24, 24, 0.012)),
          LINING, bone, fuse=False)
    # the pale gold spots: discs through both faces
    f = cup(C, a, w, n, HL, HW, D, CURL)
    for x, y, r in ((0.0, 0.42, 0.034), (-0.48, 0.02, 0.03), (0.42, -0.08, 0.032), (0.05, -0.5, 0.027),
                    (0.5, 0.45, 0.022), (-0.36, 0.62, 0.02)):
        u, v = np.array([[(x + 1) / 2]]), np.array([[(y + 1) / 2]])
        p = f(u, v)[0, 0]
        du = (f(u + 0.01, v) - f(u - 0.01, v))[0, 0]; dv = (f(u, v + 0.01) - f(u, v - 0.01))[0, 0]
        nn = np.cross(du, dv); nn /= np.linalg.norm(nn)
        if nn @ n < 0:
            nn = -nn
        k.add(f"gold{s}_{x}", lathe(p - nn * 0.007, nn, [(0, -0.034), (r, -0.034), (r, 0.018), (0, 0.018)], seg=24),
              GOLD, bone, fuse=False)


def wing_outline(L, W):
    t = np.linspace(0, 1, 20)
    h = W * np.sqrt(np.clip(np.sin(np.pi * t), 0, 1)) * (0.75 + 0.45 * t) * (1 - 0.15 * t)
    h[0], h[-1] = 0.006, 0.006
    return [(x * L, y) for x, y in zip(t, h)] + [(x * L, -y) for x, y in zip(t, h)][::-1]


def pink_wing(k, s, root, d, L, W, bone, name):
    ex = unit(*d)
    ez = np.array([0, 0, 1.0]) - ex * ex[2]; ez /= np.linalg.norm(ez)
    k.add(name, slab(wing_outline(L, W), root, ex, ez, 0.008), PINK, bone, fuse=False, glow=PINK)
    for i, (a0, b0, a1, b1) in enumerate(((0.04, 0.0, 0.92, 0.012), (0.1, 0.0, 0.7, 0.55 * W))):
        p0, p1 = np.array([a0 * L, b0]), np.array([a1 * L, b1])
        e = (p1 - p0) / np.linalg.norm(p1 - p0); o = np.array([-e[1], e[0]]) * 0.006
        bar = [tuple(p0 + o), tuple(p1 + o), tuple(p1 - o), tuple(p0 - o)]
        k.add(f"{name}vein{i}", slab(bar, root, ex, ez, 0.012), VEIN, bone, fuse=False)


def flower(k, c, n, colour, bone):
    petal, heart = FLOWERS[colour]
    n = unit(*n)
    e1 = np.cross(n, (0, 0, 1.0)); e1 /= np.linalg.norm(e1); e2 = np.cross(n, e1)
    for i in range(5):
        t = 2 * math.pi * i / 5
        k.add(f"petal{c[0]:.3f}{c[2]:.3f}_{i}", ell(c + 0.0125 * (math.cos(t) * e1 + math.sin(t) * e2), (0.011, 0.011, 0.011)),
              petal, bone, fuse=False, surface="leaf")
    k.add(f"heart{c[0]:.3f}{c[2]:.3f}", ell(c + n * 0.004, (0.008, 0.008, 0.008)), heart, bone, fuse=False, surface="leaf")


def bouquet(k, bone):
    base = np.array([0.085, -0.185, 0.215])
    heads = [((0.09, -0.215, 0.415), "yellow"), ((0.125, -0.2, 0.39), "pink"), ((0.058, -0.205, 0.39), "white"),
             ((0.108, -0.228, 0.357), "pink"), ((0.068, -0.225, 0.352), "yellow"), ((0.14, -0.212, 0.35), "white")]
    for i, (h, colour) in enumerate(heads):
        h = np.array(h)
        k.add(f"stem{i}", tube([base, (base + h) / 2 + np.array([0, -0.005, 0]), h], [0.0055, 0.005, 0.0045], seg=8),
              STEM, bone, fuse=False, surface="leaf")
        flower(k, h, (0.25 + 0.3 * (h[0] - 0.1) / 0.04, -1, 0.35), colour, bone)
    for i, (d, L) in enumerate((((0.6, -0.4, 0.7), 0.06), ((-0.5, -0.4, 0.75), 0.055), ((0.8, -0.3, 0.3), 0.05))):
        k.add(f"leaf{i}", outward(leaf(base + np.array([0, -0.01, 0.07 + 0.02 * i]), d, L, 0.013, thick=0.006)), LEAF,
              bone, fuse=False, surface="leaf")


def smile_look(u, v):
    c = np.tile(srgb(BLACK), (len(u), 1))
    c[(np.abs(v - (-0.42 + 0.78 * u * u)) < 0.26) & (np.abs(u) < 0.86)] = srgb(ROSE)
    return c


def lay_eyes(k, state):
    open_ = b.round_eye(white=RIMC, iris=IRIS, pupil=PUPIL, pupil_r=0.42, rim=RIMC, look=(-0.04, -0.02))
    look = eye_look(state, open_, srgb(BLACK), srgb(INK_LT), line=0.17)
    for s in (1, -1):
        p, n = b.on_ell(HC, HR, (0.4 * s, -0.88, 0.12))
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(p, n, (0.037, 0.042), col, up=(-0.1 * s, 0, 1), eps=0.0026, rings=20, segs=56)


def buburoza(src, out):
    k = Kit(src, coat="shell")
    # ---- the body: a big round head, a short neck, a small round abdomen ringed below
    k.add("head", ell(HC, HR, sub=5), BLACK, "head")
    k.add("thorax", ell((0, -0.005, 0.372), (0.085, 0.08, 0.062)), BLACK, "body")
    k.add("abdomen", ell((0, 0.03, 0.27), (0.1, 0.09, 0.11)), BLACK, "body")
    def rings(P):
        m = np.zeros(len(P))
        for z in (0.205, 0.245, 0.285, 0.325):
            m = np.maximum(m, inside(np.abs(P[:, 2] - z) - 0.0032, 0.0014))
        return m
    k.paint(rings, RING, on=["abdomen"])
    # ---- short legs dangling
    k.add("leg", tube([(0.05, 0.0, 0.215), (0.07, -0.04, 0.155), (0.064, -0.012, 0.11)], [0.019, 0.017, 0.016]),
          BLACK, "leg{s}", mirror=True)
    k.add("foot", ell((0.064, -0.02, 0.1), (0.02, 0.026, 0.016)), BLACK, "leg{s}", mirror=True)
    # ---- the face: cream cheeks and a small smile (in every eye state: before the build)
    for s in (1, -1):
        p, n = b.on_ell(HC, HR, (0.78 * s, -0.55, -0.3))
        k.decal(p, n, (0.026, 0.026), CHEEK, eps=0.0022, rings=10, segs=40)
    p, n = b.on_ell(HC, HR, (0, -1, -0.42))
    k.decal(p, n, (0.03, 0.015), smile_look, eps=0.0024, rings=12, segs=48)
    # ---- the antennae, ending in curls
    t = np.linspace(0, 1, 9)
    cc = np.array([0.083, -0.072, 0.712])
    r = 0.022 * (1 - 0.55 * t); ang = math.pi - 2 * math.pi * 1.05 * t
    curl = [cc + np.array([ri * math.cos(a), 0, ri * math.sin(a)]) for ri, a in zip(r, ang)]
    pts = [(0.035, -0.06, 0.6), (0.045, -0.07, 0.65)] + [tuple(p) for p in curl]
    k.add("antenna", tube(pts, list(np.linspace(0.0105, 0.0075, len(pts))), seg=12), BLACK, "head", fuse=False,
          mirror=True)
    # ---- the arms, loose, holding the bouquet on +X
    for side, pts in (("R", [(0.085, -0.04, 0.41), (0.13, -0.1, 0.34), (0.09, -0.165, 0.31)]),
                      ("L", [(-0.085, -0.04, 0.41), (-0.075, -0.13, 0.33), (0.03, -0.178, 0.3)])):
        k.add(f"arm{side}", tube(pts, [0.019, 0.018, 0.018]), BLACK, f"arm{side}", fuse=False)
        k.add(f"hand{side}", ell(pts[-1], (0.023, 0.022, 0.022)), BLACK, f"arm{side}", fuse=False)
    bouquet(k, "armR")
    # ---- the wing cases opened like a rose, and the pink wings under them
    for s, side in ((1, "R"), (-1, "L")):
        wing_case(k, s, f"wing{side}")
        pink_wing(k, s, (0.055 * s, 0.1, 0.44), (0.9 * s, 0.38, 0.15), 0.29, 0.085, f"wing{side}", f"fore{side}")
        pink_wing(k, s, (0.05 * s, 0.11, 0.42), (0.85 * s, 0.4, -0.38), 0.22, 0.065, f"wing{side}", f"hind{side}")
    k.build(voxel=0.0055, smooth=3, band=0.006, wband=0.04)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    buburoza(*args())
