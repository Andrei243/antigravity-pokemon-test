# Wynaut sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wynaut.py -- <the export's Wynaut.glb> <out.glb>
#
# The design, from the references: a small light blue blob; a big round head with an oval bulb on top, leaning back
# and to its left (-X); two long flat ears hanging from the upper sides of the head like arms, nearly to the ground
# (in the art one is flung out; here both hang a little out and forward); eyes always shut, each a bold caret "^";
# a huge open mouth over the lower face, dark red within, its upper edge cut in a zigzag, a pink tongue in its bottom;
# a small body below the head with a zigzag hem and two little feet; a flat black tail lying out behind to the right
# (+X), ending in a paddle with a white eye spot and a black dot in it.
# The skeleton: body (0, 0, 0.18), tail (0, 0.06, 0.08), head (0, 0, 0.24), ears (+-0.06, 0, 0.32),
# legs (+-0.04, 0, 0.06).
# Wobbuffet's script (which builds under __main__) is not imported: its mouth is fixed at six teeth and its own
# tongue place, and its eye is a chevron; the same way of making the mouth and the shut eye is written here at Wynaut's size.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, axes, args

BLUE, BLACK, MOUTH, TONGUE = "#6CC6D8", "#2C2C36", "#8A2232", "#EE8A8C"
INK, SPOT = "#1A2430", "#F4F4F2"

# --- the frame ------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, 0.0, 0.255]), np.array([0.132, 0.12, 0.122])
EYE_ARC, EYE_Z, EYE_HALF = 0.055, 0.29, (0.034, 0.026)
MOUTH_Z, MOUTH_HALF = 0.212, (0.088, 0.066)
LOBE_C, LOBE_N, LOBE_R = np.array([0.185, 0.16, 0.04]), unit(0.45, -0.6, 0.65), (0.06, 0.046, 0.022)
VOXEL = 0.0045


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def head_dir(arc, z):
    th = arc / HR[0]
    return unit(math.sin(th), -math.cos(th), (z - H[2]) / HR[2] * 1.05)


def mouth(k):
    """The open mouth: a dark shape with a zigzag top edge, and the pink tongue in its bottom as a patch over it."""
    top = []
    teeth = 3
    for i in range(2 * teeth + 1):
        u = 1 - 2 * i / (2 * teeth)
        top.append((u * 0.9, 0.66 if i % 2 == 0 else 0.34))
    arc = [(math.cos(a), 0.36 + 1.36 * math.sin(a)) for a in np.linspace(math.pi, 2 * math.pi, 25)]
    outline = [(1.0, 0.52)] + top + [(-1.0, 0.52)] + arc
    d = head_dir(0.0, MOUTH_Z)
    c = on_ell(H, HR, d)
    k.decal(c, d, MOUTH_HALF, MOUTH, eps=0.003, outline=polygon(outline), rings=12, segs=120)
    dt = head_dir(0.0, MOUTH_Z - 0.04)
    k.decal(on_ell(H, HR, dt), dt, (0.058, 0.03), TONGUE, eps=0.0045, rings=10, segs=60)


def caret(u, v):
    """The shut eye: a bold caret, its point at the top."""
    arm = np.abs(v - (0.5 - 1.0 * np.abs(u))) / 1.4
    ends = np.abs(u) - 0.86
    d = np.maximum(arm - 0.17, ends)
    m = np.clip(0.5 - d / 0.07, 0, 1)[:, None]
    return srgb(BLUE)[None] * (1 - m) + srgb(INK)[None] * m


def look(state):
    if state == "Open":
        return caret
    if state != "Fierce":
        return eye_look(state, caret, srgb(BLUE), srgb(INK), line=0.2)
    def fierce(u, v):
        c = np.tile(srgb(BLUE), (len(u), 1))
        c[(np.abs(v - (0.5 * u + 0.05)) < 0.2) & (np.abs(u) < 0.86)] = srgb(INK)
        return c
    return fierce


def tail(k):
    k.add("tail", tube([(0.0, 0.07, 0.075), (0.06, 0.125, 0.045), (0.12, 0.15, 0.035), (0.16, 0.16, 0.038)],
                       [0.03, 0.031, 0.033, 0.036]), BLACK, "tail")
    a, b = axes(LOBE_N)
    V, F = ell((0, 0, 0), LOBE_R)
    V = V @ np.stack([a, b, LOBE_N], axis=0) + LOBE_C
    k.add("tailpaddle", (V, F), BLACK, "tail")
    def spot(u, v):
        c = np.tile(srgb(SPOT), (len(u), 1))
        c[u * u + v * v < 0.16] = srgb(INK)
        return c
    k.decal(LOBE_C + LOBE_N * LOBE_R[2], LOBE_N, (0.03, 0.03), spot, eps=0.004, rings=12, segs=40)


def wynaut(src, out):
    k = Kit(src, coat="fur")
    k.add("head", ell(H, HR), BLUE, "head")
    # the bulb on top, leaning back and to the left, on a short neck
    k.add("stem", tube([(0, 0.008, 0.36), (-0.02, 0.018, 0.39)], [0.042, 0.036]), BLUE, "head")
    k.add("bulb", ell((-0.05, 0.03, 0.405), (0.064, 0.05, 0.084), rot=(-15, -58, 0)), BLUE, "head")
    # the body, short, flaring to a hem of points, and two little feet
    def flare(V):
        V = V.copy()
        s = 1 + 0.18 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.005, 0.1), (0.072, 0.066, 0.075), deform=flare), BLUE, "body")
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        k.add(f"hem{i}", tube([o * 0.062 + np.array([0, 0.005, 0.05]), o * 0.084 + np.array([0, 0.005, 0.03])],
                              [0.03, 0.016]), BLUE, "body")
    k.add("foot", ell((0.034, -0.01, 0.02), (0.03, 0.036, 0.022)), BLUE, "leg{s}", mirror=True)
    # the ears: long flat lobes from the upper sides of the head, hanging a little out and forward
    k.add("ear", blade([(0.1, 0.0, 0.335), (0.14, -0.012, 0.31), (0.162, -0.026, 0.245), (0.172, -0.038, 0.16),
                        (0.172, -0.046, 0.09)], [0.038, 0.04, 0.042, 0.046, 0.048], unit(0.45, -1, 0), 0.5),
          BLUE, "ear{s}", mirror=True)
    tail(k)
    mouth(k)
    k.build(voxel=VOXEL, smooth=3, band=0.005)

    def eyes(state):
        for side in (1, -1):
            d = head_dir(EYE_ARC * side, EYE_Z)
            f = look(state)
            col = f if side == 1 else (lambda u, v, f=f: f(-u, v))
            k.decal(on_ell(H, HR, d), d, EYE_HALF, col, eps=0.004, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    wynaut(*args())
