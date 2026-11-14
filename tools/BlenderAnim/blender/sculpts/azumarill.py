# Azumarill sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Marill's script (marill.py): the same blue, white belly, pink ear linings, black eyes with a glint, open smile and
# black zigzag tail with its blue ball; the ball of a body grown into an egg and the round ears into a rabbit's.
#   blender --background --factory-startup --python sculpts/azumarill.py -- <the export's Azumarill.glb> <out.glb>
#
# The design, from the references: an egg of a body, white below a wavy line like water a little under its middle,
# white bubbles of several sizes above the line; long rabbit's ears, pink inside, the one on its right standing up
# and out, the one on its left folded over outward at its top third; short stubby arms out to the sides; short legs
# with round feet; small black eyes high on the egg; an open smile; a short black zigzag tail low behind its right,
# ending in a small blue ball near the ground.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, PINK, INK, WHITE, BELLY = "#3AA6EC", "#F0607A", "#16161E", "#FFFFFF", "#E6EEF2"
MOUTH, TONGUE = "#8A2638", "#F08A9A"
B = np.array([0, -0.004, 0.255])          # the egg's middle
BR = np.array([0.175, 0.16, 0.225])
TAPER = 0.13


def egg(V):
    V = V.copy()
    s = 1 - TAPER * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V


def front(x, z):
    """The point of the egg's front at (x, z), and its outward normal."""
    zn = (z - B[2]) / BR[2]
    s = 1 - TAPER * zn
    q = max(0.0, 1 - zn * zn - (x / (BR[0] * s)) ** 2)
    p = np.array([x, B[1] - BR[1] * s * math.sqrt(q), z])
    n = (p - B) / (BR * np.array([s, s, 1])) ** 2
    return p, n / np.linalg.norm(n)


# The egg; its top on the head's bone, so that the face nods with it
k.add("body", ell(B, BR, deform=egg), BLUE, "body")
k.add("crown", ell((0, -0.006, 0.4), (0.108, 0.098, 0.076)), BLUE, "head")

# White below a wavy line like water a little under the middle, crisp
def belly(P):
    a = np.arctan2(P[:, 0], -P[:, 1])
    line = 0.205 + 0.011 * np.sin(a * 4.5 + 0.6)
    return inside(P[:, 2] - line, 0.0025) * (P[:, 2] > 0.0)
k.paint(belly, BELLY, on=["body", "crown"])

# Long rabbit's ears, pink inside: its right standing up and out, its left folded over outward at the top
EARS = {
    "R": [(0.05, 0.012, 0.43), (0.075, 0.018, 0.53), (0.105, 0.024, 0.63), (0.13, 0.03, 0.715)],
    "L": [(-0.05, 0.012, 0.43), (-0.075, 0.018, 0.53), (-0.1, 0.022, 0.61), (-0.15, 0.024, 0.655),
          (-0.215, 0.026, 0.665)],
}
LINING = {"R": 0.9, "L": 0.62}               # how far up each ear its lining goes
for s, pts in EARS.items():
    n = len(pts)
    radii = [0.04, 0.05, 0.047, 0.03] if n == 4 else [0.04, 0.05, 0.047, 0.04, 0.026]
    k.add(f"ear{s}", blade(pts, radii, (0, 1, 0), 0.34), BLUE, f"ear{s}")


def ear_lining(s):
    pts = np.array(EARS[s], float)
    line = np.vstack([pts[i] + (pts[i + 1] - pts[i]) * t for i in range(len(pts) - 1)
                      for t in np.linspace(0, 1, 30)])
    length = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(line, axis=0), axis=1))])
    length /= length[-1]
    def mask(P):
        j = np.concatenate([((P[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1)
                            for a in range(0, len(P), 4000)])
        off = P - line[j]
        across = np.sqrt(off[:, 0] ** 2 + off[:, 2] ** 2)
        w = 0.03 * np.sin(np.clip(length[j], 0, 1) * math.pi) ** 0.4
        return (inside(across - w, 0.003) * inside(length[j] - LINING[s], 0.02) * (length[j] > 0.1)
                * (off[:, 1] < -0.003))
    return mask
for s in EARS:
    k.paint(ear_lining(s), PINK, on=[f"ear{s}"])

# Short stubby arms out to the sides, a little down and forward
k.add("arm", tube([(0.14, -0.035, 0.285), (0.2, -0.055, 0.265), (0.245, -0.07, 0.245)], [0.04, 0.036, 0.034]),
      BLUE, "arm{s}", mirror=True)

# Short legs and round feet
k.add("leg", tube([(0.075, -0.02, 0.09), (0.08, -0.035, 0.04)], [0.04, 0.038]), BLUE, "leg{s}", mirror=True)
k.add("foot", ell((0.082, -0.055, 0.026), (0.042, 0.058, 0.027)), BLUE, "leg{s}", mirror=True)

# The tail: a short black zigzag low behind its right, its face turned to the front three-quarter, ending in a small
# blue ball near the ground
O = np.array([0.03, 0.15, 0.13])
TB = np.array([0.17, 0.25, 0.058])         # the ball's middle
L = TB - O
LD = unit(*L)
face = unit(0.7, -0.45, 0.3)
face = face - LD * np.dot(face, LD); face /= np.linalg.norm(face)
SD = np.cross(face, LD)
ZP = [O + L * t + SD * a for t, a in ((0.0, 0.0), (0.2, 0.03), (0.4, -0.03), (0.6, 0.03), (0.78, -0.025),
                                       (0.86, 0.0))]
for i in range(len(ZP) - 1):
    a, b = ZP[i], ZP[i + 1]
    k.add(f"tail{i}", blade([a, (a + b) / 2, b], [0.016, 0.016, 0.016], face, 0.5), INK, "tail", fuse=False)
k.add("tailball", ell(TB, (0.05, 0.05, 0.05)), BLUE, "tail")

# White bubbles of several sizes above the belly's line, on the front and round the sides
for i, (x, z, r) in enumerate([(-0.088, 0.33, 0.016), (-0.142, 0.25, 0.018), (-0.084, 0.278, 0.027),
                               (0.012, 0.276, 0.024), (0.078, 0.292, 0.024), (0.076, 0.236, 0.012),
                               (0.142, 0.27, 0.018), (0.13, 0.33, 0.011)]):
    p, nrm = front(x, z)
    k.decal(p, nrm, (r, r), WHITE, eps=0.003, rings=8, segs=36)

# An open smile with a pink tongue, flat along its top
CM, CT, CB, CI, CW = srgb(MOUTH), srgb(TONGUE), srgb(BLUE), srgb(INK), srgb(WHITE)
def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[(u / 0.7) ** 2 + ((v + 0.75) / 0.6) ** 2 < 1] = CT
    return c
def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.3 / s) if s > 0 else 1.0
p, nrm = front(0, 0.36)
k.decal(p, nrm, (0.028, 0.026), mouth, eps=0.003, outline=mouth_shape, rings=20, segs=60)

# Small black eyes with a white glint, high on the egg
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u - 0.12) / 0.32) ** 2 + ((v - 0.3) / 0.3) ** 2 < 1] = CW
    return c
E, de = front(0.058, 0.4)
def eyes(state):
    k.decal(E, de, (0.018, 0.024), eye_look(state, eye, CB, CI, line=0.2), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.0055)
k.eye_states(eyes)
k.export(out)
