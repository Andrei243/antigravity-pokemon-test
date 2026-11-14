# Snover sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/snover.py -- <the export's Snover.glb> <out.glb>
# A little tree under snow: an egg of a body, white above and brown bark below, the two met in a zigzag with a
# second dark zigzag line lower down; a cap of snow like a small fir, a folded cone with three points on its top and a
# brim standing out over the body; pale eyes in a dark rim just under the brim; short arms of snow held out and down, teal mittens like pine
# boughs; a teal tuft of a tail behind; short pointed feet of pale bark.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

SNOW, BARK, LINE, TEAL, FEET = "#F2F5F9", "#B98C76", "#8A604E", "#3F9C94", "#D8A890"

# The body: white parts throughout, the bark painted on below the zigzag
k.add("low", ell((0, 0.0, 0.19), (0.162, 0.15, 0.17)), SNOW, "body")
k.add("mid", ell((0, 0.0, 0.3), (0.145, 0.135, 0.13)), SNOW, "body")
k.add("up", ell((0, 0.0, 0.39), (0.125, 0.115, 0.1)), SNOW, "head")
k.add("face", ell((0, -0.035, 0.395), (0.108, 0.088, 0.075)), SNOW, "head")

def around(P):
    return np.arctan2(P[:, 0], -P[:, 1]) / (2 * math.pi)
k.paint(lambda P: inside(P[:, 2] - (0.262 + 0.028 * tri(around(P) * 10)), 0.0025), BARK,
        on=["low", "mid", "up", "face"])
k.paint(lambda P: inside(np.abs(P[:, 2] - (0.15 + 0.022 * tri(around(P) * 10 + 0.5))) - 0.0045, 0.003), LINE,
        on=["low", "mid"])

# Short arms of snow held out and down, teal hands like mittens of fir: a palm and short pointed lobes
SH = np.array([0.12, -0.02, 0.35])
k.add("arm", tube([SH - (0.03, 0, -0.01), SH, (0.165, -0.025, 0.325), (0.195, -0.03, 0.3)],
                  [0.05, 0.048, 0.044, 0.042]), TEAL, "arm{s}", mirror=True)
PALM = np.array([0.215, -0.032, 0.272])
k.add("palm", ell(PALM, (0.04, 0.032, 0.05), rot=(0, 30, 0)), TEAL, "arm{s}", mirror=True)
# the lobes at the hand's lower end, flat to the front like a bough's needles, a short one inward for a thumb
LOBES = [((0.235, -0.032, 0.255), (0.278, -0.036, 0.215), 0.026), ((0.22, -0.032, 0.245), (0.24, -0.036, 0.19), 0.027),
         ((0.2, -0.032, 0.25), (0.192, -0.036, 0.205), 0.022), ((0.24, -0.032, 0.285), (0.282, -0.036, 0.275), 0.02)]
for i, (base, tip, r) in enumerate(LOBES):
    base, tip = np.array(base), np.array(tip)
    k.add(f"lobe{i}_", blade([base, base + (tip - base) * 0.55, tip], [r, r * 0.8, 0.006], (0, 1, 0), 0.6), TEAL,
          "arm{s}", mirror=True)
# the snow over the arm, its jagged edge onto the hand
k.paint(lambda P: inside(np.abs(P[:, 0]) - (0.19 + 0.014 * tri(P[:, 2] * 24 + P[:, 1] * 22)), 0.0025),
        SNOW, on=["arm", "palm", "lobe"])

# A teal tuft of a tail, low behind
TB = np.array([0.0, 0.12, 0.19])
for i, tip in enumerate(((0.055, 0.25, 0.2), (-0.05, 0.24, 0.23), (0.0, 0.26, 0.12), (0.01, 0.22, 0.29))):
    tip = np.array(tip)
    k.add(f"tail{i}", tube([TB, TB + (tip - TB) * 0.55, tip], [0.04, 0.028, 0.006]), TEAL, "body")

# Short pointed feet of pale bark, turned out
def pointed(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.85 * t; V[:, 2] *= 1 - 0.7 * t
    return V
k.add("foot", ell((0.08, -0.055, 0.036), (0.042, 0.085, 0.036), rot=(0, 0, -32), deform=pointed), FEET, "leg{s}",
      fuse=False, mirror=True, surface="leather")

# The cap of snow: a folded cone, its brim standing out over the body, a point at the top and one at each side
def cone(base, apex, R, folds, amp):
    base, apex = np.array(base, float), np.array(apex, float)
    a = apex - base; a /= np.linalg.norm(a)
    e1 = np.cross(a, (0, 1.0, 0)); e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    nt, ns = 108, 26
    th = np.linspace(0, 2 * math.pi, nt, endpoint=False)
    fold = 1 - amp + amp * np.abs(np.sin(folds * th / 2))
    V = [base]
    for j in range(ns):
        s = j / ns
        r = R * (1 - s) * (1 + 0.05 * (1 - s) ** 10) * fold
        c = base + (apex - base) * s
        for i in range(nt):
            V.append(c + r[i] * (math.cos(th[i]) * e1 + math.sin(th[i]) * e2))
    V.append(apex)
    F = []
    def idx(j, i):
        return 1 + j * nt + i % nt
    for i in range(nt):
        F.append([0, idx(0, i + 1), idx(0, i)])
        for j in range(ns - 1):
            F.append([idx(j, i), idx(j, i + 1), idx(j + 1, i + 1), idx(j + 1, i)])
        F.append([idx(ns - 1, i), idx(ns - 1, i + 1), len(V) - 1])
    return np.array(V), F
k.add("cap", cone((0, 0.005, 0.462), (0, 0.01, 0.735), 0.172, 9, 0.06), SNOW, "head", fuse=False)
for s in (1, -1):
    k.add(f"peak{'R' if s > 0 else 'L'}", cone((0.038 * s, 0.005, 0.57), (0.08 * s, 0.005, 0.705), 0.048, 5, 0.06), SNOW,
          "head", fuse=False)

# The eyes: pale cyan ovals in a dark rim, a darker spot of a pupil toward the middle
RIM, IRIS, PUP, LID = srgb("#1E3A42"), srgb("#BFE8E8"), srgb("#2E6E78"), srgb(SNOW)
def eye(u, v):
    c = np.tile(RIM, (len(u), 1))
    c[u ** 2 + v ** 2 < 0.7 ** 2] = IRIS
    c[((u + 0.1) / 0.34) ** 2 + ((v + 0.04) / 0.4) ** 2 < 1] = PUP
    c[((u + 0.2) / 0.12) ** 2 + ((v - 0.22) / 0.12) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.4, -0.91, 0.05)
def eyes(state):
    k.decal((0.06, -0.1, 0.415), de, (0.026, 0.036), eye_look(state, eye, LID, RIM, line=0.2), eps=0.005, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.0065)
k.eye_states(eyes)
k.export(out)
