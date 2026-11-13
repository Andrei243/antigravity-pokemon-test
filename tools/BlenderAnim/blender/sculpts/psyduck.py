# Psyduck sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/psyduck.py -- <the export's Psyduck.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, CREAM, HAIR, NOSTRIL, SEAM = "#F7CC45", "#F7EDC6", "#26262B", "#B8A878", "#C9B886"
H = np.array([0, -0.015, 0.64])          # the head's centre
HR = np.array([0.215, 0.19, 0.185])

# A pear of a body, wider below, and a round head on it with no neck
def pear(V):
    V = V.copy()
    s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.01, 0.262), (0.208, 0.192, 0.232), deform=pear), YELLOW, "body")
k.add("head", ell(H, HR), YELLOW, "head")

# A short stubby tail, pointed, sticking out behind
k.add("tail", blade([(0, 0.13, 0.17), (0, 0.21, 0.15), (0, 0.27, 0.16)], [0.06, 0.04, 0.006], (0, 0, 1), 0.55),
      YELLOW, "tail")

# The legs are stubs inside the body (they carry its bottom in a step), so only the wide cream webbed feet show
k.add("leg", tube([(0.09, 0.0, 0.14), (0.098, -0.015, 0.088)], [0.05, 0.042]), YELLOW, "leg{s}", mirror=True)
def toes(V):
    ph = np.arctan2(V[:, 0], -V[:, 1])
    front = np.clip(-V[:, 1], 0, 1)
    s = 1 + 0.18 * np.cos(3 * ph) * front
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("foot", ell((0.105, -0.065, 0.024), (0.075, 0.11, 0.024), rot=(0, 0, -10), deform=toes), CREAM, "leg{s}",
      fuse=False, mirror=True, surface="skin")

# The arms up, the hands pressed to the sides of the head, two fingers pointing up over each
k.add("arm", tube([(0.15, -0.02, 0.38), (0.24, -0.035, 0.46), (0.255, -0.025, 0.56), (0.23, -0.015, 0.65)],
                  [0.064, 0.058, 0.054, 0.052]), YELLOW, "arm{s}", mirror=True)
k.add("hand", ell((0.215, -0.01, 0.695), (0.05, 0.068, 0.072), rot=(0, -30, 0)), YELLOW, "arm{s}", mirror=True)
for i, (y, dz) in enumerate([(-0.045, 0.0), (0.0, 0.006), (0.045, -0.01)]):
    k.add(f"finger{i}", blade([(0.2, y, 0.735 + dz), (0.188, y, 0.77 + dz), (0.175, y, 0.79 + dz)],
                              [0.022, 0.014, 0.004], (1, 0, 0.6), 0.6), YELLOW, "arm{s}", mirror=True)

# The wide flat bill, broadest at its rounded tip and drawn in toward the cheeks at its base (the sides of its
# outline seen from above are hollow there), its sides curving up as a smile; a lower bill under it
def bill(V):
    V = V.copy()
    t = np.clip(-V[:, 1], -1, 1)                       # 1 at the tip, -1 at the base
    V[:, 0] *= 0.5 + 0.58 * smoothstep(-0.95, 0.35, t)
    V[:, 2] *= 1 - 0.22 * np.clip(t, 0, 1)
    back = np.clip(0.3 - t, 0, 1)                      # 0 over the front half, 1 at the base
    V[:, 2] += 1.15 * V[:, 0] ** 2 * (0.5 + 0.5 * back)
    return V
BC, BR, BROT = (0, -0.29, 0.552), (0.178, 0.155, 0.06), (6, 0, 0)
def on_bill(U, c=BC, r=BR, rot=BROT):
    """Points of the unit sphere put where the bill's own shape puts them."""
    V = bill(np.array(U, float)) * np.array(r)
    m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
    return V @ m.T + np.array(c)
k.add("bill", ell(BC, BR, rot=BROT, deform=bill), CREAM, "head", fuse=False, surface="shell")
k.add("lowbill", ell((0, -0.26, 0.516), (0.13, 0.112, 0.038), rot=(8, 0, 0), deform=bill), CREAM, "head",
      fuse=False, surface="shell")
# The line between the bills: the upper bill's rim from corner to corner, a little under it, rising at the corners
a = np.radians(np.linspace(-118, 118, 31))
seam = on_bill(np.stack([np.sin(a), -np.cos(a), np.full_like(a, -0.18)], 1)) * np.array([0.985, 1, 1]) \
    + np.array([0, -0.004, 0])
k.add("seam", tube(seam, [0.0042] * len(seam), seg=10, per=3), SEAM, "head", fuse=False, surface="shell")
k.add("nostril", ell((0.022, -0.27, 0.612), (0.011, 0.007, 0.005)), NOSTRIL, "head", fuse=False, mirror=True,
      surface="shell")

# Three black hairs out of the crown, fanning out
for i, x in enumerate((-0.045, 0.0, 0.045)):
    k.add(f"hair{i}", tube([(x * 0.4, 0.0, 0.79), (x * 1.0, -0.005, 0.86), (x * 2.0, 0.005, 0.9)], [0.018, 0.017, 0.015]),
          HAIR, "head", fuse=False, surface="hair")

# The eyes: big round whites with a black rim and a tiny pupil, vacant
BLACK, WHITE, LID = srgb("#141414"), np.array([1.0, 1.0, 1.0]), srgb(YELLOW)
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[r > 0.87] = BLACK
    return c
de = unit(0.45, -0.87, 0.2)
E = H + de * 0.195
# The tiny pupil is a patch of its own over the white: its outline is exact, where a dot a few rings across in the
# eye's own patch came out a star
def eyes(state):
    k.decal(E, de, (0.066, 0.064), eye_look(state, eye, LID, BLACK), eps=0.007, rings=20, segs=56, mirror=True)
    if state in ("Open", "Fierce"):
        k.decal(E, de, (0.0115, 0.0115), BLACK, eps=0.0095, rings=6, segs=40, mirror=True)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
