# Houndoom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/houndoom.py -- <the export's Houndoom.glb> <out.glb>
# Houndour grown: a lean black hound on long thin legs and a long neck; two grey-white ram's horns curling up, back and
# down round the sides of the head; a long orange muzzle open on a pink tongue, a black nose, small fangs; red eyes
# under heavy black lids slanting to the nose; a white bony collar round the neck's foot with a small skull hanging at
# the throat; three white rib bands over the back; orange along the belly; thick white cuffs round all four ankles;
# white claws; a long thin tail curling up and over, ending in an arrowhead.
# The export's joints: body (0, 0.03, 0.32), head (0, -0.189, 0.5), ears (+-0.055, -0.17, 0.6), tail (0, 0.24, 0.34),
# legs (+-0.081, -0.108, 0.27), hinds (+-0.081, 0.175, 0.27).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BLACK, ORANGE, BONE, HORN, NOSE, INK = "#3A3A42", "#EC8034", "#F0F0E8", "#CFCFD2", "#141418", "#1A1418"
MOUTH, TONGUE = "#8E2A34", "#E0828C"
HC = np.array([0.0, -0.215, 0.575])       # the head's middle
HR = np.array([0.064, 0.076, 0.064])

# --- the body: a deep chest, a slim waist, a rump --------------------------------------------------------------------
k.add("chest", ell((0, -0.095, 0.36), (0.078, 0.09, 0.088)), BLACK, "body")
k.add("body", ell((0, 0.04, 0.355), (0.064, 0.16, 0.07)), BLACK, "body")
k.add("rump", ell((0, 0.165, 0.36), (0.07, 0.075, 0.072)), BLACK, "body")
# The long neck, rising forward from the chest to the head, thick at its foot
k.add("neck", tube([(0, -0.1, 0.37), (0, -0.15, 0.45), (0, -0.19, 0.52), (0, -0.205, 0.56)],
                   [0.062, 0.05, 0.044, 0.045]), BLACK, "head")

# Long thin forelegs, straight, small paws
k.add("leg", tube([(0.07, -0.11, 0.33), (0.08, -0.115, 0.2), (0.085, -0.12, 0.09), (0.086, -0.122, 0.035)],
                  [0.04, 0.03, 0.024, 0.024]), BLACK, "leg{s}", mirror=True)
k.add("paw", ell((0.086, -0.14, 0.022), (0.032, 0.044, 0.022)), BLACK, "leg{s}", mirror=True)
# Hind legs: a thigh, a hock bending back, a paw
k.add("thigh", ell((0.068, 0.18, 0.31), (0.044, 0.065, 0.075)), BLACK, "hind{s}", mirror=True)
k.add("hind", tube([(0.075, 0.185, 0.3), (0.082, 0.215, 0.18), (0.085, 0.225, 0.1), (0.086, 0.205, 0.035)],
                   [0.036, 0.027, 0.023, 0.023]), BLACK, "hind{s}", mirror=True)
k.add("hpaw", ell((0.086, 0.188, 0.022), (0.032, 0.044, 0.022)), BLACK, "hind{s}", mirror=True)

# The long thin tail: back from the rump, rising behind and curling over forward, an arrowhead at its end
TAIL = [(0, 0.23, 0.37), (0, 0.31, 0.37), (0.01, 0.4, 0.39), (0.025, 0.46, 0.43), (0.035, 0.47, 0.49),
        (0.04, 0.44, 0.53), (0.04, 0.395, 0.545)]
k.add("tail", tube(TAIL, [0.022, 0.013, 0.011, 0.011, 0.011, 0.011, 0.01]), BLACK, "tail")
# the arrowhead pointing forward and a little down, flat side to side
AB, AT = np.array([0.04, 0.405, 0.545]), np.array([0.04, 0.33, 0.53])
k.add("arrow", blade([AB + [0, 0.012, 0.002], AB + (AT - AB) * 0.35, AT], [0.034, 0.026, 0.002], (1, 0, 0), 0.4),
      BLACK, "tail", fuse=False)

# --- the head -----------------------------------------------------------------------------------------------------
k.add("head", ell(HC, HR), BLACK, "head")
# the long muzzle, forward and a little down; the open mouth carved into its front
k.add("snout", ell((0, -0.3, 0.552), (0.042, 0.08, 0.036), rot=(14, 0, 0)), ORANGE, "head")
k.add("jaw", ell((0, -0.28, 0.515), (0.04, 0.065, 0.028), rot=(28, 0, 0)), ORANGE, "head")
k.add("cheek", ell((0.032, -0.235, 0.545), (0.03, 0.04, 0.032)), ORANGE, "head", mirror=True)
# Small pointed ears behind the horns, swept back
k.add("ear", blade([(0.035, -0.19, 0.615), (0.05, -0.165, 0.65), (0.06, -0.14, 0.675)], [0.022, 0.014, 0.003],
                   (0, 0.6, -0.8), 0.45), BLACK, "ear{s}", mirror=True)

# The orange of the face: the muzzle, the jaw and the cheek, under a line from below the eye rising toward the nose
def line(P):
    return 0.548 + 0.32 * (-0.24 - P[:, 1])
FACE = ["head", "snout", "jaw", "cheek", "neck"]
k.paint(lambda P: inside(P[:, 2] - line(P), 0.0025) * (P[:, 1] < -0.235) * inside(0.488 - P[:, 2], 0.004), ORANGE,
        on=FACE)
k.paint(lambda P: inside(line(P) - P[:, 2], 0.0025) * (P[:, 1] < -0.2), BLACK, on=["head", "snout", "cheek", "neck"])
# behind the jaw's corner it is black again, a crisp edge
k.paint(lambda P: inside(-0.234 - P[:, 1], 0.0025), BLACK, on=FACE)
# Orange along the belly, from behind the forelegs to the hind legs
k.paint(lambda P: inside(P[:, 2] - 0.305, 0.003) * (P[:, 1] > -0.07) * (P[:, 1] < 0.16), ORANGE,
        on=["body", "chest", "rump"])

# The open mouth: a lens carved into the muzzle's front, its hollow dark red, a pink tongue in it
k.add("mouth", ell((0, -0.37, 0.53), (0.032, 0.075, 0.021), rot=(20, 0, 0)), MOUTH, "head", carve=True)
k.add("tongue", ell((0, -0.325, 0.513), (0.024, 0.045, 0.009), rot=(24, 0, 0)), TONGUE, "head", fuse=False,
      surface="skin")
# The black nose at the muzzle's tip
k.add("nose", ell((0, -0.38, 0.566), (0.02, 0.015, 0.014), rot=(14, 0, 0)), NOSE, "head", fuse=False, surface="skin")
# Small fangs down from the upper jaw at the mouth's corners
k.add("fang", tube([(0.023, -0.338, 0.543), (0.024, -0.343, 0.524)], [0.006, 0.001]), BONE, "head", fuse=False,
      mirror=True, surface="shell")

# The ram's horns: up off the crown, back and down round the side of the head, the tip turning forward
HORNP = [(0.03, -0.21, 0.625), (0.055, -0.198, 0.665), (0.088, -0.165, 0.688), (0.115, -0.115, 0.68),
         (0.13, -0.078, 0.64), (0.133, -0.08, 0.592), (0.125, -0.11, 0.56), (0.114, -0.142, 0.552)]
k.add("horn", tube(HORNP, [0.024, 0.023, 0.021, 0.018, 0.015, 0.011, 0.007, 0.003]), HORN, "head", fuse=False,
      mirror=True, surface="shell")

# --- the bony collar and the skull at the throat -----------------------------------------------------------------------
NC = np.array([0, -0.14, 0.44])                  # the collar's middle, round the neck's foot
AX = unit(0, -0.45, 0.89)                         # along the neck
SX = np.array([1.0, 0, 0]); SW = np.cross(AX, SX)  # SW points forward and down
if SW[1] > 0:
    SW = -SW
pts, rad = [], []
for a in np.linspace(math.radians(-170), math.radians(170), 37):
    r = 0.062 + 0.006 * math.cos(6 * a) ** 2
    pts.append(NC + r * (math.sin(a) * SX + math.cos(a) * SW))
    rad.append(0.016 + 0.004 * abs(math.cos(3 * a)))
k.add("collar", tube(pts, rad, seg=12, per=3), BONE, "body", fuse=False, surface="shell")
# the skull: a white oval hanging at the throat, two dark sockets and a dark mouth
SK = NC + SW * 0.074 - AX * 0.035
k.add("skull", ell(SK, (0.019, 0.01, 0.022), rot=(-25, 0, 0)), BONE, "body", fuse=False, surface="shell")
for s in (1, -1):
    k.add(f"socket{s}", ell(SK + np.array([0.0075 * s, -0.009, 0.005]), (0.0055, 0.004, 0.0065), rot=(-25, 0, 0)),
          NOSE, "body", fuse=False, surface="shell")

# --- three white rib bands over the back, sweeping back as they go down --------------------------------------------
for i, (y, rx, rz) in enumerate(((-0.07, 0.08, 0.09), (0.0, 0.066, 0.073), (0.07, 0.066, 0.072))):
    pts, rad = [], []
    zc = 0.36
    for a in np.linspace(-1.2, 1.2, 11):
        pts.append((math.sin(a) * (rx + 0.002), y + 0.05 * (1 - math.cos(a)), zc + math.cos(a) * (rz + 0.002)))
        rad.append(0.004 + 0.012 * math.cos(a) ** 2)
    k.add(f"rib{i}", tube(pts, rad), BONE, "body", fuse=False, surface="shell")

# --- thick white cuffs round the ankles, white claws on each paw ---------------------------------------------------
def cuff(V):
    n = 4
    rxy = np.sqrt(V[:, 0] ** 2 + V[:, 1] ** 2)
    s = (rxy ** n + np.abs(V[:, 2]) ** n) ** (1 / n)
    return V / s[:, None]
for bone, y in (("leg{s}", -0.121), ("hind{s}", 0.214)):
    k.add(f"cuff{bone[0]}", ell((0.085, y, 0.082), (0.038, 0.038, 0.028), deform=cuff), BONE, bone, fuse=False,
          mirror=True, surface="cloth")
for bone, y in (("leg{s}", -0.178), ("hind{s}", 0.15)):
    for j, dx in enumerate((-0.017, 0.0, 0.017)):
        a = np.array([0.086 + dx, y + 0.008, 0.02])
        k.add(f"claw{bone[0]}{j}", blade([a, a + [dx * 0.2, -0.016, -0.004], a + [dx * 0.3, -0.026, -0.016]],
                                         [0.0065, 0.005, 0.001], (0, 0, 1), 0.6), BONE, bone, fuse=False,
              mirror=True, surface="shell")

k.build(voxel=0.0055, smooth=3, band=0.008)

# --- the eyes: red, a dark pupil, under a heavy black lid slanting down toward the nose ------------------------------
HE = 0.024
def eye_top(u):
    return 0.18 + 0.38 * u
de = unit(0.52, -0.82, 0.22)
E = np.array([0.037, -0.258, 0.585])
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
RED, DARK, WHITE = srgb("#E03A30"), srgb("#1A0A0C"), srgb("#F2EEE8")
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
