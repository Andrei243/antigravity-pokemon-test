# Geodude sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/geodude.py -- <the export's Geodude.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, boxy, lumpy, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

GREY, DARK, CRACK = "#ACAAA0", "#8E8C84", "#6A6862"
C = np.array([0.0, 0.0, 0.218])          # the boulder's middle, on the body bone


def facets(n, amount, seed, keep_front=None, depth=(0.9, 1.02)):
    """
    A deform that cuts a sphere into a rough stone: n flat faces (planes at random turns), mixed with the round by
    amount. keep_front leaves the faces whose normals point at the front (-Y) out, so the face stays smooth.
    """
    rs = np.random.RandomState(seed)
    N = rs.normal(size=(n, 3)); N /= np.linalg.norm(N, axis=1, keepdims=True)
    if keep_front is not None:
        N = N[N[:, 1] > -keep_front]
    d = depth[0] + (depth[1] - depth[0]) * rs.rand(len(N))
    def f(V):
        proj = (V @ N.T) / d[None]                 # how far each point lies out along each face
        r = 1 / np.maximum(proj.max(1), 0.6)       # the point's distance to the polyhedron along its ray
        r = np.minimum(r, 1.08)
        return V * (1 - amount + amount * r)[:, None]
    return f


# The boulder: a round stone, wider than tall, faceted on its back and sides, smoother where the face is
k.add("body", ell(C, (0.215, 0.172, 0.185), deform=facets(15, 1.0, 74, keep_front=0.55, depth=(0.8, 0.93))), GREY, "body")
# A few lumps of rock standing off its back and top
for i, (d, r) in enumerate([((0.4, 0.5, 0.75), 0.06), ((-0.55, 0.45, 0.6), 0.055), ((0.0, 0.85, 0.35), 0.065),
                            ((0.7, 0.55, -0.2), 0.05), ((-0.65, 0.6, -0.25), 0.05)]):
    dd = unit(*d)
    k.add(f"lump{i}", ell(C + dd * np.array([0.18, 0.15, 0.15]), (r, r * 0.8, r * 0.85), rot=(20 * i, 35 * i, 0),
                          deform=facets(7, 1.0, 100 + i, depth=(0.75, 0.9))), GREY, "body", fuse=False)

# A heavy brow over the eyes, its two halves dipping to a notch in the middle: a scowl
k.add("brow", ell((0.068, -0.145, 0.282), (0.08, 0.04, 0.03), rot=(-14, -16, 8), deform=facets(10, 0.5, 7)), GREY,
      "body", mirror=True)
# The cheeks and jaw jutting forward under the eyes, the mouth a hard crease between
k.add("jaw", ell((0, -0.13, 0.155), (0.12, 0.06, 0.055), deform=facets(10, 0.5, 9)), GREY, "body")

# The arms: thin where they leave the boulder, a bulging upper arm out to a low elbow, a thick forearm rising to a
# great fist held up beside the head
SH = np.array([0.15, 0.0, 0.215]); EL = np.array([0.3, -0.03, 0.165]); WR = np.array([0.34, -0.06, 0.3])
k.add("upper", tube([SH, (SH + EL) / 2 + [0, 0, 0.02], EL], [0.032, 0.05, 0.045]), GREY, "arm{s}", mirror=True)
k.add("biceps", ell((0.235, -0.025, 0.215), (0.06, 0.045, 0.042), rot=(0, 18, 0)), GREY, "arm{s}", mirror=True)
k.add("fore", tube([EL, (EL + WR) / 2, WR], [0.045, 0.05, 0.046]), GREY, "arm{s}", mirror=True)
k.add("elbow", ell(EL + [0.012, 0.005, -0.012], (0.04, 0.04, 0.036)), GREY, "arm{s}", mirror=True)
FC = np.array([0.35, -0.075, 0.375])
k.add("fist", ell(FC, (0.066, 0.068, 0.068), deform=boxy(2.6)), GREY, "arm{s}", mirror=True)
# four fingers curled across the front of the fist, the thumb across under them
for j in range(4):
    x = FC[0] - 0.039 + 0.026 * j
    k.add(f"finger{j}", ell((x, FC[1] - 0.06, FC[2] + 0.018), (0.0135, 0.024, 0.036), deform=boxy(2.6)), GREY,
          "arm{s}", mirror=True)
k.add("thumb", tube([(FC[0] + 0.06, FC[1] - 0.05, FC[2] - 0.03), (FC[0] + 0.015, FC[1] - 0.078, FC[2] - 0.038),
                     (FC[0] - 0.03, FC[1] - 0.08, FC[2] - 0.03)], [0.02, 0.019, 0.016]), GREY, "arm{s}", mirror=True)

# Cracks in the stone: thin dark lines up the brow and over the top and sides, drawn as directions from the middle so
# that they lie on whatever surface the facets left
def cracks(P):
    m = np.zeros(len(P))
    Q = P - C; Q /= np.linalg.norm(Q, axis=1, keepdims=True)
    lines = [((0.0, -0.16, 0.3), (0.02, -0.12, 0.36), (-0.02, -0.06, 0.39)),
             ((0.12, -0.1, 0.33), (0.17, -0.02, 0.33), (0.2, 0.05, 0.28)),
             ((-0.14, -0.08, 0.32), (-0.19, 0.02, 0.3)),
             ((0.05, 0.12, 0.36), (0.1, 0.17, 0.27), (0.12, 0.18, 0.17)),
             ((-0.08, 0.15, 0.32), (-0.03, 0.18, 0.2))]
    for pts in lines:
        pts = np.array(pts) - C
        pts /= np.linalg.norm(pts, axis=1, keepdims=True)
        for a, b in zip(pts[:-1], pts[1:]):
            ab = b - a
            t = np.clip(((Q - a) @ ab) / (ab @ ab), 0, 1)
            d = np.linalg.norm(Q - (a + t[:, None] * ab), axis=1) * 0.18
            m = np.maximum(m, inside(d - 0.004, 0.003))
    return m
k.paint(cracks, CRACK, on=["body"])
# The shadow under the brow, darker
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.258) - 0.012, 0.01) * smoothstep(-0.11, -0.15, P[:, 1]) *
        inside(np.abs(P[:, 0]) - 0.13), DARK, on=["body", "brow"])

# The mouth: a hard frowning crease across the jaw
CC = srgb(CRACK)
def frown(u, v):
    c = np.tile(srgb(GREY), (len(u), 1))
    line = 0.3 - 0.75 * u * u
    w = 0.2 * (1 - 0.5 * u * u)
    c[(np.abs(v - line) < w) & (np.abs(u) < 0.9)] = CC
    return c
k.decal((0, -0.18, 0.18), unit(0, -1, 0.15), (0.075, 0.03), frown, eps=0.003, rings=28, segs=120)

# Small angry eyes under the brow: white, a dark brown iris to the inside, the lid slanting down toward the middle
SCLERA, IRIS, PUPIL, LID, INK = srgb("#F4F2EC"), srgb("#4A3426"), srgb("#120E0C"), srgb(GREY), srgb("#2A2824")
def eye(u, v):
    c = np.tile(SCLERA, (len(u), 1))
    c[((u + 0.2) / 0.42) ** 2 + ((v + 0.05) / 0.85) ** 2 < 1] = IRIS
    c[((u + 0.22) / 0.24) ** 2 + ((v + 0.05) / 0.5) ** 2 < 1] = PUPIL
    c[((u + 0.05) / 0.13) ** 2 + ((v - 0.22) / 0.16) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.84] = INK
    edge = 0.25 + 0.45 * u
    c[v > edge + 0.12] = LID
    c[np.abs(v - edge) <= 0.12] = INK
    return c
de = unit(0.38, -0.92, 0.05)
def eyes(state):
    k.decal(C + np.array([0, 0, 0.012]) + de * 0.175, de, (0.034, 0.026), eye_look(state, eye, LID, INK),
            eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.006, smooth=1)
k.eye_states(eyes)
k.export(out)
