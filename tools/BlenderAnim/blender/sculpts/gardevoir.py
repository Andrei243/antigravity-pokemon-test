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
from sculptkit import Kit, ell, tube, blade, sheet, boxy, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, GREEN, GROOVE, HORN, RED, INKH = "#F4F5FA", "#7FCB72", "#2F5C2C", "#E26878", "#D84A62", "#2A1418"
E = 0.0042                                # a crisp paint's edge
H = np.array([0, -0.028, 0.885])          # the face's middle
HR = np.array([0.064, 0.06, 0.07])
HC = np.array([0, 0.006, 0.927])          # the helmet of hair's middle
HCR = np.array([0.128, 0.118, 0.11])


# The gown: a cloth of its own (a loose sheet) from under the chest to the ground, slim at the waist and flaring in
# four broad folds, sweeping out behind, and open down the front a little to +X, its two edges standing apart over
# the green inside
SLIT = 0.2                                # the opening's middle, in radians from the front toward +X
def gown_at(u, v):
    t = v                                                  # 0 at the waist, 1 at the hem
    z = 0.745 - 0.739 * t
    g = 0.04 + 0.22 * t ** 1.2                             # half the opening
    phi = SLIT + g + u * (2 * math.pi - 2 * g)            # from one front edge round the back to the other
    r = 0.034 + 0.2 * t ** 1.4
    folds = 1 + 0.15 * t ** 1.3 * np.cos(4 * (phi - SLIT) + 0.5)
    back = np.clip(-np.cos(phi), 0, 1) ** 1.5
    edge = 1 + 0.08 * t * (np.exp(-(u / 0.04) ** 2) + np.exp(-((1 - u) / 0.04) ** 2))
    rr = r * folds * edge * (1 + 0.38 * t ** 2 * back)
    x = rr * np.sin(phi)
    y = -rr * np.cos(phi) + 0.05 * t ** 2
    return np.stack([x, y, z], axis=-1)
k.add("gown", sheet(gown_at, 72, 44, 0.012), WHITE, "body", fuse=False)
# the green inside, seen through the opening
def cone(V):
    V = V.copy()
    V[:, 2] = np.maximum(V[:, 2], -0.97)
    V[:, :2] *= (0.22 + 0.78 * np.clip((1 - V[:, 2]) / 2, 0, 1) ** 1.3)[:, None]
    return V
k.add("lining", ell((0.012, 0.004, 0.365), (0.115, 0.11, 0.36), deform=cone), GREEN, "body", fuse=False)

# A thin white chest and neck
k.add("chest", ell((0, 0.0, 0.748), (0.046, 0.038, 0.066)), WHITE, "body")
k.add("neck", tube([(0, 0.0, 0.79), (0, -0.01, 0.85)], [0.018, 0.016]), WHITE, "head")

# The red horn through the chest: a flat fin in the front-to-back plane, thin seen from the front, a rounded
# triangle pointing forward and a little up from the side, its twin behind (loose, crisp)
for nm, s in (("hornF", -1), ("hornB", 1)):
    k.add(nm, blade([(0, s * 0.02, 0.742), (0, s * 0.07, 0.762), (0, s * 0.118, 0.786), (0, s * 0.142, 0.8)],
                    [0.046, 0.034, 0.018, 0.004], (1, 0, 0), 0.2), HORN, "body", fuse=False)

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
# The fringe: a flat lock lying on the helmet from its crown and sweeping down in front of the face over the eye on
# +X to the jaw, as both pictures have it (the eye on that side hidden)
FTH = [-0.12, 0.06, 0.24, 0.32, 0.5]       # its middle's angle round the head, from the front toward +X
FE = [1.15, 0.65, 0.12, -0.38, -0.8]       # its height, as an angle over the head's middle
FW = [0.12, 0.22, 0.29, 0.3, 0.05]         # its half width, in radians
def fringe_at(u, v):
    s = 0.015 + 0.97 * u
    knots = np.linspace(0, 1, len(FTH))
    th = np.interp(s, knots, FTH) + (v - 0.5) * 2 * np.interp(s, knots, FW)
    e = np.interp(s, knots, FE)
    lift = 1.003 + 0.05 * smoothstep(0.15, 0.6, s)
    U = np.stack([np.sin(th) * np.cos(e), -np.cos(th) * np.cos(e), np.sin(e)], axis=-1)
    sh = U.shape
    return HC + boxy(2.1)(U.reshape(-1, 3)).reshape(sh) * HCR * lift[..., None]
k.add("fringe", sheet(fringe_at, 40, 16, 0.009), GREEN, "head", fuse=False)

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
    # no ring: a soft dark line over the upper lid alone, a little heavier at the outer corner
    c[(o < 1) & (o > 0.8 - 0.1 * np.clip(u, 0, 1)) & (v > 0.25 - 0.15 * u)] = OUTL
    return c
de = unit(0.45, -0.88, 0.05)
def eyes(state):
    k.decal(H + de * 0.06, de, (0.028, 0.032), eye_look(state, eye, LID, INK, line=0.15), eps=0.003, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
