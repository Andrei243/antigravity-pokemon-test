# Albinut sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/albinut.py -- <the export's Albinut.glb> <out.glb>
#
# From the picture (docs/art/reference/gemini/carpathia/10035-Albinut.jpg) and its brief (CP-dex-1b-plateau-more.md,
# 35): a round fuzzy honeybee that is one ball seen from the front. The front half is the head, a pale golden fuzzy
# ball with the face low on it: two big dark brown eyes with a large white glint, a small open smile with a pink
# tongue. Behind it, a little smaller and set back, the abdomen: amber, ringed by two dark brown bands with a golden
# band between and an orange band where it meets the head, a small tan sting at its end. On the crown a cap of pale wax
# that drips over the head's edge, and two short brown antennae out of the cap's sides, curving out and hooking forward.
# Two pairs of small translucent wings out of the back, the upper pair standing up, the lower out and back. Six stubby
# brown legs dangling under it.
# Written to be grown from: stupara.py imports it with sibling("albinut") (the palette, the eye, the wing, worker()).
# Skeleton: body (0, 0.02, 0.209), head (0, -0.05, 0.209), wings (+-0.06, 0.06, 0.329). Height about 0.48, hovering.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, slab, hull, ring_band, srgb, unit, inside, eye_look, polygon, lumpy, args,
                       outward)

# --- the line's colours ----------------------------------------------------------------------------------------------
GOLD = "#F3C762"          # the head's down
AMBER = "#E8A640"         # the abdomen
ORANGE = "#D98A2E"        # the band where the abdomen meets the head
BAND = "#5C3620"          # the dark stripes
LEG = "#7A4C2C"
WAX = "#F4E6C2"
STING = "#C89552"
WING = "#F3EFE4"
WINGRIM = "#CDBF9E"
EYE_IRIS, EYE_PUPIL, GLINT = srgb("#5A3420"), srgb("#1E120C"), np.array([1.0, 1.0, 1.0])
INK = srgb("#3A2216")
MOUTH, TONGUE = srgb("#6E2A22"), srgb("#E9787A")

# --- Albinut's frame ------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.062, 0.205])         # the head: the front half of the ball
HR = np.array([0.137, 0.126, 0.13])
AC = np.array([0.0, 0.07, 0.218])           # the abdomen, set back and a little smaller
AR = np.array([0.124, 0.152, 0.122])


def abdomen_shape(V):
    """The abdomen's unit sphere drawn to a blunt point behind (+y), where the sting is."""
    V = V.copy()
    t = np.clip(V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.5 * t ** 1.6
    V[:, 2] *= 1 - 0.5 * t ** 1.6
    return V
EYE_D = (0.42, -0.9, -0.08)
EYE_HALF = (0.024, 0.032)
WING_ROOT = np.array([0.05, 0.06, 0.322])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along the direction d from its centre, and its normal there."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def board(ob):
    """The game board's check: what the model's corners show turned 31.5 degrees, against the square it is framed in."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def eye_open(iris=EYE_IRIS, pupil=EYE_PUPIL):
    """The line's eye: dark brown, a darker pupil, a large white glint high and outward and a small one low inward."""
    def look(u, v):
        c = np.tile(iris, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.08) / 0.7) ** 2 < 1] = pupil
        c[((u - 0.22) / 0.34) ** 2 + ((v - 0.36) / 0.3) ** 2 < 1] = GLINT
        c[((u + 0.28) / 0.15) ** 2 + ((v + 0.42) / 0.13) ** 2 < 1] = GLINT
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=INK, up=(0, 0, 1)):
    """Both eyes as ovals on a head ellipsoid, +u outward on both."""
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16),
            eps=0.0018, rings=20, segs=56, mirror=True, up=up)


def tuft(k, name, c, d, length, r, colour, bone):
    """A short fat point of down standing out of a surface (fused)."""
    c = np.array(c, float); d = unit(*d)
    k.add(name, tube([c - d * r * 0.4, c + d * length * 0.55, c + d * length], [r, r * 0.6, r * 0.18]), colour, bone)


def wing_outline(length, width, n=40):
    """A wing from its narrow root (a = 0) to a round end (a = length), widest past its middle."""
    pts = []
    for i in range(n):
        ph = 2 * math.pi * i / n
        x = length / 2 * (1 - math.cos(ph))
        y = width * math.sin(ph) * (0.28 + 0.72 * math.sin(ph / 2) ** 1.4)
        pts.append((x, y))
    return pts


def wing_frame(out, normal):
    """The wing's two directions on +X: ex along its length, ez across, its face toward normal."""
    ex = unit(*out)
    n = np.array(normal, float); n = n - ex * (n @ ex); n /= np.linalg.norm(n)
    ez = np.cross(n, ex)
    return ex, ez


def wing(k, name, root, out, normal, length, width, bone="wing{s}", thick=0.005, colour=WING, rim=WINGRIM,
         mirror=True):
    """A pale gauzy wing: a warm rim plate and a paler panel stacked thicker over it (on +X, mirrored)."""
    ex, ez = wing_frame(out, normal)
    k.add(name + "rim", slab(wing_outline(length, width), root, ex, ez, thick), rim, bone, fuse=False, mirror=mirror,
          surface="shell")
    inner = [(length * 0.06 + a * 0.9, b * 0.8) for a, b in wing_outline(length, width)]
    k.add(name, slab(inner, root, ex, ez, thick + 0.0024), colour, bone, fuse=False, mirror=mirror, surface="shell")


def worker(k, name, c, fwd, size, bone="body"):
    """A tiny worker bee (an attendant of the queen's): an amber ball with a brown band, a darker tail, two wings.
    All loose; c its middle, fwd the way its head points, size its body's half length."""
    c = np.array(c, float); f = unit(*fwd)
    s = size
    side = np.cross(f, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.cross(side, f)
    k.add(name + "head", outward(ell(c + f * s * 0.55, (s * 0.62,) * 3, sub=3)), GOLD, bone, fuse=False)
    k.add(name + "body", outward(ell(c - f * s * 0.25, (s * 0.7, s * 0.7, s * 0.7), sub=3)), AMBER, bone, fuse=False)
    k.add(name + "band", ring_band(c - f * s * 0.25, f, s * 0.71, s * 0.42, thick=s * 0.08, nu=37), BAND, bone,
          fuse=False)
    for sx in (1, -1):
        root = c - f * s * 0.1 + up * s * 0.55 + side * sx * s * 0.18
        out = unit(*(up * 0.8 + side * sx * 0.55 - f * 0.25))
        ex, ez = wing_frame(out, -f + side * sx * 0.2)
        if sx < 0:
            ez = -ez
        k.add(name + ("wingR" if sx > 0 else "wingL"), slab(wing_outline(s * 1.3, s * 0.42), root, ex, ez, s * 0.12),
              WING, bone, fuse=False, surface="shell")


def albinut(src, out):
    k = Kit(src, coat="fur")
    # The ball: the head in front, the abdomen behind, both fuzzy
    k.add("head", ell(HC, HR, deform=lumpy(9, 7, 0.022)), GOLD, "head")
    k.add("abdomen", ell(AC, AR, rot=(-6, 0, 0), deform=lambda V: abdomen_shape(lumpy(8, 6, 0.022)(V))), AMBER,
          "body")
    # Short soft points of down round the head's lower edge, so the outline is fuzzy where the picture's is
    rs = np.random.RandomState(7)
    for i in range(13):
        a = math.radians(-72 + 144 * i / 12) + rs.uniform(-0.08, 0.08)
        d = (math.sin(a) * 0.95, -0.25, -math.cos(a) * 0.9 - 0.05)
        p, n = on_ell(HC, HR, d)
        tuft(k, f"downhead{i}", p - n * 0.005, n + np.array([0, 0.1, -0.2]), 0.012 + 0.004 * rs.rand(), 0.0145,
             GOLD, "head")
    # The stripes round the abdomen: an orange band at the head, two broad dark bands with a golden one between
    E = 0.0025
    def ring(y0, y1):
        return lambda P: inside(y0 - P[:, 1], E) * inside(P[:, 1] - y1, E)
    k.paint(ring(0.018, 0.046), ORANGE)
    k.paint(ring(0.046, 0.092), BAND)
    k.paint(ring(0.092, 0.128), "#F0B84E")
    k.paint(ring(0.128, 0.172), BAND)
    # A small open smile with a pink tongue, low between the eyes
    MOUTH_OUT = polygon([(-1, 0.45), (1, 0.45), (0.85, -0.2), (0.45, -0.75), (0, -0.95), (-0.45, -0.75),
                         (-0.85, -0.2)])
    def mouth(u, v):
        c = np.tile(MOUTH, (len(u), 1))
        c[(u / 0.55) ** 2 + ((v + 0.62) / 0.42) ** 2 < 1] = TONGUE
        return c
    mp, mn = on_ell(HC, HR, (0, -0.95, -0.36))
    k.decal(mp, mn, (0.017, 0.013), mouth, eps=0.0018, outline=MOUTH_OUT, rings=8, segs=40)

    # The wax cap on the crown, its edge dripping over the head (loose, crisp)
    top, tn = on_ell(HC, HR, (0, -0.25, 1))
    k.add("wax", outward(ell(top - tn * 0.006, (0.092, 0.084, 0.05), rot=(-14, 0, 0), deform=lumpy(5, 3, 0.1))),
          WAX, "head", fuse=False, surface="shell")
    # Lobes of wax run a little way down from the cap's edge, round and short, their tops under the cap
    for i, (d, r) in enumerate((((0.42, -0.6, 0.72), 0.024), ((-0.3, -0.72, 0.68), 0.021),
                                ((-0.62, 0.05, 0.76), 0.022))):
        p, n = on_ell(HC, HR, d)
        k.add(f"drip{i}", outward(ell(p + n * 0.002 + np.array([0, 0, r * 0.3]), (r, r * 0.9, r * 1.15))), WAX,
              "head", fuse=False, surface="shell")
    # Two short brown antennae out of the cap's sides, curving out and hooking forward
    for sx in (1, -1):
        p0, _ = on_ell(HC, HR, (0.38 * sx, -0.42, 0.82))
        pts = [p0 - (0, 0, 0.006), p0 + (0.018 * sx, -0.006, 0.03), p0 + (0.045 * sx, -0.02, 0.05),
               p0 + (0.066 * sx, -0.04, 0.046), p0 + (0.072 * sx, -0.052, 0.034)]
        k.add("antenna" + ("R" if sx > 0 else "L"), outward(tube(pts, [0.0085, 0.008, 0.0078, 0.0078, 0.0082])), LEG,
              "head", fuse=False)
    # Six stubby brown legs dangling under it
    for i, y in enumerate((-0.07, 0.005, 0.08)):
        pts = [(0.05, y, 0.115), (0.056, y + 0.006, 0.086), (0.06, y + 0.014, 0.064)]
        k.add(f"leg{i}", outward(tube(pts, [0.0135, 0.0128, 0.0135])), LEG, "body", fuse=False, mirror=True)
    # A small tan sting at the abdomen's end
    sb = AC + np.array([0, AR[1] - 0.012, 0.004])
    k.add("sting", hull([sb + (0.018, 0, 0.018), sb + (-0.018, 0, 0.018), sb + (0.018, 0, -0.018),
                         sb + (-0.018, 0, -0.018), sb + (0.022, 0.048, -0.03)]), STING, "body", fuse=False, surface="shell")
    # Two pairs of small gauzy wings: the upper standing up in a V, the lower out and back
    wing(k, "wingU", WING_ROOT, (0.42, 0.38, 0.82), (0.55, -0.8, 0.1), 0.175, 0.058)
    wing(k, "wingD", WING_ROOT + (0.012, 0.012, -0.012), (0.72, 0.55, 0.38), (0.45, -0.55, 0.7), 0.125, 0.042)
    k.build(voxel=0.0042, smooth=4, band=0.006, wband=0.035)
    board(k.ob)

    def eyes(state):
        lay_eyes(k, HC, HR, EYE_D, EYE_HALF, eye_open(), GOLD, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    albinut(*args())
