# Lucario sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lucario.py -- <the export's Lucario.glb> <out.glb>
#
# Riolu grown (riolu.py: the same blue and black, the mask painted round the head's axis, loose hanging paddles,
# almond eyes with a red iris). From the references: a tall slim jackal standing upright. A long black muzzle, a black
# band across the red eyes running back to four black paddles hanging behind the head (two long, two short), a blue
# forehead and tall pointed blue ears; a cream chest with a jagged edge at the shoulders and over a black waist, a
# white spike on the chest; blue shorts over black shins and feet; blue upper arms with black forearms and paws, a
# white spike on the back of each paw; a long broad blue tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, polygon, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, BLACK, WHITE, CREAM, RED, REDDK, NOSE = ("#3E9BE0", "#2A2D3A", "#F2F4F6", "#EEDFA0", "#E2402C", "#9A1E1A",
                                              "#15161C")
H = np.array([0.0, -0.005, 0.955])        # the head's centre
HR = np.array([0.098, 0.1, 0.098])

# --- the head: round skull, a long muzzle, tall pointed ears ------------------------------------------------------
k.add("head", ell(H, HR), BLUE, "head")
k.add("cheeks", ell((0, -0.035, 0.915), (0.085, 0.08, 0.07)), BLUE, "head")
k.add("snout", ell((0, -0.13, 0.905), (0.038, 0.098, 0.038), rot=(-10, 0, 0)), BLACK, "head")
k.add("jaw", ell((0, -0.1, 0.877), (0.04, 0.08, 0.028), rot=(-6, 0, 0)), BLACK, "head")
k.add("ear", blade([(0.05, 0.012, 1.02), (0.075, 0.02, 1.08), (0.098, 0.03, 1.15), (0.11, 0.036, 1.235)],
                   [0.045, 0.036, 0.022, 0.004], (0, 1, 0), 0.45), BLUE, "ear{s}", mirror=True)
# the paddles' roots, black, at the back of the head behind the eyes
k.add("droot", tube([(0.05, 0.06, 0.955), (0.075, 0.085, 0.94)], [0.03, 0.026]), BLACK, "head", mirror=True)
# the paddles: two long flat ones hanging down behind, two shorter under them; loose, their faces forward
k.add("drop", ell((0.105, 0.105, 0.82), (0.042, 0.016, 0.12), rot=(-10, -12, 0)), BLACK, "head", fuse=False,
      mirror=True)
k.add("drop2", ell((0.06, 0.11, 0.83), (0.03, 0.014, 0.08), rot=(-14, -6, 0)), BLACK, "head", fuse=False,
      mirror=True)

# --- the body: a slim chest, a narrow black waist, blue shorts -----------------------------------------------------
k.add("neck", ell((0, 0.0, 0.81), (0.062, 0.062, 0.07)), CREAM, "body")
k.add("torso", ell((0, 0.0, 0.67), (0.105, 0.085, 0.13)), CREAM, "body")
k.add("waist", ell((0, 0.005, 0.54), (0.085, 0.075, 0.07)), BLACK, "body")
k.add("hips", ell((0, 0.01, 0.46), (0.115, 0.095, 0.07)), BLUE, "body")
CORE = lambda P: inside(np.hypot(P[:, 0] / 0.15, P[:, 1] / 0.13) - 1, 0.004)   # the trunk, not the arms


def front(P):
    return np.abs(np.arctan2(P[:, 0], -P[:, 1]))


# the back of the chest and neck blue; the cream at the front jagged at the shoulders
def cream(P):
    a = front(P)
    side = 1.15 + 0.12 * tri(P[:, 2] * 22)
    return inside(a - side, 0.02)
k.paint(lambda P: (1 - cream(P)) * inside(0.55 - P[:, 2], 0.003) * inside(P[:, 2] - 0.86, 0.003) * CORE(P), BLUE)
# the black waist rising into the cream in jagged points at the front
def waist(P):
    a = front(P)
    top = 0.56 + 0.035 * np.clip(1 - np.abs(np.abs(a) - 0.35) / 0.2, 0, 1) * (np.abs(a) < 1.0)
    return inside(P[:, 2] - top, 0.003) * inside(0.49 - P[:, 2], 0.003) * CORE(P)
k.paint(waist, BLACK)
k.paint(lambda P: inside(P[:, 2] - 0.49, 0.003) * inside(0.33 - P[:, 2], 0.003) * CORE(P), BLUE)
# the chest's spike, white, standing forward
k.add("spike", blade([(0, -0.06, 0.715), (0, -0.115, 0.725), (0, -0.16, 0.735)], [0.026, 0.016, 0.002],
                     (1, 0, 0), 0.9), WHITE, "body", fuse=False, surface="shell")

# arms: blue shoulders and upper arms, black forearms and paws held down and a little forward
k.add("shoulder", ell((0.15, 0.0, 0.775), (0.05, 0.048, 0.05)), BLUE, "arm{s}", mirror=True)
k.add("arm", tube([(0.15, 0.0, 0.77), (0.2, -0.02, 0.68), (0.235, -0.04, 0.62)], [0.038, 0.034, 0.032]), BLUE,
      "arm{s}", mirror=True)
k.add("fore", tube([(0.235, -0.04, 0.625), (0.262, -0.065, 0.55), (0.272, -0.08, 0.5)], [0.033, 0.034, 0.036]),
      BLACK, "arm{s}", mirror=True)
k.add("paw", ell((0.275, -0.088, 0.47), (0.04, 0.042, 0.045)), BLACK, "arm{s}", mirror=True)
k.add("pawspike", blade([(0.29, -0.06, 0.52), (0.325, -0.035, 0.555), (0.355, -0.015, 0.585)],
                        [0.02, 0.013, 0.002], (0, 0, 1), 0.9), WHITE, "arm{s}", fuse=False, mirror=True,
      surface="shell")

# legs: blue thighs as shorts to the knee, black shins and feet
k.add("thigh", ell((0.085, 0.0, 0.4), (0.075, 0.08, 0.1)), BLUE, "leg{s}", mirror=True)
k.add("leg", tube([(0.09, 0.0, 0.36), (0.11, 0.005, 0.24), (0.115, 0.015, 0.13), (0.115, 0.01, 0.06)],
                  [0.055, 0.045, 0.038, 0.034]), BLACK, "leg{s}", mirror=True)
k.add("foot", ell((0.117, -0.045, 0.03), (0.045, 0.085, 0.03)), BLACK, "leg{s}", mirror=True)
for i, x in enumerate((-0.022, 0.0, 0.022)):
    k.add(f"toe{i}", ell((0.117 + x, -0.12, 0.022), (0.016, 0.02, 0.019)), BLACK, "leg{s}", mirror=True)
# the shorts' hem, a crisp line over the knees
k.paint(lambda P: inside(P[:, 2] - 0.315, 0.003) * inside(np.abs(P[:, 0]) - 0.19, 0.003)
        * inside(P[:, 1] - 0.1, 0.003), BLACK, on=["thigh", "leg"])
k.paint(lambda P: inside(0.315 - P[:, 2], 0.003) * inside(P[:, 2] - 0.45, 0.003), BLUE, on=["thigh", "leg"])

# the tail: long and broad, back and down then a little up to a point
k.add("tail", blade([(0, 0.08, 0.5), (0, 0.16, 0.44), (0, 0.26, 0.4), (0, 0.36, 0.4), (0, 0.44, 0.43)],
                    [0.035, 0.05, 0.055, 0.04, 0.005], (1, 0, 0), 0.45), BLUE, "tail")

# --- the mask: the muzzle black, a band across the eyes back to the paddles' roots, the blue forehead dipping
# into it between the eyes -------------------------------------------------------------------------------------------
def mask(P):
    q = P - H
    a = np.arctan2(q[:, 0], -q[:, 1])
    s = np.abs(a) * 0.1
    top = np.interp(s, [0.0, 0.012, 0.035, 0.08, 0.15, 0.3], [0.94, 0.95, 0.985, 0.99, 0.975, 0.96])
    bot = np.interp(s, [0.0, 0.05, 0.085, 0.13, 0.3], [0.85, 0.85, 0.865, 0.912, 0.912])
    near = (np.linalg.norm(q, axis=1) < 0.2) & (np.abs(a) < 1.85)
    return inside(P[:, 2] - top, 0.003) * inside(bot - P[:, 2], 0.003) * near
k.paint(mask, BLACK)
# the chin and throat cream under the black jaw
k.paint(lambda P: inside(P[:, 2] - 0.86, 0.003) * inside(front(P) - 0.9, 0.01), CREAM, on=["cheeks", "neck"])
# the back of the neck blue up into the head, no pale seam where the cream neck meets it
k.paint(lambda P: inside(1.35 - front(P), 0.02) * inside(P[:, 2] - 0.905, 0.003) * inside(0.74 - P[:, 2], 0.003)
        * inside(np.abs(P[:, 0]) - 0.11, 0.003), BLUE)
k.paint(lambda P: inside(np.linalg.norm(P - np.array([0, -0.228, 0.92]), axis=1) - 0.016, 0.003), NOSE,
        on=["snout"])

# --- the eyes: Riolu's almonds narrowed, white with a big red iris and a black slit, the top line sloping in ------
CB, CW, CR, CD, CK = srgb(BLACK), np.array([1.0, 1.0, 1.0]), srgb(RED), srgb(REDDK), srgb(NOSE)
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = ((u - 0.02) / 0.5) ** 2 + ((v - 0.02) / 0.66) ** 2
    c[r < 1] = CD
    c[r < 0.68] = CR
    c[((u - 0.02) / 0.14) ** 2 + ((v - 0.02) / 0.5) ** 2 < 1] = CK
    c[((u + 0.14) / 0.11) ** 2 + ((v - 0.24) / 0.13) ** 2 < 1] = CW
    return c
# a narrow slanted almond: its top line low at the inner corner and rising outward (Lucario's stern look)
EYE = [(-1.0, -0.12), (-0.55, 0.16), (0.0, 0.42), (0.55, 0.62), (0.95, 0.58), (1.0, 0.4),
       (0.6, -0.28), (0.0, -0.52), (-0.55, -0.42)]
LINE = srgb("#8A9AB0")                    # the closed eyes' line, light on the black mask
def eyes(state):
    look = eye_look(state, eye, CB, LINE, line=0.16)
    for s in (1, -1):
        dn = unit(0.5 * s, -0.84, 0.18)
        e = H + np.array([0, 0, -0.005]) + dn * HR
        pts = [(u * s, v) for u, v in EYE]
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(e, dn, (0.034, 0.03), lk, outline=polygon(pts), eps=0.004, rings=20, segs=56)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
