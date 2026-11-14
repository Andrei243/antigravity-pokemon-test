# Weavile sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/weavile.py -- <the export's Weavile.glb> <out.glb>
# Sneasel grown: taller and sharper, the same dark blue: a round head with a crown of dark points over it and points
# of fur at the cheeks; a great red fan of feathers standing up behind the head, ribbed; red feathers out from the sides
# of the head for ears; a red ruff round the neck; a big yellow gem on the brow; sly red eyes slanting up to their outer
# corners with a dark lid and a flick of lash; a small grin with a fang; long thin arms ending in white hands with three
# long claws; lean legs bending forward at the knee, three white claws on each foot; two long red feathers off the tail.
# The export's joints: body (0, 0, 0.44), tail (0, 0.05, 0.37), arms (+-0.1, -0.02, 0.56), head (0, -0.02, 0.62),
# legs (+-0.065, 0.01, 0.36). No ear bones: the ear feathers go on the head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, tri, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

DARK, WHITE, RED, RIB, GOLD, INK = "#3A6083", "#EEF2F7", "#D62A50", "#951630", "#F2C232", "#12141E"
OLD = np.array([0, -0.035, 0.735])
S = 1.12                                  # the head grown about its middle
H = OLD + np.array([0, 0, 0.012])         # the head's centre
HR = np.array([0.11, 0.096, 0.102]) * S
def g(p):
    return H + (np.array(p, float) - OLD) * S

# --- the head: round above, the cheeks narrowing to a small chin, a short muzzle low on the face -----------------------
def jowl(V):
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.22 * low ** 1.4
    V[:, 1] *= 1 - 0.08 * low ** 1.4
    return V
k.add("head", ell(H, HR, deform=jowl), DARK, "head")
k.add("face", ell(g((0, -0.072, 0.7)), np.array((0.074, 0.058, 0.062)) * S), DARK, "head")
k.add("muzzle", ell(g((0, -0.113, 0.674)), np.array((0.04, 0.034, 0.028)) * S), DARK, "head")
k.add("neck", tube([(0, 0.0, 0.55), (0, -0.015, 0.6), (0, -0.025, 0.65)], [0.046, 0.044, 0.046]), DARK, "head")
# The crown of dark points over the head, and two points of fur out from each cheek
for name, base, tip, r in (("crown0", (0, -0.035, 0.825), (0, -0.065, 0.885), 0.036),
                           ("crown1", (0.045, -0.035, 0.815), (0.082, -0.06, 0.866), 0.033),
                           ("crown2", (0.08, -0.03, 0.785), (0.125, -0.045, 0.812), 0.03),
                           ("cheek0", (0.09, -0.03, 0.71), (0.145, -0.025, 0.7), 0.022),
                           ("cheek1", (0.075, -0.045, 0.665), (0.125, -0.035, 0.635), 0.02)):
    b, t = g(base), g(tip)
    r *= S
    mirror = base[0] != 0
    k.add(name, blade([b, b + (t - b) * 0.5, t], [r, r * 0.6, 0.002], (0, 1, 0), 0.45), DARK, "head", mirror=mirror)

# --- a slim body, lean legs, long arms -----------------------------------------------------------------------------
k.add("chest", ell((0, 0.0, 0.51), (0.068, 0.06, 0.075)), DARK, "body")
k.add("belly", ell((0, -0.006, 0.425), (0.082, 0.072, 0.08)), DARK, "body")

k.add("thigh", ell((0.064, 0.0, 0.345), (0.052, 0.056, 0.068)), DARK, "leg{s}", mirror=True)
k.add("leg", tube([(0.065, 0.005, 0.34), (0.09, -0.065, 0.225), (0.098, 0.035, 0.09), (0.098, -0.005, 0.042)],
                  [0.048, 0.034, 0.026, 0.025]), DARK, "leg{s}", mirror=True)
k.add("foot", ell((0.098, -0.038, 0.028), (0.032, 0.056, 0.026)), DARK, "leg{s}", mirror=True)
for j, dx in enumerate((-0.017, 0.0, 0.017)):
    a = np.array([0.098 + dx, -0.075, 0.03])
    k.add(f"toe{j}", blade([a, a + [dx * 0.3, -0.035, 0.0], a + [dx * 0.5, -0.06, -0.022]],
                           [0.011, 0.008, 0.0015], (0, 0, 1), 0.6), WHITE, "leg{s}", fuse=False, mirror=True,
          surface="shell")

# Long thin arms, held down and a little forward and out; white hands with three long claws
SH, EL, WR = np.array([0.085, -0.02, 0.56]), np.array([0.165, -0.06, 0.505]), np.array([0.215, -0.1, 0.45])
k.add("arm", tube([SH, EL, WR], [0.032, 0.026, 0.023]), DARK, "arm{s}", mirror=True)
HAND = np.array([0.232, -0.115, 0.425])
k.add("hand", ell(HAND, (0.034, 0.034, 0.034)), DARK, "arm{s}", mirror=True)
AD = unit(*(WR - EL))
def white_hand(P):
    Q = P.copy()
    Q[:, 0] = np.abs(Q[:, 0])
    t = (Q - WR) @ AD
    across = Q - WR - np.outer(t, AD)
    ang = np.arctan2(across[:, 2], across[:, 0])
    return inside(0.004 + 0.006 * tri(ang * 5 / (2 * math.pi)) - t, 0.003) * (Q[:, 2] < 0.5)
k.paint(white_hand, WHITE, on=["hand", "arm"])
for j, dx in enumerate((-0.018, 0.0, 0.018)):
    a = HAND + np.array([dx, -0.02, -0.018])
    k.add(f"claw{j}", blade([a, a + [dx * 0.4, -0.03, -0.028], a + [dx * 0.6, -0.028, -0.068]],
                            [0.012, 0.009, 0.0015], (1, 0, 0), 0.55), WHITE, "arm{s}", fuse=False, mirror=True,
          surface="shell")

# --- the red ruff round the neck, lower in front, its lower edge in points ------------------------------------------
NC = np.array([0, -0.015, 0.0])
def ruff(u, v):
    th = math.pi * (-0.995 + 1.99 * u)         # the seam at the back
    front = 0.5 + 0.5 * np.cos(th)          # 1 in front, 0 behind
    top = 0.648 - 0.01 * front
    bottom = 0.6 - 0.05 * front + 0.004 * tri(th * 8 / (2 * math.pi))
    z = top + (bottom - top) * v
    r = 0.056 + 0.006 * front + (0.03 + 0.02 * front) * v ** 1.2
    return np.stack([r * np.sin(th), -0.015 - r * np.cos(th), z], axis=-1)
k.add("ruff", sheet(ruff, 96, 8, 0.012), RED, "head", fuse=False)

# --- the great fan of feathers behind the head, standing up and leaning back a little, ribbed ------------------------
FC = np.array([0, 0.04, 0.72])
LEAN = math.tan(math.radians(14))
def fan_at(a, r):
    return np.stack([r * np.sin(a), FC[1] + r * np.cos(a) * LEAN, FC[2] + r * np.cos(a)], axis=-1)
def fan(u, v):
    a = np.radians(-74 + 148 * u)
    # a flat run between the ribs, as a fan's folds
    rr = 0.3 - 0.009 * np.abs(tri(u * 2.5))
    return fan_at(a, 0.06 + (rr - 0.06) * v)
k.add("fan", sheet(fan, 64, 12, 0.014), RED, "head", fuse=False)
for j, d in enumerate((-44.4, -14.8, 14.8, 44.4)):
    a = math.radians(d)
    pts = [fan_at(np.array(a), np.array(r)) + np.array([0, -0.009, 0]) for r in (0.11, 0.19, 0.274)]
    k.add(f"rib{j}", tube(pts, [0.004, 0.0038, 0.0025]), RIB, "head", fuse=False)

# --- the ear feathers: broad red flags out from the sides of the head, a little down ---------------------------------
EAR = [(0.092, 0.0, 0.715), (0.135, 0.01, 0.698), (0.178, 0.02, 0.676), (0.215, 0.03, 0.652)]
k.add("earfeather", blade([g(p) for p in EAR], [0.018, 0.04, 0.036, 0.004], (0, 1, 0.15), 0.28), RED, "head", fuse=False,
      mirror=True, surface="fur")

# --- two long red feathers off the tail, out to +X and back -----------------------------------------------------------
for j, tip in enumerate([(0.27, 0.19, 0.4), (0.22, 0.21, 0.26)]):
    b = np.array([0.02, 0.06, 0.38]); t = np.array(tip)
    d = unit(*(t - b))
    ax = np.array([0, 1.0, 0]) - d * d[1]; ax /= np.linalg.norm(ax)
    k.add(f"plume{j}", blade([b, b + (t - b) * 0.45 + [0, 0.01, 0.01], t], [0.016, 0.036, 0.003], ax, 0.28),
          RED, "tail", fuse=False, surface="fur")
k.add("rump", ell((0, 0.05, 0.38), (0.04, 0.035, 0.035)), DARK, "tail")

# --- the big yellow gem on the brow -----------------------------------------------------------------------------------
k.decal(g((0, -0.12, 0.785)), unit(0, -1, 0.45), (0.021, 0.029), GOLD, eps=0.004, rings=8, segs=32, surface="shell")

# The mouth: a small sly grin low on the muzzle, a little white fang hanging from its top
CM, CF, CDK = srgb("#24101A"), srgb(WHITE), srgb(DARK)
def grin(u, v):
    c = np.tile(CDK, (len(u), 1))
    top = 0.05 + 0.32 * u + 0.18 * u * u
    lens = np.clip(1 - ((u - 0.3) / 0.62) ** 2, 0, 1)
    low = top - 0.2 - 0.75 * lens
    dark = (v < top) & (v > low) & (np.abs(u) < 0.9)
    c[dark] = CM
    fang = (v < top - 0.02) & (v > top - 0.42) & (np.abs(u - 0.5) < 0.16 * (v - (top - 0.42)) / 0.4)
    c[dark & fang] = CF
    return c
k.decal(g((0.004, -0.144, 0.664)), unit(0, -1, -0.15), (0.032, 0.02), grin, eps=0.004, rings=12, segs=48)

k.build(voxel=0.0055)

# --- the eyes: almond, slanting up to the outer corner where a dark lash flicks out; a red iris, a dark pupil ---------
CD, CW, CR, CP, CI, LIGHT = srgb(DARK), srgb(WHITE), srgb("#D8283E"), srgb("#1A0A10"), srgb(INK), srgb("#A9C2D8")
def top(u):
    return 0.2 + 0.46 * u - 0.12 * u * u
def bottom(u):
    return -0.5 + 0.24 * u + 0.22 * u * u
def eye(u, v):
    c = np.tile(CD, (len(u), 1))
    white = (v < top(u)) & (v > bottom(u)) & (np.abs(u) < 0.96)
    c[white] = CW
    iris = white & (((u + 0.18) / 0.4) ** 2 + ((v + 0.08) / 0.52) ** 2 < 1)
    c[iris] = CR
    c[iris & (((u + 0.18) / 0.14) ** 2 + ((v + 0.08) / 0.3) ** 2 < 1)] = CP
    c[(np.abs(v - top(u) - 0.03) < 0.14) & (np.abs(u) < 0.98)] = CI
    c[(u > 0.55) & (v > top(u) - 0.05) & (v < top(u) + 0.34 * (u - 0.4)) & (u * u + v * v < 0.97)] = CI
    return c
de = unit(0.48, -0.87, 0.08)
E = H + de * 0.093 * S + np.array([0, 0, -0.011])
def eyes(state):
    ink = CI if state in ("Open", "Fierce") else LIGHT
    k.decal(E, de, (0.04, 0.033), eye_look(state, eye, CD, ink, line=0.17), eps=0.005, rings=20, segs=56,
            mirror=True)

k.eye_states(eyes)
k.export(out)
