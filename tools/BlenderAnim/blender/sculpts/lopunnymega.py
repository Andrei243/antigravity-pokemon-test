# Mega Lopunny sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lopunnymega.py -- <the export's Lopunny-Mega.glb> <out.glb>
# Lopunny's script (which builds as it is read) copied and changed where the pictures change it: the same head, eyes,
# pink nose, brown arms and round brown tail. What the Mega changes, largest first: the long ears are its great mass,
# each a chain of three thick brown lengths with a ring of cream fleece at each joint, flying out from the head's sides
# and back to a point at hip height well away from the body; the ears' cream tops rise over the crown in a V tied in a
# knot on the brow; the body from the bust down and the powerful legs are dark, as tights, with reddish-brown patches on
# hips and knees; the wrists' fleece is smaller and the hands are fists with dark claws held up in a guard, the right a
# little higher; the ankles' fleece is a ring over cream feet set a little apart. The head is Lopunny's a fifth larger.
# The ears are a mirror pair (the art throws one ear out to the side and loops the other down).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, boxy, eye_look, polygon, args, outward, hull

src, out = args()
k = Kit(src, coat="fur")

BROWN, CREAM, PINK, INK = "#A5674A", "#F3DE98", "#E8909E", "#1A1414"
TIGHTS, PATCH, CLAWC = "#3A2B29", "#B5604A", "#2A2020"
H = np.array([0, -0.01, 0.83])           # the head's centre: Lopunny's head a fifth larger
HR = np.array([0.11, 0.1, 0.104])


def puff(name, c, r, bone, n=8, tilt=0.0, squash=(1, 1, 1), col=CREAM):
    """A cloud of fleece: a core and fused balls round it (Lopunny's)."""
    c = np.array(c, float); sq = np.array(squash, float)
    k.add(name, ell(c, np.array((r * 0.78, r * 0.74, r * 0.74)) * sq), col, bone)
    g = math.pi * (3 - math.sqrt(5))
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        rr = math.sqrt(1 - z * z)
        a = g * i + tilt
        d = np.array([rr * math.cos(a), rr * math.sin(a), z * 0.8]) * sq
        k.add(f"{name}{i}", ell(c + d * r * 0.48, np.array((r * 0.5, r * 0.5, r * 0.48)) * sq), col, bone)


def fleece_ring(name, c, axis, r, bone, n=9):
    """A ring of cream fleece round a limb or an ear: a squat core and balls round the axis."""
    c = np.array(c, float); a = np.array(axis, float); a /= np.linalg.norm(a)
    e1 = np.cross(a, (0, 1.0, 0)) if abs(a[1]) < 0.9 else np.cross(a, (1.0, 0, 0))
    e1 /= np.linalg.norm(e1); e2 = np.cross(a, e1)
    for i in range(n):
        t = 2 * math.pi * i / n
        p = c + r * 0.78 * (math.cos(t) * e1 + math.sin(t) * e2) + a * r * 0.12 * ((i % 2) - 0.5)
        k.add(f"{name}{i}", ell(p, (r * 0.46, r * 0.46, r * 0.46)), CREAM, bone)


# The head: Lopunny's
def chin(V):
    V = boxy(2.2)(V)
    V = V.copy()
    V[:, 0] *= 1 - 0.14 * np.clip(-V[:, 2], 0, 1)
    return V
k.add("head", ell(H, HR, deform=chin), BROWN, "head")
k.add("muzzle", ell((0, -0.06, 0.8), (0.06, 0.055, 0.042)), BROWN, "head")
k.add("neck", tube([(0, 0.0, 0.66), (0, -0.004, 0.75)], [0.046, 0.042]), BROWN, "head")

# The body: a brown bust, the rest in dark tights, a narrow waist and round hips
k.add("chest", ell((0, 0.005, 0.625), (0.082, 0.064, 0.077)), BROWN, "body")
k.add("waist", ell((0, 0.01, 0.54), (0.06, 0.052, 0.062)), TIGHTS, "body")
k.add("hips", ell((0, 0.012, 0.465), (0.115, 0.082, 0.068)), TIGHTS, "body")
# the tights come up to a line across the bust, dipping a little in the middle
k.paint(lambda P: inside(P[:, 2] - (0.6 + 0.022 * np.abs(P[:, 0]) / 0.08), 0.0025) * inside(np.abs(P[:, 0]) - 0.1, 0.003)
        * inside(P[:, 2] - 0.66, 0.003), TIGHTS, on=["chest"])

# Powerful legs in tights, set a little apart for a fighter's stance: thighs as thick as the waist at the hip,
# tapering to the knee, strong calves; a ring of cream fleece at each ankle low over pointed cream feet
k.add("leg", tube([(0.075, 0.01, 0.47), (0.09, 0.0, 0.37), (0.1, -0.012, 0.275), (0.108, -0.004, 0.19),
                   (0.112, 0.0, 0.115), (0.112, 0.0, 0.075)], [0.08, 0.072, 0.05, 0.054, 0.041, 0.037]),
      TIGHTS, "leg{s}", mirror=True)
for s, side in ((1, "R"), (-1, "L")):
    fleece_ring(f"anklefleece{side}", (0.112 * s, 0.0, 0.082), (0, 0, 1), 0.062, f"leg{side}")
def toe(V):
    V = V.copy()
    f = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.55 * f ** 1.5; V[:, 2] *= 1 - 0.4 * f ** 1.5
    return V
k.add("foot", ell((0.112, -0.045, 0.033), (0.044, 0.082, 0.036), deform=toe), CREAM, "leg{s}", mirror=True)
# the reddish-brown patches on hips and knees
for c, r in (((0.13, -0.02, 0.47), (0.045, 0.055, 0.04)), ((0.1, -0.055, 0.27), (0.036, 0.04, 0.032))):
    cc, rr = np.array(c), np.array(r)
    def spot(P, cc=cc, rr=rr):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        return inside(np.linalg.norm((Q - cc) / rr, axis=1) - 1, 0.06)
    k.paint(spot, PATCH, on=["leg", "hips", "waist"])

# Arms bent to hold the fists before the chest, a smaller ring of fleece at each wrist
# (the model's right fist, +X, held a little higher than the left: a fighter's guard)
for s, side in ((1, "R"), (-1, "L")):
    up = 0.04 if s > 0 else 0.0
    M = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube([np.array(p) * M for p in ((0.085, -0.005, 0.655), (0.115, -0.025, 0.6 + up * 0.4),
                                                        (0.1, -0.06, 0.57 + up), (0.07, -0.095, 0.59 + up))],
                             [0.031, 0.028, 0.026, 0.025]), BROWN, f"arm{side}")
    puff(f"cuff{side}", (0.075 * s, -0.09, 0.585 + up), 0.046, f"arm{side}", n=8, tilt=0.4 * s)
    # the fists: round and brown, three dark claws curled over the front
    k.add(f"fist{side}", ell((0.05 * s, -0.135, 0.605 + up), (0.033, 0.03, 0.032)), BROWN, f"arm{side}", fuse=False)
    for i, x in enumerate((0.03, 0.05, 0.07)):
        root = np.array([x * s, -0.16, 0.618 + up])
        k.add(f"claw{side}{i}", hull([root + (0.008, 0, 0), root - (0.008, 0, 0), root + (0, 0.008, 0.006),
                                      root + (0, -0.004, -0.03)]), CLAWC, f"arm{side}", fuse=False, surface="shell")

# A small round tail
k.add("tail", ell((0, 0.095, 0.47), (0.042, 0.038, 0.038)), BROWN, "tail")

# The ears' cream tops: two broad petals rising over the crown in a V, a knot of cream on the brow where they cross
def petal(V):
    V = V.copy()
    t = (V[:, 2] + 1) / 2
    V[:, 0] *= 0.55 + 0.75 * np.sin(np.pi * np.clip(t, 0, 1) ** 0.8)
    return V
k.add("petal", outward(ell((0.062, 0.005, 1.01), (0.046, 0.022, 0.105), rot=(-12, -26, 0), deform=petal)), CREAM,
      "ear{s}", fuse=False, mirror=True)
k.add("knot", outward(ell((0, -0.085, 0.905), (0.04, 0.024, 0.026), rot=(10, 0, 0))), CREAM, "head", fuse=False)
k.add("knotside", outward(ell((0.04, -0.07, 0.912), (0.033, 0.022, 0.024), rot=(10, 0, -30))), CREAM, "head",
      fuse=False, mirror=True)

# The long ears fly out: each rises from the head's side, goes out and back at some 40 degrees from the body's side
# and ends in a point at hip height a body's width out from the hips, behind the body's plane (so the arms and fists
# stay in view and both cameras see daylight between ear and body); three thick brown lengths, flatter across the
# ear's own plane, with a ring of fleece at each joint
SEG1 = [(0.085, 0.02, 0.88), (0.14, 0.035, 0.94), (0.2, 0.055, 0.94), (0.245, 0.075, 0.895)]
SEG2 = [(0.275, 0.09, 0.82), (0.31, 0.105, 0.74), (0.345, 0.12, 0.66), (0.37, 0.13, 0.6)]
SEG3 = [(0.395, 0.14, 0.53), (0.415, 0.15, 0.48), (0.43, 0.155, 0.44), (0.44, 0.158, 0.41)]
FLAT = unit(-0.25, 1, 0)                    # the ears are flattened across the plane they swing in
k.add("ear1", blade(SEG1, [0.044, 0.056, 0.058, 0.05], FLAT, 0.62), BROWN, "ear{s}", mirror=True)
k.add("ear2", blade(SEG2, [0.05, 0.07, 0.066, 0.048], FLAT, 0.62), BROWN, "ear{s}", mirror=True)
k.add("ear3", blade(SEG3, [0.046, 0.05, 0.034, 0.006], FLAT, 0.62), BROWN, "ear{s}", mirror=True)
for s, side in ((1, "R"), (-1, "L")):
    fleece_ring(f"earfleeceA{side}", (0.26 * s, 0.082, 0.86), (0.5 * s, 0.2, -1), 0.074, f"ear{side}")
    fleece_ring(f"earfleeceB{side}", (0.383 * s, 0.135, 0.565), (0.4 * s, 0.15, -1), 0.074, f"ear{side}")

# A small pink nose and a small calm mouth (Lopunny's)
NOSE = polygon([(-1, 0.7), (1, 0.7), (0, -1)])
k.decal((0, -0.112, 0.81), unit(0, -1, -0.15), (0.011, 0.009), PINK, eps=0.004, outline=NOSE, rings=6, segs=24)
CB, CI = srgb(BROWN), srgb(INK)
CM = srgb("#4A2418")
def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.1 + 0.45 * u * u)) < 0.22) & (np.abs(u) < 0.8)] = CM
    return c
k.decal((0, -0.11, 0.785), unit(0, -1, -0.35), (0.021, 0.013), mouth, eps=0.004, rings=8, segs=36)

# The eyes: Lopunny's, pink-red under a dark lid with a flick at the outer corner, a little larger
IRIS, IRISD, PUP, SCL = srgb("#E5506E"), srgb("#A02642"), srgb("#38101A"), srgb("#FBEFF0")
def lidline(u):
    return 0.3 - 0.32 * u * u
def eye(u, v):
    c = np.tile(CB, (len(u), 1))
    e = (u / 0.64) ** 2 + ((v + 0.1) / 0.58) ** 2
    inn = (e < 1) & (v < lidline(u))
    c[inn] = SCL
    ir = ((u + 0.03) / 0.5) ** 2 + ((v + 0.1) / 0.56) ** 2
    c[inn & (ir < 1)] = IRISD
    c[inn & (ir < 0.72)] = IRIS
    c[inn & (((u + 0.03) / 0.17) ** 2 + ((v + 0.12) / 0.26) ** 2 < 1)] = PUP
    c[inn & (((u + 0.2) / 0.13) ** 2 + ((v - 0.06) / 0.13) ** 2 < 1)] = (1, 1, 1)
    c[(e < 1) & (e > 0.8) & (v < -0.1)] = CI
    c[(np.abs(v - lidline(u) - 0.03) < 0.1) & (np.abs(u) < 0.64)] = CI
    wing = (u > 0.5) & (u < 0.9)
    t = (u - 0.5) / 0.4
    c[wing & (np.abs(v - (lidline(0.5) + 0.04 + 0.16 * t)) < 0.07 * (1 - t) + 0.02)] = CI
    return c
de = unit(0.42, -0.9, 0.12)
E = H + de * 0.1
def eyes(state):
    k.decal(E, de, (0.05, 0.05), eye_look(state, eye, CB, CI), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
