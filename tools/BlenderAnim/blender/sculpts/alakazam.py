# Alakazam sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/alakazam.py -- <the export's Alakazam.glb> <out.glb>
# Kadabra grown (kadabra.py), in the same yellow and brown and with the same eyes: thinner and taller, no tail; the
# brown over the whole chest and the shoulders, a brown cuff at each elbow and a brown pad on each knee, a yellow
# pelvis like shorts; a bigger head whose long ears sweep up and out, a long snout pointing down, a huge moustache
# streaming out to the sides to frayed ends; hands held forward at the waist, a silver spoon in each, the bowls
# turned in toward the body; big feet with white claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, polygon, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, BROWN, RED, WHITE = "#F2C232", "#8A634E", "#D8362F", "#F7F4EC"
GOLD, SILVER, CLAW = "#D9A630", "#D6DBE0", "#F4F1E8"
H = np.array([0, -0.02, 1.0])            # the head's centre


def cap(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.5, V[:, 2])      # flat beneath, domed on top
    return V


def spoon(grip, tip, bone):
    """A silver spoon: a thin handle from the fist to a flat oval bowl. Loose, metal."""
    grip, tip = np.array(grip, float), np.array(tip, float)
    d = tip - grip; L = np.linalg.norm(d); d /= L
    neck = grip + d * L * 0.62
    k.add("spoonhandle", tube([grip - d * 0.06, grip + d * 0.2 * L, neck], [0.011, 0.01, 0.007], seg=12), SILVER,
          bone, fuse=False, surface="metal")
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    n = np.cross(side, d)
    V, F = ell((0, 0, 0), (0.042, 0.065, 0.011), sub=3)     # flat, its face up
    V = V @ np.stack([side, d, n], axis=1).T + neck + d * 0.062
    k.add("spoonbowl", (V, F), SILVER, bone, fuse=False, surface="metal")


def claw(a, d, length, r, bone):
    d = np.array(d, float); d /= np.linalg.norm(d)
    a = np.array(a, float)
    k.add("claw", tube([a, a + d * length * 0.6, a + d * length], [r, r * 0.6, 0.002], seg=10), CLAW, bone,
          fuse=False, surface="shell")


# The body: a slim brown chest, a narrow waist, a yellow pelvis
k.add("chest", ell((0, 0.0, 0.76), (0.125, 0.1, 0.12)), BROWN, "body")
k.add("waist", ell((0, 0.0, 0.64), (0.085, 0.075, 0.08)), BROWN, "body")
k.add("pelvis", ell((0, 0.0, 0.54), (0.11, 0.09, 0.07)), YELLOW, "body")
k.add("neck", ell((0, 0.0, 0.87), (0.055, 0.05, 0.05)), YELLOW, "head")
# the brown runs from the neck to the waist, where the yellow pelvis begins in a crisp line, dipping to a point
def trunks(P):
    edge = 0.585 - 0.035 * np.clip(1 - np.abs(P[:, 0]) / 0.08, 0, 1) * (P[:, 1] < 0)
    return inside(P[:, 2] - edge, 0.0025)
k.paint(lambda P: (1 - trunks(P)) * (P[:, 2] < 0.86) * (P[:, 2] > 0.5) * (np.abs(P[:, 0]) < 0.15), BROWN)
k.paint(lambda P: trunks(P) * (P[:, 2] > 0.45), YELLOW, on=["waist", "pelvis"])
k.add("pad", ell((0.14, 0.0, 0.82), (0.08, 0.085, 0.065), rot=(0, 28, 0), deform=cap), BROWN, "arm{s}", mirror=True)

# The head: bigger than Kadabra's, the long snout pointing forward and down to a dark nose
k.add("head", ell(H, (0.135, 0.12, 0.12)), YELLOW, "head")
k.add("cheeks", ell((0, -0.065, 0.955), (0.105, 0.085, 0.075)), YELLOW, "head")
TIP = np.array([0, -0.235, 0.89])
k.add("snout", tube([(0, -0.09, 0.98), (0, -0.17, 0.94), TIP], [0.06, 0.042, 0.022]), YELLOW, "head")
k.add("nose", ell(TIP + np.array([0, -0.01, -0.004]), (0.016, 0.012, 0.012)), "#3A2A20", "head", fuse=False,
      surface="skin")
k.add("brow", tube([(0.03, -0.1, 1.028), (0.07, -0.088, 1.045), (0.1, -0.06, 1.052)], [0.02, 0.022, 0.016]),
      YELLOW, "head", mirror=True)
# Long pointed ears sweeping up and out
k.add("ear", blade([(0.06, 0.01, 1.05), (0.13, 0.03, 1.13), (0.2, 0.05, 1.22), (0.27, 0.06, 1.3)],
                   [0.065, 0.045, 0.025, 0.004], (0, 1, 0), 0.38), YELLOW, "ear{s}", mirror=True)

# The huge moustache: from under the nose out over the shoulders, drooping, to ends frayed into points
def moustache():
    # a ribbon, flat to the front, widening a little toward its frayed end
    pts = [(0.02, -0.228, 0.872), (0.09, -0.222, 0.845), (0.18, -0.185, 0.8), (0.27, -0.145, 0.775), (0.34, -0.115, 0.77)]
    return blade(pts, [0.013, 0.02, 0.024, 0.026, 0.026], (0, 1, 0), 0.45)
k.add("moustache", moustache(), GOLD, "head", fuse=False, mirror=True)
END = np.array([0.335, -0.117, 0.77])
for i, (d, L) in enumerate([((1, 0.2, 0.75), 0.09), ((1, 0.25, 0.3), 0.11), ((1, 0.25, -0.15), 0.12),
                           ((1, 0.2, -0.6), 0.1), ((0.75, 0.2, -1.0), 0.08)]):
    d = unit(*d)
    k.add(f"fray{i}", blade([END, END + d * L * 0.5, END + d * L], [0.013, 0.009, 0.002], (0, 1, 0), 0.5), GOLD,
          "head", fuse=False, mirror=True)

# Arms: brown shoulders, yellow upper arms, brown cuffs at the elbows, yellow forearms forward to the hands at the waist
for s, bone in ((1, "armR"), (-1, "armL")):
    m = np.array([s, 1, 1.0])
    k.add("upper" + bone[-1], tube([np.array(p) * m for p in [(0.16, -0.01, 0.8), (0.27, -0.02, 0.7), (0.32, -0.04, 0.62)]],
                                   [0.032, 0.03, 0.028]), YELLOW, bone)
    k.add("cuff" + bone[-1], ell(np.array([0.32, -0.045, 0.62]) * m, (0.048, 0.045, 0.045)), BROWN, bone)
    k.add("fore" + bone[-1], tube([np.array(p) * m for p in [(0.32, -0.05, 0.6), (0.34, -0.12, 0.58), (0.35, -0.18, 0.58)]],
                                  [0.028, 0.025, 0.026]), YELLOW, bone)
    hand = np.array([0.35, -0.21, 0.575])
    k.add("hand" + bone[-1], ell(hand * m, (0.04, 0.035, 0.035)), YELLOW, bone)
    # fingers closed round the handle, from above, and a thumb
    for i, dy in enumerate((-0.02, 0.0, 0.02)):
        a = hand + np.array([0.0, dy, 0.03])
        k.add(f"fist{i}" + bone[-1], tube([a * m, (a + np.array([-0.035, 0, -0.01])) * m,
                                           (a + np.array([-0.045, 0, -0.045])) * m], [0.014, 0.013, 0.011]),
              YELLOW, bone)
    # the spoon: its handle out past the fist, its bowl turned in toward the body
    spoon(np.array([0.33, -0.215, 0.57]) * m, np.array([0.12, -0.24, 0.6]) * m, bone)
    k.add("butt" + bone[-1], tube([np.array([0.36, -0.212, 0.57]) * m, np.array([0.43, -0.205, 0.565]) * m],
                                  [0.009, 0.008], seg=12), SILVER, bone, fuse=False, surface="metal")

# Legs: thin yellow thighs, brown knee pads, thin shins, big feet with three white claws in front and one behind
for s, bone in ((1, "legR"), (-1, "legL")):
    m = np.array([s, 1, 1.0])
    k.add("thigh" + bone[-1], tube([np.array(p) * m for p in [(0.08, 0.0, 0.53), (0.13, -0.04, 0.4), (0.15, -0.06, 0.3)]],
                                   [0.05, 0.042, 0.036]), YELLOW, bone)
    k.add("knee" + bone[-1], ell(np.array([0.15, -0.065, 0.3]) * m, (0.055, 0.05, 0.05), deform=cap), BROWN, bone)
    k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.15, -0.06, 0.28), (0.15, 0.0, 0.15), (0.15, 0.01, 0.07)]],
                                  [0.034, 0.03, 0.03]), YELLOW, bone)
    k.add("foot" + bone[-1], ell(np.array([0.155, -0.04, 0.04]) * m, (0.07, 0.095, 0.04)), YELLOW, bone)
    for i, dx in enumerate((-0.04, 0.0, 0.04)):
        a = np.array([0.155 + dx, -0.1, 0.035])
        b = np.array([0.155 + dx * 1.4, -0.145, 0.022])
        k.add(f"toe{i}" + bone[-1], tube([a * m, b * m], [0.022, 0.018]), YELLOW, bone)
        claw(b * m + np.array([0, -0.012, 0.0]), (dx * 0.5 * s, -1, -0.2), 0.04, 0.011, bone)
    claw(np.array([0.155, 0.05, 0.03]) * m, (0, 1, -0.3), 0.035, 0.01, bone)

# The eyes: Kadabra's narrow almonds under the brow, a little larger
HE = 0.035
SL = 0.4
def eye_top(u):
    return 0.18 + SL * u
def eye_bottom(u):
    return eye_top(u) - 0.7 * (1 - u * u) ** 0.8
ALMOND = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]
de = unit(0.45, -0.88, 0.1)
E = np.array([0.062, -0.098, 1.01])
LID, INK = srgb(YELLOW), srgb("#2A1E14")
SCLERA, PUPIL = srgb(WHITE), srgb("#1A1414")
def eye_open(state):
    def look(u, v):
        c = np.tile(SCLERA, (len(u), 1))
        top = np.minimum(eye_top(u), 0.0 + 0.6 * u) if state == "Fierce" else eye_top(u)
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

k.build(voxel=0.0072)
k.eye_states(eyes)
k.export(out)
