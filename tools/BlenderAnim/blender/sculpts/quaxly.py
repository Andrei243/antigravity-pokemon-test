# Quaxly sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/quaxly.py -- <the export's Quaxly.glb> <out.glb>
# A white duckling whose head and body are one mass, under a great rounded cap of sky-blue "hair" that is wider than
# the head and overhangs its brow, with a white wave across its front and a lock hanging at its left side; tall oval
# blue eyes just under the cap's edge; a broad flat yellow bill, darker below; small white wings held down and a
# little forward; short sky-blue legs on broad webbed feet.
#
# Written to be grown from (Quaxwell and Quaquaval import it with sibling("quaxly")): the line's white, its eyes and
# its bill are the functions below; the calls that build Quaxly are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, outward, axes, polygon, srgb, inside, smoothstep, tri, unit,
                       eye_look, args)

# --- the line's colours ---------------------------------------------------------------------------------------------
WHITE, TEAL, FOOT, BEAK, LIP = "#F3F6FA", "#3DB7D4", "#4CC3D8", "#F6C83A", "#D9AE2C"
INK = srgb("#141A33")
IRIS = srgb("#2E70D4")
PUPIL = srgb("#132A6A")
GLINT = np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def eye_open(iris=IRIS, pupil=PUPIL, rim=INK, rim_w=0.2, pupil_c=(0.0, -0.05), pupil_r=(0.42, 0.55),
             glint=(-0.22, 0.38, 0.22), lid=None, lid_line=None):
    """The line's open eye (u outward, v up, -1..1): an ink rim, a blue iris, a dark pupil, a white glint. lid: a
    colour and a slope (top edge v = a + b u) for an eye whose top is cut by a heavy lid (Quaxwell, Quaquaval)."""
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(iris, (len(u), 1))
        c[((u - pupil_c[0]) / pupil_r[0]) ** 2 + ((v - pupil_c[1]) / pupil_r[1]) ** 2 < 1] = pupil
        if glint is not None:
            c[(u - glint[0]) ** 2 + (v - glint[1]) ** 2 < glint[2] ** 2] = GLINT
        c[r > 1 - rim_w] = rim
        if lid is not None:
            a, b = lid_line
            edge = a + b * u
            c[v > edge] = lid
            c[np.abs(v - edge) < rim_w * 0.7] = rim
        return c
    return eye


def lay_eyes(k, state, E, de, half, look_open, lid, ink, outline=None, eps=0.003, line=0.16):
    """Both eyes for a state: a patch a side at E (and its mirror) facing de, u outward on both."""
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        c, n = np.array(E, float) * m, np.array(de, float) * m
        look = eye_look(state, look_open, lid, ink, line=line)
        if side == -1:
            look = (lambda f: (lambda u, v: f(-u, v)))(look)
        ol = None
        if outline is not None:
            ol = polygon([(side * u, v) for u, v in (outline if side == 1 else outline[::-1])])
        k.decal(c, n, half, look, eps=eps, outline=ol, rings=20, segs=56)


def webbed(V):
    """A foot's deform: three rounded toes along its front edge."""
    ph = np.arctan2(V[:, 0], -V[:, 1])
    front = np.clip(-V[:, 1], 0, 1)
    s = 1 + 0.15 * np.cos(3 * ph) * front
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V


def plume(base, d, length, width, thick, across=None, roll=0.0, root=0.5):
    """A feather of the grown line (Quaxwell's, Quaquaval's): a flat pointed plate with a hard rim from base along d,
    widest a third of the way, its face across `across` (by default square to d and to the model's length, so the
    front and back cameras see its faces), turned about d by roll degrees."""
    d = unit(*d)
    ex = np.cross(d, (0, 1.0, 0)) if across is None else np.array(across, float)
    if np.linalg.norm(ex) < 1e-3:
        ex = np.array([1.0, 0, 0])
    ex = ex - d * (ex @ d); ex /= np.linalg.norm(ex)
    if roll:
        a = math.radians(roll)
        ex = ex * math.cos(a) + np.cross(d, ex) * math.sin(a)
    ts = np.linspace(0, 1, 15)[:-1]
    w = width * (root + (1 - root) * np.sin(math.pi * np.clip(ts * 1.4, 0, 1) * 0.5)) * (1 - ts) ** 0.7
    right = [(wi, t * length) for wi, t in zip(w, ts)]
    outline = right + [(0.0, length)] + [(-a, b) for a, b in right[::-1]]
    return slab(outline, base, ex, d, thick)


def drop(c, d, length, width, thick, across=None):
    """A teardrop plate (Quaquaval's water feathers): round at c's end, pointed along d."""
    d = unit(*d)
    ex = np.cross(d, (0, 1.0, 0)) if across is None else np.array(across, float)
    if np.linalg.norm(ex) < 1e-3:
        ex = np.array([1.0, 0, 0])
    ex = ex - d * (ex @ d); ex /= np.linalg.norm(ex)
    ang = np.linspace(0, 2 * math.pi, 28, endpoint=False)
    pts = []
    for a in ang:
        x, y = math.cos(a), math.sin(a)             # y along d
        if y > 0:
            x *= (1 - y) ** 0.9
        pts.append((x * width, (y * (length - width) if y > 0 else y * width)))
    return slab(pts, c, ex, d, thick)


def pointed_beak(V):
    """The grown line's beak: drawn to a point toward the front (-Y)."""
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.72 * t; V[:, 2] *= 1 - 0.6 * t
    return V


def bill(V):
    """A duck's bill: flat, broad and rounded at the front, drawn narrower toward the face."""
    V = V.copy()
    t = np.clip(V[:, 1], 0, 1)          # toward the back
    V[:, 0] *= 1 - 0.25 * t
    V[:, 2] *= 1 - 0.3 * np.clip(-V[:, 1], 0, 1) ** 2
    return V


# --- Quaxly's frame -------------------------------------------------------------------------------------------------
VOXEL = 0.0045
H = np.array([0.0, -0.006, 0.27])            # the head's centre (white, one mass with the body)
HR = np.array([0.095, 0.086, 0.09])
CAP = np.array([0.0, 0.016, 0.365])          # the cap of hair
CAPR = np.array([0.132, 0.122, 0.086])
DE = unit(0.42, -0.9, 0.06)                  # where the eyes look out
EYE_HALF = (0.0185, 0.031)


def quaxly(src, out):
    k = Kit(src, coat="fur")

    # The body and head, one white mass: a pear with the head as wide as the body
    k.add("body", ell((0, 0.006, 0.118), (0.076, 0.072, 0.074)), WHITE, "body")
    k.add("chest", ell((0, -0.03, 0.14), (0.055, 0.045, 0.055)), WHITE, "body")
    k.add("neck", ell((0, -0.002, 0.2), (0.082, 0.076, 0.062)), WHITE, "head")
    k.add("head", ell(H, HR), WHITE, "head")
    k.add("tail", ell((0, 0.07, 0.1), (0.032, 0.03, 0.022), rot=(-25, 0, 0)), WHITE, "body")

    # The cap of hair: wider than the head, overhanging the brow, a lock hanging at the left side behind
    k.add("cap", ell(CAP, CAPR), TEAL, "head", surface="hair")
    k.add("caplock", tube([(-0.1, 0.03, 0.34), (-0.128, 0.018, 0.305), (-0.132, -0.002, 0.282)],
                          [0.04, 0.03, 0.02]), TEAL, "head", surface="hair")
    k.add("capback", ell((0, 0.06, 0.34), (0.11, 0.08, 0.06)), TEAL, "head", surface="hair")

    # The white wave across the cap's front: a line dipping twice
    def wave(P):
        s = P[:, 0] / 0.055
        t0 = 0.418 + 0.01 * np.abs(np.cos(math.pi * s))
        return (inside(np.abs(P[:, 2] - t0) - 0.0078, 0.0018) * inside(np.abs(s) - 1, 0.03)
                * inside(P[:, 1] + 0.02))
    k.paint(wave, WHITE)

    # Small white wings held down and a little forward
    k.add("wing", ell((0.098, -0.012, 0.118), (0.026, 0.044, 0.062), rot=(14, -24, 8)), WHITE, "wing{s}", mirror=True)
    k.add("wingtip", ell((0.118, -0.03, 0.072), (0.02, 0.03, 0.028), rot=(14, -24, 8)), WHITE, "wing{s}", mirror=True)

    # Short sky-blue legs on broad webbed feet
    k.add("leg", tube([(0.036, 0.0, 0.07), (0.04, -0.004, 0.03)], [0.018, 0.017]), FOOT, "leg{s}", mirror=True)
    k.add("foot", ell((0.046, -0.032, 0.012), (0.036, 0.052, 0.012), rot=(0, 0, 12), deform=webbed), FOOT, "leg{s}",
          mirror=True, surface="skin")

    # The bill: broad and flat, its lower half darker
    k.add("bill", ell((0, -0.112, 0.222), (0.062, 0.054, 0.018), rot=(-8, 0, 0), deform=bill), BEAK, "head",
          fuse=False, surface="shell")
    k.add("billlow", ell((0, -0.104, 0.208), (0.056, 0.048, 0.015), rot=(-4, 0, 0), deform=bill), LIP, "head",
          fuse=False, surface="shell")

    k.build(voxel=VOXEL, band=0.005, wband=0.03)

    E = on_ell(H, HR, DE)
    look = eye_open()
    k.eye_states(lambda state: lay_eyes(k, state, E, DE, EYE_HALF, look, srgb(WHITE), INK))
    k.export(out)


if __name__ == "__main__":
    quaxly(*args())
