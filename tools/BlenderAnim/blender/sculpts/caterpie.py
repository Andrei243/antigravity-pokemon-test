# Caterpie sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/caterpie.py -- <the export's Caterpie.glb> <out.glb>
# A green caterpillar rearing its front half up under a big round head: cream down its front and underneath, round
# cream feet on the raised segments, pale yellow rings along its green sides, great eyes ringed in yellow, a red
# forked feeler on top of its head and a cream drop of a tail tip held up behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

GREEN, CREAM, RING, RED, SEAM, MOUTH = "#86C85A", "#F2E2A2", "#F2E08E", "#E2563A", "#C8B47A", "#2A2418"
CREASE = "#5E9C40"

# --- the body: a segment on each bone's joint, low along the ground at the back, the front half rearing up ----------
CHAIN = [("tail", (0, 0.335, 0.05), 0.046), ("body", (0, 0.26, 0.06), 0.058), ("seg1", (0, 0.17, 0.068), 0.066),
         ("seg2", (0, 0.08, 0.08), 0.076), ("seg3", (0, 0.0, 0.118), 0.084), ("seg4", (0, -0.035, 0.192), 0.088),
         ("seg5", (0, -0.05, 0.268), 0.084)]
for bone, at, r in CHAIN:
    k.add(f"seg_{bone}", ell(at, (r * 1.02, r * 0.95, r)), GREEN, bone)

HC = np.array([0.0, -0.075, 0.405])            # the head's middle
HR = 0.122
k.add("skull", ell(HC, (HR * 1.02, HR, HR * 0.98)), GREEN, "head")
# The great round snout at the front of the head, below the middle, ringed by a crease
SN = np.array([0.0, -0.165, 0.37])
k.add("snout", ell(SN, (0.07, 0.055, 0.062)), GREEN, "head")
# The dark mouth, tucked in under the snout where the head meets the body
k.add("mouth", ell((0, -0.15, 0.305), (0.05, 0.03, 0.026)), MOUTH, "head")

# The axis the body runs along, from the tail's end up to the neck, for its belly, its seams and its rings
AX = np.array([(0, 0.37, 0.05)] + [at for _, at, _ in CHAIN] + [(0, -0.06, 0.32)], float)
S = np.vstack([AX[i] + (AX[i + 1] - AX[i]) * t for i in range(len(AX) - 1) for t in np.linspace(0, 1, 30, endpoint=False)]
              + [AX[-1:]])
TS = np.gradient(S, axis=0); TS /= np.linalg.norm(TS, axis=1, keepdims=True)
ARC = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(S, axis=0), axis=1))])
BELLY = np.cross(np.array([1.0, 0, 0])[None], TS)         # down at the back, forward up the raised front
def along(P):
    i = np.argmin(((P[:, None, :] - S[None]) ** 2).sum(2), axis=1)
    return S[i], BELLY[i], ARC[i], TS[i]

# Cream underneath and all down the front of the raised half, the green wrapping its back and sides
def belly(P):
    c, d, _, _ = along(P)
    q = P - c; q /= np.linalg.norm(q, axis=1, keepdims=True)
    edge = 0.32 - 0.3 * smoothstep(0.09, -0.02, P[:, 1])
    m = np.sqrt((((P - np.array([0, -0.15, 0.305])) / np.array([0.05, 0.03, 0.026])) ** 2).sum(1))
    return inside(edge - (q * d).sum(1), 0.003) * inside(P[:, 2] - 0.33, 0.003) * smoothstep(1.05, 1.25, m)
k.paint(belly, CREAM)
# Seams across the cream between one segment and the next
JOINT_ARC = [ARC[np.argmin(((S - np.array(at)) ** 2).sum(1))] for _, at, _ in CHAIN]
CUTS = [(a + b) / 2 for a, b in zip(JOINT_ARC[:-1], JOINT_ARC[1:])]
def seams(P):
    c, d, s, _ = along(P)
    q = P - c; q /= np.linalg.norm(q, axis=1, keepdims=True)
    m = np.zeros(len(P))
    for cut in CUTS:
        m = np.maximum(m, inside(np.abs(s - cut) - 0.0035, 0.002))
    return m * smoothstep(0.3, 0.5, (q * d).sum(1))
k.paint(seams, SEAM, on=["seg"])
# The crease round the snout
def crease(P):
    q = (P - SN) / np.array([0.07, 0.055, 0.062])
    r = np.sqrt((q ** 2).sum(1))
    h = np.linalg.norm(P - HC, axis=1) / HR
    return inside(np.abs(h - 1.0) - 0.03, 0.006) * inside(r - 1.12, 0.01) * smoothstep(-0.14, -0.16, P[:, 1])
k.paint(crease, CREASE)

# Round cream feet in pairs on the raised segments, standing out to the front and the sides
for bone, at, r, z in (("seg3", (0, 0.0, 0.118), 0.084, -0.01), ("seg4", (0, -0.035, 0.192), 0.088, 0.0),
                       ("seg5", (0, -0.05, 0.268), 0.084, 0.0)):
    at = np.array(at)
    k.add(f"foot_{bone}", ell(at + np.array([0.62 * r, -0.78 * r, z]), (0.03, 0.028, 0.027)), CREAM, bone, mirror=True)

# The tail: the last green segment curls up into a thin cream stalk and a long cream drop held up behind
k.add("stalk", tube([(0, 0.36, 0.06), (0, 0.38, 0.1), (0, 0.39, 0.13)], [0.016, 0.011, 0.012]), CREAM, "tail")
k.add("drop", ell((0, 0.398, 0.19), (0.024, 0.022, 0.062), rot=(-8, 0, 0),
                  deform=lambda V: V * np.array([1, 1, 1.0]) * (1 - 0.35 * np.clip(-V[:, 2:3], 0, 1))), CREAM, "tail")

# The red forked feeler: a short stem up out of the top of the head, and two round prongs spreading out to the sides
STEM = np.array([0.0, -0.11, 0.505])
k.add("feeler", tube([(0, -0.1, 0.47), STEM, (0, -0.115, 0.535)], [0.024, 0.022, 0.02]), RED, "head", fuse=False)
k.add("prong", tube([(0, -0.115, 0.53), (0.04, -0.12, 0.548), (0.08, -0.118, 0.56), (0.105, -0.112, 0.565)],
                    [0.02, 0.02, 0.019, 0.017]), RED, "head", fuse=False, mirror=True)

# Pale yellow rings along the green sides, largest near the head and smaller toward the tail
def ring(u, v, green=srgb(GREEN), yel=srgb(RING)):
    c = np.tile(yel, (len(u), 1))
    c[u * u + v * v < 0.42] = green
    return c
for bone, at, r, size in (("seg4", (0, -0.035, 0.192), 0.088, 0.034), ("seg3", (0, 0.0, 0.118), 0.084, 0.03),
                          ("seg2", (0, 0.08, 0.08), 0.076, 0.025), ("seg1", (0, 0.17, 0.068), 0.066, 0.021),
                          ("body", (0, 0.26, 0.06), 0.058, 0.017)):
    at = np.array(at)
    n = unit(0.95, 0.0 if bone in ("seg4", "seg3") else 0.25, 0.25)
    k.decal(at + n * r, n, (size, size), ring, eps=0.004, rings=18, segs=48, mirror=True)

# Big round eyes on the sides of the head: a black eye with a white glint in a pale yellow ring, a dark rim
YRING, PUPIL, LID, INK, GLINT = srgb("#F3D877"), srgb("#141210"), srgb(GREEN), srgb("#22201A"), srgb("#FFFFFF")
def eye(u, v):
    c = np.tile(YRING, (len(u), 1))
    c[((u + 0.06) / 0.66) ** 2 + ((v + 0.02) / 0.7) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.8] = INK
    return c
de = unit(0.78, -0.56, 0.2)
def eyes(state):
    k.decal(HC + de * HR, de, (0.05, 0.054), eye_look(state, eye, LID, INK), eps=0.006, rings=20, segs=56, mirror=True)
    if state == "Open":
        # the glint, a round patch of its own over the eye, high and toward the front
        dg = unit(0.7, -0.66, 0.34)
        k.decal(HC + dg * HR, dg, (0.011, 0.011), GLINT, eps=0.009, rings=8, segs=32, mirror=True)

k.build(voxel=0.006, smooth=3)
k.eye_states(eyes)
k.export(out)
