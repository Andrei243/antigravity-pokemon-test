# Stiucuta sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stiucuta.py -- <the export's Stiucuta.glb> <out.glb>
#
# Carpathia No. 10151, a young pike (plan 46, CP-dex-3b). From its picture: a slim green fish, its back green with
# pale wavy bars running down the upper flank and leaning back toward the tail, the lower third cream from the bill's
# underside to the tail's root. The head a third of the length, ending in a flat little duck-bill wider than it is
# deep, the upper lip a rounded lobe over a cream lower one, a dark mouth line between them. A big round amber eye with
# a dark rim and a dark pupil looking forward, high on the cheek behind the bill. Teal fins with darker wavy streaks:
# a pectoral pair behind the gill, a pelvic pair under the belly, the dorsal and the anal set far back, a forked tail.
# The midline fins turn 40 degrees to +X so the game's cameras see their faces; the eyes come forward on the cheeks.
#
# Written to be grown from (stiucon.py imports it with sibling): the frame is in the helpers that take the kit.
# Joints: body (0, 0, 0.144), head (0, -0.16, 0.144), dorsal (0, 0.12, 0.204), tail (0, 0.22, 0.144),
# tailFin (0, 0.29, 0.144), finR/L (+-0.045, -0.08, 0.104).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, args

GREEN, CREAM, BAR, FIN, STREAK, AMBER, INK, WHITE, MOUTH = ("#4E9A5C", "#EDE6C2", "#CFDDA2", "#86CCB4", "#4F9C88",
                                                            "#D8A23C", "#1C1E16", "#FAFAF2", "#2C4A30")
Z0 = 0.144


def body_tube(pts, radii, flat=0.74, seg=28):
    """A round tube squashed from side to side about the midline: a pike is narrower than deep."""
    V, F = tube(pts, radii, seg=seg)
    V[:, 0] *= flat
    return V, F


def yaw_ex(deg):
    """Along a midline plate, turned deg degrees out of the body's plane to +X."""
    a = math.radians(deg)
    return (math.sin(a), math.cos(a), 0.0)


def fin(k, name, outline, origin, ex, ez, bone, color=FIN, streak=STREAK, thick=0.007, mirror=False, vein=0.55):
    """A fin: a teal plate, and inside it a narrower darker plate standing proud of both faces (its streak)."""
    k.add(name, slab(outline, origin, ex, ez, thick), color, bone, fuse=False, mirror=mirror)
    if vein:
        # one or two streaks from near the root out toward the fin's far points, as the picture's wavy rays
        p = np.array(outline, float)
        m = p.mean(axis=0)
        far = p[np.argsort(-np.linalg.norm(p - p[0] * 0.5, axis=1))[:2]]
        for i, tip in enumerate(far if vein > 0.52 else far[:1]):
            a = p[0] * 0.6 + m * 0.4
            b = a + (tip - a) * 0.78
            d = b - a; L = np.linalg.norm(d); d /= L
            q = np.array([-d[1], d[0]]) * L * 0.09
            lens = [a, a + (b - a) * 0.35 + q, b, a + (b - a) * 0.55 - q * 0.8]
            k.add(f"{name}vein{i}", slab([tuple(x) for x in lens], origin, ex, ez, thick * 1.7), streak, bone,
                  fuse=False, mirror=mirror)


def rounded(c, rx, ry, a0, a1, n=12, root=((0.0, 0.0),)):
    pts = [(c[0] + rx * math.cos(math.radians(a)), c[1] + ry * math.sin(math.radians(a)))
           for a in np.linspace(a0, a1, n)]
    return list(root) + pts


# --- the body ----------------------------------------------------------------------------------------------------
def body(k):
    k.add("trunk", body_tube([(0, -0.17, Z0 + 0.012), (0, -0.06, Z0 + 0.008), (0, 0.06, Z0 + 0.002), (0, 0.17, Z0)],
                             [0.07, 0.082, 0.07, 0.042]), GREEN, "body")
    k.add("stalk", body_tube([(0, 0.16, Z0), (0, 0.24, Z0 + 0.002), (0, 0.29, Z0 + 0.003)], [0.042, 0.026, 0.018],
                             flat=0.76), GREEN, "tail")
    k.add("head", body_tube([(0, -0.31, Z0 - 0.004), (0, -0.24, Z0 + 0.006), (0, -0.16, Z0 + 0.012)],
                            [0.04, 0.058, 0.07], flat=0.82), GREEN, "head")
    # the duck-bill: a flat rounded upper lip over a cream lower one, wider than deep
    k.add("bill", ell((0, -0.352, Z0 - 0.012), (0.036, 0.066, 0.02), rot=(-6, 0, 0)), GREEN, "head")
    k.add("lowerlip", ell((0, -0.345, Z0 - 0.034), (0.031, 0.062, 0.015), rot=(-4, 0, 0)), CREAM, "head")
    k.add("jaw", body_tube([(0, -0.33, Z0 - 0.036), (0, -0.25, Z0 - 0.04), (0, -0.17, Z0 - 0.044)],
                           [0.02, 0.034, 0.042], flat=0.9), CREAM, "head")
    # the cheeks the eyes lie on, a little forward and out
    k.add("cheek", ell((0.03, -0.23, Z0 + 0.022), (0.026, 0.042, 0.036)), GREEN, "head", mirror=True)


def belly_edge(y):
    y = np.asarray(y, float)
    return np.interp(y, [-0.42, -0.3, -0.24, -0.15, 0.0, 0.12, 0.2, 0.3],
                     [Z0 - 0.03, Z0 - 0.022, Z0 - 0.01, Z0 - 0.03, Z0 - 0.034, Z0 - 0.024, Z0 - 0.006, Z0 + 0.0])


def markings(k):
    k.paint(lambda P: inside(P[:, 2] - belly_edge(P[:, 1]), 0.003), CREAM)
    # the mouth line along the bill's side, from the tip back under the eye
    k.paint(lambda P: inside(np.abs(P[:, 2] - (Z0 - 0.026 + 0.012 * np.clip((P[:, 1] + 0.3) / 0.06, 0, 1))) - 0.003,
                             0.0015) * inside(P[:, 1] + 0.255, 0.003) * inside(0.012 - np.abs(P[:, 0]), 0.003),
            MOUTH)

    # pale wavy bars down the upper flank, leaning back toward the top
    def bars(P):
        m = np.zeros(len(P))
        dz = P[:, 2] - Z0
        # (start along the body, how far down the flank the bar reaches below the centre line)
        for i, (y0, low) in enumerate(((-0.105, 0.0), (-0.062, 0.022), (-0.022, 0.006), (0.018, 0.026),
                                       (0.056, 0.01), (0.092, 0.02), (0.128, 0.004), (0.16, 0.012))):
            yc = y0 + 0.35 * dz + 0.006 * np.sin(dz * 90 + i)
            w = 0.0088 - 0.002 * (y0 > 0.12)
            m = np.maximum(m, inside(np.abs(P[:, 1] - yc) - w, 0.002) * inside(-low - dz, 0.004))
        return m * inside(belly_edge(P[:, 1]) + 0.022 - P[:, 2], 0.004)
    k.paint(bars, BAR)


# --- fins ---------------------------------------------------------------------------------------------------------
DORSAL = [(0.0, 0.0), (0.02, 0.04), (0.05, 0.066), (0.085, 0.064), (0.075, 0.03), (0.095, 0.0)]
ANAL = [(0.0, 0.0), (0.07, 0.0), (0.085, -0.04), (0.06, -0.052), (0.02, -0.03)]
PEC = [(0.0, 0.01), (0.04, 0.016), (0.08, -0.004), (0.088, -0.034), (0.06, -0.048), (0.024, -0.03), (0.0, -0.01)]
PELVIC = [(0.0, 0.006), (0.04, 0.006), (0.066, -0.02), (0.058, -0.044), (0.026, -0.038), (0.004, -0.016)]
TAIL = [(0.0, 0.016), (0.03, 0.05), (0.075, 0.098), (0.1, 0.11), (0.09, 0.07), (0.066, 0.02), (0.07, 0.0),
        (0.066, -0.02), (0.09, -0.068), (0.1, -0.104), (0.072, -0.09), (0.03, -0.046), (0.0, -0.016)]


def fins(k):
    ex = yaw_ex(40)
    fin(k, "dorsal", DORSAL, (0.0, 0.09, Z0 + 0.052), ex, (0, 0, 1), "dorsal")
    fin(k, "anal", ANAL, (0.0, 0.1, Z0 - 0.045), ex, (0, 0, 1), "body")
    big = lambda o, s: [(a * s, b * s) for a, b in o]
    fin(k, "pec", big(PEC, 1.3), (0.04, -0.125, Z0 - 0.04), unit(0.72, 0.62, -0.3), unit(0.15, 0.2, 1), "fin{s}",
        mirror=True)
    fin(k, "pelvic", big(PELVIC, 1.2), (0.03, 0.01, Z0 - 0.058), unit(0.7, 0.62, -0.35), unit(0.2, 0.25, 1), "body",
        mirror=True)
    fin(k, "tailfin", TAIL, (0.0, 0.272, Z0 + 0.003), yaw_ex(40), (0, 0, 1), "tailFin", thick=0.008, vein=0.6)


# --- the eye: amber with a dark rim and a dark pupil forward, a white glint ----------------------------------------
def open_eye(iris=AMBER, rim=INK):
    CI, CK = srgb(iris), srgb(rim)
    def look(u, v):
        c = np.tile(CI, (len(u), 1))
        c[u * u + v * v > 0.84 ** 2] = CK
        return c
    return look


def lay_eyes(k, state, at, n, half, lid, look, pupil=0.5, pupil_at=(-0.12, 0.0), glint=True):
    """Both eyes: the iris with its rim (the state's look), and in the open states a pupil and a glint of their own.
    On a patch facing sideways +u runs toward the back on both sides."""
    f = eye_look(state, look, srgb(lid), srgb(INK), line=0.16)
    at = np.array(at, float); n = np.array(n, float)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        c, nn = at * m, n * m
        k.decal(c, nn, half, f, eps=0.003, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            # the pupil forward (toward the snout: -y) and the glint up and forward
            pc = c + np.array([0, pupil_at[0] * half[0] * 2, pupil_at[1] * half[1]])
            k.decal(pc, nn, (half[0] * pupil, half[1] * pupil), INK, eps=0.0042, rings=8, segs=36)
            if glint:
                gc = c + np.array([0, -0.32 * half[0], 0.34 * half[1]])
                k.decal(gc, nn, (half[0] * 0.2, half[1] * 0.2), WHITE, eps=0.0054, rings=5, segs=24)


EYE_AT = np.array([0.05, -0.236, Z0 + 0.03])
EYE_N = unit(0.72, -0.6, 0.28)
EYE_HALF = (0.027, 0.027)


def stiucuta(src, out):
    k = Kit(src, coat="scales")
    body(k)
    markings(k)
    fins(k)
    k.build(voxel=0.004, smooth=4, band=0.005, wband=0.04)
    k.eye_states(lambda state: lay_eyes(k, state, EYE_AT, EYE_N, EYE_HALF, GREEN, open_eye()))
    k.export(out)


if __name__ == "__main__":
    stiucuta(*args())
