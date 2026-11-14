# Giratina (Origin Forme, the game's "Giratina-Origin") sculpted from nothing on the skeleton and clips of the game's own
# export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/giratinaorigin.py -- <the export's Giratina-Origin.glb> <out.glb>
# The same Pokemon as giratina.py in its other shape: the Altered Forme's grey, gold, red and black, its way of making
# the face and eyes, but legless: a long grey serpent hanging in a loop (the neck rising in front, the belly low, the
# back end rising behind to a short tail), banded red and black along its belly side, gold spikes out of its flanks,
# a gold crown closed round the head like a helmet with plates down the neck, and six black ribbon-like tendrils
# streaming back from its shoulders, each ending in a red claw.
# The export's chain: body (0.06, 0.34, 0.62) at the tail's end, seg1 to seg6 down round the loop and up the neck,
# head (0, -0.3, 0.77) at the neck's top; tail on the body joint; tendrilL/R 0 to 2 at (+-0.05, -0.12..-0.04, 0.52..0.4).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

GREY, GOLD, RED, BLACK, DARK, SHADOW = "#B4B8C2", "#E9C23A", "#D23A4C", "#2E2B33", "#4A4D58", "#3A3742"

# --- The chain of the body, from the tail's root to the neck's top: the skeleton's joints, a radius each ---
P = np.array([(0.06, 0.34, 0.619), (0.09, 0.42, 0.459), (0.06, 0.32, 0.309), (0.0, 0.12, 0.249), (0.0, -0.06, 0.289),
              (0.0, -0.17, 0.439), (0.0, -0.24, 0.619), (0.0, -0.3, 0.769)])
R = np.array([0.07, 0.095, 0.11, 0.115, 0.11, 0.095, 0.085, 0.08])
BONES = ["body"] + [f"seg{i}" for i in range(1, 7)]
TAIL = np.array([(0.045, 0.3, 0.7), (0.035, 0.265, 0.762)])
HC = np.array([0, -0.37, 0.869])             # the head's centre


def spline(pts, per=12):
    """A Catmull-Rom curve through the points (the same the kit's tube draws), with each sample's segment and share."""
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    C, S = [], []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            C.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            S.append(i + t)
    C.append(pts[-1]); S.append(len(pts) - 1)
    return np.array(C), np.array(S)


# One smooth line from the tail's tip round the loop to the neck's top, which every segment's tube follows
LINE = np.vstack([TAIL[::-1], P])
CRV, SEG = spline(LINE, per=14)
SEG = SEG - 2                                       # 0 at the body joint, i at joint i, below 0 along the tail
RAD = np.interp(SEG, np.arange(-2, len(P)), np.concatenate([[0.032, 0.05], R]))
for i, bone in enumerate(BONES):
    sel = (SEG >= i - 0.15) & (SEG <= i + 1.15)
    k.add(f"seg{i}", tube(CRV[sel][::2], RAD[sel][::2], seg=20, per=5), GREY, bone)
sel = SEG <= 0.15
k.add("tail", tube(CRV[sel][::2], RAD[sel][::2], seg=18, per=5), GREY, "tail")
# the neck carried up into the back of the skull
k.add("nape", tube([P[6], P[7], (0, -0.33, 0.83)], [0.085, 0.08, 0.075]), GREY, "head")

# Arc length along the line, for the bands, on a finer copy of it (a coarse one steps the bands' edges)
CRV_T, SEG_T, RAD_T = CRV, SEG, RAD
CRV, SEG = spline(LINE, per=80)
SEG = SEG - 2
CUM = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(CRV, axis=0), axis=1))])
LOOP = np.array([0, 0.15, 0.6])                     # the loop's middle: the belly faces away from it


def nearest(Q):
    j = np.concatenate([((Q[a:a + 3000, None, :] - CRV[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 3000)])
    return j


def bands(Q, colour):
    j = nearest(Q)
    c = CRV[j]
    rad = Q - c; rad /= np.linalg.norm(rad, axis=1, keepdims=True) + 1e-9
    outward = c - LOOP; outward[:, 0] = 0; outward /= np.linalg.norm(outward, axis=1, keepdims=True) + 1e-9
    belly = smoothstep(-0.3, -0.18, (rad * outward).sum(1))
    PER = 0.11
    f = (CUM[j] / PER) % 1.0
    mid, half = (0.22, 0.22) if colour == "black" else (0.6, 0.14)
    want = inside((np.abs(f - mid) - half) * PER, 0.0065)
    # from the tail's tip to below the head
    span = smoothstep(6.35, 6.15, SEG[j]) * (SEG[j] > -1.6)
    return belly * want * span


k.paint(lambda Q: bands(Q, "black"), BLACK, on=["seg", "tail", "nape"])
k.paint(lambda Q: bands(Q, "red"), RED, on=["seg", "tail", "nape"])

# --- Gold spikes out of the flanks along the loop, each from a grey spur, pointing back toward the tail ---
def at(s):
    i = int(np.argmin(np.abs(SEG_T - s)))
    a = CRV_T[min(i + 1, len(CRV_T) - 1)] - CRV_T[max(i - 1, 0)]
    return CRV_T[i], a / np.linalg.norm(a), RAD_T[i]


for n, s in enumerate([0.7, 1.7, 2.6, 3.5, 4.4]):
    c, a, r = at(s)
    bone = BONES[int(s)]
    side = np.array([1.0, 0, 0])
    base = c + side * r * 0.8
    k.add(f"spike{n}", blade([base - side * 0.02, base + side * 0.08 - a * 0.025, base + side * 0.16 - a * 0.06],
                             [0.032, 0.022, 0.003], (0, 0, 1), 0.75), GOLD, bone, fuse=False, mirror=True,
          surface="shell")
# two more under the belly's lowest point, pointing down and back
for n, s in enumerate([2.9, 3.6]):
    c, a, r = at(s)
    d = unit(0.55, 0, -1)
    base = c + d * r * 0.8
    k.add(f"lowspike{n}", blade([base, base + d * 0.08 - a * 0.03, base + d * 0.15 - a * 0.07],
                                [0.03, 0.02, 0.003], (0, 1, 0), 0.75), GOLD, BONES[int(s)], fuse=False, mirror=True,
          surface="shell")
# the tail's end: a gold spike rising out of it, and a pair at its sides
k.add("tailspike", blade([TAIL[1] - (0, -0.01, 0.02), TAIL[1] + (0.0, -0.02, 0.06), TAIL[1] + (-0.005, -0.04, 0.12)],
                         [0.03, 0.02, 0.003], (1, 0, 0), 0.7), GOLD, "tail", fuse=False, surface="shell")
k.add("tailside", blade([TAIL[0] + (0.04, 0, 0), TAIL[0] + (0.1, 0.03, 0.0), TAIL[0] + (0.15, 0.07, -0.01)],
                        [0.026, 0.018, 0.003], (0, 0, 1), 0.75), GOLD, "tail", fuse=False, mirror=True,
      surface="shell")

# --- The head: the Altered Forme's grey dome and black face, closed in a gold helmet ---
k.add("skull", ell(HC, (0.115, 0.13, 0.105)), GREY, "head")
k.add("snout", ell((0, -0.46, 0.84), (0.085, 0.09, 0.075)), BLACK, "head")
k.paint(lambda Q: inside(Q[:, 2] - 0.815), GOLD, on=["snout"])
k.add("chin", ell((0, -0.48, 0.775), (0.075, 0.065, 0.03)), GOLD, "head", fuse=False, surface="shell")
# the helmet's dome over the skull, its brow plate over the face, guards closed down the cheeks
k.add("dome", ell((0, -0.355, 0.905), (0.122, 0.14, 0.072), rot=(-10, 0, 0)), GOLD, "head", fuse=False,
      surface="shell")
k.add("brow", blade([(0.0, -0.47, 0.925), (0.05, -0.465, 0.915), (0.09, -0.445, 0.89)], [0.03, 0.028, 0.024],
                    (0, 1, 0.3), 0.6), GOLD, "head", fuse=False, mirror=True, surface="shell")
k.add("guard", ell((0.1, -0.385, 0.83), (0.036, 0.095, 0.09), rot=(15, 0, 0)), GOLD, "head", fuse=False, mirror=True,
      surface="shell")
# the crown's points: a tall one up from the brow, one up and out over each eye, one down from each jaw
k.add("crest", blade([(0, -0.45, 0.94), (0, -0.485, 0.99), (0, -0.49, 1.03), (0, -0.46, 1.06)], [0.05, 0.042, 0.03, 0.004],
                     (1, 0, 0), 0.5), GOLD, "head", fuse=False, surface="shell")
k.add("prong", blade([(0.07, -0.4, 0.93), (0.12, -0.42, 1.0), (0.155, -0.42, 1.05)], [0.04, 0.03, 0.004],
                     (0, 1, 0), 0.5), GOLD, "head", fuse=False, mirror=True, surface="shell")
k.add("jawspike", blade([(0.1, -0.38, 0.77), (0.13, -0.34, 0.7), (0.15, -0.3, 0.64)], [0.035, 0.026, 0.004],
                        (1, 0, 0), 0.5), GOLD, "head", fuse=False, mirror=True, surface="shell")

# Gold plates layered down the neck's sides, pointing back and down like a mane
for n, s in enumerate([5.9, 5.45, 5.0]):
    c, a, r = at(s)
    back = unit(0, 1, -0.55)
    base = c + np.array([r * 0.75, 0, 0])
    size = 1 + 0.15 * n
    k.add(f"plate{n}", blade([base, base + np.array([0.05, 0, 0]) + back * 0.07 * size,
                              base + np.array([0.08, 0, 0]) + back * 0.17 * size],
                             [0.05 * size, 0.04 * size, 0.004], (1, 0, 0), 0.3), GOLD, "head" if s > 5.5 else "seg5",
          fuse=False, mirror=True, surface="shell")

# --- Six tendrils fanning out from the upper back like ribbons, three a side: the upper pair up and out, the middle
# out, the lower out and down, all sweeping back; each a long flattened ribbon, gently waved, tapering to a red claw ---
FAN_N = unit(0.66, -0.75, 0.0)                       # the fan's face (on +X): out and forward
TIPS = [np.array([0.36, 0.34, 0.95]), np.array([0.47, 0.38, 0.64]), np.array([0.41, 0.4, 0.29])]
for i in range(3):
    root = np.array([0.05, -0.12 + 0.04 * i, 0.519 - 0.06 * i])
    tip = TIPS[i]
    d = unit(*(tip - root))
    w = np.cross(FAN_N, d); w /= np.linalg.norm(w)   # across the ribbon, in the fan's plane
    ts = np.linspace(0, 1, 7)
    pts = np.array([root + (tip - root) * t + w * 0.03 * math.sin(t * math.pi * 2.2) * min(1, t * 3) for t in ts])
    pts[1] += (0, 0.02, 0.0)                         # leave the back before turning out
    ax = FAN_N - d * (FAN_N @ d); ax /= np.linalg.norm(ax)
    k.add(f"tendril{i}", blade(pts, [0.026, 0.04, 0.046, 0.044, 0.038, 0.03, 0.02], ax, 0.36), SHADOW,
          f"tendril{{s}}{i}", fuse=False, mirror=True)
    e = unit(*(pts[-1] - pts[-2]))
    k.add(f"claw{i}", blade([pts[-1] - e * 0.02, pts[-1] + e * 0.04, pts[-1] + e * 0.1], [0.026, 0.017, 0.003],
                            ax, 0.55), RED, f"tendril{{s}}{i}", fuse=False, mirror=True, surface="shell")

# --- The eyes, as the Altered Forme's: red with a dark slit, in the black face under the gold brow ---
EYE, PUPIL, LID = srgb("#E8303C"), srgb("#1A1418"), srgb(BLACK)
def eye(u, v):
    c = np.tile(EYE, (len(u), 1))
    c[(u / 0.35) ** 2 + (v / 0.8) ** 2 < 1] = PUPIL
    return c
de = unit(0.5, -0.9, 0.12)
def eyes(state):
    k.decal(np.array([0.035, -0.46, 0.864]) + de * 0.06, de, (0.026, 0.017), eye_look(state, eye, LID, EYE, line=0.3),
            eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.0085)
k.eye_states(eyes)
k.export(out)
