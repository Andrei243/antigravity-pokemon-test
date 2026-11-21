# Archaludon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/archaludon.py -- <the export's Archaludon.glb> <out.glb>
#
# Duraludon's evolution, from the references: a dragon of steel built like a bridge. Its front is one great navy band,
# a bridge's deck seen end-on, arching forward from its top down to the ground between its forelegs: on its upper half
# a lattice of white girders (each panel an X of navy between four white triangles), on its lower half three red
# stripes running its length. At the band's top the small navy head looks forward, a yellow glaring eye on each side
# facet, two navy spires tipped red rising behind it. On each side a great white pylon stands upright beside the band,
# held on a white strut from the shoulder: it rises to a pale yellow tip and ends below in a navy block of a fist, a
# short fin flaring out from its foot. Behind the band the body is white with a toothed ridge; four short white legs
# with navy toes; a white tail running back and out, toothed along its top, rising at its end to a yellow-tipped spike.
# Every part flat-faced with hard edges, loose hulls as Duraludon's; the eyes lie on thin fused plates on the head's
# side facets. (Duraludon's tower is not this shape: only its way of making the head and the glaring eye is kept.)
# Bones: body (0, 0, 0.58); arms (+-0.1, 0, 0.72); head (0, -0.02, 0.8); legs (+-0.09, -0.05, 0.55);
# hind legs (+-0.09, 0.05, 0.48); tail (0, 0.07, 0.44).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, hull, srgb, unit, eye_look, args, polygon, outward, sibling

sui = sibling("suicune")

WHITE, NAVY, RED, TIP, YELLOW, INK = "#DCDFE8", "#2C3B78", "#C2665C", "#EBCF7A", "#F3D23E", "#141830"
GIRDER = "#EEF2F8"
TOOTH = "#B9BECB"

BAND_PATH = [(-0.11, 0.87), (-0.175, 0.73), (-0.215, 0.56), (-0.21, 0.39), (-0.17, 0.24), (-0.11, 0.12)]
BAND_HW, BAND_TH = 0.115, 0.075


def box(c, half, bevel=0.3):
    hx, hy, hz = half
    b = min(half) * bevel
    pts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                pts += [(sx * hx, sy * (hy - b), sz * (hz - b)), (sx * (hx - b), sy * hy, sz * (hz - b)),
                        (sx * (hx - b), sy * (hy - b), sz * hz)]
    return hull(np.array(pts) + np.array(c))


class Band:
    """The navy band's centre line: points, their downward tangents and forward normals, by arc length 0..1."""
    def __init__(self):
        P = sui.smooth_path([(0, y, z) for y, z in BAND_PATH], per=4)
        self.P = P
        T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
        self.N = np.stack([np.zeros(len(T)), T[:, 2], -T[:, 1]], axis=1)       # forward, square to the band
        self.N /= np.linalg.norm(self.N, axis=1, keepdims=True)
        seg = np.linalg.norm(np.diff(P, axis=0), axis=1)
        self.s = np.concatenate([[0], np.cumsum(seg)]) / seg.sum()

    def at(self, s, u, lift):
        """A point of the band's front at arc length s, across u (x), lift off the front face."""
        p = np.array([np.interp(s, self.s, self.P[:, i]) for i in range(3)])
        n = np.array([np.interp(s, self.s, self.N[:, i]) for i in range(3)]); n /= np.linalg.norm(n)
        return p + n * (BAND_TH / 2 + lift) + np.array((u, 0, 0))


def add_band(k, b):
    for i in range(len(b.P) - 1):
        pts = []
        for j in (i, i + 1):
            for sx in (-1, 1):
                for sn in (-1, 1):
                    pts.append(b.P[j] + np.array((sx * BAND_HW, 0, 0)) + b.N[j] * sn * BAND_TH / 2)
        k.add(f"band{i}", hull(pts), NAVY, "body" if b.P[i][2] < 0.72 else "head", fuse=False, surface="metal")


def flat(k, name, corners, b_s, color, bone, lift=(0.0, 0.007)):
    """A thin flat piece lying proud on the band's front: corners (s, u)."""
    pts = [b_s.at(s, u, l) for s, u in corners for l in lift]
    k.add(name, hull(pts), color, bone, fuse=False, surface="metal")


def add_girders(k, b):
    """Two panels of white girders: four triangles round a navy X, the triangles pulled in from the X's lines."""
    hw = BAND_HW - 0.022
    for j, (s0, s1) in enumerate(((0.1, 0.28), (0.295, 0.475))):
        corners = {"tl": (s0, -hw), "tr": (s0, hw), "bl": (s1, -hw), "br": (s1, hw)}
        mid = ((s0 + s1) / 2, 0.0)
        for name, (a, c) in (("t", ("tl", "tr")), ("b", ("bl", "br")), ("l", ("tl", "bl")), ("r", ("tr", "br"))):
            tri = [corners[a], corners[c], mid]
            cs = np.mean([t[0] for t in tri]); cu = np.mean([t[1] for t in tri])
            tri = [(cs + (s - cs) * 0.68, cu + (u - cu) * 0.68) for s, u in tri]
            flat(k, f"girder{j}{name}", tri, b, GIRDER, "head" if s1 < 0.3 else "body")


def add_stripes(k, b):
    """Three red stripes running down the band's lower half."""
    S = np.linspace(0.51, 0.96, 7)
    for j, u in enumerate((-0.062, 0.0, 0.062)):
        for i in range(len(S) - 1):
            flat(k, f"stripe{j}_{i}", [(S[i], u - 0.019), (S[i], u + 0.019), (S[i + 1], u - 0.019), (S[i + 1], u + 0.019)],
                 b, RED, "body")


# --- the head: Duraludon's way, smaller, a navy block looking forward at the band's top ------------------------------
HEAD_PLAN = [(0.0, 0.06), (0.06, 0.055), (0.085, -0.01), (0.048, -0.085), (0.0, -0.1)]
HEAD_PLAN = HEAD_PLAN + [(-x, y) for x, y in HEAD_PLAN[1:-1][::-1]]
HY, HZ = -0.175, 0.0


def head(k):
    pts = []
    for z, s, dy in ((0.77, 0.9, 0.015), (0.82, 1.0, 0.0), (0.885, 1.0, 0.0), (0.93, 0.72, -0.015)):
        pts += [(x * s, y * s + dy + HY, z) for x, y in HEAD_PLAN]
    k.add("head", hull(pts), NAVY, "head", fuse=False, surface="metal")
    # a blunt snout: the front coming forward and down a little
    sn = [(sx * 0.04, -0.09 + HY, 0.865) for sx in (-1, 1)] + [(sx * 0.04, -0.085 + HY, 0.785) for sx in (-1, 1)]
    sn += [(sx * 0.028, -0.135 + HY, 0.8) for sx in (-1, 1)] + [(sx * 0.03, -0.125 + HY, 0.845) for sx in (-1, 1)]
    k.add("snout", hull(sn), NAVY, "head", fuse=False, surface="metal")
    # the two spires rising behind the head, tipped red, the right one taller
    for j, (x, top, lean) in enumerate(((0.035, 1.06, 0.02), (-0.04, 1.0, -0.025))):
        b0 = np.array((x, -0.085, 0.85))
        mid = np.array((x + lean, -0.07, top - 0.05))
        root = [b0 + (sx * 0.026, sy * 0.024, 0) for sx in (-1, 1) for sy in (-1, 1)]
        upper = [mid + (sx * 0.016, sy * 0.015, 0) for sx in (-1, 1) for sy in (-1, 1)]
        k.add(f"spire{j}", hull(root + upper), NAVY, "head", fuse=False, surface="metal")
        k.add(f"spiretip{j}", hull(upper + [mid + (lean * 0.3, 0.005, 0.07)]), RED, "head", fuse=False, surface="metal")


def eye_facet():
    a, b = np.array(HEAD_PLAN[2]), np.array(HEAD_PLAN[3])
    e = b - a
    n = unit(-e[1], e[0], 0)
    if n[0] < 0:
        n = -n
    m = a + e * 0.42
    return np.array([m[0], m[1] + HY, 0.85]), n, unit(e[0], e[1], 0)


# --- the pylons, the body, the legs and the tail -----------------------------------------------------------------------
def pylons(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"arm{side}"
        col = []
        for z, x, hx, hy in ((0.41, 0.255, 0.062, 0.058), (0.6, 0.27, 0.05, 0.047), (0.8, 0.29, 0.035, 0.033),
                             (0.9, 0.3, 0.025, 0.024)):
            col += [(x + sx * hx, -0.09 + sy * hy, z) for sx in (-1, 1) for sy in (-1, 1)]
        k.add(f"pylon{side}", hull(np.array(col) * m), WHITE, bone, fuse=False, surface="metal")
        tip = col[-4:] + [(0.31, -0.095, 0.99)]
        k.add(f"pylontip{side}", hull(np.array(tip) * m), TIP, bone, fuse=False, surface="metal")
        # the navy fist under it
        k.add(f"fist{side}", box(np.array((0.255, -0.09, 0.355)) * m, (0.075, 0.07, 0.06)), NAVY, bone, fuse=False,
              surface="metal")
        # a fin flaring out from its foot
        fin = [(0.3, -0.115, 0.43), (0.3, -0.065, 0.43), (0.3, -0.115, 0.56), (0.3, -0.065, 0.56), (0.39, -0.09, 0.42),
               (0.37, -0.09, 0.47)]
        k.add(f"fin{side}", hull(np.array(fin) * m), WHITE, bone, fuse=False, surface="metal")
        # the strut from the shoulder
        k.add(f"strut{side}", box(np.array((0.17, -0.07, 0.62)) * m, (0.09, 0.035, 0.032)), WHITE, bone, fuse=False,
              surface="metal")


def body(k):
    pts = []
    for z, hx, yf, yb in ((0.24, 0.13, -0.12, 0.15), (0.45, 0.135, -0.13, 0.15), (0.62, 0.115, -0.11, 0.12),
                          (0.78, 0.07, -0.07, 0.04)):
        pts += [(sx * hx, y, z) for sx in (-1, 1) for y in (yf, yb)]
    k.add("body", hull(pts), WHITE, "body", fuse=False, surface="metal")
    # the toothed ridge down the back's two edges
    for s in (1, -1):
        for j, z in enumerate((0.72, 0.6, 0.48, 0.36)):
            x = s * (0.1 if z > 0.6 else 0.125)
            c = np.array((x, 0.14 - (0.08 if z > 0.7 else 0.0), z))
            pts = [c + (sx * 0.02, -0.02, sz * 0.04) for sx in (-1, 1) for sz in (-1, 1)] + [c + (0, 0.07, -0.045)]
            k.add(f"tooth{'R' if s > 0 else 'L'}{j}", hull(pts), TOOTH, "body", fuse=False, surface="metal")


def legs(k):
    for bone, y in (("leg", -0.09), ("hind", 0.11)):
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            pts = []
            for z, x, hx, hy in ((0.3, 0.115, 0.052, 0.058), (0.0, 0.135, 0.062, 0.07)):
                pts += [(x + sx * hx, y + sy * hy, z) for sx in (-1, 1) for sy in (-1, 1)]
            k.add(f"{bone}{side}", hull(np.array(pts) * m), WHITE, bone + side, fuse=False, surface="metal")
            for j, dx in enumerate((-0.03, 0.03)):
                t = [(0.135 + dx + sx * 0.016, y - 0.07, 0.0) for sx in (-1, 1)]
                t += [(0.135 + dx, y - 0.071, 0.065), (0.135 + dx, y - 0.088, 0.0), (0.135 + dx, y - 0.05, 0.0),
                      (0.135 + dx, y - 0.05, 0.06)]
                k.add(f"toe{bone}{side}{j}", hull(np.array(t) * m), NAVY, bone + side, fuse=False, surface="metal")


def tail(k):
    path = [np.array(p) for p in ((0.0, 0.13, 0.4), (0.07, 0.26, 0.33), (0.14, 0.38, 0.28), (0.19, 0.47, 0.27))]
    sizes = [0.07, 0.058, 0.048, 0.04]
    for i in range(3):
        p0, p1 = path[i], path[i + 1]
        d = unit(*(p1 - p0))
        u = np.cross(d, (0, 0, 1.0)); u /= np.linalg.norm(u)
        v = np.cross(u, d)
        pts = [p + u * su * r + v * sv * r * 0.9 for p, r in ((p0, sizes[i]), (p1 - d * 0.01, sizes[i + 1] * 1.05))
               for su in (-1, 1) for sv in (-1, 1)]
        k.add(f"tail{i}", hull(pts), WHITE, "tail", fuse=False, surface="metal")
        # a tooth on top of each block
        c = (p0 + p1) / 2 + np.array((0, 0, sizes[i] * 0.85))
        k.add(f"tailtooth{i}", hull([c - d * 0.035 + u * 0.018, c - d * 0.035 - u * 0.018, c + d * 0.02 + u * 0.018,
                                     c + d * 0.02 - u * 0.018, c + d * 0.04 + (0, 0, 0.05)]), TOOTH, "tail",
              fuse=False, surface="metal")
    # the spike rising at its end
    e = path[-1]
    sp = [e + (sx * 0.032, sy * 0.03, -0.03) for sx in (-1, 1) for sy in (-1, 1)]
    up = [e + (0.02 + sx * 0.018, 0.03 + sy * 0.016, 0.17) for sx in (-1, 1) for sy in (-1, 1)]
    k.add("tailspike", hull(sp + up), WHITE, "tail", fuse=False, surface="metal")
    k.add("tailtip", hull(up + [e + (0.03, 0.045, 0.25)]), TIP, "tail", fuse=False, surface="metal")


def archaludon(src, dst):
    k = Kit(src, coat="metal")
    b = Band()
    add_band(k, b)
    add_girders(k, b)
    add_stripes(k, b)
    head(k)
    pylons(k)
    body(k)
    legs(k)
    tail(k)
    ec, en, ea = eye_facet()
    k.add("eyeplate", outward(hull([ec + en * h + ea * a + np.array([0, 0, z])
                                    for h in (-0.012, 0.0015) for a in (-0.045, 0.045) for z in (-0.04, 0.04)])),
          NAVY, "head", mirror=True)
    k.build(voxel=0.003, smooth=2, band=0.003, wband=0.02)

    Y, N, K = srgb(YELLOW), srgb(NAVY), srgb(INK)
    def glare(u, v):
        c = np.tile(Y, (len(u), 1))
        c[((u + 0.25) / 0.2) ** 2 + (v / 0.75) ** 2 < 1] = K
        return c
    shape = [(-1.0, 0.25), (-0.55, -0.55), (0.6, -0.65), (1.0, -0.2), (0.75, 0.55), (-0.3, 0.75)]
    def lay(state):
        if state == "Fierce":
            def look(u, v):
                c = glare(u, v)
                c[v > 0.05 - 0.45 * u] = N
                return c
        else:
            look = eye_look(state, glare, N, K, line=0.18)
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            out_ = polygon([(s * a * 1.2, b_ * 1.2) for a, b_ in shape])
            inn = polygon([(s * a, b_) for a, b_ in shape])
            f = look if s > 0 else (lambda u, v, lk=look: lk(-u, v))
            k.decal((ec + en * 0.0015) * m, en * m, (0.036, 0.026), INK, up=(0, 0, 1), eps=0.0012, outline=out_,
                    rings=12, segs=72)
            k.decal((ec + en * 0.0015) * m, en * m, (0.036, 0.026), f, up=(0, 0, 1), eps=0.0022, outline=inn,
                    rings=20, segs=72)
    k.eye_states(lay)
    k.export(dst)


if __name__ == "__main__":
    archaludon(*args())
