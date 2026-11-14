# Gallade sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gallade.py -- <the export's Gallade.glb> <out.glb>
# Grown from kirlia.py and gardevoir.py: the same white, horn red and eye red, the same large red eyes, its green a
# deeper one; Kirlia's other evolution, a knight where Gardevoir is a dancer.
#
# The design, from the references: a white and green knight; a rounded green helmet over the head, open at the front
# on a white face, a pale blue crest fin standing up along its top from front to back; a short white point out of
# each side of the face; large red eyes; a green neck and a green chest narrowing to the waist between white sides; a
# red horn through the chest, front and back, a thin plate from the front; long green arms whose forearms are broad
# flat blades running out far past the elbows; a wide round white cushion at the hips; long white legs set wide apart,
# thin at the shins and flaring at the ground into pointed feet lined in grey.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, boxy, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, GREEN, HORN, RED, INKH, CREST, GROOVE = "#F4F5FA", "#3BAA6E", "#E04A64", "#D84A62", "#2A1418", "#7DBBD8", "#A2A8B4"
H = np.array([0, -0.05, 0.875])           # the face's middle
HC = np.array([0, 0.004, 0.9])            # the helmet's middle
HCR = (0.132, 0.13, 0.142)
FR = (0.1, 0.085, 0.108)                  # the face's radii

# The hips: a wide deep round white cushion, covering the tops of the legs
k.add("cushion", ell((0, 0.0, 0.49), (0.2, 0.15, 0.072), deform=boxy(2.4)), WHITE, "body")

# Long white legs set wide apart, thicker toward the ground, flaring into broad pointed feet
k.add("thigh", tube([(0.08, 0.0, 0.47), (0.17, 0.0, 0.32), (0.25, -0.006, 0.19)], [0.048, 0.04, 0.036]), WHITE,
      "leg{s}", mirror=True)
FX, FY = 0.33, -0.025                      # the foot's middle on the ground
k.add("shin", tube([(0.25, -0.006, 0.19), (0.29, -0.012, 0.12), (0.315, -0.018, 0.08), (FX, FY, 0.045)],
                   [0.036, 0.04, 0.052, 0.066]), WHITE, "leg{s}", mirror=True)
# the foot's flare runs out to a point at the front, a little outward
k.add("toe", ell((FX + 0.016, FY - 0.05, 0.026), (0.064, 0.09, 0.026), rot=(0, 0, -15)), WHITE, "leg{s}",
      mirror=True)
# a few grey lines down each foot's front, painted round the foot's own axis
def grooves(P):
    x = np.abs(P[:, 0]) - FX; y = P[:, 1] - FY
    r = np.hypot(x, y); a = np.arctan2(x, -y)
    m = np.zeros(len(P))
    for ai in (-0.45, 0.0, 0.45):
        m = np.maximum(m, inside(r * np.abs(a - ai) - 0.0038, 0.002))
    return m * (r > 0.04) * inside(P[:, 2] - 0.1 + 0.25 * np.abs(a) * 0.1, 0.006) * (P[:, 2] > 0.008)
k.paint(grooves, GROOVE)

# A slim body: a white waist and chest, its front and back green narrowing down to the waist; a green neck
k.add("waist", ell((0, 0.0, 0.575), (0.044, 0.038, 0.06)), WHITE, "body")
k.add("chest", ell((0, 0.0, 0.68), (0.074, 0.05, 0.072)), WHITE, "body")
k.add("neck", tube([(0, 0.0, 0.72), (0, -0.01, 0.8)], [0.03, 0.026]), GREEN, "head")
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.01 - 0.48 * np.clip(P[:, 2] - 0.56, 0, 1), 0.003)
        * inside(0.55 - P[:, 2], 0.003) * inside(P[:, 2] - 0.8, 0.003), GREEN)

# The red horn through the chest: a thin plate in the front-to-back plane, pointed forward and a little up, its twin
# behind (loose, crisp); smaller than Gardevoir's
def point(V):
    V = V.copy()
    V[:, 2] *= 1 - 0.7 * smoothstep(-0.4, 1.0, V[:, 1])   # narrowing to its point at +y (outward)
    V[:, 2] += 0.3 * smoothstep(0.2, 1.0, V[:, 1])         # the point raised
    return V
k.add("hornF", ell((0, -0.078, 0.69), (0.012, 0.056, 0.04), rot=(0, 0, 180), deform=point), HORN, "body", fuse=False)
k.add("hornB", ell((0, 0.078, 0.69), (0.012, 0.056, 0.04), deform=point), HORN, "body", fuse=False)

# Green arms held a little out and down from the shoulders; the forearms forward to the hands, each a broad flat
# blade along it, from the hand back past the elbow to a point behind, its faces turned outward (a tonfa)
EL = (0.185, 0.0, 0.62)                    # the elbow
HA = (0.215, -0.13, 0.565)                 # the hand
k.add("arm", tube([(0.06, 0.0, 0.73), (0.13, -0.002, 0.68), EL], [0.028, 0.024, 0.022]), GREEN,
      "arm{s}", mirror=True)
k.add("fore", tube([EL, HA], [0.022, 0.02]), GREEN, "arm{s}", mirror=True)
BL = [(0.214, -0.12, 0.57), (0.2, -0.05, 0.6), (0.19, 0.03, 0.62), (0.235, 0.12, 0.635), (0.29, 0.2, 0.64)]
k.add("blade", blade(BL, [0.03, 0.058, 0.064, 0.042, 0.004], unit(1, 0, -0.15), 0.18), GREEN, "arm{s}",
      fuse=False, mirror=True)
k.add("hand", ell((0.218, -0.15, 0.562), (0.024, 0.026, 0.022)), GREEN, "arm{s}", fuse=False, mirror=True)
for i, (dx, dz) in enumerate(((-0.012, 0.008), (0.0, -0.004), (0.012, 0.008))):
    k.add(f"finger{i}", tube([(0.218 + dx * 0.5, -0.168, 0.56 + dz * 0.5), (0.218 + dx, -0.196, 0.552 + dz)],
                             [0.009, 0.006]), GREEN, "arm{s}", fuse=False, mirror=True)

# The head: a large rounded green helmet, open at the front on a white face standing forward into it
k.add("helmet", ell(HC, HCR), GREEN, "head")
k.add("face", ell(H, FR), WHITE, "head")
# the helmet's edge over the face: a visor dipping to a point between the eyes, down the cheeks to the jaw
def face_zone(P):
    x, z = P[:, 0], P[:, 2]
    edge = H[2] + 0.058 - 0.04 * np.clip(1 - np.abs(x) / 0.04, 0, 1) - 0.6 * np.clip(np.abs(x) - 0.04, 0, 1)
    return inside(z - edge, 0.003) * inside(P[:, 1] - H[1] - 0.012, 0.003) \
        * inside(np.abs(x) - FR[0] + 0.4 * np.clip(H[2] - 0.01 - z, 0, 0.1), 0.003) * (z > H[2] - 0.12)
k.paint(lambda P: face_zone(P), WHITE)
k.paint(lambda P: (1 - face_zone(P)) * (P[:, 2] > H[2] - 0.07) * (np.hypot(P[:, 0], P[:, 1]) < 0.17), GREEN)

# The crest: a rounded teal fin from the forehead over the top to the back, set half into the helmet, wider and
# lower than a spike, a little width to it so that from the front it is a blade and not a hair
def fin(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 1 - 0.6 * t
    V[:, 1] += 0.12 * t ** 1.5                     # the top leaning a little back
    return V
k.add("crest", ell((0, 0.0, 0.975), (0.03, 0.13, 0.1), rot=(-15, 0, 0), deform=fin), CREST, "head", fuse=False)

# A short white point out of each side of the face at the cheek, out and a little forward: the face's sideburns
k.add("point", blade([(0.075, -0.07, 0.86), (0.11, -0.075, 0.855), (0.14, -0.078, 0.85), (0.165, -0.08, 0.846)],
                     [0.03, 0.022, 0.012, 0.002], (0, 1, 0), 0.3), WHITE, "head", fuse=False, mirror=True)

# The eyes: large and red as Kirlia's, a little keener, the line over the upper lid running flatter
INK, LID, IRIS = srgb(INKH), srgb(WHITE), srgb(RED)
DEEP, OUTL = srgb("#9A2238"), srgb("#3A1A22")
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    o = (u / 0.95) ** 2 + ((v + 0.02) / 0.9) ** 2
    ir = ((u + 0.04) / 0.64) ** 2 + ((v + 0.06) / 0.76) ** 2
    c[(o < 1) & (ir < 1)] = DEEP
    c[(o < 1) & (ir < 0.7)] = IRIS
    c[((u + 0.04) / 0.26) ** 2 + ((v + 0.04) / 0.36) ** 2 < 1] = INK
    c[((u - 0.14) / 0.18) ** 2 + ((v - 0.3) / 0.2) ** 2 < 1] = (1, 1, 1)
    c[(o < 1) & (o > 0.72) & (v > 0.15)] = OUTL
    cut = 0.66 + 0.14 * u                          # a keener top, cut lower toward the inner corner
    c[v > cut] = LID
    c[(v > cut - 0.12) & (v <= cut) & (o < 1.05)] = OUTL
    return c
de = unit(0.42, -0.9, 0.06)
def eyes(state):
    k.decal(H + de * 0.09, de, (0.034, 0.04), eye_look(state, eye, LID, INK, line=0.16), eps=0.003, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
