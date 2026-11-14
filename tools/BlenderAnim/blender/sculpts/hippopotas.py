# Hippopotas sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hippopotas.py -- <the export's Hippopotas.glb> <out.glb>
# A little sand hippo: a great round tan head with a brown-rimmed nostril splayed to each side of its snout,
# its eyes peering out of a brown mound with a scalloped skirt on the crown, a barrel of a body in sandy
# camouflage with a brown vent on the back, short thick legs with pale nails, a stub of a tail. (The male's colours.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

TAN, PATCH, DARKPATCH, BROWN, HOLE, NAIL = "#E4C886", "#C49A5C", "#A87440", "#94582E", "#1A1210", "#EDEAE2"
H = np.array([0.0, -0.15, 0.19])           # the head's middle
HR = np.array([0.13, 0.17, 0.13])

# The body: a barrel behind the head, and the head itself, the larger of the two
k.add("body", ell((0, 0.11, 0.17), (0.12, 0.16, 0.125)), TAN, "body")
k.add("neck", ell((0, -0.01, 0.2), (0.11, 0.08, 0.11)), TAN, "body")
k.add("head", ell(H, HR), TAN, "head")
# The vent on the back: a low brown mound toward the rear
k.add("vent", ell((0, 0.17, 0.285), (0.05, 0.045, 0.03)), BROWN, "body")
k.add("tail", tube([(0, 0.25, 0.2), (0, 0.285, 0.19), (0, 0.305, 0.18)], [0.032, 0.022, 0.01]), TAN, "tail")

# The legs: short and thick, pale nails at the front of each foot
for name, y, bone in (("leg", -0.06, "leg{s}"), ("hind", 0.17, "hind{s}")):
    k.add(name, tube([(0.085, y, 0.13), (0.09, y - 0.005, 0.054)], [0.055, 0.053]), TAN, bone, mirror=True)
    for j, dx in enumerate((-0.025, 0.0, 0.025)):
        k.add(f"{name}nail{j}", ell((0.09 + dx, y - 0.05, 0.02), (0.013, 0.012, 0.011)), NAIL, bone, fuse=False,
              mirror=True, surface="shell")

# The camouflage: blotches of two browns over the back, sides and hind legs, ragged at their edges
BLOTS = [((0.07, 0.06, 0.28), 0.05, PATCH), ((-0.08, 0.1, 0.27), 0.045, PATCH), ((0.12, 0.14, 0.17), 0.045, DARKPATCH),
         ((-0.12, 0.17, 0.15), 0.05, DARKPATCH), ((0.0, 0.25, 0.22), 0.04, PATCH), ((0.1, 0.22, 0.08), 0.04, PATCH),
         ((-0.09, 0.03, 0.15), 0.035, DARKPATCH), ((0.04, 0.2, 0.28), 0.035, DARKPATCH), ((-0.11, 0.24, 0.07), 0.035, PATCH)]
for i, (c, r, col) in enumerate(BLOTS):
    c = np.array(c)
    def blot(P, c=c, r=r, i=i):
        d = P - c
        th = np.arctan2(d[:, 2], d[:, 1]) + np.arctan2(d[:, 0], d[:, 1])
        rag = 1 + 0.18 * np.sin(3 * th + i) + 0.1 * np.sin(5 * th + 2 * i)
        return inside(np.linalg.norm(d, axis=1) - r * rag, 0.004)
    k.paint(blot, col, on=["body", "hind", "tail"])

# The holes: a brown rim standing round each, dark within
def ring(c, n, r, t, name, bone, mirror=False):
    n = unit(*n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(n, a)
    pts = [np.array(c) + r * (math.cos(q) * a + math.sin(q) * b) for q in np.linspace(0, 2 * math.pi, 25)]
    k.add(name, tube(pts, [t] * len(pts)), BROWN, bone, mirror=mirror)
    k.add(name + "hole", ell(np.array(c) - n * 0.01, (r * 0.85, r * 0.85, r * 0.85)), HOLE, bone, carve=True,
          mirror=mirror)


ns = unit(0.94, -0.34, -0.02)                # the nostrils, splayed to either side of the snout
ring(H + ns * HR * 0.99, ns, 0.04, 0.013, "nostril", "head", mirror=True)

# The mound on the crown the eyes peer out of: a skirt of round lobes, and a bulb above it
k.add("cap", ell((0, -0.08, 0.32), (0.06, 0.055, 0.035)), BROWN, "head")
for i, (x, y) in enumerate([(0.06, -0.06), (0.05, -0.02), (0.035, -0.11)]):
    k.add(f"lobe{i}", ell((x, y, 0.31), (0.03, 0.028, 0.018)), BROWN, "head", mirror=True)
k.add("lobeback", ell((0, -0.02, 0.31), (0.035, 0.03, 0.018)), BROWN, "head")
k.add("bulb", ell((0, -0.1, 0.37), (0.078, 0.05, 0.045), deform=boxy(2.4)), BROWN, "head")

# The eyes: two tall white ovals side by side on the bulb's front, a black pupil in each, the brown all round
INK, WHITE, LID = srgb("#18120E"), np.array([1.0, 1.0, 1.0]), srgb(BROWN)
def eye(u, v):
    return np.tile(WHITE, (len(u), 1))
de = unit(0.35, -0.94, 0.1)
EC = np.array([0.035, -0.148, 0.376])
RIGHT = np.cross((0, 0, 1.0), de); RIGHT /= np.linalg.norm(RIGHT)          # outward
UP = np.cross(de, RIGHT)
def eyes(state):
    k.decal(EC, de, (0.029, 0.034), eye_look(state, eye, LID, INK, line=0.16), eps=0.005, rings=20, segs=56, mirror=True)
    if state in ("Open", "Fierce"):
        # The pupil, a patch of its own so that its edge is round, a little toward the middle
        pc = EC - RIGHT * 0.0035 - UP * (0.0015 if state == "Open" else 0.008)
        k.decal(pc, de, (0.0125, 0.017 if state == "Open" else 0.012), INK, eps=0.008, rings=8, segs=32, mirror=True)

k.build(voxel=0.005, smooth=3)
k.eye_states(eyes)
k.export(out)
