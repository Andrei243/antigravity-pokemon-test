# Morunel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/morunel.py -- <the export's Morunel.glb> <out.glb>
#
# From its picture (No. 10115, a sturgeon fry): a small grey fish drawn side-on, a long flat pointed snout (some 0.3 of
# its length), the back dark slate-blue, the flank lighter blue-grey, the belly, the snout's underside and a cheek patch
# pale; a large round eye behind the snout, white with a big dark pupil and a glint; a little smile under it; four short
# barbels hanging under the snout; a row of small pointed plates along the back and a row of oval plates along each
# flank, shrinking toward the tail; a big pectoral fin pair, a small pelvic pair and a small dorsal fin near the tail;
# and a shark's tail whose upper lobe runs long and up, the lower short.
#
# The fish is built straight along -Y in its own frame and then turned YAW degrees about the vertical, its head toward
# the model's -X, so that the game's front camera sees the +X flank and the back camera the -X flank, each some 45
# degrees off broadside (veluza.py's way); both sides are therefore made by hand (add2, eye and plate patches).
# Belugrand grows from this script's frame (sibling("morunel")).
# Joints: body (0, 0.02, 0.136), head (0, -0.1, 0.136), tail (0, 0.18, 0.146), finR/L (+-0.05, -0.05, 0.086).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, tube, ell, hull, slab, srgb, inside, unit, eye_look, args

# --- the line's palette and frame ----------------------------------------------------------------------------------
BACK, FLANK, PALE, PLATE, FIN, INK, WHITE, MOUTH = ("#5F7082", "#8798A8", "#CDD5DC", "#6A7A8A", "#6C7E90",
                                                    "#16181E", "#F7F8F6", "#3C3F4A")
YAW = -15.0                         # degrees about Z: the head toward -X
PIVOT = np.array([0.0, 0.0, 0.0])
LIFT = 0.016                        # the whole fish raised a little, so that its fins stay over the export's lowest

_ROT = {}


def rot(yaw=None):
    a = math.radians(YAW if yaw is None else yaw)
    return np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1.0]])


def W(p):
    """A point (or points) of the fish's own frame in the model's."""
    p = np.asarray(p, float)
    return (p - PIVOT) @ rot().T + PIVOT + np.array([0, 0, LIFT])


def L(P):
    """The model's points in the fish's own frame (for paint masks)."""
    return (np.asarray(P, float) - PIVOT - np.array([0, 0, LIFT])) @ rot() + PIVOT


def turned(shape):
    V, F = shape
    return W(V), F


def mirrored(shape):
    V, F = shape
    V = np.array(V, float) * np.array([-1, 1, 1.0])
    return V, [f[::-1] for f in F]


def add2(k, name, shape, color, bone, **kw):
    """Both sides by hand, built on +X in the fish's frame, then turned."""
    k.add(name + "R", turned(shape), color, bone.format(s="R"), **kw)
    k.add(name + "L", turned(mirrored(shape)), color, bone.format(s="L"), **kw)


def decal2(k, c, n, half, look, **kw):
    """A patch on both flanks by hand: c and n on +X in the fish's frame, u toward the tail on both sides."""
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal(W(np.asarray(c, float) * m), rot() @ (unit(*n) * m), half,
                look if (s > 0 or not callable(look)) else (lambda u, v, f=look: f(-u, v)), **kw)


def line_at(line, y):
    """A centre line's height and half depth at y: line is [(y, zc, r)...]."""
    ys = [p[0] for p in line]
    return np.interp(y, ys, [p[1] for p in line]), np.interp(y, ys, [p[2] for p in line])


def fish_tube(line, width, seg=28):
    """A tube along the fish's length (line [(y, zc, r)...]), squashed across by width(y) (a factor of r)."""
    V, F = tube([(0, y, z) for y, z, _ in line], [r for _, _, r in line], seg=seg)
    V = V.copy()
    V[:, 0] *= width(V[:, 1])
    return V, F


def kite(root, along, up, length, height, width):
    """A pointed bony plate: a base of length along `along`, its point up (and leaning back), width across."""
    r = np.asarray(root, float); a = unit(*along); u = unit(*up)
    x = np.cross(a, u); x /= np.linalg.norm(x)
    pts = [r - a * length * 0.5, r + a * length * 0.5, r + x * width * 0.5, r - x * width * 0.5,
           r - u * 0.004, r + u * height + a * length * 0.3]
    return hull(pts)


def board(k):
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    s = 0.853 * w + 0.522 * l
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f (corner sum %.3f) in a square of %.3f: %.2f of it" % (np.ptp(proj), s, sq,
                                                                                       np.ptp(proj) / sq))


# --- the eyes: large, round, white with a dark rim, a big dark pupil toward the snout and a glint -----------------------
CI, CW, CG = srgb(INK), srgb(WHITE), srgb("#FFFFFF")


def eye_open(u, v, pupil=0.56, rim=0.86):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CW, (len(u), 1))
    c[r > rim] = CI
    pr = np.sqrt((u + 0.14) ** 2 + (v + 0.02) ** 2)          # u is toward the tail: the pupil looks forward
    c[pr < pupil] = CI
    c[((u + 0.0) ** 2 + (v - 0.24) ** 2) < 0.17 ** 2] = CG
    return c


# --- Morunel's body -------------------------------------------------------------------------------------------------
# (y, centre height, half depth), measured off the picture (800 px of length to 0.73)
LINE = [(-0.372, 0.070, 0.008), (-0.34, 0.071, 0.018), (-0.30, 0.075, 0.031), (-0.23, 0.086, 0.050),
        (-0.161, 0.102, 0.070), (-0.075, 0.123, 0.083), (-0.01, 0.133, 0.085), (0.062, 0.136, 0.074),
        (0.15, 0.143, 0.052), (0.22, 0.156, 0.033), (0.27, 0.168, 0.024)]


def width(y):
    """Across, as a share of the half depth: the snout flat and wide, the body a little narrower than deep."""
    return np.interp(y, [-0.38, -0.3, -0.2, -0.12, 0.0, 0.3], [1.9, 1.55, 1.1, 0.92, 0.84, 0.8])


def body(k):
    head = [p for p in LINE if p[0] <= -0.05]
    trunk = [p for p in LINE if -0.17 <= p[0] <= 0.16]
    tail = [p for p in LINE if p[0] >= 0.14]
    k.add("head", turned(fish_tube(head, width)), FLANK, "head")
    k.add("trunk", turned(fish_tube(trunk, width)), FLANK, "body")
    k.add("stalk", turned(fish_tube(tail, width)), FLANK, "tail")


def markings(k):
    def dark(P):
        P = L(P)
        zc, r = line_at(LINE, P[:, 1])
        return inside(zc + 0.42 * r - P[:, 2], 0.003)
    k.paint(dark, BACK)

    def belly(P):
        P = L(P)
        zc, r = line_at(LINE, P[:, 1])
        wave = 0.06 * np.sin(P[:, 1] * 30)
        return inside(P[:, 2] - (zc - (0.42 + wave) * r), 0.003)
    k.paint(belly, PALE)

    def cheek(P):            # the pale patch behind the eye, down to the snout's pale underside
        P = L(P)
        zc, r = line_at(LINE, P[:, 1])
        d = ((P[:, 1] + 0.12) / 0.05) ** 2 + ((P[:, 2] - (zc - 0.3 * r)) / (0.55 * r + 1e-6)) ** 2 - 1
        return inside(d, 0.12)
    k.paint(cheek, PALE)

    def gill(P):              # the dark line of the gill cover
        P = L(P)
        zc, r = line_at(LINE, P[:, 1])
        dz = (P[:, 2] - zc) / np.maximum(r, 1e-6)
        yg = -0.072 - 0.018 * dz * dz
        return inside(np.abs(P[:, 1] - yg) - 0.0028, 0.0012) * inside(np.abs(dz) - 0.55, 0.05) * (np.abs(P[:, 0]) > 0.03)
    k.paint(gill, "#3A4450")


def plates(k):
    # along the back: pointed plates leaning back, shrinking toward the tail
    for i in range(7):
        y = -0.115 + 0.04 * i
        zc, r = line_at(LINE, y)
        s = 1 - 0.07 * i
        k.add(f"scute{i}", turned(kite((0, y, zc + r - 0.006), (0, 1, 0), unit(0, 0.45, 1), 0.036 * s, 0.03 * s,
                                       0.02 * s)), PLATE, "body" if y < 0.16 else "tail", fuse=False, surface="shell")
    # along each flank: oval plates, shrinking toward the tail
    for i in range(8):
        y = -0.055 + 0.034 * i
        zc, r = line_at(LINE, y)
        s = 1 - 0.07 * i
        x = r * width(np.array([y]))[0]
        add2(k, f"side{i}", ell((x - 0.002, y, zc + 0.06 * r), (0.006, 0.012 * s, 0.0085 * s), sub=3), PLATE,
             "body" if y < 0.16 else "tail", fuse=False, surface="shell")


def barbels(k):
    # four short barbels hanging under the snout, a little forward of the mouth
    for x, y in ((0.011, -0.29), (0.027, -0.285)):
        zc, r = line_at(LINE, y)
        top = (x, y, zc - r * 0.75)
        add2(k, f"barbel{int(x * 1000)}", tube([top, (x * 1.1, y + 0.004, top[2] - 0.014), (x * 1.2, y + 0.01, top[2] - 0.026)],
                                                [0.0055, 0.005, 0.0045], seg=10), PALE, "head", fuse=False, surface="skin")


def fins(k):
    m = dict(fuse=False, surface="skin")
    # the pectorals: big, out and down and back, their faces toward the front
    pec = [(0.0, -0.022), (0.035, -0.028), (0.105, 0.012), (0.098, 0.03), (0.0, 0.026)]
    add2(k, "pec", slab(pec, (0.052, -0.055, 0.072), unit(0.62, 0.6, -0.42), (0, -1, 0.1), 0.01), FIN, "fin{s}", **m)
    # a small pelvic pair behind the middle
    pel = [(0.0, -0.014), (0.022, -0.016), (0.05, 0.008), (0.0, 0.016)]
    add2(k, "pel", slab(pel, (0.03, 0.13, 0.1), unit(0.55, 0.65, -0.42), (0, -1, 0.1), 0.008), FIN, "tail", **m)
    # a small dorsal near the tail, leaning a little to +X so that its face shows
    zc, r = line_at(LINE, 0.17)
    dor = [(-0.02, 0.0), (0.022, 0.0), (0.03, 0.04), (0.012, 0.042)]
    k.add("dorsal", turned(slab(dor, (0, 0.17, zc + r - 0.008), (0, 1, 0), unit(0.35, 0, 1), 0.009)), FIN, "tail", **m)
    # the shark's tail: the upper lobe long and up and back, the lower short, a notch at the upper lobe's tip
    tail = [(0.245, 0.15), (0.26, 0.19), (0.32, 0.262), (0.352, 0.292), (0.343, 0.262), (0.358, 0.25),
            (0.318, 0.2), (0.306, 0.17), (0.325, 0.125), (0.31, 0.118), (0.27, 0.14)]
    k.add("caudal", turned(slab([(y, z) for y, z in tail], (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.012)), FIN, "tail", **m)
    # the upper lobe's leading edge is the body itself running up into it
    k.add("lobe", turned(tube([(0, 0.25, 0.175), (0, 0.29, 0.215), (0, 0.325, 0.258)], [0.016, 0.011, 0.007],
                              seg=12)), FLANK, "tail")


def mouth(k):
    # a small smile low under the eye, on each side of the snout's root
    ci = srgb(MOUTH); cp = srgb(PALE)
    def smile(u, v):
        c = np.tile(cp, (len(u), 1))
        c[np.abs(v - (0.25 * u * u - 0.05)) < 0.22] = ci
        return c
    y = -0.215
    zc, r = line_at(LINE, y)
    x = r * width(np.array([y]))[0]
    decal2(k, (x * 0.72, y, zc - 0.62 * r), unit(0.6, -0.2, -0.75), (0.026, 0.008), smile, eps=0.0025, rings=8,
           segs=40)


EYE_Y = -0.158
EYE_HALF = (0.026, 0.026)


def eye_place():
    zc, r = line_at(LINE, EYE_Y)
    x = r * width(np.array([EYE_Y]))[0]
    n = unit(0.78, -0.5, 0.32)
    return np.array([x * 0.92, EYE_Y, zc + 0.12 * r]), n


def eyes(k, state):
    look = eye_look(state, eye_open, srgb(FLANK), CI, line=0.16)
    c, n = eye_place()
    decal2(k, c, n, EYE_HALF, look, eps=0.002, rings=20, segs=56)


def morunel(src, out):
    k = Kit(src, coat="scales")
    body(k)
    markings(k)
    plates(k)
    barbels(k)
    fins(k)
    mouth(k)
    k.build(voxel=0.0034, smooth=4, band=0.004, wband=0.03)
    board(k)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    morunel(*args())
