# Jirachi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/jirachi.py -- <the export's Jirachi.glb> <out.glb>
#
# From the references: a small white fairy that floats, its head with its hat some two thirds of its height. The hat
# is a puffy yellow star of three points (one up, two out to the sides drooping a little), over the top and back of a
# round white face, coming down in two rounded yellow flaps beside the cheeks; a teal wish tag lies along the front of
# the upper point and one hangs from the tip of each side point. Big black eyes with a white glint, a small teal tear
# under each, a small smile. The body is a small white bell with a shut third eye on the belly (a dark curved line),
# stubby arms held forward and down ending in little three-pointed hands, two short legs with round feet; and from the
# back two broad yellow streamers sweeping out, back and down like comet tails, their edges wavy. The joints: body
# (0, 0, 0.209), head (0, 0, 0.259), armR/L (+-0.05, 0, 0.209), wingR/L (+-0.04, 0.04, 0.219), legR/L
# (+-0.03, 0, 0.139).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, slab, outward, srgb, inside, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, YELLOW, TEAL, INKH, BELLY = "#F4F4F8", "#F6DC62", "#3CCABE", "#14141C", "#5A5A68"

FACE_C, FACE_R = np.array([0.0, -0.012, 0.335]), np.array([0.088, 0.074, 0.072])


def bell(V):
    """A small bell: narrow at the neck, fuller low down."""
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.3 * np.clip(V[:, 2], 0, 1) ** 1.4
    V[:, 0] *= s; V[:, 1] *= s
    return V


# --- the body ---------------------------------------------------------------------------------------------------
k.add("body", ell((0.0, 0.0, 0.198), (0.058, 0.052, 0.072), deform=bell), WHITE, "body")
k.add("neck", tube([(0.0, 0.0, 0.248), (0.0, -0.004, 0.285)], [0.033, 0.036]), WHITE, "head")
k.add("leg", ell((0.03, -0.004, 0.118), (0.025, 0.027, 0.032)), WHITE, "leg{s}", mirror=True)
k.add("foot", ell((0.033, -0.016, 0.09), (0.026, 0.032, 0.02)), WHITE, "leg{s}", mirror=True)
# Stubby arms, forward and down, ending in a little hand of three points
WRIST = np.array([0.078, -0.03, 0.188])
k.add("arm", tube([(0.045, -0.004, 0.218), (0.064, -0.018, 0.202), WRIST], [0.019, 0.017, 0.016]), WHITE, "arm{s}",
      mirror=True)
for i, d in enumerate(((0.026, -0.006, 0.014), (0.03, -0.01, -0.004), (0.02, -0.012, -0.02))):
    k.add(f"finger{i}", tube([WRIST, WRIST + np.array(d)], [0.012, 0.006]), WHITE, "arm{s}", mirror=True)

# --- the head: a round white face under a puffy yellow star ------------------------------------------------------
k.add("face", ell(FACE_C, FACE_R), WHITE, "head")
k.add("hat", ell((0.0, 0.016, 0.39), (0.116, 0.075, 0.09)), YELLOW, "head")
k.add("hattop", blade([(0.0, 0.016, 0.43), (0.0, 0.018, 0.52), (0.0, 0.022, 0.592)], [0.094, 0.056, 0.02],
                      (0, 1, 0), 0.62), YELLOW, "head")
k.add("hatside", blade([(0.08, 0.012, 0.392), (0.16, 0.014, 0.386), (0.226, 0.016, 0.366)], [0.074, 0.044, 0.017],
                       (0, 1, 0), 0.62), YELLOW, "head", mirror=True)
# The rounded flaps hanging beside the cheeks
k.add("flap", blade([(0.098, 0.004, 0.37), (0.108, -0.006, 0.32), (0.112, -0.012, 0.282)], [0.036, 0.037, 0.03],
                    unit(0.3, 1, 0), 0.72), YELLOW, "head", mirror=True)

# --- the wish tags: one along the front of the upper point, one hanging from the tip of each side point ------------
RECT = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])
k.decal((0.0, -0.02, 0.497), unit(0, -1, 0.18), (0.021, 0.062), TEAL, outline=RECT, eps=0.004, rings=12, segs=80)
TAG = [(-0.021, -0.128), (0.021, -0.128), (0.021, 0.01), (-0.021, 0.01)]
k.add("tag", slab(TAG, (0.236, 0.014, 0.36), (1, 0, 0), (0, 0, 1), 0.014), TEAL, "head", fuse=False, mirror=True)

# --- the streamers: broad ribbons out of the back, sweeping out, back and down, their lower edges wavy ------------


def streamer(s):
    root = np.array([0.03 * s, 0.03, 0.212])
    mid = np.array([0.13 * s, 0.1, 0.19])
    end = np.array([0.235 * s, 0.17, 0.15])

    def at(u, v):
        uu = 0.015 + 0.97 * u
        p = ((1 - uu) ** 2)[..., None] * root + (2 * uu * (1 - uu))[..., None] * mid + (uu ** 2)[..., None] * end
        w = 0.016 + 0.07 * uu ** 0.8
        wave = 0.011 * np.sin(5 * math.pi * uu) * uu
        drop = v * (w + wave * v)
        # the ribbon hangs down from its upper edge, leaning back a little as it falls
        return p + drop[..., None] * np.array([0.0, 0.25, -1.0]) + (0.5 * w)[..., None] * np.array([0.0, 0.0, 0.5])
    return outward(sheet(at, 40, 8, 0.014))


for s, side in ((1, "R"), (-1, "L")):
    k.add(f"streamer{side}", streamer(s), YELLOW, f"wing{side}", fuse=False)

# --- the face's marks: a small smile, a teal tear under each eye --------------------------------------------------
CW, CY, CK, CT = srgb(WHITE), srgb(YELLOW), srgb(INKH), srgb(TEAL)


def smile(u, v):
    c = np.tile(CW, (len(u), 1))
    c[(np.abs(v - (0.5 * u * u - 0.15)) < 0.32) & (np.abs(u) < 0.9)] = CK
    return c


k.decal((0.0, -0.08, 0.31), unit(0, -1, -0.2), (0.017, 0.008), smile, eps=0.004, rings=10, segs=48)
DROP = polygon([(0.0, 1.0)] + [(0.7 * math.cos(t), -0.3 + 0.7 * math.sin(t))
                               for t in np.linspace(math.pi, 2 * math.pi, 13)])      # a point up, round below
TEAR_N = unit(0.5, -0.86, -0.1)
k.decal((0.045, -0.072, 0.302), TEAR_N, (0.0085, 0.013), TEAL, outline=DROP, eps=0.004, rings=8, segs=60)
k.decal((-0.045, -0.072, 0.302), TEAR_N * np.array([-1, 1, 1]), (0.0085, 0.013), TEAL, outline=DROP, eps=0.004,
        rings=8, segs=60)

# --- the shut third eye on the belly: a dark curved line -------------------------------------------------------
CWB, CB = srgb(WHITE), srgb(BELLY)


def third(u, v):
    c = np.tile(CWB, (len(u), 1))
    c[(np.abs(v - (0.85 * u * u - 0.4)) < 0.3) & (np.abs(u) < 0.92)] = CB
    return c


k.decal((0.0, -0.05, 0.19), unit(0, -1, 0.05), (0.035, 0.016), third, eps=0.004, rings=10, segs=60)

# --- the eyes: big and black, a white glint high on the inner side ------------------------------------------------


def eye(u, v):
    c = np.tile(CK, (len(u), 1))
    c[((u + 0.18) / 0.26) ** 2 + ((v - 0.38) / 0.24) ** 2 < 1] = np.array([1.0, 1.0, 1.0])
    c[((u - 0.2) / 0.11) ** 2 + ((v + 0.45) / 0.1) ** 2 < 1] = np.array([1.0, 1.0, 1.0])
    return c


EYE_N = unit(0.43, -0.9, 0.04)


def eyes(state):
    k.decal(FACE_C + np.array([0.0, 0.0, 0.008]) + EYE_N * FACE_R * 0.98, EYE_N, (0.021, 0.03),
            eye_look(state, eye, CW, CK, line=0.16), eps=0.004, rings=20, segs=56, mirror=True)


k.build(voxel=0.0042, smooth=4, band=0.004)
k.eye_states(eyes)
k.export(out)
