# Gengar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gengar.py -- <the export's Gengar.glb> <out.glb>
# Grown from Haunter's script: the same purple and the same wedge eyes with a small pupil, now red; the grin is
# carved as Haunter's is, wider, and filled with a row of white teeth left loose so their edges stay square. A round
# body that is all head, two pointed ears, a fringe of spikes over the crown and down the back, stubby arms and legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, MOUTH, TEETH = "#7E67BC", "#2A1838", "#F2F3F8"
C = np.array([0.0, 0.0, 0.45])            # the body's middle, between the body and head bones
RX, RY, RZ = 0.27, 0.23, 0.26

# The body: one round mass, its upper half on the head bone so the face turns with it
k.add("belly", ell(C + (0, 0.01, -0.07), (RX * 0.98, RY * 0.98, RZ * 0.95)), PURPLE, "body")
k.add("head", ell(C + (0, -0.005, 0.03), (RX, RY, RZ * 0.9)), PURPLE, "head")


def spike(name, d, length, base_r, bone, flat=0.6, sink=0.86):
    """A spike out of the body along d (from its middle), flattened across the side it is seen from."""
    d = unit(*d)
    base = C + d * np.array([RX, RY, RZ]) * sink
    mid = C + d * np.array([RX, RY, RZ]) * (1.0 + length * 0.45)
    tip = C + d * np.array([RX, RY, RZ]) * (1.0 + length)
    ax = np.cross((1.0, 0, 0), d)
    if np.linalg.norm(ax) < 1e-3:
        ax = np.array([0, 1.0, 0])
    k.add(name, blade([base, mid, tip], [base_r, base_r * 0.5, 0.004], ax, flat), PURPLE, bone)


# The ears: two great points rising out and up from the top of the head, flat front to back
k.add("ear", blade([(0.15, 0.01, 0.6), (0.26, 0.02, 0.73), (0.37, 0.03, 0.87)], [0.085, 0.045, 0.004], (0, 1, 0), 0.5),
      PURPLE, "head", mirror=True)

# The fringe: spikes over the crown between the ears, and down the back
CROWN = [((0.0, 0.25, 1.0), 0.36, 0.06), ((0.09, 0.5, 0.85), 0.28, 0.05), ((-0.09, 0.5, 0.85), 0.28, 0.05),
         ((0.0, 0.8, 0.6), 0.3, 0.055), ((0.12, 0.85, 0.35), 0.22, 0.045), ((-0.12, 0.85, 0.35), 0.22, 0.045),
         ((0.0, 0.98, 0.1), 0.24, 0.05), ((0.0, 0.95, -0.25), 0.18, 0.04)]
for i, (d, length, r) in enumerate(CROWN):
    spike(f"spike{i}", d, length, r, "head" if d[2] > 0.3 else "body")

# The arms: short and thick, out to the side, each ending in a hand of three short points
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube(np.array([(0.2, -0.04, 0.43), (0.3, -0.05, 0.45), (0.37, -0.06, 0.47)]) * m,
                             [0.065, 0.055, 0.05]), PURPLE, f"arm{side}")
    H = np.array([0.39, -0.06, 0.475])
    k.add(f"hand{side}", ell(H * m, (0.045, 0.04, 0.05)), PURPLE, f"arm{side}")
    for j, d in enumerate([(0.6, -0.1, 0.8), (1.0, -0.15, 0.1), (0.7, -0.1, -0.7)]):
        d = unit(*d)
        pts = [H + d * 0.02, H + d * 0.055, H + d * 0.085]
        k.add(f"finger{side}{j}", blade(np.array(pts) * m, [0.026, 0.016, 0.003], (0, 1, 0), 0.6), PURPLE,
              f"arm{side}", fuse=False, surface="fur")

# The legs: stubby, the feet wide with three short claws to the front
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"leg{side}", tube(np.array([(0.12, 0.0, 0.26), (0.135, -0.01, 0.15), (0.14, -0.02, 0.07)]) * m,
                             [0.085, 0.075, 0.062]), PURPLE, f"leg{side}")
    k.add(f"foot{side}", ell(np.array((0.14, -0.05, 0.045)) * m, (0.072, 0.09, 0.045)), PURPLE, f"leg{side}")
    for j, dx in enumerate((-0.035, 0.0, 0.035)):
        b = np.array((0.14 + dx, -0.12, 0.03))
        k.add(f"claw{side}{j}", blade(np.array([b, b + (dx * 0.3, -0.03, -0.004), b + (dx * 0.4, -0.05, -0.01)]) * m,
                                      [0.02, 0.012, 0.003], (0, 0, 1), 0.55), PURPLE, f"leg{side}", fuse=False,
              surface="fur")

# The tail: a short point behind, low on the back
k.add("tail", tube([(0, 0.17, 0.3), (0, 0.27, 0.25), (0, 0.34, 0.22), (0, 0.39, 0.215)], [0.07, 0.045, 0.02, 0.003]),
      PURPLE, "tail")

# The grin: a wide crescent carved low on the face, its corners curling up and back round the head, dark within
MW, MD, MH = 0.2, 0.06, 0.05
MC = np.array([0.0, -RY * math.sqrt(1 - ((0.385 - C[2]) / RZ) ** 2) + 0.012, 0.385])
LIFT, BACK = 1.15, 1.0
def grin(V):
    V = V.copy()
    V[:, 2] += LIFT * V[:, 0] ** 2
    V[:, 1] += BACK * V[:, 0] ** 2
    return V
k.add("mouth", ell(MC, (MW, MD, MH), deform=grin), MOUTH, "head", carve=True)

# The teeth: one row of tall white teeth filling the grin from lip to lip, each loose and square, set just inside the
# opening and turned with the face, the gaps between them showing the dark within
def face_y(x, z):
    return -RY * math.sqrt(max(1 - (x / RX) ** 2 - ((z - C[2]) / RZ) ** 2, 0.0))
N_TEETH = 7
step = 2 * MW * 0.83 / N_TEETH
for i in range(N_TEETH):
    x = (i - (N_TEETH - 1) / 2) * step
    X = x / MW
    zc = MC[2] + MH * LIFT * X * X
    h = MH * math.sqrt(max(1 - X * X, 0.05)) * 1.08
    y = face_y(x, zc) + 0.02
    nx, ny = x / RX ** 2, y / RY ** 2
    turn = math.degrees(math.atan2(nx, -ny))
    k.add(f"tooth{i}", ell((x, y, zc), (step * 0.45, 0.008, h), rot=(0, 0, turn), deform=boxy(5)), TEETH, "head",
          fuse=False, surface="shell")

# The eyes: red wedges, high at the outer side with their tops slanting down to the middle, a darker red under the
# brow, a small black pupil toward the inner corner. u runs outward, v up.
RED, DARKRED, PUP = srgb("#E8363E"), srgb("#A81C2C"), srgb("#120A14")
LID, INK = srgb(PURPLE), srgb("#2A1E3E")
WEDGE = [(0.97, 0.6), (0.4, 0.22), (-0.3, -0.1), (-0.96, -0.32), (-0.9, -0.48), (-0.6, -0.66), (-0.2, -0.76),
         (0.25, -0.74), (0.62, -0.6), (0.88, -0.36), (0.98, -0.05)]
def red(u, v):
    c = np.tile(RED, (len(u), 1))
    top = -0.32 + 0.48 * (u + 0.96)
    w = smoothstep(-0.2, -0.05, v - top)[:, None]
    return c * (1 - w) + DARKRED[None] * w
de = unit(0.42, -0.86, 0.25)
EW, EH = 0.08, 0.085
def frame(n):
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    for side in (1, -1):
        n = de * np.array([side, 1, 1.0])
        c = C + (0, -0.005, 0.03) + n * np.array([RX, RY, RZ * 0.9])
        right, up = frame(n)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (WEDGE if side == 1 else WEDGE[::-1])])
        k.decal(c, n, (EW, EH), flip(eye_look(state, red, LID, INK, line=0.1)), eps=0.006, outline=outline,
                rings=20, segs=56)
        if state in ("Open", "Fierce"):
            pc = c + right * side * (-0.3 * EW) + up * (-0.2 * EH)
            k.decal(pc, n, (0.009, 0.015), PUP, eps=0.009, rings=5, segs=20)

k.build(voxel=0.007, smooth=3)
k.eye_states(eyes)
k.export(out)
