# Monferno sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/monferno.py -- <the export's Monferno.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

ORANGE, TAN, BLUE, NAVY, RED, GOLD = "#EC8530", "#EED6AC", "#2F80CC", "#1C3F78", "#E2452C", "#F4C43C"
FLAME, CORE, WHITE = "#EF5A2B", "#FFD43F", "#FAF6EE"
H = np.array([0, -0.01, 0.85])

# The body: slim, a tan belly over it
k.add("torso", ell((0, 0.005, 0.5), (0.115, 0.1, 0.175)), ORANGE, "body")
k.add("head", ell(H, (0.22, 0.2, 0.2)), ORANGE, "head")
k.add("muzzle", ell((0, -0.175, 0.775), (0.085, 0.06, 0.055)), TAN, "head")
# The white tuft under the chin, ragged at its lower edge
def ruff(V):
    V = V.copy()
    ph = np.arctan2(V[:, 0], -V[:, 1])
    low = np.clip(-V[:, 2], 0, 1)
    V[:, 2] -= 1.6 * low * (0.5 + 0.5 * tri(ph * 7 / (2 * math.pi))) * np.clip(-V[:, 1] + 0.2, 0, 1)
    return V
k.add("ruff", ell((0, -0.105, 0.665), (0.095, 0.06, 0.04), deform=ruff), WHITE, "body")
# Round ears, tan within
k.add("ear", ell((0.245, 0.005, 0.865), (0.088, 0.032, 0.094)), ORANGE, "ear{s}", mirror=True)
# The pointed tuft on the crown, curling back
k.add("tuft", tube([(0, 0.0, 0.98), (0, 0.035, 1.07), (0, 0.09, 1.12), (0, 0.15, 1.14)],
                   [0.06, 0.04, 0.018, 0.004]), ORANGE, "head")
# Thin arms, a golden band on each forearm, tan hands with long fingers
k.add("arm", tube([(0.09, 0, 0.6), (0.17, 0, 0.53), (0.215, -0.01, 0.43), (0.245, -0.03, 0.32)],
                  [0.034, 0.03, 0.028, 0.027]), ORANGE, "arm{s}", mirror=True)
k.add("hand", ell((0.252, -0.035, 0.275), (0.045, 0.04, 0.05)), TAN, "arm{s}", mirror=True)
for i, (x, y) in enumerate([(0.228, -0.058), (0.252, -0.064), (0.276, -0.055)]):
    k.add(f"finger{i}", tube([(x, y, 0.255), (x + 0.004 * (i - 1), y - 0.008, 0.19)], [0.014, 0.012]), TAN, "arm{s}",
          mirror=True)
k.add("thumb", tube([(0.222, -0.045, 0.29), (0.205, -0.07, 0.255)], [0.013, 0.011]), TAN, "arm{s}", mirror=True)
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.4) - 0.022), GOLD, on=["arm"])
# Legs: orange thighs, tan shins and big tan feet
k.add("leg", tube([(0.075, 0, 0.38), (0.095, -0.01, 0.24), (0.105, 0, 0.08)], [0.05, 0.04, 0.036]), ORANGE, "leg{s}",
      mirror=True)
def toes(V):
    ph = np.arctan2(V[:, 0], -V[:, 1])
    s = 1 + 0.16 * np.cos(4 * ph) * np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("foot", ell((0.11, -0.06, 0.036), (0.062, 0.115, 0.036), deform=toes), TAN, "leg{s}", mirror=True)
k.paint(lambda P: smoothstep(0.235, 0.205, P[:, 2]), TAN, on=["leg"])

# The long thin tail, curling up behind, its flame at the tip
k.add("tail", tube([(0, 0.08, 0.38), (0, 0.2, 0.3), (0, 0.33, 0.31), (0, 0.42, 0.42), (0, 0.43, 0.56), (0, 0.42, 0.68)],
                   [0.03, 0.027, 0.025, 0.024, 0.024, 0.022]), ORANGE, "tail")
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.6) - 0.02) * (P[:, 1] > 0.3), GOLD, on=["tail"])
def flame(V):
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    ph = np.arctan2(V[:, 1], V[:, 0])
    V[:, 0] *= 1 - 0.85 * t ** 1.4; V[:, 1] *= 1 - 0.85 * t ** 1.4
    V[:, 0] += 0.25 * np.sin(5 * t) * t
    return V * (1 + 0.08 * tri(ph * 3 / (2 * math.pi)))[:, None]
k.add("flame", ell((0, 0.42, 0.83), (0.06, 0.05, 0.17), deform=flame), "#E43A26", "flame", fuse=False, glow="#FF4A22")
k.add("flame2", ell((0, 0.42, 0.79), (0.045, 0.04, 0.11), rot=(0, -28, 0), deform=flame), FLAME, "flame", fuse=False,
      glow="#FF6A2A")
k.add("flame3", ell((-0.01, 0.42, 0.78), (0.04, 0.036, 0.09), rot=(0, 30, 0), deform=flame), FLAME, "flame", fuse=False,
      glow="#FF6A2A")
k.add("flamecore", ell((0, 0.42, 0.715), (0.05, 0.05, 0.075), deform=flame), CORE, "flame", fuse=False, glow="#FFE070")

# The face: orange with a blue mask across the eyes, outlined dark, a red ridge down to the nose and a tan muzzle
CO, CT, CB, CN, CR = srgb(ORANGE), srgb(TAN), srgb(BLUE), srgb(NAVY), srgb(RED)
def face(u, v):
    au = np.abs(u)
    c = np.tile(CO, (len(u), 1))
    c[(u / 0.8) ** 2 + ((v + 0.52) / 0.6) ** 2 < 1] = CT
    top = 0.4 + 0.07 * np.cos((au - 0.5) * 6.0)          # the brow rises over each eye
    f = ((au - 0.5) / 0.44) ** 2 + ((v - 0.1) / top) ** 2
    bridge = (au < 0.5) & (v > -0.02) & (v < 0.32)
    c[(f < 1.16) | ((au < 0.52) & (v > -0.06) & (v < 0.36))] = CN
    c[(f < 1) | bridge] = CB
    nose = (v < 0.1) & (v > -0.46) & (au < 0.21 * (v + 0.46) / 0.56 + 0.03)
    c[nose] = CR
    return c
k.decal((0, -0.22, 0.85), (0, -1, 0), (0.185, 0.19), face, eps=0.003, rings=44, segs=144)
k.decal((0.25, -0.03, 0.865), (0, -1, 0), (0.06, 0.066), TAN, eps=0.003, rings=8, segs=28, mirror=True)
# The open mouth with a fang
MOUTH, TONGUE = srgb("#8A2A22"), srgb("#E9705A")
def mouth(u, v):
    c = np.tile(MOUTH, (len(u), 1))
    c[v < -0.35] = TONGUE
    c[(u > 0.2) & (u < 0.5) & (v > 0.25 - 0.9 * (u - 0.2)) & (v > 0.1)] = (1, 1, 1)
    return c
k.decal((0, -0.24, 0.745), unit(0, -1, -0.25), (0.045, 0.028), mouth, eps=0.004, rings=6, segs=28,
        outline=lambda th: 1.0 if math.sin(th) < 0 else 0.35)
# The belly
k.decal((0, -0.1, 0.48), (0, -1, 0), (0.078, 0.12), TAN, eps=0.003, rings=12, segs=40)

# The eyes, under the mask's brow, looking out fiercely
de = unit(0.45, -0.89, 0.12)
E = H + de * 0.205
DARK = srgb("#1E1410")
def eye(u, v):
    c = np.tile(np.array([1.0, 1.0, 1.0]), (len(u), 1))
    c[((u + 0.25) / 0.45) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = DARK
    c[((u + 0.05) / 0.16) ** 2 + ((v - 0.25) / 0.16) ** 2 < 1] = (1, 1, 1)
    c[v > 0.55 + 0.35 * u] = CB          # the mask's brow over the eye's inner corner
    return c
def eyes(state):
    k.decal(E, de, (0.038, 0.05), eye_look(state, eye, CB, DARK), eps=0.009, rings=20, segs=56, mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
