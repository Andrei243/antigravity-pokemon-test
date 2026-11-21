# Mega Alakazam sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/alakazammega.py -- <the export's Alakazam-Mega.glb> <out.glb>
# Grown from alakazam.py (copied, since that script builds as it is read): the same yellow, brown and eyes. From the
# references: sitting cross-legged in the air; the moustache grown into a great white beard falling from the snout to
# the lap, the largest mass, with two broad fraying sweeps out under the sleeves; deep magenta pauldrons rising to
# points at the shoulders and wide bell sleeves, the hands raised out of them with the fingers spread; a crown of five
# points round the head (no ears of their own), a red gem on the brow; magenta pads on the knees, the yellow shins
# crossed and the feet clasped together in front; five spoons hovering in an arc over the crown.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, polygon, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, BROWN, ROBE, WHITE = "#F2C232", "#8A634E", "#93446F", "#F7F4EC"
BEARD, SILVER, GEM = "#F4F2EE", "#D6DBE0", "#D8362F"
H = np.array([0, -0.025, 0.71])            # the head's centre


def spoon(bowl, d, bone):
    """A silver spoon hovering: a handle from below toward its bowl, which faces the front. Loose, metal."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    bowl = np.array(bowl, float)
    k.add("spoonhandle", tube([bowl - d * 0.15, bowl - d * 0.09, bowl - d * 0.04], [0.016, 0.015, 0.013], seg=12),
          SILVER, bone, fuse=False, surface="metal")
    side = np.cross(d, (0, 1.0, 0)); side /= np.linalg.norm(side)
    n = np.cross(side, d)
    V, F = ell((0, 0, 0), (0.036, 0.052, 0.016), sub=3)
    V = V @ np.stack([side, d, n], axis=1).T + bowl
    k.add("spoonbowl", (V, F), SILVER, bone, fuse=False, surface="metal")


# The body under the beard: a brown chest and a lap between the knees
k.add("chest", ell((0, 0.0, 0.48), (0.13, 0.1, 0.13)), BROWN, "body")
k.add("lap", ell((0, -0.03, 0.33), (0.15, 0.12, 0.08)), BROWN, "body")
k.add("neck", ell((0, -0.02, 0.6), (0.06, 0.055, 0.05)), YELLOW, "head")

# The head: round, the snout long and pointing down into the beard
k.add("head", ell(H, (0.11, 0.1, 0.105)), YELLOW, "head")
k.add("cheeks", ell((0, -0.07, 0.66), (0.085, 0.07, 0.06)), YELLOW, "head")
TIP = np.array([0, -0.165, 0.56])
k.add("snout", tube([(0, -0.09, 0.69), (0, -0.14, 0.62), TIP], [0.05, 0.034, 0.016]), YELLOW, "head")
k.add("brow", tube([(0.025, -0.107, 0.7), (0.06, -0.1, 0.708), (0.09, -0.075, 0.71)], [0.017, 0.018, 0.013]),
      YELLOW, "head", mirror=True)
# The crown of five points: one up, two out to the sides, two smaller below them
def point(a, b, r, name, mirror):
    a, b = np.array(a, float), np.array(b, float)
    k.add(name, blade([a, a + (b - a) * 0.5, b], [r, r * 0.55, 0.004], (0, 1, 0), 0.5), YELLOW, "head", mirror=mirror)
point((0, -0.01, 0.78), (0, 0.0, 0.92), 0.075, "crowntop", False)
point((0.06, -0.01, 0.75), (0.215, 0.01, 0.8), 0.06, "crownup", True)
point((0.08, -0.02, 0.69), (0.18, -0.01, 0.64), 0.045, "crownlow", True)
# The red gem on the brow
k.add("gem", ell((0, -0.112, 0.755), (0.016, 0.012, 0.028)), GEM, "head", fuse=False, surface="skin")

# The beard: a great white mass from under the snout to the lap, ending in uneven points; two broad sweeps out under
# the sleeves fraying into points
k.add("beardtop", ell((0, -0.14, 0.53), (0.095, 0.06, 0.075)), BEARD, "head")
def droop(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.35 * t; V[:, 1] *= 1 - 0.2 * t
    return V
k.add("beard", ell((0, -0.15, 0.42), (0.13, 0.085, 0.15), deform=droop), BEARD, "body")
for x, z, r in ((0.0, 0.22, 0.055), (0.065, 0.25, 0.045), (-0.07, 0.245, 0.043), (0.115, 0.29, 0.036), (-0.115, 0.28, 0.036)):
    k.add("beardpt", blade([(x * 0.6, -0.15, z + 0.12), (x * 0.9, -0.15, z + 0.05), (x, -0.14, z)], [r, r * 0.65, 0.004],
                           (0, 1, 0), 0.6), BEARD, "body")
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    for j, (pts, rr) in enumerate([
            ([(0.07, -0.15, 0.5), (0.18, -0.14, 0.44), (0.3, -0.11, 0.39), (0.43, -0.07, 0.35)], [0.055, 0.05, 0.04, 0.004]),
            ([(0.09, -0.15, 0.45), (0.2, -0.13, 0.37), (0.3, -0.1, 0.32), (0.4, -0.07, 0.28)], [0.05, 0.045, 0.035, 0.004]),
            ([(0.2, -0.12, 0.42), (0.3, -0.1, 0.43), (0.39, -0.08, 0.42)], [0.035, 0.03, 0.004])]):
        k.add(f"sweep{side}{j}", blade(np.array(pts) * m, rr, (0, 1, 0), 0.55), BEARD, "body")

# The pauldrons: deep magenta, rising to points over the shoulders
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"pauldron{side}", ell(np.array([0.14, 0.0, 0.59]) * m, (0.085, 0.08, 0.06)), ROBE, f"arm{side}")
    k.add(f"pauldpt{side}", blade(np.array([(0.11, 0.0, 0.6), (0.19, 0.005, 0.66), (0.27, 0.01, 0.73)]) * m,
                                  [0.085, 0.05, 0.005], (0, 1, 0), 0.6), ROBE, f"arm{side}")
    # the bell sleeve, flaring out to the side
    k.add(f"sleeve{side}", tube(np.array([(0.18, -0.01, 0.55), (0.27, -0.03, 0.5), (0.36, -0.05, 0.46), (0.41, -0.06, 0.44)]) * m,
                                [0.06, 0.075, 0.1, 0.11]), ROBE, f"arm{side}")
    # the forearm out of the sleeve's mouth, raised, the hand open with the fingers spread
    k.add(f"fore{side}", tube(np.array([(0.37, -0.06, 0.5), (0.375, -0.07, 0.58), (0.38, -0.075, 0.63)]) * m,
                              [0.032, 0.028, 0.028]), YELLOW, f"arm{side}")
    hand = np.array([0.38, -0.075, 0.655])
    k.add(f"hand{side}", ell(hand * m, (0.038, 0.03, 0.035)), YELLOW, f"arm{side}")
    for j, d in enumerate([(-0.45, 0, 1), (-0.1, 0, 1), (0.3, 0, 1), (0.85, 0, 0.5)]):
        d = unit(*d)
        a = hand + d * 0.02
        L = 0.075 if j < 3 else 0.05
        k.add(f"finger{side}{j}", tube([a * m, (a + d * L * 0.6) * m, (a + d * L + (0.012 * (j - 1.5), 0, -0.01)) * m],
                                       [0.017, 0.015, 0.013]), YELLOW, f"arm{side}")

# The legs crossed: magenta pads on the knees, yellow shins crossing in front, the feet clasped together
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"thigh{side}", tube(np.array([(0.07, -0.02, 0.29), (0.15, -0.06, 0.24), (0.21, -0.09, 0.2)]) * m,
                               [0.06, 0.055, 0.05]), YELLOW, f"leg{side}")
    k.add(f"knee{side}", ell(np.array([0.22, -0.1, 0.2]) * m, (0.07, 0.07, 0.06)), ROBE, f"leg{side}")
    k.add(f"shin{side}", tube(np.array([(0.2, -0.13, 0.19), (0.11, -0.19, 0.17), (0.02, -0.215, 0.17)]) * m,
                              [0.04, 0.036, 0.034]), YELLOW, f"leg{side}")
    k.add(f"foot{side}", ell(np.array([0.03, -0.23, 0.175]) * m, (0.04, 0.035, 0.045)), YELLOW, f"leg{side}")
    for j, dz in enumerate((-0.025, 0.0, 0.025)):
        a = np.array([0.02, -0.255, 0.175 + dz])
        k.add(f"toe{side}{j}", tube([a * m, (a + (-0.035, -0.005, -0.005)) * m], [0.014, 0.012]), YELLOW, f"leg{side}")

# The five spoons over the crown
for i in range(5):
    a = math.radians(-64 + 32 * i)
    d = np.array([math.sin(a), 0, math.cos(a)])
    spoon(np.array([0, 0.01, 0.8]) + d * 0.3, d, "head")

# The eyes: the base's narrow almonds under the brow
HE = 0.038
SL = 0.4
def eye_top(u):
    return 0.18 + SL * u
def eye_bottom(u):
    return eye_top(u) - 0.7 * (1 - u * u) ** 0.8
ALMOND = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]
de = unit(0.45, -0.88, 0.1)
E = np.array([0.05, -0.098, 0.705])
LID, INK = srgb(YELLOW), srgb("#2A1E14")
SCLERA, PUPIL = srgb(WHITE), srgb("#1A1414")
def eye_open(state):
    def look(u, v):
        c = np.tile(SCLERA, (len(u), 1))
        top = np.minimum(eye_top(u), 0.0 + 0.6 * u) if state == "Fierce" else eye_top(u)
        c[((u + 0.05) / 0.3) ** 2 + ((v + 0.05) / 0.36) ** 2 < 1] = PUPIL
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

k.build(voxel=0.0072, band=0.006)
k.eye_states(eyes)
k.export(out)
