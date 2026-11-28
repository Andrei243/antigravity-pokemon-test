# Minisaur sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/minisaur.py -- <the export's Minisaur.glb> <out.glb>
#
# The design, from the project's picture (docs/art/reference/gemini/carpathia/10143-Minisaur.jpg) and its brief
# (CP-dex-3-river-and-caves.md, 143): a small, cute olive-green sauropod. A deep barrel of a body low on four short thick
# legs, three grey nails on the front of each foot; a cream belly under it; a long thick neck rising in a gentle S from
# the chest, its whole front cream down to the chest; a small round head with a snout, a long smile, a cream lower jaw,
# two nostrils, a big round dark eye with a white glint high behind the snout; a fern frond held in the mouth, hanging
# forward and down; a patch of dark moss over the middle of the back with grey stone plates standing in it and a few
# sprouts; a long tail going back and down and curling up high behind; grey speckles on the shoulders and thighs.
# Reference ratios (picture): head 0.21 of the height (built 0.22), neck 0.16 of the height across at its middle,
# body 0.38 of the height deep, belly 0.21 of the height off the ground, the moss's stones to 0.70 of the height, the
# tail's curl to 0.73 of the height, the fern 0.28 of the height long.
# The export's joints: body (0, 0, 0.3), tail (0, 0.22, 0.3), head (0, -0.18, 0.36) (the neck's root),
# legs (+-0.11, -0.13, 0.26), hinds (+-0.11, 0.14, 0.26).
#
# Hategant grows from this script (sibling("minisaur")): the same body, eyes, face, legs and tail at 1.3 times the size,
# a hill of moss and round scutes on its back and bony knobs on its head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, smoothstep, unit, eye_look, lumpy, args

GREEN, CREAM, MOSS, MOSS2 = "#9BB062", "#E3E6A2", "#6A9838", "#86B24C"
STONE, STONE2, NAIL, SPECK = "#A39C8E", "#8C8578", "#A89C88", "#8E9160"
FERN, FERN2 = "#6CAE3A", "#4E8A2A"
INK, IRIS, GLINT, NOSTRIL = "#1A1610", "#3A2A20", "#FFFFFF", "#3A4426"


# --- the line's helpers ------------------------------------------------------------------------------------------------
def hit(shape, o, d):
    """Where a ray from o along d first meets a part's own mesh: a place on a part before the body is fused."""
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return None if h[0] is None else np.array(h[0])


def board(ob):
    """The game's board: a square of 1.3 x max(height, 0.85 x max(width, length)); four corners show 0.853 w + 0.522 l."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    s = 0.853 * w + 0.522 * l
    print(f"BOARD: height {h:.3f} width {w:.3f} length {l:.3f}; corners {s:.3f} of square {sq:.3f} = {s / sq:.2f}")


class Scaled:
    """The kit seen at a scale: Minisaur's frame in its own units, Hategant's at 1.3 times them."""
    def __init__(self, k, K):
        self.k, self.K = k, K

    def add(self, name, shape, colour, bone, **kw):
        V, F = shape
        self.k.add(name, (np.asarray(V) * self.K, F), colour, bone, **kw)

    def paint(self, mask, colour, on=None):
        K = self.K
        self.k.paint(lambda P, m=mask: m(P / K), colour, on=on)

    def decal(self, c, n, half, colour, eps=0.003, **kw):
        self.k.decal(np.asarray(c, float) * self.K, n, (half[0] * self.K, half[1] * self.K), colour,
                     eps=eps * self.K, **kw)


def stone(name, c, size, rs, flat=0.7):
    """An angular stone plate: a hull of jittered points, flattened, standing in the moss."""
    P = rs.normal(size=(16, 3)); P /= np.linalg.norm(P, axis=1, keepdims=True)
    P = P * np.array([size, size * 0.85, size * flat]) * (0.85 + 0.25 * rs.rand(16, 1))
    P[:, 2] = np.where(P[:, 2] > 0, P[:, 2] * 1.25, P[:, 2] * 0.6)          # a peaked top, a flat foot
    return hull(np.asarray(c, float) + P)


def fern(s, name, root, d, face, length, width, bone, thick=0.022):
    """A fern frond: a pinnate plate with saw-toothed leaflets down both sides of a midrib, its face to `face`."""
    root, d, face = np.asarray(root, float), unit(*d), unit(*face)
    ex = np.cross(d, face); ex /= np.linalg.norm(ex)
    n = 9
    right = []
    for j in range(n):
        t0 = 0.1 + 0.82 * j / n
        w = width * (1 - 0.7 * t0)
        right.append((width * 0.1, t0))                  # the notch at the leaflet's root, by the midrib
        right.append((w * 0.95, t0 + 0.035))             # the leaflet's rounded tip, swept toward the frond's end
        right.append((w, t0 + 0.07))
    right = [(a, b * length) for a, b in right]
    outline = [(0.02 * width, 0.0)] + right + [(0.0, length * 1.0)] + [(-a, b) for a, b in right[::-1]] \
        + [(-0.02 * width, 0.0)]
    s.add(name, slab(outline, root, ex, d, thick), FERN, bone, fuse=False, surface="leaf")
    s.add(name + "rib", tube([root - d * 0.02, root + d * length * 0.5, root + d * length * 0.9],
                             [0.012, 0.011, 0.009]), FERN2, bone, fuse=False, surface="leaf")


def lay_eyes(s, state, centre, n, half, lid):
    """The line's eye: round and dark, a brown iris, a black pupil, a big white glint high toward the back."""
    K, I, G = srgb(INK), srgb(IRIS), srgb(GLINT)

    def look(u, v):
        c = np.tile(K, (len(u), 1))
        r = u * u + v * v
        c[r < 0.8 ** 2] = I
        c[((u - 0.05) / 0.5) ** 2 + ((v + 0.05) / 0.56) ** 2 < 1] = K
        c[((u - 0.3) / 0.3) ** 2 + ((v - 0.32) / 0.3) ** 2 < 1] = G
        c[((u + 0.3) / 0.13) ** 2 + ((v + 0.38) / 0.13) ** 2 < 1] = G
        return c
    lk = eye_look(state, look, srgb(lid), srgb(INK), line=0.16)
    s.decal(centre, n, half, lk, eps=0.0022, rings=20, segs=56, mirror=True)


# --- the frame --------------------------------------------------------------------------------------------------------
B = np.array([0.0, 0.0, 0.335])                 # the barrel's middle (the body bone at 0, 0, 0.3)
BR = np.array([0.172, 0.262, 0.172])


def sauropod(src, out, big=False):
    K = 1.3 if big else 1.0
    green = "#8FA65E" if big else GREEN
    moss, moss2 = ("#5C8A32", "#7EA846") if big else (MOSS, MOSS2)
    k = Kit(src, coat="scales")
    s = Scaled(k, K)
    rs = np.random.RandomState(143)
    fw = 0.05 if big else 0.0                    # how much further forward the grown neck carries the head

    # --- the body: a deep barrel, a chest under the neck's root, a rump over the tail's root --------------------------
    s.add("body", ell(B, BR), green, "body")
    s.add("chest", ell((0, -0.17, 0.37), (0.152, 0.14, 0.155)), green, "body")
    s.add("rump", ell((0, 0.18, 0.345), (0.15, 0.12, 0.15)), green, "body")

    # --- the legs: short and thick, the shoulders and thighs fused into the flanks, set a little wider than the body ---
    for nm, y, bone in (("shoulder", -0.13, "leg{s}"), ("thigh", 0.14, "hind{s}")):
        s.add(nm, ell((0.12, y, 0.27), (0.085, 0.1, 0.12)), green, bone, mirror=True)
        s.add(nm + "leg", tube([(0.124, y, 0.22), (0.13, y - 0.005, 0.12), (0.134, y - 0.008, 0.06)],
                               [0.077, 0.07, 0.068]), green, bone, mirror=True)
        foot = ell((0.137, y - 0.014, 0.038), (0.08, 0.082, 0.038))
        s.add(nm + "foot", foot, green, bone, mirror=True)
        for j, a in enumerate((-0.55, 0.0, 0.55)):
            cx, cy = 0.137 + 0.052 * math.sin(a), y - 0.014 - 0.07 * math.cos(a)
            p = np.array([cx, cy, 0.022])
            d = unit(math.sin(a) * 0.4, -1, -0.15)
            side = unit(math.cos(a), math.sin(a) * 0.4, 0)
            s.add(f"{nm}nail{j}", hull([p + side * 0.021, p - side * 0.021, p + (0, 0, 0.024), p - (0, 0, 0.02),
                                        p - d * 0.012 + (0, 0, 0.004), p + d * 0.016 - (0, 0, 0.006)]),
                  NAIL, bone, fuse=False, mirror=True, surface="shell")

    # --- the tail: back and down from the rump, then curling up high behind, swept out toward +X ------------------------
    TAIL = [(0, 0.22, 0.36), (0, 0.33, 0.3), (0.02, 0.44, 0.235), (0.06, 0.53, 0.225), (0.1, 0.58, 0.31),
            (0.13, 0.585, 0.44), (0.13, 0.535, 0.56), (0.11, 0.455, 0.625)]
    s.add("tail", tube(TAIL, [0.098, 0.08, 0.064, 0.054, 0.046, 0.038, 0.029, 0.018], per=10), green, "tail")

    # --- the neck: a gentle S from the chest, thick at its root; the head forward and a little down -----------------
    NECK = [(0, -0.15, 0.37), (0, -0.26 - fw * 0.4, 0.48), (0, -0.305 - fw * 0.8, 0.6), (0, -0.31 - fw, 0.7),
            (0, -0.33 - fw, 0.77)]
    NR = [0.125, 0.09, 0.074, 0.069, 0.066]
    s.add("neck", tube(NECK, NR, per=10), green, "head")
    H = np.array([0.0, -0.39 - fw, 0.8])
    HR = np.array([0.09, 0.105, 0.085])
    head = ell(H, HR)
    s.add("head", head, green, "head")
    SN = H + (0, -0.08, -0.025)
    SNR = np.array([0.064, 0.06, 0.054])
    snout = ell(SN, SNR)
    s.add("snout", snout, green, "head")
    s.add("jaw", ell(H + (0, -0.045, -0.045), (0.07, 0.085, 0.045)), green, "head")

    # --- the cream: the front of the neck down the chest, the lower jaw, the belly, under the tail -------------------
    nz = np.array([p[2] for p in NECK]); ny = np.array([p[1] for p in NECK]); nr = np.array(NR)
    mouth_y = np.array([-0.56 - fw, -0.4 - fw, -0.32 - fw, -0.26 - fw])
    mouth_z = np.array([0.755, 0.758, 0.785, 0.79])

    def cream(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        yc = np.interp(z, nz, ny); rc = np.interp(z, nz, nr)
        throat = inside(y - (yc - 0.32 * rc), 0.004) * inside(z - 0.8, 0.004) * inside(np.abs(x) - 0.9 * rc, 0.006)
        jaw = inside(z - np.interp(y, mouth_y, mouth_z), 0.003) * inside(y - (-0.3 - fw), 0.004)
        chest = inside(y + 0.24, 0.006) * inside(z - 0.42, 0.006) * inside(np.abs(x) - 0.12, 0.006)
        belly = inside(z - 0.235, 0.004) * inside(np.abs(x) - 0.125, 0.006) * inside(np.abs(y) - 0.24, 0.01)
        under = inside(z - 0.255, 0.004) * (y > 0.3) * (y < 0.56)
        return np.maximum.reduce([throat, jaw, chest, belly, under])
    s.paint(cream, CREAM, on=["neck", "head", "snout", "jaw", "chest", "body", "rump", "tail"])

    # --- the back: moss with stones standing in it (Minisaur), or a hill of moss and round scutes (Hategant) ----------
    if not big:
        s.add("moss", ell((0, 0.03, 0.485), (0.135, 0.2, 0.05), deform=lumpy(5, 3, 0.18)), moss, "body", surface="leaf")
        for j, (x, y) in enumerate(((0.11, -0.1), (-0.12, 0.05), (0.09, 0.16), (-0.06, 0.2), (0.12, 0.06))):
            s.add(f"mosslobe{j}", ell((x, y, 0.45), (0.04, 0.05, 0.035)), moss, "body", surface="leaf")
        # paler flecks where the two waves are both high (inside() is 1 below nought)
        s.paint(lambda P: inside(0.45 - np.sin(P[:, 0] * 90) * np.sin(P[:, 1] * 70 + 1), 0.05), moss2, on=["moss"])
        for j, (x, y, z, sz) in enumerate(((0.0, -0.01, 0.55, 0.058), (-0.07, -0.1, 0.52, 0.04), (0.07, 0.11, 0.52, 0.045),
                                           (-0.05, 0.14, 0.51, 0.036), (0.06, -0.12, 0.505, 0.032), (0.03, 0.2, 0.49, 0.03),
                                           (-0.1, 0.0, 0.505, 0.03))):
            s.add(f"stone{j}", stone(f"stone{j}", (x, y, z), sz, rs), STONE if j % 2 == 0 else STONE2, "body",
                  fuse=False, surface="shell")
        for j, (x, y, a) in enumerate(((0.04, -0.06, 0.6), (-0.04, 0.09, -0.6))):
            base = np.array([x, y, 0.53])
            for i, b in enumerate((-0.5, 0.5)):
                d = unit(math.sin(a + b), 0.2, 1.0)
                side = unit(math.cos(a + b), 0, -math.sin(a + b) * 0.3)
                tip = base + d * 0.05
                s.add(f"sprout{j}{i}", hull([base + side * 0.012, base - side * 0.012, base + (0, 0.012, 0),
                                              base - (0, 0.012, 0), tip, base + d * 0.03 + side * 0.016]),
                      FERN, "body", fuse=False, surface="leaf")
    else:
        hill(s, rs, moss, moss2)

    # --- the fern held in the mouth, hanging forward and down on the +X side ------------------------------------------
    if not big:
        root = H + (0.045, -0.105, -0.05)
        fern(s, "fern", root, (0.3, -0.75, -0.6), (0.72, -0.45, 0.3), 0.24, 0.075, "head")
    else:
        knobs(s, H, HR, NECK, NR)

    # --- speckles on the shoulders and thighs, two nostrils, the smile ----------------------------------------------
    for nm, y in (("shoulder", -0.1), ("thigh", 0.17)):
        base = np.array([0.18, y, 0.3])
        for j, (dy, dz, r) in enumerate(((0.0, 0.0, 0.022), (0.03, 0.028, 0.012), (-0.022, 0.03, 0.01),
                                         (0.034, -0.012, 0.009))):
            c = hit(ell(B, BR + (0.012, 0, 0)), base + (0.3, dy, dz), (-1, 0, 0))
            s.decal(c, unit(1, 0.1 * (dy / 0.03), 0.15), (r, r * 0.85), SPECK, rings=6, segs=28, mirror=True)
    nd = unit(0.3, -0.75, 0.58)
    nc = hit(snout, SN + nd * 0.3, -nd)
    s.decal(nc, nd, (0.009, 0.007), NOSTRIL, rings=6, segs=28, mirror=True)

    # The smile: dashes along the line between the green and the cream, from the snout's tip back to the cheek
    for side in (1, -1):
        prev = None
        ys = np.linspace(-0.55 - fw, -0.37 - fw, 6)
        pts = []
        for y in ys:
            z = float(np.interp(y, mouth_y, mouth_z)) + 0.004
            if side > 0 and y < -0.535 - fw:
                pts.append(hit(snout, (0, -0.8 - fw, z), (0, 1, 0)))
                continue
            pts.append(hit(ell(H + (0, -0.045, -0.02), (0.08, 0.13, 0.07)), (0.3 * side, y, z), (-side, 0, 0)))
        for a, b in zip(pts, pts[1:]):
            if b is None or a is None:
                continue
            c = (a + b) / 2
            t = unit(*(b - a))
            n = unit(*(c - (H + (0, -0.05, -0.03))))
            n = unit(*(n - t * (n @ t)))
            up = np.cross(n, t)
            s.decal(c, n, (np.linalg.norm(b - a) * 0.6, 0.0046), INK, up=up, rings=4, segs=24)

    k.build(voxel=0.0068 * K, smooth=4, band=0.005 * K, wband=0.06 * K)
    board(k.ob)

    # --- the eyes: big, round and dark, high on the head behind the snout -------------------------------------------
    de = unit(0.8, -0.32, 0.45)
    ec = hit(head, H + de * 0.3, -de)
    k.eye_states(lambda st: lay_eyes(s, st, ec, unit(0.82, -0.36, 0.44), (0.032, 0.036), green))
    k.export(out)


# --- Hategant's own ---------------------------------------------------------------------------------------------------
MC = np.array([0.0, 0.01, 0.425])               # the hill of moss over the back, draped over the flanks
MR = np.array([0.19, 0.265, 0.3])


def on_ell(c, r, d):
    d = np.asarray(d, float)
    return c + d / np.linalg.norm(d / r)


def normal_on(c, r, d):
    p = on_ell(c, r, d) - c
    return unit(*(p / (r * r)))


def hill(s, rs, moss, moss2):
    """A hill of moss over the back, round grey scutes set in it, ferns at its edges and moss hanging over the flanks."""
    def drape(V):
        V = V.copy()
        V[:, 2] = np.maximum(V[:, 2], -0.25)
        return V
    s.add("hill", ell(MC, MR, deform=lambda V: lumpy(6, 4, 0.06)(drape(V))), moss, "body", surface="leaf")
    # moss hanging down the flanks in short round lobes
    for j in range(16):
        a = 2 * math.pi * (j + 0.3) / 16
        x, y = (MR[0] + 0.005) * math.cos(a), MC[1] + MR[1] * 0.92 * math.sin(a)
        if y < -0.2 and abs(x) < 0.12:
            continue
        L = 0.035 + 0.025 * ((j * 5) % 3)
        top = np.array([x * 0.98, y, 0.38])
        s.add(f"drape{j}", ell(top + (0.004 * np.sign(x), 0, -L * 0.5), (0.036, 0.045, L * 0.5 + 0.025)), moss, "body",
              surface="leaf")
    s.paint(lambda P: inside(0.3 - np.sin(P[:, 0] * 140) * np.sin(P[:, 1] * 120 + 1) * np.sin(P[:, 2] * 110), 0.05),
            moss2, on=["hill"])
    # round scutes in rings over the hill, the top first
    k = 0
    for ring, (n, el, size) in enumerate(((1, 90, 0.062), (6, 66, 0.06), (10, 45, 0.057), (13, 25, 0.052), (14, 7, 0.044))):
        for i in range(n):
            a = 2 * math.pi * (i + 0.5 * ring) / n
            ce = math.cos(math.radians(el))
            d = np.array([ce * math.cos(a), ce * math.sin(a), math.sin(math.radians(el))])
            if el < 30 and math.sin(a) < -0.55:
                continue                                  # none down over the neck's root
            p = on_ell(MC, MR, d)
            nrm = normal_on(MC, MR, d)
            sz = size * 0.86 * (0.88 + 0.24 * rs.rand())
            V, F = ell((0, 0, 0), (sz, sz * 0.92, sz * 0.6), sub=2)
            zax = nrm
            xax = unit(*np.cross((0, 1.0, 0.01), zax)); yax = np.cross(zax, xax)
            V = V @ np.stack([xax, yax, zax], axis=0) + p - nrm * sz * 0.18
            s.add(f"scute{k}", (V, F), STONE if (i + ring) % 2 else STONE2, "body", fuse=False, surface="shell")
            k += 1
    # ferns at its edges, their faces turned out to the cameras
    for j, (a, el, L) in enumerate(((0.35, 30, 0.16), (-0.35, 32, 0.15), (2.75, 32, 0.16), (-2.8, 30, 0.15),
                                    (1.35, 36, 0.14), (-1.75, 34, 0.14))):
        ce = math.cos(math.radians(el))
        d = np.array([ce * math.cos(a), ce * math.sin(a), math.sin(math.radians(el))])
        p = on_ell(MC, MR, d) - normal_on(MC, MR, d) * 0.02
        nout = normal_on(MC, MR, d)
        fd = unit(*(nout * 0.6 + (0, 0, -0.8)))
        face = unit(*(np.cross(fd, unit(-nout[1], nout[0], 0)) + (0, 0, 0.0)))
        fern(s, f"hillfern{j}", p, fd, face, L, 0.06, "body")


def knobs(s, H, HR, NECK, NR):
    """Round bony knobs on the crown and the back of the head, and a row down the top of the neck."""
    for j, (d, r) in enumerate((((0.0, 0.1, 1.0), 0.02), ((0.55, 0.3, 0.8), 0.016), ((-0.55, 0.3, 0.8), 0.016),
                                ((0.3, 0.75, 0.6), 0.017), ((-0.3, 0.75, 0.6), 0.017), ((0.0, 0.9, 0.3), 0.018))):
        p = on_ell(H, HR, d)
        n = normal_on(H, HR, d)
        s.add(f"knob{j}", ell(p - n * r * 0.35, (r, r, r * 0.8)), STONE, "head", fuse=False, surface="shell")
    for j, t in enumerate((0.55, 0.68, 0.8)):
        i = int(t * (len(NECK) - 1)); f = t * (len(NECK) - 1) - i
        c = np.array(NECK[i]) * (1 - f) + np.array(NECK[i + 1]) * f
        r = NR[i] * (1 - f) + NR[i + 1] * f
        p = c + np.array([0, 0.93 * r, 0.3 * r])
        s.add(f"neckknob{j}", ell(p, (0.017, 0.017, 0.014)), STONE, "head", fuse=False, surface="shell")


if __name__ == "__main__":
    sauropod(*args())
