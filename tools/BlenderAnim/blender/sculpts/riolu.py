# Riolu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/riolu.py -- <the export's Riolu.glb> <out.glb>
#
# From the references: a small blue jackal pup standing upright with a big head. A black mask across the eyes and the
# top of the short muzzle, running back on each side to a long black flap hanging down beside the head; pointed blue
# ears; almond eyes, white with a large red iris and a black slit; a thin pale yellow collar; a blue body whose chest
# is black down to a jagged edge, round blue hips over black legs and feet; blue arms with a white round bump on the
# back of each paw; a short broad blue tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, polygon, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, BLACK, WHITE, YELLOW, RED, REDDK, NOSE = ("#3E9BE0", "#2A2D3A", "#F2F4F6", "#E8D27A", "#E2402C", "#9A1E1A",
                                               "#15161C")
H = np.array([0.0, 0.0, 0.645])          # the head's centre
HR = np.array([0.168, 0.152, 0.148])

# --- the head: big and round, a short muzzle, pointed ears --------------------------------------------------------
k.add("head", ell(H, HR), BLUE, "head")
k.add("cheeks", ell((0, -0.03, 0.6), (0.15, 0.12, 0.09)), BLUE, "head")
k.add("snout", ell((0, -0.135, 0.598), (0.066, 0.065, 0.047), rot=(-6, 0, 0)), BLUE, "head")
k.add("ear", blade([(0.085, 0.0, 0.73), (0.112, 0.008, 0.805), (0.142, 0.018, 0.88)], [0.066, 0.042, 0.006],
                   (0, 1, 0), 0.5), BLUE, "ear{s}", mirror=True)
# the roots of the hanging flaps, black, at the sides of the head behind the eyes
k.add("droot", tube([(0.12, 0.02, 0.655), (0.165, 0.03, 0.645)], [0.03, 0.026]), BLACK, "head", mirror=True)
# the flaps themselves: long flat black paddles hanging down and a little out, their faces forward
k.add("drop", ell((0.198, 0.035, 0.555), (0.055, 0.019, 0.105), rot=(0, -14, 24)), BLACK, "head", fuse=False,
      mirror=True)

# --- the body ------------------------------------------------------------------------------------------------------
k.add("throat", ell((0, 0.0, 0.5), (0.085, 0.08, 0.06)), BLACK, "body")
k.add("torso", ell((0, 0.0, 0.405), (0.115, 0.095, 0.11)), BLACK, "body")
k.add("hips", ell((0, 0.008, 0.29), (0.122, 0.1, 0.085)), BLUE, "body")
# the belly blue, its top jagged up into the black chest (two points at the front)
def belly(P):
    a = np.arctan2(P[:, 0], -P[:, 1])
    top = 0.335 + 0.04 * np.clip(1 - np.abs(np.abs(a) - 0.32) / 0.22, 0, 1) * (np.abs(a) < 1.2)
    return inside(P[:, 2] - top, 0.003)
k.paint(belly, BLUE, on=["torso", "hips"])
# the collar: a thin pale ring round the neck, lower at the front
NC = np.array([0, -0.004, 0.5])
ring = [NC + np.array([0.082 * math.sin(a), -0.077 * math.cos(a), -0.012 * math.cos(a)])
        for a in np.linspace(0, 2 * math.pi, 25)]
ring = ring + ring[1:3]
k.add("collar", tube(ring, [0.008] * len(ring), seg=12, per=5), YELLOW, "body", fuse=False, surface="cloth")

# arms: blue, hanging a little forward, a round paw with a white bump on its back
k.add("shoulder", ell((0.11, 0.0, 0.44), (0.05, 0.045, 0.048)), BLUE, "arm{s}", mirror=True)
k.add("arm", tube([(0.12, 0.0, 0.44), (0.17, -0.02, 0.37), (0.2, -0.045, 0.31)], [0.04, 0.034, 0.034]), BLUE,
      "arm{s}", mirror=True)
k.add("paw", ell((0.205, -0.055, 0.282), (0.042, 0.044, 0.044)), BLUE, "arm{s}", mirror=True)
k.add("bump", ell((0.235, -0.04, 0.305), (0.012, 0.026, 0.028), rot=(0, 0, 18)), WHITE, "arm{s}", fuse=False,
      mirror=True, surface="skin")

# legs: black from under the blue hips down to black feet
k.add("leg", tube([(0.075, 0.0, 0.25), (0.085, -0.005, 0.14), (0.09, 0.0, 0.06)], [0.05, 0.042, 0.036]), BLACK,
      "leg{s}", mirror=True)
k.add("foot", ell((0.092, -0.035, 0.03), (0.048, 0.072, 0.03)), BLACK, "leg{s}", mirror=True)
k.add("thigh", ell((0.07, -0.005, 0.27), (0.065, 0.075, 0.06)), BLUE, "leg{s}", mirror=True)

# a crisp line where the blue hips end and the black legs begin
k.paint(lambda P: inside(P[:, 2] - 0.215, 0.003), BLACK)
k.paint(lambda P: inside(0.215 - P[:, 2], 0.003) * inside(P[:, 2] - 0.3, 0.003), BLUE)
# the tail: short, broad and flat, sticking out behind and a little up
k.add("tail", blade([(0, 0.08, 0.27), (0, 0.16, 0.29), (0, 0.23, 0.33), (0, 0.27, 0.37)], [0.035, 0.05, 0.04, 0.008],
                    (1, 0, 0), 0.45), BLUE, "tail")

# --- the mask: black over the eyes and the muzzle's top, back to the flaps' roots, the forehead dipping into it -----
def mask(P):
    q = P - H
    a = np.arctan2(q[:, 0], -q[:, 1])
    s = np.abs(a) * 0.165
    top = np.interp(s, [0.0, 0.025, 0.07, 0.15, 0.3], [0.668, 0.682, 0.712, 0.705, 0.69])
    bot = np.interp(s, [0.0, 0.03, 0.07, 0.15, 0.3], [0.606, 0.612, 0.622, 0.618, 0.615])
    near = (np.linalg.norm(q, axis=1) < 0.25) & (np.abs(a) < 1.95)
    return inside(P[:, 2] - top, 0.003) * inside(bot - P[:, 2], 0.003) * near
k.paint(mask, BLACK, on=["head", "cheeks", "snout", "droot"])
k.paint(lambda P: inside(np.linalg.norm(P - np.array([0, -0.2, 0.612]), axis=1) - 0.018, 0.003), NOSE, on=["snout"])

# --- the eyes: almonds on the mask, white with a big red iris and a black slit, the top line sloping a little in ----
CB, CW, CR, CD, CK = srgb(BLACK), np.array([1.0, 1.0, 1.0]), srgb(RED), srgb(REDDK), srgb(NOSE)
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = ((u + 0.05) / 0.62) ** 2 + ((v + 0.02) / 0.78) ** 2
    c[r < 1] = CD
    c[r < 0.72] = CR
    c[((u + 0.05) / 0.16) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = CK
    c[((u + 0.22) / 0.12) ** 2 + ((v - 0.28) / 0.13) ** 2 < 1] = CW
    return c
# an almond: pointed at both corners, its top line rising a little toward the outer corner
EYE = [(math.cos(t), (0.8 * (0.62 + 0.19 * (math.cos(t) + 1)) if math.sin(t) > 0 else 0.68) * math.sin(t) * (1 - 0.35 * abs(math.cos(t)) ** 3)
        + (0.12 * (math.cos(t) + 1) if math.sin(t) > 0 else 0.0) - 0.05)
       for t in np.linspace(0, 2 * math.pi, 32, endpoint=False)]
LINE = srgb("#8A9AB0")                    # the closed eyes' line, light on the black mask
def eyes(state):
    look = eye_look(state, eye, CB, LINE, line=0.16)
    for s in (1, -1):
        dn = unit(0.43 * s, -0.88, 0.1)
        e = H + np.array([0, 0, 0.01]) + dn * HR
        pts = [(u * s, v) for u, v in EYE]
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(e, dn, (0.045, 0.038), lk, outline=polygon(pts), eps=0.004, rings=20, segs=56)

k.build(voxel=0.0055)
k.eye_states(eyes)
k.export(out)
