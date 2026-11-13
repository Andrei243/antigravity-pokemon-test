# Gligar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gligar.py -- <the export's Gligar.glb> <out.glb>
# A lilac-pink flying scorpion: a big round head with two tall pointed ears, big white eyes, a wide grin with two fangs
# and its tongue lolling out; a small round body; arms ending in crescent pincers, blue membranes stretched from the
# arms down to the hips, scalloped below; short legs with three claws; a long tail of beads curling to a stinger.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

PINK, DARK, BLUE = "#D97CD2", "#B565B8", "#5592D6"
MOUTH, TONGUE, TLINE, WHITE, INK = "#4E1830", "#F2907E", "#C8604E", "#F8F6F4", "#1E141E"

# The head: wide and round, a little flatter at the front where the face is; a small round body hangs under it
H = np.array([0.0, -0.035, 0.67])
HR = np.array([0.175, 0.13, 0.135])
k.add("head", ell(H, HR), PINK, "head")
k.add("face", ell((0, -0.08, 0.655), (0.15, 0.09, 0.115)), PINK, "head")
k.add("neck", ell((0, -0.005, 0.55), (0.085, 0.08, 0.06)), PINK, "body")
k.add("body", ell((0, 0.01, 0.42), (0.12, 0.11, 0.125)), PINK, "body")

# Two tall pointed ears at the head's upper corners, leaning outward, flat front to back
k.add("ear", blade([(0.085, 0.0, 0.72), (0.115, 0.008, 0.83), (0.145, 0.016, 0.93), (0.165, 0.022, 1.0)],
                   [0.065, 0.045, 0.024, 0.004], (0, 1, 0), 0.55), PINK, "ear{s}", mirror=True)

# Short legs: a thigh in the body, a thin shin to a knobbed ankle, a foot with three claws forward
k.add("thigh", tube([(0.05, 0.005, 0.38), (0.075, -0.01, 0.3)], [0.055, 0.045]), PINK, "leg{s}", mirror=True)
k.add("shin", tube([(0.075, -0.01, 0.3), (0.085, -0.005, 0.19), (0.085, 0.0, 0.09)], [0.04, 0.034, 0.032]), PINK,
      "leg{s}", mirror=True)
k.add("ankle", ell((0.085, -0.005, 0.07), (0.05, 0.05, 0.045)), PINK, "leg{s}", mirror=True)
for i, a in enumerate((-0.6, 0.0, 0.6)):
    base = np.array((0.085 + 0.02 * a, -0.03, 0.05))
    k.add(f"toe{i}", blade([base, base + (0.03 * a, -0.035, -0.02), base + (0.045 * a, -0.05, -0.045)],
                           [0.024, 0.016, 0.004], (0, 0, 1), 0.7), PINK, "leg{s}", mirror=True, fuse=False)
# A heel claw behind
k.add("heel", blade([(0.085, 0.02, 0.05), (0.09, 0.05, 0.03), (0.09, 0.065, 0.005)], [0.016, 0.01, 0.003], (0, 0, 1),
                    0.7), PINK, "leg{s}", mirror=True, fuse=False)

# The arms: from the shoulder out and up to a wrist, a round palm and a crescent pincer opening toward the head
S = np.array([0.07, 0.0, 0.543])
W = np.array([0.27, -0.02, 0.62])
k.add("arm", tube([S - (0.02, 0, 0), (0.17, -0.01, 0.56), W], [0.04, 0.032, 0.032]), PINK, "wing{s}", mirror=True)
PALM = np.array([0.3, -0.02, 0.665])
k.add("palm", ell(PALM, (0.055, 0.045, 0.055), rot=(0, -20, 0)), PINK, "wing{s}", mirror=True)
k.add("claw", blade([PALM + (0.025, 0, 0.02), (0.36, -0.02, 0.74), (0.355, -0.02, 0.83), (0.31, -0.02, 0.89),
                     (0.255, -0.02, 0.9)], [0.045, 0.04, 0.03, 0.017, 0.004], (0, 1, 0), 0.6), PINK, "wing{s}",
      mirror=True)
k.add("thumb", blade([PALM + (-0.025, 0, 0.02), (0.255, -0.02, 0.73), (0.235, -0.02, 0.77), (0.24, -0.02, 0.8)],
                     [0.035, 0.026, 0.014, 0.004], (0, 1, 0), 0.6), PINK, "wing{s}", mirror=True)

# The membranes: blue, behind the arm, from the shoulder and the hip out to the wrist, their lower edge scalloped
# into three points
SM = np.array([0.075, 0.025, 0.56])
WM = np.array([0.28, 0.01, 0.62])
HIP = np.array([0.08, 0.03, 0.36])
LOW = np.array([0.3, 0.03, 0.4])
def membrane(u, v):
    top = SM[None, None] + (WM - SM)[None, None] * u[..., None]
    bot = HIP[None, None] + (LOW - HIP)[None, None] * u[..., None]
    bot = bot + (top - bot) * (0.32 * np.abs(np.sin(np.pi * 3 * u)) ** 0.8)[..., None]
    v = 0.03 + 0.97 * v
    return top + (bot - top) * v[..., None]
k.add("skin", sheet(membrane, 30, 16, 0.012), BLUE, "wing{s}", fuse=False, mirror=True, surface="skin")

# The tail: from under the body back, down and round toward the front near the ground, a string of beads darkening
# toward the stinger
CURVE = np.array([(0, 0.06, 0.4), (0, 0.17, 0.35), (0.01, 0.27, 0.27), (0.04, 0.33, 0.16), (0.08, 0.31, 0.07),
                  (0.12, 0.24, 0.05)])
def along(t):
    """A point at t (0..1) of the tail's line, by Catmull-Rom through CURVE."""
    n = len(CURVE) - 1
    i = min(int(t * n), n - 1); f = t * n - i
    p0 = CURVE[max(i - 1, 0)]; p1 = CURVE[i]; p2 = CURVE[i + 1]; p3 = CURVE[min(i + 2, n)]
    return 0.5 * (2 * p1 + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f + (-p0 + 3 * p1 - 3 * p2 + p3) * f ** 3)
k.add("tailroot", tube([CURVE[0], CURVE[1]], [0.045, 0.04]), PINK, "tail")
pink, dark = srgb(PINK), srgb(DARK)
NB = 9
for i in range(NB):
    t = 0.12 + 0.82 * i / (NB - 1)
    r = 0.04 - 0.011 * i / (NB - 1)
    p = along(t)
    col = pink * (1 - i / NB * 0.7) + dark * (i / NB * 0.7)
    k.add(f"bead{i}", ell(p, (r, r, r)), col, "tail", fuse=False)
# The stinger: a round bulb with a hooked point up and forward
ST = along(1.0) + np.array([0.015, -0.03, 0.01])
k.add("bulb", ell(ST, (0.05, 0.05, 0.048)), dark, "tail", fuse=False)
k.add("sting", tube([ST + (0.0, -0.03, 0.03), ST + (0.0, -0.06, 0.07), ST + (-0.01, -0.06, 0.11)],
                    [0.025, 0.013, 0.003]), dark, "tail", fuse=False)

# The mouth: a wide grin carved across the lower face, dark inside
def grin(V):
    V = V.copy()
    V[:, 2] += 0.35 * V[:, 0] ** 2
    return V
k.add("mouth", ell((0, -0.16, 0.605), (0.095, 0.05, 0.028), deform=grin), MOUTH, "head", carve=True)

# The tongue lolling out of the grin and down over the chest, a groove down its middle
k.add("tongue", blade([(0, -0.14, 0.6), (0, -0.175, 0.585), (0, -0.19, 0.54), (0, -0.183, 0.49)],
                      [0.03, 0.042, 0.044, 0.036], (0, 1, 0.25), 0.4), TONGUE, "head", fuse=False, surface="skin")
k.add("groove", tube([(0, -0.192, 0.575), (0, -0.207, 0.54), (0, -0.2, 0.505)], [0.004, 0.004, 0.003]), TLINE, "head",
      fuse=False, surface="skin")
# Two fangs hanging from the grin's corners
k.add("fang", tube([(0.072, -0.148, 0.63), (0.074, -0.158, 0.605), (0.075, -0.162, 0.578)], [0.014, 0.01, 0.002]),
      WHITE, "head", fuse=False, mirror=True, surface="shell")

# A small crease on the forehead between the eyes
CB, CI = srgb(PINK), srgb(INK)
def crease(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - 0.3 * u) < 0.28) & (np.abs(u) < 0.85)] = CI
    return c
k.decal((0.012, -0.16, 0.765), unit(0, -1, 0.5), (0.018, 0.009), crease, eps=0.003, rings=8, segs=36)

k.build(voxel=0.0065)


# The eyes: big white ovals with an ink rim, their tops cut straight and slanting down toward the middle (the
# outline itself is cut, so the line is crisp), a small black pupil set in toward the nose and up. Laid by hand a
# side, u toward +X on both: the left eye's outline and colour are flipped.
EW, PUP = srgb("#FFFFFF"), srgb("#141014")
def top(u):
    return 0.8 + 0.14 * u
def eye(u, v):
    c = np.tile(EW, (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[(r > 0.86) | (v > top(u) - 0.13)] = CI
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
de = unit(0.4, -0.88, 0.22)
E = np.array([0.072, -0.155, 0.7])
def frame(n):
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    look = eye_look(state, eye, CB, CI, line=0.13)
    for side in (1, -1):
        n = de * np.array([side, 1, 1.0])
        outline = polygon([(side * a, b) for a, b in OUT])
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * np.array([side, 1, 1.0]), n, (0.055, 0.056), col, eps=0.005, outline=outline, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            right, up = frame(n)
            c = E * np.array([side, 1, 1.0]) + right * side * (-0.3 * 0.055) + up * (0.02 * 0.056)
            k.decal(c, n, (0.011, 0.014), PUP, eps=0.008, rings=6, segs=24)

k.eye_states(eyes)
k.export(out)
