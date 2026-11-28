# Salamint sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/salamint.py -- <the export's Salamint.glb> <out.glb>
# Carpathia's Fire starter (No. 10004), from its picture (docs/art/reference/gemini/carpathia/10004-Salamint.jpg) and
# its brief (CP-dex-1-plateau.md, "4. Salamint"): a small chubby fire salamander, glossy black with bright yellow-orange
# blotches that glow faintly from inside; a big round frog-like head as wide as the body and half the creature, with a
# wide smile from cheek to cheek, two small nostrils on the snout and large round dark-brown eyes with a glint, high
# on the front of the head; a fat low belly on four short stubby legs with round toes; a thick tail rising from the
# hips and curling up, a glowing coal sitting on its tip. The sparks over the coal are left out (specks).
#
# Written to be grown from (Furnander and Fierander import it with sibling("salamint")): the line's colours, its eye,
# its blotches and cracks, its coal and the board check are the functions below; Salamint's build is under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BLACK = "#29272D"           # the glossy black skin
GOLD = "#F8BC2A"            # the blotches
EMBER = "#E8661A"           # the blotches' glowing rim, the cracks of the later stages
COAL = "#3E241C"            # the coal on the tail
CHIP = "#F07A22"            # the coal's glowing chips
CHIP_GLOW = "#FF8A2A"
SMILE = "#8A8492"           # the smile: a line lighter than the skin, or it is lost
INK, WHITE = srgb("#120E10"), np.array([1.0, 1.0, 1.0])
IRIS = srgb("#9A5C34")      # a warm brown, lighter than the picture's so it reads on black
LIDLINE = srgb("#9A94A0")   # the closed eye's line: light on a black head


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def normal_on(c, r, d):
    p = on_ell(c, r, d) - np.array(c, float)
    n = p / np.array(r, float) ** 2
    return n / np.linalg.norm(n)


def join(*shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(v); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def board(ob):
    """The game's board check: the four-corner sum against the square it frames."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


# --- marks on the skin ----------------------------------------------------------------------------------------------
def blot(p0, t, a, b, edge):
    """A mark about p0: a long axis t of half length a, b across (and in depth): a blotch, a crack's stroke."""
    p0 = np.array(p0, float); t = unit(*t)
    def m(P):
        d = P - p0
        along = d @ t
        rest = np.linalg.norm(d - along[:, None] * t[None], axis=1)
        return inside(np.sqrt((along / a) ** 2 + (rest / b) ** 2) - 1, edge / min(a, b))
    return m


def blotch(k, p0, t, a, b, edge, rim=1.3):
    """A glowing blotch: an orange rim and the gold inside it (Salamint's; the cracks of its evolutions are strokes)."""
    k.paint(blot(p0, t, a * rim, b * rim, edge), EMBER)
    k.paint(blot(p0, t, a, b, edge), GOLD)


def stroke(pts, w, edge):
    """A crack: a thick line through points (a polyline), w its half width. Strokes of a few make a branching crack."""
    pts = np.array(pts, float)
    def m(P):
        best = np.full(len(P), 9.0)
        for a, b in zip(pts[:-1], pts[1:]):
            ab = b - a
            t = np.clip(((P - a) @ ab) / (ab @ ab), 0, 1)
            best = np.minimum(best, np.linalg.norm(P - (a + t[:, None] * ab), axis=1))
        return inside(best - w, edge)
    return m


# --- the eye --------------------------------------------------------------------------------------------------------
def eye_open(iris, rim=0.84, pupil=0.44, glint=(-0.3, 0.32, 0.26), cut=None, glow=None):
    """The open eye (u outward, v up): a dark rim, the iris darkening toward it, a round pupil and a big glint.
    cut: an outline (a brow's) the rim follows; glow: a lighter heart for an eye that glows (Fierander's)."""
    def eye(u, v):
        if cut is not None:
            reach = np.array([max(cut(t), 1e-3) for t in np.arctan2(v, u)])
        else:
            reach = np.ones(len(u))
        rho = np.sqrt(u * u + v * v) / reach
        w = smoothstep(0.35, rim, np.sqrt(u * u + v * v))[:, None]
        c = iris[None] * (1 - 0.35 * w)
        if glow is not None:
            c = c * w + glow[None] * (1 - w)
        r = np.sqrt((u - 0.06) ** 2 + (v + 0.04) ** 2)
        c[r < pupil] = INK
        g = np.sqrt((u - glint[0]) ** 2 + (v - glint[1]) ** 2)
        c[g < glint[2]] = WHITE
        c[rho >= rim] = INK
        return c
    return eye


def brow_cut(BA, BB):
    """An eye's outline cut straight along the brow v = BA + BB u (u outward): its reach at each angle."""
    def cut(th):
        s = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s) if s > 1e-6 else 1.0
    return cut


def lay_eyes(k, E, de, half, look_open, lid, eps, cut=None, fierce=None, line=0.17):
    """The eyes' layer for a state: the right as it is, the left with its picture (and outline) turned over."""
    m = np.array([-1, 1, 1.0])
    def lay(state):
        if state == "Fierce" and fierce is not None:
            look = fierce
        else:
            look = eye_look(state, look_open, lid, LIDLINE, line=line)
        if cut is None:
            k.decal(E, de, half, look, eps=eps, rings=20, segs=56, mirror=True)
        else:
            k.decal(E, de, half, look, eps=eps, rings=20, segs=56, outline=cut)
            k.decal(E * m, de * m, half, lambda u, v: look(-u, v), eps=eps, rings=20, segs=56,
                    outline=lambda th: cut(math.pi - th))
    return lay


# --- the coal -------------------------------------------------------------------------------------------------------
def rock(c, r, n, seed, jitter=0.25):
    """A rough faceted lump: the hull of points on a jittered ball."""
    rs = np.random.RandomState(seed)
    P = rs.normal(size=(n, 3)); P /= np.linalg.norm(P, axis=1, keepdims=True)
    P *= (1 - jitter / 2 + jitter * rs.rand(n))[:, None]
    return hull(np.array(c, float) + P * r)


def coal(k, c, r, bone, seed=7):
    """The glowing coal on the tail's tip: a dark faceted lump with glowing chips set into it, one loose piece each."""
    c = np.array(c, float)
    k.add("coal", rock(c, r, 56, seed, jitter=0.14), COAL, bone, fuse=False, surface="shell")
    rs = np.random.RandomState(seed + 1)
    chips = []
    dirs = [unit(0.3, -0.9, 0.2), unit(-0.6, -0.5, 0.5), unit(0.8, 0.1, 0.5), unit(-0.2, 0.3, 0.95),
            unit(0.5, 0.6, -0.4), unit(-0.85, 0.2, -0.3), unit(0.2, -0.4, -0.85), unit(0.55, -0.55, -0.4)]
    for d in dirs:
        q = c + d * r * 0.84
        chips.append(rock(q, r * (0.2 + 0.07 * rs.rand()), 12, int(rs.randint(1000)), jitter=0.15))
    k.add("coalchips", join(*chips), CHIP, bone, fuse=False, glow=CHIP_GLOW)


# --- Salamint -------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.125, 0.205])           # the head's centre
HR = np.array([0.132, 0.115, 0.088])
B = np.array([0.0, 0.035, 0.13])             # the body's centre
BR = np.array([0.132, 0.14, 0.098])
TAIL = [(0.0, 0.14, 0.13), (0.03, 0.215, 0.115), (0.075, 0.27, 0.14), (0.1, 0.29, 0.185), (0.098, 0.275, 0.218)]
TAIL_R = [0.068, 0.05, 0.037, 0.029, 0.024]
COAL_AT = (0.097, 0.268, 0.244)
VOXEL = 0.0036


def salamint(src, out):
    k = Kit(src, coat="skin")

    # The body: a fat low pear, the belly hanging to just over the ground between short legs
    k.add("body", ell(B, BR), BLACK, "body")
    k.add("belly", ell((0, 0.0, 0.1), (0.118, 0.13, 0.072)), BLACK, "body")
    k.add("rump", ell((0, 0.11, 0.12), (0.11, 0.08, 0.08)), BLACK, "body")

    # The head: big and round, as wide as the body, a broad flat snout and a full jaw under the smile; the throat one
    # mass with the chest
    def frog(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.8, V[:, 2])
        V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 1], 0, 1)
        return V
    k.add("head", ell(H, HR, deform=frog), BLACK, "head")
    k.add("jaw", ell((0, -0.13, 0.165), (0.126, 0.108, 0.06)), BLACK, "head")
    k.add("throat", ell((0, -0.07, 0.14), (0.115, 0.08, 0.075)), BLACK, "head")
    # The mounds the eyes sit on, frog-like, high on the front of the head
    de = unit(0.52, -0.66, 0.54)
    E = on_ell(H, HR, de)
    k.add("mound", ell(E - de * 0.016, (0.03, 0.03, 0.028)), BLACK, "head", mirror=True)

    # The legs: short and stubby, splayed a little, a fat upper leg fused into the flank, round toes
    for s, side, bone, y in ((1, "R", "leg", -0.075), (-1, "L", "leg", -0.075), (1, "R", "hind", 0.12),
                             (-1, "L", "hind", 0.12)):
        m = np.array([s, 1, 1.0])
        k.add(f"{bone}up{side}", ell(np.array((0.098, y, 0.088)) * m, (0.046, 0.05, 0.05)), BLACK, f"{bone}{side}")
        k.add(f"{bone}{side}", tube(np.array([(0.1, y, 0.08), (0.112, y - 0.008, 0.045), (0.115, y - 0.012, 0.03)]) * m,
                                    [0.034, 0.031, 0.029]), BLACK, f"{bone}{side}")
        k.add(f"{bone}foot{side}", ell(np.array((0.116, y - 0.02, 0.011)) * m, (0.036, 0.038, 0.012)), BLACK,
              f"{bone}{side}")
        for j, (dx, dy) in enumerate(((-0.024, -0.03), (0.0, -0.038), (0.026, -0.03))):
            k.add(f"{bone}toe{side}{j}", ell(np.array((0.116 + dx, y - 0.02 + dy, 0.01)) * m, (0.012, 0.013, 0.01)),
                  BLACK, f"{bone}{side}")

    # The tail: thick at the hips, rising and curling up on the +X side to the coal, no higher than the head
    k.add("tail", tube(TAIL, TAIL_R), BLACK, "tail")
    coal(k, COAL_AT, 0.04, "tail")

    # The blotches: few, large and long, the largest a fifth of the body's height, orange-rimmed; round the back too
    e = 0.0016
    for d, t, a, b in [((0.35, 0.05, 0.94), (0.1, 1, 0), 0.03, 0.016),      # along the back, two rows
                       ((-0.4, 0.3, 0.87), (0, 1, 0.3), 0.026, 0.015),
                       ((0.42, 0.55, 0.72), (0.2, 1, -0.3), 0.022, 0.014),
                       ((-0.3, -0.35, 0.89), (0, 1, 0), 0.02, 0.013),
                       ((0.85, -0.2, 0.45), (0, 1, -0.6), 0.024, 0.014),     # the flanks
                       ((-0.88, 0.15, 0.42), (0, 1, -0.4), 0.025, 0.014),
                       ((0.75, 0.6, 0.2), (0, 0.6, -1), 0.02, 0.012),
                       ((-0.7, 0.65, 0.25), (0, 0.6, -1), 0.018, 0.011),
                       ((0.05, 0.85, 0.52), (1, 0, 0.2), 0.02, 0.012)]:     # the rump
        blotch(k, on_ell(B, BR, d), t, a * 1.3, b, e)
    # on the head: on its crown and back, an eye's width from the eyes
    for d, t, a, b in [((0.0, 0.45, 0.9), (0, 1, 0), 0.02, 0.012),
                       ((0.3, 0.75, 0.6), (0.3, 1, 0), 0.015, 0.01),
                       ((-0.33, 0.7, 0.62), (-0.3, 1, 0), 0.014, 0.01),
                       ((0.86, 0.35, 0.36), (0, 1, 0.4), 0.014, 0.009),
                       ((-0.88, 0.3, 0.36), (0, 1, 0.4), 0.013, 0.009)]:
        blotch(k, on_ell(H, HR, d), t, a, b, e)
    # on the legs and the tail
    for p0, t, a, b in [((0.128, -0.072, 0.09), (0, 0.2, -1), 0.02, 0.012),
                        ((-0.13, 0.12, 0.085), (0, 0.2, -1), 0.018, 0.011),
                        ((0.1, 0.25, 0.165), (0.3, 0.6, 1), 0.015, 0.01),
                        ((0.06, 0.2, 0.16), (0.3, 1, -0.2), 0.016, 0.01)]:
        blotch(k, p0, t, a, b, e)

    # The smile: from cheek to cheek, low on the face, its corners turned up; a line lighter than the skin
    hy = H[1]
    def smile(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - hy))
        z = 0.176 + 0.016 * a * a
        return inside(np.abs(P[:, 2] - z) - 0.0036, 0.0012) * inside(np.abs(a) - 1.22, 0.04) * (P[:, 1] < hy)
    k.paint(smile, SMILE)
    # The nostrils: two small dots on the snout's top front
    k.decal(on_ell(H, HR, (0.17, -0.94, 0.3)), unit(0.17, -0.85, 0.5), (0.0065, 0.005), SMILE, eps=0.002,
            rings=4, segs=16, mirror=True)

    # The eyes: large, round, dark brown with a big glint, set on their mounds
    E2 = E + de * 0.012
    lay = lay_eyes(k, E2, de, (0.029, 0.03), eye_open(IRIS), srgb(BLACK), eps=0.0018, line=0.18)

    ob = k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.02)
    board(ob)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    salamint(*args())
