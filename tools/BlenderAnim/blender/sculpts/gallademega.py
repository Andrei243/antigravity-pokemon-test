# Mega Gallade sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gallademega.py -- <the export's Gallade-Mega.glb> <out.glb>
# Grown from gallade.py (which builds as it is read, so its way is copied here): the same white, green, horn red, eye
# red and grey grooves, the same large red eyes, the same hip cushion and long legs flaring into pointed feet.
#
# What the Mega changes, from the references, largest mass first:
#   1. a great pale steel-blue cape hanging from behind the shoulders to the knees, spreading out wide behind and to
#      both sides, far further to +X (where the art hangs it from the outstretched arm), its hem cut into V notches;
#   2. the -X forearm's blade grows into a long curved crescent sword, white with a red outer edge, sweeping up and out
#      from the elbow about half the height long;
#   3. the crest grows into a tall pale blue crescent rising from the forehead and curling back, running down the back
#      of the head;
#   4. the arms and the chest turn white (the green is left on the helmet alone), a red stripe on each upper arm;
#   5. the side points of the face grow longer and rise like ears.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, slab, boxy, srgb, inside, smoothstep, unit, eye_look, tri, args

src, dst = args()
k = Kit(src, coat="fur")

WHITE, GREEN, HORN, RED, INKH, CREST, GROOVE = "#F4F5FA", "#2FA66A", "#E04A64", "#D84A62", "#2A1418", "#8CC8E0", "#A2A8B4"
CAPE = "#5FA6C6"
H = np.array([0, -0.05, 0.875])           # the face's middle
HC = np.array([0, 0.004, 0.9])            # the helmet's middle
HCR = (0.132, 0.13, 0.142)
FR = (0.1, 0.085, 0.108)                  # the face's radii

# The hips: a wide deep round white cushion, covering the tops of the legs
k.add("cushion", ell((0, 0.0, 0.49), (0.2, 0.15, 0.072), deform=boxy(2.4)), WHITE, "body")

# Long white legs set wide apart, thicker toward the ground, flaring into broad pointed feet (as Gallade's)
k.add("thigh", tube([(0.08, 0.0, 0.47), (0.17, 0.0, 0.32), (0.25, -0.006, 0.19)], [0.056, 0.045, 0.038]), WHITE,
      "leg{s}", mirror=True)
FX, FY = 0.33, -0.025
k.add("shin", tube([(0.25, -0.006, 0.19), (0.29, -0.012, 0.12), (0.315, -0.018, 0.08), (FX, FY, 0.045)],
                   [0.037, 0.041, 0.053, 0.066]), WHITE, "leg{s}", mirror=True)
k.add("toe", ell((FX + 0.016, FY - 0.05, 0.026), (0.064, 0.09, 0.026), rot=(0, 0, -15)), WHITE, "leg{s}",
      mirror=True)
def grooves(P):
    x = np.abs(P[:, 0]) - FX; y = P[:, 1] - FY
    r = np.hypot(x, y); a = np.arctan2(x, -y)
    m = np.zeros(len(P))
    for ai in (-0.45, 0.0, 0.45):
        m = np.maximum(m, inside(r * np.abs(a - ai) - 0.0038, 0.002))
    return m * (r > 0.04) * inside(P[:, 2] - 0.1 + 0.25 * np.abs(a) * 0.1, 0.006) * (P[:, 2] > 0.008)
k.paint(grooves, GROOVE)

# A slim white body, all white now, and a white neck
k.add("waist", ell((0, 0.0, 0.575), (0.046, 0.04, 0.06)), WHITE, "body")
k.add("chest", ell((0, 0.0, 0.68), (0.078, 0.052, 0.072)), WHITE, "body")
k.add("neck", tube([(0, 0.0, 0.72), (0, -0.01, 0.8)], [0.03, 0.026]), WHITE, "head")

# The red horn through the chest, front and back, a little smaller than Gallade's
def point(V):
    V = V.copy()
    V[:, 2] *= 1 - 0.7 * smoothstep(-0.4, 1.0, V[:, 1])
    V[:, 2] += 0.3 * smoothstep(0.2, 1.0, V[:, 1])
    return V
k.add("hornF", ell((0, -0.074, 0.69), (0.012, 0.05, 0.038), rot=(0, 0, 180), deform=point), HORN, "body", fuse=False)
k.add("hornB", ell((0, 0.074, 0.69), (0.012, 0.05, 0.038), deform=point), HORN, "body", fuse=False)

# White arms held forward and down from the shoulders, a red stripe down the outside of each upper arm
EL = (0.18, -0.03, 0.63)
HA = (0.2, -0.15, 0.565)
k.add("arm", tube([(0.06, 0.0, 0.73), (0.13, -0.01, 0.685), EL], [0.03, 0.025, 0.023]), WHITE, "arm{s}", mirror=True)
k.add("fore", tube([EL, HA], [0.023, 0.021]), WHITE, "arm{s}", mirror=True)
k.add("stripe", blade([(0.075, 0.006, 0.75), (0.13, 0.002, 0.722), (0.185, -0.018, 0.66)], [0.016, 0.018, 0.012],
                      unit(1, 0, 0.8), 0.5), HORN, "arm{s}", fuse=False, mirror=True)
k.add("hand", ell((0.203, -0.17, 0.562), (0.024, 0.026, 0.022)), WHITE, "arm{s}", fuse=False, mirror=True)
for i, (dx, dz) in enumerate(((-0.012, 0.008), (0.0, -0.004), (0.012, 0.008))):
    k.add(f"finger{i}", tube([(0.203 + dx * 0.5, -0.188, 0.558 + dz * 0.5), (0.203 + dx, -0.214, 0.55 + dz)],
                             [0.009, 0.006]), WHITE, "arm{s}", fuse=False, mirror=True)


# The sword: a long crescent from the -X elbow sweeping up, out and a little forward, its plane turned to show its face
# to the front and to the side; a red crescent with a white one stacked on its inner part, thicker, so that the white
# shows on both faces with the red along the outer edge and in two jagged teeth near the root
SO = np.array([-0.19, -0.05, 0.62])
sd = unit(-0.62, -0.2, 0.76)              # along the sword
sn = unit(-0.32, -1.0, 0.05)              # its face's normal, toward the front and a little -X
sn = sn - sd * (sn @ sd); sn /= np.linalg.norm(sn)
sa = np.cross(sn, sd)                     # across, in the plate's plane
if sa[2] < 0:
    sa = -sa                              # +sa: up and out, the bow's convex side
L = 0.5
def bow(t):
    return math.sin(math.pi * t) ** 0.7
# a broad red crescent, its convex edge toward +sa, its concave edge a gentle curve back toward the body
red = [(0.012 + 0.115 * bow(t), t * L) for t in np.linspace(0, 1, 24)] + \
      [(-0.022 + 0.034 * math.sin(math.pi * t), t * L) for t in np.linspace(1, 0, 24)][1:-1]
k.add("sword", slab(red, SO, sa, sd, 0.03), HORN, "armL", fuse=False)
# a white band along its concave side, thicker, so that it shows on both faces
white = [(0.045 + 0.04 * bow(t), t * L) for t in np.linspace(0.06, 0.9, 20)] + \
        [(-0.016 + 0.034 * math.sin(math.pi * t), t * L) for t in np.linspace(0.9, 0.06, 20)]
k.add("swordW", slab(white, SO, sa, sd, 0.038), WHITE, "armL", fuse=False)
# two red teeth near the root cutting into the white from the red
for j, t in enumerate((0.2, 0.34)):
    b = np.array([0.045 + 0.04 * bow(t), t * L])
    k.add(f"tooth{j}", slab([(0.012, 0.012), (-0.03, -0.03), (0.0, -0.012)], SO + sa * b[0] + sd * b[1], sa, sd,
                            0.044), HORN, "armL", fuse=False)

# The cape: a loose sheet from behind the shoulders to the knees, spreading wide behind and to the sides (further to
# +X), its hem cut in V notches; only behind at the top, so it clears the shoulders and arms
def cape_at(u, v):
    a0 = math.pi / 2 + 0.55 - 0.85 * v           # the +X edge, coming forward as it falls
    a1 = 3 * math.pi / 2 - 0.55 + 0.55 * v       # the -X edge
    phi = a0 + u * (a1 - a0)
    side = np.clip(np.cos(phi - math.pi / 2), 0, 1)       # how far toward +X a place is
    r = 0.085 + v ** 1.15 * (0.22 + 0.3 * side ** 1.5)
    hem = 0.26 - 0.07 * side + 0.035 * (tri(u * 5.0 + 0.5) + 1) * smoothstep(0.85, 1.0, v)
    z = 0.765 - (0.765 - hem) * v
    x = r * np.sin(phi)
    y = -r * np.cos(phi) + 0.035 * (1 - v)
    return np.stack([x, y, z], axis=-1)
k.add("cape", sheet(cape_at, 90, 40, 0.016), CAPE, "body", fuse=False)

# The head: Gallade's helmet, open at the front on a white face
k.add("helmet", ell(HC, HCR), GREEN, "head")
k.add("face", ell(H, FR), WHITE, "head")
def face_zone(P):
    x, z = P[:, 0], P[:, 2]
    edge = H[2] + 0.058 - 0.04 * np.clip(1 - np.abs(x) / 0.04, 0, 1) - 0.6 * np.clip(np.abs(x) - 0.04, 0, 1)
    return inside(z - edge, 0.003) * inside(P[:, 1] - H[1] - 0.012, 0.003) \
        * inside(np.abs(x) - FR[0] + 0.4 * np.clip(H[2] - 0.01 - z, 0, 0.1), 0.003) * (z > H[2] - 0.12)
k.paint(lambda P: face_zone(P), WHITE)
k.paint(lambda P: (1 - face_zone(P)) * (P[:, 2] > H[2] - 0.07) * (np.hypot(P[:, 0], P[:, 1]) < 0.17), GREEN)

# The crest: a tall pale blue crescent from the forehead, rising and curling back, its foot running back over the top
# and down the back of the helmet; a blade with width, so that it is not a hair from the front
CR = [(0, -0.07, 0.99), (0, -0.05, 1.07), (0, 0.0, 1.13), (0, 0.06, 1.16), (0, 0.11, 1.15), (0, 0.14, 1.12)]
k.add("crest", blade(CR, [0.034, 0.05, 0.052, 0.04, 0.022, 0.004], (1, 0, 0), 0.42), CREST, "head", fuse=False)
CB = [(0, -0.02, 1.036), (0, 0.05, 1.03), (0, 0.1, 0.99), (0, 0.128, 0.92), (0, 0.13, 0.85)]
k.add("crestback", blade(CB, [0.026, 0.028, 0.026, 0.02, 0.008], (1, 0, 0), 0.7), CREST, "head", fuse=False)

# Longer white points out of each side of the face, rising out and back like ears
k.add("point", blade([(0.075, -0.06, 0.87), (0.11, -0.055, 0.89), (0.15, -0.045, 0.92), (0.185, -0.035, 0.955)],
                     [0.032, 0.024, 0.013, 0.002], unit(0, 1, -0.3), 0.35), WHITE, "head", fuse=False, mirror=True)

# The eyes: Gallade's own
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
    cut = 0.66 + 0.14 * u
    c[v > cut] = LID
    c[(v > cut - 0.12) & (v <= cut) & (o < 1.05)] = OUTL
    return c
de = unit(0.42, -0.9, 0.06)
def eyes(state):
    k.decal(H + de * 0.09, de, (0.034, 0.04), eye_look(state, eye, LID, INK, line=0.16), eps=0.003, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.0058)
k.eye_states(eyes)
k.export(dst)
