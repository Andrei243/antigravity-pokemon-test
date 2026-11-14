# Girafarig sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/girafarig.py -- <the export's Girafarig.glb> <out.glb>
#
# From the references: a slender giraffe on long legs, yellow in front and dark brown behind, the two meeting in one
# clean line down the middle of the body; angular brown spots on the yellow neck and shoulders, yellow spots on the
# brown near the line. A long neck leaning forward to a small head held up, a round pink muzzle, the mouth open under
# it, big eyes with a dark blue iris, pointed ears lined pink, a pale cap on the crown and two short horns ending in
# pale knobs. Pink triangular spines down the back of the neck to the middle of the back. Grey hooves. The tail rises
# up and back from the rump to a round dark head of its own, which looks backward with two yellow eyes and a grin of
# white teeth.
# The export's joints: body (0, 0.02, 0.5), head (0, -0.21, 0.86), ears (+-0.045, -0.21, 0.95), tail (0, 0.21, 0.55),
# legs (+-0.06, -0.15, 0.45), hinds (+-0.06, 0.17, 0.45).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, BROWN, TAILHEAD, PINK, HOOF = "#F6C431", "#55321E", "#3C2216", "#EC9FAE", "#5C5C66"
IVORY, CAP, MOUTH, INK = "#ECE8DC", "#ECE6D6", "#6A1E2A", "#1E1A1C"

# --- the body: a compact barrel, a deep chest, a rump -----------------------------------------------------------
k.add("barrel", ell((0, 0.0, 0.52), (0.128, 0.2, 0.138)), YELLOW, "body")
k.add("chest", ell((0, -0.13, 0.54), (0.125, 0.1, 0.148)), YELLOW, "body")
k.add("rump", ell((0, 0.15, 0.53), (0.124, 0.095, 0.138)), BROWN, "body")
# the neck, thick at its foot and tapering a little, leaning forward up to the head
NECK0, NECK1 = np.array([0, -0.14, 0.6]), np.array([0, -0.215, 0.87])
k.add("neck", tube([NECK0, (0, -0.172, 0.7), (0, -0.198, 0.79), NECK1], [0.098, 0.074, 0.062, 0.058]), YELLOW,
      "head")

# --- the head: large, held up, a big round pink muzzle; everything about the neck's top grown by a third -----------
HB, HS = np.array([0, -0.215, 0.87]), 1.33


def hp(p):
    return HB + (np.array(p, float) - HB) * HS


H = hp((0, -0.235, 0.92))
k.add("head", ell(H, np.array((0.065, 0.076, 0.063)) * HS), YELLOW, "head")
k.add("muzzle", ell(hp((0, -0.305, 0.925)), np.array((0.047, 0.06, 0.046)) * HS, rot=(-12, 0, 0)), YELLOW, "head")
k.add("nose", ell(hp((0, -0.355, 0.935)), np.array((0.038, 0.03, 0.036)) * HS, rot=(-12, 0, 0)), PINK, "head")
# the open mouth under the nose: a dark red patch on the muzzle's underside
k.decal(hp((0, -0.345, 0.9)), unit(0, -0.8, -0.6), (0.026 * HS, 0.022 * HS), MOUTH, eps=0.004, rings=10, segs=40)
# the pale cap on the crown
CAPZ, CAPY = hp((0, -0.262, 0.962))[2], hp((0, -0.262, 0.962))[1]
k.paint(lambda P: inside(CAPZ - P[:, 2], 0.0025) * inside(P[:, 1] - CAPY, 0.003), CAP, on=["head", "muzzle"])
# two short horns ending in pale knobs
k.add("horn", tube([hp((0.022, -0.225, 0.96)), hp((0.028, -0.222, 1.0)), hp((0.031, -0.22, 1.03))],
                   [0.014, 0.013, 0.012]), IVORY, "head", fuse=False, mirror=True, surface="shell")
k.add("knob", ell(hp((0.032, -0.22, 1.045)), (0.025, 0.025, 0.024), sub=3), IVORY, "head", fuse=False, mirror=True,
      surface="shell")
# pointed ears, out and back, lined pink
k.add("ear", blade([hp((0.04, -0.21, 0.95)), hp((0.075, -0.195, 0.965)), hp((0.115, -0.175, 0.975))],
                   [0.029, 0.024, 0.004], (0, 1, 0), 0.5), YELLOW, "ear{s}", mirror=True)

# --- pink spines down the back of the neck to the middle of the back --------------------------------------------
SPINES = [((-0.15, 0.85), 0.045), ((-0.125, 0.78), 0.055), ((-0.1, 0.72), 0.06), ((-0.07, 0.675), 0.062),
          ((-0.03, 0.665), 0.058), ((0.015, 0.664), 0.052), ((0.06, 0.66), 0.042)]
for i, ((y, z), L) in enumerate(SPINES):
    b0 = np.array([0, y + 0.02, z - 0.008])
    d = unit(0, 0.55, 1)
    k.add(f"spine{i}", blade([b0, b0 + d * L * 0.5, b0 + d * L], [0.026, 0.015, 0.002], (1, 0, 0), 0.5), PINK,
          "head" if z > 0.7 else "body", fuse=False)

# --- legs: slender but full at the top where they flow into the body, a knee, grey hooves --------------------------
k.add("fore", tube([(0.062, -0.15, 0.52), (0.066, -0.158, 0.3), (0.066, -0.152, 0.12), (0.066, -0.15, 0.05)],
                   [0.068, 0.042, 0.032, 0.031]), YELLOW, "leg{s}", mirror=True)
k.add("forehoof", ell((0.066, -0.157, 0.027), (0.036, 0.041, 0.028)), HOOF, "leg{s}", mirror=True, surface="shell")
k.add("thigh", ell((0.065, 0.165, 0.46), (0.065, 0.088, 0.105)), BROWN, "hind{s}", mirror=True)
k.add("hind", tube([(0.066, 0.17, 0.44), (0.069, 0.205, 0.3), (0.068, 0.205, 0.17), (0.066, 0.18, 0.05)],
                   [0.058, 0.04, 0.031, 0.031]), BROWN, "hind{s}", mirror=True)
k.add("hindhoof", ell((0.066, 0.172, 0.027), (0.036, 0.041, 0.028)), HOOF, "hind{s}", mirror=True, surface="shell")

# --- the tail: up and back from the rump to a round dark head of its own ------------------------------------------
TC = np.array([0, 0.315, 0.815])
TR = 0.074
k.add("tail", tube([(0, 0.21, 0.6), (0, 0.262, 0.64), (0, 0.295, 0.7), (0, 0.308, 0.75)],
                   [0.03, 0.025, 0.024, 0.027]), BROWN, "tail")
k.add("tailhead", ell(TC, (TR, TR * 1.02, TR * 0.96)), TAILHEAD, "tail")

# --- colours: one clean line between yellow and brown, then the spots ----------------------------------------------
def line_y(z):
    return 0.045 + 0.2 * (z - 0.525)

k.paint(lambda P: inside(line_y(P[:, 2]) - P[:, 1], 0.0025) * inside(P[:, 2] - 0.725, 0.003), BROWN)
k.paint(lambda P: inside(P[:, 1] - line_y(P[:, 2]), 0.0025) * inside(0.4 - P[:, 1], 0.003)
        * inside(P[:, 2] - 0.725, 0.003), YELLOW, on=["barrel", "chest", "neck", "fore"])


def convex(pts):
    """A mask of a convex polygon in (y, z), soft by a fraction of a voxel."""
    pts = np.array(pts, float)
    c = pts.mean(0)
    edges = []
    for a, b in zip(pts, np.roll(pts, -1, axis=0)):
        e = b - a
        n = np.array([e[1], -e[0]]); n /= np.linalg.norm(n)
        if np.dot(c - a, n) > 0:
            n = -n
        edges.append((a, n))
    def d(P):
        q = P[:, 1:3]
        return np.max([(q - a) @ n for a, n in edges], axis=0)
    return d


def spot(cy, cz, s, turn, seed):
    rs = np.random.RandomState(seed)
    pts = []
    for i, a in enumerate(np.linspace(0, 2 * math.pi, 5, endpoint=False)):
        r = s * (0.8 + 0.35 * rs.rand())
        pts.append((cy + r * math.cos(a + turn), cz + r * 1.15 * math.sin(a + turn)))
    return convex(pts)


SIDE = 0.03
BROWN_SPOTS = [(-0.198, 0.795, 0.036, 0.4, 1, 0.012), (-0.168, 0.69, 0.045, 0.9, 2, 0.012),
               (-0.145, 0.6, 0.05, 0.2, 3, SIDE), (-0.07, 0.55, 0.052, 1.1, 4, SIDE),
               (-0.005, 0.635, 0.036, 0.6, 5, SIDE)]
for cy, cz, s, t, seed, sx in BROWN_SPOTS:
    d = spot(cy, cz, s, t, seed)
    k.paint(lambda P, d=d, sx=sx: inside(d(P), 0.0025) * inside(sx - np.abs(P[:, 0]), 0.004), BROWN,
            on=["barrel", "chest", "neck"])
for cy, cz, s, t, seed in [(0.1, 0.6, 0.036, 0.3, 6), (0.165, 0.565, 0.032, 1.0, 7)]:
    d = spot(cy, cz, s, t, seed)
    k.paint(lambda P, d=d: inside(d(P), 0.0025) * inside(SIDE - np.abs(P[:, 0]), 0.004), YELLOW,
            on=["barrel", "rump"])

k.paint(lambda P: inside(P[:, 2] - 0.047, 0.0025), HOOF)

# --- the ears' pink linings, facing forward and out ----------------------------------------------------------------
k.decal(hp((0.078, -0.205, 0.966)), unit(0.25, -1, 0.2), (0.011, 0.027), PINK, eps=0.003, rings=6, segs=28,
        up=unit(1, 0.4, 0.2), mirror=True)

# --- the tail's face, looking back: a grin of white teeth -----------------------------------------------------------
CW, CDK = srgb("#F4F2EA"), srgb("#1A100C")


def grin(u, v):
    c = np.tile(CW, (len(u), 1))
    c[np.abs(v) < 0.12] = CDK
    for x in (-0.55, -0.18, 0.18, 0.55):
        c[np.abs(u - x) < 0.1] = CDK
    return c


gn = unit(0, 0.93, -0.36)
k.decal(TC + gn * TR, gn, (0.036, 0.015), grin, eps=0.004, rings=12, segs=60, up=(0, 0, 1),
        outline=lambda th: 1.0 / max(abs(math.cos(th)), abs(math.sin(th))) ** 0.7)

k.build(voxel=0.0065)

# --- the eyes: big, white, a dark blue iris with a black pupil and a glint, a dark lid line over the top ------------
CY, CWH, CIR, CP, CK = srgb(YELLOW), srgb("#FAFAF6"), srgb("#2C4FA8"), srgb("#101018"), srgb(INK)


def eye(u, v):
    c = np.tile(CWH, (len(u), 1))
    ir = ((u + 0.18) / 0.55) ** 2 + ((v + 0.02) / 0.68) ** 2
    c[ir < 1] = CIR
    c[((u + 0.18) / 0.26) ** 2 + ((v + 0.02) / 0.34) ** 2 < 1] = CP
    c[((u + 0.32) / 0.15) ** 2 + ((v - 0.2) / 0.15) ** 2 < 1] = (1, 1, 1)
    c[(u * u + v * v > 0.8 ** 2) & (v > 0.1)] = CK
    return c


de = unit(0.72, -0.6, 0.28)
E = hp((0.05, -0.272, 0.94))
# the tail head's eyes: yellow rings round a black middle, looking back
TY, TK = srgb("#F2C42A"), srgb("#120A08")


def teye(u, v):
    c = np.tile(TY, (len(u), 1))
    c[u * u + v * v < 0.45 ** 2] = TK
    return c


ten = unit(0.38, 0.85, 0.36)


def eyes(state):
    k.decal(E, de, (0.034, 0.037), eye_look(state, eye, CY, CK, line=0.14), eps=0.005, rings=20, segs=56,
            mirror=True)
    k.decal(TC + ten * TR, ten, (0.014, 0.015), eye_look(state, teye, srgb(TAILHEAD), TY, line=0.18), eps=0.004,
            rings=20, segs=56, mirror=True)


k.eye_states(eyes)
k.export(out)
