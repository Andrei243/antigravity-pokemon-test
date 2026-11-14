# Umbreon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/umbreon.py -- <the export's Umbreon.glb> <out.glb>
# Grown from eevee.py (the same frame of four legs and a trunk, the same head, nose, mouth and way of making eyes):
# a sleek slim quadruped in a very dark blue-grey coat with no ruff and no crown; long pointed ears and a thick
# pointed tail, each with a wide yellow band round it; a yellow ring on the forehead and one on each shoulder and
# thigh; slanted red eyes with a dark pupil and a white glint.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
COAT = "#30343F"           # a very dark blue-grey, not black, so the body keeps its shape in the game
GOLD = "#F4CA36"           # the rings and bands: between the drawing's ochre and the 3D model's bright yellow
EYE, PUPIL = "#E2263A", "#160A0E"
NOSE, INK = "#15161B", "#121318"
LINE = "#9096A6"           # the closed eyes' line: light, since the face round them is dark

# --- the frame (the skeleton's joints: body 0.28 up, head joint (0, -0.13, 0.43), ears (+-0.05, -0.13, 0.54),
#     forelegs (+-0.05, -0.08, 0.26), hind legs (+-0.05, 0.13, 0.26), tail (0, 0.16, 0.30)) ------------------------------
# Against Eevee's script: legs about twice as long (joints at 0.26 against 0.13), the body some 1.3 times as long
# (legs 0.215 apart against 0.165), the head's radii some 0.9 of Eevee's on a model 1.2 times as tall.
H = np.array([0, -0.17, 0.505])          # the head's centre, over and in front of its joint
HR = np.array([0.112, 0.112, 0.102])     # its radii: Eevee's grown by some 12 percent, a little longer
LEG_X = 0.05
FORE_Y, HIND_Y = -0.085, 0.13
BODY_Z = 0.29


def head_shape(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head():
    k.add("head", ell(H, HR, deform=head_shape), COAT, "head")
    # A muzzle a little longer than Eevee's, standing forward under the eyes
    k.add("muzzle", ell(H + (0, -0.096, -0.038), (0.047, 0.044, 0.034)), COAT, "head")
    # A short thick neck from the chest into the head, so the head never sits on a pole
    k.add("neck", tube([(0, -0.07, 0.33), (0, -0.11, 0.39), (0, -0.15, 0.45)], [0.074, 0.068, 0.068]), COAT, "head")


def add_body():
    # A deep sturdy trunk: chest, middle and rump, its belly low enough that the legs show short under it
    k.add("chest", ell((0, -0.065, BODY_Z), (0.075, 0.082, 0.096)), COAT, "body")
    k.add("middle", ell((0, 0.03, BODY_Z + 0.004), (0.068, 0.11, 0.084)), COAT, "body")
    k.add("rump", ell((0, 0.12, BODY_Z), (0.074, 0.072, 0.092)), COAT, "body")


def add_legs(color=COAT):
    # Eevee's leg and paw, longer: a broad shoulder and thigh covering the upper half of each leg, the leg tapering
    # under it, a clear paw
    k.add("shoulder", ell((LEG_X + 0.006, FORE_Y + 0.004, 0.215), (0.046, 0.056, 0.1)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y + 0.005, 0.22), (LEG_X + 0.002, FORE_Y, 0.12), (LEG_X + 0.003, FORE_Y - 0.004, 0.035)],
                      [0.042, 0.034, 0.03]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.003, FORE_Y - 0.016, 0.022), (0.034, 0.045, 0.024)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X + 0.006, HIND_Y + 0.008, 0.215), (0.05, 0.07, 0.105)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X + 0.002, HIND_Y + 0.012, 0.2), (LEG_X + 0.003, HIND_Y + 0.026, 0.12),
                        (LEG_X + 0.003, HIND_Y + 0.012, 0.065), (LEG_X + 0.003, HIND_Y, 0.035)],
                       [0.044, 0.034, 0.031, 0.03]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.003, HIND_Y - 0.016, 0.022), (0.034, 0.045, 0.024)), color, "hind{s}", mirror=True)


def along(P, pts, n=200):
    """Nearest place on a polyline (|x| taken, so a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the ears: long and pointed, leaning out and a little back, dark inside (no lining), a wide yellow band round
#     their middle ---------------------------------------------------------------------------------------------------
EAR = [(0.055, -0.135, 0.575), (0.075, -0.13, 0.63), (0.097, -0.124, 0.69), (0.118, -0.117, 0.75), (0.136, -0.11, 0.805)]
EAR_R = [0.044, 0.058, 0.058, 0.038, 0.004]


def add_ears():
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.4), COAT, "ear{s}", mirror=True)

    def band(P):
        t, off = along(P, EAR)
        return (inside(np.abs(t - 0.5) - 0.095, 0.0025) * (np.linalg.norm(off, axis=1) < 0.06) * (P[:, 2] > 0.6))
    k.paint(band, GOLD, on=["ear"])


# --- the tail: thick and pointed, back and up, swung out to the model's +X side as Eevee's is, a wide yellow band
#     round its fullest part --------------------------------------------------------------------------------------------
TAIL = [(0.0, 0.15, 0.3), (0.02, 0.2, 0.335), (0.05, 0.25, 0.39), (0.08, 0.285, 0.45), (0.1, 0.31, 0.51),
        (0.115, 0.325, 0.565)]
TAIL_R = [0.03, 0.062, 0.086, 0.084, 0.052, 0.004]


def add_tail():
    k.add("tail", blade(TAIL, TAIL_R, unit(1, -0.6, 0), 0.75), COAT, "tail")

    def band(P):
        t, off = along(P, TAIL)
        return inside(np.abs(t - 0.5) - 0.085, 0.0025) * (np.linalg.norm(off, axis=1) < 0.12) * (P[:, 1] > 0.17)
    k.paint(band, GOLD, on=["tail"])


# --- the rings: patches on the fused surface, a thick yellow ring round the coat's own colour ------------------------
CC, CG = srgb(COAT), srgb(GOLD)


def ring(inner):
    def f(u, v):
        c = np.tile(CC, (len(u), 1))
        c[u * u + v * v > inner * inner] = CG
        return c
    return f


def add_rings():
    # The shoulders and the thighs, upright ovals on their outer sides
    k.decal((LEG_X + 0.06, FORE_Y + 0.004, 0.205), (1, 0, 0), (0.034, 0.055), ring(0.6), eps=0.004, rings=20, segs=56,
            mirror=True)
    k.decal((LEG_X + 0.06, HIND_Y + 0.008, 0.21), (1, 0, 0), (0.04, 0.058), ring(0.6), eps=0.004, rings=20, segs=56,
            mirror=True)
    # The forehead, an upright oval just over the eyes
    n = unit(0, -0.6, 0.8)
    k.decal(H + n * 0.1, n, (0.027, 0.037), ring(0.52), eps=0.004, rings=20, segs=56)


# --- the face ---------------------------------------------------------------------------------------------------------
CI = srgb(INK)


def add_nose(at=H + (0, -0.145, -0.03), half=(0.013, 0.009), color=NOSE):
    k.decal(at, unit(0, -1, 0.1), half, color, eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]),
            rings=6, segs=24)


def mouth(u, v):
    c = np.tile(CC, (len(u), 1))
    c[(np.abs(v - (-0.2 + 1.4 * (np.abs(u) - 0.45) ** 2)) < 0.26) & (np.abs(u) < 0.88)] = CI
    return c


def add_mouth(at=H + (0, -0.138, -0.063), half=(0.024, 0.014), look=mouth):
    k.decal(at, unit(0, -1, -0.2), half, look, eps=0.003, rings=16, segs=72)


# The eyes: Eevee's way of making them, the shape sharpened into a slanted almond (its top edge falling to the nose)
# and coloured red with a dark pupil and a white glint; no rim. u outward, v up.
EYE_DIR = unit(0.5, -0.86, 0.0)
EYE_HALF = (0.04, 0.032)
EYE_OUTLINE = [(-1.0, -0.15), (-0.6, 0.35), (0.05, 0.72), (0.65, 0.92), (1.0, 0.65), (0.95, 0.0), (0.6, -0.6),
               (0.0, -0.85), (-0.6, -0.65)]
CE, CP = srgb(EYE), srgb(PUPIL)


def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u + 0.06) / 0.17) ** 2 + ((v + 0.02) / 0.72) ** 2 < 1] = CP
    c[((u - 0.32) / 0.17) ** 2 + ((v - 0.38) / 0.17) ** 2 < 1] = (1, 1, 1)
    return c
E = H + EYE_DIR * 0.11 + np.array([0, 0, 0.008])


def eyes(state):
    look = eye_look(state, eye, CC, srgb(LINE))
    # Laid by hand, a side each: u runs toward +X on both, so the left one's look and outline are turned over
    k.decal(E, EYE_DIR, EYE_HALF, look, eps=0.004, rings=20, segs=56, outline=polygon(EYE_OUTLINE))
    m = np.array([-1, 1, 1.0])
    k.decal(E * m, EYE_DIR * m, EYE_HALF, lambda u, v: look(-u, v), eps=0.004, rings=20, segs=56,
            outline=polygon([(-x, y) for x, y in EYE_OUTLINE]))


add_body()
add_legs()
add_head()
add_ears()
add_tail()
add_rings()
add_nose()
add_mouth()

k.build(voxel=0.0058)
k.eye_states(eyes)
k.export(out)
