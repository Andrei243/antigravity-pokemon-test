# Rookidee sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rookidee.py -- <the export's Rookidee.glb> <out.glb>
# From the references: a tiny round bird, head and body one ball; a black hood over the face, throat and upper chest,
# its lower edge jagged with a point down the middle over the yellow belly; a blue back, crown stripe, folded wings
# and a long blue tail; a crown of black spikes, the front one tallest, two more behind and one over each eye's back;
# a big white eye with a red iris, cut along its top by a black brow slanting down to the beak; a short grey cone of a
# beak; short grey legs, three toes forward and one back. Written to be grown from: Corvisquire and Corviknight import
# it (sibling("rookidee")) and keep its spikes, its beak, its feet, its tail and its way of making the eyes.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, lathe, srgb, inside, tri, unit, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
BLUE, WINGBLUE, BLACK, YELLOW = "#3B62B4", "#3354A2", "#28262C", "#E8D25C"
GREY, CLAW, WHITE, RED, GLINT = "#9A9EAA", "#5E616C", "#F8F8F4", "#D0283A", "#FFFFFF"
LINE = "#B8BCC8"                                     # the shut eye's line on the black hood


# --- helpers ------------------------------------------------------------------------------------------------------
def frame_of(d, side=None):
    """A right-handed frame round a direction: (d, side, n), side across the piece's width, n through its thickness."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(side if side is not None else np.cross(d, (0, 0, 1.0)), float)
    if np.linalg.norm(s) < 1e-3:
        s = np.array([1.0, 0, 0])
    s = s - d * (s @ d); s /= np.linalg.norm(s)
    return d, s, np.cross(d, s)


def spike(base, tip, w, t, side=None, at=0.35):
    """A pointed feather or horn: a flat-faced kite, w wide across side, t thick, widest at `at` of its length."""
    base, tip = np.array(base, float), np.array(tip, float)
    d, s, n = frame_of(tip - base, side)
    m = base + (tip - base) * at
    return hull([base + s * w * 0.3, base - s * w * 0.3, base + n * t * 0.5, base - n * t * 0.5,
                 m + s * w * 0.5, m - s * w * 0.5, m + n * t * 0.5, m - n * t * 0.5, tip])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in a direction from its centre."""
    d = np.array(d, float)
    return np.array(c, float) + d / np.linalg.norm(d / np.array(r, float))


# --- the frame: every place and size from the skeleton's joints ----------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.038, 0.204])              # the head's centre, before its joint (0, -0.04, 0.19)
    F.HR = np.array([0.08, 0.076, 0.074])
    F.BODY = ((0, 0.018, 0.147), (0.092, 0.1, 0.088), (-6, 0, 0))
    # the folded wing along the side, shoulder to tip (the wing's joint is (0.06, -0.03, 0.17))
    F.WING = ([(0.07, -0.025, 0.19), (0.088, 0.03, 0.17), (0.082, 0.09, 0.15), (0.055, 0.13, 0.156)],
              [0.032, 0.046, 0.036, 0.012], 0.38)
    F.WINGTIP = np.array([0.06, 0.118, 0.16])
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.037, 0.105, 0.013, 0.0
    F.LEGR = (0.0125, 0.011)
    F.TOE, F.HEEL, F.TOER = 0.042, 0.024, 0.0068
    F.BEAK = (np.array([0, -0.098, 0.212]), unit(0, -1, 0.04), 0.068, 0.02)
    F.EYEDIR = unit(0.64, -0.7, 0.2)
    F.EYE = (0.035, 0.031)
    F.TAILBASE = np.array([0, 0.095, 0.14])
    F.HOOD = (-0.008, 0.22, 0.15)                   # black in front of y = a + b (z - c)
    F.YELLOW = (0.152, 0.017, 0.05)                 # the yellow's top edge: z0, its teeth's height, their spacing
    F.VOXEL = 0.0034
    return F


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, colour=BLUE):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), colour, "body")
    k.add("head", ell(F.H, F.HR), colour, "head")


def coat(k, F):
    """The black hood in front of a slanting plane, the blue crown stripe over it, the yellow belly with a jagged top."""
    a, b, c = F.HOOD
    k.paint(lambda P: inside(P[:, 1] - (a + b * (P[:, 2] - c)), 0.003), BLACK)
    z0, t, sp = F.YELLOW
    edge = lambda P: z0 - t * tri(P[:, 0] / sp + 0.5)
    k.paint(lambda P: inside(P[:, 2] - edge(P), 0.0025) * inside(P[:, 1] - 0.03, 0.006)
            * inside(np.abs(P[:, 0]) - 0.075, 0.006), YELLOW)
    # the blue stripe along the crown, from behind the front spike back to the blue of the back
    k.paint(lambda P: inside(np.abs(P[:, 0]) - (0.016 + 0.12 * np.clip(P[:, 1] + 0.03, 0, 0.2)), 0.0025)
            * inside(F.H[2] + 0.04 - P[:, 2], 0.003) * inside(-0.07 - P[:, 1], 0.003), BLUE)


def crown(k, F, colour=BLACK):
    """The crown's spikes: the front one tallest, two behind it on the midline, one over each eye's back."""
    H, HR = F.H, F.HR
    top = H + np.array([0, 0, HR[2]])
    for name, b, t, w, th in (("spike0", (0, -0.05, -0.012), (0, -0.004, 0.058), 0.044, 0.018),
                              ("spike1", (0, -0.006, -0.008), (0, 0.046, 0.036), 0.038, 0.016),
                              ("spike2", (0, 0.03, -0.018), (0, 0.078, 0.012), 0.034, 0.015)):
        k.add(name, spike(top + b, top + t, w, th, side=(1, 0, 0)), colour, "head", fuse=False)
    for s in (1, -1):
        k.add("spikeside", spike(top + (0.045 * s, -0.012, -0.026), top + (0.066 * s, 0.03, 0.036), 0.03, 0.014,
                                 side=(0.3 * s, 0, 1)), colour, "head", fuse=False)


def wings_folded(k, F, colour=WINGBLUE):
    pts, radii, f = F.WING
    k.add("wing", blade(pts, radii, (1, 0, 0), f), colour, "wing{s}", mirror=True)
    for j, (dy, dz, ln) in enumerate(((1.0, 0.35, 0.05), (0.9, 0.0, 0.042), (0.8, 0.7, 0.036))):
        d = unit(0.12, dy, dz)
        k.add(f"wingtip{j}", spike(F.WINGTIP, F.WINGTIP + d * ln, 0.03, 0.012, side=(0.2, 0, 1)), colour, "wing{s}",
              fuse=False, mirror=True)


def beak(k, F, colour=GREY):
    c, d, ln, r = F.BEAK
    k.add("beak", lathe(c, d, [(0, -0.01), (r, -0.008), (r, 0.004), (r * 0.78, ln * 0.35), (r * 0.42, ln * 0.75),
                               (r * 0.14, ln * 0.96), (0, ln)], seg=28), colour, "head", fuse=False, surface="shell")


def tail(k, F, feathers, pitch=-6, thick=0.009, lean=30, base=None, bone="tail"):
    """
    Long flat feathers from the rump: (yaw toward +X, length, width, colour) each, pitched by pitch degrees and leaned
    about their own length by lean degrees so the cameras behind and before see their faces, not their edges.
    """
    base0 = F.TAILBASE if base is None else np.array(base, float)
    p = math.radians(pitch)
    for i, (yaw, ln, w, col) in enumerate(feathers):
        y = math.radians(yaw)
        d = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        up = np.cross(side, d)
        r = math.radians(lean)
        side = side * math.cos(r) + up * math.sin(r)
        half = [(0.0, w * 0.26), (ln * 0.25, w * 0.46), (ln * 0.7, w * 0.5), (ln * 0.88, w * 0.42), (ln, w * 0.1)]
        outline = half + [(a, -h) for a, h in half[::-1]]
        k.add(f"tail{i}", slab(outline, base0 + np.array([0, 0, -0.003 * i]), d, side, thick), col, bone, fuse=False)


def legs(k, F, foot=GREY, claw=CLAW, toes=(-32, 0, 32, 180), clawlen=2.0):
    for s, side in ((1, "R"), (-1, "L")):
        x = F.LEGX * s
        y = F.LEGY
        ank = np.array([x * 1.06, y - 0.004, F.ANKLE])
        k.add(f"leg{side}", tube([(x, y, F.LEGTOP), (x * 1.03, y - 0.002, (F.LEGTOP + F.ANKLE) / 2), ank],
                                 [F.LEGR[0], F.LEGR[0] * 0.95, F.LEGR[1]]), foot, f"leg{side}", fuse=False,
              surface="scales")
        for j, ang in enumerate(toes):
            rr = math.radians(ang)
            d = np.array([math.sin(rr) * s, -math.cos(rr), 0])
            ln = F.TOE if ang != 180 else F.HEEL
            tip = ank + d * ln + np.array([0, 0, -F.ANKLE + F.TOER * 0.9])
            k.add(f"toe{side}{j}", tube([ank, (ank + tip) / 2 + np.array([0, 0, F.TOER * 0.4]), tip],
                                        [F.TOER * 1.15, F.TOER, F.TOER * 0.9]), foot, f"leg{side}", fuse=False,
                  surface="scales")
            cd = d + np.array([0, 0, -0.5])
            k.add(f"claw{side}{j}", spike(tip, tip + cd * F.TOER * clawlen, F.TOER * 1.5, F.TOER * 1.3), claw,
                  f"leg{side}", fuse=False, surface="shell")


# --- the eyes -----------------------------------------------------------------------------------------------------
def eye_outline(top, side, n=72):
    """The eye's outline in the patch's u, v: its unit circle cut along the top by the brow's line top(u)."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        u, v = math.cos(th), math.sin(th)
        if v > top(u):
            lo, hi = 0.0, 1.0
            for _ in range(30):
                t = (lo + hi) / 2
                if t * v > top(t * u):
                    hi = t
                else:
                    lo = t
            u, v = lo * u, lo * v
        pts.append((side * u, v))
    pts = np.array(pts)
    a = np.arctan2(pts[:, 1], pts[:, 0]); d = np.hypot(pts[:, 0], pts[:, 1]); o = np.argsort(a)
    return lambda th: float(np.interp((th + math.pi) % (2 * math.pi) - math.pi, a[o], d[o], period=2 * math.pi))


def iris_eye(white, iris, glint, at=(-0.12, -0.08), r=0.5):
    """A white with a large solid red iris toward the front (u is toward the back) and a white glint on the iris."""
    W, I, G = srgb(white), srgb(iris), srgb(glint)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[np.hypot(u - at[0], (v - at[1]) * 0.95) < r] = I
        c[np.hypot(u - at[0] + r * 0.32, v - at[1] - r * 0.36) < r * 0.3] = G
        return c
    return look


def eyes(k, F, open_look, top, fierce_top, lid=BLACK, ink=LINE, E=None, de=None, half=None, eps=0.003):
    """Both eyes laid by hand, cut along the top by a brow slanting down toward the beak; u toward the back on both."""
    de = F.EYEDIR if de is None else de
    E = on_ell(F.H, F.HR, de) if E is None else E
    half = F.EYE if half is None else half
    L, K = srgb(lid), srgb(ink)
    def lay(state):
        for side in (1, -1):
            m = np.array([side, 1, 1.0])
            if state in ("Open", "Fierce"):
                t, look = (top if state == "Open" else fierce_top), open_look
            else:
                t, look = (lambda u: top(u) - 0.04), eye_look(state, open_look, L, K, line=0.16)
            col = look if side > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            k.decal(E * m, de * m, half, col, eps=eps, outline=eye_outline(t, side), rings=20, segs=56)
    return lay


def rookidee(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    coat(k, F)
    crown(k, F)
    wings_folded(k, F)
    beak(k, F)
    tail(k, F, [(18, 0.17, 0.05, BLUE), (6, 0.15, 0.046, WINGBLUE), (32, 0.14, 0.044, WINGBLUE)])
    legs(k, F)
    k.build(voxel=F.VOXEL, band=0.004, wband=0.03)
    k.eye_states(eyes(k, F, iris_eye(WHITE, RED, GLINT), lambda u: 0.42 + 0.42 * u, lambda u: 0.05 + 0.62 * u))
    k.export(out_path)


if __name__ == "__main__":
    rookidee(*args())
