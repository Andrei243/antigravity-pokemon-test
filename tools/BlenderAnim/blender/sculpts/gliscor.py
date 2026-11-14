# Gliscor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gliscor.py -- <the export's Gliscor.glb> <out.glb>
# Grown from gligar.py: the same tall pointed ears, the same eyes cut straight along their tops with a small dark
# pupil, a carved open mouth with fangs seated in its rim, beads down the tail, membranes as a loose sheet.
#
# The design, from the references: a big lilac-grey bat-scorpion. A wide head, broad at the cheeks, with two big
# pointed ears rising up and out, red inside and the red running down to the eyes' outer corners; angry yellow eyes
# with a small black slit; a wide open mouth, pink inside, two big upper fangs and small teeth, a ruff of points under
# the jaw. A thin red neck running into a red point on a round chest. Thin red forearms with dark bands, a grey wrist
# and huge crab pincers held up beside the head, as high as the ears. Wide dark membrane wings behind, from the arms out
# to a point level with the pincers and down to the hips, their lower edge scalloped between three fingers. Short legs
# with three claws. A long tail of beads hanging below, ending in a big round stinger with two hooked prongs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

GREY, GDARK, RED, BAND, WING, RIB = "#9494C2", "#7A78A8", "#D8283C", "#8A1E3A", "#46464E", "#34343A"
MOUTH, WHITE, INK = "#D8607A", "#F8F6F4", "#1E141E"


def seg_param(P, A, B):
    """For points P: the share along A->B of their nearest point on it, and their distance from it."""
    A, B = np.array(A, float), np.array(B, float)
    d = B - A
    t = np.clip(((P - A) @ d) / (d @ d), 0, 1)
    return t, np.linalg.norm(P - (A + t[:, None] * d), axis=1)


# ---- the head: wide, broad at the cheeks, a little flat at the front
H = np.array([0.0, -0.03, 0.735])
HR = np.array([0.15, 0.115, 0.105])
k.add("head", ell(H, HR), GREY, "head")
k.add("face", ell((0, -0.075, 0.725), (0.125, 0.07, 0.085)), GREY, "head")
k.add("cheek", ell((0.075, -0.05, 0.675), (0.08, 0.075, 0.06)), GREY, "head", mirror=True)
k.add("jaw", ell((0, -0.07, 0.655), (0.1, 0.07, 0.05)), GREY, "head")

# Two big pointed ears from the head's upper corners, up and out, flat front to back
EAR = [(0.07, 0.0, 0.79), (0.12, 0.008, 0.85), (0.17, 0.016, 0.9), (0.215, 0.022, 0.935)]
k.add("ear", blade(EAR, [0.072, 0.052, 0.026, 0.004], (0, 1, 0), 0.5), GREY, "ear{s}", mirror=True)
# their fronts red inside a grey rim, the red running down to the eyes' outer corners
EA, EB = np.array([0.075, 0.0, 0.79]), np.array([0.205, 0.02, 0.928])
def ear_lining(P):
    m = np.zeros(len(P))
    for s in (1, -1):
        Q = P * np.array([s, 1, 1.0])
        t, dist = seg_param(Q, EA, EB)
        w = 0.05 * (1 - t) ** 0.9 + 0.003
        front = inside(Q[:, 1] - (0.004 + 0.016 * t), 0.004)
        m = np.maximum(m, inside(dist - w, 0.004) * front * inside(t - 0.93, 0.02))
        # down to the eye's outer corner
        t2, d2 = seg_param(Q, (0.083, -0.1, 0.775), (0.1, -0.04, 0.82))
        m = np.maximum(m, inside(d2 - 0.016, 0.004) * inside(Q[:, 1] + 0.02, 0.01))
    return m
k.paint(ear_lining, RED)

# ---- the neck and chest: a thin red neck into a round chest with a red point down its front
k.add("neck", tube([(0, -0.005, 0.66), (0, -0.0, 0.57)], [0.034, 0.038]), RED, "body")
k.add("chest", ell((0, 0.0, 0.5), (0.078, 0.072, 0.08)), GREY, "body")
def spade(P):
    x, z = np.abs(P[:, 0]), P[:, 2]
    m = inside(x - 0.042 * np.clip((z - 0.495) / 0.07, 0, 1), 0.004) * smoothstep(0.492, 0.498, z)
    return m * inside(P[:, 1] + 0.02, 0.006) * inside(P[:, 2] - 0.6, 0.004)
k.paint(spade, RED)

# ---- the arms: a thin red forearm with dark bands from behind the head to a grey wrist, a huge pincer above it
SH = np.array([0.08, 0.02, 0.63])
WR = np.array([0.28, -0.02, 0.735])
k.add("arm", tube([SH, (0.18, 0.0, 0.68), WR], [0.03, 0.026, 0.026]), RED, "wing{s}", mirror=True)
def bands(P):
    m = np.zeros(len(P))
    for s in (1, -1):
        Q = P * np.array([s, 1, 1.0])
        t, dist = seg_param(Q, (0.15, 0.0, 0.665), WR)
        m = np.maximum(m, inside(dist - 0.04, 0.002) * inside(np.abs(((t * 3.2) % 1) - 0.5) - 0.13, 0.03)
                       * inside(t - 0.92, 0.01))
    return m
k.paint(bands, BAND, on=["arm"])
k.add("wrist", ell(WR + (0.01, 0, 0.012), (0.04, 0.038, 0.04)), GREY, "wing{s}", mirror=True)
PALM = np.array([0.33, -0.02, 0.8])
k.add("palm", ell(PALM, (0.075, 0.055, 0.07), rot=(0, -15, 0)), GREY, "wing{s}", mirror=True)
# the outer finger: big, curving out, up and over toward the head; the thumb rising inside it
k.add("claw", blade([PALM + (0.03, 0, 0.0), (0.4, -0.02, 0.86), (0.405, -0.02, 0.93), (0.37, -0.02, 0.98),
                     (0.315, -0.02, 0.995), (0.28, -0.02, 0.975)], [0.075, 0.07, 0.058, 0.04, 0.02, 0.004],
                    (0, 1, 0), 0.62), GREY, "wing{s}", mirror=True)
k.add("thumb", blade([PALM + (-0.035, 0, 0.02), (0.27, -0.02, 0.87), (0.255, -0.02, 0.91), (0.265, -0.02, 0.935)],
                     [0.05, 0.036, 0.02, 0.004], (0, 1, 0), 0.62), GREY, "wing{s}", mirror=True)
# the notches of the claw's inner edge
for i, (c, r) in enumerate([((0.345, -0.02, 0.905), 0.016), ((0.315, -0.02, 0.935), 0.014)]):
    k.add(f"notch{i}", ell(c, (r, 0.07, r * 0.8), rot=(0, 30, 0)), GDARK, "wing{s}", mirror=True, carve=True)

# ---- the legs: short, a round hip and a small foot of three claws pointing out and down
k.add("hip", ell((0.085, 0.0, 0.445), (0.048, 0.045, 0.045)), GREY, "leg{s}", mirror=True)
k.add("foot", ell((0.135, -0.015, 0.425), (0.035, 0.03, 0.028)), GREY, "leg{s}", mirror=True)
for i, a in enumerate((-0.7, 0.0, 0.7)):
    base = np.array((0.155, -0.02 + 0.015 * a, 0.425))
    d = unit(0.6, -0.15 + 0.35 * a, -0.75 + 0.25 * abs(a))
    k.add(f"toe{i}", blade([base, base + d * 0.03, base + d * 0.05], [0.014, 0.01, 0.003], (0, 0, 1), 0.7),
          GREY, "leg{s}", mirror=True, fuse=False, surface="shell")

# ---- the tail: a string of beads hanging down and curling to +X, then the stinger
def shorter(p):                     # the tail drawn up toward its root: 0.8 of its first length
    p = np.array(p, float)
    p[..., 2] = 0.42 - (0.42 - p[..., 2]) * 0.8
    return p
CURVE = shorter([(0, 0.04, 0.42), (0, 0.055, 0.33), (0.005, 0.06, 0.25), (0.03, 0.05, 0.18), (0.07, 0.035, 0.13)])
k.add("tailroot", ell((0, 0.03, 0.43), (0.05, 0.048, 0.045)), GREY, "tail")
for i, (p, r) in enumerate(zip(CURVE[1:], (0.048, 0.046, 0.045, 0.043))):
    k.add(f"bead{i}", ell(p, (r, r, r)), GREY, "tail", fuse=False)
    k.add(f"beadjoint{i}", ell((CURVE[i] + p) / 2, (r * 0.7, r * 0.7, r * 0.7)), GDARK, "tail", fuse=False)
ST = shorter([0.125, 0.02, 0.085])
k.add("bulb", ell(ST, (0.068, 0.062, 0.065)), GREY, "tail", fuse=False)
# two hooked prongs opening toward -X: one from the bulb's top curling over, one from its bottom curling up
k.add("prongup", blade([ST + (0.0, 0, 0.05), ST + (-0.015, 0, 0.1), ST + (-0.055, 0, 0.125), ST + (-0.09, 0, 0.115)],
                       [0.034, 0.025, 0.013, 0.003], (0, 1, 0), 0.65), GREY, "tail", fuse=False, surface="shell")
k.add("prongdown", blade([ST + (-0.04, 0, -0.035), ST + (-0.08, 0, -0.065), ST + (-0.115, 0, -0.06),
                          ST + (-0.13, 0, -0.035)], [0.03, 0.02, 0.011, 0.003], (0, 1, 0), 0.65), GREY, "tail",
      fuse=False, surface="shell")

# ---- the wings: dark membranes behind the arms, from shoulder to wrist and on out to a point level with the pincers,
# down to the hips, the lower edge scalloped between three finger points; darker ribs from the wrist to the points
TOP = np.array([(0.06, 0.05, 0.64), (0.28, 0.045, 0.76), (0.5, 0.06, 0.785)])
BOT = np.array([(0.09, 0.05, 0.43), (0.23, 0.05, 0.44), (0.36, 0.05, 0.49), (0.46, 0.06, 0.58)])
def piece(pts, u):
    n = len(pts) - 1
    s = np.clip(u * n, 0, n - 1e-9); i = np.floor(s).astype(int); f = (s - i)[..., None]
    return pts[i] * (1 - f) + pts[i + 1] * f
def membrane(u, v):
    u = 0.005 + 0.99 * u
    top = piece(TOP, np.clip(u / 0.55, 0, 1) * 0.5 + np.clip((u - 0.55) / 0.45, 0, 1) * 0.5)
    bot = piece(BOT, u)
    bot = bot + (top - bot) * (0.16 * np.abs(np.sin(np.pi * 3 * u)) ** 0.7)[..., None]
    v = 0.01 + 0.98 * v
    P = top + (bot - top) * v[..., None]
    P[..., 1] += 0.03 * np.sin(np.pi * v) * np.sin(np.pi * u)          # a little billow backward
    return P
k.add("membrane", sheet(membrane, 48, 22, 0.012), WING, "wing{s}", fuse=False, mirror=True, surface="skin")
for i, uf in enumerate((1 / 3, 2 / 3, 1.0)):
    tip = piece(BOT, np.array(uf * 0.995))
    a, b = np.array(WR) + (0, 0.05, 0.02), tip
    pts = [a + (b - a) * t + np.array([0, 0.03 * math.sin(math.pi * t) * math.sin(math.pi * uf * 0.99), 0])
           for t in (0.0, 0.5, 0.97)]
    k.add(f"rib{i}", tube(pts, [0.009, 0.008, 0.004], seg=10, per=6), RIB, "wing{s}", fuse=False, mirror=True,
          surface="skin")

# ---- the mouth: a wide open grin, one clean patch on the face: an ink rim, dark pink inside, a lighter tongue, two
# small pointed teeth hung from the upper lip; the two big white upper fangs at its corners are loose pieces in front
def m_top(u):
    return 0.62 - 0.22 * u * u
def m_bot(u):
    return -0.92 + 0.72 * u * u
MW = [(u, m_top(u)) for u in np.linspace(-1, 1, 25)] + [(u, m_bot(u)) for u in np.linspace(1, -1, 25)[1:-1]]
CM, CT, CW_, CK = srgb("#A83A58"), srgb(MOUTH), srgb(WHITE), srgb(INK)
def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[v < m_bot(u) + 0.55 - 0.3 * u * u] = CT                          # the tongue, lighter, low in the mouth
    for x in (-0.38, 0.38):                                             # two small pointed teeth from the upper lip
        c[(v < m_top(u)) & (v > m_top(u) - 0.42) & (np.abs(u - x) < 0.13 * (v - m_top(u) + 0.42) / 0.42)] = CW_
    rim = (v > m_top(u) - 0.17) | (v < m_bot(u) + 0.17) | (np.abs(u) > 0.9)
    c[rim] = CK
    return c
k.decal((0, -0.15, 0.682), unit(0, -1, 0.12), (0.066, 0.034), mouth, eps=0.004, outline=polygon(MW), rings=20,
        segs=72)
for s in (1, -1):
    k.add(f"fang{'R' if s > 0 else 'L'}", tube([(0.046 * s, -0.142, 0.7), (0.047 * s, -0.149, 0.685),
                                               (0.048 * s, -0.152, 0.668)], [0.0125, 0.0085, 0.0015]),
          WHITE, "head", fuse=False, surface="shell")

# ---- the ruff of points under the jaw and at the cheeks
for i, x in enumerate((-0.07, -0.035, 0.0, 0.035, 0.07)):
    base = np.array((x, -0.075 + 0.015 * abs(x) / 0.07, 0.625))
    d = unit(x * 2.5, -0.35, -1)
    k.add(f"ruff{i}", blade([base, base + d * 0.025, base + d * 0.05], [0.018, 0.012, 0.003], (0, 1, 0.3), 0.55),
          GREY, "head", fuse=False)
k.add("cheekspike", blade([(0.13, -0.045, 0.67), (0.165, -0.045, 0.655), (0.185, -0.04, 0.635)],
                          [0.022, 0.013, 0.003], (0, 1, 0), 0.55), GREY, "head", mirror=True, fuse=False)

k.build(voxel=0.006, smooth=3)

# ---- the eyes: yellow, cut straight along their tops and sloping hard down toward the nose, an ink rim, a small
# black slit set toward the nose. Laid by hand a side, u toward +X on both: the left's outline and colour flipped.
CB, CI, EY, PUP = srgb(GREY), srgb(INK), srgb("#F6D830"), srgb("#141014")
def top(u):
    return 0.42 + 0.42 * u
def eye(u, v):
    c = np.tile(EY, (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[((u + 0.12) / 0.13) ** 2 + ((v + 0.02) / 0.45) ** 2 < 1] = PUP
    c[(r > 0.84) | (v > top(u) - 0.15)] = CI
    return c
def clipped_outline(n=96):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * du, t * dv))
    return pts
OUT = clipped_outline()
de = unit(0.42, -0.88, 0.2)
E = np.array([0.058, -0.142, 0.762])
def eyes(state):
    look = eye_look(state, eye, CB, CI, line=0.13)
    for side in (1, -1):
        n = de * np.array([side, 1, 1.0])
        outline = polygon([(side * a, b) for a, b in OUT])
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * np.array([side, 1, 1.0]), n, (0.042, 0.04), col, eps=0.005, outline=outline, rings=20, segs=56)

k.eye_states(eyes)
k.export(out)
