# Palariuta (No. 10099, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/palariuta.py -- <the export's Palariuta.glb> <out.glb>
#
# From its picture (10099-Palariuta.jpg) and its brief (CP-dex-2b-mountains-more.md, 99): a small fly agaric. Largest
# first: the cap, a round scarlet dome some 0.9 of the height across and half of it tall, its rim curling down at the
# sides, a dozen white spots of two sizes over it (carried round the back), pale cream gills under its rim with faint
# radial lines; a short fat cream stalk that is the body, some 0.43 of the height across; the cap's shadow a dark band
# round the stalk's top, and in it two big amber eyes with dark upright pupils and a highlight; a collar of lumpy moss
# round the stalk's lower half (the brief's moss it sits in, the picture's ring of it, kept as part of the creature);
# two little cream feet under the moss. The picture's drifting spores are dropped. The cap's front is tipped up so its
# rim stands clear of the eyes from the game's front camera.
#
# Written to be grown from (muscarita.py imports it with sibling("palariuta")): the palette, the cap with its curl,
# spots and gills, the moss and the amber eye are functions here.
# Skeleton: body (0, 0, 0.16), head (0, 0, 0.26), legs (+-0.05, 0, 0.05).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, lumpy, eye_look, args

# --- the line's colours -----------------------------------------------------------------------------------------
RED, RED_D, SPOT = "#C8362E", "#A02824", "#F6EEE2"
GILL, GILL_D = "#EFE3CA", "#D6C6A6"
STALK, BAND = "#F1E6CC", "#3A2228"
AMBER, AMBER_D, PUPIL, WHITE = "#F2A43A", "#C66E1E", "#2A1610", "#FFF6E4"
MOSS, MOSS_D = "#7A9A44", "#5B7C32"

# --- Palariuta's frame ------------------------------------------------------------------------------------------
CAPC = np.array([0.0, 0.01, 0.3])
CAPR = np.array([0.2, 0.19, 0.14])
CAPROT = (-12.0, 0.0, 0.0)               # the front tipped up: the rim stands over the eyes
STALKC = np.array([0.0, 0.0, 0.15])
STALKR = np.array([0.1, 0.094, 0.15])
EYE = np.array([0.035, -0.084, 0.228])
EYE_HALF = (0.026, 0.03)
VOXEL = 0.0042


def rotm(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def capshape(under=0.3, curl=0.35):
    """The unit sphere made a toadstool's cap: a shallow underside, the rim curling down at its edge."""
    def f(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * under, V[:, 2])
        rh = np.hypot(V[:, 0], V[:, 1])
        V[:, 2] -= curl * rh ** 6
        return V
    return f


def toadstool(k, C, R, rot, bone, spots, seed, under=0.3, curl=0.35, stem_r=0.0, lines=16, spot_size=0.13):
    """The fly agaric's cap: a scarlet dome curling down at its rim, white spots, cream gills with faint radial lines
    under it (all painted in the cap's own frame)."""
    C, R = np.array(C, float), np.array(R, float)
    k.add("cap", ell(C, R, rot=rot, deform=capshape(under, curl), sub=5), RED, bone, surface="shell")
    m = rotm(rot)
    def frame(P):
        q = (P - C) @ m
        rh = np.hypot(q[:, 0] / R[0], q[:, 1] / R[1])
        return q, rh
    def gills(P):
        q, rh = frame(P)
        curve = -curl * R[2] * np.clip(rh, 0, 1) ** 6
        return inside(q[:, 2] - curve + 0.0035, 0.0015) * smoothstep(stem_r * 0.9, stem_r * 1.08,
                                                                       np.hypot(q[:, 0], q[:, 1]))
    def gill_lines(P):
        q, rh = frame(P)
        th = np.arctan2(q[:, 1], q[:, 0])
        d = np.abs(((th * lines / (2 * math.pi)) % 1) - 0.5) * 2 * math.pi / lines * np.hypot(q[:, 0], q[:, 1])
        return gills(P) * inside(d - 0.0035, 0.0015) * smoothstep(0.45, 0.6, rh)
    k.paint(gills, GILL, on=["cap"])
    k.paint(gill_lines, GILL_D, on=["cap"])
    # the spots: a golden spiral over the dome, two sizes, kept off the rim's curl
    golden = math.pi * (3 - math.sqrt(5))
    rs = np.random.RandomState(seed)
    for i in range(spots):
        y = 0.97 - 0.78 * (i + 0.5) / spots
        ring = math.sqrt(1 - y * y); a = i * golden + 0.3 * rs.rand()
        d = np.array([math.cos(a) * ring, math.sin(a) * ring, y])
        p = m @ (d * R) + C
        sz = (spot_size * (1.25 if i % 3 == 0 else 0.8)) * R[0] * (0.9 + 0.2 * rs.rand())
        k.paint(lambda P, p=p, sz=sz: inside(np.linalg.norm(P - p, axis=1) - sz, 0.0018), SPOT, on=["cap"])


def moss_ring(k, name, c, radius, ball, n, bone, seed, z_wave=0.01, hang=0):
    """A collar of lumpy moss round a stalk: balls of two greens overlapping in a ring, some hanging a little lower."""
    rs = np.random.RandomState(seed)
    for i in range(n):
        a = 2 * math.pi * (i + 0.3 * rs.rand()) / n
        s = ball * (0.85 + 0.3 * rs.rand())
        p = np.array(c, float) + np.array([math.cos(a) * radius, math.sin(a) * radius, z_wave * math.sin(3 * a)])
        k.add(f"{name}{i}", ell(p, (s, s, s * 0.85), deform=lumpy(3, 2, 0.25), sub=3), MOSS if i % 2 else MOSS_D,
              bone, surface="leaf")
    for i in range(hang):
        a = 2 * math.pi * (i + 0.5) / hang
        s = ball * 0.6
        p = np.array(c, float) + np.array([math.cos(a) * radius * 1.02, math.sin(a) * radius * 1.02, -ball * 0.8])
        k.add(f"{name}h{i}", ell(p, (s, s, s), sub=3), MOSS_D, bone, surface="leaf")


def amber_eye(pupil=(0.28, 0.6), hi_at=(0.3, 0.38), hi_r=0.24, rim=0.86):
    """The line's eye (u outward, v up): amber, darker at its rim, an upright dark pupil, a highlight."""
    CA, CD, CP, CW = srgb(AMBER), srgb(AMBER_D), srgb(PUPIL), srgb(WHITE)
    def look(u, v):
        r = np.hypot(u, v)
        c = np.tile(CA, (len(u), 1))
        c[r > rim] = CD
        c[(u / pupil[0]) ** 2 + ((v + 0.05) / pupil[1]) ** 2 < 1] = CP
        c[(u - hi_at[0]) ** 2 + (v - hi_at[1]) ** 2 < hi_r ** 2] = CW
        return c
    return look


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h = V[:, 2].max(); w = V[:, 0].max() - V[:, 0].min(); l = V[:, 1].max() - V[:, 1].min()
    s = 0.853 * w + 0.522 * l; q = 1.3 * max(h, 0.85 * max(w, l))
    print(f"KIT: board h {h:.3f} w {w:.3f} l {l:.3f}: sum {s:.3f} of square {q:.3f} = {s / q:.2f}")


def palariuta(src, out_path):
    k = Kit(src, coat="leaf")

    # ---- the stalk and the neck going up into the cap
    k.add("stalk", ell(STALKC, STALKR), STALK, "body")
    k.add("neck", tube([(0, 0.0, 0.2), (0, 0.004, 0.26), (0, 0.008, 0.31)], [0.09, 0.088, 0.086]), STALK, "head")
    k.add("foot", ell((0.048, -0.038, 0.022), (0.036, 0.042, 0.022)), STALK, "leg{s}", mirror=True)

    # ---- the cap
    toadstool(k, CAPC, CAPR, CAPROT, "head", 13, seed=2, stem_r=0.09)

    # ---- the cap's shadow round the stalk's top, its lower edge rising toward the sides
    def band(P):
        z = P[:, 2]; th = np.arctan2(P[:, 0], -P[:, 1])
        low = 0.2 + 0.03 * np.clip(np.abs(th) / 1.4, 0, 1) ** 2
        return inside(low - z, 0.0018) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.12)
    k.paint(band, BAND, on=["stalk", "neck"])

    # ---- the moss round its lower half
    moss_ring(k, "moss", (0, 0.0, 0.078), 0.098, 0.036, 16, "body", seed=6, hang=8)

    body = k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)
    board(body)

    n = unit(0.38, -0.92, -0.08)
    look = amber_eye()
    def lay(state):
        k.decal(EYE, n, EYE_HALF, eye_look(state, look, srgb(BAND), srgb(AMBER), line=0.17), eps=0.0018, rings=20,
                segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    palariuta(*args())
