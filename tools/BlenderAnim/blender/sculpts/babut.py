# Babut sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/babut.py -- <the export's Babut.glb> <out.glb>
#
# The design, from its picture (docs/art/reference/gemini/carpathia/10080-Babut.jpg) and brief (CP-dex-2-mountains.md,
# "80. Babut: a pebble"): a small round weather-worn grey stone, a little wider than tall and widest low down, its
# outline gently lumpy. The wind has worn a face into its front: two large shallow hollows for eyes, a dark rim round
# each and a paler floor inside, and a long crease of a mouth across the lower face with a short crease of a lip under
# it. Lichen grows in yellow blots dotted orange: a large cluster on the top toward +X, others on the top's other side,
# down the right side, low on the front and all round the back. Two small round pebble feet under its front.
# The first of its line (Babut, Babelith, Babelossus): the stone's palette, the lichen and the hollow eye are here
# and the evolutions take them through sibling("babut").
#
# Skeleton: root, body (0, 0, 0.16), head (0, 0, 0.2), legL/legR (+-0.07, -0.02, 0.06). The stone on body, the face's
# swell on head, a foot on each leg.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, smoothstep, unit, polygon, facets, boxy, eye_look, args

# --- the line's palette -----------------------------------------------------------------------------------------------
STONE, STONE_DK, RIM, HOLLOW, FLOOR = "#9C9C97", "#83837E", "#454543", "#62625F", "#8C8C87"
CRACK = "#62625E"
LICHEN, LICHEN_DOT = "#E3B520", "#E27A1C"

# --- the frame --------------------------------------------------------------------------------------------------------
C = np.array([0.0, -0.002, 0.188])       # the stone's middle
R = np.array([0.166, 0.13, 0.172])       # its half sizes: bottom about 0.035, top 0.355, sitting up on its feet
EYE_X, EYE_Z = 0.06, 0.218
EYE_HALF = (0.031, 0.026)
MOUTH_Z, MOUTH_HALF = 0.155, (0.098, 0.022)


def stone_shape(V):
    """The unit sphere worn into the pebble: widest low down, the front a little flatter, a few soft facets."""
    V = boxy(2.5)(V)
    V = facets(16, 0.38, 801, keep_front=0.55, depth=(0.86, 0.97))(V)
    V = V.copy()
    s = 1.0 - 0.05 * V[:, 2] - 0.03 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s
    V[:, 1] *= s * np.where(V[:, 1] < 0, 0.93, 1.0)
    V[:, 2] *= np.where(V[:, 2] < 0, 0.9, 1.0)
    return V


def on_stone(d, c=C, r=R):
    """A point of the stone's ellipsoid along direction d from its middle, and the outward normal there."""
    d = unit(*d)
    p = c + d * r
    n = (p - c) / (r * r)
    return p, n / np.linalg.norm(n)


def blob(rs, n=10):
    """A lichen blot's outline: a jittered polygon round its middle."""
    th = np.linspace(0, 2 * math.pi, n + 1)[:-1] + rs.uniform(0, 1)
    r = rs.uniform(0.45, 1.0, n)
    return polygon(np.stack([np.cos(th) * r, np.sin(th) * r], 1))


def lichen(k, at, n, size, rs, eps, dots=3, satellites=3):
    """A lichen cluster: a yellow blot, a few smaller blots round it and orange dots on the blot (each its own patch)."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(n, a)
    k.decal(at, n, (size, size * rs.uniform(0.7, 0.95)), LICHEN, eps=eps, outline=blob(rs), rings=6, segs=28,
            up=b if abs(n[2]) > 0.9 else (0, 0, 1))
    for _ in range(satellites):
        t = rs.uniform(0, 2 * math.pi)
        off = (a * math.cos(t) + b * math.sin(t)) * size * rs.uniform(1.35, 1.8)
        s = size * rs.uniform(0.25, 0.4)
        k.decal(at + off, n, (s, s * 0.85), LICHEN, eps=eps, outline=blob(rs, 6), rings=4, segs=20,
                up=b if abs(n[2]) > 0.9 else (0, 0, 1))
    for _ in range(dots):
        t = rs.uniform(0, 2 * math.pi)
        off = (a * math.cos(t) + b * math.sin(t)) * size * rs.uniform(0.15, 0.5)
        s = size * rs.uniform(0.15, 0.23)
        k.decal(at + off, n, (s, s), LICHEN_DOT, eps=eps * 1.5, rings=4, segs=20,
                up=b if abs(n[2]) > 0.9 else (0, 0, 1))


def crack(P, pts, half, edge):
    """A paint mask along a polyline of points: a crack worn into the stone."""
    m = np.zeros(len(P))
    pts = np.array(pts, float)
    for a, b in zip(pts[:-1], pts[1:]):
        ab = b - a
        t = np.clip(((P - a) @ ab) / (ab @ ab), 0, 1)
        d = np.linalg.norm(P - (a + t[:, None] * ab), axis=1)
        m = np.maximum(m, inside(d - half, edge))
    return m


def hollow_eye(rim=RIM, hollow="#73736E", floor="#5A5A56", light="#C4C4BE"):
    """A hollow worn for an eye: its wall a shade under the stone, its floor darker still, the upper rim casting a
    soft dark crescent down into it, and a small pale catch-light low in it. No ring round it."""
    cr, ch, cf, cl = srgb(rim), srgb(hollow), srgb(floor), srgb(light)
    def look(u, v):
        c = np.tile(ch, (len(u), 1))
        f = smoothstep(1.0, 0.55, np.sqrt((u / 0.8) ** 2 + ((v + 0.08) / 0.75) ** 2))
        c = c * (1 - f[:, None]) + cf[None] * f[:, None]
        # the crescent the upper rim casts, soft along its lower edge
        shade = smoothstep(0.05, 0.4, v - 0.3 * (1 - u * u) + 0.25)
        c = c * (1 - shade[:, None]) + cr[None] * shade[:, None]
        c[((u - 0.16) / 0.24) ** 2 + ((v + 0.5) / 0.14) ** 2 < 1] = cl
        return c
    return look


def babut(src, dest):
    k = Kit(src, coat="shell")
    rs = np.random.RandomState(80)

    # The stone, the face's gentle swell on the head bone, two pebble feet
    k.add("stone", ell(C, R, deform=stone_shape, sub=5), STONE, "body")
    k.add("face", ell((0, -0.05, 0.196), (0.122, 0.085, 0.11)), STONE, "head")
    k.add("foot", ell((0.066, -0.05, 0.022), (0.029, 0.031, 0.023)), STONE_DK, "leg{s}", mirror=True)

    # A crack down the +X cheek and two over the back
    k.paint(lambda P: crack(P, [(0.108, -0.105, 0.31), (0.124, -0.088, 0.26), (0.118, -0.098, 0.21)], 0.0025, 0.002),
            CRACK, on=["stone", "face"])
    k.paint(lambda P: crack(P, [(-0.05, 0.12, 0.3), (-0.02, 0.15, 0.23), (-0.05, 0.155, 0.15)], 0.0025, 0.002), CRACK,
            on=["stone"])
    k.paint(lambda P: crack(P, [(0.08, 0.13, 0.13), (0.12, 0.11, 0.08)], 0.0025, 0.002), CRACK, on=["stone"])

    # Lichen: the largest cluster on the top toward +X, the rest all round, the back too
    for d, size in [((0.42, -0.05, 0.9), 0.04), ((-0.45, -0.25, 0.86), 0.028), ((0.92, -0.05, 0.3), 0.026),
                    ((0.95, 0.15, -0.15), 0.022), ((0.38, -0.82, -0.45), 0.026), ((-0.96, -0.22, -0.12), 0.02),
                    ((0.25, 0.9, 0.35), 0.032), ((-0.55, 0.78, -0.05), 0.026), ((0.6, 0.7, -0.4), 0.022),
                    ((-0.15, 0.55, 0.82), 0.026)]:
        at, n = on_stone(d)
        lichen(k, at, n, size * 1.25, rs, eps=0.0028)

    # The mouth: a long crease, its ends lifting a little, and a short crease of a lip under it
    ci, cs = srgb(CRACK), srgb(STONE)
    def mouth(u, v):
        c = np.tile(cs, (len(u), 1))
        line = -0.12 + 0.42 * u * u
        c[(np.abs(v - line) < 0.24 * (1 - 0.45 * u * u)) & (np.abs(u) < 0.94)] = ci
        return c
    k.decal((0, -0.138, MOUTH_Z), unit(0, -1, 0.05), MOUTH_HALF, mouth, eps=0.0025, rings=24, segs=96)
    def lip(u, v):
        c = np.tile(cs, (len(u), 1))
        c[(np.abs(v + 0.1 * u * u) < 0.32) & (np.abs(u) < 0.9)] = srgb(STONE_DK)
        return c
    k.decal((-0.01, -0.132, MOUTH_Z - 0.026), unit(0, -1, -0.2), (0.048, 0.012), lip, eps=0.0022, rings=10, segs=48)

    k.build(voxel=0.004, smooth=3)
    board(k.ob)

    look = hollow_eye()
    def eyes(state):
        for s in (1, -1):
            dn = unit(s * 0.4, -0.91, 0.08)
            e = np.array([s * EYE_X, -0.14, EYE_Z])
            f = eye_look(state, look, srgb(STONE), srgb(RIM), line=0.17)
            lk = f if s > 0 else (lambda g: (lambda u, v: g(-u, v)))(f)
            k.decal(e, dn, EYE_HALF, lk, eps=0.0032, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(dest)


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


if __name__ == "__main__":
    babut(*args())
