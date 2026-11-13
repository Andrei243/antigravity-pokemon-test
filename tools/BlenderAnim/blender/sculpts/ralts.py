# Ralts sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ralts.py -- <the export's Ralts.glb> <out.glb>
#
# The design, from the references: a small white figure in a gown that flares to the ground and trails behind in a
# couple of points; thin white arms hanging in front; a little white face whose chin alone shows under a big green
# bowl of hair, which comes down over the eyes and is cut into panels by dark lines (a slit up the front, curving
# lines up the sides and back); two flat red horns standing in it, a tall one at the front and a short one behind;
# a tiny red dot of a mouth on the chin.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, boxy, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, GREEN, GROOVE, HORN, RED, INKH = "#F4F5FA", "#7FCB72", "#2F5C2C", "#E26878", "#D84A62", "#2A1418"
H = np.array([0, -0.008, 0.255])          # the head's middle
HR = np.array([0.068, 0.064, 0.064])
HC = np.array([0, 0.004, 0.262])         # the bowl of hair: its rim's middle
HCR = np.array([0.155, 0.15, 0.19])


# The gown: narrow at the shoulders, flaring to the ground, its bottom flat on it
def flare(V):
    """A cone with straight sides (the sphere's rings set to a radius by their height), its bottom a flat disc."""
    V = V.copy()
    z = -0.9 + 0.03 * np.logaddexp(0, (V[:, 2] + 0.9) / 0.03)   # flat at the bottom, its edge eased
    ring = np.sqrt(np.clip(1 - z ** 2, 1e-6, 1))          # a ring's own radius (the bottom disc's inside keeps its share)
    t = (1 - z) / 1.9                                    # 0 at the top, 1 at the bottom
    r = (0.24 + 0.76 * t ** 1.15) * np.minimum(1, ring / 0.12)
    V[:, 0] *= r / ring; V[:, 1] *= r / ring
    V[:, 2] = z
    return V
k.add("gown", ell((0, 0.02, 0.105), (0.112, 0.1, 0.11), deform=flare), WHITE, "body")
# the train behind, lying on the ground, ending in two points
k.add("train", ell((0, 0.09, 0.024), (0.075, 0.11, 0.024), rot=(-4, 0, 0)), WHITE, "body")
k.add("hem", blade([(0.02, 0.16, 0.018), (0.034, 0.215, 0.03), (0.042, 0.24, 0.048)], [0.016, 0.01, 0.004],
                   (0, 0, 1), 0.55), WHITE, "body", mirror=True)
k.add("neck", tube([(0, 0.0, 0.17), (0, -0.004, 0.23)], [0.021, 0.02]), WHITE, "head")
k.add("head", ell(H, HR), WHITE, "head")

# Thin arms hanging in front (loose, so they stand apart from the gown)
k.add("arm", tube([(0.032, -0.02, 0.168), (0.058, -0.045, 0.13), (0.05, -0.066, 0.092)], [0.013, 0.011, 0.012]),
      WHITE, "arm{s}", fuse=False, mirror=True)
k.add("hand", ell((0.048, -0.07, 0.084), (0.014, 0.013, 0.017)), WHITE, "arm{s}", fuse=False, mirror=True)


# The bowl of hair: a dome, its sides nearly upright, cut flat at its rim (loose, so its rim is crisp)
def dome(V):
    V = boxy(2.4)(V)
    V[:, 2] = np.maximum(V[:, 2], 0)
    return V
k.add("hair", ell(HC, HCR, deform=dome, sub=5), GREEN, "head", fuse=False)
def on_dome(th, e, lift=1.008):
    """A point on the bowl, th round from the front (+ toward +X), e up from the rim, in radians."""
    U = np.stack([np.sin(th) * np.cos(e), -np.cos(th) * np.cos(e), np.sin(e)], axis=1)
    return HC + dome(U) * HCR * lift
def groove(name, th, e):
    pts = on_dome(np.array(th, float), np.array(e, float))
    k.add(name, tube(pts, [0.0026] * len(pts), seg=8, per=6), GROOVE, "head", fuse=False)
groove("slit", [0.0, 0.0, 0.0], [0.01, 0.3, 0.62])
for s in (1, -1):
    side = "R" if s > 0 else "L"
    groove(f"lineF{side}", [s * 0.95, s * 0.82, s * 0.6, s * 0.38], [0.01, 0.45, 0.85, 1.12])
    groove(f"lineB{side}", [s * 2.25, s * 2.38, s * 2.6, s * 2.85], [0.01, 0.45, 0.85, 1.1])

# The horns: flat red blades standing in the bowl, a tall one at the front and a shorter one behind
k.add("hornF", blade([(0.012, -0.05, 0.385), (0.018, -0.095, 0.44), (0.027, -0.11, 0.49), (0.033, -0.1, 0.522)],
                     [0.046, 0.05, 0.038, 0.016], (1, 0, 0), 0.3), HORN, "head", fuse=False)
k.add("hornB", blade([(-0.014, 0.05, 0.39), (-0.022, 0.1, 0.445), (-0.028, 0.125, 0.485)],
                     [0.038, 0.032, 0.008], (1, 0, 0), 0.3), HORN, "head", fuse=False)

# The tiny red mouth on the chin
INK, LID, IRIS = srgb(INKH), srgb(WHITE), srgb(RED)
def mouth(u, v):
    c = np.tile(IRIS, (len(u), 1))
    c[u * u + v * v > 0.6] = INK
    return c
fm = unit(0, -1, -0.45)
k.decal(H + fm * 0.06, fm, (0.0065, 0.0065), mouth, eps=0.003, rings=6, segs=32)

# The eyes, under the rim of the hair (the design keeps them hidden): red with a dark pupil and a glint
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(u / 0.7) ** 2 + (v / 0.85) ** 2 < 1] = IRIS
    c[(u / 0.3) ** 2 + (v / 0.42) ** 2 < 1] = INK
    c[((u - 0.15) / 0.18) ** 2 + ((v - 0.4) / 0.18) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.42, -0.85, 0.55)
def eyes(state):
    k.decal(H + de * 0.06, de, (0.016, 0.016), eye_look(state, eye, LID, INK, line=0.16), eps=0.003, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.004)
k.eye_states(eyes)
k.export(out)
