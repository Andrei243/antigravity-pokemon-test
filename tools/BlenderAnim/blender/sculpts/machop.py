# Machop sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/machop.py -- <the export's Machop.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

SKIN, CREST, INK, RED = "#A8C4CE", "#B8AC9E", "#26303C", "#E0454A"
RIB = "#5E7C96"
H = np.array([0, -0.01, 0.665])          # the head's centre

# A stocky child's body, the head rising out of it on a thick throat
k.add("torso", ell((0, 0.0, 0.42), (0.13, 0.11, 0.15)), SKIN, "body")
k.add("hips", ell((0, 0.01, 0.31), (0.125, 0.105, 0.085)), SKIN, "body")
k.add("head", ell(H, (0.128, 0.12, 0.142)), SKIN, "head")
# A thick throat filling the crease where the head sits on the chest (a sharp crease remeshes rough)
k.add("throat", ell((0, 0.0, 0.53), (0.115, 0.1, 0.095)), SKIN, "head")
# The snout, jutting forward and a little down
k.add("snout", ell((0, -0.1, 0.6), (0.085, 0.075, 0.062)), SKIN, "head")

# The three ridges along its crown, front to back, the middle one tallest
for i, (x, top, lean) in enumerate([(-0.05, 0.065, 8), (0.0, 0.085, 0), (0.05, 0.065, -8)]):
    def fin(V, top=top):
        V = V.copy()
        up = np.clip(V[:, 2], 0, 1)
        V[:, 1] += 0.35 * up * up          # the top swept back
        return V
    k.add(f"ridge{i}", ell((x, 0.01, 0.78), (0.019, 0.085, top), rot=(0, lean, 0), deform=fin), CREST, "head",
          surface="shell")

# Big arms with heavy forearms and round fists, held a little forward
k.add("shoulder", ell((0.14, 0.0, 0.48), (0.06, 0.056, 0.058)), SKIN, "arm{s}", mirror=True)
k.add("arm", tube([(0.13, 0.0, 0.48), (0.2, 0.0, 0.4), (0.23, -0.035, 0.31)], [0.052, 0.05, 0.056]), SKIN,
      "arm{s}", mirror=True)
k.add("fist", ell((0.235, -0.05, 0.255), (0.07, 0.07, 0.072), deform=boxy(2.6)), SKIN, "arm{s}", mirror=True)
k.add("thumb", ell((0.19, -0.095, 0.27), (0.022, 0.026, 0.03)), SKIN, "arm{s}", mirror=True)
for i, x in enumerate((0.205, 0.235, 0.265)):
    k.add(f"knuckle{i}", ell((x, -0.105, 0.245), (0.017, 0.018, 0.034)), SKIN, "arm{s}", mirror=True)

# Thick legs on flat feet
k.add("leg", tube([(0.075, 0.0, 0.31), (0.095, -0.01, 0.15), (0.1, 0.0, 0.06)], [0.068, 0.058, 0.05]), SKIN,
      "leg{s}", mirror=True)
k.add("foot", ell((0.1, -0.04, 0.032), (0.062, 0.09, 0.032)), SKIN, "leg{s}", mirror=True)

# The short stub of a tail, low behind
k.add("tail", tube([(0, 0.085, 0.3), (0, 0.125, 0.295), (0, 0.145, 0.3)], [0.038, 0.026, 0.012]), SKIN, "body")

# The ribs: three short lines each side of the chest, curving down to the middle
CS, CRb = srgb(SKIN), srgb(RIB)
def ribs(u, v):
    c = np.tile(CS, (len(u), 1))
    for j, y in enumerate((0.55, 0.0, -0.55)):
        line = y + 0.35 * u - 0.2 * (1 - u * u) * 0.5
        span = (u > -0.75 + 0.15 * j) & (u < 0.8 - 0.1 * j)
        c[span & (np.abs(v - line) < 0.12)] = CRb
    return c
k.decal((0.07, -0.1, 0.43), unit(0.55, -1, 0.05), (0.05, 0.055), ribs, eps=0.003, rings=24, segs=72, mirror=True)

# The open mouth under the snout: a hollow carved into it, pink within
k.add("mouth", ell((0, -0.17, 0.56), (0.062, 0.03, 0.017), rot=(-20, 0, 0)), "#D9708A", "head", carve=True)

# The eyes: a small red iris in a dark rim, a round dark pupil and a glint; the lid's line slants down toward the
# inner corner for a determined look
WHITE, IRIS, RIM, DARK = np.array([1.0, 1.0, 1.0]), srgb(RED), srgb("#A8242E"), srgb("#1C1418")
def eye(u, v):
    r = (u / 0.9) ** 2 + (v / 0.95) ** 2
    c = np.tile(CS, (len(u), 1))
    c[r < 1] = srgb(INK)
    c[r < 0.72] = RIM
    c[r < 0.5] = IRIS
    c[((u + 0.05) / 0.26) ** 2 + ((v + 0.02) / 0.3) ** 2 < 1] = DARK
    c[((u + 0.2) / 0.12) ** 2 + ((v - 0.24) / 0.12) ** 2 < 1] = WHITE
    brow = 0.7 + 0.2 * u                  # lower at the inner corner (u < 0)
    c[(v > brow) & (r < 1.3)] = CS
    c[(np.abs(v - brow) < 0.13) & (r < 1.15)] = srgb(INK)
    return c
de = unit(0.55, -0.82, 0.05)
E = H + np.array([0, 0, -0.012]) + de * 0.124
def eyes(state):
    k.decal(E, de, (0.03, 0.036), eye_look(state, eye, CS, srgb(INK)), eps=0.007, rings=20, segs=56, mirror=True)

k.build(voxel=0.0065)
k.eye_states(eyes)
k.export(out)
