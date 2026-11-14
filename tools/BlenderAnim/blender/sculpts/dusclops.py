# Dusclops sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dusclops.py -- <the export's Dusclops.glb> <out.glb>
# Grown from Duskull's script: the same greys and bone white, the eye made as Duskull's lit hole is (a patch with a
# red light in it and a lens over the light that glows). A stout grey mummy: one egg-shaped mass rising to a dome,
# wrapped in bandages (darker lines round it), a ruff of bandage tabs hanging round its middle, one great eye on the
# dome, a bone-white wisp knotted on the crown, great bone-white hands at its sides with thick curled fingers, stubby
# wrapped legs, and a long bone-white cloth trailing from its back out to its right, ending in ragged points.
# The skeleton: body (0, 0, 0.38), head (0, 0, 0.58), arms (+-0.15, -0.05, 0.38), tail (0.06, 0.12, 0.44),
# legs (+-0.075, 0, 0.13).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, tri, args

src, out = args()
k = Kit(src, coat="fur")

GREY, BAND, BONE, BONESHADE = "#8A8C94", "#5E606A", "#DCD6CA", "#B4AEA2"

# --- the body: an egg, wide below and rising to a dome; the dome turns with the head ---------------------------------
k.add("body", ell((0, 0.0, 0.30), (0.2, 0.18, 0.2)), GREY, "body")
k.add("dome", ell((0, 0.005, 0.5), (0.168, 0.152, 0.245)), GREY, "head")
# a broad, flatter front for the eye to lie on
k.add("face", ell((0, -0.06, 0.54), (0.15, 0.1, 0.17)), GREY, "head")

# --- the legs: stubby and wrapped, a little apart --------------------------------------------------------------------
k.add("leg", tube([(0.085, 0.0, 0.17), (0.09, -0.005, 0.09), (0.09, -0.01, 0.05)], [0.07, 0.072, 0.07]), GREY,
      "leg{s}", mirror=True)
k.add("foot", ell((0.09, -0.02, 0.04), (0.074, 0.085, 0.042)), GREY, "leg{s}", mirror=True)

# --- the arms: short wrapped wrists out of the sides, holding great hands a little forward and down ------------------
k.add("arm", tube([(0.15, -0.04, 0.38), (0.21, -0.06, 0.34), (0.245, -0.07, 0.31)], [0.055, 0.05, 0.046]), GREY,
      "arm{s}", mirror=True)
H = np.array([0.275, -0.075, 0.29])
k.add("hand", ell(H, (0.066, 0.076, 0.055), rot=(0, -15, 0)), BONE, "arm{s}", mirror=True, surface="cloth")
# four thick fingers along the hand's outer edge, out and then curled down; a thumb to the front
for j, dy in enumerate((-0.054, -0.018, 0.018, 0.054)):
    b0 = H + (0.03, dy, 0.0)
    k.add(f"finger{j}", tube([b0, b0 + (0.05, dy * 0.15, 0.0), b0 + (0.075, dy * 0.2, -0.04), b0 + (0.07, dy * 0.2, -0.085)],
                            [0.027, 0.026, 0.025, 0.023]), BONE, "arm{s}", mirror=True, surface="cloth")
k.add("thumb", tube([H + (-0.01, -0.05, 0.0), H + (0.0, -0.09, -0.03), H + (0.01, -0.1, -0.07)], [0.023, 0.022, 0.02]),
      BONE, "arm{s}", mirror=True, surface="cloth")

# --- the wisp on the crown: a knot and a ribbon curling up and out to its right --------------------------------------
k.add("knot", ell((0, 0.005, 0.74), (0.065, 0.06, 0.045)), BONE, "head", surface="cloth")
k.add("wisp", blade([(0.0, 0.005, 0.77), (0.03, 0.005, 0.81), (0.08, 0.01, 0.815), (0.13, 0.015, 0.83),
                     (0.16, 0.02, 0.865)], [0.04, 0.03, 0.022, 0.014, 0.004], (0, 1, 0), 0.45),
      BONE, "head", surface="cloth")

# --- bandages: darker lines round the body, a little tilted, and round the legs; crisp, so painted with no 'on' -------
def bands(levels, tilt, xs=None):
    def m(P):
        out_ = np.zeros(len(P))
        for z0, a in levels:
            zc = z0 + tilt * a * P[:, 0] + 0.006 * np.sin(np.arctan2(P[:, 0], -P[:, 1]) * 3 + z0 * 40)
            out_ = np.maximum(out_, inside(np.abs(P[:, 2] - zc) - 0.0055, 0.0025))
        if xs is not None:
            out_ *= xs(P)
        return out_
    return m
BODYLINES = [(0.705, 1), (0.655, -1), (0.452, 1), (0.415, -1), (0.285, 1), (0.235, -1), (0.185, 1)]
# kept to the trunk (never the hands) and to the legs by a mask of place
TRUNK = lambda P: ((P[:, 2] > 0.16) & ((P[:, 0] / 0.215) ** 2 + (P[:, 1] / 0.2) ** 2 < 1)).astype(float)
LEGS = lambda P: ((P[:, 2] < 0.16) & (np.abs(np.abs(P[:, 0]) - 0.09) < 0.09)).astype(float)
k.paint(bands(BODYLINES, 0.12, xs=TRUNK), BAND)
k.paint(bands([(0.125, 1), (0.075, -1)], 0.1, xs=LEGS), BAND)

# --- the ruff: a ring of bandage tabs hanging round the middle, flaring out, notched between the tabs ----------------
def ruff(u, v):
    th = -math.pi + 2 * math.pi * (0.004 + u * 1.012)          # the seam at the back
    t = th * 10 / (2 * math.pi)
    dip = smoothstep(0.55, 1.0, tri(t))                      # a V between each two tabs; a tab's middle at the front
    length = 0.1 - 0.045 * dip
    z = 0.425 - v * length
    rx = 0.172 + 0.03 * v
    ry = 0.157 + 0.03 * v
    return np.stack([rx * np.sin(th), -ry * np.cos(th), z], axis=-1)
k.add("ruff", sheet(ruff, 220, 8, 0.014), GREY, "body", fuse=False)

# --- the cloth trailing from its back out to its right (+X), wide and flat, ending in ragged points -------------------
A0, A1 = np.array([0.07, 0.12, 0.46]), np.array([0.43, 0.2, 0.2])
L = A1 - A0
d = L / np.linalg.norm(L)
nrm = unit(0.2, -1, 0.35); nrm = nrm - d * np.dot(nrm, d); nrm /= np.linalg.norm(nrm)
across = np.cross(d, nrm)
def cloth(u, v):
    w = 0.03 + 0.045 * u
    s = 2 * v - 1
    reach = 1 - 0.13 * smoothstep(0.3, 1.0, u) * (1 - np.abs(tri(s * 1.0 + 0.5)))    # three ragged points at the end
    t = u * reach
    sag = -0.04 * np.sin(math.pi * t)
    P = A0[None, None] + t[..., None] * L[None, None] + (s * w)[..., None] * across[None, None] + sag[..., None] * nrm[None, None]
    return P
k.add("cloth", sheet(cloth, 40, 12, 0.014), BONE, "tail", fuse=False, surface="cloth")

k.build(voxel=0.007, smooth=3)

# --- the eye: a white ball with a dark rim, a red light in it, the light's lens glowing --------------------------------
INK, LID = srgb("#2A2830"), srgb(GREY)
WHITE, RIM = srgb("#F2EEE6"), srgb("#3A3A42")
CORE, RED, DEEP = srgb("#FFD2DA"), srgb("#E8406A"), srgb("#8A1A30")
def ball(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(WHITE, (len(u), 1))
    c[r < 0.46] = RED
    c[r < 0.2] = CORE
    c[r > 0.84] = RIM
    return c
EC = np.array([0.0, -0.17, 0.545])
EN = unit(0, -1, 0.12)
EH = 0.07
def eyes(state):
    k.decal(EC, EN, (EH, EH), eye_look(state, ball, LID, INK, line=0.13), eps=0.005, rings=20, segs=56)
    if state in ("Open", "Fierce"):
        hit = TREE.ray_cast(Vector(EC + EN * 0.35), Vector(-EN))[0]
        right = np.cross((0, 0, 1.0), EN); right /= np.linalg.norm(right)
        up = np.cross(EN, right)
        V, F = ell((0, 0, 0), (0.024, 0.024, 0.006))
        V = V @ np.stack([right, up, EN], axis=1).T + np.array(hit) + EN * 0.005
        k.add("light", (V, F), "#FF7A8E", "head", fuse=False, glow="#FF3A55", surface="skin")

from mathutils import Vector
from mathutils.bvhtree import BVHTree
_P, _F, _W = k.surfaces[k.name]
TREE = BVHTree.FromPolygons(_P.tolist(), _F)

k.eye_states(eyes)
k.export(out)
