# Honchkrow sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/honchkrow.py -- <the export's Honchkrow.glb> <out.glb>
# Murkrow grown: the same navy, yellow beak and red-irised scowl, but a big round-chested boss. A small head under a
# fedora of feathers (a domed crown and a wide brim sweeping back to a long upturned point), a short hooked yellow beak,
# a great white fluffy beard from under the beak down the chest in ragged points, wings folded to the sides with red
# undersides, a fan of tail feathers ending in a spiky red rosette, short dark grey legs and feet with white claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

NAVY, DEEP, BEAK, RED, WHITE = "#24396C", "#1B2B55", "#F0CB5A", "#D8302E", "#F2F0EC"
LEG, CLAW = "#4C4C56", "#F4F2EE"

# --- the body: a big round chest, a short neck, a small head ---------------------------------------------------------
def plump(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.18 * t ** 1.5
    return V
k.add("body", ell((0, 0.01, 0.36), (0.178, 0.182, 0.218), rot=(-8, 0, 0), deform=plump), NAVY, "body")
k.add("thigh", ell((0.078, -0.005, 0.175), (0.066, 0.068, 0.07)), NAVY, "leg{s}", mirror=True)
H = np.array([0, -0.075, 0.615])
HR = np.array([0.085, 0.09, 0.085])
k.add("neck", ell((0, -0.04, 0.535), (0.115, 0.11, 0.08)), NAVY, "head")
k.add("head", ell(H, HR), NAVY, "head")

# The beard: white, from the cheeks under the beak, bulging forward over the chest and down to ragged points
k.add("cheeks", ell((0, -0.115, 0.555), (0.09, 0.075, 0.05)), WHITE, "head")
# one smooth mass, broad under the cheeks and narrowing down the chest
def taper(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.38 * t ** 1.3
    V[:, 1] *= 1 - 0.15 * t
    th = np.arctan2(V[:, 0], -V[:, 1])
    V *= (1 + 0.035 * np.abs(np.sin(4 * th + 1.3 * V[:, 2])))[:, None]          # fluffy, not an egg
    return V
k.add("beard", ell((0, -0.185, 0.44), (0.13, 0.1, 0.16), rot=(12, 0, 0), deform=taper), WHITE, "body")
# the ragged end: a few points hanging down out of the beard's bottom, the middle one longest
for i, (x, y, tx, tz, r) in enumerate([(0.0, -0.215, 0.0, 0.27, 0.04), (0.045, -0.205, 0.065, 0.3, 0.033),
                                        (-0.045, -0.205, -0.065, 0.3, 0.033), (0.08, -0.18, 0.115, 0.335, 0.028),
                                        (-0.08, -0.18, -0.115, 0.335, 0.028)]):
    b = np.array([x, y, 0.36 + 0.05 * abs(x)])
    t = np.array([tx, y - 0.01, tz])
    k.add(f"tuft{i}", blade([b, (b + t) / 2, t], [r, r * 0.7, 0.003], (0, 1, 0), 0.45), WHITE, "body", fuse=False)

# --- the hat: a domed crown and a wide brim sweeping back to an upturned point ----------------------------------------
def crease(V):
    V = V.copy()
    V[:, 2] -= 0.18 * np.exp(-(V[:, 0] / 0.3) ** 2) * np.clip(V[:, 2], 0, 1) ** 2
    return V
k.add("crown", ell((0, -0.055, 0.735), (0.09, 0.1, 0.075), rot=(-6, 0, 0), deform=crease), NAVY, "head", fuse=False)
k.add("band", ell((0, -0.055, 0.7), (0.093, 0.103, 0.02)), DEEP, "head", fuse=False)


def brim_at(u, v):
    u = 0.015 + 0.97 * u
    y = -0.21 + 0.56 * u
    w = 0.18 * np.sqrt(np.sin(math.pi / 2 * np.minimum(u / 0.38, 1))) * (1 - 0.97 * smoothstep(0.42, 1, u) ** 0.8)
    x = (2 * v - 1) * w
    z = (0.69 - 0.02 * (1 - smoothstep(0, 0.35, u)) + 0.03 * (2 * v - 1) ** 2 * (w / 0.18)
         + 0.045 * smoothstep(0.6, 1, u) ** 2)
    return np.stack([x, y, z], axis=-1)
k.add("brim", sheet(brim_at, 60, 21, 0.016), NAVY, "head", fuse=False)
# two loose feathers trailing off the brim's point
for s in (1, -1):
    b = np.array([0.006 * s, 0.3, 0.725])
    k.add(f"brimtip{s}", blade([b, b + (0.02 * s, 0.05, 0.012), b + (0.04 * s, 0.09, 0.03)], [0.012, 0.008, 0.002],
                               (0, 0, 1), 0.4), NAVY, "head", fuse=False)

# --- the beak: short, hooked down over a small lower bill ------------------------------------------------------------
k.add("beak", tube([(0, -0.145, 0.612), (0, -0.2, 0.61), (0, -0.238, 0.596), (0.003, -0.252, 0.573),
                    (0.005, -0.245, 0.553)], [0.04, 0.032, 0.022, 0.011, 0.003]), BEAK, "head", fuse=False,
      surface="shell")
k.add("jaw", ell((0, -0.19, 0.586), (0.028, 0.042, 0.013), rot=(10, 0, 0)), BEAK, "head", fuse=False, surface="shell")

# --- the wings: folded to the sides, navy over a red underside that shows along the lower edge ------------------------
WING = [(0.155, -0.04, 0.5), (0.178, 0.07, 0.445), (0.175, 0.2, 0.36), (0.125, 0.34, 0.27)]
k.add("wing", blade(WING, [0.085, 0.11, 0.085, 0.01], (1, 0, 0), 0.3), NAVY, "wing{s}", fuse=False, mirror=True)
k.add("wingred", blade([(x - 0.012, y + 0.005, z - 0.035) for x, y, z in WING], [0.075, 0.1, 0.075, 0.01], (1, 0, 0),
                       0.3), RED, "wing{s}", fuse=False, mirror=True)
for i, (y, z) in enumerate([(0.42, 0.29), (0.44, 0.25), (0.41, 0.21)]):
    b = np.array([0.15, 0.24, 0.31 - 0.03 * i])
    t = np.array([0.1, y, z])
    k.add(f"primary{i}", blade([b, (b + t) / 2, t], [0.035, 0.03, 0.003], (1, 0, 0), 0.3), DEEP if i % 2 else NAVY,
          "wing{s}", fuse=False, mirror=True)

# --- the tail: a fan of navy feathers ending in a spiky red rosette --------------------------------------------------
POM = np.array([0, 0.42, 0.4])
for i, a in enumerate(np.linspace(-1, 1, 5)):
    b = np.array([a * 0.03, 0.17, 0.33])
    t = POM + np.array([a * 0.06, -0.02, -0.03 - 0.02 * abs(a)])
    m = (b + t) / 2 + np.array([0, 0.0, -0.03])
    k.add(f"tail{i}", blade([b, m, t], [0.04, 0.045, 0.03], unit(0, -0.2, 1), 0.3), NAVY if i % 2 else DEEP, "tail",
          fuse=False)
k.add("pom", ell(POM, (0.06, 0.06, 0.06)), RED, "tail", fuse=False)
rs = np.random.RandomState(7)
for i in range(26):
    d = rs.normal(size=3); d[1] = abs(d[1]) * 0.8 + 0.2; d /= np.linalg.norm(d)
    b = POM + d * 0.04
    t = POM + d * (0.1 + 0.02 * rs.rand())
    side = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.9 else np.array([1.0, 0, 0])
    k.add(f"spike{i}", blade([b, (b + t) / 2, t], [0.026, 0.016, 0.002], side, 0.45), RED, "tail", fuse=False)

# --- the legs: short, dark grey, three toes forward and one back, white claws ----------------------------------------
for s, side in ((1, "R"), (-1, "L")):
    x = 0.075 * s
    k.add(f"leg{side}", tube([(x, -0.005, 0.2), (x * 1.04, -0.01, 0.11), (x * 1.07, -0.015, 0.035)],
                             [0.03, 0.025, 0.023]), LEG, f"leg{side}", fuse=False, surface="scales")
    base = np.array([x * 1.07, -0.02, 0.025])
    for j, ang in enumerate((-32, 0, 32, 180)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        tip = base + d * (0.085 if ang != 180 else 0.045) + np.array([0, 0, -0.012])
        k.add(f"toe{side}{j}", tube([base, (base + tip) / 2 + np.array([0, 0, 0.006]), tip], [0.019, 0.016, 0.013]),
              LEG, f"leg{side}", fuse=False, surface="scales")
        k.add(f"claw{side}{j}", tube([tip, tip + (d + np.array([0, 0, -0.55])) * 0.034], [0.011, 0.0015]), CLAW,
              f"leg{side}", fuse=False, surface="shell")

# --- the eyes: Murkrow's grown fierce: white with a red iris and a dark pupil, the lid heavy and slanting to the beak --
CW, CR, CN, CK, CP, CL = (srgb("#F8F6F2"), srgb("#D8302E"), srgb(NAVY), srgb("#0E1426"), srgb("#1A0E14"),
                          srgb("#9AA6C8"))


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.sqrt((u + 0.1) ** 2 + (v + 0.15) ** 2)
    c[r < 0.52] = CR
    c[r < 0.24] = CP
    c[np.sqrt((u - 0.05) ** 2 + (v + 0.02) ** 2) < 0.1] = CW
    lid = 0.22 + 0.32 * u
    c[v > lid] = CN
    c[np.abs(v - lid) < 0.1] = CK
    return c


de = unit(0.5, -0.84, 0.14)
E = H + de / np.linalg.norm(de / HR)


def eyes(state):
    k.decal(E, de, (0.03, 0.032), eye_look(state, eye, CN, CL), eps=0.006, rings=20, segs=56, mirror=True)


k.build(voxel=0.0068)
k.eye_states(eyes)
k.export(out)
