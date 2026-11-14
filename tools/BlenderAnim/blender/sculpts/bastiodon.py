# Bastiodon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bastiodon.py -- <the export's Bastiodon.glb> <out.glb>
# Grown from shieldon.py: the same tan body and grey armour, the same round eyes with a dark rim and a solid pupil.
#
# The design, from the references: a big low quadruped whose face is a tall, flat, upright grey wall: its top edge
# battlemented, a pale cone horn on each top corner, four arched yellow windows in dark frames (two a side, one over
# the other), a long nose ridge running down its middle and swelling at the foot, a white tusk out to each side from
# the ridge's foot, and four white pegs hanging from the wall's lower edge. Under the wall a grey face: yellow eyes,
# half lidded, looking out between the pegs, and a thin red mouth. A yellow body with a grey back that runs down its
# sides in jagged stripes, a row of pale spikes along the spine, a grey pointed tail, thick grey legs with white claws.
# (The eyes are not on the wall: they look out from under it.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, boxy, tri, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

TAN, WALL, FACE, HORN, IVORY, FRAME, WINDOW = \
    "#F2D58A", "#64666C", "#5C5E64", "#CDCED2", "#E6E7EA", "#26262A", "#F2D46A"
INK, MOUTH = "#141416", "#A8424A"


def slab(outline, c, right, up, thick):
    """A flat piece with hard edges: a polygon (x, z in the plane), star-shaped about its middle, given a thickness.
    The front faces cross(right, up); each face keeps its own points, so the edges stay sharp."""
    right, up = np.array(right, float), np.array(up, float)
    n = np.cross(right, up); n /= np.linalg.norm(n)
    o = np.array(outline, float)
    m = o.mean(axis=0)
    P = np.array([c + right * x + up * z for x, z in o])
    M = c + right * m[0] + up * m[1]
    fr, bk = P + n * thick / 2, P - n * thick / 2
    V, F = [], []
    k_ = len(o)
    # front: a fan, counter-clockwise seen from the front
    base = len(V); V.extend(fr.tolist()); V.append((M + n * thick / 2).tolist())
    for i in range(k_):
        F.append([base + k_, base + i, base + (i + 1) % k_])
    base = len(V); V.extend(bk.tolist()); V.append((M - n * thick / 2).tolist())
    for i in range(k_):
        F.append([base + k_, base + (i + 1) % k_, base + i])
    for i in range(k_):
        j = (i + 1) % k_
        base = len(V)
        V.extend([fr[i].tolist(), bk[i].tolist(), bk[j].tolist(), fr[j].tolist()])
        F.append([base, base + 1, base + 2, base + 3])
    return np.array(V), F


# --- the body ----------------------------------------------------------------------------------------------------
k.add("body", ell((0, 0.15, 0.28), (0.245, 0.26, 0.175)), TAN, "body")
k.add("shoulder", ell((0, -0.02, 0.32), (0.21, 0.12, 0.14)), TAN, "body")
k.add("head", ell((0, -0.16, 0.36), (0.22, 0.13, 0.12)), FACE, "head")
k.add("tail", tube([(0, 0.36, 0.29), (0, 0.47, 0.255), (0, 0.56, 0.21)], [0.085, 0.05, 0.008]), WALL, "tail")

# The grey back, running down the sides in jagged stripes
def back(P):
    th = np.arctan2(np.abs(P[:, 0]), P[:, 2] - 0.3)
    return inside(0.395 + 0.06 * tri(P[:, 1] * 6.5) - P[:, 2], 0.004) * (P[:, 1] > -0.07)
k.paint(back, WALL, on=["body", "shoulder"])


# The legs' grey ends at a jagged line across their tops: yellow above it, grey below, both crisp
def legline(P):
    return 0.17 + 0.022 * tri(np.arctan2(P[:, 1] - np.where(P[:, 1] < 0.14, -0.02, 0.3), np.abs(P[:, 0]) - 0.17) * 1.3)


k.paint(lambda P: inside(legline(P) - P[:, 2], 0.0035) * (np.abs(P[:, 0]) > 0.085) * (P[:, 2] < 0.3), TAN,
        on=["leg", "hind"])
k.paint(lambda P: inside(P[:, 2] - legline(P), 0.0035) * (np.abs(P[:, 0]) > 0.085), WALL)

# The legs: thick, grey; two white claws at the front of each foot
for name, y, bone in (("leg", -0.02, "leg{s}"), ("hind", 0.3, "hind{s}")):
    k.add(name, tube([(0.165, y, 0.22), (0.18, y - 0.01, 0.085)], [0.085, 0.08]), WALL, bone, mirror=True)
    k.add(name + "foot", ell((0.18, y - 0.03, 0.038), (0.076, 0.085, 0.04)), WALL, bone, mirror=True)
    for j, dx in enumerate((-0.03, 0.03)):
        k.add(f"{name}claw{j}", tube([(0.18 + dx, y - 0.09, 0.045), (0.18 + dx * 1.2, y - 0.125, 0.022),
                                      (0.18 + dx * 1.3, y - 0.145, 0.0)], [0.02, 0.012, 0.002]), IVORY, bone,
              fuse=False, mirror=True, surface="shell")

# The pale spikes along the spine
for i, y in enumerate((0.02, 0.11, 0.2, 0.29, 0.37)):
    top = 0.28 + 0.175 * math.sqrt(max(1 - ((y - 0.15) / 0.26) ** 2, 0))
    k.add(f"spine{i}", tube([(0, y, top - 0.02), (0, y + 0.015, top + 0.03), (0, y + 0.035, top + 0.065)],
                            [0.035, 0.02, 0.003]), HORN, "body", fuse=False, surface="shell")

# --- the wall of its face ------------------------------------------------------------------------------------------
WY = -0.232                                       # the wall's middle, front to back
WT = 0.09
OUT = [(-0.25, 0.345), (-0.215, 0.39), (0.215, 0.39), (0.25, 0.345),
       (0.258, 0.56), (0.25, 0.79),
       (0.2, 0.805), (0.16, 0.775), (0.12, 0.818), (0.08, 0.782), (0.04, 0.825), (0.0, 0.788),
       (-0.04, 0.825), (-0.08, 0.782), (-0.12, 0.818), (-0.16, 0.775), (-0.2, 0.805),
       (-0.25, 0.79), (-0.258, 0.56)]
k.add("wall", slab(OUT, np.array([0, WY, 0]), (1, 0, 0), (0, 0, 1), WT), WALL, "head", fuse=False, surface="shell")
FRONTY = WY - WT / 2

# Two pale horns on its top corners
for s in (1, -1):
    k.add("horn" + ("R" if s > 0 else "L"), tube([(s * 0.195, WY, 0.795), (s * 0.21, WY, 0.85), (s * 0.228, WY, 0.905)],
                                                  [0.042, 0.026, 0.004]), HORN, "head", fuse=False, surface="shell")

# Four arched windows, a dark frame and a yellow pane, two a side, one over the other
def arch(w, h, n=14):
    pts = [(-w, -h), (w, -h)]
    for a in np.linspace(0, math.pi, n):
        pts.append((w * math.cos(a), (h - w) + w * math.sin(a)))
    return pts


for x in (0.115, -0.115):
    for z in (0.69, 0.565):
        fr = arch(0.036, 0.042)
        pn = arch(0.024, 0.031)
        k.add("frame", slab([(px + x, pz + z) for px, pz in fr], np.array([0, FRONTY - 0.003, 0]), (1, 0, 0), (0, 0, 1),
                            0.008), FRAME, "head", fuse=False, surface="shell")
        k.add("pane", slab([(px + x, pz + z - 0.004) for px, pz in pn], np.array([0, FRONTY - 0.008, 0]), (1, 0, 0),
                           (0, 0, 1), 0.006), WINDOW, "head", fuse=False, surface="shell")

# The nose ridge down its middle, swelling at its foot
k.add("ridge", tube([(0, FRONTY + 0.01, 0.67), (0, FRONTY - 0.025, 0.53), (0, FRONTY - 0.045, 0.41),
                     (0, FRONTY - 0.04, 0.34)], [0.02, 0.042, 0.055, 0.045], seg=20, per=6), WALL, "head",
      fuse=False, surface="shell")

# The tusks out to the sides from the ridge's foot, and the pegs hanging from the wall's lower edge
for s in (1, -1):
    k.add("tusk" + ("R" if s > 0 else "L"),
          tube([(s * 0.05, -0.305, 0.425), (s * 0.18, -0.315, 0.432), (s * 0.33, -0.305, 0.448)],
               [0.034, 0.026, 0.005], seg=18, per=6), IVORY, "head", fuse=False, surface="shell")
    for x, r in ((0.235, (0.027, 0.026, 0.034)), (0.057, (0.022, 0.024, 0.032))):
        k.add("peg", ell((s * x, FRONTY - 0.005, 0.362), r, deform=boxy(3)), IVORY, "head", fuse=False,
              surface="shell")

# The mouth: a thin red line under the nose, across the face
def mouth(u, v):
    return np.tile(srgb(MOUTH), (len(u), 1))
k.decal((0, -0.25, 0.272), unit(0, -1, -0.3), (0.075, 0.01), mouth, eps=0.004, rings=6, segs=40)

k.build(voxel=0.0065, smooth=3)


# --- the eyes: yellow, half lidded, an upright black pupil a little inward, a dark rim -----------------------------
CY, CI, LID = srgb(WINDOW), srgb(INK), srgb(FACE)


def eye(u, v):
    c = np.tile(CY, (len(u), 1))
    c[u * u + v * v > 0.825 ** 2] = CI            # the rim's edge between two rings of the patch
    lid = v > 0.3 - 0.1 * u
    c[lid] = LID
    c[np.abs(v - (0.3 - 0.1 * u)) < 0.14] = CI
    return c


EH = (0.05, 0.049)


def eyes(state):
    for s in (1, -1):
        e = np.array([s * 0.143, -0.25, 0.33])
        n = unit(s * 0.4, -1, 0.22)
        look = eye_look(state, eye, LID, CI, line=0.16)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(e, n, EH, lk, eps=0.004, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            # the upright pupil, a patch of its own so that its edge is round, under the lid's line
            right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
            up = np.cross(n, right)
            if state == "Open":
                pc, ph = e - s * right * 0.006 - up * 0.01, (0.014, 0.02)
            else:
                pc, ph = e - s * right * 0.004 - up * 0.018, (0.013, 0.013)
            k.decal(pc, n, ph, INK, eps=0.0065, rings=10, segs=36)


k.eye_states(eyes)
k.export(out)
