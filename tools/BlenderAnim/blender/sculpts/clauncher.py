# Clauncher sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/clauncher.py -- <the export's Clauncher.glb> <out.glb>
#
# From the references: a small light blue shrimp lying low, a round dome of a head in front and a body of shrinking
# segments behind it, dark grey bands between them, a dark grey fan of a tail. Two bored yellow eyes on the front of
# the head, half covered by a straight blue lid; a fringe of short blue points hangs under the face; two long thin
# yellow feelers rise from the top of the head and curve forward. One great claw on its right side (+X), held before
# it: a blue palm banded dark grey at its root, a big blue upper finger curving down to a point and a small yellow
# lower finger curving up under it. Short blue legs under the body.
#
# Written to be grown from (clawitzer.py imports it with sibling): the frame is in the constants and the functions that
# take the kit; the build is under the main guard. Bones: body (0, 0, 0.1), head (-0.02, -0.05, 0.13), armR (0.035,
# -0.04, 0.13), tail (0, 0.056, 0.1), leg{s}, hind{s} (+-0.04, -0.03 / 0.04, 0.07). There is no left arm.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, slab, polygon, lathe

BLUE, BAND, YELLOW = "#62BEEA", "#46484F", "#F2C232"
IRIS, INK, PUPIL = "#F2AE26", "#1E2028", "#121216"

# ---- the frame ----------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([-0.015, -0.06, 0.088]), np.array([0.072, 0.068, 0.07])
SEGS = [((0.0, 0.02, 0.085), (0.066, 0.058, 0.062)), ((0.0, 0.078, 0.083), (0.054, 0.05, 0.052)),
        ((0.0, 0.127, 0.083), (0.042, 0.04, 0.042)), ((0.0, 0.166, 0.086), (0.031, 0.03, 0.032))]
BANDS = (0.0, 0.052, 0.104, 0.148)          # y of the dark bands between the segments
BAND_W = 0.0075
FAN = dict(root=(0.0, 0.186, 0.09), d=(0.42, 0.85, 0.32), L=0.075, W=0.05)


def head(k, c=HEAD_C, r=HEAD_R, color=BLUE):
    k.add("head", ell(c, r), color, "head")
    k.add("chin", ell(c + (0, -0.012, -0.03), r * (0.8, 0.8, 0.55)), color, "head")


def segments(k, segs=SEGS, color=BLUE):
    for i, (c, r) in enumerate(segs):
        k.add(f"seg{i}", ell(c, r), color, "body" if i < 2 else "tail")


def bands(k, ys=BANDS, w=BAND_W, color=BAND, xmax=0.075):
    """Dark grey bands round the body between the segments, painted crisp."""
    def m(P):
        out = np.zeros(len(P))
        for y in ys:
            out = np.maximum(out, inside(np.abs(P[:, 1] - y) - w, 0.002))
        return out * inside(np.abs(P[:, 0]) - xmax, 0.002)
    k.paint(m, color)


def fan(k, root, d, L, W, color=BAND, bone="tail"):
    """The tail's fan: three lobes, run back, up and some 35 degrees out to +X."""
    d = unit(*d)
    ez = np.cross(d, unit(-0.6, 0.8, 0)); ez /= np.linalg.norm(ez)
    outline = [(0, -0.35), (0.55, -1.0), (0.8, -0.7), (0.7, -0.25), (1.0, 0.0), (0.7, 0.25), (0.8, 0.7),
               (0.55, 1.0), (0, 0.35)]
    k.add("fan", slab([(a * L, b * W) for a, b in outline], root, d, ez, 0.016), color, bone, fuse=False,
          surface="shell")


def bristles(k, c=HEAD_C, r=HEAD_R, n=6, length=0.03, w=0.012, color=BLUE, bone="head", spread=0.85):
    """The fringe of short blue points hanging under the face."""
    for i in range(n):
        t = (i - (n - 1) / 2) / ((n - 1) / 2)
        a = math.radians(t * 50 * spread)
        root = c + np.array([math.sin(a) * r[0] * 0.62, -math.cos(a) * r[1] * 0.72, -r[2] * 0.58])
        tip = root + np.array([math.sin(a) * 0.006, -0.008, -length * (1 - 0.25 * abs(t))])
        side = np.array([math.cos(a), math.sin(a), 0.0])
        pts = [root + side * w, root - side * w, root + (0, w * 0.7, 0.003), root + (0, -w * 0.7, 0.003), tip]
        k.add(f"bristle{i}", hull(pts), color, bone, fuse=False, surface="shell")


def feelers(k, c=HEAD_C, r=HEAD_R, path=((0, 0, 0), (0.004, -0.028, 0.048), (0.008, -0.07, 0.082),
                                          (0.01, -0.118, 0.096)), off=0.024, radii=(0.0065, 0.006, 0.0055, 0.005),
            color=YELLOW, bone="head"):
    for sx in (1, -1):
        root = c + np.array([sx * off, 0.01, r[2] * 0.9])
        pts = [root + np.array(p) * np.array([sx, 1, 1]) for p in path]
        k.add("feeler" + ("R" if sx > 0 else "L"), outward(tube(pts, list(radii), seg=10, per=6)), color, bone,
              fuse=False, surface="shell")


LEGS = [("legR", (0.04, -0.03, 0.04), (0.066, -0.04, 0.004)), ("legL", (-0.04, -0.03, 0.04), (-0.066, -0.04, 0.004)),
        ("hindR", (0.04, 0.04, 0.04), (0.062, 0.05, 0.004)), ("hindL", (-0.04, 0.04, 0.04), (-0.062, 0.05, 0.004))]


def legs(k, specs=LEGS, r=(0.014, 0.012, 0.009), color=BLUE):
    for bone, a, b in specs:
        a, b = np.array(a), np.array(b)
        m = (a + b) / 2 + np.array([np.sign(a[0]) * 0.012, 0, 0.012])
        k.add("leg" + bone, outward(tube([a, m, b], list(r), seg=12, per=6)), color, bone, fuse=False,
              surface="shell")


# ---- the claw ---------------------------------------------------------------------------------------------------
CLAW = dict(shoulder=(0.045, -0.045, 0.095), wrist=(0.1, -0.088, 0.097), palm_c=(0.115, -0.13, 0.1),
            palm_r=(0.052, 0.058, 0.056), band_y=-0.092,
            upper=((0.115, -0.16, 0.126), (0.117, -0.205, 0.119), (0.117, -0.238, 0.089), (0.115, -0.25, 0.054)),
            upper_r=(0.042, 0.033, 0.02, 0.008),
            lower=((0.115, -0.16, 0.059), (0.115, -0.2, 0.056), (0.115, -0.225, 0.074)), lower_w=0.021)


def claw(k, shoulder, wrist, palm_c, palm_r, band_y, upper, upper_r, lower, lower_w, color=BLUE, band=BAND,
         jaw=YELLOW, band_r=0.04, band_len=0.022):
    """The claw, all loose pieces: it stands before the face, and a fused claw would catch the eyes' patches."""
    k.add("clawarm", outward(tube([shoulder, wrist], [0.02, 0.024], seg=14, per=4)), color, "armR", fuse=False,
          surface="shell")
    k.add("clawpalm", outward(ell(palm_c, palm_r)), color, "armR", fuse=False, surface="shell")
    k.add("clawupper", outward(tube(list(upper), list(upper_r), seg=16, per=6)), color, "armR", fuse=False,
          surface="shell")
    bc = np.array([palm_c[0], band_y, palm_c[2]])
    k.add("clawband", lathe(bc + (0, band_len / 2, 0), (0, -1, 0),
                            [(0, 0), (band_r, 0), (band_r, band_len), (0, band_len)], seg=40), band, "armR",
          fuse=False, surface="shell")
    # the yellow lower finger: a curved fang of three hulls
    L = [np.array(p, float) for p in lower]
    for i in range(len(L) - 1):
        a, b = L[i], L[i + 1]
        w = lower_w * (1 - 0.4 * i)
        w2 = lower_w * (1 - 0.4 * (i + 1)) if i + 2 < len(L) else 0.002
        pts = [a + (w, 0, 0), a - (w, 0, 0), a + (0, 0, w * 0.8), a - (0, 0, w * 0.8),
               b + (w2, 0, 0), b - (w2, 0, 0), b + (0, 0, w2 * 0.8), b - (0, 0, w2 * 0.8)]
        k.add(f"jaw{i}", hull(pts), jaw, "armR", fuse=False, surface="shell")


# ---- the eyes: bored, yellow, a straight blue lid over their tops --------------------------------------------------
CB, CK, CI, CP = srgb(BLUE), srgb(INK), srgb(IRIS), srgb(PUPIL)
EYE_OFF, EYE_Z = 0.034, 0.008                 # from the head's middle: across, and up
EYE_HALF = (0.021, 0.022)
TOP = lambda u: 0.22 - 0.12 * u              # u toward the inner side here (flipped for each side below)


def clauncher_eye(u, v, top=TOP):
    c = np.tile(CK, (len(u), 1))
    rr = u * u + v * v
    t = top(u)
    c[(rr < 0.72) & (v < t - 0.16)] = CI
    c[((u + 0.08) / 0.2) ** 2 + ((v + 0.12) / 0.3) ** 2 < 1] = CP
    return c


def cut_outline(top=TOP, n=96):
    t = np.linspace(0, 2 * math.pi, n, endpoint=False)
    u, v = 0.98 * np.cos(t), 0.98 * np.sin(t)
    return np.stack([u, np.minimum(v, top(u))], axis=1)


def eye_places(c=HEAD_C, r=HEAD_R, off=EYE_OFF, z=EYE_Z):
    """Each eye's centre and normal on the head's ellipsoid, mirrored about the head's own middle."""
    out_ = []
    for sx in (1, -1):
        p = np.array([sx * off, 0.0, z])
        y = -r[1] * math.sqrt(max(1 - (p[0] / r[0]) ** 2 - (p[2] / r[2]) ** 2, 0.05))
        q = np.array([p[0], y, p[2]])
        n = q / r ** 2; n /= np.linalg.norm(n)
        out_.append((c + q, n, sx))
    return out_


def eyes(k, look=clauncher_eye, places=None, half=EYE_HALF, top=TOP, lid=CB, ink=CK, eps=0.0025):
    places = eye_places() if places is None else places
    OUT = cut_outline(top)

    def lay(state):
        f = look if state == "Open" else eye_look(state, look, lid, ink, line=0.18)
        for c, n, sx in places:
            # hand-laid: +u runs toward +X on both; make it run inward on each
            if sx > 0:
                k.decal(c, n, half, lambda u, v: f(-u, v), eps=eps, rings=20, segs=56,
                        outline=polygon(OUT * np.array([-1, 1])))
            else:
                k.decal(c, n, half, f, eps=eps, rings=20, segs=56, outline=polygon(OUT))
    return lay


def clauncher(src, out):
    k = Kit(src, coat="shell")
    head(k)
    segments(k)
    claw(k, **CLAW)
    bands(k)
    fan(k, **FAN)
    bristles(k)
    feelers(k)
    legs(k)
    k.build(voxel=0.0028, smooth=3, band=0.004, wband=0.025)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    clauncher(*args())
