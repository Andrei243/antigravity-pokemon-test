# Kricketot sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kricketot.py -- <the export's Kricketot.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="shell")

RED, BAND, CREAM, PLEAT, BROWN, GRAY = "#E2553A", "#8C3A24", "#F6E7AC", "#CDB273", "#A4482C", "#5E5E68"
H = np.array([0, -0.07, 0.52])          # the head's centre

# A tall egg of a body, broadest low down, a dark band round its foot
def egg(V):
    V = V.copy()
    s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0, 0.32), (0.22, 0.2, 0.28), deform=egg), RED, "body")
k.paint(lambda P: inside(P[:, 2] - 0.13, 0.004), BAND, on=["body"])
# The red face, sitting inside the collar
k.add("head", ell(H, (0.165, 0.15, 0.15)), RED, "head")

# Short grey legs under it, round grey feet poking out in front
k.add("leg", tube([(0.09, 0, 0.13), (0.1, -0.02, 0.05)], [0.05, 0.045]), GRAY, "leg{s}", mirror=True)
k.add("foot", ell((0.1, -0.05, 0.046), (0.075, 0.1, 0.045)), GRAY, "leg{s}", fuse=False, mirror=True)
# Stubby grey arms, a round hand on each
k.add("arm", tube([(0.17, -0.08, 0.3), (0.2, -0.12, 0.25)], [0.04, 0.042]), GRAY, "arm{s}", fuse=False, mirror=True)
k.add("hand", ell((0.21, -0.14, 0.235), (0.052, 0.05, 0.055)), GRAY, "arm{s}", fuse=False, mirror=True)

# The cream front of the coat, pleated down its middle
k.decal((0, -0.2, 0.29), (0, -1, 0), (0.135, 0.155), CREAM, eps=0.003, rings=16, segs=64,
        outline=polygon([(-0.55, 1), (0.55, 1), (1, 0.45), (1, -0.85), (0.6, -1), (-0.6, -1), (-1, -0.85), (-1, 0.45)]))
for x in (-0.036, -0.012, 0.012, 0.036):
    k.decal((x, -0.2, 0.25), (0, -1, 0), (0.004, 0.11), PLEAT, eps=0.005, rings=3, segs=24,
            outline=polygon([(-1, 1), (1, 1), (1, -1), (-1, -1)]))
# A darker seam down each side, where the coat's back meets its front
k.decal((0.18, -0.11, 0.25), unit(0.87, -0.5, 0), (0.005, 0.13), "#A8381F", eps=0.003, rings=2, segs=12, mirror=True)

# The great cream collar: a hood standing high behind the head, flared, open in front, its front ends folding down
# over the chest as two pointed lapels
HC, HRX, HRY, HRZ = np.array([0, 0.0, 0.52]), 0.25, 0.215, 0.215
def collar(u, v):
    # each row a ring of the hood at one height, open in front: narrow at the chest, wide round the face, and over
    # the crown only the back of it
    phi0 = 0.55 + 0.95 * smoothstep(0.0, 0.5, v) + 0.75 * smoothstep(0.5, 1.0, v)
    phi = phi0 + u * (2 * math.pi - 2 * phi0)               # 0 is the front, pi the back
    front = np.minimum(phi, 2 * math.pi - phi)
    th = np.radians(-34 + 118 * v)
    edge = np.exp(-((front - phi0) / 0.35) ** 2)            # near the opening's rim
    lapel = np.exp(-((front - phi0) / 0.22) ** 2) * (1 - v) ** 3
    flare = 1 + 0.12 * edge * np.sin(math.pi * np.clip(v, 0, 1))
    rx = HRX * np.cos(th) * flare + 0.025 * lapel
    ry = HRY * np.cos(th) * flare + 0.025 * lapel
    z = HC[2] + HRZ * np.sin(th) - 0.03 * lapel
    return np.stack([rx * np.sin(phi), HC[1] - ry * np.cos(phi), z], axis=-1)
k.add("collar", sheet(collar, 80, 20, 0.024), CREAM, "head", fuse=False, surface="shell")
# The collar's two points lying on the chest, meeting under the snout, edged a shade darker (not mirrored: the
# outline is not symmetric, and a mirrored decal keeps its outline unflipped)
for s in (1, -1):
    tri_pts = [(-1, 1), (1, 1), (0.65, 0.2), (-0.7, -1)]
    k.decal((0.075 * s, -0.2, 0.37), unit(0.35 * s, -1, 0.15), (0.095, 0.08), PLEAT, eps=0.005, rings=10, segs=48,
            outline=polygon([(u * s, v) for u, v in tri_pts]))
    k.decal((0.075 * s, -0.2, 0.37), unit(0.35 * s, -1, 0.15), (0.083, 0.068), CREAM, eps=0.007, rings=10, segs=48,
            outline=polygon([(u * s, v) for u, v in tri_pts]))

# The brown snout and the brown spot on the crown
k.add("nose", ell((0, -0.215, 0.47), (0.068, 0.06, 0.05), rot=(-20, 0, 0)), BROWN, "head", fuse=False)
k.add("spot", ell((0, -0.1, 0.652), (0.062, 0.05, 0.034), rot=(-25, 0, 0)), BROWN, "head", fuse=False)

# The antennae: thick grey stalks rising from the crown, curling outward like a ram's horns
def antenna():
    C = np.array([0.2, -0.03, 0.86])
    pts = [(0.045, -0.03, 0.62), (0.075, -0.03, 0.72), (0.1, -0.03, 0.8)]
    rad = [0.032, 0.034, 0.034]
    n = 12
    for i in range(n + 1):
        t = i / n
        a = math.radians(180 - 350 * t)
        r = 0.088 - 0.06 * t
        pts.append((C[0] + r * math.cos(a), C[1], C[2] + r * math.sin(a)))
        rad.append(0.035 - 0.012 * t)
    return tube(pts, rad)
k.add("antenna", antenna(), GRAY, "ear{s}", fuse=False, mirror=True)

# Small black eyes with a white glint, just above the snout
BLACK, LID = srgb("#141214"), srgb(RED)
def eye(u, v):
    c = np.tile(BLACK, (len(u), 1))
    c[((u + 0.05) / 0.32) ** 2 + ((v - 0.35) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.42, -0.88, 0.2)
def eyes(state):
    k.decal(H + de * 0.15, de, (0.026, 0.036), eye_look(state, eye, LID, BLACK), eps=0.007, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
