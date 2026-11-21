# Avalugg-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/avalugghisui.py -- <the export's Avalugg-Hisui.glb> <out.glb>
#
# Avalugg grown (avalugg.py's frame copied: its stepped plates of ice with jagged edges and a crack under each step,
# its hull of a body, its crystal legs with a band, its almond eyes under a brow; bergmite.py's crystal() and jitter()).
# From the references (drawn from the front three-quarter): a longer, narrower beast like a turtle: a top of five pale
# ice plates stepping up from front to back, narrowing toward the back, the cracks between them brown rock; under it
# a body of brown-grey rock; four thick legs of rock, each with a rope-like orange-brown band near its foot and an
# orange-brown paw below with white claws pointing forward. In front the great jaw: a white wedge of ice, wide at the
# top and coming to a point low in front like a ship's prow or a beard, jutting well forward of the plates, with pale
# blue ice blotches outlined in blue on its faces and a little white spike at each upper corner; the head is a band
# of slate-blue under the front plate, its small almond yellow eyes peering over the wedge's top.
# Where the pictures overrule the task's text: the rock is brown-grey, not "darker grey"; the top is still the base's
# plates in steps (no flat slab); the jaw's top is level and its point is at the bottom front, not a blade under the
# chin.
# Skeleton: body (0, 0, 0.28), head (0, -0.2, 0.22), legs' roots at z 0.22 (x 0.14, y -0.12 and 0.14), as Avalugg's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, boxy, hull, slab, polygon, args, sibling

bm = sibling("bergmite")
crystal, jitter = bm.crystal, bm.jitter

# --- colours ----------------------------------------------------------------------------------------------------------
TOP, TOP2 = "#D2E8F6", "#C3DFF3"
CRACK = "#6C5A50"
ROCK, ROCK2 = "#8E7E74", "#7C6C64"
BAND, PAW, CLAW = "#C27A4C", "#B4683E", "#E6EAF0"
JAW, BLOTCH, BLOTCH_RIM = "#E8EDF3", "#BCD8EE", "#5A98C8"
SOCKET, IRIS, PUPIL, INK = "#6E80A2", "#F4C22E", "#1C1C24", bm.INK

# --- the frame --------------------------------------------------------------------------------------------------------
HALF_W = 0.18
SLAB_BOTTOM = 0.3
# the plates, front to back: (front edge's y at the middle, back edge's y, top, x from, x to); narrowing to the back
PLATES = [(-0.345, -0.2, 0.345, -HALF_W, HALF_W),
          (-0.212, -0.06, 0.362, -HALF_W, HALF_W),
          (-0.072, 0.08, 0.38, -HALF_W + 0.006, HALF_W - 0.004),
          (0.068, 0.215, 0.398, -0.15, 0.158),
          (0.203, 0.335, 0.414, -0.1, 0.12)]
SLANT = 0.024
HEAD_C, HEAD_R = np.array([0.0, -0.29, 0.272]), (0.1, 0.075, 0.046)
EYE_DIR = unit(0.5, -0.84, 0.2)
EYE_HALF = (0.03, 0.018)
VOXEL = 0.0038
# the jaw's wedge: its top's four corners and its point low in front
JAW_PTS = np.array([(-0.165, -0.19, 0.25), (0.165, -0.19, 0.25), (-0.135, -0.45, 0.262), (0.135, -0.45, 0.262),
                    (0.0, -0.39, 0.03)])


def edge(xs, y, seed, amp=0.008):
    rs = np.random.RandomState(seed)
    return [(x, y + SLANT * x / HALF_W + (rs.uniform(-amp, amp) if 0 < i < len(xs) - 1 else 0))
            for i, x in enumerate(xs)]


def plate_outline(i):
    y0, y1, top, x0, x1 = PLATES[i]
    n = 7
    xs = list(np.linspace(x0, x1, n))
    front = edge(xs, y0, 10 + i, 0.011)
    back = edge(xs[::-1], y1, 20 + i, 0.012)
    rs = np.random.RandomState(30 + i)
    def side(x, ya, yb):
        ys = np.linspace(ya, yb, 4)[1:-1]
        return [(x + rs.uniform(-0.012, 0.006) * np.sign(x), y) for y in ys]
    c = 0.02
    pts = front[:]
    pts[0] = (x0 + c * 0.6, front[0][1] + c * 0.2)
    pts[-1] = (x1 - c * 0.6, front[-1][1] + c * 0.2)
    pts += side(x1, front[-1][1], back[0][1])
    pts += back
    pts += side(x0, back[-1][1], front[0][1])
    return pts


def top(k):
    for i, (y0, y1, z, x0, x1) in enumerate(PLATES):
        out_ = plate_outline(i)
        thick = z - SLAB_BOTTOM
        k.add(f"plate{i}", slab(out_, (0, 0, z - thick / 2), (1, 0, 0), (0, 1, 0), thick),
              TOP if i % 2 == 0 else TOP2, "body", fuse=False, surface="shell")
        if i == 0:
            continue
        # the crack before each step: brown rock showing under the step and along its foot
        lower = PLATES[i - 1][2]
        front = out_[:7]
        strip = [(x, y - 0.022) for x, y in front] + [(x, y + 0.004) for x, y in front[::-1]]
        h = z - 0.004 - (lower - 0.004)
        k.add(f"crack{i}", slab(strip, (0, 0, lower - 0.004 + h / 2), (1, 0, 0), (0, 1, 0), h), CRACK, "body",
              fuse=False, surface="shell")


def underside(k):
    """The body of brown-grey rock under the plates, low enough that the legs are short."""
    pts = []
    for z, sx, sy in ((0.3, 0.165, 0.31), (0.23, 0.17, 0.3), (0.14, 0.14, 0.25), (0.105, 0.09, 0.16)):
        for a in range(10):
            th = 2 * math.pi * (a + 0.5 * (z < 0.2)) / 10
            pts.append((sx * math.sin(th), 0.02 - sy * math.cos(th), z))
    k.add("under", hull(jitter(pts, 0.012, 7)), ROCK, "body", fuse=False, surface="shell")
    for j, (c, d) in enumerate((((0.15, -0.02, 0.22), (1, -0.2, -0.3)), ((-0.15, 0.05, 0.21), (-1, 0.2, -0.35)),
                                ((0.13, 0.22, 0.2), (0.8, 0.5, -0.3)), ((-0.13, -0.18, 0.2), (-0.8, -0.4, -0.3)))):
        c = np.array(c); d = unit(*d)
        k.add(f"block{j}", crystal(c - d * 0.02, c + d * 0.045, [(0, 0.04), (0.7, 0.035), (1, 0.014)], n=5,
                                   turn=10 * j), ROCK2, "body", fuse=False, surface="shell")


def legs(k):
    for name, y, bone in (("leg", -0.12, "leg{s}"), ("hind", 0.14, "hind{s}")):
        for s, side in ((1, "R"), (-1, "L")):
            b = bone.format(s=side)
            x = 0.14 * s
            # the leg: a thick column of rock, then the band, then the orange-brown paw
            k.add(f"{name}{side}", crystal((x, y, 0.27), (x + 0.014 * s, y, 0.11),
                                           [(0, 0.08), (0.5, 0.085), (1.0, 0.078)], n=7, turn=15),
                  ROCK, b, fuse=False, surface="shell")
            k.add(f"{name}band{side}", crystal((x + 0.014 * s, y, 0.128), (x + 0.014 * s, y, 0.1),
                                               [(0, 0.086), (1, 0.086)], n=7, turn=15),
                  BAND, b, fuse=False, surface="shell")
            k.add(f"{name}paw{side}", crystal((x + 0.014 * s, y, 0.105), (x + 0.016 * s, y - 0.01, 0.0),
                                              [(0, 0.08), (0.55, 0.09), (1.0, 0.082)], n=7, turn=30),
                  PAW, b, fuse=False, surface="shell")
            # three white claws pointing forward and down
            for t in range(3):
                a = math.radians(-35 + 35 * t)
                d = np.array([math.sin(a) * s, -math.cos(a), 0.0])
                root = np.array([x + 0.016 * s, y - 0.01, 0.035]) + d * 0.07
                tip = root + d * 0.035 + np.array([0, 0, -0.035])
                k.add(f"{name}claw{t}{side}", crystal(root, tip, [(0, 0.02), (0.4, 0.018), (1, 0)], n=4, turn=45),
                      CLAW, b, fuse=False, surface="shell")


def on_head(d):
    d = np.array(d, float)
    t = 1 / math.sqrt(sum((d[i] / HEAD_R[i]) ** 2 for i in range(3)))
    return HEAD_C + d * t


def blotch(k, name, tri, at, r, seed):
    """A pale blue blotch of ice outlined in blue, laid flat on a face of the wedge (two thin loose plates)."""
    a, b, c = (np.array(p, float) for p in tri)
    p = a * at[0] + b * at[1] + c * (1 - at[0] - at[1])
    n = np.cross(b - a, c - a); n /= np.linalg.norm(n)
    if np.dot(n, p - JAW_PTS.mean(axis=0)) < 0:
        n = -n
    ex = b - a; ex -= n * np.dot(ex, n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    rs = np.random.RandomState(seed)
    th = np.linspace(0, 2 * math.pi, 9, endpoint=False)
    rr = r * (1 + rs.uniform(-0.3, 0.3, len(th)))
    pts = [(math.cos(t) * q, math.sin(t) * q) for t, q in zip(th, rr)]
    rim = [(x * 1.22, y * 1.22) for x, y in pts]
    k.add(name + "rim", slab(rim, p + n * 0.003, ex, ez, 0.003), BLOTCH_RIM, "head", fuse=False, surface="shell")
    k.add(name, slab(pts, p + n * 0.0046, ex, ez, 0.003), BLOTCH, "head", fuse=False, surface="shell")


def head(k):
    # the slate-blue head under the front plate, the eyes peering over the jaw
    k.add("head", ell(HEAD_C, HEAD_R, deform=boxy(2.6)), SOCKET, "head", surface="shell")
    # the jaw: a white wedge of ice coming to a point low in front
    k.add("jaw", hull(JAW_PTS), JAW, "head", fuse=False, surface="shell")
    tl, tr, fl, fr, ap = JAW_PTS
    blotch(k, "blotchF0", (fl, fr, ap), (0.42, 0.38), 0.03, 1)
    blotch(k, "blotchF1", (fl, fr, ap), (0.2, 0.2), 0.018, 2)
    for s, side in ((1, "R"), (-1, "L")):
        top_, front_ = (tr, fr) if s > 0 else (tl, fl)
        blotch(k, f"blotchS0{side}", (top_, front_, ap), (0.35, 0.45), 0.026, 3 + (s > 0))
        blotch(k, f"blotchS1{side}", (top_, front_, ap), (0.55, 0.12), 0.016, 5 + (s > 0))
        # a little white spike at each upper front corner
        root = front_ + np.array([-0.02 * s, 0.03, -0.004])
        k.add(f"spike{side}", crystal(root, root + np.array([0.012 * s, 0.01, 0.045]),
                                      [(0, 0.016), (0.4, 0.014), (1, 0)], n=4, turn=45),
              JAW, "head", fuse=False, surface="shell")


# --- the eyes: small hard almonds of yellow under the plate, slanting down to the inner corner -------------------------
EYE_PTS = [(-1, -0.05), (-0.7, -0.55), (-0.1, -0.85), (0.5, -0.75), (0.95, -0.25), (1.0, 0.25), (0.72, 0.72),
           (0.1, 0.55), (-0.5, 0.32), (-0.95, 0.16)]
OUTLINE = polygon(EYE_PTS)


def eye_open():
    lid, iris, pup = srgb(SOCKET), srgb(IRIS), srgb(PUPIL)
    def look(u, v):
        th = np.arctan2(v, u)
        rr = np.sqrt(u * u + v * v) / np.array([OUTLINE(t) for t in th])
        c = np.tile(iris, (len(u), 1))
        c[((u - 0.05) / 0.3) ** 2 + ((v + 0.02) / 0.55) ** 2 < 1] = pup
        c[rr > 0.8] = srgb(INK)
        return c
    return look


def eyes(k, state):
    look = eye_look(state, eye_open(), srgb(SOCKET), srgb(INK), line=0.17)
    for s in (1, -1):
        d = EYE_DIR * np.array([s, 1.0, 1.0])
        if s > 0:
            outline, f = OUTLINE, look
        else:
            outline = lambda th: OUTLINE(math.pi - th)
            f = lambda u, v: look(-u, v)
        k.decal(on_head(d), d, EYE_HALF, f, eps=0.0026, outline=outline, rings=20, segs=56)


def avalugg_hisui(src, out):
    k = Kit(src, coat="shell")
    top(k)
    underside(k)
    legs(k)
    head(k)
    k.build(voxel=VOXEL, band=0.004, wband=0.03)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    avalugg_hisui(*args())
