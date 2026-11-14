# Mothim sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mothim.py -- <the export's Mothim.glb> <out.glb>
#
# The male Burmy grown up: Burmy's dark grey head (a shade darker), its way of eyes (a round patch with a large dark
# pupil), but a moth. From the references: a rounded dark grey helmet of a head with a keel down its face to a point,
# great orange eye patches with a black pupil on its sides, two broad curved feelers standing up off the crown like
# pincers, white below and orange at the tip; an orange thorax, a dark grey abdomen ending in a black bow of two
# flaps; two white hooked legs at the thorax's sides and two white nubs under it; four wings: long pale yellow
# capsules held up and out, darker yellow at the root, a rust lobe at the root of each, four orange tabs fanned at
# each tip; the hind wings small yellow lobes held down and out, a rust lobe at the root, two orange tabs at each end.
# Wings are loose flat plates with their panels as thicker plates inside them, so a panel shows front and back
# (beautifly.py, dustox.py).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, leaf, srgb, inside, smoothstep, unit, eye_look, args, outward

src, out = args()
k = Kit(src, coat="fur")

GREY, DARK, WHITE = "#4A4B53", "#2C2C33", "#F2F2EE"
ORANGE, RUST, TAB = "#EF7A24", "#D4602A", "#F2802C"
YELLOW, ROOT = "#F6D77E", "#EBB44E"


# --- flat plates (beautifly.py's) ----------------------------------------------------------------------------------
def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def earclip(p):
    idx = list(range(len(p)))
    tris = []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    guard = 0
    while len(idx) > 3 and guard < 100000:
        guard += 1
        m = len(idx)
        cut = False
        for i in range(m):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % m]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            ok = True
            for j in idx:
                if j in (a, b, c):
                    continue
                q = p[j]
                if cross(p[a], p[b], q) >= 0 and cross(p[b], p[c], q) >= 0 and cross(p[c], p[a], q) >= 0:
                    ok = False
                    break
            if ok:
                tris.append((a, b, c)); idx.pop(i); cut = True
                break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin) with sharp edges that shade flat."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i + q for q in range(4)])
    return np.vstack([front, back, np.array(rim)]), F


def capsule(a0, a1, w0, w1, n=20):
    """A rounded plate from a0 to a1 in the wing's plane, half widths w0 at a0 and w1 at a1, round at both ends."""
    a0, a1 = np.array(a0, float), np.array(a1, float)
    d = a1 - a0; L = np.linalg.norm(d); d /= L
    side = np.array([-d[1], d[0]])
    pts = []
    for i in range(n + 1):                       # round end at a1
        t = -math.pi / 2 + math.pi * i / n
        pts.append(a1 + w1 * (d * math.cos(t) + side * math.sin(t)))
    for i in range(n + 1):                       # round end at a0
        t = math.pi / 2 + math.pi * i / n
        pts.append(a0 + w0 * (d * math.cos(t) + side * math.sin(t)))
    return np.array(pts)


def oval(c, a, b, turn=0.0, n=36):
    th = np.linspace(0, 2 * math.pi, n, endpoint=False)
    ct, st = math.cos(math.radians(turn)), math.sin(math.radians(turn))
    x, y = a * np.cos(th), b * np.sin(th)
    return np.stack([c[0] + x * ct - y * st, c[1] + x * st + y * ct], axis=1)


def rect(c, d, length, width):
    """A tab: a rectangle from c along direction d (in the plane), rounded a little at its far corners."""
    c, d = np.array(c, float), np.array(d, float); d /= np.linalg.norm(d)
    s = np.array([-d[1], d[0]])
    return np.array([c - s * width, c + d * length - s * width * 0.9, c + d * length + s * width * 0.9, c + s * width])


# --- the body ------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.012, 0.362])
HR = np.array([0.05, 0.044, 0.047])
k.add("head", ell(H, HR, deform=lambda V: V * (1 + 0.1 * np.clip(V[:, 2:3], 0, 1))), GREY, "head")
# The keel down the face to a point at the mouth
k.add("keel", ell((0, -0.047, 0.34), (0.012, 0.012, 0.032), rot=(-10, 0, 0),
                  deform=lambda V: V * np.array([1, 1, 1.0]) * (1 - 0.55 * np.clip(-V[:, 2:3], 0, 1))), GREY, "head")
k.add("neck", ell((0, 0.0, 0.31), (0.032, 0.03, 0.03)), GREY, "body")
# The orange thorax, the dark grey abdomen
k.add("thorax", ell((0, 0.0, 0.268), (0.044, 0.04, 0.042)), ORANGE, "body")
k.add("abdomen", ell((0, 0.008, 0.214), (0.028, 0.027, 0.035)), GREY, "body")
# A crisp line between the orange thorax and the grey above and below it
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.266) - 0.034, 0.0025) * inside(np.abs(P[:, 0]) - 0.06, 0.002), ORANGE)
k.paint(lambda P: inside(P[:, 2] - 0.232, 0.0025), GREY)
# The black bow at its end: two flaps spreading down and out
for s in (1, -1):
    k.add(f"bow{'R' if s > 0 else 'L'}", outward(leaf((0.004 * s, 0.006, 0.195), (0.75 * s, -0.15, -1.0), 0.055, 0.032,
                                             thick=0.012)), DARK, "body", fuse=False)

# Legs: a white hook at each side of the thorax, a white nub under it
k.add("hook", tube([(0.036, -0.022, 0.262), (0.042, -0.04, 0.236), (0.03, -0.05, 0.215), (0.02, -0.05, 0.207)],
                   [0.014, 0.013, 0.009, 0.004]), WHITE, "leg{s}", fuse=False, mirror=True)
k.add("nub", ell((0.012, -0.03, 0.192), (0.011, 0.011, 0.012)), WHITE, "leg{s}", fuse=False, mirror=True)

# The feelers: broad blades curving up off the crown, white at the root and orange at the tip
def feeler(s):
    pts = [(0.022 * s, -0.016, 0.398), (0.04 * s, -0.022, 0.423), (0.042 * s, -0.024, 0.445),
           (0.03 * s, -0.022, 0.462)]
    # The white blade ends inside the orange tip, which is a little wider where they meet, so the seam is clean
    mid = ((pts[1][0] + 2 * pts[2][0]) / 3, -0.023, 0.437)
    root = blade([pts[0], pts[1], mid], [0.011, 0.012, 0.0115], (0, 1, 0), 0.4)
    tip = blade([pts[2], ((pts[2][0] + pts[3][0]) / 2, -0.023, 0.455), pts[3]], [0.0145, 0.011, 0.003],
                (0, 1, 0), 0.42)
    return root, tip
for s, side in ((1, "R"), (-1, "L")):
    root, tip = feeler(s)
    k.add(f"feeler{side}", root, WHITE, f"ear{side}", fuse=False)
    k.add(f"feelertip{side}", tip, ORANGE, f"ear{side}", fuse=False)

# --- the wings -------------------------------------------------------------------------------------------------------
WO = np.array([0.045, 0.022, 0.291])
EX, EZ = (1.0, 0.18, 0.0), (0.0, 0.06, 1.0)
WL = WO + np.array([0, 0.008, 0.0])

# Forewing: a long pale capsule up and out, a darker root, a rust lobe, four tabs fanned at the tip
F0, F1 = np.array([0.02, 0.012]), np.array([0.235, 0.095])
k.add("fore", flat(capsule(F0, F1, 0.038, 0.058), WO, EX, EZ, 0.008), YELLOW, "wing{s}", fuse=False, mirror=True)
k.add("foreroot", flat(capsule(F0, F0 + (F1 - F0) * 0.32, 0.03, 0.045), WO, EX, EZ, 0.0105), ROOT, "wing{s}",
      fuse=False, mirror=True)
k.add("forerust", flat(oval((0.04, 0.03), 0.04, 0.03, turn=25), WO, EX, EZ, 0.013), RUST, "wing{s}", fuse=False,
      mirror=True)
fd = (F1 - F0) / np.linalg.norm(F1 - F0)
fs = np.array([-fd[1], fd[0]])
for i, ang in enumerate((-60, -20, 20, 62)):
    t = math.radians(ang)
    d = fd * math.cos(t) + fs * math.sin(t)
    c = F1 + d * 0.04
    k.add(f"foretab{i}", flat(rect(c, d, 0.034, 0.0085), WO, EX, EZ, 0.011), TAB, "wing{s}", fuse=False, mirror=True)

# Hind wing: a smaller lobe down and out, a rust lobe at its root, two tabs at its end
B0, B1 = np.array([0.015, -0.02]), np.array([0.08, -0.115])
k.add("hind", flat(capsule(B0, B1, 0.036, 0.048), WL, EX, EZ, 0.008), YELLOW, "wing{s}", fuse=False, mirror=True)
k.add("hindrust", flat(oval((0.03, -0.05), 0.038, 0.032, turn=-30), WL, EX, EZ, 0.0115), RUST, "wing{s}",
      fuse=False, mirror=True)
bd = (B1 - B0) / np.linalg.norm(B1 - B0)
bs = np.array([-bd[1], bd[0]])
for i, ang in enumerate((-35, 35)):
    t = math.radians(ang)
    d = bd * math.cos(t) + bs * math.sin(t)
    k.add(f"hindtab{i}", flat(rect(B1 + d * 0.03, d, 0.03, 0.0085), WL, EX, EZ, 0.011), TAB, "wing{s}",
          fuse=False, mirror=True)

# --- the eyes: orange patches with a large black pupil on the head's sides ----------------------------------------------
EYE, PUPIL = srgb(ORANGE), srgb("#141416")
LID = srgb(GREY)
LINE = srgb("#F2A25C")         # a light line for the closed states on the dark head
def eye(u, v):
    c = np.tile(EYE, (len(u), 1))
    c[((u + 0.05) / 0.48) ** 2 + (v / 0.6) ** 2 < 1] = PUPIL
    return c

def on_head(d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = H + d / math.sqrt(((d / HR) ** 2).sum())
    nrm = (p - H) / HR ** 2
    return p, nrm / np.linalg.norm(nrm)

EP, EN = on_head((0.66, -0.72, -0.05))
def eyes(state):
    ink = PUPIL if state in ("Open", "Fierce") else LINE
    k.decal(EP, EN, (0.021, 0.027), eye_look(state, eye, LID, ink), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.004, smooth=4)
k.eye_states(eyes)
k.export(out)
