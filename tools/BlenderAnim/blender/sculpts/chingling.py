# Chingling sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chingling.py -- <the export's Chingling.glb> <out.glb>
#
# The design, from the references: a small round yellow bell, a little wider than tall, with two small dark upright
# eyes and a thin dark seam round its front below them (the bell's lip); a round dark opening on each side at the seam;
# stubby yellow arms out at its sides and two stubby feet under its front. From a small yellow knob at the top of its
# back two thick cords stream back and up, banded red and white like sausages, each ending in three stubby fingers,
# red, white and red. (No open red mouth or clapper shows in either picture: the seam is all the mouth there is.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

YELLOW, INK, HOLE, RED, WHITE = "#F2D03A", "#2B2628", "#3E383C", "#E85A5E", "#E2E8F0"
C = np.array([0.0, 0.0, 0.215])
R = np.array([0.145, 0.132, 0.132])

# The bell, the knob on its back, the arms and feet
k.add("bell", ell(C, R), YELLOW, "body")
k.add("knob", ell((0, 0.075, 0.325), (0.032, 0.032, 0.03)), YELLOW, "body")
k.add("arm", ell((0.158, -0.035, 0.25), (0.05, 0.03, 0.031), rot=(0, -38, 0)), YELLOW, "arm{s}", mirror=True)
k.add("foot", ell((0.062, -0.07, 0.072), (0.04, 0.05, 0.04), rot=(-12, 0, 0)), YELLOW, "leg{s}", mirror=True)

# The seam round the front below the eyes, a faint smile
def seam(P):
    x = P[:, 0] - C[0]
    curve = C[2] - 0.028 + 0.9 * x * x
    return inside(np.abs(P[:, 2] - curve) - 0.0028, 0.0018) * (P[:, 1] < -0.02) * inside(np.abs(x) - 0.13, 0.004)
k.paint(seam, INK, on=["bell"])

# A round dark opening on each side, on the seam
for s in (1, -1):
    d = unit(s * math.sin(math.radians(60)), -math.cos(math.radians(60)), -0.34)
    hc = C + d * R
    k.paint(lambda P, hc=hc: inside(np.linalg.norm(P - hc, axis=1) - 0.044, 0.003), HOLE, on=["bell"])


# The two cords: banded sausages along a curve from the knob back and up, each with three stubby fingers at its end
def bez(p0, p1, p2, t):
    t = np.asarray(t)[:, None]
    return (1 - t) ** 2 * p0 + 2 * (1 - t) * t * p1 + t * t * p2

BANDS = [(0.0, 0.2, RED), (0.2, 0.38, WHITE), (0.38, 0.58, RED), (0.58, 0.78, WHITE), (0.78, 1.0, RED)]
for s, side in ((1, "R"), (-1, "L")):
    p0 = np.array([0.018 * s, 0.07, 0.335])
    p1 = np.array([0.05 * s, 0.22, 0.42])
    p2 = np.array([0.15 * s, 0.37, 0.43])
    rad = lambda t: 0.021 + 0.017 * t
    for i, (a, b, col) in enumerate(BANDS):
        ts = np.linspace(a, b, 4)
        pts = bez(p0, p1, p2, ts)
        k.add(f"cord{side}{i}", tube(pts, [rad(t) * (0.92 if 0 < j < 3 else 0.82) + 0.002 for j, t in enumerate(ts)]),
              col, "body", fuse=False, surface="cloth")
    end = bez(p0, p1, p2, [1.0])[0]
    D = end - bez(p0, p1, p2, [0.95])[0]; D /= np.linalg.norm(D)
    up = np.array([0, 0, 1.0]); n = up - D * np.dot(up, D); n /= np.linalg.norm(n)
    for j, col in ((-1, RED), (0, WHITE), (1, RED)):
        f = D + 0.8 * j * n; f /= np.linalg.norm(f)
        base = end - D * 0.012
        k.add(f"finger{side}{j + 1}", tube([base, base + f * 0.035, base + f * 0.062], [0.016, 0.018, 0.019]), col,
              "body", fuse=False, surface="cloth")

# The eyes: small dark upright ovals, set apart above the seam
CY, CI = srgb(YELLOW), srgb(INK)
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u - 0.25) / 0.22) ** 2 + ((v - 0.45) / 0.2) ** 2 < 1] = srgb("#6A6266")
    return c
def eyes(state):
    for s in (1, -1):
        dn = unit(s * 0.42, -0.9, 0.12)
        e = C + dn * R
        look = eye_look(state, eye, CY, CI, line=0.2)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(e, dn, (0.013, 0.021), lk, eps=0.003, rings=20, segs=56)

k.build(voxel=0.004, smooth=3)
k.eye_states(eyes)
k.export(out)
