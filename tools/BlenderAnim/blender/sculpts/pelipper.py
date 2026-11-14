# Pelipper sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pelipper.py -- <the export's Pelipper.glb> <out.glb>
#
# From the references: a squat white pelican that is mostly beak. A huge yellow bucket of a lower bill in front, open
# on a dark red mouth that shows as a smile under a long flat yellow upper bill lying over it to a small hooked tip; a
# white body and a short thick neck rising behind to a small head with a blue cap that sweeps back to a point; eyes of
# white with a black rim and a black bar across the middle; long white wings hanging at the sides and a little
# forward, each ending in four blue feathers like fingers; short blue legs on broad blue three-toed feet; a few small
# blue tail feathers behind. Grown from Wingull's script: the same white, the same blue-tipped feathers, a yellow bill.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, BLUE, YELLOW, LID, MOUTH, INK = "#F5F7F9", "#3FA6EA", "#F6C62C", "#F8D348", "#8E2630", "#16181E"

HC = np.array([0, 0.05, 0.525])          # the head's centre
HR = np.array([0.07, 0.076, 0.068])

# ---- the body, a thick neck and a small head on top, behind the bill
k.add("body", ell((0, 0.1, 0.3), (0.13, 0.14, 0.14)), WHITE, "body")
k.add("neck", tube([(0, 0.085, 0.37), (0, 0.065, 0.44), (0, 0.055, 0.5)], [0.1, 0.082, 0.07]), WHITE, "head")
k.add("head", ell(HC, HR), WHITE, "head")
# The blue cap: the top of the head, its edge falling toward the back, and a tuft sweeping back to a point
k.add("crest", blade([(0, 0.06, 0.56), (0, 0.115, 0.595), (0, 0.165, 0.612)], [0.05, 0.03, 0.004], (1, 0, 0), 0.55),
      BLUE, "head")
k.paint(lambda P: inside(0.562 - P[:, 2] - 0.3 * (P[:, 1] - 0.05), 0.003), BLUE, on=["head", "crest", "neck"])

# ---- the bill: a great yellow bucket below, its rim falling toward the front so the dark red mouth shows as a smile
# under the flat lid of the upper bill lying over it
PC, PR = np.array([0, -0.12, 0.265]), np.array([0.19, 0.24, 0.16])
CUT, TILT = 0.6, 0.32
def bucket(V):
    V = V.copy()
    V[:, 2] = np.minimum(V[:, 2], CUT + TILT * V[:, 1])
    return V
k.add("pouch", ell(PC, PR, deform=bucket), YELLOW, "head", fuse=False, surface="shell")
slope = math.degrees(math.atan(TILT * PR[2] / PR[1]))
k.add("mouth", ell(PC + np.array([0, 0, PR[2] * CUT + 0.002]), (0.148, 0.19, 0.005), rot=(slope, 0, 0)), MOUTH, "head",
      fuse=False)
LIDLINE = [(0, 0.005, 0.455), (0, -0.12, 0.425), (0, -0.25, 0.402), (0, -0.36, 0.392), (0, -0.405, 0.388)]
k.add("bill", blade(LIDLINE, [0.075, 0.1, 0.095, 0.07, 0.03], (0, 0, 1), 0.24), LID, "head", fuse=False,
      surface="shell")
def hook(V):
    V = V.copy(); t = np.clip(-V[:, 1], 0, 1)
    V[:, 2] -= 0.5 * t * t
    return V
k.add("billtip", ell((0, -0.405, 0.383), (0.026, 0.03, 0.017), deform=hook), "#E9B21E", "head", fuse=False,
      surface="shell")

# ---- the wings: long and white, hanging at the sides a little forward, four blue feathers at each end
WING = [(0.11, 0.08, 0.41), (0.19, 0.065, 0.31), (0.235, 0.01, 0.2), (0.258, -0.045, 0.12)]
k.add("wing", blade(WING, [0.06, 0.075, 0.068, 0.054], (1, 0, 0.15), 0.42), WHITE, "wing{s}", mirror=True)
base = np.array(WING[-1])
for j, a in enumerate((-0.75, -0.25, 0.25, 0.75)):
    d = unit(0.05, -0.42 + 0.3 * a, -0.9 - 0.12 * a)
    end = base + d * (0.105 - 0.012 * abs(a)) + np.array([0, 0.035 * a, 0])
    k.add(f"wingfeather{j}", blade([base + np.array([0, 0.03 * a, 0.03]), (base + end) / 2 + np.array([0, 0.035 * a, 0]),
                                    end], [0.026, 0.024, 0.013], (1, 0, 0), 0.6), BLUE, "wing{s}", mirror=True)
k.paint(lambda P: inside(0.13 - P[:, 2], 0.003) * smoothstep(0.17, 0.2, np.abs(P[:, 0])), WHITE, on=["wing"])
k.paint(lambda P: inside(P[:, 2] - 0.13, 0.003) * smoothstep(0.17, 0.2, np.abs(P[:, 0])), BLUE,
        on=["wing", "wingfeather"])

# ---- the tail: three small blue feathers behind
for j, x in enumerate((0.0, 0.035, -0.035)):
    k.add(f"tail{j}", blade([(x * 0.4, 0.2, 0.27), (x, 0.28, 0.24), (x * 1.4, 0.33, 0.21)], [0.03, 0.022, 0.004],
                            (0, 0, 1), 0.4), BLUE, "tail")

# ---- short blue legs on broad three-toed feet, mostly behind the bucket
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    top, ank = np.array([0.06 * s, 0.06, 0.2]), np.array([0.07 * s, 0.02, 0.035])
    k.add(f"leg{sd}", tube([top, ank], [0.026, 0.022]), BLUE, f"leg{sd}", fuse=False)
    for j, a in enumerate((-26, 0, 26)):
        r_ = math.radians(a)
        d = np.array([math.sin(r_) * s, -math.cos(r_), -0.2]); d /= np.linalg.norm(d)
        k.add(f"toe{sd}{j}", blade([ank, ank + d * 0.05, ank + d * 0.09 + np.array([0, 0, -0.004])],
                                   [0.022, 0.02, 0.01], (0, 0, 1), 0.5), BLUE, f"leg{sd}", fuse=False)

k.build(voxel=0.006)

# ---- the eyes: white ovals with a black rim and a black bar across the middle; u is outward on both
CW, CK = srgb(WHITE), srgb(INK)
def eye(u, v):
    c = np.tile(CK, (len(u), 1))
    c[(u / 0.8) ** 2 + (v / 0.82) ** 2 < 1] = (1, 1, 1)
    c[(np.abs(v + 0.02) < 0.27) & ((u / 0.8) ** 2 + (v / 0.82) ** 2 < 1)] = CK
    return c
de = unit(0.66, -0.68, 0.22)
def eyes(state):
    k.decal(HC + de * 0.072, de, (0.027, 0.031), eye_look(state, eye, CW, CK, line=0.16), eps=0.004, rings=20, segs=56,
            mirror=True)

k.eye_states(eyes)
k.export(out)
