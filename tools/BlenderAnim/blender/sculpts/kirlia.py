# Kirlia sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kirlia.py -- <the export's Kirlia.glb> <out.glb>
# Grown from ralts.py: the same white, green, horn red and eye red, the same loose bowl of hair with dark grooves.
#
# The design, from the references: a small dancer; a big green bowl of hair on a little white face, two long flat
# locks hanging from its sides past the chin and splaying out a little, a lock of the fringe swept down beside one
# eye; two flat red horns standing out of the hair's upper sides like hair clips; large red eyes; thin white arms
# folded across the chest; a short white skirt flared out flat like a tutu in pointed petals; a little green under
# it and two long thin green legs drawn together to points.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, leaf, boxy, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, GREEN, GROOVE, HORN, RED, INKH = "#F4F5FA", "#7FCB72", "#2F5C2C", "#E26878", "#D84A62", "#2A1418"
H = np.array([0, -0.022, 0.567])          # the face's middle
HR = np.array([0.052, 0.05, 0.056])
HC = np.array([0, 0.004, 0.6])            # the bowl of hair's middle
HCR = np.array([0.112, 0.106, 0.104])


# Long thin green legs drawn together to points, under a little green at the hips
k.add("leg", tube([(0.022, 0.0, 0.39), (0.024, -0.004, 0.27), (0.016, -0.006, 0.13), (0.006, -0.008, 0.005)],
                  [0.017, 0.013, 0.0095, 0.004]), GREEN, "leg{s}", mirror=True)
k.add("hips", ell((0, 0.0, 0.392), (0.042, 0.036, 0.03)), GREEN, "body")

# A small white body: the chest under the folded arms, the skirt's core round the waist
k.add("chest", ell((0, 0.002, 0.462), (0.038, 0.032, 0.058)), WHITE, "body")
k.add("core", ell((0, 0.002, 0.412), (0.058, 0.052, 0.022)), WHITE, "body")
k.add("neck", tube([(0, 0.0, 0.5), (0, -0.008, 0.545)], [0.015, 0.014]), WHITE, "head")

# The tutu: pointed white petals out from the waist, flat and drooping a little, two rings a little apart in height
for i in range(7):
    a = 2 * math.pi * (i + 0.5) / 7
    d = (math.sin(a), -math.cos(a), -0.22)
    base = (0.03 * math.sin(a), -0.03 * math.cos(a), 0.418)
    k.add(f"petal{i}", leaf(base, d, 0.15, 0.075, 0.007), WHITE, "body", fuse=False)
for i in range(7):
    a = 2 * math.pi * i / 7
    d = (math.sin(a), -math.cos(a), -0.32)
    base = (0.03 * math.sin(a), -0.03 * math.cos(a), 0.41)
    k.add(f"underpetal{i}", leaf(base, d, 0.13, 0.068, 0.007), WHITE, "body", fuse=False)

# Thin white arms folded across the chest, the hands crossing before it (loose, apart from the body)
for s, side, y in ((1, "R", -0.074), (-1, "L", -0.064)):
    pts = [(0.036 * s, 0.0, 0.5), (0.066 * s, -0.02, 0.468), (0.05 * s, -0.056, 0.46), (-0.012 * s, y, 0.472)]
    k.add(f"arm{side}", tube(pts, [0.011, 0.0095, 0.009, 0.009]), WHITE, f"arm{side}", fuse=False)
    k.add(f"hand{side}", ell((-0.02 * s, y, 0.474), (0.015, 0.011, 0.012), rot=(0, 0, -18 * s)), WHITE, f"arm{side}",
          fuse=False)

# The face: a little white head with a flattened front for the eyes
k.add("head", ell(H, HR), WHITE, "head")
k.add("face", ell(H + np.array([0, -0.012, -0.004]), (0.046, 0.038, 0.048)), WHITE, "head")


# The bowl of hair: a dome cut off below at a rim, just over the eyes in front and low at the back (loose, crisp)
def dome(V):
    V = boxy(2.1)(V)
    th = np.arctan2(V[:, 0], -V[:, 1])                 # 0 at the front
    rim = -0.02 - 0.36 * (1 - np.cos(th))
    V = V.copy()
    V[:, 2] = np.maximum(V[:, 2], rim)
    return V
k.add("hair", ell(HC, HCR, deform=dome, sub=5), GREEN, "head", fuse=False)


def on_dome(th, e, lift=1.012):
    U = np.stack([np.sin(th) * np.cos(e), -np.cos(th) * np.cos(e), np.sin(e)], axis=1)
    return HC + boxy(2.1)(U) * HCR * lift
def groove(name, th, e):
    pts = on_dome(np.array(th, float), np.array(e, float))
    k.add(name, tube(pts, [0.0024] * len(pts), seg=8, per=6), GROOVE, "head", fuse=False)
# the fringe's parting, a little to the +X side, and a line round each side where the locks part from the bowl
groove("part", [0.2, 0.22, 0.18, 0.08], [0.02, 0.4, 0.8, 1.15])
for s in (1, -1):
    side = "R" if s > 0 else "L"
    groove(f"line{side}", [s * 1.15, s * 1.12, s * 1.0, s * 0.8], [-0.1, 0.35, 0.75, 1.05])

# The two long locks: flat panels from the bowl's sides past the chin, splaying out and wider below, cut straight
def lock(V):
    V = boxy(3.2)(V)
    V = V.copy()
    V[:, 1] *= 1 + 0.22 * np.clip(-V[:, 2], 0, 1)
    V[:, 2] = np.maximum(V[:, 2], -0.92)
    return V
k.add("lock", ell((0.104, 0.008, 0.525), (0.02, 0.062, 0.105), rot=(0, -13, 0), deform=lock), GREEN, "head",
      fuse=False, mirror=True)
# the fringe swept down over the outer edge of the eye on +X
k.add("fringe", blade([(0.03, -0.082, 0.632), (0.062, -0.088, 0.596), (0.078, -0.07, 0.56), (0.083, -0.05, 0.535)],
                      [0.02, 0.017, 0.011, 0.003], (0.55, -1, 0.1), 0.3), GREEN, "head", fuse=False)

# The horns: flat red blades out of the hair's upper sides, up and out and a little back, like clips
k.add("horn", blade([(0.07, 0.012, 0.66), (0.104, 0.022, 0.698), (0.134, 0.038, 0.73)], [0.032, 0.023, 0.004],
                    (0.4, -1, 0.15), 0.32), HORN, "head", fuse=False, mirror=True)

# A small calm mouth low on the face
INK, LID, IRIS = srgb(INKH), srgb(WHITE), srgb(RED)
MOUTH = srgb("#B8506A")
def mouth(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(v - (-0.1 + 0.4 * u * u)) < 0.28) & (np.abs(u) < 0.75)] = MOUTH
    return c
fm = unit(0, -1, -0.55)
k.decal(H + fm * 0.052, fm, (0.009, 0.005), mouth, eps=0.003, rings=8, segs=32)

# The eyes: large and gentle, a dark outline heavier on the upper lid, a big red iris, a dark pupil and two glints
DEEP, OUTL = srgb("#9A2238"), srgb("#3A1A22")
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    o = (u / 0.95) ** 2 + ((v + 0.02) / 0.9) ** 2
    ir = ((u + 0.04) / 0.62) ** 2 + ((v + 0.06) / 0.76) ** 2
    c[(o < 1) & (ir < 1)] = DEEP
    c[(o < 1) & (ir < 0.72)] = IRIS
    c[((u + 0.04) / 0.25) ** 2 + ((v + 0.04) / 0.36) ** 2 < 1] = INK
    c[((u - 0.14) / 0.17) ** 2 + ((v - 0.32) / 0.19) ** 2 < 1] = (1, 1, 1)
    c[((u + 0.24) / 0.08) ** 2 + ((v + 0.36) / 0.08) ** 2 < 1] = (1, 1, 1)
    c[(o < 1) & (o > 0.9)] = OUTL
    c[(o < 1) & (o > 0.64) & (v > 0.36)] = OUTL
    return c
de = unit(0.45, -0.88, 0.06)
def eyes(state):
    k.decal(H + de * 0.05, de, (0.02, 0.022), eye_look(state, eye, LID, INK, line=0.15), eps=0.003, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.004)
k.eye_states(eyes)
k.export(out)
