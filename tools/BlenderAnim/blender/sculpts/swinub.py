# Swinub sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swinub.py -- <the export's Swinub.glb> <out.glb>
#
# The design, from the references: a small round mound of light brown fur, longer than it is tall, highest a little
# behind its middle and sloping down to a rounded front; a round pink snout low at the front with two short dark
# nostrils; eyes that are only closed curved lines above and behind the snout, slanting down toward the back; three
# dark brown stripes a side running back from the front of the body, thick behind and tapering forward, the upper
# ones near the spine; a shaggy fringe round the bottom of its back half, its strands ending in a zigzag on the
# ground. No legs or tail show.
# The skeleton: body (0, 0, 0.14), head (0, -0.12, 0.16), tail (0, 0.16, 0.15), legs (+-0.08, -0.07, 0.06),
# hind legs (+-0.08, 0.08, 0.06).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BROWN, STRIPE, PINK, NOSTRIL, INKH = "#B68A62", "#6A432E", "#F2A0B0", "#B45468", "#4A3022"
C = np.array([0.0, 0.02, 0.13])
R = np.array([0.165, 0.185, 0.14])


def mound(V):
    """Lower toward the front above its middle, a little flattened below."""
    V = V.copy()
    front = np.clip(-V[:, 1], 0, 1)
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * (1 - 0.2 * front ** 1.3), V[:, 2] * 0.9)
    return V
k.add("body", ell(C, R, deform=mound), BROWN, "body")

# The snout: a round pink disc, its front flattened
def disc(V):
    V = V.copy()
    V[:, 1] = np.maximum(V[:, 1], -0.75)
    return V
k.add("snout", ell((0, -0.163, 0.072), (0.046, 0.034, 0.034), deform=disc), PINK, "head", surface="skin")

# The shaggy fringe round the bottom of the back half: flat strands out and down, a zigzag of points on the ground
n_ = 15
for i in range(n_):
    a = math.radians(-70 + 140 * i / (n_ - 1))
    o = np.array([math.sin(a), math.cos(a), 0.0])
    base = np.array([o[0] * 0.13, C[1] + o[1] * 0.142, 0.066])
    reach = 0.004 + 0.038 * math.cos(a) ** 2 + (0.012 if i % 2 else 0.0)
    tip = base + o * reach + np.array([0, 0, -0.056 + (0.01 if i % 3 == 1 else 0)])
    mid = base * 0.45 + tip * 0.55 + o * 0.012
    k.add(f"fringe{i}", blade([base, mid, tip], [0.03, 0.02, 0.005], o, 0.4), BROWN, "body")

# The stripes: three a side, at a height round the body's long axis, thick behind and tapering toward the front
def stripes(P):
    sx = np.abs(P[:, 0]); dz = P[:, 2] - C[2]
    phi = np.arctan2(dz, sx)
    r = np.sqrt(sx * sx + dz * dz)
    m = np.zeros(len(P))
    for phi0, front, w in ((1.2, -0.05, 0.1), (0.68, -0.005, 0.1), (0.18, 0.02, 0.09)):
        # rounded off behind before the rear, where every stripe's height round the axis would meet in a star
        back = np.sqrt(np.clip(1 - (np.maximum(P[:, 1] - 0.15, 0) / 0.03) ** 2, 0, 1))
        hw = w * smoothstep(front, front + 0.11, P[:, 1]) ** 0.8 * back
        m = np.maximum(m, inside((np.abs(phi - phi0) - hw) * r, 0.004) * (P[:, 1] > front) * (P[:, 1] < 0.18))
    return m
k.paint(stripes, STRIPE, on=["body"])

# The nostrils: two short upright dark marks on the snout's front
for x in (0.016, -0.016):
    k.decal((x, -0.2, 0.072), (0, -1, 0.05), (0.005, 0.0095), NOSTRIL, eps=0.004, rings=6, segs=24)

k.build(voxel=0.0055, smooth=3, band=0.008)

# The eyes: closed curved lines by design, arched and slanting down toward the back
INK, LID = srgb(INKH), srgb(BROWN)
def slit(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(v - (0.05 + 0.4 * (1 - u * u) - 0.25 * u)) < 0.22) & (np.abs(u) < 0.88)] = INK
    return c
de = unit(0.55, -0.8, 0.28)
def eyes(state):
    k.decal((0.08, -0.13, 0.172), de, (0.04, 0.022), eye_look(state, slit, LID, INK, line=0.2), eps=0.004,
            rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
