# Gardevoir sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gardevoir.py -- <the export's Gardevoir.glb> <out.glb>
# Grown from kirlia.py: the same white, green, horn red and eye red, the same loose hair with dark grooves and the
# same large gentle eyes.
#
# The design, from the references: tall, slender and elegant; a big round green bob of hair, wider than the
# shoulders, opening on a white face whose side runs out into a white point over the hair; one large red eye seen,
# the other half under a lock hanging down the face's other side; the bob's sides falling straight and flicking out
# at their ends; a thin white neck; a pink-red fin through the chest, front and back, rounded like a heart's lobe from
# the side and a thin plate from the front; long slender green arms held out and down, three thin fingers each, their
# green running onto the chest under the fin; a white gown fitted on the slim body and flowing to the ground in deep
# folds, open down the front over its green lining and two slim white legs, with a long train sweeping out behind and
# to one side over the ground, its hem in soft points.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, GREEN, GROOVE, HORN, RED, INKH = "#F4F5FA", "#7FCB72", "#3F7A3A", "#E26878", "#D84A62", "#2A1418"
H = np.array([0, -0.03, 0.905])           # the face's middle
HC = np.array([0, 0.006, 0.94])          # the bob of hair's middle
HCR = np.array([0.15, 0.136, 0.122])


# The gown: a cloth of its own (a loose sheet) from the waist to the ground, fitted above and flowing below in deep
# folds, open down the front a little to +X over its green lining, a long train sweeping out behind toward -X and
# lying on the ground, the hem in soft points where the folds come down
SLIT = 0.24                               # the opening's middle, in radians from the front toward +X
TRAIN = math.pi + 0.8                     # the train's way, behind and toward -X
def fold(phi):
    return 0.6 * np.cos(7 * phi + 0.4) + 0.4 * np.cos(11 * phi + 1.7)
def gown_at(u, v, inset=0.0, lining=False):
    t = v                                                  # 0 at the waist, 1 at the hem
    g = 0.035 + 0.3 * t ** 1.3                             # half the opening
    phi = SLIT + g + u * (2 * math.pi - 2 * g)            # from one front edge round the back to the other
    f = fold(phi)
    r = 0.036 + 0.21 * t ** 1.5
    d = (phi - TRAIN + math.pi) % (2 * math.pi) - math.pi
    w = np.exp(-(d / 0.9) ** 2)                            # how much of the train a place is
    rr = r * (1 + 0.34 * t ** 1.1 * f)
    z = 0.75 - 0.744 * t - 0.05 * t ** 4 * f
    if lining:
        # inside alone, and short of the train lying on the ground, which it would show through
        rr = rr * (1 - 0.4 * w * t ** 2) - inset
        z = np.maximum(z, 0.006)
    else:
        rr = rr * (1 + 1.25 * t ** 2.2 * w)
        z = z - w * 0.3 * smoothstep(0.4, 1.0, t)
        z = np.maximum(z, 0.006) + w * (0.03 * smoothstep(0.9, 1.0, t) + 0.035 * smoothstep(0.55, 1.0, t) * np.clip(f + 0.2, 0, 1))
    x = rr * np.sin(phi)
    y = -rr * np.cos(phi) + 0.03 * t ** 2
    return np.stack([x, y, z], axis=-1)
k.add("gown", sheet(gown_at, 150, 56, 0.01), WHITE, "body", fuse=False)
k.add("lining", sheet(lambda u, v: gown_at(u, v, 0.011 * smoothstep(0.0, 0.15, v) + 0.002, True), 90, 36, 0.006),
      GREEN, "body", fuse=False)

# Two slim white legs, seen through the opening, drawn to points at the ground
k.add("leg", tube([(0.028, 0.0, 0.66), (0.032, -0.006, 0.42), (0.03, -0.012, 0.16), (0.024, -0.02, 0.008)],
                  [0.024, 0.02, 0.016, 0.009]), WHITE, "body", mirror=True)

# A slim white body and a thin neck
k.add("waist", ell((0, 0.0, 0.7), (0.036, 0.032, 0.07)), WHITE, "body")
k.add("chest", ell((0, 0.0, 0.762), (0.046, 0.038, 0.05)), WHITE, "body")
k.add("neck", tube([(0, 0.0, 0.79), (0, -0.008, 0.88)], [0.016, 0.013]), WHITE, "head")
# the arms' green runs onto the chest in a curve meeting under the fin
k.paint(lambda P: inside(0.012 + 0.55 * np.clip(P[:, 2] - 0.73, 0, 1) - np.abs(P[:, 0]), 0.003)
        * inside(P[:, 2] - 0.805, 0.003) * inside(0.728 - P[:, 2], 0.003), GREEN)

# The red fin through the chest: a thin plate in the front-to-back plane, rounded like a heart's lobe from the side,
# its point forward and a little up; its twin behind (loose, crisp)
def lobe(V):
    V = V.copy()
    V[:, 2] *= 1 - 0.6 * smoothstep(-0.3, 1.0, V[:, 1])   # narrowing to its point at +y (outward)
    V[:, 2] += 0.25 * smoothstep(0.2, 1.0, V[:, 1])        # the point raised
    return V
k.add("hornF", ell((0, -0.068, 0.77), (0.009, 0.062, 0.042), rot=(0, 0, 180), deform=lobe), HORN, "body",
      fuse=False)
k.add("hornB", ell((0, 0.068, 0.77), (0.009, 0.062, 0.042), deform=lobe), HORN, "body", fuse=False)

# Long slender green arms held out and down, three thin fingers at each hand
k.add("arm", tube([(0.04, 0.0, 0.78), (0.085, -0.012, 0.735), (0.135, -0.03, 0.655), (0.172, -0.045, 0.585)],
                  [0.022, 0.016, 0.014, 0.012]), GREEN, "arm{s}", mirror=True)
k.add("hand", ell((0.18, -0.048, 0.57), (0.015, 0.012, 0.02), rot=(0, -25, 0)), GREEN, "arm{s}", fuse=False,
      mirror=True)
for i, (dx, dy) in enumerate(((0.0, -0.012), (0.013, 0.0), (-0.01, 0.007))):
    k.add(f"finger{i}", tube([(0.183 + dx * 0.4, -0.048 + dy * 0.4, 0.562), (0.198 + dx, -0.054 + dy, 0.518)],
                             [0.0065, 0.0025]), GREEN, "arm{s}", fuse=False, mirror=True)

# The face: a white head standing forward into the hair's opening
k.add("head", ell((0, -0.026, 0.905), (0.072, 0.064, 0.072)), WHITE, "head")
k.add("face", ell((0, -0.046, 0.897), (0.062, 0.05, 0.062)), WHITE, "head")
# the white point running out from the face's -X side over the hair, up and out
k.add("cheek", blade([(-0.058, -0.084, 0.862), (-0.09, -0.088, 0.872), (-0.12, -0.08, 0.884), (-0.148, -0.064, 0.896)],
                     [0.02, 0.014, 0.007, 0.0015], (0.25, -1, 0), 0.3), WHITE, "head", fuse=False)


# The bob: a round mass, straight down at its sides and flicking out at their ends, opened at the front on the face,
# more of it on -X; a lock hangs down over the +X side of the face
def bob(U):
    x, y, z = U[:, 0].copy(), U[:, 1].copy(), U[:, 2].copy()
    rh = np.sqrt(x * x + y * y) + 1e-9
    zc = np.clip(-z, 0, 1)
    want = np.where(z < 0, (1 - zc ** 4) ** 0.25, rh) * (1 + 0.22 * smoothstep(0.5, 0.88, zc))
    s = np.where(z < 0, want / rh, 1)
    s = np.where(rh < 0.05, 1, s)
    x, y = x * s, y * s
    z = np.maximum(z, -0.88)
    # the opening on the face: an arch, its peak over the -X eye, from x -0.7 to x 0.1
    top = 0.18 - 0.6 * ((U[:, 0] + 0.18) / 0.6) ** 2
    m = (smoothstep(-0.86, -0.6, U[:, 0]) * (1 - smoothstep(0.02, 0.18, U[:, 0]))
         * (1 - smoothstep(top - 0.12, top + 0.02, U[:, 2])) * smoothstep(0.05, 0.4, -U[:, 1]))
    y = y + m * (0.5 * (y - 0.45 + np.sqrt((y + 0.45) ** 2 + 0.03)) - y)      # a soft floor at -0.45
    return np.stack([x, y, z], axis=1)
k.add("hair", ell(HC, HCR, deform=bob, sub=5), GREEN, "head", fuse=False)


def on_bob(th, e, lift=1.01):
    U = np.stack([np.sin(th) * np.cos(e), -np.cos(th) * np.cos(e), np.sin(e)], axis=1)
    return HC + bob(U) * HCR * lift
def groove(name, th, e):
    pts = on_bob(np.array(th, float), np.array(e, float))
    k.add(name, tube(pts, [0.0022] * len(pts), seg=8, per=6), GROOVE, "head", fuse=False)
groove("part", [0.0, 0.2, 0.28, 0.3], [1.25, 0.85, 0.45, 0.1])          # the lock's edge, from the crown
groove("lineR", [0.75, 0.95, 1.12, 1.25], [1.1, 0.7, 0.3, -0.1])
groove("lineL", [-0.75, -0.95, -1.12, -1.25], [1.1, 0.7, 0.3, -0.1])

# A small calm mouth low on the face
INK, LID, IRIS = srgb(INKH), srgb(WHITE), srgb(RED)
MOUTH = srgb("#B8506A")
def mouth(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(v - (-0.1 + 0.4 * u * u)) < 0.28) & (np.abs(u) < 0.75)] = MOUTH
    return c
fm = unit(-0.12, -1, -0.6)
k.decal(H + fm * 0.07, fm, (0.009, 0.005), mouth, eps=0.003, rings=8, segs=32)

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
de = unit(0.42, -0.9, 0.08)
def eyes(state):
    k.decal(H + de * 0.07, de, (0.025, 0.03), eye_look(state, eye, LID, INK, line=0.15), eps=0.003, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
