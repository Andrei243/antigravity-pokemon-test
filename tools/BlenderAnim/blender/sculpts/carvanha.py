# Carvanha sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/carvanha.py -- <the export's Carvanha.glb> <out.glb>
#
# From the references: a round piranha, dark blue over its top and crimson red under it, the two meeting in a jaw line
# whose red points rise into the blue round the front; two bold white fangs a side hanging from the blue upper jaw.
# Angry eyes on the sides toward the front: a white eye whose top slopes down toward the snout, a red iris, a black
# pupil, a black socket behind. A yellow star low on each red cheek. Yellow jagged fins: a pair of lightning-shaped
# crests splayed in a V on the top, a long pointed fin out of each side, a jagged fin under the belly and a fan of a tail
# with a red leading edge (the midline fins turned to the +X side, kept lower than the ball is tall).
#
# Written to be grown from (Sharpedo: sibling("carvanha")): the colours, the eye, the fang, the star and the jagged
# fin are the functions and constants below.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, slab, hull, srgb, inside, unit, eye_look, polygon, tri, args

BLUE, RED, YELLOW, WHITE, INK, IRIS, LIGHT = "#2C4E9E", "#D2324E", "#F4D04A", "#F6F6F2", "#14131A", "#D8283C", "#8EA6DE"

C = np.array([0.0, 0.0, 0.256])          # the ball's middle, on the body bone
R = np.array([0.125, 0.128, 0.13])
LINE = -0.012                            # the jaw line's floor under the middle
TEETH_H, TEETH_P = 0.042, math.radians(30)


def azimuth(P):
    """The angle round the upright axis, nought straight ahead (-Y), toward +X positive."""
    return np.arctan2(P[:, 0] - C[0], -(P[:, 1] - C[1]))


def line_z(P):
    """The jaw line's height at each point's angle: red points rising round the front, smooth behind."""
    a = azimuth(P)
    w = (tri(a / TEETH_P) + 1) / 2                       # 0 in the valleys (straight ahead is one), 1 at the points
    fade = 1 - np.clip((np.abs(a) - math.radians(95)) / math.radians(25), 0, 1)
    return C[2] + LINE + TEETH_H * w * fade


def on_ball(a, z, c=C, r=R, sink=0.0):
    """The ball's surface at angle a round the upright axis and height z, and its outward normal."""
    s = np.clip((z - c[2]) / r[2], -0.999, 0.999)
    ca = math.sqrt(1 - s * s)
    d = np.array([math.sin(a) * ca, -math.cos(a) * ca, s])
    p = c + d * r * (1 - sink)
    n = d / r; n = n / np.linalg.norm(n)
    return p, n


def fang(k, name, p, n, down, length, width, thick, bone="body"):
    """A bold white fang: a solid kite of a section from a root sunk in the jaw, pointing down."""
    n = np.array(n, float); down = np.array(down, float)
    side = np.cross(n, down); side /= np.linalg.norm(side)
    root = p - n * 0.006 + np.array([0, 0, 0.008])
    pts = [root + side * width / 2, root - side * width / 2, root + n * thick / 2, root - n * thick * 0.5,
           root + down * length * 0.45 + side * width * 0.3 + n * 0.008, root + down * length * 0.45 - side * width * 0.3 + n * 0.008,
           root + down * length * 0.45 + n * (0.008 + thick * 0.4),
           root + down * length + n * 0.014]
    k.add(name, hull(pts), WHITE, bone, fuse=False, surface="shell")


def star(n_points=5, inner=0.45, turn=90.0):
    pts = []
    for i in range(2 * n_points):
        r = 1.0 if i % 2 == 0 else inner
        t = math.radians(turn) + i * math.pi / n_points
        pts.append((r * math.cos(t), r * math.sin(t)))
    return pts


def turned(pts, deg):
    t = math.radians(deg)
    return [(a * math.cos(t) - b * math.sin(t), a * math.sin(t) + b * math.cos(t)) for a, b in pts]


def ball(k):
    k.add("ball", ell(C, R, sub=5), BLUE, "body")
    # The lower jaw a little fuller than the skull, so the red half juts as a piranha's does
    k.add("jaw", ell(C + np.array([0.0, -0.012, -0.03]), R * np.array([0.98, 0.98, 0.9]), sub=5), BLUE, "body")
    k.paint(lambda P: inside(P[:, 2] - line_z(P), 0.0022), RED)


def fangs(k):
    for i, deg in enumerate((30, 60)):
        for s in (1, -1):
            a = math.radians(deg) * s
            p, n = on_ball(a, C[2] + LINE + 0.004, sink=0.02)
            down = unit(0, 0, -1) + n * 0.25
            fang(k, f"fang{i}{'R' if s > 0 else 'L'}", p, n, down / np.linalg.norm(down), 0.062 - 0.01 * i,
                 0.046 - 0.006 * i, 0.026)


def stars(k):
    for s in (1, -1):
        p, n = on_ball(math.radians(62) * s, C[2] - 0.072)
        k.decal(p, n, (0.034, 0.034), YELLOW, outline=polygon(turned(star(), -12 * s)), eps=0.004, rings=12, segs=90)


def jagged(k, name, outline, origin, ex, ez, thick, color, bone):
    k.add(name, slab(outline, origin, ex, ez, thick), color, bone, fuse=False, surface="scales")


CREST = [(-0.045, 0.0), (-0.004, 0.085), (0.035, 0.16), (0.03, 0.1), (0.07, 0.112), (0.045, 0.06), (0.085, 0.064),
         (0.055, 0.0)]
TAIL = [(-0.02, 0.03), (0.05, 0.1), (0.11, 0.15), (0.1, 0.1), (0.13, 0.075), (0.1, 0.035), (0.135, 0.005),
        (0.1, -0.035), (0.12, -0.075), (0.06, -0.055), (-0.02, -0.025)]
TAIL_EDGE = [(-0.02, 0.03), (0.05, 0.1), (0.11, 0.15), (0.096, 0.152), (0.043, 0.112), (-0.025, 0.045)]
PEC = [(0.0, 0.03), (0.06, 0.024), (0.17, 0.0), (0.075, -0.022), (0.0, -0.025)]
BELLY = [(-0.035, 0.0), (-0.012, -0.07), (0.01, -0.125), (0.022, -0.078), (0.052, -0.09), (0.042, -0.042),
         (0.062, -0.04), (0.045, 0.0)]


def fins(k):
    # The crests: a pair splayed in a V on the top, their faces turned toward the front
    for s in (1, -1):
        o = np.array([0.025 * s, -0.02, 0.36])
        ex = unit(1.0 * s, 0.55, 0.0)
        ez = unit(0.42 * s, 0.0, 1.0)
        pts = CREST if s > 0 else [(a, b) for a, b in CREST]
        jagged(k, f"crest{'R' if s > 0 else 'L'}", pts, o, ex, ez, 0.022, YELLOW, "body")
    # The tail: a fan out of the back, turned some 37 degrees to +X, its leading edge red
    o = np.array([0.0, 0.1, 0.262])
    ex, ez = unit(0.6, 0.8, 0.0), (0.0, 0.0, 1.0)
    jagged(k, "tailfin", TAIL, o, ex, ez, 0.022, YELLOW, "tail")
    jagged(k, "tailedge", TAIL_EDGE, o, ex, ez, 0.03, RED, "tail")
    # A long pointed fin out of each side, back and out, its face to the front
    for s in (1, -1):
        o = np.array([0.1 * s, -0.02, 0.236])
        jagged(k, f"pec{'R' if s > 0 else 'L'}", PEC, o, unit(0.78 * s, 0.6, -0.12), (0.0, -0.15, 1.0), 0.022, YELLOW,
               f"fin{'R' if s > 0 else 'L'}")
    # The jagged fin under the belly, hanging down and back, turned to +X
    jagged(k, "bellyfin", BELLY, np.array([0.0, 0.0, 0.145]), unit(0.6, 0.8, 0.0), (0.0, 0.0, 1.0), 0.022, YELLOW,
           "body")


# --- the eyes: white, their top sloping down toward the snout, a red iris, a black pupil, a black socket behind ------
EYE_A, EYE_Z = math.radians(44), C[2] + 0.056
EYE_HALF = (0.036, 0.031)
# u toward the back, v up: the top runs from the high back corner down to the front
EYE = [(1.0, 0.8), (1.0, -0.45), (0.6, -0.92), (-0.15, -0.98), (-0.75, -0.72), (-1.0, -0.25), (-0.92, 0.05)]
CW, CK, CI, CB, CL = srgb(WHITE), srgb(INK), srgb(IRIS), srgb(BLUE), srgb(LIGHT)


def radial(pts):
    poly = polygon(pts)
    def frac(u, v):
        th = np.arctan2(v, u)
        Rr = np.array([poly(t) for t in th])
        return np.sqrt(u * u + v * v) / np.maximum(Rr, 1e-6)
    return frac


EYE_FRAC = radial(EYE)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.12) / 0.5) ** 2 + ((v + 0.12) / 0.6) ** 2 < 1] = CI
    c[(u > 0.55) | (EYE_FRAC(u, v) > 0.82)] = CK
    return c


def side_patch(k, a, z, half, look, outline=None, side=1, eps=0.004, rings=20, segs=56, c=C, r=R):
    """A patch on the side at angle a (toward +X for side 1), u toward the back on both sides."""
    p, n = on_ball(a * side, z, c, r)
    if side < 0:
        o = None if outline is None else polygon([(-x, y) for x, y in outline])
        col = (lambda u, v: look(-u, v)) if callable(look) else look
    else:
        o = None if outline is None else polygon(outline)
        col = look
    k.decal(p, n, half, col, eps=eps, outline=o, rings=rings, segs=segs)


def eyes(k, state):
    ink = CK if state in ("Open", "Fierce") else CL           # a light line on the dark blue when shut
    for s in (1, -1):
        side_patch(k, EYE_A, EYE_Z, EYE_HALF, eye_look(state, eye_open, CB, ink, line=0.16), outline=EYE, side=s)
        if state in ("Open", "Fierce"):
            # The pupil, a patch of its own over the iris; moved forward by its angle (u toward the back)
            side_patch(k, EYE_A - 0.12 * EYE_HALF[0] / R[0], EYE_Z - 0.12 * EYE_HALF[1], (0.009, 0.011), INK, side=s,
                       eps=0.0055, rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    ball(k)
    stars(k)
    fangs(k)
    fins(k)
    k.build(voxel=0.004, smooth=3, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
