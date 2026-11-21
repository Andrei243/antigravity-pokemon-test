# Duraludon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/duraludon.py -- <the export's Duraludon.glb> <out.glb>
#
# The design, from the references: a dragon that is a tower of pale steel, every part flat-faced with hard edges. Its
# body is a tall tapering prism, broad over the hips and narrowing up in sections (a step at each seam, like a
# skyscraper's), the top of the tower bending forward into the head: an angular block whose front comes down to a
# blunt beak, a small red cap on its crown, a yellow glaring eye with a dark slit on each side-facet. A dark navy
# zigzag line runs from under the head down the chest's right side; a dark slot on the chest; a dark navy band across
# the foot of the tower between the thighs. The arms are short blocks held forward and down, each ending in a dark
# navy faceted star of a hand (a pyramid with points round its rim). Great white thighs fused into the hips, short
# feet with small navy claws. A long tail of white blocks running back and down to the ground.
# (The art's arms are flung out sideways; here they are held forward and down. The art's bulky white shape low on the
# right is the thigh, and the dark star is the hand seen face-on, the other hand's points seen edge-on as claws.)
# A body of loose hulls (registeel.py's and the Beldum line's way): the eyes lie on thin fused plates just proud of
# the head's side facets.
# Bones: body (0, 0, 0.42), head (0, -0.02, 0.57), arms (+-0.15, 0, 0.52), legs (+-0.09, 0, 0.25), tail (0, 0.08,
# 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, unit, eye_look, args, polygon, boxy, outward

WHITE, NAVY, RED, YELLOW, INK = "#DDE0E9", "#2D3A5E", "#BD4538", "#F3D23E", "#151826"


def plan(w, yf, yb, lean=0.0):
    """The tower's section at one height: a ridge down the front, front corners, sides, back corners, back."""
    pts = [(0, yf - 0.035), (0.72 * w, yf), (w, (yf + yb) / 2 + 0.01), (0.72 * w, yb), (0, yb + 0.015)]
    pts += [(-x, y) for x, y in pts[1:4][::-1]]
    return [(x, y + lean) for x, y in pts]


# The tower's sections, bottom up: (z, half width, front, back, lean) at their foot and their top, and the bone
SECTIONS = [
    ("hips", (0.14, 0.2, -0.13, 0.125, 0.0), (0.42, 0.178, -0.13, 0.115, 0.0), "body"),
    ("trunk", (0.425, 0.166, -0.122, 0.107, 0.0), (0.62, 0.128, -0.108, 0.09, -0.008), "body"),
    ("neck", (0.625, 0.117, -0.1, 0.082, -0.012), (0.79, 0.084, -0.082, 0.052, -0.05), "head"),
]


def section(lo, hi):
    pts = []
    for z, w, yf, yb, lean in (lo, hi):
        pts += [(x, y, z) for x, y in plan(w, yf, yb, lean)]
    return hull(pts)


def front_at(x, z):
    """A point of the tower's front (the right-hand facet) at width x and height z."""
    for _, lo, hi, _ in SECTIONS:
        if lo[0] <= z <= hi[0] + 0.004:
            t = (z - lo[0]) / (hi[0] - lo[0])
            w, yf, lean = (lo[1] + (hi[1] - lo[1]) * t, lo[2] + (hi[2] - lo[2]) * t, lo[4] + (hi[4] - lo[4]) * t)
            return np.array([x, yf - 0.035 + 0.035 * abs(x) / (0.72 * w) + lean, z])
    raise ValueError(z)


def strip(p0, p1, half, proud, normal=(0, -1, 0)):
    """A thin flat bar from p0 to p1 lying on a face whose outward normal is given, standing proud of it."""
    n = unit(*normal)
    d = unit(*(np.array(p1) - np.array(p0)))
    a = np.cross(n, d); a /= np.linalg.norm(a)
    pts = []
    for p in (p0, p1):
        for s in (-1, 1):
            for h in (-0.004, proud):
                pts.append(np.array(p) + a * s * half + n * h - d * s * 0.0)
    return hull(pts)


# The head's plan: back, side-back, side-front (the eye's facet between those two), front ridge
HEAD_PLAN = [(0.0, 0.07), (0.07, 0.06), (0.09, 0.0), (0.045, -0.105), (0.0, -0.125)]
HEAD_PLAN = HEAD_PLAN + [(-x, y) for x, y in HEAD_PLAN[1:-1][::-1]]
HY = -0.05                                         # the head's shift forward over the tower


def head(k):
    pts = []
    for z, s, dy in ((0.76, 0.92, 0.025), (0.83, 1.0, 0.0), (0.91, 1.0, 0.0), (0.965, 0.72, -0.025)):
        pts += [(x * s, y * s + dy + HY, z) for x, y in HEAD_PLAN]
    k.add("head", hull(pts), WHITE, "head", fuse=False, surface="metal")
    # the blunt beak: the front coming forward and down
    beak = [(sx * 0.04, -0.118 + HY, 0.88) for sx in (-1, 1)] + [(sx * 0.04, -0.105 + HY, 0.79) for sx in (-1, 1)]
    beak += [(sx * 0.028, -0.17 + HY, 0.8) for sx in (-1, 1)] + [(sx * 0.03, -0.155 + HY, 0.85) for sx in (-1, 1)]
    beak += [(0.0, -0.16 + HY, 0.775)]
    k.add("beak", hull(beak), WHITE, "head", fuse=False, surface="metal")
    # the red cap on its crown, up and back
    cap = [(sx * 0.026, y + HY, z) for sx in (-1, 1) for y, z in ((0.0, 0.96), (0.05, 0.96), (0.03, 1.0),
                                                                     (0.065, 0.99))]
    k.add("cap", hull(cap), RED, "head", fuse=False, surface="metal")


def eye_facet():
    """The centre and outward normal of the right eye's facet (from side-back to side-front)."""
    a, b = np.array(HEAD_PLAN[2]), np.array(HEAD_PLAN[3])
    e = b - a
    n = unit(-e[1], e[0], 0)
    if n[0] < 0:
        n = -n
    m = a + e * 0.45
    return np.array([m[0], m[1] + HY, 0.87]), n, unit(e[0], e[1], 0)


def duraludon(src, out):
    k = Kit(src, coat="metal")
    # the tower
    for name, lo, hi, bone in SECTIONS:
        k.add(name, section(lo, hi), WHITE, bone, fuse=False, surface="metal")
    # the navy band across the tower's foot in front, between the thighs
    band = []
    for z, w in ((0.135, 0.203), (0.225, 0.196)):
        P = plan(w + 0.004, -0.134, 0.125)
        band += [(x, y, z) for x, y in P[:3] + P[-2:]] + [(sx * (w + 0.004), -0.03, z) for sx in (-1, 1)]
    k.add("band", hull(band), NAVY, "body", fuse=False, surface="metal")
    head(k)
    # the zigzag line down the chest's right side, and the slot
    zig = [(0.028, 0.785), (0.058, 0.72), (0.026, 0.665), (0.062, 0.6), (0.03, 0.54), (0.064, 0.475), (0.036, 0.43)]
    for i, ((x0, z0), (x1, z1)) in enumerate(zip(zig[:-1], zig[1:])):
        p0, p1 = front_at(x0, z0), front_at(x1, z1)
        n = unit(0.035 / 0.1, -1, 0.15)
        bone = "head" if min(z0, z1) > 0.62 else "body"
        k.add(f"line{i}", strip(p0, p1, 0.01, 0.006, n), NAVY, bone, fuse=False, surface="metal")
    s0, s1 = front_at(-0.065, 0.5), front_at(-0.025, 0.5)
    k.add("slot", strip(s0, s1, 0.008, 0.006, unit(-0.35, -1, 0)), INK, "body", fuse=False, surface="metal")
    # the thighs, great white blocks fused into the hips; short feet with small navy claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        th = []
        for z, x0, x1, y0, y1 in ((0.38, 0.05, 0.19, -0.09, 0.08), (0.22, 0.04, 0.29, -0.15, 0.13),
                                  (0.06, 0.065, 0.245, -0.11, 0.09)):
            th += [(x0, y0 + 0.02, z), (x1 - 0.02, y0, z), (x1, (y0 + y1) / 2, z), (x1 - 0.02, y1, z), (x0, y1, z)]
        k.add(f"thigh{side}", hull(np.array(th) * m), WHITE, f"leg{side}", fuse=False, surface="metal")
        foot = [(x, y, z) for x in (0.07, 0.24) for y in (-0.16, 0.08) for z in (0.0, 0.06)]
        foot += [(0.155, -0.175, 0.03)]
        k.add(f"foot{side}", hull(np.array(foot) * m), WHITE, f"leg{side}", fuse=False, surface="metal")
        for j, x in enumerate((0.11, 0.2)):
            c = [(x - 0.022, -0.15, 0.0), (x + 0.022, -0.15, 0.0), (x - 0.018, -0.15, 0.045),
                 (x + 0.018, -0.15, 0.045), (x, -0.215, 0.0), (x, -0.2, 0.012)]
            k.add(f"claw{side}{j}", hull(np.array(c) * m), NAVY, f"leg{side}", fuse=False, surface="metal")
    # the arms: a shoulder block, a forearm block held forward and down, a dark star of a hand
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"arm{side}"
        sh = [(x, y, z) for x in (0.115, 0.235) for y in (-0.065, 0.06) for z in (0.5, 0.615)]
        sh += [(0.255, 0.0, 0.56)]
        k.add(f"shoulder{side}", hull(np.array(sh) * m), WHITE, bone, fuse=False, surface="metal")
        a0, a1 = np.array([0.215, -0.02, 0.535]), np.array([0.215, -0.19, 0.44])
        d = unit(*(a1 - a0))
        u = unit(1, 0, 0); v = np.cross(d, u)
        arm = [p + u * su * 0.05 + v * sv * 0.048 for p, sc in ((a0, 1.0), (a1, 0.9)) for su in (-1, 1)
               for sv in (-1, 1)]
        k.add(f"forearm{side}", hull(np.array(arm) * m), WHITE, bone, fuse=False, surface="metal")
        c = np.array([0.215, -0.235, 0.42])
        n = unit(0.25, -0.95, -0.12)
        e1 = np.cross(n, (0, 0, 1.0)); e1 /= np.linalg.norm(e1)
        e2 = np.cross(n, e1)
        rim = [c + 0.062 * (math.cos(t) * e1 + math.sin(t) * e2) for t in np.linspace(0, 2 * math.pi, 8, endpoint=False)]
        star = rim + [c + n * 0.05, c - n * 0.03]
        k.add(f"hand{side}", hull(np.array(star) * m), NAVY, bone, fuse=False, surface="metal")
        for j in range(4):
            t = math.pi / 4 + j * math.pi / 2
            r = math.cos(t) * e1 + math.sin(t) * e2
            q = np.cross(n, r)
            base = c + r * 0.045
            pt = [base + q * sq * 0.024 + n * sn * 0.018 for sq in (-1, 1) for sn in (-1, 1)]
            pt += [c + r * 0.108 + n * 0.006]
            k.add(f"point{side}{j}", hull(np.array(pt) * m), NAVY, bone, fuse=False, surface="metal")
    # the tail: white blocks back, down and out to +X, the last navy
    path = [np.array(p) for p in ((0.0, 0.1, 0.33), (0.06, 0.25, 0.23), (0.13, 0.37, 0.13), (0.19, 0.46, 0.06),
                                  (0.23, 0.52, 0.03))]
    sizes = [0.075, 0.062, 0.05, 0.04, 0.03]
    for i in range(4):
        p0, p1 = path[i], path[i + 1]
        d = unit(*(p1 - p0))
        u = np.cross(d, (0, 0, 1.0)); u /= np.linalg.norm(u)
        v = np.cross(u, d)
        q1 = p1 - d * 0.012
        pts = [p + u * su * r + v * sv * r * 0.9 for p, r in ((p0, sizes[i]), (q1, sizes[i + 1] * 1.05))
               for su in (-1, 1) for sv in (-1, 1)]
        k.add(f"tail{i}", hull(pts), NAVY if i == 3 else WHITE, "tail", fuse=False, surface="metal")

    # the eyes' plates: thin, fused, just proud of the head's side facets
    ec, en, ea = eye_facet()
    k.add("eyeplate", outward(hull([ec + en * h + ea * a + np.array([0, 0, z])
                                    for h in (-0.012, 0.0015) for a in (-0.05, 0.05) for z in (-0.045, 0.045)])),
          WHITE, "head", mirror=True)
    k.build(voxel=0.003, smooth=2, band=0.003, wband=0.02)

    # a yellow glaring eye: an angular almond, navy rimmed, a dark slit toward the front (u runs toward the back)
    Y, N, K = srgb(YELLOW), srgb(NAVY), srgb(INK)
    W = srgb(WHITE)
    def glare(u, v):
        c = np.tile(Y, (len(u), 1))
        c[((u + 0.25) / 0.2) ** 2 + (v / 0.75) ** 2 < 1] = K
        return c
    shape = [(-1.0, 0.25), (-0.55, -0.55), (0.6, -0.65), (1.0, -0.2), (0.75, 0.55), (-0.3, 0.75)]
    def lay(state):
        if state == "Fierce":
            def look(u, v):
                c = glare(u, v)
                c[v > 0.05 - 0.45 * u] = W
                return c
        else:
            look = eye_look(state, glare, W, N, line=0.18)
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            out_ = polygon([(a * 1.18, b * 1.18) for a, b in shape])
            inn = polygon(shape)
            if s < 0:
                out_ = polygon([(-a * 1.18, b * 1.18) for a, b in shape]); inn = polygon([(-a, b) for a, b in shape])
            f = look if s > 0 else (lambda u, v, lk=look: lk(-u, v))
            k.decal((ec + en * 0.0015) * m, en * m, (0.04, 0.028), NAVY, up=(0, 0, 1), eps=0.0012, outline=out_,
                    rings=12, segs=72)
            k.decal((ec + en * 0.0015) * m, en * m, (0.04, 0.028), f, up=(0, 0, 1), eps=0.0022, outline=inn,
                    rings=20, segs=72)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    duraludon(*args())
