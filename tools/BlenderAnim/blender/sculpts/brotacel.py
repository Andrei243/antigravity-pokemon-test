# Brotacel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/brotacel.py -- <the export's Brotacel.glb> <out.glb>
#
# Carpathia's tree frog (No. 10119, Water), from its picture (docs/art/reference/gemini/carpathia/10119-Brotacel.jpg)
# and its brief (CP-dex-3-river-and-caves.md, 119): a tiny bright-green frog sitting up, its body leaning forward
# over folded hind legs; a cream belly; a dark brown stripe edged in cream running from the snout through the eye and
# back along each flank to the rump; big eyes on bulges at the head's upper corners, a tan iris round a large black
# pupil; a thin mouth line across the face; under the chin its throat sac puffed into a pale blue bubble as big as
# half the head; four round sticky pads on every foot. The reed it perches on in the picture is scenery and is left
# out: the frog sits on the ground.
# The export's joints: body (0, 0.02, 0.156), head (0, -0.06, 0.206), front legs (+-0.05, -0.05, 0.136) on leg{s},
# hind legs (+-0.08, 0.08, 0.106) on hind{s}.
# Written to be grown from: oracul.py imports it (sibling("brotacel")) for the palette, the eye, the padded toes,
# the stripe and the board check.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, args

GREEN, GREEN_DARK, CREAM = "#8CC84A", "#6FA83A", "#F1E7C2"
STRIPE, MOUTH, SAC, SAC_LIGHT = "#4B3D2C", "#3C3226", "#9FD2EA", "#E8F6FC"
INK, IRIS, PUPIL = "#24201C", "#D3BE86", "#141210"

VOXEL = 0.0029


# --- the family's helpers ------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid along a direction from its middle, and the surface's normal there."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / np.linalg.norm(d / r)
    n = (p - c) / (r * r)
    return p, n / np.linalg.norm(n)


def seg_dist(Y, Z, a, b):
    """Distance in the (y, z) plane from points to the segment a-b."""
    a, b = np.array(a, float), np.array(b, float)
    e = b - a
    t = np.clip(((Y - a[0]) * e[0] + (Z - a[1]) * e[1]) / (e @ e), 0, 1)
    return np.hypot(Y - (a[0] + t * e[0]), Z - (a[1] + t * e[1]))


def line_dist(Y, Z, pts):
    return np.min([seg_dist(Y, Z, a, b) for a, b in zip(pts, pts[1:])], axis=0)


def stripe(k, pts, half, xmin, edge=None, below=None, top=None):
    """The line's stripe: a dark band along a path in the side's (y, z) plane on both sides, and a cream line under
    it. pts the path, half the band's half width, xmin how far out a point must be."""
    def side(P):
        return inside(xmin - np.abs(P[:, 0]), 0.002)
    lim = (lambda P: inside(P[:, 2] - top, 0.002)) if top else (lambda P: 1.0)
    if edge:
        under = [(y, z - half - edge * 0.6) for y, z in pts]
        k.paint(lambda P: inside(line_dist(P[:, 1], P[:, 2], under) - edge, 0.0018) * side(P) * lim(P), CREAM)
    k.paint(lambda P: inside(line_dist(P[:, 1], P[:, 2], pts) - half, 0.0018) * side(P) * lim(P), STRIPE)


def frog_eye(iris=IRIS, pupil_r=0.56, rim=0.17, glint=(-0.3, 0.32, 0.2)):
    """The line's eye: a dark rim, an iris, a big black pupil and a white glint (u outward, v up)."""
    K, I, P, W = srgb(INK), srgb(iris), srgb(PUPIL), np.ones(3)
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(I, (len(u), 1))
        c[r > 1 - rim] = K
        c[np.sqrt((u + 0.02) ** 2 + v * v) < pupil_r] = P
        c[np.sqrt((u - glint[0]) ** 2 + (v - glint[1]) ** 2) < glint[2]] = W
        return c
    return look


def pads(k, name, base, dirs, length, r, pad, bone, colour=GREEN):
    """Toes from a foot's middle along directions, each ending in a round sticky pad."""
    base = np.array(base, float)
    for i, d in enumerate(dirs):
        d = unit(*d)
        tip = base + d * length
        k.add(f"{name}{i}", tube([base, base + d * length * 0.55, tip], [r * 1.15, r, r * 0.95], seg=12, per=4),
              colour, bone, mirror=True)
        k.add(f"{name}pad{i}", ell(tip + d * pad * 0.3 + np.array([0, 0, pad * 0.1]), (pad, pad, pad * 0.8)), colour,
              bone, mirror=True)


def board(k):
    """The game's board check on the built body: the span it shows at the board's turn against its square."""
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); four corners %.3f (%.2f)" %
          (np.ptp(proj), sq, np.ptp(proj) / sq, 0.853 * w + 0.522 * l, (0.853 * w + 0.522 * l) / sq))


# --- Brotacel's frame ----------------------------------------------------------------------------------------------
BC, BR, BROT = np.array([0.0, 0.03, 0.118]), np.array([0.084, 0.088, 0.112]), (24, 0, 0)
HC, HR = np.array([0.0, -0.045, 0.2]), np.array([0.088, 0.074, 0.066])
BUMP_C, BUMP_R = np.array([0.05, -0.052, 0.238]), np.array([0.034, 0.032, 0.032])
EYEDIR = unit(0.62, -0.62, 0.48)
EYE_HALF = 0.025
SAC_C, SAC_R = np.array([0.012, -0.152, 0.108]), np.array([0.084, 0.058, 0.07])
STRIPE_PATH = [(-0.072, 0.238), (-0.03, 0.234), (0.01, 0.205), (0.05, 0.15), (0.09, 0.09),
               (0.125, 0.05)]
SNOUT_PATH = [(-0.118, 0.222), (-0.075, 0.239)]


def body(k):
    k.add("body", ell(BC, BR, rot=BROT), GREEN, "body")
    k.add("rump", ell((0.0, 0.08, 0.07), (0.078, 0.07, 0.065)), GREEN, "body")
    k.add("head", ell(HC, HR), GREEN, "head")
    k.add("snout", ell((0.0, -0.088, 0.196), (0.066, 0.05, 0.048)), GREEN, "head")
    k.add("bump", ell(BUMP_C, BUMP_R), GREEN, "head", mirror=True)


def legs(k):
    # front legs straight down before the chest, hands spread on the ground
    sh, el, wr = np.array([0.05, -0.06, 0.135]), np.array([0.072, -0.092, 0.075]), np.array([0.066, -0.108, 0.016])
    k.add("arm", tube([sh, el, wr], [0.019, 0.016, 0.013]), GREEN, "leg{s}", mirror=True)
    pads(k, "fing", wr + np.array([0, 0, -0.004]), [(0.9, -0.5, -0.05), (0.35, -1, -0.05), (-0.25, -1, -0.05),
                                                    (1.0, 0.1, -0.05)], 0.032, 0.0068, 0.0118, "leg{s}")
    # hind legs folded along the flanks: a fat thigh, the shin back under it, the long foot forward
    k.add("thigh", ell((0.074, 0.07, 0.07), (0.042, 0.072, 0.05), rot=(-18, 0, 8)), GREEN, "hind{s}", mirror=True)
    k.add("shin", tube([(0.1, -0.0, 0.05), (0.106, 0.06, 0.038), (0.1, 0.11, 0.024)], [0.024, 0.02, 0.016]),
          GREEN, "hind{s}", mirror=True)
    k.add("foot", tube([(0.1, 0.11, 0.016), (0.112, 0.05, 0.012), (0.118, -0.012, 0.011)], [0.016, 0.014, 0.012]),
          GREEN, "hind{s}", mirror=True)
    pads(k, "toe", (0.118, -0.012, 0.008), [(0.95, -0.55, -0.05), (0.4, -1, -0.05), (-0.15, -1, -0.05),
                                            (1.0, 0.0, -0.05)], 0.036, 0.007, 0.012, "hind{s}")


def sac(k):
    """The throat sac puffed into a bubble under the chin: a loose ball, its top under the mouth line."""
    k.add("sac", outward(ell(SAC_C, SAC_R)), SAC, "head", fuse=False, surface="shell")
    hp, hn = on_ell(SAC_C, SAC_R, (0.45, -0.65, 0.55))
    k.add("sacglint", outward(ell(hp - hn * 0.006, (0.016, 0.012, 0.011), rot=(0, 30, -30))), SAC_LIGHT, "head",
          fuse=False, surface="shell")


def marks(k):
    # the cream belly and throat, in front under the chest
    def belly(P):
        q = (P - np.array([0.0, -0.055, 0.1])) / np.array([0.066, 0.06, 0.1])
        return inside(np.linalg.norm(q, axis=1) - 1, 0.04)
    k.paint(belly, CREAM, on=["body", "rump"])
    k.paint(lambda P: inside(np.linalg.norm((P - np.array([0, -0.1, 0.165])) / np.array([0.06, 0.04, 0.03]), axis=1)
                             - 1, 0.04), CREAM, on=["head", "snout"])
    # a darker back
    k.paint(lambda P: inside(np.linalg.norm((P - np.array([0, 0.07, 0.19])) / np.array([0.05, 0.08, 0.08]), axis=1)
                             - 1, 0.1) * 0.55, GREEN_DARK, on=["body", "head"])
    stripe(k, STRIPE_PATH, 0.0095, 0.04, edge=0.0042)
    stripe(k, SNOUT_PATH, 0.0042, 0.045)
    # the mouth: a thin dark line across the snout, rising toward the cheeks
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.178 + 1.6 * P[:, 0] ** 2)) - 0.0022, 0.0012)
            * inside(P[:, 1] + 0.06, 0.003) * inside(np.abs(P[:, 0]) - 0.075, 0.003), MOUTH, on=["head", "snout"])


def eyes(k):
    ep, en = on_ell(BUMP_C, BUMP_R, EYEDIR)
    lid, ink = srgb(GREEN), srgb(INK)
    look = frog_eye()
    def lay(state):
        k.decal(ep, en, (EYE_HALF, EYE_HALF), eye_look(state, look, lid, ink, line=0.16), eps=0.0016, rings=20,
                segs=56, mirror=True)
    return lay


def brotacel(src, out_path):
    k = Kit(src, coat="scales")
    body(k)
    legs(k)
    sac(k)
    marks(k)
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.03)
    board(k)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    brotacel(*args())
