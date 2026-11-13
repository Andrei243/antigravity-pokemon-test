# Noctowl sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/noctowl.py -- <the export's Noctowl.glb> <out.glb>
# Hoothoot grown: the same brown and cream, the red eyes with black pupils of the same make and the pale beak, but a
# tall owl standing on two cream legs, its front marked with two rows of dark arrowheads, darker wings folded like a
# cloak and a fan of a tail behind; on the head a cream band whose lower edge slants down over the eyes to a point of
# brown between them, rising at the sides into two great flat crests jagged at their tops like horns.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, boxy, args

src, out = args()
k = Kit(src, coat="fur")

BROWN, WING, DARK, CREAM, PALE = "#B07F68", "#7A4B36", "#2E2226", "#E8D2A0", "#EAD9B2"
BEAK, FOOT, CLAW = "#E6B4AA", "#EBD3A0", "#F2D2D0"
H = np.array([0, -0.025, 0.775])          # the head's centre
HR = np.array([0.15, 0.135, 0.14])


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0035):
    """A polygon painted with a soft edge: the signed distance to its outline (negative inside), eased."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def mirrored(half):
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


# A tall egg of a body under a big round head
k.add("body", ell((0, 0.01, 0.38), (0.185, 0.17, 0.27)), BROWN, "body")
k.add("chest", ell((0, -0.03, 0.59), (0.15, 0.135, 0.11)), BROWN, "body")
k.add("head", ell(H, HR), BROWN, "head")
# the face a little flatter, so the eyes and their pale rings lie on it without wrapping round
k.add("face", ell((0, -0.07, 0.76), (0.135, 0.1, 0.115)), BROWN, "head")

# The crests: from each side of the crown a broad flat plate rising and leaning out, its top cut into three points,
# fused into the head so that it grows out of the cream band
def flared(V):
    V = boxy(4)(V)
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)                 # 0 at the root, 1 at the top
    V[:, 0] *= 0.8 + 0.3 * t                             # a little wider toward the top
    return V
CT = np.radians(16)                                      # how far each crest leans out
cu = np.array([math.sin(CT), 0.04, math.cos(CT)]); cx = np.array([math.cos(CT), 0, -math.sin(CT)])
CB = np.array([0.135, -0.04, 0.9])                       # the middle of a crest's plate
k.add("crest", ell(CB, (0.08, 0.024, 0.075), rot=(-3, 16, 0), deform=flared), CREAM, "head", mirror=True)
k.add("crest_root", ell((0.095, -0.05, 0.85), (0.07, 0.045, 0.05), rot=(0, 26, 0)), CREAM, "head", mirror=True)
# its top cut into three points, the outer one the tallest
for j, (off, h) in enumerate(((-0.048, 0.03), (0.0, 0.025), (0.052, 0.045))):
    b0 = CB + cx * off + cu * 0.055
    tip = b0 + cu * (0.035 + h) + cx * off * 0.25
    k.add(f"crestpt{j}", blade([b0, tip], [0.042, 0.004], (0, 1, 0), 0.55), CREAM, "head", mirror=True)

# The cream band across the brow, measured round the head's upright axis (a in radians from the front, z): its lower
# edge slanting down toward the middle over the eyes, notched there by a point of brown rising into it
AX = H[1]
BAND = mirrored([(0.0, 0.93), (0.5, 0.95), (0.95, 0.95), (1.3, 0.92), (1.3, 0.82), (0.95, 0.825), (0.6, 0.805),
                 (0.3, 0.78), (0.17, 0.772), (0.0, 0.885)])
def band(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - AX))
    R = 0.15
    return poly_mask(a * R, P[:, 2], [(p * R, z) for p, z in BAND]) * (P[:, 2] > 0.72)
k.paint(band, CREAM, on=["head", "face", "crest"])
# and the brown below it crisp, the crests' roots kept off the face and the point between the eyes
def under_band(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - AX))
    return (1 - band(P)) * (P[:, 2] > 0.72) * (P[:, 2] < 0.9) * (np.abs(a) < 1.25)
k.paint(under_band, BROWN, on=["head", "face", "crest_root"])

# The two rows of dark arrowheads on the front, pointing down
TRI = polygon([(-1, 0.6), (1, 0.6), (0, -1.0)])
for z, xs in ((0.52, (0.0, 0.09)), (0.37, (0.0, 0.095))):
    for x in xs:
        n = unit(x / 0.185 * 0.9, -1, 0.1)
        k.decal((x, -0.12, z), n, (0.048, 0.055), DARK, outline=TRI, eps=0.003, rings=8, segs=36, mirror=x > 0)

# The wings, darker, folded down the sides like a cloak and past the tail at their tips; a feather line on each
k.add("wing", ell((0.16, 0.035, 0.42), (0.055, 0.15, 0.25), rot=(18, -6, 0)), WING, "wing{s}", fuse=False, mirror=True)
k.add("wingtip", ell((0.115, 0.17, 0.25), (0.04, 0.09, 0.13), rot=(40, -6, 0)), WING, "wing{s}", fuse=False,
      mirror=True)
k.add("feather", tube([(0.207, -0.01, 0.53), (0.21, 0.025, 0.41), (0.19, 0.09, 0.3)], [0.005, 0.005, 0.004]),
      "#5E3828", "wing{s}", fuse=False, mirror=True)

# The tail: a fan of dark feathers sloping back and down behind the legs
for i, (side, length) in enumerate(((0.0, 0.3), (0.045, 0.27), (-0.045, 0.27))):
    d = unit(side * 2.0, 1, -0.75)
    base = np.array([side * 0.5, 0.11, 0.3])
    k.add(f"tail{i}", blade([base, base + d * length * 0.55, base + d * length], [0.06, 0.07, 0.015], (0, 0.6, 0.8),
                            0.3), WING, "tail", fuse=False)

# The beak: small, pale and hooked, pointing down between the eyes
k.add("beak", tube([(0, -0.14, 0.77), (0, -0.175, 0.75), (0, -0.183, 0.715), (0, -0.172, 0.69)],
                   [0.024, 0.02, 0.012, 0.003]), BEAK, "head", fuse=False, surface="shell")

# Two cream legs under the body, three long toes forward and one back, pale pink claws
for s, side in ((1, "R"), (-1, "L")):
    x = 0.065 * s
    k.add(f"leg{side}", tube([(x, -0.02, 0.27), (x * 1.05, -0.025, 0.13), (x * 1.1, -0.03, 0.04)], [0.038, 0.032, 0.026]),
          FOOT, f"leg{side}", fuse=False, surface="fur")
    base = np.array([x * 1.1, -0.035, 0.03])
    for j, ang in enumerate((-35, 0, 35, 180)):
        rr = math.radians(ang + 8 * s)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        tip = base + d * (0.075 if ang != 180 else 0.04) + np.array([0, 0, -0.012])
        k.add(f"toe{side}{j}", tube([base, (base + tip) / 2 + np.array([0, 0, 0.005]), tip], [0.016, 0.014, 0.012]),
              FOOT, f"leg{side}", fuse=False, surface="shell")
        k.add(f"claw{side}{j}", tube([tip, tip + (d + np.array([0, 0, -0.6])) * 0.03], [0.01, 0.0015]), CLAW,
              f"leg{side}", fuse=False, surface="shell")

# The pale rings round the eyes (on the fused face, under the eyes)
de = unit(0.42, -0.9, -0.02)
E = H + np.array([0, 0, -0.025]) + de * 0.142
k.decal(E, de, (0.05, 0.042), PALE, eps=0.003, rings=12, segs=48, mirror=True)

# The eyes: Hoothoot's red eyes with black pupils, rimmed in black with a white ring; the band's cream edge cuts their
# tops, slanting down toward the beak in a scowl
CRED, CBLK, CWHT, LID, CC = srgb("#E03C3C"), srgb("#141214"), srgb("#FFFFFF"), srgb(PALE), srgb(CREAM)
def eye(u, v):
    r = np.sqrt((u / 1.0) ** 2 + (v / 0.9) ** 2)
    c = np.tile(CBLK, (len(u), 1))
    c[r < 0.84] = CWHT
    c[r < 0.62] = CRED
    c[r < 0.3] = CBLK
    return c
def browed(look):
    def f(u, v):
        c = look(u, v)
        c[v > 0.35 + 0.4 * u] = CC
        return c
    return f
def eyes(state):
    k.decal(E, de, (0.036, 0.032), browed(eye_look(state, eye, LID, CBLK)), eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
