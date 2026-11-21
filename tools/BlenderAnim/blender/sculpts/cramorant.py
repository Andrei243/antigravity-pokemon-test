# Cramorant sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cramorant.py -- <the export's Cramorant.glb> <out.glb>
# Swanna's and Pidgey's ways of making a bird (pidgey.py's feathers, ducklett.py's webbed feet). From the
# references: a blue cormorant standing upright, its body a pear wide at the bottom on two short dark grey legs and
# broad webbed feet; a great ruff of pale lavender down over the chest, ragged at its lower edge and puffing out at
# the neck's base; its wings held half raised beside the body, a fan of long feathers each, their tips up and out;
# a short fan of tail feathers low behind; a thick neck in a gentle S to a small round head with a tuft of feathers
# swept up and back; a big round lime-green eye with a small black pupil, staring; a long yellow beak, its gape
# running back under the eye in a dark line, its tip hooked down a touch.
# The export's joints: body (0, 0, 0.25), wings (+-0.07, 0, 0.27), tail (0, 0.07, 0.15), head (0, -0.05, 0.45),
# legs (+-0.05, 0, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, tri, unit, eye_look, args, hull, sibling

p_ = sibling("pidgey")
d_ = sibling("ducklett")
spike = p_.spike

BLUE, BLUE_DARK, RUFF, RUFF_SHADE = "#2E6FD8", "#2253AE", "#E4E6F6", "#B9BDE0"
BEAK, LIP, GAPE, FOOT = "#F2C636", "#D9A62E", "#4A3A20", "#4C5862"
EYE_GREEN, PUPIL, INK = "#A9E04A", "#141418", "#1E2A3A"

VOXEL = 0.0068
H = np.array([0.0, -0.045, 0.548])            # the head's middle
HR = np.array([0.064, 0.072, 0.063])


def on_ell(c, r, d):
    d = np.array(d, float)
    return np.array(c, float) + d / np.linalg.norm(d / np.array(r, float))


def pear(V):
    """Wide and round at the bottom, narrowing to the chest."""
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.28 * t
    V[:, 1] *= 1 - 0.18 * t
    return V


def body(k):
    k.add("body", ell((0.0, 0.015, 0.195), (0.14, 0.13, 0.155), rot=(-6, 0, 0), deform=pear), BLUE, "body")
    k.add("chest", ell((0.0, -0.035, 0.29), (0.097, 0.088, 0.088)), BLUE, "body")
    # the ruff of down: a fused mass over the chest, pale
    k.add("ruffmass", ell((0.0, -0.065, 0.265), (0.095, 0.06, 0.12), rot=(-8, 0, 0)), RUFF, "body")
    k.add("neck", tube([(0.0, -0.035, 0.32), (0.0, -0.03, 0.39), (0.0, -0.008, 0.44), (0.0, -0.025, 0.5),
                        (0.0, -0.04, 0.53)], [0.058, 0.05, 0.046, 0.046, 0.048]), BLUE, "head")
    k.add("head", ell(H, HR, rot=(-8, 0, 0)), BLUE, "head")
    # the yellow lips at the beak's root, wrapping back under the eye
    k.add("lips", ell(H + np.array([0.0, -0.048, -0.022]), (0.034, 0.04, 0.026), rot=(-10, 0, 0)), LIP, "head")


def ruff(k):
    """Pale lavender down over the chest and the front of the neck's base, its lower edge ragged in points."""
    def m(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        low = 0.15 + 0.03 * (tri(x / 0.045) + 1) / 2 + 0.25 * np.clip(x * x / 0.12, 0, 0.2)
        front = y < 0.0 + 0.55 * np.maximum(z - 0.3, 0) - 0.5 * np.abs(x) * 0.6
        return inside(low - z, 0.003) * inside(z - 0.4, 0.004) * front * inside(np.abs(x) - 0.105, 0.004)
    k.paint(m, RUFF, on=["body", "chest", "neck"])
    # puffs of down: a collar round the neck's base, fuller at the sides, and tufts hanging from the ruff's edge
    for x, y, z, r in ((0.065, -0.06, 0.375, 0.035), (-0.065, -0.06, 0.375, 0.035), (0.09, -0.035, 0.35, 0.034),
                       (-0.09, -0.035, 0.35, 0.034), (0.0, -0.085, 0.37, 0.034), (0.035, -0.08, 0.38, 0.03),
                       (-0.035, -0.08, 0.38, 0.03)):
        k.add("puff", ell((x, y, z), (r, r * 0.9, r * 0.85)), RUFF, "body")
    for x, z, r in ((0.0, 0.16, 0.022), (0.033, 0.165, 0.021), (-0.033, 0.165, 0.021), (0.065, 0.175, 0.02),
                    (-0.065, 0.175, 0.02), (0.09, 0.195, 0.018),
                    (-0.09, 0.195, 0.018)):
        k.add("tuft", ell((x, -0.1 + 0.3 * x * x, z), (r * 0.75, r * 0.6, r * 1.45)), RUFF, "body")
    # a shade of lavender low on the ruff and under the collar
    k.paint(lambda P: inside(P[:, 2] - 0.2, 0.02) * (P[:, 1] < -0.04), RUFF_SHADE, on=["ruffmass", "tuft"])


def wings(k):
    """Each wing a fan of long feathers held half raised beside the body, tips up and out, a little forward."""
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"wingmass{side}", ell((0.11 * s, 0.0, 0.3), (0.05, 0.075, 0.07), rot=(0, -20 * s, 0)), BLUE,
              f"wing{side}")
        root = np.array([0.11 * s, 0.0, 0.29])
        d = unit(0.46 * s, -0.24, 0.85)                    # up, out and a little forward
        across = np.cross((0, 1.0, 0), d)
        across /= np.linalg.norm(across)
        if across[0] * s < 0:
            across = -across                               # toward the outer, trailing side
        # a broad wing, its outer edge ending in three fingered feathers
        WING = [(-0.01, -0.03), (0.07, -0.04), (0.14, -0.035), (0.205, -0.02), (0.215, 0.0), (0.17, 0.012),
                (0.2, 0.03), (0.155, 0.04), (0.17, 0.062), (0.12, 0.062), (0.11, 0.08), (0.06, 0.07), (0.0, 0.04)]
        k.add(f"wing{side}", slab(WING, root, d, across, 0.026), BLUE, f"wing{side}", fuse=False, surface="fur")
        # the flight feathers darker, a plate a little thicker over the wing's outer half
        TIPS = [(0.1, -0.038), (0.14, -0.035), (0.205, -0.02), (0.215, 0.0), (0.17, 0.012), (0.2, 0.03),
                (0.155, 0.04), (0.17, 0.062), (0.12, 0.062), (0.11, 0.08), (0.08, 0.07), (0.09, 0.0)]
        k.add(f"wingtip{side}", slab(TIPS, root, d, across, 0.032), BLUE_DARK, f"wing{side}", fuse=False,
              surface="fur")


def tail(k):
    """A short fan of tail feathers low behind, their faces up to the camera behind."""
    for j, yaw in enumerate((-30, -10, 10, 30)):
        y = math.radians(yaw)
        base = np.array([math.sin(y) * 0.03, 0.12, 0.13])
        d = unit(math.sin(y) * 0.9, math.cos(y), -0.35)
        k.add("tail", spike(base, base + d * 0.13, 0.065, 0.024, side=(math.cos(y), -math.sin(y), 0), at=0.5),
              BLUE if j % 2 else BLUE_DARK, "tail", fuse=False, surface="fur")


def crest(k):
    """The tuft swept up and back from the crown: a few thick feathers fanned."""
    top = H + np.array([0.0, 0.02, HR[2] * 0.8])
    for j, (dx, dy, dz, ln, w) in enumerate(((0.0, 0.55, 1.0, 0.11, 0.04), (0.45, 0.75, 0.8, 0.09, 0.035),
                                              (-0.45, 0.75, 0.8, 0.09, 0.035), (0.0, 1.0, 0.45, 0.075, 0.032))):
        d = unit(dx, dy, dz)
        k.add("crest", spike(top, top + d * ln, w, 0.022, side=np.cross(d, (0, 1.0, 0.3)), at=0.55),
              BLUE if j else BLUE_DARK, "head", fuse=False, surface="fur")


def beak(k):
    root = H + np.array([0.0, -0.05, -0.012])
    tip = root + np.array([0.0, -0.215, 0.012])
    pts = [root, root + (tip - root) * 0.35, root + (tip - root) * 0.75, tip, tip + np.array([0.0, -0.006, -0.018])]
    V, F = tube(pts, [0.026, 0.02, 0.014, 0.01, 0.005])
    # deeper than wide at the root, flat across further on
    V = V.copy()
    t = np.clip((root[1] - V[:, 1]) / 0.215, 0, 1)
    V[:, 0] = root[0] + (V[:, 0] - root[0]) * (0.85 + 0.15 * t)
    V[:, 2] = root[2] + 0.012 * t + (V[:, 2] - root[2] - 0.012 * t) * (1.0 - 0.3 * t)
    k.add("beak", (V, F), BEAK, "head", fuse=False, surface="shell")


def gape(k):
    """The beak's gape: a dark line from the root back and down under the eye, a patch on the lips."""
    for s in (1, -1):
        d = unit(0.75 * s, -0.62, -0.25)
        c = on_ell(H + np.array([0.0, -0.048, -0.022]), (0.034, 0.04, 0.026), d)
        out_ = [(-1.0, 0.12), (1.0, 0.35), (1.0, -0.15), (-1.0, -0.38)]
        from sculptkit import polygon
        k.decal(c, d, (0.022, 0.012), GAPE, eps=0.002, outline=polygon(out_), rings=10, segs=40,
                up=(0, 0, 1))


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        x = 0.055 * s
        k.add(f"leg{side}", tube([(x, 0.01, 0.12), (x * 1.04, 0.0, 0.06), (x * 1.08, -0.005, 0.02)],
                                 [0.022, 0.019, 0.018]), FOOT, f"leg{side}", fuse=False, surface="scales")
        turn = math.radians(16 * s)
        ex = np.array([math.cos(turn), math.sin(turn), 0])
        ez = np.array([math.sin(turn), -math.cos(turn), 0])
        foot = [(a * 1.7, b * 1.7) for a, b in d_.FOOT_OUTLINE]
        k.add(f"foot{side}", slab(foot, (x * 1.12, -0.012, 0.011), ex, ez, 0.022), FOOT, f"leg{side}",
              fuse=False, surface="scales")


EYEDIR = unit(0.74, -0.46, 0.3)


def eye_open(u, v):
    G, P, K = srgb(EYE_GREEN), srgb(PUPIL), srgb(INK)
    c = np.tile(G, (len(u), 1))
    c[u * u + v * v > 0.84 ** 2] = K
    c[(u / 0.24) ** 2 + (v / 0.26) ** 2 < 1] = P
    c[((u + 0.34) / 0.12) ** 2 + ((v - 0.36) / 0.12) ** 2 < 1] = srgb("#FFFFFF")
    return c


def eyes(k):
    E = on_ell(H, HR, EYEDIR)
    lid, ink = srgb(BLUE), srgb(INK)
    def lay(state):
        look = eye_look(state, eye_open, lid, ink, line=0.18)
        k.decal(E, EYEDIR, (0.037, 0.037), look, eps=0.0025, rings=20, segs=56, mirror=True)
        if state == "Open":
            # the pupil a patch of its own, so its edge is round at any size
            k.decal(E, EYEDIR, (0.037 * 0.27, 0.037 * 0.29), PUPIL, eps=0.0042, rings=6, segs=40, mirror=True)
    return lay


def cramorant(src, out_path):
    k = Kit(src, coat="fur")
    body(k)
    ruff(k)
    wings(k)
    tail(k)
    crest(k)
    beak(k)
    legs(k)
    gape(k)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    cramorant(*args())
