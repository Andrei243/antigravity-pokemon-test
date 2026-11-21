# Mega Gengar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gengarmega.py -- <the export's Gengar-Mega.glb> <out.glb>
# Grown from gengar.py (copied, since that script builds as it is read): its round body that is all head, its spike()
# and its red wedge eyes with a small pupil. From the references: a darker purple; the two ears grown into great horns
# swept up and back, a fringe of spikes over the crown and down the back and sides, all pointing back; a third eye,
# yellow with a dark ring and pupil, on the brow; the grin a closed crescent of white teeth with a red gum under them;
# thick arms reaching down and forward to the ground and short legs, each burning away to red and pink at its end in
# points of flame rising round it; the body's lowest part reddening too.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, FLAME, TIPS = "#51418A", "#C8344E", "#E879A2"
C = np.array([0.0, 0.0, 0.45])            # the body's middle, between the body and head bones
RX, RY, RZ = 0.28, 0.25, 0.285

k.add("belly", ell(C + (0, 0.01, -0.07), (RX * 0.98, RY * 0.98, RZ * 0.95)), PURPLE, "body")
k.add("head", ell(C + (0, -0.005, 0.03), (RX, RY, RZ * 0.9)), PURPLE, "head")


def spike(name, d, length, base_r, bone, flat=0.6, sink=0.86, root=None):
    """A spike out of the body along d (from its middle), flattened across the side it is seen from."""
    d = unit(*d)
    base = C + d * np.array([RX, RY, RZ]) * sink if root is None else np.array(root, float)
    tip = C + d * np.array([RX, RY, RZ]) * (1.0 + length)
    mid = base + (tip - base) * 0.45
    ax = np.cross((1.0, 0, 0), d)
    if np.linalg.norm(ax) < 1e-3:
        ax = np.array([0, 1.0, 0])
    k.add(name, blade([base, mid, tip], [base_r, base_r * 0.5, 0.004], ax, flat), PURPLE, bone)


# The ears: two great horns swept up, out and back
k.add("ear", blade([(0.12, 0.03, 0.64), (0.21, 0.15, 0.77), (0.3, 0.33, 0.89)], [0.1, 0.055, 0.004], (0, 0.7, -0.7), 0.5),
      PURPLE, "head", mirror=True)
# The fringe: over the crown, down the back and out of the sides, every point turned back
FRINGE = [((0.0, 0.55, 0.85), 0.42, 0.07), ((0.0, 0.85, 0.5), 0.4, 0.065), ((0.0, 0.98, 0.1), 0.35, 0.06),
          ((0.0, 0.95, -0.3), 0.25, 0.05),
          ((0.3, 0.8, 0.5), 0.36, 0.06), ((-0.3, 0.8, 0.5), 0.36, 0.06),
          ((0.75, 0.6, 0.3), 0.42, 0.06), ((-0.75, 0.6, 0.3), 0.42, 0.06),
          ((0.82, 0.55, 0.0), 0.36, 0.055), ((-0.82, 0.55, 0.0), 0.36, 0.055),
          ((0.7, 0.65, -0.3), 0.26, 0.045), ((-0.7, 0.65, -0.3), 0.26, 0.045),
          ((0.35, 0.9, 0.0), 0.32, 0.055), ((-0.35, 0.9, 0.0), 0.32, 0.055)]
for i, (d, length, r) in enumerate(FRINGE):
    spike(f"spike{i}", d, length * 1.3, r * 1.1, "head" if d[2] > 0.3 else "body")


def flames(name, centre, up, side, n, length, r, bone, spread=1.0):
    """Points of flame rising round a limb's end: a ring of fused blades leaning out and up."""
    centre, up = np.array(centre, float), unit(*up)
    side = unit(*side)
    other = np.cross(up, side)
    for j in range(n):
        a = 2 * math.pi * j / n + 0.3
        out_ = math.cos(a) * side + math.sin(a) * other
        d = unit(*(up * 1.0 + out_ * 0.45 * spread))
        root = centre + out_ * r * 1.6 - up * 0.02
        L = length * (0.75 + 0.25 * math.cos(3 * a))
        k.add(f"{name}{j}", blade([root, root + d * L * 0.5, root + d * L], [r * 0.75, r * 0.45, 0.004],
                                  np.cross(d, out_), 0.55), PURPLE, bone)


# The arms: thick, down and forward to hands of flame by the ground
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube(np.array([(0.22, -0.05, 0.42), (0.33, -0.09, 0.33), (0.365, -0.14, 0.22)]) * m,
                             [0.075, 0.07, 0.068]), PURPLE, f"arm{side}")
    HC = np.array([0.37, -0.15, 0.15])
    k.add(f"hand{side}", ell(HC * m, (0.075, 0.075, 0.06)), PURPLE, f"arm{side}")
    flames(f"handflame{side}", HC * m + (0, 0, -0.01), (0.3 * s, -0.25, 1.0), (1, 0, 0), 7, 0.2, 0.045, f"arm{side}")
# The legs: short and thick under the body, burning away at the feet
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"leg{side}", tube(np.array([(0.12, 0.0, 0.26), (0.14, -0.02, 0.14), (0.15, -0.03, 0.07)]) * m,
                             [0.085, 0.078, 0.07]), PURPLE, f"leg{side}")
    FC = np.array([0.15, -0.05, 0.05])
    k.add(f"foot{side}", ell(FC * m, (0.078, 0.085, 0.05)), PURPLE, f"leg{side}")
    flames(f"footflame{side}", FC * m + (0, 0, 0.0), (0.15 * s, -0.1, 1.0), (1, 0, 0), 7, 0.15, 0.045, f"leg{side}")

# The tail: a short point behind, low on the back
k.add("tail", tube([(0, 0.17, 0.3), (0, 0.27, 0.25), (0, 0.34, 0.22), (0, 0.4, 0.215)], [0.075, 0.048, 0.022, 0.004]),
      PURPLE, "tail")

# The limbs' ends burn red, then pink at the tips; the body reddens at its lowest
def burn(P, centre, z0, z1, reach):
    """Near a limb's end, by height: 1 below z0 easing to nothing at z1 (the flames' points stay purple)."""
    flat = np.linalg.norm((P - centre)[:, :2], axis=1)
    return (1 - smoothstep(z0, z1, P[:, 2])) * (flat < reach)
for s in (1, -1):
    HC = np.array([0.37 * s, -0.15, 0.13]); FC = np.array([0.15 * s, -0.05, 0.03])
    k.paint(lambda P, c=HC: burn(P, c, 0.2, 0.3, 0.25), FLAME, on=["arm", "hand"])
    k.paint(lambda P, c=HC: burn(P, c, 0.09, 0.17, 0.25), TIPS, on=["arm", "hand"])
    k.paint(lambda P, c=FC: burn(P, c, 0.13, 0.21, 0.2), FLAME, on=["leg", "foot"])
    k.paint(lambda P, c=FC: burn(P, c, 0.04, 0.1, 0.2), TIPS, on=["leg", "foot"])
k.paint(lambda P: (1 - smoothstep(0.17, 0.27, P[:, 2])) * 0.85 * (np.abs(P[:, 0]) < 0.3), FLAME, on=["belly"])

# The grin: a closed crescent of white teeth, a red gum under them, a dark edge round it, low on the face
MC = np.array([0.0, -0.24, 0.33])
MN = unit(0, -1, 0.1)
def grin_top(u):
    return 0.25 + 0.55 * u * u
def grin_bottom(u):
    return -0.85 + 1.1 * u * u
GRIN = [(u, grin_top(u)) for u in np.linspace(1, -1, 15)] + [(u, grin_bottom(u)) for u in np.linspace(-0.96, 0.96, 15)]
TEETH, GUM, LINE, EDGE = srgb("#F4F2F6"), srgb("#C8344E"), srgb("#9C98AE"), srgb("#24142E")
def grin_look(u, v):
    c = np.tile(TEETH, (len(u), 1))
    top, bot = grin_top(u), grin_bottom(u)
    gum = bot + (top - bot) * 0.28
    c[v < gum] = GUM
    for x in (-0.6, -0.3, 0.0, 0.3, 0.6):
        c[(np.abs(u - x) < 0.04) & (v >= gum)] = LINE
    c[(v > top - 0.08) | (v < bot + 0.08)] = EDGE
    c[np.abs(u) > 0.93] = EDGE
    return c
k.decal(MC, MN, (0.2, 0.09), grin_look, eps=0.005, outline=polygon(GRIN), rings=48, segs=160)

# The third eye: yellow in a dark ring, a dark pupil; open whatever the other two do
TE = np.array([0.0, -0.245, 0.535])
TN = unit(0, -1, 0.45)
def third(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(srgb("#F2D23A"), (len(u), 1))
    c[r > 0.78] = srgb("#2A1830")
    c[r < 0.36] = srgb("#2A1830")
    return c
k.decal(TE, TN, (0.042, 0.042), third, eps=0.006, rings=16, segs=48)

# The eyes: the base's red wedges, a little closer to the middle and lower, under the third eye
RED, DARKRED, PUP = srgb("#E8363E"), srgb("#A81C2C"), srgb("#120A14")
LID, INK = srgb(PURPLE), srgb("#24142E")
WEDGE = [(0.97, 0.6), (0.4, 0.22), (-0.3, -0.1), (-0.96, -0.32), (-0.9, -0.48), (-0.6, -0.66), (-0.2, -0.76),
         (0.25, -0.74), (0.62, -0.6), (0.88, -0.36), (0.98, -0.05)]
def red(u, v):
    c = np.tile(RED, (len(u), 1))
    top = -0.32 + 0.48 * (u + 0.96)
    w = smoothstep(-0.2, -0.05, v - top)[:, None]
    return c * (1 - w) + DARKRED[None] * w
de = unit(0.36, -0.9, -0.15)
EW, EH = 0.085, 0.085
def frame(n):
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    for side in (1, -1):
        n = de * np.array([side, 1, 1.0])
        c = C + (0, -0.005, 0.03) + n * np.array([RX, RY, RZ * 0.9])
        right, up = frame(n)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (WEDGE if side == 1 else WEDGE[::-1])])
        k.decal(c, n, (EW, EH), flip(eye_look(state, red, LID, INK, line=0.1)), eps=0.006, outline=outline,
                rings=20, segs=56)
        if state in ("Open", "Fierce"):
            pc = c + right * side * (-0.3 * EW) + up * (-0.2 * EH)
            k.decal(pc, n, (0.011, 0.017), PUP, eps=0.009, rings=5, segs=20)

k.build(voxel=0.007, smooth=3, band=0.008)
k.eye_states(eyes)
k.export(out)
