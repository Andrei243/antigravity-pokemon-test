# Houndour sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/houndour.py -- <the export's Houndour.glb> <out.glb>
# A small black hound on four legs: a short orange muzzle and jaw with a black nose, a frown and a fang up over the
# lip; a white skull plate over the brow and crown with a grey hollow in its middle; small pointed ears; red eyes
# under heavy black lids slanting to the nose; two white rib bands over the back; an orange patch on the chest and
# along the belly; white cuffs round all four ankles; a short pointed tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BLACK, ORANGE, BONE, HOLLOW, NOSE, INK = "#3A3A42", "#EC8034", "#F0F0E8", "#86868E", "#141418", "#1A1418"
HC = np.array([0.0, -0.15, 0.42])        # the head's middle
HR = np.array([0.072, 0.076, 0.07])

# --- the body -------------------------------------------------------------------------------------------------------
k.add("chest", ell((0, -0.07, 0.265), (0.075, 0.085, 0.082)), BLACK, "body")
k.add("body", ell((0, 0.03, 0.258), (0.07, 0.15, 0.08)), BLACK, "body")
k.add("rump", ell((0, 0.125, 0.258), (0.07, 0.075, 0.08)), BLACK, "body")
k.add("neck", tube([(0, -0.085, 0.3), (0, -0.12, 0.35), (0, -0.14, 0.39)], [0.058, 0.052, 0.05]), BLACK, "head")

# Legs, slim and straight, small round paws
k.add("leg", tube([(0.055, -0.08, 0.25), (0.06, -0.09, 0.12), (0.062, -0.095, 0.035)], [0.036, 0.03, 0.028]), BLACK,
      "leg{s}", mirror=True)
k.add("paw", ell((0.062, -0.108, 0.022), (0.034, 0.043, 0.022)), BLACK, "leg{s}", mirror=True)
k.add("thigh", ell((0.055, 0.125, 0.225), (0.045, 0.062, 0.065)), BLACK, "hind{s}", mirror=True)
k.add("hind", tube([(0.06, 0.135, 0.2), (0.062, 0.14, 0.11), (0.062, 0.135, 0.035)], [0.034, 0.029, 0.028]), BLACK,
      "hind{s}", mirror=True)
k.add("hpaw", ell((0.062, 0.122, 0.022), (0.034, 0.043, 0.022)), BLACK, "hind{s}", mirror=True)

# The short pointed tail, up and back from the rump, flat side to side
k.add("tail", blade([(0, 0.17, 0.28), (0, 0.23, 0.31), (0, 0.285, 0.36)], [0.04, 0.026, 0.004], (1, 0, 0), 0.45),
      BLACK, "tail")

# --- the head ---------------------------------------------------------------------------------------------------------
k.add("head", ell(HC, HR), BLACK, "head")
# the short orange muzzle and the jaw under it, rounded at the chin
k.add("snout", ell((0, -0.235, 0.395), (0.044, 0.055, 0.034)), ORANGE, "head")
k.add("chin", ell((0, -0.205, 0.358), (0.05, 0.052, 0.034)), ORANGE, "head")
# Small pointed ears, standing up and a little out
k.add("ear", blade([(0.04, -0.14, 0.46), (0.062, -0.135, 0.505), (0.08, -0.13, 0.545)], [0.032, 0.02, 0.003],
                   (0, 1, 0), 0.45), BLACK, "ear{s}", mirror=True)

# The orange of the face: the muzzle, the jaw and the cheek behind it, under a line from below the eye to the throat
def muzzle(P):
    line = 0.402 + 0.35 * (-0.2 - P[:, 1])
    return inside(P[:, 2] - line, 0.0025) * (P[:, 1] < -0.13) * inside(0.335 - P[:, 2], 0.004)
k.paint(muzzle, ORANGE, on=["head", "snout", "chin", "neck"])
k.paint(lambda P: inside(-(P[:, 2] - (0.402 + 0.35 * (-0.2 - P[:, 1]))), 0.0025) * (P[:, 1] < -0.13), BLACK,
        on=["head", "snout", "chin", "neck"])
# The orange on the chest, low between the forelegs, and a band along the belly
k.paint(lambda P: inside(((P[:, 0] / 0.038) ** 2 + ((P[:, 2] - 0.228) / 0.032) ** 2) - 1, 0.02) * (P[:, 1] < -0.1),
        ORANGE, on=["chest"])
k.paint(lambda P: inside(P[:, 2] - 0.2, 0.003) * (P[:, 1] > -0.04) * (P[:, 1] < 0.12), ORANGE, on=["body"])

# The skull plate: white over the brow and crown, a grey hollow in its middle (in the head's own unit sphere)
def plate(P):
    d = (P - HC) / HR
    return inside(0.48 + 0.12 * d[:, 1] - d[:, 2], 0.03) * (d[:, 1] < 0.55)
k.paint(plate, BONE, on=["head"])
def hollow(P):
    d = (P - HC) / HR
    return inside(((d[:, 0] / 0.36) ** 2 + ((d[:, 1] + 0.12) / 0.34) ** 2) - 1, 0.06) * (d[:, 2] > 0.6)
k.paint(hollow, HOLLOW, on=["head"])

# The black nose at the muzzle's tip
k.add("nose", ell((0, -0.287, 0.408), (0.022, 0.016, 0.016)), NOSE, "head", fuse=False, surface="skin")
# A frown across the muzzle's front
CO, CI = srgb(ORANGE), srgb(INK)
def frown(u, v):
    c = np.tile(CO, (len(u), 1))
    c[(np.abs(v - (0.25 - 0.7 * u * u)) < 0.2) & (np.abs(u) < 0.9)] = CI
    return c
k.decal((0, -0.27, 0.375), unit(0, -1, -0.15), (0.036, 0.018), frown, eps=0.003, rings=8, segs=40)
# A fang up from the lower jaw over the lip, at each corner
k.add("fang", tube([(0.026, -0.261, 0.366), (0.027, -0.267, 0.397)], [0.0075, 0.0012]), BONE, "head", fuse=False,
      mirror=True, surface="shell")

# --- the rib bands over the back: loose bony arcs, wider over the spine, leaning back -----------------------------------
for i, (y, rx, rz) in enumerate(((-0.025, 0.074, 0.082), (0.07, 0.07, 0.08))):
    pts, rad = [], []
    for a in np.linspace(-1.25, 1.25, 9):
        pts.append((math.sin(a) * (rx + 0.002), y + 0.035 * (1 - math.cos(a)), 0.26 + math.cos(a) * (rz + 0.002)))
        rad.append(0.009 + 0.011 * math.cos(a) ** 2)
    k.add(f"rib{i}", tube(pts, rad), BONE, "body", fuse=False, surface="shell")

# --- white cuffs round the ankles ------------------------------------------------------------------------------------
def cuff(V):
    n = 4
    rxy = np.sqrt(V[:, 0] ** 2 + V[:, 1] ** 2)
    s = (rxy ** n + np.abs(V[:, 2]) ** n) ** (1 / n)
    return V / s[:, None]
for bone, y in (("leg{s}", -0.094), ("hind{s}", 0.137)):
    k.add(f"cuff{bone[0]}", ell((0.062, y, 0.07), (0.042, 0.042, 0.022), deform=cuff), BONE, bone, fuse=False,
          mirror=True, surface="cloth")

k.build(voxel=0.0055, smooth=3, band=0.008)

# --- the eyes: red, a dark pupil, under a heavy black lid slanting down toward the nose -------------------------------
HE = 0.027
def eye_top(u):
    return 0.18 + 0.38 * u
de = unit(0.5, -0.84, 0.18)
E = np.array([0.042, -0.205, 0.437])
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def clipped(n=96):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > eye_top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > eye_top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts
OUT = clipped()
RED, DARK, GLINT, WHITE = srgb("#E03A30"), srgb("#1A0A0C"), np.array([1.0, 1.0, 1.0]), srgb("#F2EEE8")
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    r = np.sqrt(((u + 0.12) / 0.62) ** 2 + ((v + 0.05) / 0.72) ** 2)
    c[r < 1] = RED
    c[r < 0.45] = DARK
    c[v > eye_top(u) - 0.17] = srgb(INK)          # the lid's edge, the eye's own crisp top
    return c
CB, LIGHT = srgb(BLACK), srgb("#A8A8B2")
def eyes(state):
    look = eye_look(state, eye, CB, LIGHT, line=0.13)
    for side in (1, -1):
        n = de * np.array([side, 1, 1.0])
        outline = polygon([(side * a, b) for a, b in OUT]) if state in ("Open", "Fierce") else None
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * np.array([side, 1, 1.0]), n, (HE, HE * 0.85), col, eps=0.005, outline=outline, rings=20, segs=56)

k.eye_states(eyes)
k.export(out)
