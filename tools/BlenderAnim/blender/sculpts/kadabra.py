# Kadabra sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kadabra.py -- <the export's Kadabra.glb> <out.glb>
# Abra grown, in its yellow and brown: standing on its two long legs with clawed toes, a brown plate over the chest and
# big brown domes over the shoulders, three red waves on the yellow belly, a fox's long snout with a dark nose, narrow
# eyes under a brow, a red star on the forehead, tall pointed ears, a long drooping moustache, a thick tail with a
# brown band near its root sweeping round to one side, and one silver spoon held up in one hand (on -X, as the art
# shows it on the viewer's left), the other hand raised open.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, polygon, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, BROWN, DARK, RED, WHITE = "#F2C232", "#8A634E", "#2A2424", "#D8362F", "#F7F4EC"
GOLD, SILVER, CLAW = "#D9A630", "#D6DBE0", "#F4F1E8"
H = np.array([0, -0.01, 0.93])           # the head's centre


def cap(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.5, V[:, 2])      # flat beneath, domed on top
    return V


def spoon(grip, tip, bone):
    """A silver spoon: a thin handle from the fist to a flat oval bowl. Loose, metal."""
    grip, tip = np.array(grip, float), np.array(tip, float)
    d = tip - grip; L = np.linalg.norm(d); d /= L
    neck = grip + d * L * 0.62
    k.add("spoonhandle", tube([grip - d * 0.05, grip + d * 0.2 * L, neck], [0.011, 0.01, 0.007], seg=12), SILVER,
          bone, fuse=False, surface="metal")
    side = np.cross(d, (0, -1.0, 0.0001)); side /= np.linalg.norm(side)
    n = np.cross(side, d)
    V, F = ell((0, 0, 0), (0.042, 0.011, 0.065), sub=3)
    V = V @ np.stack([side, n, d], axis=1).T + neck + d * 0.062
    k.add("spoonbowl", (V, F), SILVER, bone, fuse=False, surface="metal")


def claw(a, d, length, r, bone):
    d = np.array(d, float); d /= np.linalg.norm(d)
    a = np.array(a, float)
    k.add("claw", tube([a, a + d * length * 0.6, a + d * length], [r, r * 0.6, 0.002], seg=10), CLAW, bone,
          fuse=False, surface="shell")


# The body: a narrow chest under a brown plate, a round yellow belly, hips
k.add("chest", ell((0, 0.0, 0.69), (0.14, 0.115, 0.12)), BROWN, "body")
k.add("belly", ell((0, -0.01, 0.56), (0.125, 0.11, 0.11)), YELLOW, "body")
k.add("hips", ell((0, 0.01, 0.47), (0.13, 0.11, 0.07)), YELLOW, "body")
k.add("neck", ell((0, 0.0, 0.8), (0.065, 0.06, 0.06)), YELLOW, "head")
# the plate's lower edge a crisp line, dipping a little at the middle of the front
def plate(P):
    edge = 0.615 + 0.02 * np.cos(np.clip(np.arctan2(P[:, 0], -P[:, 1]), -math.pi, math.pi))
    return inside(edge - P[:, 2], 0.0025) * (P[:, 2] < 0.79) * (np.abs(P[:, 0]) < 0.2)
k.paint(plate, BROWN)
k.paint(lambda P: (1 - plate(P)) * (P[:, 2] < 0.7) * (P[:, 2] > 0.45), YELLOW, on=["chest"])
# the shoulders' big brown domes
k.add("pad", ell((0.15, 0.0, 0.755), (0.085, 0.09, 0.07), rot=(0, 28, 0), deform=cap), BROWN, "arm{s}", mirror=True)

# The head: a fox's, a long snout standing forward and a little down to a dark nose
k.add("head", ell(H, (0.118, 0.11, 0.105)), YELLOW, "head")
k.add("cheeks", ell((0, -0.06, 0.895), (0.1, 0.08, 0.07)), YELLOW, "head")
TIP = np.array([0, -0.245, 0.875])
k.add("snout", tube([(0, -0.09, 0.91), (0, -0.17, 0.89), TIP], [0.065, 0.045, 0.024]), YELLOW, "head")
k.add("nose", ell(TIP + np.array([0, -0.012, 0.004]), (0.017, 0.012, 0.012)), "#3A2A20", "head", fuse=False,
      surface="skin")
# the brows: ridges over the eyes, sloping down to the snout
k.add("brow", tube([(0.03, -0.1, 0.958), (0.07, -0.088, 0.975), (0.1, -0.06, 0.982)], [0.02, 0.022, 0.016]),
      YELLOW, "head", mirror=True)
# Tall pointed ears, swept a little out and back
k.add("ear", blade([(0.06, 0.01, 0.98), (0.1, 0.03, 1.08), (0.14, 0.05, 1.2)], [0.06, 0.04, 0.005], (0, 1, 0), 0.4),
      YELLOW, "ear{s}", mirror=True)

# The long moustache: from the snout's sides out and drooping down past the chest, thin at its ends
k.add("moustache", tube([(0.03, -0.2, 0.872), (0.08, -0.21, 0.86), (0.13, -0.2, 0.8), (0.155, -0.19, 0.71),
                         (0.16, -0.17, 0.62), (0.15, -0.15, 0.56)], [0.012, 0.013, 0.012, 0.01, 0.007, 0.002], seg=12),
      GOLD, "head", fuse=False, mirror=True)

# Arms: the spoon arm (-X) up with the spoon over the head; the other (+X) raised open, three claws
for s, bone in ((-1, "armL"), (1, "armR")):
    m = np.array([s, 1, 1.0])
    if s < 0:
        pts = [(0.17, -0.01, 0.73), (0.27, -0.07, 0.65), (0.3, -0.11, 0.8), (0.3, -0.12, 0.86)]
        hand = np.array([0.3, -0.12, 0.885])
    else:
        pts = [(0.17, -0.01, 0.73), (0.28, -0.06, 0.62), (0.33, -0.1, 0.75)]
        hand = np.array([0.335, -0.105, 0.77])
    k.add("arm" + bone[-1], tube([np.array(p) * m for p in pts], [0.036] + [0.03] * (len(pts) - 2) + [0.027]),
          YELLOW, bone)
    k.add("hand" + bone[-1], ell(hand * m, (0.035, 0.032, 0.035)), YELLOW, bone)
    if s < 0:
        # fingers curled round the spoon's handle
        for i, dz in enumerate((-0.02, 0.0, 0.02)):
            k.add(f"fist{i}L", tube([np.array([-0.3, -0.12, hand[2] + dz]), np.array([-0.315, -0.155, hand[2] + dz]),
                                     np.array([-0.3, -0.165, hand[2] + dz + 0.004])], [0.014, 0.013, 0.011]),
                  YELLOW, bone)
        spoon((-0.29, -0.155, 0.875), (-0.37, -0.17, 1.12), bone)
    else:
        for i, (dx, dy) in enumerate(((-0.03, 0.0), (0.0, -0.01), (0.03, 0.0))):
            base = hand + np.array([dx * 0.6, dy - 0.01, 0.02])
            tipf = hand + np.array([dx * 1.4, dy - 0.03, 0.085])
            k.add(f"finger{i}R", tube([base, (base + tipf) / 2 + np.array([0, -0.01, 0]), tipf], [0.014, 0.012, 0.01]),
                  YELLOW, bone)
            claw(tipf, (dx, -0.4, 1), 0.03, 0.008, bone)

# Legs: thick thighs, thin shins, long feet with three clawed toes
for s, bone in ((1, "legR"), (-1, "legL")):
    m = np.array([s, 1, 1.0])
    k.add("thigh" + bone[-1], tube([np.array(p) * m for p in [(0.09, 0.0, 0.48), (0.15, -0.06, 0.34), (0.16, -0.07, 0.26)]],
                                   [0.07, 0.062, 0.045]), YELLOW, bone)
    k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.16, -0.07, 0.26), (0.155, 0.0, 0.14), (0.15, 0.02, 0.06)]],
                                  [0.04, 0.034, 0.03]), YELLOW, bone)
    k.add("foot" + bone[-1], ell(np.array([0.15, -0.03, 0.033]) * m, (0.05, 0.08, 0.032)), YELLOW, bone)
    for i, dx in enumerate((-0.03, 0.0, 0.03)):
        a = np.array([0.15 + dx, -0.08, 0.03])
        b = np.array([0.15 + dx * 1.5, -0.13, 0.018])
        k.add(f"toe{i}" + bone[-1], tube([a * m, b * m], [0.017, 0.014]), YELLOW, bone)
        claw(b * m + np.array([0, -0.01, 0.0]), (dx * 0.5 * s, -1, -0.25), 0.03, 0.009, bone)

# The thick fox tail: back from the rump, round to -X and down, its tip flicked up; a brown band near its root
TAIL = [(0, 0.09, 0.46), (-0.06, 0.2, 0.4), (-0.17, 0.27, 0.32), (-0.3, 0.28, 0.27), (-0.42, 0.23, 0.29),
        (-0.49, 0.18, 0.37), (-0.5, 0.15, 0.45)]
k.add("tail", tube(TAIL, [0.07, 0.115, 0.135, 0.12, 0.085, 0.05, 0.006]), YELLOW, "tail")
B0 = np.array(TAIL[2]); BD = unit(-0.75, 0.25, -0.45)
k.paint(lambda P: inside(np.abs((P - B0) @ BD) - 0.03, 0.0025), BROWN, on=["tail"])

# The three red waves on the belly, stacked
CY, CR = srgb(YELLOW), srgb(RED)
def waves(u, v):
    c = np.tile(CY, (len(u), 1))
    for v0 in (-0.55, 0.0, 0.55):
        line = v0 + 0.13 * np.sin(u * 2.4 * math.pi)
        c[(np.abs(v - line) < 0.12) & (np.abs(u) < 0.85)] = CR
    return c
k.decal((0, -0.115, 0.545), unit(0, -1, -0.1), (0.045, 0.04), waves, eps=0.003, rings=14, segs=56)

# The red star on the forehead
STAR = [(math.cos(math.pi / 2 + i * math.pi / 5) * (1.0 if i % 2 == 0 else 0.45),
         math.sin(math.pi / 2 + i * math.pi / 5) * (1.0 if i % 2 == 0 else 0.45)) for i in range(10)]
k.decal((0, -0.09, 1.0), unit(0, -1, 0.55), (0.024, 0.024), RED, eps=0.004, rings=8, segs=60, outline=polygon(STAR))

# The eyes: narrow almonds slanting down to the snout, their top cut straight by the brow; white, a small dark pupil
HE = 0.033
SL = 0.4
def eye_top(u):
    return 0.18 + SL * u
def eye_bottom(u):
    return eye_top(u) - 0.7 * (1 - u * u) ** 0.8
ALMOND = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]
de = unit(0.45, -0.88, 0.1)
E = np.array([0.062, -0.098, 0.94])
LID, INK = srgb(YELLOW), srgb("#2A1E14")
SCLERA, PUPIL = srgb(WHITE), srgb("#1A1414")
def eye_open(state):
    def look(u, v):
        c = np.tile(SCLERA, (len(u), 1))
        top = np.minimum(eye_top(u), 0.0 + 0.6 * u) if state == "Fierce" else eye_top(u)
        c[v > top - 0.14] = INK
        c[v > top] = LID
        c[((u + 0.05) / 0.26) ** 2 + ((v + 0.05) / 0.32) ** 2 < 1] = PUPIL
        c[(v > top - 0.14) & (v <= top)] = INK
        c[v > top] = LID
        return c
    return look
def eyes(state):
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        look = eye_open(state) if state in ("Open", "Fierce") else eye_look(state, eye_open("Open"), LID, INK, line=0.18)
        k.decal(E * m, de * m, (HE, HE), flip(look), eps=0.006, outline=outline, rings=20, segs=56)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
