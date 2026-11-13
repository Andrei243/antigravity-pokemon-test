# Golduck sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golduck.py -- <the export's Golduck.glb> <out.glb>
# Grown from psyduck.py: the same cream bill of an upper and a lower half with a seam, nostrils and the same way of
# laying the eyes, now slanted under a dark brow as infernape.py lays them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, LIGHT, CREAM, SEAM, NOSTRIL = "#5C98DE", "#78B0EA", "#F6DE8C", "#C9B070", "#9E8A52"
MOUTH, CLAW, GEM = "#C8606E", "#F4F4F0", "#DC383C"
H = np.array([0, 0.0, 0.875])            # the head's centre
HR = np.array([0.112, 0.112, 0.118])

# A tall lean body, deep in the chest and narrowing to the hips, its front a lighter blue
def lean(V):
    V = V.copy()
    s = 1 - 0.12 * np.clip(-V[:, 2], 0, 1) + 0.04 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.0, 0.56), (0.15, 0.125, 0.2), deform=lean), BLUE, "body")
k.add("neck", tube([(0, 0.0, 0.7), (0, 0.0, 0.78), (0, 0.0, 0.83)], [0.072, 0.06, 0.06]), BLUE, "head")
k.add("head", ell(H, HR, rot=(-8, 0, 0)), BLUE, "head")
k.paint(lambda P: inside((P[:, 0] / 0.1) ** 2 + ((P[:, 2] - 0.54) / 0.16) ** 2 - 1, 0.05)
        * smoothstep(-0.06, -0.1, P[:, 1]), LIGHT, on=["body"])

# The long thick tail, out behind and rising to a point
k.add("tail", tube([(0, 0.06, 0.45), (0, 0.18, 0.37), (0, 0.32, 0.33), (0, 0.44, 0.36), (0, 0.53, 0.45), (0, 0.58, 0.55)],
                   [0.08, 0.072, 0.058, 0.042, 0.024, 0.005]), BLUE, "tail")

# Legs: thick thighs, a bent knee, slim shins down to wide webbed feet
k.add("thigh", tube([(0.1, 0.0, 0.44), (0.145, -0.04, 0.27), (0.15, 0.0, 0.1)], [0.098, 0.064, 0.045]), BLUE, "leg{s}",
      mirror=True)
k.add("foot", ell((0.15, -0.05, 0.028), (0.058, 0.09, 0.028)), BLUE, "leg{s}", mirror=True)
for i, a in enumerate((-30, 0, 30)):
    r = math.radians(a)
    d = np.array([math.sin(r), -math.cos(r), 0])
    b0 = np.array([0.15, -0.07, 0.025])
    tip = b0 + d * 0.11 + [0, 0, -0.008]
    k.add(f"toe{i}", tube([b0, b0 + d * 0.06, tip], [0.024, 0.02, 0.016]), BLUE, "leg{s}", mirror=True)
    k.add(f"claw{i}", tube([tip - d * 0.004, tip + d * 0.03 + [0, 0, -0.01]], [0.013, 0.002]), CLAW, "leg{s}",
          fuse=False, mirror=True, surface="shell")
# the cream webs between the toes, a thin plate just under their tops
k.add("web", ell((0.15, -0.14, 0.026), (0.07, 0.05, 0.009), deform=lambda V: V * np.array([1, 1, 1.0])), CREAM,
      "leg{s}", fuse=False, mirror=True, surface="skin")
k.add("heel", tube([(0.15, 0.02, 0.03), (0.15, 0.07, 0.008)], [0.012, 0.002]), CLAW, "leg{s}", fuse=False,
      mirror=True, surface="shell")

# Arms out and forward, three webbed fingers clawed white
k.add("arm", tube([(0.13, -0.02, 0.69), (0.25, -0.08, 0.6), (0.3, -0.2, 0.6)], [0.052, 0.042, 0.036]), BLUE, "arm{s}",
      mirror=True)
k.add("hand", ell((0.31, -0.25, 0.598), (0.045, 0.05, 0.028)), BLUE, "arm{s}", mirror=True)
for i, dx in enumerate((-0.035, 0.0, 0.035)):
    b0 = np.array([0.31 + dx * 0.5, -0.27, 0.598])
    tip = np.array([0.31 + dx * 1.5, -0.35, 0.565])
    k.add(f"finger{i}", tube([b0, (b0 + tip) / 2 + [0, 0, 0.006], tip], [0.019, 0.017, 0.014]), BLUE, "arm{s}",
          mirror=True)
    d = unit(*(tip - b0))
    k.add(f"nail{i}", tube([tip, tip + d * 0.012 + [0, -0.004, -0.028]], [0.011, 0.002]), CLAW, "arm{s}", fuse=False,
          mirror=True, surface="shell")
k.add("palm", ell((0.31, -0.315, 0.588), (0.058, 0.045, 0.008)), CREAM, "arm{s}", fuse=False, mirror=True,
      surface="skin")
k.add("thumb", tube([(0.285, -0.24, 0.59), (0.27, -0.29, 0.57)], [0.016, 0.013]), BLUE, "arm{s}", mirror=True)
k.add("tclaw", tube([(0.27, -0.29, 0.57), (0.265, -0.31, 0.545)], [0.01, 0.002]), CLAW, "arm{s}", fuse=False,
      mirror=True, surface="shell")

# The crest: four points swept up and back over the head, the inner two tall, the outer two out to the sides
for i, (base, mid, tip, r) in enumerate([
        ((0.03, 0.02, 0.95), (0.045, 0.09, 1.02), (0.06, 0.19, 1.09), 0.055),
        ((0.075, 0.04, 0.91), (0.13, 0.1, 0.95), (0.2, 0.17, 0.99), 0.045)]):
    k.add(f"crest{i}", blade([base, mid, tip], [r, r * 0.55, 0.004], (0.3, 0, 1) if i else (1, 0, 0.3), 0.45), BLUE,
          "head", fuse=False, mirror=True)
k.add("ridge", ell((0, 0.05, 0.95), (0.075, 0.06, 0.05)), BLUE, "head")

# The bill, Psyduck's grown long: broad at its rounded tip, drawn in at the base, open a little on the pink mouth
def bill(V):
    V = V.copy()
    t = np.clip(-V[:, 1], -1, 1)
    V[:, 0] *= 0.62 + 0.42 * smoothstep(-0.95, 0.4, t)
    V[:, 2] *= 1 - 0.2 * np.clip(t, 0, 1)
    V[:, 2] += 0.6 * V[:, 0] ** 2 * (0.4 + 0.6 * np.clip(0.3 - t, 0, 1))
    return V
BC, BR, BROT = (0, -0.21, 0.835), (0.062, 0.15, 0.038), (16, 0, 0)
def on_bill(U, c=BC, r=BR, rot=BROT):
    V = bill(np.array(U, float)) * np.array(r)
    m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
    return V @ m.T + np.array(c)
k.add("bill", ell(BC, BR, rot=BROT, deform=bill), CREAM, "head", fuse=False, surface="shell")
k.add("lowbill", ell((0, -0.17, 0.775), (0.05, 0.12, 0.026), rot=(30, 0, 0), deform=bill), CREAM, "head",
      fuse=False, surface="shell")
k.add("mouth", ell((0, -0.125, 0.8), (0.045, 0.075, 0.03), rot=(22, 0, 0)), MOUTH, "head", fuse=False, surface="skin")
a = np.radians(np.linspace(-118, 118, 31))
seam = on_bill(np.stack([np.sin(a), -np.cos(a), np.full_like(a, -0.2)], 1)) * np.array([0.985, 1, 1]) \
    + np.array([0, -0.003, 0])
k.add("seam", tube(seam, [0.0035] * len(seam), seg=10, per=3), SEAM, "head", fuse=False, surface="shell")
k.add("nostril", ell((0.016, -0.3, 0.826), (0.009, 0.006, 0.004)), NOSTRIL, "head", fuse=False, mirror=True,
      surface="shell")

# The red gem on the brow
k.add("gem", ell((0, -0.084, 0.945), (0.015, 0.011, 0.015)), GEM, "head", fuse=False, glow="#E03A3A",
      surface="shell")

# The eyes: slanted almonds under a dark brow that cuts their tops down toward the gem; white, a red iris, a dark
# pupil. In each eye's own frame u runs outward and v up.
HE = 0.033
SL = 0.5
def eye_top(u):
    return 0.28 + SL * u
def eye_bottom(u):
    return eye_top(u) - 0.85 * (1 - u * u) ** 0.8
ALMOND = [(u, eye_top(u) + 0.14) for u in np.linspace(1, -1, 9)] + \
         [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]
de = unit(0.5, -0.82, 0.22)
E = H + de * np.array([0.108, 0.11, 0.12]) + np.array([0, 0, 0.0])
INK, LID = srgb("#1A2236"), srgb(BLUE)
SCLERA, IRIS, IRISRIM = srgb("#FAFAF6"), srgb("#D8302E"), srgb("#7A1414")
RI = 0.4
UC = -0.15
VC = eye_top(UC) - (RI - 0.05) * math.sqrt(1 + SL * SL)
def lid_cut(state, u):
    return np.minimum(eye_top(u), 0.06 + 0.7 * u) if state == "Fierce" else eye_top(u)
def eye_open(state):
    def look(u, v):
        c = np.tile(SCLERA, (len(u), 1))
        cut = lid_cut(state, u)
        c[v > cut - 0.16] = INK
        c[v > cut + 0.14] = LID
        c[v < eye_bottom(u) + 0.07] = INK
        return c
    return look
def iris_look(state):
    def look(a, b):
        u, v = UC + a * RI, VC + b * RI
        r = np.sqrt(a * a + b * b)
        c = np.tile(IRIS, (len(a), 1))
        c[r > 0.8] = IRISRIM
        c[r < 0.42] = INK
        c[((a + 0.25) / 0.2) ** 2 + ((b - 0.3) / 0.2) ** 2 < 1] = (1, 1, 1)
        c[v > lid_cut(state, u) - 0.16] = INK
        c[v > lid_cut(state, u) + 0.14] = LID
        c[(v < eye_bottom(u) + 0.07) | (np.abs(u) > 0.95)] = INK
        c[v < eye_bottom(u)] = LID
        return c
    return look
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        c, n = E * m, de * m
        right, up = frame(n)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        if state in ("Open", "Fierce"):
            k.decal(c, n, (HE, HE), flip(eye_open(state)), eps=0.007, outline=outline, rings=20, segs=56)
            ic = c + right * side * UC * HE + up * VC * HE
            k.decal(ic, n, (RI * HE, RI * HE), flip(iris_look(state)), eps=0.009, rings=12, segs=40)
        else:
            k.decal(c, n, (HE, HE), flip(eye_look(state, eye_open("Open"), LID, INK)), eps=0.007, outline=outline,
                    rings=20, segs=56)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
