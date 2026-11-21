# Persian-Alola sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py),
# grown from persian.py and meowth.py (imported: the body, the ears' way, the curled tail, the gem's way):
#   blender --background --factory-startup --python sculpts/persianalola.py -- <the export's Persian-Alola.glb> <out.glb>
#
# From the references: Persian's sleek body in a cool lilac grey, on legs a little thicker, carrying a great round
# face like a moon, half as wide again as the base's and wider than the shoulders, held low and forward; rounded ears
# set on the head's upper sides and leaning well out (some 40 degrees off upright), slate outside and pale inside; a
# small round blue gem on the brow; scowling white eyes, each cut by a dark lid sloping to the nose, under a lavender
# shadow; a small dark nose, a little w of a mouth with two fangs; three long whiskers a side; the long tail curled.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, polygon, eye_look, args, sibling

p = sibling("persian")
m = p.m

COAT = "#BCBEDA"
EAR, LINING = "#3C445E", "#D2D4E6"
GEM, GEM_LIGHT = "#3FA6E0", "#BFE8FA"
SHADOW = "#AAA3D4"
EYE_W, PUPIL, INK = "#FFFFFF", "#1C1C26", "#262634"
NOSE = "#323A52"
WHISKER = "#ECEEF6"

# --- the frame: the head the first mass, carried low and forward of its joint (0, -0.16, 0.38) ------------------------
H = np.array([0, -0.245, 0.4])
HR = np.array([0.178, 0.112, 0.152])
LEG_X, FORE_Y, HIND_Y = p.LEG_X, p.FORE_Y, p.HIND_Y
EAR_BASE = np.array([0.098, -0.225, 0.5])
EAR_TIP = np.array([0.232, -0.21, 0.588])
EAR_R = [0.068, 0.07, 0.05, 0.02]


def on_head(x, z, lift=0.0):
    return m.on_head(x, z, lift, h=H, hr=HR)


def add_head(k):
    k.add("head", ell(H, HR), COAT, "head")
    # Full cheeks: the face is a wide disc, broadest below the eyes
    k.add("cheek", ell(H + (0.085, -0.025, -0.05), (0.1, 0.078, 0.085)), COAT, "head", mirror=True)
    k.add("muzzle", ell(H + (0, -0.092, -0.052), (0.05, 0.036, 0.036)), COAT, "head")
    k.add("neck", tube([(0, -0.09, 0.33), (0, -0.15, 0.37), (0, -0.2, 0.4)], [0.078, 0.074, 0.07]), COAT, "head")
    # A deeper belly between the legs, so less daylight shows under it
    k.add("belly", ell((0, 0.0, 0.27), (0.074, 0.15, 0.085)), COAT, "body")


def add_legs(k):
    """The base's legs, half as thick again in the lower leg: broad shoulders and thighs over tapering legs."""
    k.add("shoulder", ell((LEG_X - 0.002, FORE_Y + 0.01, 0.25), (0.056, 0.066, 0.09)), COAT, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.24), (LEG_X + 0.002, FORE_Y - 0.008, 0.13), (LEG_X + 0.004, FORE_Y - 0.016, 0.04)],
                      [0.048, 0.038, 0.034]), COAT, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.005, FORE_Y - 0.036, 0.024), (0.04, 0.052, 0.026)), COAT, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X, HIND_Y + 0.005, 0.25), (0.062, 0.084, 0.1)), COAT, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X + 0.002, HIND_Y + 0.02, 0.2), (LEG_X + 0.004, HIND_Y + 0.04, 0.11),
                        (LEG_X + 0.004, HIND_Y + 0.012, 0.04)], [0.048, 0.037, 0.034]), COAT, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.005, HIND_Y - 0.008, 0.024), (0.04, 0.052, 0.026)), COAT, "hind{s}", mirror=True)


def add_gem(k, z=0.085, r=0.021):
    """A small round blue jewel on the brow: a faceted dome, a loose piece."""
    c, n = on_head(0.0, H[2] + z, 0.004)
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    side = np.cross(up, n)
    pts = []
    for i in range(10):
        a = 2 * math.pi * (i + 0.5) / 10
        pts.append(c + side * r * math.cos(a) + up * r * math.sin(a) - n * 0.004)
        pts.append(c + side * r * 0.7 * math.cos(a) + up * r * 0.7 * math.sin(a) + n * r * 0.6)
    pts.append(c + n * r * 0.85)
    k.add("gem", hull(pts), GEM, "head", fuse=False, surface="metal")


def add_whiskers(k):
    side = [((0.12, -0.31, 0.375), (0.2, -0.315, 0.39), (0.275, -0.31, 0.415)),
            ((0.125, -0.31, 0.358), (0.205, -0.31, 0.36), (0.285, -0.3, 0.362)),
            ((0.12, -0.305, 0.341), (0.2, -0.3, 0.326), (0.27, -0.29, 0.31))]
    for i, pts in enumerate(side):
        k.add(f"whisker{i}", tube(pts, [0.0085, 0.0068, 0.0052], seg=8, per=6), WHISKER, "head",
              fuse=False, mirror=True, surface="hair")


def mouth(u, v):
    """A little w under the nose."""
    c = np.tile(srgb(COAT), (len(u), 1))
    line = 1.6 * (np.abs(u) - 0.5) ** 2 - 0.1
    c[(np.abs(v - line) < 0.3) & (np.abs(u) < 0.95)] = srgb(INK)
    return c


def add_face(k):
    tip = H + np.array([0, -0.128, -0.04])
    k.decal(tip, unit(0, -1, 0.3), (0.018, 0.013), NOSE, eps=0.003,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)
    mo = H + np.array([0, -0.126, -0.072])
    k.decal(mo, unit(0, -1, -0.1), (0.042, 0.02), mouth, eps=0.003, rings=12, segs=56)
    for s in (1, -1):
        top = H + np.array([0.02 * s, -0.12, -0.078])
        k.add(f"fang{'R' if s > 0 else 'L'}", hull([top + (0.007, 0.004, 0), top + (-0.007, 0.004, 0),
                                                    top + (0, -0.004, 0), top + (0.0, 0.0, -0.018)]),
              "#FFFFFF", "head", fuse=False, surface="shell")
    # The lavender shadow over each eye, in every state
    for s in (1, -1):
        q, n = on_head(0.068 * s, H[2] + 0.018)
        k.decal(q, n, (0.058, 0.042), SHADOW, eps=0.003, rings=12, segs=48, up=unit(0.25 * s, 0, 1))


# --- the eyes: narrow and scowling, the top edge a straight dark lid sloping to the nose, white with a dark pupil ----
OUTLINE = [(-1, -0.05), (-0.4, 0.3), (0.4, 0.62), (1, 0.7), (0.9, 0.05), (0.35, -0.55), (-0.35, -0.62), (-0.85, -0.38)]
_RIM = polygon(OUTLINE)
EYE_HALF = (0.047, 0.035)


def eye(u, v):
    c = np.tile(srgb(EYE_W), (len(u), 1))
    c[np.hypot((u + 0.12) / 0.2, (v + 0.05) / 0.34) < 1] = srgb(PUPIL)
    rr = np.hypot(u, v) / np.array([_RIM(t) for t in np.arctan2(v, u)])
    c[(rr > 0.68) & (v > 0.0 + 0.35 * u)] = srgb(INK)
    c[(rr > 0.84)] = srgb(INK)
    return c


def lay_eyes(k, state):
    q, n = on_head(0.068, H[2] + 0.006)
    look = eye_look(state, eye, srgb(SHADOW), srgb(INK))
    k.decal(q, n, EYE_HALF, look, eps=0.005, outline=polygon(OUTLINE), rings=20, segs=56)
    f = np.array([-1, 1, 1.0])
    k.decal(q * f, n * f, EYE_HALF, lambda u, v: look(-u, v), eps=0.005,
            outline=polygon([(-x, y) for x, y in OUTLINE]), rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    p.CREAM = COAT
    k = Kit(src, coat="fur")
    p.add_body(k)
    add_legs(k)
    add_head(k)
    m.add_ears(k, base=EAR_BASE, tip=EAR_TIP, radii=EAR_R, outer=EAR, lining=LINING, inset=0.62, coat=COAT, root=0.28)
    p.add_tail(k)
    add_gem(k)
    add_whiskers(k)
    add_face(k)
    k.build(voxel=0.0058, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
