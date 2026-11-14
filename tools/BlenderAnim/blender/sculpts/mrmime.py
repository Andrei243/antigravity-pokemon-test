# Mr. Mime sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mrmime.py -- "<the export's Mr. Mime.glb>" <out.glb>
# Mime Jr. grown (mimejr.py), in its pink, dark blue, red and white and with its way of making the eyes and the open
# grin: the jester's cap become two wide tufts of dark blue hair sweeping up and out from the head's top, ragged
# beneath; a round white body with a big pink dot on the belly, big pink balls at the shoulders and hips; thin pale
# pink limbs with white knee joints; big white gloves with five spread fingers tipped with pink pads, the right hand
# raised flat as if against glass, the left held out low; dark blue shoes whose toes curl up.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

FACE, LIMB, BLUE, RED, WHITE, BALL = "#F6C9D2", "#F7CBD6", "#245E98", "#E8566A", "#F5F4F2", "#EA6079"
INK, MOUTH, TONGUE = "#1A1418", "#9E2F42", "#E9808C"
H = np.array([0, -0.01, 0.81])           # the head's centre

# The round pink head
k.add("head", ell(H, (0.178, 0.152, 0.145)), FACE, "head")
k.add("jowl", ell((0, -0.04, 0.765), (0.14, 0.11, 0.1)), FACE, "head")

# Two wide tufts of dark blue hair from the head's top, sweeping up and out to points, flat to the front, with a
# ragged lower edge of three teeth each
def tuft(s):
    m = np.array([s, 1, 1.0])
    bone = "head"
    pts = [(0.09, 0.035, 0.885), (0.17, 0.04, 0.96), (0.27, 0.045, 1.015), (0.36, 0.05, 1.045), (0.44, 0.05, 1.04)]
    k.add("tuft" + ("R" if s > 0 else "L"), blade([np.array(p) * m for p in pts], [0.06, 0.075, 0.065, 0.04, 0.004],
                                                  (0, 1, 0), 0.4), BLUE, bone, fuse=False)
    for i, (x, z, ln) in enumerate(((0.23, 0.955, 0.06), (0.31, 0.995, 0.055), (0.38, 1.02, 0.045))):
        a = np.array([x, 0.045, z + 0.02]) * m
        b = np.array([x + 0.035, 0.045, z - ln]) * m
        k.add(f"jag{i}" + ("R" if s > 0 else "L"), blade([a, (a + b) / 2, b], [0.026, 0.016, 0.003], (0, 1, 0), 0.55),
              BLUE, bone, fuse=False)
tuft(1); tuft(-1)

# Red cheek circles low on the sides of the face, clear of the grin's corners
for s in (1, -1):
    k.decal((0.15 * s, -0.08, 0.755), unit(0.88 * s, -0.47, -0.04), (0.04, 0.04), RED, eps=0.003, rings=12, segs=48)

# The open grin, carved, a pink tongue in it
def flat_top(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.3, V[:, 2])     # a straight upper lip over a round lower one
    return V
k.add("mouth", ell((0, -0.152, 0.752), (0.088, 0.04, 0.058), rot=(-10, 0, 0), deform=flat_top), MOUTH, "head",
      carve=True)
k.add("tongue", ell((0, -0.128, 0.712), (0.045, 0.03, 0.014)), TONGUE, "head", fuse=False)

# The round white body, a big pink dot on the belly
k.add("torso", ell((0, 0.0, 0.54), (0.21, 0.165, 0.175)), WHITE, "body")
k.add("neck", ell((0, 0.0, 0.69), (0.08, 0.07, 0.05)), FACE, "body")
k.decal((0, -0.16, 0.5), unit(0, -1, 0.05), (0.095, 0.095), BALL, eps=0.003, rings=14, segs=56)

# Big pink balls at the shoulders and hips
k.add("shoulder", ell((0.21, -0.005, 0.645), (0.1, 0.095, 0.095)), BALL, "arm{s}", mirror=True)
k.add("hip", ell((0.15, -0.01, 0.415), (0.085, 0.08, 0.08)), BALL, "leg{s}", mirror=True)


def glove(wrist, palm, normal, fingers, bone, pads=True):
    """A big white glove: a flat palm facing `normal`, five fingers out to their tips, each tipped with one clean pink
    disc standing proud of the tip's front (none where the hand shows its back)."""
    wrist, palm = np.array(wrist, float), np.array(palm, float)
    n = np.array(normal, float); n /= np.linalg.norm(n)
    k.add("cuff" + bone[-1], tube([wrist, palm], [0.03, 0.045]), WHITE, bone)
    up = palm - wrist; up /= np.linalg.norm(up)
    side = np.cross(n, up); side /= np.linalg.norm(side)
    V, F = ell((0, 0, 0), (0.068, 0.03, 0.066), sub=3)
    V = V @ np.stack([side, n, up], axis=1).T + palm
    k.add("palm" + bone[-1], (V, F), WHITE, bone)
    for i, tip in enumerate(fingers):
        tip = np.array(tip, float)
        base = palm + (tip - palm) * 0.3
        k.add(f"finger{i}" + bone[-1], tube([base, tip], [0.019, 0.015]), WHITE, bone)
        k.add(f"tip{i}" + bone[-1], ell(tip, (0.024, 0.024, 0.024)), WHITE, bone, fuse=False, surface="cloth")
        if pads:
            V, F = ell((0, 0, 0), (0.016, 0.006, 0.016), sub=3)
            V = V @ np.stack([side, n, up], axis=1).T + tip + n * 0.022
            k.add(f"pad{i}" + bone[-1], (V, F), BALL, bone, fuse=False, surface="skin")


# The right arm (the model's -X, on the viewer's left from the front) raised, the glove flat to the front
SH = np.array([-0.22, -0.01, 0.66])
k.add("upperL", tube([SH, (-0.33, -0.02, 0.75), (-0.42, -0.03, 0.82)], [0.026, 0.022, 0.021]), LIMB, "armL")
k.add("elbowL", ell((-0.42, -0.03, 0.82), (0.026, 0.026, 0.026)), LIMB, "armL")
k.add("foreL", tube([(-0.42, -0.03, 0.82), (-0.43, -0.035, 0.9), (-0.43, -0.04, 0.95)], [0.021, 0.02, 0.022]), LIMB, "armL")
P = np.array([-0.43, -0.045, 1.0])
glove((-0.43, -0.04, 0.95), P, (0, -1, 0.05),
      [(-0.52, -0.05, 1.05), (-0.48, -0.05, 1.09), (-0.425, -0.05, 1.105), (-0.37, -0.05, 1.085), (-0.34, -0.05, 1.03)],
      "armL")
# The left arm (+X) held out and down, the glove's fingers spread down and out
SH = np.array([0.22, -0.01, 0.66])
k.add("upperR", tube([SH, (0.33, -0.02, 0.69), (0.42, -0.02, 0.7)], [0.026, 0.022, 0.021]), LIMB, "armR")
k.add("elbowR", ell((0.42, -0.02, 0.7), (0.026, 0.026, 0.026)), LIMB, "armR")
k.add("foreR", tube([(0.42, -0.02, 0.7), (0.44, -0.03, 0.6), (0.45, -0.04, 0.54)], [0.021, 0.02, 0.022]), LIMB, "armR")
glove((0.45, -0.04, 0.54), (0.47, -0.05, 0.49), (0, -1, 0.1),
      [(0.41, -0.06, 0.41), (0.46, -0.06, 0.39), (0.52, -0.06, 0.4), (0.56, -0.06, 0.43), (0.57, -0.06, 0.49)],
      "armR", pads=False)

# Thin pale pink legs bent outward at white knee balls, down to dark blue shoes whose toes curl up
k.add("thigh", tube([(0.17, -0.01, 0.38), (0.24, -0.02, 0.3), (0.28, -0.03, 0.25)], [0.026, 0.023, 0.021]), LIMB,
      "leg{s}", mirror=True)
k.add("knee", ell((0.285, -0.03, 0.245), (0.033, 0.033, 0.033)), WHITE, "leg{s}", fuse=False, mirror=True, surface="cloth")
k.add("shin", tube([(0.285, -0.03, 0.24), (0.3, -0.03, 0.15), (0.31, -0.02, 0.07)], [0.021, 0.02, 0.022]), LIMB,
      "leg{s}", mirror=True)
k.add("shoe", ell((0.32, -0.04, 0.04), (0.058, 0.09, 0.04)), BLUE, "leg{s}", mirror=True)
k.add("toe", tube([(0.33, -0.1, 0.04), (0.345, -0.15, 0.05), (0.36, -0.18, 0.08), (0.355, -0.175, 0.115),
                   (0.34, -0.155, 0.12)], [0.035, 0.026, 0.018, 0.013, 0.011]), BLUE, "leg{s}", mirror=True)

# The eyes: Mime Jr.'s way, grown: white, a big dark pupil with a white glint, wide apart above the cheeks
CP, CI, CW = srgb(FACE), srgb(INK), srgb(WHITE)
def eye(u, v):
    r = u ** 2 + v ** 2
    c = np.tile(CI, (len(u), 1))
    c[r < 0.68] = CW                                                      # a solid dark rim, thick enough to hold
    c[((u + 0.04) / 0.38) ** 2 + ((v + 0.04) / 0.44) ** 2 < 1] = CI       # one solid pupil
    c[((u - 0.1) / 0.15) ** 2 + ((v - 0.16) / 0.15) ** 2 < 1] = (1, 1, 1)  # one glint
    return c
de = unit(0.42, -0.9, 0.12)
E = H + np.array([0, 0, 0.035]) + de * 0.15
def eyes(state):
    k.decal(E, de, (0.038, 0.044), eye_look(state, eye, CP, CI), eps=0.005, rings=20, segs=56, mirror=True)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
