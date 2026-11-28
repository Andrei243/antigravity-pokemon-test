# Iepurel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/iepurel.py -- <the export's Iepurel.glb> <out.glb>
#
# From the picture (docs/art/reference/gemini/carpathia/10037-Iepurel.jpg) and its brief (CP-dex-1b-plateau-more.md,
# 37): a young brown field hare crouched low, the masses largest first: a long body arched at the shoulders, great
# round haunches fused into its flanks over long flat hind feet; a head carried forward and up, with a pale sandy
# muzzle, a small dark nose, big dark eyes in pale rings and fluffy cheeks; two long broad ears, pink-brown inside and
# black at the tips, the one standing tall and the other flopped out sideways; a pale sandy bib down the chest and a
# sandy belly; straight forelegs with small paws; a fluffy tail turned up behind, brown above and sandy under.
# The ears: the picture's near ear stands and its far one flops; the game's front camera sees the +X flank, so the
# standing ear is +X (earR) and the flopped one -X (earL), flopping outward and a little forward.
# Written to be grown from: iepuroi.py imports it with sibling("iepurel") (the palette, the ear, the eye, the muzzle).
# Skeleton: body (0, 0.02, 0.2), head (0, -0.11, 0.29), ears (+-0.03, -0.11, 0.39), legs (+-0.055, -0.09, 0.2),
# hinds (+-0.09, 0.1, 0.17), tail (0, 0.19, 0.22). Height about 0.70 to the ear's tip.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, unit, inside, eye_look, polygon, lumpy, args

# --- the line's colours ----------------------------------------------------------------------------------------------
BROWN = "#B3804D"
SAND = "#E8CFA0"
LINING = "#C98E7E"
TIP = "#2A221C"
NOSE = "#5A3A30"
EYE_IRIS, EYE_PUPIL, GLINT = srgb("#3C3E4C"), srgb("#111116"), np.array([1.0, 1.0, 1.0])
INK = srgb("#3A2618")

# --- Iepurel's frame -------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.155, 0.395])
HR = np.array([0.094, 0.1, 0.086])
MC = np.array([0.0, -0.232, 0.364])         # the muzzle
MR = np.array([0.05, 0.044, 0.04])
EYE_D = (0.62, -0.72, 0.26)
EYE_HALF = (0.026, 0.029)
RING_HALF = (0.031, 0.033)
EAR_UP = [(0.03, -0.125, 0.45), (0.042, -0.125, 0.53), (0.055, -0.12, 0.62), (0.062, -0.115, 0.688)]
EAR_UP_R = [0.022, 0.035, 0.033, 0.02]
EAR_FLOP = [(-0.03, -0.125, 0.45), (-0.046, -0.13, 0.54), (-0.088, -0.148, 0.602), (-0.142, -0.162, 0.6),
            (-0.186, -0.172, 0.566)]
EAR_FLOP_R = [0.022, 0.034, 0.033, 0.029, 0.018]


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along the direction d from its centre, and its normal there."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def along(P, pts):
    """For points P: the share along a polyline (0 at its root, 1 at its end), and the offset from its nearest point."""
    pts = np.array(pts, float)
    seg = np.diff(pts, axis=0)
    L = np.linalg.norm(seg, axis=1)
    cum = np.concatenate([[0], np.cumsum(L)])
    best_d = np.full(len(P), np.inf); best_t = np.zeros(len(P)); best_o = np.zeros_like(P)
    for i in range(len(seg)):
        s = np.clip(((P - pts[i]) @ seg[i]) / (L[i] ** 2), 0, 1)
        q = pts[i] + s[:, None] * seg[i]
        o = P - q
        d = np.linalg.norm(o, axis=1)
        m = d < best_d
        best_d[m] = d[m]; best_t[m] = (cum[i] + s[m] * L[i]) / cum[-1]; best_o[m] = o[m]
    return best_t, best_o, best_d


def ear(k, name, pts, radii, bone, flat=0.42, colour=BROWN):
    """A long hare's ear: a broad blade flat to the front (fused), its lining and black tip painted."""
    k.add(name, blade(pts, radii, (0, 1, 0), flat), colour, bone)
    rmax = max(radii)
    def lining(P):
        t, o, d = along(P, pts)
        front = inside(o[:, 1] + 0.35 * rmax * flat, 0.0025)            # the front face (toward -Y)
        mid = inside(np.hypot(o[:, 0], o[:, 2]) - rmax * 0.62, 0.003)
        return front * mid * inside(0.14 - t, 0.02) * inside(t - 0.8, 0.02) * inside(d - rmax * 1.3, 0.002)
    def tip(P):
        t, o, d = along(P, pts)
        return inside(0.8 - t, 0.012) * inside(d - rmax * 1.5, 0.002)
    k.paint(lining, LINING)
    k.paint(tip, TIP)


def board(ob):
    """The game board's check: what the model's corners show turned 31.5 degrees, against the square it is framed in."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def eye_open(iris=EYE_IRIS, pupil=EYE_PUPIL):
    """The line's eye: near black, a slate iris round a black pupil, a large white glint high and outward."""
    def look(u, v):
        c = np.tile(iris, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.05) / 0.66) ** 2 < 1] = pupil
        c[((u - 0.24) / 0.32) ** 2 + ((v - 0.36) / 0.28) ** 2 < 1] = GLINT
        c[((u + 0.3) / 0.13) ** 2 + ((v + 0.44) / 0.11) ** 2 < 1] = GLINT
        return c
    return look


def face(k, hc, hr, mc, mr, eye_d, ring_half, nose_half=(0.016, 0.012), mouth_half=(0.022, 0.016), mouth=None):
    """The rings round the eyes, the nose and the mouth: what every eye state keeps (laid before build)."""
    p, n = on_ell(hc, hr, eye_d)
    k.decal(p, n, ring_half, SAND, eps=0.0022, rings=10, segs=48, mirror=True)
    np_, nn = on_ell(mc, mr, (0, -0.72, 0.7))
    k.decal(np_, nn, nose_half, NOSE, eps=0.0025, outline=polygon([(-1, 0.75), (1, 0.75), (0, -1)]), rings=6,
            segs=30)
    CI, CB = srgb(NOSE), srgb(SAND)
    def y_mouth(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(np.abs(u) < 0.14) & (v > 0.0)] = CI
        c[(np.abs(v - (0.05 - 0.55 * np.abs(u))) < 0.2) & (np.abs(u) < 0.62)] = CI
        return c
    mp, mn = on_ell(mc, mr, (0, -0.95, -0.15))
    k.decal(mp, mn, mouth_half, mouth or y_mouth, eps=0.0025, rings=8, segs=40)


def tuft(k, name, c, d, length, r, colour, bone, mirror=False):
    """A short soft point of fur standing out of a surface (fused)."""
    c = np.array(c, float); d = unit(*d)
    k.add(name, tube([c - d * r * 0.4, c + d * length * 0.55, c + d * length], [r, r * 0.6, r * 0.2]), colour, bone,
          mirror=mirror)


def iepurel(src, out):
    k = Kit(src, coat="fur")
    # The body arched at the shoulders, the shoulders' mass, the neck
    k.add("body", ell((0, 0.03, 0.222), (0.096, 0.168, 0.102), rot=(-9, 0, 0)), BROWN, "body")
    k.add("shoulders", ell((0, -0.07, 0.262), (0.086, 0.082, 0.095)), BROWN, "body")
    k.add("neck", ell((0, -0.12, 0.322), (0.07, 0.068, 0.072)), BROWN, "head")
    # Great round haunches fused into the flanks, the heels behind, long flat hind feet
    k.add("haunch", ell((0.082, 0.098, 0.182), (0.06, 0.108, 0.106), rot=(-18, 0, 0)), BROWN, "hind{s}", mirror=True)
    k.add("heel", tube([(0.088, 0.155, 0.11), (0.092, 0.158, 0.06), (0.093, 0.15, 0.03)], [0.034, 0.03, 0.026]),
          BROWN, "hind{s}", mirror=True)
    k.add("hindfoot", ell((0.093, 0.06, 0.024), (0.035, 0.108, 0.024)), BROWN, "hind{s}", mirror=True)
    # Straight forelegs a third thicker than drawn, small paws
    k.add("foreleg", tube([(0.052, -0.098, 0.205), (0.056, -0.128, 0.11), (0.058, -0.142, 0.042)],
                          [0.031, 0.025, 0.024]), BROWN, "leg{s}", mirror=True)
    k.add("paw", ell((0.058, -0.158, 0.022), (0.028, 0.042, 0.022)), BROWN, "leg{s}", mirror=True)
    # The head carried forward, the sandy muzzle, fluffy cheeks
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("muzzle", ell(MC, MR), SAND, "head")
    for i, (d, ln) in enumerate((((0.95, 0.15, -0.35), 0.03), ((0.85, 0.35, -0.6), 0.026), ((0.9, -0.05, -0.55), 0.024))):
        p, n = on_ell(HC, HR, d)
        tuft(k, f"cheek{i}", p - n * 0.006, n + np.array([0, 0.45, -0.25]), ln, 0.015, BROWN, "head", mirror=True)
    # A tuft or two at the back of the neck and the shoulder
    for i, (c, d) in enumerate((((0, -0.09, 0.355), (0, 0.6, 0.8)), ((0.07, -0.04, 0.3), (0.8, 0.5, 0.2)))):
        tuft(k, f"ruff{i}", c, d, 0.026, 0.016, BROWN, "body", mirror=(i == 1))
    # The fluffy tail turned up behind
    k.add("tail", ell((0, 0.24, 0.272), (0.042, 0.042, 0.048), rot=(-30, 0, 0), deform=lumpy(5, 3, 0.18)), BROWN,
          "tail")
    # The ears
    ear(k, "earR", EAR_UP, EAR_UP_R, "earR")
    ear(k, "earL", EAR_FLOP, EAR_FLOP_R, "earL")
    # The sandy bib down the chest, the sandy belly and the tail's underside, the muzzle's edge crisp
    k.paint(lambda P: inside(np.sqrt((P[:, 0] / 0.058) ** 2 + ((P[:, 1] + 0.17) / 0.085) ** 2
                                     + ((P[:, 2] - 0.245) / 0.105) ** 2) - 1, 0.05) * inside(P[:, 2] - 0.335, 0.004),
            SAND)
    k.paint(lambda P: inside(P[:, 2] - 0.135, 0.004) * inside(0.07 - P[:, 2], 0.004) * inside(np.abs(P[:, 0]) - 0.05, 0.006)
            * inside(P[:, 1] - 0.07, 0.01) * inside(-0.12 - P[:, 1], 0.01), SAND)
    k.paint(lambda P: inside(P[:, 2] - 0.262, 0.004) * inside(0.21 - P[:, 1], 0.004), SAND, on=["tail"])
    face(k, HC, HR, MC, MR, EYE_D, RING_HALF)
    k.build(voxel=0.0052, smooth=4, band=0.008, wband=0.045)
    board(k.ob)

    def eyes(state):
        p, nrm = on_ell(HC, HR, EYE_D)
        k.decal(p, nrm, EYE_HALF, eye_look(state, eye_open(), srgb(SAND), INK, line=0.16), eps=0.0034, rings=20,
                segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    iepurel(*args())
