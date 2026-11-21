# Cursola sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cursola.py -- <the export's Cursola.glb> <out.glb>
#
# The design, from the references: a ghost of dead coral. A pale lavender-white bulb, round above and drawn down into a
# narrow stem, its foot standing in what is left of Galarian Corsola's shell: a hard white cup with a grey lining and
# rim, grey holes on its side and three stubby feet. Inside the bulb, seen through it, the pale core with the face: two
# pink ring eyes with a dark pink point in each and a little frowning mouth (the bulb is opaque here: the core is a
# whiter patch on its front with a blotched grey edge). Out of the back of the bulb a great cloud of white branching
# coral, the largest mass, one thick fused mass behind with six thick lumpy branches spreading up and out to both sides,
# a stub or two on each.
# Reference ratios (3D picture): bulb 0.42 of the height across and 0.45 tall, coral 0.98 of the height across, cup 0.5
# across and 0.2 tall, stem 0.1 across.
# The export's joints: body (0, 0, 0.373), a single bone: everything is on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, unit, eye_look, args, outward, lumpy, polygon

GHOST, CORAL, CORE = "#E6E1F0", "#F4F2F7", "#FBFAFD"
CUP, LINING, HOLE = "#ECECF0", "#8E8E96", "#6E6E76"
PINK, POINT, INK, BLOT = "#E8508E", "#8A1E48", "#5A5464", "#C4BED0"

BC = np.array([0.0, -0.005, 0.425])           # the bulb's middle
BR = np.array([0.155, 0.13, 0.165])


def on_bulb(x, z, lift=0.0):
    q = max(0.0, 1 - (x / BR[0]) ** 2 - ((z - BC[2]) / BR[2]) ** 2)
    p = np.array([x, BC[1] - BR[1] * math.sqrt(q), z])
    n = (p - BC) / BR ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def add_bulb(k):
    def drawn(V):
        V = V.copy()
        s = 1 - 0.62 * np.clip(-V[:, 2], 0, 1) ** 1.25
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("bulb", ell(BC, BR, deform=drawn), GHOST, "body", surface="skin")
    k.add("stem", tube([(0, 0.0, 0.29), (0, 0.0, 0.215), (0, 0.0, 0.155)], [0.055, 0.036, 0.05]), GHOST, "body",
          surface="skin")


def add_cup(k):
    k.add("cup", lathe((0, 0, 0), (0, 0, 1), [(0, 0.035), (0.14, 0.035), (0.188, 0.095), (0.205, 0.14),
                                               (0.19, 0.152), (0.17, 0.112), (0, 0.1)], seg=56), CUP, "body",
          fuse=False, surface="shell")
    # the grey lining and rim, a thin shell just inside the cup's top
    k.add("lining", lathe((0, 0, 0), (0, 0, 1), [(0, 0.104), (0.166, 0.115), (0.186, 0.151), (0.196, 0.152),
                                                  (0.192, 0.158), (0.182, 0.158), (0.162, 0.12), (0, 0.109)],
                          seg=56), LINING, "body", fuse=False, surface="shell")
    for i, a in enumerate(np.radians((90, 210, 330))):
        c = (0.12 * math.cos(a), 0.12 * math.sin(a), 0.03)
        k.add(f"foot{i}", outward(ell(c, (0.04, 0.04, 0.032))), CUP, "body", fuse=False, surface="shell")
    # grey holes on the cup's side, toward the front
    for i, (az, z, r) in enumerate(((-125, 0.09, 0.022), (-62, 0.085, 0.017), (-20, 0.1, 0.013))):
        a = math.radians(az)
        rad = 0.14 + (0.188 - 0.14) * (z - 0.035) / 0.06
        c = np.array([rad * math.cos(a), rad * math.sin(a), z])
        nrm = unit(math.cos(a), math.sin(a), -0.6)
        k.add(f"hole{i}", outward(ell(c + nrm * 0.002, (r, r, r * 0.8))), HOLE, "body", fuse=False, surface="shell")


def branch(k, name, pts, r0, r1, stubs=()):
    """A thick lumpy branch of coral through pts, with stubs (a share along it, a direction, a length)."""
    pts = np.array(pts, float) * np.array([0.92, 1, 1])
    radii = np.linspace(r0, r1, len(pts))
    k.add(name, tube(pts, radii), CORAL, "body")
    # lumps along it
    for j in range(1, len(pts)):
        p = (pts[j - 1] + pts[j]) / 2
        k.add(f"{name}k{j}", ell(p + (0, 0.004 * (-1) ** j, 0.006), [radii[j] * 1.04] * 3), CORAL, "body")
    k.add(f"{name}end", ell(pts[-1], [r1 * 1.0] * 3), CORAL, "body")
    for i, (t, d, ln) in enumerate(stubs):
        j = t * (len(pts) - 1); a = int(min(j, len(pts) - 2)); f = j - a
        at = pts[a] + (pts[a + 1] - pts[a]) * f
        d = unit(*d)
        k.add(f"{name}s{i}", tube([at, at + d * ln * 0.6, at + d * ln], [r1 * 0.95, r1 * 0.85, r1 * 0.9]), CORAL, "body")


def add_coral(k):
    k.add("cloud", ell((0, 0.085, 0.485), (0.15, 0.075, 0.16), deform=lumpy(5, 3, 0.1)), CORAL, "body")
    branch(k, "b1", [(-0.07, 0.08, 0.53), (-0.17, 0.07, 0.6), (-0.27, 0.06, 0.65), (-0.33, 0.05, 0.69)], 0.055, 0.036,
           [(0.4, (0.1, -0.1, 1), 0.08), (0.75, (-0.3, 0.1, -1), 0.06)])
    branch(k, "b2", [(-0.1, 0.09, 0.43), (-0.19, 0.13, 0.42), (-0.28, 0.17, 0.45), (-0.34, 0.2, 0.43)], 0.052, 0.034,
           [(0.5, (0.0, 0.0, -1), 0.06), (0.8, (-0.2, -0.2, 1), 0.06)])
    branch(k, "b3", [(0.07, 0.08, 0.54), (0.17, 0.07, 0.58), (0.27, 0.06, 0.6), (0.36, 0.05, 0.62)], 0.055, 0.036,
           [(0.45, (0.1, 0.1, 1), 0.075), (0.85, (0.2, -0.1, -1), 0.055)])
    branch(k, "b4", [(0.1, 0.09, 0.42), (0.19, 0.13, 0.39), (0.27, 0.17, 0.37), (0.32, 0.2, 0.39)], 0.05, 0.033,
           [(0.55, (0.1, 0.0, -1), 0.05)])
    branch(k, "b5", [(-0.035, 0.1, 0.58), (-0.06, 0.15, 0.65), (-0.08, 0.19, 0.72)], 0.05, 0.034,
           [(0.6, (-1, 0.0, 0.4), 0.06)])
    branch(k, "b6", [(0.045, 0.08, 0.58), (0.08, 0.02, 0.66), (0.11, -0.04, 0.73)], 0.048, 0.033,
           [(0.5, (1, 0.1, 0.5), 0.065), (0.85, (-0.6, 0.0, 1), 0.04)])
    # two short stubs out of the bulb's lower back, as the pictures show
    branch(k, "b7", [(-0.09, 0.06, 0.34), (-0.16, 0.06, 0.31)], 0.04, 0.032)
    branch(k, "b8", [(0.09, 0.06, 0.34), (0.16, 0.06, 0.32)], 0.04, 0.032)


def add_face(k):
    """The core seen through the bulb: a whiter patch with a blotched grey edge, and the frown."""
    p, n = on_bulb(0.0, 0.43)
    CC, CB = srgb(CORE), srgb(BLOT)
    def core(u, v):
        c = np.tile(CC, (len(u), 1))
        r = np.hypot(u, v)
        th = np.arctan2(v, u)
        edge = 0.82 + 0.1 * np.sin(3 * th + 0.5) + 0.06 * np.sin(5 * th)
        c[r > edge] = CB
        return c
    k.decal(p, n, (0.092, 0.078), core, eps=0.003, rings=16, segs=64)
    q, m = on_bulb(0.0, 0.4)
    k.decal(q, m, (0.017, 0.011), INK, eps=0.0055, rings=6, segs=32,
            outline=polygon([(-1, -1), (0, 1), (1, -1), (0, -0.2)]))         # a frown, a chevron


def lay_eyes(k):
    CP, CT, CW, CI = srgb(PINK), srgb(POINT), srgb(CORE), srgb(INK)
    def eye(u, v):
        c = np.tile(CP, (len(u), 1))
        r = np.hypot(u, v)
        c[r < 0.55] = CW
        c[np.hypot(u + 0.02, v + 0.02) < 0.28] = CT
        return c
    p, n = on_bulb(0.038, 0.445)
    def eyes(state):
        k.decal(p, n, (0.022, 0.022), eye_look(state, eye, CW, CI, line=0.18), eps=0.0055, rings=20, segs=56,
                mirror=True)
    return eyes


def cursola(src, out_path):
    k = Kit(src, coat="shell")
    add_bulb(k)
    add_coral(k)
    add_cup(k)
    add_face(k)
    k.build(voxel=0.0058, band=0.005, wband=0.05)
    k.eye_states(lay_eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    cursola(*args())
