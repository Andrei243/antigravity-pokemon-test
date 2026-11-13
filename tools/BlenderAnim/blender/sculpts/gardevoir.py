# Gardevoir sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gardevoir.py -- <the export's Gardevoir.glb> <out.glb>
# Grown from kirlia.py: the same white, green, horn red and eye red, the same loose bowl of hair with dark grooves
# and the same large gentle eyes.
#
# The design, from the references: tall and elegant; a round green helmet of hair, a bob whose sides come down past
# the cheeks and curl in at their ends, a lock of the fringe swept over one side; a little white face with large red
# eyes and a white point standing out from each cheek; a thin white neck and chest with a flat red horn through it,
# pointing forward and back; long slender green arms held out and down; a white gown from under the chest to the
# ground, flaring and trailing behind, opening at the front over green.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, boxy, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, GREEN, GROOVE, HORN, RED, INKH = "#F4F5FA", "#7FCB72", "#2F5C2C", "#E26878", "#D84A62", "#2A1418"
E = 0.0042                                # a crisp paint's edge
H = np.array([0, -0.028, 0.885])          # the face's middle
HR = np.array([0.064, 0.06, 0.07])
HC = np.array([0, 0.006, 0.927])          # the helmet of hair's middle
HCR = np.array([0.128, 0.118, 0.11])


# The gown: narrow under the chest, flaring to the ground, its hem rippling and trailing a little behind
def flare(V):
    V = V.copy()
    z = -0.9 + 0.03 * np.logaddexp(0, (V[:, 2] + 0.9) / 0.03)
    ring = np.sqrt(np.clip(1 - z ** 2, 1e-6, 1))
    t = (1 - z) / 1.9                                    # 0 at the top, 1 at the bottom
    th = np.arctan2(V[:, 0], -V[:, 1])
    r = (0.2 + 0.8 * t ** 1.25) * np.minimum(1, ring / 0.12)
    r = r * (1 + 0.07 * t ** 2 * np.sin(5 * th + 0.6))    # the cloth's folds, toward the hem
    back = 1 + 0.35 * t ** 2 * np.clip(np.cos(th - math.pi), 0, 1)
    V[:, 0] *= r / ring; V[:, 1] *= r * back / ring
    V[:, 2] = z
    return V
k.add("gown", ell((0, 0.015, 0.355), (0.175, 0.17, 0.36), deform=flare), WHITE, "body")
# the train behind, lying on the ground, ending in two points
k.add("train", ell((0, 0.17, 0.022), (0.14, 0.16, 0.022), rot=(-3, 0, 0)), WHITE, "body")
k.add("hem", blade([(0.03, 0.27, 0.016), (0.055, 0.34, 0.024), (0.07, 0.375, 0.04)], [0.03, 0.016, 0.004],
                   (0, 0, 1), 0.5), WHITE, "body", mirror=True)
# the opening down the front, a little to +X, narrow at its top and wider to the ground: green under it
def opening(P):
    q = P - np.array([0, 0.015, 0])
    ph = np.arctan2(q[:, 0], -q[:, 1])
    w = 0.42 * np.clip((0.62 - P[:, 2]) / 0.62, 0, 1) ** 0.65
    return inside(np.abs(ph - 0.22) - w, E) * (P[:, 2] < 0.62) * (P[:, 1] < 0.05)
k.paint(opening, GREEN)

# A thin white chest and neck
k.add("chest", ell((0, 0.0, 0.748), (0.046, 0.038, 0.066)), WHITE, "body")
k.add("neck", tube([(0, 0.0, 0.79), (0, -0.01, 0.85)], [0.018, 0.016]), WHITE, "head")

# The red horn through the chest: a flat plate forward and up, the same behind (loose, crisp)
k.add("hornF", blade([(0, -0.02, 0.735), (0, -0.085, 0.765), (0, -0.14, 0.8)], [0.05, 0.032, 0.004],
                     (0, 0.3, 1), 0.38), HORN, "body", fuse=False)
k.add("hornB", blade([(0, 0.02, 0.735), (0, 0.085, 0.765), (0, 0.14, 0.8)], [0.05, 0.032, 0.004],
                     (0, -0.3, 1), 0.38), HORN, "body", fuse=False)

# Long slender green arms held out and down, three slim fingers at each hand
k.add("arm", tube([(0.045, 0.0, 0.776), (0.1, -0.015, 0.74), (0.17, -0.035, 0.67), (0.228, -0.05, 0.608)],
                  [0.021, 0.017, 0.015, 0.013]), GREEN, "arm{s}", mirror=True)
k.add("hand", ell((0.24, -0.054, 0.592), (0.017, 0.014, 0.022), rot=(0, -30, 0)), GREEN, "arm{s}", fuse=False,
      mirror=True)
for i, (dx, dy) in enumerate(((0.0, -0.012), (0.012, 0.0), (-0.01, 0.006))):
    k.add(f"finger{i}", tube([(0.245 + dx * 0.5, -0.054 + dy * 0.5, 0.585), (0.262 + dx, -0.06 + dy, 0.548)],
                             [0.0075, 0.003]), GREEN, "arm{s}", fuse=False, mirror=True)

# The face: a white head with a flattened front for the eyes
k.add("head", ell(H, HR), WHITE, "head")
k.add("face", ell(H + np.array([0, -0.014, -0.004]), (0.056, 0.045, 0.058)), WHITE, "head")
# the white points standing out from the cheeks
k.add("cheek", blade([(0.05, -0.06, 0.872), (0.078, -0.072, 0.876), (0.1, -0.078, 0.882)], [0.017, 0.01, 0.002],
                     (0.3, -1, 0), 0.35), WHITE, "head", fuse=False, mirror=True)


# The helmet of hair: a round dome cut off below at a rim, just over the eyes in front and low at the back
def dome(V):
    V = boxy(2.1)(V)
    th = np.arctan2(V[:, 0], -V[:, 1])
    rim = -0.06 - 0.4 * (1 - np.cos(th))
    V = V.copy()
    V[:, 2] = np.maximum(V[:, 2], rim)
    return V
k.add("hair", ell(HC, HCR, deform=dome, sub=5), GREEN, "head", fuse=False)


def on_dome(th, e, lift=1.012):
    U = np.stack([np.sin(th) * np.cos(e), -np.cos(th) * np.cos(e), np.sin(e)], axis=1)
    return HC + boxy(2.1)(U) * HCR * lift
def groove(name, th, e):
    pts = on_dome(np.array(th, float), np.array(e, float))
    k.add(name, tube(pts, [0.0028] * len(pts), seg=8, per=6), GROOVE, "head", fuse=False)
groove("part", [0.22, 0.24, 0.2, 0.08], [-0.02, 0.4, 0.8, 1.15])
for s in (1, -1):
    side = "R" if s > 0 else "L"
    groove(f"line{side}", [s * 1.0, s * 0.98, s * 0.88, s * 0.7], [-0.2, 0.3, 0.72, 1.05])

# The bob's sides: past the cheeks to the jaw, rounder below, their ends curling in under the face
def side(V):
    V = boxy(2.4)(V)
    V = V.copy()
    V[:, 1] *= 1 + 0.15 * np.clip(-V[:, 2], 0, 1)
    return V
k.add("bob", ell((0.094, 0.008, 0.884), (0.032, 0.09, 0.088), rot=(0, -6, 0), deform=side), GREEN, "head",
      fuse=False, mirror=True)
k.add("curl", tube([(0.1, -0.03, 0.81), (0.09, -0.066, 0.802), (0.072, -0.08, 0.816)], [0.022, 0.015, 0.006]),
      GREEN, "head", fuse=False, mirror=True)
# the fringe swept down over the outer edge of the eye on +X
k.add("fringe", blade([(0.03, -0.095, 0.955), (0.072, -0.102, 0.91), (0.094, -0.08, 0.86), (0.1, -0.058, 0.83)],
                      [0.024, 0.02, 0.013, 0.004], (0.55, -1, 0.1), 0.22), GREEN, "head", fuse=False)

# A small calm mouth low on the face
INK, LID, IRIS = srgb(INKH), srgb(WHITE), srgb(RED)
MOUTH = srgb("#B8506A")
def mouth(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(v - (-0.1 + 0.4 * u * u)) < 0.28) & (np.abs(u) < 0.75)] = MOUTH
    return c
fm = unit(0, -1, -0.55)
k.decal(H + fm * 0.062, fm, (0.011, 0.006), mouth, eps=0.003, rings=8, segs=32)

# The eyes: large and gentle, as Kirlia's, a little taller
DEEP, OUTL = srgb("#9A2238"), srgb("#3A1A22")
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    o = (u / 0.95) ** 2 + ((v + 0.02) / 0.92) ** 2
    ir = ((u + 0.04) / 0.62) ** 2 + ((v + 0.06) / 0.78) ** 2
    c[(o < 1) & (ir < 1)] = DEEP
    c[(o < 1) & (ir < 0.72)] = IRIS
    c[((u + 0.04) / 0.24) ** 2 + ((v + 0.04) / 0.36) ** 2 < 1] = INK
    c[((u - 0.14) / 0.17) ** 2 + ((v - 0.32) / 0.19) ** 2 < 1] = (1, 1, 1)
    c[((u + 0.24) / 0.08) ** 2 + ((v + 0.36) / 0.08) ** 2 < 1] = (1, 1, 1)
    c[(o < 1) & (o > 0.9)] = OUTL
    c[(o < 1) & (o > 0.64) & (v > 0.36)] = OUTL
    return c
de = unit(0.45, -0.88, 0.05)
def eyes(state):
    k.decal(H + de * 0.06, de, (0.028, 0.032), eye_look(state, eye, LID, INK, line=0.15), eps=0.003, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
