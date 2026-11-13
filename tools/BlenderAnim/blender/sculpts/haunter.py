# Haunter sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/haunter.py -- <the export's Haunter.glb> <out.glb>
# Grown from Gastly's script: the same wedge eyes with a small pupil and the same carved pink mouth, now a purple
# head of gas ringed with spikes, tapering into a wisp, with two hands that float free of it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, TEETH, MOUTH = "#8A6EC2", "#6F56A8", "#E07882"
C = np.array([0.0, 0.0, 0.5])            # the head's middle, over the body bone
RX, RY, RZ = 0.2, 0.17, 0.19

# The head-body: a round head, a little wider than deep
k.add("head", ell(C, (RX, RY, RZ)), PURPLE, "body")

# The wisp below it: the body drawn down and back into a point
k.add("tail", tube([(0, 0.01, 0.42), (0, 0.035, 0.31), (0.01, 0.06, 0.21), (0.035, 0.06, 0.12)],
                   [0.14, 0.085, 0.04, 0.006]), PURPLE, "tail")

# Spikes: a tall one on the crown, a great one at each upper side sweeping back, smaller ones down the cheeks, the
# wisp's sides and the back
k.add("crest", blade([(0, 0.02, 0.64), (0.0, 0.04, 0.76), (-0.01, 0.07, 0.9)], [0.075, 0.04, 0.004],
                     (0, 1, 0), 0.6), PURPLE, "body")
SPIKES = [
    ([(0.12, 0.03, 0.6), (0.26, 0.07, 0.67), (0.42, 0.12, 0.74)], [0.065, 0.032, 0.004]),
    ([(0.17, 0.0, 0.53), (0.25, 0.02, 0.56), (0.31, 0.03, 0.58)], [0.04, 0.02, 0.003]),
    ([(0.18, 0.01, 0.44), (0.26, 0.03, 0.43), (0.32, 0.04, 0.41)], [0.04, 0.018, 0.003]),
    ([(0.12, 0.04, 0.34), (0.2, 0.06, 0.3), (0.26, 0.07, 0.26)], [0.035, 0.016, 0.003]),
    ([(0.05, 0.06, 0.24), (0.11, 0.08, 0.19), (0.15, 0.09, 0.15)], [0.025, 0.012, 0.003]),
    ([(0.08, 0.13, 0.52), (0.14, 0.22, 0.52), (0.18, 0.29, 0.51)], [0.045, 0.02, 0.003]),
    ([(0.06, 0.12, 0.4), (0.1, 0.2, 0.36), (0.13, 0.26, 0.33)], [0.035, 0.016, 0.003]),
]
for i, (pts, radii) in enumerate(SPIKES):
    k.add(f"spike{i}", blade(pts, radii, (0, 0, 1) if i >= 5 else (0, 1, 0), 0.6), PURPLE, "body", mirror=True)
k.add("backspike", blade([(0, 0.13, 0.6), (0, 0.22, 0.63), (0, 0.31, 0.65)], [0.05, 0.022, 0.003], (1, 0, 0), 0.6),
      PURPLE, "body")

# The mouth: a wide grin carved low on the front, flat along the top and curving up at the corners, pink within
def grin(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.35, V[:, 2])
    V[:, 2] += 0.55 * V[:, 0] ** 2
    V[:, 1] += 0.9 * V[:, 0] ** 2
    return V
MC = np.array([0.0, -0.148, 0.415])
k.add("mouth", ell(MC, (0.135, 0.07, 0.065), rot=(-20, 0, 0), deform=grin), MOUTH, "body", carve=True)

# The teeth: a jagged row of purple points hanging from the upper lip and rising from the lower one, loose so they
# keep their points
def front(x, z):
    return -RY * math.sqrt(max(1 - (x / RX) ** 2 - ((z - C[2]) / RZ) ** 2, 0.0))
for i, x in enumerate((-0.064, -0.022, 0.022, 0.064)):
    z = 0.436 + 0.002 * (x / 0.07) ** 2
    y = front(x, z) + 0.01
    k.add(f"tooth{i}", blade([(x, y, z + 0.008), (x, y - 0.004, z - 0.01), (x, y - 0.005, z - 0.028)],
                             [0.024, 0.013, 0.002], (0, 1, 0), 0.45), TEETH, "body", fuse=False)
for i, x in enumerate((-0.052, 0.0, 0.052)):
    z = 0.386 + 0.004 * (x / 0.052) ** 2
    y = front(x, z) + 0.01
    k.add(f"lowtooth{i}", blade([(x, y, z - 0.008), (x, y - 0.004, z + 0.008), (x, y - 0.005, z + 0.024)],
                                [0.021, 0.011, 0.002], (0, 1, 0), 0.45), TEETH, "body", fuse=False)

# The hands: floating free in front at each side, the back of the hand up, three clawed fingers drooping forward
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    H = np.array([0.36, -0.1, 0.4])
    k.add(f"palm{side}", ell(H * m, (0.06, 0.055, 0.032), rot=(-15, 0, 0)), PURPLE, f"arm{side}", fuse=False)
    k.add(f"wrist{side}", tube(np.array([H + (0.0, 0.03, 0.005), H + (-0.02, 0.09, 0.02)]) * m, [0.036, 0.022]),
          PURPLE, f"arm{side}", fuse=False)
    for j, dx in enumerate((-0.035, 0.0, 0.035)):
        base = H + np.array((dx, -0.035, 0.0))
        pts = [base, base + (dx * 0.4, -0.04, -0.025), base + (dx * 0.55, -0.05, -0.07),
               base + (dx * 0.55, -0.025, -0.105)]
        k.add(f"finger{side}{j}", tube(np.array(pts) * m, [0.021, 0.017, 0.011, 0.002]), PURPLE, f"arm{side}",
              fuse=False)
    tb = H + np.array((-0.055, -0.01, -0.005))
    k.add(f"thumb{side}", tube(np.array([tb, tb + (-0.035, -0.035, -0.04), tb + (-0.025, -0.02, -0.075)]) * m,
                               [0.017, 0.012, 0.002]), PURPLE, f"arm{side}", fuse=False)

k.build(voxel=0.006, smooth=3)

# The eyes: great white wedges, high at the outer side, their tops slanting steeply down toward the middle, a small
# black pupil toward the inner corner. u runs outward, v up.
WHITE, PUP, LID, INK = srgb("#F6F4F8"), srgb("#141018"), srgb(PURPLE), srgb("#2A1E3E")
SHADE = srgb("#CFC8DA")
WEDGE = [(0.98, 0.82), (0.5, 0.42), (-0.2, 0.0), (-0.95, -0.38), (-0.75, -0.6), (0.1, -0.68), (0.7, -0.55),
         (0.95, -0.2)]
def white(u, v):
    c = np.tile(WHITE, (len(u), 1))
    w = smoothstep(-0.35, -0.75, v)[:, None] * 0.5
    return c * (1 - w) + SHADE[None] * w
de = unit(0.44, -0.85, 0.28)
EH = 0.07
def frame(n):
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    for side in (1, -1):
        n = de * np.array([side, 1, 1.0])
        c = C + n * np.array([RX, RY, RZ])
        right, up = frame(n)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (WEDGE if side == 1 else WEDGE[::-1])])
        k.decal(c, n, (EH, EH), flip(eye_look(state, white, LID, INK, line=0.1)), eps=0.006, outline=outline,
                rings=20, segs=56)
        if state in ("Open", "Fierce"):
            pc = c + right * side * (-0.42 * EH) + up * (-0.25 * EH)
            k.decal(pc, n, (0.008, 0.014), PUP, eps=0.009, rings=5, segs=20)

k.eye_states(eyes)
k.export(out)
