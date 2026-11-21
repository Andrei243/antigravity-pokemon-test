# Glimmora-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glimmoramega.py -- <the export's Glimmora-Mega.glb> <out.glb>
#
# Grown from Glimmora and Glimmet (sibling("glimmora"), whose module holds sibling("glimmet")): their deep blue
# crystal, their lilac, their pointed crystal petals (petal) and the face's cone with its eyes (cone, cone_eyes).
# From the references (they overrule the C# sculptor's stalks of crystal and its long cone of blades): the bud has
# burst; six great blue petals float free in a pinwheel round the heart, each a fat cupped teardrop with pale teal
# streaks along it and a teal root broken off in a fringe of shards; at the heart a shaggy burst of periwinkle crystal
# blades, swept back, with a pale lilac bulb behind; in front a teal star of five blunt points, two small red eyes at
# its middle, and from it a long thin steel lance thrust forward. It hovers. (The floating blue chips the pictures
# scatter round it are left out: specks at the game's size.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, inside, smoothstep, unit, args, sibling

GM = sibling("glimmora")
G = GM.G

BLUE, BLUE_DK, TEAL, STREAK = "#1A44C0", "#132F8C", "#35BFC0", "#5CC6E8"
PERI, PERI_LT, BULB = "#7F7ED8", "#A4A2EA", "#B9B2E2"
STAR, LANCE = "#4CD6C4", "#8EA4BE"
C = np.array([0.0, 0.0, 0.607])                # the heart, on the body's joint
VOXEL = 0.0062
RING, PL, PW, PT = 0.31, 0.165, 0.098, 0.034     # petals: ring radius, half length, half width, half thickness
SPIN = math.radians(62)                          # the pinwheel: a petal's length turned from the radius


def petal_frame(i):
    a = math.radians(28.6 + 60 * i)
    rad = np.array([math.cos(a), 0.0, math.sin(a)])
    tan = np.array([-math.sin(a), 0.0, math.cos(a)])
    L = math.cos(SPIN) * rad + math.sin(SPIN) * tan      # along the petal, root to tip
    N = np.array([0, -1.0, 0])                          # its face, toward the front
    N = unit(*(N + 0.25 * np.cross(L, N)))               # each rolled a little about its length
    W = np.cross(N, L)
    centre = C + rad * RING + np.array([0, 0.03, 0])
    return centre, L, W, N


def cupped(V):
    """A teardrop along z (+1 the broad tip, -1 the root), cupped toward -y (its face) and bent a little."""
    V = V.copy()
    t = (V[:, 2] + 1) / 2
    w = np.clip(0.18 + 1.1 * np.sqrt(np.clip(t, 0, 1)) * (1 - 0.35 * t ** 4), 0, None)
    V[:, 0] *= w
    V[:, 1] *= 0.55 + 0.45 * w
    V[:, 1] += 0.55 * V[:, 0] ** 2 - 0.25 * (V[:, 2] ** 2)      # cupped across, bowed along
    return V


def petals(k):
    for i in range(6):
        c, L, W, N = petal_frame(i)
        M = np.stack([W, -N, L], axis=1)
        V, F = ell((0, 0, 0), (PW, PT, PL), deform=cupped)
        k.add(f"petal{i}", (V @ M.T + c, F), BLUE, f"petal{i}", surface="shell")
        root = c - L * PL * 0.92
        # the teal root broken off in a fringe of shards
        for j, (s, ln) in enumerate(((-1, 0.05), (0, 0.065), (1, 0.045))):
            d = unit(*(-L + 0.45 * s * W))
            k.add(f"fringe{i}_{j}", G.petal(root + W * s * 0.012, d, N, 0.0, ln, 0.016, 0.012), TEAL,
                  f"petal{i}", fuse=False, surface="shell")
    # in each petal's own frame: darker at the edge behind, teal toward the root, pale streaks along it
    frames = [petal_frame(i) for i in range(6)]
    def local(P, i):
        c, L, W, N = frames[i]
        q = P - c
        return q @ W, q @ L, q @ N
    def near(P, i):
        w, l, n = local(P, i)
        return inside(np.sqrt((w / (PW * 1.5)) ** 2 + (l / (PL * 1.15)) ** 2 + (n / 0.08) ** 2) - 1, 0.01)
    def teal(P):
        m = np.zeros(len(P))
        for i in range(6):
            w, l, n = local(P, i)
            m = np.maximum(m, near(P, i) * smoothstep(-0.07, -0.13, l))
        return m
    def dark(P):
        m = np.zeros(len(P))
        for i in range(6):
            w, l, n = local(P, i)
            m = np.maximum(m, near(P, i) * smoothstep(0.005, -0.03, n) * 0.6)
        return m
    def streaks(P):
        m = np.zeros(len(P))
        for i in range(6):
            w, l, n = local(P, i)
            front = inside(0.004 - n, 0.006)
            for off in (-0.3, 0.25):
                cw = PW * (off + 0.25 * (l / PL))
                m = np.maximum(m, near(P, i) * front * inside(np.abs(w - cw) - 0.0045, 0.002)
                               * inside(np.abs(l - 0.02) - 0.09, 0.01))
        return m
    k.paint(dark, BLUE_DK, on=["petal"])
    k.paint(teal, TEAL, on=["petal"])
    k.paint(streaks, STREAK, on=["petal"])


def heart(k):
    k.add("core", ell(C, (0.08, 0.075, 0.08)), PERI, "body", surface="shell")
    # the pale lilac bulb behind, Glimmet's own pear turned to the back
    V, F = ell((0, 0, 0), (0.062, 0.062, 0.12), deform=G.pear)
    M = np.stack([np.array([1.0, 0, 0]), np.array([0, 0, 1.0]), np.array([0, -1.0, 0])], axis=1)   # its top to the front
    k.add("bulb", (V @ M.T + C + np.array([0.0, 0.13, -0.01]), F), BULB, "body", surface="shell")
    # the shaggy burst of crystal blades, swept back, in two rings
    for i in range(11):
        a = math.radians(i * 360 / 11 + 8)
        d = unit(math.cos(a), 0.35, math.sin(a))
        ln = 0.175 if i % 2 == 0 else 0.135
        k.add(f"blade{i}", G.petal(C + d * 0.03, d, (0, -1, 0), 0.0, ln, 0.04, 0.018),
              PERI if i % 2 else PERI_LT, "body", fuse=False, surface="shell")
    for i in range(8):
        a = math.radians(i * 45 + 30)
        d = unit(math.cos(a), 0.95, math.sin(a))
        k.add(f"back{i}", G.petal(C + d * 0.03, d, (0, -1, 0), 0.0, 0.13, 0.036, 0.016), PERI,
              "body", fuse=False, surface="shell")
    # the teal star of five blunt points at the lance's root
    for i in range(5):
        a = math.radians(90 + i * 72)
        d = unit(math.cos(a), -0.25, math.sin(a))
        k.add(f"star{i}", G.petal(C + np.array([0, -0.07, 0]) + d * 0.012, d, (0, -1, 0), 0.0, 0.065, 0.032, 0.014),
              STAR, "head", fuse=False, surface="shell")
    k.add("starmid", ell(STARMID_C, STARMID_R), STAR, "head", surface="shell")


LANCE_C, LANCE_R, LANCE_L = C + np.array([0, -0.07, 0]), 0.04, 0.5
STARMID_C, STARMID_R = C + np.array([0, -0.072, 0]), np.array([0.052, 0.036, 0.052])
RED, RED_DK = "#E2303C", "#A21A2A"


def eyes(k):
    """Two small red ovals on the teal star's middle, either side over the lance's root (the pictures' place)."""
    def look(u, v):
        col = np.tile(srgb(RED), (len(u), 1))
        col[np.hypot(u, v) > 0.8] = srgb(RED_DK)
        return col
    def lay(state):
        lk = G.eye_look(state, look, srgb(STAR), G.INK, line=0.16)
        for s in (1, -1):
            d = unit(0.5 * s, -0.75, 0.55)
            sc = 1 / math.sqrt(((d / STARMID_R) ** 2).sum())
            k.decal(STARMID_C + d * sc, d, (0.021, 0.017), (lambda u, v, s=s: lk(s * u, v)), up=unit(0, 0.4, 1),
                    eps=0.0018, rings=20, segs=56)
    return lay


def glimmoramega(src, out_path):
    k = Kit(src, coat="shell")
    petals(k)
    heart(k)
    G.cone(k, LANCE_C, LANCE_R, LANCE_L, LANCE, "head", name="lance")
    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    glimmoramega(*args())
