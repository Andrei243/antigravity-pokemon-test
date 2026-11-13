# Wurmple sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wurmple.py -- <the export's Wurmple.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

RED, CREAM, YELLOW, WHITE, SEAM = "#E0505E", "#F0EACC", "#F2D88C", "#E8EAF0", "#B9AE88"

# The body: a segment on each bone's joint, low along the ground at the back, the front half rising to the head
CHAIN = [("tail", (0, 0.31, 0.062), 0.056), ("body", (0, 0.24, 0.07), 0.066), ("seg1", (0, 0.14, 0.08), 0.07),
         ("seg2", (0, 0.04, 0.095), 0.072), ("seg3", (0, -0.04, 0.16), 0.072), ("seg4", (0, -0.08, 0.25), 0.074)]
for bone, at, r in CHAIN:
    k.add(f"seg_{bone}", ell(at, (r * 1.02, r * 0.92, r)), RED, bone)
HC = np.array([0.0, -0.105, 0.375])          # the head's middle
k.add("skull", ell(HC, (0.096, 0.098, 0.092)), RED, "head")
# The muzzle: two round cream lobes side by side at the front of the head
k.add("lobe", ell((0.036, -0.17, 0.352), (0.052, 0.05, 0.056)), CREAM, "head", mirror=True)

# The axis the body runs along, from the tail's end to the head, for its belly and its seams
AX = np.array([(0, 0.36, 0.062)] + [at for _, at, _ in CHAIN[1:]] + [(0, -0.095, 0.31)], float)
S = np.vstack([AX[i] + (AX[i + 1] - AX[i]) * t for i in range(len(AX) - 1) for t in np.linspace(0, 1, 30, endpoint=False)]
              + [AX[-1:]])
TS = np.gradient(S, axis=0); TS /= np.linalg.norm(TS, axis=1, keepdims=True)
ARC = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(S, axis=0), axis=1))])
BELLY = np.cross(np.array([1.0, 0, 0])[None], TS)         # down at the back, forward up the raised front
def along(P):
    i = np.argmin(((P[:, None, :] - S[None]) ** 2).sum(2), axis=1)
    return S[i], BELLY[i], ARC[i], TS[i]

# Cream underneath and up the front of the raised half (the front alone: red wraps its back and sides), with a
# clean line where the red back meets it
def belly(P):
    c, d, _, _ = along(P)
    q = P - c; q /= np.linalg.norm(q, axis=1, keepdims=True)
    edge = 0.15 + 0.2 * smoothstep(0.1, -0.04, P[:, 1])          # the red reaches round the raised half's sides
    return inside(edge - (q * d).sum(1), 0.006)
k.paint(belly, CREAM, on=["seg"])
# Seams across the cream between one segment and the next
JOINT_ARC = [ARC[np.argmin(((S - np.array(at)) ** 2).sum(1))] for _, at, _ in CHAIN[1:]]
CUTS = [(a + b) / 2 for a, b in zip(JOINT_ARC[:-1], JOINT_ARC[1:])] + [JOINT_ARC[-1] + 0.045]
def seams(P):
    c, d, s, _ = along(P)
    q = P - c; q /= np.linalg.norm(q, axis=1, keepdims=True)
    m = np.zeros(len(P))
    for cut in CUTS:
        m = np.maximum(m, inside(np.abs(s - cut) - 0.0035, 0.002))
    return m * smoothstep(0.25, 0.45, (q * d).sum(1))
k.paint(seams, SEAM, on=["seg"])

# The head: red over its top and back (the skull is red), the cream muzzle in front
k.paint(lambda P: inside(((P - HC) @ unit(0, 0.72, 0.7)) + 0.012, 0.005), CREAM, on=["skull", "lobe"])
# The cleft between the muzzle's lobes, a soft line down the middle
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.0035, 0.0025) * smoothstep(-0.18, -0.2, P[:, 1]) *
        inside(P[:, 2] - 0.39), SEAM, on=["lobe"])

# The big red horn rising from the top of the head and sweeping back
k.add("horn", tube([(0, -0.085, 0.44), (0, -0.06, 0.52), (0, -0.02, 0.59), (0, 0.03, 0.64)], [0.05, 0.034, 0.02, 0.004]),
      RED, "head")
# The ragged red fringe: spikes round the back of each low segment, down the back of the raised ones and the head
# Each spike is a loose piece, so the remesh does not round it off: it starts inside the body and ends in a point
def spike(name, base, d, length, r, bone):
    d = unit(*d)
    b = np.array(base)
    k.add(name, tube([b - d * 0.02, b + d * length * 0.45, b + d * length], [r, r * 0.55, 0.0008]), RED, bone,
          fuse=False)
for bone, at, r in CHAIN[1:4]:
    at = np.array(at)
    for j, a in enumerate((-6, 28, 62, 90, 118, 152, 186)):
        d = np.array([math.cos(math.radians(a)), 0.25, math.sin(math.radians(a))])
        base = at + np.array([math.cos(math.radians(a)), 0, math.sin(math.radians(a))]) * r * 0.9 + [0, 0.02, 0]
        spike(f"fringe_{bone}_{j}", base, d, 0.04, 0.016, bone)
for bone, at, r in CHAIN[4:]:
    at = np.array(at)
    for j, a in enumerate((28, 62, 90, 118, 152)):
        d = np.array([math.cos(math.radians(a)), math.sin(math.radians(a)), 0.15])
        base = at + np.array([math.cos(math.radians(a)), math.sin(math.radians(a)), 0]) * r * 0.9
        spike(f"fringe_{bone}_{j}", base, d, 0.04, 0.016, bone)
for j, (base, d) in enumerate([((0, -0.01, 0.42), (0, 1, 0.25)), ((0, 0.0, 0.37), (0, 1, 0.05)),
                               ((0.05, -0.02, 0.4), (0.6, 1, 0.2)), ((-0.05, -0.02, 0.4), (-0.6, 1, 0.2))]):
    spike(f"fringe_head_{j}", base, d, 0.042, 0.016, "head")

# The yellow spike on the forehead, and the two on the tail
k.add("brow_spike", tube([(0, -0.15, 0.43), (0, -0.155, 0.47), (0, -0.15, 0.515)], [0.024, 0.016, 0.003]), YELLOW,
      "head", fuse=False, surface="shell")
k.add("tail_spike", tube([(0.024, 0.3, 0.085), (0.034, 0.34, 0.16), (0.045, 0.38, 0.25)], [0.024, 0.016, 0.003]), YELLOW,
      "tail", fuse=False, mirror=True, surface="shell")

# Five pairs of little white feet: three under the low half pointing out and down, two on the raised front
for bone, at, r in CHAIN[1:4]:
    at = np.array(at)
    base = at + np.array([0.62 * r, -0.1 * r, -0.62 * r])
    k.add(f"foot_{bone}", tube([base, base + unit(0.75, -0.25, -0.62) * 0.042], [0.016, 0.002]), WHITE, bone,
          fuse=False, mirror=True, surface="shell")
for bone, at, r in CHAIN[4:]:
    at = np.array(at)
    base = at + np.array([0.68 * r, -0.62 * r, -0.05])
    k.add(f"foot_{bone}", tube([base, base + unit(0.85, -0.4, -0.25) * 0.042], [0.016, 0.002]), WHITE, bone,
          fuse=False, mirror=True, surface="shell")

# Big round eyes on the sides of the head, on the red: a yellow ring round a black oval, a dark rim
RING, PUPIL, LID, INK = srgb("#F4D46A"), srgb("#141014"), srgb(RED), srgb("#3A1A1E")
def eye(u, v):
    c = np.tile(RING, (len(u), 1))
    c[(u / 0.56) ** 2 + (v / 0.76) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.86] = INK
    return c
de = unit(1, -0.12, 0.08)
def eyes(state):
    k.decal(HC + np.array([0, 0.015, 0.012]), de, (0.032, 0.04), eye_look(state, eye, LID, INK), eps=0.006,
            rings=20, segs=56, mirror=True)

k.build(voxel=0.005, smooth=2)
k.eye_states(eyes)
k.export(out)
