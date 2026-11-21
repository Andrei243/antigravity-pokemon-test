# Helioptile sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/helioptile.py -- <the export's Helioptile.glb> <out.glb>
# From the references: a small yellow lizard on four short legs, its chest raised and its head the biggest part of it.
# A round yellow head wearing a dark grey hood over its crown and the back of the head, the hood's front edge running
# straight over the eyes with a pointed triangle down between them; two long broad grey flaps hang from the hood's
# sides to the chest, each ending in a square yellow tip. Great round eyes on the face: white with a black rim, a big
# blue iris, a black pupil and a glint; two small nostrils on a short muzzle and a wide thin smile. A small body, a
# thin neck, legs ending in dark three-toed feet; a long tail back and out to the +X side, rising at its end, its tip
# dark grey.
#
# Written to be grown from (heliolisk.py imports it with sibling("helioptile")): the line's colours, its eye, its dark
# toes and its tail with a dark tip are the functions below; the calls that build Helioptile are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, outward, srgb, inside, smoothstep, unit, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
YELLOW = "#F6D73A"
GREY = "#46474E"            # the hood, its flaps and the tail's tip
CLAW = "#34343A"            # the toes
MOUTH = "#2A221C"
NOSTRIL = "#7C6A1E"
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])
BLUE, BLUE_DEEP = srgb("#2E86DA"), srgb("#1556A8")


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def spline(pts, n):
    """A Catmull-Rom curve through pts, n points along it."""
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out_ = []
    for t in np.linspace(0, len(p) - 1, n):
        i = min(int(t), len(p) - 2); f = t - i
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        out_.append(0.5 * ((2 * p1) + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f
                           + (-p0 + 3 * p1 - 3 * p2 + p3) * f ** 3))
    return np.array(out_)


# --- the eye --------------------------------------------------------------------------------------------------------
def blue_eye(rim=0.84, iris=0.64, iris_c=(-0.04, -0.02), pupil=0.27, glint=(0.2, 0.26, 0.17)):
    """The open eye (u outward, v up): white, a black rim, a big blue iris darker at its top, a black pupil and a
    white glint up and out."""
    def eye(u, v):
        rho = np.sqrt(u * u + v * v)
        c = np.tile(WHITE, (len(u), 1))
        ri = np.hypot(u - iris_c[0], v - iris_c[1]) / iris
        w = smoothstep(0.1, 0.9, (v - iris_c[1]) / iris)[:, None]
        c[ri < 1] = (BLUE[None] * (1 - w) + BLUE_DEEP[None] * w)[ri < 1]
        c[np.hypot(u - iris_c[0], v - iris_c[1]) < pupil] = INK
        c[np.hypot(u - glint[0], v - glint[1]) < glint[2]] = WHITE
        c[rho >= rim] = INK
        return c
    return eye


def lay_eyes(k, state, E, de, half, look, lid, ink, up=(0, 0, 1)):
    """Both eyes for a state, +u outward on each."""
    fn = eye_look(state, look, lid, ink, line=0.15)
    m = np.array([-1, 1, 1.0])
    k.decal(E, de, half, fn, up=up, eps=0.002, rings=20, segs=56)
    k.decal(E * m, de * m, half, lambda u, v: fn(-u, v), up=np.array(up) * m, eps=0.002, rings=20, segs=56)


# --- toes and the tail ------------------------------------------------------------------------------------------------
def toes(k, name, bone, base, fwd, length, r, spread=0.55, colour=CLAW):
    """Three short dark toes from base along fwd, splayed, each its own loose tube."""
    f = unit(*fwd)
    side = unit(*np.cross(f, (0, 0, 1)))
    base = np.array(base, float)
    for j, a in enumerate((-spread, 0.0, spread)):
        d = unit(*(f + side * a + np.array([0, 0, -0.15])))
        k.add(f"{name}{j}", outward(tube([base, base + d * length], [r, r * 0.8])), colour, bone, fuse=False,
              surface="shell")


def tail(k, pts, radii, tip_from, colour=YELLOW, tip=GREY, edge=0.0015):
    """A round tail through pts, its end from tip_from (0..1 along it) painted dark with a crisp edge."""
    k.add("tail", tube(pts, radii), colour, "tail")
    C = spline(pts, 60)
    i = int(tip_from * 59)
    cut, d = C[i], unit(*(C[i + 1] - C[i]))
    end = C[-1]
    reach = np.linalg.norm(end - cut) + max(radii) * 1.5
    k.paint(lambda P: inside(-((P - cut) @ d), edge) * inside(np.linalg.norm(P - end, axis=1) - reach, edge), tip)
    return C


# --- Helioptile's frame -----------------------------------------------------------------------------------------------
VOXEL = 0.0028
H = np.array([0.0, -0.035, 0.204])            # the head, the biggest part
HR = np.array([0.075, 0.066, 0.071])
MUZ_C, MUZ_R = H + np.array([0.0, -0.045, -0.04]), np.array([0.046, 0.034, 0.03])
EYE_DIR = unit(0.6, -0.8, 0.16)
EYE_HALF = (0.024, 0.027)


def head(k):
    """The round head, a little wider at the cheeks, a short muzzle at the foot of the face."""
    def cheeks(V):
        V = V.copy(); V[:, 0] *= 1 + 0.07 * np.clip(-V[:, 2], 0, 1); return V
    k.add("head", ell(H, HR, deform=cheeks), YELLOW, "head")
    k.add("muzzle", ell(MUZ_C, MUZ_R), YELLOW, "head")


def hood_mask(P):
    """The grey hood: over the crown and the back of the head; its front edge straight over the eyes with a pointed
    triangle down between them, dropping behind the eyes to the nape."""
    q = (P - H) / HR
    a = np.arctan2(np.abs(q[:, 0]), -q[:, 1])                  # 0 at the face, pi at the nape
    zt = q[:, 2]
    edge = np.interp(a, [0, 0.55, 1.05, 1.5, 2.0, 2.6], [0.52, 0.5, 0.38, 0.05, -0.45, -0.85])
    band = inside(edge - zt, 0.03)                             # above the edge
    tri_ = inside(np.abs(q[:, 0]) - 0.33 * np.clip(zt - 0.06, 0, None) / 0.46, 0.012) * inside(0.06 - zt, 0.012) \
        * inside(a - 0.6, 0.02)
    near = inside(((q * np.array([0.88, 0.88, 0.88])) ** 2).sum(1) - 1.0, 0.02) * (P[:, 2] > 0.14)
    return np.maximum(band, tri_) * near


def flap(k, side):
    """A long broad grey flap from the hood's side down past the cheek to the chest, its tip square and yellow; its
    face turned some 35 degrees toward the front."""
    s = 1 if side == "R" else -1
    root = on_ell(H, HR, (0.7 * s, 0.2, 0.68)) - np.array([0.008 * s, 0, 0.004])
    mid = [root, (0.078 * s, -0.014, 0.226), (0.088 * s, -0.02, 0.165), (0.088 * s, -0.024, 0.11),
           (0.083 * s, -0.022, 0.062)]
    C = spline(mid, 40)
    w = unit(0.45 * s, 0.89, 0.0)                              # across the flap: back and out
    width = 0.04

    def piece(u0, u1, thick):
        def at(u, v):
            t = u0 + (u1 - u0) * u
            i = np.clip((t * 39).astype(int), 0, 38); f = (t * 39 - i)[..., None]
            c = C[i] * (1 - f) + C[i + 1] * f
            wd = width * (0.62 + 0.38 * smoothstep(0.0, 0.35, t))
            return c + w[None, None] * ((v - 0.5) * wd)[..., None]
        return outward(sheet(at, 26, 6, thick))
    k.add(f"flap{side}", piece(0.0, 0.81, 0.009), GREY, "head", fuse=False)
    k.add(f"flaptip{side}", piece(0.79, 1.0, 0.0108), YELLOW, "head", fuse=False)


def smile(k, c, r, x0, z0, rise, n=7, half=(0.0062, 0.0034)):
    """A wide thin smile on the face of the ellipsoid (c, r): dashes along z = z0 + rise (x / x0)^2."""
    for i in range(n):
        x = -x0 + 2 * x0 * i / (n - 1)
        z = z0 + rise * (x / x0) ** 2
        q = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
        p = np.array([x, c[1] - r[1] * math.sqrt(max(q, 0.02)), z])
        slope = 2 * rise * x / x0 ** 2
        k.decal(p, unit(x / r[0] * 0.5, -1, 0.1), half, MOUTH, up=(-slope, 0, 1), eps=0.0018, rings=5, segs=20)


def helioptile(src, out):
    k = Kit(src, coat="scales")

    # The body: small, the chest raised over the forelegs, the hips low over the hind legs, a thin neck
    k.add("torso", ell((0, 0.012, 0.078), (0.037, 0.05, 0.035), rot=(-26, 0, 0)), YELLOW, "body")
    k.add("chest", ell((0, -0.022, 0.112), (0.036, 0.032, 0.038)), YELLOW, "body")
    k.add("hips", ell((0, 0.036, 0.06), (0.039, 0.035, 0.031)), YELLOW, "body")
    k.add("neck", tube([(0, -0.024, 0.128), (0, -0.03, 0.15), (0, -0.033, 0.168)], [0.022, 0.021, 0.024]), YELLOW,
          "head")
    head(k)

    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # Hind legs: a haunch fused into the hips, a short shin, a small foot with three dark toes
        k.add(f"thigh{side}", ell(np.array((0.036, 0.032, 0.05)) * m, (0.026, 0.031, 0.031)), YELLOW, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.04, 0.03, 0.04), (0.045, 0.018, 0.013)]) * m, [0.0155, 0.013]),
              YELLOW, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.046, 0.008, 0.008)) * m, (0.0135, 0.019, 0.0075)), YELLOW, f"leg{side}")
        toes(k, f"htoe{side}", f"leg{side}", np.array((0.047, -0.006, 0.006)) * m, (0.25 * s, -1, 0), 0.016, 0.0048)
        # Forelegs from the shoulder down and a little forward
        k.add(f"fore{side}", tube(np.array([(0.028, -0.024, 0.138), (0.04, -0.042, 0.08), (0.046, -0.056, 0.016)]) * m,
                                  [0.0155, 0.0128, 0.0118]), YELLOW, f"arm{side}")
        k.add(f"hand{side}", ell(np.array((0.047, -0.063, 0.008)) * m, (0.012, 0.016, 0.0072)), YELLOW, f"arm{side}")
        toes(k, f"ftoe{side}", f"arm{side}", np.array((0.048, -0.075, 0.006)) * m, (0.3 * s, -1, 0), 0.015, 0.0046)
        flap(k, side)

    # The tail: back and out to the +X side, low, rising at its end, the tip dark grey
    tail(k, [(0, 0.035, 0.072), (0.02, 0.095, 0.05), (0.05, 0.152, 0.046), (0.076, 0.198, 0.066),
             (0.088, 0.228, 0.108)], [0.025, 0.019, 0.0145, 0.011, 0.0075], 0.74)

    # The hood over the crown and the nape, a triangle down between the eyes
    k.paint(hood_mask, GREY)

    # Two nostrils on the muzzle, a wide thin smile under them
    for s in (1, -1):
        k.decal(on_ell(MUZ_C, MUZ_R, (0.32 * s, -0.8, 0.5)), unit(0.25 * s, -0.8, 0.55), (0.0042, 0.0036), NOSTRIL,
                eps=0.0018, rings=4, segs=16)
    smile(k, MUZ_C, MUZ_R, 0.028, MUZ_C[2] - 0.013, 0.009, n=11, half=(0.0058, 0.0036))

    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.022)

    E = on_ell(H, HR, EYE_DIR)
    look = blue_eye()
    k.eye_states(lambda state: lay_eyes(k, state, E, EYE_DIR, EYE_HALF, look, srgb(YELLOW), INK))
    k.export(out)


if __name__ == "__main__":
    helioptile(*args())
