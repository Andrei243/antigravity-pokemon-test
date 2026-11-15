# Mewtwo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mewtwo.py -- <the export's Mewtwo.glb> <out.glb>
#
# The design, from the references: a tall pale grey-lilac biped; a cat-like head with a short muzzle and two short
# blunt horns standing up at the top of its sides, carried a little forward on a short thick neck; narrow fierce eyes
# with a purple iris under a heavy brow; a thick tube from the back of the skull down to the top of the spine, standing
# off the neck; a broad rounded chest plate over a slim waist, shoulders as round knobs; on the lower belly one large
# purple oval from the navel down between the thighs, crisp against the grey; thin arms bent at the elbows, the hands
# in front of the belly, three fingers with round tips; great teardrop thighs fused into the hips, tapering shins,
# long feet with round knobs at the toes and the heel; a long thick purple tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, polygon, eye_look, boxy, args

src, out = args()
k = Kit(src, coat="fur")

SKIN, PURPLE, INKH = "#E2DAE8", "#A275AE", "#2A1C30"
IRIS, WHITE = "#8A4FA8", "#FFFFFF"

# The head is drawn in its own measure about the top of the neck and then grown and carried forward
HEAD_PIVOT, HEAD_GROW, HEAD_FORWARD = np.array([0.0, 0.0, 0.86]), 1.2, np.array([0.0, -0.022, 0.0])


def hd(p):
    """A point of the head's own measure where the model has it."""
    return HEAD_PIVOT + HEAD_GROW * (np.asarray(p, float) - HEAD_PIVOT) + HEAD_FORWARD


def head_part(name, shape, color=SKIN, mirror=False):
    V, F = shape
    k.add(name, (hd(V), F), color, "head", mirror=mirror)


H = np.array([0.0, 0.0, 0.94])             # the skull's middle in the head's own measure
HR = np.array([0.082, 0.082, 0.08])


def wider_top(V):
    """A ribcage: broad at the shoulders, narrowing down to the waist."""
    V = V.copy()
    s = 1 + 0.2 * V[:, 2]
    V[:, 0] *= s
    return V


def taper_down(V):
    """A thigh: a teardrop, full at the hip, narrowing to the knee."""
    V = V.copy()
    s = 1 - 0.45 * np.clip(-V[:, 2], 0, 1) ** 1.2
    V[:, 0] *= s; V[:, 1] *= s
    return V


# --- the trunk: a broad chest plate, a slim waist, the belly ---------------------------------------------------------
k.add("chest", ell((0, 0.012, 0.73), (0.12, 0.072, 0.095), deform=wider_top), SKIN, "body")
k.add("pec", ell((0.052, -0.022, 0.74), (0.072, 0.05, 0.06)), SKIN, "body", mirror=True)
k.add("waist", tube([(0, 0.006, 0.56), (0, 0.01, 0.63), (0, 0.01, 0.68)], [0.076, 0.058, 0.08]), SKIN, "body")
k.add("belly", ell((0, -0.008, 0.505), (0.104, 0.106, 0.1)), SKIN, "body")
k.add("neck", tube([(0, 0.008, 0.79), (0, -0.008, 0.88)], [0.046, 0.042]), SKIN, "body")
k.add("shoulder", ell((0.13, 0.002, 0.79), (0.048, 0.048, 0.046)), SKIN, "arm{s}", mirror=True)

# The purple: one large oval on the lower belly, from the navel down between the thighs (the front alone)
k.paint(lambda P: inside((np.sqrt((P[:, 0] / 0.082) ** 2 + ((P[:, 2] - 0.505) / 0.1) ** 2) - 1) * 0.08, 0.0025)
        * inside(P[:, 1] + 0.01, 0.003) * inside(-0.145 - P[:, 1], 0.003), PURPLE)

# --- the legs: great teardrop thighs fused into the hips, tapering shins, long feet with round knobs -----------------
k.add("thigh", ell((0.098, -0.012, 0.405), (0.086, 0.094, 0.15), rot=(-8, 0, -7), deform=taper_down), SKIN, "leg{s}",
      mirror=True)
k.add("shin", tube([(0.11, -0.022, 0.3), (0.12, 0.0, 0.18), (0.122, 0.022, 0.085)], [0.048, 0.034, 0.027]), SKIN,
      "leg{s}", mirror=True)
k.add("ankle", ell((0.122, 0.024, 0.06), (0.032, 0.036, 0.032)), SKIN, "leg{s}", mirror=True)
k.add("foot", tube([(0.122, 0.03, 0.035), (0.122, -0.04, 0.026), (0.124, -0.1, 0.024)], [0.028, 0.025, 0.022]),
      SKIN, "leg{s}", mirror=True)
for i, (x, y) in enumerate(((0.104, -0.128), (0.146, -0.118))):
    k.add(f"toe{i}", ell((x, y, 0.024), (0.024, 0.026, 0.024)), SKIN, "leg{s}", mirror=True)
k.add("heel", ell((0.122, 0.07, 0.024), (0.024, 0.026, 0.024)), SKIN, "leg{s}", mirror=True)

# --- the arms: bent at the elbows, the forearms forward, the hands in front of the belly ----------------------------
ELBOW, HAND = np.array([0.185, -0.03, 0.635]), np.array([0.11, -0.165, 0.6])
k.add("upper", tube([(0.135, 0.0, 0.785), (0.17, -0.012, 0.71), ELBOW], [0.03, 0.025, 0.022]), SKIN, "arm{s}",
      mirror=True)
k.add("fore", tube([ELBOW, (0.15, -0.105, 0.615), HAND], [0.022, 0.02, 0.022]), SKIN, "arm{s}", mirror=True)
k.add("palm", ell(HAND + np.array([-0.006, -0.01, 0.0]), (0.026, 0.024, 0.022)), SKIN, "arm{s}", mirror=True)
for i, d in enumerate(((-0.036, -0.03, 0.006), (-0.022, -0.048, -0.004), (0.004, -0.044, -0.016))):
    tip = HAND + np.array([-0.006, -0.01, 0.0]) + np.array(d)
    k.add(f"finger{i}", tube([HAND + np.array([-0.006, -0.012, 0.0]), tip], [0.011, 0.009]), SKIN, "arm{s}",
          mirror=True)
    k.add(f"fingertip{i}", ell(tip, (0.016, 0.016, 0.016)), SKIN, "arm{s}", mirror=True)

# --- the head: a cat-like skull, a short muzzle, two blunt horns, the tube behind -----------------------------------
head_part("head", ell(H, HR, deform=boxy(2.3)))
head_part("jaw", ell((0, -0.03, 0.895), (0.062, 0.06, 0.045)))
head_part("muzzle", ell((0, -0.07, 0.902), (0.044, 0.04, 0.036)))
head_part("brow", tube([(0.018, -0.078, 0.966), (0.048, -0.072, 0.975), (0.072, -0.052, 0.976)],
                       [0.014, 0.015, 0.012]), mirror=True)
head_part("horn", tube([(0.046, 0.012, 0.985), (0.058, 0.022, 1.008), (0.066, 0.03, 1.026)], [0.032, 0.029, 0.026]),
          mirror=True)
# The tube: out of the back of the skull, arching back off the neck and down into the top of the spine
k.add("pipe", tube([hd((0, 0.05, 0.955)), (0, 0.1, 0.93), (0, 0.118, 0.86), (0, 0.1, 0.79), (0, 0.07, 0.755)],
                   [0.028, 0.027, 0.027, 0.028, 0.03]), SKIN, "head")

# --- the tail: thick and purple, back and round to the model's right, its end coming forward low ---------------------
k.add("tail", tube([(0, 0.08, 0.5), (0.03, 0.16, 0.44), (0.11, 0.23, 0.36), (0.22, 0.235, 0.28),
                    (0.31, 0.17, 0.21), (0.35, 0.07, 0.16), (0.34, -0.03, 0.13)],
                   [0.05, 0.054, 0.05, 0.046, 0.042, 0.038, 0.034]), PURPLE, "tail")

# --- the mouth: a short line low on the muzzle, its corners a little down -------------------------------------------
INK, CS = srgb(INKH), srgb(SKIN)


def mouth(u, v):
    c = np.tile(CS, (len(u), 1))
    c[(np.abs(v + 0.25 * u * u) < 0.38) & (np.abs(u) < 0.9)] = INK
    return c


k.decal(hd((0, -0.104, 0.885)), unit(0, -1, -0.25), (0.019, 0.0072), mouth, eps=0.003, rings=10, segs=48)

# --- the eyes: narrow and fierce, a large purple iris, the top line sloping down to the nose under the brow ---------
CW, CI, CP = srgb(WHITE), srgb(IRIS), srgb("#1A1020")
HE = 0.04


def eye_top(u):
    return 0.12 + 0.42 * u


def eye_bottom(u):
    return eye_top(u) - 0.85 * (1 - u * u) ** 0.7


ALMOND = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]


def eye_open(state):
    def look(u, v):
        c = np.tile(CW, (len(u), 1))
        top = np.minimum(eye_top(u), -0.05 + 0.65 * u) if state == "Fierce" else eye_top(u)
        c[((u + 0.06) / 0.56) ** 2 + ((v + 0.14) / 0.66) ** 2 < 1] = CI
        c[((u + 0.06) / 0.2) ** 2 + ((v + 0.14) / 0.3) ** 2 < 1] = CP
        c[(v > top - 0.24) & (v <= top)] = INK
        c[v > top] = CS
        return c
    return look


de = unit(0.44, -0.87, 0.12)
E = hd((0.044, -0.074, 0.948))


def eyes(state):
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        look = eye_open(state) if state in ("Open", "Fierce") else eye_look(state, eye_open("Open"), CS, INK, line=0.18)
        k.decal(E * m, de * m, (HE, HE), flip(look), eps=0.004, outline=outline, rings=20, segs=56)


k.build(voxel=0.006, band=0.004)
k.eye_states(eyes)
k.export(out)
