# Buneary sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/buneary.py -- <the export's Buneary.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BROWN, CREAM, PINK, INK = "#A5674A", "#F3DE98", "#E8909E", "#1A1414"
H = np.array([0, -0.01, 0.6])            # the head's centre
HR = np.array([0.17, 0.15, 0.152])


def puff(name, c, r, bone, n=8, tilt=0.0):
    """A cloud of fleece: a core and fused balls round it, a few lower ones fuller."""
    c = np.array(c, float)
    k.add(name, ell(c, (r * 0.78, r * 0.74, r * 0.74)), CREAM, bone)
    g = math.pi * (3 - math.sqrt(5))
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        rr = math.sqrt(1 - z * z)
        a = g * i + tilt
        d = np.array([rr * math.cos(a), rr * math.sin(a), z * 0.8])
        k.add(f"{name}{i}", ell(c + d * r * 0.48, (r * 0.5, r * 0.5, r * 0.48)), CREAM, bone)


# A round brown head on a small brown body
k.add("head", ell(H, HR, deform=boxy(2.2)), BROWN, "head")
k.add("torso", ell((0, 0.0, 0.39), (0.098, 0.082, 0.105)), BROWN, "body")
k.add("neck", ell((0, -0.005, 0.46), (0.085, 0.075, 0.05)), BROWN, "body")

# Short arms held straight out, a little down, rounded ends
k.add("arm", tube([(0.07, -0.015, 0.415), (0.14, -0.025, 0.402), (0.19, -0.03, 0.39)], [0.04, 0.037, 0.036]),
      BROWN, "arm{s}", mirror=True)
k.add("hand", ell((0.198, -0.032, 0.388), (0.04, 0.037, 0.036)), BROWN, "arm{s}", mirror=True)

# The cream fleece round its middle: a puffy skirt of fused balls over the hips, low over the legs
k.add("fleececore", ell((0, 0.005, 0.255), (0.135, 0.12, 0.1)), CREAM, "body")
for i in range(10):
    a = 2 * math.pi * (i + 0.5) / 10
    k.add(f"fleece{i}", ell((0.12 * math.sin(a), -0.105 * math.cos(a), 0.28 + 0.014 * math.cos(3 * a)),
                            (0.066, 0.063, 0.06)), CREAM, "body")
for i in range(9):
    a = 2 * math.pi * i / 9
    k.add(f"fleecelow{i}", ell((0.115 * math.sin(a), -0.1 * math.cos(a), 0.195), (0.062, 0.058, 0.055)), CREAM, "body")

# Short cream legs and big feet below the fleece
k.add("leg", tube([(0.072, 0.0, 0.19), (0.078, -0.005, 0.11), (0.08, -0.01, 0.06)], [0.058, 0.055, 0.052]), CREAM,
      "leg{s}", mirror=True)
k.add("foot", ell((0.082, -0.045, 0.038), (0.058, 0.085, 0.038)), CREAM, "leg{s}", mirror=True)

# A cream puff of a tail behind
puff("tail", (0, 0.17, 0.24), 0.075, "tail", n=7)

# The standing ear on its right (+X): tall, flat front to back, a pink lining and a cream cloud at its tip
k.add("ear", blade([(0.072, 0.0, 0.69), (0.09, 0.005, 0.8), (0.1, 0.012, 0.9)], [0.05, 0.054, 0.046], (0, 1, 0), 0.45),
      BROWN, "earR")
puff("eartuft", (0.106, 0.014, 0.955), 0.088, "earR", n=9)
LENS = polygon([(0, 1), (0.6, 0.6), (1, 0.0), (0.7, -0.6), (0, -1), (-0.7, -0.6), (-1, 0.0), (-0.6, 0.6)])
k.decal((0.089, -0.03, 0.795), unit(0, -1, 0.0), (0.024, 0.085), PINK, up=unit(0.15, 0, 1), eps=0.003,
        outline=LENS, rings=10, segs=48)

# The rolled ear on its left (-X): a brown roll at the head's side, its cream cloud at the outer end
k.add("roll", tube([(-0.09, 0.03, 0.69), (-0.15, 0.045, 0.695), (-0.2, 0.05, 0.68)], [0.065, 0.08, 0.082]), BROWN,
      "earL")
puff("rolltuft", (-0.25, 0.035, 0.67), 0.085, "earL", n=9, tilt=0.6)

# Two cream dots over the eyes
dd = unit(0.4, -0.8, 0.45)
k.decal(H + dd * 0.16, dd, (0.013, 0.011), CREAM, eps=0.003, rings=5, segs=20, mirror=True)

# A small pink nose and a little smile
NOSE = polygon([(-1, 0.7), (1, 0.7), (0, -1)])
k.decal((0, -0.16, 0.567), unit(0, -1, -0.1), (0.014, 0.011), PINK, eps=0.004, outline=NOSE, rings=6, segs=24)
CB, CI = srgb(BROWN), srgb("#3A1E16")
def smile(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.25 + 0.7 * u * u)) < 0.2) & (np.abs(u) < 0.9)] = CI
    c[(np.abs(u) < 0.09) & (v > -0.2)] = CI
    return c
k.decal((0, -0.158, 0.538), unit(0, -1, -0.3), (0.032, 0.02), smile, eps=0.003, rings=8, segs=40)

# Big dark eyes with a white glint, wide apart and a little low on the face
CE = srgb(INK)
def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u - 0.18) / 0.32) ** 2 + ((v - 0.38) / 0.28) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.45, -0.88, -0.08)
E = H + de * 0.15
def eyes(state):
    k.decal(E, de, (0.028, 0.039), eye_look(state, eye, CB, CE), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
