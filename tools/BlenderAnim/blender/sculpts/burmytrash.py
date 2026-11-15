# Burmy (Trash Cloak) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/burmytrash.py -- <the export's Burmy-Trash.glb> <out.glb>
#
# Grown from Burmy's script (burmy.py), whose head, horn, nose and eyes are kept as they are made there, moved onto this
# form's own head joint (0.507 where the Plant Cloak's is 0.514), and made like burmysandy.py's cloak. The design, from
# the references: the same dark grey bagworm in a cloak of pink insulation, a rounded egg of soft lumps rising in a
# hood round the sides and back of the head, fine dark creases fanning out under the face, two bent white wires
# poking out low at the sides and a small pink tuft hanging under it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

GREY, GREY2 = "#5D5F67", "#686A73"                 # Burmy's head and horn
PINK, PINK2 = "#F2A2B6", "#EC97AD"                 # the insulation, and a deeper lump now and then
CREASE, TUFT, WIRE = "#C9758D", "#E58AA2", "#D9DDE4"
rs = np.random.RandomState(4122)
DZ = 0.507 - 0.514                                 # this form's head joint against the Plant Cloak's

# --- the head and its horn (burmy.py's, moved by DZ) -------------------------------------------------------------
H = np.array([0.0, -0.01, 0.53 + DZ])
HR = np.array([0.105, 0.095, 0.088])
k.add("head", ell(H, HR), GREY, "head", surface="shell")
for i in range(5):
    t = i / 4
    c = (0.004 * t ** 2 * 6, 0.006 * i, 0.605 + 0.046 * i + DZ)
    r = 0.074 - 0.0125 * i
    k.add(f"horn{i}", ell(c, (r, r, 0.031 - 0.002 * i), rot=(7 if i % 2 else -7, 6 if i % 2 else -4, 0)),
          GREY2 if i % 2 else GREY, "head", surface="shell")
k.add("horncore", tube([(0, 0.0, 0.58 + DZ), (0.01, 0.015, 0.72 + DZ), (0.022, 0.03, 0.8 + DZ)], [0.05, 0.03, 0.018]),
      GREY, "head", surface="shell")
k.add("horntip", tube([(0.022, 0.03, 0.79 + DZ), (0.03, 0.04, 0.83 + DZ), (0.04, 0.05, 0.855 + DZ)],
                      [0.02, 0.011, 0.003]), GREY, "head", surface="shell")
def cone(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.75 * t; V[:, 1] *= 1 - 0.75 * t
    return V
k.add("nose", ell((0, -0.092, 0.495 + DZ), (0.022, 0.02, 0.036), rot=(-28, 0, 0), deform=cone), GREY, "head",
      surface="shell")

# --- the cloak: an egg of insulation, its top front pushed back so the face looks out of a hood ----------------------
EC = np.array([0.0, 0.03, 0.35])                   # the egg's middle
ER = np.array([0.165, 0.15, 0.255])                # its radii: from 0.095 up to beside the head's crown

def hollow(V):
    """The front of the egg's top falls back behind the face, its sides left standing as the hood."""
    V = V.copy()
    front = np.clip(-V[:, 1], 0, 1) * (1 - np.clip(np.abs(V[:, 0]) / 0.85, 0, 1) ** 2)
    V[:, 1] *= 1 - 0.85 * smoothstep(0.1, 0.5, V[:, 2]) * front
    return V
k.add("cloak", ell(EC, ER, deform=hollow), PINK, "body")

def clear(p):
    """Whether a point is where the face looks out (no lump grows there)."""
    return p[1] < 0.02 and p[2] > H[2] - 0.085 and abs(p[0]) < 0.115

# Soft lumps over the egg, larger and lower than the sand's clods
golden = math.pi * (3 - math.sqrt(5))
COUNT = 42
for i in range(COUNT):
    u = -0.9 + 1.82 * (i + 0.5) / COUNT
    ring = math.sqrt(1 - u * u)
    a = i * golden
    d = np.array([math.sin(a) * ring, -math.cos(a) * ring, u])
    p = EC + d * ER
    if clear(p):
        continue
    o = d / ER; o /= np.linalg.norm(o)
    size = 0.066 * rs.uniform(0.88, 1.12)
    k.add(f"lump{i}", ell(p - o * size * 0.55, (size * 1.1, size * 1.1, size)), PINK2 if i % 5 == 0 else PINK,
          "body")

# The hood: lumps round the sides and back of the head, set back from the face so the eyes stay on grey
for i, t in enumerate(np.linspace(-1.0, 1.0, 8)):
    for s in (1, -1):
        ang = math.radians(48 + 60 * (t + 1) / 2)
        p = H + np.array([0.125 * s * math.sin(ang), 0.035, -0.125 * math.cos(ang) + 0.035])
        size = 0.048 * rs.uniform(0.9, 1.1)
        k.add(f"hood{i}{'R' if s > 0 else 'L'}", ell(p, (size, size, size)), PINK, "body")
for i, a in enumerate(np.linspace(-2.2, 2.2, 6)):
    p = H + np.array([0.115 * math.sin(a + math.pi), 0.085 + 0.03 * abs(math.cos(a)), 0.015 - 0.03 * abs(a) / 2.2])
    k.add(f"nape{i}", ell(p, (0.054, 0.054, 0.054)), PINK, "body")

# The tuft under it: a little gathered knot and four short lobes hanging out of it
k.add("knot", ell((0, 0.02, 0.1), (0.035, 0.035, 0.03)), TUFT, "body")
for i in range(4):
    a = math.pi / 4 + i * math.pi / 2
    o = np.array([math.cos(a), math.sin(a), 0.0])
    k.add(f"tuft{i}", ell((0.025 * o[0], 0.02 + 0.025 * o[1], 0.06), (0.016, 0.016, 0.034),
                          rot=(-25 * o[1], 25 * o[0], 0)), TUFT, "body", fuse=False, surface="cloth")

# The two wires: out of the cloak low at the sides, bent down and out again
for s in (1, -1):
    k.add(f"wire{'R' if s > 0 else 'L'}",
          tube([(0.12 * s, -0.05, 0.255), (0.165 * s, -0.065, 0.25), (0.175 * s, -0.07, 0.205), (0.205 * s, -0.075, 0.19)],
               [0.013, 0.013, 0.013, 0.013], seg=14), WIRE, "body", fuse=False, surface="metal")

# Fine creases fanning out under the face, each side: dark lines in the insulation's front
CR = np.array([0.0, -0.1, H[2] - 0.075])          # where they fan from, under the chin
def creases(P):
    x = np.abs(P[:, 0]); dz = P[:, 2] - CR[2]
    r = np.hypot(x, dz)
    phi = np.arctan2(-dz, x)                       # nought out to the side, rising as the line droops
    lines = np.abs(phi * 9 / math.pi - np.round(phi * 9 / math.pi))
    m = inside(lines - 0.1, 0.03) * inside(0.05 - r, 0.004) * inside(r - 0.135, 0.006)
    m = m * (phi > 0.2) * (phi < 1.25) * (P[:, 1] < -0.06) * (x > 0.025)
    return m
k.paint(creases, CREASE)

# --- the eyes: Burmy's own, yellow rounds rimmed in black with a small black pupil -----------------------------------
YELLOW, PUPIL, INK = srgb("#F4D23A"), srgb("#141416"), srgb("#141416")
def eye(u, v):
    c = np.tile(YELLOW, (len(u), 1))
    c[(u / 0.26) ** 2 + (v / 0.26) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.66] = INK
    return c
LID = srgb(GREY)

def on_head(d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = H + d / math.sqrt(((d / HR) ** 2).sum())
    nrm = (p - H) / HR ** 2
    return p, nrm / np.linalg.norm(nrm)

EP, EN = on_head((0.44, -0.88, 0.16))
def eyes(state):
    k.decal(EP, EN, (0.022, 0.022), eye_look(state, eye, LID, INK), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.006, smooth=7, band=0.007)
k.eye_states(eyes)
k.export(out)
