# Iron Moth sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironmoth.py -- <the export's Iron Moth.glb> <out.glb>
#
# A machine Volcarona (volcarona.py's frame: wings as flat plates fanned round the body facing the cameras, marks as
# thicker slabs through both faces; the eye laid as a patch on a fused head). From the references: a black domed head
# leaning back, one round orange grille on its front crossed by two dark bars (one eye in the middle, not a pair: both
# pictures show a single disc); two red horns curling up from beside the head like brackets; a white armoured thorax
# under it whose front is a mask with four dark holes (two large slanted above, two small below), its lower sides
# running out into white points; a long abdomen hanging down and forward like a shell casing, pale ice-blue sides with
# dark slots, a black ridge down its middle and a black point at its end; six orange kite-shaped wings standing apart
# from the body, three a side (up, out, down), each with a red cap at its inner point, a black triangle in its outer
# half and a few white specks. The wings are shorter than the pictures' so the model fills its cell.
# Bones: root, body (0, 0, 0.316), wing{s} (0.04, 0.04, 0.336), head (0, -0.03, 0.386), leg{s} and hind{s} at z 0.266.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, unit, polygon, eye_look, args

WHITE, HOLE, BLACK, RED = "#ECEEF2", "#2A2A32", "#2A2A30", "#D8323A"
ICE, SLOT = "#B6DDE6", "#30343C"
ORANGE, WREDGE, TRIANGLE, SPECK = "#F5A23A", "#E2402E", "#2E2C30", "#FFF4DC"
GRILLE, GRILLE_DARK = srgb("#F28A2E"), srgb("#3A2418")

H = np.array([0.0, -0.05, 0.445])            # the head's middle
HR = np.array([0.048, 0.045, 0.054])
T = np.array([0.0, -0.025, 0.365])           # the thorax's middle
TR = np.array([0.08, 0.064, 0.058])


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def spike(k, name, base, d, length, width, thick, up, color, bone, sink=0.01, mirror=False, surface="metal"):
    """A flat-faced point from base along d, its kite section wide across `up` (larvitar.py's spike)."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * sink
    pts = [b + side * width / 2, b - side * width / 2, b + up * thick / 2, b - up * thick / 2,
           base + d * length * 0.45 + side * width * 0.3, base + d * length * 0.45 - side * width * 0.3,
           base + d * length * 0.45 + up * thick * 0.3, base + d * length * 0.45 - up * thick * 0.3,
           base + d * length]
    k.add(name, hull(np.array(pts)), color, bone, fuse=False, mirror=mirror, surface=surface)


def head(k):
    # a dome leaning a little back, its front flattened for the grille
    def dome(V):
        V = V.copy()
        up = np.clip(V[:, 2], 0, 1)
        V[:, 1] += 0.25 * up ** 1.5                 # the crown leaning back
        V[:, 0] *= 1 - 0.18 * up ** 2
        return V
    k.add("head", ell(H, HR, deform=dome), BLACK, "head")
    k.add("face", ell(H + (0, -0.018, -0.004), (0.036, 0.03, 0.036)), BLACK, "head")


def horns(k):
    pts = np.array([(0.034, -0.055, 0.405), (0.068, -0.064, 0.43), (0.08, -0.06, 0.47), (0.07, -0.05, 0.51),
                    (0.046, -0.042, 0.535)])
    k.add("horn", outward(tube(pts, [0.014, 0.014, 0.013, 0.0105, 0.007])), RED, "head", fuse=False, mirror=True,
          surface="metal")


def thorax(k):
    k.add("thorax", ell(T, TR), WHITE, "body")
    k.add("chestplate", ell(T + (0, -0.022, 0.0), (0.066, 0.05, 0.05)), WHITE, "body")
    # the white points running out of the mask's lower sides, down and out (on the leg bones, as the pictures' flaps)
    spike(k, "flap", (0.06, -0.035, 0.335), (0.75, -0.25, -0.6), 0.07, 0.05, 0.024, (0, -1, 0), WHITE, "leg{s}",
          mirror=True)
    spike(k, "flapback", (0.06, 0.02, 0.33), (0.7, 0.35, -0.6), 0.06, 0.045, 0.022, (0, 1, 0), WHITE, "hind{s}",
          mirror=True)


def mask_holes(k):
    """The four dark holes of the mask: two large slanted teardrops above, two small ovals below."""
    tear = polygon([(-1, -0.55), (-0.2, -1), (0.7, -0.6), (1, 0.2), (0.5, 0.95), (-0.4, 0.75), (-0.95, 0.15)])
    for s in (1, -1):
        c = on_ell(T + (0, -0.022, 0), (0.066, 0.05, 0.05), (0.42 * s, -0.85, 0.32))
        k.decal(c, unit(0.38 * s, -0.9, 0.2), (0.019, 0.013), HOLE, up=unit(-0.45 * s, 0, 1), eps=0.0025,
                outline=tear if s > 0 else (lambda th: tear(math.pi - th)), rings=10, segs=48)
        c2 = on_ell(T + (0, -0.022, 0), (0.066, 0.05, 0.05), (0.3 * s, -0.9, -0.38))
        k.decal(c2, unit(0.3 * s, -0.92, -0.25), (0.0115, 0.009), HOLE, eps=0.0025, rings=8, segs=40)


ABD = [(0, 0.02, 0.315), (0, -0.002, 0.255), (0, -0.032, 0.2), (0, -0.064, 0.152), (0, -0.092, 0.11)]
ABR = [0.056, 0.06, 0.054, 0.038, 0.01]


def abdomen(k):
    k.add("abdomen", tube(ABD, ABR), ICE, "body")
    k.add("abdomentip", ell((0, -0.085, 0.118), (0.02, 0.022, 0.03), rot=(-40, 0, 0)), BLACK, "body")
    pts = np.array(ABD)
    def ridge(P):
        # a black band down the middle, top and bottom, and the end black
        return inside(P[:, 2] - 0.15, 0.003)
    # the black ridge down the middle, front and back: loose flattened strips standing just proud, crisp where
    # paint hatched at this voxel
    for name, sgn in (("ridgefront", -1), ("ridgeback", 1)):
        off = np.array([0, 0.87 * sgn, -0.5 * sgn])
        rp = [np.array(p) + off * r * 0.93 for p, r in zip(ABD[:4], ABR[:4])]
        k.add(name, outward(tube(rp, [0.017, 0.018, 0.016, 0.012], seg=12)), BLACK, "body", fuse=False,
              surface="metal")
    def rings(P):
        m = np.zeros(len(P))
        for z in (0.268, 0.214):
            m = np.maximum(m, inside(np.abs(P[:, 2] - z - 0.5 * (P[:, 1] - pts[1][1])) - 0.004, 0.0018))
        return m * inside(P[:, 2] - 0.3, 0.003)
    k.paint(ridge, BLACK, on=["abdomen"])
    pass
    # dark slots on the pale sides, two a side, cast from the side and the front
    for s in (1, -1):
        for (y, z, n) in ((-0.016, 0.24, (0.85, -0.5, 0.15)), (-0.05, 0.178, (0.8, -0.55, 0.2))):
            c = np.array([0.0, y, z])
            r = np.interp(z, [p[2] for p in ABD[::-1]], ABR[::-1])
            c = c + unit(n[0] * s, n[1], n[2]) * r * 0.9
            k.decal(c, unit(n[0] * s, n[1], n[2]), (0.014, 0.0075), SLOT, up=unit(0, 0.5, 1), eps=0.0025,
                    rings=8, segs=40)


# --- the wings: kites standing apart from the body, three a side ---------------------------------------------------
# name, angle up from straight out (degrees), length, widest half width
WINGS = [("upper", 56, 0.235, 0.064), ("middle", -4, 0.245, 0.066), ("lower", -62, 0.215, 0.06)]
WING_ROOT = np.array([0.075, 0.03, 0.37])
WIDEST = 0.42


def kite(L, W, t0=0.0, t1=1.0):
    """A kite's outline (along, across) from t0 to t1 of its length: inner point, widest at WIDEST, outer point."""
    def half(t):
        return W * (t / WIDEST if t < WIDEST else (1 - t) / (1 - WIDEST))
    ts = [t0] + [t for t in (WIDEST,) if t0 < t < t1] + [t1]
    upper = [(t * L, half(t)) for t in ts]
    lower = [(t * L, -half(t)) for t in ts[::-1]]
    out = upper + lower
    # drop doubled points at a point of the kite
    clean = []
    for p in out:
        if not clean or abs(p[0] - clean[-1][0]) + abs(p[1] - clean[-1][1]) > 1e-6:
            clean.append(p)
    if abs(clean[0][0] - clean[-1][0]) + abs(clean[0][1] - clean[-1][1]) < 1e-6:
        clean.pop()
    return clean


def wings(k):
    for name, ang, L, W in WINGS:
        a = math.radians(ang)
        d = unit(math.cos(a), 0.22, math.sin(a))
        across = unit(-math.sin(a), 0, math.cos(a))
        origin = WING_ROOT + d * 0.03
        k.add(name, slab(kite(L, W, 0.0, 1.0), origin, d, across, 0.011), ORANGE, "wing{s}", fuse=False,
              mirror=True, surface="metal")
        k.add(name + "cap", slab(kite(L, W, 0.0, 0.16), origin, d, across, 0.017), WREDGE, "wing{s}", fuse=False,
              mirror=True, surface="metal")
        # the black triangle in the outer half, its point toward the wing's outer tip
        tri_ = [(0.56 * L, -0.38 * W), (0.56 * L, 0.38 * W), (0.8 * L, 0.0)]
        k.add(name + "tri", slab(tri_, origin, d, across, 0.017), TRIANGLE, "wing{s}", fuse=False, mirror=True,
              surface="metal")
        # the crease across the widest, a darker line
        crease = [(WIDEST * L - 0.003, -W * 0.97), (WIDEST * L + 0.003, -W * 0.97), (WIDEST * L + 0.003, W * 0.97),
                  (WIDEST * L - 0.003, W * 0.97)]
        k.add(name + "crease", slab(crease, origin, d, across, 0.0145), "#D27A22", "wing{s}", fuse=False,
              mirror=True, surface="metal")
        for i, (t, s_) in enumerate(((0.3, 0.35), (0.48, -0.5), (0.62, 0.45), (0.9, 0.05))):
            th = np.linspace(0, 2 * math.pi, 12, endpoint=False)
            r = 0.0052
            dot = np.stack([t * L + r * np.cos(th), s_ * W * (1 - abs(t - WIDEST)) + r * np.sin(th)], axis=1)
            k.add(f"{name}speck{i}", slab(dot, origin, d, across, 0.016), SPECK, "wing{s}", fuse=False, mirror=True,
                  surface="metal")


# --- the eye: one round orange grille on the head's front, crossed by two dark bars --------------------------------
EYE_C = on_ell(H + (0, -0.018, -0.004), (0.036, 0.03, 0.036), (0, -1, 0.1))
EYE_N = unit(0, -1, 0.12)
EYE_HALF = (0.025, 0.025)
LID, LINE = srgb(BLACK), srgb("#E07A2A")


def grille(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(GRILLE, (len(u), 1))
    c[r > 0.8] = GRILLE_DARK
    return c


FIERCE_CUT = polygon([(-1.2, -1.2), (1.2, -1.2), (1.2, 0.55), (0.0, 0.05), (-1.2, 0.55)])


def lay(k):
    def eyes(state):
        if state == "Fierce":
            k.decal(EYE_C, EYE_N, EYE_HALF, grille, eps=0.0025, rings=20, segs=56,
                    outline=lambda th: min(1.0, FIERCE_CUT(th)))
        else:
            k.decal(EYE_C, EYE_N, EYE_HALF, eye_look(state, grille, LID, LINE, line=0.17), eps=0.0025, rings=20,
                    segs=56)
        if state in ("Open", "Fierce"):
            # the two bars, each a patch of its own; under a Fierce brow only the lower part of the upright one
            k.decal(EYE_C, EYE_N, (0.0042, 0.021 if state == "Open" else 0.012), GRILLE_DARK, eps=0.0034, rings=4,
                    segs=24, outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]))
            k.decal(EYE_C + np.array([0, 0, -0.0015]), EYE_N, (0.021, 0.0042), GRILLE_DARK, eps=0.0034, rings=4,
                    segs=24, outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]))
    return eyes


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="metal")
    head(k)
    horns(k)
    thorax(k)
    abdomen(k)
    wings(k)
    mask_holes(k)
    k.build(voxel=0.0045, smooth=3, band=0.006, wband=0.04)
    k.eye_states(lay(k))
    k.export(out)
