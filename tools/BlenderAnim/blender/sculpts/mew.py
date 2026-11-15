# Mew sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mew.py -- <the export's Mew.glb> <out.glb>
#
# The design, from the references: a small pink cat-like creature that floats; a big round head, wide at the cheeks,
# with a short soft muzzle and two small triangular ears at the top of its sides; large eyes, white with a big blue
# iris and a dark pupil; a small body, narrow at the chest and fuller low down; short arms with little three-fingered
# paws held forward; long hind legs ending in big oval feet, hanging; a very long thin tail curling up round behind
# and to one side, ending in a long teardrop.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, INKH, BLUE, BLUEDK = "#F4B4CA", "#3A2434", "#3C8CDC", "#1E4A9C"
H = np.array([0.0, -0.016, 0.576])          # the head's middle (head bone 0, -0.01, 0.501)
HR = np.array([0.108, 0.096, 0.092])


def pear(V):
    """Narrow at the chest, fuller low down."""
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.25 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def cheeks(V):
    """A head wider low at the cheeks, a little flatter on top."""
    V = V.copy()
    V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2] + 0.2, 0, 1)
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.94, V[:, 2])
    return V


def drop(V):
    """A teardrop along +z: a point at -z, round at +z."""
    V = V.copy()
    t = (V[:, 2] + 1) / 2
    w = 0.6 + 0.4 * np.clip(t, 0, 1) ** 0.7
    V[:, 0] *= w; V[:, 1] *= w
    return V


# --- the body: a small pear tilted forward under the head ------------------------------------------------------------
k.add("body", ell((0, -0.012, 0.41), (0.062, 0.058, 0.078), rot=(15, 0, 0), deform=pear), PINK, "body")
k.add("neck", tube([(0, -0.012, 0.46), (0, -0.014, 0.5)], [0.036, 0.04]), PINK, "body")

# --- the head: big and round, a short muzzle, small low rounded ears --------------------------------------------------
k.add("head", ell(H, HR, deform=cheeks), PINK, "head")
k.add("muzzle", ell((0, -0.088, 0.543), (0.044, 0.036, 0.032)), PINK, "head")
k.add("ear", blade([(0.058, 0.006, 0.64), (0.074, 0.01, 0.655), (0.086, 0.014, 0.663)], [0.033, 0.023, 0.014],
                   unit(0.35, 1, 0), 0.5), PINK, "head", mirror=True)

# --- the arms: short, forward, tiny paws ------------------------------------------------------------------------------
PAW = np.array([0.086, -0.064, 0.436])
k.add("arm", tube([(0.05, -0.02, 0.458), (0.072, -0.044, 0.445), PAW], [0.017, 0.015, 0.013]), PINK, "arm{s}",
      mirror=True)
k.add("paw", ell(PAW, (0.015, 0.014, 0.013)), PINK, "arm{s}", mirror=True)
for i, d in enumerate(((0.008, -0.01, 0.007), (0.011, -0.012, -0.003), (0.003, -0.013, -0.01))):
    k.add(f"finger{i}", tube([PAW, PAW + np.array(d)], [0.007, 0.0055]), PINK, "arm{s}", mirror=True)

# --- the legs: short, tucked up and forward, very large oval feet hanging ---------------------------------------------
k.add("thigh", ell((0.047, -0.03, 0.352), (0.042, 0.05, 0.046), rot=(-20, 0, -6)), PINK, "leg{s}", mirror=True)
k.add("shin", tube([(0.05, -0.05, 0.338), (0.055, -0.072, 0.31)], [0.03, 0.027]), PINK, "leg{s}", mirror=True)
k.add("foot", ell((0.058, -0.088, 0.276), (0.034, 0.062, 0.028), rot=(55, 0, 0)), PINK, "leg{s}", mirror=True)

# --- the tail: long and thin, out to the model's right, up and over in one loop, ending in a large teardrop ---------
TAIL = [(0, 0.05, 0.37), (0.02, 0.13, 0.33), (0.1, 0.19, 0.3), (0.2, 0.19, 0.33), (0.27, 0.13, 0.41),
        (0.3, 0.07, 0.5), (0.295, 0.05, 0.58), (0.265, 0.05, 0.64), (0.21, 0.07, 0.665)]
k.add("tail", tube(TAIL, np.linspace(0.03, 0.018, len(TAIL))), PINK, "tail")
d = unit(-0.85, 0.3, 0.1)
end = np.array(TAIL[-1])
V, F = ell((0, 0, 0), (0.04, 0.03, 0.075), deform=drop, sub=4)
a = unit(*np.cross(d, (0, 0, 1.0)))
b = np.cross(d, a)
k.add("tailtip", (V @ np.stack([a, b, d], axis=1).T + end + d * 0.05, F), PINK, "tail")

# --- the mouth: a small soft smile under the muzzle -------------------------------------------------------------------
INK, CP = srgb(INKH), srgb(PINK)


def mouth(u, v):
    c = np.tile(CP, (len(u), 1))
    c[(np.abs(v - 0.35 * (u * u - 0.5)) < 0.3) & (np.abs(u) < 0.9)] = INK
    return c


k.decal((0, -0.124, 0.532), unit(0, -1, -0.3), (0.013, 0.006), mouth, eps=0.003, rings=10, segs=48)

# --- the eyes: large, white with a big blue iris, a dark pupil and a glint ------------------------------------------
CW, CB, CD, CK = np.array([1.0, 1.0, 1.0]), srgb(BLUE), srgb(BLUEDK), srgb("#14142A")


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = u ** 2 + v ** 2
    # a big blue iris on the inner side, the white showing outward; a dark pupil and a glint
    ir = ((u + 0.2) / 0.68) ** 2 + ((v + 0.08) / 0.84) ** 2
    c[ir < 1] = CD
    c[ir < 0.66] = CB
    c[((u + 0.24) / 0.28) ** 2 + ((v + 0.02) / 0.44) ** 2 < 1] = CK
    c[((u + 0.1) / 0.16) ** 2 + ((v - 0.34) / 0.18) ** 2 < 1] = CW
    # the outline: a sixth of the eye at the sides and bottom, heavier along the top (the lid's lashes)
    c[(r > 0.7) | ((r > 0.5) & (v > 0.45))] = INK
    return c


de = unit(0.37, -0.92, 0.06)
E = H + np.array([0, 0, 0.004]) + de * HR


def eyes(state):
    k.decal(E, de, (0.031, 0.044), eye_look(state, eye, CP, INK, line=0.16), eps=0.004, rings=20, segs=56, mirror=True)


k.build(voxel=0.0045, band=0.006)
k.eye_states(eyes)
k.export(out)
